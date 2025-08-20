using System.Collections.Generic;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Shipping.Manager.Models.Order
{
    /// <summary>
    /// Represents a shipment item model
    /// </summary>
    public partial record OrderItemModel : BaseNopEntityModel
    {
        #region Ctor

        public OrderItemModel()
        {
            ProductName = string.Empty;
            AttributeInfo = string.Empty;
            RentalInfo = string.Empty;
        }

        #endregion

        #region Properties

        public int OrderItemId { get; set; }

        public int ProductId { get; set; }

        [NopResourceDisplayName("Admin.Orders.Shipments.Products.ProductName")]
        public string ProductName { get; set; }

        public string AttributeInfo { get; set; }

        public string RentalInfo { get; set; }

        public int Quantity { get; set; }

        public string Price { get; set; }

        public string Warehouse { get; set; }

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