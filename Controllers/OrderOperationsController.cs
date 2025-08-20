using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Logging;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Plugin.Apollo.Integrator;
using Nop.Plugin.Apollo.Integrator.Services;
using Nop.Plugin.Shipping.Manager.Domain;
using Nop.Plugin.Shipping.Manager.Models.Order;
using Nop.Plugin.Shipping.Manager.Services;
using Nop.Plugin.Shipping.Manager.Settings;
using Nop.Services.Attributes;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Directory;
using Nop.Services.ExportImport;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Services.Shipping;
using Nop.Web.Areas.Admin.Controllers;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

using Nop.Plugin.Shipping.Manager.Factories;

namespace Nop.Plugin.Shipping.Manager.Controllers;

public partial class OrderOperationsController : BaseAdminController
{

    #region Fields

    protected readonly IAttributeParser<AddressAttribute, AddressAttributeValue> _addressAttributeParser;
    protected readonly IAddressService _addressService;
    protected readonly ICountryService _countryService;
    protected readonly ICustomerActivityService _customerActivityService;
    protected readonly IDateTimeHelper _dateTimeHelper;
    protected readonly IDownloadService _downloadService;
    protected readonly IEncryptionService _encryptionService;
    protected readonly IExportManager _exportManager;
    protected readonly IGiftCardService _giftCardService;
    protected readonly ILocalizationService _localizationService;
    protected readonly INotificationService _notificationService;
    protected readonly IOrderOperationsModelFactory _orderOperationsModelFactory;
    protected readonly IOrderProcessingService _orderProcessingService;
    protected readonly IOrderService _orderService;
    protected readonly IPaymentService _paymentService;
    protected readonly IPdfService _pdfService;
    protected readonly IPermissionService _permissionService;
    protected readonly IPriceCalculationService _priceCalculationService;
    protected readonly IProductAttributeFormatter _productAttributeFormatter;
    protected readonly IProductAttributeParser _productAttributeParser;
    protected readonly IProductAttributeService _productAttributeService;
    protected readonly IProductService _productService;
    protected readonly IShipmentService _shipmentService;
    protected readonly IShippingService _shippingService;
    protected readonly IShoppingCartService _shoppingCartService;
    protected readonly IWorkContext _workContext;
    protected readonly IWorkflowMessageService _workflowMessageService;
    protected readonly OrderSettings _orderSettings;
    protected readonly IShippingManagerService _shippingManagerService;
    protected readonly IEntityGroupService _entityGroupService;
    protected readonly ShippingManagerSettings _shippingManagerSettings;
    protected readonly ILogger _logger;
    protected readonly ISendcloudService _sendcloudService;
    protected readonly ICanadaPostService _canadaPostService;
    protected readonly IOrderItemPdfService _orderItemPdfService;
    protected readonly IFastwayService _fastwayService;
    protected readonly IPackagingOptionService _packagingOptionService;
    protected readonly IShipmentDetailsService _shipmentDetailsService;
    protected readonly ICarrierService _carrierService;

    #endregion

    #region Ctor

    public OrderOperationsController(IAttributeParser<AddressAttribute, AddressAttributeValue> addressAttributeParser,
        IAddressService addressService,
        ICountryService countryService,
        ICustomerActivityService customerActivityService,
        IDateTimeHelper dateTimeHelper,
        IDownloadService downloadService,
        IEncryptionService encryptionService,
        IExportManager exportManager,
        IGiftCardService giftCardService,
        ILocalizationService localizationService,
        INotificationService notificationService,
        IOrderOperationsModelFactory orderOperationsModelFactory,
        IOrderProcessingService orderProcessingService,
        IOrderService orderService,
        IPaymentService paymentService,
        IPdfService pdfService,
        IPermissionService permissionService,
        IPriceCalculationService priceCalculationService,
        IProductAttributeFormatter productAttributeFormatter,
        IProductAttributeParser productAttributeParser,
        IProductAttributeService productAttributeService,
        IProductService productService,
        IShipmentService shipmentService,
        IShippingService shippingService,
        IShoppingCartService shoppingCartService,
        IWorkContext workContext,
        IWorkflowMessageService workflowMessageService,
        OrderSettings orderSettings,
        IShippingManagerService shippingManagerService,
        IEntityGroupService entityGroupService,
        ShippingManagerSettings shippingManagerSettings,
        ILogger logger,
        ISendcloudService sendcloudService,
        ICanadaPostService canadaPostService,
        IOrderItemPdfService orderItemPdfService,
        IFastwayService fastwayService,
        IPackagingOptionService packagingOptionService,
        IShipmentDetailsService shipmentDetailsService,
        ICarrierService carrierService)
    {
        _addressAttributeParser = addressAttributeParser;
        _addressService = addressService;
        _countryService = countryService;
        _customerActivityService = customerActivityService;
        _dateTimeHelper = dateTimeHelper;
        _downloadService = downloadService;
        _encryptionService = encryptionService;
        _exportManager = exportManager;
        _giftCardService = giftCardService;
        _localizationService = localizationService;
        _notificationService = notificationService;
        _orderOperationsModelFactory = orderOperationsModelFactory;
        _orderProcessingService = orderProcessingService;
        _orderService = orderService;
        _paymentService = paymentService;
        _pdfService = pdfService;
        _permissionService = permissionService;
        _priceCalculationService = priceCalculationService;
        _productAttributeFormatter = productAttributeFormatter;
        _productAttributeParser = productAttributeParser;
        _productAttributeService = productAttributeService;
        _productService = productService;
        _shipmentService = shipmentService;
        _shippingService = shippingService;
        _shoppingCartService = shoppingCartService;
        _workContext = workContext;
        _workflowMessageService = workflowMessageService;
        _orderSettings = orderSettings;
        _shippingManagerService = shippingManagerService;
        _entityGroupService = entityGroupService;
        _shippingManagerSettings = shippingManagerSettings;
        _logger = logger;
        _sendcloudService = sendcloudService;
        _canadaPostService = canadaPostService;
        _orderItemPdfService = orderItemPdfService;
        _fastwayService = fastwayService;
        _packagingOptionService = packagingOptionService;
        _shipmentDetailsService = shipmentDetailsService;
        _carrierService = carrierService;
    }

