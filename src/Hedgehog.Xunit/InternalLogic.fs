module internal InternalLogic

open Hedgehog
open System.Reflection
open System
open Hedgehog.Xunit
open System.Threading.Tasks
open System.Threading
open System.Linq
open Hedgehog.FSharp

type private Marker = class end // helps with using System.Reflection
let private genxAutoBoxWith<'T> x = x |> Gen.autoWith<'T> |> Gen.map box
let private genxAutoBoxWithMethodInfo =
  typeof<Marker>.DeclaringType.GetTypeInfo().GetDeclaredMethod "genxAutoBoxWith"

let resultIsOk r =
  match r with
  | Ok _ -> true
  | Error e -> failwithf $"Result is in the Error case with the following value:%s{Environment.NewLine}%A{e}"

// awaits, or asserts that the value is true, or is in the `Ok` state, etc.
let rec yieldAndCheckReturnValue (x: obj) =
  match x with
  | :? bool        as b -> if not b then TestReturnedFalseException() |> raise
  | :? Task<unit>  as t -> Async.AwaitTask t |> yieldAndCheckReturnValue
  | :? Task<bool>  as t -> Async.AwaitTask t |> yieldAndCheckReturnValue
  | _ when x <> null && x.GetType().IsGenericType && typeof<Task>.IsAssignableFrom(x.GetType()) ->
    typeof<Async>
      .GetMethods()
      .First(fun x -> x.Name = "AwaitTask" && x.IsGenericMethod)
      .MakeGenericMethod(x.GetType().GetGenericArguments().First())
      .Invoke(null, [|x|])
    |> yieldAndCheckReturnValue
  | :? Task        as t -> Async.AwaitTask t |> yieldAndCheckReturnValue
  | :? ValueTask   as vt -> vt.AsTask() |> Async.AwaitTask |> yieldAndCheckReturnValue
  | _ when x <> null && x.GetType().IsGenericType && x.GetType().GetGenericTypeDefinition() = typedefof<ValueTask<_>> ->
    let asTaskMethod = x.GetType().GetMethod("AsTask")
    asTaskMethod.Invoke(x, null)
    |> yieldAndCheckReturnValue
  | :? Async<unit> as a -> Async.RunSynchronously(a, cancellationToken = CancellationToken.None) |> yieldAndCheckReturnValue
  | _ when x <> null && x.GetType().IsGenericType && x.GetType().GetGenericTypeDefinition() = typedefof<Async<_>> ->
    typeof<Async> // Invoked with Reflection because we can't cast an Async<MyType> to Async<obj> https://stackoverflow.com/a/26167206
      .GetMethod("RunSynchronously")
      .MakeGenericMethod(x.GetType().GetGenericArguments())
      .Invoke(null, [| x; None; Some CancellationToken.None |])
    |> yieldAndCheckReturnValue
  | _ when x <> null && x.GetType().IsGenericType && x.GetType().GetGenericTypeDefinition() = typedefof<Result<_,_>> ->
    typeof<Marker>
      .DeclaringType
      .GetTypeInfo()
      .GetDeclaredMethod("resultIsOk")
      .MakeGenericMethod(x.GetType().GetGenericArguments())
      .Invoke(null, [|x|])
    |> yieldAndCheckReturnValue
  | _                   -> ()

let dispose (o:obj) =
  match o with
  | :? IDisposable as d -> d.Dispose()
  | _ -> ()

let withTests = function
  | Some x -> PropertyConfig.withTests x
  | None -> id

let withShrinks = function
  | Some x -> PropertyConfig.withShrinks x
  | None -> PropertyConfig.withoutShrinks

let report (context: PropertyContext) (testMethod: MethodInfo) testClassInstance =
  let getAttributeGenerator (parameterInfo: ParameterInfo) =
    parameterInfo.GetCustomAttributes()
    |> Seq.tryPick(fun attr ->
      let attrType = attr.GetType().BaseType
      if attrType.IsGenericType && attrType.GetGenericTypeDefinition().IsAssignableFrom(typedefof<GenAttribute<_>>) then
        attrType
          .GetMethods()
          .First(fun x -> x.Name = "Box")
          .Invoke(attr, null)
          :?> Gen<obj> |> Some
      else
        None
    )

  let gens =
    testMethod.GetParameters()
    |> Array.map (fun p ->
      match (getAttributeGenerator p,  p.ParameterType.ContainsGenericParameters) with
        | Some gen, _ -> gen
        | _ , true    -> Gen.constant Unchecked.defaultof<_>
        | _ , false   -> genxAutoBoxWithMethodInfo
                           .MakeGenericMethod(p.ParameterType)
                           .Invoke(null, [| context.AutoGenConfig |])
                           :?> Gen<obj>)
    |> List.ofArray
    |> Gen.sequenceList
  let gens =
    match context.Size, context.Recheck with
    | _        , Some _ // could pull the size out of the recheckData... but it seems like it isn't necessary? Unable to write failing test.
    | None     ,      _ -> gens
    | Some size,      _ -> gens |> Gen.resize size
  let property =
    let invoke args =
      try
        ( if testMethod.ContainsGenericParameters then
            Array.create
              (testMethod.GetGenericArguments().Length)
              typeof<obj>
            |> fun x -> testMethod.MakeGenericMethod x
          else
            testMethod
        ) |> _.Invoke(testClassInstance, args |> Array.ofList)
        // `testMethod` is the body of a method that has been decorated with the [<Property>] attribute.
        // Above, we `Invoke` `testMethod`. `Invoke` returns whatever `testMethod` returns.
      finally
        List.iter dispose args
    if testMethod.ReturnType = typeof<Hedgehog.Property<unit>> then
      property.Bind(gens, fun x -> invoke x :?> Property<unit>)
    elif testMethod.ReturnType = typeof<Hedgehog.Property<bool>> then
      property.Bind(gens, fun x -> invoke x :?> Property<bool>)
      |> Property.falseToFailure
    else
      property.BindReturn(gens, invoke >> yieldAndCheckReturnValue)
  let config =
    PropertyConfig.defaults
    |> withTests context.Tests
    |> withShrinks context.Shrinks
  property
  |> match context.Recheck with
     | Some recheckData -> Property.reportRecheckWith recheckData config
     | None             -> Property.reportWith config

let tryRaise (report : Report) : unit =
  match report.Status with
  | Failed _ -> report |> Report.render |> Exception |> raise // todo: make it print the attribute (instead of using the default Hedgehog output)
  | _ -> Report.tryRaise report
