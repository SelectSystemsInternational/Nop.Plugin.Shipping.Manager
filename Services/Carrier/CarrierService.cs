using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Data;
using Nop.Services.Attributes;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Shipping.Pickup;

using Nop.Plugin.Shipping.Manager.Domain;
using Nop.Plugin.Shipping.Manager.Models.Carrier;

using Nop.Plugin.Apollo.Integrator.Domain;
using Nop.Plugin.Apollo.Integrator.Services;

namespace Nop.Plugin.Shipping.Manager.Services;

/// <summary>
/// Shipping service
/// </summary>
public partial class CarrierService : ICarrierService
{

    #region Fields

    protected readonly IStaticCacheManager _staticCacheManager;
    protected readonly IAddressService _addressService;
    protected readonly IAttributeParser<CheckoutAttribute, CheckoutAttributeValue> _checkoutAttributeParser;
    protected readonly IGenericAttributeService _genericAttributeService;
    protected readonly ILocalizationService _localizationService;
    protected readonly ILogger _logger;
    protected readonly IPickupPluginManager _pickupPluginManager;
    protected readonly IPriceCalculationService _priceCalculationService;
    protected readonly IProductAttributeParser _productAttributeParser;
    protected readonly IProductService _productService;
    protected readonly IRepository<Carrier> _carrierRepository;
    protected readonly IStoreContext _storeContext;
    protected readonly ShippingSettings _shippingSettings;
    protected readonly ShoppingCartSettings _shoppingCartSettings;
    protected readonly IEntityGroupService _entityGroupService;
    protected readonly IRepository<EntityGroup> _entityGroupRepository;
    protected readonly IWorkContext _workContext;
    protected readonly IRepository<CutOffTime> _cutOffTimeRepository;

    #endregion

    #region Ctor

    public CarrierService(IStaticCacheManager staticCacheManager,
        IAddressService addressService,
        IAttributeParser<CheckoutAttribute, CheckoutAttributeValue> checkoutAttributeParser,
        IGenericAttributeService genericAttributeService,
        ILocalizationService localizationService,
        ILogger logger,
        IPickupPluginManager pickupPluginManager,
        IPriceCalculationService priceCalculationService,
        IProductAttributeParser productAttributeParser,
        IProductService productService,
        IStoreContext storeContext,
        ShippingSettings shippingSettings,
        ShoppingCartSettings shoppingCartSettings,
        IEntityGroupService entityGroupService,
        IWorkContext workContext,
        IRepository<Carrier> carrierRepository,
        IRepository<CutOffTime> cutOffTimeRepository,
        IRepository<EntityGroup> entityGroupRepository)
    {
        _staticCacheManager = staticCacheManager;
        _addressService = addressService;
        _checkoutAttributeParser = checkoutAttributeParser;
        _genericAttributeService = genericAttributeService;
        _localizationService = localizationService;
        _logger = logger;
        _pickupPluginManager = pickupPluginManager;
        _priceCalculationService = priceCalculationService;
        _productAttributeParser = productAttributeParser;
        _productService = productService;
        _storeContext = storeContext;
        _shippingSettings = shippingSettings;
        _shoppingCartSettings = shoppingCartSettings;
        _entityGroupService = entityGroupService;
        _workContext = workContext;
        _carrierRepository = carrierRepository;
        _cutOffTimeRepository = cutOffTimeRepository;
        _entityGroupRepository = entityGroupRepository;
    }

    #endregion

    #region Utilities

    #endregion

    #region Methods

    #region Carriers

    /// <summary>
    /// Gets a carrier
    /// </summary>
    /// <param name="carrierId">The carrier identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the carrier
    /// </returns>
    public async Task<Carrier> GetCarrierByIdAsync(int carrierId)
    {
        // var key = _staticCacheManager.PrepareKeyForDefaultCache(ShippingManagerDefaults.CarriersByIdCacheKey, carrierId);

        return await _carrierRepository.GetByIdAsync(carrierId, cache => default);
    }


    /// <summary>
    /// Gets a carrier by name
    /// </summary>
    /// <param name="carrierId">The carrier identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the carrier
    /// </returns>
    public virtual async Task<Carrier> GetCarrierByNameAsync(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;
        else
            return (await GetAllCarriersAsync(true)).Where(c => c.Name.ToLower() == name.ToLower()).FirstOrDefault();
    }

