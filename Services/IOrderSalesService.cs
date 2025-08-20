using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Services.Shipping.Tracking;

namespace Nop.Plugin.Shipping.Manager.Services
{
    /// <summary>
    /// Shipment service interface
    /// </summary>
    public partial interface IOrderSalesService
    {

        #region Methods

        /// <summary>
        /// Get localised short date format
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the localised short date format
        /// </returns>
        public Task<string> GetLocalisedShortDateFormatAsync();

        /// <summary>
        /// Get localised short date format for display
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the localised short date format
        /// </returns>
        public Task<string> GetLocalisedDisplayShortDateFormatAsync();

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
        public Task<int> GetQuantityInShipmentsAsync(Product product, int warehouseId,
            bool ignoreShipped, bool ignoreDelivered);

        /// <summary>
        /// Gets a total number of items in all shipments
        /// </summary>
        /// <param name="orderItem">Order item</param>
        /// <returns> The total number of items in all shipments </returns>
        public int GetTotalNumberOfItemsInAllShipment(OrderItem orderItem);

        /// <summary>
        /// Gets a total number of already items which can be added to new shipments
        /// </summary>
        /// <param name="orderItem">Order item</param>
        /// <returns>Total number of already delivered items which can be added to new shipments</returns>
        public int GetTotalNumberOfItemsCanBeAddedToShipment(OrderItem orderItem);

        #endregion

        #region Order Sales

        /// <summary>
        /// Gets an order item
        /// </summary>
        /// <param name="orderItemId">Order item identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the Order item
        /// </returns>
        public Task<OrderItem> GetOrderItemByIdAsync(int orderItemId);

        /// <summary>
        /// Gets all order sales items as an array
        /// </summary>
        /// <param name="orderItemId">Order item identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the array of order sales item
        /// </returns>
        public Task<int[]> GetAllOrderSalesItemsAsync();

        /// <summary>
        /// Gets all order item sales items as an array
        /// </summary>
        /// <param name="orderItemId">Order item identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the array of order item sales items
        /// </returns>
        public Task<int[]> GetAllOrderItemSalesItemsAsync();

        /// <summary>
        /// Gets all shippable order sales items 
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the array of shippable order sales item
        /// </returns>
        public Task<IList<Order>> GetAllOrderSalesMappingAsync();

        /// <summary>
        /// Gets all shippable order item sales items 
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the array of shippable order item sales item
        /// </returns>
        public Task<IList<OrderItem>> GetAllOrderItemSalesMappingAsync();

        /// <summary>
        /// Gets all order sales items as an array
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the array of order item sales item
        /// </returns>
        public IList<Order> GetOrderSalesItemsbyOrderIds(int[] orderIds);

        /// <summary>
        /// Gets all order item sales items as an array
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the array of order item sales item
        /// </returns>
        public IList<OrderItem> GetOrderSalesItemsbyOrderItemIds(int[] orderItemIds);

        /// <summary>
        /// Gets all order sales items as a paged list
        /// </summary>
        /// <param name="orderIds">Order item identifier array</param> 
        /// <param name="paymentMethod">Payment method identifier</param> 
        /// <param name="fromDate">From date</param> 
        /// <param name="toDate">To date</param> 
        /// <param name="isPay">Order is payedr</param> 
        /// <param name="orderbyName">Order by name</param> 
        /// <param name="searchProductName">Search product name</param> 
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>/// 
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the paged list of order item sales item
        /// </returns>
        public Task<IPagedList<Order>> GetSalesOrderListAsync(int[] orderIds, string paymentMethod, DateTime? fromDate, DateTime? toDate,
                bool isPay, bool orderbyName, string searchProductName, int pageIndex = 0, int pageSize = int.MaxValue);

        /// <summary>
        /// Gets all order item sales items as a paged list
        /// </summary>
        /// <param name="orderItemIds">Order item identifier array</param> 
        /// <param name="paymentMethod">Payment method identifier</param> 
        /// <param name="fromDate">From date</param> 
        /// <param name="toDate">To date</param> 
        /// <param name="isPay">Order is payedr</param> 
        /// <param name="orderbyName">Order by name</param> 
        /// <param name="searchProductName">Search product name</param> 
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>/// 
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the paged list of order item sales item
        /// </returns>
        public Task<IList<OrderItem>> GetSalesOrderItemsListAsync(int[] orderItemIds, string paymentMethod, DateTime? fromDate, DateTime? toDate,
            bool isPay, bool orderbyName, string searchProductName, int pageIndex = 0, int pageSize = int.MaxValue);

        /// <summary>
        /// Gets customer billing email from order
        /// </summary>
        /// <param name="order">Order</param> 
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the email
        /// </returns>
        public Task<string> GetCustomerEmailDetailsAsync(Order order);

