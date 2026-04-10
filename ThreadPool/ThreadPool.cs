using System.Collections.Concurrent;

namespace ThreadPool
{
    public class PoolEventArgs : EventArgs
    {
        public string Message { get; }
        public int ThreadId { get; }
        public DateTime OccuredAt { get; } = DateTime.UtcNow;

        public PoolEventArgs(string message, int threadId = -1)
        {
            Message = message;
            ThreadId = threadId;
        }

        public override string ToString() => ThreadId >= 0
            ? $"[{OccuredAt:HH:mm:ss.fff}] W{ThreadId}: {Message}"
            : $"[{OccuredAt:HH:mm:ss.fff}] {Message}";
    }

    public sealed class TaskSystem : IDisposable
    {

        public event EventHandler<PoolEventArgs>? PoolStarted;
        public event EventHandler<PoolEventArgs>? ThreadSpawned;
        public event EventHandler<PoolEventArgs>? ThreadRemoved;
        public event EventHandler<PoolEventArgs>? TaskEnqueued;
        public event EventHandler<PoolEventArgs>? TaskStarted;
        public event EventHandler<PoolEventArgs>? TaskCompleted;
        public event EventHandler<PoolEventArgs>? TaskFailed;
        public event EventHandler<PoolEventArgs>? ThreadHangDetected;
        public event EventHandler<PoolEventArgs>? PoolDisposed;

        private sealed class Worker
        {
            private static int _counter;
            public readonly int Id = Interlocked.Increment(ref _counter);
            public readonly Queue<Action> Tasks = new();
            public readonly object Lock = new();
            public volatile bool IsBusy = false;
            public string? CurrentTask = null;
            public DateTime TaskStarted = default;
        }

        private readonly int _minThreads;
        private readonly int _maxThreads;
        private readonly int _idleTimeoutMs;
        private readonly int _scaleUpQueueThreshold;
        private readonly int _scaleUpWaitMs;
        private readonly int _hangTimeoutMs;

        private readonly List<Worker> _workers = new();
        private readonly List<Thread> _threads = new();
        private readonly object _poolLock = new();
        private readonly Mutex _scaleMutex = new();

        private volatile bool _isQuit = false;
        private int _index = 0;
        private int _activeTasks = 0;
        private readonly object _activeTasksLock = new();

        private readonly Queue<(Action f, string name, DateTime enqueuedAt)> _globalQueue = new();
        private readonly object _globalQueueLock = new();

        private int _completedCount;
        private int _failedCount;

        public int ActiveThreads { get { lock (_poolLock) return _workers.Count; } }
        public int CompletedTasks => _completedCount;
        public int FailedTasks => _failedCount;

        private readonly Thread _monitorThread;

        public TaskSystem(
            int minThreads = 2,
            int maxThreads = 16,
            int idleTimeoutMs = 3000,
            int scaleUpQueueThreshold = 1,
            int scaleUpWaitMs = 50,
            int hangTimeoutMs = 10_000)
        {
            if (minThreads < 1) throw new ArgumentOutOfRangeException(nameof(minThreads));
            if (maxThreads < minThreads) throw new ArgumentOutOfRangeException(nameof(maxThreads));

            _minThreads = minThreads;
            _maxThreads = maxThreads;
            _idleTimeoutMs = idleTimeoutMs;
            _scaleUpQueueThreshold = scaleUpQueueThreshold;
            _scaleUpWaitMs = scaleUpWaitMs;
            _hangTimeoutMs = hangTimeoutMs;

            for (int i = 0; i < _minThreads; i++)
                SpawnWorker("init");

            _monitorThread = new Thread(MonitorLoop)
            {
                IsBackground = true,
                Name = "TaskSystem-Monitor"
            };
            _monitorThread.Start();

            Log($"[Pool] Started  min={_minThreads}  max={_maxThreads}  idle={_idleTimeoutMs}ms");
            PoolStarted?.Invoke(this,
                new PoolEventArgs($"Pool started. min={_minThreads} max={_maxThreads}"));
        }

