using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Configuration;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Events;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Pickup;


using Nop.Plugin.Apollo.Integrator.Services;
using Nop.Plugin.Shipping.Manager.Settings;

namespace Nop.Plugin.Shipping.Manager.Services
{
    /// <summary>
    /// Shipment service
    /// </summary>
    public partial class OrderSalesService : IOrderSalesService
    {

        #region Fields

        protected readonly IPickupPluginManager _pickupPluginManager;
        protected readonly IRepository<Product> _productRepository;
        protected readonly IRepository<Order> _orderRepository;
        protected readonly IRepository<OrderItem> _orderItemRepository;
        protected readonly IRepository<Shipment> _shipmentRepository;
        protected readonly IRepository<ShipmentItem> _shipmentItemRepository;
        protected readonly IShippingPluginManager _shippingPluginManager;
        protected readonly IEntityGroupService _entityGroupService;
        protected readonly IWorkContext _workContext;
        protected readonly IProductService _productService;
        protected readonly IOrderService _orderService;
        protected readonly IDateTimeHelper _dateTimeHelper;
        protected readonly IAddressService _addressService;
        protected readonly IWebHelper _webHelper;
        protected readonly IShipmentService _shipmentService;
        protected readonly IShippingService _shippingService;
        protected readonly IStoreContext _storeContext;
        protected readonly IOrderProcessingService _orderProcessingService;
        protected readonly IShippingManagerMessageService _shippingManagerMessageService;
        protected readonly IPdfService _pdfService;
        protected readonly OrderSettings _orderSettings;
        protected readonly IRepository<ProductWarehouseInventory> _productWarehouseInventoryRepository;
        protected readonly ILocalizationService _localizationService;
        protected readonly IRepository<ShipmentItem> _siRepository;
        protected readonly ShippingManagerSettings _shippingManagerSettings;

        #endregion

        #region Ctor

        public OrderSalesService(IPickupPluginManager pickupPluginManager,
            IRepository<Product> productRepository,
            IRepository<Order> orderRepository,
            IRepository<OrderItem> orderItemRepository,
            IRepository<Shipment> shipmentRepository,
            IRepository<ShipmentItem> shipmentItemRepository,
            IShippingPluginManager shippingPluginManager,
            IEntityGroupService entityGroupService,
            IWorkContext workContext, 
            IProductService productService,
            IOrderService orderService,
            IDateTimeHelper dateTimeHelper,
            IAddressService addressService,
            IWebHelper webHelper,
            IShipmentService shipmentService,
            IShippingService shippingService,
            IStoreContext storeContext,
            IOrderProcessingService orderProcessingService,
            IShippingManagerMessageService shippingManagerMessageService,
            IPdfService pdfService,
            OrderSettings orderSettings,
            IRepository<ProductWarehouseInventory> productWarehouseInventoryRepository,
            ILocalizationService localizationService,
            IRepository<ShipmentItem> siRepository,
            ShippingManagerSettings shippingManagerSettings)
        {
            _pickupPluginManager = pickupPluginManager;
            _productRepository = productRepository;
            _orderRepository = orderRepository;
            _orderItemRepository = orderItemRepository;
            _shipmentRepository = shipmentRepository;
            _shipmentItemRepository = shipmentItemRepository;
            _shippingPluginManager = shippingPluginManager;
            _entityGroupService = entityGroupService;
            _workContext = workContext;
            _productService = productService;
            _orderService = orderService;
            _dateTimeHelper = dateTimeHelper;
            _addressService = addressService;
            _webHelper = webHelper;
            _shipmentService = shipmentService;
            _shippingService = shippingService;
            _storeContext = storeContext;
            _orderProcessingService = orderProcessingService;
            _shippingManagerMessageService = shippingManagerMessageService;
            _pdfService = pdfService;
            _orderSettings = orderSettings;
            _productWarehouseInventoryRepository = productWarehouseInventoryRepository;
            _localizationService = localizationService;
            _siRepository = siRepository;
            _shippingManagerSettings = shippingManagerSettings;
    }

        #endregion

        #region Methods

