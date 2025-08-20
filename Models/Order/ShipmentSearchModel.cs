using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Shipping.Manager.Models.Order
{
    /// <summary>
    /// Represents a shipment search model
    /// </summary>
    public partial record ShipmentSearchModel : BaseSearchModel
    {
        #region Ctor

        public ShipmentSearchModel()
        {
            AvailableCustomers = new List<SelectListItem>();
            AvailableShippingAddress = new List<SelectListItem>();
            AvailableCountries = new List<SelectListItem>();
            AvailableStates = new List<SelectListItem>();
            AvailableWarehouses = new List<SelectListItem>();
            AvailableCarriers = new List<SelectListItem>();
            AvailableShippingMethods = new List<SelectListItem>();
            AvailableVendorGroups = new List<SelectListItem>();

            ShipmentItemSearchModel = new ShipmentItemSearchModel();
        }

        #endregion

        #region Properties

        [NopResourceDisplayName("Admin.Orders.Shipments.List.StartDate")]
        [UIHint("DateNullable")]
        public DateTime? StartDate { get; set; }

        [NopResourceDisplayName("Admin.Orders.Shipments.List.EndDate")]
        [UIHint("DateNullable")]
        public DateTime? EndDate { get; set; }

        [NopResourceDisplayName("Admin.Orders.Shipments.List.TrackingNumber")]
        public string TrackingNumber { get; set; }

        public IList<SelectListItem> AvailableCustomers { get; set; }

        [NopResourceDisplayName("Admin.Orders.Fields.Customer")]
        public int CustomerId { get; set; }

        public IList<SelectListItem> AvailableShippingAddress { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.ShippingAddress")]
        public int ShippingAddressId { get; set; }

        public IList<SelectListItem> AvailableCountries { get; set; }

        [NopResourceDisplayName("Admin.Orders.Shipments.List.Country")]
        public int CountryId { get; set; }

        public IList<SelectListItem> AvailableStates { get; set; }

        [NopResourceDisplayName("Admin.Orders.Shipments.List.StateProvince")]
        public int? StateProvinceId { get; set; }

        [NopResourceDisplayName("Admin.Orders.Shipments.List.County")]
        public string County { get; set; }

        [NopResourceDisplayName("Admin.Orders.Shipments.List.City")]
        public string City { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.DontDisplayShipped")]
        public bool DontDisplayShipped { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.DontDisplayDelivered")]
        public bool DontDisplayDelivered { get; set; }

        [NopResourceDisplayName("Admin.Orders.Shipments.List.Warehouse")]
        public int WarehouseId { get; set; }

        public IList<SelectListItem> AvailableWarehouses { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.CarrierName")]
        public int CarrierId { get; set; }

        public IList<SelectListItem> AvailableCarriers { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.ShippingMethod")]
        public int ShippingMethodId { get; set; }

        public string ShippingMethod { get; set; }

        public IList<SelectListItem> AvailableShippingMethods { get; set; }


        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.VendorGroup")]
        public int VendorGroupId { get; set; }

        public IList<SelectListItem> AvailableVendorGroups { get; set; }

        public ShipmentItemSearchModel ShipmentItemSearchModel { get; set; }

        #endregion
    }
}
