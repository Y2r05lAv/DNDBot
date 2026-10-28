using System;
using System.Collections.Generic;
using System.Data;

namespace DNDBot.DataBase
{
     
    public class PartyMemberRecord
    {
        public int PartyId { get; set; }
        public int DeputyId { get; set; }
    }

    public static class PartyMembersData
    {
        static PartyMembersData()
        {
            BaseRepository.DBInitialization();
        }

        public static string AddPartyMember(PartyMemberRecord member)
        {
            const string insert = @"
INSERT INTO PartyMembers (PartyId, DeputyId)
VALUES (@PartyId, @DeputyId);";

            int rows = BaseRepository.ExecuteNonQuery(insert,
                BaseRepository.Param("@PartyId", SqlDbType.Int, member.PartyId),
                BaseRepository.Param("@DeputyId", SqlDbType.Int, member.DeputyId)
            );

            return rows > 0 ? "Член партії доданий." : "Не вдалося додати члена партії.";
        }

        public static string DeletePartyMember(int partyId, int deputyId)
        {
            const string delete = "DELETE FROM PartyMembers WHERE PartyId = @PartyId AND DeputyId = @DeputyId";
            int rows = BaseRepository.ExecuteNonQuery(delete,
                BaseRepository.Param("@PartyId", SqlDbType.Int, partyId),
                BaseRepository.Param("@DeputyId", SqlDbType.Int, deputyId));
            return rows > 0 ? "Член партії видалений." : "Члена партії не знайдено.";
        }

        public static PartyMemberRecord GetPartyMember(int partyId, int deputyId)
        {
            const string q = @"
SELECT PartyId, DeputyId
FROM PartyMembers
WHERE PartyId = @PartyId AND DeputyId = @DeputyId";

            using var reader = BaseRepository.ExecuteReader(q,
                BaseRepository.Param("@PartyId", SqlDbType.Int, partyId),
                BaseRepository.Param("@DeputyId", SqlDbType.Int, deputyId));
            try
            {
                if (reader.Read())
                {
                    return new PartyMemberRecord
                    {
                        PartyId = reader.GetInt32(reader.GetOrdinal("PartyId")),
                        DeputyId = reader.GetInt32(reader.GetOrdinal("DeputyId"))
                    };
                }
            }
            finally
            {
                reader.Dispose();
            }

            return null;
        }

        public static List<PartyMemberRecord> GetAllPartyMembers()
        {
            const string q = @"
SELECT PartyId, DeputyId
FROM PartyMembers
ORDER BY PartyId, DeputyId";

            var list = new List<PartyMemberRecord>();
            using var reader = BaseRepository.ExecuteReader(q);
            try
            {
                while (reader.Read())
                {
                    list.Add(new PartyMemberRecord
                    {
                        PartyId = reader.GetInt32(reader.GetOrdinal("PartyId")),
                        DeputyId = reader.GetInt32(reader.GetOrdinal("DeputyId"))
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
