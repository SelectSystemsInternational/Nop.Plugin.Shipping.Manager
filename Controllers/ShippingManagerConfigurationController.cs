using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Logging;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Vendors;
using Nop.Data;
using Nop.Plugin.Apollo.Integrator;
using Nop.Plugin.Apollo.Integrator.Services;
using Nop.Plugin.Shipping.Manager.Domain;
using Nop.Plugin.Shipping.Manager.ExportImport;
using Nop.Plugin.Shipping.Manager.Models;
using Nop.Plugin.Shipping.Manager.Services;
using Nop.Plugin.Shipping.Manager.Settings;
using Nop.Services;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.ExportImport;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Shipping;
using Nop.Services.Stores;
using Nop.Services.Vendors;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using SendCloudApi.Net.Models;

namespace Nop.Plugin.Shipping.Manager.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
public class ShippingManagerConfigurationController : BasePluginController
{

    #region Fields

    protected readonly CurrencySettings _currencySettings;
    protected readonly ShippingManagerSettings _shippingManagerSettings;
    protected readonly ICountryService _countryService;
    protected readonly ICurrencyService _currencyService;
    protected readonly ILocalizationService _localizationService;
    protected readonly IMeasureService _measureService;
    protected readonly IPermissionService _permissionService;
    protected readonly ISettingService _settingService;
    protected readonly IStateProvinceService _stateProvinceService;
    protected readonly IStoreService _storeService;
    protected readonly MeasureSettings _measureSettings;
    protected readonly IShippingService _shippingService;
    protected readonly IShippingManagerService _shippingManagerService;
    protected readonly ICarrierService _carrierService;
    protected readonly INotificationService _notificationService;
    protected readonly IStoreContext _storeContext;
    protected readonly IWorkContext _workContext;
    protected readonly IEntityGroupService _entityGroupService;
    protected readonly IShippingManagerInstallService _shippingManagerInstallService;
    protected readonly ILogger _logger;
    protected readonly IVendorService _vendorService;
    protected readonly IExportImportManager _exportImportManager;
    protected readonly ISendcloudService _sendcloudService;
    protected readonly IFastwayService _fastwayService;
    protected readonly ICanadaPostService _canadaPostService;
    protected readonly ShippingSettings _shippingSettings;
    protected readonly IGenericAttributeService _genericAttributeService;
    protected readonly IRepository<LocaleStringResource> _lsrRepository;

    SystemHelper _systemHelper = new SystemHelper();

    #endregion

    #region Ctor

    public ShippingManagerConfigurationController(CurrencySettings currencySettings,
        ShippingManagerSettings shippingManagerSettings,
        ICountryService countryService,
        ICurrencyService currencyService,
        ILocalizationService localizationService,
        IMeasureService measureService,
        IPermissionService permissionService,
        ISettingService settingService,
        IStateProvinceService stateProvinceService,
        IStoreService storeService,
        MeasureSettings measureSettings,
        IShippingService shippingService,
        IShippingManagerService shippingManagerService,
        ICarrierService carrierService,
        INotificationService notificationService,
        IStoreContext storeContext,
        IWorkContext workContext,
        IEntityGroupService entityGroupService,
        IShippingManagerInstallService shippingManagerInstallService,
        ILogger logger,
        IVendorService vendorService,
        IExportImportManager exportImportManager,
        ISendcloudService sendcloudService,
        IFastwayService fastwayService,
        ICanadaPostService canadaPostService,
        ShippingSettings shippingSettings,
        IGenericAttributeService genericAttributeService,
        IRepository<LocaleStringResource> lsrRepository)
    {
        _currencySettings = currencySettings;
        _shippingManagerSettings = shippingManagerSettings;
        _countryService = countryService;
        _currencyService = currencyService;
        _localizationService = localizationService;
        _measureService = measureService;
        _permissionService = permissionService;
        _settingService = settingService;
        _stateProvinceService = stateProvinceService;
        _storeService = storeService;
        _measureSettings = measureSettings;
        _shippingService = shippingService;
        _shippingManagerService = shippingManagerService;
        _carrierService = carrierService;
        _notificationService = notificationService;
        _storeContext = storeContext;
        _workContext = workContext;
        _entityGroupService = entityGroupService;
        _shippingManagerInstallService = shippingManagerInstallService;
        _logger = logger;
        _vendorService = vendorService;
        _exportImportManager = exportImportManager;
        _sendcloudService = sendcloudService;
        _fastwayService = fastwayService;
        _canadaPostService = canadaPostService;
        _shippingSettings = shippingSettings;
        _genericAttributeService = genericAttributeService;
        _lsrRepository = lsrRepository;
    }

    #endregion

    #region Configure Methods

