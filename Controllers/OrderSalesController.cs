using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;

using Nop.Core;
using Nop.Core.Caching;
using Nop.Data;
using Nop.Core.Infrastructure;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Logging;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Services.Configuration;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Plugins;
using Nop.Services.Security;
using Nop.Services.Shipping;
using Nop.Services.Stores;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Models.Extensions;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Areas.Admin.Factories;

using Nop.Services.Directory;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Web.Areas.Admin.Models.Payments;
using IProductModelFactory = Nop.Web.Factories.IProductModelFactory;

using Nop.Services.Common;
using Nop.Services.Logging;
using Nop.Services.Seo;

using Nop.Plugin.Shipping.Manager.Domain;
using Nop.Plugin.Shipping.Manager.ExportImport;
using Nop.Plugin.Shipping.Manager.Factories;
using Nop.Plugin.Shipping.Manager.Models.Order;
using Nop.Plugin.Shipping.Manager.Services;
using Nop.Plugin.Shipping.Manager.Settings;

using Nop.Plugin.Apollo.Integrator;
using Nop.Plugin.Apollo.Integrator.Services;

namespace Nop.Plugin.Shipping.Manager.Controllers;

public class OrderSalesController : BasePluginController
{

    #region Fields

    protected readonly IWorkContext _workContext;
    protected readonly IStoreService _storeService;
    protected readonly IAddressService _addressService;
    protected readonly ICountryService _countryService;
    protected readonly ICustomerService _customerService;
    protected readonly ILocalizationService _localizationService;
    protected readonly IPermissionService _permissionService;
    protected readonly ISettingService _settingService;
    protected readonly IStoreContext _storeContext;
    protected readonly IProductService _productService;
    protected readonly IProductModelFactory _productModelFactory;
    protected readonly HttpContext _context;
    protected readonly IHttpContextAccessor _httpContextAccessor;
    protected readonly IPriceFormatter _priceFormatter;
    protected readonly ICurrencyService _currencyService;
    protected readonly IProductAttributeService _productAttributeService;
    protected readonly IOrderService _orderService;
    protected readonly IDateTimeHelper _dateTimeHelper;
    protected readonly IPaymentService _paymentService;
    protected readonly IPaymentModelFactory _paymentModelFactory;
    protected readonly ILogger _logger;
    protected readonly IExportImportManager _exportImportManager;
    protected readonly ICustomerActivityService _customerActivityService;
    protected readonly IOrderProcessingService _orderProcessingService;
    protected readonly IGenericAttributeService _genericAttributeService;
    protected readonly IOrderItemPdfService _orderItemPdfService;
    protected readonly OrderSettings _orderSettings;
    protected readonly IOrderModelFactory _orderModelFactory;
    protected readonly ShoppingCartSettings _shoppingCartSettings;
    protected readonly IUrlRecordService _urlRecordService;
    protected readonly IProductTemplateService _productTemplateService;
    protected readonly INotificationService _notificationService;
    protected readonly IPluginService _pluginService;
    protected readonly IPaymentPluginManager _paymentPluginManager;
    protected readonly IShoppingCartService _shoppingCartService;
    protected readonly ICategoryService _categoryService;
    protected readonly IStaticCacheManager _cacheManager;
    protected readonly IRecentlyViewedProductsService _recentlyViewedProductsService;
    protected readonly INopFileProvider _fileProvider;
    protected readonly IShippingPluginManager _shippingPluginManager;
    protected readonly IWebHelper _webHelper;
    protected readonly IShipmentService _shipmentService;
    protected readonly IRepository<OrderItem> _orderItemRepository;
    protected readonly IRepository<Product> _productRepository;
    protected readonly IShippingManagerService _shippingManagerService;
    protected readonly IOrderSalesService _orderSalesService;
    protected readonly ShippingManagerSettings _shippingManagerSettings;
    protected readonly IShippingService _shippingService;
    protected readonly IOrderOperationsModelFactory _orderOperationsModelFactory;
    protected readonly ShippingSettings _shippingSettings;
    protected readonly IEntityGroupService _entityGroupService;
    protected readonly IPackagingOptionService _packagingOptionService;
    protected readonly ISendcloudService _sendcloudService;

    #endregion

    #region Ctor

    public OrderSalesController(IWorkContext workContext,
        IStoreService storeService,
        IAddressService addressService,
        ICountryService countryService,
        ICustomerService customerService,
        ILocalizationService localizationService,
        IPermissionService permissionService,
        ISettingService settingService,
        IStoreContext storeContext,
        IProductService productService,
        IProductModelFactory productModelFactory,
        IHttpContextAccessor httpContextAccessor,
        IPriceFormatter priceFormatter,
        ICurrencyService currencyService,
        IProductAttributeService productAttributeService,
        IDateTimeHelper dateTimeHelper,
        ILogger logger,
        IExportImportManager exportImportManager,
        ICustomerActivityService customerActivityService,
        IOrderProcessingService orderProcessingService,
        IOrderService orderService,
        IGenericAttributeService genericAttributeService,
        IPaymentService paymentService,
        IPaymentModelFactory paymentModelFactory,
        IOrderItemPdfService orderItemPdfService,
        OrderSettings orderSettings,
        IOrderModelFactory orderModelFactory,
        ShoppingCartSettings shoppingCartSettings,
        IUrlRecordService urlRecordService,
        IProductTemplateService productTemplateService,
        INotificationService notificationService,
        IPluginService pluginService,
        IPaymentPluginManager paymentPluginManager,
        IShoppingCartService shoppingCartService,
        ICategoryService categoryService,
        IStaticCacheManager cacheManager,
        IRecentlyViewedProductsService recentlyViewedProductsService,
        INopFileProvider fileProvider,
        IShippingPluginManager shippingPluginManager,
        IWebHelper webHelper,
        IShipmentService shipmentService,
        IRepository<OrderItem> orderItemRepository,
        IRepository<Product> productRepository,
        IShippingManagerService shippingManagerService,
        IOrderSalesService orderSalesService,
        ShippingManagerSettings shippingManagerSettings,
        IShippingService shippingService,
        IOrderOperationsModelFactory orderOperationsModelFactory,
        ShippingSettings shippingSettings,
        IEntityGroupService entityGroupService,
        IPackagingOptionService packagingOptionService,
        ISendcloudService sendcloudService)
    {
        _workContext = workContext;
        _storeService = storeService;
        _addressService = addressService;
        _countryService = countryService;
        _customerService = customerService;
        _localizationService = localizationService;
        _permissionService = permissionService;
        _settingService = settingService;
        _storeContext = storeContext;
        _productService = productService;
        _productModelFactory = productModelFactory;
        _httpContextAccessor = httpContextAccessor;
        _context = this._httpContextAccessor.HttpContext;
        _priceFormatter = priceFormatter;
        _currencyService = currencyService;
        _productAttributeService = productAttributeService;
        _orderService = orderService;
        _paymentService = paymentService;
        _paymentModelFactory = paymentModelFactory;
        _dateTimeHelper = dateTimeHelper;
        _logger = logger;
        _exportImportManager = exportImportManager;
        _customerActivityService = customerActivityService;
        _orderProcessingService = orderProcessingService;
        _genericAttributeService = genericAttributeService;
        _orderItemPdfService = orderItemPdfService;
        _orderSettings = orderSettings;
        _orderModelFactory = orderModelFactory;
        _shoppingCartSettings = shoppingCartSettings;
        _urlRecordService = urlRecordService;
        _productTemplateService = productTemplateService;
        _notificationService = notificationService;
        _pluginService = pluginService;
        _paymentPluginManager = paymentPluginManager;
        _shoppingCartService = shoppingCartService;
        _categoryService = categoryService;
        _cacheManager = cacheManager;
        _recentlyViewedProductsService = recentlyViewedProductsService;
        _fileProvider = fileProvider;
        _shippingPluginManager = shippingPluginManager;
        _webHelper = webHelper;
        _shipmentService = shipmentService;
        _orderItemRepository = orderItemRepository;
        _productRepository = productRepository;
        _shippingManagerService = shippingManagerService;
        _orderSalesService = orderSalesService;
        _shippingManagerSettings = shippingManagerSettings;
        _shippingService = shippingService;
        _orderOperationsModelFactory = orderOperationsModelFactory;
        _shippingSettings = shippingSettings;
        _entityGroupService = entityGroupService;
        _packagingOptionService = packagingOptionService;
        _sendcloudService = sendcloudService;   
    }

    #endregion

    #region Utilities

        protected virtual async ValueTask<bool> HasAccessToInvoiceAsync(Order order)
        {
            return order != null && await HasAccessToInvoiceAsync(order.Id);
        }

        protected virtual async Task<bool> HasAccessToInvoiceAsync(int orderId)
        {
            if (orderId == 0)
                return false;

            var currentVendor = await _workContext.GetCurrentVendorAsync();
            if (currentVendor == null)
                //not a vendor; has access
                return true;

            var vendorId = currentVendor.Id;
            var hasVendorProducts = (await _orderService.GetOrderItemsAsync(orderId, vendorId: vendorId)).Any();

            return hasVendorProducts;
        }

        public bool HasOrderShipment(Order order)
        {
            if (_orderSalesService.GetShipmentsByOrderId(order.Id).Count() > 0)
                return true;

        return false;
    }

    protected virtual async Task<bool> HasAccessToOrderAsync(Order order)
    {
        return await HasAccessToOrderAsync(order.Id);
    }

    protected virtual async Task<bool> HasAccessToShipmentAsync(Shipment shipment)
    {
        if (shipment == null)
            throw new ArgumentNullException(nameof(shipment));

        if (await _workContext.GetCurrentVendorAsync() == null)
            //not a vendor; has access
            return true;

        return await HasAccessToOrderAsync(shipment.OrderId);
    }