        /// <summary>
        /// Get localised short date format
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the localised short date format
        /// </returns>
        public async Task<string> GetLocalisedShortDateFormatAsync()
        {
            var language = await _workContext.GetWorkingLanguageAsync();

            string shortDatePattern = CultureInfo.GetCultureInfo(language.LanguageCulture).DateTimeFormat.ShortDatePattern;
            string toShortFormat = "DD/MM/YYYY";  // Default pattern

            if (shortDatePattern.Contains("M/d/") || shortDatePattern.Contains("MM/dd/") ||
                shortDatePattern.Contains("M.d.") || shortDatePattern.Contains("MM.dd."))
            {
                // US or others cultures pattern
                toShortFormat = "MM/DD/YYYY";
            }

            return (toShortFormat);
        }

        /// <summary>
        /// Get localised short date format for display
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the localised short date format
        /// </returns>
        public async Task<string> GetLocalisedDisplayShortDateFormatAsync()
        {
            var language = await _workContext.GetWorkingLanguageAsync();
            string shortDatePattern = CultureInfo.GetCultureInfo(language.LanguageCulture).DateTimeFormat.ShortDatePattern;

            string toDisplayFormat = "dd/MM/yyyy"; // Default pattern

            if (shortDatePattern.Contains("M/d/") || shortDatePattern.Contains("MM/dd/") ||
                shortDatePattern.Contains("M.d.") || shortDatePattern.Contains("MM.dd."))
            {
                //US or others cultures pattern
                toDisplayFormat = "MM/dd/yyyy";
            }

            return (toDisplayFormat);
        }

        /// <summary>
        /// Get quantity in shipments. For example, get planned quantity to be shipped
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="warehouseId">Warehouse identifier</param>
        /// <param name="ignoreShipped">Ignore already shipped shipments</param>
        /// <param name="ignoreDelivered">Ignore already delivered shipments</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the quantity
        /// </returns>
        public virtual async Task<int> GetQuantityInShipmentsAsync(Product product, int warehouseId,
            bool ignoreShipped, bool ignoreDelivered)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            //only products with "use multiple warehouses" are handled this way
            if (product.ManageInventoryMethod != ManageInventoryMethod.ManageStock)
                return 0;
            if (!product.UseMultipleWarehouses)
                return 0;

            const int cancelledOrderStatusId = (int)OrderStatus.Cancelled;

            var query = _siRepository.Table;

            query = from si in query
                    join s in _shipmentRepository.Table on si.ShipmentId equals s.Id
                    join o in _orderRepository.Table on s.OrderId equals o.Id
                    where !o.Deleted && o.OrderStatusId != cancelledOrderStatusId
                    select si;

            query = query.Distinct();

            if (warehouseId > 0)
                query = query.Where(si => si.WarehouseId == warehouseId);
            if (ignoreShipped)
            {
                query = from si in query
                        join s in _shipmentRepository.Table on si.ShipmentId equals s.Id
                        where !s.ShippedDateUtc.HasValue
                        select si;
            }

            if (ignoreDelivered)
            {
                query = from si in query
                        join s in _shipmentRepository.Table on si.ShipmentId equals s.Id
                        where !s.DeliveryDateUtc.HasValue
                        select si;
            }

            var queryProductOrderItems = from orderItem in _orderItemRepository.Table
                                         where orderItem.ProductId == product.Id
                                         select orderItem.Id;
            query = from si in query
                    where queryProductOrderItems.Any(orderItemId => orderItemId == si.OrderItemId)
                    select si;

            //some null validation
            var result = Convert.ToInt32(await query.SumAsync(si => (int?)si.Quantity));
            return result;
        }

        /// <summary>
        /// Gets a total number of items in all shipments
        /// </summary>
        /// <param name="orderItem">Order item</param>
        /// <returns> The total number of items in all shipments </returns>
        public virtual int GetTotalNumberOfItemsInAllShipment(OrderItem orderItem)
        {
            if (orderItem == null)
                throw new ArgumentNullException(nameof(orderItem));

            var totalInShipments = 0;
            var query = _siRepository.Table;

            const int cancelledOrderStatusId = (int)OrderStatus.Cancelled;

            query = from si in query
                    join s in _shipmentRepository.Table on si.ShipmentId equals s.Id
                    join o in _orderRepository.Table on s.OrderId equals o.Id
                    where !o.Deleted && o.OrderStatusId != cancelledOrderStatusId
                    select si;

            query = query.Distinct();

            foreach (var si in query)
                totalInShipments += si.Quantity;

            return totalInShipments;
        }

