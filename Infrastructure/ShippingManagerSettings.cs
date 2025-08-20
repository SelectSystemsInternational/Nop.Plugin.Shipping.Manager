using Nop.Core.Configuration;

namespace Nop.Plugin.Shipping.Manager.Settings
{
    /// <summary>
    /// Represents settings of the "Shipping Manager" shipping plugin
    /// </summary>
    public class ShippingManagerSettings : ISettings
    {
        public ShippingManagerSettings()
        {
            PublicKey = string.Empty;
            PrivateKey = string.Empty;
            AttacheFileLocation = string.Empty;
            ApiServices = string.Empty;
            AvailableApiServices = string.Empty;
            PackagingOptions = string.Empty;
        }

        /// <summary>
        /// Gets or sets a value for the Public License Key
        /// </summary>
        public string PublicKey { get; set; }

        /// <summary>
        /// Gets or sets a value for the Private License Key
        /// </summary>
        public string PrivateKey { get; set; }

        /// <summary>
        /// Gets or sets a value for pluign enabled
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to limit shipping methods to configured ones
        /// </summary>
        public bool LimitMethodsToCreated { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the "shipping calculation by weight and by total" method is selected
        /// </summary>
        public bool ShippingByWeightByTotalEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether multiwarehouse is used for create shipping requests
        /// </summary>
        public bool CreateShippingOptionRequests { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the "shipping calculation by weight and by total" method is selected
        /// </summary>
        public bool InternationalOperationsEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the default country 
        /// </summary>
        public int DefaultCountryId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the tables should be deleted on plugin uninstall
        /// </summary>
        public bool DeleteTablesonUninstall { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the configuration should be deleted on plugin uninstall
        /// </summary>
        public bool DeleteConfigurationDataonUninstall { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the direcory location for attached files
        /// </summary>
        public string AttacheFileLocation { get; set; }

        /// <summary>
        /// Gets or sets Ignore Services
        /// </summary>
        public string ApiServices { get; set; }

        /// <summary>
        /// Gets or sets Ignore Services
        /// </summary>
        public string AvailableApiServices { get; set; }

        /// <summary>
        /// Gets or sets the TestMode
        /// </summary>
        public bool TestMode { get; set; }

        /// <summary>
        /// Gets or sets the TestMode
        /// </summary>
        public bool DisplayCutOffTime { get; set; }

        /// <summary>
        /// Gets or sets the List Order
        /// </summary>
        public bool OrderByDate { get; set; }

        /// <summary>
        /// Gets or sets the otion to encrypt serivce point posts
        /// </summary>
        public bool EncryptServicePointPost { get; set; }

        /// <summary>
        /// Gets or sets the Report Font Name
        /// </summary>
        public string FontFileName { get; set; }

        /// <summary>
        /// Gets or sets the shipping status based on option
        /// </summary>
        public bool SetAsShippedWhenAnnounced { get; set; }

        /// <summary>
        /// Gets or sets the typ of request list the system will use to process shipping option requests
        /// </summary>
        public ProcessingMode ProcessingMode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the packing system is used
        /// </summary>
        public bool UsePackagingSystem { get; set; }

        /// <summary>
        /// Gets or sets a value defineing the packaging optioons
        /// </summary>
        public string PackagingOptions { get; set; }

        /// <summary>
        /// Gets or sets the Order Manager function
        /// </summary>
        public bool ManifestShipments { get; set; }

        /// <summary>
        /// Gets or sets the List Order
        /// </summary>
        public bool UseWarehousesConfiguration { get; set; }

        /// <summary>
        /// Gets or sets the Order Manager function
        /// </summary>
        public bool OrderManagerOperations { get; set; }

        /// <summary>
        /// Gets or sets the shipping option display mode
        /// </summary>
        public ShippingOptionDisplay ShippingOptionDisplay { get; set; }

        /// <summary>
        /// Gets or sets the option for checkout operating mode
        /// </summary>
        public CheckoutOperationMode CheckoutOperationMode { get; set; }

        /// <summary>
        /// Gets or sets the option to display the manual operations buttons
        /// </summary>
        public bool DisplayManualOperations { get; set; }

        /// <summary>
        /// Gets or sets the logo size
        /// </summary>
        public float LogoSize { get; set; }

    }

    public class ApiSettings : ISettings
    {
        public ApiSettings()
        {
            ApiKey = string.Empty;
            ApiSecret = string.Empty;
            HostURL = string.Empty;
            AuthenticationURL = string.Empty;
            CustomerNumber = string.Empty;
            MoBoCN = string.Empty;
            ContractId = string.Empty;
        }

        /// <summary>
        /// Gets or sets the ApiKey 
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the ApiSecret
        /// </summary>
        public string ApiSecret { get; set; }

        /// <summary>
        /// Gets or sets the Username 
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the Password
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the AuthenticationURL
        /// </summary>
        public string AuthenticationURL { get; set; }

        /// <summary>
        /// Gets or sets the HostURL
        /// </summary>
        public string HostURL { get; set; }

        /// <summary>
        /// Gets or sets the CustomerId
        /// </summary>
        public string CustomerNumber { get; set; }

        /// <summary>
        /// Gets or sets the Mailed on Behalf Of Customer Number
        /// </summary>
        public string MoBoCN { get; set; }

        /// <summary>
        /// Gets or sets the Account
        /// </summary>
        public string ContractId { get; set; }

        /// <summary>
        /// Gets or sets the Shipment Options
        /// </summary>
        public string ShipmentOptions { get; set; }

        /// <summary>
        /// Gets or sets the Test Mode
        /// </summary>
        public bool TestMode { get; set; }

    }

    /// <summary>
    /// Represents settings for Sendcloud Api
    /// </summary>
    public class SendcloudApiSettings : ApiSettings { }

    /// <summary>
    /// Represents settings for Aramex Api
    /// </summary>
    public class AramexApiSettings : ApiSettings { }

    /// <summary>
    /// Represents settings for Canada Post Api
    /// </summary>
    public class CanadaPostApiSettings : ApiSettings { }

}
