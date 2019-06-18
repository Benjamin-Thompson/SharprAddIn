using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace SharePointAddIn1Web.DataService
{
    interface IHTTPService
    {
        Task<HTTPResult<T>> HttpCallAsync<T>(NetworkCredential cred, string ApiUrl, HttpMethod method, string content = null, CancellationTokenSource tokenSource = default);
        Task HttpFileUploadAsync(NetworkCredential cred, string action, string filepath, CancellationTokenSource tokenSource = default);
        Task HttpFileDownloadAsync(NetworkCredential cred, string action, string filepath, CancellationTokenSource tokenSource = default, int buffersize = 8192);
    }
}
