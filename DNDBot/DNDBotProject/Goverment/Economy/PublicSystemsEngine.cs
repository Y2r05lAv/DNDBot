using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DNDBot.DataBase;

namespace DNDBot.Goverment.Economy
{
    public static class PublicSystemsEngine
    {
        private static readonly Random Rng = Random.Shared;

        public const string LawHealthMinistry = "LAW_HEALTH_MINISTRY";
        public const string LawPublicSanitation = "LAW_PUBLIC_SANITATION";
        public const string LawQuarantineProtocols = "LAW_QUARANTINE_PROTOCOLS";
        public const string LawNationalImmunization = "LAW_NATIONAL_IMMUNIZATION";
        public const string LawMedicalLicensing = "LAW_MEDICAL_LICENSING";
        public const string LawPharmaResearchAgency = "LAW_PHARMA_RESEARCH_AGENCY";
        public const string LawClinicalTrialProtocols = "LAW_CLINICAL_TRIAL_PROTOCOLS";
        public const string LawEmergencyMedicineReserve = "LAW_EMERGENCY_MEDICINE_RESERVE";
        public const string LawHospitalResidencyProgram = "LAW_HOSPITAL_RESIDENCY_PROGRAM";

        public const string LawEduMinistry = "LAW_EDU_MINISTRY";
        public const string LawBasicSchooling = "LAW_BASIC_SCHOOLING";
        public const string LawHigherEduCharter = "LAW_HIGHER_EDU_CHARTER";
        public const string LawScholarshipProgram = "LAW_SCHOLARSHIP_PROGRAM";

        public const string LawIntlCooperation = "LAW_INTL_COOPERATION";
        public const string LawIntlDataSharingHealth = "LAW_INTL_DATA_SHARING_HEALTH";
        public const string LawIntlAcademicExchange = "LAW_INTL_ACADEMIC_EXCHANGE";
        public const string LawCommunityHealthOutreach = "LAW_COMMUNITY_HEALTH_OUTREACH";
        public const string LawTeacherCertification = "LAW_TEACHER_CERTIFICATION";

        public const string ProgramHealthExchange = "HEALTH_EXCHANGE";
        public const string ProgramHealthDataSharing = "HEALTH_DATA_SHARING";
        public const string ProgramEduStudentExchange = "EDU_STUDENT_EXCHANGE";
        public const string ProgramEduJointResearch = "EDU_JOINT_RESEARCH";

        public static IReadOnlyList<string> AllProgramTypes { get; } = new[]
        {
            ProgramHealthExchange,
            ProgramHealthDataSharing,
            ProgramEduStudentExchange,
            ProgramEduJointResearch
        };

        public sealed class PublicSystemsTickResult
        {
            public decimal HealthBudgetSpent { get; set; }
            public decimal EducationBudgetSpent { get; set; }
            public decimal IntlBudgetSpent { get; set; }
            public decimal EmergencyHealthSpent { get; set; }
            public decimal EmergencyEducationSpent { get; set; }
            public decimal TotalBudgetSpent => HealthBudgetSpent + EducationBudgetSpent + IntlBudgetSpent + EmergencyHealthSpent + EmergencyEducationSpent;
            public float HealthProductionBonus { get; set; }
            public float EducationProductionBonus { get; set; }
            public float DiseaseProductionPenalty { get; set; }
            public float ProductionMultiplier { get; set; } = 1f;
            public float HealthNeed { get; set; }
            public float DiseaseHealthNeedBonus { get; set; }
            public float DiseaseTrustHit { get; set; }
            public int ActiveDiseases { get; set; }
            public int HealthDoctors { get; set; }
            public float HealthDoctorFatigue { get; set; }
            public float HealthMedicineStock { get; set; }
            public float HealthDrugResearchProgress { get; set; }
            public int HealthDrugLevel { get; set; }
            public int HealthDrugProjects { get; set; }
            public float ControlScoreBonus { get; set; }
            public float EpidemicChanceModifier { get; set; } = 1f;
            public int OperationalIntlPrograms { get; set; }
            public int BlockedIntlPrograms { get; set; }
            public string TickNotes { get; set; } = string.Empty;
        }

        private sealed class IntlProgramTickResult
        {
            public decimal BudgetSpent { get; set; }
            public float ControlScoreBonus { get; set; }
            public float EpidemicChanceModifier { get; set; } = 1f;
            public int OperationalPrograms { get; set; }
            public int BlockedPrograms { get; set; }
            public string Notes { get; set; } = string.Empty;
        }

        private sealed class DiseaseTickAggregate
        {
            public decimal ProductionPenalty { get; set; }
            public float HealthNeedBonus { get; set; }
            public float TrustPenalty { get; set; }
            public int ActiveCount { get; set; }
        }

