// ============================================================================
// DocFX API Documentation Generator
// ============================================================================
//
// PURPOSE:
//   This script generates DocFX-compatible YAML documentation from F# assemblies,
//   with intelligent handling of F# module patterns and dual-language (F#/C#) support.
//
// KEY FEATURES:
//
// 1. AUTO-OPEN MODULE MERGING
//    F# often uses a pattern where an outer module is marked [<AutoOpen>] and wraps
//    an inner module marked [<RequireQualifiedAccess>]. This creates redundant nesting
//    in documentation (e.g., Hedgehog.GenPrimitives.Gen instead of just Hedgehog.Gen).
//    
//    When SkipAutoOpenWrappers is enabled, the script:
//    - Detects AutoOpen wrapper modules
//    - Merges their RequireQualifiedAccess inner modules into a single documentation page
//    - Uses the minimal path (skipping AutoOpen segments) for cleaner API references
//    - Combines all members from merged entities, sorted alphabetically
//    
//    Example: Hedgehog.GenPrimitives (AutoOpen).Gen (RQA) becomes just Hedgehog.Gen
//
// 2. DUAL-LANGUAGE SYNTAX GENERATION
//    The script generates API documentation from both F# and C# perspectives:
//    
//    F# Style:
//      - Uses F# type syntax (unit, 'a, list, option, etc.)
//      - Presents functions with curried signatures (a -> b -> c)
//      - Default for most namespaces
//    
//    C# Style:
//      - Converts to C# types (void, T, [], ?, IEnumerable, etc.)
//      - Shows static method signatures with explicit parameters
//      - Includes "this" keyword for extension methods
//      - Applied to namespaces in CSharpNamespaces set (e.g., Hedgehog.Linq)
//    
//    Both syntaxes are included in the YAML (content.fsharp and content.csharp)
//    allowing DocFX to present the API in the user's preferred language.
//
// 3. HIERARCHICAL TOC GENERATION
//    Builds a hierarchical Table of Contents:
//    - Flattens the API structure, collecting all entities
//    - Excludes AutoOpen wrappers and merged paths
//    - Reconstructs a clean namespace hierarchy from dot-separated paths
//    - Supports intermediate namespace nodes (containers without files)
//
// 4. DOCUMENTATION PROCESSING
//    Converts XML documentation to Markdown:
//    - Extracts summary, remarks, and examples from F# XML docs
//    - Converts HTML to Markdown using ReverseMarkdown
//    - Handles F# doc-specific elements like <see cref="..."/>
//    - Preserves code formatting and structure
//
// 5. MEMBER OVERLOAD HANDLING
//    For C# namespaces with method overloading:
//    - Generates unique UIDs with parameter-based suffixes
//    - Creates display names showing parameter types for clarity
//    - Ensures each overload has a distinct documentation entry
//
// WORKFLOW:
//   1. Load assembly using FSharp.Formatting.ApiDocs
//   2. Discover AutoOpen patterns and modules to merge
//   3. Generate DocFX YAML files for each entity (merged or standalone)
//   4. Create type signatures in both F# and C# syntax
//   5. Build hierarchical TOC from flattened entity paths
//   6. Write YAML files with DocFX ManagedReference format
//
// USAGE:
//   Run this script with F# Interactive:
//     dotnet fsi gen-docfx.fsx [buildConfiguration]
//   
//   Examples:
//     dotnet fsi gen-docfx.fsx           # Uses Debug (default)
//     dotnet fsi gen-docfx.fsx Release   # Uses Release
//   
//   Customize by modifying defaultConfig or creating a new DocFxConfig instance.
//
// ============================================================================

#r "nuget: FSharp.Compiler.Service, 43.8.400"
#r "nuget: FSharp.Formatting"
#r "nuget: YamlDotNet"
#r "nuget: ReverseMarkdown"
#r "nuget: System.Text.Json"

open System
open System.IO
open System.Text.Json
open FSharp.Formatting.ApiDocs
open FSharp.Formatting.Templating
open YamlDotNet.Serialization
open YamlDotNet.Serialization.NamingConventions

open ReverseMarkdown
open FSharp.Compiler.Symbols
open FSharp.Compiler.Text

// ============================================================================
// CONFIGURATION
// ============================================================================

/// Configuration for DocFX generation
[<CLIMutable>]
type DocFxConfig = {
    /// Assemblies to generate documentation from (path, collection name)
    Assemblies: (string * string) list
    /// Output directory for generated YAML files
    OutputDir: string

    /// Git repository URL
    RepoUrl: string
    /// Git branch name
    Branch: string

    /// Root URL for substitutions
    RootUrl: string
    /// Skip AutoOpen wrapper modules and merge their inner RequireQualifiedAccess modules
    SkipAutoOpenWrappers: bool
    /// Generate C# syntax for specified namespaces (e.g., "Hedgehog.Linq")
    CSharpNamespaces: Set<string>
}

/// Default configuration for Hedgehog project
let defaultConfig = {
    Assemblies = [
        ("src/Hedgehog/bin/Debug/net8.0/Hedgehog.dll", "Hedgehog")
        ("src/Hedgehog.Xunit/bin/Debug/net8.0/Hedgehog.Xunit.dll", "Hedgehog.Xunit")
    ]
    OutputDir = "docs/api"

    RepoUrl = "https://github.com/hedgehogqa/fsharp-hedgehog.git"
    Branch = "master"

    RootUrl = "/"
    SkipAutoOpenWrappers = true
    CSharpNamespaces = Set.ofList ["Hedgehog.Linq"]
}