    /// <summary>
    /// Gets a carrier by system name
    /// </summary>
    /// <param name="carrierId">The carrier identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the carrier
    /// </returns>
    public virtual async Task<Carrier> GetCarrierBySystemNameAsync(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;
        else
            return (await GetAllCarriersAsync(true)).Where(c => c.ShippingRateComputationMethodSystemName.ToLower() == name.ToLower()).FirstOrDefault();
    }

    /// <summary>
    /// Gets a carrier shipping plugin provider 
    /// </summary>
    /// <param name="carrierId">The carrier identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the shipping plugin provider 
    /// </returns>
    public async Task<string> GetCarrierShippingPluginProvideAsync(int carrierId)
    {
        var carrier = await _carrierRepository.GetByIdAsync(carrierId, cache => default);
        return carrier.ShippingRateComputationMethodSystemName;
    }

    /// <summary>
    /// Gets all carriers
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the list of carriers
    /// </returns>
    public virtual async Task<IList<Carrier>> GetAllCarriersAsync(bool showHidden = false)
    {
        int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

        var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(ShippingManagerDefaults.CarriersByAllKey, vendorId);

        var query = await _carrierRepository.GetAllAsync(query =>
        {
            return from c in _carrierRepository.Table
                   orderby c.Name
                   select c;
        }, cache => cacheKey);

        if (vendorId != 0)
            query = (from c in query
                     join eg in _entityGroupRepository.Table on c.Id equals eg.EntityId
                     where eg.VendorId == vendorId &&
                           eg.KeyGroup == "Carrier"
                     orderby c.Name, eg.VendorId
                     select c).ToList();

        if (!showHidden)
            query = query.Where(c => c.Active).ToList();

        return query;

    }

    /// <summary>
    /// Gets all carriers
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the paged list of carriers
    /// </returns>
    public virtual async Task<IList<Carrier>> GetAllCarriersAsync(CarrierSearchModel searchModel)
    {
        int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

        var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(ShippingManagerDefaults.CarriersByAllKey, vendorId);

        var query = await _carrierRepository.GetAllAsync(query =>
        {
            return from c in _carrierRepository.Table
                   orderby c.Name
                   select c;
        }, cache => cacheKey);

        if (vendorId != 0)
            query = (from c in query
                    join eg in _entityGroupRepository.Table on c.Id equals eg.EntityId
                    where eg.VendorId == vendorId &&
                          eg.KeyGroup == "Carrier"
                    orderby c.Name, eg.VendorId
                    select c).ToList();

        if (searchModel.Active)
            query = query.Where(c => c.Active).ToList();

        if (!string.IsNullOrEmpty(searchModel.SearchName))
            query = query.Where(c => c.Name.ToLower().Contains(searchModel.SearchName.ToLower())).ToList();

        return query.ToList();
    }

    /// <summary>
    /// Inserts a carrier
    /// </summary>
    /// <param name="carrier">Carrier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// </returns> 
    public virtual async Task InsertCarrierAsync(Carrier carrier)
    {
        await _carrierRepository.InsertAsync(carrier);

        await _staticCacheManager.RemoveByPrefixAsync(ShippingManagerDefaults.CarriersByPatternKey);

        //Update entity groups
        await _entityGroupService.CreateOrUpdateEntityGroupingAsync<Carrier>(carrier);
    }

    /// <summary>
    /// Updates the carrier
    /// </summary>
    /// <param name="carrier">Carrier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// </returns>  
    public virtual async Task UpdateCarrierAsync(Carrier carrier)
    {
        await _carrierRepository.UpdateAsync(carrier);

        await _staticCacheManager.RemoveByPrefixAsync(ShippingManagerDefaults.CarriersByPatternKey);

        //Update entity groups
        await _entityGroupService.CreateOrUpdateEntityGroupingAsync<Carrier>(carrier);
    }