    protected virtual async Task<bool> HasAccessToOrderAsync(int orderId)
    {
        if (orderId == 0)
            return false;

        if (await _workContext.GetCurrentVendorAsync() == null)
            //not a vendor; has access
            return true;

        var vendorId = await _entityGroupService.GetActiveVendorScopeAsync();
        var hasVendorProducts = _orderSalesService.GetOrderItems(orderId, vendorId: vendorId).Any();

        return hasVendorProducts;
    }

    protected virtual async ValueTask<bool> HasAccessToProductAsync(OrderItem orderItem)
    {
        if (orderItem == null || orderItem.ProductId == 0)
            return false;

        if (await _workContext.GetCurrentVendorAsync() == null)
            //not a vendor; has access
            return true;

        var vendorId = (await _workContext.GetCurrentVendorAsync()).Id;

        return (await _productService.GetProductByIdAsync(orderItem.ProductId))?.VendorId == vendorId;
    }

    protected virtual async Task LogEditOrderAsync(int orderId)
    {
        var order = await _orderService.GetOrderByIdAsync(orderId);

        await _customerActivityService.InsertActivityAsync("EditOrder",
            string.Format(await _localizationService.GetResourceAsync("ActivityLog.EditOrder"), order.CustomOrderNumber), order);
    }

    protected virtual async Task<bool> HasAccessToOrderItemAsync(OrderItem orderItem)
    {
        if (orderItem == null)
            throw new ArgumentNullException(nameof(orderItem));

        var vendor = await _workContext.GetCurrentVendorAsync();
        if (vendor == null)
            //not a vendor; has access
            return true;

        var vendorId = vendor.Id;
        var product = await _productService.GetProductByIdAsync(orderItem.ProductId);

        return product.VendorId == vendorId;
    }

    #endregion

    #region Currency Methods

    public async Task<IActionResult> ConvertCurrencyToCurrentStore(decimal total = 0)
    {
        var totalbookingprice = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(total, await _workContext.GetWorkingCurrencyAsync());
        var totalbookingprice_wc = await _priceFormatter.FormatPriceAsync(totalbookingprice);
        return Json(new { Totalbookingprice = totalbookingprice_wc });
    }

    public async Task<decimal> ConvertCostToCurrentStoreCurrency(decimal total = 0)
    {
        var totalbookingprice = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(total, await _workContext.GetWorkingCurrencyAsync());
        return totalbookingprice;
    }

    public async Task<IActionResult> FormatCurrencyForCurrentStore(decimal total = 0)
    {
        var totalbookingprice_wc = await _priceFormatter.FormatPriceAsync(total);
        return Json(new { Totalbookingprice = totalbookingprice_wc });
    }

    #endregion

