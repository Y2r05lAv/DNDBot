using DNDBot.DataBase;
using Microsoft.Data.SqlClient;
using System;
using System.Configuration;

namespace DNDBot
{
    public class ResourceModifiersRecord
    {
        public long UserId { get; set; }
        public int CharacterId { get; set; }
        public int MaxManaPercentMod { get; set; }
        public int CurrentManaPercentMod { get; set; }
        public int BaseManaPercentMod { get; set; }
        public int MaxHPPercentMod { get; set; }
        public int CurrentHPPercentMod { get; set; }
        public int BaseHPPercentMod { get; set; }
    }

    public static class ResourceModifiers
    {
        private static SqlConnection sqlConnection = BaseRepository.SqlConnection;

        public static ResourceModifiersRecord Get(long userId, int characterId)
        {
            const string q = @"SELECT * FROM ResourceModifiers WHERE UserId = @UserId AND CharacterId = @CharacterId";
            using var cmd = new SqlCommand(q, sqlConnection);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@CharacterId", characterId);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return new ResourceModifiersRecord { UserId = userId, CharacterId = characterId };
            return new ResourceModifiersRecord
            {
                UserId = userId,
                CharacterId = characterId,
                MaxManaPercentMod = reader.GetInt32(reader.GetOrdinal("MaxManaPercentMod")),
                CurrentManaPercentMod = reader.GetInt32(reader.GetOrdinal("CurrentManaPercentMod")),
                BaseManaPercentMod = reader.GetInt32(reader.GetOrdinal("BaseManaPercentMod")),
                MaxHPPercentMod = reader.GetInt32(reader.GetOrdinal("MaxHPPercentMod")),
                CurrentHPPercentMod = reader.GetInt32(reader.GetOrdinal("CurrentHPPercentMod")),
                BaseHPPercentMod = reader.GetInt32(reader.GetOrdinal("BaseHPPercentMod"))
            };
        }

         
        public static void Upsert(ResourceModifiersRecord r)
        {
            const string q = @"
MERGE ResourceModifiers AS target
USING (SELECT @UserId AS UserId, @CharacterId AS CharacterId) AS src
ON (target.UserId = src.UserId AND target.CharacterId = src.CharacterId)
WHEN MATCHED THEN 
    UPDATE SET MaxManaPercentMod=@MaxManaPercentMod, CurrentManaPercentMod=@CurrentManaPercentMod, BaseManaPercentMod=@BaseManaPercentMod,
               MaxHPPercentMod=@MaxHPPercentMod, CurrentHPPercentMod=@CurrentHPPercentMod, BaseHPPercentMod=@BaseHPPercentMod
WHEN NOT MATCHED THEN
    INSERT (UserId, CharacterId, MaxManaPercentMod, CurrentManaPercentMod, BaseManaPercentMod, MaxHPPercentMod, CurrentHPPercentMod, BaseHPPercentMod)
    VALUES (@UserId, @CharacterId, @MaxManaPercentMod, @CurrentManaPercentMod, @BaseManaPercentMod, @MaxHPPercentMod, @CurrentHPPercentMod, @BaseHPPercentMod);";
            using var cmd = new SqlCommand(q, sqlConnection);
            cmd.Parameters.AddWithValue("@UserId", r.UserId);
            cmd.Parameters.AddWithValue("@CharacterId", r.CharacterId);
            cmd.Parameters.AddWithValue("@MaxManaPercentMod", r.MaxManaPercentMod);
            cmd.Parameters.AddWithValue("@CurrentManaPercentMod", r.CurrentManaPercentMod);
            cmd.Parameters.AddWithValue("@BaseManaPercentMod", r.BaseManaPercentMod);
            cmd.Parameters.AddWithValue("@MaxHPPercentMod", r.MaxHPPercentMod);
            cmd.Parameters.AddWithValue("@CurrentHPPercentMod", r.CurrentHPPercentMod);
            cmd.Parameters.AddWithValue("@BaseHPPercentMod", r.BaseHPPercentMod);
            cmd.ExecuteNonQuery();
        }

         
        public static void ApplyManaModifiersAfterRecalc(long userId, int characterId, bool keepCurrentProportion = true)
        {
            var mods = Get(userId, characterId);

             
            int prevEffectiveMax = ManaData.DataBaseValueReader<int>(userId, characterId, "MaxMana");
            int current = ManaData.DataBaseValueReader<int>(userId, characterId, "CurrentMana");

             
            int baseMana = ManaData.DataBaseValueReader<int>(userId, characterId, "BaseMana");
            int magicLevel = ManaData.DataBaseValueReader<int>(userId, characterId, "MagicLevel");

            if (baseMana <= 0) baseMana = 1;
            if (magicLevel <= 0) magicLevel = 1;

            double levelMultiplier = Math.Pow(1.12, Math.Max(1, magicLevel) - 1);  
            int rawMax = (int)Math.Round(baseMana * levelMultiplier);

             
            double maxMultiplier = 1.0 + mods.MaxManaPercentMod / 100.0;
            int effectiveMax = (int)Math.Round(rawMax * maxMultiplier);

             
            int newCurrent;
            if (keepCurrentProportion)
            {
                 
                 
                double prop = prevEffectiveMax > 0 ? (double)current / prevEffectiveMax : (double)current / Math.Max(1, rawMax);
                newCurrent = (int)Math.Round(prop * effectiveMax);
            }
            else
            {
                double curMultiplier = 1.0 + mods.CurrentManaPercentMod / 100.0;
                newCurrent = (int)Math.Round(current * curMultiplier);
            }

            newCurrent = Math.Clamp(newCurrent, 0, Math.Max(0, effectiveMax));

             
            ManaData.UpdateMagicStat(userId, characterId, "MaxMana", effectiveMax);
            ManaData.UpdateMagicStat(userId, characterId, "CurrentMana", newCurrent);
        }
    }
}