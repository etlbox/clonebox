using Force.DeepCloner;
using System.Diagnostics;

namespace CloneBox.Benchmark {
    internal class Program {

        private const int TestObjects = 200;

        static void Main(string[] args) {
            var results = new Dictionary<string, TimeSpan>();
            Console.WriteLine("CloneBox Benchmark");
            var timer = new Stopwatch();

            Console.WriteLine($"Creation of test objects started.");

            timer.Start();
            var origList = new BenchmarkObject[TestObjects];
            var clonedListCloneBox = new BenchmarkObject[TestObjects];
            var clonedListDeepClone = new BenchmarkObject[TestObjects];

            for (int i=0;i<TestObjects;i++) {
                origList[i] = BenchmarkObject.CreateTestObject(i);
            }
            timer.Stop();
            Console.WriteLine($"Creation of test objects took {timer.Elapsed}");

            Console.WriteLine($"Measuring CloneBox.Clone() X {TestObjects:N0}...");
            timer.Restart();
            for (var i = 0; i < TestObjects; i++) {
                clonedListCloneBox[i] = origList[i].CloneX(new CloneSettings() {
                    IncludeNonPublicFields = false,
                    IncludeNonPublicProperties = false,
                    IncludeNonPublicConstructors = false
                });
            }
            timer.Stop();
            results.Add("CloneBox", timer.Elapsed);
            Console.WriteLine($"CloneBox took {timer.Elapsed}");
            

            Console.WriteLine($"Measuring DeepCloner.DeepClone() X {TestObjects:N0}...");
            timer.Restart();
            for (var i = 0; i < TestObjects; i++) {
                clonedListDeepClone[i] = origList[i].DeepClone();
            }
            timer.Stop();
            results.Add("DeepCloner", timer.Elapsed);
            Console.WriteLine($"DeepCloner took {timer.Elapsed}");

            Console.WriteLine($"\r\nFinished performance tests!\r\n");
            Console.WriteLine($"Results: \r\n");

            var resultNumber = 1;
            foreach (var result in results.OrderBy(x => x.Value)) {
                Console.WriteLine($"#{resultNumber}: {result.Key} took {result.Value}, {(result.Value.TotalMilliseconds / TestObjects):N2}ms per clone operation");
                resultNumber++;
            }

        }
    }
}
