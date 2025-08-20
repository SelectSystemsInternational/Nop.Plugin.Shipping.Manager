using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Logging;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Services.Attributes;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Pickup;

using Nop.Plugin.Shipping.Manager.Domain;
using Nop.Plugin.Shipping.Manager.Settings;

using Nop.Plugin.Apollo.Integrator.Domain;
using Nop.Plugin.Apollo.Integrator.Services;

using CanadaPostApi.Api;
using CanadaPostApi.Exceptions;

namespace Nop.Plugin.Shipping.Manager.Services;

/// <summary>
/// Shipping service
/// </summary>
public partial class CanadaPostService : ICanadaPostService
{

    private const int MAX_DEAD_WEIGHT = 30; // 30 Kg

    #region Fields

    protected readonly IAddressService _addressService;
    protected readonly IAttributeParser<CheckoutAttribute, CheckoutAttributeValue> _checkoutAttributeParser;
    protected readonly IGenericAttributeService _genericAttributeService;
    protected readonly ILocalizationService _localizationService;
    protected readonly ILogger _logger;
    protected readonly IPickupPluginManager _pickupPluginManager;
    protected readonly IPriceCalculationService _priceCalculationService;
    protected readonly IProductAttributeParser _productAttributeParser;
    protected readonly IProductService _productService;
    protected readonly IRepository<ShippingMethod> _shippingMethodRepository;
    protected readonly IRepository<Carrier> _carrierRepository;
    protected readonly IStoreContext _storeContext;
    protected readonly ShippingSettings _shippingSettings;
    protected readonly ShoppingCartSettings _shoppingCartSettings;
    protected readonly IEntityGroupService _entityGroupService;
    protected readonly IRepository<EntityGroup> _entityGroupRepository;
    protected readonly IWorkContext _workContext;
    protected readonly IRepository<CutOffTime> _cutOffTimeRepository;
    protected readonly ICarrierService _carrierService;
    protected readonly ICountryService _countryService;
    protected readonly IStateProvinceService _stateProvinceService;
    protected readonly CanadaPostApiSettings _canadaPostApiSettings;
    protected readonly ShippingManagerSettings _shippingManagerSettings;
    protected readonly IShippingAddressService _shippingAddressService;
    protected readonly IRepository<Address> _addressRepository;
    protected readonly ISettingService _settingService;
    protected readonly IShipmentService _shipmentService;
    protected readonly IPriceFormatter _priceFormatter;
    protected readonly IOrderService _orderService;
    protected readonly IMeasureService _measureService;
    protected readonly IShipmentDetailsService _shipmentDetailsService;
    protected readonly IPackagingOptionService _packagingOptionService;
    protected readonly MeasureSettings _measureSettings;
    protected readonly INopFileProvider _fileProvider;

    #endregion

    #region Ctor

    public CanadaPostService(IAddressService addressService,
        IAttributeParser<CheckoutAttribute, CheckoutAttributeValue> checkoutAttributeParser,
        IGenericAttributeService genericAttributeService,
        ILocalizationService localizationService,
        ILogger logger,
        IPickupPluginManager pickupPluginManager,
        IPriceCalculationService priceCalculationService,
        IProductAttributeParser productAttributeParser,
        IProductService productService,
        IRepository<ShippingMethod> shippingMethodRepository,
        IRepository<Carrier> carrierRepository,
        IStoreContext storeContext,
        ShippingSettings shippingSettings,
        ShoppingCartSettings shoppingCartSettings,
        IEntityGroupService entityGroupService,
        IWorkContext workContext,
        IRepository<CutOffTime> cutOffTimeRepository,
        IRepository<EntityGroup> entityGroupRepository,
        ICarrierService carrierService,
        ICountryService countryService,
        IStateProvinceService stateProvinceService,
        CanadaPostApiSettings canadaPostApiSettings,
        ShippingManagerSettings shippingManagerSettings,
        IShippingAddressService shippingAddressService,
        IRepository<Address> addressRepository, 
        ISettingService settingService,
        IShipmentService shipmentService,
        IPriceFormatter priceFormatter,
        IOrderService orderService,
        IMeasureService measureService,
        IShipmentDetailsService shipmentDetailsService,
        IPackagingOptionService packagingOptionService,
        MeasureSettings measureSettings,
        INopFileProvider fileProvider)
    {
        _addressService = addressService;
        _checkoutAttributeParser = checkoutAttributeParser;
        _genericAttributeService = genericAttributeService;
        _localizationService = localizationService;
        _logger = logger;
        _pickupPluginManager = pickupPluginManager;
        _priceCalculationService = priceCalculationService;
        _productAttributeParser = productAttributeParser;
        _productService = productService;
        _shippingMethodRepository = shippingMethodRepository;
        _carrierRepository = carrierRepository;
        _storeContext = storeContext;
        _shippingSettings = shippingSettings;
        _shoppingCartSettings = shoppingCartSettings;
        _entityGroupService = entityGroupService;
        _workContext = workContext;
        _cutOffTimeRepository = cutOffTimeRepository;
        _entityGroupRepository = entityGroupRepository;
        _carrierService = carrierService;
        _countryService = countryService;
        _stateProvinceService = stateProvinceService;
        _canadaPostApiSettings = canadaPostApiSettings;
        _shippingManagerSettings = shippingManagerSettings;
        _shippingAddressService = shippingAddressService;
        _addressRepository = addressRepository;
        _settingService = settingService;
        _shipmentService = shipmentService;
        _priceFormatter = priceFormatter;
        _orderService = orderService;
        _measureService = measureService;
        _shipmentDetailsService = shipmentDetailsService;
        _packagingOptionService = packagingOptionService;
        _measureSettings = measureSettings;
        _fileProvider = fileProvider;
    }

    #endregion

    #region Utilities

    #endregion

    #region Methods

    /// <summary>
    /// Update canada post shipment rate configuration
    /// </summary>
    /// <param name="client">CanadaPostApi client</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// </returns>
    public async Task CanadaPostUpdateAsync()
    {

        var customer = await _workContext.GetCurrentCustomerAsync();
        var storeId = await _entityGroupService.GetActiveStoreScopeConfiguration();

        int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

        var shippingManagerService = EngineContext.Current.Resolve<IShippingManagerService>();
        var shippingService = EngineContext.Current.Resolve<IShippingService>();

        string responseAsString = String.Empty;

        string useragent = "Canada Post SDK; .NetStandard 2.1;";

        var authorizationApi = new AuthorizationApi(_canadaPostApiSettings.AuthenticationURL, useragent);

        authorizationApi.Configuration.Username = _canadaPostApiSettings.Username;
        authorizationApi.Configuration.Password = _canadaPostApiSettings.Password;
        authorizationApi.Configuration.CustomerNumber = _canadaPostApiSettings.CustomerNumber;
        authorizationApi.Configuration.MoBoCN = _canadaPostApiSettings.CustomerNumber;
        authorizationApi.Configuration.ContractId = _canadaPostApiSettings.ContractId;

        var response = authorizationApi.AuthorizationCreateConfiguration();

        RatesApi instance = new RatesApi(authorizationApi.Configuration);

        ////Get the Canada Post carriers list 
        var carrier = await _carrierService.GetCarrierBySystemNameAsync(ShippingManagerDefaults.CanadaPostSystemName);
        if (carrier == null)
        {
            carrier = new Carrier();
            carrier.Name = "Canada Post";
            carrier.AdminComment = "Carrier added from Canada Post Update";
            carrier.ShippingRateComputationMethodSystemName = ShippingManagerDefaults.CanadaPostSystemName;

            carrier.AddressId = (await _shippingAddressService.CreateAddressAsync("The", "Manager", string.Empty, carrier.Name, 0, 0, string.Empty,
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty)).Id;
            carrier.Active = true;

            await _carrierService.InsertCarrierAsync(carrier);
        }

        var availableServices = instance.GetServices(null, out string errors);

        //  Save the Canada Post shipping methods list for sender address 
        int displayOrder = 1;

        // Retrieve values from services object
        foreach (var service in availableServices.service)
        {
            responseAsString += "Service Name: " + service.servicename + "\r\n";
            responseAsString += "Service Code: " + service.servicecode + "\r\n";
            responseAsString += "Href: " + service.link.href + "\r\n\r\n";

            var shippingMethod = await shippingManagerService.GetShippingMethodByNameAsync(service.servicename);
            if (shippingMethod == null)
            {
                shippingMethod = new ShippingMethod();
                shippingMethod.Name = service.servicename;
                shippingMethod.Description = "Service Code: " + service.servicecode;
                shippingMethod.DisplayOrder = displayOrder++;

                await shippingService.InsertShippingMethodAsync(shippingMethod);

                var countryId = _shippingAddressService.GetCountryIdFromCodeAsync("CA"); // Canada
                var shippingManagerByWeightByTotal = (await shippingManagerService.GetRecordsAsync(shippingMethod.Id, storeId,
                    vendorId, 0, carrier.Id, 0, 0, "")).FirstOrDefault();

                if (shippingManagerByWeightByTotal == null)
                {
                    shippingManagerByWeightByTotal = new ShippingManagerByWeightByTotal();
                    shippingManagerByWeightByTotal.ShippingMethodId = shippingMethod.Id;
                    shippingManagerByWeightByTotal.CarrierId = carrier.Id;
                    shippingManagerByWeightByTotal.WarehouseId = 0;
                    shippingManagerByWeightByTotal.VendorId = vendorId;
                    shippingManagerByWeightByTotal.WeightFrom = 0;
                    shippingManagerByWeightByTotal.WeightTo = MAX_DEAD_WEIGHT;
                    shippingManagerByWeightByTotal.CalculateCubicWeight = false;
                    shippingManagerByWeightByTotal.CubicWeightFactor = 0;
                    shippingManagerByWeightByTotal.OrderSubtotalFrom = 0;
                    shippingManagerByWeightByTotal.OrderSubtotalTo = 1000000;
                    shippingManagerByWeightByTotal.CountryId = await _shippingAddressService.GetCountryIdFromCodeAsync("CA");
                    shippingManagerByWeightByTotal.StateProvinceId = 0;
                    shippingManagerByWeightByTotal.FriendlyName = string.Empty;
                    shippingManagerByWeightByTotal.TransitDays = null;

                    await shippingManagerService.InsertShippingByWeightRecordAsync(shippingManagerByWeightByTotal);
                }
            }
        }

        if (_canadaPostApiSettings.TestMode)
        {
            string message = "Shipping Manager - Sendcloud Update > Shipping method list: " + responseAsString;
            await _logger.InsertLogAsync(LogLevel.Debug, message);
        }

    }

