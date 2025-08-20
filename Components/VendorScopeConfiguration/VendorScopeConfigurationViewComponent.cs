using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Common;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Framework.Components;

using Nop.Core.Domain.Vendors;
using Nop.Services.Vendors;
using Nop.Web.Areas.Admin.Models.Vendors;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;

using Nop.Plugin.Apollo.Integrator.Services;
using Nop.Plugin.Apollo.Integrator.Models.EntityGroup;

namespace Nop.Plugin.Shipping.Manager.Components
{
    /// <summary>
    /// Represents a view component that displays the store scope configuration
    /// </summary>
    public class VendorScopeConfigurationViewComponent : NopViewComponent
    {
        #region Fields

        protected readonly ISettingModelFactory _settingModelFactory;
        protected readonly IVendorService _vendorService;
        protected readonly IEntityGroupService _entityGroupService;
        protected readonly IWorkContext _workContext;
        protected readonly IStoreContext _storeContext;
        protected readonly IGenericAttributeService _genericAttributeService;

        #endregion

        #region Ctor

        public VendorScopeConfigurationViewComponent(ISettingModelFactory settingModelFactory,
            IVendorService vendorService,
            IEntityGroupService entityGroupService,
            IWorkContext workContext,
            IStoreContext storeContext,
            IGenericAttributeService genericAttributeService)
        {
            _settingModelFactory = settingModelFactory;
            _vendorService = vendorService;
            _entityGroupService = entityGroupService;
            _workContext = workContext;
            _storeContext = storeContext;
            _genericAttributeService = genericAttributeService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Invoke view component
        /// </summary>
        /// <returns>View component result</returns>
        public async Task<IViewComponentResult> InvokeAsync()
        {
            //prepare model
            var model = await PrepareVendorScopeConfigurationModelAsync();
            
            return View("~/Plugins/SSI.Shipping.Manager/Views/Shared/VendorScopeConfiguration/Default.cshtml", model);    
        }

        /// <summary>
        /// Prepare store scope configuration model
        /// </summary>
        /// <returns>Store scope configuration model</returns>
        public async virtual Task<VendorScopeConfigurationModel> PrepareVendorScopeConfigurationModelAsync()
        {
            var storeId = await _entityGroupService.GetActiveStoreScopeConfiguration();
            var customer = await _workContext.GetCurrentCustomerAsync();
            var allVendors = (await _vendorService.GetAllVendorsAsync()).Select(vendor => vendor.ToModel<VendorModel>()).ToList();

            var model = new VendorScopeConfigurationModel();

            if (allVendors.Count() != 0)
            {

                int vendorId = 0;
                if (await _workContext.GetCurrentVendorAsync() != null)
                {
                    vendorId = (await _workContext.GetCurrentVendorAsync()).Id;
                    await _genericAttributeService.SaveAttributeAsync(customer, ApolloIntegratorDefaults.AdminAreaVendorScopeConfigurationAttribute, vendorId);
                    allVendors = (await _vendorService.GetAllVendorsAsync()).Where(v => v.Id == vendorId).Select(vendor => vendor.ToModel<VendorModel>()).ToList();
                }

                int groupVendorId = 0, searchVendorId = 0;
                var vendorGroups = new List<Vendor>();

                int selectedVendorId = await _entityGroupService.GetActiveVendorScopeAsync();
                int selectedGroupVendorId = await _entityGroupService.GetActiveGroupVendorScopeAsync();

                if (selectedGroupVendorId != 0)
                    searchVendorId = selectedGroupVendorId;
                else
                    searchVendorId = selectedVendorId;

                var vendor = await _vendorService.GetVendorByIdAsync(searchVendorId);
                if (vendor != null)
                {
                    var entityGroup = _entityGroupService.GetAllEntityGroups("EntityGroup", 0, "VendorGroup", "0", storeId, searchVendorId).FirstOrDefault();
                    if (entityGroup != null)
                    {
                        var entitryGroups = await _entityGroupService.GetEntityGroupMembersAsync(entityGroup);
                        foreach (var eg in entitryGroups)
                        {
                            vendor = await _vendorService.GetVendorByIdAsync(eg.EntityId);
                            if (vendor != null)
                            {
                                groupVendorId = searchVendorId;
                                vendorGroups.Add(vendor);
                            }
                        }
                    }
                }

                model = new VendorScopeConfigurationModel
                {
                    Vendors = allVendors,
                    VendorGroupMembers = vendorGroups.Select(vendor => vendor.ToModel<VendorModel>()).ToList(),
                    VendorId = searchVendorId,
                    GroupVendorId = selectedVendorId,
                    DisplayControl = (vendorId == 0 || groupVendorId != 0) && !(allVendors.Count == 0),
                    DisplayControlVendorGroups = groupVendorId != 0
                };
            }

            return model;
        }

        #endregion
    }
}