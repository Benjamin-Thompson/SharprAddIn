using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.EventReceivers;
using SharePointAddIn1Web.DataService;
using System.Security.Authentication;
using System.Net.Http;
using System.Net;
using System.IO;
using System.Web;

namespace SharePointAddIn1Web.Services
{
    public class SharprFileReceiver : IRemoteEventService
    {
        private static string _logAuth;
        private static string _logBaseUrl = "https://etechcons-testapi.azurewebsites.net/api/"; //todo : update this when Sharpr gets the new endpoints
        private static CredentialCache _credentialCache;
        private static string _sharprUser = "1rs2PCCgCvR8M1YVTVYZ";
        private static string _sharprPass = "05gkNHgXkB9KYDzQylSK1BTJ8mH455xj6t4xXbLn ";
        private static string _sharprURL = "https://sharprua.com/api/";

        private static NetworkCredential GetCredential()
        {
            if (_credentialCache != null)
                return _credentialCache.GetCredential(new Uri(_logBaseUrl), _logAuth);

            return null;
        }
        private static void AddCredential(NetworkCredential cred)
        {
            if (_credentialCache == null)
            {
                _credentialCache = new CredentialCache();
            }
            if (_credentialCache.GetCredential(new Uri(_logBaseUrl), _logAuth) == null)
            {
                _credentialCache.Add(new Uri(_logBaseUrl), _logAuth, cred);
            }

        }

            /// <summary>
            /// Handles events that occur before an action occurs, such as when a user adds or deletes a list item.
            /// </summary>
            /// <param name="properties">Holds information about the remote event.</param>
            /// <returns>Holds information returned from the remote event.</returns>
            public SPRemoteEventResult ProcessEvent(SPRemoteEventProperties properties)
        {
            SPRemoteEventResult result = new SPRemoteEventResult();

            using (ClientContext clientContext = TokenHelper.CreateRemoteEventReceiverClientContext(properties))
            {
                if (clientContext != null)
                {
                    clientContext.Load(clientContext.Web);
                    clientContext.ExecuteQuery();

                    if (properties.EventType == SPRemoteEventType.ItemAttachmentAdded)
                    {
                        //LogMessage("Executed ProcessEvent - ItemAttachmentAdded");
                        OnAddFiles(properties, clientContext);

                    }
                    else if (properties.EventType == SPRemoteEventType.ItemAttachmentDeleted)
                    {
                        //LogMessage("Executed ProcessEvent - ItemAttachmentDeleted");
                        OnRemoveFiles(properties, clientContext);
                    }

                    //LogMessage("Executed ProcessEvent");
                }
            }

            return result;
        }

        /// <summary>
        /// Handles events that occur after an action occurs, such as after a user adds an item to a list or deletes an item from a list.
        /// </summary>
        /// <param name="properties">Holds information about the remote event.</param>
        public void ProcessOneWayEvent(SPRemoteEventProperties properties)
        {
            using (ClientContext clientContext = TokenHelper.CreateRemoteEventReceiverClientContext(properties))
            {
                if (clientContext != null)
                {
                    clientContext.Load(clientContext.Web);
                    clientContext.ExecuteQuery();

                    if (properties.EventType == SPRemoteEventType.ItemAttachmentAdded)
                    {
                        //LogMessage("Executed One Way Event - ItemAttachementAdded");
                        OnAddFiles(properties, clientContext);

                    }
                    else if (properties.EventType == SPRemoteEventType.ItemAttachmentDeleted)
                    {
                        //LogMessage("Executed One Way Event - ItemAttachementDeleted");
                        OnRemoveFiles(properties, clientContext);
                    }

                    //test code 
                    //mocked up to call a test webapi in place of Sharpr's
                    //(to be replaced when Sharpr finishes publishing their new API)

                    //LogMessage("Executed One Way Event");

                }

            }
        }

        private static void LogMessage(string message)
        {

            var apiHttp = new HTTPService(_logAuth, _logBaseUrl);
            var cred = GetCredential();
            string content = "\"" + message + "\"";
            var t = apiHttp.HttpCallAsync<string>(cred, $"Test/", System.Net.Http.HttpMethod.Post, content, default);
        }

