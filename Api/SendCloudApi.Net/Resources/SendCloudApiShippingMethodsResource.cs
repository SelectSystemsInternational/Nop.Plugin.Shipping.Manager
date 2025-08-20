﻿using System.Collections.Generic;
using System.Threading.Tasks;
using SendCloudApi.Net.Models;

namespace SendCloudApi.Net.Resources
{
    public class SendCloudApiShippingMethodsResource : SendCloudApiAbstractResource
    {
        public SendCloudApiShippingMethodsResource(SendCloudApi client) : base(client)
        {
            Resource = "shipping_methods";
            ListResource = "shipping_methods";
            SingleResource = "shipping_method";
            CreateRequest = false;
            UpdateRequest = false;
        }

        public async Task<SendCloudShippingMethod[]> Get(string senderAddress = null, int? servicePointId = null)
        {
            var parameters = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(senderAddress))
                parameters.Add("sender_address", senderAddress);
            if (servicePointId.HasValue)
                parameters.Add("service_point_id", servicePointId.ToString());
            return await Get<SendCloudShippingMethod[]>(parameters: parameters);
        }

        public async Task<SendCloudShippingMethod> Get(int shippingMethodId)
        {
            return await Get<SendCloudShippingMethod>(shippingMethodId);
        }
    }
}
