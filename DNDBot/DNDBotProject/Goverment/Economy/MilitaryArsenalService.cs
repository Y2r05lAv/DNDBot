#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using DNDBot.DataBase;

namespace DNDBot.Goverment.Economy
{
    public sealed class MilitaryArsenalStockItem
    {
        public string ResourceType { get; set; } = string.Empty;
        public string DisplayNameUk { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal CombatPowerWeight { get; set; }
        public decimal PowerPoints => Quantity * CombatPowerWeight;
    }

    public sealed class MilitaryArsenalCompanyOffer
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public bool IsStateCompany { get; set; }
        public string ResourceType { get; set; } = string.Empty;
        public string ResourceDisplayUk { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal QuantityPerDay { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalCostPerDay => QuantityPerDay * UnitPrice;
    }

    public sealed class MilitaryArsenalProcurementResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int CompanyId { get; set; }
        public string ResourceType { get; set; } = string.Empty;
        public decimal PurchasedQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal StockAfterPurchase { get; set; }
    }

    public sealed class MilitaryArsenalDistributionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int UnitId { get; set; }
        public int UnitsAffected { get; set; }
        public decimal TotalPowerBefore { get; set; }
        public decimal PowerSpent { get; set; }
        public decimal PowerAfter { get; set; }
        public float WeaponDelta { get; set; }
        public float SupplyDelta { get; set; }
        public float QualityDelta { get; set; }
    }

    public static class MilitaryArsenalService
    {
        private sealed class OfferAccumulator
        {
            public int CompanyId { get; set; }
            public string CompanyName { get; set; } = string.Empty;
            public bool IsStateCompany { get; set; }
            public string ResourceType { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public decimal Quantity { get; set; }
            public decimal CostSum { get; set; }
        }

