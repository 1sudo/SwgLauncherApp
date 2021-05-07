using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LauncherManagement
{
    public class DisplayResolutions
    {
        [DllImport("user32.dll")]
        public static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

        [StructLayout(LayoutKind.Sequential)]
        public struct DEVMODE
        {
            private const int CCHDEVICENAME = 0x20;
            private const int CCHFORMNAME = 0x20;
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
            DEVMODE vDevMode = new DEVMODE();

            List<string> resolutions = new List<string>();

            int lastWidth = 0;
            int lastHeight = 0;

            int x = 0;
            while (EnumDisplaySettings(null, x, ref vDevMode))
            {
                int width = vDevMode.dmPelsWidth;
                int height = vDevMode.dmPelsHeight;
                int hz = vDevMode.dmDisplayFrequency;

                if (width != lastWidth && height != lastHeight)
                {
                    resolutions.Add($"{width}x{height}@{hz}");
                }

                lastWidth = width;
                lastHeight = height;
                x++;
            }

            return resolutions;
        }
    }
}
