using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;

using Nop.Plugin.Shipping.Manager.Domain;

namespace Nop.Plugin.Shipping.Manager.Data
{
    /// <summary>
    /// Represents a shipping carrier record building configuration
    /// </summary>
    public class CutOffTimeRecordBuilder : NopEntityBuilder<CutOffTime>
    {
        #region Methods


        /// <summary>
        /// Configures the entity
        /// </summary>
        /// <param name="builder">The builder to be used to configure the entity</param>
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(CutOffTime.Name))
                .AsString(256)
                .Nullable()
                .WithColumn(nameof(CutOffTime.DisplayOrder))
                .AsInt32();
        }

        #endregion
    }
}