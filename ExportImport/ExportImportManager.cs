using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Logging;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Stores;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Directory;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Stores;
using Nop.Services.ExportImport.Help;
using ClosedXML.Excel;

using Nop.Plugin.Shipping.Manager.Domain;
using Nop.Plugin.Shipping.Manager.Models;
using Nop.Plugin.Shipping.Manager.Services;
using Nop.Services.Shipping;
using Nop.Services.Vendors;



namespace Nop.Plugin.Shipping.Manager.ExportImport
{
    /// <summary>
    /// Export manager
    /// </summary>
    public partial class ExportImportManager : IExportImportManager
    {

        #region Fields

        protected readonly IProductService _productService;
        protected readonly IShippingManagerService _shippingManagerService;
        protected readonly ILogger _logger;
        protected readonly IDateTimeHelper _dateTimeHelper;
        protected readonly IPaymentService _paymentService;
        protected readonly ILocalizationService _localizationService;
        protected readonly IWorkContext _workContext;
        protected readonly IPaymentPluginManager _paymentPluginManager;
        protected readonly IOrderService _orderService;
        protected readonly IAddressService _addressService;
        protected readonly CatalogSettings _catalogSettings;
        protected readonly ICountryService _countryService;
        protected readonly IStateProvinceService _stateProvinceService;
        protected readonly IShippingService _shippingService;
        protected readonly IStoreService _storeService;
        protected readonly IVendorService _vendorService;
        protected readonly ICarrierService _carrierService;

        #endregion

        #region Ctor

        public ExportImportManager(IProductService productService,
                IShippingManagerService shippingManagerService,
                ILogger logger,
                IDateTimeHelper dateTimeHelper,
                IPaymentService paymentService,
                ILocalizationService localizationService,
                IWorkContext workContext,
                IPaymentPluginManager paymentPluginManager,
                IOrderService orderService,
                IAddressService addressService,
                CatalogSettings catalogSettings,
                ICountryService countryService,
                IStateProvinceService stateProvinceService,
                IShippingService shippingService,
                IStoreService storeService,
                IVendorService vendorService,
                ICarrierService carrierService)

        {
            _productService = productService;
            _shippingManagerService = shippingManagerService;
            _logger = logger;
            _dateTimeHelper = dateTimeHelper;
            _paymentService = paymentService;
            _localizationService = localizationService;
            _workContext = workContext;
            _paymentPluginManager = paymentPluginManager;
            _orderService = orderService;
            _addressService = addressService;
            _catalogSettings = catalogSettings;
            _countryService = countryService;
            _stateProvinceService = stateProvinceService;
            _shippingService = shippingService;
            _storeService = storeService;
            _vendorService = vendorService;
            _carrierService = carrierService;
        }

        #endregion

        #region Export

