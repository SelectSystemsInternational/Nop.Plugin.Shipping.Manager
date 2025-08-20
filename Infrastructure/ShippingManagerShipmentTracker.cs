using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using Nop.Core.Infrastructure;
using Nop.Core.Domain.Logging;
using Nop.Core.Domain.Shipping;
using Nop.Services.Common;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Shipping.Tracking;

using Nop.Plugin.Shipping.Manager.Services;
using Nop.Plugin.Shipping.Manager.Settings;

namespace Nop.Plugin.Shipping.Manager
{
    /// <summary>
    /// Represents the Shipping Manager shipment tracker
    /// </summary>
    public class ShippingManagerShipmentTracker : IShipmentTracker
    {
        #region Fields

        protected readonly IShippingManagerService _shippingManagerService;
        protected readonly ShippingManagerSettings _shippingManagerSettings;
        protected readonly ILogger _logger;
        protected readonly IAddressService _addressService;

        #endregion

        #region Ctor

        public ShippingManagerShipmentTracker(IShippingManagerService shippingManagerService, 
            ShippingManagerSettings shippingManagerSettings, 
            ILogger logger,
            IAddressService addressService)
        {
            _shippingManagerService = shippingManagerService;
            _shippingManagerSettings = shippingManagerSettings;
            _logger = logger;
            _addressService = addressService;
        }

        #endregion

        #region Methods


        /// <summary>
        /// Gets if the current tracker can track the tracking number.
        /// </summary>
        /// <param name="trackingNumber">The tracking number to track.</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the rue if the tracker can track, otherwise false.
        /// </returns>
        public virtual Task<bool> IsMatchAsync(string trackingNumber)
        {
            if (string.IsNullOrEmpty(trackingNumber))
                return Task.FromResult(false);

            //details on https://www.ups.com/us/en/tracking/help/tracking/tnh.page
            return Task.FromResult(Regex.IsMatch(trackingNumber, "^1Z[A-Z0-9]{16}$", RegexOptions.IgnoreCase) ||
                                   Regex.IsMatch(trackingNumber, "^\\d{9}$", RegexOptions.IgnoreCase) ||
                                   Regex.IsMatch(trackingNumber, "^T\\d{10}$", RegexOptions.IgnoreCase) ||
                                   Regex.IsMatch(trackingNumber, "^\\d{12}$", RegexOptions.IgnoreCase));
        }

        /// <summary>
        /// Gets an URL for a page to show tracking info (third party tracking page).
        /// </summary>
        /// <param name="trackingNumber">The tracking number to track.</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the uRL of a tracking page.
        /// </returns>
        public virtual async Task<string> GetUrlAsync(string trackingNumber, Shipment shipment = null)
        {
            string postcode = string.Empty;

            var orderService = EngineContext.Current.Resolve<IOrderService>();
            var order = await orderService.GetOrderByIdAsync(shipment.OrderId);
            if (order != null)
            {
                if (order.ShippingRateComputationMethodSystemName == ShippingManagerDefaults.SendCloudSystemName)
                {

                    if (order.ShippingAddressId.HasValue)
                    {
                        var shippingAddress = await _addressService.GetAddressByIdAsync(order.ShippingAddressId.Value);
                        if (shippingAddress != null)
                        {
                            postcode = shippingAddress.ZipPostalCode.Replace(" ", "");
                        }

                        return $"https://tracking.sendcloud.sc/forward?code={trackingNumber}&verification={postcode}";
                    }
                }

                if (order.ShippingRateComputationMethodSystemName == ShippingManagerDefaults.AramexSystemName)
                    return $"https://www.aramex.com/us/en/track/results?mode=0&ShipmentNumber={trackingNumber}";
            }

            return null;
        }

        /// <summary>
        /// Gets all events for a tracking number.
        /// </summary>
        /// <param name="trackingNumber">The tracking number to track</param>
        /// <param name="shipment">Shipment; pass null if the tracking number is not associated with a specific shipment</param> 
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of Shipment Events.
        /// </returns>
        public async Task<IList<ShipmentStatusEvent>> GetShipmentEventsAsync(string trackingNumber, Shipment shipment = null)
        {
            var result = new List<ShipmentStatusEvent>();

            if (string.IsNullOrEmpty(trackingNumber))
                return result;

            if (_shippingManagerSettings.TestMode)
            {
                await _logger.InsertLogAsync(LogLevel.Debug, "Tracking Number: " + trackingNumber, null, null);
            }

            if (!string.IsNullOrEmpty(trackingNumber))
            {
                var events = new List<ShipmentStatusEvent>();
                {
                    events.Add(new ShipmentStatusEvent
                    {
                        EventName = "Event Name",
                        Location = "Event Location",
                        Date = DateTime.Now
                    });
                }

                return events;

            }

            return new List<ShipmentStatusEvent>();

        }


        #endregion
    }
}
