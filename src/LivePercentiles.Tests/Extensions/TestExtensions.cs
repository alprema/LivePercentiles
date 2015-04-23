using System;
using System.Collections;
using KellermanSoftware.CompareNetObjects;
using NUnit.Framework;

namespace LivePercentiles.Tests.Extensions
{
    public static class TestExtensions
    {
        public static void ShouldBeFalse(this bool condition, string message = null)
        {
            Assert.IsFalse(condition, message);
        }

        public static void ShouldBeTrue(this bool condition, string message = null)
        {
            Assert.IsTrue(condition, message);
        }

        public static object ShouldEqual(this object actual, object expected, string message = null)
        {
            Assert.AreEqual(expected, actual, message);
            return expected;
        }
        
        public static IComparable ShouldBeLessThan(this IComparable arg1, IComparable arg2)
        {
            Assert.Less(arg1, arg2);
            return arg2;
        }

        public static void ShouldBeEquivalentTo(this IEnumerable collection, IEnumerable expected, bool compareDeeply = false)
        {
            if (compareDeeply)
            {
                var compareLogic = new CompareLogic();
                collection.ShouldBeEquivalentTo(expected, (a, b) => compareLogic.Compare(a, b).AreEqual);
            }
            else
                Assert.That(collection, Is.EquivalentTo(expected));
        }

        public static void ShouldBeEquivalentTo(this IEnumerable collection, IEnumerable expected, Func<object, object, bool> comparer)
        {
            Assert.That(collection, Is.EquivalentTo(expected).Using(new EqualityComparer(comparer)));
        }

        private class EqualityComparer : IEqualityComparer
        {
            private readonly Func<object, object, bool> _comparer;

            public EqualityComparer(Func<object, object, bool> comparer)
            {
                _comparer = comparer;
            }

            bool IEqualityComparer.Equals(object x, object y)
            {
                return _comparer(x, y);
            }

            public int GetHashCode(object obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}