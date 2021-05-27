﻿using System.Collections.Generic;

namespace LauncherManagement
{
    public class GameLoginResponseProperties
    {
        public string Result { get; set; }
        public string Username { get; set; }
        public List<string> Characters { get; set; }
    }

    public class GameAccountCreationProperties
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string Discord { get; set; }
        public bool SubscribeToNewsletter { get; set; }
    }

    public class CaptchaProperties
    {
        public int Value1 { get; set; }
        public int Value2 { get; set; }
        public int Answer { get; set; }
    }

    public class GameAccountCreationResponseProperties
    {
        public string Result { get; set; }
    }
}
