module internal InternalLogic

open Hedgehog
open Hedgehog.FSharp
open Hedgehog.Xunit
open System
open System.Reflection
open System.Runtime.ExceptionServices
open System.Threading
open System.Threading.Tasks

// ========================================
// Type Utilities & Helpers
// ========================================

type private Marker = class end

[<Literal>]
let private GenxAutoBoxMethodName = "genxAutoBoxWith"

[<Literal>]
let private ResultIsOkMethodName = "resultIsOk"

let private genxAutoBoxWith<'T> x =
    x |> Gen.autoWith<'T> |> Gen.map box

let private genxAutoBoxWithMethodInfo =
    typeof<Marker>.DeclaringType.GetTypeInfo().GetDeclaredMethod(GenxAutoBoxMethodName)

// ========================================
// Result Validation
// ========================================

let resultIsOk r =
    match r with
    | Ok _ -> true
    | Error e ->
        failwithf $"Result is in the Error case with the following value:%s{Environment.NewLine}%A{e}"

// ========================================
// Return Value Processing
// ========================================

/// Recursively awaits async/task values and validates boolean/Result types
/// Returns true if test passes, false if it fails
let rec yieldAndCheckReturnValue (x: obj) : bool =
    match x with
    | null -> true
    | :? bool as b -> b
    
    // Non-generic Task
    | :? Task as t when not (t.GetType().IsGenericType) ->
        Async.AwaitTask t |> yieldAndCheckReturnValue
    
    // Non-generic ValueTask
    | :? ValueTask as vt ->
        vt.AsTask() |> Async.AwaitTask |> yieldAndCheckReturnValue
    
    // Async<unit> - common case, avoid reflection
    | :? Async<unit> as a ->
        Async.RunSynchronously(a, cancellationToken = CancellationToken.None)
        |> yieldAndCheckReturnValue
    
    // Generic types requiring reflection
    | x ->
        let t = x.GetType()
        match t with
        | t when ReflectionHelpers.isGenericTask t ->
            ReflectionHelpers.invokeAwaitTask x |> yieldAndCheckReturnValue
        | t when ReflectionHelpers.isGenericValueTask t ->
            t.GetMethod("AsTask").Invoke(x, null)
            |> yieldAndCheckReturnValue
        | t when ReflectionHelpers.isAsync t ->
            ReflectionHelpers.invokeAsyncRunSynchronously x |> yieldAndCheckReturnValue
        | t when ReflectionHelpers.isResult t ->
            ReflectionHelpers.invokeResultIsOk x typeof<Marker>.DeclaringType ResultIsOkMethodName
            |> yieldAndCheckReturnValue
        | _ -> true

// ========================================
// Resource Management
// ========================================

let dispose (o: obj) =
    match o with
    | :? IDisposable as d -> d.Dispose()
    | _ -> ()

// ========================================
// Value Formatting & Display
// ========================================

let printValue (value: obj) : string =
    let prepareForPrinting (value: obj) : obj =
        if isNull value then
            value
        else
            let typeInfo = IntrospectionExtensions.GetTypeInfo(value.GetType())
            let isResizeArray = typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() = typedefof<ResizeArray<_>>
            if isResizeArray then
                value :?> System.Collections.IEnumerable
                |> Seq.cast<obj>
                |> List.ofSeq
                :> obj
            else
                value

    value |> prepareForPrinting |> sprintf "%A"

let formatParametersWithNames (parameters: ParameterInfo[]) (values: obj list) : string =
    Array.zip parameters (List.toArray values)
    |> Array.map (fun (param, value) ->
        $"%s{param.Name} = %s{printValue value}")
    |> String.concat Environment.NewLine

// ========================================
// Configuration Helpers
// ========================================

let withTests = function
    | Some x -> PropertyConfig.withTests x
    | None -> id

let withShrinks = function
    | Some x -> PropertyConfig.withShrinks x
    | None -> PropertyConfig.withoutShrinks

// ========================================
// Generator Creation
// ========================================

