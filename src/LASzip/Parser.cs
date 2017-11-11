using System.Collections.Generic;
using System.IO;
using Aardvark.Base;
using laszip.net;

namespace LASZip
{
    /// <summary>
    /// </summary>
    public struct Info
    {
        /// <summary>
        /// Total number of points.
        /// </summary>
        public readonly long Count;

        /// <summary>
        /// Bounding box of point cloud.
        /// </summary>
        public readonly Box3d Bounds;

        /// <summary>
        /// </summary>
        public Info(long count, Box3d bounds)
        {
            Count = count;
            Bounds = bounds;
        }
    }

    /// <summary>
    /// Points (positions, colors, classifications).
    /// </summary>
    public struct Points
    {
        /// <summary></summary>
        public readonly V3d[] Positions;

        /// <summary></summary>
        public readonly C4b[] Colors;

        /// <summary></summary>
        public readonly byte[] Classifications;

        /// <summary>
        /// Number of points.
        /// </summary>
        public int Count => Positions.Length;

        /// <summary></summary>
        public Points(V3d[] positions, C4b[] colors, byte[] classifications)
        {
            Positions = positions;
            Colors = colors;
            Classifications = classifications;
        }
    }

    /// <summary>
    /// </summary>
    public static class Parser
    {
        #region ReadInfo

        /// <summary>
        /// Returns info for given dataset.
        /// </summary>
        public static Info ReadInfo(string filename)
        {
            var reader = new laszip_dll();
            var compressed = false;
            reader.laszip_open_reader(filename, ref compressed);
            return ReadInfo(reader);
        }

        /// <summary>
        /// Returns info for given dataset.
        /// </summary>
        public static Info ReadInfo(Stream stream)
        {
            var reader = new laszip_dll();
            var compressed = false;
            reader.laszip_open_reader(stream, ref compressed);
            return ReadInfo(reader);
        }
        
        private static Info ReadInfo(laszip_dll reader)
        {
            var count = reader.header.number_of_point_records;

            var bounds = new Box3d(
                new V3d(reader.header.min_x, reader.header.min_y, reader.header.min_z),
                new V3d(reader.header.max_x, reader.header.max_y, reader.header.max_z)
                );

            reader.laszip_close_reader();

            return new Info(count, bounds);
        }

        #endregion

        /// <summary>
        /// Reads point data in chunks of given number of points.
        /// </summary>
        public static IEnumerable<Points> ReadPoints(string filename, int numberOfPointsPerChunk)
        {
            var reader = new laszip_dll();
            var isCompressed = false;
            reader.laszip_open_reader(filename, ref isCompressed);
            return ReadPoints(reader, numberOfPointsPerChunk);
        }

        /// <summary>
        /// Reads point data in chunks of given number of points.
        /// </summary>
        public static IEnumerable<Points> ReadPoints(Stream stream, int numberOfPointsPerChunk)
        {
            var reader = new laszip_dll();
            var isCompressed = false;
            reader.laszip_open_reader(stream, ref isCompressed);
            return ReadPoints(reader, numberOfPointsPerChunk);
        }
        
        private static IEnumerable<Points> ReadPoints(laszip_dll reader, int numberOfPointsPerChunk)
        {
            var n = reader.header.number_of_point_records;
            var numberOfChunks = n / numberOfPointsPerChunk;

            for (var j = 0; j < n; j += numberOfPointsPerChunk)
            {
                if (j + numberOfPointsPerChunk > n) numberOfPointsPerChunk = (int)(n - j);
                //Console.WriteLine($"j: {j}, numberOfPointsPerChunk: {numberOfPointsPerChunk}, n: {n}");
                var p = new double[3];
                var ps = new V3d[numberOfPointsPerChunk];
                var cs = new C4b[numberOfPointsPerChunk];
                var ts = new byte[numberOfPointsPerChunk];
                for (var i = 0; i < numberOfPointsPerChunk; i++)
                {
                    reader.laszip_read_point();
                    
                    reader.laszip_get_coordinates(p);
                    ps[i] = new V3d(p);
                    cs[i] = new C4b(reader.point.rgb[0] >> 8, reader.point.rgb[1] >> 8, reader.point.rgb[2] >> 8);
                    ts[i] = reader.point.classification;
                }
                yield return new Points(ps, cs, ts);
            }

            reader.laszip_close_reader();
        }
    }
}
