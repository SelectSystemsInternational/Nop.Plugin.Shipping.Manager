using Nop.Web.Framework.Models;

namespace Nop.Plugin.Shipping.Manager.Models.Shipping
{
    /// <summary>
    /// Represents a product availability range list model
    /// </summary>
    public partial record ProductAvailabilityRangeListModel : BasePagedListModel<ProductAvailabilityRangeModel>
    {
    }
}