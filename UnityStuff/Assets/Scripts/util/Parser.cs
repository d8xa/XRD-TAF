using System;
using System.Globalization;
using System.IO;
using System.Linq;

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
    }
}