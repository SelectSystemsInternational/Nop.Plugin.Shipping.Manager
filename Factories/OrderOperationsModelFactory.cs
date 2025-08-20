using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Logging;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Tax;
using Nop.Data;
using Nop.Services.Affiliates;
using Nop.Services.Attributes;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.Logging;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Shipping;
using Nop.Services.Stores;
using Nop.Services.Tax;
using Nop.Services.Vendors;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Framework.Models.Extensions;

using Nop.Plugin.Shipping.Manager.Domain;
using Nop.Plugin.Shipping.Manager.Models.Order;
using Nop.Plugin.Shipping.Manager.Settings;
using Nop.Plugin.Shipping.Manager.Services;

using Nop.Plugin.Apollo.Integrator.Services;

namespace Nop.Plugin.Shipping.Manager.Factories;

/// <summary>
/// Represents the order model factory implementation
/// </summary>
public partial class OrderOperationsModelFactory : IOrderOperationsModelFactory
{

    #region Fields

    protected readonly IStoreContext _storeContext;
    protected readonly AddressSettings _addressSettings;
    protected readonly CatalogSettings _catalogSettings;
    protected readonly CurrencySettings _currencySettings;
    protected readonly IActionContextAccessor _actionContextAccessor;
    protected readonly IAttributeFormatter<AddressAttribute, AddressAttributeValue> _addressAttributeFormatter;
    protected readonly IAddressAttributeModelFactory _addressAttributeModelFactory;
    protected readonly IAffiliateService _affiliateService;
    protected readonly IBaseAdminModelFactory _baseAdminModelFactory;
    protected readonly ICountryService _countryService;
    protected readonly ICurrencyService _currencyService;
    protected readonly IDateTimeHelper _dateTimeHelper;
    protected readonly IDiscountService _discountService;
    protected readonly IDownloadService _downloadService;
    protected readonly IEncryptionService _encryptionService;
    protected readonly IGiftCardService _giftCardService;
    protected readonly ILocalizationService _localizationService;
    protected readonly IMeasureService _measureService;
    protected readonly IOrderProcessingService _orderProcessingService;
    protected readonly IOrderReportService _orderReportService;
    protected readonly IOrderService _orderService;
    protected readonly IPaymentPluginManager _paymentPluginManager;
    protected readonly IPaymentService _paymentService;
    protected readonly IPictureService _pictureService;
    protected readonly IPriceCalculationService _priceCalculationService;
    protected readonly IPriceFormatter _priceFormatter;
    protected readonly IProductAttributeService _productAttributeService;
    protected readonly IProductService _productService;
    protected readonly IReturnRequestService _returnRequestService;
    protected readonly IShippingManagerShipmentService _shippingManagerShipmentService;
    protected readonly IShippingService _shippingService;
    protected readonly IStoreService _storeService;
    protected readonly ITaxService _taxService;
    protected readonly IUrlHelperFactory _urlHelperFactory;
    protected readonly IVendorService _vendorService;
    protected readonly IWorkContext _workContext;
    protected readonly MeasureSettings _measureSettings;
    protected readonly OrderSettings _orderSettings;
    protected readonly ShippingSettings _shippingSettings;
    protected readonly IUrlRecordService _urlRecordService;
    protected readonly TaxSettings _taxSettings;
    protected readonly ShippingManagerSettings _shippingManagerSettings;
    protected readonly IEntityGroupService _entityGroupService;
    protected readonly ICarrierService _carrierService;
    protected readonly IOrderSalesService _orderSalesService;
    protected readonly ILogger _logger;
    protected readonly IShipmentService _shipmentService;
    protected readonly IAddressService _addressService;
    protected readonly IShippingManagerService _shippingManagerService;
    protected readonly IShipmentDetailsService _shipmentDetailsService;
    protected readonly ISettingService _settingService;
    protected readonly IPackagingOptionService _packagingOptionService;
    protected readonly IRepository<ShipmentItem> _siRepository;
    protected readonly IWebHelper _webHelper;

    #endregion

    #region Ctor

