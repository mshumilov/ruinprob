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
/ Filename: derhrates.cpp
/
/ Function: derhrates()
/
/ Summary:
/
/ Derive the discrete hazard rates for the random TD arrangement specified by the last line of the control file. This function is not called for
/ fixed TD. The derived hazard rates are written to a file whose name is specified by the string constant hrfile (defined in the header file), and
/ the total # of years that need to be processed are determined by this function and returned by the call. All random TD models define a single
/ set of discrete hazard rates.
/
/ Parameters:
/
/ 1.) Root directory where input files reside and output files are written.
/ 2.) # of persons specified by this arrangement in the control file.
/ 3.) Array of genders for the random persons involved in this arrangement.
/ 4.) Array of ages for the random persons involved in this arrangement.
/ 5.) True/false indicator of whether or not the hazard rates file already exists. If it exists, this function only returns the total number of
/ years that need to be processed by this arrangement and the hazard rates are not re-derived. (Do not leave an old hazard rates file in the
/ root directory. If one is there it will be used but an alert message is given to the user. Kill the job with task manager if necessary.)
/
/ Input Files:
/
/ 1.) Age probability file of specific form whose name/location is set as the string constant ageprfile which is defined in the header file and
/ resides in the directory specified by the user.
/
/ Output Files:
/
/ 1.) The derived hazard rates for the random arrangement specified in the control file using filename specified by hrfile which is defined in the
/ header file and resides in the directory specified by the user.
/
/ Return Value:
/
/ This function returns the # of years that need to be processed for this arrangement, representing the maximum # of alpha decisions to make.
/----------------------------------------------------------------------------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace MinimizeRuinProbability.Model
{
    public static class DeriveHazardRates
    {
        public static int Run(string root, int npersons, Gender[] gndrs, int[] ags)
        {
            int prvage = 0;
            int remyrs;
            int strtage = 0;
            int minmage = 999;
            int maxmage = 0;
            int maxaMAge = 0;
            int minfage = 999;
            int maxfage = 0;
            int maxaFAge = 0;
            double[][] perspmf;
            double[][] perscdf;
            double sumprobs;
            double mchksum = 0;
            double fchksum = 0;
            //C++ TO C# CONVERTER TODO TASK: C# does not have an equivalent to pointers to value types:
            //ORIGINAL LINE: double * tdcdf;
            double[] tdcdf;
            //C++ TO C# CONVERTER TODO TASK: C# does not have an equivalent to pointers to value types:
            //ORIGINAL LINE: double * hrates;
            double[] hrates;
            List<double> mprobs = new List<double>();
            List<double> fprobs = new List<double>();
            // Read age probabilities for M/F into corresponding arrays. These are TD probabilities starting at a given age and will need to be adjusted for each person
            // based on their age at retirement start. Also, use the file to derive the maximum possible M/F ages, which are ages that have non-zero probabilities.


            try
            {
                foreach (var line in File.ReadLines(Path.Combine(root, Config.AgeProbFile)))
                {
                    var s = line.Split();
                    if (s.Length != 3)
                        continue;
                    var age = int.Parse(s[0]);
                    var prob1 = double.Parse(s[1], CultureInfo.InvariantCulture);
                    var prob2 = double.Parse(s[2], CultureInfo.InvariantCulture);
                    if (strtage == 0)
                    {
                        strtage = age;
                        prvage = age - 1;
                    }
                    if (age == prvage + 1)
                    {
                        if (prob1 >= 0)
                            mprobs.Add(prob1);
                        if (prob2 >= 0)
                            fprobs.Add(prob2);
                        if (prob1 > 0)
                            maxaMAge = age;
                        if (prob2 > 0)
                            maxaFAge = age;
                        prvage = age;
                    }
                }
            }
            catch (IOException ex)
            {
                Trace.Write("ERROR: Could not read file: ");
                Trace.WriteLine(root + Config.AgeProbFile + ". " + ex.Message);
                Trace.WriteLine("EXITING...derhrates()...");
                Console.Read();
                Environment.Exit(1);
            }


            // Confirm that the probabilities read sum to 1.00 for each gender (allow for floating pt precision diffs).
            for (int i = mprobs.Count - 1; i >= 0; --i)
            {
                mchksum = mchksum + mprobs[i];
            }
            for (int i = fprobs.Count - 1; i >= 0; --i)
            {
                fchksum = fchksum + fprobs[i];
            }
            if (Math.Min(mchksum, fchksum) < 1.00 - Math.Pow(0.1, 15) ||
                Math.Max(mchksum, fchksum) > 1.00 + Math.Pow(0.1, 15))
            {
                Trace.Write("ERROR: Probabilities in ");
                Trace.Write(root + Config.AgeProbFile);
                Trace.WriteLine(" do not sum to 1 for one or both genders. See below...");
                Trace.Write("--> Male probability sum = ");
                Trace.WriteLine(mchksum);
                Trace.Write("--> Female probability sum = ");
                Trace.WriteLine(fchksum);
                Trace.WriteLine("EXITING...derhrates()...");
                Console.Read();
                Environment.Exit(1);
            }
            // Find the minimum male/female ages specified in the control file at retirement start.
            for (int i = 0; i < npersons; ++i)
            {
                if (gndrs[i] == Gender.M && ags[i] < minmage)
                {
                    minmage = ags[i];
                }
                else if (gndrs[i] == Gender.F && ags[i] < minfage)
                {
                    minfage = ags[i];
                }
            }
            // Find the maximum male/female ages specified in the control file at retirement start.
            for (int i = 0; i < npersons; ++i)
            {
                if (gndrs[i] == Gender.M && ags[i] > maxmage)
                {
                    maxmage = ags[i];
                }
                else if (gndrs[i] == Gender.F && ags[i] > maxfage)
                {
                    maxfage = ags[i];
                }
            }
            // Check that no age specified on the last line of the control file is less than the start age in the probabilities file.
            if (minmage < strtage || minfage < strtage)
            {
                Trace.WriteLine(
                    $"ERROR: An age in {root + Config.ControlFile}  is less than the minimum age in {root + Config.AgeProbFile}.");
                Trace.WriteLine($"--> Smallest male age ({Config.ControlFile}) = {minmage}");
                Trace.WriteLine($"--> Smallest female age ({Config.ControlFile}) = {minfage}");
                Trace.WriteLine($"--> Smallest allowable age ({Config.AgeProbFile}) = {strtage}");
                Trace.WriteLine("EXITING...derhrates()...");
                Console.Read();
                Environment.Exit(1);
            }
            // Check that no male age specified on the last line of the control file is greater than the last (>0 prob) male age in the age probs file.
            if (maxmage > maxaMAge)
            {
                Trace.Write("ERROR: A male age in ");
                Trace.Write(root + Config.ControlFile);
                Trace.Write(" is greater than the maximum allowed in ");
                Trace.Write(root + Config.AgeProbFile);
                Trace.WriteLine(".");
                Trace.Write("--> Largest male age (");
                Trace.Write(Config.ControlFile);
                Trace.Write(") = ");
                Trace.WriteLine(maxmage);
                Trace.Write("--> Largest allowable male age (");
                Trace.Write(Config.AgeProbFile);
                Trace.Write(") = ");
                Trace.WriteLine(maxaMAge);
                Trace.WriteLine("EXITING...derhrates()...");
                Console.Read();
                Environment.Exit(1);
            }
            // Check that no female age specified on the last line of the control file is greater than the last (>0 prob) female age in the age probs file.
            if (maxfage > maxaFAge)
            {
                Trace.Write("ERROR: A female age in ");
                Trace.Write(root + Config.ControlFile);
                Trace.Write(" is greater than the maximum allowed in ");
                Trace.Write(root + Config.AgeProbFile);
                Trace.WriteLine(".");
                Trace.Write("--> Largest female age (");
                Trace.Write(Config.ControlFile);
                Trace.Write(") = ");
                Trace.WriteLine(maxfage);
                Trace.Write("--> Largest allowable female age (");
                Trace.Write(Config.AgeProbFile);
                Trace.Write(") = ");
                Trace.WriteLine(maxaFAge);
                Trace.WriteLine("EXITING...derhrates()...");
                Console.Read();
                Environment.Exit(1);
            }
            // Derive total # years to process for this arrangement (plus 1). (Includes SMax, but no alpha decision is made at SMax.)
            remyrs = Math.Max(maxaMAge - minmage + 1, maxaFAge - minfage + 1);

            // If the hazard rates file exists remove it.
            if (File.Exists(Path.Combine(root, Config.HrFile)))
                File.Delete(Path.Combine(root, Config.HrFile));

            // Construct the person-specific probability arrays (pmfs and cdfs), shifting each to begin at t=0 which represents the start of retirement.
            // Build array of pointers to individual TD probability arrays (start at time t=0 for each person). Then build inner arrays that will
            // apply to each individual person retiring, containing remyrs elements. (Do this for both the individual person pmfs and cdfs.)
            perspmf = new double[npersons][];
            perscdf = new double[npersons][];
            tdcdf = new double[remyrs];
            hrates = new double[remyrs];
            for (int i = 0; i < npersons; ++i)
            {
                // Check that age values in parameter file are valid. (Invalid gender values are checked in main().)
                if (ags[i] < strtage || gndrs[i] == Gender.M && ags[i] > maxaMAge ||
                    gndrs[i] == Gender.F && ags[i] > maxaFAge)
                {
                    Trace.Write("ERROR: Invalid age in file control.txt for person #");
                    Trace.Write(i);
                    Trace.Write(" of gender=");
                    Trace.Write(gndrs[i]);
                    Trace.Write(": ");
                    Trace.WriteLine(ags[i]);
                    Trace.WriteLine("EXITING...derhrates()...");
                    Console.Read();
                    Environment.Exit(1);
                }
                // Create the individual person PMF and CDF arrays.
                perspmf[i] = new double[remyrs];
                perscdf[i] = new double[remyrs];
                // Populate the individual arrays for each person. These are probabilities of death that
                // will sum to one and a value exists for each of the remyrs processed by this arrangement.
                sumprobs = 0.0;
                for (int j = mprobs.Count - 1; j >= (ags[i] - strtage); j--)
                    sumprobs += gndrs[i] == Gender.M ? mprobs[j] : fprobs[j];

                // Load the individual person PMF array with the applicable conditional PMF value. (Initialize with zeros.)
                for (int j = 0; j < remyrs; ++j)
                    perspmf[i][j] = 0.0;

                var maxAge = gndrs[i] == Gender.M ? maxaMAge : maxaFAge;
                for (int j = 0; j < maxAge - (ags[i] - 1); ++j)
                {
                    perspmf[i][j] = gndrs[i] == Gender.M
                        ? mprobs[ags[i] - strtage + j] / sumprobs
                        : fprobs[ags[i] - strtage + j] / sumprobs;
                }
                // Check that sum of probabilities equals 1 (within floating point precision). If not put out alert.
                sumprobs = perspmf[i].Sum();

                if (sumprobs < 1.00 - Math.Pow(0.1, 15) || sumprobs > 1.00 + Math.Pow(0.1, 15))
                {
                    Trace.WriteLine($"Alert: Sum of probabilities for Person {i + 1} is not 1.00.");
                    Trace.WriteLine($"Sum is = {sumprobs}");
                }
                // Load the individual person cdf array with the applicable cumulative probability.
                // And load the TD cdf array with the applicable cumulative probability.
                for (int j = 0; j < remyrs; ++j)
                {
                    perscdf[i][j] = 0;
                    for (int k = 0; k <= j; ++k)
                        perscdf[i][j] += perspmf[i][k];

                    if (i == 0)
                        tdcdf[j] = perscdf[i][j];
                    else
                        tdcdf[j] *= perscdf[i][j];
                }
            }
            // Build the hazard rates and write to file.
            hrates[0] = tdcdf[0];
            for (int j = 1; j < remyrs; ++j)
                hrates[j] = Math.Min((tdcdf[j] - tdcdf[j - 1]) / (1.0 - tdcdf[j - 1]), 1.00);

            // Write hazard rate probabilities to file for reference, this file will be read back in if processing years in separate calls.
            var sb = new StringBuilder();
            for (int j = 0; j < remyrs; ++j)
                sb.AppendLine($"{hrates[j]:F50} (t={j:###0})");

            File.WriteAllText(Path.Combine(root, Config.HrFile), sb.ToString());
            // Return # years to process for this arrangement, which is 1 less than the # written to the hr file (since SMax is written).
            return remyrs - 1;
        }

    }
}

