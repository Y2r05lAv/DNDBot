#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using DNDBot.DataBase;

namespace DNDBot.Goverment.Economy
{
    internal sealed class ProductionBalanceOptions
    {
        public bool StrictEconomyMode { get; set; } = true;
        public int SurplusTargetCount { get; set; } = 4;
        public decimal SurplusCoverageRatio { get; set; } = 1.08m;
        public decimal NearBalanceCoverageRatio { get; set; } = 0.98m;
        public decimal LeaderCoverageRatio { get; set; } = 1.45m;
        public bool CollapseSurplusTargetsWhenAllSurplus { get; set; } = true;
        public bool EnableOversupplyCorrection { get; set; }
        public decimal OversupplyCeilingRatio { get; set; } = 1.18m;
        public decimal LeaderOversupplyCeilingRatio { get; set; } = 1.55m;
        public int MaxBuildingsToDisable { get; set; } = 60;
        public bool AllowFallbackDisableAnyProduction { get; set; }
        public int MaxIterations { get; set; } = 4;
        public int MaxBuildingsToAdd { get; set; } = 90;
        public string? LeaderResourceOverride { get; set; }
    }

    internal sealed class ProductionResourceSnapshot
    {
        public string ResourceType { get; set; } = string.Empty;
        public decimal Demand { get; set; }
        public decimal Available { get; set; }
        public decimal CoveragePercent { get; set; }
        public bool TargetSurplus { get; set; }
    }

    internal sealed class ProductionBalanceResult
    {
        public int CountryId { get; set; }
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? LeaderResource { get; set; }
        public int AddedBuildings { get; set; }
        public long AddedJobs { get; set; }
        public decimal AddedBaseProduction { get; set; }
        public int DisabledBuildings { get; set; }
        public long DisabledJobs { get; set; }
        public decimal ReducedBaseProduction { get; set; }
        public Dictionary<string, ProductionResourceSnapshot> BeforeByResource { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, ProductionResourceSnapshot> AfterByResource { get; } = new(StringComparer.OrdinalIgnoreCase);

        public int SurplusBefore => BeforeByResource.Values.Count(v => v.CoveragePercent >= 100m);
        public int DeficitBefore => BeforeByResource.Values.Count(v => v.CoveragePercent < 100m);
        public int SurplusAfter => AfterByResource.Values.Count(v => v.CoveragePercent >= 100m);
        public int DeficitAfter => AfterByResource.Values.Count(v => v.CoveragePercent < 100m);
    }

    internal static class ProductionBalancingService
    {
        private const string LeaderResourceConfigKey = "ProductionLeaderResourceIndex";
        private const decimal Level5ProductionMultiplier = 1.9388m;
        private const decimal Level5UpkeepMultiplier = 1.4641m;

        private static readonly string[] TrackedResources =
        {
            "Food",
            "Clothing",
            "Housing",
            "Tools",
            "Luxury"
        };

        private sealed class ProductionTemplate
        {
            public string BuildingType { get; init; } = string.Empty;
            public decimal ProductionPerJob { get; init; }
            public decimal UpkeepPerJob { get; init; }
            public bool StateOwned { get; init; }
            public float StabilityBonus { get; init; }
            public float CorruptionReduction { get; init; }
        }

        private static readonly Dictionary<string, ProductionTemplate> TemplateByResource = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Food"] = new ProductionTemplate
            {
                BuildingType = "Агроіндустріальний кластер",
                ProductionPerJob = 26m,
                UpkeepPerJob = 0.012m,
                StateOwned = false,
                StabilityBonus = 0.4f,
                CorruptionReduction = 0.2f
            },
            ["Clothing"] = new ProductionTemplate
            {
                BuildingType = "Текстильний комбінат",
                ProductionPerJob = 21m,
                UpkeepPerJob = 0.013m,
                StateOwned = false,
                StabilityBonus = 0.2f,
                CorruptionReduction = 0.1f
            },
            ["Housing"] = new ProductionTemplate
            {
                BuildingType = "Будівельний трест",
                ProductionPerJob = 16m,
                UpkeepPerJob = 0.012m,
                StateOwned = true,
                StabilityBonus = 0.5f,
                CorruptionReduction = 0.1f
            },
            ["Tools"] = new ProductionTemplate
            {
                BuildingType = "Машинобудівний завод",
                ProductionPerJob = 20m,
                UpkeepPerJob = 0.014m,
                StateOwned = false,
                StabilityBonus = 0.1f,
                CorruptionReduction = 0.1f
            },
            ["Luxury"] = new ProductionTemplate
            {
                BuildingType = "Мануфактура розкоші",
                ProductionPerJob = 14m,
                UpkeepPerJob = 0.015m,
                StateOwned = false,
                StabilityBonus = 0.2f,
                CorruptionReduction = 0.1f
            }
        };

