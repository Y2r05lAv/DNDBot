using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DNDBot.DataBase
{
    public static class CountryIntlProgramsData
    {
        public sealed class CountryIntlProgramRecord
        {
            public int IntlProgramId { get; set; }
            public int CountryId { get; set; }
            public string ProgramType { get; set; } = string.Empty;
            public int? PartnerCountryId { get; set; }
            public decimal CostPerTick { get; set; }
            public int RequiredSpec { get; set; }
            public bool IsActive { get; set; } = true;
            public int TicksActive { get; set; }
            public int SuccessTicks { get; set; }
            public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        }

        static CountryIntlProgramsData()
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

        public static List<CountryIntlProgramRecord> GetByCountry(int countryId, bool activeOnly = false)
        {
            string q = @"
SELECT IntlProgramId, CountryId, ProgramType, PartnerCountryId, CostPerTick, RequiredSpec, IsActive, TicksActive, SuccessTicks, UpdatedAt
FROM CountryIntlPrograms
WHERE CountryId = @CountryId";
            if (activeOnly)
                q += " AND IsActive = 1";
            q += " ORDER BY IntlProgramId";

            var list = new List<CountryIntlProgramRecord>();
            using var reader = BaseRepository.ExecuteReader(q, BaseRepository.Param("@CountryId", SqlDbType.Int, countryId));
            try
            {
                while (reader.Read())
                    list.Add(Map(reader));
            }
            finally
            {
                reader.Dispose();
            }

            return list;
        }

        public static CountryIntlProgramRecord? GetById(int intlProgramId)
        {
            const string q = @"
SELECT IntlProgramId, CountryId, ProgramType, PartnerCountryId, CostPerTick, RequiredSpec, IsActive, TicksActive, SuccessTicks, UpdatedAt
FROM CountryIntlPrograms
WHERE IntlProgramId = @IntlProgramId";

            using var reader = BaseRepository.ExecuteReader(q, BaseRepository.Param("@IntlProgramId", SqlDbType.Int, intlProgramId));
            try
            {
                if (reader.Read())
                    return Map(reader);
            }
            finally
            {
                reader.Dispose();
            }

            return null;
        }

        public static CountryIntlProgramRecord? GetByCountryAndType(int countryId, string programType)
        {
            const string q = @"
SELECT TOP (1) IntlProgramId, CountryId, ProgramType, PartnerCountryId, CostPerTick, RequiredSpec, IsActive, TicksActive, SuccessTicks, UpdatedAt
FROM CountryIntlPrograms
WHERE CountryId = @CountryId
  AND ProgramType = @ProgramType
ORDER BY IntlProgramId";

            using var reader = BaseRepository.ExecuteReader(
                q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId),
                BaseRepository.Param("@ProgramType", SqlDbType.NVarChar, programType ?? string.Empty, 64));
            try
            {
                if (reader.Read())
                    return Map(reader);
            }
            finally
            {
                reader.Dispose();
            }

            return null;
        }

        public static int Add(CountryIntlProgramRecord record)
        {
            const string q = @"
INSERT INTO CountryIntlPrograms (
    CountryId, ProgramType, PartnerCountryId, CostPerTick, RequiredSpec, IsActive, TicksActive, SuccessTicks, UpdatedAt
)
VALUES (
    @CountryId, @ProgramType, @PartnerCountryId, @CostPerTick, @RequiredSpec, @IsActive, @TicksActive, @SuccessTicks, @UpdatedAt
);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            record.ProgramType = (record.ProgramType ?? string.Empty).Trim();
            if (record.ProgramType.Length == 0)
                return 0;
            record.UpdatedAt = DateTime.UtcNow;
            record.RequiredSpec = Math.Max(0, record.RequiredSpec);
            record.CostPerTick = Math.Max(0m, record.CostPerTick);

            object? idObj = BaseRepository.ExecuteScalar(
                q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, record.CountryId),
                BaseRepository.Param("@ProgramType", SqlDbType.NVarChar, record.ProgramType, 64),
                BaseRepository.Param("@PartnerCountryId", SqlDbType.Int, (object?)record.PartnerCountryId ?? DBNull.Value),
                DecimalParam("@CostPerTick", record.CostPerTick),
                BaseRepository.Param("@RequiredSpec", SqlDbType.Int, record.RequiredSpec),
                BaseRepository.Param("@IsActive", SqlDbType.Bit, record.IsActive),
                BaseRepository.Param("@TicksActive", SqlDbType.Int, record.TicksActive),
                BaseRepository.Param("@SuccessTicks", SqlDbType.Int, record.SuccessTicks),
                BaseRepository.Param("@UpdatedAt", SqlDbType.DateTime2, record.UpdatedAt)
            );

            return idObj == null || idObj == DBNull.Value ? 0 : Convert.ToInt32(idObj);
        }

        public static bool Update(CountryIntlProgramRecord record)
        {
            const string q = @"
UPDATE CountryIntlPrograms
SET CountryId = @CountryId,
    ProgramType = @ProgramType,
    PartnerCountryId = @PartnerCountryId,
    CostPerTick = @CostPerTick,
    RequiredSpec = @RequiredSpec,
    IsActive = @IsActive,
    TicksActive = @TicksActive,
    SuccessTicks = @SuccessTicks,
    UpdatedAt = @UpdatedAt
WHERE IntlProgramId = @IntlProgramId";

            record.ProgramType = (record.ProgramType ?? string.Empty).Trim();
            if (record.ProgramType.Length == 0)
                return false;
            record.UpdatedAt = DateTime.UtcNow;
            record.RequiredSpec = Math.Max(0, record.RequiredSpec);
            record.CostPerTick = Math.Max(0m, record.CostPerTick);

            int rows = BaseRepository.ExecuteNonQuery(
                q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, record.CountryId),
                BaseRepository.Param("@ProgramType", SqlDbType.NVarChar, record.ProgramType, 64),
                BaseRepository.Param("@PartnerCountryId", SqlDbType.Int, (object?)record.PartnerCountryId ?? DBNull.Value),
                DecimalParam("@CostPerTick", record.CostPerTick),
                BaseRepository.Param("@RequiredSpec", SqlDbType.Int, record.RequiredSpec),
                BaseRepository.Param("@IsActive", SqlDbType.Bit, record.IsActive),
                BaseRepository.Param("@TicksActive", SqlDbType.Int, record.TicksActive),
                BaseRepository.Param("@SuccessTicks", SqlDbType.Int, record.SuccessTicks),
                BaseRepository.Param("@UpdatedAt", SqlDbType.DateTime2, record.UpdatedAt),
                BaseRepository.Param("@IntlProgramId", SqlDbType.Int, record.IntlProgramId)
            );

            return rows > 0;
        }

        public static void Upsert(CountryIntlProgramRecord record)
        {
            if (record == null || record.CountryId <= 0)
                return;

            if (record.IntlProgramId > 0 && Update(record))
                return;

            int id = Add(record);
            if (id > 0)
                record.IntlProgramId = id;
        }

        public static bool SetActive(int countryId, int intlProgramId, bool isActive)
        {
            const string q = @"
UPDATE CountryIntlPrograms
SET IsActive = @IsActive,
    UpdatedAt = SYSDATETIME()
WHERE CountryId = @CountryId
  AND IntlProgramId = @IntlProgramId";

            int rows = BaseRepository.ExecuteNonQuery(
                q,
                BaseRepository.Param("@IsActive", SqlDbType.Bit, isActive),
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId),
                BaseRepository.Param("@IntlProgramId", SqlDbType.Int, intlProgramId)
            );

            return rows > 0;
        }

        private static CountryIntlProgramRecord Map(IDataRecord reader)
        {
            return new CountryIntlProgramRecord
            {
                IntlProgramId = reader.GetInt32(reader.GetOrdinal("IntlProgramId")),
                CountryId = reader.GetInt32(reader.GetOrdinal("CountryId")),
                ProgramType = reader.IsDBNull(reader.GetOrdinal("ProgramType")) ? string.Empty : reader.GetString(reader.GetOrdinal("ProgramType")),
                PartnerCountryId = reader.IsDBNull(reader.GetOrdinal("PartnerCountryId")) ? null : reader.GetInt32(reader.GetOrdinal("PartnerCountryId")),
                CostPerTick = reader.GetDecimal(reader.GetOrdinal("CostPerTick")),
                RequiredSpec = reader.GetInt32(reader.GetOrdinal("RequiredSpec")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                TicksActive = reader.GetInt32(reader.GetOrdinal("TicksActive")),
                SuccessTicks = reader.GetInt32(reader.GetOrdinal("SuccessTicks")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
            };
        }
    }
}
