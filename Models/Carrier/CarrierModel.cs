using System.Collections.Generic;
using Nop.Web.Areas.Admin.Models.Common;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Nop.Plugin.Shipping.Manager.Models.Carrier
{
    /// <summary>
    /// Represents a carrier model
    /// </summary>
    public partial record CarrierModel : BaseNopEntityModel
    {
        #region Ctor

        public CarrierModel()
        {
            Address = new AddressModel();
            ActiveShippingRateComputationMethodSystemNames = new List<SelectListItem>();
        }

        #endregion

        #region Properties

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Vendor")]
        public string VendorName { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Name")]
        public string Name { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.AdminComment")]
        public string AdminComment { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Address")]
        public AddressModel Address { get; set; }

        [NopResourceDisplayName("Admin.Address.Fields.StateProvince")]
        public string StateProvinceName { get; set; }

        [NopResourceDisplayName("Admin.Address.Fields.County")]
        public string County { get; set; }

        [NopResourceDisplayName("Admin.Address.Fields.City")]
        public string City { get; set; }

        [NopResourceDisplayName("Admin.Address.Fields.PhoneNumber")]
        public string PhoneNumber { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.ShippingRateComputationMethodSystemName")]
        public string ShippingRateComputationMethodSystemName { get; set; }

        public IList<SelectListItem> ActiveShippingRateComputationMethodSystemNames { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.Active")]
        public bool Active { get; set; }

        #endregion
    }
}