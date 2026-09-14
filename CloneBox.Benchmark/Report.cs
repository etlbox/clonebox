namespace CloneBox.Benchmark {

    internal static class Report {

        public static void Banner() {
            const string title = "CloneBox Benchmark  \u00b7  four graphs compared";
            C(ConsoleColor.Cyan, $"\u2554{new string('\u2550', title.Length + 2)}\u2557");
            C(ConsoleColor.Cyan, $"\u2551 {title} \u2551");
            C(ConsoleColor.Cyan, $"\u255a{new string('\u2550', title.Length + 2)}\u255d");
            Console.WriteLine();
        }

        public static void Intro(IReadOnlyList<Contender> all) {
            Head("Libraries");
            var w = all.Max(c => c.Name.Length);
            foreach (var c in all) {
                Console.Write("  ");
                Ink(c.IsCloneBox ? ConsoleColor.Yellow : ConsoleColor.White, c.Name.PadRight(w));
                Console.Write("  ");
                Ink(ConsoleColor.Gray, Downloads(c.Downloads).PadLeft(8));
                Ink(c.IsCloner ? ConsoleColor.DarkCyan : ConsoleColor.DarkYellow, "  " + c.Kind.ToString().PadRight(10));
                C(ConsoleColor.DarkGray, "  " + c.Note);
            }
            Console.WriteLine();
        }

        public static void ScenarioHead(Scenario s) {
            Head(s.Title);
            C(ConsoleColor.DarkGray, "  " + s.Blurb);
        }

        public static void Measuring(Contender c) {
            Console.Write("  " + c.Name.PadRight(18));
            Console.Out.Flush();
        }

        public static void Measured(Result r) {
            if (r.Error != null) C(ConsoleColor.Red, Short(r.Error, 52));
            else if (r.FullClone) C(ConsoleColor.DarkGray, Time(r));
            else C(ConsoleColor.DarkYellow, Time(r) + "  " + Short(r.FailReason, 36));
        }

        public static void Write(IReadOnlyList<Scenario> scenarios, IReadOnlyList<Result> results) {
            Console.WriteLine();
            Head("Results");
            var libs = results.Select(r => r.Contender).Distinct().ToList();

            Ink(ConsoleColor.DarkGray, $"  {"Library",-16}  {"simple",9}  {"cyclic",9}  {"dynamic",9}  {"into",9}");
            Console.WriteLine();

            foreach (var c in libs) {
                var rows = scenarios.Select(s => results.First(r => r.Contender.Name == c.Name && r.Scenario.Id == s.Id)).ToList();
                var n = rows.Count(r => r.FullClone);
                Console.Write("  ");
                Ink(c.IsCloneBox ? ConsoleColor.Yellow : ConsoleColor.White, c.Name.PadRight(16));
                foreach (var r in rows) {
                    Console.Write("  ");
                    Ink(r.FullClone ? (c.IsCloneBox ? ConsoleColor.Yellow : ConsoleColor.Gray) : ConsoleColor.Red,
                        Cell(r).PadLeft(9));
                }
                Ink(n == scenarios.Count ? ConsoleColor.Yellow : ConsoleColor.DarkGray, $"   {n}/{scenarios.Count}");
                var note = Note(c, rows);
                if (note.Length > 0) C(ConsoleColor.DarkGray, "  " + note);
                else Console.WriteLine();
            }

            Console.WriteLine();
            C(ConsoleColor.Yellow, "  CloneBox is the only library that completes all four graphs.");
            C(ConsoleColor.DarkGray, "  DeepCloner still clones same-type objects (including Expando), but DeepCloneTo requires inheritance \u2014 it cannot copy an Expando into an existing DTO.");            
            Console.WriteLine();
        }

        static string Cell(Result r) => r.FullClone ? Time(r) : "NO";

        static string Time(Result r)
            => r.MsPerClone < 0.1 ? $"{r.MsPerClone * 1000:N2} \u00b5s" : $"{r.MsPerClone:N2} ms";

        static string Note(Contender c, List<Result> rows) {
            if (c.IsCloneBox) return "CloneXTo + [DoNotClone]";
            if (c.Name == "DeepCloner") return "no clone-into for unrelated types";
            if (!c.IsCloner) return c.Note;
            var fail = rows.FirstOrDefault(r => !r.FullClone);
            return fail == null ? "" : Short(fail.FailReason, 36);
        }

        static string Downloads(long n) => n < 0 ? "this lib" : n >= 1_000_000_000 ? $"{n / 1_000_000_000d:N2}B" : $"{n / 1_000_000d:N1}M";

        static string Short(string t, int max) => t.Length <= max ? t : t[..(max - 1)] + "\u2026";

        static void Head(string t) { C(ConsoleColor.White, t); C(ConsoleColor.DarkGray, new string('\u2500', t.Length)); }
        static void C(ConsoleColor c, string t) { Ink(c, t); Console.WriteLine(); }
        static void Ink(ConsoleColor c, string t) {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = c;
            Console.Write(t);
            Console.ForegroundColor = prev;
        }
    }
}
