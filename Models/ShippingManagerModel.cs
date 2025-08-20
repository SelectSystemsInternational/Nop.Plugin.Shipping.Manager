using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Nop.Plugin.Shipping.Manager.Models
{
    public record ShippingManagerModel : BaseSearchModel
    {
        public ShippingManagerModel()
        {
            AvailableCountries = new List<SelectListItem>();
            AvailableStates = new List<SelectListItem>();
            AvailableShippingMethods = new List<SelectListItem>();
            AvailableStores = new List<SelectListItem>();
            AvailableWarehouses = new List<SelectListItem>();
            AvailableCarriers = new List<SelectListItem>();
        }

        public bool DisplayVendor { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.LimitMethodsToCreated")]
        public bool LimitMethodsToCreated { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.ReturnValidOptionsIfThereAreAny")]
        public bool ReturnValidOptionsIfThereAreAny { get; set; }

        public bool ShippingByWeightByTotalEnabled { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Store")]
        public int SearchStoreId { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Warehouse")]
        public int SearchWarehouseId { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Carrier")]
        public int SearchCarrierId { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.ShippingMethod")]
        public int SearchShippingMethodId { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Country")]
        public int SearchCountryId { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.StateProvince")]
        public int SearchStateProvinceId { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Zip")]
        public string SearchZip { get; set; }      
        
        public bool SearchActive { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.Manager.SearchActive")]

	    public IList<SelectListItem> AvailableCountries { get; set; }
        public IList<SelectListItem> AvailableStates { get; set; }
        public IList<SelectListItem> AvailableShippingMethods { get; set; }
        public IList<SelectListItem> AvailableStores { get; set; }
        public IList<SelectListItem> AvailableWarehouses { get; set; }
        public IList<SelectListItem> AvailableCarriers { get; set; }
    }
}