        public static ProductionBalanceResult AnalyzeCountry(int countryId, ProductionBalanceOptions? options = null)
        {
            options ??= new ProductionBalanceOptions();
            var result = new ProductionBalanceResult { CountryId = countryId };

            if (countryId <= 0)
            {
                result.Error = "InvalidCountryId";
                return result;
            }

            var country = CountriesData.GetCountryById(countryId);
            if (country == null)
            {
                result.Error = "CountryNotFound";
                return result;
            }

            string? leaderResource = NormalizeTrackedResource(options.LeaderResourceOverride) ?? GetLeaderResource(countryId);
            result.LeaderResource = leaderResource;

            var buildings = EconomyBuildingsData.GetByCountry(countryId);
            var snapshots = BuildCoverageSnapshots(country, buildings);
            var targets = DetermineSurplusTargets(snapshots, options, leaderResource);
            ApplyTargetFlags(snapshots, targets);

            CopySnapshots(snapshots, result.BeforeByResource);
            CopySnapshots(snapshots, result.AfterByResource);
            result.Success = true;
            return result;
        }

        public static ProductionBalanceResult BalanceCountryProduction(int countryId, ProductionBalanceOptions? options = null)
        {
            options ??= new ProductionBalanceOptions();
            var result = new ProductionBalanceResult { CountryId = countryId };

            if (countryId <= 0)
            {
                result.Error = "InvalidCountryId";
                return result;
            }

            var country = CountriesData.GetCountryById(countryId);
            if (country == null)
            {
                result.Error = "CountryNotFound";
                return result;
            }

            string? leaderResource = NormalizeTrackedResource(options.LeaderResourceOverride) ?? GetLeaderResource(countryId);
            result.LeaderResource = leaderResource;

            var workingBuildings = EconomyBuildingsData.GetByCountry(countryId);
            var beforeSnapshots = BuildCoverageSnapshots(country, workingBuildings);
            var targets = DetermineSurplusTargets(beforeSnapshots, options, leaderResource);
            ApplyTargetFlags(beforeSnapshots, targets);
            CopySnapshots(beforeSnapshots, result.BeforeByResource);

            int remainingBuildings = Math.Max(0, options.MaxBuildingsToAdd);
            int remainingDisable = Math.Max(0, options.MaxBuildingsToDisable);
            int maxIterations = Math.Max(1, options.MaxIterations);

            for (int iteration = 0; iteration < maxIterations &&
                 (remainingBuildings > 0 || (options.EnableOversupplyCorrection && remainingDisable > 0)); iteration++)
            {
                var currentSnapshots = BuildCoverageSnapshots(country, workingBuildings);
                ApplyTargetFlags(currentSnapshots, targets);
                bool anyChange = false;

                if (options.EnableOversupplyCorrection && remainingDisable > 0)
                {
                    foreach (string resource in TrackedResources)
                    {
                        if (!currentSnapshots.TryGetValue(resource, out var snapshot))
                            continue;
                        if (snapshot.Demand <= 0m || snapshot.Available <= 0m)
                            continue;

                        decimal maxCoverage = snapshot.TargetSurplus
                            ? options.OversupplyCeilingRatio
                            : options.NearBalanceCoverageRatio;
                        if (!string.IsNullOrWhiteSpace(leaderResource) &&
                            resource.Equals(leaderResource, StringComparison.OrdinalIgnoreCase))
                        {
                            maxCoverage = Math.Max(maxCoverage, options.LeaderOversupplyCeilingRatio);
                        }

                        maxCoverage = ClampDecimal(maxCoverage, 0.1m, 3m);
                        decimal maxAvailable = snapshot.Demand * maxCoverage;
                        decimal excess = snapshot.Available - maxAvailable;
                        decimal tolerance = Math.Max(50m, snapshot.Demand * 0.004m);
                        if (excess <= tolerance)
                            continue;

                        bool reduced = ReduceResourceOversupply(
                            country,
                            resource,
                            excess,
                            workingBuildings,
                            options,
                            result,
                            ref remainingDisable);

                        if (reduced)
                            anyChange = true;
                        if (remainingDisable <= 0)
                            break;
                    }

                    if (anyChange)
                    {
                        currentSnapshots = BuildCoverageSnapshots(country, workingBuildings);
                        ApplyTargetFlags(currentSnapshots, targets);
                    }
                }

                foreach (string resource in TrackedResources)
                {
                    if (!currentSnapshots.TryGetValue(resource, out var snapshot))
                        continue;
                    if (snapshot.Demand <= 0m)
                        continue;

                    decimal targetCoverage = snapshot.TargetSurplus
                        ? options.SurplusCoverageRatio
                        : options.NearBalanceCoverageRatio;

                    if (!string.IsNullOrWhiteSpace(leaderResource) &&
                        resource.Equals(leaderResource, StringComparison.OrdinalIgnoreCase))
                    {
                        targetCoverage = Math.Max(targetCoverage, options.LeaderCoverageRatio);
                    }

                    targetCoverage = ClampDecimal(targetCoverage, 0.1m, 2.5m);
                    decimal targetAvailable = snapshot.Demand * targetCoverage;
                    decimal shortfall = targetAvailable - snapshot.Available;
                    decimal tolerance = Math.Max(50m, snapshot.Demand * 0.004m);
                    if (shortfall <= tolerance)
                        continue;

                    bool added = AddLevelFiveBuildingsForResource(
                        country,
                        resource,
                        shortfall,
                        workingBuildings,
                        ref remainingBuildings,
                        result);

                    if (added)
                        anyChange = true;
                    if (remainingBuildings <= 0)
                        break;
                }

                if (!anyChange)
                    break;
            }

            var afterSnapshots = BuildCoverageSnapshots(country, workingBuildings);
            ApplyTargetFlags(afterSnapshots, targets);
            CopySnapshots(afterSnapshots, result.AfterByResource);
            result.Success = true;
            return result;
        }

