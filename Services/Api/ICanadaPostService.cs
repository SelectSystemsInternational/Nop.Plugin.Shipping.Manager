using System.Collections.Generic;
using System.Threading.Tasks;

using Nop.Services.Shipping;

using CanadaPostApi.Api;

namespace Nop.Plugin.Shipping.Manager.Services
{
    /// <summary>
    /// Shipping service interface
    /// </summary>
    public partial interface ICanadaPostService
    {

        #region Utilities

        #endregion

        #region Methods

        /// <summary>
        /// Update sendcloud shipment rate configuration
        /// </summary>
        /// <param name="client">CanadaPostApi client</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// </returns>
        public Task CanadaPostUpdateAsync();

        /// <summary>
        /// Create a sendcloud parcel
        /// </summary>
        /// <param name="shipment">Shipment</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// </returns>
        public Task<bool> CanadaPostCreateShipmentAsync(Nop.Core.Domain.Shipping.Shipment shipment);

        /// <summary>
        /// Create a canada post request shipment refund
        /// </summary>
        /// <param name="shipment">Shipment</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// </returns>
        public Task<bool> CanadaPostRefundShipmentAsync(Nop.Core.Domain.Shipping.Shipment orderShipment);

        /// <summary>
        /// Get a flag if a shipping option configuation is valid
        /// </summary>
        /// <param name="carrier">Carrier</param>
        /// <param name="shippinMethod">ShippingMethod</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the status if the sendcloud configuration
        /// </returns>
        public string CanadaPostValidateShippingOptionAsync(out string errors);

        /// <summary>
        /// Get a flag if the configuation is valid
        /// </summary>
        /// <param name="countryCode">Country code string</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the status if the sendcloud configuration
        /// </returns>
        public Task<(string, List<string>)> CanadaPostValidateConfigurationAsync(int storeId, int vendorId);

        #endregion

        #region Shipping Rate Calculation

        /// <summary>
        /// Get the shipping method options
        /// </summary>
        /// <param name="shippingOptionRequests">List of ShippingOptionRequests</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of shipping option requests
        /// </returns>rns>
        public Task<GetShippingOptionResponse> GetShippingMethodOptionsAsync(IList<GetShippingOptionRequest> shippingOptionRequests);

        /// <summary>
        ///  Gets available shipping options
        /// </summary>
        /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        /// <returns> 
        /// A task that represents the asynchronous operation
        /// The task result contains the list of responses of shipping rate options
        /// </returns>
        public Task<GetShippingOptionResponse> GetShippingOptionsAsync(GetShippingOptionRequest getShippingOptionRequest);

        /// <summary>
        ///  Print an artifact label
        /// </summary>
        /// <param name="shippingApi">The shipping api client</param>
        /// <param name="shipmentId">The shipment identifier</param>
        /// <param name="aftifactUrl">The artifact Url returned from the create shipment</param>
        /// <returns> 
        /// A task that represents the asynchronous operation
        /// The task result contains
        /// - filePath - the file path 
        /// - filename - the file name
        /// - errors - any error
        /// </returns>
        public (string, string, string) PrintArtifact(ShippingApi shippingApi, string shipmentId, string aftifactUrl);

        #endregion

    }
}