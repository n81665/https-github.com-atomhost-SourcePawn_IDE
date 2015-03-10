using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Windows;

namespace SourcePawn_IDE
{
    public class LoadIncludes
    {
        public List<IncInfo> FullInfo = new List<IncInfo>();
        private List<char> ValidSpaces = new List<char>(" \t\n\r:;,=[");
        public Dictionary<string, List<string>> includes = new Dictionary<string, List<string>>();

        bool MultilineForward = false;
        bool MultilineFunc = false;
        string MultilineFunction;

        bool ReadingFunctionInfo = false;
        string FunctionInfo;
        string FuncInfo;
        string Function;

        string filename;

        public LoadIncludes()
        {
            string BasePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\include\\";

            string[] files = Directory.GetFiles(BasePath, "*.inc", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                filename = Path.GetFileName(file);
                includes.Add(filename, new List<string>());
                using (StreamReader reader = new StreamReader(file))
                {
                    try
                    {
                        peekLines(reader);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.StackTrace + "\n\n" + ex.Message + "\n\n" + filename + "\n\n" + reader.ReadLine());
                    }
                }
            }
        }

        private void peekLines(StreamReader reader)
        {
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                if (!(MultilineForward || ReadingFunctionInfo || MultilineFunc))
                {
                    if (line.StartsWith("/**"))
                    {
                        ReadingFunctionInfo = true;
                        //FunctionInfo += line;
                        continue;
                    }
                    else if (line.StartsWith("#pragma deprecated"))
                    {
                        continue;
                    }
                    else if (line.StartsWith("#define"))
                    {
                        IncInfo info = GetDefineValue(line, 8);
                        info.Type = Type.Define;
                        FullInfo.Add(info);
                    }
                    else if (line.StartsWith("#include"))
                    {
                        GetInclude(line);
                    }
                    else if (line.StartsWith("public const String:"))
                    {
                        IncInfo info = GetDefineValue(line, 20);
                        info.Type = Type.Define;
                        FullInfo.Add(info);
                    }
                    else if (line.StartsWith("public const") || line.StartsWith("public Float:"))
                    {
                        IncInfo info = GetDefineValue(line, 13);
                        info.Type = Type.Define;
                        FullInfo.Add(info);
                    }
                    else if ((line.StartsWith("public") || line.StartsWith("stock") || line.StartsWith("native")) && !line.Contains("operator"))
                    {
                        IncInfo info = GetFunction(line);
                        if (info.Type != Type.Multiline)
                        {
                            if (!string.IsNullOrWhiteSpace(info.Function))
                            {
                                FullInfo.Add(info);
                            }
                        }
                    }
                    else if (line.StartsWith("forward") && !line.Contains("operator"))
                    {
                        IncInfo info = GetForward(line);
                        if (info.Type != Type.Multiline)
                        {
                            FullInfo.Add(info);
                        }
                    }
                    else if (line.StartsWith("enum"))
                    {
                        IncInfo info = GetEnum(line, reader);
                        FullInfo.Add(info);
                    }
                    FunctionInfo = "";
                }
                else
                {
                    if (MultilineForward)
                    {
                        int index = -1;
                        while (ValidSpaces.Contains(line[++index]) && line[index] != ',')
                        {
                        }
                        MultilineFunction += line.Substring(index - 1);
                        if (line.Contains(')'))
                        {
                            IncInfo info = new IncInfo();
                            info.FunctionInfo = FuncInfo;
                            info.Type = Type.Forward;
                            info.file = filename;
                            info.Function = MultilineFunction.Replace("  ", " ");
                            MultilineForward = false;
                            FullInfo.Add(info);
                        }
                    }
                    else if (MultilineFunc)
                    {
                        int index = -1;
                        while (ValidSpaces.Contains(line[++index]) && line[index] != ',')
                        {
                        }
                        MultilineFunction += line.Substring(index - 1).Replace("\t", "");
                        if (line.Contains(')'))
                        {
                            IncInfo info = new IncInfo();
                            info.FunctionInfo = FuncInfo;
                            info.Type = Type.Function;
                            info.file = filename;
                            info.Function = Function;
                            info.Line = MultilineFunction.Replace("  ", " ");
                            MultilineFunc = false;
                            FullInfo.Add(info);
                        }
                    }
                    else
                    {
                        if (line.Contains("*/"))
                        {
                            ReadingFunctionInfo = false;
                            continue;
                        }
                        FunctionInfo += (FunctionInfo == "" ? "" : "\n") + line;
                    }
                }
            }
        }

        private IncInfo GetEnum(string line, StreamReader reader)
        {
            IncInfo val = new IncInfo();
            val.file = filename;
            int index = line.IndexOf(' ');
            StringBuilder builder = new StringBuilder();
            while (++index < line.Length && !ValidSpaces.Contains(line[index]))
            {
                builder.Append(line[index]);
            }
            
            val.Function = builder.ToString();
            if (val.Function == "{" || val.Function == "enum" || string.IsNullOrWhiteSpace(val.Function))
            {
                val.Function = "{Unidentified}";
            }

            bool DontRead = false;

            while (!reader.EndOfStream)
            {
                string eLine = reader.ReadLine();
                if (eLine.StartsWith("}"))
                {
                    break;
                }

                if (DontRead && eLine.Contains("*/"))
                {
                    DontRead = false;
                    continue;
                }

                if (eLine.StartsWith("{") || eLine.Replace("\t", "").Replace(" ", "").StartsWith("*") || eLine.Replace("\t", "").Replace(" ", "").StartsWith("/*") || eLine.Replace("\t", "").Replace(" ", "").StartsWith("*/") || DontRead)
                {
                    continue;
                }
                else
                {
                    StringBuilder build = new StringBuilder();
                    char c;
                    bool GotValue = false;
                    for (int idx = 0; idx < eLine.Length; idx++)
                    {
                        c = eLine[idx];
                        if (!GotValue)
                        {
                            if (!ValidSpaces.Contains(c))
                            {
                                build.Append(c);
                                GotValue = true;
                            }
                        }
                        else
                        {
                            if (!ValidSpaces.Contains(c))
                            {
                                build.Append(c);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    if (val.EnumValues == null)
                    {
                        val.EnumValues = new List<string>();
                    }

                    if (!string.IsNullOrWhiteSpace(build.ToString()))
                    {
                        val.EnumValues.Add(build.ToString());
                    }

                    if (eLine.Contains("/**") && !eLine.Contains("*/"))
                    {
                        DontRead = true;
                    }
                }
            }

            return val;
        }

        private IncInfo GetForward(string line)
        {
            IncInfo val = new IncInfo();
            val.file = filename;
            int StartIndex = line.IndexOf(' ') + 1;
            int EndIndex = line.IndexOf(')') + 1;
            if (EndIndex == 0)
            {
                MultilineForward = true;
                MultilineFunction = line.Substring(StartIndex);
                val.Type = Type.Multiline;
                return val;
            }
            val.Function = line.Substring(StartIndex, EndIndex - StartIndex);
            val.Type = Type.Forward;
            val.Line = line;
            val.FunctionInfo = FunctionInfo;
            return val;
        }

        private IncInfo GetFunction(string line)
        {
            IncInfo val = new IncInfo();
            val.file = filename;
            val.Type = Type.Function;
            val.Line = line;
            val.FunctionInfo = FunctionInfo;
            int index = line.IndexOf('(');
            int EndIndex = line.IndexOf(')') + 1;
            StringBuilder builder = new StringBuilder();
            while (--index >= 0 && !ValidSpaces.Contains(line[index]))
            {
                builder.Insert(0, line[index]);
            }
            val.Function = builder.ToString();
            if (EndIndex == 0 && index > 0)
            {
                MultilineFunc = true;
                MultilineFunction = line;
                val.Type = Type.Multiline;
                Function = val.Function;
                FuncInfo = FunctionInfo;
                return val;
            }
            return val;
        }

        private IncInfo GetDefineValue(string line, int index)
        {
            IncInfo val = new IncInfo();
            val.file = filename;
            StringBuilder builder = new StringBuilder();
            while (index < line.Length && !ValidSpaces.Contains(line[index]))
            {
                builder.Append(line[index]);
                index++;
            }
            val.Value = builder.ToString();
            builder.Length = 0;
            bool ReadingInfo = false;
            for (; index < line.Length; index++)
            {
                if (line[index] == '/')
                {
                    ReadingInfo = true;
                }
                if (ReadingInfo)
                {
                    builder.Append(line[index]);
                }
            }
            val.Line = line;
            return val;
        }

        private void GetInclude(string line)
        {
            int StartIndex = line.IndexOf('<') + 1;
            int EndIndex = line.IndexOf('>');
            if (StartIndex == -1)
            {
                StartIndex = line.IndexOf('"') + 1;
                EndIndex = line.IndexOf('"', StartIndex);
            }
            if (StartIndex != -1 || EndIndex != -1)
            {
                string inc = line.Substring(StartIndex, EndIndex-StartIndex) + ".inc";
                includes[filename].Add(inc);
            }
        }
    }

    public struct IncInfo
    {
        public string Line;
        public string Value;
        public Type Type;
        public string Function;
        public string FunctionInfo;
        public List<string> EnumValues;
        public string file;
    }

    public enum Type
    {
        Enum,
        Define,
        Function,
        Forward,
        Multiline,
        Invalid
    }
}