        public static PublicSystemsTickResult ApplyTick(
            CountryRecord country,
            CountryPublicSystemsData.CountryPublicSystemsRecord systems,
            List<CountryDiseasesData.CountryDiseaseRecord> diseases,
            List<CountryIntlProgramsData.CountryIntlProgramRecord> intlPrograms,
            IReadOnlyCollection<LawRecord> acceptedLaws,
            bool allowRandomEvents)
        {
            var result = new PublicSystemsTickResult();
            if (country == null || systems == null)
                return result;

            var lawCodes = BuildLawCodeSet(acceptedLaws);
            bool hasHealthMinistry = HasLaw(lawCodes, LawHealthMinistry);
            bool hasEduMinistry = HasLaw(lawCodes, LawEduMinistry);
            bool hasQuarantineProtocols = HasLaw(lawCodes, LawQuarantineProtocols);
            bool hasNationalImmunization = HasLaw(lawCodes, LawNationalImmunization);
            bool hasMedicalLicensing = HasLaw(lawCodes, LawMedicalLicensing);
            bool hasPharmaResearchAgency = HasLaw(lawCodes, LawPharmaResearchAgency);
            bool hasClinicalTrialProtocols = HasLaw(lawCodes, LawClinicalTrialProtocols);
            bool hasEmergencyMedicineReserve = HasLaw(lawCodes, LawEmergencyMedicineReserve);
            bool hasHospitalResidency = HasLaw(lawCodes, LawHospitalResidencyProgram);

            systems.BudgetIntlMode = NormalizeIntlBudgetMode(systems.BudgetIntlMode);
            systems.MedicMobilizationTicks = Math.Max(0, systems.MedicMobilizationTicks);
            systems.VaccinationProtectionTicks = Math.Max(0, systems.VaccinationProtectionTicks);
            systems.IntlPrestigeTicker = Math.Max(0, systems.IntlPrestigeTicker);
            systems.HealthDoctors = Math.Max(0, systems.HealthDoctors);
            systems.HealthDrugLevel = Math.Max(0, systems.HealthDrugLevel);
            systems.HealthDrugProjects = Math.Max(0, systems.HealthDrugProjects);
            systems.HealthClinicalTrialTicks = Math.Max(0, systems.HealthClinicalTrialTicks);
            if (systems.HealthClinicalTrialTicks > 0)
                systems.HealthClinicalTrialTicks--;

            decimal emergencyHealthSpend = Math.Max(0m, systems.PendingEmergencyHealthSpend);
            systems.PendingEmergencyHealthSpend = 0m;
            result.EmergencyHealthSpent = emergencyHealthSpend;
            decimal emergencyEducationSpend = Math.Max(0m, systems.PendingEmergencyEducationSpend);
            systems.PendingEmergencyEducationSpend = 0m;
            result.EmergencyEducationSpent = emergencyEducationSpend;

            if (systems.MedicMobilizationTicks > 0)
                systems.MedicMobilizationTicks--;
            if (systems.VaccinationProtectionTicks > 0)
                systems.VaccinationProtectionTicks--;

            var intl = ApplyInternationalPrograms(country, systems, intlPrograms, lawCodes, allowRandomEvents, diseases);
            result.IntlBudgetSpent = intl.BudgetSpent;
            result.ControlScoreBonus = intl.ControlScoreBonus;
            result.EpidemicChanceModifier = intl.EpidemicChanceModifier;
            result.OperationalIntlPrograms = intl.OperationalPrograms;
            result.BlockedIntlPrograms = intl.BlockedPrograms;

            if (allowRandomEvents)
            {
                TrySpawnRandomDisease(
                    country,
                    systems,
                    diseases,
                    hasHealthMinistry,
                    hasNationalImmunization,
                    result.EpidemicChanceModifier);
            }

            var diseaseStats = ApplyDiseaseTick(
                country,
                systems,
                diseases,
                result.ControlScoreBonus,
                hasQuarantineProtocols,
                allowRandomEvents);

            result.ActiveDiseases = diseaseStats.ActiveCount;
            result.DiseaseProductionPenalty = (float)diseaseStats.ProductionPenalty;
            result.DiseaseHealthNeedBonus = diseaseStats.HealthNeedBonus;
            result.DiseaseTrustHit = diseaseStats.TrustPenalty;

            decimal budgetHealth = hasHealthMinistry ? Math.Max(0m, systems.BudgetHealth) : 0m;
            decimal budgetEducation = hasEduMinistry ? Math.Max(0m, systems.BudgetEducation) : 0m;
            result.HealthBudgetSpent = budgetHealth;
            result.EducationBudgetSpent = budgetEducation;

            float baseBudget = Math.Max(1f, EconomyConfigData.GetFloat("PublicBudgetBase", 1000f));
            float eh = ComputeBudgetEfficiency(budgetHealth + emergencyHealthSpend, baseBudget);
            float ee = ComputeBudgetEfficiency(budgetEducation + emergencyEducationSpend, baseBudget);
            string workforceNote = string.Empty;
            string pharmaNote = string.Empty;

            if (hasHealthMinistry)
            {
                ApplyHealthTick(country, systems, lawCodes, intlPrograms, diseaseStats, eh, budgetHealth);
                workforceNote = ApplyHealthWorkforceTick(
                    country,
                    systems,
                    diseaseStats,
                    eh,
                    hasMedicalLicensing,
                    hasHospitalResidency);
                pharmaNote = ApplyMedicineAndResearchTick(
                    systems,
                    lawCodes,
                    intlPrograms,
                    diseaseStats,
                    eh,
                    allowRandomEvents,
                    hasPharmaResearchAgency,
                    hasClinicalTrialProtocols,
                    hasEmergencyMedicineReserve);
                ApplyMedicineStockDiseaseMitigation(result, systems, diseaseStats.ActiveCount, hasEmergencyMedicineReserve);
                result.HealthNeed = ComputeHealthNeed(country, result.DiseaseHealthNeedBonus);
            }

            if (hasEduMinistry)
                ApplyEducationTick(country, systems, lawCodes, ee, budgetEducation, baseBudget);

            if (systems.QuarantineEnabled && diseaseStats.ActiveCount > 0 && hasQuarantineProtocols)
            {
                result.DiseaseProductionPenalty += 0.10f;
                if (systems.EduCohesion < 35f)
                    systems.HealthTrust = ClampStat(systems.HealthTrust - 1f);
            }

            result.HealthDoctors = systems.HealthDoctors;
            result.HealthDoctorFatigue = systems.HealthDoctorFatigue;
            result.HealthMedicineStock = systems.HealthMedicineStock;
            result.HealthDrugResearchProgress = systems.HealthDrugResearchProgress;
            result.HealthDrugLevel = systems.HealthDrugLevel;
            result.HealthDrugProjects = systems.HealthDrugProjects;
            result.HealthProductionBonus = (systems.HealthPublic - 50f) / 200f;
            result.EducationProductionBonus = (systems.EduLiteracy - 50f) / 250f;
            result.ProductionMultiplier = EconomyCalculator.Clamp(
                1f + result.HealthProductionBonus + result.EducationProductionBonus - result.DiseaseProductionPenalty,
                0.3f,
                1.8f);

            NormalizeSystemStats(systems);
            country.InternationalReputation = ClampStat((country.InternationalReputation * 0.75f) + (systems.IntlPrestige * 0.25f));

            var notes = new List<string>();
            if (!string.IsNullOrWhiteSpace(intl.Notes))
                notes.Add(intl.Notes);
            if (diseaseStats.ActiveCount > 0)
                notes.Add($"Активні хвороби: {diseaseStats.ActiveCount}");
            if (systems.QuarantineEnabled && diseaseStats.ActiveCount > 0)
                notes.Add("Карантин активний");
            if (!string.IsNullOrWhiteSpace(workforceNote))
                notes.Add(workforceNote);
            if (!string.IsNullOrWhiteSpace(pharmaNote))
                notes.Add(pharmaNote);
            result.TickNotes = string.Join(" | ", notes);

            return result;
        }

