using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chocolatey.bootstrapper
{
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;

    class Program
    {
        // This application exists soely to launch chocolatey as admin and pass across whatever input parameters it is passed
        static void Main(string[] args)
        {
            var basePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            // Basepath will only be null if the entry assembly is Native (c/c++)
            if (basePath != null)
            {
                var chocoPath = Path.Combine(basePath, "choco.exe");
                if (File.Exists(chocoPath) && args.Length > 0)
                {
                    var arguments = args.Aggregate((a, b) => a + " " + b);
                    Process.Start(chocoPath, arguments);
                }
            }
        }
    }
}
