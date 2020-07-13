using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace EmailSendReceive
{
    class JsonParser
    {
        public static EmailParams DeserializeObject(string filePath)
        {
            StreamReader r = new StreamReader(filePath);
            string data = r.ReadToEnd();
            EmailParams jsonObjectEmail = JsonConvert.DeserializeObject<EmailParams>(data);  //do konwersji pliku JSON posłużyłem się biblioteką Newtonsoft.Json (NuGet Command: PM> Install-Package Newtonsoft.Json)
            return jsonObjectEmail;
        }
    }
}
