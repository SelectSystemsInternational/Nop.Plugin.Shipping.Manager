using FluentMigrator.Builders.Create.Table;
using Nop.Core.Domain.Shipping;
using Nop.Data.Extensions;
using Nop.Data.Mapping.Builders;

using Nop.Plugin.Shipping.Manager.Domain;

namespace Nop.Plugin.Shipping.Manager.Data
{
    /// <summary>
    /// Represents a shipment item entity builder
    /// </summary>
    public partial class ShipmentDetailsBuilder : NopEntityBuilder<ShipmentDetails>
    {
        #region Methods

        /// <summary>
        /// Apply entity configuration
        /// </summary>
        /// <param name="table">Create table expression builder</param>
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(ShipmentDetails.OrderShipmentId)).AsInt32().ForeignKey<Shipment>()
                .WithColumn(nameof(ShipmentDetails.ShippingMethodId)).AsInt32()
                .WithColumn(nameof(ShipmentDetails.PackagingOptionItemId)).AsInt32()
                .WithColumn(nameof(ShipmentDetails.ShipmentId)).AsString(100).NotNullable()
                .WithColumn(nameof(ShipmentDetails.Cost)).AsDecimal(18, 2)
                .WithColumn(nameof(ShipmentDetails.Group)).AsString(100).NotNullable()
                .WithColumn(nameof(ShipmentDetails.LabelUrl)).AsString(256).NotNullable()
                .WithColumn(nameof(ShipmentDetails.ManifestUrl)).AsString(256).NotNullable()
                .WithColumn(nameof(ShipmentDetails.ScheduledShipDate)).AsDateTime2().Nullable();

        }

        #endregion
    }
}