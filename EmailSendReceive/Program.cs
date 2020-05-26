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
        static bool IsValidEmail(string email)  //walidacja formatu e-maila
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
        public static bool SendEmail(string smtpHost, Int32 smtpPort, string from, SecureString password, string to, string subject, string textBodyEncrypt) //funkcja odpowiadająca za wysyłanie naszego e-maila
        {
            bool result = false;
            SmtpClient smtpClient = new SmtpClient(smtpHost, smtpPort); //inicjalizujemy obiekt 
            smtpClient.Credentials = new NetworkCredential(from, password); //załączamy poświadczenia
            smtpClient.EnableSsl = true;    //umożliwamy szyfrowanie e-maila poprzez protokół SSL 

            MailMessage mailMessage = new MailMessage(from, to, subject, textBodyEncrypt); //inicjalizujemy obiekt mailMessage 
            mailMessage.BodyEncoding = System.Text.Encoding.UTF8;   //wybieramy sposób kodowania e-maila UTF8
            try
            {
                smtpClient.Send(mailMessage);   //wysyłanie e-maila
                result = true;
            }
            catch (SmtpFailedRecipientsException ex)    //obsługa wyjątków
            {
                for (int i = 0; i < ex.InnerExceptions.Length; i++) //iteracja po błędach 
                {
                    SmtpStatusCode status = ex.InnerExceptions[i].StatusCode;
                    if (status == SmtpStatusCode.MailboxBusy || status == SmtpStatusCode.MailboxUnavailable) //jeśli nie mogliśmy wysłać e-maila 
                    {
                        Console.WriteLine("~~Delivery failed - retrying in 5 seconds~~");
                        System.Threading.Thread.Sleep(5000);
                        smtpClient.Send(from, to, subject, textBodyEncrypt); //próbujemy wysłać e-maila ponownie co 5 sekund
                    }
                    else
                    {
                        Console.WriteLine("~~Failed to deliver message to {0}~~", ex.InnerExceptions[i].FailedRecipient);   //wysyłka e-maila nie powiodła się
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("~~Exception caught in RetryIfBusy(): {0}~~", ex.ToString());
            }

            return result;
        }
        public static MimeMessage ReceiveEmail(string imapHost, Int32 imapPort, string from, SecureString password)   //funkcja odpowiadająca za odbieranie ostatniego e-maila (tego którego wysłaliśmy)
        {
            ImapClient client = new ImapClient();   //inicjalizujemy obiekt imapClient
            try
            {
                client.Connect(imapHost, imapPort, SecureSocketOptions.SslOnConnect);   //łączymy się 
            }
            catch (ImapProtocolException ex)    //obsługa wyjątku
            {
                Console.WriteLine("~~Cannot connect with client: {0}~~", ex.ToString());    
            }
            
            client.Authenticate(from, new System.Net.NetworkCredential(string.Empty, password).Password);   //autentykacja
            client.Inbox.Open(FolderAccess.ReadOnly);   //otwieramy skrzynkę e-mail

            IList<UniqueId> uids = client.Inbox.Search(SearchQuery.All);    //pobieramy wszystkie e-maile ze skrzynki (najstarsze na początku, najnowsze na końcu)
            MimeMessage mimeMessageReceiveEmail = client.Inbox.GetMessage(uids[uids.Count - 1]);    //pobieramy najnowszy e-mail i umieszczamy go w obiekcie mimeMessageReceiveEmail
            client.Disconnect(true);    //rozłączamy się
            if (mimeMessageReceiveEmail.From.ToString() != from)    //odebranie e-maila nie powiodło się
            {
                mimeMessageReceiveEmail = null;
            }

            return mimeMessageReceiveEmail; //zwracamy obiekt mimeMessage
        }
        static void Main(string[] args) //funkcja główna
        {
            string login;   //zmienna do przetrzymywania loginu
            SecureString password = new SecureString(); //obiekt do przetrzymywania hasła
            Console.Write("E-mail adress: ");
            login = Console.ReadLine(); //podaj login
            if (!IsValidEmail(login))
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
            bool resultMain = SendEmail(jsonObject["smtpHost"].ToString(), Convert.ToInt32(jsonObject["smtpPort"]), login, password, jsonObject["to"].ToString(), jsonObject["subject"].ToString(), crypto.Encypt(jsonObject["textBody"].ToString()));    //wywołujemy funkcję SendEmail aby wysłać e-mail. W parametrze textBody przesyłamy zaszyfrowaną wiadomość za pomocą funkcji Encrypt korzystającej z klucza publicznego
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
            MimeMessage mimeMessageMain = ReceiveEmail(jsonObject["imapHost"].ToString(), Convert.ToInt32(jsonObject["imapPort"]), login, password);    //aby odczytać wiadomość wywołujemy funkcję ReceiveEmail, którą zapisujemy do zmiennej mimeMessageMain
            if (mimeMessageMain != null) //odebranie e-maila powiodło się
            {
                Console.WriteLine("~~E-mail received~~");
            }
            else //odebranie e-maila nie powiodło się
            {
                Console.WriteLine("~~E-mail not received~~");
                Environment.Exit(1);
            }
            Console.WriteLine("Message ID: {0}", mimeMessageMain.MessageId);    //wyświetlamy takie parametry jak: id wiadomości, do kogo, date wysłania, temat
            Console.WriteLine("From: {0}", mimeMessageMain.From);   //od kogo
            Console.WriteLine("To: {0}", mimeMessageMain.To);   //do kogo
            Console.WriteLine("Date: {0}", mimeMessageMain.Date);   //data wysłania
            Console.WriteLine("Subject: {0}", mimeMessageMain.Subject); //temat
            Console.WriteLine(mimeMessageMain.Body.ContentType);    //identyfikator formatu wiadomości
            Console.WriteLine("textBody (encypted): {0}", mimeMessageMain.TextBody);    //wyświetlamy zaszyfrowaną treść wiadomości
            Console.WriteLine("textBody (decrypted): {0}", crypto.Decrypt(mimeMessageMain.TextBody));   //wyświetlamy treść wiadomości odszyfrowaną funkcją Decrypt korzystającej z klucza prywatnego
            Console.Write(Environment.NewLine);
            
            string emlDir = string.Format(Directory.GetCurrentDirectory() + "\\{0}.eml", mimeMessageMain.MessageId);    //zmienna emlDir do przetrzymywania ścieżki nowo wygenerowanego pliku
            mimeMessageMain.WriteTo(emlDir); // write the message to a file
            Console.WriteLine("E-mail file saved at: {0}", emlDir); //wyświetl ścieżkę gdzie zapisaliśmy plik EML
        }
    }
}
