using Nop.Web.Framework.Models;

namespace Nop.Plugin.Shipping.Manager.Models.Order
{
    /// <summary>
    /// Represents a shipment item search model
    /// </summary>
    public partial record OrderItemSearchModel : BaseSearchModel
    {
        #region Properties

        public int OrderId { get; set; }

        #endregion
    }
}