using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Web.Extensions
{
    internal static class ByteExtensions
    {
        internal static byte[] Encrypt<T>(this T src, string key)
        {
            byte[] result;

            using (var aes = Aes.Create())
            using (var md5 = MD5.Create())
            using (var sha256 = SHA256.Create())
            {
                aes.Key = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
                aes.IV = md5.ComputeHash(Encoding.UTF8.GetBytes(key));

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (var resultStream = new MemoryStream())
                {
                    using (var aesStream = new CryptoStream(resultStream, encryptor, CryptoStreamMode.Write))
                    using (var plainStream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(src))))
                    {
                        plainStream.CopyTo(aesStream);
                    }

                    result = resultStream.ToArray();
                }
            }

            return result;
        }

        public static T Decrypt<T>(this byte[] message, string key)
        {
            byte[] result;

            using (var aes = Aes.Create())
            using (var md5 = MD5.Create())
            using (var sha256 = SHA256.Create())
            {
                aes.Key = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
                aes.IV = md5.ComputeHash(Encoding.UTF8.GetBytes(key));

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                using (var resultStream = new MemoryStream())
                {
                    using (var aesStream = new CryptoStream(resultStream, decryptor, CryptoStreamMode.Write))
                    using (var plainStream = new MemoryStream(message))
                    {
                        plainStream.CopyTo(aesStream);
                    }

                    result = resultStream.ToArray();
                }
            }

            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(result));
        }
    }
}
