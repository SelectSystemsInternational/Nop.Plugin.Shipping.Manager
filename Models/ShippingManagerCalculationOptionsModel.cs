using System.Collections.Generic;
using Nop.Services.Shipping;

using Nop.Plugin.Shipping.Manager.Domain;

namespace Nop.Plugin.Shipping.Manager.Models
{
    /// <summary>
    /// Represents a shipping manage rate calculate request and options
    /// </summary>
    public partial class ShippingManagerCalculationOptions
    {

        public ShippingManagerCalculationOptions()
        {
            ShippingRateComputationMethodSystemName = string.Empty;
            Smcro = new List<ShippingManagerCalculationRequestOption>();
        }

        /// <summary>
        /// Gets or sets the shipping rate computation method identifier or the pickup point provider identifier (if PickupInStore is true)
        /// </summary>
        public string ShippingRateComputationMethodSystemName { get; set; }

        /// <summary>
        /// Gets or sets the list of Shipping options
        /// </summary>
        public List<ShippingManagerCalculationRequestOption> Smcro { get; set; }

        /// <summary>
        /// Gets or sets the Order subtotal value
        /// </summary>
        public decimal SubTotal { get; set; }

        /// <summary>
        /// Gets or sets the Simple list flag
        /// </summary>
        public bool SimpleList { get; set; }

    }

    /// <summary>
    /// Represents a shipping manage rate calculate request and options
    /// </summary>
    public partial class ShippingManagerCalculationRequestOption
    {
        public ShippingManagerCalculationRequestOption()
        {
            Smco = new List<ShippingManagerCalculationOption>();
            Gsor = new GetShippingOptionResponse();
        }

        /// <summary>
        /// Gets or sets the shipping option product Id
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Gets or sets the shipping option warehouse Id
        /// </summary>
        public int WarehouseId { get; set; }

        /// <summary>
        /// Gets or sets the serialized CustomValues 
        /// </summary>
        public List<ShippingManagerCalculationOption> Smco { get; set; }

        /// <summary>
        /// Represents a response of getting shipping rate options
        /// </summary>
        public GetShippingOptionResponse Gsor { get; set; }

        /// <summary>
        /// Gets or sets the Weight value
        /// </summary>
        public decimal Weight { get; set; }

    }

    /// <summary>
    /// Represents a shipping manage rate calculate request and options
    /// </summary>
    public partial class ShippingManagerCalculationOption
    {
        public ShippingManagerCalculationOption()
        {
            Sor = new List<GetShippingOptionRequest>();
            Smbwtr = new ShippingManagerByWeightByTotal();
            CustomValuesXml = null;
        }

        /// <summary>
        /// Gets or sets the quantity
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets the additional fixed cost
        /// </summary>
        public ShippingManagerByWeightByTotal Smbwtr { get; set; }

        /// <summary>
        /// Gets or sets the list of shipping option requests
        /// </summary>
        public List<GetShippingOptionRequest> Sor { get; set; }

        /// <summary>
        /// Gets or sets the serialized CustomValues 
        /// </summary>
        public string CustomValuesXml { get; set; }

    }
}
