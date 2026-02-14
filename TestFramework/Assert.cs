using System.Numerics;

namespace TestFramework
{
    public class Assert
    {
        public static void areEqual<T>(T expected, T actual)
        {
            if (!expected.Equals(actual))
            {
                throw new AssertException($"Values should be equal: expected={expected}, actual={actual}");
            }
        }

        public static void areNotEqual<T>(T expected, T actual)
        {
            if (expected.Equals(actual))
            {
                throw new AssertException($"Values should not be equal: {expected}");
            }
        }

        public static void isTrue(bool value)
        {
            if (!value)
            {
                throw new AssertException($"Value should be true: {value}");
            }
        }

        public static void isFalse(bool value)
        {
            if (value)
            {
                throw new AssertException($"Value should be false: {value}");
            }
        }

        public static void isNull(object value)
        {
            if (!object.ReferenceEquals(value, null))
            {
                throw new AssertException($"Value should be null: {value}");
            }
        }

        public static void isNotNull(object value)
        {
            if (object.ReferenceEquals(value, null))
            {
                throw new AssertException($"Value should be not null");
            }
        }

        public static void areSame<T>(T expected, T actual)
        {
            if (!object.ReferenceEquals(expected, actual))
            {
                throw new AssertException($"Values should be same: {expected}");
            }
        }

        public static void areNotSame<T>(T expected, T actual)
        {
            if (object.ReferenceEquals(expected, actual))
            {
                throw new AssertException($"Values should not be same: {expected}");
            }
        }

        public static void isGreaterThan<T>(T actual, T expected) where T : IComparable<T>
        {
            if (actual.CompareTo(expected) <= 0)
            {
                throw new AssertException($"Value should be greater than: actual={actual}, expected={expected}");
            }
        }

        public static void isGreaterThanOrEqualTo<T>(T actual, T expected) where T : IComparable<T>
        {
            if (actual.CompareTo(expected) < 0)
            {
                throw new AssertException($"Value should be greater than or equal to: actual={actual}, expected={expected}");
            }
        }

        public static void isLessThan<T>(T actual, T expected) where T : IComparable<T>
        {
            if (actual.CompareTo(expected) >= 0)
            {
                throw new AssertException($"Value should be less than: actual={actual}, expected={expected}");
            }
        }

        public static void isLessThanOrEqualTo<T>(T actual, T expected) where T : IComparable<T>
        {
            if (actual.CompareTo(expected) > 0)
            {
                throw new AssertException($"Value should be less than or equal to: actual={actual}, expected={expected}");
            }
        }

        public static void isEmpty<T>(IEnumerable<T> value)
        {
            if (value == null || !value.Any())
            {
                return;
            }
            throw new AssertException($"Collection should be empty");
        }

        public static void isNotEmpty<T>(IEnumerable<T> value)
        {
            if (value != null && value.Any())
            {
                return;
            }
            throw new AssertException($"Collection should not be empty");
        }


        public static void isPositive<T>(T value) where T : INumber<T>
        {
            if (value <= T.Zero)
            {
                throw new AssertException($"Value should be positive: {value}");
            }
        }


        public static void isNegative<T>(T value) where T : INumber<T>
        {
            if (value >= T.Zero)
            {
                throw new AssertException($"Value should be negative: {value}");
            }
        }
    }
}
