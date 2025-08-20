using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;
using System.Net;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;

using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Logging;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Services.Shipping;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;

using SendCloudApi.Net.Models;
using Nop.Plugin.Shipping.Manager.Services;
using Nop.Plugin.Shipping.Manager.Factories;
using Nop.Plugin.Shipping.Manager.Settings;

using Nop.Plugin.Apollo.Integrator;
using Nop.Core.Domain.Customers;

namespace Nop.Plugin.Shipping.Manager.Controllers;

public class SendcloudController : BasePluginController
{

    #region Fields

    protected readonly ILogger _logger;
    protected readonly IOrderService _orderService;
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
    protected readonly IWebHelper _webHelper;
    protected readonly IShipmentService _shipmentService;
    protected readonly IOrderProcessingService _orderProcessingService;
    protected readonly IGenericAttributeService _genericAttributeService;
    protected readonly IWorkContext _workContext;
    protected readonly IStoreContext _storeContext;
    protected readonly ISendcloudService _sendcloudService;
    protected readonly IEncryptionService _encryptionService;
    protected readonly SendcloudApiSettings _sendcloudApiSettings;

    SystemHelper _systemHelper = new SystemHelper();

    #endregion

    #region Ctor

    public SendcloudController(ILogger logger,
        IOrderService orderService,
        CurrencySettings currencySettings,
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
        IShippingPluginManager shippingPluginManager,
        IWebHelper webHelper,
        IShipmentService shipmentService,
        IOrderProcessingService orderProcessingService,
        IGenericAttributeService genericAttributeService,
        IWorkContext workContext,
        IStoreContext storeContext,
        ISendcloudService sendcloudService,
        IEncryptionService encryptionService,
        SendcloudApiSettings sendcloudApiSettings)
    {
        _logger = logger;
        _orderService = orderService;
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
        _webHelper = webHelper;
        _shipmentService = shipmentService;
        _orderProcessingService = orderProcessingService;
        _genericAttributeService = genericAttributeService;
        _workContext = workContext;
        _storeContext = storeContext;
        _sendcloudService = sendcloudService;
        _encryptionService = encryptionService;
        _sendcloudApiSettings = sendcloudApiSettings;
    }

    #endregion

    #region Sendcloud

    public async Task<IActionResult> GetShippingAddress(string billingCountry = null, string zipCode = null, int orderId = 0, bool requestChecksum = false)
    {

        if (orderId != 0 && requestChecksum)
        {
            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order != null)
            {
                var checksum = _encryptionService.EncryptText(order.CustomOrderNumber);
                return Json(new { Checksum = checksum });
            }
        }
        else
        {
            var countryISOCode = string.Empty;
            var postalCode = string.Empty;

            if (billingCountry == null)
            {
                var store = await _storeContext.GetCurrentStoreAsync();
                var customer = await _workContext.GetCurrentCustomerAsync();
                if (store != null && customer != null && customer.ShippingAddressId.HasValue)
                {
                    var shippingOption =await _genericAttributeService.GetAttributeAsync<ShippingOption>(customer, NopCustomerDefaults.SelectedShippingOptionAttribute, store.Id);
                    var shippingAddress = await _addressService.GetAddressByIdAsync(customer.ShippingAddressId.Value);
                    if (shippingAddress != null && shippingAddress.CountryId.HasValue)
                    {
                        var country = await _countryService.GetCountryByIdAsync(shippingAddress.CountryId.Value);
                        if (country != null)
                        {
                            countryISOCode = country.TwoLetterIsoCode;
                            postalCode = shippingAddress.ZipPostalCode;
                        }
                    }
                }
            }
            else
            {
                var country = (await _countryService.GetAllCountriesAsync(showHidden: false)).Where(c => c.Name == billingCountry).FirstOrDefault();
                if (country != null)
                {
                    countryISOCode = country.TwoLetterIsoCode;
                    postalCode = zipCode;
                }

            }

            string changeServicePoint = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Sendcloud.ChangeServicePoint");
            string postOfficeBox = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Sendcloud.PostBox");

            return Json(new
            {
                SSISMapiKey = _sendcloudApiSettings.ApiKey,
                ShippingcountryISOCode = countryISOCode,
                ShippingpostalCode = postalCode,
                ChangeSPaddress = changeServicePoint,
                PostOfficeBox = postOfficeBox
            });
        }

