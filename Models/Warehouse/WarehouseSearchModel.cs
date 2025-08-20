using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Shipping.Manager.Models.Warehouse
{
    /// <summary>
    /// Represents a warehouse search model
    /// </summary>
    public partial record WarehouseSearchModel : BaseSearchModel
    {
        [NopResourceDisplayName("Plugins.Shipping.Manager.SearchName")]
        public string SearchName { get; set; }

        public int VendorId { get; set; }
    }
}