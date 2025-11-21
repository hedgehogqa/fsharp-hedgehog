module Hedgehog.Autogen.Tests.GenTests

open System
open Hedgehog.AutoGen
open Xunit
open Swensen.Unquote
open Hedgehog
open TypeShape.Core

let checkWith tests = PropertyConfig.defaultConfig |> PropertyConfig.withTests tests |> Property.checkWith


type RecOption =
  {X: RecOption option}
  member this.Depth =
    match this.X with
    | None -> 0
    | Some x -> x.Depth + 1

[<Fact>]
let ``auto with recursive option members does not cause stack overflow using default settings`` () =
    Property.checkBool <| property {
        let! _ = Gen.auto<RecOption>
        return true
    }

[<Fact>]
let ``auto with recursive option members respects max recursion depth`` () =
    Property.check <| property {
        let! depth = Gen.int32 <| Range.exponential 0 5
        let! x = Gen.autoWith<RecOption> (AutoGenConfig.defaults |> AutoGenConfig.setRecursionDepth depth)
        x.Depth <=! depth
    }

[<Fact>]
let ``auto with recursive option members generates some values with max recursion depth`` () =
    checkWith 10<tests> <| property {
        let! depth = Gen.int32 <| Range.linear 1 5
        let! xs = Gen.autoWith<RecOption> (AutoGenConfig.defaults |> AutoGenConfig.setRecursionDepth depth)
                  |> (Gen.list (Range.singleton 100))
        test <@ xs |> List.exists (fun x -> x.Depth = depth) @>
    }


type RecArray =
  {X: RecArray array}
  member this.Depth =
    match this.X with
    | [||] -> 0
    | xs -> xs |> Array.map (fun x -> x.Depth + 1) |> Array.max

[<Fact>]
let ``auto with recursive array members does not cause stack overflow using default settings`` () =
    Property.checkBool <| property {
        let! _ = Gen.auto<RecArray>
        return true
    }

[<Fact>]
let ``auto with recursive array members respects max recursion depth`` () =
    Property.check <| property {
        let! depth = Gen.int32 <| Range.exponential 0 5
        let! x = Gen.autoWith<RecArray> (AutoGenConfig.defaults |> AutoGenConfig.setRecursionDepth depth |> AutoGenConfig.setSeqRange (Range.exponential 0 5))
        x.Depth <=! depth
    }

[<Fact>]
let ``auto with recursive array members generates some values with max recursion depth`` () =
    checkWith 10<tests> <| property {
        let! depth = Gen.int32 <| Range.linear 1 5
        let! xs = Gen.autoWith<RecArray> (AutoGenConfig.defaults |> AutoGenConfig.setRecursionDepth depth |> AutoGenConfig.setSeqRange (Range.exponential 1 5))
                  |> (Gen.list (Range.singleton 100))
        test <@ xs |> List.exists (fun x -> x.Depth = depth) @>
    }


type RecList =
  {X: RecList list}
  member this.Depth =
    match this.X with
    | [] -> 0
    | xs -> xs |> List.map (fun x -> x.Depth + 1) |> List.max

[<Fact>]
let ``auto with recursive list members does not cause stack overflow using default settings`` () =
    Property.checkBool <| property {
        let! _ = Gen.auto<RecList>
        return true
    }

[<Fact>]
let ``auto with recursive list members respects max recursion depth`` () =
    Property.check <| property {
        let! depth = Gen.int32 <| Range.exponential 0 5
        let! x = Gen.autoWith<RecList> (AutoGenConfig.defaults |> AutoGenConfig.setRecursionDepth depth |> AutoGenConfig.setSeqRange (Range.exponential 0 5))
        x.Depth <=! depth
    }

[<Fact>]
let ``auto with recursive list members generates some values with max recursion depth`` () =
    checkWith 10<tests> <| property {
        let! depth = Gen.int32 <| Range.linear 1 5
        let! xs = Gen.autoWith<RecList> (AutoGenConfig.defaults |> AutoGenConfig.setRecursionDepth depth |> AutoGenConfig.setSeqRange (Range.exponential 1 5))
                  |> (Gen.list (Range.singleton 100))
        test <@ xs |> List.exists (fun x -> x.Depth = depth) @>
    }


type RecResizeArray =
  {X: RecResizeArray ResizeArray}
  member this.Depth =
    match this.X with
    | x when x.Count = 0 -> 0
    | xs -> xs |> Seq.map (fun x -> x.Depth + 1) |> Seq.max

