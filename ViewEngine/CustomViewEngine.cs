using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Razor;

using Nop.Core;
using Nop.Core.Infrastructure;
using Nop.Services.Plugins;
using Nop.Services.Configuration;

using Nop.Web.Framework.Themes;

namespace Nop.Plugin.Shipping.Manager.ViewEngine
{
    public class CustomViewEngine : IViewLocationExpander
    {

        public string storeTheme = "DefaultClean";

        public void PopulateValues(ViewLocationExpanderContext context)
        {
            if (context.AreaName?.Equals("Admin") ?? false)
                return;

            var settingService = EngineContext.Current.Resolve<ISettingService>();
            var storeContext = EngineContext.Current.Resolve<IStoreContext>();

            storeTheme = settingService.GetSettingByKeyAsync("storeinformationsettings.defaultstoretheme", "DefaultClean", storeContext.GetCurrentStore().Id, true).Result;

            var themeContext = (IThemeContext)context.ActionContext.HttpContext.RequestServices.GetService(typeof(IThemeContext));
            if (themeContext != null)
                context.Values[storeTheme] = themeContext.GetWorkingThemeNameAsync().ConfigureAwait(false).ToString();
        }

        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {

            var pluginService = EngineContext.Current.Resolve<IPluginService>();

            var plugin = pluginService.GetPluginDescriptorBySystemNameAsync<IPlugin>("Shipping.Manager", LoadPluginsMode.InstalledOnly).Result;
            if (plugin != null)
            {
                if (plugin.SystemName == "Shipping.Manager")
                {
                    if (context.ViewName == "_AdminPopupLayout" || context.ViewName == "_AdminScripts")
                    {
                        viewLocations = new[] {
                                $"~/Areas/Admin/Views/Shared/{context.ViewName}.cshtml"
                            }.Concat(viewLocations);
                    }

                    if (context.AreaName == "Admin")
                    {

                    }
                    else
                    {
                        if (!context.Values.TryGetValue(storeTheme, out string theme))
                            return viewLocations;
                        if (context.ControllerName == "RealOnePageCheckout" && context.ViewName == "RealOnePageCheckout")
                        {
                            viewLocations = new[] { $"~/Plugins/SSI.Shipping.Manager/Views/FrontView/RealOnePageCheckout.cshtml" }.Concat(viewLocations);
                        }
                        else if (context.ControllerName == "Checkout" && context.ViewName == "BillingAddress")
                        {
                            viewLocations = new[] { $"~/Plugins/SSI.Shipping.Manager/Views/FrontView/BillingAddress.cshtml" }.Concat(viewLocations);
                        }
                        else if (context.ControllerName == "Checkout" && context.ViewName == "ShippingMethod")
                        {
                            viewLocations = new[] { $"~/Plugins/SSI.Shipping.Manager/Views/FrontView/ShippingMethod.cshtml" }.Concat(viewLocations);
                        }
                        else if (context.ControllerName == "RealOnePageCheckout" && context.ViewName == "ShippingMethod")
                        {
                            viewLocations = new[] { $"~/Plugins/SSI.Shipping.Manager/Views/FrontView/RealOnePageShippingMethod.cshtml" }.Concat(viewLocations);
                        }
                        else if (context.ControllerName == "Checkout" && context.ViewName == "OpcShippingMethods")
                        {
                            viewLocations = new[] { $"~/Plugins/SSI.Shipping.Manager/Views/FrontView/OpcShippingMethods.cshtml" }.Concat(viewLocations);
                        }
                        else if (context.ControllerName == "Checkout" && context.ViewName == "Completed")
                        {
                            viewLocations = new[] { $"~/Plugins/SSI.Shipping.Manager/Views/FrontView/Completed.cshtml" }.Concat(viewLocations);
                        }
                        else if (context.ControllerName == "OrderSales")
                        { 
                            if (context.ViewName == "Confirm")                      
                                viewLocations = new[] { $"~/Areas/Admin/Views/Shared/Confirm.cshtml" }.Concat(viewLocations);
                            else if (context.ViewName == "Components/AdminWidget/Default")
                                viewLocations = new[] { $"~/Areas/Admin/Views/Shared/Components/AdminWidget/Default.cshtml" }.Concat(viewLocations);
                        }
                    }
                }
            }

            return viewLocations;
        }
    }
}
