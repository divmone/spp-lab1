using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace TestFramework
{
    public class ParallelTestRunner
    {
        private readonly int _maxDegreeOfParallelism;

        private readonly object _resultsLock = new();

        public ParallelTestRunner(int maxDegreeOfParallelism = 4)
        {
            if (maxDegreeOfParallelism < 1)
                throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism), "Должно быть >= 1");

            _maxDegreeOfParallelism = maxDegreeOfParallelism;
        }


        public List<TestResult> RunAll(Assembly assembly)
        {
            var allResults = new ConcurrentBag<TestResult>();

            var testClasses = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<TestClassAttribute>() != null)
                .ToList();

            Parallel.ForEach(testClasses, new ParallelOptions { MaxDegreeOfParallelism = _maxDegreeOfParallelism }, type =>
            {
                foreach (var result in RunClass(type))
                    allResults.Add(result);
            });

            return allResults.ToList();
        }

        public List<TestResult> RunClass(Type testClassType)
        {
            var results = new List<TestResult>();

            object sharedInstance = Activator.CreateInstance(testClassType);

            var classInitMethod = GetMethodWithAttribute<TestClassInitAttribute>(testClassType);
            var classCleanupMethod = GetMethodWithAttribute<TestClassCleanupAttribute>(testClassType);

            try
            {
                classInitMethod?.Invoke(sharedInstance, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CRITICAL] ClassInit for {testClassType.Name} failed: {ex.InnerException?.Message ?? ex.Message}");
                return results;
            }

            var tests = BuildTests(testClassType);

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = _maxDegreeOfParallelism
            };

            Parallel.ForEach(tests, parallelOptions, test =>
            {
                TestResult result = ExecuteTest(test, testClassType);

                lock (_resultsLock)
                {
                    results.Add(result);
                    PrintResult(result);
                }
            });

            try
            {
                classCleanupMethod?.Invoke(sharedInstance, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] ClassCleanup for {testClassType.Name}: {ex.InnerException?.Message ?? ex.Message}");
            }

            return results;
        }

        private record Test(
            string TestName,
            MethodInfo Method,
            object[] Parameters,
            bool IsIgnored,
            int? TimeoutMs
        );

        private static List<Test> BuildTests(Type type)
        {
            var tests = new List<Test>();

            var methods = type.GetMethods()
                .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null
                         || m.GetCustomAttribute<TestAsyncAttribute>() != null)
                .OrderByDescending(m => m.GetCustomAttribute<TestPriorityAttribute>()?.Priority ?? 0);

            foreach (var method in methods)
            {
                bool isIgnored = method.GetCustomAttribute<TestIgnoreAttribute>() != null;
                int? timeoutMs = method.GetCustomAttribute<TestTimeoutAttribute>()?.MillisecondsTimeout;
                var dataAttrs = method.GetCustomAttributes<TestDataAttribute>().ToArray();

                if (dataAttrs.Length > 0)
                {
                    int caseNumber = 0;
                    foreach (var data in dataAttrs)
                    {
                        caseNumber++;
                        tests.Add(new Test(
                            TestName: $"{type.Name}.{method.Name} (Case {caseNumber})",
                            Method: method,
                            Parameters: data.Parametrs,
                            IsIgnored: isIgnored,
                            TimeoutMs: timeoutMs
                        ));
                    }
                }
                else
                {
                    tests.Add(new Test(
                        TestName: $"{type.Name}.{method.Name}",
                        Method: method,
                        Parameters: null,
                        IsIgnored: isIgnored,
                        TimeoutMs: timeoutMs
                    ));
                }
            }

            return tests;
        }

        private TestResult ExecuteTest(Test test, Type testClassType)
        {
            if (test.IsIgnored)
                return new TestResult { TestName = test.TestName, Status = TestStatus.Ignored };

            object instance = Activator.CreateInstance(testClassType);

            var methodInitMethod = GetMethodWithAttribute<TestMethodInitAttribute>(testClassType);
            var methodCleanupMethod = GetMethodWithAttribute<TestMethodCleanupAttribute>(testClassType);

            var stopwatch = Stopwatch.StartNew();
            TestResult result;

            try
            {
                methodInitMethod?.Invoke(instance, null);
                result = RunTestWithOptionalTimeout(test, instance);
            }
            catch (Exception ex)
            {
                result = new TestResult
                {
                    TestName = test.TestName,
                    Status = TestStatus.Failed,
                    ErrorMessage = ex.InnerException?.Message ?? ex.Message
                };
            }
            finally
            {
                stopwatch.Stop();
                TryInvokeCleanup(methodCleanupMethod, instance, test.TestName);
            }

            result.ElapsedMs = stopwatch.ElapsedMilliseconds;
            return result;
        }

        private static TestResult RunTestWithOptionalTimeout(Test test, object instance)
        {
            bool isAsync = test.Method.GetCustomAttribute<TestAsyncAttribute>() != null;

            if (test.TimeoutMs.HasValue)
                return RunWithTimeout(test, instance, isAsync);

            InvokeMethod(test.Method, instance, test.Parameters, isAsync);
            return new TestResult { TestName = test.TestName, Status = TestStatus.Passed };
        }

        private static TestResult RunWithTimeout(Test test, object instance, bool isAsync)
        {
            using var cts = new CancellationTokenSource();
            Exception caught = null;

            var task = Task.Run(() =>
            {
                try
                {
                    InvokeMethod(test.Method, instance, test.Parameters, isAsync);
                }
                catch (Exception ex)
                {
                    caught = ex;
                }
            }, cts.Token);

            bool completed = task.Wait(test.TimeoutMs.Value);

            if (!completed)
            {
                cts.Cancel();
                return new TestResult
                {
                    TestName = test.TestName,
                    Status = TestStatus.Timeout,
                    ErrorMessage = $"Test exceeded time limit of {test.TimeoutMs} ms"
                };
            }

            if (caught !=  null)
            {
                return new TestResult
                {
                    TestName = test.TestName,
                    Status = TestStatus.Failed,
                    ErrorMessage = caught.InnerException?.Message ?? caught.Message
                };
            }

            return new TestResult { TestName = test.TestName, Status = TestStatus.Passed };
        }

        private static void InvokeMethod(MethodInfo method, object instance, object[] parameters, bool isAsync)
        {
            if (isAsync)
            {
                var task = (Task)method.Invoke(instance, parameters);
                task.GetAwaiter().GetResult();
            }
            else
            {
                method.Invoke(instance, parameters);
            }
        }

        private static void TryInvokeCleanup(MethodInfo cleanupMethod, object instance, string testName)
        {
            try
            {
                cleanupMethod?.Invoke(instance, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] Cleanup for {testName}: {ex.InnerException?.Message ?? ex.Message}");
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