[<Fact>]
let ``auto with recursive ResizeArray members does not cause stack overflow using default settings`` () =
    Property.checkBool <| property {
        let! _ = Gen.auto<RecResizeArray>
        return true
    }

[<Fact>]
let ``auto with recursive ResizeArray members respects max recursion depth`` () =
    Property.check <| property {
        let! depth = Gen.int32 <| Range.exponential 0 5
        let! x = Gen.autoWith<RecResizeArray> (AutoGenConfig.defaults |> AutoGenConfig.setRecursionDepth depth |> AutoGenConfig.setSeqRange (Range.exponential 0 5))
        x.Depth <=! depth
    }

[<Fact>]
let ``auto with recursive ResizeArray members generates some values with max recursion depth`` () =
    checkWith 10<tests> <| property {
        let! depth = Gen.int32 <| Range.linear 1 5
        let! xs = Gen.autoWith<RecResizeArray> (AutoGenConfig.defaults |> AutoGenConfig.setRecursionDepth depth |> AutoGenConfig.setSeqRange (Range.exponential 1 5))
                  |> (Gen.list (Range.singleton 100))
        test <@ xs |> List.exists (fun x -> x.Depth = depth) @>
    }


type RecDictionary =
  {X: System.Collections.Generic.Dictionary<RecDictionary, RecDictionary>}
  member this.Depth =
    match this.X with
    | x when x.Count = 0 -> 0
    | xs -> xs |> Seq.collect (fun x -> seq {x.Key.Depth + 1; x.Value.Depth} ) |> Seq.max

[<Fact>]
let ``auto with recursive Dictionary members does not cause stack overflow using default settings`` () =
    Property.checkBool <| property {
        let! _ = Gen.auto<RecDictionary>
        return true
    }

[<Fact>]
let ``auto with recursive Dictionary members respects max recursion depth`` () =
    Property.check <| property {
        let! depth = Gen.int32 <| Range.exponential 0 5
        let! x = Gen.autoWith<RecDictionary> (AutoGenConfig.defaults |> AutoGenConfig.setRecursionDepth depth |> AutoGenConfig.setSeqRange (Range.exponential 0 5))
        x.Depth <=! depth
    }

[<Fact>]
let ``auto with recursive Dictionary members generates some values with max recursion depth`` () =
    checkWith 10<tests> <| property {
        let! depth = Gen.int32 <| Range.linear 1 5
        let! xs = Gen.autoWith<RecDictionary> (AutoGenConfig.defaults |> AutoGenConfig.setRecursionDepth depth |> AutoGenConfig.setSeqRange (Range.exponential 1 5))
                  |> (Gen.list (Range.singleton 100))
        test <@ xs |> List.exists (fun x -> x.Depth = depth) @>
    }


type RecSet =
  {X: Set<RecSet>}
  member this.Depth =
    if this.X.IsEmpty then 0
    else
      this.X |> Seq.map (fun x -> x.Depth + 1) |> Seq.max

[<Fact>]
let ``auto with recursive set members does not cause stack overflow using default settings`` () =
    Property.checkBool <| property {
        let! _ = Gen.auto<RecSet>
        return true
    }

[<Fact>]
let ``auto with recursive set members respects max recursion depth`` () =
    Property.check <| property {
        let! depth = Gen.int32 <| Range.exponential 0 5
        let! x = Gen.autoWith<RecSet> (AutoGenConfig.defaults |> AutoGenConfig.setRecursionDepth depth |> AutoGenConfig.setSeqRange (Range.exponential 0 5))
        x.Depth <=! depth
    }

[<Fact>]
let ``auto with recursive set members generates some values with max recursion depth`` () =
    checkWith 10<tests> <| property {
        let! depth = Gen.int32 <| Range.linear 1 5
        let! xs = Gen.autoWith<RecSet> (AutoGenConfig.defaults |> AutoGenConfig.setRecursionDepth depth |> AutoGenConfig.setSeqRange (Range.exponential 1 5))
                  |> (Gen.list (Range.singleton 100))
        test <@ xs |> List.exists (fun x -> x.Depth = depth) @>
    }


type RecMap =
  {X: Map<RecMap, RecMap>}
  member this.Depth =
    if this.X.IsEmpty then 0
    else
      this.X |> Map.toSeq |> Seq.map (fun (k, v)  -> max (k.Depth + 1) (v.Depth + 1)) |> Seq.max

