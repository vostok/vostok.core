﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Vostok.Airlock
{
    // todo (spaceorc 05.10.2017) add unit tests
    public static class RoutingKey
    {
        public const string Separator = ".";
        public const string AppEventsSuffix = "app-events";
        public const string TraceEventsSuffix = "trace-events";
        public const string MetricsSuffix = "metrics";
        public const string TracesSuffix = "traces";
        public const string LogsSuffix = "logs";

        private const char UnacceptableCharPlaceholder = '-';

        public static string Create(string project, string environment, string service, params string[] suffix)
        {
            return AddSuffix(CreatePrefix(project, environment, service), suffix);
        }

        public static string CreatePrefix(string project, string environment, string service)
        {
            return Create(new[] {project, environment, service});
        }

        public static string AddSuffix(string prefix, params string[] suffix)
        {
            if (suffix == null || suffix.Length == 0)
                return prefix;
            return prefix + Separator + Create(suffix);
        }

        public static string ReplaceSuffix(string routingKey, params string[] suffix)
        {
            Parse(routingKey, out var project, out var env, out var service, out _);
            return Create(project, env, service, suffix);
        }

        public static void Parse(string routingKey, out string project, out string environment, out string service, out string[] suffix)
        {
            if (routingKey == null)
                throw new ArgumentNullException(nameof(routingKey));
            if (!TryParse(routingKey, out project, out environment, out service, out suffix))
                throw new InvalidOperationException($"Couldn't parse routing key '{routingKey}'");
        }

        public static bool TryParse(string routingKey, out string project, out string environment, out string service, out string[] suffix)
        {
            if (routingKey == null)
            {
                project = null;
                environment = null;
                service = null;
                suffix = new string[] {};
                return false;
            }
            var routingKeyParts = routingKey.Split(Separator[0]);
            if (routingKeyParts.Any(keyPart => !IsValidPart(keyPart)))
            {
                project = null;
                environment = null;
                service = null;
                suffix = new string[] {};
                return false;
            }
            project = GetRoutingKeyPart(routingKeyParts, 0);
            environment = GetRoutingKeyPart(routingKeyParts, 1);
            service = GetRoutingKeyPart(routingKeyParts, 2);
            suffix = routingKeyParts.Skip(3).ToArray();
            return project != null && environment != null && service != null;
        }

        private static string GetRoutingKeyPart(string[] routingKeyParts, int index)
        {
            return routingKeyParts.Length > index && !string.IsNullOrEmpty(routingKeyParts[index]) ? routingKeyParts[index] : null;
        }

        private static string Create(IEnumerable<string> parts)
        {
            return string.Join(Separator, parts.Select(FixInvalidChars));
        }

        private static string FixInvalidChars(string s)
        {
            if (string.IsNullOrEmpty(s))
                throw new ArgumentException("string is null or empty", nameof(s));
            if (s.All(IsAcceptableChar))
                return s;
            var sb = new StringBuilder(s);
            for (var i = 0; i < sb.Length; i++)
            {
                if (IsAcceptableChar(sb[i]))
                    continue;
                if (IsAcceptableUpperCaseChar(sb[i]))
                    sb[i] = char.ToLower(sb[i]);
                else
                    sb[i] = UnacceptableCharPlaceholder;
            }
            return sb.ToString();
        }

        private static bool IsValidPart(string s)
        {
            if (string.IsNullOrEmpty(s))
                return false;
            return s.All(t => IsAcceptableChar(t) || IsAcceptableUpperCaseChar(t));
        }

        private static bool IsAcceptableChar(char c)
        {
            return c >= 'a' && c <= 'z'
                   || c >= '0' && c <= '9'
                   || c == UnacceptableCharPlaceholder;
        }

        private static bool IsAcceptableUpperCaseChar(char c)
        {
            return c >= 'A' && c <= 'Z';
        }
    }
}