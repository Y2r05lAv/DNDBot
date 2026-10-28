using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Configuration;
using System.IO;
using System.Collections.Generic;

namespace DNDBot.DataBase
{
    internal static class BaseRepository
    {
        internal static SqlConnection SqlConnection { get; private set; } = null;
        private static readonly object SchemaLock = new object();
        private static bool SchemaApplied;

        public static void DBInitialization()
        {
            if (SqlConnection != null && SqlConnection.State == ConnectionState.Open)
                return;

            var connStrSetting = ConfigurationManager.ConnectionStrings["DataKey"];
            if (connStrSetting == null || string.IsNullOrWhiteSpace(connStrSetting.ConnectionString))
                throw new InvalidOperationException("Connection string 'DataKey' not found or empty.");

            var builder = new SqlConnectionStringBuilder(connStrSetting.ConnectionString)
            {
                 
                MultipleActiveResultSets = true
            };

            EnsureLocalDatabaseExists(builder);
            SqlConnection = new SqlConnection(builder.ConnectionString);
            SqlConnection.Open();

            if (SqlConnection.State == ConnectionState.Open)
                Console.WriteLine("Connected: Repository");

            ApplySchemaUpdates();
        }

        private static void EnsureLocalDatabaseExists(SqlConnectionStringBuilder targetBuilder)
        {
            if (string.IsNullOrWhiteSpace(targetBuilder.InitialCatalog) ||
                string.IsNullOrWhiteSpace(targetBuilder.DataSource) ||
                targetBuilder.DataSource.IndexOf("(LocalDB)", StringComparison.OrdinalIgnoreCase) < 0 ||
                !string.IsNullOrWhiteSpace(targetBuilder.AttachDBFilename))
            {
                return;
            }

            var databaseName = targetBuilder.InitialCatalog;
            var masterBuilder = new SqlConnectionStringBuilder(targetBuilder.ConnectionString)
            {
                InitialCatalog = "master"
            };

            using var masterConnection = new SqlConnection(masterBuilder.ConnectionString);
            masterConnection.Open();

            const string createDatabaseSql = @"
                IF DB_ID(@databaseName) IS NULL
                BEGIN
                    DECLARE @createSql nvarchar(max) = N'CREATE DATABASE ' + QUOTENAME(@databaseName);
                    EXEC(@createSql);
                END";

            using var command = new SqlCommand(createDatabaseSql, masterConnection);
            command.Parameters.Add("@databaseName", SqlDbType.NVarChar, 128).Value = databaseName;

            try
            {
                command.ExecuteNonQuery();
            }
            catch (SqlException ex) when (ex.Number == 1801)
            {
            }
        }
        public static int ExecuteNonQuery(string sql, params SqlParameter[] parameters)
        {
            EnsureConnection();
            using var cmd = new SqlCommand(sql, SqlConnection);
            if (parameters != null)
                cmd.Parameters.AddRange(parameters);
            return cmd.ExecuteNonQuery();
        }

        public static object ExecuteScalar(string sql, params SqlParameter[] parameters)
        {
            EnsureConnection();
            using var cmd = new SqlCommand(sql, SqlConnection);
            if (parameters != null)
                cmd.Parameters.AddRange(parameters);
            return cmd.ExecuteScalar();
        }

        public static SqlDataReader ExecuteReader(string sql, params SqlParameter[] parameters)
        {
            EnsureConnection();
            var cmd = new SqlCommand(sql, SqlConnection);
            if (parameters != null)
                cmd.Parameters.AddRange(parameters);
            return cmd.ExecuteReader();  
        }

        private static void EnsureConnection()
        {
            if (SqlConnection == null || SqlConnection.State != ConnectionState.Open)
                throw new InvalidOperationException("SqlConnection is not initialized. Call BaseRepository.DBInitialization() first.");
        }

        private static void ApplySchemaUpdates()
        {
            if (SchemaApplied)
                return;

            lock (SchemaLock)
            {
                if (SchemaApplied)
                    return;

                var sqlFiles = FindSchemaFiles();
                foreach (var file in sqlFiles)
                {
                    string sql = File.ReadAllText(file);
                    foreach (var batch in SplitSqlBatches(sql))
                    {
                        if (string.IsNullOrWhiteSpace(batch))
                            continue;
                        try
                        {
                            ExecuteNonQuery(batch);
                        }
                        catch (SqlException ex) when (IsAlreadyExistsError(ex))
                        {
                             
                             
                            continue;
                        }
                    }
                }

                SchemaApplied = true;
            }
        }

        private static List<string> FindSchemaFiles()
        {
            var byName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

             
             
            foreach (var dir in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
            {
                if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                    continue;

                foreach (var file in Directory.GetFiles(dir, "dbo.*.sql", SearchOption.TopDirectoryOnly))
                {
                    string name = Path.GetFileName(file);
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    if (!byName.ContainsKey(name))
                        byName[name] = file;
                }
            }

            var result = new List<string>(byName.Values);
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }

        private static IEnumerable<string> SplitSqlBatches(string sql)
        {
            using var reader = new StringReader(sql);
            var current = new System.Text.StringBuilder();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
                {
                    yield return current.ToString();
                    current.Clear();
                    continue;
                }
                current.AppendLine(line);
            }
            if (current.Length > 0)
                yield return current.ToString();
        }

        private static bool IsAlreadyExistsError(SqlException ex)
        {
             
            if (ex.Number == 2714)
                return true;
            return ex.Message != null &&
                   ex.Message.IndexOf("already an object named", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static SqlParameter Param(string name, SqlDbType type, object value, int size = 0)
        {
            var p = new SqlParameter(name, type);
            if (size > 0)
                p.Size = size;
            p.Value = value ?? DBNull.Value;
            return p;
        }
    }
}