    #region Order sales

    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    public virtual async Task<IActionResult> OrderSales()
    {
        if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Operate))
            return AccessDeniedView();

        //prepare model
        var model = await PrepareOrderSalesSearchModelAsync(new OrderSalesSearchModel());

        return View("~/Plugins/SSI.Shipping.Manager/Views/Order/OrderSales.cshtml", model);
    }

    private async Task<OrderSalesSearchModel> PrepareOrderSalesSearchModelAsync(OrderSalesSearchModel searchModel)
    {
        var methodsModel = new PaymentMethodRestrictionModel();
        var paymentService = await _paymentModelFactory.PreparePaymentMethodRestrictionModelAsync(methodsModel);
        searchModel.AvailablePaymentMethods.Add(new SelectListItem()
        {
            Text = "All"
        });

        int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

        searchModel.SearchProductName = string.Empty;

        foreach (var method in paymentService.AvailablePaymentMethods)
        {
            searchModel.AvailablePaymentMethods.Add(new SelectListItem()
            {
                Text = method.FriendlyName,
                Value = method.SystemName
            });
        }

        //prepare page parameters
        searchModel.SetGridPageSize();

        return searchModel;
    }

    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    public virtual async Task<IActionResult> OrderSalesList(OrderSalesSearchModel searchModel)
    {

        //prepare model
        var model = await PrepareOrderSalesListModelAsync(searchModel);

        return Json(model);
    }

    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    [HttpPost]
    public virtual async Task<IActionResult> OrderItemsByOrderId(OrderItemSearchModel searchModel)
    {
        if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Operate))
            return AccessDeniedView();

        //try to get a shipment with the specified id
        var order = await _orderService.GetOrderByIdAsync(searchModel.OrderId)
            ?? throw new ArgumentException("No order found with the specified id");

        //a vendor should have access only to his products
        if (await _workContext.GetCurrentVendorAsync() != null && !await HasAccessToOrderAsync(order))
            return Content(string.Empty);

        //prepare model
        searchModel.SetGridPageSize();
        var model = await PrepareOrderItemListModel(searchModel, order);

        return Json(model);
    }

    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    public virtual ActionResult OrderSalesListDelete(OrderSalesModel model)
    {
        return new NullJsonResult();
    }

    /// <summary>
    /// Prepare paged order list model
    /// </summary>
    /// <param name="searchModel">Order sales search model</param>

    /// <returns>Shipment item list model</returns>
    public virtual async Task<OrderSalesListModel> PrepareOrderSalesListModelAsync(OrderSalesSearchModel searchModel)
    {

        var orderIds = await _orderSalesService.GetAllOrderSalesItemsAsync();
        var orders = await _orderSalesService.GetSalesOrderListAsync(orderIds, searchModel.PaymentMethod,
                searchModel.FromDate, searchModel.ToDate, searchModel.Ispay, searchModel.OrderbyName,
                searchModel.SearchProductName,
                pageIndex: searchModel.Page - 1, pageSize: searchModel.PageSize);

        var model = await new OrderSalesListModel().PrepareToGridAsync(searchModel, orders, () =>
        {
            //fill in model values from the entity

            var orderList = orders.SelectAwait(async item =>
            {
                var order = await _orderService.GetOrderByIdAsync(item.Id);
                var paymentMethod = await _paymentPluginManager.LoadPluginBySystemNameAsync(order.PaymentMethodSystemName);
                var paymentMethodFriendlyName = paymentMethod != null ?
                    await _localizationService.GetLocalizedFriendlyNameAsync(paymentMethod, (await _workContext.GetWorkingLanguageAsync()).Id) : order.PaymentMethodSystemName;
                var shippingMethod = await _shippingPluginManager.LoadPluginBySystemNameAsync(order.ShippingRateComputationMethodSystemName);
                var shippingMethodFriendlyName = shippingMethod != null ? 
                    await _localizationService.GetLocalizedFriendlyNameAsync(shippingMethod, (await _workContext.GetWorkingLanguageAsync()).Id) : order.ShippingRateComputationMethodSystemName;
                var startDateValue = "";

                try
                {
                    startDateValue = order.PaidDateUtc.HasValue
                            ? (await _dateTimeHelper.ConvertToUserTimeAsync(order.PaidDateUtc.Value, DateTimeKind.Utc)).ToShortDateString()
                                : await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Admin.PdfReport.NotPaid");
                }
                catch (Exception ex)
                {
                    var error = ex;
                }

                var orders = new OrderSalesModel()
                {
                    Id = item.Id,
                    CustomerEmail = await _orderSalesService.GetCustomerEmailDetailsAsync(item),
                    CustomerFullName = await _orderSalesService.GetCustomerFullNameDetailsAsync(item),
                    PaymentStatus = await _localizationService.GetLocalizedEnumAsync(order.PaymentStatus),
                    PaymentDate = startDateValue,
                    TotalPrice = await _priceFormatter.FormatPriceAsync(item.OrderTotal, true, false),
                    PaymentMethodSystemName = paymentMethodFriendlyName,
                    ShippingMethodSystemName = shippingMethodFriendlyName,
                    DisplayUrl = _orderSalesService.GetEditOrderPageUrl(item.Id),
                    Status = await _orderSalesService.GetOrderShippingDetailsAsync(item),
                    ShipmentUrl = Url.Action("AddShipment", "Order", new { id = order.Id }),
                    OrderCreatedDate = order.CreatedOnUtc 
                };

                return orders;
            });

            if (searchModel.OrderbyName)
                orderList = orderList.OrderBy(x => x.CustomerFullName);
            else if (_shippingManagerSettings.OrderByDate)
                orderList = orderList.OrderBy(x => x.OrderCreatedDate);

            return orderList;
        });

        return model;
    }

    /// <summary>
    /// Prepare paged order item list model
    /// </summary>
    /// <param name="searchModel">Order item search model</param>
    /// <param name="shipment">Shipment</param>
    /// <returns>Shipment item list model</returns>
    public virtual async Task<OrderItemListModel> PrepareOrderItemListModel(OrderItemSearchModel searchModel, Order order)
    {
        if (searchModel == null)
            throw new ArgumentNullException(nameof(searchModel));

        if (order == null)
            throw new ArgumentNullException(nameof(order));

        //get shipments
        var orderItems = await _orderService.GetOrderItemsAsync(order.Id);

        var orderItemsList = orderItems.ToList().ToPagedList(searchModel);

        if (_shippingManagerSettings.TestMode)
        {
            await _logger.InsertLogAsync(LogLevel.Information, "Order Items Count: " + orderItems.Count(), null, null);
        }

        //prepare list model
        var model = await new OrderItemListModel().PrepareToGridAsync(searchModel, orderItemsList, () =>
        {
            //fill in model values from the entity
            return orderItems.SelectAwait(async item =>
            {
                var product = await _productService.GetProductByIdAsync(item.ProductId);

                //fill in model values from the entity
                var orderItemModel = new OrderItemModel
                {
                    Id = item.Id,
                    ProductName = product.Name,
                    AttributeInfo = item.AttributeDescription,
                    Quantity = item.Quantity,
                    Price = await _priceFormatter.FormatPriceAsync(item.PriceInclTax, true, false),
                    Warehouse = string.Join(",", (await _orderSalesService.GetOrderItemShippingDetailsAsync(item)).Select(x => x.Name))
                };

                //fill in additional values (not existing in the entity)

                return orderItemModel;
            });
        });

        if (_shippingManagerSettings.TestMode)
        {
            await _logger.InsertLogAsync(LogLevel.Information, "Model Count: " + model.RecordsTotal, null, null);
        }

        return model;
    }

    #endregion

    #region Order Sales Shipment

    public async Task<IActionResult> AddShipment(int id, string btnId)
    {
        bool closeWindow = false;

        //try to get an order with the specified id
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null)
            closeWindow = true;

        //a vendor should have access only to his products
        if (await _workContext.GetCurrentVendorAsync() != null && !await HasAccessToOrderAsync(order.Id))
            closeWindow = true;

        if (order.PaymentStatus != PaymentStatus.Paid) //ToDo: Add setting
        {
            _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Sales.Fields.Ispay"));
        }

        //prepare model
        var model = await PrepareOrderShipmentModelAsync(new OrderShipmentModel(), null, order);

        model.BtnId = btnId;

        if (model.Items.Count == 0 || closeWindow)
        {
            ViewBag.RefreshPage = true;
            ViewBag.Message = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Sales.NoOrderItemsToShip");
            ViewBag.BtnId = btnId;
        }
        else
        {
            var itemsFromMultipleWarehouses = model.Items.Where(x => x.AllowToChooseWarehouse && x.AvailableWarehouses.Count > 1).ToList();
            if (itemsFromMultipleWarehouses.Count == 0)
            {
                bool added = false;
                var itemsHaveWarehouses = model.Items.All(x => x.AvailableWarehouses.Count == 1);
                var itemsSomeHaveWarehouses = model.Items.Any(x => x.AvailableWarehouses.Count == 1);

                    if (itemsHaveWarehouses)
                    {
                        model.AdminComment = "Automatically created shipment for a warehouse";
                        if (order.ShippingRateComputationMethodSystemName != ShippingManagerDefaults.SendCloudSystemName)
                            model.TrackingNumber = order.Id.ToString();

                        added = await InsertShipmentForWarehouseAsync(model);
                    }
                    else
                    {
                        model.AdminComment = "Automatically created shipment";
                        if (order.ShippingRateComputationMethodSystemName != ShippingManagerDefaults.SendCloudSystemName)
                            model.TrackingNumber = order.Id.ToString();

                    added = await InsertShipmentAsync(model);
                }

                if (added)
                {
                    ViewBag.RefreshPage = true;
                    ViewBag.Message = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Sales.ShipmentAdded");
                    ViewBag.BtnId = btnId;
                }

                if (!itemsSomeHaveWarehouses)
                {
                    ViewBag.RefreshPage = true;
                    ViewBag.Message = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Sales.NoWarehousesAvailable");
                    ViewBag.BtnId = btnId;
                }

            }
        }

        return View("~/Plugins/SSI.Shipping.Manager/Views/Order/AddShipmentPopup.cshtml", model);
    }

    [HttpPost]
    public virtual async Task<IActionResult> AddShipment(string btnId, OrderShipmentModel model, IFormCollection form)
    {
        PackagingOption packagingOption = null;

        //try to get an order with the specified id
        var order = await _orderService.GetOrderByIdAsync(model.OrderId);
        if (order == null)
            return RedirectToAction("List");

        //a vendor should have access only to his products
        if (await _workContext.GetCurrentVendorAsync() != null && !await HasAccessToOrderAsync(order))
            return RedirectToAction("List");

        var orderItems = _orderSalesService.GetOrderItems(order.Id, isShipEnabled: true);

        //a vendor should have access only to his products
        if (await _workContext.GetCurrentVendorAsync() != null)
        {
            orderItems = await orderItems.WhereAwait(HasAccessToProductAsync).ToListAsync();
        }

        var shipment = new Shipment
        {
            OrderId = order.Id,
            TrackingNumber = model.TrackingNumber,
            TotalWeight = null,
            AdminComment = model.AdminComment,
            CreatedOnUtc = DateTime.UtcNow
        };

        var shipmentItems = new List<ShipmentItem>();

        decimal? totalWeight = null;

        foreach (var orderItem in orderItems)
        {
            var product = await _productService.GetProductByIdAsync(orderItem.ProductId);

            //ensure that this product can be shipped (have at least one item to ship)
            var maxQtyToAdd = await _orderService.GetTotalNumberOfItemsCanBeAddedToShipmentAsync(orderItem);
            if (maxQtyToAdd <= 0)
                continue;

            var qtyToAdd = 0; //parse quantity
            foreach (var formKey in form.Keys)
                if (formKey.Equals($"qtyToAdd{orderItem.Id}", StringComparison.InvariantCultureIgnoreCase))
                {
                    int.TryParse(form[formKey], out qtyToAdd);
                    break;
                }

            var warehouseId = 0;
            if (product.ManageInventoryMethod == ManageInventoryMethod.ManageStock &&
                product.UseMultipleWarehouses)
            {
                //multiple warehouses supported
                //warehouse is chosen by a store owner
                foreach (var formKey in form.Keys)
                    if (formKey.Equals($"warehouse_{orderItem.Id}", StringComparison.InvariantCultureIgnoreCase))
                    {
                        int.TryParse(form[formKey], out warehouseId);
                        break;
                    }
            }
            else
            {
                //multiple warehouses are not supported
                warehouseId = product.WarehouseId;
            }

            //validate quantity
            if (qtyToAdd <= 0)
                continue;
            if (qtyToAdd > maxQtyToAdd)
                qtyToAdd = maxQtyToAdd;

            //ok. we have at least one item. let's create a shipment (if it does not exist)

            var orderItemTotalWeight = orderItem.ItemWeight * qtyToAdd;
            if (orderItemTotalWeight.HasValue)
            {
                if (!totalWeight.HasValue)
                    totalWeight = 0;
                totalWeight += orderItemTotalWeight.Value;
            }

            //create a shipment item
            shipmentItems.Add(new ShipmentItem
            {
                OrderItemId = orderItem.Id,
                Quantity = qtyToAdd,
                WarehouseId = warehouseId
            });

            var quantityWithReserved = await _productService.GetTotalStockQuantityAsync(product, true, warehouseId);
            var quantityTotal = await _productService.GetTotalStockQuantityAsync(product, false, warehouseId);

            //currently reserved in current stock
            var quantityReserved = quantityTotal - quantityWithReserved;

            //If the quantity of the reserve product in the warehouse does not coincide with the total quantity of goods in the basket, 
            //it is necessary to redistribute the reserve to the warehouse
            if (!(quantityReserved == qtyToAdd && quantityReserved == maxQtyToAdd))
                await _orderSalesService.BalanceInventoryAsync(product, warehouseId, qtyToAdd);
        }

        //if we have at least one item in the shipment, then save it
        if (shipmentItems.Any())
        {
            packagingOption = await _shippingManagerService.GetDefaultPackagingOption();
            if (packagingOption != null)
                totalWeight += packagingOption.Weight;

            shipment.TotalWeight = totalWeight;

            await _shipmentService.InsertShipmentAsync(shipment);

            if (packagingOption != null)
                await _shippingManagerService.InsertShipmentDetails(shipment, packagingOption);

            foreach (var shipmentItem in shipmentItems)
            {
                shipmentItem.ShipmentId = shipment.Id;
                await _shipmentService.InsertShipmentItemAsync(shipmentItem);
            }

            //add a note
            await _orderSalesService.AddOrderNoteAsync(order, "A shipment has been added");

            if (model.CanShip)
                await _orderProcessingService.ShipAsync(shipment, true);

            if (model.CanShip && model.CanDeliver)
                await _orderProcessingService.DeliverAsync(shipment, true);

            await LogEditOrderAsync(order.Id);

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Orders.Shipments.Added"));

            return RedirectToAction("OrderShipmentDetails", new { id = shipment.Id, btnId = model.BtnId, function = "Add" });
        }

        _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Admin.Orders.Shipments.NoProductsSelected"));

        return RedirectToAction("AddShipment", model);
    }

    public async Task<IActionResult> EditShipment(int id, string btnId)
    {
        var shipmentItem = await _orderSalesService.GetShipmentItemByIdAsync(id);
        if (shipmentItem != null)
        {
            var shipment = await _shipmentService.GetShipmentByIdAsync(shipmentItem.ShipmentId);

            //try to get an order with the specified id
            var order = await _orderService.GetOrderByIdAsync(shipment.OrderId);
            if (order == null)
                return RedirectToAction("List");

            //a vendor should have access only to his products
            if (await _workContext.GetCurrentVendorAsync() != null && !await HasAccessToOrderAsync(order))
                return RedirectToAction("List");

            //prepare model
            var model = await PrepareOrderShipmentEditModelAsync(new OrderShipmentModel(), shipment, shipmentItem, order);

            model.BtnId = btnId;

            if (model.Items.Count == 0)
            {
                ViewBag.RefreshPage = true;
                ViewBag.Message = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Sales.NoOrderItemsToShip");
                ViewBag.BtnId = btnId;
            }

            return View("~/Plugins/SSI.Shipping.Manager/Views/Order/EditShipmentPopup.cshtml", model);
        }

        return RedirectToAction("List");
    }

    [HttpPost]
    public virtual async Task<IActionResult> EditShipment(string btnId, OrderShipmentModel model, IFormCollection form)
    {

        string message = "A shipment adjustment been made";

        var qtyToAdjust = 0; //parse quantity

        if (model.ServicePointId != null)
        {
            try
            {
                if (int.TryParse(model.ServicePointId, out int servicePoint))
                {
                    bool checkServicePoint = await _sendcloudService.SendcloudIsServicePointAvailableAsync(servicePoint);
                    if (!checkServicePoint)
                    {
                        _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Shipment.Sendcloud.ServicePointError"));
                        return RedirectToAction("EditShipment", model);
                    }
                }
                else
                {
                    _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Shipment.Sendcloud.ServicePointError"));
                    return RedirectToAction("EditShipment", model);
                }
            }
            catch
            {
                _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Shipment.Sendcloud.ServicePointError"));
                return RedirectToAction("EditShipment", model);
            }
        }

        //try to get an order with the specified id
        var order = await _orderService.GetOrderByIdAsync(model.OrderId);
        if (order == null)
            return RedirectToAction("List");

        //a vendor should have access only to his products
        if (await _workContext.GetCurrentVendorAsync() != null && !await HasAccessToOrderAsync(order))
            return RedirectToAction("List");

        var orderItems = _orderSalesService.GetOrderItems(order.Id, isShipEnabled: true);
        //a vendor should have access only to his products
        if (await _workContext.GetCurrentVendorAsync() != null)
        {
            orderItems = await orderItems.WhereAwait(HasAccessToProductAsync).ToListAsync();
        }

        var shipmentItem = await _orderSalesService.GetShipmentItemByIdAsync(model.Id);
        if (shipmentItem != null)
        {
            var shipment = await _shipmentService.GetShipmentByIdAsync(shipmentItem.ShipmentId);
            if (shipment != null)
            {

                message += " - Shipment Id: " + shipment.Id.ToString();

                var updateShipmentItems = new List<ShipmentItem>();
                var shipmentItems = _orderSalesService.GetShipmentItemsByShipmentId(shipment.Id);

                decimal? totalWeight = shipment.TotalWeight;

                foreach (var orderItem in orderItems)
                {

                    var updateShipmentItem = shipmentItems.Where(oi => oi.OrderItemId == orderItem.Id).FirstOrDefault();
                    if (updateShipmentItem != null)
                    {

                        //ensure that this product can be shipped (have at least one item to ship)
                        var maxQtyToAdd = await _orderService.GetTotalNumberOfItemsCanBeAddedToShipmentAsync(orderItem);

                        var product = await _productService.GetProductByIdAsync(orderItem.ProductId);


                        int shippingMethodId = 0;
                        int packagingOptionId = 0;

                        string trackingNumber = shipment.TrackingNumber;
                        string adminComment = shipment.TrackingNumber;

                        foreach (var formKey in form.Keys)
                        {
                            if (formKey.Equals($"qtyToAdd{orderItem.Id}", StringComparison.InvariantCultureIgnoreCase))
                            {
                                int.TryParse(form[formKey], out qtyToAdjust);
                            }

                            if (formKey.Equals($"TrackingNumber", StringComparison.InvariantCultureIgnoreCase))
                            {
                                trackingNumber = form[formKey];
                            }

                            if (formKey.Equals($"AdminComment", StringComparison.InvariantCultureIgnoreCase))
                            {
                                adminComment = form[formKey];
                            }

                            if (formKey.Equals($"ShippingMethodId", StringComparison.InvariantCultureIgnoreCase))
                            {
                                int.TryParse(form[formKey], out shippingMethodId);
                            }

                            if (formKey.Equals($"packagingOptions_{orderItem.Id}", StringComparison.InvariantCultureIgnoreCase))
                            {
                                int.TryParse(form[formKey], out packagingOptionId);
                                break;
                            }

                        }

                        if (shipment.TrackingNumber != trackingNumber)
                        {
                            shipment.TrackingNumber = trackingNumber;
                            message += " - Tracking Number : " + shipment.TrackingNumber;
                        }

                        shipment.AdminComment = adminComment;

                        var shippingMethod = (await _shippingService.GetAllShippingMethodsAsync()).Where(sm => sm.Id == shippingMethodId).FirstOrDefault();
                        if (shippingMethod != null)
                        {
                            order.ShippingMethod = shippingMethod.Name;

                            var customValues = new Dictionary<string, object>();
                            var newCustomValues = new Dictionary<string, object>();

                            // Add new values
                            string lrs = await _localizationService.GetResourceAsync("Service Point Id");
                            customValues.Add(lrs, model.ServicePointId);
                            lrs = await _localizationService.GetResourceAsync("Service Point PO Number");
                            customValues.Add(lrs, model.ServicePointPOBox);

                            // Get existing values
                            var customValuesXml = _sendcloudService.DeserializeCustomValues(order);
                            foreach (var key in customValuesXml)
                            {
                                if (!customValues.ContainsKey(key.Key))
                                    newCustomValues.Add(key.Key, key.Value);
                            }

                            // Add new values
                            lrs = await _localizationService.GetResourceAsync("Service Point Id");
                            newCustomValues.Add(lrs, model.ServicePointId);
                            lrs = await _localizationService.GetResourceAsync("Service Point PO Number");
                            newCustomValues.Add(lrs, model.ServicePointPOBox);

                            order.CustomValuesXml = _sendcloudService.SerializeCustomValues(newCustomValues);

                            await _orderService.UpdateOrderAsync(order);

                            message += " - Shipping Method: " + order.ShippingMethod;
                        }

                        var warehouseId = 0;
                        if (product.ManageInventoryMethod == ManageInventoryMethod.ManageStock &&
                            product.UseMultipleWarehouses)
                        {
                            //multiple warehouses supported
                            //warehouse is chosen by a store owner
                            foreach (var formKey in form.Keys)
                                if (formKey.Equals($"warehouse_{orderItem.Id}", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    int.TryParse(form[formKey], out warehouseId);
                                    break;
                                }
                        }
                        else
                        {
                            //Warehouse has changed 
                            warehouseId = product.WarehouseId;
                        }

                        if (qtyToAdjust > 0)
                        {
                            //validate quantity
                            if (qtyToAdjust > maxQtyToAdd)
                                qtyToAdjust = maxQtyToAdd;

                            if (updateShipmentItem.WarehouseId != warehouseId)
                            {
                                // warehouse is changed
                                await InsertShipmentAsync(orderItem, qtyToAdjust, warehouseId, model);

                                message += " - New Shipment created for Warehouse: " + warehouseId.ToString() + " Qauntity " + qtyToAdjust.ToString();
                            }
                            else
                            {
                                // update existing shipment

                                updateShipmentItem.Quantity += qtyToAdjust;
                                await _orderSalesService.UpdateShipmentItemAsync(updateShipmentItem);

                                updateShipmentItems.Add(updateShipmentItem);

                                message += " - Quantity adjusted to : " + qtyToAdjust.ToString();

                                var orderItemTotalWeightUpdate = orderItem.ItemWeight * qtyToAdjust;
                                if (orderItemTotalWeightUpdate.HasValue)
                                {
                                    if (!totalWeight.HasValue)
                                        totalWeight = 0;
                                    totalWeight += orderItemTotalWeightUpdate.Value;
                                }

                                var quantityWithReserved = await _productService.GetTotalStockQuantityAsync(product, true, warehouseId);
                                var quantityTotal = await _productService.GetTotalStockQuantityAsync(product, false, warehouseId);

                                //currently reserved in current stock
                                var quantityReserved = quantityTotal - quantityWithReserved;

                                //If the quantity of the reserve product in the warehouse does not coincide with the total quantity of goods in the basket, 
                                //it is necessary to redistribute the reserve to the warehouse
                                if (!(quantityReserved == qtyToAdjust && quantityReserved == maxQtyToAdd))
                                    await _orderSalesService.BalanceInventoryAsync(product, warehouseId, qtyToAdjust);

                            }
                        }
                        else if (qtyToAdjust < 0)
                        {

                            //validate quantity
                            if (qtyToAdjust < -updateShipmentItem.Quantity)
                                qtyToAdjust = 0;

                            // We have at least one item. let's adjust the shipment

                            updateShipmentItem.Quantity += qtyToAdjust;
                            await _orderSalesService.UpdateShipmentItemAsync(updateShipmentItem);

                            updateShipmentItems.Add(updateShipmentItem);

                            message += " - Quantity adjusted to : " + qtyToAdjust.ToString();

                            var orderItemTotalWeightUpdate = orderItem.ItemWeight * qtyToAdjust;
                            if (orderItemTotalWeightUpdate.HasValue)
                            {
                                if (!totalWeight.HasValue)
                                    totalWeight = 0;
                                totalWeight += orderItemTotalWeightUpdate.Value;
                            }

                            var quantityWithReserved = await _productService.GetTotalStockQuantityAsync(product, true, warehouseId);
                            var quantityTotal = await _productService.GetTotalStockQuantityAsync(product, false, warehouseId);

                            //currently reserved in current stock
                            var quantityReserved = quantityTotal - quantityWithReserved;

                        }
                        else if (updateShipmentItem.WarehouseId != warehouseId)
                        {
                            updateShipmentItem.WarehouseId = warehouseId;

                            await _orderSalesService.UpdateShipmentItemAsync(updateShipmentItem);

                            updateShipmentItems.Add(updateShipmentItem);

                            message += " - Warehouse set to : " + updateShipmentItem.WarehouseId.ToString();
                        }
                    }

                    //if we have at least one item in the shipment, then save it
                    if (updateShipmentItems.Any() || model.CanShip)
                    {

                        shipment.TotalWeight = totalWeight;
                        await _shipmentService.UpdateShipmentAsync(shipment);

                        //add a note
                        await _orderSalesService.AddOrderNoteAsync(order, message);

                        if (model.CanShip)
                            await _orderProcessingService.ShipAsync(shipment, true);

                        if (model.CanShip && model.CanDeliver)
                            await _orderProcessingService.DeliverAsync(shipment, true);

                        await LogEditOrderAsync(order.Id);

                        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Shipment.Updated"));

                        return RedirectToAction("OrderShipmentDetails", new { id = shipment.Id, btnId = model.BtnId, function = "Edit" });
                    }
                    else
                    {
                        await _shipmentService.UpdateShipmentAsync(shipment);

                        //add a note
                        await _orderSalesService.AddOrderNoteAsync(order, message);
                    }

                }

                return RedirectToAction("OrderShipmentDetails", new { id = shipment.Id, btnId = model.BtnId, function = "Close" });
            }
        }

        return RedirectToAction("EditShipment", model);
    }

    public async Task<bool> InsertShipmentAsync(OrderItem orderItem, int quantity, int warehouseId, OrderShipmentModel model)
    {

        PackagingOption packagingOption = null;

        var orderItems = _orderSalesService.GetOrderItems(model.OrderId, isShipEnabled: true);
        //a vendor should have access only to his products
        if (await _workContext.GetCurrentVendorAsync() != null)
        {
            orderItems = await orderItems.WhereAwait(HasAccessToProductAsync).ToListAsync();
        }

        var shipment = new Shipment
        {
            OrderId = model.OrderId,
            TrackingNumber = model.TrackingNumber,
            TotalWeight = null,
            AdminComment = model.AdminComment,
            CreatedOnUtc = DateTime.UtcNow
        };

        var shipmentItems = new List<ShipmentItem>();

        decimal? totalWeight = null;

        var product = await _productService.GetProductByIdAsync(orderItem.ProductId);

        //ensure that this product can be shipped (have at least one item to ship)
        var maxQtyToAdd = await _orderService.GetTotalNumberOfItemsCanBeAddedToShipmentAsync(orderItem);
        if (quantity > 0)
        { 
            var qtyToAdd = quantity;

            if (qtyToAdd > maxQtyToAdd)
                qtyToAdd = maxQtyToAdd;

            //ok. we have at least one item. let's create a shipment (if it does not exist)

            var orderItemTotalWeight = orderItem.ItemWeight * qtyToAdd;
            if (orderItemTotalWeight.HasValue)
            {
                if (!totalWeight.HasValue)
                    totalWeight = 0;
                totalWeight += orderItemTotalWeight.Value;
            }

            //create a shipment item
            shipmentItems.Add(new ShipmentItem
            {
                OrderItemId = orderItem.Id,
                Quantity = qtyToAdd,
                WarehouseId = warehouseId
            });

            var quantityWithReserved = await _productService.GetTotalStockQuantityAsync(product, true);
            var quantityTotal = await _productService.GetTotalStockQuantityAsync(product, false);

            //currently reserved in current stock
            var quantityReserved = quantityTotal - quantityWithReserved;
        }

        //if we have at least one item in the shipment, then save it
        if (shipmentItems.Any())
        {
            packagingOption = await _shippingManagerService.GetDefaultPackagingOption();
            if (packagingOption != null)
                totalWeight += packagingOption.Weight;

            shipment.TotalWeight = totalWeight;

            await _shipmentService.InsertShipmentAsync(shipment);

            if (packagingOption != null)
                await _shippingManagerService.InsertShipmentDetails(shipment, packagingOption);

            foreach (var shipmentItem in shipmentItems)
            {
                shipmentItem.ShipmentId = shipment.Id;
                await _shipmentService.InsertShipmentItemAsync(shipmentItem);
            }

            //add a note
            await _orderSalesService.AddOrderNoteForShipmentAsync(shipment, "An automatic shipment has been added");

            if (model.CanShip)
                await _orderProcessingService.ShipAsync(shipment, true);

            if (model.CanShip && model.CanDeliver)
                await _orderProcessingService.DeliverAsync(shipment, true);

            await LogEditOrderAsync(model.OrderId);

            return true;

        }

        return false;
    }

    public async Task<bool> InsertShipmentAsync(OrderShipmentModel model)
    {
        PackagingOption packagingOption = null;

        var orderItems = _orderSalesService.GetOrderItems(model.OrderId, isShipEnabled: true);
        //a vendor should have access only to his products
        if (await _workContext.GetCurrentVendorAsync() != null)
        {
            orderItems = await orderItems.WhereAwait(HasAccessToProductAsync).ToListAsync();
        }

        var shipment = new Shipment
        {
            OrderId = model.OrderId,
            TrackingNumber = model.TrackingNumber,
            TotalWeight = null,
            AdminComment = model.AdminComment,
            CreatedOnUtc = DateTime.UtcNow
        };

        var shipmentItems = new List<ShipmentItem>();

        decimal? totalWeight = null;

        foreach (var orderItem in orderItems)
        {
            var product = await _productService.GetProductByIdAsync(orderItem.ProductId);

            //ensure that this product can be shipped (have at least one item to ship)
            var maxQtyToAdd = await _orderService.GetTotalNumberOfItemsCanBeAddedToShipmentAsync(orderItem);
            if (maxQtyToAdd <= 0)
                continue;

            var qtyToAdd = orderItem.Quantity;

            //multiple warehouses are not supported
            var warehouseId = 0;

            //validate quantity
            if (qtyToAdd <= 0)
                continue;
            if (qtyToAdd > maxQtyToAdd)
                qtyToAdd = maxQtyToAdd;

            //ok. we have at least one item. let's create a shipment (if it does not exist)

            var orderItemTotalWeight = orderItem.ItemWeight * qtyToAdd;
            if (orderItemTotalWeight.HasValue)
            {
                if (!totalWeight.HasValue)
                    totalWeight = 0;
                totalWeight += orderItemTotalWeight.Value;
            }

            //create a shipment item
            shipmentItems.Add(new ShipmentItem
            {
                OrderItemId = orderItem.Id,
                Quantity = qtyToAdd,
                WarehouseId = warehouseId
            });

            var quantityWithReserved = await _productService.GetTotalStockQuantityAsync(product, true);
            var quantityTotal = await _productService.GetTotalStockQuantityAsync(product, false);

            //currently reserved in current stock
            var quantityReserved = quantityTotal - quantityWithReserved;
        }

            //if we have at least one item in the shipment, then save it
            if (shipmentItems.Any())
            {
                packagingOption = await _shippingManagerService.GetDefaultPackagingOption();
                if (packagingOption != null && _shippingManagerSettings.UsePackagingSystem)
                    totalWeight += packagingOption.Weight;

            shipment.TotalWeight = totalWeight;

            await _shipmentService.InsertShipmentAsync(shipment);

            if (packagingOption != null)
                await _shippingManagerService.InsertShipmentDetails(shipment, packagingOption);

            foreach (var shipmentItem in shipmentItems)
            {
                shipmentItem.ShipmentId = shipment.Id;
                await _shipmentService.InsertShipmentItemAsync(shipmentItem);
            }

            //add a note
            await _orderSalesService.AddOrderNoteForShipmentAsync(shipment, "An automatic shipment has been added");

            if (model.CanShip)
                await _orderProcessingService.ShipAsync(shipment, true);

            if (model.CanShip && model.CanDeliver)
                await _orderProcessingService.DeliverAsync(shipment, true);

            await LogEditOrderAsync(model.OrderId);

            return true;

        }

        return false;
    }

    public async Task<bool> InsertShipmentForWarehouseAsync(OrderShipmentModel model)
    {

        PackagingOption packagingOption = null;

        var orderItems = _orderSalesService.GetOrderItems(model.OrderId, isShipEnabled: true);

        //a vendor should have access only to his products
        if (await _workContext.GetCurrentVendorAsync() != null)
        {
            orderItems = await orderItems.WhereAwait(HasAccessToProductAsync).ToListAsync();
        }

        var shipment = new Shipment
        {
            OrderId = model.OrderId,
            TrackingNumber = model.TrackingNumber,
            TotalWeight = null,
            AdminComment = model.AdminComment,
            CreatedOnUtc = DateTime.UtcNow
        };

        var shipmentItems = new List<ShipmentItem>();

        decimal? totalWeight = null;

        foreach (var orderItem in orderItems)
        {
            var product = await _productService.GetProductByIdAsync(orderItem.ProductId);

            //ensure that this product can be shipped (have at least one item to ship)
            var maxQtyToAdd = await _orderService.GetTotalNumberOfItemsCanBeAddedToShipmentAsync(orderItem);
            if (maxQtyToAdd <= 0)
                continue;

            var qtyToAdd = orderItem.Quantity;

            //multiple warehouses are not supported
            var warehouseId = product.WarehouseId;

            var item = model.Items.Where(x => x.OrderItemId == orderItem.Id).FirstOrDefault();
            if (item != null && item.AvailableWarehouses.Count != 0)
                warehouseId = item.AvailableWarehouses.FirstOrDefault().WarehouseId;

            if (warehouseId != 0)
            {

                //validate quantity
                if (qtyToAdd <= 0)
                    continue;
                if (qtyToAdd > maxQtyToAdd)
                    qtyToAdd = maxQtyToAdd;

                //ok. we have at least one item. let's create a shipment (if it does not exist)

                var orderItemTotalWeight = orderItem.ItemWeight * qtyToAdd;
                if (orderItemTotalWeight.HasValue)
                {
                    if (!totalWeight.HasValue)
                        totalWeight = 0;
                    totalWeight += orderItemTotalWeight.Value;
                }

                //create a shipment item
                shipmentItems.Add(new ShipmentItem
                {
                    OrderItemId = orderItem.Id,
                    Quantity = qtyToAdd,
                    WarehouseId = warehouseId
                });

                var quantityWithReserved = await _productService.GetTotalStockQuantityAsync(product, true, warehouseId);
                var quantityTotal = await _productService.GetTotalStockQuantityAsync(product, false, warehouseId);

                //currently reserved in current stock
                var quantityReserved = quantityTotal - quantityWithReserved;

                //If the quantity of the reserve product in the warehouse does not coincide with the total quantity of goods in the basket, 
                //it is necessary to redistribute the reserve to the warehouse
                if (!(quantityReserved == qtyToAdd && quantityReserved == maxQtyToAdd))
                    await _orderSalesService.BalanceInventoryAsync(product, warehouseId, qtyToAdd);
            }
        }

        //if we have at least one item in the shipment, then save it
        if (shipmentItems.Any())
        {
            packagingOption = await _shippingManagerService.GetDefaultPackagingOption();
            if (packagingOption != null)
                totalWeight += packagingOption.Weight;

            shipment.TotalWeight = totalWeight;

            await _shipmentService.InsertShipmentAsync(shipment);

            if (packagingOption != null)
                await _shippingManagerService.InsertShipmentDetails(shipment, packagingOption);

            foreach (var shipmentItem in shipmentItems)
            {
                shipmentItem.ShipmentId = shipment.Id;
                await _shipmentService.InsertShipmentItemAsync(shipmentItem);
            }

            //add a note
            await _orderSalesService.AddOrderNoteForShipmentAsync(shipment, "An automatic shipment has been added");

            if (model.CanShip)
                await _orderProcessingService.ShipAsync(shipment, true);

            if (model.CanShip && model.CanDeliver)
                await _orderProcessingService.DeliverAsync(shipment, true);

            await LogEditOrderAsync(model.OrderId);

            return true;

        }

        return false;
    }

    public virtual async Task<IActionResult> OrderShipmentDetails(int id, string btnId, string function)
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageOrders))
            return AccessDeniedView();

        //try to get a shipment with the specified id
        var shipment = await _shipmentService.GetShipmentByIdAsync(id);
        if (shipment == null)
            return RedirectToAction("List");

        //a vendor should have access only to his products
        if (await _workContext.GetCurrentVendorAsync() != null && !await HasAccessToShipmentAsync(shipment))
            return RedirectToAction("List");

        //prepare model
        var model = await PrepareOrderShipmentModelAsync(null, shipment, null);

        ViewBag.RefreshPage = true;
        ViewBag.BtnId = btnId;

        if (function == "Add")
            return View("~/Plugins/SSI.Shipping.Manager/Views/Order/AddShipmentPopup.cshtml", model);
        else
            return View("~/Plugins/SSI.Shipping.Manager/Views/Order/EditShipmentPopup.cshtml", model);
    }

    /// <summary>
    /// Prepare shipment model
    /// </summary>
    /// <param name="model">Shipment model</param>
    /// <param name="shipment">Shipment</param>
    /// <param name="order">Order</param>
    /// <param name="excludeProperties">Whether to exclude populating of some properties of model</param>
    /// <returns>Shipment model</returns>
    public virtual async Task<OrderShipmentModel> PrepareOrderShipmentModelAsync(OrderShipmentModel model, Shipment shipment, Order order, bool excludeProperties = false)
    {
        if (shipment != null)
        {
            //fill in model values from the entity

            model = new OrderShipmentModel();

            model.Id = shipment.Id;
            model.OrderId = shipment.OrderId;
            model.TotalWeight = shipment.TotalWeight.ToString();
            model.TrackingNumber = shipment.TrackingNumber;
            model.ShippedDate = shipment.ShippedDateUtc.ToString();
            model.ShippedDateUtc = shipment.ShippedDateUtc;
            model.DeliveryDate = shipment.DeliveryDateUtc.ToString();
            model.DeliveryDateUtc = shipment.DeliveryDateUtc;
            model.AdminComment = shipment.AdminComment;

            model.CanShip = !shipment.ShippedDateUtc.HasValue;
            model.CanDeliver = shipment.ShippedDateUtc.HasValue && !shipment.DeliveryDateUtc.HasValue;

            var shipmentOrder = await _orderService.GetOrderByIdAsync(shipment.OrderId);

            // Get existing values
            if (shipmentOrder.ShippingRateComputationMethodSystemName.Equals(ShippingManagerDefaults.SendCloudSystemName))
            {
                var customValues = _sendcloudService.DeserializeCustomValues(shipmentOrder);
                foreach (var key in customValues)
                {
                    if (key.Key == await _localizationService.GetResourceAsync("Service Point Id"))
                        model.ServicePointId = key.Value.ToString();
                    else if (key.Key == await _localizationService.GetResourceAsync("Service Point PO Number"))
                        model.ServicePointPOBox = key.Value.ToString();
                }
            }

            model.CustomOrderNumber = shipmentOrder.CustomOrderNumber;

            model.ShippedDate = shipment.ShippedDateUtc.HasValue
                ? _dateTimeHelper.ConvertToUserTimeAsync(shipment.ShippedDateUtc.Value, DateTimeKind.Utc).ToString()
                : await _localizationService.GetResourceAsync("Admin.Orders.Shipments.ShippedDate.NotYet");
            model.DeliveryDate = shipment.DeliveryDateUtc.HasValue
                ? _dateTimeHelper.ConvertToUserTimeAsync(shipment.DeliveryDateUtc.Value, DateTimeKind.Utc).ToString()
                : await _localizationService.GetResourceAsync("Admin.Orders.Shipments.DeliveryDate.NotYet");

            model.TotalWeight = string.Empty;

            //prepare shipment items
            foreach (var item in _orderSalesService.GetShipmentItemsByShipmentId(shipment.Id))
            {
                var orderItem = await _orderService.GetOrderItemByIdAsync(item.OrderItemId);
                if (orderItem == null)
                    continue;

                var product = await _productService.GetProductByIdAsync(orderItem.ProductId);

                //fill in model values from the entity
                var shipmentItemModel = new ShipmentItemModel
                {
                    Id = item.Id,
                    QuantityInThisShipment = item.Quantity,
                    ShippedFromWarehouse = (await _shippingService.GetWarehouseByIdAsync(item.WarehouseId))?.Name
                };

                await _orderOperationsModelFactory.PrepareShipmentItemModelAsync(shipmentItemModel, orderItem);

                model.Items.Add(shipmentItemModel);
            }

            //prepare shipment events
            if (!string.IsNullOrEmpty(shipment.TrackingNumber))
            {
                var shipmentTracker = await _shipmentService.GetShipmentTrackerAsync(shipment);
                if (shipmentTracker != null)
                {
                    model.TrackingNumberUrl = await shipmentTracker.GetUrlAsync(shipment.TrackingNumber, shipment);
                    if (_shippingSettings.DisplayShipmentEventsToStoreOwner)
                        await _orderOperationsModelFactory.PrepareShipmentStatusEventModelsAsync(model.ShipmentStatusEvents, shipment);
                }
            }
        }

        if (shipment != null)
            return model;

        model.OrderId = order.Id;
        model.CustomOrderNumber = order.CustomOrderNumber;

        var shippingAddressCountry = new Country();
        if (order.ShippingAddressId.HasValue)
        {
            var shippingAddress = await _addressService.GetAddressByIdAsync(order.ShippingAddressId.Value);
            if (shippingAddress != null)
            {
                if (shippingAddress.CountryId.HasValue)
                {
                    shippingAddressCountry = await _countryService.GetCountryByIdAsync(shippingAddress.CountryId.Value);
                }
            }
        }

        // Find shipping method
        ShippingMethod shippingMethod = null;
        var smbwbt = new ShippingManagerByWeightByTotal();

        (shippingMethod, smbwbt) = await _shippingManagerService.GetShippingMethodFromFriendlyNameAsync(order.ShippingMethod);
        if (smbwbt != null)
            model.ShippingMethodId = smbwbt.ShippingMethodId;

        if (shippingMethod == null)
        {
            shippingMethod = await _shippingManagerService.GetShippingMethodByNameAsync(order);
            model.ShippingMethodId = shippingMethod.Id;
        }

        int index = 0;
        if (shippingMethod != null)
        {
            index = 0;
            (model.AvailableShippingMethods, index) = await _shippingManagerService.PrepareShippingMethodsForShipmentAsync(order.ShippingMethod,
                shippingMethod.Name, shippingAddressCountry.Id);

            if (index != 0)
                model.ShippingMethodId = index;
        }

        var orderItems = _orderSalesService.GetOrderItems(order.Id, isShipEnabled: true, vendorId: (await _workContext.GetCurrentVendorAsync())?.Id ?? 0).ToList();

        foreach (var orderItem in orderItems)
        {
            var shipmentItemModel = new ShipmentItemModel();

            var product = await _productService.GetProductByIdAsync(orderItem.ProductId);

            await _orderOperationsModelFactory.PrepareShipmentItemModelAsync(shipmentItemModel, orderItem);

            //ensure that this product can be added to a shipment
            if (shipmentItemModel.QuantityToAdd <= 0)
                continue;

            if (product.ManageInventoryMethod == ManageInventoryMethod.ManageStock && product.UseMultipleWarehouses)
            {
                //multiple warehouses supported
                shipmentItemModel.AllowToChooseWarehouse = true;
                foreach (var pwi in (await _productService.GetAllProductWarehouseInventoryRecordsAsync(orderItem.ProductId)).OrderBy(w => w.WarehouseId).ToList())
                {
                    if (await _shippingService.GetWarehouseByIdAsync(pwi.WarehouseId) is Warehouse warehouse)
                    {
                        shipmentItemModel.AvailableWarehouses.Add(new ShipmentItemModel.WarehouseInfo
                        {
                            WarehouseId = warehouse.Id,
                            WarehouseName = warehouse.Name,
                            StockQuantity = pwi.StockQuantity,
                            ReservedQuantity = pwi.ReservedQuantity,
                            PlannedQuantity = await _shipmentService.GetQuantityInShipmentsAsync(product, warehouse.Id, true, true)
                        });
                    }
                }
            }
            else
            {
                //multiple warehouses are not supported
                var warehouse = await _shippingService.GetWarehouseByIdAsync(product.WarehouseId);
                if (warehouse != null)
                {
                    shipmentItemModel.AvailableWarehouses.Add(new ShipmentItemModel.WarehouseInfo
                    {
                        WarehouseId = warehouse.Id,
                        WarehouseName = warehouse.Name,
                        StockQuantity = product.StockQuantity
                    });
                }
            }

            if (shipmentItemModel.AvailableWarehouses.Count == 0)
                shipmentItemModel.QuantityToAdd = 0;

            model.Items.Add(shipmentItemModel);
        }

        return model;
    }

    /// <summary>
    /// Prepare shipment model
    /// </summary>
    /// <param name="model">Shipment model</param>
    /// <param name="shipment">Shipment</param>
    /// <param name="order">Order</param>
    /// <param name="excludeProperties">Whether to exclude populating of some properties of model</param>
    /// <returns>Shipment model</returns>
    public virtual async Task<OrderShipmentModel> PrepareOrderShipmentEditModelAsync(OrderShipmentModel model, Shipment shipment, ShipmentItem shipmentItem,
        Order order, bool excludeProperties = false)
    {
        if (shipment != null)
        {
            //fill in model values from the entity

            model = new OrderShipmentModel();

            model.Id = shipment.Id;
            model.OrderId = shipment.OrderId;
            model.TotalWeight = shipment.TotalWeight.ToString();
            model.TrackingNumber = shipment.TrackingNumber;
            model.ShippedDate = shipment.ShippedDateUtc.ToString();
            model.ShippedDateUtc = shipment.ShippedDateUtc;
            model.DeliveryDate = shipment.DeliveryDateUtc.ToString();
            model.DeliveryDateUtc = shipment.DeliveryDateUtc;
            model.AdminComment = shipment.AdminComment;

            model.CanShip = false; // !shipment.ShippedDateUtc.HasValue; #testing
            model.CanDeliver = shipment.ShippedDateUtc.HasValue && !shipment.DeliveryDateUtc.HasValue;

            var shipmentOrder = await _orderService.GetOrderByIdAsync(shipment.OrderId);

            // Get existing values
            var customValues = _sendcloudService.DeserializeCustomValues(order);
            foreach (var key in customValues)
            {
                if (key.Key == await _localizationService.GetResourceAsync("Service Point Id"))
                    model.ServicePointId = key.Value.ToString();
                else if (key.Key == await _localizationService.GetResourceAsync("Service Point PO Number"))
                    model.ServicePointPOBox = key.Value.ToString();
            }

            model.CustomOrderNumber = shipmentOrder.CustomOrderNumber;

            model.ShippedDate = shipment.ShippedDateUtc.HasValue
                ? _dateTimeHelper.ConvertToUserTimeAsync(shipment.ShippedDateUtc.Value, DateTimeKind.Utc).ToString()
                : await _localizationService.GetResourceAsync("Admin.Orders.Shipments.ShippedDate.NotYet");
            model.DeliveryDate = shipment.DeliveryDateUtc.HasValue
                ? _dateTimeHelper.ConvertToUserTimeAsync(shipment.DeliveryDateUtc.Value, DateTimeKind.Utc).ToString()
                : await _localizationService.GetResourceAsync("Admin.Orders.Shipments.DeliveryDate.NotYet");

            model.TotalWeight = string.Empty;

            //prepare shipment items
            foreach (var item in _orderSalesService.GetShipmentItemsByShipmentId(shipment.Id))
            {
                var orderItem = await _orderService.GetOrderItemByIdAsync(item.OrderItemId);
                if (orderItem == null)
                    continue;

                var product = await _productService.GetProductByIdAsync(orderItem.ProductId);

                //fill in model values from the entity
                var shipmentItemModel = new ShipmentItemModel
                {
                    Id = item.Id,
                    QuantityInThisShipment = item.Quantity,
                    ShippedFromWarehouse = (await _shippingService.GetWarehouseByIdAsync(item.WarehouseId))?.Name
                };

                await _orderOperationsModelFactory.PrepareShipmentItemModelAsync(shipmentItemModel, orderItem);

                if (product.ManageInventoryMethod == ManageInventoryMethod.ManageStock && product.UseMultipleWarehouses)
                {
                    //multiple warehouses supported
                    shipmentItemModel.AllowToChooseWarehouse = true;
                    foreach (var pwi in (await _productService.GetAllProductWarehouseInventoryRecordsAsync(orderItem.ProductId)).OrderBy(w => w.WarehouseId).ToList())
                    {
                        if (await _shippingService.GetWarehouseByIdAsync(pwi.WarehouseId) is Warehouse warehouse)
                        {
                                bool selected = false;
                                if (item.WarehouseId == warehouse.Id)
                                    selected = true;

                                int quantityNotYetShipped = orderItem.Quantity - await _orderService.GetTotalNumberOfItemsInAllShipmentsAsync(orderItem);

                                shipmentItemModel.AvailableWarehouses.Add(new ShipmentItemModel.WarehouseInfo
                                {
                                    WarehouseId = warehouse.Id,
                                    WarehouseName = warehouse.Name,
                                    StockQuantity = pwi.StockQuantity,
                                    ReservedQuantity = pwi.ReservedQuantity,
                                    PlannedQuantity = await _orderSalesService.GetQuantityInShipmentsAsync(product, warehouse.Id, true, true),
                                    ToShipQuatity = quantityNotYetShipped,
                                    IsPreSelected = selected
                                });
                        }
                    }
                }
                else
                {
                    //multiple warehouses are not supported
                    var warehouse = await _shippingService.GetWarehouseByIdAsync(product.WarehouseId);
                    if (warehouse != null)
                    {
                        shipmentItemModel.AvailableWarehouses.Add(new ShipmentItemModel.WarehouseInfo
                        {
                            WarehouseId = warehouse.Id,
                            WarehouseName = warehouse.Name,
                            StockQuantity = product.StockQuantity
                        });
                    }
                }

                if (shipmentItemModel.AvailableWarehouses.Count == 0)
                    shipmentItemModel.QuantityToAdd = 0;

                model.Items.Add(shipmentItemModel);
            }

            //prepare shipment events
            if (!string.IsNullOrEmpty(shipment.TrackingNumber))
            {
                var shipmentTracker = await _shipmentService.GetShipmentTrackerAsync(shipment);
                if (shipmentTracker != null)
                {
                    model.TrackingNumberUrl = await shipmentTracker.GetUrlAsync(shipment.TrackingNumber, shipment);
                    if (_shippingSettings.DisplayShipmentEventsToStoreOwner)
                        await _orderOperationsModelFactory.PrepareShipmentStatusEventModelsAsync(model.ShipmentStatusEvents, shipment);
                }
            }
        }

        model.OrderId = order.Id;
        model.CustomOrderNumber = order.CustomOrderNumber;

        //shipping methods
        var shippingMethod = await _shippingManagerService.GetShippingMethodByNameAsync(order);
        if (shippingMethod != null)
            model.ShippingMethodId = shippingMethod.Id;
        else
        {
            (shippingMethod, _) = await _shippingManagerService.GetShippingMethodFromFriendlyNameAsync(order.ShippingMethod);
            if (shippingMethod != null)
                model.ShippingMethodId = shippingMethod.Id;
        }

        int index = 0;
        (model.AvailableShippingMethods, index) = await _shippingManagerService.PrepareAvailableShippingMethodsModelAsync(false, model.ShippingMethodId, order.ShippingMethod);

        if (index != 0)
            model.ShippingMethodId = index;

        var orderItems = _orderSalesService.GetOrderItems(order.Id, isShipEnabled: true, vendorId: (await _workContext.GetCurrentVendorAsync())?.Id ?? 0).ToList();

        bool found = false;
        foreach (var orderItem in orderItems)
        {
            foreach(var item in model.Items)
            {
                if (item.OrderItemId == orderItem.Id)
                    found = true;
            }

            if (!found)
            {
                var shipmentItemModel = new ShipmentItemModel();

                var product = await _productService.GetProductByIdAsync(orderItem.ProductId);

                await _orderOperationsModelFactory.PrepareShipmentItemModelAsync(shipmentItemModel, orderItem);

                //ensure that this product has been shipped
                if (shipmentItemModel.QuantityToAdd != 0)
                    continue;

                if (product.ManageInventoryMethod == ManageInventoryMethod.ManageStock &&
                    product.UseMultipleWarehouses)
                {
                    //multiple warehouses supported
                    shipmentItemModel.AllowToChooseWarehouse = true;
                    foreach (var pwi in (await _productService.GetAllProductWarehouseInventoryRecordsAsync(orderItem.ProductId)).OrderBy(w => w.WarehouseId).ToList())
                    {
                        if (await _shippingService.GetWarehouseByIdAsync(pwi.WarehouseId) is Warehouse warehouse)
                        {
                            shipmentItemModel.AvailableWarehouses.Add(new ShipmentItemModel.WarehouseInfo
                            {
                                WarehouseId = warehouse.Id,
                                WarehouseName = warehouse.Name,
                                StockQuantity = pwi.StockQuantity,
                                ReservedQuantity = pwi.ReservedQuantity,
                                PlannedQuantity =
                                    await _orderSalesService.GetQuantityInShipmentsAsync(product, warehouse.Id, true, true)
                            });
                        }
                    }
                }
                else
                {
                    //multiple warehouses are not supported
                    var warehouse = await _shippingService.GetWarehouseByIdAsync(product.WarehouseId);
                    if (warehouse != null)
                    {
                        shipmentItemModel.AvailableWarehouses.Add(new ShipmentItemModel.WarehouseInfo
                        {
                            WarehouseId = warehouse.Id,
                            WarehouseName = warehouse.Name,
                            StockQuantity = product.StockQuantity
                        });
                    }
                }

                if (shipmentItemModel.AvailableWarehouses.Count == 0)
                    shipmentItemModel.QuantityToAdd = 0;

                model.Items.Add(shipmentItemModel);
            }
        }

        return model;
    }

    #endregion

    #region Mark Order As 

    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    [HttpPost]
    public virtual async Task<IActionResult> MarkOrderAsPaid(string selectedIds)
    {

        if (selectedIds != null)
        {
            var ids = selectedIds
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => Convert.ToInt32(x))
                .ToArray();

            for (int i = 0; i < ids.Count(); i++)
            {
                if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageOrders))
                    return AccessDeniedView();

                //try to get an order with the specified id
                var order = await _orderService.GetOrderByIdAsync(ids[i]);

                try
                {
                    if (order.PaymentStatus != Core.Domain.Payments.PaymentStatus.Paid)
                    {
                        await _orderProcessingService.MarkOrderAsPaidAsync(order);
                        await LogEditOrderAsync(order.Id);
                    }
                }
                catch (Exception exc)
                {
                    //prepare model
                    var model = await _orderModelFactory.PrepareOrderModelAsync(null, order);
                    await _notificationService.ErrorNotificationAsync(exc, false);
                    return RedirectToAction("OrderSales");
                }
            }

        }

        return RedirectToAction("OrderSales");
    }

    public bool CanMarkOrderAsApproved(Order order)
    {
        if (order == null)
            throw new ArgumentNullException(nameof(order));

        if (order.OrderStatus == OrderStatus.Cancelled)
            return false;

        if (!HasOrderShipment(order))
            return false;

        if (order.OrderStatus == OrderStatus.Pending)
            return true;

        return false;
    }

    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    [HttpPost]
    public virtual async Task<IActionResult> MarkOrderAsApproved(string selectedIds)
    {

        if (selectedIds != null)
        {
            var ids = selectedIds
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => Convert.ToInt32(x))
                .ToArray();

            for (int i = 0; i < ids.Count(); i++)
            {
                if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageOrders))
                    return AccessDeniedView();

                //try to get an order with the specified id
                var ordrItemId = await _orderService.GetOrderItemByIdAsync(ids[i]);
                var order = await _orderService.GetOrderByIdAsync(ordrItemId.OrderId);

                try
                {
                    if (_orderSalesService.CanMarkOrderAsApproved(order))
                    {
                        await _orderSalesService.MarkOrderAsApprovedAsync(order);
                        await LogEditOrderAsync(order.Id);
                    }
                }
                catch (Exception exc)
                {
                    //prepare model
                    var model = await _orderModelFactory.PrepareOrderModelAsync(null, order);
                    await _notificationService.ErrorNotificationAsync(exc, false);
                    return RedirectToAction("OrderSales");
                }
            }

        }

        return RedirectToAction("OrderSales");
    }

    #endregion

    #region Sales Export 

    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    [HttpPost, ActionName("OrderSales")]
    [FormValueRequired("exportexcel-all-sales")]
    public virtual async Task<IActionResult> ExportExcelAll(OrderSalesSearchModel searchModel)
    {

        var orderItems = new List<OrderItem>();

        var orderItemIds = await _orderSalesService.GetAllOrderItemSalesItemsAsync();

        var orderItemsFound = await _orderSalesService.GetSalesOrderItemsListAsync(orderItemIds, searchModel.PaymentMethod,
            searchModel.FromDate, searchModel.ToDate, searchModel.Ispay, searchModel.OrderbyName, searchModel.SearchProductName);

        orderItems.AddRange(orderItemsFound);

        //a vendor should have access only to his products
        if (await _workContext.GetCurrentVendorAsync() != null)
        {
            orderItems = await orderItems.WhereAwait(HasAccessToProductAsync).ToListAsync();
        }

        //ensure that we at least one order found
        if (!orderItems.Any())
        {
            _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Admin.PdfReport.NoOrders"));
            return RedirectToAction("OrderSales");
        }

        try
        {
            var bytes = await _exportImportManager.ExportOrderItemToXlsx(orderItems);
            return File(bytes, MimeTypes.TextXlsx, "OrderSales.xlsx");
        }
        catch (Exception exc)
        {
            //ErrorNotification(exc);
            await _notificationService.ErrorNotificationAsync(exc);
            return RedirectToAction("OrderSales");
        }
    }

    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    [HttpPost]
    public virtual async Task<IActionResult> ExportSalesExcelSelected(string selectedIds)
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageOrders))
            return AccessDeniedView();

        var orderItems = new List<OrderItem>();
        if (selectedIds != null)
        {
            var ids = selectedIds
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => Convert.ToInt32(x))
                .ToArray();
            orderItems.AddRange(_orderSalesService.GetOrderSalesItemsbyOrderItemIds(ids));
        }
        try
        {
            var bytes = await _exportImportManager.ExportOrderItemToXlsx(orderItems);
            return File(bytes, MimeTypes.TextXlsx, "OrderSales.xlsx");
        }
        catch (Exception exc)
        {
            //ErrorNotification(exc);
            await _notificationService.ErrorNotificationAsync(exc);
            return RedirectToAction("OrderSales");
        }
    }

        #endregion

    #region PDF Reports

    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    [HttpPost, ActionName("OrderSales")]
    [FormValueRequired("pdf-report-all-sales")]
    public virtual async Task<IActionResult> PdfReportAll(OrderSalesSearchModel searchModel)
    {
        var orderItems = new List<OrderItem>();

        var orderItemIds = await _orderSalesService.GetAllOrderItemSalesItemsAsync();

        var orderItemsFound = await _orderSalesService.GetSalesOrderItemsListAsync(orderItemIds, searchModel.PaymentMethod,
            searchModel.FromDate, searchModel.ToDate, searchModel.Ispay, searchModel.OrderbyName, searchModel.SearchProductName);

        orderItems.AddRange(orderItemsFound);

        int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

        //ensure that we at least one order selected
        if (!orderItems.Any())
        {
            _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Admin.PdfReport.NoOrders"));
            return RedirectToAction("OrderSales");
        }

        try
        {
            byte[] bytes;
            using (var stream = new MemoryStream())
            {
                await _orderItemPdfService.PrintOrdersToPdfAsync(stream, orderItems,
                    _orderSettings.GeneratePdfInvoiceInCustomerLanguage ? 0 : (await _workContext.GetWorkingLanguageAsync()).Id, vendorId);
                bytes = stream.ToArray();
            }

            string reportName = "order_report_" + DateTime.Now.ToShortDateString() + "_" + CommonHelper.GenerateRandomDigitCode(4) + ".pdf";
            return File(bytes, MimeTypes.ApplicationPdf, reportName);

        }
        catch (Exception exc)
        {
            //ErrorNotification(exc);
            await _notificationService.ErrorNotificationAsync(exc);
            return RedirectToAction("OrderSales");
        }

    }

    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    [HttpPost]
    public virtual async Task<IActionResult> PdfSalesReportSelected(string selectedIds)
    {

        //a vendor should have access only to his products
        if (await _workContext.GetCurrentVendorAsync() != null)
            return RedirectToAction("List");

        var orderItems = new List<OrderItem>();
        if (selectedIds != null)
        {
            var ids = selectedIds
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => Convert.ToInt32(x))
                .ToArray();
            orderItems.AddRange(_orderSalesService.GetOrderSalesItemsbyOrderItemIds(ids));
        }

        int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

        //ensure that we at least one order selected
        if (!orderItems.Any())
        {
            _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Admin.PdfReport.NoOrders"));
            return RedirectToAction("OrderSales");
        }

        byte[] bytes;
        using (var stream = new MemoryStream())
        {
            await _orderItemPdfService.PrintOrdersToPdfAsync(stream, orderItems, 
                _orderSettings.GeneratePdfInvoiceInCustomerLanguage ? 0 : (await _workContext.GetWorkingLanguageAsync()).Id, vendorId);
            bytes = stream.ToArray();
        }

        string reportName = "orderreport_" + DateTime.Now.ToShortDateString() + "_" + CommonHelper.GenerateRandomDigitCode(4) + ".pdf";
        return File(bytes, MimeTypes.ApplicationPdf, reportName);
    }

    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    [HttpPost, ActionName("OrderSales")]
    [FormValueRequired("pdf-report-all-invoices")]
    public virtual async Task<IActionResult> PdfInvoiceAll(OrderSalesSearchModel searchModel)
    {
        var orders = new List<Order>();
        var orderItems = new List<OrderItem>();

        int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

        var orderItemIds = await _orderSalesService.GetAllOrderItemSalesItemsAsync();

        var orderItemsFound = await _orderSalesService.GetSalesOrderItemsListAsync(orderItemIds, searchModel.PaymentMethod,
            searchModel.FromDate, searchModel.ToDate, searchModel.Ispay, searchModel.OrderbyName, searchModel.SearchProductName);

        foreach (var orderItem in orderItemsFound)
        {
            var order = await _orderService.GetOrderByIdAsync(orderItem.OrderId);
            if (order != null)
            {
                if (!orders.Contains(order))
                    orders.Add(order);
            }
        }

        //ensure that we at least one order selected
        if (!orders.Any())
        {
            _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Admin.PdfReport.NoOrders"));
            return RedirectToAction("OrderSales");
        }

        try
        {
            byte[] bytes;
            await using (var stream = new MemoryStream())
            {
                await _orderItemPdfService.PrintInvoicesToPdfAsync(stream, orders,
                    _orderSettings.GeneratePdfInvoiceInCustomerLanguage ? 0 : (await _workContext.GetWorkingLanguageAsync()).Id, vendorId);
                bytes = stream.ToArray();
            }

            string reportName = "order_invoices_" + DateTime.Now.ToShortDateString() + "_" + CommonHelper.GenerateRandomDigitCode(4) + ".pdf";
            return File(bytes, MimeTypes.ApplicationPdf, reportName);
        }
        catch (Exception exc)
        {
            await _notificationService.ErrorNotificationAsync(exc);
            return RedirectToAction("OrderSales");
        }
    }

    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    [HttpPost]
    public virtual async Task<IActionResult> PdfInvoiceSelected(string selectedIds)
    {

        var orders = new List<Order>();
        if (selectedIds != null)
        {
            var ids = selectedIds
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => Convert.ToInt32(x))
                .ToArray();
            orders.AddRange(await _orderService.GetOrdersByIdsAsync(ids));
        }

        //a vendor should have access only to his products
        var currentVendor = await _workContext.GetCurrentVendorAsync();
        var vendorId = 0;
        if (currentVendor != null)
        {
            orders = await orders.WhereAwait(HasAccessToInvoiceAsync).ToListAsync();
            vendorId = currentVendor.Id;
        }

        try
        {
            byte[] bytes;
            await using (var stream = new MemoryStream())
            {
                await _orderItemPdfService.PrintInvoicesToPdfAsync(stream, orders,
                    _orderSettings.GeneratePdfInvoiceInCustomerLanguage ? 0 : (await _workContext.GetWorkingLanguageAsync()).Id, vendorId);
                bytes = stream.ToArray();
            }

            var reportName = "order_invoices.pdf";
            return File(bytes, MimeTypes.ApplicationPdf, reportName);
        }
        catch (Exception exc)
        {
            await _notificationService.ErrorNotificationAsync(exc);
            return RedirectToAction("OrderSales");
        }
    }

#endregion

}
