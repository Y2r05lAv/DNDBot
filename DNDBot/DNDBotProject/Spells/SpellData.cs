using DNDBot.DataBase;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Text;

namespace DNDBot.Spells
{
    public class SpellData : BotBodyRealisation
    {
        private static SqlConnection sqlConnection = BaseRepository.SqlConnection;

        ///



        public static int GetActiveCharacterId(long userId)
        {
            if (sqlConnection == null)
                throw new InvalidOperationException("Database connection is not initialized. Call DBInitialization() first.");

            const string query = @"SELECT ActiveCharacterId FROM UserSettings WHERE UserId = @UserId;";

            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            {
                command.Parameters.Add("@UserId", SqlDbType.BigInt).Value = userId;

                object result = command.ExecuteScalar();

                if (result == null || result == DBNull.Value)
                    throw new InvalidOperationException($"ActiveCharacterId is not set for userId={userId}. Initialize UserSettings first.");

                return Convert.ToInt32(result);
            }
        }

        private static int ResolveCharacterId(long userId, int? characterId)
        {
            if (characterId.HasValue && characterId.Value > 0)
                return characterId.Value;

            return GetActiveCharacterId(userId);
        }


        public static bool CheckifExist(long Id, int SpellId)
        {
            int characterId = ResolveCharacterId(Id, null);
            return CheckifExist(Id, characterId, SpellId);
        }

        public static bool CheckifExist(long Id, int characterId, int SpellId)
        {
            if (sqlConnection == null)
            {
                Console.WriteLine("SqlConnection - NullRefferenceExpection");
                return false;
            }

            const string query = "SELECT COUNT(*) FROM SpellBook WHERE Id = @Id AND CharacterId = @CharacterId AND SpellId = @SpellId";

            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            {
                command.Parameters.Add("@Id", SqlDbType.BigInt).Value = Id;
                command.Parameters.Add("@CharacterId", SqlDbType.Int).Value = characterId;
                command.Parameters.Add("@SpellId", SqlDbType.Int).Value = SpellId;

                int count = Convert.ToInt32(command.ExecuteScalar());
                return count > 0;
            }
        }

        public static string ClearUserData(long Id)
        {
            int characterId = ResolveCharacterId(Id, null);
            return ClearUserData(Id, characterId);
        }

        public static string ClearUserData(long Id, int characterId)
        {
            if (sqlConnection == null)
            {
                Console.WriteLine("SqlConnection - NullReferenceException");
                return "База даних не під'єднана";
            }

            string deleteQuery = "DELETE FROM SpellBook WHERE Id = @Id AND CharacterId = @CharacterId;";

            using (SqlCommand deleteCommand = new SqlCommand(deleteQuery, sqlConnection))
            {
                deleteCommand.Parameters.Add("@Id", SqlDbType.BigInt).Value = Id;
                deleteCommand.Parameters.Add("@CharacterId", SqlDbType.Int).Value = characterId;

                int rowsAffected = deleteCommand.ExecuteNonQuery();

                if (rowsAffected > 0)
                    return "Дані користувача очищені!";
                else
                    return "Немає даних для очищення.";
            }
        }

        public static string ClearUserItemData(long Id, long SpellId)
        {
            int characterId = ResolveCharacterId(Id, null);
            return ClearUserItemData(Id, characterId, SpellId);
        }

        public static string ClearUserItemData(long Id, int characterId, long SpellId)
        {
            if (sqlConnection == null)
            {
                Console.WriteLine("SqlConnection - NullReferenceException");
                return "База даних не під'єднана";
            }

            string updateQuery = "UPDATE SpellBook SET IsActive = 0 WHERE Id = @Id AND CharacterId = @CharacterId AND SpellId = @SpellId";

            using (SqlCommand updateCommand = new SqlCommand(updateQuery, sqlConnection))
            {
                updateCommand.Parameters.Add("@Id", SqlDbType.BigInt).Value = Id;
                updateCommand.Parameters.Add("@CharacterId", SqlDbType.Int).Value = characterId;
                updateCommand.Parameters.Add("@SpellId", SqlDbType.BigInt).Value = SpellId;

                int rowsAffected = updateCommand.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    Console.WriteLine($"SpellId '{SpellId}' for user '{Id}' (char '{characterId}') deactivated.");
                    return "Ефект деактивовано!";
                }

                return "Ефект не знайдено.";
            }
        }



