using System.Globalization;
using System.IO;
using System.Text;

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
                    for (var j = 0; j < data.GetLength(1); j++)
                    {
                        if (headCol != null) sb.Append(headCol[i]);
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
    }
}