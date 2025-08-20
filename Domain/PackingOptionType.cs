namespace Nop.Plugin.Shipping.Manager.Domain
{
    /// <summary>
    /// Represents a product type
    /// </summary>
    public enum PackagingOptionType
    {
        /// <summary>
        /// Simple
        /// </summary>
        Box = 10,

        /// <summary>
        /// Grouped (product with variants)
        /// </summary>
        Satchel = 20,

        /// <summary>
        /// Grouped (product with variants)
        /// </summary>
        Pallet = 30,

    }
}
