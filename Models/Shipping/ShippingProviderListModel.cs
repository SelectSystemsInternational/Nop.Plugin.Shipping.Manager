using Nop.Web.Framework.Models;

namespace Nop.Plugin.Shipping.Manager.Models.Shipping
{
    /// <summary>
    /// Represents a shipping provider list model
    /// </summary>
    public partial record ShippingProviderListModel : BasePagedListModel<ShippingProviderModel>
    {
    }
}