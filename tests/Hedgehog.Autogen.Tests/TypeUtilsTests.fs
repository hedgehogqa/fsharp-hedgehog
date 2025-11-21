module Hedgehog.Autogen.Tests.TypeUtilsTests

open System
open System.Reflection
open Hedgehog.AutoGen
open Xunit

type Id<'a> = Id of Guid
type Either<'a, 'b> = Left of 'a | Right of 'b
type Rel<'a, 'b> = Rel of 'a * 'b

type GenericTestContainer =
  static member Id<'a>() : Id<'a> = Id (Guid.NewGuid())

  static member Left<'a, 'b>(a: 'a) : Either<'a, 'b> = Left a

  static member Right<'b>(b: 'b) : Either<string, 'b> = Right b

  static member RelStr<'b>(a: string, b: 'b) : Rel<string, 'b> = Rel (a, b)

let genericTypes =
    typeof<GenericTestContainer>.GetMethods(BindingFlags.Static ||| BindingFlags.Public)
    |> Seq.filter _.ReturnType.IsGenericType
    |> Seq.map _.ReturnType
    |> Seq.sortBy (fun t ->
        if t.IsGenericType then
            t.GetGenericArguments()
            |> Seq.filter _.IsGenericParameter
            |> Seq.length
        else
            Int32.MaxValue
    )
    |> Seq.toArray

let fullTypeName (typ: Type) =
  if typ.IsGenericType then
    let genericDef = typ.GetGenericTypeDefinition()
    let genericArgs = typ.GetGenericArguments()
    let argsString =
      genericArgs
      |> Seq.map (fun t -> if t.IsGenericParameter then t.Name else t.FullName)
      |> String.concat ","

    $"%s{genericDef.FullName}[%s{argsString}]"
  else typ.FullName


[<Fact>]
let ``Generic satisfies value type - Either<'a, 'b> to Either<int, string>`` () =
    let result = genericTypes |> Array.find (TypeUtils.satisfies typeof<Either<int, string>>)
    Assert.Equal("Hedgehog.Autogen.Tests.TypeUtilsTests+Either`2[a,b]", fullTypeName result)

[<Fact>]
let ``Generic satisfies value type - Either<string, 'b> to Either<int, string>`` () =
    let result = genericTypes |> Array.find (TypeUtils.satisfies typeof<Either<string, string>>)
    Assert.Equal("Hedgehog.Autogen.Tests.TypeUtilsTests+Either`2[System.String,b]", fullTypeName result)

[<Fact>]
let ``Generic satisfies value type - Id<'a> to Id<int>`` () =
    let result = genericTypes |> Array.find (TypeUtils.satisfies typeof<Id<int>>)
    Assert.Equal("Hedgehog.Autogen.Tests.TypeUtilsTests+Id`1[a]", fullTypeName result)

[<Fact>]
let ``Generic satisfies value type - Id<'a> to Id<Guid>`` () =
    let result = genericTypes |> Array.find (TypeUtils.satisfies typeof<Id<Guid>>)
    Assert.Equal("Hedgehog.Autogen.Tests.TypeUtilsTests+Id`1[a]", fullTypeName result)

[<Fact>]
let ``Generic satisfies value type - Rel<string, 'b> to Rel<string, Guid>`` () =
    let result = genericTypes |> Array.find (TypeUtils.satisfies typeof<Rel<string, Guid>>)
    Assert.Equal("Hedgehog.Autogen.Tests.TypeUtilsTests+Rel`2[System.String,b]", fullTypeName result)

[<Fact>]
let ``Generic does not satisfy value type - Rel<string, 'b> to Rel<int, Guid>`` () =
    let result = genericTypes |> Array.tryFind (TypeUtils.satisfies typeof<Rel<int, Guid>>)
    Assert.Equal(None, result)