    /// <summary>
    /// Create a canada post shipment
    /// </summary>
    /// <param name="shipment">Shipment</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// </returns>
    public async Task<bool> CanadaPostCreateShipmentAsync(Nop.Core.Domain.Shipping.Shipment orderShipment)
    {

        string errors = string.Empty;
        List<string> errorList = new List<string>();

        if (orderShipment == null)
            return false;

        var order = await _orderService.GetOrderByIdAsync(orderShipment.OrderId);
        if (order == null)
            return false;

        if (_canadaPostApiSettings.TestMode)
        {
            string message = "Shipping Manager - Canada Post Create Shipment > For Shipment Order: " +
                order.CustomOrderNumber + "-" + orderShipment.Id.ToString();
            await _logger.InsertLogAsync(LogLevel.Debug, message);
        }

        string responseAsString = String.Empty;

        string useragent = "Canada Post SDK; .NetStandard 2.1;";

        var authorizationApi = new AuthorizationApi(_canadaPostApiSettings.AuthenticationURL, useragent);

        authorizationApi.Configuration.Username = _canadaPostApiSettings.Username;
        authorizationApi.Configuration.Password = _canadaPostApiSettings.Password;
        authorizationApi.Configuration.CustomerNumber = _canadaPostApiSettings.CustomerNumber;
        authorizationApi.Configuration.MoBoCN = _canadaPostApiSettings.MoBoCN;
        authorizationApi.Configuration.ContractId = _canadaPostApiSettings.ContractId;

        var response = authorizationApi.AuthorizationCreateConfiguration();

        RatesApi ratesApi = new RatesApi(authorizationApi.Configuration);
        ShippingApi shippingApi = new ShippingApi(authorizationApi.Configuration);

        string canadaPostShippingMethodName = order.ShippingMethod;
        string canadaPostServiceCode = string.Empty;
        if (canadaPostShippingMethodName != null)
        {
            var shippingMethod = await GetShippingMethodFromFriendlyNameAsync(canadaPostShippingMethodName);
            if (shippingMethod != null)
            {

                if (_canadaPostApiSettings.TestMode)
                {
                    string message = "Shipping Manager - Canada Post Create Shipment > Get Shipping Method from Friendly Name: " + canadaPostShippingMethodName +
                        " Shipping Method: " + shippingMethod.Name;
                    await _logger.InsertLogAsync(LogLevel.Debug, message);
                }

                canadaPostShippingMethodName = shippingMethod.Name;

            }
            else
            {
                if (_canadaPostApiSettings.TestMode)
                {
                    string message = "Shipping Manager - Canada Post Create Shipment > Shipping Method: " + canadaPostShippingMethodName;
                    await _logger.InsertLogAsync(LogLevel.Debug, message);
                }
            }
        }

        if (canadaPostShippingMethodName != null)
        {

            var availableServices = ratesApi.GetServices(null, out errors);
            if (availableServices != null)
            {

                if (_shippingManagerSettings.TestMode || _canadaPostApiSettings.TestMode)
                {
                    string message = "Shipping Manager - Canada Post Create Shipment > Shipping Methods: " + availableServices.ToString();
                    await _logger.InsertLogAsync(LogLevel.Debug, message);
                }

                // Retrieve values from services object
                foreach (var service in availableServices.service)
                {
                    if (canadaPostShippingMethodName.Contains(service.servicename))
                    {
                        canadaPostServiceCode = service.servicecode;
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(canadaPostServiceCode))
                {
                    if (_canadaPostApiSettings.TestMode)
                    {
                        string message = "Shipping Manager - Canada Post Create Shipment > Shipping Method " + canadaPostShippingMethodName + " Service Code found: " + canadaPostServiceCode;
                        await _logger.InsertLogAsync(LogLevel.Debug, message);
                    }
                }

            }

            if (!string.IsNullOrEmpty(errors))
            {
                string message = "Shipping Manager - Canada Post Create Shipment > Error : " + errors;
                await _logger.InsertLogAsync(LogLevel.Debug, message);
                errorList.Add(errors);
            }

            if (string.IsNullOrEmpty(canadaPostServiceCode) || errorList.Count() > 0)
            {
                return false;
            }

            //get the settings default Adddress 

            int shippingOriginAddressId = _shippingSettings.ShippingOriginAddressId;
            var shippingOriginAddress = await _addressService.GetAddressByIdAsync(shippingOriginAddressId);
            if (shippingOriginAddress == null)
            {
                throw new CanadaPostException("Shipping Settings - Shipping Origin not Set");
            }
            else if (!shippingOriginAddress.CountryId.HasValue)
            {
                throw new CanadaPostException("Shipping Settings - Shipping Origin not Set");
            }
            else
            {
                string shippingOriginCountryCode = "CA";
                string shippingOriginStateCode = "ON";
                var shippingOriginCountry = await _countryService.GetCountryByIdAsync(shippingOriginAddress.CountryId.Value);
                if (shippingOriginCountry != null)
                {
                    //Get the senders address

                    if (_canadaPostApiSettings.TestMode)
                    {
                        string message = "Shipping Manager - Canada Post Create Shipment > Shipping Origin Address: " + shippingOriginAddressId.ToString() +
                            " City: " + shippingOriginAddress.City + " Country: " + shippingOriginCountry.TwoLetterIsoCode;
                        await _logger.InsertLogAsync(LogLevel.Debug, message);
                    }

                    shippingOriginCountryCode = shippingOriginCountry.TwoLetterIsoCode;
                    var states = await _stateProvinceService.GetStateProvincesByCountryIdAsync(shippingOriginAddress.CountryId.Value);
                    if (states != null)
                    {
                        var state = states.Where(s => s.Id == shippingOriginAddress.StateProvinceId).FirstOrDefault();
                        if (state != null)
                            shippingOriginStateCode = state.Abbreviation;
                    }
                }

                var shippingAddress = await _addressService.GetAddressByIdAsync(order.ShippingAddressId.Value);
                if (shippingAddress != null)
                {
                    string shippingAddressCountryCode = "CA";
                    string shippingAddressStateCode = "ON";
                    if (shippingAddress.CountryId.HasValue)
                    {
                        var shippingAddressCountry = await _countryService.GetCountryByIdAsync(shippingAddress.CountryId.Value);
                        if (shippingAddressCountry != null)
                        {
                            shippingAddressCountryCode = shippingAddressCountry.TwoLetterIsoCode;
                            var states = await _stateProvinceService.GetStateProvincesByCountryIdAsync(shippingAddress.CountryId.Value);
                            if (states != null)
                            {
                                var state = states.Where(s => s.Id == shippingAddress.StateProvinceId).FirstOrDefault();
                                if (state != null)
                                    shippingAddressStateCode = state.Abbreviation;
                            }
                        }
                    }

                    string aftifactUrl = String.Empty;

                    // Create shipment object to contain xml request
                    ShipmentType shipment = new ShipmentType();
                    shipment.deliveryspec = new DeliverySpecType();
                    shipment.deliveryspec.sender = new SenderType();
                    shipment.deliveryspec.sender.addressdetails = new AddressDetailsType();
                    shipment.deliveryspec.destination = new DestinationType();
                    shipment.deliveryspec.destination.addressdetails = new DestinationAddressDetailsType();
                    shipment.deliveryspec.options = new OptionType[1];
                    shipment.deliveryspec.options[0] = new OptionType();
                    shipment.deliveryspec.parcelcharacteristics = new ParcelCharacteristicsType();
                    shipment.deliveryspec.parcelcharacteristics.dimensions = new ParcelCharacteristicsTypeDimensions();
                    shipment.deliveryspec.notification = new NotificationType();
                    shipment.deliveryspec.printpreferences = new PrintPreferencesType();
                    shipment.deliveryspec.preferences = new PreferencesType();
                    shipment.deliveryspec.settlementinfo = new SettlementInfoType();
                    shipment.deliveryspec.references = new ReferencesType();

                    //Payment and contract options

                    if (string.IsNullOrEmpty(_canadaPostApiSettings.ContractId))
                    {
                        //No manifest shipment

                        shipment.deliveryspec.settlementinfo.intendedmethodofpayment = "CreditCard";
                    }
                    else
                    {
                        // Regular Contract Shipment

                        shipment.deliveryspec.settlementinfo.intendedmethodofpayment = "Account";
                        shipment.deliveryspec.settlementinfo.contractid = _canadaPostApiSettings.ContractId; // Sandbox Test = "42708517"
                    }

                    if (_shippingManagerSettings.ManifestShipments)
                    {
                        shipment.Item = (await _storeContext.GetCurrentStoreAsync()).Name.Replace(" ", "-") + "_Group";
                    }
                    else
                    { 
                        shipment.Item = true;
                    }

                    shipment.requestedshippingpoint = shippingOriginAddress.ZipPostalCode;
                    shipment.cpcpickupindicator = true;
                    shipment.cpcpickupindicatorSpecified = true;

                    shipment.expectedmailingdateSpecified = false;
                    if (shipment.expectedmailingdateSpecified)
                        shipment.expectedmailingdate = DateTime.Now.AddDays(1);

                    shipment.deliveryspec.sender.name = shippingOriginAddress.FirstName + " " + shippingOriginAddress.LastName;
                    shipment.deliveryspec.sender.company = shippingOriginAddress.Company;
                    shipment.deliveryspec.sender.addressdetails.addressline1 = shippingOriginAddress.Address1;
                    shipment.deliveryspec.sender.addressdetails.addressline2 = shippingOriginAddress.Address2;
                    shipment.deliveryspec.sender.addressdetails.city = shippingOriginAddress.City;
                    shipment.deliveryspec.sender.addressdetails.countrycode = shippingOriginCountryCode;
                    shipment.deliveryspec.sender.addressdetails.provstate = shippingOriginStateCode;
                    shipment.deliveryspec.sender.addressdetails.postalzipcode = shippingAddress.ZipPostalCode;
                    shipment.deliveryspec.sender.contactphone = shippingAddress.PhoneNumber;

                    shipment.deliveryspec.destination.name = shippingAddress.FirstName + " " + shippingAddress.LastName;
                    shipment.deliveryspec.destination.company = shippingAddress.Company;
                    shipment.deliveryspec.destination.addressdetails.addressline1 = shippingAddress.Address1;
                    shipment.deliveryspec.destination.addressdetails.addressline2 = shippingAddress.Address2;
                    shipment.deliveryspec.destination.addressdetails.city = shippingAddress.City;
                    shipment.deliveryspec.destination.addressdetails.countrycode = shippingAddressCountryCode;
                    shipment.deliveryspec.destination.addressdetails.provstate = shippingAddressStateCode;
                    shipment.deliveryspec.destination.addressdetails.postalzipcode = shippingAddress.ZipPostalCode;

                    var options = _canadaPostApiSettings.ShipmentOptions.Split(",");
                    int count = 0;
                    foreach(var option in options)
                        shipment.deliveryspec.options[count++].optioncode = option; // i.e. "DC" Delivery Confirmation

                    shipment.deliveryspec.parcelcharacteristics.weight = orderShipment.TotalWeight.Value;

                    var baseWeight = (await _measureService.GetMeasureWeightByIdAsync(_measureSettings.BaseWeightId))?.Name;
                    var baseDimension = (await _measureService.GetMeasureDimensionByIdAsync(_measureSettings.BaseDimensionId))?.Name;

                    if (_shippingManagerSettings.UsePackagingSystem)
                    {
                        var shipmentDetails = _shipmentDetailsService.GetShipmentDetailsForShipmentId(orderShipment.Id);
                        if (shipmentDetails != null)
                        {
                            var packagingOption = _packagingOptionService.GetSimplePackagingOptionById(shipmentDetails.PackagingOptionItemId);
                            if (packagingOption != null)
                            {

                                MeasureDimension usedMeasureDimension = await GatewayMeasureDimensionAsync();

                                shipment.deliveryspec.parcelcharacteristics.dimensions.length = await ConvertFromPrimaryMeasureDimensionAsync(packagingOption.Length, usedMeasureDimension);
                                shipment.deliveryspec.parcelcharacteristics.dimensions.height = await ConvertFromPrimaryMeasureDimensionAsync(packagingOption.Height, usedMeasureDimension);
                                shipment.deliveryspec.parcelcharacteristics.dimensions.width = await ConvertFromPrimaryMeasureDimensionAsync(packagingOption.Width, usedMeasureDimension);

                                string packageDimensions = $"{packagingOption.Length:F2} x {packagingOption.Width:F2} x {packagingOption.Height:F2} [{baseDimension}]";
                                string itemWeight = $"{shipment.deliveryspec.parcelcharacteristics.weight:F2} [{baseWeight}]";

                                if (_canadaPostApiSettings.TestMode)
                                {
                                    string message = "Shipping Manager - Canada Post Create Shipment > Using Packaging Dimensions : " + packageDimensions +
                                        " and Weight: " + itemWeight;
                                    await _logger.InsertLogAsync(LogLevel.Debug, message);
                                }
                            }
                        }
                    }
                    else
                    {
                        var shipmentItems = await _shipmentService.GetShipmentItemsByShipmentIdAsync(orderShipment.Id);
                        if (shipmentItems != null && shipmentItems.Count() > 0)
                        {
                            var item = shipmentItems.FirstOrDefault();
                            var orderItem = await _orderService.GetOrderItemByIdAsync(item.OrderItemId);
                            if (orderItem != null)
                            {
                                var product = await _productService.GetProductByIdAsync(orderItem.ProductId);
                                if (product != null)
                                {
                                    MeasureDimension usedMeasureDimension = await GatewayMeasureDimensionAsync();

                                    shipment.deliveryspec.parcelcharacteristics.dimensions.length = await ConvertFromPrimaryMeasureDimensionAsync(product.Length, usedMeasureDimension);
                                    shipment.deliveryspec.parcelcharacteristics.dimensions.height = await ConvertFromPrimaryMeasureDimensionAsync(product.Height, usedMeasureDimension);
                                    shipment.deliveryspec.parcelcharacteristics.dimensions.width = await ConvertFromPrimaryMeasureDimensionAsync(product.Width, usedMeasureDimension);

                                    string packageDimensions = $"{product.Length:F2} x {product.Width:F2} x {product.Height:F2} [{baseDimension}]";
                                    string itemWeight = $"{shipment.deliveryspec.parcelcharacteristics.weight:F2} [{baseWeight}]";

                                    if (_canadaPostApiSettings.TestMode)
                                    {
                                        string message = "Shipping Manager - Canada Post Create Shipment > Using Packaging Dimensions : " + packageDimensions +
                                            " and Weight: " + itemWeight;
                                        await _logger.InsertLogAsync(LogLevel.Debug, message);
                                    }
                                }
                            }
                        }
                    }

                    if (shipment.deliveryspec.parcelcharacteristics.dimensions.height == 0 ||
                        shipment.deliveryspec.parcelcharacteristics.dimensions.length == 0 ||
                        shipment.deliveryspec.parcelcharacteristics.dimensions.width == 0 || 
                        shipment.deliveryspec.parcelcharacteristics.weight == 0)
                    {
                        string packageDimensions = $"{shipment.deliveryspec.parcelcharacteristics.dimensions.length:F2}" +
                            $"x {shipment.deliveryspec.parcelcharacteristics.dimensions.width:F2}" +
                            $"x {shipment.deliveryspec.parcelcharacteristics.dimensions.height:F2} [{baseDimension}]";
                        string itemWeight = $"{shipment.deliveryspec.parcelcharacteristics.weight:F2} [{baseWeight}]";

                        if (_canadaPostApiSettings.TestMode)
                        {
                            string message = "Shipping Manager - Canada Post Create Shipment > Invalid Packaging Dimensions : " 
                                + packageDimensions + " and Weight: " + itemWeight;
                            await _logger.InsertLogAsync(LogLevel.Debug, message);
                        }
                    }

                    shipment.deliveryspec.notification.email = shippingOriginAddress.Email;
                    shipment.deliveryspec.notification.ondelivery = true;
                    shipment.deliveryspec.notification.onexception = false;
                    shipment.deliveryspec.notification.onshipment = true;

                    shipment.deliveryspec.printpreferences.outputformat = PrintPreferencesTypeOutputformat.Item85x11;
                    shipment.deliveryspec.printpreferences.outputformatSpecified = true;
                    shipment.deliveryspec.preferences.showinsuredvalue = true;
                    shipment.deliveryspec.preferences.showpackinginstructions = true;
                    shipment.deliveryspec.preferences.showpostagerate = true;

                    shipment.deliveryspec.references.costcentre = orderShipment.OrderId.ToString();
                    shipment.deliveryspec.references.customerref1 = "Shipment-" + orderShipment.Id.ToString();
                    shipment.deliveryspec.references.customerref2 = "Order-" + orderShipment.Id.ToString();
                    shipment.deliveryspec.servicecode = canadaPostServiceCode;

                    if (_canadaPostApiSettings.TestMode)
                    {
                        string message = "Shipping Manager - Canada Post Create Shipment > Service Method: " + shipment.deliveryspec.servicecode +
                            " Name: " + shipment.deliveryspec.destination.name +
                            " Address1: " + shipment.deliveryspec.destination.addressdetails.addressline1 +
                            " Address2: " + shipment.deliveryspec.destination.addressdetails.addressline2 +
                            " City: " + shipment.deliveryspec.destination.addressdetails.city +
                            " PostalCode: " + shipment.deliveryspec.destination.addressdetails.postalzipcode +
                            " CountryCode: " + shipment.deliveryspec.destination.addressdetails.countrycode +
                            " Province: " + shipment.deliveryspec.destination.addressdetails.provstate +
                            " Customerref1: " + shipment.deliveryspec.references.customerref1 +
                            " Customerref2: " + shipment.deliveryspec.references.customerref2 +
                            " Method of Payment: " + shipment.deliveryspec.settlementinfo.intendedmethodofpayment +
                            " Contract Id: " + shipment.deliveryspec.settlementinfo.contractid +
                            " Transmit: " + shipment.Item;

                        await _logger.InsertLogAsync(LogLevel.Debug, message);
                    }

                    try
                    {

                        bool transmitted = false;

                        // Retrieve values from shipmentInfo object

                        var shipmentInfo = shippingApi.CreateShipment(shipment, out errors);
                        if (shipmentInfo != null && string.IsNullOrEmpty(errors))
                        {
                            // Retrieve values from shipmentInfo object
                            responseAsString += "Shipment ID: " + shipmentInfo.shipmentid + "\r\n";
                            foreach (LinkType link in shipmentInfo.links)
                            {
                                responseAsString += link.rel + ":  " + link.href + "\r\n";
                                if (link.rel == "label")
                                    aftifactUrl = link.href;
                            }

                            // Save details

                            if (!string.IsNullOrEmpty(aftifactUrl) && string.IsNullOrEmpty(errors))
                            {
                                var orderShipmentDetails = _shipmentDetailsService.GetShipmentDetailsForShipmentId(orderShipment.Id);
                                if (orderShipmentDetails != null)
                                {
                                    orderShipmentDetails.LabelUrl = aftifactUrl;
                                    orderShipmentDetails.ShipmentId = shipmentInfo.shipmentid;
                                }

                                var shipmentDetails = shippingApi.GetShipmentDetails(shipmentInfo.shipmentid, out errors);
                                if (string.IsNullOrEmpty(errors))
                                {

                                    if (shipmentDetails.shipmentstatus == "transmitted")
                                        transmitted = true;

                                    // Retrieve values from ShipmentDetailsType object

                                    responseAsString += "\r\nTracking Pin: " + shipmentDetails.trackingpin + "\r\n";
                                    if (!shipmentDetails.shipmentdetail.Item.Equals(true))
                                    {
                                        orderShipmentDetails.Group = shipmentDetails.shipmentdetail.Item.ToString();
                                        responseAsString += "Group Id: " + shipmentDetails.shipmentdetail.Item + "\r\n";
                                    }
                                    else
                                    {
                                        responseAsString += "Transmit Shipment: " + shipmentDetails.shipmentdetail.Item + "\r\n";
                                    }

                                    responseAsString += "Sender Postal Code: " + shipmentDetails.shipmentdetail.deliveryspec.sender.addressdetails.postalzipcode + "\r\n";
                                    responseAsString += "Destination Postal Code: " + shipmentDetails.shipmentdetail.deliveryspec.destination.addressdetails.postalzipcode + "\r\n";

                                    var shipmentPrice = shippingApi.GetShipmentDetailsInformation<ShipmentPriceType>(shipmentInfo.shipmentid, "price", out errors);

                                    // Retrieve values from ShipmentPriceType object
                                    responseAsString += "Service Code: " + shipmentPrice.servicecode + "\r\n";
                                    responseAsString += "Due amount: " + shipmentPrice.dueamount + "\r\n\r\n";

                                    orderShipmentDetails.Cost = shipmentPrice.dueamount;

                                    await _shipmentDetailsService.UpdateShipmentDetailsAsync(orderShipmentDetails);

                                    orderShipment.TrackingNumber = shipmentDetails.trackingpin;

                                    await _shipmentService.UpdateShipmentAsync(orderShipment);

                                    string filePath = string.Empty;
                                    string fileName = string.Empty;

                                    (filePath, fileName, errors) = PrintArtifact(shippingApi, orderShipmentDetails.ShipmentId, orderShipmentDetails.LabelUrl);

                                    if (_canadaPostApiSettings.TestMode)
                                    {
                                        string message = "Shipping Manager - Canada Post Create Shipment > Label Created: " +
                                            "FilePath: " + filePath + "FileName: " + fileName;                                     
                                        await _logger.InsertLogAsync(LogLevel.Debug, message);
                                    }

                                    if (!string.IsNullOrEmpty(errors))
                                    {
                                        errorList.Add(errors);
                                    }

                                }
                                else
                                {
                                    errorList.Add(errors);
                                }
                            }
                            else
                            {
                                errorList.Add(errors);
                            }
                        }
                        else
                        {
                            errorList.Add(errors);
                        }

                        if (errorList.Count() == 0)
                        {
                            // Transmit

                            if(_shippingManagerSettings.ManifestShipments && !transmitted)
                            {

                                // Create transmit object to contain xml request
                                ShipmentTransmitSetType transmit = new ShipmentTransmitSetType();
                                transmit.groupids = new String[1];
                                transmit.manifestaddress = new ManifestAddressType();
                                transmit.manifestaddress.addressdetails = new ManifestAddressDetailsType();

                                // Populate transmit object
                                transmit.groupids[0] = shipment.Item.ToString();
                                transmit.requestedshippingpoint = shipment.requestedshippingpoint;
                                transmit.cpcpickupindicator = true;
                                transmit.cpcpickupindicatorSpecified = true;
                                transmit.detailedmanifests = true;
                                transmit.methodofpayment = shipment.deliveryspec.settlementinfo.intendedmethodofpayment;
                                transmit.manifestaddress.manifestcompany = shipment.deliveryspec.sender.company;
                                transmit.manifestaddress.phonenumber = shipment.deliveryspec.sender.contactphone;
                                transmit.manifestaddress.addressdetails.addressline1 = shipment.deliveryspec.sender.addressdetails.addressline1;
                                transmit.manifestaddress.addressdetails.addressline2 = shipment.deliveryspec.sender.addressdetails.addressline2;
                                transmit.manifestaddress.addressdetails.city = shipment.deliveryspec.sender.addressdetails.city;
                                transmit.manifestaddress.addressdetails.provstate = shipment.deliveryspec.sender.addressdetails.provstate;
                                transmit.manifestaddress.addressdetails.postalzipcode = shipment.deliveryspec.sender.addressdetails.postalzipcode;

                                // Retrieve values from shipmentInfo object
                                var manifests = shippingApi.TransmitShipments(transmit, out errors);
                                if (manifests != null)
                                {
                                    // Retrieve values from manifests object
                                    foreach (LinkType link in manifests.link)
                                    {
                                        responseAsString += link.rel + ":  " + link.href + "\r\n";
                                    }
                                }
                            }
                        }

                    }
                    catch (CanadaPostException exception)
                    {
                        if (exception.Message == "")
                        {
                            if (_canadaPostApiSettings.TestMode)
                            {
                                string message = "Shipping Manager - Canada Post Create Shipment > Parcel Created Exception: " + "Invalid Shipping Method";
                                await _logger.InsertLogAsync(LogLevel.Debug, message);
                            }

                            throw new CanadaPostException("Parcel Create Exception: " + "Invalid Shipping Method");
                        }
                        else
                        {
                            string message = "Shipping Manager - Canada Post Create Shipment > Parcel Created Exception: " + exception.Message;
                            await _logger.InsertLogAsync(LogLevel.Debug, message);
                        }
                    }
                }
            }
        }

        if (errorList.Count() > 0)
        {
            foreach (var error in errorList)
            {
                string message = "Shipping Manager - Canada Post Create Shipment > Error : " + error;
                await _logger.InsertLogAsync(LogLevel.Error, message);
            }

            return false;
        }

        if (_canadaPostApiSettings.TestMode)
            await _logger.InsertLogAsync(LogLevel.Debug, responseAsString);

        return true;
    }

    /// <summary>
    /// Create a canada post shipment
    /// </summary>
    /// <param name="shipment">Shipment</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// </returns>
    public async Task<bool> CanadaPostRefundShipmentAsync(Nop.Core.Domain.Shipping.Shipment orderShipment)
    {
        string errors = string.Empty;
        string responseAsString = String.Empty;
        List<string> errorList = new List<string>();

        var orderShipmentDetails = _shipmentDetailsService.GetShipmentDetailsForShipmentId(orderShipment.Id);
        if (orderShipmentDetails != null)
        {

            // Get Shipment Details
            if (!string.IsNullOrEmpty(orderShipmentDetails.ShipmentId))
            {
                string useragent = "Canada Post SDK; .NetStandard 2.1;";

                var authorizationApi = new AuthorizationApi(_canadaPostApiSettings.AuthenticationURL, useragent);

                authorizationApi.Configuration.Username = _canadaPostApiSettings.Username;
                authorizationApi.Configuration.Password = _canadaPostApiSettings.Password;
                authorizationApi.Configuration.CustomerNumber = _canadaPostApiSettings.CustomerNumber;
                authorizationApi.Configuration.MoBoCN = _canadaPostApiSettings.MoBoCN;
                authorizationApi.Configuration.ContractId = _canadaPostApiSettings.ContractId;

                var response = authorizationApi.AuthorizationCreateConfiguration();

                ShippingApi shippingApi = new ShippingApi(authorizationApi.Configuration);

                var shipmentInfo = shippingApi.GetShipment(orderShipmentDetails.ShipmentId, out errors);
                if (shipmentInfo != null)
                {
                    // Retrieve values from ShipmentInfoType object
                    responseAsString += "\r\nShipment ID: " + shipmentInfo.shipmentid + "\r\n";
                    foreach (LinkType link in shipmentInfo.links)
                    {
                        responseAsString += link.rel + ":  " + link.href + "\r\n";
                    }
                }
                else
                {
                    responseAsString += "No shipments returned.\r\n";
                }

                if (!string.IsNullOrEmpty(errors))
                    errorList.Add(errors);
                else
                {
                    int shippingOriginAddressId = _shippingSettings.ShippingOriginAddressId;
                    var shippingOriginAddress = await _addressService.GetAddressByIdAsync(shippingOriginAddressId);
                    if (shippingOriginAddress == null)
                    {
                        throw new CanadaPostException("Shipping Settings - Shipping Origin not Set");
                    }

                    // Create shipment object to contain xml request
                    ShipmentRefundRequestType shipmentRefundRequest = new ShipmentRefundRequestType();
                    shipmentRefundRequest.email = shippingOriginAddress.Email;

                    var shipmentRefundRequestInfo = shippingApi.RequestShipmentRefund(orderShipmentDetails.ShipmentId, shipmentRefundRequest, out errors);
                    if (shipmentRefundRequestInfo != null)
                    {
                        // Retrieve values from shipmentRefundRequestInfo object
                        responseAsString += "Service Ticket ID: " + shipmentRefundRequestInfo.serviceticketid + "\r\n";
                        responseAsString += "Service Ticket Date: " + shipmentRefundRequestInfo.serviceticketdate + "\r\n";
                    }

                    if (!string.IsNullOrEmpty(errors))
                        errorList.Add(errors);
                }
            }
        }

        if (errorList.Count() > 0)
        {
            foreach (var error in errorList)
            {
                string message = "Shipping Manager - Canada Post Request Refund > Error : " + error;
                await _logger.InsertLogAsync(LogLevel.Error, message);
            }

            return false;
        }

        if (_canadaPostApiSettings.TestMode)
            await _logger.InsertLogAsync(LogLevel.Debug, responseAsString);

        return true;

    }

    /// <summary>
    /// Get a flag if a shipping option configuation is valid
    /// </summary>
    /// <param name="carrier">Carrier</param>
    /// <param name="shippinMethod">ShippingMethod</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the status if the canada post configuration
    /// </returns>
    public string CanadaPostValidateShippingOptionAsync(out string errors)
    {
        string responseAsString = String.Empty;

        string useragent = "Canada Post SDK; .NetStandard 2.1;";

        var authorizationApi = new AuthorizationApi(_canadaPostApiSettings.AuthenticationURL, useragent);

        authorizationApi.Configuration.Username = _canadaPostApiSettings.Username;
        authorizationApi.Configuration.Password = _canadaPostApiSettings.Password;
        authorizationApi.Configuration.CustomerNumber = _canadaPostApiSettings.CustomerNumber;
        authorizationApi.Configuration.MoBoCN = _canadaPostApiSettings.MoBoCN;
        authorizationApi.Configuration.ContractId = _canadaPostApiSettings.ContractId;

        var response = authorizationApi.AuthorizationCreateConfiguration();

        // Retrieve values from customer object
        responseAsString += GetCustomerInformation(authorizationApi, out errors);

        if (string.IsNullOrEmpty(errors))
        { 
            // Retrieve values from customer object
            responseAsString += GetServiceStatus(authorizationApi);
        }

        return responseAsString;

    }

    /// <summary>
    /// Get a flag if the configuation is valid
    /// </summary>
    /// <param name="countryCode">Country code string</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the status if the canada post configuration
    /// </returns>
    public async Task<(string, List<string>)> CanadaPostValidateConfigurationAsync(int storeId, int vendorId)
    {

        var errorList = new List<string>();

        var shippingManagerService = EngineContext.Current.Resolve<IShippingManagerService>();
        var shippingService = EngineContext.Current.Resolve<IShippingService>();

        var shippingManagerByWeightByTotalRecords = await shippingManagerService.GetRecordsAsync(0, storeId, vendorId, 0, 0, 0, 0, null);

        var response = CanadaPostValidateShippingOptionAsync(out string errors);

        if (!string.IsNullOrEmpty(errors))
            errorList.Add(errors);

        return (response, errorList);

    }

    #endregion

    #region Measure Utility

    private const int MIN_LENGTH = 50; // 5 cm
    private const int MIN_WEIGHT = 0; // 0 kg
    private const int ONE_KILO = 1; // 1 kg
    private const int ONE_CENTIMETER = 10; // 1 cm

    protected class Weight
    {
        public static string Units => "kg";

        public int Value { get; set; }
    }

    protected class Dimensions
    {
        public static string Units => "meters";

        public decimal Length { get; set; }

        public decimal Width { get; set; }

        public decimal Height { get; set; }
    }

    /// <returns>A task that represents the asynchronous operation</returns>
    private async Task<decimal> ConvertFromPrimaryMeasureDimensionAsync(decimal quantity, MeasureDimension usedMeasureDimension)
    {
        return Math.Round(await _measureService.ConvertFromPrimaryMeasureDimensionAsync(quantity, usedMeasureDimension) * 100, 1);
    }

    protected virtual async Task<MeasureWeight> GatewayMeasureWeightAsync()
    {
        var usedWeight = await _measureService.GetMeasureWeightBySystemKeywordAsync(Weight.Units);
        if (usedWeight == null)
            throw new NopException("Sendcloud shipping service. Could not load \"{0}\" measure weight", Weight.Units);

        return usedWeight;
    }

    protected virtual async Task<MeasureDimension> GatewayMeasureDimensionAsync()
    {

        var usedMeasureDimension = await _measureService.GetMeasureDimensionBySystemKeywordAsync(Dimensions.Units);
        if (usedMeasureDimension == null)
            throw new NopException("Sendcloud shipping service. Could not load \"{0}\" measure dimension", Dimensions.Units);

        return usedMeasureDimension;
    }

    protected virtual async Task<int> GetWeightAsync(decimal weight, MeasureWeight usedWeight)
    {
        var convertedWeight = Convert.ToInt32(Math.Ceiling(await _measureService.ConvertFromPrimaryMeasureWeightAsync(weight, usedWeight)));

        return (convertedWeight < MIN_WEIGHT ? MIN_WEIGHT : convertedWeight);
    }

    #endregion

    #region Shipping Rate Calculation

    /// <summary>
    /// Gets the transit days
    /// </summary>
    /// <param name="shippingMethodId">Shipping method ID</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the transit days
    /// </returns>
    private async Task<int?> GetTransitDaysAsync(int shippingMethodId)
    {
        //Get the vendor
        int vendorId = 0;
        if (await _workContext.GetCurrentVendorAsync() != null)
            vendorId = (await _workContext.GetCurrentVendorAsync()).Id;

        return await _settingService.GetSettingByKeyAsync<int?>(string.Format(ShippingManagerDefaults.TRANSIT_DAYS_SETTINGS_KEY, vendorId, shippingMethodId));
    }

    /// <summary>
    /// Get fixed rate
    /// </summary>
    /// <param name="shippingMethodId">Shipping method identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the rate
    /// </returns>
    private async Task<decimal> GetRateAsync(int shippingMethodId)
    {
        //Get the vendor
        int vendorId = 0;
        if ((await _workContext.GetCurrentVendorAsync()) != null)
            vendorId = (await _workContext.GetCurrentVendorAsync()).Id;

        return await _settingService.GetSettingByKeyAsync<decimal>(string.Format(ShippingManagerDefaults.FIXED_RATE_SETTINGS_KEY, vendorId, shippingMethodId));
    }

    /// <summary>
    /// Get a shipping method from the friendly name
    /// </summary>
    /// <param name="friendlyName">Friendly Name identifier</param>
    /// <param name="vendorId">Vendor identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the shipping method
    /// </returns>
    private async Task<ShippingMethod> GetShippingMethodFromFriendlyNameAsync(string friendlyName, int vendorId = 0)
    {
        var shippingManagerService = EngineContext.Current.Resolve<IShippingManagerService>();

        var (shippingMethod, smbwbt) = await shippingManagerService.GetShippingMethodFromFriendlyNameAsync(friendlyName, vendorId);

        return shippingMethod;
    }

    /// <summary>
    /// Get rate by the parameters
    /// </summary>
    /// <param name="subTotal">Subtotal</param>
    /// <param name="weight">Weight</param>
    /// <param name="shippingMethodId">Shipping method ID</param>
    /// <param name="storeId">Store ID</param>
    /// <param name="warehouseId">Warehouse ID</param>
    /// <param name="countryId">Country ID</param>
    /// <param name="stateProvinceId">State/Province ID</param>
    /// <param name="zip">Zip code</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the shipping by weight record
    /// </returns>
    private async Task<ShippingManagerByWeightByTotal?> GetMethodAsync(decimal subTotal, decimal weight, int shippingMethodId, 
        int storeId, int vendorId, int warehouseId, int countryId, int stateProvinceId, string zip)
    {
        var shippingManagerService = EngineContext.Current.Resolve<IShippingManagerService>();

        var shippingByWeightByTotalRecord = await shippingManagerService.FindRecordsAsync(shippingMethodId, storeId, vendorId, warehouseId, countryId, stateProvinceId, zip, weight, subTotal, true);
        if (shippingByWeightByTotalRecord == null)
        {
            if (_shippingManagerSettings.LimitMethodsToCreated)
                return null;

            return null;
        }

        return shippingByWeightByTotalRecord;
    }

    /// <summary>
    /// Get rate by weight and by subtotal
    /// </summary>
    /// <param name="shippingByWeightByTotalRecord">ShippingManagerByWeightByTotal</param>
    /// <param name="subTotal">subTotal value</param>
    /// <param name="weight">weight value</param>
    /// <returns>The calculated rate</returns>
    private decimal? CalculateRate(ShippingManagerByWeightByTotal shippingByWeightByTotalRecord, decimal subTotal = 0, decimal weight = 0)
    {

        // Formula [additional fixed cost] + ([order total weight] - [lower weight limit]) * [rate per weight unit] + [order subtotal] * [charge percentage]

        if (shippingByWeightByTotalRecord == null)
        {
            if (_shippingManagerSettings.LimitMethodsToCreated)
                return null;

            return decimal.Zero;
        }

        //additional fixed cost
        var shippingTotal = shippingByWeightByTotalRecord.AdditionalFixedCost;

        //charge amount per weight unit
        if (shippingByWeightByTotalRecord.RatePerWeightUnit > decimal.Zero)
        {
            var weightRate = Math.Max(weight - shippingByWeightByTotalRecord.LowerWeightLimit, decimal.Zero);
            shippingTotal += shippingByWeightByTotalRecord.RatePerWeightUnit * weightRate;
        }

        //percentage rate of subtotal
        if (shippingByWeightByTotalRecord.PercentageRateOfSubtotal > decimal.Zero)
        {
            shippingTotal += Math.Round((decimal)((((float)subTotal) * ((float)shippingByWeightByTotalRecord.PercentageRateOfSubtotal)) / 100f), 2);
        }

        return Math.Max(shippingTotal, decimal.Zero);
    }

    /// <summary>
    /// Get the shipping method options
    /// </summary>
    /// <param name="shippingOptionRequests">List of ShippingOptionRequests</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the list of shipping option requests
    /// </returns>
    public async Task<GetShippingOptionResponse> GetShippingMethodOptionsAsync(IList<GetShippingOptionRequest> shippingOptionRequests)
    {

        var result = new GetShippingOptionResponse();

        //request shipping options (separately for each package-request)
        IList<ShippingOption> srcmShippingOptions = null;
        foreach (var shippingOptionRequest in shippingOptionRequests)
        {
            var getShippingOptionResponse = await GetShippingOptionsAsync(shippingOptionRequest);

            if (getShippingOptionResponse.Success)
            {
                //success
                if (srcmShippingOptions == null)
                {
                    //first shipping option request
                    srcmShippingOptions = getShippingOptionResponse.ShippingOptions;
                }
                else
                {
                    //get shipping options which already exist for prior requested packages for this scrm (i.e. common options)
                    srcmShippingOptions = srcmShippingOptions
                        .Where(existingso => getShippingOptionResponse.ShippingOptions.Any(newso => newso.Name == existingso.Name))
                        .ToList();

                    //and sum the rates
                    foreach (var existingso in srcmShippingOptions)
                    {
                        existingso.Rate += getShippingOptionResponse
                            .ShippingOptions
                            .First(newso => newso.Name == existingso.Name)
                            .Rate;
                    }
                }
            }
            else
            {
                //errors
                foreach (var error in getShippingOptionResponse.Errors)
                {
                    result.AddError(error);
                    await _logger.WarningAsync($"Shipping ({ShippingManagerDefaults.CanadaPostSystemName}). {error}");
                }
                //clear the shipping options in this case
                srcmShippingOptions = new List<ShippingOption>();
                break;
            }
        }

        //add this scrm's options to the result
        if (srcmShippingOptions != null)
        {
            foreach (var so in srcmShippingOptions)
            {
                //set system name if not set yet
                if (string.IsNullOrEmpty(so.ShippingRateComputationMethodSystemName))
                    so.ShippingRateComputationMethodSystemName = ShippingManagerDefaults.CanadaPostSystemName;
                if (_shoppingCartSettings.RoundPricesDuringCalculation)
                    so.Rate = await _priceCalculationService.RoundPriceAsync(so.Rate);
                result.ShippingOptions.Add(so);
            }

            if (_shippingSettings.ReturnValidOptionsIfThereAreAny)
            {
                //return valid options if there are any (no matter of the errors returned by other shipping rate computation methods).
                if (result.ShippingOptions.Any() && result.Errors.Any())
                    result.Errors.Clear();
            }
        }

        //no shipping options loaded
        if (!result.ShippingOptions.Any() && !result.Errors.Any())
            result.Errors.Add(await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.ShippingOptionCouldNotbeLoaded"));

        return result;

    }

    /// <summary>
    ///  Gets available shipping options
    /// </summary>
    /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
    /// <returns> 
    /// A task that represents the asynchronous operation
    /// The task result contains the list of responses of shipping rate options
    /// </returns>
    public async Task<GetShippingOptionResponse> GetShippingOptionsAsync(GetShippingOptionRequest getShippingOptionRequest)
    {
        decimal length, height, width;
        decimal lengthTmp, widthTmp, heightTmp;

        if (getShippingOptionRequest == null)
            throw new ArgumentNullException(nameof(getShippingOptionRequest));

        var response = new GetShippingOptionResponse();

        var shippingManagerService = EngineContext.Current.Resolve<IShippingManagerService>();
        var shippingService = EngineContext.Current.Resolve<IShippingService>();

        if (getShippingOptionRequest.Items == null || !getShippingOptionRequest.Items.Any())
        {
            response.AddError("No shipment items");
            await _logger.InsertLogAsync(LogLevel.Error, "Shipping Manager - Sendcloud Get Shipping Options - No shipment items");
            return response;
        }

        //choose the shipping rate calculation method
        if (_shippingManagerSettings.ShippingByWeightByTotalEnabled)
        {
            //shipping rate calculation by products weight

            if (getShippingOptionRequest.ShippingAddress == null)
            {
                response.AddError("Shipping address is not set");
                await _logger.InsertLogAsync(LogLevel.Error, "Shipping Manager - Canada Post Get Shipping Options - Shipping address is not set");
                return response;
            }

            var storeId = getShippingOptionRequest.StoreId != 0 ? getShippingOptionRequest.StoreId : await _entityGroupService.GetActiveStoreScopeConfiguration();
            var warehouseId = getShippingOptionRequest.WarehouseFrom?.Id ?? 0;
            var countryId = getShippingOptionRequest.ShippingAddress.CountryId ?? 0;
            var stateProvinceId = getShippingOptionRequest.ShippingAddress.StateProvinceId ?? 0;

            var zip = getShippingOptionRequest.ShippingAddress.ZipPostalCode;


            bool freeShipping = getShippingOptionRequest.Items.Any(i => i.Product.IsFreeShipping);
            bool doNotPackage = getShippingOptionRequest.Items.Any(i => i.Product.ShipSeparately) &&
                (getShippingOptionRequest.Items.Any(i => i.OverriddenQuantity == 1 || getShippingOptionRequest.Items.Count() == 1));

            string productName = "Multiple Products";
            var product = getShippingOptionRequest.Items.FirstOrDefault().Product;
            if (getShippingOptionRequest.Items.Count() == 1)
                productName = product.Name;

            MeasureWeight usedMeasureWeight = await GatewayMeasureWeightAsync();

            //get total weight of shipped items (excluding items with free shipping)
            var totalWeight = await shippingService.GetTotalWeightAsync(getShippingOptionRequest, ignoreFreeShippedItems: true);
            totalWeight = await _measureService.ConvertFromPrimaryMeasureWeightAsync(totalWeight, usedMeasureWeight);

            //get subtotal of shipped items
            var subTotal = decimal.Zero;
            foreach (var packageItem in getShippingOptionRequest.Items)
            {
                if (await shippingService.IsFreeShippingAsync(packageItem.ShoppingCartItem))
                    continue;

                var price = await shippingManagerService.GetPackagePrice(packageItem.ShoppingCartItem);

                subTotal += price;
            }

            if (_shippingManagerSettings.TestMode || _canadaPostApiSettings.TestMode)
            {
                string message = "Shipping Manager - Canada Post Get Shipping Options > Product: " + productName + 
                    " For Store: " + storeId + " WarehouseId: " + warehouseId.ToString() +
                    " Package Items Count: " + getShippingOptionRequest.Items.Count +
                    " Zip: " + zip + " Weight:" + totalWeight.ToString() + " SubTotal: " + subTotal.ToString();
                await _logger.InsertLogAsync(LogLevel.Information, message);
            }

            foreach (var packageItem in getShippingOptionRequest.Items)
            {
                //if (await shippingService.IsFreeShippingAsync(packageItem.ShoppingCartItem))
                //    continue;

                product = await _productService.GetProductByIdAsync(packageItem.ShoppingCartItem.ProductId);

                int weight = await GetWeightAsync(product.Weight, usedMeasureWeight);
                (widthTmp, lengthTmp, heightTmp) = await shippingService.GetDimensionsAsync(getShippingOptionRequest.Items);

                MeasureDimension usedMeasureDimension = await GatewayMeasureDimensionAsync();

                length = await ConvertFromPrimaryMeasureDimensionAsync(lengthTmp, usedMeasureDimension);
                height = await ConvertFromPrimaryMeasureDimensionAsync(heightTmp, usedMeasureDimension);
                width = await ConvertFromPrimaryMeasureDimensionAsync(widthTmp, usedMeasureDimension);

                int vendorId = product.VendorId;

                var foundRecords = await shippingManagerService.FindMethodsAsync(storeId, vendorId, warehouseId, 0, countryId, stateProvinceId, zip, weight, subTotal);

                if (_shippingManagerSettings.TestMode || _canadaPostApiSettings.TestMode)
                {
                    string message = "Shipping Manager - Canada Post Get Shipping Options > Product: " + packageItem.ShoppingCartItem.ProductId.ToString() +
                        " For Store: " + storeId + " WarehouseId: " + warehouseId.ToString() +
                        " Records Found: " + foundRecords.Count() + " for Vendor: " + vendorId;
                    await _logger.InsertLogAsync(LogLevel.Information, message);
                }

                foreach (var record in foundRecords)
                {
                    var carrier = await _carrierService.GetCarrierByIdAsync(record.CarrierId);
                    if (carrier != null)
                    {
                        if (carrier.ShippingRateComputationMethodSystemName == ShippingManagerDefaults.CanadaPostSystemName)
                        {

                            var shippingMethod = await shippingService.GetShippingMethodByIdAsync(record.ShippingMethodId);

                            if (shippingMethod == null)
                                continue;

                            var rate = CalculateRate(record, subTotal, weight);
                            if (!rate.HasValue)
                                continue;

                            if (product.IsFreeShipping)
                                rate = 0;

                            if (_shippingManagerSettings.TestMode || _canadaPostApiSettings.TestMode)
                            {
                                string message = "Shipping Manager - Canada Post Get Shipping Options > Product: " + packageItem.ShoppingCartItem.ProductId.ToString() +
                                    " For Store: " + storeId + 
                                    " Record for Carrier: " + carrier.Name +
                                    " Shipping Method Id: " + record.ShippingMethodId.ToString() +
                                    " Rate: " + rate.Value.ToString();
                                await _logger.InsertLogAsync(LogLevel.Debug, message);
                            }

                            string carrierName = string.Empty; // Canada Post allready containts the carrier name
                            //if (carrier != null)
                            //    carrierName = carrier.Name + " - ";

                            string cutOffTimeName = string.Empty;
                            if (_shippingManagerSettings.DisplayCutOffTime)
                            {
                                var cutOffTime = await _carrierService.GetCutOffTimeByIdAsync(record.CutOffTimeId);
                                if (cutOffTime != null)
                                    cutOffTimeName = " " + cutOffTime.Name;
                            }

                            string description = await _localizationService.GetLocalizedAsync(shippingMethod, x => x.Description) + cutOffTimeName;

                            string name = !string.IsNullOrEmpty(record.FriendlyName.Trim()) ? 
                                record.FriendlyName.Trim() : await _localizationService.GetLocalizedAsync(shippingMethod, x => x.Name);

                            int? transitDays = record.TransitDays;

                                response.ShippingOptions.Add(new ShippingOption
                                {
                                    ShippingRateComputationMethodSystemName = ShippingManagerDefaults.AramexSystemName,
                                    Name = carrierName + name,
                                    Description = description,
                                    Rate = rate.Value,
                                    TransitDays = transitDays
                                });
                            }
                        }
                        else
                        {
                            string message = "Shipping Manager - Create Shipping Method Requests > No Carrier Found for Record";
                            await _logger.InsertLogAsync(LogLevel.Error, message);
                        }
                    }
                }
            }
            else
            {
                //shipping rate calculation by fixed rate
                var restrictByCountryId = getShippingOptionRequest.ShippingAddress?.CountryId;
                response.ShippingOptions = await (await shippingService.GetAllShippingMethodsAsync(restrictByCountryId)).SelectAwait(async shippingMethod => new ShippingOption
                {
                    Name = await _localizationService.GetLocalizedAsync(shippingMethod, x => x.Name),
                    Description = await _localizationService.GetLocalizedAsync(shippingMethod, x => x.Description),
                    Rate = await GetRateAsync(shippingMethod.Id),
                    TransitDays = await GetTransitDaysAsync(shippingMethod.Id)
                }).ToListAsync();

        }

        return response;
    }

    public string GetCustomerInformation(AuthorizationApi authorizationApi, out string errors)
    {
        string responseAsString = String.Empty;
        errors = String.Empty;

        var response = authorizationApi.AuthorizationCreateConfiguration();

        // Retrieve values from customer object
        var customer = authorizationApi.GetCustomerInformation(out errors);
        if (customer != null)
        {
            // Retrieve values from customer object
            responseAsString += "Customer Number: " + customer.customernumber + "\r\n";
            responseAsString += "\r\nContract Ids:\r\n";

            if (customer.contracts != null)
            {
                foreach (var contractId in customer.contracts)
                {
                    responseAsString += "- " + contractId + "\r\n";
                }
            }

            responseAsString += "\r\nPayers:\r\n";
            foreach (PayerType payer in customer.authorizedpayers)
            {
                responseAsString += "- Customer Number: " + payer.payernumber + "\r\n";
                var i = 0;
                foreach (String methodOfPayment in payer.methodsofpayment)
                {
                    if (i == 0)
                    {
                        responseAsString += "  Payment Method:\r\n";
                    }
                    i++;
                    responseAsString += " - " + methodOfPayment + "\r\n";
                }
                responseAsString += "\r\n";
            }

            if (customer.links != null)
            {
                foreach (LinkType link in customer.links)
                {
                    responseAsString += link.rel + ": " + link.href + "\r\n";
                }
            }
        }
        else
        {
            responseAsString += " Api Error: " + errors + "\r\n";
        }

        return responseAsString;
    }

    public string GetServiceStatus(AuthorizationApi authorizationApi)
    {

        string responseAsString = String.Empty;

        var response = authorizationApi.AuthorizationCreateConfiguration();

        // Retrieve values from object

        var infoMessages = authorizationApi.GetServiceInfo("shipment", out string errors);
        if (infoMessages != null)
        {
            if (infoMessages.infomessage != null)
            {
                foreach (InfoMessageType infoMessage in infoMessages.infomessage)
                {
                    responseAsString += " Message Type is :" + infoMessage.messagetype + "\r\n";
                    responseAsString += " Message Text is :" + infoMessage.messagetext + "\r\n";
                    responseAsString += " Message Start Time is :" + infoMessage.fromdatetime + "\r\n";
                    responseAsString += " Message End Time is :" + infoMessage.todatetime + "\r\n";
                }
            }
            else
            {
                responseAsString += " Shipment Service is operating" + "\r\n";
            }
        }
        else if (!string.IsNullOrWhiteSpace(errors))
        {
            responseAsString += " Api Error: " + errors + "\r\n";
        }

        infoMessages = authorizationApi.GetServiceInfo("customer", out errors);
        if (infoMessages != null)
        {
            if (infoMessages.infomessage != null)
            {
                foreach (InfoMessageType infoMessage in infoMessages.infomessage)
                {
                    responseAsString += " Message Type is :" + infoMessage.messagetype + "\r\n";
                    responseAsString += " Message Text is :" + infoMessage.messagetext + "\r\n";
                    responseAsString += " Message Start Time is :" + infoMessage.fromdatetime + "\r\n";
                    responseAsString += " Message End Time is :" + infoMessage.todatetime + "\r\n";
                }
            }
            else
            {
                responseAsString += " Customer Service is operating" + "\r\n";
            }
        }
        else if (!string.IsNullOrWhiteSpace(errors))
        {
            responseAsString += " Api Error: " + errors + "\r\n";
        }

        infoMessages = authorizationApi.GetServiceInfo("manifest", out errors);
        if (infoMessages != null)
        {
            if (infoMessages.infomessage != null)
            {
                foreach (InfoMessageType infoMessage in infoMessages.infomessage)
                {
                    responseAsString += " Message Type is :" + infoMessage.messagetype + "\r\n";
                    responseAsString += " Message Text is :" + infoMessage.messagetext + "\r\n";
                    responseAsString += " Message Start Time is :" + infoMessage.fromdatetime + "\r\n";
                    responseAsString += " Message End Time is :" + infoMessage.todatetime + "\r\n";
                }
            }
            else
            {
                responseAsString += " Manifest Service is operating" + "\r\n";
            }
        }
        else if (!string.IsNullOrWhiteSpace(errors))
        {
            responseAsString += " Api Error: " + errors + "\r\n";
        }

        infoMessages = authorizationApi.GetServiceInfo("barcode", out errors);
        if (infoMessages != null)
        {
            if (infoMessages.infomessage != null)
            {
                foreach (InfoMessageType infoMessage in infoMessages.infomessage)
                {
                    responseAsString += " Message Type is :" + infoMessage.messagetype + "\r\n";
                    responseAsString += " Message Text is :" + infoMessage.messagetext + "\r\n";
                    responseAsString += " Message Start Time is :" + infoMessage.fromdatetime + "\r\n";
                    responseAsString += " Message End Time is :" + infoMessage.todatetime + "\r\n";
                }
            }
            else
            {
                responseAsString += " Barcode Service is operating" + "\r\n";
            }
        }
        else if (!string.IsNullOrWhiteSpace(errors))
        {
            responseAsString += " Api Error: " + errors + "\r\n";
        }

        infoMessages = authorizationApi.GetServiceInfo("uamailing", out errors);
        if (infoMessages != null)
        {
            if (infoMessages.infomessage != null)
            {
                foreach (InfoMessageType infoMessage in infoMessages.infomessage)
                {
                    responseAsString += " Message Type is :" + infoMessage.messagetype + "\r\n";
                    responseAsString += " Message Text is :" + infoMessage.messagetext + "\r\n";
                    responseAsString += " Message Start Time is :" + infoMessage.fromdatetime + "\r\n";
                    responseAsString += " Message End Time is :" + infoMessage.todatetime + "\r\n";
                }
            }
            else
            {
                responseAsString += " Mailing Service is operating" + "\r\n";
            }
        }
        else if (!string.IsNullOrWhiteSpace(errors))
        {
            responseAsString += " Api Error: " + errors + "\r\n";
        }

        infoMessages = authorizationApi.GetServiceInfo("authreturn", out errors);
        if (infoMessages != null)
        {
            if (infoMessages.infomessage != null)
            {
                foreach (InfoMessageType infoMessage in infoMessages.infomessage)
                {
                    responseAsString += " Message Type is :" + infoMessage.messagetype + "\r\n";
                    responseAsString += " Message Text is :" + infoMessage.messagetext + "\r\n";
                    responseAsString += " Message Start Time is :" + infoMessage.fromdatetime + "\r\n";
                    responseAsString += " Message End Time is :" + infoMessage.todatetime + "\r\n";
                }
            }
            else
            {
                responseAsString += " AuthReturn Service is operating" + "\r\n";
            }
        }
        else if (!string.IsNullOrWhiteSpace(errors))
        {
            responseAsString += " Api Error: " + errors + "\r\n";
        }

        infoMessages = authorizationApi.GetServiceInfo("shiprate", out errors);
        if (infoMessages != null)
        {
            if (infoMessages.infomessage != null)
            {
                foreach (InfoMessageType infoMessage in infoMessages.infomessage)
                {
                    responseAsString += " Message Type is :" + infoMessage.messagetype + "\r\n";
                    responseAsString += " Message Text is :" + infoMessage.messagetext + "\r\n";
                    responseAsString += " Message Start Time is :" + infoMessage.fromdatetime + "\r\n";
                    responseAsString += " Message End Time is :" + infoMessage.todatetime + "\r\n";
                }
            }
            else
            {
                responseAsString += " Shipping Rate Service is operating" + "\r\n";
            }
        }
        else if (!string.IsNullOrWhiteSpace(errors))
        {
            responseAsString += " Api Error: " + errors + "\r\n";
        }

        infoMessages = authorizationApi.GetServiceInfo("outlet", out errors);
        if (infoMessages != null)
        {
            if (infoMessages.infomessage != null)
            {
                foreach (InfoMessageType infoMessage in infoMessages.infomessage)
                {
                    responseAsString += " Message Type is :" + infoMessage.messagetype + "\r\n";
                    responseAsString += " Message Text is :" + infoMessage.messagetext + "\r\n";
                    responseAsString += " Message Start Time is :" + infoMessage.fromdatetime + "\r\n";
                    responseAsString += " Message End Time is :" + infoMessage.todatetime + "\r\n";
                }
            }
            else
            {
                responseAsString += " Outlet Service is operating" + "\r\n";
            }
        }
        else if (!string.IsNullOrWhiteSpace(errors))
        {
            responseAsString += " Api Error: " + errors + "\r\n";
        }

        return responseAsString;
    }

    /// <summary>
    ///  Print an artifact label
    /// </summary>
    /// <param name="shippingApi">The shipping api client</param>
    /// <param name="shipmentId">The shipment identifier</param>
    /// <param name="aftifactUrl">The artifact Url returned from the create shipment</param>
    /// <returns> 
    /// A task that represents the asynchronous operation
    /// The task result contains
    /// - filePath - the file path 
    /// - filename - the file name
    /// - errors - any error
    /// </returns>
    public (string, string, string) PrintArtifact(ShippingApi shippingApi, string shipmentId, string aftifactUrl)
    {
        string errors = string.Empty;
        string responseAsString = string.Empty;

        var response = shippingApi.GetShipmentAftifact(aftifactUrl, out errors);
        if (response != null && string.IsNullOrEmpty(errors))
        {

            responseAsString += "HTTP Response Status: " + (int)response.StatusCode + "\r\n\r\n";

            // Write Artifact to file
            var mediaStr = response.ContentType;

            string fileName = $"canada_post_label_{shipmentId}";

            if (mediaStr.Equals("application/zpl"))
                fileName += ".zpl";
            else
                fileName += ".pdf";

            var filePath = _fileProvider.Combine(_fileProvider.MapPath("~/wwwroot/files/exportimport"), fileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                // Create a 4K buffer to chunk the file  
                byte[] buffer = new byte[4096];

                int bytesRead;

                // Read the chunk of the web response into the buffer
                while (0 < (bytesRead = response.GetResponseStream().Read(buffer, 0, buffer.Length)))
                {
                    // Write the chunk from the buffer to the file  
                    fileStream.Write(buffer, 0, bytesRead);
                }

                fileStream.Close();
            }

            return (filePath, fileName, errors);
        }
        else
        {
            return (null, null, errors);
        }

    }


    #endregion
}