        public static void ClearAllSpellData(bool IsDeleting = false)
        {
            if (sqlConnection == null)
            {
                Console.WriteLine("SqlConnection - NullRefferenceExpection");
                return;
            }


            string query = "DELETE FROM SpellBook";

            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            {
                Console.WriteLine("To delete write: 'CONFIRM' else words will stop program");
            }

        }

        public static void DeletingAllSpellData()
        {
            if (sqlConnection == null)
            {
                Console.WriteLine("SqlConnection - NullRefferenceExpection");
                return;
            }


            string query = "DELETE FROM SpellBook";

            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            {
                command.ExecuteNonQuery();
            }

             
             
             
             

            Console.WriteLine("All DATA was deleted from SpellBook");
        }


         
         
         
         
         
         

         
         
         

         
         

         
         

        public static string AddOrUpdateSpellBook(SpellDataClass spellDataClass, int IsActive = 1)
        {
            Console.WriteLine("AddOrUpdateSpellBook");

            if (sqlConnection == null)
                return "База даних не під'єднана";

            int characterId = ResolveCharacterId(spellDataClass.UserId, spellDataClass.CharacterId);

            string checkInactiveItemQuery = "SELECT TOP 1 SpellId FROM SpellBook WHERE Id = @Id AND CharacterId = @CharacterId AND IsActive = 0";
            string checkActiveItemQuery = "SELECT TOP 1 SpellId FROM SpellBook WHERE Id = @Id AND CharacterId = @CharacterId AND SpellId = @SpellId AND IsActive = 1";

            string updateQuery = @"
    UPDATE SpellBook
    SET Name = @Name,
        Description = @Description,
        TypeHarm = @TypeHarm,
        Type = @Type,
        Distance = @Distance,
        Target = @Target,
        DamageDice = @DamageDice,
        Modifier = @Modifier,
        ManaCost = @ManaCost,
        IsActive = @IsActive
    WHERE Id = @Id AND CharacterId = @CharacterId AND SpellId = @SpellId;";

            string insertQuery = @"
    INSERT INTO SpellBook (Id, CharacterId, SpellId, Name, Description, TypeHarm, Type, Distance, Target, DamageDice, Modifier, ManaCost, IsActive)
    SELECT @Id,
           @CharacterId,
           ISNULL(MAX(SpellId), 0) + 1,
           @Name, @Description, @TypeHarm, @Type, @Distance, @Target, @DamageDice, @Modifier, @ManaCost, @IsActive
    FROM SpellBook WHERE Id = @Id AND CharacterId = @CharacterId;";

            try
            {
                int spellIdToUse = -1;

                 
                using (SqlCommand checkInactiveCommand = new SqlCommand(checkInactiveItemQuery, sqlConnection))
                {
                    checkInactiveCommand.Parameters.Add("@Id", SqlDbType.BigInt).Value = spellDataClass.UserId;
                    checkInactiveCommand.Parameters.Add("@CharacterId", SqlDbType.Int).Value = characterId;

                    object result = checkInactiveCommand.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                        spellIdToUse = Convert.ToInt32(result);
                }

                 
                if (spellIdToUse == -1 && spellDataClass.SpellId > 0)
                {
                    using (SqlCommand checkActiveCommand = new SqlCommand(checkActiveItemQuery, sqlConnection))
                    {
                        checkActiveCommand.Parameters.Add("@Id", SqlDbType.BigInt).Value = spellDataClass.UserId;
                        checkActiveCommand.Parameters.Add("@CharacterId", SqlDbType.Int).Value = characterId;
                        checkActiveCommand.Parameters.Add("@SpellId", SqlDbType.Int).Value = spellDataClass.SpellId;

                        object activeResult = checkActiveCommand.ExecuteScalar();
                        if (activeResult != null && activeResult != DBNull.Value)
                            spellIdToUse = Convert.ToInt32(activeResult);
                    }
                }

                 
                if (spellIdToUse != -1)
                {
                    using (SqlCommand updateCommand = new SqlCommand(updateQuery, sqlConnection))
                    {
                        updateCommand.Parameters.Add("@Id", SqlDbType.BigInt).Value = spellDataClass.UserId;
                        updateCommand.Parameters.Add("@CharacterId", SqlDbType.Int).Value = characterId;
                        updateCommand.Parameters.Add("@SpellId", SqlDbType.Int).Value = spellIdToUse;
                        updateCommand.Parameters.Add("@ManaCost", SqlDbType.Int).Value = spellDataClass.ManaCost;

                        updateCommand.Parameters.Add("@Name", SqlDbType.NVarChar, 50).Value = spellDataClass.Name ?? "";
                        updateCommand.Parameters.Add("@Description", SqlDbType.NVarChar, -1).Value = spellDataClass.Description ?? "";
                        updateCommand.Parameters.Add("@TypeHarm", SqlDbType.NVarChar, 50).Value = spellDataClass.TypeHarm ?? "";
                        updateCommand.Parameters.Add("@Type", SqlDbType.NVarChar, 50).Value = spellDataClass.SpellType ?? "";
                        updateCommand.Parameters.Add("@Distance", SqlDbType.NVarChar, 50).Value = spellDataClass.Distance ?? "";
                        updateCommand.Parameters.Add("@Target", SqlDbType.NVarChar, 50).Value = spellDataClass.Target ?? "";
                        updateCommand.Parameters.Add("@DamageDice", SqlDbType.NVarChar, 50).Value = spellDataClass.DamageDice ?? "";
                        updateCommand.Parameters.Add("@Modifier", SqlDbType.NVarChar, 50).Value = spellDataClass.Modifier ?? "";
                        updateCommand.Parameters.Add("@IsActive", SqlDbType.Bit).Value = IsActive;

                        int rows = updateCommand.ExecuteNonQuery();
                        return rows > 0 ? "Ефект оновлено!" : "Не вдалося оновити ефект.";
                    }
                }

                 
                using (SqlCommand insertCommand = new SqlCommand(insertQuery, sqlConnection))
                {
                    insertCommand.Parameters.Add("@Id", SqlDbType.BigInt).Value = spellDataClass.UserId;
                    insertCommand.Parameters.Add("@CharacterId", SqlDbType.Int).Value = characterId;

                    insertCommand.Parameters.Add("@Name", SqlDbType.NVarChar, 50).Value = spellDataClass.Name ?? "";
                    insertCommand.Parameters.Add("@Description", SqlDbType.NVarChar, -1).Value = spellDataClass.Description ?? "";
                    insertCommand.Parameters.Add("@TypeHarm", SqlDbType.NVarChar, 50).Value = spellDataClass.TypeHarm ?? "";
                    insertCommand.Parameters.Add("@Type", SqlDbType.NVarChar, 50).Value = spellDataClass.SpellType ?? "";
                    insertCommand.Parameters.Add("@Distance", SqlDbType.NVarChar, 50).Value = spellDataClass.Distance ?? "";
                    insertCommand.Parameters.Add("@Target", SqlDbType.NVarChar, 50).Value = spellDataClass.Target ?? "";
                    insertCommand.Parameters.Add("@DamageDice", SqlDbType.NVarChar, 50).Value = spellDataClass.DamageDice ?? "";
                    insertCommand.Parameters.Add("@Modifier", SqlDbType.NVarChar, 50).Value = spellDataClass.Modifier ?? "";
                    insertCommand.Parameters.Add("@ManaCost", SqlDbType.Int).Value = spellDataClass.ManaCost;
                    insertCommand.Parameters.Add("@IsActive", SqlDbType.Bit).Value = IsActive;

                    insertCommand.ExecuteNonQuery();
                    return "Новий ефект додано!";
                }
            }
            catch (SqlException ex)
            {
                return $"Сталася помилка: {ex.Message}";
            }
        }