        /// <summary>
        /// Gets a total number of already items which can be added to new shipments
        /// </summary>
        /// <param name="orderItem">Order item</param>
        /// <returns>Total number of already delivered items which can be added to new shipments</returns>
        public virtual int GetTotalNumberOfItemsCanBeAddedToShipment(OrderItem orderItem)
        {
            if (orderItem == null)
                throw new ArgumentNullException(nameof(orderItem));

            var totalInShipments = GetTotalNumberOfItemsInAllShipment(orderItem);

            var qtyOrdered = orderItem.Quantity;
            var qtyCanBeAddedToShipmentTotal = qtyOrdered - totalInShipments;
            if (qtyCanBeAddedToShipmentTotal < 0)
                qtyCanBeAddedToShipmentTotal = 0;

            return qtyCanBeAddedToShipmentTotal;
        }

        #endregion

        #region Order Sales

        /// <summary>
        /// Gets an order item
        /// </summary>
        /// <param name="orderItemId">Order item identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the Order item
        /// </returns>
        public virtual async Task<OrderItem> GetOrderItemByIdAsync(int orderItemId)
        {
            if (orderItemId == 0)
                return null;

            return await _orderItemRepository.GetByIdAsync(orderItemId);
        }

        /// <summary>
        /// Gets all order sales items as an array
        /// </summary>
        /// <param name="orderItemId">Order item identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the array of order sales item
        /// </returns>
        public async Task<int[]> GetAllOrderSalesItemsAsync()
        {
            var orderSalesOrders = await GetAllOrderSalesMappingAsync();
            int[] orderId = new int[orderSalesOrders.Count];
            int i = 0;
            foreach (var item in orderSalesOrders)
            {
                orderId[i] = item.Id;
                i += 1;
            }

            return orderId;
        }

        /// <summary>
        /// Gets all order item sales items as an array
        /// </summary>
        /// <param name="orderItemId">Order item identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the array of order item sales items
        /// </returns>
        public async Task<int[]> GetAllOrderItemSalesItemsAsync()
        {
            var orderSalesOrders = await GetAllOrderItemSalesMappingAsync();
            int[] orderItemId = new int[orderSalesOrders.Count];
            int i = 0;
            foreach (var item in orderSalesOrders)
            {
                var orderItem = await GetOrderItemByIdAsync(item.Id);
                if (orderItem != null)
                {
                    orderItemId[i] = item.Id;
                    i += 1;
                }
            }

            return orderItemId;
        }

        /// <summary>
        /// Gets all shippable order sales items 
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the array of shippable order sales item
        /// </returns>
        public virtual async Task<IList<Order>> GetAllOrderSalesMappingAsync()
        {
            int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

            int ssNotYetShipped = (int)ShippingStatus.NotYetShipped;
            //int osPending = (int)OrderStatus.Pending;
            //int osProcessing = (int)OrderStatus.Processing;
            //int psPending = (int)PaymentStatus.Pending;
            //int psPaid = (int)PaymentStatus.Paid;
            //int psAuthorised = (int)PaymentStatus.Authorized;
            //int ssPartiallyShipped = (int)ShippingStatus.PartiallyShipped;

            var query = (from oi in _orderItemRepository.Table
                          join o in _orderRepository.Table on oi.OrderId equals o.Id
                          join p in _productRepository.Table on oi.ProductId equals p.Id
                          where (vendorId == 0 || p.VendorId == vendorId) &&
                                ((p.IsShipEnabled && o.ShippingStatusId == ssNotYetShipped) ||
                                 (!p.IsShipEnabled && _shippingManagerSettings.OrderManagerOperations))  &&
                                !o.Deleted
                          select o).OrderByDescending(x => x.CreatedOnUtc);

            return query.ToList();
        }

