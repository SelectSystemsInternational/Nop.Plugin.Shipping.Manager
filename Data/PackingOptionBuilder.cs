using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;

using Nop.Plugin.Shipping.Manager.Domain;

namespace Nop.Plugin.Shipping.Manager.Data
{
    /// <summary>
    /// Represents a product entity builder
    /// </summary>
    public partial class PackagingOptionBuilder : NopEntityBuilder<PackagingOption>
    {
        #region Methods

        /// <summary>
        /// Apply entity configuration
        /// </summary>
        /// <param name="table">Create table expression builder</param>
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(PackagingOption.Name)).AsString(400).NotNullable()
                .WithColumn(nameof(PackagingOption.Sku)).AsString(400).Nullable()
                .WithColumn(nameof(PackagingOption.ManufacturerPartNumber)).AsString(400).Nullable()
                .WithColumn(nameof(PackagingOption.RequiredPackagingOptionsIds)).AsString(1000).Nullable()
                .WithColumn(nameof(PackagingOption.AllowedQuantities)).AsString(1000).Nullable();
        }

        #endregion
    }
}