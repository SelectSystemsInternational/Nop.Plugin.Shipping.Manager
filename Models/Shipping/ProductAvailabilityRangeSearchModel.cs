using Nop.Web.Framework.Models;

namespace Nop.Plugin.Shipping.Manager.Models.Shipping
{
    /// <summary>
    /// Represents a product availability range search model
    /// </summary>
    public partial record ProductAvailabilityRangeSearchModel : BaseSearchModel
    {
        public int VendorId { get; set; }

    }
}