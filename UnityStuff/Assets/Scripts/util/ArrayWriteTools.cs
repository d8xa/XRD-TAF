using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace util
{
    public static class ArrayWriteTools
    {
        // TODO: handle empty headCol and headRow.
        public static void Write2D(string path, string[] headCol, string headRow, float[,] data, string sep="\t")
        {
            using (FileStream fileStream = File.Create(path))
            using (BufferedStream buffered = new BufferedStream(fileStream))
            using (StreamWriter writer = new StreamWriter(buffered))
            {
                if (headRow != null) writer.WriteLine(headRow);
                for (int i = 0; i < data.GetLength(0); i++)
                {
                    var sb = new StringBuilder();
                    if (headCol != null) sb.Append(headCol[i]).Append(sep);
                    for (var j = 0; j < data.GetLength(1); j++)
                    {
                        sb.Append(data[i,j].ToString("G", CultureInfo.InvariantCulture));
                        if (j < data.GetLength(1) - 1)
                            sb.Append(sep);
                    }
                    writer.WriteLine(sb.ToString());
                    sb.Clear();
                }
            }
        }
        
        public static void Write2D(string path, float[,] data, string sep="\t")
        {
            Write2D(path, null, null, data, sep);
        }
        
        
        public static void Write2D(string path, string[] headCol, string headRow, Vector3[,] data, string sep="\t")
        {
            using (FileStream fileStream = File.Create(path))
            using (BufferedStream buffered = new BufferedStream(fileStream))
            using (StreamWriter writer = new StreamWriter(buffered))
            {
                if (headRow != null) writer.WriteLine(headRow);
                for (int i = 0; i < data.GetLength(0); i++)
                {
                    var sb = new StringBuilder();
                    for (var j = 0; j < data.GetLength(1); j++)
                    {
                        if (headCol != null) sb.Append(headCol[i]);
                        sb.Append("(")
                            .Append(string.Join(", ", 
                                data[i,j].x.ToString("G", CultureInfo.InvariantCulture), 
                                data[i,j].y.ToString("G", CultureInfo.InvariantCulture), 
                                data[i,j].z.ToString("G", CultureInfo.InvariantCulture)
                                ))
                            .Append(")");
                        if (j < data.GetLength(1) - 1)
                            sb.Append(sep);
                    }
                    writer.WriteLine(sb.ToString());
                    sb.Clear();
                }
            }
        }
        
        public static void Write2D(string path, string[] headCol, string headRow, Vector2[,] data, string sep="\t")
        {
            // TODO: create if not exists.
            using (FileStream fileStream = File.Create(path))
            using (BufferedStream buffered = new BufferedStream(fileStream))
            using (StreamWriter writer = new StreamWriter(buffered))
            {
                if (headRow != null) writer.WriteLine(headRow);
                for (int i = 0; i < data.GetLength(0); i++)
                {
                    var sb = new StringBuilder();
                    for (var j = 0; j < data.GetLength(1); j++)
                    {
                        if (headCol != null) sb.Append(headCol[i]);
                        sb.Append("(")
                            .Append(string.Join(", ", 
                                data[i,j].x.ToString("G", CultureInfo.InvariantCulture), 
                                data[i,j].y.ToString("G", CultureInfo.InvariantCulture)
                                ))
                            .Append(")");
                        if (j < data.GetLength(1) - 1)
                            sb.Append(sep);
                    }
                    writer.WriteLine(sb.ToString());
                    sb.Clear();
                }
            }
        }

        public static void Write2D(string path, Vector3[,] data, string sep = "\t")
        {
            Write2D(path, null, null, data, sep);
        }
        
        public static void Write2D(string path, Vector2[,] data, string sep = "\t")
        {
            Write2D(path, null, null, data, sep);
        }

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