#nullable enable
using System;
using System.Collections.Generic;
using System.Data;

namespace DNDBot.DataBase
{
    public sealed class EconomyGlobalModifierRecord
    {
        public int ModifierId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ModifierType { get; set; } = string.Empty;
        public DateTime StartAt { get; set; }
        public int DurationDays { get; set; }
        public bool IsActive { get; set; }
        public float ProductionModifier { get; set; }
        public float TradeModifier { get; set; }
        public float StabilityModifier { get; set; }
        public float IncomeModifier { get; set; }
        public float UnemploymentModifier { get; set; }
        public float DemandModifier { get; set; }
        public string? DemandResourceType { get; set; }
    }

    public static class EconomyGlobalModifiersData
    {
        static EconomyGlobalModifiersData()
        {
            BaseRepository.DBInitialization();
        }

        public static List<EconomyGlobalModifierRecord> GetActive()
        {
            const string q = @"
SELECT ModifierId, Name, ModifierType, StartAt, DurationDays, IsActive,
       ProductionModifier, TradeModifier, StabilityModifier, IncomeModifier, UnemploymentModifier,
       DemandModifier, DemandResourceType
FROM EconomyGlobalModifiers
WHERE IsActive = 1
  AND DATEADD(day, DurationDays, StartAt) >= GETDATE()";

            var list = new List<EconomyGlobalModifierRecord>();
            try
            {
                using var reader = BaseRepository.ExecuteReader(q);
                while (reader.Read())
                {
                    list.Add(new EconomyGlobalModifierRecord
                    {
                        ModifierId = reader.GetInt32(reader.GetOrdinal("ModifierId")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        ModifierType = reader.GetString(reader.GetOrdinal("ModifierType")),
                        StartAt = reader.GetDateTime(reader.GetOrdinal("StartAt")),
                        DurationDays = reader.GetInt32(reader.GetOrdinal("DurationDays")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                        ProductionModifier = Convert.ToSingle(reader["ProductionModifier"]),
                        TradeModifier = Convert.ToSingle(reader["TradeModifier"]),
                        StabilityModifier = Convert.ToSingle(reader["StabilityModifier"]),
                        IncomeModifier = Convert.ToSingle(reader["IncomeModifier"]),
                        UnemploymentModifier = Convert.ToSingle(reader["UnemploymentModifier"]),
                        DemandModifier = Convert.ToSingle(reader["DemandModifier"]),
                        DemandResourceType = reader["DemandResourceType"] == DBNull.Value ? null : Convert.ToString(reader["DemandResourceType"])
                    });
                }
            }
            catch
            {
                return list;
            }

            return list;
        }
    }
}

