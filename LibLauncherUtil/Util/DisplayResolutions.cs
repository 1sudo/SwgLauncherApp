using System.Runtime.InteropServices;

namespace LibLauncherUtil.Util;

public static class DisplayResolutions
{
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments
    [DllImport("user32.dll")]
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
#pragma warning disable CA1401 // P/Invokes should not be visible
    public static extern bool EnumDisplaySettings(string? deviceName, int modeNum, ref DEVMODE devMode);
#pragma warning restore CA1401 // P/Invokes should not be visible

    [StructLayout(LayoutKind.Sequential)]
    public struct DEVMODE
    {
        const int CCHDEVICENAME = 0x20;
        const int CCHFORMNAME = 0x20;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public ScreenOrientation dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    public static List<string> GetResolutions()
    {
        DEVMODE vDevMode = new();

        List<string> resolutions = new();

        string lastResolution = "";

        int x = 0;
        while (EnumDisplaySettings(null, x, ref vDevMode))
        {
            int width = vDevMode.dmPelsWidth;
            int height = vDevMode.dmPelsHeight;
            int hz = vDevMode.dmDisplayFrequency;

            string currentResolution = $"{width}x{height}@{hz}";

            if (currentResolution != lastResolution)
            {
                resolutions.Add(currentResolution);
            }

            lastResolution = currentResolution;
            x++;
        }

        resolutions.Reverse();

        return resolutions.Distinct().ToList();
    }
}
