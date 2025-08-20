using System.Threading.Tasks;

using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Data;
using Nop.Services.Directory;

using Nop.Plugin.Apollo.Integrator;

namespace Nop.Plugin.Shipping.Manager.Services
{
    /// <summary>
    /// Represents service shipping by weight service implementation
    /// </summary>
    public partial class ShippingAddressService : IShippingAddressService
    {

        #region Fields

        protected readonly IWorkContext _workContext;
        protected readonly ICountryService _countryService;
        protected readonly IRepository<Address> _addressRepository;

        SystemHelper _systemHelper = new SystemHelper();

        #endregion

        #region Ctor

        public ShippingAddressService(IWorkContext workContext,
            ICountryService countryService,
            IRepository<Address> addressRepository)
        {
            _workContext = workContext;
            _countryService = countryService;
            _addressRepository = addressRepository;
        }

        #endregion

        #region Methods

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

    }
}
