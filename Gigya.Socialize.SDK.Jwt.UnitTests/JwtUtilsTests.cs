using System;
using Gigya.Socialize.SDK.Internals;
using NUnit.Framework;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable CommentTypo
// ReSharper disable StringLiteralTypo

namespace Gigya.Socialize.SDK.Jwt.UnitTests
{
    [TestFixture]
    public class JwtUtilsTests
    {
        /*
          _____   _        __        ____    _   _   _       __     __
         |_   _| | |      /_ |      / __ \  | \ | | | |      \ \   / /
           | |   | |       | |     | |  | | |  \| | | |       \ \_/ / 
           | |   | |       | |     | |  | | | . ` | | |        \   /  
          _| |_  | |____   | |     | |__| | | |\  | | |____     | |   
         |_____| |______|  |_|      \____/  |_| \_| |______|    |_|   
                                                          
        */
        string apiDomain = "il1.gigya.com";

        // IL1 Console Site API key
        string apiKey = "3_HkXvtGOzd1QMcVfHye7gcamXbLAoC77C4TCB7gk8mych-xEm5HTL0bCKBTNp56hk";
        
        // Console site (Partner ID: 2) - partner secret
        string secret = "exfRc6Z5xno4jeRL7h70cQy9i9UbZe/YkRm32vYEFec=";

        // A test console site user qwe@gmail.com having in data a userKey (targetUserKey)
        string targetUID = "21acbd86802c44089bddb16e0a205c36";

        // A userKey we expect - associated with console account referenced by targetUID
        private string targetUserKey = "AOfo7DCXzsYB";

        [Test]
        public void ValidateSignature()
        {
            // ARRANGE
            string jwt = GetJWT();
    
            Assert.IsTrue(jwt?.Length > 0, "Failed to obtain a jwt from getJWT()");

            // ACT
            var claims = JwtUtils.ValidateSignature(jwt, apiDomain);

            for (int i = 0; i < 200; i++)
                claims = JwtUtils.ValidateSignature(jwt, apiDomain); // should be really fast as public key cached.

            // ASSERT
            Assert.IsTrue(claims != null, "claims != null");
            Assert.IsTrue((string)claims["sub"] == targetUID, "'sub' != targetUID");
            Assert.IsTrue((string)claims["apiKey"] == apiKey, "'apiKey' == apiKey");
            Assert.IsTrue((string)claims["data.userKey"] == targetUserKey, "missing 'data.userKey'");

            foreach (var pair in claims)
                Console.WriteLine($"{pair.Key} : {pair.Value}");
        }

        /// <summary>
        /// Call to accounts.getJWT to generate an id_token - a JWT to validate after
        /// </summary>
        public string GetJWT()
        {
            var clientParams = new GSObject();

            clientParams.Put("fields", "data.userKey");
            clientParams.Put("targetUID", targetUID);

            var req = new GSRequest(
                apiKey: apiKey,
                secretKey: secret, 
                "accounts.getJWT", 
                clientParams)
            {
                APIDomain = apiDomain
            };

            var res = req.Send();

            Assert.IsTrue(res.GetErrorCode() == 0, "res.GetErrorCode() != 0");

            return res.GetData().GetString("id_token");
        }
    }
}
