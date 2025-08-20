using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;

using Nop.Core;
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
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Framework.Factories;
using Nop.Web.Framework.Models.Extensions;

using Nop.Plugin.Shipping.Manager.Domain;
using Nop.Plugin.Shipping.Manager.Models.Carrier;
using Nop.Plugin.Shipping.Manager.Services;
using Nop.Plugin.Shipping.Manager.Settings;

using Nop.Plugin.Apollo.Integrator.Services;

namespace Nop.Plugin.Shipping.Manager.Factories
{
    /// <summary>
    /// Represents the shipping model factory implementation
    /// </summary>
    public partial class CarrierModelFactory : ICarrierModelFactory
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
        protected readonly IShippingManagerService _shippingManagerService;
        protected readonly ICarrierService _carrierService;
        protected readonly ShippingManagerSettings _shippingManagerSettings;
        protected readonly IStateProvinceService _stateProvinceService;
        protected readonly ShippingSettings _shippingSettings;
        protected readonly IWorkContext _workContext;
        protected readonly IEntityGroupService _entityGroupService;

        #endregion

        #region Ctor

        public CarrierModelFactory(IAddressService addressService,
            IBaseAdminModelFactory baseAdminModelFactory,
            ICountryService countryService,
            IDateRangeService dateRangeService,
            ILocalizationService localizationService,
            ILocalizedModelFactory localizedModelFactory,
            IPickupPluginManager pickupPluginManager,
            IShippingPluginManager shippingPluginManager,
            IShippingService shippingService,
            IShippingManagerService shippingManagerService,
            ICarrierService carrierService,
            ShippingManagerSettings shippingManagerSettings,
            IStateProvinceService stateProvinceService,
            ShippingSettings shippingSettings,
            IWorkContext workContext,
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
            _shippingManagerService = shippingManagerService;
            _carrierService = carrierService;
            _shippingManagerSettings = shippingManagerSettings;
            _stateProvinceService = stateProvinceService;
            _shippingSettings = shippingSettings;
            _workContext = workContext;
            _entityGroupService = entityGroupService;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Prepare address model
        /// </summary>
        /// <param name="model">Address model</param>
        /// <param name="address">Address</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the Carrier address model
        /// </returns>  
        protected virtual async Task PrepareAddressModel(AddressModel model, Address address)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            //set some of address fields as enabled and required
            model.CountryRequired = true;

            //prepare available countries
            await _baseAdminModelFactory.PrepareCountriesAsync(model.AvailableCountries);

            //prepare available states
            await _baseAdminModelFactory.PrepareStatesAndProvincesAsync(model.AvailableStates, model.CountryId);
        }

        #endregion

        #region Carrier

        /// <summary>
        /// Prepare carrier model
        /// </summary>
        /// <param name="model">Carrier model</param>
        /// <param name="carrier">Carrier</param>
        /// <param name="excludeProperties">Whether to exclude populating of some properties of model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the Carrier model
        /// </returns> 
        public virtual async Task<CarrierModel> PrepareCarrierModelAsync(CarrierModel model, Carrier carrier, bool excludeProperties = false)
        {
            if (carrier != null)
            {
                //fill in model values from the entity
                if (model == null)
                {
                    model = carrier.ToModel<CarrierModel>();
                }
            }

            //prepare address model
            var address = await _addressService.GetAddressByIdAsync(carrier?.AddressId ?? 0);
            if (!excludeProperties && address != null)
                model.Address = address.ToModel(model.Address);
            await PrepareAddressModel(model.Address, address);

            foreach (var option in _shippingSettings.ActiveShippingRateComputationMethodSystemNames)
            {
                var shippingMethod = await _shippingPluginManager.LoadPluginBySystemNameAsync(option);
                if (shippingMethod != null)
                {
                    var shippingMethodFriendlyName = shippingMethod != null ?
                        await _localizationService.GetLocalizedFriendlyNameAsync(shippingMethod, (await _workContext.GetWorkingLanguageAsync()).Id) : option;
                    model.ActiveShippingRateComputationMethodSystemNames.Add(new SelectListItem { Value = option, Text = shippingMethodFriendlyName });
                }
            }

            string friendlyName = await _localizationService.GetResourceAsync(ShippingManagerDefaults.SendCloudSystemName);
            model.ActiveShippingRateComputationMethodSystemNames.Add(new SelectListItem { Value = ShippingManagerDefaults.SendCloudSystemName, Text = friendlyName });
            friendlyName = await _localizationService.GetResourceAsync(ShippingManagerDefaults.AramexSystemName);
            model.ActiveShippingRateComputationMethodSystemNames.Add(new SelectListItem { Value = ShippingManagerDefaults.AramexSystemName, Text = friendlyName });

            return model;
        }

