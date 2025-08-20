using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Nop.Data;
using Nop.Data.Mapping;
using Nop.Data.Migrations;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;

using FluentMigrator;

using Nop.Plugin.Shipping.Manager.Domain;
using Nop.Services.Plugins;

namespace Nop.Plugin.Shipping.Manager.UpgradeTo46068
{
    [NopMigration("2025/07/06 00:00:00", "Shipping.Manager Update Table for v48", MigrationProcessType.Update)]
    public class UpgradeTo48 : MigrationBase
    {
        #region Fields

        private readonly IMigrationManager _migrationManager;
        private readonly ILanguageService _languageService;
        private readonly ILocalizationService _localizationService;
        private readonly ISettingService _settingService;

        #endregion

        #region Ctor

        public UpgradeTo48(IMigrationManager migrationManager,
            ILanguageService languageService,
            ILocalizationService localizationService,
            ISettingService settingService)
        {
            _migrationManager = migrationManager;
            _languageService = languageService;
            _localizationService = localizationService;
            _settingService = settingService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Collect the UP migration expressions
        /// </summary>
        public override void Up()
        {
            if (!DataSettingsManager.IsDatabaseInstalled())
                return;

            //locales
            var languages = _languageService.GetAllLanguages(true);
            var languageId = languages
                .FirstOrDefault(lang => lang.UniqueSeoCode == new CultureInfo(NopCommonDefaults.DefaultLanguageCulture).TwoLetterISOLanguageName)
                ?.Id;

            //locales
            var resourcesList = new Dictionary<string, string>
            {
                ["Plugins.Shipping.Manager.Fields.Active"] = "Active",
                ["Plugins.Shipping.Manager.Fields.Active.Hint"] = "Enable method to be displayed in checkout",
                ["Plugins.Shipping.Manager.Fields.DisplayOrder"] = "Display Order",
                ["Plugins.Shipping.Manager.Fields.DisplayOrder.Hint"] = "Display order for the foudn methods",
                ["Plugins.Shipping.Manager.SearchActive"] = "Search Active Only",
                ["Plugins.Shipping.Manager.SearchActive.Hint"] = "Only records that are marked as Active will be displayed",
                ["PDFInvoice.Email"] = "Email: {0}",
                ["PDFInvoice.BillingInformation"] = "Billing Information",
                ["PDFInvoice.ShippingInformation"] = "Shipping Information",
                ["PDFInvoice.Product(s)"] = "Order Items",
                ["Plugins.Shipping.Manager.Confirm.PrintAllInvoices"] = "Please confirm you wish to print all Invoices found from selection",
                ["Plugins.Shipping.Manager.Confirm.PrintAllSales"] = "Please confirm you wish to print all Sales found from selection",
                ["Plugins.Shipping.Manager.Orders.Shipments.Cancel.Error"] = "Shipment Cancel Error",
                ["Plugins.Shipping.Manager.Orders.Shipments.Cancelled"] = "Shipment has been cancelled",

                ["Plugins.Shipping.Manager.Fields.DisplayOrder"] = "Display Order",
                ["Plugins.Shipping.Manager.Fields.DisplayOrder.Hint"] = "Display order for the found shipping methods",

                ["Plugins.Shipping.Manager.Fields.Description"] = "Description",
                ["Plugins.Shipping.Manager.Fields.Description.Hint"] = "Rate description override for Shipping Method Description",
                ["Plugins.Shipping.Manager.Shipment.ParcelsCreated"] = "Total Parcels Created:",
                ["Plugins.Shipping.Manager.Fields.DisplayManualOperations"] = "Display Manual Operations",
                ["Plugins.Shipping.Manager.Fields.DisplayManualOperations.Hint"] = "Select the option to display the manual operations buttons",
                ["Plugins.Shipping.Manager.Operation.Setup"] = "Shipping Operation Configuration",

                ["Plugins.Shipping.Manager.Shipments.ScheduledShipDate"] = "Scheduled Ship Date",
                ["Plugins.Shipping.Manager.Shipments.ScheduledShipDate.Hint"] = "The date for the Scheduled package pickup",
                ["Plugins.Shipping.Manager.Shipments.ScheduledShipDate.Button"] = "Set Ship Date",
            };

            _localizationService.AddOrUpdateLocaleResource(resourcesList);

            var shippingManagerByWeightByTotalTableName = NameCompatibilityManager.GetTableName(typeof(ShippingManagerByWeightByTotal));

            if (Schema.Schema("dbo").Table(NameCompatibilityManager.GetTableName(typeof(ShippingManagerByWeightByTotal))).Exists())
            {
                //add column description column
                var descriptionColumnName = "Description";

                if (!Schema.Table(shippingManagerByWeightByTotalTableName).Column(descriptionColumnName).Exists())
                    Alter.Table(shippingManagerByWeightByTotalTableName).AddColumn(descriptionColumnName).AsString(256).Nullable();

                //add column display order column
                var displayOrderColumnName = "DisplayOrder";

                if (!Schema.Table(shippingManagerByWeightByTotalTableName).Column(displayOrderColumnName).Exists())
                    Alter.Table(shippingManagerByWeightByTotalTableName).AddColumn(displayOrderColumnName).AsInt32().SetExistingRowsTo(0);

                //add column active
                var activeColumnName = "Active";

                if (!Schema.Table(shippingManagerByWeightByTotalTableName).Column(activeColumnName).Exists())
                    Alter.Table(shippingManagerByWeightByTotalTableName).AddColumn(activeColumnName).AsBoolean().SetExistingRowsTo(false);
            }

            var shipmentDetailsTableName = NameCompatibilityManager.GetTableName(typeof(ShipmentDetails));

            if (Schema.Schema("dbo").Table(NameCompatibilityManager.GetTableName(typeof(ShipmentDetails))).Exists())
            {
                //add column description column
                var scheduledShipDate = "ScheduledShipDate";

                if (!Schema.Table(shipmentDetailsTableName).Column(scheduledShipDate).Exists())
                    Alter.Table(shipmentDetailsTableName).AddColumn(scheduledShipDate).AsDateTime2().Nullable();
            }

        }
            

        /// <summary>
        /// Collects the DOWN migration expressions
        /// </summary>
        public override void Down()
        {
            // Nothing
        }

        #endregion
    }
}