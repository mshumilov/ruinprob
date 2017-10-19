using System;
using System.Collections.Generic;
using System.Text;

namespace MinimizeRuinProbability.Model
{
    public static class Config
    {
        public const string ConfigsRootDir = "./config/";

        public const string ControlFile = "control1.txt"; // Name of parameter control input file.
        public const string AgeProbFile = "ageprobs.txt"; // Name of age probability input file.
        public const string HrFile = "hrates.txt"; // Name of hazard rate probability output file.
    }
}
