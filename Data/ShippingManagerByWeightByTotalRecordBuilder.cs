using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;

using Nop.Plugin.Shipping.Manager.Domain;

namespace Nop.Plugin.Shipping.Manager.Data
{
    /// <summary>
    /// Represents a entity group record building configuration
    /// </summary>
    public class ShippingManagerByWeightByTotalRecordBuilder : NopEntityBuilder<ShippingManagerByWeightByTotal>
    {
        #region Methods


        /// <summary>
        /// Configures the entity
        /// </summary>
        /// <param name="builder">The builder to be used to configure the entity</param>
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(ShippingManagerByWeightByTotal.StoreId)).AsInt32()
                .WithColumn(nameof(ShippingManagerByWeightByTotal.VendorId)).AsInt32()
                .WithColumn(nameof(ShippingManagerByWeightByTotal.WarehouseId)).AsInt32()
                .WithColumn(nameof(ShippingManagerByWeightByTotal.CarrierId)).AsInt32()
                .WithColumn(nameof(ShippingManagerByWeightByTotal.CountryId)).AsInt32()
                .WithColumn(nameof(ShippingManagerByWeightByTotal.StateProvinceId)).AsInt32()
                .WithColumn(nameof(ShippingManagerByWeightByTotal.Zip)).AsString(256).Nullable()
                .WithColumn(nameof(ShippingManagerByWeightByTotal.ShippingMethodId)).AsInt32()
                .WithColumn(nameof(ShippingManagerByWeightByTotal.WeightFrom)).AsDecimal(18, 2)
                .WithColumn(nameof(ShippingManagerByWeightByTotal.WeightTo)).AsDecimal(18, 2)
                .WithColumn(nameof(ShippingManagerByWeightByTotal.CalculateCubicWeight)).AsBoolean()
                .WithColumn(nameof(ShippingManagerByWeightByTotal.CubicWeightFactor)).AsDecimal(18, 2)
                .WithColumn(nameof(ShippingManagerByWeightByTotal.OrderSubtotalFrom)).AsDecimal(18, 2)
                .WithColumn(nameof(ShippingManagerByWeightByTotal.OrderSubtotalTo)).AsDecimal(18, 2)
                .WithColumn(nameof(ShippingManagerByWeightByTotal.AdditionalFixedCost)).AsDecimal(18, 2)
                .WithColumn(nameof(ShippingManagerByWeightByTotal.PercentageRateOfSubtotal)).AsDecimal(18, 2)
                .WithColumn(nameof(ShippingManagerByWeightByTotal.RatePerWeightUnit)).AsDecimal(18, 2)
                .WithColumn(nameof(ShippingManagerByWeightByTotal.LowerWeightLimit)).AsDecimal(18, 2)
                .WithColumn(nameof(ShippingManagerByWeightByTotal.CutOffTimeId)).AsInt32()
                .WithColumn(nameof(ShippingManagerByWeightByTotal.FriendlyName)).AsString(256).Nullable()
                .WithColumn(nameof(ShippingManagerByWeightByTotal.TransitDays)).AsInt32().Nullable()
                .WithColumn(nameof(ShippingManagerByWeightByTotal.Active)).AsBoolean()
                .WithColumn(nameof(ShippingManagerByWeightByTotal.DisplayOrder)).AsInt32()
                .WithColumn(nameof(ShippingManagerByWeightByTotal.Description)).AsString(256).Nullable();
        }

        #endregion
    }
}
