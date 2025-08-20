using System.Threading.Tasks;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Services.Orders;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Pickup;
using Nop.Services.Shipping.Tracking;

namespace Nop.Plugin.Shipping.Manager.Services
{
    /// <summary>
    /// Shipment service
    /// </summary>
    public partial class CustomShipmentService : ShipmentService
    {
        #region Fields

        #endregion

        #region Ctor

        public CustomShipmentService(IPickupPluginManager pickupPluginManager,
            IRepository<Address> addressRepository,
            IRepository<Order> orderRepository,
            IRepository<OrderItem> orderItemRepository,
            IRepository<Product> productRepository,
            IRepository<Shipment> shipmentRepository,
            IRepository<ShipmentItem> siRepository,
            IShippingPluginManager shippingPluginManager) : base (pickupPluginManager,
                addressRepository,
                orderRepository,
                orderItemRepository,
                productRepository,
                shipmentRepository,
                siRepository,
                shippingPluginManager)
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Get the tracker of the shipment
        /// </summary>
        /// <param name="shipment">Shipment</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipment tracker
        /// </returns>
        public override async Task<IShipmentTracker> GetShipmentTrackerAsync(Shipment shipment)
        {

            var orderService = EngineContext.Current.Resolve<IOrderService>();

            var order = await orderService.GetOrderByIdAsync(shipment.OrderId);
            if (order != null && !string.IsNullOrEmpty(order.ShippingRateComputationMethodSystemName))
            {

                if (!order.PickupInStore)
                {
                    if (order.ShippingRateComputationMethodSystemName == ShippingManagerDefaults.SendCloudSystemName)
                    {
                        var shippingRateComputationMethod = await _shippingPluginManager.LoadPluginBySystemNameAsync("Shipping.Manager");
                        return await shippingRateComputationMethod?.GetShipmentTrackerAsync();
                    }
                    else if (order.ShippingRateComputationMethodSystemName == ShippingManagerDefaults.AramexSystemName)
                    {
                        var shippingRateComputationMethod = await _shippingPluginManager.LoadPluginBySystemNameAsync("Shipping.Manager");
                        return await shippingRateComputationMethod?.GetShipmentTrackerAsync();
                    }
                    else
                    {
                        var shippingRateComputationMethod = await _shippingPluginManager.LoadPluginBySystemNameAsync(order.ShippingRateComputationMethodSystemName);
                        if (shippingRateComputationMethod != null)
                            return await shippingRateComputationMethod?.GetShipmentTrackerAsync();
                    }
                }
                else
                {
                    var pickupPointProvider = await _pickupPluginManager.LoadPluginBySystemNameAsync(order.ShippingRateComputationMethodSystemName);
                    return await pickupPointProvider?.GetShipmentTrackerAsync();
                }
            }

            return null;
        }

        #endregion
    }
}