        /// <summary>
        /// Gets all shippable order item sales items 
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the array of shippable order item sales item
        /// </returns>
        public virtual async Task<IList<OrderItem>> GetAllOrderItemSalesMappingAsync()
        {
            int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

            int ssNotYetShipped = (int)ShippingStatus.NotYetShipped;
            //int osPending = (int)OrderStatus.Pending;
            //int osProcessing = (int)OrderStatus.Processing;
            //int psPending = (int)PaymentStatus.Pending;
            //int psPaid = (int)PaymentStatus.Paid;
            //int psAuthorised = (int)PaymentStatus.Authorized;
            //int ssPartiallyShipped = (int)ShippingStatus.PartiallyShipped;

            //var query1 = (from oi in _orderItemRepository.Table
            //              join o in _orderRepository.Table on oi.OrderId equals o.Id
            //              join si in _shipmentItemRepository.Table on oi.Id equals si.OrderItemId
            //              join p in _productRepository.Table on oi.ProductId equals p.Id
            //              where (vendorId == 0 || p.VendorId == vendorId) &&
            //                    p.IsShipEnabled && o.ShippingStatusId == ssNotYetShipped &&
            //                    !o.Deleted
            //              select oi.Id);

            //var query2 = (from oi in _orderItemRepository.Table
            //             join o in _orderRepository.Table on oi.OrderId equals o.Id
            //             join p in _productRepository.Table on oi.ProductId equals p.Id
            //             where (vendorId == 0 || p.VendorId == vendorId) && 
            //                   p.IsShipEnabled && o.ShippingStatusId == ssNotYetShipped &&
            //                   !query1.Contains(oi.Id) &&
            //                   !o.Deleted
            //             select oi);

            var query = (from oi in _orderItemRepository.Table
                         join o in _orderRepository.Table on oi.OrderId equals o.Id
                         join p in _productRepository.Table on oi.ProductId equals p.Id
                         where (vendorId == 0 || p.VendorId == vendorId) &&
                                ((p.IsShipEnabled && o.ShippingStatusId == ssNotYetShipped) ||
                                 (!p.IsShipEnabled && _shippingManagerSettings.OrderManagerOperations)) &&
                               !o.Deleted
                         select oi);

            return query.ToList();
        }

        /// <summary>
        /// Gets all order sales items as an array
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the array of order item sales item
        /// </returns>
        public virtual IList<Order> GetOrderSalesItemsbyOrderIds(int[] orderIds)
        {
            var query = (from o in _orderRepository.Table 
                         where orderIds.Contains(o.Id) &&
                               o.OrderStatusId != 40 // Cancelled
                         select o).ToList();
            return query;
        }

        /// <summary>
        /// Gets all order item sales items as an array
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the array of order item sales item
        /// </returns>
        public virtual IList<OrderItem> GetOrderSalesItemsbyOrderItemIds(int[] orderItemIds)
        {
            var query = (from oi in _orderItemRepository.Table
                         join o in _orderRepository.Table on oi.OrderId equals o.Id
                         where orderItemIds.Contains(o.Id) &&
                               o.OrderStatusId != 40 // Cancelled
                         select oi).ToList();
            return query;
        }

        /// <summary>
        /// Gets all order sales items as a paged list
        /// </summary>
        /// <param name="orderIds">Order item identifier array</param> 
        /// <param name="paymentMethod">Payment method identifier</param> 
        /// <param name="fromDate">From date</param> 
        /// <param name="toDate">To date</param> 
        /// <param name="isPay">Order is payedr</param> 
        /// <param name="orderbyName">Order by name</param> 
        /// <param name="searchProductName">Search product name</param> 
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>/// 
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the paged list of order item sales item
        /// </returns>
        public virtual async Task<IPagedList<Order>> GetSalesOrderListAsync(int[] orderIds, string paymentMethod, DateTime? fromDate, DateTime? toDate,
                bool isPay, bool orderbyName, string searchProductName, int pageIndex = 0, int pageSize = int.MaxValue)
        {

            var storeId = await _entityGroupService.GetActiveStoreScopeConfiguration();

            var query = (from oi in _orderItemRepository.Table
                         join o in _orderRepository.Table on oi.OrderId equals o.Id
                         join p in _productRepository.Table on oi.ProductId equals p.Id
                         where orderIds.Contains(o.Id) &&
                                o.OrderStatusId != 40 && // Cancelled
                                (paymentMethod == "All" || o.PaymentMethodSystemName == paymentMethod) &&
                                (isPay != true || o.PaymentStatusId != 30) // Order Not Paid
                         select o).OrderByDescending(x => x.Id).Distinct().ToList();

            if (fromDate.HasValue || toDate.HasValue)
            {
                if (fromDate.HasValue)
                    fromDate = _dateTimeHelper.ConvertToUtcTime(fromDate.Value, await _dateTimeHelper.GetCurrentTimeZoneAsync());

                if (toDate.HasValue)
                    toDate = _dateTimeHelper.ConvertToUtcTime(toDate.Value.AddDays(1), await _dateTimeHelper.GetCurrentTimeZoneAsync());

                if (query != null)
                {
                    query = (from o in query
                             where (!fromDate.HasValue || fromDate.Value <= o.CreatedOnUtc) &&
                                    (!toDate.HasValue || toDate.Value >= o.CreatedOnUtc)
                             select o).OrderByDescending(x => x.Id).Distinct().ToList();
                }
            }

            if (!string.IsNullOrEmpty(searchProductName))
            {
                int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

                //get products
                var categoryIds = new List<int> { 0 };
                var products = await _productService.SearchProductsAsync(showHidden: false,
                    categoryIds: categoryIds,
                    storeId: storeId,
                    vendorId: vendorId,
                    warehouseId: 0,
                    productType: ProductType.SimpleProduct,
                    keywords: searchProductName,
                    pageIndex: pageIndex, pageSize: pageSize,
                    overridePublished: null);

                query = (from o in query
                         join oi in _orderItemRepository.Table on  o.Id equals oi.OrderId 
                         join p in products.ToList() on oi.ProductId equals p.Id
                         select o).Distinct().ToList();

            }

            var records = new PagedList<Order>(query, pageIndex, pageSize);
            return records;
        }

