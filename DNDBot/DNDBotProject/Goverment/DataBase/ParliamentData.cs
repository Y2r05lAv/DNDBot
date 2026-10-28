using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DNDBot.DataBase
{
     
    public class ParliamentRecord
    {
        public int ParliamentId { get; set; }
        public int CountryId { get; set; }
        public int TotalSeats { get; set; }
        public decimal QuorumPercent { get; set; }
        public decimal GovernmentSupport { get; set; }
        public bool IsSessionActive { get; set; }
        public DateTime? LastSessionStart { get; set; }
    }

    public static class ParliamentData
    {
        static ParliamentData()
        {
            BaseRepository.DBInitialization();
        }

        private static SqlParameter DecimalParam(string name, decimal value, byte precision, byte scale)
        {
            return new SqlParameter(name, SqlDbType.Decimal)
            {
                Precision = precision,
                Scale = scale,
                Value = value
            };
        }

        public static string AddParliament(ParliamentRecord parliament)
        {
            const string insert = @"
INSERT INTO Parliament (CountryId, TotalSeats, QuorumPercent, GovernmentSupport, IsSessionActive, LastSessionStart)
VALUES (@CountryId, @TotalSeats, @QuorumPercent, @GovernmentSupport, @IsSessionActive, @LastSessionStart);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var idObj = BaseRepository.ExecuteScalar(insert,
                BaseRepository.Param("@CountryId", SqlDbType.Int, parliament.CountryId),
                BaseRepository.Param("@TotalSeats", SqlDbType.Int, parliament.TotalSeats),
                DecimalParam("@QuorumPercent", parliament.QuorumPercent, 4, 2),
                DecimalParam("@GovernmentSupport", parliament.GovernmentSupport, 4, 2),
                BaseRepository.Param("@IsSessionActive", SqlDbType.Bit, parliament.IsSessionActive),
                BaseRepository.Param("@LastSessionStart", SqlDbType.DateTime2, parliament.LastSessionStart)
            );

            if (idObj == null || idObj == DBNull.Value)
                return "Помилка додавання парламенту.";

            return $"Парламент додано з Id = {Convert.ToInt32(idObj)}";
        }

        public static string UpdateParliament(ParliamentRecord parliament)
        {
            const string update = @"
UPDATE Parliament
SET CountryId = @CountryId,
    TotalSeats = @TotalSeats,
    QuorumPercent = @QuorumPercent,
    GovernmentSupport = @GovernmentSupport,
    IsSessionActive = @IsSessionActive,
    LastSessionStart = @LastSessionStart
WHERE ParliamentId = @ParliamentId";

            int rows = BaseRepository.ExecuteNonQuery(update,
                BaseRepository.Param("@CountryId", SqlDbType.Int, parliament.CountryId),
                BaseRepository.Param("@TotalSeats", SqlDbType.Int, parliament.TotalSeats),
                DecimalParam("@QuorumPercent", parliament.QuorumPercent, 4, 2),
                DecimalParam("@GovernmentSupport", parliament.GovernmentSupport, 4, 2),
                BaseRepository.Param("@IsSessionActive", SqlDbType.Bit, parliament.IsSessionActive),
                BaseRepository.Param("@LastSessionStart", SqlDbType.DateTime2, parliament.LastSessionStart),
                BaseRepository.Param("@ParliamentId", SqlDbType.Int, parliament.ParliamentId)
            );

            return rows > 0 ? "Парламент оновлений." : "Парламент не знайдено або нічого не змінено.";
        }

        public static string DeleteParliament(int parliamentId)
        {
            const string delete = "DELETE FROM Parliament WHERE ParliamentId = @ParliamentId";
            int rows = BaseRepository.ExecuteNonQuery(delete,
                BaseRepository.Param("@ParliamentId", SqlDbType.Int, parliamentId));
            return rows > 0 ? "Парламент видалений." : "Парламент не знайдено.";
        }

        public static ParliamentRecord GetParliamentById(int parliamentId)
        {
            const string q = @"
SELECT ParliamentId, CountryId, TotalSeats, QuorumPercent, GovernmentSupport, IsSessionActive, LastSessionStart
FROM Parliament
WHERE ParliamentId = @ParliamentId";

            using var reader = BaseRepository.ExecuteReader(q,
                BaseRepository.Param("@ParliamentId", SqlDbType.Int, parliamentId));
            try
            {
                if (reader.Read())
                {
                    return new ParliamentRecord
                    {
                        ParliamentId = reader.GetInt32(reader.GetOrdinal("ParliamentId")),
                        CountryId = reader.GetInt32(reader.GetOrdinal("CountryId")),
                        TotalSeats = reader.GetInt32(reader.GetOrdinal("TotalSeats")),
                        QuorumPercent = reader.GetDecimal(reader.GetOrdinal("QuorumPercent")),
                        GovernmentSupport = reader.GetDecimal(reader.GetOrdinal("GovernmentSupport")),
                        IsSessionActive = reader.GetBoolean(reader.GetOrdinal("IsSessionActive")),
                        LastSessionStart = reader.IsDBNull(reader.GetOrdinal("LastSessionStart")) ? null : reader.GetDateTime(reader.GetOrdinal("LastSessionStart"))
                    };
                }
            }
            finally
            {
                reader.Dispose();
            }

            return null;
        }

        public static List<ParliamentRecord> GetAllParliaments()
        {
            const string q = @"
SELECT ParliamentId, CountryId, TotalSeats, QuorumPercent, GovernmentSupport, IsSessionActive, LastSessionStart
FROM Parliament
ORDER BY ParliamentId";

            var list = new List<ParliamentRecord>();
            using var reader = BaseRepository.ExecuteReader(q);
            try
            {
                while (reader.Read())
                {
                    list.Add(new ParliamentRecord
                    {
                        ParliamentId = reader.GetInt32(reader.GetOrdinal("ParliamentId")),
                        CountryId = reader.GetInt32(reader.GetOrdinal("CountryId")),
                        TotalSeats = reader.GetInt32(reader.GetOrdinal("TotalSeats")),
                        QuorumPercent = reader.GetDecimal(reader.GetOrdinal("QuorumPercent")),
                        GovernmentSupport = reader.GetDecimal(reader.GetOrdinal("GovernmentSupport")),
                        IsSessionActive = reader.GetBoolean(reader.GetOrdinal("IsSessionActive")),
                        LastSessionStart = reader.IsDBNull(reader.GetOrdinal("LastSessionStart")) ? null : reader.GetDateTime(reader.GetOrdinal("LastSessionStart"))
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
