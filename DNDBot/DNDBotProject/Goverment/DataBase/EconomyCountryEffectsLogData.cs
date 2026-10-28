#nullable enable
using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DNDBot.DataBase
{
    public static class EconomyCountryEffectsLogData
    {
        static EconomyCountryEffectsLogData()
        {
            BaseRepository.DBInitialization();
        }

        public static void AddLog(int countryId, long userId, string effectType, bool oldValue, bool newValue, string action, int? turnNumber)
        {
            const string q = @"
INSERT INTO EconomyCountryEffectsLog (
    CountryId, UserId, EffectType, OldValue, NewValue, Action, TurnNumber
)
VALUES (
    @CountryId, @UserId, @EffectType, @OldValue, @NewValue, @Action, @TurnNumber
);";

            BaseRepository.ExecuteNonQuery(q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId),
                BaseRepository.Param("@UserId", SqlDbType.BigInt, userId),
                BaseRepository.Param("@EffectType", SqlDbType.NVarChar, effectType, 50),
                BaseRepository.Param("@OldValue", SqlDbType.Bit, oldValue),
                BaseRepository.Param("@NewValue", SqlDbType.Bit, newValue),
                BaseRepository.Param("@Action", SqlDbType.NVarChar, action, 50),
                BaseRepository.Param("@TurnNumber", SqlDbType.Int, (object?)turnNumber ?? DBNull.Value)
            );
        }
    }
}

