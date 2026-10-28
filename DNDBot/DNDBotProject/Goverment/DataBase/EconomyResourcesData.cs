#nullable enable
using System;
using System.Collections.Generic;
using System.Data;

namespace DNDBot.DataBase
{
    public sealed class EconomyResourceRecord
    {
        public string ResourceType { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public bool IsTradeable { get; set; }
        public float DemandFactor { get; set; }
        public string? ParentResourceType { get; set; }
        public bool IgnoreProductionEfficiency { get; set; }
    }

    public static class EconomyResourcesData
    {
        static EconomyResourcesData()
        {
            BaseRepository.DBInitialization();
        }

        public static List<EconomyResourceRecord> GetAll()
        {
            const string q = @"
SELECT ResourceType, BasePrice, IsTradeable, DemandFactor, ParentResourceType, IgnoreProductionEfficiency
FROM EconomyResources";

            var list = new List<EconomyResourceRecord>();
            using var reader = BaseRepository.ExecuteReader(q);
            try
            {
                while (reader.Read())
                {
                    list.Add(new EconomyResourceRecord
                    {
                        ResourceType = reader.GetString(reader.GetOrdinal("ResourceType")),
                        BasePrice = reader.GetDecimal(reader.GetOrdinal("BasePrice")),
                        IsTradeable = reader.GetBoolean(reader.GetOrdinal("IsTradeable")),
                        DemandFactor = Convert.ToSingle(reader["DemandFactor"]),
                        ParentResourceType = reader["ParentResourceType"] == DBNull.Value
                            ? null
                            : Convert.ToString(reader["ParentResourceType"]),
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

        public static EconomyResourceRecord? GetByType(string resourceType)
        {
            const string q = @"
SELECT ResourceType, BasePrice, IsTradeable, DemandFactor, ParentResourceType, IgnoreProductionEfficiency
FROM EconomyResources
WHERE ResourceType = @ResourceType";

            using var reader = BaseRepository.ExecuteReader(q,
                BaseRepository.Param("@ResourceType", SqlDbType.NVarChar, resourceType, 30));
            try
            {
                if (!reader.Read())
                    return null;

                return new EconomyResourceRecord
                {
                    ResourceType = reader.GetString(reader.GetOrdinal("ResourceType")),
                    BasePrice = reader.GetDecimal(reader.GetOrdinal("BasePrice")),
                    IsTradeable = reader.GetBoolean(reader.GetOrdinal("IsTradeable")),
                    DemandFactor = Convert.ToSingle(reader["DemandFactor"]),
                    ParentResourceType = reader["ParentResourceType"] == DBNull.Value
                        ? null
                        : Convert.ToString(reader["ParentResourceType"]),
                    IgnoreProductionEfficiency = reader["IgnoreProductionEfficiency"] != DBNull.Value &&
                                                 Convert.ToBoolean(reader["IgnoreProductionEfficiency"])
                };
            }
            finally
            {
                reader.Dispose();
            }
        }

        public static bool AddResource(EconomyResourceRecord resource)
        {
            const string q = @"
INSERT INTO EconomyResources (ResourceType, BasePrice, IsTradeable, DemandFactor, ParentResourceType, IgnoreProductionEfficiency)
VALUES (@ResourceType, @BasePrice, @IsTradeable, @DemandFactor, @ParentResourceType, @IgnoreProductionEfficiency);";

            try
            {
                int rows = BaseRepository.ExecuteNonQuery(q,
                    BaseRepository.Param("@ResourceType", SqlDbType.NVarChar, resource.ResourceType, 30),
                    BaseRepository.Param("@BasePrice", SqlDbType.Decimal, resource.BasePrice),
                    BaseRepository.Param("@IsTradeable", SqlDbType.Bit, resource.IsTradeable),
                    BaseRepository.Param("@DemandFactor", SqlDbType.Real, resource.DemandFactor),
                    BaseRepository.Param("@ParentResourceType", SqlDbType.NVarChar, (object?)resource.ParentResourceType ?? DBNull.Value, 30),
                    BaseRepository.Param("@IgnoreProductionEfficiency", SqlDbType.Bit, resource.IgnoreProductionEfficiency)
                );
                return rows > 0;
            }
            catch
            {
                return false;
            }
        }

        public static bool UpdateResourceField(string resourceType, string fieldName, object value, SqlDbType sqlType)
        {
            string? column = fieldName switch
            {
                "ResourceType" => "ResourceType",
                "BasePrice" => "BasePrice",
                "IsTradeable" => "IsTradeable",
                "DemandFactor" => "DemandFactor",
                "ParentResourceType" => "ParentResourceType",
                "IgnoreProductionEfficiency" => "IgnoreProductionEfficiency",
                _ => null
            };
            if (column == null)
                return false;

            string q = $"UPDATE EconomyResources SET {column} = @Value WHERE ResourceType = @ResourceType";
            int rows = BaseRepository.ExecuteNonQuery(q,
                BaseRepository.Param("@Value", sqlType, value),
                BaseRepository.Param("@ResourceType", SqlDbType.NVarChar, resourceType, 30));
            return rows > 0;
        }

        public static bool DeleteResource(string resourceType)
        {
            const string q = "DELETE FROM EconomyResources WHERE ResourceType = @ResourceType";
            int rows = BaseRepository.ExecuteNonQuery(q,
                BaseRepository.Param("@ResourceType", SqlDbType.NVarChar, resourceType, 30));
            return rows > 0;
        }
    }
}