        /// <summary>
        /// Prepare carrier search model
        /// </summary>
        /// <param name="searchModel">Carrier search model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the Carrier search model
        /// </returns> 
        public virtual async Task<CarrierSearchModel> PrepareCarrierSearchModelAsync(CarrierSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //prepare page parameters
            searchModel.SetGridPageSize();

            searchModel.VendorId = await _entityGroupService.GetActiveVendorScopeAsync();
            searchModel.Active = true;

            return searchModel;
        }

        /// <summary>
        /// Prepare paged carrier list model
        /// </summary>
        /// <param name="searchModel">Carrier search model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the Carrier list model
        /// </returns> 
        //public virtual async Task<CarrierListModel> PrepareCarrierListModelAsync(CarrierSearchModel searchModel)
        //{
        //    if (searchModel == null)
        //        throw new ArgumentNullException(nameof(searchModel));

        //    //get carriers
        //    var carriers = await _carrierService.GetAllCarriersAsync(!searchModel.Active);

        //    if (!string.IsNullOrEmpty(searchModel.SearchName))
        //        carriers = carriers.Where(c => c.Name.ToLower().Contains(searchModel.SearchName.ToLower())).ToList();

        //    var carrierList = carriers.ToPagedList(searchModel);

        //    //prepare list model
        //    var model = await new CarrierListModel().PrepareToGridAsync(searchModel, carrierList, () =>
        //    {
        //        return carriers.SelectAwait(async carrier =>
        //        {
        //            //fill in model values from the entity

        //            var carrierModel = await PrepareCarrierModelAsync(null, carrier, false);

        //            carrierModel.City = carrierModel.Address.City;
        //            carrierModel.PhoneNumber = carrierModel.Address.PhoneNumber;
        //            carrierModel.County = carrierModel.Address.County;

        //            if (carrierModel.Address.StateProvinceId.HasValue)
        //            {
        //                var state = await _stateProvinceService.GetStateProvinceByIdAsync(carrierModel.Address.StateProvinceId.Value);
        //                if (state != null)
        //                    carrierModel.StateProvinceName = state.Name;
        //            }

        //            string vendorName = await _entityGroupService.GetVendorNameForEntityGroupAsync(carrier);
        //            carrierModel.VendorName = vendorName;

        //            return carrierModel;
        //        });
        //    });

        //    return model;
        //}

        public virtual async Task<CarrierListModel> PrepareCarrierListModelAsync(CarrierSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            var carriers = (await _carrierService.GetAllCarriersAsync(searchModel)).ToPagedList(searchModel); 

            //prepare list model
            var model = await new CarrierListModel().PrepareToGridAsync(searchModel, carriers, () =>
            {
                return carriers.SelectAwait(async carrier =>
                {
                    //fill in model values from the entity

                    var carrierModel = await PrepareCarrierModelAsync(null, carrier, false);

                    carrierModel.City = carrierModel.Address.City;
                    carrierModel.PhoneNumber = carrierModel.Address.PhoneNumber;
                    carrierModel.County = carrierModel.Address.County;

                    if (carrierModel.Address.StateProvinceId.HasValue)
                    {
                        var state = await _stateProvinceService.GetStateProvinceByIdAsync(carrierModel.Address.StateProvinceId.Value);
                        if (state != null)
                            carrierModel.StateProvinceName = state.Name;
                    }

                    string vendorName = await _entityGroupService.GetVendorNameForEntityGroupAsync(carrier);
                    carrierModel.VendorName = vendorName;

                    return carrierModel;
                });
            });

            return model;
        }

        #endregion

    }
}