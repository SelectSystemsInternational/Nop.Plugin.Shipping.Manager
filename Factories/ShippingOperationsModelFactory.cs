using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Shipping;
using Nop.Services.Common;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Date;
using Nop.Services.Shipping.Pickup;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Areas.Admin.Models.Common;
using Nop.Web.Areas.Admin.Models.Directory;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Framework.Factories;
using Nop.Web.Framework.Models.Extensions;

using Nop.Plugin.Shipping.Manager.Domain;
using Nop.Plugin.Shipping.Manager.Models.Shipping;
using Nop.Plugin.Shipping.Manager.Models.Warehouse;
using Nop.Plugin.Shipping.Manager.Services;
using Nop.Plugin.Shipping.Manager.Settings;

using Nop.Plugin.Apollo.Integrator.Services;

namespace Nop.Plugin.Shipping.Manager.Factories
{
    /// <summary>
    /// Represents the shipping model factory implementation
    /// </summary>
    public partial class ShippingOperationsModelFactory : IShippingOperationsModelFactory
    {

        #region Fields

        protected readonly IAddressService _addressService;
        protected readonly IBaseAdminModelFactory _baseAdminModelFactory;
        protected readonly ICountryService _countryService;
        protected readonly IDateRangeService _dateRangeService;
        protected readonly ILocalizationService _localizationService;
        protected readonly ILocalizedModelFactory _localizedModelFactory;
        protected readonly IPickupPluginManager _pickupPluginManager;
        protected readonly IShippingPluginManager _shippingPluginManager;
        protected readonly IShippingService _shippingService;
        protected readonly ICarrierService _carrierService;
        protected readonly ShippingManagerSettings _shippingManagerSettings;
        protected readonly IStateProvinceService _stateProvinceService;
        protected readonly IEntityGroupService _entityGroupService;

        #endregion

        #region Ctor

        public ShippingOperationsModelFactory(IAddressService addressService,
            IBaseAdminModelFactory baseAdminModelFactory,
            ICountryService countryService,
            IDateRangeService dateRangeService,
            ILocalizationService localizationService,
            ILocalizedModelFactory localizedModelFactory,
            IPickupPluginManager pickupPluginManager,
            IShippingPluginManager shippingPluginManager,
            IShippingService shippingService,
            ICarrierService carrierService,
            ShippingManagerSettings shippingManagerSettings,
            IStateProvinceService stateProvinceService,
            IEntityGroupService entityGroupService)
        {
            _addressService = addressService;
            _baseAdminModelFactory = baseAdminModelFactory;
            _countryService = countryService;
            _dateRangeService = dateRangeService;
            _localizationService = localizationService;
            _localizedModelFactory = localizedModelFactory;
            _pickupPluginManager = pickupPluginManager;
            _shippingPluginManager = shippingPluginManager;
            _shippingService = shippingService;
            _carrierService = carrierService;
            _shippingManagerSettings = shippingManagerSettings;
            _stateProvinceService = stateProvinceService;
            _entityGroupService = entityGroupService;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Prepare address model
        /// </summary>
        /// <param name="model">Address model</param>
        /// <param name="address">Address</param>
        protected virtual async Task PrepareAddressModelAsync(AddressModel model, Address address)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            //prepare available countries
            await _baseAdminModelFactory.PrepareCountriesAsync(model.AvailableCountries);

            //prepare available states
            await _baseAdminModelFactory.PrepareStatesAndProvincesAsync(model.AvailableStates, model.CountryId);
        }

        #endregion

        #region Shipping Provider
        
        /// <summary>
        /// Prepare shipping provider search model
        /// </summary>
        /// <param name="searchModel">Shipping provider search model</param>
        /// <returns>Shipping provider search model</returns>
        public virtual ShippingProviderSearchModel PrepareShippingProviderSearchModel(ShippingProviderSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //prepare page parameters
            searchModel.SetGridPageSize();

            return searchModel;
        }

