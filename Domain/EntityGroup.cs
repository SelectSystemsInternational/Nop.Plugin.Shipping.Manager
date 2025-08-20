using System;

using Nop.Core;

namespace Nop.Plugin.Apollo.Integrator.Domain
{
    /// <summary>
    /// Represents a venue entity group record
    /// </summary>
    public partial class EntityGroup : BaseEntity
    {

        /// <summary>
        /// Gets or sets the key group i.e. Group, Vendor, Warehouse, Customer, Product, etc
        /// </summary>
        public string KeyGroup { get; set; }

        /// <summary>
        /// Gets or sets the entity identifier
        /// </summary>
        public int EntityId { get; set; }

        /// <summary>
        /// Gets or sets the key - Normally GroupName or Member
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the value - to link all the key items in the group 
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the store identifier
        /// </summary>
        public int StoreId { get; set; }

        /// <summary>
        /// Gets or sets the customer identifier
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// Gets or sets the vendor identifier
        /// </summary>
        public int VendorId { get; set; }

        /// <summary>
        /// Gets or sets the warehouse identifier
        /// </summary>
        public int WarehouseId { get; set; }

        /// <summary>
        /// Gets or sets the created or updated date
        /// </summary>
        public DateTime? CreatedOrUpdatedDateUTC { get; set; }
    }
}
