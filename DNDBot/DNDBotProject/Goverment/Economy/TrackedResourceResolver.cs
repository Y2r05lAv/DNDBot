#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using DNDBot.DataBase;

namespace DNDBot.Goverment.Economy
{
    internal static class TrackedResourceResolver
    {
        public static bool TryResolveTrackedType(
            string? resourceType,
            IReadOnlyDictionary<string, EconomyResourceRecord>? resources,
            out ResourceType trackedType)
        {
            trackedType = default;
            string? trackedName = ResolveTrackedResourceName(resourceType, resources);
            return !string.IsNullOrWhiteSpace(trackedName) &&
                   Enum.TryParse<ResourceType>(trackedName, true, out trackedType);
        }

        public static string? ResolveTrackedResourceName(
            string? resourceType,
            IReadOnlyDictionary<string, EconomyResourceRecord>? resources)
        {
            string? current = NormalizeResourceValue(resourceType);
            if (string.IsNullOrWhiteSpace(current))
                return null;

            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int depth = 0; depth < 16 && !string.IsNullOrWhiteSpace(current) && visited.Add(current); depth++)
            {
                if (TryResolveTrackedResourceNameDirect(current, out string tracked))
                    return tracked;

                if (!TryGetResource(resources, current, out var record))
                    return null;

                current = NormalizeResourceValue(record.ParentResourceType);
            }

            return null;
        }

        private static bool TryResolveTrackedResourceNameDirect(string value, out string trackedName)
        {
            trackedName = string.Empty;
            string key = NormalizeToken(value);
            switch (key)
            {
                case "food":
                case "їжа":
                case "харчі":
                case "продовольство":
                case "еда":
                case "пища":
                case "izha":
                case "harchi":
                case "prodovolstvo":
                    trackedName = nameof(ResourceType.Food);
                    return true;

                case "clothing":
                case "одяг":
                case "одежда":
                case "odyag":
                    trackedName = nameof(ResourceType.Clothing);
                    return true;

                case "housing":
                case "житло":
                case "жилье":
                case "жильё":
                case "zhytlo":
                    trackedName = nameof(ResourceType.Housing);
                    return true;

                case "tools":
                case "інструменти":
                case "знаряддя":
                case "инструменты":
                case "instrumenty":
                    trackedName = nameof(ResourceType.Tools);
                    return true;

                case "luxury":
                case "розкіш":
                case "роскошь":
                case "rozkish":
                    trackedName = nameof(ResourceType.Luxury);
                    return true;
            }

            return false;
        }

        private static bool TryGetResource(
            IReadOnlyDictionary<string, EconomyResourceRecord>? resources,
            string resourceType,
            out EconomyResourceRecord record)
        {
            record = new EconomyResourceRecord();
            if (resources == null || resources.Count == 0 || string.IsNullOrWhiteSpace(resourceType))
                return false;

            if (resources.TryGetValue(resourceType, out var exact) && exact != null)
            {
                record = exact;
                return true;
            }

            string normalized = NormalizeToken(resourceType);
            var fallback = resources
                .Where(r => NormalizeToken(r.Key) == normalized)
                .Select(r => r.Value)
                .FirstOrDefault();

            if (fallback == null)
                return false;

            record = fallback;
            return true;
        }

        private static string? NormalizeResourceValue(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string NormalizeToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var chars = new List<char>(value.Length);
            foreach (char c in value.Trim())
            {
                if (char.IsWhiteSpace(c) || c == '_' || c == '-')
                    continue;
                chars.Add(char.ToLowerInvariant(c));
            }
            return new string(chars.ToArray());
        }
    }
}
