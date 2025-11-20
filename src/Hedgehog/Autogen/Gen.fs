namespace Hedgehog

open System
open Hedgehog.AutoGen
open TypeShape.Core

[<AutoOpen>]
module AutoGenExtensions =

  [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
  module Gen =
    let rec private autoInner<'a> (config : AutoGenConfig) (recursionState: RecursionState) : Gen<'a> =

      // Prevent auto-generating AutoGenConfig itself - it should only be passed as a parameter
      if typeof<'a> = typeof<AutoGenConfig> then
        raise (NotSupportedException "Cannot auto-generate AutoGenConfig type. It should be provided as a parameter to generator methods.")

      match recursionState |> RecursionState.reconcileFor<'a> config with
      | None ->
        Gen.delay (fun () ->
          raise (InvalidOperationException(
            sprintf "Recursion depth limit %d exceeded for type %s. " (AutoGenConfig.recursionDepth config) typeof<'a>.FullName +
            "To fix this, add a RecursionContext parameter to your generator method and use recursionContext.CanRecurse to control recursion.")))

      | Some newRecursionState ->

        let genPoco (shape: ShapePoco<'a>) =
          let bestCtor =
            shape.Constructors
            |> Seq.filter _.IsPublic
            |> Seq.sortBy _.Arity
            |> Seq.tryHead

          match bestCtor with
          | None -> failwithf "Class %O lacks a public constructor" typeof<'a>
          | Some ctor ->
            ctor.Accept {
            new IConstructorVisitor<'a, Gen<unit -> 'a>> with
              member __.Visit<'CtorParams> (ctor : ShapeConstructor<'a, 'CtorParams>) =
                autoInner config newRecursionState
                |> Gen.map (fun args ->
                    let delayedCtor () =
                      try
                        ctor.Invoke args
                      with
                        | ex ->
                          ArgumentException(sprintf "Cannot construct %O with the generated argument(s): %O. %s" typeof<'a> args AutoGenHelpers.addGenMsg, ex)
                          |> raise
                    delayedCtor
                )
            }

        let wrap (t : Gen<'b>) = unbox<Gen<'a>> t

        let memberSetterGenerator (shape: IShapeMember<'DeclaringType>) =
          shape.Accept {
            new IMemberVisitor<'DeclaringType, Gen<'DeclaringType -> 'DeclaringType>> with
            member _.Visit(shape: ShapeMember<'DeclaringType, 'MemberType>) =
              autoInner<'MemberType> config newRecursionState
              |> Gen.map (fun mtValue -> fun dt ->
                try
                  shape.Set dt mtValue
                with
                  | ex ->
                    ArgumentException(sprintf "Cannot set the %s property of %O to the generated value of %O. %s" shape.Label dt mtValue AutoGenHelpers.addGenMsg, ex)
                    |> raise
              )
          }

        let typeShape = TypeShape.Create<'a> ()

        // Check if there is a registered generator factory for a given requested generator.
        // Fallback to the default heuristics if no factory is found.
        match config.generators |> GeneratorCollection.tryFindFor typeof<'a> with
        | Some (registeredType, (args, factory)) ->
          let factoryArgs = AutoGenHelpers.prepareFactoryArgTypes typeShape registeredType args

          // and if the factory takes parameters, recurse and find generators for them
          let targetArgs =
            factoryArgs.argumentTypes
            |> Array.map (fun t ->
              if t = typeof<AutoGenContext> then
                let ctx = AutoGenContext(
                  canRecurse = newRecursionState.CanRecurse,
                  currentRecursionDepth = newRecursionState.CurrentLevel,
                  collectionRange = AutoGenConfig.seqRange config,
                  auto = {
                    new IAutoGenerator with
                      member __.Generate<'x>() = autoInner<'x> config newRecursionState })
                box ctx
              else
                // Otherwise, generate a value for this type
                let ts = TypeShape.Create(t)
                ts.Accept { new ITypeVisitor<obj> with
                  member __.Visit<'b> () = autoInner<'b> config newRecursionState |> box
                })

          let resGen = factory factoryArgs.genericTypes targetArgs
          resGen |> unbox<Gen<'a>>

        | None ->
            match typeShape with

            | Shape.Unit -> wrap <| Gen.constant ()

            | Shape.Array s ->
                s.Element.Accept {
                  new ITypeVisitor<Gen<'a>> with
                  member __.Visit<'a> () =
                    if newRecursionState.CanRecurse then
                      gen {
                        let! lengths =
                          config
                          |> AutoGenConfig.seqRange
                          |> Gen.integral
                          |> List.replicate s.Rank
                          |> Gen.sequenceList
                        let elementCount = lengths |> List.fold (*) 1
                        let! data = autoInner<'a> config newRecursionState |> Gen.list (Range.singleton elementCount)
                        return MultidimensionalArray.createWithGivenEntries<'a> data lengths |> unbox
                      }
                    else
                      0
                      |> List.replicate s.Rank
                      |> MultidimensionalArray.createWithDefaultEntries<'a>
                      |> unbox
                      |> Gen.constant }

            | Shape.Tuple (:? ShapeTuple<'a> as shape) ->
                shape.Elements
                |> Seq.toList
                |> Gen.traverse memberSetterGenerator
                |> Gen.map (fun fs -> fs |> Seq.fold (|>) (shape.CreateUninitialized ()))

            | Shape.FSharpRecord (:? ShapeFSharpRecord<'a> as shape) ->
                shape.Fields
                |> Seq.toList
                |> Gen.traverse memberSetterGenerator
                |> Gen.map (fun fs -> fs |> Seq.fold (|>) (shape.CreateUninitialized ()))

            | Shape.FSharpUnion (:? ShapeFSharpUnion<'a> as shape) ->
                let cases =
                  shape.UnionCases
                  |> Array.map (fun uc ->
                     uc.Fields
                     |> Seq.toList
                     |> Gen.traverse memberSetterGenerator)
                gen {
                  let! caseIdx = Gen.integral <| Range.constant 0 (cases.Length - 1)
                  let! fs = cases[caseIdx]
                  return fs |> Seq.fold (|>) (shape.UnionCases[caseIdx].CreateUninitialized ())
                }

            | Shape.Enum _ ->
                let values = Enum.GetValues(typeof<'a>)
                gen {
                  let! index = Gen.integral <| Range.constant 0 (values.Length - 1)
                  return values.GetValue index |> unbox
                }

            | Shape.Collection s ->
                s.Accept {
                  new ICollectionVisitor<Gen<'a>> with
                  member _.Visit<'collection, 'element when 'collection :> System.Collections.Generic.ICollection<'element>> () =
                    match typeShape with
                    | Shape.Poco (:? ShapePoco<'a> as shape) ->
                      gen {
                        let! collectionCtor = genPoco shape
                        let! elements =
                          if newRecursionState.CanRecurse
                          then autoInner<'element> config newRecursionState |> Gen.list (AutoGenConfig.seqRange config)
                          else Gen.constant []
                        let collection = collectionCtor () |> unbox<System.Collections.Generic.ICollection<'element>>
                        for e in elements do collection.Add e
                        return collection |> unbox<'a>
                      }
                    | _ -> raise (AutoGenHelpers.unsupportedTypeException<'a>())
                  }

            | Shape.CliMutable (:? ShapeCliMutable<'a> as shape) ->
                let getDepth (sm: IShapeMember<_>) =
                  let rec loop (t: Type) depth =
                    if t = null
                    then depth
                    else loop t.BaseType (depth + 1)
                  loop sm.MemberInfo.DeclaringType 0
                shape.Properties
                |> Array.toList
                |> List.groupBy _.MemberInfo.Name
                |> List.map (snd >> function
                                    | [p] -> p
                                    | ps -> ps |> List.sortByDescending getDepth |> List.head)
                |> Gen.traverse memberSetterGenerator
                |> Gen.map (fun fs -> fs |> Seq.fold (|>) (shape.CreateUninitialized ()))

            | Shape.Poco (:? ShapePoco<'a> as shape) -> genPoco shape |> Gen.map (fun x -> x ())

            | _ -> raise (AutoGenHelpers.unsupportedTypeException<'a>())

    /// Automatically generates a value of the specified type using the default configuration.
    let auto<'a> = autoInner<'a> AutoGenConfig.defaults RecursionState.empty

    /// Automatically generates a value of the specified type using the provided configuration.
    let autoWith<'a> config = autoInner<'a> config RecursionState.empty
