namespace Nop.Plugin.Shipping.Manager.Domain
{
    /// <summary>
    /// Represents a method of inventory management
    /// </summary>
    public enum ManageMaterialInventoryMethod
    {
        /// <summary>
        /// Don't track inventory for material
        /// </summary>
        DontManageMaterials = 0,

        /// <summary>
        /// Track inventory for material
        /// </summary>
        ManageMaterials = 1,

        /// <summary>
        /// Track inventory for material parent
        /// </summary>
        ParentMaterial = 2,
    }
}
