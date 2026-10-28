using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DNDBot.DataBase
{
    public static class CountryDiseasesData
    {
        public sealed class CountryDiseaseRecord
        {
            public int DiseaseId { get; set; }
            public int CountryId { get; set; }
            public string Name { get; set; } = string.Empty;
            public int Severity { get; set; } = 1;
            public int Infectiousness { get; set; } = 1;
            public int DurationTicks { get; set; } = 1;
            public int RemainingTicks { get; set; } = 1;
            public int Stage { get; set; } = 1;
            public string TargetScope { get; set; } = "whole_state";
            public bool Active { get; set; } = true;
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        }

        static CountryDiseasesData()
        {
            BaseRepository.DBInitialization();
        }

        public static List<CountryDiseaseRecord> GetByCountry(int countryId, bool activeOnly = false)
        {
            string q = @"
SELECT DiseaseId, CountryId, [Name], Severity, Infectiousness, DurationTicks, RemainingTicks, Stage, TargetScope, Active, CreatedAt, UpdatedAt
FROM CountryDiseases
WHERE CountryId = @CountryId";
            if (activeOnly)
                q += " AND Active = 1";
            q += " ORDER BY Active DESC, DiseaseId";

            var list = new List<CountryDiseaseRecord>();
            using var reader = BaseRepository.ExecuteReader(q, BaseRepository.Param("@CountryId", SqlDbType.Int, countryId));
            try
            {
                while (reader.Read())
                    list.Add(Map(reader));
            }
            finally
            {
                reader.Dispose();
            }

            return list;
        }

        public static CountryDiseaseRecord? GetActiveByName(int countryId, string name)
        {
            const string q = @"
SELECT TOP (1) DiseaseId, CountryId, [Name], Severity, Infectiousness, DurationTicks, RemainingTicks, Stage, TargetScope, Active, CreatedAt, UpdatedAt
FROM CountryDiseases
WHERE CountryId = @CountryId
  AND Active = 1
  AND [Name] = @Name
ORDER BY DiseaseId DESC";

            using var reader = BaseRepository.ExecuteReader(
                q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId),
                BaseRepository.Param("@Name", SqlDbType.NVarChar, name ?? string.Empty, 80));
            try
            {
                if (reader.Read())
                    return Map(reader);
            }
            finally
            {
                reader.Dispose();
            }

            return null;
        }

        public static int Add(CountryDiseaseRecord record)
        {
            const string q = @"
INSERT INTO CountryDiseases (
    CountryId, [Name], Severity, Infectiousness, DurationTicks, RemainingTicks, Stage, TargetScope, Active, CreatedAt, UpdatedAt
)
VALUES (
    @CountryId, @Name, @Severity, @Infectiousness, @DurationTicks, @RemainingTicks, @Stage, @TargetScope, @Active, @CreatedAt, @UpdatedAt
);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            record.Severity = Math.Clamp(record.Severity, 1, 5);
            record.Infectiousness = Math.Clamp(record.Infectiousness, 1, 5);
            record.Stage = Math.Clamp(record.Stage, 0, 4);
            record.DurationTicks = Math.Max(1, record.DurationTicks);
            record.RemainingTicks = Math.Max(0, record.RemainingTicks);
            record.UpdatedAt = DateTime.UtcNow;
            if (record.CreatedAt == default)
                record.CreatedAt = record.UpdatedAt;

            object? idObj = BaseRepository.ExecuteScalar(
                q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, record.CountryId),
                BaseRepository.Param("@Name", SqlDbType.NVarChar, record.Name ?? string.Empty, 80),
                BaseRepository.Param("@Severity", SqlDbType.Int, record.Severity),
                BaseRepository.Param("@Infectiousness", SqlDbType.Int, record.Infectiousness),
                BaseRepository.Param("@DurationTicks", SqlDbType.Int, record.DurationTicks),
                BaseRepository.Param("@RemainingTicks", SqlDbType.Int, record.RemainingTicks),
                BaseRepository.Param("@Stage", SqlDbType.Int, record.Stage),
                BaseRepository.Param("@TargetScope", SqlDbType.NVarChar, record.TargetScope ?? "whole_state", 32),
                BaseRepository.Param("@Active", SqlDbType.Bit, record.Active),
                BaseRepository.Param("@CreatedAt", SqlDbType.DateTime2, record.CreatedAt),
                BaseRepository.Param("@UpdatedAt", SqlDbType.DateTime2, record.UpdatedAt)
            );

            return idObj == null || idObj == DBNull.Value ? 0 : Convert.ToInt32(idObj);
        }

        public static bool Update(CountryDiseaseRecord record)
        {
            const string q = @"
UPDATE CountryDiseases
SET CountryId = @CountryId,
    [Name] = @Name,
    Severity = @Severity,
    Infectiousness = @Infectiousness,
    DurationTicks = @DurationTicks,
    RemainingTicks = @RemainingTicks,
    Stage = @Stage,
    TargetScope = @TargetScope,
    Active = @Active,
    UpdatedAt = @UpdatedAt
WHERE DiseaseId = @DiseaseId";

            record.Severity = Math.Clamp(record.Severity, 1, 5);
            record.Infectiousness = Math.Clamp(record.Infectiousness, 1, 5);
            record.Stage = Math.Clamp(record.Stage, 0, 4);
            record.DurationTicks = Math.Max(1, record.DurationTicks);
            record.RemainingTicks = Math.Max(0, record.RemainingTicks);
            record.UpdatedAt = DateTime.UtcNow;

            int rows = BaseRepository.ExecuteNonQuery(
                q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, record.CountryId),
                BaseRepository.Param("@Name", SqlDbType.NVarChar, record.Name ?? string.Empty, 80),
                BaseRepository.Param("@Severity", SqlDbType.Int, record.Severity),
                BaseRepository.Param("@Infectiousness", SqlDbType.Int, record.Infectiousness),
                BaseRepository.Param("@DurationTicks", SqlDbType.Int, record.DurationTicks),
                BaseRepository.Param("@RemainingTicks", SqlDbType.Int, record.RemainingTicks),
                BaseRepository.Param("@Stage", SqlDbType.Int, record.Stage),
                BaseRepository.Param("@TargetScope", SqlDbType.NVarChar, record.TargetScope ?? "whole_state", 32),
                BaseRepository.Param("@Active", SqlDbType.Bit, record.Active),
                BaseRepository.Param("@UpdatedAt", SqlDbType.DateTime2, record.UpdatedAt),
                BaseRepository.Param("@DiseaseId", SqlDbType.Int, record.DiseaseId)
            );

            return rows > 0;
        }

        public static void Upsert(CountryDiseaseRecord record)
        {
            if (record == null || record.CountryId <= 0)
                return;

            if (record.DiseaseId > 0 && Update(record))
                return;

            int id = Add(record);
            if (id > 0)
                record.DiseaseId = id;
        }

        public static int EndDiseaseByName(int countryId, string name)
        {
            const string q = @"
UPDATE CountryDiseases
SET Active = 0,
    Stage = 0,
    RemainingTicks = 0,
    UpdatedAt = SYSDATETIME()
WHERE CountryId = @CountryId
  AND [Name] = @Name
  AND Active = 1";

            return BaseRepository.ExecuteNonQuery(
                q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId),
                BaseRepository.Param("@Name", SqlDbType.NVarChar, name ?? string.Empty, 80));
        }

        public static int EndDiseaseById(int countryId, int diseaseId)
        {
            const string q = @"
UPDATE CountryDiseases
SET Active = 0,
    Stage = 0,
    RemainingTicks = 0,
    UpdatedAt = SYSDATETIME()
WHERE CountryId = @CountryId
  AND DiseaseId = @DiseaseId
  AND Active = 1";

            return BaseRepository.ExecuteNonQuery(
                q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId),
                BaseRepository.Param("@DiseaseId", SqlDbType.Int, diseaseId));
        }

        private static CountryDiseaseRecord Map(IDataRecord reader)
        {
            return new CountryDiseaseRecord
            {
                DiseaseId = reader.GetInt32(reader.GetOrdinal("DiseaseId")),
                CountryId = reader.GetInt32(reader.GetOrdinal("CountryId")),
                Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? string.Empty : reader.GetString(reader.GetOrdinal("Name")),
                Severity = reader.GetInt32(reader.GetOrdinal("Severity")),
                Infectiousness = reader.GetInt32(reader.GetOrdinal("Infectiousness")),
                DurationTicks = reader.GetInt32(reader.GetOrdinal("DurationTicks")),
                RemainingTicks = reader.GetInt32(reader.GetOrdinal("RemainingTicks")),
                Stage = reader.GetInt32(reader.GetOrdinal("Stage")),
                TargetScope = reader.IsDBNull(reader.GetOrdinal("TargetScope")) ? "whole_state" : reader.GetString(reader.GetOrdinal("TargetScope")),
                Active = reader.GetBoolean(reader.GetOrdinal("Active")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
            };
        }
    }
}
