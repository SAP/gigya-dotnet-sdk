using System.Diagnostics;
using Gigya.Socialize.SDK;

namespace SDK_Tester
{
    public class AuthRequestTester
    {
        private const string ApiDomain = "il1.gigya.com";
        
        private const string ApiKey = "3_w8RBVwf9Sr_XtUbUrT3TRyhb59WOwEplCBGfjYdSAd0XduRZ_vdffS2mp7j_EsEz";
        private const string UserKey = "AMzUkXsKaA3Q";

        private const string PrivateKey = "-----BEGIN RSA PRIVATE KEY-----MIIEogIBAAKCAQEAo2FaiTqiq8Wdw3pUFiSoM8P6BIiI8hahOclT4mHdANZYWgiBK5YCQJMNo7BuhaxyabnZbLaHB1wbNHZP8UWmclbXdH/xAfveeOuVpul11HOVzrMxYl1LODTjbXF9jO36gWmpIJpTKlTDPNA38/pmyffsRHagfUGOQvnbvLzCFtYcVKzhH3/z8QSEo7JMJyzytFyrVKvDYtQMMZVgCw+9cI05gRJ+z6nI0/yzWzzEFnIRb7dI83QnibIoRsHidqQZUIDAK7cxHEGNkDTE6Eb6jditg40yU4BgvBrp7dTGdHtsW6EOA5JrQKbOJMsRcd9mPDkj0yRMwkBMj8grwH6PGQIDAQABAoIBAATa4kFctDPNigwiiPgle7gaFUZoNkWXZZAdivgZt2MMe5ClWw1MBmIb3JZmKkqfnsDEjJD99ZJC6u4KrAJ78t/H89wa5zMLZIeMXKSaoG1BSAzd51RIeHFBpRZ9/mCfO8f3t1ZoL5t87FZUy6zc3owW6Xb5XXiLZ6pW2XBI3a2m//OzJTcCTpNun75sj1chtmiCqpqJnwegPvn1LIeSC79FuNIwja+WI5rDHBaWBmzZsvDhx3oxzsSmBIQAZ2AehxWB4pIIz4Qmx3cYy5D82bDb2kErq8eNli1oLpdUqy/PkVt3i8XtGxpiQXUVaD6+rAFK8wbxKeUJdoR9bzCLcT8CgYEA49wllCOdcAdJSgZASnL+lGEyd+vHb5jSbOwnJ7rtozClEuZ9KOxyGEgmJDrZ2Q6ITKiSCsajy0b8zY3oyglWcRKkhLaIFjAeE8tz9HmfrDanbzBRCB9ZLSvYlZcvSHVomzutqaXs7qsFZ0Mt9eZcNBFKqE4z6tz8bQoy6t6XLDMCgYEAt46qRx00ZdhahiCHAQV58oGOnTNcqVS3VtCuw42CviBtsTrnYIREex/jl6Ui90WtuSJHC06f/dGwZ0Xns90/ChocG2T3KpHstg4RgN4RoEXE+qADDl9aLIggrahPD6V021+V5i1J3JxJjEzUVt/6+vaN9KpPxXFpADGG1gQQS4MCgYA7UkceSB9m2R7FfNckCsgojR18hw/HB/xQizKub0YK5FE1mHghPV1+4Nm9OO0aS2REwOY0k/50n6iVQ0rFvqSYj4fxXSwUyrYp5R/tF/Tv+tKgae3OtYqb7fxXBaMztA1lzKWrsxz6DeA8QAspJ639iDrtkl6F2L6HDM6wwv6MbQKBgDonRcUv+HjHua5CweLN9FujNiaRriqrf0ZO6P9lZuWLapU6vzEx1mxXpwhVNiW2+pnrxSxM5Z1JgKTHXef7EUzHBt6a9z+Sabcn792u/VCUqhpo9W7pQK1ZF1lNOHcRiVszBk+dS4hML3T2plM7tM0rrb+08X7xNj3scvZ85Ri3AoGASPNY8AL/matu6V5RT4ccjYKTySjnmz806OReR7IVhBD9xf+08DsAEOkhL9e9Tj6f7ENhwRWT+7EY+eSg5zFZv5ATIU+y7FNLJHockG7bgwM6H2oJptI1kPHmiMCpnjP9kKAb8C8h223dl7v1ChvWu6/oXSZ+gDuwN+8F4V7bksU=-----END RSA PRIVATE KEY-----";

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
                    uid = "654916f7a76f4980b78e575a6fc9f470", enabledProviders = "*,testnetwork,testnetwork2"
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