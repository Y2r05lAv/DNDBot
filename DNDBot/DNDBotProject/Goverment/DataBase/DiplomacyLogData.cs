#nullable enable
using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DNDBot.DataBase
{
    public static class DiplomacyLogData
    {
        static DiplomacyLogData()
        {
            BaseRepository.DBInitialization();
        }

        public static void AddLog(int countryId, int targetCountryId, string action, int oldState, int newState, long userId, int? turnNumber)
        {
            const string q = @"
INSERT INTO DiplomacyActionsLog (
    CountryId, TargetCountryId, Action, OldState, NewState, UserId, TurnNumber
)
VALUES (
    @CountryId, @TargetCountryId, @Action, @OldState, @NewState, @UserId, @TurnNumber
);";

            BaseRepository.ExecuteNonQuery(q,
                BaseRepository.Param("@CountryId", SqlDbType.Int, countryId),
                BaseRepository.Param("@TargetCountryId", SqlDbType.Int, targetCountryId),
                BaseRepository.Param("@Action", SqlDbType.NVarChar, action, 50),
                BaseRepository.Param("@OldState", SqlDbType.Int, oldState),
                BaseRepository.Param("@NewState", SqlDbType.Int, newState),
                BaseRepository.Param("@UserId", SqlDbType.BigInt, userId),
                BaseRepository.Param("@TurnNumber", SqlDbType.Int, (object?)turnNumber ?? DBNull.Value)
            );
        }
    }
}

