using Nop.Web.Framework.Models;

namespace Nop.Plugin.Shipping.Manager.Models.Order
{
    /// <summary>
    /// Represents a shipment item list model
    /// </summary>
    public partial record ShipmentItemListModel : BasePagedListModel<ShipmentItemModel>
    {
    }
}