using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Shipping.Manager.Models
{
    public record ConfigurationModel : BaseNopModel
    {
        public ConfigurationModel()
        {
            PublicKey = string.Empty;
            PrivateKey = string.Empty;
            ApiServices = string.Empty;
            ApiServices_OverrideForStore = false;

            AvailableApiServices = new List<SelectListItem>();
            ApiServicesIds = new List<int>();
        }

        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Enabled")]
        public bool Enabled { get; set; }
        public bool Enabled_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.PublicKey")]
        public string PublicKey { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.PrivateKey")]
        public string PrivateKey { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.DeleteTablesonUninstall")]
        public bool DeleteTablesonUninstall { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.DeleteConfigurationDataonUninstall")]
        public bool DeleteConfigurationDataonUninstall { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.InternationalOperationsEnabled")]
        public bool InternationalOperationsEnabled { get; set; }

        public bool InternationalOperationsEnabled_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.OrderByDate")]
        public bool OrderByDate { get; set; }

        public bool OrderByDate_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.TestMode")]
        public bool TestMode { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.ApiServices")]
        public string ApiServices { get; set; }
        public bool ApiServices_OverrideForStore { get; set; }
        public IList<int> ApiServicesIds { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.ProcessingMode")]
        public ProcessingMode ProcessingModeId { get; set; }
        public bool ProcessingMode_OverrideForStore { get; set; }
        public SelectList ProcessingModeValues { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.UsePackagingSystem")]
        public bool UsePackagingSystem { get; set; }
        public bool UsePackagingSystem_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.PackagingOptions")]
        public string PackagingOptions { get; set; }
        public bool PackagingOptions_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.EncryptServicePointPost")]
        public bool EncryptServicePointPost { get; set; }
        public bool EncryptServicePointPost_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.SetAsShippedWhenAnnounced")]
        public bool SetAsShippedWhenAnnounced { get; set; }
        public bool SetAsShippedWhenAnnounced_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.DisplayManualOperations")]
        public bool DisplayManualOperations { get; set; }
        public bool DisplayManualOperations_OverrideForStore { get; set; }

        public ShippingOptionDisplay ShippingOptionDisplayId { get; set; }
        public bool ShippingOptionDisplay_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.ShippingOptionDisplay")]
        public SelectList ShippingOptionDisplayValues { get; set; }

        public CheckoutOperationMode CheckoutOperationModeId { get; set; }
        public bool CheckoutOperationMode_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.CheckoutOperationMode")]
        public SelectList CheckoutOperationModeValues { get; set; }

        public IList<SelectListItem> AvailableApiServices { get; set; }

        public ApiSettingsModel SendcloudApiSettings { get; set; }

        public ApiSettingsModel AramexApiSettings { get; set; }

        public ApiSettingsModel CanadaPostApiSettings { get; set; }

    }

    public record ApiSettingsModel : BaseNopModel, ISettingsModel
    {

        public ApiSettingsModel()
        {
            ApiKey = string.Empty;
            ApiSecret = string.Empty;
            Username = string.Empty;
            Password = string.Empty;
            HostURL = string.Empty;
            AuthenticationURL = string.Empty;
            WarehouseSetup = new List<WarehouseSetup>();
        }

        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Api.Fields.ApiKey")]
        public string ApiKey { get; set; }
        public bool ApiKey_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Api.Fields.ApiSecret")]
        public string ApiSecret { get; set; }
        public bool ApiSecret_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Api.Fields.Username")]
        public string Username { get; set; }
        public bool Username_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Api.Fields.Password")]
        public string Password { get; set; }
        public bool Password_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Api.Fields.AuthenticationURL")]
        public string AuthenticationURL { get; set; }
        public bool AuthenticationURL_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Api.Fields.HostURL")]
        public string HostURL { get; set; }
        public bool HostURL_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Api.Fields.CustomerNumber")]
        public string CustomerNumber { get; set; }
        public bool CustomerNumber_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Api.Fields.MoBoCN")]
        public string MoBoCN { get; set; }
        public bool MoBoCN_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Api.Fields.ContractId")]
        public string ContractId { get; set; }
        public bool ContractId_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Api.Fields.ShipmentOptions")]
        public string ShipmentOptions { get; set; }
        public bool ShipmentOptions_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Api.Fields.TestMode")]
        public bool TestMode { get; set; }

        public List<WarehouseSetup> WarehouseSetup { get; set; }

    }

    public class WarehouseSetup
    {

        public WarehouseSetup()
        {
            Name = string.Empty;
            ApiKey = string.Empty;
            ApiSecret = string.Empty;
            CustomerNumber = string.Empty;
            MoBoCN = string.Empty;
            ContractId = string.Empty;
            APITestResult = string.Empty;
        }

        public string WarehouseId { get; set; }

        [NopResourceDisplayName("Admin.Orders.Shipments.Products.Warehouse")]
        public string Name { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Api.Fields.ApiKey")]
        public string ApiKey { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Api.Fields.ApiSecret")]
        public string ApiSecret { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Api.Fields.CustomerNumber")]
        public string CustomerNumber { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Api.Fields.MoBoCN")]
        public string MoBoCN { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Api.Fields.ContractId")]
        public string ContractId { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Api.Fields.TestResult")]
        public string APITestResult { get; set; }
    }

    public record ShippingManagerWarehouseSetting
    {
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
    }
}