    public async Task<IActionResult> Configure()
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageShippingSettings))
            return AccessDeniedView();

        //load settings for a chosen store scope
        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var shippingManagerSettings = await _settingService.LoadSettingAsync<ShippingManagerSettings>(storeScope);

        bool access = await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Configure, shippingManagerSettings.PublicKey, shippingManagerSettings.PrivateKey);

            var model = new ConfigurationModel
            {
                ActiveStoreScopeConfiguration = storeScope,
                Enabled = shippingManagerSettings.Enabled,
                PrivateKey = shippingManagerSettings.PrivateKey,
                PublicKey = shippingManagerSettings.PublicKey,
                DeleteTablesonUninstall = shippingManagerSettings.DeleteTablesonUninstall,
                DeleteConfigurationDataonUninstall = shippingManagerSettings.DeleteConfigurationDataonUninstall,
                InternationalOperationsEnabled = shippingManagerSettings.InternationalOperationsEnabled,
                TestMode = shippingManagerSettings.TestMode,
                OrderByDate = shippingManagerSettings.OrderByDate,
                ProcessingModeId = shippingManagerSettings.ProcessingMode,
                ProcessingModeValues = await shippingManagerSettings.ProcessingMode.ToSelectListAsync(),
                UsePackagingSystem = shippingManagerSettings.UsePackagingSystem,
                PackagingOptions = shippingManagerSettings.PackagingOptions,
                EncryptServicePointPost = shippingManagerSettings.EncryptServicePointPost,
                SetAsShippedWhenAnnounced = shippingManagerSettings.SetAsShippedWhenAnnounced,
                DisplayManualOperations = shippingManagerSettings.DisplayManualOperations,
                ShippingOptionDisplayId = shippingManagerSettings.ShippingOptionDisplay,
                ShippingOptionDisplayValues = await shippingManagerSettings.ShippingOptionDisplay.ToSelectListAsync(),
                CheckoutOperationModeId = shippingManagerSettings.CheckoutOperationMode,
                CheckoutOperationModeValues = await shippingManagerSettings.CheckoutOperationMode.ToSelectListAsync(),
            };

        if (model.Enabled)
        {
            if (!access)
                model.Enabled = false;
        }

        //Prepare available ignore services list
        if (!String.IsNullOrEmpty(shippingManagerSettings.AvailableApiServices))
        {
            var list = shippingManagerSettings.AvailableApiServices.Split(',').ToList();
            int count = 1;
            foreach (var name in list)
            {
                var item = new SelectListItem
                {
                    Text = name,
                    Value = count.ToString()
                };

                model.AvailableApiServices.Add(item);
                count++;
            }
        }

        //Prepare list of ignored services
        if (!String.IsNullOrEmpty(shippingManagerSettings.ApiServices))
        {
            IList<string> ignoreServicesIds = shippingManagerSettings.ApiServices.Split(',').ToList();

            model.ApiServicesIds = new List<int>();
            foreach (var id in ignoreServicesIds)
            {
                if (model.AvailableApiServices != null)
                {
                    foreach (var service in model.AvailableApiServices)
                    {
                        if (service.Text == id)
                        {
                            int value = int.Parse(service.Value);
                            model.ApiServicesIds.Add(value);
                        }
                    }
                }
            }
        }

            if (model.Enabled)
            {
                if (!access)
                    model.Enabled = false;
            }

            if (storeScope > 0)
            {
                model.Enabled_OverrideForStore = await _settingService.SettingExistsAsync(shippingManagerSettings, x => x.Enabled, storeScope);
                model.ApiServices_OverrideForStore = await _settingService.SettingExistsAsync(shippingManagerSettings, x => x.ApiServices, storeScope);
                model.OrderByDate_OverrideForStore = await _settingService.SettingExistsAsync(shippingManagerSettings, x => x.OrderByDate, storeScope);
                model.ProcessingMode_OverrideForStore = await _settingService.SettingExistsAsync(shippingManagerSettings, x => x.ProcessingMode, storeScope);
                model.InternationalOperationsEnabled_OverrideForStore = await _settingService.SettingExistsAsync(shippingManagerSettings, x => x.InternationalOperationsEnabled, storeScope);
                model.PackagingOptions_OverrideForStore = await _settingService.SettingExistsAsync(shippingManagerSettings, x => x.PackagingOptions, storeScope);
                model.EncryptServicePointPost_OverrideForStore = _settingService.SettingExists(shippingManagerSettings, x => x.EncryptServicePointPost, storeScope);
                model.SetAsShippedWhenAnnounced_OverrideForStore = _settingService.SettingExists(shippingManagerSettings, x => x.SetAsShippedWhenAnnounced, storeScope);
                model.ShippingOptionDisplay_OverrideForStore = _settingService.SettingExists(shippingManagerSettings, x => x.ShippingOptionDisplay, storeScope);
                model.DisplayManualOperations_OverrideForStore = _settingService.SettingExists(shippingManagerSettings, x => x.DisplayManualOperations, storeScope);
                model.CheckoutOperationMode_OverrideForStore = _settingService.SettingExists(shippingManagerSettings, x => x.CheckoutOperationMode, storeScope);
            }

        model.SendcloudApiSettings = await PrepareModel(await _settingService.LoadSettingAsync<SendcloudApiSettings>(storeScope), storeScope);
        model.AramexApiSettings = await PrepareModel(await _settingService.LoadSettingAsync<AramexApiSettings>(storeScope), storeScope);
        model.CanadaPostApiSettings = await PrepareModel(await _settingService.LoadSettingAsync<CanadaPostApiSettings>(storeScope), storeScope);

        if (!access)
        {
            string currentUrl = _systemHelper.GetDomainNameFromHost((await _storeContext.GetCurrentStoreAsync()).Url);
            _notificationService.ErrorNotification("Demo Version has Expired - Please enter the Licence Key");
            string message = string.Format("Plugin {0} not licenced for Store {1} with PublicKey {2} and PrivateKey {3}",
                "SSI.Shipping.Manager", currentUrl, shippingManagerSettings.PublicKey, shippingManagerSettings.PrivateKey);
            await _logger.InformationAsync(message);
        }
         

        return View("~/Plugins/SSI.Shipping.Manager/Views/Configure.cshtml", model);
    }

    [HttpPost]
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        //load settings for a chosen store scope
        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var shippingManagerSettings = await _settingService.LoadSettingAsync<ShippingManagerSettings>(storeScope);

        bool installed = shippingManagerSettings.Enabled;
        bool enabled = model.Enabled;
        if (!enabled)
        {
            bool access = await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Manage, model.PublicKey, model.PrivateKey);
            if (!access)
                model.Enabled = false;
        }

        //save settings
        shippingManagerSettings.Enabled = model.Enabled;
        shippingManagerSettings.PublicKey = model.PublicKey;

        shippingManagerSettings.DeleteTablesonUninstall = model.DeleteTablesonUninstall;
        shippingManagerSettings.DeleteConfigurationDataonUninstall = model.DeleteConfigurationDataonUninstall;
        shippingManagerSettings.InternationalOperationsEnabled = model.InternationalOperationsEnabled;
        shippingManagerSettings.OrderByDate = model.OrderByDate;
        shippingManagerSettings.TestMode = model.TestMode;
        shippingManagerSettings.ProcessingMode = model.ProcessingModeId;
        shippingManagerSettings.UsePackagingSystem = model.UsePackagingSystem;
        shippingManagerSettings.PackagingOptions = model.PackagingOptions;
        shippingManagerSettings.EncryptServicePointPost = model.EncryptServicePointPost;
        shippingManagerSettings.SetAsShippedWhenAnnounced = model.SetAsShippedWhenAnnounced;
        shippingManagerSettings.DisplayManualOperations = model.DisplayManualOperations;
        shippingManagerSettings.ShippingOptionDisplay = model.ShippingOptionDisplayId;
        shippingManagerSettings.CheckoutOperationMode = model.CheckoutOperationModeId;

        ///Load the available services which can be ignored
        if (!String.IsNullOrEmpty(shippingManagerSettings.AvailableApiServices))
        {
            var list = shippingManagerSettings.AvailableApiServices.Split(',').ToList();
            int count = 1;
            foreach (var name in list)
            {
                var item = new SelectListItem
                {
                    Text = name,
                    Value = count.ToString()
                };

                model.AvailableApiServices.Add(item);
                count++;
            }
        }

        ///Load the currently ignored services
        if (model.ApiServicesIds != null)
        {
            string str = "";
            bool first = true;
            foreach (var id in model.ApiServicesIds)
            {
                if (model.AvailableApiServices != null)
                {
                    foreach (var service in model.AvailableApiServices)
                    {
                        if (int.Parse(service.Value) == id)
                        {
                            if (!first)
                                str += ",";
                            str += service.Text;
                            first = false;
                        }
                    }
                }
            }

            model.ApiServices = str;
        }

        shippingManagerSettings.ApiServices = model.ApiServices;

        await _settingService.SaveSettingAsync(shippingManagerSettings, x => x.PublicKey);

        await _settingService.SaveSettingAsync(shippingManagerSettings, x => x.DeleteTablesonUninstall);
        await _settingService.SaveSettingAsync(shippingManagerSettings, x => x.DeleteConfigurationDataonUninstall);
        await _settingService.SaveSettingAsync(shippingManagerSettings, x => x.TestMode);

        await _settingService.SaveSettingOverridablePerStoreAsync(shippingManagerSettings, x => x.Enabled, model.Enabled_OverrideForStore, storeScope, true);
        await _settingService.SaveSettingOverridablePerStoreAsync(shippingManagerSettings, x => x.OrderByDate, model.OrderByDate_OverrideForStore, storeScope, true);
        await _settingService.SaveSettingOverridablePerStoreAsync(shippingManagerSettings, x => x.ApiServices, model.ApiServices_OverrideForStore, storeScope, true);
        await _settingService.SaveSettingOverridablePerStoreAsync(shippingManagerSettings, x => x.ProcessingMode, model.ProcessingMode_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(shippingManagerSettings, x => x.InternationalOperationsEnabled, model.InternationalOperationsEnabled_OverrideForStore, storeScope, true);
        await _settingService.SaveSettingOverridablePerStoreAsync(shippingManagerSettings, x => x.UsePackagingSystem, model.UsePackagingSystem_OverrideForStore, storeScope, true);
        await _settingService.SaveSettingOverridablePerStoreAsync(shippingManagerSettings, x => x.PackagingOptions, model.PackagingOptions_OverrideForStore, storeScope, true);
        await _settingService.SaveSettingOverridablePerStoreAsync(shippingManagerSettings, x => x.EncryptServicePointPost, model.EncryptServicePointPost_OverrideForStore, storeScope, true);
        await _settingService.SaveSettingOverridablePerStoreAsync(shippingManagerSettings, x => x.SetAsShippedWhenAnnounced, model.SetAsShippedWhenAnnounced_OverrideForStore, storeScope, true);
        await _settingService.SaveSettingOverridablePerStoreAsync(shippingManagerSettings, x => x.DisplayManualOperations, model.DisplayManualOperations_OverrideForStore, storeScope, true);
        await _settingService.SaveSettingOverridablePerStoreAsync(shippingManagerSettings, x => x.ShippingOptionDisplay, model.ShippingOptionDisplay_OverrideForStore, storeScope, true);
        await _settingService.SaveSettingOverridablePerStoreAsync(shippingManagerSettings, x => x.CheckoutOperationMode, model.CheckoutOperationMode_OverrideForStore, storeScope, true);

    //Only install setup data for All Stores

        if (storeScope == 0)
        {
            if (model.Enabled && !installed)
            {
                await _shippingManagerInstallService.InstallLocalisationAsync(true);
                //await _shippingManagerInstallService.InstallPackagingOptionsAsync(true);
            }
            else if (!model.Enabled && installed)
            {
                await _shippingManagerInstallService.InstallLocalisationAsync(false);
                //await _shippingManagerInstallService.InstallPackagingOptionsAsync(false);
            }
        }

        //now clear settings cache
        await _settingService.ClearCacheAsync();

        var systemWarningModel = new List<Nop.Web.Areas.Admin.Models.Common.SystemWarningModel>();
        await _shippingManagerService.PreparePluginsWarningModelAsync(systemWarningModel);
        foreach (var warning in systemWarningModel)
        {
            await _logger.InsertLogAsync(LogLevel.Information, warning.Text);
        }

        //if (_shippingManagerSettings.UsePackagingSystem)
        //    await GetPack();

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

        return await Configure();
    }

    public async Task<ApiSettingsModel> PrepareModel(ApiSettings settings, int storeScope)
    {

        //common settings
        var model = new ApiSettingsModel();

        //some of specific settings

        model.ActiveStoreScopeConfiguration = storeScope;

        model.ApiKey = settings.ApiKey;
        model.ApiSecret = settings.ApiSecret;
        model.Username = settings.Username;
        model.Password = settings.Password;
        model.AuthenticationURL = settings.AuthenticationURL;
        model.HostURL = settings.HostURL;
        model.ContractId = settings.ContractId;
        model.ShipmentOptions = settings.ShipmentOptions;
        model.CustomerNumber = settings.CustomerNumber;
        model.MoBoCN = settings.MoBoCN;
        model.TestMode = settings.TestMode;

        if (storeScope > 0)
        {
            //load settings for a chosen store scope
            model.ApiKey_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.ApiKey, storeScope);
            model.ApiSecret_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.ApiSecret, storeScope);
            model.Username_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.Username, storeScope);
            model.Password_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.Password, storeScope);
            model.AuthenticationURL_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.AuthenticationURL, storeScope);
            model.CustomerNumber_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.CustomerNumber, storeScope);
            model.MoBoCN_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.MoBoCN, storeScope);
            model.ContractId_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.ContractId, storeScope);
            model.ShipmentOptions_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.ShipmentOptions, storeScope);
            model.HostURL_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.HostURL, storeScope);
        }

        if (_shippingManagerSettings.UseWarehousesConfiguration)
        {
            var warehouses = await _shippingService.GetAllWarehousesAsync();
            foreach (var warehouse in warehouses)
            {
                var whs = new WarehouseSetup()
                {
                    WarehouseId = warehouse.Id.ToString(),
                    Name = warehouse.Name,
                };

                string testMessage = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Configure.Aramex.Tested"); //ToDo
                GenericAttribute ga = (await _genericAttributeService.GetAttributesForEntityAsync(warehouse.Id, "Warehouse")).FirstOrDefault(m => m.Key == "ShippingManagerWarehouseSetting");
                try
                {
                    ShippingManagerWarehouseSetting kws = null;
                    if (ga != null)
                        kws = JsonConvert.DeserializeObject<ShippingManagerWarehouseSetting>(ga.Value);

                    if (kws != null)
                    {
                        whs.ApiKey = kws.ApiKey;
                        whs.ApiSecret = kws.ApiSecret;
                        if (model.HostURL.Contains("sendcloud"))
                        {
                            whs.APITestResult = "Not tested";
                        }
                        else if (model.HostURL.Contains("fastway"))
                        {
                            var quote = await _fastwayService.GetTestQuote(whs.ApiKey, whs.ApiSecret);
                            if (quote != null)
                            {
                                testMessage = testMessage + " Price : " + quote.Price.ToString() + " Tax: " + quote.Tax.ToString() + " Total: " + quote.Total.ToString();
                                whs.APITestResult = testMessage;
                            }
                        }
                        else if (model.HostURL.Contains("canadapost"))
                        {
                            //var quote = await _fastwayService.GetTestQuote(whs.ApiKey, whs.ApiSecret);
                            //if (quote != null)
                            //{
                            //    testMessage = testMessage + " Price : " + quote.Price.ToString() + " Tax: " + quote.Tax.ToString() + " Total: " + quote.Total.ToString();
                            //    whs.APITestResult = testMessage;
                            //}
                        }
                        else
                            whs.APITestResult = "Not tested";
                    }
                }
                catch (Exception exc)
                {
                    string message = "Shipping Manager - Test Aramex : " + exc.Message;
                    _notificationService.ErrorNotification(message); // Cannot add value because header 'Authorization' does not support multiple values.
                }

                model.WarehouseSetup.Add(whs);

                try
                {
                    string message = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Configure.Aramex.Tested"); //ToDo
                    if (!string.IsNullOrEmpty(whs.ApiKey) && !string.IsNullOrEmpty(whs.ApiKey))
                    {
                        var quote = await _fastwayService.GetTestQuote(whs.ApiKey, whs.ApiSecret);
                        if (quote != null)
                            whs.APITestResult = message + " Price : " + quote.Price.ToString() + " Tax: " + quote.Tax.ToString() + " Total: " + quote.Total.ToString();
                    }
                }
                catch (Exception exc)
                {
                    string message = "Shipping Manager - Test Aramex : " + exc.Message;
                    _notificationService.ErrorNotification(message); // Cannot add value because header 'Authorization' does not support multiple values.
                }

            }
        }

        return model;
    }

    #endregion

    #region SendCloud

    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    [HttpPost, ActionName("Configure")]
    [FormValueRequired("saveSendCloudConfiguration")]
    public async Task<IActionResult> SaveSendCloudConfiguration(ConfigurationModel model)
    {
        //load settings for a chosen store scope
        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();

        var shippingManagerSettings = await _settingService.LoadSettingAsync<ShippingManagerSettings>(storeScope);

        var sendcloudApiSettings = await _settingService.LoadSettingAsync<SendcloudApiSettings>(storeScope);

        sendcloudApiSettings.ApiKey = model.SendcloudApiSettings.ApiKey;
        sendcloudApiSettings.ApiSecret = model.SendcloudApiSettings.ApiSecret;
        sendcloudApiSettings.AuthenticationURL = model.SendcloudApiSettings.AuthenticationURL;
        sendcloudApiSettings.HostURL = model.SendcloudApiSettings.HostURL;
        sendcloudApiSettings.TestMode = model.SendcloudApiSettings.TestMode;

        await _settingService.SaveSettingOverridablePerStoreAsync(sendcloudApiSettings, x => x.ApiKey, model.SendcloudApiSettings.ApiKey_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(sendcloudApiSettings, x => x.ApiSecret, model.SendcloudApiSettings.ApiSecret_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(sendcloudApiSettings, x => x.AuthenticationURL, model.SendcloudApiSettings.AuthenticationURL_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(sendcloudApiSettings, x => x.HostURL, model.SendcloudApiSettings.HostURL_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingAsync(sendcloudApiSettings, x => x.TestMode);

        //Only install setup data for All Stores

        if (storeScope == 0 && _shippingManagerSettings.UseWarehousesConfiguration)
        {

        }

        //now clear settings cache
        await _settingService.ClearCacheAsync();

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

        return await Configure();
    }

    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    [HttpPost, ActionName("Configure")]
    [FormValueRequired("loadSendCloudConfiguration")]
    public async Task<IActionResult> LoadSendCloudConfiguration(ConfigurationModel model)
    {
        //load settings for a chosen store scope
        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();

        var shippingManagerSettings = await _settingService.LoadSettingAsync<ShippingManagerSettings>(storeScope);

        var settings = await _settingService.LoadSettingAsync<SendcloudApiSettings>(storeScope);

        //Only install setup data for All Stores

        if (storeScope == 0)
        {
            var servicePointClient = new SendCloudApi.Net.SendCloudApi(settings.ApiKey, settings.ApiSecret);

            await _sendcloudService.SendCloudUpdateAsync(servicePointClient, true);
        }

        //now clear settings cache
        await _settingService.ClearCacheAsync();

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Configure.Sendcloud.ConfigurationUpdated"));

        return await Configure();

    }

    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    [HttpPost, ActionName("Configure")]
    [FormValueRequired("loadSendCloudCarriers")]
    public async Task<IActionResult> LoadSendCloudCarriers(ConfigurationModel model)
    {
        //load settings for a chosen store scope
        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();

        var shippingManagerSettings = await _settingService.LoadSettingAsync<ShippingManagerSettings>(storeScope);

        var settings = await _settingService.LoadSettingAsync<SendcloudApiSettings>(storeScope);

        //Only install setup data for All Stores

        if (storeScope == 0)
        {
            var servicePointClient = new SendCloudApi.Net.SendCloudApi(settings.ApiKey, settings.ApiSecret);

            await _sendcloudService.SendCloudUpdateAsync(servicePointClient, false);
        }

        //now clear settings cache
        await _settingService.ClearCacheAsync();

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Configure.Sendcloud.ConfigurationUpdated"));

        return await Configure();

    }

    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    [HttpPost, ActionName("Configure")]
    [FormValueRequired("validateSendCloudConfiguration")]
    public async Task<IActionResult> ValidateSendCloudConfiguration(ConfigurationModel model)
    {
        var errors = new List<string>();

        //load settings for a chosen store scope
        var storeId = await _entityGroupService.GetActiveStoreScopeConfiguration();

        var shippingManagerSettings = await _settingService.LoadSettingAsync<ShippingManagerSettings>(storeId);

        int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

        var settings = await _settingService.LoadSettingAsync<SendcloudApiSettings>(storeId);

        //Only install setup data for All Stores

        var servicePointClient = new SendCloudApi.Net.SendCloudApi(settings.ApiKey, settings.ApiSecret);
        errors = await _sendcloudService.SendCloudValidateConfigurationAsync(servicePointClient, storeId, vendorId);

        if (errors.Count() == 0)
            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Configure.Sendcloud.ConfigurationVerified"));
        else
        {
            string message = string.Empty;
            foreach (var error in errors)
                message += error + "; ";

            _notificationService.ErrorNotification(message);
        }

        return await Configure();
    }

    #endregion

    #region Aramex

    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    [HttpPost, ActionName("Configure")]
    [FormValueRequired("saveAramexConfiguration")]
    public async Task<IActionResult> SaveAramexConfiguration(ConfigurationModel model)
    {
        //load settings for a chosen store scope
        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();

        var shippingManagerSettings = await _settingService.LoadSettingAsync<ShippingManagerSettings>(storeScope);

        var aramexApiSettings = await _settingService.LoadSettingAsync<AramexApiSettings>(storeScope);

        aramexApiSettings.ApiKey = model.AramexApiSettings.ApiKey;
        aramexApiSettings.ApiSecret = model.AramexApiSettings.ApiSecret;
        aramexApiSettings.AuthenticationURL = model.AramexApiSettings.AuthenticationURL;
        aramexApiSettings.HostURL = model.AramexApiSettings.HostURL;
        aramexApiSettings.TestMode = model.AramexApiSettings.TestMode;

        await _settingService.SaveSettingOverridablePerStoreAsync(aramexApiSettings, x => x.ApiKey, model.AramexApiSettings.ApiKey_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(aramexApiSettings, x => x.ApiSecret, model.AramexApiSettings.ApiSecret_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(aramexApiSettings, x => x.AuthenticationURL, model.AramexApiSettings.AuthenticationURL_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(aramexApiSettings, x => x.HostURL, model.AramexApiSettings.HostURL_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingAsync(aramexApiSettings, x => x.TestMode);

        //Only install setup data for All Stores

        if (storeScope == 0 && _shippingManagerSettings.UseWarehousesConfiguration)
        {
            if (model.AramexApiSettings.WarehouseSetup != null)
            {
                foreach (var whs in model.AramexApiSettings.WarehouseSetup)
                {
                    try
                    {
                        int warehouseId = Int32.Parse(whs.WarehouseId);
                        Warehouse wh = await _shippingService.GetWarehouseByIdAsync(warehouseId);
                        if (wh == null)
                            continue;

                        ShippingManagerWarehouseSetting kws = new ShippingManagerWarehouseSetting();
                        kws.ApiKey = whs.ApiKey;
                        kws.ApiSecret = whs.ApiSecret;
                        GenericAttribute ga = (await _genericAttributeService.GetAttributesForEntityAsync(wh.Id, "Warehouse")).FirstOrDefault(m => m.Key == "ShippingManagerWarehouseSetting");
                        if (ga == null)
                        {
                            ga = new GenericAttribute();
                            ga.EntityId = wh.Id;
                            ga.Key = "ShippingManagerWarehouseSetting";
                            ga.KeyGroup = "Warehouse";
                            ga.Value = JsonConvert.SerializeObject(kws);
                            await _genericAttributeService.InsertAttributeAsync(ga);
                        }
                        else
                        {
                            ga.Value = JsonConvert.SerializeObject(kws);
                            await _genericAttributeService.UpdateAttributeAsync(ga);
                        }
                    }
                    catch (Exception ex)
                    {
                        await _logger.ErrorAsync("Shipping Manager - Warehouse setting error", ex);
                    }
                }
            }
        }

        //now clear settings cache
        await _settingService.ClearCacheAsync();

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

        return await Configure();
    }

    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    [HttpPost, ActionName("Configure")]
    [FormValueRequired("loadAramexConfiguration")]
    public async Task<IActionResult> LoadAramexConfiguration(ConfigurationModel model)
    {
        //load settings for a chosen store scope
        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();

        var shippingManagerSettings = await _settingService.LoadSettingAsync<ShippingManagerSettings>(storeScope);

        var settings = await _settingService.LoadSettingAsync<SendcloudApiSettings>(storeScope);

        //Only install setup data for All Stores

        if (storeScope == 0)
        {
            var services = await _fastwayService.GetServices();

            await _fastwayService.AramexUpdateAsync(services, true);

        }

        //now clear settings cache
        await _settingService.ClearCacheAsync();

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Configure.Sendcloud.ConfigurationUpdated"));

        return await Configure();

    }

    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    [HttpPost, ActionName("Configure")]
    [FormValueRequired("testAramexConfiguration")]
    public async Task<IActionResult> TestAramexConfiguration(ConfigurationModel model)
    {
        //load settings for a chosen store scope
        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();

        var shippingManagerSettings = await _settingService.LoadSettingAsync<ShippingManagerSettings>(storeScope);

        var settings = await _settingService.LoadSettingAsync<AramexApiSettings>(storeScope);

        //Only install setup data for All Stores

        try
        {
            string message = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Configure.Aramex.Tested"); //ToDo
            var quote = await _fastwayService.GetTestQuote();
            message = message + " Price : " + quote.Price.ToString() + " Tax: " + quote.Tax.ToString() + " Total: " + quote.Total.ToString();
            _notificationService.SuccessNotification(message);
        }
        catch (Exception exc)
        {
            string message = "Shipping Manager - Test Aramex : " + exc.Message;
            _notificationService.ErrorNotification(message);
        }

        return await Configure();
    }

    #endregion

    #region Canada Post

    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    [HttpPost, ActionName("Configure")]
    [FormValueRequired("saveCanadaPostConfiguration")]
    public async Task<IActionResult> SaveCanadaPostConfiguration(ConfigurationModel model)
    {
        //load settings for a chosen store scope
        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();

        var shippingManagerSettings = await _settingService.LoadSettingAsync<ShippingManagerSettings>(storeScope);

        var canadaPostApiSettings = await _settingService.LoadSettingAsync<CanadaPostApiSettings>(storeScope);

        canadaPostApiSettings.Username = model.CanadaPostApiSettings.Username;
        canadaPostApiSettings.Password = model.CanadaPostApiSettings.Password;
        canadaPostApiSettings.AuthenticationURL = model.CanadaPostApiSettings.AuthenticationURL;
        canadaPostApiSettings.CustomerNumber = model.CanadaPostApiSettings.CustomerNumber;
        canadaPostApiSettings.MoBoCN = model.CanadaPostApiSettings.MoBoCN;
        canadaPostApiSettings.ContractId = model.CanadaPostApiSettings.ContractId;
        canadaPostApiSettings.ShipmentOptions = model.CanadaPostApiSettings.ShipmentOptions;
        canadaPostApiSettings.TestMode = model.CanadaPostApiSettings.TestMode;

        await _settingService.SaveSettingOverridablePerStoreAsync(canadaPostApiSettings, x => x.Username, model.CanadaPostApiSettings.Username_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(canadaPostApiSettings, x => x.Password, model.CanadaPostApiSettings.Password_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(canadaPostApiSettings, x => x.AuthenticationURL, model.CanadaPostApiSettings.AuthenticationURL_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(canadaPostApiSettings, x => x.CustomerNumber, model.CanadaPostApiSettings.CustomerNumber_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(canadaPostApiSettings, x => x.MoBoCN, model.CanadaPostApiSettings.MoBoCN_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(canadaPostApiSettings, x => x.ContractId, model.CanadaPostApiSettings.ContractId_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(canadaPostApiSettings, x => x.ShipmentOptions, model.CanadaPostApiSettings.ShipmentOptions_OverrideForStore, storeScope, false);


        await _settingService.SaveSettingAsync(canadaPostApiSettings, x => x.TestMode);

        //Only install setup data for All Stores

        if (storeScope == 0 && _shippingManagerSettings.UseWarehousesConfiguration)
        {
            if (model.AramexApiSettings.WarehouseSetup != null)
            {
                foreach (var whs in model.AramexApiSettings.WarehouseSetup)
                {
                    try
                    {
                        int warehouseId = Int32.Parse(whs.WarehouseId);
                        Warehouse wh = await _shippingService.GetWarehouseByIdAsync(warehouseId);
                        if (wh == null)
                            continue;

                        ShippingManagerWarehouseSetting kws = new ShippingManagerWarehouseSetting();
                        kws.ApiKey = whs.ApiKey;
                        kws.ApiSecret = whs.ApiSecret;
                        GenericAttribute ga = (await _genericAttributeService.GetAttributesForEntityAsync(wh.Id, "Warehouse")).FirstOrDefault(m => m.Key == "ShippingManagerWarehouseSetting");
                        if (ga == null)
                        {
                            ga = new GenericAttribute();
                            ga.EntityId = wh.Id;
                            ga.Key = "ShippingManagerWarehouseSetting";
                            ga.KeyGroup = "Warehouse";
                            ga.Value = JsonConvert.SerializeObject(kws);
                            await _genericAttributeService.InsertAttributeAsync(ga);
                        }
                        else
                        {
                            ga.Value = JsonConvert.SerializeObject(kws);
                            await _genericAttributeService.UpdateAttributeAsync(ga);
                        }
                    }
                    catch (Exception ex)
                    {
                        await _logger.ErrorAsync("Shipping Manager - Warehouse setting error", ex);
                    }
                }
            }
        }

        //now clear settings cache
        await _settingService.ClearCacheAsync();

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

        return await Configure();
    }

    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    [HttpPost, ActionName("Configure")]
    [FormValueRequired("loadCanadaPostConfiguration")]
    public async Task<IActionResult> LoadCanadaPostConfiguration(ConfigurationModel model)
    {
        var errors = new List<string>();

        //load settings for a chosen store scope
        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();

        var storeId = await _entityGroupService.GetActiveStoreScopeConfiguration();

        var shippingManagerSettings = await _settingService.LoadSettingAsync<ShippingManagerSettings>(storeId);

        int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

        var settings = await _settingService.LoadSettingAsync<CanadaPostApiSettings>(storeScope);

        //Only install setup data for All Stores

        if (storeScope == 0)
        {

            (_, errors) = await _canadaPostService.CanadaPostValidateConfigurationAsync(storeId, vendorId);         

            if (errors.Count() == 0)
            {
                await _canadaPostService.CanadaPostUpdateAsync();
                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Configure.CanadaPost.ConfigurationUpdated"));
            }
            else
            {
                string message = string.Empty;
                foreach (var error in errors)
                    message += error + "; ";

                _notificationService.ErrorNotification(message);
            }
        }

        //now clear settings cache
        await _settingService.ClearCacheAsync();

        return await Configure();

    }

    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    [HttpPost, ActionName("Configure")]
    [FormValueRequired("validateCanadaPostConfiguration")]
    public async Task<IActionResult> ValidateCanadaPostConfiguration(ConfigurationModel model)
    {
        string response = string.Empty;
        var errors = new List<string>();

        //load settings for a chosen store scope
        var storeId = await _entityGroupService.GetActiveStoreScopeConfiguration();

        var shippingManagerSettings = await _settingService.LoadSettingAsync<ShippingManagerSettings>(storeId);

        int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

        var settings = await _settingService.LoadSettingAsync<SendcloudApiSettings>(storeId);

        //Only install setup data for All Stores

        (response, errors) = await _canadaPostService.CanadaPostValidateConfigurationAsync(storeId, vendorId);

        if (errors.Count() == 0)
        {
            string htmlResponse = response.Replace("\r\n\r\n", " - ");
            htmlResponse = htmlResponse.Replace("\r\n", " - ");
            htmlResponse = htmlResponse.Replace(" - - ", " - ");
            string message = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Configure.CanadaPost.ConfigurationVerified") + " - " + htmlResponse;
            _notificationService.SuccessNotification(message);
        }
        else
        {
            string message = string.Empty;
            foreach (var error in errors)
                message += error + "; ";

            _notificationService.ErrorNotification(message);
        }

        return await Configure();
    }

    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    [HttpPost, ActionName("Configure")]
    [FormValueRequired("testCanadaPostConfiguration")]
    public async Task<IActionResult> TestCanadaPostConfiguration(ConfigurationModel model)
    {
        //load settings for a chosen store scope
        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();

        var shippingManagerSettings = await _settingService.LoadSettingAsync<ShippingManagerSettings>(storeScope);

        var settings = await _settingService.LoadSettingAsync<AramexApiSettings>(storeScope);

        //Only install setup data for All Stores

        try
        {
            string message = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Configure.CanadaPost.Tested"); //ToDo
            //var quote = await _fastwayService.GetTestQuote();
            //message = message + " Price : " + quote.Price.ToString() + " Tax: " + quote.Tax.ToString() + " Total: " + quote.Total.ToString();
            _notificationService.SuccessNotification(message);
        }
        catch (Exception exc)
        {
            string message = "Shipping Manager - Test CanadaPost : " + exc.Message;
            _notificationService.ErrorNotification(message);
        }

        return await Configure();
    }

    #endregion

    #region Generate Language Pack

    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    [HttpPost, ActionName("Configure")]
    [FormValueRequired("Generate")]

    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task<IActionResult> Generate(ConfigurationModel model)
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageLanguages))
            return AccessDeniedView();

        try
        {
            var xml = await ExportResourcesToXmlAsync(await _workContext.GetWorkingLanguageAsync());
            return File(Encoding.UTF8.GetBytes(xml), "application/xml", "Apollo_language_pack.xml");
        }
        catch (Exception exc)
        {
            await _notificationService.ErrorNotificationAsync(exc);
            return RedirectToAction("Configure");
        }
    }

    /// <summary>
    /// Export language resources to XML
    /// </summary>
    /// <param name="language">Language</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the result in XML format
    /// </returns>
    public virtual async Task<string> ExportResourcesToXmlAsync(Language language)
    {
        if (language == null)
            throw new ArgumentNullException(nameof(language));

        var settings = new XmlWriterSettings
        {
            Async = true,
            Encoding = Encoding.UTF8,
            ConformanceLevel = ConformanceLevel.Auto
        };

        await using var stream = new MemoryStream();
        await using var xmlWriter = XmlWriter.Create(stream, settings);

        await xmlWriter.WriteStartDocumentAsync();
        await xmlWriter.WriteStartElementAsync("Language");
        await xmlWriter.WriteAttributeStringAsync("Name", language.Name);
        await xmlWriter.WriteAttributeStringAsync("SupportedVersion", NopVersion.CURRENT_VERSION);

        var resources = await GetAllResourcesAsync(language.Id);
        foreach (var resource in resources)
        {
            await xmlWriter.WriteStartElementAsync("LocaleResource");
            await xmlWriter.WriteAttributeStringAsync("Name", resource.ResourceName);
            await xmlWriter.WriteElementStringAsync("Value", null, resource.ResourceValue);
            await xmlWriter.WriteEndElementAsync();
        }

        await xmlWriter.WriteEndElementAsync();
        await xmlWriter.WriteEndDocumentAsync();
        await xmlWriter.FlushAsync();

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    protected virtual async Task<IList<LocaleStringResource>> GetAllResourcesAsync(int languageId)
    {
        var locales = await _lsrRepository.GetAllAsync(query =>
        {
            return from l in query
                   orderby l.ResourceName
                   where l.LanguageId == languageId &&
                         (l.ResourceName.Contains("Plugins.Shipping.Manager") ||
                         l.ResourceName.Contains("Shipping.CanadaPost") ||
                         l.ResourceName.Contains("Shipping.Aramex") ||
                         l.ResourceName.Contains("Shipping.Sendcloud"))
                   select l;
        });

        return locales;
    }

    #endregion

    #region Bin Packing

    //public async Task<BinPackResult> GetPack()
    //{

    //    var packagingOption = await _packagingOptionService.GetPackagingOptionByIdAsync(1);
    //    if (packagingOption != null)
    //    {
    //        // Define the size of bin
    //        var binWidth = packagingOption.Width;
    //        var binHeight = packagingOption.Height;
    //        var binDepth = packagingOption.Length;

    //        // Define the cuboids to pack
    //        var parameter = new BinPackParameter(binWidth, binHeight, binDepth, new[]
    //        {
    //            new Cuboid(100, 300, 100),
    //            new Cuboid(100, 300, 100),
    //            new Cuboid(100, 300, 100),
    //            new Cuboid(100, 300, 100),
    //            new Cuboid(100, 300, 100),
    //            new Cuboid(100, 300, 100),
    //            new Cuboid(100, 300, 100),
    //            new Cuboid(100, 300, 100),
    //            new Cuboid(100, 300, 100),
    //            new Cuboid(100, 300, 100),
    //            new Cuboid(100, 300, 100),
    //            new Cuboid(100, 300, 100),
    //        });

    //        // Create a bin packer instance
    //        // The default bin packer will test all algorithms and try to find the best result
    //        // BinPackerVerifyOption is used to avoid bugs, it will check whether the result is correct
    //        var binPacker = BinPacker.GetDefault(BinPackerVerifyOption.BestOnly);

    //        // The result contains bins which contains packed cuboids whith their coordinates
    //        var result = binPacker.Pack(parameter);

    //        return result;
    //    }

    //    return null;

    //}

    #endregion

}