        public static decimal ApplyTreasuryShortfallPenalty(
            CountryRecord country,
            CountryPublicSystemsData.CountryPublicSystemsRecord systems,
            long projectedTreasuryAfterTurn,
            decimal publicSpend)
        {
            if (country == null || systems == null)
                return 0m;

            decimal spend = Math.Max(0m, publicSpend);
            if (spend <= 0m)
                return 0m;

            decimal shortfall = 0m;
            if (projectedTreasuryAfterTurn < 0)
                shortfall = Math.Min(spend, Math.Abs((decimal)projectedTreasuryAfterTurn));

            if (shortfall > 0m)
            {
                systems.BudgetDebt += shortfall;
                float hit = (float)Math.Min(3m, shortfall / Math.Max(1m, spend) * 3m);
                systems.HealthTrust = ClampStat(systems.HealthTrust - hit);
                country.Stability = ClampStat(country.Stability - hit);
                country.Happiness = ClampStat(country.Happiness - hit * 0.5f);
                return shortfall;
            }

            if (systems.BudgetDebt > 0m && projectedTreasuryAfterTurn > 0)
            {
                decimal paydown = Math.Min(systems.BudgetDebt, (decimal)projectedTreasuryAfterTurn * 0.1m);
                systems.BudgetDebt = Math.Max(0m, systems.BudgetDebt - paydown);
            }

            return 0m;
        }

        public static HashSet<string> BuildLawCodeSet(IReadOnlyCollection<LawRecord> acceptedLaws)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (acceptedLaws == null || acceptedLaws.Count == 0)
                return set;

            foreach (var law in acceptedLaws)
            {
                CollectLawCodes(law?.Title, set);
                CollectLawCodes(law?.EffectType, set);
                CollectLawCodes(law?.Description, set);
            }
            return set;
        }

        public static bool HasLaw(HashSet<string> lawCodes, string lawCode)
        {
            if (lawCodes == null || lawCodes.Count == 0 || string.IsNullOrWhiteSpace(lawCode))
                return false;
            return lawCodes.Contains(lawCode.Trim().ToUpperInvariant());
        }

        public static string NormalizeIntlBudgetMode(string? mode)
        {
            return string.Equals(mode, "manual", StringComparison.OrdinalIgnoreCase)
                ? "Manual"
                : "Auto";
        }

        public static string NormalizeProgramType(string? programType)
        {
            string raw = (programType ?? string.Empty).Trim().ToUpperInvariant();
            return raw switch
            {
                "HEALTH_EXCHANGE" => ProgramHealthExchange,
                "HEALTH_DATA_SHARING" => ProgramHealthDataSharing,
                "EDU_STUDENT_EXCHANGE" => ProgramEduStudentExchange,
                "EDU_JOINT_RESEARCH" => ProgramEduJointResearch,
                _ => raw
            };
        }

        public static bool IsProgramTypeSupported(string? programType)
        {
            string normalized = NormalizeProgramType(programType);
            return AllProgramTypes.Contains(normalized, StringComparer.OrdinalIgnoreCase);
        }

        public static string NormalizeLawCode(string? lawCode)
        {
            return string.IsNullOrWhiteSpace(lawCode)
                ? string.Empty
                : lawCode.Trim().ToUpperInvariant();
        }

        public static bool IsLawCode(string? lawCode)
        {
            string normalized = NormalizeLawCode(lawCode);
            return normalized.StartsWith("LAW_", StringComparison.Ordinal);
        }

        public static string GetLawDisplayNameUk(string? lawCode)
        {
            string normalized = NormalizeLawCode(lawCode);
            return normalized switch
            {
                LawHealthMinistry => "Закон про Міністерство охорони здоров'я",
                LawPublicSanitation => "Санітарний кодекс громад",
                LawQuarantineProtocols => "Протоколи карантину та ізоляції",
                LawNationalImmunization => "Національна програма імунізації",
                LawMedicalLicensing => "Ліцензування лікарів і протоколи практики",
                LawPharmaResearchAgency => "Нацагенція розробки ліків",
                LawClinicalTrialProtocols => "Клінічні протоколи випробувань",
                LawEmergencyMedicineReserve => "Стратегічний резерв ліків",
                LawHospitalResidencyProgram => "Резиденції та інтернатура лікарів",
                LawEduMinistry => "Закон про Міністерство освіти і науки",
                LawBasicSchooling => "Закон про базову шкільну освіту",
                LawHigherEduCharter => "Хартія вищої освіти",
                LawScholarshipProgram => "Державна стипендійна програма",
                LawIntlCooperation => "Закон про міжнародне співробітництво",
                LawIntlDataSharingHealth => "Обмін епідданими з партнерами",
                LawIntlAcademicExchange => "Академічні обміни та мобільність",
                LawCommunityHealthOutreach => "Програма громадського здоров'я",
                LawTeacherCertification => "Сертифікація педагогів",
                _ => string.IsNullOrWhiteSpace(normalized) ? (lawCode ?? string.Empty) : normalized
            };
        }

        public static string GetProgramDisplayNameUk(string? programType)
        {
            return NormalizeProgramType(programType) switch
            {
                ProgramHealthExchange => "Медичний обмін",
                ProgramHealthDataSharing => "Епіднагляд і обмін даними",
                ProgramEduStudentExchange => "Студентський обмін",
                ProgramEduJointResearch => "Спільні дослідження",
                _ => "Невідома програма"
            };
        }

        public static decimal GetDefaultProgramCost(string? programType)
        {
            return NormalizeProgramType(programType) switch
            {
                ProgramHealthExchange => 500m,
                ProgramHealthDataSharing => 250m,
                ProgramEduStudentExchange => 350m,
                ProgramEduJointResearch => 750m,
                _ => 0m
            };
        }

        public static int GetDefaultProgramRequiredSpec(string? programType)
        {
            return NormalizeProgramType(programType) switch
            {
                ProgramHealthExchange => 1,
                ProgramHealthDataSharing => 1,
                ProgramEduStudentExchange => 2,
                ProgramEduJointResearch => 3,
                _ => 0
            };
        }

