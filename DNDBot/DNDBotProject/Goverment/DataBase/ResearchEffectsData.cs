#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using DNDBot.Goverment.Research;

namespace DNDBot.DataBase
{
    public static class ResearchEffectsData
    {
        static ResearchEffectsData()
        {
            BaseRepository.DBInitialization();
        }

        public sealed class ResearchEffectRecord
        {
            public int ResearchId { get; set; }
            public int EffectType { get; set; }
            public string? EffectValue { get; set; }
            public float EffectAmount { get; set; }
            public int SortOrder { get; set; }
        }

        public static List<ResearchEffectRecord> GetAll()
        {
            const string q = "SELECT ResearchId, EffectType, EffectValue, EffectAmount, SortOrder FROM ResearchEffects";
            var list = new List<ResearchEffectRecord>();
            using var reader = BaseRepository.ExecuteReader(q);
            try
            {
                while (reader.Read())
                {
                    list.Add(new ResearchEffectRecord
                    {
                        ResearchId = reader.GetInt32(reader.GetOrdinal("ResearchId")),
                        EffectType = reader.GetInt32(reader.GetOrdinal("EffectType")),
                        EffectValue = reader.IsDBNull(reader.GetOrdinal("EffectValue")) ? null : reader.GetString(reader.GetOrdinal("EffectValue")),
                        EffectAmount = reader.GetFloat(reader.GetOrdinal("EffectAmount")),
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

        public static List<ResearchEffect> GetByResearch(int researchId)
        {
            const string q = @"
SELECT ResearchId, EffectType, EffectValue, EffectAmount, SortOrder
FROM ResearchEffects
WHERE ResearchId = @ResearchId
ORDER BY SortOrder;";

            var list = new List<ResearchEffect>();
            using var reader = BaseRepository.ExecuteReader(q,
                BaseRepository.Param("@ResearchId", SqlDbType.Int, researchId));
            try
            {
                while (reader.Read())
                {
                    list.Add(new ResearchEffect
                    {
                        Type = (ResearchEffectType)reader.GetInt32(reader.GetOrdinal("EffectType")),
                        Value = reader.IsDBNull(reader.GetOrdinal("EffectValue")) ? string.Empty : reader.GetString(reader.GetOrdinal("EffectValue")),
                        Amount = reader.GetFloat(reader.GetOrdinal("EffectAmount"))
                    });
                }
            }
            finally
            {
                reader.Dispose();
            }
            return list;
        }

        public static void ReplaceEffects(int researchId, List<ResearchEffect> effects)
        {
            const string del = "DELETE FROM ResearchEffects WHERE ResearchId = @ResearchId";
            BaseRepository.ExecuteNonQuery(del,
                BaseRepository.Param("@ResearchId", SqlDbType.Int, researchId));

            if (effects == null || effects.Count == 0)
                return;

            const string ins = @"
INSERT INTO ResearchEffects (ResearchId, EffectType, EffectValue, EffectAmount, SortOrder)
VALUES (@ResearchId, @EffectType, @EffectValue, @EffectAmount, @SortOrder);";

            int order = 0;
            foreach (var e in effects)
            {
                BaseRepository.ExecuteNonQuery(ins,
                    BaseRepository.Param("@ResearchId", SqlDbType.Int, researchId),
                    BaseRepository.Param("@EffectType", SqlDbType.Int, (int)e.Type),
                    BaseRepository.Param("@EffectValue", SqlDbType.NVarChar, (object?)e.Value ?? DBNull.Value, 100),
                    BaseRepository.Param("@EffectAmount", SqlDbType.Real, e.Amount),
                    BaseRepository.Param("@SortOrder", SqlDbType.Int, order++)
                );
            }
        }
    }
}

