#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using DNDBot.DataBase;

namespace DNDBot.Goverment.Economy
{
    public sealed class MilitaryBattleRequest
    {
        public int AttackerCountryId { get; set; }
        public int DefenderCountryId { get; set; }
        public string Terrain { get; set; } = "Plains";
        public string AttackerTactic { get; set; } = "Balanced";
        public bool IncludeAttackerVassals { get; set; } = true;
        public bool IncludeDefenderVassals { get; set; } = true;
    }

    public sealed class MilitaryBattleResult
    {
        public bool Success { get; set; }
        public string Error { get; set; } = string.Empty;
        public string ResultType { get; set; } = "Battle";
        public int AttackerCountryId { get; set; }
        public int DefenderCountryId { get; set; }
        public string Terrain { get; set; } = "Plains";
        public string AttackerTactic { get; set; } = "Balanced";
        public string DefenderTactic { get; set; } = "Balanced";
        public int? WinnerCountryId { get; set; }
        public float AttackerPower { get; set; }
        public float DefenderPower { get; set; }
        public int AttackerTroopsStart { get; set; }
        public int DefenderTroopsStart { get; set; }
        public int AttackerLosses { get; set; }
        public int DefenderLosses { get; set; }
        public float AttackerMoraleDelta { get; set; }
        public float DefenderMoraleDelta { get; set; }
        public float AttackerExperienceGain { get; set; }
        public float DefenderExperienceGain { get; set; }
        public int AttackerVassalLosses { get; set; }
        public int DefenderVassalLosses { get; set; }
        public int BattleLogId { get; set; }
        public string Summary { get; set; } = string.Empty;
    }

    public sealed class MilitaryForceEstimate
    {
        public bool Exists { get; set; }
        public int CountryId { get; set; }
        public int ActiveUnits { get; set; }
        public int OwnTroops { get; set; }
        public int VassalTroops { get; set; }
        public int TotalTroops => Math.Max(0, OwnTroops) + Math.Max(0, VassalTroops);
        public float Power { get; set; }
        public float CombatEffectiveness { get; set; }
        public float AverageExperience { get; set; }
    }

    public static class MilitaryBattleService
    {
        private sealed class CountryBattleContext
        {
            public CountryRecord Country { get; init; } = null!;
            public List<MilitaryUnitRecord> Units { get; init; } = new();
            public List<VassalMilitaryObligationRecord> Obligations { get; init; } = new();
            public SocioMilitarySnapshot Mechanics { get; init; } = new();
            public int ActiveUnits { get; init; }
            public int OwnTroops { get; init; }
            public int VassalTroops { get; init; }
        }

        private sealed class SidePowerSnapshot
        {
            public float Power { get; init; }
            public int OwnTroops { get; init; }
            public int VassalTroops { get; init; }
            public int TotalTroops => Math.Max(0, OwnTroops) + Math.Max(0, VassalTroops);
            public float AverageExperience { get; init; }
        }

        private static readonly string[] TerrainKeysInternal =
        {
            "Plains", "Forest", "Hills", "Mountains", "Swamp", "Urban", "Coastal", "Sea"
        };

        private static readonly string[] TacticKeysInternal =
        {
            "Balanced", "Aggressive", "Defensive", "Skirmish", "Siege"
        };

        private static readonly Dictionary<string, float> UnitTypeBaseMultiplier = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Infantry"] = 1.00f,
            ["Spearmen"] = 1.05f,
            ["Archers"] = 1.10f,
            ["Crossbowmen"] = 1.14f,
            ["Cavalry"] = 1.24f,
            ["HeavyInfantry"] = 1.18f,
            ["MageCorps"] = 1.35f,
            ["Siege"] = 1.22f,
            ["Scouts"] = 0.92f,
            ["NavalFleet"] = 1.28f,
            ["Support"] = 0.88f
        };

