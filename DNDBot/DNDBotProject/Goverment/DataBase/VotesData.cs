using System;
using System.Collections.Generic;
using System.Data;

namespace DNDBot.DataBase
{
     
    public class VoteRecord
    {
        public int LawId { get; set; }
        public int DeputyId { get; set; }
        public string VoteType { get; set; }
    }

    public static class VotesData
    {
        static VotesData()
        {
            BaseRepository.DBInitialization();
        }

        public static string AddVote(VoteRecord vote)
        {
            const string insert = @"
INSERT INTO Votes (LawId, DeputyId, VoteType)
VALUES (@LawId, @DeputyId, @VoteType);";

            int rows = BaseRepository.ExecuteNonQuery(insert,
                BaseRepository.Param("@LawId", SqlDbType.Int, vote.LawId),
                BaseRepository.Param("@DeputyId", SqlDbType.Int, vote.DeputyId),
                BaseRepository.Param("@VoteType", SqlDbType.NVarChar, vote.VoteType, 10)
            );

            return rows > 0 ? "Голос доданий." : "Не вдалося додати голос.";
        }

        public static string UpdateVote(VoteRecord vote)
        {
            const string update = @"
UPDATE Votes
SET VoteType = @VoteType
WHERE LawId = @LawId AND DeputyId = @DeputyId";

            int rows = BaseRepository.ExecuteNonQuery(update,
                BaseRepository.Param("@VoteType", SqlDbType.NVarChar, vote.VoteType, 10),
                BaseRepository.Param("@LawId", SqlDbType.Int, vote.LawId),
                BaseRepository.Param("@DeputyId", SqlDbType.Int, vote.DeputyId)
            );

            return rows > 0 ? "Голос оновлений." : "Голос не знайдено або нічого не змінено.";
        }

        public static string DeleteVote(int lawId, int deputyId)
        {
            const string delete = "DELETE FROM Votes WHERE LawId = @LawId AND DeputyId = @DeputyId";
            int rows = BaseRepository.ExecuteNonQuery(delete,
                BaseRepository.Param("@LawId", SqlDbType.Int, lawId),
                BaseRepository.Param("@DeputyId", SqlDbType.Int, deputyId));
            return rows > 0 ? "Голос видалений." : "Голос не знайдено.";
        }

        public static VoteRecord GetVote(int lawId, int deputyId)
        {
            const string q = @"
SELECT LawId, DeputyId, VoteType
FROM Votes
WHERE LawId = @LawId AND DeputyId = @DeputyId";

            using var reader = BaseRepository.ExecuteReader(q,
                BaseRepository.Param("@LawId", SqlDbType.Int, lawId),
                BaseRepository.Param("@DeputyId", SqlDbType.Int, deputyId));
            try
            {
                if (reader.Read())
                {
                    return new VoteRecord
                    {
                        LawId = reader.GetInt32(reader.GetOrdinal("LawId")),
                        DeputyId = reader.GetInt32(reader.GetOrdinal("DeputyId")),
                        VoteType = reader.IsDBNull(reader.GetOrdinal("VoteType")) ? string.Empty : reader.GetString(reader.GetOrdinal("VoteType"))
                    };
                }
            }
            finally
            {
                reader.Dispose();
            }

            return null;
        }

        public static List<VoteRecord> GetAllVotes()
        {
            const string q = @"
SELECT LawId, DeputyId, VoteType
FROM Votes
ORDER BY LawId, DeputyId";

            var list = new List<VoteRecord>();
            using var reader = BaseRepository.ExecuteReader(q);
            try
            {
                while (reader.Read())
                {
                    list.Add(new VoteRecord
                    {
                        LawId = reader.GetInt32(reader.GetOrdinal("LawId")),
                        DeputyId = reader.GetInt32(reader.GetOrdinal("DeputyId")),
                        VoteType = reader.IsDBNull(reader.GetOrdinal("VoteType")) ? string.Empty : reader.GetString(reader.GetOrdinal("VoteType"))
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