        public async Task<byte[]> ExportRatesToXlsxAsync(List<ExportRatesModel> itemsToExport)
        {
            var ratesProperties = new[]
            {
                new PropertyByName<ExportRatesModel, Language>("Id", (rp, l) => rp.Id),
                new PropertyByName<ExportRatesModel, Language>("Active", (rp, l) => rp.Active),
                new PropertyByName<ExportRatesModel, Language>("DisplayOrder", (rp, l) => rp.DisplayOrder),
                new PropertyByName<ExportRatesModel, Language>("Store", (rp, l) => rp.Store),
                new PropertyByName<ExportRatesModel, Language>("Vendor", (rp, l) => rp.Vendor),
                new PropertyByName<ExportRatesModel, Language>("Warehouse", (rp, l) => rp.Warehouse),
                new PropertyByName<ExportRatesModel, Language>("Carrier", (rp, l) => rp.Carrier),
                new PropertyByName<ExportRatesModel, Language>("Country", (rp, l) => rp.Country),
                new PropertyByName<ExportRatesModel, Language>("StateProvince", (rp, l) => rp.StateProvince),
                new PropertyByName<ExportRatesModel, Language>("PostcodeZip", (rp, l) => rp.PostcodeZip),
                new PropertyByName<ExportRatesModel, Language>("ShippingMethod", (rp, l) => rp.ShippingMethod),
                new PropertyByName<ExportRatesModel, Language>("WeightFrom", (rp, l) => rp.WeightFrom),
                new PropertyByName<ExportRatesModel, Language>("WeightTo", (rp, l) => rp.WeightTo),
                new PropertyByName<ExportRatesModel, Language>("CalculateCubicWeight", (rp, l) => rp.CalculateCubicWeight),
                new PropertyByName<ExportRatesModel, Language>("CubicWeightFactor", (rp, l) => rp.CubicWeightFactor),
                new PropertyByName<ExportRatesModel, Language>("OrderSubtotalFrom", (rp, l) => rp.OrderSubtotalFrom),
                new PropertyByName<ExportRatesModel, Language>("OrderSubtotalTo", (rp, l) => rp.OrderSubtotalTo),
                new PropertyByName<ExportRatesModel, Language>("AdditionalFixedCost", (rp, l) => rp.AdditionalFixedCost),
                new PropertyByName<ExportRatesModel, Language>("PercentageRateOfSubtotal", (rp, l) => rp.PercentageRateOfSubtotal),
                new PropertyByName<ExportRatesModel, Language>("RatePerWeightUnit", (rp, l) => rp.RatePerWeightUnit),
                new PropertyByName<ExportRatesModel, Language>("LowerWeightLimit", (rp, l) => rp.LowerWeightLimit),
                new PropertyByName<ExportRatesModel, Language>("CutOffTime", (rp, l) => rp.CutOffTime),
                new PropertyByName<ExportRatesModel, Language>("FriendlyName", (rp, l) => rp.FriendlyName),
                new PropertyByName<ExportRatesModel, Language>("TransitDays", (rp, l) => rp.TransitDays),
                new PropertyByName<ExportRatesModel, Language>("SendFromAddress", (rp, l) => rp.SendFromAddress),
                new PropertyByName<ExportRatesModel, Language>("Description", (rp, l) => rp.Description),
                new PropertyByName<ExportRatesModel, Language>("Delete", (rp, l) => rp.Delete),
            };

            var accommodationAvailabilityManager = new PropertyManager<ExportRatesModel, Language>(ratesProperties, _catalogSettings);

            await using var stream = new MemoryStream();
            // ok, we can run the real code of the sample now
            using (var workbook = new XLWorkbook())
            {
                // uncomment this line if you want the XML written out to the outputDir
                //xlPackage.DebugMode = true; 

                // get handles to the worksheets
                // Worksheet names cannot be more than 31 characters
                var worksheet = workbook.Worksheets.Add(typeof(Order).Name);
                var fpWorksheet = workbook.Worksheets.Add("DataForProductsFilters");
                fpWorksheet.Visibility = XLWorksheetVisibility.VeryHidden;

                //create Headers and format them 
                accommodationAvailabilityManager.WriteDefaultCaption(worksheet);

                var row = 2;
                foreach (var order in itemsToExport)
                {
                    accommodationAvailabilityManager.CurrentObject = order;
                    await accommodationAvailabilityManager.WriteDefaultToXlsxAsync(worksheet, row++);
                }

                workbook.SaveAs(stream);
            }

            return stream.ToArray();
        }

        public async Task<byte[]> ExportOrderItemToXlsx(IEnumerable<OrderItem> itemsToExport)
        {

            var orderList = new List<Order>();

            foreach (var item in itemsToExport)
            {
                var order = await _orderService.GetOrderByIdAsync(item.OrderId);
                if (!orderList.Contains(order))
                    orderList.Add(order);
            }

            return await ExportOrderItemsToXlsxAsync(orderList);
        }

