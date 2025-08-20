using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Seo;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Stores;
using Nop.Core.Domain.Tax;
using Nop.Core.Domain.Vendors;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Plugin.Apollo.Integrator;
using Nop.Plugin.Apollo.Integrator.Services;
using Nop.Plugin.Shipping.Manager.Domain;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Shipping;
using Nop.Services.Vendors;

namespace Nop.Plugin.Shipping.Manager.Services;
public class ShippingManagerInstallService : IShippingManagerInstallService
{

    #region Fields

    protected readonly ILocalizationService _localizationService;
    protected readonly ISettingService _settingService;
    protected readonly INopFileProvider _fileProvider;
    protected readonly IStoreContext _storeContext;
    protected readonly IRepository<EmailAccount> _emailAccountRepository;
    protected readonly IRepository<MessageTemplate> _messageTemplateRepository;
    protected readonly IRepository<Store> _storeRepository;
    protected readonly IRepository<Picture> _pictureRepository;
    protected readonly IRepository<Product> _productRepository;
    protected readonly IRepository<ProductAttribute> _productAttributeRepository;
    protected readonly IRepository<ProductTag> _productTagRepository;
    protected readonly IRepository<ProductTemplate> _productTemplateRepository;
    protected readonly IRepository<Category> _categoryRepository;
    protected readonly IRepository<CategoryTemplate> _categoryTemplateRepository;
    protected readonly IRepository<TaxCategory> _taxCategoryRepository;
    protected readonly IRepository<UrlRecord> _urlRecordRepository;
    protected readonly IShippingService _shippingService;
    protected readonly IVendorService _vendorService;
    protected readonly ICarrierService _carrierService;
    protected readonly IEntityGroupService _entityGroupService;
    protected readonly IShippingManagerService _shippingManagerService;
    protected readonly IProductAttributeService _productAttributeService;
    protected readonly IPermissionService _permissionService;
    protected readonly IRepository<PermissionRecord> _permissionRecordRepository;
    protected readonly INopDataProvider _dataProvider;
    protected readonly IRepository<PackagingOption> _packagingOptionRepository;
    protected readonly ICustomerService _customerService;
    protected readonly IStaticCacheManager _cacheManager;

    SystemHelper _systemHelper = new SystemHelper();

    #endregion

    #region Ctor
    public ShippingManagerInstallService(ILocalizationService localizationService,
        ISettingService settingService,
        INopFileProvider fileProvider,
        IStoreContext storeContext,
        IRepository<EmailAccount> emailAccountRepository,
        IRepository<MessageTemplate> messageTemplateRepository,
        IRepository<PackagingOption> packagingOptionRepository,
        IRepository<Store> storeRepository,
        IRepository<Picture> pictureRepository,
        IRepository<Product> productRepository,
        IRepository<ProductAttribute> productAttributeRepository,
        IRepository<ProductTag> productTagRepository,
        IRepository<ProductTemplate> productTemplateRepository,
        IRepository<Category> categoryRepository,
        IRepository<CategoryTemplate> categoryTemplateRepository,
        IRepository<TaxCategory> taxCategoryRepository,
        IRepository<UrlRecord> urlRecordRepository,
        IShippingService shippingService,
        IVendorService vendorService,
        ICarrierService carrierService,
        IEntityGroupService entityGroupService,
        IShippingManagerService shippingManagerService,
        IProductAttributeService productAttributeService,
        IPermissionService permissionService,
        IRepository<PermissionRecord> permissionRecordRepository,
        INopDataProvider dataProvider,
        ICustomerService customerService,
        IStaticCacheManager cacheManager)
    {
        _localizationService = localizationService;
        _settingService = settingService;
        _fileProvider = fileProvider;
        _emailAccountRepository = emailAccountRepository;
        _messageTemplateRepository = messageTemplateRepository;
        _storeContext = storeContext;
        _storeRepository = storeRepository;
        _packagingOptionRepository = packagingOptionRepository;
        _pictureRepository = pictureRepository;
        _productRepository = productRepository;
        _productAttributeRepository = productAttributeRepository;
        _productTagRepository = productTagRepository;
        _productTemplateRepository = productTemplateRepository;
        _categoryRepository = categoryRepository;
        _categoryTemplateRepository = categoryTemplateRepository;
        _taxCategoryRepository = taxCategoryRepository;
        _urlRecordRepository = urlRecordRepository;
        _shippingService = shippingService;
        _vendorService = vendorService;
        _carrierService = carrierService;
        _entityGroupService = entityGroupService;
        _shippingManagerService = shippingManagerService;
        _productAttributeService = productAttributeService;
        _permissionService = permissionService;
        _permissionRecordRepository = permissionRecordRepository;
        _dataProvider = dataProvider;
        _customerService = customerService;
        _cacheManager = cacheManager;
    }

    #endregion

    #region Utility

    /// <returns>A task that represents the asynchronous operation</returns>
    protected virtual async Task<T> InsertInstallationDataAsync<T>(T entity) where T : BaseEntity
    {
        return await _dataProvider.InsertEntityAsync(entity);
    }

    /// <returns>A task that represents the asynchronous operation</returns>
    protected virtual async Task InsertInstallationDataAsync<T>(params T[] entities) where T : BaseEntity
    {
        await _dataProvider.BulkInsertEntitiesAsync(entities);
    }

    /// <returns>A task that represents the asynchronous operation</returns>
    protected virtual async Task InsertInstallationDataAsync<T>(IList<T> entities) where T : BaseEntity
    {
        if (!entities.Any())
            return;

        await InsertInstallationDataAsync(entities.ToArray());
    }

    /// <returns>A task that represents the asynchronous operation</returns>
    protected virtual async Task UpdateInstallationDataAsync<T>(T entity) where T : BaseEntity
    {
        await _dataProvider.UpdateEntityAsync(entity);
    }

    /// <returns>A task that represents the asynchronous operation</returns>
    protected virtual async Task UpdateInstallationDataAsync<T>(IList<T> entities) where T : BaseEntity
    {
        if (!entities.Any())
            return;

        foreach (var entity in entities)
            await _dataProvider.UpdateEntityAsync(entity);
    }

