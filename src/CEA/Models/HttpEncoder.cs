namespace CEA.Models {
    //REF: https://referencesource.microsoft.com/#System.Web/Util/HttpEncoder.cs
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Web;
    using System;

    public class HttpEncoder {

        private static bool IsNonAsciiByte (byte b) {
            return (b >= 0x7F || b < 0x20);
        }
        internal static bool ValidateUrlEncodingParameters (byte[] bytes, int offset, int count) {
            if (bytes == null && count == 0)
                return false;
            if (bytes == null) {
                throw new ArgumentNullException ("bytes");
            }
            if (offset < 0 || offset > bytes.Length) {
                throw new ArgumentOutOfRangeException ("offset");
            }
            if (count < 0 || offset + count > bytes.Length) {
                throw new ArgumentOutOfRangeException ("count");
            }

            return true;
        }

        internal static byte[] UrlDecode (byte[] bytes, int offset, int count) {
            if (!ValidateUrlEncodingParameters (bytes, offset, count)) {
                return null;
            }

            int decodedBytesCount = 0;
            byte[] decodedBytes = new byte[count];

            for (int i = 0; i < count; i++) {
                int pos = offset + i;
                byte b = bytes[pos];

                if (b == '+') {
                    b = (byte)
                    ' ';
                } else if (b == '%' && i < count - 2) {
                    int h1 = HexToInt ((char) bytes[pos + 1]);
                    int h2 = HexToInt ((char) bytes[pos + 2]);

                    if (h1 >= 0 && h2 >= 0) { // valid 2 hex chars
                        b = (byte) ((h1 << 4) | h2);
                        i += 2;
                    }
                }

                decodedBytes[decodedBytesCount++] = b;
            }

            if (decodedBytesCount < decodedBytes.Length) {
                byte[] newDecodedBytes = new byte[decodedBytesCount];
                Array.Copy (decodedBytes, newDecodedBytes, decodedBytesCount);
                decodedBytes = newDecodedBytes;
            }

            return decodedBytes;
        }

        internal static string UrlDecode (byte[] bytes, int offset, int count, Encoding encoding) {
            if (!ValidateUrlEncodingParameters (bytes, offset, count)) {
                return null;
            }

            UrlDecoder helper = new UrlDecoder (count, encoding);

            // go through the bytes collapsing %XX and %uXXXX and appending
            // each byte as byte, with exception of %uXXXX constructs that
            // are appended as chars

            for (int i = 0; i < count; i++) {
                int pos = offset + i;
                byte b = bytes[pos];

                // The code assumes that + and % cannot be in multibyte sequence

                if (b == '+') {
                    b = (byte)
                    ' ';
                } else if (b == '%' && i < count - 2) {
                    if (bytes[pos + 1] == 'u' && i < count - 5) {
                        int h1 = HexToInt ((char) bytes[pos + 2]);
                        int h2 = HexToInt ((char) bytes[pos + 3]);
                        int h3 = HexToInt ((char) bytes[pos + 4]);
                        int h4 = HexToInt ((char) bytes[pos + 5]);

                        if (h1 >= 0 && h2 >= 0 && h3 >= 0 && h4 >= 0) { // valid 4 hex chars
                            char ch = (char) ((h1 << 12) | (h2 << 8) | (h3 << 4) | h4);
                            i += 5;

                            // don't add as byte
                            helper.AddChar (ch);
                            continue;
                        }
                    } else {
                        int h1 = HexToInt ((char) bytes[pos + 1]);
                        int h2 = HexToInt ((char) bytes[pos + 2]);

                        if (h1 >= 0 && h2 >= 0) { // valid 2 hex chars
                            b = (byte) ((h1 << 4) | h2);
                            i += 2;
                        }
                    }
                }

                helper.AddByte (b);
            }

            return Utf16StringValidator.ValidateString (helper.GetString ());
        }

        internal static string UrlDecode (string value, Encoding encoding) {
            if (value == null) {
                return null;
            }

            int count = value.Length;
            UrlDecoder helper = new UrlDecoder (count, encoding);

            // go through the string's chars collapsing %XX and %uXXXX and
            // appending each char as char, with exception of %XX constructs
            // that are appended as bytes

            for (int pos = 0; pos < count; pos++) {
                char ch = value[pos];

                if (ch == '+') {
                    ch = ' ';
                } else if (ch == '%' && pos < count - 2) {
                    if (value[pos + 1] == 'u' && pos < count - 5) {
                        int h1 = HexToInt (value[pos + 2]);
                        int h2 = HexToInt (value[pos + 3]);
                        int h3 = HexToInt (value[pos + 4]);
                        int h4 = HexToInt (value[pos + 5]);

                        if (h1 >= 0 && h2 >= 0 && h3 >= 0 && h4 >= 0) { // valid 4 hex chars
                            ch = (char) ((h1 << 12) | (h2 << 8) | (h3 << 4) | h4);
                            pos += 5;

                            // only add as char
                            helper.AddChar (ch);
                            continue;
                        }
                    } else {
                        int h1 = HexToInt (value[pos + 1]);
                        int h2 = HexToInt (value[pos + 2]);

                        if (h1 >= 0 && h2 >= 0) { // valid 2 hex chars
                            byte b = (byte) ((h1 << 4) | h2);
                            pos += 2;

                            // don't add as char
                            helper.AddByte (b);
                            continue;
                        }
                    }
                }

                if ((ch & 0xFF80) == 0)
                    helper.AddByte ((byte) ch); // 7 bit have to go as bytes because of Unicode
                else
                    helper.AddChar (ch);
            }

            return Utf16StringValidator.ValidateString (helper.GetString ());
        }

        internal static byte[] UrlEncode (byte[] bytes, int offset, int count, bool alwaysCreateNewReturnValue) {
            byte[] encoded = UrlEncode (bytes, offset, count);

            return (alwaysCreateNewReturnValue && (encoded != null) && (encoded == bytes)) ?
                (byte[]) encoded.Clone () :
                encoded;
        }

        // Set of safe chars, from RFC 1738.4 minus '+'
        public static bool IsUrlSafeChar (char ch) {
            if ((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9'))
                return true;

            switch (ch) {
                case '-':
                case '_':
                case '.':
                case '!':
                case '*':
                case '(':
                case ')':
                    return true;
            }

            return false;
        }
        public static int HexToInt (char h) {
            return (h >= '0' && h <= '9') ? h - '0' :
                (h >= 'a' && h <= 'f') ? h - 'a' + 10 :
                (h >= 'A' && h <= 'F') ? h - 'A' + 10 :
                -1;
        }
        public static char IntToHex (int n) {
            if (n <= 9)
                return (char) (n + (int)
                    '0');
            else
                return (char) (n - 10 + (int)
                    'a');
        }

        internal static byte[] UrlEncode (byte[] bytes, int offset, int count) {
            if (!ValidateUrlEncodingParameters (bytes, offset, count)) {
                return null;
            }

            int cSpaces = 0;
            int cUnsafe = 0;

            // count them first
            for (int i = 0; i < count; i++) {
                char ch = (char) bytes[offset + i];

                if (ch == ' ')
                    cSpaces++;
                else if (!IsUrlSafeChar (ch))
                    cUnsafe++;
            }

            // nothing to expand?
            if (cSpaces == 0 && cUnsafe == 0) {
                // DevDiv 912606: respect "offset" and "count"
                if (0 == offset && bytes.Length == count) {
                    return bytes;
                } else {
                    var subarray = new byte[count];
                    Buffer.BlockCopy (bytes, offset, subarray, 0, count);
                    return subarray;
                }
            }

            // expand not 'safe' characters into %XX, spaces to +s
            byte[] expandedBytes = new byte[count + cUnsafe * 2];
            int pos = 0;

            for (int i = 0; i < count; i++) {
                byte b = bytes[offset + i];
                char ch = (char) b;

                if (IsUrlSafeChar (ch)) {
                    expandedBytes[pos++] = b;
                } else if (ch == ' ') {
                    expandedBytes[pos++] = (byte)
                    '+';
                } else {
                    expandedBytes[pos++] = (byte)
                    '%';
                    expandedBytes[pos++] = (byte) IntToHex ((b >> 4) & 0xf);
                    expandedBytes[pos++] = (byte) IntToHex (b & 0x0f);
                }
            }

            return expandedBytes;
        }

        //  Helper to encode the non-ASCII url characters only
        internal static String UrlEncodeNonAscii (string str, Encoding e) {
            if (String.IsNullOrEmpty (str))
                return str;
            if (e == null)
                e = Encoding.UTF8;
            byte[] bytes = e.GetBytes (str);
            byte[] encodedBytes = UrlEncodeNonAscii (bytes, 0, bytes.Length, false /* alwaysCreateNewReturnValue */ );
            return Encoding.ASCII.GetString (encodedBytes);
        }

        internal static byte[] UrlEncodeNonAscii (byte[] bytes, int offset, int count, bool alwaysCreateNewReturnValue) {
            if (!ValidateUrlEncodingParameters (bytes, offset, count)) {
                return null;
            }

            int cNonAscii = 0;

            // count them first
            for (int i = 0; i < count; i++) {
                if (IsNonAsciiByte (bytes[offset + i]))
                    cNonAscii++;
            }

            // nothing to expand?
            if (!alwaysCreateNewReturnValue && cNonAscii == 0)
                return bytes;

            // expand not 'safe' characters into %XX, spaces to +s
            byte[] expandedBytes = new byte[count + cNonAscii * 2];
            int pos = 0;

            for (int i = 0; i < count; i++) {
                byte b = bytes[offset + i];

                if (IsNonAsciiByte (b)) {
                    expandedBytes[pos++] = (byte)
                    '%';
                    expandedBytes[pos++] = (byte) IntToHex ((b >> 4) & 0xf);
                    expandedBytes[pos++] = (byte) IntToHex (b & 0x0f);
                } else {
                    expandedBytes[pos++] = b;
                }
            }

            return expandedBytes;
        }

        internal static string UrlEncodeUnicode (string value, bool ignoreAscii) {
            if (value == null) {
                return null;
            }

            int l = value.Length;
            StringBuilder sb = new StringBuilder (l);

            for (int i = 0; i < l; i++) {
                char ch = value[i];

                if ((ch & 0xff80) == 0) { // 7 bit?
                    if (ignoreAscii || IsUrlSafeChar (ch)) {
                        sb.Append (ch);
                    } else if (ch == ' ') {
                        sb.Append ('+');
                    } else {
                        sb.Append ('%');
                        sb.Append (IntToHex ((ch >> 4) & 0xf));
                        sb.Append (IntToHex ((ch) & 0xf));
                    }
                } else { // arbitrary Unicode?
                    sb.Append ("%u");
                    sb.Append (IntToHex ((ch >> 12) & 0xf));
                    sb.Append (IntToHex ((ch >> 8) & 0xf));
                    sb.Append (IntToHex ((ch >> 4) & 0xf));
                    sb.Append (IntToHex ((ch) & 0xf));
                }
            }

            return sb.ToString ();
        }

        // Internal class to facilitate URL decoding -- keeps char buffer and byte buffer, allows appending of either chars or bytes
        private class UrlDecoder {
            private int _bufferSize;

            // Accumulate characters in a special array
            private int _numChars;
            private char[] _charBuffer;

            // Accumulate bytes for decoding into characters in a special array
            private int _numBytes;
            private byte[] _byteBuffer;

            // Encoding to convert chars to bytes
            private Encoding _encoding;

            private void FlushBytes () {
                if (_numBytes > 0) {
                    _numChars += _encoding.GetChars (_byteBuffer, 0, _numBytes, _charBuffer, _numChars);
                    _numBytes = 0;
                }
            }

            internal UrlDecoder (int bufferSize, Encoding encoding) {
                _bufferSize = bufferSize;
                _encoding = encoding;

                _charBuffer = new char[bufferSize];
                // byte buffer created on demand
            }

            internal void AddChar (char ch) {
                if (_numBytes > 0)
                    FlushBytes ();

                _charBuffer[_numChars++] = ch;
            }

            internal void AddByte (byte b) {
                // if there are no pending bytes treat 7 bit bytes as characters
                // this optimization is temp disable as it doesn't work for some encodings
                /*
                                if (_numBytes == 0 && ((b & 0x80) == 0)) {
                                    AddChar((char)b);
                                }
                                else
                */
                {
                    if (_byteBuffer == null)
                        _byteBuffer = new byte[_bufferSize];

                    _byteBuffer[_numBytes++] = b;
                }
            }

            internal String GetString () {
                if (_numBytes > 0)
                    FlushBytes ();

                if (_numChars > 0)
                    return new String (_charBuffer, 0, _numChars);
                else
                    return String.Empty;
            }
        }
        // This class contains utility methods for dealing with security contexts when crossing AppDomain boundaries.

        internal static class Utf16StringValidator {

            private const char UNICODE_NULL_CHAR = '\0';
            private const char UNICODE_REPLACEMENT_CHAR = '\uFFFD';

            public static string ValidateString (string input) {
                return ValidateString (input, false);
            }

            // only internal for unit testing
            internal static string ValidateString (string input, bool skipUtf16Validation) {
                if (skipUtf16Validation || String.IsNullOrEmpty (input)) {
                    return input;
                }

                // locate the first surrogate character
                int idxOfFirstSurrogate = -1;
                for (int i = 0; i < input.Length; i++) {
                    if (Char.IsSurrogate (input[i])) {
                        idxOfFirstSurrogate = i;
                        break;
                    }
                }

                // fast case: no surrogates = return input string
                if (idxOfFirstSurrogate < 0) {
                    return input;
                }

                // slow case: surrogates exist, so we need to validate them
                char[] chars = input.ToCharArray ();
                for (int i = idxOfFirstSurrogate; i < chars.Length; i++) {
                    char thisChar = chars[i];

                    // If this character is a low surrogate, then it was not preceded by
                    // a high surrogate, so we'll replace it.
                    if (Char.IsLowSurrogate (thisChar)) {
                        chars[i] = UNICODE_REPLACEMENT_CHAR;
                        continue;
                    }

                    if (Char.IsHighSurrogate (thisChar)) {
                        // If this character is a high surrogate and it is followed by a
                        // low surrogate, allow both to remain.
                        if (i + 1 < chars.Length && Char.IsLowSurrogate (chars[i + 1])) {
                            i++; // skip the low surrogate also
                            continue;
                        }

                        // If this character is a high surrogate and it is not followed
                        // by a low surrogate, replace it.
                        chars[i] = UNICODE_REPLACEMENT_CHAR;
                        continue;
                    }

                    // Otherwise, this is a non-surrogate character and just move to the
                    // next character.
                }
                return new String (chars);
            }
        }
    }
}