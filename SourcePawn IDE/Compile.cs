using System.IO;
using System.Reflection;
using ICSharpCode.AvalonEdit.Document;
using System.Diagnostics;

namespace SourcePawn_IDE
{
    public static class Compile
    {
        public static string Run(TextDocument doc, string SaveLoc)
        {
            string BasePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".sp");
            using (TextWriter tw = new StreamWriter(path, false))
            {
                tw.Write(doc.Text);
            }

            string CompilerPath = Path.Combine(BasePath, "\\spcomp.exe");
            string IncludePath = Path.Combine(BasePath, "\\include");
            Process p = new Process();
            p.StartInfo.FileName = BasePath + "\\spcomp.exe";
            p.StartInfo.Arguments = string.Format("\"{0}\" /i\"{2}\" /o\"{1}\"", path, SaveLoc, IncludePath);
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.Start();

            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            File.Delete(path);

            return output;   
        }
    }
}
