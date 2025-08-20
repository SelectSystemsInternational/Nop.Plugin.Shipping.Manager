using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

using Nop.Core;
using Nop.Core.Caching;
using Nop.Data;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Security;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Services.Vendors;

using Nop.Plugin.Apollo.Integrator.Domain;
using Nop.Plugin.Apollo.Integrator.Models.EntityGroup;
using Nop.Core.Domain.Vendors;

namespace Nop.Plugin.Apollo.Integrator.Services
{
    /// <summary>
    /// Generic entityGroup service
    /// </summary>
    public partial class EntityGroupService : IEntityGroupService
    {

        #region Fields

        protected readonly IStaticCacheManager _staticCacheManager; 
        protected readonly IRepository<EntityGroup> _entityGroupRepository;
        protected readonly IStoreService _storeService;
        protected readonly IStoreContext _storeContext;
        protected readonly IWorkContext _workContext;
        protected readonly IGenericAttributeService _genericAttributeService;
        protected readonly IPermissionService _permissionService;
        protected readonly ICustomerService _customerService;
        protected readonly IVendorService _vendorService;
        protected readonly IRepository<Customer> _customerRepository;

        SystemHelper _systemHelper = new SystemHelper();

        #endregion

        #region Ctor

        public EntityGroupService(IStaticCacheManager staticCacheManager,
            IRepository<EntityGroup> entityGroupRepository,
            IStoreService storeService,
            IStoreContext storeContext,
            IWorkContext workContext,
            IGenericAttributeService genericAttributeService,
            IPermissionService permissionService,
            ICustomerService customerService,
            IVendorService vendorService,
            IRepository<Customer> customerRepository)
        {
            _staticCacheManager = staticCacheManager;
            _entityGroupRepository = entityGroupRepository;
            _storeService = storeService;
            _storeContext = storeContext;
            _workContext = workContext;
            _genericAttributeService = genericAttributeService;
            _permissionService = permissionService;
            _customerService = customerService;
            _vendorService = vendorService;
            _customerRepository = customerRepository;
        }

        #endregion

        #region Customer Scope

        /// <summary>
        /// Sets the active scope
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the current customer scope
        /// </returns>
        public virtual async Task SetCustomerScopeConfiguration(int customerid)
        {
            var customer = await _customerService.GetCustomerByIdAsync((await _workContext.GetCurrentCustomerAsync()).Id);
            if (customer != null || customerid == 0)
            {
                await _genericAttributeService.SaveAttributeAsync(customer, ApolloIntegratorDefaults.AdminAreaCustomerScopeConfigurationAttribute, customerid);
                await _genericAttributeService.SaveAttributeAsync(customer, ApolloIntegratorDefaults.AdminAreaGroupCustomerScopeConfigurationAttribute, 0);
            }
        }

        /// <summary>
        /// Clear the active scope
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the current customer scope
        /// </returns>
        public virtual async Task ClearGroupScopeConfiguration(int customerid)
        {
            var customer = await _customerService.GetCustomerByIdAsync((await _workContext.GetCurrentCustomerAsync()).Id);
            if (customer != null || customerid == 0)
                await _genericAttributeService.SaveAttributeAsync(customer, ApolloIntegratorDefaults.AdminAreaGroupCustomerScopeConfigurationAttribute, 0);
        }

        /// <summary>
        /// Gets the active customer list
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the current customer scope
        /// </returns>
        public async Task<List<CustomerList>> GetActiveCustomerListAsync()
        {
            var customerList = new List<CustomerList>();
            var query = _customerRepository.Table;
            query = query.Where(c => !c.Deleted);
            query = query.Where(c => c.Active);

            var currentCustomer = await _workContext.GetCurrentCustomerAsync();

            //if (await _customerService.IsAdminAsync(currentCustomer))
            PermissionRecord acesss = _systemHelper.GetAccessPermission(SystemHelper.AccessMode.Supervisor);
            if (await _permissionService.AuthorizeAsync(acesss))
            {
                query = (from entity in query
                         join eg in _entityGroupRepository.Table on entity.Id equals eg.CustomerId
                         where eg.Key.Contains("CustomerGroup")
                         select entity);

                foreach (var customer in query)
                {
                    var companyName = customer.Company;
                    if (!string.IsNullOrEmpty(companyName))
                    {
                        var firstName = customer.FirstName;
                        var lastName = customer.LastName;

                        customerList.Add(new CustomerList
                        {
                            CustomerId = customer.Id,
                            EntityName = companyName + " : " + firstName + " " + lastName
                        });
                    }
                }
            }
            else
            {
                var companyName = currentCustomer.Company;
                if (!string.IsNullOrEmpty(companyName))
                {
                    var firstName = currentCustomer.FirstName;
                    var lastName = currentCustomer.LastName;

                    customerList.Add(new CustomerList
                    {
                        CustomerId = currentCustomer.Id,
                        EntityName = companyName + " : " + firstName + " " + lastName
                    });
                }
            }
            
            return customerList.OrderBy(c => c.EntityName).ToList();
        }

        /// <summary>
        /// Gets the active customer scope customer
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the current customer scope
        /// </returns>
        public async Task<Customer> GetActiveCompanyScopeAsync()
        {
            int customerId = await GetActiveCustomerScopeAsync();
            if (customerId != 0)
            {
                var customer = await _customerService.GetCustomerByIdAsync(customerId);
                if (customer != null)
                    return customer;
            }

            return null;    
        }

