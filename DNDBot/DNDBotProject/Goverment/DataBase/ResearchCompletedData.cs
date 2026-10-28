using System;
using System.Collections.Generic;
using System.Data;

namespace DNDBot.DataBase
{
    public static class ResearchCompletedData
    {
        static ResearchCompletedData()
        {
            BaseRepository.DBInitialization();
        }

        public static List<int> GetCompletedResearchIds(int countryId)
        {
            const string q = @"
SELECT ResearchId
FROM ResearchCompleted
WHERE CountryId = @CountryId";

            var list = new List<int>();
            using var reader = BaseRepository.ExecuteReader(q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId));
            try
            {
                while (reader.Read())
                {
                    list.Add(reader.GetInt32(reader.GetOrdinal("ResearchId")));
                }
            }
            finally
            {
                reader.Dispose();
            }

            return list;
        }

        public static bool IsCompleted(int countryId, int researchId)
        {
            const string q = @"
SELECT COUNT(1)
FROM ResearchCompleted
WHERE CountryId = @CountryId AND ResearchId = @ResearchId";

            var obj = BaseRepository.ExecuteScalar(q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId),
                BaseRepository.Param("@ResearchId", SqlDbType.Int, researchId));

            return obj != null && obj != DBNull.Value && Convert.ToInt32(obj) > 0;
        }

        public static void AddCompleted(int countryId, int researchId)
        {
            const string q = @"
IF NOT EXISTS (SELECT 1 FROM ResearchCompleted WHERE CountryId = @CountryId AND ResearchId = @ResearchId)
    INSERT INTO ResearchCompleted (CountryId, ResearchId, CompletedAt)
    VALUES (@CountryId, @ResearchId, @CompletedAt);";

            BaseRepository.ExecuteNonQuery(q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId),
                BaseRepository.Param("@ResearchId", SqlDbType.Int, researchId),
                BaseRepository.Param("@CompletedAt", SqlDbType.DateTime2, DateTime.Now));
        }
    }
}
