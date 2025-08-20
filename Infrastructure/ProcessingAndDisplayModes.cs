namespace Nop.Plugin.Shipping.Manager
{
    /// <summary>
    /// Represents shipping methof processing mode 
    /// </summary>
    public enum ProcessingMode
    {
        /// <summary>
        /// Products by Item
        /// </summary>
        Item = 1,

        /// <summary>
        /// Warehouse by volume 
        /// </summary>
        Volume = 2,

        /// <summary>
        /// Products by Method 
        /// </summary>
        Method = 3
    }

    /// <summary>
    /// Represents shipping option display mode
    /// </summary>
    public enum ShippingOptionDisplay
    {
        /// <summary>
        /// Dont list the methods when shipping delivery address unkown
        /// </summary>
        DisplayOnlyShippingOriginCountryMethods = 1,

        /// <summary>
        /// Ad country tofriendly name when shipping delivery address unkown
        /// </summary>
        AddCountryToDisplay = 2
    }

    /// <summary>
    /// Represents option for checkout operating mode 
    /// </summary>
    public enum CheckoutOperationMode
    {
        /// <summary>
        /// nopCommerce Default
        /// </summary>
        Default = 1,

        /// <summary>
        /// One page checkout 
        /// </summary>
        OnePageCheckout = 2,

        /// <summary>
        /// Real One page checkout 
        /// </summary>
        RealOnePageCheckout = 3

    }
}