    public OrderOperationsModelFactory(IStoreContext storeContext,
        AddressSettings addressSettings,
        CatalogSettings catalogSettings,
        CurrencySettings currencySettings,
        IActionContextAccessor actionContextAccessor,
        IAttributeFormatter<AddressAttribute, AddressAttributeValue> addressAttributeFormatter,
        IAddressAttributeModelFactory addressAttributeModelFactory,
        IAffiliateService affiliateService,
        IBaseAdminModelFactory baseAdminModelFactory,
        ICountryService countryService,
        ICurrencyService currencyService,
        IDateTimeHelper dateTimeHelper,
        IDiscountService discountService,
        IDownloadService downloadService,
        IEncryptionService encryptionService,
        IGiftCardService giftCardService,
        ILocalizationService localizationService,
        IMeasureService measureService,
        IOrderProcessingService orderProcessingService,
        IOrderReportService orderReportService,
        IOrderService orderService,
        IPaymentPluginManager paymentPluginManager,
        IPaymentService paymentService,
        IPictureService pictureService,
        IPriceCalculationService priceCalculationService,
        IPriceFormatter priceFormatter,
        IProductAttributeService productAttributeService,
        IProductService productService,
        IReturnRequestService returnRequestService,
        IShippingManagerShipmentService shippingManagerShipmentService,
        IShippingService shippingService,
        IStoreService storeService,
        ITaxService taxService,
        IUrlHelperFactory urlHelperFactory,
        IVendorService vendorService,
        IWorkContext workContext,
        MeasureSettings measureSettings,
        OrderSettings orderSettings,
        ShippingSettings shippingSettings,
        IUrlRecordService urlRecordService,
        TaxSettings taxSettings,
        ShippingManagerSettings shippingManagerSettings,
        IEntityGroupService entityGroupService,
        ICarrierService carrierService,
        IOrderSalesService orderSalesService,
        ILogger logger,
        IShipmentService shipmentService,
        IAddressService addressService,
        IShippingManagerService shippingManagerService,
        IShipmentDetailsService shipmentDetailsService,
        ISettingService settingService,
        IPackagingOptionService packagingOptionService,
        IRepository<ShipmentItem> siRepository,
        IWebHelper webHelper)
    {
        _storeContext = storeContext;
        _addressSettings = addressSettings;
        _catalogSettings = catalogSettings;
        _currencySettings = currencySettings;
        _actionContextAccessor = actionContextAccessor;
        _addressAttributeFormatter = addressAttributeFormatter;
        _addressAttributeModelFactory = addressAttributeModelFactory;
        _affiliateService = affiliateService;
        _baseAdminModelFactory = baseAdminModelFactory;
        _countryService = countryService;
        _currencyService = currencyService;
        _dateTimeHelper = dateTimeHelper;
        _discountService = discountService;
        _downloadService = downloadService;
        _encryptionService = encryptionService;
        _giftCardService = giftCardService;
        _localizationService = localizationService;
        _measureService = measureService;
        _orderProcessingService = orderProcessingService;
        _orderReportService = orderReportService;
        _orderService = orderService;
        _paymentPluginManager = paymentPluginManager;
        _paymentService = paymentService;
        _pictureService = pictureService;
        _priceCalculationService = priceCalculationService;
        _priceFormatter = priceFormatter;
        _productAttributeService = productAttributeService;
        _productService = productService;
        _returnRequestService = returnRequestService;
        _shippingManagerShipmentService = shippingManagerShipmentService;
        _shippingService = shippingService;
        _storeService = storeService;
        _taxService = taxService;
        _urlHelperFactory = urlHelperFactory;
        _vendorService = vendorService;
        _workContext = workContext;
        _measureSettings = measureSettings;
        _orderSettings = orderSettings;
        _shippingSettings = shippingSettings;
        _urlRecordService = urlRecordService;
        _taxSettings = taxSettings;
        _shippingManagerSettings = shippingManagerSettings;
        _entityGroupService = entityGroupService;
        _carrierService = carrierService;
        _orderSalesService = orderSalesService;
        _logger = logger;
        _shipmentService = shipmentService;
        _addressService = addressService;
        _shippingManagerService = shippingManagerService;
        _shipmentDetailsService = shipmentDetailsService;
        _settingService = settingService;
        _packagingOptionService = packagingOptionService;
        _siRepository = siRepository;
        _webHelper = webHelper;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Prepare default item
    /// </summary>
    /// <param name="items">Available items</param>
    /// <param name="withSpecialDefaultItem">Whether to insert the first special item for the default value</param>
    /// <param name="defaultItemText">Default item text; pass null to use "All" text</param>
    /// <returns>A task that represents the asynchronous operation</returns> 
    public virtual async Task PrepareDefaultItemAsync(IList<SelectListItem> items, bool withSpecialDefaultItem, string defaultItemText = null)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        //whether to insert the first special item for the default value
        if (!withSpecialDefaultItem)
            return;

        //at now we use "0" as the default value
        const string value = "0";

        //prepare item text
        defaultItemText = defaultItemText ?? await _localizationService.GetResourceAsync("Admin.Common.All");

            //insert this default item at first
            items.Insert(0, new SelectListItem { Text = defaultItemText, Value = value, Selected = true});

    }

