using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SourcePawn_IDE
{
    public struct ConVar
    {
        private static char[] digits = { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0' };
        public string Command;
        public string Value;
        public string Description;
        public string UseMinVal;
        public string MinVal;
        public string UseMaxVal;
        public string MaxVal;

        public string Handle;

        public ConVar(string Command, string Value, string Description, string UseMinVal, string MinVal, string UseMaxVal, string MaxVal, string Handle)
        {
            this.Command = Command;
            this.Value = Value;
            this.Description = Description;
            this.UseMinVal = UseMinVal.ToLower();
            this.MinVal = MinVal;
            this.UseMaxVal = UseMaxVal.ToLower();
            this.MaxVal = MaxVal;
            this.Handle = Handle;
            this.MaxVal = fixFloatString(MaxVal);
            this.MinVal = fixFloatString(MinVal);
        }

        private string fixFloatString(string input)
        {
            if (string.IsNullOrWhiteSpace(input) || input.IndexOfAny(digits) == -1)
            {
                return "_";
            }
            else
            {
                char[] chars = input.ToCharArray();
                foreach (char c in chars)
                {
                    if (!char.IsDigit(c) && c != '.' && c != ',')
                    {
                        return "_";
                    }
                }

                decimal dec = Convert.ToDecimal(input.Replace('.', ','));
                return dec.ToString("0.0").Replace(',', '.');
            }
        }
    }
}
