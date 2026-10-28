using DNDBot.DataBase;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNDBot.BotSystem
{
    class UserListDataBase
    {
        private static SqlConnection sqlConnection = BaseRepository.SqlConnection;



        public static string AddUserToDataBase(long userId)
        {
            string checkQuery = "SELECT COUNT(*) FROM Users WHERE Id = @Id";
            string insertQuery = "INSERT INTO Users (Id) VALUES (@Id)";

            using (SqlCommand checkCommand = new SqlCommand(checkQuery, sqlConnection))
            {
                checkCommand.Parameters.AddWithValue("@Id", userId);
                int count = (int)checkCommand.ExecuteScalar();

                if (count > 0)
                {
                    return "Користувач вже існує в базі!";
                }
            }

            using (SqlCommand insertCommand = new SqlCommand(insertQuery, sqlConnection))
            {
                insertCommand.Parameters.AddWithValue("@Id", userId);
                insertCommand.ExecuteNonQuery();
                return "Ваші дані було додано!";
            }
        }



        public static List<long> GetAllUsers()
        {
            string query = "SELECT Id FROM UserData";
            List<long> userIds = new List<long>();

            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    userIds.Add(reader.GetInt64(0)); 
                }
            }

            return userIds;
        }
    }
}
