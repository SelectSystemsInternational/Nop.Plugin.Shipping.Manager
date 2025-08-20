using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

using Nop.Core;
using Nop.Core.Domain.Customers;

using Nop.Plugin.Apollo.Integrator.Domain;
using Nop.Plugin.Apollo.Integrator.Models.EntityGroup;

namespace Nop.Plugin.Apollo.Integrator.Services
{
    /// <summary>
    /// Generic entity service interface
    /// </summary>
    public partial interface IEntityGroupService
    {

        #region Customer Scope

        /// <summary>
        /// Sets the active scope
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the current customer scope
        /// </returns>
        public Task SetCustomerScopeConfiguration(int customerid);

        /// <summary>
        /// Clear the active scope
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the current customer scope
        /// </returns>
        public Task ClearGroupScopeConfiguration(int customerid);

        /// <summary>
        /// Gets th active customer list
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the current customer scope
        /// </returns>
        public Task<List<CustomerList>> GetActiveCustomerListAsync();

        /// <summary>
        /// Gets the active customer scope customer
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the current customer scope
        /// </returns>
        public Task<Customer> GetActiveCompanyScopeAsync();

        /// <summary>
        /// Gets th active customer scope
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the current customer scope
        /// </returns>
        public Task<int> GetActiveCustomerScopeAsync(bool useGroup = true);

        /// <summary>
        /// Gets th active customer group scope
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the current group customer scope
        /// </returns>
        public Task<int> GetActiveGroupCustomerScopeAsync();

        /// <summary>
        /// Gets the active company scope
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the customer Id for the Company
        /// </returns>
        public Task<int> GetActiveCompanyScopeAsync(BaseEntity entity);

        /// <summary>
        /// Gets the active company scope id
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the customer Id for the Company
        /// </returns>
        public Task<int> GetActiveCompanyScopeAsync(int customerId);

        /// <summary>
        /// Gets the active company scope for a customer
        /// </summary>
        /// <param name="customerId">customer identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains true if customerId is a Company
        /// </returns>
        public Task<bool> IsCompanyScopeAsync(int customerId);

        /// <summary>
        /// Deturmine if company scope 
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains true if scope is company 
        /// </returns>
        public Task<bool> IsCompanyScopeAsync(Customer customer);

        /// <summary>
        /// Gets list of Entity groups for all customers
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the select list of all customers
        /// </returns>
        //public Task<IList<SelectListItem>> PrepareAvailableCustomersListAsync();

        /// <summary>
        /// Gets list of Entity groups for all customers
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of entityGroups
        /// </returns>
        //public Task<List<EntityGroup>> GetCustomerEntityGroupsAsync();

        #endregion

        #region Vendor Scope

        /// <summary>
        /// Gets th active vendor scope
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the current vendor scope
        /// </returns>
        public Task<int> GetActiveVendorScopeAsync();

        /// <summary>
        /// Gets th active vendor group scope
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the current group vendor scope
        /// </returns>
        public Task<int> GetActiveGroupVendorScopeAsync();

        /// <summary>
        /// Gets the active vendor scope
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the current vendor scope
        /// </returns>
        public Task SetActiveVendorScopeAsync(int vendorId);

        /// <summary>
        /// Gets list of Entity groups for all vendors
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the select list of all vendors
        /// </returns>
        public Task<IList<SelectListItem>> PrepareAvailableVendorsListAsync();

        /// <summary>
        /// Gets list of Entity groups for all vendors
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of entityGroups
        /// </returns>
        public Task<List<EntityGroup>> GetVendorEntityGroupsAsync();

        #endregion

        #region Utility

        /// <summary>
        /// Customer is a company 
        /// </summary>
        /// <returns>
        /// True if customer is a company, faluse otherwise
        /// </returns>
        public bool CustomerIsCompany(Customer customer);

        /// <summary>
        /// Gets th active store scope
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the current store scope
        /// </returns>
        public Task<int> GetActiveStoreScopeConfiguration();

        /// <summary>
        /// Represents the entity group name
        /// </summary>
        /// <returns>A string that represents the entity group key</returns> 
        public string GetEntityGroupKey(BaseEntity entity);

        /// <summary>
        /// Represents the entity key name
        /// </summary>
        /// <returns>A string that represents the entity key group</returns> 
        public string GetEntityKeyGroup(BaseEntity entity);

        /// <summary>
        /// Get Company Name for Employee
        /// </summary>
        /// <returns>A string that represents the company name</returns> 
        public Task<string> GetCompanyNameForCustomerAsync(Customer customer);

        /// <summary>
        /// Get Employee Name
        /// </summary>
        /// <returns>A string that represents the employee name</returns> 
        public Task<string> GetEmployeeNameAsync(Customer customer);

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
        public Task<EntityGroup> GetEntityGroupByIdAsync(int entityGroupId);

        /// <summary>
        /// Inserts an entity group
        /// </summary>
        /// <param name="entityGroup">entityGroup</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public Task InsertEntityGroupAsync(EntityGroup entityGroup);