    /// <returns>A task that represents the asynchronous operation</returns>
    protected virtual async Task<string> ValidateSeNameAsync<T>(T entity, string seName) where T : BaseEntity
    {
        //duplicate of ValidateSeName method of \Nop.Services\Seo\UrlRecordService.cs (we cannot inject it here)
        ArgumentNullException.ThrowIfNull(entity);

        //validation
        var okChars = "abcdefghijklmnopqrstuvwxyz1234567890 _-";
        seName = seName.Trim().ToLowerInvariant();

        var sb = new StringBuilder();
        foreach (var c in seName.ToCharArray())
        {
            var c2 = c.ToString();
            if (okChars.Contains(c2))
                sb.Append(c2);
        }

        seName = sb.ToString();
        seName = seName.Replace(" ", "-");
        while (seName.Contains("--"))
            seName = seName.Replace("--", "-");
        while (seName.Contains("__"))
            seName = seName.Replace("__", "_");

        //max length
        seName = CommonHelper.EnsureMaximumLength(seName, NopSeoDefaults.SearchEngineNameLength);

        //ensure this seName is not reserved yet
        var i = 2;
        var tempSeName = seName;
        while (true)
        {
            //check whether such slug already exists (and that is not the current entity)

            var query = from ur in _urlRecordRepository.Table
                        where tempSeName != null && ur.Slug == tempSeName
                        select ur;
            var urlRecord = await query.FirstOrDefaultAsync();

            var entityName = entity.GetType().Name;
            var reserved = urlRecord != null && !(urlRecord.EntityId == entity.Id && urlRecord.EntityName.Equals(entityName, StringComparison.InvariantCultureIgnoreCase));
            if (!reserved)
                break;

            tempSeName = $"{seName}-{i}";
            i++;
        }

        seName = tempSeName;

        return seName;
    }

    protected virtual string GetSamplesPath()
    {
        string filePath = _fileProvider.MapPath("~/Plugins/SSI.Shipping.Manager/Images/");
        return filePath;
    }

    static DateTime GetNextWeekday(DayOfWeek day)
    {
        DateTime result = DateTime.Now.AddDays(1);
        while (result.DayOfWeek != day)
            result = result.AddDays(1);
        return result;
    }

    protected virtual string ValidateSeName<T>(T entity, string name) where T : BaseEntity
    {
        //simplified and very fast (no DB calls) version of "ValidateSeName" method of UrlRecordService
        //we know that there's no same names of entities in sample data

        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        //validation
        var okChars = "abcdefghijklmnopqrstuvwxyz1234567890 _-";
        name = name.Trim().ToLowerInvariant();

        var sb = new StringBuilder();
        foreach (var c in name.ToCharArray())
        {
            var c2 = c.ToString();
            if (okChars.Contains(c2))
            {
                sb.Append(c2);
            }
        }

        name = sb.ToString();
        name = name.Replace(" ", "-");
        while (name.Contains("--"))
            name = name.Replace("--", "-");
        while (name.Contains("__"))
            name = name.Replace("__", "_");

        //max length
        name = CommonHelper.EnsureMaximumLength(name, NopSeoDefaults.SearchEngineNameLength);

        return name;
    }

    /// <returns>A task that represents the asynchronous operation</returns>
    private async Task AddProductTagAsync(Product product, string tag)
    {
        var productTag = _productTagRepository.Table.FirstOrDefault(pt => pt.Name == tag);

        if (productTag is null)
        {
            productTag = new ProductTag
            {
                Name = tag
            };

            await InsertInstallationDataAsync(productTag);

            //search engine name
            await InsertInstallationDataAsync(new UrlRecord
            {
                EntityId = productTag.Id,
                EntityName = nameof(ProductTag),
                LanguageId = 0,
                IsActive = true,
                Slug = ValidateSeName(productTag, productTag.Name)
            });
        }

        await InsertInstallationDataAsync(new ProductProductTagMapping { ProductTagId = productTag.Id, ProductId = product.Id });
    }

    private async Task DeleteProductTagAsync(string tag)
    {
        var productTag = _productTagRepository.Table.FirstOrDefault(pt => pt.Name == tag);
        if (productTag != null)
        {
            var urlRecord = _urlRecordRepository.Table.FirstOrDefault(u => u.Slug == ValidateSeName(productTag, productTag.Name));
            if (urlRecord != null)
                await _urlRecordRepository.DeleteAsync(urlRecord);

            await _productTagRepository.DeleteAsync(productTag);
        }
    }

    #endregion

    #region Methods

    public virtual async Task InstallPermissionsAsync(bool install)
    {

        if (install)
        {
            var permissions = new List<PermissionRecord>();

            PermissionRecord operateShipping = _systemHelper.GetAccessPermission(SystemHelper.AccessMode.Operate);
            var permissionRecord = (await _permissionService.GetAllPermissionRecordsAsync()).Where(x => x.Name == operateShipping.Name);
            if (permissionRecord == null || permissionRecord.Count() == 0)
            {
                await _permissionRecordRepository.InsertAsync(operateShipping);
                permissions.Add(operateShipping);
            }

            PermissionRecord superviseShipping = _systemHelper.GetAccessPermission(SystemHelper.AccessMode.Supervisor);
            permissionRecord = (await _permissionService.GetAllPermissionRecordsAsync()).Where(x => x.Name == superviseShipping.Name);
            if (permissionRecord == null || permissionRecord.Count() == 0)
            {
                await _permissionRecordRepository.InsertAsync(superviseShipping);
                permissions.Add(superviseShipping);
            }

            PermissionRecord manageShipping = _systemHelper.GetAccessPermission(SystemHelper.AccessMode.Manage);
            permissionRecord = (await _permissionService.GetAllPermissionRecordsAsync()).Where(x => x.Name == manageShipping.Name);
            if (permissionRecord == null || permissionRecord.Count() == 0)
            {
                await _permissionRecordRepository.InsertAsync(manageShipping);
                permissions.Add(manageShipping);
            }

            var customerRole = (await _customerService.GetAllCustomerRolesAsync()).Where(cr => cr.Name == "Administrators").FirstOrDefault();
            if (customerRole != null)
            {
                foreach (var permission in permissions)
                {
                    var mapping = await _permissionService.GetMappingByPermissionRecordIdAsync(permission.Id);
                    if (mapping != null && mapping.Count() == 0)
                    {
                        var prcrm = new PermissionRecordCustomerRoleMapping { CustomerRoleId = customerRole.Id, PermissionRecordId = permission.Id };
                        await _permissionService.InsertPermissionRecordCustomerRoleMappingAsync(prcrm);
                    }
                }

                await _cacheManager.ClearAsync();
            }
        }
        else
        {
            PermissionRecord superviseShipping = _systemHelper.GetAccessPermission(SystemHelper.AccessMode.Supervisor);
            var permissionRecords = (await _permissionService.GetAllPermissionRecordsAsync()).Where(x => x.Name == superviseShipping.Name);
            foreach (var record in permissionRecords)
                await _permissionRecordRepository.DeleteAsync(record);

            PermissionRecord manageShipping = _systemHelper.GetAccessPermission(SystemHelper.AccessMode.Manage);
            permissionRecords = (await _permissionService.GetAllPermissionRecordsAsync()).Where(x => x.Name == manageShipping.Name);
            foreach (var record in permissionRecords)
                await _permissionRecordRepository.DeleteAsync(record);

            PermissionRecord operateShipping = _systemHelper.GetAccessPermission(SystemHelper.AccessMode.Operate);
            permissionRecords = (await _permissionService.GetAllPermissionRecordsAsync()).Where(x => x.Name == operateShipping.Name);
            foreach (var record in permissionRecords)
                await _permissionRecordRepository.DeleteAsync(record);
        }
    }