[<Fact>]
let ``auto with recursive map members does not cause stack overflow using default settings`` () =
    Property.checkBool <| property {
        let! _ = Gen.auto<RecMap>
        return true
    }

[<Fact>]
let ``auto with recursive map members respects max recursion depth`` () =
    Property.check <| property {
        let! depth = Gen.int32 <| Range.exponential 0 5
        let! x = Gen.autoWith<RecMap> (AutoGenConfig.defaults |> AutoGenConfig.setRecursionDepth depth |> AutoGenConfig.setSeqRange (Range.exponential 0 5))
        x.Depth <=! depth
    }

[<Fact>]
let ``auto with recursive map members generates some values with max recursion depth`` () =
    checkWith 10<tests> <| property {
        let! depth = Gen.int32 <| Range.linear 1 5
        let! xs = Gen.autoWith<RecMap> (AutoGenConfig.defaults |> AutoGenConfig.setRecursionDepth depth |> AutoGenConfig.setSeqRange (Range.exponential 1 5))
                  |> (Gen.list (Range.singleton 100))
        test <@ xs |> List.exists (fun x -> x.Depth = depth) @>
    }


type MutuallyRecursive1 =
  {X: MutuallyRecursive2 option}
  member this.Depth =
    match this.X with
    | None -> 0
    | Some {X = []} -> 0
    | Some {X = mc1s} ->
        mc1s
        |> List.map (fun mc1 -> mc1.Depth + 1)
        |> List.max

and MutuallyRecursive2 =
  {X: MutuallyRecursive1 list}
  member this.Depth =
    if this.X.IsEmpty then 0
    else
      let depths =
        this.X
        |> List.choose (fun mc1 -> mc1.X)
        |> List.map (fun mc2 -> mc2.Depth + 1)
      if depths.IsEmpty then 1 // Having items in X means we recursed at least once
      else List.max depths

[<Fact>]
let ``auto with mutually recursive types does not cause stack overflow using default settings`` () =
    Property.checkBool <| property {
        let! _ = Gen.auto<MutuallyRecursive1>
        let! _ = Gen.auto<MutuallyRecursive2>
        return true
    }

[<Fact>]
let ``auto with mutually recursive types respects max recursion depth`` () =
    Property.check <| property {
        let! depth = Gen.int32 <| Range.exponential 0 5
        let! x1 = Gen.autoWith<MutuallyRecursive1> (AutoGenConfig.defaults |> AutoGenConfig.setRecursionDepth depth |> AutoGenConfig.setSeqRange (Range.exponential 0 5))
        let! x2 = Gen.autoWith<MutuallyRecursive2> (AutoGenConfig.defaults |> AutoGenConfig.setRecursionDepth depth |> AutoGenConfig.setSeqRange (Range.exponential 0 5))
        x1.Depth <=! depth
        x2.Depth <=! depth
    }

[<Fact>]
let ``auto with mutually recursive types generates some values with max recursion depth`` () =
    checkWith 10<tests> <| property {
        let! depth = Gen.int32 <| Range.linear 1 5
        let config = AutoGenConfig.defaults |> AutoGenConfig.setRecursionDepth depth |> AutoGenConfig.setSeqRange (Range.exponential 1 5)
        let! xs1 = Gen.autoWith<MutuallyRecursive1> config
                  |> (Gen.list (Range.singleton 10))
        let! xs2 = Gen.autoWith<MutuallyRecursive2> config
                  |> (Gen.list (Range.singleton 10))
        test <@ xs1 |> List.exists (fun x -> x.Depth = depth) @>
        test <@ xs2 |> List.exists (fun x -> x.Depth = depth) @>
    }

[<Fact>]
let ``auto with UInt64 generates UInt64`` () =
    Property.checkBool <| property {
        let! _ = Gen.auto<uint64>
        return true
    }

[<Fact>]
let ``auto can generate valid URIs`` () =
    checkWith 1000<tests> <| property {
        let! uri = Gen.auto<Uri>
        ignore uri
    }


type Enum =
  | A = 1
  | B = 2


[<Fact>]
let ``auto can generate enums`` () =
    checkWith 1<tests> <| property {
        let! enums =
          Gen.auto<Enum>
          |> Gen.list (Range.singleton 1000)
        test <@ enums |> List.contains Enum.A @>
        test <@ enums |> List.contains Enum.B @>
        test <@ enums |> List.forall (fun e -> e = Enum.A || e = Enum.B) @>
    }


