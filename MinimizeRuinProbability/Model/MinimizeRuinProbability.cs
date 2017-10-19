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
/ Filename: minpruin.cpp (Defines the entry point for the console application.)
/
/ Function: main()
/
/ Summary:
/
/ Compiling the code will generate a single executable file that can be invoked with a double click or via a batch file with one parameter. When invoked
/ the directory location where input files reside and output files are written to is queried, stored as a string vbl and passed to other functions.
/ The control file (control.txt) is then read. If TD is specified as random the function derhrates() is invoked which reads the file of male/female
/ death probabilities (ageprobs.txt) and derives the hazard rates that apply to the group listed in the last line of the control file. The hazard rates
/ are stored locally and written to the file specified by the constant hrfile for reference. When TD is fixed, hazard rates are not needed or derived.
/ When invoked, the executable may have either zero or one argument(s). (See below.) The arrangement specified by the control file determines the
/ total number of years to process. Iteration always begins at the terminal year (i.e., total # of years minus 1) and ends at year 0. If TD is fixed
/ and 50 years are specified in the control file then processing starts at year=49 and ends at year=0. (This results in 50 alpha decisions made by the
/ retiree, the first at year=0 (i.e., t=0) and the last at year=49 (i.e., t=49). The total # of years to process for random TD depends on the specific
/ ages specified for the group. For example, if random TD for a 65 year old male and female couple is specified in the control file then a total of 48
/ years are processed since the maximum age allowed for a male is 111 and for a female is 113 (see the file ageprobs.txt). Processing 48 years of data
/ implies that 48 alpha decisions are made, here the first is at year=0 (both M/F at 65) and the last at year=47 (F is age 112). (See the paper for a
/ warning about dated hazard risk. Rerun the optimization if the hazard functions differ notably over time from those written to hrates.txt.) Using a
/ different ageprobs.txt file can change total # of years that need to be processed which, in general is: Max((Max male age - Min male age),(Max female
/ age - Min female age)). Here maximums are allowable and minimums are actual as specified in the control file. When processing each year (except the
/ terminal year) the function getprprobs() is invoked and reads the probabilities derived for the previous year. These are then passed to the function
/ optimize() which derives the probabilities and alphas for the current year and writes the results to a file for reference. Once complete, the function
/ combfiles() is invoked which combines all separate files into a single file to be read in as the prior probabilities during processing of the next year/
/ time point. When at year=0, combfiles() also concatenates all data from all prior years into both a vertical text file and 2 horizontal csv files, where
/ the csv files can be read into a spreadsheet. This is helpful when simulating the optimal solution to verify the result and only the csv file of optimal
/ alphas is needed. (Note: The terms "year" and "time point" are used interchangeably throughout this application and mean the same thing.)
/
/ Parameters:
/
/ The executable takes zero or one argument(s) when invoked. If an argument is specified it reflects the maximum number of jobs to process concurrently
/ by the application as it executes. If no argument is specified this value defaults to the number of independent processing units that exist on the
/ machine running the application. (This application was developed and tested on the Windows� operating system.)
/
/ Input Files:
/
/ 1.) Parameter control file of specific form whose name is defined by the string constant paramfile (defined in the header and located in the directory
/ specified by the user).
/ 2.) Age probability file of specific form whose name is set as the string constant ageprfile (defined in the header file and located in the directory
/ specified by the user. This file is read and processed by the function derhrates()).
/
/ Output Files:
/
/ 1.) The derived hazard rates for the random arrangement specified in the control file. The hazard rate file is written by function derhrates() using
/ filename specified by string constant hrfile defined in the header to the directory specified by the user.
/ 2.) FinalResults_V.txt (Vertical text file of all results from all years processed, written by combfiles() after year=0 finishes.)
/ 3.) FinalAlphaResults_H.csv (Horizontal csv file of optimal alphas, written by combfiles() after year=0 finishes. Use this file to simulate the result.)
/ 4.) FinalProbResults_H.csv (Horizontal csv file of optimal probabilities, written by combfiles() after year=0 finishes.)
/
/ Note: A number of temporary data files are written to the directory specified by the user during the processing of each year and across years. These
/ are removed by the application as it runs. They are created to avoid carrying large amounts of data inside arrays that are not needed. This was
/ a design decision made to speed up runtime. When processing a given year/time point, only the prior year's data is needed.
/
/ Return Value:
/
/ This function returns the integer value of 0 (success) or an error code (failure).
/---------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MinimizeRuinProbability.Model
{
    public enum Gender
    {
        M, F
    }

    public static class MinimizeRuinProbability
    {
        public static bool Calculate(int pllproc)
        {
            int[] precisions = new int[2];
            int numyears = 0;
            int p = 0;
            int[] buckets = new int[2];
            int[][] bktarys;
            double[] @params = new double[8];
            double[] hr = null;
            double[] prV;
            List<int> unqbkts;

            string rootdir = Config.ConfigsRootDir;
            // Read in parameter control file. Load first line into array params[] and 2nd line into array precisions[].
            // (3rd line is processed conditionally based on the arrangement specified by the user i.e., fixed vs random TD.)
            try
            {
                var getParams = File.ReadAllText(Path.Combine(rootdir, Config.ControlFile));
                var s = getParams.Split(new[] {' ', '\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);

                @params = s.Take(8).Select(i => double.Parse(i, CultureInfo.InvariantCulture)).ToArray();
                precisions = s.Skip(8).Take(2).Select(int.Parse).ToArray();
                int numrand = int.Parse(s[10]);
                if (numrand > 0)
                {
                    var genders = new Gender[numrand];
                    var ages = new int[numrand];
                    // Random TD for group. Load arrays with genders and ages.
                    var genreAges = s.Skip(11).ToArray();
                    var i = 0;
                    while (i < genreAges.Length)
                    {
                        if (!Enum.TryParse(s[11 + i], out genders[i / 2]))
                        {
                            Trace.Write($"ERROR: Invalid gender in file {Config.ControlFile} for person #{i/2}: {s[11 + i]}");
                            Trace.WriteLine("");
                            Trace.WriteLine("EXITING...main()...");
                            Console.Read();
                            Environment.Exit(1);
                        }
                        ages[i/2] = int.Parse(s[12+i]);
                        i += 2;
                    }

                    // Build the hazard rates if random TD is specified and no file exists in the directory. (Don't leave an old one sitting there.)
                    numyears = DeriveHazardRates.Run(rootdir, numrand, genders, ages);
                    hr = new double[numyears + 1];

                    // File of hazard rates has now been built and written for reference, load it to the hr[] array.
                    // Read in survival probabilities that have been written to the file hrates.txt for this arrangement.
                    // (Doing it this way is useful when needing to run a single year for debugging purposes.)
                    try
                    {
                        foreach (var line in File.ReadLines(Path.Combine(rootdir, Config.HrFile)))
                        {
                            s = line.Split(' ');
                            if (s.Length != 2)
                                continue;
                            hr[p] = double.Parse(s[0], CultureInfo.InvariantCulture);
                            p++;
                            if (p >= numyears + 1)
                                break;
                        }
                    }
                    catch (IOException ex)
                    {
                        Trace.WriteLine($"ERROR: Could not read file: {Config.HrFile}. {ex.Message}");
                        Trace.WriteLine("EXITING...main()...");
                        Console.Read();
                        Environment.Exit(1);
                    }
                }
                else
                {
                    // For fixed TD load hr[] with zeros (hrates.txt is not created).
                    numyears = int.Parse(s[11]); // No SMax to account for here, so don't add an element.
                    hr = new double[numyears];
                }
            }
            catch (IOException ex)
            {
                Trace.Write("ERROR: Could not read file: ");
                Trace.WriteLine(Path.Combine(rootdir, Config.ControlFile) + $". {ex.Message}");
                Trace.WriteLine("EXITING...main()...");
                Console.Read();
                Environment.Exit(1);
            }

            // Iterate over all years, launching separate threads to proccess optimal size collections of buckets concurrently.
            for (int yr = numyears - 1; yr >= 0; yr--)
            {
                Trace.Write("\n");
                Trace.Write("Processing for year ");
                Trace.Write(yr);
                Trace.Write(" has begun ...");
                Trace.Write("\n");
                // Process all buckets for terminal year in a single call to optimize().
                int rc;
                if (yr == numyears - 1)
                {
                    buckets[0] = 1;
                    buckets[1] = (int)(@params[5] * precisions[0] + 0.5);
                    Trace.Write("--> Processing buckets ");
                    Trace.Write(buckets[0]);
                    Trace.Write(" through ");
                    Trace.Write(buckets[1]);
                    Trace.WriteLine(" ...");
                    rc = Optimize.Run(rootdir, yr, numyears, @params, precisions, buckets, hr, null, new List<int>());
                }
                // Process buckets for all other years optimally based on both current/prior year characteristics.
                else
                {
                    // Retrieve the prior year's probabilities which have been written to a file and load into an array.
                    prV = new double[(int)(@params[5] * precisions[0] + 0.5)];
                    unqbkts = GetPriorProbs.Run(rootdir, yr, @params[5], precisions[0], prV, hr[yr + 1]);
                    // Function call to determine pruning/parallel processing parameters for the current year.
                    buckets[0] = 0;
                    buckets[1] = 0;
                    rc = Optimize.Run(rootdir, yr, numyears, @params, precisions, buckets, hr, prV, unqbkts);
                    // Derive the optimal # of buckets to process per run.
                    int bktsprun = rc / (pllproc - 1);
                    Trace.Write("--> # Buckets processed per thread (excluding last one): ");
                    Trace.WriteLine(bktsprun);

                    // Build thread array of optimal size then launch calls to optimize() concurrently.
                    bktarys = new int[pllproc][];
                    var t = new Task[pllproc];
                    for (int i = 0; i < pllproc; i++)
                    {
                        // Define buckets then process them in concurrent threads.
                        bktarys[i] = new int[2];
                        bktarys[i][0] = bktsprun * i + 1;
                        bktarys[i][1] = i < pllproc - 1 ? bktsprun * (i + 1) : (int) (@params[5] * precisions[0] + 0.5);

                        Trace.WriteLine(
                            $"--> Begin concurrent processing of buckets {bktarys[i][0]} through {bktarys[i][1]} ...");

                        var currItem = i;
                        t[i] = Task.Run(() =>
                        {
                            try
                            {
                                Optimize.Run(rootdir, yr, numyears, @params,
                                    precisions, bktarys[currItem], hr, prV,
                                    unqbkts);
                            }
                            catch (Exception ex)
                            {
                                Trace.WriteLine(
                                    $"Calculation of #{currItem + 1} еркуфв failed with error: {ex.Message}. Stacktrace: {ex.StackTrace}");
                            }
                        });
                        
                    }
                    // Wait for all threads to finish, then proceed.
                    Task.WaitAll(t);

                    // Free dynamic memory allocations.
                    for (int i = 0; i < pllproc; ++i)
                    {
                        bktarys[i] = null;
                        bktarys[i] = null;
                    }
                    // Clear contents of unqbkts vector and reset capacity. (This container is reused within the loop.)
                    unqbkts.Clear();
                    // Combine all files for each year processed, and if at year=0 concatenate data files from all years.
                    CombineFiles.Run(rootdir, yr, pllproc, bktsprun, @params[5], precisions[0], numyears);
                }
                Trace.WriteLine($"Processing for year {yr} has finished.");
            }
            return true;
        }
    }
}
