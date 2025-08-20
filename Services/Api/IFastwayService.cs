using System.Collections.Generic;
using System.Threading.Tasks;

using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Shipping;
using Nop.Services.Shipping;

using Nop.Plugin.Shipping.Manager.Models;

using myFastway.ApiClient.Models;

namespace Nop.Plugin.Shipping.Manager.Services
{
    /// <summary>
    /// Shipping service interface
    /// </summary>
    public partial interface IFastwayService
    {

        #region Shipping Rate Calculation

        /// <summary>
        /// Get the shipping method options
        /// </summary>
        /// <param name="shippingOptionRequests">List of ShippingOptionRequests</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of shipping option requests
        /// </returns>
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
        /// Format the shipping method option
        /// </summary>
        /// <param name="shippingOption">Shipping Option</param>
        /// <param name="smco">Shipping Manager Calculation Option</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the formated shipping option
        /// </returns>
        public Task<ShippingOption> FormatOptionDetails(ShippingOption shippingOption, ShippingManagerCalculationOption smco);

        /// <summary>
        /// Get rate by weight and by subtotal
        /// </summary>
        /// <param name="shippingByWeightByTotalRecord">ShippingManagerByWeightByTotal</param>
        /// <param name="quote">Quoted value</param>
        /// <param name="weight">weight value</param>
        /// <returns>The calculated rate</returns>
        public Task<decimal?> CalculateRate(ShippingManagerCalculationOption smco, decimal rate, decimal weight);

        #endregion

        #region Lables

        /// <summary>
        /// Create a fastway parcel
        /// </summary>
        /// <param name="shipment">Shipment</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// </returns>
        public Task<Shipment> FastwayCreateParcelAsync(Nop.Core.Domain.Shipping.Shipment shipment);

        #endregion

        #region Fastway Api Methods

        Task<QuoteModel> GetTestQuote();

        Task<List<ServiceModel>> GetServices();

        Task<QuoteModel> GetTestQuote(string apikey, string apiSecret);

        Task AramexUpdateAsync(List<ServiceModel> services, bool updateShippingMethods);

        #endregion

    }
}