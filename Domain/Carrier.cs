using Nop.Core;

namespace Nop.Plugin.Shipping.Manager.Domain
{
    /// <summary>
    /// Represents a shipping carrier record
    /// </summary>
    public partial class Carrier : BaseEntity
    {

        /// <summary>
        /// Gets or sets the carrier name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the addmininstration comment
        /// </summary>
        public string AdminComment { get; set; }

        /// <summary>
        /// Gets or sets the address identifier of the warehouse
        /// </summary>
        public int AddressId { get; set; }

        /// <summary>
        /// Gets or sets the shipping rate computation method identifier or the pickup point provider identifier (if PickupInStore is true)
        /// </summary>
        public string ShippingRateComputationMethodSystemName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is active
        /// </summary>
        public bool Active { get; set; }

    }
}