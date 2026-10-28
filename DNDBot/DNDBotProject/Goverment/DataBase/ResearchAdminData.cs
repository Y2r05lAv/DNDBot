#nullable enable
using System;

namespace DNDBot.DataBase
{
    public static class ResearchAdminData
    {
        static ResearchAdminData()
        {
            BaseRepository.DBInitialization();
        }

        public static string ClearAllResearchData()
        {
            const string sql = @"
BEGIN TRY
    BEGIN TRAN;
    SET NOCOUNT ON;

    DELETE FROM ResearchEffects;
    DELETE FROM ResearchRequirements;
    DELETE FROM ResearchCompleted;
    DELETE FROM ResearchProgress;
    DELETE FROM ResearchDefinitions;
    DELETE FROM ResearchBranches;

    UPDATE Countries
    SET ResearchLevel = 0,
        ResearchSpeed = 50,
        CompletedResearches = 0;

    COMMIT TRAN;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    THROW;
END CATCH";

            BaseRepository.ExecuteNonQuery(sql);
            return "Дані досліджень очищені (визначення, ефекти, вимоги, прогрес, завершені та гілки).";
        }
    }
}
