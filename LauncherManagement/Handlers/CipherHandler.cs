using System;
using System.Threading.Tasks;

namespace LauncherManagement
{
    public class CipherHandler
    {
        readonly char[] _shift = new char[char.MaxValue];

        public CipherHandler()
        {
            for (int i = 0; i < char.MaxValue; i++)
            {
                _shift[i] = (char)i;
            }

            for (char c = 'A'; c <= 'Z'; c++)
            {
                char x;
                if (c <= 'M')
                {
                    x = (char)(c + 13);
                }
                else
                {
                    x = (char)(c - 13);
                }
                _shift[(int)c] = x;
            }
            for (char c = 'a'; c <= 'z'; c++)
            {
                char x;
                if (c <= 'm')
                {
                    x = (char)(c + 13);
                }
                else
                {
                    x = (char)(c - 13);
                }
                _shift[(int)c] = x;
            }
        }

        public async Task<string> Transform(string value)
        {
            try
            {
                char[] a = value.ToCharArray();

                for (int i = 0; i < a.Length; i++)
                {
                    int t = (int)a[i];
                    a[i] = _shift[t];
                }

                return new string(a);
            }
            catch (Exception e)
            {
                await LogHandler.Log(LogType.ERROR, "| (CipherHandler)Transform | " + e.Message.ToString());

                return value;
            }
        }

        public static string Encode(string plainText)
        {
            byte[] plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        public static string Decode(string base64EncodedData)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}