        /// <summary>
        /// Gets customer billing email from order item
        /// </summary>
        /// <param name="orderItem">OrderItem</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the email
        /// </returns>
        public Task<string> GetCustomerEmailDetailsAsync(OrderItem orderItem);

        /// <summary>
        /// Gets customer fullname from order
        /// </summary>
        /// <param name="order">Order</param> 
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the customer fullname
        /// </returns>
        public Task<string> GetCustomerFullNameDetailsAsync(Order order);

        /// <summary>
        /// Gets customer fullname from order item
        /// </summary>
        /// <param name="orderItem">OrderItem</param> 
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the customer fullname
        /// </returns>
        public Task<string> GetCustomerFullNameDetailsAsync(OrderItem orderItem);
        /// <summary>
        /// Gets edit order page URL
        /// </summary>
        /// <param name="orderId">Order identifier</param> 
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the URL
        /// </returns>
        public string GetEditOrderPageUrl(int orderId);
        /// <summary>
        /// Gets add shipment page URL
        /// </summary>
        /// <param name="orderId">Order</param> 
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the URL
        /// </returns>
        public string GetAddShipmntPageUrl(int orderId);

        /// <summary>
        /// Gets edit order page URL
        /// </summary>
        /// <param name="orderId">Order identifier</param> 
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the URL
        /// </returns>
        public string GetEditShipmentPageUrl(int orderId);

        /// <summary>
        /// Gets a list items of order
        /// </summary>
        /// <param name="orderId">Order identifier</param>
        /// <param name="isNotReturnable">Value indicating whether this product is returnable; pass null to ignore</param>
        /// <param name="isShipEnabled">Value indicating whether the entity is ship enabled; pass null to ignore</param>
        /// <param name="vendorId">Vendor identifier; pass 0 to ignore</param>
        /// <returns>Result</returns>
        public IList<OrderItem> GetOrderItems(int orderId, bool? isNotReturnable = null, bool? isShipEnabled = null, int vendorId = 0);

        /// <summary>
        /// Gets a list of order shipments
        /// </summary>
        /// <param name="orderId">Order identifier</param>
        /// <param name="shipped">A value indicating whether to count only shipped or not shipped shipments; pass null to ignore</param>
        /// <param name="vendorId">Vendor identifier; pass 0 to ignore</param>
        /// <returns>Result</returns>
        public IList<Shipment> GetShipmentsByOrderId(int orderId, bool? shipped = null, int vendorId = 0);

        /// <summary>
        /// Gets a shipment items of shipment
        /// </summary>
        /// <param name="shipmentId">Shipment identifier</param>
        /// <returns>Shipment items</returns>
        public IList<ShipmentItem> GetShipmentItemsByShipmentId(int shipmentId);

        /// <summary>
        /// Gets order shipping details
        /// </summary>
        /// <param name="order">Order identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the Shipment items
        /// </returns> 
        public Task<string> GetOrderShippingDetailsAsync(Order order);

        /// <summary>
        /// Gets order item shipping details
        /// </summary>
        /// <param name="orderItem">OrderItem identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the order items shipping details
        /// </returns> 
        public Task<List<Warehouse>> GetOrderItemShippingDetailsAsync(OrderItem orderItem);

        /// <summary>
        /// Gets flag to confirm order can be approved
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the flag
        /// </returns> 
        public bool CanMarkOrderAsApproved(Order order);

        /// <summary>
        /// Gets flag to confirm order has shipments
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the flag
        /// </returns> 
        public bool HasOrderShipment(Order order);

        /// <summary>
        /// Marks all orders as approved
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// </returns> 
        public Task MarkOrderAsApprovedAsync(Order order);

        /// <summary>
        /// Add order note
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="note">Note text</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public Task AddOrderNoteAsync(Order order, string note);

        /// <summary>
        /// Add order note for a shipment
        /// </summary>
        /// <param name="orderShipment">Shipment order</param>
        /// <param name="note">Note text</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public Task AddOrderNoteForShipmentAsync(Shipment orderShipment, string note);

        /// <summary>
        /// Updates the shipment item
        /// </summary>
        /// <param name="shipmentItem">Shipment item</param>
        public Task UpdateShipmentItemAsync(ShipmentItem shipmentItem);

        /// <summary>
        /// Gets the shipment item by Id
        /// </summary>
        /// <param name="shipmentItemId">Shipment identifier</param>
        public Task<ShipmentItem> GetShipmentItemByIdAsync(int shipmentItemId);

        /// <summary>
        /// Balance the given quantity in the warehouses.
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="warehouseId">Warehouse identifier</param>
        /// <param name="quantity">Quantity</param>
        public Task BalanceInventoryAsync(Product product, int warehouseId, int quantity);

        #endregion

    }
}