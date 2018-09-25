﻿/*
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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using static System.Console;

namespace Example
{
    class Program
    {
        private static void Test(string dirname)
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

            var bounds = new Box3d(meta.Select(x => x.bounds));
            WriteLine($"bounds: {bounds}");

            const double TILE_SIZE = 256.0;
            var OFFSET = new V3d(0, 0, 50);

            bounds = bounds + OFFSET;
            if (bounds.Size.Z > TILE_SIZE) throw new InvalidOperationException();

            var bb = new Box3d((bounds.Min / 256).Floor * 256, (bounds.Max / 256).Ceiling * 256);
            WriteLine($"bounds: {bb}");

            var minIncl = new V2l(bb.Min.X / TILE_SIZE, bb.Min.Y / TILE_SIZE);
            var maxExcl = new V2l(bb.Max.X / TILE_SIZE + 1, bb.Max.Y / TILE_SIZE + 1);
            WriteLine($"a: {new Cell(minIncl.X, minIncl.Y, 0, 8).BoundingBox}");
            WriteLine($"b: {new Cell(maxExcl.X, maxExcl.Y, 0, 8).BoundingBox}");

            var maxCount = 0L;
            var i = 0;
            for (var cx = 13074 /*minIncl.X*/; cx < maxExcl.X; cx++)
            {
                for (var cy = 22057 /*minIncl.Y*/; cy < maxExcl.Y; cy++)
                {
                    var localCell = new Cell(cx, cy, 0, 8);
                    var localBounds = localCell.BoundingBox - OFFSET;
                    var candidates = meta.Where(x => x.bounds.Intersects(localBounds)).ToArray();
                    if (candidates.Length > 0)
                    {
                        var sw = new Stopwatch(); sw.Start();
                        var localCount = candidates.Sum(x => x.count);
                        if (localCount > maxCount) maxCount = localCount;
                        //var ps = candidates
                        //    .Select(x => LASZip.Parser.ReadPoints(Path.Combine(dirname, x.filename), 1024 * 1024))
                        //    .Select(x => x.SelectMany(chunk => chunk.Positions.Where(p => localBounds.Contains(p)).ToArray()).ToArray())
                        //    .ToArray()
                        //    ;
                        WriteLine($"[{i,6:N0}] {localCell,6} -> {candidates.Length,6:N0} -> {localCount,15:N0}   ({sw.Elapsed})");
                        i++;
                    }
                }
            }
            WriteLine(i);
        }

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

            dirname = Path.GetFullPath(dirname);
            WriteLine($"{dirname}:");

            Test(dirname); return;

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
