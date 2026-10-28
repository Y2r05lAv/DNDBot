#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DNDBot.DataBase
{
    public static class DiplomacyRelationsData
    {
        static DiplomacyRelationsData()
        {
            BaseRepository.DBInitialization();
        }

        public sealed class DiplomacyRelationRecord
        {
            public int CountryA { get; set; }
            public int CountryB { get; set; }
            public int State { get; set; }
            public bool IsAlliance { get; set; }
            public bool IsAtWar { get; set; }
            public bool HasSanctions { get; set; }
            public bool HasEmbargo { get; set; }
            public bool HasTradeAgreement { get; set; }
            public int? LastChangedTurn { get; set; }
            public DateTime UpdatedAt { get; set; }
        }

        public static DiplomacyRelationRecord? GetRelation(int countryA, int countryB)
        {
            const string q = @"
SELECT CountryA, CountryB, State, IsAlliance, IsAtWar, HasSanctions, HasEmbargo, HasTradeAgreement, LastChangedTurn, UpdatedAt
FROM DiplomacyRelations
WHERE CountryA = @A AND CountryB = @B";

            using var reader = BaseRepository.ExecuteReader(q,
                BaseRepository.Param("@A", SqlDbType.Int, countryA),
                BaseRepository.Param("@B", SqlDbType.Int, countryB)
            );
            try
            {
                if (!reader.Read())
                    return null;
                return new DiplomacyRelationRecord
                {
                    CountryA = reader.GetInt32(reader.GetOrdinal("CountryA")),
                    CountryB = reader.GetInt32(reader.GetOrdinal("CountryB")),
                    State = reader.GetInt32(reader.GetOrdinal("State")),
                    IsAlliance = reader.GetBoolean(reader.GetOrdinal("IsAlliance")),
                    IsAtWar = reader.GetBoolean(reader.GetOrdinal("IsAtWar")),
                    HasSanctions = reader.GetBoolean(reader.GetOrdinal("HasSanctions")),
                    HasEmbargo = reader.GetBoolean(reader.GetOrdinal("HasEmbargo")),
                    HasTradeAgreement = reader.GetBoolean(reader.GetOrdinal("HasTradeAgreement")),
                    LastChangedTurn = reader.IsDBNull(reader.GetOrdinal("LastChangedTurn")) ? null : reader.GetInt32(reader.GetOrdinal("LastChangedTurn")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                };
            }
            finally
            {
                reader.Dispose();
            }
        }

        public static List<DiplomacyRelationRecord> GetByCountry(int countryId)
        {
            const string q = @"
SELECT CountryA, CountryB, State, IsAlliance, IsAtWar, HasSanctions, HasEmbargo, HasTradeAgreement, LastChangedTurn, UpdatedAt
FROM DiplomacyRelations
WHERE CountryA = @C OR CountryB = @C";

            var list = new List<DiplomacyRelationRecord>();
            using var reader = BaseRepository.ExecuteReader(q,
                BaseRepository.Param("@C", SqlDbType.Int, countryId)
            );
            try
            {
                while (reader.Read())
                {
                    list.Add(new DiplomacyRelationRecord
                    {
                        CountryA = reader.GetInt32(reader.GetOrdinal("CountryA")),
                        CountryB = reader.GetInt32(reader.GetOrdinal("CountryB")),
                        State = reader.GetInt32(reader.GetOrdinal("State")),
                        IsAlliance = reader.GetBoolean(reader.GetOrdinal("IsAlliance")),
                        IsAtWar = reader.GetBoolean(reader.GetOrdinal("IsAtWar")),
                        HasSanctions = reader.GetBoolean(reader.GetOrdinal("HasSanctions")),
                        HasEmbargo = reader.GetBoolean(reader.GetOrdinal("HasEmbargo")),
                        HasTradeAgreement = reader.GetBoolean(reader.GetOrdinal("HasTradeAgreement")),
                        LastChangedTurn = reader.IsDBNull(reader.GetOrdinal("LastChangedTurn")) ? null : reader.GetInt32(reader.GetOrdinal("LastChangedTurn")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                    });
                }
            }
            finally
            {
                reader.Dispose();
            }
            return list;
        }

        public static void Upsert(DiplomacyRelationRecord record)
        {
            const string q = @"
MERGE DiplomacyRelations AS target
USING (SELECT @A AS CountryA, @B AS CountryB) AS src
ON target.CountryA = src.CountryA AND target.CountryB = src.CountryB
WHEN MATCHED THEN UPDATE SET
    State = @State,
    IsAlliance = @IsAlliance,
    IsAtWar = @IsAtWar,
    HasSanctions = @HasSanctions,
    HasEmbargo = @HasEmbargo,
    HasTradeAgreement = @HasTradeAgreement,
    LastChangedTurn = @LastChangedTurn,
    UpdatedAt = SYSDATETIME()
WHEN NOT MATCHED THEN
    INSERT (CountryA, CountryB, State, IsAlliance, IsAtWar, HasSanctions, HasEmbargo, HasTradeAgreement, LastChangedTurn)
    VALUES (@A, @B, @State, @IsAlliance, @IsAtWar, @HasSanctions, @HasEmbargo, @HasTradeAgreement, @LastChangedTurn);";

            BaseRepository.ExecuteNonQuery(q,
                BaseRepository.Param("@A", SqlDbType.Int, record.CountryA),
                BaseRepository.Param("@B", SqlDbType.Int, record.CountryB),
                BaseRepository.Param("@State", SqlDbType.Int, record.State),
                BaseRepository.Param("@IsAlliance", SqlDbType.Bit, record.IsAlliance),
                BaseRepository.Param("@IsAtWar", SqlDbType.Bit, record.IsAtWar),
                BaseRepository.Param("@HasSanctions", SqlDbType.Bit, record.HasSanctions),
                BaseRepository.Param("@HasEmbargo", SqlDbType.Bit, record.HasEmbargo),
                BaseRepository.Param("@HasTradeAgreement", SqlDbType.Bit, record.HasTradeAgreement),
                BaseRepository.Param("@LastChangedTurn", SqlDbType.Int, (object?)record.LastChangedTurn ?? DBNull.Value)
            );
        }
    }
}

