using Nop.Core.Caching;

namespace Nop.Plugin.Apollo.Integrator
{
    /// <summary>
    /// Represents constants for the Apollo Integrator Systems
    /// </summary>
    public static class ApolloIntegratorDefaults
    {

        #region Accounts 

        /// <summary>
        /// Gets a key pattern to clear cache
        /// </summary>
        public static string AccountsByPatternKey = "Nop.accounts.";

        /// <summary>
        /// Gets a key for Departments caching
        /// </summary>
        /// <remarks>
        /// {0} : Account Id
        /// </remarks>
        public static CacheKey AccountsByIdCacheKey => new CacheKey("Nop.accounts.id-{0}", AccountsByPatternKey);

        /// <remarks>
        /// Department all
        /// </remarks>
        public static CacheKey AccountsByAllKey = new CacheKey("Nop.accounts.all-{0}-{1}", AccountsByPatternKey);

        #endregion

        #region Carriers

        /// <summary>
        /// Gets a key pattern to clear cache
        /// </summary>
        public static string CarriersPrefixCacheKey => "Nop.carriers.";

        /// <summary>
        /// Gets a key for caching
        /// </summary>
        /// <remarks>
        /// {0} : warehouse ID
        /// </remarks>
        public static string CarriersByIdCacheKey => "Nop.warehouse.id-{0}";

        #endregion

        #region Companies

        /// <summary>
        /// Gets default prefix for customer
        /// </summary>
        public static string CompanyAttributePrefix => "company_attribute_";

        /// <summary>
        /// Gets a key for caching
        /// </summary>
        /// <remarks>
        /// {0} : customer attribute ID
        /// </remarks>
        public static CacheKey CompanyAttributeValuesByAttributeCacheKey => new("Nop.companyattributevalue.byattribute.{0}");

        /// <summary>
        /// Gets a name of generic attribute to store the value of 'CustomCustomerAttributes'
        /// </summary>
        public static string CustomCompanyAttributes => "CustomCompanyAttributes";

        /// <summary>
        /// Gets a key pattern to clear cache
        /// </summary>
        public static string CompaniesByPatternKey = "Nop.companies.";

        /// <summary>
        /// Gets a key for Departments caching
        /// </summary>
        /// <remarks>
        /// {0} : Account Id
        /// </remarks>
        public static CacheKey CompaniesByIdCacheKey => new CacheKey("Nop.companies.id-{0}", CompaniesByPatternKey);

        /// <remarks>
        /// Department all
        /// </remarks>
        public static CacheKey CompaniesByAllKey = new CacheKey("Nop.companies.all-{0}-{1}", CompaniesByPatternKey);

        #endregion

        #region Customer 

        /// <summary>
        /// Gets a name of generic attribute to store the value of 'AdminAreaCustomerScopeConfiguration'
        /// </summary>
        public static string AdminAreaCustomerScopeConfigurationAttribute => "AdminAreaCustomerScopeConfiguration";

        /// <summary>
        /// Gets a name of generic attribute to store the value of 'AdminAreaCustomerScopeConfiguration'
        /// </summary>
        public static string AdminAreaGroupCustomerScopeConfigurationAttribute => "AdminAreaGroupCustomerScopeConfiguration";

        /// <summary>
        /// Gets a name of generic attribute to store the value of 'SelectedPaymentMethod'
        /// </summary>
        public static string CustomerEntityGroup => "Customer Group";

        #endregion

        #region Departments

        /// <summary>
        /// Gets a name of generic attribute to store the value of 'Department'
        /// </summary>
        public static string CompanyDepartment => "CompanyDepartment";



        /// <summary>
        /// Gets a key pattern to clear cache
        /// </summary>
        public static string DepartmentsByPatternKey = "Nop.departments.";

        /// <summary>
        /// Gets a key for Departments caching
        /// </summary>
        /// <remarks>
        /// {0} : Department Id
        /// </remarks>
        public static CacheKey DepartmentsByIdCacheKey => new CacheKey("Nop.departments.id-{0}", DepartmentsByPatternKey);

