using System.Collections.Generic;
using Nop.Web.Areas.Admin.Models.Customers;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Apollo.Integrator.Models.EntityGroup
{

    public partial record CustomerList : BaseNopModel
    {
        #region Ctor

        public CustomerList()
        {
            EntityName = string.Empty;
        }

        #endregion

        #region Properties

        public int CustomerId { get; set; }

        public string EntityName { get; set; }

        #endregion
    }

    /// <summary>
    /// Represents a store scope configuration model
    /// </summary>
    public partial record CustomerScopeConfigurationModel : BaseNopModel
    {
        #region Ctor

        public CustomerScopeConfigurationModel()
        {
            Customers = new List<CustomerList>();
            CustomerGroupMembers = new List<CustomerList>();
        }

        #endregion

        #region Properties

        public bool IsSupervisor { get; set; }

        public int CustomerId { get; set; }

        public IList<CustomerList> Customers { get; set; }

        public int GroupCustomerId { get; set; }

        public IList<CustomerList> CustomerGroupMembers { get; set; }

        public bool DisplayControl { get; set; }

        public bool DisplayControlCustomerGroups { get; set; }

        #endregion
    }
}