        /// <summary>
        /// Gets th active customer scope
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the current customer scope
        /// </returns>
        public async Task<int> GetActiveCustomerScopeAsync(bool useGroup = true)
        {
            int customerId = 0, groupCustomerId = 0;
            int storeId = await GetActiveStoreScopeConfiguration();

            var customer = await _workContext.GetCurrentCustomerAsync();
            if (customer != null)
            {
                if (await _customerService.IsAdminAsync(customer))
                    customerId = await _genericAttributeService.GetAttributeAsync<int>(customer, ApolloIntegratorDefaults.AdminAreaCustomerScopeConfigurationAttribute, storeId);
                else
                    customerId = customer.Id;

                PermissionRecord acesss = _systemHelper.GetAccessPermission(SystemHelper.AccessMode.Supervisor);
                if (await _permissionService.AuthorizeAsync(acesss))
                    groupCustomerId = await _genericAttributeService.GetAttributeAsync<int>(customer, ApolloIntegratorDefaults.AdminAreaGroupCustomerScopeConfigurationAttribute, storeId);

                if (groupCustomerId != 0 && useGroup)
                    return groupCustomerId;
            }

            return customerId;
        }

        /// <summary>
        /// Gets the active customer group scope
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the current group customer scope
        /// </returns>
        public async Task<int> GetActiveGroupCustomerScopeAsync()
        {
            int customerId = 0, groupCustomerId = 0;
            int storeId = await GetActiveStoreScopeConfiguration();

            var customer = await _workContext.GetCurrentCustomerAsync();

            PermissionRecord acesss = _systemHelper.GetAccessPermission(SystemHelper.AccessMode.Supervisor);

            if (await _permissionService.AuthorizeAsync(acesss))
            {
                customerId = await _genericAttributeService.GetAttributeAsync<int>(customer, ApolloIntegratorDefaults.AdminAreaGroupCustomerScopeConfigurationAttribute, storeId);
                groupCustomerId = await _genericAttributeService.GetAttributeAsync<int>(customer, ApolloIntegratorDefaults.AdminAreaCustomerScopeConfigurationAttribute, storeId);
            }

            if (groupCustomerId != 0)
                return groupCustomerId;

            return customerId;
        }

        /// <summary>
        /// Gets the active company scope
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the customer Id for the Company
        /// </returns>
        public virtual async Task<int> GetActiveCompanyScopeAsync(BaseEntity entity)
        {
            //Get storeId
            int storeId = await GetActiveStoreScopeConfiguration();

            string groupKey = GetEntityKeyGroup(entity);

            var keyGroup = "Member";

            var entityGroup = GetEntityGroup(groupKey, entity.Id, keyGroup, null, storeId).FirstOrDefault();
            if (entityGroup != null)
                return entityGroup.CustomerId;

            return entity.Id;
        }

        /// <summary>
        /// Gets the active company scope id
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the customer Id for the Company
        /// </returns>
        public virtual async Task<int> GetActiveCompanyScopeAsync(int customerId)
        {
            //Get storeId
            int storeId = await GetActiveStoreScopeConfiguration();

            var customer = await _customerService.GetCustomerByIdAsync(customerId);
            if (customer != null)
            {
                string groupKey = GetEntityKeyGroup(customer);

                var keyGroup = "Member";

                var entityGroup = GetEntityGroup(groupKey, customer.Id, keyGroup, null, storeId).FirstOrDefault();
                if (entityGroup != null)
                    return entityGroup.CustomerId;
            }

            return customerId;
        }

        /// <summary>
        /// Deturmine if company scope 
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains true if scope is company 
        /// </returns>
        public virtual async Task<bool> IsCompanyScopeAsync(int customerId)
        {
            //Get storeId
            int storeId = await GetActiveStoreScopeConfiguration();

            string groupKey = GetEntityKeyGroup(new Customer());

            var keyGroup = "Member";

            var entityGroup = GetEntityGroup(groupKey, customerId, keyGroup, null, storeId).FirstOrDefault();
            if (entityGroup == null)
                return true;

            return false;
        }

        /// <summary>
        /// Deturmine if company scope 
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains true if scope is company 
        /// </returns>
        public virtual async Task<bool> IsCompanyScopeAsync(Customer customer)
        {
            return await IsCompanyScopeAsync(customer.Id);
        }

        #endregion

        #region Vendor Scope

        /// <summary>
        /// Gets the active vendor scope
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the current vendor scope
        /// </returns>
        public async Task<int> GetActiveVendorScopeAsync()
        {
            int vendorId = 0, groupVendorId = 0;
            int storeId = await GetActiveStoreScopeConfiguration();

            var customer = await _workContext.GetCurrentCustomerAsync();

            PermissionRecord acesss = _systemHelper.GetAccessPermission(SystemHelper.AccessMode.Supervisor);

            var vendor = await _workContext.GetCurrentVendorAsync();
            if (vendor != null)
            {
                vendorId = vendor.Id;

                if (await _permissionService.AuthorizeAsync(acesss))
                {
                    vendorId = await _genericAttributeService.GetAttributeAsync<int>(customer, ApolloIntegratorDefaults.AdminAreaVendorScopeConfigurationAttribute, storeId);
                    groupVendorId = await _genericAttributeService.GetAttributeAsync<int>(customer, ApolloIntegratorDefaults.AdminAreaGroupVendorScopeConfigurationAttribute, storeId);
                }

                if (groupVendorId != 0)
                    return groupVendorId;
            }
            else
            {
                if (await _permissionService.AuthorizeAsync(acesss))
                {
                    vendorId = await _genericAttributeService.GetAttributeAsync<int>(customer, ApolloIntegratorDefaults.AdminAreaVendorScopeConfigurationAttribute, storeId);
                    groupVendorId = await _genericAttributeService.GetAttributeAsync<int>(customer, ApolloIntegratorDefaults.AdminAreaGroupVendorScopeConfigurationAttribute, storeId);
                }

                if (groupVendorId != 0)
                    return groupVendorId;
            }

            return vendorId;
        }

