using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GitConflictResolver
{
    class Program
    {
        private const string Header = "<<<<<<<", Separator = "=======", Footer = ">>>>>>>";
        private static readonly string[] ResolveModes = { "mt", "tm", "m", "t", "none" };
        static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine(@"Please provide enough parameters
Usage: GitConflictResolver [FILEPATH] [RESOLVEMODE]
FILEPATH: The path of the file to process
RESOLVEMODE: Indicate how to resolve the conflicted sections. Possible values: 
mt - Put mine before theirs
tm - Put theirs before mine
m -  Only keep mine
t - Only keep theirs
none - Keep none");
                return -1;
            }
            var (file, mode) = (args[0], args[1].ToLowerInvariant());
            if (!ResolveModes.Contains(mode))
            {
                Console.WriteLine($"Invalid resolve mode {mode}, possible values: {string.Join(",", ResolveModes)}");
                return -1;
            }

            var lines = File.ReadAllLines(file);
            var (conflicts, context) = (new List<Conflict>(), new List<string>());
            using (var walker = lines.OfType<string>().GetEnumerator())
            {
                string GetNextLine() => walker.MoveNext() ? walker.Current : null;
                IEnumerable<string> TakeNextLineUntilStartsWith(string prefix)
                {
                    string nextLine;
                    while ((nextLine = GetNextLine()) != null && !nextLine.StartsWith(prefix, StringComparison.Ordinal))
                        yield return nextLine;
                }
                string line;
                while ((line = GetNextLine()) != null)
                {
                    if (line.StartsWith(Header, StringComparison.Ordinal))
                    {
                        var mine = TakeNextLineUntilStartsWith(Separator).ToList();
                        var theirs = TakeNextLineUntilStartsWith(Footer).ToList();
                        conflicts.Add(new Conflict(mine, theirs, context));
                        context = new List<string>();
                    }
                    else
                        context.Add(line);
                }
            }

            if (!conflicts.Any())
            {
                Console.WriteLine($"There is no conflicts in file {file}");
                return 0;
            }

            conflicts.Last().After = context;
            var resolvedLines = conflicts.SelectMany(c => c.Resolve(mode)).ToArray();
            var text = string.Join(Environment.NewLine, resolvedLines);
            File.WriteAllText(file, text);
            return 0;
        }
    }
    public class Conflict
    {
        public List<string> Mine, Theirs, Before, After;
        public Conflict(List<string> mine, List<string> theirs, List<string> before) =>
            (Mine, Theirs, Before, After) = (mine, theirs, before, new List<string>());
        public IEnumerable<string> Resolve(string mode)
        {
            switch (mode)
            {
                case "mt": return Before.Concat(Mine).Concat(Theirs).Concat(After);
                case "tm": return Before.Concat(Theirs).Concat(Mine).Concat(After);
                case "m": return Before.Concat(Mine).Concat(After);
                case "t": return Before.Concat(Theirs).Concat(After);
                case "none": return Before.Concat(After).ToArray();
                default: throw new ApplicationException($"Invalid resolve mode {mode}");
            }
        }
    }
}