        /// <summary>
        /// Prepare paged shipping provider list model
        /// </summary>
        /// <param name="searchModel">Shipping provider search model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipping provider list model
        /// </returns>
        public virtual async Task<ShippingProviderListModel> PrepareShippingProviderListModelAsync(ShippingProviderSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //get shipping providers
            var shippingProviders = (await _shippingPluginManager.LoadAllPluginsAsync()).ToPagedList(searchModel);

            //prepare grid model
            var model = await new ShippingProviderListModel().PrepareToGridAsync(searchModel, shippingProviders, () =>
            {
                return shippingProviders.SelectAwait(async provider =>
                {
                    //fill in model values from the entity
                    var shippingProviderModel = provider.ToPluginModel<ShippingProviderModel>();

                    //fill in additional values (not existing in the entity)
                    shippingProviderModel.IsActive = _shippingPluginManager.IsPluginActive(provider);
                    shippingProviderModel.ConfigurationUrl = provider.GetConfigurationPageUrl();

                    shippingProviderModel.LogoUrl = await _shippingPluginManager.GetPluginLogoUrlAsync(provider);

                    return shippingProviderModel;
                });
            });

            return model;
        }

        #endregion

        #region Pickup Point Provider

        /// <summary>
        /// Prepare pickup point provider search model
        /// </summary>
        /// <param name="searchModel">Pickup point provider search model</param>
        /// <returns>Pickup point provider search model</returns>
        public virtual PickupPointProviderSearchModel PreparePickupPointProviderSearchModel(PickupPointProviderSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //prepare page parameters
            searchModel.SetGridPageSize();

            return searchModel;
        }

        /// <summary>
        /// Prepare paged pickup point provider list model
        /// </summary>
        /// <param name="searchModel">Pickup point provider search model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the pickup point provider list model
        /// </returns>
        public virtual async Task<PickupPointProviderListModel> PreparePickupPointProviderListModelAsync(PickupPointProviderSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //get pickup point providers
            var pickupPointProviders = (await _pickupPluginManager.LoadAllPluginsAsync()).ToPagedList(searchModel);

            //prepare grid model
            var model = await new PickupPointProviderListModel().PrepareToGridAsync(searchModel, pickupPointProviders, () =>
            {
                return pickupPointProviders.SelectAwait(async provider =>
                {
                    //fill in model values from the entity
                    var pickupPointProviderModel = provider.ToPluginModel<PickupPointProviderModel>();

                    //fill in additional values (not existing in the entity)
                    pickupPointProviderModel.IsActive = _pickupPluginManager.IsPluginActive(provider);
                    pickupPointProviderModel.ConfigurationUrl = provider.GetConfigurationPageUrl();

                    pickupPointProviderModel.LogoUrl = await _pickupPluginManager.GetPluginLogoUrlAsync(provider);

                    return pickupPointProviderModel;
                });
            });

            return model;
        }

        #endregion

        #region Shipping Methods

        /// <summary>
        /// Prepare shipping method search model
        /// </summary>
        /// <param name="searchModel">Shipping method search model</param>
        /// <returns>Shipping method search model</returns>
        public virtual async Task<ShippingMethodSearchModel> PrepareShippingMethodSearchModelAsync(ShippingMethodSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //prepare page parameters
            searchModel.SetGridPageSize();

            searchModel.VendorId = await _entityGroupService.GetActiveVendorScopeAsync();

            return searchModel;
        }

        /// <summary>
        /// Prepare paged shipping method list model
        /// </summary>
        /// <param name="searchModel">Shipping method search model</param>
        /// <returns>Shipping method list model</returns>
        public virtual async Task<ShippingMethodListModel> PrepareShippingMethodListModelAsync(ShippingMethodSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //get shipping methods
            var shippingMethods = (await _shippingService.GetAllShippingMethodsAsync()).ToPagedList(searchModel);

            //prepare list model
            var model = await new ShippingMethodListModel().PrepareToGridAsync(searchModel, shippingMethods, () =>
            {
                return shippingMethods.SelectAwait(async method =>
                {

                    var shippingMethodModel = new ShippingMethodModel();

                    shippingMethodModel.Id = method.Id;
                    shippingMethodModel.Name = method.Name;
                    shippingMethodModel.Description = method.Description;
                    shippingMethodModel.DisplayOrder = method.DisplayOrder;
                    
                    string vendorName = await _entityGroupService.GetVendorNameForEntityGroupAsync(method);
                    shippingMethodModel.VendorName = vendorName;

                    return shippingMethodModel;

                });
            });

            return model;
        }

