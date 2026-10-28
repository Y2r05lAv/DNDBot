#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DNDBot.DataBase
{
    public static class EconomyCurrencyData
    {
        static EconomyCurrencyData()
        {
            BaseRepository.DBInitialization();
        }

        public sealed class EconomyCurrencyRecord
        {
            public int CountryId { get; set; }
            public string CurrencyCode { get; set; } = string.Empty;
            public string CurrencyName { get; set; } = string.Empty;
            public decimal BaseRate { get; set; }
            public decimal CurrentRate { get; set; }
            public float InflationFactor { get; set; }
            public float SupplyIndex { get; set; }
            public float DemandIndex { get; set; }
            public decimal CentralBankReserve { get; set; }
            public float InterventionStrength { get; set; }
            public float ExternalShock { get; set; }
            public float ScarcityIndex { get; set; }
            public float BlackMarketMultiplier { get; set; }
            public decimal BlackMarketRate { get; set; }
            public DateTime LastUpdated { get; set; }
        }

        private static SqlParameter DecimalParam(string name, decimal value, byte precision = 18, byte scale = 6)
        {
            var p = new SqlParameter(name, SqlDbType.Decimal)
            {
                Precision = precision,
                Scale = scale,
                Value = value
            };
            return p;
        }

        public static EconomyCurrencyRecord? GetByCountry(int countryId)
        {
            const string q = @"
SELECT CountryId, CurrencyCode, CurrencyName, BaseRate, CurrentRate,
       InflationFactor, SupplyIndex, DemandIndex, CentralBankReserve,
       InterventionStrength, ExternalShock, ScarcityIndex, BlackMarketMultiplier,
       BlackMarketRate, LastUpdated
FROM EconomyCurrencies
WHERE CountryId = @CountryId";

            using var reader = BaseRepository.ExecuteReader(q, BaseRepository.Param("@CountryId", SqlDbType.Int, countryId));
            try
            {
                if (reader.Read())
                {
                    return Map(reader);
                }
            }
            finally
            {
                reader.Dispose();
            }
            return null;
        }

        public static EconomyCurrencyRecord? GetByCode(string currencyCode)
        {
            const string q = @"
SELECT CountryId, CurrencyCode, CurrencyName, BaseRate, CurrentRate,
       InflationFactor, SupplyIndex, DemandIndex, CentralBankReserve,
       InterventionStrength, ExternalShock, ScarcityIndex, BlackMarketMultiplier,
       BlackMarketRate, LastUpdated
FROM EconomyCurrencies
WHERE CurrencyCode = @Code";

            using var reader = BaseRepository.ExecuteReader(q, BaseRepository.Param("@Code", SqlDbType.NVarChar, currencyCode, 16));
            try
            {
                if (reader.Read())
                {
                    return Map(reader);
                }
            }
            finally
            {
                reader.Dispose();
            }
            return null;
        }

        public static List<EconomyCurrencyRecord> GetAll()
        {
            const string q = @"
SELECT CountryId, CurrencyCode, CurrencyName, BaseRate, CurrentRate,
       InflationFactor, SupplyIndex, DemandIndex, CentralBankReserve,
       InterventionStrength, ExternalShock, ScarcityIndex, BlackMarketMultiplier,
       BlackMarketRate, LastUpdated
FROM EconomyCurrencies
ORDER BY CountryId";

            var list = new List<EconomyCurrencyRecord>();
            using var reader = BaseRepository.ExecuteReader(q);
            try
            {
                while (reader.Read())
                {
                    list.Add(Map(reader));
                }
            }
            finally
            {
                reader.Dispose();
            }
            return list;
        }

        public static void Upsert(EconomyCurrencyRecord record)
        {
            const string q = @"
IF EXISTS (SELECT 1 FROM EconomyCurrencies WHERE CountryId = @CountryId)
BEGIN
    UPDATE EconomyCurrencies
    SET CurrencyCode = @CurrencyCode,
        CurrencyName = @CurrencyName,
        BaseRate = @BaseRate,
        CurrentRate = @CurrentRate,
        InflationFactor = @InflationFactor,
        SupplyIndex = @SupplyIndex,
        DemandIndex = @DemandIndex,
        CentralBankReserve = @CentralBankReserve,
        InterventionStrength = @InterventionStrength,
        ExternalShock = @ExternalShock,
        ScarcityIndex = @ScarcityIndex,
        BlackMarketMultiplier = @BlackMarketMultiplier,
        BlackMarketRate = @BlackMarketRate,
        LastUpdated = @LastUpdated
    WHERE CountryId = @CountryId;
END
ELSE
BEGIN
    INSERT INTO EconomyCurrencies (
        CountryId, CurrencyCode, CurrencyName, BaseRate, CurrentRate,
        InflationFactor, SupplyIndex, DemandIndex, CentralBankReserve,
        InterventionStrength, ExternalShock, ScarcityIndex, BlackMarketMultiplier,
        BlackMarketRate, LastUpdated
    )
    VALUES (
        @CountryId, @CurrencyCode, @CurrencyName, @BaseRate, @CurrentRate,
        @InflationFactor, @SupplyIndex, @DemandIndex, @CentralBankReserve,
        @InterventionStrength, @ExternalShock, @ScarcityIndex, @BlackMarketMultiplier,
        @BlackMarketRate, @LastUpdated
    );
END";

            BaseRepository.ExecuteNonQuery(q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, record.CountryId),
                BaseRepository.Param("@CurrencyCode", SqlDbType.NVarChar, record.CurrencyCode, 16),
                BaseRepository.Param("@CurrencyName", SqlDbType.NVarChar, record.CurrencyName, 50),
                DecimalParam("@BaseRate", record.BaseRate),
                DecimalParam("@CurrentRate", record.CurrentRate),
                BaseRepository.Param("@InflationFactor", SqlDbType.Real, record.InflationFactor),
                BaseRepository.Param("@SupplyIndex", SqlDbType.Real, record.SupplyIndex),
                BaseRepository.Param("@DemandIndex", SqlDbType.Real, record.DemandIndex),
                DecimalParam("@CentralBankReserve", record.CentralBankReserve, 18, 2),
                BaseRepository.Param("@InterventionStrength", SqlDbType.Real, record.InterventionStrength),
                BaseRepository.Param("@ExternalShock", SqlDbType.Real, record.ExternalShock),
                BaseRepository.Param("@ScarcityIndex", SqlDbType.Real, record.ScarcityIndex),
                BaseRepository.Param("@BlackMarketMultiplier", SqlDbType.Real, record.BlackMarketMultiplier),
                DecimalParam("@BlackMarketRate", record.BlackMarketRate),
                BaseRepository.Param("@LastUpdated", SqlDbType.DateTime2, record.LastUpdated)
            );
        }

        public static void UpdateRates(int countryId, decimal currentRate, decimal blackMarketRate, float inflationFactor, float supplyIndex, float demandIndex, float interventionStrength, float externalShock, float scarcityIndex, float blackMarketMultiplier)
        {
            const string q = @"
UPDATE EconomyCurrencies
SET CurrentRate = @CurrentRate,
    BlackMarketRate = @BlackMarketRate,
    InflationFactor = @InflationFactor,
    SupplyIndex = @SupplyIndex,
    DemandIndex = @DemandIndex,
    InterventionStrength = @InterventionStrength,
    ExternalShock = @ExternalShock,
    ScarcityIndex = @ScarcityIndex,
    BlackMarketMultiplier = @BlackMarketMultiplier,
    LastUpdated = @LastUpdated
WHERE CountryId = @CountryId";

            BaseRepository.ExecuteNonQuery(q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId),
                DecimalParam("@CurrentRate", currentRate),
                DecimalParam("@BlackMarketRate", blackMarketRate),
                BaseRepository.Param("@InflationFactor", SqlDbType.Real, inflationFactor),
                BaseRepository.Param("@SupplyIndex", SqlDbType.Real, supplyIndex),
                BaseRepository.Param("@DemandIndex", SqlDbType.Real, demandIndex),
                BaseRepository.Param("@InterventionStrength", SqlDbType.Real, interventionStrength),
                BaseRepository.Param("@ExternalShock", SqlDbType.Real, externalShock),
                BaseRepository.Param("@ScarcityIndex", SqlDbType.Real, scarcityIndex),
                BaseRepository.Param("@BlackMarketMultiplier", SqlDbType.Real, blackMarketMultiplier),
                BaseRepository.Param("@LastUpdated", SqlDbType.DateTime2, DateTime.Now)
            );
        }

        public static void SetRate(int countryId, decimal newRate)
        {
            const string q = @"
UPDATE EconomyCurrencies
SET BaseRate = @Rate,
    CurrentRate = @Rate,
    BlackMarketRate = @Rate,
    LastUpdated = @LastUpdated
WHERE CountryId = @CountryId";

            BaseRepository.ExecuteNonQuery(q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId),
                DecimalParam("@Rate", newRate),
                BaseRepository.Param("@LastUpdated", SqlDbType.DateTime2, DateTime.Now)
            );
        }

        private static EconomyCurrencyRecord Map(IDataRecord reader)
        {
            return new EconomyCurrencyRecord
            {
                CountryId = reader.GetInt32(reader.GetOrdinal("CountryId")),
                CurrencyCode = reader.GetString(reader.GetOrdinal("CurrencyCode")),
                CurrencyName = reader.GetString(reader.GetOrdinal("CurrencyName")),
                BaseRate = reader.GetDecimal(reader.GetOrdinal("BaseRate")),
                CurrentRate = reader.GetDecimal(reader.GetOrdinal("CurrentRate")),
                InflationFactor = Convert.ToSingle(reader["InflationFactor"]),
                SupplyIndex = Convert.ToSingle(reader["SupplyIndex"]),
                DemandIndex = Convert.ToSingle(reader["DemandIndex"]),
                CentralBankReserve = reader.GetDecimal(reader.GetOrdinal("CentralBankReserve")),
                InterventionStrength = Convert.ToSingle(reader["InterventionStrength"]),
                ExternalShock = Convert.ToSingle(reader["ExternalShock"]),
                ScarcityIndex = Convert.ToSingle(reader["ScarcityIndex"]),
                BlackMarketMultiplier = Convert.ToSingle(reader["BlackMarketMultiplier"]),
                BlackMarketRate = reader.GetDecimal(reader.GetOrdinal("BlackMarketRate")),
                LastUpdated = reader.GetDateTime(reader.GetOrdinal("LastUpdated"))
            };
        }
    }
}