        /// <summary>
        /// Updates the entityGroup
        /// </summary>
        /// <param name="entityGroup">Attribute</param>
        /// <returns>A task that represents the asynchronous operation</returns>/// 
        public Task UpdateEntityGroupAsync(EntityGroup entityGroup);

        /// <summary>
        /// Deletes an entityGroup
        /// </summary>
        /// <param name="entityGroup">Attribute</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public Task DeleteEntityGroupAsync(EntityGroup entityGroup);

        /// <summary>
        /// Deletes an entityGroups
        /// </summary>
        /// <param name="entityGroups">Attributes</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public Task DeleteEntityGroupsAsync(IList<EntityGroup> entityGroups);

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
        public Task<IList<EntityGroup>> GetEntityGroupsForEntityAsync(int entityId, string keyGroup,
            int storeId = 0, int customerId = 0, int vendorId = 0, int warehouseId = 0);

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
        public List<EntityGroup> GetEntityGroup(string keyGroup, int entityId, string key, string value,
            int storeId = 0, int customerId = 0, int vendorId = 0, int warehouseId = 0);

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
        public List<EntityGroup> GetAllEntityGroups(string keyGroup, int entityId, string key, string value,
            int storeId = 0, int customerId = 0, int vendorId = 0, int warehouseId = 0);

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
        public IPagedList<EntityGroup> GetAllEntityGroups(int pageIndex, int pageSize, string keyGroup, int entityId, string key, string value,
            int storeId = 0, int customerId = 0, int vendorId = 0, int warehouseId = 0);

        /// <summary>
        /// Get all entity groups for vendor
        /// </summary>
        /// <param name="vendorId">Vendor identifier</param>
        /// <returns>List of entity groups</returns>
        public List<EntityGroup> GetAllEntityGroupsForVendor(int vendorId);

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
        public Task<EntityGroup> GetEntityGroupForMemberAsync(string groupKey, string keyGroup,
            int storeId = 0, int customerId = 0, int vendorId = 0, int warehouseId = 0);

        /// <summary>
        /// Get entity group members
        /// </summary>
        /// <param name="entityGroup">Entity group</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the entity group
        /// </returns>
        public Task<List<EntityGroup>> GetEntityGroupMembersAsync(EntityGroup entityGroup);

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
        //public Task SaveEntityGroupAsync<TPropType>(BaseEntity entity, string key, TPropType value,
        //    int storeId = 0, int customerId = 0, int vendorId = 0, int warehouseId = 0);

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
        public Task SaveEntityGroupMemberAsync<TPropType>(BaseEntity entity, string key, BaseEntity value,
            int storeId = 0, int customerId = 0, int vendorId = 0, int warehouseId = 0);

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
        //public Task<TPropType> GetEntityGroupAsync<TPropType>(BaseEntity entity, string key,
        //    int storeId = 0, int customerId = 0, int vendorId = 0, int warehouseId = 0, TPropType defaultValue = default(TPropType));

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
        public Task<EntityGroup> GetEntityGroupMemberAsync<TPropType>(BaseEntity entity, string key,
            int storeId = 0, int customerId = 0, int vendorId = 0, int warhouseId = 0);

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
        public Task<EntityGroup> CreateOrUpdateEntityGroupingAsync<TPropType>(BaseEntity entity, int customerId = 0, bool addMember = true, int warehouseId = 0);

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
        public Task<EntityGroup> CreateOrUpdateEntityGroupMemberAsync<TPropType>(BaseEntity entity, string key, EntityGroup value,
            int storeId = 0, int customerId = 0, int vendorId = 0, int warehouseId = 0);

        /// <summary>
        /// Delete entity group member
        /// </summary>
        /// <typeparam name="TPropType">Property type</typeparam>
        /// <param name="entity">Entity</param>
        /// <param name="key">Key</param>
        /// <param name="vendorId">Load a value specific for a certain vendor; pass 0 to load a value shared for all vendors</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public Task DeleteEntityGroupMemberAsync<TPropType>(BaseEntity entity, string key,
            int customerId = 0, int vendorId = 0, int warehouseId = 0);

        #endregion

        #region Entity Group Vendor

        /// <summary>
        /// Get vendor name for entity 
        /// </summary>
        /// <param name="entity">Entity</param>
        /// A task that represents the asynchronous operation
        /// The task result contains a string for the vendor group name
        /// </returns> 
        public Task<string> GetVendorNameForEntityGroupAsync(BaseEntity entity);

        #endregion

        #region Entity Group Customer

        /// <summary>
        /// Get vendor name for entity 
        /// </summary>
        /// <param name="entity">Entity</param>
        /// A task that represents the asynchronous operation
        /// The task result contains a string for the customer group name
        /// </returns> 
        public Task<string> GetCompanyNameForEntityGroupAsync(BaseEntity entity);

        #endregion

    }
}