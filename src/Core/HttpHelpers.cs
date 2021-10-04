using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Bnnsoft.Sdk.Signers
{
    public static class HttpHelpers
    {
        /// <summary>
        /// Makes a http request to the specified endpoint
        /// </summary>
        /// <param name="endpointUri"></param>
        /// <param name="httpMethod"></param>
        /// <param name="headers"></param>
        /// <param name="requestBody"></param>
        public static void InvokeHttpRequest(Uri endpointUri,
                                             string httpMethod,
                                             IDictionary<string, string> headers,
                                             string requestBody)
        {
            try
            {
                var request = ConstructWebRequest(endpointUri, httpMethod, headers);

                if (!string.IsNullOrEmpty(requestBody))
                {
                    var buffer = new byte[8192]; // arbitrary buffer size                        
                    var requestStream = request.GetRequestStream();
                    using (var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody)))
                    {
                        var bytesRead = 0;
                        while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            requestStream.Write(buffer, 0, bytesRead);
                        }
                    }
                }

                CheckResponse(request);
            }
            catch (WebException ex)
            {
                using (var response = ex.Response as HttpWebResponse)
                {
                    if (response != null)
                    {
                        var errorMsg = ReadResponseBody(response);
                        Console.WriteLine("\n-- HTTP call failed with exception '{0}', status code '{1}'", errorMsg, response.StatusCode);
                    }
                }
            }
        }

        /// <summary>
        /// Construct a HttpWebRequest onto the specified endpoint and populate
        /// the headers.
        /// </summary>
        /// <param name="endpointUri">The endpoint to call</param>
        /// <param name="httpMethod">GET, PUT etc</param>
        /// <param name="headers">The set of headers to apply to the request</param>
        /// <returns>Initialized HttpWebRequest instance</returns>
        public static HttpWebRequest ConstructWebRequest(Uri endpointUri,
                                                         string httpMethod,
                                                         IDictionary<string, string> headers)
        {
            var request = (HttpWebRequest)WebRequest.Create(endpointUri);
            request.Method = httpMethod;

            foreach (var header in headers.Keys)
            {
                // not all headers can be set via the dictionary
                if (header.Equals("host", StringComparison.OrdinalIgnoreCase))
                {
#if BCL35

#elif BCL45

#elif NETSTANDARD20
                    request.Host = headers[header];
#elif NETCOREAPP3_1
[assembly: AssemblyDescription("The Amazon Web Services SDK for .NET (.NET Core 3.1) - AWS Elemental MediaStore. AWS Elemental MediaStore is an AWS storage service optimized for media. It gives you the performance, consistency, and low latency required to deliver live and on-demand video content. AWS Elemental MediaStore acts as the origin store in your video workflow.")]
#else
#error Unknown platform constant - unable to set correct AssemblyDescription
#endif
                }
                else if (header.Equals("content-length", StringComparison.OrdinalIgnoreCase))
                    request.ContentLength = long.Parse(headers[header]);
                else if (header.Equals("content-type", StringComparison.OrdinalIgnoreCase))
                    request.ContentType = headers[header];
                else
                    request.Headers.Add(header, headers[header]);
            }

            return request;
        }

        public static void CheckResponse(HttpWebRequest request)
        {
            // Get the response and read any body into a string, then display.
            using (var response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Console.WriteLine("\n-- HTTP call succeeded");
                    var responseBody = ReadResponseBody(response);
                    if (!string.IsNullOrEmpty(responseBody))
                    {
                        Console.WriteLine("\n-- Response body:");
                        Console.WriteLine(responseBody);
                    }
                }
                else
                    Console.WriteLine("\n-- HTTP call failed, status code: {0}", response.StatusCode);
            }
        }

        /// <summary>
        /// Reads the response data from the service call, if any
        /// </summary>
        /// <param name="response">
        /// The response instance obtained from the previous request
        /// </param>
        /// <returns>The body content of the response</returns>
        public static string ReadResponseBody(HttpWebResponse response)
        {
            if (response == null)
                throw new ArgumentNullException("response", "Value cannot be null");

            // Then, open up a reader to the response and read the contents to a string
            // and return that to the caller.
            string responseBody = string.Empty;
            using (var responseStream = response.GetResponseStream())
            {
                if (responseStream != null)
                {
                    using (var reader = new StreamReader(responseStream))
                    {
                        responseBody = reader.ReadToEnd();
                    }
                }
            }
            return responseBody;
        }


        /// <summary>
        /// Helper routine to url encode canonicalized header names and values for safe
        /// inclusion in the presigned url.
        /// </summary>
        /// <param name="data">The string to encode</param>
        /// <param name="isPath">Whether the string is a URL path or not</param>
        /// <returns>The encoded string</returns>
#pragma warning disable CA1055 // Uri return values should not be strings
        public static string UrlEncode(string data, bool isPath = false)
#pragma warning restore CA1055 // Uri return values should not be strings
        {
            // The Set of accepted and valid Url characters per RFC3986. Characters outside of this set will be encoded.
            const string validUrlCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";

            var encoded = new StringBuilder(data.Length * 2);
            string unreservedChars = String.Concat(validUrlCharacters, (isPath ? "/:" : ""));

            foreach (char symbol in System.Text.Encoding.UTF8.GetBytes(data))
            {
                if (unreservedChars.IndexOf(symbol) != -1)
                    encoded.Append(symbol);
                else
                    encoded.Append("%").Append(String.Format("{0:X2}", (int)symbol));
            }

            return encoded.ToString();
        }
    }
}