module private GeneratorFactory =
    /// Tries to get a custom generator from GenAttribute on a parameter
    let tryGetAttributeGenerator (parameterInfo: ParameterInfo) : Gen<obj> option =
        parameterInfo.GetCustomAttributes()
        |> Seq.tryPick (fun attr ->
            let attrType = attr.GetType().BaseType
            let isGenAttribute =
                attrType.IsGenericType &&
                attrType.GetGenericTypeDefinition().IsAssignableFrom(typedefof<GenAttribute<_>>)

            if isGenAttribute then
                let boxMethod = attrType.GetMethods() |> Array.find (fun m -> m.Name = "Box")
                boxMethod.Invoke(attr, null) :?> Gen<obj> |> Some
            else
                None)

    /// Creates a generator for a parameter based on attribute or type
    let createGenerator (autoGenConfig: obj) (parameter: ParameterInfo) : Gen<obj> =
        match tryGetAttributeGenerator parameter, parameter.ParameterType.ContainsGenericParameters with
        | Some gen, _ ->
            gen
        | _, true ->
            Gen.constant Unchecked.defaultof<_>
        | _, false ->
            genxAutoBoxWithMethodInfo
                .MakeGenericMethod(parameter.ParameterType)
                .Invoke(null, [| autoGenConfig |])
                :?> Gen<obj>

    /// Creates a list generator for all test method parameters
    let createParameterListGenerator (context: PropertyContext) (parameters: ParameterInfo[]) : Gen<obj list> =
        let gens =
            parameters
            |> Array.map (createGenerator context.AutoGenConfig)
            |> List.ofArray
            |> Gen.sequenceList

        match context.Size, context.Recheck with
        | _, Some _ -> gens  // Size from recheck data if present
        | Some size, _ -> gens |> Gen.resize size
        | None, _ -> gens

// ========================================
// Property Creation
// ========================================

module private PropertyBuilder =
    /// Invokes the test method with the given arguments
    let invokeTestMethod (testMethod: MethodInfo) (testClassInstance: obj) (args: obj list) : obj =
        let methodToInvoke =
            if testMethod.ContainsGenericParameters then
                let genericArgs = Array.create (testMethod.GetGenericArguments().Length) typeof<obj>
                testMethod.MakeGenericMethod(genericArgs)
            else
                testMethod

        try
            methodToInvoke.Invoke(testClassInstance, args |> Array.ofList)
        with
        | :? TargetInvocationException as tie when not (isNull tie.InnerException) ->
            // Unwrap reflection exception to show the actual user exception instead of TargetInvocationException.
            // We use ExceptionDispatchInfo.Capture().Throw() to preserve the original stack trace.
            // Note: This adds a "--- End of stack trace from previous location ---" marker
            // and appends additional frames as the exception propagates, which we filter out later.
            ExceptionDispatchInfo.Capture(tie.InnerException).Throw()
            failwith "unreachable"

    /// Creates a property based on the test method's return type
    let createProperty
        (testMethod: MethodInfo)
        (testClassInstance: obj)
        (parameters: ParameterInfo[])
        (gens: Gen<obj list>) : Property<unit> =

        let invoke args =
            try
                invokeTestMethod testMethod testClassInstance args
            finally
                List.iter dispose args

        let createJournal args =
            let formattedParams = formatParametersWithNames parameters args
            Journal.singleton (fun () -> formattedParams)

        // Handle Property<unit> return type
        if testMethod.ReturnType = typeof<Property<unit>> then
            Property.bindWith createJournal (invoke >> unbox<Property<unit>>) gens

        // Handle Property<bool> return type
        elif testMethod.ReturnType = typeof<Property<bool>> then
            Property.bindWith createJournal (invoke >> unbox<Property<bool>>) gens
            |> Property.falseToFailure

        // Handle all other return types (Task, Async, bool, Result, etc.)
        else
            Property.bindReturnWith createJournal (invoke >> yieldAndCheckReturnValue) gens
            |> Property.falseToFailure

// ========================================
// Report Generation
// ========================================

let report (context: PropertyContext) (testMethod: MethodInfo) testClassInstance : Report =
    let parameters = testMethod.GetParameters()
    let gens = GeneratorFactory.createParameterListGenerator context parameters
    let property = PropertyBuilder.createProperty testMethod testClassInstance parameters gens

    let config =
        PropertyConfig.defaults
        |> withTests context.Tests
        |> withShrinks context.Shrinks

    match context.Recheck with
    | Some recheckData -> Property.reportRecheckWith recheckData config property
    | None -> Property.reportWith config property
