using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Bnnsoft.Sdk.Signers
{
#pragma warning disable DE0004 // API is deprecated
    public class SignClient : WebClient
#pragma warning restore DE0004 // API is deprecated
    {
        private string host = "vin-hsm.com";

        public SignClient(string apiId, string secret, string service, string region)
        {
            this.endpointUri = string.Format("https://{0}-{1}.{2}", service, region, host);
            this.service = service;
            this.region = region;
            API_ID = apiId;
            Secret = secret;
        }

        private string prebuf;

        protected override void OnUploadStringCompleted(UploadStringCompletedEventArgs e)
        {
            base.OnUploadStringCompleted(e);
        }
             
        private readonly string endpointUri;
        private readonly string service;
        private readonly string region;

        public string API_ID { get; }
        public string Secret { get; }


        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);

            var uri = new Uri(endpointUri);

            // precompute hash of the body content
            var contentHash = AWS4SignerBase.CanonicalRequestHashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(this.prebuf));
            var contentHashString = AWS4SignerBase.ToHexString(contentHash, true);

            var headers = new Dictionary<string, string>
            {
                    {AWS4SignerBase.X_Amz_Content_SHA256, contentHashString},
                    {"content-length", this.prebuf.Length.ToString()},
                    {"content-type", "application/json"}
            };


            var signer = new AWS4SignerForChunkedUpload
            {
                EndpointUri = request.RequestUri,
                HttpMethod = request.Method,
                Service = service,
                Region = region
            };

            var authorization = signer.ComputeSignature(headers,
                "",   // no query parameters
                contentHashString,
                API_ID,
                Secret);

            try
            {
                request.Headers.Add("Authorization", authorization);
                request.Headers.Add(HttpRequestHeader.AcceptCharset, "utf-8");
                foreach (var header in headers.Keys)
                {
                    if (header.Equals("host", StringComparison.OrdinalIgnoreCase))
                    {

                    }
                    else if (header.Equals("content-length", StringComparison.OrdinalIgnoreCase))
                        request.ContentLength = long.Parse(headers[header]);
                    else if (header.Equals("content-type", StringComparison.OrdinalIgnoreCase))
                        request.ContentType = headers[header];
                    else
                        request.Headers.Add(header, headers[header]);
                }

            }
            finally
            {

            }

            return request;
        }

        private static char IntToHex(int n)
        {
            if (n <= 9)
                return (char)(n + (int)'0');
            else
                return (char)(n - 10 + (int)'a');
        }

        private static bool IsSafe(char ch)
        {
            if (ch >= 'a' && ch <= 'z' || ch >= 'A' && ch <= 'Z' || ch >= '0' && ch <= '9')
                return true;

            switch (ch)
            {
                case '-':
                case '_':
                case '.':
                case '!':
                case '*':
                case '\'':
                case '(':
                case ')':
                    return true;
            }

            return false;
        }

        private static byte[] UrlEncodeBytesToBytesInternal(byte[] bytes, int offset, int count, bool alwaysCreateReturnValue)
        {
            int cSpaces = 0;
            int cUnsafe = 0;

            // count them first
            for (int i = 0; i < count; i++)
            {
                char ch = (char)bytes[offset + i];

                if (ch == ' ')
                    cSpaces++;
                else if (!IsSafe(ch))
                    cUnsafe++;
            }

            // nothing to expand?
            if (!alwaysCreateReturnValue && cSpaces == 0 && cUnsafe == 0)
                return bytes;

            // expand not 'safe' characters into %XX, spaces to +s
            byte[] expandedBytes = new byte[count + cUnsafe * 2];
            int pos = 0;

            for (int i = 0; i < count; i++)
            {
                byte b = bytes[offset + i];
                char ch = (char)b;

                if (IsSafe(ch))
                {
                    expandedBytes[pos++] = b;
                }
                else if (ch == ' ')
                {
                    expandedBytes[pos++] = (byte)'+';
                }
                else
                {
                    expandedBytes[pos++] = (byte)'%';
                    expandedBytes[pos++] = (byte)IntToHex((b >> 4) & 0xf);
                    expandedBytes[pos++] = (byte)IntToHex(b & 0x0f);
                }
            }

            return expandedBytes;
        }

        private static byte[] UrlEncodeToBytes(string str, Encoding e)
        {
            if (str == null)
                return null;
            byte[] bytes = e.GetBytes(str);
            return UrlEncodeBytesToBytesInternal(bytes, 0, bytes.Length, false);
        }
        private static string UrlEncode(string str)
        {
            if (str == null)
                return null;
            return UrlEncode(str, Encoding.UTF8);
        }

        private static string UrlEncode(string str, Encoding e)
        {
            if (str == null)
                return null;
            return Encoding.ASCII.GetString(UrlEncodeToBytes(str, e));
        }
        public new byte[] DownloadData(string address)
        {
            this.prebuf = null;
            return base.DownloadData(endpointUri + address);
        }
        public static string DecodeFromUtf8(string utf8String)
        {
            // copy the string as UTF-8 bytes.
            byte[] utf8Bytes = new byte[utf8String.Length];
            for (int i = 0; i < utf8String.Length; ++i)
            {
                //Debug.Assert( 0 <= utf8String[i] && utf8String[i] <= 255, "the char must be in byte's range");
                utf8Bytes[i] = (byte)utf8String[i];
            }

            return Encoding.UTF8.GetString(utf8Bytes, 0, utf8Bytes.Length);
        }
        public new string UploadString(string address, string method, String data)
        {
            this.prebuf = data;            
            var sstring =  base.UploadString(endpointUri + address, method, this.prebuf);
            return DecodeFromUtf8(sstring);
        }

    }
}

