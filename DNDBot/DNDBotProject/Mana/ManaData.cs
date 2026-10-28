using DNDBot.DataBase;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace DNDBot
{
    public class ManaData
    {

        private static SqlConnection sqlConnection = BaseRepository.SqlConnection;

        public static void DeletingAllData()
        {
            if (sqlConnection == null)
            {
                Console.WriteLine("SqlConnection - NullRefferenceExpection");
                return;
            }

            string query = "DELETE FROM MagicStats";
            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            {
                command.ExecuteNonQuery();
                Console.WriteLine("All DATA was deleted from MagicStats");
            }
        }

        public static void AddMagicStatsToDatabase(ManaDataClass magic)
        {
            if (magic == null) throw new ArgumentNullException(nameof(magic));

            string query = @"
                MERGE INTO MagicStats AS target
                USING (SELECT @UserId AS UserId, @CharacterId AS CharacterId) AS source
                ON target.UserId = source.UserId AND target.CharacterId = source.CharacterId
                WHEN MATCHED THEN
                    UPDATE SET 
                        MagicLevel = @MagicLevel,
                        MagicXP = @MagicXP,
                        MagicArmour = @MagicArmour,
                        MaxMana = @MaxMana,
                        CurrentMana = @CurrentMana,
                        Stability = @Stability,
                        Exhaustion = @Exhaustion,
                        ManaZoneRadius = @ManaZoneRadius,
                        ManaZoneControl = @ManaZoneControl,
                        RegenTurn = @RegenTurn,
                        RegenAfterFight = @RegenAfterFight,
                        Resonance = @Resonance
                WHEN NOT MATCHED THEN
                    INSERT (UserId, CharacterId, MagicLevel, MagicXP, MagicArmour, MaxMana, CurrentMana, Stability, Exhaustion, ManaZoneRadius, ManaZoneControl, RegenTurn, RegenAfterFight, Resonance)
                    VALUES (@UserId, @CharacterId, @MagicLevel, @MagicXP, @MagicArmour, @MaxMana, @CurrentMana, @Stability, @Exhaustion, @ManaZoneRadius, @ManaZoneControl, @RegenTurn, @RegenAfterFight, @Resonance);";

            using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
            {
                cmd.Parameters.AddWithValue("@UserId", magic.UserId);
                cmd.Parameters.AddWithValue("@CharacterId", magic.CharacterId);
                cmd.Parameters.AddWithValue("@MagicLevel", magic.MagicLevel);
                cmd.Parameters.AddWithValue("@MagicXP", magic.MagicXP);
                cmd.Parameters.AddWithValue("@MagicArmour", magic.MagicArmour);
                cmd.Parameters.AddWithValue("@MaxMana", magic.MaxMana);
                cmd.Parameters.AddWithValue("@CurrentMana", magic.CurrentMana);
                cmd.Parameters.AddWithValue("@Stability", magic.Stability);
                cmd.Parameters.AddWithValue("@Exhaustion", magic.Exhaustion);
                cmd.Parameters.AddWithValue("@ManaZoneRadius", magic.ManaZoneRadius);
                cmd.Parameters.AddWithValue("@ManaZoneControl", magic.ManaZoneControl);
                cmd.Parameters.AddWithValue("@RegenTurn", magic.RegenTurn);
                cmd.Parameters.AddWithValue("@RegenAfterFight", magic.RegenAfterFight);
                cmd.Parameters.AddWithValue("@Resonance", magic.Resonance);
                cmd.ExecuteNonQuery();
            }
        }


        public static T DataBaseValueReader<T>(long userId, int characterId, string columnName)
        {
            const string query = "SELECT * FROM MagicStats WHERE UserId = @UserId AND CharacterId = @CharacterId";

            using (SqlCommand command = new SqlCommand(query, sqlConnection))
            {
                command.Parameters.AddWithValue("@UserId", userId);
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

        public static void UpdateMagicStatAsync(long userId, string stat, int value)
    => UpdateMagicStat(userId, UserData.GetActiveCharacterId(userId), stat, value);

        public static void UpdateMagicStat(long userId, int characterId, string stat, int value)
        {

            string query = $@"
            IF EXISTS (
                SELECT 1 FROM MagicStats 
                WHERE UserId = @UserId AND CharacterId = @CharacterId
            )
            BEGIN
                UPDATE MagicStats
                SET {stat} = @Value
                WHERE UserId = @UserId AND CharacterId = @CharacterId
            END
            ELSE
            BEGIN
                INSERT INTO MagicStats (UserId, CharacterId, {stat})
                VALUES (@UserId, @CharacterId, @Value)
            END";

            using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@CharacterId", characterId);
                cmd.Parameters.AddWithValue("@Value", value);

                cmd.ExecuteNonQuery();
            }
        }

        public static ManaDataClass GetMagicStats(long userId) => GetMagicStats(userId, UserData.GetActiveCharacterId(userId));

        public static ManaDataClass GetMagicStats(long userId, int characterId)
        {
            const string selectQuery = @"
        SELECT *
        FROM MagicStats
        WHERE UserId = @userId AND CharacterId = @characterId";

            const string insertQuery = @"
        INSERT INTO MagicStats (
            UserId,
            CharacterId,
            MagicLevel,
            MagicXP,
            BaseMana,
            MaxMana,
            CurrentMana,
            Stability,
            Exhaustion,
            ManaZoneRadius,
            ManaZoneControl,
            RegenTurn,
            RegenAfterFight,
            Resonance
        )
        VALUES (
            @userId,
            @characterId,
            1,
            0,
            100,
            0,
            100,
            100,
            0,
            'Arcane',
            0,
            0,
            0,
            0,
            NULL
        )";

            try
            {
                using (var selectCmd = new SqlCommand(selectQuery, sqlConnection))
                {
                    selectCmd.Parameters.AddWithValue("@userId", userId);
                    selectCmd.Parameters.AddWithValue("@characterId", characterId);

                    using (var reader = selectCmd.ExecuteReader())
                    {
                        if (reader.Read())
                            return ReadMana(reader, userId, characterId);
                    }
                }

                using (var insertCmd = new SqlCommand(insertQuery, sqlConnection))
                {
                    insertCmd.Parameters.AddWithValue("@userId", userId);
                    insertCmd.Parameters.AddWithValue("@characterId", characterId);
                    insertCmd.ExecuteNonQuery();
                }

                using (var selectCmd = new SqlCommand(selectQuery, sqlConnection))
                {
                    selectCmd.Parameters.AddWithValue("@userId", userId);
                    selectCmd.Parameters.AddWithValue("@characterId", characterId);

                    using (var reader = selectCmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            throw new Exception("MagicStats insert failed unexpectedly.");

                        return ReadMana(reader, userId, characterId);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex}\nError initializing MagicStats for UserId={userId}, CharacterId={characterId}");
                throw;
            }
        }

        public static List<ManaDataClass> GetAllPlayersMagicStats()
        {
            const string query = @"
            SELECT 
                ms.*,
                ud.Name AS CharacterName
            FROM MagicStats ms
            INNER JOIN UserData ud
                ON ms.UserId = ud.Id
               AND ms.CharacterId = ud.CharacterId
            ORDER BY ud.Name";

            var result = new List<ManaDataClass>();

            try
            {
                using (var cmd = new SqlCommand(query, sqlConnection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                         
                        var mana = ReadMana(reader,
                            reader.GetInt64(reader.GetOrdinal("UserId")),
                            reader.GetInt32(reader.GetOrdinal("CharacterId"))
                        );

                         
                        mana.CharacterName = reader.GetString(reader.GetOrdinal("CharacterName"));

                        result.Add(mana);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading all MagicStats: {ex}");
                throw;
            }

            return result;
        }


        private static ManaDataClass ReadMana(SqlDataReader reader, long userId, int characterId)
        {
            return new ManaDataClass
            {
                UserId = userId,
                CharacterId = characterId,
                CharacterName = "",

                MagicLevel = reader.GetInt32(reader.GetOrdinal("MagicLevel")),
                MagicXP = reader.GetInt32(reader.GetOrdinal("MagicXP")),
                MagicArmour = reader.GetInt32(reader.GetOrdinal("MagicArmour")),

                BaseMana = reader.GetInt32(reader.GetOrdinal("BaseMana")),
                CurrentMana = reader.GetInt32(reader.GetOrdinal("CurrentMana")),

                MaxMana = reader.GetInt32(reader.GetOrdinal("MaxMana")) == 0 ? 
                GetEffectiveMaxMana(reader.GetInt32(reader.GetOrdinal("BaseMana")), reader.GetInt32(reader.GetOrdinal("MagicLevel"))) :
                reader.GetInt32(reader.GetOrdinal("MaxMana")),

                Stability = reader.GetInt32(reader.GetOrdinal("Stability")),
                Exhaustion = reader.GetInt32(reader.GetOrdinal("Exhaustion")),

                ManaZoneRadius = reader.GetDouble(reader.GetOrdinal("ManaZoneRadius")),
                ManaZoneControl = reader.GetInt32(reader.GetOrdinal("ManaZoneControl")),

                RegenTurn = reader.GetInt32(reader.GetOrdinal("RegenTurn")),
                RegenAfterFight = reader.GetInt32(reader.GetOrdinal("RegenAfterFight")),

                Resonance = reader.IsDBNull(reader.GetOrdinal("Resonance")) 
                ? null 
                : reader.GetString(reader.GetOrdinal("Resonance"))
            };
        }

        public static int GetEffectiveMaxMana(int baseMaxMana, int magicLevel)
        {
            double multiplier = Math.Pow(1.12, magicLevel - 1);
            return (int)Math.Round(baseMaxMana * multiplier);
        }
    }
}

    