        /// <remarks>
        /// Department all
        /// </remarks>
        public static CacheKey DepartmentsByAllKey = new CacheKey("Nop.departments.all-{0}-{1}", DepartmentsByPatternKey);

        #endregion

        #region Positions

        /// <summary>
        /// Gets a name of generic attribute to store the value of 'Position'
        /// </summary>
        public static string CompanyPosition => "CompanyPosition";

        /// <summary>
        /// Gets a key pattern to clear cache
        /// </summary>
        public static string PositionsByPatternKey = "Nop.positions.";

        /// <summary>
        /// Gets a key for Positions caching
        /// </summary>
        /// <remarks>
        /// {0} : Position Id
        /// </remarks>
        public static CacheKey PositionsByIdCacheKey => new CacheKey("Nop.positions.id-{0}", PositionsByPatternKey);

        /// <remarks>
        /// Position all
        /// </remarks>
        public static CacheKey PositionsByAllKey = new CacheKey("Nop.positions.all-{0}-{1}", PositionsByPatternKey);

        #endregion

        #region PaymentTerms

        /// <summary>
        /// Gets a name of generic attribute to store the value of 'Position'
        /// </summary>
        public static string PaymentTerms => "PaymentTerms";

        /// <summary>
        /// Gets a key pattern to clear cache
        /// </summary>
        public static string PaymentTermsByPatternKey = "Nop.paymentterms.";

        /// <summary>
        /// Gets a key for Positions caching
        /// </summary>
        /// <remarks>
        /// {0} : Position Id
        /// </remarks>
        public static CacheKey PaymentTermsByIdCacheKey => new CacheKey("Nop.paymentterms.id-{0}", PaymentTermsByPatternKey);

        /// <remarks>
        /// Position all
        /// </remarks>
        public static CacheKey PaymentTermsByAllKey = new CacheKey("Nop.paymentterms.all-{0}-{1}", PaymentTermsByPatternKey);

        #endregion

        #region Transactions 

        /// <summary>
        /// Gets a key pattern to clear cache
        /// </summary>
        public static string TransactionsByPatternKey = "Nop.transaction.";

        /// <summary>
        /// Gets a key for Departments caching
        /// </summary>
        /// <remarks>
        /// {0} : Account Id
        /// </remarks>
        public static CacheKey TransactionsByIdCacheKey => new CacheKey("Nop.transaction.id-{0}", AccountsByPatternKey);

        /// <remarks>
        /// Department all
        /// </remarks>
        public static CacheKey TransactionsByAllKey = new CacheKey("Nop.transaction.all-{0}-{1}", AccountsByPatternKey);

        #endregion

        #region Entity Groups

        /// <summary>
        /// Gets a key pattern to clear cache
        /// </summary>
        public static string EntityGroupPatternKey = "Nop.entitygroup.";

        /// <remarks>
        /// Entity group all
        /// {0} : customerId
        /// {1} : vendorId
        /// </remarks>
        public static CacheKey EntityGroupsByAllKey = new CacheKey("Nop.entitygroup.all-{0}-{1}", EntityGroupPatternKey);

        /// <summary>
        /// Gets a key for caching
        /// </summary>
        /// <remarks>
        /// {0} : entity Id
        /// {1} : key group
        /// {2} : key
        /// </remarks>
        public static CacheKey EntityGroupKey = new CacheKey("Nop.entitygroup.id.{0}-{1}-{2}", EntityGroupPatternKey);

        /// <summary>
        /// Gets a key for entity group member caching
        /// </summary>
        /// <remarks>
        /// {0} : groupKey
        /// {1} : keygroup
        /// {2} : storeId
        /// {3} : customerId
        /// {4} : vendorId
        /// {5} : warehouseId
        /// </remarks>
        public static CacheKey EntityGroupMemberKey = new CacheKey("Nop.entitygroup.member.{0}-{1}-{2}-{3}-{4}-{5}", EntityGroupPatternKey);

