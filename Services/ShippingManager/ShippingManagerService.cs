using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;

using Nop.Core;
using Nop.Core.Infrastructure;
using Nop.Core.Caching;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Logging;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Security;
using Nop.Data;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Plugins;
using Nop.Services.Security;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Tracking;
using Nop.Services.Stores;
using Nop.Services.Vendors;
using Nop.Web.Areas.Admin.Models.Common;

using Nop.Plugin.Shipping.Manager.Domain;
using Nop.Plugin.Shipping.Manager.Models;
using Nop.Plugin.Shipping.Manager.Settings;

using Nop.Plugin.Apollo.Integrator;
using Nop.Plugin.Apollo.Integrator.Services;

namespace Nop.Plugin.Shipping.Manager.Services
{
    /// <summary>
    /// Represents service shipping by weight service implementation
    /// </summary>
    public partial class ShippingManagerService : IShippingManagerService
    {

        #region Fields

        protected readonly IStaticCacheManager _staticCacheManager;
        protected readonly IRepository<ShippingManagerByWeightByTotal> _sbwtRepository;
        protected readonly IStoreContext _storeContext;
        protected readonly ShippingManagerSettings _shippingManagerSettings;
        protected readonly IPermissionService _permissionService;
        protected readonly ILogger _logger;
        protected readonly ICarrierService _carrierService;
        protected readonly IWorkContext _workContext;
        protected readonly ICountryService _countryService;
        protected readonly IShippingService _shippingService;
        protected readonly IStoreService _storeService;
        protected readonly IVendorService _vendorService;
        protected readonly IStateProvinceService _stateProvinceService;
        protected readonly IEntityGroupService _entityGroupService;
        protected readonly IPluginService _pluginService;
        protected readonly ILocalizationService _localizationService;
        protected readonly IRepository<Shipment> _shipmentRepository;
        protected readonly IShoppingCartService _shoppingCartService;
        protected readonly IShipmentDetailsService _shipmentDetailsService;
        protected readonly ISettingService _settingService;
        protected readonly IEncryptionService _encryptionService;
        protected readonly IPackagingOptionService _packagingOptionService;
        protected readonly IRepository<Address> _addressRepository;
        protected readonly IRepository<ShippingMethod> _shippingMethodRepository;

        SystemHelper _systemHelper = new SystemHelper();

        #endregion

        #region Ctor

        public ShippingManagerService(IStaticCacheManager staticCacheManager,
            IRepository<ShippingManagerByWeightByTotal> sbwtRepository,
            IStoreContext storeContext,
            ShippingManagerSettings shippingManagerSettings,
            IPermissionService permissionService,
            ILogger logger,
            ICarrierService carrierService,
            IWorkContext workContext,
            ICountryService countryService,
            IShippingService shippingService,
            IStoreService storeService,
            IVendorService vendorService,
            IStateProvinceService stateProvinceService,
            IEntityGroupService entityGroupService,
            IPluginService pluginService,
            ILocalizationService localizationService,
            IRepository<Shipment> shipmentRepository,
            IShoppingCartService shoppingCartService,
            IShipmentDetailsService shipmentDetailsService,
            ISettingService settingService,
            IEncryptionService encryptionService,
            IPackagingOptionService packagingOptionService,
            IRepository<Address> addressRepository,
            IRepository<ShippingMethod> shippingMethodRepository)
        {
            _staticCacheManager = staticCacheManager;
            _sbwtRepository = sbwtRepository;
            _storeContext = storeContext;
            _shippingManagerSettings = shippingManagerSettings;
            _permissionService = permissionService;
            _logger = logger;
            _carrierService = carrierService;
            _workContext = workContext;
            _countryService = countryService;
            _shippingService = shippingService;
            _storeService = storeService;
            _vendorService = vendorService;
            _stateProvinceService = stateProvinceService;
            _entityGroupService = entityGroupService;
            _pluginService = pluginService;
            _localizationService = localizationService;
            _shipmentRepository = shipmentRepository;
            _shoppingCartService = shoppingCartService;
            _shipmentDetailsService = shipmentDetailsService;
            _settingService = settingService;
            _encryptionService = encryptionService;
            _packagingOptionService = packagingOptionService;
            _addressRepository = addressRepository;
            _shippingMethodRepository = shippingMethodRepository;
        }

        #endregion

        #region Utility

        /// <summary>
        /// Prepare plugins warning model
        /// </summary>
        /// <param name="models">List of system warning models</param>
        /// <returns>task that represents the asynchronous operation</returns>
        public async Task PreparePluginsWarningModelAsync(IList<SystemWarningModel> models)
        {

            IServiceCollection serviceCollection = EngineContext.Current.Resolve<IServiceCollection>();

            if (models == null)
                throw new ArgumentNullException(nameof(models));

            //check whether there are incompatible plugins
            foreach (var pluginName in _pluginService.GetIncompatiblePlugins())
            {
                models.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Warning,
                    Text = string.Format(await _localizationService.GetResourceAsync("Admin.System.Warnings.PluginNotLoaded"), pluginName)
                });
            }

            var warningFormat = await _localizationService
                .GetResourceAsync("Admin.System.Warnings.PluginRequiredAssembly");

