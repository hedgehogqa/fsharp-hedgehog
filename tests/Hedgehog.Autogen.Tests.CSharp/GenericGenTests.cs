using System.Collections.Immutable;
using Xunit;
using Hedgehog.Linq;
using Range = Hedgehog.Linq.Range;

namespace Hedgehog.AutoGen.Linq.Tests;

public sealed record Uuid(Guid Value);

public sealed record Name(string Value);

public sealed record Id<T>(Guid Value);

public abstract record Either<TLeft, TRight>
{
  public sealed record Left(TLeft Value) : Either<TLeft, TRight>;

  public sealed record Right(TRight Value) : Either<TLeft, TRight>;
}

public abstract record Maybe<T>
{
  public sealed record Just(T Value) : Maybe<T>;

  public sealed record Nothing : Maybe<T>;
}

public sealed record OuterRecord(Maybe<Guid> Value);

public sealed class OuterClass
{
  public OuterClass(Maybe<Guid> value) => Value = value;
  public Maybe<Guid> Value { get; set; }
}

public sealed record RecursiveRec(Maybe<RecursiveRec> Value);

public sealed class GenericTestGenerators
{
  public static Gen<Guid> Guid() =>
    Gen.Byte(Range.ConstantBoundedByte())
      .Array(Range.Singleton(12))
      .Select(bytes => new byte[4].Concat(bytes).ToArray())
      .Select(bytes => new Guid(bytes));

  public static Gen<Id<T>> IdGen<T>(Gen<Guid> gen) =>
    gen.Select(value => new Id<T>(value));

  public static Gen<Uuid> UuidGen() =>
    Guid().Select(value => new Uuid(value));

  public static Gen<Name> NameGen(Gen<string> gen) =>
    gen.Select(value => new Name("Name: " + value));

  public static Gen<Maybe<T>> AlwaysJust<T>(AutoGenContext context, Gen<T> gen) =>
    context.CanRecurse
      ? gen.Select(Maybe<T> (value) => new Maybe<T>.Just(value))
      : Gen.Constant<Maybe<T>>(new Maybe<T>.Nothing());

  public static Gen<Either<TLeft, TRight>> AlwaysLeft<TLeft, TRight>(Gen<TRight> genB, Gen<TLeft> genA) =>
    genA.Select(Either<TLeft, TRight> (value) => new Either<TLeft, TRight>.Left(value));
}

public class GenericGenTests
{
  private static bool IsCustomGuid(Guid guid) =>
    new Span<byte>(guid.ToByteArray(), 0, 4).ToArray().All(b => b == 0);

  [Fact]
  public void ShouldGenerateRecursiveRecords()
  {
    var config = AutoGenConfig.Defaults.AddGenerators<GenericTestGenerators>();
    var prop = from x in Gen.AutoWith<RecursiveRec>(config).ForAll()
               select x != null;

    prop.Check();
  }

  [Fact]
  public void ShouldGenerateValueWithPhantomGenericType_Id()
  {
    var config = AutoGenConfig.Defaults.AddGenerators<GenericTestGenerators>();
    var prop = from x in Gen.AutoWith<Id<string>>(config).ForAll()
               select IsCustomGuid(x.Value);

    prop.Check();
  }

  [Fact]
  public void ShouldGenerateGenericValueForUnionType_Either()
  {
    var config = AutoGenConfig.Defaults.AddGenerators<GenericTestGenerators>();
    var prop = from x in Gen.AutoWith<Either<int, string>>(config).ForAll()
               select x is Either<int, string>.Left;
    prop.Check();
  }

  [Fact]
  public void ShouldGenerateGenericValueForUnionType_Maybe()
  {
    var config = AutoGenConfig.Defaults.AddGenerators<GenericTestGenerators>();
    var prop = from x in Gen.AutoWith<Maybe<string>>(config).ForAll()
               select x is Maybe<string>.Just;
    prop.Check();
  }

  [Fact]
  public void ShouldGenerateValueUsingGeneratorWithoutParameters_Uuid()
  {
    var config = AutoGenConfig.Defaults.AddGenerators<GenericTestGenerators>();
    var prop = from x in Gen.AutoWith<Uuid>(config).ForAll()
               select IsCustomGuid(x.Value);
    prop.Check();
  }

  [Fact]
  public void ShouldGenerateValueUsingGeneratorWithParameters_Name()
  {
    var config = AutoGenConfig.Defaults.AddGenerators<GenericTestGenerators>();
    var prop = from x in Gen.AutoWith<Name>(config).ForAll()
               select x.Value.StartsWith("Name: ");
    prop.Check();
  }

  [Fact]
  public void ShouldGenerateOuterFSharpRecordWithGenericTypeInside()
  {
    var config = AutoGenConfig.Defaults.AddGenerators<GenericTestGenerators>();
    var prop = from x in Gen.AutoWith<OuterRecord>(config).ForAll()
      select x.Value switch
      {
        Maybe<Guid>.Just(var v) => IsCustomGuid(v),
        Maybe<Guid>.Nothing => false,
        _ => throw new InvalidOperationException("C# cannot do exhaustive matching")
      };

    prop.Check();
  }

  [Fact]
  public void ShouldGenerateOuterClassWithGenericTypeInside()
  {
    var config = AutoGenConfig.Defaults.AddGenerators<GenericTestGenerators>();
    var prop = from x in Gen.AutoWith<OuterClass>(config).ForAll()
      select x.Value switch
      {
        Maybe<Guid>.Just(var v) => IsCustomGuid(v),
        Maybe<Guid>.Nothing => false,
        _ => throw new InvalidOperationException("C# cannot do exhaustive matching")
      };
    prop.Check();
  }

  [Fact]
  public void ShouldGenerateImmutableListUsingAutoGenConfigParameter()
  {
    var config = AutoGenConfig.Defaults
      .SetCollectionRange(Range.Singleton(7))
      .AddGenerators<GenericTestGenerators>();

    var prop = from x in Gen.AutoWith<ImmutableList<int>>(config).ForAll()
               select x.Count == 7;

    prop.Check();

    var propString = from x in Gen.AutoWith<ImmutableList<string>>(config).ForAll()
                     select x.Count == 7;

    propString.Check();
  }
}
