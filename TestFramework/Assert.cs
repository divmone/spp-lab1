using System.Linq.Expressions;
using System.Numerics;
using System.Text;

namespace TestFramework
{
    public class Assert
    {
        public static void areEqual<T>(T expected, T actual)
        {
            if (!expected.Equals(actual))
                throw new AssertException($"Values should be equal: expected={expected}, actual={actual}");
        }

        public static void areNotEqual<T>(T expected, T actual)
        {
            if (expected.Equals(actual))
                throw new AssertException($"Values should not be equal: {expected}");
        }

        public static void isTrue(bool value)
        {
            if (!value)
                throw new AssertException($"Value should be true: {value}");
        }

        public static void isFalse(bool value)
        {
            if (value)
                throw new AssertException($"Value should be false: {value}");
        }

        public static void isNull(object value)
        {
            if (!object.ReferenceEquals(value, null))
                throw new AssertException($"Value should be null: {value}");
        }

        public static void isNotNull(object value)
        {
            if (object.ReferenceEquals(value, null))
                throw new AssertException("Value should be not null");
        }

        public static void areSame<T>(T expected, T actual)
        {
            if (!object.ReferenceEquals(expected, actual))
                throw new AssertException($"Values should be same: {expected}");
        }

        public static void areNotSame<T>(T expected, T actual)
        {
            if (object.ReferenceEquals(expected, actual))
                throw new AssertException($"Values should not be same: {expected}");
        }

        public static void isGreaterThan<T>(T actual, T expected) where T : IComparable<T>
        {
            if (actual.CompareTo(expected) <= 0)
                throw new AssertException($"Value should be greater than: actual={actual}, expected={expected}");
        }

        public static void isGreaterThanOrEqualTo<T>(T actual, T expected) where T : IComparable<T>
        {
            if (actual.CompareTo(expected) < 0)
                throw new AssertException($"Value should be greater than or equal to: actual={actual}, expected={expected}");
        }

        public static void isLessThan<T>(T actual, T expected) where T : IComparable<T>
        {
            if (actual.CompareTo(expected) >= 0)
                throw new AssertException($"Value should be less than: actual={actual}, expected={expected}");
        }

        public static void isLessThanOrEqualTo<T>(T actual, T expected) where T : IComparable<T>
        {
            if (actual.CompareTo(expected) > 0)
                throw new AssertException($"Value should be less than or equal to: actual={actual}, expected={expected}");
        }

        public static void isEmpty<T>(IEnumerable<T> value)
        {
            if (value == null || !value.Any()) return;
            throw new AssertException("Collection should be empty");
        }

        public static void isNotEmpty<T>(IEnumerable<T> value)
        {
            if (value != null && value.Any()) return;
            throw new AssertException("Collection should not be empty");
        }

        public static void isPositive<T>(T value) where T : INumber<T>
        {
            if (value <= T.Zero)
                throw new AssertException($"Value should be positive: {value}");
        }

        public static void isNegative<T>(T value) where T : INumber<T>
        {
            if (value >= T.Zero)
                throw new AssertException($"Value should be negative: {value}");
        }

        public static void That(Expression<Func<bool>> expression)
        {
            bool result;
            try { result = expression.Compile()(); }
            catch (Exception ex)
            {
                throw new AssertException(
                    $"Assert.That threw during evaluation: {ex.Message}\n" +
                    $"  Expression: {expression.Body}");
            }

            if (!result)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Assert.That failed.");
                sb.AppendLine($"  Expression : {expression.Body}");
                DescribeExpression(expression.Body, sb, "  ");
                throw new AssertException(sb.ToString().TrimEnd());
            }
        }

        private static void DescribeExpression(Expression expr, StringBuilder sb, string indent)
        {
            switch (expr)
            {
                case BinaryExpression bin: DescribeBinary(bin, sb, indent); break;
                case UnaryExpression unary: DescribeUnary(unary, sb, indent); break;
                case MethodCallExpression c: DescribeMethodCall(c, sb, indent); break;
                case MemberExpression member:
                    sb.AppendLine($"{indent}{member.Member.Name} = {EvalSafe(member) ?? "null"}");
                    break;
                case ConstantExpression constant:
                    sb.AppendLine($"{indent}Constant = {constant.Value ?? "null"}");
                    break;
                default:
                    sb.AppendLine($"{indent}Node ({expr.NodeType}): {EvalSafe(expr) ?? "null"}");
                    break;
            }
        }

        private static void DescribeBinary(BinaryExpression bin, StringBuilder sb, string indent)
        {
            string op = bin.NodeType switch
            {
                ExpressionType.Equal => "==",
                ExpressionType.NotEqual => "!=",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "<=",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.AndAlso => "&&",
                ExpressionType.OrElse => "||",
                ExpressionType.Add => "+",
                ExpressionType.Subtract => "-",
                ExpressionType.Multiply => "*",
                ExpressionType.Divide => "/",
                ExpressionType.Modulo => "%",
                _ => bin.NodeType.ToString()
            };

            sb.AppendLine($"{indent}Operator : {op}");
            sb.AppendLine($"{indent}Left     : {bin.Left}  =>  {EvalSafe(bin.Left) ?? "null"}");
            sb.AppendLine($"{indent}Right    : {bin.Right}  =>  {EvalSafe(bin.Right) ?? "null"}");

            if (bin.Left is BinaryExpression || bin.Left is MethodCallExpression)
            { sb.AppendLine($"{indent}  [left subtree]"); DescribeExpression(bin.Left, sb, indent + "    "); }
            if (bin.Right is BinaryExpression || bin.Right is MethodCallExpression)
            { sb.AppendLine($"{indent}  [right subtree]"); DescribeExpression(bin.Right, sb, indent + "    "); }
        }

        private static void DescribeUnary(UnaryExpression unary, StringBuilder sb, string indent)
        {
            string op = unary.NodeType switch
            {
                ExpressionType.Not => "!",
                ExpressionType.Negate => "-",
                _ => unary.NodeType.ToString()
            };
            sb.AppendLine($"{indent}Operator : {op}");
            sb.AppendLine($"{indent}Operand  : {unary.Operand}  =>  {EvalSafe(unary.Operand) ?? "null"}");
            DescribeExpression(unary.Operand, sb, indent + "  ");
        }

        private static void DescribeMethodCall(MethodCallExpression call, StringBuilder sb, string indent)
        {
            sb.AppendLine($"{indent}Method   : {call.Method.DeclaringType?.Name}.{call.Method.Name}");
            if (call.Object != null)
                sb.AppendLine($"{indent}Instance : {call.Object}  =>  {EvalSafe(call.Object) ?? "null"}");
            for (int i = 0; i < call.Arguments.Count; i++)
                sb.AppendLine($"{indent}Arg[{i}]   : {call.Arguments[i]}  =>  {EvalSafe(call.Arguments[i]) ?? "null"}");
        }

        private static object? EvalSafe(Expression expr)
        {
            try { return Expression.Lambda(expr).Compile().DynamicInvoke(); }
            catch { return "<eval error>"; }
        }
    }
}