using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DNDBot.DataBase
{
    public static class ResearchRequirementsData
    {
        static ResearchRequirementsData()
        {
            BaseRepository.DBInitialization();
        }

        public sealed class ResearchRequirementRecord
        {
            public int ResearchId { get; set; }
            public int RequiredResearchId { get; set; }
        }

        public static List<ResearchRequirementRecord> GetAll()
        {
            const string q = "SELECT ResearchId, RequiredResearchId FROM ResearchRequirements";
            var list = new List<ResearchRequirementRecord>();
            using var reader = BaseRepository.ExecuteReader(q);
            try
            {
                while (reader.Read())
                {
                    list.Add(new ResearchRequirementRecord
                    {
                        ResearchId = reader.GetInt32(reader.GetOrdinal("ResearchId")),
                        RequiredResearchId = reader.GetInt32(reader.GetOrdinal("RequiredResearchId"))
                    });
                }
            }
            finally
            {
                reader.Dispose();
            }
            return list;
        }

        public static List<int> GetByResearch(int researchId)
        {
            const string q = "SELECT RequiredResearchId FROM ResearchRequirements WHERE ResearchId = @ResearchId";
            var list = new List<int>();
            using var reader = BaseRepository.ExecuteReader(q,
                BaseRepository.Param("@ResearchId", SqlDbType.Int, researchId));
            try
            {
                while (reader.Read())
                    list.Add(reader.GetInt32(reader.GetOrdinal("RequiredResearchId")));
            }
            finally
            {
                reader.Dispose();
            }
            return list;
        }

        public static void ReplaceRequirements(int researchId, List<int> requirements)
        {
            const string del = "DELETE FROM ResearchRequirements WHERE ResearchId = @ResearchId";
            BaseRepository.ExecuteNonQuery(del,
                BaseRepository.Param("@ResearchId", SqlDbType.Int, researchId));

            if (requirements == null || requirements.Count == 0)
                return;

            const string ins = @"
INSERT INTO ResearchRequirements (ResearchId, RequiredResearchId)
VALUES (@ResearchId, @RequiredResearchId);";

            foreach (var req in requirements.Distinct())
            {
                BaseRepository.ExecuteNonQuery(ins,
                    BaseRepository.Param("@ResearchId", SqlDbType.Int, researchId),
                    BaseRepository.Param("@RequiredResearchId", SqlDbType.Int, req)
                );
            }
        }
    }
}