    public virtual async Task InstallLocalisationAsync(bool install)
    {

        //locales
        var resourcesList = new Dictionary<string, string>
        {
            ["Plugins.Shipping.Manager.Manage"] = "Manage Shipping Rates",
            ["Plugins.Shipping.Manager.Methods"] = "Shipping methods",
            ["Plugins.Shipping.Manager.Carriers"] = "Carriers",
            ["Plugins.Shipping.Manager.Warehouses"] = "Warehouses",
            ["Plugins.Shipping.Manager.DatesAndRanges"] = "Dates and Ranges",
            ["Plugins.Shipping.Manager.PickupPointProviders"] = "Pick up Points",
            ["Plugins.Shipping.Manager.Shipments"] = "Shipments Processing",
            ["Plugins.Shipping.Manager.Sales"] = "Sales Processing",

            ["Plugins.Shipping.Manager.Carriers.AddNew"] = "Add new Carrier",
            ["Plugins.Shipping.Manager.Carriers.EditCarrierDetails"] = "Edit Carrier",
            ["Plugins.Shipping.Manager.Carriers.BackToList"] = "Back to carriers list",
            ["Plugins.Shipping.Manager.Carriers.Fields.Name"] = "Name",
            ["Plugins.Shipping.Manager.Carriers.Updated"] = "The carrier has been updated successfully.",
            ["Plugins.Shipping.Manager.Carriers.Added"] = "The new carrier has been added successfully.",
            ["Plugins.Shipping.Manager.Carriers.Deleted"] = "The new carrier has been deleted successfully.",
            ["Plugins.Shipping.Manager.Carrier.Active"] = "Search Active Only",
            ["Plugins.Shipping.Manager.Carrier.Active.Hint"] = "Only Carriers that are marked as Active will be displayed",

            ["Plugins.Shipping.Manager.SearchActive"] = "Search Active Only",
            ["Plugins.Shipping.Manager.SearchActive.Hint"] = "Only records that are marked as Active will be displayed",

            ["Activitylog.AddNewCarrier"] = "Add new carrier",
            ["Activitylog.EditCarrier"] = "Edit carrier",

            ["Plugins.Shipping.Manager.SearchName"] = "Search Name",
            ["Plugins.Shipping.Manager.SearchName.Hint"] = "Enter the name or part name and click Search",

            ["Plugins.Shipping.Manager.CutoffTimes"] = "Carrier Cut of times",
            ["Plugins.Shipping.Manager.CutOffTimes.Added"] = "The cut off time has been added successfully.",
            ["Plugins.Shipping.Manager.CutOffTimes.AddNew"] = "Add a new cut off time",
            ["Plugins.Shipping.Manager.CutOffTimes.BackToList"] = "back to cut off time list",
            ["Plugins.Shipping.Manager.CutOffTimes.Deleted"] = "The cut off time has been deleted successfully.",
            ["Plugins.Shipping.Manager.CutOffTimes.EditCutOffTimeDetails"] = "Edit cut off time details",
            ["Plugins.Shipping.Manager.CutOffTimes.Fields.DisplayOrder"] = "Display order",
            ["Plugins.Shipping.Manager.CutOffTimes.Fields.DisplayOrder.Hint"] = "The display order of this cut off time. 1 represents the top of the list.",
            ["Plugins.Shipping.Manager.CutOffTimes.Fields.Name"] = "Name",
            ["Plugins.Shipping.Manager.CutOffTimes.Fields.Name.Hint"] = "Enter cut off time name.",
            ["Plugins.Shipping.Manager.CutOffTimes.Fields.Name.Require"] = "Please provide a name.",
            ["Plugins.Shipping.Manager.CutOffTimes.Hint"] = "List of cut off times which will be available on the shipping selection page.",
            ["Plugins.Shipping.Manager.CutOffTimes.Updated"] = "The cut off time has been updated successfully.",
            ["Plugins.Shipping.Manager.CutOffTimes.Fields.Name.Required"] = "Please provide a name.",

            ["Plugins.Shipping.Manager.Fields.Name"] = "Name",
            ["Plugins.Shipping.Manager.Fields.Name.Hint"] = "Enter the name",
            ["Plugins.Shipping.Manager.Fields.AdminComment"] = "Administration Comment",
            ["Plugins.Shipping.Manager.Fields.AdminComment.Hint"] = "Enter a comment for Administration Use",
            ["Plugins.Shipping.Manager.Fields.AdditionalFixedCost"] = "Additional fixed cost",
            ["Plugins.Shipping.Manager.Fields.AdditionalFixedCost.Hint"] = "Specify an additional fixed cost per shopping cart for this option. Set to 0 if you don't want an additional fixed cost to be applied.",
            ["Plugins.Shipping.Manager.Fields.Carrier"] = "Carrier",
            ["Plugins.Shipping.Manager.Fields.Carrier.Hint"] = "If an asterisk is selected, then this rate will apply to all carriers",
            ["Plugins.Shipping.Manager.Fields.Active"] = "Search Active Only",
            ["Plugins.Shipping.Manager.Fields.Active.Hint"] = "Only Carriers that are marked as Active will be displayed",
            ["Plugins.Shipping.Manager.Fields.Country"] = "Country",
            ["Plugins.Shipping.Manager.Fields.Country.Hint"] = "If an asterisk is selected, then this rate will apply to all customers, regardless of the country.",
            ["Plugins.Shipping.Manager.Fields.DataHtml"] = "Data",
            ["Plugins.Shipping.Manager.Fields.LimitMethodsToCreated"] = "Limit shipping methods to configured ones",
            ["Plugins.Shipping.Manager.Fields.LimitMethodsToCreated.Hint"] = "If you check this option, then your customers will be limited to shipping options configured here. Otherwise, they'll be able to choose any existing shipping options even they are not configured here (zero shipping fee in this case).",
            ["Plugins.Shipping.Manager.Fields.ReturnValidOptionsIfThereAreAny"] = "Return valid options if there are any",
            ["Plugins.Shipping.Manager.Fields.ReturnValidOptionsIfThereAreAny.Hint"] = "If you check this option, then regardless of errors for some shipping methods the rates will be returned for any other methods configured.",
            ["Plugins.Shipping.Manager.Fields.LowerWeightLimit"] = "Lower weight limit",
            ["Plugins.Shipping.Manager.Fields.LowerWeightLimit.Hint"] = "Lower weight limit. This field can be used for \"per extra weight unit\" scenarios.",
            ["Plugins.Shipping.Manager.Fields.OrderSubtotalFrom"] = "Order subtotal from",
            ["Plugins.Shipping.Manager.Fields.OrderSubtotalFrom.Hint"] = "Enter the lower Order Subtotal limit for this rate to be applied.",
            ["Plugins.Shipping.Manager.Fields.OrderSubtotalTo"] = "Order subtotal to",
            ["Plugins.Shipping.Manager.Fields.OrderSubtotalTo.Hint"] = "Enter the upper Order Subtotal limit for this rate to be applied.",
            ["Plugins.Shipping.Manager.Fields.PercentageRateOfSubtotal"] = "Charge percentage (of Shipping Total)",
            ["Plugins.Shipping.Manager.Fields.PercentageRateOfSubtotal.Hint"] = "Percentage charged on top of the calcualted shipping total",
            ["Plugins.Shipping.Manager.Fields.Rate"] = "Rate",
            ["Plugins.Shipping.Manager.Fields.RatePerWeightUnit"] = "Rate per weight unit",
            ["Plugins.Shipping.Manager.Fields.RatePerWeightUnit.Hint"] = "Rate per weight unit.",
            ["Plugins.Shipping.Manager.Fields.ShippingMethod"] = "Shipping method",
            ["Plugins.Shipping.Manager.Fields.ShippingMethod.Hint"] = "Choose shipping method.",
            ["Plugins.Shipping.Manager.Fields.StateProvince"] = "State / province",
            ["Plugins.Shipping.Manager.Fields.StateProvince.Hint"] = "If an asterisk is selected, then this rate will apply to all customers from the given country, regardless of the state.",
            ["Plugins.Shipping.Manager.Fields.Store"] = "Store",
            ["Plugins.Shipping.Manager.Fields.Store.Hint"] = "If an asterisk is selected, then this rate will apply to all stores.",
            ["Plugins.Shipping.Manager.Fields.Warehouse"] = "Warehouse",
            ["Plugins.Shipping.Manager.Fields.Warehouse.Hint"] = "If an asterisk is selected, then this rate will apply to all warehouses.",
            ["Plugins.Shipping.Manager.Fields.WeightFrom"] = "Order weight from",
            ["Plugins.Shipping.Manager.Fields.WeightFrom.Hint"] = "Enter the lower weight limit for this rate to be applied.",
            ["Plugins.Shipping.Manager.Fields.WeightTo"] = "Order weight to",
            ["Plugins.Shipping.Manager.Fields.WeightTo.Hint"] = "Enter the upper weight limit for this rate to be applied.",
            ["Plugins.Shipping.Manager.Fields.CalculateCubicWeight"] = "Calculate Cubic Weight",
            ["Plugins.Shipping.Manager.Fields.CalculateCubicWeight.Hint"] = "Cubix weight will be calculated from the diminsions if enabled",
            ["Plugins.Shipping.Manager.Fields.CubicWeightFactor"] = "Cubic weight  factor",
            ["Plugins.Shipping.Manager.Fields.CubicWeightFactor.Hint"] = "Enter the Cubic Weight factor to apply to the diminsions calculation (H x L x W x factor)",
            ["Plugins.Shipping.Manager.Fields.Zip"] = "Postcode",
            ["Plugins.Shipping.Manager.Fields.Zip.Hint"] = "Postal code. If empty, then this rate will apply to all customers from the given country or state, regardless of the zip code.",
            ["Plugins.Shipping.Manager.Fields.ShippingRateComputationMethodSystemName"] = "Shipping plugin provider",
            ["Plugins.Shipping.Manager.Fields.ShippingRateComputationMethodSystemName.Hint"] = "Select the Shipping plugin provider (if required)",
            ["Plugins.Shipping.Manager.Fields.Active"] = "Active",
            ["Plugins.Shipping.Manager.Fields.Active.Hint"] = "Select if this Carrier is Active",
            ["Plugins.Shipping.Manager.Fields.FriendlyName"] = "Friendly Name",
            ["Plugins.Shipping.Manager.Fields.FriendlyName.Hint"] = "Enter the friendly name to be displayed to the customer when selcting rates",
            ["Plugins.Shipping.Manager.Fields.TransitDays"] = "Transit Days",
            ["Plugins.Shipping.Manager.Fields.TransitDays.Hint"] = "The number of days of delivery of the goods",
            ["Plugins.Shipping.Manager.Fields.SendFromAddress"] = "Send From",
            ["Plugins.Shipping.Manager.Fields.SendFromAddress.Hint"] = "Select the Sendcloud Send From Address for this rates",

            ["Plugins.Shipping.Manager.Fields.Description"] = "Description",
                ["Plugins.Shipping.Manager.Fields.Description.Hint"] = "Rate description override for Shipping Method Description",["Plugins.Shipping.Manager.Fixed"] = "Fixed Rate",
            ["Plugins.Shipping.Manager.ShippingByWeight"] = "By Weight",
            ["Plugins.Shipping.Manager.Formula"] = "Formula to calculate rates",
            ["Plugins.Shipping.Manager.Formula.Value"] = "{[additional fixed cost] + ([order total weight] - [lower weight limit]) * [rate per weight unit]} * [charge percentage]",
            ["Plugins.Shipping.Manager.AddRecord"] = "Add record",

            ["Plugins.Shipping.Manager.Fields.CarrierName"] = "Carrier",
            ["Plugins.Shipping.Manager.Fields.CarrierName.Hint"] = "Load shipments with products shipped by a specific carrier",
            ["Plugins.Shipping.Manager.Fields.VendorGroup"] = "Vendor Group",
            ["Plugins.Shipping.Manager.Fields.VendorGroup.Hint"] = "Select the Vendor Group",
            ["Plugins.Shipping.Manager.EntityGroup.Instruction.Hint"] = "Enter Instruction Hint",
            ["Plugins.Shipping.Manager.EntityGroup.Instruction"] = "Instructions:",
            ["Plugins.Shipping.Manager.Shipping.Methods.Description"] = "Shipping methods used by shipping providers",
            ["Plugins.Shipping.Manager.Fields.ShippingAddress"] = "Shipping Address",
            ["Plugins.Shipping.Manager.Fields.ShippingAddress.Hint"] = "The order shipping address",
            ["Plugins.Shipping.Manager.Fields.CutOffTime"] = "Cut off Time",
            ["Plugins.Shipping.Manager.Fields.CutOffTime.Hint"] = "Enter the carrier pickup Cut off Time",
            ["Plugins.Shipping.Manager.Fields.Keygroup"] = "Key Group",
            ["Plugins.Shipping.Manager.Fields.Keygroup.Hint"] = "Key Group",
            ["Plugins.Shipping.Manager.Fields.Key"] = "Key",
            ["Plugins.Shipping.Manager.Fields.Key.Hint"] = "Key",
            ["Plugins.Shipping.Manager.Fields.Value"] = "Value",
            ["Plugins.Shipping.Manager.Fields.Value.Hint"] = "Value",
            ["Plugins.Shipping.Manager.Fields.Entity"] = "Entity Id",
            ["Plugins.Shipping.Manager.Fields.Entity.Hint"] = "Entity Id",
            ["Plugins.Shipping.Manager.Fields.Vendor"] = "Vendor",
            ["Plugins.Shipping.Manager.Fields.DontDisplayShipped"] = "Dont Display Shipped",
            ["Plugins.Shipping.Manager.Fields.DontDisplayShipped.Hint"] = "Select to not display shipped orders",
            ["Plugins.Shipping.Manager.Fields.DontDisplayDelivered"] = "Dont Display Delivered",
            ["Plugins.Shipping.Manager.Fields.DontDisplayDelivered.Hint"] = "Select to not display delivered orders",

            ["Plugins.Shipping.Manager.SearchName"] = "Search Name",
            ["Plugins.Shipping.Manager.SearchName.Hint"] = "Enter the name or part name and click Search",

            ["Plugins.Shipping.Manager.EntityGroup"] = "Entity Groups",
            ["Plugins.Shipping.Manager.VendorScope.VendorScope"] = "Group Supervisor",
            ["Plugins.Shipping.Manager.VendorScope.SelectVendor"] = "Select the Group Vendor",
            ["Plugins.Shipping.Manager.VendorScope.SelectGroupVendor"] = "Select the Vendor",
            ["Plugins.Shipping.Manager.VendorScope.AllVendors"] = "All Vendors",
            ["Plugins.Shipping.Manager.VendorScope.CheckAll.Hint"] = "Select the vendor to display the associated entities",
            ["Plugins.Shipping.Manager.VendorScope.CheckAll"] = "Select the Group Vendor",
            ["Plugins.Shipping.Manager.Supervisor"] = "Supervisor",

            ["Plugins.Shipping.Manager.Sales.Values.Fields.Name"] = "Name",
            ["Plugins.Shipping.Manager.Sales.Values.Fields.AttributeDescription"] = "Product Details",
            ["Plugins.Shipping.Manager.Sales.Values.Fields.CustomerEmail"] = "Customer",
            ["Plugins.Shipping.Manager.Sales.Values.Fields.OrderId"] = "Order",
            ["Plugins.Shipping.Manager.Sales.Values.Fields.PaymentStatus"] = "Status",
            ["Plugins.Shipping.Manager.Sales.Values.Fields.PaymentDate"] = "Payment Date",
            ["Plugins.Shipping.Manager.Sales.Values.Fields.Price"] = "Price",
            ["Plugins.Shipping.Manager.Sales.Values.Fields.Quantity"] = "Quantity",
            ["Plugins.Shipping.Manager.Sales.Values.Fields.TotalPrice"] = "Total",
            ["Plugins.Shipping.Manager.Sales.Values.Fields.PaymentMethod"] = "Payment Method",
            ["Plugins.Shipping.Manager.Sales.Values.Fields.ShippingMethod"] = "Shipping Method",
            ["Plugins.Shipping.Manager.Sales.Values.fields.Action"] = "Edit",
            ["Plugins.Shipping.Manager.Sales.SetOrdersAsPaid"] = "Set as Paid (Selected)",
            ["Plugins.Shipping.Manager.Sales.SetOrdersAsApproved"] = "Set as Approved (Selected)",

            ["Plugins.Shipping.Manager.Admin.PdfReport"] = "PDF Report",
            ["Plugins.Shipping.Manager.Admin.PdfReport.All"] = "PDF Report All",
            ["Plugins.Shipping.Manager.Admin.PdfReport.Selected"] = "PDF Report Selected",
            ["Plugins.Shipping.Manager.Admin.PdfReport.NoOrders"] = "No Orders Found",

                            ["Plugins.Shipping.Manager.Confirm.PrintAllInvoices"] = "Please confirm you wish to print all Invoices found from selection",
                ["Plugins.Shipping.Manager.Confirm.PrintAllSales"] = "Please confirm you wish to print all Sales found from selection",
		["Plugins.Shipping.Manager.Admin.PdfReport"] = "Orders Report",
            ["Plugins.Shipping.Manager.Admin.PdfReport.PackagingReports"] = "Packing Reports",
            ["Plugins.Shipping.Manager.Admin.PdfReport.Category"] = "Category",
            ["Plugins.Shipping.Manager.Admin.PdfReport.Customer"] = "Customer",
            ["Plugins.Shipping.Manager.Admin.PdfReport.OrderDetails"] = "Order Details",
            ["Plugins.Shipping.Manager.Admin.PdfReport.PaidDate"] = "Paid Date",
            ["Plugins.Shipping.Manager.Admin.PdfReport.Order"] = "Order {0} : Shipment Cost {1}",
            ["Plugins.Shipping.Manager.Admin.PdfReport.Payment"] = "Payment",
            ["Plugins.Shipping.Manager.Admin.PdfReport.Method"] = "Method",
            ["Plugins.Shipping.Manager.Admin.PdfReport.Store"] = "Store: {0}",
            ["Plugins.Shipping.Manager.Admin.PdfReport.Date"] = "Date: {0}",
            ["Plugins.Shipping.Manager.Admin.PdfReport.NotPaid"] = "Not Paid",
            ["Plugins.Shipping.Manager.Admin.PdfReport.ShippingTotal"] = "Total Shipping:",
            ["Plugins.Shipping.Manager.Admin.PdfReport.ShipmentMethod"] = "Shipment Method: {0}",
            ["Plugins.Shipping.Manager.Admin.PdfReport.ShipmentName"] = "Send to: {0}",

            ["Plugins.Shipping.Manager.Sales.Fields.SearchProductName"] = "Search Product Name",
            ["Plugins.Shipping.Manager.Sales.Fields.FromDate"] = "Order Created Start Date",
            ["Plugins.Shipping.Manager.Sales.Fields.FromDate.Hint"] = "Select Start Date of Search",
            ["Plugins.Shipping.Manager.Sales.Fields.ToDate"] = "Order Created End Date",
            ["Plugins.Shipping.Manager.Sales.Fields.ToDate.Hint"] = "Select the End Date of Search",
            ["Plugins.Shipping.Manager.Sales.Fields.Ispay"] = "Order Not Paid",
            ["Plugins.Shipping.Manager.Sales.Fields.Ispay.Hint"] = "Select to display Orders Not Paid",
            ["Plugins.Shipping.Manager.Sales.Fields.OrderbyName"] = "Order by Name",
            ["Plugins.Shipping.Manager.Sales.Fields.OrderbyName.Hint"] = "Select Order by Name",
            ["Plugins.Shipping.Manager.Sales.Fields.PaymentMethod"] = "Payment Method",
            ["Plugins.Shipping.Manager.Sales.Fields.PaymentMethod.Hint"] = "Select Payment Method",
            ["Plugins.Shipping.Manager.Sales.Values.Fields.PaymentDate"] = "Payment Date",
            ["Plugins.Shipping.Manager.Sales.Values.Fields.Status"] = "Shipping Status",
            ["Plugins.Shipping.Manager.Sales.Values.Fields.Warehouse"] = "Warehouse",

            ["Plugins.Shipping.Manager.Sales.AddShipment"] = "Add",
            ["Plugins.Shipping.Manager.Sales.NoShipmentCreated"] = "No Shipment Created",
            ["Plugins.Shipping.Manager.Sales.NoWarehouseSelected"] = "No Warehouse for Shipment",
            ["Plugins.Shipping.Manager.Sales.EditShipment"] = "Edit",
            ["Plugins.Shipping.Manager.Sales.NoOrderItemsToShip"] = "There are no order items that can be shipped",
            ["Plugins.Shipping.Manager.Sales.NoWarehousesAvailable"] = "There are no warehouses availale for the order product items",
            ["Plugins.Shipping.Manager.Sales.ShipmentAdded"] = "An automatic shipment was added",

            ["Plugins.Shipping.Manager.Delivery.Shipment"] = "Shipment",

            ["Plugins.Shipping.Manager.Orders.Shipments.CanShip"] = "Shippment Sent",
            ["Plugins.Shipping.Manager.Orders.Shipments.CanDeliver"] = "Shipment Delivered",
            ["Plugins.Shipping.Manager.Orders.Shipments.ShippingMethod"] = "Shipment Method",
            ["Plugins.Shipping.Manager.Orders.Shipments.ShippingMethod.Hint"] = "Select the Shipping Method to process this shipment",
            ["Plugins.Shipping.Manager.Orders.Shipments.ShipmentId"] = "Shipment Id",
            ["Plugins.Shipping.Manager.Orders.Shipments.ShipmentId.Hint"] = "The carrier shipment idendifier",
            ["Plugins.Shipping.Manager.Orders.Shipments.ServicePointId"] = "Service Point Id",
            ["Plugins.Shipping.Manager.Orders.Shipments.ServicePointPOBoxNumber"] = "PO Box",

            ["Plugins.Shipping.Manager.Orders.Shipments.BackToShipmentList"] = "back to shipment processing",
            ["Plugins.Shipping.Manager.Orders.Shipments.Edit.Title"] = "Edit Shipment",
            ["Plugins.Shipping.Manager.Orders.Shipments.ShippingMethod.Button"] = "Set Method",
            ["Plugins.Shipping.Manager.Orders.Shipments.CreatePackageSelected"] = "Create Shipment Package",
            ["Plugins.Shipping.Manager.Orders.Shipments.PrintLabel"] = "Print Label",

            ["Plugins.Shipping.Manager.Orders.Shipments.PackagingOption.Button"] = "Set Packaging Option",
            ["Plugins.Shipping.Manager.Orders.Shipments.Label.View"] = "Print Label",
            ["Plugins.Shipping.Manager.Orders.Shipments.Refund.Button"] = "Request Refund",
            ["Plugins.Shipping.Manager.Orders.Shipments.Cancel.Button"] = "Cancel Shipment",
            ["Plugins.Shipping.Manager.Orders.Shipments.Refund.Error"] = "Error requesting refund - Check error log",
            ["Plugins.Shipping.Manager.Orders.Shipments.Cancel.Error"] = "Shipment Cancel Error",
            ["Plugins.Shipping.Manager.Orders.Shipments.Cancelled"] = "Shipment has been cancelled",

            ["Plugins.Shipping.Manager.Shipments.ScheduledShipDate"] = "Scheduled Ship Date",
            ["Plugins.Shipping.Manager.Shipments.ScheduledShipDate.Hint"] = "The date for the Scheduled package pickup",
            ["Plugins.Shipping.Manager.Shipments.ScheduledShipDate.Button"] = "Set Ship Date",

            ["Plugins.Shipping.Manager.Products.OrderQtyShipped"] = "Order Shipped",

            ["Plugins.Shipping.Manager.Products.Warehouse.ChooseQty"] = "{0} ({1} in stock, {2} reserved, {3} planned, {4} to ship)",

            ["Plugins.Shipping.Manager.Shipments.PrintPackagingSlips.All"] = "Print Report",
            ["Plugins.Shipping.Manager.Shipments.Packaging.Name"] = "Packaging Option",
            ["Plugins.Shipping.Manager.Shipments.Packaging.Name.Hint"] = "Select the Packaging Option for this shipment",
            ["Plugins.Shipping.Manager.Shipments.Packaging.Dimensions"] = "Package Dimensions",
            ["Plugins.Shipping.Manager.Shipments.Packaging.Dimensions.Hint"] = "The total dimensions of the package",
            ["Plugins.Shipping.Manager.Shipments.Packaging.TrackingNumber"] = "Tracking No.",


            ["Plugins.Shipping.Manager.Shipment.CreatePackageSelected"] = "Create Parcel (selected)",
            ["Plugins.Shipping.Manager.Shipment.Sendcloud.SelectShipment"] = "Please select a shipment to process",
            ["Plugins.Shipping.Manager.Shipment.Sendcloud.ParcelCreated"] = "Sendcloud Parcel Created",
            ["Plugins.Shipping.Manager.Shipment.Sendcloud.ServicePointError"] = "Sendcloud Service Point does not exist",

            ["Plugins.Shipping.Manager.Sendcloud.ServicePointAddress"] = "Service Point Address",
            ["Plugins.Shipping.Manager.Sendcloud.ChangeServicePoint"] = "Change service point",
            ["Plugins.Shipping.Manager.Sendcloud.PostBox"] = "Post Office Box",

            ["service point id"] = "Service Point Id",
            ["service point carrier"] = "Service Point Carrier",
            ["service point address"] = "Service Point Address",
            ["service point lat"] = "Service Point Latitude",
            ["service point long"] = "Service Point Longitude",

            ["Plugins.Shipping.Manager.Shipment.CanadaPost.ParcelCreated"] = "Canada Post Parcel Created",

            ["Plugins.Shipping.Manager.Shipment.FastWay.ParcelCreated"] = "Aramax Parcel Created",

            ["PDFPackagingSlip.Shipment"] = "Shipment #{0}",
            ["PDFPackagingSlip.Name"] = "Name: {0}",
            ["PDFPackagingSlip.ShippingMethod"] = "Shipping method: {0}",
            ["PDFPackagingSlip.ProductName"] = "Product Name",
            ["PDFPackagingSlip.SKU"] = "Stock Code SKU",
            ["PDFPackagingSlip.QTY"] = "Pick Quantity",
            ["PDFInvoice.Tax"] = "Tax:",
            ["PDFInvoice.OrderTotal"] = "Order total:",
            ["PDFPackagingSlip.Address"] = "Address: {0}",
            ["PDFPackagingSlip.Address2"] = "Address 2: {0}",
            ["PDFPackagingSlip.Company"] = "Company: {0}",
            ["PDFPackagingSlip.Order"] = "Order #{0}",
            ["PDFPackagingSlip.Phone"] = "Phone: {0}",
		["PDFPackagingSlip.Email"] = "Email: {0}",

                ["PDFInvoice.Address"] = "Address: {0}",
                ["PDFInvoice.Address2"] = "Address 2: {0}",
                ["PDFInvoice.BillingInformation"] = "Billing Information",
                ["PDFInvoice.Company"] = "Company: {0}",
                ["PDFInvoice.Discount"] = "Discount:",
                ["PDFInvoice.Fax"] = "Fax: {0}",
                ["PDFInvoice.GiftCardInfo"] = "Gift card({0}):",
                ["PDFInvoice.Name"] = "Name: {0}",
                ["PDFInvoice.Email"] = "Email: {0}",
                ["PDFInvoice.Order#"] = "Order# {0}",
                ["PDFInvoice.OrderDate"] = "Date: {0}",
                ["PDFInvoice.OrderNotes"] = "Order notes:",
                ["PDFInvoice.OrderNotes.CreatedOn"] = "Created on ",
                ["PDFInvoice.OrderNotes.Note"] = "Note ",
                ["PDFInvoice.OrderTotal"] = "Order total:",
                ["PDFInvoice.PaymentMethod"] = "Payment method:{0}",
                ["PDFInvoice.PaymentMethodAdditionalFee"] = "Payment Method Additional Fee:",
                ["PDFInvoice.Phone"] = "Phone: {0}",
                ["PDFInvoice.Pickup"] = "Pickup point:",
                ["PDFInvoice.Product(s)"] = "Order Items",
                ["PDFInvoice.ProductName"] = "Name",
                ["PDFInvoice.ProductPrice"] = "Price",
                ["PDFInvoice.ProductQuantity"] = "Qty",
                ["PDFInvoice.ProductTotal"] = "Total",
                ["PDFInvoice.RewardPoints"] = "{0} reward points: ",
                ["PDFInvoice.Shipping"] = "Shipping:",
                ["PDFInvoice.ShippingInformation"] = "Shipping Information",
                ["PDFInvoice.ShippingMethod"] = "Shipping method: {0}",
                ["PDFInvoice.SKU"] = "SKU",
                ["PDFInvoice.Sub-Total"] = "Sub-total:",
                ["PDFInvoice.Tax"] = "Tax:",
                ["PDFInvoice.TaxRate"] = "Tax: ",
                ["PDFInvoice.VATNumber"] = "VAT number: {0}",
                ["PDFInvoice.VendorName"] = "Vendor name ",

        };

        if (install)
        {
            //add or update locales

            await _localizationService.AddOrUpdateLocaleResourceAsync(resourcesList);

        }
        else
        {
            foreach (var item in resourcesList)
                await _localizationService.DeleteLocaleResourceAsync(item.Key);
        }
    }

