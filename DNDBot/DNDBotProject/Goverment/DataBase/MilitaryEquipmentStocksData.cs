#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DNDBot.DataBase
{
    public sealed class MilitaryEquipmentStockRecord
    {
        public int CountryId { get; set; }
        public string ResourceType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public static class MilitaryEquipmentStocksData
    {
        static MilitaryEquipmentStocksData()
        {
            BaseRepository.DBInitialization();
        }

        private static SqlParameter DecimalParam(string name, decimal value, byte precision = 18, byte scale = 2)
        {
            return new SqlParameter(name, SqlDbType.Decimal)
            {
                Precision = precision,
                Scale = scale,
                Value = value
            };
        }

        public static List<MilitaryEquipmentStockRecord> GetByCountry(int countryId)
        {
            const string q = @"
SELECT CountryId, ResourceType, Quantity, UpdatedAt
FROM MilitaryEquipmentStocks
WHERE CountryId = @CountryId
ORDER BY ResourceType;";

            var list = new List<MilitaryEquipmentStockRecord>();
            using var reader = BaseRepository.ExecuteReader(
                q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId));
            try
            {
                while (reader.Read())
                {
                    list.Add(new MilitaryEquipmentStockRecord
                    {
                        CountryId = reader.GetInt32(reader.GetOrdinal("CountryId")),
                        ResourceType = reader.GetString(reader.GetOrdinal("ResourceType")),
                        Quantity = reader.GetDecimal(reader.GetOrdinal("Quantity")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                    });
                }
            }
            finally
            {
                reader.Dispose();
            }

            return list;
        }

        public static decimal GetQuantity(int countryId, string resourceType)
        {
            if (string.IsNullOrWhiteSpace(resourceType))
                return 0m;

            const string q = @"
SELECT Quantity
FROM MilitaryEquipmentStocks
WHERE CountryId = @CountryId AND ResourceType = @ResourceType;";

            object? value = BaseRepository.ExecuteScalar(
                q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId),
                BaseRepository.Param("@ResourceType", SqlDbType.NVarChar, resourceType.Trim(), 30));

            if (value == null || value == DBNull.Value)
                return 0m;

            return Convert.ToDecimal(value);
        }

        public static bool AddQuantity(int countryId, string resourceType, decimal amount)
        {
            if (countryId <= 0 || string.IsNullOrWhiteSpace(resourceType))
                return false;

            decimal delta = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
            if (delta == 0m)
                return true;

            const string q = @"
UPDATE MilitaryEquipmentStocks
SET Quantity = Quantity + @Delta,
    UpdatedAt = SYSUTCDATETIME()
WHERE CountryId = @CountryId AND ResourceType = @ResourceType;

IF @@ROWCOUNT = 0
BEGIN
    INSERT INTO MilitaryEquipmentStocks (CountryId, ResourceType, Quantity, UpdatedAt)
    VALUES (@CountryId, @ResourceType, @Delta, SYSUTCDATETIME());
END";

            int rows = BaseRepository.ExecuteNonQuery(
                q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId),
                BaseRepository.Param("@ResourceType", SqlDbType.NVarChar, resourceType.Trim(), 30),
                DecimalParam("@Delta", delta));

            if (rows > 0)
            {
                DeleteNonPositive(countryId, resourceType);
                return true;
            }

            return false;
        }

        public static bool SetQuantity(int countryId, string resourceType, decimal amount)
        {
            if (countryId <= 0 || string.IsNullOrWhiteSpace(resourceType))
                return false;

            string key = resourceType.Trim();
            decimal qty = decimal.Round(Math.Max(0m, amount), 2, MidpointRounding.AwayFromZero);

            if (qty <= 0m)
                return DeleteRecord(countryId, key);

            const string q = @"
UPDATE MilitaryEquipmentStocks
SET Quantity = @Quantity,
    UpdatedAt = SYSUTCDATETIME()
WHERE CountryId = @CountryId AND ResourceType = @ResourceType;

IF @@ROWCOUNT = 0
BEGIN
    INSERT INTO MilitaryEquipmentStocks (CountryId, ResourceType, Quantity, UpdatedAt)
    VALUES (@CountryId, @ResourceType, @Quantity, SYSUTCDATETIME());
END";

            int rows = BaseRepository.ExecuteNonQuery(
                q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId),
                BaseRepository.Param("@ResourceType", SqlDbType.NVarChar, key, 30),
                DecimalParam("@Quantity", qty));

            return rows > 0;
        }

        public static decimal ConsumeQuantity(int countryId, string resourceType, decimal amount)
        {
            if (countryId <= 0 || string.IsNullOrWhiteSpace(resourceType))
                return 0m;

            decimal want = decimal.Round(Math.Max(0m, amount), 2, MidpointRounding.AwayFromZero);
            if (want <= 0m)
                return 0m;

            decimal current = GetQuantity(countryId, resourceType);
            if (current <= 0m)
                return 0m;

            decimal consumed = Math.Min(current, want);
            decimal remaining = current - consumed;
            SetQuantity(countryId, resourceType, remaining);
            return consumed;
        }

        public static int ClearByCountry(int countryId)
        {
            const string q = "DELETE FROM MilitaryEquipmentStocks WHERE CountryId = @CountryId";
            return BaseRepository.ExecuteNonQuery(
                q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId));
        }

        private static bool DeleteRecord(int countryId, string resourceType)
        {
            const string q = "DELETE FROM MilitaryEquipmentStocks WHERE CountryId = @CountryId AND ResourceType = @ResourceType";
            BaseRepository.ExecuteNonQuery(
                q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId),
                BaseRepository.Param("@ResourceType", SqlDbType.NVarChar, resourceType, 30));
            return true;
        }

        private static void DeleteNonPositive(int countryId, string resourceType)
        {
            const string q = @"
DELETE FROM MilitaryEquipmentStocks
WHERE CountryId = @CountryId
  AND ResourceType = @ResourceType
  AND Quantity <= 0;";

            BaseRepository.ExecuteNonQuery(
                q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId),
                BaseRepository.Param("@ResourceType", SqlDbType.NVarChar, resourceType.Trim(), 30));
        }
    }
}
