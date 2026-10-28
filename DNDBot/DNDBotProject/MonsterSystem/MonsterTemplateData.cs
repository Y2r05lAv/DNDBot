using DNDBot.DataBase;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace DNDBot.MonsterSystem
{
    internal static class MonsterTemplateData
    {
        private static SqlConnection SqlConnection => BaseRepository.SqlConnection;

        public static int AddTemplate(long createdBy, MonsterCreator monster)
        {
            if (monster == null)
                throw new ArgumentNullException(nameof(monster));

            EnsureConnection();

            const string sql = @"
INSERT INTO MonsterTemplates (Name, HP, Armour, XP, AttackName, AttackDamageDice, CreatedBy)
VALUES (@Name, @HP, @Armour, @XP, @AttackName, @AttackDamageDice, @CreatedBy);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var cmd = new SqlCommand(sql, SqlConnection);
            cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 128).Value = (monster.Name ?? "").Trim();
            cmd.Parameters.Add("@HP", SqlDbType.Int).Value = monster.GetEffectiveMaxHP();
            cmd.Parameters.Add("@Armour", SqlDbType.Int).Value = monster.Armour;
            cmd.Parameters.Add("@XP", SqlDbType.Int).Value = monster.XP;
            cmd.Parameters.Add("@AttackName", SqlDbType.NVarChar, 128).Value = ToDbValue(monster.AttackName);
            cmd.Parameters.Add("@AttackDamageDice", SqlDbType.NVarChar, 128).Value = ToDbValue(monster.AttackDamageDice);
            cmd.Parameters.Add("@CreatedBy", SqlDbType.BigInt).Value = createdBy;

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public static List<MonsterTemplateRecord> GetTemplates()
        {
            EnsureConnection();

            const string sql = @"
SELECT TemplateId, Name, HP, Armour, XP, AttackName, AttackDamageDice, CreatedBy, CreatedAt
FROM MonsterTemplates
ORDER BY Name ASC, TemplateId ASC";

            var list = new List<MonsterTemplateRecord>();
            using var cmd = new SqlCommand(sql, SqlConnection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new MonsterTemplateRecord
                {
                    TemplateId = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    HP = reader.GetInt32(2),
                    Armour = reader.GetInt32(3),
                    XP = reader.GetInt32(4),
                    AttackName = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    AttackDamageDice = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    CreatedBy = reader.GetInt64(7),
                    CreatedAtUtc = reader.GetDateTime(8)
                });
            }

            return list;
        }

        public static MonsterTemplateRecord GetTemplateById(int templateId)
        {
            if (templateId <= 0)
                return null;

            EnsureConnection();

            const string sql = @"
SELECT TemplateId, Name, HP, Armour, XP, AttackName, AttackDamageDice, CreatedBy, CreatedAt
FROM MonsterTemplates
WHERE TemplateId = @TemplateId";

            using var cmd = new SqlCommand(sql, SqlConnection);
            cmd.Parameters.Add("@TemplateId", SqlDbType.Int).Value = templateId;
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return new MonsterTemplateRecord
            {
                TemplateId = reader.GetInt32(0),
                Name = reader.GetString(1),
                HP = reader.GetInt32(2),
                Armour = reader.GetInt32(3),
                XP = reader.GetInt32(4),
                AttackName = reader.IsDBNull(5) ? "" : reader.GetString(5),
                AttackDamageDice = reader.IsDBNull(6) ? "" : reader.GetString(6),
                CreatedBy = reader.GetInt64(7),
                CreatedAtUtc = reader.GetDateTime(8)
            };
        }

        private static object ToDbValue(string value)
            => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

        private static void EnsureConnection()
        {
            if (SqlConnection == null || SqlConnection.State != ConnectionState.Open)
                throw new InvalidOperationException("SqlConnection is not initialized.");
        }
    }
}
