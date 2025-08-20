using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;

using Nop.Plugin.Shipping.Manager.Domain;

namespace Nop.Plugin.Shipping.Manager.Data
{
    /// <summary>
    /// Represents a shipping carrier record building configuration
    /// </summary>
    public class CarrierRecordBuilder : NopEntityBuilder<Carrier>
    {
        #region Methods


        /// <summary>
        /// Configures the entity
        /// </summary>
        /// <param name="builder">The builder to be used to configure the entity</param>
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(Carrier.Name)).AsString(256).Nullable()
                .WithColumn(nameof(Carrier.AdminComment)).AsString(256).Nullable()
                .WithColumn(nameof(Carrier.AddressId)).AsInt32()
                .WithColumn(nameof(Carrier.ShippingRateComputationMethodSystemName)).AsString(256).Nullable()
                .WithColumn(nameof(Carrier.Active)).AsBoolean().Nullable();
        }

        #endregion
    }
}