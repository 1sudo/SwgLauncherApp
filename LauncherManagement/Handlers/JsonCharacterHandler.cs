using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System.Diagnostics;
using System.IO;

namespace LauncherManagement
{
    public class JsonCharacterHandler
    {

        public string GetLastSavedCharacter()
        {
            string filePath = Path.Join(Directory.GetCurrentDirectory(), "character.json");

            if (File.Exists(filePath))
            {
                string content = File.ReadAllText(filePath);
                if (ValidationHandler.ValidateJson("character.json"))
                {
                    CharacterProperties characterProperties = JsonConvert.DeserializeObject<CharacterProperties>(content);
                    return characterProperties.Character;
                }
            }

            return "";
        }

        public void SaveCharacter(string character)
        {
            string filePath = Path.Join(Directory.GetCurrentDirectory(), "character.json");

            if (File.Exists(filePath))
            {
                string content = File.ReadAllText(filePath);
                if (ValidationHandler.ValidateJson("character.json"))
                {
                    CharacterProperties characterProperties = JsonConvert.DeserializeObject<CharacterProperties>(content);
                    characterProperties.Character = character;
                    File.WriteAllTextAsync(filePath, JsonConvert.SerializeObject(characterProperties, Formatting.Indented));
                }
            }
            else
            {
                CharacterProperties characterProperties = new CharacterProperties
                {
                    Character = character
                };

                File.WriteAllTextAsync(filePath, JsonConvert.SerializeObject(characterProperties, Formatting.Indented));
            }
        }
    }
}
