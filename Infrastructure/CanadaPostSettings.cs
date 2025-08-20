using System.Collections.Generic;
using Nop.Core.Configuration;

namespace Nop.Plugin.Shipping.CanadaPost
{
    /// <summary>
    /// Represents settings of Canada Post shipping plugin
    /// </summary>
    public class CanadaPostSettings : ISettings
    {
        public CanadaPostSettings()
        {
            SelectedServicesCodes = new List<string>();
        }

        /// <summary>
        /// Gets or sets the License Public Key
        /// </summary>
        public string PublicKey { get; set; }

        /// <summary>
        /// Gets or sets the License Private Key
        /// </summary>
        public string PrivateKey { get; set; }

        /// <summary>
        /// Gets or sets customer number
        /// </summary>
        public string CustomerNumber { get; set; }

        /// <summary>
        /// Gets or sets contract identifier
        /// </summary>
        public string ContractId { get; set; }

        /// <summary>
        /// Gets or sets the API key
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Gets or sets value indicating whether to use sandbox (testing environment)
        /// </summary>
        public bool UseSandbox { get; set; }

        /// <summary>
        /// Gets or sets a list of codes of selected shipping services
        /// </summary>
        public List<string> SelectedServicesCodes { get; set; }

        /// <summary>
        /// Gets or sets an amount of the additional handling charge
        /// </summary>
        public decimal AdditionalHandlingCharge { get; set; }

        /// <summary>
        /// Gets or sets an amount of the additional delivery days
        /// </summary>
        public int AdditionalShippingDays { get; set; }

        /// <summary>
        /// Gets or sets an amount of the additional delivery days
        /// </summary>
        public string DeliveryDaysMessage { get; set; }

        /// <summary>
        /// Gets or sets an amount of the additional delivery days
        /// </summary>
        public string DeliveryParcelsMessage { get; set; }

        /// <summary>
        /// Gets or sets a defulat number of delivery days to display to the customer
        /// </summary>
        public int DefaultDeliveryDays { get; set; }

        /// <summary>
        /// Gets or sets a default header on the option name
        /// </summary>
        public string DefaultHeader { get; set; }

        /// <summary>
        /// Gets or sets the TestMode
        /// </summary>
        public bool TestMode { get; set; }
    }
}