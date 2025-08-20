using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Shipping.Manager.Models.Order
{
    /// <summary>
    /// Represents a shipment model
    /// </summary>
    public record OrderShipmentModel : BaseNopEntityModel
    {
        #region Ctor

        public OrderShipmentModel()
        {
            ShipmentStatusEvents = new List<ShipmentStatusEventModel>();
            Items = new List<ShipmentItemModel>();
            AvailableShippingMethods = new List<SelectListItem>();
        }

        #endregion

        #region Properties

        [NopResourceDisplayName("Admin.Orders.Shipments.Id")]
        public override int Id { get; set; }

        public int OrderId { get; set; }

        [NopResourceDisplayName("Admin.Orders.Shipments.CustomOrderNumber")]
        public string CustomOrderNumber { get; set; }

        [NopResourceDisplayName("Admin.Orders.Shipments.TotalWeight")]
        public string TotalWeight { get; set; }

        [NopResourceDisplayName("Admin.Orders.Shipments.TrackingNumber")]
        public string TrackingNumber { get; set; }

        public string TrackingNumberUrl { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Orders.Shipments.ServicePointId")]
        public string ServicePointId { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Orders.Shipments.ServicePointPOBoxNumber")]
        public string ServicePointPOBox { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Orders.Shipments.ShippingMethod")]
        public int ShippingMethodId { get; set; }

        public IList<SelectListItem> AvailableShippingMethods { get; set; }

        [NopResourceDisplayName("Admin.Orders.Shipments.ShippedDate")]
        public string ShippedDate { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Orders.Shipments.CanShip")]
        public bool CanShip { get; set; }

        public DateTime? ShippedDateUtc { get; set; }

        [NopResourceDisplayName("Admin.Orders.Shipments.DeliveryDate")]
        public string DeliveryDate { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Orders.Shipments.CanDeliver")]
        public bool CanDeliver { get; set; }

        public DateTime? DeliveryDateUtc { get; set; }

        [NopResourceDisplayName("Admin.Orders.Shipments.AdminComment")]
        public string AdminComment { get; set; }

        public List<ShipmentItemModel> Items { get; set; }

        //[NopResourceDisplayName("Plugins.Shipping.Manager.Shipments.Packaging.Name")]
        //public int PackagingOptionId { get; set; }
        //public IList<SelectListItem> AvailablePackagingOptions { get; set; }

        public IList<ShipmentStatusEventModel> ShipmentStatusEvents { get; set; }

        public string BtnId { get; set; }

        #endregion
    }
}