    /// <summary>
    /// Prepare available carriers
    /// </summary>
    /// <param name="items">Warehouse items</param>
    /// <param name="withSpecialDefaultItem">Whether to insert the first special item for the default value</param>
    /// <param name="defaultItemText">Default item text; pass null to use default value of the default item text</param>
    /// <returns>A task that represents the asynchronous operation</returns> 
    public virtual async Task PrepareCarriersListAsync(IList<SelectListItem> items, bool withSpecialDefaultItem = true, string defaultItemText = null)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

        //prepare available carriers
        var availableCarriers = await _carrierService.GetAllCarriersAsync();
        foreach (var carrier in availableCarriers)
        {
            string name = carrier.Name;
            if (vendorId == 0)
            {
                bool first = true;
                carrier.Name += " (";
                var entityGroups = _entityGroupService.GetAllEntityGroups(_entityGroupService.GetEntityKeyGroup(carrier), carrier.Id, "Member", null);
                foreach (var eg in entityGroups)
                {
                    var vendor = await _vendorService.GetVendorByIdAsync(eg.VendorId);
                    if (vendor != null)
                    {
                        if (first)
                            carrier.Name += vendor.Name;
                        else
                            carrier.Name += "," + vendor.Name;

                        first = false;
                    }
                }
                carrier.Name += ")";
            }                   

            items.Add(new SelectListItem { Value = carrier.Id.ToString(), Text = carrier.Name });
        }

