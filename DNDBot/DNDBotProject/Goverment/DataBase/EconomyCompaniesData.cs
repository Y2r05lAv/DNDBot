#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DNDBot.DataBase
{
    public sealed class EconomyCompanyRecord
    {
        public int CompanyId { get; set; }
        public int CountryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? OwnerName { get; set; }
        public string ProductionType { get; set; } = "General";
        public int JobsOffered { get; set; }
        public decimal BaseSalary { get; set; }
        public decimal TaxRate { get; set; }
        public decimal Budget { get; set; }
        public bool IsStateCompany { get; set; }
        public float MilitaryProductionShare { get; set; }
        public string? InputResourceType { get; set; }
        public decimal InputAmount { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public static class EconomyCompaniesData
    {
        static EconomyCompaniesData()
        {
            BaseRepository.DBInitialization();
        }

        private static SqlParameter DecimalParam(string name, decimal value, byte precision, byte scale)
        {
            return new SqlParameter(name, SqlDbType.Decimal)
            {
                Precision = precision,
                Scale = scale,
                Value = value
            };
        }

        public static List<EconomyCompanyRecord> GetByCountry(int countryId)
        {
            const string q = @"
SELECT CompanyId, CountryId, Name, OwnerName, ProductionType, JobsOffered, BaseSalary, TaxRate, Budget,
       IsStateCompany, MilitaryProductionShare, InputResourceType, InputAmount, IsActive, CreatedAt, UpdatedAt
FROM EconomyCompanies
WHERE CountryId = @CountryId
ORDER BY CompanyId;";

            var list = new List<EconomyCompanyRecord>();
            try
            {
                using var reader = BaseRepository.ExecuteReader(
                    q,
                    BaseRepository.Param("@CountryId", SqlDbType.Int, countryId)
                );

                try
                {
                    while (reader.Read())
                    {
                        list.Add(new EconomyCompanyRecord
                        {
                            CompanyId = reader.GetInt32(reader.GetOrdinal("CompanyId")),
                            CountryId = reader.GetInt32(reader.GetOrdinal("CountryId")),
                            Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? string.Empty : reader.GetString(reader.GetOrdinal("Name")),
                            OwnerName = reader.IsDBNull(reader.GetOrdinal("OwnerName")) ? null : reader.GetString(reader.GetOrdinal("OwnerName")),
                            ProductionType = reader.IsDBNull(reader.GetOrdinal("ProductionType")) ? "General" : reader.GetString(reader.GetOrdinal("ProductionType")),
                            JobsOffered = reader.GetInt32(reader.GetOrdinal("JobsOffered")),
                            BaseSalary = reader.GetDecimal(reader.GetOrdinal("BaseSalary")),
                            TaxRate = reader.GetDecimal(reader.GetOrdinal("TaxRate")),
                            Budget = reader.GetDecimal(reader.GetOrdinal("Budget")),
                            IsStateCompany = reader.GetBoolean(reader.GetOrdinal("IsStateCompany")),
                            MilitaryProductionShare = Convert.ToSingle(reader["MilitaryProductionShare"]),
                            InputResourceType = reader.IsDBNull(reader.GetOrdinal("InputResourceType")) ? null : reader.GetString(reader.GetOrdinal("InputResourceType")),
                            InputAmount = reader.IsDBNull(reader.GetOrdinal("InputAmount")) ? 0m : reader.GetDecimal(reader.GetOrdinal("InputAmount")),
                            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
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
                return new List<EconomyCompanyRecord>();
            }

            return list;
        }

        public static EconomyCompanyRecord? GetById(int companyId, int countryId)
        {
            const string q = @"
SELECT CompanyId, CountryId, Name, OwnerName, ProductionType, JobsOffered, BaseSalary, TaxRate, Budget,
       IsStateCompany, MilitaryProductionShare, InputResourceType, InputAmount, IsActive, CreatedAt, UpdatedAt
FROM EconomyCompanies
WHERE CompanyId = @CompanyId AND CountryId = @CountryId;";

            try
            {
                using var reader = BaseRepository.ExecuteReader(
                    q,
                    BaseRepository.Param("@CompanyId", SqlDbType.Int, companyId),
                    BaseRepository.Param("@CountryId", SqlDbType.Int, countryId)
                );

                try
                {
                    if (reader.Read())
                    {
                        return new EconomyCompanyRecord
                        {
                            CompanyId = reader.GetInt32(reader.GetOrdinal("CompanyId")),
                            CountryId = reader.GetInt32(reader.GetOrdinal("CountryId")),
                            Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? string.Empty : reader.GetString(reader.GetOrdinal("Name")),
                            OwnerName = reader.IsDBNull(reader.GetOrdinal("OwnerName")) ? null : reader.GetString(reader.GetOrdinal("OwnerName")),
                            ProductionType = reader.IsDBNull(reader.GetOrdinal("ProductionType")) ? "General" : reader.GetString(reader.GetOrdinal("ProductionType")),
                            JobsOffered = reader.GetInt32(reader.GetOrdinal("JobsOffered")),
                            BaseSalary = reader.GetDecimal(reader.GetOrdinal("BaseSalary")),
                            TaxRate = reader.GetDecimal(reader.GetOrdinal("TaxRate")),
                            Budget = reader.GetDecimal(reader.GetOrdinal("Budget")),
                            IsStateCompany = reader.GetBoolean(reader.GetOrdinal("IsStateCompany")),
                            MilitaryProductionShare = Convert.ToSingle(reader["MilitaryProductionShare"]),
                            InputResourceType = reader.IsDBNull(reader.GetOrdinal("InputResourceType")) ? null : reader.GetString(reader.GetOrdinal("InputResourceType")),
                            InputAmount = reader.IsDBNull(reader.GetOrdinal("InputAmount")) ? 0m : reader.GetDecimal(reader.GetOrdinal("InputAmount")),
                            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                        };
                    }
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

        public static int AddCompany(EconomyCompanyRecord company)
        {
            const string q = @"
INSERT INTO EconomyCompanies (
    CountryId, Name, OwnerName, ProductionType, JobsOffered, BaseSalary, TaxRate, Budget,
    IsStateCompany, MilitaryProductionShare, InputResourceType, InputAmount, IsActive, CreatedAt, UpdatedAt
)
VALUES (
    @CountryId, @Name, @OwnerName, @ProductionType, @JobsOffered, @BaseSalary, @TaxRate, @Budget,
    @IsStateCompany, @MilitaryProductionShare, @InputResourceType, @InputAmount, @IsActive, @CreatedAt, @UpdatedAt
);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            try
            {
                var now = DateTime.UtcNow;
                var idObj = BaseRepository.ExecuteScalar(
                    q,
                    BaseRepository.Param("@CountryId", SqlDbType.Int, company.CountryId),
                    BaseRepository.Param("@Name", SqlDbType.NVarChar, company.Name, 100),
                    BaseRepository.Param("@OwnerName", SqlDbType.NVarChar, (object?)company.OwnerName ?? DBNull.Value, 100),
                    BaseRepository.Param("@ProductionType", SqlDbType.NVarChar, company.ProductionType, 50),
                    BaseRepository.Param("@JobsOffered", SqlDbType.Int, company.JobsOffered),
                    DecimalParam("@BaseSalary", company.BaseSalary, 18, 4),
                    DecimalParam("@TaxRate", company.TaxRate, 5, 4),
                    DecimalParam("@Budget", company.Budget, 18, 2),
                    BaseRepository.Param("@IsStateCompany", SqlDbType.Bit, company.IsStateCompany),
                    BaseRepository.Param("@MilitaryProductionShare", SqlDbType.Real, company.MilitaryProductionShare),
                    BaseRepository.Param("@InputResourceType", SqlDbType.NVarChar, (object?)company.InputResourceType ?? DBNull.Value, 30),
                    DecimalParam("@InputAmount", company.InputAmount, 18, 4),
                    BaseRepository.Param("@IsActive", SqlDbType.Bit, company.IsActive),
                    BaseRepository.Param("@CreatedAt", SqlDbType.DateTime2, company.CreatedAt == default ? now : company.CreatedAt),
                    BaseRepository.Param("@UpdatedAt", SqlDbType.DateTime2, now)
                );

                if (idObj == null || idObj == DBNull.Value)
                    return 0;
                return Convert.ToInt32(idObj);
            }
            catch
            {
                return 0;
            }
        }

        public static bool DeleteCompany(int companyId, int countryId)
        {
            const string q = "DELETE FROM EconomyCompanies WHERE CompanyId = @CompanyId AND CountryId = @CountryId;";
            try
            {
                int rows = BaseRepository.ExecuteNonQuery(
                    q,
                    BaseRepository.Param("@CompanyId", SqlDbType.Int, companyId),
                    BaseRepository.Param("@CountryId", SqlDbType.Int, countryId)
                );
                return rows > 0;
            }
            catch
            {
                return false;
            }
        }

        public static bool UpdateCompanyField(int companyId, int countryId, string fieldName, object value, SqlDbType type)
        {
            string column = fieldName switch
            {
                "Name" => "Name",
                "OwnerName" => "OwnerName",
                "ProductionType" => "ProductionType",
                "JobsOffered" => "JobsOffered",
                "BaseSalary" => "BaseSalary",
                "TaxRate" => "TaxRate",
                "Budget" => "Budget",
                "IsStateCompany" => "IsStateCompany",
                "MilitaryProductionShare" => "MilitaryProductionShare",
                "InputResourceType" => "InputResourceType",
                "InputAmount" => "InputAmount",
                "IsActive" => "IsActive",
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(column))
                return false;

            string q = $"UPDATE EconomyCompanies SET {column} = @Value, UpdatedAt = @UpdatedAt WHERE CompanyId = @CompanyId AND CountryId = @CountryId;";

            try
            {
                int rows = BaseRepository.ExecuteNonQuery(
                    q,
                    BaseRepository.Param("@Value", type, value ?? DBNull.Value),
                    BaseRepository.Param("@UpdatedAt", SqlDbType.DateTime2, DateTime.UtcNow),
                    BaseRepository.Param("@CompanyId", SqlDbType.Int, companyId),
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
