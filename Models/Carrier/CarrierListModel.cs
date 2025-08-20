using Nop.Web.Framework.Models;

namespace Nop.Plugin.Shipping.Manager.Models.Carrier
{
    /// <summary>
    /// Represents a carrier list model
    /// </summary>
    public partial record CarrierListModel : BasePagedListModel<CarrierModel>
    {
    }
}