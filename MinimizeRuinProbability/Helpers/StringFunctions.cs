//----------------------------------------------------------------------------------------
//	Copyright © 2006 - 2017 Tangible Software Solutions Inc.
//	This class can be used by anyone provided that the copyright notice remains intact.
//
//	This class provides the ability to replicate various classic C string functions
//	which don't have exact equivalents in the .NET Framework.
//----------------------------------------------------------------------------------------

using System;

namespace MinimizeRuinProbability.Helpers
{
    internal static class StringFunctions
    {
        //------------------------------------------------------------------------------------
        //	This method allows replacing a single character in a string, to help convert
        //	C++ code where a single character in a character array is replaced.
        //------------------------------------------------------------------------------------
        internal static string ChangeCharacter(string sourceString, int charIndex, char changeChar)
        {
            return (charIndex > 0 ? sourceString.Substring(0, charIndex) : "")
                   + changeChar.ToString() + (charIndex < sourceString.Length - 1 ? sourceString.Substring(charIndex + 1) : "");
        }

        //------------------------------------------------------------------------------------
        //	This method replicates the classic C string function 'isxdigit' (and 'iswxdigit').
        //------------------------------------------------------------------------------------
        internal static bool IsXDigit(char character)
        {
            if (char.IsDigit(character))
                return true;
            else if ("ABCDEFabcdef".IndexOf(character) > -1)
                return true;
            else
                return false;
        }

        //------------------------------------------------------------------------------------
        //	This method replicates the classic C string function 'strchr' (and 'wcschr').
        //------------------------------------------------------------------------------------
        internal static string StrChr(string stringToSearch, char charToFind)
        {
            int index = stringToSearch.IndexOf(charToFind);
            if (index > -1)
                return stringToSearch.Substring(index);
            else
                return null;
        }

        //------------------------------------------------------------------------------------
        //	This method replicates the classic C string function 'strrchr' (and 'wcsrchr').
        //------------------------------------------------------------------------------------
        internal static string StrRChr(string stringToSearch, char charToFind)
        {
            int index = stringToSearch.LastIndexOf(charToFind);
            if (index > -1)
                return stringToSearch.Substring(index);
            else
                return null;
        }

        //------------------------------------------------------------------------------------
        //	This method replicates the classic C string function 'strstr' (and 'wcsstr').
        //------------------------------------------------------------------------------------
        internal static string StrStr(string stringToSearch, string stringToFind)
        {
            int index = stringToSearch.IndexOf(stringToFind, StringComparison.Ordinal);
            if (index > -1)
                return stringToSearch.Substring(index);
            else
                return null;
        }

        //------------------------------------------------------------------------------------
        //	This method replicates the classic C string function 'strtok' (and 'wcstok').
        //	Note that the .NET string 'Split' method cannot be used to replicate 'strtok' since
        //	it doesn't allow changing the delimiters between each token retrieval.
        //------------------------------------------------------------------------------------
        private static string _activeString;
        private static int _activePosition;
        internal static string StrTok(string stringToTokenize, string delimiters)
        {
            if (stringToTokenize != null)
            {
                _activeString = stringToTokenize;
                _activePosition = -1;
            }

            //the stringToTokenize was never set:
            if (_activeString == null)
                return null;

            //all tokens have already been extracted:
            if (_activePosition == _activeString.Length)
                return null;

            //bypass delimiters:
            _activePosition++;
            while (_activePosition < _activeString.Length && delimiters.IndexOf(_activeString[_activePosition]) > -1)
            {
                _activePosition++;
            }

            //only delimiters were left, so return null:
            if (_activePosition == _activeString.Length)
                return null;

            //get starting position of string to return:
            int startingPosition = _activePosition;

            //read until next delimiter:
            do
            {
                _activePosition++;
            } while (_activePosition < _activeString.Length && delimiters.IndexOf(_activeString[_activePosition]) == -1);

            return _activeString.Substring(startingPosition, _activePosition - startingPosition);
        }
    }
}