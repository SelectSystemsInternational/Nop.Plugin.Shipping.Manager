using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Logging;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Plugin.Apollo.Integrator.Domain;
using Nop.Plugin.Apollo.Integrator.Services;
using Nop.Plugin.Shipping.Manager.Domain;
using Nop.Plugin.Shipping.Manager.Models;
using Nop.Plugin.Shipping.Manager.Settings;
using Nop.Services.Attributes;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Pickup;

using myFastway.ApiClient;
using myFastway.ApiClient.Models;

namespace Nop.Plugin.Shipping.Manager.Services
{
    /// <summary>
    /// Shipping service
    /// </summary>
    public partial class FastwayService : IFastwayService
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
        protected readonly AramexApiSettings _aramexApiSettings;
        protected readonly ShippingManagerSettings _shippingManagerSettings;
        protected readonly IRepository<Address> _addressRepository;
        protected readonly ISettingService _settingService;
        protected readonly IStateProvinceService _stateProvinceService;
        protected readonly IMeasureService _measureService;
        protected readonly EmailAccountSettings _emailAccountSettings;
        protected readonly IEmailAccountService _emailAccountService;
        protected readonly IOrderService _orderService;
        protected readonly INopFileProvider _fileProvider;
        protected readonly IShippingAddressService _shippingAddressService;

        #endregion

        #region Ctor