    public virtual async Task InstallMessageTemaplatesAsync(bool install)
    {

        var eaGeneral = _emailAccountRepository.Table.FirstOrDefault();
        if (eaGeneral == null)
            throw new Exception("Default email account cannot be loaded");

        var messageTemplates = new List<MessageTemplate>
        {
            new MessageTemplate
            {
                Name = "ShippingManager.OrderShippmentCreated.VendorNotification",
                Subject = "%Store.Name%. Your Order shippment has been created",
                Body = $"<p>{Environment.NewLine}<a href=\"%Store.URL%\">%Store.Name%</a>{Environment.NewLine}<br />{Environment.NewLine}<br />{Environment.NewLine}Hello %Order.CustomerFullName%,{Environment.NewLine}<br />{Environment.NewLine}Thanks for buying from <a href=\"%Store.URL%\">%Store.Name%</a>. Below is the summary of the order.{Environment.NewLine}Your order has ben approved for payment - To make payment click this link <a href=\"%Store.URL%plugins/multisafepay/deferredpayment/%Order.OrderNumber%\">Make Payment</a>{ Environment.NewLine }<br />{Environment.NewLine}<br />{Environment.NewLine}Order Number: %Order.OrderNumber%{Environment.NewLine}<br />{Environment.NewLine}Order Details: <a target=\"_blank\" href=\"%Order.OrderURLForCustomer%\">%Order.OrderURLForCustomer%</a>{Environment.NewLine}<br />{Environment.NewLine}Date Ordered: %Order.CreatedOn%{Environment.NewLine}<br />{Environment.NewLine}<br />{Environment.NewLine}<br />{Environment.NewLine}<br />{Environment.NewLine}Billing Address{Environment.NewLine}<br />{Environment.NewLine}%Order.BillingFirstName% %Order.BillingLastName%{Environment.NewLine}<br />{Environment.NewLine}%Order.BillingAddress1%{Environment.NewLine}<br />{Environment.NewLine}%Order.BillingCity% %Order.BillingZipPostalCode%{Environment.NewLine}<br />{Environment.NewLine}%Order.BillingStateProvince% %Order.BillingCountry%{Environment.NewLine}<br />{Environment.NewLine}<br />{Environment.NewLine}<br />{Environment.NewLine}<br />{Environment.NewLine}%if (%Order.Shippable%) Shipping Address{Environment.NewLine}<br />{Environment.NewLine}%Order.ShippingFirstName% %Order.ShippingLastName%{Environment.NewLine}<br />{Environment.NewLine}%Order.ShippingAddress1%{Environment.NewLine}<br />{Environment.NewLine}%Order.ShippingCity% %Order.ShippingZipPostalCode%{Environment.NewLine}<br />{Environment.NewLine}%Order.ShippingStateProvince% %Order.ShippingCountry%{Environment.NewLine}<br />{Environment.NewLine}<br />{Environment.NewLine}Shipping Method: %Order.ShippingMethod%{Environment.NewLine}<br />{Environment.NewLine}<br />{Environment.NewLine} endif% %Order.Product(s)%{Environment.NewLine}</p>{Environment.NewLine}",
                IsActive = true,
                EmailAccountId = eaGeneral.Id
            }
        };

        if (install)
            await _messageTemplateRepository.InsertAsync(messageTemplates);
        else
        {
            var template = _messageTemplateRepository.Table.FirstOrDefault(pt => pt.Name == "ShippingManager.OrderShippmentCreated.VendorNotification");
            if (template != null)
                await _messageTemplateRepository.DeleteAsync(template);
        }
    }

