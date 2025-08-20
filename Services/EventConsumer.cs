using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

using Nop.Core;
using Nop.Core.Domain.Gdpr;
using Nop.Services.Common;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.Security;
using Nop.Services.Shipping;
using Nop.Web.Areas.Admin.Models.Common;
using Nop.Web.Areas.Admin.Models.Orders;
using Nop.Web.Framework.Events;
using Nop.Web.Framework.Menu;
using Nop.Web.Framework.Models;
using Nop.Web.Models.Customer;

using Nop.Plugin.Apollo.Integrator;
using Nop.Plugin.Shipping.Manager.Settings;


using Nop.Web.Models.Sitemap;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Nop.Plugin.Shipping.Manager.Services;

/// <summary>
/// Represents the plugin event consumer
/// </summary>
public class EventConsumer :
    BaseAdminMenuCreatedEventConsumer,
    IConsumer<CustomerPermanentlyDeleted>,
    IConsumer<ModelPreparedEvent<BaseNopModel>>,
    IConsumer<ModelReceivedEvent<BaseNopModel>>,
    IConsumer<SystemWarningCreatedEvent>
{
    #region Fields

    protected readonly IAdminMenu _adminMenu;
    protected readonly IGenericAttributeService _genericAttributeService;
    protected readonly IHttpContextAccessor _httpContextAccessor;
    protected readonly ILocalizationService _localizationService;
    protected readonly IShipmentService _shipmentService;
    protected readonly IPermissionService _permissionService;
    protected readonly IWorkContext _workContext;
    protected readonly IShippingManagerService _shippingManagerService;
    protected readonly ShippingManagerSettings _shippingManagerSettings;

    #endregion

    #region Ctor

    public EventConsumer(IAdminMenu adminMenu,
        IGenericAttributeService genericAttributeService,
        IHttpContextAccessor httpContextAccessor,
        ILocalizationService localizationService,
        IShipmentService shipmentService,
        IPermissionService permissionService,
        IPluginManager<IPlugin> pluginManager,
        IWorkContext workContext,
        IShippingManagerService shippingManagerService,
        ShippingManagerSettings shippingManagerSettings) : base(pluginManager)
    {
        _adminMenu = adminMenu;
        _genericAttributeService = genericAttributeService;
        _httpContextAccessor = httpContextAccessor;
        _localizationService = localizationService;
        _shipmentService = shipmentService;
        _permissionService = permissionService;
        _workContext = workContext;
        _shippingManagerService = shippingManagerService;
        _shippingManagerSettings = shippingManagerSettings;
    }

    #endregion

    #region Utitites

    /// <summary>
    /// Checks is the current customer has rights to access this menu item
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the true if access is granted, otherwise false
    /// </returns>
    protected override async Task<bool> CheckAccessAsync()
    {
        return await _permissionService.AuthorizeAsync(StandardPermission.Configuration.MANAGE_PAYMENT_METHODS);
    }

    /// <summary>
    /// Gets the menu item
    /// </summary>
    /// <param name="plugin">The instance of <see cref="IPlugin"/> interface</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the instance of <see cref="AdminMenuItem"/>
    /// </returns>
    protected override async Task<AdminMenuItem> GetAdminMenuItemAsync(IPlugin plugin)
    {
        var descriptor = plugin.PluginDescriptor;

        AdminMenuItem mainMenuItem = new AdminMenuItem();
        AdminMenuItem entityGroupMenuItem = new AdminMenuItem();
        AdminMenuItem salesMenuItem = new AdminMenuItem();
        AdminMenuItem shipmentsMenuItem = new AdminMenuItem();
        AdminMenuItem carriersMenuItem = new AdminMenuItem();
        AdminMenuItem warehousesMenuItem = new AdminMenuItem();
        AdminMenuItem pickupPointProvidersMenuItem = new AdminMenuItem();
        AdminMenuItem datesAndRangesMenuItem = new AdminMenuItem();
        AdminMenuItem manageRates = new AdminMenuItem();
        AdminMenuItem manageMethods = new AdminMenuItem();
        AdminMenuItem configureSystem = new AdminMenuItem();
        AdminMenuItem shippingSettingMenuItem = new AdminMenuItem();

        var vendor = await _workContext.GetCurrentVendorAsync();

        var lrs = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Menu");
        if (lrs != null)
        {
            mainMenuItem = new AdminMenuItem()
            {
                SystemName = "Nop.Plugins.Shipping.Manager.Menu",
                Title = lrs,
                IconClass = "far fa-calendar-days",
                Visible = true
            };
        }

        if (_shippingManagerSettings.Enabled && await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Operate))
        {
            lrs = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Sales");
            if (lrs != null)
            {
                salesMenuItem = new AdminMenuItem()
                {
                    SystemName = "Nop.Plugins.Shipping.Manager.Sales",
                    Title = lrs,
                    Url = _adminMenu.GetMenuItemUrl("OrderSales", "OrderSales"),
                    Visible = true,
                    IconClass = "fa fa-genderless",
                };
            }

            lrs = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Shipments");
            if (lrs != null)
            {
                shipmentsMenuItem = new AdminMenuItem()
                {
                    SystemName = "Nop.Plugins.Shipping.Manager.Shipments",
                    Title = lrs,
                    Url = _adminMenu.GetMenuItemUrl("OrderOperations", "ShipmentList"),
                    Visible = true,
                    IconClass = "fa fa-genderless",
                };
            }

            lrs = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Methods");
            if (lrs != null)
            {
                manageMethods = new AdminMenuItem()
                {
                    SystemName = "Nop.Plugins.Shipping.Manager.Methods",
                    Title = lrs,
                    Url = _adminMenu.GetMenuItemUrl("ShippingOperations", "Methods"),
                    Visible = true,
                    IconClass = "fa fa-genderless",
                };
            }

            if (salesMenuItem != null)
                mainMenuItem.ChildNodes.Add(salesMenuItem);

            if (shipmentsMenuItem != null)
                mainMenuItem.ChildNodes.Add(shipmentsMenuItem);

            if (manageMethods != null)
                mainMenuItem.ChildNodes.Add(manageMethods);
        }

        if (_shippingManagerSettings.Enabled && await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Manage))
        {
            lrs = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Manage");
            if (lrs != null)
            {
                manageRates = new AdminMenuItem()
                {
                    SystemName = "Nop.Plugins.Shipping.Manager.Manage",
                    Title = lrs,
                    Url = _adminMenu.GetMenuItemUrl("ShippingManager", "Manage"),
                    Visible = true,
                    IconClass = "fa fa-genderless",
                };
            }

            lrs = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Carriers");
            if (lrs != null)
            {
                carriersMenuItem = new AdminMenuItem()
                {
                    SystemName = "Nop.Plugins.Shipping.Manager.Carriers",
                    Title = lrs,
                    Url = _adminMenu.GetMenuItemUrl("Carriers", "Carriers"),
                    Visible = true,
                    IconClass = "fa fa-genderless",
                };
            }

            lrs = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Warehouses");
            if (lrs != null)
            {
                warehousesMenuItem = new AdminMenuItem()
                {
                    SystemName = "Nop.Plugins.Shipping.Manager.Warehouses",
                    Title = lrs,
                    Url = _adminMenu.GetMenuItemUrl("ShippingOperations", "Warehouses"),
                    Visible = true,
                    IconClass = "fa fa-genderless",
                };
            }

            lrs = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.PickupPointProviders");
            if (lrs != null)
            {
                pickupPointProvidersMenuItem = new AdminMenuItem()
                {
                    SystemName = "Nop.Plugins.Shipping.Manager.PickupPointProviders",
                    Title = lrs,
                    Url = _adminMenu.GetMenuItemUrl("ShippingOperations", "PickupPointProviders"),
                    Visible = true,
                    IconClass = "fa fa-genderless",
                };
            }

            lrs = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.DatesAndRanges");
            if (lrs != null)
            {
                datesAndRangesMenuItem = new AdminMenuItem()
                {
                    SystemName = "Nop.Plugins.Shipping.Manager.DatesAndRanges",
                    Title = lrs,
                    Url = _adminMenu.GetMenuItemUrl("ShippingOperations", "DatesAndRanges"),
                    Visible = true,
                    IconClass = "fa fa-genderless",
                };
            }

            if (manageRates != null)
                mainMenuItem.ChildNodes.Add(manageRates);

            if (carriersMenuItem != null)
                mainMenuItem.ChildNodes.Add(carriersMenuItem);

            if (warehousesMenuItem != null)
                mainMenuItem.ChildNodes.Add(warehousesMenuItem);

            if (datesAndRangesMenuItem != null)
                mainMenuItem.ChildNodes.Add(datesAndRangesMenuItem);

            if (pickupPointProvidersMenuItem != null)
                mainMenuItem.ChildNodes.Add(pickupPointProvidersMenuItem);

        }

        if (await _permissionService.AuthorizeAsync(StandardPermission.Configuration.MANAGE_PLUGINS) && vendor == null)
        {
            lrs = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Configuration");
            if (lrs != null)
            {
                configureSystem = new AdminMenuItem()
                {
                    SystemName = "Nop.Plugins.Shipping.Manager.Configuration",
                    Title = lrs,
                    Url = _adminMenu.GetMenuItemUrl("ShippingManagerConfiguration", "Configure"),
                    Visible = true,
                    IconClass = "fa fa-genderless",
                };
            }

            if (configureSystem != null && await _workContext.GetCurrentVendorAsync() == null)
                mainMenuItem.ChildNodes.Add(configureSystem);
        }

        if (_shippingManagerSettings.Enabled && await _permissionService.AuthorizeAsync(StandardPermission.Configuration.MANAGE_PLUGINS) && vendor == null)
        {
            lrs = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.EntityGroup");
            if (lrs != null)
            {
                entityGroupMenuItem = new AdminMenuItem()
                {
                    SystemName = "Nop.Plugins.Shipping.Manager.EntityGroup",
                    Title = lrs,
                    Url = _adminMenu.GetMenuItemUrl("EntityGroup", "EntityGroup"),
                    Visible = true,
                    IconClass = "fa fa-genderless",
                };
            }

            lrs = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.ShippingSettings");
            if (lrs != null)
            {
                shippingSettingMenuItem = new AdminMenuItem()
                {
                    SystemName = "Nop.Plugins.Shipping.Manager.ShippingSettings",
                    Title = lrs,
                    Url = _adminMenu.GetMenuItemUrl("ShippingSetting", "Shipping"),
                    Visible = true,
                    IconClass = "fa fa-genderless",
                };
            }

            if (entityGroupMenuItem != null && await _workContext.GetCurrentVendorAsync() == null)
                mainMenuItem.ChildNodes.Add(entityGroupMenuItem);

            if (shippingSettingMenuItem != null)
                mainMenuItem.ChildNodes.Add(shippingSettingMenuItem);
        }

        return mainMenuItem;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Handle customer permanently deleted event
    /// </summary>
    /// <param name="eventMessage">Event message</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task HandleEventAsync(CustomerPermanentlyDeleted eventMessage)
    {
        //delete customer's details
    }

    /// <summary>
    /// Handle model prepared event
    /// </summary>
    /// <param name="eventMessage">Event message</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task HandleEventAsync(ModelPreparedEvent<BaseNopModel> eventMessage)
    {
        if (eventMessage.Model is not CustomerNavigationModel navigationModel)
            return;

        ////add a new menu item in the customer navigation
        //var orderItem = navigationModel.CustomerNavigationItems.FirstOrDefault(item => item.Tab == (int)CustomerNavigationEnum.Orders);
        //var position = navigationModel.CustomerNavigationItems.IndexOf(orderItem) + 1;
        //navigationModel.CustomerNavigationItems.Insert(position, new()
        //{
        //    RouteName = PayPalCommerceDefaults.Route.PaymentTokens,
        //    ItemClass = "paypal-payment-tokens",
        //    Tab = PayPalCommerceDefaults.PaymentTokensMenuTab,
        //    Title = await _localizationService.GetResourceAsync("Plugins.Payments.PayPalCommerce.PaymentTokens")
        //});
    }

    /// <summary>
    /// Handle model received event
    /// </summary>
    /// <param name="eventMessage">Event message</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task HandleEventAsync(ModelReceivedEvent<BaseNopModel> eventMessage)
    {
        if (eventMessage.Model is not ShipmentModel shipmentModel)
            return;
    }

    /// <summary>
    /// Handle system warning created event
    /// </summary>
    /// <param name="eventMessage">Event message</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public Task HandleEventAsync(SystemWarningCreatedEvent eventMessage)
    {
        //if (!_settings.MerchantIdRequired)
        //    return Task.CompletedTask;

        return Task.CompletedTask;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the plugin system name
    /// </summary>
    protected override string PluginSystemName => ShippingManagerDefaults.SystemName;

    /// <summary>
    /// Menu item insertion type
    /// </summary>
    protected override MenuItemInsertType InsertType => MenuItemInsertType.TryAfterThanBefore;

    /// <summary>
    /// The system name of the menu item after with need to insert the current one
    /// </summary>
    protected override string AfterMenuSystemName => "Reports";

    /// <summary>
    /// The system name of the menu item before with need to insert the current one
    /// </summary>
    protected override string BeforeMenuSystemName => "Local plugins";

    #endregion

}