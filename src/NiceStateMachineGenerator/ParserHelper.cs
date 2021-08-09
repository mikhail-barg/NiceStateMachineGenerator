using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiceStateMachineGenerator
{
    internal static class ParserHelper
    {
        public static void CheckTokenType(JToken token, string tokenName, JTokenType expectedType)
        {
            if (token.Type != expectedType)
            {
                throw new ParseValidationException(token, $"Token '{tokenName}' should be {expectedType}, but it is {token.Type}");
            };
        }

        public static void CheckAllTokensHandled(JObject jObject, HashSet<string> handledTokens)
        {
            foreach (KeyValuePair<string, JToken?> pair in jObject)
            {
                if (!handledTokens.Contains(pair.Key))
                {
                    throw new ParseValidationException(pair.Value, $"Unused token '{pair.Key}'");
                };
            }
        }

        public static JToken? GetJToken(JObject container, string tokenName, HashSet<string> handledTokens, bool required)
        {
            JToken? result;
            if (!container.TryGetValue(tokenName, out result))
            {
                if (required)
                {
                    throw new ParseValidationException(container, $"Missing required token '{tokenName}'");
                };
                return null;
            };
            if (!handledTokens.Add(tokenName))
            {
                throw new ApplicationException($"Trying to get '{tokenName}' second time");
            };
            return result;
        }

        public static JObject? GetJObject(JObject container, string tokenName, HashSet<string> handledTokens, bool required)
        {
            JToken? token = GetJToken(container, tokenName, handledTokens, required);
            if (token == null)
            {
                return null;
            };
            CheckTokenType(token, tokenName, JTokenType.Object);
            return (JObject)token;
        }

        public static JObject GetJObjectRequired(JObject container, string tokenName, HashSet<string> handledTokens)
        {
#pragma warning disable CS8603 // no nulls when required
            return GetJObject(container, tokenName, handledTokens, required: true);
#pragma warning restore CS8603
        }

        public static JArray? GetJArray(JObject container, string tokenName, HashSet<string> handledTokens, bool required)
        {
            JToken? token = GetJToken(container, tokenName, handledTokens, required);
            if (token == null)
            {
                return null;
            };
            CheckTokenType(token, tokenName, JTokenType.Array);
            return (JArray)token;
        }

        public static string? GetJString(JObject container, string tokenName, HashSet<string> handledTokens, out JToken? token, bool required)
        {
            token = GetJToken(container, tokenName, handledTokens, required);
            if (token == null)
            {
                return null;
            };
            if (token.Type == JTokenType.Null)
            {
                if (required)
                {
                    throw new ParseValidationException(token, $"Required token '{token}' is null");
                };
                return null;
            };
            CheckTokenType(token, tokenName, JTokenType.String);
            return (string?)token;
        }

        public static string GetJStringRequired(JObject container, string tokenName, HashSet<string> handledTokens, out JToken token)
        {
#pragma warning disable CS8600, CS8601, CS8603 // no nulls when required
            string result = GetJString(container, tokenName, handledTokens, out JToken? tokenInternal, required: true);
            token = tokenInternal;
            return result;
#pragma warning restore CS8600, CS8601, CS8603 
        }

        public static string CheckAndConvertToString(JToken token, string tokenName)
        {
            CheckTokenType(token, tokenName, JTokenType.String);
#pragma warning disable CS8600, CS8603 // strings are not null.
            return (string)token;
#pragma warning restore CS8600, CS8603
        }

        public static bool GetJBoolWithDefault(JObject container, string tokenName, bool defaultValue, HashSet<string> handledTokens)
        {
            JToken? token = GetJToken(container, tokenName, handledTokens, false);
            if (token == null)
            {
                return defaultValue;
            }
            CheckTokenType(token, tokenName, JTokenType.Boolean);
            return (bool)token;
        }

        public static double? GetJDouble(JObject container, string tokenName, HashSet<string> handledTokens, bool required)
        {
            JToken? token = GetJToken(container, tokenName, handledTokens, required);
            if (token == null)
            {
                return null;
            };
            switch (token.Type)
            {
            case JTokenType.Null:
                return null;
            case JTokenType.Float:
                return (double)token;
            case JTokenType.Integer:
                return (double)(int)token;
            default:
                throw new ParseValidationException(token, $"token {tokenName} should be Float or Int, but it is {token.Type}");
            }
        }
    }
}
