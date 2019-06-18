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


namespace SharePointAddIn1Web.Services
{
    public class SharprFileReceiver : IRemoteEventService
    {
        private string _auth;
        private string _baseUrl = "https://etechcons-testapi.azurewebsites.net/api/"; //todo : update this when Sharpr gets the new endpoints
        private CredentialCache _credentialCache;

        private NetworkCredential GetCredential()
        {
            if (_credentialCache != null)
                return _credentialCache.GetCredential(new Uri(_baseUrl), _auth);

            return null;
        }
        private void AddCredential(NetworkCredential cred)
        {
            if (_credentialCache == null)
            {
                _credentialCache = new CredentialCache();
            }
            if (_credentialCache.GetCredential(new Uri(_baseUrl), _auth) == null)
            {
                _credentialCache.Add(new Uri(_baseUrl), _auth, cred);
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

                    //temporary code to be replaced with Sharpr's new API endpoint

                    var apiHttp = new HTTPService(_auth, _baseUrl);
                    var cred = GetCredential();
                    string content = "\"ProcessEvent method fired\"";
                    var t = apiHttp.HttpCallAsync<string>(cred, $"Test/", System.Net.Http.HttpMethod.Post, content, default);

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


                    //test code 
                    //mocked up to call a test webapi in place of Sharpr's
                    //(to be replaced when Sharpr finishes publishing their new API)


                    var apiHttp = new HTTPService(_auth, _baseUrl);
                    var cred = GetCredential();
                    string content = "\"ProcessOneWayEvent method fired\"";
                    var t = apiHttp.HttpCallAsync<string>(cred, $"Test/", System.Net.Http.HttpMethod.Post, content, default);

                }

            }
        }
    }
}
