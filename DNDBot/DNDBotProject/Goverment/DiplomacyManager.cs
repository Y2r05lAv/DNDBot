using System;
using System.Collections.Generic;
using System.Linq;
using DNDBot.DataBase;

namespace DNDBot.Goverment
{
    public enum DiplomaticState
    {
        Alliance = 0,
        Friendly = 1,
        Neutral = 2,
        Tense = 3,
        Hostile = 4,
        War = 5
    }

    public enum DiplomaticAction
    {
        DeclareWar,
        MakePeace,
        FormAlliance,
        BreakAlliance,
        ApplySanctions,
        LiftSanctions,
        SignTradeAgreement,
        BreakTradeAgreement,
        ImposeEmbargo,
        LiftEmbargo
    }

    public sealed class DiplomaticRelation
    {
        public int CountryA { get; set; }
        public int CountryB { get; set; }
        public DiplomaticState State { get; set; }
        public bool IsAlliance { get; set; }
        public bool IsAtWar { get; set; }
        public bool HasSanctions { get; set; }
        public bool HasEmbargo { get; set; }
        public bool HasTradeAgreement { get; set; }
    }

    public sealed class DiplomacyModifiers
    {
        public float ProductionModifier { get; set; }
        public float IncomeModifier { get; set; }
        public float TradeModifier { get; set; }
        public float ExpenseModifier { get; set; }
        public float UnemploymentDelta { get; set; }
        public float MortalityRateDelta { get; set; }
        public float HappinessDelta { get; set; }
        public float BlackMarketRiskDelta { get; set; }
        public float InflationDelta { get; set; }
    }

    public static class DiplomacyManager
    {
        private static readonly Random Rng = Random.Shared;

        private static (int A, int B) Normalize(int a, int b)
        {
            return a < b ? (a, b) : (b, a);
        }

        public static DiplomaticRelation GetRelation(int countryId, int targetCountryId)
        {
            var (a, b) = Normalize(countryId, targetCountryId);
            var record = DiplomacyRelationsData.GetRelation(a, b);
            if (record == null)
            {
                return new DiplomaticRelation
                {
                    CountryA = a,
                    CountryB = b,
                    State = DiplomaticState.Neutral,
                    IsAlliance = false,
                    IsAtWar = false,
                    HasSanctions = false,
                    HasEmbargo = false,
                    HasTradeAgreement = false
                };
            }

            return new DiplomaticRelation
            {
                CountryA = record.CountryA,
                CountryB = record.CountryB,
                State = (DiplomaticState)record.State,
                IsAlliance = record.IsAlliance,
                IsAtWar = record.IsAtWar,
                HasSanctions = record.HasSanctions,
                HasEmbargo = record.HasEmbargo,
                HasTradeAgreement = record.HasTradeAgreement
            };
        }

        public static List<DiplomaticRelation> GetRelationsForCountry(int countryId)
        {
            var list = DiplomacyRelationsData.GetByCountry(countryId);
            return list.Select(r => new DiplomaticRelation
            {
                CountryA = r.CountryA,
                CountryB = r.CountryB,
                State = (DiplomaticState)r.State,
                IsAlliance = r.IsAlliance,
                IsAtWar = r.IsAtWar,
                HasSanctions = r.HasSanctions,
                HasEmbargo = r.HasEmbargo,
                HasTradeAgreement = r.HasTradeAgreement
            }).ToList();
        }

