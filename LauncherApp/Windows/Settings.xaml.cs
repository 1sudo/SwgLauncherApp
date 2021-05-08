using LauncherManagement;
using System.Windows;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;

namespace LauncherApp
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        readonly MainWindow _mainWindow;
        public Settings(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
        }

        private void Window_Activated(object sender, System.EventArgs e)
        {
            foreach (string resolution in DisplayResolutions.GetResolutions())
            {
                ResolutionBox.Items.Add(resolution);
            }

            LoadSettings();
        }

        void LoadSettings()
        {
            string file = Path.Join(Directory.GetCurrentDirectory(), "settings.json");

            List<GameSettingsProperty> properties = JsonConvert.DeserializeObject<List<GameSettingsProperty>>(file);

            foreach(GameSettingsProperty property in properties)
            {
                Trace.WriteLine(property.Category);
            }
        }

        void GetResolution()
        {
            string file = Path.Join(Directory.GetCurrentDirectory(), "settings.json");

            if (File.Exists(file))
            {
                JsonCharacterHandler characterHandler = new JsonCharacterHandler();

                characterHandler.GetLastSavedCharacter();

                for (int i = 0; i < ResolutionBox.Items.Count; i++)
                {
                    if (characterHandler.GetLastSavedCharacter() == ResolutionBox.Items[i].ToString())
                    {
                        ResolutionBox.SelectedIndex = i;
                    }
                }
            }
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            string[] categories = {
                "ClientGraphics", "ClientGraphics", "ClientGraphics", "SharedUtility", "Direct3d9", "Direct3d9",
                "ClientAudio", "ClientAudio", "ClientGraphics", "ClientGraphics", "ClientGraphics", 
                "ClientGraphics", "ClientGraphics", "ClientGraphics", "ClientGraphics", "SharedUtility", "ClientGame",
                "ClientGame", "ClientSkeletalAnimation", "ClientSkeletalAnimation", "ClientObject_DetailAppearanceTemplate",
                "ClientTextureRenderer", "SharedFile"
            };

            string[] names =
            {
                "Disable Hardware Mouse Cursor", "Display Resolution", "Use Safe Renderer (Slower)", "Cache File", "Raster Major",
                "Shader Version", "Sound Provider", "Disable Audio", "Borderless Window", "Window Mode", "Disable Bump Mapping",
                "Use Low Detail Textures", "Use Low Detail Normal Maps", "Disable Multi-Pass Rendering",
                "Disable Vsync (Allow Tearing)", "Disable File Caching", "Skip Intro Sequence", "Disable World Preloading", 
                "Disable Character Level Of Detail Manager", "Use Low Detail Characters", "Use Low Detail Meshes", "Disable Texture Baking",
                "Disable Asynchronous Loader"
            };

            string[] settings =
            {
                "useHardwareMouseCursor", "screenWidth,screenHeight", "useSafeRenderer", "cache", "rasterMajor",
                "maxVertexShaderVersion,maxPixelShaderVersion", "soundProvider", "disableMiles", "borderlessWindow",
                "windowed", "disableOptionTag", "discardHighestMipMapLevels", "discardHighestNormalMipMapLevels",
                "disableOptionTag", "allowTearing", "disableFileCaching", "skipIntro", "preloadWorldSnapshot",
                "lodManagerEnable", "skipL0", "skipL0", "disableTextureBaking", "enableAsynchronousLoader"
            };

            string[] values =
            {
                DisableHardwareMouseCheckbox.IsChecked.ToString(), ResolutionBox.SelectedValue.ToString(),
                UseSafeRendererCheckbox.IsChecked.ToString(), "misc/cache_large.iff", "", ShaderBox.SelectedValue.ToString(),
                "Miles Fast 2D Positional Audio", DisableAudioCheckbox.IsChecked.ToString(), BorderlessWindowCheckbox.IsChecked.ToString(),
                WindowModeCheckbox.IsChecked.ToString(), DisableBumpMappingCheckbox.IsChecked.ToString(), LowDetailTexturesCheckbox.IsChecked.ToString(),
                LowDetailNormalsCheckbox.IsChecked.ToString(), DisableMultiPassRenderingCheckbox.IsChecked.ToString(), DisableVsyncCheckbox.IsChecked.ToString(),
                DisableFileCachingCheckbox.IsChecked.ToString(), SkipIntroCheckbox.IsChecked.ToString(), DisableWorldPreloadingCheckbox.IsChecked.ToString(),
                DisableLODManagerCheckbox.IsChecked.ToString(), LowDetailCharactersCheckbox.IsChecked.ToString(), LowDetailMeshesCheckbox.IsChecked.ToString(),
                DisableTextureBakingCheckbox.IsChecked.ToString(), DisableAsyncLoaderCheckbox.IsChecked.ToString()
            };

            bool[] required =
            {
                true, true, true, true, true, true, true, false, false, false, false, false, 
                false, false, false, false, false, false, false, false, false, false, false
            };

            List<GameSettingsProperty> gameSettingProperties = new List<GameSettingsProperty>();

            for (int i = 0; i < settings.Length; i++)
            {
                GameSettingsProperty gameSettingsProperty = new GameSettingsProperty();

                gameSettingsProperty.Category = categories[i];
                gameSettingsProperty.Name = names[i];
                gameSettingsProperty.Setting = settings[i];
                gameSettingsProperty.Value = values[i];
                gameSettingsProperty.Required = required[i];

                gameSettingProperties.Add(gameSettingsProperty);
            }

            GameSettingsHandler gameSettingsHandler = new GameSettingsHandler();
            await gameSettingsHandler.SaveSettings(gameSettingProperties);
        }
    }
}