[<Fact>]
let ``auto can generate byte`` () =
    Property.check <| property {
        let! _ = Gen.auto<byte>
        ()
    }


[<Fact>]
let ``auto can generate int16`` () =
    Property.check <| property {
        let! _ = Gen.auto<int16>
        ()
    }


[<Fact>]
let ``auto can generate uint16`` () =
    Property.check <| property {
        let! _ = Gen.auto<uint16>
        ()
    }


[<Fact>]
let ``auto can generate int`` () =
    Property.check <| property {
        let! _ = Gen.auto<int>
        ()
    }


[<Fact>]
let ``auto can generate uint32`` () =
    Property.check <| property {
        let! _ = Gen.auto<uint32>
        ()
    }


[<Fact>]
let ``auto can generate int64`` () =
    Property.check <| property {
        let! _ = Gen.auto<int64>
        ()
    }


[<Fact>]
let ``auto can generate uint64`` () =
    Property.check <| property {
        let! _ = Gen.auto<uint64>
        ()
    }


[<Fact>]
let ``auto can generate single`` () =
    Property.check <| property {
        let! _ = Gen.auto<single>
        ()
    }


[<Fact>]
let ``auto can generate float`` () =
    Property.check <| property {
        let! _ = Gen.auto<float>
        ()
    }


[<Fact>]
let ``auto can generate decimal`` () =
    Property.check <| property {
        let! _ = Gen.auto<decimal>
        ()
    }


[<Fact>]
let ``auto can generate bool`` () =
    Property.check <| property {
        let! _ = Gen.auto<bool>
        ()
    }


[<Fact>]
let ``auto can generate GUID`` () =
    Property.check <| property {
        let! _ = Gen.auto<Guid>
        ()
    }


[<Fact>]
let ``auto can generate char`` () =
    Property.check <| property {
        let! _ = Gen.auto<char>
        ()
    }


[<Fact>]
let ``auto can generate string`` () =
    Property.check <| property {
        let! _ = Gen.auto<string>
        ()
    }


[<Fact>]
let ``auto can generate DateTime`` () =
    Property.check <| property {
        let! _ = Gen.auto<DateTime>
        ()
    }


[<Fact>]
let ``auto can generate DateTimeOffset`` () =
    Property.check <| property {
        let! _ = Gen.auto<DateTimeOffset>
        ()
    }


type TypeWithoutAccessibleCtor private (state: int) =
    static member CustomConstructor state = TypeWithoutAccessibleCtor state
    member this.State = state


[<Fact>]
let ``auto can generate custom classes with no suitable constructors using overrides`` () =
  let myTypeGen = gen {
    let! state = Gen.int32 <| Range.exponentialBounded ()
    return TypeWithoutAccessibleCtor.CustomConstructor state
  }
  checkWith 1<tests> <| property {
      let config = AutoGenConfig.defaults |> AutoGenConfig.addGenerator myTypeGen
      let! _ = Gen.autoWith<TypeWithoutAccessibleCtor> config
      ()
  }


[<Fact>]
let ``auto uses specified overrides`` () =
  let constantIntGen = Gen.constant 1
  Property.check <| property {
      let config = AutoGenConfig.defaults |> AutoGenConfig.addGenerator constantIntGen
      let! i = Gen.autoWith<int> config
      test <@ i = 1 @>
  }


