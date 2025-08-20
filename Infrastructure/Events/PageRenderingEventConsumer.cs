using System.Linq;
using System.Threading.Tasks; 

using Nop.Services.Events;
using Nop.Services.Logging;
using Nop.Services.Shipping;
using Nop.Web.Framework.Events;
using Nop.Web.Framework.UI;

namespace Nop.Plugin.Shipping.Manager.Infrastructure.Events
{
    /// <summary>
    /// Represents mollie payment method 
    /// </summary>
    public partial class PageRenderingEventConsumer : IConsumer<PageRenderingEvent>
    {
        #region Fields

        protected readonly ILogger _logger;
        protected readonly IShippingPluginManager _shippingPluginManager;

        #endregion

        #region Ctor

        public PageRenderingEventConsumer(
            ILogger logger,
            IShippingPluginManager shippingPluginManager)
        {
            _logger = logger;
            _shippingPluginManager = shippingPluginManager;
        }

        #endregion

        #region Utilities

        #endregion

        #region Page rendering

        /// Handle page rendering event
        /// </summary>
        /// <param name="eventMessage">Event message</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task HandleEventAsync(PageRenderingEvent eventMessage)
        {
            //check whether the plugin is active
            if (!await _shippingPluginManager.IsPluginActiveAsync(ShippingManagerDefaults.SystemName))
                return;
            
            var routeName = eventMessage.GetRouteName();
            if (routeName is null ||
                (!routeName.Equals(ShippingManagerDefaults.CheckoutBillingAddressRouteName) &&
                 !routeName.Equals(ShippingManagerDefaults.CheckoutShippingMethodRouteName) &&
                 !routeName.Equals(ShippingManagerDefaults.OnePageCheckoutRouteName) &&
                 !routeName.Equals(ShippingManagerDefaults.RealOnePageCheckoutRouteName) && 
                 !routeName.Equals(ShippingManagerDefaults.CheckoutCompleted))
                )
                return;

            //add Embedded Fields sсript and styles to the one page checkout
            if (eventMessage.GetRouteName().Any(routeName => routeName.Equals(ShippingManagerDefaults.CheckoutBillingAddressRouteName)))
            {

            }
            else if (routeName.Equals(ShippingManagerDefaults.CheckoutShippingMethodRouteName))
            {
                eventMessage.Helper.AddScriptParts(ResourceLocation.Footer, ShippingManagerDefaults.SendcloudScriptPath, excludeFromBundle: true);
                eventMessage.Helper.AddScriptParts(ResourceLocation.Footer, ShippingManagerDefaults.CheckoutShippingMethodScriptPath, excludeFromBundle: true);
                eventMessage.Helper.AddCssFileParts(ShippingManagerDefaults.EmbeddedFieldsStylePath, excludeFromBundle: true);
            }
            else if (routeName.Equals(ShippingManagerDefaults.OnePageCheckoutRouteName))
            {
                eventMessage.Helper.AddScriptParts(ResourceLocation.Footer, ShippingManagerDefaults.SendcloudScriptPath, excludeFromBundle: true);
                eventMessage.Helper.AddScriptParts(ResourceLocation.Footer, ShippingManagerDefaults.CheckoutOpcShippingMethodScriptPath, excludeFromBundle: true);
                eventMessage.Helper.AddCssFileParts(ShippingManagerDefaults.EmbeddedFieldsStylePath, excludeFromBundle: true);
            }
            else if (routeName.Equals(ShippingManagerDefaults.RealOnePageCheckoutRouteName))
            {
                eventMessage.Helper.AddScriptParts(ResourceLocation.Footer, ShippingManagerDefaults.SendcloudScriptPath, excludeFromBundle: true);
                eventMessage.Helper.AddScriptParts(ResourceLocation.Footer, ShippingManagerDefaults.RealOnePageCheckoutScriptPath, excludeFromBundle: true);
            }
            else if (routeName.Equals(ShippingManagerDefaults.CheckoutCompleted))
            {
                eventMessage.Helper.AddScriptParts(ResourceLocation.Footer, ShippingManagerDefaults.CheckoutCompletedScriptPath, excludeFromBundle: true);
            }

        }

        #endregion
    }
}
