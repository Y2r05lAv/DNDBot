#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DNDBot.DataBase
{
     
    public class PartyRecord
    {
        public int PartyId { get; set; }
        public int CountryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Ideology { get; set; } = string.Empty;
        public int Discipline { get; set; }
        public bool GovernmentAligned { get; set; }
        public int Influence { get; set; }
    }

    public static class PartiesData
    {
        static PartiesData()
        {
            BaseRepository.DBInitialization();
        }

        public static string AddParty(PartyRecord party)
        {
            const string insert = @"
INSERT INTO Parties (CountryId, Name, Ideology, Discipline, GovernmentAligned, Influence)
VALUES (@CountryId, @Name, @Ideology, @Discipline, @GovernmentAligned, @Influence);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var idObj = BaseRepository.ExecuteScalar(insert,
                BaseRepository.Param("@CountryId", SqlDbType.Int, party.CountryId),
                BaseRepository.Param("@Name", SqlDbType.NVarChar, party.Name, 100),
                BaseRepository.Param("@Ideology", SqlDbType.NVarChar, party.Ideology, 100),
                BaseRepository.Param("@Discipline", SqlDbType.Int, party.Discipline),
                BaseRepository.Param("@GovernmentAligned", SqlDbType.Bit, party.GovernmentAligned),
                BaseRepository.Param("@Influence", SqlDbType.Int, party.Influence)
            );

            if (idObj == null || idObj == DBNull.Value)
                return "Помилка додавання партії.";

            return $"Партія додана з Id = {Convert.ToInt32(idObj)}";
        }

        public static string UpdateParty(PartyRecord party)
        {
            const string update = @"
UPDATE Parties
SET CountryId = @CountryId,
    Name = @Name,
    Ideology = @Ideology,
    Discipline = @Discipline,
    GovernmentAligned = @GovernmentAligned,
    Influence = @Influence
WHERE PartyId = @PartyId";

            int rows = BaseRepository.ExecuteNonQuery(update,
                BaseRepository.Param("@CountryId", SqlDbType.Int, party.CountryId),
                BaseRepository.Param("@Name", SqlDbType.NVarChar, party.Name, 100),
                BaseRepository.Param("@Ideology", SqlDbType.NVarChar, party.Ideology, 100),
                BaseRepository.Param("@Discipline", SqlDbType.Int, party.Discipline),
                BaseRepository.Param("@GovernmentAligned", SqlDbType.Bit, party.GovernmentAligned),
                BaseRepository.Param("@Influence", SqlDbType.Int, party.Influence),
                BaseRepository.Param("@PartyId", SqlDbType.Int, party.PartyId)
            );

            return rows > 0 ? "Партія оновлена." : "Партію не знайдено або нічого не змінено.";
        }

        public static string DeleteParty(int partyId)
        {
            const string delete = "DELETE FROM Parties WHERE PartyId = @PartyId";
            int rows = BaseRepository.ExecuteNonQuery(delete,
                BaseRepository.Param("@PartyId", SqlDbType.Int, partyId));
            return rows > 0 ? "Партія видалена." : "Партію не знайдено.";
        }

        public static PartyRecord? GetPartyById(int partyId)
        {
            const string q = @"
SELECT PartyId, CountryId, Name, Ideology, Discipline, GovernmentAligned, Influence
FROM Parties
WHERE PartyId = @PartyId";

            using var reader = BaseRepository.ExecuteReader(q,
                BaseRepository.Param("@PartyId", SqlDbType.Int, partyId));
            try
            {
                if (reader.Read())
                {
                    return new PartyRecord
                    {
                        PartyId = reader.GetInt32(reader.GetOrdinal("PartyId")),
                        CountryId = reader.GetInt32(reader.GetOrdinal("CountryId")),
                        Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? string.Empty : reader.GetString(reader.GetOrdinal("Name")),
                        Ideology = reader.IsDBNull(reader.GetOrdinal("Ideology")) ? string.Empty : reader.GetString(reader.GetOrdinal("Ideology")),
                        Discipline = reader.GetInt32(reader.GetOrdinal("Discipline")),
                        GovernmentAligned = reader.GetBoolean(reader.GetOrdinal("GovernmentAligned")),
                        Influence = reader.GetInt32(reader.GetOrdinal("Influence"))
                    };
                }
            }
            finally
            {
                reader.Dispose();
            }

            return null;
        }

        public static List<PartyRecord> GetAllParties()
        {
            const string q = @"
SELECT PartyId, CountryId, Name, Ideology, Discipline, GovernmentAligned, Influence
FROM Parties
ORDER BY PartyId";

            var list = new List<PartyRecord>();
            using var reader = BaseRepository.ExecuteReader(q);
            try
            {
                while (reader.Read())
                {
                    list.Add(new PartyRecord
                    {
                        PartyId = reader.GetInt32(reader.GetOrdinal("PartyId")),
                        CountryId = reader.GetInt32(reader.GetOrdinal("CountryId")),
                        Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? string.Empty : reader.GetString(reader.GetOrdinal("Name")),
                        Ideology = reader.IsDBNull(reader.GetOrdinal("Ideology")) ? string.Empty : reader.GetString(reader.GetOrdinal("Ideology")),
                        Discipline = reader.GetInt32(reader.GetOrdinal("Discipline")),
                        GovernmentAligned = reader.GetBoolean(reader.GetOrdinal("GovernmentAligned")),
                        Influence = reader.GetInt32(reader.GetOrdinal("Influence"))
                    });
                }
            }
            finally
            {
                reader.Dispose();
            }
            return list;
        }

        public static string GetPartyNameById(int partyId)
        {
            const string q = @"
SELECT Name
FROM Parties
WHERE PartyId = @PartyId";

            var obj = BaseRepository.ExecuteScalar(q,
                BaseRepository.Param("@PartyId", SqlDbType.Int, partyId));

            if (obj == null || obj == DBNull.Value)
                return "Невідома партія";

            string? name = Convert.ToString(obj);
            return string.IsNullOrWhiteSpace(name) ? "Невідома партія" : name;
        }
    }
}

