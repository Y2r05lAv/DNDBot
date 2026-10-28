using DNDBot.DataBase;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Text;

namespace DNDBot
{
    ///



    public class UserInventory : BotBodyRealisation
    {
        private static SqlConnection sqlConnection = BaseRepository.SqlConnection;

         
         
         
        public static int GetActiveCharacterId(long userId)
        {
            if (sqlConnection == null)
                throw new InvalidOperationException("SqlConnection - NullReferenceException");

            const string q = @"SELECT ActiveCharacterId FROM UserSettings WHERE UserId = @UserId";
            using var cmd = new SqlCommand(q, sqlConnection);
            cmd.Parameters.Add("@UserId", SqlDbType.BigInt).Value = userId;
            object res = cmd.ExecuteScalar();
            if (res == null || res == DBNull.Value)
                throw new Exception($"ActiveCharacterId is NULL for user {userId}. Initialize UserSettings first.");
            return Convert.ToInt32(res);
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

            const string query = "SELECT COUNT(*) FROM UserInventory WHERE Id = @Id AND CharacterId = @CharacterId";
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
                Console.WriteLine("SqlConnection - NullReferenceException");
                return "База даних не під'єднана";
            }

            const string deleteQuery = "DELETE FROM UserInventory WHERE Id = @Id;";
            using (SqlCommand deleteCommand = new SqlCommand(deleteQuery, sqlConnection))
            {
                deleteCommand.Parameters.AddWithValue("@Id", id);
                int rowsAffected = deleteCommand.ExecuteNonQuery();

                if (rowsAffected > 0)
                    return $"Дані для гравця з ID '{id}' були успішно видалені!";

                return "Виникла помилка видалення: Гравець відсутній";
            }
        }

        public static string ClearUserData(long id, int characterId)
        {
            if (sqlConnection == null)
            {
                Console.WriteLine("SqlConnection - NullReferenceException");
                return "База даних не під'єднана";
            }

            const string deleteQuery = "DELETE FROM UserInventory WHERE Id = @Id AND CharacterId = @CharacterId;";
            using var deleteCommand = new SqlCommand(deleteQuery, sqlConnection);
            deleteCommand.Parameters.AddWithValue("@Id", id);
            deleteCommand.Parameters.AddWithValue("@CharacterId", characterId);
            int rowsAffected = deleteCommand.ExecuteNonQuery();

            if (rowsAffected > 0)
                return $"Інвентар персонажа (CharId={characterId}) для гравця '{id}' був успішно видалений!";

            return "Виникла помилка видалення: Персонаж або інвентар відсутній";
        }

        public static string ClearUserItemData(long id, long itemId)
            => ClearUserItemData(id, GetActiveCharacterId(id), itemId);

