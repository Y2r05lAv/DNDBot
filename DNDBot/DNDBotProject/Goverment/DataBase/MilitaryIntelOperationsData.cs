#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DNDBot.DataBase
{
    public sealed class MilitaryIntelOperationLogRecord
    {
        public int IntelLogId { get; set; }
        public int SourceCountryId { get; set; }
        public int TargetCountryId { get; set; }
        public string OperationType { get; set; } = "ScoutForces";
        public long BudgetGold { get; set; }
        public bool Success { get; set; }
        public bool Detected { get; set; }
        public bool VisibleToTarget { get; set; }
        public float SuccessChance { get; set; }
        public float DetectionChance { get; set; }
        public float Roll { get; set; }
        public long SourceTreasuryDelta { get; set; }
        public long TargetTreasuryDelta { get; set; }
        public float SourceIntelDelta { get; set; }
        public float SourceCounterIntelDelta { get; set; }
        public float TargetIntelDelta { get; set; }
        public float TargetCounterIntelDelta { get; set; }
        public float TargetArmyMoraleDelta { get; set; }
        public float TargetLogisticsDelta { get; set; }
        public int AffectedUnits { get; set; }
        public float ReconPowerEstimate { get; set; }
        public int ReconTroopsEstimate { get; set; }
        public string Report { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public static class MilitaryIntelOperationsData
    {
        static MilitaryIntelOperationsData()
        {
            BaseRepository.DBInitialization();
        }

        public static int Add(MilitaryIntelOperationLogRecord record)
        {
            const string q = @"
INSERT INTO MilitaryIntelOperations (
    SourceCountryId, TargetCountryId, OperationType, BudgetGold, Success, Detected, VisibleToTarget,
    SuccessChance, DetectionChance, Roll,
    SourceTreasuryDelta, TargetTreasuryDelta,
    SourceIntelDelta, SourceCounterIntelDelta, TargetIntelDelta, TargetCounterIntelDelta,
    TargetArmyMoraleDelta, TargetLogisticsDelta,
    AffectedUnits, ReconPowerEstimate, ReconTroopsEstimate,
    Report, CreatedAt
)
VALUES (
    @SourceCountryId, @TargetCountryId, @OperationType, @BudgetGold, @Success, @Detected, @VisibleToTarget,
    @SuccessChance, @DetectionChance, @Roll,
    @SourceTreasuryDelta, @TargetTreasuryDelta,
    @SourceIntelDelta, @SourceCounterIntelDelta, @TargetIntelDelta, @TargetCounterIntelDelta,
    @TargetArmyMoraleDelta, @TargetLogisticsDelta,
    @AffectedUnits, @ReconPowerEstimate, @ReconTroopsEstimate,
    @Report, @CreatedAt
);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            try
            {
                object? idObj = BaseRepository.ExecuteScalar(
                    q,
                    BaseRepository.Param("@SourceCountryId", SqlDbType.Int, record.SourceCountryId),
                    BaseRepository.Param("@TargetCountryId", SqlDbType.Int, record.TargetCountryId),
                    BaseRepository.Param("@OperationType", SqlDbType.NVarChar, record.OperationType ?? "ScoutForces", 40),
                    BaseRepository.Param("@BudgetGold", SqlDbType.BigInt, record.BudgetGold),
                    BaseRepository.Param("@Success", SqlDbType.Bit, record.Success),
                    BaseRepository.Param("@Detected", SqlDbType.Bit, record.Detected),
                    BaseRepository.Param("@VisibleToTarget", SqlDbType.Bit, record.VisibleToTarget),
                    BaseRepository.Param("@SuccessChance", SqlDbType.Real, record.SuccessChance),
                    BaseRepository.Param("@DetectionChance", SqlDbType.Real, record.DetectionChance),
                    BaseRepository.Param("@Roll", SqlDbType.Real, record.Roll),
                    BaseRepository.Param("@SourceTreasuryDelta", SqlDbType.BigInt, record.SourceTreasuryDelta),
                    BaseRepository.Param("@TargetTreasuryDelta", SqlDbType.BigInt, record.TargetTreasuryDelta),
                    BaseRepository.Param("@SourceIntelDelta", SqlDbType.Real, record.SourceIntelDelta),
                    BaseRepository.Param("@SourceCounterIntelDelta", SqlDbType.Real, record.SourceCounterIntelDelta),
                    BaseRepository.Param("@TargetIntelDelta", SqlDbType.Real, record.TargetIntelDelta),
                    BaseRepository.Param("@TargetCounterIntelDelta", SqlDbType.Real, record.TargetCounterIntelDelta),
                    BaseRepository.Param("@TargetArmyMoraleDelta", SqlDbType.Real, record.TargetArmyMoraleDelta),
                    BaseRepository.Param("@TargetLogisticsDelta", SqlDbType.Real, record.TargetLogisticsDelta),
                    BaseRepository.Param("@AffectedUnits", SqlDbType.Int, record.AffectedUnits),
                    BaseRepository.Param("@ReconPowerEstimate", SqlDbType.Real, record.ReconPowerEstimate),
                    BaseRepository.Param("@ReconTroopsEstimate", SqlDbType.Int, record.ReconTroopsEstimate),
                    BaseRepository.Param("@Report", SqlDbType.NVarChar, record.Report ?? string.Empty, 1200),
                    BaseRepository.Param("@CreatedAt", SqlDbType.DateTime2, record.CreatedAt == default ? DateTime.UtcNow : record.CreatedAt)
                );
                return idObj == null || idObj == DBNull.Value ? 0 : Convert.ToInt32(idObj);
            }
            catch
            {
                return 0;
            }
        }

        public static List<MilitaryIntelOperationLogRecord> GetByCountry(int countryId, int take = 50)
        {
            const string q = @"
SELECT TOP (@Take)
    IntelLogId, SourceCountryId, TargetCountryId, OperationType, BudgetGold, Success, Detected, VisibleToTarget,
    SuccessChance, DetectionChance, Roll,
    SourceTreasuryDelta, TargetTreasuryDelta,
    SourceIntelDelta, SourceCounterIntelDelta, TargetIntelDelta, TargetCounterIntelDelta,
    TargetArmyMoraleDelta, TargetLogisticsDelta,
    AffectedUnits, ReconPowerEstimate, ReconTroopsEstimate,
    Report, CreatedAt
FROM MilitaryIntelOperations
WHERE SourceCountryId = @CountryId
   OR (TargetCountryId = @CountryId AND VisibleToTarget = 1)
ORDER BY IntelLogId DESC;";

            var list = new List<MilitaryIntelOperationLogRecord>();
            int safeTake = Math.Max(1, Math.Min(300, take));
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
                return new List<MilitaryIntelOperationLogRecord>();
            }

            return list;
        }

        public static MilitaryIntelOperationLogRecord? GetByIdForCountry(int intelLogId, int countryId)
        {
            const string q = @"
SELECT TOP 1
    IntelLogId, SourceCountryId, TargetCountryId, OperationType, BudgetGold, Success, Detected, VisibleToTarget,
    SuccessChance, DetectionChance, Roll,
    SourceTreasuryDelta, TargetTreasuryDelta,
    SourceIntelDelta, SourceCounterIntelDelta, TargetIntelDelta, TargetCounterIntelDelta,
    TargetArmyMoraleDelta, TargetLogisticsDelta,
    AffectedUnits, ReconPowerEstimate, ReconTroopsEstimate,
    Report, CreatedAt
FROM MilitaryIntelOperations
WHERE IntelLogId = @IntelLogId
  AND (
        SourceCountryId = @CountryId
        OR (TargetCountryId = @CountryId AND VisibleToTarget = 1)
      );";

            try
            {
                using var reader = BaseRepository.ExecuteReader(
                    q,
                    BaseRepository.Param("@IntelLogId", SqlDbType.Int, intelLogId),
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

        private static MilitaryIntelOperationLogRecord Map(SqlDataReader reader)
        {
            return new MilitaryIntelOperationLogRecord
            {
                IntelLogId = reader.GetInt32(reader.GetOrdinal("IntelLogId")),
                SourceCountryId = reader.GetInt32(reader.GetOrdinal("SourceCountryId")),
                TargetCountryId = reader.GetInt32(reader.GetOrdinal("TargetCountryId")),
                OperationType = reader.IsDBNull(reader.GetOrdinal("OperationType")) ? "ScoutForces" : reader.GetString(reader.GetOrdinal("OperationType")),
                BudgetGold = reader.IsDBNull(reader.GetOrdinal("BudgetGold")) ? 0L : reader.GetInt64(reader.GetOrdinal("BudgetGold")),
                Success = !reader.IsDBNull(reader.GetOrdinal("Success")) && reader.GetBoolean(reader.GetOrdinal("Success")),
                Detected = !reader.IsDBNull(reader.GetOrdinal("Detected")) && reader.GetBoolean(reader.GetOrdinal("Detected")),
                VisibleToTarget = !reader.IsDBNull(reader.GetOrdinal("VisibleToTarget")) && reader.GetBoolean(reader.GetOrdinal("VisibleToTarget")),
                SuccessChance = reader.IsDBNull(reader.GetOrdinal("SuccessChance")) ? 0f : Convert.ToSingle(reader["SuccessChance"]),
                DetectionChance = reader.IsDBNull(reader.GetOrdinal("DetectionChance")) ? 0f : Convert.ToSingle(reader["DetectionChance"]),
                Roll = reader.IsDBNull(reader.GetOrdinal("Roll")) ? 0f : Convert.ToSingle(reader["Roll"]),
                SourceTreasuryDelta = reader.IsDBNull(reader.GetOrdinal("SourceTreasuryDelta")) ? 0L : reader.GetInt64(reader.GetOrdinal("SourceTreasuryDelta")),
                TargetTreasuryDelta = reader.IsDBNull(reader.GetOrdinal("TargetTreasuryDelta")) ? 0L : reader.GetInt64(reader.GetOrdinal("TargetTreasuryDelta")),
                SourceIntelDelta = reader.IsDBNull(reader.GetOrdinal("SourceIntelDelta")) ? 0f : Convert.ToSingle(reader["SourceIntelDelta"]),
                SourceCounterIntelDelta = reader.IsDBNull(reader.GetOrdinal("SourceCounterIntelDelta")) ? 0f : Convert.ToSingle(reader["SourceCounterIntelDelta"]),
                TargetIntelDelta = reader.IsDBNull(reader.GetOrdinal("TargetIntelDelta")) ? 0f : Convert.ToSingle(reader["TargetIntelDelta"]),
                TargetCounterIntelDelta = reader.IsDBNull(reader.GetOrdinal("TargetCounterIntelDelta")) ? 0f : Convert.ToSingle(reader["TargetCounterIntelDelta"]),
                TargetArmyMoraleDelta = reader.IsDBNull(reader.GetOrdinal("TargetArmyMoraleDelta")) ? 0f : Convert.ToSingle(reader["TargetArmyMoraleDelta"]),
                TargetLogisticsDelta = reader.IsDBNull(reader.GetOrdinal("TargetLogisticsDelta")) ? 0f : Convert.ToSingle(reader["TargetLogisticsDelta"]),
                AffectedUnits = reader.IsDBNull(reader.GetOrdinal("AffectedUnits")) ? 0 : reader.GetInt32(reader.GetOrdinal("AffectedUnits")),
                ReconPowerEstimate = reader.IsDBNull(reader.GetOrdinal("ReconPowerEstimate")) ? 0f : Convert.ToSingle(reader["ReconPowerEstimate"]),
                ReconTroopsEstimate = reader.IsDBNull(reader.GetOrdinal("ReconTroopsEstimate")) ? 0 : reader.GetInt32(reader.GetOrdinal("ReconTroopsEstimate")),
                Report = reader.IsDBNull(reader.GetOrdinal("Report")) ? string.Empty : reader.GetString(reader.GetOrdinal("Report")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            };
        }
    }
}
