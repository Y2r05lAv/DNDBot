using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DNDBot.DataBase
{
     
    public class LawRecord
    {
        public int LawId { get; set; }
        public int CountryId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string EffectType { get; set; }
        public decimal EffectValue { get; set; }
        public int CreatedByCharacterId { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int? ParentLawId { get; set; }
    }

    public static class LawsData
    {
        static LawsData()
        {
            BaseRepository.DBInitialization();
        }

        private static SqlParameter DecimalParam(string name, decimal value, byte precision, byte scale)
        {
            return new SqlParameter(name, SqlDbType.Decimal)
            {
                Precision = precision,
                Scale = scale,
                Value = value
            };
        }

        private static SqlParameter NVarCharMaxParam(string name, string value)
        {
            return new SqlParameter(name, SqlDbType.NVarChar)
            {
                Size = -1,
                Value = value ?? (object)DBNull.Value
            };
        }

        public static string AddLaw(LawRecord law)
        {
            const string insert = @"
INSERT INTO Laws (CountryId, Title, Description, Status, EffectType, EffectValue, CreatedByCharacterId, ExpiresAt, ParentLawId)
VALUES (@CountryId, @Title, @Description, @Status, @EffectType, @EffectValue, @CreatedByCharacterId, @ExpiresAt, @ParentLawId);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var idObj = BaseRepository.ExecuteScalar(insert,
                BaseRepository.Param("@CountryId", SqlDbType.Int, law.CountryId),
                BaseRepository.Param("@Title", SqlDbType.NVarChar, law.Title, 500),
                NVarCharMaxParam("@Description", law.Description),
                BaseRepository.Param("@Status", SqlDbType.NVarChar, law.Status, 20),
                BaseRepository.Param("@EffectType", SqlDbType.NVarChar, law.EffectType, 500),
                DecimalParam("@EffectValue", law.EffectValue, 10, 2),
                BaseRepository.Param("@CreatedByCharacterId", SqlDbType.Int, law.CreatedByCharacterId),
                BaseRepository.Param("@ExpiresAt", SqlDbType.DateTime2, law.ExpiresAt),
                BaseRepository.Param("@ParentLawId", SqlDbType.Int, (object?)law.ParentLawId ?? DBNull.Value)
            );

            if (idObj == null || idObj == DBNull.Value)
                return "Помилка додавання закону.";

            return $"Закон додано з Id = {Convert.ToInt32(idObj)}";
        }

        public static string UpdateLaw(LawRecord law)
        {
            const string update = @"
UPDATE Laws
SET CountryId = @CountryId,
    Title = @Title,
    Description = @Description,
    Status = @Status,
    EffectType = @EffectType,
    EffectValue = @EffectValue,
    CreatedByCharacterId = @CreatedByCharacterId,
    ExpiresAt = @ExpiresAt,
    ParentLawId = @ParentLawId
WHERE LawId = @LawId";

            int rows = BaseRepository.ExecuteNonQuery(update,
                BaseRepository.Param("@CountryId", SqlDbType.Int, law.CountryId),
                BaseRepository.Param("@Title", SqlDbType.NVarChar, law.Title, 500),
                NVarCharMaxParam("@Description", law.Description),
                BaseRepository.Param("@Status", SqlDbType.NVarChar, law.Status, 20),
                BaseRepository.Param("@EffectType", SqlDbType.NVarChar, law.EffectType, 500),
                DecimalParam("@EffectValue", law.EffectValue, 10, 2),
                BaseRepository.Param("@CreatedByCharacterId", SqlDbType.Int, law.CreatedByCharacterId),
                BaseRepository.Param("@ExpiresAt", SqlDbType.DateTime2, law.ExpiresAt),
                BaseRepository.Param("@ParentLawId", SqlDbType.Int, (object?)law.ParentLawId ?? DBNull.Value),
                BaseRepository.Param("@LawId", SqlDbType.Int, law.LawId)
            );

            return rows > 0 ? "Закон оновлений." : "Закон не знайдено або нічого не змінено.";
        }

        public static string DeleteLaw(int lawId)
        {
            const string delete = "DELETE FROM Laws WHERE LawId = @LawId";
            int rows = BaseRepository.ExecuteNonQuery(delete,
                BaseRepository.Param("@LawId", SqlDbType.Int, lawId));
            return rows > 0 ? "Закон видалений." : "Закон не знайдено.";
        }

        public static LawRecord GetLawById(int lawId)
        {
            const string q = @"
SELECT LawId, CountryId, Title, Description, Status, EffectType, EffectValue, CreatedByCharacterId, ExpiresAt, ParentLawId
FROM Laws
WHERE LawId = @LawId";

            using var reader = BaseRepository.ExecuteReader(q,
                BaseRepository.Param("@LawId", SqlDbType.Int, lawId));
            try
            {
                if (reader.Read())
                {
                    return MapLaw(reader);
                }
            }
            finally
            {
                reader.Dispose();
            }

            return null;
        }

        public static List<LawRecord> GetAllLaws()
        {
            const string q = @"
SELECT LawId, CountryId, Title, Description, Status, EffectType, EffectValue, CreatedByCharacterId, ExpiresAt, ParentLawId
FROM Laws
ORDER BY LawId";

            var list = new List<LawRecord>();
            using var reader = BaseRepository.ExecuteReader(q);
            try
            {
                while (reader.Read())
                {
                    list.Add(MapLaw(reader));
                }
            }
            finally
            {
                reader.Dispose();
            }
            return list;
        }

        public static List<LawRecord> GetByCountry(int countryId)
        {
            const string q = @"
SELECT LawId, CountryId, Title, Description, Status, EffectType, EffectValue, CreatedByCharacterId, ExpiresAt, ParentLawId
FROM Laws
WHERE CountryId = @CountryId
ORDER BY LawId;";

            var list = new List<LawRecord>();
            using var reader = BaseRepository.ExecuteReader(
                q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId)
            );
            try
            {
                while (reader.Read())
                    list.Add(MapLaw(reader));
            }
            finally
            {
                reader.Dispose();
            }

            return list;
        }

        public static List<LawRecord> GetByCountryAndStatus(int countryId, string status)
        {
            const string q = @"
SELECT LawId, CountryId, Title, Description, Status, EffectType, EffectValue, CreatedByCharacterId, ExpiresAt, ParentLawId
FROM Laws
WHERE CountryId = @CountryId AND Status = @Status
ORDER BY LawId;";

            var list = new List<LawRecord>();
            using var reader = BaseRepository.ExecuteReader(
                q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId),
                BaseRepository.Param("@Status", SqlDbType.NVarChar, status, 20)
            );
            try
            {
                while (reader.Read())
                    list.Add(MapLaw(reader));
            }
            finally
            {
                reader.Dispose();
            }

            return list;
        }

        private static LawRecord MapLaw(SqlDataReader reader)
        {
            return new LawRecord
            {
                LawId = reader.GetInt32(reader.GetOrdinal("LawId")),
                CountryId = reader.GetInt32(reader.GetOrdinal("CountryId")),
                Title = reader.IsDBNull(reader.GetOrdinal("Title")) ? string.Empty : reader.GetString(reader.GetOrdinal("Title")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? string.Empty : reader.GetString(reader.GetOrdinal("Status")),
                EffectType = reader.IsDBNull(reader.GetOrdinal("EffectType")) ? string.Empty : reader.GetString(reader.GetOrdinal("EffectType")),
                EffectValue = reader.GetDecimal(reader.GetOrdinal("EffectValue")),
                CreatedByCharacterId = reader.GetInt32(reader.GetOrdinal("CreatedByCharacterId")),
                ExpiresAt = reader.IsDBNull(reader.GetOrdinal("ExpiresAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ExpiresAt")),
                ParentLawId = reader.IsDBNull(reader.GetOrdinal("ParentLawId")) ? null : reader.GetInt32(reader.GetOrdinal("ParentLawId"))
            };
        }
    }
}
