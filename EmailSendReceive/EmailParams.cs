using System;
using System.Collections.Generic;
using System.Text;

namespace EmailSendReceive
{
    public class EmailParams
    {
        public string smtpHost { get; set; }
        public int smtpPort { get; set; }
        public string imapHost { get; set; }
        public int imapPort { get; set; }
        public string to { get; set; }
        public string subject { get; set; }
        public string textBody { get; set; }
    }
}
