using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Shipping.Manager.Models.Order
{
    /// <summary>
    /// Represents a shipment item model
    /// </summary>
    public partial record ShipmentItemModel : BaseNopEntityModel
    {
        #region Ctor

        public ShipmentItemModel()
        {
            AvailableWarehouses = new List<WarehouseInfo>();
        }

        #endregion

        #region Properties

        public int ShipmentItemId { get; set; }

        public int OrderItemId { get; set; }

        public int ProductId { get; set; }

        [NopResourceDisplayName("Admin.Orders.Shipments.Products.ProductName")]
        public string ProductName { get; set; }

        public string Sku { get; set; }

        public string AttributeInfo { get; set; }

        public string RentalInfo { get; set; }

        public bool ShipSeparately { get; set; }

        //weight of one item (product)
        [NopResourceDisplayName("Admin.Orders.Shipments.Products.ItemWeight")]
        public string ItemWeight { get; set; }

        [NopResourceDisplayName("Admin.Orders.Shipments.Products.ItemDimensions")]
        public string ItemDimensions { get; set; }

        public int QuantityToAdd { get; set; }

        [NopResourceDisplayName("Admin.Orders.Shipments.Products.QtyOrdered")]
        public int QuantityOrdered { get; set; }

        [NopResourceDisplayName("Admin.Orders.Shipments.Products.QtyShipped")]
        public int QuantityInThisShipment { get; set; }

        public int QuantityInAllShipments { get; set; }

        public int QuantityInShipment { get; set; }

        public string ShippedFromWarehouse { get; set; }

        public bool AllowToChooseWarehouse { get; set; }

        //used before a shipment is created
        public List<WarehouseInfo> AvailableWarehouses { get; set; }

        #endregion

        #region Nested Classes

        public record WarehouseInfo : BaseNopModel
        {
            public int WarehouseId { get; set; }
            public string WarehouseName { get; set; }
            public int StockQuantity { get; set; }
            public int ReservedQuantity { get; set; }
            public int PlannedQuantity { get; set; }
            public int ToShipQuatity { get; set; }
            public bool IsPreSelected { get; set; }
        }

        #endregion
    }
}