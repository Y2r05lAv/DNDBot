#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DNDBot.DataBase
{
    public sealed class EconomyCountryProductSanctionRecord
    {
        public int SanctionId { get; set; }
        public int SourceCountryId { get; set; }
        public int TargetCountryId { get; set; }
        public string ResourceType { get; set; } = string.Empty;
        public int ExportLevel { get; set; }
        public int ImportLevel { get; set; }
        public float ExportEffectiveness { get; set; }
        public float ImportEffectiveness { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public static class EconomyCountryProductSanctionsData
    {
        static EconomyCountryProductSanctionsData()
        {
            BaseRepository.DBInitialization();
        }

        public static List<EconomyCountryProductSanctionRecord> GetBySource(int sourceCountryId)
        {
            const string q = @"
SELECT SanctionId, SourceCountryId, TargetCountryId, ResourceType, ExportLevel, ImportLevel, ExportEffectiveness, ImportEffectiveness, CreatedAt, UpdatedAt
FROM EconomyCountryProductSanctions
WHERE SourceCountryId = @SourceCountryId";

            var list = new List<EconomyCountryProductSanctionRecord>();
            using var reader = BaseRepository.ExecuteReader(q, BaseRepository.Param("@SourceCountryId", SqlDbType.Int, sourceCountryId));
            try
            {
                while (reader.Read())
                {
                    list.Add(new EconomyCountryProductSanctionRecord
                    {
                        SanctionId = reader.GetInt32(reader.GetOrdinal("SanctionId")),
                        SourceCountryId = reader.GetInt32(reader.GetOrdinal("SourceCountryId")),
                        TargetCountryId = reader.GetInt32(reader.GetOrdinal("TargetCountryId")),
                        ResourceType = reader.GetString(reader.GetOrdinal("ResourceType")),
                        ExportLevel = reader.GetInt32(reader.GetOrdinal("ExportLevel")),
                        ImportLevel = reader.GetInt32(reader.GetOrdinal("ImportLevel")),
                        ExportEffectiveness = Convert.ToSingle(reader["ExportEffectiveness"]),
                        ImportEffectiveness = Convert.ToSingle(reader["ImportEffectiveness"]),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
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

        public static EconomyCountryProductSanctionRecord? GetBySourceTargetResource(int sourceCountryId, int targetCountryId, string resourceType)
        {
            const string q = @"
SELECT SanctionId, SourceCountryId, TargetCountryId, ResourceType, ExportLevel, ImportLevel, ExportEffectiveness, ImportEffectiveness, CreatedAt, UpdatedAt
FROM EconomyCountryProductSanctions
WHERE SourceCountryId = @SourceCountryId AND TargetCountryId = @TargetCountryId AND ResourceType = @ResourceType";

            using var reader = BaseRepository.ExecuteReader(q,
                BaseRepository.Param("@SourceCountryId", SqlDbType.Int, sourceCountryId),
                BaseRepository.Param("@TargetCountryId", SqlDbType.Int, targetCountryId),
                BaseRepository.Param("@ResourceType", SqlDbType.NVarChar, resourceType, 30));
            try
            {
                if (reader.Read())
                {
                    return new EconomyCountryProductSanctionRecord
                    {
                        SanctionId = reader.GetInt32(reader.GetOrdinal("SanctionId")),
                        SourceCountryId = reader.GetInt32(reader.GetOrdinal("SourceCountryId")),
                        TargetCountryId = reader.GetInt32(reader.GetOrdinal("TargetCountryId")),
                        ResourceType = reader.GetString(reader.GetOrdinal("ResourceType")),
                        ExportLevel = reader.GetInt32(reader.GetOrdinal("ExportLevel")),
                        ImportLevel = reader.GetInt32(reader.GetOrdinal("ImportLevel")),
                        ExportEffectiveness = Convert.ToSingle(reader["ExportEffectiveness"]),
                        ImportEffectiveness = Convert.ToSingle(reader["ImportEffectiveness"]),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                    };
                }
            }
            finally
            {
                reader.Dispose();
            }

            return null;
        }

        public static void Upsert(int sourceCountryId, int targetCountryId, string resourceType, int exportLevel, int importLevel, float exportEffectiveness, float importEffectiveness)
        {
            const string q = @"
MERGE EconomyCountryProductSanctions AS target
USING (SELECT @SourceCountryId AS SourceCountryId, @TargetCountryId AS TargetCountryId, @ResourceType AS ResourceType) AS src
ON target.SourceCountryId = src.SourceCountryId AND target.TargetCountryId = src.TargetCountryId AND target.ResourceType = src.ResourceType
WHEN MATCHED THEN
    UPDATE SET ExportLevel = @ExportLevel, ImportLevel = @ImportLevel, ExportEffectiveness = @ExportEffectiveness, ImportEffectiveness = @ImportEffectiveness, UpdatedAt = SYSDATETIME()
WHEN NOT MATCHED THEN
    INSERT (SourceCountryId, TargetCountryId, ResourceType, ExportLevel, ImportLevel, ExportEffectiveness, ImportEffectiveness)
    VALUES (@SourceCountryId, @TargetCountryId, @ResourceType, @ExportLevel, @ImportLevel, @ExportEffectiveness, @ImportEffectiveness);";

            BaseRepository.ExecuteNonQuery(q,
                BaseRepository.Param("@SourceCountryId", SqlDbType.Int, sourceCountryId),
                BaseRepository.Param("@TargetCountryId", SqlDbType.Int, targetCountryId),
                BaseRepository.Param("@ResourceType", SqlDbType.NVarChar, resourceType, 30),
                BaseRepository.Param("@ExportLevel", SqlDbType.Int, exportLevel),
                BaseRepository.Param("@ImportLevel", SqlDbType.Int, importLevel),
                BaseRepository.Param("@ExportEffectiveness", SqlDbType.Real, exportEffectiveness),
                BaseRepository.Param("@ImportEffectiveness", SqlDbType.Real, importEffectiveness)
            );
        }

        public static bool Delete(int sourceCountryId, int targetCountryId, string resourceType)
        {
            const string q = @"
DELETE FROM EconomyCountryProductSanctions
WHERE SourceCountryId = @SourceCountryId AND TargetCountryId = @TargetCountryId AND ResourceType = @ResourceType";

            int rows = BaseRepository.ExecuteNonQuery(q,
                BaseRepository.Param("@SourceCountryId", SqlDbType.Int, sourceCountryId),
                BaseRepository.Param("@TargetCountryId", SqlDbType.Int, targetCountryId),
                BaseRepository.Param("@ResourceType", SqlDbType.NVarChar, resourceType, 30));
            return rows > 0;
        }
    }
}

