#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DNDBot.DataBase
{
    public sealed class EconomyCountrySanctionRecord
    {
        public int SanctionId { get; set; }
        public int SourceCountryId { get; set; }
        public int TargetCountryId { get; set; }
        public int ExportLevel { get; set; }
        public int ImportLevel { get; set; }
        public float ExportEffectiveness { get; set; }
        public float ImportEffectiveness { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public static class EconomyCountrySanctionsData
    {
        static EconomyCountrySanctionsData()
        {
            BaseRepository.DBInitialization();
        }

        public static List<EconomyCountrySanctionRecord> GetBySource(int sourceCountryId)
        {
            const string q = @"
SELECT SanctionId, SourceCountryId, TargetCountryId, ExportLevel, ImportLevel, ExportEffectiveness, ImportEffectiveness, CreatedAt, UpdatedAt
FROM EconomyCountrySanctions
WHERE SourceCountryId = @SourceCountryId";

            var list = new List<EconomyCountrySanctionRecord>();
            using var reader = BaseRepository.ExecuteReader(q, BaseRepository.Param("@SourceCountryId", SqlDbType.Int, sourceCountryId));
            try
            {
                while (reader.Read())
                {
                    list.Add(new EconomyCountrySanctionRecord
                    {
                        SanctionId = reader.GetInt32(reader.GetOrdinal("SanctionId")),
                        SourceCountryId = reader.GetInt32(reader.GetOrdinal("SourceCountryId")),
                        TargetCountryId = reader.GetInt32(reader.GetOrdinal("TargetCountryId")),
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

        public static EconomyCountrySanctionRecord? GetBySourceTarget(int sourceCountryId, int targetCountryId)
        {
            const string q = @"
SELECT SanctionId, SourceCountryId, TargetCountryId, ExportLevel, ImportLevel, ExportEffectiveness, ImportEffectiveness, CreatedAt, UpdatedAt
FROM EconomyCountrySanctions
WHERE SourceCountryId = @SourceCountryId AND TargetCountryId = @TargetCountryId";

            using var reader = BaseRepository.ExecuteReader(q,
                BaseRepository.Param("@SourceCountryId", SqlDbType.Int, sourceCountryId),
                BaseRepository.Param("@TargetCountryId", SqlDbType.Int, targetCountryId));
            try
            {
                if (reader.Read())
                {
                    return new EconomyCountrySanctionRecord
                    {
                        SanctionId = reader.GetInt32(reader.GetOrdinal("SanctionId")),
                        SourceCountryId = reader.GetInt32(reader.GetOrdinal("SourceCountryId")),
                        TargetCountryId = reader.GetInt32(reader.GetOrdinal("TargetCountryId")),
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

        public static void Upsert(int sourceCountryId, int targetCountryId, int exportLevel, int importLevel, float exportEffectiveness, float importEffectiveness)
        {
            const string q = @"
MERGE EconomyCountrySanctions AS target
USING (SELECT @SourceCountryId AS SourceCountryId, @TargetCountryId AS TargetCountryId) AS src
ON target.SourceCountryId = src.SourceCountryId AND target.TargetCountryId = src.TargetCountryId
WHEN MATCHED THEN
    UPDATE SET ExportLevel = @ExportLevel, ImportLevel = @ImportLevel, ExportEffectiveness = @ExportEffectiveness, ImportEffectiveness = @ImportEffectiveness, UpdatedAt = SYSDATETIME()
WHEN NOT MATCHED THEN
    INSERT (SourceCountryId, TargetCountryId, ExportLevel, ImportLevel, ExportEffectiveness, ImportEffectiveness)
    VALUES (@SourceCountryId, @TargetCountryId, @ExportLevel, @ImportLevel, @ExportEffectiveness, @ImportEffectiveness);";

            BaseRepository.ExecuteNonQuery(q,
                BaseRepository.Param("@SourceCountryId", SqlDbType.Int, sourceCountryId),
                BaseRepository.Param("@TargetCountryId", SqlDbType.Int, targetCountryId),
                BaseRepository.Param("@ExportLevel", SqlDbType.Int, exportLevel),
                BaseRepository.Param("@ImportLevel", SqlDbType.Int, importLevel),
                BaseRepository.Param("@ExportEffectiveness", SqlDbType.Real, exportEffectiveness),
                BaseRepository.Param("@ImportEffectiveness", SqlDbType.Real, importEffectiveness)
            );
        }

        public static bool Delete(int sourceCountryId, int targetCountryId)
        {
            const string q = @"
DELETE FROM EconomyCountrySanctions
WHERE SourceCountryId = @SourceCountryId AND TargetCountryId = @TargetCountryId";

            int rows = BaseRepository.ExecuteNonQuery(q,
                BaseRepository.Param("@SourceCountryId", SqlDbType.Int, sourceCountryId),
                BaseRepository.Param("@TargetCountryId", SqlDbType.Int, targetCountryId));
            return rows > 0;
        }
    }
}

