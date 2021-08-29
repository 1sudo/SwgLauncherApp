using System.Collections.Generic;
using System.Threading.Tasks;

namespace LauncherManagement
{
    public class CharacterHandler : DatabaseHandler
    {
        public async Task<string> GetLastSavedCharacterAsync()
        {
            List<DatabaseProperties.Characters> characters = await ExecuteCharacterAsync
                (
                    $"SELECT * FROM Characters;'"
                );

            if (characters.Count > 0)
            {
                return (string.IsNullOrEmpty(characters[0].Character)) ? "None" : characters[0].Character;
            }

            return "None";
        }

        public async Task SaveCharacterAsync(string character)
        {
            List<DatabaseProperties.Characters> characters = await ExecuteCharacterAsync
                (
                    $"SELECT * FROM Characters;'"
                );

            if (characters.Count > 0)
            {
                await ExecuteCharacterAsync($"UPDATE Characters SET Character = '{character}' where Id = 1;");
            }
            else
            {
                await ExecuteCharacterAsync($"INSERT into Characters (Character) VALUES ('{character}');");
            }
        }
    }
}
