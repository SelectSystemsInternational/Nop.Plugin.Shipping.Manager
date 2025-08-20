using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Nop.Core;
using Nop.Core.Infrastructure;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Logging;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Data;
using Nop.Core.Caching;
using Nop.Services.Attributes;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Shipping.Pickup;
using Nop.Services.Shipping;
using Nop.Services.Vendors;

using Nop.Plugin.Shipping.Manager.Models;
using Nop.Plugin.Shipping.Manager.Settings;

using Nop.Plugin.Apollo.Integrator.Domain;
using Nop.Plugin.Apollo.Integrator.Services;
using Nop.Core.Domain.Catalog;

namespace Nop.Plugin.Shipping.Manager.Services;

/// <summary>
/// Shipping service
/// </summary>
public partial class CustomShippingService : ShippingService
{

    #region Fields

    protected readonly IEntityGroupService _entityGroupService;
    protected readonly ShippingManagerSettings _shippingManagerSettings;
    protected readonly IWorkContext _workContext;
    protected readonly IRepository<EntityGroup> _entityGroupRepository;
    protected readonly ICarrierService _carrierService;
    protected readonly ISendcloudService _sendcloudService;
    protected readonly IFastwayService _fastwayService;
    protected readonly IStaticCacheManager _staticCacheManager;
    protected readonly IMeasureService _measureService;
    protected readonly IVendorService _vendorService;

    #endregion

    #region Ctor

    public CustomShippingService(IAddressService addressService,
        IAttributeParser<CheckoutAttribute, CheckoutAttributeValue> checkoutAttributeParser,
        ICountryService countryService,
        ICustomerService customerService,
        IGenericAttributeService genericAttributeService,
        ILocalizationService localizationService,
        ILogger logger,
        IPickupPluginManager pickupPluginManager,
        IPriceCalculationService priceCalculationService,
        IProductAttributeParser productAttributeParser,
        IProductService productService,
        IRepository<ShippingMethod> shippingMethodRepository,
        IRepository<ShippingMethodCountryMapping> shippingMethodCountryMappingRepository,
        IRepository<Warehouse> warehouseRepository,
        IShippingPluginManager shippingPluginManager,
        IStateProvinceService stateProvinceService,
        IStoreContext storeContext,
        ShippingSettings shippingSettings,
        ShippingManagerSettings shippingManagerSettings,
        ShoppingCartSettings shoppingCartSettings,
        IEntityGroupService entityGroupService,
        IWorkContext workContext,
        IRepository<EntityGroup> entityGroupRepository,
        ICarrierService carrierService,
        ISendcloudService sendcloudService,
        IFastwayService fastwayService,
        IStaticCacheManager staticCacheManager,
        IMeasureService measureService,
        IVendorService vendorService) : base(addressService,
            checkoutAttributeParser,
            countryService,
            customerService,
            genericAttributeService,
            localizationService,
            logger,
            pickupPluginManager,
            priceCalculationService,
            productAttributeParser,
            productService,
            shippingMethodRepository,
            shippingMethodCountryMappingRepository,
            warehouseRepository,
            shippingPluginManager,
            stateProvinceService,
            storeContext,
            shippingSettings,
            shoppingCartSettings)
    {
        _entityGroupService = entityGroupService;
        _workContext = workContext;
        _entityGroupRepository = entityGroupRepository;
        _shippingManagerSettings = shippingManagerSettings;
        _carrierService = carrierService;
        _sendcloudService = sendcloudService;
        _fastwayService = fastwayService;
        _staticCacheManager = staticCacheManager;
        _measureService = measureService;
        _vendorService = vendorService;
    }

    #endregion

    #region Utility

    protected class Weight
    {
        public static string Units => "kg";

        public int Value { get; set; }
    }

    protected virtual async Task<MeasureWeight> GatewayMeasureWeightAsync()
    {
        var usedWeight = await _measureService.GetMeasureWeightBySystemKeywordAsync(Weight.Units);
        if (usedWeight == null)
            throw new NopException("Fastway shipping service. Could not load \"{0}\" measure weight", Weight.Units);

        return usedWeight;
    }

    #endregion

    #region Method Overrides

    #region Shipping methods

