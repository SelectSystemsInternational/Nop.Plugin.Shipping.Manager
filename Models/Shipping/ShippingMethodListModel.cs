using Nop.Web.Framework.Models;

namespace Nop.Plugin.Shipping.Manager.Models.Shipping
{
    /// <summary>
    /// Represents a shipping method list model
    /// </summary>
    public partial record ShippingMethodListModel : BasePagedListModel<ShippingMethodModel>
    {
    }
}