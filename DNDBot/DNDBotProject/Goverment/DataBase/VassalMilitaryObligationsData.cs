#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DNDBot.DataBase
{
    public sealed class VassalMilitaryObligationRecord
    {
        public int ObligationId { get; set; }
        public int CountryId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public int RegimentsProvided { get; set; }
        public decimal FundingPerTurn { get; set; }
        public float Loyalty { get; set; }
        public float RebellionRisk { get; set; }
        public bool IsActive { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public static class VassalMilitaryObligationsData
    {
        static VassalMilitaryObligationsData()
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

        public static List<VassalMilitaryObligationRecord> GetByCountry(int countryId)
        {
            const string q = @"
SELECT ObligationId, CountryId, SubjectName, RegimentsProvided, FundingPerTurn, Loyalty, RebellionRisk, IsActive, LastUpdated
FROM VassalMilitaryObligations
WHERE CountryId = @CountryId
ORDER BY ObligationId;";

            var list = new List<VassalMilitaryObligationRecord>();
            try
            {
                using var reader = BaseRepository.ExecuteReader(q, BaseRepository.Param("@CountryId", SqlDbType.Int, countryId));
                try
                {
                    while (reader.Read())
                    {
                        list.Add(new VassalMilitaryObligationRecord
                        {
                            ObligationId = reader.GetInt32(reader.GetOrdinal("ObligationId")),
                            CountryId = reader.GetInt32(reader.GetOrdinal("CountryId")),
                            SubjectName = reader.IsDBNull(reader.GetOrdinal("SubjectName")) ? string.Empty : reader.GetString(reader.GetOrdinal("SubjectName")),
                            RegimentsProvided = reader.GetInt32(reader.GetOrdinal("RegimentsProvided")),
                            FundingPerTurn = reader.GetDecimal(reader.GetOrdinal("FundingPerTurn")),
                            Loyalty = Convert.ToSingle(reader["Loyalty"]),
                            RebellionRisk = Convert.ToSingle(reader["RebellionRisk"]),
                            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                            LastUpdated = reader.GetDateTime(reader.GetOrdinal("LastUpdated"))
                        });
                    }
                }
                finally
                {
                    reader.Dispose();
                }
            }
            catch
            {
                return new List<VassalMilitaryObligationRecord>();
            }

            return list;
        }

        public static int Add(VassalMilitaryObligationRecord record)
        {
            const string q = @"
INSERT INTO VassalMilitaryObligations (
    CountryId, SubjectName, RegimentsProvided, FundingPerTurn, Loyalty, RebellionRisk, IsActive, LastUpdated
)
VALUES (
    @CountryId, @SubjectName, @RegimentsProvided, @FundingPerTurn, @Loyalty, @RebellionRisk, @IsActive, @LastUpdated
);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            try
            {
                var idObj = BaseRepository.ExecuteScalar(q,
                    BaseRepository.Param("@CountryId", SqlDbType.Int, record.CountryId),
                    BaseRepository.Param("@SubjectName", SqlDbType.NVarChar, record.SubjectName, 100),
                    BaseRepository.Param("@RegimentsProvided", SqlDbType.Int, record.RegimentsProvided),
                    DecimalParam("@FundingPerTurn", record.FundingPerTurn),
                    BaseRepository.Param("@Loyalty", SqlDbType.Real, record.Loyalty),
                    BaseRepository.Param("@RebellionRisk", SqlDbType.Real, record.RebellionRisk),
                    BaseRepository.Param("@IsActive", SqlDbType.Bit, record.IsActive),
                    BaseRepository.Param("@LastUpdated", SqlDbType.DateTime2, DateTime.UtcNow)
                );
                return idObj == null || idObj == DBNull.Value ? 0 : Convert.ToInt32(idObj);
            }
            catch
            {
                return 0;
            }
        }

        public static bool Update(VassalMilitaryObligationRecord record)
        {
            const string q = @"
UPDATE VassalMilitaryObligations
SET SubjectName = @SubjectName,
    RegimentsProvided = @RegimentsProvided,
    FundingPerTurn = @FundingPerTurn,
    Loyalty = @Loyalty,
    RebellionRisk = @RebellionRisk,
    IsActive = @IsActive,
    LastUpdated = @LastUpdated
WHERE ObligationId = @ObligationId AND CountryId = @CountryId;";

            try
            {
                int rows = BaseRepository.ExecuteNonQuery(q,
                    BaseRepository.Param("@ObligationId", SqlDbType.Int, record.ObligationId),
                    BaseRepository.Param("@CountryId", SqlDbType.Int, record.CountryId),
                    BaseRepository.Param("@SubjectName", SqlDbType.NVarChar, record.SubjectName, 100),
                    BaseRepository.Param("@RegimentsProvided", SqlDbType.Int, record.RegimentsProvided),
                    DecimalParam("@FundingPerTurn", record.FundingPerTurn),
                    BaseRepository.Param("@Loyalty", SqlDbType.Real, record.Loyalty),
                    BaseRepository.Param("@RebellionRisk", SqlDbType.Real, record.RebellionRisk),
                    BaseRepository.Param("@IsActive", SqlDbType.Bit, record.IsActive),
                    BaseRepository.Param("@LastUpdated", SqlDbType.DateTime2, DateTime.UtcNow)
                );
                return rows > 0;
            }
            catch
            {
                return false;
            }
        }

        public static bool Delete(int obligationId, int countryId)
        {
            const string q = "DELETE FROM VassalMilitaryObligations WHERE ObligationId = @ObligationId AND CountryId = @CountryId;";
            try
            {
                int rows = BaseRepository.ExecuteNonQuery(q,
                    BaseRepository.Param("@ObligationId", SqlDbType.Int, obligationId),
                    BaseRepository.Param("@CountryId", SqlDbType.Int, countryId));
                return rows > 0;
            }
            catch
            {
                return false;
            }
        }

        public static bool UpdateField(int obligationId, int countryId, string fieldName, object value, SqlDbType type)
        {
            string column = fieldName switch
            {
                "SubjectName" => "SubjectName",
                "RegimentsProvided" => "RegimentsProvided",
                "FundingPerTurn" => "FundingPerTurn",
                "Loyalty" => "Loyalty",
                "RebellionRisk" => "RebellionRisk",
                "IsActive" => "IsActive",
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(column))
                return false;

            string q = $"UPDATE VassalMilitaryObligations SET {column} = @Value, LastUpdated = @UpdatedAt WHERE ObligationId = @ObligationId AND CountryId = @CountryId;";
            try
            {
                int rows = BaseRepository.ExecuteNonQuery(
                    q,
                    BaseRepository.Param("@Value", type, value ?? DBNull.Value),
                    BaseRepository.Param("@UpdatedAt", SqlDbType.DateTime2, DateTime.UtcNow),
                    BaseRepository.Param("@ObligationId", SqlDbType.Int, obligationId),
                    BaseRepository.Param("@CountryId", SqlDbType.Int, countryId)
                );
                return rows > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
