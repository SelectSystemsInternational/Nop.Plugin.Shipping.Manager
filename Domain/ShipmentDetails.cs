using System;

using Nop.Core;

namespace Nop.Plugin.Shipping.Manager.Domain
{
    /// <summary>
    /// Represents a shipment item packaging option
    /// </summary>
    public partial class ShipmentDetails : BaseEntity
    {
        /// <summary>
        /// Gets or sets the shipment identifier
        /// </summary>
        public int OrderShipmentId { get; set; }

        /// <summary>
        /// Gets or sets the order item identifier
        /// </summary>
        public int PackagingOptionItemId { get; set; }

        /// <summary>
        /// Gets or sets the shiping method identifier
        /// </summary>
        public int ShippingMethodId { get; set; }

        /// <summary>
        /// Gets or sets the third party shipment identifier
        /// </summary>
        public string ShipmentId { get; set; }

        /// <summary>
        /// Gets or sets the tracking pin
        /// </summary>
        public decimal Cost { get; set; }

        /// <summary>
        /// Gets or sets the group name
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// Gets or sets the lable url
        /// </summary>
        public string LabelUrl { get; set; }

        /// <summary>
        /// Gets or sets the manifest url
        /// </summary>
        public string ManifestUrl { get; set; }

        /// <summary>
        /// Gets or sets the serialized CustomValues
        /// </summary>
        public string CustomValuesXml { get; set; }

        /// <summary>
        /// Gets or sets the scheduled ship date
        /// </summary>
        public DateTime? ScheduledShipDate { get; set; }

    }
}