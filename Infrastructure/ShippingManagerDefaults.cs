using Nop.Core.Caching;

using Nop.Core.Domain.Shipping;
using Nop.Plugin.Shipping.Manager.Domain;

namespace Nop.Plugin.Shipping.Manager
{
    /// <summary>
    /// Represents constants of the "Fixed or by weight" shipping plugin
    /// </summary>
    public static class ShippingManagerDefaults
    {

        /// <summary>
        /// Shipping manager system key
        /// </summary>
        public static string SystemKey => "92fea131-82ec-4843-a0e6-e262dd9f4837";

        /// <summary>
        /// Shipping manager public key
        /// </summary>
        public static string PublicKey => "4864cd50-6e81-4776-9ae8-4acc075f1729";

        #region SendCloud

        /// <summary>
        /// Gets a name of generic attribute to store the value of 'SelectedShippingOption'
        /// </summary>
        public static string SendCloudSelectedServicePoint => "SendCloudSelectedServicePoint";

        /// <summary>
        /// Sendcloud shipping method system name
        /// </summary>
        public static string SendCloudSystemName => "Shipping.Sendcloud";

        /// <summary>
        /// Sendcloud shipping method system name
        /// </summary>
        public static string AramexSystemName => "Shipping.Aramex";

        /// <summary>
        /// Australia Post shipping method system name
        /// </summary>
        public static string AustraliaPostSystemName => "Shipping.AustraliaPost";

        /// <summary>
        /// Australia Post shipping method system name
        /// </summary>
        public static string CanadaPostSystemName => "Shipping.CanadaPost";

        #endregion

        #region Shipping Manager 

        /// <summary>
        /// Mollie payment method system name
        /// </summary>
        public static string SystemName => "Shipping.Manager";

        /// <summary>
        /// The key of the settings to save fixed rate of the shipping method
        /// </summary>
        public const string FIXED_RATE_SETTINGS_KEY = "ShippingRateComputationMethod.FixedByWeightByTotal.Rate.ShippingMethodId{0}_{1}";

        /// <summary>
        /// The key of the settings to save transit days of the shipping method
        /// </summary>
        public const string TRANSIT_DAYS_SETTINGS_KEY = "ShippingRateComputationMethod.FixedByWeightByTotal.TransitDays.ShippingMethodId{0}_{1}";

        /// <summary>
        /// The key of the settings to save fixed rate of the shipping method
        /// </summary>
        public const string MATERIALS_MANAGEMENT_SETTINGS_ENABLED = "MaterialsManagementSettings.Enabled";

        /// <summary>
        /// The key of the settings to save fixed rate of the shipping method
        /// </summary>
        public const string CURRENT_SHIPPING_METHOD_SELECTOR = "Shipping.Manager.CurrentShippingMethod";

        #endregion

        #region Carriers

        /// <summary>
        /// Gets a key pattern to clear cache
        /// </summary>
        public const string CarriersByPatternKey = "Nop.carriers.";

        /// <summary>
        /// Gets a key for Carriers caching
        /// </summary>
        /// <remarks>
        /// {0} : Carrier Id
        /// </remarks>
        public static CacheKey CarriersByIdCacheKey => new CacheKey("Nop.carriers.id-{0}", CarriersByPatternKey);

        /// <remarks>
        /// Carrier all
        /// </remarks>
        public static CacheKey CarriersByAllKey = new CacheKey("Nop.carriers.all-{0}", CarriersByPatternKey);

        #endregion

        #region Cut Off Time

        /// <summary>
        /// Gets a key pattern to clear cache
        /// </summary>
        public const string CutOffTimeByPatternKey = "Nop.cutofftime.";

        /// <summary>
        /// Gets a key for Carriers caching
        /// </summary>
        /// <remarks>
        /// {0} : Carrier Id
        /// </remarks>
        public static CacheKey CutOffTimeByIdCacheKey => new CacheKey("Nop.cutofftime.id-{0}", CutOffTimeByPatternKey);

        /// <remarks>
        /// Carrier all
        /// </remarks>
        public static CacheKey CutOffTimeByAllKey = new CacheKey("Nop.cutofftime.all-{0}", CutOffTimeByPatternKey);

        #endregion

        #region Delivery Dates

        /// <summary>
        /// Gets a key pattern to clear cache
        /// </summary>
        public const string DeliveryDatesByPatternKey = "Nop.deliverydates.";

        /// <summary>
        /// Gets a key for Carriers caching
        /// </summary>
        /// <remarks>
        /// {0} : Carrier Id
        /// </remarks>
        public static CacheKey DeliveryDatesByIdCacheKey => new CacheKey("Nop.deliverydates.id-{0}", DeliveryDatesByPatternKey);

        /// <remarks>
        /// Carrier all
        /// </remarks>
        public static CacheKey DeliveryDatesByAllKey = new CacheKey("Nop.deliverydates.all-{0}", DeliveryDatesByPatternKey);

        #endregion

        #region Entity Groups

        /// <summary>
        /// Gets a key pattern to clear cache
        /// </summary>
        public const string EntityGroupPatternKey = "Nop.entitygroup.";

        /// <remarks>
        /// Entity group all
        /// </remarks>
        public static CacheKey EntityGroupsByAllKey = new CacheKey("Nop.entitygroup.all-{0}", EntityGroupPatternKey);

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
        /// {2} : vendorId 
        /// {3} : warehouseId
        /// {4} : storeId 
        /// </remarks>
        public static CacheKey EntityGroupMemberKey = new CacheKey("Nop.entitygroupmember.{0}-{1}-{2}-{3}-{4}", EntityGroupPatternKey);

