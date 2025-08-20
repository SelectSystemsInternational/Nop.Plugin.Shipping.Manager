using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Razor;

using Nop.Core.Infrastructure;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Date;
using Nop.Services.Orders;

using Nop.Plugin.Shipping.Manager.ViewEngine;
using Nop.Plugin.Shipping.Manager.ExportImport;
using Nop.Plugin.Shipping.Manager.Services;
using Nop.Plugin.Shipping.Manager.Factories;

using Nop.Plugin.Apollo.Integrator.Services;

namespace Nop.Plugin.Shipping.Manager.Infrastructure
{
    /// <summary>
    /// Represents object for the configuring services on application startup
    /// </summary>
    public class NopStartup : INopStartup
    {
        /// <summary>
        /// Add and configure any of the middleware
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        /// <param name="configuration">Configuration of the application</param>
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IServiceCollection, ServiceCollection>();
            services.AddScoped<IShippingManagerService, ShippingManagerService>();
            services.AddScoped<IShippingAddressService, ShippingAddressService>();
            services.AddScoped<ICarrierService, CarrierService>();
            services.AddScoped<ICarrierModelFactory, CarrierModelFactory>();
            services.AddScoped<IEntityGroupService, EntityGroupService>();
            services.AddScoped<IShippingManagerShipmentService, ShippingManagerShipmentService>();
            services.AddScoped<IShippingManagerMessageService, ShippingManagerMessageService>();
            services.AddScoped<IShippingManagerInstallService, ShippingManagerInstallService>();
            services.AddScoped<IShippingOperationsModelFactory, ShippingOperationsModelFactory>();
            services.AddScoped<IOrderOperationsModelFactory, OrderOperationsModelFactory>();
            services.AddScoped<IExportImportManager, ExportImportManager>();
            services.AddScoped<IOrderItemPdfService, OrderItemPdfService>();
            services.AddScoped<IOrderSalesService, OrderSalesService>();
            services.AddScoped<ISendcloudService, SendcloudService>();
            services.AddScoped<IFastwayService, FastwayService>();
            services.AddScoped<ICanadaPostService, CanadaPostService>();
            services.AddScoped<IShipmentDetailsService, ShipmentDetailsService>();
            services.AddScoped<IPackagingOptionService, PackagingOptionService>();

            services.AddScoped<IShipmentService, CustomShipmentService>();
            services.AddScoped<IShippingService, CustomShippingService>();
            services.AddScoped<IDateRangeService, CustomDateRangeService>();
            services.AddScoped<IOrderTotalCalculationService, CustomerOrderTotalCalculationService>();

            services.Configure<RazorViewEngineOptions>(o =>
            {
                o.ViewLocationExpanders.Add(new CustomViewEngine());
            });

        }

        /// <summary>
        /// Configure the using of added middleware
        /// </summary>
        /// <param name="application">Builder for configuring an application's request pipeline</param>
        public void Configure(IApplicationBuilder application)
        {
        }

        /// <summary>
        /// Gets order of this startup configuration implementation
        /// </summary>
        public int Order => 3000;
    }
}
