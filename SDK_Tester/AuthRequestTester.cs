using System.Diagnostics;
using Gigya.Socialize.SDK;

namespace SDK_Tester
{
    public class AuthRequestTester
    {
        private const string ApiDomain = "";
        
        private const string ApiKey = "";
        private const string UserKey = "";

        private const string PrivateKey = "";

        public void Run()
        {
            const string method = "accounts.getAccountInfo";
            var req = new GSAuthRequest(
                UserKey,  
                PrivateKey, 
                ApiKey,
                method,
                new
                {
                    uid = "", enabledProviders = "*,testnetwork,testnetwork2"
                })
            {
                APIDomain = ApiDomain
            };

            var res = req.Send();

            var response = res.GetData<UserInfoResponse>();
            
            Debug.Assert(new GSArray(response.emails?.unverified).GetString(0) == "a@a.com");
        }

        public class UserInfoResponse
        {
            public Emails emails;
        }

        public class Emails
        {
            public string unverified;
        }
    }
}