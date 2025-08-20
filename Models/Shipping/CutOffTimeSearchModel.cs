using Nop.Web.Framework.Models;

namespace Nop.Plugin.Shipping.Manager.Models.Shipping
{
    /// <summary>
    /// Represents a cut off times search model
    /// </summary>
    public partial record CutOffTimeSearchModel : BaseSearchModel
    {
        public int VendorId { get; set; }

    }
}