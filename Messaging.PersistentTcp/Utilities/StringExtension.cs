using System.Text;

namespace Messaging.PersistentTcp.Utilities
{
    public static class StringExtension
    {
        public static string ToStringUTF8(this byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        public static byte[] ToBytesUTF8(this string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }
    }
}
