using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace ViewModels;

internal class SettingsViewModelProperties : ObservableObject
{
    private string? _selectedResolution;
    private ObservableCollection<string>? _availableResolutions;
    private int _shaderComboIndex;
    private int _memoryComboIndex;
    private int _fpsComboIndex;
    private int _maxZoomComboIndex;
    private bool _windowModeCheckbox;
    private bool _borderlessWindowCheckbox;
    private bool _disableVsyncCheckbox;
    private bool _skipIntroCheckbox;
    private bool _disableAudioCheckbox;
    private bool _disableLodManagerCheckbox;
    private bool _disableWorldPreloadingCheckbox;
    private bool _useLowDetailCharactersCheckbox;
    private bool _useLowDetailMeshesCheckbox;
    private bool _disableTextureBakingCheckbox;
    private bool _disableBumpMappingCheckbox;
    private bool _useLowDetailTexturesCheckbox;
    private bool _useLowDetailNormalMapsCheckbox;
    private bool _disableMultiPassRenderingCheckbox;
    private bool _disableHardwareMouseCursorCheckbox;
    private bool _disableFastMouseCursorCheckbox;
    private bool _useSafeRendererCheckbox;
    private bool _disableFileCachingCheckbox;
    private bool _disableAsynchronousLoaderCheckbox;

    public string? SelectedResolution
    {
        get => _selectedResolution;
        set => SetProperty(ref _selectedResolution, value);
    }

    public ObservableCollection<string>? AvailableResolutions
    {
        get => _availableResolutions;
        set => SetProperty(ref _availableResolutions, value);
    }

    public int ShaderComboIndex
    {
        get => _shaderComboIndex;
        set => SetProperty(ref _shaderComboIndex, value);
    }

    public int MemoryComboIndex
    {
        get => _memoryComboIndex;
        set => SetProperty(ref _memoryComboIndex, value);
    }

    public int FpsComboIndex
    {
        get => _fpsComboIndex; 
        set => SetProperty(ref _fpsComboIndex, value);
    }

    public int MaxZoomComboIndex
    {
        get => _maxZoomComboIndex; 
        set => SetProperty(ref _maxZoomComboIndex, value);
    }

    public bool WindowModeCheckbox
    {
        get => _windowModeCheckbox;
        set => SetProperty(ref _windowModeCheckbox, value);
    }

    public bool BorderlessWindowCheckbox
    {
        get => _borderlessWindowCheckbox;
        set => SetProperty(ref _borderlessWindowCheckbox, value);
    }

    public bool DisableVsyncCheckbox
    {
        get => _disableVsyncCheckbox;
        set => SetProperty(ref _disableVsyncCheckbox, value);
    }

    public bool SkipIntroCheckbox
    {
        get => _skipIntroCheckbox;
        set => SetProperty(ref _skipIntroCheckbox, value);
    }

    public bool DisableAudioCheckbox
    {
        get => _disableAudioCheckbox;
        set => SetProperty(ref _disableAudioCheckbox, value);
    }

    public bool DisableLodManagerCheckbox
    {
        get => _disableLodManagerCheckbox;
        set => SetProperty(ref _disableLodManagerCheckbox, value);
    }

    public bool DisableWorldPreloadingCheckbox
    {
        get => _disableWorldPreloadingCheckbox;
        set => SetProperty(ref _disableWorldPreloadingCheckbox, value);
    }

    public bool UseLowDetailCharactersCheckbox
    {
        get => _useLowDetailCharactersCheckbox;
        set => SetProperty(ref _useLowDetailCharactersCheckbox, value);
    }

    public bool UseLowDetailMeshesCheckbox
    {
        get => _useLowDetailMeshesCheckbox;
        set => SetProperty(ref _useLowDetailMeshesCheckbox, value);
    }

    public bool DisableTextureBakingCheckbox
    {
        get => _disableTextureBakingCheckbox;
        set => SetProperty(ref _disableTextureBakingCheckbox, value);
    }

    public bool DisableBumpMappingCheckbox
    {
        get => _disableBumpMappingCheckbox;
        set => SetProperty(ref _disableBumpMappingCheckbox, value);
    }

    public bool UseLowDetailTexturesCheckbox
    {
        get => _useLowDetailTexturesCheckbox;
        set => SetProperty(ref _useLowDetailTexturesCheckbox, value);
    }

    public bool UseLowDetailNormalMapsCheckbox
    {
        get => _useLowDetailNormalMapsCheckbox;
        set => SetProperty(ref _useLowDetailNormalMapsCheckbox, value);
    }

    public bool DisableMultiPassRenderingCheckbox
    {
        get => _disableMultiPassRenderingCheckbox;
        set => SetProperty(ref _disableMultiPassRenderingCheckbox, value);
    }

    public bool DisableHardwareMouseCursorCheckbox
    {
        get => _disableHardwareMouseCursorCheckbox;
        set => SetProperty(ref _disableHardwareMouseCursorCheckbox, value);
    }

    public bool DisableFastMouseCursorCheckbox
    {
        get => _disableFastMouseCursorCheckbox;
        set => SetProperty(ref _disableFastMouseCursorCheckbox, value);
    }

    public bool UseSafeRendererCheckbox
    {
        get => _useSafeRendererCheckbox;
        set => SetProperty(ref _useSafeRendererCheckbox, value);
    }

    public bool DisableFileCachingCheckbox
    {
        get => _disableFileCachingCheckbox;
        set => SetProperty(ref _disableFileCachingCheckbox, value);
    }

    public bool DisableAsynchronousLoaderCheckbox
    {
        get => _disableAsynchronousLoaderCheckbox;
        set => SetProperty(ref _disableAsynchronousLoaderCheckbox, value);
    }
}
