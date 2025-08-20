using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;

using Nop.Core;
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

using SendCloudApi.Net.Models;
using SendCloudApi.Net.Exceptions;
using Nop.Services.Payments;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using Nop.Plugin.Shipping.Manager.Models;

namespace Nop.Plugin.Shipping.Manager.Services
{
    /// <summary>
    /// Shipping service
    /// </summary>
    public partial class SendcloudService : ISendcloudService
    {

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
        protected readonly SendcloudApiSettings _sendcloudApiSettings;
        protected readonly ShippingManagerSettings _shippingManagerSettings;
        protected readonly IShippingAddressService _shippingAddressService;
        protected readonly ISettingService _settingService;
        protected readonly IShipmentService _shipmentService;
        protected readonly IPriceFormatter _priceFormatter;
        protected readonly IOrderService _orderService;
        protected readonly IMeasureService _measureService;
        protected readonly IShipmentDetailsService _shipmentDetailsService;    

        #endregion

        #region Ctor

        public SendcloudService(IAddressService addressService,
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
            SendcloudApiSettings sendcloudApiSettings,
            ShippingManagerSettings shippingManagerSettings,
            IShippingAddressService shippingAddressService,
            ISettingService settingService,
            IShipmentService shipmentService,
            IPriceFormatter priceFormatter,
            IOrderService orderService,
            IMeasureService measureService,
            IShipmentDetailsService shipmentDetailsService)
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
            _sendcloudApiSettings = sendcloudApiSettings;
            _shippingManagerSettings = shippingManagerSettings;
            _shippingAddressService = shippingAddressService;
            _settingService = settingService;
            _shipmentService = shipmentService;
            _priceFormatter = priceFormatter;
            _orderService = orderService;
            _measureService = measureService;
            _shipmentDetailsService = shipmentDetailsService;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Get the shipping send from addresses
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of send from addresses
        /// </returns>rns>
        public async Task<List<SenderAddress>> GetSendFromAddressesAsync()
        {
            List<SenderAddress> sendFromAddressList = new List<SenderAddress>();

            if (_shippingManagerSettings.TestMode)
            {
                string message = "Shipping Manager - SendCloud Get Send From Addresses";
                _logger.InsertLog(LogLevel.Debug, message);
            }

            var shippingClient = new SendCloudApi.Net.SendCloudApi(_sendcloudApiSettings.ApiKey, _sendcloudApiSettings.ApiSecret);
            var servicePointClient = new SendCloudApi.Net.SendCloudApi(_sendcloudApiSettings.ApiKey, _sendcloudApiSettings.ApiSecret);
            if (shippingClient == null || servicePointClient == null)
            {
                string message = "Error connecting to Sendcloud Clients";
                _logger.InsertLog(LogLevel.Error, message);
                return null;
            }

            var defaultSenderAddress = new SenderAddress();
            defaultSenderAddress.Id = 0;

            var senderAddresses = await servicePointClient.SenderAddresses.Get();
            if (senderAddresses != null)
                sendFromAddressList.AddRange(senderAddresses);

            return sendFromAddressList;
        }

        /// <summary>
        /// Get house number and street details from input string
        /// </summary>
        /// <param name="houseNumber">Address string</param>
        /// <param name="houseNumberAddition">Address string</param>
        /// <returns>
        /// The House number and additional string
        /// </returns>
        public void SplitHouseNumber(ref string houseNumber, ref string houseNumberAddition)
        {
            string newHouseNumber = string.Empty;

            Regex regex = new Regex(@"^(\b\D+\b)?\s*(\b.*?\d.*?\b)\s*(\b\D+\b)?$");

            Match match = regex.Match(houseNumber);
            if (match.Success)
            {
                if (match.Groups[1].ToString() == string.Empty && match.Groups[3].ToString() == string.Empty)
                {
                    newHouseNumber = match.Groups[2].ToString();
                }
                else if (match.Groups[1].ToString() != string.Empty && match.Groups[3].ToString() != string.Empty)
                {
                    houseNumberAddition = (match.Groups[1] != null) ? match.Groups[1].ToString() : match.Groups[3].ToString();
                    newHouseNumber = match.Groups[2].ToString();
                }
                else
                {
                    houseNumberAddition = (match.Groups[1].ToString() != string.Empty) ? match.Groups[1].ToString() : match.Groups[3].ToString();
                    newHouseNumber = match.Groups[2].ToString();
                }
            }

            if (!string.IsNullOrEmpty(newHouseNumber))
                houseNumber = newHouseNumber.Trim();

            if (!string.IsNullOrEmpty(houseNumberAddition))
                houseNumberAddition = houseNumberAddition.Trim();
        }

        /// <summary>
        /// Serialize CustomValues of ProcessPaymentRequest
        /// </summary>
        /// <param name="request">Request</param>
        /// <returns>Serialized CustomValues</returns>
        public virtual string SerializeCustomValues(Dictionary<string, object> customValues)
        {
            if (customValues == null)
                throw new ArgumentNullException(nameof(customValues));

            if (!customValues.Any())
                return null;

            //XmlSerializer won't serialize objects that implement IDictionary by default.
            //http://msdn.microsoft.com/en-us/magazine/cc164135.aspx 

            //also see http://ropox.ru/tag/ixmlserializable/ (Russian language)

            var ds = new DictionarySerializer(customValues);
            var xs = new XmlSerializer(typeof(DictionarySerializer));

            using (var textWriter = new StringWriter())
            {
                using (var xmlWriter = XmlWriter.Create(textWriter))
                {
                    xs.Serialize(xmlWriter, ds);
                }

                var result = textWriter.ToString();
                return result;
            }
        }

