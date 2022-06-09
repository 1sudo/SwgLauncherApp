using System;
using LauncherApp.Models.Properties;

namespace LauncherApp.Models.Handlers
{
    public static class CaptchaHandler
    {
        public static CaptchaProperties QuestionAndAnswer()
        {
            int val1 = GetRandomNumber(30);
            int val2 = GetRandomNumber(30);

            return new CaptchaProperties
            {
                Value1 = val1,
                Value2 = val2,
                Answer = val1 + val2,
            };
        }

        internal static int GetRandomNumber(int maxVal)
        {
            Random rand = new();

            return rand.Next(1, maxVal);
        }
    }
}
