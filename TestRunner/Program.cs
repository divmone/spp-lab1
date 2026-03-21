using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using TestFramework;

class Program
{
    static void Main(string[] args)
    {
        string testAssemblyPath = Path.Combine(AppContext.BaseDirectory, "TestProject.dll");

        int maxDop = 4;
        if (args.Length > 1 && int.TryParse(args[1], out int parsedDop))
            maxDop = parsedDop;

        Assembly assembly = Assembly.LoadFrom(testAssemblyPath);

        RunTests(assembly, true, maxDop, out long parallelMs);
        RunTests(assembly, false, maxDop, out long sequentialMs);
        ResultsPrinter.PrintBenchmarkComparison(sequentialMs, parallelMs);

        RunWithTaskSystem(assembly);
    }

    static void RunTests(Assembly assembly, bool isParallel, int maxDop, out long elapsedMs)
    {
        string line = new string('═', 60);

        if (isParallel)
        {
            Console.WriteLine(line);
            Console.WriteLine($"  PARALLEL RUN  (MaxDOP = {maxDop})");
            Console.WriteLine(line);
            Console.WriteLine();

            var runner = new ParallelTestRunner(maxDop);
            var watch = Stopwatch.StartNew();
            var results = runner.RunAll(assembly);
            watch.Stop();

            elapsedMs = watch.ElapsedMilliseconds;
            ResultsPrinter.PrintSummary(results, elapsedMs);
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine(line);
            Console.WriteLine("  SEQUENTIAL RUN");
            Console.WriteLine(line);
            Console.WriteLine();

            var runner = new TestRunner();
            var watch = Stopwatch.StartNew();
            var results = runner.RunAll(assembly);
            watch.Stop();

            elapsedMs = watch.ElapsedMilliseconds;
            ResultsPrinter.PrintSummary(results, elapsedMs);
        }
    }

    static void RunWithTaskSystem(Assembly assembly)
    {
        string line = new string('═', 60);
        Console.WriteLine();
        Console.WriteLine(line);
        Console.WriteLine("  DYNAMIC THREAD POOL RUN (TaskSystem)");
        Console.WriteLine(line);
        Console.WriteLine();

        using var pool = new TaskSystem(
            minThreads: 2,
            maxThreads: 16,
            idleTimeoutMs: 2_000,
            scaleUpQueueThreshold: 3,
            scaleUpWaitMs: 400,
            hangTimeoutMs: 8_000);

        var runner = new TaskSystemTestRunner(pool);
        var allResults = new List<TestResult>();
        var rnd = new Random(42);
        var sw = Stopwatch.StartNew();

        Banner("Scenario 1: Idle (1 sec) -> Peak load");
        Thread.Sleep(1_000);
        allResults.AddRange(runner.RunAll(assembly));

        Banner("Scenario 2: Single submissions (interval 100-350 ms)");
        var tests = CollectTests(assembly);
        for (int i = 0; i < 12; i++)
        {
            var tc = tests[rnd.Next(tests.Count)];
            pool.Async(() =>
            {
                var r = RunSingle(tc);
                lock (allResults) allResults.Add(r);
                Console.WriteLine(r);
            }, tc.Name);
            Thread.Sleep(rnd.Next(100, 350));
        }
        pool.Wait();

        Banner("Scenario 3: Second peak load");
        allResults.AddRange(runner.RunAll(assembly));

        sw.Stop();

        Console.WriteLine($"\n  Total tasks run: {allResults.Count}");
        ResultsPrinter.PrintSummary(allResults, sw.ElapsedMilliseconds);
    }

    record TestCase(Type ClassType, MethodInfo Method, string Name,
                bool IsAsync, object[]? Params, int? TimeoutMs);

    static List<TestCase> CollectTests(Assembly asm)
    {
        var list = new List<TestCase>();
        foreach (var type in asm.GetTypes())
        {
            if (type.GetCustomAttribute<TestClassAttribute>() == null) continue;
            foreach (var m in type.GetMethods())
            {
                bool isMethod = m.GetCustomAttribute<TestMethodAttribute>() != null;
                bool isAsync = m.GetCustomAttribute<TestAsyncAttribute>() != null;
                if (!isMethod && !isAsync) continue;
                if (m.GetCustomAttribute<TestIgnoreAttribute>() != null) continue;

                int? timeout = m.GetCustomAttribute<TestTimeoutAttribute>()?.MillisecondsTimeout;
                var datas = m.GetCustomAttributes<TestDataAttribute>().ToArray();

                if (datas.Length > 0)
                {
                    int n = 0;
                    foreach (var d in datas)
                        list.Add(new TestCase(type, m,
                            $"{type.Name}.{m.Name} (Case {++n})", isAsync, d.Parametrs, timeout));
                }
                else
                    list.Add(new TestCase(type, m,
                        $"{type.Name}.{m.Name}", isAsync, null, timeout));
            }
        }
        return list;
    }

    static TestResult RunSingle(TestCase tc)
    {
        object? inst;
        try { inst = Activator.CreateInstance(tc.ClassType)!; }
        catch (Exception ex) { return new TestResult { TestName = tc.Name, Status = TestStatus.Failed, ErrorMessage = ex.Message }; }

        var initM = tc.ClassType.GetMethods()
            .FirstOrDefault(m => m.GetCustomAttribute<TestMethodInitAttribute>() != null);
        try { initM?.Invoke(inst, null); } catch { }

        var sw = Stopwatch.StartNew();
        TestResult result;
        try
        {
            if (tc.IsAsync)
                ((Task)tc.Method.Invoke(inst, tc.Params)!).GetAwaiter().GetResult();
            else
                tc.Method.Invoke(inst, tc.Params);
            result = new TestResult { TestName = tc.Name, Status = TestStatus.Passed };
        }
        catch (Exception ex)
        {
            result = new TestResult
            {
                TestName = tc.Name,
                Status = TestStatus.Failed,
                ErrorMessage = ex.InnerException?.Message ?? ex.Message
            };
        }
        sw.Stop();
        result.ElapsedMs = sw.ElapsedMilliseconds;

        var cleanM = tc.ClassType.GetMethods()
            .FirstOrDefault(m => m.GetCustomAttribute<TestMethodCleanupAttribute>() != null);
        try { cleanM?.Invoke(inst, null); } catch { }

        return result;
    }

    static void Banner(string text)
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"\n  == {text} ==");
        Console.ResetColor();
    }

}