using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aardvark.Base;
using static System.Console;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            var dirname = ".";
            if (args.Length == 1)
            {
                dirname = args[0];
            }
            else
            {
                WriteLine("Please supply a directory name on the command line which contains .las or .laz files.");
            }

            WriteLine($"{dirname}:");

            var totalPointCount = 0L;
            var bounds = Box3d.Invalid;
            var bounds2 = Box3d.Invalid;

            foreach (var filename in Directory.EnumerateFiles(dirname, "*.laz", SearchOption.AllDirectories))
            {
                var info = LASZip.Parser.ReadInfo(filename);
                totalPointCount += info.Count;
                bounds.ExtendBy(info.Bounds);

                WriteLine($"{Path.GetFileName(filename),-40}{info.Count,20:N0}");
                foreach (var ps in LASZip.Parser.ReadPoints(filename, 1024*1024))
                {
                    bounds2.ExtendBy(new Box3d(ps.Positions));
                    WriteLine($"  chunk {ps.Count,20:N0}");
                }
            }
            WriteLine($"total point count: {totalPointCount:N0}");
            WriteLine($"bounds           : {bounds}");
            WriteLine($"bounds2          : {bounds2}");
        }
    }
}
