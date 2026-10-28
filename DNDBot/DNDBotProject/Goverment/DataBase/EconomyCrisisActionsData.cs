#nullable enable
using System;
using System.Data;

namespace DNDBot.DataBase
{
    public static class EconomyCrisisActionsData
    {
        static EconomyCrisisActionsData()
        {
            BaseRepository.DBInitialization();
        }

        public static int GetLastAppliedTurn(int countryId, string actionType)
        {
            const string q = @"
SELECT LastAppliedTurn
FROM EconomyCrisisActionsLog
WHERE CountryId = @CountryId AND ActionType = @ActionType";

            object? obj;
            try
            {
                obj = BaseRepository.ExecuteScalar(q,
                    BaseRepository.Param("@CountryId", SqlDbType.Int, countryId),
                    BaseRepository.Param("@ActionType", SqlDbType.NVarChar, actionType, 30));
            }
            catch
            {
                return 0;
            }

            if (obj == null || obj == DBNull.Value)
                return 0;
            return Convert.ToInt32(obj);
        }

        public static void UpsertLastApplied(int countryId, string actionType, int turnNumber)
        {
            const string q = @"
MERGE EconomyCrisisActionsLog AS target
USING (SELECT @CountryId AS CountryId, @ActionType AS ActionType) AS source
ON target.CountryId = source.CountryId AND target.ActionType = source.ActionType
WHEN MATCHED THEN
    UPDATE SET LastAppliedTurn = @TurnNumber, LastAppliedAt = GETDATE()
WHEN NOT MATCHED THEN
    INSERT (CountryId, ActionType, LastAppliedTurn, LastAppliedAt)
    VALUES (@CountryId, @ActionType, @TurnNumber, GETDATE());";

            try
            {
                BaseRepository.ExecuteNonQuery(q,
                    BaseRepository.Param("@CountryId", SqlDbType.Int, countryId),
                    BaseRepository.Param("@ActionType", SqlDbType.NVarChar, actionType, 30),
                    BaseRepository.Param("@TurnNumber", SqlDbType.Int, turnNumber));
            }
            catch
            {
                 
            }
        }
    }
}

