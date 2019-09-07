using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GitConflictResolver
{
    class Program
    {
        private const string ConflictHeader = "<<<<<<<", 
                             ConflictSeparator = "=======", 
                             ConflictFooter = ">>>>>>>";
        private static readonly string[] ResolveModes = {"mt", "tm", "m", "t", "none"};
        static async Task<int> Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Please provide enough parameters");
                Console.Error.WriteLine(@"Usage: GitConflictResolver FILEPATH [RESOLVEMODE]
FILEPATH: The path of the file to process
RESOLVEMODE: Indicate how to resolve the conflicted sections. Possible values: 
mt - Put mine before theirs
tm - Put theirs before mine
m -  Only keep mine
t - Only keep theirs
none - Keep none");
                return -1;
            }

            var file = args[0];
            var mode = args[1].ToLowerInvariant();
            if (!ResolveModes.Contains(mode))
            {
                Console.WriteLine($"Invalid resolve mode {mode}, possible values: mt, tm, m, t, none");
            }

            var lines = await File.ReadAllLinesAsync(file);
            var conflicts = new List<Conflict>();
            var context = new List<string>();
            using (var walker = ((IEnumerable<string>) lines).GetEnumerator())
            {
                string GetNextLine()
                {
                    walker.MoveNext();
                    return walker.Current;
                }
                while (walker.MoveNext())
                {
                    var line = walker.Current;
                    if (line.StartsWith(ConflictHeader, StringComparison.Ordinal))
                    {
                        var mine = new List<string>();
                        walker.MoveNext();
                        line = walker.Current;
                        while (!line.StartsWith(ConflictSeparator, StringComparison.Ordinal))
                        {
                            mine.Add(line);
                            line = GetNextLine();
                        }

                        var theirs = new List<string>();
                        line = GetNextLine();
                        while (!line.StartsWith(ConflictFooter, StringComparison.Ordinal))
                        {
                            theirs.Add(line);
                            line = GetNextLine();
                        }

                        conflicts.Add(new Conflict(mine, theirs, context));
                        context = new List<string>();
                    }
                    else
                    {
                        context.Add(line);
                    }
                }
            }
            
            if (!conflicts.Any())
            {
                Console.WriteLine($"There is no conflicts in file {file}");
                return 0;
            }

            var lastConflict = conflicts.Last();
            lastConflict.After = context;

            var resolvedLines = conflicts.SelectMany(c => c.Resolve(mode)).ToArray();
            
            var text = string.Join(Environment.NewLine, resolvedLines);
            await File.WriteAllTextAsync(file, text);

            return 0;
        }
    }

    public class Conflict
    {
        public List<string> Mine, Theirs, Before, After;

        public Conflict(List<string> mine,
                        List<string> theirs,
                        List<string> before)
        {
            Mine = mine;
            Theirs = theirs;
            Before = before;
            After = new List<string>();
        }

        public string[] Resolve(string mode)
        {
            switch(mode)
            {
                case "mt":
                    return Before
                        .Concat(Mine)
                        .Concat(Theirs)
                        .Concat(After)
                        .ToArray();
                case "tm":
                    return Before
                        .Concat(Theirs)
                        .Concat(Mine)
                        .Concat(After)
                        .ToArray();
                case "m":
                    return Before
                        .Concat(Mine)
                        .Concat(After)
                        .ToArray();
                case "t":
                    return Before
                        .Concat(Theirs)
                        .Concat(After)
                        .ToArray();
                case "none":
                    return Before
                        .Concat(After)
                        .ToArray();
                default:
                    throw new ApplicationException($"Invalid resolve mode {mode}");
            }
        }
    }
}