        /// <summary>
        /// Export apollo sales to XLSX
        /// </summary>
        /// <param name="orders">Orders</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public virtual async Task<byte[]> ExportOrderItemsToXlsxAsync(IList<Order> orders)
        {
            //a vendor should have access only to part of order information
            var ignore = await _workContext.GetCurrentVendorAsync() != null;

            //lambda expressions for choosing correct order address
            async Task<Address> orderBillingAddress(Order o) => await _addressService.GetAddressByIdAsync(o.BillingAddressId);

            //property array
            var properties = new[]
            {
                new PropertyByName<Order, Language>("OrderId", (o, l) => o.Id),
                new PropertyByName<Order, Language>("StoreId", (o, l) => o.StoreId),
                new PropertyByName<Order, Language>("CustomerId", (o, l) => o.CustomerId, ignore),
                new PropertyByName<Order, Language>("OrderSubtotalInclTax", (o, l) => o.OrderSubtotalInclTax, ignore),
                new PropertyByName<Order, Language>("OrderSubtotalExclTax", (o, l) => o.OrderSubtotalExclTax, ignore),
                new PropertyByName<Order, Language>("OrderSubTotalDiscountInclTax", (o, l) => o.OrderSubTotalDiscountInclTax, ignore),
                new PropertyByName<Order, Language>("OrderSubTotalDiscountExclTax", (o, l) => o.OrderSubTotalDiscountExclTax, ignore),
                new PropertyByName<Order, Language>("OrderShippingInclTax", (o, l) => o.OrderShippingInclTax, ignore),
                new PropertyByName<Order, Language>("OrderShippingExclTax", (o, l) => o.OrderShippingExclTax, ignore),
                new PropertyByName<Order, Language>("PaymentMethodAdditionalFeeInclTax", (o, l) => o.PaymentMethodAdditionalFeeInclTax, ignore),
                new PropertyByName<Order, Language>("PaymentMethodAdditionalFeeExclTax", (o, l) => o.PaymentMethodAdditionalFeeExclTax, ignore),
                new PropertyByName<Order, Language>("TaxRates", (o, l) => o.TaxRates, ignore),
                new PropertyByName<Order, Language>("OrderTax", (o, l) => o.OrderTax, ignore),
                new PropertyByName<Order, Language>("OrderTotal", (o, l) => o.OrderTotal, ignore),
                new PropertyByName<Order, Language>("RefundedAmount", (o, l) => o.RefundedAmount, ignore),
                new PropertyByName<Order, Language>("OrderDiscount", (o, l) => o.OrderDiscount, ignore),
                new PropertyByName<Order, Language>("CurrencyRate", (o, l) => o.CurrencyRate),
                new PropertyByName<Order, Language>("CustomerCurrencyCode", (o, l) => o.CustomerCurrencyCode),
                new PropertyByName<Order, Language>("AffiliateId", (o, l) => o.AffiliateId, ignore),
                new PropertyByName<Order, Language>("PaymentMethodSystemName", (o, l) => o.PaymentMethodSystemName, ignore),
                new PropertyByName<Order, Language>("ShippingPickupInStore", (o, l) => o.PickupInStore, ignore),
                new PropertyByName<Order, Language>("ShippingMethod", (o, l) => o.ShippingMethod),
                new PropertyByName<Order, Language>("ShippingRateComputationMethodSystemName", (o, l) => o.ShippingRateComputationMethodSystemName, ignore),
                new PropertyByName<Order, Language>("CustomValuesXml", (o, l) => o.CustomValuesXml, ignore),
                new PropertyByName<Order, Language>("VatNumber", (o, l) => o.VatNumber, ignore),
                new PropertyByName<Order, Language>("CreatedOnUtc", (o, l) => o.CreatedOnUtc),
                new PropertyByName<Order, Language>("BillingFirstName", async (o, l)  => (await orderBillingAddress(o))?.FirstName ?? string.Empty),
                new PropertyByName<Order, Language>("BillingLastName", async (o, l)  => (await orderBillingAddress(o))?.LastName ?? string.Empty),
                new PropertyByName<Order, Language>("BillingEmail", async (o, l)  => (await orderBillingAddress(o))?.Email ?? string.Empty),
                new PropertyByName<Order, Language>("BillingCompany", async (o, l)  => (await orderBillingAddress(o))?.Company ?? string.Empty),
                new PropertyByName<Order, Language>("BillingCountry", async (o, l)  => (await _countryService.GetCountryByAddressAsync(await orderBillingAddress(o)))?.Name ?? string.Empty),
                new PropertyByName<Order, Language>("BillingStateProvince", async (o, l)  => (await _stateProvinceService.GetStateProvinceByAddressAsync(await orderBillingAddress(o)))?.Name ?? string.Empty),
                new PropertyByName<Order, Language>("BillingCounty", async (o, l)  => (await orderBillingAddress(o))?.County ?? string.Empty),
                new PropertyByName<Order, Language>("BillingCity", async (o, l)  => (await orderBillingAddress(o))?.City ?? string.Empty),
                new PropertyByName<Order, Language>("BillingAddress1", async (o, l)  => (await orderBillingAddress(o))?.Address1 ?? string.Empty),
                new PropertyByName<Order, Language>("BillingAddress2", async (o, l)  => (await orderBillingAddress(o))?.Address2 ?? string.Empty),
                new PropertyByName<Order, Language>("BillingZipPostalCode", async (o, l)  => (await orderBillingAddress(o))?.ZipPostalCode ?? string.Empty),
                new PropertyByName<Order, Language>("BillingPhoneNumber", async (o, l)  => (await orderBillingAddress(o))?.PhoneNumber ?? string.Empty),
                new PropertyByName<Order, Language>("BillingFaxNumber", async (o, l)  => (await orderBillingAddress(o))?.FaxNumber ?? string.Empty),
            };

            return await ExportOrderItemsToXlsxAsync(properties, orders);
        }

