using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ViewModels;
using LibLauncherApp.Properties;
using LauncherApp.Models;

namespace LauncherApp.ViewModels;

internal class SettingsViewModel : SettingsViewModelProperties
{
    public IAsyncRelayCommand SaveSettingsButton { get; }
    public IRelayCommand RevertSettingsButton { get; }

    public SettingsViewModel()
    {
        SaveSettingsButton = new AsyncRelayCommand(SaveSettings);
        RevertSettingsButton = new RelayCommand(RevertSettings);

        Task.Run(SetConfigOptions);
    }

    private async Task SaveSettings()
    {
        var config = ConfigFile.GetConfig();
        var currentServer = config?.Servers?[config.ActiveServer];

        if (currentServer is null) return;

        List<AdditionalSettingProperties> properties = new();

        int fps = 0;
        int ram = 0;
        int maxZoom = 0;

        switch (FpsComboIndex)
        {
            case 3: fps = 30; break;
            case 2: fps = 60; break;
            case 1: fps = 144; break;
            case 0: fps = 240; break;
        }

        switch (MemoryComboIndex)
        {
            case 3: ram = 512; break;
            case 2: ram = 1024; break;
            case 1: ram = 2048; break;
            case 0: ram = 4096; break;
        }

        switch (MaxZoomComboIndex)
        {
            case 0: maxZoom = 1; break;
            case 1: maxZoom = 3; break;
            case 2: maxZoom = 5; break;
            case 3: maxZoom = 7; break;
            case 4: maxZoom = 10; break;
        }

        currentServer.Fps = fps;
        currentServer.Ram = ram;
        currentServer.MaxZoom = maxZoom;

        if (config is null) return;

        ConfigFile.SetConfig(config);

        if (SelectedResolution is null) return;

        string screenWidth = SelectedResolution.Split("x")[0];
        string screenHeight = SelectedResolution.Split("x")[1].Split("@")[0];
        string refreshRate = SelectedResolution.Split("@")[1];

        properties.Add(new AdditionalSettingProperties { Category = "ClientGraphics", Key = "screenWidth", Value = $"{screenWidth}" });
        properties.Add(new AdditionalSettingProperties { Category = "ClientGraphics", Key = "screenHeight", Value = $"{screenHeight}" });
        properties.Add(new AdditionalSettingProperties { Category = "Direct3d9", Key = "fullscreenRefreshRate", Value = $"{refreshRate}" });

        if (ShaderComboIndex == 1)
        {
            properties.Add(new AdditionalSettingProperties { Category = "Direct3d9", Key = "maxVertexShaderVersion", Value = "0x0200" });
            properties.Add(new AdditionalSettingProperties { Category = "Direct3d9", Key = "maxPixelShaderVersion", Value = "0x0200" });
        }
        if (ShaderComboIndex == 2)
        {
            properties.Add(new AdditionalSettingProperties { Category = "Direct3d9", Key = "maxVertexShaderVersion", Value = "0x0101" });
            properties.Add(new AdditionalSettingProperties { Category = "Direct3d9", Key = "maxPixelShaderVersion", Value = "0x0104" });
        }
        if (ShaderComboIndex == 3)
        {
            properties.Add(new AdditionalSettingProperties { Category = "Direct3d9", Key = "maxVertexShaderVersion", Value = "0x0101" });
            properties.Add(new AdditionalSettingProperties { Category = "Direct3d9", Key = "maxPixelShaderVersion", Value = "0x0101" });
        }
        if (ShaderComboIndex == 4)
        {
            properties.Add(new AdditionalSettingProperties { Category = "Direct3d9", Key = "maxVertexShaderVersion", Value = "0" });
            properties.Add(new AdditionalSettingProperties { Category = "Direct3d9", Key = "maxPixelShaderVersion", Value = "0" });
        }
        if (DisableVsyncCheckbox)
        {
            properties.Add(new AdditionalSettingProperties { Category = "Direct3d9", Key = "allowTearing", Value = "1" });
        }
        if (UseSafeRendererCheckbox)
        {
            properties.Add(new AdditionalSettingProperties { Category = "ClientGraphics", Key = "useSafeRenderer", Value = "1" });
        }
        if (BorderlessWindowCheckbox)
        {
            properties.Add(new AdditionalSettingProperties { Category = "ClientGraphics", Key = "borderlessWindow", Value = "1" });
        }
        if (WindowModeCheckbox)
        {
            properties.Add(new AdditionalSettingProperties { Category = "ClientGraphics", Key = "windowed", Value = "1" });
        }
        if (UseLowDetailTexturesCheckbox)
        {
            properties.Add(new AdditionalSettingProperties { Category = "ClientGraphics", Key = "discardHighestMipMapLevels", Value = "1" });
        }
        if (UseLowDetailNormalMapsCheckbox)
        {
            properties.Add(new AdditionalSettingProperties { Category = "ClientGraphics", Key = "discardHighestNormalMipMapLevels", Value = "1" });
        }
        if (DisableBumpMappingCheckbox)
        {
            properties.Add(new AdditionalSettingProperties { Category = "ClientGraphics", Key = "disableOptionTag", Value = "DOT3" });
            properties.Add(new AdditionalSettingProperties { Category = "ClientTerrain", Key = "dot3Terrain", Value = "0" });
        }
        if (DisableMultiPassRenderingCheckbox)
        {
            properties.Add(new AdditionalSettingProperties { Category = "ClientGraphics", Key = "disableOptionTag", Value = "HIQL" });
        }
        if (DisableHardwareMouseCursorCheckbox)
        {
            properties.Add(new AdditionalSettingProperties { Category = "ClientGraphics", Key = "useHardwareMouseCursor", Value = "0" });
        }
        if (SkipIntroCheckbox)
        {
            properties.Add(new AdditionalSettingProperties { Category = "ClientGame", Key = "skipIntro", Value = "1" });
        }
        if (DisableWorldPreloadingCheckbox)
        {
            properties.Add(new AdditionalSettingProperties { Category = "ClientGame", Key = "preloadWorldSnapshot", Value = "0" });
        }
        if (DisableFastMouseCursorCheckbox)
        {
            properties.Add(new AdditionalSettingProperties { Category = "ClientUserInterface", Key = "alwaysSetMouseCursor", Value = "1" });
        }
        if (DisableAudioCheckbox)
        {
            properties.Add(new AdditionalSettingProperties { Category = "ClientAudio", Key = "disableMiles", Value = "1" });
        }
        if (DisableLodManagerCheckbox)
        {
            properties.Add(new AdditionalSettingProperties { Category = "ClientSkeletalAnimation", Key = "lodManagerEnable", Value = "0" });
        }
        if (DisableTextureBakingCheckbox)
        {
            properties.Add(new AdditionalSettingProperties { Category = "ClientTextureRenderer", Key = "disableTextureBaking", Value = "1" });
        }
        if (DisableFileCachingCheckbox)
        {
            properties.Add(new AdditionalSettingProperties { Category = "SharedUtility", Key = "disableFileCaching", Value = "1" });
        }
        if (DisableAsynchronousLoaderCheckbox)
        {
            properties.Add(new AdditionalSettingProperties { Category = "SharedFile", Key = "enableAsynchronousLoader", Value = "0" });
        }
        if (UseLowDetailCharactersCheckbox)
        {
            properties.Add(new AdditionalSettingProperties { Category = "ClientSkeletalAnimation", Key = "skipL0", Value = "1" });
        }
        if (UseLowDetailMeshesCheckbox)
        {
            properties.Add(new AdditionalSettingProperties { Category = "ClientObject/DetailAppearanceTemplate", Key = "skipL0", Value = "1" });
        }

        await FileHandler.SaveOptionsCfg(config, properties);
    }

