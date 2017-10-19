using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MinimizeRuinProbability.Helpers;

namespace MinimizeRuinProbability
{
    class Program
    {
        static void Main(string[] args)
        {
            AppHelper.EnableLogging();
            AppHelper.UseDotAsDecimalSeparatorInStrings();

            var startTime = DateTime.Now;

            try
            {
                if (args.Length > 1)
                {
                    Trace.WriteLine(
                        "ERROR: Parameter misspecification. Incorrect # of parameters to the executable (expecting zero or one...).");
                    Trace.WriteLine("EXITING...main()...");
                    Console.Read();
                    Environment.Exit(1);
                }

                var concurrency = args.Length == 1? int.Parse(args[0]) : 0;
                // When concurrency==0, replace with the # of independent processing units on the computer running the application.
                // Note that main() runs in its own thread but that is not accounted for here as it sits idle.
                if (concurrency == 0)
                    concurrency = Environment.ProcessorCount + 1;
                if (concurrency == 1)
                    concurrency++; // If just one processing unit use 2 threads.

                AppHelper.InitializeInputFilesIfNotExists();

                Model.MinimizeRuinProbability.Calculate(concurrency);

            }
            finally
            {
                Trace.WriteLine("");
                Trace.WriteLine($"Time spent: {DateTime.Now - startTime:hh\\:mm\\:ss}");
                Trace.WriteLine("");
                Trace.WriteLine("Press Enter to exit...");
                Console.ReadLine();
            }
        }
    }
}
