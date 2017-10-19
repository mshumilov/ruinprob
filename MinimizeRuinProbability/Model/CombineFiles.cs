/*
/ Copyright (c) 2014 Chris Rook, All Rights Reserved.
/
/ Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
/
/ 1. Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
/ 2. Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the
/ documentation and/or other materials provided with the distribution.
/ 3. Neither the name of the copyright holder nor the names of its contributors may be used to endorse or promote products derived from this
/ software without specific prior written permission.
/
/ THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED
/ TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR
/ CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
/ PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
/ LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
/ EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE. (Source: http://opensource.org/licenses/BSD-3-Clause.)
/
/ Filename: combfiles.cpp
/
/ Function: combfiles()
/
/ Summary:
/
/ Function to aggregate the individual bucket-specific output files written by optimize() into single files for each time point. These files are
/ subsequently read by getprprobs() when processing the next time point. At the last time point (i.e., year=0) all files for the individual time
/ points are aggregated into 3 files then deleted. The first is a (space-delimited) vertical file containing all data for all time points. This
/ file is named FinalResults_V.txt. The remaining 2 files are comma-separated horizontal files for import into a spreadsheet, and these files are
/ named FinalAlphaResults_H.csv and FinalProbResults_H.csv.
/
/ Parameters:
/
/ 1.) Root directory where input files reside and output files are written.
/ 2.) Current year/time point number being processed. (Years start at 0 and end at one less than the total # of years being processed by the given
/ arrangement specified in the control file.)
/ 3.) Number of concurrent processes used to derive the optimal values for each time point.
/ 4.) Number of buckets processed per thread within each time point. (Except the last one. This is specific to each time point.)
/ 5.) Maximum ruin factor value specified in the control file (i.e., RFMax).
/ 6.) Precision value for the ruin factor discretization as specified in the control file (i.e., PR).
/ 7.) Total number of years processed by the arrangement specified in the control file.
/
/ Input Files:
/
/ 1.) For a given time point, input files of the form "Year_X_Buckets_S_thru_E.txt" are read from the root directory. (Where X is the time point,
/ S is the start bucket #, and E is the end bucket #. This function aggregates these individual files into a single file named: Year_X_All_Buckets.txt.
/ 2.) Year_X_All_buckets.txt (Written by this function for each time point and all such files are read by this function and aggregated at the last time
/ point, which is year=0.)
/
/ Output Files:
/
/ 1.) Year_X_All_Buckets.txt, where X reflects the year/time point being processed.
/ 2.) FinalResults_V.txt (Vertical text file of all results from all years processed.)
/ 3.) FinalAlphaResults_H.csv (Horizontal csv file of just optimal alphas. Use this file to simulate and confirm the result.)
/ 4.) FinalProbResults_H.csv (Horizontal csv file of optimal probabilities.)
/
/ Note: The last 3 output files above are only written at the last time point when all processing ends (i.e., year=0). The first file is written at every
/ time point.
/
/ Return Value:
/
/ None.
/------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace MinimizeRuinProbability.Model
{
    public static class CombineFiles
    {
        public static void Run(string root, int curyear, int pllruns, int bcktsprun, double rfmax, int pprec, int nyrs)
        {
            int nbkts = (int) (rfmax * pprec + 0.5);

            var yearFiles = new List<string>();
            for (int j = 0; j < pllruns; ++j)
            {
                var fndfname = j < pllruns - 1
                    ? $"Year_{curyear}_Buckets_{(long) bcktsprun * j + 1}_thru_{(long) bcktsprun * (j + 1)}.txt"
                    : $"Year_{curyear}_Buckets_{(long) bcktsprun * j + 1}_thru_{nbkts}.txt";
                yearFiles.Add(fndfname);
            }

            try
            { 
                CombineMultipleFilesIntoSingleOne(root, yearFiles, $"Year_{curyear}_All_Buckets.txt");
                // remove files concatenated
                RemoveFiles(root, $"Year_{curyear}_Buckets*.txt");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error on attempt to combine files after year={curyear} has failed. {ex.Message}");
                Trace.WriteLine("EXITING...combfiles()...");
                Console.Read();
                Environment.Exit(1);
            }

            // If at last year then build the normalized and transposed final data files and delete all intermediate data files.
            if (curyear == 0)
            {

                yearFiles = new List<string> {$"Year_{curyear}_All_Buckets.txt"};
                for (int j = curyear + 1; j < nyrs; ++j)
                    yearFiles.Add($"Year_{j}_All_Buckets.txt");
                // Build normalized data file.
                try
                {
                    CombineMultipleFilesIntoSingleOne(root, yearFiles, "FinalResults_V.txt");
                    RemoveFiles(root, "Year_*.txt");
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("Error on combine year files: " + ex.Message);
                    Trace.WriteLine("EXITING...combfiles()...");
                    Console.Read();
                    Environment.Exit(1);
                }

                // Build transposed probability/alpha data files.

                var fnlprobs = new double[nyrs][];
                var fnlalphas = new double[nyrs][];
                for (int j = 0; j < nyrs; ++j) // A pointer to any type can hold an array of that type.
                {
                    fnlprobs[j] = new double[nbkts]; // Each array value now points to array of long doubles (2D array).
                    fnlalphas[j] = new double[nbkts]; // Each array value now points to array of doubles (2D array).
                }

                // Open normalized file just created, transpose it and write to csv file.
                try {
                    foreach (var line in File.ReadLines(Path.Combine(root, "FinalResults_V.txt")))
                    {
                        var s = line.Split();
                        if (s.Length != 4)
                            continue;
                        var fnlyear = int.Parse(s[0]);
                        var fnlrf = double.Parse(s[1], CultureInfo.InvariantCulture);
                        var fnlprob = double.Parse(s[2], CultureInfo.InvariantCulture);
                        var fnlalpha = double.Parse(s[3], CultureInfo.InvariantCulture);
                        var fnlbkt = (int) (fnlrf * pprec + 0.5);
                        if (fnlbkt >= 1 && fnlbkt <= (int) (rfmax * pprec + 0.5))
                        {
                            fnlprobs[fnlyear][fnlbkt - 1] = fnlprob;
                            fnlalphas[fnlyear][fnlbkt - 1] = fnlalpha;
                        }
                    }
                }
                catch (IOException ex)
                {
                    Trace.Write("ERROR: Could not read file: ");
                    Trace.WriteLine(root + "FinalResults_V.txt. Message: " + ex.Message);
                    Trace.WriteLine("EXITING...combfiles()...");
                    Console.Read();
                    Environment.Exit(1);
                }

                // Transpose and write to file.
                var sbp = new StringBuilder("RF");
                var sba = new StringBuilder("RF");
                for (int i = 0; i <= nbkts; ++i) // The first line (i=0) of the csv files holds column headers.
                {
                    if (i > 0)
                    {
                        float rf = (float) i / pprec;
                        var str = $"{rf:F10}";
                        sbp.Append(str);
                        sba.Append(str);
                    }
                    for (int j = 0; j < nyrs; ++j)
                    {
                        if (i == 0)
                        {
                            sbp.Append($", Time (t={j})");
                            sba.Append($", Time (t={j})");
                        }
                        else
                        {
                            //precision(50);
                            var str = $",{fnlprobs[j][i - 1]:F50}";
                            sbp.Append(str);
                            sba.Append($",{fnlalphas[j][i - 1]}");
                        }
                    }
                    sbp.AppendLine();
                    sba.AppendLine();
                }
                File.AppendAllText(root + "FinalProbResults_H.csv", sbp.ToString());
                File.AppendAllText(root + "FinalAlphaResults_H.csv", sba.ToString());
            }
        }

        private static void RemoveFiles(string root, string mask)
        {
            var dir = new DirectoryInfo(root);
            foreach (var file in dir.EnumerateFiles(mask))
            {
                try
                {
                    file.Delete();
                }
                catch (Exception) { }
            }
        }

        private static void CombineMultipleFilesIntoSingleOne(string root, IEnumerable<string> inputFilePaths,
            string outputFilePath)
        {
            try
            {
                using (var outputStream = File.Create(Path.Combine(root, outputFilePath)))
                {
                    foreach (var inputFilePath in inputFilePaths)
                    {
                        using (var inputStream = File.OpenRead(Path.Combine(root, inputFilePath)))
                        {
                            // Buffer size can be passed as the second argument.
                            inputStream.CopyTo(outputStream);
                        }
                    }
                }
            }
            catch (Exception)
            {
                try
                {
                    if (File.Exists(outputFilePath))
                        File.Delete(outputFilePath);
                }
                catch (Exception)
                {
                }
                throw;
            }
        }

    }
}