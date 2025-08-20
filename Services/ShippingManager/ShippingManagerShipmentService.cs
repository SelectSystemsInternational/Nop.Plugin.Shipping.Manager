using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Orders;
using Nop.Data;
using Nop.Core.Domain.Shipping;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Orders;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Pickup;

using Nop.Plugin.Shipping.Manager.Settings;

using Nop.Plugin.Apollo.Integrator.Domain;
using Nop.Plugin.Apollo.Integrator.Services;

namespace Nop.Plugin.Shipping.Manager.Services
{
    /// <summary>
    /// Shipment service
    /// </summary>
    public partial class ShippingManagerShipmentService : IShippingManagerShipmentService
    {

        #region Fields

        protected readonly IPickupPluginManager _pickupPluginManager;
        protected readonly IRepository<Product> _productRepository;
        protected readonly IRepository<Order> _orderRepository;
        protected readonly IRepository<OrderItem> _orderItemRepository;
        protected readonly IRepository<Shipment> _shipmentRepository;
        protected readonly IRepository<ShipmentItem> _siRepository;
        protected readonly IShippingPluginManager _shippingPluginManager;
        protected readonly IEntityGroupService _entityGroupService;
        protected readonly IWorkContext _workContext;
        protected readonly IProductService _productService;
        protected readonly IOrderService _orderService;
        protected readonly IAddressService _addressService;
        protected readonly IWebHelper _webHelper;
        protected readonly IShipmentService _shipmentService;
        protected readonly IShippingService _shippingService;
        protected readonly IStoreContext _storeContext;
        protected readonly IOrderProcessingService _orderProcessingService;
        protected readonly IShippingManagerMessageService _shippingManagerMessageService;
        protected readonly IPdfService _pdfService;
        protected readonly OrderSettings _orderSettings;
        protected readonly ICarrierService _carrierService;
        protected readonly ShippingManagerSettings _shippingManagerSettings;
        protected readonly IRepository<Address> _addressRepository;

        #endregion

        #region Ctor

