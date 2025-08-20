using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Shipping;

using Nop.Plugin.Shipping.Manager.Domain;

namespace Nop.Plugin.Shipping.Manager.Services
{
    /// <summary>
    /// Shipment service interface
    /// </summary>
    public partial interface IShipmentDetailsService
    {
        /// <summary>
        /// Deletes a shipment packaging option
        /// </summary>
        /// <param name="shipment">Shipment</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task DeleteShipmentDetailsAsync(ShipmentDetails shipmentDetails);

        /// <summary>
        /// Search shipments
        /// </summary>
        /// <param name="shippingId">Shipping identifier</param>
        /// <returns>
        /// A list of shipping item packing options for the shipment id
        /// </returns>
        ShipmentDetails GetShipmentDetailsForShipmentId(int shippingId = 0);

        /// <summary>
        /// Search for shipment packaging options
        /// </summary>
        /// <param name="shippingId">Shipping item identifier; 0 to load all records</param>
        /// <param name="vendorId">Vendor identifier; 0 to load all records</param>
        /// <param name="warehouseId">Warehouse identifier, only shipments with products from a specified warehouse will be loaded; 0 to load all orders</param>
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
        public Task<IPagedList<ShipmentDetails>> GetAllShipmentDetailsAsync(int shippingId = 0,
            int vendorId = 0,
            int warehouseId = 0,
            int shippingCountryId = 0,
            int shippingStateId = 0,
            string shippingCounty = null,
            string shippingCity = null,
            string trackingNumber = null,
            bool loadNotShipped = false,
            bool loadNotDelivered = false,
            int orderId = 0,
            DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
            int pageIndex = 0, int pageSize = int.MaxValue);

        /// <summary>
        /// Get shipment by identifiers
        /// </summary>
        /// <param name="shipmentIds">Shipment identifiers</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipments
        /// </returns>
        Task<IList<ShipmentDetails>> GetShipmentDetailsByIdsAsync(int[] shipmentIds);

        /// <summary>
        /// Gets a shipment packaging option
        /// </summary>
        /// <param name="shipmentDetailsId">Shipment Packing Option identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipment
        /// </returns>
        Task<ShipmentDetails> GetShipmentDetailssByIdAsync(int shipmentDetailsId);

        /// <summary>
        /// Gets a list of order shipments
        /// </summary>
        /// <param name="orderId">Order identifier</param>
        /// <param name="shipped">A value indicating whether to count only shipped or not shipped shipments; pass null to ignore</param>
        /// <param name="vendorId">Vendor identifier; pass 0 to ignore</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result
        /// </returns>
        Task<IList<ShipmentDetails>> GetShipmentDetailsByOrderIdAsync(int orderId, bool? shipped = null, int vendorId = 0);

        /// <summary>
        /// Inserts a shipment packaging option
        /// </summary>
        /// <param name="shipment">Shipment</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task InsertShipmentDetailsAsync(ShipmentDetails shipmentDetails);

        /// <summary>
        /// Updates the shipment packaging option
        /// </summary>
        /// <param name="shipment">Shipment</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task UpdateShipmentDetailsAsync(ShipmentDetails shipmentDetails);

        /// <summary>
        /// Get quantity in shipments. For example, get planned quantity to be shipped
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="warehouseId">Warehouse identifier</param>
        /// <param name="ignoreShipped">Ignore already shipped shipments</param>
        /// <param name="ignoreDelivered">Ignore already delivered shipments</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the quantity
        /// </returns>
        Task<int> GetQuantityInShipmentDetailsAsync(Product product, int warehouseId,
            bool ignoreShipped, bool ignoreDelivered);

    }
}
