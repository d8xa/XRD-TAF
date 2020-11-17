using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace util
{
    public static class ArrayWriteTools
    {
        #region Base methods

        // TODO: handle empty headCol and headRow.
        public static void Write2D(string path, string[] headCol, string headRow, float[,] data, 
            string sep="\t", bool reverse = false)
        {
            bool StrictCompare(int a, int b) => reverse ? a >= b : a < b;
            var iBounds = reverse ? new Vector2Int(data.GetLength(0) - 1, 0) : new Vector2Int(0, data.GetLength(0));
            var jBounds = reverse ? new Vector2Int(data.GetLength(1) - 1, 0) : new Vector2Int(0, data.GetLength(1));
            var increment = reverse ? -1 : 1;

            using (var fileStream = File.Create(path))
            using (var buffered = new BufferedStream(fileStream))
            using (var writer = new StreamWriter(buffered))
            {
                if (headRow != null) writer.WriteLine(headRow);
                for (var i = iBounds.x; StrictCompare(i,iBounds.y); i += increment)
                {
                    var sb = new StringBuilder();
                    if (headCol != null) sb.Append(headCol[i] + sep);

                    for (var j = jBounds.x; StrictCompare(j, jBounds.y); j += increment)
                    {
                        sb.Append(data[i, j].ToString("G", CultureInfo.InvariantCulture));
                        if (StrictCompare(j, jBounds.y - increment))
                            sb.Append(sep);
                    }
                    writer.WriteLine(sb.ToString());
                }
            }
        }
        
        private static void Write2D(string path, string[] headCol, string headRow, Vector3[,] data, 
            string sep="\t", bool reverse = false)
        {
            bool StrictCompare(int a, int b) => reverse ? a >= b : a < b;
            var iBounds = reverse ? new Vector2Int(data.GetLength(0) - 1, 0) : new Vector2Int(0, data.GetLength(0));
            var jBounds = reverse ? new Vector2Int(data.GetLength(1) - 1, 0) : new Vector2Int(0, data.GetLength(1));
            var increment = reverse ? -1 : 1;

            using (var fileStream = File.Create(path))
            using (var buffered = new BufferedStream(fileStream))
            using (var writer = new StreamWriter(buffered))
            {
                if (headRow != null) writer.WriteLine(headRow);
                for (var i = iBounds.x; StrictCompare(i,iBounds.y); i += increment)
                {
                    var sb = new StringBuilder();
                    if (headCol != null) sb.Append(headCol[i] + sep);

                    for (var j = jBounds.x; StrictCompare(j, jBounds.y); j += increment)
                    {
                        sb.Append("(")
                            .Append(string.Join(", ",
                                data[i, j].x.ToString("G", CultureInfo.InvariantCulture),
                                data[i, j].y.ToString("G", CultureInfo.InvariantCulture),
                                data[i, j].z.ToString("G", CultureInfo.InvariantCulture)
                            ))
                            .Append(")");
                        if (StrictCompare(j, jBounds.y - increment))
                            sb.Append(sep);
                    }
                    writer.WriteLine(sb.ToString());
                }
            }
        }

        private static void Write2D(string path, string[] headCol, string headRow, Vector2[,] data, 
            string sep = "\t", bool reverse = false)
        {
            bool StrictCompare(int a, int b) => reverse ? a >= b : a < b;
            var iBounds = reverse ? new Vector2Int(data.GetLength(0) - 1, 0) : new Vector2Int(0, data.GetLength(0));
            var jBounds = reverse ? new Vector2Int(data.GetLength(1) - 1, 0) : new Vector2Int(0, data.GetLength(1));
            var increment = reverse ? -1 : 1;

            // TODO: create if not exists.
            using (var fileStream = File.Create(path))
            using (var buffered = new BufferedStream(fileStream))
            using (var writer = new StreamWriter(buffered))
            {
                if (headRow != null) writer.WriteLine(headRow);
                for (var i = iBounds.x; StrictCompare(i,iBounds.y); i += increment)
                {
                    var sb = new StringBuilder();
                    for (var j = jBounds.x; StrictCompare(j, jBounds.y); j += increment)
                    {
                        if (headCol != null) sb.Append(headCol[i]);
                        sb.Append("(")
                            .Append(string.Join(", ",
                                data[i, j].x.ToString("G", CultureInfo.InvariantCulture),
                                data[i, j].y.ToString("G", CultureInfo.InvariantCulture)
                            ))
                            .Append(")");
                        if (StrictCompare(j, jBounds.y - increment))
                            sb.Append(sep);
                    }

                    writer.WriteLine(sb.ToString());
                }
            }
        }

        #endregion

        #region Caller methods

        public static void Write2D(string path, float[,] data, string sep="\t", bool reverse = false)
        {
            Write2D(path, null, null, data, sep, reverse);
        }
        
        public static void Write2D(string path, Vector3[,] data, string sep = "\t", bool reverse = false)
        {
            Write2D(path, null, null, data, sep, reverse);
        }
        
        public static void Write2D(string path, Vector2[,] data, string sep = "\t", bool reverse = false)
        {
            Write2D(path, null, null, data, sep, reverse);
        }

        #endregion
        
        
        public static Vector2[,] Coerce2D(Vector2[] data, int xdim, int ydim)
        {
            var result = new Vector2[xdim, ydim];
            for (int j = 0; j < xdim; j++)
            {
                for (int i = 0; i < ydim; i++)
                {
                    result[i, j] = data[i * ydim + j];
                }
            }
            
            return result;
        }
    }
}