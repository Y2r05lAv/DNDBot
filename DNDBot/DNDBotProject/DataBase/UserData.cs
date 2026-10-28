using DNDBot.DataBase;
using DNDBot.Spells;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;

namespace DNDBot
{
    ///




    public class UserData : BotBodyRealisation
    {
        private static SqlConnection sqlConnection = BaseRepository.SqlConnection;

        public class CharacterAdminRecord
        {
            public long UserId { get; set; }
            public int CharacterId { get; set; }
            public string Name { get; set; }
            public bool IsActive { get; set; }
            public int? CountryId { get; set; }
        }

         
         
         

        ///



        public static int GetActiveCharacterId(long userId)
        {
            if (sqlConnection == null)
                throw new InvalidOperationException("SqlConnection - NullReferenceException");

             
            const string qGet = "SELECT ActiveCharacterId FROM UserSettings WHERE UserId=@UserId";
            using (var cmd = new SqlCommand(qGet, sqlConnection))
            {
                cmd.Parameters.Add("@UserId", SqlDbType.BigInt).Value = userId;

                var obj = cmd.ExecuteScalar();

                 
                if (obj != null && obj != DBNull.Value)
                    return Convert.ToInt32(obj);
            }

             
            int charId = EnsureDefaultCharacter(userId);  
            return charId;
        }

        public static int? GetActiveCharacterCountryId(long userId)
        {
            return GetCharacterCountryId(GetActiveCharacterId(userId));
        }

        public static int? GetCharacterCountryId(int characterId)
        {
            if (sqlConnection == null)
                throw new InvalidOperationException("SqlConnection is null");

            const string q = "SELECT CountryId FROM Characters WHERE CharacterId=@CharId";
            using var cmd = new SqlCommand(q, sqlConnection);
            cmd.Parameters.Add("@CharId", SqlDbType.Int).Value = characterId;

            var obj = cmd.ExecuteScalar();
            if (obj == null || obj == DBNull.Value)
                return null;

            int val = Convert.ToInt32(obj);
            return val > 0 ? val : null;
        }

        public static string SetCharacterCountry(int characterId, int countryId)
        {
            if (sqlConnection == null)
                throw new InvalidOperationException("SqlConnection is null");

            if (countryId <= 0)
                return "Невірний CountryId.";

            if (DNDBot.DataBase.CountriesData.GetCountryById(countryId) == null)
                return "Країну не знайдено.";

            const string qCheck = "SELECT COUNT(*) FROM Characters WHERE CharacterId=@CharId";
            using (var chk = new SqlCommand(qCheck, sqlConnection))
            {
                chk.Parameters.Add("@CharId", SqlDbType.Int).Value = characterId;
                if (Convert.ToInt32(chk.ExecuteScalar()) == 0)
                    return "Персонажа не знайдено.";
            }

            const string qUpd = "UPDATE Characters SET CountryId=@CountryId WHERE CharacterId=@CharId";
            using (var cmd = new SqlCommand(qUpd, sqlConnection))
            {
                cmd.Parameters.Add("@CountryId", SqlDbType.Int).Value = countryId;
                cmd.Parameters.Add("@CharId", SqlDbType.Int).Value = characterId;
                cmd.ExecuteNonQuery();
            }

            return "Країну призначено персонажу.";
        }

        public static List<CharacterAdminRecord> GetAllCharactersForAdmin()
        {
            if (sqlConnection == null)
                throw new InvalidOperationException("SqlConnection is null");

            var list = new List<CharacterAdminRecord>();

            const string q = @"
SELECT c.UserId, c.CharacterId, COALESCE(u.Name, c.Name) AS Name, c.IsActive, c.CountryId
FROM Characters c
INNER JOIN UserData u
    ON u.Id = c.UserId AND u.CharacterId = c.CharacterId
WHERE c.UserId <> @MasterId
ORDER BY c.UserId ASC, c.CharacterId ASC";

            using var cmd = new SqlCommand(q, sqlConnection);
            cmd.Parameters.Add("@MasterId", SqlDbType.BigInt).Value = masterID;
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new CharacterAdminRecord
                {
                    UserId = r.GetInt64(0),
                    CharacterId = r.GetInt32(1),
                    Name = r.IsDBNull(2) ? "" : r.GetString(2),
                    IsActive = r.GetBoolean(3),
                    CountryId = r.IsDBNull(4) ? null : r.GetInt32(4)
                });
            }