        public static string? GetLeaderResource(int countryId)
        {
            if (countryId <= 0)
                return null;

            decimal raw = EconomyConfigData.GetDecimalForCountry(countryId, LeaderResourceConfigKey, 0m);
            int index = Math.Clamp((int)Math.Round(raw, MidpointRounding.AwayFromZero), 0, 5);
            return LeaderIndexToResource(index);
        }

        public static bool SetLeaderResource(int countryId, string? resourceType)
        {
            if (countryId <= 0)
                return false;

            string? normalized = NormalizeTrackedResource(resourceType);
            int index = ResourceToLeaderIndex(normalized);
            return EconomyConfigData.UpsertCountryDecimal(countryId, LeaderResourceConfigKey, index);
        }

        public static string? NormalizeTrackedResource(string? resourceType)
        {
            if (string.IsNullOrWhiteSpace(resourceType))
                return null;

            string key = NormalizeToken(resourceType);
            return key switch
            {
                "food" or "izha" => "Food",
                "clothing" or "odyag" => "Clothing",
                "housing" or "zhytlo" => "Housing",
                "tools" or "instrumenty" => "Tools",
                "luxury" or "rozkish" => "Luxury",
                _ => null
            };
        }

        private static int ResourceToLeaderIndex(string? resourceType)
        {
            return resourceType switch
            {
                "Food" => 1,
                "Clothing" => 2,
                "Housing" => 3,
                "Tools" => 4,
                "Luxury" => 5,
                _ => 0
            };
        }

        private static string? LeaderIndexToResource(int index)
        {
            return index switch
            {
                1 => "Food",
                2 => "Clothing",
                3 => "Housing",
                4 => "Tools",
                5 => "Luxury",
                _ => null
            };
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

        private static void CopySnapshots(
            IReadOnlyDictionary<string, ProductionResourceSnapshot> source,
            IDictionary<string, ProductionResourceSnapshot> destination)
        {
            destination.Clear();
            foreach (var pair in source)
            {
                destination[pair.Key] = new ProductionResourceSnapshot
                {
                    ResourceType = pair.Value.ResourceType,
                    Demand = pair.Value.Demand,
                    Available = pair.Value.Available,
                    CoveragePercent = pair.Value.CoveragePercent,
                    TargetSurplus = pair.Value.TargetSurplus
                };
            }
        }

        private static Dictionary<string, ProductionResourceSnapshot> BuildCoverageSnapshots(
            CountryRecord country,
            IReadOnlyCollection<EconomyBuildingRecord> buildings)
        {
            var demand = EstimateDemandByResource(country);
            var available = EstimateAvailableByResource(country, buildings);
            var snapshots = new Dictionary<string, ProductionResourceSnapshot>(StringComparer.OrdinalIgnoreCase);

            foreach (string resource in TrackedResources)
            {
                demand.TryGetValue(resource, out decimal resourceDemand);
                available.TryGetValue(resource, out decimal resourceAvailable);
                decimal coverage = resourceDemand <= 0m ? 100m : (resourceAvailable / resourceDemand) * 100m;
                snapshots[resource] = new ProductionResourceSnapshot
                {
                    ResourceType = resource,
                    Demand = Math.Max(0m, resourceDemand),
                    Available = Math.Max(0m, resourceAvailable),
                    CoveragePercent = coverage,
                    TargetSurplus = false
                };
            }

            return snapshots;
        }

        private static HashSet<string> DetermineSurplusTargets(
            IReadOnlyDictionary<string, ProductionResourceSnapshot> snapshots,
            ProductionBalanceOptions options,
            string? leaderResource)
        {
            int targetCount = options.StrictEconomyMode
                ? Math.Clamp(options.SurplusTargetCount, 3, 4)
                : Math.Clamp(options.SurplusTargetCount, 1, TrackedResources.Length);

             
             
            if (options.CollapseSurplusTargetsWhenAllSurplus && IsAllTrackedResourcesInSurplus(snapshots))
                targetCount = 1;

            var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(leaderResource))
                targets.Add(leaderResource);

            var ordered = snapshots.Values
                .OrderByDescending(v => v.CoveragePercent)
                .ThenByDescending(v => v.Available)
                .ThenBy(v => v.ResourceType, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var row in ordered)
            {
                if (targets.Count >= targetCount)
                    break;
                targets.Add(row.ResourceType);
            }

            return targets;
        }

