using System.Security.Cryptography;
using System.Text;

namespace Kosuru.Config
{
    internal sealed class KosuruConfig
    {
        public string Token { get; set; }

        internal string EncodeToken()
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(Token));
        }

        internal string DecodeToken()
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(Token));
        }
    }
}
