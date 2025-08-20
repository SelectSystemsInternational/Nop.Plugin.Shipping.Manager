using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Logging;
using Nop.Core.Domain.Shipping;
using Nop.Core.Infrastructure;
using Nop.Plugin.Apollo.Integrator;
using Nop.Plugin.Shipping.CanadaPost;
using Nop.Plugin.Shipping.Manager.Domain;
using Nop.Plugin.Shipping.Manager.Services;
using Nop.Plugin.Shipping.Manager.Settings;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Plugins;
using Nop.Services.Security;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Tracking;
using Nop.Services.Stores;
using Nop.Web.Framework.Menu;

namespace Nop.Plugin.Shipping.Manager
{
    /// <summary>
    /// Fixed rate or by weight shipping computation method 
    /// </summary>
    public class ShippingManagerPlugin : BasePlugin, IShippingRateComputationMethod, IAdminMenuPlugin
    {

        #region Fields

        protected readonly ShippingManagerSettings _shippingManagerSettings;
        protected readonly ILocalizationService _localizationService;
        protected readonly IPriceCalculationService _priceCalculationService;
        protected readonly ISettingService _settingService;
        protected readonly IShippingService _shippingService;
        protected readonly IStoreContext _storeContext;
        protected readonly IWebHelper _webHelper;
        protected readonly IShippingManagerService _shippingManagerService;
        protected readonly ICarrierService _carrierService;
        protected readonly IWorkContext _workContext;
        protected readonly IShippingManagerInstallService _shippingManagerInstallService;
        protected readonly ILogger _logger;
        protected readonly ISendcloudService _sendcloudService;
        protected readonly IMeasureService _measureService;
        protected readonly IProductService _productService;
        protected readonly IShoppingCartService _shoppingCartService;
        protected readonly ICountryService _countryService;
        protected readonly IGenericAttributeService _genericAttributeService;
        protected readonly IStoreService _storeService;
        protected readonly ShippingSettings _shippingSettings;
        protected readonly IAddressService _addressService;
        protected readonly IEncryptionService _encryptionService;

        SystemHelper _systemHelper = new SystemHelper();

        #endregion

        #region Ctor

        public ShippingManagerPlugin(ShippingManagerSettings shippingManagerSettings,
            ILocalizationService localizationService,
            IPriceCalculationService priceCalculationService,
            ISettingService settingService,
            IShippingService shippingService,
            IStoreContext storeContext,
            IWebHelper webHelper,
            IShippingManagerService shippingManagerService,
            ICarrierService carrierService,
            IWorkContext workContext,
            IShippingManagerInstallService shippingManagerInstallService,
            ILogger logger,
            ISendcloudService sendcloudService,
            IMeasureService measureService,
            IProductService productService,
            IShoppingCartService shoppingCartService,
            ICountryService countryService,
            IGenericAttributeService genericAttributeService,
            IStoreService storeService,
            ShippingSettings shippingSettings,
            IAddressService addressService,
            IEncryptionService encryptionService)
        {
            _shippingManagerSettings = shippingManagerSettings;
            _localizationService = localizationService;
            _priceCalculationService = priceCalculationService;
            _settingService = settingService;
            _shippingService = shippingService;
            _storeContext = storeContext;
            _webHelper = webHelper;
            _shippingManagerService = shippingManagerService;
            _carrierService = carrierService;
            _workContext = workContext;
            _shippingManagerInstallService = shippingManagerInstallService;
            _logger = logger;
            _sendcloudService = sendcloudService;
            _measureService = measureService;
            _productService = productService;
            _shoppingCartService = shoppingCartService;
            _countryService = countryService;
            _genericAttributeService = genericAttributeService;
            _storeService = storeService;
            _shippingSettings = shippingSettings;
            _addressService = addressService;
            _encryptionService = encryptionService;
        }

        #endregion

        #region Utility

        private const int MIN_WEIGHT = 500; // 500 g

        protected class Weight
        {
            public static string Units => "kg";

            public int Value { get; set; }
        }

        protected class Dimensions
        {
            public static string Units => "meters";

            public int Length { get; set; }

            public int Width { get; set; }

            public int Height { get; set; }
        }

        /// <returns>A task that represents the asynchronous operation</returns>
        private async Task<decimal> ConvertFromPrimaryMeasureDimensionAsync(decimal quantity, MeasureDimension usedMeasureDimension)
        {
            return await _measureService.ConvertFromPrimaryMeasureDimensionAsync(quantity, usedMeasureDimension);
        }

        protected virtual async Task<MeasureWeight> GatewayMeasureWeightAsync()
        {
            var usedWeight = await _measureService.GetMeasureWeightBySystemKeywordAsync(Weight.Units);
            if (usedWeight == null)
                throw new NopException("Shipping Manager shipping service. Could not load \"{0}\" measure weight", Weight.Units);

            return usedWeight;
        }

        protected virtual async Task<MeasureDimension> GatewayMeasureDimensionAsync()
        {

            var usedMeasureDimension = await _measureService.GetMeasureDimensionBySystemKeywordAsync(Dimensions.Units);
            if (usedMeasureDimension == null)
                throw new NopException("Australia Post shipping service. Could not load \"{0}\" measure dimension", Dimensions.Units);

            return usedMeasureDimension;
        }

        protected virtual async Task<decimal> GetWeightAsync(decimal weight, MeasureWeight usedWeight)
        {
            var convertedWeight = Convert.ToInt32(Math.Ceiling(await _measureService.ConvertFromPrimaryMeasureWeightAsync(weight, usedWeight)));

            // Allow 0 for FreeShippedItems
            if (weight == 0)
                return convertedWeight;

            return (convertedWeight < MIN_WEIGHT ? MIN_WEIGHT : convertedWeight);
        }

        /// <summary>
        /// Gets the transit days
        /// </summary>
        /// <param name="shippingMethodId">Shipping method ID</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the ransit days
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
        /// <param name="shippingMethodId">Shipping method ID</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the rate
        /// </returns>
        private async Task<decimal> GetRateAsync(int shippingMethodId)
        {
            //Get the vendor
            int vendorId = 0;
            if (await _workContext.GetCurrentVendorAsync() != null)
                vendorId = (await _workContext.GetCurrentVendorAsync()).Id;

            return await _settingService.GetSettingByKeyAsync<decimal>(string.Format(ShippingManagerDefaults.FIXED_RATE_SETTINGS_KEY, vendorId, shippingMethodId));
        }