        return Ok();
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public virtual async Task<IActionResult> SendcloudWebhook()
    {
        string message = "";
        Order order = null;
        var webhookEvent = new WebhookEvent();

        webhookEvent.Action = "No Action";

        if (_shippingManagerSettings.TestMode)
        {
            message = "Sendcloud Webhook " + HttpContext.Request.Method.ToString();
            await _logger.InsertLogAsync(LogLevel.Debug, message);
        }

        if (HttpContext.Request.Method == "POST")
        {

            if (_shippingManagerSettings.TestMode)
            {
                message = "Sendcloud Webhook Post Method";
                await _logger.InsertLogAsync(LogLevel.Debug, message);
            }

            if (Request == null)
            {
                message = "Sendcloud Webhook Post Method Request is null";
                await _logger.InsertLogAsync(LogLevel.Debug, message);
            }
            else
            {

                message = "Sendcloud Webhook Post Method Request is not null";
                await _logger.InsertLogAsync(LogLevel.Debug, message);

                try
                {
                    //parse JSON from response

                    using (var streamReader = new StreamReader(Request.Body))
                    {
                        var json = streamReader.ReadToEnd();
                        if (!string.IsNullOrEmpty(json))
                        {
                            message = "Sendcloud Webhook Post Method Request : " + json.ToString();
                            await _logger.InsertLogAsync(LogLevel.Debug, message);

                            webhookEvent = JsonConvert.DeserializeObject<WebhookEvent>(json);
                        }
                    }
                }
                catch (Exception ex)
                {
                    message = "Sendcloud Webhook Decode JSON Post : Exception " + ex.Message;
                    await _logger.InsertLogAsync(LogLevel.Error, message);
                    await _logger.ErrorAsync(ex.Message, ex);
                }
            }

            int orderId = 0;
            int shippingId = 0;
            Nop.Core.Domain.Shipping.Shipment shipment = null;

            if (webhookEvent != null)
            {

                    if (_shippingManagerSettings.TestMode)
                    {
                        message = "Sendcloud Webhook Action: " + webhookEvent.Action;
                        await _logger.InsertLogAsync(LogLevel.Debug, message);
                    }

                switch (webhookEvent.Action)
                {
                    case "parcel_status_changed":

                            if (_shippingManagerSettings.TestMode)
                            {
                                message = "Sendcloud Webhook - Shipment Order No: " + webhookEvent.Parcel.OrderNumber;
                                await _logger.InsertLogAsync(LogLevel.Debug, message);
                            }

                            var orderNumber = webhookEvent.Parcel.OrderNumber.Split('-');

                            if (orderNumber.Length == 2)
                            {
                                if (int.TryParse(orderNumber[1], out shippingId))
                                {
                                    shipment = await _shipmentService.GetShipmentByIdAsync(shippingId);
                                    if (shipment != null)
                                    {
                                        if (shipment.TrackingNumber != null)
                                        {
                                            if (shipment.TrackingNumber == webhookEvent.Parcel.TrackingNumber)
                                            {
                                                if (int.TryParse(orderNumber[0], out orderId))
                                                {
                                                    message = "Sendcloud Webhook - Decoded Shipment Order No: " + orderId.ToString();
                                                    await _logger.InsertLogAsync(LogLevel.Debug, message);
                                                }
                                                else if (_shippingManagerSettings.TestMode)
                                                {
                                                    message = "Sendcloud Webhook - Error decoding Shipment Order No: " + webhookEvent.Parcel.OrderNumber;
                                                    await _logger.InsertLogAsync(LogLevel.Debug, message);
                                                }
                                            }
                                            else if (_shippingManagerSettings.TestMode)
                                            {
                                                message = "Sendcloud Webhook - Tracking Number does not match Shipment " +
                                                    shipment.TrackingNumber + " - " + webhookEvent.Parcel.TrackingNumber;
                                                await _logger.InsertLogAsync(LogLevel.Debug, message);
                                            }
                                        }
                                        else if (_shippingManagerSettings.TestMode)
                                        {
                                            message = "Sendcloud Webhook - Tracking Number is Not Available using Order Number: " + orderNumber[0];
                                            await _logger.InsertLogAsync(LogLevel.Debug, message);

                                            if (!int.TryParse(orderNumber[0], out orderId))
                                            {
                                                message = "Sendcloud Webhook - Error Decoding Order Number: " + orderNumber[0];
                                                await _logger.InsertLogAsync(LogLevel.Debug, message);
                                            }
                                        }
                                    }
                                    else if (_shippingManagerSettings.TestMode)
                                    {
                                        message = "Sendcloud Webhook - Shipment Not found for Id: " + shippingId.ToString();
                                        await _logger.InsertLogAsync(LogLevel.Debug, message);
                                    }
                                }
                            }
                            else if (_shippingManagerSettings.TestMode)
                            {
                                message = "Sendcloud Webhook - Shipment Order Format Error: " + webhookEvent.Parcel.OrderNumber;
                                await _logger.InsertLogAsync(LogLevel.Debug, message);                            
                            }

                        break;

                    case "return_delivered":

                        if (_shippingManagerSettings.TestMode)
                        {
                            message = "Sendcloud Webhook - Return Delivered Action";
                            await _logger.InsertLogAsync(LogLevel.Debug, message);
                        }

                        break;

                    default:

                            if (_shippingManagerSettings.TestMode)
                            {
                                message = "Sendcloud Webhook - No Action Coded for: " + webhookEvent.Action;
                                await _logger.InsertLogAsync(LogLevel.Debug, message);
                            }

                        break;

                }

                if (orderId != 0)
                {
                    //Load order by identifier (if provided)

                    if (_shippingManagerSettings.TestMode)
                    {
                        message = "Sendcloud Webhook - Order " + orderId.ToString();
                        await _logger.InsertLogAsync(LogLevel.Debug, message);
                    }

                    order = await _orderService.GetOrderByIdAsync(orderId);
                }

                if (order == null || order.Deleted)
                {
                    //Order not active or deleted

                        if (_shippingManagerSettings.TestMode)
                        {
                            message = "Sendcloud Webhook - Order for Transaction " + orderId + " Not Found or Deleted";
                            await _logger.InsertLogAsync(LogLevel.Debug, message);
                        }
                    }
                    else
                    {
                        message = "Order Shipment Status updated By Sendcloud to " + webhookEvent.Parcel.Status.Message;

                    await _orderService.InsertOrderNoteAsync(new OrderNote
                    {
                        OrderId = order.Id,
                        Note = message,
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow
                    });

                    switch (webhookEvent.Parcel.Status.Message)
                    {
                        case "At Customs":
                        case "At sorting centre":
                        case "Awaiting customer pickup":
                        case "Being sorted":
                        case "Driver en route":
                        case "En route to sorting center":
                        case "Not sorted":
                        case "Parcel en route":
                        case "Ready to Send":
                        case "Shipment picked up by driver":
                        case "Sorted":
                        case "Unable to deliver":

                            if (order.ShippingStatus != ShippingStatus.Shipped)
                                await _orderProcessingService.ShipAsync(shipment, true);
                            break;

                        case "Announced":
                        case "Announced: not collected":
                        case "Being announced":
                        case "Ready to send":

                            if (order.ShippingStatus != ShippingStatus.Shipped && _shippingManagerSettings.SetAsShippedWhenAnnounced)
                                await _orderProcessingService.ShipAsync(shipment, true);
                            break;

                        case "Delivered":
                        case "Shipment collected by customer":

                            if (order.ShippingStatus != ShippingStatus.Delivered)
                                await _orderProcessingService.DeliverAsync(shipment, true);
                            break;
                    }

                    if (_shippingManagerSettings.TestMode)
                    {
                        message = "Sendcloud Webhook - Order for Transaction " + orderId + " Status: " + webhookEvent.Parcel.Status.Id + " Message: " + webhookEvent.Parcel.Status.Message;
                        _logger.InsertLog(LogLevel.Information, message);
                    }

                    await _orderService.UpdateOrderAsync(order);

                        
                    }
                }
            }

            return Ok();
        }

