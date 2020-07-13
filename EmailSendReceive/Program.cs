using MimeKit;
using Newtonsoft.Json.Linq;
using System;
using System.Security;

namespace EmailSendReceive
{
    class Program
    {
        static void Main(string[] args) //funkcja główna
        {
            string login;   //zmienna do przetrzymywania loginu
            SecureString passwordProgram = new SecureString(); //obiekt do przetrzymywania hasła
            Console.Write("E-mail adress: ");
            login = Console.ReadLine(); //podaj login
            EmailHandler.IsValidEmail(login);  //funkcja odpowiadająca za walidacje e-maila
            Console.Write("E-mail password: ");
            passwordProgram = EmailHandler.PasswordReader();   //funkcja odpowiadająca za wczytywanie hasła

            Crypto crypto = new Crypto();   //inicjalizacja obiektu Crypto
            Console.Write(Environment.NewLine);
            Console.Write("Provide the path of the JSON file to send and read data: ");
            string jsonPath = Console.ReadLine();   //wprowadzamy ściezkę do pliku z parametrami

            EmailParams jsonObjectProg = JsonParser.DeserializeObject(jsonPath); //funkcja odpowiadająca za parsowanie z pliku do obiektu typu JSON

            string textEncyptToSend = crypto.Encypt(jsonObjectProg.textBody); //funkcja odpowiadająca za szyfrowanie treści wiadomości
            string checksumToSend = crypto.CreateSha256Hash(textEncyptToSend);  //funkcja odpowiadająca za generowanie sumy kontrolnej zaszyfrowanej treści wiadomosci
            string bodyProg = String.Format("{0}{1}{2}", textEncyptToSend, Environment.NewLine, checksumToSend);    //cała wiadomość ma nastepującą formę: zaszyfrowana wiadomość + znak nowej linii + suma kontrolna zaszyfrowanej wiadmosci

            string txtPath = "";    //zmienna gdzie bedziemy przetrzymywać lokalizacje pliku z wzorem podpisu cyfrowego
            bool isDkimProg = EmailHandler.EmailDKIMOrNo();    // funckja gdzie decydujemy czy nasz e-mail ma miec podpis cyfrowy bądz nie
            if (isDkimProg) //jeśli e-mail ma mieć podpid cyfrowy
            {
                Console.Write("Provide the path of the TXT file to with DKIM: ");   //to podaj ścieżkę pliku z wzorem podpisu cyfrowego
                txtPath = Console.ReadLine();
            }

            EmailHandler.SendEmail(jsonObjectProg.smtpHost, jsonObjectProg.smtpPort, login, passwordProgram, jsonObjectProg.to, jsonObjectProg.subject, bodyProg, isDkimProg, txtPath);   //wywołujemy funkcję SendEmail aby wysłać e-mail. W parametrze textBody przesyłamy zaszyfrowaną wiadomość za pomocą funkcji Encrypt korzystającej z klucza publicznego

            Console.Write(Environment.NewLine);
            MimeMessage mimeMessageProg = EmailHandler.ReceiveEmail(jsonObjectProg.imapHost, jsonObjectProg.imapPort, login, passwordProgram);    //aby odczytać wiadomość wywołujemy funkcję ReceiveEmail, którą zapisujemy do zmiennej mimeMessageProg

            string textEncyptToRead = mimeMessageProg.TextBody.Split(Environment.NewLine)[0];
            string checksumToRead = mimeMessageProg.TextBody.Split(Environment.NewLine)[1];
            if (checksumToSend == checksumToRead)   //jeśli sumy kontrole są takie same
            {
                Console.WriteLine("~~E-mail checksum correct~~");   //wyświetl informację że suma kontrolna jest prawidłowa
            }
            else
            {
                Console.WriteLine("~~E-mail checksum not correct~~");   //wyświetl informację że suma kontrolna jest nie prawidłowa
                Environment.Exit(1);
            }
            Console.WriteLine("Message ID: {0}", mimeMessageProg.MessageId);    //wyświetlamy takie parametry jak: id wiadomości, do kogo, date wysłania, temat
            Console.WriteLine("From: {0}", mimeMessageProg.From);   //od kogo
            Console.WriteLine("To: {0}", mimeMessageProg.To);   //do kogo
            Console.WriteLine("Date: {0}", mimeMessageProg.Date);   //data wysłania
            Console.WriteLine("Subject: {0}", mimeMessageProg.Subject); //temat

            Console.WriteLine(mimeMessageProg.Body.ContentType);    //identyfikator formatu wiadomości
            Console.WriteLine("textBody message (encypted): {0}", textEncyptToRead);    //wyświetlamy zaszyfrowaną treść wiadomości
            Console.WriteLine("textBody message (decrypted): {0}", crypto.Decrypt(textEncyptToRead));   //wyświetlamy treść wiadomości odszyfrowaną funkcją Decrypt korzystającej z klucza prywatnego
            Console.WriteLine("textBody checksum: {0}", checksumToRead);   //wyświetlamy sume kontrolną wiadomości 
            Console.Write(Environment.NewLine);

            EmailHandler.SaveEmlFile(mimeMessageProg); //funkcja zapisująca e-maila do pliku .eml
        }
    }
}
