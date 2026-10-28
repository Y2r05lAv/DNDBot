using System;
using System.Collections.Generic;
using System.Data;

namespace DNDBot.DataBase
{
     
    public class SessionAttendanceRecord
    {
        public int SessionId { get; set; }
        public int DeputyId { get; set; }
        public bool IsPresent { get; set; }
        public string Reason { get; set; }
    }

    public static class SessionAttendanceData
    {
        static SessionAttendanceData()
        {
            BaseRepository.DBInitialization();
        }

        public static string AddAttendance(SessionAttendanceRecord attendance)
        {
            const string insert = @"
INSERT INTO SessionAttendance (SessionId, DeputyId, IsPresent, Reason)
VALUES (@SessionId, @DeputyId, @IsPresent, @Reason);";

            int rows = BaseRepository.ExecuteNonQuery(insert,
                BaseRepository.Param("@SessionId", SqlDbType.Int, attendance.SessionId),
                BaseRepository.Param("@DeputyId", SqlDbType.Int, attendance.DeputyId),
                BaseRepository.Param("@IsPresent", SqlDbType.Bit, attendance.IsPresent),
                BaseRepository.Param("@Reason", SqlDbType.NVarChar, attendance.Reason, 50)
            );

            return rows > 0 ? "Відвідування додано." : "Не вдалося додати відвідування.";
        }

        public static string UpdateAttendance(SessionAttendanceRecord attendance)
        {
            const string update = @"
UPDATE SessionAttendance
SET IsPresent = @IsPresent,
    Reason = @Reason
WHERE SessionId = @SessionId AND DeputyId = @DeputyId";

            int rows = BaseRepository.ExecuteNonQuery(update,
                BaseRepository.Param("@IsPresent", SqlDbType.Bit, attendance.IsPresent),
                BaseRepository.Param("@Reason", SqlDbType.NVarChar, attendance.Reason, 50),
                BaseRepository.Param("@SessionId", SqlDbType.Int, attendance.SessionId),
                BaseRepository.Param("@DeputyId", SqlDbType.Int, attendance.DeputyId)
            );

            return rows > 0 ? "Відвідування оновлено." : "Запис не знайдено або нічого не змінено.";
        }

        public static string DeleteAttendance(int sessionId, int deputyId)
        {
            const string delete = "DELETE FROM SessionAttendance WHERE SessionId = @SessionId AND DeputyId = @DeputyId";
            int rows = BaseRepository.ExecuteNonQuery(delete,
                BaseRepository.Param("@SessionId", SqlDbType.Int, sessionId),
                BaseRepository.Param("@DeputyId", SqlDbType.Int, deputyId));
            return rows > 0 ? "Відвідування видалено." : "Запис не знайдено.";
        }

        public static SessionAttendanceRecord GetAttendance(int sessionId, int deputyId)
        {
            const string q = @"
SELECT SessionId, DeputyId, IsPresent, Reason
FROM SessionAttendance
WHERE SessionId = @SessionId AND DeputyId = @DeputyId";

            using var reader = BaseRepository.ExecuteReader(q,
                BaseRepository.Param("@SessionId", SqlDbType.Int, sessionId),
                BaseRepository.Param("@DeputyId", SqlDbType.Int, deputyId));
            try
            {
                if (reader.Read())
                {
                    return new SessionAttendanceRecord
                    {
                        SessionId = reader.GetInt32(reader.GetOrdinal("SessionId")),
                        DeputyId = reader.GetInt32(reader.GetOrdinal("DeputyId")),
                        IsPresent = reader.GetBoolean(reader.GetOrdinal("IsPresent")),
                        Reason = reader.IsDBNull(reader.GetOrdinal("Reason")) ? null : reader.GetString(reader.GetOrdinal("Reason"))
                    };
                }
            }
            finally
            {
                reader.Dispose();
            }

            return null;
        }

        public static List<SessionAttendanceRecord> GetAllAttendance()
        {
            const string q = @"
SELECT SessionId, DeputyId, IsPresent, Reason
FROM SessionAttendance
ORDER BY SessionId, DeputyId";

            var list = new List<SessionAttendanceRecord>();
            using var reader = BaseRepository.ExecuteReader(q);
            try
            {
                while (reader.Read())
                {
                    list.Add(new SessionAttendanceRecord
                    {
                        SessionId = reader.GetInt32(reader.GetOrdinal("SessionId")),
                        DeputyId = reader.GetInt32(reader.GetOrdinal("DeputyId")),
                        IsPresent = reader.GetBoolean(reader.GetOrdinal("IsPresent")),
                        Reason = reader.IsDBNull(reader.GetOrdinal("Reason")) ? null : reader.GetString(reader.GetOrdinal("Reason"))
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