        /// <summary>
        /// Gets a entity group for entity caching
        /// </summary>
        /// <remarks>
        /// {0} : entityId
        /// {1} : keygroup
        /// {2} : storeId
        /// {3} : vendorId
        /// </remarks>
        public static CacheKey EntityGroupForEntityKey = new CacheKey("Nop.entitygroupforentity.{0}-{1}-{2}-{3}", EntityGroupPatternKey);

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
        public static CacheKey EntityGroupMembersKey = new CacheKey("Nop.entitygroupmembers.{0}-{1}-{2}-{3}-{4}-{5}", EntityGroupMembersPatternKey);
        public const string EntityGroupMembersPatternKey = "Nop.entitygroupmembers.";

        #endregion

        #region PackagingOptions

        /// <summary>
        /// Gets a key pattern to clear cache
        /// </summary>
        public const string PackagingOptionsByPatternKey = "Nop.packagingoptions.";

        /// <summary>
        /// Gets a key for PackagingOptions caching
        /// </summary>
        /// <remarks>
        /// {0} : PackagingOptions Id
        /// </remarks>
        public static CacheKey PackagingOptionsByIdCacheKey => new CacheKey("Nop.packagingoptions.id-{0}", PackagingOptionsByPatternKey);

        /// <remarks>
        /// Carrier all
        /// </remarks>
        public static CacheKey PackagingOptionsByAllKey = new CacheKey("Nop.packagingoptions.all-{0}", PackagingOptionsByPatternKey);

        #endregion

        #region Product Availability Ranges

        /// <summary>
        /// Gets a key pattern to clear cache
        /// </summary>
        public const string ProductAvailabilityRangesByPatternKey = "Nop.productavailabilityranges.";

        /// <summary>
        /// Gets a key for Carriers caching
        /// </summary>
        /// <remarks>
        /// {0} : Carrier Id
        /// </remarks>
        public static CacheKey ProductAvailabilityRangesByIdCacheKey => new CacheKey("Nop.productavailabilityranges.id-{0}", ProductAvailabilityRangesByPatternKey);

        /// <remarks>
        /// Carrier all
        /// </remarks>
        public static CacheKey ProductAvailabilityRangesByAllKey = new CacheKey("Nop.productavailabilityranges.all-{0}", ProductAvailabilityRangesByPatternKey);

        #endregion

        #region Shipping Methods

        /// <remarks>
        /// Warehouses all
        /// </remarks>
        public static CacheKey ShippingMethodsByVendorAllKey = new CacheKey("Nop.shippingmethod.all.{0}-{1}", NopEntityCacheDefaults<ShippingMethod>.AllPrefix);

        #endregion

        #region Warehouses

        /// <summary>
        /// Gets a key pattern to clear cache
        /// </summary>
        public const string WarehousesByPatternKey = "Nop.warehouses.";

        /// <summary>
        /// Gets a key for Warehouses caching
        /// </summary>
        /// <remarks>
        /// {0} : Warehouse Id
        /// </remarks>
        public static CacheKey WarehousesByIdCacheKey => new CacheKey("Nop.warehouses.id-{0}", WarehousesByPatternKey);

        /// <remarks>
        /// Warehouses all
        /// </remarks>
        public static CacheKey WarehousesByAllKey = new CacheKey("Nop.warehouses.all-{0}", WarehousesByPatternKey);

        #endregion

        #region Service Points

        /// <summary>
        /// Sendcloud Service Point Routes 
        /// </summary>

        public static string CheckoutBillingAddressRouteName => "CheckoutBillingAddress";

        public static string CheckoutShippingMethodRouteName => "CheckoutShippingMethod";

        public static string CheckoutCompleted => "CheckoutCompleted";

        public static string OnePageCheckoutRouteName => "CheckoutOnePage";

        public static string RealOnePageCheckoutRouteName => "RealOnePageCheckout";

        /// <summary>
        /// Path to RealOnePageCheckout js script
        /// </summary>
        public static string RealOnePageCheckoutScriptPath => "~/Plugins/SSI.Shipping.Manager/Content/Scripts/RealOnePageCheckout.js";

        /// <summary>
        /// Path to CheckoutShippingMethod js script
        /// </summary>
        public static string SendcloudScriptPath => "https://embed.sendcloud.sc/spp/1.0.0/api.min.js";
        public static string CheckoutShippingMethodScriptPath => "~/Plugins/SSI.Shipping.Manager/Content/Scripts/CheckoutShippingMethod.js";

        public static string CheckoutOpcShippingMethodScriptPath => "~/Plugins/SSI.Shipping.Manager/Content/Scripts/CheckoutOpcShippingMethod.js";

        /// <summary>
        /// Path to CheckoutShippingMethod js script
        /// </summary>
        public static string CheckoutCompletedScriptPath => "~/Plugins/SSI.Shipping.Manager/Content/Scripts/CheckoutCompleted.js";

        /// <summary>
        /// Path to Checkout Shipping Method css styles
        /// </summary>
        public static string EmbeddedFieldsStylePath => "~/Plugins/SSI.Shipping.Manager/Content/css/Checkout.css";

        #endregion

    }
}
