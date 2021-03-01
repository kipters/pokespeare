using System;
using System.Security.Cryptography;
using System.Text;

namespace Pokespeare.Common
{
    internal static class HashExtensions
    {
        public static string ComputeBase64Hash(this HashAlgorithm self, string text)
        {
            var inData = Encoding.UTF8.GetBytes(text);
            var outData = self.ComputeHash(inData);
            return Convert.ToBase64String(outData);
        }
    }
}