        public static string ClearUserItemData(long id, int characterId, long itemId)
        {
            if (sqlConnection == null)
            {
                Console.WriteLine("SqlConnection - NullReferenceException");
                return "База даних не під'єднана";
            }

             
            const string updateQuery = "UPDATE UserInventory SET IsActive = 0 WHERE Id = @Id AND CharacterId = @CharacterId AND ItemId = @ItemId";
            using (SqlCommand updateCommand = new SqlCommand(updateQuery, sqlConnection))
            {
                updateCommand.Parameters.AddWithValue("@Id", id);
                updateCommand.Parameters.AddWithValue("@CharacterId", characterId);
                updateCommand.Parameters.AddWithValue("@ItemId", itemId);

                int rowsAffected = updateCommand.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    string msg = $"Предмет з ItemId '{itemId}' для гравця з ID '{id}' (CharId={characterId}) був успішно помічений як неактивний!";
                    Console.WriteLine(msg);
                    return msg;
                }

                return "Виникла помилка: Гравець або предмет не знайдений.";
            }
        }

        public static void ClearAllInventoryData(bool isDeleting = false)
        {
            if (sqlConnection == null)
            {
                Console.WriteLine("SqlConnection - NullRefferenceExpection");
                return;
            }

            string query = "DELETE FROM UserInventory";
            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            {
                Console.WriteLine("To delete write: 'CONFIRM' else words will stop program");
            }
        }

        public static void DeletingAllInventoryData()
        {
            if (sqlConnection == null)
            {
                Console.WriteLine("SqlConnection - NullRefferenceExpection");
                return;
            }

            string query = "DELETE FROM UserInventory";
            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            {
                command.ExecuteNonQuery();
            }

            Console.WriteLine("All DATA was deleted from UserInventory");
        }

         
         
         
        public static string AddOrUpdateUserInventory(InventoryDataClass data, int isActive = 1)
        {
            if (sqlConnection == null)
                return "База даних не під'єднана";

            int characterId = data.CharacterId;
            if (characterId <= 0)
                characterId = GetActiveCharacterId(data.UserId);

             
            const string checkInactiveItemQuery = @"SELECT TOP 1 ItemId FROM UserInventory WHERE Id = @Id AND CharacterId = @CharacterId AND IsActive = 0";

             
            const string checkActiveItemQuery = @"SELECT TOP 1 ItemId FROM UserInventory WHERE Id = @Id AND CharacterId = @CharacterId AND ItemId = @ItemId AND IsActive = 1";

             
            const string updateQuery = @"
UPDATE UserInventory
SET ItemAmount = @ItemAmount,
    ItemWeight = @ItemWeight,
    ItemName = @ItemName,
    AllWeight = @AllWeight,
    IsActive = @IsActive
WHERE Id = @Id AND CharacterId = @CharacterId AND ItemId = @ItemId";

             
            const string insertQuery = @"
INSERT INTO UserInventory (Id, CharacterId, ItemId, ItemName, ItemAmount, ItemWeight, AllWeight, IsActive)
SELECT @Id,
       @CharacterId,
       ISNULL(MAX(ItemId), 0) + 1,
       @ItemName, @ItemAmount, @ItemWeight, @AllWeight, @IsActive
FROM UserInventory
WHERE Id = @Id AND CharacterId = @CharacterId";

            try
            {
                int itemIdToUse = -1;

                using (SqlCommand checkInactiveCommand = new SqlCommand(checkInactiveItemQuery, sqlConnection))
                {
                    checkInactiveCommand.Parameters.Add("@Id", SqlDbType.BigInt).Value = data.UserId;
                    checkInactiveCommand.Parameters.Add("@CharacterId", SqlDbType.Int).Value = characterId;
                    object result = checkInactiveCommand.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                        itemIdToUse = (int)result;
                }

                if (itemIdToUse == -1)
                {
                    using (SqlCommand checkActiveCommand = new SqlCommand(checkActiveItemQuery, sqlConnection))
                    {
                        checkActiveCommand.Parameters.Add("@Id", SqlDbType.BigInt).Value = data.UserId;
                        checkActiveCommand.Parameters.Add("@CharacterId", SqlDbType.Int).Value = characterId;
                        checkActiveCommand.Parameters.Add("@ItemId", SqlDbType.Int).Value = data.ItemId;
                        object activeResult = checkActiveCommand.ExecuteScalar();
                        if (activeResult != null && activeResult != DBNull.Value)
                            itemIdToUse = (int)activeResult;
                    }
                }

                double allWeight = Math.Round(data.ItemWeight * data.ItemAmount, 2);

                if (itemIdToUse != -1)
                {
                    using (SqlCommand updateCommand = new SqlCommand(updateQuery, sqlConnection))
                    {
                        updateCommand.Parameters.Add("@Id", SqlDbType.BigInt).Value = data.UserId;
                        updateCommand.Parameters.Add("@CharacterId", SqlDbType.Int).Value = characterId;
                        updateCommand.Parameters.Add("@ItemId", SqlDbType.Int).Value = itemIdToUse;
                        updateCommand.Parameters.Add("@ItemName", SqlDbType.NVarChar, 50).Value = data.ItemName;
                        updateCommand.Parameters.Add("@ItemAmount", SqlDbType.Int).Value = data.ItemAmount;
                        updateCommand.Parameters.Add("@ItemWeight", SqlDbType.Float).Value = Math.Round(data.ItemWeight, 2);
                        updateCommand.Parameters.Add("@AllWeight", SqlDbType.Float).Value = allWeight;
                        updateCommand.Parameters.Add("@IsActive", SqlDbType.TinyInt).Value = isActive;
                        updateCommand.ExecuteNonQuery();
                        return "Предмет оновлено!";
                    }
                }

                using (SqlCommand insertCommand = new SqlCommand(insertQuery, sqlConnection))
                {
                    insertCommand.Parameters.Add("@Id", SqlDbType.BigInt).Value = data.UserId;
                    insertCommand.Parameters.Add("@CharacterId", SqlDbType.Int).Value = characterId;
                    insertCommand.Parameters.Add("@ItemName", SqlDbType.NVarChar, 50).Value = data.ItemName;
                    insertCommand.Parameters.Add("@ItemAmount", SqlDbType.Int).Value = data.ItemAmount;
                    insertCommand.Parameters.Add("@ItemWeight", SqlDbType.Float).Value = Math.Round(data.ItemWeight, 2);
                    insertCommand.Parameters.Add("@AllWeight", SqlDbType.Float).Value = allWeight;
                    insertCommand.Parameters.Add("@IsActive", SqlDbType.TinyInt).Value = isActive;
                    insertCommand.ExecuteNonQuery();
                    return "Новий предмет додано!";
                }
            }
            catch (SqlException ex)
            {
                return $"Сталася помилка: {ex.Message}";
            }
        }

         
         
         

        public static string InventoryDataBaseReader(long userId, string returnDataInfo = "")
            => InventoryDataBaseReader(userId, GetActiveCharacterId(userId), returnDataInfo);

        public static string InventoryDataBaseReader(long userId, int characterId, string returnDataInfo = "")
        {
            const string query = "SELECT * FROM UserInventory WHERE Id = @Id AND CharacterId = @CharacterId AND IsActive = 1";
            StringBuilder result = new StringBuilder();

            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            {
                command.Parameters.AddWithValue("@Id", userId);
                command.Parameters.AddWithValue("@CharacterId", characterId);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int itemId = reader.GetInt32(reader.GetOrdinal("ItemId"));
                        string itemName = reader.GetString(reader.GetOrdinal("ItemName"));
                        int itemAmount = reader.GetInt32(reader.GetOrdinal("ItemAmount"));
                        double itemWeight = reader.GetDouble(reader.GetOrdinal("ItemWeight"));
                        double itemAllWeight = reader.GetDouble(reader.GetOrdinal("AllWeight"));
                        result.AppendLine($"\n\nАйді предмета: {itemId}\nІм'я предмета: {itemName}\nКількість: {itemAmount}\nВага: {itemWeight}\nЗагальна у кг вага: {itemAllWeight}");
                    }
                }
            }

            return result.Length > 0 ? result.ToString() : "\nУ користувача немає предметів в інвентарі.";
        }

        public static string InventoryDataGetSpecialObjectInfo(long userId, long itemId, string returnDataInfo = "")
            => InventoryDataGetSpecialObjectInfo(userId, GetActiveCharacterId(userId), itemId, returnDataInfo);

        public static string InventoryDataGetSpecialObjectInfo(long userId, int characterId, long itemId, string returnDataInfo = "")
        {
            const string query = "SELECT * FROM UserInventory WHERE Id = @Id AND CharacterId = @CharacterId AND ItemId = @ItemId AND IsActive = 1";
            StringBuilder result = new StringBuilder();

            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            {
                command.Parameters.AddWithValue("@Id", userId);
                command.Parameters.AddWithValue("@CharacterId", characterId);
                command.Parameters.AddWithValue("@ItemId", itemId);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string itemName = reader.GetString(reader.GetOrdinal("ItemName"));
                        int itemAmount = reader.GetInt32(reader.GetOrdinal("ItemAmount"));
                        double itemWeight = reader.GetDouble(reader.GetOrdinal("ItemWeight"));
                        double itemAllWeight = reader.GetDouble(reader.GetOrdinal("AllWeight"));
                        result.AppendLine($"\n\nАйді предмета: {itemId}\nІм'я предмета: {itemName}\nКількість: {itemAmount}\nВага одиниці: {itemWeight}\nЗагальна у кг вага: {itemAllWeight}");
                    }
                }
            }

            return result.Length > 0 ? result.ToString() : "У користувача немає такого предмету!";
        }

         
         
         
        public static void UpdateDataBaseValue(string updateValue, long userId, int changeToValue)
            => UpdateDataBaseValue(updateValue, userId, GetActiveCharacterId(userId), changeToValue);

        public static void UpdateDataBaseValue(string updateValue, long userId, int characterId, int changeToValue)
        {
            string query = $"UPDATE UserInventory SET {updateValue} = @{updateValue} WHERE Id = @Id AND CharacterId = @CharacterId";
            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            {
                command.Parameters.AddWithValue("@Id", userId);
                command.Parameters.AddWithValue("@CharacterId", characterId);
                command.Parameters.AddWithValue($"@{updateValue}", changeToValue);
                command.ExecuteNonQuery();
            }
        }

        public static T DataBaseValueReader<T>(long userId, long itemId, string columnName)
            => DataBaseValueReader<T>(userId, GetActiveCharacterId(userId), itemId, columnName);

        public static T DataBaseValueReader<T>(long userId, int characterId, long itemId, string columnName)
        {
            const string query = "SELECT * FROM UserInventory WHERE Id = @Id AND CharacterId = @CharacterId AND ItemId = @ItemId";
            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            {
                command.Parameters.AddWithValue("@Id", userId);
                command.Parameters.AddWithValue("@CharacterId", characterId);
                command.Parameters.AddWithValue("@ItemId", itemId);

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

         
         
         
        public static List<InventoryDataClass> GetUsersFromDatabase()
        {
            var users = new List<InventoryDataClass>();
            const string query = "SELECT DISTINCT Id, CharacterId FROM UserInventory";

            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    users.Add(new InventoryDataClass
                    {
                        UserId = reader.GetInt64(reader.GetOrdinal("Id")),
                        CharacterId = reader.GetInt32(reader.GetOrdinal("CharacterId"))
                    });
                }
            }

            return users;
        }

        public static string GetUsersFromDatabaseMaster(long userId)
            => GetUsersFromDatabaseMaster(userId, GetActiveCharacterId(userId));

        public static string GetUsersFromDatabaseMaster(long userId, int characterId)
        {
            string returnValue = "";
            const string query = "SELECT Id, CharacterId, ItemId, ItemName, ItemAmount, ItemWeight, AllWeight FROM UserInventory WHERE Id = @Id AND CharacterId = @CharacterId AND IsActive = 1";

            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            {
                command.Parameters.AddWithValue("@Id", userId);
                command.Parameters.AddWithValue("@CharacterId", characterId);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        returnValue += "Айді предмета: " + reader.GetInt32(reader.GetOrdinal("ItemId")) + "\n";
                        returnValue += "Ім'я предмета: " + reader.GetString(reader.GetOrdinal("ItemName")) + "\n";
                        returnValue += "Кількість предмета: " + reader.GetInt32(reader.GetOrdinal("ItemAmount")) + "\n";
                        returnValue += "Вага 1 предмета: " + reader.GetDouble(reader.GetOrdinal("ItemWeight")) + "\n";
                        returnValue += "Загальна вага у кг: " + reader.GetDouble(reader.GetOrdinal("AllWeight")) + "\n\n";
                    }
                }
            }

            return returnValue;
        }

        public static double GetAllInventoryWeight(long userId)
            => GetAllInventoryWeight(userId, GetActiveCharacterId(userId));

        public static double GetAllInventoryWeight(long userId, int characterId)
        {
            const string query = "SELECT AllWeight FROM UserInventory WHERE Id = @Id AND CharacterId = @CharacterId AND IsActive = 1";
            double allWeight = 0;

            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            {
                command.Parameters.AddWithValue("@Id", userId);
                command.Parameters.AddWithValue("@CharacterId", characterId);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                        allWeight += reader.GetDouble(reader.GetOrdinal("AllWeight"));
                }
            }

            return allWeight;
        }
    }
}
