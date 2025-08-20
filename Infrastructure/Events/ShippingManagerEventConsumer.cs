using System.Threading.Tasks;

using Nop.Core.Domain.Shipping;
using Nop.Core.Events;
using Nop.Services.Configuration;
using Nop.Services.Events;
using Nop.Services.Logging;
using Nop.Services.Shipping;

using Nop.Plugin.Shipping.Manager.Services;
using Nop.Plugin.Shipping.Manager.Settings;

using Nop.Plugin.Apollo.Integrator.Services;

namespace Nop.Plugin.Shipping.Manager.Infrastructure.Cache
{
    /// <summary>
    /// Event consumer of the "Fixed or by weight" shipping plugin (used for removing unused settings)
    /// </summary>
    public partial class ShippingManagerEventConsumer : IConsumer<EntityInsertedEvent<Shipment>>
    {
        #region Fields
        
        protected readonly ISettingService _settingService;
        protected readonly IEntityGroupService _entityGroupService;
        protected readonly IShipmentService _shipmentService;
        protected readonly ShippingManagerSettings _shippingManagerSettings;
        protected readonly ILogger _logger;
        protected readonly ISendcloudService _sendcloudService;

        #endregion

        #region Ctor

        public ShippingManagerEventConsumer(ISettingService settingService,
            IEntityGroupService entityGroupService,
            IShipmentService shipmentService,
            ShippingManagerSettings shippingManagerSettings,
            ILogger logger,
            ISendcloudService sendcloudService)
        {
            _settingService = settingService;
            _entityGroupService = entityGroupService;
            _shipmentService = shipmentService;
            _shippingManagerSettings = shippingManagerSettings;
            _logger = logger;
            _sendcloudService = sendcloudService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Handle shipping method deleted event
        /// </summary>
        /// <param name="eventMessage">Event message</param>
        public async Task HandleEventAsync(EntityDeletedEvent<ShippingMethod> eventMessage)
        {
            var shippingMethod = eventMessage?.Entity;
            if (shippingMethod == null)
                return;

            //Get vendor
            int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

            //delete saved fixed rate if exists
            var setting = _settingService.GetSetting(string.Format(ShippingManagerDefaults.FIXED_RATE_SETTINGS_KEY, vendorId, shippingMethod.Id));
            if (setting != null)
                _settingService.DeleteSetting(setting);
        }

        public async Task HandleEventAsync(EntityInsertedEvent<Shipment> shipmentInsertedEvent)
        {

            var shipment = await _shipmentService.GetShipmentByIdAsync(shipmentInsertedEvent.Entity.Id);
            if (shipment == null)
                return;

        }

        #endregion
    }
}