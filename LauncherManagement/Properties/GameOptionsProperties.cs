namespace LauncherManagement
{
    public static class ServerSelection
    {
        public static int ActiveServer { get; set; }
    }

    public static class GameOptionsProperties
    {
        private static int fps;
        private static int ram;
        private static int maxZoom;

        public static int Fps { get => fps; set => fps = value; }
        public static int Ram { get => ram; set => ram = value; }
        public static int MaxZoom { get => maxZoom; set => maxZoom = value; }
    }
}