        /// <summary>
        /// Prepare shipping method model
        /// </summary>
        /// <param name="model">Shipping method model</param>
        /// <param name="shippingMethod">Shipping method</param>
        /// <param name="excludeProperties">Whether to exclude populating of some properties of model</param>
        /// <returns>Shipping method model</returns>
        public virtual async Task<ShippingMethodModel> PrepareShippingMethodModelAsync(ShippingMethodModel model,
            ShippingMethod shippingMethod, bool excludeProperties = false)
        {
            Func<ShippingMethodLocalizedModel, int, Task> localizedModelConfiguration = null;

            if (shippingMethod != null)
            {
                //fill in model values from the entity
                model = model ?? shippingMethod.ToModel<ShippingMethodModel>();

                //define localized model configuration action
                localizedModelConfiguration = async (locale, languageId) =>
                {
                    locale.Name = await _localizationService.GetLocalizedAsync(shippingMethod, entity => entity.Name, languageId, false, false);
                    locale.Description = await _localizationService.GetLocalizedAsync(shippingMethod, entity => entity.Description, languageId, false, false);
                };
            }

            //prepare localized models
            if (!excludeProperties)
                model.Locales = await _localizedModelFactory.PrepareLocalizedModelsAsync(localizedModelConfiguration);

            return model;
        }

        /// <summary>
        /// Prepare shipping method restriction model
        /// </summary>
        /// <param name="model">Shipping method restriction model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipping method restriction model
        /// </returns>
        public virtual async Task<ShippingMethodRestrictionModel> PrepareShippingMethodRestrictionModelAsync(ShippingMethodRestrictionModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            var countries = await _countryService.GetAllCountriesAsync(showHidden: true);
            model.AvailableCountries = await countries.SelectAwait(async country =>
            {
                var countryModel = country.ToModel<CountryModel>();
                countryModel.NumberOfStates = (await _stateProvinceService.GetStateProvincesByCountryIdAsync(country.Id))?.Count ?? 0;

                return countryModel;
            }).ToListAsync();

            foreach (var shippingMethod in await _shippingService.GetAllShippingMethodsAsync())
            {
                model.AvailableShippingMethods.Add(shippingMethod.ToModel<ShippingMethodModel>());
                foreach (var country in countries)
                {
                    if (!model.Restricted.ContainsKey(country.Id))
                        model.Restricted[country.Id] = new Dictionary<int, bool>();

                    model.Restricted[country.Id][shippingMethod.Id] = await _shippingService.CountryRestrictionExistsAsync(shippingMethod, country.Id);
                }
            }

            return model;
        }

        #endregion

        #region All Dates and Ranges

        /// <summary>
        /// Prepare dates and ranges search model
        /// </summary>
        /// <param name="searchModel">Dates and ranges search model</param>
        /// <returns>Dates and ranges search model</returns>
        public virtual async Task<DatesRangesSearchModel> PrepareDatesRangesSearchModelAsync(DatesRangesSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //prepare nested search models
            await PrepareDeliveryDateSearchModelAsync(searchModel.DeliveryDateSearchModel);
            await PrepareProductAvailabilityRangeSearchModelAsync(searchModel.ProductAvailabilityRangeSearchModel);
            await PrepareCutOffTimeSearchModelAsync(searchModel.CutOffTimeSearchModel);

            searchModel.VendorId = await _entityGroupService.GetActiveVendorScopeAsync();

            return searchModel;
        }

        #endregion

        #region Delivery Date

        /// <summary>
        /// Prepare delivery date search model
        /// </summary>
        /// <param name="searchModel">Delivery date search model</param>
        /// <returns>Delivery date search model</returns>
        protected virtual async Task<DeliveryDateSearchModel> PrepareDeliveryDateSearchModelAsync(DeliveryDateSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //prepare page parameters
            searchModel.SetGridPageSize();

            searchModel.VendorId = await _entityGroupService.GetActiveVendorScopeAsync();

            return searchModel;
        }


