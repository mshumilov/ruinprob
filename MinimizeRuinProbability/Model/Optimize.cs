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
/ Filename: optimize.cpp
/
/ Function: optimize()
/
/ Summary:
/
/ This function derives the optimal alpha and probability values for a single year/time point and bucket range specified by the call. When the first
/ and last buckets are set to 0 this function makes a single pass over the given year/time point and approximates the bucket number where heavy algorithm
/ pruning begins. Heavy pruning sets alpha=pa which is a decision rule that takes effect when the optimal probability of ruin exceeds the first k
/ decimal places of the maximum possible probability of ruin for this year/arrangement, where k is the last parameter specified on the first line of the
/ control file. Thus a value of 4.00 indicates that pruning begins when the probability of ruin exceeds the first 4 decimal places of the highest possible
/ probability of ruin for this year/arrangement. Note that this value is stored locally in the variable prnpwr and only has an impact when TD is random.
/ The initial pass approximates this value by only considering alpha=pa. Optimal bucket sizes are then formed based off of this approximation and then
/ the actual calls to this function are made where the point of heavy pruning is determined exactly. The approximation is only used to estimate optimal
/ bucket collection sizes for threaded processing. For all other cases the specific buckets listed are processed and written to a file with the bucket
/ boundaries contained in the file name. The function combfiles() is invoked by main() after all buckets for a year/time point have been processed and
/ written to a file and it aggregates all individual files then deletes them leaving a single file containing all relevant optimal values for each year/
/ time point. Once the final year/time point has been processed (i.e., t=0) the individual files for each year/time point are aggregated into final
/ (horizontal & vertical) files for the specific arrangement being modeled and the year-specific files are deleted.
/
/ Parameters:
/
/ 1.) Root directory where input files reside and output files are written.
/ 2.) Year value to process. (This is the time point value which always starts at zero and ends at # time points to process for this arrangement less 1.)
/ 3.) Total # of years/time points processed by the arrangement specified in the control file.
/ 4.) Array containing all values from the first line of the control file.
/ 5.) Array containing the precision values from the 2nd line of the control file.
/ 6.) Array containing the bucket limits for this call.
/ 7.) Array containing the hazard rates derived for this arrangement.
/ 8.) Array of optimal probabilities derived for the prior year/time point (i.e., current year/time point + 1)
/ 9.) Vector of unique bucket endpoints for prior year where collections of buckets with equal probabilities are treated as a single bucket in the
/ expected value calculation.
/
/ Input Files:
/
/ None. (The file of prior probabilities is read by the function getprprobs() and passed to this function via the parameter Vp.)
/
/ Output Files:
/
/ 1.) When processing the terminal time point the file "Year_X_All_Buckets.txt" is written to directory root. (Where X = nyrs-1 is the time point.)
/ 2.) When processing all other time points the file "Year_Y_Buckets_S_thru_E.txt" is written to directory root. (Where Y is the time point, S is the
/ start bucket #, and E is the end bucket #. Once processing for this year has ended these files are aggregated by combfiles() into a single file
/ named "Year_Y_All_Buckets.txt".)
/
/ Return Value:
/
/ The approximate bucket # where heavy algorithm pruning begins when invoking this function with start and end buckets equal to 0. Otherwise return 0.
/-------------------------------------------------------------------------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Accord.Statistics.Distributions.Univariate;


