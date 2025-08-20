using AutoMapper;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Stores;
using Nop.Core.Domain.Vendors;
using Nop.Core.Infrastructure.Mapper;
using Nop.Services.Plugins;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Pickup;
using Nop.Web.Framework.Models;
using Nop.Web.Areas.Admin.Models.Vendors;

using Nop.Plugin.Shipping.Manager.Domain;
using Nop.Plugin.Shipping.Manager.Models.Carrier;
using Nop.Plugin.Shipping.Manager.Models.Order;
using Nop.Plugin.Shipping.Manager.Models.Shipping;
using Nop.Plugin.Shipping.Manager.Models.Warehouse;
using AutoMapper.Internal;

namespace Nop.Plugin.Shipping.Manager.Infrastructure.Mapper
{
    /// <summary>
    /// AutoMapper configuration for admin area models
    /// </summary>
    public class ShippingManagerMapperConfiguration : Profile, IOrderedMapperProfile
    {
        #region Ctor

        public ShippingManagerMapperConfiguration()
        {
            //create specific maps
            CreateShippingMaps();
            CreateVendorMaps();
            CreateWarehouseMaps();


            //add some generic mapping rules
            this.Internal().ForAllMaps((mapConfiguration, map) =>
            {
                //exclude Form and CustomProperties from mapping BaseNopModel
                if (typeof(BaseNopModel).IsAssignableFrom(mapConfiguration.DestinationType))
                {
                    //map.ForMember(nameof(BaseNopModel.Form), options => options.Ignore());
                    map.ForMember(nameof(BaseNopModel.CustomProperties), options => options.Ignore());
                }

                //exclude ActiveStoreScopeConfiguration from mapping ISettingsModel
                if (typeof(ISettingsModel).IsAssignableFrom(mapConfiguration.DestinationType))
                    map.ForMember(nameof(ISettingsModel.ActiveStoreScopeConfiguration), options => options.Ignore());

                //exclude Locales from mapping ILocalizedModel
                if (typeof(ILocalizedModel).IsAssignableFrom(mapConfiguration.DestinationType))
                    map.ForMember(nameof(ILocalizedModel<ILocalizedModel>.Locales), options => options.Ignore());

                //exclude some properties from mapping store mapping supported entities and models
                if (typeof(IStoreMappingSupported).IsAssignableFrom(mapConfiguration.DestinationType))
                    map.ForMember(nameof(IStoreMappingSupported.LimitedToStores), options => options.Ignore());
                if (typeof(IStoreMappingSupportedModel).IsAssignableFrom(mapConfiguration.DestinationType))
                {
                    map.ForMember(nameof(IStoreMappingSupportedModel.AvailableStores), options => options.Ignore());
                    map.ForMember(nameof(IStoreMappingSupportedModel.SelectedStoreIds), options => options.Ignore());
                }

                //exclude some properties from mapping ACL supported entities and models
                if (typeof(IAclSupported).IsAssignableFrom(mapConfiguration.DestinationType))
                    map.ForMember(nameof(IAclSupported.SubjectToAcl), options => options.Ignore());
                if (typeof(IAclSupportedModel).IsAssignableFrom(mapConfiguration.DestinationType))
                {
                    map.ForMember(nameof(IAclSupportedModel.AvailableCustomerRoles), options => options.Ignore());
                    map.ForMember(nameof(IAclSupportedModel.SelectedCustomerRoleIds), options => options.Ignore());
                }

                //exclude some properties from mapping discount supported entities and models
                if (typeof(IDiscountSupportedModel).IsAssignableFrom(mapConfiguration.DestinationType))
                {
                    map.ForMember(nameof(IDiscountSupportedModel.AvailableDiscounts), options => options.Ignore());
                    map.ForMember(nameof(IDiscountSupportedModel.SelectedDiscountIds), options => options.Ignore());
                }

                if (typeof(IPluginModel).IsAssignableFrom(mapConfiguration.DestinationType))
                {
                    //exclude some properties from mapping plugin models
                    map.ForMember(nameof(IPluginModel.ConfigurationUrl), options => options.Ignore());
                    map.ForMember(nameof(IPluginModel.IsActive), options => options.Ignore());
                    map.ForMember(nameof(IPluginModel.LogoUrl), options => options.Ignore());

                    //define specific rules for mapping plugin models
                    if (typeof(IPlugin).IsAssignableFrom(mapConfiguration.SourceType))
                    {
                        map.ForMember(nameof(IPluginModel.DisplayOrder), options => options.MapFrom(plugin => ((IPlugin)plugin).PluginDescriptor.DisplayOrder));
                        map.ForMember(nameof(IPluginModel.FriendlyName), options => options.MapFrom(plugin => ((IPlugin)plugin).PluginDescriptor.FriendlyName));
                        map.ForMember(nameof(IPluginModel.SystemName), options => options.MapFrom(plugin => ((IPlugin)plugin).PluginDescriptor.SystemName));
                    }
                }
            });
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Create shipping maps 
        /// </summary>
        protected virtual void CreateShippingMaps()
        {
            CreateMap<Carrier, CarrierModel>()
                .ForMember(entity => entity.Address, options => options.Ignore());
            CreateMap<CarrierModel, Carrier>()
                .ForMember(entity => entity.AddressId, options => options.Ignore());

            CreateMap<DeliveryDate, DeliveryDateModel>();
            CreateMap<DeliveryDateModel, DeliveryDate>();

            CreateMap<IPickupPointProvider, PickupPointProviderModel>();

            CreateMap<ProductAvailabilityRange, ProductAvailabilityRangeModel>();
            CreateMap<ProductAvailabilityRangeModel, ProductAvailabilityRange>();

            CreateMap<CutOffTime, CutOffTimeModel>();
            CreateMap<CutOffTimeModel, CutOffTime>();

            CreateMap<ShippingMethod, ShippingMethodModel>();
            CreateMap<ShippingMethodModel, ShippingMethod>();

            CreateMap<IShippingRateComputationMethod, ShippingProviderModel>();

            CreateMap<Shipment, ShipmentModel>()
                .ForMember(model => model.ShippedDate, options => options.Ignore())
                .ForMember(model => model.DeliveryDate, options => options.Ignore())
                .ForMember(model => model.TotalWeight, options => options.Ignore())
                .ForMember(model => model.TrackingNumberUrl, options => options.Ignore())
                .ForMember(model => model.Items, options => options.Ignore())
                .ForMember(model => model.ShipmentStatusEvents, options => options.Ignore())
                .ForMember(model => model.CanShip, options => options.Ignore())
                .ForMember(model => model.CanDeliver, options => options.Ignore())
                .ForMember(model => model.CustomOrderNumber, options => options.Ignore());

            CreateMap<IShippingRateComputationMethod, ShippingProviderModel>();
        }

        protected virtual void CreateWarehouseMaps()
        {
            CreateMap<Warehouse, WarehouseModel>()
                .ForMember(entity => entity.Address, options => options.Ignore());
            CreateMap<WarehouseModel, Warehouse>()
                .ForMember(entity => entity.AddressId, options => options.Ignore());
        }

        protected virtual void CreateVendorMaps()
        {
            CreateMap<Vendor, VendorModel>();
            CreateMap<VendorModel, Vendor>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Order of this mapper implementation
        /// </summary>
        public int Order => 0;

        #endregion
    }
}