// ============================================================================
// DEPENDENCY RESOLUTION
// ============================================================================

module DependencyResolver =
    
    // Minimal types for deps.json structure
    type DepsRuntime = { runtime: Map<string, obj> option }
    type DepsJson = { targets: Map<string, Map<string, DepsRuntime>> }
    
    let private nugetCache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages")
    
    let private tryResolveDll (packageName: string) (version: string) (dllPath: string) =
        if dllPath.EndsWith(".dll") then
            let nugetPath = Path.Combine(nugetCache, packageName.ToLowerInvariant(), version, dllPath.Replace('/', Path.DirectorySeparatorChar))
            if File.Exists(nugetPath) then 
                Some (Path.GetDirectoryName(nugetPath))
            else None
        else None
    
    let private tryResolvePackage (assemblyName: string) (packageKey: string, packageInfo: DepsRuntime) =
        match packageKey.Split('/'), packageInfo.runtime with
        | [| packageName; version |], Some runtimeDlls when packageName <> assemblyName ->
            runtimeDlls
            |> Map.toSeq
            |> Seq.tryPick (fun (dllPath, _) -> tryResolveDll packageName version dllPath)
            |> Option.iter (fun dir -> printfn $"  Found dependency: %s{packageName} -> %s{dir}")
            |> fun _ -> runtimeDlls |> Map.toSeq |> Seq.tryPick (fun (dllPath, _) -> tryResolveDll packageName version dllPath)
        | _ -> None
    
    /// Parse deps.json to find all runtime library directories
    let resolveLibDirs (assemblyPath: string) : string list =
        let assemblyDir = Path.GetDirectoryName(assemblyPath)
        let assemblyName = Path.GetFileNameWithoutExtension(assemblyPath)
        let depsJsonPath = Path.Combine(assemblyDir, assemblyName + ".deps.json")
        
        if not (File.Exists(depsJsonPath)) then
            printfn $"Warning: deps.json not found at %s{depsJsonPath}"
            [assemblyDir]
        else
            try
                let deps = JsonSerializer.Deserialize<DepsJson>(File.ReadAllText(depsJsonPath), JsonSerializerOptions(PropertyNameCaseInsensitive = true))
                
                let dependencyDirs =
                    deps.targets.Values
                    |> Seq.collect Map.toSeq
                    |> Seq.choose (tryResolvePackage assemblyName)
                    |> Seq.distinct
                    |> Seq.toList

                assemblyDir :: dependencyDirs
            with ex ->
                printfn $"Warning: Could not parse deps.json: %s{ex.Message}"
                [assemblyDir]

// ============================================================================
// DOCFX DATA MODELS
// ============================================================================

[<CLIMutable>]
type DocFxSyntax = {
    content: string
    [<YamlMember(Alias = "content.fsharp")>]
    contentFSharp: string
    [<YamlMember(Alias = "content.csharp")>]
    contentCSharp: string
    // parameters: ... (optional, for detailed breakdown)
    // return: ... (optional)
}

[<CLIMutable>]
type DocFxRemote = {
    path: string
    branch: string
    repo: string
}

[<CLIMutable>]
type DocFxSource = {
    remote: DocFxRemote
    id: string
    path: string
    startLine: int
}

[<CLIMutable>]
type DocFxItem = {
    uid: string
    commentId: string
    id: string
    parent: string
    children: string list
    langs: string list
    name: string
    nameWithType: string
    fullName: string
    [<YamlMember(Alias = "type")>]
    type_: string // Class, Method, Property, etc.
    assemblies: string list
    [<YamlMember(Alias = "namespace")>]
    namespace_: string
    summary: string
    // example: string list (optional)
    syntax: DocFxSyntax
    source: DocFxSource
}

[<CLIMutable>]
type DocFxFile = {
    items: DocFxItem list
}

// ============================================================================
// HTML & TEXT PROCESSING
// ============================================================================