        private static void ApplyHealthTick(
            CountryRecord country,
            CountryPublicSystemsData.CountryPublicSystemsRecord systems,
            HashSet<string> lawCodes,
            List<CountryIntlProgramsData.CountryIntlProgramRecord> intlPrograms,
            DiseaseTickAggregate diseaseStats,
            float healthBudgetEfficiency,
            decimal budgetHealth)
        {
            int infraHealthBonus = HasProgramActive(intlPrograms, ProgramHealthExchange) ? 1 : 0;
            int mobilizationBonus = systems.MedicMobilizationTicks > 0 ? 10 : 0;
            int outreachBonus = HasLaw(lawCodes, LawCommunityHealthOutreach) ? 1 : 0;
            systems.HealthCapacity = ClampStat(
                systems.HealthCapacity +
                (float)Math.Round(1f * healthBudgetEfficiency) +
                infraHealthBonus +
                mobilizationBonus +
                outreachBonus);

            int sanitationBonus = HasLaw(lawCodes, LawPublicSanitation) ? 1 : 0;
            if (HasProgramActive(intlPrograms, ProgramHealthExchange))
                sanitationBonus += 1;
            sanitationBonus += outreachBonus;

            int sanitationDecay = (country.WarState ? 1 : 0) + Math.Min(3, diseaseStats.ActiveCount);
            systems.HealthSanitation = ClampStat(
                systems.HealthSanitation +
                (float)Math.Round(1f * healthBudgetEfficiency) +
                sanitationBonus -
                sanitationDecay);

            float healthNeed = ComputeHealthNeed(country, diseaseStats.HealthNeedBonus);
            if (systems.HealthCapacity >= healthNeed)
            {
                systems.HealthPublic = ClampStat(
                    systems.HealthPublic +
                    1f +
                    (float)Math.Floor((systems.HealthCapacity - healthNeed) / 25f));
            }
            else
            {
                systems.HealthPublic = ClampStat(
                    systems.HealthPublic -
                    1f -
                    (float)Math.Floor((healthNeed - systems.HealthCapacity) / 15f));
            }

            if (budgetHealth >= systems.LastHealthBudget * 0.95m)
                systems.HealthTrust = ClampStat(systems.HealthTrust + 1f);
            if (systems.LastHealthBudget > 0m && budgetHealth <= systems.LastHealthBudget * 0.7m)
                systems.HealthTrust = ClampStat(systems.HealthTrust - 2f);
            if (diseaseStats.TrustPenalty > 0f)
                systems.HealthTrust = ClampStat(systems.HealthTrust - diseaseStats.TrustPenalty);
            if (outreachBonus > 0)
            {
                systems.HealthTrust = ClampStat(systems.HealthTrust + 1f);
                if (diseaseStats.ActiveCount == 0)
                    systems.HealthPublic = ClampStat(systems.HealthPublic + 0.5f);
            }

            systems.LastHealthBudget = budgetHealth;
        }

        private static float ComputeHealthNeed(CountryRecord country, float diseaseNeedBonus)
        {
            float healthNeed = 30f;
            if (IsWinter())
                healthNeed += 10f;
            if (country.WarState)
                healthNeed += 10f;
            if (country.Happiness < 35f)
                healthNeed += 5f;
            healthNeed += Math.Max(0f, diseaseNeedBonus);
            return EconomyCalculator.Clamp(healthNeed, 0f, 100f);
        }

        private static void ApplyEducationTick(
            CountryRecord country,
            CountryPublicSystemsData.CountryPublicSystemsRecord systems,
            HashSet<string> lawCodes,
            float educationBudgetEfficiency,
            decimal budgetEducation,
            float baseBudget)
        {
            int schoolingBonus = HasLaw(lawCodes, LawBasicSchooling) ? 1 : 0;
            int literacyDecay = (country.WarState ? 1 : 0) + (country.IsInCrisis ? 1 : 0);
            systems.EduLiteracy = ClampStat(
                systems.EduLiteracy +
                (float)Math.Round(1f * educationBudgetEfficiency) +
                schoolingBonus -
                literacyDecay);

            int universityBonus = HasLaw(lawCodes, LawHigherEduCharter) ? 1 : 0;
            int teacherCertificationBonus = HasLaw(lawCodes, LawTeacherCertification) ? 1 : 0;
            int qualityDecay = budgetEducation < (decimal)baseBudget * 0.5m ? 1 : 0;
            systems.EduQuality = ClampStat(
                systems.EduQuality +
                (float)Math.Round(0.5f * educationBudgetEfficiency) +
                universityBonus +
                teacherCertificationBonus -
                qualityDecay);

            int vocationalBonus = HasLaw(lawCodes, LawScholarshipProgram) ? 1 : 0;
            int specialistsGrowth = (int)Math.Floor((systems.EduLiteracy + systems.EduQuality) / 40f) + vocationalBonus;
            if (teacherCertificationBonus > 0 && systems.EduQuality >= 55f)
                specialistsGrowth += 1;
            systems.EduSpecialists = Math.Clamp(systems.EduSpecialists + Math.Max(0, specialistsGrowth), 0, 999);

            int civicsBonus = HasLaw(lawCodes, LawBasicSchooling) ? 1 : 0;
            int unrestPressure = 0;
            if (systems.EduQuality > 70f && systems.EduCohesion < 40f)
                unrestPressure += 1;
            if (country.Unrest > 60f)
                unrestPressure += 1;
            systems.EduCohesion = ClampStat(systems.EduCohesion + civicsBonus + teacherCertificationBonus - unrestPressure);

            systems.LastEducationBudget = budgetEducation;
        }

