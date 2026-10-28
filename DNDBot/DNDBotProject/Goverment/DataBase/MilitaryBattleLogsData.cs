#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DNDBot.DataBase
{
    public sealed class MilitaryBattleLogRecord
    {
        public int BattleLogId { get; set; }
        public int AttackerCountryId { get; set; }
        public int DefenderCountryId { get; set; }
        public string Terrain { get; set; } = "Plains";
        public string AttackerTactic { get; set; } = "Balanced";
        public string DefenderTactic { get; set; } = "Balanced";
        public string ResultType { get; set; } = "Battle";
        public int? WinnerCountryId { get; set; }
        public float AttackerPower { get; set; }
        public float DefenderPower { get; set; }
        public int AttackerTroopsStart { get; set; }
        public int DefenderTroopsStart { get; set; }
        public int AttackerLosses { get; set; }
        public int DefenderLosses { get; set; }
        public int AttackerVassalLosses { get; set; }
        public int DefenderVassalLosses { get; set; }
        public float AttackerMoraleDelta { get; set; }
        public float DefenderMoraleDelta { get; set; }
        public float AttackerExperienceGain { get; set; }
        public float DefenderExperienceGain { get; set; }
        public string Summary { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public static class MilitaryBattleLogsData
    {
        static MilitaryBattleLogsData()
        {
            BaseRepository.DBInitialization();
        }

        public static int Add(MilitaryBattleLogRecord record)
        {
            const string q = @"
INSERT INTO MilitaryBattleLogs (
    AttackerCountryId, DefenderCountryId, Terrain, AttackerTactic, DefenderTactic, ResultType, WinnerCountryId,
    AttackerPower, DefenderPower, AttackerTroopsStart, DefenderTroopsStart, AttackerLosses, DefenderLosses, AttackerVassalLosses, DefenderVassalLosses,
    AttackerMoraleDelta, DefenderMoraleDelta, AttackerExperienceGain, DefenderExperienceGain, Summary, CreatedAt
)
VALUES (
    @AttackerCountryId, @DefenderCountryId, @Terrain, @AttackerTactic, @DefenderTactic, @ResultType, @WinnerCountryId,
    @AttackerPower, @DefenderPower, @AttackerTroopsStart, @DefenderTroopsStart, @AttackerLosses, @DefenderLosses, @AttackerVassalLosses, @DefenderVassalLosses,
    @AttackerMoraleDelta, @DefenderMoraleDelta, @AttackerExperienceGain, @DefenderExperienceGain, @Summary, @CreatedAt
);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            try
            {
                object? idObj = BaseRepository.ExecuteScalar(
                    q,
                    BaseRepository.Param("@AttackerCountryId", SqlDbType.Int, record.AttackerCountryId),
                    BaseRepository.Param("@DefenderCountryId", SqlDbType.Int, record.DefenderCountryId),
                    BaseRepository.Param("@Terrain", SqlDbType.NVarChar, record.Terrain ?? "Plains", 30),
                    BaseRepository.Param("@AttackerTactic", SqlDbType.NVarChar, record.AttackerTactic ?? "Balanced", 30),
                    BaseRepository.Param("@DefenderTactic", SqlDbType.NVarChar, record.DefenderTactic ?? "Balanced", 30),
                    BaseRepository.Param("@ResultType", SqlDbType.NVarChar, record.ResultType ?? "Battle", 20),
                    BaseRepository.Param("@WinnerCountryId", SqlDbType.Int, (object?)record.WinnerCountryId ?? DBNull.Value),
                    BaseRepository.Param("@AttackerPower", SqlDbType.Real, record.AttackerPower),
                    BaseRepository.Param("@DefenderPower", SqlDbType.Real, record.DefenderPower),
                    BaseRepository.Param("@AttackerTroopsStart", SqlDbType.Int, record.AttackerTroopsStart),
                    BaseRepository.Param("@DefenderTroopsStart", SqlDbType.Int, record.DefenderTroopsStart),
                    BaseRepository.Param("@AttackerLosses", SqlDbType.Int, record.AttackerLosses),
                    BaseRepository.Param("@DefenderLosses", SqlDbType.Int, record.DefenderLosses),
                    BaseRepository.Param("@AttackerVassalLosses", SqlDbType.Int, record.AttackerVassalLosses),
                    BaseRepository.Param("@DefenderVassalLosses", SqlDbType.Int, record.DefenderVassalLosses),
                    BaseRepository.Param("@AttackerMoraleDelta", SqlDbType.Real, record.AttackerMoraleDelta),
                    BaseRepository.Param("@DefenderMoraleDelta", SqlDbType.Real, record.DefenderMoraleDelta),
                    BaseRepository.Param("@AttackerExperienceGain", SqlDbType.Real, record.AttackerExperienceGain),
                    BaseRepository.Param("@DefenderExperienceGain", SqlDbType.Real, record.DefenderExperienceGain),
                    BaseRepository.Param("@Summary", SqlDbType.NVarChar, record.Summary ?? string.Empty, 600),
                    BaseRepository.Param("@CreatedAt", SqlDbType.DateTime2, record.CreatedAt == default ? DateTime.UtcNow : record.CreatedAt)
                );
                return idObj == null || idObj == DBNull.Value ? 0 : Convert.ToInt32(idObj);
            }
            catch
            {
                return 0;
            }
        }

        public static List<MilitaryBattleLogRecord> GetByCountry(int countryId, int take = 50)
        {
            const string q = @"
SELECT TOP (@Take)
    BattleLogId, AttackerCountryId, DefenderCountryId, Terrain, AttackerTactic, DefenderTactic, ResultType, WinnerCountryId,
    AttackerPower, DefenderPower, AttackerTroopsStart, DefenderTroopsStart, AttackerLosses, DefenderLosses, AttackerVassalLosses, DefenderVassalLosses,
    AttackerMoraleDelta, DefenderMoraleDelta, AttackerExperienceGain, DefenderExperienceGain, Summary, CreatedAt
FROM MilitaryBattleLogs
WHERE AttackerCountryId = @CountryId OR DefenderCountryId = @CountryId
ORDER BY BattleLogId DESC;";

            var list = new List<MilitaryBattleLogRecord>();
            int safeTake = Math.Max(1, Math.Min(200, take));
            try
            {
                using var reader = BaseRepository.ExecuteReader(
                    q,
                    BaseRepository.Param("@Take", SqlDbType.Int, safeTake),
                    BaseRepository.Param("@CountryId", SqlDbType.Int, countryId));
                try
                {
                    while (reader.Read())
                        list.Add(Map(reader));
                }
                finally
                {
                    reader.Dispose();
                }
            }
            catch
            {
                return new List<MilitaryBattleLogRecord>();
            }

            return list;
        }

        public static MilitaryBattleLogRecord? GetByIdForCountry(int battleLogId, int countryId)
        {
            const string q = @"
SELECT TOP 1
    BattleLogId, AttackerCountryId, DefenderCountryId, Terrain, AttackerTactic, DefenderTactic, ResultType, WinnerCountryId,
    AttackerPower, DefenderPower, AttackerTroopsStart, DefenderTroopsStart, AttackerLosses, DefenderLosses, AttackerVassalLosses, DefenderVassalLosses,
    AttackerMoraleDelta, DefenderMoraleDelta, AttackerExperienceGain, DefenderExperienceGain, Summary, CreatedAt
FROM MilitaryBattleLogs
WHERE BattleLogId = @BattleLogId
  AND (AttackerCountryId = @CountryId OR DefenderCountryId = @CountryId);";

            try
            {
                using var reader = BaseRepository.ExecuteReader(
                    q,
                    BaseRepository.Param("@BattleLogId", SqlDbType.Int, battleLogId),
                    BaseRepository.Param("@CountryId", SqlDbType.Int, countryId));
                try
                {
                    if (reader.Read())
                        return Map(reader);
                }
                finally
                {
                    reader.Dispose();
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        private static MilitaryBattleLogRecord Map(SqlDataReader reader)
        {
            return new MilitaryBattleLogRecord
            {
                BattleLogId = reader.GetInt32(reader.GetOrdinal("BattleLogId")),
                AttackerCountryId = reader.GetInt32(reader.GetOrdinal("AttackerCountryId")),
                DefenderCountryId = reader.GetInt32(reader.GetOrdinal("DefenderCountryId")),
                Terrain = reader.IsDBNull(reader.GetOrdinal("Terrain")) ? "Plains" : reader.GetString(reader.GetOrdinal("Terrain")),
                AttackerTactic = reader.IsDBNull(reader.GetOrdinal("AttackerTactic")) ? "Balanced" : reader.GetString(reader.GetOrdinal("AttackerTactic")),
                DefenderTactic = reader.IsDBNull(reader.GetOrdinal("DefenderTactic")) ? "Balanced" : reader.GetString(reader.GetOrdinal("DefenderTactic")),
                ResultType = reader.IsDBNull(reader.GetOrdinal("ResultType")) ? "Battle" : reader.GetString(reader.GetOrdinal("ResultType")),
                WinnerCountryId = reader.IsDBNull(reader.GetOrdinal("WinnerCountryId")) ? null : reader.GetInt32(reader.GetOrdinal("WinnerCountryId")),
                AttackerPower = Convert.ToSingle(reader["AttackerPower"]),
                DefenderPower = Convert.ToSingle(reader["DefenderPower"]),
                AttackerTroopsStart = reader.GetInt32(reader.GetOrdinal("AttackerTroopsStart")),
                DefenderTroopsStart = reader.GetInt32(reader.GetOrdinal("DefenderTroopsStart")),
                AttackerLosses = reader.GetInt32(reader.GetOrdinal("AttackerLosses")),
                DefenderLosses = reader.GetInt32(reader.GetOrdinal("DefenderLosses")),
                AttackerVassalLosses = reader.IsDBNull(reader.GetOrdinal("AttackerVassalLosses")) ? 0 : reader.GetInt32(reader.GetOrdinal("AttackerVassalLosses")),
                DefenderVassalLosses = reader.IsDBNull(reader.GetOrdinal("DefenderVassalLosses")) ? 0 : reader.GetInt32(reader.GetOrdinal("DefenderVassalLosses")),
                AttackerMoraleDelta = Convert.ToSingle(reader["AttackerMoraleDelta"]),
                DefenderMoraleDelta = Convert.ToSingle(reader["DefenderMoraleDelta"]),
                AttackerExperienceGain = Convert.ToSingle(reader["AttackerExperienceGain"]),
                DefenderExperienceGain = Convert.ToSingle(reader["DefenderExperienceGain"]),
                Summary = reader.IsDBNull(reader.GetOrdinal("Summary")) ? string.Empty : reader.GetString(reader.GetOrdinal("Summary")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            };
        }
    }
}
