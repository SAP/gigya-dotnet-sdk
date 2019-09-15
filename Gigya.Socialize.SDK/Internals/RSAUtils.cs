using System;
using System.IO;
using System.Security.Cryptography;

namespace Gigya.Socialize.SDK.Internals
{
    internal static class RsaUtils
    {
        private const string KeyHeader = "-----BEGIN RSA PRIVATE KEY-----";
        private const string KeyFooter = "-----END RSA PRIVATE KEY-----";
        
        public static RSACryptoServiceProvider DecodeRsaPrivateKey(string privateKey)
        {
            var key = Convert.FromBase64String(privateKey
                .Replace("\r\n","")
                .Replace(KeyHeader, "")
                .Replace(KeyFooter, ""));

            var mem = new MemoryStream(key);
            var bin = new BinaryReader(mem);
            var rsa = new RSACryptoServiceProvider();
            try
            {
                var twoBytes = bin.ReadUInt16();
                if (twoBytes == 0x8130)
                    bin.ReadByte();
                else if (twoBytes == 0x8230)
                    bin.ReadInt16();
                else
                    return null;

                twoBytes = bin.ReadUInt16();
                if (twoBytes != 0x0102)
                    return null;
                var bt = bin.ReadByte();
                if (bt != 0x00)
                    return null;

                var elems = GetIntegerSize(bin);
                var modulus = bin.ReadBytes(elems);

                elems = GetIntegerSize(bin);
                var e = bin.ReadBytes(elems);

                elems = GetIntegerSize(bin);
                var d = bin.ReadBytes(elems);

                elems = GetIntegerSize(bin);
                var p = bin.ReadBytes(elems);

                elems = GetIntegerSize(bin);
                var q = bin.ReadBytes(elems);

                elems = GetIntegerSize(bin);
                var dp = bin.ReadBytes(elems);

                elems = GetIntegerSize(bin);
                var dq = bin.ReadBytes(elems);

                elems = GetIntegerSize(bin);
                var iq = bin.ReadBytes(elems);

                var rsaParams = new RSAParameters
                {
                    Modulus = modulus,
                    Exponent = e,
                    D = d,
                    P = p,
                    Q = q,
                    DP = dp,
                    DQ = dq,
                    InverseQ = iq
                };
                
                rsa.ImportParameters(rsaParams);
                
                return rsa;
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                bin.Close();
            }
        }
        
        private static int GetIntegerSize(BinaryReader binaryReader)
        {
            int count;
            var bt = binaryReader.ReadByte();
            if (bt != 0x02)
                return 0;
            bt = binaryReader.ReadByte();

            if (bt == 0x81)
                count = binaryReader.ReadByte();
            else if (bt == 0x82)
            {
                var highByte = binaryReader.ReadByte();
                var lowByte = binaryReader.ReadByte();
                byte[] modInt = {lowByte, highByte, 0x00, 0x00};
                count = BitConverter.ToInt32(modInt, 0);
            }
            else
            {
                count = bt;
            }


            while (binaryReader.ReadByte() == 0x00)
            {
                count -= 1;
            }

            binaryReader.BaseStream.Seek(-1, SeekOrigin.Current);
            return count;
        }
    }
}