﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SendCloudApi.Net.Exceptions;
using SendCloudApi.Net.Helpers;
using SendCloudApi.Net.Resources;

namespace SendCloudApi.Net
{

    public class SendCloudApi
    {
        protected readonly string _apiKey;
        protected readonly string _apiSecret;
        protected readonly string _partnerUuid;
        public readonly SendCloudApiParcelsResource Parcels;
        public readonly SendCloudApiParcelDocumentsResource ParcelDocuments;
        public readonly SendCloudApiParcelStatusResource ParcelStatuses;
        public readonly SendCloudApiReturnsResource Returns;
        public readonly SendCloudApiBrandsResource Brands;
        public readonly SendCloudApiShippingMethodsResource ShippingMethods;
        public readonly SendCloudApiLabelsResource Label;
        public readonly SendCloudApiUsersResource User;
        public readonly SendCloudApiInvoicesResource Invoices;
        public readonly SendCloudApiSenderAddressResource SenderAddresses;
        public readonly SendCloudApiIntegrationsResource Integrations;
        public readonly SendCloudApiReturnPortalResource ReturnPortal;
        public readonly SendCloudApiServicePointsResource ServicePoints;

        public SendCloudApi(string apiKey, string apiSecret, string partnerUuid = null)
        {
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
            {
                throw new SendCloudException("Sendcloud Configuration: You must have an API key and an API secret key");
            }

            _apiKey = apiKey;
            _apiSecret = apiSecret;
            _partnerUuid = partnerUuid;
            Parcels = new SendCloudApiParcelsResource(this);
            ParcelDocuments = new SendCloudApiParcelDocumentsResource(this);
            ParcelStatuses = new SendCloudApiParcelStatusResource(this);
            Returns = new SendCloudApiReturnsResource(this);
            Brands = new SendCloudApiBrandsResource(this);
            ShippingMethods = new SendCloudApiShippingMethodsResource(this);
            Label = new SendCloudApiLabelsResource(this);
            User = new SendCloudApiUsersResource(this);
            Invoices = new SendCloudApiInvoicesResource(this);
            SenderAddresses = new SendCloudApiSenderAddressResource(this);
            Integrations = new SendCloudApiIntegrationsResource(this);
            ReturnPortal = new SendCloudApiReturnPortalResource(this);
            ServicePoints = new SendCloudApiServicePointsResource(this);

        }

        internal string GetApiKey()
        {
            return _apiKey;
        }

        internal string GetApiSecret()
        {
            return _apiSecret;
        }