        public static string DeactivateSpell(long userId, int spellId)
        {
            int characterId = ResolveCharacterId(userId, null);
            return DeactivateSpell(userId, characterId, spellId);
        }

        public static string DeactivateSpell(long userId, int characterId, int spellId)
        {
            string query = "UPDATE SpellBook SET IsActive = 0 WHERE Id = @Id AND CharacterId = @CharacterId AND SpellId = @SpellId";

            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            {
                command.Parameters.Add("@Id", SqlDbType.BigInt).Value = userId;
                command.Parameters.Add("@CharacterId", SqlDbType.Int).Value = characterId;
                command.Parameters.Add("@SpellId", SqlDbType.Int).Value = spellId;

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected > 0)
                    return "Ефект було успішно деактивовано!";

                return "Ефект не знайдено або вже деактивовано.";
            }
        }





        public static List<SpellDataClass> SpellDataBaseReaderList(long userId)
        {
            int characterId = ResolveCharacterId(userId, null);
            return SpellDataBaseReaderList(userId, characterId);
        }

        public static List<SpellDataClass> SpellDataBaseReaderList(long userId, int characterId)
        {
            var userSpells = new List<SpellDataClass>();

            if (sqlConnection == null)
                return userSpells;

            const string query = @"
SELECT
    Id,
    CharacterId,
    SpellId,
    Name,
    Description,
    TypeHarm,
    [Type],
    Distance,
    Target,
    DamageDice,
    Modifier,
    ManaCost,
    IsActive
FROM SpellBook
WHERE Id = @Id AND CharacterId = @CharacterId AND IsActive = 1
ORDER BY SpellId;";

            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            {
                command.Parameters.Add("@Id", SqlDbType.BigInt).Value = userId;
                command.Parameters.Add("@CharacterId", SqlDbType.Int).Value = characterId;

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        userSpells.Add(new SpellDataClass
                        {
                            UserId = (long)reader["Id"],
                            CharacterId = (int)reader["CharacterId"],
                            SpellId = (int)reader["SpellId"],
                            Name = (string)reader["Name"],
                            Description = (string)reader["Description"],
                            TypeHarm = (string)reader["TypeHarm"],
                            SpellType = (string)reader["Type"],       
                            Distance = (string)reader["Distance"],
                            Target = (string)reader["Target"],
                            DamageDice = (string)reader["DamageDice"],
                            Modifier = ReturnCharacterModificatorNameEnglishToUkrainian((string)reader["Modifier"]),
                            ManaCost = reader["ManaCost"] != DBNull.Value ? Convert.ToInt32(reader["ManaCost"]) : 0,
                            IsActive = (int)reader["IsActive"]
                        });
                    }
                }
            }

            return userSpells;
        }


        public static string FormatSpellFull(SpellDataClass s)
        {
            return
                    $@"🧩 Ефект

            Айді персонажа: {s.CharacterId}
            ID ефекту: {s.SpellId}

            Ім'я: {s.Name}
            Тип шкоди: {s.TypeHarm}
            Тип ефекту: {s.SpellType}
            Дистанція: {s.Distance}
            Ціль: {s.Target}

            Вартість мани: {(s.ManaCost > 0 ? s.ManaCost.ToString() : "—")}

            Урон: {(!string.IsNullOrWhiteSpace(s.DamageDice) ? s.DamageDice : "—")}
            Модифікатор: {(!string.IsNullOrWhiteSpace(s.Modifier) ? s.Modifier : "—")}

            Опис:
            {s.Description}

            Статус: {(s.IsActive == 1 ? "✅ Активне" : "❌ Неактивне")}";
        }



        public static string SpellDataGetSpecialObjectInfo(long userId, long SpellId, string returnDataInfo = "")
        {
            int characterId = ResolveCharacterId(userId, null);
            return SpellDataGetSpecialObjectInfo(userId, characterId, SpellId, returnDataInfo);
        }

        public static string SpellDataGetSpecialObjectInfo(long userId, int characterId, long SpellId, string returnDataInfo = "")
        {
            string query = $"SELECT {returnDataInfo} FROM SpellBook WHERE Id = @Id AND CharacterId = @CharacterId AND SpellId = @SpellId AND IsActive = 1";

            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            {
                command.Parameters.Add("@Id", SqlDbType.BigInt).Value = userId;
                command.Parameters.Add("@CharacterId", SqlDbType.Int).Value = characterId;
                command.Parameters.Add("@SpellId", SqlDbType.BigInt).Value = SpellId;

                object result = command.ExecuteScalar();
                return result?.ToString() ?? "";
            }
        }




        public static void UpdateDataBaseValue(string updateValue, long userID, int changeToValue)
        {
            int characterId = ResolveCharacterId(userID, null);
            UpdateDataBaseValue(updateValue, userID, characterId, changeToValue);
        }

        public static void UpdateDataBaseValue(string updateValue, long userID, int characterId, int changeToValue)
        {
            string query = $"UPDATE SpellBook SET {updateValue} = @{updateValue} WHERE Id = @Id AND CharacterId = @CharacterId";

            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            {
                command.Parameters.AddWithValue("@Id", userID);
                command.Parameters.AddWithValue("@CharacterId", characterId);
                command.Parameters.AddWithValue($"@{updateValue}", changeToValue);

                command.ExecuteNonQuery();
            }
        }

        public static T DataBaseValueReader<T>(long userId, long SpellId, string columnName)
        {
            int characterId = ResolveCharacterId(userId, null);
            return DataBaseValueReader<T>(userId, characterId, SpellId, columnName);
        }

        public static T DataBaseValueReader<T>(long userId, int characterId, long SpellId, string columnName)
        {
            string query = "SELECT * FROM SpellBook WHERE Id = @Id AND CharacterId = @CharacterId AND SpellId = @SpellId";

            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            {
                command.Parameters.Add("@Id", SqlDbType.BigInt).Value = userId;
                command.Parameters.Add("@CharacterId", SqlDbType.Int).Value = characterId;
                command.Parameters.Add("@SpellId", SqlDbType.BigInt).Value = SpellId;

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        if (!reader.IsDBNull(reader.GetOrdinal(columnName)))
                        {
                            return (T)Convert.ChangeType(reader[columnName], typeof(T));
                        }
                    }
                }
            }

            return default;
        }

        public static List<SpellDataClass> GetSpellsFromDatabase(long userId)
        {
            int characterId = ResolveCharacterId(userId, null);
            return GetSpellsFromDatabase(userId, characterId);
        }

        public static List<SpellDataClass> GetSpellsFromDatabase(long userId, int characterId)
        {
            var spells = new List<SpellDataClass>();

            string query = "SELECT DISTINCT Id, CharacterId, SpellId, Name FROM SpellBook WHERE Id = @Id AND CharacterId = @CharacterId AND IsActive = 1";

            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            {
                command.Parameters.Add("@Id", SqlDbType.BigInt).Value = userId;
                command.Parameters.Add("@CharacterId", SqlDbType.Int).Value = characterId;

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        spells.Add(new SpellDataClass
                        {
                            UserId = Convert.ToInt64(reader["Id"]),
                            CharacterId = Convert.ToInt32(reader["CharacterId"]),
                            SpellId = Convert.ToInt32(reader["SpellId"]),
                            Name = reader["Name"].ToString()
                        });
                    }
                }
            }

            return spells;
        }


        public static string GetSpellsFromDatabaseMaster(long userId)
        {
            int characterId = ResolveCharacterId(userId, null);
            return GetSpellsFromDatabaseMaster(userId, characterId);
        }

        public static string GetSpellsFromDatabaseMaster(long userId, int characterId)
        {
            if (sqlConnection == null)
                return "";

            StringBuilder sb = new StringBuilder();

            string query = @"
SELECT SpellId, Name, Description, TypeHarm, Type, Distance, Target, DamageDice, Modifier, ManaCost
FROM SpellBook
WHERE Id = @Id AND CharacterId = @CharacterId AND IsActive = 1
ORDER BY SpellId;";

            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            {
                command.Parameters.Add("@Id", SqlDbType.BigInt).Value = userId;
                command.Parameters.Add("@CharacterId", SqlDbType.Int).Value = characterId;

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int spellId = reader.GetInt32(reader.GetOrdinal("SpellId"));
                        string spellName = reader["Name"]?.ToString() ?? "";
                        string description = reader["Description"]?.ToString() ?? "";
                        string typeHarm = reader["TypeHarm"]?.ToString() ?? "";
                        string type = reader["Type"]?.ToString() ?? "";
                        string distance = reader["Distance"]?.ToString() ?? "";
                        string target = reader["Target"]?.ToString() ?? "";
                        string damageDice = reader["DamageDice"]?.ToString() ?? "";
                        string modifier = reader["Modifier"]?.ToString() ?? "";
                        string manaCost = reader["ManaCost"] != DBNull.Value ? reader["ManaCost"].ToString() : "0";

                        sb.AppendLine();
                        sb.AppendLine($"ID ефекту: {spellId}");
                        sb.AppendLine($"Ім'я ефекту: {spellName}");
                        sb.AppendLine($"Опис: {description}");
                        sb.AppendLine($"Тип шкоди: {typeHarm}");
                        sb.AppendLine($"Тип ефекту: {type}");
                        sb.AppendLine($"Дистанція: {distance}");
                        sb.AppendLine($"Ціль: {target}");
                        sb.AppendLine($"Кістки шкоди: {damageDice}");
                        sb.AppendLine($"Модифікатор: {modifier}");
                        sb.AppendLine($"Ціна мани: {manaCost}");
                    }
                }
            }

            return sb.ToString();
        }

        public static SpellDataClass GetSpellToClass(long userId, int SpellId)
        {
            int characterId = ResolveCharacterId(userId, null);
            return GetSpellToClass(userId, characterId, SpellId);
        }

        public static SpellDataClass GetSpellToClass(long userId, int characterId, int SpellId)
        {
            SpellDataClass returnValue = null;

            string query = @"SELECT Name, Description, TypeHarm, Type, Distance, Target, DamageDice, Modifier, ManaCost, IsActive 
                     FROM SpellBook 
                     WHERE Id = @Id AND CharacterId = @CharacterId AND SpellId = @SpellId AND IsActive = 1";

            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            {
                command.Parameters.Add("@Id", SqlDbType.BigInt).Value = userId;
                command.Parameters.Add("@CharacterId", SqlDbType.Int).Value = characterId;
                command.Parameters.Add("@SpellId", SqlDbType.Int).Value = SpellId;

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        returnValue = new SpellDataClass
                        {
                            UserId = userId,
                            CharacterId = characterId,
                            SpellId = SpellId,
                            Name = reader["Name"]?.ToString(),
                            Description = reader["Description"]?.ToString(),
                            TypeHarm = reader["TypeHarm"]?.ToString(),
                            SpellType = reader["Type"]?.ToString(),
                            Distance = reader["Distance"]?.ToString(),
                            Target = reader["Target"]?.ToString(),
                            DamageDice = reader["DamageDice"]?.ToString(),
                            Modifier = reader["Modifier"]?.ToString(),
                            ManaCost = reader["ManaCost"] != DBNull.Value ? Convert.ToInt32(reader["ManaCost"]) : 0,
                            IsActive = (int)reader["IsActive"]

                        };
                    }
                }
            }

            return returnValue;
        }

        public static string UpdateDamage(long userId, int spellId, string damage)
        {
            int characterId = ResolveCharacterId(userId, null);  

            if (sqlConnection == null)
                return "";

            const string query = @"
UPDATE SpellBook
SET DamageDice = @DamageDice
WHERE Id = @Id AND CharacterId = @CharacterId AND SpellId = @SpellId;";

            using var cmd = new SqlCommand(query, sqlConnection);
            cmd.Parameters.Add("@DamageDice", SqlDbType.NVarChar, 64).Value = damage ?? "";
            cmd.Parameters.Add("@Id", SqlDbType.BigInt).Value = userId;
            cmd.Parameters.Add("@CharacterId", SqlDbType.Int).Value = characterId;
            cmd.Parameters.Add("@SpellId", SqlDbType.Int).Value = spellId;

            int rows = cmd.ExecuteNonQuery();
            return rows > 0 ? "✅ Урон оновлено." : "⚠️ Ефект не знайдено.";
        }




    }
}
