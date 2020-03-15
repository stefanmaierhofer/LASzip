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
using Aardvark.Data.Points;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using static System.Console;

namespace Example
{
    static class Program
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

            dirname = Path.GetFullPath(dirname);
            WriteLine($"{dirname}:");

            //Test(dirname); return;

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
                foreach (var ps in LASZip.Parser.ReadPoints(filename, 1024 * 1024))
                {
                    var bb = new Box3d(ps.Positions);
                    bounds2.ExtendBy(bb);
                    //WriteLine($"  chunk {ps.Count,20:N0} {bb,20}");
                    //WriteLine($"        {"",20:N0} {new Cell(bb),20}");
                }

                if (totalPointCount > 100000000) break;
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

            var bb = new Box3d((bounds.Min / 256).Floor() * 256, (bounds.Max / 256).Ceiling() * 256);
            WriteLine($"bounds: {bb}");

            var minIncl = new V2l(bb.Min.X / TILE_SIZE, bb.Min.Y / TILE_SIZE);
            var maxExcl = new V2l(bb.Max.X / TILE_SIZE + 1, bb.Max.Y / TILE_SIZE + 1);
            WriteLine($"a: {new Cell(minIncl.X, minIncl.Y, 0, 8).BoundingBox}");
            WriteLine($"b: {new Cell(maxExcl.X, maxExcl.Y, 0, 8).BoundingBox}");

            var maxCount = 0L;
            var i = 0;
            var importConfig = ParseConfig.Default.WithMaxChunkPointCount(16 * 1024 * 1024);
            for (var cx = minIncl.X; cx < maxExcl.X; cx++)
            {
                for (var cy = minIncl.Y; cy < maxExcl.Y; cy++)
                {
                    i++;

                    var localCell = new Cell(cx, cy, 0, 8);
                    var localBounds = localCell.BoundingBox - OFFSET;
                    var candidates = meta.Where(x => x.bounds.Intersects(localBounds)).ToArray();
                    if (candidates.Length > 0)
                    {
                        var key = $"cell_{localCell.X}_{localCell.Y}_{localCell.Z}_{localCell.Exponent}";


                        var targetFilename = $"G://{key}.bin";
                        var targetFilenameTmp = $"{targetFilename}.tmp";
                        if (File.Exists(targetFilename))
                        {
                            WriteLine($"[{i,6:N0}] {targetFilename} already exists");
                            continue;
                        }



                        var sw = new Stopwatch(); sw.Start();
                        var localCount = candidates.Sum(x => x.count);
                        if (localCount > maxCount) maxCount = localCount;
                        var chunks = candidates
                            .SelectMany(x => Aardvark.Data.Points
                                .Import.Laszip.Chunks(Path.Combine(dirname, x.filename), importConfig)
                                .Select(y => y.ImmutableFilterByPosition(localBounds.Contains))
                                )
                            .Where(x => x.Count > 0)
                            .Select(x => new Chunk(x.Positions, x.Colors))
                            .ToArray()
                            ;
                        if (chunks.Sum(x => x.Count) == 0) continue;

                        //var store = PointCloud.OpenStore($"T:/Koeln/{key}.bin");
                        //PointCloud.Chunks(chunks, ImportConfig.Default.WithStorage(store).WithKey(key));

                        var countWritten = 0L;
                        using (var f = File.OpenWrite(targetFilenameTmp))
                        using (var z = new GZipStream(f, CompressionMode.Compress))
                        using (var bw = new BinaryWriter(z))
                        {
                            var o = chunks.First().Positions[0];
                            bw.Write(o.X); bw.Write(o.Y); bw.Write(o.Z);
                            bw.Write(chunks.Sum(x => (long)x.Count));
                            foreach (var chunk in chunks)
                            {
                                var chunk2 = chunk.ImmutableFilterSequentialMinDistL1(0.01);
                                if (chunk2.Count == 0) continue;
                                var ps = chunk2.Positions.Map(p => (V3f)(p - o).Round(2));
                                for (var j = 0; j < ps.Length; j++)
                                {
                                    var p = ps[j];
                                    bw.Write(p.X);
                                    bw.Write(p.Y);
                                    bw.Write(p.Z);
                                    countWritten++;
                                }
                                for (var j = 0; j < chunk2.Count; j++)
                                {
                                    var c = chunk2.Colors[j];
                                    bw.Write(c.R); bw.Write(c.G); bw.Write(c.B);
                                }
                            }
                        }

                        File.Move(targetFilenameTmp, targetFilename);

                        WriteLine($"[{i,6:N0}] {localCell,6} -> {candidates.Length,6:N0} -> {localCount,15:N0}  {chunks.Sum(x => x.Count),15:N0}  {countWritten,15:N0}  ({sw.Elapsed})");
                    }
                }
            }
            WriteLine(i);
        }

        static Chunk ImmutableFilterByPosition(this Chunk self, Func<V3d, bool> predicate)
        {
            if (!self.HasPositions) return self;

            var ps = new List<V3d>();
            var cs = self.Colors != null ? new List<C4b>() : null;

            for (var i = 0; i < self.Positions.Count; i++)
            {
                if (predicate(self.Positions[i]))
                {
                    ps.Add(self.Positions[i]);
                    if (cs != null) cs.Add(self.Colors[i]);
                }
            }
            return new Chunk(ps, cs);
        }
    }
}
