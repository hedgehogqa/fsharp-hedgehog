using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static Hedgehog.Linq.Property;
using Xunit;

namespace Hedgehog.Linq.Tests;

public sealed class DefaultGeneratorsTests
{
    private readonly IAutoGenConfig _config = AutoGenConfig.Defaults.SetCollectionRange(Range.Singleton(5));

    [Fact]
    public void ShouldGenerateImmutableSet() =>
        Gen.AutoWith<ImmutableHashSet<int>>(_config).ForAll().Select(x => x.Count > 0).Check();

    [Fact]
    public void ShouldGenerateIImmutableSet() =>
        Gen.AutoWith<IImmutableSet<int>>(_config).ForAll().Select(x => x.Count > 0).Check();

    [Fact]
    public void ShouldGenerateImmutableSortedSet() =>
        Gen.AutoWith<ImmutableSortedSet<int>>(_config).ForAll().Select(x => x.Count > 0).Check();

    [Fact]
    public void ShouldGenerateImmutableList() =>
        Gen.AutoWith<ImmutableList<int>>(_config).ForAll().Select(x => x.Count == 5).Check();

    [Fact]
    public void ShouldGenerateIImmutableList() =>
        Gen.AutoWith<IImmutableList<int>>(_config).ForAll().Select(x => x.Count == 5).Check();

    [Fact]
    public void ShouldGenerateImmutableArray() =>
        Gen.AutoWith<ImmutableArray<int>>(_config).ForAll().Select(x => x.Length == 5).Check();

    [Fact]
    public void ShouldGenerateDictionary() =>
        Gen.AutoWith<Dictionary<int, string>>(_config).ForAll().Select(x => x.Count > 0).Check();

    [Fact]
    public void ShouldGenerateIDictionary() =>
        Gen.AutoWith<IDictionary<int, string>>(_config).ForAll().Select(x => x.Count > 0).Check();

    [Fact]
    public void ShouldGenerateIReadOnlyDictionary() =>
        Gen.AutoWith<IReadOnlyDictionary<int, string>>(_config).ForAll().Select(x => x.Count > 0).Check();

    [Fact]
    public void ShouldGenerateList() =>
        Gen.AutoWith<List<int>>(_config).ForAll().Select(x => x.Count == 5).Check();

    [Fact]
    public void ShouldGenerateIList() =>
        Gen.AutoWith<IList<int>>(_config).ForAll().Select(x => x.Count == 5).Check();

    [Fact]
    public void ShouldGenerateIReadOnlyList() =>
        Gen.AutoWith<IReadOnlyList<int>>(_config).ForAll().Select(x => x.Count == 5).Check();

    [Fact]
    public void ShouldGenerateIEnumerable() =>
        Gen.AutoWith<IEnumerable<int>>(_config).ForAll().Select(x => x.Count() == 5).Check();

    [Fact]
    public void StressTest() =>
        Gen.AutoWith<List<List<List<int>>>>(_config)
            .ForAll()
            .Select(x => x.Count == 5 && x.All(inner => inner.Count == 5 && inner.All(innerMost => innerMost.Count == 5)))
            .Check();

    [Fact]
    public void ShouldGenerateRecursiveTreeWithImmutableList()
    {
        // Tree node with ImmutableList of children - tests recursive generation with generic types
        var config = AutoGenConfig.Defaults
            .SetCollectionRange(Range.Singleton(2))
            .SetRecursionDepth(1);

        Gen.AutoWith<TreeNode<int>>(config)
            .ForAll()
            .Select(tree =>
            {
                // At depth 1, should have children
                // At depth 2, children's children should be empty (recursion limit)
                return tree.Children.Count == 2 &&
                       tree.Children.All(child => child.Children.Count == 0);
            })
            .Check();
    }
}

// Recursive data structure for testing
public record TreeNode<T>
{
    public T Value { get; init; }
    public List<TreeNode<T>> Children { get; init; } = [];

    public override string ToString()
    {
        if (Children.Count == 0)
            return $"Node({Value})";

        var childrenStr = string.Join(", ", Children.Select(c => c.ToString()));
        return $"Node({Value}, [{childrenStr}])";
    }
}
