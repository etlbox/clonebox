using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace CloneBox.Benchmark {

    internal static class Measure {

        public const string WorkerArg = "--worker";
        const string Ok = "##OK";
        const string Err = "##ERR";
        const int StackOverflow = unchecked((int)0xC00000FD);

        public static Result Run(Contender contender, Scenario scenario, int contenderIndex, int scenarioIndex) {
            using var worker = Process.Start(StartInfo(contenderIndex, scenarioIndex));
            if (worker == null)
                return Fail(contender, scenario, "worker did not start");

            var stdout = worker.StandardOutput.ReadToEndAsync();
            var stderr = worker.StandardError.ReadToEndAsync();

            if (!worker.WaitForExit((int)Config.WorkerTimeout.TotalMilliseconds)) {
                TryKill(worker);
                return Fail(contender, scenario, $"timed out after {Config.WorkerTimeout.TotalSeconds:N0}s");
            }

            Drain(stdout);
            Drain(stderr);

            var payload = stdout.Result.Split('\n')
                .Select(l => l.TrimEnd('\r'))
                .LastOrDefault(l => l.StartsWith("##", StringComparison.Ordinal));

            if (payload == null)
                return Fail(contender, scenario, worker.ExitCode == StackOverflow
                    ? "stack overflow"
                    : $"exited 0x{worker.ExitCode:X8}");

            return Parse(contender, scenario, payload);
        }

        public static void WorkerMain(int contenderIndex, int scenarioIndex) {
            var contender = Contenders.All()[contenderIndex];
            var scenario = Scenarios.All()[scenarioIndex];
            var pool = new object[scenario.Pool];
            for (var i = 0; i < pool.Length; i++)
                pool[i] = scenario.Create(i);

            Console.Out.WriteLine(Serialize(RunInProcess(contender, scenario, pool)));
            Console.Out.Flush();
        }

        static Result RunInProcess(Contender contender, Scenario scenario, object[] pool) {
            string[] issues;
            try {
                contender.Clone(pool[0]);
                issues = scenario.Inspect(contender.Clone(pool[0]), pool[0]);
            } catch (Exception ex) {
                return Fail(contender, scenario, Describe(ex));
            }

            var ring = new object?[64];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var before = GC.GetAllocatedBytesForCurrentThread();
            var n = 0;
            var sw = Stopwatch.StartNew();
            try {
                while (n < scenario.Clones && sw.Elapsed < Config.Budget)
                    ring[n % ring.Length] = contender.Clone(pool[n++ % pool.Length]);
            } catch (Exception ex) {
                return Fail(contender, scenario, Describe(ex));
            }
            sw.Stop();
            GC.KeepAlive(ring);

            return new Result {
                Contender = contender,
                Scenario = scenario,
                Elapsed = sw.Elapsed,
                Clones = n,
                Allocated = GC.GetAllocatedBytesForCurrentThread() - before,
                Issues = issues
            };
        }

        static ProcessStartInfo StartInfo(int contenderIndex, int scenarioIndex) {
            var host = Environment.ProcessPath!;
            var psi = new ProcessStartInfo {
                FileName = host,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            if (Path.GetFileNameWithoutExtension(host).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
                psi.ArgumentList.Add(Assembly.GetEntryAssembly()!.Location);
            psi.ArgumentList.Add(WorkerArg);
            psi.ArgumentList.Add(contenderIndex.ToString(CultureInfo.InvariantCulture));
            psi.ArgumentList.Add(scenarioIndex.ToString(CultureInfo.InvariantCulture));
            return psi;
        }

        static string Serialize(Result r) {
            if (r.Error != null)
                return $"{Err}|{r.Error.Replace('|', '/')}";
            return string.Join('|', Ok,
                r.Elapsed.Ticks.ToString(CultureInfo.InvariantCulture),
                r.Clones.ToString(CultureInfo.InvariantCulture),
                r.Allocated.ToString(CultureInfo.InvariantCulture),
                string.Join('~', r.Issues).Replace('|', '/'));
        }

        static Result Parse(Contender c, Scenario s, string line) {
            var p = line.Split('|');
            if (p[0] == Err)
                return Fail(c, s, p.Length > 1 ? p[1] : "unknown error");
            if (p[0] != Ok || p.Length < 5)
                return Fail(c, s, "unreadable worker result");
            return new Result {
                Contender = c,
                Scenario = s,
                Elapsed = TimeSpan.FromTicks(long.Parse(p[1], CultureInfo.InvariantCulture)),
                Clones = int.Parse(p[2], CultureInfo.InvariantCulture),
                Allocated = long.Parse(p[3], CultureInfo.InvariantCulture),
                Issues = string.IsNullOrEmpty(p[4]) ? [] : p[4].Split('~')
            };
        }

        static Result Fail(Contender c, Scenario s, string error)
            => new() { Contender = c, Scenario = s, Error = error };

        static void TryKill(Process p) {
            try { if (!p.HasExited) p.Kill(entireProcessTree: true); } catch { }
            try { p.WaitForExit(3000); } catch { }
        }

        static void Drain(Task<string> io) {
            try { _ = io.Result; } catch { }
        }

        static string Describe(Exception ex) {
            while (ex.InnerException != null) ex = ex.InnerException;
            var msg = ex.Message.Split('\n')[0].Trim();
            if (msg.Length > 56) msg = msg[..55] + "\u2026";
            return $"{ex.GetType().Name}: {msg}";
        }
    }
}
