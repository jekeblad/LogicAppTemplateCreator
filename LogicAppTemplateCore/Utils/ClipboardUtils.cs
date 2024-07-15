using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicAppTemplateExtractor.Utils
{
    public static class ClipboardUtils
    {
        public static string GetClipboardText()
        {
            // Windows-specific command to get clipboard content
            string command = "powershell.exe";
            string args = "Get-Clipboard";

            using (Process process = new Process())
            {
                process.StartInfo.FileName = command;
                process.StartInfo.Arguments = args;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return output.Trim();
            }
        }
    }
}