        public static DiplomacyModifiers GetCountryModifiers(int countryId)
        {
            var relations = GetRelationsForCountry(countryId);
            var mods = new DiplomacyModifiers();

            int alliances = 0;
            bool atWar = false;

            foreach (var rel in relations)
            {
                var state = rel.State;
                if (rel.IsAtWar || state == DiplomaticState.War)
                    atWar = true;

                switch (state)
                {
                    case DiplomaticState.Alliance:
                        mods.TradeModifier += 0.10f;
                        mods.IncomeModifier += 0.05f;
                        mods.ProductionModifier += 0.02f;
                        mods.ExpenseModifier -= 0.02f;
                        mods.HappinessDelta += 1f;
                        alliances++;
                        break;
                    case DiplomaticState.Friendly:
                        mods.TradeModifier += 0.05f;
                        mods.IncomeModifier += 0.02f;
                        mods.HappinessDelta += 0.5f;
                        break;
                    case DiplomaticState.Tense:
                        mods.TradeModifier -= 0.05f;
                        mods.IncomeModifier -= 0.02f;
                        mods.BlackMarketRiskDelta += 0.02f;
                        mods.HappinessDelta -= 0.5f;
                        break;
                    case DiplomaticState.Hostile:
                        mods.TradeModifier -= 0.10f;
                        mods.IncomeModifier -= 0.05f;
                        mods.BlackMarketRiskDelta += 0.05f;
                        mods.HappinessDelta -= 1f;
                        break;
                    case DiplomaticState.War:
                        mods.TradeModifier -= 0.20f;
                        mods.IncomeModifier -= 0.10f;
                        mods.ProductionModifier -= 0.10f;
                        mods.ExpenseModifier += 0.10f;
                        mods.UnemploymentDelta += 0.05f;
                        mods.MortalityRateDelta += 0.005f;
                        mods.HappinessDelta -= 2f;
                        mods.BlackMarketRiskDelta += 0.08f;
                        mods.InflationDelta += 2f;
                        break;
                }

                if (rel.HasSanctions)
                {
                    mods.TradeModifier -= 0.05f;
                    mods.BlackMarketRiskDelta += 0.03f;
                    mods.HappinessDelta -= 0.5f;
                }

                if (rel.HasEmbargo)
                {
                    mods.TradeModifier -= 0.15f;
                    mods.BlackMarketRiskDelta += 0.07f;
                    mods.HappinessDelta -= 1f;
                }

                if (rel.HasTradeAgreement)
                {
                    mods.TradeModifier += 0.08f;
                    mods.IncomeModifier += 0.03f;
                }
            }

            if (atWar && alliances > 0)
            {
                float reduction = Math.Min(0.2f, alliances * 0.05f);
                mods.ProductionModifier += reduction;
                mods.IncomeModifier += reduction;
                mods.ExpenseModifier -= reduction * 0.5f;
                mods.UnemploymentDelta -= reduction * 0.2f;
                mods.MortalityRateDelta -= reduction * 0.002f;
            }

            return mods;
        }

        public static bool CanReceiveInternationalAid(int countryId)
        {
            var relations = GetRelationsForCountry(countryId);
            foreach (var rel in relations)
            {
                if (rel.IsAtWar || rel.State == DiplomaticState.War || rel.HasEmbargo)
                    return false;
            }
            return true;
        }

        public static (bool success, string message) ApplyAction(int countryId, int targetCountryId, DiplomaticAction action, long userId, bool isMaster, bool propagateAllies = true)
        {
            if (countryId == targetCountryId)
                return (false, "Не можна виконувати дію щодо своєї країни.");

            var (a, b) = Normalize(countryId, targetCountryId);
            var relation = GetRelation(a, b);
            var oldState = relation.State;

            if (!isMaster)
            {
                float corruption = CountriesData.GetCountryById(countryId)?.Corruption ?? 0f;
                float intel = CountriesData.GetCountryById(countryId)?.IntelLevel ?? 0f;
                float counter = CountriesData.GetCountryById(targetCountryId)?.CounterIntelLevel ?? 0f;
                float baseSuccess = 0.85f;
                float riskPenalty = Math.Min(0.3f, corruption / 200f);
                float intelBonus = Math.Max(-0.2f, Math.Min(0.2f, (intel - counter) / 200f));
                float chance = baseSuccess - riskPenalty + intelBonus + (float)(Rng.NextDouble() * 0.1f - 0.05f);
                chance = Math.Max(0.1f, Math.Min(0.95f, chance));
                if (Rng.NextDouble() > chance)
                {
                    DiplomacyLogData.AddLog(countryId, targetCountryId, $"Failed:{action}", (int)oldState, (int)oldState, userId, CountriesData.GetCountryById(countryId)?.TurnsSurvived);
                    return (false, "Дипломатична спроба не вдалася (ризик/корупція).");
                }
            }

            ApplyActionToRelation(relation, action);
            PersistRelation(countryId, targetCountryId, relation, userId, action.ToString(), oldState);

            if (action == DiplomaticAction.DeclareWar && propagateAllies)
                PullAlliesIntoWar(countryId, targetCountryId, userId);

            UpdateWarState(countryId);
            UpdateWarState(targetCountryId);

            return (true, "Дипломатичну дію застосовано.");
        }

        public static void SetState(int countryId, int targetCountryId, DiplomaticState state, long userId)
        {
            var relation = GetRelation(countryId, targetCountryId);
            var oldState = relation.State;

            relation.State = state;
            relation.IsAtWar = state == DiplomaticState.War;
            relation.IsAlliance = state == DiplomaticState.Alliance;
            if (state == DiplomaticState.War)
            {
                relation.HasTradeAgreement = false;
            }

            PersistRelation(countryId, targetCountryId, relation, userId, "SetState", oldState);
            UpdateWarState(countryId);
            UpdateWarState(targetCountryId);
        }