    #endregion

    #region Utilities

    protected virtual async ValueTask<bool> HasAccessToOrderAsync(Order order)
    {
        return order != null && await HasAccessToOrderAsync(order.Id);
    }

    protected virtual async Task<bool> HasAccessToOrderAsync(int orderId)
    {
        if (orderId == 0)
            return false;

        if (await _workContext.GetCurrentVendorAsync() == null)
            //not a vendor; has access
            return true;

        var vendorId = (await _workContext.GetCurrentVendorAsync()).Id;
        var hasVendorProducts = (await _orderService.GetOrderItemsAsync(orderId, vendorId: vendorId)).Any();

        return hasVendorProducts;
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

    protected virtual async ValueTask<bool> HasAccessToShipmentAsync(Shipment shipment)
    {
        if (shipment == null)
            throw new ArgumentNullException(nameof(shipment));

        if (await _workContext.GetCurrentVendorAsync() == null)
            //not a vendor; has access
            return true;

        return await HasAccessToOrderAsync(shipment.OrderId);
    }

    /// <returns>A task that represents the asynchronous operation</returns>
    protected virtual async Task LogEditOrderAsync(int orderId)
    {
        var order = await _orderService.GetOrderByIdAsync(orderId);

        await _customerActivityService.InsertActivityAsync("EditOrder",
            string.Format(await _localizationService.GetResourceAsync("ActivityLog.EditOrder"), order.CustomOrderNumber), order);
    }

    #endregion

    #region Shipments

    public virtual async Task<IActionResult> ShipmentList()
    {
        if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Operate))
            return AccessDeniedView();

        //prepare model
        var model = await _orderOperationsModelFactory.PrepareShipmentSearchModelAsync(new ShipmentSearchModel());

