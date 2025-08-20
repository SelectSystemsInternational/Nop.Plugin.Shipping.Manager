using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Services.Shipping.Tracking;
using Nop.Web.Areas.Admin.Models.Common;

using Nop.Plugin.Shipping.Manager.Domain;
using Nop.Plugin.Shipping.Manager.Models;

using Nop.Plugin.Apollo.Integrator;

namespace Nop.Plugin.Shipping.Manager.Services
{
    /// <summary>
    /// Represents service shipping by weight service
    /// </summary>
    public partial interface IShippingManagerService
    {

        #region Methods

        /// <summary>
        /// Gets th active store scope
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the current store scope
        /// </returns>
        Task<int> GetActiveStoreScopeConfiguration();

        /// <summary>
        /// Deturmines if access is availbale using Config Keys and ACL
        /// </summary>
        /// <param name="carrierId">The carrier identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result true if access is availbale of faluse otherwise
        /// </returns>
        Task<bool> AuthorizeAsync(SystemHelper.AccessMode requestedAccess, string publicKey = "", string privateKey = "");

        /// <summary>
        /// Gets a shipping method by name
        /// </summary>
        /// <param name="name">The shipping method name</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipping method
        /// </returns>
        Task<ShippingMethod> GetShippingMethodByNameAsync(string name);

        /// <summary>
        /// Gets a shipping method by order
        /// </summary>
        /// <param name="name">The shipping method name</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipping method
        /// </returns>
        Task<ShippingMethod> GetShippingMethodByNameAsync(Order order);

        /// <summary>
        /// Gets a shipping method by Id
        /// </summary>
        /// <param name="shippingMethodId">The shipping mwethod identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipping method
        /// </returns>
        Task<ShippingMethod> GetShippingMethodByIdAsync(int shippingMethodId);

        /// <summary>
        /// Gets a carrier by Id
        /// </summary>
        /// <param name="carrierId">The shipping method identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipping method
        /// </returns>
        Task<Carrier> GetCarrierByIdAsync(int carrierId);

        /// <summary>
        /// Prepare available shipping methods model
        /// </summary>
        /// <param name="selected">The selected shipping model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of shipping methods
        /// </returns>
        Task<(IList<SelectListItem>, int index)> PrepareAvailableShippingMethodsModelAsync(bool addDefaultItem = true, int selected = 0, string shippingMethod = null);

        /// <summary>
        /// Prepare available shipping methods model
        /// </summary>
        /// <param name="selected">The selected shipping model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of shipping methods
        /// </returns>
        Task<(IList<SelectListItem>, int index)> PrepareShippingMethodsForShipmentAsync(string shippingMethodName = null, string friendlyName = null, int countryId = 0);

        /// <summary>
        /// Prepare available shipping methods model as a selectable list
        /// </summary>
        /// <param name="selected">The selected shipping model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the selectable list of shipping methods
        /// </returns>
        Task<IList<SelectListItem>> PrepareAvailableCarriersModelAsync(bool addDefaultItem, int selected = 0);

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
        Task<IList<ShippingManagerByWeightByTotal>> GetAllRatesAsync(int vendorId = 0, int filterByCountryId = 0);

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
        Task<IPagedList<ShippingManagerByWeightByTotal>> GetAllRatesPagedAsync(int vendorId = 0, int filterByCountryId = 0, int pageIndex = 0, int pageSize = int.MaxValue);

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
        Task<IList<ShippingManagerByWeightByTotal>> GetRecordsAsync(int shippingMethodId, int storeId, int vendorId,
            int warehouseId, int carrierId, int countryId, int stateProvinceId, string zip);

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
        Task<IList<ShippingManagerByWeightByTotal>> GetRecordsAsync(int shippingMethodId = 0, int storeId = 0, int vendorId = 0,
            int warehouseId = 0, int carrierId = 0, int countryId = 0, int stateProvinceId = 0, string zip = null, decimal? weight = null, decimal? orderSubtotal = null);

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
        ShippingManagerByWeightByTotal FindMethodAsync(ShippingManagerByWeightByTotal smbwbt,
            int storeId, int vendorId, int warehouseId, int carrierId,
            int countryId, int stateProvinceId, string zip, decimal? weight, decimal? orderSubtotal);

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
        Task<IList<ShippingManagerByWeightByTotal>> FindMethodsAsync(int storeId, int vendorId, int warehouseId, int carrierId,
            int countryId, int stateProvinceId, string zip, decimal? weight, decimal? orderSubtotal, int shippingMethodId = 0);

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
        Task<IPagedList<ShippingManagerByWeightByTotal>> FindRecordsAsync(int shippingMethodId, int storeId, int vendorId, int warehouseId, int carrierId,
            int countryId, int stateProvinceId, string zip, decimal? weight, decimal? orderSubtotal, bool active, int pageIndex, int pageSize);

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
        /// <param name="active">Active</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the paged list of shipping by weight records
        /// </returns> 
        Task<ShippingManagerByWeightByTotal> FindRecordsAsync(int shippingMethodId, int storeId, int vendorId, int warehouseId,
            int countryId, int stateProvinceId, string zip, decimal weight, decimal orderSubtotal, bool active);

        /// <summary>
        /// Get list of shipping methods for configuration
        /// </summary>
        /// <param name="active">Active</param>
        /// <param name="vendorId">Vendor identifier</param>
        /// <returns>Shipping methods by display order</returns>
        Task<List<ShippingMethod>> GetShippingMethodListForShipment(bool active = true, int countryId = 0, int vendorId = 0);