        private static void OnAddFiles(SPRemoteEventProperties properties, ClientContext clientContext)
        {
            List oList = clientContext.Web.Lists.GetById(properties.ItemEventProperties.ListId);
            clientContext.Load(oList);
            clientContext.ExecuteQuery();

            ListItem item = oList.GetItemById(properties.ItemEventProperties.ListItemId);

            foreach (Attachment f in item.AttachmentFiles)
            {
                Microsoft.SharePoint.Client.File sf = clientContext.Web.GetFileByServerRelativeUrl(f.ServerRelativeUrl);
                clientContext.Load(sf);
                clientContext.ExecuteQuery();

                string mimeType = MimeMapping.GetMimeMapping(sf.Name);

                FileInfo myFileinfo = new FileInfo(sf.Name);
                WebClient client1 = new WebClient();
                client1.Credentials = clientContext.Credentials;

                byte[] fileContents =
                      client1.DownloadData(sf.LinkingUrl);
                      

                MemoryStream mStream = new MemoryStream();

                mStream.Write(fileContents, 0, fileContents.Length);

                string[] tags = null;

                if (sf.Tag != null)
                {
                    //split comma delimited tags into an array of strings
                    tags = ((string)sf.Tag).Split(',');
                }
                string contentType = mimeType;
                //now that we have the contents, upload to Sharpr
                UploadFileToSharpr(sf.UniqueId.ToString(), sf.Name, tags, contentType, mStream);
            }
        }


        private static void OnRemoveFiles(SPRemoteEventProperties properties, ClientContext clientContext)
        {
            List oList = clientContext.Web.Lists.GetById(properties.ItemEventProperties.ListId);
            clientContext.Load(oList);
            clientContext.ExecuteQuery();

            ListItem item = oList.GetItemById(properties.ItemEventProperties.ListItemId);

            foreach (Attachment f in item.AttachmentFiles)
            {
                Microsoft.SharePoint.Client.File sf = clientContext.Web.GetFileByServerRelativeUrl(f.ServerRelativeUrl);

                RemoveFileFromSharpr(sf.UniqueId.ToString());
            }
        }

        private static HttpClient CreateSharprRequest()
        {
            var client = new HttpClient();

            string userpass = _sharprUser + ":" + _sharprPass;
            var userpassB = Encoding.UTF8.GetBytes(userpass);
            var userpassB64 = Convert.ToBase64String(userpassB);

            //var decrypted = Convert.FromBase64String("MXJzMlBDQ2dDdlI4TTFZVlRWWVo6MDVna05IZ1hrQjlLWUR6UXlsU0sxQlRKOG1INDU1eGo2dDR4WGJMbiA=");

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", userpassB64);
            client.DefaultRequestHeaders.Add("Accept-Encoding", "deflate");

            return client;
        }

        private static string UploadFileToSharpr(string fileGUID, string fileName, string[] tags, string contentType, MemoryStream fileContents)
        {
            string result = "PENDING";
            HttpClient client = CreateSharprRequest();

            if (fileContents.CanRead && fileContents.Length > 0)
            {
                string fileDataString = contentType + Convert.ToBase64String(fileContents.ToArray());
                StringBuilder sb = new StringBuilder();
                sb.Append("{");
                sb.Append("\"ref\":\"" + fileGUID + "\",");
                sb.Append("\"filename\":\"" + fileName + "\",");
                sb.Append("\"data\":\"data:" + contentType + ";base64, " + fileDataString + "\",");
                sb.Append("\"file_size\":\"" + fileDataString.Length.ToString() + "\"");
                //sb.Append("\"category\":\"" + fileGUID + "\",");
                //sb.Append("\"classification\":\"" + fileGUID + "\",");
                if (tags != null)
                {
                    sb.Append(", \"tags\":{");
                    foreach(string t in tags)
                    {
                        sb.Append("\"" + t + "\",");
                    }
                    sb.Remove(sb.Length - 1, 1); //remove the trailing ","
                    sb.Append( "}");
                }
                
                sb.Append("}");

                var content = new StringContent(sb.ToString(), Encoding.UTF8, "application/json");

                var tResponse = client.PutAsync(_sharprURL + "v2/files/sync", content);
                tResponse.Wait();

                var tRead = tResponse.Result.Content.ReadAsStringAsync();
                tRead.Wait();

                if (tRead.Result != null) result = tResponse.Result.StatusCode.ToString();
            }
            else
            {
                result = "FILE-EMPTY";
            }

            client.Dispose();

            return result;
        }


        private static string RemoveFileFromSharpr(string fileGUID)
        {
            string result = "PENDING";
            HttpClient client = CreateSharprRequest();

            ArraySegment<byte> buffer = new ArraySegment<byte>();

            if (fileGUID != null && fileGUID.Length > 0)
            {

                //# An API Response ID is also sent that references Sharpr's log ID
                //responseId = response.getHeader("API-Response-Id")
                StringBuilder sb = new StringBuilder();
                sb.Append("{");
                sb.Append("\"ref\":\"" + fileGUID + "\"");

                sb.Append("}");

                var content = new StringContent(sb.ToString(), Encoding.UTF8, "application/json");

                var tResponse = client.DeleteAsync(_sharprURL + "v2/files/sync/" + fileGUID);
                tResponse.Wait();

                var tRead = tResponse.Result.Content.ReadAsStringAsync();
                tRead.Wait();

                if (tRead.Result != null) result = tRead.Result;
            }
            else
            {
                result = "FILE-EMPTY";
            }

            client.Dispose();

            return result;
        }

    }
}
