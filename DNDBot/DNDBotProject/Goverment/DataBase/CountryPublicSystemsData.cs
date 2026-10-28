using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DNDBot.DataBase
{
    public static class CountryPublicSystemsData
    {
        public sealed class CountryPublicSystemsRecord
        {
            public int CountryId { get; set; }
            public decimal BudgetHealth { get; set; }
            public decimal BudgetEducation { get; set; }
            public decimal BudgetIntl { get; set; }
            public string BudgetIntlMode { get; set; } = "Auto";
            public decimal BudgetDebt { get; set; }
            public float HealthPublic { get; set; } = 50f;
            public float HealthSanitation { get; set; } = 50f;
            public float HealthCapacity { get; set; } = 50f;
            public float HealthTrust { get; set; } = 50f;
            public float EduLiteracy { get; set; } = 50f;
            public float EduQuality { get; set; } = 50f;
            public int EduSpecialists { get; set; } = 50;
            public float EduCohesion { get; set; } = 50f;
            public float IntlPrestige { get; set; } = 50f;
            public decimal LastHealthBudget { get; set; }
            public decimal LastEducationBudget { get; set; }
            public bool QuarantineEnabled { get; set; }
            public int MedicMobilizationTicks { get; set; }
            public int VaccinationProtectionTicks { get; set; }
            public int HealthDoctors { get; set; } = 80;
            public float HealthDoctorFatigue { get; set; } = 20f;
            public float HealthMedicineStock { get; set; } = 35f;
            public float HealthDrugResearchProgress { get; set; }
            public int HealthDrugLevel { get; set; }
            public int HealthDrugProjects { get; set; }
            public int HealthClinicalTrialTicks { get; set; }
            public decimal PendingEmergencyHealthSpend { get; set; }
            public decimal PendingEmergencyEducationSpend { get; set; }
            public int IntlPrestigeTicker { get; set; }
            public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        }

        static CountryPublicSystemsData()
        {
            BaseRepository.DBInitialization();
        }

        private static SqlParameter DecimalParam(string name, decimal value, byte precision = 18, byte scale = 2)
        {
            return new SqlParameter(name, SqlDbType.Decimal)
            {
                Precision = precision,
                Scale = scale,
                Value = value
            };
        }

        private static string NormalizeMode(string? mode)
        {
            return string.Equals(mode, "manual", StringComparison.OrdinalIgnoreCase)
                ? "Manual"
                : "Auto";
        }

        public static CountryPublicSystemsRecord GetByCountry(int countryId)
        {
            const string q = @"
SELECT CountryId, BudgetHealth, BudgetEducation, BudgetIntl, BudgetIntlMode, BudgetDebt,
       HealthPublic, HealthSanitation, HealthCapacity, HealthTrust,
       EduLiteracy, EduQuality, EduSpecialists, EduCohesion, IntlPrestige,
       LastHealthBudget, LastEducationBudget,
       QuarantineEnabled, MedicMobilizationTicks, VaccinationProtectionTicks,
       HealthDoctors, HealthDoctorFatigue, HealthMedicineStock, HealthDrugResearchProgress,
       HealthDrugLevel, HealthDrugProjects, HealthClinicalTrialTicks,
       PendingEmergencyHealthSpend, PendingEmergencyEducationSpend, IntlPrestigeTicker, UpdatedAt
FROM CountryPublicSystems
WHERE CountryId = @CountryId";

            using var reader = BaseRepository.ExecuteReader(q, BaseRepository.Param("@CountryId", SqlDbType.Int, countryId));
            try
            {
                if (reader.Read())
                    return Map(reader);
            }
            finally
            {
                reader.Dispose();
            }

            var created = new CountryPublicSystemsRecord
            {
                CountryId = countryId
            };
            Upsert(created);
            return created;
        }

        public static void Upsert(CountryPublicSystemsRecord record)
        {
            if (record == null || record.CountryId <= 0)
                return;

            const string q = @"
IF EXISTS (SELECT 1 FROM CountryPublicSystems WHERE CountryId = @CountryId)
BEGIN
    UPDATE CountryPublicSystems
    SET BudgetHealth = @BudgetHealth,
        BudgetEducation = @BudgetEducation,
        BudgetIntl = @BudgetIntl,
        BudgetIntlMode = @BudgetIntlMode,
        BudgetDebt = @BudgetDebt,
        HealthPublic = @HealthPublic,
        HealthSanitation = @HealthSanitation,
        HealthCapacity = @HealthCapacity,
        HealthTrust = @HealthTrust,
        EduLiteracy = @EduLiteracy,
        EduQuality = @EduQuality,
        EduSpecialists = @EduSpecialists,
        EduCohesion = @EduCohesion,
        IntlPrestige = @IntlPrestige,
        LastHealthBudget = @LastHealthBudget,
        LastEducationBudget = @LastEducationBudget,
        QuarantineEnabled = @QuarantineEnabled,
        MedicMobilizationTicks = @MedicMobilizationTicks,
        VaccinationProtectionTicks = @VaccinationProtectionTicks,
        HealthDoctors = @HealthDoctors,
        HealthDoctorFatigue = @HealthDoctorFatigue,
        HealthMedicineStock = @HealthMedicineStock,
        HealthDrugResearchProgress = @HealthDrugResearchProgress,
        HealthDrugLevel = @HealthDrugLevel,
        HealthDrugProjects = @HealthDrugProjects,
        HealthClinicalTrialTicks = @HealthClinicalTrialTicks,
        PendingEmergencyHealthSpend = @PendingEmergencyHealthSpend,
        PendingEmergencyEducationSpend = @PendingEmergencyEducationSpend,
        IntlPrestigeTicker = @IntlPrestigeTicker,
        UpdatedAt = @UpdatedAt
    WHERE CountryId = @CountryId;
END
ELSE
BEGIN
    INSERT INTO CountryPublicSystems (
        CountryId, BudgetHealth, BudgetEducation, BudgetIntl, BudgetIntlMode, BudgetDebt,
        HealthPublic, HealthSanitation, HealthCapacity, HealthTrust,
        EduLiteracy, EduQuality, EduSpecialists, EduCohesion, IntlPrestige,
        LastHealthBudget, LastEducationBudget, QuarantineEnabled, MedicMobilizationTicks,
        VaccinationProtectionTicks, HealthDoctors, HealthDoctorFatigue, HealthMedicineStock,
        HealthDrugResearchProgress, HealthDrugLevel, HealthDrugProjects, HealthClinicalTrialTicks,
        PendingEmergencyHealthSpend, PendingEmergencyEducationSpend, IntlPrestigeTicker, UpdatedAt
    )
    VALUES (
        @CountryId, @BudgetHealth, @BudgetEducation, @BudgetIntl, @BudgetIntlMode, @BudgetDebt,
        @HealthPublic, @HealthSanitation, @HealthCapacity, @HealthTrust,
        @EduLiteracy, @EduQuality, @EduSpecialists, @EduCohesion, @IntlPrestige,
        @LastHealthBudget, @LastEducationBudget, @QuarantineEnabled, @MedicMobilizationTicks,
        @VaccinationProtectionTicks, @HealthDoctors, @HealthDoctorFatigue, @HealthMedicineStock,
        @HealthDrugResearchProgress, @HealthDrugLevel, @HealthDrugProjects, @HealthClinicalTrialTicks,
        @PendingEmergencyHealthSpend, @PendingEmergencyEducationSpend, @IntlPrestigeTicker, @UpdatedAt
    );
END";

            record.BudgetIntlMode = NormalizeMode(record.BudgetIntlMode);
            record.UpdatedAt = DateTime.UtcNow;

            BaseRepository.ExecuteNonQuery(
                q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, record.CountryId),
                DecimalParam("@BudgetHealth", record.BudgetHealth),
                DecimalParam("@BudgetEducation", record.BudgetEducation),
                DecimalParam("@BudgetIntl", record.BudgetIntl),
                BaseRepository.Param("@BudgetIntlMode", SqlDbType.NVarChar, record.BudgetIntlMode, 10),
                DecimalParam("@BudgetDebt", record.BudgetDebt),
                BaseRepository.Param("@HealthPublic", SqlDbType.Real, record.HealthPublic),
                BaseRepository.Param("@HealthSanitation", SqlDbType.Real, record.HealthSanitation),
                BaseRepository.Param("@HealthCapacity", SqlDbType.Real, record.HealthCapacity),
                BaseRepository.Param("@HealthTrust", SqlDbType.Real, record.HealthTrust),
                BaseRepository.Param("@EduLiteracy", SqlDbType.Real, record.EduLiteracy),
                BaseRepository.Param("@EduQuality", SqlDbType.Real, record.EduQuality),
                BaseRepository.Param("@EduSpecialists", SqlDbType.Int, record.EduSpecialists),
                BaseRepository.Param("@EduCohesion", SqlDbType.Real, record.EduCohesion),
                BaseRepository.Param("@IntlPrestige", SqlDbType.Real, record.IntlPrestige),
                DecimalParam("@LastHealthBudget", record.LastHealthBudget),
                DecimalParam("@LastEducationBudget", record.LastEducationBudget),
                BaseRepository.Param("@QuarantineEnabled", SqlDbType.Bit, record.QuarantineEnabled),
                BaseRepository.Param("@MedicMobilizationTicks", SqlDbType.Int, record.MedicMobilizationTicks),
                BaseRepository.Param("@VaccinationProtectionTicks", SqlDbType.Int, record.VaccinationProtectionTicks),
                BaseRepository.Param("@HealthDoctors", SqlDbType.Int, record.HealthDoctors),
                BaseRepository.Param("@HealthDoctorFatigue", SqlDbType.Real, record.HealthDoctorFatigue),
                BaseRepository.Param("@HealthMedicineStock", SqlDbType.Real, record.HealthMedicineStock),
                BaseRepository.Param("@HealthDrugResearchProgress", SqlDbType.Real, record.HealthDrugResearchProgress),
                BaseRepository.Param("@HealthDrugLevel", SqlDbType.Int, record.HealthDrugLevel),
                BaseRepository.Param("@HealthDrugProjects", SqlDbType.Int, record.HealthDrugProjects),
                BaseRepository.Param("@HealthClinicalTrialTicks", SqlDbType.Int, record.HealthClinicalTrialTicks),
                DecimalParam("@PendingEmergencyHealthSpend", record.PendingEmergencyHealthSpend),
                DecimalParam("@PendingEmergencyEducationSpend", record.PendingEmergencyEducationSpend),
                BaseRepository.Param("@IntlPrestigeTicker", SqlDbType.Int, record.IntlPrestigeTicker),
                BaseRepository.Param("@UpdatedAt", SqlDbType.DateTime2, record.UpdatedAt)
            );
        }

        private static CountryPublicSystemsRecord Map(IDataRecord reader)
        {
            return new CountryPublicSystemsRecord
            {
                CountryId = reader.GetInt32(reader.GetOrdinal("CountryId")),
                BudgetHealth = reader.GetDecimal(reader.GetOrdinal("BudgetHealth")),
                BudgetEducation = reader.GetDecimal(reader.GetOrdinal("BudgetEducation")),
                BudgetIntl = reader.GetDecimal(reader.GetOrdinal("BudgetIntl")),
                BudgetIntlMode = reader.IsDBNull(reader.GetOrdinal("BudgetIntlMode"))
                    ? "Auto"
                    : NormalizeMode(reader.GetString(reader.GetOrdinal("BudgetIntlMode"))),
                BudgetDebt = reader.GetDecimal(reader.GetOrdinal("BudgetDebt")),
                HealthPublic = Convert.ToSingle(reader["HealthPublic"]),
                HealthSanitation = Convert.ToSingle(reader["HealthSanitation"]),
                HealthCapacity = Convert.ToSingle(reader["HealthCapacity"]),
                HealthTrust = Convert.ToSingle(reader["HealthTrust"]),
                EduLiteracy = Convert.ToSingle(reader["EduLiteracy"]),
                EduQuality = Convert.ToSingle(reader["EduQuality"]),
                EduSpecialists = reader.GetInt32(reader.GetOrdinal("EduSpecialists")),
                EduCohesion = Convert.ToSingle(reader["EduCohesion"]),
                IntlPrestige = Convert.ToSingle(reader["IntlPrestige"]),
                LastHealthBudget = reader.GetDecimal(reader.GetOrdinal("LastHealthBudget")),
                LastEducationBudget = reader.GetDecimal(reader.GetOrdinal("LastEducationBudget")),
                QuarantineEnabled = reader.GetBoolean(reader.GetOrdinal("QuarantineEnabled")),
                MedicMobilizationTicks = reader.GetInt32(reader.GetOrdinal("MedicMobilizationTicks")),
                VaccinationProtectionTicks = reader.GetInt32(reader.GetOrdinal("VaccinationProtectionTicks")),
                HealthDoctors = reader.GetInt32(reader.GetOrdinal("HealthDoctors")),
                HealthDoctorFatigue = Convert.ToSingle(reader["HealthDoctorFatigue"]),
                HealthMedicineStock = Convert.ToSingle(reader["HealthMedicineStock"]),
                HealthDrugResearchProgress = Convert.ToSingle(reader["HealthDrugResearchProgress"]),
                HealthDrugLevel = reader.GetInt32(reader.GetOrdinal("HealthDrugLevel")),
                HealthDrugProjects = reader.GetInt32(reader.GetOrdinal("HealthDrugProjects")),
                HealthClinicalTrialTicks = reader.GetInt32(reader.GetOrdinal("HealthClinicalTrialTicks")),
                PendingEmergencyHealthSpend = reader.GetDecimal(reader.GetOrdinal("PendingEmergencyHealthSpend")),
                PendingEmergencyEducationSpend = reader.GetDecimal(reader.GetOrdinal("PendingEmergencyEducationSpend")),
                IntlPrestigeTicker = reader.GetInt32(reader.GetOrdinal("IntlPrestigeTicker")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
            };
        }
    }
}
