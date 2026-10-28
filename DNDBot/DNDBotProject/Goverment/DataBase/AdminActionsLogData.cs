#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DNDBot.DataBase
{
    public static class AdminActionsLogData
    {
        static AdminActionsLogData()
        {
            BaseRepository.DBInitialization();
        }

        public sealed class AdminActionsLogRecord
        {
            public int LogId { get; set; }
            public int CountryId { get; set; }
            public long UserId { get; set; }
            public string ActionType { get; set; } = string.Empty;
            public string? OldValue { get; set; }
            public string? NewValue { get; set; }
            public string? Details { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public static void AddLog(int countryId, long userId, string actionType, string? oldValue, string? newValue, string? details)
        {
            const string q = @"
INSERT INTO AdminActionsLog (
    CountryId, UserId, ActionType, OldValue, NewValue, Details
)
VALUES (
    @CountryId, @UserId, @ActionType, @OldValue, @NewValue, @Details
);";

            BaseRepository.ExecuteNonQuery(q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId),
                BaseRepository.Param("@UserId", SqlDbType.BigInt, userId),
                BaseRepository.Param("@ActionType", SqlDbType.NVarChar, actionType, 50),
                BaseRepository.Param("@OldValue", SqlDbType.NVarChar, (object?)oldValue ?? DBNull.Value, 200),
                BaseRepository.Param("@NewValue", SqlDbType.NVarChar, (object?)newValue ?? DBNull.Value, 200),
                BaseRepository.Param("@Details", SqlDbType.NVarChar, (object?)details ?? DBNull.Value, 500)
            );
        }

        public static List<AdminActionsLogRecord> GetByCountry(int countryId, int page, int pageSize, out int totalCount)
        {
            const string qCount = "SELECT COUNT(1) FROM AdminActionsLog WHERE CountryId = @CountryId";
            var countObj = BaseRepository.ExecuteScalar(qCount, BaseRepository.Param("@CountryId", SqlDbType.Int, countryId));
            totalCount = countObj == null || countObj == DBNull.Value ? 0 : Convert.ToInt32(countObj);

            const string q = @"
SELECT LogId, CountryId, UserId, ActionType, OldValue, NewValue, Details, CreatedAt
FROM AdminActionsLog
WHERE CountryId = @CountryId
ORDER BY CreatedAt DESC, LogId DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            int offset = Math.Max(0, page) * Math.Max(1, pageSize);
            var list = new List<AdminActionsLogRecord>();
            using var reader = BaseRepository.ExecuteReader(q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId),
                BaseRepository.Param("@Offset", SqlDbType.Int, offset),
                BaseRepository.Param("@PageSize", SqlDbType.Int, pageSize)
            );
            try
            {
                while (reader.Read())
                {
                    list.Add(new AdminActionsLogRecord
                    {
                        LogId = reader.GetInt32(reader.GetOrdinal("LogId")),
                        CountryId = reader.GetInt32(reader.GetOrdinal("CountryId")),
                        UserId = reader.GetInt64(reader.GetOrdinal("UserId")),
                        ActionType = reader.GetString(reader.GetOrdinal("ActionType")),
                        OldValue = reader.IsDBNull(reader.GetOrdinal("OldValue")) ? null : reader.GetString(reader.GetOrdinal("OldValue")),
                        NewValue = reader.IsDBNull(reader.GetOrdinal("NewValue")) ? null : reader.GetString(reader.GetOrdinal("NewValue")),
                        Details = reader.IsDBNull(reader.GetOrdinal("Details")) ? null : reader.GetString(reader.GetOrdinal("Details")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
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

