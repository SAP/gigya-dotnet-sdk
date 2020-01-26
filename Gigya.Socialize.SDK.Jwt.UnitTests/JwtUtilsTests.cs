using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gigya.Socialize.SDK.Internals;
using NUnit.Framework;

// ReSharper disable StringLiteralTypo

namespace Gigya.Socialize.SDK.Jwt.UnitTests
{
    [TestFixture]
    public class JwtUtilsTests
    {
        [Test]
        public void ValidateSignature_IL1()
        {
            string jwt = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsImtpZCI6IlJEQXpSRVl5TmpCRk5USTVSak5ETURrd1JEUkJNMEZDUkRRM1FqQkNSRUpDUmpZNE9ESkZRUSIsImtleWlkIjoiUkRBelJFWXlOakJGTlRJNVJqTkRNRGt3UkRSQk0wRkNSRFEzUWpCQ1JFSkNSalk0T0RKRlFRIn0.eyJpc3MiOiJodHRwczovL2ZpZG0uZ2lneWEuY29tL2p3dC8zX0hrWHZ0R096ZDFRTWNWZkh5ZTdnY2FtWGJMQW9DNzdDNFRDQjdnazhteWNoLXhFbTVIVEwwYkNLQlROcDU2aGsvIiwiYXBpS2V5IjoiM19Ia1h2dEdPemQxUU1jVmZIeWU3Z2NhbVhiTEFvQzc3QzRUQ0I3Z2s4bXljaC14RW01SFRMMGJDS0JUTnA1NmhrIiwiaWF0IjoxNTgwMDM4NDM2LCJleHAiOjE1ODAwMzg3MzYsInN1YiI6ImU4OTJlZmMxM2MyNjQxY2ZiMTUzZWRlYmUyYjkwM2FhIiwiZGF0YS51c2VyS2V5IjoiQUhRbmZRZ21SWFY5In0.mWms1Nk9mntS4wn-aBCvCnC6hARNXUJj3nwVMtmk0i4tZwEdxbPgv7-4WOZ7G9cGZhQIa4cgPTG1z_PpJtxWA4Y2bjibRyTvMAs56lWcas2LPm5EFr9h_nJbjuzC8L0J4KNariCQpVeaSqlA1AZLddDYRp7GM_pBl6WxA2ux0H5lKI1a-TC_g2d1kzpiFGcVSrEP1_zavXQVIlw-_agfxrdMVNiTY_ld4gFrdlkY-u-rDLLgR3cCeELc1JKMus7OJBnBcr5coR0w_QkqmRQzVDCs_gAA1xngVHrqd96iV6pq7uCmeXSe-oZkBrHUAMGA8wwFg4Q5r--jFxxMgNFNBg";
            var claims = JwtUtils.ValidateSignature(jwt, "il1.gigya.com");
            
            Console.WriteLine($"Is valid: {claims != null}");
            Console.WriteLine();
            if (claims != null)
                foreach (var pair in claims)
                    Console.WriteLine($"{pair.Key} : {pair.Value}");
        }

        [Test]
        public void ValidateSignature()
        {
            // 2020-01-16T13:22:13.896Z
            string jwt = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsImtpZCI6IlJFUTBNVVE1TjBOQ1JUSkVNemszTTBVMVJrTkRRMFUwUTBNMVJFRkJSamhETWpkRU5VRkJRZyIsImtleWlkIjoiUkVRME1VUTVOME5DUlRKRU16azNNMFUxUmtORFEwVTBRME0xUkVGQlJqaERNamRFTlVGQlFnIn0.eyJpc3MiOiJodHRwczovL2ZpZG0uZ2lneWEuY29tL2p3dC8zX3NxRF9hRlYydzBxLTJKU0otUnZ2dEpfemlLN19kWW1NeHVBdjRjbzAtaFlBNDNXanY0TFNURERFVUpDbVEwTE8vIiwiYXBpS2V5IjoiM19zcURfYUZWMncwcS0ySlNKLVJ2dnRKX3ppSzdfZFltTXh1QXY0Y28wLWhZQTQzV2p2NExTVERERVVKQ21RMExPIiwiaWF0IjoxNTc5MTgwOTMzLCJleHAiOjE1NzkxODEyMzMsInN1YiI6Il9ndWlkX1hkVThtbEJMR0E2d0VnWHotRmNKWkhibDh2b1FJLVY4NHg0aFJRSDk3ZWs9IiwiZGF0YS51c2VyS2V5IjoiQUdvbmR0ejM1YkVKIn0.hdjM1WE2aV1vEeP02fNW_E_tYjFgkZqZ0xy7Ugvoyg_7MpVZMnoSbIwrDnE__IZ92OZsZGTPRnpwT8nunBknMWW2lTSezSNQDbfR5SHToa7HX6VAgNgUuLepO632ZK9TyN-1y5VG_ovg75Dxt64U-PHzmYhonqjMDeC8vNXiH28LasZQu_PJzhcE5SApkwOJjjybBn1iNs6a2hdvyJXttRk_JXxhkMrEiDxTNQp4wz5mhxE3ruqZkK7fx1tO22dL8ubXOc4s1_tUv7PdVCF_4KPMsXLm74gX1wI2_uoek6fxukOlHjcverdltZJIkYGbn9Qf9I71TGQcIB4DznoTdg";
            
            var claims = JwtUtils.ValidateSignature(jwt, "us1.gigya.com");
            
            Console.WriteLine($"Is valid: {claims != null}");

            Console.WriteLine();
            if (claims != null)
                foreach (var pair in claims)
                    Console.WriteLine($"{pair.Key} : {pair.Value}");

        }
    }
}
