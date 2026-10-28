#nullable enable
using System;
using System.Collections.Generic;

namespace DNDBot.Goverment.Economy
{
    public enum ResourceType
    {
        Food,
        Clothing,
        Housing,
        Tools,
        Luxury
    }

    public enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }

    public enum PopulationGroup
    {
        Children,
        WorkingAge,
        Elderly
    }

    public sealed class PopulationBreakdown
    {
        public long Children { get; set; }
        public long WorkingAge { get; set; }
        public long Elderly { get; set; }
        public float GrowthRatePerTurn { get; set; }

        public long Total
        {
            get
            {
                long sum = Children + WorkingAge + Elderly;
                return sum < 0 ? 0 : sum;
            }
        }
    }

    public sealed class DemandInput
    {
        public PopulationBreakdown Population { get; set; } = new();
        public Season Season { get; set; }
        public float AverageHappiness { get; set; }
        public float AverageWealthIndex { get; set; }
        public float Corruption { get; set; }
        public float BlackMarketRisk { get; set; }
        public bool LuxuryEnabled { get; set; } = true;
        public List<IDemandModifier> Modifiers { get; set; } = new();
        public Dictionary<ResourceType, decimal> ResourceBasePrices { get; set; } = new();
        public Random? Random { get; set; }
    }

    public sealed class DemandProfile
    {
        public decimal Food { get; set; }
        public decimal Clothing { get; set; }
        public decimal Housing { get; set; }
        public decimal Tools { get; set; }
        public decimal Luxury { get; set; }
    }

    public sealed class DemandConfig
    {
        public DemandProfile ChildrenRates { get; set; } = new();
        public DemandProfile WorkingRates { get; set; } = new();
        public DemandProfile ElderlyRates { get; set; } = new();
        public decimal ClothingWinterMultiplier { get; set; } = 1.25m;
        public decimal HousingPerNewPerson { get; set; } = 0.5m;
        public float LuxuryHappinessWeight { get; set; } = 0.6f;
        public float LuxuryWealthWeight { get; set; } = 0.4f;

        public decimal BlackMarketMinFactor { get; set; } = 0.3m;
        public decimal BlackMarketMaxFactor { get; set; } = 0.7m;
        public decimal BlackMarketPriceMultiplier { get; set; } = 1.5m;
        public float BlackMarketBaseSuccess { get; set; } = 0.2f;
        public float BlackMarketCorruptionWeight { get; set; } = 0.6f;
        public float BlackMarketRiskWeight { get; set; } = 0.3f;

        public static DemandConfig CreateDefault()
        {
            return new DemandConfig
            {
                ChildrenRates = new DemandProfile
                {
                    Food = 0.8m,
                    Clothing = 0.7m,
                    Housing = 0.6m,
                    Tools = 0.05m,
                    Luxury = 0.2m
                },
                WorkingRates = new DemandProfile
                {
                    Food = 1.0m,
                    Clothing = 1.0m,
                    Housing = 1.0m,
                    Tools = 0.2m,
                    Luxury = 0.5m
                },
                ElderlyRates = new DemandProfile
                {
                    Food = 0.9m,
                    Clothing = 0.9m,
                    Housing = 1.1m,
                    Tools = 0.05m,
                    Luxury = 0.4m
                }
            };
        }

        public decimal GetClothingMultiplier(Season season)
        {
            return season == Season.Winter ? ClothingWinterMultiplier : 1m;
        }
    }

    public sealed class DemandRequestResult
    {
        public long TotalPopulation { get; set; }
        public Dictionary<ResourceType, decimal> Requested { get; set; } = new();
        public Dictionary<PopulationGroup, Dictionary<ResourceType, decimal>> RequestedByGroup { get; set; } = new();

        public decimal GetRequested(ResourceType type)
        {
            return Requested.TryGetValue(type, out var value) ? value : 0m;
        }
    }

    public sealed class ResourceMarketEntry
    {
        public decimal Requested { get; set; }
        public decimal Consumed { get; set; }
        public decimal Shortage { get; set; }
        public decimal BlackMarketPurchased { get; set; }
        public decimal BlackMarketPrice { get; set; }
        public decimal FinalConsumed { get; set; }
        public decimal RemainingShortage { get; set; }
    }

    public sealed class ResourceMarketResult
    {
        public long TotalPopulation { get; set; }
        public Dictionary<ResourceType, ResourceMarketEntry> Entries { get; } = new();
        public Dictionary<PopulationGroup, Dictionary<ResourceType, decimal>> RequestedByGroup { get; set; } = new();

        public ResourceMarketEntry GetEntry(ResourceType type)
        {
            if (!Entries.TryGetValue(type, out var entry))
            {
                entry = new ResourceMarketEntry();
                Entries[type] = entry;
            }
            return entry;
        }

        public decimal GetRequested(ResourceType type) => GetEntry(type).Requested;
        public decimal GetConsumed(ResourceType type) => GetEntry(type).Consumed;
        public decimal GetRemainingShortage(ResourceType type) => GetEntry(type).RemainingShortage;
    }

    public sealed class SupplySnapshot
    {
        public Dictionary<ResourceType, decimal> Supply { get; } = new();

        public decimal GetSupply(ResourceType type)
        {
            return Supply.TryGetValue(type, out var value) ? value : 0m;
        }
    }

    public interface IDemandModifier
    {
        void Apply(DemandContext context);
    }

    public sealed class PercentDemandModifier : IDemandModifier
    {
        public ResourceType ResourceType { get; }
        public decimal PercentDelta { get; }

        public PercentDemandModifier(ResourceType resourceType, decimal percentDelta)
        {
            ResourceType = resourceType;
            PercentDelta = percentDelta;
        }

        public void Apply(DemandContext context)
        {
            var current = context.Get(ResourceType);
            context.Set(ResourceType, current * (1m + PercentDelta));
        }
    }

    public sealed class PercentAllDemandModifier : IDemandModifier
    {
        public decimal PercentDelta { get; }

        public PercentAllDemandModifier(decimal percentDelta)
        {
            PercentDelta = percentDelta;
        }

        public void Apply(DemandContext context)
        {
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                var current = context.Get(type);
                context.Set(type, current * (1m + PercentDelta));
            }
        }
    }

    public sealed class DemandContext
    {
        public DemandInput Input { get; }
        public DemandConfig Config { get; }
        public Dictionary<ResourceType, decimal> Demand { get; }

        public DemandContext(DemandInput input, DemandConfig config, Dictionary<ResourceType, decimal> demand)
        {
            Input = input;
            Config = config;
            Demand = demand;
        }

        public void Set(ResourceType type, decimal value)
        {
            Demand[type] = value;
        }

        public void Add(ResourceType type, decimal delta)
        {
            Demand[type] = Get(type) + delta;
        }

        public decimal Get(ResourceType type)
        {
            return Demand.TryGetValue(type, out var value) ? value : 0m;
        }
    }

    public sealed class DemandCalculator
    {
        public DemandRequestResult CalculateDemand(DemandInput input, DemandConfig? config = null)
        {
            var cfg = config ?? DemandConfig.CreateDefault();
            long children = input.Population?.Children ?? 0;
            long working = input.Population?.WorkingAge ?? 0;
            long elderly = input.Population?.Elderly ?? 0;
            float growthRate = input.Population?.GrowthRatePerTurn ?? 0f;

            decimal clothingMultiplier = cfg.GetClothingMultiplier(input.Season);
            float happinessFactor = Clamp01(input.AverageHappiness / 100f);
            float wealthFactor = Clamp01(input.AverageWealthIndex);
            float luxuryFactor = Clamp01(happinessFactor * cfg.LuxuryHappinessWeight + wealthFactor * cfg.LuxuryWealthWeight);
            bool luxuryEnabled = input.LuxuryEnabled;

            var byGroup = new Dictionary<PopulationGroup, Dictionary<ResourceType, decimal>>
            {
                [PopulationGroup.Children] = BuildGroupDemand(children, cfg.ChildrenRates, clothingMultiplier, luxuryFactor, luxuryEnabled),
                [PopulationGroup.WorkingAge] = BuildGroupDemand(working, cfg.WorkingRates, clothingMultiplier, luxuryFactor, luxuryEnabled),
                [PopulationGroup.Elderly] = BuildGroupDemand(elderly, cfg.ElderlyRates, clothingMultiplier, luxuryFactor, luxuryEnabled)
            };

            var total = new Dictionary<ResourceType, decimal>();
            foreach (var kv in byGroup)
            {
                foreach (var entry in kv.Value)
                {
                    if (total.TryGetValue(entry.Key, out var current))
                        total[entry.Key] = current + entry.Value;
                    else
                        total[entry.Key] = entry.Value;
                }
            }

            long totalPopulation = (input.Population?.Total ?? 0);
            decimal newPeople = totalPopulation * (decimal)Math.Max(0f, growthRate);
            if (newPeople > 0m)
            {
                AddOrSet(total, ResourceType.Housing, newPeople * cfg.HousingPerNewPerson);
            }

            var context = new DemandContext(input, cfg, total);
            if (input.Modifiers != null)
            {
                foreach (var modifier in input.Modifiers)
                {
                    modifier?.Apply(context);
                }
            }

            Normalize(total);

            return new DemandRequestResult
            {
                TotalPopulation = totalPopulation,
                RequestedByGroup = byGroup,
                Requested = total
            };
        }

        private static Dictionary<ResourceType, decimal> BuildGroupDemand(
            long population,
            DemandProfile profile,
            decimal clothingMultiplier,
            float luxuryFactor,
            bool luxuryEnabled)
        {
            var demand = new Dictionary<ResourceType, decimal>
            {
                [ResourceType.Food] = population * profile.Food,
                [ResourceType.Clothing] = population * profile.Clothing * clothingMultiplier,
                [ResourceType.Housing] = population * profile.Housing,
                [ResourceType.Tools] = population * profile.Tools,
                [ResourceType.Luxury] = luxuryEnabled ? population * profile.Luxury * (decimal)luxuryFactor : 0m
            };
            return demand;
        }

        private static void Normalize(Dictionary<ResourceType, decimal> demand)
        {
            foreach (var key in demand.Keys)
            {
                if (demand[key] < 0m)
                    demand[key] = 0m;
            }
        }

        private static void AddOrSet(Dictionary<ResourceType, decimal> target, ResourceType type, decimal value)
        {
            if (target.TryGetValue(type, out var current))
                target[type] = current + value;
            else
                target[type] = value;
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }
    }

    public sealed class ConsumptionCalculator
    {
        public ResourceMarketResult CalculateConsumption(DemandRequestResult request, SupplySnapshot supply)
        {
            var result = new ResourceMarketResult
            {
                TotalPopulation = request.TotalPopulation,
                RequestedByGroup = request.RequestedByGroup
            };

            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                decimal requested = request.GetRequested(type);
                decimal available = supply.GetSupply(type);
                decimal consumed = requested > 0m ? Math.Min(requested, available) : 0m;
                decimal shortage = requested - consumed;

                result.Entries[type] = new ResourceMarketEntry
                {
                    Requested = requested,
                    Consumed = consumed,
                    Shortage = shortage,
                    BlackMarketPurchased = 0m,
                    BlackMarketPrice = 0m,
                    FinalConsumed = consumed,
                    RemainingShortage = shortage
                };
            }

            return result;
        }
    }

    public sealed class BlackMarketCalculator
    {
        public ResourceMarketResult ApplyBlackMarket(ResourceMarketResult market, DemandInput input, DemandConfig config)
        {
            var rng = input.Random ?? Random.Shared;
            float corruptionFactor = Clamp01(input.Corruption / 100f);
            float riskFactor = Clamp01(input.BlackMarketRisk);
            float successChance = Clamp01(config.BlackMarketBaseSuccess + corruptionFactor * config.BlackMarketCorruptionWeight - riskFactor * config.BlackMarketRiskWeight);

            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                var entry = market.GetEntry(type);
                if (entry.Shortage <= 0m)
                    continue;

                decimal factor = Lerp(config.BlackMarketMinFactor, config.BlackMarketMaxFactor, (decimal)rng.NextDouble());
                decimal potential = entry.Shortage * factor;
                decimal purchased = potential * (decimal)successChance;

                decimal basePrice = input.ResourceBasePrices.TryGetValue(type, out var price) ? price : 1m;
                decimal blackPrice = basePrice * config.BlackMarketPriceMultiplier;

                entry.BlackMarketPurchased = purchased;
                entry.BlackMarketPrice = blackPrice;
                entry.FinalConsumed = entry.Consumed + purchased;
                entry.RemainingShortage = Math.Max(0m, entry.Shortage - purchased);
            }

            return market;
        }

        private static decimal Lerp(decimal min, decimal max, decimal t)
        {
            return min + (max - min) * t;
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }
    }

    public sealed class ResourceDemandService
    {
        public ResourceMarketResult CalculateMarket(DemandInput input, SupplySnapshot supply, DemandConfig? config = null)
        {
            var cfg = config ?? DemandConfig.CreateDefault();
            var requested = new DemandCalculator().CalculateDemand(input, cfg);
            var market = new ConsumptionCalculator().CalculateConsumption(requested, supply);
            return new BlackMarketCalculator().ApplyBlackMarket(market, input, cfg);
        }
    }

    public sealed class ShortageEntry
    {
        public ResourceType ResourceType { get; set; }
        public decimal Requested { get; set; }
        public decimal Consumed { get; set; }
        public decimal RemainingShortage { get; set; }
    }

    public sealed class ResourceImpactWeights
    {
        public float HappinessWeight { get; set; }
        public float MortalityWeight { get; set; }
        public float ProductivityWeight { get; set; }
    }

    public sealed class DemandImpactConfig
    {
        public float HappinessScale { get; set; } = 10f;
        public float MortalityScale { get; set; } = 0.02f;
        public float ProductivityScale { get; set; } = 0.15f;
        public float ShortageScoreScale { get; set; } = 100f;

        private readonly Dictionary<ResourceType, ResourceImpactWeights> _weights = new();

        public DemandImpactConfig()
        {
            _weights[ResourceType.Food] = new ResourceImpactWeights { HappinessWeight = 1.0f, MortalityWeight = 1.0f, ProductivityWeight = 0.4f };
            _weights[ResourceType.Clothing] = new ResourceImpactWeights { HappinessWeight = 0.4f, MortalityWeight = 0.2f, ProductivityWeight = 0.1f };
            _weights[ResourceType.Housing] = new ResourceImpactWeights { HappinessWeight = 0.6f, MortalityWeight = 0.3f, ProductivityWeight = 0.2f };
            _weights[ResourceType.Tools] = new ResourceImpactWeights { HappinessWeight = 0.1f, MortalityWeight = 0.0f, ProductivityWeight = 1.0f };
            _weights[ResourceType.Luxury] = new ResourceImpactWeights { HappinessWeight = 0.5f, MortalityWeight = 0.0f, ProductivityWeight = 0.1f };
        }

        public ResourceImpactWeights GetWeights(ResourceType type)
        {
            return _weights.TryGetValue(type, out var weights) ? weights : new ResourceImpactWeights();
        }

        public static DemandImpactConfig CreateDefault() => new();
    }

    public sealed class DemandImpact
    {
        public float HappinessDelta { get; set; }
        public float MortalityRateDelta { get; set; }
        public float ProductivityDelta { get; set; }
    }

    public sealed class ShortageReport
    {
        public List<ShortageEntry> Shortages { get; } = new();
        public DemandImpact Impact { get; set; } = new();
        public float TotalShortageScore { get; set; }

        public bool HasShortages => Shortages.Count > 0;
    }

    public sealed class ShortageEvaluator
    {
        public ShortageReport EvaluateShortages(ResourceMarketResult market, DemandImpactConfig? config = null)
        {
            var cfg = config ?? DemandImpactConfig.CreateDefault();
            var report = new ShortageReport();
            float shortageScore = 0f;

            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                var entry = market.GetEntry(type);
                decimal requested = entry.Requested;
                decimal remaining = entry.RemainingShortage;

                if (remaining > 0m)
                {
                    report.Shortages.Add(new ShortageEntry
                    {
                        ResourceType = type,
                        Requested = requested,
                        Consumed = entry.FinalConsumed,
                        RemainingShortage = remaining
                    });
                }

                float ratio = requested <= 0m ? 0f : (float)(remaining / requested);
                var weights = cfg.GetWeights(type);
                report.Impact.HappinessDelta -= ratio * weights.HappinessWeight * cfg.HappinessScale;
                report.Impact.MortalityRateDelta += ratio * weights.MortalityWeight * cfg.MortalityScale;
                report.Impact.ProductivityDelta -= ratio * weights.ProductivityWeight * cfg.ProductivityScale;
                shortageScore += ratio * cfg.ShortageScoreScale;
            }

            report.TotalShortageScore = shortageScore;
            return report;
        }
    }

    public sealed class DemandExampleResult
    {
        public ResourceMarketResult Market { get; set; } = new();
        public ShortageReport ShortageReport { get; set; } = new();
    }

    public static class DemandExample
    {
        public static DemandExampleResult RunExample()
        {
            var input = new DemandInput
            {
                Population = new PopulationBreakdown
                {
                    Children = 20000L,
                    WorkingAge = 50000L,
                    Elderly = 15000L,
                    GrowthRatePerTurn = 0.01f
                },
                Season = Season.Winter,
                AverageHappiness = 62f,
                AverageWealthIndex = 0.4f,
                Corruption = 35f,
                BlackMarketRisk = 0.3f,
                LuxuryEnabled = true,
                ResourceBasePrices = new Dictionary<ResourceType, decimal>
                {
                    [ResourceType.Food] = 1m,
                    [ResourceType.Clothing] = 2m,
                    [ResourceType.Housing] = 5m,
                    [ResourceType.Tools] = 3m,
                    [ResourceType.Luxury] = 8m
                }
            };

            var supply = new SupplySnapshot();
            supply.Supply[ResourceType.Food] = 70000m;
            supply.Supply[ResourceType.Clothing] = 18000m;
            supply.Supply[ResourceType.Housing] = 200m;
            supply.Supply[ResourceType.Tools] = 6000m;
            supply.Supply[ResourceType.Luxury] = 3000m;

            var service = new ResourceDemandService();
            var market = service.CalculateMarket(input, supply, DemandConfig.CreateDefault());
            var report = new ShortageEvaluator().EvaluateShortages(market, DemandImpactConfig.CreateDefault());

            return new DemandExampleResult
            {
                Market = market,
                ShortageReport = report
            };
        }
    }
}

