#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DNDBot.DataBase
{
    public sealed class EconomyAidTrancheRecord
    {
        public int TrancheId { get; set; }
        public int FromCountryId { get; set; }
        public int ToCountryId { get; set; }
        public decimal DailyAmount { get; set; }
        public int RemainingDays { get; set; }
        public bool IsActive { get; set; }
        public long CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastAppliedAt { get; set; }
    }

    public static class EconomyAidTranchesData
    {
        static EconomyAidTranchesData()
        {
            BaseRepository.DBInitialization();
        }

        private static SqlParameter DecimalParam(string name, decimal value, byte precision = 28, byte scale = 2)
        {
            return new SqlParameter(name, SqlDbType.Decimal)
            {
                Precision = precision,
                Scale = scale,
                Value = value
            };
        }

        public static int Add(int fromCountryId, int toCountryId, decimal dailyAmount, int days, long createdByUserId)
        {
            const string q = @"
INSERT INTO EconomyAidTranches (
    FromCountryId,
    ToCountryId,
    DailyAmount,
    RemainingDays,
    IsActive,
    CreatedByUserId,
    CreatedAt
)
VALUES (
    @FromCountryId,
    @ToCountryId,
    @DailyAmount,
    @RemainingDays,
    1,
    @CreatedByUserId,
    @CreatedAt
);

SELECT CAST(SCOPE_IDENTITY() AS INT);";

            object? idObj = BaseRepository.ExecuteScalar(
                q,
                BaseRepository.Param("@FromCountryId", SqlDbType.Int, fromCountryId),
                BaseRepository.Param("@ToCountryId", SqlDbType.Int, toCountryId),
                DecimalParam("@DailyAmount", dailyAmount),
                BaseRepository.Param("@RemainingDays", SqlDbType.Int, days),
                BaseRepository.Param("@CreatedByUserId", SqlDbType.BigInt, createdByUserId),
                BaseRepository.Param("@CreatedAt", SqlDbType.DateTime2, DateTime.UtcNow));

            if (idObj == null || idObj == DBNull.Value)
                return 0;

            return Convert.ToInt32(idObj);
        }

        public static List<EconomyAidTrancheRecord> GetActiveByFromCountry(int fromCountryId)
        {
            const string q = @"
SELECT TrancheId,
       FromCountryId,
       ToCountryId,
       DailyAmount,
       RemainingDays,
       IsActive,
       CreatedByUserId,
       CreatedAt,
       LastAppliedAt
FROM EconomyAidTranches
WHERE FromCountryId = @FromCountryId
  AND IsActive = 1
  AND RemainingDays > 0
ORDER BY TrancheId;";

            var list = new List<EconomyAidTrancheRecord>();
            using var reader = BaseRepository.ExecuteReader(
                q,
                BaseRepository.Param("@FromCountryId", SqlDbType.Int, fromCountryId));
            try
            {
                while (reader.Read())
                {
                    list.Add(Map(reader));
                }
            }
            finally
            {
                reader.Dispose();
            }

            return list;
        }

        public static bool ApplySuccessfulDay(int trancheId)
        {
            const string q = @"
UPDATE EconomyAidTranches
SET RemainingDays = CASE WHEN RemainingDays > 1 THEN RemainingDays - 1 ELSE 0 END,
    IsActive = CASE WHEN RemainingDays > 1 THEN 1 ELSE 0 END,
    LastAppliedAt = @AppliedAt
WHERE TrancheId = @TrancheId
  AND IsActive = 1
  AND RemainingDays > 0;";

            int rows = BaseRepository.ExecuteNonQuery(
                q,
                BaseRepository.Param("@TrancheId", SqlDbType.Int, trancheId),
                BaseRepository.Param("@AppliedAt", SqlDbType.DateTime2, DateTime.UtcNow));
            return rows > 0;
        }

        public static bool Deactivate(int trancheId)
        {
            const string q = @"
UPDATE EconomyAidTranches
SET IsActive = 0
WHERE TrancheId = @TrancheId;";

            int rows = BaseRepository.ExecuteNonQuery(
                q,
                BaseRepository.Param("@TrancheId", SqlDbType.Int, trancheId));
            return rows > 0;
        }

        private static EconomyAidTrancheRecord Map(IDataRecord reader)
        {
            int ordLastAppliedAt = reader.GetOrdinal("LastAppliedAt");
            return new EconomyAidTrancheRecord
            {
                TrancheId = reader.GetInt32(reader.GetOrdinal("TrancheId")),
                FromCountryId = reader.GetInt32(reader.GetOrdinal("FromCountryId")),
                ToCountryId = reader.GetInt32(reader.GetOrdinal("ToCountryId")),
                DailyAmount = reader.GetDecimal(reader.GetOrdinal("DailyAmount")),
                RemainingDays = reader.GetInt32(reader.GetOrdinal("RemainingDays")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                CreatedByUserId = reader.GetInt64(reader.GetOrdinal("CreatedByUserId")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                LastAppliedAt = reader.IsDBNull(ordLastAppliedAt) ? null : reader.GetDateTime(ordLastAppliedAt)
            };
        }
    }
}
