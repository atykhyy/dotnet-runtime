// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Collections.Immutable.Tests
{
    public partial class ImmutableSortedSetTest : ImmutableSetTest
    {
        private enum Operation
        {
            Add,
            Union,
            Remove,
            Except,
            Last,
        }

        protected override bool IncludesGetHashCodeDerivative
        {
            get { return false; }
        }

        [Fact]
        public void RandomOperationsTest()
        {
            int operationCount = this.RandomOperationsCount;
            var expected = new SortedSet<int>();
            ImmutableSortedSet<int> actual = ImmutableSortedSet<int>.Empty;

            int seed = unchecked((int)DateTime.Now.Ticks);
            Debug.WriteLine("Using random seed {0}", seed);
            var random = new Random(seed);

            for (int iOp = 0; iOp < operationCount; iOp++)
            {
                switch ((Operation)random.Next((int)Operation.Last))
                {
                    case Operation.Add:
                        int value = random.Next();
                        Debug.WriteLine("Adding \"{0}\" to the set.", value);
                        expected.Add(value);
                        actual = actual.Add(value);
                        break;
                    case Operation.Union:
                        int inputLength = random.Next(100);
                        int[] values = Enumerable.Range(0, inputLength).Select(i => random.Next()).ToArray();
                        Debug.WriteLine("Adding {0} elements to the set.", inputLength);
                        expected.UnionWith(values);
                        actual = actual.Union(values);
                        break;
                    case Operation.Remove:
                        if (expected.Count > 0)
                        {
                            int position = random.Next(expected.Count);
                            int element = expected.Skip(position).First();
                            Debug.WriteLine("Removing element \"{0}\" from the set.", element);
                            Assert.True(expected.Remove(element));
                            actual = actual.Remove(element);
                        }

                        break;
                    case Operation.Except:
                        int[] elements = expected.Where(el => random.Next(2) == 0).ToArray();
                        Debug.WriteLine("Removing {0} elements from the set.", elements.Length);
                        expected.ExceptWith(elements);
                        actual = actual.Except(elements);
                        break;
                }

                Assert.Equal<int>(expected.ToList(), actual.ToList());
            }
        }

        [Fact]
        public void CustomSort()
        {
            this.CustomSortTestHelper(
                ImmutableSortedSet<string>.Empty.WithComparer(StringComparer.Ordinal),
                true,
                new[] { "apple", "APPLE" },
                new[] { "APPLE", "apple" });
            this.CustomSortTestHelper(
                ImmutableSortedSet<string>.Empty.WithComparer(StringComparer.OrdinalIgnoreCase),
                true,
                new[] { "apple", "APPLE" },
                new[] { "apple" });
        }

        [Fact]
        public void ChangeSortComparer()
        {
            ImmutableSortedSet<string> ordinalSet = ImmutableSortedSet<string>.Empty
                .WithComparer(StringComparer.Ordinal)
                .Add("apple")
                .Add("APPLE");
            Assert.Equal(2, ordinalSet.Count); // claimed count
            Assert.False(ordinalSet.Contains("aPpLe"));

            ImmutableSortedSet<string> ignoreCaseSet = ordinalSet.WithComparer(StringComparer.OrdinalIgnoreCase);
            Assert.Equal(1, ignoreCaseSet.Count);
            Assert.True(ignoreCaseSet.Contains("aPpLe"));
        }

        [Fact]
        public void ToUnorderedTest()
        {
            ImmutableHashSet<int> result = ImmutableSortedSet<int>.Empty.Add(3).ToImmutableHashSet();
            Assert.True(result.Contains(3));
        }

        [Fact]
        public void ToImmutableSortedSetFromArrayTest()
        {
            ImmutableSortedSet<int> set = new[] { 1, 2, 2 }.ToImmutableSortedSet();
            Assert.Same(Comparer<int>.Default, set.KeyComparer);
            Assert.Equal(2, set.Count);
        }

        [Theory]
        [InlineData(new int[] { }, new int[] { })]
        [InlineData(new int[] { 1 }, new int[] { 1 })]
        [InlineData(new int[] { 1, 1 }, new int[] { 1 })]
        [InlineData(new int[] { 1, 1, 1 }, new int[] { 1 })]
        [InlineData(new int[] { 1, 2, 3 }, new int[] { 1, 2, 3 })]
        [InlineData(new int[] { 3, 2, 1 }, new int[] { 1, 2, 3 })]
        [InlineData(new int[] { 1, 1, 3 }, new int[] { 1, 3 })]
        [InlineData(new int[] { 1, 2, 2 }, new int[] { 1, 2 })]
        [InlineData(new int[] { 1, 2, 2, 3, 3, 3 }, new int[] { 1, 2, 3 })]
        [InlineData(new int[] { 1, 2, 3, 1, 2, 3 }, new int[] { 1, 2, 3 })]
        [InlineData(new int[] { 1, 1, 2, 2, 2, 3, 3, 3, 3 }, new int[] { 1, 2, 3 })]
        public void ToImmutableSortedSetFromEnumerableTest(int[] input, int[] expectedOutput)
        {
            IEnumerable<int> enumerableInput = input.Select(i => i); // prevent querying for indexable interfaces
            ImmutableSortedSet<int> set = enumerableInput.ToImmutableSortedSet();
            Assert.Equal((IEnumerable<int>)expectedOutput, set.ToArray());
        }

        [Theory]
        [InlineData(new int[] { }, new int[] { 1 })]
        [InlineData(new int[] { 1 }, new int[] { 1 })]
        [InlineData(new int[] { 1, 1 }, new int[] { 1 })]
        [InlineData(new int[] { 1, 1, 1 }, new int[] { 1 })]
        [InlineData(new int[] { 1, 2, 3 }, new int[] { 1, 2, 3 })]
        [InlineData(new int[] { 3, 2, 1 }, new int[] { 1, 2, 3 })]
        [InlineData(new int[] { 1, 1, 3 }, new int[] { 1, 3 })]
        [InlineData(new int[] { 1, 2, 2 }, new int[] { 1, 2 })]
        [InlineData(new int[] { 1, 2, 2, 3, 3, 3 }, new int[] { 1, 2, 3 })]
        [InlineData(new int[] { 1, 2, 3, 1, 2, 3 }, new int[] { 1, 2, 3 })]
        [InlineData(new int[] { 1, 1, 2, 2, 2, 3, 3, 3, 3 }, new int[] { 1, 2, 3 })]
        public void UnionWithEnumerableTest(int[] input, int[] expectedOutput)
        {
            IEnumerable<int> enumerableInput = input.Select(i => i); // prevent querying for indexable interfaces
            ImmutableSortedSet<int> set = ImmutableSortedSet.Create(1).Union(enumerableInput);
            Assert.Equal((IEnumerable<int>)expectedOutput, set.ToArray());
        }

        [Fact]
        public void IndexOfTest()
        {
            ImmutableSortedSet<int> set = ImmutableSortedSet<int>.Empty;
            Assert.Equal(~0, set.IndexOf(5));

            set = ImmutableSortedSet<int>.Empty.Union(Enumerable.Range(1, 10).Select(n => n * 10)); // 10, 20, 30, ... 100
            Assert.Equal(0, set.IndexOf(10));
            Assert.Equal(1, set.IndexOf(20));
            Assert.Equal(4, set.IndexOf(50));
            Assert.Equal(8, set.IndexOf(90));
            Assert.Equal(9, set.IndexOf(100));

            Assert.Equal(~0, set.IndexOf(5));
            Assert.Equal(~1, set.IndexOf(15));
            Assert.Equal(~2, set.IndexOf(25));
            Assert.Equal(~5, set.IndexOf(55));
            Assert.Equal(~9, set.IndexOf(95));
            Assert.Equal(~10, set.IndexOf(105));

            ImmutableSortedSet<int?> nullableSet = ImmutableSortedSet<int?>.Empty;
            Assert.Equal(~0, nullableSet.IndexOf(null));
            nullableSet = nullableSet.Add(null).Add(0);
            Assert.Equal(0, nullableSet.IndexOf(null));
        }

        [Fact]
        public void IndexGetTest()
        {
            ImmutableSortedSet<int> set = ImmutableSortedSet<int>.Empty
                .Union(Enumerable.Range(1, 10).Select(n => n * 10)); // 10, 20, 30, ... 100

            int i = 0;
            foreach (int item in set)
            {
                AssertAreSame(item, set[i++]);
            }

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => set[-1]);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => set[set.Count]);
        }

        [Fact]
        public void ReverseTest()
        {
            IEnumerable<int> range = Enumerable.Range(1, 10);
            ImmutableSortedSet<int> set = ImmutableSortedSet<int>.Empty.Union(range);
            List<int> expected = range.Reverse().ToList();
            List<int> actual = set.Reverse().ToList();
            Assert.Equal<int>(expected, actual);
        }

        [Fact]
        public void MaxTest()
        {
            Assert.Equal(5, ImmutableSortedSet<int>.Empty.Union(Enumerable.Range(1, 5)).Max);
            Assert.Equal(0, ImmutableSortedSet<int>.Empty.Max);
        }

        [Fact]
        public void MinTest()
        {
            Assert.Equal(1, ImmutableSortedSet<int>.Empty.Union(Enumerable.Range(1, 5)).Min);
            Assert.Equal(0, ImmutableSortedSet<int>.Empty.Min);
        }

        [Fact]
        public void InitialBulkAdd()
        {
            Assert.Equal(1, Empty<int>().Union(new[] { 1, 1 }).Count);
            Assert.Equal(2, Empty<int>().Union(new[] { 1, 2 }).Count);
        }

        [Fact]
        public void ICollectionOfTMethods()
        {
            ICollection<string> set = ImmutableSortedSet.Create<string>();
            Assert.Throws<NotSupportedException>(() => set.Add("a"));
            Assert.Throws<NotSupportedException>(() => set.Clear());
            Assert.Throws<NotSupportedException>(() => set.Remove("a"));
            Assert.True(set.IsReadOnly);
        }

        [Fact]
        public void IListOfTMethods()
        {
            IList<string> set = ImmutableSortedSet.Create<string>("b");
            Assert.Throws<NotSupportedException>(() => set.Insert(0, "a"));
            Assert.Throws<NotSupportedException>(() => set.RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => set[0] = "a");
            Assert.Equal("b", set[0]);
            Assert.True(set.IsReadOnly);
        }

        [Fact]
        public void UnionOptimizationsTest()
        {
            ImmutableSortedSet<int> set = ImmutableSortedSet.Create(1, 2, 3);
            ImmutableSortedSet<int>.Builder builder = set.ToBuilder();

            Assert.Same(set, ImmutableSortedSet.Create<int>().Union(builder));
            Assert.Same(set, set.Union(ImmutableSortedSet.Create<int>()));

            ImmutableSortedSet<int> smallSet = ImmutableSortedSet.Create(1);
            ImmutableSortedSet<int> unionSet = smallSet.Union(set);
            Assert.Same(set, unionSet); // adding a larger set to a smaller set is reversed, and then the smaller in this case has nothing unique
        }

        [Fact]
        public void Create()
        {
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;

            ImmutableSortedSet<string> set = ImmutableSortedSet.Create<string>();
            Assert.Equal(0, set.Count);
            Assert.Same(Comparer<string>.Default, set.KeyComparer);

            set = ImmutableSortedSet.Create<string>(comparer);
            Assert.Equal(0, set.Count);
            Assert.Same(comparer, set.KeyComparer);

            set = ImmutableSortedSet.Create("a");
            Assert.Equal(1, set.Count);
            Assert.Same(Comparer<string>.Default, set.KeyComparer);

            set = ImmutableSortedSet.Create(comparer, "a");
            Assert.Equal(1, set.Count);
            Assert.Same(comparer, set.KeyComparer);

            set = ImmutableSortedSet.Create("a", "b");
            Assert.Equal(2, set.Count);
            Assert.Same(Comparer<string>.Default, set.KeyComparer);

            set = ImmutableSortedSet.Create(comparer, "a", "b");
            Assert.Equal(2, set.Count);
            Assert.Same(comparer, set.KeyComparer);

            set = ImmutableSortedSet.CreateRange((IEnumerable<string>)new[] { "a", "b" });
            Assert.Equal(2, set.Count);
            Assert.Same(Comparer<string>.Default, set.KeyComparer);

            set = ImmutableSortedSet.CreateRange(comparer, (IEnumerable<string>)new[] { "a", "b" });
            Assert.Equal(2, set.Count);
            Assert.Same(comparer, set.KeyComparer);

            set = ImmutableSortedSet.Create(default(string));
            Assert.Equal(1, set.Count);

            set = ImmutableSortedSet.CreateRange(new[] { null, "a", null, "b" });
            Assert.Equal(3, set.Count);
        }

        [Fact]
        public void IListMethods()
        {
            IList list = ImmutableSortedSet.Create("a", "b");
            Assert.True(list.Contains("a"));
            Assert.Equal("a", list[0]);
            Assert.Equal("b", list[1]);
            Assert.Equal(0, list.IndexOf("a"));
            Assert.Equal(1, list.IndexOf("b"));
            Assert.Throws<NotSupportedException>(() => list.Add("b"));
            Assert.Throws<NotSupportedException>(() => list[3] = "c");
            Assert.Throws<NotSupportedException>(() => list.Clear());
            Assert.Throws<NotSupportedException>(() => list.Insert(0, "b"));
            Assert.Throws<NotSupportedException>(() => list.Remove("a"));
            Assert.Throws<NotSupportedException>(() => list.RemoveAt(0));
            Assert.True(list.IsFixedSize);
            Assert.True(list.IsReadOnly);
        }

        [Fact]
        public void EnumeratorRecyclingMisuse()
        {
            ImmutableSortedSet<int> collection = ImmutableSortedSet.Create<int>();
            ImmutableSortedSet<int>.Enumerator enumerator = collection.GetEnumerator();
            ImmutableSortedSet<int>.Enumerator enumeratorCopy = enumerator;
            Assert.False(enumerator.MoveNext());
            enumerator.Dispose();
            Assert.Throws<ObjectDisposedException>(() => enumerator.MoveNext());
            Assert.Throws<ObjectDisposedException>(() => enumerator.Reset());
            Assert.Throws<ObjectDisposedException>(() => enumerator.Current);
            Assert.Throws<ObjectDisposedException>(() => enumeratorCopy.MoveNext());
            Assert.Throws<ObjectDisposedException>(() => enumeratorCopy.Reset());
            Assert.Throws<ObjectDisposedException>(() => enumeratorCopy.Current);
            enumerator.Dispose(); // double-disposal should not throw
            enumeratorCopy.Dispose();

            // We expect that acquiring a new enumerator will use the same underlying Stack<T> object,
            // but that it will not throw exceptions for the new enumerator.
            enumerator = collection.GetEnumerator();
            Assert.False(enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            enumerator.Dispose();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsDebuggerTypeProxyAttributeSupported))]
        public void DebuggerAttributesValid()
        {
            DebuggerAttributes.ValidateDebuggerDisplayReferences(ImmutableSortedSet.Create<int>());
            ImmutableSortedSet<string> set = ImmutableSortedSet.Create("1", "2", "3");
            DebuggerAttributeInfo info = DebuggerAttributes.ValidateDebuggerTypeProxyProperties(set);

            object rootNode = DebuggerAttributes.GetFieldValue(ImmutableSortedSet.Create<object>(), "_root");
            DebuggerAttributes.ValidateDebuggerDisplayReferences(rootNode);
            PropertyInfo itemProperty = info.Properties.Single(pr => pr.GetCustomAttribute<DebuggerBrowsableAttribute>().State == DebuggerBrowsableState.RootHidden);
            string[] items = itemProperty.GetValue(info.Instance) as string[];
            Assert.Equal(set, items);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsDebuggerTypeProxyAttributeSupported))]
        public static void TestDebuggerAttributes_Null()
        {
            Type proxyType = DebuggerAttributes.GetProxyType(ImmutableSortedSet.Create("1", "2", "3"));
            TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => Activator.CreateInstance(proxyType, (object)null));
            Assert.IsType<ArgumentNullException>(tie.InnerException);
        }

        [Fact]
        public void SymmetricExceptWithComparerTests()
        {
            ImmutableSortedSet<string> set = ImmutableSortedSet.Create<string>("a").WithComparer(StringComparer.OrdinalIgnoreCase);
            var otherCollection = new[] {"A"};

            var expectedSet = new SortedSet<string>(set, set.KeyComparer);
            expectedSet.SymmetricExceptWith(otherCollection);

            ImmutableSortedSet<string> actualSet = set.SymmetricExcept(otherCollection);
            CollectionAssertAreEquivalent(expectedSet.ToList(), actualSet.ToList());
        }

        [Fact]
        public void ItemRef()
        {
            var array = new[] { 1, 2, 3 }.ToImmutableSortedSet();

            ref readonly int safeRef = ref array.ItemRef(1);
            ref int unsafeRef = ref Unsafe.AsRef(safeRef);

            Assert.Equal(2, array.ItemRef(1));

            unsafeRef = 4;

            Assert.Equal(4, array.ItemRef(1));
        }

        [Fact]
        public void ItemRef_OutOfBounds()
        {
            var array = new[] { 1, 2, 3 }.ToImmutableSortedSet();

            Assert.Throws<ArgumentOutOfRangeException>(() => array.ItemRef(5));
        }

        protected override IImmutableSet<T> Empty<T>()
        {
            return ImmutableSortedSet<T>.Empty;
        }

        protected ImmutableSortedSet<T> EmptyTyped<T>()
        {
            return ImmutableSortedSet<T>.Empty;
        }

        protected override ISet<T> EmptyMutable<T>()
        {
            return new SortedSet<T>();
        }
    }
}
