using MimeKit;
using Newtonsoft.Json.Linq;
using System;
using System.Security;
using EmailSendReceive;

namespace EmailSendReceive
{
    class Program
    {
        static void Main(string[] args) //funkcja główna
        {
            SendAndReceive.RealizeSendAndReceiveEmail();
        }
    }
}