        private static readonly Dictionary<string, float> UnitLevelMultiplier = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Squad"] = 0.85f,
            ["Platoon"] = 0.92f,
            ["Company"] = 1.00f,
            ["Battalion"] = 1.08f,
            ["Regiment"] = 1.14f,
            ["Brigade"] = 1.20f,
            ["Division"] = 1.27f,
            ["Legion"] = 1.33f,
            ["Garrison"] = 0.95f,
            ["Fleet"] = 1.22f
        };

        private static readonly Dictionary<string, float> RoleMultiplier = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Field"] = 1.00f,
            ["Garrison"] = 0.96f,
            ["Vanguard"] = 1.08f,
            ["Reserve"] = 0.88f,
            ["SiegeTrain"] = 1.06f,
            ["Fleet"] = 1.12f
        };

        private static readonly Dictionary<string, float> EquipmentTierMultiplier = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Militia"] = 0.78f,
            ["Standard"] = 1.00f,
            ["Heavy"] = 1.15f,
            ["Elite"] = 1.28f,
            ["Arcane"] = 1.35f,
            ["Naval"] = 1.18f
        };

        public static IReadOnlyList<string> TerrainKeys => TerrainKeysInternal;
        public static IReadOnlyList<string> TacticKeys => TacticKeysInternal;

        public static string NormalizeTerrain(string? terrain)
        {
            if (string.IsNullOrWhiteSpace(terrain))
                return "Plains";

            string value = terrain.Trim().ToLowerInvariant();
            return value switch
            {
                "plains" or "рівнина" or "равнина" => "Plains",
                "forest" or "ліс" or "лес" => "Forest",
                "hills" or "пагорби" or "холмы" => "Hills",
                "mountains" or "гори" or "горы" => "Mountains",
                "swamp" or "болото" or "болота" => "Swamp",
                "urban" or "місто" or "город" => "Urban",
                "coastal" or "узбережжя" or "побережье" => "Coastal",
                "sea" or "море" => "Sea",
                _ => terrain.Trim()
            };
        }

        public static string NormalizeTactic(string? tactic)
        {
            if (string.IsNullOrWhiteSpace(tactic))
                return "Balanced";

            string value = tactic.Trim().ToLowerInvariant();
            return value switch
            {
                "balanced" or "збалансована" or "сбалансированная" => "Balanced",
                "aggressive" or "агресивна" or "агрессивная" => "Aggressive",
                "defensive" or "оборонна" or "оборонительная" => "Defensive",
                "skirmish" or "маневрена" or "маневренная" => "Skirmish",
                "siege" or "облога" or "осада" => "Siege",
                _ => tactic.Trim()
            };
        }

        public static string FormatTerrainUk(string? terrain)
        {
            return NormalizeTerrain(terrain) switch
            {
                "Plains" => "Рівнина",
                "Forest" => "Ліс",
                "Hills" => "Пагорби",
                "Mountains" => "Гори",
                "Swamp" => "Болото",
                "Urban" => "Місто",
                "Coastal" => "Узбережжя",
                "Sea" => "Море",
                _ => terrain ?? "Рівнина"
            };
        }

        public static string FormatTacticUk(string? tactic)
        {
            return NormalizeTactic(tactic) switch
            {
                "Balanced" => "Збалансована",
                "Aggressive" => "Агресивна",
                "Defensive" => "Оборонна",
                "Skirmish" => "Маневрена",
                "Siege" => "Облога",
                _ => tactic ?? "Збалансована"
            };
        }

        public static string FormatResultTypeUk(string? resultType)
        {
            if (string.IsNullOrWhiteSpace(resultType))
                return "Бій";
            return resultType.Trim().ToLowerInvariant() switch
            {
                "retreat" => "Відступ",
                "battle" => "Бій",
                _ => resultType.Trim()
            };
        }

        public static float EstimateUnitPower(MilitaryUnitRecord unit, string terrain, bool defending)
        {
            if (unit == null)
                return 0f;

            string status = NormalizeUnitStatus(unit.Status);
            if (string.Equals(status, "Destroyed", StringComparison.OrdinalIgnoreCase))
                return 0f;

            int soldiers = Math.Max(0, unit.Soldiers);
            if (soldiers <= 0)
                return 0f;

            string level = NormalizeUnitLevel(unit.UnitLevel);
            string role = NormalizeUnitRole(unit.UnitRole);
            string type = NormalizeUnitType(unit.UnitType);
            string equipmentTier = NormalizeEquipmentTier(unit.EquipmentTier);

            float levelMult = UnitLevelMultiplier.TryGetValue(level, out var l) ? l : 1f;
            float roleMult = RoleMultiplier.TryGetValue(role, out var r) ? r : 1f;
            float typeMult = UnitTypeBaseMultiplier.TryGetValue(type, out var t) ? t : 1f;
            float equipmentTierMult = EquipmentTierMultiplier.TryGetValue(equipmentTier, out var e) ? e : 1f;

            float readiness = GetStatusReadiness(unit);
            float training = EconomyCalculator.Clamp(unit.TrainingLevel, 0f, 100f) / 100f;
            float morale = EconomyCalculator.Clamp(unit.Morale, 0f, 100f) / 100f;
            float weapon = EconomyCalculator.Clamp(unit.WeaponReadiness, 0f, 100f) / 100f;
            float supply = EconomyCalculator.Clamp(unit.SupplyLevel, 0f, 100f) / 100f;
            float discipline = 0.5f + EconomyCalculator.Clamp(unit.CommanderLoyalty, 0f, 100f) / 200f;
            float command = (EconomyCalculator.Clamp(unit.CommanderSkill, 0f, 100f) * 0.58f
                + EconomyCalculator.Clamp(unit.CommanderExperience, 0f, 100f) * 0.42f) / 100f;
            float experience = EconomyCalculator.Clamp(unit.BattleExperience, 0f, 100f) / 100f;
            float fatiguePenalty = 1f - EconomyCalculator.Clamp(unit.Fatigue, 0f, 100f) / 180f;
            float equipmentQualityFactor = 0.62f + EconomyCalculator.Clamp(unit.EquipmentQuality, 0f, 100f) / 100f * 0.78f;
            float experienceFactor = 0.72f + experience * 0.56f;

            float coreReadiness = training * 0.23f
                + morale * 0.22f
                + weapon * 0.21f
                + supply * 0.17f
                + command * 0.11f
                + discipline * 0.06f;
            coreReadiness = Math.Max(0.35f, coreReadiness);

            float terrainMod = GetTerrainModifier(type, role, NormalizeTerrain(terrain), defending);

            float power = soldiers
                * readiness
                * levelMult
                * roleMult
                * typeMult
                * equipmentTierMult
                * equipmentQualityFactor
                * experienceFactor
                * Math.Max(0.45f, fatiguePenalty)
                * coreReadiness
                * terrainMod;

            return Math.Max(0f, power);
        }

        public static MilitaryForceEstimate EstimateCountryPower(int countryId, string terrain, string tactic, bool attacking, bool includeVassals)
        {
            var context = BuildCountryContext(countryId, includeVassals);
            if (context == null)
                return new MilitaryForceEstimate { Exists = false, CountryId = countryId };

            string normalizedTerrain = NormalizeTerrain(terrain);
            string normalizedTactic = NormalizeTactic(tactic);
            var side = ComputeSidePower(context, normalizedTerrain, normalizedTactic, attacking, includeVassals);

            return new MilitaryForceEstimate
            {
                Exists = true,
                CountryId = context.Country.CountryId,
                ActiveUnits = context.ActiveUnits,
                OwnTroops = side.OwnTroops,
                VassalTroops = side.VassalTroops,
                Power = side.Power,
                CombatEffectiveness = context.Mechanics.CombatEffectiveness,
                AverageExperience = side.AverageExperience
            };
        }

        public static MilitaryBattleResult ResolveBattle(MilitaryBattleRequest request)
        {
            return Resolve(request, retreat: false);
        }

        public static MilitaryBattleResult ResolveRetreat(MilitaryBattleRequest request)
        {
            return Resolve(request, retreat: true);
        }

        private static MilitaryBattleResult Resolve(MilitaryBattleRequest request, bool retreat)
        {
            var result = new MilitaryBattleResult
            {
                Success = false,
                ResultType = retreat ? "Retreat" : "Battle",
                AttackerCountryId = request.AttackerCountryId,
                DefenderCountryId = request.DefenderCountryId,
                Terrain = NormalizeTerrain(request.Terrain),
                AttackerTactic = NormalizeTactic(request.AttackerTactic)
            };

            if (request.AttackerCountryId <= 0 || request.DefenderCountryId <= 0 || request.AttackerCountryId == request.DefenderCountryId)
            {
                result.Error = "Невірна пара країн для бойової операції.";
                return result;
            }

            var attacker = BuildCountryContext(request.AttackerCountryId, request.IncludeAttackerVassals);
            var defender = BuildCountryContext(request.DefenderCountryId, request.IncludeDefenderVassals);
            if (attacker == null || defender == null)
            {
                result.Error = "Не вдалося знайти дані однієї з країн.";
                return result;
            }

            string defenderTactic = retreat
                ? "Aggressive"
                : ChooseDefenderTactic(attacker, defender, result.Terrain);
            result.DefenderTactic = defenderTactic;

            var attackerSide = ComputeSidePower(attacker, result.Terrain, result.AttackerTactic, attacking: true, request.IncludeAttackerVassals);
            var defenderSide = ComputeSidePower(defender, result.Terrain, defenderTactic, attacking: false, request.IncludeDefenderVassals);
            if (attackerSide.TotalTroops <= 0 || defenderSide.TotalTroops <= 0)
            {
                result.Error = "Для однієї зі сторін немає боєздатних військ.";
                return result;
            }

            float attackerPower = attackerSide.Power * RandomFactor(0.92f, 1.08f);
            float defenderPower = defenderSide.Power * RandomFactor(0.92f, 1.08f);
            if (retreat)
                attackerPower *= 0.84f;

            attackerPower = Math.Max(1f, attackerPower);
            defenderPower = Math.Max(1f, defenderPower);

            result.AttackerPower = attackerPower;
            result.DefenderPower = defenderPower;
            result.AttackerTroopsStart = attackerSide.TotalTroops;
            result.DefenderTroopsStart = defenderSide.TotalTroops;

            float attackerLossRate;
            float defenderLossRate;
            float attackerMoraleDelta;
            float defenderMoraleDelta;
            float attackerExperienceGain;
            float defenderExperienceGain;
            int? winnerCountryId;

            if (retreat)
            {
                float pressure = defenderPower / Math.Max(1f, attackerPower);
                attackerLossRate = EconomyCalculator.Clamp(0.02f + Math.Max(0f, pressure - 0.8f) * 0.05f, 0.01f, 0.18f);
                defenderLossRate = EconomyCalculator.Clamp(attackerLossRate * 0.2f, 0f, 0.05f);
                attackerMoraleDelta = -8f - attackerLossRate * 18f;
                defenderMoraleDelta = 3f - defenderLossRate * 4f;
                attackerExperienceGain = 2.5f;
                defenderExperienceGain = 2f;
                winnerCountryId = defender.Country.CountryId;
            }
            else
            {
                float totalPower = attackerPower + defenderPower;
                float baseIntensity = 0.07f + RandomFactor(0f, 0.06f);
                attackerLossRate = baseIntensity
                    * (defenderPower / totalPower)
                    * GetTacticCasualtyMultiplier(result.AttackerTactic, attacking: true);
                defenderLossRate = baseIntensity
                    * (attackerPower / totalPower)
                    * GetTacticCasualtyMultiplier(defenderTactic, attacking: false);

                float ratio = attackerPower / Math.Max(1f, defenderPower);
                if (ratio >= 1.18f)
                {
                    attackerLossRate *= 0.78f;
                    defenderLossRate *= 1.45f;
                    attackerMoraleDelta = 8f - attackerLossRate * 12f;
                    defenderMoraleDelta = -13f - defenderLossRate * 14f;
                    attackerExperienceGain = 8f;
                    defenderExperienceGain = 4f;
                    winnerCountryId = attacker.Country.CountryId;
                }
                else if (ratio <= 0.85f)
                {
                    attackerLossRate *= 1.45f;
                    defenderLossRate *= 0.78f;
                    attackerMoraleDelta = -13f - attackerLossRate * 14f;
                    defenderMoraleDelta = 8f - defenderLossRate * 12f;
                    attackerExperienceGain = 4f;
                    defenderExperienceGain = 8f;
                    winnerCountryId = defender.Country.CountryId;
                }
                else
                {
                    attackerLossRate *= 1.1f;
                    defenderLossRate *= 1.1f;
                    attackerMoraleDelta = -2f - attackerLossRate * 10f;
                    defenderMoraleDelta = -2f - defenderLossRate * 10f;
                    attackerExperienceGain = 5.5f;
                    defenderExperienceGain = 5.5f;
                    winnerCountryId = null;
                }

                attackerLossRate = EconomyCalculator.Clamp(attackerLossRate, 0.01f, 0.4f);
                defenderLossRate = EconomyCalculator.Clamp(defenderLossRate, 0.01f, 0.4f);
            }

            int attackerLosses = Math.Min(attackerSide.TotalTroops, (int)Math.Round(attackerSide.TotalTroops * attackerLossRate, MidpointRounding.AwayFromZero));
            int defenderLosses = Math.Min(defenderSide.TotalTroops, (int)Math.Round(defenderSide.TotalTroops * defenderLossRate, MidpointRounding.AwayFromZero));
            if (attackerLosses < 0) attackerLosses = 0;
            if (defenderLosses < 0) defenderLosses = 0;

            int attackerVassalLosses = request.IncludeAttackerVassals && attackerSide.VassalTroops > 0
                ? Math.Min(attackerLosses, (int)Math.Round(attackerLosses * (attackerSide.VassalTroops / (float)Math.Max(1, attackerSide.TotalTroops)), MidpointRounding.AwayFromZero))
                : 0;
            int defenderVassalLosses = request.IncludeDefenderVassals && defenderSide.VassalTroops > 0
                ? Math.Min(defenderLosses, (int)Math.Round(defenderLosses * (defenderSide.VassalTroops / (float)Math.Max(1, defenderSide.TotalTroops)), MidpointRounding.AwayFromZero))
                : 0;

            int attackerOwnLosses = Math.Max(0, attackerLosses - attackerVassalLosses);
            int defenderOwnLosses = Math.Max(0, defenderLosses - defenderVassalLosses);

            ApplyUnitCasualties(attacker, attackerOwnLosses, attackerMoraleDelta, attackerExperienceGain, retreat);
            ApplyUnitCasualties(defender, defenderOwnLosses, defenderMoraleDelta, defenderExperienceGain, retreat: false);

            ApplyVassalLosses(attacker.Obligations, attackerVassalLosses);
            ApplyVassalLosses(defender.Obligations, defenderVassalLosses);

            ApplyCountryAftermath(attacker.Country, attackerOwnLosses, attackerMoraleDelta);
            ApplyCountryAftermath(defender.Country, defenderOwnLosses, defenderMoraleDelta);

            string attackerName = string.IsNullOrWhiteSpace(attacker.Country.Name) ? $"#{attacker.Country.CountryId}" : attacker.Country.Name;
            string defenderName = string.IsNullOrWhiteSpace(defender.Country.Name) ? $"#{defender.Country.CountryId}" : defender.Country.Name;
            string winnerText = winnerCountryId.HasValue
                ? (winnerCountryId.Value == attacker.Country.CountryId ? attackerName : defenderName)
                : "нічия";

            result.Success = true;
            result.WinnerCountryId = winnerCountryId;
            result.AttackerLosses = attackerLosses;
            result.DefenderLosses = defenderLosses;
            result.AttackerVassalLosses = attackerVassalLosses;
            result.DefenderVassalLosses = defenderVassalLosses;
            result.AttackerMoraleDelta = attackerMoraleDelta;
            result.DefenderMoraleDelta = defenderMoraleDelta;
            result.AttackerExperienceGain = attackerExperienceGain;
            result.DefenderExperienceGain = defenderExperienceGain;
            result.Summary = retreat
                ? $"{attackerName} організовано відступила з поля бою ({FormatTerrainUk(result.Terrain)}). Переслідування ворога принесло обмежені втрати."
                : $"Бій {attackerName} проти {defenderName} ({FormatTerrainUk(result.Terrain)}). Переможець: {winnerText}.";

            int logId = MilitaryBattleLogsData.Add(new MilitaryBattleLogRecord
            {
                AttackerCountryId = attacker.Country.CountryId,
                DefenderCountryId = defender.Country.CountryId,
                Terrain = result.Terrain,
                AttackerTactic = result.AttackerTactic,
                DefenderTactic = result.DefenderTactic,
                ResultType = result.ResultType,
                WinnerCountryId = result.WinnerCountryId,
                AttackerPower = result.AttackerPower,
                DefenderPower = result.DefenderPower,
                AttackerTroopsStart = result.AttackerTroopsStart,
                DefenderTroopsStart = result.DefenderTroopsStart,
                AttackerLosses = result.AttackerLosses,
                DefenderLosses = result.DefenderLosses,
                AttackerVassalLosses = result.AttackerVassalLosses,
                DefenderVassalLosses = result.DefenderVassalLosses,
                AttackerMoraleDelta = result.AttackerMoraleDelta,
                DefenderMoraleDelta = result.DefenderMoraleDelta,
                AttackerExperienceGain = result.AttackerExperienceGain,
                DefenderExperienceGain = result.DefenderExperienceGain,
                Summary = result.Summary,
                CreatedAt = DateTime.UtcNow
            });
            result.BattleLogId = logId;

            return result;
        }

        private static CountryBattleContext? BuildCountryContext(int countryId, bool includeVassals)
        {
            var country = CountriesData.GetCountryById(countryId);
            if (country == null)
                return null;

            var units = MilitaryUnitsData.GetByCountry(countryId)
                .Where(u => Math.Max(0, u.Soldiers) > 0 && !string.Equals(NormalizeUnitStatus(u.Status), "Destroyed", StringComparison.OrdinalIgnoreCase))
                .ToList();
            var obligations = VassalMilitaryObligationsData.GetByCountry(countryId)
                .Where(o => o.IsActive && Math.Max(0, o.RegimentsProvided) > 0)
                .ToList();
            var buildings = EconomyBuildingsData.GetByCountry(countryId);
            long totalJobs = buildings.Sum(b => Math.Max(0, b.JobsProvided));
            float unemploymentRate = EconomyCalculator.Clamp(country.UnemploymentRate / 100f, 0f, 1f);

            var mechanics = SocioMilitaryMechanics.Build(
                country,
                buildings,
                totalJobs,
                unemploymentRate,
                country.MilitaryUpkeep,
                units,
                obligations);

            int ownTroops = 0;
            foreach (var unit in units)
            {
                float readiness = GetStatusReadiness(unit);
                ownTroops += Math.Max(0, (int)Math.Round(Math.Max(0, unit.Soldiers) * readiness, MidpointRounding.AwayFromZero));
            }

            if (ownTroops <= 0)
                ownTroops = Math.Max(0, country.ArmySize);

            int vassalTroops = includeVassals ? Math.Max(0, mechanics.VassalTroops) : 0;
            int activeUnits = units.Count(u => GetStatusReadiness(u) >= 0.3f);

            return new CountryBattleContext
            {
                Country = country,
                Units = units,
                Obligations = obligations,
                Mechanics = mechanics,
                ActiveUnits = activeUnits,
                OwnTroops = ownTroops,
                VassalTroops = vassalTroops
            };
        }

        private static SidePowerSnapshot ComputeSidePower(
            CountryBattleContext context,
            string terrain,
            string tactic,
            bool attacking,
            bool includeVassals)
        {
            float unitPower = 0f;
            float weightedExp = 0f;
            float weightSum = 0f;
            int ownTroops = context.OwnTroops;

            foreach (var unit in context.Units)
            {
                float readiness = GetStatusReadiness(unit);
                if (readiness <= 0f)
                    continue;

                float power = EstimateUnitPower(unit, terrain, defending: !attacking);
                unitPower += power;

                float weight = Math.Max(1f, unit.Soldiers * readiness);
                weightedExp += EconomyCalculator.Clamp(unit.BattleExperience, 0f, 100f) * weight;
                weightSum += weight;
            }

            if (unitPower <= 0f)
            {
                float fallbackCombat = EconomyCalculator.Clamp(context.Mechanics.CombatEffectiveness, 1f, 100f) / 100f;
                unitPower = ownTroops * (0.35f + fallbackCombat * 0.65f);
            }

            float combat = EconomyCalculator.Clamp(context.Mechanics.CombatEffectiveness / 100f, 0.05f, 1f);
            float morale = EconomyCalculator.Clamp(context.Country.ArmyMorale / 100f, 0.05f, 1f);
            float command = EconomyCalculator.Clamp(
                (context.Mechanics.CommanderSkill * 0.45f
                + context.Mechanics.CommanderExperience * 0.35f
                + context.Mechanics.CommanderLoyalty * 0.2f) / 100f,
                0.1f,
                1f);
            float supply = EconomyCalculator.Clamp((context.Mechanics.SupplyCoverage + context.Mechanics.AmmunitionIssued) * 0.5f, 0.1f, 1f);

            float strategicMultiplier = 0.48f + combat * 0.24f + morale * 0.15f + command * 0.08f + supply * 0.05f;
            float tacticMultiplier = GetTacticPowerMultiplier(tactic, attacking);
            float totalPower = unitPower * strategicMultiplier * tacticMultiplier;

            int vassalTroops = includeVassals ? context.VassalTroops : 0;
            if (vassalTroops > 0)
            {
                float avgLoyalty = context.Obligations.Count == 0
                    ? 55f
                    : context.Obligations.Average(o => EconomyCalculator.Clamp(o.Loyalty, 0f, 100f));
                float avgRebellionRisk = context.Obligations.Count == 0
                    ? 0f
                    : context.Obligations.Average(o => EconomyCalculator.Clamp(o.RebellionRisk, 0f, 100f));
                float vassalQuality = EconomyCalculator.Clamp(0.45f + avgLoyalty / 100f * 0.45f - avgRebellionRisk / 100f * 0.2f, 0.2f, 1f);
                totalPower += vassalTroops * (0.35f + combat * 0.45f) * vassalQuality;
            }

            if (context.Country.WarState)
                totalPower *= 1.03f;
            if (context.Country.MartialLaw)
                totalPower *= 0.98f;

            float avgExperience = weightSum <= 0f
                ? EconomyCalculator.Clamp(context.Mechanics.CommanderExperience * 0.6f, 0f, 100f)
                : EconomyCalculator.Clamp(weightedExp / weightSum, 0f, 100f);

            return new SidePowerSnapshot
            {
                Power = Math.Max(1f, totalPower),
                OwnTroops = Math.Max(0, ownTroops),
                VassalTroops = Math.Max(0, vassalTroops),
                AverageExperience = avgExperience
            };
        }

        private static void ApplyUnitCasualties(
            CountryBattleContext context,
            int ownLosses,
            float moraleDelta,
            float experienceGain,
            bool retreat)
        {
            if (context.Units.Count == 0)
                return;

            var participants = context.Units
                .Where(u => GetStatusReadiness(u) > 0.05f && Math.Max(0, u.Soldiers) > 0)
                .ToList();
            if (participants.Count == 0)
                return;

            int safeOwnTroops = Math.Max(1, context.OwnTroops);
            float lossPressure = ownLosses <= 0 ? 0f : EconomyCalculator.Clamp(ownLosses / (float)safeOwnTroops, 0f, 1f);

            int remainingLosses = Math.Max(0, ownLosses);
            int totalWeight = participants.Sum(u => Math.Max(1, u.Soldiers));
            int totalParticipants = participants.Count;

            for (int i = 0; i < totalParticipants; i++)
            {
                var unit = participants[i];
                int before = Math.Max(0, unit.Soldiers);
                int allocatedLoss;
                if (remainingLosses <= 0)
                {
                    allocatedLoss = 0;
                }
                else if (i == totalParticipants - 1)
                {
                    allocatedLoss = Math.Min(before, remainingLosses);
                }
                else
                {
                    float share = before / (float)Math.Max(1, totalWeight);
                    allocatedLoss = Math.Min(before, (int)Math.Round(ownLosses * share, MidpointRounding.AwayFromZero));
                    if (allocatedLoss > remainingLosses)
                        allocatedLoss = remainingLosses;
                }

                remainingLosses -= allocatedLoss;
                unit.Soldiers = Math.Max(0, before - allocatedLoss);

                float localMoraleJitter = RandomFactor(-2f, 2f);
                float localMoraleDelta = moraleDelta + localMoraleJitter;
                if (retreat)
                    localMoraleDelta -= 2f;

                unit.Morale = EconomyCalculator.Clamp(unit.Morale + localMoraleDelta, 0f, 100f);
                unit.BattleExperience = EconomyCalculator.Clamp(unit.BattleExperience + experienceGain + lossPressure * 8f, 0f, 100f);
                unit.CommanderExperience = EconomyCalculator.Clamp(unit.CommanderExperience + experienceGain * 0.35f, 0f, 100f);
                unit.WeaponReadiness = EconomyCalculator.Clamp(unit.WeaponReadiness - 2f - lossPressure * 10f, 0f, 100f);
                unit.SupplyLevel = EconomyCalculator.Clamp(unit.SupplyLevel - 3f - lossPressure * 12f, 0f, 100f);
                unit.Fatigue = EconomyCalculator.Clamp(unit.Fatigue + (retreat ? 12f : 8f) + lossPressure * 20f, 0f, 100f);
                unit.TrainingLevel = EconomyCalculator.Clamp(unit.TrainingLevel - lossPressure * 6f + experienceGain * 0.15f, 0f, 100f);

                if (unit.Soldiers <= 0)
                {
                    unit.Status = "Destroyed";
                    unit.IsMobilized = false;
                    unit.FormationProgress = 0f;
                    unit.Morale = 0f;
                }
                else if (unit.Morale < 20f && !string.Equals(NormalizeUnitStatus(unit.Status), "Forming", StringComparison.OrdinalIgnoreCase))
                {
                    unit.Status = "Routed";
                }
                else if (string.Equals(NormalizeUnitStatus(unit.Status), "Routed", StringComparison.OrdinalIgnoreCase) && unit.Morale >= 35f)
                {
                    unit.Status = "Reserve";
                }

                unit.UpdatedAt = DateTime.UtcNow;
                MilitaryUnitsData.UpdateUnit(unit);
            }
        }

        private static void ApplyVassalLosses(List<VassalMilitaryObligationRecord> obligations, int vassalLosses)
        {
            if (obligations == null || obligations.Count == 0 || vassalLosses <= 0)
                return;

            int regimentSize = Math.Max(500, EconomyConfigData.GetInt("VassalRegimentSize", 2500));
            int regimentsLost = (int)Math.Ceiling(vassalLosses / (double)regimentSize);
            if (regimentsLost <= 0)
                return;

            int remaining = regimentsLost;
            var ordered = obligations
                .Where(o => o.IsActive && o.RegimentsProvided > 0)
                .OrderByDescending(o => o.RegimentsProvided)
                .ThenBy(o => o.ObligationId)
                .ToList();

            foreach (var obligation in ordered)
            {
                if (remaining <= 0)
                    break;

                int loss = Math.Min(Math.Max(0, obligation.RegimentsProvided), remaining);
                if (loss <= 0)
                    continue;

                obligation.RegimentsProvided = Math.Max(0, obligation.RegimentsProvided - loss);
                obligation.Loyalty = EconomyCalculator.Clamp(obligation.Loyalty - loss * 2f, 0f, 100f);
                obligation.RebellionRisk = EconomyCalculator.Clamp(obligation.RebellionRisk + loss * 1.5f, 0f, 100f);
                obligation.LastUpdated = DateTime.UtcNow;

                VassalMilitaryObligationsData.Update(obligation);
                remaining -= loss;
            }
        }

        private static void ApplyCountryAftermath(CountryRecord country, int ownLosses, float moraleDelta)
        {
            if (country == null)
                return;

            int beforeArmy = Math.Max(0, country.ArmySize);
            country.ArmySize = Math.Max(0, country.ArmySize - Math.Max(0, ownLosses));
            country.Population = Math.Max(0L, country.Population - Math.Max(0, ownLosses));
            country.ArmyMorale = EconomyCalculator.Clamp(country.ArmyMorale + moraleDelta, 0f, 100f);

            float casualtyShare = beforeArmy <= 0 ? 0f : Math.Min(1f, ownLosses / (float)Math.Max(1, beforeArmy));
            float loyaltyDelta = (moraleDelta >= 0f ? 1.2f : -2.1f) - casualtyShare * 14f;
            country.ArmyLoyalty = EconomyCalculator.Clamp(country.ArmyLoyalty + loyaltyDelta, 0f, 100f);
            country.CoupRisk = EconomyCalculator.Clamp(country.CoupRisk + (moraleDelta < 0f ? 1.5f : -0.7f) + casualtyShare * 6f, 0f, 100f);
            country.LastUpdate = DateTime.UtcNow;

            CountriesData.UpdateCountry(country);
        }

        private static string ChooseDefenderTactic(CountryBattleContext attacker, CountryBattleContext defender, string terrain)
        {
            float attackerCombat = EconomyCalculator.Clamp(attacker.Mechanics.CombatEffectiveness, 1f, 100f);
            float defenderCombat = EconomyCalculator.Clamp(defender.Mechanics.CombatEffectiveness, 1f, 100f);
            float ratio = defenderCombat / Math.Max(1f, attackerCombat);

            if (string.Equals(terrain, "Urban", StringComparison.OrdinalIgnoreCase))
                return ratio >= 1.05f ? "Defensive" : "Skirmish";
            if (string.Equals(terrain, "Sea", StringComparison.OrdinalIgnoreCase) || string.Equals(terrain, "Coastal", StringComparison.OrdinalIgnoreCase))
                return ratio >= 0.95f ? "Aggressive" : "Balanced";
            if (ratio < 0.85f)
                return "Defensive";
            if (ratio > 1.2f)
                return "Aggressive";
            return "Balanced";
        }

        private static float GetTerrainModifier(string unitType, string role, string terrain, bool defending)
        {
            float modifier = 1f;

            switch (terrain)
            {
                case "Plains":
                    if (unitType == "Cavalry")
                        modifier += 0.18f;
                    if (unitType == "Siege")
                        modifier += 0.04f;
                    break;
                case "Forest":
                    if (unitType == "Archers" || unitType == "Crossbowmen")
                        modifier += 0.14f;
                    if (unitType == "Scouts")
                        modifier += 0.16f;
                    if (unitType == "Cavalry")
                        modifier -= 0.2f;
                    if (unitType == "Siege")
                        modifier -= 0.1f;
                    break;
                case "Hills":
                    if (unitType == "Archers" || unitType == "Crossbowmen")
                        modifier += 0.12f;
                    if (unitType == "Siege")
                        modifier += 0.08f;
                    if (unitType == "Cavalry")
                        modifier -= 0.12f;
                    break;
                case "Mountains":
                    if (unitType == "Infantry" || unitType == "Spearmen")
                        modifier += 0.06f;
                    if (unitType == "MageCorps")
                        modifier += 0.1f;
                    if (unitType == "Cavalry")
                        modifier -= 0.25f;
                    if (unitType == "HeavyInfantry")
                        modifier -= 0.1f;
                    break;
                case "Swamp":
                    if (unitType == "Cavalry")
                        modifier -= 0.3f;
                    if (unitType == "HeavyInfantry")
                        modifier -= 0.18f;
                    if (unitType == "Scouts")
                        modifier += 0.08f;
                    if (unitType == "MageCorps")
                        modifier += 0.06f;
                    break;
                case "Urban":
                    if (unitType == "Siege")
                        modifier += defending ? -0.08f : 0.15f;
                    if (unitType == "Cavalry")
                        modifier -= 0.15f;
                    if (role == "Garrison" && defending)
                        modifier += 0.25f;
                    break;
                case "Coastal":
                    if (unitType == "NavalFleet")
                        modifier += 0.2f;
                    if (unitType == "Archers" || unitType == "Crossbowmen")
                        modifier += 0.06f;
                    break;
                case "Sea":
                    if (unitType == "NavalFleet")
                        modifier += 0.45f;
                    else if (unitType == "MageCorps")
                        modifier -= 0.2f;
                    else
                        modifier -= 0.55f;
                    break;
            }

            if (role == "Fleet")
            {
                if (terrain == "Sea" || terrain == "Coastal")
                    modifier += 0.14f;
                else
                    modifier -= 0.45f;
            }

            if (defending && role == "Garrison")
                modifier += 0.05f;
            if (defending && terrain == "Hills")
                modifier += 0.03f;

            return EconomyCalculator.Clamp(modifier, 0.25f, 1.65f);
        }

        private static float GetTacticPowerMultiplier(string tactic, bool attacking)
        {
            return NormalizeTactic(tactic) switch
            {
                "Aggressive" => attacking ? 1.16f : 1.08f,
                "Defensive" => attacking ? 0.93f : 1.15f,
                "Skirmish" => attacking ? 1.05f : 1.01f,
                "Siege" => attacking ? 1.12f : 0.96f,
                _ => 1f
            };
        }

        private static float GetTacticCasualtyMultiplier(string tactic, bool attacking)
        {
            return NormalizeTactic(tactic) switch
            {
                "Aggressive" => attacking ? 1.35f : 1.22f,
                "Defensive" => attacking ? 0.86f : 0.78f,
                "Skirmish" => attacking ? 0.92f : 0.9f,
                "Siege" => attacking ? 1.2f : 0.95f,
                _ => 1f
            };
        }

        private static float GetStatusReadiness(MilitaryUnitRecord unit)
        {
            string status = NormalizeUnitStatus(unit.Status);
            return status switch
            {
                "Destroyed" => 0f,
                "Routed" => 0.45f,
                "Forming" => EconomyCalculator.Clamp(0.25f + EconomyCalculator.Clamp(unit.FormationProgress, 0f, 1f) * 0.75f, 0.25f, 1f),
                "Reserve" => 0.75f,
                _ => 1f
            };
        }

        private static string NormalizeUnitStatus(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "Forming";

            string value = raw.Trim().ToLowerInvariant();
            return value switch
            {
                "forming" or "формується" => "Forming",
                "active" or "активна" => "Active",
                "reserve" or "резерв" => "Reserve",
                "routed" or "розбита" or "відступає" => "Routed",
                "destroyed" or "знищена" => "Destroyed",
                _ => raw.Trim()
            };
        }

        private static string NormalizeUnitLevel(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "Squad";

            string value = raw.Trim().ToLowerInvariant();
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
                _ => raw.Trim()
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

        private static string NormalizeUnitType(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "Infantry";

            string value = raw.Trim().ToLowerInvariant();
            return value switch
            {
                "infantry" or "піхота" => "Infantry",
                "spearmen" or "копійники" => "Spearmen",
                "archers" or "лучники" => "Archers",
                "crossbowmen" or "арбалетники" => "Crossbowmen",
                "cavalry" or "кавалерія" => "Cavalry",
                "heavyinfantry" or "heavy" or "важка піхота" => "HeavyInfantry",
                "magecorps" or "mages" or "маги" => "MageCorps",
                "siege" or "облогові" => "Siege",
                "scouts" or "розвідники" => "Scouts",
                "navalfleet" or "fleet" or "флот" => "NavalFleet",
                "support" or "підтримка" => "Support",
                _ => raw.Trim()
            };
        }

        private static float RandomFactor(float min, float max)
        {
            if (max <= min)
                return min;
            return min + (max - min) * Random.Shared.NextSingle();
        }
    }
}
