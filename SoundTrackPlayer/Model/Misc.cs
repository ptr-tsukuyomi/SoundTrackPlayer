using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.FileIO;

namespace SoundTrackPlayer.Model
{
    internal class Misc
    {
        public static List<string> ParseArguments(string arguments)
        {
            var reader = new StringReader(arguments);
            var parser = new TextFieldParser(reader);

            parser.SetDelimiters(" ");
            parser.HasFieldsEnclosedInQuotes = true;

            var result = parser.ReadFields();
            if (result is not null)
            {
                return new List<string>(result).FindAll((x) => !string.IsNullOrEmpty(x));
            } else
            {
                return new List<string>();
            }
        }
    }
}