        internal string GetBasicAuth()
        {
            return $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes(_apiKey + ":" + _apiSecret))}";
        }

        internal string GetAccessToken()
        {
            return $"AccessToken {_apiKey}";
        }

        internal async Task<T> Create<T>(string url, string authorization, string data, string returnObject, string dateTimeFormat)
        {
            return await SendRequest<T>(url, authorization, "POST", null, data, returnObject, dateTimeFormat);
        }

        internal async Task<T> Get<T>(string url, string authorization, Dictionary<string, string> parameters, string returnObject, string dateTimeFormat)
        {
            return await SendRequest<T>(url, authorization, "GET", parameters, null, returnObject, dateTimeFormat);
        }

        internal async Task<T> Update<T>(string url, string authorization, string data, string returnObject, string dateTimeFormat)
        {
            return await SendRequest<T>(url, authorization, "PUT", null, data, returnObject, dateTimeFormat);
        }

        internal async Task<T> Cancel<T>(string url, string authorization, string data, string returnObject, string dateTimeFormat)
        {
            return await SendRequest<T>(url, authorization, "DELETE", null, null, returnObject, dateTimeFormat);
        }

        internal Uri GetUrl(string url, Dictionary<string, string> parameters = null)
        {
            if (parameters != null && parameters.Any())
            {
                var queryParameters = string.Join("&",
                    parameters.Select(
                        p =>
                            string.IsNullOrEmpty(p.Value)
                                ? Uri.EscapeDataString(p.Key)
                                : $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
                if (!string.IsNullOrWhiteSpace(queryParameters))
                {
                    url += "?" + queryParameters;
                }
            }
            return new Uri(url);
        }

        private async Task<T> SendRequest<T>(string url, string authorization, string method, Dictionary<string, string> parameters, string data, string returnObject, string dateTimeFormat)
        {
            var httpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(authorization))
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", authorization);
            }
            if (!string.IsNullOrWhiteSpace(_partnerUuid))
            {
                httpClient.DefaultRequestHeaders.Add("Sendcloud-Partner-Id", _partnerUuid);
            }

            var requestUri = GetUrl(url, parameters);
            HttpResponseMessage response = new HttpResponseMessage();
            switch (method.ToUpper())
            {
                case "GET":
                    {
                        response = await httpClient.GetAsync(requestUri).ConfigureAwait(false);
                    }
                    break;
                case "POST":
                    {
                        var content = new StringContent(data);
                        content.Headers.Clear();
                        content.Headers.Add("Content-Type", "application/json;charset=utf-8");
                        response = await httpClient.PostAsync(requestUri, content).ConfigureAwait(false);
                    }
                    break;
                case "PUT":
                    {
                        var content = new StringContent(data);
                        content.Headers.Clear();
                        content.Headers.Add("Content-Type", "application/json;charset=utf-8");
                        response = await httpClient.PutAsync(requestUri, content).ConfigureAwait(false);
                    }
                    break;
                case "DELETE":
                    {
                        var content = new StringContent(String.Empty);
                        content.Headers.Clear();
                        content.Headers.Add("Content-Type", "application/json");
                        response = await httpClient.PostAsync(requestUri, content).ConfigureAwait(false);
                    }
                break;
            }
            if (response.IsSuccessStatusCode)
            {
                var jsonResult = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(returnObject))
                {
                    Dictionary<string, T> result = JsonHelper.DeserializeAsDictionary<T>(jsonResult, dateTimeFormat);
                    return result[returnObject];
                }
                return JsonHelper.Deserialize<T>(jsonResult, dateTimeFormat);
            }
            else if (method == "DELETE" && response.StatusCode == HttpStatusCode.Gone)
            {
                var jsonResult = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(returnObject))
                {
                    Dictionary<string, T> result = JsonHelper.DeserializeAsDictionary<T>(jsonResult, dateTimeFormat);
                    return result[returnObject];
                }
                return JsonHelper.Deserialize<T>(jsonResult, dateTimeFormat);
            }

            await HandleResponseError(response);
            return default(T);
        }

        private async Task HandleResponseError(HttpResponseMessage response)
        {
            string message;
            switch (response.StatusCode)
            {
                case HttpStatusCode.NotFound:
                    message = "Page not found";
                    break;
                default:
                    var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var result = JsonHelper.Deserialize<SendCloudError>(responseBody, "yyyy-MM-dd HH:mm:ss");

                    message = result.Message;

                    if (!string.IsNullOrEmpty(result.Error?.Message))
                    {
                        message = result.Error.Message;
                    }

                    break;
            }
            throw new SendCloudException(message);
        }

        //public async Task<string> Download(string url)
        //{
        //    var httpClient = new HttpClient();
        //    httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes(_apiKey + ":" + _apiSecret))}");
        //    if (!string.IsNullOrWhiteSpace(_partnerUuid))
        //    {
        //        httpClient.DefaultRequestHeaders.Add("Sendcloud-Partner-Id", _partnerUuid);
        //    }
        //    httpClient.DefaultRequestHeaders.Accept.Clear();
        //    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));

        //    var response = await httpClient.GetAsync(url).ConfigureAwait(false);
        //    if (response.IsSuccessStatusCode)
        //    {
        //        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        //    }
        //    return null;
        //}

        public async Task<byte[]> Download(string url)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes(_apiKey + ":" + _apiSecret))}");
            if (!string.IsNullOrWhiteSpace(_partnerUuid))
            {
                httpClient.DefaultRequestHeaders.Add("Sendcloud-Partner-Id", _partnerUuid);
            }
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));

            var response = await httpClient.GetAsync(url).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }
            return null;
        }

        //public async Task<Stream> Download(string url)
        //{
        //    var httpClient = new HttpClient();
        //    httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes(_apiKey + ":" + _apiSecret))}");
        //    if (!string.IsNullOrWhiteSpace(_partnerUuid))
        //    {
        //        httpClient.DefaultRequestHeaders.Add("Sendcloud-Partner-Id", _partnerUuid);
        //    }
        //    httpClient.DefaultRequestHeaders.Accept.Clear();
        //    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));

        //    var response = await httpClient.GetAsync(url).ConfigureAwait(false);
        //    if (response.IsSuccessStatusCode)
        //    {
        //        return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        //    }
        //    return null;
        //}
    }
}
