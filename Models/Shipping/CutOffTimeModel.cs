using System.Collections.Generic;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Shipping.Manager.Models.Shipping
{
    /// <summary>
    /// Represents a cut off time model
    /// </summary>
    public partial record CutOffTimeModel : BaseNopEntityModel, ILocalizedModel<CutOffTimeLocalizedModel>
    {
        #region Ctor

        public CutOffTimeModel()
        {
            Locales = new List<CutOffTimeLocalizedModel>();
            VendorName = string.Empty;
            Name = string.Empty;
        }

        #endregion

        #region Properties

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Vendor")]
        public string VendorName { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.CutOffTimes.Fields.Name")]
        public string Name { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.CutOffTimes.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        public IList<CutOffTimeLocalizedModel> Locales { get; set; }

        #endregion
    }

    public partial class CutOffTimeLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.CutOffTimes.Fields.Name")]
        public string Name { get; set; }
    }
}