module ShrinkTests =

  // We need to hit an error case in order to test shrinking. That error case may be uncommon.
  // We therefore run 1 million tests to increase the probability of hitting an error case.
  let render property =
    let config =
      PropertyConfig.defaultConfig
      |> PropertyConfig.withTests 1_000_000<tests>
    Property.reportWith config property
    |> Report.render

  type MyRecord =
    { String: string
      Int: int }

  [<Fact>]
  let ``auto of record shrinks correctly`` () =
    let property = property {
      let! value = Gen.auto<MyRecord>
      test <@ not (value.String.Contains('b')) @>
    }
    let rendered = render property
    test <@ rendered.Contains "{ String = \"b\"\n  Int = 0 }" @>


  type MyCliMutable() =
    let mutable myString = ""
    let mutable myInt = 0
    member _.String
      with get () = myString
      and set value = myString <- value
    member _.Int
      with get () = myInt
      and set value = myInt <- value
    override _.ToString() =
      "String = " + myString + "; Int = " + myInt.ToString()

  [<Fact>]
  let ``auto of CLI mutable shrinks correctly`` () =
    let property = property {
      let! value = Gen.auto<MyCliMutable>
      test <@ not (value.String.Contains('b')) @>
    }
    let rendered = render property
    test <@ rendered.Contains "String = b; Int = 0" @>

  [<RequireQualifiedAccess>]
  type MyDu =
    | Case1 of String * int

  [<Fact>]
  let ``auto of discriminated union shrinks correctly`` () =
    let property = property {
      let! MyDu.Case1(s, _) = Gen.auto<MyDu>
      test <@ not (s.Contains('b')) @>
    }
    let rendered = render property
    test <@ rendered.Contains "Case1 (\"b\", 0)" @>


  [<Fact>]
  let ``auto of tuple shrinks correctly`` () =
    let property = property {
      let! s, _ = Gen.auto<string * int>
      test <@ not (s.Contains('b')) @>
    }
    let rendered = render property
    test <@ rendered.Contains "(\"b\", 0)" @>

  [<Fact>]
  let ``shuffleCase shrinks correctly`` () =
    let property = property {
      let! value = Gen.shuffleCase "abcdefg"
      test <@ not (value.StartsWith "A") @>
    }
    let rendered = render property
    test <@ rendered.Contains "\"Abcdefg\"" @>

  [<Fact>]
  let ``shuffle shrinks correctly`` () =
    let property = property {
      let n = 10
      let nMinus1 = n - 1
      let! value =
        ()
        |> Seq.replicate n
        |> Seq.mapi (fun i _ -> i)
        |> Seq.toList
        |> Gen.shuffle
      test <@ nMinus1 <> value.Head @>
    }
    let rendered = render property
    test <@ rendered.Contains "[9; 0; 1; 2; 3; 4; 5; 6; 7; 8]" @>

  [<Fact>]
  let ``one-dimentional array shrinks correctly when empty allowed`` () =
    let property = property {
      let! array = Gen.auto<int []>
      test <@ array.Length = 0 @>
    }
    let rendered = render property
    test <@ rendered.Contains "[|0|]" @>

  [<Fact>]
  let ``one-dimentional array shrinks correctly when empty disallowed`` () =
    let property = property {
      let! array =
        AutoGenConfig.defaults
        |> AutoGenConfig.setSeqRange (Range.constant 2 5)
        |> Gen.autoWith<int []>
      test <@ 1 <> array[0] @>
    }
    let rendered = render property
    test <@ rendered.Contains "[|1; 0" @>

  [<Fact>]
  let ``two-dimentional array shrinks correctly when empty allowed`` () =
    let property = property {
      let! array = Gen.auto<int [,]>
      test <@ array.Length = 0 @>
    }
    let rendered = render property
    test <@ rendered.Contains "[[0]]" @>

  [<Fact>]
  let ``two-dimentional array shrinks correctly when empty disallowed`` () =
    let property = property {
      let! array =
        AutoGenConfig.defaults
        |> AutoGenConfig.setSeqRange (Range.constant 1 5)
        |> Gen.autoWith<int [,]>
      test <@ 1 <> array[0,0] @>
    }
    let rendered = render property
    test <@ rendered.Contains "[[1; 0" ||
            rendered.Contains "[[1]\n [0]" ||
            rendered.Contains "[[1]]"@>

  [<Fact>]
  let ``auto of ResizeArray shrinks correctly`` () =
    let property = property {
      let! resizeArray =
        AutoGenConfig.defaults
        |> AutoGenConfig.setSeqRange (Range.constant 4 4)
        |> Gen.autoWith<ResizeArray<int>>
      test <@ 1 <> resizeArray[0] @>
    }
    let rendered = render property
    test <@ rendered.Contains "[1; 0; 0; 0]" @>

[<Fact>]
let ``MultidimensionalArray.createWithGivenEntries works for 2x2`` () =
  let data = [ 0; 1; 2; 3 ]
  let lengths = [ 2; 2 ]

  let array : int [,] =
    MultidimensionalArray.createWithGivenEntries data lengths
    |> unbox

  <@
    array[0, 0] = 0
    && array[0, 1] = 1
    && array[1, 0] = 2
    && array[1, 1] = 3
  @>
  |> test

