using System.Threading.Tasks;

using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;

namespace Nop.Plugin.Shipping.Manager.Services
{
    /// <summary>
    /// Represents service shipping by weight service
    /// </summary>
    public partial interface IShippingAddressService
    {

        #region Methods

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

    }
}