        /// <summary>
        /// Gets the active vendor group scope
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the current group vendor scope
        /// </returns>
        public async Task<int> GetActiveGroupVendorScopeAsync()
        {
            int vendorId = 0, groupVendorId = 0;
            int storeId = await GetActiveStoreScopeConfiguration();

            var customer = await _workContext.GetCurrentCustomerAsync();

            PermissionRecord acesss = _systemHelper.GetAccessPermission(SystemHelper.AccessMode.Supervisor);

            if (await _permissionService.AuthorizeAsync(acesss))
            {
                vendorId = await _genericAttributeService.GetAttributeAsync<int>(customer, ApolloIntegratorDefaults.AdminAreaGroupVendorScopeConfigurationAttribute, storeId);
                groupVendorId = await _genericAttributeService.GetAttributeAsync<int>(customer, ApolloIntegratorDefaults.AdminAreaVendorScopeConfigurationAttribute, storeId);
            }

            if (groupVendorId != 0)
                return groupVendorId;

            return vendorId;
        }

        /// <summary>
        /// Gets the active vendor scope
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the current vendor scope
        /// </returns>
        public async Task SetActiveVendorScopeAsync(int vendorId)
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            await _genericAttributeService.SaveAttributeAsync(customer, ApolloIntegratorDefaults.AdminAreaVendorScopeConfigurationAttribute, vendorId);
            await _genericAttributeService.SaveAttributeAsync(customer, ApolloIntegratorDefaults.AdminAreaGroupVendorScopeConfigurationAttribute, 0);
        }

        /// <summary>
        /// Gets list of Entity groups for all vendors
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the select list of all vendors
        /// </returns>
        public async Task<IList<SelectListItem>> PrepareAvailableVendorsListAsync()
        {
            var availableVendors = new List<SelectListItem>();
            int storeId = await GetActiveStoreScopeConfiguration();

            int vendorId = 0;
            var vendor = await _workContext.GetCurrentVendorAsync();
            if (vendor != null)
                vendorId = vendor.Id;

            if (await _workContext.GetCurrentVendorAsync() != null)
            {
                var vendorEntityGroups = await GetVendorEntityGroupsAsync();
                if (vendorEntityGroups.Any(v => v.VendorId == vendorId))
                {
                    var vendorEntityGroup = vendorEntityGroups.Where(v => v.VendorId == vendorId).FirstOrDefault();
                    var entitryGroupMembers = await GetEntityGroupMembersAsync(vendorEntityGroup);
                    foreach (var member in entitryGroupMembers)
                    {
                        vendor = await _vendorService.GetVendorByIdAsync(member.EntityId);
                        if (vendor != null)
                        {
                            availableVendors.Add(new SelectListItem { Text = vendor.Name, Value = vendor.Id.ToString() });
                        }
                    }
                }
                else
                {
                    availableVendors.Add(new SelectListItem { Text = vendor.Name, Value = vendor.Id.ToString() });
                }
            }
            else
            {
                vendorId = await GetActiveVendorScopeAsync();
                int selectedGroupVendorId = await GetActiveGroupVendorScopeAsync();
                if (vendorId == 0 || selectedGroupVendorId != 0)
                {
                    if (selectedGroupVendorId == 0)
                    {
                        availableVendors.Add(new SelectListItem { Text = "*", Value = "0" });

                        foreach (var v in await _vendorService.GetAllVendorsAsync())
                            availableVendors.Add(new SelectListItem { Text = v.Name, Value = v.Id.ToString(), Selected = (v.Id == vendorId) });
                    }
                    else
                    {
                        var entityGroup = GetAllEntityGroups("EntityGroup", 0, "VendorGroup", "0", storeId, selectedGroupVendorId).FirstOrDefault();
                        if (entityGroup != null)
                        {
                            var entitryGroups = await GetEntityGroupMembersAsync(entityGroup);
                            foreach (var eg in entitryGroups)
                            {
                                vendor = await _vendorService.GetVendorByIdAsync(eg.EntityId);
                                if (vendor != null)
                                {
                                    availableVendors.Add(new SelectListItem { Text = vendor.Name, Value = vendor.Id.ToString(), Selected = (vendor.Id == vendorId) });
                                }
                            }
                        }
                    }
                }
                else
                {
                    vendor = await _vendorService.GetVendorByIdAsync(vendorId);
                    availableVendors.Add(new SelectListItem { Text = vendor.Name, Value = vendor.Id.ToString(), Selected = (vendor.Id == vendorId) });
                }
            }

            return availableVendors;
        }

        /// <summary>
        /// Gets list of Entity groups for all vendors
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of entityGroups
        /// </returns>
        public virtual async Task<List<EntityGroup>> GetVendorEntityGroupsAsync()
        {
            var vendors = await _vendorService.GetAllVendorsAsync();

            var query = from v in vendors
                        join eg in _entityGroupRepository.Table on v.Id equals eg.VendorId
                        where (eg.Key == "VendorGroup")
                        orderby v.Name, eg.VendorId
                        select eg;

            var groupList = query.ToList();

            return groupList;
        }

        #endregion

        #region Utility

