using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GitConflictResolver
{
    class Program
    {
        private const string Header = "<<<<<<<",  Separator = "=======", Footer = ">>>>>>>";
        private static readonly string[] ResolveModes = {"mt", "tm", "m", "t", "none"};
        static async Task<int> Main(string[] args)
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

            (string[] lines, string line, int i, var conflicts) = (await File.ReadAllLinesAsync(file), null, -1, new List<Conflict>());
            string GetNextLine() => ++i < lines.Length ? lines[i] : null;
            IEnumerable<string> TakeNextLineUntilStartsWith(string prefix)
            {
                while ((line = GetNextLine()) != null && !line.StartsWith(prefix, StringComparison.Ordinal))
                    yield return line;
            }
            for(var context = TakeNextLineUntilStartsWith(Header).ToList(); line != null;)
            {
                var mine = TakeNextLineUntilStartsWith(Separator).ToList();
                var theirs = TakeNextLineUntilStartsWith(Footer).ToList();
                conflicts.Add(new Conflict(mine, theirs, conflicts.Any()? null : context,
                    context = TakeNextLineUntilStartsWith(Header).ToList()));
            }
            if (!conflicts.Any())
            {
                Console.WriteLine($"There is no conflicts in file {file}");
                return 0;
            }
            var resolvedLines = conflicts.SelectMany(c => c.Resolve(mode)).ToArray();
            var text = string.Join(Environment.NewLine, resolvedLines);
            await File.WriteAllTextAsync(file, text);
            return 0;
        }
    }
    public class Conflict
    {
        public List<string> Before, Mine, Theirs, After;
        public Conflict(List<string> mine, List<string> theirs, List<string> before = null, List<string> after = null) =>
            (Before, Mine, Theirs, After) = (before?? new List<string>(), mine, theirs, after?? new List<string>());
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