using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.CodeCompletion;
using System.Reflection;
using System.IO;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Microsoft.Win32;
using System.Windows.Threading;
using System.ComponentModel;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Rendering;

namespace SourcePawn_IDE
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        delegate void UpdateHighlighting(bool force);
        bool IsUpdating = false;

        SourcePawnHighlightRules rules = new SourcePawnHighlightRules();
        IHighlightingDefinition SPDefinition = null;
        List<List<int>> errors = new List<List<int>>();
        List<List<string>> strErrors = new List<List<string>>();
        List<string> filePaths = new List<string>();
        Dictionary<string, List<string>> includes = new Dictionary<string, List<string>>();
        List<IncInfo> FullInfo = new List<IncInfo>();
        int n = 1;
        
        List<List<string>> SelectionIncludes = new List<List<string>>();

        CompletionWindow window;
        IList<ICompletionData> autocomplete;
        Dictionary<string, TreeViewItem> FuncInfo = new Dictionary<string, TreeViewItem>();
        Dictionary<string, TreeViewItem> ForwardInfo = new Dictionary<string, TreeViewItem>();
        bool acIsVisible = false;

        string filter = "SourcePawn file (*.sp)|*.sp|Include file (*.inc)|*.inc|All files (*.*)|*.*";
        List<char> ValidSpaces = new List<char>(" \t\n\r:;,=");
        List<char> ValidChars = new List<char>(" \t\n\r(),[]{}");

        List<bool> Saved = new List<bool>();

        ContextMenu menu = new ContextMenu();

        List<FoldingManager> fm = new List<FoldingManager>();
        FoldingStrategy fold = new FoldingStrategy();

        ToolTip toolTip = new ToolTip();

        int OpenBracketOffset = -1;
        
        public MainWindow()
        {
            InitializeComponent();

            menu.Items.Add(new MenuItem() { Header = "Comment selection" });
            (menu.Items[0] as MenuItem).Click += (Se, Ea) =>
            {
                TextEditor te = (TextEditors.SelectedContent as TextEditor);
                if (te.SelectionLength > 0)
                {
                    te.Document.Insert(te.SelectionStart, "/*");
                    te.Document.Insert(te.SelectionStart + te.SelectionLength, "*/");
                    te.Select(te.SelectionStart, te.SelectionLength - 2);
                }
            };
            menu.Items.Add(new MenuItem() { Header = "Comment line from caret" });
            (menu.Items[1] as MenuItem).Click += (Se, Ea) =>
            {
                TextEditor te = (TextEditors.SelectedContent as TextEditor);
                te.Document.Insert(te.CaretOffset, "//");
            };
            menu.Items.Add(new MenuItem() { Header = "Comment line(s) selected" });
            (menu.Items[2] as MenuItem).Click += (Se, Ea) =>
            {
                TextEditor te = (TextEditors.SelectedContent as TextEditor);
                int SelStart = te.SelectionStart;
                int SelLength = te.SelectionLength;
                DocumentLine line = te.Document.GetLineByOffset(SelStart);
                DocumentLine endline = te.Document.GetLineByOffset(SelStart + SelLength);
                if (line == endline)
                {
                    te.Document.Insert(te.Document.GetLineByOffset(SelStart).Offset, "//");
                }
                else
                {
                    do
                    {
                        te.Document.Insert(line.Offset, "//");
                    } while ((line = line.NextLine) != endline);
                    te.Document.Insert(line.Offset, "//");
                }
            };

            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                using (Stream s = this.GetType().Assembly.GetManifestResourceStream("SourcePawn_IDE.SP.xshd"))
                {
                    XmlReader reader = XmlReader.Create(s);

                    XshdSyntaxDefinition xshd = HighlightingLoader.LoadXshd(reader);
                    SPDefinition = HighlightingLoader.Load(xshd, HighlightingManager.Instance);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            LoadIncludes li = new LoadIncludes();
            includes = li.includes;
            FullInfo = li.FullInfo;
            foreach (IncInfo info in FullInfo)
            {
                switch (info.Type)
                {
                    case Type.Define:
                        rules.Define.Add(info.Value);
                        (FunctionView.Items[0] as TreeViewItem).Items.Add(info.Value);
                        break;
                    case Type.Enum:
                        if (info.Function == "{Unidentified}")
                        {
                            foreach (string val in info.EnumValues)
                            {
                                rules.Define.Add(val);
                                ((FunctionView.Items[6] as TreeViewItem).Items[0] as TreeViewItem).Items.Add(val);
                            }
                        }
                        else
                        {
                            TreeViewItem ti = new TreeViewItem();
                            ti.Header = info.Function;
                            foreach (string val in info.EnumValues)
                            {
                                rules.Define.Add(val);
                                ti.Items.Add(val);
                            }

                            (FunctionView.Items[6] as TreeViewItem).Items.Add(ti);
                        }
                        break;
                    case Type.Forward:
                        TreeViewItem item = new TreeViewItem();
                        item.Header = info.Function;
                        if (!string.IsNullOrWhiteSpace(info.FunctionInfo))
                        {
                            ToolTip tip = new ToolTip();
                            tip.Content = info.FunctionInfo;
                            item.ToolTip = tip;
                        }
                        (FunctionView.Items[2] as TreeViewItem).Items.Add(item);
                        if (!ForwardInfo.ContainsKey(info.Function))
                        {
                            ForwardInfo.Add(info.Function, item);
                        }
                        break;
                    case Type.Function:
                        item = new TreeViewItem();
                        item.Header = info.Function;
                        item.Tag = info.Line;
                        if (!string.IsNullOrWhiteSpace(info.FunctionInfo))
                        {
                            ToolTip tip = new ToolTip();
                            tip.Content = info.FunctionInfo;
                            item.ToolTip = tip;
                        }
                        (FunctionView.Items[4] as TreeViewItem).Items.Add(item);
                        if (!FuncInfo.ContainsKey(info.Function))
                        {
                            FuncInfo.Add(info.Function, item);
                        }
                        break;
                }
            }

            string[] args = Environment.GetCommandLineArgs();
            bool fileopened = false;
            for (int i = 1; i < args.Length; i++)
            {
                string arg = args[i];
                if (File.Exists(arg))
                {
                    try
                    {
                        using (StreamReader sr = new StreamReader(arg))
                        {
                            NewDocument(sr.ReadToEnd(), System.IO.Path.GetFileName(arg));
                            fileopened = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
            if (!fileopened)
            {
                NewDocument(SimpleGen(), "New " + n++.ToString());
            }
        }

        private void ErrorBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ErrorBox.SelectedIndex > -1)
            {
                ((TextEditors.SelectedItem as TabItem).Content as TextEditor).ScrollToLine(errors[TextEditors.SelectedIndex][ErrorBox.SelectedIndex]);
            }
        }

        private void NewDocument(string Script, string Header)
        {
            int index = -1;
            TabItem item = null;
            TextEditor te = new TextEditor();

            SourcePawnHighlightRules rules2 = new SourcePawnHighlightRules();
            te.SyntaxHighlighting = SPDefinition;
            te.TextArea.TextView.LineTransformers.Add(rules2);
            te.Document.Insert(0, Script);
            te.FontSize = 15.75;
            te.FontFamily = new FontFamily("Consolas");
            te.ShowLineNumbers = true;
            te.TextArea.TextEntered += new TextCompositionEventHandler(TextArea_TextEntered);
            te.PreviewKeyDown+=new KeyEventHandler(te_PreviewKeyDown);
            te.TextChanged += (Se, Ea) =>
            {
                fold.UpdateFoldings(fm[TextEditors.SelectedIndex], (TextEditors.SelectedContent as TextEditor).Document);
                if (Saved[TextEditors.SelectedIndex])
                {
                    Saved[TextEditors.SelectedIndex] = false;
                    (TextEditors.SelectedItem as TabItem).Header += " *";
                }
            };
            te.TextArea.TextEntering += (Sa, Ea) =>
            {
                if (Ea.Text == "(" && acIsVisible)
                {
                    window.CompletionList.RequestInsertion(Ea);
                }
            };
            te.LostFocus += (Sa, Ea) =>
            {
                toolTip.IsOpen = false;
                OpenBracketOffset = -1;
            };
            te.TextArea.Caret.PositionChanged += (Se, Ea) =>
            {
                UpdateToolTip();
            };
            te.ContextMenu = menu;

            item = new TabItem();
            item.Content = te;
            item.Header = Header;

            index = TextEditors.Items.Add(item);
            errors.Add(new List<int>());
            strErrors.Add(new List<string>());
            filePaths.Add(null);
            SelectionIncludes.Add(new List<string>());
            Saved.Add(true);

            FoldingManager foldmanager = FoldingManager.Install(te.TextArea);
            fold.UpdateFoldings(foldmanager, te.Document);
            fm.Add(foldmanager);

            if (index != -1)
            {
                TextEditors.SelectedIndex = index;
            }
            te.Focus();
            UpdateIncludes(false);
        }

        private void UpdateToolTip()
        {
            if (OpenBracketOffset >= 0 && toolTip.IsOpen)
            {
                TextEditor te = TextEditors.SelectedContent as TextEditor;
                DocumentLine line = te.Document.GetLineByOffset(te.CaretOffset);
                if (te.Document.GetLineByOffset(OpenBracketOffset).LineNumber == line.LineNumber)
                {
                    if (te.CaretOffset < OpenBracketOffset || (te.CaretOffset < te.Text.Length ? te.Document.GetCharAt(te.CaretOffset - 1) == ')' : false))
                    {
                        toolTip.IsOpen = false;
                        OpenBracketOffset = -1;
                    }
                }
                else
                {
                    toolTip.IsOpen = false;
                    OpenBracketOffset = -1;
                }
            }
        }

        private void TextArea_TextEntered(object sender, TextCompositionEventArgs e)
        {
            TextEditor te = (TextEditors.SelectedContent as TextEditor);
            if ((te.SelectionStart > 1 ? ValidChars.Contains(te.Text[te.SelectionStart - 2]) : true) && (te.SelectionStart < te.Text.Length ? ValidChars.Contains(te.Text[te.SelectionStart]) : true) && !IsInCommentString())
            {
                if (char.IsLetter(e.Text[0]) && !acIsVisible)
                {
                    acIsVisible = true;
                    window = new CompletionWindow(te.TextArea);
                    CopyTo(autocomplete, window.CompletionList.CompletionData);
                    window.Show();
                    window.Closed += delegate
                    {
                        acIsVisible = false;
                        window = null;
                    };
                }
            }
            if (e.Text == "(")
            {
                OpenBracketOffset = te.CaretOffset;
                int idx = te.SelectionStart;
                while (--idx > 0 && !ValidSpaces.Contains(te.Document.GetCharAt(idx)))
                {
                }
                TreeViewItem func;
                if (idx > 0)
                {
                    if (FuncInfo.TryGetValue(te.Text.Substring(idx + 1, te.CaretOffset - idx - 2), out func))
                    {
                        Point p = te.TextArea.TextView.GetVisualPosition(te.TextArea.Caret.Position, VisualYPosition.LineBottom) - te.TextArea.TextView.ScrollOffset;
                        toolTip.HorizontalOffset = p.X;
                        toolTip.VerticalOffset = p.Y;
                        toolTip.Placement = System.Windows.Controls.Primitives.PlacementMode.Relative;
                        toolTip.PlacementTarget = te;
                        toolTip.FontSize = 12;
                        toolTip.Content = func.Tag;
                        toolTip.IsOpen = true;
                    }
                }
            }
            fold.UpdateFoldings(fm[TextEditors.SelectedIndex], te.Document);
        }

        private bool IsInCommentString()
        {
            TextEditor te = (TextEditors.SelectedContent as TextEditor);

            int off = te.SelectionStart;

            IHighlighter documentHighlighter = te.TextArea.GetService(typeof(IHighlighter)) as IHighlighter;
            HighlightedLine result = documentHighlighter.HighlightLine(te.Document.GetLineByOffset(off).LineNumber);
            return result.Sections.Any(s => s.Offset <= off && s.Offset + s.Length >= off && (s.Color.Name == "Comment" || s.Color.Name == "String" || s.Color.Name == "Char"));
        }

        private void CopyTo(IList<ICompletionData> lista, IList<ICompletionData> listb)
        {
            for (int i = 0; i < lista.Count; i++)
            {
                listb.Add(lista[i]);
            }
            listb.Add(new CompletionString("public"));
            listb.Add(new CompletionString("stock"));
            listb.Add(new CompletionString("native"));
            listb.Add(new CompletionString("enum"));
            listb.Add(new CompletionString("static"));
            listb.Add(new CompletionString("decl"));
            listb.Add(new CompletionString("new"));
            listb.Add(new CompletionString("bool"));
            listb.Add(new CompletionString("Float"));
            listb.Add(new CompletionString("String"));
            listb.Add(new CompletionString("else"));
            listb.Add(new CompletionString("if"));
            listb.Add(new CompletionString("switch"));
            listb.Add(new CompletionString("case"));
            listb.Add(new CompletionString("default"));
            listb.Add(new CompletionString("do"));
            listb.Add(new CompletionString("for"));
            listb.Add(new CompletionString("while"));
            listb.Add(new CompletionString("break"));
            listb.Add(new CompletionString("continue"));
            listb.Add(new CompletionString("return"));
            listb.Add(new CompletionString("true"));
            listb.Add(new CompletionString("false"));
        }

        private void te_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!IsUpdating)
            {
                IsUpdating = true;
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork+=new DoWorkEventHandler(worker_DoWork);
                worker.RunWorkerAsync();
            }
            if (e.Key == Key.Space && acIsVisible)
            {
                window.Close();
            }
            else if (e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Right)
            {
                UpdateToolTip();
            }
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            this.Dispatcher.BeginInvoke(new UpdateHighlighting(UpdateIncludes), DispatcherPriority.Send, false);
        }

        private void worker_DoWork2(object sender, DoWorkEventArgs e)
        {
            this.Dispatcher.BeginInvoke(new UpdateHighlighting(UpdateIncludes), DispatcherPriority.Send, true);
        }

        private void UpdateIncludes(bool force)
        {
            if (TextEditors.SelectedIndex > -1)
            {
                TextEditor te = (TextEditors.SelectedContent as TextEditor);
                SourcePawnHighlightRules rule = (te.TextArea.TextView.LineTransformers[1] as SourcePawnHighlightRules);
                string file = te.Text;
                int index = 0;

                List<string> incs = new List<string>();


                while ((index = file.IndexOf("#include ", index)) >= 0)
                {
                    index += 8;
                    StringBuilder builder = new StringBuilder();
                    char c;
                    while (++index < file.Length && !ValidSpaces.Contains((c = file[index])))
                    {
                        if (c == '<' || c == '>')
                            continue;
                        builder.Append(c);
                    }
                    builder.Append(".inc");

                    if (includes.ContainsKey(builder.ToString()))
                    {
                        incs.Add(builder.ToString());
                    }
                }

                if (!ListsEqual(incs, SelectionIncludes[TextEditors.SelectedIndex]) || force)
                {
                    window = new CompletionWindow(te.TextArea);

                    rule.Define.Clear();
                    rule.KeyWords1.Clear();
                    rule.KeyWords2.Clear();
                    (FunctionView.Items[1] as TreeViewItem).Items.Clear();
                    (FunctionView.Items[3] as TreeViewItem).Items.Clear();
                    (FunctionView.Items[5] as TreeViewItem).Items.Clear();
                    (FunctionView.Items[7] as TreeViewItem).Items.Clear();
                    (FunctionView.Items[7] as TreeViewItem).Items.Add(new TreeViewItem() { Header = "Unidentified" });

                    foreach (string inc in incs)
                    {
                        AddToRules(inc, rule);
                    }

                    te.TextArea.TextView.Redraw();
                    FunctionView.InvalidateVisual();
                    SelectionIncludes[TextEditors.SelectedIndex] = incs;
                    autocomplete = window.CompletionList.CompletionData;
                    window.Close();
                }
                IsUpdating = false;
            }
            else
            {
                (FunctionView.Items[1] as TreeViewItem).Items.Clear();
                (FunctionView.Items[3] as TreeViewItem).Items.Clear();
                (FunctionView.Items[5] as TreeViewItem).Items.Clear();
                (FunctionView.Items[7] as TreeViewItem).Items.Clear();
                (FunctionView.Items[7] as TreeViewItem).Items.Add(new TreeViewItem() { Header = "Unidentified" });
            }
        }

        private bool ListsEqual(List<string> lista, List<string> listb)
        {
            if (lista.Count == listb.Count)
            {
                lista.Sort();
                listb.Sort();
                for (int i = 0; i < lista.Count; i++)
                {
                    if (lista[i] != listb[i])
                        return false;
                }
                return true;
            }
            return false;
        }

        private void AddToRules(string file, SourcePawnHighlightRules rule)
        {
            List<string> inc = includes[file];
            foreach (IncInfo info in FullInfo)
            {
                if (inc.Contains(info.file) || info.file == file)
                {
                    switch (info.Type)
                    {
                        case Type.Define:
                            if (!(FunctionView.Items[1] as TreeViewItem).Items.Contains(info.Value))
                            {
                                rule.Define.Add(info.Value);
                                (FunctionView.Items[1] as TreeViewItem).Items.Add(info.Value);
                            }
                            break;
                        case Type.Enum:
                            if (!(FunctionView.Items[7] as TreeViewItem).Items.Contains(info.Function))
                            {
                                if (info.Function == "{Unidentified}")
                                {
                                    foreach (string val in info.EnumValues)
                                    {
                                        if (!((FunctionView.Items[7] as TreeViewItem).Items[0] as TreeViewItem).Items.Contains(val))
                                        {
                                            rule.Define.Add(val);
                                            ((FunctionView.Items[7] as TreeViewItem).Items[0] as TreeViewItem).Items.Add(val);
                                        }
                                    }
                                }
                                else
                                {
                                    TreeViewItem ti = new TreeViewItem();
                                    ti.Header = info.Function;
                                    rule.KeyWords2.Add(info.Function);
                                    foreach (string val in info.EnumValues)
                                    {
                                        rule.Define.Add(val);
                                        ti.Items.Add(val);
                                    }
                                    (FunctionView.Items[7] as TreeViewItem).Items.Add(ti);
                                }
                            }
                            break;
                        case Type.Forward:
                            if (!(FunctionView.Items[3] as TreeViewItem).Items.Contains(info.Function))
                            {
                                TreeViewItem tvi = new TreeViewItem();
                                if (ForwardInfo.TryGetValue(info.Function, out tvi))
                                {
                                    TreeViewItem item = new TreeViewItem();
                                    item.Header = tvi.Header;
                                    item.ToolTip = tvi.ToolTip;
                                    (FunctionView.Items[3] as TreeViewItem).Items.Add(item);
                                }
                                else
                                {
                                    (FunctionView.Items[3] as TreeViewItem).Items.Add(info.Function);
                                }
                                window.CompletionList.CompletionData.Add(new CompletionString(info.Function));
                            }
                            break;
                        case Type.Function:
                            if (!(FunctionView.Items[5] as TreeViewItem).Items.Contains(info.Function))
                            {
                                TreeViewItem tvi = new TreeViewItem();
                                if (FuncInfo.TryGetValue(info.Function, out tvi))
                                {
                                    TreeViewItem item = new TreeViewItem();
                                    item.Tag = tvi.Tag;
                                    item.Header = tvi.Header;
                                    item.ToolTip = tvi.ToolTip;
                                    (FunctionView.Items[5] as TreeViewItem).Items.Add(item);
                                }
                                else
                                {
                                    (FunctionView.Items[5] as TreeViewItem).Items.Add(info.Function);
                                }
                                window.CompletionList.CompletionData.Add(new CompletionString(info.Function));
                            }
                            break;
                    }
                }
            }
        }

        private void SimpleScript_Click(object sender, RoutedEventArgs e)
        {
            NewDocument(SimpleGen(), "New " + n++.ToString());
        }

        private void GenScript_Click(object sender, RoutedEventArgs e)
        {
            ScriptGenerator script = new ScriptGenerator();
            if (script.ShowDialog().GetValueOrDefault(false))
            {
                NewDocument(script.Script, "New " + n++.ToString());
            }
        }

        private void CompileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (TextEditors.SelectedIndex > -1)
            {
                ErrorBox.Items.Clear();
                strErrors[TextEditors.SelectedIndex].Clear();
                errors[TextEditors.SelectedIndex].Clear();
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "SourceMod plugin file (*.smx)|*.smx";
                if (sfd.ShowDialog().GetValueOrDefault(false))
                {
                    AddErrors(Compile.Run((TextEditors.SelectedContent as TextEditor).Document, sfd.FileName));
                }
            }
        }

        private void AddErrors(string error)
        {
            string[] lines = error.Split('\n');
            foreach (string str in lines)
            {
                int index = str.IndexOf(".sp(");
                if (index >= 0)
                {
                    index += 4;
                    int endIndex = str.IndexOf(')', index);
                    if (endIndex >= 0)
                    {
                        string subStr = str.Substring(index, endIndex - index);
                        int line;
                        if (int.TryParse(subStr, out line))
                        {
                            errors[TextEditors.SelectedIndex].Add(line);
                        }
                        strErrors[TextEditors.SelectedIndex].Add(str);
                        ErrorBox.Items.Add(str);
                    }
                }
            }
            UpdateErrors();
        }

        private void TextEditors_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_DoWork2);
            worker.RunWorkerAsync();
            UpdateErrors();
        }

        private void UpdateErrors()
        {
            if (TextEditors.SelectedIndex > -1)
            {
                rules.ErrorLine.Clear();
                foreach (int i in errors[TextEditors.SelectedIndex])
                {
                    rules.ErrorLine.Add(i);
                }

                ((TextEditors.SelectedItem as TabItem).Content as TextEditor).TextArea.TextView.LineTransformers[1] = rules;
                ((TextEditors.SelectedItem as TabItem).Content as TextEditor).InvalidateVisual();

                ErrorBox.Items.Clear();
                foreach (string i in strErrors[TextEditors.SelectedIndex])
                {
                    ErrorBox.Items.Add(i);
                }
            }
            else
            {
                ErrorBox.Items.Clear();
                rules.ErrorLine.Clear();
            }
        }

        private string SimpleGen()
        {
            return "/* Script generated by SourcePawn IDE */\n\n#include <sourcemod>\n\n#define PLUGIN_VERSION \"1.0.0.0\"\n\npublic Plugin:myinfo =\n{\n    name = \"\",\n    author = \"unknown\",\n    description = \"none\",\n    version = PLUGIN_VERSION,\n    url = \"http://sourcemod.net\"\n}\n\npublic OnPluginStart()\n{\n    CreateConVar(\"_version\", PLUGIN_VERSION, \"\", FCVAR_PLUGIN|FCVAR_NOTIFY|FCVAR_DONTRECORD);\n}";
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = filter;
            ofd.Multiselect = false;
            if (ofd.ShowDialog().GetValueOrDefault(false))
            {
                using (StreamReader reader = new StreamReader(ofd.FileName))
                {
                    NewDocument(reader.ReadToEnd(), ofd.SafeFileName);
                    filePaths[TextEditors.SelectedIndex] = ofd.FileName;
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (TextEditors.SelectedIndex > -1)
            {
                string path = filePaths[TextEditors.SelectedIndex];
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    Save();
                }
                else
                {
                    SaveDialog();
                }
            }
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveDialog();
        }

        private void SaveAll_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < TextEditors.Items.Count; i++)
            {
                if (!Saved[i])
                {
                    string path = filePaths[i];
                    if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    {
                        Save(i);
                    }
                    else
                    {
                        SaveDialog(i);
                    }
                }
            }
        }

        private void SaveDialog(int index = -1)
        {
            int idx = index == -1 ? TextEditors.SelectedIndex : index;
            if (idx > -1)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Title = (string)(TextEditors.Items[idx] as TabItem).Header;
                sfd.Filter = filter;
                if (sfd.ShowDialog().GetValueOrDefault(false))
                {
                    using (StreamWriter writer = new StreamWriter(sfd.FileName))
                    {
                        writer.Write(((TextEditors.Items[idx] as TabItem).Content as TextEditor).Text);
                    }
                    (TextEditors.Items[idx] as TabItem).Header = sfd.SafeFileName;
                    filePaths[idx] = sfd.FileName;
                    Saved[idx] = true;
                }
            }
        }

        private void Save(int index = -1)
        {
            int idx = index == -1 ? TextEditors.SelectedIndex : index;
            if (idx > -1)
            {
                using (StreamWriter writer = new StreamWriter(filePaths[idx]))
                {
                    writer.Write(((TextEditors.Items[idx] as TabItem).Content as TextEditor).Text);
                }
                (TextEditors.Items[idx] as TabItem).Header = System.IO.Path.GetFileName(filePaths[idx]);
                Saved[idx] = true;
            }
        }

        private void FunctionView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (TextEditors.SelectedIndex > -1)
            {
                TextEditor te = (TextEditors.SelectedContent as TextEditor);
                if (FunctionView.SelectedItem is TreeViewItem)
                {
                    te.Document.Replace(te.SelectionStart, te.SelectionLength, (string)((FunctionView.SelectedItem as TreeViewItem).Header) ?? "");
                }
                else
                {
                    te.Document.Replace(te.SelectionStart, te.SelectionLength, (string)(FunctionView.SelectedItem) ?? "");
                }
            }
        }

        private void search_Click(object sender, RoutedEventArgs e)
        {
            SearchReplace sr = new SearchReplace(true);
            sr.SearchText += new EventHandler<SearchTextEventArgs>(sr_SearchText);
            sr.Show();
        }

        private void replace_Click(object sender, RoutedEventArgs e)
        {
            SearchReplace sr = new SearchReplace(false);
            sr.SearchText += new EventHandler<SearchTextEventArgs>(sr_SearchText);
            sr.Show();
        }

        private void sr_SearchText(object sender, SearchTextEventArgs e)
        {
            if (TextEditors.SelectedIndex > -1)
            {
                TextEditor te = (TextEditors.SelectedContent as TextEditor);
                int index = 0, sIndex = te.SelectionStart, count = 0;
                if (e.Text == "")
                {
                    MessageBox.Show("Please enter a search term");
                    return;
                }
                if (e.All)
                {
                    while ((index = te.Text.IndexOf(e.Text, index, e.CaseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase)) != -1)
                    {
                        te.Select(index, e.Text.Length);
                        te.SelectedText = e.Replace;
                        index++;
                        count++;
                    }
                    MessageBox.Show(string.Format("Replaced all occurrences of {0} with {1}\n\nThe total count was {2}", e.Text, e.Replace, count));
                }
                else
                {
                    if (e.Replace != null)
                    {
                        if (te.SelectedText == e.Text)
                        {
                            te.SelectedText = e.Replace;
                        }
                    }
                    if (e.Direction == 0)
                    {
                        if (te.SelectedText.Equals(e.Text, e.CaseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase))
                        {
                            if (++sIndex < te.Text.Length)
                            {
                                index = te.Text.IndexOf(e.Text, sIndex, e.CaseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase);
                            }
                        }
                        else
                        {
                            index = te.Text.IndexOf(e.Text, --sIndex, e.CaseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase);
                        }

                        if (index == -1 || sIndex == te.Text.Length)
                        {
                            index = te.Text.IndexOf(e.Text, e.CaseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase);
                        }
                    }
                    else
                    {
                        if (--sIndex != -1)
                        {
                            index = te.Text.LastIndexOf(e.Text, sIndex, e.CaseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase);
                        }

                        if (index == -1 || sIndex == -1)
                        {
                            index = te.Text.LastIndexOf(e.Text, e.CaseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase);
                        }
                    }

                    if (index == -1)
                    {
                        MessageBox.Show(string.Format("No occurrences of: {0}", e.Text));
                    }
                    else
                    {
                        te.Select(index, e.Text.Length);
                        te.ScrollToLine(te.Document.GetLineByOffset(index).LineNumber);
                    }
                }
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            for (int i = 0; i < TextEditors.Items.Count; i++)
            {
                if (!Saved[i])
                {
                    string name = (TextEditors.Items[i] as TabItem).Header.ToString().Replace(" *", "");
                    switch (MessageBox.Show("Do you wanna save the changes in " + name + "?", "Save " + name + "?", MessageBoxButton.YesNoCancel))
                    {
                        case MessageBoxResult.OK:
                            SaveDialog(i);
                            break;
                        case MessageBoxResult.Cancel:
                            e.Cancel = true;
                            return;
                    }
                }
            }
            Application.Current.Shutdown();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            int idx = TextEditors.SelectedIndex;
            if (idx >= 0)
            {
                if (!Saved[idx])
                {
                    switch (MessageBox.Show("Do you wanna save your changes to " + (TextEditors.SelectedItem as TabItem).Header.ToString().Replace(" *", "") + "?", "Save", MessageBoxButton.YesNoCancel))
                    {
                        case MessageBoxResult.Yes:
                            SaveDialog();
                            break;
                        case MessageBoxResult.Cancel:
                            return;
                    }
                }
                TextEditors.Items.RemoveAt(idx);
                errors.RemoveAt(idx);
                strErrors.RemoveAt(idx);
                filePaths.RemoveAt(idx);
                Saved.RemoveAt(idx);
                SelectionIncludes.RemoveAt(idx);
            }
        }
    }
}