        /// <summary>
        /// Prepare paged delivery date list model
        /// </summary>
        /// <param name="searchModel">Delivery date search model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the delivery date list model
        /// </returns>
        public virtual async Task<DeliveryDateListModel> PrepareDeliveryDateListModelAsync(DeliveryDateSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //get delivery dates
            var deliveryDates = (await _dateRangeService.GetAllDeliveryDatesAsync()).ToPagedList(searchModel);

            //prepare grid model
            var model = await new DeliveryDateListModel().PrepareToGridAsync(searchModel, deliveryDates, () =>
            {
                //fill in model values from the entity
                return deliveryDates.SelectAwait(async deliveryDates =>
                {

                    var deliveryDateListModel = new DeliveryDateModel();

                    deliveryDateListModel.Id = deliveryDates.Id;
                    deliveryDateListModel.Name = deliveryDates.Name;
                    deliveryDateListModel.DisplayOrder = deliveryDates.DisplayOrder;

                    string vendorName = await _entityGroupService.GetVendorNameForEntityGroupAsync(deliveryDates);
                    deliveryDateListModel.VendorName = vendorName;

                    return deliveryDateListModel;

                });
            });

            return model;
        }

        /// <summary>
        /// Prepare delivery date model
        /// </summary>
        /// <param name="model">Delivery date model</param>
        /// <param name="deliveryDate">Delivery date</param>
        /// <param name="excludeProperties">Whether to exclude populating of some properties of model</param>
        /// <returns>Delivery date model</returns>
        public virtual async Task<DeliveryDateModel> PrepareDeliveryDateModelAsync(DeliveryDateModel model, DeliveryDate deliveryDate, bool excludeProperties = false)
        {
            Func<DeliveryDateLocalizedModel, int, Task> localizedModelConfiguration = null;

            if (deliveryDate != null)
            {
                //fill in model values from the entity
                model = model ?? deliveryDate.ToModel<DeliveryDateModel>();

                //define localized model configuration action
                localizedModelConfiguration = async (locale, languageId) =>
                {
                    locale.Name = await _localizationService.GetLocalizedAsync(deliveryDate, entity => entity.Name, languageId, false, false);
                };

                string vendorName = await _entityGroupService.GetVendorNameForEntityGroupAsync(deliveryDate);
                model.VendorName = vendorName;
            }

            //prepare localized models
            if (!excludeProperties)
                model.Locales = await _localizedModelFactory.PrepareLocalizedModelsAsync(localizedModelConfiguration);

            return model;
        }

        #endregion

        #region Product Availability Range

        /// <summary>
        /// Prepare product availability range search model
        /// </summary>
        /// <param name="searchModel">Product availability range search model</param>
        /// <returns>Product availability range search model</returns>
        protected virtual async Task<ProductAvailabilityRangeSearchModel> PrepareProductAvailabilityRangeSearchModelAsync(ProductAvailabilityRangeSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //prepare page parameters
            searchModel.SetGridPageSize();

            searchModel.VendorId = await _entityGroupService.GetActiveVendorScopeAsync();

            return searchModel;
        }

        /// <summary>
        /// Prepare paged product availability range list model
        /// </summary>
        /// <param name="searchModel">Product availability range search model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the product availability range list model
        /// </returns>
        public virtual async Task<ProductAvailabilityRangeListModel> PrepareProductAvailabilityRangeListModelAsync(ProductAvailabilityRangeSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //get product availability ranges
            var productAvailabilityRanges = (await _dateRangeService.GetAllProductAvailabilityRangesAsync()).ToPagedList(searchModel);

            //prepare grid model
            var model = await new ProductAvailabilityRangeListModel().PrepareToGridAsync(searchModel, productAvailabilityRanges, () =>
            {

                //fill in model values from the entity
                return productAvailabilityRanges.SelectAwait(async productAvailabilityRanges =>
                {

                    var productAvailabilityRangeModel = new ProductAvailabilityRangeModel();

                    productAvailabilityRangeModel.Id = productAvailabilityRanges.Id;
                    productAvailabilityRangeModel.Name = productAvailabilityRanges.Name;
                    productAvailabilityRangeModel.DisplayOrder = productAvailabilityRanges.DisplayOrder;

                    string vendorName = await _entityGroupService.GetVendorNameForEntityGroupAsync(productAvailabilityRanges);
                    productAvailabilityRangeModel.VendorName = vendorName;

                    return productAvailabilityRangeModel;

                });

            });

            return model;
        }