    /// <summary>
    /// Gets a shipping method
    /// </summary>
    /// <param name="shippingMethodId">The shipping method identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the shipping method
    /// </returns>
    public override async Task<ShippingMethod> GetShippingMethodByIdAsync(int shippingMethodId)
    {
        if (shippingMethodId == 0)
            return null;

        return await base.GetShippingMethodByIdAsync(shippingMethodId);
    }

    /// <summary>
    /// Gets all shipping methods
    /// </summary>
    /// <param name="filterByCountryId">The country identifier to filter by</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the shipping methods
    /// </returns>
    public override async Task<IList<ShippingMethod>> GetAllShippingMethodsAsync(int? filterByCountryId = null)
    {

        // Check if Shipping.Manager is Active
        var plugin = (await _shippingPluginManager.LoadActivePluginsAsync(systemName: ShippingManagerDefaults.SystemName)).FirstOrDefault();
        if (plugin == null || !_shippingManagerSettings.Enabled)
            return await base.GetAllShippingMethodsAsync(filterByCountryId);


        if (!_shippingManagerSettings.InternationalOperationsEnabled)
            filterByCountryId = null;

        var shippingMethods = new List<ShippingMethod>();

        int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

        if (vendorId == 0)
        {
            return await base.GetAllShippingMethodsAsync(filterByCountryId);
        }
        else
        {
            var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(ShippingManagerDefaults.ShippingMethodsByVendorAllKey, filterByCountryId, vendorId);

            if (_shippingManagerSettings.InternationalOperationsEnabled && filterByCountryId.HasValue && filterByCountryId.Value > 0)
            {
                return await _shippingMethodRepository.GetAllAsync(query =>
                {
                    var query1 = from sm in query
                                 join smcm in _shippingMethodCountryMappingRepository.Table on sm.Id equals smcm.ShippingMethodId
                                 join eg in _entityGroupRepository.Table on sm.Id equals eg.EntityId
                                 where (vendorId == 0 || eg.VendorId == vendorId) &&
                                       (eg.KeyGroup == "ShippingMethod") &&
                                       smcm.CountryId == filterByCountryId.Value
                                 select sm.Id;

                    query1 = query1.Distinct();

                    var query2 = from sm in query
                                 join eg in _entityGroupRepository.Table on sm.Id equals eg.EntityId
                                 where (vendorId == 0 || eg.VendorId == vendorId) &&
                                       (eg.KeyGroup == "ShippingMethod") &&
                                       !query1.Contains(sm.Id)
                                 orderby sm.DisplayOrder, sm.Name
                                 select sm;

                    return query2;

                }, cache => cacheKey);
            }

            return await _shippingMethodRepository.GetAllAsync(query =>
            {
                return from sm in query
                       join eg in _entityGroupRepository.Table on sm.Id equals eg.EntityId
                       where (vendorId == 0 || eg.VendorId == vendorId) &&
                             (eg.KeyGroup == "ShippingMethod")
                       orderby sm.DisplayOrder, sm.Name
                       select sm;

            }, cache => cacheKey);

        }
    }

    /// <summary>
    /// Inserts a shipping method
    /// </summary>
    /// <param name="shippingMethod">Shipping method</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public override async Task InsertShippingMethodAsync(ShippingMethod shippingMethod)
    {
        await base.InsertShippingMethodAsync(shippingMethod);

        //Update entity groups
        await _entityGroupService.CreateOrUpdateEntityGroupingAsync<ShippingMethod>(shippingMethod);

    }

    /// <summary>
    /// Updates the shipping method
    /// </summary>
    /// <param name="shippingMethod">Shipping method</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public override async Task UpdateShippingMethodAsync(ShippingMethod shippingMethod)
    {
        await base.UpdateShippingMethodAsync(shippingMethod);

        //Update entity groups
        await _entityGroupService.CreateOrUpdateEntityGroupingAsync<ShippingMethod>(shippingMethod);
    }

