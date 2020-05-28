using System;
using Xunit;

namespace EmailSendReceiveXUnitTestProject
{
    //1) arrange - prepare to the tests
    //2) act - testing application logic
    //3) assert - make sure that the application logic is what we expect
    public class Email
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

        [Fact]
        public void Validate()
        {
            Assert.True(IsValidEmail("tomaszbanach12@gmail.com"));
        }
    }
}
