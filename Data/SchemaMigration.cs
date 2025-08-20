using Nop.Core;
using Nop.Data.Mapping;
using Nop.Data.Migrations;
using Nop.Data.Extensions;
using Nop.Services.Configuration;
using Nop.Services.Stores;

using FluentMigrator.Infrastructure;

using Nop.Plugin.Shipping.Manager.Domain;
using Nop.Plugin.Shipping.Manager.Settings;

using Nop.Plugin.Apollo.Integrator.Domain;

namespace Nop.Plugin.Shipping.Manager.Data
{

    [NopMigration("2022/10/09 09:40:55:1687541", "Shipping.Manager base schema")]
    public class SchemaMigration : FluentMigrator.Migration
    {
        protected IMigrationManager _migrationManager;
        protected ISettingService _settingService;
        protected IStoreService _storeService;
        protected IStoreContext _storeContext;
        protected IMigrationContext _context;

        public SchemaMigration(IMigrationManager migrationManager,
            ISettingService settingService,
            IStoreService storeService,
            IStoreContext storeContext,
            IMigrationContext context)
        {
            _migrationManager = migrationManager;
            _settingService = settingService;
            _storeService = storeService;
            _storeContext = storeContext;
            _context = context;
        }

        public override void Up()
        {
            // Try to create database tables
            if (!Schema.Schema("dbo").Table(NameCompatibilityManager.GetTableName(typeof(Carrier))).Exists())
                Create.TableFor<Carrier>();

            // Try to create database tables
            if (!Schema.Schema("dbo").Table(NameCompatibilityManager.GetTableName(typeof(CutOffTime))).Exists())
                Create.TableFor<CutOffTime>();

            if (!Schema.Schema("dbo").Table(NameCompatibilityManager.GetTableName(typeof(EntityGroup))).Exists())
                Create.TableFor<EntityGroup>();

            if (!Schema.Schema("dbo").Table(NameCompatibilityManager.GetTableName(typeof(ShippingManagerByWeightByTotal))).Exists())
                Create.TableFor<ShippingManagerByWeightByTotal>();

            if (!Schema.Schema("dbo").Table(NameCompatibilityManager.GetTableName(typeof(ShipmentDetails))).Exists())
                Create.TableFor<ShipmentDetails>();
        }

        public override void Down()
        {
            var shippingManagerSettings = _settingService.LoadSetting<ShippingManagerSettings>();

            if (shippingManagerSettings.DeleteTablesonUninstall)
            {
                if (Schema.Schema("dbo").Table(NameCompatibilityManager.GetTableName(typeof(Carrier))).Exists())
                    Delete.Table(NameCompatibilityManager.GetTableName(typeof(Carrier)));

                if (Schema.Schema("dbo").Table(NameCompatibilityManager.GetTableName(typeof(CutOffTime))).Exists())
                    Delete.Table(NameCompatibilityManager.GetTableName(typeof(CutOffTime)));

                if (Schema.Schema("dbo").Table(NameCompatibilityManager.GetTableName(typeof(EntityGroup))).Exists())
                    Delete.Table(NameCompatibilityManager.GetTableName(typeof(EntityGroup)));

                if (Schema.Schema("dbo").Table(NameCompatibilityManager.GetTableName(typeof(ShippingManagerByWeightByTotal))).Exists())
                    Delete.Table(NameCompatibilityManager.GetTableName(typeof(ShippingManagerByWeightByTotal)));

                if (Schema.Schema("dbo").Table(NameCompatibilityManager.GetTableName(typeof(ShipmentDetails))).Exists())
                    Delete.Table(NameCompatibilityManager.GetTableName(typeof(ShipmentDetails)));
            }

            _settingService.DeleteSettingAsync<ShippingManagerSettings>().GetAwaiter().GetResult();
        }
    }
}
