using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DNDBot.DataBase
{
    public sealed class EconomyFestivalRecord
    {
        public int FestivalId { get; set; }
        public int CountryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DurationTurns { get; set; }
        public int RemainingTurns { get; set; }
        public decimal Cost { get; set; }
        public float StabilityBonus { get; set; }
        public float PopulationGrowthBonus { get; set; }
        public float ProductionPenalty { get; set; }
        public float IncomePenalty { get; set; }
        public bool IsActive { get; set; }
    }

    public static class EconomyFestivalsData
    {
        static EconomyFestivalsData()
        {
            BaseRepository.DBInitialization();
        }

        private static SqlParameter DecimalParam(string name, decimal value, byte precision = 18, byte scale = 2)
        {
            var p = new SqlParameter(name, SqlDbType.Decimal)
            {
                Precision = precision,
                Scale = scale,
                Value = value
            };
            return p;
        }

        public static List<EconomyFestivalRecord> GetActiveByCountry(int countryId)
        {
            const string q = @"
SELECT FestivalId, CountryId, Name, DurationTurns, RemainingTurns, Cost, StabilityBonus, PopulationGrowthBonus, ProductionPenalty, IncomePenalty, IsActive
FROM EconomyFestivals
WHERE CountryId = @CountryId AND IsActive = 1";

            var list = new List<EconomyFestivalRecord>();
            using var reader = BaseRepository.ExecuteReader(q, BaseRepository.Param("@CountryId", SqlDbType.Int, countryId));
            try
            {
                while (reader.Read())
                {
                    list.Add(new EconomyFestivalRecord
                    {
                        FestivalId = reader.GetInt32(reader.GetOrdinal("FestivalId")),
                        CountryId = reader.GetInt32(reader.GetOrdinal("CountryId")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        DurationTurns = reader.GetInt32(reader.GetOrdinal("DurationTurns")),
                        RemainingTurns = reader.GetInt32(reader.GetOrdinal("RemainingTurns")),
                        Cost = reader.GetDecimal(reader.GetOrdinal("Cost")),
                        StabilityBonus = Convert.ToSingle(reader["StabilityBonus"]),
                        PopulationGrowthBonus = Convert.ToSingle(reader["PopulationGrowthBonus"]),
                        ProductionPenalty = Convert.ToSingle(reader["ProductionPenalty"]),
                        IncomePenalty = Convert.ToSingle(reader["IncomePenalty"]),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                    });
                }
            }
            finally
            {
                reader.Dispose();
            }
            return list;
        }

        public static int AddFestival(EconomyFestivalRecord festival)
        {
            const string q = @"
INSERT INTO EconomyFestivals (
    CountryId, Name, DurationTurns, RemainingTurns, Cost,
    StabilityBonus, PopulationGrowthBonus, ProductionPenalty, IncomePenalty, IsActive
)
VALUES (
    @CountryId, @Name, @DurationTurns, @RemainingTurns, @Cost,
    @StabilityBonus, @PopulationGrowthBonus, @ProductionPenalty, @IncomePenalty, @IsActive
);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            int remaining = festival.RemainingTurns > 0 ? festival.RemainingTurns : festival.DurationTurns;
            var idObj = BaseRepository.ExecuteScalar(q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, festival.CountryId),
                BaseRepository.Param("@Name", SqlDbType.NVarChar, festival.Name, 80),
                BaseRepository.Param("@DurationTurns", SqlDbType.Int, festival.DurationTurns),
                BaseRepository.Param("@RemainingTurns", SqlDbType.Int, remaining),
                DecimalParam("@Cost", festival.Cost),
                BaseRepository.Param("@StabilityBonus", SqlDbType.Real, festival.StabilityBonus),
                BaseRepository.Param("@PopulationGrowthBonus", SqlDbType.Real, festival.PopulationGrowthBonus),
                BaseRepository.Param("@ProductionPenalty", SqlDbType.Real, festival.ProductionPenalty),
                BaseRepository.Param("@IncomePenalty", SqlDbType.Real, festival.IncomePenalty),
                BaseRepository.Param("@IsActive", SqlDbType.Bit, festival.IsActive)
            );

            if (idObj == null || idObj == DBNull.Value)
                return 0;
            return Convert.ToInt32(idObj);
        }

        public static void DecrementFestival(int festivalId, int remainingTurns, bool isActive)
        {
            const string q = @"
UPDATE EconomyFestivals
SET RemainingTurns = @RemainingTurns, IsActive = @IsActive
WHERE FestivalId = @FestivalId";

            BaseRepository.ExecuteNonQuery(q,
                BaseRepository.Param("@RemainingTurns", SqlDbType.Int, remainingTurns),
                BaseRepository.Param("@IsActive", SqlDbType.Bit, isActive),
                BaseRepository.Param("@FestivalId", SqlDbType.Int, festivalId));
        }

        public static bool EndFestival(int festivalId, int countryId)
        {
            const string q = @"
UPDATE EconomyFestivals
SET RemainingTurns = 0, IsActive = 0
WHERE FestivalId = @FestivalId AND CountryId = @CountryId";

            int rows = BaseRepository.ExecuteNonQuery(q,
                BaseRepository.Param("@FestivalId", SqlDbType.Int, festivalId),
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId));
            return rows > 0;
        }

        public static bool UpdateFestivalField(int festivalId, int countryId, string fieldName, object value, SqlDbType type)
        {
            string column = fieldName switch
            {
                "Name" => "Name",
                "DurationTurns" => "DurationTurns",
                "RemainingTurns" => "RemainingTurns",
                "Cost" => "Cost",
                "StabilityBonus" => "StabilityBonus",
                "PopulationGrowthBonus" => "PopulationGrowthBonus",
                "ProductionPenalty" => "ProductionPenalty",
                "IncomePenalty" => "IncomePenalty",
                "IsActive" => "IsActive",
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(column))
                return false;

            string q = $"UPDATE EconomyFestivals SET {column} = @Value WHERE FestivalId = @FestivalId AND CountryId = @CountryId";
            int rows = BaseRepository.ExecuteNonQuery(q,
                BaseRepository.Param("@FestivalId", SqlDbType.Int, festivalId),
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId),
                BaseRepository.Param("@Value", type, value)
            );
            return rows > 0;
        }
    }
}