            //check whether there are any collision of loaded assembly
            foreach (var assembly in _pluginService.GetAssemblyCollisions())
            {
                //get plugin references message
                var message = assembly.Collisions
                    .Select(item => string.Format(warningFormat, item.PluginName, item.AssemblyVersion))
                    .Aggregate("", (current, all) => all + ", " + current).TrimEnd(',', ' ');

                models.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Warning,
                    Text = string.Format(await _localizationService.GetResourceAsync("Admin.System.Warnings.AssemblyHasCollision"),
                        assembly.ShortName, assembly.AssemblyInMemory, message)
                });
            }

            //check whether there are different plugins which try to override the same interface
            var baseLibraries = new[] { "Nop.Core", "Nop.Data", "Nop.Services", "Nop.Web", "Nop.Web.Framework" };
            var overridenServices = serviceCollection.Where(p =>
                    p.ServiceType.FullName != null &&
                    p.ServiceType.FullName.StartsWith("Nop.", StringComparison.InvariantCulture) &&
                    !p.ServiceType.FullName.StartsWith(
                        typeof(IConsumer<>).FullName?.Replace("~1", string.Empty) ?? string.Empty,
                        StringComparison.InvariantCulture)).Select(p =>
                    KeyValuePair.Create(p.ServiceType.FullName, p.ImplementationType?.Assembly.GetName().Name))
                .Where(p => baseLibraries.All(library =>
                    !p.Value?.StartsWith(library, StringComparison.InvariantCultureIgnoreCase) ?? false))
                .GroupBy(p => p.Key, p => p.Value)
                .Where(p => p.Count() > 1)
                .ToDictionary(p => p.Key, p => p.ToList());

            foreach (var overridenService in overridenServices)
            {
                var assemblies = overridenService.Value
                    .Aggregate("", (current, all) => all + ", " + current).TrimEnd(',', ' ');

                models.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Warning,
                    Text = string.Format(await _localizationService.GetResourceAsync("Admin.System.Warnings.PluginsOverrideSameService"), overridenService.Key, assemblies)
                });
            }
        }

        /// <summary>
        /// Gets a shopping cart package price 
        /// </summary>
        /// <param name="sci">Shopping Cart Item</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the package price
        /// </returns>
        public async Task<decimal> GetPackagePrice(ShoppingCartItem sci)
        {
            if (sci != null)
            {
                if (_shippingManagerSettings.TestMode)
                {
                    string message = "Shipping Manager - Get Package Price - For Product : " + sci.ProductId.ToString() +
                        " Shopping Cart Type: " + sci.ShoppingCartType.ToString() +
                        " Quantity: " + sci.Quantity.ToString();

                    await _logger.InsertLogAsync(LogLevel.Debug, message);
                }

                var price = (await _shoppingCartService.GetSubTotalAsync(sci, true)).subTotal;
                return price;
            }

            return 0;
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

                //shippingOption.Name = shippingMethod.Name; Use Original Name
                if (!string.IsNullOrEmpty(description))
                    shippingOption.Description = description;
                else
                    shippingOption.Description = shippingOption.Name;

                if (smco.Smbwtr.TransitDays != 0)
                    shippingOption.TransitDays = smco.Smbwtr.TransitDays;

                shippingOption.DisplayOrder = shippingMethod.DisplayOrder;

                return shippingOption;
            }

            return null;
        }

        /// <summary>
        /// Get default Billing address
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the address
        /// </returns> 
        public async Task<Address> GetDefaultBillingAddressAsync(Customer customer)
        {
            var address = await _addressRepository.GetByIdAsync(customer.BillingAddressId);
            return address;
        }

        /// <summary>
        /// Get default country code for a customer
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the country code string
        /// </returns>
        public async Task<string> GetDefaultCountryCodeAsync(Customer customer)
        {
            string countryCode = string.Empty;

            var address = await _addressRepository.GetByIdAsync(customer.BillingAddressId);

            var country = await _countryService.GetCountryByIdAsync(address.CountryId.Value);
            if (country != null)
                countryCode = country.TwoLetterIsoCode;

            return countryCode;
        }

        /// <summary>
        /// Get country from Code
        /// </summary>
        /// <param name="countryCode">Country code string</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the country identifier
        /// </returns>
        public async Task<int> GetCountryIdFromCodeAsync(string countryCode)
        {
            var country = await _countryService.GetCountryByTwoLetterIsoCodeAsync(countryCode);
            if (country != null)
                return country.Id;
            else
                return 0;
        }

        /// <summary>
        /// Create an address
        /// </summary>
        /// <param name="firstName">FirstName</param>
        /// <param name="lastName">LastName</param> 
        /// <param name="email">Email</param>
        /// <param name="company">company</param>
        /// <param name="countryId">Country idendtifier</param>
        /// <param name="stateProvinceId">StateProvince idendtifier</param> 
        /// <param name="county">County</param>
        /// <param name="city">City</param>
        /// <param name="address1">Address line 1</param>
        /// <param name="address2">Address line 2</param>
        /// <param name="zipPostalCode">ZipPostalCode</param> 
        /// <param name="phoneNumber">PhoneNumber</param>
        /// <param name="faxNumber">FaxNumber</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the address
        /// </returns> 
        public async Task<Address> CreateAddressAsync(string firstName, string lastName, string email,
            string company, int countryId, int stateProvinceId, string county, string city, string address1, string address2,
            string zipPostalCode, string phoneNumber, string faxNumber)
        {
            var customer = await _workContext.GetCurrentCustomerAsync();

            var address = new Address();

            if (customer != null)
            {
                var defaultAddress = await _addressRepository.GetByIdAsync(customer.BillingAddressId);

                address.FirstName = firstName;
                address.LastName = lastName;
                address.Email = email;
                address.Company = company;

                if (countryId == 0)
                    address.CountryId = defaultAddress.CountryId;
                else
                    address.CountryId = countryId;

                if (stateProvinceId == 0)
                    address.StateProvinceId = defaultAddress.StateProvinceId;
                else
                    address.StateProvinceId = stateProvinceId;

                address.County = county;
                address.City = city;
                address.Address1 = address1;
                address.Address2 = address2;
                address.ZipPostalCode = zipPostalCode;
                address.PhoneNumber = phoneNumber;
                address.FaxNumber = faxNumber;

                await _addressRepository.InsertAsync(address);
            }

            return address;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the active store scope
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the current store scope
        /// </returns>
        public async Task<int> GetActiveStoreScopeConfiguration()
        {
            //ensure that we have 2 (or more) stores
            if ((await _storeService.GetAllStoresAsync()).Count < 2)
                return 0;
            else
                return (await _storeContext.GetCurrentStoreAsync()).Id;
        }

        /// <summary>
        /// Deturmines if access is availbale using Config Keys and ACL
        /// </summary>
        /// <param name="carrierId">The carrier identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result true if access is availbale of faluse otherwise
        /// </returns>
        public async Task<bool> AuthorizeAsync(SystemHelper.AccessMode requestedAccess, string publicKey = "", string privateKey = "")
        {
            var store = await _storeContext.GetCurrentStoreAsync();

            string dateInstall = DateTime.Now.Date.ToString();
            var demoDateInstall = DateTime.Parse(dateInstall).AddDays(14);
            Guid pKeyInstall = _systemHelper.DateToGuid(demoDateInstall);
            string publicKeyInstall = pKeyInstall.ToString();
            string urlInstall = _systemHelper.GetDomainNameFromHost(store.Url);
            string privateKeyInstall = _encryptionService.EncryptText(urlInstall, publicKeyInstall);

            string pKey = string.Empty;
            bool hasKey = false;
            string key = publicKey != "" ? publicKey : _shippingManagerSettings.PublicKey;
            if (key != null)
            {
                if (key.ToString() == ShippingManagerDefaults.SystemKey) //System Key
                    hasKey = true;
                else if (key.ToString().Contains(ShippingManagerDefaults.PublicKey))
                {
                    // Purchase Key
                    string sKey = _encryptionService.EncryptText("Licence", key);
                    pKey = (privateKey != "" ? privateKey : _shippingManagerSettings.PrivateKey);
                    if (sKey != pKey)
                    {
                        _shippingManagerSettings.PrivateKey = sKey;
                        await _settingService.SaveSettingAsync(_shippingManagerSettings);
                        hasKey = true;
                    }
                    else if (sKey == pKey)
                    {
                        hasKey = true;
                    }
                }
                else if (key.ToString() != ShippingManagerDefaults.PublicKey)
                {
                    try
                    {
                        pKey = _encryptionService.EncryptText(urlInstall, key);
                        if (pKey == (privateKey != "" ? privateKey : _shippingManagerSettings.PrivateKey))
                        {
                            Guid guid = Guid.Parse(key);
                            DateTime keyDate = _systemHelper.GuidToDate(guid);
                            if (keyDate > DateTime.Now)
                                hasKey = true;
                        }
                    }
                    catch (Exception exc)
                    {
                        string message = string.Format("Error checking licence for Store {1} with PublicKey {2} and PrivateKey {3}",
                            ShippingManagerDefaults.SystemName, urlInstall, key, pKey);
                        await _logger.ErrorAsync(message, exc);
                    }
                }
            }

            if (hasKey)
            {
                PermissionRecord acesss = _systemHelper.GetAccessPermission(requestedAccess);
                return await _permissionService.AuthorizeAsync(acesss);
            }
            else if (_shippingManagerSettings.Enabled)
            {
                string message = string.Format("Plugin {0} was installed for Store {1} with PublicKey {2} and PrivateKey {3}",
                    ShippingManagerDefaults.SystemName, urlInstall, key, pKey);
                await _logger.InformationAsync(message);
            }

            return false;
        }

        /// <summary>
        /// Gets a shipping method by name
        /// </summary>
        /// <param name="name">The shipping method name</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipping method
        /// </returns>
        public virtual async Task<ShippingMethod> GetShippingMethodByNameAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            else
            {
                return (await _shippingService.GetAllShippingMethodsAsync(0)).Where(c => c.Name.ToLower() == name.ToLower()).FirstOrDefault();
            }
        }

        /// <summary>
        /// Gets a shipping method by order
        /// </summary>
        /// <param name="name">The shipping method name</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipping method
        /// </returns>
        public virtual async Task<ShippingMethod> GetShippingMethodByNameAsync(Order order)
        {
            string name = order.ShippingMethod;
            var shippingMethod = string.Empty;

            if (order.ShippingRateComputationMethodSystemName.Equals(ShippingManagerDefaults.AramexSystemName))
            {
                var carriers = await _carrierService.GetAllCarriersAsync();
                foreach (var carrier in carriers)
                {
                    if (name.Contains(carrier.Name))
                    {
                        var names = name.Split(carrier.Name + " - ");
                        if (names.Count() == 2)
                            shippingMethod = names[1];
                    }
                }
            }            

            if (string.IsNullOrEmpty(name))
                return null;
            else
            {
                return (await _shippingService.GetAllShippingMethodsAsync(0)).Where(c => c.Name.ToLower() == name.ToLower()).FirstOrDefault();
            }
        }

        /// <summary>
        /// Gets a shipping method by Id
        /// </summary>
        /// <param name="shippingMethodId">The shipping method identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipping method
        /// </returns>
        public virtual async Task<ShippingMethod> GetShippingMethodByIdAsync(int shippingMethodId)
        {
            if (shippingMethodId == 0)
                return null;
            else
            {
                return (await _shippingService.GetAllShippingMethodsAsync(0)).Where(c => c.Id == shippingMethodId).FirstOrDefault();
            }
        }

        /// <summary>
        /// Gets a carrier by Id
        /// </summary>
        /// <param name="carrierId">The shipping method identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipping method
        /// </returns>
        public virtual async Task<Carrier> GetCarrierByIdAsync(int carrierId)
        {
            if (carrierId == 0)
                return null;
            else
            {
                return await _carrierService.GetCarrierByIdAsync(carrierId);
            }
        }

        /// <summary>
        /// Prepare available shipping methods model
        /// </summary>
        /// <param name="selected">The selected shipping model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of shipping methods
        /// </returns>
        public virtual async Task<(IList<SelectListItem>, int index)> PrepareAvailableShippingMethodsModelAsync(bool addDefaultItem = true, int selected = 0, string shippingMethod = null)
        {
            bool found = false;

            var availableShippingMethods = new List<SelectListItem>();

            int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

            if (addDefaultItem)
                availableShippingMethods.Add(new SelectListItem { Text = "*", Value = "0" });

            var shippingMethods = await _shippingService.GetAllShippingMethodsAsync();
            foreach (var sm in shippingMethods.OrderBy(name => name.Name))
            {
                string shippingMethodName = sm.Name;

                if (vendorId == 0)
                {
                    var vendors = await _vendorService.GetAllVendorsAsync();
                    if (vendors.Count() > 0)
                    {
                        string vendorName = await _entityGroupService.GetVendorNameForEntityGroupAsync(sm);
                        shippingMethodName = shippingMethodName + " (" + vendorName + ")";
                    }
                }

                if (selected != 0)
                {
                    availableShippingMethods.Add(new SelectListItem { Text = shippingMethodName, Value = sm.Id.ToString(), Selected = sm.Id == selected });
                    found = true;
                }
                else
                    availableShippingMethods.Add(new SelectListItem { Text = shippingMethodName, Value = sm.Id.ToString() });
            }

            int index = 0;
            if (!found && selected == 0 && !string.IsNullOrEmpty(shippingMethod))
            {
                index = (availableShippingMethods.Count() + 1);
                var addShippingMethod = new SelectListItem { Text = shippingMethod, Value = index.ToString(), Selected = true };
                availableShippingMethods.Add(addShippingMethod);
            }

            return (availableShippingMethods, index);
        }

        /// <summary>
        /// Prepare available shipping methods model
        /// </summary>
        /// <param name="selected">The selected shipping model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of shipping methods
        /// </returns>
        public virtual async Task<(IList<SelectListItem>, int index)> PrepareShippingMethodsForShipmentAsync(string shippingMethodName = null, string friendlyName = null, int countryId = 0)
        {
            int index = 0;
            bool found = false;

            var availableShippingMethods = new List<SelectListItem>();

            int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

            if (!friendlyName.Contains(" - ") && !shippingMethodName.Contains(friendlyName))
            {
                if (!string.IsNullOrEmpty(friendlyName) && !string.IsNullOrEmpty(shippingMethodName))
                    shippingMethodName = friendlyName + " - " + shippingMethodName;
            }

            var shippingMethods = await GetShippingMethodListForShipment(false, countryId);
            foreach (var sm in shippingMethods.OrderBy(name => name.Name))
            {
                //if (vendorId != 0) // ToDo
                //{
                //    var vendors = await _vendorService.GetAllVendorsAsync();
                //    if (vendors.Count() > 0)
                //    {
                //        string vendorName = await _entityGroupService.GetVendorNameForEntityGroupAsync(sm);
                //        shippingMethodName = shippingMethodName + " (" + vendorName + ")";
                //    }
                //}

                if (sm.Name == shippingMethodName)
                {
                    availableShippingMethods.Add(new SelectListItem { Text = sm.Name, Value = sm.Id.ToString(), Selected = true });
                    index = sm.Id;
                    found = true;
                }
                else
                    availableShippingMethods.Add(new SelectListItem { Text = sm.Name, Value = sm.Id.ToString() });
            }


            if (!found && !string.IsNullOrEmpty(friendlyName))
            {
                index = (availableShippingMethods.Count() + 1);
                var addShippingMethod = new SelectListItem { Text = shippingMethodName, Value = index.ToString(), Selected = true };
                availableShippingMethods.Add(addShippingMethod);
            }

            return (availableShippingMethods, index);
        }

        /// <summary>
        /// Prepare available shipping methods model as a selectable list
        /// </summary>
        /// <param name="selected">The selected shipping model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the selectable list of shipping methods
        /// </returns>
        public virtual async Task<IList<SelectListItem>> PrepareAvailableCarriersModelAsync(bool addDefaultItem, int selected = 0)
        {
            var availableCarriers = new List<SelectListItem>();

            int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

            if (addDefaultItem)
                availableCarriers.Add(new SelectListItem { Text = "*", Value = "0" });

            foreach (var carrier in await _carrierService.GetAllCarriersAsync())
            {
                string carrierName = carrier.Name;

                if (vendorId == 0)
                {
                    string vendorName = await _entityGroupService.GetVendorNameForEntityGroupAsync(carrier);
                    carrierName = carrierName + " (" + vendorName + ")";
                }

                if (selected != 0)
                    availableCarriers.Add(new SelectListItem { Text = carrierName, Value = carrier.Id.ToString(), Selected = carrier.Id == selected });
                else
                    availableCarriers.Add(new SelectListItem { Text = carrierName, Value = carrier.Id.ToString() });
            }

            return availableCarriers;
        }

        /// <summary>
        /// Get a paged list of all shipping by weight records
        /// </summary>
        /// <param name="vendorId">Vendor Indetifier</param> 
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the paged list of shipping by weight records
        /// </returns> 
        public virtual async Task<IList<ShippingManagerByWeightByTotal>> GetAllRatesAsync(int vendorId = 0, int filterByCountryId = 0)
        {

            var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(ShippingManagerDefaults.ShippingMethodsByVendorAllKey, filterByCountryId, vendorId);

            var query = await _sbwtRepository.GetAllAsync(query =>
            {
                return from sbw in _sbwtRepository.Table
                       where (vendorId == 0 || vendorId == sbw.VendorId) &&
                             (filterByCountryId == 0 || sbw.CountryId == 0 || filterByCountryId == sbw.CountryId)
                       orderby sbw.StoreId, sbw.VendorId, sbw.WarehouseId, sbw.CarrierId, sbw.CountryId, sbw.StateProvinceId,
                               sbw.Zip, sbw.ShippingMethodId, sbw.WeightFrom, sbw.OrderSubtotalFrom
                       select sbw;
            }, cache => cacheKey);

            return query.ToList();
        }

        /// <summary>
        /// Get a paged list of all shipping by weight records
        /// </summary>
        /// <param name="vendorId">Vendor Indetifier</param> 
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the paged list of shipping by weight records
        /// </returns> 
        public virtual async Task<IPagedList<ShippingManagerByWeightByTotal>> GetAllRatesPagedAsync(int vendorId = 0, int filterByCountryId = 0, int pageIndex = 0, int pageSize = int.MaxValue)
        {

            var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(ShippingManagerDefaults.ShippingMethodsByVendorAllKey, filterByCountryId, vendorId);

            var query = await _sbwtRepository.GetAllAsync(query =>
            {
                return from sbw in _sbwtRepository.Table
                       where (vendorId == 0 || vendorId == sbw.VendorId) &&
                             (filterByCountryId == 0 || sbw.CountryId == 0 || filterByCountryId == sbw.CountryId)
                       orderby sbw.DisplayOrder, sbw.StoreId, sbw.VendorId, sbw.WarehouseId, sbw.CarrierId, sbw.CountryId, sbw.StateProvinceId,
                               sbw.Zip, sbw.ShippingMethodId, sbw.WeightFrom, sbw.OrderSubtotalFrom
                       select sbw;
            }, cache => cacheKey);

            var records = new PagedList<ShippingManagerByWeightByTotal>(query, pageIndex, pageSize);

            return records;
        }

        /// <summary>
        /// Filter Shipping Weight Records
        /// </summary>
        /// <param name="shippingMethodId">Shipping method identifier</param>
        /// <param name="storeId">Store identifier</param>
        /// <param name="vendorId">Vendor identifier</param>
        /// <param name="warehouseId">Warehouse identifier</param>
        /// <param name="carrierId">Carrier identifier</param>
        /// <param name="countryId">Country indentifier</param>
        /// <param name="stateProvinceId">State Province identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of shipping by weight records
        /// </returns> 
        public virtual async Task<IList<ShippingManagerByWeightByTotal>> GetRecordsAsync(int shippingMethodId = 0, int storeId = 0, int vendorId = 0,
            int warehouseId = 0, int carrierId = 0, int countryId = 0, int stateProvinceId = 0, string zip = null)
        {

            var existingRates = await GetAllRatesAsync(vendorId, countryId);

            //filter by shipping method
            var matchedByMethod = shippingMethodId == 0
                ? existingRates
                : existingRates.Where(sbw => sbw.ShippingMethodId == shippingMethodId);

            //filter by store
            var matchedByStore = storeId == 0
                ? matchedByMethod
                : matchedByMethod.Where(r => r.StoreId == storeId || r.StoreId == 0);

            //filter by warehouse
            var matchedByWarehouse = warehouseId == 0
                ? matchedByStore
                : matchedByStore.Where(r => r.WarehouseId == warehouseId || r.WarehouseId == 0);

            //filter by carrier
            var matchedByCarrier = carrierId == 0
                ? matchedByWarehouse
                : matchedByWarehouse.Where(r => r.CarrierId == carrierId || r.CarrierId == 0);

            //filter by country
            var matchedByCountry = countryId == 0
                ? matchedByCarrier
                : matchedByCarrier.Where(r => r.CountryId == countryId || r.CountryId == 0);

            //filter by state/province
            var matchedByStateProvince = stateProvinceId == 0
                ? matchedByCountry
                : matchedByCountry.Where(r => r.StateProvinceId == stateProvinceId || r.StateProvinceId == 0);

            //filter by zip
            var matchedByZip = string.IsNullOrEmpty(zip)
                ? matchedByStateProvince
                : matchedByStateProvince.Where(r => string.IsNullOrEmpty(r.Zip) || r.Zip.Equals(zip, StringComparison.InvariantCultureIgnoreCase));

            var records = matchedByZip.ToList();

            return records;
        }

        /// <summary>
        /// Filter Shipping Weight Records
        /// </summary>
        /// <param name="shippingMethodId">Shipping method identifier</param>
        /// <param name="storeId">Store identifier</param>
        /// <param name="vendorId">Vendor identifier</param>
        /// <param name="warehouseId">Warehouse identifier</param>
        /// <param name="carrierId">Carrier identifier</param>
        /// <param name="countryId">Country indentifier</param>
        /// <param name="stateProvinceId">State Province identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of shipping by weight records
        /// </returns> 
        public virtual async Task<IList<ShippingManagerByWeightByTotal>> GetRecordsAsync(int shippingMethodId = 0, int storeId = 0, int vendorId = 0,
            int warehouseId = 0, int carrierId = 0, int countryId = 0, int stateProvinceId = 0, string zip = null, decimal? weight = null, decimal? orderSubtotal = null)
        {

            zip = zip?.Trim() ?? string.Empty;

            //filter by weight and shipping method
            var existingRates = (await GetAllRatesAsync(vendorId, countryId))
                .Where(sbw => sbw.ShippingMethodId != 0 && (!weight.HasValue || weight >= sbw.WeightFrom && weight <= sbw.WeightTo))
                .ToList();

            //filter by order subtotal
            var matchedBySubtotal = !orderSubtotal.HasValue ? existingRates :
                existingRates.Where(sbw => orderSubtotal >= sbw.OrderSubtotalFrom && orderSubtotal <= sbw.OrderSubtotalTo);

            //filter by shipping method
            var matchedByMethod = shippingMethodId == 0
                ? matchedBySubtotal
                : matchedBySubtotal.Where(sbw => sbw.ShippingMethodId == shippingMethodId);

            //filter by store
            var matchedByStore = storeId == 0
                ? matchedByMethod
                : matchedByMethod.Where(r => r.StoreId == storeId || r.StoreId == 0);

            //filter by warehouse
            var matchedByWarehouse = warehouseId == 0
                ? matchedByStore
                : matchedByStore.Where(r => r.WarehouseId == warehouseId || r.WarehouseId == 0);

            //filter by carrier
            var matchedByCarrier = carrierId == 0
                ? matchedByWarehouse
                : matchedByWarehouse.Where(r => r.CarrierId == carrierId || r.CarrierId == 0);

            //filter by country
            var matchedByCountry = countryId == 0
                ? matchedByCarrier
                : matchedByCarrier.Where(r => r.CountryId == countryId || r.CountryId == 0);

            //filter by state/province
            var matchedByStateProvince = stateProvinceId == 0
                ? matchedByCountry
                : matchedByCountry.Where(r => r.StateProvinceId == stateProvinceId || r.StateProvinceId == 0);

            //filter by zip
            var matchedByZip = string.IsNullOrEmpty(zip)
                ? matchedByStateProvince
                : matchedByStateProvince.Where(r => string.IsNullOrEmpty(r.Zip) || r.Zip.Equals(zip, StringComparison.InvariantCultureIgnoreCase));

            var records = matchedByZip.ToList();

            return records;
        }

        /// <summary>
        /// Filter Shipping Weight Records from existing list
        /// </summary>
        /// <param name="storeId">Store identifier</param>
        /// <param name="vendorId">Vendor identifier</param>
        /// <param name="warehouseId">Warehouse identifier</param>
        /// <param name="carrierId">Carrier identifier</param>
        /// <param name="countryId">Country identifier</param>
        /// <param name="stateProvinceId">State identifier</param>
        /// <param name="zip">Zip postal code</param>
        /// <param name="weight">Weight</param>
        /// <param name="orderSubtotal">Order subtotal</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of shipping by weight records
        /// </returns> 
        public virtual ShippingManagerByWeightByTotal FindMethodAsync(ShippingManagerByWeightByTotal smbwbt, 
            int storeId, int vendorId, int warehouseId, int carrierId,
            int countryId, int stateProvinceId, string zip, decimal? weight, decimal? orderSubtotal)
        {
            zip = zip?.Trim() ?? string.Empty;

            var smbwbtList = new List<ShippingManagerByWeightByTotal>();

            if (smbwbt != null && 
                (smbwbt.VendorId == 0 || vendorId == 0 || smbwbt.VendorId == vendorId) &&
                (smbwbt.CountryId == 0 || countryId == 0 || smbwbt.CountryId == countryId))
                smbwbtList.Add(smbwbt);

            //filter active methods by weight and shipping method
            var existingRates = smbwbtList
                .Where(sbw => sbw.ShippingMethodId != 0 &&
                    (!weight.HasValue || weight >= sbw.WeightFrom && weight <= sbw.WeightTo) &&
                    (sbw.Active == true))
                .ToList();

            //filter by order subtotal
            var matchedBySubtotal = !orderSubtotal.HasValue ? existingRates :
                existingRates.Where(sbw => orderSubtotal >= sbw.OrderSubtotalFrom && orderSubtotal <= sbw.OrderSubtotalTo);

            //filter by store
            var matchedByStore = storeId == 0
                ? matchedBySubtotal
                : matchedBySubtotal.Where(r => r.StoreId == storeId || r.StoreId == 0);

            //filter by warehouse
            var matchedByWarehouse = warehouseId == 0
                ? matchedByStore
                : matchedByStore.Where(r => r.WarehouseId == warehouseId || r.WarehouseId == 0);

            //filter by carrier
            var matchedByCarrier = carrierId == 0
                ? matchedByWarehouse
                : matchedByWarehouse.Where(r => r.CarrierId == carrierId || r.CarrierId == 0);

            //filter by country
            var matchedByCountry = countryId == 0
                ? matchedByCarrier
                : matchedByCarrier.Where(r => r.CountryId == countryId || r.CountryId == 0);

            //filter by state/province
            var matchedByStateProvince = stateProvinceId == 0
                ? matchedByCountry
                : matchedByCountry.Where(r => r.StateProvinceId == stateProvinceId || r.StateProvinceId == 0);

            //filter by zip
            var matchedByZip = string.IsNullOrEmpty(zip)
                ? matchedByStateProvince
                : matchedByStateProvince.Where(r => string.IsNullOrEmpty(r.Zip) || r.Zip.Equals(zip, StringComparison.InvariantCultureIgnoreCase));

            return matchedByZip.FirstOrDefault();
        }

        /// <summary>
        /// Filter Shipping Weight Records
        /// </summary>
        /// <param name="storeId">Store identifier</param>
        /// <param name="vendorId">Vendor identifier</param>
        /// <param name="warehouseId">Warehouse identifier</param>
        /// <param name="carrierId">Carrier identifier</param>
        /// <param name="countryId">Country identifier</param>
        /// <param name="stateProvinceId">State identifier</param>
        /// <param name="zip">Zip postal code</param>
        /// <param name="weight">Weight</param>
        /// <param name="orderSubtotal">Order subtotal</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of shipping by weight records
        /// </returns> 
        public virtual async Task<IList<ShippingManagerByWeightByTotal>> FindMethodsAsync(int storeId, int vendorId, int warehouseId, int carrierId,
            int countryId, int stateProvinceId, string zip, decimal? weight, decimal? orderSubtotal, int shippingMethodId = 0)
        {

            var existingRates = new List<ShippingManagerByWeightByTotal>();

            zip = zip?.Trim() ?? string.Empty;

            //filter by weight and shipping method

            if (shippingMethodId == 0)
            {
                existingRates = (await GetAllRatesAsync(vendorId, countryId))
                    .Where(sbw => sbw.ShippingMethodId != 0 && (!weight.HasValue || weight >= sbw.WeightFrom && weight <= sbw.WeightTo) &&
                          (sbw.Active == true))
                    .ToList();
            }
            else
            {
                existingRates = (await GetAllRatesAsync(vendorId, countryId))
                    .Where(sbw => sbw.ShippingMethodId == shippingMethodId && (!weight.HasValue || weight >= sbw.WeightFrom && weight <= sbw.WeightTo)&&
                          (sbw.Active == true))
                    .ToList();
            }

            //filter by order subtotal
            var matchedBySubtotal = !orderSubtotal.HasValue ? existingRates :
                existingRates.Where(sbw => orderSubtotal >= sbw.OrderSubtotalFrom && orderSubtotal <= sbw.OrderSubtotalTo);

            //filter by store
            var matchedByStore = storeId == 0
                ? matchedBySubtotal
                : matchedBySubtotal.Where(r => r.StoreId == storeId || r.StoreId == 0);

            //filter by warehouse
            var matchedByWarehouse = warehouseId == 0
                ? matchedByStore
                : matchedByStore.Where(r => r.WarehouseId == warehouseId || r.WarehouseId == 0);

            //filter by carrier
            var matchedByCarrier = carrierId == 0
                ? matchedByWarehouse
                : matchedByWarehouse.Where(r => r.CarrierId == carrierId || r.CarrierId == 0);

            //filter by country
            var matchedByCountry = countryId == 0
                ? matchedByCarrier
                : matchedByCarrier.Where(r => r.CountryId == countryId || r.CountryId == 0);

            //filter by state/province
            var matchedByStateProvince = stateProvinceId == 0
                ? matchedByCountry
                : matchedByCountry.Where(r => r.StateProvinceId == stateProvinceId || r.StateProvinceId == 0);

            //filter by zip
            var matchedByZip = string.IsNullOrEmpty(zip)
                ? matchedByStateProvince
                : matchedByStateProvince.Where(r => string.IsNullOrEmpty(r.Zip) || r.Zip.Equals(zip, StringComparison.InvariantCultureIgnoreCase));

            //sort from particular to general, more particular cases will be the first
            var foundRecords = matchedByZip.OrderBy(r => r.DisplayOrder)
                .ThenBy(r => r.StoreId == 0)
                .ThenBy(r => r.VendorId != 0)
                .ThenBy(r => r.WarehouseId == 0)
                .ThenBy(r => r.CountryId == 0)
                .ThenBy(r => r.StateProvinceId == 0)
                .ThenBy(r => string.IsNullOrEmpty(r.Zip));

            var records = foundRecords.ToList();

            return records;
        }

        /// <summary>
        /// Find shipping weight records
        /// </summary>
        /// <param name="shippingMethodId">Shipping method identifier</param>
        /// <param name="storeId">Store identifier</param>
        /// <param name="vendorId">Vendor identifier</param>
        /// <param name="warehouseId">Warehouse identifier</param>
        /// <param name="carrierId">Carrier identifier</param>
        /// <param name="countryId">Country identifier</param>
        /// <param name="stateProvinceId">State identifier</param>
        /// <param name="zip">Zip postal code</param>
        /// <param name="weight">Weight</param>
        /// <param name="orderSubtotal">Order subtotal</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the paged list of shipping by weight records
        /// </returns> 
        public virtual async Task<IPagedList<ShippingManagerByWeightByTotal>> FindRecordsAsync(int shippingMethodId, int storeId, int vendorId, int warehouseId, int carrierId,
            int countryId, int stateProvinceId, string zip, decimal? weight, decimal? orderSubtotal, bool active, int pageIndex, int pageSize)
        {
            zip = zip?.Trim() ?? string.Empty;

            //filter active methods by weight and shipping method
            var existingRates = (await GetAllRatesAsync(vendorId, countryId))
                .Where(sbw => 
                        (shippingMethodId == 0 || sbw.ShippingMethodId == shippingMethodId) && 
                        (!weight.HasValue || weight >= sbw.WeightFrom && weight <= sbw.WeightTo) && 
                        (sbw.Active == active))
                .ToList();

            //filter by order subtotal
            var matchedBySubtotal = !orderSubtotal.HasValue ? existingRates :
                existingRates.Where(sbw => orderSubtotal >= sbw.OrderSubtotalFrom && orderSubtotal <= sbw.OrderSubtotalTo);

            if (shippingMethodId == 0)
                matchedBySubtotal = await GetAllRatesAsync(vendorId, countryId);

            //filter by store
            var matchedByStore = storeId == 0
                ? matchedBySubtotal
                : matchedBySubtotal.Where(r => r.StoreId == storeId || r.StoreId == 0);

            //filter by warehouse
            var matchedByWarehouse = warehouseId == 0
                ? matchedByStore
                : matchedByStore.Where(r => r.WarehouseId == warehouseId || r.WarehouseId == 0);

            //filter by carrier
            var matchedByCarrier = carrierId == 0
                ? matchedByWarehouse
                : matchedByWarehouse.Where(r => r.CarrierId == carrierId || r.CarrierId == 0);

            //filter by country
            var matchedByCountry = countryId == 0
                ? matchedByCarrier
                : matchedByCarrier.Where(r => r.CountryId == countryId || r.CountryId == 0);

            //filter by state/province
            var matchedByStateProvince = stateProvinceId == 0
                ? matchedByCountry
                : matchedByCountry.Where(r => r.StateProvinceId == stateProvinceId || r.StateProvinceId == 0);

            //filter by zip
            var matchedByZip = string.IsNullOrEmpty(zip)
                ? matchedByStateProvince
                : matchedByStateProvince.Where(r => string.IsNullOrEmpty(r.Zip) || r.Zip.Equals(zip, StringComparison.InvariantCultureIgnoreCase));

            //sort from particular to general, more particular cases will be the first
            var foundRecords = matchedByZip.OrderBy(r => r.DisplayOrder)
                .ThenBy(r => r.StoreId == 0)
                .ThenBy(r => r.VendorId != 0)
                .ThenBy(r => r.WarehouseId == 0)
                .ThenBy(r => r.CountryId == 0)
                .ThenBy(r => r.StateProvinceId == 0)
                .ThenBy(r => string.IsNullOrEmpty(r.Zip));

            var records = new PagedList<ShippingManagerByWeightByTotal>(foundRecords.ToList(), pageIndex, pageSize);

            return records;
        }

        /// <summary>
        /// Get a shipping by weight record by passed parameters
        /// </summary>
        /// <param name="shippingMethodId">Shipping method identifier</param>
        /// <param name="storeId">Store identifier</param>
        /// <param name="vendorId">Vendor identifier</param>
        /// <param name="warehouseId">Warehouse identifier</param>
        /// <param name="carrierId">Carrier identifier</param>
        /// <param name="countryId">Country identifier</param>
        /// <param name="stateProvinceId">State identifier</param>
        /// <param name="zip">Zip postal code</param>
        /// <param name="weight">Weight</param>
        /// <param name="orderSubtotal">Order subtotal</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the paged list of shipping by weight records
        /// </returns> 
        public virtual async Task<ShippingManagerByWeightByTotal> FindRecordsAsync(int shippingMethodId, int storeId, int vendorId, int warehouseId,
            int countryId, int stateProvinceId, string zip, decimal weight, decimal orderSubtotal, bool active)
        {
            var foundRecords = await FindRecordsAsync(shippingMethodId, storeId, vendorId, warehouseId, 0, countryId, stateProvinceId, zip, weight, orderSubtotal, active, 0, int.MaxValue);
            return foundRecords.FirstOrDefault();
        }

        /// <summary>
        /// Get list of shipping methods for configuration
        /// </summary>
        /// <param name="active">Active</param>
        /// <param name="vendorId">Vendor identifier</param>
        /// <returns>Shipping methods by display order</returns>
        public virtual async Task<List<ShippingMethod>> GetShippingMethodListForShipment(bool active = true, int countryId = 0, int vendorId = 0)
        {
            var shippingMethodList = new List<ShippingMethod>();

            var query = from sbw in _sbwtRepository.Table
                        join sm in _shippingMethodRepository.Table.ToList() on sbw.ShippingMethodId equals sm.Id
                        where (!active || sbw.Active == active) &&
                              (vendorId == 0 || sbw.VendorId == vendorId) &&
                              //!string.IsNullOrEmpty(sbw.FriendlyName) &&
                              (countryId == 0 || sbw.CountryId == countryId)
                        orderby sm.DisplayOrder
                        select sbw;

            foreach (var sbw in query)
            {
                var newShippingMethod = new ShippingMethod();
                var shippingMethod = await _shippingService.GetShippingMethodByIdAsync(sbw.ShippingMethodId);
                if (shippingMethod != null)
                {
                    newShippingMethod.Id = sbw.Id;
                    newShippingMethod.DisplayOrder = shippingMethod.DisplayOrder;
                    newShippingMethod.Description = shippingMethod.Description;

                    if (!string.IsNullOrEmpty(sbw.FriendlyName))
                        newShippingMethod.Name = sbw.FriendlyName;
                    else
                        newShippingMethod.Name = shippingMethod.Name;

                    shippingMethodList.Add(newShippingMethod);
                }
            }

            return shippingMethodList;
        }

        /// <summary>
        /// Get a shipping method by passed parameters
        /// </summary>
        /// <param name="friendlyName">Friendly Name identifier</param>
        /// <param name="vendorId">Vendor identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipping method
        /// </returns> 
        public virtual async Task<(ShippingMethod, ShippingManagerByWeightByTotal)> GetShippingMethodFromFriendlyNameAsync(string friendlyName, int vendorId = 0)
        {

            var query = from sbw in _sbwtRepository.Table
                        join sm in _shippingMethodRepository.Table.ToList() on sbw.ShippingMethodId equals sm.Id
                        where (vendorId == 0 || vendorId == sbw.VendorId) &&
                            sbw.FriendlyName == friendlyName
                        orderby sm.DisplayOrder
                        select sbw;

            if (query != null && query.Count() != 0)
            {
                var returnQuery = query.FirstOrDefault();
                var shippingMethod = await _shippingService.GetShippingMethodByIdAsync(returnQuery.ShippingMethodId);
                if (shippingMethod != null)
                    return (shippingMethod, returnQuery);
            }

            return (null, null);
        }

        /// <summary>
        /// Get shipping method display order by passed parameters
        /// </summary>
        /// <param name="friendlyName">Friendly Name identifier</param>
        /// <param name="vendorId">Vendor identifier</param>
        /// <returns>Shipping method display order</returns>
        public virtual async Task<int> GetShippingMethodOrderFromName(string name, int vendorId = 0)
        {
            var query = from sbw in _sbwtRepository.Table
                        join sm in _shippingMethodRepository.Table.ToList() on sbw.ShippingMethodId equals sm.Id
                        where (vendorId == 0 || vendorId == sbw.VendorId) &&
                            sbw.FriendlyName == name
                        orderby sm.DisplayOrder
                        select sbw;

            if (query != null && query.Count() != 0)
            {
                var returnQuery = query.FirstOrDefault();
                var shippingMethod = await _shippingService.GetShippingMethodByIdAsync(returnQuery.ShippingMethodId);
                if (shippingMethod != null)
                    return shippingMethod.DisplayOrder;
            }
            else
            {
                var shippingMethods = await _shippingService.GetAllShippingMethodsAsync();
                var shippingMethod = shippingMethods.Where(n => n.Name == name).FirstOrDefault();
                if (shippingMethod != null)
                    return shippingMethod.DisplayOrder;
            }

            return 0;
        }


        /// <summary>
        /// Get a shipping by weight record by passed parameters
        /// </summary>
        /// <param name="friendlyName">Friendly Name identifier</param>
        /// <param name="vendorId">Vendor identifier</param>
        /// <returns>Shipping by weight record</returns>
        public virtual async Task<ShippingMethod> GetShippingMethodSendFromAddress(int shippingMethodId, int vendorId = 0)
        {

            var query = from sbw in _sbwtRepository.Table
                        join sm in _shippingMethodRepository.Table.ToList() on sbw.ShippingMethodId equals sm.Id
                        where (vendorId == 0 || vendorId == sbw.VendorId) &&
                            sbw.ShippingMethodId == shippingMethodId
                        orderby sm.DisplayOrder
                        select sbw;

            if (query != null && query.Count() != 0)
            {
                var shippingMethod = await _shippingService.GetShippingMethodByIdAsync(query.FirstOrDefault().ShippingMethodId);
                if (shippingMethod != null)
                    return shippingMethod;
            }

            return null;
        }

        /// <summary>
        /// Get all shiping methods for export
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of shipping methods
        /// </returns> 
        public async Task<List<ExportRatesModel>> GetRatesForExportAsync()
        {

            int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

            var rates = await GetAllRatesAsync(vendorId);

            return await rates.SelectAwait(async x =>
            {
                var ratesModel = new ExportRatesModel
                {
                    Id = x.Id,
                    Store = x.StoreId,
                    Active = x.Active,
                    DisplayOrder = x.DisplayOrder,

                    Vendor = x.VendorId == 0 ? x.VendorId.ToString() :
                        (await _vendorService.GetVendorByIdAsync(x.VendorId) == null ? "0" : (await _vendorService.GetVendorByIdAsync(x.VendorId)).Name),
                    Warehouse = x.WarehouseId == 0 ? x.WarehouseId.ToString() :
                        (await _shippingService.GetWarehouseByIdAsync(x.WarehouseId) == null ? "0" : (await _shippingService.GetWarehouseByIdAsync(x.WarehouseId)).Name),
                    Carrier = x.CarrierId == 0 ? x.CarrierId.ToString() :
                        (await _carrierService.GetCarrierByIdAsync(x.CarrierId) == null ? "0" : (await _carrierService.GetCarrierByIdAsync(x.CarrierId)).Name),
                    ShippingMethod = x.ShippingMethodId == 0 ? x.ShippingMethodId.ToString() :
                        (await _shippingService.GetShippingMethodByIdAsync(x.ShippingMethodId) == null ? "0" : (await _shippingService.GetShippingMethodByIdAsync(x.ShippingMethodId)).Name),
                    Country = x.CountryId == 0 ? x.CountryId.ToString() :
                        (await _countryService.GetCountryByIdAsync(x.CountryId) == null ? "0" : (await _countryService.GetCountryByIdAsync(x.CountryId)).Name),
                    StateProvince = x.StateProvinceId == 0 ? x.StateProvinceId.ToString() :
                        (await _stateProvinceService.GetStateProvinceByIdAsync(x.StateProvinceId) == null ? "0" : (await _stateProvinceService.GetStateProvinceByIdAsync(x.StateProvinceId)).Name),
                    CutOffTime = x.CutOffTimeId == 0 ? x.CutOffTimeId.ToString() :
                        (await _carrierService.GetCutOffTimeByIdAsync(x.CutOffTimeId) == null ? "0" : (await _carrierService.GetCutOffTimeByIdAsync(x.CutOffTimeId)).Name),

                    PostcodeZip = x.Zip,
                    WeightFrom = x.WeightFrom.ToString(),
                    WeightTo = x.WeightTo.ToString(),
                    CalculateCubicWeight = x.CalculateCubicWeight ? "Yes" : "No",
                    CubicWeightFactor = x.CubicWeightFactor.ToString(),
                    OrderSubtotalFrom = x.OrderSubtotalFrom.ToString(),
                    OrderSubtotalTo = x.OrderSubtotalTo.ToString(),
                    AdditionalFixedCost = x.AdditionalFixedCost.ToString(),
                    PercentageRateOfSubtotal = x.PercentageRateOfSubtotal.ToString(),
                    RatePerWeightUnit = x.RatePerWeightUnit.ToString(),
                    LowerWeightLimit = x.LowerWeightLimit.ToString(),
                    FriendlyName = x.FriendlyName,
                    TransitDays = x.TransitDays,
                    SendFromAddress = x.SendFromAddressId,
                    Description = x.Description
                };

                return ratesModel;

            }).ToListAsync();

        }

        /// <summary>
        /// Get a shipping by weight record by identifier
        /// </summary>
        /// <param name="shippingByWeightRecordId">Record identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the  shipping by weight record
        /// </returns> 
        public virtual async Task<ShippingManagerByWeightByTotal> GetByIdAsync(int shippingByWeightRecordId)
        {
            if (shippingByWeightRecordId == 0)
                return null;

            return await _sbwtRepository.GetByIdAsync(shippingByWeightRecordId);
        }

        /// <summary>
        /// Insert the shipping by weight record
        /// </summary>
        /// <param name="shippingByWeightRecord">Shipping by weight record</param>
        /// <returns>task that represents the asynchronous operation</returns>
        public virtual async Task InsertShippingByWeightRecordAsync(ShippingManagerByWeightByTotal shippingByWeightRecord)
        {
            await _sbwtRepository.InsertAsync(shippingByWeightRecord);

            await _staticCacheManager.RemoveByPrefixAsync(NopEntityCacheDefaults<ShippingMethod>.AllPrefix);
        }

        /// <summary>
        /// Update the shipping by weight record
        /// </summary>
        /// <param name="shippingByWeightRecord">Shipping by weight record</param>
        /// <returns>task that represents the asynchronous operation</returns> 
        public virtual async Task UpdateShippingByWeightRecordAsync(ShippingManagerByWeightByTotal shippingByWeightRecord)
        {
            await _sbwtRepository.UpdateAsync(shippingByWeightRecord);

            await _staticCacheManager.RemoveByPrefixAsync(NopEntityCacheDefaults<ShippingMethod>.AllPrefix);
        }

        /// <summary>
        /// Delete the shipping by weight record
        /// </summary>
        /// <param name="shippingByWeightRecord">Shipping by weight record</param>
        /// <returns>task that represents the asynchronous operation</returns>
        public virtual async Task DeleteShippingByWeightRecordAsync(ShippingManagerByWeightByTotal shippingByWeightRecord)
        {
            await _sbwtRepository.DeleteAsync(shippingByWeightRecord);

            await _staticCacheManager.RemoveByPrefixAsync(NopEntityCacheDefaults<ShippingMethod>.AllPrefix);
        }

        #endregion

        #region ShippingServices

        /// <summary>
        /// Gets an URL for a page to show tracking info (third party tracking page).
        /// </summary>
        /// <param name="trackingNumber">The tracking number to track.</param>
        /// <returns>URL of a tracking page.</returns>
        public virtual string GetSendCloudTrackingUrl(string trackingNumber)
        {
            return $"https://tracking.sendcloud.sc/forward?carrier=sendcloud&code={trackingNumber}&type=letter&verification=9999";
        }

        /// <summary>
        /// Gets an URL for a page to show tracking info (third party tracking page).
        /// </summary>
        /// <param name="trackingNumber">The tracking number to track.</param>
        /// <returns>URL of a tracking page.</returns>
        public virtual string GetUrl(string trackingNumber)
        {
            return $"https://tracking.sendcloud.sc/forward?carrier=sendcloud&code={trackingNumber}&type=letter&verification=9999";
        }

        /// <summary>
        /// Gets all events for a tracking number
        /// </summary>
        /// <param name="trackingNumber">The tracking number to track</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipment events
        /// </returns> 
        public virtual async Task<IEnumerable<ShipmentStatusEvent>> GetShipmentEventsAsync(string trackingNumber)
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            try
            {
                ////create request details
                //var request = CreateTrackRequest(trackingNumber);

                ////get tracking info
                //var response = TrackAsync(request).Result;
                //return response.Shipment?
                //    .SelectMany(shipment => shipment.Package?
                //        .SelectMany(package => package.Activity?
                //            .Select(activity => PrepareShipmentStatusEvent(activity))));

                return new List<ShipmentStatusEvent>();
            }
            catch (Exception exception)
            {
                //log errors
                var message = $"Error while getting Shipping Manager tracking info - {trackingNumber}{Environment.NewLine}{exception.Message}";
                await _logger.ErrorAsync(message, exception, customer);

                return new List<ShipmentStatusEvent>();
            }
        }

        /// <summary>
        /// Gets a shipments for a tracking number
        /// </summary>
        /// <param name="trackingNumber">The tracking number to track</param>
        /// <returns>the shipment for thye tracking number</returns>
        public virtual Shipment GetShipmentForTrackingNumber(string trackingNumber)
        {

            var query = from shipment in _shipmentRepository.Table
                        where shipment.TrackingNumber == trackingNumber
                        select shipment;

            return query.FirstOrDefault();

        }

        #endregion

        #region PackagingOptions

        /// <summary>
        /// Gets the default packaging option if service is enabled
        /// </summary>
        /// <param name="trackingNumber">The tracking number to track.</param>
        /// <returns>URL of a tracking page.</returns>
        public async Task<PackagingOption> GetDefaultPackagingOption()
        {
            PackagingOption packagingOption = null;
            if (_shippingManagerSettings.UsePackagingSystem)
            {
                var materialsManagementSettingsEnabled = await _settingService.GetSettingByKeyAsync<bool?>(ShippingManagerDefaults.MATERIALS_MANAGEMENT_SETTINGS_ENABLED, false);
                if (materialsManagementSettingsEnabled.Value)
                {
                    //ToDo - read from Materials System
                }
                else
                {
                    packagingOption = _packagingOptionService.GetSimplePackagingOptions(true).FirstOrDefault();
                }
            }

            return packagingOption;
        }


        /// <summary>
        /// Inserts a shipment item packaging option
        /// </summary>
        /// <param name="shipmentItem">The shipment item</param>
        /// <param name="shipment">The shipment</param>
        /// <param name="packagingOption">The packaging option </param>
        /// <returns>URL of a tracking page.</returns>
        public async Task<ShipmentDetails> InsertShipmentDetails(Shipment shipment, PackagingOption packagingOption)
        {
            ShipmentDetails shipmentDetails = null;

            if (_shippingManagerSettings.UsePackagingSystem)
            {
                if (packagingOption != null)
                {
                    shipmentDetails = new ShipmentDetails();
                    shipmentDetails.OrderShipmentId = shipment.Id;
                    shipmentDetails.PackagingOptionItemId = packagingOption.Id;
                    shipmentDetails.ShippingMethodId = 0;
                    shipmentDetails.ShipmentId = string.Empty;
                    shipmentDetails.Cost = 0;
                    shipmentDetails.Group = string.Empty;
                    shipmentDetails.LabelUrl = string.Empty;
                    shipmentDetails.ManifestUrl = string.Empty;
                    shipmentDetails.CustomValuesXml = string.Empty;

                    await _shipmentDetailsService.InsertShipmentDetailsAsync(shipmentDetails);
                }
            }

            return shipmentDetails;
        }

        #endregion

    }
}