        /// <summary>
        /// Prepare product availability range model
        /// </summary>
        /// <param name="model">Product availability range model</param>
        /// <param name="productAvailabilityRange">Product availability range</param>
        /// <param name="excludeProperties">Whether to exclude populating of some properties of model</param>
        /// <returns>Product availability range model</returns>
        public virtual async Task<ProductAvailabilityRangeModel> PrepareProductAvailabilityRangeModelAsync(ProductAvailabilityRangeModel model,
            ProductAvailabilityRange productAvailabilityRange, bool excludeProperties = false)
        {
            Func<ProductAvailabilityRangeLocalizedModel, int, Task> localizedModelConfiguration = null;

            if (productAvailabilityRange != null)
            {
                //fill in model values from the entity
                model = model ?? productAvailabilityRange.ToModel<ProductAvailabilityRangeModel>();

                //define localized model configuration action
                localizedModelConfiguration = async (locale, languageId) =>
                {
                    locale.Name = await _localizationService.GetLocalizedAsync(productAvailabilityRange, entity => entity.Name, languageId, false, false);
                };

                string vendorName = await _entityGroupService.GetVendorNameForEntityGroupAsync(productAvailabilityRange);
                model.VendorName = vendorName;
            }

            //prepare localized models
            if (!excludeProperties)
                model.Locales = await _localizedModelFactory.PrepareLocalizedModelsAsync(localizedModelConfiguration);

            return model;
        }

        #endregion

        #region Cut Off Time

        /// <summary>
        /// Prepare cut of time search model
        /// </summary>
        /// <param name="searchModel">Product availability range search model</param>
        /// <returns>Product availability range search model</returns>
        protected virtual async Task<CutOffTimeSearchModel> PrepareCutOffTimeSearchModelAsync(CutOffTimeSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //prepare page parameters
            searchModel.SetGridPageSize();

            searchModel.VendorId = await _entityGroupService.GetActiveVendorScopeAsync();

            return searchModel;
        }

        /// <summary>
        /// Prepare paged cut of time list model
        /// </summary>
        /// <param name="searchModel">Product availability range search model</param>
        /// <returns>Product availability range list model</returns>
        public virtual async Task<CutOffTimeListModel> PrepareCutOffTimeListModelAsync(CutOffTimeSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //get product availability ranges
            var cutOffTimes = (await _carrierService.GetAllCutOffTimesAsync()).ToPagedList(searchModel);

            //prepare grid model
            var model = await new CutOffTimeListModel().PrepareToGridAsync(searchModel, cutOffTimes, () =>
            {

                //fill in model values from the entity
                return cutOffTimes.SelectAwait(async cutOffTime =>
                {

                    var cutOffTimesModel = new CutOffTimeModel();

                    cutOffTimesModel.Id = cutOffTime.Id;
                    cutOffTimesModel.Name = cutOffTime.Name;
                    cutOffTimesModel.DisplayOrder = cutOffTime.DisplayOrder;

                    string vendorName = await _entityGroupService.GetVendorNameForEntityGroupAsync(cutOffTime);
                    cutOffTimesModel.VendorName = vendorName;

                    return cutOffTimesModel;

                });

            });

            return model;
        }

