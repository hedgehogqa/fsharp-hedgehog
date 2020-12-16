namespace Hedgehog.CSharp

#if !FABLE_COMPILER

open System
open System.Runtime.CompilerServices
open Hedgehog

[<Extension>]
type Gen =

    static member FromValue (value : 'T) : Gen<'T> =
        Gen.constant(value)

    static member FromRandom (random : Random<Tree<'T>>) : Gen<'T> =
        Gen.ofRandom(random)

    static member Delay (func : Func<_>) : Gen<'T> =
        Gen.delay func.Invoke

    static member Create (shrink : 'T -> seq<'T>) (random : Random<'T>) : Gen<'T> =
        Gen.create shrink random

    static member Sized (scaler : Func<Size, Gen<'T>>) : Gen<'T> =
        Gen.sized scaler.Invoke

    static member IntegralByte (range : Range<byte>) : Gen<byte> =
        Gen.integral range

    static member IntegralSByte (range : Range<sbyte>) : Gen<sbyte> =
        Gen.integral range

    static member IntegralInt16 (range : Range<int16>) : Gen<int16> =
        Gen.integral range

    static member IntegralUInt16 (range : Range<uint16>) : Gen<uint16> =
        Gen.integral range

    static member IntegralInt32 (range : Range<int32>) : Gen<int32> =
        Gen.integral range

    static member IntegralUInt32 (range : Range<uint32>) : Gen<uint32> =
        Gen.integral range

    static member IntegralInt64 (range : Range<int64>) : Gen<int64> =
        Gen.integral range

    static member IntegralUInt64 (range : Range<uint64>) : Gen<uint64> =
        Gen.integral range

    static member IntegralDouble (range : Range<double>) : Gen<double> =
        Gen.integral range

    static member IntegralSingle (range : Range<float>) : Gen<float> =
        Gen.integral range

    static member IntegralDecimal (range : Range<decimal>) : Gen<decimal> =
        Gen.integral range

    static member Item (sequence : seq<'T>) : Gen<'T> =
        Gen.item sequence

    static member Frequency (gens : seq<int * Gen<'T>>) : Gen<'T> =
        Gen.frequency gens

    static member Choice (gens : seq<Gen<'T>>) : Gen<'T> =
        Gen.choice gens

    static member ChoiceRecursive (nonrecs : seq<Gen<'T>>) (recs : seq<Gen<'T>>) : Gen<'T> =
        Gen.choiceRec nonrecs recs

    static member Char (lo : char) (hi : char) : Gen<char> =
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

    static member Byte (range : Range<byte>) : Gen<byte> =
        Gen.byte range

    static member SByte (range : Range<sbyte>) : Gen<sbyte> =
        Gen.sbyte range

    static member Int16 (range : Range<int16>) : Gen<int16> =
        Gen.int16 range

    static member UInt16 (range : Range<uint16>) : Gen<uint16> =
        Gen.uint16 range

    static member Int32 (range : Range<int32>) : Gen<int32> =
        Gen.int range

    static member UInt32 (range : Range<uint32>) : Gen<uint32> =
        Gen.uint32 range

    static member Int64 (range : Range<int64>) : Gen<int64> =
        Gen.int64 range

    static member UInt64 (range : Range<uint64>) : Gen<uint64> =
        Gen.uint64 range

    static member Double (range : Range<double>) : Gen<double> =
        Gen.double range

    static member Single (range : Range<float>) : Gen<float> =
        Gen.float range

    static member Guid : Gen<Guid> =
        Gen.guid

    static member DateTime : Gen<DateTime> =
        Gen.dateTime

    [<Extension>]
    static member inline Apply(mf : Gen<Func<'T, 'TResult>>, ma : Gen<'T>) : Gen<'TResult> =
        Gen.apply (mf |> Gen.map (fun f -> f.Invoke)) ma

    [<Extension>]
    static member inline Array(gen : Gen<'T>, range : Range<int>) : Gen<'T []> =
        Gen.array range gen

    [<Extension>]
    static member inline Enumerable(gen : Gen<'T>, range : Range<int>) : Gen<seq<'T>> =
        Gen.seq range gen

    [<Extension>]
    static member inline GenerateTree(gen : Gen<'T>) : Tree<'T> =
        Gen.generateTree gen

    [<Extension>]
    static member inline List(gen : Gen<'T>, range : Range<int>) : Gen<List<'T>> =
        Gen.list range gen

    [<Extension>]
    static member inline NoShrink (gen : Gen<'T>) : Gen<'T> =
        Gen.noShrink gen

    [<Extension>]
    static member inline Option(gen : Gen<'T>) : Gen<Option<'T>> =
        Gen.option gen

    [<Extension>]
    static member inline PrintSample(gen : Gen<'T>) : unit =
        Gen.printSample gen

    [<Extension>]
    static member inline Resize(gen : Gen<'T>, size : Size) : Gen<'T> =
        Gen.resize size gen

    [<Extension>]
    static member inline Sample(gen : Gen<'T>, size : Size, count : int) : List<'T> =
        Gen.sample size count gen

    [<Extension>]
    static member inline SampleTree(gen : Gen<'T>, size : Size, count : int) : List<Tree<'T>> =
        Gen.sampleTree size count gen

    [<Extension>]
    static member inline Scale(gen : Gen<'T>, scaler : Func<int, int>) : Gen<'T> =
        Gen.scale scaler.Invoke gen

    [<Extension>]
    static member inline SelectMany(gen : Gen<'T>, binder : Func<'T, Gen<'U>>) : Gen<'U> =
        Gen.bind gen binder.Invoke

    [<Extension>]
    static member inline SelectMany(gen : Gen<'T>, binder : Func<'T, Gen<'TCollection>>, projection : Func<'T, 'TCollection, 'TResult>) : Gen<'TResult> =
        Gen.bind gen (fun a ->
            Gen.map (fun b -> projection.Invoke(a, b)) (binder.Invoke(a))
        )

    [<Extension>]
    static member inline SelectRandom(gen : Gen<'T>, binder : Func<Random<Tree<'T>>, Random<Tree<'TResult>>>) : Gen<'TResult> =
        Gen.mapRandom binder.Invoke gen

    [<Extension>]
    static member inline SelectTree(gen : Gen<'T>, binder : Func<Tree<'T>, Tree<'TResult>>) : Gen<'TResult> =
        Gen.mapTree binder.Invoke gen

    [<Extension>]
    static member inline Select(gen : Gen<'T>, mapper : Func<'T, 'TResult>) : Gen<'TResult> =
        Gen.map mapper.Invoke gen

    [<Extension>]
    static member inline Select2(genA : Gen<'T>, mapper : Func<'T, 'U, 'TResult>, genB : Gen<'U>) : Gen<'TResult> =
        Gen.map2 (fun a b -> mapper.Invoke(a, b))
            genA
            genB

    [<Extension>]
    static member inline Select3(genA : Gen<'T>, mapper : Func<'T, 'U, 'V, 'TResult>, genB : Gen<'U>, genC : Gen<'V>) : Gen<'TResult> =
        Gen.map3 (fun a b c -> mapper.Invoke(a, b, c))
            genA
            genB
            genC

    [<Extension>]
    static member inline Select4(genA : Gen<'T>, mapper : Func<'T, 'U, 'V, 'W, 'TResult>, genB : Gen<'U>, genC : Gen<'V>, genD : Gen<'W>) : Gen<'TResult> =
        Gen.map4 (fun a b c d -> mapper.Invoke(a, b, c, d))
            genA
            genB
            genC
            genD

    [<Extension>]
    static member inline Shrink(gen : Gen<'T>, shrinker : Func<'T, List<'T>>) : Gen<'T> =
        Gen.shrink shrinker.Invoke gen

    [<Extension>]
    static member inline ShrinkLazy(gen : Gen<'T>, shrinker : Func<'T, seq<'T>>) : Gen<'T> =
        Gen.shrinkLazy shrinker.Invoke gen

    [<Extension>]
    static member inline Some(gen : Gen<Option<'T>>) : Gen<'T> =
        Gen.some gen

    [<Extension>]
    static member inline String(gen : Gen<char>, range : Range<int>) : Gen<string> =
        Gen.string range gen

    [<Extension>]
    static member inline ToGen(random : Random<Tree<'T>>) : Gen<'T> =
        Gen.ofRandom random

    [<Extension>]
    static member inline ToRandom(gen : Gen<'T>) : Random<Tree<'T>> =
        Gen.toRandom gen

    [<Extension>]
    static member inline TryFinally(gen : Gen<'T>, after : Action) : Gen<'T> =
        Gen.tryFinally gen after.Invoke

    [<Extension>]
    static member inline TryWhere(gen : Gen<'T>, after : Func<exn, Gen<'T>>) : Gen<'T> =
        Gen.tryWith gen after.Invoke

    [<Extension>]
    static member inline TryWith(gen : Gen<'T>, after : Func<exn, Gen<'T>>) : Gen<'T> =
        Gen.tryWith gen after.Invoke

    [<Extension>]
    static member inline Tuple2(gen : Gen<'T>) : Gen<'T * 'T> =
        Gen.tuple gen

    [<Extension>]
    static member inline Tuple3(gen : Gen<'T>) : Gen<'T * 'T * 'T> =
        Gen.tuple3 gen

    [<Extension>]
    static member inline Tuple4(gen : Gen<'T>) : Gen<'T * 'T * 'T * 'T> =
        Gen.tuple4 gen

    [<Extension>]
    static member inline Where(gen : Gen<'T>, predicate : Func<'T, bool>) : Gen<'T> =
        Gen.filter predicate.Invoke gen

    [<Extension>]
    static member inline Zip(genA : Gen<'T>, genB : Gen<'U>) : Gen<'T * 'U> =
        Gen.zip genA genB

    [<Extension>]
    static member inline Zip3(genA : Gen<'T>, genB : Gen<'U>, genC : Gen<'V>) : Gen<'T * 'U * 'V> =
        Gen.zip3 genA genB genC

    [<Extension>]
    static member inline Zip4(genA : Gen<'T>, genB : Gen<'U>, genC : Gen<'V>, genD : Gen<'W>) : Gen<'T * 'U * 'V * 'W> =
        Gen.zip4 genA genB genC genD

#endif
