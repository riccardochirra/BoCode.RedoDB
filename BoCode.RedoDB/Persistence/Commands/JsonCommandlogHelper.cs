using System.Collections.Generic;

namespace BoCode.RedoDB.Persistence.Commands
{
    /// <summary>
    /// This class contains helper function to deal with the content
    /// of the json commandlog.
    /// </summary>
    public class JsonCommandlogHelper
    {
        private readonly string _commandlog;

        public JsonCommandlogHelper(string commandlog)
        {
            _commandlog = commandlog;
        }
        public IEnumerable<string> GetCommandJsons()
        {
            //counts the number of '{'
            int openCount = 0;
            int firstCurlyBracePosition = 0;
            List<string> jsons = new List<string>();
            char[] chars = _commandlog.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] == '{')
                {
                    if (openCount == 0) firstCurlyBracePosition = i;
                    openCount++;
                }
                if (chars[i] == '}')
                {
                    openCount--;
                    if (openCount == 0)
                    {
                        //closing curly brace of a command json found. Extract the command json.
                        jsons.Add(_commandlog.Substring(firstCurlyBracePosition, i - firstCurlyBracePosition + 1));
                    }

                }
            }
            return jsons;
        }
    }
}
