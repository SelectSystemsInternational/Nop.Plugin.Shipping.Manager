using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using Nop.Core;
using Nop.Core.Configuration;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Shipping;
using Nop.Core.Events;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Services;
using Nop.Services.Authentication.MultiFactor;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Gdpr;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Plugins;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Areas.Admin.Models.Settings;
using Nop.Web.Framework.Controllers;

using Nop.Plugin.Apollo.Integrator.Services;
using Nop.Core.Domain.Directory;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Framework;

namespace Nop.Plugin.Shipping.Manager.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
public partial class ShippingSettingController : BasePluginController
{

    #region Fields

    protected readonly AppSettings _appSettings;
    protected readonly IAddressService _addressService;
    protected readonly ICustomerActivityService _customerActivityService;
    protected readonly ICustomerService _customerService;
    protected readonly INopDataProvider _dataProvider;
    protected readonly IEncryptionService _encryptionService;
    protected readonly IEventPublisher _eventPublisher;
    protected readonly IGenericAttributeService _genericAttributeService;
    protected readonly IGdprService _gdprService;
    protected readonly ILocalizedEntityService _localizedEntityService;
    protected readonly ILocalizationService _localizationService;
    protected readonly IMultiFactorAuthenticationPluginManager _multiFactorAuthenticationPluginManager;
    protected readonly INopFileProvider _fileProvider;
    protected readonly INotificationService _notificationService;
    protected readonly IOrderService _orderService;
    protected readonly IPermissionService _permissionService;
    protected readonly IPictureService _pictureService;
    protected readonly ISettingModelFactory _settingModelFactory;
    protected readonly ISettingService _settingService;
    protected readonly IStoreContext _storeContext;
    protected readonly IStoreService _storeService;
    protected readonly IWorkContext _workContext;
    protected readonly IUploadService _uploadService;
    protected readonly ICurrencyService _currencyService;
    protected readonly CurrencySettings _currencySettings;
    protected readonly IAddressModelFactory _addressModelFactory;
    protected readonly IEntityGroupService _entityGroupService;
    private static readonly char[] _separator = [','];

    #endregion

    #region Ctor

    public ShippingSettingController(AppSettings appSettings,
        IAddressService addressService,
        ICustomerActivityService customerActivityService,
        ICustomerService customerService,
        INopDataProvider dataProvider,
        IEncryptionService encryptionService,
        IEventPublisher eventPublisher,
        IGenericAttributeService genericAttributeService,
        IGdprService gdprService,
        ILocalizedEntityService localizedEntityService,
        ILocalizationService localizationService,
        IMultiFactorAuthenticationPluginManager multiFactorAuthenticationPluginManager,
        INopFileProvider fileProvider,
        INotificationService notificationService,
        IOrderService orderService,
        IPermissionService permissionService,
        IPictureService pictureService,
        ISettingModelFactory settingModelFactory,
        ISettingService settingService,
        IStoreContext storeContext,
        IStoreService storeService,
        IWorkContext workContext,
        IUploadService uploadService,
        ICurrencyService currencyService,
        CurrencySettings currencySettings,
        IAddressModelFactory addressModelFactory,
        IEntityGroupService entityGroupService)
    {
        _appSettings = appSettings;
        _addressService = addressService;
        _customerActivityService = customerActivityService;
        _customerService = customerService;
        _dataProvider = dataProvider;
        _encryptionService = encryptionService;
        _eventPublisher = eventPublisher;
        _genericAttributeService = genericAttributeService;
        _gdprService = gdprService;
        _localizedEntityService = localizedEntityService;
        _localizationService = localizationService;
        _multiFactorAuthenticationPluginManager = multiFactorAuthenticationPluginManager;
        _fileProvider = fileProvider;
        _notificationService = notificationService;
        _orderService = orderService;
        _permissionService = permissionService;
        _pictureService = pictureService;
        _settingModelFactory = settingModelFactory;
        _settingService = settingService;
        _storeContext = storeContext;
        _storeService = storeService;
        _workContext = workContext;
        _uploadService = uploadService;
        _currencyService = currencyService;
        _currencySettings = currencySettings;
        _addressModelFactory = addressModelFactory;
        _entityGroupService = entityGroupService;
    }

    #endregion

    #region Utilities

    #endregion

    #region Methods

