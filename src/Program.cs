using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
namespace GitConflictResolver
{
    class Program
    {
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
            if (!new []{"mt", "tm", "m", "t", "none"}.Contains(mode))
            {
                Console.WriteLine($"Invalid resolve mode {mode}");
                return -1;
            }
            var (changes, curMode) = (new Dictionary<char, List<string>> { ['m'] = new List<string>(), ['t'] = new List<string>() }, ' ');
            var resolvedLines = (await File.ReadAllLinesAsync(file)).SelectMany(line =>
            {
                (var newLines, var prefix) = (new List<string>(), line.Substring(0, Math.Min(line.Length, ">>>>>>>".Length)));
                curMode = prefix == "<<<<<<<"? 'm' : prefix == "======="? 't' : curMode;
                if (prefix == ">>>>>>>")
                    (newLines, changes['m'], changes['t']) = (mode.SelectMany(c => changes.ContainsKey(c) ? changes[c] : new List<string>()).ToList(), 
                                new List<string>(), new List<string>());
                else if (prefix != "<<<<<<<" && prefix != "=======")
                    (changes.ContainsKey(curMode)? changes[curMode] : newLines).Add(line);
                return newLines;
            }).ToArray();
            await File.WriteAllTextAsync(file, string.Join(Environment.NewLine, resolvedLines));
            return 0;
        }
    }
}