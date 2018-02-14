/*
    Copyright 2017,2018. Stefan Maierhofer.

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

        http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.
 */
using Aardvark.Base;
using System.IO;
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