            return list;
        }

        public static int GetDistinctUserCount(bool excludeMaster = false)
        {
            if (sqlConnection == null)
                throw new InvalidOperationException("SqlConnection is null");

            string q = excludeMaster
                ? "SELECT COUNT(DISTINCT Id) FROM UserData WHERE Id <> @MasterId"
                : "SELECT COUNT(DISTINCT Id) FROM UserData";

            using var cmd = new SqlCommand(q, sqlConnection);
            if (excludeMaster)
                cmd.Parameters.Add("@MasterId", SqlDbType.BigInt).Value = masterID;

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public static List<int> GetUserCharacterIds(long userId)
        {
            if (sqlConnection == null)
                throw new InvalidOperationException("SqlConnection - NullReferenceException");

            var characterIds = new List<int>();
            const string qGet = "SELECT CharacterId FROM Characters WHERE UserId=@UserId";
            using (var cmd = new SqlCommand(qGet, sqlConnection))
            {
                cmd.Parameters.Add("@UserId", SqlDbType.BigInt).Value = userId;
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        characterIds.Add(reader.GetInt32(0));
                }
            }

             
            if (characterIds.Count == 0)
            {
                characterIds.Add(EnsureDefaultCharacter(userId));
            }

            return characterIds;
        }



        public static void DeleteCharacter(long userId, int characterId)
        {
            if (sqlConnection == null)
                throw new InvalidOperationException("SqlConnection is null");

             
            const string qCheck = @"
SELECT COUNT(*) 
FROM Characters 
WHERE UserId=@UserId AND CharacterId=@CharId";
            using (var chk = new SqlCommand(qCheck, sqlConnection))
            {
                chk.Parameters.Add("@UserId", SqlDbType.BigInt).Value = userId;
                chk.Parameters.Add("@CharId", SqlDbType.Int).Value = characterId;

                if (Convert.ToInt32(chk.ExecuteScalar()) == 0)
                    throw new Exception("Анкета не існує або не належить користувачу.");
            }

             
            const string qCount = "SELECT COUNT(*) FROM Characters WHERE UserId=@UserId";
            int count;
            using (var cnt = new SqlCommand(qCount, sqlConnection))
            {
                cnt.Parameters.Add("@UserId", SqlDbType.BigInt).Value = userId;
                count = Convert.ToInt32(cnt.ExecuteScalar());
            }
            if (count <= 1)
                throw new Exception("Неможливо видалити останню анкету.");

             
            int active = GetActiveCharacterId(userId);
            if (active == characterId)
            {
                const string qNext = @"
SELECT TOP 1 CharacterId 
FROM Characters
WHERE UserId=@UserId AND CharacterId<>@CharId
ORDER BY CharacterId ASC";
                using var next = new SqlCommand(qNext, sqlConnection);
                next.Parameters.Add("@UserId", SqlDbType.BigInt).Value = userId;
                next.Parameters.Add("@CharId", SqlDbType.Int).Value = characterId;

                var obj = next.ExecuteScalar();
                if (obj == null || obj == DBNull.Value)
                    throw new Exception("Не можу знайти іншу анкету для активації.");

                int newActive = Convert.ToInt32(obj);
                SetActiveCharacterId(userId, newActive);  
            }

             
            Exec("DELETE FROM SpellBook WHERE Id=@UserId AND CharacterId=@CharId");
            Exec("DELETE FROM UserInventory WHERE Id=@UserId AND CharacterId=@CharId");
            Exec("DELETE FROM UserData WHERE Id=@UserId AND CharacterId=@CharId");

             
            const string qDelChar = "DELETE FROM Characters WHERE UserId=@UserId AND CharacterId=@CharId";
            using (var cmd = new SqlCommand(qDelChar, sqlConnection))
            {
                cmd.Parameters.Add("@UserId", SqlDbType.BigInt).Value = userId;
                cmd.Parameters.Add("@CharId", SqlDbType.Int).Value = characterId;
                cmd.ExecuteNonQuery();
            }

            void Exec(string sql)
            {
                using var cmd = new SqlCommand(sql, sqlConnection);
                cmd.Parameters.Add("@UserId", SqlDbType.BigInt).Value = userId;
                cmd.Parameters.Add("@CharId", SqlDbType.Int).Value = characterId;
                cmd.ExecuteNonQuery();
            }
        }

        public static string GetCharacterName(long userId, int characterId)
        {
            if (sqlConnection == null)
                throw new InvalidOperationException("SqlConnection is null");

            const string qCharacters = @"
SELECT Name
FROM Characters
WHERE UserId=@UserId AND CharacterId=@CharId";

            using (var cmd = new SqlCommand(qCharacters, sqlConnection))
            {
                cmd.Parameters.Add("@UserId", SqlDbType.BigInt).Value = userId;
                cmd.Parameters.Add("@CharId", SqlDbType.Int).Value = characterId;

                var result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    string fromCharacters = result.ToString()?.Trim() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(fromCharacters))
                        return fromCharacters;
                }
            }

            const string qUserData = @"
