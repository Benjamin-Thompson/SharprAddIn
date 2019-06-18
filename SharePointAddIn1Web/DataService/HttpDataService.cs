using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http.Formatting;

namespace SharePointAddIn1Web.DataService
{
    public class HTTPCode
    {
        public int statusCode { get; set; }
        public string reason { get; set; }
    }
    public class HTTPResult<T>
    {
        private HTTPCode status;
        private Task<T> task;

        public T Payload { get; set; }
        public HTTPCode Result { get; set; }
        public HTTPResult(HTTPCode code, T payload)
        {
            Payload = payload;
            Result = code;
        }

        public HTTPResult(HTTPCode status, Task<T> task)
        {
            this.status = status;
            this.task = task;
        }
    }
    public class HTTPService : IHTTPService
    {

        private Uri _baseUri;
        private string _authType;


        public HTTPService()
        { }
        public HTTPService(string AuthType, string url)
        {
            _baseUri = new Uri(url); _authType = AuthType;
        }
        public async Task<HTTPResult<T>> HttpCallAsync<T>(NetworkCredential cred, string action, HttpMethod method, string content = null, CancellationTokenSource tokenSource = default)
        {
            HttpClientHandler authtHandler = new HttpClientHandler { Credentials = cred ?? CredentialCache.DefaultCredentials };
            try
            {
                using (HttpClient client = new HttpClient(authtHandler))
                {
                    client.Timeout = TimeSpan.FromMinutes(10);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var requestMessage = new HttpRequestMessage
                    {
                        RequestUri = new Uri(_baseUri, action),
                        Method = method
                    };
                    if ((method == HttpMethod.Post || method == HttpMethod.Put) && !string.IsNullOrWhiteSpace(content))
                        requestMessage.Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json");
                    CancellationToken token = tokenSource == null ? default : tokenSource.Token;
                    using (HttpResponseMessage message = await client.SendAsync(requestMessage, token))
                    {
                        var status = new HTTPCode { statusCode = (int)message.StatusCode, reason = message.ReasonPhrase };
                        if (message.IsSuccessStatusCode)
                        {
                            using (var mc = message.Content)
                            {
                                return new HTTPResult<T>(status, await mc.ReadAsAsync<T>(token));

                                //  return Tuple.Create(await mc.ReadAsAsync<T>(token), status);
                            }
                        }
                        return new HTTPResult<T>(status, default(T));  // Tuple.Create(default(T), status);
                    }
                }
            }
            catch (Exception ex)
            {
                return new HTTPResult<T>(new HTTPCode { statusCode = 400, reason = "Exception" }, default(T));  //Tuple.Create(default(T), new HTTPCode { statusCode = 400, reason = "Exception" });
            }
        }
        //this API should post the file and at some point return 
        public async Task HttpFileUploadAsync(NetworkCredential cred, string action, string filepath, CancellationTokenSource tokenSource = default)
        {
            HttpClientHandler authtHandler = new HttpClientHandler { Credentials = cred ?? CredentialCache.DefaultCredentials };
            try
            {
                using (var stream = File.OpenRead(filepath))
                {
                    using (HttpClient client = new HttpClient(authtHandler) { Timeout = Timeout.InfiniteTimeSpan })  //set timespan so it does not break
                    {
                        CancellationToken token = tokenSource == null ? default : tokenSource.Token;
                        using (HttpResponseMessage response = await client.PostAsync(new Uri(_baseUri, action), new StreamContent(stream), token))
                        {
                            response.EnsureSuccessStatusCode(); // generic way of handling success
                        }
                    }
                }
            }
            //need to replace generic exception with several 1. cannot open file; 2.different timeouts in the post; 3. authentication...
            catch (Exception ex)
            {

            }

        }
        //this is for downloading file
        public async Task HttpFileDownloadAsync(NetworkCredential cred, string action, string filepath, CancellationTokenSource tokenSource = default, int buffersize = 8192)
        {
            HttpClientHandler authtHandler = new HttpClientHandler { Credentials = cred ?? CredentialCache.DefaultCredentials };
            try
            {
                using (HttpClient client = new HttpClient(authtHandler) { Timeout = Timeout.InfiniteTimeSpan })  //set timespan so it does not break
                {
                    CancellationToken token = tokenSource == null ? default : tokenSource.Token;
                    using (HttpResponseMessage response = await client.GetAsync(new Uri(_baseUri, action), HttpCompletionOption.ResponseHeadersRead, token))
                    {
                        using (var remoteStream = await response.Content.ReadAsStreamAsync())
                        using (var content = File.Open(filepath, FileMode.Create))  //handle 
                        {
                            //
                            // remoteStream.CopyTo(content);
                            //
                            var buffer = new byte[buffersize];
                            int read;
                            while ((read = remoteStream.Read(buffer, 0, buffer.Length)) != 0) //need to be async
                            {
                                content.Write(buffer, 0, read); //needs to be async

                                content.Flush(); //needs to be async
                            }
                        }
                        //return outputFileName;
                    }
                }


            }
            //need to replace generic exception with several 1. cannot open file; 2.different timeouts in the post; 3. authentication...
            catch (Exception ex)
            {

            }

        }
        //depricated
        [System.Runtime.InteropServices.DllImport("kernel32", SetLastError = true)]
        private static extern bool FlushFileBuffers(IntPtr handle);

    }
}
