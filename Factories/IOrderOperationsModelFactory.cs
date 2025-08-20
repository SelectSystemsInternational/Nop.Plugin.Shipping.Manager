using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;

using Nop.Plugin.Shipping.Manager.Models.Order;

namespace Nop.Plugin.Shipping.Manager.Factories
{
    /// <summary>
    /// Represents the order model factory
    /// </summary>
    public partial interface IOrderOperationsModelFactory
    {

        #region Utilities

        /// <summary>
        /// Prepare default item
        /// </summary>
        /// <param name="items">Available items</param>
        /// <param name="withSpecialDefaultItem">Whether to insert the first special item for the default value</param>
        /// <param name="defaultItemText">Default item text; pass null to use "All" text</param>
        /// <returns>A task that represents the asynchronous operation</returns> 
        public Task PrepareDefaultItemAsync(IList<SelectListItem> items, bool withSpecialDefaultItem, string defaultItemText = null);

        /// <summary>
        /// Prepare available carriers
        /// </summary>
        /// <param name="items">Warehouse items</param>
        /// <param name="withSpecialDefaultItem">Whether to insert the first special item for the default value</param>
        /// <param name="defaultItemText">Default item text; pass null to use default value of the default item text</param>
        /// <returns>A task that represents the asynchronous operation</returns> 
        public Task PrepareCarriersListAsync(IList<SelectListItem> items, bool withSpecialDefaultItem = true, string defaultItemText = null);

        #endregion

        #region Methods

        /// <summary>
        /// Prepare shipment item model
        /// </summary>
        /// <param name="model">Shipment item model</param>
        /// <param name="orderItem">Order item</param>
        /// <returns>A task that represents the asynchronous operation</returns>  
        public Task PrepareShipmentItemModelAsync(ShipmentItemModel model, OrderItem orderItem);

        /// <summary>
        /// Prepare shipment status event models
        /// </summary>
        /// <param name="models">List of shipment status event models</param>
        /// <param name="shipment">Shipment</param>
        /// <returns>A task that represents the asynchronous operation</returns> 
        public Task PrepareShipmentStatusEventModelsAsync(IList<ShipmentStatusEventModel> models, Shipment shipment);

        /// <summary>
        /// Prepare shipment search model
        /// </summary>
        /// <param name="searchModel">Shipment search model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipment search model
        /// </returns>
        public Task<ShipmentSearchModel> PrepareShipmentSearchModelAsync(ShipmentSearchModel searchModel);

        /// <summary>
        /// Prepare paged shipment list model
        /// </summary>
        /// <param name="searchModel">Shipment search model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the Shipment list model
        /// </returns>
        public Task<ShipmentListModel> PrepareShipmentListModelAsync(ShipmentSearchModel searchModel);

        /// <summary>
        /// Prepare shipment model
        /// </summary>
        /// <param name="model">Shipment model</param>
        /// <param name="shipment">Shipment</param>
        /// <param name="order">Order</param>
        /// <param name="excludeProperties">Whether to exclude populating of some properties of model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipment model
        /// </returns>
        public Task<ShipmentModel> PrepareShipmentModelAsync(ShipmentModel model, Shipment shipment, Order order, bool excludeProperties = false);

        /// <summary>
        /// Prepare paged shipment item list model
        /// </summary>
        /// <param name="searchModel">Shipment item search model</param>
        /// <param name="shipment">Shipment</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipment item list model
        /// </returns>
        public Task<ShipmentItemListModel> PrepareShipmentItemListModelAsync(ShipmentItemSearchModel searchModel, Shipment shipment);

        #endregion

    }
}