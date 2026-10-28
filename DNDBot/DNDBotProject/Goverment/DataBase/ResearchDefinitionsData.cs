#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using DNDBot.Goverment.Research;

namespace DNDBot.DataBase
{
    public static class ResearchDefinitionsData
    {
        static ResearchDefinitionsData()
        {
            BaseRepository.DBInitialization();
        }

        public sealed class ResearchDefinitionRecord
        {
            public int ResearchId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Branch { get; set; } = "Загальне";
            public string Description { get; set; } = string.Empty;
            public int ScienceCost { get; set; }
            public long MoneyCost { get; set; }
            public int DurationDays { get; set; }
            public int SortOrder { get; set; }
            public bool IsActive { get; set; }
        }

        public static List<ResearchDefinition> GetAllActive()
        {
            const string q = @"
SELECT ResearchId, Name, Branch, Description, ScienceCost, MoneyCost, DurationDays, SortOrder, IsActive
FROM ResearchDefinitions
WHERE IsActive = 1
ORDER BY SortOrder, ResearchId;";
            var defs = new List<ResearchDefinitionRecord>();
            using var reader = BaseRepository.ExecuteReader(q);
            try
            {
                while (reader.Read())
                {
                    defs.Add(new ResearchDefinitionRecord
                    {
                        ResearchId = reader.GetInt32(reader.GetOrdinal("ResearchId")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        Branch = reader.GetString(reader.GetOrdinal("Branch")),
                        Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? string.Empty : reader.GetString(reader.GetOrdinal("Description")),
                        ScienceCost = reader.GetInt32(reader.GetOrdinal("ScienceCost")),
                        MoneyCost = reader.GetInt64(reader.GetOrdinal("MoneyCost")),
                        DurationDays = reader.GetInt32(reader.GetOrdinal("DurationDays")),
                        SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                    });
                }
            }
            finally
            {
                reader.Dispose();
            }

            if (defs.Count == 0)
                return new List<ResearchDefinition>();

            var req = ResearchRequirementsData.GetAll();
            var eff = ResearchEffectsData.GetAll();

            return defs.Select(d => new ResearchDefinition
            {
                Id = d.ResearchId,
                Name = d.Name,
                Branch = d.Branch,
                Description = d.Description,
                ScienceCost = d.ScienceCost,
                MoneyCost = d.MoneyCost,
                DurationDays = d.DurationDays,
                SortOrder = d.SortOrder,
                IsActive = d.IsActive,
                Requirements = req.Where(r => r.ResearchId == d.ResearchId).Select(r => r.RequiredResearchId).ToList(),
                Effects = eff.Where(e => e.ResearchId == d.ResearchId).OrderBy(e => e.SortOrder).Select(e => new ResearchEffect
                {
                    Type = (ResearchEffectType)e.EffectType,
                    Value = e.EffectValue ?? string.Empty,
                    Amount = e.EffectAmount
                }).ToList()
            }).ToList();
        }

        public static ResearchDefinition? GetById(int researchId)
        {
            const string q = @"
SELECT ResearchId, Name, Branch, Description, ScienceCost, MoneyCost, DurationDays, SortOrder, IsActive
FROM ResearchDefinitions
WHERE ResearchId = @ResearchId;";
            using var reader = BaseRepository.ExecuteReader(q,
                BaseRepository.Param("@ResearchId", SqlDbType.Int, researchId));
            try
            {
                if (!reader.Read())
                    return null;

                var def = new ResearchDefinition
                {
                    Id = reader.GetInt32(reader.GetOrdinal("ResearchId")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Branch = reader.GetString(reader.GetOrdinal("Branch")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? string.Empty : reader.GetString(reader.GetOrdinal("Description")),
                    ScienceCost = reader.GetInt32(reader.GetOrdinal("ScienceCost")),
                    MoneyCost = reader.GetInt64(reader.GetOrdinal("MoneyCost")),
                    DurationDays = reader.GetInt32(reader.GetOrdinal("DurationDays")),
                    SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                };

                def.Requirements = ResearchRequirementsData.GetByResearch(def.Id);
                def.Effects = ResearchEffectsData.GetByResearch(def.Id);
                return def;
            }
            finally
            {
                reader.Dispose();
            }
        }

        public static HashSet<int> GetAllIds()
        {
            const string q = @"
SELECT ResearchId
FROM ResearchDefinitions;";
            var set = new HashSet<int>();
            using var reader = BaseRepository.ExecuteReader(q);
            try
            {
                while (reader.Read())
                {
                    set.Add(reader.GetInt32(reader.GetOrdinal("ResearchId")));
                }
            }
            finally
            {
                reader.Dispose();
            }
            return set;
        }

        public static int Add(ResearchDefinition def)
        {
            const string q = @"
INSERT INTO ResearchDefinitions (Name, Branch, Description, ScienceCost, MoneyCost, DurationDays, SortOrder, IsActive)
VALUES (@Name, @Branch, @Description, @ScienceCost, @MoneyCost, @DurationDays, @SortOrder, @IsActive);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var idObj = BaseRepository.ExecuteScalar(q,
                BaseRepository.Param("@Name", SqlDbType.NVarChar, def.Name, 100),
                BaseRepository.Param("@Branch", SqlDbType.NVarChar, def.Branch, 50),
                BaseRepository.Param("@Description", SqlDbType.NVarChar, def.Description ?? string.Empty, 500),
                BaseRepository.Param("@ScienceCost", SqlDbType.Int, def.ScienceCost),
                BaseRepository.Param("@MoneyCost", SqlDbType.BigInt, def.MoneyCost),
                BaseRepository.Param("@DurationDays", SqlDbType.Int, def.DurationDays),
                BaseRepository.Param("@SortOrder", SqlDbType.Int, def.SortOrder),
                BaseRepository.Param("@IsActive", SqlDbType.Bit, def.IsActive)
            );
            return idObj == null || idObj == DBNull.Value ? 0 : Convert.ToInt32(idObj);
        }

        public static void Update(ResearchDefinition def)
        {
            const string q = @"
UPDATE ResearchDefinitions
SET Name = @Name,
    Branch = @Branch,
    Description = @Description,
    ScienceCost = @ScienceCost,
    MoneyCost = @MoneyCost,
    DurationDays = @DurationDays,
    SortOrder = @SortOrder,
    IsActive = @IsActive
WHERE ResearchId = @ResearchId;";

            BaseRepository.ExecuteNonQuery(q,
                BaseRepository.Param("@Name", SqlDbType.NVarChar, def.Name, 100),
                BaseRepository.Param("@Branch", SqlDbType.NVarChar, def.Branch, 50),
                BaseRepository.Param("@Description", SqlDbType.NVarChar, def.Description ?? string.Empty, 500),
                BaseRepository.Param("@ScienceCost", SqlDbType.Int, def.ScienceCost),
                BaseRepository.Param("@MoneyCost", SqlDbType.BigInt, def.MoneyCost),
                BaseRepository.Param("@DurationDays", SqlDbType.Int, def.DurationDays),
                BaseRepository.Param("@SortOrder", SqlDbType.Int, def.SortOrder),
                BaseRepository.Param("@IsActive", SqlDbType.Bit, def.IsActive),
                BaseRepository.Param("@ResearchId", SqlDbType.Int, def.Id)
            );
        }
    }
}

