using model;

namespace util
{
    public static class FieldParseTools
    {
        /// <summary>
        /// Not null, empty or whitespace.
        /// </summary>
        public static bool IsValue(string text)
        {
            return !(string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(text));
        }

        public static void ParseField(string input, ref string target)
        {
            if (IsValue(input))
                target = input;
        }
        
        public static string ParseField(string input, string target)
        {
            if (IsValue(input)) target = input;
            return target;
        }

        public static void ParseField(string input, ref float target)
        {
            if (IsValue(input)) 
                target = float.Parse(input, Settings.defaults.cultureInfo);
        }

        public static void ParseField(string input, ref int target)
        {
            if (IsValue(input)) 
                target = int.Parse(input, Settings.defaults.cultureInfo);
        }
        
        

        public static float? ParseFloat(string input)
        {
            float? value = null;
            if (IsValue(input)) 
                value = float.Parse(input, Settings.defaults.cultureInfo);
            return value;
        }

        public static int? ParseInt(string input)
        {
            int? value = null;
            if (IsValue(input)) 
                value = int.Parse(input, Settings.defaults.cultureInfo);
            return value;
        }
    }
}