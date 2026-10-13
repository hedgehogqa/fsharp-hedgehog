namespace Hedgehog.MSTest

open System.Reflection
open Hedgehog
open Hedgehog.FSharp

/// Represents the context for property-based testing, including configuration and test parameters.
type internal PropertyContext =
    { AutoGenConfig: IAutoGenConfig
      Tests: int<tests> option
      Shrinks: int<shrinks> option
      Size: Size option
      Seed: uint64 option
      Recheck: string option }

module internal PropertyContext =
    let defaults: PropertyContext =
        { AutoGenConfig = AutoGenConfig.defaults
          Tests = None
          Shrinks = None
          Size = None
          Seed = None
          Recheck = None }

    let private append (ctx: PropertyContext) (attr: IPropertyAttribute) : PropertyContext =
        let config =
            match attr.AutoGenConfig with
            | Some t ->
                AutoGenConfig.instantiate t attr.AutoGenConfigArgs
                |> AutoGenConfig.merge ctx.AutoGenConfig
            | None -> ctx.AutoGenConfig

        { ctx with
            AutoGenConfig = config
            Tests = attr.Tests |> Option.orElse ctx.Tests
            Shrinks = attr.Shrinks |> Option.orElse ctx.Shrinks
            Size = attr.Size |> Option.orElse ctx.Size
            Seed = attr.Seed |> Option.orElse ctx.Seed }

    let fromMethod (method: MethodInfo) =
        let propertyAttribute =
            method.GetCustomAttributes()
            |> Seq.cast<obj>
            |> Seq.filter (fun attr -> attr :? IPropertyAttribute)
            |> Seq.exactlyOne
            :?> IPropertyAttribute

        // MSTest runs a property inherited from a base class once per derived test class: the class it runs is the
        // reflected type of the method, while the declaring type is the base class. Type.getAllAttributes lists a class
        // before its base classes; reversed, the settings of a derived class are applied last and win.
        let classAttributes =
            method.ReflectedType |> Type.getAllAttributes<IPropertyAttribute> |> List.rev

        let context =
            [ propertyAttribute ] |> Seq.append classAttributes |> Seq.fold append defaults

        let recheckData =
            method.GetCustomAttributes(typeof<RecheckAttribute>)
            |> Seq.tryHead
            |> Option.map (fun x -> (x :?> RecheckAttribute).GetRecheckData)

        { context with Recheck = recheckData }