        private async Task<byte[]> ExportOrderItemsToXlsxAsync(PropertyByName<Order, Language>[] properties, IEnumerable<Order> itemsToExport)
        {
            var orderItemProperties = new[]
            {
                new PropertyByName<OrderItem, Language>("Name", async (oi, l) => (await _productService.GetProductByIdAsync(oi.ProductId)).Name),
                new PropertyByName<OrderItem, Language>("AttributeDescription", (oi, l) => oi.AttributeDescription),
                new PropertyByName<OrderItem, Language>("Sku", async (oi, l) => await _productService.FormatSkuAsync(await _productService.GetProductByIdAsync(oi.ProductId), oi.AttributesXml)),
                new PropertyByName<OrderItem, Language>("PriceExclTax", (oi, l) => oi.UnitPriceExclTax),
                new PropertyByName<OrderItem, Language>("PriceInclTax", (oi, l) => oi.UnitPriceInclTax),
                new PropertyByName<OrderItem, Language>("Quantity", (oi, l) => oi.Quantity),
                new PropertyByName<OrderItem, Language>("DiscountExclTax", (oi, l) => oi.DiscountAmountExclTax),
                new PropertyByName<OrderItem, Language>("DiscountInclTax", (oi, l) => oi.DiscountAmountInclTax),
                new PropertyByName<OrderItem, Language>("TotalExclTax", (oi, l) => oi.PriceExclTax),
                new PropertyByName<OrderItem, Language>("TotalInclTax", (oi, l) => oi.PriceInclTax)
            };

            var orderItemsManager = new PropertyManager<OrderItem, Language>(orderItemProperties, _catalogSettings);

            await using var stream = new MemoryStream();
            // ok, we can run the real code of the sample now
            using (var workbook = new XLWorkbook())
            {
                // uncomment this line if you want the XML written out to the outputDir
                //xlPackage.DebugMode = true; 

                // get handles to the worksheets
                // Worksheet names cannot be more than 31 characters
                var worksheet = workbook.Worksheets.Add(typeof(Order).Name);
                var fpWorksheet = workbook.Worksheets.Add("DataForProductsFilters");
                fpWorksheet.Visibility = XLWorksheetVisibility.VeryHidden;

                //create Headers and format them 
                var manager = new PropertyManager<Order, Language>(properties, _catalogSettings);
                manager.WriteDefaultCaption(worksheet);

                var row = 2;
                foreach (var order in itemsToExport)
                {
                    manager.CurrentObject = order;
                    await manager.WriteDefaultToXlsxAsync(worksheet, row++);

                    //a vendor should have access only to his products
                    var vendor = await _workContext.GetCurrentVendorAsync();
                    var orderItems = await _orderService.GetOrderItemsAsync(order.Id, vendorId: vendor?.Id ?? 0);

                    if (!orderItems.Any())
                        continue;

                    orderItemsManager.WriteDefaultCaption(worksheet, row, 2);
                    worksheet.Row(row).OutlineLevel = 1;
                    worksheet.Row(row).Collapse();

                    foreach (var orderItem in orderItems)
                    {
                        row++;
                        orderItemsManager.CurrentObject = orderItem;
                        await orderItemsManager.WriteDefaultToXlsxAsync(worksheet, row, 2, fpWorksheet);
                        worksheet.Row(row).OutlineLevel = 1;
                        worksheet.Row(row).Collapse();
                    }

                    row++;
                }

                workbook.SaveAs(stream);
            }

            return stream.ToArray();
        }

