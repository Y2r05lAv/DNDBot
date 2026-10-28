

    #nullable enable

using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DNDBot.DataBase
{
     
    public class CountryRecord
    {
        public int CountryId { get; set; }
        public required string Name { get; set; }
        public string? FlagEmoji { get; set; }
        public required string FormOfGovernment { get; set; }
        public int GovernmentLevel { get; set; }
        public int GovernmentXP { get; set; }

        public float Stability { get; set; }
        public float PopularLoyalty { get; set; }
        public float EliteLoyalty { get; set; }
        public float Corruption { get; set; }
        public float Legitimacy { get; set; }
        public float Control { get; set; }
        public float Inflation { get; set; }

        public long Treasury { get; set; }
        public int TaxLevel { get; set; }
        public decimal IncomePerTurn { get; set; }
        public decimal ExpensesPerTurn { get; set; }
        public decimal CorruptionLosses { get; set; }
        public float WorkRate { get; set; }
        public long AvailableWorkers { get; set; }
        public float UnemploymentRate { get; set; }
        public bool WarState { get; set; }
        public int SanctionsLevel { get; set; }

        public long Population { get; set; }
        public float PopulationGrowth { get; set; }
        public float Happiness { get; set; }
        public float Unrest { get; set; }
        public long Refugees { get; set; }

        public float NobilityInfluence { get; set; }
        public float MerchantsInfluence { get; set; }
        public float ClergyInfluence { get; set; }
        public float MilitaryInfluence { get; set; }
        public float MagesInfluence { get; set; }
        public float PeasantsInfluence { get; set; }
        public float ExternalInfluence { get; set; }

        public bool HasParliament { get; set; }
        public int ParliamentSeats { get; set; }
        public int ParliamentFractions { get; set; }
        public float ParliamentSupport { get; set; }
        public float OppositionStrength { get; set; }

        public int ActiveLawsCount { get; set; }
        public int MaxLawsAllowed { get; set; }
        public bool EmergencyLawActive { get; set; }

        public int ArmySize { get; set; }
        public float ArmyLoyalty { get; set; }
        public float ArmyMorale { get; set; }
        public decimal MilitaryUpkeep { get; set; }
        public bool MartialLaw { get; set; }

        public float MagicInfluence { get; set; }
        public float ManaStability { get; set; }
        public int ActiveManaZones { get; set; }
        public float ArcaneRisk { get; set; }

        public int Cities { get; set; }
        public int Villages { get; set; }
        public float InfrastructureLevel { get; set; }
        public float LogisticsEfficiency { get; set; }

        public int ResearchLevel { get; set; }
        public float ResearchSpeed { get; set; }
        public int CompletedResearches { get; set; }

        public float CoupRisk { get; set; }
        public float RebellionRisk { get; set; }
        public float AssassinationRisk { get; set; }

        public int Allies { get; set; }
        public int Enemies { get; set; }
        public float InternationalReputation { get; set; }

        public float IntelLevel { get; set; }
        public float CounterIntelLevel { get; set; }

        public DateTime? LastUpdate { get; set; }
        public int TurnsSurvived { get; set; }

        public bool IsInCrisis { get; set; }
        public bool IsInGoldenAge { get; set; }
        public bool IsCollapsed { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public static class CountriesData
    {
        static CountriesData()
        {
            BaseRepository.DBInitialization();
        }

        private static SqlParameter DecimalParam(string name, decimal value, byte precision, byte scale)
        {
            var p = new SqlParameter(name, SqlDbType.Decimal)
            {
                Precision = precision,
                Scale = scale,
                Value = value
            };
            return p;
        }

        private static long SafeAddLong(long a, long b)
        {
            try
            {
                return checked(a + b);
            }
            catch (OverflowException)
            {
                return long.MaxValue;
            }
        }

        private static int SafeAddInt(int a, int b)
        {
            long sum = (long)a + b;
            if (sum > int.MaxValue)
                return int.MaxValue;
            if (sum < int.MinValue)
                return int.MinValue;
            return (int)sum;
        }

        public static string AddCountry(CountryRecord country)
        {
            const string insert = @"
INSERT INTO Countries (
    Name, FlagEmoji, FormOfGovernment, GovernmentLevel, GovernmentXP,
    Stability, PopularLoyalty, EliteLoyalty, Corruption, Legitimacy, Control, Inflation,
    Treasury, TaxLevel, IncomePerTurn, ExpensesPerTurn, CorruptionLosses,
    WorkRate, AvailableWorkers, UnemploymentRate, WarState, SanctionsLevel,
    Population, PopulationGrowth, Happiness, Unrest, Refugees,
    NobilityInfluence, MerchantsInfluence, ClergyInfluence, MilitaryInfluence, MagesInfluence, PeasantsInfluence, ExternalInfluence,
    HasParliament, ParliamentSeats, ParliamentFractions, ParliamentSupport, OppositionStrength,
    ActiveLawsCount, MaxLawsAllowed, EmergencyLawActive,
    ArmySize, ArmyLoyalty, ArmyMorale, MilitaryUpkeep, MartialLaw,
    MagicInfluence, ManaStability, ActiveManaZones, ArcaneRisk,
    Cities, Villages, InfrastructureLevel, LogisticsEfficiency,
    ResearchLevel, ResearchSpeed, CompletedResearches,
    CoupRisk, RebellionRisk, AssassinationRisk,
    Allies, Enemies, InternationalReputation, IntelLevel, CounterIntelLevel,
    LastUpdate, TurnsSurvived,
    IsInCrisis, IsInGoldenAge, IsCollapsed,
    CreatedAt
)
VALUES (
    @Name, @FlagEmoji, @FormOfGovernment, @GovernmentLevel, @GovernmentXP,
    @Stability, @PopularLoyalty, @EliteLoyalty, @Corruption, @Legitimacy, @Control, @Inflation,
    @Treasury, @TaxLevel, @IncomePerTurn, @ExpensesPerTurn, @CorruptionLosses,
    @WorkRate, @AvailableWorkers, @UnemploymentRate, @WarState, @SanctionsLevel,
    @Population, @PopulationGrowth, @Happiness, @Unrest, @Refugees,
    @NobilityInfluence, @MerchantsInfluence, @ClergyInfluence, @MilitaryInfluence, @MagesInfluence, @PeasantsInfluence, @ExternalInfluence,
    @HasParliament, @ParliamentSeats, @ParliamentFractions, @ParliamentSupport, @OppositionStrength,
    @ActiveLawsCount, @MaxLawsAllowed, @EmergencyLawActive,
    @ArmySize, @ArmyLoyalty, @ArmyMorale, @MilitaryUpkeep, @MartialLaw,
    @MagicInfluence, @ManaStability, @ActiveManaZones, @ArcaneRisk,
    @Cities, @Villages, @InfrastructureLevel, @LogisticsEfficiency,
    @ResearchLevel, @ResearchSpeed, @CompletedResearches,
    @CoupRisk, @RebellionRisk, @AssassinationRisk,
    @Allies, @Enemies, @InternationalReputation, @IntelLevel, @CounterIntelLevel,
    @LastUpdate, @TurnsSurvived,
    @IsInCrisis, @IsInGoldenAge, @IsCollapsed,
    @CreatedAt
);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var idObj = BaseRepository.ExecuteScalar(insert,
                BaseRepository.Param("@Name", SqlDbType.NVarChar, country.Name, 100),
                BaseRepository.Param("@FlagEmoji", SqlDbType.NVarChar, country.FlagEmoji, 5),
                BaseRepository.Param("@FormOfGovernment", SqlDbType.NVarChar, country.FormOfGovernment, 50),
                BaseRepository.Param("@GovernmentLevel", SqlDbType.Int, country.GovernmentLevel),
                BaseRepository.Param("@GovernmentXP", SqlDbType.Int, country.GovernmentXP),
                BaseRepository.Param("@Stability", SqlDbType.Real, country.Stability),
                BaseRepository.Param("@PopularLoyalty", SqlDbType.Real, country.PopularLoyalty),
                BaseRepository.Param("@EliteLoyalty", SqlDbType.Real, country.EliteLoyalty),
                BaseRepository.Param("@Corruption", SqlDbType.Real, country.Corruption),
                BaseRepository.Param("@Legitimacy", SqlDbType.Real, country.Legitimacy),
                BaseRepository.Param("@Control", SqlDbType.Real, country.Control),
                BaseRepository.Param("@Inflation", SqlDbType.Real, country.Inflation),
                BaseRepository.Param("@Treasury", SqlDbType.BigInt, country.Treasury),
                BaseRepository.Param("@TaxLevel", SqlDbType.Int, country.TaxLevel),
                DecimalParam("@IncomePerTurn", country.IncomePerTurn, 28, 2),
                DecimalParam("@ExpensesPerTurn", country.ExpensesPerTurn, 28, 2),
                DecimalParam("@CorruptionLosses", country.CorruptionLosses, 28, 2),
                BaseRepository.Param("@WorkRate", SqlDbType.Real, country.WorkRate),
                BaseRepository.Param("@AvailableWorkers", SqlDbType.BigInt, country.AvailableWorkers),
                BaseRepository.Param("@UnemploymentRate", SqlDbType.Real, country.UnemploymentRate),
                BaseRepository.Param("@WarState", SqlDbType.Bit, country.WarState),
                BaseRepository.Param("@SanctionsLevel", SqlDbType.Int, country.SanctionsLevel),
                BaseRepository.Param("@Population", SqlDbType.BigInt, country.Population),
                BaseRepository.Param("@PopulationGrowth", SqlDbType.Real, country.PopulationGrowth),
                BaseRepository.Param("@Happiness", SqlDbType.Real, country.Happiness),
                BaseRepository.Param("@Unrest", SqlDbType.Real, country.Unrest),
                BaseRepository.Param("@Refugees", SqlDbType.BigInt, country.Refugees),
                BaseRepository.Param("@NobilityInfluence", SqlDbType.Real, country.NobilityInfluence),
                BaseRepository.Param("@MerchantsInfluence", SqlDbType.Real, country.MerchantsInfluence),
                BaseRepository.Param("@ClergyInfluence", SqlDbType.Real, country.ClergyInfluence),
                BaseRepository.Param("@MilitaryInfluence", SqlDbType.Real, country.MilitaryInfluence),
                BaseRepository.Param("@MagesInfluence", SqlDbType.Real, country.MagesInfluence),
                BaseRepository.Param("@PeasantsInfluence", SqlDbType.Real, country.PeasantsInfluence),
                BaseRepository.Param("@ExternalInfluence", SqlDbType.Real, country.ExternalInfluence),
                BaseRepository.Param("@HasParliament", SqlDbType.Bit, country.HasParliament),
                BaseRepository.Param("@ParliamentSeats", SqlDbType.Int, country.ParliamentSeats),
                BaseRepository.Param("@ParliamentFractions", SqlDbType.Int, country.ParliamentFractions),
                BaseRepository.Param("@ParliamentSupport", SqlDbType.Real, country.ParliamentSupport),
                BaseRepository.Param("@OppositionStrength", SqlDbType.Real, country.OppositionStrength),
                BaseRepository.Param("@ActiveLawsCount", SqlDbType.Int, country.ActiveLawsCount),
                BaseRepository.Param("@MaxLawsAllowed", SqlDbType.Int, country.MaxLawsAllowed),
                BaseRepository.Param("@EmergencyLawActive", SqlDbType.Bit, country.EmergencyLawActive),
                BaseRepository.Param("@ArmySize", SqlDbType.Int, country.ArmySize),
                BaseRepository.Param("@ArmyLoyalty", SqlDbType.Real, country.ArmyLoyalty),
                BaseRepository.Param("@ArmyMorale", SqlDbType.Real, country.ArmyMorale),
                DecimalParam("@MilitaryUpkeep", country.MilitaryUpkeep, 28, 2),
                BaseRepository.Param("@MartialLaw", SqlDbType.Bit, country.MartialLaw),
                BaseRepository.Param("@MagicInfluence", SqlDbType.Real, country.MagicInfluence),
                BaseRepository.Param("@ManaStability", SqlDbType.Real, country.ManaStability),
                BaseRepository.Param("@ActiveManaZones", SqlDbType.Int, country.ActiveManaZones),
                BaseRepository.Param("@ArcaneRisk", SqlDbType.Real, country.ArcaneRisk),
                BaseRepository.Param("@Cities", SqlDbType.Int, country.Cities),
                BaseRepository.Param("@Villages", SqlDbType.Int, country.Villages),
                BaseRepository.Param("@InfrastructureLevel", SqlDbType.Real, country.InfrastructureLevel),
                BaseRepository.Param("@LogisticsEfficiency", SqlDbType.Real, country.LogisticsEfficiency),
                BaseRepository.Param("@ResearchLevel", SqlDbType.Int, country.ResearchLevel),
                BaseRepository.Param("@ResearchSpeed", SqlDbType.Real, country.ResearchSpeed),
                BaseRepository.Param("@CompletedResearches", SqlDbType.Int, country.CompletedResearches),
                BaseRepository.Param("@CoupRisk", SqlDbType.Real, country.CoupRisk),
                BaseRepository.Param("@RebellionRisk", SqlDbType.Real, country.RebellionRisk),
                BaseRepository.Param("@AssassinationRisk", SqlDbType.Real, country.AssassinationRisk),
                BaseRepository.Param("@Allies", SqlDbType.Int, country.Allies),
                BaseRepository.Param("@Enemies", SqlDbType.Int, country.Enemies),
                BaseRepository.Param("@InternationalReputation", SqlDbType.Real, country.InternationalReputation),
                BaseRepository.Param("@IntelLevel", SqlDbType.Real, country.IntelLevel),
                BaseRepository.Param("@CounterIntelLevel", SqlDbType.Real, country.CounterIntelLevel),
                BaseRepository.Param("@LastUpdate", SqlDbType.DateTime2, (object?)country.LastUpdate ?? DBNull.Value),
                BaseRepository.Param("@TurnsSurvived", SqlDbType.Int, country.TurnsSurvived),
                BaseRepository.Param("@IsInCrisis", SqlDbType.Bit, country.IsInCrisis),
                BaseRepository.Param("@IsInGoldenAge", SqlDbType.Bit, country.IsInGoldenAge),
                BaseRepository.Param("@IsCollapsed", SqlDbType.Bit, country.IsCollapsed),
                BaseRepository.Param("@CreatedAt", SqlDbType.DateTime2, country.CreatedAt)
            );

            if (idObj == null || idObj == DBNull.Value)
                return "Помилка додавання країни.";

            return $"Країна додана з Id = {Convert.ToInt32(idObj)}";
        }

        public static string UpdateCountry(CountryRecord country)
        {
            const string update = @"
UPDATE Countries
SET Name = @Name,
    FlagEmoji = @FlagEmoji,
    FormOfGovernment = @FormOfGovernment,
    GovernmentLevel = @GovernmentLevel,
    GovernmentXP = @GovernmentXP,
    Stability = @Stability,
    PopularLoyalty = @PopularLoyalty,
    EliteLoyalty = @EliteLoyalty,
    Corruption = @Corruption,
    Legitimacy = @Legitimacy,
    Control = @Control,
    Inflation = @Inflation,
    Treasury = @Treasury,
    TaxLevel = @TaxLevel,
    IncomePerTurn = @IncomePerTurn,
    ExpensesPerTurn = @ExpensesPerTurn,
    CorruptionLosses = @CorruptionLosses,
    WorkRate = @WorkRate,
    AvailableWorkers = @AvailableWorkers,
    UnemploymentRate = @UnemploymentRate,
    WarState = @WarState,
    SanctionsLevel = @SanctionsLevel,
    Population = @Population,
    PopulationGrowth = @PopulationGrowth,
    Happiness = @Happiness,
    Unrest = @Unrest,
    Refugees = @Refugees,
    NobilityInfluence = @NobilityInfluence,
    MerchantsInfluence = @MerchantsInfluence,
    ClergyInfluence = @ClergyInfluence,
    MilitaryInfluence = @MilitaryInfluence,
    MagesInfluence = @MagesInfluence,
    PeasantsInfluence = @PeasantsInfluence,
    ExternalInfluence = @ExternalInfluence,
    HasParliament = @HasParliament,
    ParliamentSeats = @ParliamentSeats,
    ParliamentFractions = @ParliamentFractions,
    ParliamentSupport = @ParliamentSupport,
    OppositionStrength = @OppositionStrength,
    ActiveLawsCount = @ActiveLawsCount,
    MaxLawsAllowed = @MaxLawsAllowed,
    EmergencyLawActive = @EmergencyLawActive,
    ArmySize = @ArmySize,
    ArmyLoyalty = @ArmyLoyalty,
    ArmyMorale = @ArmyMorale,
    MilitaryUpkeep = @MilitaryUpkeep,
    MartialLaw = @MartialLaw,
    MagicInfluence = @MagicInfluence,
    ManaStability = @ManaStability,
    ActiveManaZones = @ActiveManaZones,
    ArcaneRisk = @ArcaneRisk,
    Cities = @Cities,
    Villages = @Villages,
    InfrastructureLevel = @InfrastructureLevel,
    LogisticsEfficiency = @LogisticsEfficiency,
    ResearchLevel = @ResearchLevel,
    ResearchSpeed = @ResearchSpeed,
    CompletedResearches = @CompletedResearches,
    CoupRisk = @CoupRisk,
    RebellionRisk = @RebellionRisk,
    AssassinationRisk = @AssassinationRisk,
    Allies = @Allies,
    Enemies = @Enemies,
    InternationalReputation = @InternationalReputation,
    IntelLevel = @IntelLevel,
    CounterIntelLevel = @CounterIntelLevel,
    LastUpdate = @LastUpdate,
    TurnsSurvived = @TurnsSurvived,
    IsInCrisis = @IsInCrisis,
    IsInGoldenAge = @IsInGoldenAge,
    IsCollapsed = @IsCollapsed,
    CreatedAt = @CreatedAt
WHERE CountryId = @CountryId";

            int rows = BaseRepository.ExecuteNonQuery(update,
                BaseRepository.Param("@Name", SqlDbType.NVarChar, country.Name, 100),
                BaseRepository.Param("@FlagEmoji", SqlDbType.NVarChar, country.FlagEmoji, 5),
                BaseRepository.Param("@FormOfGovernment", SqlDbType.NVarChar, country.FormOfGovernment, 50),
                BaseRepository.Param("@GovernmentLevel", SqlDbType.Int, country.GovernmentLevel),
                BaseRepository.Param("@GovernmentXP", SqlDbType.Int, country.GovernmentXP),
                BaseRepository.Param("@Stability", SqlDbType.Real, country.Stability),
                BaseRepository.Param("@PopularLoyalty", SqlDbType.Real, country.PopularLoyalty),
                BaseRepository.Param("@EliteLoyalty", SqlDbType.Real, country.EliteLoyalty),
                BaseRepository.Param("@Corruption", SqlDbType.Real, country.Corruption),
                BaseRepository.Param("@Legitimacy", SqlDbType.Real, country.Legitimacy),
                BaseRepository.Param("@Control", SqlDbType.Real, country.Control),
                BaseRepository.Param("@Inflation", SqlDbType.Real, country.Inflation),
                BaseRepository.Param("@Treasury", SqlDbType.BigInt, country.Treasury),
                BaseRepository.Param("@TaxLevel", SqlDbType.Int, country.TaxLevel),
                DecimalParam("@IncomePerTurn", country.IncomePerTurn, 28, 2),
                DecimalParam("@ExpensesPerTurn", country.ExpensesPerTurn, 28, 2),
                DecimalParam("@CorruptionLosses", country.CorruptionLosses, 28, 2),
                BaseRepository.Param("@WorkRate", SqlDbType.Real, country.WorkRate),
                BaseRepository.Param("@AvailableWorkers", SqlDbType.BigInt, country.AvailableWorkers),
                BaseRepository.Param("@UnemploymentRate", SqlDbType.Real, country.UnemploymentRate),
                BaseRepository.Param("@WarState", SqlDbType.Bit, country.WarState),
                BaseRepository.Param("@SanctionsLevel", SqlDbType.Int, country.SanctionsLevel),
                BaseRepository.Param("@Population", SqlDbType.BigInt, country.Population),
                BaseRepository.Param("@PopulationGrowth", SqlDbType.Real, country.PopulationGrowth),
                BaseRepository.Param("@Happiness", SqlDbType.Real, country.Happiness),
                BaseRepository.Param("@Unrest", SqlDbType.Real, country.Unrest),
                BaseRepository.Param("@Refugees", SqlDbType.BigInt, country.Refugees),
                BaseRepository.Param("@NobilityInfluence", SqlDbType.Real, country.NobilityInfluence),
                BaseRepository.Param("@MerchantsInfluence", SqlDbType.Real, country.MerchantsInfluence),
                BaseRepository.Param("@ClergyInfluence", SqlDbType.Real, country.ClergyInfluence),
                BaseRepository.Param("@MilitaryInfluence", SqlDbType.Real, country.MilitaryInfluence),
                BaseRepository.Param("@MagesInfluence", SqlDbType.Real, country.MagesInfluence),
                BaseRepository.Param("@PeasantsInfluence", SqlDbType.Real, country.PeasantsInfluence),
                BaseRepository.Param("@ExternalInfluence", SqlDbType.Real, country.ExternalInfluence),
                BaseRepository.Param("@HasParliament", SqlDbType.Bit, country.HasParliament),
                BaseRepository.Param("@ParliamentSeats", SqlDbType.Int, country.ParliamentSeats),
                BaseRepository.Param("@ParliamentFractions", SqlDbType.Int, country.ParliamentFractions),
                BaseRepository.Param("@ParliamentSupport", SqlDbType.Real, country.ParliamentSupport),
                BaseRepository.Param("@OppositionStrength", SqlDbType.Real, country.OppositionStrength),
                BaseRepository.Param("@ActiveLawsCount", SqlDbType.Int, country.ActiveLawsCount),
                BaseRepository.Param("@MaxLawsAllowed", SqlDbType.Int, country.MaxLawsAllowed),
                BaseRepository.Param("@EmergencyLawActive", SqlDbType.Bit, country.EmergencyLawActive),
                BaseRepository.Param("@ArmySize", SqlDbType.Int, country.ArmySize),
                BaseRepository.Param("@ArmyLoyalty", SqlDbType.Real, country.ArmyLoyalty),
                BaseRepository.Param("@ArmyMorale", SqlDbType.Real, country.ArmyMorale),
                DecimalParam("@MilitaryUpkeep", country.MilitaryUpkeep, 28, 2),
                BaseRepository.Param("@MartialLaw", SqlDbType.Bit, country.MartialLaw),
                BaseRepository.Param("@MagicInfluence", SqlDbType.Real, country.MagicInfluence),
                BaseRepository.Param("@ManaStability", SqlDbType.Real, country.ManaStability),
                BaseRepository.Param("@ActiveManaZones", SqlDbType.Int, country.ActiveManaZones),
                BaseRepository.Param("@ArcaneRisk", SqlDbType.Real, country.ArcaneRisk),
                BaseRepository.Param("@Cities", SqlDbType.Int, country.Cities),
                BaseRepository.Param("@Villages", SqlDbType.Int, country.Villages),
                BaseRepository.Param("@InfrastructureLevel", SqlDbType.Real, country.InfrastructureLevel),
                BaseRepository.Param("@LogisticsEfficiency", SqlDbType.Real, country.LogisticsEfficiency),
                BaseRepository.Param("@ResearchLevel", SqlDbType.Int, country.ResearchLevel),
                BaseRepository.Param("@ResearchSpeed", SqlDbType.Real, country.ResearchSpeed),
                BaseRepository.Param("@CompletedResearches", SqlDbType.Int, country.CompletedResearches),
                BaseRepository.Param("@CoupRisk", SqlDbType.Real, country.CoupRisk),
                BaseRepository.Param("@RebellionRisk", SqlDbType.Real, country.RebellionRisk),
                BaseRepository.Param("@AssassinationRisk", SqlDbType.Real, country.AssassinationRisk),
                BaseRepository.Param("@Allies", SqlDbType.Int, country.Allies),
                BaseRepository.Param("@Enemies", SqlDbType.Int, country.Enemies),
                BaseRepository.Param("@InternationalReputation", SqlDbType.Real, country.InternationalReputation),
                BaseRepository.Param("@IntelLevel", SqlDbType.Real, country.IntelLevel),
                BaseRepository.Param("@CounterIntelLevel", SqlDbType.Real, country.CounterIntelLevel),
                BaseRepository.Param("@LastUpdate", SqlDbType.DateTime2, (object?)country.LastUpdate ?? DBNull.Value),
                BaseRepository.Param("@TurnsSurvived", SqlDbType.Int, country.TurnsSurvived),
                BaseRepository.Param("@IsInCrisis", SqlDbType.Bit, country.IsInCrisis),
                BaseRepository.Param("@IsInGoldenAge", SqlDbType.Bit, country.IsInGoldenAge),
                BaseRepository.Param("@IsCollapsed", SqlDbType.Bit, country.IsCollapsed),
                BaseRepository.Param("@CreatedAt", SqlDbType.DateTime2, country.CreatedAt),
                BaseRepository.Param("@CountryId", SqlDbType.Int, country.CountryId)
            );

            return rows > 0 ? "Країна оновлена." : "Країну не знайдено або нічого не змінено.";
        }

        public static string DeleteCountry(int countryId)
        {
            const string sql = @"
BEGIN TRY
    BEGIN TRAN;
    SET NOCOUNT ON;

    DECLARE @PartyIds TABLE (Id INT PRIMARY KEY);
    INSERT INTO @PartyIds (Id)
    SELECT PartyId FROM Parties WHERE CountryId = @CountryId;

    DECLARE @DeputyIds TABLE (Id INT PRIMARY KEY);
    INSERT INTO @DeputyIds (Id)
    SELECT DeputyId FROM Deputies WHERE CountryId = @CountryId;

    DECLARE @LawIds TABLE (Id INT PRIMARY KEY);
    INSERT INTO @LawIds (Id)
    SELECT LawId FROM Laws WHERE CountryId = @CountryId;

    DECLARE @SessionIds TABLE (Id INT PRIMARY KEY);
    INSERT INTO @SessionIds (Id)
    SELECT SessionId FROM ParliamentSessions WHERE CountryId = @CountryId;

    DELETE FROM Votes WHERE DeputyId IN (SELECT Id FROM @DeputyIds)
        OR LawId IN (SELECT Id FROM @LawIds);
    DELETE FROM SessionAttendance WHERE DeputyId IN (SELECT Id FROM @DeputyIds)
        OR SessionId IN (SELECT Id FROM @SessionIds);
    DELETE FROM PartyMembers WHERE DeputyId IN (SELECT Id FROM @DeputyIds)
        OR PartyId IN (SELECT Id FROM @PartyIds);

    DELETE FROM Deputies WHERE CountryId = @CountryId;
    DELETE FROM Parties WHERE CountryId = @CountryId;
    DELETE FROM ParliamentSessions WHERE CountryId = @CountryId;
    DELETE FROM Parliament WHERE CountryId = @CountryId;
    DELETE FROM Laws WHERE CountryId = @CountryId;
    IF OBJECT_ID('EconomyAidTranches', 'U') IS NOT NULL
        DELETE FROM EconomyAidTranches WHERE FromCountryId = @CountryId OR ToCountryId = @CountryId;
    IF OBJECT_ID('EconomyCities', 'U') IS NOT NULL
        DELETE FROM EconomyCities WHERE CountryId = @CountryId;
    DELETE FROM Countries WHERE CountryId = @CountryId;

    COMMIT TRAN;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    THROW;
END CATCH";

            int rows = BaseRepository.ExecuteNonQuery(sql,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId));
            return rows > 0 ? "Країна видалена." : "Країну не знайдено.";
        }

        public static string MergeCountries(int targetCountryId, int sourceCountryId)
        {
            if (targetCountryId <= 0 || sourceCountryId <= 0)
                return "Невірні Id країн.";
            if (targetCountryId == sourceCountryId)
                return "Потрібні дві різні країни.";

            var target = GetCountryById(targetCountryId);
            if (target == null)
                return $"Країну #{targetCountryId} не знайдено.";
            var source = GetCountryById(sourceCountryId);
            if (source == null)
                return $"Країну #{sourceCountryId} не знайдено.";

            long mergedTreasury = SafeAddLong(target.Treasury, source.Treasury);
            long mergedPopulation = SafeAddLong(target.Population, source.Population);
            long mergedRefugees = SafeAddLong(target.Refugees, source.Refugees);
            long mergedWorkers = SafeAddLong(target.AvailableWorkers, source.AvailableWorkers);
            int mergedArmy = SafeAddInt(target.ArmySize, source.ArmySize);
            int mergedCities = SafeAddInt(target.Cities, source.Cities);
            int mergedVillages = SafeAddInt(target.Villages, source.Villages);
            int mergedTurns = Math.Max(target.TurnsSurvived, source.TurnsSurvived);
            DateTime? mergedLastUpdate = target.LastUpdate;
            if (source.LastUpdate.HasValue && (!mergedLastUpdate.HasValue || source.LastUpdate.Value > mergedLastUpdate.Value))
                mergedLastUpdate = source.LastUpdate;

            const string sql = @"
BEGIN TRY
    BEGIN TRAN;
    SET NOCOUNT ON;

    IF OBJECT_ID('EconomyBudget', 'U') IS NOT NULL
    BEGIN
        IF EXISTS (SELECT 1 FROM EconomyBudget WHERE CountryId = @SourceCountryId)
        BEGIN
            IF EXISTS (SELECT 1 FROM EconomyBudget WHERE CountryId = @TargetCountryId)
                DELETE FROM EconomyBudget WHERE CountryId = @SourceCountryId;
            ELSE
                UPDATE EconomyBudget SET CountryId = @TargetCountryId WHERE CountryId = @SourceCountryId;
        END
    END

    IF OBJECT_ID('EconomyCurrencies', 'U') IS NOT NULL
    BEGIN
        IF EXISTS (SELECT 1 FROM EconomyCurrencies WHERE CountryId = @SourceCountryId)
        BEGIN
            IF EXISTS (SELECT 1 FROM EconomyCurrencies WHERE CountryId = @TargetCountryId)
                DELETE FROM EconomyCurrencies WHERE CountryId = @SourceCountryId;
            ELSE
                UPDATE EconomyCurrencies SET CountryId = @TargetCountryId WHERE CountryId = @SourceCountryId;
        END
    END

    IF OBJECT_ID('EconomyAidTranches', 'U') IS NOT NULL
    BEGIN
        UPDATE EconomyAidTranches
        SET FromCountryId = @TargetCountryId
        WHERE FromCountryId = @SourceCountryId;

        UPDATE EconomyAidTranches
        SET ToCountryId = @TargetCountryId
        WHERE ToCountryId = @SourceCountryId;

        DELETE FROM EconomyAidTranches
        WHERE FromCountryId = ToCountryId;
    END

    IF OBJECT_ID('Parliament', 'U') IS NOT NULL
    BEGIN
        IF EXISTS (SELECT 1 FROM Parliament WHERE CountryId = @SourceCountryId)
        BEGIN
            IF EXISTS (SELECT 1 FROM Parliament WHERE CountryId = @TargetCountryId)
                DELETE FROM Parliament WHERE CountryId = @SourceCountryId;
            ELSE
                UPDATE Parliament SET CountryId = @TargetCountryId WHERE CountryId = @SourceCountryId;
        END
    END

    IF OBJECT_ID('ResearchCompleted', 'U') IS NOT NULL
    BEGIN
        INSERT INTO ResearchCompleted (CountryId, ResearchId, CompletedAt)
        SELECT @TargetCountryId, rc.ResearchId, rc.CompletedAt
        FROM ResearchCompleted rc
        WHERE rc.CountryId = @SourceCountryId
          AND NOT EXISTS (
              SELECT 1 FROM ResearchCompleted t
              WHERE t.CountryId = @TargetCountryId AND t.ResearchId = rc.ResearchId
          );

        DELETE FROM ResearchCompleted WHERE CountryId = @SourceCountryId;
    END

    IF OBJECT_ID('ResearchProgress', 'U') IS NOT NULL
    BEGIN
        MERGE ResearchProgress AS tgt
        USING (
            SELECT ResearchId, Progress, StartedAt, IsCompleted
            FROM ResearchProgress
            WHERE CountryId = @SourceCountryId
        ) src
        ON tgt.CountryId = @TargetCountryId AND tgt.ResearchId = src.ResearchId
        WHEN MATCHED THEN UPDATE SET
            Progress = CASE WHEN tgt.Progress >= src.Progress THEN tgt.Progress ELSE src.Progress END,
            StartedAt = CASE WHEN tgt.StartedAt <= src.StartedAt THEN tgt.StartedAt ELSE src.StartedAt END,
            IsCompleted = CASE WHEN tgt.IsCompleted = 1 OR src.IsCompleted = 1 THEN 1 ELSE 0 END
        WHEN NOT MATCHED THEN
            INSERT (CountryId, ResearchId, Progress, StartedAt, IsCompleted)
            VALUES (@TargetCountryId, src.ResearchId, src.Progress, src.StartedAt, src.IsCompleted);

        DELETE FROM ResearchProgress WHERE CountryId = @SourceCountryId;
    END

    IF OBJECT_ID('EconomyCrisisActionsLog', 'U') IS NOT NULL
    BEGIN
        MERGE EconomyCrisisActionsLog AS tgt
        USING (
            SELECT ActionType, LastAppliedTurn, LastAppliedAt
            FROM EconomyCrisisActionsLog
            WHERE CountryId = @SourceCountryId
        ) src
        ON tgt.CountryId = @TargetCountryId AND tgt.ActionType = src.ActionType
        WHEN MATCHED THEN UPDATE SET
            LastAppliedTurn = CASE WHEN tgt.LastAppliedTurn >= src.LastAppliedTurn THEN tgt.LastAppliedTurn ELSE src.LastAppliedTurn END,
            LastAppliedAt = CASE WHEN tgt.LastAppliedAt >= src.LastAppliedAt THEN tgt.LastAppliedAt ELSE src.LastAppliedAt END
        WHEN NOT MATCHED THEN
            INSERT (CountryId, ActionType, LastAppliedTurn, LastAppliedAt)
            VALUES (@TargetCountryId, src.ActionType, src.LastAppliedTurn, src.LastAppliedAt);

        DELETE FROM EconomyCrisisActionsLog WHERE CountryId = @SourceCountryId;
    END

    IF OBJECT_ID('EconomyCountrySanctions', 'U') IS NOT NULL
    BEGIN
        DELETE s
        FROM EconomyCountrySanctions s
        WHERE s.SourceCountryId = @SourceCountryId
          AND EXISTS (
              SELECT 1 FROM EconomyCountrySanctions t
              WHERE t.SourceCountryId = @TargetCountryId AND t.TargetCountryId = s.TargetCountryId
          );

        UPDATE EconomyCountrySanctions
        SET SourceCountryId = @TargetCountryId
        WHERE SourceCountryId = @SourceCountryId;

        DELETE s
        FROM EconomyCountrySanctions s
        WHERE s.TargetCountryId = @SourceCountryId
          AND EXISTS (
              SELECT 1 FROM EconomyCountrySanctions t
              WHERE t.SourceCountryId = s.SourceCountryId AND t.TargetCountryId = @TargetCountryId
          );

        UPDATE EconomyCountrySanctions
        SET TargetCountryId = @TargetCountryId
        WHERE TargetCountryId = @SourceCountryId;

        DELETE FROM EconomyCountrySanctions
        WHERE SourceCountryId = @TargetCountryId AND TargetCountryId = @TargetCountryId;
    END

    IF OBJECT_ID('EconomyCountryProductSanctions', 'U') IS NOT NULL
    BEGIN
        DELETE s
        FROM EconomyCountryProductSanctions s
        WHERE s.SourceCountryId = @SourceCountryId
          AND EXISTS (
              SELECT 1 FROM EconomyCountryProductSanctions t
              WHERE t.SourceCountryId = @TargetCountryId AND t.TargetCountryId = s.TargetCountryId AND t.ResourceType = s.ResourceType
          );

        UPDATE EconomyCountryProductSanctions
        SET SourceCountryId = @TargetCountryId
        WHERE SourceCountryId = @SourceCountryId;

        DELETE s
        FROM EconomyCountryProductSanctions s
        WHERE s.TargetCountryId = @SourceCountryId
          AND EXISTS (
              SELECT 1 FROM EconomyCountryProductSanctions t
              WHERE t.SourceCountryId = s.SourceCountryId AND t.TargetCountryId = @TargetCountryId AND t.ResourceType = s.ResourceType
          );

        UPDATE EconomyCountryProductSanctions
        SET TargetCountryId = @TargetCountryId
        WHERE TargetCountryId = @SourceCountryId;

        DELETE FROM EconomyCountryProductSanctions
        WHERE SourceCountryId = @TargetCountryId AND TargetCountryId = @TargetCountryId;
    END

    IF OBJECT_ID('DiplomacyRelations', 'U') IS NOT NULL
    BEGIN
        DELETE dr
        FROM DiplomacyRelations dr
        WHERE dr.CountryA = @SourceCountryId
          AND EXISTS (
              SELECT 1 FROM DiplomacyRelations t
              WHERE t.CountryA = @TargetCountryId AND t.CountryB = dr.CountryB
          );

        DELETE dr
        FROM DiplomacyRelations dr
        WHERE dr.CountryB = @SourceCountryId
          AND EXISTS (
              SELECT 1 FROM DiplomacyRelations t
              WHERE t.CountryA = dr.CountryA AND t.CountryB = @TargetCountryId
          );

        DELETE FROM DiplomacyRelations
        WHERE (CountryA = @SourceCountryId AND CountryB = @TargetCountryId)
           OR (CountryA = @TargetCountryId AND CountryB = @SourceCountryId);

        UPDATE DiplomacyRelations SET CountryA = @TargetCountryId WHERE CountryA = @SourceCountryId;
        UPDATE DiplomacyRelations SET CountryB = @TargetCountryId WHERE CountryB = @SourceCountryId;

        DELETE FROM DiplomacyRelations WHERE CountryA = @TargetCountryId AND CountryB = @TargetCountryId;
    END

    IF OBJECT_ID('EconomyBuildings', 'U') IS NOT NULL
        UPDATE EconomyBuildings SET CountryId = @TargetCountryId WHERE CountryId = @SourceCountryId;
    IF OBJECT_ID('EconomyCities', 'U') IS NOT NULL
    BEGIN
        INSERT INTO EconomyCities (CountryId, CityName)
        SELECT @TargetCountryId, src.CityName
        FROM EconomyCities src
        WHERE src.CountryId = @SourceCountryId
          AND NOT EXISTS (
              SELECT 1 FROM EconomyCities t
              WHERE t.CountryId = @TargetCountryId AND t.CityName = src.CityName
          );

        DELETE FROM EconomyCities WHERE CountryId = @SourceCountryId;

        IF OBJECT_ID('EconomyBuildings', 'U') IS NOT NULL AND COL_LENGTH('EconomyBuildings', 'CityName') IS NOT NULL
        BEGIN
            INSERT INTO EconomyCities (CountryId, CityName)
            SELECT DISTINCT @TargetCountryId, LTRIM(RTRIM(eb.CityName))
            FROM EconomyBuildings eb
            WHERE eb.CountryId = @TargetCountryId
              AND LTRIM(RTRIM(ISNULL(eb.CityName, N''))) <> N''
              AND NOT EXISTS (
                  SELECT 1 FROM EconomyCities t
                  WHERE t.CountryId = @TargetCountryId AND t.CityName = LTRIM(RTRIM(eb.CityName))
              );
        END

        IF NOT EXISTS (
            SELECT 1 FROM EconomyCities
            WHERE CountryId = @TargetCountryId AND CityName = N'Столиця'
        )
            INSERT INTO EconomyCities (CountryId, CityName) VALUES (@TargetCountryId, N'Столиця');
    END
    IF OBJECT_ID('EconomyFestivals', 'U') IS NOT NULL
        UPDATE EconomyFestivals SET CountryId = @TargetCountryId WHERE CountryId = @SourceCountryId;
    IF OBJECT_ID('EconomyTradeAgreements', 'U') IS NOT NULL
    BEGIN
        UPDATE EconomyTradeAgreements SET CountryId = @TargetCountryId WHERE CountryId = @SourceCountryId;
        UPDATE EconomyTradeAgreements SET PartnerCountryId = @TargetCountryId WHERE PartnerCountryId = @SourceCountryId;
        DELETE FROM EconomyTradeAgreements WHERE CountryId = @TargetCountryId AND PartnerCountryId = @TargetCountryId;
    END
    IF OBJECT_ID('EconomyMoneyLog', 'U') IS NOT NULL
        UPDATE EconomyMoneyLog SET CountryId = @TargetCountryId WHERE CountryId = @SourceCountryId;
    IF OBJECT_ID('EconomyTurnHistory', 'U') IS NOT NULL
        UPDATE EconomyTurnHistory SET CountryId = @TargetCountryId WHERE CountryId = @SourceCountryId;
    IF OBJECT_ID('EconomyCountryEffectsLog', 'U') IS NOT NULL
        UPDATE EconomyCountryEffectsLog SET CountryId = @TargetCountryId WHERE CountryId = @SourceCountryId;
    IF OBJECT_ID('MilitaryUnits', 'U') IS NOT NULL
        UPDATE MilitaryUnits SET CountryId = @TargetCountryId WHERE CountryId = @SourceCountryId;
    IF OBJECT_ID('VassalMilitaryObligations', 'U') IS NOT NULL
        UPDATE VassalMilitaryObligations SET CountryId = @TargetCountryId WHERE CountryId = @SourceCountryId;

    IF OBJECT_ID('DiplomacyActionsLog', 'U') IS NOT NULL
    BEGIN
        UPDATE DiplomacyActionsLog SET CountryId = @TargetCountryId WHERE CountryId = @SourceCountryId;
        UPDATE DiplomacyActionsLog SET TargetCountryId = @TargetCountryId WHERE TargetCountryId = @SourceCountryId;
    END

    IF OBJECT_ID('AdminActionsLog', 'U') IS NOT NULL
        UPDATE AdminActionsLog SET CountryId = @TargetCountryId WHERE CountryId = @SourceCountryId;

    IF OBJECT_ID('Parties', 'U') IS NOT NULL
        UPDATE Parties SET CountryId = @TargetCountryId WHERE CountryId = @SourceCountryId;
    IF OBJECT_ID('Deputies', 'U') IS NOT NULL
        UPDATE Deputies SET CountryId = @TargetCountryId WHERE CountryId = @SourceCountryId;
    IF OBJECT_ID('Laws', 'U') IS NOT NULL
        UPDATE Laws SET CountryId = @TargetCountryId WHERE CountryId = @SourceCountryId;
    IF OBJECT_ID('ParliamentSessions', 'U') IS NOT NULL
        UPDATE ParliamentSessions SET CountryId = @TargetCountryId WHERE CountryId = @SourceCountryId;

    IF OBJECT_ID('Characters', 'U') IS NOT NULL
        UPDATE Characters SET CountryId = @TargetCountryId WHERE CountryId = @SourceCountryId;

    UPDATE Countries
    SET Treasury = @Treasury,
        Population = @Population,
        Refugees = @Refugees,
        AvailableWorkers = @AvailableWorkers,
        ArmySize = @ArmySize,
        Cities = CASE
            WHEN OBJECT_ID('EconomyCities', 'U') IS NOT NULL THEN
                (SELECT CASE WHEN COUNT(1) > 0 THEN COUNT(1) ELSE 1 END FROM EconomyCities WHERE CountryId = @TargetCountryId)
            ELSE @Cities
        END,
        Villages = @Villages,
        TurnsSurvived = @TurnsSurvived,
        LastUpdate = @LastUpdate
    WHERE CountryId = @TargetCountryId;

    DELETE FROM Countries WHERE CountryId = @SourceCountryId;

    COMMIT TRAN;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    THROW;
END CATCH";

            BaseRepository.ExecuteNonQuery(sql,
                BaseRepository.Param("@TargetCountryId", SqlDbType.Int, targetCountryId),
                BaseRepository.Param("@SourceCountryId", SqlDbType.Int, sourceCountryId),
                BaseRepository.Param("@Treasury", SqlDbType.BigInt, mergedTreasury),
                BaseRepository.Param("@Population", SqlDbType.BigInt, mergedPopulation),
                BaseRepository.Param("@Refugees", SqlDbType.BigInt, mergedRefugees),
                BaseRepository.Param("@AvailableWorkers", SqlDbType.BigInt, mergedWorkers),
                BaseRepository.Param("@ArmySize", SqlDbType.Int, mergedArmy),
                BaseRepository.Param("@Cities", SqlDbType.Int, mergedCities),
                BaseRepository.Param("@Villages", SqlDbType.Int, mergedVillages),
                BaseRepository.Param("@TurnsSurvived", SqlDbType.Int, mergedTurns),
                BaseRepository.Param("@LastUpdate", SqlDbType.DateTime2, (object?)mergedLastUpdate ?? DBNull.Value)
            );

            string targetName = string.IsNullOrWhiteSpace(target.Name) ? $"#{target.CountryId}" : target.Name;
            string sourceName = string.IsNullOrWhiteSpace(source.Name) ? $"#{source.CountryId}" : source.Name;
            return $"✅ Країни об'єднані: {sourceName} ➜ {targetName}.";
        }

        public static string DeleteAllCountriesAndData()
        {
            const string sql = @"
BEGIN TRY
    BEGIN TRAN;
    SET NOCOUNT ON;

    DELETE FROM Votes;
    DELETE FROM SessionAttendance;
    DELETE FROM PartyMembers;
    DELETE FROM Deputies;
    DELETE FROM Parties;
    DELETE FROM ParliamentSessions;
    DELETE FROM Parliament;
    DELETE FROM Laws;

    DELETE FROM EconomyTurnHistory;
    DELETE FROM EconomyMoneyLog;
    DELETE FROM EconomyTradeAgreements;
    DELETE FROM EconomyAidTranches;
    DELETE FROM EconomyFestivals;
    DELETE FROM EconomyBuildings;
    DELETE FROM EconomyCities;
    DELETE FROM EconomyBudget;
    DELETE FROM EconomyCurrencies;
    DELETE FROM EconomyCountryEffectsLog;
    DELETE FROM EconomyCrisisActionsLog;
    DELETE FROM EconomyCountryProductSanctions;
    DELETE FROM EconomyCountrySanctions;
    DELETE FROM MilitaryUnits;
    DELETE FROM VassalMilitaryObligations;
    DELETE FROM DiplomacyActionsLog;
    DELETE FROM DiplomacyRelations;
    DELETE FROM AdminActionsLog;
    DELETE FROM ResearchCompleted;
    DELETE FROM ResearchProgress;

    DELETE FROM Countries;

    COMMIT TRAN;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    THROW;
END CATCH";

            BaseRepository.ExecuteNonQuery(sql);
            return "Усі країни та їх дані видалені.";
        }

        public static CountryRecord? GetCountryById(int countryId)
        {
            const string q = @"
SELECT *
FROM Countries
WHERE CountryId = @CountryId";

            using var reader = BaseRepository.ExecuteReader(q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId));
            try
            {
                if (reader.Read())
                {
                    return Map(reader);
                }
            }
            finally
            {
                reader.Dispose();
            }

            return null;
        }

        public static List<CountryRecord> GetAllCountries()
        {
            const string q = @"
SELECT *
FROM Countries
ORDER BY CountryId";

            var list = new List<CountryRecord>();
            using var reader = BaseRepository.ExecuteReader(q);
            try
            {
                while (reader.Read())
                {
                    list.Add(Map(reader));
                }
            }
            finally
            {
                reader.Dispose();
            }
            return list;
        }

        public static string GetCountryNameById(int countryId)
        {
            const string q = @"
SELECT Name
FROM Countries
WHERE CountryId = @CountryId";

            var obj = BaseRepository.ExecuteScalar(q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId));

            if (obj == null || obj == DBNull.Value)
                return "Невідома країна";

            string? name = Convert.ToString(obj);
            return string.IsNullOrWhiteSpace(name) ? "Невідома країна" : name;
        }

        private static CountryRecord Map(IDataRecord reader)
        {
            return new CountryRecord
            {
                CountryId = reader.GetInt32(reader.GetOrdinal("CountryId")),
                Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? string.Empty : reader.GetString(reader.GetOrdinal("Name")),
                FlagEmoji = reader.IsDBNull(reader.GetOrdinal("FlagEmoji")) ? null : reader.GetString(reader.GetOrdinal("FlagEmoji")),
                FormOfGovernment = reader.IsDBNull(reader.GetOrdinal("FormOfGovernment")) ? string.Empty : reader.GetString(reader.GetOrdinal("FormOfGovernment")),
                GovernmentLevel = reader.GetInt32(reader.GetOrdinal("GovernmentLevel")),
                GovernmentXP = reader.GetInt32(reader.GetOrdinal("GovernmentXP")),
                Stability = Convert.ToSingle(reader["Stability"]),
                PopularLoyalty = Convert.ToSingle(reader["PopularLoyalty"]),
                EliteLoyalty = Convert.ToSingle(reader["EliteLoyalty"]),
                Corruption = Convert.ToSingle(reader["Corruption"]),
                Legitimacy = Convert.ToSingle(reader["Legitimacy"]),
                Control = Convert.ToSingle(reader["Control"]),
                Inflation = Convert.ToSingle(reader["Inflation"]),
                Treasury = reader.GetInt64(reader.GetOrdinal("Treasury")),
                TaxLevel = reader.GetInt32(reader.GetOrdinal("TaxLevel")),
                IncomePerTurn = reader.GetDecimal(reader.GetOrdinal("IncomePerTurn")),
                ExpensesPerTurn = reader.GetDecimal(reader.GetOrdinal("ExpensesPerTurn")),
                CorruptionLosses = reader.GetDecimal(reader.GetOrdinal("CorruptionLosses")),
                WorkRate = Convert.ToSingle(reader["WorkRate"]),
                AvailableWorkers = reader.GetInt64(reader.GetOrdinal("AvailableWorkers")),
                UnemploymentRate = Convert.ToSingle(reader["UnemploymentRate"]),
                WarState = reader.GetBoolean(reader.GetOrdinal("WarState")),
                SanctionsLevel = reader.GetInt32(reader.GetOrdinal("SanctionsLevel")),
                Population = reader.GetInt64(reader.GetOrdinal("Population")),
                PopulationGrowth = Convert.ToSingle(reader["PopulationGrowth"]),
                Happiness = Convert.ToSingle(reader["Happiness"]),
                Unrest = Convert.ToSingle(reader["Unrest"]),
                Refugees = reader.GetInt64(reader.GetOrdinal("Refugees")),
                NobilityInfluence = Convert.ToSingle(reader["NobilityInfluence"]),
                MerchantsInfluence = Convert.ToSingle(reader["MerchantsInfluence"]),
                ClergyInfluence = Convert.ToSingle(reader["ClergyInfluence"]),
                MilitaryInfluence = Convert.ToSingle(reader["MilitaryInfluence"]),
                MagesInfluence = Convert.ToSingle(reader["MagesInfluence"]),
                PeasantsInfluence = Convert.ToSingle(reader["PeasantsInfluence"]),
                ExternalInfluence = Convert.ToSingle(reader["ExternalInfluence"]),
                HasParliament = reader.GetBoolean(reader.GetOrdinal("HasParliament")),
                ParliamentSeats = reader.GetInt32(reader.GetOrdinal("ParliamentSeats")),
                ParliamentFractions = reader.GetInt32(reader.GetOrdinal("ParliamentFractions")),
                ParliamentSupport = Convert.ToSingle(reader["ParliamentSupport"]),
                OppositionStrength = Convert.ToSingle(reader["OppositionStrength"]),
                ActiveLawsCount = reader.GetInt32(reader.GetOrdinal("ActiveLawsCount")),
                MaxLawsAllowed = reader.GetInt32(reader.GetOrdinal("MaxLawsAllowed")),
                EmergencyLawActive = reader.GetBoolean(reader.GetOrdinal("EmergencyLawActive")),
                ArmySize = reader.GetInt32(reader.GetOrdinal("ArmySize")),
                ArmyLoyalty = Convert.ToSingle(reader["ArmyLoyalty"]),
                ArmyMorale = Convert.ToSingle(reader["ArmyMorale"]),
                MilitaryUpkeep = reader.GetDecimal(reader.GetOrdinal("MilitaryUpkeep")),
                MartialLaw = reader.GetBoolean(reader.GetOrdinal("MartialLaw")),
                MagicInfluence = Convert.ToSingle(reader["MagicInfluence"]),
                ManaStability = Convert.ToSingle(reader["ManaStability"]),
                ActiveManaZones = reader.GetInt32(reader.GetOrdinal("ActiveManaZones")),
                ArcaneRisk = Convert.ToSingle(reader["ArcaneRisk"]),
                Cities = reader.GetInt32(reader.GetOrdinal("Cities")),
                Villages = reader.GetInt32(reader.GetOrdinal("Villages")),
                InfrastructureLevel = Convert.ToSingle(reader["InfrastructureLevel"]),
                LogisticsEfficiency = Convert.ToSingle(reader["LogisticsEfficiency"]),
                ResearchLevel = reader.GetInt32(reader.GetOrdinal("ResearchLevel")),
                ResearchSpeed = Convert.ToSingle(reader["ResearchSpeed"]),
                CompletedResearches = reader.GetInt32(reader.GetOrdinal("CompletedResearches")),
                CoupRisk = Convert.ToSingle(reader["CoupRisk"]),
                RebellionRisk = Convert.ToSingle(reader["RebellionRisk"]),
                AssassinationRisk = Convert.ToSingle(reader["AssassinationRisk"]),
                Allies = reader.GetInt32(reader.GetOrdinal("Allies")),
                Enemies = reader.GetInt32(reader.GetOrdinal("Enemies")),
                InternationalReputation = Convert.ToSingle(reader["InternationalReputation"]),
                IntelLevel = reader.IsDBNull(reader.GetOrdinal("IntelLevel")) ? 0f : Convert.ToSingle(reader["IntelLevel"]),
                CounterIntelLevel = reader.IsDBNull(reader.GetOrdinal("CounterIntelLevel")) ? 0f : Convert.ToSingle(reader["CounterIntelLevel"]),
                LastUpdate = reader.IsDBNull(reader.GetOrdinal("LastUpdate")) ? null : reader.GetDateTime(reader.GetOrdinal("LastUpdate")),
                TurnsSurvived = reader.GetInt32(reader.GetOrdinal("TurnsSurvived")),
                IsInCrisis = reader.GetBoolean(reader.GetOrdinal("IsInCrisis")),
                IsInGoldenAge = reader.GetBoolean(reader.GetOrdinal("IsInGoldenAge")),
                IsCollapsed = reader.GetBoolean(reader.GetOrdinal("IsCollapsed")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            };
        }
    }
}






