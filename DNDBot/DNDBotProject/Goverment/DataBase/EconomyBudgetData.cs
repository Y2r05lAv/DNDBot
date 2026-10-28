using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DNDBot.DataBase
{
    public static class EconomyBudgetData
    {
        static EconomyBudgetData()
        {
            BaseRepository.DBInitialization();
        }

        public sealed class EconomyBudgetRecord
        {
            public int CountryId { get; set; }
            public decimal InfrastructureShare { get; set; }
            public decimal MilitaryShare { get; set; }
            public decimal ScienceShare { get; set; }
            public decimal SocialShare { get; set; }
            public DateTime UpdatedAt { get; set; }
        }

        private static SqlParameter DecimalParam(string name, decimal value, byte precision = 5, byte scale = 4)
        {
            var p = new SqlParameter(name, SqlDbType.Decimal)
            {
                Precision = precision,
                Scale = scale,
                Value = value
            };
            return p;
        }

        public static EconomyBudgetRecord GetByCountry(int countryId)
        {
            const string q = @"
SELECT CountryId, InfrastructureShare, MilitaryShare, ScienceShare, SocialShare, UpdatedAt
FROM EconomyBudget
WHERE CountryId = @CountryId;";

            using var reader = BaseRepository.ExecuteReader(q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId));
            try
            {
                if (reader.Read())
                {
                    return new EconomyBudgetRecord
                    {
                        CountryId = reader.GetInt32(reader.GetOrdinal("CountryId")),
                        InfrastructureShare = reader.GetDecimal(reader.GetOrdinal("InfrastructureShare")),
                        MilitaryShare = reader.GetDecimal(reader.GetOrdinal("MilitaryShare")),
                        ScienceShare = reader.GetDecimal(reader.GetOrdinal("ScienceShare")),
                        SocialShare = reader.GetDecimal(reader.GetOrdinal("SocialShare")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                    };
                }
            }
            finally
            {
                reader.Dispose();
            }

            return new EconomyBudgetRecord
            {
                CountryId = countryId,
                InfrastructureShare = 0.25m,
                MilitaryShare = 0.25m,
                ScienceShare = 0.25m,
                SocialShare = 0.25m,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public static void Upsert(EconomyBudgetRecord record)
        {
            const string q = @"
IF EXISTS (SELECT 1 FROM EconomyBudget WHERE CountryId = @CountryId)
BEGIN
    UPDATE EconomyBudget
    SET InfrastructureShare = @InfrastructureShare,
        MilitaryShare = @MilitaryShare,
        ScienceShare = @ScienceShare,
        SocialShare = @SocialShare,
        UpdatedAt = @UpdatedAt
    WHERE CountryId = @CountryId;
END
ELSE
BEGIN
    INSERT INTO EconomyBudget (CountryId, InfrastructureShare, MilitaryShare, ScienceShare, SocialShare, UpdatedAt)
    VALUES (@CountryId, @InfrastructureShare, @MilitaryShare, @ScienceShare, @SocialShare, @UpdatedAt);
END;";

            BaseRepository.ExecuteNonQuery(q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, record.CountryId),
                DecimalParam("@InfrastructureShare", record.InfrastructureShare),
                DecimalParam("@MilitaryShare", record.MilitaryShare),
                DecimalParam("@ScienceShare", record.ScienceShare),
                DecimalParam("@SocialShare", record.SocialShare),
                BaseRepository.Param("@UpdatedAt", SqlDbType.DateTime2, record.UpdatedAt)
            );
        }
    }
}
