using System.Collections.Generic;
using Nop.Web.Areas.Admin.Models.Vendors;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Apollo.Integrator.Models.EntityGroup
{
    /// <summary>
    /// Represents a store scope configuration model
    /// </summary>
    public partial record VendorScopeConfigurationModel : BaseNopModel
    {
        #region Ctor

        public VendorScopeConfigurationModel()
        {
            Vendors = new List<VendorModel>();
            VendorGroupMembers = new List<VendorModel>();
        }

        #endregion

        #region Properties

        public int VendorId { get; set; }

        public IList<VendorModel> Vendors { get; set; }

        public int GroupVendorId { get; set; }

        public IList<VendorModel> VendorGroupMembers { get; set; }

        public bool DisplayControl { get; set; }

        public bool DisplayControlVendorGroups { get; set; }

        #endregion
    }
}