SELECT Name
FROM UserData
WHERE Id=@Id AND CharacterId=@CharId";

            using (var cmd = new SqlCommand(qUserData, sqlConnection))
            {
                cmd.Parameters.Add("@Id", SqlDbType.BigInt).Value = userId;
                cmd.Parameters.Add("@CharId", SqlDbType.Int).Value = characterId;

                var result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    string fromUserData = result.ToString()?.Trim() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(fromUserData))
                        return fromUserData;
                }
            }

            return $"Анкета #{characterId}";
        }


        public static List<(int CharacterId, string Name, bool IsActive)> GetUserCharacters(long userId)
        {
            if (sqlConnection == null)
                throw new InvalidOperationException("SqlConnection - NullReferenceException");

            var list = new List<(int, string, bool)>();

            const string q = @"
SELECT CharacterId, Name, IsActive
FROM Characters
WHERE UserId = @UserId
ORDER BY CharacterId ASC";

            using var cmd = new SqlCommand(q, sqlConnection);
            cmd.Parameters.Add("@UserId", SqlDbType.BigInt).Value = userId;

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                int cid = r.GetInt32(0);
                string name = r.GetString(1);
                bool active = r.GetBoolean(2);
                list.Add((cid, name, active));
            }

            return list;
        }


        public static int CreateNewCharacter(long userId, string name)
        {
            if (sqlConnection == null)
                throw new InvalidOperationException("SqlConnection - NullReferenceException");

            if (string.IsNullOrWhiteSpace(name))
                name = "Нова анкета";

             
            const string qDeactivate = "UPDATE Characters SET IsActive = 0 WHERE UserId = @UserId";
            using (var deact = new SqlCommand(qDeactivate, sqlConnection))
            {
                deact.Parameters.Add("@UserId", SqlDbType.BigInt).Value = userId;
                deact.ExecuteNonQuery();
            }

             
            const string qInsert = @"
INSERT INTO Characters(UserId, Name, IsActive)
VALUES(@UserId, @Name, 1);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            int newCharId;
            using (var ins = new SqlCommand(qInsert, sqlConnection))
            {
                ins.Parameters.Add("@UserId", SqlDbType.BigInt).Value = userId;
                ins.Parameters.Add("@Name", SqlDbType.NVarChar, 64).Value = name;
                newCharId = Convert.ToInt32(ins.ExecuteScalar());
            }

             
            EnsureUserSettingsRow(userId, newCharId);
            SetActiveCharacterId(userId, newCharId, setCharactersIsActive: false);

            return newCharId;
        }


        public static void SetActiveCharacterId(long userId, int characterId, bool setCharactersIsActive = true)
        {
            if (sqlConnection == null)
                throw new InvalidOperationException("SqlConnection - NullReferenceException");

             
            const string qCheck = "SELECT COUNT(*) FROM Characters WHERE UserId=@UserId AND CharacterId=@CharId";
            using (var chk = new SqlCommand(qCheck, sqlConnection))
            {
                chk.Parameters.Add("@UserId", SqlDbType.BigInt).Value = userId;
                chk.Parameters.Add("@CharId", SqlDbType.Int).Value = characterId;
                int ok = Convert.ToInt32(chk.ExecuteScalar());
                if (ok == 0)
                    throw new Exception("Ця анкета не належить користувачу або не існує.");
            }

            EnsureUserSettingsRow(userId, characterId);

            const string qUpd = "UPDATE UserSettings SET ActiveCharacterId=@CharId WHERE UserId=@UserId";
            using (var cmd = new SqlCommand(qUpd, sqlConnection))
            {
                cmd.Parameters.Add("@UserId", SqlDbType.BigInt).Value = userId;
                cmd.Parameters.Add("@CharId", SqlDbType.Int).Value = characterId;
                cmd.ExecuteNonQuery();
            }

            if (setCharactersIsActive)
            {
                const string qDeact = "UPDATE Characters SET IsActive=0 WHERE UserId=@UserId";
                using (var d = new SqlCommand(qDeact, sqlConnection))
                {
                    d.Parameters.Add("@UserId", SqlDbType.BigInt).Value = userId;
                    d.ExecuteNonQuery();
                }

                const string qAct = "UPDATE Characters SET IsActive=1 WHERE UserId=@UserId AND CharacterId=@CharId";
                using (var a = new SqlCommand(qAct, sqlConnection))
                {
                    a.Parameters.Add("@UserId", SqlDbType.BigInt).Value = userId;
                    a.Parameters.Add("@CharId", SqlDbType.Int).Value = characterId;
                    a.ExecuteNonQuery();
                }
            }
        }


        public static void EnsureUserSettingsRow(long userId, int activeCharId)
        {
            if (sqlConnection == null)
                throw new InvalidOperationException("SqlConnection is null");

             
            const string qExists = "SELECT COUNT(*) FROM UserSettings WHERE UserId=@UserId";
            int count;
            using (var ex = new SqlCommand(qExists, sqlConnection))
            {
                ex.Parameters.Add("@UserId", SqlDbType.BigInt).Value = userId;
                count = Convert.ToInt32(ex.ExecuteScalar());
            }

            if (count == 0)
            {
                const string ins = "INSERT INTO UserSettings(UserId, ActiveCharacterId) VALUES(@UserId, @CharId)";
                using var cmd = new SqlCommand(ins, sqlConnection);
                cmd.Parameters.Add("@UserId", SqlDbType.BigInt).Value = userId;
                cmd.Parameters.Add("@CharId", SqlDbType.Int).Value = activeCharId;
                cmd.ExecuteNonQuery();
                return;
            }

             
            const string qNull = "SELECT ActiveCharacterId FROM UserSettings WHERE UserId=@UserId";
            using (var cmd = new SqlCommand(qNull, sqlConnection))
            {
                cmd.Parameters.Add("@UserId", SqlDbType.BigInt).Value = userId;
                var obj = cmd.ExecuteScalar();
                if (obj == null || obj == DBNull.Value)
                {
                    const string upd = "UPDATE UserSettings SET ActiveCharacterId=@CharId WHERE UserId=@UserId";
                    using var u = new SqlCommand(upd, sqlConnection);
                    u.Parameters.Add("@UserId", SqlDbType.BigInt).Value = userId;
                    u.Parameters.Add("@CharId", SqlDbType.Int).Value = activeCharId;
                    u.ExecuteNonQuery();
                }
            }
        }



        public static int EnsureDefaultCharacter(long userId)
        {
            if (sqlConnection == null)
                throw new InvalidOperationException("SqlConnection is null");

             
            const string qFind = @"
SELECT TOP 1 CharacterId
FROM Characters
WHERE UserId=@UserId
ORDER BY CASE WHEN IsActive=1 THEN 0 ELSE 1 END, CharacterId ASC";

            int charId;
            using (var find = new SqlCommand(qFind, sqlConnection))
            {
                find.Parameters.Add("@UserId", SqlDbType.BigInt).Value = userId;
                var obj = find.ExecuteScalar();

                if (obj == null || obj == DBNull.Value)
                {
                     
                    charId = CreateNewCharacter(userId, "Персонаж 1");
                    return charId;  
                }

                charId = Convert.ToInt32(obj);
            }

             
            EnsureUserSettingsRow(userId, charId);

             
            SetActiveCharacterId(userId, charId);

            return charId;
        }




         
         
         

        public static bool CheckifExist(long id)
            => CheckifExist(id, GetActiveCharacterId(id));

        public static bool CheckifExist(long id, int characterId)
        {
            if (sqlConnection == null)
            {
                Console.WriteLine("SqlConnection - NullRefferenceExpection");
                return false;
            }

            const string query = "SELECT COUNT(*) FROM UserData WHERE Id = @Id AND CharacterId = @CharacterId";

            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            {
                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@CharacterId", characterId);

                int count = (int)command.ExecuteScalar();
                return count > 0;
            }
        }

         
         
         

        ///


        public static string ClearUserData(long id)
        {
            if (sqlConnection == null)
            {
                Console.WriteLine("SqlConnection - NullRefferenceExpection");
                return "Дата база не під'єднана";
            }

            const string query = "DELETE FROM UserData WHERE Id = @Id;";

            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            {
                command.Parameters.AddWithValue("@Id", id);
                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected > 0)
                    return $"Гравця з айді '{id}' було успішно видалено";

                return $"Виникла помилка видалення: Гравець відсутній";
            }
        }

        ///


        public static string ClearUserData(long id, int characterId)
        {
            if (sqlConnection == null)
            {
                Console.WriteLine("SqlConnection - NullRefferenceExpection");
                return "Дата база не під'єднана";
            }

            const string query = "DELETE FROM UserData WHERE Id = @Id AND CharacterId = @CharacterId;";
            using var cmd = new SqlCommand(query, sqlConnection);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@CharacterId", characterId);

            int rowsAffected = cmd.ExecuteNonQuery();
            if (rowsAffected > 0)
                return $"Персонажа (CharId={characterId}) для користувача '{id}' було успішно видалено";

            return "Виникла помилка видалення: Персонаж відсутній";
        }

        public static void ClearAllData(bool isDeleting = false)
        {
            if (sqlConnection == null)
            {
                Console.WriteLine("SqlConnection - NullRefferenceExpection");
                return;
            }

             
            string query = "DELETE FROM UserData";
            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            {
                Console.WriteLine("To delete write: 'CONFIRM' else words will stop program");
            }
        }

        public static void DeletingAllData()
        {
            if (sqlConnection == null)
            {
                Console.WriteLine("SqlConnection - NullReferenceException");
                return;
            }

             
             
             
            var steps = new (string Name, string Sql)[]
            {
                ("Votes", "IF OBJECT_ID('Votes', 'U') IS NOT NULL DELETE FROM Votes;"),
                ("SessionAttendance", "IF OBJECT_ID('SessionAttendance', 'U') IS NOT NULL DELETE FROM SessionAttendance;"),
                ("PartyMembers", "IF OBJECT_ID('PartyMembers', 'U') IS NOT NULL DELETE FROM PartyMembers;"),
                ("Deputies", "IF OBJECT_ID('Deputies', 'U') IS NOT NULL DELETE FROM Deputies;"),
                ("Parties", "IF OBJECT_ID('Parties', 'U') IS NOT NULL DELETE FROM Parties;"),
                ("ParliamentSessions", "IF OBJECT_ID('ParliamentSessions', 'U') IS NOT NULL DELETE FROM ParliamentSessions;"),
                ("Parliament", "IF OBJECT_ID('Parliament', 'U') IS NOT NULL DELETE FROM Parliament;"),
                ("Laws", "IF OBJECT_ID('Laws', 'U') IS NOT NULL DELETE FROM Laws;"),

                ("UserSettings", "IF OBJECT_ID('UserSettings', 'U') IS NOT NULL DELETE FROM UserSettings;"),
                ("SpellBook", "IF OBJECT_ID('SpellBook', 'U') IS NOT NULL DELETE FROM SpellBook;"),
                ("UserInventory", "IF OBJECT_ID('UserInventory', 'U') IS NOT NULL DELETE FROM UserInventory;"),
                ("MagicStats", "IF OBJECT_ID('MagicStats', 'U') IS NOT NULL DELETE FROM MagicStats;"),
                ("UserData", "IF OBJECT_ID('UserData', 'U') IS NOT NULL DELETE FROM UserData;"),
                ("Characters", "IF OBJECT_ID('Characters', 'U') IS NOT NULL DELETE FROM Characters;")
            };

            foreach (var step in steps)
            {
                ExecuteDeleteStep(step.Name, step.Sql);
            }

            Console.WriteLine("[DB-CLEAR][OK] All requested tables were cleaned.");

            static bool IsTableMissingError(SqlException ex) => ex.Number == 208;

            void ExecuteDeleteStep(string stepName, string sql)
            {
                try
                {
                    using var command = new SqlCommand(sql, sqlConnection);
                    int rows = command.ExecuteNonQuery();
                    Console.WriteLine($"[DB-CLEAR][OK] {stepName}: deleted {rows} rows.");
                }
                catch (SqlException ex) when (IsTableMissingError(ex))
                {
                    Console.WriteLine($"[DB-CLEAR][WARN] {stepName}: table is missing, step skipped.");
                }
                catch (SqlException ex)
                {
                    Console.WriteLine($"[DB-CLEAR][FAIL] Step '{stepName}' failed (SqlError={ex.Number}, Line={ex.LineNumber}, Procedure={ex.Procedure ?? "N/A"}): {ex.Message}");
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DB-CLEAR][FAIL] Step '{stepName}' failed: {ex.Message}");
                    throw;
                }
            }
        }

        public static string ClearAllDataForCountry(int countryId)
        {
            if (sqlConnection == null)
                return "База даних не під'єднана.";

            if (countryId <= 0)
                return "Невірний CountryId.";

            var usersInCountry = new List<long>();
            const string qUsers = "SELECT DISTINCT UserId FROM Characters WHERE CountryId=@CountryId";
            using (var cmd = new SqlCommand(qUsers, sqlConnection))
            {
                cmd.Parameters.Add("@CountryId", SqlDbType.Int).Value = countryId;
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    usersInCountry.Add(r.GetInt64(0));
            }

            if (usersInCountry.Count == 0)
                return "Немає персонажів у вибраній країні.";

            var usersToDeleteSettings = new List<long>();
            foreach (var userId in usersInCountry)
            {
                int? activeCharId = null;
                const string qActive = "SELECT ActiveCharacterId FROM UserSettings WHERE UserId=@UserId";
                using (var cmd = new SqlCommand(qActive, sqlConnection))
                {
                    cmd.Parameters.Add("@UserId", SqlDbType.BigInt).Value = userId;
                    var obj = cmd.ExecuteScalar();
                    if (obj != null && obj != DBNull.Value)
                        activeCharId = Convert.ToInt32(obj);
                }

                if (activeCharId.HasValue)
                {
                    const string qIsActiveInCountry = @"
SELECT COUNT(*)
FROM Characters
WHERE UserId=@UserId AND CharacterId=@CharId AND CountryId=@CountryId";
                    using var cmd = new SqlCommand(qIsActiveInCountry, sqlConnection);
                    cmd.Parameters.Add("@UserId", SqlDbType.BigInt).Value = userId;
                    cmd.Parameters.Add("@CharId", SqlDbType.Int).Value = activeCharId.Value;
                    cmd.Parameters.Add("@CountryId", SqlDbType.Int).Value = countryId;
                    bool activeInCountry = Convert.ToInt32(cmd.ExecuteScalar()) > 0;

                    if (activeInCountry)
                    {
                        const string qNewActive = @"
SELECT TOP 1 CharacterId
FROM Characters
WHERE UserId=@UserId AND CountryId<>@CountryId
ORDER BY CharacterId ASC";
                        using var pick = new SqlCommand(qNewActive, sqlConnection);
                        pick.Parameters.Add("@UserId", SqlDbType.BigInt).Value = userId;
                        pick.Parameters.Add("@CountryId", SqlDbType.Int).Value = countryId;
                        var obj = pick.ExecuteScalar();
                        if (obj != null && obj != DBNull.Value)
                        {
                            int newActive = Convert.ToInt32(obj);
                            SetActiveCharacterId(userId, newActive);
                        }
                        else
                        {
                            usersToDeleteSettings.Add(userId);
                        }
                    }
                }
            }

            int deletedSpells = ExecDelete(@"
DELETE sb
FROM SpellBook sb
INNER JOIN Characters c
    ON sb.Id = c.UserId AND sb.CharacterId = c.CharacterId
WHERE c.CountryId=@CountryId");

            int deletedInventory = ExecDelete(@"
DELETE ui
FROM UserInventory ui
INNER JOIN Characters c
    ON ui.Id = c.UserId AND ui.CharacterId = c.CharacterId
WHERE c.CountryId=@CountryId");

            int deletedMagic = ExecDelete(@"
DELETE ms
FROM MagicStats ms
INNER JOIN Characters c
    ON ms.UserId = c.UserId AND ms.CharacterId = c.CharacterId
WHERE c.CountryId=@CountryId");

            int deletedUserData = ExecDelete(@"
DELETE ud
FROM UserData ud
INNER JOIN Characters c
    ON ud.Id = c.UserId AND ud.CharacterId = c.CharacterId
WHERE c.CountryId=@CountryId");

            int deletedCharacters = ExecDelete("DELETE FROM Characters WHERE CountryId=@CountryId");

            foreach (var userId in usersToDeleteSettings)
            {
                using var cmd = new SqlCommand("DELETE FROM UserSettings WHERE UserId=@UserId", sqlConnection);
                cmd.Parameters.Add("@UserId", SqlDbType.BigInt).Value = userId;
                cmd.ExecuteNonQuery();
            }

            return $"Очищено дані для країни #{countryId}: персонажів {deletedCharacters}, анкет {deletedUserData}, інвентар {deletedInventory}, заклинання {deletedSpells}, магія {deletedMagic}.";

            int ExecDelete(string sql)
            {
                using var cmd = new SqlCommand(sql, sqlConnection);
                cmd.Parameters.Add("@CountryId", SqlDbType.Int).Value = countryId;
                return cmd.ExecuteNonQuery();
            }
        }


         
         
         

        public static string AddUserToDataBase(UserDataClass userDataClass)
        {
            if (sqlConnection == null)
                return "Дата база не під'єднана";

            int characterId = userDataClass.CharacterId;
            if (characterId <= 0)
                characterId = GetActiveCharacterId(userDataClass.Id);

            const string checkQuery = "SELECT COUNT(*) FROM UserData WHERE Id = @Id AND CharacterId = @CharacterId";
            const string insertQuery = @"INSERT INTO UserData
                (Id, CharacterId, Strenght, Agility, Stamina, Intelligence, Wisdom, Charisma, Health, Armor, Name,
                 CopperCoin, SilverCoin, GoldenCoin, PlatinumCoin, MaxHP, TempHP, TempMaxHPReduction, XP, Level, StrenghtForWeightMathf)
                VALUES
                (@Id, @CharacterId, @Strenght, @Agility, @Stamina, @Intelligence, @Wisdom, @Charisma, @Health, @Armor, @Name,
                 @CopperCoin, @SilverCoin, @GoldenCoin, @PlatinumCoin, @MaxHP, @TempHP, @TempMaxHPReduction, @XP, @Level, @StrenghtForWeightMathf)";

            using (SqlCommand checkCommand = new SqlCommand(checkQuery, sqlConnection))
            {
                checkCommand.Parameters.AddWithValue("@Id", userDataClass.Id);
                checkCommand.Parameters.AddWithValue("@CharacterId", characterId);
                int count = (int)checkCommand.ExecuteScalar();

                if (count != 0)
                    return "Виникла помилка: Об'єкт вже існує";
            }

            using (SqlCommand insertCommand = new SqlCommand(insertQuery, sqlConnection))
            {
                insertCommand.Parameters.AddWithValue("@Id", userDataClass.Id);
                insertCommand.Parameters.AddWithValue("@CharacterId", characterId);
                insertCommand.Parameters.AddWithValue("@Strenght", userDataClass.Strenght);
                insertCommand.Parameters.AddWithValue("@Agility", userDataClass.Agility);
                insertCommand.Parameters.AddWithValue("@Stamina", userDataClass.Stamina);
                insertCommand.Parameters.AddWithValue("@Intelligence", userDataClass.Intelligence);
                insertCommand.Parameters.AddWithValue("@Wisdom", userDataClass.Wisdom);
                insertCommand.Parameters.AddWithValue("@Charisma", userDataClass.Charisma);
                insertCommand.Parameters.AddWithValue("@Health", userDataClass.Health);
                insertCommand.Parameters.AddWithValue("@Armor", userDataClass.Armor);
                insertCommand.Parameters.AddWithValue("@Name", userDataClass.Name);
                insertCommand.Parameters.AddWithValue("@XP", userDataClass.XP);
                insertCommand.Parameters.AddWithValue("@Level", userDataClass.Level);
                insertCommand.Parameters.AddWithValue("@CopperCoin", userDataClass.CopperCoin);
                insertCommand.Parameters.AddWithValue("@SilverCoin", userDataClass.SilverCoin);
                insertCommand.Parameters.AddWithValue("@GoldenCoin", userDataClass.GoldenCoin);
                insertCommand.Parameters.AddWithValue("@PlatinumCoin", userDataClass.PlatinumCoin);
                insertCommand.Parameters.AddWithValue("@MaxHP", userDataClass.MaxHP);
                insertCommand.Parameters.AddWithValue("@TempHP", userDataClass.TempHP);
                insertCommand.Parameters.AddWithValue("@TempMaxHPReduction", userDataClass.TempMaxHPReduction);
                insertCommand.Parameters.AddWithValue("@StrenghtForWeightMathf", userDataClass.StrenghtForWeightMathf);

                insertCommand.ExecuteNonQuery();
                return "Ваші дані було додано!";
            }
        }

         
         
         

        public static string DataBaseReader(long userId)
            => DataBaseReader(userId, GetActiveCharacterId(userId));

        public static string DataBaseReader(long userId, int characterId)
        {
            const string query = "SELECT * FROM UserData WHERE Id = @Id AND CharacterId = @CharacterId";
            Console.WriteLine($"[DEBUG] Requested CharacterId = {characterId}");


            if (!CheckifExist(userId, characterId))
                return "Not existing user";

            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            {
                command.Parameters.AddWithValue("@Id", userId);
                command.Parameters.AddWithValue("@CharacterId", characterId);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string name = reader["Name"].ToString();
                        int health = (int)reader["Health"];
                        int maxHP = (int)reader["MaxHP"];
                        int tempHP = (int)reader["TempHP"];
                        int tempMaxHPReduction = (int)reader["TempMaxHPReduction"];
                        int level = (int)reader["Level"];
                        int xp = (int)reader["XP"];
                        int armor = (int)reader["Armor"];
                        int strength = (int)reader["Strenght"];
                        int strengthValue = (int)reader["StrenghtForWeightMathf"];
                        int agility = (int)reader["Agility"];
                        int stamina = (int)reader["Stamina"];
                        int intelligence = (int)reader["Intelligence"];
                        int wisdom = (int)reader["Wisdom"];
                        int charisma = (int)reader["Charisma"];
                        int copperCoin = (int)reader["CopperCoin"];
                        int silverCoin = (int)reader["SilverCoin"];
                        int goldCoin = (int)reader["GoldenCoin"];
                        int platinumCoin = (int)reader["PlatinumCoin"];

                        double CopperCoin = copperCoin > 0 ? copperCoin * 0.00907 : 0;
                        double SilverCoin = silverCoin > 0 ? silverCoin * 0.00907 : 0;
                        double GoldCoin = goldCoin > 0 ? goldCoin * 0.00907 : 0;
                        double PlatinumCoin = platinumCoin > 0 ? platinumCoin * 0.00907 : 0;

                        double coinWeight = CopperCoin + SilverCoin + GoldCoin + PlatinumCoin;
                        double allWeight = coinWeight;

                        if (UserInventory.CheckifExist(userId, characterId))
                            allWeight += UserInventory.GetAllInventoryWeight(userId, characterId);

                        double allowedWeight = strengthValue * 15 / 2.205;
                        float fAllWeight = MathF.Round((float)allWeight, 2);
                        float fAllowedWeight = MathF.Round((float)allowedWeight, 2);

                        string healthDisplay = BuildHealthDisplay(health, maxHP, tempHP);
                        string reductionLine = tempMaxHPReduction > 0
                            ? $"\nТимчасове зниження HP: -{tempMaxHPReduction}"
                            : string.Empty;

                        string xpProgress = BuildXpProgressDisplay(level, xp);

                        return $"Ваш айді: {userId}\nІм'я персонажа: {name}\n\nУ вас зараз {healthDisplay} здоров'я{reductionLine}\n\n" +
                                $"У вас зараз {level} рівень\nУ вас зараз {xpProgress} одиниць досвіду\n\n" +
                                $"Модифікатори:\nСила: {strength}\nСпритність: {agility}\nВитривалість: {stamina}\n" +
                                $"Інтелект: {intelligence}\nМудрість: {wisdom}\nХаризма: {charisma}\n\n" +
                                $"Клас обладунків: {armor}\n\nЗагальна вага: {fAllWeight}/{fAllowedWeight} кг";
                    }
                }
            }

            return "";
        }

        public static string DataBaseWalletReader(long userId)
            => DataBaseWalletReader(userId, GetActiveCharacterId(userId));

        public static string DataBaseWalletReader(long userId, int characterId)
        {
            const string query = "SELECT * FROM UserData WHERE Id = @Id AND CharacterId = @CharacterId";

            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            {
                command.Parameters.AddWithValue("@Id", userId);
                command.Parameters.AddWithValue("@CharacterId", characterId);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int CopperCoin = reader.GetInt32(reader.GetOrdinal("CopperCoin"));
                        int SilverCoin = reader.GetInt32(reader.GetOrdinal("SilverCoin"));
                        int GoldCoin = reader.GetInt32(reader.GetOrdinal("GoldenCoin"));
                        int PlatinumCoin = reader.GetInt32(reader.GetOrdinal("PlatinumCoin"));

                        double _CopperCoin = CopperCoin > 0 ? CopperCoin * 0.00907 : 0;
                        double _SilverCoin = SilverCoin > 0 ? SilverCoin * 0.00907 : 0;
                        double _GoldCoin = GoldCoin > 0 ? GoldCoin * 0.00907 : 0;
                        double _PlatinumCoin = PlatinumCoin > 0 ? PlatinumCoin * 0.00907 : 0;

                        double coinWeight = _CopperCoin + _SilverCoin + _GoldCoin + _PlatinumCoin;
                        coinWeight = MathF.Round((float)coinWeight, 2);

                        return $"Мідних монет: {CopperCoin}\nСрібних монет: {SilverCoin}\nЗолотих монет: {GoldCoin}\nПлатинових монет: {PlatinumCoin}\nУсього вага: {MathF.Round((float)coinWeight, 2)} кг";
                    }
                }
            }

            return "";
        }

        public static double WeightReader(long userId)
            => WeightReader(userId, GetActiveCharacterId(userId));

        public static double WeightReader(long userId, int characterId)
        {
            const string query = "SELECT * FROM UserData WHERE Id = @Id AND CharacterId = @CharacterId";

            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            {
                command.Parameters.AddWithValue("@Id", userId);
                command.Parameters.AddWithValue("@CharacterId", characterId);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int CopperCoin = reader.GetInt32(reader.GetOrdinal("CopperCoin"));
                        int SilverCoin = reader.GetInt32(reader.GetOrdinal("SilverCoin"));
                        int GoldCoin = reader.GetInt32(reader.GetOrdinal("GoldenCoin"));
                        int PlatinumCoin = reader.GetInt32(reader.GetOrdinal("PlatinumCoin"));

                        double _CopperCoin = CopperCoin > 0 ? CopperCoin * 0.00907 : 0;
                        double _SilverCoin = SilverCoin > 0 ? SilverCoin * 0.00907 : 0;
                        double _GoldCoin = GoldCoin > 0 ? GoldCoin * 0.00907 : 0;
                        double _PlatinumCoin = PlatinumCoin > 0 ? PlatinumCoin * 0.00907 : 0;

                        double coinWeight = _CopperCoin + _SilverCoin + _GoldCoin + _PlatinumCoin;
                        return MathF.Round((float)coinWeight, 2);
                    }
                }
            }

            return 0;
        }

        public static string BuildHealthDisplay(int currentHp, int maxHp, int tempHp)
        {
            string baseHealth = FormatSignedHealth(currentHp);
            if (tempHp <= 0)
                return $"{baseHealth}/{maxHp}";

            string effectiveHealth = FormatSignedHealth(currentHp + tempHp);
            return $"{effectiveHealth}/{maxHp} (база: {baseHealth}/{maxHp}, тимчасові: +{tempHp})";
        }

        private static bool TryGetNextLevelXpRequirement(int level, out int nextLevelXp)
        {
            return LevelSystem.XpConsuption.TryGetValue(level + 1, out nextLevelXp);
        }

        private static string BuildXpProgressDisplay(int level, int xp)
        {
            if (TryGetNextLevelXpRequirement(level, out int nextLevelXp))
                return $"{xp}/{nextLevelXp}";

            return $"{xp}/MAX";
        }

        public static (int CurrentHp, int MaxHp, int TempHp, int TempMaxHpReduction) GetHealthSnapshot(long userId, int characterId)
        {
            const string query = @"
SELECT Health, MaxHP, TempHP, TempMaxHPReduction
FROM UserData
WHERE Id = @Id AND CharacterId = @CharacterId";

            using var command = new SqlCommand(query, sqlConnection);
            command.Parameters.AddWithValue("@Id", userId);
            command.Parameters.AddWithValue("@CharacterId", characterId);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
                return default;

            return (
                CurrentHp: reader.GetInt32(reader.GetOrdinal("Health")),
                MaxHp: reader.GetInt32(reader.GetOrdinal("MaxHP")),
                TempHp: reader.GetInt32(reader.GetOrdinal("TempHP")),
                TempMaxHpReduction: reader.GetInt32(reader.GetOrdinal("TempMaxHPReduction"))
            );
        }

        public static (int Added, int CurrentHp, int MaxHp, int TempHp) AddTemporaryHp(long userId, int characterId, int amount)
        {
            var state = GetHealthSnapshot(userId, characterId);
            if (amount <= 0)
                return (0, state.CurrentHp, state.MaxHp, state.TempHp);

            long sum = (long)state.TempHp + amount;
            int newTempHp = sum > int.MaxValue ? int.MaxValue : (int)sum;
            int added = newTempHp - state.TempHp;

            const string query = "UPDATE UserData SET TempHP=@TempHP WHERE Id=@Id AND CharacterId=@CharacterId";
            using var command = new SqlCommand(query, sqlConnection);
            command.Parameters.AddWithValue("@Id", userId);
            command.Parameters.AddWithValue("@CharacterId", characterId);
            command.Parameters.AddWithValue("@TempHP", newTempHp);
            command.ExecuteNonQuery();

            return (added, state.CurrentHp, state.MaxHp, newTempHp);
        }

        public static (int Removed, int CurrentHp, int MaxHp, int TempHp) ClearTemporaryHp(long userId, int characterId)
        {
            var state = GetHealthSnapshot(userId, characterId);
            const string query = "UPDATE UserData SET TempHP=0 WHERE Id=@Id AND CharacterId=@CharacterId";

            using var command = new SqlCommand(query, sqlConnection);
            command.Parameters.AddWithValue("@Id", userId);
            command.Parameters.AddWithValue("@CharacterId", characterId);
            command.ExecuteNonQuery();

            return (state.TempHp, state.CurrentHp, state.MaxHp, 0);
        }

        public static (int AppliedReduction, int TotalReduction, int CurrentHp, int MaxHp) ApplyTemporaryMaxHpReduction(long userId, int characterId, int amount)
        {
            var state = GetHealthSnapshot(userId, characterId);
            if (amount <= 0)
                return (0, state.TempMaxHpReduction, state.CurrentHp, state.MaxHp);

            int maxReducible = Math.Max(0, state.MaxHp - 1);
            int applied = Math.Min(amount, maxReducible);
            if (applied <= 0)
                return (0, state.TempMaxHpReduction, state.CurrentHp, state.MaxHp);

            int newMaxHp = state.MaxHp - applied;
            int newCurrentHp = state.CurrentHp - applied;
            if (newCurrentHp > newMaxHp)
                newCurrentHp = newMaxHp;

            long reductionSum = (long)state.TempMaxHpReduction + applied;
            int totalReduction = reductionSum > int.MaxValue ? int.MaxValue : (int)reductionSum;

            const string query = @"
UPDATE UserData
SET Health=@Health, MaxHP=@MaxHP, TempMaxHPReduction=@TempMaxHPReduction
WHERE Id=@Id AND CharacterId=@CharacterId";

            using var command = new SqlCommand(query, sqlConnection);
            command.Parameters.AddWithValue("@Id", userId);
            command.Parameters.AddWithValue("@CharacterId", characterId);
            command.Parameters.AddWithValue("@Health", newCurrentHp);
            command.Parameters.AddWithValue("@MaxHP", newMaxHp);
            command.Parameters.AddWithValue("@TempMaxHPReduction", totalReduction);
            command.ExecuteNonQuery();

            return (applied, totalReduction, newCurrentHp, newMaxHp);
        }

        public static (int Restored, int CurrentHp, int MaxHp) ResetTemporaryMaxHpReduction(long userId, int characterId)
        {
            var state = GetHealthSnapshot(userId, characterId);
            if (state.TempMaxHpReduction <= 0)
                return (0, state.CurrentHp, state.MaxHp);

            long rawMaxHp = (long)state.MaxHp + state.TempMaxHpReduction;
            long rawCurrentHp = (long)state.CurrentHp + state.TempMaxHpReduction;

            int newMaxHp = rawMaxHp > int.MaxValue ? int.MaxValue : (int)rawMaxHp;
            int newCurrentHp = rawCurrentHp > int.MaxValue ? int.MaxValue : (int)rawCurrentHp;
            if (newCurrentHp > newMaxHp)
                newCurrentHp = newMaxHp;

            const string query = @"
UPDATE UserData
SET Health=@Health, MaxHP=@MaxHP, TempMaxHPReduction=0
WHERE Id=@Id AND CharacterId=@CharacterId";

            using var command = new SqlCommand(query, sqlConnection);
            command.Parameters.AddWithValue("@Id", userId);
            command.Parameters.AddWithValue("@CharacterId", characterId);
            command.Parameters.AddWithValue("@Health", newCurrentHp);
            command.Parameters.AddWithValue("@MaxHP", newMaxHp);
            command.ExecuteNonQuery();

            return (state.TempMaxHpReduction, newCurrentHp, newMaxHp);
        }

        public static (int DamageToTempHp, int DamageToHealth, int CurrentHp, int MaxHp, int TempHp) ApplyDamageWithTemporaryHp(long userId, int characterId, int damage)
        {
            var state = GetHealthSnapshot(userId, characterId);
            int incomingDamage = Math.Max(0, damage);

            int damageToTempHp = Math.Min(state.TempHp, incomingDamage);
            int damageToHealth = incomingDamage - damageToTempHp;

            int newTempHp = state.TempHp - damageToTempHp;
            int newCurrentHp = state.CurrentHp - damageToHealth;

            const string query = @"
UPDATE UserData
SET Health=@Health, TempHP=@TempHP
WHERE Id=@Id AND CharacterId=@CharacterId";

            using var command = new SqlCommand(query, sqlConnection);
            command.Parameters.AddWithValue("@Id", userId);
            command.Parameters.AddWithValue("@CharacterId", characterId);
            command.Parameters.AddWithValue("@Health", newCurrentHp);
            command.Parameters.AddWithValue("@TempHP", newTempHp);
            command.ExecuteNonQuery();

            return (damageToTempHp, damageToHealth, newCurrentHp, state.MaxHp, newTempHp);
        }

        private static string FormatSignedHealth(int health)
            => health < 0 ? "\u2010" + Math.Abs(health) : health.ToString();

         
         
         

        public static void UpdateDataBaseValue(string updateValue, long userId, int changeToValue)
            => UpdateDataBaseValue(updateValue, userId, GetActiveCharacterId(userId), changeToValue);

        public static void UpdateDataBaseValue(string updateValue, long userId, int characterId, int changeToValue)
        {
             
            string query = $"UPDATE UserData SET {updateValue} = @{updateValue} WHERE Id = @Id AND CharacterId = @CharacterId";

            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            {
                command.Parameters.AddWithValue("@Id", userId);
                command.Parameters.AddWithValue("@CharacterId", characterId);
                command.Parameters.AddWithValue($"@{updateValue}", changeToValue);
                command.ExecuteNonQuery();
            }
        }

        public static T DataBaseValueReader<T>(long userId, string columnName)
            => DataBaseValueReader<T>(userId, GetActiveCharacterId(userId), columnName);

        public static T DataBaseValueReader<T>(long userId, int characterId, string columnName)
        {
            const string query = "SELECT * FROM UserData WHERE Id = @Id AND CharacterId = @CharacterId";

            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            {
                command.Parameters.AddWithValue("@Id", userId);
                command.Parameters.AddWithValue("@CharacterId", characterId);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        if (!reader.IsDBNull(reader.GetOrdinal(columnName)))
                        {
                            var value = reader[columnName];
                            if (value is T castedValue)
                                return castedValue;

                            return (T)Convert.ChangeType(value, typeof(T));
                        }
                    }
                }
            }

            return default;
        }

         
         
         

        public static List<UserDataClass> GetUsersFromDatabase()
        {
            var users = new List<UserDataClass>();

            const string query = "SELECT Id, CharacterId, Name, XP, Level FROM UserData";

            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var user = new UserDataClass
                    {
                        Id = reader.GetInt64(reader.GetOrdinal("Id")),
                        CharacterId = reader.GetInt32(reader.GetOrdinal("CharacterId")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        XP = reader.GetInt32(reader.GetOrdinal("XP")),
                        Level = reader.GetInt32(reader.GetOrdinal("Level")),
                    };

                    users.Add(user);
                }
            }

            return users;
        }

        ///



        public static string GetAllUsers()
        {
            const string query = "SELECT * FROM UserData";
            string result = "";

            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    long userId = (long)reader["Id"];
                    if (userId == masterID)
                        continue;

                    int charId = reader.GetInt32(reader.GetOrdinal("CharacterId"));
                    string name = reader["Name"].ToString();
                    int health = (int)reader["Health"];
                    int maxHP = (int)reader["MaxHP"];
                    int tempHP = (int)reader["TempHP"];
                    int tempMaxHPReduction = (int)reader["TempMaxHPReduction"];
                    int level = (int)reader["Level"];
                    int xp = (int)reader["XP"];
                    int armor = (int)reader["Armor"];
                    int strength = (int)reader["Strenght"];
                    int strengthValue = (int)reader["StrenghtForWeightMathf"];
                    int agility = (int)reader["Agility"];
                    int stamina = (int)reader["Stamina"];
                    int intelligence = (int)reader["Intelligence"];
                    int wisdom = (int)reader["Wisdom"];
                    int charisma = (int)reader["Charisma"];
                    int copperCoin = (int)reader["CopperCoin"];
                    int silverCoin = (int)reader["SilverCoin"];
                    int goldCoin = (int)reader["GoldenCoin"];
                    int platinumCoin = (int)reader["PlatinumCoin"];

                    double CopperCoin = copperCoin > 0 ? copperCoin * 0.00907 : 0;
                    double SilverCoin = silverCoin > 0 ? silverCoin * 0.00907 : 0;
                    double GoldCoin = goldCoin > 0 ? goldCoin * 0.00907 : 0;
                    double PlatinumCoin = platinumCoin > 0 ? platinumCoin * 0.00907 : 0;

                    double coinWeight = CopperCoin + SilverCoin + GoldCoin + PlatinumCoin;
                    double allWeight = coinWeight;

                    if (UserInventory.CheckifExist(userId, charId))
                        allWeight += UserInventory.GetAllInventoryWeight(userId, charId);

                    double allowedWeight = strengthValue * 15 / 2.205;
                    float fAllWeight = MathF.Round((float)allWeight, 2);
                    float fAllowedWeight = MathF.Round((float)allowedWeight, 2);

                    string healthDisplay = BuildHealthDisplay(health, maxHP, tempHP);
                    string reductionLine = tempMaxHPReduction > 0
                        ? $"\nТимчасове зниження HP: -{tempMaxHPReduction}"
                        : string.Empty;
                    string xpProgress = BuildXpProgressDisplay(level, xp);

                    result += $"Айді: {userId} (CharId: {charId})\nІм'я: {name}\nЗдоров'я: {healthDisplay}{reductionLine}\n" +
                              $"Рівень: {level}, Досвід: {xpProgress}\n" +
                              $"Модифікатори: Сила: {strength}, Спритність: {agility}, Витривалість: {stamina}, " +
                              $"Інтелект: {intelligence}, Мудрість: {wisdom}, Харизма: {charisma}\n" +
                              $"Клас обладунків: {armor}\n" +
                              $"Загальна вага: {fAllWeight}/{fAllowedWeight} кг\n\n";
                }
            }

            return string.IsNullOrEmpty(result) ? "У базі даних немає користувачів." : result;
        }
    }
}
