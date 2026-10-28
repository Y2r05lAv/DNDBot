using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DNDBot.DataBase
{
     
    public class ParliamentSessionRecord
    {
        public int SessionId { get; set; }
        public int CountryId { get; set; }
        public int CalledByCharacterId { get; set; }
        public string Status { get; set; }
        public int RequiredQuorum { get; set; }
        public int PresentCount { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    public static class ParliamentSessionsData
    {
        static ParliamentSessionsData()
        {
            BaseRepository.DBInitialization();
        }

        public static string AddSession(ParliamentSessionRecord session)
        {
            const string insert = @"
INSERT INTO ParliamentSessions (CountryId, CalledByCharacterId, Status, RequiredQuorum, PresentCount, StartTime, EndTime)
VALUES (@CountryId, @CalledByCharacterId, @Status, @RequiredQuorum, @PresentCount, @StartTime, @EndTime);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var idObj = BaseRepository.ExecuteScalar(insert,
                BaseRepository.Param("@CountryId", SqlDbType.Int, session.CountryId),
                BaseRepository.Param("@CalledByCharacterId", SqlDbType.Int, session.CalledByCharacterId),
                BaseRepository.Param("@Status", SqlDbType.NVarChar, session.Status, 20),
                BaseRepository.Param("@RequiredQuorum", SqlDbType.Int, session.RequiredQuorum),
                BaseRepository.Param("@PresentCount", SqlDbType.Int, session.PresentCount),
                BaseRepository.Param("@StartTime", SqlDbType.DateTime2, session.StartTime),
                BaseRepository.Param("@EndTime", SqlDbType.DateTime2, session.EndTime)
            );

            if (idObj == null || idObj == DBNull.Value)
                return "Помилка додавання сесії.";

            return $"Сесія додана з Id = {Convert.ToInt32(idObj)}";
        }

        public static string UpdateSession(ParliamentSessionRecord session)
        {
            const string update = @"
UPDATE ParliamentSessions
SET CountryId = @CountryId,
    CalledByCharacterId = @CalledByCharacterId,
    Status = @Status,
    RequiredQuorum = @RequiredQuorum,
    PresentCount = @PresentCount,
    StartTime = @StartTime,
    EndTime = @EndTime
WHERE SessionId = @SessionId";

            int rows = BaseRepository.ExecuteNonQuery(update,
                BaseRepository.Param("@CountryId", SqlDbType.Int, session.CountryId),
                BaseRepository.Param("@CalledByCharacterId", SqlDbType.Int, session.CalledByCharacterId),
                BaseRepository.Param("@Status", SqlDbType.NVarChar, session.Status, 20),
                BaseRepository.Param("@RequiredQuorum", SqlDbType.Int, session.RequiredQuorum),
                BaseRepository.Param("@PresentCount", SqlDbType.Int, session.PresentCount),
                BaseRepository.Param("@StartTime", SqlDbType.DateTime2, session.StartTime),
                BaseRepository.Param("@EndTime", SqlDbType.DateTime2, session.EndTime),
                BaseRepository.Param("@SessionId", SqlDbType.Int, session.SessionId)
            );

            return rows > 0 ? "Сесія оновлена." : "Сесію не знайдено або нічого не змінено.";
        }

        public static string DeleteSession(int sessionId)
        {
            const string delete = "DELETE FROM ParliamentSessions WHERE SessionId = @SessionId";
            int rows = BaseRepository.ExecuteNonQuery(delete,
                BaseRepository.Param("@SessionId", SqlDbType.Int, sessionId));
            return rows > 0 ? "Сесія видалена." : "Сесію не знайдено.";
        }

        public static ParliamentSessionRecord GetSessionById(int sessionId)
        {
            const string q = @"
SELECT SessionId, CountryId, CalledByCharacterId, Status, RequiredQuorum, PresentCount, StartTime, EndTime
FROM ParliamentSessions
WHERE SessionId = @SessionId";

            using var reader = BaseRepository.ExecuteReader(q,
                BaseRepository.Param("@SessionId", SqlDbType.Int, sessionId));
            try
            {
                if (reader.Read())
                {
                    return new ParliamentSessionRecord
                    {
                        SessionId = reader.GetInt32(reader.GetOrdinal("SessionId")),
                        CountryId = reader.GetInt32(reader.GetOrdinal("CountryId")),
                        CalledByCharacterId = reader.GetInt32(reader.GetOrdinal("CalledByCharacterId")),
                        Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? string.Empty : reader.GetString(reader.GetOrdinal("Status")),
                        RequiredQuorum = reader.GetInt32(reader.GetOrdinal("RequiredQuorum")),
                        PresentCount = reader.GetInt32(reader.GetOrdinal("PresentCount")),
                        StartTime = reader.GetDateTime(reader.GetOrdinal("StartTime")),
                        EndTime = reader.IsDBNull(reader.GetOrdinal("EndTime")) ? null : reader.GetDateTime(reader.GetOrdinal("EndTime"))
                    };
                }
            }
            finally
            {
                reader.Dispose();
            }

            return null;
        }

        public static List<ParliamentSessionRecord> GetAllSessions()
        {
            const string q = @"
SELECT SessionId, CountryId, CalledByCharacterId, Status, RequiredQuorum, PresentCount, StartTime, EndTime
FROM ParliamentSessions
ORDER BY SessionId";

            var list = new List<ParliamentSessionRecord>();
            using var reader = BaseRepository.ExecuteReader(q);
            try
            {
                while (reader.Read())
                {
                    list.Add(new ParliamentSessionRecord
                    {
                        SessionId = reader.GetInt32(reader.GetOrdinal("SessionId")),
                        CountryId = reader.GetInt32(reader.GetOrdinal("CountryId")),
                        CalledByCharacterId = reader.GetInt32(reader.GetOrdinal("CalledByCharacterId")),
                        Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? string.Empty : reader.GetString(reader.GetOrdinal("Status")),
                        RequiredQuorum = reader.GetInt32(reader.GetOrdinal("RequiredQuorum")),
                        PresentCount = reader.GetInt32(reader.GetOrdinal("PresentCount")),
                        StartTime = reader.GetDateTime(reader.GetOrdinal("StartTime")),
                        EndTime = reader.IsDBNull(reader.GetOrdinal("EndTime")) ? null : reader.GetDateTime(reader.GetOrdinal("EndTime"))
                    });
                }
            }
            finally
            {
                reader.Dispose();
            }
            return list;
        }
    }
}