        private static void ApplyTargetFlags(
            IDictionary<string, ProductionResourceSnapshot> snapshots,
            ISet<string> targets)
        {
            foreach (var row in snapshots.Values)
                row.TargetSurplus = targets.Contains(row.ResourceType);
        }

        private static bool IsAllTrackedResourcesInSurplus(
            IReadOnlyDictionary<string, ProductionResourceSnapshot> snapshots)
        {
            bool hasDemand = false;
            foreach (var row in snapshots.Values)
            {
                if (row.Demand <= 0m)
                    continue;

                hasDemand = true;
                if (row.CoveragePercent < 100m)
                    return false;
            }

            return hasDemand;
        }

        private static bool ReduceResourceOversupply(
            CountryRecord country,
            string resourceType,
            decimal excess,
            List<EconomyBuildingRecord> workingBuildings,
            ProductionBalanceOptions options,
            ProductionBalanceResult result,
            ref int remainingDisable)
        {
            if (country == null ||
                string.IsNullOrWhiteSpace(resourceType) ||
                excess <= 0m ||
                remainingDisable <= 0 ||
                workingBuildings == null ||
                workingBuildings.Count == 0)
            {
                return false;
            }

            float warModifier = country.WarState ? EconomyConfigData.GetFloat("WarProductionModifier", 0.85f) : 1f;
            float sanctionsModifier = EconomyCalculator.ApplySanctionsModifier(
                (SanctionsLevel)country.SanctionsLevel,
                EconomyConfigData.GetFloat("SanctionsNoneModifier", 1f),
                EconomyConfigData.GetFloat("SanctionsPartialModifier", 0.9f),
                EconomyConfigData.GetFloat("SanctionsFullModifier", 0.7f));

            float productionEfficiency = EconomyCalculator.ProductionEfficiency(
                1f,
                country.Stability,
                country.Corruption,
                warModifier,
                sanctionsModifier,
                0f);

            float stepPenalty = EconomyConfigData.GetFloat("BuildingDegradeStepPenalty", 0.1f);
            var resourcesByType = LoadResourceMap();

            var candidates = workingBuildings
                .Where(b => CanReduceBuildingForResource(b, resourceType, resourcesByType))
                .OrderByDescending(b => IsAutoBalancedLevelFiveBuilding(b) ? 1 : 0)
                .ThenByDescending(b => ComputeEffectiveBuildingOutput(b, productionEfficiency, stepPenalty, resourcesByType))
                .ThenByDescending(b => b.BaseProduction)
                .ThenBy(b => b.BuildingId)
                .ToList();

            if (candidates.Count == 0 && options.AllowFallbackDisableAnyProduction)
            {
                candidates = workingBuildings
                    .Where(b => IsProductionBuilding(b) && !IsUnderConstruction(b) &&
                                !string.Equals(b.BuildingStatus, BuildingStatus.Abandoned.ToString(), StringComparison.OrdinalIgnoreCase) &&
                                b.BaseProduction > 0m)
                    .OrderByDescending(b => IsAutoBalancedLevelFiveBuilding(b) ? 1 : 0)
                    .ThenByDescending(b => ComputeEffectiveBuildingOutput(b, productionEfficiency, stepPenalty, resourcesByType))
                    .ThenByDescending(b => b.BaseProduction)
                    .ThenBy(b => b.BuildingId)
                    .ToList();
            }

            if (candidates.Count == 0)
                return false;

            decimal remainingExcess = excess;
            bool changed = false;

            foreach (var building in candidates)
            {
                if (remainingDisable <= 0 || remainingExcess <= 0m)
                    break;

                decimal effectiveOutput = ComputeEffectiveBuildingOutput(building, productionEfficiency, stepPenalty, resourcesByType);
                if (effectiveOutput <= 0m)
                    continue;

                decimal oldBase = building.BaseProduction;
                decimal reductionShare = ClampDecimal(remainingExcess / effectiveOutput, 0m, 1m);
                if (reductionShare <= 0m)
                    continue;

                decimal newBase = decimal.Round(oldBase * (1m - reductionShare), 2, MidpointRounding.AwayFromZero);
                if (reductionShare >= 0.995m || newBase < 0.01m)
                    newBase = 0m;
                if (newBase >= oldBase)
                    continue;

                bool updated = EconomyBuildingsData.UpdateBuildingField(
                    building.BuildingId,
                    country.CountryId,
                    "BaseProduction",
                    newBase,
                    SqlDbType.Decimal);

                if (!updated)
                    continue;

                building.BaseProduction = newBase;
                changed = true;
                remainingDisable--;

                decimal reducedBase = oldBase - newBase;
                decimal reducedOutput = effectiveOutput * reductionShare;
                remainingExcess -= Math.Max(0m, reducedOutput);

                result.DisabledBuildings++;
                result.DisabledJobs += (long)Math.Round(
                    Math.Max(0, building.JobsProvided) * (double)reductionShare,
                    MidpointRounding.AwayFromZero);
                result.ReducedBaseProduction += reducedBase;
            }

            return changed;
        }

