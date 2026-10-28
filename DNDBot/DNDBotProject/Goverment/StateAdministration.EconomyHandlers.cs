#nullable enable
using DNDBot.BotSystem;
using Microsoft.Extensions.Logging;
using DNDBot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using System.Data;
using DNDBot.DataBase;
using DNDBot.Goverment.Research;
using DNDBot.Goverment.Economy;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DNDBot.Goverment
{
    public partial class StateAdministration : BotBodyRealisation
    {
        private static readonly NumberFormatInfo SpaceNumberFormat = new NumberFormatInfo
        {
            NumberGroupSeparator = " ",
            NumberDecimalSeparator = "."
        };
        private const string TimeAutomationConfigKey = "AutoGovernanceEnabled";
        private const string NetIncomeLimitEnabledConfigKey = "NetStabilizerEnabled";
        private const string NetIncomeLimitTargetConfigKey = "NetStabilizerTargetDailyNet";
        private const string NetIncomeLimitToleranceConfigKey = "NetStabilizerToleranceDailyNet";
        private const int TimeAutomationPageSize = 10;
        private const string BuildingTemplatePrefix = "state_economy_building_template_";
        private const string BuildingTemplateUsePrefix = "state_economy_building_template_use_";
        private const string BuildingTemplateLevelPreviewPrefix = "state_economy_building_template_preview|";
        private const string SectorRevenueMultiplierConfigPrefix = "SectorRevenueMultiplier:";
        private const string SectorTaxOverrideConfigPrefix = "SectorTaxOverride:";
        private const decimal SectorTaxOverrideAutoValue = -1m;
        private const decimal SectorRevenueMultiplierMin = 0.1m;
        private const decimal SectorRevenueMultiplierMax = 5m;
        private const decimal SectorTaxOverrideMax = 0.95m;

        private static readonly string[] SectorDisplayOrder =
        {
            "General",
            "Food",
            "Industry",
            "Services",
            "Logistics",
            "Weapons",
            "MilitaryIndustry",
            "SiegeWorks",
            "ArcaneArms"
        };

        private sealed class EconomySectorSummary
        {
            public string SectorKey { get; set; } = "General";
            public int CompaniesCount { get; set; }
            public long Jobs { get; set; }
            public decimal Payroll { get; set; }
            public decimal AverageSalary { get; set; }
            public decimal AverageTaxRate { get; set; }
            public decimal Budgets { get; set; }
        }

        private static string FmtNumber(decimal value, int decimals = 0)
        {
            return value.ToString($"N{decimals}", SpaceNumberFormat);
        }

        private static string FmtNumber(long value)
        {
            return value.ToString("N0", SpaceNumberFormat);
        }

        private static string FmtNumber(int value)
        {
            return value.ToString("N0", SpaceNumberFormat);
        }

        private static string FmtTreasury(long treasuryGold, int countryId)
        {
            return TreasuryCoinService.FormatTreasury(treasuryGold, countryId);
        }

        private static string FmtDeltaNumber(long value)
        {
            if (value > 0)
                return $"+{FmtNumber(value)}";
            if (value < 0)
                return $"-{FmtNumber(Math.Abs(value))}";
            return "0";
        }

        private static string FmtSignedDecimal(decimal value, int maxDecimals = 2)
        {
            if (value > 0m)
                return $"+{FmtNumberCompact(value, maxDecimals)}";
            if (value < 0m)
                return $"-{FmtNumberCompact(Math.Abs(value), maxDecimals)}";
            return "0";
        }

        private static string FmtSignedFloat(float value, int maxDecimals = 2)
        {
            if (value > 0f)
                return $"+{FmtNumberCompact(value, maxDecimals)}";
            if (value < 0f)
                return $"-{FmtNumberCompact(Math.Abs(value), maxDecimals)}";
            return "0";
        }

        private static string FmtNumberCompact(decimal value, int maxDecimals = 2)
        {
            if (maxDecimals <= 0)
                return value.ToString("N0", SpaceNumberFormat);
            string format = "#,0." + new string('#', maxDecimals);
            return value.ToString(format, SpaceNumberFormat);
        }

        private static string FmtNumberCompact(float value, int maxDecimals = 2)
        {
            return FmtNumberCompact((decimal)value, maxDecimals);
        }

        private static string FmtNumberCompact(int value)
        {
            return FmtNumber(value);
        }

        private static string FmtNumberCompact(long value)
        {
            return FmtNumber(value);
        }

        private static string FmtNumberCompact(double value, int maxDecimals = 2)
        {
            return FmtNumberCompact((decimal)value, maxDecimals);
        }

        private static bool IsTimeAutomationEnabled(int countryId)
        {
            if (countryId <= 0)
                return false;
            return EconomyConfigData.GetDecimalForCountry(countryId, TimeAutomationConfigKey, 0m) >= 0.5m;
        }

        private static void SetTimeAutomationEnabled(int countryId, bool enabled)
        {
            if (countryId <= 0)
                return;
            EconomyConfigData.UpsertCountryDecimal(countryId, TimeAutomationConfigKey, enabled ? 1m : 0m);
        }

        private static bool IsNetIncomeLimitEnabled(int countryId)
        {
            if (countryId <= 0)
                return false;
            return EconomyConfigData.GetDecimalForCountry(countryId, NetIncomeLimitEnabledConfigKey, 1m) >= 0.5m;
        }

        private static decimal GetNetIncomeLimitTargetDaily(int countryId)
        {
            if (countryId <= 0)
                return 5000m;
            return Math.Max(0m, EconomyConfigData.GetDecimalForCountry(countryId, NetIncomeLimitTargetConfigKey, 5000m));
        }

        private static decimal GetNetIncomeLimitToleranceDaily(int countryId)
        {
            if (countryId <= 0)
                return 500m;
            return Math.Max(0m, EconomyConfigData.GetDecimalForCountry(countryId, NetIncomeLimitToleranceConfigKey, 500m));
        }

        private static string FormatCountryName(CountryRecord country)
        {
            if (country == null)
                return "Невідома країна";
            if (!string.IsNullOrWhiteSpace(country.Name))
                return country.Name;
            return $"#{country.CountryId}";
        }

        private static string FormatResourceTypeUk(string? resourceType)
        {
            if (string.IsNullOrWhiteSpace(resourceType))
                return "-";

            if (MilitaryArsenalCatalog.TryFormatResourceTypeUk(resourceType, out string militaryName))
                return militaryName;

            return NormalizeResourceKey(resourceType) switch
            {
                "food" => "Їжа",
                "clothing" => "Одяг",
                "housing" => "Житло",
                "tools" => "Інструменти",
                "luxury" => "Розкіш",
                "goods" => "Товари",
                "materials" => "Матеріали",
                "material" => "Матеріали",
                _ => resourceType
            };
        }

        private static string FormatBuildingCategoryUk(string? category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return "-";

            return category.Trim().ToLowerInvariant() switch
            {
                "production" => "Виробництво",
                "housing" => "Житло",
                "administration" => "Адміністрація",
                "science" => "Наука",
                "military" => "Військова",
                "culture" => "Культура",
                "other" => "Інше",
                _ => category
            };
        }

        private static string FormatBuildingStatusUk(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return "-";

            return status.Trim().ToLowerInvariant() switch
            {
                "active" => "Активна",
                "inactive" => "Неактивна",
                "underconstruction" => "Будується",
                "degraded" => "Зношена",
                "abandoned" => "Покинута",
                _ => status
            };
        }

        private static bool IsBuildingUnderConstruction(EconomyBuildingRecord? building)
        {
            if (building == null)
                return false;

            return Math.Max(0, building.BuildTurns) > 0;
        }

        private static bool AreBuildingEffectsActive(EconomyBuildingRecord? building)
        {
            if (building == null || IsBuildingUnderConstruction(building))
                return false;

            return string.Equals(building.BuildingStatus, BuildingStatus.Active.ToString(), StringComparison.OrdinalIgnoreCase)
                || string.Equals(building.BuildingStatus, BuildingStatus.Degraded.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        private static decimal GetDemandByResource(EconomyTurnHistoryData.EconomyTurnRecord? turn, string resourceType)
        {
            if (turn == null)
                return 0m;

            return NormalizeResourceKey(resourceType) switch
            {
                "food" => turn.FoodDemand,
                "clothing" => turn.ClothingDemand,
                "housing" => turn.HousingDemand,
                "tools" => turn.ToolsDemand,
                "luxury" => turn.LuxuryDemand,
                _ => 0m
            };
        }

        private static decimal SumTrackedResourceAmount(
            IReadOnlyDictionary<string, decimal>? source,
            string trackedResourceType,
            IReadOnlyDictionary<string, EconomyResourceRecord> resources)
        {
            if (source == null || source.Count == 0 || string.IsNullOrWhiteSpace(trackedResourceType))
                return 0m;

            decimal total = 0m;
            foreach (var pair in source)
            {
                if (pair.Value <= 0m)
                    continue;

                string? mapped = TrackedResourceResolver.ResolveTrackedResourceName(pair.Key, resources);
                if (mapped == null)
                    continue;

                if (string.Equals(mapped, trackedResourceType, StringComparison.OrdinalIgnoreCase))
                    total += pair.Value;
            }

            return total;
        }

        private static Dictionary<string, decimal> BuildEstimatedDemandByResource(
            StateDataClass state,
            IReadOnlyCollection<EconomyGlobalModifierRecord> globalModifiers,
            Dictionary<string, EconomyResourceRecord> resources)
        {
            var input = BuildDemandInputForReport(state, globalModifiers, resources);
            var config = BuildDemandConfigForReport();
            var request = new DemandCalculator().CalculateDemand(input, config);

            return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["Food"] = request.GetRequested(ResourceType.Food),
                ["Clothing"] = request.GetRequested(ResourceType.Clothing),
                ["Housing"] = request.GetRequested(ResourceType.Housing),
                ["Tools"] = request.GetRequested(ResourceType.Tools),
                ["Luxury"] = request.GetRequested(ResourceType.Luxury)
            };
        }

        private static DemandInput BuildDemandInputForReport(
            StateDataClass state,
            IReadOnlyCollection<EconomyGlobalModifierRecord> globalModifiers,
            Dictionary<string, EconomyResourceRecord> resources)
        {
            long elderly = Math.Max(0L, state.PensionersPopulation + state.SocialProtectedPopulation);
            float growthRate = state.PopulationGrowthFinal / 100f;
            bool luxuryEnabled = EconomyConfigData.GetInt("DemandLuxuryEnabled", 1) > 0;
            float blackMarketRisk = EconomyConfigData.GetFloat("BlackMarketRiskDefault", 0.3f);
            decimal inflationPriceMultiplier = EconomyService.GetInflationPriceMultiplier(state.Inflation);

            var priceMap = new Dictionary<ResourceType, decimal>();
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                if (resources.TryGetValue(type.ToString(), out var resource))
                    priceMap[type] = resource.BasePrice * inflationPriceMultiplier;
            }

            return new DemandInput
            {
                Population = new PopulationBreakdown
                {
                    Children = Math.Max(0L, state.ChildrenPopulation),
                    WorkingAge = Math.Max(0L, state.WorkingAgePopulation),
                    Elderly = elderly,
                    GrowthRatePerTurn = growthRate
                },
                Season = GetDemandSeasonForReport(),
                AverageHappiness = EconomyCalculator.Clamp(state.Happiness, 0f, 100f),
                AverageWealthIndex = ComputeDemandWealthIndexForReport(state),
                Corruption = EconomyCalculator.Clamp(state.Corruption, 0f, 100f),
                BlackMarketRisk = EconomyCalculator.Clamp(blackMarketRisk, 0f, 1f),
                LuxuryEnabled = luxuryEnabled,
                Modifiers = BuildDemandModifiersForReport(globalModifiers, resources),
                ResourceBasePrices = priceMap
            };
        }

        private static DemandConfig BuildDemandConfigForReport()
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

        private static List<IDemandModifier> BuildDemandModifiersForReport(
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
                    if (string.IsNullOrWhiteSpace(mod.DemandResourceType) || mod.DemandResourceType.Equals("All", StringComparison.OrdinalIgnoreCase))
                    {
                        modifiers.Add(new PercentAllDemandModifier(percent));
                    }
                    else if (Enum.TryParse<ResourceType>(mod.DemandResourceType, true, out var type))
                    {
                        modifiers.Add(new PercentDemandModifier(type, percent));
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

        private static Season GetDemandSeasonForReport()
        {
            string raw = EconomyConfigData.GetText("CurrentSeason", "Summer");
            return Enum.TryParse<Season>(raw, true, out var season) ? season : Season.Summer;
        }

        private static float ComputeDemandWealthIndexForReport(StateDataClass state)
        {
            float fallback = EconomyConfigData.GetFloat("DemandWealthDefault", 0.4f);
            if (state.Population <= 0)
                return fallback;

            float scale = EconomyConfigData.GetFloat("DemandWealthPerCapitaScale", 100f);
            if (scale <= 0f)
                return fallback;

            float perCapita = (float)(state.Treasury / (double)state.Population);
            return EconomyCalculator.Clamp(perCapita / scale, 0f, 1f);
        }

        private static int GetReportBuildingPriority(EconomyBuildingRecord building)
        {
            if (building == null)
                return 100;

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

        private sealed class ResourceProductionNowResult
        {
            public Dictionary<string, decimal> NetByResource { get; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, decimal> ProducedByResource { get; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, decimal> ConsumedByResource { get; } = new(StringComparer.OrdinalIgnoreCase);
            public float EfficiencyPct { get; set; }
        }

        private static ResourceProductionNowResult ComputeResourceProductionNow(StateDataClass state, List<EconomyBuildingRecord> buildings)
        {
            var result = new ResourceProductionNowResult();
            if (state == null || buildings.Count == 0)
                return result;

            long availableWorkers = state.AvailableWorkers > 0
                ? state.AvailableWorkers
                : Math.Max(0L, state.WorkingAgePopulation - state.ArmySize);

            float warModifier = state.WarState ? EconomyConfigData.GetFloat("WarProductionModifier", 0.85f) : 1f;
            float sanctionsModifier = EconomyCalculator.ApplySanctionsModifier(
                (SanctionsLevel)state.SanctionsLevel,
                EconomyConfigData.GetFloat("SanctionsNoneModifier", 1f),
                EconomyConfigData.GetFloat("SanctionsPartialModifier", 0.9f),
                EconomyConfigData.GetFloat("SanctionsFullModifier", 0.7f)
            );

            float productionEfficiency = EconomyCalculator.ProductionEfficiency(
                1f,
                state.Stability,
                state.Corruption,
                warModifier,
                sanctionsModifier,
                0f
            );

            float stepPenalty = EconomyConfigData.GetFloat("BuildingDegradeStepPenalty", 0.1f);
            long workersRemaining = availableWorkers;
            var resourcesByType = EconomyResourcesData.GetAll()
                .Where(r => !string.IsNullOrWhiteSpace(r.ResourceType))
                .GroupBy(r => r.ResourceType, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToDictionary(r => r.ResourceType, r => r, StringComparer.OrdinalIgnoreCase);

            var ordered = buildings
                .OrderBy(GetReportBuildingPriority)
                .ThenBy(b => Math.Max(0, b.JobsProvided))
                .ThenBy(b => b.BuildingId)
                .ToList();

            var plans = new List<BuildingResourceFlowPlan>();
            foreach (var b in ordered)
            {
                string status = b.BuildingStatus ?? string.Empty;
                bool isUnderConstruction = IsBuildingUnderConstruction(b);
                bool isAbandoned = string.Equals(status, BuildingStatus.Abandoned.ToString(), StringComparison.OrdinalIgnoreCase);
                int jobs = Math.Max(0, b.JobsProvided);
                bool hasWorkers = (!isUnderConstruction && !isAbandoned) && (jobs <= 0 || workersRemaining >= jobs);
                if (hasWorkers && jobs > 0)
                    workersRemaining -= jobs;

                bool isActive = hasWorkers && !isUnderConstruction && !isAbandoned;
                if (!isActive)
                    continue;
                if (!HasResourceOutputBuilding(b))
                    continue;

                int degradation = Math.Max(0, b.DegradationLevel);
                decimal degradationPenalty = Math.Min(0.9m, degradation * (decimal)Math.Max(0f, stepPenalty));
                decimal productionMultiplier = Math.Max(0.1m, 1m - degradationPenalty);
                bool ignoreByResource = !string.IsNullOrWhiteSpace(b.ResourceType) &&
                                        resourcesByType.TryGetValue(b.ResourceType, out var resourceRecord) &&
                                        resourceRecord.IgnoreProductionEfficiency;
                decimal efficiencyFactor = (b.IgnoreProductionEfficiency || ignoreByResource)
                    ? 1m
                    : (decimal)productionEfficiency;
                decimal potential = b.BaseProduction * efficiencyFactor * productionMultiplier;
                if (potential <= 0m)
                    continue;

                plans.Add(new BuildingResourceFlowPlan
                {
                    BuildingId = b.BuildingId,
                    OutputResourceType = b.ResourceType!,
                    OutputAmount = potential,
                    InputResourceType = b.InputResourceType,
                    InputAmount = Math.Max(0m, b.InputAmount)
                });
            }

            var flow = BuildingResourceFlowCalculator.Calculate(plans);
            foreach (var pair in flow.AvailableByResource)
            {
                if (pair.Value > 0m)
                    result.NetByResource[pair.Key] = pair.Value;
            }
            foreach (var pair in flow.ProducedByResource)
            {
                if (pair.Value > 0m)
                    result.ProducedByResource[pair.Key] = pair.Value;
            }
            foreach (var pair in flow.ConsumedByResource)
            {
                if (pair.Value > 0m)
                    result.ConsumedByResource[pair.Key] = pair.Value;
            }

            result.EfficiencyPct = productionEfficiency * 100f;
            return result;
        }

        private async Task ShowEconomyMenu(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                "💰 Економіка",
                StateKeyboardBuilder.EconomyMenuKeyboard(userId == masterID),
                cancellationToken
            );
        }

        private async Task ShowEconomyMasterBalancingMenu(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken)
        {
            if (userId != masterID)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Нема доступу.",
                    StateKeyboardBuilder.EconomyMenuKeyboard(false),
                    cancellationToken
                );
                return;
            }

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                "🧠 Майстер-група: балансування та провідний ресурс",
                StateKeyboardBuilder.EconomyMasterBalancingKeyboard(),
                cancellationToken
            );
        }

        private async Task ShowEconomyIncomeLimitMenu(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken,
            string? statusMessage = null)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            if (userId != masterID && !IsSelectedCountryOwner(userId, selectedCountryId))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Нема доступу.",
                    StateKeyboardBuilder.EconomyMenuKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            if (country == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Країну не знайдено.",
                    StateKeyboardBuilder.EconomyMenuKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }

            bool enabled = IsNetIncomeLimitEnabled(country.CountryId);
            decimal targetDaily = GetNetIncomeLimitTargetDaily(country.CountryId);
            decimal toleranceDaily = GetNetIncomeLimitToleranceDaily(country.CountryId);
            var preview = new EconomyService().PreviewTurn(country.CountryId, 1);

            string countryName = string.IsNullOrWhiteSpace(country.Name) ? $"#{country.CountryId}" : country.Name;
            var sb = new StringBuilder();
            sb.AppendLine($"🎯 Ліміт чистого доходу: {countryName}");
            sb.AppendLine($"Стан: {(enabled ? "увімкнено" : "вимкнено")}");
            sb.AppendLine($"Ціль (за день): {FmtNumber(targetDaily)}");
            sb.AppendLine($"Допуск: ±{FmtNumber(toleranceDaily)}");
            sb.AppendLine($"Факт (прев'ю 1 день): {FmtDeltaNumber(preview.NetResult)}");
            sb.AppendLine();
            sb.AppendLine("Коли ліміт увімкнено, надлишок чистого доходу автоматично йде у стабілізаційне фінансування.");
            sb.AppendLine("Оберіть пресет, введіть свій ліміт або вимкніть ліміт.");

            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                sb.AppendLine();
                sb.AppendLine(statusMessage);
            }

            EconomyIncomeLimitUiContext[userId] = (chatId, msgId);

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.EconomyIncomeLimitKeyboard(enabled, targetDaily),
                cancellationToken
            );
        }

        private async Task StartEconomyIncomeLimitInput(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            if (userId != masterID && !IsSelectedCountryOwner(userId, selectedCountryId))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Нема доступу.",
                    StateKeyboardBuilder.EconomyMenuKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }

            EconomyIncomeLimitUiContext[userId] = (chatId, msgId);
            EconomyInputState[userId] = EconomyInputMode.IncomeLimit;
            EconomyInputStep[userId] = 0;

            await SendHelperMessageAsync(
                botClient,
                userId,
                "Вкажіть ціль чистого доходу за день (число >= 0).\nАбо введіть `off` / `вимкнути`, щоб вимкнути ліміт.",
                cancellationToken);
        }

        private async Task SetEconomyIncomeLimitPreset(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            decimal targetDaily,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            if (userId != masterID && !IsSelectedCountryOwner(userId, selectedCountryId))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Нема доступу.",
                    StateKeyboardBuilder.EconomyMenuKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }

            if (EconomyInputState.TryGetValue(userId, out var mode) && mode == EconomyInputMode.IncomeLimit)
            {
                await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
                ClearEconomyInput(userId);
            }

            decimal normalized = decimal.Round(Math.Max(0m, targetDaily), 2, MidpointRounding.AwayFromZero);
            bool savedTarget = EconomyConfigData.UpsertCountryDecimal(selectedCountryId.Value, NetIncomeLimitTargetConfigKey, normalized);
            bool savedEnabled = EconomyConfigData.UpsertCountryDecimal(selectedCountryId.Value, NetIncomeLimitEnabledConfigKey, 1m);

            string status = savedTarget && savedEnabled
                ? $"✅ Ліміт увімкнено. Ціль: {FmtNumber(normalized)} / день."
                : "❌ Не вдалося зберегти ліміт доходу.";

            await ShowEconomyIncomeLimitMenu(botClient, userId, chatId, msgId, cancellationToken, status);
        }

        private async Task DisableEconomyIncomeLimit(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            if (userId != masterID && !IsSelectedCountryOwner(userId, selectedCountryId))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Нема доступу.",
                    StateKeyboardBuilder.EconomyMenuKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }

            if (EconomyInputState.TryGetValue(userId, out var mode) && mode == EconomyInputMode.IncomeLimit)
            {
                await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
                ClearEconomyInput(userId);
            }

            bool saved = EconomyConfigData.UpsertCountryDecimal(selectedCountryId.Value, NetIncomeLimitEnabledConfigKey, 0m);
            string status = saved
                ? "✅ Ліміт доходу вимкнено."
                : "❌ Не вдалося вимкнути ліміт доходу.";

            await ShowEconomyIncomeLimitMenu(botClient, userId, chatId, msgId, cancellationToken, status);
        }

        private async Task ShowEconomyProductionBalanceConfirm(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            if (userId != masterID)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Нема доступу.",
                    StateKeyboardBuilder.EconomyMenuKeyboard(false),
                    cancellationToken
                );
                return;
            }

            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            if (country == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Країну не знайдено.",
                    StateKeyboardBuilder.EconomyMenuKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }

            var preview = ProductionBalancingService.AnalyzeCountry(
                selectedCountryId.Value,
                BuildStandardProductionBalancePreviewOptions());

            string leaderText = string.IsNullOrWhiteSpace(preview.LeaderResource)
                ? "не обрано"
                : FormatResourceTypeUk(preview.LeaderResource);

            string countryName = string.IsNullOrWhiteSpace(country.Name) ? $"#{country.CountryId}" : country.Name;
            var sb = new StringBuilder();
            sb.AppendLine($"⚖ Балансування виробництва: {countryName}");
            if (preview.Success)
            {
                sb.AppendLine($"Поточний стан: профіцит {FmtNumber(preview.SurplusBefore)}/5, дефіцит {FmtNumber(preview.DeficitBefore)}/5");
                sb.AppendLine($"Провідний ресурс: {leaderText}");
            }
            sb.AppendLine();
            sb.AppendLine("Підтвердити балансування лише для цієї країни?");

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.EconomyProductionBalanceConfirmKeyboard(),
                cancellationToken
            );
        }

        private static ProductionBalanceOptions BuildStandardProductionBalancePreviewOptions()
        {
            return new ProductionBalanceOptions
            {
                StrictEconomyMode = false,
                SurplusTargetCount = 1,
                SurplusCoverageRatio = 1.00m,
                NearBalanceCoverageRatio = 0.88m,
                LeaderCoverageRatio = 0.96m,
                MaxIterations = 4,
                MaxBuildingsToAdd = 120
            };
        }

        private static ProductionBalanceOptions BuildStandardProductionBalanceApplyOptions()
        {
            return new ProductionBalanceOptions
            {
                StrictEconomyMode = false,
                SurplusTargetCount = 1,
                SurplusCoverageRatio = 1.00m,
                NearBalanceCoverageRatio = 0.88m,
                LeaderCoverageRatio = 0.96m,
                EnableOversupplyCorrection = true,
                OversupplyCeilingRatio = 0.92m,
                LeaderOversupplyCeilingRatio = 1.04m,
                MaxBuildingsToDisable = 300,
                MaxIterations = 8,
                MaxBuildingsToAdd = 120
            };
        }

        private static ProductionBalanceOptions BuildLuxuryProductionBalancePreviewOptions()
        {
            return new ProductionBalanceOptions
            {
                StrictEconomyMode = false,
                SurplusTargetCount = 5,
                SurplusCoverageRatio = 1.05m,
                NearBalanceCoverageRatio = 1.01m,
                LeaderCoverageRatio = 1.20m,
                CollapseSurplusTargetsWhenAllSurplus = false,
                MaxIterations = 8,
                MaxBuildingsToAdd = 180
            };
        }

        private static ProductionBalanceOptions BuildLuxuryProductionBalanceApplyOptions()
        {
            return new ProductionBalanceOptions
            {
                StrictEconomyMode = false,
                SurplusTargetCount = 5,
                SurplusCoverageRatio = 1.05m,
                NearBalanceCoverageRatio = 1.01m,
                LeaderCoverageRatio = 1.20m,
                CollapseSurplusTargetsWhenAllSurplus = false,
                EnableOversupplyCorrection = true,
                OversupplyCeilingRatio = 1.14m,
                LeaderOversupplyCeilingRatio = 1.32m,
                MaxBuildingsToDisable = 220,
                MaxIterations = 10,
                MaxBuildingsToAdd = 180
            };
        }

        private async Task ApplyEconomyProductionBalance(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            bool confirmed,
            CancellationToken cancellationToken)
        {
            if (userId != masterID)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Нема доступу.",
                    StateKeyboardBuilder.EconomyMenuKeyboard(false),
                    cancellationToken
                );
                return;
            }

            if (!confirmed)
            {
                await ShowEconomyMenu(botClient, userId, chatId, msgId, cancellationToken);
                return;
            }

            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            int countryId = selectedCountryId.Value;
            var country = CountriesData.GetCountryById(countryId);
            if (country == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Країну не знайдено.",
                    StateKeyboardBuilder.EconomyMenuKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }

            var result = ProductionBalancingService.BalanceCountryProduction(
                countryId,
                BuildStandardProductionBalanceApplyOptions());

            if (!result.Success)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "❌ Не вдалося виконати балансування виробництва.",
                    StateKeyboardBuilder.EconomyMenuKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }

            string countryName = string.IsNullOrWhiteSpace(country.Name) ? $"#{country.CountryId}" : country.Name;
            string leaderText = string.IsNullOrWhiteSpace(result.LeaderResource)
                ? "не обрано"
                : FormatResourceTypeUk(result.LeaderResource);

            var sb = new StringBuilder();
            sb.AppendLine($"✅ Балансування завершено: {countryName}");
            sb.AppendLine($"Провідний ресурс: {leaderText}");
            sb.AppendLine($"Було: профіцит {FmtNumber(result.SurplusBefore)}/5, дефіцит {FmtNumber(result.DeficitBefore)}/5");
            sb.AppendLine($"Стало: профіцит {FmtNumber(result.SurplusAfter)}/5, дефіцит {FmtNumber(result.DeficitAfter)}/5");
            sb.AppendLine($"Додано L5 будівель: {FmtNumber(result.AddedBuildings)}");
            if (result.AddedBuildings > 0)
            {
                sb.AppendLine($"Додано робочих місць: +{FmtNumber(result.AddedJobs)}");
                sb.AppendLine($"Додано базового виробництва: +{FmtNumberCompact(result.AddedBaseProduction)}");
            }
            if (result.DisabledBuildings > 0)
            {
                sb.AppendLine($"Знижено перепродукцію будівлями: {FmtNumber(result.DisabledBuildings)}");
                sb.AppendLine($"Зменшено робочих місць (екв.): -{FmtNumber(result.DisabledJobs)}");
                sb.AppendLine($"Зменшено базового виробництва: -{FmtNumberCompact(result.ReducedBaseProduction)}");
            }

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.EconomyMenuKeyboard(userId == masterID),
                cancellationToken
            );
        }

        private async Task ShowEconomyLuxuryProductionBalanceConfirm(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            if (userId != masterID)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Нема доступу.",
                    StateKeyboardBuilder.EconomyMenuKeyboard(false),
                    cancellationToken
                );
                return;
            }

            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            if (country == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Країну не знайдено.",
                    StateKeyboardBuilder.EconomyMenuKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }

            var preview = ProductionBalancingService.AnalyzeCountry(
                selectedCountryId.Value,
                BuildLuxuryProductionBalancePreviewOptions());

            string leaderText = string.IsNullOrWhiteSpace(preview.LeaderResource)
                ? "не обрано"
                : FormatResourceTypeUk(preview.LeaderResource);

            string countryName = string.IsNullOrWhiteSpace(country.Name) ? $"#{country.CountryId}" : country.Name;
            var sb = new StringBuilder();
            sb.AppendLine($"💎 Розкішне балансування: {countryName}");
            if (preview.Success)
            {
                sb.AppendLine($"Поточний стан: профіцит {FmtNumber(preview.SurplusBefore)}/5, дефіцит {FmtNumber(preview.DeficitBefore)}/5");
                sb.AppendLine($"Провідний ресурс: {leaderText}");
            }
            sb.AppendLine();
            sb.AppendLine("Режим намагається утримувати невеликий профіцит по всіх 5 ресурсах.");
            sb.AppendLine("Провідний ресурс отримує підвищений профіцит.");
            sb.AppendLine();
            sb.AppendLine("Підтвердити розкішне балансування лише для цієї країни?");

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.EconomyLuxuryProductionBalanceConfirmKeyboard(),
                cancellationToken
            );
        }

        private async Task ApplyEconomyLuxuryProductionBalance(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            bool confirmed,
            CancellationToken cancellationToken)
        {
            if (userId != masterID)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Нема доступу.",
                    StateKeyboardBuilder.EconomyMenuKeyboard(false),
                    cancellationToken
                );
                return;
            }

            if (!confirmed)
            {
                await ShowEconomyMenu(botClient, userId, chatId, msgId, cancellationToken);
                return;
            }

            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            int countryId = selectedCountryId.Value;
            var country = CountriesData.GetCountryById(countryId);
            if (country == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Країну не знайдено.",
                    StateKeyboardBuilder.EconomyMenuKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }

            var result = ProductionBalancingService.BalanceCountryProduction(
                countryId,
                BuildLuxuryProductionBalanceApplyOptions());

            if (!result.Success)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "❌ Не вдалося виконати розкішне балансування.",
                    StateKeyboardBuilder.EconomyMenuKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }

            string countryName = string.IsNullOrWhiteSpace(country.Name) ? $"#{country.CountryId}" : country.Name;
            string leaderText = string.IsNullOrWhiteSpace(result.LeaderResource)
                ? "не обрано"
                : FormatResourceTypeUk(result.LeaderResource);

            var sb = new StringBuilder();
            sb.AppendLine($"✅ Розкішне балансування завершено: {countryName}");
            sb.AppendLine($"Провідний ресурс: {leaderText}");
            sb.AppendLine("Ціль: невеликий профіцит по всіх ресурсах + підвищений профіцит провідного.");
            sb.AppendLine($"Було: профіцит {FmtNumber(result.SurplusBefore)}/5, дефіцит {FmtNumber(result.DeficitBefore)}/5");
            sb.AppendLine($"Стало: профіцит {FmtNumber(result.SurplusAfter)}/5, дефіцит {FmtNumber(result.DeficitAfter)}/5");
            sb.AppendLine($"Додано L5 будівель: {FmtNumber(result.AddedBuildings)}");
            if (result.AddedBuildings > 0)
            {
                sb.AppendLine($"Додано робочих місць: +{FmtNumber(result.AddedJobs)}");
                sb.AppendLine($"Додано базового виробництва: +{FmtNumberCompact(result.AddedBaseProduction)}");
            }
            if (result.DisabledBuildings > 0)
            {
                sb.AppendLine($"Знижено перепродукцію будівлями: {FmtNumber(result.DisabledBuildings)}");
                sb.AppendLine($"Зменшено робочих місць (екв.): -{FmtNumber(result.DisabledJobs)}");
                sb.AppendLine($"Зменшено базового виробництва: -{FmtNumberCompact(result.ReducedBaseProduction)}");
            }

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.EconomyMenuKeyboard(userId == masterID),
                cancellationToken
            );
        }

        private async Task ShowEconomyLeaderResourceMenu(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken,
            string? statusMessage = null)
        {
            if (userId != masterID)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Нема доступу.",
                    StateKeyboardBuilder.EconomyMenuKeyboard(false),
                    cancellationToken
                );
                return;
            }

            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            if (country == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Країну не знайдено.",
                    StateKeyboardBuilder.EconomyMenuKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }

            string? currentLeader = ProductionBalancingService.GetLeaderResource(selectedCountryId.Value);
            string leaderText = string.IsNullOrWhiteSpace(currentLeader)
                ? "не обрано"
                : FormatResourceTypeUk(currentLeader);
            string countryName = string.IsNullOrWhiteSpace(country.Name) ? $"#{country.CountryId}" : country.Name;

            var sb = new StringBuilder();
            sb.AppendLine($"🏁 Провідний ресурс: {countryName}");
            sb.AppendLine($"Поточний вибір: {leaderText}");
            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                sb.AppendLine();
                sb.AppendLine(statusMessage);
            }
            sb.AppendLine();
            sb.AppendLine("Оберіть ресурс. Після цього запустіть \"Балансування виробництва\" для застосування.");

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.EconomyLeaderResourceKeyboard(currentLeader),
                cancellationToken
            );
        }

        private async Task SetEconomyLeaderResource(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            string token,
            CancellationToken cancellationToken)
        {
            if (userId != masterID)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Нема доступу.",
                    StateKeyboardBuilder.EconomyMenuKeyboard(false),
                    cancellationToken
                );
                return;
            }

            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            string trimmed = (token ?? string.Empty).Trim();
            string? normalized = string.Equals(trimmed, "none", StringComparison.OrdinalIgnoreCase)
                ? null
                : ProductionBalancingService.NormalizeTrackedResource(trimmed);

            if (!string.Equals(trimmed, "none", StringComparison.OrdinalIgnoreCase) && normalized == null)
            {
                await ShowEconomyLeaderResourceMenu(
                    botClient,
                    userId,
                    chatId,
                    msgId,
                    cancellationToken,
                    "❌ Невідомий ресурс.");
                return;
            }

            bool saved = ProductionBalancingService.SetLeaderResource(selectedCountryId.Value, normalized);
            if (!saved)
            {
                await ShowEconomyLeaderResourceMenu(
                    botClient,
                    userId,
                    chatId,
                    msgId,
                    cancellationToken,
                    "❌ Не вдалося зберегти вибір.");
                return;
            }

            string status = normalized == null
                ? "✅ Провідний ресурс скинуто."
                : $"✅ Провідний ресурс: {FormatResourceTypeUk(normalized)}.";

            await ShowEconomyLeaderResourceMenu(
                botClient,
                userId,
                chatId,
                msgId,
                cancellationToken,
                status);
        }

        private async Task ShowCountryEffectsMenu(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            if (userId != masterID)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Нема доступу.",
                    StateKeyboardBuilder.MainStateKeyboard(userId),
                    cancellationToken
                );
                return;
            }

            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            if (country == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Країну не знайдено.",
                    StateKeyboardBuilder.MainStateKeyboard(userId),
                    cancellationToken
                );
                return;
            }

            string name = string.IsNullOrWhiteSpace(country.Name) ? $"#{country.CountryId}" : country.Name;
            string crisis = country.IsInCrisis ? "Так" : "Ні";
            string collapse = country.IsCollapsed ? "Так" : "Ні";
            string golden = country.IsInGoldenAge ? "Так" : "Ні";

            var effects = EconomyService.GetCountryEffectsPreview(country);
            string text = $"🌪 Ефекти країни: {name}\n" +
                          $"Криза: {crisis}\n" +
                          $"Колапс: {collapse}\n" +
                          $"Золота доба: {golden}\n\n" +
                          "Поточні модифікатори:\n" +
                          $"• Виробництво: {effects.ProductionModifier:+0.##%;-0.##%;0%}\n" +
                          $"• Доходи: {effects.IncomeModifier:+0.##%;-0.##%;0%}\n" +
                          $"• Витрати: {effects.ExpenseModifier:+0.##%;-0.##%;0%}\n" +
                          $"• Безробіття: {effects.UnemploymentDelta:+0.##%;-0.##%;0%}\n" +
                          $"• Смертність: {effects.MortalityRateDelta:+0.##%;-0.##%;0%}\n" +
                          $"• Стабільність: {effects.StabilityBonus:+0.##;-0.##;0}\n" +
                          $"• Ріст населення: {effects.PopulationGrowthBonus:+0.##%;-0.##%;0%}\n" +
                          $"• Бонус науки: {effects.ResearchBonus:+0.##;-0.##;0}\n" +
                          $"• Бонус армії: {effects.ArmyBonus:+0;-0;0}";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.CountryEffectsKeyboard(country.IsInCrisis, country.IsCollapsed, country.IsInGoldenAge),
                cancellationToken
            );
        }

        private async Task ApplyCountryEffectsAction(ITelegramBotClient botClient, long userId, long chatId, int msgId, string actionKey, CancellationToken cancellationToken)
        {
            if (userId != masterID)
                return;

            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            if (country == null)
                return;

            string action = actionKey.ToLowerInvariant();
            if (action == "toggle_collapse")
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Ви впевнені, що хочете увімкнути колапс для цієї країни?",
                    StateKeyboardBuilder.CountryEffectsConfirmCollapseKeyboard(),
                    cancellationToken
                );
                return;
            }

            bool oldCrisis = country.IsInCrisis;
            bool oldCollapse = country.IsCollapsed;
            bool oldGolden = country.IsInGoldenAge;

            switch (action)
            {
                case "toggle_crisis":
                    country.IsInCrisis = !country.IsInCrisis;
                    if (country.IsInCrisis)
                    {
                        country.IsCollapsed = false;
                        country.IsInGoldenAge = false;
                    }
                    break;
                case "toggle_collapse_confirmed":
                    country.IsCollapsed = !country.IsCollapsed;
                    if (country.IsCollapsed)
                    {
                        country.IsInCrisis = false;
                        country.IsInGoldenAge = false;
                    }
                    break;
                case "toggle_golden":
                    country.IsInGoldenAge = !country.IsInGoldenAge;
                    if (country.IsInGoldenAge)
                    {
                        country.IsInCrisis = false;
                        country.IsCollapsed = false;
                    }
                    break;
                case "clear":
                    country.IsInCrisis = false;
                    country.IsCollapsed = false;
                    country.IsInGoldenAge = false;
                    break;
                default:
                    return;
            }

            CountriesData.UpdateCountry(country);
            LogCountryEffectChange(selectedCountryId.Value, userId, "Crisis", oldCrisis, country.IsInCrisis, action, country.TurnsSurvived);
            LogCountryEffectChange(selectedCountryId.Value, userId, "Collapse", oldCollapse, country.IsCollapsed, action, country.TurnsSurvived);
            LogCountryEffectChange(selectedCountryId.Value, userId, "GoldenAge", oldGolden, country.IsInGoldenAge, action, country.TurnsSurvived);
            await ShowCountryEffectsMenu(botClient, userId, chatId, msgId, cancellationToken);
        }

        private static void LogCountryEffectChange(int countryId, long userId, string effectType, bool oldValue, bool newValue, string action, int turnNumber)
        {
            if (oldValue == newValue)
                return;

            EconomyCountryEffectsLogData.AddLog(countryId, userId, effectType, oldValue, newValue, action, turnNumber);
        }

        private async Task ShowEconomyCurrencyMenu(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            EconomyCurrencyActionState.Remove(userId);
            EconomyCurrencyConvertState.Remove(userId);
            if (!CurrencyExchangeService.IsEnabled)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Валютна система вимкнена. Перекази/транші та позики доступні у Gold.",
                    StateKeyboardBuilder.EconomyCurrencyDisabledKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                "💱 Валюта",
                StateKeyboardBuilder.EconomyCurrencyMenuKeyboard(userId == masterID),
                cancellationToken
            );
        }

        private async Task ShowEconomyCurrencyRate(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            if (!CurrencyExchangeService.IsEnabled)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Валюта недоступна.",
                    StateKeyboardBuilder.EconomyCurrencyDisabledKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.EconomyCurrencyMenuKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }

            var service = new CurrencyExchangeService();
            string text = service.BuildRateSummary(selectedCountryId.Value);
            text += "\n\n" + service.BuildExampleTurnCalculation(selectedCountryId.Value);

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.EconomyCurrencyMenuKeyboard(userId == masterID),
                cancellationToken
            );
        }

        private async Task ShowEconomyCurrencyBlackMarket(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            if (!CurrencyExchangeService.IsEnabled)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Валюта недоступна.",
                    StateKeyboardBuilder.EconomyCurrencyDisabledKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни.",
                    StateKeyboardBuilder.EconomyCurrencyMenuKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }
            var service = new CurrencyExchangeService();
            string text = service.BuildBlackMarketRateView(selectedCountryId.Value);
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.EconomyCurrencyMenuKeyboard(userId == masterID),
                cancellationToken
            );
        }

        private async Task ShowEconomyCurrencyCentralBank(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            if (!CurrencyExchangeService.IsEnabled)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Валюта недоступна.",
                    StateKeyboardBuilder.EconomyCurrencyDisabledKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни.",
                    StateKeyboardBuilder.EconomyCurrencyMenuKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }
            var service = new CurrencyExchangeService();
            string text = service.BuildCentralBankView(selectedCountryId.Value);
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.EconomyCurrencyMenuKeyboard(userId == masterID),
                cancellationToken
            );
        }

        private async Task ShowEconomyCurrencyInflation(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            if (!CurrencyExchangeService.IsEnabled)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Валюта недоступна.",
                    StateKeyboardBuilder.EconomyCurrencyDisabledKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни.",
                    StateKeyboardBuilder.EconomyCurrencyMenuKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }
            var service = new CurrencyExchangeService();
            string text = service.BuildInflationView(selectedCountryId.Value);
            bool canManageInflation = CanManageInflation(userId, selectedCountryId.Value);
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.EconomyCurrencyInflationKeyboard(canManageInflation),
                cancellationToken
            );
        }

        private bool CanManageInflation(long userId, int selectedCountryId)
        {
            if (selectedCountryId <= 0)
                return false;

            return userId == masterID || IsSelectedCountryOwner(userId, selectedCountryId);
        }

        private async Task ShiftEconomyInflation(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            int delta,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни.",
                    StateKeyboardBuilder.EconomyCurrencyMenuKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }

            if (!CanManageInflation(userId, selectedCountryId.Value))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Нема доступу до керування інфляцією.",
                    StateKeyboardBuilder.EconomyCurrencyInflationKeyboard(false),
                    cancellationToken
                );
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            if (country == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Країну не знайдено.",
                    StateKeyboardBuilder.EconomyCurrencyMenuKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }

            float before = country.Inflation;
            float after = EconomyCalculator.Clamp(before + delta, 0f, 200f);
            country.Inflation = after;
            country.LastUpdate = DateTime.UtcNow;
            CountriesData.UpdateCountry(country);
            LogAdminAction(
                country.CountryId,
                userId,
                "Inflation",
                before.ToString(CultureInfo.InvariantCulture),
                after.ToString(CultureInfo.InvariantCulture),
                $"Shift:{delta:+#;-#;0}");

            await ShowEconomyCurrencyInflation(botClient, userId, chatId, msgId, cancellationToken);
        }

        private async Task SetEconomyInflation(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            float targetInflation,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни.",
                    StateKeyboardBuilder.EconomyCurrencyMenuKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }

            if (!CanManageInflation(userId, selectedCountryId.Value))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Нема доступу до керування інфляцією.",
                    StateKeyboardBuilder.EconomyCurrencyInflationKeyboard(false),
                    cancellationToken
                );
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            if (country == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Країну не знайдено.",
                    StateKeyboardBuilder.EconomyCurrencyMenuKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }

            float before = country.Inflation;
            float after = EconomyCalculator.Clamp(targetInflation, 0f, 200f);
            country.Inflation = after;
            country.LastUpdate = DateTime.UtcNow;
            CountriesData.UpdateCountry(country);
            LogAdminAction(
                country.CountryId,
                userId,
                "Inflation",
                before.ToString(CultureInfo.InvariantCulture),
                after.ToString(CultureInfo.InvariantCulture),
                $"Set:{after:0.##}");

            await ShowEconomyCurrencyInflation(botClient, userId, chatId, msgId, cancellationToken);
        }

        private async Task StartCurrencyConvertInput(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            if (!CurrencyExchangeService.IsEnabled)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Валюта недоступна.",
                    StateKeyboardBuilder.EconomyCurrencyDisabledKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }
            EconomyInputState[userId] = EconomyInputMode.CurrencyConvert;
            EconomyCurrencyUiContext[userId] = (chatId, msgId);
            EconomyCurrencyConvertState[userId] = new CurrencyConvertContext();
            await ShowCurrencySelect(botClient, userId, chatId, msgId, "from", 0, cancellationToken);
        }

        private async Task StartCurrencyAdjustInput(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            if (!CurrencyExchangeService.IsEnabled)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Валюта недоступна.",
                    StateKeyboardBuilder.EconomyCurrencyDisabledKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }
            if (userId != masterID)
            {
                await SafeEditMessageTextAsync(botClient, chatId, msgId, "Нема доступу.", StateKeyboardBuilder.EconomyCurrencyMenuKeyboard(false), cancellationToken);
                return;
            }
            EconomyInputState[userId] = EconomyInputMode.CurrencyAdjustRate;
            EconomyCurrencyUiContext[userId] = (chatId, msgId);
            EconomyCurrencyActionState[userId] = new CurrencyActionContext { Action = CurrencyActionType.AdjustRate };
            await ShowCurrencyCountrySelect(botClient, userId, chatId, msgId, CurrencyActionType.AdjustRate, "single", 0, cancellationToken);
        }

        private async Task StartCurrencyTransferInput(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            EconomyInputState[userId] = EconomyInputMode.CurrencyTransfer;
            EconomyCurrencyUiContext[userId] = (chatId, msgId);

            if (userId == masterID)
            {
                EconomyCurrencyActionState[userId] = new CurrencyActionContext
                {
                    Action = CurrencyActionType.Transfer,
                    TransferDays = 1
                };
                await ShowCurrencyCountrySelect(botClient, userId, chatId, msgId, CurrencyActionType.Transfer, "from", 0, cancellationToken);
                return;
            }

            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни.",
                    StateKeyboardBuilder.EconomyCurrencyMenuKeyboard(false),
                    cancellationToken
                );
                return;
            }

            if (!IsSelectedCountryOwner(userId, selectedCountryId.Value))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Нема доступу.",
                    StateKeyboardBuilder.EconomyCurrencyMenuKeyboard(false),
                    cancellationToken
                );
                return;
            }

            EconomyCurrencyActionState[userId] = new CurrencyActionContext
            {
                Action = CurrencyActionType.Transfer,
                FromCountryId = selectedCountryId.Value,
                TransferDays = 1
            };
            await ShowCurrencyCountrySelect(botClient, userId, chatId, msgId, CurrencyActionType.Transfer, "to", 0, cancellationToken);
        }

        private async Task StartCurrencyLoanInput(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (userId != masterID)
            {
                if (!selectedCountryId.HasValue)
                {
                    await SafeEditMessageTextAsync(botClient, chatId, msgId, "Не знайдено країни.", StateKeyboardBuilder.EconomyCurrencyMenuKeyboard(false), cancellationToken);
                    return;
                }
                if (!IsSelectedCountryOwner(userId, selectedCountryId.Value))
                {
                    await SafeEditMessageTextAsync(botClient, chatId, msgId, "Нема доступу.", StateKeyboardBuilder.EconomyCurrencyMenuKeyboard(false), cancellationToken);
                    return;
                }
            }
            EconomyInputState[userId] = EconomyInputMode.CurrencyLoan;
            EconomyCurrencyUiContext[userId] = (chatId, msgId);
            EconomyCurrencyActionState[userId] = new CurrencyActionContext
            {
                Action = CurrencyActionType.Loan,
                ToCountryId = selectedCountryId ?? 0
            };

            if (selectedCountryId.HasValue)
            {
                await ShowCurrencyCountrySelect(botClient, userId, chatId, msgId, CurrencyActionType.Loan, "from", 0, cancellationToken);
                return;
            }

            await ShowCurrencyCountrySelect(botClient, userId, chatId, msgId, CurrencyActionType.Loan, "to", 0, cancellationToken);
        }

        private async Task StartCurrencyRepayInput(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            if (userId != masterID)
            {
                await SafeEditMessageTextAsync(botClient, chatId, msgId, "Нема доступу.", StateKeyboardBuilder.EconomyCurrencyMenuKeyboard(false), cancellationToken);
                return;
            }
            EconomyInputState[userId] = EconomyInputMode.CurrencyRepay;
            EconomyCurrencyUiContext[userId] = (chatId, msgId);
            EconomyCurrencyActionState[userId] = new CurrencyActionContext { Action = CurrencyActionType.Repay };
            await ShowCurrencyCountrySelect(botClient, userId, chatId, msgId, CurrencyActionType.Repay, "single", 0, cancellationToken);
        }

        private async Task ShowEconomyReport(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var provider = new StateDataProvider();
            var state = provider.GetStateData(userId, selectedCountryId.Value);
            var service = new EconomyService();
            var dayPreview = service.PreviewTurn(selectedCountryId.Value, 1);
            long dayBudgetDelta = dayPreview.Income - dayPreview.Expenses;
            bool isMaster = userId == masterID;
            var lastTurn = EconomyTurnHistoryData.GetLatestTurn(selectedCountryId.Value);
            var buildings = EconomyBuildingsData.GetByCountry(selectedCountryId.Value);
            var trade = EconomyTradeAgreementsData.GetActiveByCountry(selectedCountryId.Value);
            int totalJobs = buildings.Sum(b => Math.Max(0, b.JobsProvided));
            var resources = EconomyResourcesData.GetAll().ToDictionary(r => r.ResourceType, StringComparer.OrdinalIgnoreCase);
            var festivals = EconomyFestivalsData.GetActiveByCountry(selectedCountryId.Value);
            string unemploymentLabel = (state.UnemploymentRate >= 99.9f && totalJobs == 0)
                ? $"{state.UnemploymentRate:0.##}% (немає робочих місць)"
                : $"{state.UnemploymentRate:0.##}%";

            float sanctionsModifier = EconomyCalculator.ApplySanctionsModifier(
                (SanctionsLevel)state.SanctionsLevel,
                EconomyConfigData.GetFloat("SanctionsNoneModifier", 1f),
                EconomyConfigData.GetFloat("SanctionsPartialModifier", 0.9f),
                EconomyConfigData.GetFloat("SanctionsFullModifier", 0.7f)
            );
            float warTradeModifier = state.WarState ? EconomyConfigData.GetFloat("WarTradeModifier", 0.7f) : 1f;
            decimal inflationPriceMultiplier = EconomyService.GetInflationPriceMultiplier(state.Inflation);
            decimal importLawFactor = ClampDecimal(dayPreview.ImportLimitFactor, 0m, 1m);
            decimal exportLawFactor = ClampDecimal(dayPreview.ExportLimitFactor, 0m, 1m);
            var partnerSanctions = EconomyCountrySanctionsData.GetBySource(selectedCountryId.Value)
                .ToDictionary(s => s.TargetCountryId, s => s);

            decimal agreementExportIncome = 0m;
            decimal agreementImportCost = 0m;
            var importQtyTotals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var exportQtyTotals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var agreementLines = new List<string>();
            foreach (var a in trade)
            {
                decimal exportBase = 0m;
                if (!string.IsNullOrWhiteSpace(a.ExportResourceType) && resources.TryGetValue(a.ExportResourceType, out var resExp))
                    exportBase = resExp.BasePrice;
                decimal importBase = 0m;
                if (!string.IsNullOrWhiteSpace(a.ImportResourceType) && resources.TryGetValue(a.ImportResourceType, out var resImp))
                    importBase = resImp.BasePrice;

                decimal exportUnit = (a.ExportUnitPrice > 0 ? a.ExportUnitPrice : exportBase) * inflationPriceMultiplier;
                decimal importUnit = (a.ImportUnitPrice > 0 ? a.ImportUnitPrice : importBase) * inflationPriceMultiplier;

                float exportPartnerModifier = 1f;
                float importPartnerModifier = 1f;
                if (a.PartnerCountryId.HasValue && partnerSanctions.TryGetValue(a.PartnerCountryId.Value, out var sanction))
                {
                    exportPartnerModifier = sanction.ExportLevel == (int)SanctionsLevel.Embargo
                        ? 0f
                        : EconomyCalculator.ApplyPercentModifier(sanction.ExportEffectiveness);

                    importPartnerModifier = sanction.ImportLevel == (int)SanctionsLevel.Embargo
                        ? 0f
                        : EconomyCalculator.ApplyPercentModifier(sanction.ImportEffectiveness);
                }

                decimal exportPerDayQty = GetTradeLegPerDayQty(a, isExport: true);
                decimal importPerDayQty = GetTradeLegPerDayQty(a, isExport: false);
                decimal exportTotal = exportPerDayQty * exportUnit;
                decimal importTotal = importPerDayQty * importUnit;
                exportTotal *= (decimal)sanctionsModifier * (decimal)warTradeModifier * (decimal)exportPartnerModifier * exportLawFactor;
                importTotal *= (decimal)sanctionsModifier * (decimal)warTradeModifier * (decimal)importPartnerModifier * importLawFactor;
                if (!string.IsNullOrWhiteSpace(a.ExportResourceType) && exportPerDayQty > 0m)
                {
                    decimal exportQty = exportPerDayQty * (decimal)sanctionsModifier * (decimal)warTradeModifier * (decimal)exportPartnerModifier * exportLawFactor;
                    if (exportQtyTotals.TryGetValue(a.ExportResourceType, out var current))
                        exportQtyTotals[a.ExportResourceType] = current + exportQty;
                    else
                        exportQtyTotals[a.ExportResourceType] = exportQty;
                }
                if (!string.IsNullOrWhiteSpace(a.ImportResourceType) && importPerDayQty > 0m)
                {
                    decimal importQty = importPerDayQty * (decimal)sanctionsModifier * (decimal)warTradeModifier * (decimal)importPartnerModifier * importLawFactor;
                    if (importQtyTotals.TryGetValue(a.ImportResourceType, out var current))
                        importQtyTotals[a.ImportResourceType] = current + importQty;
                    else
                        importQtyTotals[a.ImportResourceType] = importQty;
                }

                agreementExportIncome += exportTotal;
                agreementImportCost += importTotal;

                decimal net = exportTotal - importTotal;
                string line = $"{a.PartnerName}: +{FmtNumber(exportTotal)} / -{FmtNumber(importTotal)} / {FmtNumber(net)}";
                agreementLines.Add(line);
            }
            decimal agreementNet = agreementExportIncome - agreementImportCost;

            var breakdown = CalculateEconomyBreakdown(
                selectedCountryId.Value,
                state,
                buildings,
                festivals,
                trade,
                resources,
                agreementExportIncome,
                agreementImportCost
            );

            if (!isMaster)
            {
                var sbPublic = new StringBuilder();
                sbPublic.AppendLine($"📊 Економічний звіт: {state.CountryName}");
                sbPublic.AppendLine($"Казна: {FmtTreasury(state.Treasury, selectedCountryId.Value)}");
                sbPublic.AppendLine($"Інфляція: {state.Inflation:0.##}% (множник цін x{FmtNumberCompact(inflationPriceMultiplier, 3)})");
                sbPublic.AppendLine($"Біля казни (за день): +{FmtNumber(dayPreview.Income)} / -{FmtNumber(dayPreview.Expenses)} / Δ {FmtDeltaNumber(dayBudgetDelta)}");
                sbPublic.AppendLine($"Закони (щодня для казни): +{FmtNumber(dayPreview.LawTreasuryDailyIncome)} / -{FmtNumber(dayPreview.LawTreasuryDailyExpense)} / Δ {FmtDeltaNumber((long)Math.Round(dayPreview.LawTreasuryDailyIncome - dayPreview.LawTreasuryDailyExpense))}");
                sbPublic.AppendLine($"Казна після дня: {FmtNumber(dayPreview.TreasuryNow)} → {FmtNumber(dayPreview.TreasuryAfter)}");
                if (dayPreview.DeficitShortfall > 0)
                    sbPublic.AppendLine($"⚠ Дефіцит обмежено казною. Не покрито: {FmtNumber(dayPreview.DeficitShortfall)}");
                sbPublic.AppendLine($"Стабільність: {state.Stability:0.##}");
                sbPublic.AppendLine($"Безробіття: {unemploymentLabel}");
                sbPublic.AppendLine($"Війна: {(state.WarState ? "Так" : "Ні")}");
                sbPublic.AppendLine($"Санкції: {FormatSanctionsLevel(state.SanctionsLevel)}");
                sbPublic.AppendLine($"Торгових угод: {FmtNumber(trade.Count)}");
            sbPublic.AppendLine($"Ліміти торгівлі (закони): імпорт {(dayPreview.ImportLimitFactor * 100m):0.##}% | експорт {(dayPreview.ExportLimitFactor * 100m):0.##}%");
            sbPublic.AppendLine($"Ресурси компаній: спожито {FmtNumberCompact(dayPreview.CompanyResourceConsumed)} з {FmtNumberCompact(dayPreview.CompanyResourceRequired)} (покриття {dayPreview.CompanyResourceCoverage:0.##}%)");
            sbPublic.AppendLine();
            AppendPopulationIncomeTaxBreakdown(sbPublic, dayPreview, state, compactValues: false);

            sbPublic.AppendLine();
            if (lastTurn == null)
            {
                    sbPublic.AppendLine("📦 Попит/дефіцит: даних ще немає. Запустіть день економіки.");
                }
                else
                {
                    sbPublic.AppendLine($"📦 Попит/дефіцит (день {FmtNumber(lastTurn.TurnNumber)})");
                    sbPublic.AppendLine($"Їжа: {FmtNumberCompact(lastTurn.FoodDemand)}");
                    sbPublic.AppendLine($"Одяг: {FmtNumberCompact(lastTurn.ClothingDemand)}");
                    sbPublic.AppendLine($"Житло: {FmtNumberCompact(lastTurn.HousingDemand)}");
                    sbPublic.AppendLine($"Інструменти: {FmtNumberCompact(lastTurn.ToolsDemand)}");
                    sbPublic.AppendLine($"Розкіш: {FmtNumberCompact(lastTurn.LuxuryDemand)}");
                    sbPublic.AppendLine($"Дефіцит ресурсів: {FmtNumberCompact(lastTurn.ShortageScore)}");

                    if (lastTurn.BlackMarketPurchased > 0)
                    {
                        sbPublic.AppendLine();
                        sbPublic.AppendLine("🕶 Чорний ринок");
                        sbPublic.AppendLine($"Обсяг: {FmtNumber(lastTurn.BlackMarketPurchased)}");
                        sbPublic.AppendLine($"Вартість: {FmtNumber(lastTurn.BlackMarketValue)}");
                    }
                }

                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    sbPublic.ToString(),
                    StateKeyboardBuilder.EconomyReportKeyboard(isMaster),
                    cancellationToken
                );
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"📊 Економічний звіт: {state.CountryName}");
            sb.AppendLine($"Казна: {FmtTreasury(state.Treasury, selectedCountryId.Value)}");
            sb.AppendLine($"Податки: {state.TaxLevel}%");
            sb.AppendLine($"Інфляція: {state.Inflation:0.##}% (множник цін x{FmtNumberCompact(inflationPriceMultiplier, 3)})");
            sb.AppendLine($"Біля казни (за день): +{FmtNumber(dayPreview.Income)} / -{FmtNumber(dayPreview.Expenses)} / Δ {FmtDeltaNumber(dayBudgetDelta)}");
            sb.AppendLine($"Закони (щодня для казни): +{FmtNumber(dayPreview.LawTreasuryDailyIncome)} / -{FmtNumber(dayPreview.LawTreasuryDailyExpense)} / Δ {FmtDeltaNumber((long)Math.Round(dayPreview.LawTreasuryDailyIncome - dayPreview.LawTreasuryDailyExpense))}");
            sb.AppendLine($"Казна після дня: {FmtNumber(dayPreview.TreasuryNow)} → {FmtNumber(dayPreview.TreasuryAfter)}");
            if (dayPreview.DeficitShortfall > 0)
                sb.AppendLine($"⚠ Дефіцит обмежено казною. Не покрито: {FmtNumber(dayPreview.DeficitShortfall)}");
            sb.AppendLine($"Корупційні втрати: {FmtNumber(dayPreview.CorruptionLoss)}");
            sb.AppendLine($"Робоча сила: {FmtNumber(state.AvailableWorkers)} ({state.WorkRate:0.##}%)");
            sb.AppendLine($"Безробіття: {unemploymentLabel}");
            sb.AppendLine($"Війна: {(state.WarState ? "Так" : "Ні")}");
            sb.AppendLine($"Санкції: {FormatSanctionsLevel(state.SanctionsLevel)}");
            sb.AppendLine($"Робочі місця: {FmtNumber(totalJobs)}");
            sb.AppendLine($"Торгових угод: {FmtNumber(trade.Count)}");
            sb.AppendLine($"Ліміти торгівлі (закони): імпорт {(dayPreview.ImportLimitFactor * 100m):0.##}% | експорт {(dayPreview.ExportLimitFactor * 100m):0.##}%");
            sb.AppendLine($"Ресурси компаній: спожито {FmtNumberCompact(dayPreview.CompanyResourceConsumed)} з {FmtNumberCompact(dayPreview.CompanyResourceRequired)} (покриття {dayPreview.CompanyResourceCoverage:0.##}%, ліміт використання {(dayPreview.CompanyResourceUseFactor * 100m):0.##}%)");

            sb.AppendLine();
            if (lastTurn == null)
            {
                sb.AppendLine("📦 Виробництво / торгівля: даних ще немає. Запустіть день економіки.");
            }
            else
            {
                sb.AppendLine($"📦 Виробництво / торгівля (день {FmtNumber(lastTurn.TurnNumber)})");
                sb.AppendLine($"Виробництво: {FmtNumber(lastTurn.ProductionValue)}");
                sb.AppendLine($"Торгівля: {FmtNumber(lastTurn.TradeIncome)}");
                sb.AppendLine($"Податки: {FmtNumber(lastTurn.TaxIncome)}");
                sb.AppendLine($"Зайнятість: {lastTurn.EmploymentRate:0.##}%");
                sb.AppendLine($"Безробіття: {lastTurn.UnemploymentRate:0.##}%");
                sb.AppendLine($"Дефіцит ресурсів: {FmtNumberCompact(lastTurn.ShortageScore)}");

                sb.AppendLine();
                sb.AppendLine($"📦 Попит (день {FmtNumber(lastTurn.TurnNumber)})");
                sb.AppendLine($"Їжа: {FmtNumberCompact(lastTurn.FoodDemand)}");
                sb.AppendLine($"Одяг: {FmtNumberCompact(lastTurn.ClothingDemand)}");
                sb.AppendLine($"Житло: {FmtNumberCompact(lastTurn.HousingDemand)}");
                sb.AppendLine($"Інструменти: {FmtNumberCompact(lastTurn.ToolsDemand)}");
                sb.AppendLine($"Розкіш: {FmtNumberCompact(lastTurn.LuxuryDemand)}");

                if (lastTurn.BlackMarketPurchased > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("🕶 Чорний ринок");
                    sb.AppendLine($"Обсяг: {FmtNumber(lastTurn.BlackMarketPurchased)}");
                    sb.AppendLine($"Вартість: {FmtNumber(lastTurn.BlackMarketValue)}");
                }
            }

            var resourceOutput = BuildResourceOutputSummary(state, buildings, festivals, out float productionEfficiency);
            if (resourceOutput.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("📦 Вихід ресурсів (сумарно)");
                sb.AppendLine($"Ефективність виробництва: {productionEfficiency:0.##}%");
                foreach (var line in resourceOutput)
                    sb.AppendLine($"• {line}");
            }

            var productionNow = BuildResourceProductionMap(state, buildings, festivals, out _);
            var trackedResources = new[] { "Food", "Clothing", "Housing", "Tools", "Luxury" };
            Dictionary<string, decimal>? estimatedDemand = null;
            if (lastTurn == null)
            {
                var globalModifiers = EconomyGlobalModifiersData.GetActive();
                estimatedDemand = BuildEstimatedDemandByResource(state, globalModifiers, resources);
            }

            sb.AppendLine();
            sb.AppendLine("📊 Забезпечення ресурсами (оцінка на зараз)");
            if (lastTurn == null)
                sb.AppendLine("ℹ Попит розраховано за поточним станом (ще без завершеного дня економіки).");

            foreach (var resource in trackedResources)
            {
                decimal demand = lastTurn != null
                    ? GetDemandByResource(lastTurn, resource)
                    : (estimatedDemand != null && estimatedDemand.TryGetValue(resource, out var estimated) ? estimated : 0m);

                decimal netProduced = SumTrackedResourceAmount(productionNow, resource, resources);
                decimal imported = SumTrackedResourceAmount(importQtyTotals, resource, resources);
                decimal exported = SumTrackedResourceAmount(exportQtyTotals, resource, resources);
                decimal available = Math.Max(0m, netProduced + imported - exported);
                decimal coveragePct = demand <= 0m ? 100m : (available / demand) * 100m;
                string coverageIcon = coveragePct >= 100m ? "✅" : coveragePct >= 90m ? "🟡" : "❌";

                sb.AppendLine($"{coverageIcon} {FormatResourceTypeUk(resource)}: попит {FmtNumberCompact(demand)}, доступно {FmtNumberCompact(available)}");
                sb.AppendLine($"   нетто-виробн {FmtNumberCompact(netProduced)} | імп {FmtNumberCompact(imported)} | експ {FmtNumberCompact(exported)} | покриття {FmtNumberCompact(coveragePct, 1)}%");
            }

            if (lastTurn != null)
            {
                sb.AppendLine();
                sb.AppendLine($"🧠 Вплив дефіциту минулого дня: щастя {lastTurn.HappinessDelta:+0.##;-0.##;0}, смертність +{(lastTurn.MortalityDelta * 100f):0.##}%");
            }

            sb.AppendLine();
            sb.AppendLine("🤝 Торгівля (угоди)");
            sb.AppendLine($"Експорт: {FmtNumber(agreementExportIncome)}");
            sb.AppendLine($"Імпорт: {FmtNumber(agreementImportCost)}");
            sb.AppendLine($"Баланс: {FmtNumber(agreementNet)}");

            sb.AppendLine();
            sb.AppendLine("💰 Доходи (за день)");
            sb.AppendLine($"Ставки доходів (осн/соц/брутто+/нетто+): {(dayPreview.MainIncomeTaxRate * 100m):0.##}% / {(dayPreview.SocialContributionRate * 100m):0.##}% / {(dayPreview.AdditionalGrossIncomeTaxRate * 100m):0.##}% / {(dayPreview.AdditionalNetIncomeTaxRate * 100m):0.##}%");
            sb.AppendLine($"Податки із зарплат: {FmtNumber(dayPreview.WagesTax)}");
            sb.AppendLine($"Податки з виробництва: {FmtNumber(dayPreview.ProfitTax)}");
            sb.AppendLine($"Соціальні збори із зарплат: {FmtNumber(dayPreview.SocialContributionIncome)}");
            sb.AppendLine($"Податок на брутто-дохід: {FmtNumber(dayPreview.AdditionalGrossIncomeTax)}");
            sb.AppendLine($"Податок на нетто-дохід: {FmtNumber(dayPreview.AdditionalNetIncomeTax)}");
            sb.AppendLine($"Доходи громадян (брутто/нетто): {FmtNumber(dayPreview.CitizenGrossIncome)} / {FmtNumber(dayPreview.CitizenNetIncome)}");
            sb.AppendLine($"Торгівля (усього): {FmtNumber(dayPreview.TradeIncome)}");
            sb.AppendLine($"Угоди (довідково): +{FmtNumber(dayPreview.AgreementExportIncome)} / -{FmtNumber(dayPreview.AgreementImportCost)} = {FmtNumber(dayPreview.AgreementExportIncome - dayPreview.AgreementImportCost)}");
            sb.AppendLine($"Дивіденди держкомпаній: {FmtNumber(dayPreview.StateCompanyDividends)}");
            sb.AppendLine($"Закони (щоденне поповнення казни): {FmtNumber(dayPreview.LawTreasuryDailyIncome)}");
            sb.AppendLine($"Разом доходів: {FmtNumber(dayPreview.Income)}");
            sb.AppendLine();
            AppendPopulationIncomeTaxBreakdown(sb, dayPreview, state, compactValues: false);

            sb.AppendLine();
            sb.AppendLine("💸 Витрати (за день)");
            sb.AppendLine($"Бюджет: інф {FmtNumber(dayPreview.BudgetInfrastructure)} / армія {FmtNumber(dayPreview.BudgetMilitary)} / наука {FmtNumber(dayPreview.BudgetScience)} / соц {FmtNumber(dayPreview.BudgetSocial)}");
            sb.AppendLine($"Утримання будівель: {FmtNumber(dayPreview.Upkeep)}");
            sb.AppendLine($"Зарплати держслужбовцям: {FmtNumber(dayPreview.GovSalaries)}");
            sb.AppendLine($"Соц. виплати: {FmtNumber(dayPreview.SocialSpending)}");
            sb.AppendLine($"Наука: {FmtNumber(dayPreview.ScienceSpending)}");
            sb.AppendLine($"Фестивалі: {FmtNumber(dayPreview.FestivalCost)}");
            sb.AppendLine($"Армія (після ліміту): {FmtNumber(dayPreview.MilitaryUpkeep)}");
            sb.AppendLine($"Інфраструктура: {FmtNumber(dayPreview.InfrastructureSpending)}");
            sb.AppendLine($"Стабілізаційне фінансування: {FmtNumber(dayPreview.StabilizationReserveFunding)}");
            sb.AppendLine($"Корупційні втрати: {FmtNumber(dayPreview.CorruptionLoss)}");
            sb.AppendLine($"Парламент: {FmtNumber(dayPreview.DeputySalaryTotal)} / крадіжки: {FmtNumber(dayPreview.DeputyTheft)}");
            sb.AppendLine($"Закони (щоденні витрати казни): {FmtNumber(dayPreview.LawTreasuryDailyExpense)}");
            sb.AppendLine($"Разом витрат: {FmtNumber(dayPreview.Expenses)}");

            sb.AppendLine();
            sb.AppendLine($"⚖ Баланс (за день, факт для казни): {FmtDeltaNumber(dayPreview.NetResult)}");

            if (agreementLines.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("📄 По угодах:");
                int maxLines = 10;
                foreach (var line in agreementLines.Take(maxLines))
                    sb.AppendLine($"• {line}");
                if (agreementLines.Count > maxLines)
                    sb.AppendLine($"…ще {FmtNumber(agreementLines.Count - maxLines)}");
            }

            var text = sb.ToString();

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.EconomyReportKeyboard(isMaster),
                cancellationToken
            );
        }

        private async Task ShowEconomySectorReport(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken,
            string? statusMessage = null)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            bool canEdit = userId == masterID || IsSelectedCountryOwner(userId, selectedCountryId);
            var provider = new StateDataProvider();
            var state = provider.GetStateData(userId, selectedCountryId.Value);

            var allCompanies = EconomyCompaniesData.GetByCountry(selectedCountryId.Value);
            var companies = allCompanies.Where(c => c.IsActive).ToList();
            bool usingActive = companies.Count > 0;
            if (!usingActive)
                companies = allCompanies;

            var sectorSummaries = BuildEconomySectorSummaries(companies);
            if (sectorSummaries.Count == 0)
                sectorSummaries.Add(new EconomySectorSummary { SectorKey = "General" });

            string selectedSector = ResolveSelectedEconomySector(userId, sectorSummaries);
            EconomySectorSelectionByUser[userId] = selectedSector;

            var selectedSectorSummary = sectorSummaries
                .FirstOrDefault(s => string.Equals(s.SectorKey, selectedSector, StringComparison.OrdinalIgnoreCase));
            decimal sectorRevenueMultiplier = GetEconomySectorRevenueMultiplier(selectedCountryId.Value, selectedSector);
            decimal? sectorTaxOverride = GetEconomySectorTaxOverride(selectedCountryId.Value, selectedSector);

            var topCompaniesInSector = companies
                .Where(c => string.Equals(NormalizeEconomySectorKey(c.ProductionType), selectedSector, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(c => Math.Max(0m, c.BaseSalary) * Math.Max(0, c.JobsOffered))
                .ThenBy(c => c.Name)
                .Take(8)
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine($"🏭 Галузевий звіт: {state.CountryName}");
            sb.AppendLine($"Компанії у вибірці: {FmtNumber(companies.Count)} {(usingActive ? "(активні)" : "(усі)")}");

            if (companies.Count == 0)
            {
                sb.AppendLine();
                sb.AppendLine("Компанії ще не створені.");
            }
            else
            {
                sb.AppendLine();
                sb.AppendLine("Сектори (компанії / місця / фонд зарплат):");
                foreach (var sector in sectorSummaries)
                {
                    string marker = string.Equals(sector.SectorKey, selectedSector, StringComparison.OrdinalIgnoreCase) ? "👉" : "•";
                    sb.AppendLine($"{marker} {FormatProductionTypeUk(sector.SectorKey)}: {FmtNumber(sector.CompaniesCount)} / {FmtNumber(sector.Jobs)} / {FmtNumberCompact(sector.Payroll)}");
                }

                sb.AppendLine();
                sb.AppendLine($"🔎 Обраний сектор: {FormatProductionTypeUk(selectedSector)}");
                sb.AppendLine($"Множник доходу сектора: x{FmtNumberCompact(sectorRevenueMultiplier, 3)}");
                sb.AppendLine($"Податок сектора: {(sectorTaxOverride.HasValue ? $"{(sectorTaxOverride.Value * 100m):0.##}%" : "авто (ставка компаній)")}");

                if (selectedSectorSummary != null)
                {
                    sb.AppendLine($"Компаній: {FmtNumber(selectedSectorSummary.CompaniesCount)}");
                    sb.AppendLine($"Робочі місця: {FmtNumber(selectedSectorSummary.Jobs)}");
                    sb.AppendLine($"Сер. зарплата: {FmtNumberCompact(selectedSectorSummary.AverageSalary, 3)}");
                    sb.AppendLine($"Сер. податок (факт): {(selectedSectorSummary.AverageTaxRate * 100m):0.##}%");
                    sb.AppendLine($"Бюджети компаній: {FmtNumberCompact(selectedSectorSummary.Budgets)}");
                }

                if (topCompaniesInSector.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("Хто скільки отримує (топ компаній):");
                    foreach (var company in topCompaniesInSector)
                    {
                        int jobs = Math.Max(0, company.JobsOffered);
                        decimal salary = Math.Max(0m, company.BaseSalary);
                        decimal payroll = salary * jobs;
                        sb.AppendLine($"• {company.Name}: зарплата {FmtNumberCompact(salary, 3)}, місця {FmtNumber(jobs)}, фонд {FmtNumberCompact(payroll)}");
                    }
                }
            }

            sb.AppendLine();
            if (canEdit)
                sb.AppendLine("Редагування: оберіть сектор і змініть множник доходу або податок сектора.");
            else
                sb.AppendLine("Режим перегляду: редагування доступне власнику країни або майстру.");

            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                sb.AppendLine();
                sb.AppendLine(statusMessage);
            }

            var sectorButtons = sectorSummaries
                .Select(s =>
                {
                    bool isSelected = string.Equals(s.SectorKey, selectedSector, StringComparison.OrdinalIgnoreCase);
                    string title = $"{(isSelected ? "✅" : "▫")} {FormatProductionTypeUk(s.SectorKey)}";
                    string callback = $"state_economy_report_sector_select_{EncodeKey(s.SectorKey)}";
                    return (title, callback);
                })
                .ToList();

            EconomySectorUiContext[userId] = (chatId, msgId);

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.EconomyReportSectorsKeyboard(sectorButtons, canEdit, hasSelection: true),
                cancellationToken
            );
        }

        private async Task SelectEconomySectorReport(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            string encodedSectorToken,
            CancellationToken cancellationToken)
        {
            string decodedSector;
            try
            {
                decodedSector = DecodeKey(encodedSectorToken);
            }
            catch
            {
                decodedSector = "General";
            }

            EconomySectorSelectionByUser[userId] = NormalizeEconomySectorKey(decodedSector);
            await ShowEconomySectorReport(botClient, userId, chatId, msgId, cancellationToken);
        }

        private async Task StartEconomySectorRevenueInput(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            if (userId != masterID && !IsSelectedCountryOwner(userId, selectedCountryId))
            {
                await ShowEconomySectorReport(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу до редагування секторів.");
                return;
            }

            string sectorKey = ResolveSelectedEconomySectorForCountry(userId, selectedCountryId.Value);
            decimal current = GetEconomySectorRevenueMultiplier(selectedCountryId.Value, sectorKey);

            EconomySectorUiContext[userId] = (chatId, msgId);
            EconomyInputState[userId] = EconomyInputMode.SectorRevenueMultiplier;
            EconomyInputStep[userId] = 0;

            await SendHelperMessageAsync(
                botClient,
                userId,
                $"Вкажіть множник доходу для сектора \"{FormatProductionTypeUk(sectorKey)}\" ({FmtNumberCompact(SectorRevenueMultiplierMin, 1)}-{FmtNumberCompact(SectorRevenueMultiplierMax, 1)}).\nПоточне значення: x{FmtNumberCompact(current, 3)}",
                cancellationToken);
        }

        private async Task StartEconomySectorTaxInput(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            if (userId != masterID && !IsSelectedCountryOwner(userId, selectedCountryId))
            {
                await ShowEconomySectorReport(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу до редагування секторів.");
                return;
            }

            string sectorKey = ResolveSelectedEconomySectorForCountry(userId, selectedCountryId.Value);
            decimal? current = GetEconomySectorTaxOverride(selectedCountryId.Value, sectorKey);

            EconomySectorUiContext[userId] = (chatId, msgId);
            EconomyInputState[userId] = EconomyInputMode.SectorTaxOverride;
            EconomyInputStep[userId] = 0;

            string currentLabel = current.HasValue
                ? $"{(current.Value * 100m):0.##}%"
                : "авто (ставка компаній)";

            await SendHelperMessageAsync(
                botClient,
                userId,
                $"Вкажіть податок сектора \"{FormatProductionTypeUk(sectorKey)}\" у % (0-95).\nПоточне значення: {currentLabel}\nДля автоставки введіть `auto` / `off`.",
                cancellationToken);
        }

        private async Task ResetEconomySectorSettings(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            if (userId != masterID && !IsSelectedCountryOwner(userId, selectedCountryId))
            {
                await ShowEconomySectorReport(botClient, userId, chatId, msgId, cancellationToken, "Нема доступу до редагування секторів.");
                return;
            }

            string sectorKey = ResolveSelectedEconomySectorForCountry(userId, selectedCountryId.Value);
            bool savedMultiplier = EconomyConfigData.UpsertCountryDecimal(
                selectedCountryId.Value,
                BuildSectorRevenueMultiplierConfigKey(sectorKey),
                1m);
            bool savedTax = EconomyConfigData.UpsertCountryDecimal(
                selectedCountryId.Value,
                BuildSectorTaxOverrideConfigKey(sectorKey),
                SectorTaxOverrideAutoValue);

            if (EconomyInputState.TryGetValue(userId, out var mode) &&
                (mode == EconomyInputMode.SectorRevenueMultiplier || mode == EconomyInputMode.SectorTaxOverride))
            {
                await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
                ClearEconomyInput(userId);
            }

            string status = (savedMultiplier && savedTax)
                ? $"✅ Налаштування сектора \"{FormatProductionTypeUk(sectorKey)}\" скинуто."
                : $"❌ Не вдалося скинути налаштування сектора \"{FormatProductionTypeUk(sectorKey)}\".";

            await ShowEconomySectorReport(botClient, userId, chatId, msgId, cancellationToken, status);
        }

        private static List<EconomySectorSummary> BuildEconomySectorSummaries(IReadOnlyCollection<EconomyCompanyRecord> companies)
        {
            if (companies == null || companies.Count == 0)
                return new List<EconomySectorSummary>();

            return companies
                .GroupBy(c => NormalizeEconomySectorKey(c.ProductionType), StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    long jobs = group.Sum(c => Math.Max(0, c.JobsOffered));
                    decimal payroll = group.Sum(c => Math.Max(0m, c.BaseSalary) * Math.Max(0, c.JobsOffered));
                    decimal budgets = group.Sum(c => Math.Max(0m, c.Budget));

                    decimal avgSalary;
                    decimal avgTax;
                    if (jobs > 0)
                    {
                        decimal salaryWeighted = group.Sum(c => Math.Max(0m, c.BaseSalary) * Math.Max(0, c.JobsOffered));
                        decimal taxWeighted = group.Sum(c => Math.Max(0m, c.TaxRate) * Math.Max(0, c.JobsOffered));
                        avgSalary = salaryWeighted / jobs;
                        avgTax = taxWeighted / jobs;
                    }
                    else
                    {
                        avgSalary = group.Any() ? group.Average(c => Math.Max(0m, c.BaseSalary)) : 0m;
                        avgTax = group.Any() ? group.Average(c => Math.Max(0m, c.TaxRate)) : 0m;
                    }

                    return new EconomySectorSummary
                    {
                        SectorKey = group.Key,
                        CompaniesCount = group.Count(),
                        Jobs = jobs,
                        Payroll = payroll,
                        AverageSalary = avgSalary,
                        AverageTaxRate = avgTax,
                        Budgets = budgets
                    };
                })
                .OrderBy(s => GetSectorDisplayOrderIndex(s.SectorKey))
                .ThenByDescending(s => s.Jobs)
                .ThenByDescending(s => s.CompaniesCount)
                .ToList();
        }

        private static int GetSectorDisplayOrderIndex(string? sectorKey)
        {
            string normalized = NormalizeEconomySectorKey(sectorKey);
            for (int i = 0; i < SectorDisplayOrder.Length; i++)
            {
                if (string.Equals(SectorDisplayOrder[i], normalized, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return int.MaxValue;
        }

        private static string ResolveSelectedEconomySector(long userId, IReadOnlyList<EconomySectorSummary> sectorSummaries)
        {
            if (sectorSummaries == null || sectorSummaries.Count == 0)
                return "General";

            if (EconomySectorSelectionByUser.TryGetValue(userId, out var selected))
            {
                string normalizedSelected = NormalizeEconomySectorKey(selected);
                if (sectorSummaries.Any(s => string.Equals(s.SectorKey, normalizedSelected, StringComparison.OrdinalIgnoreCase)))
                    return normalizedSelected;
            }

            return sectorSummaries[0].SectorKey;
        }

        private static string ResolveSelectedEconomySectorForCountry(long userId, int countryId)
        {
            if (countryId <= 0)
                return "General";

            var companies = EconomyCompaniesData.GetByCountry(countryId);
            var active = companies.Where(c => c.IsActive).ToList();
            var source = active.Count > 0 ? active : companies;
            var sectors = BuildEconomySectorSummaries(source);
            if (sectors.Count == 0)
            {
                EconomySectorSelectionByUser[userId] = "General";
                return "General";
            }

            string selected = ResolveSelectedEconomySector(userId, sectors);
            EconomySectorSelectionByUser[userId] = selected;
            return selected;
        }

        private static string NormalizeEconomySectorKey(string? productionType)
        {
            string normalized = NormalizeProductionType(productionType);
            if (string.IsNullOrWhiteSpace(normalized))
                return "General";

            string value = normalized.Trim().ToLowerInvariant();
            string compact = value.Replace(" ", string.Empty).Replace("-", string.Empty).Replace("_", string.Empty);

            switch (compact)
            {
                case "general":
                case "загальне":
                    return "General";
                case "food":
                case "харчі":
                case "agriculture":
                case "farm":
                case "ферма":
                    return "Food";
                case "industry":
                case "промисловість":
                case "manufacturing":
                    return "Industry";
                case "services":
                case "послуги":
                case "finance":
                case "financial":
                    return "Services";
                case "logistics":
                case "логістика":
                    return "Logistics";
                case "weapons":
                case "arms":
                case "зброя":
                case "озброєння":
                    return "Weapons";
                case "militaryindustry":
                case "military":
                case "військове":
                case "військпром":
                case "оборонпром":
                case "defense":
                    return "MilitaryIndustry";
                case "siegeworks":
                case "siege":
                case "облогова":
                case "облогове":
                    return "SiegeWorks";
                case "arcanearms":
                case "арканзброя":
                case "магічназброя":
                    return "ArcaneArms";
            }

            if (compact.Contains("weapon") || compact.Contains("arms") || compact.Contains("збро"))
                return "Weapons";
            if (compact.Contains("military") || compact.Contains("військ") || compact.Contains("оборон") || compact.Contains("defense"))
                return "MilitaryIndustry";
            if (compact.Contains("siege") || compact.Contains("облог"))
                return "SiegeWorks";
            if (compact.Contains("arcane") || compact.Contains("аркан") || compact.Contains("магіч"))
                return "ArcaneArms";
            if (compact.Contains("logistic") || compact.Contains("логіст"))
                return "Logistics";
            if (compact.Contains("service") || compact.Contains("послуг") || compact.Contains("financ"))
                return "Services";
            if (compact.Contains("agri") || compact.Contains("farm") || compact.Contains("харч"))
                return "Food";
            if (compact.Contains("indust") || compact.Contains("manufact") || compact.Contains("промисл"))
                return "Industry";

            return "General";
        }

        private static string BuildSectorRevenueMultiplierConfigKey(string sectorKey)
        {
            return $"{SectorRevenueMultiplierConfigPrefix}{NormalizeEconomySectorKey(sectorKey)}";
        }

        private static string BuildSectorTaxOverrideConfigKey(string sectorKey)
        {
            return $"{SectorTaxOverrideConfigPrefix}{NormalizeEconomySectorKey(sectorKey)}";
        }

        private static decimal GetEconomySectorRevenueMultiplier(int countryId, string sectorKey)
        {
            string key = BuildSectorRevenueMultiplierConfigKey(sectorKey);
            decimal configured = EconomyConfigData.GetDecimalForCountry(countryId, key, 1m);
            return ClampDecimal(configured, SectorRevenueMultiplierMin, SectorRevenueMultiplierMax);
        }

        private static decimal? GetEconomySectorTaxOverride(int countryId, string sectorKey)
        {
            string key = BuildSectorTaxOverrideConfigKey(sectorKey);
            decimal configured = EconomyConfigData.GetDecimalForCountry(countryId, key, SectorTaxOverrideAutoValue);
            if (configured < 0m)
                return null;
            return ClampDecimal(configured, 0m, SectorTaxOverrideMax);
        }

        private async Task ShowEconomyWarnings(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var service = new EconomyService();
            var warnings = service.BuildWarnings(selectedCountryId.Value);
            string text;
            if (warnings.Count == 0)
            {
                text = "✅ Економічні попередження: відсутні.";
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine("⚠ Економічні попередження");
                foreach (var w in warnings)
                {
                    string icon = w.Severity switch
                    {
                        EconomyWarningSeverity.Critical => "🛑",
                        EconomyWarningSeverity.Warning => "⚠",
                        _ => "ℹ"
                    };
                    sb.AppendLine($"{icon} {w.Message}");
                    if (w.SuggestedActions != null && w.SuggestedActions.Count > 0)
                        sb.AppendLine($"  Рішення: {string.Join(", ", w.SuggestedActions)}");
                }
                text = sb.ToString();
            }

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.EconomyWarningsKeyboard(),
                cancellationToken
            );
        }

        private async Task ShowEconomyPreview(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            int days = EconomyPreviewDaysBuffer.TryGetValue(userId, out var stored)
                ? stored
                : (int)Math.Round(EconomyConfigData.GetFloat("PreviewDaysDefault", 7f));
            if (days < 1) days = 1;

            var service = new EconomyService();
            var preview = service.PreviewTurn(selectedCountryId.Value, days);
            long previewBudgetDelta = preview.Income - preview.Expenses;

            var sb = new StringBuilder();
            sb.AppendLine($"🔮 Прогноз економіки через {FmtNumber(preview.Days)} дн.");
            sb.AppendLine();
            sb.AppendLine($"Казна: {FmtNumber(preview.TreasuryNow)} → {FmtNumber(preview.TreasuryAfter)}");
            sb.AppendLine($"Гроші за період: +{FmtNumber(preview.Income)} / -{FmtNumber(preview.Expenses)} / Δ {FmtDeltaNumber(previewBudgetDelta)}");
            sb.AppendLine($"Закони (казна за період): +{FmtNumber(preview.LawTreasuryDailyIncome)} / -{FmtNumber(preview.LawTreasuryDailyExpense)} / Δ {FmtDeltaNumber((long)Math.Round(preview.LawTreasuryDailyIncome - preview.LawTreasuryDailyExpense))}");
            if (preview.StabilizationReserveFunding > 0m)
                sb.AppendLine($"Стабілізаційне фінансування (за період): {FmtNumber(preview.StabilizationReserveFunding)}");
            if (preview.Days > 1)
                sb.AppendLine($"Середньо за день: +{FmtNumber(Math.Round(preview.Income / (decimal)preview.Days))} / -{FmtNumber(Math.Round(preview.Expenses / (decimal)preview.Days))} / Δ {FmtDeltaNumber((long)Math.Round(previewBudgetDelta / (decimal)preview.Days))}");
            if (preview.DeficitShortfall > 0)
                sb.AppendLine($"⚠ Дефіцит обмежено казною. Не покрито: {FmtNumber(preview.DeficitShortfall)}");
            sb.AppendLine($"Ліміти торгівлі (закони): імпорт {(preview.ImportLimitFactor * 100m):0.##}% | експорт {(preview.ExportLimitFactor * 100m):0.##}%");
            sb.AppendLine($"Ресурси компаній: спожито {FmtNumberCompact(preview.CompanyResourceConsumed)} з {FmtNumberCompact(preview.CompanyResourceRequired)} (покриття {preview.CompanyResourceCoverage:0.##}%, ліміт використання {(preview.CompanyResourceUseFactor * 100m):0.##}%)");
            sb.AppendLine("✅ Прогноз враховує закони, ліміти торгівлі та споживання ресурсів компаніями.");
            sb.AppendLine($"Стабільність: {preview.StabilityNow:0.##} → {preview.StabilityAfter:0.##}");
            sb.AppendLine($"Безробіття: {preview.UnemploymentNow:0.##}% → {preview.UnemploymentAfter:0.##}%");
            sb.AppendLine($"Ризик кризи: {preview.CrisisRisk}%");

            if (preview.Warnings.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Попередження:");
                foreach (var w in preview.Warnings)
                {
                    string icon = w.Severity switch
                    {
                        EconomyWarningSeverity.Critical => "🛑",
                        EconomyWarningSeverity.Warning => "⚠",
                        _ => "ℹ"
                    };
                    sb.AppendLine($"{icon} {w.Message}");
                }
            }

            EconomyPreviewUiContext[userId] = (chatId, msgId);

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.EconomyPreviewKeyboard(),
                cancellationToken
            );
        }

        private async Task StartEconomyPreviewDaysInput(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            EconomyPreviewUiContext[userId] = (chatId, msgId);
            EconomyInputState[userId] = EconomyInputMode.PreviewDays;
            EconomyInputStep[userId] = 0;
            await botClient.SendTextMessageAsync(userId, "Вкажіть кількість днів для прогнозу (ціле число):", cancellationToken: cancellationToken);
        }

        private async Task ShowEconomyBreakdown(ITelegramBotClient botClient, long userId, long chatId, int msgId, string kind, CancellationToken cancellationToken)
        {
            if (userId != masterID)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Нема доступу.",
                    StateKeyboardBuilder.EconomyReportKeyboard(false),
                    cancellationToken
                );
                return;
            }

            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var provider = new StateDataProvider();
            var state = provider.GetStateData(userId, selectedCountryId.Value);
            if (state == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Країну не знайдено.",
                    StateKeyboardBuilder.EconomyLaborKeyboard(),
                    cancellationToken
                );
                return;
            }

            var preview = new EconomyService().PreviewTurn(selectedCountryId.Value, 1);
            string normalizedKind = (kind ?? string.Empty).Trim().ToLowerInvariant();
            string text = normalizedKind switch
            {
                "income" => BuildIncomeBreakdownText(preview, state),
                "expenses" => BuildExpensesBreakdownText(preview),
                "stability" => BuildStabilityBreakdownText(state, preview),
                _ => BuildUnemploymentBreakdownText(state, preview, EconomyBuildingsData.GetByCountry(selectedCountryId.Value))
            };

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.EconomyReportKeyboard(true),
                cancellationToken
            );
        }

        private static string BuildIncomeBreakdownText(EconomyPreviewResult preview, StateDataClass state)
        {
            decimal agreementNet = preview.AgreementExportIncome - preview.AgreementImportCost;
            decimal tradeBase = preview.TradeIncome - agreementNet;
            decimal incomeTotal = preview.Income;
            decimal componentsTotal =
                preview.WagesTax +
                preview.ProfitTax +
                preview.SocialContributionIncome +
                preview.AdditionalGrossIncomeTax +
                preview.AdditionalNetIncomeTax +
                tradeBase +
                agreementNet +
                preview.StateCompanyDividends +
                preview.LawTreasuryDailyIncome;
            decimal residual = incomeTotal - componentsTotal;

            var sb = new StringBuilder();
            sb.AppendLine("ℹ Деталі: Доходи");
            sb.AppendLine("Період: 1 день");
            sb.AppendLine($"Ставки доходів (осн/соц/брутто+/нетто+): {(preview.MainIncomeTaxRate * 100m):0.##}% / {(preview.SocialContributionRate * 100m):0.##}% / {(preview.AdditionalGrossIncomeTaxRate * 100m):0.##}% / {(preview.AdditionalNetIncomeTaxRate * 100m):0.##}%");
            sb.AppendLine($"Податки із зарплат: {FmtNumberCompact(preview.WagesTax)}");
            sb.AppendLine($"Податки з виробництва: {FmtNumberCompact(preview.ProfitTax)}");
            sb.AppendLine($"Соціальні збори із зарплат: {FmtNumberCompact(preview.SocialContributionIncome)}");
            sb.AppendLine($"Додатковий податок на брутто-дохід: {FmtNumberCompact(preview.AdditionalGrossIncomeTax)}");
            sb.AppendLine($"Додатковий податок на нетто-дохід: {FmtNumberCompact(preview.AdditionalNetIncomeTax)}");
            sb.AppendLine($"Доходи громадян (брутто/нетто): {FmtNumberCompact(preview.CitizenGrossIncome)} / {FmtNumberCompact(preview.CitizenNetIncome)}");
            sb.AppendLine($"Торгівля (базова, від виробництва): {FmtNumberCompact(tradeBase)}");
            sb.AppendLine($"Торгівля (угоди): +{FmtNumberCompact(preview.AgreementExportIncome)} / -{FmtNumberCompact(preview.AgreementImportCost)} = {FmtSignedDecimal(agreementNet)}");
            sb.AppendLine($"Дивіденди держкомпаній: {FmtNumberCompact(preview.StateCompanyDividends)}");
            sb.AppendLine($"Закони (поповнення казни): {FmtNumberCompact(preview.LawTreasuryDailyIncome)}");
            if (Math.Abs(residual) >= 0.01m)
                sb.AppendLine($"Коригування доходу (глобальні/дипломатичні ефекти): {FmtSignedDecimal(residual)}");
            sb.AppendLine($"Разом доходів: {FmtNumber(preview.Income)}");
            sb.AppendLine();
            AppendPopulationIncomeTaxBreakdown(sb, preview, state, compactValues: true);
            return sb.ToString();
        }

        private static void AppendPopulationIncomeTaxBreakdown(
            StringBuilder sb,
            EconomyPreviewResult preview,
            StateDataClass state,
            bool compactValues)
        {
            long employed = Math.Max(0L, state.EmployedWorkers);
            long unemployed = Math.Max(0L, state.UnemployedWorkers);
            long pensioners = Math.Max(0L, state.PensionersPopulation);
            long socialProtected = Math.Max(0L, state.SocialProtectedPopulation);
            long children = Math.Max(0L, state.ChildrenPopulation);

            decimal payrollGross = Math.Max(0m, preview.CitizenGrossIncome);
            decimal payrollNet = Math.Max(0m, preview.CitizenNetIncome);
            decimal payrollTaxes = Math.Max(
                0m,
                preview.WagesTax +
                preview.SocialContributionIncome +
                preview.AdditionalGrossIncomeTax +
                preview.AdditionalNetIncomeTax);

            decimal unemploymentSupport = Math.Max(0m, (decimal)state.UnemploymentSupportSpending);
            decimal pensionSpending = Math.Max(0m, (decimal)state.PensionSpending);
            decimal socialProtectedSpending = Math.Max(0m, (decimal)state.SocialProtectedSpending);
            decimal educationSpending = Math.Max(0m, (decimal)state.EducationSpending);

            string F(decimal value) => compactValues ? FmtNumberCompact(value) : FmtNumber(value);
            string PerPerson(decimal amount, long people)
            {
                if (people <= 0L)
                    return "0";
                decimal perPerson = amount / people;
                return compactValues ? FmtNumberCompact(perPerson, 2) : FmtNumber(perPerson, 2);
            }

            sb.AppendLine("👥 Групи населення (за день)");
            sb.AppendLine($"Працюючі ({FmtNumber(employed)}): брутто {F(payrollGross)} | податки {F(payrollTaxes)} | нетто {F(payrollNet)}");
            if (employed > 0L)
                sb.AppendLine($"  • На 1 працюючого: брутто {PerPerson(payrollGross, employed)} | податки {PerPerson(payrollTaxes, employed)} | нетто {PerPerson(payrollNet, employed)}");

            sb.AppendLine($"Безробітні ({FmtNumber(unemployed)}): допомога {F(unemploymentSupport)} | податки 0");
            if (unemployed > 0L)
                sb.AppendLine($"  • На 1 безробітного: виплата {PerPerson(unemploymentSupport, unemployed)}");

            sb.AppendLine($"Пенсіонери ({FmtNumber(pensioners)}): пенсії {F(pensionSpending)} | податки 0");
            if (pensioners > 0L)
                sb.AppendLine($"  • На 1 пенсіонера: виплата {PerPerson(pensionSpending, pensioners)}");

            sb.AppendLine($"Соцзахищені ({FmtNumber(socialProtected)}): соцзахист {F(socialProtectedSpending)} | податки 0");
            if (socialProtected > 0L)
                sb.AppendLine($"  • На 1 соцзахищеного: виплата {PerPerson(socialProtectedSpending, socialProtected)}");

            sb.AppendLine($"Діти ({FmtNumber(children)}): фінансування освіти {F(educationSpending)} | податки 0");
            if (children > 0L)
                sb.AppendLine($"  • На 1 дитину: бюджет освіти {PerPerson(educationSpending, children)}");

            sb.AppendLine("Примітка: податки на доходи нараховуються на зарплатний фонд працюючих.");
        }

        private static string BuildExpensesBreakdownText(EconomyPreviewResult preview)
        {
            decimal expensesTotal = preview.Expenses;
            decimal militaryBaseUpkeep = preview.MilitaryUpkeep - preview.MilitarySalarySpending - preview.MilitarySupplySpending - preview.StateOrderBudget;
            if (militaryBaseUpkeep < 0m)
                militaryBaseUpkeep = 0m;
            decimal componentsTotal =
                preview.Upkeep +
                preview.GovSalaries +
                preview.SocialSpending +
                preview.ScienceSpending +
                preview.FestivalCost +
                preview.MilitaryUpkeep +
                preview.InfrastructureSpending +
                preview.StabilizationReserveFunding +
                preview.CorruptionLoss +
                preview.DeputySalaryTotal +
                preview.DeputyTheft +
                preview.VassalFunding +
                preview.LawTreasuryDailyExpense;
            decimal residual = expensesTotal - componentsTotal;

            var sb = new StringBuilder();
            sb.AppendLine("ℹ Деталі: Витрати");
            sb.AppendLine("Період: 1 день");
            sb.AppendLine($"Бюджетні ліміти: інф {FmtNumberCompact(preview.BudgetInfrastructure)} / армія {FmtNumberCompact(preview.BudgetMilitary)} / наука {FmtNumberCompact(preview.BudgetScience)} / соц {FmtNumberCompact(preview.BudgetSocial)}");
            sb.AppendLine($"Утримання будівель: {FmtNumberCompact(preview.Upkeep)}");
            sb.AppendLine($"Зарплати держслужбовців: {FmtNumberCompact(preview.GovSalaries)}");
            sb.AppendLine($"Соціальні виплати: {FmtNumberCompact(preview.SocialSpending)}");
            sb.AppendLine($"Наука: {FmtNumberCompact(preview.ScienceSpending)}");
            sb.AppendLine($"Фестивалі: {FmtNumberCompact(preview.FestivalCost)}");
            sb.AppendLine($"Армія (разом): {FmtNumberCompact(preview.MilitaryUpkeep)}");
            sb.AppendLine($"  • утримання/зарплати/забезпечення/замовлення: {FmtNumberCompact(militaryBaseUpkeep)} / {FmtNumberCompact(preview.MilitarySalarySpending)} / {FmtNumberCompact(preview.MilitarySupplySpending)} / {FmtNumberCompact(preview.StateOrderBudget)}");
            sb.AppendLine($"Інфраструктура: {FmtNumberCompact(preview.InfrastructureSpending)}");
            sb.AppendLine($"Стабілізаційне фінансування: {FmtNumberCompact(preview.StabilizationReserveFunding)}");
            sb.AppendLine($"Корупційні втрати: {FmtNumberCompact(preview.CorruptionLoss)}");
            sb.AppendLine($"Парламент: {FmtNumberCompact(preview.DeputySalaryTotal)} / крадіжки: {FmtNumberCompact(preview.DeputyTheft)}");
            sb.AppendLine($"Утримання васалів: {FmtNumberCompact(preview.VassalFunding)}");
            sb.AppendLine($"Закони (щоденні витрати казни): {FmtNumberCompact(preview.LawTreasuryDailyExpense)}");
            if (Math.Abs(residual) >= 0.01m)
                sb.AppendLine($"Коригування витрат (війна/режими/глобальні ефекти): {FmtSignedDecimal(residual)}");
            sb.AppendLine($"Разом витрат: {FmtNumber(preview.Expenses)}");
            return sb.ToString();
        }

        private static string BuildStabilityBreakdownText(StateDataClass state, EconomyPreviewResult preview)
        {
            float unemploymentRate = EconomyCalculator.Clamp(preview.UnemploymentAfter / 100f, 0f, 1f);
            float taxRate = EconomyCalculator.Clamp(state.TaxLevel / 100f, 0f, 1f);

            float employmentStability = EconomyConfigData.GetFloat("EmploymentStability", 6f);
            float unemploymentStability = EconomyConfigData.GetFloat("UnemploymentStability", 8f);
            float taxStabilityPenalty = EconomyConfigData.GetFloat("TaxStabilityPenalty", 4f);
            float warStabilityPenalty = EconomyConfigData.GetFloat("WarStabilityPenalty", 6f);
            float deficitStabilityPenalty = EconomyConfigData.GetFloat("DeficitStabilityPenalty", 6f);

            float employmentBonus = (1f - unemploymentRate) * employmentStability;
            float unemploymentPenalty = -unemploymentRate * unemploymentStability;
            float taxPenalty = -taxRate * taxStabilityPenalty;
            float warPenalty = state.WarState ? -warStabilityPenalty : 0f;
            float deficitPenalty = state.Treasury < 0 ? -deficitStabilityPenalty : 0f;

            float knownDelta = employmentBonus + unemploymentPenalty + taxPenalty + warPenalty + deficitPenalty;
            float actualDelta = preview.StabilityAfter - preview.StabilityNow;
            float residualDelta = actualDelta - knownDelta;

            var sb = new StringBuilder();
            sb.AppendLine("ℹ Деталі: Стабільність");
            sb.AppendLine($"Базове значення: {FmtNumberCompact(preview.StabilityNow, 2)}");
            sb.AppendLine($"Зайнятість: {FmtSignedFloat(employmentBonus, 2)}");
            sb.AppendLine($"Безробіття ({FmtNumberCompact(unemploymentRate * 100f, 2)}%): {FmtSignedFloat(unemploymentPenalty, 2)}");
            sb.AppendLine($"Податкове навантаження ({FmtNumberCompact(taxRate * 100f, 2)}%): {FmtSignedFloat(taxPenalty, 2)}");
            sb.AppendLine($"Стан війни: {FmtSignedFloat(warPenalty, 2)}");
            sb.AppendLine($"Дефіцит казни: {FmtSignedFloat(deficitPenalty, 2)}");
            if (Math.Abs(residualDelta) >= 0.01f)
                sb.AppendLine($"Інші модифікатори (будівлі/фестивалі/закони/дипломатія): {FmtSignedFloat(residualDelta, 2)}");
            sb.AppendLine($"Підсумкова зміна: {FmtSignedFloat(actualDelta, 2)}");
            sb.AppendLine($"Стабільність після розрахунку: {FmtNumberCompact(preview.StabilityAfter, 2)}");
            return sb.ToString();
        }

        private static string BuildUnemploymentBreakdownText(StateDataClass state, EconomyPreviewResult preview, List<EconomyBuildingRecord> buildings)
        {
            long availableWorkers = state.AvailableWorkers > 0
                ? state.AvailableWorkers
                : Math.Max(0L, state.WorkingAgePopulation - state.ArmySize);
            int totalJobs = buildings.Sum(b => Math.Max(0, b.JobsProvided));
            decimal delta = (decimal)(preview.UnemploymentAfter - preview.UnemploymentNow);

            var sb = new StringBuilder();
            sb.AppendLine("ℹ Деталі: Безробіття");
            sb.AppendLine($"Працездатне населення: {FmtNumber(availableWorkers)}");
            sb.AppendLine($"Робочі місця: {FmtNumber(totalJobs)}");
            sb.AppendLine($"Було: {FmtNumberCompact(preview.UnemploymentNow, 2)}%");
            sb.AppendLine($"Стало: {FmtNumberCompact(preview.UnemploymentAfter, 2)}%");
            sb.AppendLine($"Зміна: {FmtSignedDecimal(delta, 2)} п.п.");
            return sb.ToString();
        }

        private async Task ShowEconomyLaborMarket(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var provider = new StateDataProvider();
            var state = provider.GetStateData(userId, selectedCountryId.Value);
            var buildings = EconomyBuildingsData.GetByCountry(selectedCountryId.Value);
            long availableWorkers = state.AvailableWorkers > 0
                ? state.AvailableWorkers
                : Math.Max(0L, state.WorkingAgePopulation - state.ArmySize);
            int totalJobs = buildings.Sum(b => Math.Max(0, b.JobsProvided));
            var distribution = EconomyServiceLaborDistribution(state, buildings, availableWorkers, totalJobs);

            var sb = new StringBuilder();
            sb.AppendLine($"👥 Ринок праці: {state.CountryName}");
            sb.AppendLine($"Робоча сила: {FmtNumber(availableWorkers)}");
            sb.AppendLine($"Робочі місця: {FmtNumber(totalJobs)}");
            sb.AppendLine();
            foreach (var e in distribution.Entries)
            {
                string label = e.Sector switch
                {
                    "Agriculture" => "С/г",
                    "Industry" => "Промисловість",
                    "Services" => "Послуги",
                    "Science" => "Наука",
                    "Government" => "Держсектор",
                    "Military" => "Військо",
                    "Unemployed" => "Безробітні",
                    _ => e.Sector
                };
                sb.AppendLine($"{label}: {e.Share * 100f:0.#}%");
            }

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.EconomyLaborKeyboard(),
                cancellationToken
            );
        }

        private async Task ShowEconomyCompaniesOverview(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var provider = new StateDataProvider();
            var state = provider.GetStateData(userId, selectedCountryId.Value);
            var buildings = EconomyBuildingsData.GetByCountry(selectedCountryId.Value);
            var companyBuildings = buildings.Where(IsCompanyBuilding).ToList();
            var registryCompanies = EconomyCompaniesData.GetByCountry(selectedCountryId.Value);
            var activeRegistryCompanies = registryCompanies.Where(c => c.IsActive).ToList();

            bool useRegistry = activeRegistryCompanies.Count > 0;
            int companiesCount = useRegistry ? activeRegistryCompanies.Count : companyBuildings.Count;
            long companyJobs = useRegistry
                ? activeRegistryCompanies.Sum(c => Math.Max(0, c.JobsOffered))
                : companyBuildings.Sum(c => Math.Max(0, c.JobsProvided));

            int stateOwnedCount = useRegistry
                ? activeRegistryCompanies.Count(c => c.IsStateCompany)
                : companyBuildings.Count(c => c.StateOwned);
            int privateOwnedCount = Math.Max(0, companiesCount - stateOwnedCount);

            decimal estimatedPayroll = useRegistry
                ? activeRegistryCompanies.Sum(c => Math.Max(0, c.BaseSalary) * Math.Max(0, c.JobsOffered))
                : Math.Max(0m, (decimal)state.CompanyAverageSalary) * Math.Max(0L, state.EmployedWorkers);

            decimal totalCompanyBudget = useRegistry
                ? activeRegistryCompanies.Sum(c => Math.Max(0m, c.Budget))
                : 0m;

            decimal weightedTax = 0m;
            long weightedTaxJobs = 0;
            if (useRegistry)
            {
                foreach (var company in activeRegistryCompanies)
                {
                    int jobs = Math.Max(0, company.JobsOffered);
                    if (jobs <= 0)
                        continue;
                    weightedTax += Math.Max(0m, company.TaxRate) * jobs;
                    weightedTaxJobs += jobs;
                }
            }

            decimal avgTaxRateDisplay = weightedTaxJobs > 0
                ? (weightedTax / weightedTaxJobs) * 100m
                : Math.Max(0m, (decimal)state.CompanyAverageTaxRate);

            var categoryStats = companyBuildings
                .GroupBy(c => FormatCompanyCategory(c.Category))
                .Select(g => new
                {
                    Category = g.Key,
                    Count = g.Count(),
                    Jobs = g.Sum(x => Math.Max(0, x.JobsProvided))
                })
                .OrderByDescending(x => x.Count)
                .ThenByDescending(x => x.Jobs)
                .ThenBy(x => x.Category)
                .ToList();

            var typeStats = useRegistry
                ? activeRegistryCompanies
                    .GroupBy(c => NormalizeProductionType(c.ProductionType))
                    .Select(g => new { Type = g.Key, Count = g.Count(), Jobs = g.Sum(x => Math.Max(0, x.JobsOffered)) })
                    .OrderByDescending(x => x.Count)
                    .ThenByDescending(x => x.Jobs)
                    .ThenBy(x => x.Type)
                    .Take(6)
                    .ToList()
                : companyBuildings
                    .GroupBy(c => string.IsNullOrWhiteSpace(c.Type) ? "Невідомий тип" : c.Type.Trim())
                    .Select(g => new { Type = g.Key, Count = g.Count(), Jobs = g.Sum(x => Math.Max(0, x.JobsProvided)) })
                    .OrderByDescending(x => x.Count)
                    .ThenByDescending(x => x.Jobs)
                    .ThenBy(x => x.Type)
                    .Take(6)
                    .ToList();

            var sb = new StringBuilder();
            sb.AppendLine($"🏭 Компанії: {state.CountryName}");
            sb.AppendLine($"Кількість компаній: {FmtNumber(companiesCount)}");
            sb.AppendLine($"Робочі місця компаній: {FmtNumber(companyJobs)}");
            sb.AppendLine($"Працюють / безробітні: {FmtNumber(state.EmployedWorkers)} / {FmtNumber(state.UnemployedWorkers)}");
            sb.AppendLine($"Середня зарплата: {state.CompanyAverageSalary:0.##}");
            sb.AppendLine($"Середній податок компаній: {avgTaxRateDisplay:0.##}%");
            sb.AppendLine($"Оцінка фонду зарплат: {FmtNumberCompact(estimatedPayroll)}");
            if (useRegistry)
                sb.AppendLine($"Сукупний бюджет компаній: {FmtNumberCompact(totalCompanyBudget)}");
            sb.AppendLine($"Держзамовлення: {FmtNumberCompact(state.StateOrderBudget)} (нових місць: {FmtNumber(state.StateOrderJobsCreated)})");
            sb.AppendLine($"Приватні / державні: {FmtNumber(privateOwnedCount)} / {FmtNumber(stateOwnedCount)}");
            sb.AppendLine($"Реєстр компаній: {(useRegistry ? "активний" : "дані з будівель")}");

            if (companiesCount == 0)
            {
                sb.AppendLine();
                sb.AppendLine("Компанії ще не створені.");
            }
            else
            {
                if (categoryStats.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("За категоріями:");
                    foreach (var item in categoryStats)
                        sb.AppendLine($"• {item.Category}: {FmtNumber(item.Count)} (місця {FmtNumber(item.Jobs)})");
                }

                if (typeStats.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("Топ типів компаній:");
                    foreach (var item in typeStats)
                        sb.AppendLine($"• {FormatProductionTypeUk(item.Type)}: {FmtNumber(item.Count)} (місця {FmtNumber(item.Jobs)})");
                }
            }

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.EconomyCompaniesKeyboard(),
                cancellationToken
            );
        }

        private async Task ShowEconomyMilitaryLogistics(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var provider = new StateDataProvider();
            var state = provider.GetStateData(userId, selectedCountryId.Value);
            var units = MilitaryUnitsData.GetByCountry(selectedCountryId.Value);
            var stock = MilitaryArsenalService.GetStockOverview(selectedCountryId.Value);

            int activeUnits = units.Count(u => string.Equals(u.Status, "Active", StringComparison.OrdinalIgnoreCase));
            int formingUnits = units.Count(u => string.Equals(u.Status, "Forming", StringComparison.OrdinalIgnoreCase));
            int reserveUnits = units.Count(u => string.Equals(u.Status, "Reserve", StringComparison.OrdinalIgnoreCase));
            int mobilizedUnits = units.Count(u => u.IsMobilized);
            decimal stockTotal = stock.Sum(s => s.Quantity);
            decimal stockPower = stock.Sum(s => s.PowerPoints);
            float avgFormationProgress = formingUnits > 0
                ? units
                    .Where(u => string.Equals(u.Status, "Forming", StringComparison.OrdinalIgnoreCase))
                    .Average(u => EconomyCalculator.Clamp(u.FormationProgress * 100f, 0f, 100f))
                : 100f;

            var risks = new List<string>();
            if (state.SupplyCoverage < 60f)
                risks.Add("⚠ Низьке забезпечення");
            if (state.AmmunitionIssued < 60f)
                risks.Add("⚠ Дефіцит амуніції");
            if (state.DesertionRisk > 25f)
                risks.Add("⚠ Високий ризик дезертирства");
            if (state.CrimeLevel > 40f)
                risks.Add("⚠ Підвищена злочинність");

            var sb = new StringBuilder();
            sb.AppendLine($"🚚 Логістика війська: {state.CountryName}");
            sb.AppendLine($"Армія: {FmtNumber(state.ArmySize)}");
            sb.AppendLine($"Підрозділи (активні/формуються/резерв): {FmtNumber(activeUnits)} / {FmtNumber(formingUnits)} / {FmtNumber(reserveUnits)}");
            sb.AppendLine($"Мобілізовані частини: {FmtNumber(mobilizedUnits)}");
            sb.AppendLine($"Арсенал: {FmtNumber(stock.Count)} типів / {FmtNumberCompact(stockTotal)} од. (сила {FmtNumberCompact(stockPower)})");
            if (formingUnits > 0)
                sb.AppendLine($"Середній прогрес формування: {avgFormationProgress:0.##}%");
            sb.AppendLine();
            sb.AppendLine("Витрати на військо:");
            sb.AppendLine($"• Утримання: {FmtNumberCompact(state.MilitaryUpkeep)}");
            sb.AppendLine($"• Зарплати: {FmtNumberCompact(state.MilitarySalarySpending)}");
            sb.AppendLine($"• Забезпечення: {FmtNumberCompact(state.MilitarySupplySpending)}");
            sb.AppendLine($"• Держзамовлення: {FmtNumberCompact(state.StateOrderBudget)}");
            sb.AppendLine();
            sb.AppendLine($"Забезпечення: {state.SupplyCoverage:0.##}%");
            sb.AppendLine($"Амуніція: {state.AmmunitionIssued:0.##}%");
            sb.AppendLine($"Якість зброї: {state.WeaponQuality:0.##}%");
            sb.AppendLine($"Боєздатність: {state.CombatEffectiveness:0.##}%");
            sb.AppendLine($"Час формування частин: x{state.FormationTimeMultiplier:0.##}");
            sb.AppendLine($"Тиск мобілізації: {state.MobilizationPressure:0.##}%");
            sb.AppendLine($"Ризик дезертирства: {state.DesertionRisk:0.##}%");
            sb.AppendLine($"Злочинність / поліція: {state.CrimeLevel:0.##}% / {state.PoliceEffectiveness:0.##}%");
            sb.AppendLine();
            sb.AppendLine("Командування:");
            sb.AppendLine($"• Навички: {state.CommanderSkill:0.##}%");
            sb.AppendLine($"• Лояльність: {state.CommanderLoyalty:0.##}%");
            sb.AppendLine($"• Корупція: {state.CommanderCorruption:0.##}%");
            sb.AppendLine($"• Досвід: {state.CommanderExperience:0.##}%");

            if (risks.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Ризики:");
                foreach (var risk in risks)
                    sb.AppendLine(risk);
            }

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.EconomyMilitaryLogisticsKeyboard(),
                cancellationToken
            );
        }

        private async Task ShowEconomyArmyStructure(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var provider = new StateDataProvider();
            var state = provider.GetStateData(userId, selectedCountryId.Value);
            var units = MilitaryUnitsData.GetByCountry(selectedCountryId.Value);

            string[] levelOrder = { "Division", "Brigade", "Regiment", "Battalion", "Company", "Platoon", "Squad" };
            var unitsByLevel = units
                .GroupBy(u => NormalizeUnitLevelKey(u.UnitLevel))
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            var unitsByType = units
                .GroupBy(u => FormatUnitTypeUk(u.UnitType))
                .Select(g => new { Type = g.Key, Count = g.Count(), Soldiers = g.Sum(x => Math.Max(0, x.Soldiers)) })
                .OrderByDescending(x => x.Count)
                .ThenByDescending(x => x.Soldiers)
                .Take(6)
                .ToList();

            var unitsByStatus = units
                .GroupBy(u => FormatUnitStatusUk(u.Status))
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            var commanders = units
                .Where(u => !string.IsNullOrWhiteSpace(u.CommanderName))
                .OrderByDescending(u => Math.Max(0, u.Soldiers))
                .ThenByDescending(u => u.CommanderSkill)
                .Take(5)
                .ToList();

            int totalSoldiersInUnits = units.Sum(u => Math.Max(0, u.Soldiers));

            var sb = new StringBuilder();
            sb.AppendLine($"🧱 Структура армії: {state.CountryName}");
            sb.AppendLine("Ієрархія: Відділення → Взвод → Рота → Батальйон → Полк → Бригада → Дивізія");
            sb.AppendLine($"Загалом частин: {FmtNumber(units.Count)}");
            sb.AppendLine($"Солдати в записаних частинах: {FmtNumber(totalSoldiersInUnits)}");
            sb.AppendLine($"Власна армія / васальні війська: {FmtNumber(state.ArmySize)} / {FmtNumber(state.VassalTroops)}");
            sb.AppendLine($"Васальні полки: {FmtNumber(state.VassalRegiments)}");

            sb.AppendLine();
            sb.AppendLine("Частини за рівнями:");
            if (units.Count == 0)
            {
                sb.AppendLine($"• Детальних записів немає. Оцінка: {state.UnitStructureText}");
            }
            else
            {
                foreach (var level in levelOrder)
                {
                    int count = unitsByLevel.TryGetValue(level, out int value) ? value : 0;
                    if (count > 0)
                        sb.AppendLine($"• {FormatUnitLevelUk(level)}: {FmtNumber(count)}");
                }
            }

            if (unitsByType.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("За типами:");
                foreach (var item in unitsByType)
                    sb.AppendLine($"• {item.Type}: {FmtNumber(item.Count)} (солдат {FmtNumber(item.Soldiers)})");
            }

            if (unitsByStatus.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("За статусами:");
                foreach (var item in unitsByStatus)
                    sb.AppendLine($"• {item.Status}: {FmtNumber(item.Count)}");
            }

            if (commanders.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Командири (топ):");
                foreach (var cmd in commanders)
                {
                    string name = string.IsNullOrWhiteSpace(cmd.CommanderName) ? "Невідомий" : cmd.CommanderName!;
                    sb.AppendLine($"• {name} ({FormatUnitLevelUk(cmd.UnitLevel)}): навички {cmd.CommanderSkill:0.#}% / лояльність {cmd.CommanderLoyalty:0.#}% / корупція {cmd.CommanderCorruption:0.#}%");
                }
            }

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.EconomyArmyStructureKeyboard(),
                cancellationToken
            );
        }

        private async Task ShowEconomyVassalsOverview(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var provider = new StateDataProvider();
            var state = provider.GetStateData(userId, selectedCountryId.Value);
            var obligations = VassalMilitaryObligationsData.GetByCountry(selectedCountryId.Value);
            var activeObligations = obligations.Where(o => o.IsActive).ToList();

            int activeCount = activeObligations.Count;
            int totalCount = obligations.Count;
            int regimentsFromObligations = activeObligations.Sum(o => Math.Max(0, o.RegimentsProvided));
            decimal fundingFromObligations = activeObligations.Sum(o => Math.Max(0m, o.FundingPerTurn));
            float avgLoyalty = activeCount == 0 ? 0f : activeObligations.Average(o => EconomyCalculator.Clamp(o.Loyalty, 0f, 100f));
            float avgRebellionRisk = activeCount == 0 ? 0f : activeObligations.Average(o => EconomyCalculator.Clamp(o.RebellionRisk, 0f, 100f));
            float maxRebellionRisk = activeCount == 0 ? 0f : activeObligations.Max(o => EconomyCalculator.Clamp(o.RebellionRisk, 0f, 100f));

            var list = obligations
                .OrderByDescending(o => o.IsActive)
                .ThenByDescending(o => o.RebellionRisk)
                .ThenBy(o => o.SubjectName)
                .Take(10)
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine($"🏰 Васали / колонії: {state.CountryName}");
            sb.AppendLine($"Зобов'язання (активні/всього): {FmtNumber(activeCount)} / {FmtNumber(totalCount)}");
            sb.AppendLine($"Полки від васалів: {FmtNumber(state.VassalRegiments)}");
            sb.AppendLine($"Орієнтовні війська: {FmtNumber(state.VassalTroops)}");
            sb.AppendLine($"Фінансування від васалів: {FmtNumberCompact(state.VassalFunding)}");
            sb.AppendLine($"Середня лояльність (активні): {avgLoyalty:0.##}%");
            sb.AppendLine($"Ризик повстання (середній/пік): {avgRebellionRisk:0.##}% / {maxRebellionRisk:0.##}%");

            if (activeCount > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Підсумок активних зобов'язань:");
                sb.AppendLine($"• Полки: {FmtNumber(regimentsFromObligations)}");
                sb.AppendLine($"• Фінансування: {FmtNumberCompact(fundingFromObligations)}");
            }

            if (list.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Суб'єкти:");
                foreach (var item in list)
                {
                    string marker = item.IsActive ? "✅" : "⏸";
                    string subject = string.IsNullOrWhiteSpace(item.SubjectName) ? $"#{item.ObligationId}" : item.SubjectName;
                    sb.AppendLine(
                        $"• {marker} {subject}: полки {FmtNumber(item.RegimentsProvided)}, фінанс. {FmtNumberCompact(item.FundingPerTurn)}, " +
                        $"лояльність {EconomyCalculator.Clamp(item.Loyalty, 0f, 100f):0.#}%, ризик {EconomyCalculator.Clamp(item.RebellionRisk, 0f, 100f):0.#}%");
                }

                if (obligations.Count > list.Count)
                    sb.AppendLine($"… ще {FmtNumber(obligations.Count - list.Count)} суб'єкт(ів).");
            }
            else
            {
                sb.AppendLine();
                sb.AppendLine("Васальних військових зобов'язань немає.");
            }

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.EconomyVassalsKeyboard(),
                cancellationToken
            );
        }

        private static bool IsCompanyBuilding(EconomyBuildingRecord building)
        {
            return string.Equals(building.Category, "Production", StringComparison.OrdinalIgnoreCase)
                || string.Equals(building.Category, "Military", StringComparison.OrdinalIgnoreCase);
        }

        private static string FormatCompanyCategory(string? category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return "Інше";

            return category.Trim().ToLowerInvariant() switch
            {
                "production" => "Виробництво",
                "military" => "Військова промисловість",
                "science" => "Наука",
                "administration" => "Адміністрація",
                "housing" => "Житло",
                "culture" => "Культура",
                _ => category.Trim()
            };
        }

        private static string NormalizeUnitLevelKey(string? level)
        {
            if (string.IsNullOrWhiteSpace(level))
                return "Squad";

            string value = level.Trim().ToLowerInvariant();
            return value switch
            {
                "division" or "дивізія" => "Division",
                "brigade" or "бригада" => "Brigade",
                "regiment" or "полк" => "Regiment",
                "battalion" or "батальйон" => "Battalion",
                "company" or "рота" => "Company",
                "platoon" or "взвод" => "Platoon",
                "squad" or "відділення" => "Squad",
                "legion" or "легіон" => "Legion",
                "garrison" or "гарнізон" => "Garrison",
                "fleet" or "флот" => "Fleet",
                _ => level.Trim()
            };
        }

        private static string FormatUnitLevelUk(string? level)
        {
            if (string.IsNullOrWhiteSpace(level))
                return "Відділення";

            string value = level.Trim().ToLowerInvariant();
            return value switch
            {
                "division" => "Дивізія",
                "brigade" => "Бригада",
                "regiment" => "Полк",
                "battalion" => "Батальйон",
                "company" => "Рота",
                "platoon" => "Взвод",
                "squad" => "Відділення",
                "legion" => "Легіон",
                "garrison" => "Гарнізон",
                "fleet" => "Флот",
                _ => level.Trim()
            };
        }

        private static string NormalizeUnitRole(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "Field";

            string value = raw.Trim().ToLowerInvariant();
            return value switch
            {
                "field" or "польова" => "Field",
                "garrison" or "гарнізон" => "Garrison",
                "vanguard" or "авангард" => "Vanguard",
                "reserve" or "резерв" => "Reserve",
                "siegetrain" or "облогова" => "SiegeTrain",
                "fleet" or "флот" => "Fleet",
                _ => raw.Trim()
            };
        }

        private static string FormatUnitRoleUk(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return "Польова";

            string value = role.Trim().ToLowerInvariant();
            return value switch
            {
                "field" => "Польова",
                "garrison" => "Гарнізон",
                "vanguard" => "Авангард",
                "reserve" => "Резерв",
                "siegetrain" => "Облогова",
                "fleet" => "Флотська",
                _ => role.Trim()
            };
        }

        private static string NormalizeEquipmentTier(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "Standard";

            string value = raw.Trim().ToLowerInvariant();
            return value switch
            {
                "militia" or "ополчення" => "Militia",
                "standard" or "стандарт" => "Standard",
                "heavy" or "важка" => "Heavy",
                "elite" or "елітна" => "Elite",
                "arcane" or "арканна" or "магична" => "Arcane",
                "naval" or "морська" => "Naval",
                _ => raw.Trim()
            };
        }

        private static string FormatEquipmentTierUk(string? tier)
        {
            if (string.IsNullOrWhiteSpace(tier))
                return "Стандарт";

            string value = tier.Trim().ToLowerInvariant();
            return value switch
            {
                "militia" => "Ополчення",
                "standard" => "Стандарт",
                "heavy" => "Важка",
                "elite" => "Елітна",
                "arcane" => "Арканна",
                "naval" => "Морська",
                _ => tier.Trim()
            };
        }

        private static string FormatUnitTypeUk(string? unitType)
        {
            if (string.IsNullOrWhiteSpace(unitType))
                return "Піхота";

            string value = unitType.Trim().ToLowerInvariant();
            return value switch
            {
                "infantry" or "піхота" => "Піхота",
                "spearmen" => "Копійники",
                "archers" => "Лучники",
                "crossbowmen" => "Арбалетники",
                "cavalry" => "Кавалерія",
                "heavyinfantry" => "Важка піхота",
                "magecorps" => "Маги",
                "siege" => "Облогові",
                "scouts" => "Розвідники",
                "navalfleet" => "Флот",
                "support" or "підтримка" => "Підтримка",
                _ => unitType.Trim()
            };
        }

        private static string NormalizeUnitRace(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "Human";

            string value = raw.Trim().ToLowerInvariant();
            return value switch
            {
                "human" or "humans" or "людина" or "люди" => "Human",
                "elf" or "elves" or "ельф" or "ельфи" => "Elf",
                "tiefling" or "tieflings" or "тіфлінг" or "тіфлінги" or "тифлінг" or "тифлінги" => "Tiefling",
                "mushroom" or "mushrooms" or "fungus" or "fungi" or "гриб" or "гриби" => "Mushroomfolk",
                "dwarf" or "dwarves" or "дварф" or "дворф" or "дворфи" => "Dwarf",
                "orc" or "orcs" or "орк" or "орки" => "Orc",
                "gnome" or "gnomes" or "гном" or "гноми" => "Gnome",
                "halfling" or "halflings" or "халфлінг" or "халфлінги" => "Halfling",
                _ => raw.Trim()
            };
        }

        private static string FormatUnitRaceUk(string? race)
        {
            if (string.IsNullOrWhiteSpace(race))
                return "Люди";

            string value = race.Trim().ToLowerInvariant();
            return value switch
            {
                "human" => "Люди",
                "elf" => "Ельфи",
                "tiefling" => "Тіфлінги",
                "mushroomfolk" => "Гриби",
                "dwarf" => "Дворфи",
                "orc" => "Орки",
                "gnome" => "Гноми",
                "halfling" => "Халфлінги",
                _ => race.Trim()
            };
        }

        private static string FormatUnitStatusUk(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return "Формується";

            string value = status.Trim().ToLowerInvariant();
            return value switch
            {
                "forming" => "Формується",
                "active" => "Активна",
                "reserve" => "Резерв",
                "routed" => "Розбита",
                "destroyed" => "Знищена",
                _ => status.Trim()
            };
        }

        private static SectorDistribution EconomyServiceLaborDistribution(StateDataClass state, List<EconomyBuildingRecord> buildings, long availableWorkers, int totalJobs)
        {
            int agricultureJobs = 0;
            int industryJobs = 0;
            int servicesJobs = 0;
            int scienceJobs = 0;
            int governmentJobs = 0;
            int militaryJobs = Math.Max(0, state.ArmySize);

            foreach (var b in buildings)
            {
                int jobs = Math.Max(0, b.JobsProvided);
                string category = b.Category ?? string.Empty;
                if (category.Equals("Science", StringComparison.OrdinalIgnoreCase))
                    scienceJobs += jobs;
                else if (category.Equals("Administration", StringComparison.OrdinalIgnoreCase))
                    governmentJobs += jobs;
                else if (category.Equals("Military", StringComparison.OrdinalIgnoreCase))
                    militaryJobs += jobs;
                else if (category.Equals("Culture", StringComparison.OrdinalIgnoreCase) || category.Equals("Other", StringComparison.OrdinalIgnoreCase) || category.Equals("Housing", StringComparison.OrdinalIgnoreCase))
                    servicesJobs += jobs;
                else if (category.Equals("Production", StringComparison.OrdinalIgnoreCase))
                    agricultureJobs += string.Equals(b.ResourceType, "Food", StringComparison.OrdinalIgnoreCase) ? jobs : 0;
                else
                    servicesJobs += jobs;

                if (category.Equals("Production", StringComparison.OrdinalIgnoreCase) && !string.Equals(b.ResourceType, "Food", StringComparison.OrdinalIgnoreCase))
                    industryJobs += jobs;
            }

            int totalJobsSafe = Math.Max(1, totalJobs);
            float fillRatio = Math.Min(1f, availableWorkers / (float)totalJobsSafe);

            long agriculture = (long)Math.Round(agricultureJobs * fillRatio);
            long industry = (long)Math.Round(industryJobs * fillRatio);
            long services = (long)Math.Round(servicesJobs * fillRatio);
            long science = (long)Math.Round(scienceJobs * fillRatio);
            long government = (long)Math.Round(governmentJobs * fillRatio);
            long military = (long)Math.Round(militaryJobs * fillRatio);

            long employed = agriculture + industry + services + science + government + military;
            long unemployed = Math.Max(0L, availableWorkers - employed);

            long denom = Math.Max(1L, availableWorkers);
            return new SectorDistribution
            {
                AvailableWorkers = availableWorkers,
                TotalJobs = totalJobs,
                Unemployed = unemployed,
                Entries = new List<SectorDistributionEntry>
                {
                    new SectorDistributionEntry { Sector = "Agriculture", Workers = agriculture, Share = agriculture / (float)denom },
                    new SectorDistributionEntry { Sector = "Industry", Workers = industry, Share = industry / (float)denom },
                    new SectorDistributionEntry { Sector = "Services", Workers = services, Share = services / (float)denom },
                    new SectorDistributionEntry { Sector = "Science", Workers = science, Share = science / (float)denom },
                    new SectorDistributionEntry { Sector = "Government", Workers = government, Share = government / (float)denom },
                    new SectorDistributionEntry { Sector = "Military", Workers = military, Share = military / (float)denom },
                    new SectorDistributionEntry { Sector = "Unemployed", Workers = unemployed, Share = unemployed / (float)denom }
                }
            };
        }

        private async Task ShowCrisisActions(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            if (country == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Країну не знайдено.",
                    StateKeyboardBuilder.EconomyWarningsKeyboard(),
                    cancellationToken
                );
                return;
            }

            var actions = BuildCrisisActions(country);
            var rows = new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[]>();

            var sb = new StringBuilder();
            sb.AppendLine("🧯 Антикризові дії");
            sb.AppendLine($"Казна: {FmtNumber(country.Treasury)}");
            sb.AppendLine();

            foreach (var a in actions)
            {
                int lastTurn = EconomyCrisisActionsData.GetLastAppliedTurn(country.CountryId, a.ActionType.ToString());
                bool onCooldown = country.TurnsSurvived - lastTurn < a.CooldownTurns;
                decimal cost = a.Cost;
                bool canPay = cost <= 0 || country.Treasury >= (long)Math.Round(cost);
                string title = onCooldown ? $"⏳ {a.Name}" : a.Name;
                string data = onCooldown ? "state_noop" : $"state_economy_crisis_action_{a.ActionType}";
                rows.Add(new[] { Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(title, data) });

                string costLabel = cost < 0 ? $"+{FmtNumber(Math.Abs(cost))}" : $"{FmtNumber(cost)}";
                sb.AppendLine($"• {a.Name}: вартість {costLabel}, cooldown {FmtNumber(a.CooldownTurns)} дн.");
                foreach (var eff in a.ImmediateEffects)
                    sb.AppendLine($"  {eff}");
                if (!canPay)
                    sb.AppendLine("  ⚠ Недостатньо коштів");
            }

            rows.Add(new[] { Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("⬅ Назад", "state_economy") });

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(rows),
                cancellationToken
            );
        }

        private async Task ApplyCrisisAction(ITelegramBotClient botClient, long userId, long chatId, int msgId, string actionKey, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            if (country == null)
                return;

            if (!Enum.TryParse<CrisisActionType>(actionKey, true, out var actionType))
                return;

            var action = BuildCrisisActions(country).FirstOrDefault(a => a.ActionType == actionType);
            if (action == null)
                return;

            int lastTurn = EconomyCrisisActionsData.GetLastAppliedTurn(country.CountryId, action.ActionType.ToString());
            if (country.TurnsSurvived - lastTurn < action.CooldownTurns)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Дія на кулдауні.",
                    StateKeyboardBuilder.EconomyWarningsKeyboard(),
                    cancellationToken
                );
                return;
            }

            if (action.Cost > 0 && country.Treasury < (long)Math.Round(action.Cost))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Недостатньо коштів.",
                    StateKeyboardBuilder.EconomyWarningsKeyboard(),
                    cancellationToken
                );
                return;
            }

            ApplyCrisisActionEffects(country, action);
            EconomyCrisisActionsData.UpsertLastApplied(country.CountryId, action.ActionType.ToString(), country.TurnsSurvived);
            CountriesData.UpdateCountry(country);

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                $"✅ Дію застосовано: {action.Name}",
                StateKeyboardBuilder.EconomyWarningsKeyboard(),
                cancellationToken
            );
        }

        private static List<CrisisAction> BuildCrisisActions(CountryRecord country)
        {
            var actions = new List<CrisisAction>();

            actions.Add(new CrisisAction
            {
                ActionType = CrisisActionType.SpendingCuts,
                Name = "Скорочення витрат",
                Cost = 0m,
                CooldownTurns = 3,
                ImmediateEffects = new List<string> { "↓ витрати бюджету", "↓ стабільність" }
            });

            actions.Add(new CrisisAction
            {
                ActionType = CrisisActionType.RaiseTaxes,
                Name = "Підвищити податки",
                Cost = 0m,
                CooldownTurns = 3,
                ImmediateEffects = new List<string> { "↑ доходи", "↓ стабільність" }
            });

            decimal subsidiesCost = Math.Max(100m, Math.Abs(country.Treasury) * 0.05m);
            actions.Add(new CrisisAction
            {
                ActionType = CrisisActionType.EmergencySubsidies,
                Name = "Екстрені дотації",
                Cost = subsidiesCost,
                CooldownTurns = 4,
                ImmediateEffects = new List<string> { "↑ стабільність" }
            });

            decimal purgeCost = Math.Max(50m, Math.Abs(country.Treasury) * 0.03m);
            actions.Add(new CrisisAction
            {
                ActionType = CrisisActionType.AntiCorruptionPurge,
                Name = "Антикорупційна чистка",
                Cost = purgeCost,
                CooldownTurns = 5,
                ImmediateEffects = new List<string> { "↓ корупція", "↓ стабільність" }
            });

            decimal printGain = Math.Max(200m, Math.Abs(country.Treasury) * 0.1m);
            actions.Add(new CrisisAction
            {
                ActionType = CrisisActionType.PrintMoney,
                Name = "Друк грошей",
                Cost = -printGain,
                CooldownTurns = 6,
                ImmediateEffects = new List<string> { "↑ казна", "↑ інфляція", "↓ стабільність" }
            });

            return actions;
        }

        private static void ApplyCrisisActionEffects(CountryRecord country, CrisisAction action)
        {
            switch (action.ActionType)
            {
                case CrisisActionType.SpendingCuts:
                    country.MilitaryUpkeep = Math.Max(0m, country.MilitaryUpkeep * 0.9m);
                    country.Stability = Math.Max(0f, country.Stability - 3f);
                    break;
                case CrisisActionType.RaiseTaxes:
                    country.TaxLevel = Math.Min(100, country.TaxLevel + 5);
                    country.Stability = Math.Max(0f, country.Stability - 4f);
                    break;
                case CrisisActionType.EmergencySubsidies:
                    country.Treasury -= (long)Math.Round(action.Cost);
                    country.Stability = Math.Min(100f, country.Stability + 5f);
                    break;
                case CrisisActionType.AntiCorruptionPurge:
                    country.Treasury -= (long)Math.Round(action.Cost);
                    country.Corruption = Math.Max(0f, country.Corruption - 5f);
                    country.Stability = Math.Max(0f, country.Stability - 2f);
                    break;
                case CrisisActionType.PrintMoney:
                    country.Treasury += (long)Math.Round(Math.Abs(action.Cost));
                    country.Inflation = country.Inflation + 5f;
                    country.Stability = Math.Max(0f, country.Stability - 2f);
                    break;
            }
        }

        private async Task ShowEconomyResourcesFlowReport(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var provider = new StateDataProvider();
            var state = provider.GetStateData(userId, selectedCountryId.Value);
            if (state == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Країну не знайдено.",
                    StateKeyboardBuilder.EconomyMenuKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }
            var buildings = EconomyBuildingsData.GetByCountry(selectedCountryId.Value);
            var trade = EconomyTradeAgreementsData.GetActiveByCountry(selectedCountryId.Value);

            var productionNow = ComputeResourceProductionNow(state, buildings);
            var production = productionNow.NetByResource;
            float efficiencyPct = productionNow.EfficiencyPct;

            float sanctionsModifier = EconomyCalculator.ApplySanctionsModifier(
                (SanctionsLevel)state.SanctionsLevel,
                EconomyConfigData.GetFloat("SanctionsNoneModifier", 1f),
                EconomyConfigData.GetFloat("SanctionsPartialModifier", 0.9f),
                EconomyConfigData.GetFloat("SanctionsFullModifier", 0.7f)
            );
            float warTradeModifier = state.WarState ? EconomyConfigData.GetFloat("WarTradeModifier", 0.7f) : 1f;
            var partnerSanctions = EconomyCountrySanctionsData.GetBySource(selectedCountryId.Value)
                .ToDictionary(s => s.TargetCountryId, s => s);

            var importTotals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var exportTotals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            foreach (var a in trade)
            {
                float exportPartnerModifier = 1f;
                float importPartnerModifier = 1f;
                if (a.PartnerCountryId.HasValue && partnerSanctions.TryGetValue(a.PartnerCountryId.Value, out var sanction))
                {
                    exportPartnerModifier = sanction.ExportLevel == (int)SanctionsLevel.Embargo
                        ? 0f
                        : EconomyCalculator.ApplyPercentModifier(sanction.ExportEffectiveness);

                    importPartnerModifier = sanction.ImportLevel == (int)SanctionsLevel.Embargo
                        ? 0f
                        : EconomyCalculator.ApplyPercentModifier(sanction.ImportEffectiveness);
                }

                decimal exportQtyModifier = (decimal)sanctionsModifier * (decimal)warTradeModifier * (decimal)exportPartnerModifier;
                decimal importQtyModifier = (decimal)sanctionsModifier * (decimal)warTradeModifier * (decimal)importPartnerModifier;
                decimal exportPerDayQty = GetTradeLegPerDayQty(a, isExport: true);
                decimal importPerDayQty = GetTradeLegPerDayQty(a, isExport: false);

                if (!string.IsNullOrWhiteSpace(a.ExportResourceType) && exportPerDayQty > 0m)
                {
                    decimal exportQty = exportPerDayQty * exportQtyModifier;
                    if (exportTotals.TryGetValue(a.ExportResourceType, out var current))
                        exportTotals[a.ExportResourceType] = current + exportQty;
                    else
                        exportTotals[a.ExportResourceType] = exportQty;
                }

                if (!string.IsNullOrWhiteSpace(a.ImportResourceType) && importPerDayQty > 0m)
                {
                    decimal importQty = importPerDayQty * importQtyModifier;
                    if (importTotals.TryGetValue(a.ImportResourceType, out var current))
                        importTotals[a.ImportResourceType] = current + importQty;
                    else
                        importTotals[a.ImportResourceType] = importQty;
                }
            }

            var allKeys = production.Keys
                .Union(productionNow.ConsumedByResource.Keys, StringComparer.OrdinalIgnoreCase)
                .Union(importTotals.Keys, StringComparer.OrdinalIgnoreCase)
                .Union(exportTotals.Keys, StringComparer.OrdinalIgnoreCase)
                .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                .ToList();

            string text;
            if (allKeys.Count == 0)
            {
                text = "📦 Рух ресурсів: даних ще немає.";
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine($"📦 Рух ресурсів: {state.CountryName}");
                sb.AppendLine($"Ефективність виробництва: {efficiencyPct:0.##}%");
                sb.AppendLine("Факт. виробництво = база × ефективність × стан будівель.");
                sb.AppendLine();
                foreach (var key in allKeys)
                {
                    production.TryGetValue(key, out var net);
                    productionNow.ProducedByResource.TryGetValue(key, out var grossProduced);
                    productionNow.ConsumedByResource.TryGetValue(key, out var internalConsumed);
                    importTotals.TryGetValue(key, out var imp);
                    exportTotals.TryGetValue(key, out var exp);
                    decimal baseSum = buildings
                        .Where(b => HasResourceOutputBuilding(b))
                        .Where(b => !string.IsNullOrWhiteSpace(b.ResourceType))
                        .Where(b => string.Equals(b.ResourceType, key, StringComparison.OrdinalIgnoreCase))
                        .Sum(b => b.BaseProduction);
                    decimal available = net + imp - exp;
                    if (internalConsumed > 0m)
                    {
                        sb.AppendLine($"• {FormatResourceTypeUk(key)}: доступно {FmtNumberCompact(available)} (виробн {FmtNumberCompact(grossProduced)}, техспож {FmtNumberCompact(internalConsumed)}, нетто {FmtNumberCompact(net)}, база {FmtNumberCompact(baseSum)}, імп {FmtNumberCompact(imp)}, експ {FmtNumberCompact(exp)})");
                    }
                    else
                    {
                        sb.AppendLine($"• {FormatResourceTypeUk(key)}: доступно {FmtNumberCompact(available)} (виробн {FmtNumberCompact(net)}, база {FmtNumberCompact(baseSum)}, імп {FmtNumberCompact(imp)}, експ {FmtNumberCompact(exp)})");
                    }
                }
                sb.AppendLine();
                sb.AppendLine("Доступно = (виробн - внутр. споживання) + імпорт - експорт.");
                text = sb.ToString();
            }

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.EconomyMenuKeyboard(userId == masterID),
                cancellationToken
            );
        }

        private async Task ShowEconomySanctionsMenu(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            if (country == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Країну не знайдено.",
                    StateKeyboardBuilder.EconomyMenuKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }

            float none = EconomyConfigData.GetFloat("SanctionsNoneModifier", 1f);
            float partial = EconomyConfigData.GetFloat("SanctionsPartialModifier", 0.9f);
            float full = EconomyConfigData.GetFloat("SanctionsFullModifier", 0.7f);
            var imposed = EconomyCountrySanctionsData.GetBySource(selectedCountryId.Value);
            int imposedActive = imposed.Count(s =>
                s.ExportLevel == (int)SanctionsLevel.Embargo ||
                s.ImportLevel == (int)SanctionsLevel.Embargo ||
                s.ExportEffectiveness > 0f ||
                s.ImportEffectiveness > 0f);

            string text =
$@"🛡 Протекціонізм (ліміти торгівлі)
Країна: {country.Name}
Зовнішній режим: {FormatSanctionsLevel(country.SanctionsLevel)}
Активних країн з лімітами: {imposedActive}
";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.EconomySanctionsKeyboard(country.SanctionsLevel),
                cancellationToken
            );
        }

        private async Task SetEconomySanctionsLevel(ITelegramBotClient botClient, long userId, long chatId, int msgId, int level, CancellationToken cancellationToken)
        {
            if (level != 0 && level != (int)SanctionsLevel.Embargo)
                return;

            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            if (country == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Країну не знайдено.",
                    StateKeyboardBuilder.EconomyMenuKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }

            country.SanctionsLevel = level;
            CountriesData.UpdateCountry(country);

            await ShowEconomySanctionsMenu(botClient, userId, chatId, msgId, cancellationToken);
        }

        private async Task ShowEconomySanctionsTargets(ITelegramBotClient botClient, long userId, long chatId, int msgId, int page, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var countries = CountriesData.GetAllCountries()
                .Where(c => c.CountryId != selectedCountryId.Value)
                .ToList();

            int perPage = 10;
            if (page < 0) page = 0;
            int totalPages = (int)Math.Ceiling(countries.Count / (double)perPage);
            if (totalPages == 0) totalPages = 1;
            if (page >= totalPages) page = totalPages - 1;

            var pageItems = countries.Skip(page * perPage).Take(perPage).ToList();
            var sanctions = EconomyCountrySanctionsData.GetBySource(selectedCountryId.Value)
                .Where(s =>
                    s.ExportLevel == (int)SanctionsLevel.Embargo ||
                    s.ImportLevel == (int)SanctionsLevel.Embargo ||
                    s.ExportEffectiveness > 0f ||
                    s.ImportEffectiveness > 0f)
                .ToDictionary(s => s.TargetCountryId, s => s);

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                "🌍 Ліміти торгівлі по країнах: оберіть ціль",
                StateKeyboardBuilder.EconomySanctionsTargetsKeyboard(pageItems, sanctions, page, totalPages),
                cancellationToken
            );
        }

        private async Task ShowEconomySanctionsTargetInfo(ITelegramBotClient botClient, long userId, long chatId, int msgId, int targetCountryId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            if (selectedCountryId.Value == targetCountryId)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не можна встановлювати ліміти на свою країну.",
                    StateKeyboardBuilder.EconomySanctionsTargetsKeyboard(new List<CountryRecord>(), new Dictionary<int, EconomyCountrySanctionRecord>(), 0, 1),
                    cancellationToken
                );
                return;
            }

            var target = CountriesData.GetCountryById(targetCountryId);
            if (target == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Країну не знайдено.",
                    StateKeyboardBuilder.EconomySanctionsTargetsKeyboard(new List<CountryRecord>(), new Dictionary<int, EconomyCountrySanctionRecord>(), 0, 1),
                    cancellationToken
                );
                return;
            }

            var record = EconomyCountrySanctionsData.GetBySourceTarget(selectedCountryId.Value, targetCountryId);
            int exportLevel = record?.ExportLevel ?? 0;
            int importLevel = record?.ImportLevel ?? 0;
            float exportEff = record?.ExportEffectiveness ?? 1f;
            float importEff = record?.ImportEffectiveness ?? 1f;

            float exportImpact = exportLevel == (int)SanctionsLevel.Embargo
                ? 100f
                : EconomyCalculator.Clamp(exportEff, 0f, 1f) * 100f;
            float importImpact = importLevel == (int)SanctionsLevel.Embargo
                ? 100f
                : EconomyCalculator.Clamp(importEff, 0f, 1f) * 100f;

            _logger.LogInformation(
                "Sanctions target info: user={UserId} target={TargetId} exportLevel={ExportLevel} importLevel={ImportLevel} exportEff={ExportEff} importEff={ImportEff} exportImpact={ExportImpact} importImpact={ImportImpact}",
                userId, targetCountryId, exportLevel, importLevel, exportEff, importEff, exportImpact, importImpact);

            string text =
$@"🌍 Ліміти торгівлі по країні
Країна: {target.Name}
Експорт (всі товари): {(exportLevel == (int)SanctionsLevel.Embargo ? "ЕМБАРГО" : $"-{exportImpact:0.#}%")} (ефект {exportEff * 100f:0.#}%)
Імпорт (всі товари): {(importLevel == (int)SanctionsLevel.Embargo ? "ЕМБАРГО" : $"-{importImpact:0.#}%")} (ефект {importEff * 100f:0.#}%)

Пояснення:
• Ембарго блокує торгівлю повністю.
• Ефект = зниження обсягу (0–99%), 100% відповідає ембарго.";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.EconomySanctionsTargetKeyboard(targetCountryId, exportLevel, importLevel, exportEff, importEff),
                cancellationToken
            );
        }

        private async Task SetEconomySanctionsTargetLevel(ITelegramBotClient botClient, long userId, long chatId, int msgId, int targetCountryId, int level, bool isExport, CancellationToken cancellationToken)
        {
            if (level != 0 && level != (int)SanctionsLevel.Embargo)
                return;

            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            if (selectedCountryId.Value == targetCountryId)
                return;

            var existing = EconomyCountrySanctionsData.GetBySourceTarget(selectedCountryId.Value, targetCountryId);
            int exportLevel = existing?.ExportLevel ?? 0;
            int importLevel = existing?.ImportLevel ?? 0;
            float exportEff = existing?.ExportEffectiveness ?? 1f;
            float importEff = existing?.ImportEffectiveness ?? 1f;

            _logger.LogInformation("Sanctions target level set: user={UserId} target={TargetId} isExport={IsExport} level={Level} beforeExport={BeforeExport} beforeImport={BeforeImport} exportEff={ExportEff} importEff={ImportEff}",
                userId, targetCountryId, isExport, level, exportLevel, importLevel, exportEff, importEff);

            if (isExport)
            {
                exportLevel = level;
                if (level == (int)SanctionsLevel.Embargo)
                    exportEff = 1f;
            }
            else
            {
                importLevel = level;
                if (level == (int)SanctionsLevel.Embargo)
                    importEff = 1f;
            }

            if (exportLevel == 0 && importLevel == 0 && exportEff <= 0f && importEff <= 0f)
            {
                EconomyCountrySanctionsData.Delete(selectedCountryId.Value, targetCountryId);
            }
            else
            {
                EconomyCountrySanctionsData.Upsert(selectedCountryId.Value, targetCountryId, exportLevel, importLevel, exportEff, importEff);
            }

            await ShowEconomySanctionsTargetInfo(botClient, userId, chatId, msgId, targetCountryId, cancellationToken);
        }

        private async Task SetEconomySanctionsTargetEfficiency(ITelegramBotClient botClient, long userId, long chatId, int msgId, int targetCountryId, float effPct, bool isExport, CancellationToken cancellationToken)
        {
            if (effPct < 0) effPct = 0;
            if (effPct >= 100)
            {
                await SetEconomySanctionsTargetLevel(botClient, userId, chatId, msgId, targetCountryId, (int)SanctionsLevel.Embargo, isExport, cancellationToken);
                return;
            }
            if (effPct > 99) effPct = 99;

            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            if (selectedCountryId.Value == targetCountryId)
                return;

            var existing = EconomyCountrySanctionsData.GetBySourceTarget(selectedCountryId.Value, targetCountryId);
            int exportLevel = existing?.ExportLevel ?? 0;
            int importLevel = existing?.ImportLevel ?? 0;
            float exportEff = existing?.ExportEffectiveness ?? 1f;
            float importEff = existing?.ImportEffectiveness ?? 1f;

            float eff = effPct / 100f;
            if (isExport)
                exportEff = eff;
            else
                importEff = eff;

            if (isExport)
                exportLevel = 0;
            else
                importLevel = 0;

            _logger.LogInformation("Sanctions target efficiency set: user={UserId} target={TargetId} isExport={IsExport} effPct={EffPct} exportLevel={ExportLevel} importLevel={ImportLevel} exportEff={ExportEff} importEff={ImportEff}",
                userId, targetCountryId, isExport, effPct, exportLevel, importLevel, exportEff, importEff);

            EconomyCountrySanctionsData.Upsert(selectedCountryId.Value, targetCountryId, exportLevel, importLevel, exportEff, importEff);

            await ShowEconomySanctionsTargetInfo(botClient, userId, chatId, msgId, targetCountryId, cancellationToken);
        }

        private async Task StartEconomySanctionsEfficiencyInput(ITelegramBotClient botClient, long userId, long chatId, int msgId, int targetCountryId, bool isExport, CancellationToken cancellationToken)
        {
            EconomyInputState[userId] = EconomyInputMode.SanctionsEfficiency;
            EconomySanctionsEdit[userId] = new SanctionsEfficiencyContext
            {
                TargetCountryId = targetCountryId,
                IsExport = isExport
            };
            EconomySanctionsUiContext[userId] = (chatId, msgId);

            _logger.LogInformation("Sanctions target efficiency input started: user={UserId} target={TargetId} isExport={IsExport} chatId={ChatId} msgId={MsgId}",
                userId, targetCountryId, isExport, chatId, msgId);

            string prompt = isExport
                ? "Вкажіть ліміт експорту у % (0-99):"
                : "Вкажіть ліміт імпорту у % (0-99):";
            await SendHelperMessageAsync(botClient, userId, prompt, cancellationToken);
        }

        private async Task ShowEconomySanctionsProducts(ITelegramBotClient botClient, long userId, long chatId, int msgId, int page, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var productSanctions = EconomyCountryProductSanctionsData.GetBySource(selectedCountryId.Value);
            var targetsWithSanctions = new HashSet<int>(
                productSanctions
                    .Where(s => s.ExportLevel == (int)SanctionsLevel.Embargo
                        || s.ImportLevel == (int)SanctionsLevel.Embargo
                        || s.ExportEffectiveness > 0f
                        || s.ImportEffectiveness > 0f)
                    .Select(s => s.TargetCountryId)
            );

            var countries = CountriesData.GetAllCountries()
                .Where(c => c.CountryId != selectedCountryId.Value)
                .ToList();

            int perPage = 10;
            if (page < 0) page = 0;
            int totalPages = (int)Math.Ceiling(countries.Count / (double)perPage);
            if (totalPages == 0) totalPages = 1;
            if (page >= totalPages) page = totalPages - 1;

            var pageItems = countries.Skip(page * perPage).Take(perPage).ToList();

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                "📦 Ліміти на товари: оберіть країну",
                StateKeyboardBuilder.EconomySanctionsProductCountryKeyboard(pageItems, targetsWithSanctions, page, totalPages),
                cancellationToken
            );
        }

        private async Task ShowEconomySanctionsProductResources(ITelegramBotClient botClient, long userId, long chatId, int msgId, int targetCountryId, int page, CancellationToken cancellationToken)
        {
            var resources = EconomyResourcesData.GetAll();
            int topCount = EconomyConfigData.GetInt("ProductSanctionsTopResources", 10);
            if (topCount < 0) topCount = 0;

            var topResources = resources
                .OrderByDescending(r => r.BasePrice)
                .ThenBy(r => r.ResourceType)
                .Take(topCount)
                .ToList();

            var topKeys = new HashSet<string>(topResources.Select(r => r.ResourceType), StringComparer.OrdinalIgnoreCase);
            var remainingResources = resources
                .Where(r => !topKeys.Contains(r.ResourceType))
                .OrderBy(r => r.ResourceType)
                .ToList();

            int perPage = 10;
            if (page < 0) page = 0;
            int totalPages = (int)Math.Ceiling(remainingResources.Count / (double)perPage);
            if (totalPages == 0) totalPages = 1;
            if (page >= totalPages) page = totalPages - 1;

            var pageItems = remainingResources.Skip(page * perPage).Take(perPage).ToList();
            bool showTop = page == 0 && topResources.Count > 0;

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                "📦 Оберіть ресурс для ліміту (або кнопку «Весь імпорт/експорт країни»):",
                StateKeyboardBuilder.EconomySanctionsProductResourceKeyboard(targetCountryId, pageItems, topResources, page, totalPages, showTop),
                cancellationToken
            );
        }

        private async Task ShowEconomySanctionsProductInfo(ITelegramBotClient botClient, long userId, long chatId, int msgId, int targetCountryId, string resourceType, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var target = CountriesData.GetCountryById(targetCountryId);
            if (target == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Країну не знайдено.",
                    StateKeyboardBuilder.EconomySanctionsProductCountryKeyboard(new List<CountryRecord>(), new HashSet<int>(), 0, 1),
                    cancellationToken
                );
                return;
            }

            var record = EconomyCountryProductSanctionsData.GetBySourceTargetResource(selectedCountryId.Value, targetCountryId, resourceType);
            int exportLevel = record?.ExportLevel ?? 0;
            int importLevel = record?.ImportLevel ?? 0;
            float exportEff = record?.ExportEffectiveness ?? 0f;
            float importEff = record?.ImportEffectiveness ?? 0f;

            float exportImpact = exportLevel == (int)SanctionsLevel.Embargo
                ? 100f
                : EconomyCalculator.Clamp(exportEff, 0f, 1f) * 100f;
            float importImpact = importLevel == (int)SanctionsLevel.Embargo
                ? 100f
                : EconomyCalculator.Clamp(importEff, 0f, 1f) * 100f;

            string text =
$@"📦 Ліміт торгівлі по товару
Країна: {target.Name}
Ресурс: {FormatResourceTypeUk(resourceType)}
Експорт: {(exportLevel == (int)SanctionsLevel.Embargo ? "ЕМБАРГО" : $"-{exportImpact:0.#}%")} (ефект {exportEff * 100f:0.#}%)
Імпорт: {(importLevel == (int)SanctionsLevel.Embargo ? "ЕМБАРГО" : $"-{importImpact:0.#}%")} (ефект {importEff * 100f:0.#}%)
Ефект = зниження обсягу (0–99%). Ембарго = 100% блок.";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.EconomySanctionsProductKeyboard(targetCountryId, EncodeResourceKey(resourceType), exportLevel, importLevel, exportEff, importEff),
                cancellationToken
            );
        }

        private async Task SetEconomySanctionsProductLevel(ITelegramBotClient botClient, long userId, long chatId, int msgId, int targetCountryId, string resourceType, int level, bool isExport, CancellationToken cancellationToken)
        {
            if (level != 0 && level != (int)SanctionsLevel.Embargo)
                return;

            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var existing = EconomyCountryProductSanctionsData.GetBySourceTargetResource(selectedCountryId.Value, targetCountryId, resourceType);
            int exportLevel = existing?.ExportLevel ?? 0;
            int importLevel = existing?.ImportLevel ?? 0;
            float exportEff = existing?.ExportEffectiveness ?? 0f;
            float importEff = existing?.ImportEffectiveness ?? 0f;

            if (isExport)
            {
                exportLevel = level;
                if (level == (int)SanctionsLevel.Embargo)
                    exportEff = 1f;
            }
            else
            {
                importLevel = level;
                if (level == (int)SanctionsLevel.Embargo)
                    importEff = 1f;
            }

            if (exportLevel == 0 && importLevel == 0 && exportEff <= 0f && importEff <= 0f)
            {
                EconomyCountryProductSanctionsData.Delete(selectedCountryId.Value, targetCountryId, resourceType);
            }
            else
            {
                EconomyCountryProductSanctionsData.Upsert(selectedCountryId.Value, targetCountryId, resourceType, exportLevel, importLevel, exportEff, importEff);
            }

            await ShowEconomySanctionsProductInfo(botClient, userId, chatId, msgId, targetCountryId, resourceType, cancellationToken);
        }

        private async Task SetEconomySanctionsProductEfficiency(ITelegramBotClient botClient, long userId, long chatId, int msgId, int targetCountryId, string resourceType, float effPct, bool isExport, CancellationToken cancellationToken)
        {
            if (effPct < 0) effPct = 0;
            if (effPct >= 100)
            {
                await SetEconomySanctionsProductLevel(botClient, userId, chatId, msgId, targetCountryId, resourceType, (int)SanctionsLevel.Embargo, isExport, cancellationToken);
                return;
            }
            if (effPct > 99) effPct = 99;

            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var existing = EconomyCountryProductSanctionsData.GetBySourceTargetResource(selectedCountryId.Value, targetCountryId, resourceType);
            int exportLevel = existing?.ExportLevel ?? 0;
            int importLevel = existing?.ImportLevel ?? 0;
            float exportEff = existing?.ExportEffectiveness ?? 0f;
            float importEff = existing?.ImportEffectiveness ?? 0f;

            float eff = effPct / 100f;
            if (isExport)
            {
                exportEff = eff;
                exportLevel = 0;
            }
            else
            {
                importEff = eff;
                importLevel = 0;
            }

            EconomyCountryProductSanctionsData.Upsert(selectedCountryId.Value, targetCountryId, resourceType, exportLevel, importLevel, exportEff, importEff);

            await ShowEconomySanctionsProductInfo(botClient, userId, chatId, msgId, targetCountryId, resourceType, cancellationToken);
        }

        private async Task StartEconomySanctionsProductEfficiencyInput(ITelegramBotClient botClient, long userId, long chatId, int msgId, int targetCountryId, string resourceType, bool isExport, CancellationToken cancellationToken)
        {
            EconomyInputState[userId] = EconomyInputMode.ProductSanctionsEfficiency;
            EconomyProductSanctionsEdit[userId] = new ProductSanctionsEfficiencyContext
            {
                TargetCountryId = targetCountryId,
                ResourceType = resourceType,
                IsExport = isExport
            };
            EconomyProductSanctionsUiContext[userId] = (chatId, msgId);

            string prompt = isExport
                ? "Вкажіть ліміт експорту для цього товару у % (0-99):"
                : "Вкажіть ліміт імпорту для цього товару у % (0-99):";
            await SendHelperMessageAsync(botClient, userId, prompt, cancellationToken);
        }

        private async Task ResetEconomySanctionsProduct(ITelegramBotClient botClient, long userId, long chatId, int msgId, int targetCountryId, string resourceType, bool isExport, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var existing = EconomyCountryProductSanctionsData.GetBySourceTargetResource(selectedCountryId.Value, targetCountryId, resourceType);
            if (existing == null)
            {
                await ShowEconomySanctionsProductInfo(botClient, userId, chatId, msgId, targetCountryId, resourceType, cancellationToken);
                return;
            }

            int exportLevel = existing.ExportLevel;
            int importLevel = existing.ImportLevel;
            float exportEff = existing.ExportEffectiveness;
            float importEff = existing.ImportEffectiveness;

            if (isExport)
            {
                exportLevel = 0;
                exportEff = 0f;
            }
            else
            {
                importLevel = 0;
                importEff = 0f;
            }

            if (exportLevel == 0 && importLevel == 0 && exportEff <= 0f && importEff <= 0f)
            {
                EconomyCountryProductSanctionsData.Delete(selectedCountryId.Value, targetCountryId, resourceType);
            }
            else
            {
                EconomyCountryProductSanctionsData.Upsert(selectedCountryId.Value, targetCountryId, resourceType, exportLevel, importLevel, exportEff, importEff);
            }

            await ShowEconomySanctionsProductInfo(botClient, userId, chatId, msgId, targetCountryId, resourceType, cancellationToken);
        }

        private async Task HandleBuildingCategorySelection(ITelegramBotClient botClient, long userId, long chatId, int msgId, string data, CancellationToken cancellationToken)
        {
            if (!EconomyInputState.TryGetValue(userId, out var mode) || mode != EconomyInputMode.AddBuilding)
                return;

            int step = EconomyInputStep[userId];
            if (step < 0 || step >= BuildingFields.Count)
                step = 0;
            var field = BuildingFields[step];
            if (field.FieldName != "Category")
                return;

            if (!EconomyBuildingBuffer.TryGetValue(userId, out var building))
            {
                building = new EconomyBuildingRecord();
                EconomyBuildingBuffer[userId] = building;
            }

            string category = data.Split('_').LastOrDefault() ?? string.Empty;
            building.Category = category;

            EconomyInputStep[userId] = step + 1;
            if (EconomyInputStep[userId] >= BuildingFields.Count)
            {
                bool ok = TryAddBuildingWithPayment(userId, building, out int id, out decimal chargedCost, out string payerSource, out string paymentError);
                await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
                ClearEconomyInput(userId);
                string result;
                if (ok)
                {
                    result = $"✅ Будівлю додано (Id={id}).";
                    if (chargedCost > 0m)
                        result += $"\nСписано: {FmtNumberCompact(chargedCost)} ({payerSource}).";
                }
                else
                {
                    result = paymentError;
                }
                await botClient.SendTextMessageAsync(userId, result, cancellationToken: cancellationToken);
                return;
            }

            var nextField = BuildingFields[EconomyInputStep[userId]];
            if (IsBuildingResourceSelectionField(nextField.FieldName))
            {
                await ShowBuildingResourceSelect(botClient, userId, nextField.FieldName, 0, cancellationToken);
                return;
            }
            await SendModeFieldPromptAsync(
                botClient,
                userId,
                EconomyInputMode.AddBuilding,
                nextField.FieldName,
                nextField.Prompt,
                cancellationToken);
        }
        private async Task ResetEconomySanctionsTarget(ITelegramBotClient botClient, long userId, long chatId, int msgId, int targetCountryId, bool isExport, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var existing = EconomyCountrySanctionsData.GetBySourceTarget(selectedCountryId.Value, targetCountryId);
            if (existing == null)
            {
                await ShowEconomySanctionsTargetInfo(botClient, userId, chatId, msgId, targetCountryId, cancellationToken);
                return;
            }

            int exportLevel = existing.ExportLevel;
            int importLevel = existing.ImportLevel;
            float exportEff = existing.ExportEffectiveness;
            float importEff = existing.ImportEffectiveness;

            if (isExport)
            {
                exportLevel = 0;
                exportEff = 0f;
            }
            else
            {
                importLevel = 0;
                importEff = 0f;
            }

            _logger.LogInformation("Sanctions target reset: user={UserId} target={TargetId} isExport={IsExport} exportLevel={ExportLevel} importLevel={ImportLevel} exportEff={ExportEff} importEff={ImportEff}",
                userId, targetCountryId, isExport, exportLevel, importLevel, exportEff, importEff);

            if (exportLevel == 0 && importLevel == 0 && exportEff <= 0f && importEff <= 0f)
            {
                EconomyCountrySanctionsData.Delete(selectedCountryId.Value, targetCountryId);
            }
            else
            {
                EconomyCountrySanctionsData.Upsert(selectedCountryId.Value, targetCountryId, exportLevel, importLevel, exportEff, importEff);
            }

            await ShowEconomySanctionsTargetInfo(botClient, userId, chatId, msgId, targetCountryId, cancellationToken);
        }

        private static string FormatSanctionsLevel(int level)
        {
            return level switch
            {
                1 => "Часткові",
                2 => "Повні",
                3 => "Ембарго",
                _ => "Немає"
            };
        }

        private static string FormatBreakdownSourceUk(string? source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return "Невідоме джерело";

            return source.Trim().ToLowerInvariant() switch
            {
                "payroll" => "Фонд оплати праці",
                "production" => "Виробництво",
                "trade" => "Торгівля",
                "companies" => "Компанії",
                "buildings" => "Будівлі",
                "government" => "Держсектор",
                "population" => "Населення",
                "science" => "Наука",
                "festivals" => "Фестивалі",
                "military" => "Військо",
                "infrastructure" => "Інфраструктура",
                "corruption" => "Корупція",
                "parliament" => "Парламент",
                "economyservice" => "Економічний розрахунок",
                _ => source.Trim()
            };
        }

        private async Task ShowEconomyMoneyLog(ITelegramBotClient botClient, long userId, long chatId, int msgId, int page, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            const int perPage = 10;
            if (page < 0) page = 0;
            var items = EconomyMoneyLogData.GetByCountry(selectedCountryId.Value, page, perPage, out int totalCount);
            int totalPages = (int)Math.Ceiling(totalCount / (double)perPage);
            if (totalPages == 0) totalPages = 1;
            if (page >= totalPages) page = totalPages - 1;

            var lines = new List<string>();
            foreach (var it in items)
            {
                string who = ResolveUserDisplayName(it.UserId);
                string sign = it.Kind == "Expense" ? "-" : "+";
                string turn = it.TurnNumber > 0 ? $" (день {FmtNumber(it.TurnNumber)})" : string.Empty;
                lines.Add($"{it.CreatedAt:dd.MM HH:mm} {sign}{FmtNumber(it.Amount)} | {it.Reason} | {who}{turn}");
            }

            string text = lines.Count == 0
                ? "📜 Лог коштів: порожньо."
                : "📜 Лог коштів:\n" + string.Join("\n", lines);

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.EconomyMoneyLogKeyboard(page, totalPages),
                cancellationToken
            );
        }

        private static string ResolveUserDisplayName(long userId)
        {
            if (userId == 0)
                return "Система";

            string name = UserData.DataBaseValueReader<string>(userId, "Name");
            if (string.IsNullOrWhiteSpace(name))
                return $"Користувач:{userId}";
            return name;
        }

        private async Task ShowEconomyResourcesMenu(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            MilitaryArsenalCatalog.EnsureResourcesInEconomyCatalog();

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                "📦 Ресурси",
                StateKeyboardBuilder.EconomyResourcesKeyboard(),
                cancellationToken
            );
        }

        private async Task ShowEconomyResourcesList(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            int page,
            CancellationToken cancellationToken,
            bool militaryOnly = false)
        {
            MilitaryArsenalCatalog.EnsureResourcesInEconomyCatalog();

            var resources = EconomyResourcesData.GetAll()
                .OrderBy(r => r.ResourceType)
                .ToList();
            if (militaryOnly)
            {
                resources = resources
                    .Where(r => MilitaryArsenalCatalog.IsMilitaryResourceType(r.ResourceType))
                    .ToList();
            }

            int perPage = 10;
            if (page < 0) page = 0;
            int totalPages = (int)Math.Ceiling(resources.Count / (double)perPage);
            if (totalPages == 0) totalPages = 1;
            if (page >= totalPages) page = totalPages - 1;

            var pageItems = resources.Skip(page * perPage).Take(perPage).ToList();
            var items = pageItems
                .Select(r =>
                {
                    string parentSuffix = string.IsNullOrWhiteSpace(r.ParentResourceType)
                        ? string.Empty
                        : $" → {FormatResourceTypeUk(r.ParentResourceType)}";
                    return ($"{FormatResourceTypeUk(r.ResourceType)}{parentSuffix} ({FmtNumberCompact(r.BasePrice)})", $"state_economy_resourceinfo|{EncodeResourceKey(r.ResourceType)}");
                })
                .ToList();

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                militaryOnly ? "🛡 Список військових ресурсів:" : "📦 Список ресурсів:",
                militaryOnly
                    ? StateKeyboardBuilder.EconomyResourcesMilitaryListKeyboard(items, page, totalPages)
                    : StateKeyboardBuilder.EconomyResourcesListKeyboard(items, page, totalPages),
                cancellationToken
            );
        }

        private async Task ShowEconomyResourceInfo(ITelegramBotClient botClient, long userId, long chatId, int msgId, string resourceType, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(resourceType))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Невідомий ресурс.",
                    StateKeyboardBuilder.EconomyResourcesKeyboard(),
                    cancellationToken
                );
                return;
            }

            var res = EconomyResourcesData.GetByType(resourceType);
            if (res == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Ресурс не знайдено.",
                    StateKeyboardBuilder.EconomyResourcesListKeyboard(new List<(string text, string data)>(), 0, 1),
                    cancellationToken
                );
                return;
            }

string text =
$@"📦 Ресурс: {FormatResourceTypeUk(res.ResourceType)}
Батьківський тип: {(string.IsNullOrWhiteSpace(res.ParentResourceType) ? "-" : FormatResourceTypeUk(res.ParentResourceType))}
Базова ціна: {FmtNumberCompact(res.BasePrice)}
Торговий: {(res.IsTradeable ? "Так" : "Ні")}
Попит: {FmtNumberCompact(res.DemandFactor)}
Ігнорувати ефективність виробн.: {(res.IgnoreProductionEfficiency ? "Так" : "Ні")}";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.EconomyResourceInfoKeyboard(EncodeResourceKey(res.ResourceType)),
                cancellationToken
            );
        }

        private async Task ShowEconomyResourceEditMenu(ITelegramBotClient botClient, long userId, long chatId, int msgId, string resourceType, CancellationToken cancellationToken)
        {
            var res = EconomyResourcesData.GetByType(resourceType);
            if (res == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Ресурс не знайдено.",
                    StateKeyboardBuilder.EconomyResourcesKeyboard(),
                    cancellationToken
                );
                return;
            }

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                "⚙ Редагування ресурсу:",
                StateKeyboardBuilder.EconomyResourceEditKeyboard(EncodeResourceKey(res.ResourceType)),
                cancellationToken
            );
        }

        private async Task ToggleEconomyResourceProductionEfficiency(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            string resourceType,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(resourceType))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Невідомий ресурс.",
                    StateKeyboardBuilder.EconomyResourcesKeyboard(),
                    cancellationToken
                );
                return;
            }

            var res = EconomyResourcesData.GetByType(resourceType);
            if (res == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Ресурс не знайдено.",
                    StateKeyboardBuilder.EconomyResourcesKeyboard(),
                    cancellationToken
                );
                return;
            }

            bool newValue = !res.IgnoreProductionEfficiency;
            bool updated = EconomyResourcesData.UpdateResourceField(
                res.ResourceType,
                "IgnoreProductionEfficiency",
                newValue,
                SqlDbType.Bit);

            if (!updated)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "❌ Не вдалося змінити налаштування ефективності ресурсу.",
                    StateKeyboardBuilder.EconomyResourceInfoKeyboard(EncodeResourceKey(res.ResourceType)),
                    cancellationToken
                );
                return;
            }

            await ShowEconomyResourceInfo(botClient, userId, chatId, msgId, res.ResourceType, cancellationToken);
        }

        private async Task ToggleEconomyBuildingProductionEfficiency(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            int buildingId,
            CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var building = EconomyBuildingsData.GetByCountry(selectedCountryId.Value)
                .FirstOrDefault(x => x.BuildingId == buildingId);
            if (building == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Будівлю не знайдено.",
                    StateKeyboardBuilder.EconomyBuildingsKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }

            bool newValue = !building.IgnoreProductionEfficiency;
            bool updated = EconomyBuildingsData.UpdateBuildingField(
                buildingId,
                selectedCountryId.Value,
                "IgnoreProductionEfficiency",
                newValue,
                SqlDbType.Bit);

            if (!updated)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "❌ Не вдалося змінити налаштування ефективності будівлі.",
                    StateKeyboardBuilder.EconomyBuildingInfoKeyboard(buildingId, userId == masterID),
                    cancellationToken
                );
                return;
            }

            await ShowEconomyBuildingInfo(botClient, userId, chatId, msgId, buildingId, cancellationToken);
        }

        private static List<string> BuildResourceOutputSummary(StateDataClass state, List<EconomyBuildingRecord> buildings, List<EconomyFestivalRecord> festivals, out float efficiencyPct)
        {
            var lines = new List<string>();
            var production = ComputeResourceProductionNow(state, buildings);
            efficiencyPct = production.EfficiencyPct;
            if (production.NetByResource.Count == 0)
                return lines;

            foreach (var pair in production.NetByResource.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
            {
                decimal baseSum = buildings
                    .Where(b => HasResourceOutputBuilding(b))
                    .Where(b => !string.IsNullOrWhiteSpace(b.ResourceType))
                    .Where(b => string.Equals(b.ResourceType, pair.Key, StringComparison.OrdinalIgnoreCase))
                    .Sum(b => b.BaseProduction);
                production.ProducedByResource.TryGetValue(pair.Key, out var grossProduced);
                production.ConsumedByResource.TryGetValue(pair.Key, out var internalConsumed);
                if (internalConsumed > 0m)
                {
                    lines.Add($"{FormatResourceTypeUk(pair.Key)}: {FmtNumberCompact(pair.Value)} (виробн {FmtNumberCompact(grossProduced)}, техспож {FmtNumberCompact(internalConsumed)}, база {FmtNumberCompact(baseSum)})");
                }
                else
                {
                    lines.Add($"{FormatResourceTypeUk(pair.Key)}: {FmtNumberCompact(pair.Value)} (база {FmtNumberCompact(baseSum)})");
                }
            }

            return lines;
        }

        private static Dictionary<string, decimal> BuildResourceProductionMap(StateDataClass state, List<EconomyBuildingRecord> buildings, List<EconomyFestivalRecord> festivals, out float efficiencyPct)
        {
            var production = ComputeResourceProductionNow(state, buildings);
            efficiencyPct = production.EfficiencyPct;
            return production.NetByResource;
        }

        private async Task DeleteEconomyResource(ITelegramBotClient botClient, long userId, long chatId, int msgId, string resourceType, CancellationToken cancellationToken)
        {
            bool deleted = EconomyResourcesData.DeleteResource(resourceType);
            string result = deleted ? "✅ Ресурс видалено." : "Не вдалося видалити ресурс.";
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                result,
                StateKeyboardBuilder.EconomyResourcesKeyboard(),
                cancellationToken
            );
        }

        private async Task ShowEconomyResourceDeleteConfirm(ITelegramBotClient botClient, long userId, long chatId, int msgId, string resourceType, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(resourceType))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Невідомий ресурс.",
                    StateKeyboardBuilder.EconomyResourcesKeyboard(),
                    cancellationToken
                );
                return;
            }

            var res = EconomyResourcesData.GetByType(resourceType);
            if (res == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Ресурс не знайдено.",
                    StateKeyboardBuilder.EconomyResourcesKeyboard(),
                    cancellationToken
                );
                return;
            }

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                $"🗑 Видалити ресурс `{res.ResourceType}`?",
                StateKeyboardBuilder.EconomyResourceDeleteConfirmKeyboard(EncodeResourceKey(res.ResourceType)),
                cancellationToken
            );
        }

        private async Task RunEconomyTurn(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var service = new EconomyService();
            var result = service.ApplyTurn(selectedCountryId.Value);
            var researchService = new ResearchService();
            string researchTick = researchService.UpdateResearch(selectedCountryId.Value);
            var ai = ApplyTimeAutomationTurns(selectedCountryId.Value, 1);
            string summary = result.Summary;
            if (!string.Equals(researchTick, "Немає активного дослідження.", StringComparison.Ordinal))
                summary += $"\n\n{researchTick}";
            if (ai.AdvancedCountries > 0 || ai.FailedCountries > 0)
            {
                summary +=
$"\n\n🤖 ШІ-країни: {FmtNumber(ai.AdvancedCountries)} оновлено" +
(ai.FailedCountries > 0 ? $", помилок: {FmtNumber(ai.FailedCountries)}" : string.Empty);
            }

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                summary,
                StateKeyboardBuilder.EconomyMenuKeyboard(userId == masterID),
                cancellationToken
            );
        }

        private async Task ShowEconomyBuildingsMenu(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            int countryId = selectedCountryId.Value;
            var buildings = EconomyBuildingsData.GetByCountry(countryId);
            EnsureCountryCityCatalog(countryId, buildings);
            var underConstruction = buildings
                .Where(IsBuildingUnderConstruction)
                .OrderBy(b => Math.Max(0, b.BuildTurns))
                .ThenBy(b => b.Type)
                .ToList();
            int militaryBuildings = buildings.Count(b => string.Equals(b.Category, "Military", StringComparison.OrdinalIgnoreCase));
            int civilBuildings = Math.Max(0, buildings.Count - militaryBuildings);
            var cityNames = GetAvailableBuildingCities(countryId, buildings);

            string underConstructionLabel = underConstruction.Count == 0
                ? "немає"
                : string.Join("\n", underConstruction.Take(6).Select(b =>
                    $"• {b.Type} [{FormatBuildingCityName(b.CityName)}] (L{FmtNumber(b.Level)}), ще {FmtNumber(Math.Max(1, b.BuildTurns))} дн."))
                    + (underConstruction.Count > 6 ? $"\n• ...ще {FmtNumber(underConstruction.Count - 6)}" : string.Empty);

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                "🏗 Будівлі\n" +
                $"Всього: {FmtNumber(buildings.Count)}\n" +
                $"Цивільні / військові: {FmtNumber(civilBuildings)} / {FmtNumber(militaryBuildings)}\n" +
                $"Міста у довіднику: {FmtNumber(cityNames.Count)}\n" +
                $"У будівництві: {FmtNumber(underConstruction.Count)}\n" +
                "Ефекти будівель у будівництві: ❌ неактивні до завершення.\n\n" +
                $"Поточні будівництва:\n{underConstructionLabel}",
                StateKeyboardBuilder.EconomyBuildingsKeyboard(userId == masterID),
                cancellationToken
            );
        }

        private async Task ShowEconomyCitiesMenu(ITelegramBotClient botClient, long userId, long chatId, int msgId, int page, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            int countryId = selectedCountryId.Value;
            var buildings = EconomyBuildingsData.GetByCountry(countryId);
            EnsureCountryCityCatalog(countryId, buildings);
            var cities = EconomyCitiesData.GetByCountry(countryId);
            var buildingCountByCity = buildings
                .GroupBy(b => NormalizeBuildingCityName(b.CityName), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            var items = cities
                .Select(c =>
                {
                    string cityName = NormalizeBuildingCityName(c.CityName);
                    int cityBuildings = buildingCountByCity.TryGetValue(cityName, out var cnt) ? cnt : 0;
                    return ($"🏙 {FormatBuildingCityName(cityName)} (будівель: {FmtNumber(cityBuildings)})", $"state_economy_cityinfo_{c.CityId}");
                })
                .ToList();

            int perPage = 10;
            if (page < 0) page = 0;
            int totalPages = (int)Math.Ceiling(items.Count / (double)perPage);
            if (totalPages == 0) totalPages = 1;
            if (page >= totalPages) page = totalPages - 1;

            var pageItems = items.Skip(page * perPage).Take(perPage).ToList();

            string text =
                "🏙 Міста країни\n" +
                $"Всього міст: {FmtNumber(cities.Count)}\n" +
                $"Будівель загалом: {FmtNumber(buildings.Count)}\n\n" +
                "Оберіть місто для деталей або скористайтесь кнопками нижче.";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.EconomyCitiesListKeyboard(pageItems, page, totalPages),
                cancellationToken
            );
        }

        private async Task ShowEconomyCityInfo(ITelegramBotClient botClient, long userId, long chatId, int msgId, int cityId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            int countryId = selectedCountryId.Value;
            var buildings = EconomyBuildingsData.GetByCountry(countryId);
            EnsureCountryCityCatalog(countryId, buildings);

            var city = EconomyCitiesData.GetById(cityId, countryId);
            if (city == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Місто не знайдено.",
                    StateKeyboardBuilder.EconomyCityInfoKeyboard(cityId, canDelete: false),
                    cancellationToken
                );
                return;
            }

            string cityName = NormalizeBuildingCityName(city.CityName);
            int buildingsInCity = buildings.Count(b => string.Equals(
                NormalizeBuildingCityName(b.CityName),
                cityName,
                StringComparison.OrdinalIgnoreCase));
            int underConstruction = buildings.Count(b => string.Equals(
                NormalizeBuildingCityName(b.CityName),
                cityName,
                StringComparison.OrdinalIgnoreCase) && IsBuildingUnderConstruction(b));

            bool isCapital = string.Equals(cityName, EconomyBuildingRecord.DefaultCityName, StringComparison.OrdinalIgnoreCase);
            string text =
                $"🏙 {FormatBuildingCityName(cityName)}\n" +
                $"Будівель: {FmtNumber(buildingsInCity)}\n" +
                $"У будівництві: {FmtNumber(underConstruction)}\n" +
                $"Столиця: {(isCapital ? "Так" : "Ні")}\n" +
                $"Створено: {city.CreatedAt:yyyy-MM-dd HH:mm}";

            if (isCapital)
                text += "\n\nСтолицю не можна зруйнувати.";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.EconomyCityInfoKeyboard(city.CityId, canDelete: !isCapital),
                cancellationToken
            );
        }

        private async Task ShowEconomyCityDestroySelection(ITelegramBotClient botClient, long userId, long chatId, int msgId, int page, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            int countryId = selectedCountryId.Value;
            EnsureCountryCityCatalog(countryId);

            var cities = EconomyCitiesData.GetByCountry(countryId)
                .Where(c => !string.Equals(NormalizeBuildingCityName(c.CityName), EconomyBuildingRecord.DefaultCityName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var items = cities
                .Select(c => ($"🧨 {FormatBuildingCityName(c.CityName)}", $"state_economy_city_delete_{c.CityId}"))
                .ToList();

            int perPage = 10;
            if (page < 0) page = 0;
            int totalPages = (int)Math.Ceiling(items.Count / (double)perPage);
            if (totalPages == 0) totalPages = 1;
            if (page >= totalPages) page = totalPages - 1;
            var pageItems = items.Skip(page * perPage).Take(perPage).ToList();

            string text = items.Count == 0
                ? "🧨 Руйнування міст\nНемає міст для руйнування (столиця не руйнується)."
                : "🧨 Руйнування міст\nОберіть місто для руйнування. Усі його будівлі буде перенесено в столицю.";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.EconomyCitiesDestroyKeyboard(pageItems, page, totalPages),
                cancellationToken
            );
        }

        private async Task StartEconomyCityCreation(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await botClient.SendTextMessageAsync(
                    userId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    cancellationToken: cancellationToken);
                return;
            }

            await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
            ClearEconomyInput(userId);

            EconomyInputState[userId] = EconomyInputMode.CreateCityName;
            EconomyInputStep[userId] = 0;

            await SendHelperMessageAsync(botClient, userId, "Введіть назву нового міста:", cancellationToken);
        }

        private async Task HandleEconomyCityCreateInput(ITelegramBotClient botClient, long userId, string input, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
                ClearEconomyInput(userId);
                await botClient.SendTextMessageAsync(
                    userId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    cancellationToken: cancellationToken);
                return;
            }

            if (!TryNormalizeBuildingCityInput(input, out var cityName, out var error))
            {
                await SendHelperMessageAsync(botClient, userId, error, cancellationToken);
                return;
            }

            int countryId = selectedCountryId.Value;
            EnsureCountryCityCatalog(countryId);
            var existing = EconomyCitiesData.GetByCountry(countryId);
            if (existing.Any(c => string.Equals(NormalizeBuildingCityName(c.CityName), cityName, StringComparison.OrdinalIgnoreCase)))
            {
                await SendHelperMessageAsync(botClient, userId, $"❌ Місто \"{cityName}\" уже існує.", cancellationToken);
                return;
            }

            int cityId = EconomyCitiesData.EnsureCity(countryId, cityName);
            if (cityId <= 0)
            {
                await SendHelperMessageAsync(botClient, userId, "❌ Не вдалося створити місто.", cancellationToken);
                return;
            }

            SyncCountryCitiesCount(countryId);
            await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
            ClearEconomyInput(userId);
            await botClient.SendTextMessageAsync(userId, $"✅ Місто створено: {cityName}", cancellationToken: cancellationToken);
        }

        private async Task ShowEconomyCityDeleteConfirm(ITelegramBotClient botClient, long userId, long chatId, int msgId, int cityId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            int countryId = selectedCountryId.Value;
            var buildings = EconomyBuildingsData.GetByCountry(countryId);
            EnsureCountryCityCatalog(countryId, buildings);
            var city = EconomyCitiesData.GetById(cityId, countryId);
            if (city == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Місто не знайдено.",
                    StateKeyboardBuilder.EconomyCityInfoKeyboard(cityId, canDelete: false),
                    cancellationToken
                );
                return;
            }

            string cityName = NormalizeBuildingCityName(city.CityName);
            if (string.Equals(cityName, EconomyBuildingRecord.DefaultCityName, StringComparison.OrdinalIgnoreCase))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "❌ Столицю не можна зруйнувати.",
                    StateKeyboardBuilder.EconomyCityInfoKeyboard(cityId, canDelete: false),
                    cancellationToken
                );
                return;
            }

            int affectedBuildings = buildings.Count(b => string.Equals(
                NormalizeBuildingCityName(b.CityName),
                cityName,
                StringComparison.OrdinalIgnoreCase));

            string text =
                $"🧨 Зруйнувати місто \"{FormatBuildingCityName(cityName)}\"?\n" +
                $"Будівель буде перенесено у столицю: {FmtNumber(affectedBuildings)}";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.EconomyCityDeleteConfirmKeyboard(cityId),
                cancellationToken
            );
        }

        private async Task DeleteEconomyCity(ITelegramBotClient botClient, long userId, long chatId, int msgId, int cityId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            int countryId = selectedCountryId.Value;
            EnsureCountryCityCatalog(countryId);

            var city = EconomyCitiesData.GetById(cityId, countryId);
            if (city == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Місто не знайдено.",
                    StateKeyboardBuilder.EconomyCityInfoKeyboard(cityId, canDelete: false),
                    cancellationToken
                );
                return;
            }

            string cityName = NormalizeBuildingCityName(city.CityName);
            if (string.Equals(cityName, EconomyBuildingRecord.DefaultCityName, StringComparison.OrdinalIgnoreCase))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "❌ Столицю не можна зруйнувати.",
                    StateKeyboardBuilder.EconomyCityInfoKeyboard(cityId, canDelete: false),
                    cancellationToken
                );
                return;
            }

            EconomyCitiesData.EnsureCity(countryId, EconomyBuildingRecord.DefaultCityName);
            int movedBuildings = EconomyBuildingsData.MoveBuildingsToCity(countryId, cityName, EconomyBuildingRecord.DefaultCityName);
            bool deleted = EconomyCitiesData.DeleteCity(city.CityId, countryId);
            if (deleted)
                SyncCountryCitiesCount(countryId);

            string result = deleted
                ? $"✅ Місто \"{FormatBuildingCityName(cityName)}\" зруйновано. Перенесено будівель: {FmtNumber(movedBuildings)}."
                : "❌ Не вдалося зруйнувати місто.";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                result,
                StateKeyboardBuilder.EconomyCityInfoKeyboard(cityId, canDelete: false),
                cancellationToken
            );
        }

        private async Task ShowBuildingTemplates(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            int page,
            CancellationToken cancellationToken,
            bool militaryOnly = false)
        {
            int safePage = NormalizeBuildingTemplatePage(page, militaryOnly);
            int totalPages = GetBuildingTemplatePageCount(militaryOnly);
            int totalTemplates = GetBuildingTemplateCount(militaryOnly);
            var pageItems = GetBuildingTemplatePageItems(safePage, militaryOnly);
            string title = militaryOnly
                ? "🛡 Каталог військових шаблонів будівель"
                : "🏛 Каталог цивільних шаблонів будівель";

            string text =
$"{title}\n" +
$"📚 Шаблонів: {FmtNumber(totalTemplates)} | Сторінка: {FmtNumber(safePage + 1)}/{FmtNumber(totalPages)}\n\n" +
"Оберіть шаблон, щоб подивитися ефекти, вартість, час будівництва і різницю між рівнями." +
(militaryOnly ? "\nПідказка: зброярські/амуніційні шаблони мають рядок \"Виробництво\" у прев'ю." : string.Empty);

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.EconomyBuildingTemplatesKeyboard(pageItems, safePage, totalPages, militaryOnly),
                cancellationToken
            );
        }

        private async Task ShowBuildingTemplatePreview(ITelegramBotClient botClient, long userId, long chatId, int msgId, string data, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            string? key = ExtractTemplateKey(data);
            var building = BuildTemplateFromCallback(selectedCountryId.Value, data);
            if (building == null || string.IsNullOrWhiteSpace(key) || !TryGetBuildingTemplateDefinition(key, out var definition))
            {
                int fallbackTotalPages = GetBuildingTemplatePageCount();
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Шаблон не знайдено.",
                    StateKeyboardBuilder.EconomyBuildingTemplatesKeyboard(GetBuildingTemplatePageItems(0), 0, fallbackTotalPages),
                    cancellationToken
                );
                return;
            }

            string currentKey = definition.Key;
            bool militaryOnly = string.Equals(definition.Category, "Military", StringComparison.OrdinalIgnoreCase);
            string? prevKey = GetAdjacentBuildingTemplateKey(currentKey, -1, militaryOnly);
            string? nextKey = GetAdjacentBuildingTemplateKey(currentKey, 1, militaryOnly);
            int returnPage = GetBuildingTemplatePageByKey(currentKey, militaryOnly);
            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            float inflationPct = country?.Inflation ?? 0f;
            string text = BuildTemplatePreviewText(building, definition, inflationPct);
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.EconomyBuildingTemplatePreviewKeyboard(currentKey, prevKey, nextKey, returnPage, militaryOnly),
                cancellationToken
            );
        }

        private async Task StartBuildingTemplate(ITelegramBotClient botClient, long userId, string data, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await botClient.SendTextMessageAsync(userId, "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.", cancellationToken: cancellationToken);
                return;
            }

            var building = BuildTemplateFromCallback(selectedCountryId.Value, data);
            if (building == null)
            {
                await botClient.SendTextMessageAsync(userId, "Шаблон не знайдено.", cancellationToken: cancellationToken);
                return;
            }

            await StartBuildingCitySelection(botClient, userId, building, EconomyInputMode.AddBuildingTemplate, cancellationToken);
        }

        private async Task ShowBuildingEditMenu(ITelegramBotClient botClient, long userId, long chatId, int msgId, int buildingId, CancellationToken cancellationToken)
        {
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                "✏️ Редагування будівлі: оберіть поле",
                StateKeyboardBuilder.EconomyBuildingEditKeyboard(buildingId),
                cancellationToken
            );
        }

        private async Task ShowEconomyBuildingsList(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            int page,
            CancellationToken cancellationToken,
            bool militaryOnly = false)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var buildings = EconomyBuildingsData.GetByCountry(selectedCountryId.Value);
            if (militaryOnly)
            {
                buildings = buildings
                    .Where(b => string.Equals(b.Category, "Military", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var items = buildings
                .OrderBy(b => FormatBuildingCityName(b.CityName), StringComparer.OrdinalIgnoreCase)
                .ThenBy(b => b.Category)
                .ThenBy(b => b.Type)
                .Select(b =>
                {
                    if (IsBuildingUnderConstruction(b))
                        return ($"⏳ {b.Type} [{FormatBuildingCityName(b.CityName)}] (L{b.Level}) • {FmtNumber(Math.Max(1, b.BuildTurns))} дн.", $"state_economy_buildinginfo_{b.BuildingId}");
                    return ($"🏗 {b.Type} [{FormatBuildingCityName(b.CityName)}] (L{b.Level})", $"state_economy_buildinginfo_{b.BuildingId}");
                })
                .ToList();

            int perPage = 10;
            if (page < 0) page = 0;
            int totalPages = (int)Math.Ceiling(items.Count / (double)perPage);
            if (totalPages == 0) totalPages = 1;
            if (page >= totalPages) page = totalPages - 1;

            var pageItems = items.Skip(page * perPage).Take(perPage).ToList();

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                militaryOnly ? "🛡 Список військових будівель:" : "🏗 Список будівель:",
                militaryOnly
                    ? StateKeyboardBuilder.EconomyBuildingsMilitaryListKeyboard(pageItems, page, totalPages, userId == masterID)
                    : StateKeyboardBuilder.EconomyBuildingsListKeyboard(pageItems, page, totalPages, userId == masterID),
                cancellationToken
            );
        }

        private async Task ShowEconomyBuildingsUnderConstructionList(ITelegramBotClient botClient, long userId, long chatId, int msgId, int page, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var underConstruction = EconomyBuildingsData.GetByCountry(selectedCountryId.Value)
                .Where(IsBuildingUnderConstruction)
                .OrderBy(b => Math.Max(0, b.BuildTurns))
                .ThenBy(b => b.Type)
                .ToList();

            var items = underConstruction
                .Select(b => ($"⏳ {b.Type} [{FormatBuildingCityName(b.CityName)}] (L{b.Level}) • ще {FmtNumber(Math.Max(1, b.BuildTurns))} дн.", $"state_economy_buildinginfo_{b.BuildingId}"))
                .ToList();

            int perPage = 10;
            if (page < 0) page = 0;
            int totalPages = (int)Math.Ceiling(items.Count / (double)perPage);
            if (totalPages == 0) totalPages = 1;
            if (page >= totalPages) page = totalPages - 1;

            var pageItems = items.Skip(page * perPage).Take(perPage).ToList();
            string text = items.Count == 0
                ? "⏳ Будівлі у будівництві\nЗараз немає активних будівництв.\n\nЕфекти будівлі починають працювати лише після завершення будівництва."
                : $"⏳ Будівлі у будівництві\nАктивних будівництв: {FmtNumber(items.Count)}\n\nЕфекти цих будівель: ❌ неактивні до завершення.";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.EconomyBuildingsUnderConstructionKeyboard(pageItems, page, totalPages),
                cancellationToken
            );
        }

        private async Task ShowEconomyBuildingInfo(ITelegramBotClient botClient, long userId, long chatId, int msgId, int buildingId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var buildings = EconomyBuildingsData.GetByCountry(selectedCountryId.Value);
            var b = buildings.FirstOrDefault(x => x.BuildingId == buildingId);
            if (b == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Будівлю не знайдено.",
                    StateKeyboardBuilder.EconomyBuildingsKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            float inflationPct = country?.Inflation ?? 0f;
            decimal upgradeCost = CalculateBuildingUpgradeCost(b, inflationPct);
            decimal creationCost = GetBuildingCreationCost(b, inflationPct);
            string effectiveStatus = IsBuildingUnderConstruction(b)
                ? BuildingStatus.UnderConstruction.ToString()
                : b.BuildingStatus;
            string effectsStatus = AreBuildingEffectsActive(b) ? "✅ Активні" : "❌ Неактивні";
            string upgradeInfo = b.Level >= MaxBuildingLevel
                ? "Досягнуто максимального рівня"
                : IsBuildingUnderConstruction(b)
                    ? "Недоступно під час будівництва"
                    : FmtNumber(upgradeCost);

string text =
$@"🏗 {b.Type}
Категорія: {FormatBuildingCategoryUk(b.Category)}
Місто: {FormatBuildingCityName(b.CityName)}
Рівень: {FmtNumber(b.Level)}
Робочі місця: {FmtNumber(b.JobsProvided)}
Утримання: {FmtNumber(b.UpkeepCost)}
Базове виробництво: {FmtNumberCompact(b.BaseProduction)}
Вартість будівництва (база): {FmtNumber(b.BuildCost)}
Вартість будівництва (з інфляцією): {FmtNumber(creationCost)}
Тривалість будівництва: {FmtNumber(b.BuildTurns)} дн.
Вихідний ресурс: {(string.IsNullOrWhiteSpace(b.ResourceType) ? "-" : FormatResourceTypeUk(b.ResourceType))}
Вхідний ресурс: {(string.IsNullOrWhiteSpace(b.InputResourceType) ? "-" : FormatResourceTypeUk(b.InputResourceType))}
Споживання вхідного ресурсу/день: {FmtNumberCompact(b.InputAmount)}
Державна власність: {(b.StateOwned ? "Так" : "Ні")}
Ігнорувати ефективність виробн.: {(b.IgnoreProductionEfficiency ? "Так" : "Ні")}
Статус: {FormatBuildingStatusUk(effectiveStatus)}
Ефекти будівлі: {effectsStatus}
Неактивних днів: {FmtNumber(b.InactiveTurns)}
Деградація: {FmtNumber(b.DegradationLevel)}
Бонус стабільності: {FmtNumberCompact(b.StabilityBonus)}
Зменшення корупції: {FmtNumberCompact(b.CorruptionReduction)}
Бонус науки: {FmtNumberCompact(b.ResearchBonus)}
Бонус армії: {FmtNumberCompact(b.ArmyBonus)}
Покращення до L{FmtNumber(Math.Min(MaxBuildingLevel, b.Level + 1))}: {upgradeInfo}
Детальний прев'ю L{FmtNumber(Math.Min(MaxBuildingLevel, b.Level + 1))}..L{FmtNumber(MaxBuildingLevel)}: кнопка ""👁 Прев'ю рівнів""";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.EconomyBuildingInfoKeyboard(buildingId, userId == masterID),
                cancellationToken
            );
        }

        private async Task UpgradeEconomyBuilding(ITelegramBotClient botClient, long userId, long chatId, int msgId, int buildingId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            int countryId = selectedCountryId.Value;
            var country = CountriesData.GetCountryById(countryId);
            if (country == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Країну не знайдено.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var building = EconomyBuildingsData.GetByCountry(countryId).FirstOrDefault(x => x.BuildingId == buildingId);
            if (building == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Будівлю не знайдено.",
                    StateKeyboardBuilder.EconomyBuildingsKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }

            if (building.Level >= MaxBuildingLevel)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    $"🏗 {building.Type}\nМаксимальний рівень уже досягнуто (L{building.Level}).",
                    StateKeyboardBuilder.EconomyBuildingInfoKeyboard(buildingId, userId == masterID),
                    cancellationToken
                );
                return;
            }

            if (IsBuildingUnderConstruction(building))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    $"🏗 {building.Type}\nБудівля вже в процесі будівництва/покращення.",
                    StateKeyboardBuilder.EconomyBuildingInfoKeyboard(buildingId, userId == masterID),
                    cancellationToken
                );
                return;
            }

            decimal upgradeCost = CalculateBuildingUpgradeCost(building, country.Inflation);
            if (country.Treasury < upgradeCost)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    $"Недостатньо коштів у казні для покращення.\nПотрібно: {FmtNumber(upgradeCost)}\nЄ: {FmtNumber(country.Treasury)}",
                    StateKeyboardBuilder.EconomyBuildingInfoKeyboard(buildingId, userId == masterID),
                    cancellationToken
                );
                return;
            }

            country.Treasury -= (long)Math.Ceiling(upgradeCost);
            CountriesData.UpdateCountry(country);

            bool progressionApplied = TryApplyBuildingUpgradeProgression(building);
            if (!progressionApplied)
            {
                country.Treasury += (long)Math.Ceiling(upgradeCost);
                CountriesData.UpdateCountry(country);

                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    $"🏗 {building.Type}\nМаксимальний рівень уже досягнуто (L{building.Level}).",
                    StateKeyboardBuilder.EconomyBuildingInfoKeyboard(buildingId, userId == masterID),
                    cancellationToken
                );
                return;
            }

            building.BuildingStatus = BuildingStatus.UnderConstruction.ToString();
            building.InactiveTurns = 0;
            building.DegradationLevel = 0;

            bool upgraded = EconomyBuildingsData.UpdateBuildingAfterUpgrade(building);
            if (!upgraded)
            {
                 
                country.Treasury += (long)Math.Ceiling(upgradeCost);
                CountriesData.UpdateCountry(country);

                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не вдалося покращити будівлю.",
                    StateKeyboardBuilder.EconomyBuildingInfoKeyboard(buildingId, userId == masterID),
                    cancellationToken
                );
                return;
            }

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                $"✅ Будівлю покращено до L{building.Level}.\nСписано з казни: {FmtNumber(upgradeCost)}.\nЧас покращення: {FmtNumber(building.BuildTurns)} дн.",
                StateKeyboardBuilder.EconomyBuildingInfoKeyboard(buildingId, userId == masterID),
                cancellationToken
            );
        }

        private const int MaxBuildingLevel = 10;
        private const decimal UpgradeCostBaseLevelMultiplier = 1.02m;
        private const decimal UpgradeCostPerLevelMultiplier = 0.08m;
        private const decimal UpgradeJobsMultiplier = 1.30m;
        private const int UpgradeJobsAdditive = 2;
        private const decimal UpgradeUpkeepMultiplier = 1.12m;
        private const decimal UpgradeUpkeepAdditive = 0.8m;
        private const decimal UpgradeProductionMultiplier = 1.45m;
        private const decimal UpgradeProductionAdditive = 18m;
        private const decimal UpgradeBuildCostMultiplier = 1.18m;
        private const decimal UpgradeBuildCostAdditive = 2m;
        private const decimal UpgradeBuildTurnsMultiplier = 0.85m;
        private const decimal UpgradeBuildTurnsAdditive = 1m;
        private const float UpgradeBonusMultiplier = 1.20f;
        private const float UpgradeBonusAdditive = 0.08f;
        private const decimal UpgradeArmyMultiplier = 1.30m;
        private const int UpgradeArmyAdditive = 2;

        private static bool TryApplyBuildingUpgradeProgression(EconomyBuildingRecord building)
        {
            if (building == null || building.Level >= MaxBuildingLevel)
                return false;

            building.Level = Math.Min(MaxBuildingLevel, building.Level + 1);
            building.JobsProvided = ScaleIntPreserveZero(building.JobsProvided, UpgradeJobsMultiplier, UpgradeJobsAdditive);
            building.UpkeepCost = ScaleDecimalPreserveZero(building.UpkeepCost, UpgradeUpkeepMultiplier, UpgradeUpkeepAdditive);
            building.BaseProduction = ScaleDecimalPreserveZero(building.BaseProduction, UpgradeProductionMultiplier, UpgradeProductionAdditive);
            building.BuildCost = ScaleDecimal(building.BuildCost, UpgradeBuildCostMultiplier, UpgradeBuildCostAdditive);
            building.BuildTurns = ScaleBuildTurnsForUpgrade(building.BuildTurns);
            building.StabilityBonus = ScaleFloatPreserveZero(building.StabilityBonus, UpgradeBonusMultiplier, UpgradeBonusAdditive);
            building.CorruptionReduction = ScaleFloatPreserveZero(building.CorruptionReduction, UpgradeBonusMultiplier, UpgradeBonusAdditive);
            building.ResearchBonus = ScaleFloatPreserveZero(building.ResearchBonus, UpgradeBonusMultiplier, UpgradeBonusAdditive);
            building.PopulationGrowthBonus = ScaleFloatPreserveZero(building.PopulationGrowthBonus, UpgradeBonusMultiplier, UpgradeBonusAdditive);
            building.FestivalBoost = ScaleFloatPreserveZero(building.FestivalBoost, UpgradeBonusMultiplier, UpgradeBonusAdditive);
            building.ArmyBonus = ScaleIntPreserveZero(building.ArmyBonus, UpgradeArmyMultiplier, UpgradeArmyAdditive);
            return true;
        }

        private static int ScaleBuildTurnsForUpgrade(int value)
        {
            int safe = Math.Max(1, value);
            return Math.Max(1, (int)Math.Ceiling((safe * UpgradeBuildTurnsMultiplier) + UpgradeBuildTurnsAdditive));
        }

        private static decimal ScaleDecimalPreserveZero(decimal value, decimal multiplier, decimal additive)
        {
            if (value <= 0m)
                return 0m;
            return ScaleDecimal(value, multiplier, additive);
        }

        private static int ScaleIntPreserveZero(int value, decimal multiplier, int additive)
        {
            if (value <= 0)
                return 0;
            return ScaleInt(value, multiplier, additive);
        }

        private static float ScaleFloatPreserveZero(float value, float multiplier, float additive)
        {
            if (value <= 0f)
                return 0f;
            return ScaleFloat(value, multiplier, additive);
        }

        private static decimal CalculateBuildingUpgradeCost(EconomyBuildingRecord building, float inflationPct)
        {
            decimal baseCost = Math.Max(1m, building.BuildCost);
            int currentLevel = Math.Max(1, Math.Min(MaxBuildingLevel, building.Level));
            decimal levelMultiplier = UpgradeCostBaseLevelMultiplier + (currentLevel * UpgradeCostPerLevelMultiplier);
            decimal inflationMultiplier = EconomyService.GetInflationPriceMultiplier(inflationPct);
            return decimal.Ceiling(baseCost * levelMultiplier * inflationMultiplier);
        }

        private static decimal ScaleDecimal(decimal value, decimal multiplier, decimal additive)
        {
            decimal scaled = (value * multiplier) + additive;
            return scaled < 0m ? 0m : decimal.Round(scaled, 2, MidpointRounding.AwayFromZero);
        }

        private static int ScaleInt(int value, decimal multiplier, int additive)
        {
            decimal scaled = (value * multiplier) + additive;
            int rounded = (int)Math.Round(scaled, MidpointRounding.AwayFromZero);
            return rounded < 0 ? 0 : rounded;
        }

        private static float ScaleFloat(float value, float multiplier, float additive)
        {
            float scaled = (value * multiplier) + additive;
            if (scaled < 0f)
                return 0f;
            return (float)Math.Round(scaled, 3, MidpointRounding.AwayFromZero);
        }

        private async Task DeleteEconomyBuilding(ITelegramBotClient botClient, long userId, long chatId, int msgId, int buildingId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            bool deleted = EconomyBuildingsData.DeleteBuilding(buildingId, selectedCountryId.Value);
            string text = deleted ? "✅ Будівлю видалено." : "Будівлю не знайдено або немає доступу.";
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.EconomyBuildingsKeyboard(userId == masterID),
                cancellationToken
            );
        }

        private async Task ShowEconomyFestivalsMenu(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                "🎭 Фестивалі",
                StateKeyboardBuilder.EconomyFestivalsKeyboard(userId == masterID),
                cancellationToken
            );
        }

        private async Task ShowEconomyFestivalsList(ITelegramBotClient botClient, long userId, long chatId, int msgId, int page, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var festivals = EconomyFestivalsData.GetActiveByCountry(selectedCountryId.Value);
            var items = festivals
                .OrderBy(f => f.RemainingTurns)
                .Select(f => ($"🎭 {f.Name} ({FmtNumber(f.RemainingTurns)}/{FmtNumber(f.DurationTurns)} дн.)", $"state_economy_festivalinfo_{f.FestivalId}"))
                .ToList();

            int perPage = 10;
            if (page < 0) page = 0;
            int totalPages = (int)Math.Ceiling(items.Count / (double)perPage);
            if (totalPages == 0) totalPages = 1;
            if (page >= totalPages) page = totalPages - 1;

            var pageItems = items.Skip(page * perPage).Take(perPage).ToList();

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                "🎭 Активні фестивалі:",
                StateKeyboardBuilder.EconomyFestivalsListKeyboard(pageItems, page, totalPages, userId == masterID),
                cancellationToken
            );
        }

        private async Task ShowEconomyFestivalInfo(ITelegramBotClient botClient, long userId, long chatId, int msgId, int festivalId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var festivals = EconomyFestivalsData.GetActiveByCountry(selectedCountryId.Value);
            var f = festivals.FirstOrDefault(x => x.FestivalId == festivalId);
            if (f == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Фестиваль не знайдено.",
                    StateKeyboardBuilder.EconomyFestivalsKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }

string text =
$@"🎭 {f.Name}
Тривалість: {FmtNumber(f.RemainingTurns)}/{FmtNumber(f.DurationTurns)} дн.
Вартість: {FmtNumber(f.Cost)}
Бонус стабільності: {FmtNumberCompact(f.StabilityBonus)}";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.EconomyFestivalInfoKeyboard(festivalId, userId == masterID),
                cancellationToken
            );
        }

        private async Task ShowFestivalEditMenu(ITelegramBotClient botClient, long userId, long chatId, int msgId, int festivalId, CancellationToken cancellationToken)
        {
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                "✏️ Редагування фестивалю: оберіть поле",
                StateKeyboardBuilder.EconomyFestivalEditKeyboard(festivalId),
                cancellationToken
            );
        }

        private async Task EndEconomyFestival(ITelegramBotClient botClient, long userId, long chatId, int msgId, int festivalId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            bool ended = EconomyFestivalsData.EndFestival(festivalId, selectedCountryId.Value);
            string text = ended ? "✅ Фестиваль завершено." : "Фестиваль не знайдено або немає доступу.";
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.EconomyFestivalsKeyboard(userId == masterID),
                cancellationToken
            );
        }

        private async Task ShowTradeMenu(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                "🤝 Торгівля",
                StateKeyboardBuilder.EconomyTradeMenuKeyboard(userId == masterID),
                cancellationToken
            );
        }

        private async Task SyncTradeAgreementsPairs(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            if (userId != masterID)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Нема доступу.",
                    StateKeyboardBuilder.EconomyTradeMenuKeyboard(false),
                    cancellationToken
                );
                return;
            }

            var countryIds = CountriesData.GetAllCountries()
                .Select(c => c.CountryId)
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            var activeAgreements = new List<EconomyTradeAgreementRecord>();
            foreach (var countryId in countryIds)
                activeAgreements.AddRange(EconomyTradeAgreementsData.GetActiveByCountry(countryId));

            int checkedCount = 0;
            int skippedCount = 0;
            int alreadyMirroredCount = 0;
            int createdCount = 0;
            int failedCount = 0;
            var failedItems = new List<string>();

            foreach (var agreement in activeAgreements.OrderBy(a => a.AgreementId))
            {
                if (!agreement.PartnerCountryId.HasValue || agreement.PartnerCountryId.Value <= 0 || agreement.PartnerCountryId.Value == agreement.CountryId)
                {
                    skippedCount++;
                    continue;
                }

                checkedCount++;
                var mirrored = FindMirroredTradeAgreement(agreement);
                if (mirrored != null)
                {
                    alreadyMirroredCount++;
                    continue;
                }

                var mirrorToCreate = BuildMirroredTradeAgreement(agreement);
                if (mirrorToCreate.CountryId <= 0 ||
                    !mirrorToCreate.PartnerCountryId.HasValue ||
                    mirrorToCreate.PartnerCountryId.Value <= 0)
                {
                    failedCount++;
                    if (failedItems.Count < 5)
                        failedItems.Add($"#{agreement.AgreementId}: некоректні дані дзеркала.");
                    continue;
                }

                int mirrorId = EconomyTradeAgreementsData.AddAgreement(mirrorToCreate);
                if (mirrorId > 0)
                {
                    createdCount++;
                }
                else
                {
                    failedCount++;
                    if (failedItems.Count < 5)
                        failedItems.Add($"#{agreement.AgreementId}: не вдалося записати дзеркало.");
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine("🔁 Синхронізація торгових пар завершена.");
            sb.AppendLine($"Перевірено активних угод: {FmtNumber(checkedCount)}");
            sb.AppendLine($"Вже були синхронні: {FmtNumber(alreadyMirroredCount)}");
            sb.AppendLine($"Додано дзеркальних угод: {FmtNumber(createdCount)}");
            sb.AppendLine($"Пропущено: {FmtNumber(skippedCount)}");
            sb.AppendLine($"Помилки: {FmtNumber(failedCount)}");
            if (failedItems.Count > 0)
                sb.AppendLine($"Деталі: {string.Join(" | ", failedItems)}");

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                sb.ToString(),
                StateKeyboardBuilder.EconomyTradeMenuKeyboard(true),
                cancellationToken
            );
        }

        private static string ShortenTradeLabel(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "-";
            if (maxLength <= 0)
                return string.Empty;

            string normalized = value.Trim();
            if (normalized.Length <= maxLength)
                return normalized;
            if (maxLength <= 3)
                return normalized.Substring(0, maxLength);
            return normalized.Substring(0, maxLength - 3) + "...";
        }

        private static string BuildTradePendingItemLabel(TradeApprovalRequest request)
        {
            var agreement = request.Agreement;
            var sourceCountry = CountriesData.GetCountryById(agreement.CountryId);
            string sourceName = sourceCountry != null ? FormatCountryName(sourceCountry) : $"#{agreement.CountryId}";

            string partnerName = "-";
            if (agreement.PartnerCountryId.HasValue && agreement.PartnerCountryId.Value > 0)
            {
                var partnerCountry = CountriesData.GetCountryById(agreement.PartnerCountryId.Value);
                partnerName = partnerCountry != null ? FormatCountryName(partnerCountry) : $"#{agreement.PartnerCountryId.Value}";
            }
            else if (!string.IsNullOrWhiteSpace(agreement.PartnerName))
            {
                partnerName = agreement.PartnerName;
            }

            string flowShort = request.FlowMode switch
            {
                TradeFlowMode.ExportOnly => "Експ",
                TradeFlowMode.ImportOnly => "Імп",
                _ => "Екс/Імп"
            };

            return $"#{request.RequestId} {ShortenTradeLabel(sourceName, 12)} -> {ShortenTradeLabel(partnerName, 12)} ({flowShort})";
        }

        private async Task ShowTradePendingList(ITelegramBotClient botClient, long userId, long chatId, int msgId, int page, CancellationToken cancellationToken)
        {
            if (userId != masterID)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Нема доступу.",
                    StateKeyboardBuilder.EconomyTradeMenuKeyboard(false),
                    cancellationToken
                );
                return;
            }

            var pendingRequests = TradeApprovalRequests.Values
                .OrderByDescending(r => r.CreatedAtUtc)
                .ThenByDescending(r => r.RequestId)
                .ToList();

            var items = pendingRequests
                .Select(r => (BuildTradePendingItemLabel(r), $"state_economy_trade_pending_info_{r.RequestId}"))
                .ToList();

            int perPage = 10;
            if (page < 0) page = 0;
            int totalPages = (int)Math.Ceiling(items.Count / (double)perPage);
            if (totalPages == 0) totalPages = 1;
            if (page >= totalPages) page = totalPages - 1;

            var pageItems = items.Skip(page * perPage).Take(perPage).ToList();
            string text = pendingRequests.Count == 0
                ? "⏳ Запитів на схвалення немає."
                : $"⏳ Запити на схвалення: {FmtNumber(pendingRequests.Count)}";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.EconomyTradePendingListKeyboard(pageItems, page, totalPages),
                cancellationToken
            );
        }

        private async Task ShowTradePendingInfo(ITelegramBotClient botClient, long userId, long chatId, int msgId, int requestId, CancellationToken cancellationToken)
        {
            if (userId != masterID)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Нема доступу.",
                    StateKeyboardBuilder.EconomyTradeMenuKeyboard(false),
                    cancellationToken
                );
                return;
            }

            if (!TradeApprovalRequests.TryGetValue(requestId, out var request))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Запит не знайдено або вже оброблено.",
                    StateKeyboardBuilder.EconomyTradePendingListKeyboard(new List<(string text, string data)>(), 0, 1),
                    cancellationToken
                );
                return;
            }

            string text = BuildTradeApprovalText(request) + $"\nСтворено (UTC): {request.CreatedAtUtc:yyyy-MM-dd HH:mm:ss}";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.EconomyTradePendingInfoKeyboard(requestId),
                cancellationToken
            );
        }

        private async Task ShowTradeList(ITelegramBotClient botClient, long userId, long chatId, int msgId, int page, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var agreements = EconomyTradeAgreementsData.GetByCountry(selectedCountryId.Value);
            var items = agreements
                .OrderByDescending(a => a.IsActive)
                .ThenBy(a => a.PartnerName)
                .Select(a => ($"{(a.IsActive ? "✅" : "⛔")} {a.PartnerName}", $"state_economy_tradeinfo_{a.AgreementId}"))
                .ToList();

            int perPage = 10;
            if (page < 0) page = 0;
            int totalPages = (int)Math.Ceiling(items.Count / (double)perPage);
            if (totalPages == 0) totalPages = 1;
            if (page >= totalPages) page = totalPages - 1;

            var pageItems = items.Skip(page * perPage).Take(perPage).ToList();

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                "🤝 Список угод:",
                StateKeyboardBuilder.EconomyTradeListKeyboard(pageItems, page, totalPages, userId == masterID),
                cancellationToken
            );
        }

        private async Task ShowTradeInfo(ITelegramBotClient botClient, long userId, long chatId, int msgId, int agreementId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var agreements = EconomyTradeAgreementsData.GetByCountry(selectedCountryId.Value);
            var a = agreements.FirstOrDefault(x => x.AgreementId == agreementId);
            if (a == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Країну не знайдено.",
                    StateKeyboardBuilder.EconomyTradeMenuKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            decimal inflationPriceMultiplier = EconomyService.GetInflationPriceMultiplier(country?.Inflation ?? 0f);

            decimal exportUnitPrice = 0m;
            decimal importUnitPrice = 0m;
            if (!string.IsNullOrWhiteSpace(a.ExportResourceType))
            {
                var res = EconomyResourcesData.GetAll()
                    .FirstOrDefault(r => string.Equals(r.ResourceType, a.ExportResourceType, StringComparison.OrdinalIgnoreCase));
                if (res != null)
                    exportUnitPrice = res.BasePrice;
            }
            if (!string.IsNullOrWhiteSpace(a.ImportResourceType))
            {
                var res = EconomyResourcesData.GetAll()
                    .FirstOrDefault(r => string.Equals(r.ResourceType, a.ImportResourceType, StringComparison.OrdinalIgnoreCase));
                if (res != null)
                    importUnitPrice = res.BasePrice;
            }
            decimal exportUnit = (a.ExportUnitPrice > 0 ? a.ExportUnitPrice : exportUnitPrice) * inflationPriceMultiplier;
            decimal importUnit = (a.ImportUnitPrice > 0 ? a.ImportUnitPrice : importUnitPrice) * inflationPriceMultiplier;
            int deliveryDays = GetTradeDeliveryDays(a);
            decimal exportPerDayQty = GetTradeLegPerDayQty(a, isExport: true);
            decimal importPerDayQty = GetTradeLegPerDayQty(a, isExport: false);
            decimal exportPerDeliveryQty = GetTradeLegPerDeliveryQty(a, isExport: true);
            decimal importPerDeliveryQty = GetTradeLegPerDeliveryQty(a, isExport: false);
            decimal exportTotal = exportPerDayQty * exportUnit;
            decimal importTotal = importPerDayQty * importUnit;
            string qtyMode = a.QuantityIsPerDay ? "за день" : "за доставку";
            string text =
$@"🤝 {a.PartnerName}
Активна: {(a.IsActive ? "Так" : "Ні")}
Тип доставки: {FormatTradeDeliveryModeUk(a.DeliveryMode)}
Час доставки: {FmtNumber(deliveryDays)} дн.
Формат кількості: {qtyMode}
Експортний ресурс: {(string.IsNullOrWhiteSpace(a.ExportResourceType) ? "-" : FormatResourceTypeUk(a.ExportResourceType))}
Імпортний ресурс: {(string.IsNullOrWhiteSpace(a.ImportResourceType) ? "-" : FormatResourceTypeUk(a.ImportResourceType))}
Експорт: {FmtNumberCompact(exportPerDayQty)} / день (за {FmtNumber(deliveryDays)} дн: {FmtNumberCompact(exportPerDeliveryQty)})
Імпорт: {FmtNumberCompact(importPerDayQty)} / день (за {FmtNumber(deliveryDays)} дн: {FmtNumberCompact(importPerDeliveryQty)})
Вартість (за день): експорт {FmtNumberCompact(exportTotal)} | імпорт {FmtNumberCompact(importTotal)}";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.EconomyTradeInfoKeyboard(agreementId, userId == masterID),
                cancellationToken
            );
        }

        private async Task StartTradeCreation(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await botClient.SendTextMessageAsync(userId, "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.", cancellationToken: cancellationToken);
                return;
            }

            EconomyInputState[userId] = EconomyInputMode.AddTrade;
            EconomyInputStep[userId] = 0;
            EconomyTradeBuffer[userId] = new EconomyTradeAgreementRecord
            {
                CountryId = selectedCountryId.Value,
                IsActive = true,
                PeriodDays = 1,
                DeliveryMode = "Caravan",
                QuantityIsPerDay = true
            };
            EconomyTradeFlowMode.Remove(userId);
            EconomyTradeUiContext[userId] = (chatId, msgId);

            await ShowTradePartnerSelect(botClient, userId, chatId, msgId, 0, cancellationToken);
        }

        private async Task HandleTradePartnerSelection(ITelegramBotClient botClient, long userId, long chatId, int msgId, string data, CancellationToken cancellationToken)
        {
            if (!EconomyTradeBuffer.TryGetValue(userId, out var agreement))
            {
                await botClient.SendTextMessageAsync(userId, "Немає активної угоди. Почніть створення заново.", cancellationToken: cancellationToken);
                ClearEconomyInput(userId);
                return;
            }

            if (data.EndsWith("_skip", StringComparison.OrdinalIgnoreCase))
            {
                agreement.PartnerCountryId = null;
                agreement.PartnerName = "-";
            }
            else if (data.Contains("_page_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseTrailingId(data, out int page))
                    await ShowTradePartnerSelect(botClient, userId, chatId, msgId, page, cancellationToken);
                return;
            }
            else
            {
                if (TryParseTrailingId(data, out int partnerId))
                {
                    if (partnerId == agreement.CountryId)
                    {
                        await ShowTradePartnerSelect(botClient, userId, chatId, msgId, 0, cancellationToken);
                        return;
                    }

                    var partner = CountriesData.GetCountryById(partnerId);
                    agreement.PartnerCountryId = partnerId;
                    agreement.PartnerName = partner?.Name ?? $"#{partnerId}";
                }
            }

            EconomyTradeFlowMode.Remove(userId);
            EconomyInputStep[userId] = 0;
            await ShowTradeFlowModeSelect(botClient, chatId, msgId, cancellationToken);
        }

        private async Task ShowTradePartnerSelect(ITelegramBotClient botClient, long userId, long chatId, int msgId, int page, CancellationToken cancellationToken)
        {
            int currentCountryId = 0;
            if (EconomyTradeBuffer.TryGetValue(userId, out var agreement) && agreement.CountryId > 0)
                currentCountryId = agreement.CountryId;
            if (currentCountryId <= 0)
                currentCountryId = GetSelectedCountryIdForUser(userId) ?? 0;

            var countries = CountriesData.GetAllCountries()
                .Where(c => c.CountryId > 0 && c.CountryId != currentCountryId)
                .ToList();
            int perPage = 10;
            if (page < 0) page = 0;
            int totalPages = (int)Math.Ceiling(countries.Count / (double)perPage);
            if (totalPages == 0) totalPages = 1;
            if (page >= totalPages) page = totalPages - 1;

            var pageItems = countries.Skip(page * perPage).Take(perPage).ToList();
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                "🤝 Оберіть країну-партнера (для угоди):",
                StateKeyboardBuilder.EconomyTradePartnerKeyboard(pageItems, page, totalPages),
                cancellationToken
            );
        }

        private async Task ShowTradeFlowModeSelect(ITelegramBotClient botClient, long chatId, int msgId, CancellationToken cancellationToken)
        {
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                "🔀 Оберіть тип торгової угоди:",
                StateKeyboardBuilder.EconomyTradeFlowModeKeyboard(),
                cancellationToken
            );
        }

        private async Task HandleTradeFlowModeSelection(ITelegramBotClient botClient, long userId, long chatId, int msgId, string data, CancellationToken cancellationToken)
        {
            if (!EconomyTradeBuffer.TryGetValue(userId, out var agreement))
            {
                await botClient.SendTextMessageAsync(userId, "Немає активної угоди. Почніть створення заново.", cancellationToken: cancellationToken);
                ClearEconomyInput(userId);
                return;
            }

            if (!TryParseTradeFlowMode(data, out var flowMode))
            {
                await ShowTradeFlowModeSelect(botClient, chatId, msgId, cancellationToken);
                return;
            }

            if ((flowMode == TradeFlowMode.ImportOnly || flowMode == TradeFlowMode.Both) &&
                (!agreement.PartnerCountryId.HasValue || agreement.PartnerCountryId.Value <= 0))
            {
                await SendHelperMessageAsync(botClient, userId, "❌ Для імпорту потрібно обрати країну-партнера.", cancellationToken);
                await ShowTradePartnerSelect(botClient, userId, chatId, msgId, 0, cancellationToken);
                return;
            }

            EconomyTradeFlowMode[userId] = flowMode;
            if (flowMode == TradeFlowMode.ExportOnly)
            {
                agreement.ImportResourceType = null;
                agreement.ImportQty = 0m;
                agreement.ImportUnitPrice = 0m;
            }
            else if (flowMode == TradeFlowMode.ImportOnly)
            {
                agreement.ExportResourceType = null;
                agreement.ExportQty = 0m;
                agreement.ExportUnitPrice = 0m;
            }

            EconomyInputStep[userId] = 0;
            await ProceedTradeCreation(botClient, userId, chatId, msgId, cancellationToken);
        }

        private async Task EndTradeAgreement(ITelegramBotClient botClient, long userId, long chatId, int msgId, int agreementId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var agreement = EconomyTradeAgreementsData
                .GetByCountry(selectedCountryId.Value)
                .FirstOrDefault(x => x.AgreementId == agreementId);

            bool ended = EconomyTradeAgreementsData.EndAgreement(agreementId, selectedCountryId.Value);
            bool mirrorEnded = ended && TryEndMirroredTradeAgreement(agreement);
            string text = ended
                ? (mirrorEnded ? "✅ Угоду завершено в обох країнах." : "✅ Угоду завершено.")
                : "❌ Угоду не завершено або немає доступу.";
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.EconomyTradeMenuKeyboard(userId == masterID),
                cancellationToken
            );
        }

        private async Task ShowTradeEditMenu(ITelegramBotClient botClient, long userId, long chatId, int msgId, int agreementId, CancellationToken cancellationToken)
        {
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                "💰 Економіка: Торгівля — редагування",
                StateKeyboardBuilder.EconomyTradeEditKeyboard(agreementId),
                cancellationToken
            );
        }

        private async Task StartBuildingCreation(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await botClient.SendTextMessageAsync(userId, "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.", cancellationToken: cancellationToken);
                return;
            }

            var building = new EconomyBuildingRecord
            {
                CountryId = selectedCountryId.Value,
                CityName = EconomyBuildingRecord.DefaultCityName
            };

            await StartBuildingCitySelection(botClient, userId, building, EconomyInputMode.AddBuilding, cancellationToken);
        }

        private async Task StartBuildingCitySelection(
            ITelegramBotClient botClient,
            long userId,
            EconomyBuildingRecord building,
            EconomyInputMode nextMode,
            CancellationToken cancellationToken)
        {
            await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
            ClearEconomyInput(userId);

            if (building == null || building.CountryId <= 0)
            {
                await botClient.SendTextMessageAsync(userId, "❌ Не знайдено дані для створення будівлі.", cancellationToken: cancellationToken);
                return;
            }

            var allBuildings = EconomyBuildingsData.GetByCountry(building.CountryId);
            EnsureCountryCityCatalog(building.CountryId, allBuildings);
            EconomyBuildingCitySelectionContext[userId] = new BuildingCitySelectionContext
            {
                CountryId = building.CountryId,
                NextMode = nextMode
            };
            EconomyBuildingCityChoices[userId] = GetAvailableBuildingCities(building.CountryId, allBuildings);
            building.CityName = NormalizeBuildingCityName(building.CityName);
            EconomyBuildingBuffer[userId] = building;
            EconomyInputState[userId] = EconomyInputMode.BuildingCityName;
            EconomyInputStep[userId] = 0;

            await ShowBuildingCitySelection(
                botClient,
                userId,
                chatId: 0,
                msgId: 0,
                page: 0,
                cancellationToken,
                editExistingMessage: false);
        }

        private async Task ShowBuildingCitySelection(
            ITelegramBotClient botClient,
            long userId,
            long chatId,
            int msgId,
            int page,
            CancellationToken cancellationToken,
            bool editExistingMessage)
        {
            if (!EconomyBuildingCitySelectionContext.TryGetValue(userId, out var cityContext) ||
                !EconomyBuildingBuffer.TryGetValue(userId, out var building))
            {
                await botClient.SendTextMessageAsync(userId, "❌ Контекст вибору міста втрачено. Почніть створення будівлі ще раз.", cancellationToken: cancellationToken);
                return;
            }

            if (!EconomyBuildingCityChoices.TryGetValue(userId, out var cities) || cities == null || cities.Count == 0)
            {
                var allBuildings = EconomyBuildingsData.GetByCountry(cityContext.CountryId);
                EnsureCountryCityCatalog(cityContext.CountryId, allBuildings);
                cities = GetAvailableBuildingCities(cityContext.CountryId, allBuildings);
                EconomyBuildingCityChoices[userId] = cities;
            }

            int perPage = 8;
            if (page < 0) page = 0;
            int totalPages = (int)Math.Ceiling(cities.Count / (double)perPage);
            if (totalPages < 1) totalPages = 1;
            if (page >= totalPages) page = totalPages - 1;

            int start = page * perPage;
            var pageCities = cities.Skip(start).Take(perPage).ToList();
            var pageItems = pageCities
                .Select((city, i) => ($"🏙 {city}", $"state_economy_building_city_pick_{start + i}"))
                .ToList();

            string text =
$@"🏙 Вибір міста для будівлі
Країна: #{cityContext.CountryId}
Поточний вибір: {FormatBuildingCityName(building.CityName)}

Оберіть місто, або створіть нове.";

            var keyboard = StateKeyboardBuilder.EconomyBuildingCityKeyboard(pageItems, page, totalPages);
            if (editExistingMessage && chatId != 0 && msgId != 0)
            {
                await SafeEditMessageTextAsync(botClient, chatId, msgId, text, keyboard, cancellationToken);
                return;
            }

            await SendHelperMessageAsync(botClient, userId, text, keyboard, cancellationToken);
        }

        private async Task StartBuildingNewCityInput(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            if (!EconomyBuildingCitySelectionContext.ContainsKey(userId) ||
                !EconomyBuildingBuffer.ContainsKey(userId))
            {
                await botClient.SendTextMessageAsync(userId, "❌ Контекст вибору міста втрачено. Почніть створення будівлі ще раз.", cancellationToken: cancellationToken);
                return;
            }

            EconomyInputState[userId] = EconomyInputMode.BuildingCityName;
            EconomyInputStep[userId] = 0;
            await SendHelperMessageAsync(botClient, userId, "Введіть назву нового міста:", cancellationToken);
        }

        private async Task SelectBuildingCityAndContinue(ITelegramBotClient botClient, long userId, int choiceIndex, CancellationToken cancellationToken)
        {
            if (!EconomyBuildingCitySelectionContext.TryGetValue(userId, out var cityContext) ||
                !EconomyBuildingBuffer.TryGetValue(userId, out var building))
            {
                await botClient.SendTextMessageAsync(userId, "❌ Контекст вибору міста втрачено. Почніть створення будівлі ще раз.", cancellationToken: cancellationToken);
                return;
            }

            if (!EconomyBuildingCityChoices.TryGetValue(userId, out var cities) || cities == null || cities.Count == 0)
            {
                var allBuildings = EconomyBuildingsData.GetByCountry(cityContext.CountryId);
                EnsureCountryCityCatalog(cityContext.CountryId, allBuildings);
                cities = GetAvailableBuildingCities(cityContext.CountryId, allBuildings);
                EconomyBuildingCityChoices[userId] = cities;
            }

            if (choiceIndex < 0 || choiceIndex >= cities.Count)
            {
                await SendHelperMessageAsync(botClient, userId, "❌ Невірний вибір міста.", cancellationToken);
                return;
            }

            string chosenCity = NormalizeBuildingCityName(cities[choiceIndex]);
            building.CityName = chosenCity;
            EconomyBuildingBuffer[userId] = building;

            await ContinueBuildingCreationAfterCitySelection(botClient, userId, cityContext.NextMode, cancellationToken);
        }

        private async Task HandleBuildingCityNameInput(ITelegramBotClient botClient, long userId, string input, CancellationToken cancellationToken)
        {
            if (!EconomyBuildingCitySelectionContext.TryGetValue(userId, out var cityContext) ||
                !EconomyBuildingBuffer.TryGetValue(userId, out var building))
            {
                await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
                ClearEconomyInput(userId);
                await botClient.SendTextMessageAsync(userId, "❌ Контекст вибору міста втрачено. Почніть створення будівлі ще раз.", cancellationToken: cancellationToken);
                return;
            }

            if (!TryNormalizeBuildingCityInput(input, out var newCity, out var error))
            {
                await SendHelperMessageAsync(botClient, userId, error, cancellationToken);
                return;
            }

            if (!EconomyBuildingCityChoices.TryGetValue(userId, out var cities) || cities == null)
            {
                var allBuildings = EconomyBuildingsData.GetByCountry(cityContext.CountryId);
                EnsureCountryCityCatalog(cityContext.CountryId, allBuildings);
                cities = GetAvailableBuildingCities(cityContext.CountryId, allBuildings);
            }

            if (!cities.Any(c => string.Equals(c, newCity, StringComparison.OrdinalIgnoreCase)))
            {
                cities.Add(newCity);
                cities = cities
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Select(NormalizeBuildingCityName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                int capitalIndex = cities.FindIndex(c => string.Equals(c, EconomyBuildingRecord.DefaultCityName, StringComparison.OrdinalIgnoreCase));
                if (capitalIndex > 0)
                {
                    string capital = cities[capitalIndex];
                    cities.RemoveAt(capitalIndex);
                    cities.Insert(0, capital);
                }
            }

            EconomyBuildingCityChoices[userId] = cities;
            building.CityName = newCity;
            EconomyBuildingBuffer[userId] = building;

            await ContinueBuildingCreationAfterCitySelection(botClient, userId, cityContext.NextMode, cancellationToken);
        }

        private async Task ContinueBuildingCreationAfterCitySelection(
            ITelegramBotClient botClient,
            long userId,
            EconomyInputMode nextMode,
            CancellationToken cancellationToken)
        {
            EconomyBuildingCitySelectionContext.Remove(userId);
            EconomyBuildingCityChoices.Remove(userId);

            EconomyInputState[userId] = nextMode;
            EconomyInputStep[userId] = 0;

            await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);

            if (nextMode == EconomyInputMode.AddBuildingTemplate)
            {
                await SendModeFieldPromptAsync(
                    botClient,
                    userId,
                    EconomyInputMode.AddBuildingTemplate,
                    BuildingTemplateFields[0].FieldName,
                    BuildingTemplateFields[0].Prompt,
                    cancellationToken);
                return;
            }

            await SendModeFieldPromptAsync(
                botClient,
                userId,
                EconomyInputMode.AddBuilding,
                BuildingFields[0].FieldName,
                BuildingFields[0].Prompt,
                cancellationToken);
        }

        private async Task CancelBuildingCitySelection(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
            ClearEconomyInput(userId);
            await botClient.SendTextMessageAsync(userId, "Дію скасовано.", cancellationToken: cancellationToken);
        }

        private async Task StartFestivalCreation(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await botClient.SendTextMessageAsync(userId, "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.", cancellationToken: cancellationToken);
                return;
            }

            EconomyInputState[userId] = EconomyInputMode.AddFestival;
            EconomyInputStep[userId] = 0;
            EconomyFestivalBuffer[userId] = new EconomyFestivalRecord { CountryId = selectedCountryId.Value, IsActive = true };

            await botClient.SendTextMessageAsync(userId, FestivalFields[0].Prompt, cancellationToken: cancellationToken);
        }

        public async Task HandleTextInput(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            if (message == null || message.From == null)
                return;
            if (message.Chat == null || message.Chat.Type != Telegram.Bot.Types.Enums.ChatType.Private)
                return;

            long userId = message.From.Id;
            if (ResearchInputState.TryGetValue(userId, out var researchMode) && researchMode != ResearchInputMode.None)
            {
                TrackUserMessage(userId, message.MessageId);
                string researchInput = (message.Text ?? string.Empty).Trim();
                if (string.Equals(researchInput, "скасувати", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(researchInput, "відміна", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(researchInput, "/cancel", StringComparison.OrdinalIgnoreCase))
                {
                    await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
                    ClearResearchInput(userId);
                    await botClient.SendTextMessageAsync(userId, "Дію скасовано.", cancellationToken: cancellationToken);
                    return;
                }
                await HandleResearchInput(botClient, userId, researchInput, cancellationToken);
                return;
            }

            if (AdminInputState.TryGetValue(userId, out var adminMode) && adminMode != AdminInputMode.None)
            {
                TrackUserMessage(userId, message.MessageId);
                string adminInput = (message.Text ?? string.Empty).Trim();
                if (string.Equals(adminInput, "скасувати", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(adminInput, "відміна", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(adminInput, "/cancel", StringComparison.OrdinalIgnoreCase))
                {
                    await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
                    ClearAdminInput(userId);
                    await botClient.SendTextMessageAsync(userId, "Дію скасовано.", cancellationToken: cancellationToken);
                    return;
                }

                await HandleAdminInput(botClient, userId, adminMode, adminInput, cancellationToken);
                return;
            }

            bool hasEdit = EconomyEditState.ContainsKey(userId);
            bool hasMode = EconomyInputState.TryGetValue(userId, out var mode) && mode != EconomyInputMode.None;
            if (hasEdit || hasMode)
                TrackUserMessage(userId, message.MessageId);

            string rawText = (message.Text ?? string.Empty).Trim();
            if (!hasEdit && !hasMode)
            {
                if (await TryHandlePublicSystemsTextCommand(botClient, userId, rawText, cancellationToken))
                    return;
            }

            if (EconomyEditState.TryGetValue(userId, out var edit))
            {
                await HandleEditInput(botClient, userId, edit, message.Text ?? string.Empty, cancellationToken);
                return;
            }
            if (!EconomyInputState.TryGetValue(userId, out mode) || mode == EconomyInputMode.None)
                return;

            string input = (message.Text ?? string.Empty).Trim();
            if (string.Equals(input, "скасувати", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(input, "відміна", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(input, "/cancel", StringComparison.OrdinalIgnoreCase))
            {
                if (mode == EconomyInputMode.AddBuilding || mode == EconomyInputMode.AddBuildingTemplate ||
                    mode == EconomyInputMode.AddTrade ||
                    mode == EconomyInputMode.AddCompany ||
                    mode == EconomyInputMode.AddMilitaryUnit ||
                    mode == EconomyInputMode.AddVassalObligation ||
                    mode == EconomyInputMode.BuildingCityName ||
                    mode == EconomyInputMode.CreateCityName ||
                    mode == EconomyInputMode.AddTreasury || mode == EconomyInputMode.RemoveTreasury ||
                    mode == EconomyInputMode.AddResource ||
                    mode == EconomyInputMode.SanctionsEfficiency ||
                    mode == EconomyInputMode.ProductSanctionsEfficiency ||
                    mode == EconomyInputMode.PreviewDays ||
                    mode == EconomyInputMode.IncomeLimit ||
                    mode == EconomyInputMode.PublicBudgetHealth ||
                    mode == EconomyInputMode.PublicBudgetEducation ||
                    mode == EconomyInputMode.PublicBudgetIntl ||
                    mode == EconomyInputMode.SectorRevenueMultiplier ||
                    mode == EconomyInputMode.SectorTaxOverride ||
                    mode == EconomyInputMode.CurrencyConvert || mode == EconomyInputMode.CurrencyAdjustRate ||
                    mode == EconomyInputMode.CurrencyTransfer || mode == EconomyInputMode.CurrencyLoan ||
                    mode == EconomyInputMode.CurrencyRepay ||
                    mode == EconomyInputMode.MilitaryAutoProvisionPercent)
                    await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
                ClearEconomyInput(userId);
                await botClient.SendTextMessageAsync(userId, "Дію скасовано.", cancellationToken: cancellationToken);
                return;
            }

            switch (mode)
            {
                case EconomyInputMode.AddBuilding:
                    await HandleBuildingInput(botClient, userId, input, cancellationToken);
                    break;
                case EconomyInputMode.BuildingCityName:
                    await HandleBuildingCityNameInput(botClient, userId, input, cancellationToken);
                    break;
                case EconomyInputMode.CreateCityName:
                    await HandleEconomyCityCreateInput(botClient, userId, input, cancellationToken);
                    break;
                case EconomyInputMode.AddCompany:
                    await HandleCompanyInput(botClient, userId, input, cancellationToken);
                    break;
                case EconomyInputMode.AddFestival:
                    await HandleFestivalInput(botClient, userId, input, cancellationToken);
                    break;
                case EconomyInputMode.AddTrade:
                    await HandleTradeInput(botClient, userId, input, cancellationToken);
                    break;
                case EconomyInputMode.AddMilitaryUnit:
                    await HandleMilitaryUnitInput(botClient, userId, input, cancellationToken);
                    break;
                case EconomyInputMode.AddVassalObligation:
                    await HandleVassalInput(botClient, userId, input, cancellationToken);
                    break;
                case EconomyInputMode.AddBuildingTemplate:
                    await HandleBuildingTemplateInput(botClient, userId, input, cancellationToken);
                    break;
                case EconomyInputMode.AddResource:
                    await HandleResourceInput(botClient, userId, input, cancellationToken);
                    break;
                case EconomyInputMode.AddTreasury:
                case EconomyInputMode.RemoveTreasury:
                    await HandleTreasuryInput(botClient, userId, input, mode == EconomyInputMode.AddTreasury, cancellationToken);
                    break;
                case EconomyInputMode.SanctionsEfficiency:
                    await HandleSanctionsEfficiencyInput(botClient, userId, input, cancellationToken);
                    break;
                case EconomyInputMode.ProductSanctionsEfficiency:
                    await HandleProductSanctionsEfficiencyInput(botClient, userId, input, cancellationToken);
                    break;
                case EconomyInputMode.PreviewDays:
                    await HandlePreviewDaysInput(botClient, userId, input, cancellationToken);
                    break;
                case EconomyInputMode.IncomeLimit:
                    await HandleIncomeLimitInput(botClient, userId, input, cancellationToken);
                    break;
                case EconomyInputMode.PublicBudgetHealth:
                case EconomyInputMode.PublicBudgetEducation:
                case EconomyInputMode.PublicBudgetIntl:
                    await HandlePublicBudgetInput(botClient, userId, input, mode, cancellationToken);
                    break;
                case EconomyInputMode.SectorRevenueMultiplier:
                    await HandleSectorRevenueMultiplierInput(botClient, userId, input, cancellationToken);
                    break;
                case EconomyInputMode.SectorTaxOverride:
                    await HandleSectorTaxOverrideInput(botClient, userId, input, cancellationToken);
                    break;
                case EconomyInputMode.CurrencyConvert:
                    await HandleCurrencyConvertInput(botClient, userId, input, cancellationToken);
                    break;
                case EconomyInputMode.CurrencyAdjustRate:
                    await HandleCurrencyAdjustInput(botClient, userId, input, cancellationToken);
                    break;
                case EconomyInputMode.CurrencyTransfer:
                    await HandleCurrencyTransferInput(botClient, userId, input, cancellationToken);
                    break;
                case EconomyInputMode.CurrencyLoan:
                    await HandleCurrencyLoanInput(botClient, userId, input, cancellationToken);
                    break;
                case EconomyInputMode.CurrencyRepay:
                    await HandleCurrencyRepayInput(botClient, userId, input, cancellationToken);
                    break;
                case EconomyInputMode.MilitaryAutoProvisionPercent:
                    await HandleMilitaryArsenalAutoProvisionInput(botClient, userId, input, cancellationToken);
                    break;
            }
        }

        public async Task<bool> TryCancelUserInputAsync(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            bool hasResearch = ResearchInputState.TryGetValue(userId, out var researchMode) && researchMode != ResearchInputMode.None;
            bool hasAdmin = AdminInputState.TryGetValue(userId, out var adminMode) && adminMode != AdminInputMode.None;
            bool hasEconomyMode = EconomyInputState.TryGetValue(userId, out var economyMode) && economyMode != EconomyInputMode.None;
            bool hasEconomyEdit = EconomyEditState.ContainsKey(userId);

            if (!hasResearch && !hasAdmin && !hasEconomyMode && !hasEconomyEdit)
                return false;

            await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);

            if (hasResearch)
                ClearResearchInput(userId);
            if (hasAdmin)
                ClearAdminInput(userId);
            if (hasEconomyMode || hasEconomyEdit)
            {
                EconomyEditState.Remove(userId);
                ClearEconomyInput(userId);
            }

            return true;
        }

        private async Task HandleCurrencyConvertInput(ITelegramBotClient botClient, long userId, string input, CancellationToken cancellationToken)
        {
            var parts = (input ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 1 || !TryParseDecimal(parts[0], out var amount))
            {
                await SendHelperMessageAsync(botClient, userId, "❌ Вкажіть суму для конвертації.", cancellationToken);
                return;
            }

            if (!EconomyCurrencyConvertState.TryGetValue(userId, out var ctx) ||
                string.IsNullOrWhiteSpace(ctx.FromCode) ||
                string.IsNullOrWhiteSpace(ctx.ToCode))
            {
                await SendHelperMessageAsync(botClient, userId, "❌ Спершу виберіть дві валюти.", cancellationToken);
                return;
            }

            var service = new CurrencyExchangeService();
            string from = service.ResolveCurrencyCode(ctx.FromCode);
            string to = service.ResolveCurrencyCode(ctx.ToCode);
            try
            {
                decimal result = service.ConvertCurrency(amount, from, to, useBlackMarket: false, out string details);
                await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
                ClearEconomyInput(userId);
                await UpdateCurrencyUiOrSend(botClient, userId, $"✅ Конвертація: {amount} {from} = {result:0.####} {to}\n{details}", cancellationToken);
            }
            catch (Exception ex)
            {
                await SendHelperMessageAsync(botClient, userId, $"❌ Помилка: {ex.Message}", cancellationToken);
            }
        }

        private async Task HandleCurrencyAdjustInput(ITelegramBotClient botClient, long userId, string input, CancellationToken cancellationToken)
        {
            if (userId != masterID)
            {
                await SendHelperMessageAsync(botClient, userId, "Нема доступу.", cancellationToken);
                return;
            }
            await SendHelperMessageAsync(botClient, userId, "❌ Використайте inline-меню для вибору валюти та ставки.", cancellationToken);
        }

        private async Task HandleCurrencyTransferInput(ITelegramBotClient botClient, long userId, string input, CancellationToken cancellationToken)
        {
            if (!EconomyCurrencyActionState.TryGetValue(userId, out var ctx) ||
                ctx.Action != CurrencyActionType.Transfer ||
                ctx.FromCountryId <= 0 ||
                ctx.ToCountryId <= 0)
            {
                await SendHelperMessageAsync(botClient, userId, "❌ Спершу оберіть країни в inline-меню.", cancellationToken);
                return;
            }

            if (userId != masterID && !IsSelectedCountryOwner(userId, ctx.FromCountryId))
            {
                await SendHelperMessageAsync(botClient, userId, "Нема доступу.", cancellationToken);
                EconomyCurrencyActionState.Remove(userId);
                EconomyInputState[userId] = EconomyInputMode.None;
                return;
            }

            if (!ctx.AwaitingTransferDaysInput)
            {
                await SendHelperMessageAsync(botClient, userId, "❌ Використайте inline-меню для вибору параметрів переказу.", cancellationToken);
                return;
            }

            string normalized = (input ?? string.Empty).Trim()
                .Replace(" ", string.Empty)
                .Replace("\u00A0", string.Empty);
            if (!int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out int days) ||
                days < 1 || days > 365)
            {
                await SendHelperMessageAsync(botClient, userId, "❌ Вкажіть кількість днів від 1 до 365.", cancellationToken);
                return;
            }

            ctx.TransferDays = days;
            ctx.AwaitingTransferDaysInput = false;
            EconomyCurrencyActionState[userId] = ctx;

            await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);

            if (EconomyCurrencyUiContext.TryGetValue(userId, out var ui))
            {
                await ShowCurrencyAmountSelect(botClient, userId, ui.ChatId, ui.MsgId, CurrencyActionType.Transfer, cancellationToken);
                return;
            }

            await SendHelperMessageAsync(botClient, userId, $"✅ Тривалість переказу: {days} дн. Тепер оберіть суму у меню.", cancellationToken);
        }

        private async Task HandleCurrencyLoanInput(ITelegramBotClient botClient, long userId, string input, CancellationToken cancellationToken)
        {
            if (!EconomyCurrencyActionState.TryGetValue(userId, out var ctx) || ctx.ToCountryId <= 0 || ctx.FromCountryId < 0)
            {
                await SendHelperMessageAsync(botClient, userId, "❌ Спершу оберіть країни в inline-меню.", cancellationToken);
                return;
            }
            if (!TryParseDecimal(input, out var amount) || amount <= 0m)
            {
                await SendHelperMessageAsync(botClient, userId, "❌ Невірна сума. Приклад: 10 000", cancellationToken);
                return;
            }

            if (userId == masterID)
            {
                var service = new CurrencyExchangeService();
                string result = service.Loan(ctx.FromCountryId, ctx.ToCountryId, amount, userId);
                await UpdateCurrencyUiOrSend(botClient, userId, result, cancellationToken);
                EconomyCurrencyActionState.Remove(userId);
                EconomyInputState[userId] = EconomyInputMode.None;
                return;
            }

            if (!IsSelectedCountryOwner(userId, ctx.ToCountryId))
            {
                await SendHelperMessageAsync(botClient, userId, "? Нема доступу.", cancellationToken);
                EconomyCurrencyActionState.Remove(userId);
                EconomyInputState[userId] = EconomyInputMode.None;
                return;
            }

            long requestId = Interlocked.Increment(ref NextLoanApprovalId);
            var request = new LoanApprovalRequest
            {
                RequestId = requestId,
                RequesterUserId = userId,
                FromCountryId = ctx.FromCountryId,
                ToCountryId = ctx.ToCountryId,
                Amount = amount,
                CreatedAtUtc = DateTime.UtcNow
            };
            LoanApprovalRequests[requestId] = request;

            string lenderLabel = FormatCountryLabel(ctx.FromCountryId);
            string receiverLabel = FormatCountryLabel(ctx.ToCountryId);
            string requesterName = ResolveUserDisplayName(userId);
            string text = $"📝 Запит на позику #{requestId}\n" +
                          $"Від: {lenderLabel}\n" +
                          $"Отримувач: {receiverLabel}\n" +
                          $"Сума: {FmtNumber(amount)}\n" +
                          $"Ініціатор: {requesterName}";

            await botClient.SendTextMessageAsync(
                chatId: masterID,
                text: text,
                replyMarkup: StateKeyboardBuilder.LoanApprovalKeyboard(requestId),
                cancellationToken: cancellationToken
            );

            await UpdateCurrencyUiOrSend(botClient, userId, "✅ Запит на позику надіслано. Очікуйте підтвердження.", cancellationToken);
            EconomyCurrencyActionState.Remove(userId);
            EconomyInputState[userId] = EconomyInputMode.None;
        }

        private static string FormatCountryLabel(int countryId)
        {
            if (countryId == 0)
                return "🏛️ Державний банк";

            var country = CountriesData.GetCountryById(countryId);
            if (country == null)
                return $"#{countryId}";

            string flag = string.IsNullOrWhiteSpace(country.FlagEmoji) ? "" : $"{country.FlagEmoji} ";
            string name = string.IsNullOrWhiteSpace(country.Name) ? $"#{country.CountryId}" : country.Name;
            return $"{flag}{name}";
        }

        private async Task HandleLoanApprovalDecision(ITelegramBotClient botClient, CallbackQuery callbackQuery, string data, CancellationToken cancellationToken)
        {
            if (callbackQuery.Message?.Chat == null)
                return;

            if (callbackQuery.From.Id != masterID)
            {
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id, "Нема доступу");
                return;
            }

            const string prefix = "state_economy_loanapprove_";
            string rest = data.Substring(prefix.Length);
            var parts = rest.Split('_', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || !long.TryParse(parts[0], out var requestId))
                return;

            bool approve = string.Equals(parts[1], "yes", StringComparison.OrdinalIgnoreCase);

            if (!LoanApprovalRequests.TryGetValue(requestId, out var request))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    callbackQuery.Message.Chat.Id,
                    callbackQuery.Message.MessageId,
                    "Запит вже оброблено.",
                    StateKeyboardBuilder.EmptyInlineKeyboard(),
                    cancellationToken
                );
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (approve)
            {
                var service = new CurrencyExchangeService();
                string result = service.Loan(request.FromCountryId, request.ToCountryId, request.Amount, request.RequesterUserId);
                await botClient.SendTextMessageAsync(request.RequesterUserId, $"✅ Позику схвалено.\n{result}", cancellationToken: cancellationToken);

                await SafeEditMessageTextAsync(
                    botClient,
                    callbackQuery.Message.Chat.Id,
                    callbackQuery.Message.MessageId,
                    $"✅ Схвалено.\n{result}",
                    StateKeyboardBuilder.EmptyInlineKeyboard(),
                    cancellationToken
                );
            }
            else
            {
                await botClient.SendTextMessageAsync(request.RequesterUserId, "❌ Запит на позику відхилено.", cancellationToken: cancellationToken);
                await SafeEditMessageTextAsync(
                    botClient,
                    callbackQuery.Message.Chat.Id,
                    callbackQuery.Message.MessageId,
                    "❌ Відхилено.",
                    StateKeyboardBuilder.EmptyInlineKeyboard(),
                    cancellationToken
                );
            }

            LoanApprovalRequests.Remove(requestId);
            await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
        }

        private static string FormatTradeFlowModeUk(TradeFlowMode mode)
        {
            return mode switch
            {
                TradeFlowMode.ExportOnly => "Лише експорт",
                TradeFlowMode.ImportOnly => "Лише імпорт",
                _ => "Експорт і імпорт"
            };
        }

        private static string NormalizeTradeDeliveryMode(string? value)
        {
            string key = NormalizeResourceKey(value ?? string.Empty);
            if (string.IsNullOrWhiteSpace(key))
                return "Caravan";

            if (key is "caravan" or "land" or "ground" or "carriage" or "wagon" or "pesho" or "pisho")
                return "Caravan";
            if (key is "waterway" or "water" or "sea" or "naval" or "ship" or "boat" or "river")
                return "Waterway";
            if (key is "skycourier" or "sky" or "air" or "aerial" or "gryphon" or "griffin" or "airship")
                return "SkyCourier";

            if (key.StartsWith("карав") || key.StartsWith("сух") || key.StartsWith("піш"))
                return "Caravan";
            if (key.StartsWith("вод") || key.StartsWith("мор") || key.StartsWith("річ") || key.StartsWith("кораб"))
                return "Waterway";
            if (key.StartsWith("повіт") || key.StartsWith("неб") || key.StartsWith("гриф"))
                return "SkyCourier";

            return "Caravan";
        }

        private static string FormatTradeDeliveryModeUk(string? value)
        {
            return NormalizeTradeDeliveryMode(value) switch
            {
                "Waterway" => "⛵ Водний шлях",
                "SkyCourier" => "🦅 Небесні кур'єри",
                _ => "🐎 Караванами"
            };
        }

        private static int GetTradeDeliveryDays(EconomyTradeAgreementRecord agreement)
        {
            return Math.Max(1, agreement?.PeriodDays ?? 1);
        }

        private static decimal GetTradeLegPerDayQty(EconomyTradeAgreementRecord agreement, bool isExport)
        {
            if (agreement == null)
                return 0m;

            decimal rawQty = Math.Max(0m, isExport ? agreement.ExportQty : agreement.ImportQty);
            if (rawQty <= 0m)
                return 0m;

            if (agreement.QuantityIsPerDay)
                return rawQty;

            int deliveryDays = GetTradeDeliveryDays(agreement);
            return rawQty / deliveryDays;
        }

        private static decimal GetTradeLegPerDeliveryQty(EconomyTradeAgreementRecord agreement, bool isExport)
        {
            if (agreement == null)
                return 0m;

            decimal rawQty = Math.Max(0m, isExport ? agreement.ExportQty : agreement.ImportQty);
            if (rawQty <= 0m)
                return 0m;

            if (!agreement.QuantityIsPerDay)
                return rawQty;

            int deliveryDays = GetTradeDeliveryDays(agreement);
            return rawQty * deliveryDays;
        }

        private static string FormatTradeUnitPrice(decimal unitPrice)
        {
            return unitPrice > 0m ? FmtNumberCompact(unitPrice) : "ринкова";
        }

        private static string FormatTradeLegText(
            string directionLabel,
            string? resourceType,
            decimal perDayQty,
            decimal perDeliveryQty,
            decimal unitPrice,
            int deliveryDays)
        {
            if (string.IsNullOrWhiteSpace(resourceType) || perDayQty <= 0m)
                return $"{directionLabel}: -";

            return $"{directionLabel}: {FormatResourceTypeUk(resourceType)} | {FmtNumberCompact(perDayQty)} / день | за {FmtNumber(deliveryDays)} дн: {FmtNumberCompact(perDeliveryQty)} | ціна {FormatTradeUnitPrice(unitPrice)}";
        }

        private static string BuildTradeApprovalText(TradeApprovalRequest request)
        {
            var agreement = request.Agreement;
            string requesterName = ResolveUserDisplayName(request.RequesterUserId);
            var sourceCountry = CountriesData.GetCountryById(agreement.CountryId);
            string sourceLabel = sourceCountry != null ? FormatCountryName(sourceCountry) : $"#{agreement.CountryId}";
            string partnerLabel = "-";
            if (agreement.PartnerCountryId.HasValue && agreement.PartnerCountryId.Value > 0)
            {
                var partnerCountry = CountriesData.GetCountryById(agreement.PartnerCountryId.Value);
                partnerLabel = partnerCountry != null ? FormatCountryName(partnerCountry) : $"#{agreement.PartnerCountryId.Value}";
            }
            else if (!string.IsNullOrWhiteSpace(agreement.PartnerName))
            {
                partnerLabel = agreement.PartnerName;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"📝 Запит на торгову угоду #{request.RequestId}");
            sb.AppendLine($"Ініціатор: {requesterName}");
            sb.AppendLine($"Країна: {sourceLabel}");
            sb.AppendLine($"Партнер: {partnerLabel}");
            sb.AppendLine($"Тип: {FormatTradeFlowModeUk(request.FlowMode)}");
            sb.AppendLine($"Тип доставки: {FormatTradeDeliveryModeUk(agreement.DeliveryMode)}");
            sb.AppendLine($"Час доставки: {FmtNumber(GetTradeDeliveryDays(agreement))} дн.");
            sb.AppendLine($"Формат кількості: {(agreement.QuantityIsPerDay ? "за день" : "за доставку")}");
            if (request.FlowMode != TradeFlowMode.ImportOnly)
                sb.AppendLine(FormatTradeLegText(
                    "📤 Експорт",
                    agreement.ExportResourceType,
                    GetTradeLegPerDayQty(agreement, isExport: true),
                    GetTradeLegPerDeliveryQty(agreement, isExport: true),
                    agreement.ExportUnitPrice,
                    GetTradeDeliveryDays(agreement)));
            if (request.FlowMode != TradeFlowMode.ExportOnly)
                sb.AppendLine(FormatTradeLegText(
                    "📥 Імпорт",
                    agreement.ImportResourceType,
                    GetTradeLegPerDayQty(agreement, isExport: false),
                    GetTradeLegPerDeliveryQty(agreement, isExport: false),
                    agreement.ImportUnitPrice,
                    GetTradeDeliveryDays(agreement)));
            return sb.ToString();
        }

        private async Task HandleTradeApprovalDecision(ITelegramBotClient botClient, CallbackQuery callbackQuery, string data, CancellationToken cancellationToken)
        {
            if (callbackQuery.Message?.Chat == null)
                return;

            if (callbackQuery.From.Id != masterID)
            {
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id, "Нема доступу");
                return;
            }

            const string prefix = "state_economy_tradeapprove_";
            string rest = data.Substring(prefix.Length);
            var parts = rest.Split('_', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || !long.TryParse(parts[0], out var requestId))
            {
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            bool approve = string.Equals(parts[1], "yes", StringComparison.OrdinalIgnoreCase);

            if (!TradeApprovalRequests.TryGetValue(requestId, out var request))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    callbackQuery.Message.Chat.Id,
                    callbackQuery.Message.MessageId,
                    "Запит вже оброблено.",
                    StateKeyboardBuilder.EmptyInlineKeyboard(),
                    cancellationToken
                );
                await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                return;
            }

            if (approve)
            {
                if (!TryValidateTradeAgreementForSubmission(request.Agreement, request.FlowMode, out string validationError, out _))
                {
                    await botClient.SendTextMessageAsync(
                        request.RequesterUserId,
                        $"❌ Запит на торгову угоду #{requestId} відхилено.\n{validationError}",
                        cancellationToken: cancellationToken);

                    await SafeEditMessageTextAsync(
                        botClient,
                        callbackQuery.Message.Chat.Id,
                        callbackQuery.Message.MessageId,
                        $"❌ Відхилено.\n{validationError}",
                        StateKeyboardBuilder.EmptyInlineKeyboard(),
                        cancellationToken
                    );
                    TradeApprovalRequests.Remove(requestId);
                    await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
                    return;
                }

                bool created = TryCreateTradeAgreementsPair(request.Agreement, out int agreementId, out int mirrorId, out string createError);
                if (created)
                {
                    await botClient.SendTextMessageAsync(
                        request.RequesterUserId,
                        mirrorId > 0
                            ? $"✅ Майстер схвалив торгову угоду #{requestId}. Створено Id={agreementId} та дзеркальну угоду Id={mirrorId}."
                            : $"✅ Майстер схвалив торгову угоду #{requestId}. Створено Id={agreementId}.",
                        cancellationToken: cancellationToken);

                    await SafeEditMessageTextAsync(
                        botClient,
                        callbackQuery.Message.Chat.Id,
                        callbackQuery.Message.MessageId,
                        mirrorId > 0
                            ? $"✅ Схвалено. Створено угоди Id={agreementId} та Id={mirrorId}."
                            : $"✅ Схвалено. Створено угоду Id={agreementId}.",
                        StateKeyboardBuilder.EmptyInlineKeyboard(),
                        cancellationToken
                    );
                }
                else
                {
                    await botClient.SendTextMessageAsync(
                        request.RequesterUserId,
                        $"❌ Запит на торгову угоду #{requestId} не вдалося застосувати через помилку збереження.",
                        cancellationToken: cancellationToken);

                    await SafeEditMessageTextAsync(
                        botClient,
                        callbackQuery.Message.Chat.Id,
                        callbackQuery.Message.MessageId,
                        string.IsNullOrWhiteSpace(createError)
                            ? "❌ Не вдалося створити торгову угоду після схвалення."
                            : $"❌ {createError}",
                        StateKeyboardBuilder.EmptyInlineKeyboard(),
                        cancellationToken
                    );
                }
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    request.RequesterUserId,
                    $"❌ Запит на торгову угоду #{requestId} відхилено майстром.",
                    cancellationToken: cancellationToken);

                await SafeEditMessageTextAsync(
                    botClient,
                    callbackQuery.Message.Chat.Id,
                    callbackQuery.Message.MessageId,
                    "❌ Відхилено.",
                    StateKeyboardBuilder.EmptyInlineKeyboard(),
                    cancellationToken
                );
            }

            TradeApprovalRequests.Remove(requestId);
            await SafeAnswerCallbackQueryAsync(botClient, callbackQuery.Id);
        }

        private async Task HandleCurrencyRepayInput(ITelegramBotClient botClient, long userId, string input, CancellationToken cancellationToken)
        {
            if (userId != masterID)
            {
                await SendHelperMessageAsync(botClient, userId, "Нема доступу.", cancellationToken);
                return;
            }
            await SendHelperMessageAsync(botClient, userId, "❌ Використайте inline-меню для вибору країн.", cancellationToken);
        }

        private async Task UpdateCurrencyUiOrSend(ITelegramBotClient botClient, long userId, string text, CancellationToken cancellationToken)
        {
            if (EconomyCurrencyUiContext.TryGetValue(userId, out var ctx))
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    ctx.ChatId,
                    ctx.MsgId,
                    text,
                    CurrencyExchangeService.IsEnabled
                        ? StateKeyboardBuilder.EconomyCurrencyMenuKeyboard(userId == masterID)
                        : StateKeyboardBuilder.EconomyCurrencyDisabledKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }
            await botClient.SendTextMessageAsync(userId, text, cancellationToken: cancellationToken);
        }

        private async Task HandleCurrencyConvertSelection(ITelegramBotClient botClient, long userId, long chatId, int msgId, string data, CancellationToken cancellationToken)
        {
            if (!EconomyCurrencyConvertState.TryGetValue(userId, out var ctx))
                ctx = new CurrencyConvertContext();

            if (data.Contains("_page_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseTrailingId(data, out int page))
                {
                    string target = data.Contains("currency_from_", StringComparison.OrdinalIgnoreCase) ? "from" : "to";
                    await ShowCurrencySelect(botClient, userId, chatId, msgId, target, page, cancellationToken);
                }
                return;
            }

            const string fromPrefix = "state_economy_currency_from_";
            const string toPrefix = "state_economy_currency_to_";
            if (data.StartsWith(fromPrefix, StringComparison.OrdinalIgnoreCase))
                ctx.FromCode = data.Substring(fromPrefix.Length);
            else if (data.StartsWith(toPrefix, StringComparison.OrdinalIgnoreCase))
                ctx.ToCode = data.Substring(toPrefix.Length);

            EconomyCurrencyConvertState[userId] = ctx;

            if (string.IsNullOrWhiteSpace(ctx.FromCode))
            {
                await ShowCurrencySelect(botClient, userId, chatId, msgId, "from", 0, cancellationToken);
                return;
            }

            if (string.IsNullOrWhiteSpace(ctx.ToCode))
            {
                await ShowCurrencySelect(botClient, userId, chatId, msgId, "to", 0, cancellationToken);
                return;
            }

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                $"💰 Економіка: {ctx.FromCode} > {ctx.ToCode}\nОберіть дію:",
                StateKeyboardBuilder.EconomyCurrencyMenuKeyboard(userId == masterID),
                cancellationToken
            );
        }

        private async Task ShowTimeMenu(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            if (!IsSelectedCountryOwner(userId, selectedCountryId) && userId != masterID)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Нема доступу.",
                    StateKeyboardBuilder.MainStateKeyboard(userId),
                    cancellationToken
                );
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            if (country == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Країну не знайдено.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            string text =
$@"⏳ Час
Країна: {(string.IsNullOrWhiteSpace(country.Name) ? $"#{country.CountryId}" : country.Name)}
Поточний день: {FmtNumber(country.TurnsSurvived)}

Оберіть, скільки днів пройде:";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.TimeMenuKeyboard(userId == masterID),
                cancellationToken
            );
        }

        private async Task ShowTimeDemography(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            if (!IsSelectedCountryOwner(userId, selectedCountryId) && userId != masterID)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Нема доступу.",
                    StateKeyboardBuilder.MainStateKeyboard(userId),
                    cancellationToken
                );
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            if (country == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Країну не знайдено.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var latest = EconomyTurnHistoryData.GetLatestTurn(country.CountryId);
            if (latest == null)
            {
                string emptyText =
$@"👶 Цикл населення
Країна: {FormatCountryName(country)}

Ще немає даних за ходи.
Запусти хоча б `1 день` у меню часу.";

                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    emptyText,
                    StateKeyboardBuilder.TimeMenuKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }

            long deathsTotal = latest.PopulationDeathsChildren
                + latest.PopulationDeathsWorking
                + latest.PopulationDeathsPensioners
                + latest.PopulationDeathsSocial;
            long startTotal = latest.PopulationStartChildren
                + latest.PopulationStartWorkingAge
                + latest.PopulationStartPensioners
                + latest.PopulationStartSocialProtected;
            long endTotal = latest.PopulationEndChildren
                + latest.PopulationEndWorkingAge
                + latest.PopulationEndPensioners
                + latest.PopulationEndSocialProtected;

            string growthReduction = latest.PopulationGrowthReduction > 0
                ? $"\nДодатковий спад (негативний приріст): {FmtNumber(latest.PopulationGrowthReduction)}"
                : string.Empty;

            string text =
$@"👶 Цикл населення (останній день)
Країна: {FormatCountryName(country)}
День: {FmtNumber(latest.TurnNumber)}

➕ Народжені: {FmtNumber(latest.PopulationBirths)}
🔁 Переходи:
👶 → 👨‍💼: {FmtNumber(latest.PopulationChildToWorking)}
👨‍💼 → 👴: {FmtNumber(latest.PopulationWorkingToPensioners)}
👨‍💼 → ♿: {FmtNumber(latest.PopulationWorkingToSocial)}
👴 → ♿: {FmtNumber(latest.PopulationPensionersToSocial)}

⚰️ Смертність:
👶: {FmtNumber(latest.PopulationDeathsChildren)}
👨‍💼: {FmtNumber(latest.PopulationDeathsWorking)}
👴: {FmtNumber(latest.PopulationDeathsPensioners)}
♿: {FmtNumber(latest.PopulationDeathsSocial)}
Всього: {FmtNumber(deathsTotal)}{growthReduction}

📊 Структура груп:
Старт: {FmtNumber(latest.PopulationStartChildren)} / {FmtNumber(latest.PopulationStartWorkingAge)} / {FmtNumber(latest.PopulationStartPensioners)} / {FmtNumber(latest.PopulationStartSocialProtected)}
Кінець: {FmtNumber(latest.PopulationEndChildren)} / {FmtNumber(latest.PopulationEndWorkingAge)} / {FmtNumber(latest.PopulationEndPensioners)} / {FmtNumber(latest.PopulationEndSocialProtected)}

👥 Населення: {FmtNumber(startTotal)} → {FmtNumber(endTotal)} (Δ {FmtDeltaNumber(endTotal - startTotal)})";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.TimeMenuKeyboard(userId == masterID),
                cancellationToken
            );
        }

        private async Task ShowTimeAutomationMenu(ITelegramBotClient botClient, long userId, long chatId, int msgId, int page, CancellationToken cancellationToken)
        {
            if (userId != masterID)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Нема доступу.",
                    StateKeyboardBuilder.MainStateKeyboard(userId),
                    cancellationToken
                );
                return;
            }

            var countries = CountriesData.GetAllCountries()
                .OrderBy(c => string.IsNullOrWhiteSpace(c.Name) ? $"#{c.CountryId}" : c.Name)
                .ToList();

            if (countries.Count == 0)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "🤖 ШІ-країни\nСписок країн порожній.",
                    StateKeyboardBuilder.TimeMenuKeyboard(true),
                    cancellationToken
                );
                return;
            }

            int totalPages = (int)Math.Ceiling(countries.Count / (double)TimeAutomationPageSize);
            if (totalPages <= 0)
                totalPages = 1;

            page = Math.Clamp(page, 0, totalPages - 1);
            var pageCountries = countries
                .Skip(page * TimeAutomationPageSize)
                .Take(TimeAutomationPageSize)
                .Select(c => (c.CountryId, FormatCountryName(c), IsTimeAutomationEnabled(c.CountryId)))
                .ToList();

            int enabledCount = countries.Count(c => IsTimeAutomationEnabled(c.CountryId));
            string text =
$@"🤖 ШІ-підтримка країн
Коли гравець прокручує день/час, ці країни теж автоматично отримують хід.
✅ увімкнено | ⬜ вимкнено

Активно: {FmtNumber(enabledCount)} з {FmtNumber(countries.Count)}
Сторінка: {FmtNumber(page + 1)}/{FmtNumber(totalPages)}";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.TimeAutomationCountriesKeyboard(pageCountries, page, totalPages),
                cancellationToken
            );
        }

        private async Task ToggleTimeAutomationCountry(ITelegramBotClient botClient, long userId, long chatId, int msgId, int countryId, CancellationToken cancellationToken)
        {
            if (userId != masterID)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Нема доступу.",
                    StateKeyboardBuilder.MainStateKeyboard(userId),
                    cancellationToken
                );
                return;
            }

            var country = CountriesData.GetCountryById(countryId);
            if (country == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Країну не знайдено.",
                    StateKeyboardBuilder.TimeMenuKeyboard(true),
                    cancellationToken
                );
                return;
            }

            bool nextEnabled = !IsTimeAutomationEnabled(countryId);
            SetTimeAutomationEnabled(countryId, nextEnabled);

            var allCountries = CountriesData.GetAllCountries()
                .OrderBy(c => string.IsNullOrWhiteSpace(c.Name) ? $"#{c.CountryId}" : c.Name)
                .ToList();
            int index = allCountries.FindIndex(c => c.CountryId == countryId);
            int page = index >= 0 ? index / TimeAutomationPageSize : 0;
            await ShowTimeAutomationMenu(botClient, userId, chatId, msgId, page, cancellationToken);
        }

        private (int AdvancedCountries, int FailedCountries) ApplyTimeAutomationTurns(int sourceCountryId, int days)
        {
            if (days <= 0)
                return (0, 0);

            int advanced = 0;
            int failed = 0;
            var service = new EconomyService();
            var researchService = new ResearchService();
            var autoCountries = CountriesData.GetAllCountries()
                .Where(c => c.CountryId > 0 && c.CountryId != sourceCountryId && IsTimeAutomationEnabled(c.CountryId))
                .ToList();

            foreach (var country in autoCountries)
            {
                bool countryFailed = false;
                for (int i = 0; i < days; i++)
                {
                    try
                    {
                        service.ApplyTurn(country.CountryId);
                        researchService.UpdateResearch(country.CountryId);
                    }
                    catch (Exception ex)
                    {
                        countryFailed = true;
                        _logger.LogWarning(ex, "AI turn failed. CountryId={CountryId} DayIndex={DayIndex}", country.CountryId, i + 1);
                        break;
                    }
                }

                if (countryFailed)
                    failed++;
                else
                    advanced++;
            }

            return (advanced, failed);
        }

        private async Task RunTimeAdvance(ITelegramBotClient botClient, long userId, long chatId, int msgId, int days, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            if (!IsSelectedCountryOwner(userId, selectedCountryId) && userId != masterID)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Нема доступу.",
                    StateKeyboardBuilder.MainStateKeyboard(userId),
                    cancellationToken
                );
                return;
            }

            days = Math.Clamp(days, 1, 365);
            var service = new EconomyService();
            var researchService = new ResearchService();
            var before = CountriesData.GetCountryById(selectedCountryId.Value);
            if (before == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Країну не знайдено.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var beforeBuildings = EconomyBuildingsData.GetByCountry(selectedCountryId.Value);
            var beforeFestivals = EconomyFestivalsData.GetActiveByCountry(selectedCountryId.Value);
            int completedResearchBefore = ResearchCompletedData.GetCompletedResearchIds(selectedCountryId.Value).Count;

            long incomeSum = 0;
            long expensesSum = 0;
            long netSum = 0;
            int crisisCount = 0;
            int advancedDays = 0;
            EconomyTurnResult? last = null;

            for (int i = 0; i < days; i++)
            {
                try
                {
                    last = service.ApplyTurn(selectedCountryId.Value);
                    researchService.UpdateResearch(selectedCountryId.Value);
                    incomeSum += last.Income;
                    expensesSum += last.Expenses;
                    netSum += last.NetResult;
                    if (last.CrisisTriggered)
                        crisisCount++;
                    advancedDays++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Time advance failed at day {Day} for country {CountryId}. UserId={UserId}", i + 1, selectedCountryId.Value, userId);
                    break;
                }
            }

            if (advancedDays == 0)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не вдалося прокрутити час. Перевірте налаштування економіки та спробуйте ще раз.",
                    StateKeyboardBuilder.TimeMenuKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }

            var ai = ApplyTimeAutomationTurns(selectedCountryId.Value, advancedDays);
            string aiText = (ai.AdvancedCountries > 0 || ai.FailedCountries > 0)
                ? $"\n\n🤖 ШІ-країни: {FmtNumber(ai.AdvancedCountries)} оновлено на {FmtNumber(advancedDays)} дн." +
                  (ai.FailedCountries > 0 ? $" | помилок: {FmtNumber(ai.FailedCountries)}" : string.Empty)
                : string.Empty;

            var after = CountriesData.GetCountryById(selectedCountryId.Value) ?? before;
            var afterBuildings = EconomyBuildingsData.GetByCountry(selectedCountryId.Value);
            var afterFestivals = EconomyFestivalsData.GetActiveByCountry(selectedCountryId.Value);
            int completedResearchAfter = ResearchCompletedData.GetCompletedResearchIds(selectedCountryId.Value).Count;
            int completedResearches = Math.Max(0, completedResearchAfter - completedResearchBefore);
            string researchStatus = researchService.GetActiveResearchStatus(selectedCountryId.Value).Replace("\n", " | ");

            int completedBuildings = CountCompletedBuildings(beforeBuildings, afterBuildings, out var completedTypes);
            int underConstruction = afterBuildings.Count(IsBuildingUnderConstruction);
            int festivalsEnded = CountEndedFestivals(beforeFestivals, afterFestivals, out var endedFestivalNames);
            string crisisText = crisisCount > 0 ? $"⚠ Кризових днів: {FmtNumber(crisisCount)}" : "Кризових днів не було";

            var history = EconomyTurnHistoryData.GetRecentTurns(selectedCountryId.Value, advancedDays);
            decimal productionSum = history.Sum(h => h.ProductionValue);
            decimal tradeSum = history.Sum(h => h.TradeIncome);
            decimal taxSum = history.Sum(h => h.TaxIncome);
            decimal corruptionSum = history.Sum(h => h.CorruptionLoss);
            decimal blackMarketQty = history.Sum(h => h.BlackMarketPurchased);
            decimal blackMarketValue = history.Sum(h => h.BlackMarketValue);
            float shortageAvg = history.Count > 0 ? history.Average(h => h.ShortageScore) : 0f;
            float happinessDeltaSum = history.Sum(h => h.HappinessDelta);
            float mortalityDeltaSum = history.Sum(h => h.MortalityDelta);
            float productivityDeltaSum = history.Sum(h => h.ProductivityDelta);
            decimal foodAvg = history.Count > 0 ? history.Average(h => h.FoodDemand) : 0m;
            decimal clothingAvg = history.Count > 0 ? history.Average(h => h.ClothingDemand) : 0m;
            decimal housingAvg = history.Count > 0 ? history.Average(h => h.HousingDemand) : 0m;
            decimal toolsAvg = history.Count > 0 ? history.Average(h => h.ToolsDemand) : 0m;
            decimal luxuryAvg = history.Count > 0 ? history.Average(h => h.LuxuryDemand) : 0m;

            string completedLabel = completedTypes.Count > 0
                ? string.Join("; ", completedTypes.Take(5)) + (completedTypes.Count > 5 ? $"; …ще {completedTypes.Count - 5}" : "")
                : "немає";
            var underConstructionTypes = afterBuildings
                .Where(IsBuildingUnderConstruction)
                .OrderBy(b => Math.Max(0, b.BuildTurns))
                .ThenBy(b => b.Type)
                .Select(b => $"{b.Type} ({FmtNumber(Math.Max(1, b.BuildTurns))} дн.)")
                .ToList();
            string underConstructionLabel = underConstructionTypes.Count > 0
                ? string.Join("; ", underConstructionTypes.Take(5)) + (underConstructionTypes.Count > 5 ? $"; …ще {underConstructionTypes.Count - 5}" : "")
                : "немає";
            string endedFestivalsLabel = endedFestivalNames.Count > 0
                ? string.Join("; ", endedFestivalNames.Take(5)) + (endedFestivalNames.Count > 5 ? $"; …ще {endedFestivalNames.Count - 5}" : "")
                : "немає";

            string text =
$@"⏳ Час: пройшло {FmtNumber(advancedDays)} дн.
Казна: {FmtNumber(before.Treasury)} → {FmtNumber(after.Treasury)} (Δ {FmtNumber(netSum)})
Доходи / витрати: {FmtNumber(incomeSum)} / {FmtNumber(expensesSum)} (≈ {FmtNumber(incomeSum / Math.Max(1, advancedDays))} / {FmtNumber(expensesSum / Math.Max(1, advancedDays))} за день)

📈 Економіка:
Виробництво: {FmtNumber(productionSum)}
Торгівля: {FmtNumber(tradeSum)}
Податки: {FmtNumber(taxSum)}
Корупційні втрати: {FmtNumber(corruptionSum)}
Чорний ринок: {FmtNumberCompact(blackMarketQty)} (вартість {FmtNumberCompact(blackMarketValue)})
Дефіцит ресурсів (середнє): {shortageAvg:0.##}
Ефекти дефіциту: щастя {FmtSigned(happinessDeltaSum)} / смертність {FmtSigned(mortalityDeltaSum)} / продуктивність {FmtSigned(productivityDeltaSum)}
Сер. попит: Food {FmtNumberCompact(foodAvg)}, Clothing {FmtNumberCompact(clothingAvg)}, Housing {FmtNumberCompact(housingAvg)}, Tools {FmtNumberCompact(toolsAvg)}, Luxury {FmtNumberCompact(luxuryAvg)}

👥 Соціум:
Населення: {FmtNumber(before.Population)} → {FmtNumber(after.Population)} (Δ {FmtDelta(after.Population - before.Population)})
Щастя: {before.Happiness:0.##} → {after.Happiness:0.##} (Δ {FmtDelta(after.Happiness - before.Happiness)})
Соц. напруга: {before.Unrest:0.##} → {after.Unrest:0.##} (Δ {FmtDelta(after.Unrest - before.Unrest)})
Стабільність: {before.Stability:0.##} → {after.Stability:0.##} (Δ {FmtDelta(after.Stability - before.Stability)})
Безробіття: {before.UnemploymentRate:0.##}% → {after.UnemploymentRate:0.##}%
Корупція: {before.Corruption:0.##} → {after.Corruption:0.##}
Інфляція: {before.Inflation:0.##}% → {after.Inflation:0.##}%

🛡 Військо та наука:
Армія: {FmtNumber(before.ArmySize)} → {FmtNumber(after.ArmySize)} (Δ {FmtDelta(after.ArmySize - before.ArmySize)})
Мораль: {before.ArmyMorale:0.##} → {after.ArmyMorale:0.##} (Δ {FmtDelta(after.ArmyMorale - before.ArmyMorale)})
Лояльність: {before.ArmyLoyalty:0.##} → {after.ArmyLoyalty:0.##} (Δ {FmtDelta(after.ArmyLoyalty - before.ArmyLoyalty)})
Утримання армії: {FmtNumberCompact(before.MilitaryUpkeep)} → {FmtNumberCompact(after.MilitaryUpkeep)}
Наука: рівень {before.ResearchLevel} → {after.ResearchLevel}, швидкість {before.ResearchSpeed:0.##} → {after.ResearchSpeed:0.##}
Завершено досліджень: {FmtNumber(completedResearches)}
Статус дослідження: {researchStatus}

🤝 Дипломатія:
Війна: {YesNo(after.WarState)}
Санкції: {((SanctionsLevel)after.SanctionsLevel)}
Союзники: {before.Allies} → {after.Allies}
Вороги: {before.Enemies} → {after.Enemies}
Репутація: {before.InternationalReputation:0.##} → {after.InternationalReputation:0.##}
Розвідка: {before.IntelLevel:0.##} → {after.IntelLevel:0.##}
Контррозвідка: {before.CounterIntelLevel:0.##} → {after.CounterIntelLevel:0.##}

🏗 Будівлі:
Завершено: {FmtNumber(completedBuildings)}
У будівництві: {FmtNumber(underConstruction)}
Всього: {FmtNumber(afterBuildings.Count)}
Список завершених: {completedLabel}
Список у будівництві: {underConstructionLabel}

🎭 Фестивалі:
Завершено: {FmtNumber(festivalsEnded)}
Активні: {FmtNumber(afterFestivals.Count)}
Список завершених: {endedFestivalsLabel}

{crisisText}{(advancedDays < days ? $"\n⚠ Виконано не всі дні: {FmtNumber(advancedDays)}/{FmtNumber(days)}." : string.Empty)}{aiText}";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.TimeMenuKeyboard(userId == masterID),
                cancellationToken
            );

            static int CountCompletedBuildings(List<EconomyBuildingRecord> beforeBuildings, List<EconomyBuildingRecord> afterBuildings, out List<string> completedTypes)
            {
                completedTypes = new List<string>();
                if (beforeBuildings == null || afterBuildings == null)
                    return 0;

                var afterById = afterBuildings.ToDictionary(b => b.BuildingId);
                var typeCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var b in beforeBuildings)
                {
                    if (!IsBuildingUnderConstruction(b))
                        continue;
                    if (!afterById.TryGetValue(b.BuildingId, out var a))
                        continue;
                    if (!IsBuildingUnderConstruction(a))
                    {
                        string type = string.IsNullOrWhiteSpace(a.Type) ? "Будівля" : a.Type;
                        typeCounts[type] = typeCounts.TryGetValue(type, out var c) ? c + 1 : 1;
                    }
                }

                foreach (var kv in typeCounts.OrderByDescending(k => k.Value).ThenBy(k => k.Key))
                    completedTypes.Add($"{kv.Key} x{kv.Value}");

                return typeCounts.Sum(k => k.Value);
            }

            static int CountEndedFestivals(List<EconomyFestivalRecord> beforeFestivals, List<EconomyFestivalRecord> afterFestivals, out List<string> endedNames)
            {
                endedNames = new List<string>();
                if (beforeFestivals == null || afterFestivals == null)
                    return 0;

                var afterIds = new HashSet<int>(afterFestivals.Select(f => f.FestivalId));
                foreach (var f in beforeFestivals)
                {
                    if (!afterIds.Contains(f.FestivalId))
                        endedNames.Add(string.IsNullOrWhiteSpace(f.Name) ? $"Фестиваль #{f.FestivalId}" : f.Name);
                }

                return endedNames.Count;
            }

            static string FmtDelta(double value)
            {
                if (value > 0) return $"+{value:0.##}";
                if (value < 0) return $"{value:0.##}";
                return "0";
            }

            static string FmtSigned(float value)
            {
                if (value > 0) return $"+{value:0.###}";
                if (value < 0) return $"{value:0.###}";
                return "0";
            }

            static string YesNo(bool value) => value ? "Так" : "Ні";
        }

        private async Task ShowCurrencySelect(ITelegramBotClient botClient, long userId, long chatId, int msgId, string target, int page, CancellationToken cancellationToken)
        {
            var currencies = EconomyCurrencyData.GetAll()
                .Select(c => (code: c.CurrencyCode, name: c.CurrencyName))
                .ToList();
            currencies.Insert(0, ("GOLD", "Gold"));

            int perPage = 10;
            if (page < 0) page = 0;
            int totalPages = (int)Math.Ceiling(currencies.Count / (double)perPage);
            if (totalPages == 0) totalPages = 1;
            if (page >= totalPages) page = totalPages - 1;

            var pageItems = currencies.Skip(page * perPage).Take(perPage).ToList();
            string title = target == "from"
                ? "💱 Оберіть валюту (з якої конвертуємо):"
                : "💱 Оберіть валюту (в яку конвертуємо):";

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                title,
                StateKeyboardBuilder.EconomyCurrencySelectKeyboard(pageItems, page, totalPages, target),
                cancellationToken
            );
        }

        private static bool TryParseDecimal(string raw, out decimal value)
        {
            value = 0m;
            if (string.IsNullOrWhiteSpace(raw))
                return false;
            string normalized = raw.Trim()
                .Replace(" ", string.Empty)
                .Replace("\u00A0", string.Empty)
                .Replace(',', '.');
            return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        private async Task HandleCurrencyActionSelection(ITelegramBotClient botClient, long userId, long chatId, int msgId, string data, CancellationToken cancellationToken)
        {
            const string prefix = "state_economy_currency_action_";
            string rest = data.Substring(prefix.Length);
            var parts = rest.Split('_', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                return;

            if (!TryParseCurrencyAction(parts[0], out var action))
                return;
            string target = parts[1];
            bool isMaster = userId == masterID;
            int? selectedCountryId = GetSelectedCountryIdForUser(userId);

            if (!EconomyCurrencyActionState.TryGetValue(userId, out var ctx))
                ctx = new CurrencyActionContext { Action = action, TransferDays = 1 };
            ctx.Action = action;
            if (ctx.TransferDays <= 0)
                ctx.TransferDays = 1;
            EconomyCurrencyActionState[userId] = ctx;

            if (target == "from" || target == "to" || target == "single")
            {
                if (parts.Length >= 3 && string.Equals(parts[2], "page", StringComparison.OrdinalIgnoreCase))
                {
                    if (parts.Length >= 4 && int.TryParse(parts[3], out int page))
                        await ShowCurrencyCountrySelect(botClient, userId, chatId, msgId, action, target, page, cancellationToken);
                    return;
                }

                if (parts.Length < 3 || !int.TryParse(parts[2], out int countryId))
                    return;

                if (target == "from")
                {
                    if (action == CurrencyActionType.Transfer && !isMaster)
                    {
                        await SafeEditMessageTextAsync(
                            botClient,
                            chatId,
                            msgId,
                            "Нема доступу до вибору країни-відправника.",
                            StateKeyboardBuilder.EconomyCurrencyMenuKeyboard(false),
                            cancellationToken
                        );
                        return;
                    }

                    ctx.FromCountryId = countryId;
                    ctx.AwaitingTransferDaysInput = false;
                    EconomyCurrencyActionState[userId] = ctx;
                    if (action == CurrencyActionType.Loan)
                    {
                        await ShowCurrencyAmountInput(botClient, userId, chatId, msgId, action, cancellationToken);
                        return;
                    }
                    await ShowCurrencyCountrySelect(botClient, userId, chatId, msgId, action, "to", 0, cancellationToken);
                    return;
                }
                if (target == "to")
                {
                    if (action == CurrencyActionType.Transfer)
                    {
                        if (!isMaster)
                        {
                            if (!selectedCountryId.HasValue || !IsSelectedCountryOwner(userId, selectedCountryId.Value))
                            {
                                await SafeEditMessageTextAsync(
                                    botClient,
                                    chatId,
                                    msgId,
                                    "Нема доступу.",
                                    StateKeyboardBuilder.EconomyCurrencyMenuKeyboard(false),
                                    cancellationToken
                                );
                                return;
                            }

                            ctx.FromCountryId = selectedCountryId.Value;
                        }

                        if (ctx.FromCountryId <= 0)
                        {
                            await SendHelperMessageAsync(botClient, userId, "❌ Спершу оберіть країну-відправника.", cancellationToken);
                            return;
                        }

                        if (countryId == ctx.FromCountryId)
                        {
                            await SendHelperMessageAsync(botClient, userId, "❌ Країна-отримувач має бути іншою.", cancellationToken);
                            return;
                        }
                    }

                    ctx.ToCountryId = countryId;
                    ctx.AwaitingTransferDaysInput = false;
                    EconomyCurrencyActionState[userId] = ctx;
                    if (action == CurrencyActionType.Loan)
                    {
                        await ShowCurrencyCountrySelect(botClient, userId, chatId, msgId, action, "from", 0, cancellationToken);
                        return;
                    }
                    if (action == CurrencyActionType.Transfer)
                    {
                        await ShowCurrencyTransferDaysSelect(botClient, userId, chatId, msgId, action, cancellationToken);
                        return;
                    }
                    await ShowCurrencyAmountSelect(botClient, userId, chatId, msgId, action, cancellationToken);
                    return;
                }
                if (target == "single")
                {
                    ctx.FromCountryId = countryId;
                    EconomyCurrencyActionState[userId] = ctx;
                    if (action == CurrencyActionType.AdjustRate)
                        await ShowCurrencyRateSelect(botClient, userId, chatId, msgId, action, cancellationToken);
                    else
                        await ShowCurrencyAmountSelect(botClient, userId, chatId, msgId, action, cancellationToken);
                    return;
                }
            }

            if (target == "days")
            {
                if (action != CurrencyActionType.Transfer || parts.Length < 3)
                    return;

                if (!isMaster && !IsSelectedCountryOwner(userId, ctx.FromCountryId))
                {
                    await SafeEditMessageTextAsync(
                        botClient,
                        chatId,
                        msgId,
                        "Нема доступу.",
                        StateKeyboardBuilder.EconomyCurrencyMenuKeyboard(false),
                        cancellationToken
                    );
                    return;
                }

                if (ctx.FromCountryId <= 0 || ctx.ToCountryId <= 0)
                {
                    await SendHelperMessageAsync(botClient, userId, "❌ Спершу оберіть країни для переказу.", cancellationToken);
                    return;
                }

                if (string.Equals(parts[2], "custom", StringComparison.OrdinalIgnoreCase))
                {
                    ctx.AwaitingTransferDaysInput = true;
                    EconomyCurrencyActionState[userId] = ctx;
                    EconomyInputState[userId] = EconomyInputMode.CurrencyTransfer;

                    await SendHelperMessageAsync(
                        botClient,
                        userId,
                        "Вкажіть кількість днів для траншу (1..365).",
                        cancellationToken);
                    return;
                }

                if (!int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int days) ||
                    days < 1 || days > 365)
                {
                    await SendHelperMessageAsync(botClient, userId, "❌ Невірна тривалість траншу.", cancellationToken);
                    return;
                }

                ctx.TransferDays = days;
                ctx.AwaitingTransferDaysInput = false;
                EconomyCurrencyActionState[userId] = ctx;
                await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
                await ShowCurrencyAmountSelect(botClient, userId, chatId, msgId, action, cancellationToken);
                return;
            }

            if (target == "amount")
            {
                if (parts.Length < 3 || !TryParseDecimal(parts[2], out var amount))
                    return;
                await ApplyCurrencyAmountAction(botClient, userId, chatId, msgId, ctx, amount, cancellationToken);
                return;
            }

            if (target == "rate")
            {
                if (parts.Length < 3 || !TryParseDecimal(parts[2], out var rate))
                    return;
                await ApplyCurrencyRateAction(botClient, userId, chatId, msgId, ctx, rate, cancellationToken);
                return;
            }
        }

        private async Task ShowCurrencyCountrySelect(ITelegramBotClient botClient, long userId, long chatId, int msgId, CurrencyActionType action, string target, int page, CancellationToken cancellationToken)
        {
            var all = CountriesData.GetAllCountries();
            if (action == CurrencyActionType.Transfer &&
                target == "to" &&
                EconomyCurrencyActionState.TryGetValue(userId, out var transferCtx) &&
                transferCtx.FromCountryId > 0)
            {
                all = all.Where(c => c.CountryId != transferCtx.FromCountryId).ToList();
            }

            int perPage = 10;
            if (page < 0) page = 0;
            int totalPages = (int)Math.Ceiling(all.Count / (double)perPage);
            if (totalPages == 0) totalPages = 1;
            if (page >= totalPages) page = totalPages - 1;
            var pageItems = all.Skip(page * perPage).Take(perPage).ToList();

            string title = target switch
            {
                "from" => "Оберіть країну-відправника:",
                "to" => "Оберіть країну-отримувача:",
                _ => "Оберіть країну:"
            };

            var rows = new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton[]>();

            if (action == CurrencyActionType.Loan && target == "from")
            {
                rows.Add(new[] { Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("🏛 Держбанк", $"state_economy_currency_action_{ActionToToken(action)}_{target}_0") });
            }

            foreach (var c in pageItems)
            {
                string flag = string.IsNullOrWhiteSpace(c.FlagEmoji) ? "" : $"{c.FlagEmoji} ";
                string name = string.IsNullOrWhiteSpace(c.Name) ? $"#{c.CountryId}" : c.Name;
                rows.Add(new[] { Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData($"{flag}{name}", $"state_economy_currency_action_{ActionToToken(action)}_{target}_{c.CountryId}") });
            }

            if (totalPages > 1)
            {
                var nav = new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>();
                if (page > 0)
                    nav.Add(Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("⬅ Попередні", $"state_economy_currency_action_{ActionToToken(action)}_{target}_page_{page - 1}"));
                if (page < totalPages - 1)
                    nav.Add(Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("➡ Наступні", $"state_economy_currency_action_{ActionToToken(action)}_{target}_page_{page + 1}"));
                if (nav.Count > 0)
                    rows.Add(nav.ToArray());
            }

            rows.Add(new[] { Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("🔙 Назад", "state_economy_currency") });

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                title,
                new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(rows),
                cancellationToken
            );
        }

        private async Task ShowCurrencyAmountSelect(ITelegramBotClient botClient, long userId, long chatId, int msgId, CurrencyActionType action, CancellationToken cancellationToken)
        {
            var amounts = new List<decimal> { 100m, 500m, 1000m, 2500m, 5000m, 10000m, 25000m, 50000m };
            string title = action switch
            {
                CurrencyActionType.Transfer when CurrencyExchangeService.IsEnabled => "Оберіть суму (у валюті відправника):",
                CurrencyActionType.Transfer => "Оберіть суму (Gold):",
                _ => "Оберіть суму (Gold):"
            };

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                title,
                StateKeyboardBuilder.EconomyCurrencyAmountKeyboard(amounts, ActionToToken(action)),
                cancellationToken
            );
        }

        private async Task ShowBuildingTemplateLevelPreview(ITelegramBotClient botClient, long userId, long chatId, int msgId, string data, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            if (!TryParseTemplateLevelPreviewData(data, out string key, out int requestedLevel))
            {
                int fallbackTotalPages = GetBuildingTemplatePageCount();
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Невірний формат предпросмотру шаблону.",
                    StateKeyboardBuilder.EconomyBuildingTemplatesKeyboard(GetBuildingTemplatePageItems(0), 0, fallbackTotalPages),
                    cancellationToken
                );
                return;
            }

            var building = BuildTemplateFromCallback(selectedCountryId.Value, key);
            if (building == null || !TryGetBuildingTemplateDefinition(key, out var definition))
            {
                int fallbackTotalPages = GetBuildingTemplatePageCount();
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Шаблон не знайдено.",
                    StateKeyboardBuilder.EconomyBuildingTemplatesKeyboard(GetBuildingTemplatePageItems(0), 0, fallbackTotalPages),
                    cancellationToken
                );
                return;
            }

            var levels = BuildTemplateLevelSnapshots(building, MaxBuildingLevel);
            int safeLevel = Math.Clamp(requestedLevel, 1, levels.Count);
            var currentLevel = levels[safeLevel - 1];
            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            float inflationPct = country?.Inflation ?? 0f;

            decimal totalCreationCharge = GetBuildingCreationCost(building, inflationPct);
            int totalCreationTurns = Math.Max(0, building.BuildTurns);
            var previewTemplate = CloneBuildingForSimulation(building);
            previewTemplate.Level = safeLevel;
            if (TryProjectTemplateBuildingForCreation(previewTemplate, inflationPct, out var projectedPreview, out decimal projectedCharge, out _))
            {
                totalCreationCharge = projectedCharge;
                totalCreationTurns = Math.Max(0, projectedPreview.BuildTurns);
            }

            string text = BuildTemplateLevelPreviewText(building, definition, currentLevel, totalCreationCharge, totalCreationTurns);

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.EconomyBuildingTemplateLevelPreviewKeyboard(definition.Key, safeLevel, levels.Count),
                cancellationToken
            );
        }

        private sealed class BuildingUpgradeProjection
        {
            public EconomyBuildingRecord Building { get; set; } = new();
            public decimal UpgradeCostStep { get; set; }
            public decimal UpgradeCostTotal { get; set; }
            public int UpgradeTurnsStep { get; set; }
            public int UpgradeTurnsTotal { get; set; }
        }

        private async Task ShowEconomyBuildingUpgradePreview(ITelegramBotClient botClient, long userId, long chatId, int msgId, int buildingId, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            int countryId = selectedCountryId.Value;
            var country = CountriesData.GetCountryById(countryId);
            if (country == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Країну не знайдено.",
                    StateKeyboardBuilder.PublicOverviewKeyboard(),
                    cancellationToken
                );
                return;
            }

            var building = EconomyBuildingsData.GetByCountry(countryId).FirstOrDefault(x => x.BuildingId == buildingId);
            if (building == null)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    "Будівлю не знайдено.",
                    StateKeyboardBuilder.EconomyBuildingsKeyboard(userId == masterID),
                    cancellationToken
                );
                return;
            }

            var projections = BuildBuildingUpgradeProjections(building, country.Inflation, MaxBuildingLevel);
            if (projections.Count <= 1)
            {
                await SafeEditMessageTextAsync(
                    botClient,
                    chatId,
                    msgId,
                    $"🏗 {building.Type}\nМаксимальний рівень уже досягнуто (L{FmtNumber(building.Level)}).",
                    StateKeyboardBuilder.EconomyBuildingInfoKeyboard(buildingId, userId == masterID),
                    cancellationToken
                );
                return;
            }

            string text = BuildBuildingUpgradePreviewText(building, projections, country.Inflation);
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                text,
                StateKeyboardBuilder.EconomyBuildingInfoKeyboard(buildingId, userId == masterID),
                cancellationToken
            );
        }

        private static string BuildBuildingUpgradePreviewText(
            EconomyBuildingRecord current,
            IReadOnlyList<BuildingUpgradeProjection> projections,
            float inflationPct)
        {
            var sb = new StringBuilder();
            var currentProjection = projections[0];

            bool isHousing = string.Equals(current.Category, "Housing", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(current.ResourceType, "Housing", StringComparison.OrdinalIgnoreCase);
            bool hasHouseBenchmark = TryGetHouseUpgradeBenchmark(inflationPct, out decimal houseCost, out decimal houseOutput, out decimal houseEfficiency);

            sb.AppendLine($"🔎 Прев'ю покращення: {current.Type}");
            sb.AppendLine($"📍 Місто: {FormatBuildingCityName(current.CityName)}");
            sb.AppendLine($"🏷 Категорія: {FormatBuildingCategoryUk(current.Category)}");
            sb.AppendLine($"📊 Поточний рівень: L{FmtNumber(current.Level)}");
            sb.AppendLine($"📈 Діапазон: L{FmtNumber(Math.Min(MaxBuildingLevel, current.Level + 1))}..L{FmtNumber(MaxBuildingLevel)}");
            sb.AppendLine();
            sb.AppendLine($"L{FmtNumber(currentProjection.Building.Level)} зараз:");
            sb.AppendLine($"• Робочі місця: {FmtNumber(currentProjection.Building.JobsProvided)}");
            sb.AppendLine($"• Утримання: {FmtNumberCompact(currentProjection.Building.UpkeepCost)} / день");
            sb.AppendLine($"• Виробництво: {BuildBuildingOutputSummary(currentProjection.Building)}");
            sb.AppendLine($"• Бонуси: {BuildBuildingBonusSummary(currentProjection.Building)}");
            sb.AppendLine();

            for (int i = 1; i < projections.Count; i++)
            {
                var previous = projections[i - 1];
                var next = projections[i];

                sb.AppendLine($"L{FmtNumber(next.Building.Level)} (крок з L{FmtNumber(previous.Building.Level)}):");
                sb.AppendLine($"• Ціна кроку: {FmtNumber(next.UpgradeCostStep)} | Сумарно: {FmtNumber(next.UpgradeCostTotal)}");
                sb.AppendLine($"• Час кроку: {FmtNumber(next.UpgradeTurnsStep)} дн. | Сумарно: {FmtNumber(next.UpgradeTurnsTotal)} дн.");
                sb.AppendLine($"• Робочі місця: {FmtNumber(next.Building.JobsProvided)}");
                sb.AppendLine($"• Утримання: {FmtNumberCompact(next.Building.UpkeepCost)} / день");
                sb.AppendLine($"• Виробництво: {BuildBuildingOutputSummary(next.Building)}");
                sb.AppendLine($"• Бонуси: {BuildBuildingBonusSummary(next.Building)}");

                if (isHousing && hasHouseBenchmark)
                {
                    decimal housingGain = Math.Max(0m, next.Building.BaseProduction - previous.Building.BaseProduction);
                    decimal stepEfficiency = next.UpgradeCostStep > 0m ? housingGain / next.UpgradeCostStep : 0m;
                    string verdict = stepEfficiency >= houseEfficiency ? "✅ вигідніше за новий дім" : "⚠ слабше за новий дім";
                    sb.AppendLine($"• Приріст житла: +{FmtNumberCompact(housingGain)} / день ({verdict})");
                    sb.AppendLine($"• Орієнтир нового дому: {FmtNumberCompact(houseOutput)} житла за {FmtNumber(houseCost)}");
                }

                if (i < projections.Count - 1)
                    sb.AppendLine();
            }

            sb.AppendLine();
            sb.Append("Натисни \"⬆ Покращити\", щоб виконати лише один наступний крок.");
            return sb.ToString();
        }

        private static string BuildBuildingOutputSummary(EconomyBuildingRecord building)
        {
            if (building.BaseProduction <= 0m || string.IsNullOrWhiteSpace(building.ResourceType))
                return "немає";

            return $"{FmtNumberCompact(building.BaseProduction)} {FormatResourceTypeUk(building.ResourceType)} / день";
        }

        private static string BuildBuildingBonusSummary(EconomyBuildingRecord building)
        {
            var parts = new List<string>();
            if (Math.Abs(building.StabilityBonus) > 0.0001f)
                parts.Add($"стабільність +{FmtNumberCompact(building.StabilityBonus)}");
            if (Math.Abs(building.CorruptionReduction) > 0.0001f)
                parts.Add($"антикорупція +{FmtNumberCompact(building.CorruptionReduction)}");
            if (Math.Abs(building.ResearchBonus) > 0.0001f)
                parts.Add($"наука +{FmtNumberCompact(building.ResearchBonus)}");
            if (Math.Abs(building.PopulationGrowthBonus) > 0.0001f)
                parts.Add($"ріст населення +{FmtNumberCompact(building.PopulationGrowthBonus)}");
            if (Math.Abs(building.FestivalBoost) > 0.0001f)
                parts.Add($"фестивалі +{FmtNumberCompact(building.FestivalBoost)}");
            if (building.ArmyBonus != 0)
                parts.Add($"армія +{FmtNumber(building.ArmyBonus)}");

            return parts.Count == 0 ? "немає" : string.Join(", ", parts);
        }

        private static bool TryGetHouseUpgradeBenchmark(float inflationPct, out decimal houseCost, out decimal houseOutput, out decimal houseEfficiency)
        {
            houseCost = 0m;
            houseOutput = 0m;
            houseEfficiency = 0m;

            if (!TryGetBuildingTemplateDefinition("housing", out var housingTemplate))
                return false;

            if (housingTemplate.BuildCost <= 0m || housingTemplate.BaseProduction <= 0m)
                return false;

            decimal inflationMultiplier = EconomyService.GetInflationPriceMultiplier(inflationPct);
            houseCost = decimal.Ceiling(housingTemplate.BuildCost * inflationMultiplier);
            houseOutput = housingTemplate.BaseProduction;
            houseEfficiency = houseCost > 0m ? houseOutput / houseCost : 0m;
            return houseEfficiency > 0m;
        }

        private static List<BuildingUpgradeProjection> BuildBuildingUpgradeProjections(EconomyBuildingRecord building, float inflationPct, int maxTargetLevel)
        {
            var projections = new List<BuildingUpgradeProjection>();
            if (building == null)
                return projections;

            int safeTargetLevel = Math.Clamp(maxTargetLevel, Math.Max(1, building.Level), MaxBuildingLevel);
            var current = CloneBuildingForSimulation(building);
            current.Level = Math.Max(1, current.Level);

            projections.Add(new BuildingUpgradeProjection
            {
                Building = CloneBuildingForSimulation(current),
                UpgradeCostStep = 0m,
                UpgradeCostTotal = 0m,
                UpgradeTurnsStep = 0,
                UpgradeTurnsTotal = 0
            });

            decimal totalCost = 0m;
            int totalTurns = 0;
            while (current.Level < safeTargetLevel)
            {
                decimal stepCost = CalculateBuildingUpgradeCost(current, inflationPct);
                bool upgraded = TryApplyBuildingUpgradeProgression(current);
                if (!upgraded)
                    break;

                int stepTurns = Math.Max(1, current.BuildTurns);
                totalCost += stepCost;
                totalTurns += stepTurns;

                projections.Add(new BuildingUpgradeProjection
                {
                    Building = CloneBuildingForSimulation(current),
                    UpgradeCostStep = stepCost,
                    UpgradeCostTotal = totalCost,
                    UpgradeTurnsStep = stepTurns,
                    UpgradeTurnsTotal = totalTurns
                });
            }

            return projections;
        }

        private static EconomyBuildingRecord CloneBuildingForSimulation(EconomyBuildingRecord source)
        {
            return new EconomyBuildingRecord
            {
                BuildingId = source.BuildingId,
                CountryId = source.CountryId,
                CityName = source.CityName,
                Type = source.Type,
                Category = source.Category,
                Level = source.Level,
                JobsProvided = source.JobsProvided,
                UpkeepCost = source.UpkeepCost,
                BaseProduction = source.BaseProduction,
                BuildCost = source.BuildCost,
                BuildTurns = source.BuildTurns,
                ResourceType = source.ResourceType,
                InputResourceType = source.InputResourceType,
                InputAmount = source.InputAmount,
                StateOwned = source.StateOwned,
                StabilityBonus = source.StabilityBonus,
                CorruptionReduction = source.CorruptionReduction,
                ResearchBonus = source.ResearchBonus,
                PopulationGrowthBonus = source.PopulationGrowthBonus,
                FestivalBoost = source.FestivalBoost,
                ArmyBonus = source.ArmyBonus,
                CompanyId = source.CompanyId,
                BuildingStatus = source.BuildingStatus,
                InactiveTurns = source.InactiveTurns,
                DegradationLevel = source.DegradationLevel,
                IgnoreProductionEfficiency = source.IgnoreProductionEfficiency
            };
        }

        private async Task ShowCurrencyTransferDaysSelect(ITelegramBotClient botClient, long userId, long chatId, int msgId, CurrencyActionType action, CancellationToken cancellationToken)
        {
            int days = 1;
            if (EconomyCurrencyActionState.TryGetValue(userId, out var ctx) && ctx.TransferDays > 0)
                days = ctx.TransferDays;

            string mode = days <= 1 ? "одноразовий переказ" : $"транш на {days} дн.";
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                $"Оберіть тривалість переказу ({mode}):",
                StateKeyboardBuilder.EconomyCurrencyTransferDaysKeyboard(ActionToToken(action)),
                cancellationToken
            );
        }

        private async Task ShowCurrencyAmountInput(ITelegramBotClient botClient, long userId, long chatId, int msgId, CurrencyActionType action, CancellationToken cancellationToken)
        {
            string title = action == CurrencyActionType.Loan
                ? "Введіть суму позики (Gold). Приклад: 10 000"
                : "Введіть суму.";

            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new[]
            {
                new[] { Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("🔙 Назад", "state_economy_currency") }
            });

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                title,
                keyboard,
                cancellationToken
            );
        }

        private async Task ShowCurrencyRateSelect(ITelegramBotClient botClient, long userId, long chatId, int msgId, CurrencyActionType action, CancellationToken cancellationToken)
        {
            var rates = new List<decimal> { 0.5m, 0.75m, 1.0m, 1.25m, 1.5m, 2.0m };
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                "Оберіть курс (Gold за 1 одиницю валюти):",
                StateKeyboardBuilder.EconomyCurrencyRatePresetKeyboard(rates, ActionToToken(action)),
                cancellationToken
            );
        }

        private async Task ApplyCurrencyAmountAction(ITelegramBotClient botClient, long userId, long chatId, int msgId, CurrencyActionContext ctx, decimal amount, CancellationToken cancellationToken)
        {
            var service = new CurrencyExchangeService();
            string result;
            switch (ctx.Action)
            {
                case CurrencyActionType.Transfer:
                    if (ctx.FromCountryId <= 0 || ctx.ToCountryId <= 0)
                    {
                        result = "❌ Оберіть країну-відправника та країну-отримувача.";
                        break;
                    }

                    if (ctx.FromCountryId == ctx.ToCountryId)
                    {
                        result = "❌ Країна-отримувач має бути іншою.";
                        break;
                    }

                    if (userId != masterID && !IsSelectedCountryOwner(userId, ctx.FromCountryId))
                    {
                        result = "Нема доступу.";
                        break;
                    }

                    int transferDays = ctx.TransferDays <= 0 ? 1 : ctx.TransferDays;
                    if (transferDays > 1)
                        result = service.CreateAidTranche(ctx.FromCountryId, ctx.ToCountryId, amount, transferDays, userId);
                    else
                        result = service.Transfer(ctx.FromCountryId, ctx.ToCountryId, amount, userId);
                    break;
                case CurrencyActionType.Loan:
                    result = service.Loan(ctx.FromCountryId, ctx.ToCountryId, amount, userId);
                    break;
                case CurrencyActionType.Repay:
                    result = service.Repay(ctx.FromCountryId, amount, userId);
                    break;
                default:
                    result = "Невідома дія.";
                    break;
            }

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                result,
                CurrencyExchangeService.IsEnabled
                    ? StateKeyboardBuilder.EconomyCurrencyMenuKeyboard(userId == masterID)
                    : StateKeyboardBuilder.EconomyCurrencyDisabledKeyboard(userId == masterID),
                cancellationToken
            );
            EconomyCurrencyActionState.Remove(userId);
            EconomyInputState[userId] = EconomyInputMode.None;
        }

        private async Task ApplyCurrencyRateAction(ITelegramBotClient botClient, long userId, long chatId, int msgId, CurrencyActionContext ctx, decimal rate, CancellationToken cancellationToken)
        {
            var service = new CurrencyExchangeService();
            string result = service.AdjustRate(ctx.FromCountryId, rate);
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                result,
                CurrencyExchangeService.IsEnabled
                    ? StateKeyboardBuilder.EconomyCurrencyMenuKeyboard(userId == masterID)
                    : StateKeyboardBuilder.EconomyCurrencyDisabledKeyboard(userId == masterID),
                cancellationToken
            );
            EconomyCurrencyActionState.Remove(userId);
            EconomyInputState[userId] = EconomyInputMode.None;
        }

        private static bool TryParseCurrencyAction(string token, out CurrencyActionType action)
        {
            action = CurrencyActionType.Transfer;
            return token switch
            {
                "transfer" => (action = CurrencyActionType.Transfer) == CurrencyActionType.Transfer,
                "loan" => (action = CurrencyActionType.Loan) == CurrencyActionType.Loan,
                "repay" => (action = CurrencyActionType.Repay) == CurrencyActionType.Repay,
                "adjust" => (action = CurrencyActionType.AdjustRate) == CurrencyActionType.AdjustRate,
                _ => false
            };
        }

        private static string ActionToToken(CurrencyActionType action)
        {
            return action switch
            {
                CurrencyActionType.Transfer => "transfer",
                CurrencyActionType.Loan => "loan",
                CurrencyActionType.Repay => "repay",
                CurrencyActionType.AdjustRate => "adjust",
                _ => "transfer"
            };
        }

        private static decimal GetBuildingCreationCost(EconomyBuildingRecord building, float inflationPct)
        {
            if (building == null)
                return 0m;

            decimal raw = Math.Max(0m, building.BuildCost);
            if (raw <= 0m)
                return 0m;

            decimal inflationMultiplier = EconomyService.GetInflationPriceMultiplier(inflationPct);
            return decimal.Ceiling(raw * inflationMultiplier);
        }

        private static long RoundBuildingPaymentToLong(decimal cost)
        {
            if (cost <= 0m)
                return 0L;

            decimal rounded = decimal.Ceiling(cost);
            if (rounded >= long.MaxValue)
                return long.MaxValue;
            return (long)rounded;
        }

        private static bool TryChargeUserWalletForBuilding(long userId, decimal cost, out string error)
        {
            error = string.Empty;
            if (cost <= 0m)
                return true;

            long roundedCost = RoundBuildingPaymentToLong(cost);
            if (roundedCost > int.MaxValue)
            {
                error = "❌ Вартість будівлі завелика для оплати з гаманця.";
                return false;
            }

            int required = (int)roundedCost;
            int userGold = UserData.DataBaseValueReader<int>(userId, "GoldenCoin");
            if (userGold < required)
            {
                error = $"❌ Недостатньо золота у гаманці. Потрібно: {FmtNumber(required)}.";
                return false;
            }

            UserData.UpdateDataBaseValue("GoldenCoin", userId, userGold - required);
            return true;
        }

        private static void RefundUserWalletForBuilding(long userId, decimal cost)
        {
            long roundedCost = RoundBuildingPaymentToLong(cost);
            if (roundedCost <= 0 || roundedCost > int.MaxValue)
                return;

            int amount = (int)roundedCost;
            int userGold = UserData.DataBaseValueReader<int>(userId, "GoldenCoin");
            UserData.UpdateDataBaseValue("GoldenCoin", userId, userGold + amount);
        }

        private static bool TryChargeCompanyBudgetForBuilding(int companyId, int countryId, decimal cost, out string error)
        {
            error = string.Empty;
            if (cost <= 0m)
                return true;

            var company = EconomyCompaniesData.GetById(companyId, countryId);
            if (company == null)
            {
                error = "❌ Компанію для оплати не знайдено.";
                return false;
            }

            if (company.Budget < cost)
            {
                error = $"❌ Недостатньо коштів у бюджеті компанії \"{company.Name}\". Потрібно: {FmtNumberCompact(cost)}.";
                return false;
            }

            decimal newBudget = company.Budget - cost;
            bool ok = EconomyCompaniesData.UpdateCompanyField(companyId, countryId, "Budget", newBudget, SqlDbType.Decimal);
            if (!ok)
            {
                error = "❌ Не вдалося списати кошти з бюджету компанії.";
                return false;
            }

            return true;
        }

        private static void RefundCompanyBudgetForBuilding(int companyId, int countryId, decimal cost)
        {
            if (cost <= 0m)
                return;

            var company = EconomyCompaniesData.GetById(companyId, countryId);
            if (company == null)
                return;

            decimal newBudget = company.Budget + cost;
            EconomyCompaniesData.UpdateCompanyField(companyId, countryId, "Budget", newBudget, SqlDbType.Decimal);
        }

        private static bool TryChargeStateTreasuryForBuilding(long userId, CountryRecord country, EconomyBuildingRecord building, decimal cost, out string error)
        {
            error = string.Empty;
            if (cost <= 0m)
                return true;
            if (cost > long.MaxValue)
            {
                error = "❌ Вартість будівлі завелика.";
                return false;
            }

            long required = RoundBuildingPaymentToLong(cost);
            if (country.Treasury < required)
            {
                error = $"❌ Недостатньо коштів у казні. Потрібно: {FmtNumber(required)}.";
                return false;
            }

            country.Treasury -= required;
            CountriesData.UpdateCountry(country);
            EconomyMoneyLogData.AddLog(
                country.CountryId,
                userId,
                required,
                "Expense",
                $"Будівництво: {building.Type}",
                country.TurnsSurvived
            );
            return true;
        }

        private static void RefundStateTreasuryForBuilding(CountryRecord country, decimal cost)
        {
            if (country == null || cost <= 0m || cost > long.MaxValue)
                return;

            long refund = RoundBuildingPaymentToLong(cost);
            country.Treasury += refund;
            CountriesData.UpdateCountry(country);
        }

        private bool TryChargeBuildingCreation(long userId, EconomyBuildingRecord building, decimal cost, out string payerSource, out string error)
        {
            payerSource = string.Empty;
            error = string.Empty;

            if (building == null)
            {
                error = "❌ Дані будівлі пошкоджені.";
                return false;
            }

            var country = CountriesData.GetCountryById(building.CountryId);
            if (country == null)
            {
                error = "❌ Країну не знайдено.";
                return false;
            }

            if (!TryChargeStateTreasuryForBuilding(userId, country, building, cost, out error))
                return false;

            payerSource = "казна держави";
            return true;
        }

        private static EconomyCompanyRecord? FindCompanyForBuildingPayment(long userId, int countryId, decimal cost)
        {
            if (countryId <= 0 || cost <= 0m)
                return null;

            var companies = EconomyCompaniesData.GetByCountry(countryId)
                .Where(c => c.IsActive && c.Budget >= cost)
                .OrderByDescending(c => c.Budget)
                .ToList();

            if (companies.Count == 0)
                return null;

            bool isMasterUser = userId == masterID;
            if (!isMasterUser)
            {
                string userName = ResolveUserDisplayName(userId);
                var owned = companies.FirstOrDefault(c =>
                    !string.IsNullOrWhiteSpace(c.OwnerName) &&
                    string.Equals(c.OwnerName.Trim(), userName, StringComparison.OrdinalIgnoreCase));
                if (owned != null)
                    return owned;
                return null;
            }

            return companies[0];
        }

        private void RefundBuildingCreation(long userId, EconomyBuildingRecord building, decimal cost)
        {
            if (building == null || cost <= 0m)
                return;

            var country = CountriesData.GetCountryById(building.CountryId);
            if (country != null)
                RefundStateTreasuryForBuilding(country, cost);
        }

        private bool TryAddBuildingWithPayment(
            long userId,
            EconomyBuildingRecord building,
            out int buildingId,
            out decimal chargedCost,
            out string payerSource,
            out string error,
            decimal? overrideCharge = null)
        {
            buildingId = 0;
            chargedCost = 0m;
            payerSource = string.Empty;
            error = string.Empty;

            if (building == null)
            {
                error = "❌ Дані будівлі не знайдено.";
                return false;
            }

            building.CityName = NormalizeBuildingCityName(building.CityName);

            var countrySnapshot = CountriesData.GetCountryById(building.CountryId);
            float inflationPct = countrySnapshot?.Inflation ?? 0f;
            decimal effectiveCharge = overrideCharge ?? GetBuildingCreationCost(building, inflationPct);
            chargedCost = effectiveCharge <= 0m ? 0m : decimal.Ceiling(effectiveCharge);
            if (chargedCost > 0m)
            {
                if (!TryChargeBuildingCreation(userId, building, chargedCost, out payerSource, out error))
                    return false;
            }

            building.BuildTurns = Math.Max(0, building.BuildTurns);
            if (building.BuildTurns > 0)
            {
                building.BuildingStatus = BuildingStatus.UnderConstruction.ToString();
                building.InactiveTurns = 0;
                building.DegradationLevel = 0;
            }
            else if (string.Equals(building.BuildingStatus, BuildingStatus.UnderConstruction.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                building.BuildingStatus = BuildingStatus.Active.ToString();
            }

            buildingId = EconomyBuildingsData.AddBuilding(building);
            if (buildingId <= 0)
            {
                if (chargedCost > 0m)
                    RefundBuildingCreation(userId, building, chargedCost);
                error = "❌ Не вдалося додати будівлю.";
                return false;
            }

            EconomyCitiesData.EnsureCity(building.CountryId, EconomyBuildingRecord.DefaultCityName);
            EconomyCitiesData.EnsureCity(building.CountryId, building.CityName);
             
            SyncCountryCitiesCount(building.CountryId);

            return true;
        }

        private async Task HandleBuildingInput(ITelegramBotClient botClient, long userId, string input, CancellationToken cancellationToken)
        {
            int step = EconomyInputStep[userId];
            if (step < 0 || step >= BuildingFields.Count)
                step = 0;

            var field = BuildingFields[step];
            if (!EconomyBuildingBuffer.TryGetValue(userId, out var building))
            {
                building = new EconomyBuildingRecord();
                EconomyBuildingBuffer[userId] = building;
            }

            if (field.FieldName == "Category" && string.IsNullOrWhiteSpace(input))
            {
                await botClient.SendTextMessageAsync(userId, "Оберіть категорію будівлі:", replyMarkup: StateKeyboardBuilder.EconomyBuildingCategoryKeyboard(), cancellationToken: cancellationToken);
                return;
            }

            if (!TryAssignBuildingField(building, field.FieldName, field.Kind, input, out string error))
            {
                await SendHelperMessageAsync(botClient, userId, error, cancellationToken);
                return;
            }

            EconomyInputStep[userId] = step + 1;
            if (EconomyInputStep[userId] >= BuildingFields.Count)
            {
                bool ok = TryAddBuildingWithPayment(userId, building, out int id, out decimal chargedCost, out string payerSource, out string paymentError);
                await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
                ClearEconomyInput(userId);
                string result;
                if (ok)
                {
                    result = $"✅ Будівлю додано (Id={id}).";
                    if (chargedCost > 0m)
                        result += $"\nСписано: {FmtNumberCompact(chargedCost)} ({payerSource}).";
                }
                else
                {
                    result = paymentError;
                }
                await botClient.SendTextMessageAsync(userId, result, cancellationToken: cancellationToken);
                return;
            }

            var nextField = BuildingFields[EconomyInputStep[userId]];
            if (IsBuildingResourceSelectionField(nextField.FieldName))
            {
                await ShowBuildingResourceSelect(botClient, userId, nextField.FieldName, 0, cancellationToken);
                return;
            }
            if (nextField.FieldName == "Category")
            {
                await botClient.SendTextMessageAsync(userId, "Оберіть категорію будівлі:", replyMarkup: StateKeyboardBuilder.EconomyBuildingCategoryKeyboard(), cancellationToken: cancellationToken);
                return;
            }
            await SendModeFieldPromptAsync(
                botClient,
                userId,
                EconomyInputMode.AddBuilding,
                nextField.FieldName,
                nextField.Prompt,
                cancellationToken);
        }

        private async Task HandleFestivalInput(ITelegramBotClient botClient, long userId, string input, CancellationToken cancellationToken)
        {
            int step = EconomyInputStep[userId];
            if (step < 0 || step >= FestivalFields.Count)
                step = 0;

            var field = FestivalFields[step];
            if (!EconomyFestivalBuffer.TryGetValue(userId, out var festival))
            {
                festival = new EconomyFestivalRecord();
                EconomyFestivalBuffer[userId] = festival;
            }

            if (!TryAssignFestivalField(festival, field.FieldName, field.Kind, input, out string error))
            {
                await botClient.SendTextMessageAsync(userId, error, cancellationToken: cancellationToken);
                return;
            }

            EconomyInputStep[userId] = step + 1;
            if (EconomyInputStep[userId] >= FestivalFields.Count)
            {
                festival.RemainingTurns = festival.DurationTurns;
                festival.IsActive = true;
                int id = EconomyFestivalsData.AddFestival(festival);
                ClearEconomyInput(userId);
                string result = id > 0 ? $"✅ Фестиваль створено (Id={id})." : "Не вдалося створити фестиваль.";
                await botClient.SendTextMessageAsync(userId, result, cancellationToken: cancellationToken);
                return;
            }

            await botClient.SendTextMessageAsync(userId, FestivalFields[EconomyInputStep[userId]].Prompt, cancellationToken: cancellationToken);
        }

        private async Task HandleSanctionsEfficiencyInput(ITelegramBotClient botClient, long userId, string input, CancellationToken cancellationToken)
        {
            if (!EconomySanctionsEdit.TryGetValue(userId, out var ctx))
            {
                ClearEconomyInput(userId);
                await botClient.SendTextMessageAsync(userId, "❌ Нема доступу або контекст не знайдено.", cancellationToken: cancellationToken);
                return;
            }

            if (!float.TryParse(input.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var pct))
            {
                await SendHelperMessageAsync(botClient, userId, "❌ Вкажіть відсоток (0-100).", cancellationToken);
                return;
            }

            if (!EconomySanctionsUiContext.TryGetValue(userId, out var ui))
            {
                ClearEconomyInput(userId);
                await botClient.SendTextMessageAsync(userId, "❌ Нема контексту інтерфейсу.", cancellationToken: cancellationToken);
                return;
            }

            await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
            ClearEconomyInput(userId);

            await SetEconomySanctionsTargetEfficiency(botClient, userId, ui.ChatId, ui.MsgId, ctx.TargetCountryId, pct, ctx.IsExport, cancellationToken);
        }

        private async Task HandleProductSanctionsEfficiencyInput(ITelegramBotClient botClient, long userId, string input, CancellationToken cancellationToken)
        {
            if (!EconomyProductSanctionsEdit.TryGetValue(userId, out var ctx))
            {
                ClearEconomyInput(userId);
                await botClient.SendTextMessageAsync(userId, "❌ Нема доступу або контекст не знайдено.", cancellationToken: cancellationToken);
                return;
            }

            if (!float.TryParse(input.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var pct))
            {
                await SendHelperMessageAsync(botClient, userId, "❌ Вкажіть відсоток (0-100).", cancellationToken);
                return;
            }

            if (!EconomyProductSanctionsUiContext.TryGetValue(userId, out var ui))
            {
                ClearEconomyInput(userId);
                await botClient.SendTextMessageAsync(userId, "❌ Нема контексту інтерфейсу.", cancellationToken: cancellationToken);
                return;
            }

            await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
            ClearEconomyInput(userId);

            await SetEconomySanctionsProductEfficiency(botClient, userId, ui.ChatId, ui.MsgId, ctx.TargetCountryId, ctx.ResourceType, pct, ctx.IsExport, cancellationToken);
        }

        private async Task HandlePreviewDaysInput(ITelegramBotClient botClient, long userId, string input, CancellationToken cancellationToken)
        {
            if (!int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out int days) || days < 1)
            {
                await botClient.SendTextMessageAsync(userId, "❌ Вкажіть кількість днів (>= 1).", cancellationToken: cancellationToken);
                return;
            }

            EconomyPreviewDaysBuffer[userId] = days;
            await SendHelperMessageAsync(botClient, userId, $"✅ Прогноз враховано: {FmtNumber(days)} дн.", cancellationToken);
            ClearEconomyInput(userId);

            if (EconomyPreviewUiContext.TryGetValue(userId, out var ctx))
            {
                await ShowEconomyPreview(botClient, userId, ctx.ChatId, ctx.MsgId, cancellationToken);
                return;
            }

            await botClient.SendTextMessageAsync(userId, "❌ Нема контексту для прев'ю.", cancellationToken: cancellationToken);
        }

        private static bool IsIncomeLimitDisableToken(string value)
        {
            string normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
            return normalized == "off" ||
                   normalized == "disable" ||
                   normalized == "none" ||
                   normalized == "вимкнути" ||
                   normalized == "вимк" ||
                   normalized == "выключить";
        }

        private async Task HandleIncomeLimitInput(ITelegramBotClient botClient, long userId, string input, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                ClearEconomyInput(userId);
                await botClient.SendTextMessageAsync(userId, "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.", cancellationToken: cancellationToken);
                return;
            }

            if (userId != masterID && !IsSelectedCountryOwner(userId, selectedCountryId))
            {
                ClearEconomyInput(userId);
                await botClient.SendTextMessageAsync(userId, "Нема доступу.", cancellationToken: cancellationToken);
                return;
            }

            string raw = (input ?? string.Empty).Trim();
            bool disableRequested = IsIncomeLimitDisableToken(raw);
            decimal targetDaily = 0m;

            if (!disableRequested)
            {
                if (!TryParseDecimal(raw, out targetDaily) || targetDaily < 0m)
                {
                    await SendHelperMessageAsync(botClient, userId, "❌ Вкажіть число >= 0, або `off` для вимкнення.", cancellationToken);
                    return;
                }

                if (targetDaily > 1_000_000_000m)
                {
                    await SendHelperMessageAsync(botClient, userId, "❌ Завелике значення. Максимум: 1 000 000 000.", cancellationToken);
                    return;
                }
            }

            bool success;
            string statusMessage;
            if (disableRequested)
            {
                success = EconomyConfigData.UpsertCountryDecimal(selectedCountryId.Value, NetIncomeLimitEnabledConfigKey, 0m);
                statusMessage = success
                    ? "✅ Ліміт доходу вимкнено."
                    : "❌ Не вдалося вимкнути ліміт доходу.";
            }
            else
            {
                decimal normalizedTarget = decimal.Round(Math.Max(0m, targetDaily), 2, MidpointRounding.AwayFromZero);
                bool savedTarget = EconomyConfigData.UpsertCountryDecimal(selectedCountryId.Value, NetIncomeLimitTargetConfigKey, normalizedTarget);
                bool savedEnabled = EconomyConfigData.UpsertCountryDecimal(selectedCountryId.Value, NetIncomeLimitEnabledConfigKey, 1m);
                success = savedTarget && savedEnabled;
                statusMessage = success
                    ? $"✅ Ліміт увімкнено. Ціль: {FmtNumber(normalizedTarget)} / день."
                    : "❌ Не вдалося зберегти ліміт доходу.";
            }

            EconomyIncomeLimitUiContext.TryGetValue(userId, out var uiContext);
            await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
            ClearEconomyInput(userId);

            if (uiContext.ChatId != 0 && uiContext.MsgId != 0)
            {
                await ShowEconomyIncomeLimitMenu(botClient, userId, uiContext.ChatId, uiContext.MsgId, cancellationToken, statusMessage);
                return;
            }

            await botClient.SendTextMessageAsync(userId, statusMessage, cancellationToken: cancellationToken);
        }

        private static bool IsSectorTaxOverrideAutoToken(string value)
        {
            string normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
            return normalized == "auto" ||
                   normalized == "off" ||
                   normalized == "none" ||
                   normalized == "default" ||
                   normalized == "скинути" ||
                   normalized == "авто" ||
                   normalized == "вимкнути";
        }

        private async Task HandleSectorRevenueMultiplierInput(ITelegramBotClient botClient, long userId, string input, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                ClearEconomyInput(userId);
                await botClient.SendTextMessageAsync(userId, "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.", cancellationToken: cancellationToken);
                return;
            }

            if (userId != masterID && !IsSelectedCountryOwner(userId, selectedCountryId))
            {
                ClearEconomyInput(userId);
                await botClient.SendTextMessageAsync(userId, "Нема доступу.", cancellationToken: cancellationToken);
                return;
            }

            if (!TryParseDecimal(input, out var rawMultiplier))
            {
                await SendHelperMessageAsync(
                    botClient,
                    userId,
                    $"❌ Вкажіть число в межах {FmtNumberCompact(SectorRevenueMultiplierMin, 1)}-{FmtNumberCompact(SectorRevenueMultiplierMax, 1)}.",
                    cancellationToken);
                return;
            }

            if (rawMultiplier < SectorRevenueMultiplierMin || rawMultiplier > SectorRevenueMultiplierMax)
            {
                await SendHelperMessageAsync(
                    botClient,
                    userId,
                    $"❌ Діапазон множника: {FmtNumberCompact(SectorRevenueMultiplierMin, 1)}-{FmtNumberCompact(SectorRevenueMultiplierMax, 1)}.",
                    cancellationToken);
                return;
            }

            string sectorKey = ResolveSelectedEconomySectorForCountry(userId, selectedCountryId.Value);
            decimal multiplier = decimal.Round(rawMultiplier, 3, MidpointRounding.AwayFromZero);
            bool saved = EconomyConfigData.UpsertCountryDecimal(
                selectedCountryId.Value,
                BuildSectorRevenueMultiplierConfigKey(sectorKey),
                multiplier);

            EconomySectorUiContext.TryGetValue(userId, out var uiContext);
            await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
            ClearEconomyInput(userId);

            string status = saved
                ? $"✅ Для сектора \"{FormatProductionTypeUk(sectorKey)}\" встановлено множник доходу x{FmtNumberCompact(multiplier, 3)}."
                : $"❌ Не вдалося зберегти множник доходу для сектора \"{FormatProductionTypeUk(sectorKey)}\".";

            if (uiContext.ChatId != 0 && uiContext.MsgId != 0)
            {
                await ShowEconomySectorReport(botClient, userId, uiContext.ChatId, uiContext.MsgId, cancellationToken, status);
                return;
            }

            await botClient.SendTextMessageAsync(userId, status, cancellationToken: cancellationToken);
        }

        private async Task HandleSectorTaxOverrideInput(ITelegramBotClient botClient, long userId, string input, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                ClearEconomyInput(userId);
                await botClient.SendTextMessageAsync(userId, "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.", cancellationToken: cancellationToken);
                return;
            }

            if (userId != masterID && !IsSelectedCountryOwner(userId, selectedCountryId))
            {
                ClearEconomyInput(userId);
                await botClient.SendTextMessageAsync(userId, "Нема доступу.", cancellationToken: cancellationToken);
                return;
            }

            string raw = (input ?? string.Empty).Trim();
            bool autoRequested = IsSectorTaxOverrideAutoToken(raw);
            decimal valueToStore = SectorTaxOverrideAutoValue;

            if (!autoRequested)
            {
                if (!TryParseDecimal(raw, out var parsed))
                {
                    await SendHelperMessageAsync(botClient, userId, "❌ Вкажіть податок у % (0-95) або `auto`.", cancellationToken);
                    return;
                }

                decimal normalized = parsed > 1m ? parsed / 100m : parsed;
                if (normalized < 0m || normalized > SectorTaxOverrideMax)
                {
                    await SendHelperMessageAsync(botClient, userId, "❌ Діапазон податку: 0-95%.", cancellationToken);
                    return;
                }

                valueToStore = decimal.Round(normalized, 4, MidpointRounding.AwayFromZero);
            }

            string sectorKey = ResolveSelectedEconomySectorForCountry(userId, selectedCountryId.Value);
            bool saved = EconomyConfigData.UpsertCountryDecimal(
                selectedCountryId.Value,
                BuildSectorTaxOverrideConfigKey(sectorKey),
                valueToStore);

            EconomySectorUiContext.TryGetValue(userId, out var uiContext);
            await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
            ClearEconomyInput(userId);

            string status = saved
                ? autoRequested
                    ? $"✅ Для сектора \"{FormatProductionTypeUk(sectorKey)}\" увімкнено автоставку податку."
                    : $"✅ Для сектора \"{FormatProductionTypeUk(sectorKey)}\" встановлено податок {(valueToStore * 100m):0.##}%."
                : $"❌ Не вдалося зберегти податок для сектора \"{FormatProductionTypeUk(sectorKey)}\".";

            if (uiContext.ChatId != 0 && uiContext.MsgId != 0)
            {
                await ShowEconomySectorReport(botClient, userId, uiContext.ChatId, uiContext.MsgId, cancellationToken, status);
                return;
            }

            await botClient.SendTextMessageAsync(userId, status, cancellationToken: cancellationToken);
        }

        private async Task HandleTradeInput(ITelegramBotClient botClient, long userId, string input, CancellationToken cancellationToken)
        {
            if (!EconomyTradeFlowMode.TryGetValue(userId, out var flowMode))
            {
                await botClient.SendTextMessageAsync(userId, "❌ Спочатку оберіть тип угоди: імпорт, експорт або обидва.", cancellationToken: cancellationToken);
                return;
            }

            int step = EconomyInputStep.TryGetValue(userId, out var currentStep) ? currentStep : 0;
            step = GetNextTradeFieldIndex(step, flowMode);
            if (step >= TradeFields.Count)
            {
                long uiChatId = 0;
                int uiMsgId = 0;
                if (EconomyTradeUiContext.TryGetValue(userId, out var uiContext))
                {
                    uiChatId = uiContext.ChatId;
                    uiMsgId = uiContext.MsgId;
                }
                EconomyInputStep[userId] = step;
                await ProceedTradeCreation(botClient, userId, uiChatId, uiMsgId, cancellationToken);
                return;
            }

            var field = TradeFields[step];
            if (!EconomyTradeBuffer.TryGetValue(userId, out var agreement))
            {
                agreement = new EconomyTradeAgreementRecord();
                EconomyTradeBuffer[userId] = agreement;
            }

            if (!TryAssignTradeField(agreement, field.FieldName, field.Kind, input, out string error))
            {
                await botClient.SendTextMessageAsync(userId, error, cancellationToken: cancellationToken);
                return;
            }

            EconomyInputStep[userId] = step + 1;
            long chatId = 0;
            int msgId = 0;
            if (EconomyTradeUiContext.TryGetValue(userId, out var ctx))
            {
                chatId = ctx.ChatId;
                msgId = ctx.MsgId;
            }
            await ProceedTradeCreation(botClient, userId, chatId, msgId, cancellationToken);
        }

        private static bool TryAssignTradeField(EconomyTradeAgreementRecord agreement, string fieldName, FieldKind kind, string input, out string error)
        {
            error = "";
            if (fieldName == "ExportResourceType" || fieldName == "ImportResourceType")
            {
                string s = (input ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(s) || s == "-")
                {
                    if (fieldName == "ExportResourceType")
                        agreement.ExportResourceType = null;
                    else
                        agreement.ImportResourceType = null;
                    return true;
                }
                var resources = EconomyResourcesData.GetAll();
                var exact = resources.FirstOrDefault(r => string.Equals(r.ResourceType, s, StringComparison.OrdinalIgnoreCase));
                if (exact == null)
                {
                    string norm = NormalizeResourceKey(s);
                    var normalizedMatches = resources
                        .Where(r => NormalizeResourceKey(r.ResourceType) == norm)
                        .ToList();
                    if (normalizedMatches.Count == 1)
                    {
                        exact = normalizedMatches[0];
                    }
                    else
                    {
                        var sample = resources
                            .Select(r => r.ResourceType)
                            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                            .Take(10)
                            .ToList();
                        string list = sample.Count > 0 ? string.Join(", ", sample) : "-";
                        error = $"❌ Невідомий ресурс. Використовуйте значення з EconomyResources. Приклади: {list}";
                        return false;
                    }
                }
                if (fieldName == "ExportResourceType")
                {
                    agreement.ExportResourceType = exact.ResourceType;
                    if (agreement.ExportQty > 0m &&
                        !TryValidateTradeQuantity(agreement, "ExportQty", agreement.ExportQty, out error))
                    {
                        return false;
                    }
                }
                else
                {
                    agreement.ImportResourceType = exact.ResourceType;
                    if (agreement.ImportQty > 0m &&
                        !TryValidateTradeQuantity(agreement, "ImportQty", agreement.ImportQty, out error))
                    {
                        return false;
                    }
                }
                return true;
            }

            if (fieldName == "ExportQty" || fieldName == "ImportQty")
            {
                string normalized = (input ?? string.Empty).Trim().Replace(',', '.');
                if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var qty))
                {
                    error = "❌ Вкажіть кількість числом (> 0).";
                    return false;
                }
                if (qty <= 0m)
                {
                    error = "❌ Кількість має бути більшою за 0.";
                    return false;
                }
                if (!TryValidateTradeQuantity(agreement, fieldName, qty, out error))
                    return false;

                if (fieldName == "ExportQty")
                    agreement.ExportQty = qty;
                else
                    agreement.ImportQty = qty;
                return true;
            }

            if (fieldName == "QuantityIsPerDay")
            {
                if (!TryParseBool(input, out bool isPerDay))
                {
                    error = "❌ Вкажіть так/ні або 1/0.";
                    return false;
                }

                agreement.QuantityIsPerDay = isPerDay;
                if (agreement.ExportQty > 0m &&
                    !TryValidateTradeQuantity(agreement, "ExportQty", agreement.ExportQty, out error))
                {
                    return false;
                }
                if (agreement.ImportQty > 0m &&
                    !TryValidateTradeQuantity(agreement, "ImportQty", agreement.ImportQty, out error))
                {
                    return false;
                }
                return true;
            }

            if (fieldName == "PeriodDays")
            {
                if (!int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) || i <= 0)
                {
                    error = "❌ Вкажіть час доставки у днях (>0).";
                    return false;
                }
                agreement.PeriodDays = i;
                if (agreement.ExportQty > 0m &&
                    !TryValidateTradeQuantity(agreement, "ExportQty", agreement.ExportQty, out error))
                {
                    return false;
                }
                if (agreement.ImportQty > 0m &&
                    !TryValidateTradeQuantity(agreement, "ImportQty", agreement.ImportQty, out error))
                {
                    return false;
                }
                return true;
            }

            if (fieldName == "DeliveryMode")
            {
                agreement.DeliveryMode = NormalizeTradeDeliveryMode(input);
                return true;
            }

            return TryAssignCommon(agreement, fieldName, kind, input, out error);
        }

        private static bool TryValidateTradeQuantity(EconomyTradeAgreementRecord agreement, string quantityField, decimal qty, out string error)
        {
            error = string.Empty;
            if (qty <= 0m)
                return true;

            bool isExport = string.Equals(quantityField, "ExportQty", StringComparison.Ordinal);
            string? resourceType = isExport ? agreement.ExportResourceType : agreement.ImportResourceType;
            if (string.IsNullOrWhiteSpace(resourceType))
            {
                error = isExport
                    ? "❌ Спочатку оберіть ресурс експорту."
                    : "❌ Спочатку оберіть ресурс імпорту.";
                return false;
            }

            int producerCountryId = isExport ? agreement.CountryId : (agreement.PartnerCountryId ?? 0);
            if (producerCountryId <= 0)
            {
                error = isExport
                    ? "❌ Не вдалося визначити країну-експортера."
                    : "❌ Для імпорту оберіть країну-партнера.";
                return false;
            }

            decimal maxProduction = GetCountryResourceProductionCapacity(producerCountryId, resourceType);
            int deliveryDays = GetTradeDeliveryDays(agreement);
            bool quantityPerDay = agreement.QuantityIsPerDay;
            decimal requestedPerDay = quantityPerDay ? qty : (qty / deliveryDays);

            if (requestedPerDay > maxProduction)
            {
                var producer = CountriesData.GetCountryById(producerCountryId);
                string producerName = producer != null
                    ? FormatCountryName(producer)
                    : $"#{producerCountryId}";
                if (quantityPerDay)
                {
                    error = $"❌ {producerName} зараз виробляє {FmtNumberCompact(maxProduction)} {FormatResourceTypeUk(resourceType)} / день. Вкажіть кількість не більше цього значення.";
                }
                else
                {
                    decimal maxPerDelivery = maxProduction * deliveryDays;
                    error = $"❌ {producerName} зараз виробляє {FmtNumberCompact(maxProduction)} {FormatResourceTypeUk(resourceType)} / день. Для {FmtNumber(deliveryDays)} дн. доставки максимум {FmtNumberCompact(maxPerDelivery)} за доставку.";
                }
                return false;
            }

            return true;
        }

        private static decimal GetCountryResourceProductionCapacity(int countryId, string resourceType)
        {
            if (countryId <= 0 || string.IsNullOrWhiteSpace(resourceType))
                return 0m;

            var provider = new StateDataProvider();
            var state = provider.GetStateData(0, countryId);
            if (state == null)
                return 0m;

            var buildings = EconomyBuildingsData.GetByCountry(countryId);
            var production = ComputeResourceProductionNow(state, buildings).NetByResource;
            if (production.Count == 0)
                return 0m;

            if (production.TryGetValue(resourceType, out var direct))
                return Math.Max(0m, direct);

            string normalized = NormalizeResourceKey(resourceType);
            foreach (var pair in production)
            {
                if (NormalizeResourceKey(pair.Key) == normalized)
                    return Math.Max(0m, pair.Value);
            }

            return 0m;
        }

        private static bool TryValidateTradeProducerHasResource(int producerCountryId, string? resourceType, out string error)
        {
            error = string.Empty;
            if (producerCountryId <= 0 || string.IsNullOrWhiteSpace(resourceType))
            {
                error = "❌ Не вдалося визначити країну-виробника або ресурс для перевірки.";
                return false;
            }

            decimal production = GetCountryResourceProductionCapacity(producerCountryId, resourceType);
            if (production > 0m)
                return true;

            var producer = CountriesData.GetCountryById(producerCountryId);
            string producerName = producer != null
                ? FormatCountryName(producer)
                : $"#{producerCountryId}";
            error = $"❌ Угоду відхилено: {producerName} не виробляє ресурс «{FormatResourceTypeUk(resourceType)}».";
            return false;
        }

        private static bool TryValidateTradeAgreementForSubmission(
            EconomyTradeAgreementRecord agreement,
            TradeFlowMode flowMode,
            out string error,
            out string? invalidField)
        {
            error = string.Empty;
            invalidField = null;
            if (agreement == null)
            {
                error = "❌ Не знайдено чернетку угоди.";
                return false;
            }

            bool hasExport = flowMode != TradeFlowMode.ImportOnly;
            bool hasImport = flowMode != TradeFlowMode.ExportOnly;

            if (hasExport)
            {
                if (string.IsNullOrWhiteSpace(agreement.ExportResourceType))
                {
                    invalidField = "ExportResourceType";
                    error = "❌ Для експорту оберіть ресурс.";
                    return false;
                }
                if (agreement.ExportQty <= 0m)
                {
                    invalidField = "ExportQty";
                    error = "❌ Для експорту вкажіть кількість більше 0.";
                    return false;
                }
                if (!TryValidateTradeProducerHasResource(agreement.CountryId, agreement.ExportResourceType, out error))
                {
                    invalidField = "ExportResourceType";
                    return false;
                }
                if (!TryValidateTradeQuantity(agreement, "ExportQty", agreement.ExportQty, out error))
                {
                    invalidField = "ExportQty";
                    return false;
                }
            }

            if (hasImport)
            {
                if (!agreement.PartnerCountryId.HasValue || agreement.PartnerCountryId.Value <= 0)
                {
                    invalidField = "PartnerCountryId";
                    error = "❌ Для імпорту потрібно обрати країну-партнера.";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(agreement.ImportResourceType))
                {
                    invalidField = "ImportResourceType";
                    error = "❌ Для імпорту оберіть ресурс.";
                    return false;
                }
                if (agreement.ImportQty <= 0m)
                {
                    invalidField = "ImportQty";
                    error = "❌ Для імпорту вкажіть кількість більше 0.";
                    return false;
                }
                if (!TryValidateTradeProducerHasResource(agreement.PartnerCountryId.Value, agreement.ImportResourceType, out error))
                {
                    invalidField = "ImportResourceType";
                    return false;
                }
                if (!TryValidateTradeQuantity(agreement, "ImportQty", agreement.ImportQty, out error))
                {
                    invalidField = "ImportQty";
                    return false;
                }
            }

            agreement.PeriodDays = Math.Max(1, agreement.PeriodDays);
            agreement.DeliveryMode = NormalizeTradeDeliveryMode(agreement.DeliveryMode);
            return true;
        }

        private static EconomyTradeAgreementRecord CloneTradeAgreement(EconomyTradeAgreementRecord source)
        {
            return new EconomyTradeAgreementRecord
            {
                AgreementId = source.AgreementId,
                CountryId = source.CountryId,
                PartnerCountryId = source.PartnerCountryId,
                PartnerName = source.PartnerName,
                IsActive = source.IsActive,
                ExportBonus = source.ExportBonus,
                ImportBonus = source.ImportBonus,
                ResourceType = source.ResourceType,
                ExportResourceType = source.ExportResourceType,
                ImportResourceType = source.ImportResourceType,
                ExportQty = source.ExportQty,
                ExportUnitPrice = source.ExportUnitPrice,
                ImportQty = source.ImportQty,
                ImportUnitPrice = source.ImportUnitPrice,
                PeriodDays = source.PeriodDays,
                DeliveryMode = source.DeliveryMode,
                QuantityIsPerDay = source.QuantityIsPerDay
            };
        }

        private static bool TryCreateTradeAgreementsPair(
            EconomyTradeAgreementRecord agreement,
            out int primaryAgreementId,
            out int mirrorAgreementId,
            out string error)
        {
            primaryAgreementId = 0;
            mirrorAgreementId = 0;
            error = string.Empty;

            if (agreement == null)
            {
                error = "Не знайдено дані торгової угоди.";
                return false;
            }

            var primary = CloneTradeAgreement(agreement);
            primary.AgreementId = 0;
            primary.IsActive = true;
            primary.PeriodDays = Math.Max(1, primary.PeriodDays);
            primary.DeliveryMode = NormalizeTradeDeliveryMode(primary.DeliveryMode);
            primaryAgreementId = EconomyTradeAgreementsData.AddAgreement(primary);
            if (primaryAgreementId <= 0)
            {
                error = "Не вдалося створити торгову угоду.";
                return false;
            }

            if (!primary.PartnerCountryId.HasValue || primary.PartnerCountryId.Value <= 0)
                return true;

            var mirrored = BuildMirroredTradeAgreement(primary);
            mirrorAgreementId = EconomyTradeAgreementsData.AddAgreement(mirrored);
            if (mirrorAgreementId > 0)
                return true;

            EconomyTradeAgreementsData.EndAgreement(primaryAgreementId, primary.CountryId);
            primaryAgreementId = 0;
            error = "Не вдалося створити дзеркальну угоду для країни-партнера.";
            return false;
        }

        private static EconomyTradeAgreementRecord BuildMirroredTradeAgreement(EconomyTradeAgreementRecord source)
        {
            int partnerCountryId = source.PartnerCountryId ?? 0;
            var sourceCountry = CountriesData.GetCountryById(source.CountryId);
            string? sourceCountryName = sourceCountry?.Name;
            if (string.IsNullOrWhiteSpace(sourceCountryName))
                sourceCountryName = $"#{source.CountryId}";

            var mirrored = new EconomyTradeAgreementRecord
            {
                CountryId = partnerCountryId,
                PartnerCountryId = source.CountryId,
                PartnerName = sourceCountryName,
                IsActive = true,
                ExportBonus = source.ImportBonus,
                ImportBonus = source.ExportBonus,
                ResourceType = BuildLegacyTradeResourceValue(source.ImportResourceType, source.ExportResourceType),
                ExportResourceType = source.ImportResourceType,
                ImportResourceType = source.ExportResourceType,
                ExportQty = source.ImportQty,
                ExportUnitPrice = source.ImportUnitPrice,
                ImportQty = source.ExportQty,
                ImportUnitPrice = source.ExportUnitPrice,
                PeriodDays = Math.Max(1, source.PeriodDays),
                DeliveryMode = NormalizeTradeDeliveryMode(source.DeliveryMode),
                QuantityIsPerDay = source.QuantityIsPerDay
            };

            return mirrored;
        }

        private static string? BuildLegacyTradeResourceValue(string? exportResourceType, string? importResourceType)
        {
            if (!string.IsNullOrWhiteSpace(exportResourceType))
                return exportResourceType;
            if (!string.IsNullOrWhiteSpace(importResourceType))
                return importResourceType;
            return null;
        }

        private static bool TryEndMirroredTradeAgreement(EconomyTradeAgreementRecord? sourceAgreement)
        {
            var mirrored = FindMirroredTradeAgreement(sourceAgreement);
            if (mirrored == null)
                return false;

            return EconomyTradeAgreementsData.EndAgreement(mirrored.AgreementId, mirrored.CountryId);
        }

        private static EconomyTradeAgreementRecord? FindMirroredTradeAgreement(
            EconomyTradeAgreementRecord? sourceAgreement,
            string? changedSourceFieldName = null)
        {
            if (sourceAgreement == null)
                return null;
            if (!sourceAgreement.PartnerCountryId.HasValue || sourceAgreement.PartnerCountryId.Value <= 0)
                return null;

            int partnerCountryId = sourceAgreement.PartnerCountryId.Value;
            int sourceCountryId = sourceAgreement.CountryId;

            string ignoredMirrorFieldName = string.Empty;
            if (!string.IsNullOrWhiteSpace(changedSourceFieldName))
                TryMapTradeFieldToMirroredField(changedSourceFieldName, out ignoredMirrorFieldName);

            var candidates = EconomyTradeAgreementsData.GetByCountry(partnerCountryId)
                .Where(a => a.PartnerCountryId.HasValue &&
                            a.PartnerCountryId.Value == sourceCountryId)
                .ToList();
            if (candidates.Count == 0)
                return null;

            var strict = candidates
                .Where(a =>
                    IsMirroredTradeFieldMatch(a, sourceAgreement, "ExportResourceType", ignoredMirrorFieldName) &&
                    IsMirroredTradeFieldMatch(a, sourceAgreement, "ImportResourceType", ignoredMirrorFieldName) &&
                    IsMirroredTradeFieldMatch(a, sourceAgreement, "ExportQty", ignoredMirrorFieldName) &&
                    IsMirroredTradeFieldMatch(a, sourceAgreement, "ImportQty", ignoredMirrorFieldName) &&
                    IsMirroredTradeFieldMatch(a, sourceAgreement, "ExportUnitPrice", ignoredMirrorFieldName) &&
                    IsMirroredTradeFieldMatch(a, sourceAgreement, "ImportUnitPrice", ignoredMirrorFieldName) &&
                    IsMirroredTradeFieldMatch(a, sourceAgreement, "PeriodDays", ignoredMirrorFieldName) &&
                    IsMirroredTradeFieldMatch(a, sourceAgreement, "DeliveryMode", ignoredMirrorFieldName) &&
                    IsMirroredTradeFieldMatch(a, sourceAgreement, "QuantityIsPerDay", ignoredMirrorFieldName) &&
                    IsMirroredTradeFieldMatch(a, sourceAgreement, "IsActive", ignoredMirrorFieldName))
                .OrderByDescending(a => a.AgreementId)
                .FirstOrDefault();
            if (strict != null)
                return strict;

            return candidates
                .OrderByDescending(a => CountMirroredTradeMatches(a, sourceAgreement, ignoredMirrorFieldName))
                .ThenByDescending(a => a.AgreementId)
                .FirstOrDefault();
        }

        private static bool TrySyncMirroredTradeAgreementField(
            int agreementId,
            int countryId,
            string fieldName,
            object value,
            SqlDbType sqlType,
            out bool syncAttempted)
        {
            syncAttempted = false;
            if (!TryMapTradeFieldToMirroredField(fieldName, out string mirroredFieldName))
                return true;

            var sourceAgreement = EconomyTradeAgreementsData.GetByCountry(countryId)
                .FirstOrDefault(a => a.AgreementId == agreementId);
            if (sourceAgreement == null)
            {
                syncAttempted = true;
                return false;
            }

            if (!sourceAgreement.PartnerCountryId.HasValue || sourceAgreement.PartnerCountryId.Value <= 0)
                return true;

            syncAttempted = true;
            var mirroredAgreement = FindMirroredTradeAgreement(sourceAgreement, fieldName);
            if (mirroredAgreement == null)
            {
                var mirrorToCreate = BuildMirroredTradeAgreement(sourceAgreement);
                if (mirrorToCreate.CountryId <= 0 ||
                    !mirrorToCreate.PartnerCountryId.HasValue ||
                    mirrorToCreate.PartnerCountryId.Value <= 0)
                {
                    return false;
                }

                int mirrorId = EconomyTradeAgreementsData.AddAgreement(mirrorToCreate);
                return mirrorId > 0;
            }

            return EconomyTradeAgreementsData.UpdateAgreementField(
                mirroredAgreement.AgreementId,
                mirroredAgreement.CountryId,
                mirroredFieldName,
                value,
                sqlType);
        }

        private static bool TryMapTradeFieldToMirroredField(string fieldName, out string mirroredFieldName)
        {
            mirroredFieldName = fieldName switch
            {
                "ExportResourceType" => "ImportResourceType",
                "ImportResourceType" => "ExportResourceType",
                "ExportQty" => "ImportQty",
                "ExportUnitPrice" => "ImportUnitPrice",
                "ImportQty" => "ExportQty",
                "ImportUnitPrice" => "ExportUnitPrice",
                "PeriodDays" => "PeriodDays",
                "DeliveryMode" => "DeliveryMode",
                "QuantityIsPerDay" => "QuantityIsPerDay",
                "IsActive" => "IsActive",
                "ExportBonus" => "ImportBonus",
                "ImportBonus" => "ExportBonus",
                _ => string.Empty
            };

            return !string.IsNullOrWhiteSpace(mirroredFieldName);
        }

        private static bool IsSameTradeResource(string? left, string? right)
        {
            string leftKey = NormalizeResourceKey(left ?? string.Empty);
            string rightKey = NormalizeResourceKey(right ?? string.Empty);
            return string.Equals(leftKey, rightKey, StringComparison.Ordinal);
        }

        private static bool ShouldIgnoreMirrorField(string mirrorFieldName, string ignoredMirrorFieldName)
        {
            return !string.IsNullOrWhiteSpace(ignoredMirrorFieldName) &&
                   string.Equals(mirrorFieldName, ignoredMirrorFieldName, StringComparison.Ordinal);
        }

        private static bool IsMirroredTradeFieldMatch(
            EconomyTradeAgreementRecord candidate,
            EconomyTradeAgreementRecord sourceAgreement,
            string mirrorFieldName,
            string ignoredMirrorFieldName)
        {
            if (ShouldIgnoreMirrorField(mirrorFieldName, ignoredMirrorFieldName))
                return true;

            return mirrorFieldName switch
            {
                "ExportResourceType" => IsSameTradeResource(candidate.ExportResourceType, sourceAgreement.ImportResourceType),
                "ImportResourceType" => IsSameTradeResource(candidate.ImportResourceType, sourceAgreement.ExportResourceType),
                "ExportQty" => candidate.ExportQty == sourceAgreement.ImportQty,
                "ImportQty" => candidate.ImportQty == sourceAgreement.ExportQty,
                "ExportUnitPrice" => candidate.ExportUnitPrice == sourceAgreement.ImportUnitPrice,
                "ImportUnitPrice" => candidate.ImportUnitPrice == sourceAgreement.ExportUnitPrice,
                "PeriodDays" => Math.Max(1, candidate.PeriodDays) == Math.Max(1, sourceAgreement.PeriodDays),
                "DeliveryMode" => string.Equals(
                    NormalizeTradeDeliveryMode(candidate.DeliveryMode),
                    NormalizeTradeDeliveryMode(sourceAgreement.DeliveryMode),
                    StringComparison.Ordinal),
                "QuantityIsPerDay" => candidate.QuantityIsPerDay == sourceAgreement.QuantityIsPerDay,
                "IsActive" => candidate.IsActive == sourceAgreement.IsActive,
                "ExportBonus" => Math.Abs(candidate.ExportBonus - sourceAgreement.ImportBonus) < 0.0001f,
                "ImportBonus" => Math.Abs(candidate.ImportBonus - sourceAgreement.ExportBonus) < 0.0001f,
                _ => true
            };
        }

        private static int CountMirroredTradeMatches(
            EconomyTradeAgreementRecord candidate,
            EconomyTradeAgreementRecord sourceAgreement,
            string ignoredMirrorFieldName)
        {
            int score = 0;
            if (IsMirroredTradeFieldMatch(candidate, sourceAgreement, "ExportResourceType", ignoredMirrorFieldName)) score++;
            if (IsMirroredTradeFieldMatch(candidate, sourceAgreement, "ImportResourceType", ignoredMirrorFieldName)) score++;
            if (IsMirroredTradeFieldMatch(candidate, sourceAgreement, "ExportQty", ignoredMirrorFieldName)) score++;
            if (IsMirroredTradeFieldMatch(candidate, sourceAgreement, "ImportQty", ignoredMirrorFieldName)) score++;
            if (IsMirroredTradeFieldMatch(candidate, sourceAgreement, "ExportUnitPrice", ignoredMirrorFieldName)) score++;
            if (IsMirroredTradeFieldMatch(candidate, sourceAgreement, "ImportUnitPrice", ignoredMirrorFieldName)) score++;
            if (IsMirroredTradeFieldMatch(candidate, sourceAgreement, "PeriodDays", ignoredMirrorFieldName)) score++;
            if (IsMirroredTradeFieldMatch(candidate, sourceAgreement, "DeliveryMode", ignoredMirrorFieldName)) score++;
            if (IsMirroredTradeFieldMatch(candidate, sourceAgreement, "QuantityIsPerDay", ignoredMirrorFieldName)) score++;
            if (IsMirroredTradeFieldMatch(candidate, sourceAgreement, "IsActive", ignoredMirrorFieldName)) score++;
            return score;
        }

        private async Task HandleTradeResourceSelection(ITelegramBotClient botClient, long userId, long chatId, int msgId, string data, CancellationToken cancellationToken)
        {
            if (!EconomyTradeBuffer.TryGetValue(userId, out var agreement))
            {
                await botClient.SendTextMessageAsync(userId, "❌ Не знайдено чернетки угоди. Почніть створення заново.", cancellationToken: cancellationToken);
                ClearEconomyInput(userId);
                return;
            }
            if (!EconomyTradeFlowMode.TryGetValue(userId, out var flowMode))
            {
                await botClient.SendTextMessageAsync(userId, "❌ Спочатку оберіть тип угоди: імпорт, експорт або обидва.", cancellationToken: cancellationToken);
                return;
            }

            string fieldName = data.Contains("_import_", StringComparison.OrdinalIgnoreCase) ? "ImportResourceType" : "ExportResourceType";

            if (data.EndsWith("_skip", StringComparison.OrdinalIgnoreCase))
            {
                if (fieldName == "ExportResourceType")
                    agreement.ExportResourceType = null;
                else
                    agreement.ImportResourceType = null;
            }
            else if (data.Contains("_page_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseTrailingId(data, out int page))
                    await ShowTradeResourceSelect(botClient, userId, chatId, msgId, fieldName, page, cancellationToken);
                return;
            }
            else
            {
                string exportPrefix = "state_economy_trade_res_export_";
                string importPrefix = "state_economy_trade_res_import_";
                string? resource = null;
                if (data.StartsWith(exportPrefix, StringComparison.OrdinalIgnoreCase))
                    resource = data.Substring(exportPrefix.Length);
                else if (data.StartsWith(importPrefix, StringComparison.OrdinalIgnoreCase))
                    resource = data.Substring(importPrefix.Length);

                if (!string.IsNullOrWhiteSpace(resource))
                {
                    if (fieldName == "ExportResourceType")
                        agreement.ExportResourceType = resource;
                    else
                        agreement.ImportResourceType = resource;
                }
            }

            int currentStep = EconomyInputStep.TryGetValue(userId, out var stepFromState)
                ? GetNextTradeFieldIndex(stepFromState, flowMode)
                : GetTradeFieldIndex(fieldName);
            if (currentStep < 0 || currentStep >= TradeFields.Count || !string.Equals(TradeFields[currentStep].FieldName, fieldName, StringComparison.Ordinal))
            {
                int fallbackStep = GetTradeFieldIndex(fieldName);
                if (fallbackStep >= 0)
                    currentStep = fallbackStep;
            }

            EconomyInputStep[userId] = Math.Max(0, currentStep + 1);
            await ProceedTradeCreation(botClient, userId, chatId, msgId, cancellationToken);
        }

        private async Task ShowTradeResourceSelect(ITelegramBotClient botClient, long userId, long chatId, int msgId, string fieldName, int page, CancellationToken cancellationToken)
        {
            MilitaryArsenalCatalog.EnsureResourcesInEconomyCatalog();
            var resources = EconomyResourcesData.GetAll()
                .Where(r => r.IsTradeable)
                .OrderBy(r => r.ResourceType, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (resources.Count == 0)
            {
                resources = EconomyResourcesData.GetAll()
                    .OrderBy(r => r.ResourceType, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            int perPage = 10;
            if (page < 0) page = 0;
            int totalPages = (int)Math.Ceiling(resources.Count / (double)perPage);
            if (totalPages == 0) totalPages = 1;
            if (page >= totalPages) page = totalPages - 1;
            var pageItems = resources.Skip(page * perPage).Take(perPage).ToList();

            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                fieldName == "ExportResourceType" ? "Оберіть ресурс експорту:" : "Оберіть ресурс імпорту:",
                StateKeyboardBuilder.EconomyTradeResourceKeyboard(pageItems, page, totalPages, fieldName == "ExportResourceType" ? "export" : "import"),
                cancellationToken
            );
        }

        private static bool TryParseTradeFlowMode(string data, out TradeFlowMode flowMode)
        {
            flowMode = TradeFlowMode.Both;
            if (string.IsNullOrWhiteSpace(data))
                return false;

            if (data.EndsWith("_export", StringComparison.OrdinalIgnoreCase))
            {
                flowMode = TradeFlowMode.ExportOnly;
                return true;
            }
            if (data.EndsWith("_import", StringComparison.OrdinalIgnoreCase))
            {
                flowMode = TradeFlowMode.ImportOnly;
                return true;
            }
            if (data.EndsWith("_both", StringComparison.OrdinalIgnoreCase))
            {
                flowMode = TradeFlowMode.Both;
                return true;
            }

            return false;
        }

        private static bool IsTradeFieldEnabledForMode(string fieldName, TradeFlowMode flowMode)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
                return false;

            bool exportField = fieldName.StartsWith("Export", StringComparison.Ordinal);
            bool importField = fieldName.StartsWith("Import", StringComparison.Ordinal);
            return flowMode switch
            {
                TradeFlowMode.ExportOnly => !importField,
                TradeFlowMode.ImportOnly => !exportField,
                _ => true
            };
        }

        private static int GetTradeFieldIndex(string fieldName)
        {
            for (int i = 0; i < TradeFields.Count; i++)
            {
                if (string.Equals(TradeFields[i].FieldName, fieldName, StringComparison.Ordinal))
                    return i;
            }
            return -1;
        }

        private static int GetNextTradeFieldIndex(int startIndex, TradeFlowMode flowMode)
        {
            int start = Math.Max(0, startIndex);
            for (int i = start; i < TradeFields.Count; i++)
            {
                if (IsTradeFieldEnabledForMode(TradeFields[i].FieldName, flowMode))
                    return i;
            }
            return TradeFields.Count;
        }

        private async Task ProceedTradeCreation(ITelegramBotClient botClient, long userId, long chatId, int msgId, CancellationToken cancellationToken)
        {
            if (!EconomyTradeBuffer.TryGetValue(userId, out var agreement))
            {
                await botClient.SendTextMessageAsync(userId, "❌ Не знайдено чернетки угоди. Почніть створення заново.", cancellationToken: cancellationToken);
                ClearEconomyInput(userId);
                return;
            }
            if (!EconomyTradeFlowMode.TryGetValue(userId, out var flowMode))
            {
                await botClient.SendTextMessageAsync(userId, "❌ Спочатку оберіть тип угоди: імпорт, експорт або обидва.", cancellationToken: cancellationToken);
                return;
            }

            long targetChatId = chatId;
            int targetMsgId = msgId;
            if ((targetChatId == 0 || targetMsgId == 0) && EconomyTradeUiContext.TryGetValue(userId, out var ui))
            {
                targetChatId = ui.ChatId;
                targetMsgId = ui.MsgId;
            }

            int step = EconomyInputStep.TryGetValue(userId, out var currentStep) ? currentStep : 0;
            step = GetNextTradeFieldIndex(step, flowMode);
            EconomyInputStep[userId] = step;

            if (step >= TradeFields.Count)
            {
                if (!TryValidateTradeAgreementForSubmission(agreement, flowMode, out string validationError, out string? invalidField))
                {
                    await SendHelperMessageAsync(botClient, userId, validationError, cancellationToken);

                    if (string.Equals(invalidField, "PartnerCountryId", StringComparison.Ordinal))
                    {
                        if (targetChatId != 0 && targetMsgId != 0)
                            await ShowTradePartnerSelect(botClient, userId, targetChatId, targetMsgId, 0, cancellationToken);
                        return;
                    }

                    int retryStep = 0;
                    if (!string.IsNullOrWhiteSpace(invalidField))
                    {
                        int invalidIndex = GetTradeFieldIndex(invalidField);
                        if (invalidIndex >= 0)
                            retryStep = invalidIndex;
                    }
                    retryStep = GetNextTradeFieldIndex(retryStep, flowMode);
                    EconomyInputStep[userId] = retryStep;

                    if (retryStep < TradeFields.Count)
                    {
                        var retryField = TradeFields[retryStep];
                        if (retryField.FieldName == "ExportResourceType" || retryField.FieldName == "ImportResourceType")
                        {
                            if (targetChatId != 0 && targetMsgId != 0)
                                await ShowTradeResourceSelect(botClient, userId, targetChatId, targetMsgId, retryField.FieldName, 0, cancellationToken);
                            await SendHelperMessageAsync(botClient, userId, retryField.Prompt, cancellationToken);
                            return;
                        }

                        await SendModeFieldPromptAsync(
                            botClient,
                            userId,
                            EconomyInputMode.AddTrade,
                            retryField.FieldName,
                            retryField.Prompt,
                            cancellationToken);
                    }
                    return;
                }

                await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);

                if (userId == masterID)
                {
                    bool created = TryCreateTradeAgreementsPair(agreement, out int id, out int mirrorId, out string createError);
                    ClearEconomyInput(userId);
                    string result = created
                        ? mirrorId > 0
                            ? $"✅ Торгову угоду створено (Id={id}). Дзеркальна імпорт/експорт угода створена у партнера (Id={mirrorId})."
                            : $"✅ Торгову угоду створено (Id={id})."
                        : (string.IsNullOrWhiteSpace(createError) ? "Не вдалося створити торгову угоду." : createError);
                    await botClient.SendTextMessageAsync(userId, result, cancellationToken: cancellationToken);
                    return;
                }

                long requestId = Interlocked.Increment(ref NextTradeApprovalId);
                var request = new TradeApprovalRequest
                {
                    RequestId = requestId,
                    RequesterUserId = userId,
                    FlowMode = flowMode,
                    Agreement = CloneTradeAgreement(agreement),
                    CreatedAtUtc = DateTime.UtcNow
                };
                TradeApprovalRequests[requestId] = request;

                ClearEconomyInput(userId);

                string approvalText = BuildTradeApprovalText(request);
                await botClient.SendTextMessageAsync(
                    chatId: masterID,
                    text: approvalText,
                    replyMarkup: StateKeyboardBuilder.TradeApprovalKeyboard(requestId),
                    cancellationToken: cancellationToken
                );

                await botClient.SendTextMessageAsync(
                    userId,
                    $"✅ Запит на торгову угоду #{requestId} надіслано майстру. Очікуйте підтвердження.",
                    cancellationToken: cancellationToken);
                return;
            }

            var nextField = TradeFields[step];
            if (nextField.FieldName == "ExportResourceType" || nextField.FieldName == "ImportResourceType")
            {
                if (targetChatId != 0 && targetMsgId != 0)
                    await ShowTradeResourceSelect(botClient, userId, targetChatId, targetMsgId, nextField.FieldName, 0, cancellationToken);

                await SendHelperMessageAsync(botClient, userId, nextField.Prompt, cancellationToken);
                return;
            }

            await SendModeFieldPromptAsync(
                botClient,
                userId,
                EconomyInputMode.AddTrade,
                nextField.FieldName,
                nextField.Prompt,
                cancellationToken);
        }

        private async Task HandleBuildingTemplateInput(ITelegramBotClient botClient, long userId, string input, CancellationToken cancellationToken)
        {
            int step = EconomyInputStep[userId];
            if (step < 0 || step >= BuildingTemplateFields.Count)
                step = 0;

            var field = BuildingTemplateFields[step];
            if (!EconomyBuildingBuffer.TryGetValue(userId, out var building))
            {
                await botClient.SendTextMessageAsync(userId, "❌ Нема доступу. Почніть створення заново.", cancellationToken: cancellationToken);
                ClearEconomyInput(userId);
                return;
            }

            if (field.FieldName == "Level")
            {
                if (!int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out var level) ||
                    level < 1 || level > MaxBuildingLevel)
                {
                    await SendHelperMessageAsync(botClient, userId, $"❌ Вкажіть рівень від 1 до {FmtNumber(MaxBuildingLevel)}.", cancellationToken);
                    return;
                }

                building.Level = level;
            }
            else if (field.FieldName == "Quantity")
            {
                if (!int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out var q) || q < 1)
                {
                    await SendHelperMessageAsync(botClient, userId, "❌ Вкажіть кількість (>=1).", cancellationToken);
                    return;
                }
                EconomyTemplateQuantity[userId] = q;
            }
            else
            {
                if (IsBuildingResourceSelectionField(field.FieldName))
                {
                    if (!TryAssignBuildingField(building, field.FieldName, field.Kind, input, out string error))
                    {
                        await SendHelperMessageAsync(botClient, userId, error, cancellationToken);
                        return;
                    }
                }
                else if (!TryAssignCommon(building, field.FieldName, field.Kind, input, out string error))
                {
                    await SendHelperMessageAsync(botClient, userId, error, cancellationToken);
                    return;
                }
            }

            EconomyInputStep[userId] = step + 1;
             
            while (EconomyInputStep[userId] < BuildingTemplateFields.Count &&
                   IsProductionOnlyBuildingField(BuildingTemplateFields[EconomyInputStep[userId]].FieldName) &&
                   !IsProductionBuilding(building))
            {
                building.ResourceType = null;
                building.InputResourceType = null;
                building.InputAmount = 0m;
                EconomyInputStep[userId]++;
            }

            if (EconomyInputStep[userId] >= BuildingTemplateFields.Count)
            {
                await CompleteBuildingTemplateCreationAsync(botClient, userId, building, cancellationToken);
                return;
            }

            var nextField = BuildingTemplateFields[EconomyInputStep[userId]];
            if (IsBuildingResourceSelectionField(nextField.FieldName))
            {
                await ShowBuildingResourceSelect(botClient, userId, nextField.FieldName, 0, cancellationToken);
                return;
            }
            await SendModeFieldPromptAsync(
                botClient,
                userId,
                EconomyInputMode.AddBuildingTemplate,
                nextField.FieldName,
                nextField.Prompt,
                cancellationToken);
        }

        private static bool TryProjectTemplateBuildingForCreation(
            EconomyBuildingRecord templateBuilding,
            float inflationPct,
            out EconomyBuildingRecord projectedBuilding,
            out decimal projectedCharge,
            out string error)
        {
            projectedBuilding = new EconomyBuildingRecord();
            projectedCharge = 0m;
            error = string.Empty;

            if (templateBuilding == null)
            {
                error = "❌ Дані шаблону пошкоджені.";
                return false;
            }

            var current = CloneBuildingForSimulation(templateBuilding);
            int targetLevel = Math.Clamp(current.Level, 1, MaxBuildingLevel);

            current.Level = 1;
            current.JobsProvided = Math.Max(0, current.JobsProvided);
            current.UpkeepCost = Math.Max(0m, current.UpkeepCost);
            current.BaseProduction = Math.Max(0m, current.BaseProduction);
            current.BuildCost = Math.Max(1m, current.BuildCost);
            current.BuildTurns = Math.Max(0, current.BuildTurns);
            current.InputAmount = Math.Max(0m, current.InputAmount);
            current.StabilityBonus = Math.Max(0f, current.StabilityBonus);
            current.CorruptionReduction = Math.Max(0f, current.CorruptionReduction);
            current.ResearchBonus = Math.Max(0f, current.ResearchBonus);
            current.PopulationGrowthBonus = Math.Max(0f, current.PopulationGrowthBonus);
            current.FestivalBoost = Math.Max(0f, current.FestivalBoost);
            current.ArmyBonus = Math.Max(0, current.ArmyBonus);

            decimal totalCost = GetBuildingCreationCost(current, inflationPct);
            int totalTurns = Math.Max(0, current.BuildTurns);

            while (current.Level < targetLevel)
            {
                decimal stepCost = CalculateBuildingUpgradeCost(current, inflationPct);
                bool upgraded = TryApplyBuildingUpgradeProgression(current);
                if (!upgraded)
                {
                    error = "❌ Не вдалося порахувати шаблон для обраного рівня.";
                    return false;
                }

                totalCost += stepCost;
                totalTurns += Math.Max(1, current.BuildTurns);
            }

            current.Level = targetLevel;
            current.BuildTurns = totalTurns;

            projectedBuilding = current;
            projectedCharge = totalCost <= 0m ? 0m : decimal.Ceiling(totalCost);
            return true;
        }

        private async Task CompleteBuildingTemplateCreationAsync(
            ITelegramBotClient botClient,
            long userId,
            EconomyBuildingRecord templateBuilding,
            CancellationToken cancellationToken)
        {
            int quantity = EconomyTemplateQuantity.TryGetValue(userId, out var q) ? Math.Max(1, q) : 1;

            var country = CountriesData.GetCountryById(templateBuilding.CountryId);
            float inflationPct = country?.Inflation ?? 0f;
            if (!TryProjectTemplateBuildingForCreation(templateBuilding, inflationPct, out var projectedBuilding, out decimal projectedCharge, out string projectionError))
            {
                await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
                ClearEconomyInput(userId);
                await botClient.SendTextMessageAsync(userId, projectionError, cancellationToken: cancellationToken);
                return;
            }

            int added = 0;
            decimal totalCharged = 0m;
            string payerSource = string.Empty;
            string failureReason = string.Empty;

            for (int i = 0; i < quantity; i++)
            {
                var buildingToCreate = CloneBuildingForSimulation(projectedBuilding);
                bool ok = TryAddBuildingWithPayment(
                    userId,
                    buildingToCreate,
                    out _,
                    out decimal chargedCost,
                    out string payer,
                    out string paymentError,
                    projectedCharge);

                if (!ok)
                {
                    failureReason = paymentError;
                    break;
                }

                added++;
                totalCharged += chargedCost;
                if (string.IsNullOrWhiteSpace(payerSource) && chargedCost > 0m)
                    payerSource = payer;
            }

            await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
            ClearEconomyInput(userId);

            string result = added == quantity
                ? $"✅ Додано будівель: {FmtNumber(added)}."
                : $"⚠ Додано {FmtNumber(added)} з {FmtNumber(quantity)}.";

            if (!string.IsNullOrWhiteSpace(failureReason))
                result += $"\n{failureReason}";
            if (totalCharged > 0m)
                result += $"\nСписано: {FmtNumberCompact(totalCharged)} ({payerSource}).";
            if (added > 0)
                result += $"\nПобудовано як L{FmtNumber(projectedBuilding.Level)}: час 1 шт. = {FmtNumber(projectedBuilding.BuildTurns)} дн.";

            await botClient.SendTextMessageAsync(userId, result, cancellationToken: cancellationToken);
        }

        private static string? MapEconomyModeToSelectionKind(EconomyInputMode mode)
        {
            return mode switch
            {
                EconomyInputMode.AddBuilding => "building",
                EconomyInputMode.AddBuildingTemplate => "building",
                EconomyInputMode.AddCompany => "company",
                EconomyInputMode.AddTrade => "trade",
                EconomyInputMode.AddMilitaryUnit => "militaryunit",
                EconomyInputMode.AddVassalObligation => "vassal",
                EconomyInputMode.AddResource => "resource",
                _ => null
            };
        }

        private static readonly (string text, string value)[] DefaultMilitaryRaceOptions =
        {
            ("🧑 Люди", "Human"),
            ("🧝 Ельфи", "Elf"),
            ("😈 Тіфлінги", "Tiefling"),
            ("🍄 Гриби", "Mushroomfolk"),
            ("⛰ Дворфи", "Dwarf"),
            ("🪓 Орки", "Orc"),
            ("🧙 Гноми", "Gnome"),
            ("🦶 Халфлінги", "Halfling")
        };

        private InlineKeyboardMarkup BuildMilitaryRaceInlineKeyboard(long userId, string callbackPrefix)
        {
            var options = new List<(string text, string value)>(DefaultMilitaryRaceOptions.Length + 6);
            var knownRaces = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var option in DefaultMilitaryRaceOptions)
            {
                string normalized = NormalizeUnitRace(option.value);
                if (knownRaces.Add(normalized))
                    options.Add((option.text, normalized));
            }

            int? selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (selectedCountryId.HasValue)
            {
                var country = CountriesData.GetCountryById(selectedCountryId.Value);
                if (country != null)
                {
                    var races = GetPopulationRaces(country);
                    foreach (var pair in races
                        .OrderByDescending(x => x.Value)
                        .ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        string normalized = NormalizeUnitRace(pair.Key);
                        if (string.IsNullOrWhiteSpace(normalized))
                            continue;

                        if (normalized.Length > 40)
                            normalized = normalized.Substring(0, 40).Trim();

                        if (string.IsNullOrWhiteSpace(normalized) || !knownRaces.Add(normalized))
                            continue;

                        options.Add(($"🧬 {FormatUnitRaceUk(normalized)}", normalized));
                    }
                }
            }

            return StateKeyboardBuilder.InlineChoiceKeyboard(options, callbackPrefix, columns: 2);
        }

        private bool TryBuildInlineSelectionKeyboard(long userId, string kind, string fieldName, out string prompt, out InlineKeyboardMarkup keyboard)
        {
            prompt = string.Empty;
            keyboard = StateKeyboardBuilder.EmptyInlineKeyboard();
            const string prefix = "state_inlinepick_";

            if (string.Equals(fieldName, "Category", StringComparison.Ordinal))
            {
                prompt = "Оберіть категорію:";
                keyboard = StateKeyboardBuilder.InlineBuildingCategoryKeyboard(prefix);
                return true;
            }

            if (string.Equals(fieldName, "ProductionType", StringComparison.Ordinal))
            {
                prompt = "Оберіть тип виробництва:";
                keyboard = StateKeyboardBuilder.InlineCompanyProductionTypeKeyboard(prefix);
                return true;
            }

            if (string.Equals(fieldName, "DeliveryMode", StringComparison.Ordinal))
            {
                prompt = "Оберіть тип доставки:";
                var options = new List<(string text, string value)>
                {
                    ("🐎 Каравани", "Caravan"),
                    ("⛵ Водний шлях", "Waterway"),
                    ("🦅 Небесні кур'єри", "SkyCourier")
                };
                keyboard = StateKeyboardBuilder.InlineChoiceKeyboard(options, prefix, columns: 1);
                return true;
            }

            if (string.Equals(fieldName, "UnitLevel", StringComparison.Ordinal))
            {
                prompt = "Оберіть рівень частини:";
                keyboard = StateKeyboardBuilder.InlineMilitaryUnitLevelKeyboard(prefix);
                return true;
            }

            if (string.Equals(fieldName, "UnitType", StringComparison.Ordinal))
            {
                prompt = "Оберіть тип частини:";
                keyboard = StateKeyboardBuilder.InlineMilitaryUnitTypeKeyboard(prefix);
                return true;
            }

            if (string.Equals(fieldName, "UnitRole", StringComparison.Ordinal))
            {
                prompt = "Оберіть роль частини:";
                keyboard = StateKeyboardBuilder.InlineMilitaryUnitRoleKeyboard(prefix);
                return true;
            }

            if (string.Equals(fieldName, "EquipmentTier", StringComparison.Ordinal))
            {
                prompt = "Оберіть клас спорядження:";
                keyboard = StateKeyboardBuilder.InlineMilitaryEquipmentTierKeyboard(prefix);
                return true;
            }

            if (string.Equals(fieldName, "Race", StringComparison.Ordinal))
            {
                prompt = "Оберіть расу частини (включно зі спеціальними расами країни):";
                keyboard = BuildMilitaryRaceInlineKeyboard(userId, prefix);
                return true;
            }

            if (string.Equals(fieldName, "Status", StringComparison.Ordinal))
            {
                if (string.Equals(kind, "militaryunit", StringComparison.OrdinalIgnoreCase))
                {
                    prompt = "Оберіть статус частини:";
                    keyboard = StateKeyboardBuilder.InlineMilitaryUnitStatusKeyboard(prefix);
                    return true;
                }
            }

            if (string.Equals(fieldName, "IsStateCompany", StringComparison.Ordinal) ||
                string.Equals(fieldName, "IsActive", StringComparison.Ordinal) ||
                string.Equals(fieldName, "StateOwned", StringComparison.Ordinal) ||
                string.Equals(fieldName, "IsMobilized", StringComparison.Ordinal) ||
                string.Equals(fieldName, "QuantityIsPerDay", StringComparison.Ordinal) ||
                string.Equals(fieldName, "IsTradeable", StringComparison.Ordinal))
            {
                if (string.Equals(fieldName, "StateOwned", StringComparison.Ordinal))
                    prompt = "Будівля державна?";
                else if (string.Equals(fieldName, "IsStateCompany", StringComparison.Ordinal))
                    prompt = "Це державна компанія?";
                else if (string.Equals(fieldName, "QuantityIsPerDay", StringComparison.Ordinal))
                    prompt = "Кількість вказана за день?";
                else if (string.Equals(fieldName, "IsTradeable", StringComparison.Ordinal))
                    prompt = "Ресурс доступний для торгівлі?";
                else if (string.Equals(fieldName, "IsMobilized", StringComparison.Ordinal))
                    prompt = "Увімкнути екстрену мобілізацію?";
                else if (string.Equals(fieldName, "IsActive", StringComparison.Ordinal))
                {
                    prompt = string.Equals(kind, "trade", StringComparison.OrdinalIgnoreCase)
                        ? "Активувати торгову угоду?"
                        : "Позначити як активне?";
                }
                else
                    prompt = "Оберіть значення:";
                keyboard = StateKeyboardBuilder.InlineBoolChoiceKeyboard(prefix);
                return true;
            }

            return false;
        }

        private async Task SendModeFieldPromptAsync(
            ITelegramBotClient botClient,
            long userId,
            EconomyInputMode mode,
            string fieldName,
            string fallbackPrompt,
            CancellationToken cancellationToken)
        {
            if (mode == EconomyInputMode.AddResource &&
                string.Equals(fieldName, "ParentResourceType", StringComparison.Ordinal))
            {
                string currentResourceType = string.Empty;
                if (EconomyResourceBuffer.TryGetValue(userId, out var bufferedResource))
                    currentResourceType = bufferedResource.ResourceType ?? string.Empty;

                await ShowResourceParentSelect(botClient, userId, currentResourceType, 0, cancellationToken);
                return;
            }

            var kind = MapEconomyModeToSelectionKind(mode);
            if (!string.IsNullOrWhiteSpace(kind) &&
                TryBuildInlineSelectionKeyboard(userId, kind, fieldName, out string inlinePrompt, out var keyboard))
            {
                await SendHelperMessageAsync(botClient, userId, inlinePrompt, keyboard, cancellationToken);
                return;
            }

            await SendHelperMessageAsync(botClient, userId, fallbackPrompt, cancellationToken);
        }

        private static string ResolveFieldPromptForKind(string kind, string fieldName)
        {
            IReadOnlyList<(string FieldName, string Prompt, FieldKind Kind)> fields = kind switch
            {
                "building" => BuildingFields,
                "company" => CompanyFields,
                "festival" => FestivalFields,
                "trade" => TradeFields,
                "militaryunit" => MilitaryUnitFields,
                "vassal" => VassalFields,
                "resource" => ResourceFields,
                _ => Array.Empty<(string FieldName, string Prompt, FieldKind Kind)>()
            };

            foreach (var f in fields)
            {
                if (string.Equals(f.FieldName, fieldName, StringComparison.Ordinal))
                    return f.Prompt;
            }

            if (string.Equals(kind, "trade", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(fieldName, "PartnerName", StringComparison.Ordinal))
            {
                return "Назва країни-партнера (або '-' якщо немає):";
            }

            return $"Введіть значення для поля «{fieldName}»:";
        }

        private static bool IsAllowedInlineSelectionValue(string kind, string fieldName, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (string.Equals(fieldName, "Category", StringComparison.Ordinal))
            {
                return value is "Housing" or "Production" or "Administration" or "Science" or "Military" or "Culture" or "Other";
            }

            if (string.Equals(fieldName, "ProductionType", StringComparison.Ordinal))
            {
                return value is "General" or "MilitaryIndustry" or "ArcaneArms" or "SiegeWorks" or "Weapons" or "Food" or "Industry" or "Logistics" or "Services";
            }

            if (string.Equals(fieldName, "DeliveryMode", StringComparison.Ordinal))
            {
                return value is "Caravan" or "Waterway" or "SkyCourier";
            }

            if (string.Equals(fieldName, "UnitLevel", StringComparison.Ordinal))
            {
                return value is "Garrison" or "Squad" or "Platoon" or "Company" or "Battalion" or "Regiment" or "Brigade" or "Division" or "Legion" or "Fleet";
            }

            if (string.Equals(fieldName, "UnitType", StringComparison.Ordinal))
            {
                return value is "Infantry" or "Spearmen" or "Archers" or "Crossbowmen" or "Cavalry" or "HeavyInfantry" or "MageCorps" or "Siege" or "Scouts" or "NavalFleet" or "Support";
            }

            if (string.Equals(fieldName, "UnitRole", StringComparison.Ordinal))
            {
                return value is "Field" or "Garrison" or "Vanguard" or "Reserve" or "SiegeTrain" or "Fleet";
            }

            if (string.Equals(fieldName, "EquipmentTier", StringComparison.Ordinal))
            {
                return value is "Militia" or "Standard" or "Heavy" or "Elite" or "Arcane" or "Naval";
            }

            if (string.Equals(fieldName, "Race", StringComparison.Ordinal))
            {
                string normalizedRace = NormalizeUnitRace(value);
                return !string.IsNullOrWhiteSpace(normalizedRace) && normalizedRace.Length <= 40;
            }

            if (string.Equals(fieldName, "Status", StringComparison.Ordinal) &&
                string.Equals(kind, "militaryunit", StringComparison.OrdinalIgnoreCase))
            {
                return value is "Forming" or "Active" or "Reserve";
            }

            if (string.Equals(fieldName, "IsStateCompany", StringComparison.Ordinal) ||
                string.Equals(fieldName, "IsActive", StringComparison.Ordinal) ||
                string.Equals(fieldName, "StateOwned", StringComparison.Ordinal) ||
                string.Equals(fieldName, "IsMobilized", StringComparison.Ordinal) ||
                string.Equals(fieldName, "QuantityIsPerDay", StringComparison.Ordinal) ||
                string.Equals(fieldName, "IsTradeable", StringComparison.Ordinal))
            {
                return value is "true" or "false";
            }

            return false;
        }

        private async Task HandleInlineSelectionChoice(ITelegramBotClient botClient, long userId, string rawChoice, CancellationToken cancellationToken)
        {
            string choice = (rawChoice ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(choice))
                return;

            if (string.Equals(choice, "cancel", StringComparison.OrdinalIgnoreCase))
            {
                await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
                EconomyEditState.Remove(userId);
                ClearEconomyInput(userId);
                await botClient.SendTextMessageAsync(userId, "Дію скасовано.", cancellationToken: cancellationToken);
                return;
            }

            if (EconomyEditState.TryGetValue(userId, out var edit))
            {
                if (!IsAllowedInlineSelectionValue(edit.Kind, edit.FieldName, choice))
                {
                    await botClient.SendTextMessageAsync(userId, "❌ Невірний вибір.", cancellationToken: cancellationToken);
                    return;
                }

                await HandleEditInput(botClient, userId, edit, choice, cancellationToken);
                return;
            }

            if (!EconomyInputState.TryGetValue(userId, out var mode) || mode == EconomyInputMode.None)
                return;

            var kind = MapEconomyModeToSelectionKind(mode);
            if (string.IsNullOrWhiteSpace(kind))
                return;

            IReadOnlyList<(string FieldName, string Prompt, FieldKind Kind)> fields = mode switch
            {
                EconomyInputMode.AddBuilding => BuildingFields,
                EconomyInputMode.AddBuildingTemplate => BuildingTemplateFields,
                EconomyInputMode.AddCompany => CompanyFields,
                EconomyInputMode.AddTrade => TradeFields,
                EconomyInputMode.AddMilitaryUnit => MilitaryUnitCreationFields,
                EconomyInputMode.AddVassalObligation => VassalFields,
                EconomyInputMode.AddResource => ResourceFields,
                _ => Array.Empty<(string FieldName, string Prompt, FieldKind Kind)>()
            };

            int step = EconomyInputStep.TryGetValue(userId, out int currentStep) ? currentStep : 0;
            if (step < 0 || step >= fields.Count)
                return;

            var field = fields[step];
            if (!IsAllowedInlineSelectionValue(kind!, field.FieldName, choice))
                return;

            switch (mode)
            {
                case EconomyInputMode.AddBuilding:
                    await HandleBuildingInput(botClient, userId, choice, cancellationToken);
                    break;
                case EconomyInputMode.AddBuildingTemplate:
                    await HandleBuildingTemplateInput(botClient, userId, choice, cancellationToken);
                    break;
                case EconomyInputMode.AddCompany:
                    await HandleCompanyInput(botClient, userId, choice, cancellationToken);
                    break;
                case EconomyInputMode.AddTrade:
                    await HandleTradeInput(botClient, userId, choice, cancellationToken);
                    break;
                case EconomyInputMode.AddMilitaryUnit:
                    await HandleMilitaryUnitInput(botClient, userId, choice, cancellationToken);
                    break;
                case EconomyInputMode.AddVassalObligation:
                    await HandleVassalInput(botClient, userId, choice, cancellationToken);
                    break;
                case EconomyInputMode.AddResource:
                    await HandleResourceInput(botClient, userId, choice, cancellationToken);
                    break;
            }
        }

        private static void ClearEconomyInput(long userId)
        {
            EconomyInputState[userId] = EconomyInputMode.None;
            EconomyInputStep[userId] = 0;
            EconomyBuildingBuffer.Remove(userId);
            EconomyBuildingCitySelectionContext.Remove(userId);
            EconomyBuildingCityChoices.Remove(userId);
            EconomyCompanyBuffer.Remove(userId);
            EconomyFestivalBuffer.Remove(userId);
            EconomyTradeBuffer.Remove(userId);
            EconomyTradeFlowMode.Remove(userId);
            EconomyMilitaryUnitBuffer.Remove(userId);
            EconomyVassalBuffer.Remove(userId);
            EconomyTemplateQuantity.Remove(userId);
            EconomyHelperMessages.Remove(userId);
            EconomyUserMessages.Remove(userId);
            EconomyMoneyAmountBuffer.Remove(userId);
            EconomyMoneyReasonBuffer.Remove(userId);
            EconomyTradeUiContext.Remove(userId);
            EconomyIncomeLimitUiContext.Remove(userId);
            EconomySectorUiContext.Remove(userId);
            EconomyPublicUiContext.Remove(userId);
            EconomyPublicModuleRouteByUser.Remove(userId);
            EconomyResourceBuffer.Remove(userId);
            EconomySanctionsEdit.Remove(userId);
            EconomySanctionsUiContext.Remove(userId);
            EconomyProductSanctionsEdit.Remove(userId);
            EconomyProductSanctionsUiContext.Remove(userId);
            EconomyCurrencyUiContext.Remove(userId);
            EconomyMilitaryProvisionUiContext.Remove(userId);
            EconomyCurrencyConvertState.Remove(userId);
            EconomyCurrencyActionState.Remove(userId);
            MilitaryBattlePlanByUser.Remove(userId);
        }

        private async Task StartResourceCreation(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            EconomyInputState[userId] = EconomyInputMode.AddResource;
            EconomyInputStep[userId] = 0;
            EconomyResourceBuffer[userId] = new EconomyResourceRecord();
            await SendModeFieldPromptAsync(
                botClient,
                userId,
                EconomyInputMode.AddResource,
                ResourceFields[0].FieldName,
                ResourceFields[0].Prompt,
                cancellationToken);
        }

        private async Task HandleResourceInput(ITelegramBotClient botClient, long userId, string input, CancellationToken cancellationToken)
        {
            int step = EconomyInputStep[userId];
            if (step < 0 || step >= ResourceFields.Count)
                step = 0;

            var field = ResourceFields[step];
            if (!EconomyResourceBuffer.TryGetValue(userId, out var resource))
            {
                resource = new EconomyResourceRecord();
                EconomyResourceBuffer[userId] = resource;
            }

            if (!TryAssignResourceField(resource, field.FieldName, field.Kind, input, out string error))
            {
                await SendHelperMessageAsync(botClient, userId, error, cancellationToken);
                return;
            }

            EconomyInputStep[userId] = step + 1;
            if (EconomyInputStep[userId] >= ResourceFields.Count)
            {
                bool ok = EconomyResourcesData.AddResource(resource);
                await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
                ClearEconomyInput(userId);
                string result = ok ? $"✅ Ресурс додано: {resource.ResourceType}" : "❌ Не вдалося додати ресурс (можливо, вже існує).";
                await botClient.SendTextMessageAsync(userId, result, cancellationToken: cancellationToken);
                return;
            }

            var nextField = ResourceFields[EconomyInputStep[userId]];
            await SendModeFieldPromptAsync(
                botClient,
                userId,
                EconomyInputMode.AddResource,
                nextField.FieldName,
                nextField.Prompt,
                cancellationToken);
        }

        private static bool TryAssignResourceField(EconomyResourceRecord resource, string fieldName, FieldKind kind, string input, out string error)
        {
            error = "";
            if (fieldName == "ResourceType")
            {
                if (string.IsNullOrWhiteSpace(input))
                {
                    error = "❌ Назва ресурсу не може бути порожньою.";
                    return false;
                }
                resource.ResourceType = input.Trim();
                return true;
            }

            if (fieldName == "ParentResourceType")
            {
                if (!TryResolveParentResourceInput(input, resource.ResourceType, out var parentResourceType, out error))
                    return false;

                resource.ParentResourceType = parentResourceType;
                return true;
            }

            return TryAssignCommon(resource, fieldName, kind, input, out error);
        }

        private static bool TryResolveParentResourceInput(
            string? input,
            string? currentResourceType,
            out string? parentResourceType,
            out string error)
        {
            parentResourceType = null;
            error = string.Empty;

            string raw = (input ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(raw) || raw == "-")
                return true;

            if (!string.IsNullOrWhiteSpace(currentResourceType) &&
                string.Equals(raw, currentResourceType, StringComparison.OrdinalIgnoreCase))
            {
                error = "❌ Ресурс не може бути батьківським сам для себе.";
                return false;
            }

            var resources = EconomyResourcesData.GetAll();
            var exact = resources.FirstOrDefault(r => string.Equals(r.ResourceType, raw, StringComparison.OrdinalIgnoreCase));
            if (exact == null)
            {
                string normalized = NormalizeResourceKey(raw);
                var normalizedMatches = resources
                    .Where(r => NormalizeResourceKey(r.ResourceType) == normalized)
                    .ToList();

                if (normalizedMatches.Count == 1)
                {
                    exact = normalizedMatches[0];
                }
                else
                {
                    var byType = resources
                        .Where(r => !string.IsNullOrWhiteSpace(r.ResourceType))
                        .GroupBy(r => r.ResourceType, StringComparer.OrdinalIgnoreCase)
                        .Select(g => g.First())
                        .ToDictionary(r => r.ResourceType, r => r, StringComparer.OrdinalIgnoreCase);
                    string? trackedName = TrackedResourceResolver.ResolveTrackedResourceName(raw, byType);
                    if (!string.IsNullOrWhiteSpace(trackedName) && byType.TryGetValue(trackedName, out var trackedResource))
                    {
                        exact = trackedResource;
                    }
                    else
                    {
                        string samples = string.Join(", ", resources
                            .Select(r => r.ResourceType)
                            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                            .Take(12));
                        error = $"❌ Невідомий батьківський тип. Використовуйте значення з EconomyResources. Приклади: {samples}";
                        return false;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(currentResourceType) &&
                string.Equals(exact.ResourceType, currentResourceType, StringComparison.OrdinalIgnoreCase))
            {
                error = "❌ Ресурс не може бути батьківським сам для себе.";
                return false;
            }

            if (WouldCreateResourceCycle(currentResourceType, exact.ResourceType, resources))
            {
                error = "❌ Цей батьківський тип створює циклічну залежність ресурсів.";
                return false;
            }

            parentResourceType = exact.ResourceType;
            return true;
        }

        private static bool WouldCreateResourceCycle(
            string? resourceType,
            string parentResourceType,
            List<EconomyResourceRecord> resources)
        {
            if (string.IsNullOrWhiteSpace(resourceType) || string.IsNullOrWhiteSpace(parentResourceType))
                return false;

            string child = resourceType.Trim();
            var map = (resources ?? new List<EconomyResourceRecord>())
                .Where(r => !string.IsNullOrWhiteSpace(r.ResourceType))
                .GroupBy(r => r.ResourceType, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToDictionary(r => r.ResourceType, r => r, StringComparer.OrdinalIgnoreCase);

            string? current = parentResourceType;
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (!string.IsNullOrWhiteSpace(current) && visited.Add(current))
            {
                if (string.Equals(current, child, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (!map.TryGetValue(current, out var record))
                    break;

                current = record.ParentResourceType;
            }

            return false;
        }

        private static string FormatBuildingCityName(string? cityName)
        {
            return NormalizeBuildingCityName(cityName);
        }

        private static string NormalizeBuildingCityName(string? cityName)
        {
            string raw = CollapseWhitespaces((cityName ?? string.Empty).Trim());
            if (string.IsNullOrWhiteSpace(raw))
                return EconomyBuildingRecord.DefaultCityName;

            if (raw.Length > 60)
                raw = raw.Substring(0, 60).Trim();

            return string.IsNullOrWhiteSpace(raw) ? EconomyBuildingRecord.DefaultCityName : raw;
        }

        private static bool TryNormalizeBuildingCityInput(string? input, out string cityName, out string error)
        {
            cityName = string.Empty;
            error = string.Empty;

            string raw = CollapseWhitespaces((input ?? string.Empty).Trim());
            if (string.IsNullOrWhiteSpace(raw))
            {
                error = "❌ Назва міста не може бути порожньою.";
                return false;
            }

            if (raw.Length < 2)
            {
                error = "❌ Назва міста закоротка (мінімум 2 символи).";
                return false;
            }

            if (raw.Length > 60)
            {
                error = "❌ Назва міста задовга (максимум 60 символів).";
                return false;
            }

            cityName = NormalizeBuildingCityName(raw);
            return true;
        }

        private static string CollapseWhitespaces(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return string.Join(" ", value
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(part => !string.IsNullOrWhiteSpace(part)));
        }

        private static void EnsureCountryCityCatalog(int countryId, List<EconomyBuildingRecord>? buildings = null)
        {
            if (countryId <= 0)
                return;

            EconomyCitiesData.EnsureCity(countryId, EconomyBuildingRecord.DefaultCityName);

            var allBuildings = buildings ?? EconomyBuildingsData.GetByCountry(countryId);
            foreach (string city in allBuildings
                .Select(b => NormalizeBuildingCityName(b.CityName))
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                EconomyCitiesData.EnsureCity(countryId, city);
            }
        }

        private static void SyncCountryCitiesCount(int countryId, CountryRecord? cachedCountry = null, bool persist = true)
        {
            if (countryId <= 0)
                return;

            int cityCount = EconomyCitiesData.CountByCountry(countryId);
            if (cityCount <= 0)
                cityCount = 1;

            var country = cachedCountry ?? CountriesData.GetCountryById(countryId);
            if (country == null)
                return;

            country.Cities = cityCount;
            if (persist)
            {
                country.LastUpdate = DateTime.UtcNow;
                CountriesData.UpdateCountry(country);
            }
        }

        private static List<string> GetAvailableBuildingCities(int countryId, List<EconomyBuildingRecord>? buildings = null)
        {
            var citiesSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (countryId > 0)
            {
                foreach (var city in EconomyCitiesData.GetByCountry(countryId))
                    citiesSet.Add(NormalizeBuildingCityName(city.CityName));
            }

            var sourceBuildings = buildings ?? (countryId > 0 ? EconomyBuildingsData.GetByCountry(countryId) : new List<EconomyBuildingRecord>());
            foreach (var city in sourceBuildings
                .Select(b => NormalizeBuildingCityName(b.CityName))
                .Where(c => !string.IsNullOrWhiteSpace(c)))
            {
                citiesSet.Add(city);
            }

            citiesSet.Add(EconomyBuildingRecord.DefaultCityName);

            var cities = citiesSet
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                .ToList();

            int capitalIndex = cities.FindIndex(c => string.Equals(c, EconomyBuildingRecord.DefaultCityName, StringComparison.OrdinalIgnoreCase));
            if (capitalIndex < 0)
            {
                cities.Insert(0, EconomyBuildingRecord.DefaultCityName);
            }
            else if (capitalIndex > 0)
            {
                string capitalName = cities[capitalIndex];
                cities.RemoveAt(capitalIndex);
                cities.Insert(0, capitalName);
            }

            return cities;
        }

        private async Task StartTreasuryChange(ITelegramBotClient botClient, long userId, bool add, CancellationToken cancellationToken)
        {
            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await botClient.SendTextMessageAsync(userId, "Не знайдено країни. Оберіть країну або призначте ідентифікатор країни персонажу.", cancellationToken: cancellationToken);
                return;
            }

            EconomyInputState[userId] = add ? EconomyInputMode.AddTreasury : EconomyInputMode.RemoveTreasury;
            EconomyInputStep[userId] = 0;
            EconomyMoneyAmountBuffer.Remove(userId);
            EconomyMoneyReasonBuffer.Remove(userId);

            string prompt = add
                ? "Введіть суму для додавання в казну (золото, тільки число):"
                : "Введіть суму для вилучення з казни (золото, тільки число):";
            await SendHelperMessageAsync(botClient, userId, prompt, cancellationToken);
        }

        private async Task HandleTreasuryInput(ITelegramBotClient botClient, long userId, string input, bool add, CancellationToken cancellationToken)
        {
            int step = EconomyInputStep[userId];
            if (step < 0 || step > 1)
                step = 0;

            if (step == 0)
            {
                if (!long.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
                {
                    await SendHelperMessageAsync(botClient, userId, "❌ Вкажіть суму (>0).", cancellationToken);
                    return;
                }
                if (amount > int.MaxValue)
                {
                    await SendHelperMessageAsync(botClient, userId, $"❌ Максимальна сума: {FmtNumber(int.MaxValue)}.", cancellationToken);
                    return;
                }

                EconomyMoneyAmountBuffer[userId] = amount;
                EconomyInputStep[userId] = 1;
                await SendHelperMessageAsync(botClient, userId, "Вкажіть причину (можна пропустити):", cancellationToken);
                return;
            }

            string reason = string.IsNullOrWhiteSpace(input)
                ? (add ? "Надходження до казни" : "Виплата з казни")
                : input.Trim();
            EconomyMoneyReasonBuffer[userId] = reason;

            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
                ClearEconomyInput(userId);
                await botClient.SendTextMessageAsync(userId, "Не знайдено країни.", cancellationToken: cancellationToken);
                return;
            }

            long amountValue = EconomyMoneyAmountBuffer.TryGetValue(userId, out var a) ? a : 0;
            if (amountValue <= 0)
            {
                await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
                ClearEconomyInput(userId);
                await botClient.SendTextMessageAsync(userId, "❌ Невірна сума.", cancellationToken: cancellationToken);
                return;
            }

            var country = CountriesData.GetCountryById(selectedCountryId.Value);
            if (country == null)
            {
                await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
                ClearEconomyInput(userId);
                await botClient.SendTextMessageAsync(userId, "Країну не знайдено.", cancellationToken: cancellationToken);
                return;
            }

            int userGold = UserData.DataBaseValueReader<int>(userId, "GoldenCoin");
            if (add)
            {
                if (userId != masterID && userGold < amountValue)
                {
                    await SendHelperMessageAsync(botClient, userId, "❌ Недостатньо золота у гаманці.", cancellationToken);
                    return;
                }
                if (userId != masterID)
                {
                    userGold -= (int)amountValue;
                    UserData.UpdateDataBaseValue("GoldenCoin", userId, userGold);
                }
                country.Treasury += amountValue;
            }
            else
            {
                if (country.Treasury < amountValue)
                {
                    await SendHelperMessageAsync(botClient, userId, "❌ Недостатньо в казні.", cancellationToken);
                    return;
                }
                country.Treasury -= amountValue;
                userGold += (int)amountValue;
                UserData.UpdateDataBaseValue("GoldenCoin", userId, userGold);
            }

            CountriesData.UpdateCountry(country);
            EconomyMoneyLogData.AddLog(country.CountryId, userId, amountValue, add ? "Income" : "Expense", reason, 0);

            await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
            ClearEconomyInput(userId);
            string result = add
                ? $"✅ Додано до казни {FmtNumber(amountValue)}."
                : $"✅ Вилучено з казни {FmtNumber(amountValue)}.";
            await botClient.SendTextMessageAsync(userId, result, cancellationToken: cancellationToken);
        }

        private async Task SendHelperMessageAsync(ITelegramBotClient botClient, long userId, string text, CancellationToken cancellationToken)
        {
            var msg = await botClient.SendTextMessageAsync(userId, text, cancellationToken: cancellationToken);
            if (!EconomyHelperMessages.TryGetValue(userId, out var list))
            {
                list = new List<int>();
                EconomyHelperMessages[userId] = list;
            }
            list.Add(msg.MessageId);
        }

        private async Task SendHelperMessageAsync(ITelegramBotClient botClient, long userId, string text, Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup replyMarkup, CancellationToken cancellationToken)
        {
            var msg = await botClient.SendTextMessageAsync(userId, text, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
            if (!EconomyHelperMessages.TryGetValue(userId, out var list))
            {
                list = new List<int>();
                EconomyHelperMessages[userId] = list;
            }
            list.Add(msg.MessageId);
        }

        private static string GetBuildingResourceSelectPrompt(string fieldName)
        {
            if (string.Equals(fieldName, "InputResourceType", StringComparison.Ordinal))
                return "Оберіть вхідний ресурс (що будівля споживає):";
            return "Оберіть вихідний ресурс (що будівля виробляє):";
        }

        private static string GetResourceParentSelectPrompt(string currentResourceType)
        {
            if (!string.IsNullOrWhiteSpace(currentResourceType))
                return $"Оберіть батьківський тип для ресурсу «{FormatResourceTypeUk(currentResourceType)}»:";
            return "Оберіть батьківський тип ресурсу:";
        }

        private static List<EconomyResourceRecord> GetResourceParentCandidates(string? currentResourceType)
        {
            return EconomyResourcesData.GetAll()
                .Where(r => !string.IsNullOrWhiteSpace(r.ResourceType))
                .GroupBy(r => r.ResourceType, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .Where(r => string.IsNullOrWhiteSpace(currentResourceType) ||
                            !string.Equals(r.ResourceType, currentResourceType, StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r.ResourceType, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private async Task ShowResourceParentSelect(ITelegramBotClient botClient, long userId, string currentResourceType, int page, CancellationToken cancellationToken)
        {
            var resources = GetResourceParentCandidates(currentResourceType);

            int perPage = 10;
            if (page < 0) page = 0;
            int totalPages = (int)Math.Ceiling(resources.Count / (double)perPage);
            if (totalPages == 0) totalPages = 1;
            if (page >= totalPages) page = totalPages - 1;

            var pageItems = resources.Skip(page * perPage).Take(perPage).ToList();
            await SendHelperMessageAsync(
                botClient,
                userId,
                GetResourceParentSelectPrompt(currentResourceType),
                StateKeyboardBuilder.EconomyResourceParentKeyboard(pageItems, page, totalPages),
                cancellationToken
            );
        }

        private async Task EditResourceParentSelect(ITelegramBotClient botClient, long chatId, int msgId, string currentResourceType, int page, CancellationToken cancellationToken)
        {
            var resources = GetResourceParentCandidates(currentResourceType);

            int perPage = 10;
            if (page < 0) page = 0;
            int totalPages = (int)Math.Ceiling(resources.Count / (double)perPage);
            if (totalPages == 0) totalPages = 1;
            if (page >= totalPages) page = totalPages - 1;

            var pageItems = resources.Skip(page * perPage).Take(perPage).ToList();
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                GetResourceParentSelectPrompt(currentResourceType),
                StateKeyboardBuilder.EconomyResourceParentKeyboard(pageItems, page, totalPages),
                cancellationToken
            );
        }

        private async Task HandleResourceParentSelection(ITelegramBotClient botClient, long userId, long chatId, int msgId, string data, CancellationToken cancellationToken)
        {
            bool isEditMode = false;
            string currentResourceType = string.Empty;

            if (EconomyEditState.TryGetValue(userId, out var edit) &&
                string.Equals(edit.Kind, "resource", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(edit.FieldName, "ParentResourceType", StringComparison.Ordinal))
            {
                isEditMode = true;
                currentResourceType = edit.Key ?? string.Empty;
            }
            else if (EconomyInputState.TryGetValue(userId, out var mode) && mode == EconomyInputMode.AddResource)
            {
                int step = EconomyInputStep.TryGetValue(userId, out var currentStep) ? currentStep : 0;
                if (step < 0 || step >= ResourceFields.Count)
                    return;

                var field = ResourceFields[step];
                if (!string.Equals(field.FieldName, "ParentResourceType", StringComparison.Ordinal))
                    return;

                if (EconomyResourceBuffer.TryGetValue(userId, out var resourceBuffer))
                    currentResourceType = resourceBuffer.ResourceType ?? string.Empty;
            }
            else
            {
                return;
            }

            if (data.StartsWith("state_economy_resource_parent_page_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseTrailingId(data, out int page))
                    await EditResourceParentSelect(botClient, chatId, msgId, currentResourceType, page, cancellationToken);
                return;
            }

            if (string.Equals(data, "state_economy_resource_parent_manual", StringComparison.OrdinalIgnoreCase))
            {
                await SendHelperMessageAsync(botClient, userId, "Введіть батьківський тип ресурсу вручну (або '-' якщо немає):", cancellationToken);
                return;
            }

            string selectedValue;
            if (string.Equals(data, "state_economy_resource_parent_none", StringComparison.OrdinalIgnoreCase))
            {
                selectedValue = "-";
            }
            else if (data.StartsWith("state_economy_resource_parent_pick|", StringComparison.OrdinalIgnoreCase))
            {
                var parts = data.Split('|');
                if (parts.Length < 2)
                    return;

                string selectedResourceType = DecodeResourceKey(parts[1]);
                if (string.IsNullOrWhiteSpace(selectedResourceType))
                {
                    await SendHelperMessageAsync(botClient, userId, "❌ Невірний вибір ресурсу.", cancellationToken);
                    return;
                }

                selectedValue = selectedResourceType;
            }
            else
            {
                return;
            }

            if (isEditMode)
            {
                if (!EconomyEditState.TryGetValue(userId, out var currentEdit) ||
                    !string.Equals(currentEdit.Kind, "resource", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(currentEdit.FieldName, "ParentResourceType", StringComparison.Ordinal))
                {
                    return;
                }

                await HandleEditInput(botClient, userId, currentEdit, selectedValue, cancellationToken);
                return;
            }

            await HandleResourceInput(botClient, userId, selectedValue, cancellationToken);
        }

        private async Task ShowBuildingResourceSelect(ITelegramBotClient botClient, long userId, string fieldName, int page, CancellationToken cancellationToken)
        {
            MilitaryArsenalCatalog.EnsureResourcesInEconomyCatalog();
            var resources = EconomyResourcesData.GetAll()
                .OrderBy(r => r.ResourceType)
                .ToList();

            int perPage = 10;
            if (page < 0) page = 0;
            int totalPages = (int)Math.Ceiling(resources.Count / (double)perPage);
            if (totalPages == 0) totalPages = 1;
            if (page >= totalPages) page = totalPages - 1;

            var pageItems = resources.Skip(page * perPage).Take(perPage).ToList();
            await SendHelperMessageAsync(
                botClient,
                userId,
                GetBuildingResourceSelectPrompt(fieldName),
                StateKeyboardBuilder.EconomyBuildingResourceKeyboard(pageItems, page, totalPages),
                cancellationToken
            );
        }

        private async Task EditBuildingResourceSelect(ITelegramBotClient botClient, long chatId, int msgId, string fieldName, int page, CancellationToken cancellationToken)
        {
            MilitaryArsenalCatalog.EnsureResourcesInEconomyCatalog();
            var resources = EconomyResourcesData.GetAll()
                .OrderBy(r => r.ResourceType)
                .ToList();

            int perPage = 10;
            if (page < 0) page = 0;
            int totalPages = (int)Math.Ceiling(resources.Count / (double)perPage);
            if (totalPages == 0) totalPages = 1;
            if (page >= totalPages) page = totalPages - 1;

            var pageItems = resources.Skip(page * perPage).Take(perPage).ToList();
            await SafeEditMessageTextAsync(
                botClient,
                chatId,
                msgId,
                GetBuildingResourceSelectPrompt(fieldName),
                StateKeyboardBuilder.EconomyBuildingResourceKeyboard(pageItems, page, totalPages),
                cancellationToken
            );
        }

        private async Task HandleBuildingResourceSelection(ITelegramBotClient botClient, long userId, long chatId, int msgId, string data, CancellationToken cancellationToken)
        {
            if (!EconomyInputState.TryGetValue(userId, out var mode) ||
                (mode != EconomyInputMode.AddBuilding && mode != EconomyInputMode.AddBuildingTemplate))
                return;

            var fieldList = mode == EconomyInputMode.AddBuilding ? BuildingFields : BuildingTemplateFields;
            int step = EconomyInputStep[userId];
            if (step < 0 || step >= fieldList.Count)
                step = 0;

            var field = fieldList[step];
            if (!IsBuildingResourceSelectionField(field.FieldName))
                return;

            if (!EconomyBuildingBuffer.TryGetValue(userId, out var building))
            {
                building = new EconomyBuildingRecord();
                EconomyBuildingBuffer[userId] = building;
            }

            if (data.Contains("_page_", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseTrailingId(data, out int page))
                    await EditBuildingResourceSelect(botClient, chatId, msgId, field.FieldName, page, cancellationToken);
                return;
            }

            if (data.EndsWith("_skip", StringComparison.OrdinalIgnoreCase))
            {
                 
                if (mode != EconomyInputMode.AddBuildingTemplate)
                {
                    if (string.Equals(field.FieldName, "ResourceType", StringComparison.Ordinal))
                        building.ResourceType = null;
                    else
                        building.InputResourceType = null;
                }
            }
            else
            {
                const string prefix = "state_economy_building_res_";
                if (data.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    string resource = data.Substring(prefix.Length);
                    if (string.Equals(field.FieldName, "ResourceType", StringComparison.Ordinal))
                        building.ResourceType = string.IsNullOrWhiteSpace(resource) ? null : resource;
                    else
                        building.InputResourceType = string.IsNullOrWhiteSpace(resource) ? null : resource;
                }
            }

            EconomyInputStep[userId] = step + 1;
            if (mode == EconomyInputMode.AddBuildingTemplate)
            {
                if (EconomyInputStep[userId] >= fieldList.Count)
                {
                    await CompleteBuildingTemplateCreationAsync(botClient, userId, building, cancellationToken);
                    return;
                }

                 
                while (EconomyInputStep[userId] < fieldList.Count &&
                       IsProductionOnlyBuildingField(fieldList[EconomyInputStep[userId]].FieldName) &&
                       !IsProductionBuilding(building))
                {
                    building.ResourceType = null;
                    building.InputResourceType = null;
                    building.InputAmount = 0m;
                    EconomyInputStep[userId]++;
                }

                if (EconomyInputStep[userId] >= fieldList.Count)
                {
                    await CompleteBuildingTemplateCreationAsync(botClient, userId, building, cancellationToken);
                    return;
                }

                var nextTemplateField = fieldList[EconomyInputStep[userId]];
                if (IsBuildingResourceSelectionField(nextTemplateField.FieldName))
                {
                    await ShowBuildingResourceSelect(botClient, userId, nextTemplateField.FieldName, 0, cancellationToken);
                    return;
                }
                await SendModeFieldPromptAsync(
                    botClient,
                    userId,
                    EconomyInputMode.AddBuildingTemplate,
                    nextTemplateField.FieldName,
                    nextTemplateField.Prompt,
                    cancellationToken);
                return;
            }

            if (EconomyInputStep[userId] >= fieldList.Count)
            {
                bool ok = TryAddBuildingWithPayment(userId, building, out int id, out decimal chargedCost, out string payerSource, out string paymentError);
                await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
                ClearEconomyInput(userId);
                string result;
                if (ok)
                {
                    result = $"✅ Будівлю додано (Id={id}).";
                    if (chargedCost > 0m)
                        result += $"\nСписано: {FmtNumberCompact(chargedCost)} ({payerSource}).";
                }
                else
                {
                    result = paymentError;
                }
                await botClient.SendTextMessageAsync(userId, result, cancellationToken: cancellationToken);
                return;
            }

            var nextField = fieldList[EconomyInputStep[userId]];
            if (IsBuildingResourceSelectionField(nextField.FieldName))
            {
                await ShowBuildingResourceSelect(botClient, userId, nextField.FieldName, 0, cancellationToken);
                return;
            }
            await SendModeFieldPromptAsync(
                botClient,
                userId,
                EconomyInputMode.AddBuilding,
                nextField.FieldName,
                nextField.Prompt,
                cancellationToken);
        }

        private async Task CleanupHelperMessagesAsync(ITelegramBotClient botClient, long userId, CancellationToken cancellationToken)
        {
            await DeleteMessagesIfAny(botClient, userId, EconomyHelperMessages, cancellationToken);
            await DeleteMessagesIfAny(botClient, userId, EconomyUserMessages, cancellationToken);
        }

        private void TrackUserMessage(long userId, int messageId)
        {
            if (!EconomyUserMessages.TryGetValue(userId, out var list))
            {
                list = new List<int>();
                EconomyUserMessages[userId] = list;
            }
            list.Add(messageId);
        }

        private async Task DeleteMessagesIfAny(ITelegramBotClient botClient, long userId, Dictionary<long, List<int>> store, CancellationToken cancellationToken)
        {
            if (!store.TryGetValue(userId, out var list) || list.Count == 0)
                return;

            foreach (var messageId in list)
            {
                try
                {
                    await botClient.DeleteMessageAsync(userId, messageId, cancellationToken: cancellationToken);
                }
                catch (ApiRequestException ex)
                {
                    if (ex.Message != null &&
                        (ex.Message.Contains("message to delete not found", StringComparison.OrdinalIgnoreCase) ||
                         ex.Message.Contains("message can't be deleted", StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }
                    _logger.LogWarning(ex, "StateAdministration DeleteMessagesIfAny failed. UserId={UserId} MessageId={MessageId}", userId, messageId);
                }
            }

            store.Remove(userId);
        }

        private static bool TryAssignBuildingField(EconomyBuildingRecord building, string fieldName, FieldKind kind, string input, out string error)
        {
            error = "";
            switch (fieldName)
            {
                case "ResourceType":
                    building.ResourceType = (string.Equals(input, "-", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(input))
                        ? null
                        : input.Trim();
                    return true;
                case "InputResourceType":
                    building.InputResourceType = (string.Equals(input, "-", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(input))
                        ? null
                        : input.Trim();
                    return true;
                case "InputAmount":
                    if (!decimal.TryParse(input.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var inputAmount))
                    {
                        error = "❌ Вкажіть число (з крапкою або комою).";
                        return false;
                    }
                    if (inputAmount < 0m)
                    {
                        error = "❌ Споживання ресурсу не може бути від'ємним.";
                        return false;
                    }
                    building.InputAmount = inputAmount;
                    return true;
                case "Category":
                    building.Category = NormalizeCategory(input);
                    if (string.IsNullOrWhiteSpace(building.Category))
                    {
                        error = "❌ Невірна категорія. Доступні: житло, виробництво, адміністрація, наука, військова, культура, інше.";
                        return false;
                    }
                    return true;
            }

            return TryAssignCommon(building, fieldName, kind, input, out error);
        }

        private static bool TryAssignFestivalField(EconomyFestivalRecord festival, string fieldName, FieldKind kind, string input, out string error)
        {
            return TryAssignCommon(festival, fieldName, kind, input, out error);
        }

        private static bool TryAssignByKind(object target, string fieldName, FieldKind kind, string input, out string error)
        {
            error = string.Empty;

            if (target is EconomyBuildingRecord building)
                return TryAssignBuildingField(building, fieldName, kind, input, out error);

            if (target is EconomyFestivalRecord festival)
                return TryAssignFestivalField(festival, fieldName, kind, input, out error);

            if (target is EconomyTradeAgreementRecord trade)
                return TryAssignTradeField(trade, fieldName, kind, input, out error);

            if (target is EconomyCompanyRecord company)
            {
                if (fieldName == "OwnerName" && (string.IsNullOrWhiteSpace(input) || input.Trim() == "-"))
                {
                    company.OwnerName = null;
                    return true;
                }

                if (fieldName == "InputResourceType")
                {
                    company.InputResourceType = (string.IsNullOrWhiteSpace(input) || input.Trim() == "-")
                        ? null
                        : input.Trim();
                    return true;
                }

                if (fieldName == "InputAmount")
                {
                    if (!decimal.TryParse(input.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var inputAmount))
                    {
                        error = "❌ Вкажіть число (з крапкою або комою).";
                        return false;
                    }

                    if (inputAmount < 0m)
                    {
                        error = "❌ Споживання ресурсу не може бути від'ємним.";
                        return false;
                    }

                    company.InputAmount = inputAmount;
                    return true;
                }

                if (fieldName == "ProductionType")
                {
                    company.ProductionType = NormalizeProductionType(input);
                    return true;
                }

                if (fieldName == "TaxRate")
                {
                    if (!decimal.TryParse(input.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var taxRate))
                    {
                        error = "❌ Вкажіть число 0..0.95.";
                        return false;
                    }

                    company.TaxRate = ClampDecimal(taxRate, 0m, 0.95m);
                    return true;
                }

                if (fieldName == "MilitaryProductionShare")
                {
                    if (!float.TryParse(input.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var share))
                    {
                        error = "❌ Вкажіть число 0..100.";
                        return false;
                    }

                    company.MilitaryProductionShare = EconomyCalculator.Clamp(share, 0f, 100f);
                    return true;
                }
            }

            if (target is MilitaryUnitRecord unit)
            {
                if (fieldName == "CommanderName" && (string.IsNullOrWhiteSpace(input) || input.Trim() == "-"))
                {
                    unit.CommanderName = null;
                    return true;
                }

                if (fieldName == "UnitName")
                {
                    unit.UnitName = string.IsNullOrWhiteSpace(input) ? "Частина" : input.Trim();
                    return true;
                }

                if (fieldName == "UnitLevel")
                {
                    unit.UnitLevel = NormalizeUnitLevelKey(input);
                    return true;
                }

                if (fieldName == "UnitRole")
                {
                    unit.UnitRole = NormalizeUnitRole(input);
                    return true;
                }

                if (fieldName == "UnitType")
                {
                    unit.UnitType = NormalizeUnitType(input);
                    return true;
                }

                if (fieldName == "EquipmentTier")
                {
                    unit.EquipmentTier = NormalizeEquipmentTier(input);
                    return true;
                }

                if (fieldName == "Race")
                {
                    unit.Race = NormalizeUnitRace(input);
                    return true;
                }

                if (fieldName == "Status")
                {
                    unit.Status = NormalizeUnitStatus(input);
                    return true;
                }

                if (fieldName == "FormationProgress")
                {
                    if (!float.TryParse(input.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var progress))
                    {
                        error = "❌ Вкажіть число 0..100.";
                        return false;
                    }

                    if (progress > 1f)
                        progress /= 100f;
                    unit.FormationProgress = EconomyCalculator.Clamp(progress, 0f, 1f);
                    return true;
                }
            }

            if (target is VassalMilitaryObligationRecord vassal && fieldName == "SubjectName")
            {
                if (string.IsNullOrWhiteSpace(input))
                {
                    error = "❌ Назва васала не може бути порожньою.";
                    return false;
                }

                vassal.SubjectName = input.Trim();
                return true;
            }

            return TryAssignCommon(target, fieldName, kind, input, out error);
        }

        private static bool TryAssignCommon(object target, string fieldName, FieldKind kind, string input, out string error)
        {
            error = "";
            var prop = target.GetType().GetProperty(fieldName);
            if (prop == null)
            {
                error = "❌ Невірне поле.";
                return false;
            }

            switch (kind)
            {
                case FieldKind.String:
                    prop.SetValue(target, input.Trim());
                    return true;
                case FieldKind.Int:
                    if (!int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                    {
                        error = "❌ Вкажіть ціле число.";
                        return false;
                    }
                    prop.SetValue(target, i);
                    return true;
                case FieldKind.Long:
                    if (!long.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
                    {
                        error = "❌ Вкажіть ціле число (long).";
                        return false;
                    }
                    prop.SetValue(target, l);
                    return true;
                case FieldKind.Float:
                    if (!float.TryParse(input.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
                    {
                        error = "❌ Вкажіть число (з крапкою або комою).";
                        return false;
                    }
                    prop.SetValue(target, f);
                    return true;
                case FieldKind.Decimal:
                    if (!decimal.TryParse(input.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var d))
                    {
                        error = "❌ Вкажіть число (з крапкою або комою).";
                        return false;
                    }
                    prop.SetValue(target, d);
                    return true;
                case FieldKind.Bool:
                    if (TryParseBool(input, out var b))
                    {
                        prop.SetValue(target, b);
                        return true;
                    }
                    error = "❌ Вкажіть так/ні або 1/0.";
                    return false;
                default:
                    error = "❌ Невідомий тип поля.";
                    return false;
            }
        }

        private static string NormalizeCategory(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            string s = input.Trim().ToLowerInvariant();
            if (s.StartsWith("house") || s.StartsWith("жит")) return "Housing";
            if (s.StartsWith("prod") || s.StartsWith("вироб")) return "Production";
            if (s.StartsWith("admin") || s.StartsWith("адмін") || s.StartsWith("адмiн")) return "Administration";
            if (s.StartsWith("sci") || s.StartsWith("наук")) return "Science";
            if (s.StartsWith("mil") || s.StartsWith("військ") || s.StartsWith("вiйськ")) return "Military";
            if (s.StartsWith("cult") || s.StartsWith("культ")) return "Culture";
            if (s.StartsWith("other") || s.StartsWith("інш") || s.StartsWith("iнш")) return "Other";
            return string.Empty;
        }

        private static bool TryParseBool(string input, out bool value)
        {
            var s = (input ?? string.Empty).Trim().ToLowerInvariant();
            if (s == "1" || s == "true" || s == "так" || s == "yes")
            {
                value = true;
                return true;
            }
            if (s == "0" || s == "false" || s == "ні" || s == "no")
            {
                value = false;
                return true;
            }
            value = false;
            return false;
        }

        private static decimal ClampDecimal(decimal value, decimal min, decimal max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        private async Task HandleEditInput(ITelegramBotClient botClient, long userId, EconomyEditContext edit, string rawInput, CancellationToken cancellationToken)
        {
            string input = (rawInput ?? string.Empty).Trim();
            if (string.Equals(input, "скасувати", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(input, "відміна", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(input, "/cancel", StringComparison.OrdinalIgnoreCase))
            {
                EconomyEditState.Remove(userId);
                await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
                await botClient.SendTextMessageAsync(userId, "Дію скасовано.", cancellationToken: cancellationToken);
                return;
            }

            if (edit.Kind == "resource" && edit.FieldName == "ResourceType" &&
                (string.IsNullOrWhiteSpace(input) || input == "-"))
            {
                await botClient.SendTextMessageAsync(userId, "❌ Назва ресурсу не може бути порожньою.", cancellationToken: cancellationToken);
                return;
            }

            object? value;
            SqlDbType sqlType;
            string error;
            if (edit.Kind == "resource" && string.Equals(edit.FieldName, "ParentResourceType", StringComparison.Ordinal))
            {
                if (!TryResolveParentResourceInput(input, edit.Key, out var parentResourceType, out error))
                {
                    await botClient.SendTextMessageAsync(userId, error, cancellationToken: cancellationToken);
                    return;
                }

                value = (object?)parentResourceType ?? DBNull.Value;
                sqlType = SqlDbType.NVarChar;
            }
            else
            {
                if (!TryParseValue(edit.FieldKind, edit.FieldName, input, out value, out sqlType, out error))
                {
                    await botClient.SendTextMessageAsync(userId, error, cancellationToken: cancellationToken);
                    return;
                }
            }

            var selectedCountryId = GetSelectedCountryIdForUser(userId);
            if (!selectedCountryId.HasValue)
            {
                EconomyEditState.Remove(userId);
                await botClient.SendTextMessageAsync(userId, "Не знайдено країни.", cancellationToken: cancellationToken);
                return;
            }

            object dbValue = value ?? DBNull.Value;
            bool ok = edit.Kind switch
            {
                "building" => EconomyBuildingsData.UpdateBuildingField(edit.Id, selectedCountryId.Value, edit.FieldName, dbValue, sqlType),
                "company" => EconomyCompaniesData.UpdateCompanyField(edit.Id, selectedCountryId.Value, edit.FieldName, dbValue, sqlType),
                "festival" => EconomyFestivalsData.UpdateFestivalField(edit.Id, selectedCountryId.Value, edit.FieldName, dbValue, sqlType),
                "trade" => EconomyTradeAgreementsData.UpdateAgreementField(edit.Id, selectedCountryId.Value, edit.FieldName, dbValue, sqlType),
                "militaryunit" => MilitaryUnitsData.UpdateUnitField(edit.Id, selectedCountryId.Value, edit.FieldName, dbValue, sqlType),
                "vassal" => VassalMilitaryObligationsData.UpdateField(edit.Id, selectedCountryId.Value, edit.FieldName, dbValue, sqlType),
                "resource" => EconomyResourcesData.UpdateResourceField(edit.Key ?? string.Empty, edit.FieldName, dbValue, sqlType),
                _ => false
            };

            bool mirrorSyncAttempted = false;
            bool mirrorSyncOk = true;
            if (ok && string.Equals(edit.Kind, "trade", StringComparison.OrdinalIgnoreCase))
            {
                mirrorSyncOk = TrySyncMirroredTradeAgreementField(
                    edit.Id,
                    selectedCountryId.Value,
                    edit.FieldName,
                    dbValue,
                    sqlType,
                    out mirrorSyncAttempted);

                if (mirrorSyncAttempted && !mirrorSyncOk)
                {
                    _logger.LogWarning(
                        "Trade mirror sync failed: agreement={AgreementId} country={CountryId} field={Field}",
                        edit.Id,
                        selectedCountryId.Value,
                        edit.FieldName);
                }
            }

            EconomyEditState.Remove(userId);
            await CleanupHelperMessagesAsync(botClient, userId, cancellationToken);
            string msg = ok
                ? (mirrorSyncAttempted
                    ? (mirrorSyncOk
                        ? "✅ Зміни збережено та синхронізовано з країною-партнером."
                        : "⚠ Зміни збережено, але синхронізувати з країною-партнером не вдалося.")
                    : "✅ Зміни збережено.")
                : "❌ Не вдалося оновити.";
            await botClient.SendTextMessageAsync(userId, msg, cancellationToken: cancellationToken);
        }

        private static bool TryParseValue(FieldKind kind, string fieldName, string input, out object? value, out SqlDbType sqlType, out string error)
        {
            error = "";
            value = null;
            sqlType = SqlDbType.NVarChar;

            if ((fieldName == "ResourceType" || fieldName == "InputResourceType" || fieldName == "ParentResourceType") &&
                (string.Equals(input, "-", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(input)))
            {
                value = DBNull.Value;
                sqlType = SqlDbType.NVarChar;
                return true;
            }

            if ((fieldName == "OwnerName" || fieldName == "CommanderName") &&
                (string.Equals(input, "-", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(input)))
            {
                value = DBNull.Value;
                sqlType = SqlDbType.NVarChar;
                return true;
            }

            if (fieldName == "TaxRate")
            {
                if (!decimal.TryParse(input.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var taxRate))
                {
                    error = "❌ Вкажіть число 0..0.95";
                    return false;
                }
                taxRate = ClampDecimal(taxRate, 0m, 0.95m);
                value = taxRate;
                sqlType = SqlDbType.Decimal;
                return true;
            }

            if (fieldName == "InputAmount")
            {
                if (!decimal.TryParse(input.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
                {
                    error = "❌ Вкажіть число (з крапкою або комою).";
                    return false;
                }
                if (amount < 0m)
                {
                    error = "❌ Споживання ресурсу не може бути від'ємним.";
                    return false;
                }
                value = amount;
                sqlType = SqlDbType.Decimal;
                return true;
            }

            if (fieldName == "PeriodDays")
            {
                if (!int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out var days) || days <= 0)
                {
                    error = "❌ Вкажіть час доставки у днях (>0).";
                    return false;
                }

                value = days;
                sqlType = SqlDbType.Int;
                return true;
            }

            if (fieldName == "DeliveryMode")
            {
                value = NormalizeTradeDeliveryMode(input);
                sqlType = SqlDbType.NVarChar;
                return true;
            }

            if (fieldName == "DemandFactor")
            {
                string raw = input.Replace('%', ' ').Trim();
                if (!float.TryParse(raw.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var factor))
                {
                    error = "❌ Вкажіть коефіцієнт попиту (наприклад 0, 1 або 1.25).";
                    return false;
                }

                if (factor < 0f)
                {
                    error = "❌ Коефіцієнт попиту не може бути від'ємним.";
                    return false;
                }

                value = factor;
                sqlType = SqlDbType.Real;
                return true;
            }

            if (fieldName == "UnitLevel")
            {
                value = NormalizeUnitLevelKey(input);
                sqlType = SqlDbType.NVarChar;
                return true;
            }

            if (fieldName == "UnitName")
            {
                value = string.IsNullOrWhiteSpace(input) ? "Частина" : input.Trim();
                sqlType = SqlDbType.NVarChar;
                return true;
            }

            if (fieldName == "UnitRole")
            {
                value = NormalizeUnitRole(input);
                sqlType = SqlDbType.NVarChar;
                return true;
            }

            if (fieldName == "UnitType")
            {
                value = NormalizeUnitType(input);
                sqlType = SqlDbType.NVarChar;
                return true;
            }

            if (fieldName == "EquipmentTier")
            {
                value = NormalizeEquipmentTier(input);
                sqlType = SqlDbType.NVarChar;
                return true;
            }

            if (fieldName == "Race")
            {
                value = NormalizeUnitRace(input);
                sqlType = SqlDbType.NVarChar;
                return true;
            }

            if (fieldName == "Status")
            {
                value = NormalizeUnitStatus(input);
                sqlType = SqlDbType.NVarChar;
                return true;
            }

            if (fieldName == "EquipmentQuality" || fieldName == "BattleExperience" || fieldName == "Fatigue")
            {
                if (!float.TryParse(input.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var metric))
                {
                    error = "❌ Вкажіть число 0..100.";
                    return false;
                }
                value = EconomyCalculator.Clamp(metric, 0f, 100f);
                sqlType = SqlDbType.Real;
                return true;
            }

            if (fieldName == "ProductionType")
            {
                value = NormalizeProductionType(input);
                sqlType = SqlDbType.NVarChar;
                return true;
            }

            if (fieldName == "SubjectName")
            {
                if (string.IsNullOrWhiteSpace(input))
                {
                    error = "❌ Назва васала не може бути порожньою.";
                    return false;
                }
                value = input.Trim();
                sqlType = SqlDbType.NVarChar;
                return true;
            }

            if (fieldName == "MilitaryProductionShare")
            {
                if (!float.TryParse(input.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var share))
                {
                    error = "❌ Вкажіть число 0..100.";
                    return false;
                }
                value = EconomyCalculator.Clamp(share, 0f, 100f);
                sqlType = SqlDbType.Real;
                return true;
            }

            if (fieldName == "Category")
            {
                string cat = NormalizeCategory(input);
                if (string.IsNullOrWhiteSpace(cat))
                {
                    error = "❌ Невірна категорія. Доступні: житло, виробництво, адміністрація, наука, військова, культура, інше.";
                    return false;
                }
                value = cat;
                sqlType = SqlDbType.NVarChar;
                return true;
            }

            switch (kind)
            {
                case FieldKind.String:
                    value = input.Trim();
                    sqlType = SqlDbType.NVarChar;
                    return true;
                case FieldKind.Int:
                    if (!int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                    {
                        error = "❌ Вкажіть ціле число.";
                        return false;
                    }
                    value = i;
                    sqlType = SqlDbType.Int;
                    return true;
                case FieldKind.Long:
                    if (!long.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
                    {
                        error = "❌ Вкажіть ціле число (long).";
                        return false;
                    }
                    value = l;
                    sqlType = SqlDbType.BigInt;
                    return true;
                case FieldKind.Float:
                    if (!float.TryParse(input.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
                    {
                        error = "❌ Вкажіть число (з крапкою або комою).";
                        return false;
                    }
                    value = f;
                    sqlType = SqlDbType.Real;
                    return true;
                case FieldKind.Decimal:
                    if (!decimal.TryParse(input.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var d))
                    {
                        error = "❌ Вкажіть число (з крапкою або комою).";
                        return false;
                    }
                    value = d;
                    sqlType = SqlDbType.Decimal;
                    return true;
                case FieldKind.Bool:
                    if (TryParseBool(input, out var b))
                    {
                        value = b;
                        sqlType = SqlDbType.Bit;
                        return true;
                    }
                    error = "❌ Вкажіть так/ні або 1/0.";
                    return false;
                default:
                    error = "❌ Невідомий тип поля.";
                    return false;
            }
        }

        private sealed class TemplateLevelSnapshot
        {
            public int Level { get; set; }
            public int JobsProvided { get; set; }
            public decimal UpkeepCost { get; set; }
            public decimal BaseProduction { get; set; }
            public decimal BuildCost { get; set; }
            public int BuildTurns { get; set; }
            public float StabilityBonus { get; set; }
            public float CorruptionReduction { get; set; }
            public float ResearchBonus { get; set; }
            public float PopulationGrowthBonus { get; set; }
            public float FestivalBoost { get; set; }
            public int ArmyBonus { get; set; }
            public string? ResourceType { get; set; }
        }

        private static string BuildTemplatePreviewText(EconomyBuildingRecord template, BuildingTemplateDefinition definition, float inflationPct)
        {
            var levels = BuildTemplateLevelSnapshots(template, MaxBuildingLevel);
            var l1 = levels[0];
            decimal l1Charge = GetBuildingCreationCost(template, inflationPct);

            var sb = new StringBuilder();
            sb.AppendLine($"{definition.MenuTitle}");
            sb.AppendLine($"🏗 {template.Type}");
            sb.AppendLine($"📜 {definition.FantasyNote}");
            sb.AppendLine();
            sb.AppendLine($"🏷 Категорія: {FormatBuildingCategoryUk(template.Category)}");
            sb.AppendLine($"🏛 Власність: {(template.StateOwned ? "Державна" : "Приватна")}");
            sb.AppendLine($"🪙 Ціна L1 (база): {FmtNumber(l1.BuildCost)} монет");
            sb.AppendLine($"💰 Списання з казни за створення L1: {FmtNumber(l1Charge)} монет");
            sb.AppendLine($"⏱️ Час L1: {FmtNumber(l1.BuildTurns)} дн.");
            sb.AppendLine();
            sb.AppendLine("🎯 Ефект L1:");
            AppendTemplateLevelEffectLines(sb, l1);
            sb.AppendLine();
            sb.Append("Натисни \"👁 Предосмотр\", щоб подивитися рівні L2..L10, або \"✅ Використати шаблон\", щоб створити будівлю.");
            return sb.ToString();
        }

        private static string BuildTemplateLevelPreviewText(
            EconomyBuildingRecord template,
            BuildingTemplateDefinition definition,
            TemplateLevelSnapshot level,
            decimal totalCreationCharge,
            int totalCreationTurns)
        {
            var sb = new StringBuilder();
            int safeLevel = Math.Max(1, level.Level);

            sb.AppendLine($"{definition.MenuTitle}");
            sb.AppendLine($"🏗 {template.Type}");
            sb.AppendLine($"📜 {definition.FantasyNote}");
            sb.AppendLine();
            sb.AppendLine($"🏷 Категорія: {FormatBuildingCategoryUk(template.Category)}");
            sb.AppendLine($"🏛 Власність: {(template.StateOwned ? "Державна" : "Приватна")}");
            sb.AppendLine($"🪙 Ціна L{FmtNumber(safeLevel)} (база): {FmtNumber(level.BuildCost)} монет");
            sb.AppendLine($"⏱️ Час L{FmtNumber(safeLevel)} (база): {FmtNumber(level.BuildTurns)} дн.");
            sb.AppendLine($"💰 Списання за створення L{FmtNumber(safeLevel)} (L1→L{FmtNumber(safeLevel)}): {FmtNumber(totalCreationCharge)} монет");
            sb.AppendLine($"🧱 Час створення L{FmtNumber(safeLevel)} (L1→L{FmtNumber(safeLevel)}): {FmtNumber(totalCreationTurns)} дн.");
            sb.AppendLine();
            sb.AppendLine($"🎯 Ефект L{FmtNumber(safeLevel)}:");
            AppendTemplateLevelEffectLines(sb, level);
            return sb.ToString();
        }

        private static void AppendTemplateLevelEffectLines(StringBuilder sb, TemplateLevelSnapshot level)
        {
            sb.AppendLine($"• Робочі місця: {FmtNumber(level.JobsProvided)}");
            sb.AppendLine($"• Утримання: {FmtNumberCompact(level.UpkeepCost)} / день");
            sb.AppendLine("• Виробництво (ресурси/день):");
            foreach (var output in BuildTemplateResourceOutputs(level))
                sb.AppendLine($"• {FormatResourceTypeUk(output.ResourceType)}: {FmtNumberCompact(output.Amount)}");
            sb.AppendLine($"• Ріст населення: {FmtNumberCompact(level.PopulationGrowthBonus)}");
            sb.AppendLine($"• Підсилення фестивалів: {FmtNumberCompact(level.FestivalBoost)}");
            sb.AppendLine($"• Бонуси: {BuildTemplateBonusSummary(level)}");
        }

        private static List<(string ResourceType, decimal Amount)> BuildTemplateResourceOutputs(TemplateLevelSnapshot level)
        {
            var ordered = new[] { "Food", "Clothing", "Housing", "Tools", "Luxury", "Goods", "Materials" };
            var canonical = ordered.ToDictionary(r => r, _ => 0m, StringComparer.OrdinalIgnoreCase);
            var extra = new List<(string ResourceType, decimal Amount)>();

            if (level.BaseProduction > 0m && !string.IsNullOrWhiteSpace(level.ResourceType))
            {
                string normalized = NormalizeTemplateOutputResource(level.ResourceType);
                if (canonical.ContainsKey(normalized))
                {
                    canonical[normalized] += level.BaseProduction;
                }
                else
                {
                    extra.Add((level.ResourceType.Trim(), level.BaseProduction));
                }
            }

            var result = ordered
                .Select(r => (ResourceType: r, Amount: canonical[r]))
                .ToList();
            result.AddRange(extra);
            return result;
        }

        private static string NormalizeTemplateOutputResource(string? resourceType)
        {
            if (string.IsNullOrWhiteSpace(resourceType))
                return string.Empty;

            return NormalizeResourceKey(resourceType) switch
            {
                "food" => "Food",
                "clothing" => "Clothing",
                "housing" => "Housing",
                "tools" => "Tools",
                "luxury" => "Luxury",
                "goods" => "Goods",
                "material" or "materials" => "Materials",
                _ => resourceType.Trim()
            };
        }

        private static string BuildTemplateBonusSummary(TemplateLevelSnapshot level)
        {
            var parts = new List<string>();
            if (Math.Abs(level.StabilityBonus) > 0.0001f)
                parts.Add($"стабільність +{FmtNumberCompact(level.StabilityBonus)}");
            if (Math.Abs(level.CorruptionReduction) > 0.0001f)
                parts.Add($"антикорупція +{FmtNumberCompact(level.CorruptionReduction)}");
            if (Math.Abs(level.ResearchBonus) > 0.0001f)
                parts.Add($"наука +{FmtNumberCompact(level.ResearchBonus)}");
            if (Math.Abs(level.PopulationGrowthBonus) > 0.0001f)
                parts.Add($"ріст населення +{FmtNumberCompact(level.PopulationGrowthBonus)}");
            if (Math.Abs(level.FestivalBoost) > 0.0001f)
                parts.Add($"фестивалі +{FmtNumberCompact(level.FestivalBoost)}");
            if (level.ArmyBonus != 0)
                parts.Add($"армія +{FmtNumber(level.ArmyBonus)}");

            return parts.Count == 0 ? "немає" : string.Join(", ", parts);
        }

        private static string BuildTemplateLevelDelta(TemplateLevelSnapshot from, TemplateLevelSnapshot to)
        {
            string FmtSignedInt(int value) => value > 0 ? $"+{FmtNumber(value)}" : value < 0 ? $"-{FmtNumber(Math.Abs(value))}" : "0";
            string FmtSignedDecimal(decimal value) => value > 0m ? $"+{FmtNumberCompact(value)}" : value < 0m ? $"-{FmtNumberCompact(Math.Abs(value))}" : "0";
            string FmtSignedFloat(float value) => value > 0f ? $"+{FmtNumberCompact(value)}" : value < 0f ? $"-{FmtNumberCompact(Math.Abs(value))}" : "0";

            var parts = new List<string>
            {
                $"місця {FmtSignedInt(to.JobsProvided - from.JobsProvided)}",
                $"утримання {FmtSignedDecimal(to.UpkeepCost - from.UpkeepCost)}",
                $"ціна {FmtSignedDecimal(to.BuildCost - from.BuildCost)}",
                $"час {FmtSignedInt(to.BuildTurns - from.BuildTurns)} дн."
            };

            if (!string.IsNullOrWhiteSpace(to.ResourceType) && (from.BaseProduction > 0m || to.BaseProduction > 0m))
                parts.Add($"виробн {FmtSignedDecimal(to.BaseProduction - from.BaseProduction)}");
            if (Math.Abs(from.StabilityBonus) > 0.0001f || Math.Abs(to.StabilityBonus) > 0.0001f)
                parts.Add($"стабіл. {FmtSignedFloat(to.StabilityBonus - from.StabilityBonus)}");
            if (Math.Abs(from.CorruptionReduction) > 0.0001f || Math.Abs(to.CorruptionReduction) > 0.0001f)
                parts.Add($"антикор. {FmtSignedFloat(to.CorruptionReduction - from.CorruptionReduction)}");
            if (Math.Abs(from.ResearchBonus) > 0.0001f || Math.Abs(to.ResearchBonus) > 0.0001f)
                parts.Add($"наука {FmtSignedFloat(to.ResearchBonus - from.ResearchBonus)}");
            if (Math.Abs(from.PopulationGrowthBonus) > 0.0001f || Math.Abs(to.PopulationGrowthBonus) > 0.0001f)
                parts.Add($"ріст {FmtSignedFloat(to.PopulationGrowthBonus - from.PopulationGrowthBonus)}");
            if (Math.Abs(from.FestivalBoost) > 0.0001f || Math.Abs(to.FestivalBoost) > 0.0001f)
                parts.Add($"фести {FmtSignedFloat(to.FestivalBoost - from.FestivalBoost)}");
            if (from.ArmyBonus != 0 || to.ArmyBonus != 0)
                parts.Add($"армія {FmtSignedInt(to.ArmyBonus - from.ArmyBonus)}");

            return $"L{from.Level} → L{to.Level}: {string.Join(" | ", parts)}";
        }

        private static List<TemplateLevelSnapshot> BuildTemplateLevelSnapshots(EconomyBuildingRecord template, int count)
        {
            var current = CloneBuildingForSimulation(template);
            current.Level = Math.Max(1, current.Level);
            current.JobsProvided = Math.Max(0, current.JobsProvided);
            current.UpkeepCost = Math.Max(0m, current.UpkeepCost);
            current.BaseProduction = Math.Max(0m, current.BaseProduction);
            current.BuildCost = Math.Max(1m, current.BuildCost);
            current.BuildTurns = Math.Max(1, current.BuildTurns);
            current.StabilityBonus = Math.Max(0f, current.StabilityBonus);
            current.CorruptionReduction = Math.Max(0f, current.CorruptionReduction);
            current.ResearchBonus = Math.Max(0f, current.ResearchBonus);
            current.PopulationGrowthBonus = Math.Max(0f, current.PopulationGrowthBonus);
            current.FestivalBoost = Math.Max(0f, current.FestivalBoost);
            current.ArmyBonus = Math.Max(0, current.ArmyBonus);

            var snapshots = new List<TemplateLevelSnapshot>();
            for (int i = 0; i < Math.Max(1, count); i++)
            {
                snapshots.Add(new TemplateLevelSnapshot
                {
                    Level = current.Level,
                    JobsProvided = current.JobsProvided,
                    UpkeepCost = current.UpkeepCost,
                    BaseProduction = current.BaseProduction,
                    BuildCost = current.BuildCost,
                    BuildTurns = current.BuildTurns,
                    StabilityBonus = current.StabilityBonus,
                    CorruptionReduction = current.CorruptionReduction,
                    ResearchBonus = current.ResearchBonus,
                    PopulationGrowthBonus = current.PopulationGrowthBonus,
                    FestivalBoost = current.FestivalBoost,
                    ArmyBonus = current.ArmyBonus,
                    ResourceType = current.ResourceType
                });

                if (i < count - 1)
                    TryApplyBuildingUpgradeProgression(current);
            }

            return snapshots;
        }

        private static string? ExtractTemplateKey(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                return null;

            string raw = data.Trim();
            if (raw.StartsWith(BuildingTemplateUsePrefix, StringComparison.OrdinalIgnoreCase))
            {
                string value = raw.Substring(BuildingTemplateUsePrefix.Length).Trim().ToLowerInvariant();
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }

            if (raw.StartsWith(BuildingTemplatePrefix, StringComparison.OrdinalIgnoreCase))
            {
                string value = raw.Substring(BuildingTemplatePrefix.Length).Trim().ToLowerInvariant();
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }

            return raw.ToLowerInvariant();
        }

        private static bool TryParseTemplateLevelPreviewData(string data, out string key, out int level)
        {
            key = string.Empty;
            level = 1;

            if (string.IsNullOrWhiteSpace(data))
                return false;

            if (!data.StartsWith(BuildingTemplateLevelPreviewPrefix, StringComparison.OrdinalIgnoreCase))
                return false;

            string payload = data.Substring(BuildingTemplateLevelPreviewPrefix.Length).Trim();
            if (string.IsNullOrWhiteSpace(payload))
                return false;

            string[] parts = payload.Split('|', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return false;

            key = parts[0].Trim().ToLowerInvariant();
            if (parts.Length > 1 && int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedLevel))
                level = Math.Max(1, parsedLevel);

            return !string.IsNullOrWhiteSpace(key);
        }

        private static void ApplyTemplateConstructionDefaults(EconomyBuildingRecord building, string key)
        {
            if (TryGetBuildingTemplateDefinition(key, out var definition))
            {
                building.BuildCost = definition.BuildCost;
                building.BuildTurns = definition.BuildTurns;
                return;
            }

            building.BuildCost = 120m;
            building.BuildTurns = 3;
        }

        private static EconomyBuildingRecord? BuildTemplateFromCallback(int countryId, string data)
        {
            string? key = ExtractTemplateKey(data);
            if (!TryGetBuildingTemplateDefinition(key, out var definition))
                return null;

            var template = CreateBuildingTemplate(
                countryId,
                definition.Type,
                definition.Category,
                definition.JobsProvided,
                definition.UpkeepCost,
                definition.BaseProduction,
                definition.ResourceType,
                definition.StateOwned,
                definition.StabilityBonus,
                definition.CorruptionReduction,
                definition.ResearchBonus,
                definition.ArmyBonus);

            ApplyTemplateConstructionDefaults(template, definition.Key);
            return template;
        }

        private static EconomyBuildingRecord CreateBuildingTemplate(
            int countryId,
            string type,
            string category,
            int jobsProvided,
            decimal upkeepCost,
            decimal baseProduction,
            string? resourceType,
            bool stateOwned,
            float stabilityBonus = 0f,
            float corruptionReduction = 0f,
            float researchBonus = 0f,
            int armyBonus = 0)
        {
            return new EconomyBuildingRecord
            {
                CountryId = countryId,
                Type = type,
                Category = category,
                Level = 1,
                JobsProvided = jobsProvided,
                UpkeepCost = upkeepCost,
                BaseProduction = baseProduction,
                ResourceType = resourceType,
                InputResourceType = null,
                InputAmount = 0m,
                StateOwned = stateOwned,
                StabilityBonus = stabilityBonus,
                CorruptionReduction = corruptionReduction,
                ResearchBonus = researchBonus,
                ArmyBonus = armyBonus
            };
        }

        private async Task StartEditField(ITelegramBotClient botClient, long userId, string data, string kind, CancellationToken cancellationToken)
        {
            var parts = data.Split('_');
            if (parts.Length < 5)
            {
                await botClient.SendTextMessageAsync(userId, "❌ Невірний формат.", cancellationToken: cancellationToken);
                return;
            }

            if (!int.TryParse(parts[^2], out int id))
            {
                await botClient.SendTextMessageAsync(userId, "❌ Невірний Id.", cancellationToken: cancellationToken);
                return;
            }

            string fieldName = parts[^1];
            var fieldKind = ResolveFieldKind(kind, fieldName);
            if (fieldKind == null)
            {
                await botClient.SendTextMessageAsync(userId, "❌ Невірне поле.", cancellationToken: cancellationToken);
                return;
            }

            EconomyEditState[userId] = new EconomyEditContext
            {
                Kind = kind,
                Id = id,
                FieldName = fieldName,
                FieldKind = fieldKind.Value
            };

            if (TryBuildInlineSelectionKeyboard(userId, kind, fieldName, out string inlinePrompt, out var inlineKeyboard))
            {
                await SendHelperMessageAsync(botClient, userId, inlinePrompt, inlineKeyboard, cancellationToken);
                return;
            }

            string prompt = ResolveFieldPromptForKind(kind, fieldName);
            await SendHelperMessageAsync(botClient, userId, prompt, cancellationToken);
        }

        private async Task StartEditResourceField(ITelegramBotClient botClient, long userId, string resourceType, string fieldName, CancellationToken cancellationToken)
        {
            var fieldKind = ResolveFieldKind("resource", fieldName);
            if (fieldKind == null)
            {
                await botClient.SendTextMessageAsync(userId, "❌ Невірне поле.", cancellationToken: cancellationToken);
                return;
            }

            EconomyEditState[userId] = new EconomyEditContext
            {
                Kind = "resource",
                Key = resourceType,
                FieldName = fieldName,
                FieldKind = fieldKind.Value
            };

            if (string.Equals(fieldName, "ParentResourceType", StringComparison.Ordinal))
            {
                await ShowResourceParentSelect(botClient, userId, resourceType, 0, cancellationToken);
                return;
            }

            if (TryBuildInlineSelectionKeyboard(userId, "resource", fieldName, out string inlinePrompt, out var inlineKeyboard))
            {
                await SendHelperMessageAsync(botClient, userId, inlinePrompt, inlineKeyboard, cancellationToken);
                return;
            }

            string prompt = ResolveFieldPromptForKind("resource", fieldName);
            await SendHelperMessageAsync(botClient, userId, prompt, cancellationToken);
        }

        private static string EncodeResourceKey(string resourceType)
        {
            if (string.IsNullOrWhiteSpace(resourceType))
                return string.Empty;

            string normalized = resourceType.Trim().ToLowerInvariant();
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
            return Convert.ToHexString(hash.AsSpan(0, 5)).ToLowerInvariant();
        }

        private static string DecodeResourceKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string key = value.Trim();
            var resources = EconomyResourcesData.GetAll();

            var direct = resources.FirstOrDefault(r =>
                string.Equals(r.ResourceType, key, StringComparison.OrdinalIgnoreCase));
            if (direct != null)
                return direct.ResourceType;

            var byToken = resources
                .Where(r => string.Equals(EncodeResourceKey(r.ResourceType), key, StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToList();
            if (byToken.Count == 1)
                return byToken[0].ResourceType;

            string legacyDecoded = TryDecodeLegacyResourceKey(key);
            if (!string.IsNullOrWhiteSpace(legacyDecoded))
            {
                var legacyMatch = resources.FirstOrDefault(r =>
                    string.Equals(r.ResourceType, legacyDecoded, StringComparison.OrdinalIgnoreCase));
                if (legacyMatch != null)
                    return legacyMatch.ResourceType;
            }

            string normalized = NormalizeResourceKey(key);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                var normalizedMatches = resources
                    .Where(r => string.Equals(NormalizeResourceKey(r.ResourceType), normalized, StringComparison.Ordinal))
                    .Take(2)
                    .ToList();
                if (normalizedMatches.Count == 1)
                    return normalizedMatches[0].ResourceType;
            }

            return string.Empty;
        }

        private static string TryDecodeLegacyResourceKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
            try
            {
                string s = value.Replace('-', '+').Replace('_', '/');
                switch (s.Length % 4)
                {
                    case 2:
                        s += "==";
                        break;
                    case 3:
                        s += "=";
                        break;
                }

                byte[] bytes = Convert.FromBase64String(s);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string NormalizeResourceKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
            var sb = new StringBuilder(value.Length);
            foreach (char c in value.Trim())
            {
                if (char.IsWhiteSpace(c) || c == '_' || c == '-')
                    continue;
                sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }

        private static bool IsProductionBuilding(EconomyBuildingRecord building)
        {
            return string.Equals(building.Category, "Production", StringComparison.OrdinalIgnoreCase)
                || string.Equals(building.Category, "Housing", StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasResourceOutputBuilding(EconomyBuildingRecord? building)
        {
            return building != null
                && building.BaseProduction > 0m
                && !string.IsNullOrWhiteSpace(building.ResourceType);
        }

        private static bool IsBuildingResourceSelectionField(string? fieldName)
        {
            return string.Equals(fieldName, "ResourceType", StringComparison.Ordinal) ||
                   string.Equals(fieldName, "InputResourceType", StringComparison.Ordinal);
        }

        private static bool IsProductionOnlyBuildingField(string? fieldName)
        {
            return string.Equals(fieldName, "ResourceType", StringComparison.Ordinal) ||
                   string.Equals(fieldName, "InputResourceType", StringComparison.Ordinal) ||
                   string.Equals(fieldName, "InputAmount", StringComparison.Ordinal);
        }

        private sealed class EconomyReportBreakdown
        {
            public decimal WagesTax { get; set; }
            public decimal ProfitTax { get; set; }
            public decimal SocialContributionIncome { get; set; }
            public decimal AdditionalGrossIncomeTax { get; set; }
            public decimal AdditionalNetIncomeTax { get; set; }
            public decimal CitizenGrossIncome { get; set; }
            public decimal CitizenNetIncome { get; set; }
            public decimal MainIncomeTaxRate { get; set; }
            public decimal SocialContributionRate { get; set; }
            public decimal AdditionalGrossIncomeTaxRate { get; set; }
            public decimal AdditionalNetIncomeTaxRate { get; set; }
            public decimal TradeBaseIncome { get; set; }
            public decimal AgreementExportIncome { get; set; }
            public decimal AgreementImportCost { get; set; }
            public decimal AgreementNet { get; set; }
            public decimal IncomeTotal { get; set; }
            public BreakdownResult IncomeBreakdown { get; set; } = new();

            public decimal Upkeep { get; set; }
            public decimal GovSalaries { get; set; }
            public decimal SocialSpending { get; set; }
            public decimal ScienceSpending { get; set; }
            public decimal FestivalCost { get; set; }
            public decimal MilitaryUpkeep { get; set; }
            public decimal InfrastructureSpending { get; set; }
            public decimal StabilizationReserveFunding { get; set; }
            public decimal CorruptionLoss { get; set; }
            public decimal DeputySalaryTotal { get; set; }
            public decimal DeputyTheft { get; set; }
            public decimal ExpensesTotal { get; set; }
            public BreakdownResult ExpensesBreakdown { get; set; } = new();
            public BreakdownResult StabilityBreakdown { get; set; } = new();
            public BreakdownResult UnemploymentBreakdown { get; set; } = new();
            public decimal BudgetInfrastructure { get; set; }
            public decimal BudgetMilitary { get; set; }
            public decimal BudgetScience { get; set; }
            public decimal BudgetSocial { get; set; }

            public decimal NetTotal => IncomeTotal - ExpensesTotal;
        }

        private static EconomyReportBreakdown CalculateEconomyBreakdown(
            int countryId,
            StateDataClass state,
            List<EconomyBuildingRecord> buildings,
            List<EconomyFestivalRecord> festivals,
            List<EconomyTradeAgreementRecord> tradeAgreements,
            Dictionary<string, EconomyResourceRecord> resources,
            decimal agreementExportIncome,
            decimal agreementImportCost)
        {
            _ = festivals;
            _ = tradeAgreements;
            _ = resources;
            _ = agreementExportIncome;
            _ = agreementImportCost;

            var preview = new EconomyService().PreviewTurn(countryId, 1);
            long availableWorkers = state.AvailableWorkers > 0
                ? state.AvailableWorkers
                : Math.Max(0L, state.WorkingAgePopulation - state.ArmySize);
            int totalJobs = buildings.Sum(b => Math.Max(0, b.JobsProvided));

            var incomeBreakdown = new BreakdownResult
            {
                Name = "Доходи",
                BaseValue = preview.Income,
                Total = preview.Income,
                Modifiers = new List<BreakdownModifier>()
            };
            if (preview.WagesTax != 0m)
                incomeBreakdown.Modifiers.Add(new BreakdownModifier { Name = "Податки із зарплат", Value = preview.WagesTax, Source = "Фонд оплати праці" });
            if (preview.ProfitTax != 0m)
                incomeBreakdown.Modifiers.Add(new BreakdownModifier { Name = "Податки з виробництва", Value = preview.ProfitTax, Source = "Виробництво" });
            if (preview.SocialContributionIncome != 0m)
                incomeBreakdown.Modifiers.Add(new BreakdownModifier { Name = "Соціальні збори із зарплат", Value = preview.SocialContributionIncome, Source = "Фонд оплати праці" });
            if (preview.AdditionalGrossIncomeTax != 0m)
                incomeBreakdown.Modifiers.Add(new BreakdownModifier { Name = "Податок на брутто-дохід", Value = preview.AdditionalGrossIncomeTax, Source = "Фонд оплати праці" });
            if (preview.AdditionalNetIncomeTax != 0m)
                incomeBreakdown.Modifiers.Add(new BreakdownModifier { Name = "Податок на нетто-дохід", Value = preview.AdditionalNetIncomeTax, Source = "Фонд оплати праці" });
            if (preview.TradeIncome != 0m)
                incomeBreakdown.Modifiers.Add(new BreakdownModifier { Name = "Торгівля", Value = preview.TradeIncome, Source = "Торгівля" });
            if (preview.StateCompanyDividends != 0m)
                incomeBreakdown.Modifiers.Add(new BreakdownModifier { Name = "Дивіденди держкомпаній", Value = preview.StateCompanyDividends, Source = "Компанії" });
            if (preview.LawTreasuryDailyIncome != 0m)
                incomeBreakdown.Modifiers.Add(new BreakdownModifier { Name = "Закони (щоденне поповнення казни)", Value = preview.LawTreasuryDailyIncome, Source = "Парламент" });

            var expensesBreakdown = new BreakdownResult
            {
                Name = "Витрати",
                BaseValue = preview.Expenses,
                Total = preview.Expenses,
                Modifiers = new List<BreakdownModifier>()
            };
            if (preview.Upkeep != 0m)
                expensesBreakdown.Modifiers.Add(new BreakdownModifier { Name = "Утримання будівель", Value = preview.Upkeep, Source = "Будівлі" });
            if (preview.GovSalaries != 0m)
                expensesBreakdown.Modifiers.Add(new BreakdownModifier { Name = "Зарплати держслужбовців", Value = preview.GovSalaries, Source = "Держсектор" });
            if (preview.SocialSpending != 0m)
                expensesBreakdown.Modifiers.Add(new BreakdownModifier { Name = "Соціальні виплати", Value = preview.SocialSpending, Source = "Населення" });
            if (preview.ScienceSpending != 0m)
                expensesBreakdown.Modifiers.Add(new BreakdownModifier { Name = "Наука", Value = preview.ScienceSpending, Source = "Наука" });
            if (preview.FestivalCost != 0m)
                expensesBreakdown.Modifiers.Add(new BreakdownModifier { Name = "Фестивалі", Value = preview.FestivalCost, Source = "Фестивалі" });
            if (preview.MilitaryUpkeep != 0m)
                expensesBreakdown.Modifiers.Add(new BreakdownModifier { Name = "Армія (після ліміту)", Value = preview.MilitaryUpkeep, Source = "Військо" });
            if (preview.InfrastructureSpending != 0m)
                expensesBreakdown.Modifiers.Add(new BreakdownModifier { Name = "Інфраструктура", Value = preview.InfrastructureSpending, Source = "Інфраструктура" });
            if (preview.StabilizationReserveFunding != 0m)
                expensesBreakdown.Modifiers.Add(new BreakdownModifier { Name = "Стабілізаційне фінансування", Value = preview.StabilizationReserveFunding, Source = "Бюджетний стабілізатор" });
            if (preview.CorruptionLoss != 0m)
                expensesBreakdown.Modifiers.Add(new BreakdownModifier { Name = "Корупційні втрати", Value = preview.CorruptionLoss, Source = "Корупція" });
            if (preview.DeputySalaryTotal != 0m)
                expensesBreakdown.Modifiers.Add(new BreakdownModifier { Name = "Витрати на парламент", Value = preview.DeputySalaryTotal, Source = "Парламент" });
            if (preview.DeputyTheft != 0m)
                expensesBreakdown.Modifiers.Add(new BreakdownModifier { Name = "Втрати через депутатів", Value = preview.DeputyTheft, Source = "Парламент" });
            if (preview.LawTreasuryDailyExpense != 0m)
                expensesBreakdown.Modifiers.Add(new BreakdownModifier { Name = "Закони (щоденні витрати казни)", Value = preview.LawTreasuryDailyExpense, Source = "Парламент" });

            var stabilityBreakdown = new BreakdownResult
            {
                Name = "Стабільність",
                BaseValue = (decimal)preview.StabilityNow,
                Total = (decimal)preview.StabilityAfter,
                Modifiers = new List<BreakdownModifier>
                {
                    new BreakdownModifier
                    {
                        Name = "Зміна за розрахунком дня",
                        Value = (decimal)(preview.StabilityAfter - preview.StabilityNow),
                        Source = "Економічний розрахунок"
                    }
                }
            };

            var unemploymentBreakdown = new BreakdownResult
            {
                Name = "Безробіття",
                BaseValue = (decimal)preview.UnemploymentNow,
                Total = (decimal)preview.UnemploymentAfter,
                Modifiers = new List<BreakdownModifier>
                {
                    new BreakdownModifier { Name = "Працездатне населення", Value = availableWorkers, Source = "Населення" },
                    new BreakdownModifier { Name = "Робочі місця", Value = totalJobs, Source = "Будівлі" },
                    new BreakdownModifier { Name = "Зміна за розрахунком дня", Value = (decimal)(preview.UnemploymentAfter - preview.UnemploymentNow), Source = "Економічний розрахунок" }
                }
            };

            return new EconomyReportBreakdown
            {
                WagesTax = preview.WagesTax,
                ProfitTax = preview.ProfitTax,
                SocialContributionIncome = preview.SocialContributionIncome,
                AdditionalGrossIncomeTax = preview.AdditionalGrossIncomeTax,
                AdditionalNetIncomeTax = preview.AdditionalNetIncomeTax,
                CitizenGrossIncome = preview.CitizenGrossIncome,
                CitizenNetIncome = preview.CitizenNetIncome,
                MainIncomeTaxRate = preview.MainIncomeTaxRate,
                SocialContributionRate = preview.SocialContributionRate,
                AdditionalGrossIncomeTaxRate = preview.AdditionalGrossIncomeTaxRate,
                AdditionalNetIncomeTaxRate = preview.AdditionalNetIncomeTaxRate,
                TradeBaseIncome = preview.TradeIncome,
                AgreementExportIncome = preview.AgreementExportIncome,
                AgreementImportCost = preview.AgreementImportCost,
                AgreementNet = preview.AgreementExportIncome - preview.AgreementImportCost,
                IncomeTotal = preview.Income,
                IncomeBreakdown = incomeBreakdown,
                Upkeep = preview.Upkeep,
                GovSalaries = preview.GovSalaries,
                SocialSpending = preview.SocialSpending,
                ScienceSpending = preview.ScienceSpending,
                FestivalCost = preview.FestivalCost,
                MilitaryUpkeep = preview.MilitaryUpkeep,
                InfrastructureSpending = preview.InfrastructureSpending,
                StabilizationReserveFunding = preview.StabilizationReserveFunding,
                CorruptionLoss = preview.CorruptionLoss,
                DeputySalaryTotal = preview.DeputySalaryTotal,
                DeputyTheft = preview.DeputyTheft,
                ExpensesTotal = preview.Expenses,
                ExpensesBreakdown = expensesBreakdown,
                StabilityBreakdown = stabilityBreakdown,
                UnemploymentBreakdown = unemploymentBreakdown,
                BudgetInfrastructure = preview.BudgetInfrastructure,
                BudgetMilitary = preview.BudgetMilitary,
                BudgetScience = preview.BudgetScience,
                BudgetSocial = preview.BudgetSocial
            };
        }

        private static decimal ApplyBudgetCap(decimal baseAmount, decimal budgeted)
        {
            if (baseAmount <= 0m)
                return 0m;
            if (budgeted <= 0m)
                return 0m;
            return budgeted >= baseAmount ? baseAmount : budgeted;
        }

        private static FieldKind? ResolveFieldKind(string kind, string fieldName)
        {
            if (kind == "building")
            {
                foreach (var f in BuildingFields.Concat(BuildingTemplateFields))
                {
                    if (f.FieldName == fieldName)
                        return f.Kind;
                }
                return null;
            }
            if (kind == "company")
            {
                foreach (var f in CompanyFields)
                {
                    if (f.FieldName == fieldName)
                        return f.Kind;
                }
                return null;
            }
            if (kind == "festival")
            {
                foreach (var f in FestivalFields)
                {
                    if (f.FieldName == fieldName)
                        return f.Kind;
                }
                return null;
            }
            if (kind == "trade")
            {
                if (fieldName == "PartnerName")
                    return FieldKind.String;
                foreach (var f in TradeFields)
                {
                    if (f.FieldName == fieldName)
                        return f.Kind;
                }
                return null;
            }
            if (kind == "militaryunit")
            {
                foreach (var f in MilitaryUnitFields)
                {
                    if (f.FieldName == fieldName)
                        return f.Kind;
                }
                return null;
            }
            if (kind == "vassal")
            {
                foreach (var f in VassalFields)
                {
                    if (f.FieldName == fieldName)
                        return f.Kind;
                }
                return null;
            }
            if (kind == "resource")
            {
                foreach (var f in ResourceFields)
                {
                    if (f.FieldName == fieldName)
                        return f.Kind;
                }
                return null;
            }
            return null;
        }

    }
}


