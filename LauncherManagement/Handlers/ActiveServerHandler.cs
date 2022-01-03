using System.Collections.Generic;
using System.Threading.Tasks;

namespace LauncherManagement
{
    public class ActiveServerHandler : DatabaseHandler
    {
        public async Task GetActiveServer()
        {
            List<DatabaseProperties.ActiveServer>? config = await ExecuteActiveServerAsync("SELECT Id FROM ActiveServer;");

            ServerSelection.ActiveServer = config![0].Id;
        }

        public async Task SetActiveServer(int id)
        {
            await ExecuteActiveServerAsync
                (
                    "UPDATE ActiveServer " +
                    $"Set Id = {id};"
                );

            // Update the property with new ID
            await GetActiveServer();
        }

        public async Task InsertDefaultRow()
        {
            List<DatabaseProperties.ActiveServer>? config = await ExecuteActiveServerAsync("SELECT * FROM ActiveServer;");

            if (config is not null && config.Count < 1)
            {
                await ExecuteActiveServerAsync
                    (
                        "INSERT into ActiveServer " +
                        "(Id) " +
                        "VALUES " +
                        "(1);"
                    );
            }
        }
    }
}
