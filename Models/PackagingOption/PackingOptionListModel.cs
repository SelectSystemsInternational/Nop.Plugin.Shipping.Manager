using Nop.Web.Framework.Models;

namespace Nop.Plugin.Shipping.Manager.Models.PackagingOption
{
    /// <summary>
    /// Represents a packaging option list model
    /// </summary>
    public partial record PackagingOptionListModel : BasePagedListModel<PackagingOptionModel>
    {
    }
}