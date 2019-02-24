using System;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace CEA.Models
{
    public class EncodeHelper
    {
        //REF: https://referencesource.microsoft.com/#System.Web/httpserverutility.cs
        public static string UrlEncode(string str, Encoding e) {
            if (str == null)
                return null;
            byte[] bytes = e.GetBytes(str);
            return Encoding.ASCII.GetString(HttpEncoder.UrlEncode(bytes, 0, bytes.Length, false /* alwaysCreateNewReturnValue */));     
        }
        
        public static string UrlDecode(string str, Encoding e) {
            return HttpEncoder.UrlDecode(str, e);
        }

        public static string ConvertToNCR(string s)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in s)
            {
                if (c > 127)
                    sb.AppendFormat("&#{0};", Convert.ToInt32(c));
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }

        public static string ConvertFromNCR(string s)
        {
            return Regex.Replace(s, "&#(?<ncr>\\d+?);", m => 
                Convert.ToChar(int.Parse(m.Groups["ncr"].Value)).ToString()
            );
        }
        public static string DecodeMailSubject(string raw)
        {
            //若字串結尾是?=會識別不出來，故加上一個空白
            raw += " ";
            StringBuilder sb = new StringBuilder();
            //先解出一段一段的=?..... ?=
            foreach (Match m in Regex.Matches(raw, "=[?](?<enc>.+?)[?](?<type>.+?)[?](?<body>.+?)[?]=[^0-9A-Z]"))
            {
                string enc = m.Groups["enc"].Value.ToLower();
                string type = m.Groups["type"].Value.ToLower();
                string body = m.Groups["body"].Value;
                Encoding encoder = null;
                //識別出Encoding
                if (enc == "gbk" || enc == "x-gbk")
                    encoder = Encoding.GetEncoding(936);
                else if (enc == "big5")
                    encoder = Encoding.GetEncoding(950);
                else if (enc == "utf-8")
                    encoder = Encoding.UTF8;
                else
                {
                    throw new ApplicationException("不支援編碼格式[" + m.Groups["enc"].Value + "]!");
                }
                if (type == "q")
                {
                    body = body.Replace("=", "%");
                    body = UrlDecode(body, encoder);
                    raw = raw.Replace(m.Value, body);
                }
                else if (type == "b")
                {
                    byte[] buff = Convert.FromBase64String(body);
                    raw = raw.Replace(m.Value, encoder.GetString(buff));
                }
            }
            return raw;
        }
    }
}