using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace DNDBot.DataBase
{
     
    public class DeputyRecord
    {
        public int DeputyId { get; set; }
        public int CountryId { get; set; }
        public int? PartyId { get; set; }
        public string Name { get; set; }
        public int Loyalty { get; set; }
        public int Corruption { get; set; }
        public int Influence { get; set; }
        public int AttendanceChance { get; set; }
        public bool IsAlive { get; set; }
    }

    public static class DeputiesData
    {
        static DeputiesData()
        {
            BaseRepository.DBInitialization();
        }

        public static string AddDeputy(DeputyRecord deputy)
        {
            var id = AddDeputyAndReturnId(deputy);
            if (!id.HasValue)
                return "Помилка додавання депутата.";

            return $"Депутат додано з Id = {id.Value}";
        }

        public static int? AddDeputyAndReturnId(DeputyRecord deputy)
        {
            const string insert = @"
INSERT INTO Deputies (CountryId, PartyId, Name, Loyalty, Corruption, Influence, AttendanceChance, IsAlive)
VALUES (@CountryId, @PartyId, @Name, @Loyalty, @Corruption, @Influence, @AttendanceChance, @IsAlive);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var idObj = BaseRepository.ExecuteScalar(insert,
                BaseRepository.Param("@CountryId", System.Data.SqlDbType.Int, deputy.CountryId),
                BaseRepository.Param("@PartyId", System.Data.SqlDbType.Int, deputy.PartyId),
                BaseRepository.Param("@Name", System.Data.SqlDbType.NVarChar, deputy.Name, 100),
                BaseRepository.Param("@Loyalty", System.Data.SqlDbType.Int, deputy.Loyalty),
                BaseRepository.Param("@Corruption", System.Data.SqlDbType.Int, deputy.Corruption),
                BaseRepository.Param("@Influence", System.Data.SqlDbType.Int, deputy.Influence),
                BaseRepository.Param("@AttendanceChance", System.Data.SqlDbType.Int, deputy.AttendanceChance),
                BaseRepository.Param("@IsAlive", System.Data.SqlDbType.Bit, deputy.IsAlive)
            );

            if (idObj == null || idObj == DBNull.Value)
                return null;

            return Convert.ToInt32(idObj);
        }

        public static string UpdateDeputy(DeputyRecord deputy)
        {
            const string update = @"
UPDATE Deputies
SET CountryId = @CountryId,
    PartyId = @PartyId,
    Loyalty = @Loyalty,
    Corruption = @Corruption,
    Influence = @Influence,
    AttendanceChance = @AttendanceChance,
    IsAlive = @IsAlive,
    Name = @Name
WHERE DeputyId = @DeputyId";

            int rows = BaseRepository.ExecuteNonQuery(update,
                BaseRepository.Param("@CountryId", System.Data.SqlDbType.Int, deputy.CountryId),
                BaseRepository.Param("@PartyId", System.Data.SqlDbType.Int, deputy.PartyId),
                BaseRepository.Param("@Name", System.Data.SqlDbType.NVarChar, deputy.Name, 100),
                BaseRepository.Param("@Loyalty", System.Data.SqlDbType.Int, deputy.Loyalty),
                BaseRepository.Param("@Corruption", System.Data.SqlDbType.Int, deputy.Corruption),
                BaseRepository.Param("@Influence", System.Data.SqlDbType.Int, deputy.Influence),
                BaseRepository.Param("@AttendanceChance", System.Data.SqlDbType.Int, deputy.AttendanceChance),
                BaseRepository.Param("@IsAlive", System.Data.SqlDbType.Bit, deputy.IsAlive),
                BaseRepository.Param("@DeputyId", System.Data.SqlDbType.Int, deputy.DeputyId)
            );

            return rows > 0 ? "Депутат оновлений." : "Депутат не знайдено або нічого не змінено.";
        }

        public static bool TryDeleteDeputy(int deputyId)
        {
            const string delete = "DELETE FROM Deputies WHERE DeputyId = @DeputyId";
            int rows = BaseRepository.ExecuteNonQuery(delete,
                BaseRepository.Param("@DeputyId", System.Data.SqlDbType.Int, deputyId));
            return rows > 0;
        }

        public static string DeleteDeputy(int deputyId)
        {
            return TryDeleteDeputy(deputyId) ? "Депутат видалений." : "Депутат не знайдено.";
        }

        public static DeputyRecord GetDeputyById(int deputyId)
        {
            const string q = @"
SELECT DeputyId, CountryId, PartyId, Name, Loyalty, Corruption, Influence, AttendanceChance, IsAlive
FROM Deputies
WHERE DeputyId = @DeputyId";

            using var reader = BaseRepository.ExecuteReader(q,
                BaseRepository.Param("@DeputyId", System.Data.SqlDbType.Int, deputyId));
            try
            {
                if (reader.Read())
                {
                    return new DeputyRecord
                    {
                        DeputyId = reader.GetInt32(reader.GetOrdinal("DeputyId")),
                        CountryId = reader.IsDBNull(reader.GetOrdinal("CountryId")) ? 0 : reader.GetInt32(reader.GetOrdinal("CountryId")),
                        PartyId = reader.IsDBNull(reader.GetOrdinal("PartyId")) ? null : reader.GetInt32(reader.GetOrdinal("PartyId")),
                        Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? string.Empty : reader.GetString(reader.GetOrdinal("Name")),
                        Loyalty = reader.IsDBNull(reader.GetOrdinal("Loyalty")) ? 0 : reader.GetInt32(reader.GetOrdinal("Loyalty")),
                        Corruption = reader.IsDBNull(reader.GetOrdinal("Corruption")) ? 0 : reader.GetInt32(reader.GetOrdinal("Corruption")),
                        Influence = reader.IsDBNull(reader.GetOrdinal("Influence")) ? 0 : reader.GetInt32(reader.GetOrdinal("Influence")),
                        AttendanceChance = reader.IsDBNull(reader.GetOrdinal("AttendanceChance")) ? 0 : reader.GetInt32(reader.GetOrdinal("AttendanceChance")),
                        IsAlive = reader.GetBoolean(reader.GetOrdinal("IsAlive"))
                    };
                }
            }
            finally
            {
                reader.Dispose();
            }

            return null;
        }

        public static List<DeputyRecord> GetAllDeputies()
        {
            const string q = @"
SELECT DeputyId, CountryId, PartyId, Name, Loyalty, Corruption, Influence, AttendanceChance, IsAlive
FROM Deputies
ORDER BY DeputyId";

            var list = new List<DeputyRecord>();
            using var reader = BaseRepository.ExecuteReader(q);
            try
            {
                while (reader.Read())
                {
                    list.Add(new DeputyRecord
                    {
                        DeputyId = reader.GetInt32(reader.GetOrdinal("DeputyId")),
                        CountryId = reader.IsDBNull(reader.GetOrdinal("CountryId")) ? 0 : reader.GetInt32(reader.GetOrdinal("CountryId")),
                        PartyId = reader.IsDBNull(reader.GetOrdinal("PartyId")) ? null : reader.GetInt32(reader.GetOrdinal("PartyId")),
                        Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? string.Empty : reader.GetString(reader.GetOrdinal("Name")),
                        Loyalty = reader.IsDBNull(reader.GetOrdinal("Loyalty")) ? 0 : reader.GetInt32(reader.GetOrdinal("Loyalty")),
                        Corruption = reader.IsDBNull(reader.GetOrdinal("Corruption")) ? 0 : reader.GetInt32(reader.GetOrdinal("Corruption")),
                        Influence = reader.IsDBNull(reader.GetOrdinal("Influence")) ? 0 : reader.GetInt32(reader.GetOrdinal("Influence")),
                        AttendanceChance = reader.IsDBNull(reader.GetOrdinal("AttendanceChance")) ? 0 : reader.GetInt32(reader.GetOrdinal("AttendanceChance")),
                        IsAlive = reader.GetBoolean(reader.GetOrdinal("IsAlive"))
                    });
                }
            }
            finally
            {
                reader.Dispose();
            }
            return list;
        }

        public static List<DeputyRecord> GetByCountry(int countryId, bool aliveOnly = true)
        {
            string q = @"
SELECT DeputyId, CountryId, PartyId, Name, Loyalty, Corruption, Influence, AttendanceChance, IsAlive
FROM Deputies
WHERE CountryId = @CountryId" + (aliveOnly ? " AND IsAlive = 1" : "") + @"
ORDER BY DeputyId";

            var list = new List<DeputyRecord>();
            using var reader = BaseRepository.ExecuteReader(q,
                BaseRepository.Param("@CountryId", System.Data.SqlDbType.Int, countryId));
            try
            {
                while (reader.Read())
                {
                    list.Add(new DeputyRecord
                    {
                        DeputyId = reader.GetInt32(reader.GetOrdinal("DeputyId")),
                        CountryId = reader.IsDBNull(reader.GetOrdinal("CountryId")) ? 0 : reader.GetInt32(reader.GetOrdinal("CountryId")),
                        PartyId = reader.IsDBNull(reader.GetOrdinal("PartyId")) ? null : reader.GetInt32(reader.GetOrdinal("PartyId")),
                        Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? string.Empty : reader.GetString(reader.GetOrdinal("Name")),
                        Loyalty = reader.IsDBNull(reader.GetOrdinal("Loyalty")) ? 0 : reader.GetInt32(reader.GetOrdinal("Loyalty")),
                        Corruption = reader.IsDBNull(reader.GetOrdinal("Corruption")) ? 0 : reader.GetInt32(reader.GetOrdinal("Corruption")),
                        Influence = reader.IsDBNull(reader.GetOrdinal("Influence")) ? 0 : reader.GetInt32(reader.GetOrdinal("Influence")),
                        AttendanceChance = reader.IsDBNull(reader.GetOrdinal("AttendanceChance")) ? 0 : reader.GetInt32(reader.GetOrdinal("AttendanceChance")),
                        IsAlive = reader.GetBoolean(reader.GetOrdinal("IsAlive"))
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
