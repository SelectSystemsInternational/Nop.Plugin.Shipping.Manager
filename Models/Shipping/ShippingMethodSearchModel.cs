using Nop.Web.Framework.Models;

namespace Nop.Plugin.Shipping.Manager.Models.Shipping
{
    /// <summary>
    /// Represents a shipping method search model
    /// </summary>
    public partial record ShippingMethodSearchModel : BaseSearchModel
    {
        public int VendorId { get; set; }
    }
}