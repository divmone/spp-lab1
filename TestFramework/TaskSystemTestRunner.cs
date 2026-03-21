using System.Diagnostics;
using System.Reflection;

namespace TestFramework
{
    public class TaskSystemTestRunner
    {
        private readonly TaskSystem _pool;

        public TaskSystemTestRunner(TaskSystem pool)
        {
            _pool = pool;
        }

        public List<TestResult> RunAll(Assembly assembly)
        {
            var allResults = new List<TestResult>();
            var resultsLock = new object();

            foreach (var type in assembly.GetTypes())
            {
                if (type.GetCustomAttribute<TestClassAttribute>() == null) continue;

                foreach (var result in RunClass(type))
                {
                    lock (resultsLock)
                        allResults.Add(result);
                }
            }

            return allResults;
        }

        public List<TestResult> RunClass(Type testClassType)
        {
            var results = new List<TestResult>();
            var resultsLock = new object();

            var methods = testClassType.GetMethods()
                .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null
                         || m.GetCustomAttribute<TestAsyncAttribute>() != null)
                .OrderByDescending(m => m.GetCustomAttribute<TestPriorityAttribute>()?.Priority ?? 0);

            foreach (var method in methods)
            {
                bool isIgnored = method.GetCustomAttribute<TestIgnoreAttribute>() != null;
                bool isAsync = method.GetCustomAttribute<TestAsyncAttribute>() != null;
                int? timeoutMs = method.GetCustomAttribute<TestTimeoutAttribute>()?.MillisecondsTimeout;
                var dataAttrs = method.GetCustomAttributes<TestDataAttribute>().ToArray();

                if (isIgnored)
                {
                    var ignored = new TestResult
                    {
                        TestName = $"{testClassType.Name}.{method.Name}",
                        Status = TestStatus.Ignored
                    };
                    lock (resultsLock) results.Add(ignored);
                    Console.WriteLine(ignored);
                    continue;
                }

                if (dataAttrs.Length > 0)
                {
                    int caseNum = 0;
                    foreach (var data in dataAttrs)
                    {
                        caseNum++;
                        string name = $"{testClassType.Name}.{method.Name} (Case {caseNum})";
                        var pars = data.Parametrs;

                        _pool.Async(() =>
                        {
                            var r = RunMethod(testClassType, method, pars, name, isAsync, timeoutMs);
                            Console.WriteLine(r);
                            lock (resultsLock) results.Add(r);
                        }, name);
                    }
                }
                else
                {
                    string name = $"{testClassType.Name}.{method.Name}";

                    _pool.Async(() =>
                    {
                        var r = RunMethod(testClassType, method, null, name, isAsync, timeoutMs);
                        Console.WriteLine(r);
                        lock (resultsLock) results.Add(r);
                    }, name);
                }
            }

            _pool.Wait();
            return results;
        }

        private static TestResult RunMethod(
            Type classType,
            MethodInfo method,
            object[]? parameters,
            string name,
            bool isAsync,
            int? timeoutMs)
        {
            object? instance;
            try { instance = Activator.CreateInstance(classType)!; }
            catch (Exception ex)
            {
                return new TestResult
                {
                    TestName = name,
                    Status = TestStatus.Failed,
                    ErrorMessage = "CreateInstance: " + ex.Message
                };
            }

            var initMethod = GetAttr<TestMethodInitAttribute>(classType);
            var cleanupMethod = GetAttr<TestMethodCleanupAttribute>(classType);

            try { initMethod?.Invoke(instance, null); }
            catch (Exception ex)
            {
                return new TestResult
                {
                    TestName = name,
                    Status = TestStatus.Failed,
                    ErrorMessage = "Init: " + (ex.InnerException?.Message ?? ex.Message)
                };
            }

            var sw = Stopwatch.StartNew();
            TestResult result;

            if (timeoutMs.HasValue)
                result = RunWithTimeout(method, instance, parameters, name, isAsync, timeoutMs.Value);
            else
                result = RunDirect(method, instance, parameters, name, isAsync);

            sw.Stop();
            result.ElapsedMs = sw.ElapsedMilliseconds;

            try { cleanupMethod?.Invoke(instance, null); } catch { }

            return result;
        }

        private static TestResult RunDirect(
            MethodInfo method, object instance, object[]? parameters,
            string name, bool isAsync)
        {
            try
            {
                if (isAsync)
                    ((Task)method.Invoke(instance, parameters)!).GetAwaiter().GetResult();
                else
                    method.Invoke(instance, parameters);

                return new TestResult { TestName = name, Status = TestStatus.Passed };
            }
            catch (Exception ex)
            {
                return new TestResult
                {
                    TestName = name,
                    Status = TestStatus.Failed,
                    ErrorMessage = ex.InnerException?.Message ?? ex.Message
                };
            }
        }

        private static TestResult RunWithTimeout(
            MethodInfo method, object instance, object[]? parameters,
            string name, bool isAsync, int timeoutMs)
        {
            Exception? caught = null;

            var t = new Thread(() =>
            {
                try
                {
                    if (isAsync)
                        ((Task)method.Invoke(instance, parameters)!).GetAwaiter().GetResult();
                    else
                        method.Invoke(instance, parameters);
                }
                catch (Exception ex) { caught = ex; }
            })
            { IsBackground = true };

            t.Start();

            if (!t.Join(timeoutMs))
            {
                try { t.Interrupt(); } catch { }
                return new TestResult
                {
                    TestName = name,
                    Status = TestStatus.Timeout,
                    ErrorMessage = $"Test exceeded time limit of {timeoutMs} ms"
                };
            }

            return caught != null
                ? new TestResult
                {
                    TestName = name,
                    Status = TestStatus.Failed,
                    ErrorMessage = caught.InnerException?.Message ?? caught.Message
                }
                : new TestResult { TestName = name, Status = TestStatus.Passed };
        }

        private static MethodInfo? GetAttr<T>(Type type) where T : Attribute
            => type.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<T>() != null);
    }
}   