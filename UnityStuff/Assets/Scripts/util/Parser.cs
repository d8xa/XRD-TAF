using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace util
{
    public static class Parser
    {
        internal static float[] ImportAngles(string path)
        {
            string text;
            if (File.Exists(path))
            {
                using (var reader = new StreamReader(path))
                    text = reader.ReadToEnd();
            }
            else throw new FileNotFoundException();

            return text
                .Trim(' ')
                .Split('\n')
                .Where(s => s.Length > 0)
                .Select(s => float.Parse(s, CultureInfo.InvariantCulture))
                .ToArray();
        }
        
         /// <summary>
        /// Parses tuple in string to Vector3.
        /// </summary>
        /// <param name="tuple">string containing a tuple of form (float,float,float)</param>
        private static Vector3 ParseTuple(string tuple)
        {
            var floats = tuple
                .TrimStart('(').TrimEnd(')')
                .Split(',')
                .Select(s => float.Parse(s, CultureInfo.InvariantCulture))
                .ToArray();
            return new Vector3(floats[0], floats[1], floats[2]);
        }
        
        internal static Vector3[,] ReadArray(string path, bool headCol, bool headRow, string sep="\t", bool reverse = false)
        {
            string text;
            if (File.Exists(path))
            {
                using (var reader = new StreamReader(path))
                    text = reader.ReadToEnd();
            }
            else throw new FileNotFoundException();

            var parsed = text
                .Trim(' ')
                .Split('\n')
                .Where(s => s.Length > 0)
                .Select(s => Regex.Split(s, sep))
                .Select(row => row.Select(ParseTuple).ToArray())
                .ToArray();

            var iOffset = (reverse ? -1 : 1) * (headRow ? 1 : 0);
            var jOffset = headCol ? 1 : 0;

            var n = parsed.Length - Math.Abs(iOffset);    // row count.
            var m = parsed.Select(row => row.Length).Max() - jOffset;    // column count.

            var data = new Vector3[n,m]; 
            
            bool StrictCompare(int a, int b) => reverse ? a >= b : a < b;
            var iBounds = reverse ? new Vector2Int(n - 1, 0) : new Vector2Int(0, n);
            var increment = reverse ? -1 : 1;

            for (int i = iBounds.x; StrictCompare(i, iBounds.y); i += increment)
            {
                // TODO: check length of inner array and decide how to fill rest if length < m.
                for (int j = 0; j < m; j++)
                {
                    data[i, j] = parsed[i + iOffset][j + jOffset];
                }
            }

            return data;
        }

        /// <summary>
        /// Reads a table from a text file into a 2-dimensional array.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="headCol">The first column (index column).</param>
        /// <param name="headRow">The first row (title row).</param>
        /// <param name="sep">The column separator</param>
        /// <param name="reverse"></param>
        /// <exception cref="FileNotFoundException"></exception>
        internal static float[,] ReadTable(string path, bool headCol, bool headRow, string sep="\t", bool reverse = false)
        {
            string text;
            if (File.Exists(path))
            {
                using (var reader = new StreamReader(path))
                    text = reader.ReadToEnd();
            }
            else throw new FileNotFoundException();

            var parsed = text
                .Trim(' ')
                .Split('\n')
                .Where(s => s.Length > 0)
                .Select(s => Regex.Split(s, sep))
                .Select(row => row.Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToArray())
                .ToArray();

            var iOffset = (reverse ? -1 : 1) * (headRow ? 1 : 0);
            var jOffset = headCol ? 1 : 0;

            var n = parsed.Length - Math.Abs(iOffset);    // row count.
            var m = parsed.Select(row => row.Length).Max() - jOffset;    // column count.

            var data = new float[n,m]; 
            
            bool StrictCompare(int a, int b) => reverse ? a >= b : a < b;
            var iBounds = reverse ? new Vector2Int(n - 1, 0) : new Vector2Int(0, n);
            var increment = reverse ? -1 : 1;

            for (int i = iBounds.x; StrictCompare(i, iBounds.y); i += increment)
            {
                // TODO: check length of inner array and decide how to fill rest if length < m.
                for (int j = 0; j < m; j++)
                {
                    data[i, j] = parsed[i + iOffset][j + jOffset];
                }
            }

            return data;
        }

        internal static Vector3[] ReadTableVector(string path, bool headCol, bool headRow, string sep = "\t",
            bool reverse = false)
        {
            string text;
            if (File.Exists(path))
            {
                using (var reader = new StreamReader(path))
                    text = reader.ReadToEnd();
            }
            else throw new FileNotFoundException();

            var parsed = text
                .Trim(' ')
                .Split('\n')
                .Where(s => s.Length > 0)
                .Select(s => Regex.Split(s, sep))
                .Select(row => row.Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToArray())
                .ToArray();

            var iOffset = (reverse ? -1 : 1) * (headRow ? 1 : 0);
            var jOffset = headCol ? 1 : 0;

            var n = parsed.Length - Math.Abs(iOffset);    // row count.
            var m = parsed.Select(row => row.Length).Max() - jOffset;    // column count.

            var data = new Vector3[n]; 
            
            bool StrictCompare(int a, int b) => reverse ? a >= b : a < b;
            var iBounds = reverse ? new Vector2Int(n - 1, 0) : new Vector2Int(0, n);
            var increment = reverse ? -1 : 1;

            for (int i = iBounds.x; StrictCompare(i, iBounds.y); i += increment)
            {
                data[i] = new Vector3(
                    parsed[i + iOffset][0 + jOffset],
                    parsed[i + iOffset][1 + jOffset],
                    parsed[i + iOffset][2 + jOffset]
                );
            }

            return data;
        }
    }
}