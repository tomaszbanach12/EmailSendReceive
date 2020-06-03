using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using MimeKit.Cryptography;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

using System.Security;

namespace EmailSendReceive
{
    class Email
    {
        public static void IsValidEmail(string email)  //walidacja formatu e-maila
        {
            System.Net.Mail.MailAddress addr = new System.Net.Mail.MailAddress(email);
            if (addr.Address != email)
            {
                Console.WriteLine("~~Wrong E-mail format~~");
                Environment.Exit(1);
            }
        }

        public static bool EmailDKIMOrNo()  
        {
            bool response;
            string input;
            while (true)
            {
                Console.Write("Do you want to send e-mail with DKIM? [y/n]: ");
                input = Console.ReadLine();
                if (input.ToUpper() == "Y")
                {
                    response = true;
                    break;
                }
                else if (input.ToUpper() == "N")
                {
                    response = false;
                    break;
                }
            }

            return response;
        }

        public static SecureString PasswordReader()
        {
            SecureString passwordEmail = new SecureString();
            do
            {
                ConsoleKeyInfo key = Console.ReadKey(true); //odczytaj wprowadzoną litere
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter) //jeśli wciśniety klawisz nie jest backspace oraz enter
                {
                    passwordEmail.AppendChar(key.KeyChar);   //dodaj do zmiennej password literę którą wprowadziliśmy
                    Console.Write("*");         //wyświetl * 
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && passwordEmail.Length > 0) //jeśli wciśniety klawisz jest backspace oraz zmienna password jest większa od 0
                    {
                        passwordEmail.RemoveAt(passwordEmail.Length - 1); //usun ze zmiennej password literę którą wprowadziliśmy 
                        Console.Write("\b \b"); //usuń z wyświetlania znak backspace oraz literę którą wprowadziliśmy
                    }
                    else if (key.Key == ConsoleKey.Enter)   //jeśli wciśniety klawisz jest enterem
                    {
                        break;  //wyjdz z pętli
                    }
                }
            } while (true);