        /// <summary>
        /// Gets all order item sales items as a paged list
        /// </summary>
        /// <param name="orderItemIds">Order item identifier array</param> 
        /// <param name="paymentMethod">Payment method identifier</param> 
        /// <param name="fromDate">From date</param> 
        /// <param name="toDate">To date</param> 
        /// <param name="isPay">Order is payedr</param> 
        /// <param name="orderbyName">Order by name</param> 
        /// <param name="searchProductName">Search product name</param> 
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>/// 
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the paged list of order item sales item
        /// </returns>
        public virtual async Task<IList<OrderItem>> GetSalesOrderItemsListAsync(int[] orderItemIds, string paymentMethod, DateTime? fromDate, DateTime? toDate,
            bool isPay, bool orderbyName, string searchProductName, int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var storeId = await _entityGroupService.GetActiveStoreScopeConfiguration();

            var query = (from oi in _orderItemRepository.Table
                         join o in _orderRepository.Table on oi.OrderId equals o.Id
                         join p in _productRepository.Table on oi.ProductId equals p.Id
                         where orderItemIds.Contains(oi.Id) &&
                                p.IsShipEnabled &&
                                o.OrderStatusId != 40 && // Cancelled
                                (paymentMethod == "All" || o.PaymentMethodSystemName == paymentMethod) &&
                                (isPay != true || o.PaymentStatusId != 30) // Order Not Paid
                         select oi).ToList();

            if (fromDate.HasValue || toDate.HasValue)
            {
                if (fromDate.HasValue)
                    fromDate = _dateTimeHelper.ConvertToUtcTime(fromDate.Value, await _dateTimeHelper.GetCurrentTimeZoneAsync());

                if (toDate.HasValue)
                    toDate = _dateTimeHelper.ConvertToUtcTime(toDate.Value.AddDays(1), await _dateTimeHelper.GetCurrentTimeZoneAsync());

                if (query != null)
                {
                    query = (from oi in query
                             join o in _orderRepository.Table on oi.OrderId equals o.Id
                             where (!fromDate.HasValue || fromDate.Value <= o.CreatedOnUtc) &&
                                    (!toDate.HasValue || toDate.Value >= o.CreatedOnUtc)
                             select oi).ToList();
                }
            }

            if (!string.IsNullOrEmpty(searchProductName))
            {
                int vendorId = await _entityGroupService.GetActiveVendorScopeAsync();

                //get products
                var categoryIds = new List<int> { 0 };
                var products = await _productService.SearchProductsAsync(showHidden: false,
                    categoryIds: categoryIds,
                    storeId: storeId,
                    vendorId: vendorId,
                    warehouseId: 0,
                    productType: ProductType.SimpleProduct,
                    keywords: searchProductName,
                    pageIndex: pageIndex, pageSize: pageSize,
                    overridePublished: null);

                query = (from oi in query
                         join o in _orderRepository.Table on oi.OrderId equals o.Id  
                         join p in products.ToList() on oi.ProductId equals p.Id
                         select oi).ToList();

            }

            return query;
        }