    [IgnoreAntiforgeryToken]
    public virtual async Task<IActionResult> SaveServicePointWebhook(int? orderId, int? spid, string? spcarrier, string? spaddress, string? splat, string? splong, int? spponumber, string? checksum)
    {
        //&orderid=5079&spid=10805976&spcarrier=postnl&splat=51.495078&splong=4.290596&spponumber=x&guid={order.OrderGuid}

        string message = "";
        Order order = null;

        if (_shippingManagerSettings.TestMode)
        {
            message = "Sendcloud Save Service Point Webhook > Raw Url : " + _webHelper.GetRawUrl(HttpContext.Request);
            _logger.InsertLog(LogLevel.Debug, message);
        }

        if (orderId.HasValue)
        {
            if (_shippingManagerSettings.TestMode)
            {
                message = "Sendcloud Save Service Point Webhook > Order Id: " + orderId.ToString();
                _logger.InsertLog(LogLevel.Debug, message);
            }
        }

        if (spid.HasValue && spcarrier != null)
        {
            if (_shippingManagerSettings.TestMode)
            {
                message = "Sendcloud Save Service Point Webhook > Service Point Id: " + spid.ToString() + " Carrier: " + spcarrier;
                _logger.InsertLog(LogLevel.Debug, message);
            }
        }


            if (spid.HasValue && spcarrier != null)
            {
                if (_shippingManagerSettings.TestMode)
                {
                    message = "Sendcloud Save Service Point Webhook > Service Point Id: " + spid.ToString() + " Carrier: " + spcarrier;
                    _logger.InsertLog(LogLevel.Debug, message);
                }
            }

            if (spaddress != null)
                spaddress = WebUtility.HtmlDecode(spaddress);

            if (!string.IsNullOrEmpty(splat) && !string.IsNullOrEmpty(splong))
            {
                var numberFormat = CultureInfo.GetCultureInfo((await _workContext.GetWorkingLanguageAsync()).LanguageCulture).NumberFormat;
                if (splat.Contains("."))
                    splat = splat.Replace(".", numberFormat.NumberDecimalSeparator);
                if (splong.Contains("."))
                    splong = splong.Replace(".", numberFormat.NumberDecimalSeparator);
                if (_shippingManagerSettings.TestMode)
                {
                    message = "Sendcloud Save Service Point Webhook > Service Point SPlat: " + splat.ToString() + " SPlong: " + splong.ToString();
                    _logger.InsertLog(LogLevel.Debug, message);
                }
            }

        if (spponumber.HasValue)
        {
            if (_shippingManagerSettings.TestMode)
            {
                message = "Sendcloud Save Service Point Webhook > Service Point SPPONumber: " + spponumber.ToString();
                _logger.InsertLog(LogLevel.Debug, message);
            }
        }

            if (spid.HasValue)
            {
                if (orderId.HasValue)
                {
                    order = await _orderService.GetOrderByIdAsync(orderId.Value);
                    if (order == null)
                    {
                        if (_shippingManagerSettings.TestMode)
                        {
                            message = "Sendcloud Save Service Point Webhook > Order is Not Found: " + orderId.Value.ToString();
                            _logger.InsertLog(LogLevel.Information, message);
                        }
                    }
                    else if (order.Deleted)
                    {
                        if (_shippingManagerSettings.TestMode)
                        {
                            message = "Sendcloud Save Service Point Webhook > Order is Deleted: " + order.Id.ToString();
                            _logger.InsertLog(LogLevel.Information, message);
                        }
                    }
                    try
                    {
                        string encrypt = order.CustomOrderNumber;
                        if (_shippingManagerSettings.EncryptServicePointPost)
                            encrypt = _encryptionService.EncryptText(order.OrderGuid.ToString());

                        checksum = WebUtility.HtmlDecode(checksum);

                    if (encrypt != checksum)
                    {
                        if (_shippingManagerSettings.TestMode)
                        {
                            message = "Sendcloud Save Service Point Webhook > Order is Invalid or Security Compromised " + order.Id.ToString();
                            _logger.InsertLog(LogLevel.Information, message);
                        }
                    }
                    else
                    {
                        var store = await _storeContext.GetCurrentStoreAsync();
                        var customer = await _workContext.GetCurrentCustomerAsync();
                        if (customer != null && store != null)
                        {
                            var sendCloudSelectedServicePoint = await _genericAttributeService.GetAttributeAsync<string>(customer, ShippingManagerDefaults.SendCloudSelectedServicePoint, store.Id);
                            sendCloudSelectedServicePoint = spid.ToString() +
                                (spcarrier != null ? ";carrier: " + spcarrier.ToString() : null) +
                                (spaddress != null ? ";address: " + spaddress.ToString() : null) +
                                (splat != null ? ";splat: " + splat.ToString() : null) +
                                (splong != null ? ";splat: " + splat.ToString() : null) +
                                (spponumber.HasValue ? ";spponumber: " + spponumber.Value.ToString() : null);
                            await _genericAttributeService.SaveAttributeAsync<string>(customer, ShippingManagerDefaults.SendCloudSelectedServicePoint,
                                "SendCloudSelectedServicePoint:" + sendCloudSelectedServicePoint, store.Id);
                            if (_shippingManagerSettings.TestMode)
                            {
                                message = "Sendcloud Save Service Point Webhook > Service Point Saved: " + sendCloudSelectedServicePoint;
                                _logger.InsertLog(LogLevel.Information, message);
                            }

                            var customValues = new Dictionary<string, object>();
                            var newCustomValues = new Dictionary<string, object>();

                            // Add new values
                            string lrs = await _localizationService.GetResourceAsync("Service Point Id");
                            customValues.Add(lrs, spid);
                            lrs = await _localizationService.GetResourceAsync("Service Point Carrier");
                            customValues.Add(lrs, spcarrier);
                            lrs = await _localizationService.GetResourceAsync("Service Point Address");
                            customValues.Add(lrs, spaddress);
                            lrs = await _localizationService.GetResourceAsync("Service Point Lat");
                            customValues.Add(lrs, splat);
                            lrs = await _localizationService.GetResourceAsync("Service Point Long");
                            customValues.Add(lrs, splong);
                            if (spponumber != null)
                            {
                                lrs = await _localizationService.GetResourceAsync("Service Point PO Number");
                                customValues.Add(lrs, spponumber.Value.ToString());
                            }

                            // Get existing values
                            var customValuesXml = _sendcloudService.DeserializeCustomValues(order);
                            foreach (var key in customValuesXml)
                            {
                                if (!customValues.ContainsKey(key.Key))
                                    newCustomValues.Add(key.Key, key.Value);
                            }

                            // Add new values
                            lrs = await _localizationService.GetResourceAsync("Service Point Id");
                            newCustomValues.Add(lrs, spid.ToString());
                            lrs = await _localizationService.GetResourceAsync("Service Point Carrier");
                            newCustomValues.Add(lrs, spcarrier);
                            lrs = await _localizationService.GetResourceAsync("Service Point Address");
                            newCustomValues.Add(lrs, spaddress);
                            lrs = await _localizationService.GetResourceAsync("Service Point Lat");
                            newCustomValues.Add(lrs, splat);
                            lrs = await _localizationService.GetResourceAsync("Service Point Long");
                            newCustomValues.Add(lrs, splong);
                            if (spponumber != null)
                            {
                                lrs = await _localizationService.GetResourceAsync("Service Point PO Number");
                                newCustomValues.Add(lrs, spponumber.Value.ToString());
                            }

                            order.CustomValuesXml = _sendcloudService.SerializeCustomValues(newCustomValues);

                            await _orderService.UpdateOrderAsync(order);

                        }
                    }
                }
                catch (Exception exc)
                {
                    message = "Sendcloud Save Service Point Webhook > Error Decoding values: " + exc.Message;
                    _logger.InsertLog(LogLevel.Information, message);
                }
            }

        }

        return Ok();
    }

    #endregion
}
