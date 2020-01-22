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
            string jwt = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsImtpZCI6IlJEQXpSRVl5TmpCRk5USTVSak5ETURrd1JEUkJNMEZDUkRRM1FqQkNSRUpDUmpZNE9ESkZRUSIsImtleWlkIjoiUkRBelJFWXlOakJGTlRJNVJqTkRNRGt3UkRSQk0wRkNSRFEzUWpCQ1JFSkNSalk0T0RKRlFRIn0.eyJpc3MiOiJodHRwczovL2ZpZG0uZ2lneWEuY29tL2p3dC8zX0hrWHZ0R096ZDFRTWNWZkh5ZTdnY2FtWGJMQW9DNzdDNFRDQjdnazhteWNoLXhFbTVIVEwwYkNLQlROcDU2aGsvIiwiYXBpS2V5IjoiM19Ia1h2dEdPemQxUU1jVmZIeWU3Z2NhbVhiTEFvQzc3QzRUQ0I3Z2s4bXljaC14RW01SFRMMGJDS0JUTnA1NmhrIiwiaWF0IjoxNTc5NTExMTg4LCJleHAiOjE1Nzk1MTE0ODgsInN1YiI6ImU4OTJlZmMxM2MyNjQxY2ZiMTUzZWRlYmUyYjkwM2FhIiwiZGF0YS51c2VyS2V5IjoiQUhRbmZRZ21SWFY5In0.fDDUhGSQHNNAx9FPQxls81uWIJ1g7bg7chcJ5-VQMlsLXLKiqRO1NmenmlKzqMstva02SdaCBZMlPkLikRhc93bg8RU2n-uBeI-bwojGAl4ULM4vwzrZ4CI-WZbcjNZgkR3HLUfxhIALDQJbl3qgOg3Ar8lpWoiHb6ucXelxdvUaVDmxyI6pwVFSdK2dzu2VxU-wVL1F7Y1f7xBZxRcjbqfSVl569ljsEaLubQVwbzV9r3p1tIKnZCyZ0qcGRkoxA2ZHRdbXf1E5VH-cpvkYMXiLuGcFXAwxQatLHepKTwK1OrRtZcIKuvRqSBCzhOUY2yPyGTaUxW0amksdudOUHA";
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