        /// <summary>
        /// Prepare cut of time model
        /// </summary>
        /// <param name="model">Product availability range model</param>
        /// <param name="CutOffTime">Product availability range</param>
        /// <param name="excludeProperties">Whether to exclude populating of some properties of model</param>
        /// <returns>Product availability range model</returns>
        public virtual async Task<CutOffTimeModel> PrepareCutOffTimeModelAsync(CutOffTimeModel model,
            CutOffTime cutOffTime, bool excludeProperties = false)
        {
            Func<CutOffTimeLocalizedModel, int, Task> localizedModelConfiguration = null;

            if (cutOffTime != null)
            {
                //fill in model values from the entity
                model = model ?? cutOffTime.ToModel<CutOffTimeModel>();

                //define localized model configuration action
                localizedModelConfiguration = async (locale, languageId) =>
                {
                    locale.Name = await _localizationService.GetLocalizedAsync(cutOffTime, entity => entity.Name, languageId, false, false);
                };
            }

            //prepare localized models
            if (!excludeProperties)
                model.Locales = await _localizedModelFactory.PrepareLocalizedModelsAsync(localizedModelConfiguration);

            return model;
        }

        #endregion

        #region Warehouse

        /// <summary>
        /// Prepare warehouse model
        /// </summary>
        /// <param name="model">Warehouse model</param>
        /// <param name="warehouse">Warehouse</param>
        /// <param name="excludeProperties">Whether to exclude populating of some properties of model</param>
        /// <returns>Warehouse model</returns>
        public virtual async Task<WarehouseModel> PrepareWarehouseModelAsync(WarehouseModel model, Warehouse warehouse, bool excludeProperties = false)
        {
            if (warehouse != null)
            {
                //fill in model values from the entity
                if (model == null)
                {
                    model = warehouse.ToModel<WarehouseModel>();
                }
            }

            //prepare address model
            var address = await _addressService.GetAddressByIdAsync(warehouse?.AddressId ?? 0);
            if (!excludeProperties && address != null)
                model.Address = address.ToModel(model.Address);
            await PrepareAddressModelAsync(model.Address, address);

            return model;
        }

        /// <summary>
        /// Prepare warehouse search model
        /// </summary>
        /// <param name="searchModel">Warehouse search model</param>
        /// <returns>Warehouse search model</returns>
        public virtual async Task<WarehouseSearchModel> PrepareWarehouseSearchModelAsync(WarehouseSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //prepare page parameters
            searchModel.SetGridPageSize();

            searchModel.VendorId = await _entityGroupService.GetActiveVendorScopeAsync();

            return searchModel;
        }

        /// <summary>
        /// Prepare paged warehouse list model
        /// </summary>
        /// <param name="searchModel">Warehouse search model</param>
        /// <returns>Warehouse list model</returns>
        public virtual async Task<WarehouseListModel> PrepareWarehouseListModelAsync(WarehouseSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //get warehouses
            var warehouses = await _shippingService.GetAllWarehousesAsync();

            if (!string.IsNullOrEmpty(searchModel.SearchName))
                warehouses = warehouses.Where(w => w.Name.ToLower().Contains(searchModel.SearchName.ToLower())).ToList();

            var warehouseList = warehouses.ToPagedList(searchModel);

            //prepare list model
            var model = await new WarehouseListModel().PrepareToGridAsync(searchModel, warehouseList, () =>
            {
                return warehouses.SelectAwait(async warehouse =>
                {
                    //fill in model values from the entity
                    var warehouseModel = await PrepareWarehouseModelAsync(null, warehouse, false);

                    warehouseModel.City = warehouseModel.Address.City;
                    warehouseModel.PhoneNumber = warehouseModel.Address.PhoneNumber;
                    warehouseModel.County = warehouseModel.Address.County;

                    if (warehouseModel.Address.StateProvinceId.HasValue)
                    {
                        var state = await _stateProvinceService.GetStateProvinceByIdAsync(warehouseModel.Address.StateProvinceId.Value);
                        if (state != null)
                            warehouseModel.StateProvinceName = state.Name;
                    }

                    string vendorName = await _entityGroupService.GetVendorNameForEntityGroupAsync(warehouse);
                    warehouseModel.VendorName = vendorName;

                    return warehouseModel;
                });
            });

            return model;
        }

        #endregion

    }
}