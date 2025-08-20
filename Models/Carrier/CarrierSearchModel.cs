using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Shipping.Manager.Models.Carrier
{
    /// <summary>
    /// Represents a carrier search model
    /// </summary>
    public partial record CarrierSearchModel : BaseSearchModel
    {
        [NopResourceDisplayName("Plugins.Shipping.Manager.SearchName")]
        public string SearchName { get; set; }

        public int VendorId { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.SearchActive")]
        public bool Active { get; set; }
    }
}
