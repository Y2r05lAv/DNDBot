#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DNDBot.DataBase;

namespace DNDBot.Goverment.Economy
{
    public sealed class MilitaryIntelOperationRequest
    {
        public int SourceCountryId { get; set; }
        public int TargetCountryId { get; set; }
        public string OperationType { get; set; } = "ScoutForces";
        public long BudgetGold { get; set; } = 500;
        public long UserId { get; set; }
    }

    public class MilitaryIntelOperationPreview
    {
        public bool IsValid { get; set; }
        public string Error { get; set; } = string.Empty;
        public string OperationType { get; set; } = "ScoutForces";
        public long BudgetGold { get; set; }
        public float SuccessChance { get; set; }
        public float DetectionChance { get; set; }
        public float IntelligenceAdvantage { get; set; }
    }

    public sealed class MilitaryIntelOperationResult : MilitaryIntelOperationPreview
    {
        public bool Success { get; set; }
        public bool Detected { get; set; }
        public float Roll { get; set; }
        public long SourceTreasuryDelta { get; set; }
        public long TargetTreasuryDelta { get; set; }
        public float SourceIntelDelta { get; set; }
        public float SourceCounterIntelDelta { get; set; }
        public float TargetIntelDelta { get; set; }
        public float TargetCounterIntelDelta { get; set; }
        public float TargetArmyMoraleDelta { get; set; }
        public float TargetLogisticsDelta { get; set; }
        public int AffectedUnits { get; set; }
        public float ReconPowerEstimate { get; set; }
        public int ReconTroopsEstimate { get; set; }
        public int IntelLogId { get; set; }
        public string Report { get; set; } = string.Empty;
    }

    public static class MilitaryIntelService
    {
        private sealed class ChanceSnapshot
        {
            public float SuccessChance { get; init; }
            public float DetectionChance { get; init; }
            public float IntelligenceAdvantage { get; init; }
            public float BudgetFactor { get; init; }
        }

        private static readonly string[] OperationKeysInternal =
        {
            "ScoutForces",
            "StealTreasury",
            "SabotageLogistics",
            "CounterintelRaid"
        };

        public static IReadOnlyList<string> OperationKeys => OperationKeysInternal;

        public static string NormalizeOperationType(string? operationType)
        {
            if (string.IsNullOrWhiteSpace(operationType))
                return "ScoutForces";

            string value = operationType.Trim().ToLowerInvariant();
            return value switch
            {
                "scoutforces" or "scout" or "recon" or "розвідка" or "розвідка військ" => "ScoutForces",
                "stealtreasury" or "steal" or "economic" or "економічне шпигунство" or "кража казни" => "StealTreasury",
                "sabotagelogistics" or "sabotage" or "диверсія" or "диверсія логістики" => "SabotageLogistics",
                "counterintelraid" or "counterintel" or "контррозвідувальний рейд" or "контррейд" => "CounterintelRaid",
                _ => operationType.Trim()
            };
        }

        public static string FormatOperationTypeUk(string? operationType)
        {
            return NormalizeOperationType(operationType) switch
            {
                "ScoutForces" => "Розвідка військ",
                "StealTreasury" => "Економічне шпигунство",
                "SabotageLogistics" => "Диверсія логістики",
                "CounterintelRaid" => "Контррозвідувальний рейд",
                _ => operationType ?? "Розвідка військ"
            };
        }

        public static MilitaryIntelOperationPreview PreviewOperation(MilitaryIntelOperationRequest request)
        {
            string operationType = NormalizeOperationType(request.OperationType);
            long budget = NormalizeBudget(request.BudgetGold);
            var preview = new MilitaryIntelOperationPreview
            {
                IsValid = false,
                OperationType = operationType,
                BudgetGold = budget
            };

            if (request.SourceCountryId <= 0 || request.TargetCountryId <= 0)
            {
                preview.Error = "Невірні параметри країн для операції.";
                return preview;
            }

            if (request.SourceCountryId == request.TargetCountryId)
            {
                preview.Error = "Не можна проводити операцію проти власної держави.";
                return preview;
            }

            var source = CountriesData.GetCountryById(request.SourceCountryId);
            var target = CountriesData.GetCountryById(request.TargetCountryId);
            if (source == null || target == null)
            {
                preview.Error = "Одна зі сторін не знайдена.";
                return preview;
            }

            var chances = ComputeChances(source, target, operationType, budget);
            preview.SuccessChance = chances.SuccessChance;
            preview.DetectionChance = chances.DetectionChance;
            preview.IntelligenceAdvantage = chances.IntelligenceAdvantage;
            preview.IsValid = true;
            return preview;
        }

