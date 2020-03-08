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
            var changes = new Dictionary<char, List<string>>();
            foreach(var keyChar in "mtcn")    
                changes[keyChar] = new List<string>();
            var modeMap = new Dictionary<string, char> { ["<<<<<<<"] = 'm', ["======="] = 't', [">>>>>>>"] = 'c'};
            var curMode = 'c';
            var resolvedLines = (await File.ReadAllLinesAsync(file)).SelectMany(line =>
            {
                var prefix = line.Substring(0, Math.Min(line.Length, ">>>>>>>".Length));
                curMode = modeMap.ContainsKey(prefix)? modeMap[prefix] : curMode;
                if (!modeMap.ContainsKey(prefix)) 
                    changes[curMode].Add(line);
                else if (modeMap[prefix] == 'c')
                {
                    var newLines = ("c" + mode).SelectMany(ch => changes[ch]).ToArray();
                    foreach(var key in changes.Keys)    
                        changes[key].Clear();
                    return newLines;
                }
                return new string[0];
            }).ToArray();
            resolvedLines.AddRange(changes['c']);
            await File.WriteAllTextAsync(file, string.Join(Environment.NewLine, resolvedLines));
            return 0;
        }
    }
}