    //public virtual async Task InstallPackagingOptionsAsync(bool install)
    //{
    //    if (install)
    //    {
    //        var packagingOptionBox = new Material
    //        {
    //            MaterialType = PackagingOptionType.Box,
    //            UseIndividually = true,
    //            Name = "Box - Wine Packing",
    //            Sku = "PO_BOX_W1",
    //            ShortDescription = "A box used for packing bottles of wine.",
    //            Price = 1M,
    //            IsShipEnabled = true,
    //            IsFreeShipping = false,
    //            Weight = 0.5M,
    //            Length = 30,
    //            Width = 40,
    //            Height = 40,
    //            ManageInventoryMethod = ManageInventoryMethod.ManageStock,
    //            StockQuantity = 10000,
    //            NotifyAdminForQuantityBelow = 5,
    //            AllowBackInStockSubscriptions = false,
    //            DisplayStockAvailability = true,
    //            LowStockActivity = LowStockActivity.DisableBuyButton,
    //            BackorderMode = BackorderMode.NoBackorders,
    //            OrderMinimumQuantity = 1,
    //            OrderMaximumQuantity = 10000,
    //            Published = true,
    //            CreatedOnUtc = DateTime.UtcNow,
    //            UpdatedOnUtc = DateTime.UtcNow
    //        };

