using System;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Shipping;

namespace Nop.Plugin.Shipping.Manager.Services
{
    /// <summary>
    /// Shipment service interface
    /// </summary>
    public partial interface IShippingManagerShipmentService
    {

        /// <summary>
        /// Search shipments
        /// </summary>
        /// <param name="vendorId">Vendor identifier; 0 to load all records</param>
        /// <param name="vendorGroupId">Vendor Group identifier; 0 to load all records</param>
        /// <param name="billingAddressId">Billing Address identifier; 0 to load all records</param>
        /// <param name="shippingAddressId">Shipping Address identifier; 0 to load all records</param>
        /// <param name="warehouseId">Warehouse identifier, only shipments with products from a specified warehouse will be loaded; 0 to load all orders</param>
        /// <param name="carrierId">Carrier identifier, only shipments with products from a specified carrier will be loaded; 0 to load all orders</param> 
        /// <param name="shippingCountryId">Shipping country identifier; 0 to load all records</param>
        /// <param name="shippingStateId">Shipping state identifier; 0 to load all records</param>
        /// <param name="shippingCounty">Shipping county; null to load all records</param>
        /// <param name="shippingCity">Shipping city; null to load all records</param>
        /// <param name="trackingNumber">Search by tracking number</param>
        /// <param name="loadNotShipped">A value indicating whether we should load only not shipped shipments</param>
        /// <param name="loadNotDelivered">A value indicating whether we should load only not delivered shipments</param>
        /// <param name="orderId">Order identifier; 0 to load all records</param>
        /// <param name="createdFromUtc">Created date from (UTC); null to load all records</param>
        /// <param name="createdToUtc">Created date to (UTC); null to load all records</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipments
        /// </returns>
        public Task<IPagedList<Shipment>> GetAllShipmentsAsync(int pageIndex = 0, int pageSize = int.MaxValue,
            int vendorId = 0,
            int vendorGroupId = 0,
            int billingAddressId = 0,
            int shippingAddressId = 0,
            int warehouseId = 0,
            int carrierId = 0,
            int shippingCountryId = 0,
            int shippingStateId = 0,
            string shippingCounty = null,
            string shippingCity = null,
            string trackingNumber = null,
            string shippingMethod = null,
            bool loadNotShipped = false,
            bool loadNotDelivered = false,
            int orderId = 0,
            DateTime? createdFromUtc = null, DateTime? createdToUtc = null);

    }
}