#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DNDBot.DataBase
{
    public static class EconomyTurnHistoryData
    {
        static EconomyTurnHistoryData()
        {
            BaseRepository.DBInitialization();
        }

        public sealed class EconomyTurnRecord
        {
            public int CountryId { get; set; }
            public int TurnNumber { get; set; }
            public decimal Income { get; set; }
            public decimal Expenses { get; set; }
            public decimal NetResult { get; set; }
            public decimal CorruptionLoss { get; set; }
            public float EmploymentRate { get; set; }
            public float UnemploymentRate { get; set; }
            public decimal ProductionValue { get; set; }
            public decimal TradeIncome { get; set; }
            public decimal TaxIncome { get; set; }
            public float Stability { get; set; }
            public decimal FoodDemand { get; set; }
            public decimal ClothingDemand { get; set; }
            public decimal HousingDemand { get; set; }
            public decimal ToolsDemand { get; set; }
            public decimal LuxuryDemand { get; set; }
            public decimal BlackMarketPurchased { get; set; }
            public decimal BlackMarketValue { get; set; }
            public float ShortageScore { get; set; }
            public float HappinessDelta { get; set; }
            public float MortalityDelta { get; set; }
            public float ProductivityDelta { get; set; }
            public long PopulationBirths { get; set; }
            public long PopulationChildToWorking { get; set; }
            public long PopulationWorkingToPensioners { get; set; }
            public long PopulationWorkingToSocial { get; set; }
            public long PopulationPensionersToSocial { get; set; }
            public long PopulationDeathsChildren { get; set; }
            public long PopulationDeathsWorking { get; set; }
            public long PopulationDeathsPensioners { get; set; }
            public long PopulationDeathsSocial { get; set; }
            public long PopulationGrowthReduction { get; set; }
            public long PopulationStartChildren { get; set; }
            public long PopulationStartWorkingAge { get; set; }
            public long PopulationStartPensioners { get; set; }
            public long PopulationStartSocialProtected { get; set; }
            public long PopulationEndChildren { get; set; }
            public long PopulationEndWorkingAge { get; set; }
            public long PopulationEndPensioners { get; set; }
            public long PopulationEndSocialProtected { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        private static SqlParameter DecimalParam(string name, decimal value, byte precision = 28, byte scale = 2)
        {
            var p = new SqlParameter(name, SqlDbType.Decimal)
            {
                Precision = precision,
                Scale = scale,
                Value = value
            };
            return p;
        }

        public static void AddTurn(
            int countryId,
            int turnNumber,
            decimal income,
            decimal expenses,
            decimal netResult,
            decimal corruptionLoss,
            float employmentRate,
            float unemploymentRate,
            decimal productionValue,
            decimal tradeIncome,
            decimal taxIncome,
            float stability,
            decimal foodDemand,
            decimal clothingDemand,
            decimal housingDemand,
            decimal toolsDemand,
            decimal luxuryDemand,
            decimal blackMarketPurchased,
            decimal blackMarketValue,
            float shortageScore,
            float happinessDelta,
            float mortalityDelta,
            float productivityDelta,
            long populationBirths,
            long populationChildToWorking,
            long populationWorkingToPensioners,
            long populationWorkingToSocial,
            long populationPensionersToSocial,
            long populationDeathsChildren,
            long populationDeathsWorking,
            long populationDeathsPensioners,
            long populationDeathsSocial,
            long populationGrowthReduction,
            long populationStartChildren,
            long populationStartWorkingAge,
            long populationStartPensioners,
            long populationStartSocialProtected,
            long populationEndChildren,
            long populationEndWorkingAge,
            long populationEndPensioners,
            long populationEndSocialProtected)
        {
            const string q = @"
INSERT INTO EconomyTurnHistory (
    CountryId, TurnNumber, Income, Expenses, NetResult, CorruptionLoss,
    EmploymentRate, UnemploymentRate, ProductionValue, TradeIncome, TaxIncome, Stability,
    FoodDemand, ClothingDemand, HousingDemand, ToolsDemand, LuxuryDemand,
    BlackMarketPurchased, BlackMarketValue,
    ShortageScore, HappinessDelta, MortalityDelta, ProductivityDelta,
    PopulationBirths, PopulationChildToWorking, PopulationWorkingToPensioners, PopulationWorkingToSocial, PopulationPensionersToSocial,
    PopulationDeathsChildren, PopulationDeathsWorking, PopulationDeathsPensioners, PopulationDeathsSocial,
    PopulationGrowthReduction,
    PopulationStartChildren, PopulationStartWorkingAge, PopulationStartPensioners, PopulationStartSocialProtected,
    PopulationEndChildren, PopulationEndWorkingAge, PopulationEndPensioners, PopulationEndSocialProtected
)
VALUES (
    @CountryId, @TurnNumber, @Income, @Expenses, @NetResult, @CorruptionLoss,
    @EmploymentRate, @UnemploymentRate, @ProductionValue, @TradeIncome, @TaxIncome, @Stability,
    @FoodDemand, @ClothingDemand, @HousingDemand, @ToolsDemand, @LuxuryDemand,
    @BlackMarketPurchased, @BlackMarketValue,
    @ShortageScore, @HappinessDelta, @MortalityDelta, @ProductivityDelta,
    @PopulationBirths, @PopulationChildToWorking, @PopulationWorkingToPensioners, @PopulationWorkingToSocial, @PopulationPensionersToSocial,
    @PopulationDeathsChildren, @PopulationDeathsWorking, @PopulationDeathsPensioners, @PopulationDeathsSocial,
    @PopulationGrowthReduction,
    @PopulationStartChildren, @PopulationStartWorkingAge, @PopulationStartPensioners, @PopulationStartSocialProtected,
    @PopulationEndChildren, @PopulationEndWorkingAge, @PopulationEndPensioners, @PopulationEndSocialProtected
);";

            BaseRepository.ExecuteNonQuery(q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId),
                BaseRepository.Param("@TurnNumber", SqlDbType.Int, turnNumber),
                DecimalParam("@Income", income),
                DecimalParam("@Expenses", expenses),
                DecimalParam("@NetResult", netResult),
                DecimalParam("@CorruptionLoss", corruptionLoss),
                BaseRepository.Param("@EmploymentRate", SqlDbType.Real, employmentRate),
                BaseRepository.Param("@UnemploymentRate", SqlDbType.Real, unemploymentRate),
                DecimalParam("@ProductionValue", productionValue),
                DecimalParam("@TradeIncome", tradeIncome),
                DecimalParam("@TaxIncome", taxIncome),
                BaseRepository.Param("@Stability", SqlDbType.Real, stability),
                DecimalParam("@FoodDemand", foodDemand),
                DecimalParam("@ClothingDemand", clothingDemand),
                DecimalParam("@HousingDemand", housingDemand),
                DecimalParam("@ToolsDemand", toolsDemand),
                DecimalParam("@LuxuryDemand", luxuryDemand),
                DecimalParam("@BlackMarketPurchased", blackMarketPurchased),
                DecimalParam("@BlackMarketValue", blackMarketValue),
                BaseRepository.Param("@ShortageScore", SqlDbType.Real, shortageScore),
                BaseRepository.Param("@HappinessDelta", SqlDbType.Real, happinessDelta),
                BaseRepository.Param("@MortalityDelta", SqlDbType.Real, mortalityDelta),
                BaseRepository.Param("@ProductivityDelta", SqlDbType.Real, productivityDelta),
                BaseRepository.Param("@PopulationBirths", SqlDbType.BigInt, populationBirths),
                BaseRepository.Param("@PopulationChildToWorking", SqlDbType.BigInt, populationChildToWorking),
                BaseRepository.Param("@PopulationWorkingToPensioners", SqlDbType.BigInt, populationWorkingToPensioners),
                BaseRepository.Param("@PopulationWorkingToSocial", SqlDbType.BigInt, populationWorkingToSocial),
                BaseRepository.Param("@PopulationPensionersToSocial", SqlDbType.BigInt, populationPensionersToSocial),
                BaseRepository.Param("@PopulationDeathsChildren", SqlDbType.BigInt, populationDeathsChildren),
                BaseRepository.Param("@PopulationDeathsWorking", SqlDbType.BigInt, populationDeathsWorking),
                BaseRepository.Param("@PopulationDeathsPensioners", SqlDbType.BigInt, populationDeathsPensioners),
                BaseRepository.Param("@PopulationDeathsSocial", SqlDbType.BigInt, populationDeathsSocial),
                BaseRepository.Param("@PopulationGrowthReduction", SqlDbType.BigInt, populationGrowthReduction),
                BaseRepository.Param("@PopulationStartChildren", SqlDbType.BigInt, populationStartChildren),
                BaseRepository.Param("@PopulationStartWorkingAge", SqlDbType.BigInt, populationStartWorkingAge),
                BaseRepository.Param("@PopulationStartPensioners", SqlDbType.BigInt, populationStartPensioners),
                BaseRepository.Param("@PopulationStartSocialProtected", SqlDbType.BigInt, populationStartSocialProtected),
                BaseRepository.Param("@PopulationEndChildren", SqlDbType.BigInt, populationEndChildren),
                BaseRepository.Param("@PopulationEndWorkingAge", SqlDbType.BigInt, populationEndWorkingAge),
                BaseRepository.Param("@PopulationEndPensioners", SqlDbType.BigInt, populationEndPensioners),
                BaseRepository.Param("@PopulationEndSocialProtected", SqlDbType.BigInt, populationEndSocialProtected)
            );
        }

        public static EconomyTurnRecord? GetLatestTurn(int countryId)
        {
            const string q = @"
SELECT TOP 1 CountryId, TurnNumber, Income, Expenses, NetResult, CorruptionLoss,
       EmploymentRate, UnemploymentRate, ProductionValue, TradeIncome, TaxIncome, Stability,
       FoodDemand, ClothingDemand, HousingDemand, ToolsDemand, LuxuryDemand,
       BlackMarketPurchased, BlackMarketValue,
       ShortageScore, HappinessDelta, MortalityDelta, ProductivityDelta,
       PopulationBirths, PopulationChildToWorking, PopulationWorkingToPensioners, PopulationWorkingToSocial, PopulationPensionersToSocial,
       PopulationDeathsChildren, PopulationDeathsWorking, PopulationDeathsPensioners, PopulationDeathsSocial,
       PopulationGrowthReduction,
       PopulationStartChildren, PopulationStartWorkingAge, PopulationStartPensioners, PopulationStartSocialProtected,
       PopulationEndChildren, PopulationEndWorkingAge, PopulationEndPensioners, PopulationEndSocialProtected,
       CreatedAt
FROM EconomyTurnHistory
WHERE CountryId = @CountryId
ORDER BY TurnNumber DESC, CreatedAt DESC";

            using var reader = BaseRepository.ExecuteReader(q, BaseRepository.Param("@CountryId", SqlDbType.Int, countryId));
            try
            {
                if (reader.Read())
                {
                    return new EconomyTurnRecord
                    {
                        CountryId = reader.GetInt32(reader.GetOrdinal("CountryId")),
                        TurnNumber = reader.GetInt32(reader.GetOrdinal("TurnNumber")),
                        Income = reader.GetDecimal(reader.GetOrdinal("Income")),
                        Expenses = reader.GetDecimal(reader.GetOrdinal("Expenses")),
                        NetResult = reader.GetDecimal(reader.GetOrdinal("NetResult")),
                        CorruptionLoss = reader.GetDecimal(reader.GetOrdinal("CorruptionLoss")),
                        EmploymentRate = Convert.ToSingle(reader["EmploymentRate"]),
                        UnemploymentRate = Convert.ToSingle(reader["UnemploymentRate"]),
                        ProductionValue = reader.GetDecimal(reader.GetOrdinal("ProductionValue")),
                        TradeIncome = reader.GetDecimal(reader.GetOrdinal("TradeIncome")),
                        TaxIncome = reader.GetDecimal(reader.GetOrdinal("TaxIncome")),
                        Stability = Convert.ToSingle(reader["Stability"]),
                        FoodDemand = reader.GetDecimal(reader.GetOrdinal("FoodDemand")),
                        ClothingDemand = reader.GetDecimal(reader.GetOrdinal("ClothingDemand")),
                        HousingDemand = reader.GetDecimal(reader.GetOrdinal("HousingDemand")),
                        ToolsDemand = reader.GetDecimal(reader.GetOrdinal("ToolsDemand")),
                        LuxuryDemand = reader.GetDecimal(reader.GetOrdinal("LuxuryDemand")),
                        BlackMarketPurchased = reader.GetDecimal(reader.GetOrdinal("BlackMarketPurchased")),
                        BlackMarketValue = reader.GetDecimal(reader.GetOrdinal("BlackMarketValue")),
                        ShortageScore = Convert.ToSingle(reader["ShortageScore"]),
                        HappinessDelta = Convert.ToSingle(reader["HappinessDelta"]),
                        MortalityDelta = Convert.ToSingle(reader["MortalityDelta"]),
                        ProductivityDelta = Convert.ToSingle(reader["ProductivityDelta"]),
                        PopulationBirths = reader.GetInt64(reader.GetOrdinal("PopulationBirths")),
                        PopulationChildToWorking = reader.GetInt64(reader.GetOrdinal("PopulationChildToWorking")),
                        PopulationWorkingToPensioners = reader.GetInt64(reader.GetOrdinal("PopulationWorkingToPensioners")),
                        PopulationWorkingToSocial = reader.GetInt64(reader.GetOrdinal("PopulationWorkingToSocial")),
                        PopulationPensionersToSocial = reader.GetInt64(reader.GetOrdinal("PopulationPensionersToSocial")),
                        PopulationDeathsChildren = reader.GetInt64(reader.GetOrdinal("PopulationDeathsChildren")),
                        PopulationDeathsWorking = reader.GetInt64(reader.GetOrdinal("PopulationDeathsWorking")),
                        PopulationDeathsPensioners = reader.GetInt64(reader.GetOrdinal("PopulationDeathsPensioners")),
                        PopulationDeathsSocial = reader.GetInt64(reader.GetOrdinal("PopulationDeathsSocial")),
                        PopulationGrowthReduction = reader.GetInt64(reader.GetOrdinal("PopulationGrowthReduction")),
                        PopulationStartChildren = reader.GetInt64(reader.GetOrdinal("PopulationStartChildren")),
                        PopulationStartWorkingAge = reader.GetInt64(reader.GetOrdinal("PopulationStartWorkingAge")),
                        PopulationStartPensioners = reader.GetInt64(reader.GetOrdinal("PopulationStartPensioners")),
                        PopulationStartSocialProtected = reader.GetInt64(reader.GetOrdinal("PopulationStartSocialProtected")),
                        PopulationEndChildren = reader.GetInt64(reader.GetOrdinal("PopulationEndChildren")),
                        PopulationEndWorkingAge = reader.GetInt64(reader.GetOrdinal("PopulationEndWorkingAge")),
                        PopulationEndPensioners = reader.GetInt64(reader.GetOrdinal("PopulationEndPensioners")),
                        PopulationEndSocialProtected = reader.GetInt64(reader.GetOrdinal("PopulationEndSocialProtected")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                    };
                }
            }
            finally
            {
                reader.Dispose();
            }

            return null;
        }

        public static List<EconomyTurnRecord> GetRecentTurns(int countryId, int count)
        {
            if (count < 1)
                count = 1;
            const string q = @"
SELECT TOP (@Count) CountryId, TurnNumber, Income, Expenses, NetResult, CorruptionLoss,
       EmploymentRate, UnemploymentRate, ProductionValue, TradeIncome, TaxIncome, Stability,
       FoodDemand, ClothingDemand, HousingDemand, ToolsDemand, LuxuryDemand,
       BlackMarketPurchased, BlackMarketValue,
       ShortageScore, HappinessDelta, MortalityDelta, ProductivityDelta,
       PopulationBirths, PopulationChildToWorking, PopulationWorkingToPensioners, PopulationWorkingToSocial, PopulationPensionersToSocial,
       PopulationDeathsChildren, PopulationDeathsWorking, PopulationDeathsPensioners, PopulationDeathsSocial,
       PopulationGrowthReduction,
       PopulationStartChildren, PopulationStartWorkingAge, PopulationStartPensioners, PopulationStartSocialProtected,
       PopulationEndChildren, PopulationEndWorkingAge, PopulationEndPensioners, PopulationEndSocialProtected,
       CreatedAt
FROM EconomyTurnHistory
WHERE CountryId = @CountryId
ORDER BY TurnNumber DESC, CreatedAt DESC";

            var list = new List<EconomyTurnRecord>();
            using var reader = BaseRepository.ExecuteReader(q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId),
                BaseRepository.Param("@Count", SqlDbType.Int, count));
            try
            {
                while (reader.Read())
                {
                    list.Add(new EconomyTurnRecord
                    {
                        CountryId = reader.GetInt32(reader.GetOrdinal("CountryId")),
                        TurnNumber = reader.GetInt32(reader.GetOrdinal("TurnNumber")),
                        Income = reader.GetDecimal(reader.GetOrdinal("Income")),
                        Expenses = reader.GetDecimal(reader.GetOrdinal("Expenses")),
                        NetResult = reader.GetDecimal(reader.GetOrdinal("NetResult")),
                        CorruptionLoss = reader.GetDecimal(reader.GetOrdinal("CorruptionLoss")),
                        EmploymentRate = Convert.ToSingle(reader["EmploymentRate"]),
                        UnemploymentRate = Convert.ToSingle(reader["UnemploymentRate"]),
                        ProductionValue = reader.GetDecimal(reader.GetOrdinal("ProductionValue")),
                        TradeIncome = reader.GetDecimal(reader.GetOrdinal("TradeIncome")),
                        TaxIncome = reader.GetDecimal(reader.GetOrdinal("TaxIncome")),
                        Stability = Convert.ToSingle(reader["Stability"]),
                        FoodDemand = reader.GetDecimal(reader.GetOrdinal("FoodDemand")),
                        ClothingDemand = reader.GetDecimal(reader.GetOrdinal("ClothingDemand")),
                        HousingDemand = reader.GetDecimal(reader.GetOrdinal("HousingDemand")),
                        ToolsDemand = reader.GetDecimal(reader.GetOrdinal("ToolsDemand")),
                        LuxuryDemand = reader.GetDecimal(reader.GetOrdinal("LuxuryDemand")),
                        BlackMarketPurchased = reader.GetDecimal(reader.GetOrdinal("BlackMarketPurchased")),
                        BlackMarketValue = reader.GetDecimal(reader.GetOrdinal("BlackMarketValue")),
                        ShortageScore = Convert.ToSingle(reader["ShortageScore"]),
                        HappinessDelta = Convert.ToSingle(reader["HappinessDelta"]),
                        MortalityDelta = Convert.ToSingle(reader["MortalityDelta"]),
                        ProductivityDelta = Convert.ToSingle(reader["ProductivityDelta"]),
                        PopulationBirths = reader.GetInt64(reader.GetOrdinal("PopulationBirths")),
                        PopulationChildToWorking = reader.GetInt64(reader.GetOrdinal("PopulationChildToWorking")),
                        PopulationWorkingToPensioners = reader.GetInt64(reader.GetOrdinal("PopulationWorkingToPensioners")),
                        PopulationWorkingToSocial = reader.GetInt64(reader.GetOrdinal("PopulationWorkingToSocial")),
                        PopulationPensionersToSocial = reader.GetInt64(reader.GetOrdinal("PopulationPensionersToSocial")),
                        PopulationDeathsChildren = reader.GetInt64(reader.GetOrdinal("PopulationDeathsChildren")),
                        PopulationDeathsWorking = reader.GetInt64(reader.GetOrdinal("PopulationDeathsWorking")),
                        PopulationDeathsPensioners = reader.GetInt64(reader.GetOrdinal("PopulationDeathsPensioners")),
                        PopulationDeathsSocial = reader.GetInt64(reader.GetOrdinal("PopulationDeathsSocial")),
                        PopulationGrowthReduction = reader.GetInt64(reader.GetOrdinal("PopulationGrowthReduction")),
                        PopulationStartChildren = reader.GetInt64(reader.GetOrdinal("PopulationStartChildren")),
                        PopulationStartWorkingAge = reader.GetInt64(reader.GetOrdinal("PopulationStartWorkingAge")),
                        PopulationStartPensioners = reader.GetInt64(reader.GetOrdinal("PopulationStartPensioners")),
                        PopulationStartSocialProtected = reader.GetInt64(reader.GetOrdinal("PopulationStartSocialProtected")),
                        PopulationEndChildren = reader.GetInt64(reader.GetOrdinal("PopulationEndChildren")),
                        PopulationEndWorkingAge = reader.GetInt64(reader.GetOrdinal("PopulationEndWorkingAge")),
                        PopulationEndPensioners = reader.GetInt64(reader.GetOrdinal("PopulationEndPensioners")),
                        PopulationEndSocialProtected = reader.GetInt64(reader.GetOrdinal("PopulationEndSocialProtected")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                    });
                }
            }
            finally
            {
                reader.Dispose();
            }

            return list;
        }
    }
}