    /// <summary>
    /// Deletes a carrier
    /// </summary>
    /// <param name="carrier">The carrier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// </returns> 
    public virtual async Task DeleteCarrierAsync(Carrier carrier)
    {
        //Get vendor scope
        int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

        //Update entity groups
        await _entityGroupService.DeleteEntityGroupMemberAsync<Carrier>(carrier, "Member", vendorId: vendorId);

        await _staticCacheManager.RemoveByPrefixAsync(ShippingManagerDefaults.CarriersByPatternKey);

        var entityGroups = _entityGroupService.GetAllEntityGroups(_entityGroupService.GetEntityKeyGroup(carrier), carrier.Id, "Member", null);
        if (entityGroups.Count == 0)
            await _carrierRepository.DeleteAsync(carrier);
    }

    #endregion

    #region Cut of time

    /// <summary>
    /// Get a cut of time
    /// </summary>
    /// <param name="cutOffTimeId">The cut of time identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the list of cut off times 
    /// </returns>  
    public virtual async Task<CutOffTime> GetCutOffTimeByIdAsync(int cutOffTimeId)
    {
        return await _cutOffTimeRepository.GetByIdAsync(cutOffTimeId, cache => default);
    }

    /// <summary>
    /// Get all cut of times
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the list of cut off times
    /// </returns>
    public virtual async Task<IList<CutOffTime>> GetAllCutOffTimesAsync()
    {
        int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

        var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(ShippingManagerDefaults.CutOffTimeByAllKey, vendorId);

        if (vendorId == 0)
        {
            return await _cutOffTimeRepository.GetAllAsync(query =>
            {
                return from cot in _cutOffTimeRepository.Table
                       orderby cot.DisplayOrder, cot.Id
                       select cot;
            }, cache => cacheKey);

        }
        else
        {
            return await _cutOffTimeRepository.GetAllAsync(query =>
            {
                return from cot in _cutOffTimeRepository.Table
                       join eg in _entityGroupRepository.Table on cot.Id equals eg.EntityId
                       where (vendorId == 0 || eg.VendorId == vendorId) &&
                             (eg.KeyGroup == "CutOffTime")
                       orderby cot.DisplayOrder, cot.Id
                       select cot;
            }, cache => cacheKey);
        }
    }

    /// <summary>
    /// Insert the cut of time
    /// </summary>
    /// <param name="cutOffTime">Cut off time</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// </returns> 
    public virtual async Task InsertCutOffTimeAsync(CutOffTime cutOffTime)
    {
        await _cutOffTimeRepository.InsertAsync(cutOffTime);

        await _staticCacheManager.RemoveByPrefixAsync(ShippingManagerDefaults.CutOffTimeByPatternKey);

        //Update entity groups
        await _entityGroupService.CreateOrUpdateEntityGroupingAsync<CutOffTime>(cutOffTime);
    }

    /// <summary>
    /// Update the cut of time
    /// </summary>
    /// <param name="cutOffTime">Cut off time</param>
    /// <returns>
    /// A task that represents the asynchronous operation 
    /// </returns>
    public virtual async Task UpdateCutOffTimeAsync(CutOffTime cutOffTime)
    {
        await _cutOffTimeRepository.UpdateAsync(cutOffTime);

        await _staticCacheManager.RemoveByPrefixAsync(ShippingManagerDefaults.CutOffTimeByPatternKey);

        //Update entity groups
        await _entityGroupService.CreateOrUpdateEntityGroupingAsync<CutOffTime>(cutOffTime);
    }

    /// <summary>
    /// Delete the cut of time
    /// </summary>
    /// <param name="cutOffTime">Cut off time</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// </returns>         
    public virtual async Task DeleteCutOffTimeAsync(CutOffTime cutOffTime)
    {
        var storeId = await _entityGroupService.GetActiveStoreScopeConfiguration();

        //Get vendor scope
        int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

        await _staticCacheManager.RemoveByPrefixAsync(ShippingManagerDefaults.CutOffTimeByPatternKey);

        //Update entity groups
        await _entityGroupService.DeleteEntityGroupMemberAsync<Carrier>(cutOffTime, "Member", vendorId: vendorId);

        var entityGroups = _entityGroupService.GetAllEntityGroups(_entityGroupService.GetEntityKeyGroup(cutOffTime), cutOffTime.Id, "Member", null, storeId);
        if (entityGroups.Count == 0)
            await _cutOffTimeRepository.DeleteAsync(cutOffTime);
    }

    #endregion

    #endregion

}