        #endregion

        #region Import

        /// <summary>
        /// Get property list by excel cells
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="worksheet">Excel worksheet</param>
        /// <returns>Property list</returns>
        public static IList<PropertyByName<T, Language>> GetPropertiesByExcelCells<T>(IXLWorksheet worksheet)
        {
            var properties = new List<PropertyByName<T, Language>>();
            var poz = 1;
            while (true)
            {
                try
                {
                    var cell = worksheet.Row(1).Cell(poz);

                    if (string.IsNullOrEmpty(cell?.Value.ToString()))
                        break;

                    poz += 1;
                    properties.Add(new PropertyByName<T, Language>(cell.Value.ToString()));
                }
                catch
                {
                    break;
                }
            }

            return properties;
        }

        /// <summary>
        /// Import rates from XLSX file
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public virtual async Task ImportRatesFromXlsxAsync(Stream stream)
        {
            using var workbook = new XLWorkbook(stream);
            // get the first worksheet in the workbook
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet == null)
                throw new NopException("No worksheet found");

            //the columns
            var properties = GetPropertiesByExcelCells<ShippingManagerByWeightByTotal>(worksheet);

            var manager = new PropertyManager<ShippingManagerByWeightByTotal, Language>(properties, _catalogSettings);

            var iRow = 2;

