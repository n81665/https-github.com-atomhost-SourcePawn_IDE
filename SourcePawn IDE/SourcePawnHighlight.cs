using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Document;
using System.Windows.Media;
using System.Windows;
using System.Diagnostics;
using ICSharpCode.AvalonEdit.Highlighting;
using System.IO;
using ICSharpCode.AvalonEdit;

namespace SourcePawn_IDE
{
    public enum TYPE
    {
        KeyWords1,
        KeyWords2,
        Define
    }

    public class SourcePawnHighlightRules : DocumentColorizingTransformer
    {
        public List<char> ValidChars = new List<char>(" ,.\r\t\n:;(){}[]+-*/|^&%");
        public List<string> KeyWords1 = new List<string>();
        public List<string> KeyWords2 = new List<string>();
        public List<string> Define = new List<string>();
        public List<int> ErrorLine = new List<int>();

        //public HighlightingRuleSet colorizer;
        public Dictionary<TYPE, SolidColorBrush> GetColor = new Dictionary<TYPE, SolidColorBrush>();

        char[] sc = new char[] { '"', '\'' };

        public SourcePawnHighlightRules()
        {
            GetColor[TYPE.KeyWords1] = new SolidColorBrush(Color.FromArgb(255, 0, 0, 255));
            GetColor[TYPE.KeyWords2] = new SolidColorBrush(Color.FromArgb(255, 0, 128, 192));
            GetColor[TYPE.Define] = new SolidColorBrush(Color.FromArgb(255, 232, 116, 0));
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            int lineStartOffset = line.Offset, lineEndOffset = line.EndOffset;
            string text = CurrentContext.Document.GetText(line);

            foreach (string str in KeyWords1)
            {
                TheWhile(text, str, lineStartOffset, TYPE.KeyWords1);
            }
            foreach (string str in KeyWords2)
            {
                TheWhile(text, str, lineStartOffset, TYPE.KeyWords2);
            }
            foreach (string str in Define)
            {
                TheWhile(text, str, lineStartOffset, TYPE.Define);
            }
            
            if (ErrorLine.Contains(line.LineNumber))
            {
                base.ChangeLinePart(lineStartOffset, lineEndOffset, (VisualLineElement element) =>
                {
                    element.TextRunProperties.SetBackgroundBrush(new SolidColorBrush(Color.FromRgb(255, 128, 128)));
                });
            }
        }
        
        public bool IsInStringComment(int offset)
        {
            IHighlighter documentHighlighter = CurrentContext.TextView.Services.GetService(typeof(IHighlighter)) as IHighlighter;
            HighlightedLine result = documentHighlighter.HighlightLine(CurrentContext.Document.GetLineByOffset(offset).LineNumber);
            return result.Sections.Any(s => s.Offset <= offset && s.Offset + s.Length >= offset && (s.Color.Name == "Comment" || s.Color.Name == "String" || s.Color.Name == "Char"));
        }

        private void TheWhile(string text, string str, int lineStartOffset, TYPE type)
        {
            int index, start = 0, len = str.Length, tLen = text.Length;
            bool Continue = false, Short = false;

            while ((index = text.IndexOf(str, start)) >= 0 && index < tLen)
            {
                Short = tLen <= (index + len);
                if (index > 0)
                {
                    if (ValidChars.Contains(CurrentContext.Document.GetCharAt(lineStartOffset + index - 1)))
                    {
                        Continue = Short ? true : ValidChars.Contains(CurrentContext.Document.GetCharAt(index + lineStartOffset + len));
                    }
                    else
                    {
                        Continue = false;
                    }
                }
                else
                {
                    Continue = Short ? true : ValidChars.Contains(CurrentContext.Document.GetCharAt(index + lineStartOffset + len));
                }

                if (Continue && !IsInStringComment(index + lineStartOffset))
                {
                    base.ChangeLinePart(
                        lineStartOffset + index, // startOffset
                        lineStartOffset + index + len, // endOffset
                        (VisualLineElement element) =>
                        {
                            element.TextRunProperties.SetForegroundBrush(GetColor[type]);
                        });
                }
                start = index + 1;
            }
        }
    }
}