        public FastwayService(IAddressService addressService,
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
            AramexApiSettings aramexApiSettings,
            ShippingManagerSettings shippingManagerSettings,
            IRepository<Address> addressRepository,
            ISettingService settingService,
            IStateProvinceService stateProvinceService,
            IMeasureService measureService,
            EmailAccountSettings emailAccountSettings,
            IEmailAccountService emailAccountService,
            IOrderService orderService,
            INopFileProvider fileProvider,
            IShippingAddressService shippingAddressService)
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
            _aramexApiSettings = aramexApiSettings;
            _shippingManagerSettings = shippingManagerSettings;
            _addressRepository = addressRepository;
            _settingService = settingService;
            _stateProvinceService = stateProvinceService;
            _measureService = measureService;
            _emailAccountSettings = emailAccountSettings;
            _emailAccountService = emailAccountService;
            _orderService = orderService;
            _fileProvider = fileProvider;
            _shippingAddressService = shippingAddressService;
        }

        #endregion

        #region Utility

        private const int MIN_DIMENSION = 20; // 0 cm
        private const int MAX_LENGTH = 2000; // 2000mm = 200cm
        private const int ONE_CENTIMETER = 10; // 10mm = 1cm
        private const int ONE_METER = 1000; // 1000 mm = 1m

        private const int MIN_WEIGHT = 0; // 0 g
        private const int MAX_DEAD_WEIGHT = 25; // 25 Kg
        private const int MAX_CUBIC_WEIGHT_FACTOR = 250; // 250 Standard
        private const int MAX_CUBIC_WEIGHT = 40; // 40 Kg
        private const int ONE_KILO = 1; // 1 kg

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

        /// <returns>A task that represents the asynchronous operation</returns>
        private async Task<int> ConvertFromPrimaryMeasureDimensionAsync(decimal quantity, MeasureDimension usedMeasureDimension)
        {
            return Convert.ToInt32(Math.Ceiling(await _measureService.ConvertFromPrimaryMeasureDimensionAsync(quantity, usedMeasureDimension)));
        }

        protected virtual async Task<MeasureWeight> GatewayMeasureWeightAsync()
        {
            var usedWeight = await _measureService.GetMeasureWeightBySystemKeywordAsync(Weight.Units);
            if (usedWeight == null)
                throw new NopException("Fastway shipping service. Could not load \"{0}\" measure weight", Weight.Units);

            return usedWeight;
        }

        protected virtual async Task<MeasureDimension> GatewayMeasureDimensionAsync()
        {

            var usedMeasureDimension = await _measureService.GetMeasureDimensionBySystemKeywordAsync(Dimensions.Units);
            if (usedMeasureDimension == null)
                throw new NopException("Fastway shipping service. Could not load \"{0}\" measure dimension", Dimensions.Units);

            return usedMeasureDimension;
        }

        protected virtual async Task<decimal> GetTotalWeightAsync(GetShippingOptionRequest getShippingOptionRequest, MeasureWeight usedWeight)
        {
            var shippingService = EngineContext.Current.Resolve<IShippingService>();

            var weight = await _measureService.ConvertFromPrimaryMeasureWeightAsync(await shippingService.GetTotalWeightAsync(getShippingOptionRequest), usedWeight);

            // Allow 0 for FreeShippedItems
            if (weight == 0)
                return weight;

            return (weight < MIN_WEIGHT ? MIN_WEIGHT : weight);
        }

        protected virtual async Task<decimal> GetWeightAsync(GetShippingOptionRequest getShippingOptionRequest, MeasureWeight usedWeight)
        {
            var shippingService = EngineContext.Current.Resolve<IShippingService>();

            decimal weight = await shippingService.GetTotalWeightAsync(getShippingOptionRequest, ignoreFreeShippedItems: true);
            weight = await _measureService.ConvertFromPrimaryMeasureWeightAsync(weight, usedWeight);

            // Allow 0 for FreeShippedItems
            if (weight == 0)
                return weight;

            return (weight < MIN_WEIGHT ? MIN_WEIGHT : weight);
        }

        #endregion

        #region Shipping Rate Calculation

        /// <summary>
        /// Get rate by weight and by subtotal
        /// </summary>
        /// <param name="shippingByWeightByTotalRecord">ShippingManagerByWeightByTotal</param>
        /// <param name="quote">Quoted value</param>
        /// <param name="weight">weight value</param>
        /// <returns>The calculated rate</returns>
        public async Task<decimal?> CalculateRate(ShippingManagerCalculationOption smco, decimal rate, decimal weight)
        {
            // Formula: {[additional fixed cost] + ([order total weight] - [lower weight limit]) * [rate per weight unit]} * [charge percentage] + [order subtotal]

            if (smco.Smbwtr == null)
            {
                if (_shippingManagerSettings.LimitMethodsToCreated)
                    return null;

                return decimal.Zero;
            }

            //additional fixed cost
            var shippingTotal = smco.Smbwtr.AdditionalFixedCost + await _priceCalculationService.RoundPriceAsync(rate);

            //charge amount per weight unit
            if (smco.Smbwtr.RatePerWeightUnit > decimal.Zero)
            {
                var weightRate = Math.Max(weight - smco.Smbwtr.LowerWeightLimit, decimal.Zero);
                shippingTotal += smco.Smbwtr.RatePerWeightUnit * weightRate;
            }

            // percentage rate of subtotal
            //if (smco.Smbwtr.PercentageRateOfSubtotal > decimal.Zero)
            //{
            //    shippingTotal += Math.Round((decimal)((((float)subTotal) * ((float)smco.Smbwtr.PercentageRateOfSubtotal)) / 100f), 2);
            //}

            if (smco.Smbwtr.PercentageRateOfSubtotal > decimal.Zero)
            {
                shippingTotal += Math.Round((decimal)((((float)shippingTotal) * ((float)smco.Smbwtr.PercentageRateOfSubtotal)) / 100f), 2);
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
                        await _logger.WarningAsync($"Shipping ({ShippingManagerDefaults.AramexSystemName}). {error}");
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
                        so.ShippingRateComputationMethodSystemName = ShippingManagerDefaults.AramexSystemName;
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
            int length, height, width, quantity;
            decimal lengthTmp, widthTmp, heightTmp;

            if (getShippingOptionRequest == null)
                throw new ArgumentNullException(nameof(getShippingOptionRequest));

            var response = new GetShippingOptionResponse();

            var shippingManagerService = EngineContext.Current.Resolve<IShippingManagerService>();
            var shippingService = EngineContext.Current.Resolve<IShippingService>();

            if (getShippingOptionRequest.Items == null || !getShippingOptionRequest.Items.Any())
            {
                response.AddError("No shipment items");
                await _logger.InsertLogAsync(LogLevel.Error, "Shipping Manager - Fastway Shipping Options - No shipment items");
                return response;
            }

            MeasureWeight usedMeasureWeight = await GatewayMeasureWeightAsync();

            //choose the shipping rate calculation method
            if (_shippingManagerSettings.ShippingByWeightByTotalEnabled)
            {

                if (getShippingOptionRequest.ShippingAddress == null)
                {
                    response.AddError("Shipping address is not set");
                    await _logger.InsertLogAsync(LogLevel.Error, "Shipping Manager - Fastway Get Shipping Options - Shipping address is not set");
                    return response;
                }

                var storeId = getShippingOptionRequest.StoreId != 0 ? getShippingOptionRequest.StoreId : await _entityGroupService.GetActiveStoreScopeConfiguration();
                var warehouseId = getShippingOptionRequest.WarehouseFrom?.Id ?? 0;

                bool freeShipping = getShippingOptionRequest.Items.Any(i => i.Product.IsFreeShipping);
                bool doNotPackage = getShippingOptionRequest.Items.Any(i => i.Product.ShipSeparately) &&
                    (getShippingOptionRequest.Items.Any(i => i.OverriddenQuantity == 1 || getShippingOptionRequest.Items.Count() == 1));

                string productName = "Multiple Products";
                var product = getShippingOptionRequest.Items.FirstOrDefault().Product;
                if (getShippingOptionRequest.Items.Count() == 1)
                    productName = product.Name;

                // Get Dimensions in meters

                MeasureDimension usedMeasureDimension = await GatewayMeasureDimensionAsync();
                (widthTmp, lengthTmp, heightTmp) = await shippingService.GetDimensionsAsync(getShippingOptionRequest.Items);

                //if (widthTmp < MIN_DIMENSION)
                //    widthTmp = MIN_DIMENSION;
                //if (lengthTmp < MIN_DIMENSION)
                //    lengthTmp = MIN_DIMENSION;
                //if (heightTmp < MIN_DIMENSION)
                //    heightTmp = MIN_DIMENSION;

                length = await ConvertFromPrimaryMeasureDimensionAsync(lengthTmp, usedMeasureDimension);
                height = await ConvertFromPrimaryMeasureDimensionAsync(heightTmp, usedMeasureDimension);
                width = await ConvertFromPrimaryMeasureDimensionAsync(widthTmp, usedMeasureDimension);

                quantity = 1; // One item in package (Ship Separately already determined previously)

                //get total weight of shipped items (not including items with free shipping for rate calculation)               
                decimal weight = await GetTotalWeightAsync(getShippingOptionRequest, usedMeasureWeight);
                weight = Math.Round(weight / (decimal)ONE_KILO, 2);

                //estimate packaging only required if multiple items 

                int totalPackagesDims = 1;
                int totalPackagesWeights = 1;
                if (length > MAX_LENGTH || width > MAX_LENGTH || height > MAX_LENGTH)
                {
                    totalPackagesDims = Convert.ToInt32(Math.Ceiling((decimal)Math.Max(Math.Max(length, width), height) / MAX_LENGTH));
                }

                int maxWeight = MAX_DEAD_WEIGHT;
                if (weight > maxWeight && !doNotPackage)
                {
                    totalPackagesWeights = Convert.ToInt32(Math.Ceiling(weight / (decimal)maxWeight));
                }
                var totalPackages = totalPackagesDims > totalPackagesWeights ? totalPackagesDims : totalPackagesWeights;
                if (totalPackages == 0)
                    totalPackages = 1;
                if (totalPackages > 1)
                {
                    //recalculate dims, weight
                    weight = weight / totalPackages;
                    height = height / totalPackages;
                    width = width / totalPackages;
                    length = length / totalPackages;
                    if (weight < MIN_WEIGHT)
                        weight = MIN_WEIGHT;
                    if (height < MIN_DIMENSION)
                        height = MIN_DIMENSION;
                    if (width < MIN_DIMENSION)
                        width = MIN_DIMENSION;
                    if (length < MIN_DIMENSION)
                        length = MIN_DIMENSION;

                    quantity = totalPackages;
                }

                decimal cubicWeight = Math.Round(MAX_CUBIC_WEIGHT_FACTOR *
                    (Math.Ceiling((length / (decimal)ONE_METER) * 100) / 100) *
                        (Math.Ceiling((width / (decimal)ONE_METER) * 100) / 100) *
                            (Math.Ceiling((height / (decimal)ONE_METER) * 100) / 100), 2);

                if (_shippingManagerSettings.TestMode)
                {
                    string message = "Shipping Manager - Fastway Get Shipping Options > Product: " + productName +
                        " For Store: " + storeId + " WarehouseId: " + warehouseId.ToString() +
                        " Package Items Count: " + getShippingOptionRequest.Items.Count +
                        " Weight:" + weight.ToString() +
                        " Cubic Weight:" + cubicWeight.ToString() +
                        " Length: " + length.ToString() + " Width: " + width.ToString() + " Height: " + height.ToString();
                    await _logger.InsertLogAsync(LogLevel.Information, message);
                }

                if (length > MAX_LENGTH || width > MAX_LENGTH || height > MAX_LENGTH)
                {
                    response.AddError("Fastway Error : Package greater that  maximum dimensions");
                }
                else if (weight > MAX_DEAD_WEIGHT)
                {
                    response.AddError("Fastway Error : Package greater that maximum dead weight");
                }
                else if (cubicWeight > MAX_CUBIC_WEIGHT)
                {
                    response.AddError("Fastway Error : Package greater that maximum cubic weight");
                }
                else
                {
                    // Fastway takes the dimensions in centimeters and weight in kilograms, 
                    // so dimensions should be converted and rounded up from millimetres to centimeters,
                    // kilograms should be converted to kilograms and rounded to two decimal.
                    length = length / ONE_CENTIMETER + (length % ONE_CENTIMETER > 0 ? 1 : 0);
                    width = width / ONE_CENTIMETER + (width % ONE_CENTIMETER > 0 ? 1 : 0);
                    height = height / ONE_CENTIMETER + (height % ONE_CENTIMETER > 0 ? 1 : 0);

                    if (_shippingManagerSettings.TestMode)
                    {
                        string message = "Shipping Manager - Fastway Get Shipping Options > Product: " + productName +
                            " For Store: " + storeId + " WarehouseId: " + warehouseId.ToString() +
                            " Package Items Count: " + getShippingOptionRequest.Items.Count +
                            " Weight:" + weight.ToString() +
                            " Cubic Weight:" + cubicWeight.ToString() +
                            " Length: " + length.ToString() + " Width: " + width.ToString() + " Height: " + height.ToString();
                        await _logger.InsertLogAsync(LogLevel.Information, message);
                    }

                    string label = string.Empty;
                    string instructions = string.Empty;

                    var itemList = new List<CreateConsignmentItemModel>();

                    var item = CreateConsignmentItem(quantity, "Parcel", "P", string.Empty, string.Empty,
                        weight, length, width, height, label);

                    itemList.Add(item);

                    var customer = await _workContext.GetCurrentCustomerAsync();
                    if (customer.ShippingAddressId.HasValue)
                    {
                        var defaultShippingAddress = await _addressService.GetAddressByIdAsync(customer.ShippingAddressId.Value); // ToDo - Use default shipping address method

                        var to = await CreateToContact(getShippingOptionRequest, defaultShippingAddress);
                        if (to == null)
                        {
                            if (_shippingManagerSettings.TestMode)
                            {
                                string message = "Shipping Manager - Fastway Get Shipping Options > Product: " + productName +
                                    " Can not create shipping address from Customer (using first address in list)";
                                await _logger.InsertLogAsync(LogLevel.Error, message);
                            }

                            return response;
                        }

                        var warehouse = await shippingService.GetWarehouseByIdAsync(warehouseId);
                        var from = await CreateFromContact(getShippingOptionRequest, warehouse, instructions);
                        if (from == null)
                        {
                            if (_shippingManagerSettings.TestMode)
                            {
                                string message = "Shipping Manager - Fastway Get Shipping Options > Product: " + productName +
                                    " Warehouse: " + warehouse == null ? warehouseId.ToString() : warehouse.Name + " is used but no shipping address can be created";
                                await _logger.InsertLogAsync(LogLevel.Error, message);
                            }

                            return response;
                        }

                        var consignment = CreateConsignment(from, to, itemList, null);
                        if (from == null)
                            return response;

                        decimal? rate = null;

                        try
                        {
                            if (_aramexApiSettings.TestMode)
                            {
                                var fastwayRate = new QuoteModel();

                                fastwayRate.Price = 1;
                                fastwayRate.Tax = (decimal)0.1;
                                fastwayRate.Total = (decimal)1.1;
                            }
                            else
                            {
                                var apiClient = await GetClient(warehouseId);
                                if (apiClient == null)
                                    return response;

                                var fastwayRate = await GetQuote(apiClient, consignment);
                                if (fastwayRate.Total != 0)
                                    rate = fastwayRate.Total;

                                if (_shippingManagerSettings.TestMode)
                                {
                                    string message = "Shipping Manager - Fastway Get Shipping Option for Weight: " + weight +
                                        " Height: " + height.ToString() + " Length: " + length.ToString() + " Width: " + width.ToString() +
                                         " Rate: " + fastwayRate.Total.ToString();
                                    await _logger.InsertLogAsync(LogLevel.Information, message);
                                }

                                // var servcies = await CanConsignWithServices(apiClient, consignment);

                            }

                            if (rate.HasValue)
                            {
                                if (freeShipping)
                                    rate = 0;

                                response.ShippingOptions.Add(new ShippingOption
                                {
                                    ShippingRateComputationMethodSystemName = ShippingManagerDefaults.AramexSystemName,
                                    Name = "Aramex",
                                    Rate = rate.Value,
                                    TransitDays = 0
                                });
                            }

                        }
                        catch (BadRequestException exc)
                        {
                            string message = exc.Message;
                            string errors = string.Empty;
                            foreach (var error in exc.Errors)
                                errors += error.ToString() + " ";

                            await _logger.InsertLogAsync(LogLevel.Error, message, errors);
                        }
                    }
                }
            }

            return response;
        }

        /// <summary>
        /// Format the shipping method option
        /// </summary>
        /// <param name="shippingOption">Shipping Option</param>
        /// <param name="smco">Shipping Manager Calculation Option</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the formated shipping option
        /// </returns>
        public async Task<ShippingOption> FormatOptionDetails(ShippingOption shippingOption, ShippingManagerCalculationOption smco)
        {
            var shippingService = EngineContext.Current.Resolve<IShippingService>();

            string carrierName = "Aramex";
            var carrier = await _carrierService.GetCarrierByIdAsync(smco.Smbwtr.CarrierId);
            if (carrier != null)
                carrierName = carrier.Name;

            string cutOffTimeName = string.Empty;
            if (_shippingManagerSettings.DisplayCutOffTime)
            {
                var cutOffTime = await _carrierService.GetCutOffTimeByIdAsync(smco.Smbwtr.CutOffTimeId);
                if (cutOffTime != null)
                    cutOffTimeName = " " + cutOffTime.Name;
            }

            string description = string.Empty;
            var shippingMethod = await shippingService.GetShippingMethodByIdAsync(smco.Smbwtr.ShippingMethodId);
            if (shippingMethod != null)
            {
                description = await _localizationService.GetLocalizedAsync(shippingMethod, x => x.Description) + cutOffTimeName;

                if (!string.IsNullOrEmpty(smco.Smbwtr.FriendlyName))
                    shippingOption.Name = smco.Smbwtr.FriendlyName;
                else
                {
                    shippingOption.Name = shippingMethod.Name;
                    if (!shippingOption.Name.Contains(carrierName))
                        shippingOption.Name = carrierName + " - " + shippingMethod.Name;
                }

                if (!string.IsNullOrEmpty(description))
                    shippingOption.Description = description;

                if (smco.Smbwtr.TransitDays != 0)
                    shippingOption.TransitDays = smco.Smbwtr.TransitDays;

                shippingOption.DisplayOrder = shippingMethod.DisplayOrder;

                return shippingOption;
            }

            return null;
        }

        #endregion

        #region

        #region Lables

        /// <summary>
        /// Create a Fastway parcel
        /// </summary>
        /// <param name="shipment">Shipment</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// </returns>
        public async Task<Core.Domain.Shipping.Shipment> FastwayCreateParcelAsync(Core.Domain.Shipping.Shipment shipment)
        {
            Warehouse warehouse = null;
            int warehouseId = 0;

            if (shipment == null)
                return shipment;

            var shipmentService = EngineContext.Current.Resolve<IShipmentService>();
            var shippingService = EngineContext.Current.Resolve<IShippingService>();
            var shipmentDetailsService = EngineContext.Current.Resolve<IShipmentDetailsService>();
            var packagingOptionService = EngineContext.Current.Resolve<IPackagingOptionService>();

            var order = await _orderService.GetOrderByIdAsync(shipment.OrderId);
            if (order == null)
                return null;

            string consignmentId = shipment.Id.ToString() + "-" + order.Id.ToString();

            if (_shippingManagerSettings.TestMode)
            {
                string message = "Shipping Manager - Fastway Create Parcel > For Shipment Order: " +
                    order.CustomOrderNumber + "-" + shipment.Id.ToString();
                await _logger.InsertLogAsync(LogLevel.Debug, message);
            }

            if (_shippingManagerSettings.TestMode)
            {
                string message = "Shipping Manager - Fastway Create Consignement > For Store: " + order.StoreId.ToString() +
                    " Consignment Number: " + consignmentId;
                await _logger.InsertLogAsync(LogLevel.Information, message);
            }

            string label = string.Empty;
            string instructions = string.Empty;
            var itemList = new List<CreateConsignmentItemModel>();

            var shipmentItems = await shipmentService.GetShipmentItemsByShipmentIdAsync(shipment.Id);
            if (shipmentItems.Count() == 0 && shipmentItems.Count() > 0)
                return null;

            DateTime? scheduledPickupDate = null;
            var shipmentDetails = shipmentDetailsService.GetShipmentDetailsForShipmentId(shipment.Id);
            if (shipmentDetails == null)
                return null;
            else if (shipmentDetails.ScheduledShipDate.HasValue)
                scheduledPickupDate = shipmentDetails.ScheduledShipDate.Value;

            decimal totalWeight = 0;
            decimal length = 0, width = 0, height = 0;
            if (_shippingManagerSettings.UsePackagingSystem)
            {
                var packagingOption = packagingOptionService.GetSimplePackagingOptionById(shipmentDetails.PackagingOptionItemId);
                if (packagingOption != null)
                {
                    length = packagingOption.Length;
                    height = packagingOption.Height;
                    width = packagingOption.Width;

                    totalWeight = shipment.TotalWeight.Value;

                    if (_aramexApiSettings.TestMode)
                    {
                    string packageDimensions = $"{length:F2} x {width:F2} x {height:F2}";
                    string message = "Shipping Manager - Aramex Create Shipment > Using Packaging Dimensions : " + packageDimensions +
                            " and Weight: " + totalWeight;
                        await _logger.InsertLogAsync(LogLevel.Debug, message);
                    }
                }
            }
            else
            {
                foreach (var shipmentItem in shipmentItems)
                {
                    var orderItem = await _orderService.GetOrderItemByIdAsync(shipmentItem.OrderItemId);
                    if (orderItem != null)
                    {
                        var product = await _productService.GetProductByIdAsync(orderItem.ProductId);
                        if (product != null)
                        {
                            warehouse = await shippingService.GetWarehouseByIdAsync(shipmentItem.WarehouseId);
                            if (warehouse == null)
                            {
                                // use default shipping address
                            }
                            else
                                warehouseId = warehouse.Id;

                            length = product.Length;
                            height = product.Height;
                            width = product.Width;

                            totalWeight += product.Weight;

                            if (_aramexApiSettings.TestMode)
                            {
                                string packageDimensions = $"{length:F2} x {width:F2} x {height:F2}";
                                string message = "Shipping Manager - Canada Post Create Shipment > Using Packaging Dimensions : " + packageDimensions +
                                    " and Weight: " + totalWeight;
                                await _logger.InsertLogAsync(LogLevel.Debug, message);
                            }
                        }
                    }
                }
            }

            int quantity = 1;

            var item = await CreateConsignmentItemToSend(quantity, consignmentId, "P", string.Empty, string.Empty, totalWeight, length, width, height, label);

            itemList.Add(item);

            var customer = await _workContext.GetCurrentCustomerAsync();
            if (customer.ShippingAddressId.HasValue)
            {
                var defaultShippingAddress = await _addressService.GetAddressByIdAsync(customer.ShippingAddressId.Value); // ToDo - Use default address  

                var to = await CreateToContactForShipment(order, defaultShippingAddress);
                if (to == null)
                    return null;

                var from = await CreateFromContactForShipment(order, warehouse, instructions);
                if (from == null)
                    return null;

                var instructionsPublic = string.Empty;
                if (!shipment.AdminComment.Contains("Automatically created shipment"))
                    instructionsPublic = shipment.AdminComment;

                var consignment = CreateConsignment(from, to, itemList, instructionsPublic);
                if (from == null)
                    return null;
                try
                {
                    if (_shippingManagerSettings.TestMode)
                    {
                        string message = "Shipping Manager - Fastway Create Parcel > Consignment Id: " + consignmentId;
                        await _logger.InsertLogAsync(LogLevel.Debug, message);
                    }

                    var apiClient = await GetClient(warehouseId);
                    if (apiClient != null)
                    {
                        var persistedConsignment = await CanConsignWithServices(order.ShippingMethod, scheduledPickupDate, apiClient, consignment);
                        if (persistedConsignment.ConId != 0)
                        {
                            var createdonsignment = await GetConsignmentById(apiClient, persistedConsignment);

                            shipment.TrackingNumber = createdonsignment.ConId.ToString();

                            await shipmentService.UpdateShipmentAsync(shipment);

                            shipmentDetails = shipmentDetailsService.GetShipmentDetailsForShipmentId(shipment.Id);
                            if (shipmentDetails != null)
                            {
                                if (createdonsignment.PickupDetails != null)
                                    shipmentDetails.ScheduledShipDate = createdonsignment.PickupDetails.PreferredPickupDate;
                                shipmentDetails.Cost = createdonsignment.Total;
                                await shipmentDetailsService.UpdateShipmentDetailsAsync(shipmentDetails);
                            }

                            if (_shippingManagerSettings.TestMode)
                            {
                                string message = "Shipping Manager - Fastway Create Parcel > Parcel Updated Shipment: " + shipment.Id.ToString() +
                                    " Tracking Number: " + shipment.TrackingNumber;
                                await _logger.InsertLogAsync(LogLevel.Debug, message);
                            }

                            await GetLabelsForConsignment(apiClient, createdonsignment);
                        }
                        else
                        {
                            throw new Exception("Shipping Settings - Error createing Consignement");
                        }
                    }
                }
                catch (BadRequestException exc)
                {
                    shipment.TrackingNumber = null;
                    await shipmentService.UpdateShipmentAsync(shipment);

                    string message = exc.Message;
                    string errors = string.Empty;
                    foreach (var error in exc.Errors)
                        errors += error.ToString() + " ";

                    await _logger.InsertLogAsync(LogLevel.Error, message, errors);

                    if (errors.Contains("Message: Insufficient funds to complete this consignment"))
                        throw new NopException("Aramex Error: Insufficient funds to complete this consignment");
                    else if (errors.Contains("Code:"))
                        throw new NopException(errors);
                }
            }

            return shipment;
        }

        #endregion

        #endregion

        #region Contact Methods

        private async Task<ContactModel> CreateToContact(GetShippingOptionRequest getShippingOptionRequest, Address defaultAddress, string instructions = null)
        {

            int? countryId = getShippingOptionRequest.ShippingAddress.CountryId;
            int? stateProvinceId = getShippingOptionRequest.ShippingAddress.StateProvinceId;

            string countryCode = "AU";
            if (countryId == 0 && defaultAddress.CountryId.HasValue)
                countryId = defaultAddress.CountryId.Value;

            if (countryId.HasValue && stateProvinceId.HasValue)
            {

                var country = await _countryService.GetCountryByIdAsync(countryId.Value);
                if (country != null)
                    countryCode = country.TwoLetterIsoCode;

                string stateCode = "NSW";
                if (stateProvinceId == 0 && defaultAddress.StateProvinceId.HasValue)
                    stateProvinceId = defaultAddress.StateProvinceId.Value;

                var state = await _stateProvinceService.GetStateProvinceByIdAsync(stateProvinceId.Value);
                if (state != null)
                    stateCode = state.Abbreviation;

                string contactName = defaultAddress.FirstName + " " + defaultAddress.LastName;
                if (!string.IsNullOrWhiteSpace(getShippingOptionRequest.ShippingAddress.FirstName))
                {
                    contactName = getShippingOptionRequest.ShippingAddress.FirstName;
                    if (!string.IsNullOrWhiteSpace(getShippingOptionRequest.ShippingAddress.LastName))
                        contactName += " " + getShippingOptionRequest.ShippingAddress.LastName;
                }

                string address = defaultAddress.Address1;
                if (!string.IsNullOrWhiteSpace(defaultAddress.Address2))
                    address = address + " " + getShippingOptionRequest.ShippingAddress.Address2;

                if (!string.IsNullOrWhiteSpace(getShippingOptionRequest.ShippingAddress.Address1))
                {
                    address = getShippingOptionRequest.ShippingAddress.Address1;
                    if (!string.IsNullOrWhiteSpace(getShippingOptionRequest.ShippingAddress.Address2))
                        address = address + " " + getShippingOptionRequest.ShippingAddress.Address2;
                }

                string locality = defaultAddress.City;
                if (!string.IsNullOrWhiteSpace(getShippingOptionRequest.ShippingAddress.City))
                    locality = getShippingOptionRequest.ShippingAddress.City;

                string phoneNumber = defaultAddress.PhoneNumber;
                if (!string.IsNullOrWhiteSpace(getShippingOptionRequest.ShippingAddress.PhoneNumber))
                    phoneNumber = getShippingOptionRequest.ShippingAddress.PhoneNumber;

                string company = defaultAddress.Company;
                if (!string.IsNullOrWhiteSpace(getShippingOptionRequest.ShippingAddress.Company))
                    company = getShippingOptionRequest.ShippingAddress.Company;

                string email = defaultAddress.Email;
                if (!string.IsNullOrWhiteSpace(getShippingOptionRequest.ShippingAddress.Email))
                    email = getShippingOptionRequest.ShippingAddress.Email;

                string postCode = defaultAddress.ZipPostalCode;
                if (!string.IsNullOrWhiteSpace(getShippingOptionRequest.ShippingAddress.ZipPostalCode))
                    postCode = getShippingOptionRequest.ShippingAddress.ZipPostalCode;

                var to = CreateContact(contactName, company, email, phoneNumber, address, locality,
                    postCode, stateCode, countryCode, instructions);

                return to;
            }

            return null;
        }

        private async Task<ContactModel> CreateToContactForShipment(Order order, Address defaultAddress, string instructions = null)
        {

            if (!order.ShippingAddressId.HasValue)
                return null;

            var shippingAddress = await _addressService.GetAddressByIdAsync(order.ShippingAddressId.Value);
            if (shippingAddress == null)
                return null;

            int? countryId = shippingAddress.CountryId;
            int? stateProvinceId = shippingAddress.StateProvinceId;

            string countryCode = "AU";
            if (countryId == 0 && defaultAddress.CountryId.HasValue)
                countryId = defaultAddress.CountryId.Value;

            if (countryId.HasValue && stateProvinceId.HasValue)
            {

                var country = await _countryService.GetCountryByIdAsync(countryId.Value);
                if (country != null)
                    countryCode = country.TwoLetterIsoCode;

                string stateCode = "NSW";
                if (stateProvinceId == 0 && defaultAddress.StateProvinceId.HasValue)
                    stateProvinceId = defaultAddress.StateProvinceId.Value;

                var state = await _stateProvinceService.GetStateProvinceByIdAsync(stateProvinceId.Value);
                if (state != null)
                    stateCode = state.Abbreviation;

                string contactName = defaultAddress.FirstName + " " + defaultAddress.LastName;
                if (!string.IsNullOrWhiteSpace(shippingAddress.FirstName))
                {
                    contactName = shippingAddress.FirstName;
                    if (!string.IsNullOrWhiteSpace(shippingAddress.LastName))
                        contactName += " " + shippingAddress.LastName;
                }

                string address = defaultAddress.Address1;
                if (!string.IsNullOrWhiteSpace(defaultAddress.Address2))
                    address = address + " " + shippingAddress.Address2;

                if (!string.IsNullOrWhiteSpace(shippingAddress.Address1))
                {
                    address = shippingAddress.Address1;
                    if (!string.IsNullOrWhiteSpace(shippingAddress.Address2))
                        address = address + " " + shippingAddress.Address2;
                }

                string locality = defaultAddress.City;
                if (!string.IsNullOrWhiteSpace(shippingAddress.City))
                    locality = shippingAddress.City;

                string phoneNumber = defaultAddress.PhoneNumber;
                if (!string.IsNullOrWhiteSpace(shippingAddress.PhoneNumber))
                    phoneNumber = shippingAddress.PhoneNumber;

                string company = defaultAddress.Company;
                if (!string.IsNullOrWhiteSpace(shippingAddress.Company))
                    company = shippingAddress.Company;

                string email = defaultAddress.Email;
                if (!string.IsNullOrWhiteSpace(shippingAddress.Email))
                    email = shippingAddress.Email;

                string postCode = defaultAddress.ZipPostalCode;
                if (!string.IsNullOrWhiteSpace(shippingAddress.ZipPostalCode))
                    postCode = shippingAddress.ZipPostalCode;

                var to = CreateContact(contactName, company, email, phoneNumber, address, locality,
                    postCode, stateCode, countryCode, instructions);

                return to;
            }

            return null;
        }

        private async Task<ContactModel> CreateFromContact(GetShippingOptionRequest getShippingOptionRequest, Warehouse warehouse = null, string instructions = null)
        {

            string contactName = "Sender";
            string companyName = "Company";
            string address = "Address";
            string locality = "City";
            string phoneNumber = "9999 9999";
            string email = "sender@noname.com";
            string postCode = "9999";

            var store = await _storeContext.GetCurrentStoreAsync();
            if (store != null)
            {
                companyName = store.Name;
                if (!string.IsNullOrEmpty(store.CompanyName))
                    companyName = store.CompanyName;
                contactName = store.Name;
            }

            var emailAccount = await _emailAccountService.GetEmailAccountByIdAsync(_emailAccountSettings.DefaultEmailAccountId);
            if (emailAccount != null)
                email = emailAccount.Email;

            //ship from default address

            string countryCode = "AU";
            string stateCode = "NSW";
            if (getShippingOptionRequest.CountryFrom != null)
            {
                countryCode = getShippingOptionRequest.CountryFrom.TwoLetterIsoCode;
                stateCode = getShippingOptionRequest.StateProvinceFrom.Abbreviation;
            }

            var originAddress = await _addressService.GetAddressByIdAsync(_shippingSettings.ShippingOriginAddressId);
            if (originAddress != null)
            {
                if (!string.IsNullOrWhiteSpace(originAddress.FirstName))
                {
                    contactName = originAddress.FirstName;
                    if (!string.IsNullOrWhiteSpace(originAddress.LastName))
                        contactName += " " + originAddress.LastName;
                }
                companyName = originAddress.Company;
                if (!string.IsNullOrWhiteSpace(originAddress.Address1))
                {
                    address = originAddress.Address1;
                    if (!string.IsNullOrWhiteSpace(originAddress.Address2))
                        address = address + " " + originAddress.Address2;
                }

                if (!string.IsNullOrWhiteSpace(originAddress.PhoneNumber))
                    phoneNumber = originAddress.PhoneNumber;
                if (!string.IsNullOrWhiteSpace(originAddress.Email))
                    email = originAddress.Email;
            }

            //warehouse address
            if (warehouse != null)
            {
                originAddress = await _addressService.GetAddressByIdAsync(warehouse.AddressId);
                if (originAddress != null)
                {
                    companyName += " - " + originAddress.Company;
                    if (!string.IsNullOrWhiteSpace(originAddress.Address1))
                    {
                        address = originAddress.Address1;
                        if (!string.IsNullOrWhiteSpace(originAddress.Address2))
                            address = address + " " + originAddress.Address2;
                    }
                }
            }

            if (originAddress == null)
                return null;

            if (!string.IsNullOrWhiteSpace(originAddress.City))
                locality = originAddress.City;
            if (!string.IsNullOrWhiteSpace(originAddress.ZipPostalCode))
                postCode = originAddress.ZipPostalCode;

            var to = CreateContact(contactName, companyName, email, phoneNumber, address, locality,
                    postCode, stateCode, countryCode, instructions);

            return to;
        }

        private async Task<ContactModel> CreateFromContactForShipment(Order order, Warehouse warehouse = null, string instructions = null)
        {
            string contactName = "Sender";
            string companyName = "Company";
            string address = "Address";
            string locality = "City";
            string countryCode = "AU";
            string stateCode = "State";
            string phoneNumber = "9999 9999";
            string email = "sender@noname.com";
            string postCode = "9999";

            var store = await _storeContext.GetCurrentStoreAsync();
            if (store != null)
            {
                companyName = store.Name;
                if (!string.IsNullOrEmpty(store.CompanyName))
                    companyName = store.CompanyName;
                contactName = store.Name;
            }

            var emailAccount = await _emailAccountService.GetEmailAccountByIdAsync(_emailAccountSettings.DefaultEmailAccountId);
            if (emailAccount != null)
                email = emailAccount.Email;

            //ship from default address

            var originAddress = await _addressService.GetAddressByIdAsync(_shippingSettings.ShippingOriginAddressId);
            if (originAddress != null)
            {

                var country = await _countryService.GetCountryByIdAsync(originAddress.CountryId.Value);
                if (country != null)
                    countryCode = country.TwoLetterIsoCode;

                var state = await _stateProvinceService.GetStateProvinceByIdAsync(originAddress.StateProvinceId.Value);
                if (state != null)
                    stateCode = state.Abbreviation;

                if (!string.IsNullOrWhiteSpace(originAddress.FirstName))
                {
                    contactName = originAddress.FirstName;
                    if (!string.IsNullOrWhiteSpace(originAddress.LastName))
                        contactName += " " + originAddress.LastName;
                }

                companyName = originAddress.Company;
                if (!string.IsNullOrWhiteSpace(originAddress.Address1))
                {
                    address = originAddress.Address1;
                    if (!string.IsNullOrWhiteSpace(originAddress.Address2))
                        address = address + " " + originAddress.Address2;
                }

                if (!string.IsNullOrWhiteSpace(originAddress.PhoneNumber))
                    phoneNumber = originAddress.PhoneNumber;
                if (!string.IsNullOrWhiteSpace(originAddress.Email))
                    email = originAddress.Email;
            }

            //warehouse address
            if (warehouse != null)
            {
                originAddress = await _addressService.GetAddressByIdAsync(warehouse.AddressId);
                if (originAddress != null)
                {
                    companyName += " - " + originAddress.Company;
                    if (!string.IsNullOrWhiteSpace(originAddress.Address1))
                    {
                        address = originAddress.Address1;
                        if (!string.IsNullOrWhiteSpace(originAddress.Address2))
                            address = address + " " + originAddress.Address2;
                    }
                }
            }

            if (originAddress == null)
                return null;

            if (!string.IsNullOrWhiteSpace(originAddress.City))
                locality = originAddress.City;
            if (!string.IsNullOrWhiteSpace(originAddress.ZipPostalCode))
                postCode = originAddress.ZipPostalCode;

            var to = CreateContact(contactName, companyName, email, phoneNumber, address, locality,
                    postCode, stateCode, countryCode, instructions);

            return to;
        }

        #endregion

        #region Fastway Methods

        //https://github.com/mindfulsoftware/myFastway.ApiClient/wiki/Endpoints%EA%9E%89-Consignments#consign

        protected const string CONSIGNMENTS_BASE_ROUTE = "consignments";
        protected const string CONTACTS_BASE_ROUTE = "contacts";

        public async Task<QuoteModel> GetStandardQuote(ApiClient apiClient)
        {

            var itemList = new List<CreateConsignmentItemModel>();
            var item = CreateConsignmentItem(1, "Parcel", "P", string.Empty, string.Empty, 5, 25, 10, 10, string.Empty);

            itemList.Add(item);

            var consignment = CreateConsignment("Tony Receiver", "Tony's Tools", "tony@tonystools.com.au", "0400 123 456", "73 Katoomba St", "Katoomba",
                "2780", "NSW", "AU", itemList);

            var result = await CanQuote(apiClient, consignment);

            return result;
        }

        public async Task<QuoteModel> GetQuote(ApiClient apiClient, CreateConsignmentModel consignment)
        {
            var result = await CanQuote(apiClient, consignment);
            return result;
        }

        protected async Task<PersistedConsignmentModel> Consign(ApiClient apiClient, CreateConsignmentModel consignment)
        {
            return await apiClient.PostSingle<PersistedConsignmentModel>(CONSIGNMENTS_BASE_ROUTE, consignment);
        }

        protected CreateConsignmentModel GetStandardTestConsignment()
        {
            var result = new CreateConsignmentModel
            {
                ConTypeId = ConTypeId.Standard,
                To = new ContactModel
                {
                    ContactName = "Tony Receiver",
                    BusinessName = "Tony's Tools",
                    Email = "tony@tonystools.com.au",
                    PhoneNumber = "0400 123 456",
                    Address = new AddressModel
                    {
                        StreetAddress = "73 Katoomba St",
                        Locality = "Katoomba",
                        PostalCode = "2780",
                        StateOrProvince = "NSW",
                        Country = "AU"
                    },
                },
                Items = new[]
                {
                    new CreateConsignmentItemModel
                    {
                        Quantity = 1,
                        PackageType = "P",
                        Reference = "Parcel",
                        WeightDead = 5,
                        Length = 25,
                        Width = 10,
                        Height = 10
                    },
                    new CreateConsignmentItemModel
                    {
                        Quantity = 1,
                        PackageType = "S",
                        Reference = "Satchel",
                        SatchelSize = "A4"
                    }
                }
            };
            return result;
        }

        protected CreateConsignmentItemModel CreateConsignmentItem(int quantity, string reference, string packageType, string myItemCode,
            string satchelSize, decimal weightDead, decimal length, decimal width, decimal height, string label, int? myItemId = null)
        {

            var result = new CreateConsignmentItemModel
            {
                Quantity = (byte)quantity,
                Reference = reference,
                PackageType = packageType,
                MyItemId = myItemId,
                MyItemCode = myItemCode,
                SatchelSize = satchelSize,
                WeightDead = weightDead,
                Length = length,
                Width = width,
                Height = height,
                Label = label
            };

            return result;
        }

        protected async Task<CreateConsignmentItemModel> CreateConsignmentItemToSend(int quantity, string reference, string packageType, string myItemCode,
            string satchelSize, decimal deadWeight, decimal length, decimal width, decimal height, string label, int? myItemId = null)
        {

            MeasureDimension usedMeasureDimension = await GatewayMeasureDimensionAsync();
            int convertedLength = await ConvertFromPrimaryMeasureDimensionAsync(length, usedMeasureDimension);
            int convertedHeight = await ConvertFromPrimaryMeasureDimensionAsync(height, usedMeasureDimension);
            int convertedWidth = await ConvertFromPrimaryMeasureDimensionAsync(width, usedMeasureDimension);

            // Fastway takes the dimensions in centimeters and weight in kilograms, 
            // so dimensions should be converted and rounded up from millimetres to centimeters,
            // kilograms should be converted to kilograms and rounded to two decimal.
            convertedLength = convertedLength / ONE_CENTIMETER + (convertedLength % ONE_CENTIMETER > 0 ? 1 : 0);
            convertedHeight = convertedHeight / ONE_CENTIMETER + (convertedHeight % ONE_CENTIMETER > 0 ? 1 : 0);
            convertedWidth = convertedWidth / ONE_CENTIMETER + (convertedWidth % ONE_CENTIMETER > 0 ? 1 : 0);

            MeasureWeight usedMeasureWeight = await GatewayMeasureWeightAsync();
            var convertedWeight = await _measureService.ConvertFromPrimaryMeasureWeightAsync(deadWeight, usedMeasureWeight);

            var result = new CreateConsignmentItemModel
            {
                Quantity = (byte)quantity,
                Reference = reference,
                PackageType = packageType,
                MyItemId = myItemId,
                MyItemCode = myItemCode,
                SatchelSize = satchelSize,
                WeightDead = convertedWeight,
                Length = convertedLength,
                Width = convertedWidth,
                Height = convertedHeight,
                Label = label
            };

            return result;
        }

        protected CreateConsignmentModel CreateConsignment(string contactName, string businessName, string email, string phoneNumber,
            string streetAddress, string locality, string postCode, string stateCode, string countryCode, List<CreateConsignmentItemModel> items)
        {
            var result = new CreateConsignmentModel
            {
                ConTypeId = ConTypeId.Standard,
                To = new ContactModel
                {
                    DisplayName = contactName,
                    ContactName = contactName,
                    BusinessName = businessName,
                    Email = email,
                    PhoneNumber = phoneNumber,
                    Address = new AddressModel
                    {
                        StreetAddress = streetAddress,
                        Locality = locality,
                        PostalCode = postCode,
                        StateOrProvince = stateCode,
                        Country = countryCode
                    },
                },

                Items = items
            };

            return result;
        }

        protected CreateConsignmentModel CreateConsignment(ContactModel from, ContactModel to, List<CreateConsignmentItemModel> items, string instructionsPublic)
        {
            var result = new CreateConsignmentModel
            {
                ConTypeId = ConTypeId.Standard,
                To = to,
                From = from,
                Items = items,
                InstructionsPublic = instructionsPublic
            };

            return result;
        }

        protected ContactModel CreateContact(string contactName, string businessName, string email, string phoneNumber,
            string streetAddress, string locality, string postCode, string stateCode, string countryCode, string instructions)
        {
            var contact = new ContactModel
            {
                DisplayName = contactName,
                ContactName = contactName,
                BusinessName = businessName,
                Email = email,
                PhoneNumber = phoneNumber,
                Address = new AddressModel
                {
                    StreetAddress = streetAddress,
                    Locality = locality,
                    PostalCode = postCode,
                    StateOrProvince = stateCode,
                    Country = countryCode
                },
                Instructions = instructions
            };

            return contact;
        }

        protected ContactModel GetContact()
        {
            return new ContactModel
            {
                ContactName = "Sarah Sender",
                BusinessName = "Sarahs' Stuff",
                Email = "sarah@sarahs-stuff.com.au",
                PhoneNumber = "0400 000 111",
                Address = new AddressModel
                {
                    StreetAddress = "333 Collins St",
                    Locality = "Melbourne",
                    PostalCode = "3000",
                    StateOrProvince = "VIC",
                    Country = "AU"
                }
            };
        }

        #endregion

        #region Fastway Api Methods

        public async Task<ApiClient> GetClient(int warehouseId)
        {
            string apiVersion = "1.0";
            ApiClient apiClient = null;

            string apiKey = _aramexApiSettings.ApiKey;
            string apiSecret = _aramexApiSettings.ApiSecret;

            apiClient = new ApiClient(_aramexApiSettings.AuthenticationURL, apiKey, apiSecret, string.Empty, true, _aramexApiSettings.HostURL, apiVersion);

            if (warehouseId == 0)
            {

                if (!string.IsNullOrEmpty(_aramexApiSettings.AuthenticationURL) && !string.IsNullOrEmpty(_aramexApiSettings.HostURL) &&
                    !string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(apiSecret))
                {
                    apiClient = new ApiClient(_aramexApiSettings.AuthenticationURL, apiKey, apiSecret, string.Empty, true, _aramexApiSettings.HostURL, apiVersion);
                }
            }
            else
            {
                GenericAttribute ga = (await _genericAttributeService.GetAttributesForEntityAsync(warehouseId, "Warehouse")).FirstOrDefault(m => m.Key == "ShippingManagerWarehouseSetting");
                ShippingManagerWarehouseSetting kws = null;
                if (ga != null)
                    kws = JsonConvert.DeserializeObject<ShippingManagerWarehouseSetting>(ga.Value);

                if (kws != null)
                {
                    apiKey = kws.ApiKey;
                    apiSecret = kws.ApiSecret;

                    if (!string.IsNullOrEmpty(_aramexApiSettings.AuthenticationURL) && !string.IsNullOrEmpty(_aramexApiSettings.HostURL) &&
                        !string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(apiSecret))
                    {
                        apiClient = new ApiClient(_aramexApiSettings.AuthenticationURL, apiKey, apiSecret, string.Empty, true, _aramexApiSettings.HostURL, apiVersion);
                    }
                }
            }

            return apiClient;
        }

        public ApiClient GetClient()
        {
            ApiClient apiClient = null;
            string apiVersion = "1.0";
            //string baseAddress = "https://api.myfastway.com.au";
            //string authority = "https://identity.fastway.org";
            //string clientId = "";
            //string secret = "";
            //bool requireHttps = true;
            //string scope = string.Empty;

            if (!string.IsNullOrEmpty(_aramexApiSettings.AuthenticationURL) && !string.IsNullOrEmpty(_aramexApiSettings.HostURL) &&
                !string.IsNullOrEmpty(_aramexApiSettings.ApiKey) && !string.IsNullOrEmpty(_aramexApiSettings.ApiSecret))
            {
                apiClient = new ApiClient(_aramexApiSettings.AuthenticationURL, _aramexApiSettings.ApiKey,
                    _aramexApiSettings.ApiSecret, string.Empty, true, _aramexApiSettings.HostURL, apiVersion);
            }

            return apiClient;
        }

        public ApiClient GetClient(string apiKey, string apiSecret)
        {
            ApiClient apiClient = null;
            string apiVersion = "1.0";

            //string baseAddress = "https://api.myfastway.com.au";
            //string authority = "https://identity.fastway.org";
            //string clientId = "";
            //string secret = "";
            //bool requireHttps = true;
            //string scope = string.Empty;

            if (!string.IsNullOrEmpty(_aramexApiSettings.AuthenticationURL) && !string.IsNullOrEmpty(_aramexApiSettings.HostURL) &&
                !string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(apiSecret))
            {
                apiClient = new ApiClient(_aramexApiSettings.AuthenticationURL, apiKey, apiSecret, string.Empty, true, _aramexApiSettings.HostURL, apiVersion);
            }

            return apiClient;
        }

        public async Task<QuoteModel> GetTestQuote()
        {
            var apiClient = GetClient();
            if (apiClient != null)
            {
                var consignment = GetStandardTestConsignment();
                var quote = await apiClient.PostSingle<QuoteModel>($"{CONSIGNMENTS_BASE_ROUTE}/quote", consignment);
                return quote;
            }

            return null;
        }

        public async Task<List<ServiceModel>> GetServices()
        {
            var apiClient = GetClient();
            if (apiClient != null)
            {
                var services = await apiClient.GetCollection<ServiceModel>("consignment-services");
                var hasAvailableServices = services.Any();
                if (hasAvailableServices)
                {
                    return services.ToList();
                }
            }

            return null;
        }

        /// <summary>
        /// Update sendcloud shipment rate configuration
        /// </summary>
        /// <param name="client">SendCloudApi client</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// </returns>
        public async Task AramexUpdateAsync(List<ServiceModel> services, bool updateShippingMethods)
        {

            var customer = await _workContext.GetCurrentCustomerAsync();
            var storeId = await _entityGroupService.GetActiveStoreScopeConfiguration();

            int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

            var shippingManagerService = EngineContext.Current.Resolve<IShippingManagerService>();
            var shippingService = EngineContext.Current.Resolve<IShippingService>();

            //Create the SendCloud shipping methods list for sender address

            int displayOrder = 1;
            string shippingMethodList = string.Empty;

            var countryCode = await _shippingAddressService.GetDefaultCountryCodeAsync(customer);
            int countryId = await _shippingAddressService.GetCountryIdFromCodeAsync(countryCode);

            if (countryId != 0)
            {
                var carrier = await _carrierService.GetCarrierByNameAsync("Aramex");
                if (carrier == null)
                {
                    carrier = new Carrier();
                    carrier.Name = "Aramex";
                    carrier.AdminComment = "Carrier added from SendCloud Update";
                    carrier.ShippingRateComputationMethodSystemName = ShippingManagerDefaults.AramexSystemName;
                    carrier.AddressId = (await _shippingAddressService.CreateAddressAsync("The", "Manager", string.Empty, carrier.Name, 0, 0, string.Empty,
                        string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty)).Id;
                    carrier.Active = true;
                    await _carrierService.InsertCarrierAsync(carrier);
                }

                var serviceOptions = new List<ServiceItemModel>();

                foreach (var service in services)
                {
                    if (service.Code.Equals("DELOPT"))
                    {
                        foreach (var option in service.Items)
                        {
                            var serviceName = GetServiceName(option.Code);
                            var rateName = "Aramex - Economy - " + serviceName;
                            var shippingMethodName = "Aramex - Economy - " + GetServiceCode(serviceName);
                            var price = option.Price;

                            var shippingMethod = await shippingManagerService.GetShippingMethodByNameAsync(shippingMethodName);
                            if (shippingMethod == null)
                            {
                                shippingMethod = new ShippingMethod();
                                shippingMethod.Name = shippingMethodName;
                                shippingMethod.Description = option.Description;
                                shippingMethod.DisplayOrder = displayOrder++;                                

                                await shippingService.InsertShippingMethodAsync(shippingMethod);

                                if (shippingMethodList != string.Empty)
                                    shippingMethodList += ", ";

                                shippingMethodList += shippingMethodName;

                                bool showHidden = true;

                                if (showHidden)
                                {

                                    var language = await _workContext.GetWorkingLanguageAsync();

                                    int stateId = 0;
                                    string zip = null;

                                    var shippingManagerByWeightByTotal = (await shippingManagerService.GetRecordsAsync(shippingMethod.Id, storeId,
                                                        vendorId, 0, carrier.Id, countryId, stateId, zip)).FirstOrDefault();
                                    if (shippingManagerByWeightByTotal == null)
                                    {
                                        shippingManagerByWeightByTotal = new ShippingManagerByWeightByTotal();

                                        shippingManagerByWeightByTotal.FriendlyName = rateName;
                                        shippingManagerByWeightByTotal.ShippingMethodId = shippingMethod.Id;
                                        shippingManagerByWeightByTotal.CarrierId = carrier.Id;
                                        shippingManagerByWeightByTotal.WarehouseId = 0;
                                        shippingManagerByWeightByTotal.VendorId = vendorId;
                                        shippingManagerByWeightByTotal.WeightFrom = MIN_WEIGHT;
                                        shippingManagerByWeightByTotal.WeightTo = MAX_DEAD_WEIGHT;
                                        shippingManagerByWeightByTotal.CalculateCubicWeight = false;
                                        shippingManagerByWeightByTotal.CubicWeightFactor = 0;
                                        shippingManagerByWeightByTotal.OrderSubtotalFrom = 0;
                                        shippingManagerByWeightByTotal.OrderSubtotalTo = 1000000;
                                        shippingManagerByWeightByTotal.CountryId = countryId;
                                        shippingManagerByWeightByTotal.StateProvinceId = stateId;
                                        shippingManagerByWeightByTotal.TransitDays = 1;
                                        shippingManagerByWeightByTotal.SendFromAddressId = 0;
                                        shippingManagerByWeightByTotal.AdditionalFixedCost = price;
                                        shippingManagerByWeightByTotal.Active = true;
                                        shippingManagerByWeightByTotal.DisplayOrder = displayOrder;

                                        await shippingManagerService.InsertShippingByWeightRecordAsync(shippingManagerByWeightByTotal);
                                    }

                                    serviceOptions.Add(option);
                                }
                            }
                        }
                    }
                    else if (service.Code.Equals("PRIORITY"))
                    {
                        foreach (var option in serviceOptions)
                        {
                            var serviceName = GetServiceName(option.Code);
                            var rateName = "Aramex - Priority - " + serviceName;
                            var shippingMethodName = "Aramex - Priority - " + GetServiceCode(serviceName);
                            var price = 3.30M + option.Price;

                            var shippingMethod = await shippingManagerService.GetShippingMethodByNameAsync(shippingMethodName);
                            if (shippingMethod == null)
                            {
                                shippingMethod = new ShippingMethod();
                                shippingMethod.Name = shippingMethodName;
                                shippingMethod.Description = option.Description;
                                shippingMethod.DisplayOrder = displayOrder++;

                                await shippingService.InsertShippingMethodAsync(shippingMethod);

                                if (shippingMethodList != string.Empty)
                                    shippingMethodList += ", ";

                                shippingMethodList += shippingMethodName;

                                bool showHidden = true;

                                if (showHidden)
                                {

                                    var language = await _workContext.GetWorkingLanguageAsync();

                                    int stateId = 0;
                                    string zip = null;

                                    var shippingManagerByWeightByTotal = (await shippingManagerService.GetRecordsAsync(shippingMethod.Id, storeId,
                                                        vendorId, 0, carrier.Id, countryId, stateId, zip)).FirstOrDefault();
                                    if (shippingManagerByWeightByTotal == null)
                                    {
                                        shippingManagerByWeightByTotal = new ShippingManagerByWeightByTotal();

                                        shippingManagerByWeightByTotal.FriendlyName = rateName;
                                        shippingManagerByWeightByTotal.ShippingMethodId = shippingMethod.Id;
                                        shippingManagerByWeightByTotal.CarrierId = carrier.Id;
                                        shippingManagerByWeightByTotal.WarehouseId = 0;
                                        shippingManagerByWeightByTotal.VendorId = vendorId;
                                        shippingManagerByWeightByTotal.WeightFrom = MIN_WEIGHT;
                                        shippingManagerByWeightByTotal.WeightTo = MAX_DEAD_WEIGHT;
                                        shippingManagerByWeightByTotal.CalculateCubicWeight = false;
                                        shippingManagerByWeightByTotal.CubicWeightFactor = 0;
                                        shippingManagerByWeightByTotal.OrderSubtotalFrom = 0;
                                        shippingManagerByWeightByTotal.OrderSubtotalTo = 1000000;
                                        shippingManagerByWeightByTotal.CountryId = countryId;
                                        shippingManagerByWeightByTotal.StateProvinceId = stateId;
                                        shippingManagerByWeightByTotal.TransitDays = 1;
                                        shippingManagerByWeightByTotal.SendFromAddressId = 0;
                                        shippingManagerByWeightByTotal.AdditionalFixedCost = price;
                                        shippingManagerByWeightByTotal.Active = true;
                                        shippingManagerByWeightByTotal.DisplayOrder = displayOrder;

                                        await shippingManagerService.InsertShippingByWeightRecordAsync(shippingManagerByWeightByTotal);
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

        bool IsPriorityService(string shippingMethod)
        {
            if (shippingMethod.Contains("Priority"))
                return true;
            return false;
        }

        string GetServiceName(string code)
        {
            string name = string.Empty;

            if (code == "STN")
                name = "Standard Service";
            else if (code == "ATL")
                name = "Authority to Leave";
            else if (code == "SGR")
                name = "Signature Delivery";
            else if (code == "PIN")
                name = "Secure PIN";

            return name;
        }

        string GetServiceCode(string shippingMethod)
        {
            string code = string.Empty;

            if (shippingMethod.Contains("Standard Service"))
                code = "STN";
            else if (shippingMethod.Contains("Authority to Leave"))
                code = "ATL";
            else if (shippingMethod.Contains("Signature Delivery"))
                code = "SGR";
            else if (shippingMethod.Contains("Secure PIN"))
                code = "PIN";

            return code;
        }

        public async Task<QuoteModel> GetTestQuote(string apiKey, string apiSecret)
        {
            var apiClient = GetClient(apiKey, apiSecret);
            if (apiClient != null)
            {
                var consignment = GetStandardTestConsignment();
                var quote = await apiClient.PostSingle<QuoteModel>($"{CONSIGNMENTS_BASE_ROUTE}/quote", consignment);
                return quote;
            }

            return null;
        }

        public async Task<QuoteModel> CanQuote(ApiClient apiClient, CreateConsignmentModel consignment)
        {
            var quote = await apiClient.PostSingle<QuoteModel>($"{CONSIGNMENTS_BASE_ROUTE}/quote", consignment);
            return quote;
        }

        public async Task CanConsignWithPickupDates(ApiClient apiClient, CreateConsignmentModel consignment)
        {
            consignment.PickupTypeId = PickupType.Required;
            consignment.PickupDetails = new PickupDetails
            {
                PreferredPickupDate = DateTime.Today.AddDays(7),
                PreferredPickupCycleId = PickupCycle.AM
            };

            var result = await apiClient.PostSingle<PersistedConsignmentModel>(CONSIGNMENTS_BASE_ROUTE, consignment);
        }

        public async Task<PersistedConsignmentModel> CannotSetPickupDateGreaterThan30Days(ApiClient apiClient, CreateConsignmentModel consignment)
        {

            consignment.PickupTypeId = PickupType.Required;
            consignment.PickupDetails = new PickupDetails
            {
               PreferredPickupDate = DateTime.Today.AddDays(31),
               PreferredPickupCycleId = PickupCycle.AM
            };

            var result = await apiClient.PostSingle<PersistedConsignmentModel>(CONSIGNMENTS_BASE_ROUTE, consignment);
            return result;
        }

        public async Task CanConsignWithFuturePickupDates(ApiClient apiClient, CreateConsignmentModel consignment)
        {
            consignment.PickupTypeId = PickupType.Future;
            consignment.FromInstructionsPublic = "Initial pickup instructions";
            var result = await apiClient.PostSingle<PersistedConsignmentModel>(CONSIGNMENTS_BASE_ROUTE, consignment);

            var updateRequest = new UpdatePickupDetailsRequest
            {
                PreferredPickupDate = DateTime.Today.AddDays(7),
                PreferredPickupCycleId = PickupCycle.PM,
                FromInstructionsPublic = "Updated pickup instructions"
            };

            await apiClient.PostSingle($"{CONSIGNMENTS_BASE_ROUTE}/{result.ConId}/pickup-details", updateRequest);
            var loadedConsignment = await apiClient.GetSingle<PersistedConsignmentModel>($"{CONSIGNMENTS_BASE_ROUTE}/{result.ConId}");

        }

        public async Task<PersistedConsignmentModel> CanConsignWithServices(string shippingMethod, DateTime? scheduledPickupDate, ApiClient apiClient, CreateConsignmentModel consignment)
        {
            var servicesList = new List<CreateConsignmentServiceModel>();

            var services = await apiClient.GetCollection<ServiceModel>("consignment-services");
            var hasAvailableServices = services.Any();
            if (hasAvailableServices)
            {
                if (IsPriorityService(shippingMethod))
                {
                    var service = new CreateConsignmentServiceModel
                    {
                        ServiceCode = "PRIORITY",
                        ServiceItemCode = "PRIORITY"
                    };

                    servicesList.Add(service);
                }

                var code = GetServiceCode(shippingMethod);
                if (!string.IsNullOrEmpty(code))
                {
                    var service = new CreateConsignmentServiceModel
                    {
                        ServiceCode = "DELOPT",
                        ServiceItemCode = code
                    };

                    servicesList.Add(service);
                }
            }

            consignment.Services = servicesList;

            var fastwayRate = await GetQuote(apiClient, consignment); // for test purposes 

            if (scheduledPickupDate.HasValue)
            {
                var pickupCycle = PickupCycle.AM;
                if (scheduledPickupDate.Value.ToString("tt") == "PM")
                    pickupCycle = PickupCycle.PM;

                consignment.PickupTypeId = PickupType.Required;
                consignment.PickupDetails = new PickupDetails
                {
                    PreferredPickupDate = scheduledPickupDate.Value,
                    PreferredPickupCycleId = pickupCycle
                };
            }

            var persistedConsignment = await Consign(apiClient, consignment);
            return persistedConsignment;
        }

        public async Task<PersistedConsignmentModel> GetConsignmentById(ApiClient apiClient, PersistedConsignmentModel persistedConsignment)
        {
            var loadedConsignment = await apiClient.GetSingle<PersistedConsignmentModel>($"{CONSIGNMENTS_BASE_ROUTE}/{persistedConsignment.ConId}");
            return loadedConsignment;
        }

        public async Task<PersistedConsignmentModel> CanConsignWithExistingContact(ApiClient apiClient, CreateConsignmentModel consignment)
        {
            var persistedContact = await apiClient.PostSingle<ContactModel>(CONTACTS_BASE_ROUTE, consignment.To);
            consignment.To = new ContactModel { ContactId = persistedContact.ContactId };

            var persistedConsignment = await apiClient.PostSingle<PersistedConsignmentModel>(CONSIGNMENTS_BASE_ROUTE, consignment);
            return persistedConsignment;
        }

        public async Task QuoteMatchesConsignment(ApiClient apiClient, CreateConsignmentModel consignment)
        {
            var quote = await apiClient.PostSingle<QuoteModel>($"{CONSIGNMENTS_BASE_ROUTE}/quote", consignment);
            var persistedConsignment = await apiClient.PostSingle<PersistedConsignmentModel>(CONSIGNMENTS_BASE_ROUTE, consignment);
        }

        public async Task GetByDateRangeReturnsMultiple(ApiClient apiClient, CreateConsignmentModel consignment)
        {
            var persistedConsignment = await Consign(apiClient, consignment);
            var dateFormat = DateTime.Now.ToString("yyyy-MM-dd");
            var listItems = await apiClient.GetCollection<ConsignmentListItem>($"{CONSIGNMENTS_BASE_ROUTE}?fromDate={dateFormat}&toDate={dateFormat}&pageNumber=0&pageSize=10");
        }

        public async Task<List<DeletedReasonModel>> GetDeletedReasons(ApiClient apiClient)
        {
            var deletedReasons = await apiClient.GetCollection<DeletedReasonModel>($"{CONSIGNMENTS_BASE_ROUTE}/deleted-reasons");
            return deletedReasons.ToList();
        }

        public async Task<System.Net.Http.HttpResponseMessage> DeleteConsignment(ApiClient apiClient, PersistedConsignmentModel persistedConsignment, DeletedReasonModel deletedReason)
        {
            var deleteResponse = await apiClient.Delete($"{CONSIGNMENTS_BASE_ROUTE}/{persistedConsignment.ConId}/reason/{deletedReason.Id}");
            return deleteResponse;
        }

        public async Task<System.Net.Http.HttpResponseMessage> UnDeleteConsignment(ApiClient apiClient, PersistedConsignmentModel persistedConsignment, DeletedReasonModel deletedReason)
        {
            var undeleteResponse = await apiClient.PutSingle($"{CONSIGNMENTS_BASE_ROUTE}/{persistedConsignment.ConId}/undelete", null);
            return undeleteResponse;
        }
        
        public async Task<IEnumerable<ConsignmentSearchItem>> GetPendingConsignments(ApiClient apiClient, int pageIndex, int pageSize)
        {
            var pending = await apiClient.GetCollection<ConsignmentSearchItem>($"{CONSIGNMENTS_BASE_ROUTE}/pending?pageNumber="
                + pageIndex.ToString() + "&pageSize=" + pageSize.ToString());
            return pending;
        }

        public async Task CanConsignUsingMyItemId(ApiClient apiClient)
        {
            // save my item
            var newMyItem = new FastwayItemsModel
            {
                Code = "BB",
                Height = 10,
                Length = 20,
                Name = "Big Box",
                WeightDead = 5,
                Width = 30
            };

            var persistedMyItem = await apiClient.PostSingle<FastwayItemsModel>("my-items", newMyItem);
            //Assert.True(persistedMyItem.MyItemId > 0);

            // consign with my item
            var consignment = GetStandardTestConsignment();
            consignment.Items = new[]
            {
                new CreateConsignmentItemModel
                {
                    Quantity = 1,
                    MyItemId = persistedMyItem.MyItemId
                },
            };

            var persistedConsignment = await Consign(apiClient, consignment);
        }

        public async Task GetLabelsForConsignment(ApiClient apiClient, PersistedConsignmentModel persistedConsignment)
        {
            await WriteLabelsPDF(apiClient, persistedConsignment.ConId, "A4");
            await WriteLabelsPDF(apiClient, persistedConsignment.ConId, "4x6");
        }

        public async Task GetLabelsForConsignmentLabels(ApiClient apiClient, PersistedConsignmentModel persistedConsignment)
        {
            var label = persistedConsignment.Items.First().Label;
            await WriteLabelsPDF(apiClient, persistedConsignment.ConId, "A4", label);
            await WriteLabelsPDF(apiClient, persistedConsignment.ConId, "4x6", label);
        }

        public async Task WriteLabelsPDF(ApiClient apiClient, int conId, string pageSize, string label = null)
        {
            var labelPart = string.IsNullOrWhiteSpace(label) ? string.Empty : $"/{label}";
            var labelsPdf = await apiClient.GetBytes($"{CONSIGNMENTS_BASE_ROUTE}/{conId}/labels{labelPart}?pageSize={pageSize}");
            var suffix = string.IsNullOrWhiteSpace(label) ? conId.ToString() : label;

            //var fileName = $@"Labels_{pageSize}_{suffix}_{CommonHelper.GenerateRandomDigitCode(4)}.pdf";
            var fileName = $@"Labels_{pageSize}_{suffix}.pdf";

            var filePath = _fileProvider.Combine(_fileProvider.MapPath("~/wwwroot/files/exportimport"), fileName);

            await File.WriteAllBytesAsync(filePath, labelsPdf);
        }

        #endregion
    }
}