        /// <summary>
        /// Deserialize CustomValues of Order
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Serialized CustomValues CustomValues</returns>
        public virtual Dictionary<string, object> DeserializeCustomValues(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            if (string.IsNullOrWhiteSpace(order.CustomValuesXml))
                return new Dictionary<string, object>();

            var serializer = new XmlSerializer(typeof(DictionarySerializer));

            using (var textReader = new StringReader(order.CustomValuesXml))
            {
                using (var xmlReader = XmlReader.Create(textReader))
                {
                    if (serializer.Deserialize(xmlReader) is DictionarySerializer ds)
                        return ds.Dictionary;
                    return new Dictionary<string, object>();
                }
            }
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

        #endregion

        #region Methods

        /// <summary>
        /// Update sendcloud shipment rate configuration
        /// </summary>
        /// <param name="client">SendCloudApi client</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// </returns>
        public async Task SendCloudUpdateAsync(SendCloudApi.Net.SendCloudApi client, bool updateShippingMethods)
        {

            var customer = await _workContext.GetCurrentCustomerAsync();
            var storeId = await _entityGroupService.GetActiveStoreScopeConfiguration();

            int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

            var shippingManagerService = EngineContext.Current.Resolve<IShippingManagerService>();
            var shippingService = EngineContext.Current.Resolve<IShippingService>();

            //Get the senders address
            var senderAddresses = await client.SenderAddresses.Get();

            //Get the SendCloud carriers list 
            var sendCloudCarriers = await client.ServicePoints.GetCarriers();
            foreach (string carrierName in sendCloudCarriers)
            {
                if (_shippingManagerSettings.TestMode)
                {
                    string message = "Shipping Manager - Sendcloud Update > Carrier Name: " + carrierName;
                    await _logger.InsertLogAsync(LogLevel.Debug, message);
                }

                var carrierService = await _carrierService.GetCarrierByNameAsync(carrierName);
                if (carrierService == null)
                {
                    var carrier = new Carrier();
                    carrier.Name = carrierName.ToUpper();
                    carrier.AdminComment = "Carrier added from SendCloud Update";
                    carrier.ShippingRateComputationMethodSystemName = ShippingManagerDefaults.SendCloudSystemName;

                    carrier.AddressId = (await _shippingAddressService.CreateAddressAsync("The", "Manager", string.Empty, carrier.Name, 0, 0, string.Empty, 
                        string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty)).Id;
                    carrier.Active = true;

                   await _carrierService.InsertCarrierAsync(carrier);
                }
            }

            if (updateShippingMethods)
            {

                var customerAddress = await _shippingAddressService.GetDefaultBillingAddressAsync(customer);

                var countryCode = await _shippingAddressService.GetDefaultCountryCodeAsync(customer);
                var shippingCountries = await _countryService.GetAllCountriesForShippingAsync();

                string shippingMethodList = string.Empty;

                foreach (var senderAddress in senderAddresses)
                {

                    if (_shippingManagerSettings.TestMode)
                    {
                        string message = "Shipping Manager - Sendcloud Update > Sender Address: " + senderAddress.Id.ToString() +
                            " City: " + senderAddress.City + " Country: " + senderAddress.Country;
                        await _logger.InsertLogAsync(LogLevel.Debug, message);
                    }

                    //Get the SendCloud shipping methods list for sender address 
                    int displayOrder = 1;
                    var sendCloudShippingMethods = await client.ShippingMethods.Get(senderAddress.Id.ToString(), null);
                    foreach (var sendCloudShippingMethod in sendCloudShippingMethods)
                    {
                        var shippingMethod = await shippingManagerService.GetShippingMethodByNameAsync(sendCloudShippingMethod.Name);
                        if (shippingMethod == null)
                        {
                            shippingMethod = new ShippingMethod();
                            shippingMethod.Name = sendCloudShippingMethod.Name;
                            shippingMethod.Description = sendCloudShippingMethod.Name;
                            shippingMethod.DisplayOrder = displayOrder++;

                            await shippingService.InsertShippingMethodAsync(shippingMethod);

                            if (shippingMethodList != string.Empty)
                                shippingMethodList += ", ";
                            shippingMethodList += sendCloudShippingMethod.Name;
                        }

                        bool showHidden = true;

                        if (showHidden)
                        {
                            var carriers = await _carrierService.GetAllCarriersAsync(showHidden);
                            foreach (var carrier in carriers)
                            {
                                if (sendCloudShippingMethod.Carrier.ToLower().Contains(carrier.Name.ToLower()))
                                {
                                    foreach (var sendCloudCountry in sendCloudShippingMethod.Countries)
                                    {
                                        int countryId = await _shippingAddressService.GetCountryIdFromCodeAsync(sendCloudCountry.Iso2);

                                        var countryFound = shippingCountries.Where(c => c.Id == countryId).FirstOrDefault();
                                        if (countryFound != null)
                                        {
                                            int stateId = 0;
                                            string zip = null;

                                            var shippingManagerByWeightByTotal = (await shippingManagerService.GetRecordsAsync(shippingMethod.Id, storeId,
                                                vendorId, 0, carrier.Id, countryId, stateId, zip)).FirstOrDefault();

                                            if (shippingManagerByWeightByTotal == null)
                                            {
                                                shippingManagerByWeightByTotal = new ShippingManagerByWeightByTotal();
                                                shippingManagerByWeightByTotal.ShippingMethodId = shippingMethod.Id;
                                                shippingManagerByWeightByTotal.CarrierId = carrier.Id;
                                                shippingManagerByWeightByTotal.WarehouseId = 0;
                                                shippingManagerByWeightByTotal.VendorId = vendorId;
                                                shippingManagerByWeightByTotal.WeightFrom = sendCloudShippingMethod.MinWeight;
                                                shippingManagerByWeightByTotal.WeightTo = sendCloudShippingMethod.MaxWeight;
                                                shippingManagerByWeightByTotal.CalculateCubicWeight = false;
                                                shippingManagerByWeightByTotal.CubicWeightFactor = 0;
                                                shippingManagerByWeightByTotal.OrderSubtotalFrom = 0;
                                                shippingManagerByWeightByTotal.OrderSubtotalTo = 1000000;
                                                shippingManagerByWeightByTotal.CountryId = countryId;
                                                shippingManagerByWeightByTotal.StateProvinceId = stateId;
                                                shippingManagerByWeightByTotal.FriendlyName = string.Empty;
                                                shippingManagerByWeightByTotal.TransitDays = 2;
                                                shippingManagerByWeightByTotal.SendFromAddressId = senderAddress.Id;

                                                await shippingManagerService.InsertShippingByWeightRecordAsync(shippingManagerByWeightByTotal);
                                            }
                                        }
                                    }
                                }
                            }
                        }

                    }

                    if (_shippingManagerSettings.TestMode)
                    {
                        string message = "Shipping Manager - Sendcloud Update > Shipping method list: " + shippingMethodList;
                        await _logger.InsertLogAsync(LogLevel.Debug, message);
                    }
                }
            }
        }

        /// <summary>
        /// Create a sendcloud parcel
        /// </summary>
        /// <param name="shipment">Shipment</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// </returns>
        public async Task SendCloudCreateParcelAsync(Nop.Core.Domain.Shipping.Shipment shipment)
        {
            string city = string.Empty;

            if (shipment == null)
                return;

            var order = await _orderService.GetOrderByIdAsync(shipment.OrderId);
            if (order == null)
                return;

            if (_shippingManagerSettings.TestMode)
            {
                string message = "Shipping Manager - SendCloud Create Parcel > For Shipment Order: " + 
                    order.CustomOrderNumber + "-" + shipment.Id.ToString();
                await _logger.InsertLogAsync(LogLevel.Debug, message);
            }

            // test code to be removed : ToDo

            //var label = await shippingClient.Label.Get(99903135);

            ////Get all parcels

            // var parcels = await client.Parcels.Get();

            ////Get a single parcel
            ////Returns a Parcel object

            //var parcel = await client.Parcels.Get(null, null, null, null, null, null);

            var shippingClient = new SendCloudApi.Net.SendCloudApi(_sendcloudApiSettings.ApiKey, _sendcloudApiSettings.ApiSecret);
            var servicePointClient = new SendCloudApi.Net.SendCloudApi(_sendcloudApiSettings.ApiKey, _sendcloudApiSettings.ApiSecret);
            if (shippingClient == null || servicePointClient == null)
            {
                string message = "Error connecting to Sendcloud Clients";
                await _logger.InsertLogAsync(LogLevel.Error, message);
                return;
            }

            var shippingManagerService = EngineContext.Current.Resolve<IShippingManagerService>();
            var shippingService = EngineContext.Current.Resolve<IShippingService>();

            var defaultSenderAddress = new SenderAddress();
            defaultSenderAddress.Id = 0;

            var senderAddresses = await servicePointClient.SenderAddresses.Get();
            if (senderAddresses != null && senderAddresses.Count() > 0)
            {
                //get the settings default Adddress 

                bool useSmbwbtAddress = false;

                int shippingOriginAddressId = _shippingSettings.ShippingOriginAddressId;
                var shippingOriginAddress = await _addressService.GetAddressByIdAsync(shippingOriginAddressId);
                if (shippingOriginAddress != null && shippingOriginAddress.CountryId.HasValue)
                {
                    var country = await _countryService.GetCountryByIdAsync(shippingOriginAddress.CountryId.Value);

                    if (country != null)
                    {
                        //Get the senders address

                        city = shippingOriginAddress.City;

                        if (_shippingManagerSettings.TestMode)
                        {
                            string message = "Shipping Manager - SendCloud Create Parcel > Shipping Origin Address: " + shippingOriginAddressId.ToString() +
                                " Country: " + country.TwoLetterIsoCode + " City: " + city;
                            await _logger.InsertLogAsync(LogLevel.Debug, message);
                        }

                        foreach (var senderAddress in senderAddresses)
                        {
                            if (senderAddress.Country == country.TwoLetterIsoCode &&
                                senderAddress.City == shippingOriginAddress.City)

                                defaultSenderAddress = senderAddress;
                        }

                        if (_shippingManagerSettings.TestMode)
                        {
                            string message = "Shipping Manager - SendCloud Create Parcel > Sender Address: " + defaultSenderAddress.Id.ToString() +
                                " City: " + defaultSenderAddress.City + " Country: " + defaultSenderAddress.Country;
                            await _logger.InsertLogAsync(LogLevel.Debug, message);
                        }
                    }

                    if (defaultSenderAddress != null && defaultSenderAddress.Id == 0)
                    {
                        defaultSenderAddress = senderAddresses[0];

                        string message = "Shipping Manager - SendCloud Create Parcel > Shipping Origin Address not set - Using Sender Address: " + defaultSenderAddress.Id.ToString() +
                            " City: " + defaultSenderAddress.City + " Country: " + defaultSenderAddress.Country;
                        await _logger.InformationAsync(message);
                    }

                    if (defaultSenderAddress != null && defaultSenderAddress.Id != 0)
                    {
                        if (shipment.TrackingNumber != null && shipment.TrackingNumber.Contains("EOID:"))
                        {

                            string orderNumber = order.CustomOrderNumber + "-" + shipment.Id.ToString();

                            if (_shippingManagerSettings.TestMode)
                            {
                                string message = "Shipping Manager - SendCloud Create Parcel > Retry Tracking Number : " + shipment.TrackingNumber +
                                " for Order: " + orderNumber;
                                await _logger.InsertLogAsync(LogLevel.Debug, message);
                            }

                            var parcel = await shippingClient.Parcels.Get(null, null, null, null, orderNumber, null);

                            if (parcel != null && parcel.Count() > 0)
                            {

                                int[] parcelIds = new int[1];
                                parcelIds[0] = parcel[0].Id;

                                if (_shippingManagerSettings.TestMode)
                                {
                                    string message = "Shipping Manager - SendCloud Create Parcel > Parcel Id: " + parcel[0].Id + " Found - Requesting Label";
                                    await _logger.InsertLogAsync(LogLevel.Debug, message);
                                }

                                var updateparcel = new SendCloudApi.Net.Models.CreateParcel
                                {
                                    Id = parcel[0].Id,
                                    RequestLabel = true,
                                    ToServicePointId = null
                                };

                                if (_sendcloudApiSettings.TestMode)
                                {
                                    var sendCloudMethods = await servicePointClient.ShippingMethods.Get("all", null);
                                    if (sendCloudMethods != null)
                                    {
                                        var unstampedLetter = sendCloudMethods.Where(x => x.Name == "Unstamped letter").FirstOrDefault();
                                        var sendCloudTestShipment = new SendCloudShippingMethod
                                        {
                                            Id = unstampedLetter.Id
                                        };

                                        updateparcel.Shipment = sendCloudTestShipment;
                                    }
                                    ;
                                }

                                var response = await shippingClient.Parcels.Update(updateparcel);

                                if (_shippingManagerSettings.TestMode)
                                {
                                    string message = "Shipping Manager - SendCloud Create Parcel > Parcel Update Response: " + response.Status.Message;
                                    await _logger.InsertLogAsync(LogLevel.Debug, message);
                                }

                                if (response.Status.Message == "Ready to send")
                                    shipment.TrackingNumber = response.TrackingNumber;

                                await _shipmentService.UpdateShipmentAsync(shipment);

                                if (_shippingManagerSettings.TestMode)
                                {
                                    string message = "Shipping Manager - SendCloud Create Parcel > Parcel Updated Shipment: " + shipment.Id.ToString() +
                                        " Tracking Number: " + response.TrackingNumber;
                                    await _logger.InsertLogAsync(LogLevel.Debug, message);
                                }
                            }
                            else
                            {
                                if (_shippingManagerSettings.TestMode)
                                {
                                    string message = "Shipping Manager - SendCloud Create Parcel > Parcel Not Found for Order: " + orderNumber;
                                    await _logger.InsertLogAsync(LogLevel.Debug, message);

                                    throw new SendCloudException(message);
                                }
                            }
                        }
                        else
                        {
                            ShippingManagerByWeightByTotal smbwbt = null;
                            ShippingMethod shippingMethod = null;
                            string sendCloudShippingMethodName = order.ShippingMethod;
                            if (sendCloudShippingMethodName != null)
                            {
                                (shippingMethod, smbwbt) = await GetShippingMethodFromFriendlyNameAsync(sendCloudShippingMethodName);
                                if (shippingMethod != null)
                                {

                                    if (_shippingManagerSettings.TestMode)
                                    {
                                        string message = "Shipping Manager - SendCloud Create Parcel > Get Shipping Method from Friendly Name: " + sendCloudShippingMethodName +
                                            " Shipping Method: " + shippingMethod.Name;
                                        await _logger.InsertLogAsync(LogLevel.Debug, message);
                                    }

                                    sendCloudShippingMethodName = shippingMethod.Name;

                                }
                                else
                                {
                                    if (_shippingManagerSettings.TestMode)
                                    {
                                        string message = "Shipping Manager - SendCloud Create Parcel > Shipping Method: " + sendCloudShippingMethodName;
                                        await _logger.InsertLogAsync(LogLevel.Debug, message);
                                    }
                                }
                            }

                            if (sendCloudShippingMethodName != null)
                            {
                                if (smbwbt != null)
                                {
                                    defaultSenderAddress = await GetSendFromAddressesForShippingMethodAsync(smbwbt, country, city);
                                    if (defaultSenderAddress != null)
                                    {
                                        if (smbwbt.SendFromAddressId != 0)
                                            useSmbwbtAddress = true;

                                        if (sendCloudShippingMethodName != null && smbwbt != null)
                                        {
                                            var sendCloudMethods = new List<SendCloudShippingMethod>();

                                            if (defaultSenderAddress.Id != 0)
                                                sendCloudMethods = (await servicePointClient.ShippingMethods.Get(defaultSenderAddress.Id.ToString(), null)).ToList();

                                            if (!sendCloudMethods.Any())
                                                sendCloudMethods = (await servicePointClient.ShippingMethods.Get("all", null)).ToList();
                                            if (sendCloudMethods != null)
                                            {

                                                if (_shippingManagerSettings.TestMode)
                                                {
                                                    string message = "Shipping Manager - SendCloud Create Parcel > Shipping Methods: " + sendCloudMethods.Count();
                                                    await _logger.InsertLogAsync(LogLevel.Debug, message);
                                                }

                                                var sendCloudShippingMethod = sendCloudMethods.Where(x => x.Name == sendCloudShippingMethodName).FirstOrDefault();
                                                if (sendCloudShippingMethod != null)
                                                {
                                                    if (smbwbt != null && smbwbt.SendFromAddressId != 0)
                                                    {
                                                        // Check for and use Send From Address for Rate 
                                                        foreach (var senderAddress in senderAddresses)
                                                        {
                                                            if (senderAddress.Id == smbwbt.SendFromAddressId)
                                                            {
                                                                defaultSenderAddress = senderAddress;
                                                                useSmbwbtAddress = true;
                                                            }
                                                        }
                                                    }

                                                    if (_shippingManagerSettings.TestMode)
                                                    {
                                                        string message = "Shipping Manager - SendCloud Create Parcel > Shipping Method found: " + sendCloudShippingMethod.Name +
                                                            " Sender Address: " + defaultSenderAddress.Id.ToString() +
                                                            " Country: " + defaultSenderAddress.Country + " City: " + defaultSenderAddress.City;
                                                        _logger.InsertLog(LogLevel.Debug, message);
                                                    }

                                                    var shippingAddress = await _addressService.GetAddressByIdAsync(order.ShippingAddressId.Value);
                                                    if (shippingAddress != null)
                                                    {
                                                        string addressLine = string.Empty;
                                                        if (!string.IsNullOrEmpty(shippingAddress.Address1))
                                                            addressLine = shippingAddress.Address1;
                                                        if (!string.IsNullOrEmpty(shippingAddress.Address2))
                                                            addressLine += " " + shippingAddress.Address2;
                                                        string houseNumberExt = addressLine;
                                                        string houseNumber = addressLine;
                                                        SplitHouseNumber(ref houseNumber, ref houseNumberExt);

                                                        if (houseNumber != string.Empty && houseNumberExt != string.Empty)
                                                            addressLine = houseNumberExt;

                                                        string countryCode = "NL";
                                                        if (shippingAddress.CountryId.HasValue)
                                                        {
                                                            var shippingAddressCountry = await _countryService.GetCountryByIdAsync(shippingAddress.CountryId.Value);
                                                            if (shippingAddressCountry != null)
                                                                countryCode = shippingAddressCountry.TwoLetterIsoCode;
                                                        }

                                                        int parcelItemNo = 0;
                                                        decimal totalWeight = 0;
                                                        decimal totalPrice = 0;
                                                        var parcelItems = new ParcelItem[0];

                                                        var sendCloudShipment = new SendCloudShippingMethod
                                                        {
                                                            Id = sendCloudShippingMethod.Id,
                                                        };

                                                        var unstampedLetter = sendCloudMethods.Where(x => x.Name == "Unstamped letter").FirstOrDefault();
                                                        var sendCloudTestShipment = new SendCloudShippingMethod
                                                        {
                                                            Id = unstampedLetter.Id
                                                        };

                                                        if (_sendcloudApiSettings.TestMode)
                                                        {
                                                            string message = "Shipping Manager - SendCloud Create Parcel > Test Mode: " + sendCloudTestShipment.Id;
                                                            await _logger.InsertLogAsync(LogLevel.Debug, message);
                                                        }
                                                        else
                                                        {
                                                            string message = "Shipping Manager - SendCloud Create Parcel > Shipment Created Id: " + sendCloudShipment.Id;
                                                            await _logger.InsertLogAsync(LogLevel.Debug, message);
                                                        }

                                                        MeasureWeight usedMeasureWeight = await GatewayMeasureWeightAsync();

                                                        var orderItems = await _orderService.GetOrderItemsAsync(order.Id);
                                                        if (orderItems != null)
                                                        {
                                                            parcelItems = new ParcelItem[orderItems.Count];

                                                            var shipmentItems = await _shipmentService.GetShipmentItemsByShipmentIdAsync(shipment.Id);
                                                            foreach (var shipmentItem in shipmentItems)
                                                            {
                                                                var orderItem = await _orderService.GetOrderItemByIdAsync(shipmentItem.OrderItemId);
                                                                if (orderItem != null)
                                                                {
                                                                    if (!useSmbwbtAddress && _shippingSettings.UseWarehouseLocation)
                                                                    {
                                                                        var warehouse = await shippingService.GetWarehouseByIdAsync(shipmentItem.WarehouseId);
                                                                        if (warehouse != null)
                                                                        {
                                                                            var warehouseAddress = await _addressService.GetAddressByIdAsync(warehouse.AddressId);
                                                                            if (warehouseAddress != null)
                                                                            {
                                                                                foreach (var senderAddress in senderAddresses)
                                                                                {
                                                                                    if (warehouseAddress.CountryId.HasValue)
                                                                                    {
                                                                                        country = await _countryService.GetCountryByIdAsync(warehouseAddress.CountryId.Value);

                                                                                        defaultSenderAddress = await GetSendFromAddressesForShippingMethodAsync(null, country, warehouseAddress.City);

                                                                                        if (_shippingManagerSettings.TestMode)
                                                                                        {
                                                                                            string message = "Shipping Manager - SendCloud Create Parcel > " +
                                                                                                " Using Warhouse Sender Address: " + defaultSenderAddress.Id.ToString() +
                                                                                                " Country: " + defaultSenderAddress.Country + " City: " + defaultSenderAddress.City;
                                                                                            _logger.InsertLog(LogLevel.Debug, message);
                                                                                        }
                                                                                    }
                                                                                }
                                                                            }
                                                                        }
                                                                    }

                                                                    var product = await _productService.GetProductByIdAsync(orderItem.ProductId);
                                                                    if (product != null)
                                                                    {

                                                                        decimal weight = product.Weight * shipmentItem.Quantity;

                                                                        weight = await GetWeightAsync(weight, usedMeasureWeight); //ToDo

                                                                        var parcelItem = new ParcelItem
                                                                        {
                                                                            Description = product.Name,
                                                                            Quantity = shipmentItem.Quantity,
                                                                            Weight = (double)weight,
                                                                            Value = (double)product.Price,
                                                                            ProductId = product.Id.ToString(),
                                                                            StockKeepingUnit = product.Sku
                                                                        };

                                                                        if (shipment.TotalWeight.HasValue)
                                                                            totalWeight = shipment.TotalWeight.Value;
                                                                        else
                                                                            totalWeight += weight;

                                                                        totalPrice = order.OrderTotal;

                                                                        parcelItems[parcelItemNo] = parcelItem;
                                                                        parcelItemNo++;

                                                                        if (_shippingManagerSettings.TestMode)
                                                                        {
                                                                            string message = "Shipping Manager - SendCloud Create Parcel > Parcel Item: " + parcelItemNo +
                                                                                " StockKeepingUnit: " + parcelItem.StockKeepingUnit +
                                                                                " ProductId: " + parcelItem.ProductId.ToString() +
                                                                                " Description: " + parcelItem.Description +
                                                                                " Quantity: " + parcelItem.Quantity.ToString() +
                                                                                " Weight: " + parcelItem.Weight.ToString();


                                                                            await _logger.InsertLogAsync(LogLevel.Debug, message);
                                                                        }
                                                                    }
                                                                }
                                                            }

                                                            string totalOrderValue = FormatOrderTotalValue(totalPrice);

                                                            totalWeight = await GetWeightAsync(totalWeight, usedMeasureWeight); //ToDo
                                                                                                                                //totalWeight = FormatOrderTotalWeight(totalWeight);

                                                            //Create a new parcel
                                                            var newParcel = new SendCloudApi.Net.Models.CreateParcel
                                                            {
                                                                Name = shippingAddress.FirstName + " " + shippingAddress.LastName,
                                                                CompanyName = shippingAddress.Company,
                                                                HouseNumber = houseNumber,
                                                                Address = addressLine,
                                                                City = shippingAddress.City,
                                                                PostalCode = shippingAddress.ZipPostalCode,
                                                                Email = shippingAddress.Email,
                                                                Telephone = shippingAddress.PhoneNumber,
                                                                Country = countryCode,
                                                                RequestLabel = true,
                                                                ShippingMethod = _sendcloudApiSettings.TestMode ? sendCloudTestShipment.Id : sendCloudShippingMethod.Id,
                                                                OrderNumber = order.CustomOrderNumber + "-" + shipment.Id.ToString(),
                                                                Weight = (double)totalWeight,
                                                                TotalOrderValueCurrency = order.CustomerCurrencyCode,
                                                                TotalOrderValue = totalOrderValue,
                                                                Shipment = _sendcloudApiSettings.TestMode ? sendCloudTestShipment : sendCloudShipment,
                                                                ParcelItems = parcelItems,
                                                                //CustomsInvoiceNr = shipment.Id.ToString(),
                                                                //CustomsShipmentType = CustomsShipmentType.CommercialGoods,
                                                            };

                                                            if (defaultSenderAddress.Id != 0)
                                                                newParcel.SenderAddressId = defaultSenderAddress.Id;

                                                            if (!_sendcloudApiSettings.TestMode)
                                                            {
                                                                // Get Existing values
                                                                var customValues = DeserializeCustomValues(order);
                                                                foreach (var key in customValues)
                                                                {
                                                                    if (key.Key == await _localizationService.GetResourceAsync("service point id"))
                                                                    {
                                                                        if (int.TryParse(key.Value.ToString(), out int spid))
                                                                            newParcel.ToServicePointId = spid;
                                                                    }
                                                                    else if (key.Key == await _localizationService.GetResourceAsync("Service Point PO Number"))
                                                                    {
                                                                        newParcel.ToPostNumber = key.Value.ToString();
                                                                    }
                                                                }
                                                            }

                                                            if (_shippingManagerSettings.TestMode)
                                                            {
                                                                string message = "Shipping Manager - SendCloud Create Parcel > Shipment Method: " + newParcel.ShippingMethod +
                                                                    " Name: " + newParcel.Name +
                                                                    " HouseNumber: " + newParcel.HouseNumber +
                                                                    " Address: " + newParcel.Address +
                                                                    " City: " + newParcel.City +
                                                                    " PostalCode: " + newParcel.PostalCode +
                                                                    " Email: " + newParcel.Email +
                                                                    " Telephone: " + newParcel.Telephone +
                                                                    " CountryCode: " + newParcel.Country +
                                                                    " OrderNumber: " + newParcel.OrderNumber +
                                                                    " TotalOrderValueCurrency: " + newParcel.TotalOrderValueCurrency +
                                                                    " TotalOrderValue: " + newParcel.TotalOrderValue +
                                                                    " Weight: " + newParcel.Weight.ToString() +
                                                                    " CustomsInvoiceNr: " + newParcel.CustomsInvoiceNr +
                                                                    " CustomsShipmentType: " + newParcel.CustomsShipmentType +
                                                                    " CustomsInvoiceNr: " + newParcel.CustomsInvoiceNr +
                                                                    " SenderAddressId: " + defaultSenderAddress.Id.ToString();
                                                                await _logger.InsertLogAsync(LogLevel.Debug, message);
                                                            }

                                                            try
                                                            {
                                                                var response = await servicePointClient.Parcels.Create(newParcel);
                                                                if (response != null)
                                                                {
                                                                    shipment = await _shipmentService.GetShipmentByIdAsync(shipment.Id);

                                                                    if (response.Status.Message == "Ready to send")
                                                                        shipment.TrackingNumber = response.TrackingNumber;
                                                                    else
                                                                        shipment.TrackingNumber = "EOID: " + response.ExternalOrderId + " " + response.Status.Message;

                                                                    if (_shippingManagerSettings.TestMode)
                                                                    {
                                                                        string message = "Shipping Manager - SendCloud Create Parcel > Parcel Created Id: " + response.Id +
                                                                            " Status: " + response.Status.Message;
                                                                        await _logger.InsertLogAsync(LogLevel.Debug, message);
                                                                    }

                                                                    await _shipmentService.UpdateShipmentAsync(shipment);

                                                                    var orderShipmentDetails = _shipmentDetailsService.GetShipmentDetailsForShipmentId(shipment.Id);
                                                                    if (orderShipmentDetails != null)
                                                                    {
                                                                        orderShipmentDetails.ShipmentId = response.Id.ToString();
                                                                        orderShipmentDetails.ShippingMethodId = newParcel.ShippingMethod.Value;
                                                                        await _shipmentDetailsService.UpdateShipmentDetailsAsync(orderShipmentDetails);
                                                                    }
                                                                    else
                                                                    {
                                                                        orderShipmentDetails = new ShipmentDetails();
                                                                        orderShipmentDetails.OrderShipmentId = shipment.Id;
                                                                        orderShipmentDetails.ShipmentId = response.Id.ToString();
                                                                        orderShipmentDetails.ShippingMethodId = newParcel.ShippingMethod.Value;
                                                                        orderShipmentDetails.ManifestUrl = "No Url";
                                                                        orderShipmentDetails.LabelUrl = "No Url";
                                                                        orderShipmentDetails.Group = "No Group";
                                                                        await _shipmentDetailsService.InsertShipmentDetailsAsync(orderShipmentDetails);
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    string message = "Shipping Manager - SendCloud Create Parcel > Parcel Created Error";
                                                                    await _logger.InsertLogAsync(LogLevel.Error, message);
                                                                }
                                                            }
                                                            catch (SendCloudException exception)
                                                            {
                                                                if (exception.Message == "shipment: \"Invalid shipment.id\"")
                                                                {
                                                                    if (_shippingManagerSettings.TestMode)
                                                                    {
                                                                        string message = "Shipping Manager - SendCloud Create Parcel > Parcel Created Exception: " + "Invalid Shipping Method";
                                                                        await _logger.InsertLogAsync(LogLevel.Debug, message);
                                                                    }
                                                                    throw new SendCloudException("Parcel Create Exception: " + "Invalid Shipping Method");
                                                                }
                                                                else
                                                                {
                                                                    if (_shippingManagerSettings.TestMode)
                                                                    {
                                                                        string message = "Shipping Manager - SendCloud Create Parcel > Parcel Created Exception: " + exception.Message;
                                                                        await _logger.InsertLogAsync(LogLevel.Debug, message);
                                                                    }
                                                                }

                                                                if (exception.Message == "User not allowed to announce")
                                                                {
                                                                    try
                                                                    {
                                                                        newParcel.RequestLabel = false;

                                                                        var response = await servicePointClient.Parcels.Create(newParcel);
                                                                        if (response != null)
                                                                        {

                                                                            shipment = await _shipmentService.GetShipmentByIdAsync(shipment.Id);

                                                                            if (response.Status.Message == "Ready to send")
                                                                                shipment.TrackingNumber = response.TrackingNumber;
                                                                            else
                                                                                shipment.TrackingNumber = "EOID: " + response.ExternalOrderId + " " + response.Status.Message;

                                                                            if (_shippingManagerSettings.TestMode)
                                                                            {
                                                                                string message = "Shipping Manager - SendCloud Create Parcel > Retry Parcel Created Id: " + response.Id +
                                                                                    " Status: " + response.Status.Message + "Tracking Number: " + response.TrackingNumber;
                                                                                await _logger.InsertLogAsync(LogLevel.Debug, message);
                                                                            }

                                                                            await _shipmentService.UpdateShipmentAsync(shipment);

                                                                            var orderShipmentDetails = _shipmentDetailsService.GetShipmentDetailsForShipmentId(shipment.Id);
                                                                            if (orderShipmentDetails != null)
                                                                            {
                                                                                orderShipmentDetails.ShipmentId = response.Id.ToString();
                                                                                await _shipmentDetailsService.UpdateShipmentDetailsAsync(orderShipmentDetails);
                                                                            }
                                                                            else
                                                                            {
                                                                                orderShipmentDetails = new ShipmentDetails();
                                                                                orderShipmentDetails.OrderShipmentId = shipment.Id;
                                                                                orderShipmentDetails.ShipmentId = response.Id.ToString();
                                                                                orderShipmentDetails.ManifestUrl = "No Url";
                                                                                orderShipmentDetails.LabelUrl = "No Url";
                                                                                orderShipmentDetails.Group = "No Group";
                                                                                await _shipmentDetailsService.InsertShipmentDetailsAsync(orderShipmentDetails);
                                                                            }
                                                                        }
                                                                    }
                                                                    catch (SendCloudException newException)
                                                                    {
                                                                        if (_shippingManagerSettings.TestMode)
                                                                        {
                                                                            string message = "Shipping Manager - SendCloud Create Parcel > Parcel Created Exception: " +
                                                                                newException.Message + " after " + exception.Message;
                                                                            await _logger.InsertLogAsync(LogLevel.Debug, message);
                                                                        }

                                                                        throw new SendCloudException(newException.Message + " after " + exception.Message);
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    throw new SendCloudException("Parcel Create Exception: " + exception.Message);
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    throw new SendCloudException("Shipping Manager - Sendcloud - Shipping Method not found");
                                                }
                                            }
                                            else
                                            {
                                                throw new SendCloudException("Shipping Manager - Sendcloud - Get Shipping Methods Error");
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    throw new SendCloudException("Shipping Settings - Shipping Origin not Set");
                                }
                            }
                        }
                    }
                    else
                    {
                        throw new SendCloudException("Shipping Settings - Shipping Origin not Set");
                    }
                }
            }
        }

        /// <summary>
        /// Create a canada post shipment
        /// </summary>
        /// <param name="shipment">Shipment</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// </returns>
        public async Task<bool> SendcloudCancelParcelAsync(Nop.Core.Domain.Shipping.Shipment orderShipment)
        {
            string errors = string.Empty;
            string responseAsString = String.Empty;

            var orderShipmentDetails = _shipmentDetailsService.GetShipmentDetailsForShipmentId(orderShipment.Id);
            if (orderShipmentDetails != null)
            {

                // Get Shipment Details
                if (!string.IsNullOrEmpty(orderShipmentDetails.ShipmentId))
                {
                    try
                    {
                        var servicePointClient = new SendCloudApi.Net.SendCloudApi(_sendcloudApiSettings.ApiKey, _sendcloudApiSettings.ApiSecret);
                        var response = await servicePointClient.Parcels.Cancel(int.Parse(orderShipmentDetails.ShipmentId));
                        if (response != null && response.Status == "deleted")
                        {
                            orderShipmentDetails.ShipmentId = string.Empty;
                            orderShipmentDetails.ShippingMethodId = 0;

                            await _shipmentDetailsService.UpdateShipmentDetailsAsync(orderShipmentDetails);

                            await _shipmentService.UpdateShipmentAsync(orderShipment);

                            return true;
                        }
                    }
                    catch (SendCloudException exc)
                    {
                        var exception = exc;
                        string message = "Shipping Manager - SendCloud Cancel Parcel Exception: " + "Invalid Shipping Method";
                        await _logger.InsertLogAsync(LogLevel.Debug, message);

                        if (exc.Message.Contains("Page not found"))
                        {
                            var shipmentDetails = _shipmentDetailsService.GetShipmentDetailsForShipmentId(orderShipment.Id);
                            if (shipmentDetails != null)
                                await _shipmentDetailsService.DeleteShipmentDetailsAsync(shipmentDetails);

                            //try to get a shipment with the specified id
                            var shipment = await _shipmentService.GetShipmentByIdAsync(orderShipment.Id);
                            if (shipment != null)
                            {
                                shipment.TrackingNumber = string.Empty;
                                await _shipmentService.UpdateShipmentAsync(shipment);
                            }

                            return false;
                        }
                    }
                }
            }

            return false;
        }

        public async Task<bool> SendcloudIsServicePointAvailableAsync(int servicePoint)
        {
            var servicePointClient = new SendCloudApi.Net.SendCloudApi(_sendcloudApiSettings.ApiKey, _sendcloudApiSettings.ApiSecret);
            return await servicePointClient.ServicePoints.IsServicePointAvailable(servicePoint);
        }

        /// <summary>
        /// Get the shipping send from addresses
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of send from addresses
        /// </returns>rns>
        public async Task<SenderAddress> GetSendFromAddressesForShippingMethodAsync(ShippingManagerByWeightByTotal smbwbt, Core.Domain.Directory.Country country, string city)
        {
            var defaultSenderAddress = new SenderAddress();

            var servicePointClient = new SendCloudApi.Net.SendCloudApi(_sendcloudApiSettings.ApiKey, _sendcloudApiSettings.ApiSecret);
            if (servicePointClient == null)
            {
                string message = "Error connecting to Sendcloud Clients";
                await _logger.InsertLogAsync(LogLevel.Error, message);
                return null;
            }

            var senderAddresses = await servicePointClient.SenderAddresses.Get();
            if (senderAddresses != null && senderAddresses.Count() > 0)
            {
                if (smbwbt != null && smbwbt.SendFromAddressId != 0)
                {
                    //get the default Address from configuration

                    if (_shippingManagerSettings.TestMode)
                    {
                        string message = "Shipping Manager - Get SendFrom Addresses For Shipping Method : Sender Address Id : " + smbwbt.SendFromAddressId.ToString();
                        await _logger.InsertLogAsync(LogLevel.Debug, message);
                    }

                    foreach (var senderAddress in senderAddresses)
                    {
                        if (smbwbt.SendFromAddressId == senderAddress.Id)
                            defaultSenderAddress = senderAddress;
                    }
                }

                if (defaultSenderAddress.Id == 0)
                {
                    //get the settings from country and city 

                    if (_shippingManagerSettings.TestMode)
                    {
                        string message = "Shipping Manager - Get SendFrom Addresses For Shipping Method : Country : " + country + " City: " + city;
                        await _logger.InsertLogAsync(LogLevel.Debug, message);
                    }

                    foreach (var senderAddress in senderAddresses)
                    {
                        if (senderAddress.Country == country.TwoLetterIsoCode && senderAddress.City == city)
                            defaultSenderAddress = senderAddress;
                    }

                    if (_shippingManagerSettings.TestMode)
                    {
                        string message = "Shipping Manager - SendCloud Create Parcel > Sender Address: " + defaultSenderAddress.Id.ToString() +
                            " City: " + defaultSenderAddress.City + " Country: " + defaultSenderAddress.Country;
                        await _logger.InsertLogAsync(LogLevel.Debug, message);
                    }
                }

                if (defaultSenderAddress.Id == 0)
                {
                    //get the settings default Address 

                    defaultSenderAddress = senderAddresses[0];

                    string message = "Shipping Manager - SendCloud Create Parcel > Shipping Origin Address not set - Using Sender Address: " +
                    defaultSenderAddress.Id.ToString() + " City: " + defaultSenderAddress.City + " Country: " + defaultSenderAddress.Country;
                    await _logger.InformationAsync(message);

                    return defaultSenderAddress;
                }

            }

            return defaultSenderAddress;
        }
        
        /// <summary>
        /// Format the order total 
        /// </summary>
        /// <param name="shipment">Shipment</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the formated order total 
        /// </returns>
        private string FormatOrderTotalValue(decimal value)
        {
            var sendcloudNfi = new CultureInfo("en-US", false).NumberFormat;
            //and just to make sure the number format is always correct for sendcloud:
            sendcloudNfi.NumberDecimalSeparator = ".";
            sendcloudNfi.NumberGroupSeparator = "";
            string valueString = value.ToString("N2", sendcloudNfi);
            return valueString;
        }

        /// <summary>
        /// Format the order total 
        /// </summary>
        /// <param name="shipment">Shipment</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the formated order total 
        /// </returns>
        private decimal FormatOrderTotalWeight(decimal value)
        {
            var sendcloudNfi = new CultureInfo("en-US", false).NumberFormat;
            //and just to make sure the number format is always correct for sendcloud:
            sendcloudNfi.NumberDecimalSeparator = ".";
            sendcloudNfi.NumberGroupSeparator = "";
            string valueString = value.ToString("N3", sendcloudNfi);
            return Convert.ToDecimal(valueString);
        }

        /// <summary>
        /// Get a flag if a shipping option configuration is valid
        /// </summary>
        /// <param name="carrier">Carrier</param>
        /// <param name="shippinMethod">ShippingMethod</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the status if the sendcloud configuration
        /// </returns>
        public async Task<bool> SendCloudValidateShippingOptionAsync(Carrier carrier, ShippingMethod shippingMethod)
        {

            var servicePointClient = new SendCloudApi.Net.SendCloudApi(_sendcloudApiSettings.ApiKey, _sendcloudApiSettings.ApiSecret);

            //Get the senders address
            var senderAddress = await servicePointClient.SenderAddresses.Get();

            //Get the SendCloud carriers list 
            var sendCloudCarriers = await servicePointClient.ServicePoints.GetCarriers();

            bool foundCarrier = false;
            foreach (string carrierName in sendCloudCarriers)
            {
                if (carrierName.ToLower() == carrier.Name.ToLower())
                {
                    foundCarrier = true;
                    break;
                }
            }

            bool shippingMethodFound = false;
            var sendCloudShippingMethods = await servicePointClient.ShippingMethods.Get("all", null);

            foreach (var sendCloudShippingMethod in sendCloudShippingMethods)
            {
                if (shippingMethod.Name.ToLower() == sendCloudShippingMethod.Name.ToLower())
                {
                    shippingMethodFound = true;
                    break;
                }
            }

            return (shippingMethodFound && foundCarrier);
        }

        /// <summary>
        /// Get a flag if the configuration is valid
        /// </summary>
        /// <param name="countryCode">Country code string</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the status if the sendcloud configuration
        /// </returns>
        public async Task<List<string>> SendCloudValidateConfigurationAsync(SendCloudApi.Net.SendCloudApi client, int storeId, int vendorId)
        {

            var errors = new List<string>();

            var shippingManagerService = EngineContext.Current.Resolve<IShippingManagerService>();
            var shippingService = EngineContext.Current.Resolve<IShippingService>();

            var shippingManagerByWeightByTotalRecords = await shippingManagerService.GetRecordsAsync(0, storeId, vendorId, 0, 0, 0, 0, null);

            //Get the senders address
            var senderAddress = await client.SenderAddresses.Get();

            //Get the SendCloud carriers list 
            var sendCloudCarriers = await client.ServicePoints.GetCarriers();

            var sendCloudShippingMethods = await client.ShippingMethods.Get("all", null);

            foreach (var record in shippingManagerByWeightByTotalRecords)
            {
                //Check carrier

                var carrier = await _carrierService.GetCarrierByIdAsync(record.CarrierId);
                if (carrier != null)
                {
                    if (carrier.ShippingRateComputationMethodSystemName == ShippingManagerDefaults.SendCloudSystemName)
                    {
                        bool foundCarrier = false;

                        foreach (string carrierName in sendCloudCarriers)
                        {
                            if (carrierName.ToLower() == carrier.Name.ToLower())
                            {
                                foundCarrier = true;
                                break;
                            }
                        }

                        if (!foundCarrier)
                        {
                            string error = "Carrier " + carrier.Name + " not found in Sendcloud settings";
                            if (!errors.Contains(error))
                                errors.Add(error);
                        }

                        var shippingMethods = _shippingMethodRepository.Table;

                        var shippingMethod = shippingMethods.Where(sm => sm.Id == record.ShippingMethodId).FirstOrDefault();

                        bool shippingMethodFound = false;

                        foreach (var sendCloudShippingMethod in sendCloudShippingMethods)
                        {
                            if (shippingMethod.Name.ToLower() == sendCloudShippingMethod.Name.ToLower())
                            {
                                shippingMethodFound = true;
                                break;
                            }
                        }

                        if (!shippingMethodFound)
                        {
                            string error = "Shipping Method " + shippingMethod.Name + " not found in Sendcloud settings";
                            if (!errors.Contains(error))
                                errors.Add(error);
                        }
                    }
                }
                else
                {
                    string message = "Shipping Manager - Create Shipping Method Requests > No Carrier Found for Record";
                    await _logger.InsertLogAsync(LogLevel.Error, message);
                }
            }

            return (errors);
        }

        #endregion

        #region Measure Utility

        private const int MIN_LENGTH = 50; // 5 cm
        private const decimal MIN_WEIGHT = 0.1M; // 0.1 kg
        private const int ONE_KILO = 1; // 1 kg
        private const int ONE_CENTIMETER = 10; // 1 cm

        protected class Weight
        {
            public static string Units => "kg";

            public int Value { get; set; }
        }

        protected class Dimensions
        {
            public static string Units => "millimetres";

            public int Length { get; set; }

            public int Width { get; set; }

            public int Height { get; set; }
        }

        protected virtual async Task<decimal> GetWeightAsync(decimal weight, MeasureWeight usedWeight)
        {
            var convertedWeight = await _measureService.ConvertFromPrimaryMeasureWeightAsync(weight, usedWeight);

            //// Allow 0 for FreeShippedItems
            //if (weight == 0)
            //    return convertedWeight;

            return (convertedWeight < MIN_WEIGHT ? MIN_WEIGHT : convertedWeight);
        }

        /// <returns>A task that represents the asynchronous operation</returns>
        private async Task<int> ConvertFromPrimaryMeasureDimensionAsync(decimal quantity, MeasureDimension usedMeasureDimension)
        {
            return Convert.ToInt32(Math.Ceiling(await _measureService.ConvertFromPrimaryMeasureDimensionAsync(quantity, usedMeasureDimension)));
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
        private async Task<(ShippingMethod, ShippingManagerByWeightByTotal)> GetShippingMethodFromFriendlyNameAsync(string friendlyName, int vendorId = 0)
        {
            var shippingManagerService = EngineContext.Current.Resolve<IShippingManagerService>();

            var (shippingMethod, smbwbt) = await shippingManagerService.GetShippingMethodFromFriendlyNameAsync(friendlyName, vendorId);

            return (shippingMethod, smbwbt);
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
        ///  Gets available shipping options
        /// </summary>
        /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        /// <returns>Represents a response of getting shipping rate options</returns>
        public async Task<GetShippingOptionResponse> GetShippingOptionsAsync(GetShippingOptionRequest getShippingOptionRequest)
        {
            decimal length, height, width;
            decimal lengthTmp, widthTmp, heightTmp;

            if (getShippingOptionRequest == null)
                throw new ArgumentNullException(nameof(getShippingOptionRequest));

            var response = new GetShippingOptionResponse();

            var shippingManagerService = EngineContext.Current.Resolve<IShippingManagerService>();
            var shippingService = EngineContext.Current.Resolve<IShippingService>();
            var shoppingCartService = EngineContext.Current.Resolve<IShoppingCartService>();

            if (getShippingOptionRequest.Items == null || !getShippingOptionRequest.Items.Any())
            {
                response.AddError("No shipment items");
                _logger.InsertLog(LogLevel.Error, "Shipping Manager - Sendcloud Get Shipping Options - No shipment items");
                return response;
            }

            //choose the shipping rate calculation method
            if (_shippingManagerSettings.ShippingByWeightByTotalEnabled)
            {
                //shipping rate calculation by products weight

                if (getShippingOptionRequest.ShippingAddress == null)
                {
                    if (_shippingManagerSettings.TestMode)
                        _logger.InsertLog(LogLevel.Error, "Shipping Manager - Sendcloud Get Shipping Options - Shipping address is not set");

                    response.AddError("Shipping address is not set");
                    return response;
                }
                else if (!getShippingOptionRequest.ShippingAddress.CountryId.HasValue &&
                         _shippingManagerSettings.ShippingOptionDisplay == ShippingOptionDisplay.DisplayOnlyShippingOriginCountryMethods)
                {
                    return response;
                }

                var store = await _storeContext.GetCurrentStoreAsync();
                var storeId = getShippingOptionRequest.StoreId != 0 ? getShippingOptionRequest.StoreId : store.Id;
                var countryId = getShippingOptionRequest.ShippingAddress.CountryId ?? 0;
                var stateProvinceId = getShippingOptionRequest.ShippingAddress.StateProvinceId ?? 0;
                var warehouseId = getShippingOptionRequest.WarehouseFrom?.Id ?? 0;
                var zip = getShippingOptionRequest.ShippingAddress.ZipPostalCode;

                //bool freeShipping = getShippingOptionRequest.Items.Any(i => i.ShoppingCartItem.IsFreeShipping);

                //get subtotal of shipped items
                var subTotal = decimal.Zero;
                foreach (var packageItem in getShippingOptionRequest.Items)
                {
                    if (await shippingService.IsFreeShippingAsync(packageItem.ShoppingCartItem))
                        continue;

                    subTotal += (await shoppingCartService.GetSubTotalAsync(packageItem.ShoppingCartItem, true)).subTotal;
                }

                //get weight of shipped items (excluding items with free shipping)
                var weight = await shippingService.GetTotalWeightAsync(getShippingOptionRequest, ignoreFreeShippedItems: true);

                weight = await _measureService.ConvertFromPrimaryMeasureWeightAsync(weight, await GatewayMeasureWeightAsync());

                if (_shippingManagerSettings.TestMode)
                {
                    string message = "Shipping Manager - For Store : " + storeId +
                        " Sendcloud Get Shipping Options > CountryId: " + countryId.ToString() + " StateId: " + stateProvinceId.ToString() +
                        " WarehouseId: " + warehouseId.ToString() + " Zip: " + zip + " Weight:" + weight.ToString() + " SubTotal: " + subTotal.ToString() +
                        " Package Items Count: " + getShippingOptionRequest.Items.Count;
                    _logger.InsertLog(LogLevel.Debug, message);
                }

                (widthTmp, lengthTmp, heightTmp) = await shippingService.GetDimensionsAsync(getShippingOptionRequest.Items);
                length = await _measureService.ConvertFromPrimaryMeasureDimensionAsync(lengthTmp, await GatewayMeasureDimensionAsync());
                height = await _measureService.ConvertFromPrimaryMeasureDimensionAsync(heightTmp, await GatewayMeasureDimensionAsync());
                width = await _measureService.ConvertFromPrimaryMeasureDimensionAsync(widthTmp, await GatewayMeasureDimensionAsync());

                var foundRecords = await shippingManagerService.FindMethodsAsync(storeId, 0, warehouseId, 0, countryId, stateProvinceId, zip, weight, subTotal);

                if (_shippingManagerSettings.TestMode)
                {
                    string message = "Shipping Manager - For Store : " + storeId +
                        " Sendcloud Get Shipping Options > Package Item - Records Found: " + foundRecords.Count();
                    _logger.InsertLog(LogLevel.Debug, message);
                }

                foreach (var record in foundRecords)
                {
                    var carrier = await _carrierService.GetCarrierByIdAsync(record.CarrierId);
                    if (carrier != null && carrier.Active)
                    {
                        if (carrier.ShippingRateComputationMethodSystemName == ShippingManagerDefaults.SendCloudSystemName)
                        {

                            var shippingMethod = await shippingService.GetShippingMethodByIdAsync(record.ShippingMethodId);

                            if (shippingMethod == null)
                                continue;

                            var rate = CalculateRate(record, subTotal, weight);
                            if (!rate.HasValue)
                                continue;

                            if (_shippingManagerSettings.TestMode)
                            {
                                string message = "Shipping Manager - For Store : " + storeId +
                                    " Sendcloud Get Shipping Options > Record for Carrier: " + carrier.Name +
                                    " Shipping Method Id: " + record.ShippingMethodId.ToString() +
                                    " Rate: " + rate.Value.ToString();
                                _logger.InsertLog(LogLevel.Debug, message);
                            }

                            string carrierName = string.Empty; // Sendcloud already contains the carrier name
                            //if (carrier != null)
                            //    carrierName = carrier.Name + " - ";

                            string cutOffTimeName = string.Empty;
                            if (_shippingManagerSettings.DisplayCutOffTime)
                            {
                                var cutOffTime = await _carrierService.GetCarrierByIdAsync(record.CutOffTimeId);
                                if (cutOffTime != null)
                                    cutOffTimeName = " " + cutOffTime.Name;
                            }

                            string description = string.Empty;

                            if (!string.IsNullOrEmpty(record.Description))
                                description = record.Description + cutOffTimeName;
                            else
                                description = await _localizationService.GetLocalizedAsync(shippingMethod, x => x.Description) + cutOffTimeName;

                            string name = await _localizationService.GetLocalizedAsync(shippingMethod, x => x.Name);

                            if (countryId != 0)
                            {
                                if (!string.IsNullOrEmpty(record.FriendlyName))
                                    name = record.FriendlyName.Trim();
                            }
                            else if (_shippingManagerSettings.ShippingOptionDisplay == ShippingOptionDisplay.AddCountryToDisplay)
                            {
                                var country = await _countryService.GetCountryByIdAsync(record.CountryId);
                                if (country != null)
                                {
                                    name = country.Name + " - " + record.FriendlyName.Trim();
                                }
                            }

                            int? transitDays = record.TransitDays;

                            response.ShippingOptions.Add(new ShippingOption
                            {
                                ShippingRateComputationMethodSystemName = ShippingManagerDefaults.SendCloudSystemName,
                                Name = carrierName + name,
                                Description = description,
                                Rate = rate.Value,
                                TransitDays = transitDays
                            });
                        }
                    }
                }

            }
            else
            {
                //shipping rate calculation by fixed rate
                var restrictByCountryId = getShippingOptionRequest.ShippingAddress?.CountryId;

                if (_shippingManagerSettings.TestMode)
                {
                    string message = "Shipping Manager - Sendcloud Service Fixed Rate Method > CountryId: " + restrictByCountryId.ToString();
                    _logger.InsertLog(LogLevel.Debug, message);
                }

                //shipping rate calculation by fixed rate
                restrictByCountryId = getShippingOptionRequest.ShippingAddress?.CountryId;
                response.ShippingOptions = await (await shippingService.GetAllShippingMethodsAsync(restrictByCountryId)).SelectAwait(async shippingMethod => new ShippingOption
                {
                    ShippingRateComputationMethodSystemName = ShippingManagerDefaults.SendCloudSystemName,
                    Name = await _localizationService.GetLocalizedAsync(shippingMethod, x => x.Name),
                    Description = await _localizationService.GetLocalizedAsync(shippingMethod, x => x.Description),
                    Rate = await GetRateAsync(shippingMethod.Id),
                    TransitDays = await GetTransitDaysAsync(shippingMethod.Id)
                }).ToListAsync();
            }

            return response;
        }

        /// <summary>
        /// Get the shipping method options
        /// </summary>
        /// <param name="shippingOptionRequests">List of ShippingOptionRequests</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of shipping option requests
        /// </returns>
        public async Task<GetShippingOptionResponse> GetShippingMethodOptionsAsync(ShippingManagerCalculationOption smco)
        {

            var result = new GetShippingOptionResponse();

            //request shipping options (separately for each package-request)
            IList<ShippingOption> srcmShippingOptions = null;

            var getShippingOptionResponse = await GetShippingOptionsAsync(smco);

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
                    await _logger.WarningAsync($"Shipping ({ShippingManagerDefaults.SendCloudSystemName}). {error}");
                }

                //clear the shipping options in this case
                srcmShippingOptions = new List<ShippingOption>();
            }

            //add this scrm's options to the result
            if (srcmShippingOptions != null)
            {
                foreach (var so in srcmShippingOptions)
                {
                    //set system name if not set yet
                    if (string.IsNullOrEmpty(so.ShippingRateComputationMethodSystemName))
                        so.ShippingRateComputationMethodSystemName = ShippingManagerDefaults.SendCloudSystemName;
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
        public async Task<GetShippingOptionResponse> GetShippingOptionsAsync(ShippingManagerCalculationOption smco)
        {
            int length, height, width;
            decimal lengthTmp, widthTmp, heightTmp;

            GetShippingOptionRequest getShippingOptionRequest = smco.Sor.FirstOrDefault();

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
                    await _logger.InsertLogAsync(LogLevel.Error, "Shipping Manager - " +
                        "Sendcloud Get Shipping Options - Shipping address is not set");
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

                if (_shippingManagerSettings.TestMode)
                {
                    string message = "Shipping Manager - Sendcloud Get Shipping Options > Product: " + productName +
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

                    decimal weight = await GetWeightAsync(product.Weight, usedMeasureWeight);
                    (widthTmp, lengthTmp, heightTmp) = await shippingService.GetDimensionsAsync(getShippingOptionRequest.Items);

                    MeasureDimension usedMeasureDimension = await GatewayMeasureDimensionAsync();

                    length = await ConvertFromPrimaryMeasureDimensionAsync(lengthTmp, usedMeasureDimension);
                    height = await ConvertFromPrimaryMeasureDimensionAsync(heightTmp, usedMeasureDimension);
                    width = await ConvertFromPrimaryMeasureDimensionAsync(widthTmp, usedMeasureDimension);

                    int vendorId = product.VendorId;

                    int carrierId = 0;
                    var record = shippingManagerService.FindMethodAsync(smco.Smbwtr, storeId, vendorId,
                        warehouseId, carrierId, countryId, stateProvinceId, zip, weight, subTotal);

                    if (record == null)
                    {
                        var message = "Shipping Manager - Create Shipping Method Requests > No Record found for Search Product:" + packageItem.ShoppingCartItem.ProductId.ToString() +
                                " For Store: " + storeId + " WarehouseId: " + warehouseId.ToString() + " for Vendor: " + vendorId;
                        await _logger.InsertLogAsync(LogLevel.Error, message);
                    }
                    else
                    {
                        if (_shippingManagerSettings.TestMode)
                        {
                            string message = "Shipping Manager - Sendcloud Get Shipping Options > Product: " + packageItem.ShoppingCartItem.ProductId.ToString() +
                                " For Store: " + storeId + " WarehouseId: " + warehouseId.ToString() + " for Vendor: " + vendorId;
                            await _logger.InsertLogAsync(LogLevel.Information, message);
                        }

                        var carrier = await _carrierService.GetCarrierByIdAsync(record.CarrierId);
                        if (carrier != null)
                        {
                            if (carrier.ShippingRateComputationMethodSystemName == ShippingManagerDefaults.SendCloudSystemName)
                            {

                                var shippingMethod = await shippingService.GetShippingMethodByIdAsync(record.ShippingMethodId);

                                if (shippingMethod == null)
                                    continue;

                                var rate = CalculateRate(record, subTotal, weight);
                                if (!rate.HasValue)
                                    continue;

                                if (product.IsFreeShipping)
                                    rate = 0;

                                if (_shippingManagerSettings.TestMode)
                                {
                                    string message = "Shipping Manager - Sendcloud Get Shipping Options > Product: " + packageItem.ShoppingCartItem.ProductId.ToString() +
                                        " For Store: " + storeId +
                                        " Record for Carrier: " + carrier.Name +
                                        " Shipping Method Id: " + record.ShippingMethodId.ToString() +
                                        " Rate: " + rate.Value.ToString();
                                    await _logger.InsertLogAsync(LogLevel.Debug, message);
                                }

                                string carrierName = string.Empty; // Sendcloud allready containts the carrier name
                                                                   //if (carrier != null)
                                                                   //    carrierName = carrier.Name + " - ";

                                string cutOffTimeName = string.Empty;
                                if (_shippingManagerSettings.DisplayCutOffTime)
                                {
                                    var cutOffTime = await _carrierService.GetCutOffTimeByIdAsync(record.CutOffTimeId);
                                    if (cutOffTime != null)
                                        cutOffTimeName = " " + cutOffTime.Name;
                                }

                                string description = string.Empty;

                                if (!string.IsNullOrEmpty(record.Description))
                                    description = record.Description;
                                else
                                    description = shippingMethod.Description;

                                if (string.IsNullOrEmpty(description))
                                    description = shippingMethod.Name.Trim();

                                description = await _localizationService.GetResourceAsync(description);
                                if (string.IsNullOrEmpty(description))
                                    description = shippingMethod.Name.Trim();

                                description += cutOffTimeName;

                                string name = !string.IsNullOrEmpty(record.FriendlyName.Trim()) ?
                                    record.FriendlyName.Trim() : await _localizationService.GetLocalizedAsync(shippingMethod, x => x.Name);

                                int? transitDays = record.TransitDays;

                                response.ShippingOptions.Add(new ShippingOption
                                {
                                    ShippingRateComputationMethodSystemName = ShippingManagerDefaults.SendCloudSystemName,
                                    Name = carrierName + name,
                                    Description = description,
                                    Rate = rate.Value,
                                    TransitDays = transitDays,
                                    DisplayOrder = shippingMethod.DisplayOrder
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

        #endregion
    }
}