    public virtual async Task<IActionResult> Shipping()
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageSettings))
            return AccessDeniedView();

        //prepare model
        var model = await PrepareShippingSettingsModelAsync();

        return View("~/Plugins/SSI.Shipping.Manager/Views/Settings/ShippingSettings.cshtml", model);
    }

    [HttpPost]
    public virtual async Task<IActionResult> Shipping(ShippingSettingsModel model)
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageSettings))
            return AccessDeniedView();

        if (ModelState.IsValid)
        {
            //load settings for a chosen store scope
            var vendorScope = await _entityGroupService.GetActiveVendorScopeAsync();
            var shippingSettings = await _settingService.LoadSettingAsync<ShippingSettings>(vendorScope);
            shippingSettings = model.ToSettings(shippingSettings);

            //we do not clear cache after each setting update.
            //this behavior can increase performance because cached settings will not be cleared 
            //and loaded from database after each update
            await _settingService.SaveSettingOverridablePerStoreAsync(shippingSettings, x => x.AllowPickupInStore, model.AllowPickupInStore_OverrideForStore, vendorScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(shippingSettings, x => x.UseWarehouseLocation, model.UseWarehouseLocation_OverrideForStore, vendorScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(shippingSettings, x => x.FreeShippingOverXEnabled, model.FreeShippingOverXEnabled_OverrideForStore, vendorScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(shippingSettings, x => x.FreeShippingOverXValue, model.FreeShippingOverXValue_OverrideForStore, vendorScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(shippingSettings, x => x.FreeShippingOverXIncludingTax, model.FreeShippingOverXIncludingTax_OverrideForStore, vendorScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(shippingSettings, x => x.ConsiderAssociatedProductsDimensions, model.ConsiderAssociatedProductsDimensions_OverrideForStore, vendorScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(shippingSettings, x => x.ShippingSorting, model.ShippingSorting_OverrideForStore, vendorScope, false);

            if (model.ShippingOriginAddress_OverrideForStore || vendorScope == 0)
            {
                //update address
                var addressId = await _settingService.SettingExistsAsync(shippingSettings, x => x.ShippingOriginAddressId, vendorScope) ?
                    shippingSettings.ShippingOriginAddressId : 0;
                var originAddress = await _addressService.GetAddressByIdAsync(addressId) ??
                    new Address
                    {
                        CreatedOnUtc = DateTime.UtcNow
                    };
                //update ID manually (in case we're in multi-store configuration mode it'll be set to the shared one)
                model.ShippingOriginAddress.Id = addressId;
                originAddress = model.ShippingOriginAddress.ToEntity(originAddress);
                if (originAddress.Id > 0)
                    await _addressService.UpdateAddressAsync(originAddress);
                else
                    await _addressService.InsertAddressAsync(originAddress);
                shippingSettings.ShippingOriginAddressId = originAddress.Id;

                await _settingService.SaveSettingAsync(shippingSettings, x => x.ShippingOriginAddressId, vendorScope, false);
            }
            else if (vendorScope > 0)
                await _settingService.DeleteSettingAsync(shippingSettings, x => x.ShippingOriginAddressId, vendorScope);

            //now clear settings cache
            await _settingService.ClearCacheAsync();

            //activity log
            await _customerActivityService.InsertActivityAsync("EditSettings", await _localizationService.GetResourceAsync("ActivityLog.EditSettings"));

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Configuration.Updated"));

            return RedirectToAction("Shipping");
        }

        //prepare model
        model = await _settingModelFactory.PrepareShippingSettingsModelAsync(model);

        //if we got this far, something failed, redisplay form
        return View(model);
    }

    /// <summary>
    /// Prepare shipping settings model
    /// </summary>
    /// <param name="model">Shipping settings model</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the shipping settings model
    /// </returns>
    public virtual async Task<ShippingSettingsModel> PrepareShippingSettingsModelAsync(ShippingSettingsModel model = null)
    {
        //load settings for a chosen store scope
        var vendorScope = await _entityGroupService.GetActiveVendorScopeAsync();
        var shippingSettings = await _settingService.LoadSettingAsync<ShippingSettings>(vendorScope);

        //fill in model values from the entity
        model ??= shippingSettings.ToSettingsModel<ShippingSettingsModel>();

        //fill in additional values (not existing in the entity)
        model.ActiveStoreScopeConfiguration = vendorScope;
        model.PrimaryStoreCurrencyCode = (await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId))?.CurrencyCode;
        model.SortShippingValues = await shippingSettings.ShippingSorting.ToSelectListAsync();

        //fill in overridden values
        if (vendorScope > 0)
        {
            model.AllowPickupInStore_OverrideForStore = await _settingService.SettingExistsAsync(shippingSettings, x => x.AllowPickupInStore, vendorScope);
            model.UseWarehouseLocation_OverrideForStore = await _settingService.SettingExistsAsync(shippingSettings, x => x.UseWarehouseLocation, vendorScope);
            model.FreeShippingOverXEnabled_OverrideForStore = await _settingService.SettingExistsAsync(shippingSettings, x => x.FreeShippingOverXEnabled, vendorScope);
            model.FreeShippingOverXValue_OverrideForStore = await _settingService.SettingExistsAsync(shippingSettings, x => x.FreeShippingOverXValue, vendorScope);
            model.FreeShippingOverXIncludingTax_OverrideForStore = await _settingService.SettingExistsAsync(shippingSettings, x => x.FreeShippingOverXIncludingTax, vendorScope);
            model.ConsiderAssociatedProductsDimensions_OverrideForStore = await _settingService.SettingExistsAsync(shippingSettings, x => x.ConsiderAssociatedProductsDimensions, vendorScope);
            model.ShippingOriginAddress_OverrideForStore = await _settingService.SettingExistsAsync(shippingSettings, x => x.ShippingOriginAddressId, vendorScope);
            model.ShippingSorting_OverrideForStore = await _settingService.SettingExistsAsync(shippingSettings, x => x.ShippingSorting, vendorScope);
        }

        //prepare shipping origin address
        var originAddress = await _addressService.GetAddressByIdAsync(shippingSettings.ShippingOriginAddressId);
        if (originAddress != null)
            model.ShippingOriginAddress = originAddress.ToModel(model.ShippingOriginAddress);
        await _addressModelFactory.PrepareAddressModelAsync(model.ShippingOriginAddress, originAddress);
        model.ShippingOriginAddress.ZipPostalCodeRequired = true;

        return model;
    }

    #endregion
}