type CtorThrows(__: Guid) =
  do failwith ""
[<Fact>]
let ``Shape.Poco with throwing Ctor, upon failure, includes arg in exception`` () =
  let guid = Guid.NewGuid()
  raisesWith<ArgumentException>
    <@
      AutoGenConfig.defaults
      |> AutoGenConfig.addGenerator (Gen.constant guid)
      |> Gen.autoWith<CtorThrows>
      |> Gen.sample 0 1
      |> Seq.exactlyOne
    @>
    (fun e -> <@ guid |> string |> e.Message.Contains @>)

type PropertyThrows() =
  member _.ReadWriteProperty with get () = Guid.Empty
  member _.ReadWriteProperty with set (__: Guid) = failwith ""
[<Fact>]
let ``Shape.CliMutable with throwing Property, upon failure, includes arg in exception`` () =
  let guid = Guid.NewGuid()
  raisesWith<ArgumentException>
    <@
      AutoGenConfig.defaults
      |> AutoGenConfig.addGenerator (Gen.constant guid)
      |> Gen.autoWith<PropertyThrows>
      |> Gen.sample 0 1
      |> Seq.exactlyOne
    @>
    (fun e -> <@ guid |> string |> e.Message.Contains @>)

[<Fact>]
let ``auto can generate Nullable`` () =
  Property.check <| property {
    let! _ = Gen.auto<Nullable<int>>
    ()
  }

type RecordWithNullables =
  { field : Nullable<float>
    colour : Nullable<DateTimeKind>
  }

[<Fact>]
let ``auto can generate record with Nullable fields`` () =
  Property.check <| property {
    let! _ = Gen.auto<RecordWithNullables>
    ()
  }

[<Fact>]
let ``auto can generate Nullable bool without recursion`` () =
  Property.check <| property {
    let! _ =
      AutoGenConfig.defaults
      |> AutoGenConfig.setRecursionDepth 0
      |> Gen.autoWith<Nullable<bool>>
    ()
  }

[<Fact>]
let ``auto can generate seq`` () =
  Property.checkBool <| property {
    let! expectedLen = Gen.int32 (Range.linear 0 105)
    let! xs =
      AutoGenConfig.defaults
      |> AutoGenConfig.setSeqRange (Range.singleton expectedLen)
      |> Gen.autoWith<seq<int>>

    return Seq.length xs = expectedLen
  }


type Animal() =
  let mutable value = obj()
  let mutable count = 0
  member _.AnimalCount with get() = count
  member _.Value
    with get() : obj = value
    and set(v: obj) =
      count <- count + 1
      value <- v

type Dog() =
  inherit Animal()
  let mutable count = 0
  member _.DogCount with get() = count
  member _.Value
    with get() : string = base.Value :?> string
    and set(v: string) =
      count <- count + 1
      base.Value <- v

type Poodle() =
  inherit Dog()

[<Fact>]
let ``Type Dog is Shape_CliMutable`` () =
  let isDogCliMutable =
    match TypeShape.Create<Dog> () with
    | Shape.CliMutable _ -> true
    | _ -> false
  test <@ isDogCliMutable @>

[<Fact>]
let ``Type Poodle is Shape_CliMutable`` () =
  let isPoodleCliMutable =
    match TypeShape.Create<Poodle> () with
    | Shape.CliMutable _ -> true
    | _ -> false
  test <@ isPoodleCliMutable @>

[<Fact>]
let ``auto can generate Shape_CliMutable Dog which has a shadowed property that strengthens the type`` () =
  Property.check <| property {
    let actual =
      Gen.auto<Dog>
      |> Gen.sample 0 1
      |> Seq.head
    actual.Value // Does not throw an exception
    |> ignore
  }

[<Fact>]
let ``auto can generate Shape_CliMutable Poodle which inherits from Dog which has a shadowed property that strengthens the type`` () =
  Property.check <| property {
    let actual =
      Gen.auto<Poodle>
      |> Gen.sample 0 1
      |> Seq.head
    actual.Value // Does not throw an exception
    |> ignore
  }

[<Fact>]
let ``auto does not set shadowed property of Shape_CliMutable`` () =
  Property.check <| property {
    let actual =
      Gen.auto<Dog>
      |> Gen.sample 0 1
      |> Seq.head
    test <@ actual.AnimalCount = actual.DogCount @>
  }
