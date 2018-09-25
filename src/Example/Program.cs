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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using static System.Console;

namespace Example
{
    class Program
    {
        private static void Test()
        {
            var metaFileName = Path.GetFullPath(@"meta.json");
            var meta = JArray
                .Parse(File.ReadAllText(metaFileName))
                .Select(x => (
                    filename: (string)x["FileName"],
                    count: (long)x["Count"],
                    bounds: Box3d.Parse((string)x["Bounds"])
                ))
                .ToArray()
                ;
            var area = meta.Sum(x => x.bounds.Size.X * x.bounds.Size.Y) / 1000000.0;
            WriteLine($"area: {area:N3} km²");

            var bbTotal = new Box3d(meta.Select(x => x.bounds));
            WriteLine($"bounds: {bbTotal}");
        }

        static void Main(string[] args)
        {
            Test(); return;

            var dirname = ".";
            if (args.Length == 1)
            {
                dirname = args[0];
            }
            else
            {
                WriteLine("Please supply a directory name on the command line which contains .las or .laz files.");
            }

            dirname = Path.GetFullPath(dirname);
            WriteLine($"{dirname}:");

            var totalPointCount = 0L;
            var bounds = Box3d.Invalid;
            var bounds2 = Box3d.Invalid;

            var meta = new List<JObject>();
            var sw = new Stopwatch(); sw.Start();
            var i = 1;
            foreach (var filename in Directory.EnumerateFiles(dirname, "*.laz", SearchOption.AllDirectories))
            {
                var info = LASZip.Parser.ReadInfo(filename);
                totalPointCount += info.Count;
                bounds.ExtendBy(info.Bounds);
                var cell = new Cell(info.Bounds);

                var relativeFilename = filename.Substring(dirname.Length + 1).Replace('\\', '/');
                meta.Add(JObject.FromObject(new
                {
                    FileName = relativeFilename,
                    info.Count,
                    Bounds = info.Bounds.ToString(),
                    Cell = cell.ToString()
                }));
                WriteLine($"[{i++,7}] {relativeFilename,-40} {info.Count,20:N0} {totalPointCount,20:N0}  {cell,10:0.00}");
                //foreach (var ps in LASZip.Parser.ReadPoints(filename, 1024 * 1024))
                //{
                //    var bb = new Box3d(ps.Positions);
                //    bounds2.ExtendBy(bb);
                //    //WriteLine($"  chunk {ps.Count,20:N0} {bb,20}");
                //    //WriteLine($"        {"",20:N0} {new Cell(bb),20}");
                //}

                //if (totalPointCount > 100000000) break;
            }
            sw.Stop();
            WriteLine($"total point count: {totalPointCount:N0}");
            WriteLine($"bounds           : {bounds}");
            WriteLine($"bounds2          : {bounds2}");
            WriteLine($"{sw.Elapsed}");

            var metaFileName = Path.GetFullPath(@"meta.json");
            var json = JsonConvert.SerializeObject(meta, Formatting.Indented);
            File.WriteAllText(metaFileName, json);
            //WriteLine(json);
            WriteLine($"stored meta data: {metaFileName}");
        }
    }
}
