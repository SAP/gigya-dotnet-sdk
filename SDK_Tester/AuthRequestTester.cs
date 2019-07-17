using System.Diagnostics;
using Gigya.Socialize.SDK;

namespace SDK_Tester
{
    public class AuthRequestTester
    {
        private const string ApiDomain = "il1-st7.gigya.com";
        
        private const string ApiKey = "3_iC1Ys3bom_XB1bKlO65ShCrOWFfHVC4FeI5uS9ra2WM_BXb6Uu-pOge2TtR6y_-1";
        private const string UserKey = "AMzUkXsKaA3Q";

        private const string PrivateKey = @"-----BEGIN RSA PRIVATE KEY-----
MIIEowIBAAKCAQEAuYQMUHbzBHcgU9tDDwZElGvMrkyt6Rw56rdGL3yUHfvwUVn4
L4HEoazaEJ9Wk9H0LVLmU1hoWagAI9tXACFFCk3+ycJfGX4ULDMz/5agXhrzZf0B
dam8ZutJ1fxA3tRwd/xO8rypzeQU+wYNlvEJzAZT9JCASruG3Vq9n2hGMys+lS3f
xtGmk0JBhE0RCz/TpJzMmx5WeB+3hhT/uukh23MGEHTtCK1DafaSxOSjtL0oeDlL
hlB+HihEVaNmuXTWuQPc6STcuNIgzsrt0CEGVgHk1d+DH8p2CPwIf8FbpLZF3iiw
4eZRWzrcQYfagwUbi49rprzQZp4//UAnR9jBPQIDAQABAoIBACyt2ohjiXZFGX5Q
s5cR/6DOUJqW4ZifoWw/zRHBO2v4QiHZnP4WDw0QE+nGckPCIEBtM4cVpvYW0cfQ
+uRPXvETJT6ixyQc2w9lGowfEwrvCzlAJKKZqUQRPTRh3x67g8XF+J6Z5PxvBsWJ
KOs2LJGcYYpoZdl5zgqQINScOLH4OnzYTr4/pbuzoVKkWM+bZDFJoNtBBnG6+eHG
k47sNkF4cQzW97MrxMwO4VDdhq/Kr5tK3ndJCrlTxrOGwPA2Nf3vqv43olIplaJf
NZrCl1JKyIBmrvcWM4wxW+hGCrRDyOB2iFpg/XvgoimtBTYgPNoc7Nri7mJL+sc3
Vke63vECgYEA4hETT+C/o/COysG7bv9bP8W6ABIN99kZNW4GcOddnz3GGz3EXS2q
wgQyz8Ijk6ihuxVSU9yKb9Q0mT1fxTJW/4D5b5JxdN2nwsIbGhuK3sV2n6ThLbj/
T6bBUlSAxogqimDCk/ERkNKnltD4JgKznlM2ENjc1pSbL9kVhxbC4rECgYEA0hRu
ABs7CHTmcte9hNYebTvL6lYIOGV/Rk1DwW0ezE0Hw8p8jexVGSfHtH+qOJxGD6bc
cfD9+beIaxfJVJ/9iGQiVdro3yfhPgeMQStcWAmijWBE/Z9xzutU9Os/fwMMpgQA
65lqhcPsbG4CBiNmNq5eIbxj138uwqzV+UM0Mk0CgYEAjal9nJSOAsF/+Xalac0C
9VeGUvz9W87jiSPFTYLunBctyWxPXMR9OM9AuAhEGweVMZMO4BZXefRUcaKQHRaK
hdngdRYjmsQ7mEPij92qjCbZSvkbUneXJeatRlZFzCMP5V71D5gFFeertUqF9evD
evdR7gS3fo/pH3a9ksWkokECgYBmfWsRCDfrr0SCgLhIJ0Ie3o5kW+aUxQer36QP
qNHesDH6lj3f6420wRCQAbyk87DGkAx6Vi1B+AVI4gjqDUfek6OgqTT1MfqUjZAi
dyoNFV5FhNMDvRcD8RG4j1CiAXXZRJjCWE18xxH/8EdygTCrurPX15YKG1VPyox1
mBDN/QKBgAMUsxYhe4wnG4JjkMeb7kJNnr0jVdMgga3nwlw8QUb9C+oRpQIBZb9v
+7TvXp92RiqPPP7fX5SoAGhgUHhLsiKFBdbrPghUmHbATm7EGFMtVK2QC4TlfVS0
4o/LvlUlojOf6uFMViscfDS5YzcdjOx0xmOitIT1mo6haFDs4xx7
-----END RSA PRIVATE KEY-----
";

        public void Run()
        {
            const string method = "accounts.getAccountInfo";
            var req = new GSAuthRequest(
                ApiKey, 
                UserKey,  
                PrivateKey, 
                method,
                new
                {
                    uid = "100c3d9b00c54c519ebd9afebd31c6f4", enabledProviders = "*,testnetwork,testnetwork2"
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