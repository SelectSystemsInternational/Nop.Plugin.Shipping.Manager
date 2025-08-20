using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Data;
using Nop.Services.Attributes;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Stores;
using Nop.Services.Shipping.Pickup;

using Nop.Plugin.Shipping.Manager.Domain;
using Nop.Plugin.Shipping.Manager.Settings;

using Nop.Plugin.Apollo.Integrator.Domain;
using Nop.Plugin.Apollo.Integrator.Services;

namespace Nop.Plugin.Shipping.Manager.Services;

/// <summary>
/// Shipping service
/// </summary>
public partial class PackagingOptionService : IPackagingOptionService
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
    protected readonly IStoreContext _storeContext;
    protected readonly ShippingSettings _shippingSettings;
    protected readonly ShoppingCartSettings _shoppingCartSettings;
    protected readonly IEntityGroupService _entityGroupService;
    protected readonly IRepository<EntityGroup> _entityGroupRepository;
    protected readonly IWorkContext _workContext;
    protected readonly IRepository<CutOffTime> _cutOffTimeRepository;
    protected readonly IStoreMappingService _storeMappingService;
    protected readonly IRepository<ProductWarehouseInventory> _productWarehouseInventoryRepository;
    protected readonly ILanguageService _languageService;
    protected readonly IRepository<LocalizedProperty> _localizedPropertyRepository;
    protected readonly IRepository<ProductManufacturer> _productManufacturerRepository;
    protected readonly IRepository<PackagingOption> _packagingOptionRepository;
    protected readonly ShippingManagerSettings _shippingManagerSettings;

    #endregion

    #region Ctor

    public PackagingOptionService(IStaticCacheManager staticCacheManager,
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
        IRepository<CutOffTime> cutOffTimeRepository,
        IRepository<EntityGroup> entityGroupRepository,
        IStoreMappingService storeMappingService,
        IRepository<ProductWarehouseInventory> productWarehouseInventoryRepository,
        ILanguageService languageService,
        IRepository<LocalizedProperty> localizedPropertyRepository,
        IRepository<ProductManufacturer> productManufacturerRepository,
        IRepository<PackagingOption> packagingOptionRepository,
        ShippingManagerSettings shippingManagerSettings)
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
        _cutOffTimeRepository = cutOffTimeRepository;
        _entityGroupRepository = entityGroupRepository;
        _storeMappingService = storeMappingService;
        _productWarehouseInventoryRepository = productWarehouseInventoryRepository;
        _languageService = languageService;
        _localizedPropertyRepository = localizedPropertyRepository;
        _productManufacturerRepository = productManufacturerRepository;
        _packagingOptionRepository = packagingOptionRepository;
        _shippingManagerSettings = shippingManagerSettings;
    }

    #endregion

    #region Utilities

    #endregion

    #region Methods

    #region Simple Packing Options

    /// <summary>
    /// Gets a PackagingOption
    /// </summary>
    /// <param name="packagingOptionId">The packaging option identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the PackagingOption
    /// </returns>
    public PackagingOption GetSimplePackagingOptionById(int packagingOptionId)
    {
        var packagingOptions = GetSimplePackagingOptions(false);

        int count = 0;
        foreach (var option in packagingOptions)
        {
            if (count++ == packagingOptionId)
                return option;
        }

        return null;
    }

    /// <summary>
    /// Gets all simple packaging options
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the list of PackagingOptions
    /// </returns>
    public virtual IList<PackagingOption> GetSimplePackagingOptions(bool defaultValue = true)
    {
        var packagingOptionList = new List<PackagingOption>();

        int count = 0;
        if (!string.IsNullOrEmpty(_shippingManagerSettings.PackagingOptions))
        {
            var options = _shippingManagerSettings.PackagingOptions.Split(";");
            foreach (var option in options)
            {
                if (!string.IsNullOrEmpty(option))
                {
                    var packagingDetails = option.Split(":");
                    if (packagingDetails.Count() == 5)
                    {
                        var packagingOption = new PackagingOption();
                        packagingOption.Id = count++;
                        packagingOption.Name = packagingDetails[0];
                        packagingOption.Length = decimal.Parse(packagingDetails[1]);
                        packagingOption.Width = decimal.Parse(packagingDetails[2]);
                        packagingOption.Height = decimal.Parse(packagingDetails[3]);
                        packagingOption.Weight = decimal.Parse(packagingDetails[4]);
                        packagingOptionList.Add(packagingOption);
                        if (defaultValue)
                            break;
                    }
                }
            }

            return packagingOptionList.ToList();
        }

        return null;
    }

    /// <summary>
    /// Gets all simple packaging options select list
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the list of PackagingOptions
    /// </returns>
    public virtual IList<SelectListItem> PrepareAvailablePackagingOptionsSelectList(int selectedItem)
    {
        var selectList = new List<SelectListItem>();

        int count = 0;
        foreach (var option in GetSimplePackagingOptions(false))
            selectList.Add(new SelectListItem { Text = option.Name, Value = count.ToString(), Selected = (count++ == selectedItem - 1) });

        return selectList;
    }

    #endregion

    //#region PackagingOptions

    ///// <summary>
    ///// Gets a PackagingOption
    ///// </summary>
    ///// <param name="packagingOptionId">The packaging option identifier</param>
    ///// <returns>
    ///// A task that represents the asynchronous operation
    ///// The task result contains the PackagingOption
    ///// </returns>
    //public async Task<PackagingOption> GetPackagingOptionByIdAsync(int packagingOptionId)
    //{
    //    return await _packagingOptionRepository.GetByIdAsync(packagingOptionId, cache => default);
    //}

    ///// <summary>
    ///// Gets a packaging option
    ///// </summary>
    ///// <param name="name">The packaging option name</param>
    ///// <returns>
    ///// A task that represents the asynchronous operation
    ///// The task result contains the PackagingOption
    ///// </returns>
    //public virtual async Task<PackagingOption> GetPackagingOptionByNameAsync(string name)
    //{
    //    if (string.IsNullOrEmpty(name))
    //        return null;
    //    else
    //        return (await GetAllPackagingOptionsAsync(true)).Where(c => c.Name.ToLower() == name.ToLower()).FirstOrDefault();
    //}

    ///// <summary>
    ///// Gets all packaging options
    ///// </summary>
    ///// <param name="showHidden">A flag to show hidden packaging options</param>
    ///// <returns>
    ///// A task that represents the asynchronous operation
    ///// The task result contains the list of PackagingOptions
    ///// </returns>
    //public virtual async Task<IList<PackagingOption>> GetAllPackagingOptionsAsync(bool showHidden = false)
    //{
    //    int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

    //    var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(ShippingManagerDefaults.PackagingOptionsByAllKey, vendorId);

    //    var query = await _packagingOptionRepository.GetAllAsync(query =>
    //    {
    //        return from po in _packagingOptionRepository.Table
    //               orderby po.Name
    //               select po;
    //    }, cache => cacheKey);

    //    if (vendorId != 0)
    //        query = (from po in query
    //                 join eg in _entityGroupRepository.Table on po.Id equals eg.EntityId
    //                 where eg.VendorId == vendorId &&
    //                       eg.KeyGroup == "PackagingOption"
    //                 orderby po.Name, eg.VendorId
    //                 select po).ToList();

    //    if (!showHidden)
    //        query = query.Where(po => po.Published).ToList();

    //    return query;

    //}

    ///// <summary>
    ///// Gets all packaging options for search criteria
    ///// </summary>
    ///// <returns>
    ///// A task that represents the asynchronous operation
    ///// The task result contains the paged list of PackagingOptions
    ///// </returns>
    //public virtual async Task<IList<PackagingOption>> GetAllPackagingOptionsAsync(PackagingOptionSearchModel searchModel)
    //{
    //    int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

    //    var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(ShippingManagerDefaults.PackagingOptionsByAllKey, vendorId);

    //    var query = await _packagingOptionRepository.GetAllAsync(query =>
    //    {
    //        return from po in _packagingOptionRepository.Table
    //               orderby po.Name
    //               select po;
    //    }, cache => cacheKey);

    //    if (vendorId != 0)
    //        query = (from c in query
    //                join eg in _entityGroupRepository.Table on c.Id equals eg.EntityId
    //                where eg.VendorId == vendorId &&
    //                      eg.KeyGroup == "PackagingOption"
    //                orderby c.Name, eg.VendorId
    //                select c).ToList();

    //    if (searchModel.Active)
    //        query = query.Where(c => c.Published).ToList();

    //    if (!string.IsNullOrEmpty(searchModel.SearchName))
    //        query = query.Where(c => c.Name.ToLower().Contains(searchModel.SearchName.ToLower())).ToList();

    //    return query.ToList();
    //}

    ///// <summary>
    ///// Search packaging options
    ///// </summary>
    ///// <param name="pageIndex">Page index</param>
    ///// <param name="pageSize">Page size</param>
    ///// <param name="manufacturerIds">Manufacturer identifiers</param>
    ///// <param name="storeId">Store identifier; 0 to load all records</param>
    ///// <param name="vendorId">Vendor identifier; 0 to load all records</param>
    ///// <param name="warehouseId">Warehouse identifier; 0 to load all records</param>
    ///// <param name="productType">Product type; 0 to load all records</param>
    ///// <param name="visibleIndividuallyOnly">A values indicating whether to load only products marked as "visible individually"; "false" to load all records; "true" to load "visible individually" only</param>
    ///// <param name="excludeFeaturedProducts">A value indicating whether loaded products are marked as featured (relates only to categories and manufacturers); "false" (by default) to load all records; "true" to exclude featured products from results</param>
    ///// <param name="priceMin">Minimum price; null to load all records</param>
    ///// <param name="priceMax">Maximum price; null to load all records</param>
    ///// <param name="keywords">Keywords</param>
    ///// <param name="searchDescriptions">A value indicating whether to search by a specified "keyword" in product descriptions</param>
    ///// <param name="searchManufacturerPartNumber">A value indicating whether to search by a specified "keyword" in manufacturer part number</param>
    ///// <param name="searchSku">A value indicating whether to search by a specified "keyword" in product SKU</param>
    ///// <param name="searchProductTags">A value indicating whether to search by a specified "keyword" in product tags</param>
    ///// <param name="languageId">Language identifier (search for text searching)</param>
    ///// <param name="filteredSpecOptions">Specification options list to filter products; null to load all records</param>
    ///// <param name="orderBy">Order by</param>
    ///// <param name="showHidden">A value indicating whether to show hidden records</param>
    ///// <param name="overridePublished">
    ///// null - process "Published" property according to "showHidden" parameter
    ///// true - load only "Published" products
    ///// false - load only "Unpublished" products
    ///// </param>
    ///// <returns>
    ///// A task that represents the asynchronous operation
    ///// The task result contains the products
    ///// </returns>
    //public virtual async Task<IPagedList<PackagingOption>> SearchPackagingOptionAsync(
    //    int pageIndex = 0,
    //    int pageSize = int.MaxValue,
    //    IList<int> manufacturerIds = null,
    //    int storeId = 0,
    //    int vendorId = 0,
    //    int warehouseId = 0,
    //    PackagingOptionType? packagingOptionType = null,
    //    bool visibleIndividuallyOnly = false,
    //    bool excludeFeaturedProducts = false,
    //    decimal? priceMin = null,
    //    decimal? priceMax = null,
    //    string keywords = null,
    //    bool searchDescriptions = false,
    //    bool searchManufacturerPartNumber = true,
    //    bool searchSku = true,
    //    int languageId = 0,
    //    PackagingOptionSortingEnum orderBy = PackagingOptionSortingEnum.Position,
    //    bool showHidden = false,
    //    bool? overridePublished = null)
    //{
    //    //some databases don't support int.MaxValue
    //    if (pageSize == int.MaxValue)
    //        pageSize = int.MaxValue - 1;

    //    var packagingOptionQuery = _packagingOptionRepository.Table;

    //    if (!showHidden)
    //        packagingOptionQuery = packagingOptionQuery.Where(p => p.Published);
    //    else if (overridePublished.HasValue)
    //        packagingOptionQuery = packagingOptionQuery.Where(p => p.Published == overridePublished.Value);

    //    //apply store mapping constraints
    //    packagingOptionQuery = await _storeMappingService.ApplyStoreMapping(packagingOptionQuery, storeId);

    //    packagingOptionQuery =
    //        from p in packagingOptionQuery
    //        where !p.Deleted &&
    //            (!visibleIndividuallyOnly || p.UseIndividually) &&
    //            (vendorId == 0 || p.VendorId == vendorId) &&
    //            (
    //                warehouseId == 0 ||
    //                (
    //                    !p.UseMultipleWarehouses ? p.WarehouseId == warehouseId :
    //                        _productWarehouseInventoryRepository.Table.Any(pwi => pwi.Id == warehouseId && pwi.ProductId == p.Id)
    //                )
    //            ) &&
    //            (packagingOptionType == null || p.PackagingOptionTypeId == (int)packagingOptionType) &&
    //            (showHidden || LinqToDB.Sql.Between(DateTime.UtcNow, p.AvailableStartDateTimeUtc ?? DateTime.MinValue, p.AvailableEndDateTimeUtc ?? DateTime.MaxValue)) &&
    //            (priceMin == null || p.Price >= priceMin) &&
    //            (priceMax == null || p.Price <= priceMax)
    //        select p;

    //    if (!string.IsNullOrEmpty(keywords))
    //    {
    //        var langs = await _languageService.GetAllLanguagesAsync(showHidden: true);

    //        //Set a flag which will to points need to search in localized properties. If showHidden doesn't set to true should be at least two published languages.
    //        var searchLocalizedValue = languageId > 0 && langs.Count >= 2 && (showHidden || langs.Count(l => l.Published) >= 2);

    //        IQueryable<int> productsByKeywords;

    //        productsByKeywords =
    //                from p in _packagingOptionRepository.Table
    //                where p.Name.Contains(keywords) ||
    //                    (searchDescriptions &&
    //                        p.ShortDescription.Contains(keywords)) ||
    //                    (searchManufacturerPartNumber && p.ManufacturerPartNumber == keywords) ||
    //                    (searchSku && p.Sku == keywords)
    //                select p.Id;

    //        if (searchLocalizedValue)
    //        {
    //            productsByKeywords = productsByKeywords.Union(
    //                        from lp in _localizedPropertyRepository.Table
    //                        let checkName = lp.LocaleKey == nameof(Product.Name) &&
    //                                        lp.LocaleValue.Contains(keywords)
    //                        let checkShortDesc = searchDescriptions &&
    //                                        lp.LocaleKey == nameof(Product.ShortDescription) &&
    //                                        lp.LocaleValue.Contains(keywords)
    //                        where
    //                            lp.LocaleKeyGroup == nameof(Product) && lp.LanguageId == languageId && (checkName || checkShortDesc)

    //                        select lp.EntityId);
    //        }

    //        packagingOptionQuery =
    //            from p in packagingOptionQuery
    //            from pbk in LinqToDB.LinqExtensions.InnerJoin(productsByKeywords, pbk => pbk == p.Id)
    //            select p;
    //    }

    //    if (manufacturerIds is not null)
    //    {
    //        if (manufacturerIds.Contains(0))
    //            manufacturerIds.Remove(0);

    //        if (manufacturerIds.Any())
    //        {
    //            var productManufacturerQuery =
    //                from pm in _productManufacturerRepository.Table
    //                where (!excludeFeaturedProducts || !pm.IsFeaturedProduct) &&
    //                    manufacturerIds.Contains(pm.ManufacturerId)
    //                group pm by pm.ProductId into pm
    //                select new
    //                {
    //                    ProductId = pm.Key,
    //                    DisplayOrder = pm.First().DisplayOrder
    //                };

    //            packagingOptionQuery =
    //                from p in packagingOptionQuery
    //                join pm in productManufacturerQuery on p.Id equals pm.ProductId
    //                orderby pm.DisplayOrder, p.Name
    //                select p;
    //        }
    //    }

    //    return await packagingOptionQuery.OrderBy(orderBy).ToPagedListAsync(pageIndex, pageSize);
    //}

    ///// <summary>
    ///// Inserts a packaging option
    ///// </summary>
    ///// <param name="packagingOption">The packaging option</param>
    ///// <returns>
    ///// A task that represents the asynchronous operation
    ///// </returns> 
    //public virtual async Task InsertPackagingOptionAsync(PackagingOption packagingOption)
    //{
    //    await _packagingOptionRepository.InsertAsync(packagingOption);

    //    await _staticCacheManager.RemoveByPrefixAsync(ShippingManagerDefaults.PackagingOptionsByPatternKey);

    //    //Update entity groups
    //    await _entityGroupService.CreateOrUpdateEntityGroupingAsync<PackagingOption>(packagingOption);
    //}

    ///// <summary>
    ///// Updates the packaging option
    ///// </summary>
    ///// <param name="packagingOption">The packaging option</param>
    ///// <returns>
    ///// A task that represents the asynchronous operation
    ///// </returns>  
    //public virtual async Task UpdatePackagingOptionAsync(PackagingOption packagingOption)
    //{
    //    await _packagingOptionRepository.UpdateAsync(packagingOption);

    //    await _staticCacheManager.RemoveByPrefixAsync(ShippingManagerDefaults.PackagingOptionsByPatternKey);

    //    //Update entity groups
    //    await _entityGroupService.CreateOrUpdateEntityGroupingAsync<PackagingOption>(packagingOption);
    //}

    ///// <summary>
    ///// Deletes a PackagingOption
    ///// </summary>
    ///// <param name="packagingOption">The packaging option</param>
    ///// <returns>
    ///// A task that represents the asynchronous operation
    ///// </returns> 
    //public virtual async Task DeletePackagingOptionAsync(PackagingOption packagingOption)
    //{
    //    //Get vendor scope
    //    int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

    //    //Update entity groups
    //    await _entityGroupService.DeleteEntityGroupMemberAsync<PackagingOption>(packagingOption, "Member", vendorId: vendorId);

    //    await _staticCacheManager.RemoveByPrefixAsync(ShippingManagerDefaults.PackagingOptionsByPatternKey);

    //    var entityGroups = _entityGroupService.GetAllEntityGroups(_entityGroupService.GetEntityKeyGroup(packagingOption), packagingOption.Id, "Member", null);
    //    if (entityGroups.Count == 0)
    //        await _packagingOptionRepository.DeleteAsync(packagingOption);
    //}

    //#endregion

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
        return await _cutOffTimeRepository.GetByIdAsync(cutOffTimeId);
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
        await _entityGroupService.DeleteEntityGroupMemberAsync<PackagingOption>(cutOffTime, "Member", vendorId: vendorId);

        var entityGroups = _entityGroupService.GetAllEntityGroups(_entityGroupService.GetEntityKeyGroup(cutOffTime), cutOffTime.Id, "Member", null, storeId);
        if (entityGroups.Count == 0)
            await _cutOffTimeRepository.DeleteAsync(cutOffTime);
    }

    #endregion

    #endregion

}