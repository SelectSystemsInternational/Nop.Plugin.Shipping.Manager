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

namespace Nop.Plugin.Shipping.Manager.UpgradeTo460
{
    [NopMigration("2022/06/15 00:00:00", "Shipping.Manager Shipment Details Update")]
    public class ShippingManagerMigration : MigrationBase
    {
        #region Fields

        protected readonly IMigrationManager _migrationManager;
        protected readonly ILanguageService _languageService;
        protected readonly ILocalizationService _localizationService;
        protected readonly ISettingService _settingService;

        #endregion

        #region Ctor

        public ShippingManagerMigration(IMigrationManager migrationManager,
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
            var languages = _languageService.GetAllLanguagesAsync(true).Result;
            var languageId = languages
                .FirstOrDefault(lang => lang.UniqueSeoCode == new CultureInfo(NopCommonDefaults.DefaultLanguageCulture).TwoLetterISOLanguageName)
                ?.Id;

            //_localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
            //{

            //};

            //if (!Schema.Schema("dbo").Table(NameCompatibilityManager.GetTableName(typeof(ShipmentDetails))).Exists())
            //    _migrationManager.BuildTable<ShipmentDetails>(Create);

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