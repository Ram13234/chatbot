using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;


namespace Microsoft.Bot.Connector
{
    [Obsolete("The StateAPI is being deprecated.  Please refer to https://aka.ms/yr235k for details on how to replace with your own storage.", false)]
    public partial class StateClient
    {
        /// <summary>
        /// Create a new instance of the StateClient class
        /// </summary>
        /// <param name="baseUri">Base URI for the State service</param>
        /// <param name="microsoftAppId">Optional. Your Microsoft app id. If null, this setting is read from settings["MicrosoftAppId"]</param>
        /// <param name="microsoftAppPassword">Optional. Your Microsoft app password. If null, this setting is read from settings["MicrosoftAppPassword"]</param>
        /// <param name="handlers">Optional. The delegating handlers to add to the http client pipeline.</param>
        [Obsolete("The StateAPI is being deprecated.  Please refer to https://aka.ms/yr235k for details on how to replace with your own storage.", false)]
        public StateClient(Uri baseUri, string microsoftAppId = null, string microsoftAppPassword = null, params DelegatingHandler[] handlers)
            : this(baseUri, new MicrosoftAppCredentials(microsoftAppId, microsoftAppPassword), handlers: handlers)
        {
        }

        /// <summary>
        /// Create a new instance of the StateClient class
        /// </summary>
        /// <param name="baseUri">Base URI for the State service</param>
        /// <param name="credentials">Credentials for the Connector service</param>
        /// <param name="addJwtTokenRefresher">(DEPRECATED)</param>
        /// <param name="handlers">Optional. The delegating handlers to add to the http client pipeline.</param>
        [Obsolete("The StateAPI is being deprecated.  Please refer to https://aka.ms/yr235k for details on how to replace with your own storage.", false)]
        public StateClient(Uri baseUri, MicrosoftAppCredentials credentials, bool addJwtTokenRefresher = true, params DelegatingHandler[] handlers)
            : this(baseUri, handlers)
        {
            this.Credentials = credentials;
        }


        /// <summary>
        /// Create a new instance of the StateClient class
        /// </summary>
        /// <remarks> This constructor will use https://state.botframework.com as the baseUri</remarks>
        /// <param name="credentials">Credentials for the Connector service</param>
        /// <param name="addJwtTokenRefresher">(DEPRECATED)</param>
        /// <param name="handlers">Optional. The delegating handlers to add to the http client pipeline.</param>
        [Obsolete("The StateAPI is being deprecated.  Please refer to https://aka.ms/yr235k for details on how to replace with your own storage.", false)]
        public StateClient(MicrosoftAppCredentials credentials, bool addJwtTokenRefresher = true, params DelegatingHandler[] handlers)
            : this(handlers)
        {
            this.Credentials = credentials;
        }
        
        partial void CustomInitialize()
        {
            ConnectorClient.AddUserAgent(this);
        }
        
        internal static string GetClientVersion<T>(T client) where T : ServiceClient<T>
        {
            var type = client.GetType();
            var assembly = type.GetTypeInfo().Assembly;
            return assembly.GetName().Version.ToString();
        }

    }
}
