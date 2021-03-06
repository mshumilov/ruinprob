﻿//----------------------------------------------------------------------------------------
//	Copyright © 2006 - 2017 Tangible Software Solutions Inc.
//	This class can be used by anyone provided that the copyright notice remains intact.
//
//	This class provides the ability to convert basic C++ 'cin' and C 'scanf' behavior.
//----------------------------------------------------------------------------------------
namespace MinimizeRuinProbability.Helpers
{
    internal static class ConsoleInput
    {
        private static bool _goodLastRead;
        internal static bool LastReadWasGood => _goodLastRead;

        internal static string ReadToWhiteSpace(bool skipLeadingWhiteSpace)
        {
            string input = "";

            char nextChar;
            while (char.IsWhiteSpace(nextChar = (char)System.Console.Read()))
            {
                //accumulate leading white space if skipLeadingWhiteSpace is false:
                if (!skipLeadingWhiteSpace)
                    input += nextChar;
            }
            //the first non white space character:
            input += nextChar;

            //accumulate characters until white space is reached:
            while (!char.IsWhiteSpace(nextChar = (char)System.Console.Read()))
            {
                input += nextChar;
            }

            _goodLastRead = input.Length > 0;
            return input;
        }

        internal static string ScanfRead(string unwantedSequence = null, int maxFieldLength = -1)
        {
            string input = "";

            char nextChar;
            if (unwantedSequence != null)
            {
                nextChar = '\0';
                foreach (char t in unwantedSequence)
                {
                    if (char.IsWhiteSpace(t))
                    {
                        //ignore all subsequent white space:
                        while (char.IsWhiteSpace(nextChar = (char)System.Console.Read()))
                        {
                        }
                    }
                    else
                    {
                        //ensure each character matches the expected character in the sequence:
                        nextChar = (char)System.Console.Read();
                        if (nextChar != t)
                            return null;
                    }
                }

                input = nextChar.ToString();
                if (maxFieldLength == 1)
                    return input;
            }

            while (!char.IsWhiteSpace(nextChar = (char)System.Console.Read()))
            {
                input += nextChar;
                if (maxFieldLength == input.Length)
                    return input;
            }

            return input;
        }
    }
}