        public static List<MilitaryArsenalStockItem> GetStockOverview(int countryId)
        {
            MilitaryArsenalCatalog.EnsureResourcesInEconomyCatalog();

            var items = MilitaryEquipmentStocksData.GetByCountry(countryId)
                .Where(s => s.Quantity > 0m)
                .Select(s =>
                {
                    var definition = MilitaryArsenalCatalog.Find(s.ResourceType);
                    decimal weight = definition?.CombatPowerWeight ?? 0.5m;
                    string category = definition?.Category ?? "Other";
                    string display = definition?.DisplayNameUk ?? s.ResourceType;
                    return new MilitaryArsenalStockItem
                    {
                        ResourceType = definition?.ResourceType ?? s.ResourceType,
                        DisplayNameUk = display,
                        Category = category,
                        Quantity = decimal.Round(Math.Max(0m, s.Quantity), 2, MidpointRounding.AwayFromZero),
                        CombatPowerWeight = weight
                    };
                })
                .OrderByDescending(s => s.PowerPoints)
                .ThenBy(s => s.DisplayNameUk, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return items;
        }

        public static decimal GetTotalStockPowerPoints(int countryId)
        {
            return GetStockOverview(countryId).Sum(s => s.PowerPoints);
        }

        public static List<MilitaryArsenalCompanyOffer> GetCompanyOffers(int countryId)
        {
            MilitaryArsenalCatalog.EnsureResourcesInEconomyCatalog();

            var companies = EconomyCompaniesData.GetByCountry(countryId)
                .Where(c => c.IsActive)
                .ToDictionary(c => c.CompanyId);
            if (companies.Count == 0)
                return new List<MilitaryArsenalCompanyOffer>();

            var resources = EconomyResourcesData.GetAll()
                .Where(r => !string.IsNullOrWhiteSpace(r.ResourceType))
                .GroupBy(r => r.ResourceType, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToDictionary(r => r.ResourceType, StringComparer.OrdinalIgnoreCase);

            var accum = new Dictionary<string, OfferAccumulator>(StringComparer.OrdinalIgnoreCase);

            foreach (var building in EconomyBuildingsData.GetByCountry(countryId))
            {
                if (!building.CompanyId.HasValue)
                    continue;
                if (!companies.TryGetValue(building.CompanyId.Value, out var company))
                    continue;
                if (!IsBuildingOperational(building))
                    continue;
                if (!MilitaryArsenalCatalog.IsMilitaryResourceType(building.ResourceType))
                    continue;

                var definition = MilitaryArsenalCatalog.Find(building.ResourceType);
                if (definition == null)
                    continue;

                decimal quantity = EstimateBuildingOutput(building, company);
                if (quantity <= 0m)
                    continue;

                decimal unitPrice = ResolveUnitPrice(definition, company, resources);
                string key = $"{company.CompanyId}|{definition.ResourceType}";

                if (!accum.TryGetValue(key, out var bucket))
                {
                    bucket = new OfferAccumulator
                    {
                        CompanyId = company.CompanyId,
                        CompanyName = string.IsNullOrWhiteSpace(company.Name) ? $"Компанія #{company.CompanyId}" : company.Name,
                        IsStateCompany = company.IsStateCompany,
                        ResourceType = definition.ResourceType,
                        Category = definition.Category,
                        Quantity = 0m,
                        CostSum = 0m
                    };
                    accum[key] = bucket;
                }

                bucket.Quantity += quantity;
                bucket.CostSum += quantity * unitPrice;
            }

            return accum.Values
                .Where(a => a.Quantity > 0m)
                .Select(a => new MilitaryArsenalCompanyOffer
                {
                    CompanyId = a.CompanyId,
                    CompanyName = a.CompanyName,
                    IsStateCompany = a.IsStateCompany,
                    ResourceType = a.ResourceType,
                    ResourceDisplayUk = MilitaryArsenalCatalog.Find(a.ResourceType)?.DisplayNameUk ?? a.ResourceType,
                    Category = a.Category,
                    QuantityPerDay = decimal.Round(a.Quantity, 2, MidpointRounding.AwayFromZero),
                    UnitPrice = a.Quantity <= 0m
                        ? 0m
                        : decimal.Round(a.CostSum / a.Quantity, 2, MidpointRounding.AwayFromZero)
                })
                .OrderByDescending(a => a.QuantityPerDay)
                .ThenBy(a => a.CompanyName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(a => a.ResourceDisplayUk, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static MilitaryArsenalProcurementResult PurchaseFromCompany(
            int countryId,
            int companyId,
            string resourceType,
            decimal requestedQuantity,
            long actorUserId)
        {
            var result = new MilitaryArsenalProcurementResult
            {
                CompanyId = companyId,
                ResourceType = resourceType
            };

            if (countryId <= 0 || companyId <= 0 || string.IsNullOrWhiteSpace(resourceType))
            {
                result.Message = "Невірні параметри закупівлі.";
                return result;
            }

            decimal normalizedRequested = decimal.Round(Math.Max(0m, requestedQuantity), 2, MidpointRounding.AwayFromZero);
            if (normalizedRequested <= 0m)
            {
                result.Message = "Кількість для закупівлі має бути більшою за 0.";
                return result;
            }

            var country = CountriesData.GetCountryById(countryId);
            if (country == null)
            {
                result.Message = "Країну не знайдено.";
                return result;
            }

            var offers = GetCompanyOffers(countryId);
            var offer = offers.FirstOrDefault(o =>
                o.CompanyId == companyId &&
                o.ResourceType.Equals(resourceType, StringComparison.OrdinalIgnoreCase));

            if (offer == null)
            {
                result.Message = "Компанія зараз не має цієї військової продукції.";
                return result;
            }

            decimal quantity = Math.Min(normalizedRequested, offer.QuantityPerDay);
            if (quantity <= 0m)
            {
                result.Message = "Немає доступної кількості для закупівлі.";
                return result;
            }

            decimal totalCost = decimal.Round(quantity * offer.UnitPrice, 2, MidpointRounding.AwayFromZero);
            decimal treasuryCharge = decimal.Ceiling(totalCost);
            if (treasuryCharge <= 0m)
                treasuryCharge = 1m;

            if (country.Treasury < (long)treasuryCharge)
            {
                result.Message = $"Недостатньо коштів у казні: потрібно {treasuryCharge.ToString("N0", CultureInfo.InvariantCulture)}.";
                return result;
            }

            country.Treasury -= (long)treasuryCharge;
            country.LastUpdate = DateTime.UtcNow;
            CountriesData.UpdateCountry(country);

            var company = EconomyCompaniesData.GetById(companyId, countryId);
            if (company != null)
            {
                decimal nextBudget = decimal.Round(Math.Max(0m, company.Budget) + treasuryCharge, 2, MidpointRounding.AwayFromZero);
                EconomyCompaniesData.UpdateCompanyField(company.CompanyId, countryId, "Budget", nextBudget, SqlDbType.Decimal);
            }

            MilitaryEquipmentStocksData.AddQuantity(countryId, offer.ResourceType, quantity);
            decimal stockAfter = MilitaryEquipmentStocksData.GetQuantity(countryId, offer.ResourceType);

            EconomyMoneyLogData.AddLog(
                countryId,
                actorUserId,
                treasuryCharge,
                "Expense",
                $"Держзакупівля: {offer.ResourceDisplayUk} у {offer.CompanyName}",
                country.TurnsSurvived);

            result.Success = true;
            result.PurchasedQuantity = quantity;
            result.UnitPrice = offer.UnitPrice;
            result.PaidAmount = treasuryCharge;
            result.StockAfterPurchase = stockAfter;
            result.Message = $"Закуплено {quantity:0.##} од. «{offer.ResourceDisplayUk}» за {treasuryCharge:0.##}.";
            return result;
        }

        public static MilitaryArsenalDistributionResult AutoDistributeAll(int countryId)
        {
            var result = new MilitaryArsenalDistributionResult();
            var units = GetDistributableUnits(countryId);
            if (units.Count == 0)
            {
                result.Message = "Немає бойових частин для розподілу.";
                return result;
            }

            var stock = GetStockOverview(countryId);
            if (stock.Count == 0)
            {
                result.Message = "Склад порожній. Спочатку проведіть закупівлі.";
                return result;
            }

            decimal totalPower = stock.Sum(s => s.PowerPoints);
            if (totalPower <= 0m)
            {
                result.Message = "На складі немає придатної амуніції для розподілу.";
                return result;
            }

            decimal totalSoldiers = units.Sum(u => Math.Max(1, u.Soldiers));
            decimal averageSoldiers = units.Count == 0 ? 1m : totalSoldiers / units.Count;
            decimal neededPower = Math.Max(1m, totalSoldiers * 0.80m);
            decimal coverage = ClampDecimal(totalPower / neededPower, 0m, 2.10m);

            float baseWeaponDelta = (float)(4m + coverage * 10m);
            float baseSupplyDelta = (float)(5m + coverage * 12m);
            float baseQualityDelta = (float)(2m + coverage * 8m);

            int affected = 0;
            float weaponApplied = 0f;
            float supplyApplied = 0f;
            float qualityApplied = 0f;

            foreach (var unit in units)
            {
                float scale = ComputeUnitScale(unit.Soldiers, averageSoldiers);
                float weaponDelta = baseWeaponDelta * scale;
                float supplyDelta = baseSupplyDelta * scale;
                float qualityDelta = baseQualityDelta * scale;

                ApplyUnitDelta(unit, weaponDelta, supplyDelta, qualityDelta);
                if (!MilitaryUnitsData.UpdateUnit(unit))
                    continue;

                affected++;
                weaponApplied += weaponDelta;
                supplyApplied += supplyDelta;
                qualityApplied += qualityDelta;
            }

            if (affected <= 0)
            {
                result.Message = "Не вдалося оновити військові частини.";
                return result;
            }

            MilitaryEquipmentStocksData.ClearByCountry(countryId);

            result.Success = true;
            result.UnitsAffected = affected;
            result.TotalPowerBefore = totalPower;
            result.PowerSpent = totalPower;
            result.PowerAfter = 0m;
            result.WeaponDelta = weaponApplied / affected;
            result.SupplyDelta = supplyApplied / affected;
            result.QualityDelta = qualityApplied / affected;
            result.Message =
                $"Авто-розподіл завершено: охоплено {affected} частин, середні бонуси " +
                $"озброєння +{result.WeaponDelta:0.#}, забезпечення +{result.SupplyDelta:0.#}, якість +{result.QualityDelta:0.#}.";
            return result;
        }

        public static MilitaryArsenalDistributionResult DistributeToUnit(int countryId, int unitId, int stockSharePercent)
        {
            var result = new MilitaryArsenalDistributionResult { UnitId = unitId };
            if (countryId <= 0 || unitId <= 0)
            {
                result.Message = "Невірні параметри розподілу.";
                return result;
            }

            int sharePercent = Math.Max(5, Math.Min(100, stockSharePercent));
            decimal share = sharePercent / 100m;

            var unit = MilitaryUnitsData.GetByCountry(countryId)
                .FirstOrDefault(u => u.UnitId == unitId);
            if (unit == null)
            {
                result.Message = "Частину не знайдено.";
                return result;
            }

            var stock = GetStockOverview(countryId);
            if (stock.Count == 0)
            {
                result.Message = "Склад порожній.";
                return result;
            }

            decimal totalPowerBefore = stock.Sum(s => s.PowerPoints);
            decimal spentPower = 0m;

            foreach (var item in stock)
            {
                decimal plannedQty = decimal.Round(item.Quantity * share, 2, MidpointRounding.AwayFromZero);
                if (plannedQty <= 0m && item.Quantity > 0m && sharePercent == 100)
                    plannedQty = item.Quantity;

                if (plannedQty <= 0m)
                    continue;

                decimal consumed = MilitaryEquipmentStocksData.ConsumeQuantity(countryId, item.ResourceType, plannedQty);
                if (consumed <= 0m)
                    continue;

                spentPower += consumed * item.CombatPowerWeight;
            }

            if (spentPower <= 0m)
            {
                result.Message = "Не вдалося видати спорядження цій частині.";
                return result;
            }

            decimal needPower = Math.Max(1m, Math.Max(1, unit.Soldiers) * 0.85m);
            decimal coverage = ClampDecimal(spentPower / needPower, 0m, 2.25m);

            float weaponDelta = (float)(5m + coverage * 14m);
            float supplyDelta = (float)(4m + coverage * 16m);
            float qualityDelta = (float)(3m + coverage * 12m);

            ApplyUnitDelta(unit, weaponDelta, supplyDelta, qualityDelta);
            bool saved = MilitaryUnitsData.UpdateUnit(unit);
            if (!saved)
            {
                result.Message = "Ресурси списано, але не вдалося оновити частину.";
                return result;
            }

            decimal powerAfter = GetTotalStockPowerPoints(countryId);

            result.Success = true;
            result.UnitsAffected = 1;
            result.TotalPowerBefore = totalPowerBefore;
            result.PowerSpent = spentPower;
            result.PowerAfter = powerAfter;
            result.WeaponDelta = weaponDelta;
            result.SupplyDelta = supplyDelta;
            result.QualityDelta = qualityDelta;
            result.Message =
                $"Частині видано {sharePercent}% складу: озброєння +{weaponDelta:0.#}, " +
                $"забезпечення +{supplyDelta:0.#}, якість +{qualityDelta:0.#}.";
            return result;
        }

        private static List<MilitaryUnitRecord> GetDistributableUnits(int countryId)
        {
            return MilitaryUnitsData.GetByCountry(countryId)
                .Where(u => Math.Max(0, u.Soldiers) > 0)
                .Where(u =>
                    !string.Equals(u.Status, "Destroyed", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(u.Status, "Routed", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(u => u.Soldiers)
                .ThenBy(u => u.UnitId)
                .ToList();
        }

        private static bool IsBuildingOperational(EconomyBuildingRecord building)
        {
            if (building == null)
                return false;
            if (building.BuildTurns > 0)
                return false;
            if (building.BaseProduction <= 0m)
                return false;

            return string.Equals(building.BuildingStatus, BuildingStatus.Active.ToString(), StringComparison.OrdinalIgnoreCase)
                || string.Equals(building.BuildingStatus, BuildingStatus.Degraded.ToString(), StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(building.BuildingStatus);
        }

        private static decimal EstimateBuildingOutput(EconomyBuildingRecord building, EconomyCompanyRecord company)
        {
            decimal baseProduction = Math.Max(0m, building.BaseProduction);
            if (baseProduction <= 0m)
                return 0m;

            decimal levelFactor = 1m + Math.Max(0, building.Level - 1) * 0.12m;
            decimal degradationFactor = string.Equals(building.BuildingStatus, BuildingStatus.Degraded.ToString(), StringComparison.OrdinalIgnoreCase)
                ? 0.72m
                : 1m;

            decimal share = ClampDecimal((decimal)Math.Max(0f, company.MilitaryProductionShare) / 100m, 0m, 1m);
            decimal shareFactor = 0.55m + share * 0.90m;
            decimal typeFactor = IsMilitaryCompany(company) ? 1.25m : 0.85m;
            decimal ownershipFactor = company.IsStateCompany ? 1.05m : 1m;

            decimal output = baseProduction * levelFactor * degradationFactor * shareFactor * typeFactor * ownershipFactor;
            return decimal.Round(Math.Max(0m, output), 2, MidpointRounding.AwayFromZero);
        }

        private static decimal ResolveUnitPrice(
            MilitaryResourceDefinition definition,
            EconomyCompanyRecord company,
            IReadOnlyDictionary<string, EconomyResourceRecord> resources)
        {
            decimal basePrice = definition.BasePrice;
            if (resources.TryGetValue(definition.ResourceType, out var record))
                basePrice = Math.Max(0.05m, record.BasePrice);

            decimal militaryShare = ClampDecimal((decimal)Math.Max(0f, company.MilitaryProductionShare) / 100m, 0m, 1m);
            decimal shareDiscount = 1m - Math.Min(0.22m, militaryShare * 0.20m);
            decimal stateDiscount = company.IsStateCompany ? 0.94m : 1m;
            decimal unitPrice = basePrice * shareDiscount * stateDiscount;

            return decimal.Round(Math.Max(0.05m, unitPrice), 2, MidpointRounding.AwayFromZero);
        }

        private static bool IsMilitaryCompany(EconomyCompanyRecord company)
        {
            string productionType = (company.ProductionType ?? string.Empty).Trim().ToLowerInvariant();
            if (company.MilitaryProductionShare >= 20f)
                return true;

            return productionType.Contains("military", StringComparison.Ordinal)
                || productionType.Contains("weapon", StringComparison.Ordinal)
                || productionType.Contains("ammo", StringComparison.Ordinal)
                || productionType.Contains("armor", StringComparison.Ordinal)
                || productionType.Contains("siege", StringComparison.Ordinal)
                || productionType.Contains("arcane", StringComparison.Ordinal)
                || productionType.Contains("arms", StringComparison.Ordinal)
                || productionType.Contains("війсь", StringComparison.Ordinal)
                || productionType.Contains("збро", StringComparison.Ordinal)
                || productionType.Contains("облог", StringComparison.Ordinal)
                || productionType.Contains("арк", StringComparison.Ordinal);
        }

        private static void ApplyUnitDelta(MilitaryUnitRecord unit, float weaponDelta, float supplyDelta, float qualityDelta)
        {
            unit.WeaponReadiness = EconomyCalculator.Clamp(unit.WeaponReadiness + weaponDelta, 0f, 100f);
            unit.SupplyLevel = EconomyCalculator.Clamp(unit.SupplyLevel + supplyDelta, 0f, 100f);
            unit.EquipmentQuality = EconomyCalculator.Clamp(unit.EquipmentQuality + qualityDelta, 0f, 100f);
            unit.UpdatedAt = DateTime.UtcNow;
        }

        private static float ComputeUnitScale(int soldiers, decimal averageSoldiers)
        {
            decimal avg = Math.Max(1m, averageSoldiers);
            decimal ratio = ClampDecimal(Math.Max(1, soldiers) / avg, 0.25m, 2m);
            decimal scale = 0.85m + (decimal)Math.Sqrt((double)ratio) * 0.22m;
            return (float)ClampDecimal(scale, 0.75m, 1.30m);
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