        public ShippingManagerShipmentService(IPickupPluginManager pickupPluginManager,
            IRepository<Product> productRepository,
            IRepository<Order> orderRepository,
            IRepository<OrderItem> orderItemRepository,
            IRepository<Shipment> shipmentRepository,
            IRepository<ShipmentItem> siRepository,
            IShippingPluginManager shippingPluginManager,
            IEntityGroupService entityGroupService,
            IWorkContext workContext,
            IProductService productService,
            IOrderService orderService,
            IAddressService addressService,
            IWebHelper webHelper,
            IShipmentService shipmentService,
            IShippingService shippingService,
            IStoreContext storeContext,
            IOrderProcessingService orderProcessingService,
            IShippingManagerMessageService shippingManagerMessageService,
            IPdfService pdfService,
            OrderSettings orderSettings,
            ICarrierService carrierService,
            ShippingManagerSettings shippingManagerSettings,
            IRepository<Address> addressRepository)
        {
            _pickupPluginManager = pickupPluginManager;
            _productRepository = productRepository;
            _orderRepository = orderRepository;
            _orderItemRepository = orderItemRepository;
            _shipmentRepository = shipmentRepository;
            _siRepository = siRepository;
            _shippingPluginManager = shippingPluginManager;
            _entityGroupService = entityGroupService;
            _workContext = workContext;
            _productService = productService;
            _orderService = orderService;
            _addressService = addressService;
            _webHelper = webHelper;
            _shipmentService = shipmentService;
            _shippingService = shippingService;
            _storeContext = storeContext;
            _orderProcessingService = orderProcessingService;
            _shippingManagerMessageService = shippingManagerMessageService;
            _pdfService = pdfService;
            _orderSettings = orderSettings;
            _carrierService = carrierService;
            _shippingManagerSettings = shippingManagerSettings;
            _addressRepository = addressRepository;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Search shipments
        /// </summary>
        /// <param name="vendorId">Vendor identifier; 0 to load all records</param>
        /// <param name="vendorGroupId">Vendor Group identifier; 0 to load all records</param>
        /// <param name="billingAddressId">Billing Address identifier; 0 to load all records</param>
        /// <param name="shippingAddressId">Shipping Address identifier; 0 to load all records</param>
        /// <param name="warehouseId">Warehouse identifier, only shipments with products from a specified warehouse will be loaded; 0 to load all orders</param>
        /// <param name="carrierId">Carrier identifier, only shipments with products from a specified carrier will be loaded; 0 to load all orders</param> 
        /// <param name="shippingCountryId">Shipping country identifier; 0 to load all records</param>
        /// <param name="shippingStateId">Shipping state identifier; 0 to load all records</param>
        /// <param name="shippingCounty">Shipping county; null to load all records</param>
        /// <param name="shippingCity">Shipping city; null to load all records</param>
        /// <param name="trackingNumber">Search by tracking number</param>
        /// <param name="loadNotShipped">A value indicating whether we should load only not shipped shipments</param>
        /// <param name="loadNotDelivered">A value indicating whether we should load only not delivered shipments</param>
        /// <param name="orderId">Order identifier; 0 to load all records</param>
        /// <param name="createdFromUtc">Created date from (UTC); null to load all records</param>
        /// <param name="createdToUtc">Created date to (UTC); null to load all records</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipments
        /// </returns>
        public virtual async Task<IPagedList<Shipment>> GetAllShipmentsAsync(int pageIndex = 0, int pageSize = int.MaxValue, 
            int vendorId = 0,
            int vendorGroupId = 0,
            int billingAddressId = 0,
            int shippingAddressId = 0,
            int warehouseId = 0,
            int carrierId = 0,
            int shippingCountryId = 0,
            int shippingStateId = 0,
            string shippingCounty = null,
            string shippingCity = null,
            string trackingNumber = null,
            string shippingMethod = null,
            bool loadNotShipped = false,
            bool loadNotDelivered = false,
            int orderId = 0,
            DateTime? createdFromUtc = null, DateTime? createdToUtc = null)
        {

            var entityGroupMembers = new List<EntityGroup>();
            var vendorGroup = await _entityGroupService.GetEntityGroupByIdAsync(vendorGroupId);
            if (vendorGroup != null)
                entityGroupMembers = await _entityGroupService.GetEntityGroupMembersAsync(vendorGroup);

            var carrier = await _carrierService.GetCarrierByIdAsync(carrierId);

            var shipments = await _shipmentRepository.GetAllPagedAsync(query =>
            {
                if (orderId > 0)
                    query = query.Where(o => o.OrderId == orderId);

                if (billingAddressId > 0)
                    query = from s in query
                            join o in _orderRepository.Table on s.OrderId equals o.Id
                            where _addressRepository.Table.Any(a => a.Id == (o.BillingAddressId) && a.Id == billingAddressId)
                            select s;

                if (shippingAddressId > 0)
                    query = from s in query
                            join o in _orderRepository.Table on s.OrderId equals o.Id
                            where _addressRepository.Table.Any(a => a.Id == (o.ShippingAddressId) && a.Id == shippingAddressId)
                            select s;

                if (!string.IsNullOrEmpty(trackingNumber))
                    query = query.Where(s => s.TrackingNumber.Contains(trackingNumber));

                if (!string.IsNullOrEmpty(shippingMethod))
                    query = from s in query
                            join o in _orderRepository.Table on s.OrderId equals o.Id
                            where o.ShippingMethod.Contains(shippingMethod)
                            select s;

                if (shippingCountryId > 0)
                    query = from s in query
                            join o in _orderRepository.Table on s.OrderId equals o.Id
                            where _addressRepository.Table.Any(a => a.Id == (o.PickupInStore ? o.PickupAddressId : o.ShippingAddressId) && a.CountryId == shippingCountryId)
                            select s;

                if (shippingStateId > 0)
                    query = from s in query
                            join o in _orderRepository.Table on s.OrderId equals o.Id
                            where _addressRepository.Table.Any(a =>
                                a.Id == (o.PickupInStore ? o.PickupAddressId : o.ShippingAddressId) &&
                                a.StateProvinceId == shippingStateId)
                            select s;

                if (!string.IsNullOrWhiteSpace(shippingCounty))
                    query = from s in query
                            join o in _orderRepository.Table on s.OrderId equals o.Id
                            where _addressRepository.Table.Any(a =>
                                a.Id == (o.PickupInStore ? o.PickupAddressId : o.ShippingAddressId) &&
                                a.County.Contains(shippingCounty))
                            select s;

                if (!string.IsNullOrWhiteSpace(shippingCity))
                    query = from s in query
                            join o in _orderRepository.Table on s.OrderId equals o.Id
                            where _addressRepository.Table.Any(a =>
                                a.Id == (o.PickupInStore ? o.PickupAddressId : o.ShippingAddressId) &&
                                a.City.Contains(shippingCity))
                            select s;

                if (loadNotShipped)
                    query = query.Where(s => !s.ShippedDateUtc.HasValue);

                if (loadNotDelivered)
                    query = query.Where(s => !s.DeliveryDateUtc.HasValue);

                if (createdFromUtc.HasValue)
                    query = query.Where(s => createdFromUtc.Value <= s.CreatedOnUtc);

                if (createdToUtc.HasValue)
                    query = query.Where(s => createdToUtc.Value >= s.CreatedOnUtc);

                query = from s in query
                        join o in _orderRepository.Table on s.OrderId equals o.Id
                        where !o.Deleted
                        select s;

                query = query.Distinct();

                if (vendorGroupId != 0)
                {

                    var vendorOrderItemList = new List<int>();

                    foreach (var member in entityGroupMembers)
                    {
                        var queryVendorOrderItems = from orderItem in _orderItemRepository.Table
                                                    join p in _productRepository.Table on orderItem.ProductId equals p.Id
                                                    where p.VendorId == member.EntityId
                                                    select orderItem.Id;

                        vendorOrderItemList.AddRange(queryVendorOrderItems);
                    }

                    query = from s in query
                            join si in _siRepository.Table on s.Id equals si.ShipmentId
                            where vendorOrderItemList.Contains(si.OrderItemId)
                            select s;
                }
                else if (vendorId > 0)
                {
                    var queryVendorOrderItems = from orderItem in _orderItemRepository.Table
                                                join p in _productRepository.Table on orderItem.ProductId equals p.Id
                                                where p.VendorId == vendorId
                                                select orderItem.Id;

                    query = from s in query
                            join si in _siRepository.Table on s.Id equals si.ShipmentId
                            where queryVendorOrderItems.Contains(si.OrderItemId)
                            select s;

                    query = query.Distinct();
                }

                if (warehouseId > 0)
                {
                    query = from s in query
                            join si in _siRepository.Table on s.Id equals si.ShipmentId
                            where si.WarehouseId == warehouseId
                            select s;

                    query = query.Distinct();
                }

                if (carrierId > 0)
                {
                    if (carrier != null)
                        query = from s in query
                                join o in _orderRepository.Table on s.OrderId equals o.Id
                                where !o.ShippingMethod.ToLower().Contains(carrier.Name.ToLower())
                                select s;
                }

                if (!_shippingManagerSettings.OrderByDate)
                    query = query.OrderByDescending(s => s.OrderId); // ToDo o.CreatedOnUtc ??
                else
                    query = query.OrderBy(s => s.CreatedOnUtc);

                return query;

            }, pageIndex, pageSize);

            return shipments;
        }

        #endregion

    }
}
