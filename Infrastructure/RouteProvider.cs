using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Core;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Services.Plugins;
using Nop.Services.Configuration;
using Nop.Web.Framework.Mvc.Routing;
using Nop.Plugin.Shipping.Manager.Services;

namespace Nop.Plugin.Shipping.Manager
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {

            endpointRouteBuilder.MapControllerRoute("Nop.Plugins.Shipping.Manager.AddShipmentPopup", 
                "Plugins/ShippingManager/AddShipment", 
                new { controller = "OrderSales", action = "AddShipment" });
            
            endpointRouteBuilder.MapControllerRoute("Nop.Plugins.Shipping.Manager.OrderShipmentDetails",
                "Plugins/ShippingManager/shipment/{shipmentId}",
                new { controller = "OrderSales", action = "OrderShipmentDetails" });

            endpointRouteBuilder.MapControllerRoute("Nop.Plugins.Shipping.Manager.EditShipmentPopup",
                "Plugins/ShippingManager/EditShipment",
                new { controller = "OrderSales", action = "EditShipment" });

            endpointRouteBuilder.MapControllerRoute("Nop.Plugins.Shipping.Manager.SendcloudWebhook", "Sendcloud/Webhook",
                new { controller = "Sendcloud", action = "SendcloudWebhook" });

            endpointRouteBuilder.MapControllerRoute("Nop.Plugins.Shipping.Manager.SaveServicePointWebhook", "Sendcloud/SaveServicePoint",
                 new { controller = "Sendcloud", action = "SaveServicePointWebhook" });

            endpointRouteBuilder.MapControllerRoute("Nop.Plugins.Shipping.Manager.GetShippingAddress", "GetShippingAddress",
                new { controller = "Sendcloud", action = "GetShippingAddress" });

        }

        public int Priority
        {
            get { return 100; }
        }
    }
}