    /// <summary>
    /// Deletes a shipping method
    /// </summary>
    /// <param name="shippingMethod">The shipping method</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public override async Task DeleteShippingMethodAsync(ShippingMethod shippingMethod)
    {
        var storeId = await _entityGroupService.GetActiveStoreScopeConfiguration();

        //Get vendor scope
        int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

        //Update entity groups
        await _entityGroupService.DeleteEntityGroupMemberAsync<ShippingMethod>(shippingMethod, "Member", vendorId: vendorId);

        var entityGroups = _entityGroupService.GetAllEntityGroups(_entityGroupService.GetEntityKeyGroup(shippingMethod), shippingMethod.Id, "Member", null, storeId);
        if (entityGroups.Count == 0)
            await base.DeleteShippingMethodAsync(shippingMethod);

    }

    #endregion

    #region Warehouses

    /// <summary>
    /// Gets all warehouses
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the paged list of warehouses
    /// </returns>
    public override async Task<IList<Warehouse>> GetAllWarehousesAsync(string name = null)
    {
        int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

        var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(ShippingManagerDefaults.WarehousesByAllKey, vendorId);

        var warehouses = await _warehouseRepository.GetAllAsync(query =>
        {
            return from w in _warehouseRepository.Table
                   orderby w.Name
                   select w;
        }, cache => cacheKey);

        if (vendorId != 0)
            warehouses = (from w in warehouses
                          join eg in _entityGroupRepository.Table on w.Id equals eg.EntityId
                          where eg.VendorId == vendorId &&
                          eg.KeyGroup == "Warehouse"
                          orderby w.Name, eg.VendorId
                          select w).ToList();

        if (!string.IsNullOrEmpty(name))
            warehouses = warehouses.Where(wh => wh.Name.Contains(name)).ToList();

        return warehouses.ToList();
    }

    /// <summary>
    /// Inserts a warehouse
    /// </summary>
    /// <param name="warehouse">Warehouse</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public override async Task InsertWarehouseAsync(Warehouse warehouse)
    {
        await _warehouseRepository.InsertAsync(warehouse);

        await _staticCacheManager.RemoveByPrefixAsync(ShippingManagerDefaults.WarehousesByPatternKey);

        //Update entity groups
        await _entityGroupService.CreateOrUpdateEntityGroupingAsync<Warehouse>(warehouse);
    }

    /// <summary>
    /// Updates the warehouse
    /// </summary>
    /// <param name="warehouse">Warehouse</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public override async Task UpdateWarehouseAsync(Warehouse warehouse)
    {
        await base.UpdateWarehouseAsync(warehouse);

        await _staticCacheManager.RemoveByPrefixAsync(ShippingManagerDefaults.WarehousesByPatternKey);

        //Update entity groups
        await _entityGroupService.CreateOrUpdateEntityGroupingAsync<Warehouse>(warehouse);
    }

    /// <summary>
    /// Deletes a warehouse
    /// </summary>
    /// <param name="warehouse">The warehouse</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public override async Task DeleteWarehouseAsync(Warehouse warehouse)
    {
        var storeId = await _entityGroupService.GetActiveStoreScopeConfiguration();

        //Get vendor scope
        int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

        //Update entity groups
        await _entityGroupService.DeleteEntityGroupMemberAsync<Warehouse>(warehouse, "Member", vendorId: vendorId);

        await _staticCacheManager.RemoveByPrefixAsync(ShippingManagerDefaults.WarehousesByPatternKey);

        var entityGroups = _entityGroupService.GetAllEntityGroups(_entityGroupService.GetEntityKeyGroup(warehouse), warehouse.Id, "Member", null, storeId);
        if (entityGroups.Count == 0)
            await base.DeleteWarehouseAsync(warehouse);
    }

    #endregion

    #region Workflow

    /// <summary>
    /// Whether the shopping cart item is free shipping
    /// </summary>
    /// <param name="shoppingCartItem">Shopping cart item</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains true if the shopping cart item is free shipping; otherwise false
    /// </returns>
    public override async Task<bool> IsFreeShippingAsync(ShoppingCartItem shoppingCartItem)
    {
        //first, check whether shipping is required
        if (!await IsShipEnabledAsync(shoppingCartItem))
            return true;

        //then whether the product is free shipping
        if (shoppingCartItem.ProductId != 0 && !(await _productService.GetProductByIdAsync(shoppingCartItem.ProductId)).IsFreeShipping)
            return false;

        if (string.IsNullOrEmpty(shoppingCartItem.AttributesXml))
            return true;

        //and whether associated products of the shopping cart item is free shipping
        return await (await _productAttributeParser.ParseProductAttributeValuesAsync(shoppingCartItem.AttributesXml))
            .Where(attributeValue => attributeValue.AttributeValueType == AttributeValueType.AssociatedToProduct)
            .AllAwaitAsync(async attributeValue => (await _productService.GetProductByIdAsync(attributeValue.AssociatedProductId))?.IsFreeShipping ?? true);
    }

