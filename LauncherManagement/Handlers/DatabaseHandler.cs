using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace LauncherManagement
{
    public class DatabaseHandler
    {
        SQLiteAsyncConnection _db;

        public DatabaseHandler()
        {
            try
            {
                _db = new SQLiteAsyncConnection(Path.Join(Directory.GetCurrentDirectory(), "LauncherSettings.db"));
            }
            catch { }
        }
        
        public async Task CreateTables()
        {
            try
            {
                await _db.CreateTablesAsync<
                    DatabaseProperties.Accounts,
                    DatabaseProperties.ActiveServer,
                    DatabaseProperties.Characters,
                    DatabaseProperties.LauncherConfig,
                    DatabaseProperties.Settings>();
            } 
            catch { }
        }

        public async Task<List<DatabaseProperties.Accounts>> ExecuteAccountAsync(string data)
        {
            try
            {
                var results = await _db.QueryAsync<DatabaseProperties.Accounts>(data);

                await _db.CloseAsync();

                return results;
            }
            catch { }

            return new List<DatabaseProperties.Accounts>();
        }

        public async Task<List<DatabaseProperties.ActiveServer>> ExecuteActiveServerAsync(string data)
        {
            try
            {
                var results = await _db.QueryAsync<DatabaseProperties.ActiveServer>(data);

                await _db.CloseAsync();

                return results;
            }
            catch { }

            return new List<DatabaseProperties.ActiveServer>();
        }

        internal async Task<List<DatabaseProperties.LauncherConfig>> ExecuteLauncherConfigAsync(string data)
        {
            try
            {
                var results = await _db.QueryAsync<DatabaseProperties.LauncherConfig>(data);

                await _db.CloseAsync();

                return results;
            }
            catch { }

            return new List<DatabaseProperties.LauncherConfig>();
        }

        internal async Task<List<DatabaseProperties.Characters>> ExecuteCharacterAsync(string data)
        {
            try
            {
                var results = await _db.QueryAsync<DatabaseProperties.Characters>(data);

                await _db.CloseAsync();

                return results;
            }
            catch { }

            return new List<DatabaseProperties.Characters>();
        }

        internal async Task<List<DatabaseProperties.Settings>> ExecuteSettingsAsync(string data)
        {
            try
            {
                var results = await _db.QueryAsync<DatabaseProperties.Settings>(data);

                await _db.CloseAsync();

                return results;
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
            }

            return new List<DatabaseProperties.Settings>();
        }
    }
}
