using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System.Diagnostics;
using System.IO;

namespace LauncherManagement
{
    public class JsonCharacterHandler
    {
        public bool ValidateCharacterConfig()
        {
            JObject json = new JObject();

            string schemaJson = @"{
                'Character': 'char',
            }";

            JSchema schema = JSchema.Parse(schemaJson);

            try
            {
                json = JObject.Parse(File.ReadAllText(Path.Join(Directory.GetCurrentDirectory(), "character.json")));
            }
            catch
            {
                return false;
            }

            bool validSchema = json.IsValid(schema);

            int keysContained = 0;

            if (json.ContainsKey("Character"))
            {
                keysContained++;
            }

            if (validSchema && keysContained == 1)
            {
                return true;
            }

            return false;
        }

        public string GetLastSavedCharacter()
        {
            string filePath = Path.Join(Directory.GetCurrentDirectory(), "character.json");

            if (File.Exists(filePath))
            {
                string content = File.ReadAllText(filePath);
                if (ValidateCharacterConfig())
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
                if (ValidateCharacterConfig())
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