        /// <summary>
        /// Gets customer billing email from order
        /// </summary>
        /// <param name="order">Order</param> 
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the email
        /// </returns>
        public async Task<string> GetCustomerEmailDetailsAsync(Order order)
        {
            var billingAddress = await _addressService.GetAddressByIdAsync(order.BillingAddressId);
            return billingAddress.FirstName + " " + billingAddress.LastName + " (" + billingAddress.Email + ")";
        }

        /// <summary>
        /// Gets customer billing email from order item
        /// </summary>
        /// <param name="orderItem">OrderItem</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the email
        /// </returns>
        public async Task<string> GetCustomerEmailDetailsAsync(OrderItem orderItem)
        {
            string formated = "";
            var order = await _orderService.GetOrderByIdAsync(orderItem.OrderId);
            if (order != null)
            {
                var billingAddress = await _addressService.GetAddressByIdAsync(order.BillingAddressId);
                formated = billingAddress.FirstName + " " + billingAddress.LastName + " (" + billingAddress.Email + ")";
            }
            return formated;
        }

        /// <summary>
        /// Gets customer fullname from order
        /// </summary>
        /// <param name="order">Order</param> 
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the customer fullname
        /// </returns>
        public async Task<string> GetCustomerFullNameDetailsAsync(Order order)
        {
            string formated = "";
            var billingAddress = await _addressService.GetAddressByIdAsync(order.BillingAddressId);
            formated = billingAddress.FirstName + " " + billingAddress.LastName;
            return formated;
        }

        /// <summary>
        /// Gets customer fullname from order item
        /// </summary>
        /// <param name="orderItem">OrderItem</param> 
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the customer fullname
        /// </returns>
        public async Task<string> GetCustomerFullNameDetailsAsync(OrderItem orderItem)
        {
            string formated = "";
            var order = await _orderService.GetOrderByIdAsync(orderItem.OrderId);
            if (order != null)
            {
                var billingAddress = await _addressService.GetAddressByIdAsync(order.BillingAddressId);
                formated = billingAddress.FirstName + " " + billingAddress.LastName;
            }
            return formated;
        }

        /// <summary>
        /// Gets edit order page URL
        /// </summary>
        /// <param name="orderId">Order identifier</param> 
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the URL
        /// </returns>
        public string GetEditOrderPageUrl(int orderId)
        {
            return _webHelper.GetStoreLocation() + "Admin/Order/Edit/" + orderId.ToString();
        }

        /// <summary>
        /// Gets add shipment page URL
        /// </summary>
        /// <param name="orderId">Order</param> 
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the URL
        /// </returns>
        public string GetAddShipmntPageUrl(int orderId)
        {
            return _webHelper.GetStoreLocation() + "Admin/OrderSales/AddShipment/" + orderId.ToString();
        }

        /// <summary>
        /// Gets edit order page URL
        /// </summary>
        /// <param name="orderId">Order identifier</param> 
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the URL
        /// </returns>
        public string GetEditShipmentPageUrl(int orderId)
        {
            var shipment = GetShipmentsByOrderId(orderId);
            if (shipment != null && shipment.Count != 0)
                return _webHelper.GetStoreLocation() + "Admin/Order/ShipmentDetails/" + shipment.FirstOrDefault().Id.ToString();
            else
                return _webHelper.GetStoreLocation() + "Admin/Order/ShipmentList/";
        }

        /// <summary>
        /// Gets a list items of order
        /// </summary>
        /// <param name="orderId">Order identifier</param>
        /// <param name="isNotReturnable">Value indicating whether this product is returnable; pass null to ignore</param>
        /// <param name="isShipEnabled">Value indicating whether the entity is ship enabled; pass null to ignore</param>
        /// <param name="vendorId">Vendor identifier; pass 0 to ignore</param>
        /// <returns>Result</returns>
        public virtual IList<OrderItem> GetOrderItems(int orderId, bool? isNotReturnable = null, bool? isShipEnabled = null, int vendorId = 0)
        {
            if (orderId == 0)
                return new List<OrderItem>();

            return (from oi in _orderItemRepository.Table
                    join p in _productRepository.Table on oi.ProductId equals p.Id
                    where
                    oi.OrderId == orderId &&
                    (!isShipEnabled.HasValue || (p.IsShipEnabled == isShipEnabled.Value)) &&
                    (!isNotReturnable.HasValue || (p.NotReturnable == isNotReturnable)) &&
                    (vendorId <= 0 || (p.VendorId == vendorId))
                    select oi).ToList();
        }

