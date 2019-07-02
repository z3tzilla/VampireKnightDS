using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VampireKnightDS
{
    public class FontMap
    {
        readonly Dictionary<string, string> glyphs;
        readonly Dictionary<string, string> codes;
        readonly Dictionary<string, byte> hexLookup;

        public FontMap(string pathToFontMap)
        {
            Console.WriteLine("Loading Font Map...");
            glyphs = new Dictionary<string, string>();
            codes = new Dictionary<string, string>();
            foreach (string line in File.ReadAllLines(pathToFontMap, Encoding.UTF8))
            {
                string byteSequence = line.Substring(0, 4);
                string characterGlyph = line.Substring(5);

                glyphs.Add(byteSequence, characterGlyph);
                codes.TryAdd(characterGlyph, byteSequence);
            }

            hexLookup = new Dictionary<string, byte>();
            for (int i = 0; i <= 0xFF; i++)
            {
                hexLookup.Add(i.ToString("X2"), (byte)i);
            }
        }

        public string FromBinary(byte[] source)
        {
            var result = new StringBuilder();
            byte currentState = 0;
            int printedSymbols = 0;
            for (uint i = 0; i < source.Length; i++)
            {
                if (source[i] < 0x10)
                {
                    if (currentState >= 0x90)
                    {
                        if (printedSymbols == 0)
                        {
                            result.AppendFormat("{0}{1:X2}{2}", '{', currentState, '}');
                            currentState = 0;
                        }
                    }
                    result.AppendFormat("{0}{1:X2}{2}", '{', source[i], '}');
                }
                else if (source[i] < 0x90)
                {
                    if (currentState >= 0x90)
                    {
                        string key = $"{currentState:X2}{source[i]:X2}";
                        if (glyphs.ContainsKey(key))
                        {
                            result.Append(glyphs[key]);
                            printedSymbols++;
                            continue;
                        }
                        if (printedSymbols == 0)
                        {
                            result.AppendFormat("{0}{1:X2}{2}", '{', currentState, '}');
                            currentState = 0;
                        }
                        result.AppendFormat("{0}{1:X2}{2}", '{', source[i], '}');
                    }
                    else
                    {
                        result.AppendFormat("{0}{1:X2}{2}", '{', source[i], '}');
                    }
                }
                else
                {
                    if (currentState >= 0x90)
                    {
                        if (printedSymbols == 0)
                        {
                            result.AppendFormat("{0}{1:X2}{2}", '{', currentState, '}');
                        }
                    }
                    currentState = source[i];
                    printedSymbols = 0;
                }
                //result.AppendFormat("{0}{1:X2}{2}", '{', source[i], '}');
            }

            return result.ToString().Replace("{01}{0A}{02}{00}{02}", "{First Name}").Replace("{01}{0A}{02}{00}{01}", "{Last Name}");
        }

        public byte[] ToBinary(string source)
        {
            // HACK: only the first mention of character glyph counts (though we only need groups 90-91)

            source = source.Replace("{First Name}", "{01}{0A}{02}{00}{02}").Replace("{Last Name}", "{01}{0A}{02}{00}{01}");
            var result = new List<byte>();

            byte currentGroup = 0xFF;
            int i = 0;
            while (i < source.Length)
            {
                if (source[i] == '{')
                {
                    result.Add(
                        hexLookup[source.Substring(i + 1, 2)]);
                    i += 4;
                    continue;
                }

                string code = codes[source.Substring(i++, 1)];
                byte group = hexLookup[code.Substring(0, 2)];
                byte index = hexLookup[code.Substring(2, 2)];
                if (group != currentGroup)
                {
                    result.Add(group);
                    currentGroup = group;
                }
                result.Add(index);
            }
            result.Add(0x00);

            while (result.Count % 4 != 0)
            {
                result.Add(0x00);
            }

            return result.ToArray();
        }
    }
}
