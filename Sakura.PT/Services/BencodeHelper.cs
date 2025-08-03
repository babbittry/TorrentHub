using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

namespace Sakura.PT.Services
{
    public static class BencodeHelper
    {
        public static byte[] Encode(object data)
        {
            using (var stream = new MemoryStream())
            {
                EncodeToStream(stream, data);
                return stream.ToArray();
            }
        }

        private static void EncodeToStream(MemoryStream stream, object data)
        {
            if (data is string s)
            {
                EncodeString(stream, s);
            }
            else if (data is int i)
            {
                EncodeInteger(stream, i);
            }
            else if (data is long l)
            {
                EncodeInteger(stream, l);
            }
            else if (data is List<object> list)
            {
                EncodeList(stream, list);
            }
            else if (data is Dictionary<string, object> dict)
            {
                EncodeDictionary(stream, dict);
            }
            else if (data is byte[] b)
            {
                 // Peer lists in compact format are often represented as byte arrays.
                 // Bencode specification for strings is length-prefix followed by the string.
                 // The 'string' can be raw bytes.
                byte[] length = Encoding.ASCII.GetBytes(b.Length.ToString());
                stream.Write(length, 0, length.Length);
                stream.WriteByte((byte)':');
                stream.Write(b, 0, b.Length);
            }
            else
            {
                throw new ArgumentException($"Unsupported data type for Bencoding: {data?.GetType().Name ?? "null"}");
            }
        }

        private static void EncodeString(MemoryStream stream, string s)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            byte[] length = Encoding.ASCII.GetBytes(bytes.Length.ToString());
            stream.Write(length, 0, length.Length);
            stream.WriteByte((byte)':');
            stream.Write(bytes, 0, bytes.Length);
        }

        private static void EncodeInteger(MemoryStream stream, long l)
        {
            stream.WriteByte((byte)'i');
            byte[] bytes = Encoding.ASCII.GetBytes(l.ToString());
            stream.Write(bytes, 0, bytes.Length);
            stream.WriteByte((byte)'e');
        }

        private static void EncodeList(MemoryStream stream, List<object> list)
        {
            stream.WriteByte((byte)'l');
            foreach (var item in list)
            {
                EncodeToStream(stream, item);
            }
            stream.WriteByte((byte)'e');
        }

        private static void EncodeDictionary(MemoryStream stream, Dictionary<string, object> dict)
        {
            stream.WriteByte((byte)'d');
            // Keys must be sorted based on their raw byte representation, not lexicographically.
            var sortedKeys = dict.Keys.OrderBy(k => BitConverter.ToString(Encoding.UTF8.GetBytes(k)), StringComparer.Ordinal).ToList();
            foreach (var key in sortedKeys)
            { 
                EncodeString(stream, key);
                EncodeToStream(stream, dict[key]);
            }
            stream.WriteByte((byte)'e');
        }
    }
}