        private static string ApplyHealthWorkforceTick(
            CountryRecord country,
            CountryPublicSystemsData.CountryPublicSystemsRecord systems,
            DiseaseTickAggregate diseaseStats,
            float healthBudgetEfficiency,
            bool hasMedicalLicensing,
            bool hasHospitalResidency)
        {
            int doctorGrowth = 0;
            if (hasMedicalLicensing)
                doctorGrowth += 1;
            if (hasHospitalResidency && systems.EduQuality >= 45f)
                doctorGrowth += 1;
            if (healthBudgetEfficiency >= 1.2f)
                doctorGrowth += 1;
            if (healthBudgetEfficiency < 0.5f)
                doctorGrowth -= 1;
            if (country.WarState)
                doctorGrowth -= 1;

            systems.HealthDoctors = Math.Max(0, systems.HealthDoctors + doctorGrowth);

            float fatiguePressure =
                (diseaseStats.ActiveCount * 2f) +
                (systems.MedicMobilizationTicks > 0 ? 4f : 0f) +
                (country.WarState ? 2f : 0f) +
                (Math.Max(0f, 0.9f - healthBudgetEfficiency) * 3f);

            float fatigueRecovery = 1.5f;
            if (hasHospitalResidency)
                fatigueRecovery += 1.2f;
            if (hasMedicalLicensing)
                fatigueRecovery += 0.8f;
            if (systems.HealthDoctors >= 120)
                fatigueRecovery += 0.6f;

            systems.HealthDoctorFatigue = ClampStat(systems.HealthDoctorFatigue + fatiguePressure - fatigueRecovery);

            float doctorNeed =
                60f +
                (diseaseStats.ActiveCount * 18f) +
                (country.WarState ? 20f : 0f);
            float doctorCoverage = doctorNeed <= 0f ? 1f : systems.HealthDoctors / doctorNeed;

            if (doctorCoverage >= 1.1f)
                systems.HealthCapacity = ClampStat(systems.HealthCapacity + 1.5f);
            else if (doctorCoverage < 0.8f)
                systems.HealthCapacity = ClampStat(systems.HealthCapacity - 1.5f);

            if (systems.HealthDoctorFatigue >= 70f)
            {
                systems.HealthCapacity = ClampStat(systems.HealthCapacity - 2f);
                systems.HealthTrust = ClampStat(systems.HealthTrust - 1f);
            }
            else if (systems.HealthDoctorFatigue <= 30f && systems.HealthDoctors >= 100)
            {
                systems.HealthPublic = ClampStat(systems.HealthPublic + 0.5f);
            }

            if (systems.HealthDoctorFatigue >= 70f)
                return "Перевтома медиків";
            if (doctorCoverage < 0.8f)
                return "Дефіцит лікарів";
            if (doctorGrowth > 0 && hasHospitalResidency)
                return "Працюють медичні резиденції";

            return string.Empty;
        }

        private static string ApplyMedicineAndResearchTick(
            CountryPublicSystemsData.CountryPublicSystemsRecord systems,
            HashSet<string> lawCodes,
            List<CountryIntlProgramsData.CountryIntlProgramRecord> intlPrograms,
            DiseaseTickAggregate diseaseStats,
            float healthBudgetEfficiency,
            bool allowRandomEvents,
            bool hasPharmaResearchAgency,
            bool hasClinicalTrialProtocols,
            bool hasEmergencyMedicineReserve)
        {
            int maxProjects = GetMaxDrugProjects(lawCodes);
            systems.HealthDrugProjects = Math.Clamp(systems.HealthDrugProjects, 0, maxProjects);

            int activeDiseases = Math.Max(0, diseaseStats.ActiveCount);
            float stockDelta =
                (hasEmergencyMedicineReserve ? 1.2f : 0.6f) +
                (systems.HealthDrugLevel * 0.35f) -
                (activeDiseases * (hasEmergencyMedicineReserve ? 1.5f : 1.9f)) -
                (systems.HealthDrugProjects * 0.35f);

            if (systems.VaccinationProtectionTicks > 0)
                stockDelta += 0.6f;

            systems.HealthMedicineStock = ClampStat(systems.HealthMedicineStock + stockDelta);

            if (!hasPharmaResearchAgency)
            {
                systems.HealthDrugProjects = 0;
                systems.HealthDrugResearchProgress = 0f;
                systems.HealthClinicalTrialTicks = 0;
                return systems.HealthMedicineStock < 20f ? "Низькі запаси ліків" : string.Empty;
            }

            if (!hasClinicalTrialProtocols)
                systems.HealthClinicalTrialTicks = 0;

            if (systems.HealthDrugProjects <= 0)
            {
                if (systems.HealthDrugResearchProgress > 0f)
                    systems.HealthDrugResearchProgress = Math.Max(0f, systems.HealthDrugResearchProgress - 0.25f);
                return systems.HealthMedicineStock < 20f ? "Низькі запаси ліків" : string.Empty;
            }

            float doctorsFactor = EconomyCalculator.Clamp(systems.HealthDoctors / 160f, 0.4f, 1.7f);
            float fatigueFactor = EconomyCalculator.Clamp(1f - systems.HealthDoctorFatigue / 140f, 0.35f, 1f);
            float budgetFactor = EconomyCalculator.Clamp(0.6f + healthBudgetEfficiency, 0.3f, 2f);
            float projectFactor = 0.7f + (systems.HealthDrugProjects * 0.45f);
            float qualityFactor = EconomyCalculator.Clamp(systems.EduQuality / 70f, 0.6f, 1.7f);
            float progressGain = budgetFactor * doctorsFactor * fatigueFactor * projectFactor * qualityFactor;

            if (HasLaw(lawCodes, LawIntlDataSharingHealth))
                progressGain += 0.6f;
            if (HasProgramActive(intlPrograms, ProgramHealthDataSharing))
                progressGain += 0.4f;
            if (hasClinicalTrialProtocols && systems.HealthClinicalTrialTicks > 0)
                progressGain *= 1.35f;

            systems.HealthDrugResearchProgress += progressGain;

            int breakthroughs = 0;
            while (systems.HealthDrugResearchProgress >= 100f && systems.HealthDrugProjects > 0)
            {
                systems.HealthDrugResearchProgress -= 100f;
                systems.HealthDrugProjects = Math.Max(0, systems.HealthDrugProjects - 1);
                systems.HealthDrugLevel = Math.Max(0, systems.HealthDrugLevel + 1);
                breakthroughs++;

                systems.HealthMedicineStock = ClampStat(systems.HealthMedicineStock + 14f);
                systems.HealthTrust = ClampStat(systems.HealthTrust + 1.5f);
                systems.HealthPublic = ClampStat(systems.HealthPublic + 0.8f);
            }

            if (allowRandomEvents &&
                systems.HealthDrugProjects > 0 &&
                !hasClinicalTrialProtocols &&
                Rng.NextDouble() < (0.025 * systems.HealthDrugProjects))
            {
                systems.HealthTrust = ClampStat(systems.HealthTrust - 2f);
                systems.HealthPublic = ClampStat(systems.HealthPublic - 1f);
                return breakthroughs > 0
                    ? $"Фарма-проривів: {breakthroughs}, але був невдалий тест"
                    : "Невдале випробування препарату";
            }

            if (allowRandomEvents &&
                systems.HealthDrugProjects > 0 &&
                hasClinicalTrialProtocols &&
                Rng.NextDouble() < 0.02)
            {
                systems.HealthTrust = ClampStat(systems.HealthTrust + 1f);
                systems.HealthMedicineStock = ClampStat(systems.HealthMedicineStock + 2f);
            }

            if (breakthroughs > 0)
                return $"Фарма-проривів: {breakthroughs}";
            if (systems.HealthMedicineStock < 20f)
                return "Низькі запаси ліків";
            return "Триває розробка ліків";
        }