        /// <summary>
        /// Get a shipping method by passed parameters
        /// </summary>
        /// <param name="friendlyName">Friendly Name identifier</param>
        /// <param name="vendorId">Vendor identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipping method
        /// </returns> 
        Task<(ShippingMethod, ShippingManagerByWeightByTotal)> GetShippingMethodFromFriendlyNameAsync(string friendlyName, int vendorId = 0);

        /// <summary>
        /// Get all shiping methods for export
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of shipping methods
        /// </returns> 
        Task<List<ExportRatesModel>> GetRatesForExportAsync();

        /// <summary>
        /// Get a shipping by weight record by identifier
        /// </summary>
        /// <param name="shippingByWeightRecordId">Record identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the  shipping by weight record
        /// </returns> 
        Task<ShippingManagerByWeightByTotal> GetByIdAsync(int shippingByWeightRecordId);

        /// <summary>
        /// Insert the shipping by weight record
        /// </summary>
        /// <param name="shippingByWeightRecord">Shipping by weight record</param>
        /// <returns>task that represents the asynchronous operation</returns>
        Task InsertShippingByWeightRecordAsync(ShippingManagerByWeightByTotal shippingByWeightRecord);

        /// <summary>
        /// Update the shipping by weight record
        /// </summary>
        /// <param name="shippingByWeightRecord">Shipping by weight record</param>
        /// <returns>task that represents the asynchronous operation</returns> 
        Task UpdateShippingByWeightRecordAsync(ShippingManagerByWeightByTotal shippingByWeightRecord);

        /// <summary>
        /// Delete the shipping by weight record
        /// </summary>
        /// <param name="shippingByWeightRecord">Shipping by weight record</param>
        /// <returns>task that represents the asynchronous operation</returns>
        Task DeleteShippingByWeightRecordAsync(ShippingManagerByWeightByTotal shippingByWeightRecord);

        #endregion

        #region Utility

        /// <summary>
        /// Prepare plugins warning model
        /// </summary>
        /// <param name="models">List of system warning models</param>
        /// <returns>task that represents the asynchronous operation</returns>
        public Task PreparePluginsWarningModelAsync(IList<SystemWarningModel> models);

        /// <summary>
        /// Gets a shopping cart package price 
        /// </summary>
        /// <param name="sci">Shopping Cart Item</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the package price
        /// </returns>
        public Task<decimal> GetPackagePrice(ShoppingCartItem sci);

        /// <summary>
        /// Format the shipping method option
        /// </summary>
        /// <param name="shippingOption">Shipping Option</param>
        /// <param name="smco">Shipping Manager Calculation Option</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the formated shipping option
        /// </returns>
        public Task<ShippingOption> FormatOptionDetails(ShippingOption shippingOption, ShippingManagerCalculationOption smco);

        /// <summary>
        /// Get default Billing address
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the address
        /// </returns> 
        public Task<Address> GetDefaultBillingAddressAsync(Customer customer);

        /// <summary>
        /// Get default country code for a customer
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the country code string
        /// </returns>
        public Task<string> GetDefaultCountryCodeAsync(Customer customer);

        /// <summary>
        /// Get country from Code
        /// </summary>
        /// <param name="countryCode">Country code string</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the country identifier
        /// </returns>
        public Task<int> GetCountryIdFromCodeAsync(string countryCode);

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
        public Task<Address> CreateAddressAsync(string firstName, string lastName, string email,
            string company, int countryId, int stateProvinceId, string county, string city, string address1, string address2,
            string zipPostalCode, string phoneNumber, string faxNumber);

        #endregion

        #region ShippingServices

        /// <summary>
        /// Gets an URL for a page to show tracking info (third party tracking page).
        /// </summary>
        /// <param name="trackingNumber">The tracking number to track.</param>
        /// <returns>URL of a tracking page.</returns>
        public string GetSendCloudTrackingUrl(string trackingNumber);
        /// <summary>
        /// Gets an URL for a page to show tracking info (third party tracking page).
        /// </summary>
        /// <param name="trackingNumber">The tracking number to track.</param>
        /// <returns>URL of a tracking page.</returns>
        public string GetUrl(string trackingNumber);

        /// <summary>
        /// Gets all events for a tracking number
        /// </summary>
        /// <param name="trackingNumber">The tracking number to track</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipment events
        /// </returns> 
        public Task<IEnumerable<ShipmentStatusEvent>> GetShipmentEventsAsync(string trackingNumber);

        /// <summary>
        /// Gets a shipments for a tracking number
        /// </summary>
        /// <param name="trackingNumber">The tracking number to track</param>
        /// <returns>the shipment for thye tracking number</returns>
        public Shipment GetShipmentForTrackingNumber(string trackingNumber);

        #endregion

        #region PackagingOptions

        /// <summary>
        /// Gets the default packaging option if service is enabled
        /// </summary>
        /// <param name="trackingNumber">The tracking number to track.</param>
        /// <returns>URL of a tracking page.</returns>
        public Task<PackagingOption> GetDefaultPackagingOption();


        /// <summary>
        /// Inserts a shipment item packaging option
        /// </summary>
        /// <param name="shipment">The shipment</param>
        /// <param name="packagingOption">The packaging option </param>
        /// <returns>URL of a tracking page.</returns>
        public Task<ShipmentDetails> InsertShipmentDetails(Shipment shipment, PackagingOption packagingOption);

        #endregion

    }
}
