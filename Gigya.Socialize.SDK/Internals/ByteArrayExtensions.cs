using System;
using System.Text;

namespace Gigya.Socialize.SDK.Internals
{
    public static class ByteArrayExtensions
    {
        /// <summary>
        /// Convert the byte array into a string, using UTF8 encoding by default unless you pass your own encoder.
        /// </summary>
        public static string GetString(this byte[] buf, Encoding encoding = null)
        {
            return (encoding ?? Encoding.UTF8).GetString(buf);
        }

        /// <summary>
        /// Returns a byte array from a base64url (not base64 !) string.
        /// </summary>
        public static byte[] FromBase64UrlString(this string str)
        {
            // Taken from JWT spec
            var output = str;
            output = output.Replace('-', '+'); // 62nd char of encoding
            output = output.Replace('_', '/'); // 63rd char of encoding
            switch (output.Length % 4)
            {
                // Pad with trailing '='s
                case 0:
                    break; // No pad chars in this case
                case 2:
                    output += "==";
                    break; // Two pad chars
                case 3:
                    output += "=";
                    break; // One pad char
                default:
                    throw new FormatException("Illegal base64url string!");
            }
            return Convert.FromBase64String(output); // Standard base64 decoder
        }
    }
}