    #endregion

    #region Shipping Options

    /// <summary>
    ///  Gets available shipping options
    /// </summary>
    /// <param name="cart">Shopping cart</param>
    /// <param name="shippingAddress">Shipping address</param>
    /// <param name="customer">Load records allowed only to a specified customer; pass null to ignore ACL permissions</param>
    /// <param name="allowedShippingRateComputationMethodSystemName">Filter by shipping rate computation method identifier; null to load shipping options of all shipping rate computation methods</param>
    /// <param name="storeId">Load records allowed only in a specified store; pass 0 to load all records</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the shipping options
    /// </returns>
    public override async Task<GetShippingOptionResponse> GetShippingOptionsAsync(IList<ShoppingCartItem> cart, Address shippingAddress,
        Customer customer = null, string allowedShippingRateComputationMethodSystemName = "", int storeId = 0)
    {

        // Check if Shipping.Manager is Active
        var plugin = (await _shippingPluginManager.LoadActivePluginsAsync(customer, storeId, ShippingManagerDefaults.SystemName)).FirstOrDefault();
        if (plugin == null || !_shippingManagerSettings.Enabled)
            return await base.GetShippingOptionsAsync(cart, shippingAddress, customer, allowedShippingRateComputationMethodSystemName, storeId);

        if (cart == null)
            throw new ArgumentNullException(nameof(cart));

        if (_shippingManagerSettings.TestMode)
        {
            string countryName = string.Empty;
            string stateName = string.Empty;
            if (shippingAddress.CountryId.HasValue)
            {
                var country = await _countryService.GetCountryByIdAsync(shippingAddress.CountryId.Value);
                if (country != null)
                    countryName = country.Name;

                if (shippingAddress.StateProvinceId.HasValue)
                {
                    var stateProvince = await _stateProvinceService.GetStateProvinceByIdAsync(shippingAddress.StateProvinceId.Value);
                    if (stateProvince != null)
                        stateName = stateProvince.Name;
                }

                string message = "Shipping Manager - Custom Shipping Service > For Store : " + storeId +
                        " Get Shipping Options > For Cart with " + cart.Count + " item(s) :" +
                        " Shipping Adddress : Country: " + country.Name + " State: " + stateName +
                        " Zip: " + shippingAddress.ZipPostalCode +
                        " allowedShippingRateComputationMethodSystemName = " + allowedShippingRateComputationMethodSystemName;
                await _logger.InsertLogAsync(LogLevel.Information, message);
            }
        }

        var result = new GetShippingOptionResponse();

        //create shipping packages
        var (shippingOptionRequests, shippingFromMultipleLocations) = await CreateShippingOptionRequestsAsync(cart, shippingAddress, storeId);
        result.ShippingFromMultipleLocations = shippingFromMultipleLocations;

        if (_shippingManagerSettings.TestMode)
        {
            string message = "Shipping Manager - Custom Shipping Service > For Store : " + storeId +
                " Shipping Options Requests Created: " + shippingOptionRequests.Count() +
                " Shipping From Multiple Locations: " + result.ShippingFromMultipleLocations.ToString();
            await _logger.InsertLogAsync(LogLevel.Debug, message);
        }

        if (_shippingManagerSettings.ProcessingMode == ProcessingMode.Method)
        {
            //request shipping options (separately for each package-request)
            IList<ShippingOption> srcmShippingOptions = null;
            foreach (var shippingOptionRequest in shippingOptionRequests)
            {
                var sendcloudService = EngineContext.Current.Resolve<ISendcloudService>();
                var getShippingOptionResponse = await sendcloudService.GetShippingOptionsAsync(shippingOptionRequest);

                if (getShippingOptionResponse.Success)
                {
                    //success
                    if (srcmShippingOptions == null)
                    {
                        //first shipping option request
                        srcmShippingOptions = getShippingOptionResponse.ShippingOptions;
                    }
                    else
                    {
                        //get shipping options which already exist for prior requested packages for this scrm (i.e. common options)
                        srcmShippingOptions = srcmShippingOptions
                            .Where(existingso => getShippingOptionResponse.ShippingOptions.Any(newso => newso.Name == existingso.Name))
                            .ToList();

                        //and sum the rates
                        foreach (var existingso in srcmShippingOptions)
                        {
                            existingso.Rate += getShippingOptionResponse
                                .ShippingOptions
                                .First(newso => newso.Name == existingso.Name)
                                .Rate;
                        }
                    }
                }
                else
                {
                    //errors
                    foreach (var error in getShippingOptionResponse.Errors)
                    {
                        result.AddError(error);
                        await _logger.WarningAsync($"Shipping ({ShippingManagerDefaults.SendCloudSystemName}). {error}");
                    }
                    //clear the shipping options in this case
                    srcmShippingOptions = new List<ShippingOption>();
                    break;
                }
            }

            foreach (var so in srcmShippingOptions)
            {
                //set system name if not set yet
                if (string.IsNullOrEmpty(so.ShippingRateComputationMethodSystemName))
                    so.ShippingRateComputationMethodSystemName = ShippingManagerDefaults.SendCloudSystemName;
                if (_shoppingCartSettings.RoundPricesDuringCalculation)
                    so.Rate = await _priceCalculationService.RoundPriceAsync(so.Rate);
                result.ShippingOptions.Add(so);
            }
        }
        else
        {

            var smrList = new List<ShippingManagerCalculationOptions>();

        if (string.IsNullOrEmpty(allowedShippingRateComputationMethodSystemName))
        {
            smrList = await ProcessRequestListByTypeAsync(shippingOptionRequests, cart, customer, storeId);
        }
        else
        {
            var srcm = (await _shippingPluginManager.LoadActivePluginsAsync(customer, storeId, allowedShippingRateComputationMethodSystemName)).FirstOrDefault();
            if (srcm != null)
            {
                var shippingMethodOptions = await GetShippingMethodOptionsAsync(srcm, shippingOptionRequests);
                result = await GetShippingMethodOptionResponsesAsync(result, shippingMethodOptions, storeId);
            }
        }

    // Combine all the responses to present available options 
    foreach (var smr in smrList)
    {
        var tempResult = new GetShippingOptionResponse();
        foreach (var product in smr.Smcro)
        {
            if (smr.ShippingRateComputationMethodSystemName == ShippingManagerDefaults.AramexSystemName || 
                smr.ShippingRateComputationMethodSystemName == ShippingManagerDefaults.SendCloudSystemName ||
                smr.ShippingRateComputationMethodSystemName == ShippingManagerDefaults.SystemName)
                tempResult = await GetShippingMethodOptionResponsesAsync(tempResult, product.Gsor, storeId);
            else
                tempResult = await ShippingOptionCombineResponsesAsync(tempResult, product.Gsor, storeId);
        }

            result = await GetShippingMethodOptionResponsesAsync(result, tempResult, storeId);
        }
    }

        if (_shippingSettings.ReturnValidOptionsIfThereAreAny)
        {
            //return valid options if there are any (no matter of the errors returned by other shipping rate computation methods).
            if (result.ShippingOptions.Any() && result.Errors.Any())
                result.Errors.Clear();
        }

        //no shipping options loaded
        if (!result.ShippingOptions.Any() && !result.Errors.Any())
            result.Errors.Add(await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.ShippingOptionCouldNotbeLoaded"));
        else if (result.Errors.Any())
        {
            if (_shippingManagerSettings.TestMode)
            {
                string message = "Shipping Manager - Custom Shipping Service > For Store : " + storeId +
                    " Shipping Method Error Count: " + result.Errors.Count();

                foreach (var error in result.Errors)
                    message += "Error: " + error;

                await _logger.InsertLogAsync(LogLevel.Debug, message);
            }
        }

        result.ShippingOptions = result.ShippingOptions.OrderBy(x => x.Rate).ToList();

        return result;
    }

    /// <summary>
    ///  Builds a request list either by Product or Warehouse         
    /// </summary>
    /// <param name="cart">Shopping cart</param>
    /// <param name="shippingAddress">Shipping address</param>
    /// <param name="customer">Load records allowed only to a specified customer; pass null to ignore ACL permissions</param>
    /// <param name="allowedShippingRateComputationMethodSystemName">Filter by shipping rate computation method identifier; null to load shipping options of all shipping rate computation methods</param>
    /// <param name="storeId">Load records allowed only in a specified store; pass 0 to load all records</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the shipping options request list
    /// </returns>
    public virtual async Task<List<ShippingManagerCalculationOptions>> ProcessRequestListByTypeAsync(IList<GetShippingOptionRequest> shippingOptionRequests,
        IList<ShoppingCartItem> cart, Customer customer, int storeId = 0)
    {
        var smrList = new List<ShippingManagerCalculationOptions>();
        var result = new GetShippingOptionResponse();

        if (_shippingManagerSettings.ProcessingMode == ProcessingMode.Item)
        {
            smrList = await CreateRequestsListByProductAsync(shippingOptionRequests, cart, storeId);

            if (_shippingManagerSettings.TestMode)
            {
                string message = "Shipping Manager - Custom Shipping Service > For Store : " + storeId +
                        " Using Item Method > SMRList Count: " + smrList.Count +
                        " Item List Count: " + cart.Count() +
                        " Shipping Option Request Count: " + shippingOptionRequests.Count;
                await _logger.InsertLogAsync(LogLevel.Debug, message);
            }

            foreach (var smrItem in smrList)
            {
                string srcmName = smrItem.ShippingRateComputationMethodSystemName;

                if (_shippingManagerSettings.TestMode)
                {
                    string message = "Shipping Manager - Custom Shipping Service > For Store : " + storeId +
                        " SRCM System Name: " + smrItem.ShippingRateComputationMethodSystemName + " SMRItem Count: " + smrItem.Smcro.Count() + " + " + "Item List Count: " + cart.Count();
                    await _logger.InsertLogAsync(LogLevel.Debug, message);
                }

                if (smrItem.SimpleList && smrItem.Smcro.Count() == 0 || // To Do checks
                    !smrItem.SimpleList && smrItem.Smcro.Count() != cart.Count())
                {
                    // Only partial cost can be calculated for this method
                    string error = "Not enough Vendor methods for " + smrItem.ShippingRateComputationMethodSystemName;
                    result.Errors.Add(error);
                }
                else
                {
                    if (smrItem.ShippingRateComputationMethodSystemName == ShippingManagerDefaults.SystemName)
                    {
                        // Shipping.Manager 
                        var srcm = (await _shippingPluginManager.LoadActivePluginsAsync(customer, storeId, ShippingManagerDefaults.SystemName)).FirstOrDefault();
                        if (srcm != null)
                        {
                            foreach (var productRequest in smrItem.Smcro)
                                foreach (var smco in productRequest.Smco)
                                    await GetShippingMethodResponsesForProductAsync(srcm, productRequest, smco, storeId);
                        }
                    }
                    else if (smrItem.ShippingRateComputationMethodSystemName == ShippingManagerDefaults.SendCloudSystemName)
                    {
                        // Shipping.Sendcloud 
                        var srcm = (await _shippingPluginManager.LoadActivePluginsAsync(customer, storeId, ShippingManagerDefaults.SystemName)).FirstOrDefault();
                        if (srcm != null)
                        {
                            foreach (var productRequest in smrItem.Smcro)
                                await GetSendcloudResponsesForProductAsync(productRequest, storeId);
                        }
                    }
                    else if (smrItem.ShippingRateComputationMethodSystemName == ShippingManagerDefaults.AramexSystemName)
                    {
                        // Shipping.Aramex
                        var srcm = (await _shippingPluginManager.LoadActivePluginsAsync(customer,
                        storeId, ShippingManagerDefaults.SystemName)).FirstOrDefault();
                        if (srcm != null)
                        {
                            foreach (var productRequest in smrItem.Smcro)
                                foreach (var smco in productRequest.Smco)
                                    await GetFastwayResponsesForProductAsync(productRequest, smco, storeId);
                        }
                    }
                    else if (smrItem.ShippingRateComputationMethodSystemName == ShippingManagerDefaults.SystemName)
                        {
                            // Shipping.Manager 
                            var srcm = (await _shippingPluginManager.LoadActivePluginsAsync(customer, storeId, ShippingManagerDefaults.SystemName)).FirstOrDefault();
                            if (srcm != null)
                            {
                                foreach (var productRequest in smrItem.Smcro)
                                    foreach (var smco in productRequest.Smco)
                                        await GetShippingMethodResponsesForProductAsync(srcm, productRequest, smco, storeId);
                            }
                        }
else
                    {
                        // Shipping.Plugin
                        var srcm = (await _shippingPluginManager.LoadActivePluginsAsync(customer, storeId, srcmName)).FirstOrDefault();
                        if (srcm != null)
                        {
                            foreach (var productRequest in smrItem.Smcro)
                                foreach (var smco in productRequest.Smco)
                                    await GetShippingCalculationMethodResponsesForProductAsync(srcm, productRequest, smco, storeId);
                        }
                    }
                }
            }
        }

        if (_shippingManagerSettings.ProcessingMode == ProcessingMode.Volume)
        {
            smrList = await CreateRequestsListByWarehouseAsync(shippingOptionRequests, cart, storeId);

            if (_shippingManagerSettings.TestMode)
            {
                string message = "Shipping Manager - Custom Shipping Service > For Store : " + storeId +
                        " Using Volume Method > SMRList Count: " + smrList.Count +
                        " Item List Count: " + cart.Count() +
                        " Shipping Option Request Count: " + shippingOptionRequests.Count;
                await _logger.InsertLogAsync(LogLevel.Debug, message);
            }

            // Process the list if there are any shipping options
            foreach (var smrItem in smrList)
            {
                string srcmName = smrItem.ShippingRateComputationMethodSystemName;

                if (smrItem.ShippingRateComputationMethodSystemName == ShippingManagerDefaults.SystemName)
                {
                    // Shipping.Manager 
                    var srcm = (await _shippingPluginManager.LoadActivePluginsAsync(customer, storeId, ShippingManagerDefaults.SystemName)).FirstOrDefault();
                    if (srcm != null)
                    {
                        foreach (var warehouseRequest in smrItem.Smcro)
                            foreach (var smco in warehouseRequest.Smco)
                                await GetShippingMethodResponsesForWarehouseAsync(srcm, warehouseRequest, smco, storeId);
                    }
                }
                else if (smrItem.ShippingRateComputationMethodSystemName == ShippingManagerDefaults.SendCloudSystemName)
                {
                    // Shipping.Sendcloud 
                    var srcm = (await _shippingPluginManager.LoadActivePluginsAsync(customer, storeId, ShippingManagerDefaults.SystemName)).FirstOrDefault();
                    if (srcm != null)
                    {
                        foreach (var warehouseRequest in smrItem.Smcro)
                            await GetSendcloudResponsesForProductAsync(warehouseRequest, storeId); // No Difference for Product and warehouse 
                    }
                }
                else if (smrItem.ShippingRateComputationMethodSystemName == ShippingManagerDefaults.AramexSystemName)
                {
                    // Shipping.Aramex
                    var srcm = (await _shippingPluginManager.LoadActivePluginsAsync(customer, storeId, ShippingManagerDefaults.SystemName)).FirstOrDefault();
                    if (srcm != null)
                    {
                        foreach (var warehouseRequest in smrItem.Smcro)
                            foreach (var smco in warehouseRequest.Smco)
                                await GetFastwayResponsesForWarehouseAsync(warehouseRequest, smco, storeId);
                    }
                }
                else
                {
                    // Shipping.Plugin
                    var srcm = (await _shippingPluginManager.LoadActivePluginsAsync(customer, storeId, srcmName)).FirstOrDefault();
                    if (srcm != null)
                    {
                        foreach (var warehouseRequest in smrItem.Smcro)
                            foreach (var smco in warehouseRequest.Smco)
                                await GetShippingCalculationMethodResponsesForWarehouseAsync(srcm, warehouseRequest, smco, storeId);
                    }
                }
            }
        }

        return smrList;
    }

    #endregion

    #endregion
}
