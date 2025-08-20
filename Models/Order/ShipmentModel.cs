using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;

using Nop.Web.Areas.Admin.Models.Orders;

namespace Nop.Plugin.Shipping.Manager.Models.Order
{
    /// <summary>
    /// Represents a shipment model
    /// </summary>
    public partial record ShipmentModel : BaseNopEntityModel
    {
        #region Ctor

        public ShipmentModel()
        {
            ShipmentStatusEvents = new List<ShipmentStatusEventModel>();
            Items = new List<ShipmentItemModel>();
            AvailableShippingMethods = new List<SelectListItem>();
            AvailablePackagingOptions = new List<SelectListItem>();
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

        public string LabelUrl { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Orders.Shipments.ShipmentId")]
        public string ShipmentId { get; set; }

        public string ArtifactUrl { get; set; }

        public string ManifestUrl { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Orders.Shipments.ShippingMethod")]
        public int ShippingMethodId { get; set; }

        public string ShippingMethodName { get; set; }

        public IList<SelectListItem> AvailableShippingMethods { get; set; }

        public string ShippingRateComputationMethodSystemName { get; set; }

        [NopResourceDisplayName("Admin.Orders.Shipments.ShippedDate")]
        public string ShippedDate { get; set; }

        public bool CanShip { get; set; }

        public DateTime? ShippedDateUtc { get; set; }

        [NopResourceDisplayName("Admin.Orders.Shipments.DeliveryDate")]
        public string DeliveryDate { get; set; }

        public bool CanDeliver { get; set; }

        public DateTime? DeliveryDateUtc { get; set; }

        [NopResourceDisplayName("Admin.Orders.Shipments.AdminComment")]
        public string AdminComment { get; set; }

        public List<ShipmentItemModel> Items { get; set; }

        public IList<ShipmentStatusEventModel> ShipmentStatusEvents { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Shipments.Packaging.Name")]
        public int PackagingOptionId { get; set; }
        public IList<SelectListItem> AvailablePackagingOptions { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Shipments.Packaging.Dimensions")]
        public string PackageDimensions { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Shipments.ScheduledShipDate")]
        [UIHint("DateTimeNullable")]
        public DateTime? ScheduledShipDate { get; set; }

        #endregion

    }
}