using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Document;
using System.Windows;

namespace SourcePawn_IDE
{
    public class FoldingStrategy : AbstractFoldingStrategy
    {
        List<char> ValidSpaces = new List<char>(" \t\n\r");


        public FoldingStrategy()
        {
        }
   
        public override IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset)
        {
            firstErrorOffset = -1;
            return CreateNewFoldings(document);
        }

        public IEnumerable<NewFolding> CreateNewFoldings(ITextSource document)
        {
            List<NewFolding> newFoldings = new List<NewFolding>();
            
            int line = 0;

            for (int i = 0; i < document.TextLength; i++)
            {
                char c = document.GetCharAt(i);
                if (c == '{')
                {
                    int startOffset = i;
                    int endOffset = FindCorrectClosingBracket(i + 1, line, document);
                    if (endOffset > startOffset)
                    {
                        int offset = startOffset;
                        while (--offset > 0 && ValidSpaces.Contains(document.GetCharAt(offset)))
                        {
                        }
                        char c1 = document.GetCharAt(offset);
                        if (c1 == ')' || c1 == '=')
                        {
                            newFoldings.Add(new NewFolding(offset + 1, endOffset + 1));
                        }
                    }
                }
                else if (c == '\n' || c == '\r')
                {
                    line++;
                }
            }
            newFoldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
            return newFoldings;
        }

        int FindCorrectClosingBracket(int index, int line, ITextSource document)
        {
            bool inString = false;
            bool inChar = false;

            bool lineComment = false;
            bool blockComment = false;

            int quickResult = QuickSearch(document, index, line);
            if (quickResult != -1) return quickResult;

            int brackets = 1;
            int nline = line;

            while (index < document.TextLength)
            {
                char ch = document.GetCharAt(index);
                switch (ch)
                {
                    case '\r':
                    case '\n':
                        nline++;
                        lineComment = false;
                        inChar = false;
                        inString = false;
                        break;
                    case '/':
                        if (blockComment)
                        {
                            if (document.GetCharAt(index - 1) == '*')
                            {
                                blockComment = false;
                            }
                        }
                        if (!inString && !inChar && index + 1 < document.TextLength)
                        {
                            if (!blockComment && document.GetCharAt(index + 1) == '/')
                            {
                                lineComment = true;
                            }
                            if (!lineComment && document.GetCharAt(index + 1) == '*')
                            {
                                blockComment = true;
                            }
                        }
                        break;
                    case '"':
                        if (!(inChar || lineComment || blockComment))
                        {
                            inString = !inString;
                        }
                        break;
                    case '\'':
                        if (!(inString || lineComment || blockComment))
                        {
                            inChar = !inChar;
                        }
                        break;
                    case '\\':
                        if (inString || inChar)
                            index++;
                        break;
                    default:
                        if (ch == '{')
                        {
                            if (!(inString || inChar || lineComment || blockComment))
                            {
                                ++brackets;
                            }
                        }
                        else if (ch == '}')
                        {
                            if (!(inString || inChar || lineComment || blockComment))
                            {
                                --brackets;
                                if (brackets == 0)
                                {
                                    if (nline == line)
                                    {
                                        return -1;
                                    }
                                    else
                                    {
                                        return index;
                                    }
                                }
                            }
                        }
                        break;
                }
                ++index;
            }
            return -1;
        }

        int QuickSearch(ITextSource document, int index, int line)
        {
            int brackets = 1;
            int nline = line;
            for (int i = index; i < document.TextLength; ++i)
            {
                char ch = document.GetCharAt(i);
                if (ch == '{')
                {
                    brackets++;
                }
                else if (ch == '}')
                {
                    brackets++;
                    if (brackets == 0)
                    {
                        if (nline == line)
                        {
                            return -2;
                        }
                        else
                        {
                            return i;
                        }
                    }
                }
                else if (ch == '"')
                {
                    break;
                }
                else if (ch == '\'')
                {
                    break;
                }
                else if (ch == '/' && i > 0)
                {
                    if (document.GetCharAt(i - 1) == '/') break;
                }
                else if (ch == '*' && i > 0)
                {
                    if (document.GetCharAt(i - 1) == '/') break;
                }
                else if (ch == '\n' || ch == '\r')
                {
                    nline++;
                }
            }
            return -1;
        }
    }
}
