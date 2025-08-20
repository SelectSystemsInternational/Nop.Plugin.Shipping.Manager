using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Directory;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Shipping;
using Nop.Services.Stores;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;

using Nop.Plugin.Shipping.Manager.Domain;
using Nop.Plugin.Shipping.Manager.Models.Carrier;
using Nop.Plugin.Shipping.Manager.Services;
using Nop.Plugin.Shipping.Manager.Factories;
using Nop.Plugin.Shipping.Manager.Settings;

using Nop.Plugin.Apollo.Integrator;

namespace Nop.Plugin.Shipping.Manager.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
public class CarriersController : BasePluginController
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
    protected readonly ICarrierModelFactory _carrierModelFactory;
    protected readonly IAddressService _addressService;
    protected readonly ICustomerActivityService _customerActivityService;
    protected readonly INotificationService _notificationService;
    protected readonly IShippingPluginManager _shippingPluginManager;

    #endregion

    #region Ctor

    public CarriersController(CurrencySettings currencySettings,
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
        ICarrierModelFactory carrierModelFactory,
        IAddressService addressService,
        ICustomerActivityService customerActivityService,
        INotificationService notificationService,
        IShippingPluginManager shippingPluginManager)
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
        _carrierModelFactory = carrierModelFactory;
        _addressService = addressService;
        _customerActivityService = customerActivityService;
        _notificationService = notificationService;
        _shippingPluginManager = shippingPluginManager;
    }

    #endregion

    #region Carriers

    public virtual async Task<IActionResult> Carriers()
    {
        if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Manage))
            return AccessDeniedView();

        //prepare model
        var model = await _carrierModelFactory.PrepareCarrierSearchModelAsync(new CarrierSearchModel());

        return View("~/Plugins/SSI.Shipping.Manager/Views/Carrier/Carriers.cshtml", model);
    }

    [HttpPost]
    public virtual async Task<IActionResult> Carriers(CarrierSearchModel searchModel)
    {
        if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Manage))
            return AccessDeniedView();

        //prepare model
        var model = await _carrierModelFactory.PrepareCarrierListModelAsync(searchModel);

        return Json(model);
    }

    public virtual async Task<IActionResult> CreateCarrier()
    {
        if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Manage))
            return AccessDeniedView();

        //prepare model
        var model = await _carrierModelFactory.PrepareCarrierModelAsync(new CarrierModel(), null);

        return View("~/Plugins/SSI.Shipping.Manager/Views/Carrier/CreateCarrier.cshtml", model);
    }

    [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
    public virtual async Task<IActionResult> CreateCarrier(CarrierModel model, bool continueEditing)
    {
        if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Manage))
            return AccessDeniedView();

        if (ModelState.IsValid)
        {
            var address = model.Address.ToEntity<Address>();
            address.CreatedOnUtc = DateTime.UtcNow;
            await _addressService.InsertAddressAsync(address);

            var carrier = model.ToEntity<Carrier>();
            carrier.AddressId = address.Id;

            await _carrierService.InsertCarrierAsync(carrier);

            //activity log
            await _customerActivityService.InsertActivityAsync("AddNewCarrier",
                string.Format(await _localizationService.GetResourceAsync("ActivityLog.AddNewCarrier"), carrier.Id), carrier);

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Carriers.Added"));

            return continueEditing ? RedirectToAction("EditCarrier", new { id = carrier.Id }) : RedirectToAction("Carriers");
        }

        //prepare model
        model = await _carrierModelFactory.PrepareCarrierModelAsync(model, null, true);

        //if we got this far, something failed, redisplay form
        return View("~/Plugins/SSI.Shipping.Manager/Views/Carrier/CreateCarrier.cshtml", model);
    }

    public virtual async Task<IActionResult> EditCarrier(int id)
    {
        if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Manage))
            return AccessDeniedView();

        //try to get a carrier with the specified id
        var carrier = await _carrierService.GetCarrierByIdAsync(id);
        if (carrier == null)
            return RedirectToAction("Carriers");

        //prepare model
        var model = await _carrierModelFactory.PrepareCarrierModelAsync(null, carrier);

        return View("~/Plugins/SSI.Shipping.Manager/Views/Carrier/EditCarrier.cshtml", model);
    }

    [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
    public virtual async Task<IActionResult> EditCarrier(CarrierModel model, bool continueEditing)
    {
        if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Manage))
            return AccessDeniedView();

        //try to get a carrier with the specified id
        var carrier = await _carrierService.GetCarrierByIdAsync(model.Id);
        if (carrier == null)
            return RedirectToAction("Carriers");

        if (ModelState.IsValid)
        {
            var address = await _addressService.GetAddressByIdAsync(carrier.AddressId) ??
                new Address
                {
                    CreatedOnUtc = DateTime.UtcNow
                };

            address = model.Address.ToEntity(address);
            if (address.Id > 0)
                await _addressService.UpdateAddressAsync(address);
            else
                await _addressService.InsertAddressAsync(address);

            //fill entity from model
            carrier.Name = model.Name;
            carrier.AdminComment = model.AdminComment;

            var shippingMethod = _shippingPluginManager.LoadPluginBySystemNameAsync(ShippingManagerDefaults.SystemName);
            if (model.ShippingRateComputationMethodSystemName != ShippingManagerDefaults.SendCloudSystemName &&
                model.ShippingRateComputationMethodSystemName != ShippingManagerDefaults.CanadaPostSystemName &&
                model.ShippingRateComputationMethodSystemName != ShippingManagerDefaults.AramexSystemName)
            {
                shippingMethod = _shippingPluginManager.LoadPluginBySystemNameAsync(model.ShippingRateComputationMethodSystemName);
            }

            if (shippingMethod == null)
                model.ShippingRateComputationMethodSystemName = ShippingManagerDefaults.SystemName;

            carrier.ShippingRateComputationMethodSystemName = model.ShippingRateComputationMethodSystemName;

            carrier.Active = model.Active;
            carrier.AddressId = address.Id;

            await _carrierService.UpdateCarrierAsync(carrier);

            //activity log
            await _customerActivityService.InsertActivityAsync("EditCarrier",
                string.Format(await _localizationService.GetResourceAsync("ActivityLog.EditCarrier"), carrier.Id), carrier);

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Carriers.Updated"));

            return continueEditing ? RedirectToAction("EditCarrier", carrier.Id) : RedirectToAction("Carriers");
        }

        //prepare model
        model = await _carrierModelFactory.PrepareCarrierModelAsync(model, carrier, true);

        //if we got this far, something failed, redisplay form
        return View("~/Plugins/SSI.Shipping.Manager/Views/Carrier/EditCarrier.cshtml", model);
    }

    [HttpPost]
    public virtual async Task<IActionResult> DeleteCarrier(int id)
    {
        if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Manage))
            return AccessDeniedView();

        //try to get a carrier with the specified id
        var carrier = await _carrierService.GetCarrierByIdAsync(id);
        if (carrier == null)
            return RedirectToAction("Carriers");

        await _carrierService.DeleteCarrierAsync(carrier);

        //activity log
        await _customerActivityService.InsertActivityAsync("DeleteCarrier",
            string.Format(await _localizationService.GetResourceAsync("ActivityLog.DeleteCarrier"), carrier.Id), carrier);

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Carriers.Deleted"));

        return RedirectToAction("Carriers");
    }

    #endregion

}