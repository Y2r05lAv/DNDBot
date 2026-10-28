#nullable enable
using System;
using System.Collections.Generic;
using System.Data;

namespace DNDBot.DataBase
{
    public sealed class MilitaryUnitRecord
    {
        public int UnitId { get; set; }
        public int CountryId { get; set; }
        public int? ParentUnitId { get; set; }
        public string UnitName { get; set; } = "Частина";
        public string UnitLevel { get; set; } = "Squad";
        public string UnitRole { get; set; } = "Field";
        public string UnitType { get; set; } = "Infantry";
        public string Race { get; set; } = "Human";
        public int Soldiers { get; set; }
        public string EquipmentTier { get; set; } = "Standard";
        public float EquipmentQuality { get; set; } = 50f;
        public float BattleExperience { get; set; } = 15f;
        public float Fatigue { get; set; }
        public float TrainingLevel { get; set; }
        public float Morale { get; set; }
        public float WeaponReadiness { get; set; }
        public float SupplyLevel { get; set; }
        public string? CommanderName { get; set; }
        public float CommanderSkill { get; set; }
        public float CommanderLoyalty { get; set; }
        public float CommanderCorruption { get; set; }
        public float CommanderExperience { get; set; }
        public string Status { get; set; } = "Forming";
        public bool IsMobilized { get; set; }
        public float FormationProgress { get; set; }
        public int FormationBaseDays { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public static class MilitaryUnitsData
    {
        static MilitaryUnitsData()
        {
            BaseRepository.DBInitialization();
        }

        public static List<MilitaryUnitRecord> GetByCountry(int countryId)
        {
            const string q = @"
SELECT UnitId, CountryId, ParentUnitId, UnitName, UnitLevel, UnitRole, UnitType, Race, Soldiers,
       EquipmentTier, EquipmentQuality, BattleExperience, Fatigue,
       TrainingLevel, Morale, WeaponReadiness, SupplyLevel,
       CommanderName, CommanderSkill, CommanderLoyalty, CommanderCorruption, CommanderExperience,
       [Status], IsMobilized, FormationProgress, FormationBaseDays, CreatedAt, UpdatedAt
FROM MilitaryUnits
WHERE CountryId = @CountryId
ORDER BY UnitId;";

            var list = new List<MilitaryUnitRecord>();
            try
            {
                using var reader = BaseRepository.ExecuteReader(q, BaseRepository.Param("@CountryId", SqlDbType.Int, countryId));
                try
                {
                    while (reader.Read())
                    {
                        list.Add(new MilitaryUnitRecord
                        {
                            UnitId = reader.GetInt32(reader.GetOrdinal("UnitId")),
                            CountryId = reader.GetInt32(reader.GetOrdinal("CountryId")),
                            ParentUnitId = reader.IsDBNull(reader.GetOrdinal("ParentUnitId")) ? null : reader.GetInt32(reader.GetOrdinal("ParentUnitId")),
                            UnitName = reader.IsDBNull(reader.GetOrdinal("UnitName")) ? "Частина" : reader.GetString(reader.GetOrdinal("UnitName")),
                            UnitLevel = reader.IsDBNull(reader.GetOrdinal("UnitLevel")) ? "Squad" : reader.GetString(reader.GetOrdinal("UnitLevel")),
                            UnitRole = reader.IsDBNull(reader.GetOrdinal("UnitRole")) ? "Field" : reader.GetString(reader.GetOrdinal("UnitRole")),
                            UnitType = reader.IsDBNull(reader.GetOrdinal("UnitType")) ? "Infantry" : reader.GetString(reader.GetOrdinal("UnitType")),
                            Race = reader.IsDBNull(reader.GetOrdinal("Race")) ? "Human" : reader.GetString(reader.GetOrdinal("Race")),
                            Soldiers = reader.GetInt32(reader.GetOrdinal("Soldiers")),
                            EquipmentTier = reader.IsDBNull(reader.GetOrdinal("EquipmentTier")) ? "Standard" : reader.GetString(reader.GetOrdinal("EquipmentTier")),
                            EquipmentQuality = Convert.ToSingle(reader["EquipmentQuality"]),
                            BattleExperience = Convert.ToSingle(reader["BattleExperience"]),
                            Fatigue = Convert.ToSingle(reader["Fatigue"]),
                            TrainingLevel = Convert.ToSingle(reader["TrainingLevel"]),
                            Morale = Convert.ToSingle(reader["Morale"]),
                            WeaponReadiness = Convert.ToSingle(reader["WeaponReadiness"]),
                            SupplyLevel = Convert.ToSingle(reader["SupplyLevel"]),
                            CommanderName = reader.IsDBNull(reader.GetOrdinal("CommanderName")) ? null : reader.GetString(reader.GetOrdinal("CommanderName")),
                            CommanderSkill = Convert.ToSingle(reader["CommanderSkill"]),
                            CommanderLoyalty = Convert.ToSingle(reader["CommanderLoyalty"]),
                            CommanderCorruption = Convert.ToSingle(reader["CommanderCorruption"]),
                            CommanderExperience = Convert.ToSingle(reader["CommanderExperience"]),
                            Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? "Forming" : reader.GetString(reader.GetOrdinal("Status")),
                            IsMobilized = reader.GetBoolean(reader.GetOrdinal("IsMobilized")),
                            FormationProgress = Convert.ToSingle(reader["FormationProgress"]),
                            FormationBaseDays = reader.GetInt32(reader.GetOrdinal("FormationBaseDays")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                        });
                    }
                }
                finally
                {
                    reader.Dispose();
                }
            }
            catch
            {
                return new List<MilitaryUnitRecord>();
            }

            return list;
        }

        public static int AddUnit(MilitaryUnitRecord unit)
        {
            const string q = @"
INSERT INTO MilitaryUnits (
    CountryId, ParentUnitId, UnitName, UnitLevel, UnitRole, UnitType, Race, Soldiers,
    EquipmentTier, EquipmentQuality, BattleExperience, Fatigue,
    TrainingLevel, Morale, WeaponReadiness, SupplyLevel,
    CommanderName, CommanderSkill, CommanderLoyalty, CommanderCorruption, CommanderExperience,
    [Status], IsMobilized, FormationProgress, FormationBaseDays, CreatedAt, UpdatedAt
)
VALUES (
    @CountryId, @ParentUnitId, @UnitName, @UnitLevel, @UnitRole, @UnitType, @Race, @Soldiers,
    @EquipmentTier, @EquipmentQuality, @BattleExperience, @Fatigue,
    @TrainingLevel, @Morale, @WeaponReadiness, @SupplyLevel,
    @CommanderName, @CommanderSkill, @CommanderLoyalty, @CommanderCorruption, @CommanderExperience,
    @Status, @IsMobilized, @FormationProgress, @FormationBaseDays, @CreatedAt, @UpdatedAt
);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            object? idObj;
            try
            {
                var now = DateTime.UtcNow;
                idObj = BaseRepository.ExecuteScalar(q,
                    BaseRepository.Param("@CountryId", SqlDbType.Int, unit.CountryId),
                    BaseRepository.Param("@ParentUnitId", SqlDbType.Int, (object?)unit.ParentUnitId ?? DBNull.Value),
                    BaseRepository.Param("@UnitName", SqlDbType.NVarChar, unit.UnitName, 100),
                    BaseRepository.Param("@UnitLevel", SqlDbType.NVarChar, unit.UnitLevel, 20),
                    BaseRepository.Param("@UnitRole", SqlDbType.NVarChar, unit.UnitRole, 20),
                    BaseRepository.Param("@UnitType", SqlDbType.NVarChar, unit.UnitType, 20),
                    BaseRepository.Param("@Race", SqlDbType.NVarChar, unit.Race, 40),
                    BaseRepository.Param("@Soldiers", SqlDbType.Int, unit.Soldiers),
                    BaseRepository.Param("@EquipmentTier", SqlDbType.NVarChar, unit.EquipmentTier, 20),
                    BaseRepository.Param("@EquipmentQuality", SqlDbType.Real, unit.EquipmentQuality),
                    BaseRepository.Param("@BattleExperience", SqlDbType.Real, unit.BattleExperience),
                    BaseRepository.Param("@Fatigue", SqlDbType.Real, unit.Fatigue),
                    BaseRepository.Param("@TrainingLevel", SqlDbType.Real, unit.TrainingLevel),
                    BaseRepository.Param("@Morale", SqlDbType.Real, unit.Morale),
                    BaseRepository.Param("@WeaponReadiness", SqlDbType.Real, unit.WeaponReadiness),
                    BaseRepository.Param("@SupplyLevel", SqlDbType.Real, unit.SupplyLevel),
                    BaseRepository.Param("@CommanderName", SqlDbType.NVarChar, (object?)unit.CommanderName ?? DBNull.Value, 100),
                    BaseRepository.Param("@CommanderSkill", SqlDbType.Real, unit.CommanderSkill),
                    BaseRepository.Param("@CommanderLoyalty", SqlDbType.Real, unit.CommanderLoyalty),
                    BaseRepository.Param("@CommanderCorruption", SqlDbType.Real, unit.CommanderCorruption),
                    BaseRepository.Param("@CommanderExperience", SqlDbType.Real, unit.CommanderExperience),
                    BaseRepository.Param("@Status", SqlDbType.NVarChar, unit.Status, 20),
                    BaseRepository.Param("@IsMobilized", SqlDbType.Bit, unit.IsMobilized),
                    BaseRepository.Param("@FormationProgress", SqlDbType.Real, unit.FormationProgress),
                    BaseRepository.Param("@FormationBaseDays", SqlDbType.Int, unit.FormationBaseDays),
                    BaseRepository.Param("@CreatedAt", SqlDbType.DateTime2, unit.CreatedAt == default ? now : unit.CreatedAt),
                    BaseRepository.Param("@UpdatedAt", SqlDbType.DateTime2, now)
                );
            }
            catch
            {
                return 0;
            }

            return idObj == null || idObj == DBNull.Value ? 0 : Convert.ToInt32(idObj);
        }

        public static bool UpdateUnit(MilitaryUnitRecord unit)
        {
            const string q = @"
UPDATE MilitaryUnits
SET ParentUnitId = @ParentUnitId,
    UnitName = @UnitName,
    UnitLevel = @UnitLevel,
    UnitRole = @UnitRole,
    UnitType = @UnitType,
    Race = @Race,
    Soldiers = @Soldiers,
    EquipmentTier = @EquipmentTier,
    EquipmentQuality = @EquipmentQuality,
    BattleExperience = @BattleExperience,
    Fatigue = @Fatigue,
    TrainingLevel = @TrainingLevel,
    Morale = @Morale,
    WeaponReadiness = @WeaponReadiness,
    SupplyLevel = @SupplyLevel,
    CommanderName = @CommanderName,
    CommanderSkill = @CommanderSkill,
    CommanderLoyalty = @CommanderLoyalty,
    CommanderCorruption = @CommanderCorruption,
    CommanderExperience = @CommanderExperience,
    [Status] = @Status,
    IsMobilized = @IsMobilized,
    FormationProgress = @FormationProgress,
    FormationBaseDays = @FormationBaseDays,
    UpdatedAt = @UpdatedAt
WHERE UnitId = @UnitId AND CountryId = @CountryId;";

            int rows;
            try
            {
                rows = BaseRepository.ExecuteNonQuery(q,
                    BaseRepository.Param("@UnitId", SqlDbType.Int, unit.UnitId),
                    BaseRepository.Param("@CountryId", SqlDbType.Int, unit.CountryId),
                    BaseRepository.Param("@ParentUnitId", SqlDbType.Int, (object?)unit.ParentUnitId ?? DBNull.Value),
                    BaseRepository.Param("@UnitName", SqlDbType.NVarChar, unit.UnitName, 100),
                    BaseRepository.Param("@UnitLevel", SqlDbType.NVarChar, unit.UnitLevel, 20),
                    BaseRepository.Param("@UnitRole", SqlDbType.NVarChar, unit.UnitRole, 20),
                    BaseRepository.Param("@UnitType", SqlDbType.NVarChar, unit.UnitType, 20),
                    BaseRepository.Param("@Race", SqlDbType.NVarChar, unit.Race, 40),
                    BaseRepository.Param("@Soldiers", SqlDbType.Int, unit.Soldiers),
                    BaseRepository.Param("@EquipmentTier", SqlDbType.NVarChar, unit.EquipmentTier, 20),
                    BaseRepository.Param("@EquipmentQuality", SqlDbType.Real, unit.EquipmentQuality),
                    BaseRepository.Param("@BattleExperience", SqlDbType.Real, unit.BattleExperience),
                    BaseRepository.Param("@Fatigue", SqlDbType.Real, unit.Fatigue),
                    BaseRepository.Param("@TrainingLevel", SqlDbType.Real, unit.TrainingLevel),
                    BaseRepository.Param("@Morale", SqlDbType.Real, unit.Morale),
                    BaseRepository.Param("@WeaponReadiness", SqlDbType.Real, unit.WeaponReadiness),
                    BaseRepository.Param("@SupplyLevel", SqlDbType.Real, unit.SupplyLevel),
                    BaseRepository.Param("@CommanderName", SqlDbType.NVarChar, (object?)unit.CommanderName ?? DBNull.Value, 100),
                    BaseRepository.Param("@CommanderSkill", SqlDbType.Real, unit.CommanderSkill),
                    BaseRepository.Param("@CommanderLoyalty", SqlDbType.Real, unit.CommanderLoyalty),
                    BaseRepository.Param("@CommanderCorruption", SqlDbType.Real, unit.CommanderCorruption),
                    BaseRepository.Param("@CommanderExperience", SqlDbType.Real, unit.CommanderExperience),
                    BaseRepository.Param("@Status", SqlDbType.NVarChar, unit.Status, 20),
                    BaseRepository.Param("@IsMobilized", SqlDbType.Bit, unit.IsMobilized),
                    BaseRepository.Param("@FormationProgress", SqlDbType.Real, unit.FormationProgress),
                    BaseRepository.Param("@FormationBaseDays", SqlDbType.Int, unit.FormationBaseDays),
                    BaseRepository.Param("@UpdatedAt", SqlDbType.DateTime2, DateTime.UtcNow)
                );
            }
            catch
            {
                return false;
            }

            return rows > 0;
        }

        public static bool DeleteUnit(int unitId, int countryId)
        {
            const string q = "DELETE FROM MilitaryUnits WHERE UnitId = @UnitId AND CountryId = @CountryId;";
            try
            {
                int rows = BaseRepository.ExecuteNonQuery(q,
                    BaseRepository.Param("@UnitId", SqlDbType.Int, unitId),
                    BaseRepository.Param("@CountryId", SqlDbType.Int, countryId));
                return rows > 0;
            }
            catch
            {
                return false;
            }
        }

        public static bool UpdateUnitField(int unitId, int countryId, string fieldName, object value, SqlDbType type)
        {
            string column = fieldName switch
            {
                "ParentUnitId" => "ParentUnitId",
                "UnitName" => "UnitName",
                "UnitLevel" => "UnitLevel",
                "UnitRole" => "UnitRole",
                "UnitType" => "UnitType",
                "Race" => "Race",
                "Soldiers" => "Soldiers",
                "EquipmentTier" => "EquipmentTier",
                "EquipmentQuality" => "EquipmentQuality",
                "BattleExperience" => "BattleExperience",
                "Fatigue" => "Fatigue",
                "TrainingLevel" => "TrainingLevel",
                "Morale" => "Morale",
                "WeaponReadiness" => "WeaponReadiness",
                "SupplyLevel" => "SupplyLevel",
                "CommanderName" => "CommanderName",
                "CommanderSkill" => "CommanderSkill",
                "CommanderLoyalty" => "CommanderLoyalty",
                "CommanderCorruption" => "CommanderCorruption",
                "CommanderExperience" => "CommanderExperience",
                "Status" => "[Status]",
                "IsMobilized" => "IsMobilized",
                "FormationProgress" => "FormationProgress",
                "FormationBaseDays" => "FormationBaseDays",
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(column))
                return false;

            string q = $"UPDATE MilitaryUnits SET {column} = @Value, UpdatedAt = @UpdatedAt WHERE UnitId = @UnitId AND CountryId = @CountryId;";
            try
            {
                int rows = BaseRepository.ExecuteNonQuery(
                    q,
                    BaseRepository.Param("@Value", type, value ?? DBNull.Value),
                    BaseRepository.Param("@UpdatedAt", SqlDbType.DateTime2, DateTime.UtcNow),
                    BaseRepository.Param("@UnitId", SqlDbType.Int, unitId),
                    BaseRepository.Param("@CountryId", SqlDbType.Int, countryId)
                );
                return rows > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
