using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CEA.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CEA.Controllers
{
    [Route("api/[controller]")]
    public class AnalyzerController : Controller
    {

        string[] encNames = new string[] { "big5", "gb2312", "unicode", "utf-8", "utf-16" };

        private string getHexString(string word, string encodingName)
        {
            byte[] b = System.Text.Encoding.GetEncoding(encodingName).GetBytes(word);
            return BitConverter.ToString(b).Replace("-", " ");
        }
        private string[] getByteHexStrings(string ch, string encodingName) 
        {
            byte[] b = System.Text.Encoding.GetEncoding(encodingName).GetBytes(ch);
            return BitConverter.ToString(b).Split('-');
        }

        private Dictionary<string, object> encode(string text)
        {
            var d = new Dictionary<string, object>();
            text = text ?? string.Empty;
            var first = true;
            var charOrig = new List<string>();
            Func<string, string> toPropName = (en)=> {
                en = en.Replace("-", string.Empty).Replace("unicode", "ucs2");
                return en[0].ToString().ToUpper() + en.Substring(1);
            };
            foreach (var encName in encNames)
            {
                var enc = Encoding.GetEncoding(encName);
                var fixEncName = encName.Replace("unicode", "ucs2").Replace("-", string.Empty).FirstCharToUpper();
                d.Add(
                    "code" + fixEncName,
                    getHexString(text, encName));
                d.Add("pvw" + fixEncName, enc.GetString(enc.GetBytes(text)));
                //add grouped as char results
                var charGroups = new List<string[]>();
                var i = 0;
                while (i < text.Length)
                {
                    //check if surrogate pairs
                    var c = text[i];
                    var t = c.ToString();
                    if (c >= 0xd800 && c <= 0xdbff && i < text.Length - 1 
                        && text[i + 1] >= 0xdc00 && text[i + 1] <= 0xdfff)
                    {
                        t = text.Substring(i, 2);
                        i++;
                    }
                    if (first) charOrig.Add(t);
                    charGroups.Add(getByteHexStrings(t, encName));
                    i++;
                }
                first = false;
                d.Add("chars" + toPropName(encName), charGroups.ToArray());
            }
            d.Add("charsOrig", charOrig);
            d.Add("ueBig5", EncodeHelper.UrlEncode(text, Encoding.GetEncoding(950)));
            d.Add("ueUnicode", HttpEncoder.UrlEncodeUnicode(text, true));
            d.Add("ueUtf8", EncodeHelper.UrlEncode(text, Encoding.UTF8));
            d.Add("codeNcr", EncodeHelper.ConvertToNCR(text));
            d.Add("text", text);
            return d;
        }

        [HttpPost("EncodeText")]
        [ApiErrorHandler]
        public Dictionary<string, object> EncodeText([FromForm] string text)
        {
            return encode(text);
        }

        [HttpPost("DecodeUrlEncode")]
        [ApiErrorHandler]
        public Dictionary<string, object> DecodeUrlEncode(string code, string encType)
        {
            string text = string.Empty;
            if (encType == "ncr")
            {
                text = EncodeHelper.ConvertFromNCR(code);
            }
            else
            {
                var enc = Encoding.GetEncoding(encType.Replace("utf8", "utf-8"));
                text = HttpEncoder.UrlDecode(code, enc);
            }
            return encode(text);
        }

        [HttpPost("DecodeMailSubject")]
        [ApiErrorHandler]
        public Dictionary<string, string> DecodeMailSubject(string code)
        {
            return new Dictionary<string, string>()
            {
                ["mailSubject"] = EncodeHelper.DecodeMailSubject(code)
            };
        }
    }

    //REF: https://stackoverflow.com/a/4405876
    public static class StringExtensions
    {
        public static string FirstCharToUpper(this string input)
        {
            switch (input)
            {
                case null: throw new ArgumentNullException(nameof(input));
                case "": throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input));
                default: return input.First().ToString().ToUpper() + input.Substring(1);
            }
        }
    }
}
