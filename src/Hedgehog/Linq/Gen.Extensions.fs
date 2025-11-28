namespace Hedgehog.Linq

open System
open System.Runtime.CompilerServices
open Hedgehog
open Hedgehog.FSharp


[<AbstractClass; Sealed>]
type GenExtensions private () =

    [<Extension>]
    static member Apply (genFunc : Gen<Func<'T, 'TResult>>, genArg : Gen<'T>) : Gen<'TResult> =
        Gen.apply genArg (genFunc |> Gen.map _.Invoke)

    /// <summary>
    /// Generates an array using a 'Range' to determine the length.
    /// </summary>
    /// <param name="gen">Item generator.</param>
    /// <param name="range">Range determining the length of the array.</param>
    [<Extension>]
    static member Array (gen : Gen<'T>, range : Range<int>) : Gen<'T []> =
        Gen.array range gen

    /// <summary>
    /// Generates an enumerable using a 'Range' to determine the length.
    /// </summary>
    /// <param name="gen">Item generator.</param>
    /// <param name="range">Range determining the length of the enumerable.</param>
    [<Extension>]
    static member Enumerable (gen : Gen<'T>, range : Range<int>) : Gen<seq<'T>> =
        Gen.seq range gen

    /// Run a generator. The size passed to the generator is always 30;
    /// if you want another size then you should explicitly use 'resize'.
    [<Extension>]
    static member GenerateTree (gen : Gen<'T>) : Tree<'T> =
        Gen.generateTree gen

    /// <summary>
    /// Generates a List using a 'Range' to determine the length and a 'Gen' to produce the elements.
    /// </summary>
    /// <param name="gen">Generates the items in the List.</param>
    /// <param name="range">Range determining the length of the List.</param>
    [<Extension>]
    static member List (gen : Gen<'T>, range : Range<int>) : Gen<ResizeArray<'T>> =
        Gen.list range gen
        |> Gen.map ResizeArray

    /// Prevent a 'Gen' from shrinking.
    [<Extension>]
    static member NoShrink (gen : Gen<'T>) : Gen<'T> =
        Gen.noShrink gen

    /// Generates a <c>null</c> or a value from gen. Null becomes less common with larger Sizes.
    [<Extension>]
    static member NullReference (gen : Gen<'T>) : Gen<'T> =
        Gen.option gen |> Gen.map (Option.defaultValue null)

    /// Generates a <c>null</c> or a value from gen. Null becomes less common with larger Sizes.
    [<Extension>]
    static member NullValue (gen : Gen<'T>) : Gen<Nullable<'T>> =
        Gen.option gen |> Gen.map (Option.defaultWith Nullable << Option.map Nullable)

    /// Samples the gen 5 times with a Size of 10, called the "Outcome" in the returned string.
    /// Then the shrink path to each Outcome is produced. This may be useful in debugging
    /// shrink paths in complex Gens.
    [<Extension>]
    static member RenderSample (gen : Gen<'T>) : string =
        Gen.renderSample gen

    /// Overrides the size parameter. Returns a generator which uses the
    /// given size instead of the runtime-size parameter.
    [<Extension>]
    static member Resize (gen : Gen<'T>, size : Size) : Gen<'T> =
        Gen.resize size gen

    /// <summary>Returns a List of values produced by the generator.</summary>
    /// <param name="gen">Value generator.</param>
    /// <param name="size">The size parameter for the generator.</param>
    /// <param name="count">The number of samples to produce, i.e. the length of the List.</param>
    [<Extension>]
    static member Sample (gen : Gen<'T>, size : Size, count : int) : ResizeArray<'T> =
        Gen.sample size count gen
        |> ResizeArray

    /// <summary>Returns a List of deterministic values by scaling through sizes from startSize.
    /// This is useful for visualizing how a range scales across different sizes.
    /// Uses a fixed seed for deterministic output.</summary>
    /// <param name="gen">Value generator.</param>
    /// <param name="size">The starting size parameter.</param>
    /// <param name="count">The number of samples to produce (sizes will increment from startSize).</param>
    [<Extension>]
    static member SampleFrom (gen : Gen<'T>, size: Size, count : int) : ResizeArray<'T> =
        Gen.sampleFrom size count gen
        |> ResizeArray

    [<Extension>]
    static member SampleTree (gen : Gen<'T>, size : Size, count : int) : ResizeArray<Tree<'T>> =
        Gen.sampleTree size count gen
        |> ResizeArray

    /// Adjust the size parameter, by transforming it with the given
    /// function.
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

    /// <summary>
    /// Projects each value of a generator into a new form. Similar to <c>Enumerable.Select</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// Gen&lt;Point&gt; pointGen = Gen.Int32(Range.Constant(0,200))
    ///     .Tuple2()
    ///     .Select(tuple => new Point(tuple.Item1, tuple.Item2));
    /// </code>
    /// </example>
    [<Extension>]
    static member Select (gen : Gen<'T>, mapper : Func<'T, 'TResult>) : Gen<'TResult> =
        Gen.map mapper.Invoke gen

    /// <summary>
    /// Projects each value of a generator into a new form. Similar to <c>Enumerable.Select</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// Gen&lt;Point&gt; pointGen = Gen.Int32(Range.Constant(0,200))
    ///     .Tuple2()
    ///     .Select(tuple => new Point(tuple.Item1, tuple.Item2));
    /// </code>
    /// </example>
    [<Extension>]
    static member Select (genA : Gen<'T>, mapper : Func<'T, 'U, 'TResult>, genB : Gen<'U>) : Gen<'TResult> =
        Gen.map2 (fun a b -> mapper.Invoke (a, b))
            genA
            genB

    /// <summary>
    /// Projects each value of a generator into a new form. Similar to <c>Enumerable.Select</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// Gen&lt;Point&gt; pointGen = Gen.Int32(Range.Constant(0,200))
    ///     .Tuple2()
    ///     .Select(tuple => new Point(tuple.Item1, tuple.Item2));
    /// </code>
    /// </example>
    [<Extension>]
    static member Select (genA : Gen<'T>, mapper : Func<'T, 'U, 'V, 'TResult>, genB : Gen<'U>, genC : Gen<'V>) : Gen<'TResult> =
        Gen.map3 (fun a b c -> mapper.Invoke (a, b, c))
            genA
            genB
            genC

    /// <summary>
    /// Projects each value of a generator into a new form. Similar to <c>Enumerable.Select</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// Gen&lt;Point&gt; pointGen = Gen.Int32(Range.Constant(0,200))
    ///     .Tuple2()
    ///     .Select(tuple => new Point(tuple.Item1, tuple.Item2));
    /// </code>
    /// </example>
    [<Extension>]
    static member Select (genA : Gen<'T>, mapper : Func<'T, 'U, 'V, 'W, 'TResult>, genB : Gen<'U>, genC : Gen<'V>, genD : Gen<'W>) : Gen<'TResult> =
        Gen.map4 (fun a b c d -> mapper.Invoke (a, b, c, d))
            genA
            genB
            genC
            genD

    /// Apply an additional shrinker to all generated trees.
    [<Extension>]
    static member Shrink (gen : Gen<'T>, shrinker : Func<'T, ResizeArray<'T>>) : Gen<'T> =
        Gen.shrink (shrinker.Invoke >> Seq.toList) gen

    /// Apply an additional shrinker to all generated trees.
    [<Extension>]
    static member ShrinkLazy (gen : Gen<'T>, shrinker : Func<'T, seq<'T>>) : Gen<'T> =
        Gen.shrinkLazy shrinker.Invoke gen

    /// Runs an option generator until it produces a 'Some'.
    [<Extension>]
    static member Some (gen : Gen<Option<'T>>) : Gen<'T> =
        Gen.some gen

    /// Generates a random string using 'Range' to determine the length and the
    /// given character generator.
    [<Extension>]
    static member String (gen : Gen<char>, range : Range<int>) : Gen<string> =
        Gen.string range gen

    [<Extension>]
    static member ToGen (random : Random<Tree<'T>>) : Gen<'T> =
        Gen.ofRandom random

    [<Extension>]
    static member ToRandom (gen : Gen<'T>) : Random<Tree<'T>> =
        Gen.toRandom gen

    /// <summary>
    /// Ensures a cleanup action runs after the generator executes, even if an exception is thrown.
    /// </summary>
    /// <param name="gen">The generator to wrap with cleanup logic.</param>
    /// <param name="after">Action to execute after the generator completes or fails.</param>
    [<Extension>]
    static member TryFinally (gen : Gen<'T>, after : Action) : Gen<'T> =
        Gen.tryFinally after.Invoke gen

    /// <summary>
    /// Catches exceptions thrown by a generator and handles them with a recovery function.
    /// Use this to provide fallback behavior when a generator might throw an exception.
    /// </summary>
    /// <param name="gen">The generator that might throw an exception.</param>
    /// <param name="after">Function that receives the exception and returns a recovery generator.</param>
    /// <example>
    /// <code>
    /// var gen = riskyGen.TryWith(ex => Gen.Constant(defaultValue));
    /// </code>
    /// </example>
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

    /// Generates a value that satisfies a predicate.
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

    [<Extension>]
    static member WithNull(self : Gen<'T>) =
        Gen.withNull self

    /// Generates a value that is not null.
    [<Extension>]
    static member NotNull(self : Gen<'T>) =
        Gen.notNull self
