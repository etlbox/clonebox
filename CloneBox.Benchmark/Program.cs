using System.Globalization;
using System.Text;

namespace CloneBox.Benchmark {

    internal static class Config {
        public static readonly TimeSpan Budget = TimeSpan.FromSeconds(8);
        public static readonly TimeSpan WorkerTimeout = TimeSpan.FromSeconds(20);
    }

    internal static class Program {

        static void Main(string[] args) {
            Console.OutputEncoding = Encoding.UTF8;

            if (args.Length == 3 && args[0] == Measure.WorkerArg) {
                Measure.WorkerMain(
                    int.Parse(args[1], CultureInfo.InvariantCulture),
                    int.Parse(args[2], CultureInfo.InvariantCulture));
                return;
            }

            var libs = Contenders.All();
            var scenarios = Scenarios.All();

            Report.Banner();
            Report.Intro(libs);

            var results = new List<Result>();
            for (var s = 0; s < scenarios.Length; s++) {
                Report.ScenarioHead(scenarios[s]);
                for (var i = 0; i < libs.Length; i++) {
                    Report.Measuring(libs[i]);
                    var result = Measure.Run(libs[i], scenarios[s], i, s);
                    Report.Measured(result);
                    results.Add(result);
                }
                Console.WriteLine();
            }

            Report.Write(scenarios, results);
        }
    }
}
