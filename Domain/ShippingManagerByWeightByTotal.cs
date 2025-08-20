using Nop.Core;

namespace Nop.Plugin.Shipping.Manager.Domain
{
    /// <summary>
    /// Represents a shipping by weight record
    /// </summary>
    public partial class ShippingManagerByWeightByTotal : BaseEntity
    {
        /// <summary>
        /// Gets or sets the store identifier
        /// </summary>
        public int StoreId { get; set; }

        /// <summary>
        /// Gets or sets the warehouse identifier
        /// </summary>
        public int VendorId { get; set; }

        /// <summary>
        /// Gets or sets the warehouse identifier
        /// </summary>
        public int WarehouseId { get; set; }

        /// <summary>
        /// Gets or sets the carrier identifier
        /// </summary>
        public int CarrierId { get; set; }

        /// <summary>
        /// Gets or sets the country identifier
        /// </summary>
        public int CountryId { get; set; }

        /// <summary>
        /// Gets or sets the state/province identifier
        /// </summary>
        public int StateProvinceId { get; set; }

        /// <summary>
        /// Gets or sets the zip
        /// </summary>
        public string Zip { get; set; }

        /// <summary>
        /// Gets or sets the shipping method identifier
        /// </summary>
        public int ShippingMethodId { get; set; }

        /// <summary>
        /// Gets or sets the "Weight from" value
        /// </summary>
        public decimal WeightFrom { get; set; }

        /// <summary>
        /// Gets or sets the "Weight to" value
        /// </summary>
        public decimal WeightTo { get; set; }

        /// <summary>
        /// Gets or sets the flag to deturmine if dimensions aer used to calculate the cubic weight 
        /// </summary>
        public bool CalculateCubicWeight { get; set; }

        /// <summary>
        /// Gets or sets the cubic weight factor for
        /// Express freight: 250, General Freight: 333, International Courier: 200, Air freight: 164 Sea Freight: 1000
        /// </summary>
        public decimal CubicWeightFactor { get; set; }

        /// <summary>
        /// Gets or sets the "Order subtotal from" value
        /// </summary>
        public decimal OrderSubtotalFrom { get; set; }

        /// <summary>
        /// Gets or sets the "Order subtotal to" value
        /// </summary>
        public decimal OrderSubtotalTo { get; set; }

        /// <summary>
        /// Gets or sets the additional fixed cost
        /// </summary>
        public decimal AdditionalFixedCost { get; set; }

        /// <summary>
        /// Gets or sets the shipping charge percentage
        /// </summary>
        public decimal PercentageRateOfSubtotal { get; set; }

        /// <summary>
        /// Gets or sets the shipping charge amount (per weight unit)
        /// </summary>
        public decimal RatePerWeightUnit { get; set; }

        /// <summary>
        /// Gets or sets the lower weight limit
        /// </summary>
        public decimal LowerWeightLimit { get; set; }

        /// <summary>
        /// Gets or sets the cut of time 
        /// </summary>
        public int CutOffTimeId { get; set; }

        /// <summary>
        /// Gets or sets the rate friendly name 
        /// </summary>
        public string FriendlyName { get; set; }

        /// <summary>
        /// Gets or sets the rate friendly name 
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the transit days
        /// </summary>
        public int? TransitDays { get; set; }

        /// <summary>
        /// Gets or sets the sender address id
        /// </summary>
        public int SendFromAddressId { get; set; }

        /// <summary>
        /// Gets or sets the rate as active
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public int DisplayOrder { get; set; }
    }
}
