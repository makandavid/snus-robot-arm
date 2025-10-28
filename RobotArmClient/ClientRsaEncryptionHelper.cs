using System.Security.Cryptography;
using System.Text;

namespace RobotArmClient
{
    public static class ClientRsaEncryptionHelper
    {
        private static readonly string PublicKeyXml;

        static ClientRsaEncryptionHelper()
        {
            try
            {
                // Load keys from predefined locations
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                PublicKeyXml = File.ReadAllText(Path.Combine(baseDir, "publicKey.xml"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to load server public key: " + ex.Message);
                throw;
            }
        }

        public static string Encrypt(string plainText)
        {
            using var rsa = new RSACryptoServiceProvider();
            rsa.PersistKeyInCsp = false;

            rsa.FromXmlString(PublicKeyXml);
            byte[] bytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encrypted = rsa.Encrypt(bytes, false); // PKCS#1 v1.5 padding
            return Convert.ToBase64String(encrypted);
        }
    }
}