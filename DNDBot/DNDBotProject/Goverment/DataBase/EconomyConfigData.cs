using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DNDBot.DataBase
{
    public static class EconomyConfigData
    {
        private static string BuildCountryKey(int countryId, string key)
        {
            return $"Country:{countryId}:{key}";
        }

        private static decimal? TryGetDecimalRaw(string key)
        {
            try
            {
                const string q = "SELECT ValueDecimal FROM EconomyConfig WHERE [Key] = @Key";
                var obj = BaseRepository.ExecuteScalar(q, BaseRepository.Param("@Key", SqlDbType.NVarChar, key, 100));
                if (obj == null || obj == DBNull.Value)
                    return null;
                return Convert.ToDecimal(obj);
            }
            catch
            {
                return null;
            }
        }

        private static SqlParameter DecimalParam(string name, decimal value, byte precision = 18, byte scale = 4)
        {
            return new SqlParameter(name, SqlDbType.Decimal)
            {
                Precision = precision,
                Scale = scale,
                Value = value
            };
        }

        static EconomyConfigData()
        {
            BaseRepository.DBInitialization();
        }

        public static float GetFloat(string key, float defaultValue)
        {
            try
            {
                const string q = "SELECT ValueDecimal FROM EconomyConfig WHERE [Key] = @Key";
                var obj = BaseRepository.ExecuteScalar(q, BaseRepository.Param("@Key", SqlDbType.NVarChar, key, 100));
                if (obj == null || obj == DBNull.Value)
                    return defaultValue;
                return Convert.ToSingle(obj);
            }
            catch
            {
                return defaultValue;
            }
        }

        public static decimal GetDecimal(string key, decimal defaultValue)
        {
            try
            {
                const string q = "SELECT ValueDecimal FROM EconomyConfig WHERE [Key] = @Key";
                var obj = BaseRepository.ExecuteScalar(q, BaseRepository.Param("@Key", SqlDbType.NVarChar, key, 100));
                if (obj == null || obj == DBNull.Value)
                    return defaultValue;
                return Convert.ToDecimal(obj);
            }
            catch
            {
                return defaultValue;
            }
        }

        public static decimal GetDecimalForCountry(int countryId, string key, decimal defaultValue)
        {
            if (countryId > 0)
            {
                string countryKey = BuildCountryKey(countryId, key);
                var countryValue = TryGetDecimalRaw(countryKey);
                if (countryValue.HasValue)
                    return countryValue.Value;
            }

            return GetDecimal(key, defaultValue);
        }

        public static string GetText(string key, string defaultValue)
        {
            try
            {
                const string q = "SELECT ValueText FROM EconomyConfig WHERE [Key] = @Key";
                var obj = BaseRepository.ExecuteScalar(q, BaseRepository.Param("@Key", SqlDbType.NVarChar, key, 100));
                if (obj == null || obj == DBNull.Value)
                    return defaultValue;
                return Convert.ToString(obj) ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        public static string GetTextForCountry(int countryId, string key, string defaultValue)
        {
            if (countryId > 0)
            {
                try
                {
                    const string q = "SELECT ValueText FROM EconomyConfig WHERE [Key] = @Key";
                    string countryKey = BuildCountryKey(countryId, key);
                    var obj = BaseRepository.ExecuteScalar(q, BaseRepository.Param("@Key", SqlDbType.NVarChar, countryKey, 100));
                    if (obj != null && obj != DBNull.Value)
                        return Convert.ToString(obj) ?? defaultValue;
                }
                catch
                {
                     
                }
            }

            return GetText(key, defaultValue);
        }

        public static int GetInt(string key, int defaultValue)
        {
            try
            {
                const string q = "SELECT ValueDecimal FROM EconomyConfig WHERE [Key] = @Key";
                var obj = BaseRepository.ExecuteScalar(q, BaseRepository.Param("@Key", SqlDbType.NVarChar, key, 100));
                if (obj == null || obj == DBNull.Value)
                    return defaultValue;
                return Convert.ToInt32(obj);
            }
            catch
            {
                return defaultValue;
            }
        }

        public static bool UpsertDecimal(string key, decimal value)
        {
            try
            {
                const string q = @"
IF EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = @Key)
    UPDATE EconomyConfig
    SET ValueDecimal = @ValueDecimal,
        ValueText = NULL
    WHERE [Key] = @Key;
ELSE
    INSERT INTO EconomyConfig ([Key], ValueDecimal, ValueText)
    VALUES (@Key, @ValueDecimal, NULL);";

                BaseRepository.ExecuteNonQuery(
                    q,
                    BaseRepository.Param("@Key", SqlDbType.NVarChar, key, 100),
                    DecimalParam("@ValueDecimal", value)
                );
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool UpsertCountryDecimal(int countryId, string key, decimal value)
        {
            if (countryId <= 0)
                return UpsertDecimal(key, value);

            return UpsertDecimal(BuildCountryKey(countryId, key), value);
        }

        public static bool UpsertText(string key, string value)
        {
            try
            {
                const string q = @"
IF EXISTS (SELECT 1 FROM EconomyConfig WHERE [Key] = @Key)
    UPDATE EconomyConfig
    SET ValueText = @ValueText
    WHERE [Key] = @Key;
ELSE
    INSERT INTO EconomyConfig ([Key], ValueDecimal, ValueText)
    VALUES (@Key, NULL, @ValueText);";

                BaseRepository.ExecuteNonQuery(
                    q,
                    BaseRepository.Param("@Key", SqlDbType.NVarChar, key, 100),
                    BaseRepository.Param("@ValueText", SqlDbType.NVarChar, value, 200)
                );
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool UpsertCountryText(int countryId, string key, string value)
        {
            if (countryId <= 0)
                return UpsertText(key, value);

            return UpsertText(BuildCountryKey(countryId, key), value);
        }
    }
}
