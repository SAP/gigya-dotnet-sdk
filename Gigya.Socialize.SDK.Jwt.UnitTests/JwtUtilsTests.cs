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
        string apiKey = "...";
        
        // Console site (Partner ID) - partner secret
        string secret = "...";

        // A test console site user 
        string targetUID = "...";

        [Test]
        public void ValidateSignature()
        {
            // ARRANGE
            string jwtIdToken = GetJWT();

            Console.WriteLine("id_token: " + jwtIdToken);
            Console.WriteLine();

            Assert.IsTrue(jwtIdToken?.Length > 0, "Failed to obtain a jwt from getJWT()");

            // ACT
            var claims = JwtUtils.ValidateSignature(jwtIdToken, apiDomain);

            for (int i = 0; i < 200; i++)
                claims = JwtUtils.ValidateSignature(jwtIdToken, apiDomain); // should be really fast as public key cached.

            // ASSERT
            Assert.IsTrue(claims != null, "claims != null");
            Assert.IsTrue((string)claims["sub"] == targetUID, "'sub' != targetUID");
            Assert.IsTrue((string)claims["apiKey"] == apiKey, "'apiKey' == apiKey");
            Assert.IsTrue((string)claims["email"] != null, "missing 'email'");

            foreach (var pair in claims)
                Console.WriteLine($"{pair.Key} : {pair.Value}");
        }

        /// <summary>
        /// Call to accounts.getJWT to generate an id_token - a JWT to validate after
        /// </summary>
        public string GetJWT()
        {
            var clientParams = new GSObject();

            clientParams.Put("fields", "profile.firstName,profile.lastName,email,data.userKey");
            clientParams.Put("targetUID", targetUID);

            var req = new GSRequest(
                apiKey,
                secret, 
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
