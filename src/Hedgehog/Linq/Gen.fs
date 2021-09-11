namespace Hedgehog.Linq

#if !FABLE_COMPILER

open System
open System.Runtime.CompilerServices
open Hedgehog

type Gen private () =

    static member FromValue (value : 'T) : Gen<'T> =
        Gen.constant value

    static member FromRandom (random : Random<Tree<'T>>) : Gen<'T> =
        Gen.ofRandom random

    static member Delay (func : Func<Gen<'T>>) : Gen<'T> =
        Gen.delay func.Invoke

    static member Create (shrink : Func<'T, seq<'T>>, random : Random<'T>) : Gen<'T> =
        Gen.create shrink.Invoke random

    static member Sized (scaler : Func<Size, Gen<'T>>) : Gen<'T> =
        Gen.sized scaler.Invoke

    static member Integral (range : Range<byte>) : Gen<byte> =
        Gen.integral range

    static member Integral (range : Range<sbyte>) : Gen<sbyte> =
        Gen.integral range

    static member Integral (range : Range<int16>) : Gen<int16> =
        Gen.integral range

    static member Integral (range : Range<uint16>) : Gen<uint16> =
        Gen.integral range

    static member Integral (range : Range<int32>) : Gen<int32> =
        Gen.integral range

    static member Integral (range : Range<uint32>) : Gen<uint32> =
        Gen.integral range

    static member Integral (range : Range<int64>) : Gen<int64> =
        Gen.integral range

    static member Integral (range : Range<uint64>) : Gen<uint64> =
        Gen.integral range

    static member Integral (range : Range<double>) : Gen<double> =
        Gen.integral range

    static member Integral (range : Range<decimal>) : Gen<decimal> =
        Gen.integral range

    static member Item (sequence : seq<'T>) : Gen<'T> =
        Gen.item sequence

    static member Frequency (gens : seq<int * Gen<'T>>) : Gen<'T> =
        Gen.frequency gens

    static member Choice (gens : seq<Gen<'T>>) : Gen<'T> =
        Gen.choice gens

    static member ChoiceRecursive (nonrecs : seq<Gen<'T>>, recs : seq<Gen<'T>>) : Gen<'T> =
        Gen.choiceRec nonrecs recs

    static member Char (lo : char, hi : char) : Gen<char> =
        Gen.char lo hi

    static member UnicodeAll : Gen<char> =
        Gen.unicodeAll

    static member Digit : Gen<char> =
        Gen.digit

    static member Lower : Gen<char> =
        Gen.lower

    static member Upper : Gen<char> =
        Gen.upper

    static member Ascii : Gen<char> =
        Gen.ascii

    static member Latin1 : Gen<char> =
        Gen.latin1

    static member Unicode : Gen<char> =
        Gen.unicode

    static member Alpha : Gen<char> =
        Gen.alpha

    static member AlphaNumeric : Gen<char> =
        Gen.alphaNum

    static member Bool : Gen<bool> =
        Gen.bool

    static member SByte (range : Range<sbyte>) : Gen<sbyte> =
        Gen.sbyte range

    static member Byte (range : Range<byte>) : Gen<byte> =
        Gen.byte range

    static member Int16 (range : Range<int16>) : Gen<int16> =
        Gen.int16 range

    static member UInt16 (range : Range<uint16>) : Gen<uint16> =
        Gen.uint16 range

    static member Int32 (range : Range<int32>) : Gen<int32> =
        Gen.int32 range

    static member UInt32 (range : Range<uint32>) : Gen<uint32> =
        Gen.uint32 range

    static member Int64 (range : Range<int64>) : Gen<int64> =
        Gen.int64 range

    static member UInt64 (range : Range<uint64>) : Gen<uint64> =
        Gen.uint64 range

    static member Single (range : Range<single>) : Gen<single> =
        Gen.single range

    static member Double (range : Range<double>) : Gen<double> =
        Gen.double range

    static member Decimal (range : Range<decimal>) : Gen<decimal> =
        Gen.decimal range

    static member Guid : Gen<Guid> =
        Gen.guid

    static member DateTime (range : Range<DateTime>) : Gen<DateTime> =
        Gen.dateTime range

    static member DateTimeOffset (range : Range<DateTimeOffset>) : Gen<DateTimeOffset> =
        Gen.dateTimeOffset range

[<Extension>]
[<AbstractClass; Sealed>]
type GenExtensions private () =

    [<Extension>]
    static member Apply (genFunc : Gen<Func<'T, 'TResult>>, genArg : Gen<'T>) : Gen<'TResult> =
        Gen.apply genArg (genFunc |> Gen.map (fun f -> f.Invoke))

    [<Extension>]
    static member Array (gen : Gen<'T>, range : Range<int>) : Gen<'T []> =
        Gen.array range gen

    [<Extension>]
    static member Enumerable (gen : Gen<'T>, range : Range<int>) : Gen<seq<'T>> =
        Gen.seq range gen

    [<Extension>]
    static member GenerateTree (gen : Gen<'T>) : Tree<'T> =
        Gen.generateTree gen

    [<Extension>]
    static member List (gen : Gen<'T>, range : Range<int>) : Gen<ResizeArray<'T>> =
        Gen.list range gen
        |> Gen.map ResizeArray

    [<Extension>]
    static member NoShrink (gen : Gen<'T>) : Gen<'T> =
        Gen.noShrink gen

    [<Extension>]
    static member NullReference (gen : Gen<'T>) : Gen<'T> =
        Gen.option gen |> Gen.map (Option.defaultValue null)

    [<Extension>]
    static member NullValue (gen : Gen<'T>) : Gen<Nullable<'T>> =
        Gen.option gen |> Gen.map (Option.defaultWith Nullable << Option.map Nullable)

    [<Extension>]
    static member RenderSample (gen : Gen<'T>) : string =
        Gen.renderSample gen

    [<Extension>]
    static member Resize (gen : Gen<'T>, size : Size) : Gen<'T> =
        Gen.resize size gen

    [<Extension>]
    static member Sample (gen : Gen<'T>, size : Size, count : int) : ResizeArray<'T> =
        Gen.sample size count gen
        |> ResizeArray

    [<Extension>]
    static member SampleTree (gen : Gen<'T>, size : Size, count : int) : ResizeArray<Tree<'T>> =
        Gen.sampleTree size count gen
        |> ResizeArray

    [<Extension>]
    static member Scale (gen : Gen<'T>, scaler : Func<int, int>) : Gen<'T> =
        Gen.scale scaler.Invoke gen

    [<Extension>]
    static member SelectMany (gen : Gen<'T>, binder : Func<'T, Gen<'U>>) : Gen<'U> =
        Gen.bind binder.Invoke gen

    [<Extension>]
    static member SelectMany (gen : Gen<'T>, binder : Func<'T, Gen<'TCollection>>, projection : Func<'T, 'TCollection, 'TResult>) : Gen<'TResult> =
        GenBuilder.gen {
            let! a = gen
            let! b = binder.Invoke a
            return projection.Invoke (a, b)
        }

    [<Extension>]
    static member SelectRandom (gen : Gen<'T>, binder : Func<Random<Tree<'T>>, Random<Tree<'TResult>>>) : Gen<'TResult> =
        Gen.mapRandom binder.Invoke gen

    [<Extension>]
    static member SelectTree (gen : Gen<'T>, binder : Func<Tree<'T>, Tree<'TResult>>) : Gen<'TResult> =
        Gen.mapTree binder.Invoke gen

    [<Extension>]
    static member Select (gen : Gen<'T>, mapper : Func<'T, 'TResult>) : Gen<'TResult> =
        Gen.map mapper.Invoke gen

    [<Extension>]
    static member Select (genA : Gen<'T>, mapper : Func<'T, 'U, 'TResult>, genB : Gen<'U>) : Gen<'TResult> =
        Gen.map2 (fun a b -> mapper.Invoke (a, b))
            genA
            genB

    [<Extension>]
    static member Select (genA : Gen<'T>, mapper : Func<'T, 'U, 'V, 'TResult>, genB : Gen<'U>, genC : Gen<'V>) : Gen<'TResult> =
        Gen.map3 (fun a b c -> mapper.Invoke (a, b, c))
            genA
            genB
            genC

    [<Extension>]
    static member Select (genA : Gen<'T>, mapper : Func<'T, 'U, 'V, 'W, 'TResult>, genB : Gen<'U>, genC : Gen<'V>, genD : Gen<'W>) : Gen<'TResult> =
        Gen.map4 (fun a b c d -> mapper.Invoke (a, b, c, d))
            genA
            genB
            genC
            genD

    [<Extension>]
    static member Shrink (gen : Gen<'T>, shrinker : Func<'T, ResizeArray<'T>>) : Gen<'T> =
        Gen.shrink (shrinker.Invoke >> Seq.toList) gen

    [<Extension>]
    static member ShrinkLazy (gen : Gen<'T>, shrinker : Func<'T, seq<'T>>) : Gen<'T> =
        Gen.shrinkLazy shrinker.Invoke gen

    [<Extension>]
    static member Some (gen : Gen<Option<'T>>) : Gen<'T> =
        Gen.some gen

    [<Extension>]
    static member String (gen : Gen<char>, range : Range<int>) : Gen<string> =
        Gen.string range gen

    [<Extension>]
    static member ToGen (random : Random<Tree<'T>>) : Gen<'T> =
        Gen.ofRandom random

    [<Extension>]
    static member ToRandom (gen : Gen<'T>) : Random<Tree<'T>> =
        Gen.toRandom gen

    [<Extension>]
    static member TryFinally (gen : Gen<'T>, after : Action) : Gen<'T> =
        Gen.tryFinally after.Invoke gen

    [<Extension>]
    static member TryWith (gen : Gen<'T>, after : Func<exn, Gen<'T>>) : Gen<'T> =
        Gen.tryWith after.Invoke gen

    [<Extension>]
    static member Tuple2 (gen : Gen<'T>) : Gen<'T * 'T> =
        Gen.tuple gen

    [<Extension>]
    static member Tuple3 (gen : Gen<'T>) : Gen<'T * 'T * 'T> =
        Gen.tuple3 gen

    [<Extension>]
    static member Tuple4 (gen : Gen<'T>) : Gen<'T * 'T * 'T * 'T> =
        Gen.tuple4 gen

    [<Extension>]
    static member Where (gen : Gen<'T>, predicate : Func<'T, bool>) : Gen<'T> =
        Gen.filter predicate.Invoke gen

    [<Extension>]
    static member Zip (genA : Gen<'T>, genB : Gen<'U>) : Gen<'T * 'U> =
        Gen.zip genA genB

    [<Extension>]
    static member Zip (genA : Gen<'T>, genB : Gen<'U>, genC : Gen<'V>) : Gen<'T * 'U * 'V> =
        Gen.zip3 genA genB genC

    [<Extension>]
    static member Zip (genA : Gen<'T>, genB : Gen<'U>, genC : Gen<'V>, genD : Gen<'W>) : Gen<'T * 'U * 'V * 'W> =
        Gen.zip4 genA genB genC genD

#endif
