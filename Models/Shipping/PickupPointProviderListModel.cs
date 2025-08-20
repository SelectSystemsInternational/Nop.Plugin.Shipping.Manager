using Nop.Web.Framework.Models;

namespace Nop.Plugin.Shipping.Manager.Models.Shipping
{
    /// <summary>
    /// Represents a pickup point provider list model
    /// </summary>
    public partial record PickupPointProviderListModel : BasePagedListModel<PickupPointProviderModel>
    {
    }
}