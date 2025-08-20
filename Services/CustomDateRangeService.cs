using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Shipping;
using Nop.Data;
using Nop.Services.Shipping.Date;

using Nop.Plugin.Apollo.Integrator.Domain;
using Nop.Plugin.Apollo.Integrator.Services;

namespace Nop.Plugin.Shipping.Manager.Services
{
    /// <summary>
    /// Represents the date range service
    /// </summary>
    public partial class CustomDateRangeService : DateRangeService
    {

        #region Fields

        protected readonly IStoreContext _storeContext;
        protected readonly IStaticCacheManager _staticCacheManager;
        protected readonly IEntityGroupService _entityGroupService;
        protected readonly IRepository<EntityGroup> _entityGroupRepository;

        #endregion

        #region Ctor

        public CustomDateRangeService(IStoreContext storeContext, 
            IStaticCacheManager staticCacheManager,
            IRepository<DeliveryDate> deliveryDateRepository,
            IRepository<ProductAvailabilityRange> productAvailabilityRangeRepository,
            IEntityGroupService entityGroupService,
            IRepository<EntityGroup> entityGroupRepository) : base(
                deliveryDateRepository,
                productAvailabilityRangeRepository)
        {
            _storeContext = storeContext;
            _staticCacheManager = staticCacheManager;
            _entityGroupService = entityGroupService;
            _entityGroupRepository = entityGroupRepository;
        }

        #endregion

        #region Methods

        #region Delivery dates

        /// <summary>
        /// Get all delivery dates
        /// </summary>
        /// <returns>Delivery dates</returns>
        public override async Task<IList<DeliveryDate>> GetAllDeliveryDatesAsync()
        {
            int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

            var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(ShippingManagerDefaults.DeliveryDatesByAllKey, vendorId);

            var query = await _deliveryDateRepository.GetAllAsync(query =>
            {
                return from d in _deliveryDateRepository.Table
                       orderby d.DisplayOrder, d.Name
                       select d;
            }, cache => cacheKey);

            if (vendorId != 0)
                query = (from d in query
                         join eg in _entityGroupRepository.Table on d.Id equals eg.EntityId
                         where eg.VendorId == vendorId &&
                               eg.KeyGroup == "DeliveryDate"
                         orderby d.DisplayOrder, d.Name
                         select d).ToList();

            return query.ToList();
        }

        /// <summary>
        /// Insert a delivery date
        /// </summary>
        /// <param name="deliveryDate">Delivery date</param>
        public override async Task InsertDeliveryDateAsync(DeliveryDate deliveryDate)
        {
            await base.InsertDeliveryDateAsync(deliveryDate);

            await _staticCacheManager.RemoveByPrefixAsync(ShippingManagerDefaults.DeliveryDatesByPatternKey);

            //Update entity groups
            await _entityGroupService.CreateOrUpdateEntityGroupingAsync<DeliveryDate>(deliveryDate);
        }

        /// <summary>
        /// Update the delivery date
        /// </summary>
        /// <param name="deliveryDate">Delivery date</param>
        public override async Task UpdateDeliveryDateAsync(DeliveryDate deliveryDate)
        {
            await base.UpdateDeliveryDateAsync(deliveryDate);

            await _staticCacheManager.RemoveByPrefixAsync(ShippingManagerDefaults.DeliveryDatesByPatternKey);

            //Update entity groups
            await _entityGroupService.CreateOrUpdateEntityGroupingAsync<DeliveryDate>(deliveryDate);
        }

        /// <summary>
        /// Delete a delivery date
        /// </summary>
        /// <param name="deliveryDate">The delivery date</param>
        public override async Task DeleteDeliveryDateAsync(DeliveryDate deliveryDate)
        {
            var storeId = await _entityGroupService.GetActiveStoreScopeConfiguration();

            //Get vendor scope
            int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

            await _entityGroupService.DeleteEntityGroupMemberAsync<DeliveryDate>(deliveryDate, "Member", vendorId: vendorId);

            await _staticCacheManager.RemoveByPrefixAsync(ShippingManagerDefaults.DeliveryDatesByPatternKey);

            var entityGroups = _entityGroupService.GetAllEntityGroups(_entityGroupService.GetEntityKeyGroup(deliveryDate), deliveryDate.Id, "Member", null, storeId);
            if (entityGroups.Count == 0)
                await base.DeleteDeliveryDateAsync(deliveryDate);
        }

        #endregion

        #region Product availability ranges

        /// <summary>
        /// Get all product availability ranges
        /// </summary>
        /// <returns>Product availability ranges</returns>
        public override async Task<IList<ProductAvailabilityRange>> GetAllProductAvailabilityRangesAsync()
        {
            int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

            var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(ShippingManagerDefaults.ProductAvailabilityRangesByAllKey, vendorId);

            var query = await _productAvailabilityRangeRepository.GetAllAsync(query =>
            {
                return from par in _productAvailabilityRangeRepository.Table
                       orderby par.Name
                       select par;
            }, cache => cacheKey);

            if (vendorId != 0)
                query = (from par in query
                         join eg in _entityGroupRepository.Table on par.Id equals eg.EntityId
                         where eg.VendorId == vendorId &&
                               eg.KeyGroup == "ProductAvailabilityRange"
                         orderby par.DisplayOrder, par.Name
                         select par).ToList();

            return query.ToList();

        }

        /// <summary>
        /// Insert the product availability range
        /// </summary>
        /// <param name="productAvailabilityRange">Product availability range</param>
        public override async Task InsertProductAvailabilityRangeAsync(ProductAvailabilityRange productAvailabilityRange)
        {
            await base.InsertProductAvailabilityRangeAsync(productAvailabilityRange);

            await _staticCacheManager.RemoveByPrefixAsync(ShippingManagerDefaults.ProductAvailabilityRangesByPatternKey);

            //Update entity groups
            await _entityGroupService.CreateOrUpdateEntityGroupingAsync<ProductAvailabilityRange>(productAvailabilityRange);

        }

        /// <summary>
        /// Update the product availability range
        /// </summary>
        /// <param name="productAvailabilityRange">Product availability range</param>
        public override async Task UpdateProductAvailabilityRangeAsync(ProductAvailabilityRange productAvailabilityRange)
        {
            await base.UpdateProductAvailabilityRangeAsync(productAvailabilityRange);

            await _staticCacheManager.RemoveByPrefixAsync(ShippingManagerDefaults.ProductAvailabilityRangesByPatternKey);

            //Update entity groups
            await _entityGroupService.CreateOrUpdateEntityGroupingAsync<ProductAvailabilityRange>(productAvailabilityRange);
        }

        /// <summary>
        /// Delete the product availability range
        /// </summary>
        /// <param name="productAvailabilityRange">Product availability range</param>
        public override async Task DeleteProductAvailabilityRangeAsync(ProductAvailabilityRange productAvailabilityRange)
        {
            var storeId = await _entityGroupService.GetActiveStoreScopeConfiguration();

            //Get vendor scope
            int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

            await _entityGroupService.DeleteEntityGroupMemberAsync<ProductAvailabilityRange>(productAvailabilityRange, "Member", vendorId: vendorId);

            await _staticCacheManager.RemoveByPrefixAsync(ShippingManagerDefaults.ProductAvailabilityRangesByPatternKey);

            var entityGroups = _entityGroupService.GetAllEntityGroups(_entityGroupService.GetEntityKeyGroup(productAvailabilityRange), productAvailabilityRange.Id, "Member", null, storeId);
            if (entityGroups.Count == 0)
                await base.DeleteProductAvailabilityRangeAsync(productAvailabilityRange);
        }

        #endregion

        #endregion
    }
}