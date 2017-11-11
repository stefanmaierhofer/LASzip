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
            WriteLine("Example");

            var totalPointCount = 0L;
            var bounds = Box3d.Invalid;
            var bounds2 = Box3d.Invalid;

            foreach (var filename in Directory.EnumerateFiles(@"T:\Visdom", "*.laz", SearchOption.AllDirectories))
            {
                var info = LASZip.Parser.ReadInfo(filename);
                totalPointCount += info.Count;
                bounds.ExtendBy(info.Bounds);

                foreach (var ps in LASZip.Parser.ReadPoints(filename, 100))
                {
                    bounds2.ExtendBy(new Box3d(ps.Positions));
                }


                WriteLine($"{Path.GetFileName(filename),-40}{info.Count,20:N0}");
            }
            WriteLine($"total point count: {totalPointCount:N0}");
            WriteLine($"bounds           : {bounds}");
            WriteLine($"bounds2          : {bounds2}");
        }
    }
}