        return View("~/Plugins/SSI.Shipping.Manager/Views/Order/ShipmentList.cshtml", model);
    }

    public virtual async Task<IActionResult> Edit(int id)
    {
        if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Operate))
            return AccessDeniedView();

        return await ShipmentList();
    }

    [HttpPost]
    public virtual async Task<IActionResult> ShipmentList(ShipmentSearchModel searchModel)
    {
        if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Operate))
            return AccessDeniedView();

        //prepare model
        var model = await _orderOperationsModelFactory.PrepareShipmentListModelAsync(searchModel);

        return Json(model);
    }

    [HttpPost]
    public virtual async Task<IActionResult> ShipmentsItemsByShipmentId(ShipmentItemSearchModel searchModel)
    {
        if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Operate))
            return AccessDeniedView();

        //try to get a shipment with the specified id
        var shipment = await _shipmentService.GetShipmentByIdAsync(searchModel.ShipmentId)
            ?? throw new ArgumentException("No shipment found with the specified id");

        //a vendor should have access only to his products
        if (await _workContext.GetCurrentVendorAsync() != null && !await HasAccessToShipmentAsync(shipment))
            return Content(string.Empty);

        //try to get an order with the specified id
        var order = await _orderService.GetOrderByIdAsync(shipment.OrderId)
            ?? throw new ArgumentException("No order found with the specified id");

        //a vendor should have access only to his products
        if (await _workContext.GetCurrentVendorAsync() != null && !await HasAccessToOrderAsync(order))
            return Content(string.Empty);

        //prepare model
        searchModel.SetGridPageSize();
        var model = await _orderOperationsModelFactory.PrepareShipmentItemListModelAsync(searchModel, shipment);

        return Json(model);
    }

    public virtual async Task<IActionResult> AddShipment(int orderId)
    {
        if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Operate))
            return AccessDeniedView();

        //try to get an order with the specified id
        var order = await _orderService.GetOrderByIdAsync(orderId);
        if (order == null)
            return RedirectToAction("List");

        //a vendor should have access only to his products
        if (await _workContext.GetCurrentVendorAsync() != null && !await HasAccessToOrderAsync(order))
            return RedirectToAction("List");

        //prepare model
        var model = await _orderOperationsModelFactory.PrepareShipmentModelAsync(new ShipmentModel(), null, order);

        return View("~/Plugins/SSI.Shipping.Manager/Views/Order/AddShipment.cshtml", model);
    }

    [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
    [FormValueRequired("save", "save-continue")]
    public virtual async Task<IActionResult> AddShipment(int orderId, IFormCollection form, bool continueEditing)
    {
        PackagingOption packagingOption = null;

        if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Operate))
            return AccessDeniedView();

        //try to get an order with the specified id
        var order = await _orderService.GetOrderByIdAsync(orderId);
        if (order == null)
            return RedirectToAction("List");

        //a vendor should have access only to his products
        if (await _workContext.GetCurrentVendorAsync() != null && !await HasAccessToOrderAsync(order))
            return RedirectToAction("List");

        //a vendor should have access only to his products
        var orderItems = await _orderService.GetOrderItemsAsync(order.Id, vendorId: (await _workContext.GetCurrentVendorAsync())?.Id ?? 0);

        if (!orderItems.Any())
            return RedirectToAction("List");

        Shipment shipment = null;
        var shipmentItems = new List<ShipmentItem>();

        decimal? totalWeight = null;
        foreach (var orderItem in orderItems)
        {
            var product = await _productService.GetProductByIdAsync(orderItem.ProductId);

            //is shippable
            if (product != null)
            {
                if (!product.IsShipEnabled)
                    continue;

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
                if (product.ManageInventoryMethod == ManageInventoryMethod.ManageStock && product.UseMultipleWarehouses)
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

                foreach (var formKey in form.Keys)
                    if (formKey.Equals($"qtyToAdd{orderItem.Id}", StringComparison.InvariantCultureIgnoreCase))
                    {
                        int.TryParse(form[formKey], out qtyToAdd);
                        break;
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

                if (shipment == null)
                {
                    var trackingNumber = form["TrackingNumber"];
                    var adminComment = form["AdminComment"];
                    shipment = new Shipment
                    {
                        OrderId = order.Id,
                        TrackingNumber = trackingNumber,
                        TotalWeight = null,
                        ShippedDateUtc = null,
                        DeliveryDateUtc = null,
                        AdminComment = adminComment,
                        CreatedOnUtc = DateTime.UtcNow
                    };
                }

                //create a shipment item
                var shipmentItem = new ShipmentItem
                {
                    OrderItemId = orderItem.Id,
                    Quantity = qtyToAdd,
                    WarehouseId = warehouseId
                };

                shipmentItems.Add(shipmentItem);
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
            await _orderService.InsertOrderNoteAsync(new OrderNote
            {
                OrderId = order.Id,
                Note = "A shipment has been added",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow
            });

            await LogEditOrderAsync(order.Id);

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Orders.Shipments.Added"));
            return continueEditing
                    ? RedirectToAction("ShipmentDetails", new { id = shipment.Id })
                    : RedirectToAction("Edit", new { id = orderId });
        }

        _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Admin.Orders.Shipments.NoProductsSelected"));

        return RedirectToAction("AddShipment", new { orderId });
    }

    public virtual async Task<IActionResult> EditShipment(int orderId)
    {
        if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Operate))
            return AccessDeniedView();

        //try to get an order with the specified id
        var order = await _orderService.GetOrderByIdAsync(orderId);
        if (order == null)
            return RedirectToAction("List");

        //a vendor should have access only to his products
        var orderItems = await _orderService.GetOrderItemsAsync(order.Id, vendorId: (await _workContext.GetCurrentVendorAsync())?.Id ?? 0);
        if (!orderItems.Any())
            return RedirectToAction("List");

        //prepare model
        var model = await _orderOperationsModelFactory.PrepareShipmentModelAsync(new ShipmentModel(), null, order);

        return View("~/Plugins/SSI.Shipping.Manager/Views/Order/AddShipment.cshtml", model);
    }

    [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
    [FormValueRequired("save", "save-continue")]
    public virtual async Task<IActionResult> EditShipment(int orderId, IFormCollection form, bool continueEditing)
    {
        PackagingOption packagingOption = null;

        if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Operate))
            return AccessDeniedView();

        //try to get an order with the specified id
        var order = await _orderService.GetOrderItemByIdAsync(orderId);
        if (order == null)
            return RedirectToAction("List");

        //a vendor should have access only to his products
        var orderItems = await _orderService.GetOrderItemsAsync(order.Id, vendorId: (await _workContext.GetCurrentVendorAsync())?.Id ?? 0);

        if (!orderItems.Any())
            return RedirectToAction("List");

        Shipment shipment = null;
        var shipmentItems = new List<ShipmentItem>();

        decimal? totalWeight = null;
        foreach (var orderItem in orderItems)
        {
            var product = await _productService.GetProductByIdAsync(orderItem.ProductId);

            //is shippable
            if (product != null)
            {
                if (!product.IsShipEnabled)
                    continue;

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
                if (product.ManageInventoryMethod == ManageInventoryMethod.ManageStock && product.UseMultipleWarehouses)
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

                foreach (var formKey in form.Keys)
                    if (formKey.Equals($"qtyToAdd{orderItem.Id}", StringComparison.InvariantCultureIgnoreCase))
                    {
                        int.TryParse(form[formKey], out qtyToAdd);
                        break;
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

                packagingOption = await _shippingManagerService.GetDefaultPackagingOption();
                if (packagingOption != null)
                    totalWeight += packagingOption.Weight;

                if (shipment == null)
                {
                    var trackingNumber = form["TrackingNumber"];
                    var adminComment = form["AdminComment"];
                    shipment = new Shipment
                    {
                        OrderId = order.Id,
                        TrackingNumber = trackingNumber,
                        TotalWeight = null,
                        ShippedDateUtc = null,
                        DeliveryDateUtc = null,
                        AdminComment = adminComment,
                        CreatedOnUtc = DateTime.UtcNow
                    };
                }

                //create a shipment item
                var shipmentItem = new ShipmentItem
                {
                    OrderItemId = orderItem.Id,
                    Quantity = qtyToAdd,
                    WarehouseId = warehouseId
                };

                shipmentItems.Add(shipmentItem);
            }
        }

        //if we have at least one item in the shipment, then save it
        if (shipmentItems.Any())
        {
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
            await _orderService.InsertOrderNoteAsync(new OrderNote
            {
                OrderId = order.Id,
                Note = "A shipment has been added",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow
            });

            await LogEditOrderAsync(order.Id);

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Orders.Shipments.Added"));
            return continueEditing
                    ? RedirectToAction("ShipmentDetails", new { id = shipment.Id })
                    : RedirectToAction("Edit", new { id = orderId });
        }

        _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Admin.Orders.Shipments.NoProductsSelected"));

        return RedirectToAction("EditShipment", new { orderId });
    }

    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    [HttpPost, ActionName("ShipmentList")]
    [FormValueRequired("pdf-exportpackagingreport")]
    public virtual async Task<IActionResult> PdfPackagingReport(ShipmentSearchModel model)
    {
        if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Operate))
            return AccessDeniedView();

        var startDateValue = model.StartDate == null ? null
                        : (DateTime?)_dateTimeHelper.ConvertToUtcTime(model.StartDate.Value, await _dateTimeHelper.GetCurrentTimeZoneAsync());

        var endDateValue = model.EndDate == null ? null
                        : (DateTime?)_dateTimeHelper.ConvertToUtcTime(model.EndDate.Value, await _dateTimeHelper.GetCurrentTimeZoneAsync()).AddDays(1);

        //a vendor should have access only to his products
        var vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

        //load shipments
        var shipments = await _shipmentService.GetAllShipmentsAsync(vendorId: vendorId,
            warehouseId: model.WarehouseId,
            shippingCountryId: model.CountryId,
            shippingStateId: model.StateProvinceId.HasValue ? model.StateProvinceId.Value : 0,
            shippingCounty: model.County,
            shippingCity: model.City,
            trackingNumber: model.TrackingNumber,
            loadNotShipped: model.DontDisplayShipped,
            loadNotDelivered: model.DontDisplayDelivered,
            createdFromUtc: startDateValue,
            createdToUtc: endDateValue);

        //ensure that we at least one shipment selected
        if (!shipments.Any())
        {
            _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Admin.Orders.Shipments.NoShipmentsSelected"));
            return RedirectToAction("ShipmentList");
        }

        byte[] bytes;
        using (var stream = new MemoryStream())
        {
            await _orderItemPdfService.PrintPackagingReportToPdfAsync(stream, shipments,
                _orderSettings.GeneratePdfInvoiceInCustomerLanguage ? 0 : (await _workContext.GetWorkingLanguageAsync()).Id);
            bytes = stream.ToArray();
        }

        string reportName = "packagingreport_" + DateTime.Now.ToShortDateString() + "_" + CommonHelper.GenerateRandomDigitCode(4) + ".pdf";
        return File(bytes, MimeTypes.ApplicationPdf, reportName);
    }

    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    [HttpPost, ActionName("ShipmentList")]
    [FormValueRequired("pdf-exportpackagingslips-all")]
    public virtual async Task<IActionResult> PdfPackagingSlipAll(ShipmentSearchModel model)
    {
        if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Operate))
            return AccessDeniedView();

        var startDateValue = model.StartDate == null ? null
                        : (DateTime?)_dateTimeHelper.ConvertToUtcTime(model.StartDate.Value, await _dateTimeHelper.GetCurrentTimeZoneAsync());

        var endDateValue = model.EndDate == null ? null
                        : (DateTime?)_dateTimeHelper.ConvertToUtcTime(model.EndDate.Value, await _dateTimeHelper.GetCurrentTimeZoneAsync()).AddDays(1);

        //a vendor should have access only to his products
        var vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

        //load shipments
        var shipments = await _shipmentService.GetAllShipmentsAsync(vendorId: vendorId,
            warehouseId: model.WarehouseId,
            shippingCountryId: model.CountryId,
            shippingStateId: model.StateProvinceId.HasValue ? model.StateProvinceId.Value : 0,
            shippingCounty: model.County,
            shippingCity: model.City,
            trackingNumber: model.TrackingNumber,
            loadNotShipped: model.DontDisplayShipped,
            createdFromUtc: startDateValue,
            createdToUtc: endDateValue);

        //ensure that we at least one shipment selected
        if (!shipments.Any())
        {
            _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Admin.Orders.Shipments.NoShipmentsSelected"));
            return RedirectToAction("ShipmentList");
        }

        try
        {
            byte[] bytes;
            using (var stream = new MemoryStream())
            {
                await _pdfService.PrintPackagingSlipsToPdfAsync(stream, shipments, _orderSettings.GeneratePdfInvoiceInCustomerLanguage ? null : await _workContext.GetWorkingLanguageAsync());
                bytes = stream.ToArray();
            }

            return File(bytes, "application/zip", "packagingslips.zip");
        }
        catch (Exception exc)
        {
            await _notificationService.ErrorNotificationAsync(exc);
            return RedirectToAction("ShipmentList");
        }
    }

    [HttpPost]
    public virtual async Task<IActionResult> PdfPackagingSlipSelected(string selectedIds)
    {
        if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Operate))
            return AccessDeniedView();

        var shipments = new List<Shipment>();
        if (selectedIds != null)
        {
            var ids = selectedIds
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => Convert.ToInt32(x))
                .ToArray();
            shipments.AddRange(await _shipmentService.GetShipmentsByIdsAsync(ids));
        }

        foreach (var shipment in shipments)
        {
            //a vendor should have access only to his products
            if (await _workContext.GetCurrentVendorAsync() != null && !await HasAccessToShipmentAsync(shipment))
                return Content(string.Empty);
        }

        //ensure that we at least one shipment selected
        if (!shipments.Any())
        {
            _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Admin.Orders.Shipments.NoShipmentsSelected"));
            return RedirectToAction("ShipmentList");
        }

        try
        {
            byte[] bytes;
            await using (var stream = new MemoryStream())
            {
                await _pdfService.PrintPackagingSlipsToPdfAsync(stream, shipments, _orderSettings.GeneratePdfInvoiceInCustomerLanguage ? null : await _workContext.GetWorkingLanguageAsync());
                bytes = stream.ToArray();
            }

            return File(bytes, "application/zip", "packagingslips.zip");
        }
        catch (Exception exc)
        {
            await _notificationService.ErrorNotificationAsync(exc);
            return RedirectToAction("ShipmentList");
        }
    }

        [HttpPost]
        public virtual async Task<IActionResult> SetAsPackageSelected(ICollection<int> selectedIds)
        {
            int count = 0;
            string message = string.Empty;

            if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Operate))
                return AccessDeniedView();

        var shipments = new List<Shipment>();
        if (selectedIds != null)
        {
            shipments.AddRange(await _shipmentService.GetShipmentsByIdsAsync(selectedIds.ToArray()));
        }

            if (selectedIds.Count == 0)
                return Json(new { Result = true, Message = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Shipment.SelectShipment") });
            else
            {
                foreach (var shipment in shipments)
                {
                    message = string.Empty;

                //a vendor should have access only to his products
                if (await _workContext.GetCurrentVendorAsync() != null && !await HasAccessToShipmentAsync(shipment))
                    return Content(string.Empty);

                var order = await _orderService.GetOrderByIdAsync(shipment.OrderId);
                if (order != null)
                {
                    if (order.ShippingRateComputationMethodSystemName.Contains(ShippingManagerDefaults.CanadaPostSystemName))
                    {

                        if (_shippingManagerSettings.TestMode)
                        {
                            message = "Shipping Manager Create Shipment for Order: " + order.Id.ToString() +
                                " for Shipping Method: " + order.ShippingMethod;
                            await _logger.InsertLogAsync(LogLevel.Information, message, message);
                        }

                            try
                            {
                                var status = await _canadaPostService.CanadaPostCreateShipmentAsync(shipment);
                                if (!status)
                                    return Json(new { Success = false, Message = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Shipment.ErrorCheckLog") });
                                else
                                    count++;
                            }
                            catch (Exception exc)
                            {
                                message = "Shipping Manager - " + exc.Message;
                                await _logger.InsertLogAsync(LogLevel.Information, message);

                            return Json(new { Success = false, Message = message });
                        }

                    }
                    else if (order.ShippingRateComputationMethodSystemName.Contains(ShippingManagerDefaults.SendCloudSystemName))
                    {

                        if (_shippingManagerSettings.TestMode)
                        {
                            message = "Shipping Manager Create Shipment for Order: " + order.Id.ToString() +
                                " for Shipping Method: " + order.ShippingMethod;
                            await _logger.InsertLogAsync(LogLevel.Information, message, message);
                        }

                            try
                            {
                                if (shipment.ShippedDateUtc == null)
                                {
                                    await _sendcloudService.SendCloudCreateParcelAsync(shipment);
                                    count++;
                                }
                            }
                            catch (Exception exc)
                            {
                                message = "Shipping Manager - " + exc.Message;
                                await _logger.InsertLogAsync(LogLevel.Information, message);

                            return Json(new { Success = false, Message = message });
                        }

                    }
                    else if (order.ShippingRateComputationMethodSystemName.Contains(ShippingManagerDefaults.AramexSystemName))
                    {

                        if (_shippingManagerSettings.TestMode)
                        {
                            message = "Shipping Manager Create Shipment for Order: " + order.Id.ToString() +
                                " for Shipping Method: " + order.ShippingMethod;
                            await _logger.InsertLogAsync(LogLevel.Information, message, message);
                        }

                            try
                            {
                                if (shipment.ShippedDateUtc == null)
                                {
                                    await _fastwayService.FastwayCreateParcelAsync(shipment);
                                    count++;
                                }
                            }
                            catch (Exception exc)
                            {
                                message = "Shipping Manager - " + exc.Message;
                                await _logger.InsertLogAsync(LogLevel.Information, message);

                                return Json(new { Success = false, Message = message });
                            }

                    }
                    else
                    {
                        message = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Shipment.NoShipmentOption");
                        return Json(new { Success = false, Message = message });
                    }
                }

            }
        }

            message = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Shipment.ParcelsCreated") + " " + count.ToString();
            return Json(new { Result = true, Message = message });
        }

    [HttpPost]
    public virtual async Task<IActionResult> SetAsShippedSelected(ICollection<int> selectedIds)
    {
        if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Operate))
            return AccessDeniedView();

        var shipments = new List<Shipment>();
        if (selectedIds != null)
        {
            shipments.AddRange(await _shipmentService.GetShipmentsByIdsAsync(selectedIds.ToArray()));
        }

        foreach (var shipment in shipments)
        {
            //a vendor should have access only to his products
            if (await _workContext.GetCurrentVendorAsync() != null && !await HasAccessToShipmentAsync(shipment))
                return Content(string.Empty);

            try
            {
                await _orderProcessingService.ShipAsync(shipment, true);
            }
            catch
            {
                //ignore any exception
            }
        }

        return Json(new { Result = true });
    }

    [HttpPost]
    public virtual async Task<IActionResult> SetAsDeliveredSelected(ICollection<int> selectedIds)
    {
        if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Operate))
            return AccessDeniedView();

        var shipments = new List<Shipment>();
        if (selectedIds != null)
        {
            shipments.AddRange(await _shipmentService.GetShipmentsByIdsAsync(selectedIds.ToArray()));
        }

        foreach (var shipment in shipments)
        {
            //a vendor should have access only to his products
            if (await _workContext.GetCurrentVendorAsync() != null && !await HasAccessToShipmentAsync(shipment))
                return Content(string.Empty);

            try
            {
                await _orderProcessingService.DeliverAsync(shipment, true);
            }
            catch
            {
                //ignore any exception
            }
        }

        return Json(new { Result = true });
    }

    #endregion

    #region Shipment Details

    public virtual async Task<IActionResult> ShipmentDetails(int id)
    {
        if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Operate))
            return AccessDeniedView();

        //try to get a shipment with the specified id
        var shipment = await _shipmentService.GetShipmentByIdAsync(id);
        if (shipment == null)
            return RedirectToAction("List");

        //try to get an order with the specified id
        var order = await _orderService.GetOrderByIdAsync(shipment.OrderId);
        if (order == null)
            return RedirectToAction("List");

        //a vendor should have access only to his products
        if (await _workContext.GetCurrentVendorAsync() != null && !await HasAccessToShipmentAsync(shipment))
            return RedirectToAction("List");

        //prepare model
        var model = await _orderOperationsModelFactory.PrepareShipmentModelAsync(null, shipment, order);

        return View("~/Plugins/SSI.Shipping.Manager/Views/Order/ShipmentDetails.cshtml", model);
    }

    [HttpPost]
    public virtual async Task<IActionResult> DeleteShipment(int id)
    {
        if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Operate))
            return AccessDeniedView();

        //try to get a shipment with the specified id
        var shipment = await _shipmentService.GetShipmentByIdAsync(id);
        if (shipment == null)
            return RedirectToAction("List");

        //a vendor should have access only to his products
        if (await _workContext.GetCurrentVendorAsync() != null && !await HasAccessToShipmentAsync(shipment))
            return RedirectToAction("List");

        var shipmentItems = await _shipmentService.GetShipmentItemsByShipmentIdAsync(shipment.Id);

        foreach (var shipmentItem in shipmentItems)
        {
            var orderItem = await _orderService.GetOrderItemByIdAsync(shipmentItem.OrderItemId);
            if (orderItem == null)
                continue;

            var product = await _productService.GetProductByIdAsync(orderItem.ProductId);
            if (product != null)
                await _productService.ReverseBookedInventoryAsync(product, shipmentItem,
                    string.Format(await _localizationService.GetResourceAsync("Admin.StockQuantityHistory.Messages.DeleteShipment"), shipment.OrderId));
        }

        var orderId = shipment.OrderId;
        await _shipmentService.DeleteShipmentAsync(shipment);

        var order = await _orderService.GetOrderByIdAsync(orderId);

        //add a note
        await _orderService.InsertOrderNoteAsync(new OrderNote
        {
            OrderId = order.Id,
            Note = "A shipment has been deleted",
            DisplayToCustomer = false,
            CreatedOnUtc = DateTime.UtcNow
        });

        await _orderService.UpdateOrderAsync(order);

        await LogEditOrderAsync(order.Id);

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Orders.Shipments.Deleted"));
        return RedirectToAction("Edit", new { id = orderId });
    }

    [HttpPost, ActionName("ShipmentDetails")]
    [FormValueRequired("setshippingmethod")]
    public virtual async Task<IActionResult> SetShippingMethod(ShipmentModel model)
    {
        if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Operate))
            return AccessDeniedView();

        int vendorId = 0;
        var vendor = await _workContext.GetCurrentVendorAsync();
        if (vendor != null)
            vendorId = vendor.Id;

        //try to get a shipment with the specified id
        var shipment = await _shipmentService.GetShipmentByIdAsync(model.Id);
        if (shipment == null)
            return RedirectToAction("List");

        //a vendor should have access only to his products
        if (await _workContext.GetCurrentVendorAsync() != null && !await HasAccessToShipmentAsync(shipment))
            return RedirectToAction("List");

        //try to get an order with the specified id
        var order = await _orderService.GetOrderByIdAsync(shipment.OrderId);
        if (order == null)
            return RedirectToAction("List");

        // Find shipping method
        ShippingMethod shippingMethod = null;
        var smbwbt = new ShippingManagerByWeightByTotal();

        if (!string.IsNullOrEmpty(model.ShippingMethodName))
        {
            //shipping methods
            (shippingMethod, smbwbt) = await _shippingManagerService.GetShippingMethodFromFriendlyNameAsync(model.ShippingMethodName);
            if (shippingMethod != null && smbwbt != null)
            {
                model.ShippingMethodId = smbwbt.ShippingMethodId;
                var carrierShippingPluginProvider = await _carrierService.GetCarrierShippingPluginProvideAsync(smbwbt.CarrierId);
                if (!string.IsNullOrEmpty(carrierShippingPluginProvider))
                    model.ShippingRateComputationMethodSystemName = carrierShippingPluginProvider;
            }
            else
            {
                shippingMethod = await _shippingManagerService.GetShippingMethodByIdAsync(model.ShippingMethodId);
                if (shippingMethod != null)
                {
                    var replace = " - " + shippingMethod.Name;
                    model.ShippingMethodName = model.ShippingMethodName.Replace(replace, "");
                    order.ShippingMethod = model.ShippingMethodName;
                }

                if (model.ShippingMethodName.Contains(" - "))
                {
                    var names = model.ShippingMethodName.Split(" - ");
                    model.ShippingMethodName = names[0];
                }
            }

            order.ShippingMethod = model.ShippingMethodName;
            order.ShippingRateComputationMethodSystemName = model.ShippingRateComputationMethodSystemName;
        }
        else
        {
            //shipping methods
            shippingMethod = await _shippingManagerService.GetShippingMethodByIdAsync(model.ShippingMethodId);
            if (shippingMethod != null)
            {
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

                var rates = await _shippingManagerService.GetAllRatesAsync(vendorId, shippingAddressCountry.Id);
                if (rates != null)
                {
                    var rate = rates.Where(sm => sm.ShippingMethodId == model.ShippingMethodId).FirstOrDefault();
                    if (rate != null)
                    {
                        if (!string.IsNullOrEmpty(rate.FriendlyName))
                            order.ShippingMethod = rate.FriendlyName;
                        else
                            order.ShippingMethod = shippingMethod.Name;

                        var carrier = await _shippingManagerService.GetCarrierByIdAsync(rate.CarrierId);
                        if (carrier != null)
                            order.ShippingRateComputationMethodSystemName = carrier.ShippingRateComputationMethodSystemName;
                    }
                }
            }
        }

        await _orderService.UpdateOrderAsync(order);

        await _shipmentService.UpdateShipmentAsync(shipment);

        return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
    }

    [HttpPost, ActionName("ShipmentDetails")]
    [FormValueRequired("setpackagingoption")]
    public virtual async Task<IActionResult> SetPackagingOption(ShipmentModel model)
    {
        if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Operate))
            return AccessDeniedView();

        //try to get a shipment with the specified id
        var shipment = await _shipmentService.GetShipmentByIdAsync(model.Id);
        if (shipment == null)
            return RedirectToAction("List");

        //a vendor should have access only to his products 
        if (await _workContext.GetCurrentVendorAsync() != null && !await HasAccessToShipmentAsync(shipment))
            return RedirectToAction("List");

        decimal? totalWeight = null;
        var shipmentDetails = _shipmentDetailsService.GetShipmentDetailsForShipmentId(model.Id);
        if (shipmentDetails != null)
        {
            var packagingOption = _packagingOptionService.GetSimplePackagingOptionById(model.PackagingOptionId);
            if (packagingOption != null)
            {
                shipmentDetails.PackagingOptionItemId = packagingOption.Id;
                await _shipmentDetailsService.UpdateShipmentDetailsAsync(shipmentDetails);

                var shipmentItems = await _shipmentService.GetShipmentItemsByShipmentIdAsync(shipment.Id);
                if (shipmentItems.Any())
                {
                    totalWeight = 0;
                    foreach (var si in shipmentItems)
                    {
                        if (si.Quantity > 0)
                        {
                            var orderItem = await _orderService.GetOrderItemByIdAsync(si.OrderItemId);
                            if (orderItem == null)
                                continue;

                            totalWeight += orderItem.ItemWeight * si.Quantity;
                        }
                    }
                }

                totalWeight += packagingOption.Weight;

                shipment.TotalWeight = totalWeight;
            }
        }

        await _shipmentService.UpdateShipmentAsync(shipment);

        return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
    }

    [HttpPost, ActionName("ShipmentDetails")]
    [FormValueRequired("setscheduledshipdate")]
    public virtual async Task<IActionResult> SetScheduledShipDate(ShipmentModel model)
    {
        if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Operate))
            return AccessDeniedView();

        //try to get a shipment with the specified id
        var shipment = await _shipmentService.GetShipmentByIdAsync(model.Id);
        if (shipment == null)
            return RedirectToAction("List");

        if (!model.ScheduledShipDate.HasValue || model.ScheduledShipDate.HasValue && model.ScheduledShipDate.Value > DateTime.Now)
        {
            var orderShipmentDetails = _shipmentDetailsService.GetShipmentDetailsForShipmentId(shipment.Id);
            if (orderShipmentDetails != null)
            {
                orderShipmentDetails.ScheduledShipDate = model.ScheduledShipDate;
                await _shipmentDetailsService.UpdateShipmentDetailsAsync(orderShipmentDetails);
            }
        }

        return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
    }

    [HttpPost, ActionName("ShipmentDetails")]
    [FormValueRequired("settrackingnumber")]
    public virtual async Task<IActionResult> SetTrackingNumber(ShipmentModel model)
    {
        if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Operate))
            return AccessDeniedView();

        //try to get a shipment with the specified id
        var shipment = await _shipmentService.GetShipmentByIdAsync(model.Id);
        if (shipment == null)
            return RedirectToAction("List");

        //a vendor should have access only to his products
        if (await _workContext.GetCurrentVendorAsync() != null && !await HasAccessToShipmentAsync(shipment))
            return RedirectToAction("List");

        shipment.TrackingNumber = model.TrackingNumber;

        await _shipmentService.UpdateShipmentAsync(shipment);

        return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
    }

    [HttpPost, ActionName("ShipmentDetails")]
    [FormValueRequired("refundorcancelshipment")] 
    public virtual async Task<IActionResult> RefundShipment(ShipmentModel model)
    {
        bool status = false;

        if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Operate))
            return AccessDeniedView();

        //try to get a shipment with the specified id
        var shipment = await _shipmentService.GetShipmentByIdAsync(model.Id);
        if (shipment == null)
            return RedirectToAction("List");

        //a vendor should have access only to his products
        if (await _workContext.GetCurrentVendorAsync() != null && !await HasAccessToShipmentAsync(shipment))
            return RedirectToAction("List");

        if (model.ShippingRateComputationMethodSystemName == ShippingManagerDefaults.CanadaPostSystemName)
        {
            status = await _canadaPostService.CanadaPostRefundShipmentAsync(shipment);
            if (!status)
                _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Orders.Shipments.Refund.Error"));
        }
        else if (model.ShippingRateComputationMethodSystemName == ShippingManagerDefaults.SendCloudSystemName)
        {
            status = await _sendcloudService.SendcloudCancelParcelAsync(shipment);
            if (!status)
                _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Orders.Shipments.Cancel.Error"));
            else
            {
                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Orders.Shipments.Cancelled"));
                //try to get a shipment with the specified id
                shipment = await _shipmentService.GetShipmentByIdAsync(model.Id);
                if (shipment != null)
                {
                    shipment.TrackingNumber = string.Empty;
                    await _shipmentService.UpdateShipmentAsync(shipment);
                }
            }
        }

        return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
    }

    [HttpPost, ActionName("ShipmentDetails")]
    [FormValueRequired("setadmincomment")]
    public virtual async Task<IActionResult> SetShipmentAdminComment(ShipmentModel model)
    {
        if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Operate))
            return AccessDeniedView();

        //try to get a shipment with the specified id
        var shipment = await _shipmentService.GetShipmentByIdAsync(model.Id);
        if (shipment == null)
            return RedirectToAction("List");

        //a vendor should have access only to his products
        if (await _workContext.GetCurrentVendorAsync() != null && !await HasAccessToShipmentAsync(shipment))
            return RedirectToAction("List");

        shipment.AdminComment = model.AdminComment;
        
        await _shipmentService.UpdateShipmentAsync(shipment);

        return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
    }

    [HttpPost, ActionName("ShipmentDetails")]
    [FormValueRequired("setasshipped")]
    public virtual async Task<IActionResult> SetAsShipped(int id)
    {
        if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Operate))
            return AccessDeniedView();

        //try to get a shipment with the specified id
        var shipment = await _shipmentService.GetShipmentByIdAsync(id);
        if (shipment == null)
            return RedirectToAction("List");

        //a vendor should have access only to his products
        if (await _workContext.GetCurrentVendorAsync() != null && !await HasAccessToShipmentAsync(shipment))
            return RedirectToAction("List");

        try
        {
            await _orderProcessingService.ShipAsync(shipment, true);
            await LogEditOrderAsync(shipment.OrderId);
            return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
        }
        catch (Exception exc)
        {
            //error
            await _notificationService.ErrorNotificationAsync(exc);
            return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
        }
    }

    [HttpPost, ActionName("ShipmentDetails")]
    [FormValueRequired("saveshippeddate")]
    public virtual async Task<IActionResult> EditShippedDate(ShipmentModel model)
    {
        if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Operate))
            return AccessDeniedView();

        //try to get a shipment with the specified id
        var shipment = await _shipmentService.GetShipmentByIdAsync(model.Id);
        if (shipment == null)
            return RedirectToAction("List");

        //a vendor should have access only to his products
        if (await _workContext.GetCurrentVendorAsync() != null && !await HasAccessToShipmentAsync(shipment))
            return RedirectToAction("List");

        try
        {
            if (!model.ShippedDateUtc.HasValue)
            {
                throw new Exception("Enter shipped date");
            }

            shipment.ShippedDateUtc = model.ShippedDateUtc;
            await _shipmentService.UpdateShipmentAsync(shipment);

            return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
        }
        catch (Exception exc)
        {
            //error
            await _notificationService.ErrorNotificationAsync(exc);
            return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
        }
    }

        [HttpPost, ActionName("ShipmentDetails")]
        [FormValueRequired("clearshippeddate")]
        public virtual async Task<IActionResult> ClearShippedDate(ShipmentModel model)
        {
            if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Operate))
                return AccessDeniedView();

            //try to get a shipment with the specified id
            var shipment = await _shipmentService.GetShipmentByIdAsync(model.Id);
            if (shipment == null)
                return RedirectToAction("List");

            //a vendor should have access only to his products
            if (await _workContext.GetCurrentVendorAsync() != null && !await HasAccessToShipmentAsync(shipment))
                return RedirectToAction("List");

            try
            {
                shipment.ShippedDateUtc = null;
                await _shipmentService.UpdateShipmentAsync(shipment);

                return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
            }
            catch (Exception exc)
            {
                //error
                await _notificationService.ErrorNotificationAsync(exc);
                return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
            }
        }

        [HttpPost, ActionName("ShipmentDetails")]
        [FormValueRequired("setasdelivered")]
        public virtual async Task<IActionResult> SetAsDelivered(int id)
        {
            if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Operate))
                return AccessDeniedView();

        //try to get a shipment with the specified id
        var shipment = await _shipmentService.GetShipmentByIdAsync(id);
        if (shipment == null)
            return RedirectToAction("List");

        //a vendor should have access only to his products
        if (await _workContext.GetCurrentVendorAsync() != null && !await HasAccessToShipmentAsync(shipment))
            return RedirectToAction("List");

        try
        {
            await _orderProcessingService.DeliverAsync(shipment, true);
            await LogEditOrderAsync(shipment.OrderId);
            return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
        }
        catch (Exception exc)
        {
            //error
            await _notificationService.ErrorNotificationAsync(exc);
            return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
        }
    }

    [HttpPost, ActionName("ShipmentDetails")]
    [FormValueRequired("savedeliverydate")]
    public virtual async Task<IActionResult> EditDeliveryDate(ShipmentModel model)
    {
        if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Operate))
            return AccessDeniedView();

        //try to get a shipment with the specified id
        var shipment = await _shipmentService.GetShipmentByIdAsync(model.Id);
        if (shipment == null)
            return RedirectToAction("List");

        //a vendor should have access only to his products
        if (await _workContext.GetCurrentVendorAsync() != null && !await HasAccessToShipmentAsync(shipment))
            return RedirectToAction("List");

        try
        {
            if (!model.DeliveryDateUtc.HasValue)
            {
                throw new Exception("Enter delivery date");
            }

            shipment.DeliveryDateUtc = model.DeliveryDateUtc;
            await _shipmentService.UpdateShipmentAsync(shipment);

            return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
        }
        catch (Exception exc)
        {
            //error
            await _notificationService.ErrorNotificationAsync(exc);
            return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
        }
    }

        [HttpPost, ActionName("ShipmentDetails")]
        [FormValueRequired("cleardeliverydate")]
        public virtual async Task<IActionResult> ClearDeliveryDate(ShipmentModel model)
        {
            if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Operate))
                return AccessDeniedView();

            //try to get a shipment with the specified id
            var shipment = await _shipmentService.GetShipmentByIdAsync(model.Id);
            if (shipment == null)
                return RedirectToAction("List");

            //a vendor should have access only to his products
            if (await _workContext.GetCurrentVendorAsync() != null && !await HasAccessToShipmentAsync(shipment))
                return RedirectToAction("List");

            try
            {
                shipment.DeliveryDateUtc = null;
                await _shipmentService.UpdateShipmentAsync(shipment);

                return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
            }
            catch (Exception exc)
            {
                //error
                await _notificationService.ErrorNotificationAsync(exc);
                return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
            }
        }

        public virtual async Task<IActionResult> PdfPackagingSlip(int shipmentId)
        {
            if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Operate))
                return AccessDeniedView();

        //try to get a shipment with the specified id
        var shipment = await _shipmentService.GetShipmentByIdAsync(shipmentId);
        if (shipment == null)
            return RedirectToAction("List");

        //a vendor should have access only to his products
        if (await _workContext.GetCurrentVendorAsync() != null && !await HasAccessToShipmentAsync(shipment))
            return RedirectToAction("List");

        var shipments = new List<Shipment>
        {
            shipment
        };

        byte[] bytes;
        using (var stream = new MemoryStream())
        {
            await _pdfService.PrintPackagingSlipsToPdfAsync(stream, shipments, _orderSettings.GeneratePdfInvoiceInCustomerLanguage ? null : await _workContext.GetWorkingLanguageAsync());
            bytes = stream.ToArray();
        }

            string reportName = "shipment_" + shipment.Id.ToString() + "_" + 
                "packaging_slip_" + DateTime.Now.ToShortDateString() + "_" + CommonHelper.GenerateRandomDigitCode(4) + ".pdf";
            return File(bytes, MimeTypes.ApplicationPdf, reportName);
        }

    public virtual async Task<IActionResult> CreateShipment(int shipmentId)
    {
        string message = string.Empty;

        if (!await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Operate))
            return AccessDeniedView();

        //try to get a shipment with the specified id
        var shipment = await _shipmentService.GetShipmentByIdAsync(shipmentId);
        if (shipment == null)
            return RedirectToAction("List");

        //a vendor should have access only to his products
        if (await _workContext.GetCurrentVendorAsync() != null && !await HasAccessToShipmentAsync(shipment))
            return RedirectToAction("List");

        var order = await _orderService.GetOrderByIdAsync(shipment.OrderId);
        if (order != null)
        {

            if (_shippingManagerSettings.TestMode)
            {
                message = "Shipping Manager Create Shipment for Order: " + order.Id.ToString() +
                    " for Shipping Method: " + order.ShippingMethod;
                await _logger.InsertLogAsync(LogLevel.Information, message, message);
            }

            try
            {
                if (order.ShippingRateComputationMethodSystemName.Contains(ShippingManagerDefaults.CanadaPostSystemName))
                {
                    var status = await _canadaPostService.CanadaPostCreateShipmentAsync(shipment);
                    if (status)
                        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Shipment.CanadaPost.ParcelCreated"));
                    else
                        _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Shipment.ErrorCheckLog"));
                }
                else if (order.ShippingRateComputationMethodSystemName.Contains(ShippingManagerDefaults.SendCloudSystemName))
                {
                    await _sendcloudService.SendCloudCreateParcelAsync(shipment);
                    _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Shipment.Sendcloud.ParcelCreated"));
                }
                else if (order.ShippingRateComputationMethodSystemName.Contains(ShippingManagerDefaults.AramexSystemName))
                {
                    shipment = await _fastwayService.FastwayCreateParcelAsync(shipment);
                    if (!string.IsNullOrEmpty(shipment.TrackingNumber))
                        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Shipment.Fastway.ParcelCreated"));
                    else
                        _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Shipment.ErrorCheckLog"));
                }
                else
                {
                    message = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Shipment.NoShipmentOption");
                    return Json(new { Success = false, Message = message });
                }
            }
            catch (Exception exc)
            {
                message = "Shipping Manager - " + exc.Message;
                await _logger.InsertLogAsync(LogLevel.Information, message);

                if (exc.Message.Contains("Error reading JObject from JsonReader"))
                    _notificationService.ErrorNotification("Api Error: Check Package Configuration and Weights and Measures");
                else
                    await _notificationService.ErrorNotificationAsync(exc);
            }
        }

        return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
    }

    #endregion

}