        private static void ApplyMedicineStockDiseaseMitigation(
            PublicSystemsTickResult result,
            CountryPublicSystemsData.CountryPublicSystemsRecord systems,
            int activeDiseases,
            bool hasEmergencyMedicineReserve)
        {
            if (result == null || systems == null || activeDiseases <= 0)
                return;

            float stockFactor = systems.HealthMedicineStock / 100f;
            float drugFactor = 1f + (Math.Min(6, systems.HealthDrugLevel) * 0.1f);
            float reliefRatio = EconomyCalculator.Clamp(stockFactor * 0.35f * drugFactor, 0f, 0.55f);
            result.DiseaseProductionPenalty = Math.Max(0f, result.DiseaseProductionPenalty * (1f - reliefRatio));

            float lowStockThreshold = hasEmergencyMedicineReserve ? 18f : 12f;
            if (systems.HealthMedicineStock < lowStockThreshold)
            {
                systems.HealthTrust = ClampStat(systems.HealthTrust - 1f);
                result.DiseaseHealthNeedBonus += 4f;
            }
        }

        private static int GetMaxDrugProjects(HashSet<string> lawCodes)
        {
            int max = HasLaw(lawCodes, LawPharmaResearchAgency) ? 1 : 0;
            if (max <= 0)
                return 0;

            if (HasLaw(lawCodes, LawClinicalTrialProtocols))
                max++;
            if (HasLaw(lawCodes, LawIntlDataSharingHealth))
                max++;

            return Math.Clamp(max, 0, 5);
        }

        private static void NormalizeSystemStats(CountryPublicSystemsData.CountryPublicSystemsRecord systems)
        {
            systems.HealthPublic = ClampStat(systems.HealthPublic);
            systems.HealthSanitation = ClampStat(systems.HealthSanitation);
            systems.HealthCapacity = ClampStat(systems.HealthCapacity);
            systems.HealthTrust = ClampStat(systems.HealthTrust);
            systems.HealthDoctorFatigue = ClampStat(systems.HealthDoctorFatigue);
            systems.HealthMedicineStock = ClampStat(systems.HealthMedicineStock);
            systems.HealthDrugResearchProgress = EconomyCalculator.Clamp(systems.HealthDrugResearchProgress, 0f, 99.99f);
            systems.HealthDoctors = Math.Max(0, systems.HealthDoctors);
            systems.HealthDrugLevel = Math.Max(0, systems.HealthDrugLevel);
            systems.HealthDrugProjects = Math.Max(0, systems.HealthDrugProjects);
            systems.HealthClinicalTrialTicks = Math.Max(0, systems.HealthClinicalTrialTicks);
            systems.EduLiteracy = ClampStat(systems.EduLiteracy);
            systems.EduQuality = ClampStat(systems.EduQuality);
            systems.EduCohesion = ClampStat(systems.EduCohesion);
            systems.IntlPrestige = ClampStat(systems.IntlPrestige);
            systems.BudgetDebt = Math.Max(0m, systems.BudgetDebt);
            systems.PendingEmergencyHealthSpend = Math.Max(0m, systems.PendingEmergencyHealthSpend);
            systems.PendingEmergencyEducationSpend = Math.Max(0m, systems.PendingEmergencyEducationSpend);
            systems.UpdatedAt = DateTime.UtcNow;
        }

