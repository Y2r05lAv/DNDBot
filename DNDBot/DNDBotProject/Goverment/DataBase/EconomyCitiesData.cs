#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DNDBot.DataBase
{
    public sealed class EconomyCityRecord
    {
        public int CityId { get; set; }
        public int CountryId { get; set; }
        public string CityName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public static class EconomyCitiesData
    {
        static EconomyCitiesData()
        {
            BaseRepository.DBInitialization();
        }

        public static List<EconomyCityRecord> GetByCountry(int countryId)
        {
            const string q = @"
SELECT CityId, CountryId, CityName, CreatedAt
FROM EconomyCities
WHERE CountryId = @CountryId
ORDER BY CASE WHEN CityName = N'Столиця' THEN 0 ELSE 1 END, CityName;";

            var list = new List<EconomyCityRecord>();
            using var reader = BaseRepository.ExecuteReader(
                q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId));
            try
            {
                while (reader.Read())
                {
                    list.Add(new EconomyCityRecord
                    {
                        CityId = reader.GetInt32(reader.GetOrdinal("CityId")),
                        CountryId = reader.GetInt32(reader.GetOrdinal("CountryId")),
                        CityName = reader.GetString(reader.GetOrdinal("CityName")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                    });
                }
            }
            finally
            {
                reader.Dispose();
            }

            return list;
        }

        public static EconomyCityRecord? GetById(int cityId, int countryId)
        {
            const string q = @"
SELECT TOP 1 CityId, CountryId, CityName, CreatedAt
FROM EconomyCities
WHERE CityId = @CityId AND CountryId = @CountryId;";

            using var reader = BaseRepository.ExecuteReader(
                q,
                BaseRepository.Param("@CityId", SqlDbType.Int, cityId),
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId));
            try
            {
                if (reader.Read())
                {
                    return new EconomyCityRecord
                    {
                        CityId = reader.GetInt32(reader.GetOrdinal("CityId")),
                        CountryId = reader.GetInt32(reader.GetOrdinal("CountryId")),
                        CityName = reader.GetString(reader.GetOrdinal("CityName")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                    };
                }
            }
            finally
            {
                reader.Dispose();
            }

            return null;
        }

        public static EconomyCityRecord? GetByName(int countryId, string cityName)
        {
            const string q = @"
SELECT TOP 1 CityId, CountryId, CityName, CreatedAt
FROM EconomyCities
WHERE CountryId = @CountryId AND CityName = @CityName;";

            using var reader = BaseRepository.ExecuteReader(
                q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId),
                BaseRepository.Param("@CityName", SqlDbType.NVarChar, cityName, 60));
            try
            {
                if (reader.Read())
                {
                    return new EconomyCityRecord
                    {
                        CityId = reader.GetInt32(reader.GetOrdinal("CityId")),
                        CountryId = reader.GetInt32(reader.GetOrdinal("CountryId")),
                        CityName = reader.GetString(reader.GetOrdinal("CityName")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                    };
                }
            }
            finally
            {
                reader.Dispose();
            }

            return null;
        }

        public static int EnsureCity(int countryId, string cityName)
        {
            const string q = @"
IF NOT EXISTS (
    SELECT 1
    FROM EconomyCities
    WHERE CountryId = @CountryId AND CityName = @CityName
)
BEGIN
    INSERT INTO EconomyCities (CountryId, CityName)
    VALUES (@CountryId, @CityName);
END

SELECT TOP 1 CityId
FROM EconomyCities
WHERE CountryId = @CountryId AND CityName = @CityName;";

            var idObj = BaseRepository.ExecuteScalar(
                q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId),
                BaseRepository.Param("@CityName", SqlDbType.NVarChar, cityName, 60));

            if (idObj == null || idObj == DBNull.Value)
                return 0;

            return Convert.ToInt32(idObj);
        }

        public static bool DeleteCity(int cityId, int countryId)
        {
            const string q = @"
DELETE FROM EconomyCities
WHERE CityId = @CityId AND CountryId = @CountryId;";

            int rows = BaseRepository.ExecuteNonQuery(
                q,
                BaseRepository.Param("@CityId", SqlDbType.Int, cityId),
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId));
            return rows > 0;
        }

        public static int CountByCountry(int countryId)
        {
            const string q = @"
SELECT COUNT(1)
FROM EconomyCities
WHERE CountryId = @CountryId;";

            var obj = BaseRepository.ExecuteScalar(
                q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId));
            if (obj == null || obj == DBNull.Value)
                return 0;

            return Convert.ToInt32(obj);
        }
    }
}