            while (true)
            {
                var allColumnsAreEmpty = manager.GetDefaultProperties
                    .Select(property => worksheet.Row(iRow).Cell(property.PropertyOrderPosition))
                    .All(cell => cell?.Value == null || string.IsNullOrEmpty(cell.Value.ToString()));

                if (allColumnsAreEmpty)
                    break;

                try
                {
                    var shippingManagerByWeightByTotal = new ShippingManagerByWeightByTotal();

                    manager.ReadDefaultFromXlsx(worksheet, iRow);

                    bool isDelete = false;
                    bool isException = false;
                    foreach (var property in manager.GetDefaultProperties)
                    {
                        switch (property.PropertyName)
                        {
                            case "Id":
                                shippingManagerByWeightByTotal.Id = property.IntValue;
                                break;
                            case "Active":
                                shippingManagerByWeightByTotal.Active = property.BooleanValue;
                                break;
                            case "DisplayOrder":
                                shippingManagerByWeightByTotal.DisplayOrder = property.IntValue;
                                break;
                            case "Store":
                                var strStore = property.StringValue;
                                var store = (await _storeService.GetAllStoresAsync()).Where(v => v.Name == strStore).FirstOrDefault();
                                if (store != null)
                                    shippingManagerByWeightByTotal.StoreId = store.Id;
                                break;
                            case "Vendor":
                                var strVendor = property.StringValue;
                                var vendor = (await _vendorService.GetAllVendorsAsync()).Where(v => v.Name == strVendor).FirstOrDefault();
                                if (vendor != null)
                                    shippingManagerByWeightByTotal.VendorId = vendor.Id;
                                break;
                            case "Warehouse":
                                var strWarehouse = property.StringValue;
                                var warehouse = (await _vendorService.GetAllVendorsAsync()).Where(v => v.Name == strWarehouse).FirstOrDefault();
                                if (warehouse != null)
                                    shippingManagerByWeightByTotal.WarehouseId = warehouse.Id;
                                break;
                            case "Carrier":
                                var strCarrier = property.StringValue;
                                var carrier = (await _carrierService.GetAllCarriersAsync()).Where(v => v.Name == strCarrier).FirstOrDefault();
                                if (carrier != null)
                                    shippingManagerByWeightByTotal.CarrierId = carrier.Id;
                                break;
                            case "Country":
                                var strCountry = property.StringValue;
                                var country = (await _countryService.GetAllCountriesAsync()).Where(v => v.Name == strCountry).FirstOrDefault();
                                if (country != null)
                                    shippingManagerByWeightByTotal.CountryId = country.Id;
                                break;
                            case "StateProvince":
                                var strStateProvince = property.StringValue;
                                var stateProvince = (await _stateProvinceService.GetStateProvincesByCountryIdAsync(shippingManagerByWeightByTotal.CountryId)).Where(s => s.Name == strStateProvince).FirstOrDefault();
                                if (stateProvince != null)
                                    shippingManagerByWeightByTotal.StateProvinceId = stateProvince.Id;
                                break;

                            case "PostcodeZip":
                                var postcodeZip = property.IntValue;
                                break;
                            case "ShippingMethod":
                                var strShippingMethod = property.StringValue;
                                var shippingMethod = (await _shippingService.GetAllShippingMethodsAsync()).Where(s => s.Name == strShippingMethod).FirstOrDefault();
                                if (shippingMethod == null)
                                {
                                    shippingMethod = new ShippingMethod();
                                    shippingMethod.Name = strShippingMethod;
                                    shippingMethod.DisplayOrder = 1;
                                    await _shippingService.InsertShippingMethodAsync(shippingMethod);
                                }
                                shippingManagerByWeightByTotal.ShippingMethodId = shippingMethod.Id;
                                break;
                            case "WeightFrom":
                                shippingManagerByWeightByTotal.WeightFrom = property.DecimalValue;
                                break;
                            case "WeightTo":
                                shippingManagerByWeightByTotal.WeightTo = property.DecimalValue;
                                break;
                            case "CalculateCubicWeight":
                                shippingManagerByWeightByTotal.CalculateCubicWeight = property.BooleanValue;
                                break;
                            case "CubicWeightFactor":
                                shippingManagerByWeightByTotal.CubicWeightFactor = property.DecimalValue;
                                break;
                            case "OrderSubtotalFrom":
                                shippingManagerByWeightByTotal.OrderSubtotalFrom = property.DecimalValue;
                                break;
                            case "OrderSubtotalTo":
                                shippingManagerByWeightByTotal.OrderSubtotalTo = property.DecimalValue;
                                break;
                            case "AdditionalFixedCost":
                                shippingManagerByWeightByTotal.AdditionalFixedCost = property.DecimalValue;
                                break;
                            case "PercentageRateOfSubtotal":
                                shippingManagerByWeightByTotal.PercentageRateOfSubtotal = property.DecimalValue;
                                break;
                            case "RatePerWeightUnit":
                                shippingManagerByWeightByTotal.RatePerWeightUnit = property.DecimalValue;
                                break;
                            case "LowerWeightLimit":
                                shippingManagerByWeightByTotal.LowerWeightLimit = property.DecimalValue;
                                break;
                            case "CutOffTime":
                                var strCutOffTime = property.StringValue;
                                var cutOffTime = (await _carrierService.GetAllCutOffTimesAsync()).Where(v => v.Name == strCutOffTime).FirstOrDefault();
                                if (cutOffTime != null)
                                    shippingManagerByWeightByTotal.CutOffTimeId = cutOffTime.Id;
                                break;
                            case "FriendlyName":
                                shippingManagerByWeightByTotal.FriendlyName = property.StringValue;
                                break;
                            case "Description":
                                shippingManagerByWeightByTotal.Description = property.StringValue;
                                break;
                            case "SendFromAddress":
                                shippingManagerByWeightByTotal.SendFromAddressId = property.IntValue;
                                break;
                            case "TransitDays":
                                shippingManagerByWeightByTotal.TransitDays = property.IntValue;
                                break;
                            case "Delete":
                                isDelete = property.BooleanValue;
                                break;
                        }
                    }

                    if (shippingManagerByWeightByTotal.Id != 0)
                    {
                        var rate = await _shippingManagerService.GetByIdAsync(shippingManagerByWeightByTotal.Id);
                        if (rate == null)
                        {
                            var rateList = await _shippingManagerService.GetRecordsAsync(shippingManagerByWeightByTotal.ShippingMethodId,
                                shippingManagerByWeightByTotal.StoreId, shippingManagerByWeightByTotal.VendorId,
                                shippingManagerByWeightByTotal.WarehouseId, shippingManagerByWeightByTotal.CarrierId,
                                shippingManagerByWeightByTotal.CountryId, shippingManagerByWeightByTotal.StateProvinceId);

                            if (rateList != null && rateList.Count > 1)
                            {
                                isException = true;
                                _logger.InsertLog(LogLevel.Error, "Import rate: Multiple records found at row " + iRow);
                                iRow++;
                                continue;
                            }
                            else
                                rate = rateList.FirstOrDefault();
                        }

                        if (!isException && rate != null)
                        {
                            if (isDelete)
                            {
                                await _shippingManagerService.DeleteShippingByWeightRecordAsync(rate);
                            }
                            else
                            {
                                rate.Active = shippingManagerByWeightByTotal.Active;
                                rate.StoreId = shippingManagerByWeightByTotal.StoreId;
                                rate.VendorId = shippingManagerByWeightByTotal.VendorId;
                                rate.WarehouseId = shippingManagerByWeightByTotal.WarehouseId;
                                rate.CarrierId = shippingManagerByWeightByTotal.CarrierId;
                                rate.CountryId = shippingManagerByWeightByTotal.CountryId;
                                rate.StateProvinceId = shippingManagerByWeightByTotal.StateProvinceId;
                                rate.Zip = shippingManagerByWeightByTotal.Zip;
                                rate.ShippingMethodId = shippingManagerByWeightByTotal.ShippingMethodId;
                                rate.WeightFrom = shippingManagerByWeightByTotal.WeightFrom;
                                rate.WeightTo = shippingManagerByWeightByTotal.WeightTo;
                                rate.CalculateCubicWeight = shippingManagerByWeightByTotal.CalculateCubicWeight;
                                rate.CubicWeightFactor = shippingManagerByWeightByTotal.CubicWeightFactor;
                                rate.OrderSubtotalFrom = shippingManagerByWeightByTotal.OrderSubtotalFrom;
                                rate.OrderSubtotalTo = shippingManagerByWeightByTotal.OrderSubtotalTo;
                                rate.AdditionalFixedCost = shippingManagerByWeightByTotal.AdditionalFixedCost;
                                rate.PercentageRateOfSubtotal = shippingManagerByWeightByTotal.PercentageRateOfSubtotal;
                                rate.RatePerWeightUnit = shippingManagerByWeightByTotal.RatePerWeightUnit;
                                rate.LowerWeightLimit = shippingManagerByWeightByTotal.LowerWeightLimit;
                                rate.CutOffTimeId = shippingManagerByWeightByTotal.CutOffTimeId;
                                rate.FriendlyName = shippingManagerByWeightByTotal.FriendlyName;
                                rate.TransitDays = shippingManagerByWeightByTotal.TransitDays;
                                rate.SendFromAddressId = shippingManagerByWeightByTotal.SendFromAddressId;
                                rate.DisplayOrder = shippingManagerByWeightByTotal.DisplayOrder;
                                rate.Description = shippingManagerByWeightByTotal.Description;

                                await _shippingManagerService.UpdateShippingByWeightRecordAsync(rate);
                            }
                        }
                    }
                    else
                    {
                        await _shippingManagerService.InsertShippingByWeightRecordAsync(shippingManagerByWeightByTotal);
                    }

                    var newRate = (await _shippingManagerService.GetRecordsAsync(shippingManagerByWeightByTotal.ShippingMethodId,
                        shippingManagerByWeightByTotal.StoreId, shippingManagerByWeightByTotal.VendorId,
                        shippingManagerByWeightByTotal.WarehouseId, shippingManagerByWeightByTotal.CarrierId,
                        shippingManagerByWeightByTotal.CountryId, shippingManagerByWeightByTotal.StateProvinceId)).FirstOrDefault();

                    if (!isDelete && newRate == null)
                    {
                        _logger.InsertLog(LogLevel.Error, "Import Rate: Record not found" + " at row " + iRow);
                    }
                    else if (isDelete && newRate != null)
                    {
                        _logger.InsertLog(LogLevel.Error, "Import Rate: Deleted record found at row " + iRow);
                    }

                }
                catch (Exception ex)
                {
                    var error = ex;
                    await _logger.InsertLogAsync(LogLevel.Error, "Import : something wrong at row  " + iRow);
                }

                iRow++;

            }
        }

        #endregion

    }
}
