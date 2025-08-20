using Nop.Web.Framework.Models;

namespace Nop.Plugin.Shipping.Manager.Models.Shipping
{
    /// <summary>
    /// Represents a delivery date search model
    /// </summary>
    public partial record DeliveryDateSearchModel : BaseSearchModel
    {
        public int VendorId { get; set; }

    }
}