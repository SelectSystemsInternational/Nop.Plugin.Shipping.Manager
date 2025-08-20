using Nop.Web.Framework.Models;

namespace Nop.Plugin.Shipping.Manager.Models.Shipping
{
    /// <summary>
    /// Represents a delivery date list model
    /// </summary>
    public partial record DeliveryDateListModel : BasePagedListModel<DeliveryDateModel>
    {
    }
}