using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Security;

namespace EmailSendReceive
{
    class Program
    {
        static void Main(string[] args) //funkcja główna
        {
            string login;   //zmienna do przetrzymywania loginu
            SecureString password = new SecureString(); //obiekt do przetrzymywania hasła
            Console.Write("E-mail adress: ");
            login = Console.ReadLine(); //podaj login
            if (!Email.IsValidEmail(login))
            {
                Console.WriteLine("~~Wrong E-mail format~~");
                Environment.Exit(1);
            }
            Console.Write("E-mail password: ");

            do
            {
                ConsoleKeyInfo key = Console.ReadKey(true); //odczytaj wprowadzoną litere
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter) //jeśli wciśniety klawisz nie jest backspace oraz enter
                {
                    password.AppendChar(key.KeyChar);   //dodaj do zmiennej password literę którą wprowadziliśmy
                    Console.Write("*");         //wyświetl * 
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && password.Length > 0) //jeśli wciśniety klawisz jest backspace oraz zmienna password jest większa od 0
                    {
                        password.RemoveAt(password.Length - 1); //usun ze zmiennej password literę którą wprowadziliśmy 
                        Console.Write("\b \b"); //usuń z wyświetlania znak backspace oraz literę którą wprowadziliśmy
                    }
                    else if (key.Key == ConsoleKey.Enter)   //jeśli wciśniety klawisz jest enterem
                    {
                        break;  //wyjdz z pętli
                    }
                }
            } while (true);

            Crypto crypto = new Crypto();   //inicjalizacja obiektu Crypto
            Console.Write(Environment.NewLine); 
            Console.Write("Provide the path of the JSON file to send and read data: ");
            string path = Console.ReadLine();   //wprowadzamy ściezkę do pliku z parametrami
            StreamReader r = new StreamReader(path);
            string data = r.ReadToEnd();
            JObject jsonObject = JObject.Parse(data); //do konwersji pliku JSON posłużyłem się biblioteką Newtonsoft.Json (NuGet Command: PM> Install-Package Newtonsoft.Json)
            string textEncyptToSend = crypto.Encypt(jsonObject["textBody"].ToString());
            //string checksumToSend = crypto.CreateMD5(textEncyptToSend);
            string checksumToSend = crypto.CreateSha256Hash(textEncyptToSend);
            string textBodyEncyptMain = String.Format("{0}{1}{2}", textEncyptToSend, Environment.NewLine, checksumToSend);
            bool resultMain = Email.SendEmail(jsonObject["smtpHost"].ToString(), Convert.ToInt32(jsonObject["smtpPort"]), login, password, jsonObject["to"].ToString(), jsonObject["subject"].ToString(), textBodyEncyptMain);   //wywołujemy funkcję SendEmail aby wysłać e-mail. W parametrze textBody przesyłamy zaszyfrowaną wiadomość za pomocą funkcji Encrypt korzystającej z klucza publicznego
            if (resultMain == true) //wysyłka e-maila powiodła się
            {
                Console.WriteLine("~~E-mail sended~~");
            }
            else //wysyłka e-maila nie powiodła się
            {
                Console.WriteLine("~~E-mail not sended~~");
                Environment.Exit(1);
            }  
            Console.Write(Environment.NewLine);
            MimeMessage mimeMessageMain = Email.ReceiveEmail(jsonObject["imapHost"].ToString(), Convert.ToInt32(jsonObject["imapPort"]), login, password);    //aby odczytać wiadomość wywołujemy funkcję ReceiveEmail, którą zapisujemy do zmiennej mimeMessageMain
            if (mimeMessageMain != null) //odebranie e-maila powiodło się
            {
                Console.WriteLine("~~E-mail received~~");
            }
            else //odebranie e-maila nie powiodło się
            {
                Console.WriteLine("~~E-mail not received~~");
                Environment.Exit(1);
            }
            string textEncyptToRead = mimeMessageMain.TextBody.Split(Environment.NewLine)[0];
            string checksumToRead = mimeMessageMain.TextBody.Split(Environment.NewLine)[1];
            if (checksumToSend == checksumToRead)
            {
                Console.WriteLine("~~E-mail checksum correct~~");
            }
            else
            {
                Console.WriteLine("~~E-mail checksum not correct~~");
                Environment.Exit(1);
            }
            Console.WriteLine("Message ID: {0}", mimeMessageMain.MessageId);    //wyświetlamy takie parametry jak: id wiadomości, do kogo, date wysłania, temat
            Console.WriteLine("From: {0}", mimeMessageMain.From);   //od kogo
            Console.WriteLine("To: {0}", mimeMessageMain.To);   //do kogo
            Console.WriteLine("Date: {0}", mimeMessageMain.Date);   //data wysłania
            Console.WriteLine("Subject: {0}", mimeMessageMain.Subject); //temat
            
            Console.WriteLine(mimeMessageMain.Body.ContentType);    //identyfikator formatu wiadomości
            Console.WriteLine("textBody message (encypted): {0}", textEncyptToRead);    //wyświetlamy zaszyfrowaną treść wiadomości
            Console.WriteLine("textBody message (decrypted): {0}", crypto.Decrypt(textEncyptToRead));   //wyświetlamy treść wiadomości odszyfrowaną funkcją Decrypt korzystającej z klucza prywatnego
            Console.WriteLine("textBody checksum: {0}", checksumToRead);   //wyświetlamy sume kontrolną wiadomości 
            Console.Write(Environment.NewLine);
            
            string emlDir = string.Format(Directory.GetCurrentDirectory() + "\\{0}.eml", mimeMessageMain.MessageId);    //zmienna emlDir do przetrzymywania ścieżki nowo wygenerowanego pliku
            mimeMessageMain.WriteTo(emlDir); // write the message to a file
            Console.WriteLine("E-mail file saved at: {0}", emlDir); //wyświetl ścieżkę gdzie zapisaliśmy plik EML
        }
    }
}
