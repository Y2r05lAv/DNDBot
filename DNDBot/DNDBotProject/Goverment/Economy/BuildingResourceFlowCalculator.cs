#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace DNDBot.Goverment.Economy
{
    internal sealed class BuildingResourceFlowPlan
    {
        public int BuildingId { get; set; }
        public string OutputResourceType { get; set; } = string.Empty;
        public decimal OutputAmount { get; set; }
        public string? InputResourceType { get; set; }
        public decimal InputAmount { get; set; }
    }

    internal sealed class BuildingResourceFlowResult
    {
        public Dictionary<int, decimal> OutputByBuildingId { get; } = new();
        public Dictionary<string, decimal> ProducedByResource { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, decimal> ConsumedByResource { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, decimal> AvailableByResource { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    internal static class BuildingResourceFlowCalculator
    {
        public static BuildingResourceFlowResult Calculate(
            IReadOnlyCollection<BuildingResourceFlowPlan>? plans,
            IReadOnlyDictionary<string, decimal>? initialSupply = null,
            IReadOnlyDictionary<string, decimal>? exportByResource = null)
        {
            var result = new BuildingResourceFlowResult();
            if (initialSupply != null)
            {
                foreach (var pair in initialSupply)
                {
                    string key = NormalizeResourceKey(pair.Key);
                    decimal qty = Math.Max(0m, pair.Value);
                    if (string.IsNullOrWhiteSpace(key) || qty <= 0m)
                        continue;
                    AddQuantity(result.AvailableByResource, key, qty);
                }
            }

            if (plans == null || plans.Count == 0)
            {
                ApplyExports(result.AvailableByResource, exportByResource);
                return result;
            }

            var normalizedPlans = plans
                .Where(p => p != null)
                .Select(p => new BuildingResourceFlowPlan
                {
                    BuildingId = p.BuildingId,
                    OutputResourceType = NormalizeResourceKey(p.OutputResourceType),
                    OutputAmount = Math.Max(0m, p.OutputAmount),
                    InputResourceType = NormalizeResourceKey(p.InputResourceType),
                    InputAmount = Math.Max(0m, p.InputAmount)
                })
                .Where(p => !string.IsNullOrWhiteSpace(p.OutputResourceType) && p.OutputAmount > 0m)
                .ToList();

            if (normalizedPlans.Count == 0)
            {
                ApplyExports(result.AvailableByResource, exportByResource);
                return result;
            }

            var producersByResource = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < normalizedPlans.Count; i++)
            {
                string resource = normalizedPlans[i].OutputResourceType;
                if (!producersByResource.TryGetValue(resource, out var indices))
                {
                    indices = new List<int>();
                    producersByResource[resource] = indices;
                }
                indices.Add(i);
            }

            var depthCache = new Dictionary<int, int>();
            int ResolveDepth(int index, HashSet<int> visiting)
            {
                if (depthCache.TryGetValue(index, out var cached))
                    return cached;

                var plan = normalizedPlans[index];
                if (!RequiresInput(plan))
                {
                    depthCache[index] = 0;
                    return 0;
                }

                string inputResource = plan.InputResourceType ?? string.Empty;
                if (!producersByResource.TryGetValue(inputResource, out var producers) || producers.Count == 0)
                {
                    depthCache[index] = 1;
                    return 1;
                }

                if (!visiting.Add(index))
                {
                    depthCache[index] = 50;
                    return 50;
                }

                int minProducerDepth = int.MaxValue;
                foreach (int producerIndex in producers)
                {
                    if (producerIndex == index)
                        continue;
                    int producerDepth = ResolveDepth(producerIndex, visiting);
                    if (producerDepth < minProducerDepth)
                        minProducerDepth = producerDepth;
                }

                visiting.Remove(index);

                int depth = minProducerDepth == int.MaxValue ? 1 : Math.Min(50, minProducerDepth + 1);
                depthCache[index] = depth;
                return depth;
            }

            var executionOrder = Enumerable.Range(0, normalizedPlans.Count)
                .OrderBy(i => ResolveDepth(i, new HashSet<int>()))
                .ThenBy(i => normalizedPlans[i].BuildingId)
                .ThenBy(i => i)
                .ToList();

            foreach (int index in executionOrder)
            {
                var plan = normalizedPlans[index];
                decimal productionRatio = 1m;
                if (RequiresInput(plan))
                {
                    string inputResource = plan.InputResourceType ?? string.Empty;
                    decimal requiredInput = plan.InputAmount;
                    decimal availableInput = GetQuantity(result.AvailableByResource, inputResource);
                    productionRatio = availableInput <= 0m
                        ? 0m
                        : Math.Min(1m, availableInput / requiredInput);

                    decimal consumedInput = requiredInput * productionRatio;
                    if (consumedInput > 0m)
                    {
                        result.AvailableByResource[inputResource] = Math.Max(0m, availableInput - consumedInput);
                        AddQuantity(result.ConsumedByResource, inputResource, consumedInput);
                    }
                }

                decimal produced = plan.OutputAmount * productionRatio;
                if (produced <= 0m)
                    continue;

                AddQuantity(result.OutputByBuildingId, plan.BuildingId, produced);
                AddQuantity(result.ProducedByResource, plan.OutputResourceType, produced);
                AddQuantity(result.AvailableByResource, plan.OutputResourceType, produced);
            }

            ApplyExports(result.AvailableByResource, exportByResource);
            return result;
        }

        private static bool RequiresInput(BuildingResourceFlowPlan plan)
        {
            return plan != null &&
                   plan.InputAmount > 0m &&
                   !string.IsNullOrWhiteSpace(plan.InputResourceType);
        }

        private static string NormalizeResourceKey(string? key)
        {
            return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim();
        }

        private static void AddQuantity(Dictionary<string, decimal> target, string key, decimal qty)
        {
            if (qty == 0m || string.IsNullOrWhiteSpace(key))
                return;

            if (target.TryGetValue(key, out var current))
                target[key] = current + qty;
            else
                target[key] = qty;
        }

        private static void AddQuantity(Dictionary<int, decimal> target, int key, decimal qty)
        {
            if (qty == 0m)
                return;

            if (target.TryGetValue(key, out var current))
                target[key] = current + qty;
            else
                target[key] = qty;
        }

        private static decimal GetQuantity(Dictionary<string, decimal> source, string key)
        {
            return source.TryGetValue(key, out var value) ? value : 0m;
        }

        private static void ApplyExports(Dictionary<string, decimal> available, IReadOnlyDictionary<string, decimal>? exportByResource)
        {
            if (available == null || exportByResource == null || exportByResource.Count == 0)
                return;

            foreach (var pair in exportByResource)
            {
                string key = NormalizeResourceKey(pair.Key);
                decimal qty = Math.Max(0m, pair.Value);
                if (string.IsNullOrWhiteSpace(key) || qty <= 0m)
                    continue;

                decimal current = GetQuantity(available, key);
                available[key] = Math.Max(0m, current - qty);
            }
        }
    }
}
