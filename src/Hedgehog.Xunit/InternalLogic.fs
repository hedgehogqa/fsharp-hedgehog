module internal InternalLogic

open Hedgehog
open Hedgehog.FSharp
open Hedgehog.Xunit
open System
open System.Reflection
open System.Threading.Tasks

// ========================================
// Type Utilities & Helpers
// ========================================

type private Marker = class end

[<Literal>]
let private GenxAutoBoxMethodName = "genxAutoBoxWith"

[<Literal>]
let private ResultIsOkMethodName = "resultIsOk"

[<Literal>]
let private ConvertAsyncToObjMethodName = "convertAsyncToObj"

let private convertAsyncToObj<'T> (a: Async<'T>) : Async<obj> =
    async {
        let! x = a
        return box x
    }

let private genxAutoBoxWith<'T> x =
    x |> Gen.autoWith<'T> |> Gen.map box

let private genxAutoBoxWithMethodInfo =
    typeof<Marker>.DeclaringType.GetTypeInfo().GetDeclaredMethod(GenxAutoBoxMethodName)

let private convertAsyncToObjMethodInfo =
    typeof<Marker>.DeclaringType.GetTypeInfo().GetDeclaredMethod(ConvertAsyncToObjMethodName)

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

let private toAsyncObj (asyncVal: obj) (t: Type) : Async<obj> =
    convertAsyncToObjMethodInfo
        .MakeGenericMethod(t)
        .Invoke(null, [| asyncVal |])
    |> unbox<Async<obj>>

/// Wraps a test method return value into a Property, handling async/task natively
let rec wrapReturnValue (x: obj) : Property<unit> =
    match x with
    | null -> Property.success ()
    | :? bool as b -> Property.ofBool b
    | :? Property<unit> as p -> p
    | :? Property<bool> as p -> p |> Property.falseToFailure
    
    // Non-generic Task
    | :? Task as t when not (t.GetType().IsGenericType) ->
        Property.ofTaskUnit t
    
    // Non-generic ValueTask
    | :? ValueTask as vt ->
        vt.AsTask() |> Property.ofTaskUnit
    
    // Async<unit> - common case, avoid reflection
    | :? Async<unit> as a ->
        Property.ofAsync a |> Property.map (fun _ -> ())
    
    // Generic types requiring reflection
    | x ->
        let t = x.GetType()
        match t with
        | t when ReflectionHelpers.isGenericTask t ->
            let taskResultType = t.GetGenericArguments().[0]
            let asyncVal = ReflectionHelpers.invokeAwaitTask x
            // Use toAsyncObj to avoid InvalidCastException, then wrap in Property
            let asyncObj = toAsyncObj asyncVal taskResultType
            Property.ofAsync asyncObj |> Property.map wrapReturnValue |> Property.bind id
            
        | t when ReflectionHelpers.isGenericValueTask t ->
            let task = t.GetMethod("AsTask").Invoke(x, null)
            wrapReturnValue task
            
        | t when ReflectionHelpers.isAsync t ->
            let asyncResultType = t.GetGenericArguments().[0]
            // Use toAsyncObj to avoid InvalidCastException, then wrap in Property
            let asyncObj = toAsyncObj x asyncResultType
            Property.ofAsync asyncObj |> Property.map wrapReturnValue |> Property.bind id
            
        | t when ReflectionHelpers.isResult t ->
            // Wrap the Result in a Property and use map to check it
            // This ensures exceptions from resultIsOk are caught by Property.map
            Property.success x
            |> Property.map (fun r ->
                let isOk = ReflectionHelpers.invokeResultIsOk r typeof<Marker>.DeclaringType ResultIsOkMethodName :?> bool
                if not isOk then failwith "Result is Error")
            
        | _ -> Property.success ()

// ========================================
// Resource Management
// ========================================

let dispose (o: obj) =
    match o with
    | :? IDisposable as d -> d.Dispose()
    | _ -> ()

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

        methodToInvoke.Invoke(testClassInstance, args |> Array.ofList)


    /// Creates a property based on the test method's return type
    let createProperty
        (testMethod: MethodInfo)
        (testClassInstance: obj)
        (parameters: ParameterInfo[])
        (gens: Gen<obj list>) : Property<unit> =

        let invoke args =
            try
                try
                    invokeTestMethod testMethod testClassInstance args
                finally
                    List.iter dispose args
            with
                // Unwrap TargetInvocationException to get the actual exception.
                // It is safe to do it because invokeTestMethod uses reflection that adds this wrapper.
                | :? TargetInvocationException as e when not (isNull e.InnerException) ->
                    box e.InnerException
                | e -> box e

        let createJournal args =
            let parameterEntries =
                Array.zip parameters (List.toArray args)
                |> Array.map (fun (param, value) -> 
                    fun () -> TestParameter (param.Name, value))
                |> Array.toSeq
            Journal.ofSeq parameterEntries
        
        let wrapWithExceptionHandling (result: obj) : Property<unit> =
            match result with
            | :? exn as e -> Property.exn e
            | _ -> wrapReturnValue result


        // Handle Property<unit> return type
        if testMethod.ReturnType = typeof<Property<unit>> then
            Property.bindWith createJournal (invoke >> unbox<Property<unit>>) gens
        
        // Handle Property<bool> return type
        elif testMethod.ReturnType = typeof<Property<bool>> then
            Property.bindWith createJournal (invoke >> unbox<Property<bool>>) gens
            |> Property.falseToFailure
        
        // Handle all other return types (Task, Async, bool, Result, etc.)
        else
            Property.bindWith createJournal (invoke >> wrapWithExceptionHandling) gens


// ========================================
// Report Generation
// ========================================

let reportAsync (context: PropertyContext) (testMethod: MethodInfo) testClassInstance : Async<Report> =
    let parameters = testMethod.GetParameters()
    let gens = GeneratorFactory.createParameterListGenerator context parameters
    let property = PropertyBuilder.createProperty testMethod testClassInstance parameters gens

    let config =
        PropertyConfig.defaults
        |> withTests context.Tests
        |> withShrinks context.Shrinks

    match context.Recheck with
    | Some recheckData -> 
        Property.reportRecheckWith recheckData config property |> async.Return
    | None -> 
        Property.reportAsyncWith config property