        /// <summary>
        /// Customer is a company 
        /// </summary>
        /// <returns>
        /// True if customer is a company, faluse otherwise
        /// </returns>
        public bool CustomerIsCompany(Customer customer)
        {
            var query = from entity in _entityGroupRepository.Table 
                        where (customer.Id != 0 &&
                            entity.CustomerId == customer.Id) &&
                            entity.Key.Contains("CustomerGroup")
                         select entity;

            if (query != null)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Gets the active store scope
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the current store scope
        /// </returns>
        public async Task<int> GetActiveStoreScopeConfiguration()
        {
            return await _storeContext.GetActiveStoreScopeConfigurationAsync();
        }

        /// <summary>
        /// Represents the entity group name
        /// </summary>
        /// <returns>A string that represents the entity group key</returns> 
        public virtual string GetEntityGroupKey(BaseEntity entity)
        {
            string groupType = string.Empty;

            string entityType = entity.GetType().Name;

            switch (entityType)
            {
                case "EntityGroup":
                    groupType = "GroupEntityGroup";
                    break;
                case "Account":
                    groupType = "AccountGroup";
                    break;
                case "Category":
                    groupType = "CategoryGroup";
                    break;
                case "Carrier":
                    groupType = "CarrierGroup";
                    break;
                case "Customer":
                    groupType = "CustomerGroup";
                    break;
                case "CutOffTime":
                    groupType = "CutOffTimeGroup";
                    break;
                case "Department":
                    groupType = "DepartmentGroup";
                    break;
                case "DeliveryDate":
                    groupType = "DeliveryDateGroup";
                    break;
                case "PaymentTerms":
                    groupType = "PaymentTerms";
                    break;
                case "PackagingOption":
                    groupType = "PackagingOption";
                    break;
                case "Product":
                    groupType = "ProductGroup";
                    break;
                case "Position":
                    groupType = "PositionGroup";
                    break;
                case "ProductAvailabilityRange":
                    groupType = "ProductAvailabilityRangeGroup";
                    break;
                case "ShippingMethod":
                    groupType = "ShippingMethodGroup";
                    break;
                case "Store":
                    groupType = "StoreGroup";
                    break;
                case "Vendor":
                    groupType = "VendorGroup";
                    break;
                case "Warehouse":
                    groupType = "WarehouseGroup";
                    break;
                case "DefaultValue":
                    groupType = "DefaultValue";
                    break;
            }

            return groupType;
        }

        /// <summary>
        /// Represents the entity key name
        /// </summary>
        /// <returns>A string that represents the entity key group</returns> 
        public virtual string GetEntityKeyGroup(BaseEntity entity)
        {
            string groupType = string.Empty;

            string entityType = entity.GetType().Name;

            switch (entityType)
            {
                case "EntityGroup":
                    groupType = "EntityGroup";
                    break;
                case "Account":
                    groupType = "Account";
                    break;
                case "Category":
                    groupType = "Category";
                    break;
                case "Carrier":
                    groupType = "Carrier";
                    break;
                case "Customer":
                    groupType = "Customer";
                    break;
                case "CutOffTime":
                    groupType = "CutOffTime";
                    break;
                case "DeliveryDate":
                    groupType = "DeliveryDate";
                    break;
                case "Department":
                    groupType = "Department";
                    break;
                case "PaymentTerms":
                    groupType = "PaymentTerms";
                    break;
                case "PackagingOption":
                    groupType = "PackagingOption";
                    break;
                case "Product":
                    groupType = "Product";
                    break;
                case "Position":
                    groupType = "Position";
                    break;
                case "ProductAvailabilityRange":
                    groupType = "ProductAvailabilityRange";
                    break;
                case "ShippingMethod":
                    groupType = "ShippingMethod";
                    break;
                case "Store":
                    groupType = "Store";
                    break;
                case "Vendor":
                    groupType = "Vendor";
                    break;
                case "Warehouse":
                    groupType = "Warehouse";
                    break;
                case "DefaultValue":
                    groupType = "DefaultValue";
                    break;
            }

            return groupType;
        }

        /// <summary>
        /// Is a Supervisor
        /// </summary>
        /// <returns>A string that represents the entity key group</returns> 
        public virtual async Task<bool> IsSupervisorAsync()
        {
            PermissionRecord acesss = _systemHelper.GetAccessPermission(SystemHelper.AccessMode.Supervisor);
            return await _permissionService.AuthorizeAsync(acesss);
        }

        /// <summary>
        /// Get Company Name for Employee
        /// </summary>
        /// <returns>A string that represents the company name</returns> 
        public virtual async Task<string> GetCompanyNameForCustomerAsync(Customer customer)
        {
            var companyCustomerId = await GetActiveCompanyScopeAsync(customer);
            var companyCustomer = await _customerService.GetCustomerByIdAsync(companyCustomerId);
            if (companyCustomer != null)
                return companyCustomer.Company;

            return "No Company";
        }

        /// <summary>
        /// Get Employee Name
        /// </summary>
        /// <returns>A string that represents the employee name</returns> 
        public virtual async Task<string> GetEmployeeNameAsync(Customer customer)
        {

            var firstName = customer.FirstName;
            var lastName = customer.LastName;
            if (firstName != string.Empty && lastName != string.Empty)
            {
                return firstName = firstName + " " + lastName;
            }
            else if (firstName != string.Empty && lastName == string.Empty)
            {
                return firstName;
            }
            else if (firstName == string.Empty && lastName != string.Empty)
            {
                return lastName;
            }
            else
                return "No Name";          
        }

        #endregion

        #region Entity Group

        /// <summary>
        /// Gets an entityGroup
        /// </summary>
        /// <param name="entityGroupId">Attribute identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the entityGroup
        /// </returns>
        public virtual async Task<EntityGroup> GetEntityGroupByIdAsync(int entityGroupId)
        {
            return await _entityGroupRepository.GetByIdAsync(entityGroupId, cache => default);
        }

        /// <summary>
        /// Inserts an entity group
        /// </summary>
        /// <param name="entityGroup">entityGroup</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public virtual async Task InsertEntityGroupAsync(EntityGroup entityGroup)
        {
            entityGroup.CreatedOrUpdatedDateUTC = DateTime.UtcNow;

            await _staticCacheManager.RemoveByPrefixAsync(ApolloIntegratorDefaults.EntityGroupPatternKey);

            await _entityGroupRepository.InsertAsync(entityGroup);
        }

        /// <summary>
        /// Updates the entityGroup
        /// </summary>
        /// <param name="entityGroup">Attribute</param>
        /// <returns>A task that represents the asynchronous operation</returns>/// 
        public virtual async Task UpdateEntityGroupAsync(EntityGroup entityGroup)
        {
            entityGroup.CreatedOrUpdatedDateUTC = DateTime.UtcNow;

            await _staticCacheManager.RemoveByPrefixAsync(ApolloIntegratorDefaults.EntityGroupPatternKey);

            await _entityGroupRepository.UpdateAsync(entityGroup);            
        }

        /// <summary>
        /// Deletes an entityGroup
        /// </summary>
        /// <param name="entityGroup">Attribute</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public virtual async Task DeleteEntityGroupAsync(EntityGroup entityGroup)
        {
            await _staticCacheManager.RemoveByPrefixAsync(ApolloIntegratorDefaults.EntityGroupPatternKey);

            await _entityGroupRepository.DeleteAsync(entityGroup);
        }

        /// <summary>
        /// Deletes an entityGroups
        /// </summary>
        /// <param name="entityGroups">Attributes</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public virtual async Task DeleteEntityGroupsAsync(IList<EntityGroup> entityGroups)
        {
            await _staticCacheManager.RemoveByPrefixAsync(ApolloIntegratorDefaults.EntityGroupPatternKey);

            await _entityGroupRepository.DeleteAsync(entityGroups);
        }

        /// <summary>
        /// Get entity groups for entity
        /// </summary>
        /// <param name="entityId">Entity identifier</param>
        /// <param name="keyGroup">Key group</param>
        /// <param name="storeId">Store identifier</param> 
        /// <param name="vendorId">Vendor identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the entityGroup list
        /// </returns>
        public virtual async Task<IList<EntityGroup>> GetEntityGroupsForEntityAsync(int entityId, string keyGroup, 
            int storeId = 0, int customerId = 0, int vendorId = 0, int warehouseId = 0)
        {

            var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(ApolloIntegratorDefaults.EntityGroupForEntityKey, entityId, keyGroup, storeId, customerId, vendorId, warehouseId);

            return await _entityGroupRepository.GetAllAsync(query =>
            {
                return from eg in _entityGroupRepository.Table
                       where
                           eg.EntityId == entityId &&
                           eg.KeyGroup == keyGroup &&
                           eg.StoreId == storeId &&
                           eg.CustomerId == customerId &&
                           eg.VendorId == vendorId &&
                           eg.WarehouseId == warehouseId
                       select eg;
            }, cache => cacheKey);

        }

        /// <summary>
        /// Get entity group
        /// </summary>
        /// <param name="keyGroup">Key group</param>
        /// <param name="entityId">Entity identifier</param>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param> 
        /// <param name="storeId">Store identifier</param> 
        /// <param name="vendorId">Vendor identifier</param>
        /// <param name="warehouseId">Warehouse identifier</param> 
        /// <returns>Get entityGroups</returns>
        public virtual List<EntityGroup> GetEntityGroup(string keyGroup, int entityId, string key, string value, 
            int storeId = 0, int customerId = 0, int vendorId = 0, int warehouseId = 0)
        {

            var query = from eg in _entityGroupRepository.Table
                        where 
                            (keyGroup == null || eg.KeyGroup == keyGroup) &&
                            (entityId == 0 || eg.EntityId == entityId) &&
                            (key == null || eg.Key == key) &&
                            (value == null || eg.Value == value) &&
                            (storeId == 0 || eg.StoreId == storeId) &&
                            (vendorId == 0 || eg.VendorId == vendorId) &&
                            (customerId == 0 || eg.CustomerId == customerId) &&
                            (warehouseId == 0 || eg.WarehouseId == warehouseId)                            
                            orderby eg.CustomerId, eg.VendorId
                        select eg;

            return query.ToList();
        }

        /// <summary>
        /// Get all entity groups for criteria
        /// </summary>
        /// <param name="keyGroup">Key group</param>
        /// <param name="entityId">Entity identifier</param>
        /// <param name="key">Key</param>/// 
        /// <param name="value">Value identifier</param>
        /// <param name="storeId">Load a value specific for a certain store; pass 0 to load a value shared for all stores</param> 
        /// <param name="vendorId">Load a value specific for a certain vendor; pass 0 to load a value shared for all vendors</param>
        /// <param name="warehouseId">Load a value specific for a certain warehouse; pass 0 to load a value shared for all warehouse</param>
        /// <returns>List of entity groups</returns>
        public virtual List<EntityGroup> GetAllEntityGroups(string keyGroup, int entityId, string key, string value, 
            int storeId = 0, int customerId = 0, int vendorId = 0, int warehouseId = 0)
        {

            var query = from eg in _entityGroupRepository.Table
                        where 
                            (keyGroup == null || eg.KeyGroup == keyGroup) &&
                            (entityId == 0 || eg.EntityId == entityId) &&
                            (key == null || eg.Key == key) &&
                            (value == null || eg.Value == value) &&
                            (storeId == 0 || eg.StoreId == storeId) &&
                            (customerId == 0 || eg.CustomerId == customerId) &&
                            (vendorId == 0 || eg.VendorId == vendorId) &&
                            (warehouseId == 0 || eg.WarehouseId == warehouseId)                            
                            orderby eg.VendorId, eg.Id
                        select eg;

            return query.ToList();
        }

        /// <summary>
        /// Get all entity groups for criteria as a paged list
        /// </summary>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>/// 
        /// <param name="keyGroup">Key group</param>
        /// <param name="entityId">Entity identifier</param>
        /// <param name="key">Key</param>/// 
        /// <param name="value">Value identifier</param>
        /// <param name="storeId">Load a value specific for a certain store; pass 0 to load a value shared for all stores</param>
        /// <param name="vendorId">Load a value specific for a certain vendor; pass 0 to load a value shared for all vendors</param>
        /// <param name="warehouseId">Load a value specific for a certain warehouse; pass 0 to load a value shared for all warehouse</param>
        /// <returns>Paged list of entity groups</returns>
        public virtual IPagedList<EntityGroup> GetAllEntityGroups(int pageIndex, int pageSize, string keyGroup, int entityId, string key, string value,
            int storeId = 0, int customerId = 0, int vendorId = 0, int warehouseId = 0)
        {
            var query = GetAllEntityGroups(keyGroup, entityId, key, value, storeId, customerId, vendorId, warehouseId);
            return new PagedList<EntityGroup>(query, pageIndex, pageSize);
        }

        /// <summary>
        /// Get all entity groups for vendor
        /// </summary>
        /// <param name="vendorId">Vendor identifier</param>
        /// <returns>List of entity groups</returns>
        public List<EntityGroup> GetAllEntityGroupsForVendor(int vendorId)
        {

            var query = from eg in _entityGroupRepository.Table
                        where eg.VendorId == vendorId
                        select eg;

            return query.ToList();
        }

    #endregion

        #region Entity Group Member

        /// <summary>
        /// Get entity group for a member
        /// </summary>
        /// <param name="entityId">Entity identifier</param>
        /// <param name="keyGroup">Key group</param>
        /// <param name="storeId">Store identifier</param> 
        /// <param name="customerId">Customer identifier</param> 
        /// <param name="vendorId">Vendor identifier</param>
        /// <param name="warehouseId">Warehouse identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the entity Group
        /// </returns>
        public virtual async Task<EntityGroup> GetEntityGroupForMemberAsync(string groupKey, string keyGroup, 
                int storeId = 0, int customerId = 0, int vendorId = 0, int warehouseId = 0)
        {
            var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(ApolloIntegratorDefaults.EntityGroupMemberKey, groupKey, keyGroup, 
                storeId, customerId, vendorId, warehouseId);

            var query = await _entityGroupRepository.GetAllAsync(query =>
            {
                return from eg in _entityGroupRepository.Table
                            where eg.Id == eg.EntityId &&
                                  eg.KeyGroup == keyGroup &&
                                  eg.Key == groupKey &&
                                  eg.Value == "0" &&
                                  eg.StoreId == storeId &&
                                  eg.CustomerId == customerId &&
                                  eg.VendorId == vendorId && 
                                  eg.WarehouseId == warehouseId                                  
                            select eg;
            }, cache => cacheKey);

            return query.FirstOrDefault();

        }

        /// <summary>
        /// Get entity group members
        /// </summary>
        /// <param name="entityGroup">Entity group</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the entity group
        /// </returns>
        public virtual async Task<List<EntityGroup>> GetEntityGroupMembersAsync(EntityGroup entityGroup)
        {
            var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(ApolloIntegratorDefaults.EntityGroupMemberKey, 
                entityGroup.EntityId, entityGroup.Key, entityGroup.KeyGroup, 
                entityGroup.StoreId, entityGroup.CustomerId, entityGroup.VendorId, entityGroup.WarehouseId);

            var query = await _entityGroupRepository.GetAllAsync(query =>
            {
                return from eg in _entityGroupRepository.Table
                       where eg.Key == "Member" &&
                                  eg.Value == entityGroup.EntityId.ToString() &&
                                  (entityGroup.CustomerId == 0 || eg.CustomerId == entityGroup.CustomerId) &&
                                  (entityGroup.VendorId == 0 || eg.VendorId == entityGroup.VendorId) &&
                                  (entityGroup.WarehouseId == 0 || eg.WarehouseId == entityGroup.WarehouseId) &&
                                  (entityGroup.StoreId == 0 || eg.StoreId == entityGroup.StoreId)
                       orderby eg.EntityId
                       select eg;

            }, cache => cacheKey);

            return query.ToList();
        }

        /// <summary>
        /// Save entityGroup value
        /// </summary>
        /// <typeparam name="TPropType">Property type</typeparam>
        /// <param name="entity">Entity</param>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="storeId">Store identifier</param> 
        /// <param name="customerId">Customer identifier</param>
        /// <param name="vendorId">Vendor identifier</param>
        /// <param name="warehouseId">Warehouse identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the entity group
        /// </returns> 
        //public virtual async Task SaveEntityGroupAsync<TPropType>(BaseEntity entity, string key, TPropType value, 
        //    int storeId = 0, int customerId = 0, int vendorId = 0, int warehouseId = 0)
        //{
        //    if (entity == null)
        //        throw new ArgumentNullException(nameof(entity));

        //    if (key == null)
        //        throw new ArgumentNullException(nameof(key));

        //    var keyGroup = entity.GetType().Name;

        //    var props = (await GetEntityGroupsForEntityAsync(entity.Id, keyGroup, storeId, customerId, vendorId, warehouseId))
        //        .Where(x => x.StoreId == storeId)
        //        .ToList();
        //    var prop = props.FirstOrDefault(eg =>
        //        eg.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase)); //should be culture invariant

        //    var valueStr = CommonHelper.To<string>(value);

        //    if (prop != null)
        //    {
        //        if (string.IsNullOrWhiteSpace(valueStr))
        //        {
        //            //delete
        //            await DeleteEntityGroupAsync(prop);
        //        }
        //        else
        //        {
        //            //update
        //            prop.Value = valueStr;
        //            await UpdateEntityGroupAsync(prop);
        //        }
        //    }
        //    else
        //    {
        //        if (string.IsNullOrWhiteSpace(valueStr)) 
        //            return;

        //        //insert
        //        prop = new EntityGroup
        //        {
        //            EntityId = entity.Id,
        //            Key = key,
        //            KeyGroup = keyGroup,
        //            Value = valueStr,
        //            StoreId = storeId,
        //        };

        //        await InsertEntityGroupAsync(prop);
        //    }
        //}

        /// <summary>
        /// Save entityGroup value
        /// </summary>
        /// <typeparam name="TPropType">Property type</typeparam>
        /// <param name="entity">Entity</param>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="storeId">Store identifier</param> 
        /// <param name="vendorId">Vendor identifier</param>
        /// <param name="warehouseId">Warehouse identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the entity group
        /// </returns>         
        public virtual async Task SaveEntityGroupMemberAsync<TPropType>(BaseEntity entity, string key, BaseEntity value, 
            int storeId = 0, int customerId = 0, int vendorId = 0, int warehouseId = 0)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (key == null)
                throw new ArgumentNullException(nameof(key));

            var keyGroup = entity.GetType().Name;

            var props = (await GetEntityGroupsForEntityAsync(entity.Id, keyGroup, storeId, customerId, vendorId, warehouseId))
                .Where(x => x.StoreId == storeId)
                .ToList();

            var prop = props.FirstOrDefault(eg =>
                eg.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase)); //should be culture invariant

