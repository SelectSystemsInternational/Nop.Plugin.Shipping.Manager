using System;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Stores;
using Nop.Core.Domain.Catalog;

namespace Nop.Plugin.Shipping.Manager.Domain
{
    /// <summary>
    /// Represents a material
    /// </summary>
    public partial class PackagingOption : BaseEntity, ILocalizedEntity, IAclSupported, IStoreMappingSupported, ISoftDeletedEntity
    {
        /// <summary>
        /// Gets or sets a material group identifier
        /// </summary>
        public int MaterialGroupId { get; set; }

        /// <summary>
        /// Gets or sets the parent product identifier. It's used to identify associated products (only with "grouped" products)
        /// </summary>
        public int ParentGroupedMaterialId { get; set; }

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the admin comment
        /// </summary>
        public string AdminComment { get; set; }

        /// <summary>
        /// Gets or sets a vendor identifier
        /// </summary>
        public int VendorId { get; set; }

        /// <summary>
        /// Gets or sets the code
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the manufacturer part number
        /// </summary>
        public string ManufacturerPartNumber { get; set; }

        /// <summary>
        /// Gets or sets the material as labour (not material)
        /// </summary>
        public bool IsLabour { get; set; }

        /// <summary>
        /// Gets or sets the material type
        /// </summary>
        public int MaterialTypeId { get; set; }

        /// <summary>
        /// Gets or sets the picture identifier
        /// </summary>
        public int PictureId { get; set; }

        /// <summary>
        /// Gets or sets the supplier identifier
        /// </summary>
        public int SupplierId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating how to manage inventory
        /// </summary>
        public int ManageMaterialInventoryMethodId { get; set; }

        /// <summary>
        /// Gets or sets the stock quantity
        /// </summary>
        public decimal MaterialQuantity { get; set; }

        /// <summary>
        /// Gets or sets the used quantity from last confirmation
        /// </summary>
        public decimal UsedQuantity { get; set; }

        /// <summary>
        /// Gets or sets the minimum stock quantity
        /// </summary>
        public decimal MinMaterialQuantity { get; set; }

        /// <summary>
        /// Gets or sets the low stock activity identifier
        /// </summary>
        public int LowMaterialActivityId { get; set; }

        /// <summary>
        /// Gets or sets the unit
        /// </summary>
        public int MeasureUnitId { get; set; }

        /// <summary>
        /// Gets or sets a warehouse identifier
        /// </summary>
        public int WarehouseId { get; set; }

        /// <summary>
        /// Gets or sets the material cost
        /// </summary>
        public decimal MaterialCost { get; set; }

        /// <summary>
        /// Gets or sets the material cost
        /// </summary>
        public decimal MaterialPrice { get; set; }

        /// <summary>
        /// Gets or sets the weight
        /// </summary>
        public decimal Weight { get; set; }

        /// <summary>
        /// Gets or sets the length
        /// </summary>
        public decimal Length { get; set; }

        /// <summary>
        /// Gets or sets the width
        /// </summary>
        public decimal Width { get; set; }

        /// <summary>
        /// Gets or sets the height
        /// </summary>
        public decimal Height { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is subject to ACL
        /// </summary>
        public bool SubjectToAcl { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is limited/restricted to certain stores
        /// </summary>
        public bool LimitedToStores { get; set; }

        /// <summary>
        /// Gets or sets a display order.
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is active
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity has been deleted
        /// </summary>
        public bool Deleted { get; set; }

        /// <summary>
        /// Gets or sets the date and time of product creation
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the date and time of product update
        /// </summary>
        public DateTime UpdatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the value indicating how to manage inventory
        /// </summary>
        public ManageMaterialInventoryMethod ManageMaterialInventoryMethod
        {
            get => (ManageMaterialInventoryMethod)ManageMaterialInventoryMethodId;
            set => ManageMaterialInventoryMethodId = (int)value;
        }

        /// <summary>
        /// Gets or sets the low stock activity
        /// </summary>
        public LowMaterialActivity LowMaterialActivity
        {
            get => (LowMaterialActivity)LowMaterialActivityId;
            set => LowMaterialActivityId = (int)value;
        }

    }
}
