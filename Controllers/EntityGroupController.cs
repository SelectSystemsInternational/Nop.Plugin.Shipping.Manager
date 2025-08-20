using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
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
using Nop.Services.Vendors;
using Nop.Web.Framework;
using Nop.Web.Framework.Extensions;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Models.Extensions;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.Filters;

using Nop.Plugin.Shipping.Manager.Services;
using Nop.Plugin.Shipping.Manager.Settings;

using Nop.Plugin.Apollo.Integrator;
using Nop.Plugin.Apollo.Integrator.Domain;
using Nop.Plugin.Apollo.Integrator.Services;
using Nop.Plugin.Apollo.Integrator.Models.EntityGroup;

namespace Nop.Plugin.Shipping.Manager.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    public class EntityGroupController : BasePluginController
    {

        #region Fields

        protected readonly CurrencySettings _currencySettings;
        protected readonly ShippingManagerSettings _shippingManagerSettings;
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
        protected readonly IVendorService _vendorService;
        protected readonly INotificationService _notificationService;
        protected readonly IStoreContext _storeContext;
        protected readonly IWorkContext _workContext;
        protected readonly IEntityGroupService _entityGroupService;
        protected readonly IShippingManagerInstallService _shippingManagerInstallService;
        protected readonly ILogger _logger;
        protected readonly IGenericAttributeService _genericAttributeService;


        SystemHelper _systemHelper = new SystemHelper();

        #endregion

        #region Ctor

        public EntityGroupController(CurrencySettings currencySettings,
            ShippingManagerSettings shippingManagerSettings,
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
            IVendorService vendorService,
            INotificationService notificationService,
            IStoreContext storeContext,
            IWorkContext workContext,
            IEntityGroupService entityGroupService,
            IShippingManagerInstallService shippingManagerInstallService,
            ILogger logger,
            IGenericAttributeService genericAttributeService)
        {
            _currencySettings = currencySettings;
            _shippingManagerSettings = shippingManagerSettings;
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
            _vendorService = vendorService;
            _notificationService = notificationService;
            _storeContext = storeContext;
            _workContext = workContext;
            _entityGroupService = entityGroupService;
            _shippingManagerInstallService = shippingManagerInstallService;
            _logger = logger;
            _genericAttributeService = genericAttributeService;
        }

        #endregion

        #region Utility

        public virtual async Task<IActionResult> ChangeVendorScopeConfiguration(int vendorid, string returnUrl = "")
        {
            var customer = await _workContext.GetCurrentCustomerAsync();

            var vendor = await _vendorService.GetVendorByIdAsync(vendorid);
            if (vendor != null || vendorid == 0)
            {
                await _genericAttributeService.SaveAttributeAsync(customer, ApolloIntegratorDefaults.AdminAreaVendorScopeConfigurationAttribute, vendorid);
                await _genericAttributeService.SaveAttributeAsync(customer, ApolloIntegratorDefaults.AdminAreaGroupVendorScopeConfigurationAttribute, 0);
            }

            return Redirect(returnUrl);
        }

        public virtual async Task<IActionResult> ChangeGroupVendorScopeConfiguration(int vendorid, string returnUrl = "")
        {
            var customer = await _workContext.GetCurrentCustomerAsync();

            var vendor = await _vendorService.GetVendorByIdAsync(vendorid);
            if (vendor != null || vendorid == 0)
            {
                await _genericAttributeService.SaveAttributeAsync(customer, ApolloIntegratorDefaults.AdminAreaGroupVendorScopeConfigurationAttribute, vendorid);
            }

            return Redirect(returnUrl);
        }

        #endregion

        #region Entity Group Methods

        public async Task<IActionResult> EntityGroup()
        {
            if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Configure))
                return AccessDeniedView();

            var model = new EntityGroupSearchModel
            {

            };

            //stores
            model.AvailableStores.Add(new SelectListItem { Text = "*", Value = "0" });
            foreach (var store in await _storeService.GetAllStoresAsync())
                model.AvailableStores.Add(new SelectListItem { Text = store.Name, Value = store.Id.ToString() });

            //warehouses
            model.AvailableWarehouses.Add(new SelectListItem { Text = "*", Value = "0" });
            foreach (var warehouses in await _shippingService.GetAllWarehousesAsync())
                model.AvailableWarehouses.Add(new SelectListItem { Text = warehouses.Name, Value = warehouses.Id.ToString() });

            //vendors
            model.AvailableVendors.Add(new SelectListItem { Text = "*", Value = "0" });
            foreach (var vendors in await _vendorService.GetAllVendorsAsync())
                model.AvailableVendors.Add(new SelectListItem { Text = vendors.Name, Value = vendors.Id.ToString() });

            //prepare available entity
            model.AvailableEntity.Add(new SelectListItem { Text = "*", Value = "0" });
            var entities = _entityGroupService.GetAllEntityGroups(null, 0, null, null);
            foreach (var e in entities)
                model.AvailableEntity.Add(new SelectListItem { Text = e.KeyGroup + " " + e.EntityId.ToString(), Value = e.Id.ToString() });

            //KeyGroup
            int number = 0;
            model.KeyGroupList.Add(new SelectListItem { Text = "*", Value = number.ToString() });
            foreach (var entity in Enum.GetNames(typeof(EntityGroupTypes.KeyGroup)))
            {
                number++;
                model.KeyGroupList.Add(new SelectListItem { Text = entity, Value = number.ToString() });
            }

            //Key
            number = 0;
            model.KeyList.Add(new SelectListItem { Text = "*", Value = number.ToString() });
            foreach (var entity in Enum.GetNames(typeof(EntityGroupTypes.Key)))
            {
                number++;
                model.KeyList.Add(new SelectListItem { Text = entity, Value = number.ToString() });
            }

            model.SetGridPageSize();

            return View("~/Plugins/SSI.Shipping.Manager/Views/EntityGroup/EntityGroup.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> EntityGroup(EntityGroupModel model)
        {
            if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Configure))
                return Content("Access denied");

            return Json(new { Result = true });
        }

        [HttpPost]
        public async Task<IActionResult> SaveMode(bool value)
        {
            if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Manage))
                return Content("Access denied");

            return Json(new { Result = true });
        }

        #endregion

        #region Entity Groups

        [HttpPost]
        public async Task<IActionResult> EntityGroupList(EntityGroupSearchModel searchModel, EntityGroupSearchModel filter)
        {
            if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Configure))
                return AccessDeniedView();

            filter.SearchKeyGroup = Enum.GetName(typeof(EntityGroupTypes.KeyGroup), filter.SearchKeyGroupId - 1);
            filter.SearchKey = Enum.GetName(typeof(EntityGroupTypes.Key), filter.SearchKeyId - 1);
            if (searchModel.AvailableStores.SelectionIsNotPossible())
                filter.SearchStoreId = await _entityGroupService.GetActiveStoreScopeConfiguration();

            var records = _entityGroupService.GetAllEntityGroups(
                pageIndex: searchModel.Page - 1,
                pageSize: searchModel.PageSize,
                storeId: filter.SearchStoreId,
                warehouseId: filter.SearchWarehouseId,
                vendorId: filter.SearchVendorId,
                entityId: filter.SearchEntityId,
                keyGroup: filter.SearchKeyGroup,
                key: filter.SearchKey,
                value: filter.SearchValue);

            var gridModel = await new EntityGroupListModel().PrepareToGridAsync(searchModel, records, () =>
            {
                return records.SelectAwait(async record =>
                {
                    var model = new EntityGroupModel
                    {
                        Id = record.Id,
                        StoreId = record.StoreId,
                        StoreName = (await _storeService.GetStoreByIdAsync(record.StoreId))?.Name ?? "*",
                        WarehouseId = record.WarehouseId,
                        WarehouseName = (await _shippingService.GetWarehouseByIdAsync(record.WarehouseId))?.Name ?? "*",
                        VendorId = record.VendorId,
                        VendorName = (await _vendorService.GetVendorByIdAsync(record.VendorId))?.Name ?? "*",
                        EntityId = record.EntityId,
                        EntityName = record.KeyGroup + " " + record.EntityId.ToString(),
                        Key = record.Key,
                        KeyGroup = record.KeyGroup,
                        Value = record.Value,
                    };

                    return model;
                });
            });

            return Json(gridModel);
        }

        public async Task<IActionResult> AddEntityGroupPopup()
        {
            if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Configure))
                return AccessDeniedView();

            var model = await PrepareEntityGroupModelAsync();

            return View("~/Plugins/SSI.Shipping.Manager/Views/EntityGroup/AddEntityGroupPopup.cshtml", model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> AddEntityGroupPopup(EntityGroupModel model)
        {
            if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Configure))
                return AccessDeniedView();

            if (model.AvailableStores.SelectionIsNotPossible())
                model.StoreId = await _entityGroupService.GetActiveStoreScopeConfiguration();

            model.KeyGroup = Enum.GetName(typeof(EntityGroupTypes.KeyGroup), model.KeyGroupId - 1);
            model.Key = Enum.GetName(typeof(EntityGroupTypes.Key), model.KeyId - 1);

            if (model.KeyGroup == "Vendor" && model.Key == "Member" && model.EntityId == 0)
                model.EntityId = await _entityGroupService.GetActiveVendorScopeAsync();

            if (model.KeyGroup != null && model.Key != null && model.Value != null)
            {
                await _entityGroupService.InsertEntityGroupAsync(new EntityGroup
                {
                    StoreId = model.StoreId,
                    WarehouseId = model.WarehouseId,
                    VendorId = model.VendorId,
                    KeyGroup = model.KeyGroup,
                    EntityId = model.EntityId,
                    Key = model.Key,
                    Value = model.Value,
                });

                if (model.Value == "0")
                {
                    var entityGroup = _entityGroupService.GetAllEntityGroups(model.KeyGroup, 0, model.Key, "0", model.StoreId, model.VendorId, model.WarehouseId).FirstOrDefault();
                    if (entityGroup != null)
                    {
                        entityGroup.EntityId = entityGroup.Id;
                        await _entityGroupService.UpdateEntityGroupAsync(entityGroup);
                    }
                }

                ViewBag.RefreshPage = true;
            }
            else
                model = await PrepareEntityGroupModelAsync();

            return View("~/Plugins/SSI.Shipping.Manager/Views/EntityGroup/AddEntityGroupPopup.cshtml", model);
        }

        private async Task<EntityGroupModel> PrepareEntityGroupModelAsync()
        {

            var model = new EntityGroupModel();

            //stores
            model.AvailableStores.Add(new SelectListItem { Text = "*", Value = "0" });
                foreach (var store in await _storeService.GetAllStoresAsync())
                    model.AvailableStores.Add(new SelectListItem { Text = store.Name, Value = store.Id.ToString()
            });

            //warehouses
            model.AvailableWarehouses.Add(new SelectListItem { Text = "*", Value = "0" });
            foreach (var warehouses in await _shippingService.GetAllWarehousesAsync())
                model.AvailableWarehouses.Add(new SelectListItem { Text = warehouses.Name, Value = warehouses.Id.ToString() });

            //vendors
            model.AvailableVendors.Add(new SelectListItem { Text = "*", Value = "0" });
            foreach (var vendors in await _vendorService.GetAllVendorsAsync())
                model.AvailableVendors.Add(new SelectListItem { Text = vendors.Name, Value = vendors.Id.ToString() });

            //prepare available entity
            model.AvailableEntity.Add(new SelectListItem { Text = "*", Value = "0" });
            var entities = _entityGroupService.GetAllEntityGroups(null, 0, null, null);
            foreach (var e in entities)
                model.AvailableEntity.Add(new SelectListItem { Text = e.KeyGroup + " " + e.EntityId.ToString(), Value = e.Id.ToString() });

            //KeyGroup
            int number = 0;
            model.KeyGroupList.Add(new SelectListItem { Text = "Please Select", Value = number.ToString() });
            foreach (var entity in Enum.GetNames(typeof(EntityGroupTypes.KeyGroup)))
            {
                number++;
                model.KeyGroupList.Add(new SelectListItem { Text = entity, Value = number.ToString() });
            }

            //Key
            number = 0;
            model.KeyList.Add(new SelectListItem { Text = "Please Select", Value = number.ToString() });
            foreach (var entity in Enum.GetNames(typeof(EntityGroupTypes.Key)))
            {
                number++;
                model.KeyList.Add(new SelectListItem { Text = entity, Value = number.ToString() });
            }

            return model;
        }

        public virtual async Task<IActionResult> EditEntityGroupPopup(int id)
        {
            if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Configure))
                return AccessDeniedView();

            var entityGroup = await _entityGroupService.GetEntityGroupByIdAsync(id);
            if (entityGroup == null)
                //no record found with the specified id
                return RedirectToAction("Manage");

            var model = new EntityGroupModel
            {
                Id = entityGroup.Id,
                StoreId = entityGroup.StoreId,
                WarehouseId = entityGroup.WarehouseId,
                VendorId = entityGroup.VendorId,
                EntityId = entityGroup.EntityId,
                KeyGroup = entityGroup.KeyGroup,
                KeyGroupId = (int)Enum.Parse(typeof(EntityGroupTypes.KeyGroup), entityGroup.KeyGroup) + 1,
                Key = entityGroup.Key,
                KeyId = (int)Enum.Parse(typeof(EntityGroupTypes.Key), entityGroup.Key) + 1,
                Value = entityGroup.Value,
            };

            var selectedStore = await _storeService.GetStoreByIdAsync(entityGroup.StoreId);
            var selectedWarehouse = await _shippingService.GetWarehouseByIdAsync(entityGroup.WarehouseId);
            var selectedVendor = await _shippingService.GetWarehouseByIdAsync(entityGroup.VendorId);

            var selectedEntity = entityGroup;

            //stores
            model.AvailableStores.Add(new SelectListItem { Text = "*", Value = "0" });
            foreach (var store in await _storeService.GetAllStoresAsync())
                model.AvailableStores.Add(new SelectListItem { Text = store.Name, Value = store.Id.ToString(), Selected = (selectedStore != null && store.Id == selectedStore.Id) });

            //warehouses
            model.AvailableWarehouses.Add(new SelectListItem { Text = "*", Value = "0" });
            foreach (var warehouse in await _shippingService.GetAllWarehousesAsync())
                model.AvailableWarehouses.Add(new SelectListItem { Text = warehouse.Name, Value = warehouse.Id.ToString(), Selected = (selectedWarehouse != null && warehouse.Id == selectedWarehouse.Id) });

            //warehouses
            model.AvailableVendors.Add(new SelectListItem { Text = "*", Value = "0" });
            foreach (var vendor in await _vendorService.GetAllVendorsAsync())
                model.AvailableVendors.Add(new SelectListItem { Text = vendor.Name, Value = vendor.Id.ToString(), Selected = (selectedVendor != null && vendor.Id == selectedVendor.Id) });

            //prepare available entity
            model.AvailableEntity.Add(new SelectListItem { Text = "*", Value = "0" });
            var entities = _entityGroupService.GetAllEntityGroups(null, 0, null, null);
            foreach (var e in entities)
                model.AvailableEntity.Add(new SelectListItem { Text = e.KeyGroup + " " + e.EntityId.ToString(), Value = e.Id.ToString() });

            //KeyGroup
            int number = 0;
            model.KeyGroupList.Add(new SelectListItem { Text = "Please Select", Value = number.ToString()});
            foreach (var entity in Enum.GetNames(typeof(EntityGroupTypes.KeyGroup)))
            {
                number++;
                model.KeyGroupList.Add(new SelectListItem { Text = entity, Value = number.ToString(), Selected = (model.KeyGroupId == number) });
            }

            //Key
            number = 0;
            model.KeyList.Add(new SelectListItem { Text = "Please Select", Value = number.ToString() });
            foreach (var entity in Enum.GetNames(typeof(EntityGroupTypes.Key)))
            {
                number++;
                model.KeyList.Add(new SelectListItem { Text = entity, Value = number.ToString(), Selected = (model.KeyId == number) });
            }

            return View("~/Plugins/SSI.Shipping.Manager/Views/EntityGroup/EditEntityGroupPopup.cshtml", model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> EditEntityGroupPopup(EntityGroupModel model)
        {
            if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Configure))
                return AccessDeniedView();

            int entityId = 0;
            var entityGroup = await _entityGroupService.GetEntityGroupByIdAsync(model.EntityId);
            if (model.EntityId == 0)
                return await EditEntityGroupPopup(model.Id);
            else 
                entityId = entityGroup.EntityId;

            entityGroup = await _entityGroupService.GetEntityGroupByIdAsync(model.Id);
            if (entityGroup == null)
                //no record found with the specified id
                return RedirectToAction("Manage");

            model.KeyGroup = Enum.GetName(typeof(EntityGroupTypes.KeyGroup), model.KeyGroupId - 1);
            model.Key = Enum.GetName(typeof(EntityGroupTypes.Key), model.KeyId - 1);

            if (model.KeyGroup != null && model.Key != null && model.Value != null)
            {

                entityGroup.StoreId = model.StoreId;
                entityGroup.WarehouseId = model.WarehouseId;
                entityGroup.VendorId = model.VendorId;
                entityGroup.EntityId = entityId;
                entityGroup.KeyGroup = model.KeyGroup;
                entityGroup.Key = model.Key;
                entityGroup.Value = model.Value;

                await _entityGroupService.UpdateEntityGroupAsync(entityGroup);

                ViewBag.RefreshPage = true;
            }

            return View("~/Plugins/SSI.Shipping.Manager/Views/EntityGroup/EditEntityGroupPopup.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteEntityGroup(int id)
        {
            if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Configure))
                return Content("Access denied");

            var entityGroup = await _entityGroupService.GetEntityGroupByIdAsync(id);
            if (entityGroup != null)
                await _entityGroupService.DeleteEntityGroupAsync(entityGroup);

            return new NullJsonResult();
        }

        #endregion

    }
}