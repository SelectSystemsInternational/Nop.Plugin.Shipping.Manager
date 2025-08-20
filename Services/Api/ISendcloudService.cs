using System.Collections.Generic;
using System.Threading.Tasks;

using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Services.Shipping;

using Nop.Plugin.Shipping.Manager.Domain;
using Nop.Plugin.Shipping.Manager.Models;
using SendCloudApi.Net.Models;

namespace Nop.Plugin.Shipping.Manager.Services
{
    /// <summary>
    /// Shipping service interface
    /// </summary>
    public partial interface ISendcloudService
    {

        #region Utilities

        /// <summary>
        /// Get the shipping send from addresses
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of send from addresses
        /// </returns>rns>
        public Task<List<SenderAddress>> GetSendFromAddressesAsync();

        /// <summary>
        /// Create a sendcloud parcel
        /// </summary>
        /// <param name="houseNumber">Address string</param>
        /// <returns>
        /// The House number and additonal string
        /// </returns>
        public void SplitHouseNumber(ref string houseNumber, ref string houseNumberAddition);

        /// <summary>
        /// Serialize CustomValues of ProcessPaymentRequest
        /// </summary>
        /// <param name="request">Request</param>
        /// <returns>Serialized CustomValues</returns>
        public string SerializeCustomValues(Dictionary<string, object> customValues);

        /// <summary>
        /// Deserialize CustomValues of Order
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Serialized CustomValues CustomValues</returns>
        public Dictionary<string, object> DeserializeCustomValues(Order order);

        #endregion

        #region Methods

        /// <summary>
        /// Update sendcloud shipment rate configuration
        /// </summary>
        /// <param name="client">SendCloudApi client</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// </returns>
        public Task SendCloudUpdateAsync(SendCloudApi.Net.SendCloudApi client, bool updateShippingMethods);

        /// <summary>
        /// Create a sendcloud parcel
        /// </summary>
        /// <param name="shipment">Shipment</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// </returns>
        public Task SendCloudCreateParcelAsync(Nop.Core.Domain.Shipping.Shipment shipment);

        /// <summary>
        /// Create a canada post shipment
        /// </summary>
        /// <param name="shipment">Shipment</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// </returns>
        public Task<bool> SendcloudCancelParcelAsync(Nop.Core.Domain.Shipping.Shipment orderShipment);

        /// <summary>
        /// Is service point available
        /// </summary>
        /// <param name="servicePoint">ServicePoint</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// </returns>
        public Task<bool> SendcloudIsServicePointAvailableAsync(int servicePoint);

        /// <summary>
        /// Validate shipping option
        /// </summary>
        /// <param name="carrier">Carrier</param>
        /// <param name="shippingMethod">ShippingMethod</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// </returns>
        public Task<bool> SendCloudValidateShippingOptionAsync(Carrier carrier, ShippingMethod shippinMethod);

        /// <summary>
        /// Get a flag if the configuration is valid
        /// </summary>
        /// <param name="countryCode">Country code string</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the status if the sendcloud configuration
        /// </returns>
        public Task<List<string>> SendCloudValidateConfigurationAsync(SendCloudApi.Net.SendCloudApi client, int storeId, int vendorId);

        #endregion

        #region Shipping Rate Calculation

        /// <summary>
        ///  Gets available shipping options
        /// </summary>
        /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        /// <returns>Represents a response of getting shipping rate options</returns>
        public Task<GetShippingOptionResponse> GetShippingOptionsAsync(GetShippingOptionRequest getShippingOptionRequest);

        /// <summary>
        /// Get the shipping method options
        /// </summary>
        /// <param name="shippingOptionRequests">List of ShippingOptionRequests</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of shipping option requests
        /// </returns>rns>
        public Task<GetShippingOptionResponse> GetShippingMethodOptionsAsync(ShippingManagerCalculationOption smco);

        /// <summary>
        ///  Gets available shipping options
        /// </summary>
        /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        /// <returns> 
        /// A task that represents the asynchronous operation
        /// The task result contains the list of responses of shipping rate options
        /// </returns>
        public Task<GetShippingOptionResponse> GetShippingOptionsAsync(ShippingManagerCalculationOption smco);

        #endregion

    }
}
