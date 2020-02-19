using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EchoAutoTest
{
    public static class Logger
    {
        public static bool hasError = false;

        public static void Error(string message, string app)
        {
            if (!hasError)
            {
                hasError = true;
            }
            WriteEntry(message, "ERROR", app);
        }

        public static void Warning(string message, string app)
        {
            WriteEntry(message, "WARNING", app);
        }

        public static void Info(string message, string app)
        {
            WriteEntry(message, "INFO", app);
        }

        private static void WriteEntry(string message, string type, string app)
        {
            Console.WriteLine($"{type} FROM [{app}] : {message}");
            using (var sw = new StreamWriter(app, true))
            {
                sw.WriteLineAsync($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}  {type}  {message}");
            }
        }

    }
}
