using Nop.Web.Framework.Models;

namespace Nop.Plugin.Shipping.Manager.Models.Order
{
    /// <summary>
    /// Represents a shipment list model
    /// </summary>
    public partial record ShipmentListModel : BasePagedListModel<ShipmentModel>
    {
    }
}