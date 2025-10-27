using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class RsaEncryptionHelper
{
    private static readonly string PublicKeyXml;
    private static readonly string PrivateKeyXml;

    // Static constructor runs once when the class is first used
    static RsaEncryptionHelper()
    {
        try
        {
            // Load keys from predefined locations
            PublicKeyXml = File.ReadAllText(@"C:\Users\patri\Documents\FTN 6 semestar\Projekti\SNUS\snus-robot-arm\RobotArmServer\publicKey.xml");
            PrivateKeyXml = File.ReadAllText(@"C:\Users\patri\Documents\FTN 6 semestar\Projekti\SNUS\snus-robot-arm\RobotArmServer\privateKey.xml");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to load RSA keys: " + ex.Message);
            throw;
        }
    }

    public static string Encrypt(string plainText)
    {
        using var rsa = new RSACryptoServiceProvider();
        rsa.PersistKeyInCsp = false;

        rsa.FromXmlString(PublicKeyXml);
        byte[] bytes = Encoding.UTF8.GetBytes(plainText);
        byte[] encrypted = rsa.Encrypt(bytes, false);
        return Convert.ToBase64String(encrypted);
    }

    public static string Decrypt(string cipherTextBase64)
    {
        if (string.IsNullOrWhiteSpace(cipherTextBase64))
            throw new ArgumentNullException(nameof(cipherTextBase64), "Encrypted command is null or empty");

        byte[] bytes = Convert.FromBase64String(cipherTextBase64); // safe now
        using var rsa = RSA.Create();
        rsa.FromXmlString(PrivateKeyXml);
        var decryptedBytes = rsa.Decrypt(bytes, RSAEncryptionPadding.Pkcs1);
        return Encoding.UTF8.GetString(decryptedBytes);
    }

}