        public void Async(Action f, string name = "Task")
        {
            ObjectDisposedException.ThrowIf(_isQuit, this);

            Interlocked.Increment(ref _activeTasks);

            lock (_globalQueueLock)
                _globalQueue.Enqueue((f, name, DateTime.UtcNow));

            TaskEnqueued?.Invoke(this,
                new PoolEventArgs($"Task '{name}' enqueued. Queue={_globalQueue.Count}"));

            Log($"++[Queue] +task '{name}'  queue={_globalQueue.Count}  active={_activeTasks}");

            int i = Interlocked.Increment(ref _index) - 1;
            Worker[]? snapshot;
            lock (_poolLock) snapshot = _workers.ToArray();

            int n = snapshot.Length;
            if (n > 0)
            {
                for (int j = i; j < n + i; j++)
                    if (TryPushToWorker(f, name, snapshot[(i + j) % n])) return;

                var w = snapshot[i % n];
                lock (w.Lock) w.Tasks.Enqueue(f);
                lock (w.Lock) Monitor.Pulse(w.Lock);
            }
        }

        public void Wait()
        {
            lock (_activeTasksLock)
                while (Volatile.Read(ref _activeTasks) != 0)
                    Monitor.Wait(_activeTasksLock);
        }

        private Worker SpawnWorker(string reason)
        {
            var w = new Worker();
            var t = new Thread(() => Run(w))
            {
                IsBackground = true,
                Name = $"TaskSystem-W{w.Id}"
            };
            lock (_poolLock) { _workers.Add(w); _threads.Add(t); }
            t.Start();
            Log($"[Pool] +thread {t.Name} ({reason})  count={_workers.Count}");
            ThreadSpawned?.Invoke(this,
                new PoolEventArgs($"Thread spawned ({reason}). Total={_workers.Count}", w.Id));
            return w;
        }

        private void TryScaleUp(string reason)
        {
            if (!_scaleMutex.WaitOne(0)) return;
            try
            {
                int count;
                lock (_poolLock) count = _workers.Count;
                if (count < _maxThreads) SpawnWorker(reason);
            }
            finally { _scaleMutex.ReleaseMutex(); }
        }

        private void RemoveWorker(Worker w)
        {
            lock (_poolLock)
            {
                _workers.Remove(w);
                Log($"[Pool] -thread W{w.Id}  count={_workers.Count}");
            }
            ThreadRemoved?.Invoke(this,
                new PoolEventArgs($"Thread removed. Remaining={_workers.Count}", w.Id));
        }

        private void Run(Worker self)
        {
            while (true)
            {
                Action? f = null;

                Worker[]? snapshot;
                lock (_poolLock) snapshot = _workers.ToArray();
                foreach (var w in snapshot)
                    if (TryPopFromWorker(out f, w)) break;

                if (f == null)
                {
                    lock (self.Lock)
                    {
                        bool signalled = Monitor.Wait(self.Lock, _idleTimeoutMs);

                        if (!signalled)
                        {
                            bool shouldExit = false;
                            lock (_poolLock)
                                if (_workers.Count > _minThreads)
                                {
                                    _workers.Remove(self);
                                    shouldExit = true;
                                    Log($"[Pool] -thread W{self.Id} (idle)  count={_workers.Count}");
                                    ThreadRemoved?.Invoke(this,
                                        new PoolEventArgs(
                                            $"Thread idle-timeout removed. Remaining={_workers.Count}",
                                            self.Id));
                                }
                            if (shouldExit) return;
                            continue;
                        }

                        if (_isQuit && self.Tasks.Count == 0) return;
                        if (self.Tasks.Count == 0) continue;

                        f = self.Tasks.Dequeue();
                    }
                }

                lock (_globalQueueLock)
                    if (_globalQueue.Count > 0) _globalQueue.Dequeue();

                self.IsBusy = true;
                self.CurrentTask = "task";
                self.TaskStarted = DateTime.UtcNow;

                TaskStarted?.Invoke(this,
                    new PoolEventArgs($"Task started", self.Id));

                try
                {
                    f!();
                    Interlocked.Increment(ref _completedCount);
                    TaskCompleted?.Invoke(this,
                        new PoolEventArgs($"Task completed", self.Id));
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref _failedCount);
                    Log($"[Worker W{self.Id}] Error: {ex.Message}");
                    TaskFailed?.Invoke(this,
                        new PoolEventArgs($"Task FAILED: {ex.Message}", self.Id));
                }
                finally
                {
                    self.IsBusy = false;
                    self.CurrentTask = null;
                }

                if (Interlocked.Decrement(ref _activeTasks) == 0)
                    lock (_activeTasksLock)
                        Monitor.PulseAll(_activeTasksLock);
            }
        }

