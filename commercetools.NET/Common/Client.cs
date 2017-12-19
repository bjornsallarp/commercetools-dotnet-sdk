using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace commercetools.Common
{
    /// <summary>
    /// A client for executing requests against the commercetools web service.
    /// </summary>
    public class Client
    {
        #region Properties

        /// <summary>
        /// Configuration
        /// </summary>
        public Configuration Configuration { get; private set; }

        /// <summary>
        /// Token
        /// </summary>
        public Token Token { get; private set; }

        /// <summary>
        /// Factory responsible for creating response models
        /// </summary>
        public ResponseModelFactory ResponseModelFactory { get; private set; }

        /// <summary>
        /// HttpClient used for all requests
        /// </summary>
        private HttpClient HttpClient { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        public Client(Configuration configuration, HttpClient httpClient)
        {
            this.Configuration = configuration;
            this.ResponseModelFactory = new ResponseModelFactory();

            httpClient.DefaultRequestHeaders.UserAgent.Clear();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(GetUserAgent());
            this.HttpClient = httpClient;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public Client(Configuration configuration)
            : this(configuration, new HttpClient())
        {}
        
        #endregion

        #region Web Service Methods

        /// <summary>
        /// Executes a GET request.
        /// </summary>
        /// <param name="endpoint">API endpoint, excluding the project key</param>
        /// <param name="values">Values</param>
        /// <returns>JSON object</returns>
        public Task<Response<T>> GetAsync<T>(string endpoint, NameValueCollection values = null)
        {
            HttpRequestMessage httpRequestMessage = CreateHttpRequestMessage(HttpMethod.Get, endpoint, values);
            return SendAsync<T>(httpRequestMessage);
        }

        /// <summary>
        /// Executes a POST request.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="payload">Body of the request</param>
        /// <returns>JSON object</returns>
        public Task<Response<T>> PostAsync<T>(string endpoint, string payload)
        {
            HttpContent content = new StringContent(payload, Encoding.UTF8, "application/json");
            HttpRequestMessage httpRequestMessage = CreateHttpRequestMessage(HttpMethod.Delete, endpoint, null, content);
            return SendAsync<T>(httpRequestMessage);
        }

        /// <summary>
        /// Executes a DELETE request.
        /// </summary>
        /// <param name="endpoint">API endpoint, excluding the project key</param>
        /// <param name="values">Values</param>
        /// <returns>JSON object</returns>
        public Task<Response<T>> DeleteAsync<T>(string endpoint, NameValueCollection values = null)
        {
            HttpRequestMessage httpRequestMessage = CreateHttpRequestMessage(HttpMethod.Delete, endpoint, values);
            return SendAsync<T>(httpRequestMessage);
        }

        /// <summary>
        /// Makes sure the token is correct before sending the request
        /// </summary>
        /// <typeparam name="T">Response model type</typeparam>
        /// <param name="httpRequestMessage">Request to send</param>
        /// <param name="addAuthToken">Makes sure a valid auth token is added to the request if true</param>
        /// <returns>Response with deserialized model of type T</returns>
        private async Task<Response<T>> SendAsync<T>(HttpRequestMessage httpRequestMessage, bool addAuthToken = true)
        {
            Response<T> response = new Response<T>();

            if (addAuthToken)
            {
                if (!IsTokenValid(this.Token))
                {
                    await EnsureToken();
                }

                if (this.Token == null)
                {
                    response.Success = false;
                    response.Errors.Add(new ErrorMessage("no_token", "Could not retrieve token"));
                    return response;
                }
                httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue(this.Token.TokenType, this.Token.AccessToken);
            }
            
            HttpResponseMessage httpResponseMessage = await HttpClient.SendAsync(httpRequestMessage);
            response = await GetResponse<T>(httpResponseMessage);
            return response;
        }

        /// <summary>
        /// Creates an API-request. Does not add auth-header 
        /// </summary>
        /// <param name="method">Method to send. GET/POST/DELETE etc</param>
        /// <param name="endpoint">API endpoint, excluding the project key</param>
        /// <param name="queryParameters">Querystring parameters to add to the request url</param>
        /// <param name="content">Content to send with the request</param>
        /// <returns>HttpRequestMessage</returns>
        private HttpRequestMessage CreateHttpRequestMessage(HttpMethod method, string endpoint, NameValueCollection queryParameters, HttpContent content = null)
        {
            if (!string.IsNullOrWhiteSpace(endpoint) && !endpoint.StartsWith("/"))
            {
                endpoint = string.Concat("/", endpoint);
            }

            string url = string.Concat(this.Configuration.ApiUrl, "/", this.Configuration.ProjectKey, endpoint,
                queryParameters.ToQueryString());

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(method, new Uri(url))
            {
                Version = HttpVersion.Version10,
                Content = content
            };
            httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return httpRequestMessage;
        }

        #endregion

        #region Token Methods

        /// <summary>
        /// Client side validation of a given token
        /// </summary>
        /// <param name="token">Token to validate</param>
        /// <returns>Returns false if it's obvious the token needs to be renewed</returns>
        private static bool IsTokenValid(Token token)
        {
            return token == null || token.IsExpired() ? false : true;
        }

        /// <summary>
        /// Ensures that the token for this instance has been retrieved and that it has not expired.
        /// </summary>
        private async Task EnsureToken()
        {
            if (IsTokenValid(this.Token))
            {
                return;
            }

            this.Token = null;
            Response<Token> tokenResponse = await GetTokenAsync();

            if (tokenResponse.Success)
            {
                this.Token = tokenResponse.Result;
            }
            
            /*
             * The refresh token flow is currently only available for the password flow, which is currently not supported by the SDK.
             * More info: https://dev.commercetools.com/http-api-authorization.html#password-flow
             * 
            else if (this.Token.IsExpired())
            {
                this.Token = RefreshTokenAsync(this.Token.RefreshToken);
            }
             */
        }

        /// <summary>
        /// Retrieves a token from the authorization API using the client credentials flow.
        /// </summary>
        /// <returns>Token</returns>
        /// <see href="http://dev.commercetools.com/http-api-authorization.html#authorization-flows"/>
        public Task<Response<Token>> GetTokenAsync()
        {
            var pairs = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("scope", string.Concat(this.Configuration.Scope.ToEnumMemberString(), ":", this.Configuration.ProjectKey))
            };

            HttpRequestMessage httpRequestMessage = CreateTokenRequestMessage(pairs);
            return SendAsync<Token>(httpRequestMessage, false);
        }

        /// <summary>
        /// Refreshes a token from the authorization API using the refresh token flow.
        /// </summary>
        /// <param name="refreshToken">Refresh token value from the current token</param>
        /// <returns>Token</returns>
        /// <see href="http://dev.commercetools.com/http-api-authorization.html#authorization-flows"/>
        public Task<Response<Token>> RefreshTokenAsync(string refreshToken)
        {
            var pairs = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", refreshToken)
            };

            HttpRequestMessage requestMessage = CreateTokenRequestMessage(pairs);
            return SendAsync<Token>(requestMessage, false);
        }

        /// <summary>
        /// Creates a post request with basic authentication header and the given form content
        /// </summary>
        /// <param name="formContent">Parameters to post</param>
        /// <returns>HttpRequestMessage</returns>
        private HttpRequestMessage CreateTokenRequestMessage(IEnumerable<KeyValuePair<string, string>> formContent)
        {
            string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Concat(this.Configuration.ClientID, ":", this.Configuration.ClientSecret)));

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(this.Configuration.OAuthUrl))
            {
                Version = HttpVersion.Version10,
                Content = new FormUrlEncodedContent(formContent)
            };
            httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            return httpRequestMessage;
        }

        #endregion

        #region Utility

        /// <summary>
        /// Gets a response object from the API response.
        /// </summary>
        /// <typeparam name="T">Type of result</typeparam>
        /// <param name="httpResponseMessage">HttpResponseMessage</param>
        /// <returns>Response</returns>
        private async Task<Response<T>> GetResponse<T>(HttpResponseMessage httpResponseMessage)
        {
            Response<T> response = new Response<T>();
            Type resultType = typeof(T);

            response.StatusCode = (int)httpResponseMessage.StatusCode;
            response.ReasonPhrase = httpResponseMessage.ReasonPhrase;
            string jsonContent = await httpResponseMessage.Content.ReadAsStringAsync();

            if (response.StatusCode >= 200 && response.StatusCode < 300)
            {
                response.Success = true;

                if (resultType == typeof(JObject) || resultType == typeof(JArray) || resultType.IsArray || (resultType.IsGenericType && resultType.Name.Equals(typeof(List<>).Name)))
                {
                    response.Result = JsonConvert.DeserializeObject<T>(jsonContent);
                }
                else
                {
                    dynamic data = JsonConvert.DeserializeObject(jsonContent);
                    T model = ResponseModelFactory.CreateInstance<T>(data);
                    if (model != null)
                    {
                        response.Result = model;
                    }
                }
            }
            else
            {
                JObject data = JsonConvert.DeserializeObject<JObject>(jsonContent);

                response.Success = false;
                response.Errors = new List<ErrorMessage>();

                if (data != null && (data["errors"] != null))
                {
                    foreach (JObject error in data["errors"])
                    {
                        if (error.HasValues)
                        {
                            string code = error.Value<string>("code");
                            string message = error.Value<string>("message");
                            response.Errors.Add(new ErrorMessage(code, message));
                        }
                    }
                }
            }

            return response;
        }

        /// <summary>
        /// Returns a user agent string to identify the client
        /// </summary>
        /// <returns>User agent string</returns>
        private static string GetUserAgent()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string assemblyVersion = assembly.GetName().Version.ToString();
            string dotNetVersion = Environment.Version.ToString();
            return string.Format("commercetools-dotnet-sdk/{0} .NET/{1}", assemblyVersion, dotNetVersion);
        }

        #endregion
    }
}
