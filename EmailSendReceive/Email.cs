using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Security;

namespace EmailSendReceive
{
    class Email
    {
        public static bool IsValidEmail(string email)  //walidacja formatu e-maila
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
    }
}
