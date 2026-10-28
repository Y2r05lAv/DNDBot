using System;
using System.Collections.Generic;
using System.Data;

namespace DNDBot.DataBase
{
    public static class ResearchBranchesData
    {
        static ResearchBranchesData()
        {
            BaseRepository.DBInitialization();
        }

        public sealed class ResearchBranchRecord
        {
            public string BranchName { get; set; } = string.Empty;
            public int SortOrder { get; set; }
        }

        public static List<ResearchBranchRecord> GetAll()
        {
            const string q = @"
SELECT BranchName, SortOrder
FROM ResearchBranches
ORDER BY SortOrder, BranchName;";
            var list = new List<ResearchBranchRecord>();
            using var reader = BaseRepository.ExecuteReader(q);
            try
            {
                while (reader.Read())
                {
                    list.Add(new ResearchBranchRecord
                    {
                        BranchName = reader.GetString(reader.GetOrdinal("BranchName")),
                        SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder"))
                    });
                }
            }
            finally
            {
                reader.Dispose();
            }
            return list;
        }

        public static ResearchBranchRecord EnsureBranch(string branchName)
        {
            var name = branchName?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                name = "Загальне";

            const string q = @"
SELECT BranchName, SortOrder
FROM ResearchBranches
WHERE BranchName = @BranchName;";
            using var reader = BaseRepository.ExecuteReader(q,
                BaseRepository.Param("@BranchName", SqlDbType.NVarChar, name, 50));
            try
            {
                if (reader.Read())
                {
                    return new ResearchBranchRecord
                    {
                        BranchName = reader.GetString(reader.GetOrdinal("BranchName")),
                        SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder"))
                    };
                }
            }
            finally
            {
                reader.Dispose();
            }

            int nextOrder = 0;
            const string qMax = "SELECT ISNULL(MAX(SortOrder), 0) FROM ResearchBranches";
            var maxObj = BaseRepository.ExecuteScalar(qMax);
            if (maxObj != null && maxObj != DBNull.Value)
                nextOrder = Convert.ToInt32(maxObj) + 10;

            const string insert = @"
INSERT INTO ResearchBranches (BranchName, SortOrder)
VALUES (@BranchName, @SortOrder);";
            BaseRepository.ExecuteNonQuery(insert,
                BaseRepository.Param("@BranchName", SqlDbType.NVarChar, name, 50),
                BaseRepository.Param("@SortOrder", SqlDbType.Int, nextOrder)
            );

            return new ResearchBranchRecord { BranchName = name, SortOrder = nextOrder };
        }
    }
}
