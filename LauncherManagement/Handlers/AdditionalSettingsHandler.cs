using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace LauncherManagement
{
    public class AdditionalSettingsHandler : DatabaseHandler
    {
        public async Task<List<DatabaseProperties.AdditionalSettings>> GetSettings()
        {
            return await ExecuteAdditionalSettingsAsync
                (
                    "SELECT * " +
                    "FROM AdditionalSettings;"
                );
        }

        public async Task SaveSettings(List<GameSettingsProperty> properties)
        {
            StringBuilder sb = new();
            foreach (GameSettingsProperty property in properties)
            {
                sb.AppendLine($"('{property.Category}', '{property.Key}', '{property.Value}'),");
            }

            await ExecuteAdditionalSettingsAsync
                (
                    "INSERT INTO AdditionalSettings " +
                    "(Category, Key, Value)" +
                    "VALUES" +
                    $"{sb};"
                );
        }

        public async Task InsertDefaultRows()
        {
            List<DatabaseProperties.AdditionalSettings> settings = await ExecuteAdditionalSettingsAsync
                (
                    "SELECT * " +
                    "FROM AdditionalSettings;"
                );

            if (settings.Count < 1)
            {
                await ExecuteAdditionalSettingsAsync
                    (
                        "INSERT INTO AdditionalSettings " +
                        "(Category, Key, Value)" +
                        "VALUES" +
                        "('ClientGraphics', 'screenWidth', '1024'), " +
                        "('ClientGraphics', 'screenHeight', '768'), " +
                        "('ClientGraphics', 'allowTearing', '1'), " +
                        "('ClientGame', 'skipIntro', '1'), " +
                        "('ClientGame', 'preloadWorldSnapshot', '0'), " +
                        "('ClientSkeletalAnimation', 'lodManagerEnable', '0'), " +
                        "('SharedUtility', 'disableFileCaching', '1');"
                    );
            }
        }
    }
}
