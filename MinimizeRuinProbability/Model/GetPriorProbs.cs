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
/ Filename: getprprobs.cpp
/
/ Function: getprprobs()
/
/ Summary:
/
/ This function reads the prior time point's probability file and populates an array with these values. It also returns a vector containing only the
/ bucket #'s from the prior time point that have unique probabilities assigned to them. When deriving the expected probability of ruin for any time
/ point after the next time point it is not necessary to process every bucket if sequential buckets hold the same probability. Therefore the optimize()
/ function only processes those buckets with unique probabilities. Note that the probability of falling into a new larger bucket that was formed from
/ multiple sequential buckets is higher.
/
/ Parameters:
/
/ 1.) Root directory where input files reside and output files are written.
/ 2.) Current year/time point being processed. (These start at 0 and end at one less than the total # being processed by the given arrangement specified
/ in the control file.)
/ 3.) Maximum ruin factor value specified in the control file (i.e., RFMax).
/ 4.) Precision value for the ruin factor discretization as specified in the control file (i.e., PR).
/ 5.) An empty array of appropriate size (i.e., total # of buckets) long doubles that will be populated by this function.
/ 6.) Value of the prior time point's hazard rate (0.00 if fixed TD). This is to determine when the highest possible probability has been reached and
/ all buckets from this time point onward can be treated as one collection.
/
/ Input Files:
/
/ 1.) File of probabilities from the prior time point that has already been derived. This file has the form "Year_X_All_Buckets.txt", where X refers to
/ actual time point. Time points start at 0 and end at 1 less than the total # being processed by the given arrangement. Each time point reflects
/ a decision that needs to be made. Time t=0 reflects the first asset allocation decision at the start of retirement and time T=SMax-1 reflects the
/ last asset allocation decision made the year before SMax when TD is random. When TD is fixed, TD-1 is the last time point requiring an asset
/ allocation decision. Note that the prior year/time point probabilities being read are from the current value + 1.
/
/ Output Files:
/
/ None.
/
/ Return Value:
/
/ This function returns a vector of bucket #'s with unique probabilities (from the prior time point which has already been processed). (It also populates
/ the array Vp whose location is passed to this function.)
/------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace MinimizeRuinProbability.Model
{
    public static class GetPriorProbs
    {
        public static List<int> Run(string root, int curyear, double rfmax, int pprec, double[] vp, double prhrate)
        {
            bool prMax = false;
            double prevprob = 0;
            var nbuckets = (int)(rfmax * pprec + 0.5);
            var PrB = new List<int>();
            string ifname = Path.Combine(root, $"Year_{curyear + 1}_All_Buckets.txt");
            // Array to indicate (0/1) where unique probabilities from prior time point reside.
            var bktPrn = new int[nbuckets];
            // Open and read the prior time point's probability file and load the values into an array.
            try 
            {
                foreach (string line in File.ReadLines(ifname))
                {
                    var s = line.Split();
                    if (s.Length != 4)
                        continue;
                    int inyr = int.Parse(s[0]);
                    double inrf = double.Parse(s[1], CultureInfo.InvariantCulture);
                    double inprob = double.Parse(s[2], CultureInfo.InvariantCulture);
                    //var inalpha = double.Parse(s[3], CultureInfo.InvariantCulture);

                    if (inyr == curyear + 1)
                    {
                        var inb = (int)(inrf * pprec + 0.5);
                        vp[inb - 1] = inprob;
                    }
                }
            }
            catch (IOException ex)
            {
                Trace.Write("ERROR: Could not read prior year probability file: ");
                Trace.WriteLine(ifname + ". " + ex.Message);
                Trace.WriteLine("EXITING...getprprobs()...");
                Console.Read();
                Environment.Exit(1);
            }
            // Check the data just read in for any issues and load pruning array.
            for (var b = 1; b <= nbuckets; ++b)
            {
                // Populate the pruning array with 1's and 0's, where 1 indicates unique probability (last bucket in the sequence).
                if (b > 1 && b < nbuckets && !vp[b - 1].Equals(vp[b]) && !prMax) // Always process first and last buckets individually.
                {
                    if (vp[b] >= 1.00 - prhrate) // The next bucket holds the maximum possible probability, stop scanning.
                        prMax = true;
                    bktPrn[b - 1] = 1; // This bucket will account for a unique probability.
                }
                else if (b > 1 && b < nbuckets)
                {
                    bktPrn[b - 1] = 0; // This bucket will not account for a unique probability.
                }
                else
                {
                    bktPrn[b - 1] = 1; // First and last buckets will always be processed.
                }
                if (b == 1)
                    prevprob = 0;
                // Check for and report issues found in the prior time point's probability file.
                if (vp[b - 1] < 0 || vp[b - 1] > 1.00 - prhrate + 2.0 * Math.Pow(0.1, 16) ||
                    vp[b - 1] < prevprob - Math.Pow(0.1, 15))
                {
                    //cout.setf(ios_base.@fixed, ios_base.floatfield);
                    Trace.WriteLine("There is an issue with the previous years data (see below):");
                    Trace.Write("Vp[");
                    Trace.Write(b - 2);
                    Trace.Write("] = ");
                    Trace.WriteLine(vp[b - 2]);
                    Trace.Write("Vp[");
                    Trace.Write(b - 1);
                    Trace.Write("] = ");
                    Trace.WriteLine(vp[b - 1]);
                    Trace.WriteLine("EXITING...getprprobs()...");
                    Console.Read();
                    Environment.Exit(1);
                }
                prevprob = vp[b - 1];
            }
            // Build an array with values that are the bucket #'s from the prior time point that have a
            // unique probability and thus will be processed during the expected value computation.
            for (var b = 1; b <= nbuckets; ++b)
                if (bktPrn[b - 1] == 1)
                    PrB.Add(b);

            // For informational purposes.
            Trace.Write("--> # Unique bucket probs at prior time point reported by getprprobs(): ");
            Trace.Write(PrB.Count);
            Trace.Write("\n");
            // Return the vector PrB.
            return PrB;
        }

    }
}

