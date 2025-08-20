using Nop.Web.Framework.Models;

namespace Nop.Plugin.Shipping.Manager.Models.Warehouse
{
    /// <summary>
    /// Represents a warehouse list model
    /// </summary>
    public partial record WarehouseListModel : BasePagedListModel<WarehouseModel>
    {
    }
}