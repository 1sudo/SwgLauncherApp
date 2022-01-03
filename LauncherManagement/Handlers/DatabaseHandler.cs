using SQLite;

namespace LauncherManagement
{
    public class DatabaseHandler
    {
        readonly SQLiteAsyncConnection? _db;

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
                if (_db is null)
                {
                    return;
                }

                await _db.CreateTablesAsync<
                    DatabaseProperties.Accounts,
                    DatabaseProperties.ActiveServer,
                    DatabaseProperties.Characters,
                    DatabaseProperties.LauncherConfig,
                    DatabaseProperties.Settings
                    >();
            } 
            catch { }

            try
            {
                if (_db is null)
                {
                    return;
                }

                await _db.CreateTablesAsync<
                    DatabaseProperties.AdditionalSettings,
                    DatabaseProperties.TreMods
                    >();
            }
            catch { }

            
        }

        public async Task<List<DatabaseProperties.Accounts>?> ExecuteAccountAsync(string data)
        {
            try
            {
                if (_db is null)
                {
                    return null;
                }

                var results = await _db.QueryAsync<DatabaseProperties.Accounts>(data);

                await _db.CloseAsync();

                return results;
            }
            catch (Exception e)
            {
                await LogHandler.Log(LogType.ERROR, "| ExecuteAccountAsync | " + e.Message.ToString());
            }

            return new List<DatabaseProperties.Accounts>();
        }

        public async Task<List<DatabaseProperties.ActiveServer>?> ExecuteActiveServerAsync(string data)
        {
            try
            {
                if (_db is null)
                {
                    return null;
                }

                var results = await _db.QueryAsync<DatabaseProperties.ActiveServer>(data);

                await _db.CloseAsync();

                return results;
            }
            catch (Exception e)
            {
                await LogHandler.Log(LogType.ERROR, "| ExecuteActiveServerAsync | " + e.Message.ToString());
            }

            return new List<DatabaseProperties.ActiveServer>();
        }

        internal async Task<List<DatabaseProperties.LauncherConfig>?> ExecuteLauncherConfigAsync(string data)
        {
            try
            {
                if (_db is null)
                {
                    return null;
                }

                var results = await _db.QueryAsync<DatabaseProperties.LauncherConfig>(data);

                await _db.CloseAsync();

                return results;
            }
            catch (Exception e)
            {
                await LogHandler.Log(LogType.ERROR, "| ExecuteLauncherConfigAsync | " + e.Message.ToString());
            }

            return new List<DatabaseProperties.LauncherConfig>();
        }

        internal async Task<List<DatabaseProperties.Characters>?> ExecuteCharacterAsync(string data)
        {
            try
            {
                if (_db is null)
                {
                    return null;
                }

                var results = await _db.QueryAsync<DatabaseProperties.Characters>(data);

                await _db.CloseAsync();

                return results;
            }
            catch (Exception e)
            {
                await LogHandler.Log(LogType.ERROR, "| ExecuteCharacterAsync | " + e.Message.ToString());
            }

            return new List<DatabaseProperties.Characters>();
        }

        internal async Task<List<DatabaseProperties.Settings>?> ExecuteSettingsAsync(string data)
        {
            try
            {
                if (_db is null)
                {
                    return null;
                }

                var results = await _db.QueryAsync<DatabaseProperties.Settings>(data);

                await _db.CloseAsync();

                return results;
            }
            catch (Exception e)
            {
                await LogHandler.Log(LogType.ERROR, "| ExecuteSettingsAsync | " + e.Message.ToString());
            }

            return new List<DatabaseProperties.Settings>();
        }

        internal async Task<List<DatabaseProperties.AdditionalSettings>?> ExecuteAdditionalSettingsAsync(string data)
        {
            try
            {
                if (_db is null)
                {
                    return null;
                }

                var results = await _db.QueryAsync<DatabaseProperties.AdditionalSettings>(data);

                await _db.CloseAsync();

                return results;
            }
            catch (Exception e)
            {
                await LogHandler.Log(LogType.ERROR, "| ExecuteAdditionalSettingsAsync | " + e.Message.ToString());
            }

            return new List<DatabaseProperties.AdditionalSettings>();
        }

        internal async Task<List<DatabaseProperties.TreMods>?> ExecuteTreModsAsync(string data)
        {
            try
            {
                if (_db is null)
                {
                    return null;
                }

                var results = await _db.QueryAsync<DatabaseProperties.TreMods>(data);

                await _db.CloseAsync();

                return results;
            }
            catch (Exception e)
            {
                await LogHandler.Log(LogType.ERROR, "| ExecuteTreModsAsync | " + e.Message.ToString());
            }

            return new List<DatabaseProperties.TreMods>();
        }
    }
}