        #endregion

        #region Shipping Rate Calculation

        /// <summary>
        ///  Gets available shipping options
        /// </summary>
        /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        /// <returns>Represents a response of getting shipping rate options</returns>
        public async Task<GetShippingOptionResponse> GetShippingOptionsAsync(GetShippingOptionRequest getShippingOptionRequest)
        {
            var store = await _storeContext.GetCurrentStoreAsync();
            var customer = await _workContext.GetCurrentCustomerAsync();

            decimal length, height, width;
            decimal lengthTmp, widthTmp, heightTmp, weight, deadWeight;

            if (getShippingOptionRequest == null)
                throw new ArgumentNullException(nameof(getShippingOptionRequest));

            var response = new GetShippingOptionResponse();

            var shippingManagerService = EngineContext.Current.Resolve<IShippingManagerService>();
            var shippingService = EngineContext.Current.Resolve<IShippingService>();

            if (getShippingOptionRequest.Items == null || !getShippingOptionRequest.Items.Any())
            {
                response.AddError("No shipment items");
                await _logger.InsertLogAsync(LogLevel.Error, "Shipping Manager - Get Shipping Options - No shipment items");
                return response;
            }

            //choose the shipping rate calculation method
            if (_shippingManagerSettings.ShippingByWeightByTotalEnabled)
            {
                //shipping rate calculation by products weight

                if (getShippingOptionRequest.ShippingAddress == null)
                {
                    response.AddError("Shipping address is not set");
                    await _logger.InsertLogAsync(LogLevel.Error, "Shipping Manager - Get Shipping Options - Shipping address is not set");
                    return response;
                }

                var storeId = getShippingOptionRequest.StoreId != 0 ? getShippingOptionRequest.StoreId : await _shippingManagerService.GetActiveStoreScopeConfiguration();
                var countryId = getShippingOptionRequest.ShippingAddress.CountryId ?? 0;
                var stateProvinceId = getShippingOptionRequest.ShippingAddress.StateProvinceId ?? 0;
                var zip = getShippingOptionRequest.ShippingAddress.ZipPostalCode;
                var warehouseId = getShippingOptionRequest.WarehouseFrom?.Id ?? 0;

                MeasureWeight usedMeasureWeight = await GatewayMeasureWeightAsync();

                //get total weight of shipped items (excluding items with free shipping)
                var totalWeight = await _shippingService.GetTotalWeightAsync(getShippingOptionRequest, ignoreFreeShippedItems: true);
                totalWeight = await _measureService.ConvertFromPrimaryMeasureWeightAsync(totalWeight, usedMeasureWeight);

                //get subtotal of shipped items
                var subTotal = decimal.Zero;
                foreach (var packageItem in getShippingOptionRequest.Items)
                {
                    if (await _shippingService.IsFreeShippingAsync(packageItem.ShoppingCartItem))
                        continue;

                    var unitPrice = await shippingManagerService.GetPackagePrice(packageItem.ShoppingCartItem);

                    subTotal += unitPrice;
                }

                if (_shippingManagerSettings.TestMode)
                {
                    string message = "Shipping Manager - Get Shipping Options > For Store: " + storeId + 
                        " CountryId: " + countryId.ToString() + " StateId: " + stateProvinceId.ToString() +
                        " WarehouseId: " + warehouseId.ToString() + " Zip: " + zip + " Weight:" + totalWeight.ToString() + " SubTotal: " + subTotal.ToString() +
                        " Package Items Count: " + getShippingOptionRequest.Items.Count;
                    await _logger.InsertLogAsync(LogLevel.Information, message);
                }

                //foreach (var packageItem in getShippingOptionRequest.Items)
                //{
                //    if (await _shippingService.IsFreeShippingAsync(packageItem.ShoppingCartItem))
                //        continue;

                string productName = "Multiple Products";
                var product = getShippingOptionRequest.Items.FirstOrDefault().Product;
                if (getShippingOptionRequest.Items.Count() == 1)
                    productName = product.Name;

                // Convert package weight and dimensions for deadweight search
                deadWeight = totalWeight;
                (widthTmp, lengthTmp, heightTmp) = await _shippingService.GetDimensionsAsync(getShippingOptionRequest.Items);

                MeasureDimension usedMeasureDimension = await GatewayMeasureDimensionAsync();
                length = await ConvertFromPrimaryMeasureDimensionAsync(lengthTmp, usedMeasureDimension);
                height = await ConvertFromPrimaryMeasureDimensionAsync(heightTmp, usedMeasureDimension);
                width = await ConvertFromPrimaryMeasureDimensionAsync(widthTmp, usedMeasureDimension);

                int vendorId = product.VendorId;

                var shippingMethodId = await _genericAttributeService.GetAttributeAsync<int>(customer, ShippingManagerDefaults.CURRENT_SHIPPING_METHOD_SELECTOR, store.Id);

                var matchedRecords = await shippingManagerService.FindMethodsAsync(storeId, vendorId, warehouseId, 0, countryId, stateProvinceId, zip, null, subTotal, shippingMethodId);

                if (_shippingManagerSettings.TestMode)
                {
                    string message = "Shipping Manager - Get Shipping Options > For Store: " + storeId + 
                        " Product: " + productName +
                        " WarehouseId: " + warehouseId.ToString() +
                        " Package Items Count: " + getShippingOptionRequest.Items.Count +
                        " Records Found: " + matchedRecords.Count() + " for Vendor: " + vendorId +
                        " Dead Weight:" + deadWeight.ToString() +
                        " Length: " + length.ToString() + " Width: " + width.ToString() + " Height: " + height.ToString();
                    await _logger.InsertLogAsync(LogLevel.Information, message);
                }

                var foundRecords = new List<ShippingManagerByWeightByTotal>();
                foreach (var record in matchedRecords)
                {
                    weight = 0;
                    if (record.CalculateCubicWeight)
                    {
                        weight = length * width * height * record.CubicWeightFactor;
                        if (weight >= record.WeightFrom && weight <= record.WeightTo)
                            foundRecords.Add(record);
                    }
                    else
                    {
                        if (deadWeight >= record.WeightFrom && deadWeight <= record.WeightTo)
                            foundRecords.Add(record);
                    }
                }

                foreach (var record in foundRecords)
                {
                    var carrier = await _carrierService.GetCarrierByIdAsync(record.CarrierId);
                    if (carrier != null)
                    {
                        if (carrier.ShippingRateComputationMethodSystemName == ShippingManagerDefaults.SystemName)
                        {
                            var shippingMethod = await _shippingService.GetShippingMethodByIdAsync(record.ShippingMethodId);

                            if (shippingMethod == null)
                                continue;

                            var rate = CalculateRate(record, deadWeight);
                            if (!rate.HasValue)
                                continue;

                            if (_shippingManagerSettings.TestMode)
                            {
                                string message = "Shipping Manager - Get Shipping Options > For Store : " + storeId +
                                    " Record for Carrier: " + carrier.Name +
                                    " Shipping Method Id: " + record.ShippingMethodId.ToString() +
                                    " Rate: " + rate.Value.ToString();
                                await _logger.InsertLogAsync(LogLevel.Debug, message);
                            }

                            string carrierName = string.Empty;
                            if (carrier != null)
                                carrierName = carrier.Name + " - ";

                            string cutOffTimeName = string.Empty;
                            if (_shippingManagerSettings.DisplayCutOffTime)
                            {
                                var cutOffTime = await _carrierService.GetCutOffTimeByIdAsync(record.CutOffTimeId);
                                if (cutOffTime != null)
                                    cutOffTimeName = " " + cutOffTime.Name;
                            }

                            string description = await _localizationService.GetLocalizedAsync(shippingMethod, x => x.Description) + cutOffTimeName;

                            int? transitDays = record.TransitDays;

                            response.ShippingOptions.Add(new ShippingOption
                            {
                                ShippingRateComputationMethodSystemName = ShippingManagerDefaults.SystemName,
                                Name = carrierName + await _localizationService.GetLocalizedAsync(shippingMethod, x => x.Name),
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
                response.ShippingOptions = await (await _shippingService.GetAllShippingMethodsAsync(restrictByCountryId)).SelectAwait(async shippingMethod => new ShippingOption
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
        /// Get rate by weight and by total
        /// </summary>
        /// <param name="subTotal">Subtotal</param>
        /// <param name="weight">Weight</param>
        /// <param name="shippingMethodId">Shipping method ID</param>
        /// <param name="storeId">Store ID</param>
        /// <param name="warehouseId">Warehouse ID</param>
        /// <param name="countryId">Country ID</param>
        /// <param name="stateProvinceId">State/Province ID</param>
        /// <param name="zip">Zip code</param>
        /// <returns>Rate</returns>
        private async Task<ShippingManagerByWeightByTotal?> GetMethod(decimal subTotal, decimal weight, int shippingMethodId, int storeId, int vendorId, int warehouseId, int countryId, int stateProvinceId, string zip, bool active)
        {
            var shippingByWeightByTotalRecord = await _shippingManagerService.FindRecordsAsync(shippingMethodId, storeId, vendorId, 
                warehouseId, countryId, stateProvinceId, zip, weight, subTotal, active);
            if (shippingByWeightByTotalRecord == null)
            {
                if (_shippingManagerSettings.LimitMethodsToCreated)
                    return null;

                return null;
            }

            return shippingByWeightByTotalRecord;
        }   

        /// <summary>
        /// Get rate by weight and by total
        /// </summary>
        /// <param name="subTotal">Subtotal</param>
        /// <param name="weight">Weight</param>
        /// <param name="shippingMethodId">Shipping method ID</param>
        /// <param name="storeId">Store ID</param>
        /// <param name="warehouseId">Warehouse ID</param>
        /// <param name="countryId">Country ID</param>
        /// <param name="stateProvinceId">State/Province ID</param>
        /// <param name="zip">Zip code</param>
        /// <returns>Rate</returns>
        private decimal? CalculateRate(ShippingManagerByWeightByTotal shippingByWeightByTotalRecord, decimal weight)
        {
            // Formula: {[additional fixed cost] + ([order total weight] - [lower weight limit]) * [rate per weight unit]} * [charge percentage] + [order subtotal]

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
            //if (shippingByWeightByTotalRecord.PercentageRateOfSubtotal > decimal.Zero)
            //{
            //    shippingTotal += Math.Round((decimal)((((float)subTotal) * ((float)shippingByWeightByTotalRecord.PercentageRateOfSubtotal)) / 100f), 2);
            //}

            if (shippingByWeightByTotalRecord.PercentageRateOfSubtotal > decimal.Zero)
            {
                shippingTotal += Math.Round((decimal)((((float)shippingTotal) * ((float)shippingByWeightByTotalRecord.PercentageRateOfSubtotal)) / 100f), 2);
            }

            return Math.Max(shippingTotal, decimal.Zero);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets fixed shipping rate (if shipping rate computation method allows it and the rate can be calculated before checkout).
        /// </summary>
        /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the fixed shipping rate; or null in case there's no fixed shipping rate
        /// </returns>
        public async Task<decimal?> GetFixedRateAsync(GetShippingOptionRequest getShippingOptionRequest)
        {
            if (getShippingOptionRequest == null)
                throw new ArgumentNullException(nameof(getShippingOptionRequest));

            //if the "shipping calculation by weight" method is selected, the fixed rate isn't calculated
            if (_shippingManagerSettings.ShippingByWeightByTotalEnabled)
                return null;

            var restrictByCountryId = getShippingOptionRequest.ShippingAddress?.CountryId;
            var rates = await (await _shippingService.GetAllShippingMethodsAsync(restrictByCountryId))
                .SelectAwait(async shippingMethod => await GetRateAsync(shippingMethod.Id)).Distinct().ToListAsync();

            //return default rate if all of them equal
            if (rates.Count == 1)
                return rates.FirstOrDefault();

            return null;
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/ShippingManagerConfiguration/Configure";
        }

        /// <summary>
        /// Get associated shipment tracker
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipment tracker
        /// </returns>
        public Task<IShipmentTracker> GetShipmentTrackerAsync()
        {
            return Task.FromResult<IShipmentTracker>(new ShippingManagerShipmentTracker(_shippingManagerService,
                _shippingManagerSettings, _logger, _addressService));
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public override async System.Threading.Tasks.Task InstallAsync()
        {
            var store = await _storeService.GetStoreByIdAsync(0);
            if (store == null)
                store = await _storeContext.GetCurrentStoreAsync();

            //settings

            string dateInstall = DateTime.Now.Date.ToString();
            var demoDateInstall = DateTime.Parse(dateInstall).AddDays(28);
            Guid pKeyInstall = _systemHelper.DateToGuid(demoDateInstall);
            string publicKeyInstall = pKeyInstall.ToString();
            string urlInstall = _systemHelper.GetDomainNameFromHost(store.Url);
            string privateKeyInstall = _encryptionService.EncryptText(urlInstall, publicKeyInstall);

            var shippingManagerSettings = await _settingService.LoadSettingAsync<ShippingManagerSettings>();

            if (shippingManagerSettings.Enabled)
            {
                // Settings remain from last installation
                shippingManagerSettings.Enabled = false;
                await _settingService.SaveSettingAsync(shippingManagerSettings);
            }
            else
            {

                shippingManagerSettings = new ShippingManagerSettings
                {
                    Enabled = false,
                    PublicKey = publicKeyInstall,
                    PrivateKey = privateKeyInstall,
                    DeleteTablesonUninstall = false,
                    ShippingByWeightByTotalEnabled = true,
                    CreateShippingOptionRequests = false,
                    InternationalOperationsEnabled = true,
                    DisplayCutOffTime = false,
                    OrderByDate = false,
                    FontFileName = "Calibri Regular.ttf",
                    ProcessingMode = ProcessingMode.Volume,
                    TestMode = false,
                    UsePackagingSystem = true,
                    PackagingOptions = "Box1:200:200:200:1.8;Box2:100.5:200.5:300.5:2.8;",
                    UseWarehousesConfiguration = false,
                    OrderManagerOperations = false,
                    ManifestShipments = false,
                    SetAsShippedWhenAnnounced = true,
                    DisplayManualOperations = false,
                    ShippingOptionDisplay = ShippingOptionDisplay.AddCountryToDisplay,
                    CheckoutOperationMode = CheckoutOperationMode.Default,
                    EncryptServicePointPost = true
                };

                int countryId = 0;
                var countries = await _countryService.GetAllCountriesAsync(showHidden: true);
                if (countries != null)
                {
                    int shippingOriginAddressId = _shippingSettings.ShippingOriginAddressId;
                    var shippingOriginAddress = await _addressService.GetAddressByIdAsync(shippingOriginAddressId);
                    if (shippingOriginAddress != null)
                    {
                        if (shippingOriginAddress.CountryId.HasValue)
                            countryId = shippingOriginAddress.CountryId.Value;
                    }
                    else
                        await _logger.InsertLogAsync(LogLevel.Error, "Shipping Settings - Shipping Origin not Set");

                    if (countryId == 0)
                    {
                        var country = countries.Where(x => x.Name == "United States of America").FirstOrDefault();
                        if (country != null)
                            countryId = country.Id;

                        shippingManagerSettings.DefaultCountryId = countryId;
                    }
                }

                shippingManagerSettings.AvailableApiServices =
                    "Shipping.Sendcloud," +
                    "Shipping.Aramex," +
                    "Shipping.AustraliaPost," +
                    "Shipping.CanadaPost";

                //Create settings for default store
                var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();

                // Setup Canada Post Default Settings
                shippingManagerSettings.ApiServices = "Shipping.CanadaPost";
                CanadaPostApiSettings canadaPostApiSettings = new CanadaPostApiSettings();

                canadaPostApiSettings.TestMode = true;
                canadaPostApiSettings.ApiKey = "Not Used";
                canadaPostApiSettings.ApiSecret = "Not Used";
                canadaPostApiSettings.HostURL = "Not Used";
                canadaPostApiSettings.AuthenticationURL = "https://ct.soa-gw.canadapost.ca";

                var canadaPostSettings = await _settingService.LoadSettingAsync<CanadaPostSettings>();
                if (canadaPostSettings != null)
                {
                    if (canadaPostSettings.ApiKey != null)
                    {
                        var key = canadaPostSettings.ApiKey.Split(":");
                        if (key.Count() == 2)
                        {
                            canadaPostApiSettings.Username = key[0];
                            canadaPostApiSettings.Password = key[1];
                        }

                        canadaPostApiSettings.CustomerNumber = canadaPostSettings.CustomerNumber;
                        canadaPostApiSettings.MoBoCN = canadaPostSettings.CustomerNumber;
                        canadaPostApiSettings.ContractId = canadaPostSettings.ContractId;
                        canadaPostApiSettings.ShipmentOptions = "DC";
                    }
                }

                await _settingService.SaveSettingAsync(canadaPostApiSettings, storeScope);

                // Setup Sendcloud Default Settings

                shippingManagerSettings.ApiServices += ",Shipping.Sendcloud";
                var sendcloudApiSettings = new SendcloudApiSettings();

                sendcloudApiSettings.HostURL = "https://panel.sendcloud.sc/api/v2/";
                sendcloudApiSettings.AuthenticationURL = "Not Used";
                sendcloudApiSettings.ApiKey = "Please Enter";
                sendcloudApiSettings.ApiSecret = "Please Enter";

                await _settingService.SaveSettingAsync(sendcloudApiSettings, storeScope);


                // Setup Aramex Default Settings
                shippingManagerSettings.ApiServices += ",Shipping.Aramex";
                var aramexApiSettings = new AramexApiSettings();

                aramexApiSettings.HostURL = "https://api.myfastway.com.au";
                aramexApiSettings.AuthenticationURL = "https://identity.fastway.org";
                aramexApiSettings.ApiKey = "fw-fl2-SYD3450061-8ae1bdf78adc";
                aramexApiSettings.ApiSecret = "26c89fa8-9cd7-46e3-a753-b09d7dcc9ba5";

                await _settingService.SaveSettingAsync(aramexApiSettings, storeScope);

                // Save Shipping Manager settings

                await _settingService.SaveSettingAsync(shippingManagerSettings);

                //locales
                await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
                {
                    ["Plugins.Shipping.Manager.Menu"] = "Shipping Manager",
                    ["Plugins.Shipping.Manager.Configuration"] = "Configuration",
                    ["Plugins.Shipping.Manager.Enabled"] = "Enabled",
                    ["Plugins.Shipping.Manager.ShippingSettings"] = "Shipping Settings",
                    ["Plugins.Shipping.Manager.Enabled.Hint"] = "Enable the pluign to setup the system and provide services",
                    ["Plugins.Shipping.Manager.Fields.PublicKey"] = "Licence Key",
                    ["Plugins.Shipping.Manager.Fields.PublicKey.Hint"] = "Enter the Enter the Licence Key supplied after plugin purchase",
                    ["Plugins.Shipping.Manager.Fields.PrivateKey"] = "Private Key",
                    ["Plugins.Shipping.Manager.Fields.PrivateKey.Hint"] = "Enter the Private Licence Key supplied via email after plugin purchase",
                    ["Plugins.Shipping.Manager.Fields.DeleteTablesonUninstall"] = "Delete Tables on Uninstall",
                    ["Plugins.Shipping.Manager.Fields.DeleteTablesonUninstall.Hint"] = "Select to Delete Tables when Plugin Uninstalled",
                    ["Plugins.Shipping.Manager.Fields.DeleteConfigurationDataonUninstall"] = "Delete Configuration on Uninstall",
                    ["Plugins.Shipping.Manager.Fields.DeleteConfigurationDataonUninstall.Hint"] = "Select to Delete the Configuration when Plugin Uninstalled",
                    ["Plugins.Shipping.Manager.Fields.InternationalOperationsEnabled"] = "International Operations Enabled",
                    ["Plugins.Shipping.Manager.Fields.InternationalOperationsEnabled.Hint"] = "Select to enable International Operations mode to display the County and activate associated operations",
                    ["Plugins.Shipping.Manager.Fields.OrderByDate"] = "Order by Date",
                    ["Plugins.Shipping.Manager.Fields.OrderByDate.Hint"] = "Order Sales and Shipment records by Date Increasing",
                    ["Plugins.Shipping.Manager.Fields.TestMode"] = "Test Mode",
                    ["Plugins.Shipping.Manager.Fields.TestMode.Hint"] = "Enable the operation of this Shipping Method in Test Mode",
                    ["Plugins.Shipping.Manager.Fields.ProcessingMode"] = "Processing Mode by",
                    ["Plugins.Shipping.Manager.Fields.ProcessingMode.Hint"] = "The system will process the Shipment Packages by the method selected",
                    ["Plugins.Shipping.Manager.Fields.UsePackagingSystem"] = "Use Packaging System",
                    ["Plugins.Shipping.Manager.Fields.UsePackagingSystem.Hint"] = "Select this option to use packaging options when creating shipments",
                    ["Plugins.Shipping.Manager.Fields.PackagingOptions"] = "Packaging Options",
                    ["Plugins.Shipping.Manager.Fields.PackagingOptions.Hint"] = "Enter the packaging options in the format Name:Length:Width:Height:Weight seperated by ;",

                    ["Plugins.Shipping.Manager.Fields.EncryptServicePointPost"] = "Encrypt Service Point Post",
                    ["Plugins.Shipping.Manager.Fields.EncryptServicePointPost.Hint"] = "Select to Encrypt Service Point Posts",
                    ["Plugins.Shipping.Manager.Fields.SetAsShippedWhenAnnounced"] = "Shipped when Announced",
                    ["Plugins.Shipping.Manager.Fields.SetAsShippedWhenAnnounced.Hint"] = "The order status will be set to Shipped when a parcel is announced",
                    ["Plugins.Shipping.Manager.Fields.ShippingOptionDisplay"] = "Shipping Option Display",
                    ["Plugins.Shipping.Manager.Fields.ShippingOptionDisplay.Hint"] = "Select the option for displaying the shipping method when shipping delivery address is unknown",
                    ["Plugins.Shipping.Manager.Fields.CheckoutOperationMode"] = "Checkout Operation Mode",
                    ["Plugins.Shipping.Manager.Fields.CheckoutOperationMode.Hint"] = "Select the mode of operation for the checkout",
                    ["Plugins.Shipping.Manager.Fields.DisplayManualOperations"] = "Display Manual Operations",
                    ["Plugins.Shipping.Manager.Fields.DisplayManualOperations.Hint"] = "Select the option to display the manual operations buttons",

                    ["Plugins.Shipping.Manager.Fields.ApiServices"] = "Available Api Services",
                    ["Plugins.Shipping.Manager.Fields.ApiServices.Hint"] = "Select the list of Api Services available",

                    ["Plugins.Shipping.Manager.Configure.ApiServiceIds.NoAvailableApiServices"] = "No Available list of Api Services",

                    ["Plugins.Shipping.Manager.Api.Fields.ApiKey"] = "Api Key",
                    ["Plugins.Shipping.Manager.Api.Fields.ApiKey.Hint"] = "Enter the Api Key from the Integration Settings",
                    ["Plugins.Shipping.Manager.Api.Fields.ApiSecret"] = "Api Secret",
                    ["Plugins.Shipping.Manager.Api.Fields.ApiSecret.Hint"] = "Enter the Api Secret from the Integration Settings",
                    ["Plugins.Shipping.Manager.Api.Fields.Username"] = "Username",
                    ["Plugins.Shipping.Manager.Api.Fields.Username.Hint"] = "Enter the Username from the Api Key in the Developer Settings",
                    ["Plugins.Shipping.Manager.Api.Fields.Password"] = "Password",
                    ["Plugins.Shipping.Manager.Api.Fields.Password.Hint"] = "Enter the Password from the Api Key in the Developer Settings",
                    ["Plugins.Shipping.Manager.Api.Fields.AuthenticationURL"] = "Authentication URL",
                    ["Plugins.Shipping.Manager.Api.Fields.AuthenticationURL.Hint"] = "Enter the Authentication URL from the Integration Settings",
                    ["Plugins.Shipping.Manager.Api.Fields.HostURL"] = "Host URL",
                    ["Plugins.Shipping.Manager.Api.Fields.HostURL.Hint"] = "Enter the Host URL from the Integration Settings",
                    ["Plugins.Shipping.Manager.Api.Fields.UsePackagingSystem"] = "Use Packaging System",
                    ["Plugins.Shipping.Manager.Api.Fields.UsePackagingSystem.Hint"] = "Enable to use the Packaging System for shipments",
                    ["Plugins.Shipping.Manager.Api.Fields.CustomerNumber"] = "Customer Number",
                    ["Plugins.Shipping.Manager.Api.Fields.CustomerId.Hint"] = "Enter the Customer Number",
                    ["Plugins.Shipping.Manager.Api.Fields.MoBoCN"] = "Mailed on Behalf of",
                    ["Plugins.Shipping.Manager.Api.Fields.MoBoCN.Hint"] = "Enter the Mailed on Behalf of Customer Number",
                    ["Plugins.Shipping.Manager.Api.Fields.ContractId"] = "Contract Id",
                    ["Plugins.Shipping.Manager.Api.Fields.ContractId.Hint"] = "Enter your Canada Post ContractId",
                    ["Plugins.Shipping.Manager.Api.Fields.ShipmentOptions"] = "Default Shipment Options",
                    ["Plugins.Shipping.Manager.Api.Fields.ShipmentOptions.Hint"] = "Enter the default shipment options seperated by a comma i.e. DC,XX,YY Note: DC = Delivery Confirmation",

                    ["Plugins.Shipping.Manager.Api.Fields.TestMode"] = "Api Test Mode",
                    ["Plugins.Shipping.Manager.Api.Fields.TestMode.Hint"] = "Enable to operate this Api in Test Mode",

                    ["Shipping.Sendcloud"] = "Shipping Sendcloud",
                    ["Plugins.Shipping.Manager.Configuration"] = "Configuration",
                    ["Plugins.Shipping.Manager.Operation.Setup"] = "Shipping Operation Configuration",
                    ["Plugins.Shipping.Manager.Configure.Setup"] = "Shipping Manager Configuration",
                    ["Plugins.Shipping.Manager.Configure.Note.Restart"] = "Please restart the application once the configuration has been modified.",
                    ["Plugins.Shipping.Manager.Configure.ApiServiceIds.NoAvailableApiServices"] = "No Available list of Api Services",

                    ["Plugins.Shipping.Manager.Configure.Sendcloud"] = "SendClound Configuration",
                    ["Plugins.Shipping.Manager.Configure.Sendcloud.Instructions"] = "Enter the following settings for you SendCloud Integration",
                    ["Plugins.Shipping.Manager.Configure.Sendcloud.ConfigurationUpdated"] = "Sendcloud Configuration Updated",
                    ["Plugins.Shipping.Manager.Configure.Sendcloud.ConfigurationVerified"] = "Sendcloud Configuration Verified",
                    ["Plugins.Shipping.Manager.Configure.Sendcloud.LoadConfiguration"] = "Load Sendcloud Configuration",
                    ["Plugins.Shipping.Manager.Configure.Sendcloud.LoadCarriers"] = "Load Carriers",
                    ["Plugins.Shipping.Manager.Configure.Sendcloud.ValidateConfiguration"] = "Validate Sendcloud Configuration",
                    ["Plugins.Shipping.Manager.Configure.ApiServiceIds.NoAvailableApiServices"] = "No Available list of Api Services",
                    ["Plugins.Shipping.Manager.SendcloudServicePoint"] = "Sendcloud Service Point",

                    ["Plugins.Shipping.Manager.Shipment.SelectShipment"] = "Please select a shipment to process",
                    ["Plugins.Shipping.Manager.Shipment.CreateShipment"] = "Create Shipment",
                    ["Plugins.Shipping.Manager.Shipment.CreateShipmentSelected"] = "Create Shipment (selected)",
                    ["Plugins.Shipping.Manager.Shipment.NoShipmentOption"] = "No Shipment Option Available",
                    ["Plugins.Shipping.Manager.Shipment.ParcelsCreated"] = "Total Parcels Created:",
                    ["Plugins.Shipping.Manager.Shipment.ErrorCheckLog"] = "Shipment not Created - Check Error Log",

                    ["Plugins.Shipping.Manager.Shipment.Sendcloud.ParcelCreated"] = "Sendcloud Parcel Created",
                    ["Plugins.Shipping.Manager.Shipment.Updated"] = "The shipment has been added updated",

                    ["Shipping.Aramex"] = "Shipping Aramex",
                    ["Plugins.Shipping.Manager.Configure.Aramex"] = "Aramex Configuration",
                    ["Plugins.Shipping.Manager.Configure.Aramex.Instructions"] = "Overall system configuration - Enter the following settings from your Aramex MyFastway Api settings",
                    ["Plugins.Shipping.Manager.Configure.Aramex.Test"] = "Test Configuration",
                    ["Plugins.Shipping.Manager.Configure.Aramex.Tested"] = "Configuration Tested : Quote = ",
                    ["Plugins.Shipping.Manager.Configure.Aramex.LoadConfiguration"] = "Load Sendcloud Configuration",

                    ["Shipping.CanadaPost"] = "Shipping Canada Post",
                    ["Plugins.Shipping.Manager.Configure.CanadaPost"] = "Canada Post Configuration",
                    ["Plugins.Shipping.Manager.Configure.CanadaPost.Instructions"] = "Overall system configuration - Enter the following settings from your Canada Post Api settings",
                    ["Plugins.Shipping.Manager.Configure.CanadaPost.ConfigurationUpdated"] = "Canada Post Configuration Updated",
                    ["Plugins.Shipping.Manager.Configure.CanadaPost.ConfigurationVerified"] = "Canada Post Configuration Verified",
                    ["Plugins.Shipping.Manager.Configure.CanadaPost.LoadConfiguration"] = "Load Canada Post Configuration",
                    ["Plugins.Shipping.Manager.Configure.CanadaPost.ValidateConfiguration"] = "Validate CanadaPost Configuration",
                    ["Plugins.Shipping.Manager.Configure.CanadaPost.Test"] = "Test Connection",
                    ["Plugins.Shipping.Manager.Configure.CanadaPost.Tested"] = "Configuration Tested : Quote = ",

                    ["Plugins.Shipping.Manager.Api.Fields.TestResult"] = "API Test Result",
                    ["Plugins.Shipping.Manager.Api.Fields.TestResult.Hint"] = "The result following a call to the selected API will be displayed",

                    ["Plugins.Shipping.Manager.ByWeightbyTotal.SampleExcel"] = "Sample Excel",
                    ["Plugins.Shipping.Manager.ByWeightbyTotal.ExcelfileUploadTip"] = "The file that is exported can be edited and then Imported to update rates or create new rates. Set the Id to 0 to create a new record",
                    ["Plugins.Shipping.Manager.ByWeightbyTotal.ImportfromExcelTip"] = "Select the Excel File to Import",
                    ["Plugins.Shipping.Manager.Sales.SetOrdersasApproved"] = "Set orders as Approved",
                    ["Plugins.Shipping.Manager.Configure.WebserviceWarehouseSettings"] = "The following settings are for each warehouse configured in the Aramex system",
                    ["Plugins.Shipping.Manager.Orders.Shipments.Products.Warehouse.Hint"] = "The warehouse for the settings to be configured",

                    ["Plugins.Shipping.Manager.Checkout.ShippingMethod"] = "Please select a shipping method. If a Service Point option is selected then the selection mape page will be displayed",
                    ["Plugins.Shipping.Manager.Generate.LanguagePack"] = "Export Local Resources",

                    ["Plugins.Shipping.Manager.ShippingOptionCouldNotbeLoaded"] = "Shipping options could not be loaded for the Products Selected or this Vendor",
                    ["ActivityLog.DeleteCarrier"] = "Deleted a carrier (ID = {0})",

                });

                await _shippingManagerInstallService.InstallLocalisationAsync(true);
                await _shippingManagerInstallService.InstallPermissionsAsync(true);

            }

            await base.InstallAsync();

        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public override async Task UninstallAsync()
        {

            if (_shippingManagerSettings.DeleteConfigurationDataonUninstall)
            {

                //locales
                await _localizationService.DeleteLocaleResourcesAsync("Plugins.Shipping.Manager");

                await _localizationService.DeleteLocaleResourcesAsync("Shipping.Sendcloud");
                await _localizationService.DeleteLocaleResourcesAsync("Shipping.Aramex");
                await _localizationService.DeleteLocaleResourcesAsync("Shipping.CanadaPost");

                await _shippingManagerInstallService.InstallLocalisationAsync(false);
                await _shippingManagerInstallService.InstallPermissionsAsync(false);

                //load settings for a chosen store scope
                var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
                var shippingManagerSettings = await _settingService.LoadSettingAsync<ShippingManagerSettings>(storeScope);

                //Get vendor
                int vendorId = 0;

                //fixed rates
                var fixedRates = await (await _shippingService.GetAllShippingMethodsAsync())
                    .SelectAwait(async shippingMethod => await _settingService.GetSettingAsync(
                        string.Format(ShippingManagerDefaults.FIXED_RATE_SETTINGS_KEY, vendorId, shippingMethod.Id)))
                    .Where(setting => setting != null).ToListAsync();

                await _settingService.DeleteSettingsAsync(fixedRates);

                await _settingService.DeleteSettingAsync<AramexApiSettings>();
                await _settingService.DeleteSettingAsync<SendcloudApiSettings>();
                await _settingService.DeleteSettingAsync<CanadaPostApiSettings>();
            }
            else
            {
                _shippingManagerSettings.DeleteTablesonUninstall = false;
                await _settingService.SaveSettingAsync(_shippingManagerSettings);
            }

            await base.UninstallAsync();

        }

        public async System.Threading.Tasks.Task ManageSiteMapAsync(SiteMapNode rootNode)
        {
            var store = await _storeContext.GetCurrentStoreAsync();

            //load settings for a chosen store scope

            SiteMapNode mainMenuItem = new SiteMapNode();
            SiteMapNode entityGroupMenuItem = new SiteMapNode();
            SiteMapNode salesMenuItem = new SiteMapNode();
            SiteMapNode shipmentsMenuItem = new SiteMapNode();
            SiteMapNode carriersMenuItem = new SiteMapNode();
            SiteMapNode warehousesMenuItem = new SiteMapNode();
            SiteMapNode pickupPointProvidersMenuItem = new SiteMapNode();
            SiteMapNode datesAndRangesMenuItem = new SiteMapNode();
            SiteMapNode manageRates = new SiteMapNode();
            SiteMapNode manageMethods = new SiteMapNode();
            SiteMapNode configureSystem = new SiteMapNode();
            SiteMapNode shippingSettingMenuItem = new SiteMapNode();

            var storeScope = store.Id;
            var shippingManagerSettings = await _settingService.LoadSettingAsync<ShippingManagerSettings>(storeScope);

            if (shippingManagerSettings.Enabled)
            {
                var lrs = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Menu");
                if (lrs != null)
                {
                    mainMenuItem = new SiteMapNode()
                    {
                        SystemName = "Nop.Plugin.Shipping.Manager.Menu",
                        Title = lrs,
                        IconClass = "fas fa-truck",
                        Visible = true
                    };
                }

                if (await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Operate))
                {
                    lrs = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Sales");
                    if (lrs != null)
                    {
                        salesMenuItem = new SiteMapNode()
                        {
                            SystemName = "Nop.Plugins.Shipping.Manager.Sales",
                            Title = lrs,
                            ControllerName = "OrderSales",
                            ActionName = "OrderSales",
                            Visible = true,
                            IconClass = "far fa-dot-circle",
                            RouteValues = new RouteValueDictionary() { { "area", "Admin" } },
                        };
                    }

                    lrs = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Shipments");
                    if (lrs != null)
                    {
                        shipmentsMenuItem = new SiteMapNode()
                        {
                            SystemName = "Nop.Plugins.Shipping.Manager.Shipments",
                            Title = lrs,
                            ControllerName = "OrderOperations",
                            ActionName = "ShipmentList",
                            Visible = true,
                            IconClass = "far fa-dot-circle",
                            RouteValues = new RouteValueDictionary() { { "area", "Admin" } },
                        };
                    }

                    lrs = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Methods");
                    if (lrs != null)
                    {
                        manageMethods = new SiteMapNode()
                        {
                            SystemName = "Nop.Plugins.Shipping.Manager.Methods",
                            Title = lrs,
                            ControllerName = "ShippingOperations",
                            ActionName = "Methods",
                            Visible = true,
                            IconClass = "far fa-dot-circle",
                            RouteValues = new RouteValueDictionary() { { "area", "Admin" } },
                        };
                    }
                }

                if (await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Manage))
                {
                    lrs = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Carriers");
                    if (lrs != null)
                    {
                        carriersMenuItem = new SiteMapNode()
                        {
                            SystemName = "Nop.Plugins.Shipping.Manager.Carriers",
                            Title = lrs,
                            ControllerName = "Carriers",
                            ActionName = "Carriers",
                            Visible = true,
                            IconClass = "far fa-dot-circle",
                            RouteValues = new RouteValueDictionary() { { "area", "Admin" } },
                        };
                    }

                    lrs = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Warehouses");
                    if (lrs != null)
                    {
                        warehousesMenuItem = new SiteMapNode()
                        {
                            SystemName = "Nop.Plugins.Shipping.Manager.Warehouses",
                            Title = lrs,
                            ControllerName = "ShippingOperations",
                            ActionName = "Warehouses",
                            Visible = true,
                            IconClass = "far fa-dot-circle",
                            RouteValues = new RouteValueDictionary() { { "area", "Admin" } },
                        };
                    }

                    lrs = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.PickupPointProviders");
                    if (lrs != null)
                    {
                        pickupPointProvidersMenuItem = new SiteMapNode()
                        {
                            SystemName = "Nop.Plugins.Shipping.Manager.PickupPointProviders",
                            Title = lrs,
                            ControllerName = "ShippingOperations",
                            ActionName = "PickupPointProviders",
                            Visible = true,
                            IconClass = "far fa-dot-circle",
                            RouteValues = new RouteValueDictionary() { { "area", "Admin" } },
                        };
                    }

                    lrs = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.DatesAndRanges");
                    if (lrs != null)
                    {
                        datesAndRangesMenuItem = new SiteMapNode()
                        {
                            SystemName = "Nop.Plugins.Shipping.Manager.DatesAndRanges",
                            Title = lrs,
                            ControllerName = "ShippingOperations",
                            ActionName = "DatesAndRanges",
                            Visible = true,
                            IconClass = "far fa-dot-circle",
                            RouteValues = new RouteValueDictionary() { { "area", "Admin" } },
                        };
                    }

                    lrs = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Manage");
                    if (lrs != null)
                    {
                        manageRates = new SiteMapNode()
                        {
                            SystemName = "Nop.Plugins.Shipping.Manager.Manage",
                            Title = lrs,
                            ControllerName = "ShippingManager",
                            ActionName = "Manage",
                            Visible = true,
                            IconClass = "far fa-dot-circle",
                            RouteValues = new RouteValueDictionary() { { "area", "Admin" } },
                        };
                    }
                }

                if (await _shippingManagerService.AuthorizeAsync(SystemHelper.AccessMode.Configure))
                {
                    lrs = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.EntityGroup");
                    if (lrs != null)
                    {
                        entityGroupMenuItem = new SiteMapNode()
                        {
                            SystemName = "Nop.Plugins.Shipping.Manager.EntityGroup",
                            Title = lrs,
                            ControllerName = "EntityGroup",
                            ActionName = "EntityGroup",
                            Visible = true,
                            IconClass = "far fa-dot-circle",
                            RouteValues = new RouteValueDictionary() { { "area", "Admin" } },
                        };
                    }

                    lrs = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Configuration");
                    if (lrs != null)
                    {
                        configureSystem = new SiteMapNode()
                        {
                            SystemName = "Nop.Plugin.Shipping.Manager.Configure",
                            Title = lrs,
                            ControllerName = "ShippingManagerConfiguration",
                            ActionName = "Configure",
                            Visible = true,
                            IconClass = "far fa-dot-circle",
                            RouteValues = new RouteValueDictionary() { { "area", "Admin" } },
                        };
                    }

                    lrs = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.ShippingSettings");
                    if (lrs != null)
                    {
                        shippingSettingMenuItem = new SiteMapNode()
                        {
                            SystemName = "Nop.Plugins.Shipping.Manager.ShippingSettings",
                            Title = lrs,
                            ControllerName = "ShippingSetting",
                            ActionName = "Shipping",
                            Visible = true,
                            IconClass = "far fa-dot-circle",
                            RouteValues = new RouteValueDictionary() { { "area", "Admin" } },
                        };
                    }
                }

                if (salesMenuItem != null)
                    mainMenuItem.ChildNodes.Add(salesMenuItem);

                if (shipmentsMenuItem != null)
                    mainMenuItem.ChildNodes.Add(shipmentsMenuItem);

                if (manageMethods != null)
                    mainMenuItem.ChildNodes.Add(manageMethods);

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

                if (configureSystem != null && await _workContext.GetCurrentVendorAsync() == null)
                    mainMenuItem.ChildNodes.Add(configureSystem);

                if (entityGroupMenuItem != null && await _workContext.GetCurrentVendorAsync() == null)
                    mainMenuItem.ChildNodes.Add(entityGroupMenuItem);

                if (shippingSettingMenuItem != null)
                    mainMenuItem.ChildNodes.Add(shippingSettingMenuItem);

                if (mainMenuItem != null)
                    rootNode.ChildNodes.Add(mainMenuItem);

            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a shipment tracker
        /// </summary>
        public IShipmentTracker ShipmentTracker => new ShippingManagerShipmentTracker(_shippingManagerService, 
            _shippingManagerSettings, _logger, _addressService);

        #endregion

    }
}
