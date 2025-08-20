using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;
using DocumentFormat.OpenXml.Bibliography;

namespace Nop.Plugin.Shipping.Manager.Models
{
    public record ShippingManagerByWeightByTotalModel : BaseNopEntityModel
    {
        public ShippingManagerByWeightByTotalModel()
        {
            AvailableStores = new List<SelectListItem>();
            AvailableVendors = new List<SelectListItem>();
            AvailableCountries = new List<SelectListItem>();
            AvailableStates = new List<SelectListItem>();
            AvailableShippingMethods = new List<SelectListItem>();
            AvailableWarehouses = new List<SelectListItem>();
            AvailableCarriers = new List<SelectListItem>();
            AvailableCutOffTime = new List<SelectListItem>();
            AvailableSendFromAddress = new List<SelectListItem>();
        }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Active")]
        public bool Active { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Store")]
        public int StoreId { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Store")]
        public string StoreName { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Vendor")]
        public int VendorId { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Vendor")]
        public string VendorName { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Warehouse")]
        public int WarehouseId { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Warehouse")]
        public string WarehouseName { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Carrier")]
        public int CarrierId { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Carrier")]
        public string CarrierName { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.CutOffTime")]
        public int CutOffTimeId { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.CutOffTime")]
        public string CutOffTimeName { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Country")]
        public int CountryId { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Country")]
        public string CountryName { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.StateProvince")]
        public int StateProvinceId { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.StateProvince")]
        public string StateProvinceName { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Zip")]
        public string Zip { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.ShippingMethod")]
        public int ShippingMethodId { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.ShippingMethod")]
        public string ShippingMethodName { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.WeightFrom")]
        public decimal WeightFrom { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.WeightTo")]
        public decimal WeightTo { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.CalculateCubicWeight")]
        public bool CalculateCubicWeight { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.CubicWeightFactor")]
        public decimal CubicWeightFactor { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.OrderSubtotalFrom")]
        public decimal OrderSubtotalFrom { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.OrderSubtotalTo")]
        public decimal OrderSubtotalTo { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.AdditionalFixedCost")]
        public decimal AdditionalFixedCost { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.PercentageRateOfSubtotal")]
        public decimal PercentageRateOfSubtotal { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.RatePerWeightUnit")]
        public decimal RatePerWeightUnit { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.LowerWeightLimit")]
        public decimal LowerWeightLimit { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.FriendlyName")]
        public string FriendlyName { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.TransitDays")]
        public int? TransitDays { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.SendFromAddress")]
        public int SendFromAddressId { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Description")]
        public string Description { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.DataHtml")]
        public string DataHtml { get; set; }

        public string PrimaryStoreCurrencyCode { get; set; }
        public string BaseWeightIn { get; set; }

        public IList<SelectListItem> AvailableStores { get; set; }
        public IList<SelectListItem> AvailableVendors { get; set; }
        public IList<SelectListItem> AvailableCountries { get; set; }
        public IList<SelectListItem> AvailableStates { get; set; }
        public IList<SelectListItem> AvailableShippingMethods { get; set; }
        public IList<SelectListItem> AvailableWarehouses { get; set; }
        public IList<SelectListItem> AvailableCarriers { get; set; }
        public IList<SelectListItem> AvailableCutOffTime { get; set; }
        public IList<SelectListItem> AvailableSendFromAddress { get; set; }

        public bool UseSendFromAddress { get; set; }

        public string BtnId { get; set; }
    }
}
