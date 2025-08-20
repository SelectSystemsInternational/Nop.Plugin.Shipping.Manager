using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;

using Nop.Plugin.Apollo.Integrator.Domain;

namespace Nop.Plugin.Shipping.Manager.Data
{
    /// <summary>
    /// Represents a entity group record building configuration
    /// </summary>
    public class EntityGroupRecordBuilder : NopEntityBuilder<EntityGroup>
    {

        #region Methods

        /// <summary>
        /// Configures the entity
        /// </summary>
        /// <param name="builder">The builder to be used to configure the entity</param>
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(EntityGroup.EntityId))
                .AsInt32()
                .WithColumn(nameof(EntityGroup.StoreId))
                .AsInt32()
                .WithColumn(nameof(EntityGroup.VendorId))
                .AsInt32()
                .WithColumn(nameof(EntityGroup.WarehouseId))
                .AsInt32()
                .WithColumn(nameof(EntityGroup.KeyGroup))
                .AsString(256)
                .Nullable()
                .WithColumn(nameof(EntityGroup.Key))
                .AsString(256)
                .Nullable()
                .WithColumn(nameof(EntityGroup.Value))
                .AsString(256)
                .Nullable()
                .WithColumn(nameof(EntityGroup.CreatedOrUpdatedDateUTC))
                .AsDateTime2();
        }

        #endregion

    }
}