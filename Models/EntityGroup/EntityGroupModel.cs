using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Apollo.Integrator.Models.EntityGroup
{
    public record EntityGroupModel : BaseNopEntityModel
    {
        public EntityGroupModel()
        {
            AvailableEntity = new List<SelectListItem>();
            AvailableStores = new List<SelectListItem>();
            AvailableWarehouses = new List<SelectListItem>();
            AvailableVendors = new List<SelectListItem>();
            KeyGroupList = new List<SelectListItem>();
            KeyList = new List<SelectListItem>();
        }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Store")]
        public int StoreId { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Store")]
        public string StoreName { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Warehouse")]
        public int WarehouseId { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Warehouse")]
        public string WarehouseName { get; set; }

        [NopResourceDisplayName("Admin.Catalog.Products.Fields.Vendor")]
        public int VendorId { get; set; }
        [NopResourceDisplayName("Admin.Catalog.Products.Fields.Vendor")]
        public string VendorName { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.KeyGroup")]
        public string KeyGroup { get; set; }
        public int KeyGroupId { get; set; }
        public IList<SelectListItem> KeyGroupList { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Entity")]
        public int EntityId { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Entity")]
        public string EntityName { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Key")]
        public string Key { get; set; }
        public int KeyId { get; set; }
        public IList<SelectListItem> KeyList { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Value")]
        public string Value { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Value")]
        public string ValueName { get; set; }

        public IList<SelectListItem> AvailableEntity { get; set; }
        public IList<SelectListItem> AvailableStores { get; set; }
        public IList<SelectListItem> AvailableWarehouses { get; set; }
        public IList<SelectListItem> AvailableVendors { get; set; }
    }
}