        private static IntlProgramTickResult ApplyInternationalPrograms(
            CountryRecord country,
            CountryPublicSystemsData.CountryPublicSystemsRecord systems,
            List<CountryIntlProgramsData.CountryIntlProgramRecord> programs,
            HashSet<string> lawCodes,
            bool allowRandomEvents,
            List<CountryDiseasesData.CountryDiseaseRecord> diseases)
        {
            var result = new IntlProgramTickResult();
            if (country == null || systems == null || programs == null || programs.Count == 0)
                return result;

            if (!HasLaw(lawCodes, LawIntlCooperation))
                return result;

            decimal prestigeFactor = ClampDecimal(1m - (decimal)systems.IntlPrestige / 500m, 0.7m, 1m);
            var operational = new List<CountryIntlProgramsData.CountryIntlProgramRecord>();
            decimal totalOperationalCost = 0m;

            foreach (var program in programs)
            {
                if (program == null || !program.IsActive)
                    continue;

                program.ProgramType = NormalizeProgramType(program.ProgramType);
                bool unlocked = IsProgramUnlocked(program.ProgramType, lawCodes);
                bool diplomacyOk = IsProgramDiplomacyAllowed(country.CountryId, program.PartnerCountryId);
                int requiredSpec = Math.Max(program.RequiredSpec, GetDefaultProgramRequiredSpec(program.ProgramType));
                bool specialistsOk = systems.EduSpecialists >= requiredSpec;

                if (!unlocked || !diplomacyOk || !specialistsOk)
                {
                    result.BlockedPrograms++;
                    continue;
                }

                decimal baseCost = program.CostPerTick > 0m ? program.CostPerTick : GetDefaultProgramCost(program.ProgramType);
                baseCost = Math.Max(0m, baseCost);
                decimal adjustedCost = baseCost * prestigeFactor;
                totalOperationalCost += adjustedCost;
                operational.Add(program);
            }

            if (operational.Count == 0 || totalOperationalCost <= 0m)
            {
                systems.IntlPrestigeTicker = 0;
                return result;
            }

            decimal budgetSpent = systems.BudgetIntlMode.Equals("Manual", StringComparison.OrdinalIgnoreCase)
                ? Math.Min(totalOperationalCost, Math.Max(0m, systems.BudgetIntl))
                : totalOperationalCost;
            float coverage = totalOperationalCost <= 0m
                ? 1f
                : (float)Math.Min(1m, budgetSpent / totalOperationalCost);

            result.BudgetSpent = budgetSpent;
            result.OperationalPrograms = operational.Count;
            result.EpidemicChanceModifier = 1f;

            if (systems.BudgetIntlMode.Equals("Manual", StringComparison.OrdinalIgnoreCase) && coverage < 1f)
                systems.IntlPrestige = ClampStat(systems.IntlPrestige - 1f);

            foreach (var program in operational)
            {
                program.TicksActive = Math.Max(0, program.TicksActive) + 1;
                if (coverage >= 0.95f)
                    program.SuccessTicks = Math.Max(0, program.SuccessTicks) + 1;

                switch (program.ProgramType)
                {
                    case ProgramHealthExchange:
                        systems.HealthCapacity = ClampStat(systems.HealthCapacity + (1f * coverage));
                        systems.HealthSanitation = ClampStat(systems.HealthSanitation + (1f * coverage));
                        if (allowRandomEvents && Rng.NextDouble() < 0.01 * coverage)
                            systems.HealthPublic = ClampStat(systems.HealthPublic + 1f);
                        if (allowRandomEvents && Rng.NextDouble() < 0.008 * coverage)
                        {
                            systems.HealthTrust = ClampStat(systems.HealthTrust - 1f);
                            systems.IntlPrestige = ClampStat(systems.IntlPrestige - 1f);
                        }
                        break;

                    case ProgramHealthDataSharing:
                        result.ControlScoreBonus += 4f * coverage;
                        result.EpidemicChanceModifier *= 0.6f;
                        if (allowRandomEvents && Rng.NextDouble() < 0.01 * coverage)
                            systems.IntlPrestige = ClampStat(systems.IntlPrestige - 3f);
                        break;

                    case ProgramEduStudentExchange:
                        systems.EduQuality = ClampStat(systems.EduQuality + (0.8f * coverage));
                        systems.EduSpecialists = Math.Clamp(systems.EduSpecialists + (int)Math.Ceiling(1f * coverage), 0, 999);
                        if (allowRandomEvents &&
                            (systems.EduCohesion < 40f || country.Treasury < 0) &&
                            Rng.NextDouble() < 0.035 * coverage)
                        {
                            systems.EduSpecialists = Math.Max(0, systems.EduSpecialists - 1);
                            systems.IntlPrestige = ClampStat(systems.IntlPrestige - 2f);
                        }
                        break;

                    case ProgramEduJointResearch:
                        country.ResearchSpeed = ClampStat(country.ResearchSpeed + (0.3f * coverage));
                        if (allowRandomEvents && Rng.NextDouble() < 0.03 * coverage)
                            country.ResearchSpeed = ClampStat(country.ResearchSpeed + 0.4f);
                        if (allowRandomEvents && Rng.NextDouble() < 0.015 * coverage)
                        {
                            systems.IntlPrestige = ClampStat(systems.IntlPrestige - 4f);
                            country.Stability = ClampStat(country.Stability - 0.8f);
                        }
                        break;
                }
            }

            if (allowRandomEvents && operational.Any(p => p.ProgramType == ProgramHealthExchange) && Rng.NextDouble() < 0.01 * coverage)
            {
                diseases.Add(new CountryDiseasesData.CountryDiseaseRecord
                {
                    CountryId = country.CountryId,
                    Name = "Імпортований штам",
                    Severity = 2,
                    Infectiousness = 2,
                    DurationTicks = 4,
                    RemainingTicks = 4,
                    Stage = 1,
                    TargetScope = "whole_state",
                    Active = true
                });
                systems.IntlPrestige = ClampStat(systems.IntlPrestige - 2f);
            }

            systems.IntlPrestigeTicker = Math.Max(0, systems.IntlPrestigeTicker) + 1;
            if (systems.IntlPrestigeTicker >= 3)
            {
                systems.IntlPrestige = ClampStat(systems.IntlPrestige + 1f);
                systems.IntlPrestigeTicker = 0;
            }

            result.Notes = $"Міжнародні програми: активних {result.OperationalPrograms}, заблокованих {result.BlockedPrograms}";
            return result;
        }

        private static DiseaseTickAggregate ApplyDiseaseTick(
            CountryRecord country,
            CountryPublicSystemsData.CountryPublicSystemsRecord systems,
            List<CountryDiseasesData.CountryDiseaseRecord> diseases,
            float controlScoreBonus,
            bool hasQuarantineProtocols,
            bool allowRandomEvents)
        {
            var aggregate = new DiseaseTickAggregate();
            if (diseases == null || diseases.Count == 0)
                return aggregate;

            foreach (var disease in diseases)
            {
                if (disease == null || !disease.Active)
                    continue;

                disease.Severity = Math.Clamp(disease.Severity, 1, 5);
                disease.Infectiousness = Math.Clamp(disease.Infectiousness, 1, 5);
                disease.DurationTicks = Math.Max(1, disease.DurationTicks);
                disease.RemainingTicks = Math.Max(0, disease.RemainingTicks);
                if (disease.RemainingTicks <= 0)
                {
                    disease.Active = false;
                    disease.Stage = 0;
                    disease.UpdatedAt = DateTime.UtcNow;
                    continue;
                }

                int stage = disease.Stage <= 0 ? 1 : Math.Clamp(disease.Stage, 1, 4);
                int effectiveInfectiousness = disease.Infectiousness;
                if (systems.QuarantineEnabled && hasQuarantineProtocols)
                    effectiveInfectiousness = Math.Max(1, effectiveInfectiousness - 1);

                float controlScore =
                    ((systems.HealthCapacity + systems.HealthSanitation + systems.HealthTrust) / 3f)
                    - (disease.Severity * 10f + effectiveInfectiousness * 6f)
                    + controlScoreBonus;

                disease.RemainingTicks = Math.Max(0, disease.RemainingTicks - 1);
                if (controlScore >= 10f)
                {
                    bool accelerate = allowRandomEvents ? Rng.NextDouble() < 0.35 : true;
                    if (accelerate)
                    {
                        disease.Stage = Math.Min(4, stage + 1);
                        disease.RemainingTicks = Math.Max(0, disease.RemainingTicks - 1);
                    }
                }
                else if (controlScore <= -10f)
                {
                    if (stage < 2)
                        disease.Stage = 2;
                    else
                        disease.Stage = Math.Min(3, stage + 1);
                    disease.RemainingTicks += 1;
                    aggregate.TrustPenalty += Math.Clamp(disease.Severity, 1, 3);
                }

                float progress = 1f - (disease.RemainingTicks / (float)Math.Max(1, disease.DurationTicks));
                if (progress >= 0.33f && disease.Stage < 2)
                    disease.Stage = 2;
                if (progress >= 0.66f && disease.Stage < 3)
                    disease.Stage = 3;
                if (progress >= 0.90f && disease.Stage < 4)
                    disease.Stage = 4;

                if (disease.RemainingTicks <= 0 || (disease.Stage >= 4 && disease.RemainingTicks <= 1))
                {
                    disease.Active = false;
                    disease.Stage = 0;
                    disease.RemainingTicks = 0;
                    disease.UpdatedAt = DateTime.UtcNow;
                    continue;
                }

                stage = Math.Clamp(disease.Stage, 1, 4);
                float stageFactor = GetStageFactor(stage);
                aggregate.ProductionPenalty += (decimal)(0.02f * disease.Severity * effectiveInfectiousness * stageFactor);
                aggregate.HealthNeedBonus += 8f * disease.Severity * stageFactor;
                aggregate.ActiveCount++;
                disease.UpdatedAt = DateTime.UtcNow;
            }

            if (systems.QuarantineEnabled && aggregate.ActiveCount > 0 && hasQuarantineProtocols)
                aggregate.ProductionPenalty += 0.10m;

            aggregate.ProductionPenalty = Math.Max(0m, aggregate.ProductionPenalty);
            aggregate.HealthNeedBonus = Math.Max(0f, aggregate.HealthNeedBonus);
            aggregate.TrustPenalty = Math.Max(0f, aggregate.TrustPenalty);
            return aggregate;
        }