        private bool TryPopFromWorker(out Action? f, Worker w)
        {
            f = null;
            if (!Monitor.TryEnter(w.Lock)) return false;
            try
            {
                if (w.Tasks.Count == 0) return false;
                f = w.Tasks.Dequeue();
                return true;
            }
            finally { Monitor.Exit(w.Lock); }
        }

        private bool TryPushToWorker(Action f, string name, Worker w)
        {
            if (!Monitor.TryEnter(w.Lock)) return false;
            try { w.Tasks.Enqueue(f); }
            finally { Monitor.Exit(w.Lock); }
            lock (w.Lock) Monitor.Pulse(w.Lock);
            return true;
        }

        private void MonitorLoop()
        {
            while (!_isQuit)
            {
                Thread.Sleep(100);
                if (_isQuit) break;

                int qLen, active, busy;
                lock (_globalQueueLock) qLen = _globalQueue.Count;
                lock (_poolLock) { active = _workers.Count; busy = BusyCount(); }

                if (qLen >= _scaleUpQueueThreshold)
                    TryScaleUp($"queue={qLen}");

                (Action f, string name, DateTime enqueuedAt) oldest = default;
                lock (_globalQueueLock)
                    if (_globalQueue.Count > 0) oldest = _globalQueue.Peek();

                if (oldest != default)
                {
                    double waitMs = (DateTime.UtcNow - oldest.enqueuedAt).TotalMilliseconds;
                    if (waitMs >= _scaleUpWaitMs)
                        TryScaleUp($"wait={waitMs:F0}ms");
                }

                List<Worker>? hung = null;
                lock (_poolLock)
                    foreach (var w in _workers)
                    {
                        if (!w.IsBusy || w.TaskStarted == default) continue;
                        double runMs = (DateTime.UtcNow - w.TaskStarted).TotalMilliseconds;
                        if (runMs > _hangTimeoutMs) { hung ??= new(); hung.Add(w); }
                    }

                if (hung != null)
                    foreach (var w in hung)
                    {
                        Log($"[Pool] Hung thread W{w.Id} detected — replacing");
                        ThreadHangDetected?.Invoke(this,
                            new PoolEventArgs("Hung thread detected!", w.Id));
                        RemoveWorker(w);
                        SpawnWorker("hang-replace");
                    }

                Console.WriteLine(
                    $"  [Monitor] threads={active} (busy={busy}|idle={active - busy})" +
                    $"  queue={qLen}  completed={_completedCount}  errors={_failedCount}");
            }
        }

        private int BusyCount()
        {
            int n = 0;
            foreach (var w in _workers) if (w.IsBusy) n++;
            return n;
        }

        public void Dispose()
        {
            _isQuit = true;

            Worker[] snapshot;
            lock (_poolLock) snapshot = _workers.ToArray();
            foreach (var w in snapshot)
                lock (w.Lock) Monitor.PulseAll(w.Lock);

            Thread[] tSnapshot;
            lock (_poolLock) tSnapshot = _threads.ToArray();
            foreach (var t in tSnapshot)
                t.Join(500);

            _scaleMutex.Dispose();
            Log("[Pool] Disposed.");
            PoolDisposed?.Invoke(this, new PoolEventArgs("Pool disposed."));
        }

        private static void Log(string msg)
            => Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {msg}");
    }
}