        /// <summary>
        /// Gets a entity group for entity caching
        /// </summary>
        /// <remarks>
        /// {0} : entityId
        /// {1} : keygroup
        /// {2} : storeId
        /// {3} : customerId
        /// {4} : vendorId
        /// {5} : warehouseId
        /// </remarks>
        public static CacheKey EntityGroupForEntityKey = new CacheKey("Nop.entitygroup.forentity.{0}-{1}-{2}-{3}-{4}-{5}", EntityGroupPatternKey);

        /// <summary>
        /// Gets a key for entity group members caching
        /// </summary>
        /// <remarks>
        /// {0} : groupKey
        /// {1} : groupKey
        /// {2} : keygroup
        /// {3} : vendorId 
        /// {4} : warehouseId
        /// {5} : storeId 
        /// </remarks>
        public static CacheKey EntityGroupMembersKey = new CacheKey("Nop.entitygroup.members.{0}-{1}-{2}-{3}-{4}-{5}", EntityGroupPatternKey);
       
        public static string EntityGroupMembersPatternKey = "Nop.entitygroupmembers.";

        /// <summary>
        /// Gets a key for caching
        /// </summary>
        /// <remarks>
        /// {0} : entity ID
        /// {1} : key group
        /// </remarks>
        public static string EntityGroupCacheKey => "Nop.genericattribute.{0}-{1}";

        /// <summary>
        /// Gets a key pattern to clear cache
        /// </summary>
        public static string EntityGroupPrefixCacheKey => "Nop.genericattribute.";

        #endregion

        #region Vendor


        /// <summary>
        /// Gets a name of generic attribute to store the value of 'AdminAreaVendorScopeConfiguration'
        /// </summary>
        public static string AdminAreaVendorScopeConfigurationAttribute => "AdminAreaVendorScopeConfiguration";

        /// <summary>
        /// Gets a name of generic attribute to store the value of 'AdminAreaVendorScopeConfiguration'
        /// </summary>
        public static string AdminAreaGroupVendorScopeConfigurationAttribute => "AdminAreaGroupVendorScopeConfiguration";

        #endregion

        #region System

        /// <summary>
        /// Gets a system name of 'apollo system' customer object
        /// </summary>
        public static string SystemCustomerName => "SystemManager";

        /// <summary>
        /// Gets a name of system credit identifier object
        /// </summary>
        public static string SystemCreditIdentifier => "SystemCreditIdentifier";

        /// <summary>
        /// Gets a name of system credit payment method
        /// </summary>
        public static string SystemCreditPaymentMethod => "Payments.OrderPay";

        /// <summary>
        /// Mollie payment method system name
        /// </summary>
        public static string SystemName => "Apollo.Manager";

        /// <summary>
        /// Gets a name of the view component to display buttons
        /// </summary>
        public const string BUTTONS_COMPONENT_NAME = "Apollo.Manager.Buttons";

        /// <summary>
        /// Gets a name of the view component to display customer uttons
        /// </summary>
        public const string CUSTOMER_MERGE_BUTTON_COMPONENT_NAME = "Apollo.Manager.Customer.Merge.Button";

        /// <summary>
        /// Gets a name of the view component to display customer uttons
        /// </summary>
        public const string COMPANY_ATTRIBUTES_COMPONENT_NAME = "CompanyAttributes";

        /// <summary>
        /// Gets the customer merge route name
        /// </summary>
        public static string CustomerMergeRouteName => "Apollo.Manager.Customer.Merge";

        /// <summary>
        /// Gets the select customer route name
        /// </summary>
        public static string SelectCustomerRouteName => "Apollo.Manager.Customer.SelectCustomer";

        /// <summary>
        /// Gets a name of generic attribute to store the value of 'CompanyImpersonated'
        /// </summary>
        public static string CompanyImpersonated => "CompanyImpersonated";

        #endregion

    }
}
