using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DNDBot.DataBase
{
    public static class EconomyMoneyLogData
    {
        static EconomyMoneyLogData()
        {
            BaseRepository.DBInitialization();
        }

        public sealed class EconomyMoneyLogRecord
        {
            public int LogId { get; set; }
            public int CountryId { get; set; }
            public long UserId { get; set; }
            public decimal Amount { get; set; }
            public string Kind { get; set; } = string.Empty;  
            public string Reason { get; set; } = string.Empty;
            public int TurnNumber { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        private static SqlParameter DecimalParam(string name, decimal value, byte precision = 28, byte scale = 2)
        {
            var p = new SqlParameter(name, SqlDbType.Decimal)
            {
                Precision = precision,
                Scale = scale,
                Value = value
            };
            return p;
        }

        public static void AddLog(int countryId, long userId, decimal amount, string kind, string reason, int turnNumber)
        {
            const string q = @"
INSERT INTO EconomyMoneyLog (
    CountryId, UserId, Amount, Kind, Reason, TurnNumber
)
VALUES (
    @CountryId, @UserId, @Amount, @Kind, @Reason, @TurnNumber
);";

            BaseRepository.ExecuteNonQuery(q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId),
                BaseRepository.Param("@UserId", SqlDbType.BigInt, userId),
                DecimalParam("@Amount", amount),
                BaseRepository.Param("@Kind", SqlDbType.NVarChar, kind, 10),
                BaseRepository.Param("@Reason", SqlDbType.NVarChar, reason, 200),
                BaseRepository.Param("@TurnNumber", SqlDbType.Int, turnNumber)
            );
        }

        public static List<EconomyMoneyLogRecord> GetByCountry(int countryId, int page, int pageSize, out int totalCount)
        {
            const string qCount = "SELECT COUNT(1) FROM EconomyMoneyLog WHERE CountryId = @CountryId";
            var countObj = BaseRepository.ExecuteScalar(qCount, BaseRepository.Param("@CountryId", SqlDbType.Int, countryId));
            totalCount = countObj == null || countObj == DBNull.Value ? 0 : Convert.ToInt32(countObj);

            const string q = @"
SELECT LogId, CountryId, UserId, Amount, Kind, Reason, TurnNumber, CreatedAt
FROM EconomyMoneyLog
WHERE CountryId = @CountryId
ORDER BY CreatedAt DESC, LogId DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            int offset = Math.Max(0, page) * Math.Max(1, pageSize);
            var list = new List<EconomyMoneyLogRecord>();
            using var reader = BaseRepository.ExecuteReader(q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId),
                BaseRepository.Param("@Offset", SqlDbType.Int, offset),
                BaseRepository.Param("@PageSize", SqlDbType.Int, pageSize)
            );
            try
            {
                while (reader.Read())
                {
                    list.Add(new EconomyMoneyLogRecord
                    {
                        LogId = reader.GetInt32(reader.GetOrdinal("LogId")),
                        CountryId = reader.GetInt32(reader.GetOrdinal("CountryId")),
                        UserId = reader.GetInt64(reader.GetOrdinal("UserId")),
                        Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                        Kind = reader.GetString(reader.GetOrdinal("Kind")),
                        Reason = reader.GetString(reader.GetOrdinal("Reason")),
                        TurnNumber = reader.GetInt32(reader.GetOrdinal("TurnNumber")),
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
