using System;
using System.Security.Cryptography;
using System.Text;

namespace EmailSendReceive
{
    class Crypto    //klasa służąca do przeprowadzania operacji kryptograficznych 
    {
        private static RSACryptoServiceProvider csp = new RSACryptoServiceProvider(2048);   //obiekt odpowiadający za operacje kryptograficzne
        private RSAParameters _privateKey;  //zmienna gdzie będziemy przetrzymywać klucz prywatny
        private RSAParameters _publicKey;   //zmienna gdzie będziemy przetrzymywać klucz publiczny
        public Crypto()
        {
            _privateKey = csp.ExportParameters(true);   //eksportujemy klucz prywatny wartość true - klucz prywatny
            _publicKey = csp.ExportParameters(false);   //eksportujemy klucz publiczny wartość false - klucz publiczny
        }

        public string Encypt(string plainText)  //funkcja do szyfrowania
        {
            csp = new RSACryptoServiceProvider();   //inicjalizujemy nową instancję RSACryptoServiceProvider
            csp.ImportParameters(_publicKey);   //importujemy klucz publiczny
            byte[] data = Encoding.Unicode.GetBytes(plainText); //kodujemy tablice na unicode
            byte[] cypher = csp.Encrypt(data, false);   //deklaracja zmiennej cypher gdzie przetrzymujemy zaszyfrowany tekst (kluczem publicznym)
            return Convert.ToBase64String(cypher);  //zwracamy zmienną kodowaną w Base64
        }

        public string Decrypt(string cypherText)    //funkcja do deszyfrowania
        {
            byte[] dataBytes = Convert.FromBase64String(cypherText);    //deklaracja zmiennej dataBytes
            csp.ImportParameters(_privateKey);  //importujemy klucz prywatny 
            byte[] plainText = csp.Decrypt(dataBytes, false);   //deklaracja zmiennej plainText gdzie przetrzymujemy odszyfrowany tekst (kluczem prywatnym)
            return Encoding.Unicode.GetString(plainText);   //zwracamy zmienną plainText zakodowaną w Unicode
        }

        public string CreateSha256Hash(string input)   //funkcja do generowania haszu SHA256
        {
            SHA256 sha256Hash = SHA256.Create();
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            StringBuilder builder = new StringBuilder();    //zamiana tablicy bajtów na string zapisanym w systemie heksadecymalnym
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }

        public string CreateMD5(string input)   //funkcja do generowania haszu MD5
        {
            MD5 md5 = MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            StringBuilder sb = new StringBuilder(); //zamiana tablicy bajtów na string zapisanym w systemie heksadecymalnym
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }
}
