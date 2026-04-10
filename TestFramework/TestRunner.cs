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
        private readonly Func<MethodInfo, bool> _filter;

        public TestRunner(Func<MethodInfo, bool>? filter = null)
        {
            _filter = filter ?? TestFilter.All;
        }

        public List<TestResult> RunAll(Assembly assembly)
        {
            var allResults = new List<TestResult>();
            foreach (Type type in assembly.GetTypes())
            {
                if (type.GetCustomAttribute<TestClassAttribute>() == null) continue;
                allResults.AddRange(RunClass(type));
            }
            return allResults;
        }

        public List<TestResult> RunClass(Type testClassType)
        {
            var results = new List<TestResult>();
            object instance = Activator.CreateInstance(testClassType)!;

            var classInitMethod = GetMethodWithAttribute<TestClassInitAttribute>(testClassType);
            var classCleanupMethod = GetMethodWithAttribute<TestClassCleanupAttribute>(testClassType);

            classInitMethod?.Invoke(instance, null);

            var methods = testClassType.GetMethods()
                .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null
                         || m.GetCustomAttribute<TestAsyncAttribute>() != null)
                .Where(_filter)    // ← фильтрация делегатом
                .OrderByDescending(m => m.GetCustomAttribute<TestPriorityAttribute>()?.Priority ?? 0);

            foreach (var method in methods)
            {
                bool isIgnored = method.GetCustomAttribute<TestIgnoreAttribute>() != null;
                int? timeoutMs = method.GetCustomAttribute<TestTimeoutAttribute>()?.MillisecondsTimeout;
                bool isAsync = method.GetCustomAttribute<TestAsyncAttribute>() != null;
                var dataAttrs = method.GetCustomAttributes<TestDataAttribute>().ToArray();
                var dataSource = method.GetCustomAttribute<TestDataSourceAttribute>(); // ЛР 4

                if (isIgnored)
                {
                    results.Add(new TestResult
                    {
                        TestName = $"{testClassType.Name}.{method.Name}",
                        Status = TestStatus.Ignored
                    });
                    continue;
                }

                if (dataSource != null)
                {
                    var generator = testClassType.GetMethod(
                        dataSource.MethodName,
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                    if (generator == null)
                    {
                        Console.WriteLine($"[WARN] DataSource method '{dataSource.MethodName}' not found in {testClassType.Name}");
                    }
                    else
                    {
                        var cases = ((IEnumerable<object[]>)generator.Invoke(null, null)!).ToList();
                        int n = 0;
                        foreach (var caseArgs in cases)
                        {
                            n++;
                            var r = RunMethod(testClassType, method, caseArgs,
                                $"{testClassType.Name}.{method.Name} (Gen {n})", isAsync, timeoutMs);
                            PrintResult(r);
                            results.Add(r);
                        }
                    }
                    continue;
                }

              
                if (dataAttrs.Length > 0)
                {
                    int caseNum = 0;
                    foreach (var data in dataAttrs)
                    {
                        caseNum++;
                        var r = RunMethod(testClassType, method, data.Parametrs,
                            $"{testClassType.Name}.{method.Name} (Case {caseNum})", isAsync, timeoutMs);
                        PrintResult(r);
                        results.Add(r);
                    }
                }
                else
                {
                    var r = RunMethod(testClassType, method, null,
                        $"{testClassType.Name}.{method.Name}", isAsync, timeoutMs);
                    PrintResult(r);
                    results.Add(r);
                }
            }

            classCleanupMethod?.Invoke(instance, null);
            return results;
        }

        private TestResult RunMethod(
            Type testClassType, MethodInfo method, object[]? parameters,
            string testName, bool isAsync, int? timeoutMs)
        {
            object instance = Activator.CreateInstance(testClassType)!;
            var initMethod = GetMethodWithAttribute<TestMethodInitAttribute>(testClassType);
            var cleanupMethod = GetMethodWithAttribute<TestMethodCleanupAttribute>(testClassType);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                initMethod?.Invoke(instance, null);

                if (isAsync)
                    ((Task)method.Invoke(instance, parameters)!).GetAwaiter().GetResult();
                else
                    method.Invoke(instance, parameters);

                return new TestResult
                {
                    TestName = testName,
                    Status = TestStatus.Passed,
                    ElapsedMs = stopwatch.ElapsedMilliseconds
                };
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

        private static MethodInfo? GetMethodWithAttribute<TAttr>(Type type) where TAttr : Attribute
            => type.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<TAttr>() != null);

        private static void PrintResult(TestResult result)
            => Console.WriteLine(result.ToString());
    }
}