    //        await InsertInstallationDataAsync(packagingOptionBox);
    //    }
    //    else
    //    {
    //        var packagingOptions = _packagingOptionRepository.Table.Where(p => p.Name == "Box - Wine Packing");
    //        foreach (var packagingOption in packagingOptions)
    //        {
    //            if (packagingOption.Deleted)
    //                continue;
    //            await _packagingOptionRepository.DeleteAsync(packagingOption);
    //        }
    //    }
    //}

    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual async Task InstallConfigurationAsync(bool install)
    {
        await InstallVendorConfigurationAsync(install, "One");
        await InstallVendorConfigurationAsync(install, "Two");
        await _entityGroupService.SetActiveVendorScopeAsync(0);
    }

    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual async Task InstallVendorConfigurationAsync(bool install, string optionName)
    {

        string vendorName = "Vendor " + optionName;
        string carrierName = "Carrier " + optionName;

        if (install)
        {

            // Vendor One Setup 


            var vendor = (await _vendorService.GetAllVendorsAsync()).Where(c => c.Name.Equals(vendorName)).FirstOrDefault();
            if (vendor == null)
            {
                vendor = new Vendor
                {
                    Name = vendorName,
                    Email = vendorName.Replace(" ","") + "Email@gmail.com",
                    Description = "Some description for " + vendorName,
                    AdminComment = string.Empty,
                    PictureId = 0,
                    Active = true,
                    DisplayOrder = 1,
                    PageSize = 6,
                    AllowCustomersToSelectPageSize = true,
                    PageSizeOptions = "6, 3, 9, 18",
                    PriceRangeFiltering = true,
                    ManuallyPriceRange = true,
                    PriceFrom = NopCatalogDefaults.DefaultPriceRangeFrom,
                    PriceTo = NopCatalogDefaults.DefaultPriceRangeTo,
                };

                await _vendorService.InsertVendorAsync(vendor);

                await InsertInstallationDataAsync(new UrlRecord
                {
                    EntityId = vendor.Id,
                    EntityName = nameof(Vendor),
                    LanguageId = 0,
                    IsActive = true,
                    Slug = await ValidateSeNameAsync(vendor, vendor.Name)
                });

                vendor = (await _vendorService.GetAllVendorsAsync()).Where(c => c.Name.Equals(vendorName)).FirstOrDefault();
                if (vendor != null)
                {
                    await _entityGroupService.SetActiveVendorScopeAsync(vendor.Id);

                    var carrier = (await _carrierService.GetAllCarriersAsync()).Where(x => x.Name.Contains(carrierName)).FirstOrDefault();
                    if (carrier == null)
                    {
                        carrier = new Carrier
                        {
                            Name = carrierName,
                            AdminComment = "New test " + carrierName,
                            AddressId = 0,
                            ShippingRateComputationMethodSystemName = ShippingManagerDefaults.SystemName,
                            Active = true,
                        };

                        await _carrierService.InsertCarrierAsync(carrier);
                    }

                    string smVendorName = vendorName + " Option A";
                    var shippingMethodVendor = (await _shippingService.GetAllShippingMethodsAsync()).Where(c => c.Name.Equals(smVendorName)).FirstOrDefault();
                    if (shippingMethodVendor == null)
                    {
                        shippingMethodVendor = new ShippingMethod
                        {
                            Name = smVendorName,
                            Description = "Shipping by " + smVendorName,
                            DisplayOrder = 1
                        };

                        await _shippingService.InsertShippingMethodAsync(shippingMethodVendor);
                    }

                    smVendorName = vendorName + " Option B";
                    shippingMethodVendor = (await _shippingService.GetAllShippingMethodsAsync()).Where(c => c.Name.Equals(smVendorName)).FirstOrDefault();
                    if (shippingMethodVendor == null)
                    {
                        shippingMethodVendor = new ShippingMethod
                        {
                            Name = smVendorName,
                            Description = "Shipping by " + smVendorName,
                            DisplayOrder = 1
                        };

                        await _shippingService.InsertShippingMethodAsync(shippingMethodVendor);
                    }

                    decimal price = 10;
                    if (vendor.Name.Contains("Two"))
                        price += 10;

                    var shippingManagerByWeightByTotal = new ShippingManagerByWeightByTotal
                    {
                        ShippingMethodId = shippingMethodVendor.Id,
                        CarrierId = carrier.Id,
                        WarehouseId = 0,
                        VendorId = vendor.Id,
                        WeightFrom = 0,
                        WeightTo = 1000000,
                        CalculateCubicWeight = false,
                        CubicWeightFactor = 0,
                        OrderSubtotalFrom = 0,
                        OrderSubtotalTo = 1000000,
                        AdditionalFixedCost = price,
                        CountryId = 0,
                        StateProvinceId = 0,
                        FriendlyName = null,
                        TransitDays = 2,
                        SendFromAddressId = 0
                    };

                    await _shippingManagerService.InsertShippingByWeightRecordAsync(shippingManagerByWeightByTotal);
                }
            }
        }
        else
        {
            var vendor = (await _vendorService.GetAllVendorsAsync()).Where(x => x.Name.Contains(vendorName)).FirstOrDefault();
            if (vendor != null)
            {
                await _entityGroupService.SetActiveVendorScopeAsync(vendor.Id);

                var shippingMethods = await _shippingService.GetAllShippingMethodsAsync();
                if (shippingMethods.Any())
                {
                    foreach(var shippingMethod in shippingMethods)
                        await _shippingService.DeleteShippingMethodAsync(shippingMethod);
                }
            
                var carrier = (await _carrierService.GetAllCarriersAsync()).Where(x => x.Name.Contains(carrierName)).FirstOrDefault();
                if (carrier != null && vendor != null)
                    await _carrierService.DeleteCarrierAsync(carrier);

                var rates = await _shippingManagerService.GetAllRatesAsync(vendor.Id);
                foreach (var rate in rates)
                    await _shippingManagerService.DeleteShippingByWeightRecordAsync(rate);

                var entityGroups = _entityGroupService.GetAllEntityGroupsForVendor(vendor.Id);
                foreach (var entityGroup in entityGroups)
                    await _entityGroupService.DeleteEntityGroupAsync(entityGroup);

                await _vendorService.DeleteVendorAsync(vendor);
            }
        }
    }

    #endregion

}
