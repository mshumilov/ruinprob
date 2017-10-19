using System;
using System.Diagnostics;
using System.IO;
using MinimizeRuinProbability.Model;

namespace MinimizeRuinProbability.Helpers
{
    public static class AppHelper
    {
        /// <summary>
        /// Should be called once on app start.
        /// </summary>
        public static void EnableLogging()
        {
            if (!Directory.Exists(Config.ConfigsRootDir))
                Directory.CreateDirectory(Config.ConfigsRootDir);

            Trace.Listeners.Clear();
            TextWriterTraceListener twtl =
                new TextWriterTraceListener(Path.Combine(Config.ConfigsRootDir, AppDomain.CurrentDomain.FriendlyName + ".log"))
                {
                    Name = "TextLogger",
                    TraceOutputOptions = TraceOptions.ThreadId | TraceOptions.DateTime
                };
            ConsoleTraceListener ctl = new ConsoleTraceListener(false) {TraceOutputOptions = TraceOptions.DateTime};

            Trace.Listeners.Add(twtl);
            Trace.Listeners.Add(ctl);
            Trace.AutoFlush = true;
        }


        public static void UseDotAsDecimalSeparatorInStrings()
        {
            System.Globalization.CultureInfo customCulture =
                (System.Globalization.CultureInfo) System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;
        }

        /// <summary>
        /// Create default input files if they are not created yet.
        /// </summary>
        public static void InitializeInputFilesIfNotExists()
        {
            if (!Directory.Exists(Config.ConfigsRootDir))
                Directory.CreateDirectory(Config.ConfigsRootDir);

            InitializeAgeProbsFileIfNotExists();
            InitializeControlFileIfNotExists();
        }

        private static void InitializeAgeProbsFileIfNotExists()
        {
            var filePath = Path.Combine(Config.ConfigsRootDir, Config.AgeProbFile);
            if (File.Exists(filePath))
                return;
            File.WriteAllText(filePath, Properties.Resources.ageprobs);
        }

        private static void InitializeControlFileIfNotExists()
        {
            var filePath = Path.Combine(Config.ConfigsRootDir, Config.ControlFile);
            if (File.Exists(filePath))
                return;
            File.WriteAllText(filePath, Properties.Resources.control);
        }
    }
}