        private static bool CanReduceBuildingForResource(
            EconomyBuildingRecord building,
            string resourceType,
            IReadOnlyDictionary<string, EconomyResourceRecord> resourcesByType)
        {
            if (building == null || !IsProductionBuilding(building) || building.BaseProduction <= 0m)
                return false;
            if (IsUnderConstruction(building))
                return false;
            if (string.Equals(building.BuildingStatus, BuildingStatus.Abandoned.ToString(), StringComparison.OrdinalIgnoreCase))
                return false;

            string? output = TrackedResourceResolver.ResolveTrackedResourceName(building.ResourceType, resourcesByType)
                ?? NormalizeTrackedResource(building.ResourceType);
            if (string.IsNullOrWhiteSpace(output))
                return false;

            return output.Equals(resourceType, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAutoBalancedLevelFiveBuilding(EconomyBuildingRecord building)
        {
            if (building == null || building.Level < 5)
                return false;

            string type = building.Type ?? string.Empty;
            return type.EndsWith(" L5", StringComparison.OrdinalIgnoreCase);
        }

        private static decimal ComputeEffectiveBuildingOutput(
            EconomyBuildingRecord building,
            float productionEfficiency,
            float stepPenalty,
            IReadOnlyDictionary<string, EconomyResourceRecord> resourcesByType)
        {
            if (building == null || building.BaseProduction <= 0m)
                return 0m;

            int degradation = Math.Max(0, building.DegradationLevel);
            decimal degradationPenalty = Math.Min(0.9m, degradation * (decimal)Math.Max(0f, stepPenalty));
            decimal productionMultiplier = Math.Max(0.1m, 1m - degradationPenalty);
            bool ignoreByResource = resourcesByType != null &&
                                    !string.IsNullOrWhiteSpace(building.ResourceType) &&
                                    resourcesByType.TryGetValue(building.ResourceType, out var resourceRecord) &&
                                    resourceRecord.IgnoreProductionEfficiency;
            decimal efficiencyFactor = (building.IgnoreProductionEfficiency || ignoreByResource)
                ? 1m
                : (decimal)productionEfficiency;
            return Math.Max(0m, building.BaseProduction * efficiencyFactor * productionMultiplier);
        }

        private static bool AddLevelFiveBuildingsForResource(
            CountryRecord country,
            string resourceType,
            decimal shortfall,
            List<EconomyBuildingRecord> workingBuildings,
            ref int remainingBuildings,
            ProductionBalanceResult result)
        {
            if (shortfall <= 0m || remainingBuildings <= 0)
                return false;

            if (!TemplateByResource.TryGetValue(resourceType, out var template))
                return false;

            int maxJobsPerBuilding = ComputeMaxJobsPerBuilding(country.Population);
            decimal outputPerJob = Math.Max(1m, template.ProductionPerJob * Level5ProductionMultiplier);
            int requiredJobs = (int)Math.Ceiling((double)(shortfall / outputPerJob));
            if (requiredJobs <= 0)
                return false;

            bool addedAny = false;
            while (requiredJobs > 0 && remainingBuildings > 0)
            {
                int jobs = Math.Min(requiredJobs, maxJobsPerBuilding);
                if (jobs <= 0)
                    break;
                requiredJobs -= jobs;

                var building = CreateLevelFiveBalancingBuilding(country.CountryId, resourceType, template, jobs);
                int addedId = EconomyBuildingsData.AddBuilding(building);
                if (addedId <= 0)
                    break;

                building.BuildingId = addedId;
                workingBuildings.Add(building);
                remainingBuildings--;
                addedAny = true;

                result.AddedBuildings++;
                result.AddedJobs += jobs;
                result.AddedBaseProduction += building.BaseProduction;
            }

            return addedAny;
        }

        private static EconomyBuildingRecord CreateLevelFiveBalancingBuilding(
            int countryId,
            string resourceType,
            ProductionTemplate template,
            int jobs)
        {
            int safeJobs = Math.Max(1, jobs);
            decimal production = decimal.Round(
                safeJobs * template.ProductionPerJob * Level5ProductionMultiplier,
                2,
                MidpointRounding.AwayFromZero);
            decimal upkeep = decimal.Round(
                Math.Max(1m, safeJobs * template.UpkeepPerJob * Level5UpkeepMultiplier),
                2,
                MidpointRounding.AwayFromZero);
            decimal buildCost = decimal.Round(
                ClampDecimal((production * 1.35m + safeJobs * 0.9m) * 1.8m, 1m, 9_000_000_000m),
                2,
                MidpointRounding.AwayFromZero);

            return new EconomyBuildingRecord
            {
                CountryId = countryId,
                Type = $"{template.BuildingType} L5",
                Category = "Production",
                Level = 5,
                JobsProvided = safeJobs,
                UpkeepCost = upkeep,
                BaseProduction = production,
                BuildCost = buildCost,
                BuildTurns = 0,
                ResourceType = resourceType,
                InputResourceType = string.Empty,
                InputAmount = 0m,
                StateOwned = template.StateOwned,
                StabilityBonus = template.StabilityBonus,
                CorruptionReduction = template.CorruptionReduction,
                ResearchBonus = 0f,
                PopulationGrowthBonus = 0f,
                FestivalBoost = 0f,
                ArmyBonus = 0,
                CompanyId = null,
                BuildingStatus = BuildingStatus.Active.ToString(),
                InactiveTurns = 0,
                DegradationLevel = 0
            };
        }

        private static int ComputeMaxJobsPerBuilding(long population)
        {
            if (population <= 75_000L)
                return 1200;
            if (population <= 300_000L)
                return 2200;
            if (population <= 1_000_000L)
                return 4200;
            if (population <= 5_000_000L)
                return 6500;
            return 9000;
        }

        private static Dictionary<string, EconomyResourceRecord> LoadResourceMap()
        {
            return EconomyResourcesData.GetAll()
                .Where(r => !string.IsNullOrWhiteSpace(r.ResourceType))
                .GroupBy(r => r.ResourceType ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToDictionary(r => r.ResourceType, r => r, StringComparer.OrdinalIgnoreCase);
        }

        private static Dictionary<string, decimal> EstimateDemandByResource(CountryRecord country)
        {
            var resources = LoadResourceMap();
            var globalModifiers = EconomyGlobalModifiersData.GetActive();
            var population = PopulationCalculator.GetPopulationSnapshot(country);
            long elderly = Math.Max(0L, population.Pensioners + population.SocialProtected);

            decimal inflationPriceMultiplier = EconomyService.GetInflationPriceMultiplier(country.Inflation);
            var priceMap = new Dictionary<ResourceType, decimal>();
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                if (resources.TryGetValue(type.ToString(), out var resource))
                    priceMap[type] = Math.Max(0.01m, resource.BasePrice * inflationPriceMultiplier);
                else
                    priceMap[type] = Math.Max(0.01m, inflationPriceMultiplier);
            }

            var input = new DemandInput
            {
                Population = new PopulationBreakdown
                {
                    Children = Math.Max(0L, population.Children),
                    WorkingAge = Math.Max(0L, population.WorkingAge),
                    Elderly = elderly,
                    GrowthRatePerTurn = country.PopulationGrowth / 100f
                },
                Season = GetCurrentSeason(),
                AverageHappiness = EconomyCalculator.Clamp(country.Happiness, 0f, 100f),
                AverageWealthIndex = ComputeWealthIndex(country),
                Corruption = EconomyCalculator.Clamp(country.Corruption, 0f, 100f),
                BlackMarketRisk = EconomyCalculator.Clamp(EconomyConfigData.GetFloat("BlackMarketRiskDefault", 0.3f), 0f, 1f),
                LuxuryEnabled = EconomyConfigData.GetInt("DemandLuxuryEnabled", 1) > 0,
                Modifiers = BuildDemandModifiers(globalModifiers, resources),
                ResourceBasePrices = priceMap
            };

            var request = new DemandCalculator().CalculateDemand(input, BuildDemandConfig());
            return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["Food"] = request.GetRequested(ResourceType.Food),
                ["Clothing"] = request.GetRequested(ResourceType.Clothing),
                ["Housing"] = request.GetRequested(ResourceType.Housing),
                ["Tools"] = request.GetRequested(ResourceType.Tools),
                ["Luxury"] = request.GetRequested(ResourceType.Luxury)
            };
        }

        private static List<IDemandModifier> BuildDemandModifiers(
            IReadOnlyCollection<EconomyGlobalModifierRecord> globalModifiers,
            IReadOnlyDictionary<string, EconomyResourceRecord> resources)
        {
            var modifiers = new List<IDemandModifier>();
            if (globalModifiers != null && globalModifiers.Count > 0)
            {
                foreach (var mod in globalModifiers)
                {
                    if (Math.Abs(mod.DemandModifier) < 0.0001f)
                        continue;

                    decimal percent = (decimal)mod.DemandModifier;
                    if (string.IsNullOrWhiteSpace(mod.DemandResourceType) ||
                        mod.DemandResourceType.Equals("All", StringComparison.OrdinalIgnoreCase))
                    {
                        modifiers.Add(new PercentAllDemandModifier(percent));
                    }
                    else if (Enum.TryParse<ResourceType>(mod.DemandResourceType, true, out var resourceType))
                    {
                        modifiers.Add(new PercentDemandModifier(resourceType, percent));
                    }
                }
            }

            if (resources != null && resources.Count > 0)
            {
                foreach (ResourceType tracked in Enum.GetValues(typeof(ResourceType)))
                {
                    if (!resources.TryGetValue(tracked.ToString(), out var resource))
                        continue;

                    float factorRaw = float.IsNaN(resource.DemandFactor) ? 1f : resource.DemandFactor;
                    float factor = EconomyCalculator.Clamp(factorRaw, 0f, 10f);
                    decimal percent = (decimal)factor - 1m;
                    if (Math.Abs(percent) < 0.0001m)
                        continue;

                    modifiers.Add(new PercentDemandModifier(tracked, percent));
                }
            }

            return modifiers;
        }

        private static DemandConfig BuildDemandConfig()
        {
            decimal food = EconomyConfigData.GetDecimal("DemandFoodPerPerson", 1m);
            decimal clothing = EconomyConfigData.GetDecimal("DemandClothingPerPerson", 0.3m);
            decimal housing = EconomyConfigData.GetDecimal("DemandHousingPerPerson", 0.1m);
            decimal tools = EconomyConfigData.GetDecimal("DemandToolsPerWorker", 0.2m);
            decimal luxury = EconomyConfigData.GetDecimal("DemandLuxuryPerPerson", 0.15m);

            return new DemandConfig
            {
                ClothingWinterMultiplier = EconomyConfigData.GetDecimal("DemandClothingWinterMultiplier", 1.25m),
                HousingPerNewPerson = EconomyConfigData.GetDecimal("DemandHousingPerNewPerson", 0.5m),
                LuxuryHappinessWeight = EconomyConfigData.GetFloat("DemandLuxuryHappinessWeight", 0.6f),
                LuxuryWealthWeight = EconomyConfigData.GetFloat("DemandLuxuryWealthWeight", 0.4f),
                BlackMarketMinFactor = EconomyConfigData.GetDecimal("BlackMarketMinFactor", 0.3m),
                BlackMarketMaxFactor = EconomyConfigData.GetDecimal("BlackMarketMaxFactor", 0.7m),
                BlackMarketPriceMultiplier = EconomyConfigData.GetDecimal("BlackMarketPriceMultiplier", 1.5m),
                BlackMarketBaseSuccess = EconomyConfigData.GetFloat("BlackMarketBaseSuccess", 0.2f),
                BlackMarketCorruptionWeight = EconomyConfigData.GetFloat("BlackMarketCorruptionWeight", 0.6f),
                BlackMarketRiskWeight = EconomyConfigData.GetFloat("BlackMarketRiskWeight", 0.3f),
                ChildrenRates = new DemandProfile
                {
                    Food = food * 0.8m,
                    Clothing = clothing * 0.7m,
                    Housing = housing * 0.6m,
                    Tools = tools * 0.2m,
                    Luxury = luxury * 0.4m
                },
                WorkingRates = new DemandProfile
                {
                    Food = food,
                    Clothing = clothing,
                    Housing = housing,
                    Tools = tools,
                    Luxury = luxury
                },
                ElderlyRates = new DemandProfile
                {
                    Food = food * 0.9m,
                    Clothing = clothing * 0.9m,
                    Housing = housing * 1.1m,
                    Tools = tools * 0.2m,
                    Luxury = luxury * 0.8m
                }
            };
        }

        private static Season GetCurrentSeason()
        {
            string raw = EconomyConfigData.GetText("CurrentSeason", "Summer");
            return Enum.TryParse<Season>(raw, true, out var season) ? season : Season.Summer;
        }

        private static float ComputeWealthIndex(CountryRecord country)
        {
            float fallback = EconomyConfigData.GetFloat("DemandWealthDefault", 0.4f);
            if (country.Population <= 0)
                return fallback;

            float scale = EconomyConfigData.GetFloat("DemandWealthPerCapitaScale", 100f);
            if (scale <= 0f)
                return fallback;

            float perCapita = (float)(country.Treasury / (double)Math.Max(1L, country.Population));
            return EconomyCalculator.Clamp(perCapita / scale, 0f, 1f);
        }

        private static Dictionary<string, decimal> EstimateAvailableByResource(
            CountryRecord country,
            IReadOnlyCollection<EconomyBuildingRecord> buildings)
        {
            var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            if (buildings == null || buildings.Count == 0)
                return result;
            var resources = LoadResourceMap();

            var population = PopulationCalculator.GetPopulationSnapshot(country);
            long availableWorkers = country.AvailableWorkers > 0
                ? country.AvailableWorkers
                : PopulationCalculator.ComputeCivilianWorkforce(population.WorkingAge, country.ArmySize, country.WarState);

            float warModifier = country.WarState ? EconomyConfigData.GetFloat("WarProductionModifier", 0.85f) : 1f;
            float sanctionsModifier = EconomyCalculator.ApplySanctionsModifier(
                (SanctionsLevel)country.SanctionsLevel,
                EconomyConfigData.GetFloat("SanctionsNoneModifier", 1f),
                EconomyConfigData.GetFloat("SanctionsPartialModifier", 0.9f),
                EconomyConfigData.GetFloat("SanctionsFullModifier", 0.7f));

            float productionEfficiency = EconomyCalculator.ProductionEfficiency(
                1f,
                country.Stability,
                country.Corruption,
                warModifier,
                sanctionsModifier,
                0f);

            float stepPenalty = EconomyConfigData.GetFloat("BuildingDegradeStepPenalty", 0.1f);
            long workersRemaining = Math.Max(0L, availableWorkers);

            var ordered = buildings
                .OrderBy(GetBuildingPriority)
                .ThenBy(b => Math.Max(0, b.JobsProvided))
                .ThenBy(b => b.BuildingId)
                .ToList();

            var plans = new List<BuildingResourceFlowPlan>();
            foreach (var building in ordered)
            {
                bool isUnderConstruction = IsUnderConstruction(building);
                bool isAbandoned = string.Equals(
                    building.BuildingStatus,
                    BuildingStatus.Abandoned.ToString(),
                    StringComparison.OrdinalIgnoreCase);
                int jobs = Math.Max(0, building.JobsProvided);
                bool hasWorkers = (!isUnderConstruction && !isAbandoned) && (jobs <= 0 || workersRemaining >= jobs);
                if (hasWorkers && jobs > 0)
                    workersRemaining -= jobs;

                bool isActive = hasWorkers && !isUnderConstruction && !isAbandoned;
                if (!isActive || !IsProductionBuilding(building) || building.BaseProduction <= 0m)
                    continue;
                if (string.IsNullOrWhiteSpace(building.ResourceType))
                    continue;

                decimal potential = ComputeEffectiveBuildingOutput(building, productionEfficiency, stepPenalty, resources);
                if (potential <= 0m)
                    continue;

                plans.Add(new BuildingResourceFlowPlan
                {
                    BuildingId = building.BuildingId,
                    OutputResourceType = building.ResourceType ?? string.Empty,
                    OutputAmount = potential,
                    InputResourceType = building.InputResourceType,
                    InputAmount = Math.Max(0m, building.InputAmount)
                });
            }

            var flow = BuildingResourceFlowCalculator.Calculate(plans);
            foreach (var pair in flow.AvailableByResource)
            {
                string? resource = TrackedResourceResolver.ResolveTrackedResourceName(pair.Key, resources)
                    ?? NormalizeTrackedResource(pair.Key);
                if (string.IsNullOrWhiteSpace(resource) || pair.Value <= 0m)
                    continue;

                if (result.TryGetValue(resource, out decimal current))
                    result[resource] = current + pair.Value;
                else
                    result[resource] = pair.Value;
            }

            return result;
        }

        private static bool IsProductionBuilding(EconomyBuildingRecord building)
        {
            return string.Equals(building.Category, "Production", StringComparison.OrdinalIgnoreCase)
                || string.Equals(building.Category, "Housing", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsUnderConstruction(EconomyBuildingRecord? building)
        {
            if (building == null)
                return false;

            return Math.Max(0, building.BuildTurns) > 0;
        }

        private static int GetBuildingPriority(EconomyBuildingRecord building)
        {
            string category = building.Category ?? string.Empty;
            string resource = building.ResourceType ?? string.Empty;
            if (category.Equals("Production", StringComparison.OrdinalIgnoreCase))
            {
                if (resource.Equals("Food", StringComparison.OrdinalIgnoreCase))
                    return 0;
                if (resource.Equals("Housing", StringComparison.OrdinalIgnoreCase))
                    return 1;
                if (resource.Equals("Clothing", StringComparison.OrdinalIgnoreCase))
                    return 2;
                if (resource.Equals("Tools", StringComparison.OrdinalIgnoreCase))
                    return 3;
                if (resource.Equals("Goods", StringComparison.OrdinalIgnoreCase))
                    return 4;
                if (resource.Equals("Luxury", StringComparison.OrdinalIgnoreCase))
                    return 5;
                return 6;
            }

            if (category.Equals("Housing", StringComparison.OrdinalIgnoreCase))
                return 7;
            if (category.Equals("Science", StringComparison.OrdinalIgnoreCase) || category.Equals("Culture", StringComparison.OrdinalIgnoreCase))
                return 8;
            if (category.Equals("Administration", StringComparison.OrdinalIgnoreCase) || category.Equals("Other", StringComparison.OrdinalIgnoreCase))
                return 9;
            if (category.Equals("Military", StringComparison.OrdinalIgnoreCase))
                return 10;
            return 11;
        }

        private static decimal ClampDecimal(decimal value, decimal min, decimal max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }
}
