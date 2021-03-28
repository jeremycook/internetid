#nullable enable

using Microsoft.AspNetCore.Routing;
using System;
using System.Text.RegularExpressions;

namespace InternetId.Server
{
    public class SlugifyParameterTransformer : IOutboundParameterTransformer
    {
        private static readonly Regex pattern = new("([a-z])([A-Z])", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(10));
        private const string replacement = "$1-$2";

        public string? TransformOutbound(object? value)
        {
            if (value is string asString)
            {
                return pattern.Replace(asString, replacement).ToLowerInvariant();
            }
            else if (value?.ToString() is string toString)
            {
                return pattern.Replace(toString, replacement).ToLowerInvariant();
            }
            else
            {
                return null;
            }
        }
    }
}