            var valueStr = CommonHelper.To<string>(value.Id);

            if (prop != null)
            {
                if (string.IsNullOrWhiteSpace(valueStr))
                {
                    //delete
                    await DeleteEntityGroupAsync(prop);
                }
                else
                {
                    //update
                    prop.Value = valueStr;
                    await UpdateEntityGroupAsync(prop);
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(valueStr))
                    return;

                //insert
                prop = new EntityGroup
                {
                    EntityId = entity.Id,
                    Key = key,
                    KeyGroup = keyGroup,
                    Value = valueStr,
                    StoreId = storeId,
                    CustomerId = customerId,
                    VendorId = vendorId,
                    WarehouseId = warehouseId
                };

                await InsertEntityGroupAsync(prop);
            }
        }

        /// <summary>
        /// Get an entityGroup of an entity
        /// </summary>
        /// <typeparam name="TPropType">Property type</typeparam>
        /// <param name="entity">Entity</param>
        /// <param name="key">Key</param>
        /// <param name="storeId">Store identifier</param>
        /// <param name="customerId">Customer identifier</param> 
        /// <param name="vendorId">Vendor identifier</param>
        /// <param name="warehouseId">Warehouse identifier</param>
        /// <param name="defaultValue">Default value</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the entity group
        /// </returns>
        //public virtual async Task<TPropType> GetEntityGroupAsync<TPropType>(BaseEntity entity, string key, 
        //    int storeId = 0, int customerId = 0, int vendorId = 0, int warehouseId = 0, TPropType defaultValue = default(TPropType))
        //{
        //    if (entity == null)
        //        throw new ArgumentNullException(nameof(entity));

        //    var keyGroup = entity.GetType().Name;

        //    var props = await GetEntityGroupsForEntityAsync(entity.Id, keyGroup, storeId, customerId, vendorId, warehouseId);

        //    //little hack here (only for unit testing). we should write expect-return rules in unit tests for such cases
        //    if (props == null)
        //        return defaultValue;

        //    props = props.Where(x => x.StoreId == storeId).ToList();
        //    if (!props.Any())
        //        return defaultValue;

        //    var prop = props.FirstOrDefault(eg =>
        //        eg.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase)); //should be culture invariant

        //    if (prop == null || string.IsNullOrEmpty(prop.Value))
        //        return defaultValue;

        //    return CommonHelper.To<TPropType>(prop.Value);
        //}

        /// <summary>
        /// Get an entityGroup of an entity
        /// </summary>
        /// <typeparam name="TPropType">Property type</typeparam>
        /// <param name="entity">Entity</param>
        /// <param name="key">Key</param>
        /// <param name="storeId">Store identifier</param> 
        /// <param name="customerId">Customer identifier</param>  
        /// <param name="vendorId">Vendor identifier</param>
        /// <param name="warehouseId">Warehouse identifier</param>
        /// <param name="defaultValue">Default value</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the entityGroup list
        /// </returns>
        public virtual async Task<EntityGroup> GetEntityGroupMemberAsync<TPropType>(BaseEntity entity, string key, 
            int storeId = 0, int customerId = 0, int vendorId = 0, int warehouseId = 0)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var keyGroup = entity.GetType().Name;

            var props = await GetEntityGroupsForEntityAsync(entity.Id, keyGroup, storeId, customerId, vendorId, warehouseId);

            if (props != null)
            {
                props = props.Where(x => x.StoreId == storeId).ToList();
                if (props.Any())
                {
                    var prop = props.FirstOrDefault(eg =>
                    eg.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase)); //should be culture invariant

                    if (prop != null)
                        return prop;
                }
            }

            return null;
        }

        /// <summary>
        /// Create or Update and entity group
        /// </summary>
        /// <typeparam name="TPropType">Property type</typeparam>
        /// <param name="entity">Entity</param>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="storeId">Store identifier</param>
        /// <param name="customerId">Customer identifier</param> 
        /// <param name="vendorId">Vendor identifier</param>
        /// <param name="warehouseId">Warehouse identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the entityGroup
        /// </returns>
        public virtual async Task<EntityGroup> CreateOrUpdateEntityGroupingAsync<TPropType>(BaseEntity entity, int customerId = 0, bool addMember = true, int warehouseId = 0)
        {
            int storeId = await GetActiveStoreScopeConfiguration();

            //Get vendor
            int vendorId = await GetActiveVendorScopeAsync();

            //Get customer
            if (customerId == 0)
                customerId = await GetActiveCustomerScopeAsync();

            string groupKey = GetEntityGroupKey(entity);
            
            var keyGroup = "EntityGroup";

            EntityGroup entityGroup = await GetEntityGroupForMemberAsync(groupKey, keyGroup, storeId, customerId, vendorId, warehouseId);
            if (entityGroup == null)
            {
                entityGroup = new EntityGroup
                {
                    Key = groupKey,                 
                    KeyGroup = keyGroup,       
                    Value = "0",
                    StoreId = storeId,
                    CustomerId = customerId,
                    VendorId = vendorId,            
                    WarehouseId = warehouseId     
                };

                await InsertEntityGroupAsync(entityGroup);

                entityGroup.EntityId = entityGroup.Id;

                await UpdateEntityGroupAsync(entityGroup);

            }

            entityGroup = await GetEntityGroupForMemberAsync(groupKey, keyGroup, storeId, customerId, vendorId, warehouseId);

            if (addMember) // Dont create member for Company
            {
                if (entityGroup != null)
                {
                    keyGroup = "Member";
                    await CreateOrUpdateEntityGroupMemberAsync<TPropType>(entity, keyGroup, entityGroup, storeId, customerId, vendorId, warehouseId);
                }
            }

            return entityGroup;

        }

        /// <summary>
        /// Create or Update and entity group member
        /// </summary>
        /// <typeparam name="TPropType">Property type</typeparam>
        /// <param name="entity">Entity</param>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="storeId">Store identifier</param> 
        /// <param name="vendorId">Vendor identifier</param>
        /// <param name="warehouseId">Warehouse identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the entityGroup
        /// </returns>        
        public virtual async Task<EntityGroup> CreateOrUpdateEntityGroupMemberAsync<TPropType>(BaseEntity entity, string key, EntityGroup value, 
            int storeId = 0, int customerId = 0, int vendorId = 0, int warehouseId = 0)
        {

            var member = await GetEntityGroupMemberAsync<TPropType>(entity, key, storeId, customerId, vendorId, warehouseId);
            if (member == null)
                await SaveEntityGroupMemberAsync<TPropType>(entity, key, value, storeId, customerId, vendorId, warehouseId);

            member = await GetEntityGroupMemberAsync<TPropType>(entity, key, storeId, customerId, vendorId, warehouseId);

            return member;
        }

        /// <summary>
        /// Delete entity group member
        /// </summary>
        /// <typeparam name="TPropType">Property type</typeparam>
        /// <param name="entity">Entity</param>
        /// <param name="key">Key</param>
        /// <param name="vendorId">Load a value specific for a certain vendor; pass 0 to load a value shared for all vendors</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public virtual async Task DeleteEntityGroupMemberAsync<TPropType>(BaseEntity entity, string key, 
            int customerId = 0, int vendorId = 0, int warehouseId = 0)
        {
            int storeId = await GetActiveStoreScopeConfiguration();

            var member = await GetEntityGroupMemberAsync<TPropType>(entity, key, storeId, customerId, vendorId, warehouseId);
            if (member != null)
                await DeleteEntityGroupAsync(member);
        }

        #endregion

        #region Entity Group Vendor

        /// <summary>
        /// Get vendor name for entity 
        /// </summary>
        /// <param name="entity">Entity</param>
        /// A task that represents the asynchronous operation
        /// The task result contains a string for the vendor group name
        /// </returns> 
        public virtual async Task<string> GetVendorNameForEntityGroupAsync(BaseEntity entity)
        {
            string vendorName = string.Empty;

            var entityGroups = GetEntityGroup(GetEntityKeyGroup(entity), entity.Id, "Member", null);

            if (entityGroups.Count == 0)
                vendorName = "All Vendors";
            else
            {
                foreach (var e in entityGroups)
                {
                    if (e.VendorId == 0)
                    {
                        vendorName += "All Vendors";
                    }
                    else
                    {
                        var vendor = await _vendorService.GetVendorByIdAsync(e.VendorId);
                        if (vendor != null)
                        {
                            if (string.IsNullOrEmpty(vendorName))
                                vendorName += vendor.Name;
                            else
                                vendorName += ", " + vendor.Name;
                        }
                    }
                }
            }

            return vendorName;
        }

        #endregion

        #region Entity Group Customer

        /// <summary>
        /// Get company name for entity 
        /// </summary>
        /// <param name="entity">Entity</param>
        /// A task that represents the asynchronous operation
        /// The task result contains a string for the company group name
        /// </returns> 
        public virtual async Task<string> GetCompanyNameForEntityGroupAsync(BaseEntity entity)
        {
            string companyName = string.Empty;

            int companyId = await GetActiveCustomerScopeAsync(false);

            var entityGroups = GetEntityGroup(GetEntityKeyGroup(entity), entity.Id, "Member", null);

            if (entityGroups.Count == 0)
                companyName = "All Companies";
            else
            {
                foreach (var e in entityGroups)
                {
                    if (e.CustomerId == 0)
                    {
                        companyName += "All Companies";
                    }
                    else
                    {
                        var customer = await _customerService.GetCustomerByIdAsync(e.CustomerId);
                        if (customer != null)
                        {
                            if ((await IsSupervisorAsync()) || customer.Id == companyId)
                            {
                                var companyFullName = customer.Company;
                                if (string.IsNullOrEmpty(companyName))
                                    companyName += companyFullName;
                                else
                                    companyName += ", " + companyFullName;
                            }
                        }
                    }
                }
            }

            return companyName;
        }

        #endregion

    }
} 