        public static MilitaryIntelOperationResult ExecuteOperation(MilitaryIntelOperationRequest request)
        {
            var preview = PreviewOperation(request);
            var result = new MilitaryIntelOperationResult
            {
                IsValid = preview.IsValid,
                Error = preview.Error,
                OperationType = preview.OperationType,
                BudgetGold = preview.BudgetGold,
                SuccessChance = preview.SuccessChance,
                DetectionChance = preview.DetectionChance,
                IntelligenceAdvantage = preview.IntelligenceAdvantage
            };

            if (!preview.IsValid)
                return result;

            var source = CountriesData.GetCountryById(request.SourceCountryId);
            var target = CountriesData.GetCountryById(request.TargetCountryId);
            if (source == null || target == null)
            {
                result.Error = "Одна зі сторін не знайдена.";
                return result;
            }

            string operationType = NormalizeOperationType(request.OperationType);
            long budget = NormalizeBudget(request.BudgetGold);
            var chances = ComputeChances(source, target, operationType, budget);
            result.SuccessChance = chances.SuccessChance;
            result.DetectionChance = chances.DetectionChance;
            result.IntelligenceAdvantage = chances.IntelligenceAdvantage;

            float roll = Random.Shared.NextSingle();
            bool success = roll <= chances.SuccessChance;
            float detectionChance = EconomyCalculator.Clamp(
                chances.DetectionChance + (success ? 0f : 0.18f),
                0.05f,
                0.98f);
            bool detected = Random.Shared.NextSingle() <= detectionChance;

            result.Roll = roll;
            result.Success = success;
            result.Detected = detected;
            result.DetectionChance = detectionChance;

            try
            {
                string report = string.Empty;
                int affectedUnits = 0;

                if (success)
                {
                    switch (operationType)
                    {
                        case "ScoutForces":
                        {
                            report = BuildScoutReport(source, target, chances, out float reconPower, out int reconTroops, out affectedUnits);
                            result.ReconPowerEstimate = reconPower;
                            result.ReconTroopsEstimate = reconTroops;
                            result.SourceIntelDelta += 0.25f + chances.BudgetFactor * 0.6f;
                            break;
                        }
                        case "StealTreasury":
                        {
                            long targetTreasury = Math.Max(0L, target.Treasury);
                            long capByBudget = Math.Max(0L, (long)Math.Round(
                                budget * (2.2 + chances.BudgetFactor * 1.4),
                                MidpointRounding.AwayFromZero));
                            long capByTreasury = Math.Max(0L, (long)Math.Round(
                                targetTreasury * (0.08 + chances.BudgetFactor * 0.07),
                                MidpointRounding.AwayFromZero));
                            long stolen = Math.Min(targetTreasury, Math.Min(capByBudget, capByTreasury));

                            if (stolen > 0)
                            {
                                source.Treasury = Math.Max(0L, source.Treasury + stolen);
                                target.Treasury = Math.Max(0L, target.Treasury - stolen);
                                result.SourceTreasuryDelta += stolen;
                                result.TargetTreasuryDelta -= stolen;
                                report =
$@"Агенти вивели кошти з казни цілі.
Отримано: {stolen.ToString("N0", CultureInfo.InvariantCulture)} золота.";
                            }
                            else
                            {
                                report = "Агенти проникли в фінансову мережу цілі, але коштів для виводу не знайдено.";
                            }

                            result.SourceIntelDelta += 0.35f + chances.BudgetFactor * 0.4f;
                            result.TargetCounterIntelDelta += 0.12f + chances.BudgetFactor * 0.2f;
                            break;
                        }
                        case "SabotageLogistics":
                        {
                            var units = MilitaryUnitsData.GetByCountry(target.CountryId)
                                .Where(u => u != null && u.Soldiers > 0 && !IsDestroyedStatus(u.Status))
                                .ToList();

                            if (units.Count == 0)
                            {
                                report = "Саботаж підготовлено, але в цілі немає активних частин для порушення логістики.";
                            }
                            else
                            {
                                int toAffect = Math.Max(1, (int)Math.Round(
                                    units.Count * (0.35f + chances.BudgetFactor * 0.45f),
                                    MidpointRounding.AwayFromZero));
                                toAffect = Math.Min(units.Count, toAffect);

                                var picked = units
                                    .OrderBy(_ => Random.Shared.Next())
                                    .Take(toAffect)
                                    .ToList();

                                foreach (var unit in picked)
                                {
                                    float fatigueGain = RandomRange(2.8f, 6.6f + chances.BudgetFactor * 4.5f);
                                    float supplyLoss = RandomRange(3.5f, 9.5f + chances.BudgetFactor * 8f);
                                    float readinessLoss = RandomRange(2.5f, 7.2f + chances.BudgetFactor * 6f);
                                    float moraleLoss = RandomRange(0.7f, 2.6f + chances.BudgetFactor * 2.4f);

                                    unit.Fatigue = EconomyCalculator.Clamp(unit.Fatigue + fatigueGain, 0f, 100f);
                                    unit.SupplyLevel = EconomyCalculator.Clamp(unit.SupplyLevel - supplyLoss, 0f, 100f);
                                    unit.WeaponReadiness = EconomyCalculator.Clamp(unit.WeaponReadiness - readinessLoss, 0f, 100f);
                                    unit.Morale = EconomyCalculator.Clamp(unit.Morale - moraleLoss, 0f, 100f);
                                    unit.UpdatedAt = DateTime.UtcNow;
                                    MilitaryUnitsData.UpdateUnit(unit);
                                }

                                affectedUnits = picked.Count;
                                float moraleHit = 1.2f + chances.BudgetFactor * 2.4f;
                                float logisticsHit = 0.4f + chances.BudgetFactor * 1.8f;
                                target.ArmyMorale = EconomyCalculator.Clamp(target.ArmyMorale - moraleHit, 0f, 100f);
                                target.LogisticsEfficiency = EconomyCalculator.Clamp(target.LogisticsEfficiency - logisticsHit, 0f, 100f);

                                result.TargetArmyMoraleDelta -= moraleHit;
                                result.TargetLogisticsDelta -= logisticsHit;
                                report =
$@"Порушено поставки та ротацію тилу цілі.
Зачеплено частин: {picked.Count.ToString("N0", CultureInfo.InvariantCulture)}.
Мораль армії цілі: -{moraleHit:0.##}, логістика: -{logisticsHit:0.##}.";
                            }

                            result.SourceIntelDelta += 0.44f + chances.BudgetFactor * 0.6f;
                            break;
                        }
                        case "CounterintelRaid":
                        {
                            float intelLoss = 0.7f + chances.BudgetFactor * 2.8f;
                            float counterLoss = 0.45f + chances.BudgetFactor * 2.2f;
                            float ownCounterGain = 0.35f + chances.BudgetFactor * 1.5f;
                            float ownIntelGain = 0.12f + chances.BudgetFactor * 0.4f;

                            result.TargetIntelDelta -= intelLoss;
                            result.TargetCounterIntelDelta -= counterLoss;
                            result.SourceCounterIntelDelta += ownCounterGain;
                            result.SourceIntelDelta += ownIntelGain;

                            report =
$@"Проведено контррозвідувальний рейд по агентурних каналах цілі.
Розвідка цілі: -{intelLoss:0.##}, контррозвідка цілі: -{counterLoss:0.##}.";
                            break;
                        }
                        default:
                            report = "Операцію виконано.";
                            break;
                    }
                }
                else
                {
                    float intelLoss = 0.2f + chances.BudgetFactor * (operationType == "SabotageLogistics" ? 0.85f : 0.45f);
                    result.SourceIntelDelta -= intelLoss;
                    if (operationType == "CounterintelRaid")
                    {
                        float counterLoss = 0.35f + chances.BudgetFactor * 0.7f;
                        result.SourceCounterIntelDelta -= counterLoss;
                    }

                    report = "Операція провалена: агентурна мережа не виконала задачу.";
                }

                if (detected)
                {
                    float targetCounterGain = 0.42f + chances.BudgetFactor * 1.35f;
                    float sourceRepLoss = 0.8f + chances.BudgetFactor * 2.2f;
                    source.InternationalReputation = EconomyCalculator.Clamp(source.InternationalReputation - sourceRepLoss, 0f, 100f);
                    target.InternationalReputation = EconomyCalculator.Clamp(target.InternationalReputation + sourceRepLoss * 0.3f, 0f, 100f);
                    result.TargetCounterIntelDelta += targetCounterGain;

                    string diplomacyImpact = ApplyDetectionDiplomacyPenalty(operationType, source.CountryId, target.CountryId, request.UserId);
                    if (!string.IsNullOrWhiteSpace(diplomacyImpact))
                        report += $"\n{diplomacyImpact}";
                }

                source.IntelLevel = EconomyCalculator.Clamp(source.IntelLevel + result.SourceIntelDelta, 0f, 100f);
                source.CounterIntelLevel = EconomyCalculator.Clamp(source.CounterIntelLevel + result.SourceCounterIntelDelta, 0f, 100f);
                target.IntelLevel = EconomyCalculator.Clamp(target.IntelLevel + result.TargetIntelDelta, 0f, 100f);
                target.CounterIntelLevel = EconomyCalculator.Clamp(target.CounterIntelLevel + result.TargetCounterIntelDelta, 0f, 100f);

                source.LastUpdate = DateTime.UtcNow;
                target.LastUpdate = DateTime.UtcNow;
                CountriesData.UpdateCountry(source);
                CountriesData.UpdateCountry(target);

                if (result.SourceTreasuryDelta > 0)
                {
                    EconomyMoneyLogData.AddLog(
                        source.CountryId,
                        request.UserId,
                        result.SourceTreasuryDelta,
                        "Income",
                        $"Розвідоперація: {FormatOperationTypeUk(operationType)}",
                        source.TurnsSurvived);
                }

                if (result.TargetTreasuryDelta < 0)
                {
                    decimal targetLoss = Math.Abs(result.TargetTreasuryDelta);
                    EconomyMoneyLogData.AddLog(
                        target.CountryId,
                        0,
                        targetLoss,
                        "Expense",
                        detected
                            ? "Втрати від ворожої шпигунської операції"
                            : "Невраховані фінансові втрати (ймовірне шпигунство)",
                        target.TurnsSurvived);
                }

                result.Report = report;
                result.AffectedUnits = affectedUnits;
                int logId = MilitaryIntelOperationsData.Add(new MilitaryIntelOperationLogRecord
                {
                    SourceCountryId = source.CountryId,
                    TargetCountryId = target.CountryId,
                    OperationType = operationType,
                    BudgetGold = budget,
                    Success = success,
                    Detected = detected,
                    VisibleToTarget = detected,
                    SuccessChance = result.SuccessChance,
                    DetectionChance = result.DetectionChance,
                    Roll = result.Roll,
                    SourceTreasuryDelta = result.SourceTreasuryDelta,
                    TargetTreasuryDelta = result.TargetTreasuryDelta,
                    SourceIntelDelta = result.SourceIntelDelta,
                    SourceCounterIntelDelta = result.SourceCounterIntelDelta,
                    TargetIntelDelta = result.TargetIntelDelta,
                    TargetCounterIntelDelta = result.TargetCounterIntelDelta,
                    TargetArmyMoraleDelta = result.TargetArmyMoraleDelta,
                    TargetLogisticsDelta = result.TargetLogisticsDelta,
                    AffectedUnits = result.AffectedUnits,
                    ReconPowerEstimate = result.ReconPowerEstimate,
                    ReconTroopsEstimate = result.ReconTroopsEstimate,
                    Report = result.Report,
                    CreatedAt = DateTime.UtcNow
                });
                result.IntelLogId = logId;
                return result;
            }
            catch (Exception ex)
            {
                result.Error = $"Помилка виконання операції: {ex.Message}";
                return result;
            }
        }

        private static string BuildScoutReport(
            CountryRecord source,
            CountryRecord target,
            ChanceSnapshot chances,
            out float reconPower,
            out int reconTroops,
            out int affectedUnits)
        {
            var estimate = MilitaryBattleService.EstimateCountryPower(
                target.CountryId,
                "Plains",
                "Balanced",
                attacking: false,
                includeVassals: true);

            var targetUnits = MilitaryUnitsData.GetByCountry(target.CountryId)
                .Where(u => u != null && u.Soldiers > 0 && !IsDestroyedStatus(u.Status))
                .ToList();
            affectedUnits = targetUnits.Count;

            float precision = EconomyCalculator.Clamp(
                0.33f
                - chances.BudgetFactor * 0.17f
                + Math.Max(0f, (target.CounterIntelLevel - source.IntelLevel) / 260f),
                0.05f,
                0.36f);

            float noisyPower = estimate.Power * (1f + RandomRange(-precision, precision));
            int noisyTroops = (int)Math.Round(
                estimate.TotalTroops * (1f + RandomRange(-precision, precision)),
                MidpointRounding.AwayFromZero);

            reconPower = Math.Max(0f, noisyPower);
            reconTroops = Math.Max(0, noisyTroops);

            float avgTraining = targetUnits.Count == 0 ? 0f : targetUnits.Average(u => u.TrainingLevel);
            float avgMorale = targetUnits.Count == 0 ? 0f : targetUnits.Average(u => u.Morale);
            float trainingEstimate = EconomyCalculator.Clamp(avgTraining + RandomRange(-precision * 22f, precision * 22f), 0f, 100f);
            float moraleEstimate = EconomyCalculator.Clamp(avgMorale + RandomRange(-precision * 18f, precision * 18f), 0f, 100f);

            return
$@"Отримано звіт агентури щодо армії цілі.
Оцінка сили: ~{Math.Round(reconPower, MidpointRounding.AwayFromZero).ToString("N0", CultureInfo.InvariantCulture)}.
Оцінка чисельності: ~{reconTroops.ToString("N0", CultureInfo.InvariantCulture)}.
Підготовка/мораль (оцінка): {trainingEstimate:0.#}% / {moraleEstimate:0.#}%.
Виявлено діючих частин: {affectedUnits.ToString("N0", CultureInfo.InvariantCulture)}.";
        }

        private static ChanceSnapshot ComputeChances(CountryRecord source, CountryRecord target, string operationType, long budget)
        {
            float budgetFactor = EconomyCalculator.Clamp(
                (float)Math.Log10(1d + budget / 250d) / 2.6f,
                0f,
                1f);
            float baseSuccess = operationType switch
            {
                "ScoutForces" => 0.62f,
                "StealTreasury" => 0.47f,
                "SabotageLogistics" => 0.41f,
                "CounterintelRaid" => 0.52f,
                _ => 0.5f
            };

            float baseDetection = operationType switch
            {
                "ScoutForces" => 0.30f,
                "StealTreasury" => 0.42f,
                "SabotageLogistics" => 0.58f,
                "CounterintelRaid" => 0.48f,
                _ => 0.45f
            };

            var relation = DiplomacyManager.GetRelation(source.CountryId, target.CountryId);
            bool atWar = relation.IsAtWar || relation.State == DiplomaticState.War;
            float warBonus = atWar ? 0.05f : 0f;
            float alliancePenalty = relation.IsAlliance ? 0.1f : 0f;
            float hostileBonus = relation.State == DiplomaticState.Hostile ? 0.03f : 0f;

            float intelAdvantage = EconomyCalculator.Clamp(
                (source.IntelLevel - target.CounterIntelLevel) / 100f,
                -1f,
                1f);
            float counterAdvantage = EconomyCalculator.Clamp(
                (source.CounterIntelLevel - target.IntelLevel) / 100f,
                -1f,
                1f);
            float corruptionPenalty = EconomyCalculator.Clamp(source.Corruption / 100f, 0f, 1f) * 0.18f;

            float successChance = baseSuccess
                + intelAdvantage * 0.28f
                + budgetFactor * 0.18f
                + warBonus
                + hostileBonus
                - alliancePenalty
                - corruptionPenalty;

            if (operationType == "CounterintelRaid")
                successChance += counterAdvantage * 0.14f;

            successChance = EconomyCalculator.Clamp(successChance, 0.08f, 0.95f);

            float detectionChance = baseDetection
                + EconomyCalculator.Clamp(target.CounterIntelLevel / 100f, 0f, 1f) * 0.36f
                - EconomyCalculator.Clamp(source.IntelLevel / 100f, 0f, 1f) * 0.22f
                - budgetFactor * 0.05f;

            if (relation.IsAlliance || relation.State == DiplomaticState.Friendly)
                detectionChance += 0.04f;

            detectionChance = EconomyCalculator.Clamp(detectionChance, 0.05f, 0.97f);
            return new ChanceSnapshot
            {
                SuccessChance = successChance,
                DetectionChance = detectionChance,
                IntelligenceAdvantage = intelAdvantage,
                BudgetFactor = budgetFactor
            };
        }

        private static string ApplyDetectionDiplomacyPenalty(string operationType, int sourceCountryId, int targetCountryId, long userId)
        {
            if (NormalizeOperationType(operationType) == "ScoutForces")
                return string.Empty;

            var relation = DiplomacyManager.GetRelation(sourceCountryId, targetCountryId);
            DiplomaticState next = StepWorse(relation.State);
            if (next == relation.State)
                return string.Empty;

            DiplomacyManager.SetState(sourceCountryId, targetCountryId, next, userId);
            return $"Дипломатичний інцидент: відносини погіршилися ({FormatDiplomaticStateUk(relation.State)} → {FormatDiplomaticStateUk(next)}).";
        }

        private static DiplomaticState StepWorse(DiplomaticState state)
        {
            return state switch
            {
                DiplomaticState.Alliance => DiplomaticState.Friendly,
                DiplomaticState.Friendly => DiplomaticState.Tense,
                DiplomaticState.Neutral => DiplomaticState.Tense,
                DiplomaticState.Tense => DiplomaticState.Hostile,
                DiplomaticState.Hostile => DiplomaticState.Hostile,
                DiplomaticState.War => DiplomaticState.War,
                _ => state
            };
        }

        private static string FormatDiplomaticStateUk(DiplomaticState state)
        {
            return state switch
            {
                DiplomaticState.Alliance => "Союз",
                DiplomaticState.Friendly => "Дружні",
                DiplomaticState.Neutral => "Нейтральні",
                DiplomaticState.Tense => "Напружені",
                DiplomaticState.Hostile => "Ворожі",
                DiplomaticState.War => "Війна",
                _ => "Нейтральні"
            };
        }

        private static bool IsDestroyedStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return false;
            string normalized = status.Trim();
            return normalized.Equals("Destroyed", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Знищена", StringComparison.OrdinalIgnoreCase);
        }

        private static long NormalizeBudget(long budget)
        {
            if (budget < 100)
                return 100;
            if (budget > 100_000)
                return 100_000;
            return budget;
        }

        private static float RandomRange(float min, float max)
        {
            if (max <= min)
                return min;
            return min + (max - min) * Random.Shared.NextSingle();
        }
    }
}
