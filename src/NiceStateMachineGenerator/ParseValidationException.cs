using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiceStateMachineGenerator
{
    public class ParseValidationException : Exception
    {
        public readonly JToken? Token;

        public ParseValidationException(JToken? token, string message) 
            : this(token, message, null)
        {
        }

        public ParseValidationException(JToken? token, string message, Exception? innerException) 
            : base(message + FormatPath(token), innerException)
        {
            this.Token = token;
        }

        private static string FormatPath(JToken? token)
        {
            if (token == null)
            {
                return "";
            }
            IJsonLineInfo lineInfo = token;
            return $", path: {token.Path}; line: {lineInfo.LineNumber}, pos: {lineInfo.LinePosition}";
        }
    }
}
