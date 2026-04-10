using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using TestFramework;
using ThreadPool;

class Program
{
    static void Main(string[] args)
    {
        string testAssemblyPath = Path.Combine(AppContext.BaseDirectory, "TestProject.dll");
        Assembly assembly = Assembly.LoadFrom(testAssemblyPath);

        PrintBanner("ЛР 4: Расширенное тестирование");

        // ═══════════════════════════════════════════════════════════════
        // 1. СОБЫТИЯ ПУЛА ПОТОКОВ
        // ═══════════════════════════════════════════════════════════════
        DemoPoolEvents(assembly);

        // ═══════════════════════════════════════════════════════════════
        // 2. ФИЛЬТРАЦИЯ ТЕСТОВ ДЕЛЕГАТАМИ
        // ═══════════════════════════════════════════════════════════════
        DemoFiltering(assembly);

        // ═══════════════════════════════════════════════════════════════
        // 3. ПАРАМЕТРИЗОВАННЫЕ ТЕСТЫ (yield return) + Assert.That
        // ═══════════════════════════════════════════════════════════════
        DemoYieldAndAssertThat(assembly);
    }

    static void DemoPoolEvents(Assembly assembly)
    {
        PrintSection("1. ПОДПИСКА НА СОБЫТИЯ ПУЛА ПОТОКОВ");

        int spawnCount = 0, completedCount = 0, failedCount = 0;

        using var pool = new TaskSystem(minThreads: 2, maxThreads: 8, idleTimeoutMs: 1500);

        pool.PoolStarted += (_, e) => Log(ConsoleColor.Green,
            $"[EVENT] PoolStarted    : {e.Message}");
        pool.ThreadSpawned += (_, e) => {
            spawnCount++;
            Log(ConsoleColor.Cyan, $"[EVENT] ThreadSpawned  : {e}");
        };
        pool.ThreadRemoved += (_, e) =>
            Log(ConsoleColor.Yellow, $"[EVENT] ThreadRemoved  : {e}");
        pool.TaskEnqueued += (_, e) =>
            Log(ConsoleColor.DarkGray, $"[EVENT] TaskEnqueued  : {e.Message}");
        pool.TaskStarted += (_, e) =>
            Log(ConsoleColor.DarkCyan, $"[EVENT] TaskStarted   : {e}");
        pool.TaskCompleted += (_, e) => {
            completedCount++;
            Log(ConsoleColor.Green, $"[EVENT] TaskCompleted  : {e}");
        };
        pool.TaskFailed += (_, e) => {
            failedCount++;
            Log(ConsoleColor.Red, $"[EVENT] TaskFailed     : {e}");
        };
        pool.ThreadHangDetected += (_, e) =>
            Log(ConsoleColor.Magenta, $"[EVENT] HangDetected   : {e}");
        pool.PoolDisposed += (_, e) =>
            Log(ConsoleColor.DarkYellow, $"[EVENT] PoolDisposed : {e.Message}");

        var taskRunner = new TaskSystemTestRunner(pool);
        taskRunner.Run(assembly);

        Console.WriteLine();
        Console.WriteLine($"  Событий ThreadSpawned  : {spawnCount}");
        Console.WriteLine($"  Задач завершено (событие): {completedCount}");
        Console.WriteLine($"  Задач с ошибкой (событие): {failedCount}");
    }

    static void DemoFiltering(Assembly assembly)
    {
        PrintSection("2. ФИЛЬТРАЦИЯ ТЕСТОВ ДЕЛЕГАТАМИ");

        RunFiltered(assembly, "Категория «Books»",
            TestFilter.ByCategory("Books"));

        RunFiltered(assembly, "Автор «Alice»",
            TestFilter.ByAuthor("Alice"));

        RunFiltered(assembly, "Автор «Bob» (generator-тесты)",
            TestFilter.ByAuthor("Bob"));

        RunFiltered(assembly, "Приоритет >= 9",
            TestFilter.ByMinPriority(9));

        RunFiltered(assembly, "Категория «AssertThat» И Автор «Alice»",
            TestFilter.And(
                TestFilter.ByCategory("AssertThat"),
                TestFilter.ByAuthor("Alice")));
    }

    static void RunFiltered(Assembly assembly, string title,
                            Func<MethodInfo, bool> filter)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"  ▶ Фильтр: {title}");
        Console.ResetColor();

        var runner = new TestRunner(filter);
        var sw = Stopwatch.StartNew();
        var results = runner.RunAll(assembly);
        sw.Stop();

        ResultsPrinter.PrintSummary(results, sw.ElapsedMilliseconds);
    }

    static void DemoYieldAndAssertThat(Assembly assembly)
    {
        PrintSection("3. ПАРАМЕТРИЗОВАННЫЕ ТЕСТЫ (yield return) + Assert.That");

        Console.WriteLine("  Все тесты без фильтра (включая generator и Assert.That):");
        Console.WriteLine();

        var runner = new TestRunner();
        var sw = Stopwatch.StartNew();
        var results = runner.RunAll(assembly);
        sw.Stop();

        ResultsPrinter.PrintSummary(results, sw.ElapsedMilliseconds);
    }

    static void PrintBanner(string title)
    {
        string line = new string('═', 70);
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine();
        Console.WriteLine(line);
        Console.WriteLine($"  {title}");
        Console.WriteLine(line);
        Console.ResetColor();
    }

    static void PrintSection(string title)
    {
        string line = new string('─', 70);
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine();
        Console.WriteLine(line);
        Console.WriteLine($"  {title}");
        Console.WriteLine(line);
        Console.ResetColor();
    }

    static void Log(ConsoleColor color, string msg)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(msg);
        Console.ResetColor();
    }
}