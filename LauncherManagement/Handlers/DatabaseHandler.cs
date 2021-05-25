using SQLite;
using System.Collections.Generic;
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
                    DatabaseProperties.Characters,
                    DatabaseProperties.LauncherConfig,
                    DatabaseProperties.GameSettings>();
            } 
            catch { }
        }

        public async Task<List<DatabaseProperties.Accounts>> ExecuteAccount(string data)
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

        public async Task<List<DatabaseProperties.LauncherConfig>> ExecuteLauncherConfig(string data)
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


    }
}
