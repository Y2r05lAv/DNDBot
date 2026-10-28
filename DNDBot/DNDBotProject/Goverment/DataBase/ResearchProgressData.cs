#nullable enable
using System;
using System.Data;
using DNDBot.Goverment.Research;

namespace DNDBot.DataBase
{
    public static class ResearchProgressData
    {
        static ResearchProgressData()
        {
            BaseRepository.DBInitialization();
        }

        public static ResearchProgressRecord? GetActiveResearch(int countryId)
        {
            const string q = @"
SELECT CountryId, ResearchId, Progress, StartedAt, IsCompleted
FROM ResearchProgress
WHERE CountryId = @CountryId AND IsCompleted = 0";

            using var reader = BaseRepository.ExecuteReader(q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId));
            try
            {
                if (reader.Read())
                {
                    return new ResearchProgressRecord
                    {
                        CountryId = reader.GetInt32(reader.GetOrdinal("CountryId")),
                        ResearchId = reader.GetInt32(reader.GetOrdinal("ResearchId")),
                        Progress = Convert.ToSingle(reader["Progress"]),
                        StartedAt = reader.GetDateTime(reader.GetOrdinal("StartedAt")),
                        IsCompleted = reader.GetBoolean(reader.GetOrdinal("IsCompleted"))
                    };
                }
            }
            finally
            {
                reader.Dispose();
            }

            return null;
        }

        public static void UpsertProgress(ResearchProgressRecord record)
        {
            const string q = @"
IF EXISTS (SELECT 1 FROM ResearchProgress WHERE CountryId = @CountryId AND ResearchId = @ResearchId)
    UPDATE ResearchProgress
    SET Progress = @Progress, StartedAt = @StartedAt, IsCompleted = @IsCompleted
    WHERE CountryId = @CountryId AND ResearchId = @ResearchId
ELSE
    INSERT INTO ResearchProgress (CountryId, ResearchId, Progress, StartedAt, IsCompleted)
    VALUES (@CountryId, @ResearchId, @Progress, @StartedAt, @IsCompleted);";

            BaseRepository.ExecuteNonQuery(q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, record.CountryId),
                BaseRepository.Param("@ResearchId", SqlDbType.Int, record.ResearchId),
                BaseRepository.Param("@Progress", SqlDbType.Real, record.Progress),
                BaseRepository.Param("@StartedAt", SqlDbType.DateTime2, record.StartedAt),
                BaseRepository.Param("@IsCompleted", SqlDbType.Bit, record.IsCompleted)
            );
        }

        public static void MarkCompleted(int countryId, int researchId)
        {
            const string q = @"
UPDATE ResearchProgress
SET IsCompleted = 1
WHERE CountryId = @CountryId AND ResearchId = @ResearchId";

            BaseRepository.ExecuteNonQuery(q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId),
                BaseRepository.Param("@ResearchId", SqlDbType.Int, researchId));
        }
    }
}

