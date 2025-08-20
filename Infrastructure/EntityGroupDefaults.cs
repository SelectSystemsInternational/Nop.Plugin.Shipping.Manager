
namespace Nop.Plugin.Shipping.Manager
{
    /// <summary>
    /// Represents constants of the "Fixed or by weight" shipping plugin
    /// </summary>
    public static class ApolloIntegratorDefaults
    {

        #region Shipping Manager 

        /// <summary>
        /// Gets a name of generic attribute to store the value of 'SelectedPaymentMethod'
        /// </summary>
        public static string CustomerEntityGroup => "Customer Group";

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

        #region Entity Groups

        /// <summary>
        /// Gets a name of generic attribute to store the value of 'AdminAreaStoreScopeConfiguration'
        /// </summary>
        public static string AdminAreaVendorScopeConfigurationAttribute => "AdminAreaVendorScopeConfiguration";

        /// <summary>
        /// Gets a name of generic attribute to store the value of 'AdminAreaStoreScopeConfiguration'
        /// </summary>
        public static string AdminAreaGroupVendorScopeConfigurationAttribute => "AdminAreaGroupVendorScopeConfiguration";

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
    }

    public class EntityGroupTypes
    {
        public enum KeyGroup { EntityGroup, Carrier, Category, Customer, CutOffTime, 
            DeliveryDate, Product, ProductAvailabilityRange, 
            ShippingMethod, Vendor, Warehouse,  };

        public enum Key { Member, CarrierGroup, CategoryGroup, CustomerGroup, CutOffTimeGroup, 
            DeliveryDateGroup, ProductGroup, ProductAvailabilityRangeGroup, 
            ShippingMethodGroup, VendorGroup, WarehouseGroup };

    };

}
