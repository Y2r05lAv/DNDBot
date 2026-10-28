#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DNDBot.DataBase
{
    public sealed class EconomyTradeAgreementRecord
    {
        public int AgreementId { get; set; }
        public int CountryId { get; set; }
        public int? PartnerCountryId { get; set; }
        public string PartnerName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public float ExportBonus { get; set; }
        public float ImportBonus { get; set; }
        public string? ResourceType { get; set; }  
        public string? ExportResourceType { get; set; }
        public string? ImportResourceType { get; set; }
        public decimal ExportQty { get; set; }
        public decimal ExportUnitPrice { get; set; }
        public decimal ImportQty { get; set; }
        public decimal ImportUnitPrice { get; set; }
        public int PeriodDays { get; set; }
        public string DeliveryMode { get; set; } = "Caravan";
        public bool QuantityIsPerDay { get; set; } = true;
    }

    public static class EconomyTradeAgreementsData
    {
        static EconomyTradeAgreementsData()
        {
            BaseRepository.DBInitialization();
        }

        public static List<EconomyTradeAgreementRecord> GetByCountry(int countryId)
        {
            const string q = @"
SELECT AgreementId, CountryId, PartnerCountryId, PartnerName, IsActive, ExportBonus, ImportBonus,
       ResourceType, ExportResourceType, ImportResourceType, ExportQty, ExportUnitPrice, ImportQty, ImportUnitPrice, PeriodDays,
       DeliveryMode, QuantityIsPerDay
FROM EconomyTradeAgreements
WHERE CountryId = @CountryId";

            var list = new List<EconomyTradeAgreementRecord>();
            using var reader = BaseRepository.ExecuteReader(q, BaseRepository.Param("@CountryId", SqlDbType.Int, countryId));
            try
            {
                while (reader.Read())
                {
                    list.Add(new EconomyTradeAgreementRecord
                    {
                        AgreementId = reader.GetInt32(reader.GetOrdinal("AgreementId")),
                        CountryId = reader.GetInt32(reader.GetOrdinal("CountryId")),
                        PartnerCountryId = reader.IsDBNull(reader.GetOrdinal("PartnerCountryId")) ? null : reader.GetInt32(reader.GetOrdinal("PartnerCountryId")),
                        PartnerName = reader.GetString(reader.GetOrdinal("PartnerName")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                        ExportBonus = Convert.ToSingle(reader["ExportBonus"]),
                        ImportBonus = Convert.ToSingle(reader["ImportBonus"]),
                        ResourceType = reader.IsDBNull(reader.GetOrdinal("ResourceType")) ? null : reader.GetString(reader.GetOrdinal("ResourceType")),
                        ExportResourceType = reader.IsDBNull(reader.GetOrdinal("ExportResourceType")) ? null : reader.GetString(reader.GetOrdinal("ExportResourceType")),
                        ImportResourceType = reader.IsDBNull(reader.GetOrdinal("ImportResourceType")) ? null : reader.GetString(reader.GetOrdinal("ImportResourceType")),
                        ExportQty = reader.GetDecimal(reader.GetOrdinal("ExportQty")),
                        ExportUnitPrice = reader.GetDecimal(reader.GetOrdinal("ExportUnitPrice")),
                        ImportQty = reader.GetDecimal(reader.GetOrdinal("ImportQty")),
                        ImportUnitPrice = reader.GetDecimal(reader.GetOrdinal("ImportUnitPrice")),
                        PeriodDays = reader.GetInt32(reader.GetOrdinal("PeriodDays")),
                        DeliveryMode = reader.IsDBNull(reader.GetOrdinal("DeliveryMode"))
                            ? "Caravan"
                            : reader.GetString(reader.GetOrdinal("DeliveryMode")),
                        QuantityIsPerDay = reader.IsDBNull(reader.GetOrdinal("QuantityIsPerDay"))
                            ? true
                            : reader.GetBoolean(reader.GetOrdinal("QuantityIsPerDay"))
                    });
                }
            }
            finally
            {
                reader.Dispose();
            }

            foreach (var a in list)
            {
                if (string.IsNullOrWhiteSpace(a.ExportResourceType) && !string.IsNullOrWhiteSpace(a.ResourceType))
                    a.ExportResourceType = a.ResourceType;
                if (string.IsNullOrWhiteSpace(a.ImportResourceType) && !string.IsNullOrWhiteSpace(a.ResourceType))
                    a.ImportResourceType = a.ResourceType;
            }
            return list;
        }

        public static List<EconomyTradeAgreementRecord> GetActiveByCountry(int countryId)
        {
            const string q = @"
SELECT AgreementId, CountryId, PartnerCountryId, PartnerName, IsActive, ExportBonus, ImportBonus,
       ResourceType, ExportResourceType, ImportResourceType, ExportQty, ExportUnitPrice, ImportQty, ImportUnitPrice, PeriodDays,
       DeliveryMode, QuantityIsPerDay
FROM EconomyTradeAgreements
WHERE CountryId = @CountryId AND IsActive = 1";

            var list = new List<EconomyTradeAgreementRecord>();
            using var reader = BaseRepository.ExecuteReader(q, BaseRepository.Param("@CountryId", SqlDbType.Int, countryId));
            try
            {
                while (reader.Read())
                {
                    list.Add(new EconomyTradeAgreementRecord
                    {
                        AgreementId = reader.GetInt32(reader.GetOrdinal("AgreementId")),
                        CountryId = reader.GetInt32(reader.GetOrdinal("CountryId")),
                        PartnerCountryId = reader.IsDBNull(reader.GetOrdinal("PartnerCountryId")) ? null : reader.GetInt32(reader.GetOrdinal("PartnerCountryId")),
                        PartnerName = reader.GetString(reader.GetOrdinal("PartnerName")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                        ExportBonus = Convert.ToSingle(reader["ExportBonus"]),
                        ImportBonus = Convert.ToSingle(reader["ImportBonus"]),
                        ResourceType = reader.IsDBNull(reader.GetOrdinal("ResourceType")) ? null : reader.GetString(reader.GetOrdinal("ResourceType")),
                        ExportResourceType = reader.IsDBNull(reader.GetOrdinal("ExportResourceType")) ? null : reader.GetString(reader.GetOrdinal("ExportResourceType")),
                        ImportResourceType = reader.IsDBNull(reader.GetOrdinal("ImportResourceType")) ? null : reader.GetString(reader.GetOrdinal("ImportResourceType")),
                        ExportQty = reader.GetDecimal(reader.GetOrdinal("ExportQty")),
                        ExportUnitPrice = reader.GetDecimal(reader.GetOrdinal("ExportUnitPrice")),
                        ImportQty = reader.GetDecimal(reader.GetOrdinal("ImportQty")),
                        ImportUnitPrice = reader.GetDecimal(reader.GetOrdinal("ImportUnitPrice")),
                        PeriodDays = reader.GetInt32(reader.GetOrdinal("PeriodDays")),
                        DeliveryMode = reader.IsDBNull(reader.GetOrdinal("DeliveryMode"))
                            ? "Caravan"
                            : reader.GetString(reader.GetOrdinal("DeliveryMode")),
                        QuantityIsPerDay = reader.IsDBNull(reader.GetOrdinal("QuantityIsPerDay"))
                            ? true
                            : reader.GetBoolean(reader.GetOrdinal("QuantityIsPerDay"))
                    });
                }
            }
            finally
            {
                reader.Dispose();
            }

            foreach (var a in list)
            {
                if (string.IsNullOrWhiteSpace(a.ExportResourceType) && !string.IsNullOrWhiteSpace(a.ResourceType))
                    a.ExportResourceType = a.ResourceType;
                if (string.IsNullOrWhiteSpace(a.ImportResourceType) && !string.IsNullOrWhiteSpace(a.ResourceType))
                    a.ImportResourceType = a.ResourceType;
            }
            return list;
        }

        public static int AddAgreement(EconomyTradeAgreementRecord agreement)
        {
            const string q = @"
INSERT INTO EconomyTradeAgreements (
    CountryId, PartnerCountryId, PartnerName, IsActive, ExportBonus, ImportBonus,
    ResourceType, ExportResourceType, ImportResourceType, ExportQty, ExportUnitPrice, ImportQty, ImportUnitPrice, PeriodDays,
    DeliveryMode, QuantityIsPerDay
)
VALUES (
    @CountryId, @PartnerCountryId, @PartnerName, @IsActive, @ExportBonus, @ImportBonus,
    @ResourceType, @ExportResourceType, @ImportResourceType, @ExportQty, @ExportUnitPrice, @ImportQty, @ImportUnitPrice, @PeriodDays,
    @DeliveryMode, @QuantityIsPerDay
);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var idObj = BaseRepository.ExecuteScalar(q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, agreement.CountryId),
                BaseRepository.Param("@PartnerCountryId", SqlDbType.Int, (object?)agreement.PartnerCountryId ?? DBNull.Value),
                BaseRepository.Param("@PartnerName", SqlDbType.NVarChar, agreement.PartnerName, 80),
                BaseRepository.Param("@IsActive", SqlDbType.Bit, agreement.IsActive),
                BaseRepository.Param("@ExportBonus", SqlDbType.Real, agreement.ExportBonus),
                BaseRepository.Param("@ImportBonus", SqlDbType.Real, agreement.ImportBonus),
                BaseRepository.Param("@ResourceType", SqlDbType.NVarChar, (object?)agreement.ResourceType ?? DBNull.Value, 30),
                BaseRepository.Param("@ExportResourceType", SqlDbType.NVarChar, (object?)agreement.ExportResourceType ?? DBNull.Value, 30),
                BaseRepository.Param("@ImportResourceType", SqlDbType.NVarChar, (object?)agreement.ImportResourceType ?? DBNull.Value, 30),
                BaseRepository.Param("@ExportQty", SqlDbType.Decimal, agreement.ExportQty),
                BaseRepository.Param("@ExportUnitPrice", SqlDbType.Decimal, agreement.ExportUnitPrice),
                BaseRepository.Param("@ImportQty", SqlDbType.Decimal, agreement.ImportQty),
                BaseRepository.Param("@ImportUnitPrice", SqlDbType.Decimal, agreement.ImportUnitPrice),
                BaseRepository.Param("@PeriodDays", SqlDbType.Int, agreement.PeriodDays),
                BaseRepository.Param("@DeliveryMode", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(agreement.DeliveryMode) ? "Caravan" : agreement.DeliveryMode, 30),
                BaseRepository.Param("@QuantityIsPerDay", SqlDbType.Bit, agreement.QuantityIsPerDay)
            );

            if (idObj == null || idObj == DBNull.Value)
                return 0;
            return Convert.ToInt32(idObj);
        }

        public static bool EndAgreement(int agreementId, int countryId)
        {
            const string q = @"
UPDATE EconomyTradeAgreements
SET IsActive = 0
WHERE AgreementId = @AgreementId AND CountryId = @CountryId";

            int rows = BaseRepository.ExecuteNonQuery(q,
                BaseRepository.Param("@AgreementId", SqlDbType.Int, agreementId),
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId));
            return rows > 0;
        }

        public static bool UpdateAgreementField(int agreementId, int countryId, string fieldName, object value, SqlDbType type)
        {
            string column = fieldName switch
            {
                "PartnerName" => "PartnerName",
                "ExportBonus" => "ExportBonus",
                "ImportBonus" => "ImportBonus",
                "IsActive" => "IsActive",
                "PartnerCountryId" => "PartnerCountryId",
                "ResourceType" => "ResourceType",
                "ExportResourceType" => "ExportResourceType",
                "ImportResourceType" => "ImportResourceType",
                "ExportQty" => "ExportQty",
                "ExportUnitPrice" => "ExportUnitPrice",
                "ImportQty" => "ImportQty",
                "ImportUnitPrice" => "ImportUnitPrice",
                "PeriodDays" => "PeriodDays",
                "DeliveryMode" => "DeliveryMode",
                "QuantityIsPerDay" => "QuantityIsPerDay",
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(column))
                return false;

            string q = $"UPDATE EconomyTradeAgreements SET {column} = @Value WHERE AgreementId = @AgreementId AND CountryId = @CountryId";
            int rows = BaseRepository.ExecuteNonQuery(q,
                BaseRepository.Param("@AgreementId", SqlDbType.Int, agreementId),
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId),
                BaseRepository.Param("@Value", type, value)
            );
            return rows > 0;
        }
    }
}

