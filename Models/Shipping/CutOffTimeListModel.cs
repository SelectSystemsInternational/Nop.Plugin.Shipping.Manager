using Nop.Web.Framework.Models;

namespace Nop.Plugin.Shipping.Manager.Models.Shipping
{
    /// <summary>
    /// Represents a cut off time list model
    /// </summary>
    public partial record CutOffTimeListModel : BasePagedListModel<CutOffTimeModel>
    {
    }
}