        //insert special item for the default value
        await PrepareDefaultItemAsync(items, withSpecialDefaultItem, defaultItemText);
    }

    /// <summary>
    /// Prepare available warehouses
    /// </summary>
    /// <param name="items">Warehouse items</param>
    /// <param name="withSpecialDefaultItem">Whether to insert the first special item for the default value</param>
    /// <param name="defaultItemText">Default item text; pass null to use default value of the default item text</param>
    /// <returns>A task that represents the asynchronous operation</returns> 
    public virtual async Task PrepareWarehousesListAsync(IList<SelectListItem> items, bool withSpecialDefaultItem = true, string defaultItemText = null)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

        //prepare available warehouses
        var availableWarehouses = await _shippingService.GetAllWarehousesAsync();
        foreach (var warehouse in availableWarehouses)
        {
            string name = warehouse.Name;
            if (vendorId == 0)
            {
                bool first = true;
                warehouse.Name += " (";
                var entityGroups = _entityGroupService.GetAllEntityGroups(_entityGroupService.GetEntityKeyGroup(warehouse), warehouse.Id, "Member", null);
                foreach (var eg in entityGroups)
                {
                    var vendor = await _vendorService.GetVendorByIdAsync(eg.VendorId);
                    if (vendor != null)
                    {
                        if (first)
                            warehouse.Name += vendor.Name;
                        else
                            warehouse.Name += "," + vendor.Name;

                        first = false;
                    }
                }
                warehouse.Name += ")";
            }

            items.Add(new SelectListItem { Value = warehouse.Id.ToString(), Text = warehouse.Name });
        }

        //insert special item for the default value
        await PrepareDefaultItemAsync(items, withSpecialDefaultItem, defaultItemText);
    }

    /// <summary>
    /// Gets a shipment item of shipment
    /// </summary>
    /// <param name="shipmentItemId">Shipment Item identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the shipment items
    /// </returns>
    public virtual ShipmentItem GetShipmentItemByShipmentItemIdAsync(int shipmentItemId)
    {
        if (shipmentItemId == 0)
            return null;

        return _siRepository.Table.Where(si => si.Id == shipmentItemId).FirstOrDefault();
    }

    #endregion

    #region Methods

    /// <summary>
    /// Prepare shipment item model
    /// </summary>
    /// <param name="model">Shipment item model</param>
    /// <param name="orderItem">Order item</param>
    /// <returns>A task that represents the asynchronous operation</returns>  
    public virtual async Task PrepareShipmentItemModelAsync(ShipmentItemModel model, OrderItem orderItem)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        int quantityInShipment = 0;
        var shipmentItem = GetShipmentItemByShipmentItemIdAsync(model.Id);
        if (shipmentItem != null)
        {
            quantityInShipment = shipmentItem.Quantity;
        }

        var product = await _productService.GetProductByIdAsync(orderItem.ProductId);

        //fill in additional values (not existing in the entity)
        model.OrderItemId = orderItem.Id;
        model.ProductId = orderItem.ProductId;
        model.ProductName = product != null ? product.Name : null;
        model.Sku = await _productService.FormatSkuAsync(product, orderItem.AttributesXml);
        model.AttributeInfo = orderItem.AttributeDescription;
        model.ShipSeparately = product != null ? product.ShipSeparately : false;
        model.QuantityOrdered = orderItem.Quantity;
        model.QuantityInAllShipments = await _orderService.GetTotalNumberOfItemsInAllShipmentsAsync(orderItem);
        model.QuantityInShipment = quantityInShipment;
        model.QuantityToAdd = await _orderService.GetTotalNumberOfItemsCanBeAddedToShipmentAsync(orderItem);

        var baseWeight = (await _measureService.GetMeasureWeightByIdAsync(_measureSettings.BaseWeightId))?.Name;
        var baseDimension = (await _measureService.GetMeasureDimensionByIdAsync(_measureSettings.BaseDimensionId))?.Name;
        if (orderItem.ItemWeight.HasValue)
            model.ItemWeight = $"{orderItem.ItemWeight:F2} [{baseWeight}]";
        model.ItemDimensions = $"{product.Length:F2} x {product.Width:F2} x {product.Height:F2} [{baseDimension}]";
    
        if (!product.IsRental)
            return;

        var rentalStartDate = orderItem.RentalStartDateUtc.HasValue
            ? _productService.FormatRentalDate(product, orderItem.RentalStartDateUtc.Value) : string.Empty;
        var rentalEndDate = orderItem.RentalEndDateUtc.HasValue
            ? _productService.FormatRentalDate(product, orderItem.RentalEndDateUtc.Value) : string.Empty;

        model.RentalInfo = string.Format(await _localizationService.GetResourceAsync("Order.Rental.FormattedDate"),
            rentalStartDate, rentalEndDate);

    }

    /// <summary>
    /// Prepare shipment status event models
    /// </summary>
    /// <param name="models">List of shipment status event models</param>
    /// <param name="shipment">Shipment</param>
    /// <returns>A task that represents the asynchronous operation</returns> 
    public virtual async Task PrepareShipmentStatusEventModelsAsync(IList<ShipmentStatusEventModel> models, Shipment shipment)
    {
        if (models == null)
            throw new ArgumentNullException(nameof(models));

        var shipmentTracker = await _shipmentService.GetShipmentTrackerAsync(shipment);
        var shipmentEvents = await shipmentTracker.GetShipmentEventsAsync(shipment.TrackingNumber);
        if (shipmentEvents == null)
            return;

        foreach (var shipmentEvent in shipmentEvents)
        {
            var shipmentStatusEventModel = new ShipmentStatusEventModel
            {
                Date = shipmentEvent.Date,
                EventName = shipmentEvent.EventName,
                Location = shipmentEvent.Location
            };
            var shipmentEventCountry = await _countryService.GetCountryByTwoLetterIsoCodeAsync(shipmentEvent.CountryCode);
            shipmentStatusEventModel.Country = shipmentEventCountry != null
                ? await _localizationService.GetLocalizedAsync(shipmentEventCountry, x => x.Name) : shipmentEvent.CountryCode;
            models.Add(shipmentStatusEventModel);
        }
    }

    /// <summary>
    /// Prepare shipment search model
    /// </summary>
    /// <param name="searchModel">Shipment search model</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the shipment search model
    /// </returns>
    public virtual async Task<ShipmentSearchModel> PrepareShipmentSearchModelAsync(ShipmentSearchModel searchModel)
    {
        if (searchModel == null)
            throw new ArgumentNullException(nameof(searchModel));

        if (!_shippingManagerSettings.InternationalOperationsEnabled)
        {
            //prepare available states
            await _baseAdminModelFactory.PrepareStatesAndProvincesAsync(searchModel.AvailableStates, _shippingManagerSettings.DefaultCountryId);
        }
        else
        {
            await _baseAdminModelFactory.PrepareCountriesAsync(searchModel.AvailableCountries);
        }

        //prepare available warehouses
        await PrepareWarehousesListAsync(searchModel.AvailableWarehouses);

        //prepare available carriers
        await PrepareCarriersListAsync(searchModel.AvailableCarriers);

        //prepare nested search model
        PrepareShipmentItemSearchModel(searchModel.ShipmentItemSearchModel);

        searchModel.DontDisplayShipped = true;
        searchModel.DontDisplayDelivered = true;

        //prepare page parameters
        searchModel.SetGridPageSize();

        int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

        //get shipments to fill Drop Downs
        var shipments = await _shippingManagerShipmentService.GetAllShipmentsAsync(searchModel.Page - 1, searchModel.PageSize,
            vendorId,
            searchModel.VendorGroupId,
            searchModel.CustomerId,
            searchModel.ShippingAddressId,
            searchModel.WarehouseId,
            searchModel.CarrierId,
            searchModel.CountryId,
            searchModel.StateProvinceId.HasValue ? searchModel.StateProvinceId.Value : 0,
            searchModel.County,
            searchModel.City,
            searchModel.TrackingNumber,
            searchModel.ShippingMethod,
            searchModel.DontDisplayShipped,
            searchModel.DontDisplayDelivered);

        await PrepareDefaultItemAsync(searchModel.AvailableCustomers, true);
        await PrepareDefaultItemAsync(searchModel.AvailableShippingAddress, true);
        await PrepareDefaultItemAsync(searchModel.AvailableShippingMethods, true);

        var orderList = new List<Order>();

            int shippingMethodsCount = 1;
            foreach (var shipment in shipments)
            {
                var order = await _orderService.GetOrderByIdAsync(shipment.OrderId);
                if (order != null)
                    orderList.Add(order);

            //var customerName = shipment.Order.BillingAddress.FirstName + " " + shipment.Order.BillingAddress.LastName;

            //searchModel.AvailableCustomers.Add(new SelectListItem { Value = shipment.Order.BillingAddressId.ToString(), Text = customerName });

            //customerName = (!string.IsNullOrEmpty(shipment.Order.ShippingAddress.FirstName) ? shipment.Order.ShippingAddress.FirstName : "") +
            //               (!string.IsNullOrEmpty(shipment.Order.ShippingAddress.LastName) ? " " + shipment.Order.ShippingAddress.LastName : "");

            //string shippingAddress =
            //    (!string.IsNullOrEmpty(shipment.Order.ShippingAddress.Company) ? shipment.Order.ShippingAddress.Company + "," : "") +
            //    (!string.IsNullOrEmpty(customerName) ? ":" + customerName : "") +
            //    (!string.IsNullOrEmpty(shipment.Order.ShippingAddress.Address1) ? "," + shipment.Order.ShippingAddress.Address1 : "") +
            //    (!string.IsNullOrEmpty(shipment.Order.ShippingAddress.Address2) ? "," + shipment.Order.ShippingAddress.Address2 : "") +
            //    (!string.IsNullOrEmpty(shipment.Order.ShippingAddress.City) ? "," + shipment.Order.ShippingAddress.City : "");
            //    searchModel.AvailableShippingAddress.Add(new SelectListItem { Value = shipment.Order.ShippingAddressId.ToString(), Text = shippingAddress });
        }

        var billingAddressList = new List<Address>();
        var shippingAddressList = new List<Address>();
        foreach (var order in orderList)
        {
            var billingAddress = await _addressService.GetAddressByIdAsync(order.BillingAddressId);
            if (!billingAddressList.Contains(billingAddress))
                billingAddressList.Add(billingAddress);

            if (order.ShippingAddressId.HasValue)
            {
                var shippingAddress = await _addressService.GetAddressByIdAsync(order.ShippingAddressId.Value);
                if (!shippingAddressList.Contains(shippingAddress))
                    shippingAddressList.Add(shippingAddress);
            }

                if (!string.IsNullOrEmpty(order.ShippingMethod))
                {
                    var shippingMethod = order.ShippingMethod;
                    var found = searchModel.AvailableShippingMethods.Where(x => x.Text == shippingMethod).FirstOrDefault();
                    if (found == null)
                    {
                        searchModel.AvailableShippingMethods.Add(new SelectListItem { Value = shippingMethodsCount.ToString(), Text = shippingMethod });
                        shippingMethodsCount++;
                    }

                    searchModel.ShippingMethodId = 0;
                }
            }

            foreach (var ba in billingAddressList)
            {
                var customerName = ba.FirstName + " " + ba.LastName;
                searchModel.AvailableCustomers.Add(new SelectListItem { Value = ba.Id.ToString(), Text = customerName });
            }

            foreach (var sa in shippingAddressList)
            {
                var customerName = (!string.IsNullOrEmpty(sa.FirstName) ? sa.FirstName : "") +
                    (!string.IsNullOrEmpty(sa.LastName) ? " " + sa.LastName : "");

                string address =
                    (!string.IsNullOrEmpty(sa.Company) ? sa.Company + "," : "") +
                    (!string.IsNullOrEmpty(customerName) ? customerName : "") +
                    (!string.IsNullOrEmpty(sa.Address1) ? "," + sa.Address1 : "") +
                    (!string.IsNullOrEmpty(sa.Address2) ? "," + sa.Address2 : "") +
                    (!string.IsNullOrEmpty(sa.City) ? "," + sa.City : "");

                if (!searchModel.AvailableShippingAddress.Where(x => x.Text == address).Any())
                    searchModel.AvailableShippingAddress.Add(new SelectListItem { Value = sa.Id.ToString(), Text = address });
            }

        await PrepareDefaultItemAsync(searchModel.AvailableVendorGroups, true);
        foreach (var entityGroup in await _entityGroupService.GetVendorEntityGroupsAsync())
        {
            var vendor = await _vendorService.GetVendorByIdAsync(entityGroup.VendorId);
            if (vendor != null)
            {
                searchModel.AvailableVendorGroups.Add(new SelectListItem { Value = entityGroup.Id.ToString(), Text = vendor.Name });
            }
        }

        return searchModel;
    }

    /// <summary>
    /// Prepare paged shipment list model
    /// </summary>
    /// <param name="searchModel">Shipment search model</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the Shipment list model
    /// </returns>
    public virtual async Task<ShipmentListModel> PrepareShipmentListModelAsync(ShipmentSearchModel searchModel)
    {
        if (searchModel == null)
            throw new ArgumentNullException(nameof(searchModel));

        //get parameters to filter shipments
        var vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

        var startDateValue = !searchModel.StartDate.HasValue ? null
            : (DateTime?)_dateTimeHelper.ConvertToUtcTime(searchModel.StartDate.Value, await _dateTimeHelper.GetCurrentTimeZoneAsync());
        var endDateValue = !searchModel.EndDate.HasValue ? null
            : (DateTime?)_dateTimeHelper.ConvertToUtcTime(searchModel.EndDate.Value, await _dateTimeHelper.GetCurrentTimeZoneAsync()).AddDays(1);

            if (searchModel.ShippingMethod == "All")
                searchModel.ShippingMethod = string.Empty;

        //get shipments
        var shipments = await _shippingManagerShipmentService.GetAllShipmentsAsync(searchModel.Page - 1, searchModel.PageSize,
            vendorId,
            searchModel.VendorGroupId,
            searchModel.CustomerId,
            searchModel.ShippingAddressId,
            searchModel.WarehouseId,
            searchModel.CarrierId,
            searchModel.CountryId,
            searchModel.StateProvinceId.HasValue ? searchModel.StateProvinceId.Value : 0,
            searchModel.County,
            searchModel.City,
            searchModel.TrackingNumber,
            searchModel.ShippingMethod,
            searchModel.DontDisplayShipped,
            searchModel.DontDisplayDelivered,
            0,
            startDateValue,
            endDateValue);

        //prepare list model
        var model = await new ShipmentListModel().PrepareToGridAsync(searchModel, shipments, () =>
        {
            //fill in model values from the entity
            return shipments.SelectAwait(async shipment =>
            {
                //fill in model values from the entity
                var shipmentModel = shipment.ToModel<ShipmentModel>();

                //convert dates to the user time
                shipmentModel.ShippedDate = shipment.ShippedDateUtc.HasValue
                    ? (await _dateTimeHelper.ConvertToUserTimeAsync(shipment.ShippedDateUtc.Value, DateTimeKind.Utc)).ToString()
                    : await _localizationService.GetResourceAsync("Admin.Orders.Shipments.ShippedDate.NotYet");
                shipmentModel.DeliveryDate = shipment.DeliveryDateUtc.HasValue
                    ? (await _dateTimeHelper.ConvertToUserTimeAsync(shipment.DeliveryDateUtc.Value, DateTimeKind.Utc)).ToString()
                    : await _localizationService.GetResourceAsync("Admin.Orders.Shipments.DeliveryDate.NotYet");

                //fill in additional values (not existing in the entity)
                shipmentModel.CanShip = !shipment.ShippedDateUtc.HasValue;
                shipmentModel.CanDeliver = shipment.ShippedDateUtc.HasValue && !shipment.DeliveryDateUtc.HasValue;

                var order = await _orderService.GetOrderByIdAsync(shipment.OrderId);
                if (order != null)
                    shipmentModel.CustomOrderNumber = order.CustomOrderNumber;

                if (shipment.TotalWeight.HasValue)
                    shipmentModel.TotalWeight = $"{shipment.TotalWeight:F2} [{(await _measureService.GetMeasureWeightByIdAsync(_measureSettings.BaseWeightId))?.Name}]";

                return shipmentModel;
            });
        });

        return model;
    }

    /// <summary>
    /// Prepare shipment model
    /// </summary>
    /// <param name="model">Shipment model</param>
    /// <param name="shipment">Shipment</param>
    /// <param name="order">Order</param>
    /// <param name="excludeProperties">Whether to exclude populating of some properties of model</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the shipment model
    /// </returns>
    public virtual async Task<ShipmentModel> PrepareShipmentModelAsync(ShipmentModel model, Shipment shipment, Order order,
        bool excludeProperties = false)
    {
        PackagingOption packagingOption = null;

        if (shipment != null)
        {
            //fill in model values from the entity
            model ??= shipment.ToModel<ShipmentModel>();

            model.CanShip = !shipment.ShippedDateUtc.HasValue;
            model.CanDeliver = shipment.ShippedDateUtc.HasValue && !shipment.DeliveryDateUtc.HasValue;

            var shipmentOrder = await _orderService.GetOrderByIdAsync(shipment.OrderId);

            model.CustomOrderNumber = shipmentOrder.CustomOrderNumber;
            model.ShippingRateComputationMethodSystemName = shipmentOrder.ShippingRateComputationMethodSystemName;

            model.ShippedDate = shipment.ShippedDateUtc.HasValue
                ? (await _dateTimeHelper.ConvertToUserTimeAsync(shipment.ShippedDateUtc.Value, DateTimeKind.Utc)).ToString()
                : await _localizationService.GetResourceAsync("Admin.Orders.Shipments.ShippedDate.NotYet");
            model.DeliveryDate = shipment.DeliveryDateUtc.HasValue
                ? (await _dateTimeHelper.ConvertToUserTimeAsync(shipment.DeliveryDateUtc.Value, DateTimeKind.Utc)).ToString()
                : await _localizationService.GetResourceAsync("Admin.Orders.Shipments.DeliveryDate.NotYet");

            var shipmentDetails = _shipmentDetailsService.GetShipmentDetailsForShipmentId(model.Id);
            if (_shippingManagerSettings.UsePackagingSystem)
            {
                if (shipmentDetails == null)
                {
                    packagingOption = await _shippingManagerService.GetDefaultPackagingOption();
                    if (packagingOption != null)
                    {
                        await _shippingManagerService.InsertShipmentDetails(shipment, packagingOption);
                        shipment.TotalWeight += packagingOption.Weight;
                        await _shipmentService.UpdateShipmentAsync(shipment);
                    }
                }

                shipmentDetails = _shipmentDetailsService.GetShipmentDetailsForShipmentId(model.Id);
                if (shipmentDetails != null)
                {
                    model.PackagingOptionId = shipmentDetails.PackagingOptionItemId;
                    model.AvailablePackagingOptions = _packagingOptionService.PrepareAvailablePackagingOptionsSelectList(model.PackagingOptionId);
                    packagingOption = _packagingOptionService.GetSimplePackagingOptionById(model.PackagingOptionId);
                    if (packagingOption != null)
                    {
                        var baseDimension = (await _measureService.GetMeasureDimensionByIdAsync(_measureSettings.BaseDimensionId))?.Name;
                        model.PackageDimensions = $"{packagingOption.Length:F2} x {packagingOption.Width:F2} x {packagingOption.Height:F2} [{baseDimension}]";
                    }
                }
            }
            else
            {
                model.PackageDimensions = "Packaging not used";
            }

            if (shipmentDetails != null)
            {
                var currentStoreUrl = (await _storeContext.GetCurrentStoreAsync()).Url;
                string fileName = $"canada_post_label_{shipmentDetails.ShipmentId}";
                model.ArtifactUrl = currentStoreUrl + "files/exportimport/" + fileName + ".pdf";
                model.ShipmentId = shipmentDetails.ShipmentId;
                if (shipmentDetails.ScheduledShipDate.HasValue)
                    model.ScheduledShipDate = shipmentDetails.ScheduledShipDate;
            }           

            if (shipment.TotalWeight.HasValue)
                model.TotalWeight =
                    $"{shipment.TotalWeight:F2} [{(await _measureService.GetMeasureWeightByIdAsync(_measureSettings.BaseWeightId))?.Name}]";

            //prepare shipment items
            foreach (var item in await _shipmentService.GetShipmentItemsByShipmentIdAsync(shipment.Id))
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

                await PrepareShipmentItemModelAsync(shipmentItemModel, orderItem);

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
                        await PrepareShipmentStatusEventModelsAsync(model.ShipmentStatusEvents, shipment);
                }

                string pageSize = "4x6";
                var suffix = shipment.TrackingNumber;
                var fileName = $@"Labels_{pageSize}_{suffix}.pdf";
                model.LabelUrl = $"{_webHelper.GetStoreLocation()}files/exportimport/{fileName}";
            }

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

            if (shippingMethod != null)
            {
                int index = 0;
                (model.AvailableShippingMethods, index) = await _shippingManagerService.PrepareShippingMethodsForShipmentAsync(order.ShippingMethod,
                    shippingMethod.Name, shippingAddressCountry.Id);

                if (index != 0)
                    model.ShippingMethodId = index;
            }
        }

        if (shipment != null)
            return model;

        model.OrderId = order.Id;
        model.CustomOrderNumber = order.CustomOrderNumber;
        model.ShippingRateComputationMethodSystemName = order.ShippingRateComputationMethodSystemName;  

        var orderItems = (await _orderService.GetOrderItemsAsync(order.Id, isShipEnabled: true, vendorId: (await _workContext.GetCurrentVendorAsync())?.Id ?? 0)).ToList();

        foreach (var orderItem in orderItems)
        {
            var shipmentItemModel = new ShipmentItemModel();

            var product = await _productService.GetProductByIdAsync(orderItem.ProductId);

            await PrepareShipmentItemModelAsync(shipmentItemModel, orderItem);

            //ensure that this product can be added to a shipment
            if (shipmentItemModel.QuantityToAdd <= 0)
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
                                await _shipmentService.GetQuantityInShipmentsAsync(product, warehouse.Id, true, true)
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

            model.Items.Add(shipmentItemModel);
        }

        return model;
    }

    /// <summary>
    /// Prepare paged shipment item list model
    /// </summary>
    /// <param name="searchModel">Shipment item search model</param>
    /// <param name="shipment">Shipment</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the shipment item list model
    /// </returns>
    public virtual async Task<ShipmentItemListModel> PrepareShipmentItemListModelAsync(ShipmentItemSearchModel searchModel, Shipment shipment)
    {
        if (searchModel == null)
            throw new ArgumentNullException(nameof(searchModel));

        if (shipment == null)
            throw new ArgumentNullException(nameof(shipment));

        //get shipments
        var shipmentItems = (await _shipmentService.GetShipmentItemsByShipmentIdAsync(shipment.Id)).ToPagedList(searchModel);

        if (_shippingManagerSettings.TestMode)
        {
            await _logger.InsertLogAsync(LogLevel.Debug, "Shipment Items Count: " + shipmentItems.Count(), null, null);
        }

        //prepare list model
        var model = await new ShipmentItemListModel().PrepareToGridAsync(searchModel, shipmentItems, () =>
        {
            //fill in model values from the entity
            return shipmentItems.SelectAwait(async item =>
            {
                //fill in model values from the entity
                var shipmentItemModel = new ShipmentItemModel
                {
                    Id = item.Id,
                    ShipmentItemId = item.ShipmentId,
                    OrderItemId = item.OrderItemId,
                    QuantityInThisShipment = item.Quantity
                };

                //fill in additional values (not existing in the entity)
                var orderItem = await _orderService.GetOrderItemByIdAsync(item.OrderItemId);
                if (orderItem == null)
                    return shipmentItemModel;

                shipmentItemModel.QuantityOrdered = orderItem.Quantity;

                var product = await _productService.GetProductByIdAsync(orderItem.ProductId);

                shipmentItemModel.OrderItemId = orderItem.Id;
                shipmentItemModel.ProductId = orderItem.ProductId;
                shipmentItemModel.ProductName = product.Name;

                shipmentItemModel.ShippedFromWarehouse = (await _shippingService.GetWarehouseByIdAsync(item.WarehouseId))?.Name;

                var baseWeight = (await _measureService.GetMeasureWeightByIdAsync(_measureSettings.BaseWeightId))?.Name;
                var baseDimension = (await _measureService.GetMeasureDimensionByIdAsync(_measureSettings.BaseDimensionId))?.Name;

                if (orderItem.ItemWeight.HasValue)
                    shipmentItemModel.ItemWeight = $"{orderItem.ItemWeight:F2} [{baseWeight}]";

                shipmentItemModel.ItemDimensions =
                    $"{product.Length:F2} x {product.Width:F2} x {product.Height:F2} [{baseDimension}]";

                return shipmentItemModel;
            });
        });

        if (_shippingManagerSettings.TestMode)
        {
            await _logger.InsertLogAsync(LogLevel.Debug, "Model Count: " + model.RecordsTotal, null, null);
        }

        return model;
    }

    /// <summary>
    /// Prepare shipment item search model
    /// </summary>
    /// <param name="searchModel">Shipment item search model</param>
    /// <returns>Shipment item search model</returns>
    protected virtual ShipmentItemSearchModel PrepareShipmentItemSearchModel(ShipmentItemSearchModel searchModel)
    {
        if (searchModel == null)
            throw new ArgumentNullException(nameof(searchModel));

        //prepare page parameters
        searchModel.SetGridPageSize();

        return searchModel;
    }

    #endregion

}
