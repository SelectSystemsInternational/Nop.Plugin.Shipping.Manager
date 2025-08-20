using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

using myFastway.ApiClient.Models;

namespace myFastway.ApiClient
{
    public class ApiClient
    {
        const string JsonContentType = "application/json";

        protected readonly ConfigModel _config;

        static readonly HttpClient _httpClient = new HttpClient();

        public ApiClient(string authority, string clientId, string secret, string scope, bool requireHttps, string baseAddress, string apiVersion)
        {
            //string apiVersion = "1.0";
            //string baseAddress = "https://api.myfastway.com.au";
            //string authority = "https://identity.fastway.org";
            //string clientId = "fw-fl2-SYD3450061-e817403c7263";
            //string secret = "2cb69b6d-905c-41f8-83d8-d4f6f103cc63";
            //bool requireHttps = true;
            //string scope = string.Empty;

            _config = new ConfigModel(authority, clientId, secret, scope, requireHttps, baseAddress, apiVersion);

        }

        /// <summary>
        /// Returns an access token using the HttpClient
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetClientCredentialHttpClient()
        {

            var content = new StringContent($"grant_type=client_credentials&client_id={_config.OAuth.ClientId}&scope={_config.OAuth.Scope}&client_secret={_config.OAuth.Secret}");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            var response = await _httpClient.PostAsync($"{_config.OAuth.Authority}/connect/token", content);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                return JObject.Parse(result)["access_token"].ToString();
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
            {
                var errorList = new List<ErrorModel>();
                var errorModel = new ErrorModel();

                errorModel.Code = "System.Net.HttpStatusCode.InternalServerError";
                errorModel.Message = "Fastwasy system maybe unavailable - Check Api settings and if issue persists try again later";
                errorList.Add(errorModel);

                throw new BadRequestException(errorList);
            }

            return null;
        }

        public async Task<T> GetSingle<T>(string url, string apiVersion = "1.0")
        {
            var response = await CallApi(GetClientCredentialHttpClient, async (client) => await client.GetAsync($"api/{url}"), apiVersion);
            var result = await ParseResponse<T>(response);
            return result;
        }

        public async Task<byte[]> GetBytes(string url, string apiVersion = "1.0")
        {
            var response = await CallApi(GetClientCredentialHttpClient, async (client) => await client.GetAsync($"api/{url}"), apiVersion);
            var result = await response.Content.ReadAsByteArrayAsync();
            return result;
        }

        public async Task<HttpResponseMessage> PostSingle(string url, object payload, string apiVersion = "1.0")
        {
            var content = GetPayloadContent(payload);
            var result = await CallApi(GetClientCredentialHttpClient, async (client) => await client.PostAsync($"api/{url}", content), apiVersion);
            return result;
        }

        public async Task<HttpResponseMessage> PutSingle(string url, object payload, string apiVersion = "1.0")
        {
            var content = GetPayloadContent(payload);
            var result = await CallApi(GetClientCredentialHttpClient, async (client) => await client.PutAsync($"api/{url}", content), apiVersion);
            return result;
        }

        public async Task<T> PostSingle<T>(string url, object payload, string apiVersion = "1.0")
        {

            var response = await PostSingle(url, payload, apiVersion);
            var result = await ParseResponse<T>(response);
            return result;
        }

        public async Task<T> PutSingle<T>(string url, object payload, string apiVersion = "1.0")
        {

            var response = await PutSingle(url, payload, apiVersion);
            var result = await ParseResponse<T>(response);
            return result;
        }

        public async Task<IEnumerable<T>> GetCollection<T>(string url, string apiVersion = "1.0")
        {

            var response = await CallApi(GetClientCredentialHttpClient, async (client) => await client.GetAsync($"api/{url}"), apiVersion);
            var result = await ParseResponse<IEnumerable<T>>(response);
            return result;
        }

        public async Task<HttpResponseMessage> Delete(string url, string apiVersion = "1.0")
        {
            var result = await CallApi(GetClientCredentialHttpClient, async client => await client.DeleteAsync($"api/{url}"), apiVersion);
            return result;
        }

        StringContent GetPayloadContent(object payload)
        {
            var serialised = payload == null ? string.Empty : JsonConvert.SerializeObject(payload);
            var result = new StringContent(serialised, Encoding.UTF8, JsonContentType);
            return result;
        }

        async Task<T> ParseResponse<T>(HttpResponseMessage response)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            var jobj = JObject.Parse(responseBody);

            if (response.IsSuccessStatusCode)
                return jobj["data"].ToObject<T>();

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errors = jobj["errors"].ToObject<List<ErrorModel>>();
                throw new BadRequestException(errors);
            }

            return default(T);
        }

        public async Task<IList<ErrorModel>> ParseErrors(HttpResponseMessage response)
        {
            if (response.StatusCode != HttpStatusCode.BadRequest)
                return null;

            var responseBody = await response.Content.ReadAsStringAsync();
            var jobj = JObject.Parse(responseBody);
            return jobj["errors"].ToObject<List<ErrorModel>>();
        }

        /// <summary>
        /// Make a call to the api, setting the required headers and bearer token.  In the case where the token has expired, it renews
        /// the token and trys a single time more.
        /// </summary>
        /// <param name="getAccessToken">a function to retrieve an acces token</param>
        /// <param name="callApi">the function making the request</param>
        /// <param name="apiVersion">the version of the api being called.</param>
        /// <returns></returns>
        async Task<HttpResponseMessage> CallApi(Func<Task<string>> getAccessToken, Func<HttpClient, Task<HttpResponseMessage>> callApi, string apiVersion)
        {
            HttpResponseMessage retVal = null;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                var jsonHeader = new MediaTypeWithQualityHeaderValue(JsonContentType);

                if (!string.IsNullOrEmpty(apiVersion))
                    jsonHeader.Parameters.Add(new NameValueHeaderValue("api-version", apiVersion));

                client.DefaultRequestHeaders.Accept.Add(jsonHeader);
                client.BaseAddress = new Uri(_config.Api.BaseAddress);
                var accessToken = await getAccessToken();
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await getAccessToken());

                retVal = await callApi(client);

                // check once to see if the access token has expired (default lifetime: 60 mins)
                if (retVal.StatusCode == HttpStatusCode.Unauthorized)
                {
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await getAccessToken());
                    retVal = await callApi(client);
                }

                return retVal;
            }
        }

        /// <summary>
        /// For a given model, loads a json object with the expected results from disk.  Before a test referencing this function will pass, a 'local' copy
        /// of the file will have to be created by copying <typeparamref name="T"/>.json to <typeparamref name="T"/>.local.json populating the default
        /// object with the expected results.
        /// </summary>
        /// <typeparam name="T">The type of the model to be loaded from file</typeparam>
        /// <param name="filename">The phyiscal file name (excluding the .local extension)</param>
        /// <returns></returns>
        public async Task<T> LoadModelFromFile<T>(string filename)
        {

            string localFileName = $"{filename}.local.json";

            if (File.Exists(localFileName))
            {
                var serializedObject = await File.ReadAllTextAsync(localFileName);
                return JsonConvert.DeserializeObject<T>(serializedObject);
            }

            throw new FileNotFoundException($"Cannot find file {localFileName}.  Please copy {filename}.json to a new file {localFileName} poplulating the model with the expected results ", localFileName);
        }
    }

    public class BadRequestException : Exception
    {
        public List<ErrorModel> Errors { get; }

        public BadRequestException(List<ErrorModel> errors)
        {
            Errors = errors;
        }
    }

    public class FastwayException : Exception
    {
        public FastwayException(string message) : base(message) { }
    }
}