        /// <summary>
        /// Gets a list of order shipments
        /// </summary>
        /// <param name="orderId">Order identifier</param>
        /// <param name="shipped">A value indicating whether to count only shipped or not shipped shipments; pass null to ignore</param>
        /// <param name="vendorId">Vendor identifier; pass 0 to ignore</param>
        /// <returns>Result</returns>
        public virtual IList<Shipment> GetShipmentsByOrderId(int orderId, bool? shipped = null, int vendorId = 0)
        {
            if (orderId == 0)
                return new List<Shipment>();

            var shipments = _shipmentRepository.Table;
            if (shipped.HasValue)
            {
                shipments = shipments.Where(s => s.ShippedDateUtc.HasValue == shipped);
            }

            return shipments.Where(shipment => shipment.OrderId == orderId).ToList();
        }

        /// <summary>
        /// Gets a shipment items of shipment
        /// </summary>
        /// <param name="shipmentId">Shipment identifier</param>
        /// <returns>Shipment items</returns>
        public virtual IList<ShipmentItem> GetShipmentItemsByShipmentId(int shipmentId)
        {
            if (shipmentId == 0)
                return null;

            return _shipmentItemRepository.Table.Where(si => si.ShipmentId == shipmentId).ToList();
        }

        /// <summary>
        /// Gets order shipping details
        /// </summary>
        /// <param name="order">Order identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the Shipment items
        /// </returns> 
        public async Task<string> GetOrderShippingDetailsAsync(Order order)
        {

            string status = string.Empty;
            if (order != null)
            {
                var shipments = GetShipmentsByOrderId(order.Id);
                if (shipments.Count == 0)
                    status = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Sales.NoShipmentCreated");
                else if (shipments.Count == 1)
                    status = shipments.Count.ToString() + " Shipment";
                else
                    status = shipments.Count.ToString() + " Shipments";
          
                return status;
            }

            return null;

        }

        /// <summary>
        /// Gets order item shipping details
        /// </summary>
        /// <param name="orderItem">OrderItem identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the order items shipping details
        /// </returns> 
        public async Task<List<Warehouse>> GetOrderItemShippingDetailsAsync(OrderItem orderItem)
        {
            var warehouseList = new List<Warehouse>();

            var order = await _orderService.GetOrderByIdAsync(orderItem.OrderId);
            if (order != null)
            {
                var shipments = GetShipmentsByOrderId(orderItem.OrderId);
                foreach (var shipment in shipments)
                {
                    var shipmentItems = GetShipmentItemsByShipmentId(shipment.Id);
                    if (shipmentItems != null)
                    {
                        foreach (var shipmentItem in shipmentItems)
                        {
                            if (orderItem.Id == shipmentItem.OrderItemId)
                            {
                                var wareHouse = await _shippingService.GetWarehouseByIdAsync(shipmentItem.WarehouseId);
                                if (wareHouse != null)
                                    warehouseList.Add(wareHouse);
                            }
                        }
                    }
                }
            }

            if (warehouseList.Count == 0)
            {
                var noWarehouse = new Warehouse();
                noWarehouse.Name = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Sales.NoShipmentCreated");

                var shipments = await _shipmentService.GetShipmentsByOrderIdAsync(order.Id);
                if (shipments != null && shipments.Count != 0)
                    noWarehouse.Name = await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Sales.NoWarehouseSelected");

                warehouseList.Add(noWarehouse);
            }

            return warehouseList;
        }

        /// <summary>
        /// Gets flag to confirm order can be approved
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the flag
        /// </returns> 
        public bool CanMarkOrderAsApproved(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            if (order.OrderStatus == OrderStatus.Cancelled)
                return false;

            if (!HasOrderShipment(order))
                return false;

            if (order.OrderStatus == OrderStatus.Pending)
                return true;

            return false;
        }

        /// <summary>
        /// Gets flag to confirm order has shipments
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the flag
        /// </returns> 
        public bool HasOrderShipment(Order order)
        {
            if (GetShipmentsByOrderId(order.Id).Count() > 0)
                return true;

            return false;
        }

