using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace TestFramework
{
    public class TestRunner
    {
        public List<TestResult> RunAll(Assembly assembly)
        {
            var allResults = new List<TestResult>();

            foreach (Type type in assembly.GetTypes())
            {
                if (type.GetCustomAttribute<TestClassAttribute>() == null)
                    continue;

                allResults.AddRange(RunClass(type));
            }

            return allResults;
        }

        public List<TestResult> RunClass(Type testClassType)
        {
            var results = new List<TestResult>();
            object instance = Activator.CreateInstance(testClassType);

            var classInitMethod = GetMethodWithAttribute<TestClassInitAttribute>(testClassType);
            var classCleanupMethod = GetMethodWithAttribute<TestClassCleanupAttribute>(testClassType);

            classInitMethod?.Invoke(instance, null);
      
            var methods = testClassType.GetMethods()
                .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null
                         || m.GetCustomAttribute<TestAsyncAttribute>() != null)
                .OrderByDescending(m => m.GetCustomAttribute<TestPriorityAttribute>()?.Priority ?? 0);

            foreach (var method in methods)
            {
                bool isIgnored = method.GetCustomAttribute<TestIgnoreAttribute>() != null;
                int? timeoutMs = method.GetCustomAttribute<TestTimeoutAttribute>()?.MillisecondsTimeout;
                bool isAsync = method.GetCustomAttribute<TestAsyncAttribute>() != null;
                var dataAttrs = method.GetCustomAttributes<TestDataAttribute>().ToArray();

                if (isIgnored)
                {
                    results.Add(new TestResult { TestName = $"{testClassType.Name}.{method.Name}", Status = TestStatus.Ignored });
                    continue;
                }

                if (dataAttrs.Length > 0)
                {
                    int caseNum = 0;
                    foreach (var data in dataAttrs)
                    {
                        caseNum++;
                        var result = RunMethod(testClassType, method, data.Parametrs,
                            $"{testClassType.Name}.{method.Name} (Case {caseNum})", isAsync, timeoutMs);
                        PrintResult(result);
                        results.Add(result);
                    }
                }
                else
                {
                    var result = RunMethod(testClassType, method, null,
                        $"{testClassType.Name}.{method.Name}", isAsync, timeoutMs);
                    PrintResult(result);
                    results.Add(result);
                }
            }

            classCleanupMethod?.Invoke(instance, null);
           
            return results;
        }

        private TestResult RunMethod(
            Type testClassType, MethodInfo method, object[] parameters,
            string testName, bool isAsync, int? timeoutMs)
        {
            object instance = Activator.CreateInstance(testClassType);

            var initMethod = GetMethodWithAttribute<TestMethodInitAttribute>(testClassType);
            var cleanupMethod = GetMethodWithAttribute<TestMethodCleanupAttribute>(testClassType);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                initMethod?.Invoke(instance, null);

                if (isAsync)
                    ((Task)method.Invoke(instance, parameters)).GetAwaiter().GetResult();
                else
                    method.Invoke(instance, parameters);

                return new TestResult { TestName = testName, Status = TestStatus.Passed, ElapsedMs = stopwatch.ElapsedMilliseconds };
            }
            catch (Exception ex)
            {
                return new TestResult
                {
                    TestName = testName,
                    Status = TestStatus.Failed,
                    ErrorMessage = ex.InnerException?.Message ?? ex.Message,
                    ElapsedMs = stopwatch.ElapsedMilliseconds
                };
            }
            finally
            {
                stopwatch.Stop();
                cleanupMethod?.Invoke(instance, null);
            }
        }

        private static MethodInfo GetMethodWithAttribute<TAttr>(Type type) where TAttr : Attribute
            => type.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<TAttr>() != null);

        private static void PrintResult(TestResult result)
        {
            Console.WriteLine(result.ToString());
        }
    }
}