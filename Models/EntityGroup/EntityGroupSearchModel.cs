using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Nop.Plugin.Apollo.Integrator.Models.EntityGroup
{
    public record EntityGroupSearchModel : BaseSearchModel
    {
        public EntityGroupSearchModel()
        {
            AvailableEntity = new List<SelectListItem>();
            AvailableStores = new List<SelectListItem>();
            AvailableWarehouses = new List<SelectListItem>();
            AvailableVendors = new List<SelectListItem>();
            KeyGroupList = new List<SelectListItem>();
            KeyList = new List<SelectListItem>();
        }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.LimitMethodsToCreated")]
        public bool LimitMethodsToCreated { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.ReturnValidOptionsIfThereAreAny")]
        public bool ReturnValidOptionsIfThereAreAny { get; set; }

        public bool ShippingByWeightByTotalEnabled { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Store")]
        public int SearchStoreId { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Warehouse")]
        public int SearchWarehouseId { get; set; }

        [NopResourceDisplayName("Admin.Catalog.Products.Fields.Vendor")]
        public int SearchVendorId { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.KeyGroup")]
        public int SearchKeyGroupId { get; set; }
        public string SearchKeyGroup { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Entity")]
        public int SearchEntityId { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Key")]
        public int SearchKeyId { get; set; }
        public string SearchKey { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Value")]
        public string SearchValue { get; set; }
        
        public IList<SelectListItem> AvailableEntity { get; set; }
        public IList<SelectListItem> AvailableStores { get; set; }
        public IList<SelectListItem> AvailableWarehouses { get; set; }
        public IList<SelectListItem> AvailableVendors { get; set; }
        public IList<SelectListItem> KeyGroupList { get; set; }
        public IList<SelectListItem> KeyList { get; set; }
    }
}