            return passwordEmail;
        }

        public static void SendEmail(string smtpHost, Int32 smtpPort, string from, SecureString password, string to, string subject, string bodyEmail, bool isDkimEmail, string txtDKIMPath)
        {
            SmtpClient smtpClient = new SmtpClient();   //inicjalizacja klienta smtp z przestrzeni nazw MailKit.Net.Smtp
            try
            {
                smtpClient.Connect(smtpHost, smtpPort, SecureSocketOptions.StartTls);   //łączymy się 
            }
            catch (SmtpCommandException ex) //obsługa wyjątku
            {
                Console.WriteLine("~~Cannot connect with client (SMTP Command Exception): {0}~~", ex.ToString());
                Environment.Exit(1);
            }
            catch (SmtpProtocolException ex)    //obsługa wyjątku
            {
                Console.WriteLine("~~Cannot connect with client (SMTP Protocol Exception): {0}~~", ex.ToString());
                Environment.Exit(1);
            }

            try
            {
                smtpClient.Authenticate(from, new NetworkCredential(string.Empty, password).Password);
            }
            catch (AuthenticationException ex)  //obsługa wyjątku
            {
                Console.WriteLine("~~Authentication error (Authentication Exception): {0}~~", ex.ToString());
                Environment.Exit(1);
            }
            catch (SmtpCommandException ex) //obsługa wyjątku
            {
                Console.WriteLine("~~Authentication error (SMTP Command Exception): {0}~~", ex.ToString());
                Environment.Exit(1);
            }
            catch (SmtpProtocolException ex)    //obsługa wyjątku
            {
                Console.WriteLine("~~Authentication error (SMTP Protocol Exception): {0}~~", ex.ToString());
                Environment.Exit(1);
            }


            smtpClient.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            MimeMessage mimeMessage = new MimeMessage();    //inicjalizujemy obiekt mailMessage 

            if (isDkimEmail)
            {
                string domain = from.Split("@")[1];

                HeaderId[] headers = new HeaderId[] { HeaderId.From, HeaderId.Subject, HeaderId.Date };
                DkimSigner signer = new DkimSigner(txtDKIMPath, domain, "mail", DkimSignatureAlgorithm.RsaSha256)
                {
                    HeaderCanonicalizationAlgorithm = DkimCanonicalizationAlgorithm.Relaxed,
                    BodyCanonicalizationAlgorithm = DkimCanonicalizationAlgorithm.Relaxed,
                    AgentOrUserIdentifier = "@" + domain,
                    QueryMethod = "dns/txt",
                };

                //message.Prepare(EncodingConstraint.SevenBit);
                //message.Prepare(EncodingConstraint.EightBit);

                signer.Sign(mimeMessage, headers);
            }

            mimeMessage.From.Add(new MailboxAddress(from)); //od kogo e-mail
            mimeMessage.To.Add(new MailboxAddress(from));   //do kogo
            mimeMessage.Subject = subject;  //temat
            BodyBuilder bodyBuilder = new BodyBuilder();    //bodyBuilder do tworzenia treści e-maila
            bodyBuilder.TextBody = bodyEmail;   //tekst treści
            mimeMessage.Body = bodyBuilder.ToMessageBody(); //wrzucenie tekstu bodyBuildera do treści e-maila 
            try
            {
                smtpClient.Send(mimeMessage);   //wysyłanie e-maila
            }
            catch (SmtpCommandException ex) //obsługa wyjątku
            {
                Console.WriteLine("~~E-mail has not been sent (SMTP Command Exception): {0}", ex.Message);
                Environment.Exit(1);
            }
            catch (SmtpProtocolException ex)    //obsługa wyjątku
            {
                Console.WriteLine("~~E-mail has not been sent (SMTP Protocol Exception): {0}", ex.Message);
                Environment.Exit(1);
            }
            Console.WriteLine("~~E-mail sent~~");
            smtpClient.Disconnect(true);
        }

        public static MimeMessage ReceiveEmail(string imapHost, Int32 imapPort, string from, SecureString password)   //funkcja odpowiadająca za odbieranie ostatniego e-maila (tego którego wysłaliśmy)
        {
            ImapClient imapClient = new ImapClient();   //inicjalizacja klienta imap z przestrzeni nazw MailKit.Net.ImapClient
            try
            {
                imapClient.Connect(imapHost, imapPort, SecureSocketOptions.SslOnConnect);   //łączymy się 
            }
            catch (ImapCommandException ex) //obsługa wyjątku
            {
                Console.WriteLine("~~Cannot connect with client (IMAP Command Exception): {0}~~", ex.ToString());
                Environment.Exit(1);
            }
            catch (ImapProtocolException ex)    //obsługa wyjątku
            {
                Console.WriteLine("~~Cannot connect with client (IMAP Protocol Exception): {0}~~", ex.ToString());
                Environment.Exit(1);
            }

            try
            {
                imapClient.Authenticate(from, new NetworkCredential(string.Empty, password).Password);   //autentykacja
            }
            catch (AuthenticationException ex)  //obsługa wyjątku
            {
                Console.WriteLine("~~Authentication error (Authentication Exception): {0}~~", ex.ToString());
                Environment.Exit(1);
            }
            catch (ImapCommandException ex) //obsługa wyjątku
            {
                Console.WriteLine("~~Authentication error (IMAP Command Exception): {0}~~", ex.ToString());
                Environment.Exit(1);
            }
            catch (ImapProtocolException ex)    //obsługa wyjątku
            {
                Console.WriteLine("~~Authentication error (IMAP Protocol Exception): {0}~~", ex.ToString());
                Environment.Exit(1);
            }

            imapClient.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            imapClient.Inbox.Open(FolderAccess.ReadOnly);   //otwieramy skrzynkę e-mail

            IList<UniqueId> uids = imapClient.Inbox.Search(SearchQuery.All);    //pobieramy wszystkie e-maile ze skrzynki (najstarsze na początku, najnowsze na końcu)
            MimeMessage mimeMessageReceiveEmail = imapClient.Inbox.GetMessage(uids[uids.Count - 1]);    //pobieramy najnowszy e-mail i umieszczamy go w obiekcie mimeMessageReceiveEmail
            imapClient.Disconnect(true);    //rozłączamy się
            if (mimeMessageReceiveEmail.From.ToString() == from)    //odebranie e-maila powiodło się
            {
                Console.WriteLine("~~E-mail received~~");
            }
            else
            {       
                Console.WriteLine("~~E-mail not received~~");   //odebranie e-maila nie powiodło się
                Environment.Exit(1);
            }

            return mimeMessageReceiveEmail; //zwracamy obiekt mimeMessage
        }

        public static void SaveEmlFile(MimeMessage mimeMessageEmail)
        {
            string emlDir = string.Format(Directory.GetCurrentDirectory() + "\\{0}.eml", mimeMessageEmail.MessageId);    //zmienna emlDir do przetrzymywania ścieżki nowo wygenerowanego pliku
            mimeMessageEmail.WriteTo(emlDir); // write the message to a file
            Console.WriteLine("E-mail file saved at: {0}", emlDir); //wyświetl ścieżkę gdzie zapisaliśmy plik EML
        }
    }
}
