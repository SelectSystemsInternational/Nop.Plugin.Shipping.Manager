using Nop.Web.Framework.Models;

namespace Nop.Plugin.Shipping.Manager.Models.Shipping
{
    /// <summary>
    /// Represents a dates and ranges search model
    /// </summary>
    public partial record DatesRangesSearchModel : BaseSearchModel
    {
        #region Ctor

        public DatesRangesSearchModel()
        {
            DeliveryDateSearchModel = new DeliveryDateSearchModel();
            ProductAvailabilityRangeSearchModel = new ProductAvailabilityRangeSearchModel();
            CutOffTimeSearchModel = new CutOffTimeSearchModel();
        }

        #endregion

        #region Properties

        public DeliveryDateSearchModel DeliveryDateSearchModel { get; set; }

        public ProductAvailabilityRangeSearchModel ProductAvailabilityRangeSearchModel { get; set; }

        public CutOffTimeSearchModel CutOffTimeSearchModel { get; set; }

        public int VendorId { get; set; }

    #endregion
}
}