        /// <summary>
        /// Marks all orders as approved
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// </returns> 
        public async Task MarkOrderAsApprovedAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            if (CanMarkOrderAsApproved(order))
            {

                // Send the email 

                if (order.PaymentStatusId == (int)PaymentStatus.Pending)
                {
                    //var orderPlacedAttachmentFilePath = _orderSettings.AttachPdfInvoiceToOrderPlacedEmail ? await _pdfService.PrintOrderToPdfAsync(order) : null;
                    //var orderPlacedAttachmentFileName = _orderSettings.AttachPdfInvoiceToOrderPlacedEmail ? "order.pdf" : null;
                    //await _shippingManagerMessageService.SendOrderShippmentCreatedVendorNotificationAsync(order, order.CustomerLanguageId,
                    //    orderPlacedAttachmentFilePath, orderPlacedAttachmentFileName);
                }

                //order.PaymentStatusId = (int)PaymentStatus.Paid; // for testing
                order.OrderStatusId = (int)OrderStatus.Processing;
                order.ShippingStatusId = (int)ShippingStatus.NotYetShipped;

                await _orderService.UpdateOrderAsync(order);

                //add a note
                await AddOrderNoteAsync(order, "Order has been marked as Approved");
            }
        }

        /// <summary>
        /// Add order note
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="note">Note text</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public virtual async Task AddOrderNoteAsync(Order order, string note)
        {
            await _orderService.InsertOrderNoteAsync(new OrderNote
            {
                OrderId = order.Id,
                Note = note,
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Add order note for a shipment
        /// </summary>
        /// <param name="orderShipment">Shipment order</param>
        /// <param name="note">Note text</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public virtual async Task AddOrderNoteForShipmentAsync(Shipment orderShipment, string note)
        {
            var shipment = await _shipmentService.GetShipmentByIdAsync(orderShipment.Id);
            if (shipment != null)
            {
                await _orderService.InsertOrderNoteAsync(new OrderNote
                {
                    OrderId = shipment.OrderId,
                    Note = note,
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Updates the shipment item
        /// </summary>
        /// <param name="shipmentItem">Shipment item</param>
        public virtual async Task UpdateShipmentItemAsync(ShipmentItem shipmentItem)
        {
            if (shipmentItem == null)
                throw new ArgumentNullException(nameof(shipmentItem));

            await _siRepository.UpdateAsync(shipmentItem);
        }

        /// <summary>
        /// Gets a shipment item
        /// </summary>
        /// <param name="shipmentItemId">Shipment item identifier</param>
        /// <returns>Shipment item</returns>
        public virtual async Task<ShipmentItem> GetShipmentItemByIdAsync(int shipmentItemId)
        {
            if (shipmentItemId == 0)
                return null;

            return await _siRepository.GetByIdAsync(shipmentItemId);
        }

        /// <summary>
        /// Balance the given quantity in the warehouses.
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="warehouseId">Warehouse identifier</param>
        /// <param name="quantity">Quantity</param>
        public virtual async Task BalanceInventoryAsync(Product product, int warehouseId, int quantity)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            //Warehouse to which reserve is being transferred
            var productInventory = _productWarehouseInventoryRepository.Table
                .Where(pwi => pwi.ProductId == product.Id && pwi.WarehouseId == warehouseId)
                .ToList()
                .FirstOrDefault();

            if (productInventory == null)
                return;

            var selectQty = Math.Min(productInventory.StockQuantity - productInventory.ReservedQuantity, quantity);
            productInventory.ReservedQuantity += selectQty;

            //remove from reserve in other warehouses what has just been reserved in the current warehouse to equalize the total
            var productAnotherInventories = _productWarehouseInventoryRepository.Table
                .Where(pwi => pwi.ProductId == product.Id && pwi.WarehouseId != warehouseId)
                .OrderByDescending(ob => ob.ReservedQuantity)
                .ToList();

            var qty = selectQty;
            //We need to make a balance in all warehouses, as resources of one warehouse may not be enough
            foreach (var productAnotherInventory in productAnotherInventories)
            {
                if (qty > 0)
                {
                    if (productAnotherInventory.ReservedQuantity >= qty)
                    {
                        productAnotherInventory.ReservedQuantity -= qty;
                    }
                    else
                    {
                        //Here we can transfer only a part of the reserve, the rest will be sought in other warehouses.
                        qty = selectQty - productAnotherInventory.ReservedQuantity;
                        productAnotherInventory.ReservedQuantity = 0;
                    }
                }
            }

            await _productService.UpdateProductAsync(product);
        }

        #endregion

    }
}