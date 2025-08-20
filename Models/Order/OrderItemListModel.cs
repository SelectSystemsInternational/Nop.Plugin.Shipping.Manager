using Nop.Web.Framework.Models;

namespace Nop.Plugin.Shipping.Manager.Models.Order
{
    /// <summary>
    /// Represents a order item list model
    /// </summary>
    public partial record OrderItemListModel : BasePagedListModel<OrderItemModel>
    {
    }
}