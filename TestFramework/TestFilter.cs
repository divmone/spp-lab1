using System.Reflection;

namespace TestFramework
{
    public static class TestFilter
    {
        public static readonly Func<MethodInfo, bool> All = _ => true;
        public static Func<MethodInfo, bool> ByCategory(string category)
            => m => m.GetCustomAttributes(typeof(TestCategoryAttribute), false)
                     .Cast<TestCategoryAttribute>()
                     .Any(a => string.Equals(a.Category, category, StringComparison.OrdinalIgnoreCase));

        public static Func<MethodInfo, bool> ByMinPriority(int minPriority)
            => m =>
            {
                var attr = m.GetCustomAttributes(typeof(TestPriorityAttribute), false)
                            .Cast<TestPriorityAttribute>()
                            .FirstOrDefault();
                return attr != null && attr.Priority >= minPriority;
            };
        public static Func<MethodInfo, bool> ByAuthor(string author)
            => m => m.GetCustomAttributes(typeof(TestAuthorAttribute), false)
                     .Cast<TestAuthorAttribute>()
                     .Any(a => string.Equals(a.Author, author, StringComparison.OrdinalIgnoreCase));

        public static Func<MethodInfo, bool> And(params Func<MethodInfo, bool>[] filters)
            => m => filters.All(f => f(m));

        public static Func<MethodInfo, bool> Or(params Func<MethodInfo, bool>[] filters)
            => m => filters.Any(f => f(m));
    }
}