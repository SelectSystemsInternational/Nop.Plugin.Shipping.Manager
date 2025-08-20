using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Shipping;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Shipping;
using Nop.Services.Stores;
using Nop.Services.Vendors;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Models.Extensions;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.Filters;

using Nop.Plugin.Shipping.Manager.Domain;
using Nop.Plugin.Shipping.Manager.Models;
using Nop.Plugin.Shipping.Manager.Services;
using Nop.Plugin.Shipping.Manager.Settings;
using Nop.Plugin.Shipping.Manager.ExportImport;

using Nop.Plugin.Apollo.Integrator;
using Nop.Plugin.Apollo.Integrator.Services;

namespace Nop.Plugin.Shipping.Manager.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    public class ShippingManagerController : BasePluginController
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
        protected readonly ShippingSettings _shippingSettings;
        protected readonly IGenericAttributeService _genericAttributeService;

        SystemHelper _systemHelper = new SystemHelper();

        #endregion

        #region Ctor

        public ShippingManagerController(CurrencySettings currencySettings,
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
            ShippingSettings shippingSettings,
            IGenericAttributeService genericAttributeService)
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
            _shippingSettings = shippingSettings;
            _genericAttributeService = genericAttributeService;
        }

        #endregion

        #region Manage Methods

        public async Task<IActionResult> Manage()
        {
            if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Manage))
                return AccessDeniedView();

            //Get vendor
            int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

            var model = new ShippingManagerModel
            {
                LimitMethodsToCreated = _shippingManagerSettings.LimitMethodsToCreated,
                ReturnValidOptionsIfThereAreAny = _shippingSettings.ReturnValidOptionsIfThereAreAny,
                ShippingByWeightByTotalEnabled = _shippingManagerSettings.ShippingByWeightByTotalEnabled,
                DisplayVendor = vendorId == 0
            };

            //stores
            model.AvailableStores.Add(new SelectListItem { Text = "*", Value = "0" });
            foreach (var store in await _storeService.GetAllStoresAsync())
                model.AvailableStores.Add(new SelectListItem { Text = store.Name, Value = store.Id.ToString() });

            //warehouses
            model.AvailableWarehouses.Add(new SelectListItem { Text = "*", Value = "0" });
            foreach (var warehouses in await _shippingService.GetAllWarehousesAsync())
                model.AvailableWarehouses.Add(new SelectListItem { Text = warehouses.Name, Value = warehouses.Id.ToString() });

            //carriers
            model.AvailableCarriers = await _shippingManagerService.PrepareAvailableCarriersModelAsync(true);

            //shipping methods
            int index = 0;
            (model.AvailableShippingMethods, index) = await _shippingManagerService.PrepareAvailableShippingMethodsModelAsync(true);

            //countries
            if (!_shippingManagerSettings.InternationalOperationsEnabled)
            {
                //prepare available states
                model.AvailableStates.Add(new SelectListItem { Text = "*", Value = "0" });
                var states = (await _stateProvinceService.GetStateProvincesByCountryIdAsync(_shippingManagerSettings.DefaultCountryId)).ToList();
                var result = new List<SelectListItem>();
                foreach (var state in states)
                {
                    model.AvailableStates.Add(new SelectListItem
                    {
                        Value = state.Id.ToString(),
                        Text = await _localizationService.GetLocalizedAsync(state, x => x.Name)
                    });
                }
            }
            else
            {
                //prepare available countries
                var countries = await _countryService.GetAllCountriesAsync();
                model.AvailableCountries.Add(new SelectListItem { Text = "*", Value = "0" });
                foreach (var c in countries)
                    model.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString() });

                //states
                model.AvailableStates.Add(new SelectListItem { Text = "*", Value = "0" });
            }

            model.SetGridPageSize();
            model.SearchActive = true;

            return View("~/Plugins/SSI.Shipping.Manager/Views/Manage.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> Manage(ShippingManagerModel model)
        {
            if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Manage))
                return Content("Access denied");

            //save settings
            _shippingManagerSettings.LimitMethodsToCreated = model.LimitMethodsToCreated;
            _shippingSettings.ReturnValidOptionsIfThereAreAny = model.ReturnValidOptionsIfThereAreAny;

            await _settingService.SaveSettingAsync(_shippingManagerSettings);
            await _settingService.SaveSettingAsync(_shippingSettings);

            return Json(new { Result = true });
        }

        [HttpPost]
        public async Task<IActionResult> SaveMode(bool value)
        {
            if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Manage))
                return Content("Access denied");

            //save settings
            _shippingManagerSettings.ShippingByWeightByTotalEnabled = value;
            await _settingService.SaveSettingAsync(_shippingManagerSettings);

            return Json(new { Result = true });
        }

        #endregion

        #region Fixed rate

        [HttpPost]
        public async Task<IActionResult> ShippingManagerFixedShippingRateList(ShippingManagerModel searchModel)
        {
            if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Manage))
                return AccessDeniedView();

            //Get vendor
            int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

            var shippingMethods = (await _shippingService.GetAllShippingMethodsAsync()).ToPagedList(searchModel);

            var gridModel = await new ShippingManagerFixedRateListModel().PrepareToGridAsync(searchModel, shippingMethods, () =>
            {
                return shippingMethods.SelectAwait(async shippingMethod => new ShippingManagerFixedRateModel
                {
                    ShippingMethodId = shippingMethod.Id,
                    ShippingMethodName = shippingMethod.Name,

                    Rate = await _settingService
                        .GetSettingByKeyAsync<decimal>(string.Format(ShippingManagerDefaults.FIXED_RATE_SETTINGS_KEY, vendorId, shippingMethod.Id)),
                    TransitDays = await _settingService
                        .GetSettingByKeyAsync<int?>(string.Format(ShippingManagerDefaults.TRANSIT_DAYS_SETTINGS_KEY, vendorId, shippingMethod.Id))
                });
            });

            return Json(gridModel);
        }

        [HttpPost]
        public async Task<IActionResult> ShippingManagerUpdateFixedShippingRate(ShippingManagerFixedRateModel model)
        {
            if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Manage))
                return Content("Access denied");

            //Get vendor
            int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

            await _settingService.SetSettingAsync(string.Format(ShippingManagerDefaults.FIXED_RATE_SETTINGS_KEY, vendorId, model.ShippingMethodId), model.Rate);

            return new NullJsonResult();
        }

        #endregion

        #region Rate by weight

        [HttpPost]
        public async Task<IActionResult> RateByWeightByTotalList(ShippingManagerModel searchModel, ShippingManagerModel filter)
        {
            if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Manage))
                return AccessDeniedView();

            //Get vendor
            int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

            var records = await _shippingManagerService.FindRecordsAsync(
              pageIndex: searchModel.Page - 1,
              pageSize: searchModel.PageSize,
              active: searchModel.SearchActive,
              storeId: filter.SearchStoreId,
              vendorId: vendorId,
              warehouseId: filter.SearchWarehouseId,
              carrierId: filter.SearchCarrierId,
              countryId: filter.SearchCountryId,
              stateProvinceId: filter.SearchStateProvinceId,
              zip: filter.SearchZip,
              shippingMethodId: filter.SearchShippingMethodId,
              weight: null,
              orderSubtotal: null
              );

            var gridModel = await new ShippingManagerByWeightByTotalListModel().PrepareToGridAsync(searchModel, records, () =>
            {
                return records.SelectAwait(async record =>
                {
                    var model = new ShippingManagerByWeightByTotalModel
                    {
                        Id = record.Id,
                        Active = record.Active,
                        StoreId = record.StoreId,
                        StoreName = (await _storeService.GetStoreByIdAsync(record.StoreId))?.Name ?? "*",
                        VendorId = record.VendorId,
                        VendorName = (await _vendorService.GetVendorByIdAsync(record.VendorId))?.Name ?? "*",
                        WarehouseId = record.WarehouseId,
                        WarehouseName = (await _shippingService.GetWarehouseByIdAsync(record.WarehouseId))?.Name ?? "*",
                        CarrierId = record.CarrierId,
                        CarrierName = (await _carrierService.GetCarrierByIdAsync(record.CarrierId))?.Name ?? "*",
                        CutOffTimeId = record.CutOffTimeId,
                        CutOffTimeName = (await _carrierService.GetCutOffTimeByIdAsync(record.CutOffTimeId))?.Name ?? "*",
                        ShippingMethodId = record.ShippingMethodId,
                        ShippingMethodName = (await _shippingService.GetShippingMethodByIdAsync(record.ShippingMethodId))?.Name ?? "Unavailable",
                        CountryId = record.CountryId,
                        CountryName = (await _countryService.GetCountryByIdAsync(record.CountryId))?.Name ?? "*",
                        StateProvinceId = record.StateProvinceId,
                        StateProvinceName = (await _stateProvinceService.GetStateProvinceByIdAsync(record.StateProvinceId))?.Name ?? "*",
                        Zip = !string.IsNullOrEmpty(record.Zip) ? record.Zip : "*",
                        WeightFrom = record.WeightFrom,
                        WeightTo = record.WeightTo,
                        CalculateCubicWeight = record.CalculateCubicWeight,
                        CubicWeightFactor = record.CubicWeightFactor,
                        OrderSubtotalFrom = record.OrderSubtotalFrom,
                        OrderSubtotalTo = record.OrderSubtotalTo,
                        AdditionalFixedCost = record.AdditionalFixedCost,
                        PercentageRateOfSubtotal = record.PercentageRateOfSubtotal,
                        RatePerWeightUnit = record.RatePerWeightUnit,
                        LowerWeightLimit = record.LowerWeightLimit,
                        FriendlyName = record.FriendlyName,
                        TransitDays = record.TransitDays,
                        SendFromAddressId = record.SendFromAddressId,
                        DisplayOrder = record.DisplayOrder,
                        Description = record.Description,
                    };

                    var htmlSb = new StringBuilder("<div>");
                    htmlSb.AppendFormat("{0}: {1}",
                        await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Fields.Active"), 
                        model.Active);
                    htmlSb.Append("<br />");
                    htmlSb.AppendFormat("{0}: {1}",
                        await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Fields.DisplayOrder"),
                        model.DisplayOrder);
                    htmlSb.Append("<br />");
                    htmlSb.AppendFormat("{0}: {1}",
                        await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Fields.AdditionalFixedCost"), 
                        model.AdditionalFixedCost);
                    htmlSb.Append("<br />");
                    htmlSb.AppendFormat("{0}: {1}",
                        await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Fields.WeightFrom"), 
                        model.WeightFrom);
                    htmlSb.Append("<br />");
                    htmlSb.AppendFormat("{0}: {1}",
                        await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Fields.WeightTo"), 
                        model.WeightTo);
                    htmlSb.Append("<br />");
                    htmlSb.AppendFormat("{0}: {1}",
                        await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Fields.RatePerWeightUnit"), 
                        model.RatePerWeightUnit);
                    htmlSb.Append("<br />");
                    htmlSb.AppendFormat("{0}: {1}",
                        await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Fields.LowerWeightLimit"), 
                        model.LowerWeightLimit);
                    htmlSb.Append("<br />");
                    htmlSb.AppendFormat("{0}: {1}",
                        await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Fields.CalculateCubicWeight"), 
                        model.CalculateCubicWeight ? "Yes" : "No");
                    htmlSb.Append("<br />");
                    htmlSb.AppendFormat("{0}: {1}",
                        await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Fields.CubicWeightFactor"), 
                        model.CubicWeightFactor);
                    htmlSb.Append("<br />");
                    htmlSb.AppendFormat("{0}: {1}",
                        await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Fields.OrderSubtotalFrom"), 
                        model.OrderSubtotalFrom);
                    htmlSb.Append("<br />");
                    htmlSb.AppendFormat("{0}: {1}",
                        await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Fields.OrderSubtotalTo"), 
                        model.OrderSubtotalTo);
                    htmlSb.Append("<br />");
                    htmlSb.AppendFormat("{0}: {1}",
                        await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Fields.PercentageRateOfSubtotal"),
                        model.PercentageRateOfSubtotal);
                    htmlSb.Append("<br />");
                    htmlSb.AppendFormat("{0}: {1}",
                        await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Fields.CutOffTime"),
                        model.CutOffTimeName);
                    htmlSb.Append("<br />");
                    htmlSb.AppendFormat("{0}: {1}",
                        await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Fields.FriendlyName"), 
                        model.FriendlyName);
                    htmlSb.Append("<br />");
                    htmlSb.AppendFormat("{0}: {1}",
                        await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Fields.TransitDays"),
                        model.TransitDays);
                    htmlSb.Append("<br />");
                    htmlSb.AppendFormat("{0}: {1}",
                        await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Fields.SendFromAddress"),
                        model.SendFromAddressId);
                    htmlSb.Append("<br />");
                    htmlSb.AppendFormat("{0}: {1}",
                        await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Fields.Description"),
                        model.Description);
                    htmlSb.Append("<br />");
                    htmlSb.Append("</div>");

                    model.DataHtml = htmlSb.ToString();

                    return model;
                });
            });

            return Json(gridModel);
        }

        public async Task<IActionResult> AddRateByWeightByTotalPopup(string btnId)
        {
            if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Manage))
                return AccessDeniedView();

            var model = new ShippingManagerByWeightByTotalModel
            {
                PrimaryStoreCurrencyCode = (await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId))?.CurrencyCode,
                BaseWeightIn = (await _measureService.GetMeasureWeightByIdAsync(_measureSettings.BaseWeightId))?.Name,
                WeightTo = 1000000,
                OrderSubtotalTo = 1000000,
                TransitDays = 2
            };

            model.BtnId = btnId;

            var shippingMethods = await _shippingService.GetAllShippingMethodsAsync();
            if (!shippingMethods.Any())
                return Content("No shipping methods can be loaded");

            //stores
            model.AvailableStores.Add(new SelectListItem { Text = "*", Value = "0" });
            foreach (var store in await _storeService.GetAllStoresAsync())
                model.AvailableStores.Add(new SelectListItem { Text = store.Name, Value = store.Id.ToString() });

            //cutofftime
            model.AvailableCutOffTime.Add(new SelectListItem { Text = "*", Value = "0" });
            foreach (var cutOffTime in await _carrierService.GetAllCutOffTimesAsync())
                model.AvailableCutOffTime.Add(new SelectListItem { Text = cutOffTime.Name, Value = cutOffTime.Id.ToString() });

            //warehouses
            model.AvailableWarehouses.Add(new SelectListItem { Text = "*", Value = "0" });
            foreach (var warehouses in await _shippingService.GetAllWarehousesAsync())
                model.AvailableWarehouses.Add(new SelectListItem { Text = warehouses.Name, Value = warehouses.Id.ToString() });

            //send from addresses
            if (_shippingManagerSettings.ApiServices.Contains(ShippingManagerDefaults.SendCloudSystemName))
            {
                model.UseSendFromAddress = true;
                model.AvailableSendFromAddress.Add(new SelectListItem { Text = "*", Value = "0" });
                try
                {
                    var sendFromAddresses = await _sendcloudService.GetSendFromAddressesAsync();
                    foreach (var sendFromAddress in sendFromAddresses)
                    {
                        string details = sendFromAddress.CompanyName + "," + sendFromAddress.Country + "," + sendFromAddress.City + "," + sendFromAddress.PostalCode;
                        model.AvailableSendFromAddress.Add(new SelectListItem { Text = details, Value = sendFromAddress.Id.ToString() });
                    }
                }
                catch(Exception exc)
                {
                    model.UseSendFromAddress = true;
                }
            }
            else
            {
                model.AvailableSendFromAddress.Add(new SelectListItem { Text = "No Sendfrom Address", Value = "0" });
            }

            //vendors
            model.AvailableVendors = await _entityGroupService.PrepareAvailableVendorsListAsync();

            //carriers
            model.AvailableCarriers = await _shippingManagerService.PrepareAvailableCarriersModelAsync(false);

            //shipping methods
            int index = 0;
            (model.AvailableShippingMethods, index) = await _shippingManagerService.PrepareAvailableShippingMethodsModelAsync(true);

            //countries
            if (!_shippingManagerSettings.InternationalOperationsEnabled)
            {
                //prepare available states
                model.AvailableStates.Add(new SelectListItem { Text = "*", Value = "0" });
                var states = (await _stateProvinceService.GetStateProvincesByCountryIdAsync(_shippingManagerSettings.DefaultCountryId)).ToList();
                var result = new List<SelectListItem>();
                foreach (var state in states)
                {
                    model.AvailableStates.Add(new SelectListItem
                    {
                        Value = state.Id.ToString(),
                        Text = await _localizationService.GetLocalizedAsync(state, x => x.Name)
                    });
                }
            }
            else
            {
                //prepare available countries
                var countries = await _countryService.GetAllCountriesAsync();
                model.AvailableCountries.Add(new SelectListItem { Text = "*", Value = "0" });
                foreach (var c in countries)
                    model.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString() });

                //states
                model.AvailableStates.Add(new SelectListItem { Text = "*", Value = "0" });
            }

            return View("~/Plugins/SSI.Shipping.Manager/Views/AddRateByWeightByTotalPopup.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> AddRateByWeightByTotalPopup(string btnId, ShippingManagerByWeightByTotalModel model)
        {
            if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Manage))
                return AccessDeniedView();

            int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

            if (vendorId == 0)
                vendorId = model.VendorId;

            await _shippingManagerService.InsertShippingByWeightRecordAsync(new ShippingManagerByWeightByTotal
            {
                Active = model.Active,
                StoreId = model.StoreId,
                WarehouseId = model.WarehouseId,
                VendorId = vendorId,
                CarrierId = model.CarrierId,
                CutOffTimeId = model.CutOffTimeId,
                CountryId = model.CountryId,
                StateProvinceId = model.StateProvinceId,
                Zip = model.Zip == "*" ? null : model.Zip,
                ShippingMethodId = model.ShippingMethodId,
                WeightFrom = model.WeightFrom,
                WeightTo = model.WeightTo,
                CalculateCubicWeight = model.CalculateCubicWeight,
                CubicWeightFactor = model.CubicWeightFactor,
                OrderSubtotalFrom = model.OrderSubtotalFrom,
                OrderSubtotalTo = model.OrderSubtotalTo,
                AdditionalFixedCost = model.AdditionalFixedCost,
                RatePerWeightUnit = model.RatePerWeightUnit,
                PercentageRateOfSubtotal = model.PercentageRateOfSubtotal,
                LowerWeightLimit = model.LowerWeightLimit,
                FriendlyName = model.FriendlyName,
                TransitDays = model.TransitDays,
                SendFromAddressId = model.SendFromAddressId,
                DisplayOrder = model.DisplayOrder,
                Description = model.Description,
            });

            ViewBag.RefreshPage = true;
            ViewBag.BtnId = btnId;

            return View("~/Plugins/SSI.Shipping.Manager/Views/AddRateByWeightByTotalPopup.cshtml", model);
        }

        public async Task<IActionResult> EditRateByWeightByTotalPopup(int id, string btnId)
        {
            if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Manage))
                return AccessDeniedView();

            var sbw = await _shippingManagerService.GetByIdAsync(id);
            if (sbw == null)
                //no record found with the specified id
                return RedirectToAction("Manage");

            var model = new ShippingManagerByWeightByTotalModel
            {
                Id = sbw.Id,
                Active = sbw.Active,
                StoreId = sbw.StoreId,
                WarehouseId = sbw.WarehouseId,
                VendorId = sbw.VendorId,
                CarrierId = sbw.CarrierId,
                CutOffTimeId = sbw.CutOffTimeId,
                CountryId = sbw.CountryId,
                StateProvinceId = sbw.StateProvinceId,
                Zip = sbw.Zip,
                ShippingMethodId = sbw.ShippingMethodId,
                WeightFrom = sbw.WeightFrom,
                WeightTo = sbw.WeightTo,
                CalculateCubicWeight = sbw.CalculateCubicWeight,
                CubicWeightFactor = sbw.CubicWeightFactor,
                OrderSubtotalFrom = sbw.OrderSubtotalFrom,
                OrderSubtotalTo = sbw.OrderSubtotalTo,
                AdditionalFixedCost = sbw.AdditionalFixedCost,
                PercentageRateOfSubtotal = sbw.PercentageRateOfSubtotal,
                RatePerWeightUnit = sbw.RatePerWeightUnit,
                LowerWeightLimit = sbw.LowerWeightLimit,
                PrimaryStoreCurrencyCode = (await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId))?.CurrencyCode,
                BaseWeightIn = (await _measureService.GetMeasureWeightByIdAsync(_measureSettings.BaseWeightId))?.Name,
                FriendlyName = sbw.FriendlyName,
                TransitDays = sbw.TransitDays,
                SendFromAddressId = sbw.SendFromAddressId,
                DisplayOrder = sbw.DisplayOrder,
                Description = sbw.Description,          
            };

            model.BtnId = btnId;

            var shippingMethods = await _shippingService.GetAllShippingMethodsAsync();
            if (!shippingMethods.Any())
                return Content("No shipping methods can be loaded");

            var selectedStore = await _storeService.GetStoreByIdAsync(sbw.StoreId);
            var selectedVendor = await _vendorService.GetVendorByIdAsync(sbw.VendorId);
            var selectedWarehouse = await _shippingService.GetWarehouseByIdAsync(sbw.WarehouseId);
            var selectedCarrier = await _carrierService.GetCarrierByIdAsync(sbw.CarrierId);
            var selectedCutOffTime = await _carrierService.GetCutOffTimeByIdAsync(sbw.CutOffTimeId);
            var selectedState = await _stateProvinceService.GetStateProvinceByIdAsync(sbw.StateProvinceId);
            var selectedShippingMethod = await  _shippingService.GetShippingMethodByIdAsync(sbw.ShippingMethodId);
            var selectedCountry = await _countryService.GetCountryByIdAsync(sbw.CountryId);
            var selectedSendFromAddressId = sbw.SendFromAddressId;

            //stores
            model.AvailableStores.Add(new SelectListItem { Text = "*", Value = "0" });
            foreach (var store in await _storeService.GetAllStoresAsync())
                model.AvailableStores.Add(new SelectListItem { Text = store.Name, Value = store.Id.ToString(), Selected = (selectedStore != null && store.Id == selectedStore.Id) });

            //warehouses
            model.AvailableWarehouses.Add(new SelectListItem { Text = "*", Value = "0" });
            foreach (var warehouse in await _shippingService.GetAllWarehousesAsync())
                model.AvailableWarehouses.Add(new SelectListItem { Text = warehouse.Name, Value = warehouse.Id.ToString(), Selected = (selectedWarehouse != null && warehouse.Id == selectedWarehouse.Id) });

            //cutofftime
            model.AvailableCutOffTime.Add(new SelectListItem { Text = "*", Value = "0" });
            foreach (var cutOffTime in await _carrierService.GetAllCutOffTimesAsync())
                model.AvailableCutOffTime.Add(new SelectListItem { Text = cutOffTime.Name, Value = cutOffTime.Id.ToString(), Selected = (selectedCutOffTime != null && cutOffTime.Id == selectedCutOffTime.Id) });

            //send from addresses
            if (_shippingManagerSettings.ApiServices.Contains(ShippingManagerDefaults.SendCloudSystemName))
            {
                model.UseSendFromAddress = true;
                model.AvailableSendFromAddress.Add(new SelectListItem { Text = "*", Value = "0" });
                try
                {
                    var sendFromAddresses = await _sendcloudService.GetSendFromAddressesAsync();
                    foreach (var sendFromAddress in sendFromAddresses)
                    {
                        string details = sendFromAddress.CompanyName + "," + sendFromAddress.Country + "," + sendFromAddress.City + "," + sendFromAddress.PostalCode;
                        model.AvailableSendFromAddress.Add(new SelectListItem { Text = details, Value = sendFromAddress.Id.ToString() });
                    }
                }
                catch (Exception exc)
                {
                    model.UseSendFromAddress = true;
                }
            }

            //vendors
            model.AvailableVendors = await _entityGroupService.PrepareAvailableVendorsListAsync();

            //carrier
            model.AvailableCarriers = await _shippingManagerService.PrepareAvailableCarriersModelAsync(false, selectedCarrier == null ? 0 : selectedCarrier.Id);

            //shipping methods
            int index = 0;
            (model.AvailableShippingMethods, index) = await _shippingManagerService.PrepareAvailableShippingMethodsModelAsync(true, selectedShippingMethod == null ? 0 : selectedShippingMethod.Id);

            //countries
            if (!_shippingManagerSettings.InternationalOperationsEnabled)
            {
                //prepare available states
                model.AvailableStates.Add(new SelectListItem { Text = "*", Value = "0" });
                var states = (await _stateProvinceService.GetStateProvincesByCountryIdAsync(_shippingManagerSettings.DefaultCountryId)).ToList();
                var result = new List<SelectListItem>();
                foreach (var state in states)
                {
                    model.AvailableStates.Add(new SelectListItem
                    {
                        Value = state.Id.ToString(),
                        Text = await _localizationService.GetLocalizedAsync(state, x => x.Name),
                        Selected = (selectedState != null && state.Id == selectedState.Id)
                    });
                }
            }
            else
            {
                //prepare available countries
                var countries = await _countryService.GetAllCountriesAsync();
                model.AvailableCountries.Add(new SelectListItem { Text = "*", Value = "0" });
                foreach (var c in countries)
                    model.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString(), Selected = (selectedCountry != null && c.Id == selectedCountry.Id) });

                //prepare available states
                model.AvailableStates.Add(new SelectListItem { Text = "*", Value = "0" });
                if (selectedCountry != null)
                {
                    var states = (await _stateProvinceService.GetStateProvincesByCountryIdAsync(selectedCountry.Id)).ToList();
                    foreach (var state in states)
                    {
                        model.AvailableStates.Add(new SelectListItem
                        {
                            Value = state.Id.ToString(),
                            Text = await _localizationService.GetLocalizedAsync(state, x => x.Name),
                            Selected = (selectedState != null && state.Id == selectedState.Id)
                        });
                    }
                }
            }

            return View("~/Plugins/SSI.Shipping.Manager/Views/EditRateByWeightByTotalPopup.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> EditRateByWeightByTotalPopup(string btnId, ShippingManagerByWeightByTotalModel model)
        {
            if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Manage))
                return AccessDeniedView();

            var sbw = await _shippingManagerService.GetByIdAsync(model.Id);
            if (sbw == null)
                //no record found with the specified id
                return RedirectToAction("Manage");

            int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

            if (vendorId == 0)
                sbw.VendorId = model.VendorId;
            else
                sbw.VendorId = vendorId;

            sbw.StoreId = model.StoreId;
            sbw.Active = model.Active;
            sbw.WarehouseId = model.WarehouseId;
            sbw.CarrierId = model.CarrierId;
            sbw.CutOffTimeId = model.CutOffTimeId;
            sbw.CountryId = model.CountryId;
            sbw.StateProvinceId = model.StateProvinceId;
            sbw.Zip = model.Zip == "*" ? null : model.Zip;
            sbw.ShippingMethodId = model.ShippingMethodId;
            sbw.WeightFrom = model.WeightFrom;
            sbw.WeightTo = model.WeightTo;
            sbw.CalculateCubicWeight = model.CalculateCubicWeight;
            sbw.CubicWeightFactor = model.CubicWeightFactor;
            sbw.OrderSubtotalFrom = model.OrderSubtotalFrom;
            sbw.OrderSubtotalTo = model.OrderSubtotalTo;
            sbw.AdditionalFixedCost = model.AdditionalFixedCost;
            sbw.RatePerWeightUnit = model.RatePerWeightUnit;
            sbw.PercentageRateOfSubtotal = model.PercentageRateOfSubtotal;
            sbw.LowerWeightLimit = model.LowerWeightLimit;
            sbw.FriendlyName = model.FriendlyName;
            sbw.TransitDays = model.TransitDays;
            sbw.SendFromAddressId = model.SendFromAddressId;
            sbw.DisplayOrder = model.DisplayOrder;
            sbw.Description = model.Description;

            await _shippingManagerService.UpdateShippingByWeightRecordAsync(sbw);

            ViewBag.RefreshPage = true;
            ViewBag.BtnId = btnId;

            return View("~/Plugins/SSI.Shipping.Manager/Views/EditRateByWeightByTotalPopup.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRateByWeightByTotal(int id)
        {
            if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Manage))
                return Content("Access denied");

            var sbw = await _shippingManagerService.GetByIdAsync(id);
            if (sbw != null)
                await _shippingManagerService.DeleteShippingByWeightRecordAsync(sbw);

            return new NullJsonResult();
        }

        #endregion

        #region Export

        [AuthorizeAdmin]
        [Area(AreaNames.ADMIN)]
        [HttpPost, ActionName("Manage")]
        [FormValueRequired("ExportRatesToExcel")]
        public virtual async Task<IActionResult> ExportRatesToExcel(Microsoft.AspNetCore.Http.IFormCollection fc)
        {
            if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Manage))
                return AccessDeniedView();

            var rates = await _shippingManagerService.GetRatesForExportAsync();

            try
            {
                var bytes = await _exportImportManager.ExportRatesToXlsxAsync(rates);
                return File(bytes, MimeTypes.TextXlsx, "RatesByWeightByTotal.xlsx");
            }
            catch (Exception exc)
            {
                await _notificationService.ErrorNotificationAsync(exc);
                return RedirectToAction("Manage");
            }
        }

        [AuthorizeAdmin]
        [Area(AreaNames.ADMIN)]
        [HttpPost]
        public virtual async Task<IActionResult> ImportRatesToExcel(IFormFile importexcelfile)
        {

            try
            {
                if (importexcelfile != null && importexcelfile.Length > 0)
                {
                    await _exportImportManager.ImportRatesFromXlsxAsync(importexcelfile.OpenReadStream());
                }
                else
                {
                    _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Admin.Common.UploadFile"));
                    return RedirectToAction("Manage");
                }

                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Catalog.Products.Imported"));
                return RedirectToAction("Manage");
            }
            catch (Exception exc)
            {
                //ErrorNotification(exc);
                await _notificationService.ErrorNotificationAsync(exc);
                return RedirectToAction("Manage");
            }
        }

        #endregion

    }
}