public static class Optimize
{
    public static int Run(string root, int y, int nyrs, double[] prms, int[] prc, int[] bkts, double[] sprobs, double[] Vp, List<int> PrB)
    {
        int pr = prc[0];
        int pa = prc[1];
        double smn = prms[0];
        double svr = prms[1];
        double bmn = prms[2];
        double bvr = prms[3];
        double cv = prms[4];
        double rfmax = prms[5];
        double er = prms[6];
        double prnpwr = prms[7];
        int nbuckets = (int)(rfmax * pr + 0.5);
        int nuqbkts = PrB.Count;
        int prunepnt = 0;
        int ties;
        int stalpha = 0;
        int stbkt = bkts[0];
        int endbkt = bkts[1];
        double rf;
        double alpha;
        double OPT_alpha = 0;
        double mean;
        double std;
        double[] Ac;
        double OPT_pruin = 1.0;
        double cdfval;
        double eprob;
        double[] Vc;
        double rhs_cdf;
        double lhs_cdf;
        double pruin = 0;
        double tiethresh;
        double pruneprob;
        NormalDistribution normdist;

        // Create the arrays to hold current year optimal alphas and probabilities.
        Ac = new double[nbuckets];
        Vc = new double[nbuckets];
        // Before processing the current year, run through all buckets using only alpha=100% to find the pruning point.
        if (stbkt == 0 && endbkt == 0)
        {
            stalpha = pa;
            stbkt = 1;
            endbkt = nbuckets;
        }

        var outputFile = "";
        // Process year specified.
        if (y == (nyrs - 1)) // Last decision point (either TD-1 or SMax-1).
        {
            if (bkts[0] == 0 && bkts[1] == 0) // This should not happen, return with error.
            {
                Trace.Write("ERROR: Attempt to find pruning point at terminal year.");
                Trace.Write("\n");
                Trace.Write("EXITING...optimize()...");
                Trace.Write("\n");
                Console.Read();
                Environment.Exit(1);
            }
            for (int b = stbkt; b <= endbkt; ++b) // Get min PRuin at the last time point.
            {
                rf = (double)b / pr;
                for (int a = 0; a <= pa; ++a)
                {
                    alpha = (double)a / pa;
                    var dalpha = (double)alpha;
                    mean = (1.00 - (double)er) * (1.00 + dalpha * (double)smn + (1.00 - dalpha) * (double)bmn);
                    std = (1.00 - (double)er) * Math.Sqrt(Math.Pow(dalpha, 2) * (double)svr + Math.Pow(1.00 - dalpha, 2) * (double)bvr + 2.00 * dalpha * (1.00 - dalpha) * (double)cv);
                    normdist = new NormalDistribution(mean, std);
                    cdfval = (double)normdist.DistributionFunction(rf);
                    if ((a == 0) || (cdfval < 0.5 && cdfval < OPT_pruin) || (cdfval >= 0.5 && cdfval <= OPT_pruin))
                    {
                        OPT_pruin = cdfval;
                        OPT_alpha = alpha;
                    }
                }
                Ac[b - 1] = OPT_alpha;
                Vc[b - 1] = (1.00 - sprobs[y]) * OPT_pruin;

            }
            // Write results to file. 
            // These will be loaded to an array when processing the next time point (i.e., the one before this one).
            outputFile = Path.Combine(root, $"Year_{y}_All_Buckets.txt");
        }
        else // Process all other years.
        {
            // One-half of the maximum possible PRuin is the tie threshold, in general.
            tiethresh = (0.5) * ((1.00 - sprobs[y]));
            // Prune point #2 begins at set number of decimals after max probability for this arrangement.
            pruneprob = (double)(Math.Floor(Math.Pow(10.0, (double)prnpwr) * (1.00 - (double)sprobs[y])) / Math.Pow(10.0, (double)prnpwr));
            if (bkts[0] == 0 && bkts[1] == 0)
            {
                //C++ TO C# CONVERTER TODO TASK: Calls to 'setf' using two arguments are not converted by C++ to C# Converter:
                //cout.setf(ios_base.@fixed, ios_base.floatfield);
                Trace.Write("--> Value of pruneprob reported by optimize() is: ");
                Trace.Write(pruneprob);
                Trace.Write("\n");
            }
            for (int b = stbkt; b <= endbkt; ++b) // PRuin at any future time point.
            {
                ties = 0; // Initialize for le/gt comparisons.
                rf = (double)b / pr; // Derive ruin factor.
                OPT_alpha = 99.00; // Initialize to unrealistic value.
                OPT_pruin = 99.00; // Initialize to unrealistic value.
                for (int a = stalpha; a <= pa; ++a)
                {
                    if ((prunepnt == 0 && (a == stalpha || OPT_pruin > 0.00)) || (prunepnt == 1 && a == pa))
                    {
                        alpha = (double)a / pa;
                        var dalpha = (double)alpha;
                        mean = (1.00 - (double)er) * (1.00 + dalpha * (double)smn + (1.00 - dalpha) * (double)bmn);
                        std = (1.00 - (double)er) * Math.Sqrt(Math.Pow(dalpha, 2) * (double)svr + Math.Pow(1.00 - dalpha, 2) * (double)bvr + 2.00 * dalpha * (1.00 - dalpha) * (double)cv);
                        normdist = new NormalDistribution(mean, std);
                        cdfval = (double)normdist.DistributionFunction(rf);
                        if (cdfval == 1.00)
                        {
                            eprob = 1.00 - sprobs[y + 1];
                        }
                        else
                        {
                            //-------------------------------------------------------------------------------------------------------------------//
                            rhs_cdf = 1.00;
                            lhs_cdf = (double)normdist.DistributionFunction(rf * (1 + pr/1.5));
                            eprob = (rhs_cdf - lhs_cdf) * Vp[0]; // First bucket, unique processing.
                            rhs_cdf = lhs_cdf;
                            for (int pb = 2; pb <= nuqbkts; ++pb) // All others but last bucket, standard processing
                            { // for unique probs only.
                                lhs_cdf = (double)normdist.DistributionFunction(rf * (1.0 + pr / (PrB[pb - 1] + 0.5)));
                                eprob = eprob + (rhs_cdf - lhs_cdf) * Vp[PrB[pb - 1] - 1];
                                rhs_cdf = lhs_cdf;
                            }
                            eprob = eprob + (rhs_cdf - cdfval) * (1.00 - sprobs[y + 1]); // Last bucket, unique processing.
                            eprob = eprob / (1.00 - cdfval); // Make the probability conditional.
                                                             //-------------------------------------------------------------------------------------------------------------------//
                        }

                        // Deal with numerical instability near zero.
                        if (ties == 0)
                        {
                            pruin = (1.00 - sprobs[y]) * (cdfval + eprob - (cdfval * eprob));
                            if (pruin > tiethresh)
                            {
                                ties = 1;
                            }
                        }
                        // Deal with numerical instability near one.
                        if (ties == 1)
                        {
                            pruin = 1.00 - (sprobs[y] + (1.00 - cdfval) * (1.00 - eprob) - sprobs[y] * (1.00 - cdfval) * (1.00 - eprob));
                        }
                        // Update optimal values.
                        if (a == 0 || ties == 0 && pruin < OPT_pruin || ties == 1 && pruin <= OPT_pruin)
                        {
                            OPT_pruin = pruin;
                            OPT_alpha = alpha;
                        }
                    }
                }
                // Load optimal values into correspnding arrays.
                Ac[b - 1] = OPT_alpha;
                Vc[b - 1] = OPT_pruin;
                // Set pruning for this timepoints processing, allow for floating point precision diffs.
                if (prunepnt == 0 && Vc[b - 1] >= pruneprob - (double)(Math.Pow(0.1, 16) + Math.Pow(0.1, 17)))
                {
                    prunepnt = 1;
                    // Initial run finds the (approximate) bucket where pruning starts then uses this information to split the remaining buckets by
                    // sizes that will run in shortest time. For this run, end the call as soon as the bucket number has been found.
                    if (bkts[0] == 0 && bkts[1] == 0)
                    {
                        Trace.Write("--> Pruning RC returned by optimize() is: ");
                        Trace.Write(b);
                        Trace.Write("\n");
                        return b;
                    }
                }
            }
            // Write current year to file with bucket boundaries contained in the name.
            outputFile = Path.Combine(root, $"Year_{y}_Buckets_{stbkt}_thru_{endbkt}.txt");
            
        }

        var sb = new StringBuilder();
        for (int b = stbkt; b <= endbkt; ++b)
        {
            //fout.setf(ios_base.@fixed, ios_base.floatfield);
            var vc = $"{Vc[b - 1]:F50}";
            sb.AppendLine($"{y} {(double)b / pr:F10} {vc} {Ac[b - 1]:F10}");
        }
        File.WriteAllText(outputFile, sb.ToString());

        return 0;
    }

}
