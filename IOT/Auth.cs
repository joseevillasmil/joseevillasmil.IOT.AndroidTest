using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace joseevillasmil.IOT.AndroidTest.IOT
{
    public static class Auth
    {
        private static string sharedSecret = "Shared IOT KEY";
        private static byte[] _salt = Encoding.UTF8.GetBytes("Shared Salt.");
        public static string EncryptStringAES(string plainText)
        {

            string outStr = null;
            RijndaelManaged aesAlg = null;

            try
            {
                Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(sharedSecret, _salt);

                aesAlg = new RijndaelManaged();
                aesAlg.Key = key.GetBytes(aesAlg.KeySize / 8);

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    // prepend the IV
                    msEncrypt.Write(BitConverter.GetBytes(aesAlg.IV.Length), 0, sizeof(int));
                    msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                    }
                    outStr = Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
            finally
            {
                if (aesAlg != null)
                    aesAlg.Clear();
            }
            return outStr;
        }

        public static string DecryptStringAES(string cipherText)
        {

            RijndaelManaged aesAlg = null;
            string plaintext = null;

            try
            {
                Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(sharedSecret, _salt);

                byte[] bytes = Convert.FromBase64String(cipherText);
                using (MemoryStream msDecrypt = new MemoryStream(bytes))
                {

                    aesAlg = new RijndaelManaged();
                    aesAlg.Key = key.GetBytes(aesAlg.KeySize / 8);
                    aesAlg.IV = ReadByteArray(msDecrypt);

                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            plaintext = srDecrypt.ReadToEnd();
                    }
                }
            }
            finally
            {
                if (aesAlg != null)
                    aesAlg.Clear();
            }

            return plaintext;
        }

        private static byte[] ReadByteArray(Stream s)
        {
            byte[] rawLength = new byte[sizeof(int)];
            if (s.Read(rawLength, 0, rawLength.Length) != rawLength.Length)
            {
                throw new SystemException("Stream did not contain properly formatted byte array");
            }

            byte[] buffer = new byte[BitConverter.ToInt32(rawLength, 0)];
            if (s.Read(buffer, 0, buffer.Length) != buffer.Length)
            {
                throw new SystemException("Did not read byte array properly");
            }

            return buffer;
        }

        public static string GenerateToken(string role)
        {
            AuthToken token = new AuthToken()
            {
                startAt = DateTime.UtcNow,
                endsAt = DateTime.UtcNow.AddMinutes(60),
                role = role
            };

            token.Signature = EncryptStringAES(token.ToString());
            string json = JsonSerializer.Serialize(token);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        }

        public static bool ValidateToken(string _token)
        {
            AuthToken token = JsonSerializer.Deserialize<AuthToken>(Encoding.UTF8.GetString(Convert.FromBase64String(_token)));
            if (token.startAt <= DateTime.Now && token.endsAt >= DateTime.Now)
            {
                string comp1 = DecryptStringAES(token.Signature);
                string comp2 = token.ToString();
                if (String.Equals(comp1, comp2))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class AuthToken
    {
        public DateTime startAt { get; set; }
        public DateTime endsAt { get; set; }
        public string role { get; set; }
        public string Signature { get; set; }

        public override string ToString()
        {
            return $"{role}|{endsAt.Ticks}|{endsAt.Ticks}";
        }
    }
}