module TextProcessing =
    
    /// HTML to Markdown converter instance
    let private converter = 
        let config = Config(
            GithubFlavored = true,
            UnknownTags = Config.UnknownTagsOption.PassThrough,
            SmartHrefHandling = true
        )
        Converter(config)
    
    /// Extract HTML text from ApiDocHtml objects
    let getHtmlText (html: ApiDocHtml) : string =
        html.HtmlText
    
    /// Strip HTML tags and decode entities (for type signatures)
    let stripHtml (html: string) : string =
        if String.IsNullOrWhiteSpace html then ""
        else
            html
                // Remove all HTML tags
                |> fun s -> System.Text.RegularExpressions.Regex.Replace(s, "<[^>]+>", "")
                // Decode HTML entities
                |> System.Net.WebUtility.HtmlDecode
                |> fun s -> s.Trim()
    
    /// Convert HTML to Markdown using ReverseMarkdown
    let htmlToMarkdown (html: string) : string =
        if String.IsNullOrWhiteSpace html then ""
        else
            // Pre-process XML doc specific tags that ReverseMarkdown doesn't handle
            let preprocessed =
                html
                    // Remove <description> tags (XML doc specific)
                    .Replace("<description>", "").Replace("</description>", "")
                    // Convert <see cref="T:Foo"/> to just the type name in backticks
                    |> fun s -> System.Text.RegularExpressions.Regex.Replace(s, "<see cref=\"[^:]*:([^\"]+)\"\s*/?>", "`$1`")
                    |> fun s -> System.Text.RegularExpressions.Regex.Replace(s, "<see cref=\"([^\"]+)\"\s*/?>", "`$1`")
                    |> fun s -> System.Text.RegularExpressions.Regex.Replace(s, "<see cref=\"[^\"]+\">([^<]+)</see>", "`$1`")
            
            converter.Convert(preprocessed).Trim()
    
    /// Extract and process documentation from ApiDocComment
    let getDocumentation (comment: ApiDocComment) : string =
        let parts = ResizeArray<string>()
        
        // Add summary
        let summary = 
            comment.Summary
            |> getHtmlText
            |> htmlToMarkdown
        if not (String.IsNullOrWhiteSpace summary) then
            parts.Add(summary)
        
        // Add remarks if available
        let remarks = 
            match comment.Remarks with
            | Some r -> r |> getHtmlText |> htmlToMarkdown
            | None -> ""
        if not (String.IsNullOrWhiteSpace remarks) then
            parts.Add(remarks)
        
        // Add examples if available
        for example in comment.Examples do
            let exampleText = 
                example
                |> getHtmlText
                |> htmlToMarkdown
            if not (String.IsNullOrWhiteSpace exampleText) then
                parts.Add("**Example:**\n\n" + exampleText)
        
        String.concat "\n\n" parts
    
    /// Extract and clean summary text from ApiDocComment (backward compatibility)
    let getSummary (comment: ApiDocComment) : string =
        getDocumentation comment

// ============================================================================
// TYPE SIGNATURE EXTRACTION
// ============================================================================

