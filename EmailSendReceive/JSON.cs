using Newtonsoft.Json.Linq;
using System.IO;

namespace EmailSendReceive
{
    class JSON
    {
        public static JObject JSONParser(string filePath)
        {
            StreamReader r = new StreamReader(filePath);
            string data = r.ReadToEnd();
            JObject jsonObjectEmail = JObject.Parse(data);  //do konwersji pliku JSON posłużyłem się biblioteką Newtonsoft.Json (NuGet Command: PM> Install-Package Newtonsoft.Json)
            return jsonObjectEmail;
        }
    }
}
