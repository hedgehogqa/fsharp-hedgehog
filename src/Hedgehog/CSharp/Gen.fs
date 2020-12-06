namespace Hedgehog.CSharp

#if !FABLE_COMPILER

open System
open System.Runtime.CompilerServices
open Hedgehog

module Gen =

    let FromValue value : Gen<'T> =
        Gen.constant(value)

    let FromRandom random : Gen<'T> =
        Gen.ofRandom(random)

    let Delay (func : Func<_>) : Gen<'T> =
        Gen.delay func.Invoke

    let Create shrink random : Gen<'T> =
        Gen.create shrink random

    let Sized (scaler : Func<_, _>) : Gen<'T> =
        Gen.sized scaler.Invoke

    let inline Integral range : Gen<'T> =
        Gen.integral range

    let Item sequence : Gen<'T> =
        Gen.item sequence

    let Frequency gens : Gen<'T> =
        Gen.frequency gens

    let Choice gens : Gen<'T> =
        Gen.choice gens

    let ChoiceRecursive nonrecs recs =
        Gen.choiceRec nonrecs recs

    let Char lo hi =
        Gen.char lo hi

    let UnicodeAll =
        Gen.unicodeAll

    let Digit =
        Gen.digit

    let Lower =
        Gen.lower

    let Upper =
        Gen.upper

    let Ascii =
        Gen.ascii

    let Latin1 =
        Gen.latin1

    let Unicode =
        Gen.unicode

    let Alpha =
        Gen.alpha

    let AlphaNumeric =
        Gen.alphaNum

    let Bool =
        Gen.bool

    let Byte range =
        Gen.byte range

    let SByte range =
        Gen.sbyte range

    let Int16 range =
        Gen.int16 range

    let UInt16 range =
        Gen.uint16 range

    let Int32 range =
        Gen.int range

    let UInt32 range =
        Gen.uint32 range

    let Int64 range =
        Gen.int64 range

    let UInt64 (range : Range<uint64>) =
        Gen.uint64 range

    let Double range =
        Gen.double range

    let Single (range : Range<float>) =
        Gen.float range

    let Guid =
        Gen.guid

    let DateTime =
        Gen.dateTime

[<Extension>]
type GenExtensions =

    [<Extension>]
    static member inline Apply(mf : Gen<Func<'T, 'U>>) ma =
        Gen.apply (mf |> Gen.map (fun f -> f.Invoke)) ma

    [<Extension>]
    static member inline Array(gen : Gen<'T>, range) =
        Gen.array range gen

    [<Extension>]
    static member inline Enumerable(gen : Gen<'T>, range) =
        Gen.seq range gen

    [<Extension>]
    static member inline GenerateTree(gen : Gen<'T>) =
        Gen.generateTree gen

    [<Extension>]
    static member inline List(gen : Gen<'T>, range) =
        Gen.list range gen

    [<Extension>]
    static member inline NoShrink gen : Gen<'T> =
        Gen.noShrink gen

    [<Extension>]
    static member inline Option(gen : Gen<'T>) =
        Gen.option gen

    [<Extension>]
    static member inline PrintSample(gen : Gen<'T>) =
        Gen.printSample gen

    [<Extension>]
    static member inline Resize(gen, size) : Gen<'T> =
        Gen.resize size gen

    [<Extension>]
    static member inline Sample(gen : Gen<'T>, size, count) =
        Gen.sample size count gen

    [<Extension>]
    static member inline SampleTree(gen : Gen<'T>, size, count) =
        Gen.sampleTree size count gen

    [<Extension>]
    static member inline Scale(gen : Gen<'T>, scaler: Func<_, _>) =
        Gen.scale scaler.Invoke gen

    [<Extension>]
    static member inline SelectMany(gen : Gen<'T>, f: Func<_, _>) : Gen<'U> =
        Gen.bind gen f.Invoke

    [<Extension>]
    static member inline SelectMany(gen, f : Func<_, _>, proj : Func<'T, 'TCollection, 'TResult>) : Gen<'TResult> =
        Gen.bind gen (fun a ->
            Gen.map (fun b -> proj.Invoke(a, b)) (f.Invoke(a))
        )

    [<Extension>]
    static member inline SelectRandom(gen : Gen<'T>, f: Func<_, _>) : Gen<'U> =
        Gen.mapRandom f.Invoke gen

    [<Extension>]
    static member inline SelectTree(gen : Gen<'T>, f: Func<_, _>) : Gen<'U> =
        Gen.mapTree f.Invoke gen

    [<Extension>]
    static member inline Select(gen : Gen<'T>, f: Func<_, _>) : Gen<'U> =
        Gen.map f.Invoke gen

    [<Extension>]
    static member inline Select2(genA, f: Func<'T, 'U, 'TResult>, genB) =
        Gen.map2 (fun a b -> f.Invoke(a, b))
            genA
            genB

    [<Extension>]
    static member inline Select3(genA, f: Func<'T, 'U, 'V, 'TResult>, genB, genC) =
        Gen.map3 (fun a b c -> f.Invoke(a, b, c))
            genA
            genB
            genC

    [<Extension>]
    static member inline Select4(genA, f: Func<'T, 'U, 'V, 'W, 'TResult>, genB, genC, genD) =
        Gen.map4 (fun a b c d -> f.Invoke(a, b, c, d))
            genA
            genB
            genC
            genD

    [<Extension>]
    static member inline Shrink(gen : Gen<'T>, f: Func<_, _>) =
        Gen.shrink f.Invoke gen

    [<Extension>]
    static member inline ShrinkLazy(gen : Gen<'T>, f: Func<_, _>) =
        Gen.shrinkLazy f.Invoke gen

    [<Extension>]
    static member inline Some(gen) : Gen<'T> =
        Gen.some gen

    [<Extension>]
    static member inline String gen range =
        Gen.string range gen

    [<Extension>]
    static member inline ToGen(random) : Gen<'T> =
        Gen.ofRandom random

    [<Extension>]
    static member inline ToRandom(gen : Gen<'T>) =
        Gen.toRandom gen

    [<Extension>]
    static member inline TryFinally(gen : Gen<'T>, after: Action) =
        Gen.tryFinally gen after.Invoke

    [<Extension>]
    static member inline TryWhere(gen : Gen<'T>, after: Func<_, _>) =
        Gen.tryWith gen after.Invoke

    [<Extension>]
    static member inline TryWith(gen : Gen<'T>, after: Func<_, _>) =
        Gen.tryWith gen after.Invoke

    [<Extension>]
    static member inline Tuple2(gen : Gen<'T>) =
        Gen.tuple gen

    [<Extension>]
    static member inline Tuple3(gen : Gen<'T>) =
        Gen.tuple3 gen

    [<Extension>]
    static member inline Tuple4(gen : Gen<'T>) =
        Gen.tuple4 gen

    [<Extension>]
    static member inline Where(gen : Gen<'T>, predicate: Func<_, _>) =
        Gen.filter predicate.Invoke gen

    [<Extension>]
    static member inline Zip(genA, genB) : Gen<'T * 'U> =
        Gen.zip genA genB

    [<Extension>]
    static member inline Zip3(genA, genB, genC) : Gen<'T * 'U * 'V> =
        Gen.zip3 genA genB genC

    [<Extension>]
    static member inline Zip4(genA, genB, genC, genD) : Gen<'T * 'U * 'V * 'W> =
        Gen.zip4 genA genB genC genD

#endif