module TypeSignature =
    
    /// Check if a member is an extension method
    let isExtensionMethod (member': ApiDocMember) : bool =
        member'.Attributes 
        |> List.exists (fun attr -> 
            attr.FullName = "System.Runtime.CompilerServices.ExtensionAttribute")
    
    /// Extract all parameter types from a member
    let extractParameters (member': ApiDocMember) : string list =
        [ for p in member'.Parameters ->
            p.ParameterType
            |> TextProcessing.getHtmlText
            |> TextProcessing.stripHtml ]
    
    /// Extract parameter names from a member
    let extractParameterNames (member': ApiDocMember) : string list =
        [ for p in member'.Parameters ->
            p.ParameterNameText ]
    
    /// Extract return type information from a member
    let extractReturnType (member': ApiDocMember) : string =
        try
            match member'.ReturnInfo.ReturnType with
            | None -> "unit"
            | Some (_, returnTypeHtml) ->
                returnTypeHtml
                |> TextProcessing.getHtmlText
                |> TextProcessing.stripHtml
        with _ -> "unit"
    
    /// Convert F# type to C# type representation
    let fsharpToCSharp (fsharpType: string) : string =
        // First do specific replacements
        let intermediate =
            fsharpType
                .Replace("unit", "void")
                .Replace(" list", "[]")
                .Replace(" option", "?")
                .Replace(" seq", "IEnumerable")
        
        // Replace all F# type parameters (e.g., 'T, 'U, 'V, 'a, etc.) with C# equivalents
        let withoutQuotes = System.Text.RegularExpressions.Regex.Replace(intermediate, @"'(\w+)", "$1")
        
        // Convert F# tuple syntax (T * U * V) to C# tuple syntax (T, U, V)
        // Strategy: Find tuple patterns (separated by *), wrap in parens, then replace * with ,
        // Match anything containing * and wrap it in parentheses, then convert * to ,
        System.Text.RegularExpressions.Regex.Replace(
            withoutQuotes,
            @"(\w+(?:\s*\*\s*\w+)+)",
            (fun (m: System.Text.RegularExpressions.Match) ->
                // Found a tuple pattern like "T * U" or "T * U * V"
                let tuple = m.Groups[1].Value
                let withCommas = tuple.Replace(" * ", ", ")
                "(" + withCommas + ")"))
    
    /// Generate F# syntax signature for a member
    let generateFSharpSyntax (member': ApiDocMember) : string =
        try
            let paramTypes = extractParameters member'
            let returnType = extractReturnType member'
            
            if List.isEmpty paramTypes then
                $"val %s{member'.Name} : %s{returnType}"
            else
                let paramsStr = String.concat " -> " paramTypes
                $"val %s{member'.Name} : %s{paramsStr} -> %s{returnType}"
        with _ ->
            $"val %s{member'.Name} : ..."

    /// Generate C# syntax signature for a member
    let generateCSharpSyntax (member': ApiDocMember) : string =
        try
            let paramTypes = extractParameters member'
            let paramNames = extractParameterNames member'
            let returnType = extractReturnType member' |> fsharpToCSharp
            let isExtension = isExtensionMethod member'
            
            if List.isEmpty paramTypes then
                $"public static %s{returnType} %s{member'.Name}()"
            else
                let paramsWithNames = List.zip paramTypes paramNames
                let paramsStr = 
                    paramsWithNames 
                    |> List.mapi (fun i (paramType, paramName) -> 
                        let csharpType = fsharpToCSharp paramType
                        // Add "this" to the first parameter if it's an extension method
                        let thisKeyword = if isExtension && i = 0 then "this " else ""
                        $"%s{thisKeyword}%s{csharpType} %s{paramName}")
                    |> String.concat ", "

                $"public static %s{returnType} %s{member'.Name}(%s{paramsStr})"
        with _ ->
            $"public static void %s{member'.Name}(...)"

// ============================================================================
// API DOC ENTITY HELPERS
// ============================================================================

module ApiDocHelpers =
    
    /// Get all members from an entity (static members, instance members, and values/functions)
    let getMembers (entity: ApiDocEntity) : ApiDocMember list =
        Seq.append entity.StaticMembers (Seq.append entity.InstanceMembers entity.ValuesAndFuncs)
        |> Seq.filter (fun m -> 
            m.Kind = ApiDocMemberKind.StaticMember || 
            m.Kind = ApiDocMemberKind.InstanceMember ||
            m.Kind = ApiDocMemberKind.ValueOrFunction)
        |> Seq.toList
    
    /// Generate type syntax showing its members (F# style)
    let generateTypeSyntaxFSharp (typeName: string) (members: ApiDocMember list) : string =
        if List.isEmpty members then
            $"type %s{typeName}"
        else
            let memberSignatures = 
                members
                |> List.sortBy _.Name
                |> List.map (fun m ->
                    let isStatic = m.Kind = ApiDocMemberKind.StaticMember
                    let prefix = if isStatic then "static " else ""
                    let signature = TypeSignature.generateFSharpSyntax m
                    // Extract just the signature part (after the "val name : ")
                    let typeStr = 
                        if signature.Contains(" : ") then
                            signature.Substring(signature.IndexOf(" : ") + 3)
                        else
                            "..."

                    $"  %s{prefix}member %s{m.Name} : %s{typeStr}")
                |> fun sigs -> String.concat "\n" sigs

            $"type %s{typeName} =\n%s{memberSignatures}"

    /// Generate type syntax showing its members (C# style)
    let generateTypeSyntaxCSharp (typeName: string) (members: ApiDocMember list) : string =
        if List.isEmpty members then
            $"public class %s{typeName}"
        else
            let memberSignatures = 
                members
                |> List.sortBy (fun m -> m.Name)
                |> List.map TypeSignature.generateCSharpSyntax
                |> fun sigs -> String.concat "\n  " sigs

            $"public class %s{typeName} {{\n  %s{memberSignatures}\n}}"

    /// Get nested entities from an entity
    let getNestedEntities (entity: ApiDocEntity) : ApiDocEntity list =
        entity.NestedEntities |> Seq.toList
    
    /// Check if an entity has the AutoOpen attribute
    let isAutoOpen (entity: ApiDocEntity) : bool =
        entity.Attributes 
        |> List.exists (fun attr -> 
            attr.Name = "AutoOpen" || 
            attr.FullName = "Microsoft.FSharp.Core.AutoOpenAttribute")
    
    /// Sanitize UID by replacing invalid characters
    let sanitizeUid (uid: string) : string =
        uid.Replace("<", "_").Replace(">", "_").Replace("'", "")
    
    /// Create a source reference from config
    /// Create a source reference from config and member info
    let createSource (config: DocFxConfig) (filePath: string) (line: int) (id: string) : DocFxSource =
        let relativePath = 
            if String.IsNullOrWhiteSpace filePath then ""
            else Path.GetRelativePath(Environment.CurrentDirectory, filePath).Replace("\\", "/")
        
        {
            remote = { 
                path = relativePath
                branch = config.Branch
                repo = config.RepoUrl 
            }
            id = id
            path = relativePath
            startLine = line
        }
    
    /// Check if a namespace should use C# syntax
    let isCSharpNamespace (config: DocFxConfig) (nsName: string) : bool =
        config.CSharpNamespaces |> Set.contains nsName
    
    /// Create syntax object with appropriate language support
    let createSyntax (config: DocFxConfig) (nsName: string) (fsharpContent: string) (csharpContent: string) : DocFxSyntax =
        if isCSharpNamespace config nsName then
            {
                content = csharpContent
                contentFSharp = fsharpContent
                contentCSharp = csharpContent
            }
        else
            {
                content = fsharpContent
                contentFSharp = fsharpContent
                contentCSharp = csharpContent
            }

// ============================================================================
// DOCFX MAPPING
// ============================================================================

module DocFxMapping =
    
    /// Generate a unique UID suffix for overloaded methods based on parameters
    let generateOverloadSuffix (m: ApiDocMember) : string =
        let paramCount = m.Parameters |> Seq.length
        if paramCount = 0 then
            ""
        else
            let paramTypes = TypeSignature.extractParameters m
            // Create a short signature based on parameter count and types
            let typeSignature = 
                paramTypes 
                |> List.map (fun t -> 
                    // Simplify type names for the suffix
                    let simplified = 
                        t.Replace("Microsoft.FSharp.Core.FSharpFunc", "Func")
                         .Replace("Microsoft.FSharp.Collections.FSharpList", "List")
                         .Replace("System.", "")
                         .Replace("Hedgehog.", "")
                    // Take just the last part of the type name
                    let parts = simplified.Split([|'.'; '<'; '>'; ','; ' '|], StringSplitOptions.RemoveEmptyEntries)
                    if parts.Length > 0 then parts[0] else "T")
                |> String.concat "-"
            
            if String.IsNullOrWhiteSpace typeSignature then
                $"-%d{paramCount}"
            else
                $"-%s{typeSignature}"

    /// Generate a unique UID for a member with overload suffix
    let generateMemberUid (entityPath: string) (m: ApiDocMember) : string =
        let baseName = $"%s{entityPath}.%s{m.Name}"
        let overloadSuffix = generateOverloadSuffix m
        ApiDocHelpers.sanitizeUid (baseName + overloadSuffix)
    
    /// Generate a display name with parameter types for overloaded methods
    let generateDisplayName (config: DocFxConfig) (nsName: string) (m: ApiDocMember) : string =
        // For F# namespaces, just use the name (F# doesn't support overloading)
        // For C# namespaces, include parameter types for clarity
        if ApiDocHelpers.isCSharpNamespace config nsName then
            let paramCount = m.Parameters |> Seq.length
            if paramCount = 0 then
                m.Name
            else
                let paramTypes = TypeSignature.extractParameters m
                let typeList = 
                    paramTypes 
                    |> List.map (fun t ->
                        // Simplify for display
                        let simplified = 
                            t.Replace("Microsoft.FSharp.Core.FSharpFunc", "Func")
                             .Replace("Microsoft.FSharp.Collections.FSharpList", "List")
                             .Replace("System.", "")
                             .Replace("Hedgehog.", "")
                        let parts = simplified.Split([|'.'; '<'; '>'; ','; ' '|], StringSplitOptions.RemoveEmptyEntries)
                        if parts.Length > 0 then parts[0] else "T")
                    |> String.concat ", "

                $"%s{m.Name}(%s{typeList})"
        else
            // For F# code, just use the member name
            m.Name

    /// Map an API member to a DocFX item
    let mapMember (config: DocFxConfig) (collectionName: string) (nsName: string) (entityPath: string) (m: ApiDocMember) : DocFxItem =
        let uid = generateMemberUid entityPath m
        let displayName = generateDisplayName config nsName m
        let fsharpSyntax = TypeSignature.generateFSharpSyntax m
        let csharpSyntax = TypeSignature.generateCSharpSyntax m
        {
            uid = uid
            commentId = $"M:%s{uid}"
            id = m.Name
            parent = ApiDocHelpers.sanitizeUid entityPath
            children = []
            langs = if ApiDocHelpers.isCSharpNamespace config nsName then ["csharp"; "fsharp"] else ["fsharp"]
            name = displayName
            nameWithType = displayName
            fullName = uid
            type_ = "Method"
            assemblies = [collectionName]
            namespace_ = nsName
            summary = TextProcessing.getSummary m.Comment
            syntax = ApiDocHelpers.createSyntax config nsName fsharpSyntax csharpSyntax
            source = 
                try
                    match m.Symbol.DeclarationLocation with
                    | Some loc -> ApiDocHelpers.createSource config loc.FileName loc.StartLine m.Name
                    | None -> 
                        match m.SourceLocation with
                        | Some file -> ApiDocHelpers.createSource config file 1 m.Name
                        | None -> ApiDocHelpers.createSource config "" 0 m.Name
                with _ ->
                    match m.SourceLocation with
                    | Some file -> ApiDocHelpers.createSource config file 1 m.Name
                    | None -> ApiDocHelpers.createSource config "" 0 m.Name
        }

// ============================================================================
// TOC (Table of Contents) MODELS & GENERATION
// ============================================================================

[<CLIMutable>]
type TocItem = {
    uid: string
    name: string
    [<YamlMember(Alias = "href")>]
    href: string
    [<YamlMember(Alias = "items")>]
    items: TocItem list
}

// ============================================================================
// MODULE MERGING
// ============================================================================

/// Type to hold information about a merged module
type MergedModule = {
    Name: string
    FullPath: string
    ParentPath: string
    NamespaceName: string
    InnerEntities: ApiDocEntity list
}

module ModuleMerging =
    
    /// Build a full path by appending entity name to parent path
    let buildFullPath (parentPath: string) (entityName: string) : string =
        $"%s{parentPath}.%s{entityName}"

    /// Calculate the minimal path by skipping AutoOpen segments
    let rec getMinimalPath (parentMinimal: string) (entity: ApiDocEntity) : string =
        if ApiDocHelpers.isAutoOpen entity then
            parentMinimal  // Skip AutoOpen - don't add to path
        else
            buildFullPath parentMinimal entity.Name
    
    /// Recursively collect all entities with their paths
    let rec collectEntities (nsName: string) (fullPath: string) (minimalPath: string) (entity: ApiDocEntity) : (string * string * string * ApiDocEntity) list =
        let newFullPath = buildFullPath fullPath entity.Name
        let newMinimalPath = getMinimalPath minimalPath entity
        
        let current = 
            if ApiDocHelpers.isAutoOpen entity then []
            else [(newMinimalPath, newFullPath, nsName, entity)]
        
        let nested =
            ApiDocHelpers.getNestedEntities entity
            |> List.collect (fun ne -> collectEntities nsName newFullPath newMinimalPath ne)
        
        current @ nested
    
    /// Find modules that need to be merged (multiple entities with same minimal path)
    let findMergedModules (namespaces: ApiDocNamespace list) : MergedModule list =
        namespaces
        |> List.collect (fun ns ->
            ns.Entities
            |> List.collect (fun entity -> collectEntities ns.Name ns.Name ns.Name entity))
        |> List.groupBy (fun (minimalPath, _, _, _) -> minimalPath)
        |> List.choose (fun (minimalPath, group) ->
            if List.length group > 1 then
                let _, _, nsName, _ = List.head group
                let entities = group |> List.map (fun (_, _, _, e) -> e)
                let name = minimalPath.Split('.') |> Array.last
                let parentPath = minimalPath.Substring(0, minimalPath.LastIndexOf('.'))
                Some { 
                    Name = name
                    FullPath = minimalPath
                    ParentPath = parentPath
                    NamespaceName = nsName
                    InnerEntities = entities 
                }
            else
                None)
    
    /// Collect all AutoOpen entity paths recursively
    let rec collectAutoOpenPaths (parentPath: string) (entity: ApiDocEntity) : string list =
        let fullPath = buildFullPath parentPath entity.Name
        let current = if ApiDocHelpers.isAutoOpen entity then [fullPath] else []
        let nested = 
            ApiDocHelpers.getNestedEntities entity
            |> List.collect (fun ne -> collectAutoOpenPaths fullPath ne)
        current @ nested

module TocGeneration =
    
    /// Represents a flattened TOC item with its full path
    type FlatTocItem = {
        FullPath: string
        Uid: string
        Name: string
        Href: string
        CollectionName: string
    }
    
    /// Tree node for building the hierarchy
    type private TreeNode = {
        Path: string
        Children: Map<string, TreeNode>
    }
    
    /// Recursively build a flat list of TOC items for an entity and its nested entities
    let rec collectFlatTocItems (collectionName: string) (excludePaths: Set<string>) (nsName: string) (parentPath: string) (entity: ApiDocEntity) : FlatTocItem list =
        let fullPath = ModuleMerging.buildFullPath parentPath entity.Name
        
        // Skip entities that were merged or are AutoOpen wrappers
        if Set.contains fullPath excludePaths then
            []
        else
            let uid = ApiDocHelpers.sanitizeUid fullPath
            let current = {
                FullPath = fullPath
                Uid = uid
                Name = entity.Name
                Href = $"%s{uid}.yml"
                CollectionName = collectionName
            }
            
            let nested =
                ApiDocHelpers.getNestedEntities entity
                |> List.collect (fun ne -> collectFlatTocItems collectionName excludePaths nsName fullPath ne)
            
            current :: nested
    
    /// Build a hierarchical tree from a flat list of items, grouped by collection
    let buildTree (items: FlatTocItem list) : TocItem list =
        // Group items by collection
        let itemsByCollection = items |> List.groupBy (fun i -> i.CollectionName)
        
        // For each collection, build a hierarchy
        itemsByCollection
        |> List.map (fun (collectionName, collectionItems) ->
            // Create a map for quick lookup of items
            let itemMap = collectionItems |> List.map (fun i -> i.FullPath, i) |> Map.ofList
            
            /// Insert a path into the tree
            let rec insertPath (segments: string list) (currentPath: string list) (node: TreeNode) : TreeNode =
                match segments with
                | [] -> node
                | segment :: rest ->
                    let childPath = currentPath @ [segment]
                    let childKey = segment
                    let childNode = 
                        match Map.tryFind childKey node.Children with
                        | Some existing -> existing
                        | None -> { Path = String.concat "." childPath; Children = Map.empty }
                    let updatedChild = insertPath rest childPath childNode
                    { node with Children = Map.add childKey updatedChild node.Children }
            
            /// Convert tree nodes to TocItems
            let rec nodeToTocItem (node: TreeNode) : TocItem =
                let children = 
                    node.Children 
                    |> Map.toList 
                    |> List.sortBy fst
                    |> List.map (snd >> nodeToTocItem)
                
                match Map.tryFind node.Path itemMap with
                | Some item ->
                    // Has an actual file - include uid and href
                    { uid = item.Uid; name = item.Name; href = item.Href; items = children }
                | None ->
                    // Intermediate namespace node - no uid, just a container
                    let name = node.Path.Split('.') |> Array.last
                    { uid = ""; name = name; href = ""; items = children }
            
            // Build the tree by inserting all paths
            let root = { Path = ""; Children = Map.empty }
            let tree = 
                collectionItems
                |> List.fold (fun acc item ->
                    let segments = item.FullPath.Split('.') |> Array.toList
                    insertPath segments [] acc) root
            
            // Convert to TocItems - get all children of root
            let childItems = 
                tree.Children 
                |> Map.toList 
                |> List.sortBy fst
                |> List.map (snd >> nodeToTocItem)
            
            // Create a root node for this collection
            { uid = ""; name = collectionName; href = ""; items = childItems })
        |> List.sortBy (fun item -> item.name)

// ============================================================================
// FILE I/O
// ============================================================================

module FileIO =
    
    /// Setup output directory (create if needed, clean old YAML files)
    let setupOutputDirectory (outputDir: string) : unit =
        if not (Directory.Exists(outputDir)) then
            Directory.CreateDirectory(outputDir) |> ignore
        
        // Delete only YAML files, preserving manually created files
        Directory.GetFiles(outputDir, "*.yml")
        |> Array.iter File.Delete
    
    /// Write a DocFX YAML file
    let writeYamlFile (serializer: ISerializer) (outputDir: string) (filename: string) (docFxFile: DocFxFile) : unit =
        let yaml = serializer.Serialize(docFxFile)
        let fullPath = $"%s{outputDir}/%s{filename}"
        File.WriteAllText(fullPath, "### YamlMime:ManagedReference\n" + yaml)
    
    /// Write TOC YAML file
    let writeTocFile (serializer: ISerializer) (outputDir: string) (tocItems: TocItem list) : unit =
        // Prepend the API Documentation index page
        let indexItem = {
            uid = ""
            name = "API Documentation"
            href = "index.md"
            items = []
        }
        let allItems = indexItem :: tocItems
        
        let tocYaml = serializer.Serialize(allItems)
        // Remove empty uid fields (uid: '') from the YAML
        let cleanedYaml = 
            System.Text.RegularExpressions.Regex.Replace(
                tocYaml, 
                @"^\s*uid:\s*''\s*$", 
                "", 
                System.Text.RegularExpressions.RegexOptions.Multiline)
        File.WriteAllText($"%s{outputDir}/toc.yml", cleanedYaml)

// ============================================================================
// ENTITY PROCESSING
// ============================================================================

module EntityProcessing =
    
    /// Process a merged module and write YAML file
    let processMergedModule (config: DocFxConfig) (collectionName: string) (serializer: ISerializer) (merged: MergedModule) : unit =
        let uid = ApiDocHelpers.sanitizeUid merged.FullPath
        
        // Collect all members from all inner entities and sort alphabetically
        let allMembers = merged.InnerEntities |> List.collect ApiDocHelpers.getMembers
        let memberChildren = 
            allMembers 
            |> List.map (DocFxMapping.generateMemberUid merged.FullPath)
        
        // Use the first non-empty summary
        let summary = 
            merged.InnerEntities
            |> List.map (fun e -> TextProcessing.getSummary e.Comment)
            |> List.filter (fun s -> not (String.IsNullOrWhiteSpace s))
            |> List.tryHead
            |> Option.defaultValue ""
        
        // Create syntax for both languages
        let fsharpSyntax = ApiDocHelpers.generateTypeSyntaxFSharp merged.Name allMembers
        let csharpSyntax = ApiDocHelpers.generateTypeSyntaxCSharp merged.Name allMembers
        
        // Create the merged module item
        let item = {
            uid = uid
            commentId = $"T:%s{uid}"
            id = merged.Name
            parent = merged.ParentPath
            children = memberChildren
            langs = if ApiDocHelpers.isCSharpNamespace config merged.NamespaceName then ["csharp"; "fsharp"] else ["fsharp"]
            name = merged.Name
            nameWithType = merged.Name
            fullName = merged.FullPath
            type_ = "Class"
            assemblies = [collectionName]
            namespace_ = merged.NamespaceName
            summary = summary
            syntax = ApiDocHelpers.createSyntax config merged.NamespaceName fsharpSyntax csharpSyntax
            source = 
                match merged.InnerEntities with
                | first :: _ ->
                    try
                        let loc = first.Symbol.DeclarationLocation
                        ApiDocHelpers.createSource config loc.FileName loc.StartLine merged.Name
                    with _ -> ApiDocHelpers.createSource config "" 0 merged.Name
                | [] -> ApiDocHelpers.createSource config "" 0 merged.Name
        }
        
        let memberItems = 
            allMembers
            |> List.sortBy (fun m -> m.Name)
            |> List.map (DocFxMapping.mapMember config collectionName merged.NamespaceName merged.FullPath)
        
        let docFxFile = { items = item :: memberItems }
        let filename = $"%s{uid}.yml"
        FileIO.writeYamlFile serializer config.OutputDir filename docFxFile
    
    /// Process a regular entity and write YAML file
    let rec processEntity (config: DocFxConfig) (collectionName: string) (serializer: ISerializer) (excludePaths: Set<string>) (nsName: string) (parentPath: string) (minimalPath: string) (entity: ApiDocEntity) : unit =
        let fullPath = ModuleMerging.buildFullPath parentPath entity.Name
        let newMinimalPath = ModuleMerging.getMinimalPath minimalPath entity
        
        // Skip if AutoOpen or merged
        if not (ApiDocHelpers.isAutoOpen entity) && not (Set.contains newMinimalPath excludePaths) then
            let members = ApiDocHelpers.getMembers entity
            let uid = ApiDocHelpers.sanitizeUid fullPath
            let memberChildren = 
                members 
                |> List.map (DocFxMapping.generateMemberUid fullPath)
            
            // Create syntax for both languages
            let fsharpSyntax = ApiDocHelpers.generateTypeSyntaxFSharp entity.Name members
            let csharpSyntax = ApiDocHelpers.generateTypeSyntaxCSharp entity.Name members
            
            let item = {
                uid = uid
                commentId = $"T:%s{uid}"
                id = entity.Name
                parent = parentPath
                children = memberChildren
                langs = if ApiDocHelpers.isCSharpNamespace config nsName then ["csharp"; "fsharp"] else ["fsharp"]
                name = entity.Name
                nameWithType = entity.Name
                fullName = fullPath
                type_ = "Class"
                assemblies = [collectionName]
                namespace_ = nsName
                summary = TextProcessing.getSummary entity.Comment
                syntax = ApiDocHelpers.createSyntax config nsName fsharpSyntax csharpSyntax
                source = 
                try
                    let loc = entity.Symbol.DeclarationLocation
                    ApiDocHelpers.createSource config loc.FileName loc.StartLine entity.Name
                with _ -> ApiDocHelpers.createSource config "" 0 entity.Name
            }
            
            let memberItems = 
                members
                |> List.sortBy (fun m -> m.Name)
                |> List.map (DocFxMapping.mapMember config collectionName nsName fullPath)
            
            let docFxFile = { items = item :: memberItems }
            let filename = $"%s{uid}.yml"
            FileIO.writeYamlFile serializer config.OutputDir filename docFxFile
        
        // Process nested entities
        ApiDocHelpers.getNestedEntities entity
        |> List.iter (fun ne -> processEntity config collectionName serializer excludePaths nsName fullPath newMinimalPath ne)

// ============================================================================
// MAIN EXECUTION
// ============================================================================

/// Main entry point
let generateDocFx (config: DocFxConfig) : unit =
    printfn "Generating DocFX documentation for multiple assemblies..."
    
    // Setup serializer - disable anchors/aliases for DocFX compatibility
    let serializer = 
        SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreFields()
            .DisableAliases()
            .Build()
    
    // Setup output directory
    FileIO.setupOutputDirectory config.OutputDir
    
    // Process each assembly separately and collect TOC items
    let allTocItems = ResizeArray<TocGeneration.FlatTocItem>()
    
    for (assemblyPath, collectionName) in config.Assemblies do
        printfn $"\nProcessing assembly: %s{collectionName} (%s{assemblyPath})"
        
        // Resolve library directories from deps.json
        let libDirs = DependencyResolver.resolveLibDirs assemblyPath
        
        // Setup inputs and substitutions
        let inputs = [ ApiDocInput.FromFile(assemblyPath) ]
        let substitutions : Substitutions =
            [ ParamKey("root"), config.RootUrl
              ParamKey("fsdocs-collection-name"), collectionName ]
        
        // Generate API model for this assembly
        printfn "  Generating ApiDocModel..."
        let model = 
            ApiDocs.GenerateModel(
                inputs = inputs,
                collectionName = collectionName,
                substitutions = substitutions,
                libDirs = libDirs,
                qualify = false
            )
        
        printfn "  Mapping to DocFX format..."
        
        // Get all namespaces
        let namespaces = model.Collection.Namespaces |> List.ofSeq
        
        // Find modules that need to be merged
        let mergedModules = 
            if config.SkipAutoOpenWrappers then
                ModuleMerging.findMergedModules namespaces
            else []
        
        let mergedPaths = mergedModules |> List.map (fun m -> m.FullPath) |> Set.ofList
        
        // Collect all AutoOpen paths recursively for TOC exclusion
        let autoOpenPaths =
            namespaces
            |> List.collect (fun ns ->
                ns.Entities
                |> List.collect (fun e -> ModuleMerging.collectAutoOpenPaths ns.Name e))
            |> Set.ofList
        
        let excludePaths = Set.union autoOpenPaths mergedPaths
        
        // Process merged modules first
        for merged in mergedModules do
            EntityProcessing.processMergedModule config collectionName serializer merged
        
        // Process regular entities
        for ns in namespaces do
            for entity in ns.Entities do
                EntityProcessing.processEntity config collectionName serializer mergedPaths ns.Name ns.Name ns.Name entity
        
        // Collect TOC items for this assembly
        let flatTocItems = 
            namespaces
            |> List.collect (fun ns ->
                ns.Entities
                |> List.collect (fun e -> TocGeneration.collectFlatTocItems collectionName excludePaths ns.Name ns.Name e))
        
        // Add merged modules as flat items
        let mergedFlatItems = 
            mergedModules
            |> List.map (fun m -> {
                TocGeneration.FullPath = m.FullPath
                TocGeneration.Uid = ApiDocHelpers.sanitizeUid m.FullPath
                TocGeneration.Name = m.Name
                TocGeneration.Href = $"%s{ApiDocHelpers.sanitizeUid m.FullPath}.yml"
                TocGeneration.CollectionName = collectionName
            })
        
        // Add all items from this assembly to the global collection
        allTocItems.AddRange(flatTocItems @ mergedFlatItems)
        
        printfn $"  Done! Generated documentation for %d{namespaces.Length} namespaces"
    
    // Build hierarchical TOC from all collected items
    let allFlatItems = 
        allTocItems
        |> Seq.toList
        |> List.distinctBy (fun i -> i.Uid)
    
    let tocTree = TocGeneration.buildTree allFlatItems
    FileIO.writeTocFile serializer config.OutputDir tocTree

    printfn $"\nCompleted! Generated documentation for %d{config.Assemblies.Length} assemblies in %s{config.OutputDir}"


// ============================================================================
// COMMAND-LINE ARGUMENTS
// ============================================================================

/// Parse command-line arguments and run with configuration
let runWithArgs (args: string[]) : unit =
    let buildConfiguration = 
        if args.Length > 0 then args[0]
        else "Debug"
    
    let assemblies = [
        ($"src/Hedgehog/bin/%s{buildConfiguration}/net8.0/Hedgehog.dll", "Hedgehog")
        ($"src/Hedgehog.Xunit/bin/%s{buildConfiguration}/net8.0/Hedgehog.Xunit.dll", "Hedgehog.Xunit")
    ]
    
    let config = { defaultConfig with Assemblies = assemblies }
    
    printfn $"Using build configuration: %s{buildConfiguration}"
    printfn $"Assemblies: %A{assemblies |> List.map snd}"
    
    generateDocFx config

// Run with command-line arguments
runWithArgs fsi.CommandLineArgs[1..]