    public void RevertSettings()
    {
        MemoryComboIndex = 1;
        FpsComboIndex = 2;
        MaxZoomComboIndex = 2;
        UseSafeRendererCheckbox = false;
        BorderlessWindowCheckbox = false;
        WindowModeCheckbox = false;
        UseLowDetailCharactersCheckbox = false;
        UseLowDetailNormalMapsCheckbox = false;
        DisableVsyncCheckbox = true;
        DisableFastMouseCursorCheckbox = false;
        SkipIntroCheckbox = true;
        DisableAudioCheckbox = false;
        DisableWorldPreloadingCheckbox = true;
        DisableLodManagerCheckbox = true;
        DisableTextureBakingCheckbox = false;
        DisableFileCachingCheckbox = true;
        DisableAsynchronousLoaderCheckbox = false;
        UseLowDetailMeshesCheckbox = false;
        DisableBumpMappingCheckbox = false;
        DisableMultiPassRenderingCheckbox = false;
        DisableHardwareMouseCursorCheckbox = false;
        ShaderComboIndex = 0;
        SelectedResolution = "1024x768@60";
    }

    public async Task SetConfigOptions()
    {
        List<string> resolutions = LibLauncherApp.Util.DisplayResolutions.GetResolutions();
        AvailableResolutions = new ObservableCollection<string>();

        // Population resolutions
        resolutions.ForEach(resolution =>
        {
            AvailableResolutions.Add(resolution);
        });

        var config = ConfigFile.GetCurrentServer();

        if (config is null) return;

        int fps = config.Fps;
        int ram = config.Ram;
        int maxZoom = config.MaxZoom;

        switch (ram)
        {
            case 512: MemoryComboIndex = 3; break;
            case 1024: MemoryComboIndex = 2; break;
            case 2048: MemoryComboIndex = 1; break;
            case 4096: MemoryComboIndex = 0; break;
        }

        switch (fps)
        {
            case 30: FpsComboIndex = 3; break;
            case 60: FpsComboIndex = 2; break;
            case 144: FpsComboIndex = 1; break;
            case 240: FpsComboIndex = 0; break;
        }

        switch (maxZoom)
        {
            case 1: MaxZoomComboIndex = 0; break;
            case 3: MaxZoomComboIndex = 1; break;
            case 5: MaxZoomComboIndex = 2; break;
            case 7: MaxZoomComboIndex = 3; break;
            case 10: MaxZoomComboIndex = 4; break;
        }

        var properties = await FileHandler.ParseOptionsCfg(ConfigFile.GetConfig());

        string screenHeight = "";
        string screenWidth = "";
        string refreshRate = "";
        string vertexShaderVersion = "";
        string pixelShaderVersion = "";

        properties.ForEach(property =>
        {
            switch (property.Key)
            {
                case "\tscreenWidth":
                    screenWidth = property.Value ?? ""; 
                    break;
                case "\tscreenHeight":
                    screenHeight = property.Value ?? ""; 
                    break;
                case "\tfullscreenRefreshRate":
                    refreshRate = property.Value ?? ""; 
                    break;
                case "\tuseSafeRenderer": 
                    UseSafeRendererCheckbox = true; 
                    break;
                case "\tborderlessWindow": 
                    BorderlessWindowCheckbox = true; 
                    break;
                case "\twindowed": 
                    WindowModeCheckbox = true; 
                    break;
                case "\tdiscardHighestMipMapLevels": 
                    UseLowDetailCharactersCheckbox = true; 
                    break;
                case "\tdiscardHighestNormalMipMapLevels": 
                    UseLowDetailNormalMapsCheckbox = true; 
                    break;
                case "\tallowTearing": 
                    DisableVsyncCheckbox = true; 
                    break;
                case "\talwaysSetMouseCursor": 
                    DisableFastMouseCursorCheckbox = true; 
                    break;
                case "\tskipIntro": 
                    SkipIntroCheckbox = true; 
                    break;
                case "\tdisableMiles": 
                    DisableAudioCheckbox = true; 
                    break;
                case "\tpreloadWorldSnapshot": 
                    DisableWorldPreloadingCheckbox = true; 
                    break;
                case "\tlodManagerEnable": 
                    DisableLodManagerCheckbox = true; 
                    break;
                case "\tdisableTextureBaking": 
                    DisableTextureBakingCheckbox = true; 
                    break;
                case "\tdisableFileCaching": 
                    DisableFileCachingCheckbox = true; 
                    break;
                case "\tenableAsynchronousLoader": 
                    DisableAsynchronousLoaderCheckbox = true; 
                    break;
                case "\tskipL0":
                    if (property.Category == "ClientSkeletalAnimation")
                        UseLowDetailCharactersCheckbox = true;
                    else
                        UseLowDetailMeshesCheckbox = true;
                    break;
                case "\tdisableOptionTag":
                    if (property.Value == "DOT3")
                        DisableBumpMappingCheckbox = true;
                    else
                        DisableMultiPassRenderingCheckbox = true;
                    break;
                case "\tuseHardwareMouseCursor":
                    if (property.Value == "0")
                        DisableHardwareMouseCursorCheckbox = true;
                    else
                        DisableHardwareMouseCursorCheckbox = false;
                    break;
            }

            if (property.Category == "Direct3d9")
            {
                if (string.IsNullOrEmpty(property.Key) && string.IsNullOrEmpty(property.Value))
                {
                    ShaderComboIndex = 0;
                }

                if (property.Key == "\tmaxVertexShaderVersion")
                {
                    vertexShaderVersion = property.Value ?? "";
                }

                if (property.Key == "\tmaxPixelShaderVersion")
                {
                    pixelShaderVersion = property.Value ?? "";
                }
            }
        });

        if (vertexShaderVersion == "0x0200" && pixelShaderVersion == "0x0200")
        {
            ShaderComboIndex = 1;
        }
        if (vertexShaderVersion == "0x0101" && pixelShaderVersion == "0x0104")
        {
            ShaderComboIndex = 2;
        }
        if (vertexShaderVersion == "0x0101" && pixelShaderVersion == "0x0101")
        {
            ShaderComboIndex = 3;
        }
        if (vertexShaderVersion == "0" && pixelShaderVersion == "0")
        {
            ShaderComboIndex = 4;
        }

        // Find selected resolution in available resolution list
        // If it exists, set it as the active resolution
        if (!string.IsNullOrEmpty(screenWidth) && !string.IsNullOrEmpty(screenHeight) && !string.IsNullOrEmpty(refreshRate))
        {
            foreach (var res in AvailableResolutions)
            {
                if (res == $"{screenWidth}x{screenHeight}@{refreshRate}")
                {
                    SelectedResolution = res;
                }
            }
        }
    }

}