        private static void TrySpawnRandomDisease(
            CountryRecord country,
            CountryPublicSystemsData.CountryPublicSystemsRecord systems,
            List<CountryDiseasesData.CountryDiseaseRecord> diseases,
            bool hasHealthMinistry,
            bool hasNationalImmunization,
            float epidemicChanceModifier)
        {
            if (country == null || systems == null || diseases == null)
                return;

            int activeDiseases = diseases.Count(d => d != null && d.Active);
            if (activeDiseases >= 3)
                return;

            float chance = EconomyConfigData.GetFloat("PublicEpidemicChance", 0.02f);
            chance *= Math.Max(0.1f, epidemicChanceModifier);
            if (!hasHealthMinistry)
                chance += 0.01f;
            if (hasNationalImmunization && systems.VaccinationProtectionTicks > 0)
                chance *= 0.4f;
            chance = EconomyCalculator.Clamp(chance, 0f, 0.25f);

            if (Rng.NextDouble() >= chance)
                return;

            string[] names =
            {
                "Гриповий спалах",
                "Сіра гарячка",
                "Лихоманка мандрівників",
                "Пилкова чума"
            };

            int duration = Rng.Next(4, 9);
            diseases.Add(new CountryDiseasesData.CountryDiseaseRecord
            {
                CountryId = country.CountryId,
                Name = names[Rng.Next(names.Length)],
                Severity = Rng.Next(2, 5),
                Infectiousness = Rng.Next(2, 5),
                DurationTicks = duration,
                RemainingTicks = duration,
                Stage = 1,
                TargetScope = "whole_state",
                Active = true
            });
        }

        private static bool IsProgramUnlocked(string programType, HashSet<string> lawCodes)
        {
            string normalized = NormalizeProgramType(programType);
            if (normalized == ProgramHealthDataSharing)
                return HasLaw(lawCodes, LawIntlDataSharingHealth);
            if (normalized == ProgramEduStudentExchange || normalized == ProgramEduJointResearch)
                return HasLaw(lawCodes, LawIntlAcademicExchange);
            return true;
        }

        private static bool IsProgramDiplomacyAllowed(int countryId, int? partnerCountryId)
        {
            if (!partnerCountryId.HasValue || partnerCountryId.Value <= 0)
                return true;

            var relation = DiplomacyManager.GetRelation(countryId, partnerCountryId.Value);
            bool notWorseThanNeutral = relation.State <= DiplomaticState.Neutral;
            return notWorseThanNeutral && !relation.IsAtWar && relation.State != DiplomaticState.War;
        }

        private static bool HasProgramActive(
            List<CountryIntlProgramsData.CountryIntlProgramRecord> programs,
            string programType)
        {
            if (programs == null || programs.Count == 0)
                return false;
            string normalized = NormalizeProgramType(programType);
            return programs.Any(p => p != null &&
                                     p.IsActive &&
                                     string.Equals(NormalizeProgramType(p.ProgramType), normalized, StringComparison.OrdinalIgnoreCase));
        }

        private static float ComputeBudgetEfficiency(decimal budget, float baseBudget)
        {
            if (budget <= 0m || baseBudget <= 0f)
                return 0f;

            double value = Math.Sqrt((double)(budget / (decimal)baseBudget));
            return EconomyCalculator.Clamp((float)value, 0f, 2f);
        }

        private static bool IsWinter()
        {
            string season = EconomyConfigData.GetText("CurrentSeason", "Summer");
            return season.Equals("Winter", StringComparison.OrdinalIgnoreCase);
        }

        private static float GetStageFactor(int stage)
        {
            return stage switch
            {
                1 => 1f,
                2 => 2f,
                3 => 1f,
                4 => 0.5f,
                _ => 0f
            };
        }

        private static float ClampStat(float value)
        {
            return EconomyCalculator.Clamp(value, 0f, 100f);
        }

        private static decimal ClampDecimal(decimal value, decimal min, decimal max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        private static void CollectLawCodes(string? raw, HashSet<string> output)
        {
            if (string.IsNullOrWhiteSpace(raw) || output == null)
                return;

            foreach (string token in Tokenize(raw))
            {
                if (token.StartsWith("LAW_", StringComparison.OrdinalIgnoreCase))
                    output.Add(token.ToUpperInvariant());
            }
        }

        private static IEnumerable<string> Tokenize(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                yield break;

            var sb = new StringBuilder(raw.Length);
            foreach (char c in raw)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    sb.Append(char.ToUpperInvariant(c));
                    continue;
                }

                if (sb.Length > 0)
                {
                    yield return sb.ToString();
                    sb.Clear();
                }
            }

            if (sb.Length > 0)
                yield return sb.ToString();
        }
    }
}