        private static void ApplyActionToRelation(DiplomaticRelation relation, DiplomaticAction action)
        {
            switch (action)
            {
                case DiplomaticAction.DeclareWar:
                    relation.State = DiplomaticState.War;
                    relation.IsAtWar = true;
                    relation.IsAlliance = false;
                    relation.HasTradeAgreement = false;
                    break;
                case DiplomaticAction.MakePeace:
                    relation.State = DiplomaticState.Neutral;
                    relation.IsAtWar = false;
                    relation.HasEmbargo = false;
                    relation.HasSanctions = false;
                    break;
                case DiplomaticAction.FormAlliance:
                    relation.State = DiplomaticState.Alliance;
                    relation.IsAlliance = true;
                    relation.IsAtWar = false;
                    break;
                case DiplomaticAction.BreakAlliance:
                    relation.IsAlliance = false;
                    relation.State = DiplomaticState.Neutral;
                    break;
                case DiplomaticAction.ApplySanctions:
                    relation.HasSanctions = true;
                    if (relation.State == DiplomaticState.Neutral || relation.State == DiplomaticState.Friendly)
                        relation.State = DiplomaticState.Tense;
                    break;
                case DiplomaticAction.LiftSanctions:
                    relation.HasSanctions = false;
                    if (!relation.HasEmbargo && !relation.IsAlliance && !relation.IsAtWar)
                        relation.State = DiplomaticState.Neutral;
                    break;
                case DiplomaticAction.SignTradeAgreement:
                    relation.HasTradeAgreement = true;
                    if (relation.State == DiplomaticState.Neutral)
                        relation.State = DiplomaticState.Friendly;
                    break;
                case DiplomaticAction.BreakTradeAgreement:
                    relation.HasTradeAgreement = false;
                    if (!relation.HasEmbargo && !relation.HasSanctions && !relation.IsAlliance && !relation.IsAtWar)
                        relation.State = DiplomaticState.Neutral;
                    break;
                case DiplomaticAction.ImposeEmbargo:
                    relation.HasEmbargo = true;
                    relation.State = DiplomaticState.Hostile;
                    relation.HasTradeAgreement = false;
                    break;
                case DiplomaticAction.LiftEmbargo:
                    relation.HasEmbargo = false;
                    if (!relation.HasSanctions && !relation.IsAlliance && !relation.IsAtWar)
                        relation.State = DiplomaticState.Neutral;
                    break;
            }
        }

        private static void PersistRelation(int countryId, int targetCountryId, DiplomaticRelation relation, long userId, string action, DiplomaticState oldState)
        {
            var (a, b) = Normalize(countryId, targetCountryId);
            var turn = CountriesData.GetCountryById(countryId)?.TurnsSurvived;
            DiplomacyRelationsData.Upsert(new DiplomacyRelationsData.DiplomacyRelationRecord
            {
                CountryA = a,
                CountryB = b,
                State = (int)relation.State,
                IsAlliance = relation.IsAlliance,
                IsAtWar = relation.IsAtWar,
                HasSanctions = relation.HasSanctions,
                HasEmbargo = relation.HasEmbargo,
                HasTradeAgreement = relation.HasTradeAgreement,
                LastChangedTurn = turn
            });
            DiplomacyLogData.AddLog(countryId, targetCountryId, action, (int)oldState, (int)relation.State, userId, turn);
        }

        private static void PullAlliesIntoWar(int countryId, int targetCountryId, long userId)
        {
            var alliesA = GetRelationsForCountry(countryId)
                .Where(r => r.IsAlliance && r.State == DiplomaticState.Alliance)
                .Select(r => r.CountryA == countryId ? r.CountryB : r.CountryA)
                .ToList();
            var alliesB = GetRelationsForCountry(targetCountryId)
                .Where(r => r.IsAlliance && r.State == DiplomaticState.Alliance)
                .Select(r => r.CountryA == targetCountryId ? r.CountryB : r.CountryA)
                .ToList();

            foreach (var ally in alliesA)
                ApplyAction(ally, targetCountryId, DiplomaticAction.DeclareWar, userId, true, false);
            foreach (var ally in alliesB)
                ApplyAction(ally, countryId, DiplomaticAction.DeclareWar, userId, true, false);
        }

        private static void UpdateWarState(int countryId)
        {
            var country = CountriesData.GetCountryById(countryId);
            if (country == null)
                return;

            bool war = GetRelationsForCountry(countryId).Any(r => r.IsAtWar || r.State == DiplomaticState.War);
            if (country.WarState != war)
            {
                country.WarState = war;
                CountriesData.UpdateCountry(country);
            }
        }
    }
}

