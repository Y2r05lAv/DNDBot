#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DNDBot.DataBase
{
    public sealed class EconomyBuildingRecord
    {
        public const string DefaultCityName = "Столиця";

        public int BuildingId { get; set; }
        public int CountryId { get; set; }
        public string CityName { get; set; } = DefaultCityName;
        public string Type { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Level { get; set; }
        public int JobsProvided { get; set; }
        public decimal UpkeepCost { get; set; }
        public decimal BaseProduction { get; set; }
        public decimal BuildCost { get; set; }
        public int BuildTurns { get; set; }
        public string? ResourceType { get; set; }
        public string? InputResourceType { get; set; }
        public decimal InputAmount { get; set; }
        public bool StateOwned { get; set; }
        public float StabilityBonus { get; set; }
        public float CorruptionReduction { get; set; }
        public float ResearchBonus { get; set; }
        public float PopulationGrowthBonus { get; set; }
        public float FestivalBoost { get; set; }
        public int ArmyBonus { get; set; }
        public int? CompanyId { get; set; }
        public string BuildingStatus { get; set; } = "Active";
        public int InactiveTurns { get; set; }
        public int DegradationLevel { get; set; }
        public bool IgnoreProductionEfficiency { get; set; }
    }

    public static class EconomyBuildingsData
    {
        static EconomyBuildingsData()
        {
            BaseRepository.DBInitialization();
        }

        private static SqlParameter DecimalParam(string name, decimal value, byte precision = 18, byte scale = 2)
        {
            var p = new SqlParameter(name, SqlDbType.Decimal)
            {
                Precision = precision,
                Scale = scale,
                Value = value
            };
            return p;
        }

        public static List<EconomyBuildingRecord> GetByCountry(int countryId)
        {
            const string q = @"
SELECT BuildingId, CountryId, CityName, Type, Category, Level, JobsProvided, UpkeepCost, BaseProduction, BuildCost, BuildTurns, ResourceType, InputResourceType, InputAmount, StateOwned,
       StabilityBonus, CorruptionReduction, ResearchBonus, PopulationGrowthBonus, FestivalBoost, ArmyBonus,
       CompanyId, BuildingStatus, InactiveTurns, DegradationLevel, IgnoreProductionEfficiency
FROM EconomyBuildings
WHERE CountryId = @CountryId";

            var list = new List<EconomyBuildingRecord>();
            using var reader = BaseRepository.ExecuteReader(q, BaseRepository.Param("@CountryId", SqlDbType.Int, countryId));
            try
            {
                while (reader.Read())
                {
                    list.Add(new EconomyBuildingRecord
                    {
                        BuildingId = reader.GetInt32(reader.GetOrdinal("BuildingId")),
                        CountryId = reader.GetInt32(reader.GetOrdinal("CountryId")),
                        CityName = reader.IsDBNull(reader.GetOrdinal("CityName"))
                            ? EconomyBuildingRecord.DefaultCityName
                            : (reader.GetString(reader.GetOrdinal("CityName")).Trim().Length == 0
                                ? EconomyBuildingRecord.DefaultCityName
                                : reader.GetString(reader.GetOrdinal("CityName")).Trim()),
                        Type = reader.GetString(reader.GetOrdinal("Type")),
                        Category = reader.GetString(reader.GetOrdinal("Category")),
                        Level = reader.GetInt32(reader.GetOrdinal("Level")),
                        JobsProvided = reader.GetInt32(reader.GetOrdinal("JobsProvided")),
                        UpkeepCost = reader.GetDecimal(reader.GetOrdinal("UpkeepCost")),
                        BaseProduction = reader.GetDecimal(reader.GetOrdinal("BaseProduction")),
                        BuildCost = reader.GetDecimal(reader.GetOrdinal("BuildCost")),
                        BuildTurns = reader.GetInt32(reader.GetOrdinal("BuildTurns")),
                        ResourceType = reader.IsDBNull(reader.GetOrdinal("ResourceType")) ? null : reader.GetString(reader.GetOrdinal("ResourceType")),
                        InputResourceType = reader.IsDBNull(reader.GetOrdinal("InputResourceType")) ? null : reader.GetString(reader.GetOrdinal("InputResourceType")),
                        InputAmount = reader.IsDBNull(reader.GetOrdinal("InputAmount")) ? 0m : reader.GetDecimal(reader.GetOrdinal("InputAmount")),
                        StateOwned = reader.GetBoolean(reader.GetOrdinal("StateOwned")),
                        StabilityBonus = Convert.ToSingle(reader["StabilityBonus"]),
                        CorruptionReduction = Convert.ToSingle(reader["CorruptionReduction"]),
                        ResearchBonus = Convert.ToSingle(reader["ResearchBonus"]),
                        PopulationGrowthBonus = Convert.ToSingle(reader["PopulationGrowthBonus"]),
                        FestivalBoost = Convert.ToSingle(reader["FestivalBoost"]),
                        ArmyBonus = reader.GetInt32(reader.GetOrdinal("ArmyBonus")),
                        CompanyId = reader.IsDBNull(reader.GetOrdinal("CompanyId")) ? null : reader.GetInt32(reader.GetOrdinal("CompanyId")),
                        BuildingStatus = reader.IsDBNull(reader.GetOrdinal("BuildingStatus")) ? "Active" : reader.GetString(reader.GetOrdinal("BuildingStatus")),
                        InactiveTurns = reader.IsDBNull(reader.GetOrdinal("InactiveTurns")) ? 0 : reader.GetInt32(reader.GetOrdinal("InactiveTurns")),
                        DegradationLevel = reader.IsDBNull(reader.GetOrdinal("DegradationLevel")) ? 0 : reader.GetInt32(reader.GetOrdinal("DegradationLevel")),
                        IgnoreProductionEfficiency = reader["IgnoreProductionEfficiency"] != DBNull.Value &&
                                                     Convert.ToBoolean(reader["IgnoreProductionEfficiency"])
                    });
                }
            }
            finally
            {
                reader.Dispose();
            }

            return list;
        }

        public static int AddBuilding(EconomyBuildingRecord building)
        {
            const string q = @"
INSERT INTO EconomyBuildings (
    CountryId, CityName, Type, Category, Level, JobsProvided, UpkeepCost, BaseProduction, BuildCost, BuildTurns, ResourceType, InputResourceType, InputAmount, StateOwned,
    StabilityBonus, CorruptionReduction, ResearchBonus, PopulationGrowthBonus, FestivalBoost, ArmyBonus,
    CompanyId, BuildingStatus, InactiveTurns, DegradationLevel, IgnoreProductionEfficiency
)
VALUES (
    @CountryId, @CityName, @Type, @Category, @Level, @JobsProvided, @UpkeepCost, @BaseProduction, @BuildCost, @BuildTurns, @ResourceType, @InputResourceType, @InputAmount, @StateOwned,
    @StabilityBonus, @CorruptionReduction, @ResearchBonus, @PopulationGrowthBonus, @FestivalBoost, @ArmyBonus,
    @CompanyId, @BuildingStatus, @InactiveTurns, @DegradationLevel, @IgnoreProductionEfficiency
);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            string cityName = string.IsNullOrWhiteSpace(building.CityName)
                ? EconomyBuildingRecord.DefaultCityName
                : building.CityName.Trim();

            var idObj = BaseRepository.ExecuteScalar(q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, building.CountryId),
                BaseRepository.Param("@CityName", SqlDbType.NVarChar, cityName, 60),
                BaseRepository.Param("@Type", SqlDbType.NVarChar, building.Type, 50),
                BaseRepository.Param("@Category", SqlDbType.NVarChar, building.Category, 30),
                BaseRepository.Param("@Level", SqlDbType.Int, building.Level),
                BaseRepository.Param("@JobsProvided", SqlDbType.Int, building.JobsProvided),
                DecimalParam("@UpkeepCost", building.UpkeepCost),
                DecimalParam("@BaseProduction", building.BaseProduction),
                DecimalParam("@BuildCost", building.BuildCost),
                BaseRepository.Param("@BuildTurns", SqlDbType.Int, building.BuildTurns),
                BaseRepository.Param("@ResourceType", SqlDbType.NVarChar, (object?)building.ResourceType ?? DBNull.Value, 30),
                BaseRepository.Param("@InputResourceType", SqlDbType.NVarChar, (object?)building.InputResourceType ?? DBNull.Value, 30),
                DecimalParam("@InputAmount", building.InputAmount),
                BaseRepository.Param("@StateOwned", SqlDbType.Bit, building.StateOwned),
                BaseRepository.Param("@StabilityBonus", SqlDbType.Real, building.StabilityBonus),
                BaseRepository.Param("@CorruptionReduction", SqlDbType.Real, building.CorruptionReduction),
                BaseRepository.Param("@ResearchBonus", SqlDbType.Real, building.ResearchBonus),
                BaseRepository.Param("@PopulationGrowthBonus", SqlDbType.Real, building.PopulationGrowthBonus),
                BaseRepository.Param("@FestivalBoost", SqlDbType.Real, building.FestivalBoost),
                BaseRepository.Param("@ArmyBonus", SqlDbType.Int, building.ArmyBonus),
                BaseRepository.Param("@CompanyId", SqlDbType.Int, (object?)building.CompanyId ?? DBNull.Value),
                BaseRepository.Param("@BuildingStatus", SqlDbType.NVarChar, building.BuildingStatus, 20),
                BaseRepository.Param("@InactiveTurns", SqlDbType.Int, building.InactiveTurns),
                BaseRepository.Param("@DegradationLevel", SqlDbType.Int, building.DegradationLevel),
                BaseRepository.Param("@IgnoreProductionEfficiency", SqlDbType.Bit, building.IgnoreProductionEfficiency)
            );

            if (idObj == null || idObj == DBNull.Value)
                return 0;
            return Convert.ToInt32(idObj);
        }

        public static bool DeleteBuilding(int buildingId, int countryId)
        {
            const string q = "DELETE FROM EconomyBuildings WHERE BuildingId = @BuildingId AND CountryId = @CountryId";
            int rows = BaseRepository.ExecuteNonQuery(q,
                BaseRepository.Param("@BuildingId", SqlDbType.Int, buildingId),
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId));
            return rows > 0;
        }

        public static int MoveBuildingsToCity(int countryId, string fromCityName, string toCityName)
        {
            const string q = @"
UPDATE EconomyBuildings
SET CityName = @ToCityName
WHERE CountryId = @CountryId
  AND LTRIM(RTRIM(ISNULL(CityName, N''))) = @FromCityName;";

            string fromCity = string.IsNullOrWhiteSpace(fromCityName)
                ? EconomyBuildingRecord.DefaultCityName
                : fromCityName.Trim();
            string toCity = string.IsNullOrWhiteSpace(toCityName)
                ? EconomyBuildingRecord.DefaultCityName
                : toCityName.Trim();

            return BaseRepository.ExecuteNonQuery(
                q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId),
                BaseRepository.Param("@FromCityName", SqlDbType.NVarChar, fromCity, 60),
                BaseRepository.Param("@ToCityName", SqlDbType.NVarChar, toCity, 60));
        }

        public static bool UpdateBuildingField(int buildingId, int countryId, string fieldName, object value, SqlDbType type)
        {
            string column = fieldName switch
            {
                "Type" => "Type",
                "CityName" => "CityName",
                "Category" => "Category",
                "Level" => "Level",
                "JobsProvided" => "JobsProvided",
                "UpkeepCost" => "UpkeepCost",
                "BaseProduction" => "BaseProduction",
                "BuildCost" => "BuildCost",
                "BuildTurns" => "BuildTurns",
                "ResourceType" => "ResourceType",
                "InputResourceType" => "InputResourceType",
                "InputAmount" => "InputAmount",
                "StateOwned" => "StateOwned",
                "StabilityBonus" => "StabilityBonus",
                "CorruptionReduction" => "CorruptionReduction",
                "ResearchBonus" => "ResearchBonus",
                "PopulationGrowthBonus" => "PopulationGrowthBonus",
                "FestivalBoost" => "FestivalBoost",
                "ArmyBonus" => "ArmyBonus",
                "CompanyId" => "CompanyId",
                "BuildingStatus" => "BuildingStatus",
                "InactiveTurns" => "InactiveTurns",
                "DegradationLevel" => "DegradationLevel",
                "IgnoreProductionEfficiency" => "IgnoreProductionEfficiency",
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(column))
                return false;

            string q = $"UPDATE EconomyBuildings SET {column} = @Value WHERE BuildingId = @BuildingId AND CountryId = @CountryId";
            int rows = BaseRepository.ExecuteNonQuery(q,
                BaseRepository.Param("@BuildingId", SqlDbType.Int, buildingId),
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId),
                BaseRepository.Param("@Value", type, value)
            );
            return rows > 0;
        }

        public static bool UpdateBuildingLifecycle(int buildingId, int countryId, string status, int inactiveTurns, int degradationLevel, int? buildTurns = null)
        {
            string q = @"
UPDATE EconomyBuildings
SET BuildingStatus = @Status,
    InactiveTurns = @InactiveTurns,
    DegradationLevel = @DegradationLevel
    {0}
WHERE BuildingId = @BuildingId AND CountryId = @CountryId";

            string buildTurnsClause = buildTurns.HasValue ? ", BuildTurns = @BuildTurns" : string.Empty;
            q = string.Format(q, buildTurnsClause);

            var parameters = new List<SqlParameter>
            {
                BaseRepository.Param("@Status", SqlDbType.NVarChar, status, 20),
                BaseRepository.Param("@InactiveTurns", SqlDbType.Int, inactiveTurns),
                BaseRepository.Param("@DegradationLevel", SqlDbType.Int, degradationLevel),
                BaseRepository.Param("@BuildingId", SqlDbType.Int, buildingId),
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId)
            };
            if (buildTurns.HasValue)
                parameters.Add(BaseRepository.Param("@BuildTurns", SqlDbType.Int, buildTurns.Value));

            int rows = BaseRepository.ExecuteNonQuery(q, parameters.ToArray());
            return rows > 0;
        }

        public static bool UpdateBuildingAfterUpgrade(EconomyBuildingRecord building)
        {
            const string q = @"
UPDATE EconomyBuildings
SET Level = @Level,
    JobsProvided = @JobsProvided,
    UpkeepCost = @UpkeepCost,
    BaseProduction = @BaseProduction,
    BuildCost = @BuildCost,
    BuildTurns = @BuildTurns,
    StabilityBonus = @StabilityBonus,
    CorruptionReduction = @CorruptionReduction,
    ResearchBonus = @ResearchBonus,
    PopulationGrowthBonus = @PopulationGrowthBonus,
    FestivalBoost = @FestivalBoost,
    ArmyBonus = @ArmyBonus,
    BuildingStatus = @BuildingStatus,
    InactiveTurns = @InactiveTurns,
    DegradationLevel = @DegradationLevel
WHERE BuildingId = @BuildingId AND CountryId = @CountryId";

            int rows = BaseRepository.ExecuteNonQuery(q,
                BaseRepository.Param("@Level", SqlDbType.Int, building.Level),
                BaseRepository.Param("@JobsProvided", SqlDbType.Int, building.JobsProvided),
                DecimalParam("@UpkeepCost", building.UpkeepCost),
                DecimalParam("@BaseProduction", building.BaseProduction),
                DecimalParam("@BuildCost", building.BuildCost),
                BaseRepository.Param("@BuildTurns", SqlDbType.Int, building.BuildTurns),
                BaseRepository.Param("@StabilityBonus", SqlDbType.Real, building.StabilityBonus),
                BaseRepository.Param("@CorruptionReduction", SqlDbType.Real, building.CorruptionReduction),
                BaseRepository.Param("@ResearchBonus", SqlDbType.Real, building.ResearchBonus),
                BaseRepository.Param("@PopulationGrowthBonus", SqlDbType.Real, building.PopulationGrowthBonus),
                BaseRepository.Param("@FestivalBoost", SqlDbType.Real, building.FestivalBoost),
                BaseRepository.Param("@ArmyBonus", SqlDbType.Int, building.ArmyBonus),
                BaseRepository.Param("@BuildingStatus", SqlDbType.NVarChar, building.BuildingStatus, 20),
                BaseRepository.Param("@InactiveTurns", SqlDbType.Int, building.InactiveTurns),
                BaseRepository.Param("@DegradationLevel", SqlDbType.Int, building.DegradationLevel),
                BaseRepository.Param("@BuildingId", SqlDbType.Int, building.BuildingId),
                BaseRepository.Param("@CountryId", SqlDbType.Int, building.CountryId)
            );

            return rows > 0;
        }
    }
}

