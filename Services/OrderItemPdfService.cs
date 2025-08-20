using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Shipping;
using System.Linq;
using System.Globalization;
using Nop.Core.Infrastructure;
using Nop.Services.Localization;
using Nop.Services.Helpers;
using Nop.Services.Media;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Stores;
using Nop.Core.Domain.Tax;
using Nop.Core.Domain.Vendors;
using Nop.Services.Attributes;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Html;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Stores;
using Nop.Services.Shipping;
using Nop.Services.Vendors;

using Nop.Plugin.Shipping.Manager.Settings;

namespace Nop.Plugin.Shipping.Manager.Services;

public class OrderItemPdfService : IOrderItemPdfService
{

    #region Fields

    protected readonly AddressSettings _addressSettings;
    protected readonly CatalogSettings _catalogSettings;
    protected readonly CurrencySettings _currencySettings;
    protected readonly ICurrencyService _currencyService;
    protected readonly IPriceFormatter _priceFormatter;
    protected readonly IProductService _productService;
    protected readonly VendorSettings _vendorSettings;
    protected readonly IVendorService _vendorService;
    protected readonly IDateTimeHelper _dateTimeHelper;
    protected readonly ILanguageService _languageService;
    protected readonly ILocalizationService _localizationService;
    protected readonly INopFileProvider _fileProvider;     
    protected readonly IPictureService _pictureService;     
    protected readonly ISettingService _settingService;
    protected readonly IStoreContext _storeContext;
    protected readonly IStoreService _storeService;      
    protected readonly IWorkContext _workContext;   
    protected readonly PdfSettings _pdfSettings;
    protected readonly TaxSettings _taxSettings;
    protected readonly IOrderService _orderService;
    protected readonly IPaymentService _paymentService;
    protected readonly IPaymentPluginManager _paymentPluginManager;
    protected readonly IAddressService _addressService;
    protected readonly IStateProvinceService _stateProvinceService;
    protected readonly IShipmentService _shipmentService;
    protected readonly ICountryService _countryService;
    protected readonly IAttributeFormatter<AddressAttribute, AddressAttributeValue> _addressAttributeFormatter;
    protected readonly ShippingManagerSettings _shippingManagerSettings;
    protected readonly IHtmlFormatter _htmlFormatter;
	protected readonly IGiftCardService _giftCardService;
        protected readonly IRewardPointService _rewardPointService;

    public static string CurrentReportName = "Report";

    #endregion

    #region Ctor

    public OrderItemPdfService(AddressSettings addressSettings,
        CatalogSettings catalogSettings,
        CurrencySettings currencySettings,
        ICurrencyService currencyService,
        IDateTimeHelper dateTimeHelper,
        ILanguageService languageService,
        ILocalizationService localizationService,
        IMeasureService measureService,
        INopFileProvider fileProvider,
        IOrderService orderService,
        IPaymentService paymentService,
        IPictureService pictureService,
        IPriceFormatter priceFormatter,
        IProductService productService,
        ISettingService settingService,
        IStoreContext storeContext,
        IStoreService storeService,
        IVendorService vendorService,
        IWorkContext workContext,
        MeasureSettings measureSettings,
        PdfSettings pdfSettings,
        TaxSettings taxSettings,
        VendorSettings vendorSettings,
        IPaymentPluginManager paymentPluginManager,
        IAddressService addressService,
        IStateProvinceService stateProvinceService,
        IShipmentService shipmentService,
        ICountryService countryService,
        IAttributeFormatter<AddressAttribute, AddressAttributeValue> addressAttributeFormatter,
        ShippingManagerSettings shippingManagerSettings,
        IHtmlFormatter htmlFormatter,
            IGiftCardService giftCardService,
            IRewardPointService rewardPointService)
    {
        _addressSettings = addressSettings;
        _catalogSettings = catalogSettings;
        _currencySettings = currencySettings;
        _currencyService = currencyService;
        _priceFormatter = priceFormatter;
        _productService = productService;
        _vendorSettings = vendorSettings;
        _vendorService = vendorService;
        _dateTimeHelper = dateTimeHelper;
        _languageService = languageService;
        _localizationService = localizationService;       
        _fileProvider = fileProvider;       
        _pictureService = pictureService;      
        _settingService = settingService;
        _storeContext = storeContext;
        _storeService = storeService;   
        _workContext = workContext;    
        _pdfSettings = pdfSettings;
        _taxSettings = taxSettings;
        _orderService =orderService;
        _paymentService = paymentService;
        _paymentPluginManager = paymentPluginManager;
        _addressService = addressService;
        _stateProvinceService = stateProvinceService;
        _shipmentService = shipmentService;
        _countryService = countryService;
        _addressAttributeFormatter = addressAttributeFormatter;
        _shippingManagerSettings = shippingManagerSettings;
        _htmlFormatter = htmlFormatter;
            _giftCardService = giftCardService;
            _rewardPointService = rewardPointService;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Sets up and applies page numbering
    /// </summary>
    /// <returns>Font</returns>
    public class PageHeaderFooter : PdfPageEventHelper
    {
        protected readonly Font _pageNumberFont = new Font(Font.HELVETICA, 8f, Font.NORMAL);

        public override void OnEndPage(PdfWriter writer, Document document)
        {
            AddPageNumber(writer, document);
        }

        private void AddPageNumber(PdfWriter writer, Document document)
        {

            var text = writer.PageNumber.ToString();

            var numberTable = new PdfPTable(2) { WidthPercentage = 100f };
            numberTable.SetTotalWidth(new float[] { 250, 250 });

            var textCell = new PdfPCell(new Phrase(CurrentReportName, _pageNumberFont))
            { HorizontalAlignment = Element.ALIGN_LEFT, Border = Rectangle.TOP_BORDER, BorderWidthTop = 0.5f };

            var numberCell = new PdfPCell(new Phrase(text, _pageNumberFont))
            { HorizontalAlignment = Element.ALIGN_RIGHT, Border = Rectangle.TOP_BORDER, BorderWidthTop = 0.5f };

            numberTable.AddCell(textCell);

            numberTable.AddCell(numberCell);

            numberTable.WriteSelectedRows(0, -1, document.LeftMargin, document.Bottom + 20, writer.DirectContent);
        }
    }

    /// <summary>
    /// Get font
    /// </summary>
    /// <returns>Font</returns>
    protected virtual Font GetFont()
    {
        //nopCommerce supports Unicode characters
        //nopCommerce uses Free Serif font by default (~/App_Data/Pdf/FreeSerif.ttf file)
        //It was downloaded from http://savannah.gnu.org/projects/freefont
        return GetFont(_shippingManagerSettings.FontFileName);
    }

    /// <summary>
    /// Get font
    /// </summary>
    /// <param name="fontFileName">Font file name</param>
    /// <returns>Font</returns>
    protected virtual Font GetFont(string fontFileName)
    {
            if (fontFileName == null)
            {
                //_shippingManagerSettings.fontfilename	Calibri Regular.ttf
                fontFileName = _pdfSettings.FontFamily;
            }

            try
            {
                var fontPath = _fileProvider.Combine(_fileProvider.MapPath("~/Plugins/SSI.Shipping.Manager/Content/Fonts/"), fontFileName);
                var baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                var font = new Font(baseFont, 10, Font.NORMAL);
                return font;
            }
            catch
            {
                try
                {
                    var fontPath = _fileProvider.Combine(_fileProvider.MapPath("~/App_Data/Pdf/"), fontFileName);
                    var baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                    var font = new Font(baseFont, 10, Font.NORMAL);
                    return font;
                }
                catch
                {
                    var fontPath = _fileProvider.Combine(_fileProvider.MapPath("~/App_Data/Pdf/"), "FreeSerif.ttf");
                    var baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                    var font = new Font(baseFont, 10, Font.NORMAL);
                    return font;
                }
            }
        }


    /// <summary>
    /// Get font direction
    /// </summary>
    /// <param name="lang">Language</param>
    /// <returns>Font direction</returns>
    protected virtual int GetDirection(Language lang)
    {
        return lang.Rtl ? PdfWriter.RUN_DIRECTION_RTL : PdfWriter.RUN_DIRECTION_LTR;
    }

    /// <summary>
    /// Get element alignment
    /// </summary>
    /// <param name="lang">Language</param>
    /// <param name="isOpposite">Is opposite?</param>
    /// <returns>Element alignment</returns>
    protected virtual int GetAlignment(Language lang, bool isOpposite = false)
    {
        //if we need the element to be opposite, like logo etc`.
        if (!isOpposite)
            return lang.Rtl ? Element.ALIGN_RIGHT : Element.ALIGN_LEFT;

        return lang.Rtl ? Element.ALIGN_LEFT : Element.ALIGN_RIGHT;
    }

    /// <summary>
    /// Get PDF cell
    /// </summary>
    /// <param name="resourceKey">Locale</param>
    /// <param name="lang">Language</param>
    /// <param name="font">Font</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the pDF cell
    /// </returns>
    protected virtual async Task<PdfPCell> GetPdfCellAsync(string resourceKey, Language lang, Font font)
    {
        return new PdfPCell(new Phrase(await _localizationService.GetResourceAsync(resourceKey, lang.Id), font));
    }

    /// <summary>
    /// Get PDF cell
    /// </summary>
    /// <param name="text">Text</param>
    /// <param name="font">Font</param>
    /// <returns>PDF cell</returns>
    protected virtual PdfPCell GetPdfCell(object text, Font font)
    {
        return new PdfPCell(new Phrase(text.ToString(), font));
    }

    /// <summary>
    /// Get paragraph
    /// </summary>
    /// <param name="resourceKey">Locale</param>
    /// <param name="lang">Language</param>
    /// <param name="font">Font</param>
    /// <param name="args">Locale arguments</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the paragraph
    /// </returns>
    protected virtual async Task<Paragraph> GetParagraphAsync(string resourceKey, Language lang, Font font, params object[] args)
    {
        return await GetParagraphAsync(resourceKey, string.Empty, lang, font, args);
    }

        /// <summary>
        /// Get paragraph
        /// </summary>
        /// <param name="resourceKey">Locale</param>
        /// <param name="indent">Indent</param>
        /// <param name="lang">Language</param>
        /// <param name="font">Font</param>
        /// <param name="args">Locale arguments</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the paragraph
        /// </returns>
        protected virtual async Task<Paragraph> GetParagraphAsync(string resourceKey, string indent, Language lang, Font font, params object[] args)
        {
            var formatText = await _localizationService.GetResourceAsync(resourceKey, lang.Id);
            if (string.IsNullOrEmpty(formatText))
                formatText = resourceKey;
            return new Paragraph(indent + (args.Any() ? string.Format(formatText, args) : formatText), font);
        }

    #endregion

    #region Sales orders

    /// <summary>
    /// Print sales orders to PDF
    /// </summary>
    /// <param name="stream">Stream</param>
    /// <param name="orderItems">Order items</param>
    /// <param name="languageId">Language identifier; 0 to use a language used when placing an order</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual async Task PrintOrdersToPdfAsync(Stream stream, IList<OrderItem> orderItems, int languageId = 0, int vendorId = 0)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        if (orderItems == null)
            throw new ArgumentNullException(nameof(orderItems));

        var pageSize = PageSize.A4;

        if (_pdfSettings.LetterPageSizeEnabled)
        {
            pageSize = PageSize.Letter;
        }

        var doc = new Document(pageSize);
        var pdfWriter = PdfWriter.GetInstance(doc, stream);
        doc.Open();

        CurrentReportName = "Orders Report";

        pdfWriter.PageEvent = new PageHeaderFooter();

        //fonts
        var titleFont = GetFont();
        titleFont.SetStyle(Font.BOLD);
        titleFont.Color = BaseColor.Black;
        var font = GetFont();
        var attributesFont = GetFont();
        attributesFont.SetStyle(Font.ITALIC);

        // var ordCount = orderItems.Count;
        var lang = await _languageService.GetLanguageByIdAsync(languageId == 0 ? 1 : languageId);

        if (lang == null || !lang.Published)
            lang = await _workContext.GetWorkingLanguageAsync();

        await PrintOrdersHeaderAsync(lang, titleFont, doc, font, attributesFont);

        var productsHeader = new PdfPTable(1)
        {
            RunDirection = GetDirection(lang),
            WidthPercentage = 100f
        };

        var count = 5;

        var productsTable = new PdfPTable(count)
        {
            RunDirection = GetDirection(lang),
            WidthPercentage = 100f
        };

        var widths = new Dictionary<int, int[]>
        {
            { 4, new[] { 50, 20, 10, 20 } },
            { 5, new[] { 15, 40, 10, 20, 15 } },
            { 6, new[] { 40, 13, 13, 12, 10, 12 } },
            { 7, new[] { 40, 13, 13, 12, 10, 12 } }
        };

        productsTable.SetWidths(lang.Rtl ? widths[count].Reverse().ToArray() : widths[count]);

        //Customer

        var cellProductItem = await GetPdfCellAsync("Plugins.Shipping.Manager.Admin.PdfReport.Customer", lang, font);
        cellProductItem.BackgroundColor = BaseColor.LightGray;
        cellProductItem.HorizontalAlignment = Element.ALIGN_CENTER;
        productsTable.AddCell(cellProductItem);

        //Order Details

        cellProductItem = await GetPdfCellAsync("Plugins.Shipping.Manager.Admin.PdfReport.OrderDetails", lang, font);
        cellProductItem.BackgroundColor = BaseColor.LightGray;
        cellProductItem.HorizontalAlignment = Element.ALIGN_CENTER;
        productsTable.AddCell(cellProductItem);

        //Payment 

        cellProductItem = await GetPdfCellAsync("Plugins.Shipping.Manager.Admin.PdfReport.Payment", lang, font);
        cellProductItem.BackgroundColor = BaseColor.LightGray;
        cellProductItem.HorizontalAlignment = Element.ALIGN_CENTER;
        productsTable.AddCell(cellProductItem);

        // Paid Date

        cellProductItem = await GetPdfCellAsync("Plugins.Shipping.Manager.Admin.PdfReport.PaidDate", lang, font);
        cellProductItem.BackgroundColor = BaseColor.LightGray;
        cellProductItem.HorizontalAlignment = Element.ALIGN_CENTER;
        productsTable.AddCell(cellProductItem);

        // Payment Method

        cellProductItem = await GetPdfCellAsync("Plugins.Shipping.Manager.Admin.PdfReport.Method", lang, font);
        cellProductItem.BackgroundColor = BaseColor.LightGray;
        cellProductItem.HorizontalAlignment = Element.ALIGN_CENTER;
        productsTable.AddCell(cellProductItem);

        foreach (var orderItem in orderItems)
        {
            var order = await _orderService.GetOrderByIdAsync(orderItem.OrderId);
            var pdfSettingsByStore = await _settingService.LoadSettingAsync<PdfSettings>(order.StoreId);
           
            var p = await _productService.GetProductByIdAsync(orderItem.ProductId);

            // a vendor should have access only to his products
            if (vendorId > 0 && p.VendorId != vendorId)
                continue;

            // Customer
            var billingAddress = await _addressService.GetAddressByIdAsync(order.BillingAddressId);
            var customerId = billingAddress.FirstName + billingAddress.LastName;
            cellProductItem = GetPdfCell(customerId, font);

            cellProductItem.HorizontalAlignment = Element.ALIGN_CENTER;
            productsTable.AddCell(cellProductItem);            
           
            var pAttribTable = new PdfPTable(1) { RunDirection = GetDirection(lang) };
            pAttribTable.DefaultCell.Border = Rectangle.NO_BORDER;

            // Order Details
            var getParentProduct = await _productService.GetProductByIdAsync(p.ParentGroupedProductId);
            if(getParentProduct != null)
            {
                pAttribTable.AddCell(new Paragraph(getParentProduct.Name, font));
                cellProductItem.AddElement(new Paragraph(getParentProduct.Name, font));
            }
            
            var name = await _localizationService.GetLocalizedAsync(p, x => x.Name, lang.Id);
            pAttribTable.AddCell(new Paragraph(name, font));
            cellProductItem.AddElement(new Paragraph(name, font));
            // Product Attributes
            if (!string.IsNullOrEmpty(orderItem.AttributeDescription))
            {
                var attributesParagraph =
                    new Paragraph(_htmlFormatter.ConvertHtmlToPlainText(orderItem.AttributeDescription, true, true),
                        attributesFont);
                pAttribTable.AddCell(attributesParagraph);
            }
            productsTable.AddCell(pAttribTable);

            // Payment Status                
            var paymentStatus= await _localizationService.GetLocalizedEnumAsync(order.PaymentStatus);
            cellProductItem = GetPdfCell(paymentStatus, font);
            cellProductItem.HorizontalAlignment = Element.ALIGN_LEFT;
            productsTable.AddCell(cellProductItem);

            //Paid Date
            var paidDateUtc = "Not Paid";
            if (order.PaidDateUtc != null)
            {
                //Payment Status
                paidDateUtc = order.PaidDateUtc.ToString();
            }

            cellProductItem = GetPdfCell(paidDateUtc, font);
            cellProductItem.HorizontalAlignment = Element.ALIGN_LEFT;
            productsTable.AddCell(cellProductItem);

            //Payment Method
            var paymentMethod = await _paymentPluginManager.LoadPluginBySystemNameAsync(order.PaymentMethodSystemName);
            var paymentMethodFriendlyName = paymentMethod != null ? 
                await _localizationService.GetLocalizedFriendlyNameAsync(paymentMethod, 
                (await _workContext.GetWorkingLanguageAsync()).Id) : (order.PaymentMethodSystemName==null ? "" : order.PaymentMethodSystemName);

            cellProductItem = GetPdfCell(paymentMethodFriendlyName, font);
            cellProductItem.HorizontalAlignment = Element.ALIGN_LEFT;
            productsTable.AddCell(cellProductItem);

        }
        doc.Add(productsTable);
        
        doc.NewPage();
        doc.Close();
    }

    /// <summary>
    /// Print products
    /// </summary>
    /// <param name="lang">Language</param>
    /// <param name="order">Order</param>
    /// <param name="font">Text font</param>
    /// <param name="titleFont">Title font</param>
    /// <param name="doc">Document</param>
    protected virtual async Task PrintOrdersHeaderAsync(Language lang, Font titleFont, Document doc, Font font, Font attributesFont)
    {
        var productsHeader = new PdfPTable(1)
        {
            RunDirection = GetDirection(lang),
            WidthPercentage = 100f
        };

        var count = 3;

        var HeaderTable = new PdfPTable(count)
        {
            RunDirection = GetDirection(lang),
            WidthPercentage = 100f
        };

        var widths = new Dictionary<int, int[]>
        {
            { 3, new[] { 50, 45, 30 } }

        };

        HeaderTable.SetWidths(lang.Rtl ? widths[count].Reverse().ToArray() : widths[count]);

        // Report Name

        var cellProductItem = await GetPdfCellAsync("Plugins.Shipping.Manager.Admin.PdfReport", lang, titleFont);
        //cellProductItem.BackgroundColor = BaseColor.LightGray;
        //cellProductItem.HorizontalAlignment = Element.ALIGN_CENTER;
        cellProductItem.Border = Rectangle.NO_BORDER;
        HeaderTable.AddCell(cellProductItem);

        //SKU       

        cellProductItem = await GetPdfCellAsync("Plugins.Shipping.Manager.Admin.PdfReport.Category", lang, titleFont);
        //cellProductItem.BackgroundColor = BaseColor.LightGray;
        //cellProductItem.HorizontalAlignment = Element.ALIGN_CENTER;
        cellProductItem.Border = Rectangle.NO_BORDER;
        HeaderTable.AddCell(cellProductItem);


        //Vendor name
        var datestring = "Date: " + DateTime.UtcNow;
        cellProductItem = GetPdfCell(datestring, titleFont);
        cellProductItem.Border = Rectangle.NO_BORDER;
        HeaderTable.AddCell(cellProductItem);


        doc.Add(HeaderTable);
        doc.Add(new Paragraph(" "));

    }

    #endregion

    #region Shipping Reports

    /// <summary>
    /// Print packaging report to PDF
    /// </summary>
    /// <param name="stream">Stream</param>
    /// <param name="shipments">Shipments</param>
    /// <param name="languageId">Language identifier; 0 to use a language used when placing an order</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual async Task PrintPackagingReportToPdfAsync(Stream stream, IList<Shipment> shipments, int languageId = 0)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        if (shipments == null)
            throw new ArgumentNullException(nameof(shipments));

        var store = await _storeContext.GetCurrentStoreAsync();

        //by default _pdfSettings contains settings for the current active store
        //and we need PdfSettings for the store which was used to place an order
        //so let's load it based on a store of the current order
        var pdfSettingsByStore = await _settingService.LoadSettingAsync<PdfSettings>(store.Id);

        var pageSize = PageSize.A4;

        if (pdfSettingsByStore.LetterPageSizeEnabled)
        {
            pageSize = PageSize.Letter;
        }

        var doc = new Document(pageSize);
        var pdfWriter = PdfWriter.GetInstance(doc, stream);
        doc.Open();

        CurrentReportName = "Packing Report";

        pdfWriter.PageEvent = new PageHeaderFooter();

        //fonts
        var titleFont = GetFont();
        titleFont.SetStyle(Font.BOLD);
        titleFont.Color = BaseColor.Black;
        var font = GetFont();
        var attributesFont = GetFont();
        attributesFont.SetStyle(Font.ITALIC);

        var lang = await _workContext.GetWorkingLanguageAsync();

        await PrintHeader(pdfSettingsByStore, lang, font, titleFont, doc, store);

        var shipmentTotals = new Order();

        //header
        bool first = true;
        foreach (var shipment in shipments)
        {

            var order = await _orderService.GetOrderByIdAsync(shipment.OrderId);
            if (order != null)
            { 

                lang = await _languageService.GetLanguageByIdAsync(languageId == 0 ? order.CustomerLanguageId : languageId);
                if (lang == null || !lang.Published)
                    lang = await _workContext.GetWorkingLanguageAsync();

                var addressTable = new PdfPTable(1);
                if (lang.Rtl)
                    addressTable.RunDirection = PdfWriter.RUN_DIRECTION_RTL;
                addressTable.DefaultCell.Border = Rectangle.NO_BORDER;
                addressTable.WidthPercentage = 100f;

                string formatText = string.Format(await _localizationService.GetResourceAsync("Admin.Orders.Shipments.Id", lang.Id), shipment.Id) + " : " +
                     string.Format(await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Admin.PdfReport.Order", lang.Id),
                        order.CustomOrderNumber, await _priceFormatter.FormatPriceAsync(order.OrderShippingInclTax, false, false));

                addressTable.AddCell(new Paragraph(formatText, titleFont));

                if (!order.PickupInStore)
                {
                    if (first)
                    {
                        // use first order values
                        shipmentTotals.CustomerCurrencyCode = order.CustomerCurrencyCode;
                        shipmentTotals.CustomerTaxDisplayType = order.CustomerTaxDisplayType;
                        shipmentTotals.CurrencyRate = order.CurrencyRate;
                        shipmentTotals.TaxRates = order.TaxRates;
                        first = false;
                    }

                    // Running totals
                    shipmentTotals.OrderShippingExclTax += order.OrderShippingExclTax;
                    shipmentTotals.OrderShippingInclTax += order.OrderShippingInclTax;
                    shipmentTotals.OrderTotal += order.OrderTotal;

                    if (order.ShippingAddressId == null || await _addressService.GetAddressByIdAsync(order.ShippingAddressId.Value) is not Address shippingAddress)
                        throw new NopException($"Shipping is required, but address is not available. Order ID = {order.Id}");

                    if (_addressSettings.CompanyEnabled && !string.IsNullOrEmpty(shippingAddress.Company))
                        addressTable.AddCell(await GetParagraphAsync("Admin.Customers.Customers.Fields.Company", lang, font, shippingAddress.Company));

                    string contact = shippingAddress.FirstName + " " + shippingAddress.LastName;

                    if (_addressSettings.PhoneEnabled)
                        contact += " " + shippingAddress.PhoneNumber;

                    if (_addressSettings.StreetAddressEnabled)
                        contact += " " + shippingAddress.Address1 + " " + shippingAddress.Address2;

                    if (_addressSettings.CityEnabled || _addressSettings.StateProvinceEnabled ||
                        _addressSettings.CountyEnabled || _addressSettings.ZipPostalCodeEnabled)
                    {
                        var addressLine = $"{shippingAddress.City}, " +
                            $"{(!string.IsNullOrEmpty(shippingAddress.County) ? $"{shippingAddress.County}, " : string.Empty)}" +
                            $"{(await _stateProvinceService.GetStateProvinceByAddressAsync(shippingAddress) is StateProvince stateProvince ? await _localizationService.GetLocalizedAsync(stateProvince, x => x.Name, lang.Id) : string.Empty)} " +
                            $"{shippingAddress.ZipPostalCode}";
                        contact += " " + addressLine;
                    }

                    if (_addressSettings.CountryEnabled && await _countryService.GetCountryByAddressAsync(shippingAddress) is Country country)
                        contact += " " + await _localizationService.GetLocalizedAsync(country, x => x.Name, lang.Id);

                    addressTable.AddCell(await GetParagraphAsync("Plugins.Shipping.Manager.Admin.PdfReport.ShipmentName", lang, font, contact));

                    //custom attributes
                    var customShippingAddressAttributes = await _addressAttributeFormatter.FormatAttributesAsync(shippingAddress.CustomAttributes);
                    if (!string.IsNullOrEmpty(customShippingAddressAttributes))
                    {
                        addressTable.AddCell(new Paragraph(_htmlFormatter.ConvertHtmlToPlainText(customShippingAddressAttributes, true, true), font));
                    }
                }
                else if (order.PickupAddressId.HasValue && await _addressService.GetAddressByIdAsync(order.PickupAddressId.Value) is Address pickupAddress)
                {
                    addressTable.AddCell(new Paragraph(await _localizationService.GetResourceAsync("Admin.Orders.Shipments.PickupInStore", lang.Id), titleFont));

                    string contact = pickupAddress.Address1;

                    if (!string.IsNullOrEmpty(pickupAddress.City))
                        contact += " " + pickupAddress.City;

                    if (!string.IsNullOrEmpty(pickupAddress.County))
                        contact += " " + pickupAddress.County;

                    if (await _countryService.GetCountryByAddressAsync(pickupAddress) is Country country)
                        contact += " " + await _localizationService.GetLocalizedAsync(country, x => x.Name, lang.Id);

                    if (!string.IsNullOrEmpty(pickupAddress.ZipPostalCode))
                        contact += " " + pickupAddress.ZipPostalCode;

                    addressTable.AddCell(new Paragraph(" "));
                }

                addressTable.AddCell(await GetParagraphAsync("Plugins.Shipping.Manager.Admin.PdfReport.ShipmentMethod", lang, font, order.ShippingMethod));
                addressTable.AddCell(new Paragraph(" "));
                doc.Add(addressTable);

                var productsTable = new PdfPTable(3) { WidthPercentage = 100f };
                if (lang.Rtl)
                {
                    productsTable.RunDirection = PdfWriter.RUN_DIRECTION_RTL;
                    productsTable.SetWidths(new[] { 20, 20, 60 });
                }
                else
                {
                    productsTable.SetWidths(new[] { 60, 20, 20 });
                }

                //product name
                var cell = await GetPdfCellAsync("Admin.Orders.Shipments.Products", lang, font);
                cell.BackgroundColor = BaseColor.LightGray;
                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                productsTable.AddCell(cell);

                //SKU
                cell = await GetPdfCellAsync("Admin.Orders.Shipments.Products.Sku", lang, font);
                cell.BackgroundColor = BaseColor.LightGray;
                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                productsTable.AddCell(cell);

                //qty
                cell = await GetPdfCellAsync("Admin.Orders.Shipments.Products.QtyToPickup", lang, font);
                cell.BackgroundColor = BaseColor.LightGray;
                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                productsTable.AddCell(cell);

                foreach (var si in await _shipmentService.GetShipmentItemsByShipmentIdAsync(shipment.Id))
                {
                    var productAttribTable = new PdfPTable(1);
                    if (lang.Rtl)
                        productAttribTable.RunDirection = PdfWriter.RUN_DIRECTION_RTL;
                    productAttribTable.DefaultCell.Border = Rectangle.NO_BORDER;

                    //product name
                    var orderItem = await _orderService.GetOrderItemByIdAsync(si.OrderItemId);
                    if (orderItem == null)
                        continue;

                    var product = await _productService.GetProductByIdAsync(orderItem.ProductId);

                    var name = await _localizationService.GetLocalizedAsync(product, x => x.Name, lang.Id);
                    productAttribTable.AddCell(new Paragraph(name, font));
                    //attributes
                    if (!string.IsNullOrEmpty(orderItem.AttributeDescription))
                    {
                        var attributesParagraph = new Paragraph(_htmlFormatter.ConvertHtmlToPlainText(orderItem.AttributeDescription, true, true), attributesFont);
                        productAttribTable.AddCell(attributesParagraph);
                    }

                    //rental info
                    if (product.IsRental)
                    {
                        var rentalStartDate = orderItem.RentalStartDateUtc.HasValue
                            ? _productService.FormatRentalDate(product, orderItem.RentalStartDateUtc.Value) : string.Empty;
                        var rentalEndDate = orderItem.RentalEndDateUtc.HasValue
                            ? _productService.FormatRentalDate(product, orderItem.RentalEndDateUtc.Value) : string.Empty;
                        var rentalInfo = string.Format(await _localizationService.GetResourceAsync("Order.Rental.FormattedDate"),
                            rentalStartDate, rentalEndDate);

                        var rentalInfoParagraph = new Paragraph(rentalInfo, attributesFont);
                        productAttribTable.AddCell(rentalInfoParagraph);
                    }

                    productsTable.AddCell(productAttribTable);

                    //SKU
                    var sku = await _productService.FormatSkuAsync(product, orderItem.AttributesXml);
                    cell = GetPdfCell(sku ?? string.Empty, font);
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    productsTable.AddCell(cell);

                    //qty
                    cell = GetPdfCell(si.Quantity, font);
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    productsTable.AddCell(cell);
                }

                doc.Add(productsTable);

                doc.Add(new Phrase(Environment.NewLine));

            }
        }

        //totals
        await PrintTotalsAsync(pdfSettingsByStore, lang, shipmentTotals, font, titleFont, doc);

        doc.Close();
    }

        /// <summary>
        /// Print packaging slips to PDF
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="shipments">Shipments</param>
        /// <param name="languageId">Language identifier; 0 to use a language used when placing an order</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public virtual async Task PrintPackagingSlipsToPdfAsync(Stream stream, IList<Shipment> shipments, int languageId = 0)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (shipments == null)
                throw new ArgumentNullException(nameof(shipments));

            var pageSize = PageSize.A4;

            if (_pdfSettings.LetterPageSizeEnabled)
            {
                pageSize = PageSize.Letter;
            }

            var doc = new Document(pageSize);
            PdfWriter.GetInstance(doc, stream);
            doc.Open();

            //fonts
            var titleFont = GetFont();
            titleFont.SetStyle(Font.BOLD);
            titleFont.Color = BaseColor.Black;
            var font = GetFont();
            var attributesFont = GetFont();
            attributesFont.SetStyle(Font.ITALIC);

            var shipmentCount = shipments.Count;
            var shipmentNum = 0;

            foreach (var shipment in shipments)
            {
                var order = await _orderService.GetOrderByIdAsync(shipment.OrderId);

                var lang = await _languageService.GetLanguageByIdAsync(languageId == 0 ? order.CustomerLanguageId : languageId);
                if (lang == null || !lang.Published)
                    lang = await _workContext.GetWorkingLanguageAsync();

                var addressTable = new PdfPTable(1);
                if (lang.Rtl)
                    addressTable.RunDirection = PdfWriter.RUN_DIRECTION_RTL;
                addressTable.DefaultCell.Border = Rectangle.NO_BORDER;
                addressTable.WidthPercentage = 100f;

                addressTable.AddCell(await GetParagraphAsync("PDFPackagingSlip.Shipment", lang, titleFont, shipment.Id));
                addressTable.AddCell(await GetParagraphAsync("PDFPackagingSlip.Order", lang, titleFont, order.CustomOrderNumber));

                if (!order.PickupInStore)
                {
                    if (order.ShippingAddressId == null || await _addressService.GetAddressByIdAsync(order.ShippingAddressId.Value) is not Address shippingAddress)
                        throw new NopException($"Shipping is required, but address is not available. Order ID = {order.Id}");

                    if (_addressSettings.CompanyEnabled && !string.IsNullOrEmpty(shippingAddress.Company))
                        addressTable.AddCell(await GetParagraphAsync("PDFPackagingSlip.Company", lang, font, shippingAddress.Company));

                    addressTable.AddCell(await GetParagraphAsync("PDFPackagingSlip.Name", lang, font, shippingAddress.FirstName + " " + shippingAddress.LastName));
                    addressTable.AddCell(await GetParagraphAsync("PDFPackagingSlip.Email", lang, font, shippingAddress.Email));
                    if (_addressSettings.PhoneEnabled)
                        addressTable.AddCell(await GetParagraphAsync("PDFPackagingSlip.Phone", lang, font, shippingAddress.PhoneNumber));
                    if (_addressSettings.StreetAddressEnabled)
                        addressTable.AddCell(await GetParagraphAsync("PDFPackagingSlip.Address", lang, font, shippingAddress.Address1));

                    if (_addressSettings.StreetAddress2Enabled && !string.IsNullOrEmpty(shippingAddress.Address2))
                        addressTable.AddCell(await GetParagraphAsync("PDFPackagingSlip.Address2", lang, font, shippingAddress.Address2));

                    if (_addressSettings.CityEnabled || _addressSettings.StateProvinceEnabled ||
                        _addressSettings.CountyEnabled || _addressSettings.ZipPostalCodeEnabled)
                    {
                        var addressLine = $"{shippingAddress.City}, " +
                            $"{(!string.IsNullOrEmpty(shippingAddress.County) ? $"{shippingAddress.County}, " : string.Empty)}" +
                            $"{(await _stateProvinceService.GetStateProvinceByAddressAsync(shippingAddress) is StateProvince stateProvince ? await _localizationService.GetLocalizedAsync(stateProvince, x => x.Name, lang.Id) : string.Empty)} " +
                            $"{shippingAddress.ZipPostalCode}";
                        addressTable.AddCell(new Paragraph(addressLine, font));
                    }

                    if (_addressSettings.CountryEnabled && await _countryService.GetCountryByAddressAsync(shippingAddress) is Country country)
                        addressTable.AddCell(new Paragraph(await _localizationService.GetLocalizedAsync(country, x => x.Name, lang.Id), font));

                    //custom attributes
                    var customShippingAddressAttributes = await _addressAttributeFormatter.FormatAttributesAsync(shippingAddress.CustomAttributes);
                    if (!string.IsNullOrEmpty(customShippingAddressAttributes))
                    {
                        addressTable.AddCell(new Paragraph(_htmlFormatter.ConvertHtmlToPlainText(customShippingAddressAttributes, true, true), font));
                    }
                }
                else
                    if (order.PickupAddressId.HasValue && await _addressService.GetAddressByIdAsync(order.PickupAddressId.Value) is Address pickupAddress)
                {
                    addressTable.AddCell(new Paragraph(await _localizationService.GetResourceAsync("PDFInvoice.Pickup", lang.Id), titleFont));

                    if (!string.IsNullOrEmpty(pickupAddress.Address1))
                        addressTable.AddCell(new Paragraph($"   {string.Format(await _localizationService.GetResourceAsync("PDFInvoice.Address", lang.Id), pickupAddress.Address1)}", font));

                    if (!string.IsNullOrEmpty(pickupAddress.City))
                        addressTable.AddCell(new Paragraph($"   {pickupAddress.City}", font));

                    if (!string.IsNullOrEmpty(pickupAddress.County))
                        addressTable.AddCell(new Paragraph($"   {pickupAddress.County}", font));

                    if (await _countryService.GetCountryByAddressAsync(pickupAddress) is Country country)
                        addressTable.AddCell(new Paragraph($"   {await _localizationService.GetLocalizedAsync(country, x => x.Name, lang.Id)}", font));

                    if (!string.IsNullOrEmpty(pickupAddress.ZipPostalCode))
                        addressTable.AddCell(new Paragraph($"   {pickupAddress.ZipPostalCode}", font));

                    addressTable.AddCell(new Paragraph(" "));
                }

                addressTable.AddCell(new Paragraph(" "));

                addressTable.AddCell(await GetParagraphAsync("PDFPackagingSlip.ShippingMethod", lang, font, order.ShippingMethod));
                addressTable.AddCell(new Paragraph(" "));
                doc.Add(addressTable);

                var productsTable = new PdfPTable(3) { WidthPercentage = 100f };
                if (lang.Rtl)
                {
                    productsTable.RunDirection = PdfWriter.RUN_DIRECTION_RTL;
                    productsTable.SetWidths(new[] { 20, 20, 60 });
                }
                else
                {
                    productsTable.SetWidths(new[] { 60, 20, 20 });
                }

                //product name
                var cell = await GetPdfCellAsync("PDFPackagingSlip.ProductName", lang, font);
                cell.BackgroundColor = BaseColor.LightGray;
                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                productsTable.AddCell(cell);

                //SKU
                cell = await GetPdfCellAsync("PDFPackagingSlip.SKU", lang, font);
                cell.BackgroundColor = BaseColor.LightGray;
                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                productsTable.AddCell(cell);

                //qty
                cell = await GetPdfCellAsync("PDFPackagingSlip.QTY", lang, font);
                cell.BackgroundColor = BaseColor.LightGray;
                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                productsTable.AddCell(cell);

                foreach (var si in await _shipmentService.GetShipmentItemsByShipmentIdAsync(shipment.Id))
                {
                    var productAttribTable = new PdfPTable(1);
                    if (lang.Rtl)
                        productAttribTable.RunDirection = PdfWriter.RUN_DIRECTION_RTL;
                    productAttribTable.DefaultCell.Border = Rectangle.NO_BORDER;

                    //product name
                    var orderItem = await _orderService.GetOrderItemByIdAsync(si.OrderItemId);
                    if (orderItem == null)
                        continue;

                    var product = await _productService.GetProductByIdAsync(orderItem.ProductId);

                    var name = await _localizationService.GetLocalizedAsync(product, x => x.Name, lang.Id);
                    productAttribTable.AddCell(new Paragraph(name, font));
                    //attributes
                    if (!string.IsNullOrEmpty(orderItem.AttributeDescription))
                    {
                        var attributesParagraph = new Paragraph(_htmlFormatter.ConvertHtmlToPlainText(orderItem.AttributeDescription, true, true), attributesFont);
                        productAttribTable.AddCell(attributesParagraph);
                    }

                    //rental info
                    if (product.IsRental)
                    {
                        var rentalStartDate = orderItem.RentalStartDateUtc.HasValue
                            ? _productService.FormatRentalDate(product, orderItem.RentalStartDateUtc.Value) : string.Empty;
                        var rentalEndDate = orderItem.RentalEndDateUtc.HasValue
                            ? _productService.FormatRentalDate(product, orderItem.RentalEndDateUtc.Value) : string.Empty;
                        var rentalInfo = string.Format(await _localizationService.GetResourceAsync("Order.Rental.FormattedDate"),
                            rentalStartDate, rentalEndDate);

                        var rentalInfoParagraph = new Paragraph(rentalInfo, attributesFont);
                        productAttribTable.AddCell(rentalInfoParagraph);
                    }

                    productsTable.AddCell(productAttribTable);

                    //SKU
                    var sku = await _productService.FormatSkuAsync(product, orderItem.AttributesXml);
                    cell = GetPdfCell(sku ?? string.Empty, font);
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    productsTable.AddCell(cell);

                    //qty
                    cell = GetPdfCell(si.Quantity, font);
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    productsTable.AddCell(cell);
                }

                doc.Add(productsTable);

                shipmentNum++;
                if (shipmentNum < shipmentCount)
                    doc.NewPage();
            }

            doc.Close();
        }

        /// <summary>
        /// Print footer
        /// </summary>
        /// <param name="pdfSettingsByStore">PDF settings</param>
        /// <param name="pdfWriter">PDF writer</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="lang">Language</param>
        /// <param name="font">Font</param>
        protected virtual void PrintFooter(PdfSettings pdfSettingsByStore, PdfWriter pdfWriter, Rectangle pageSize, Language lang, Font font)
        {
            if (string.IsNullOrEmpty(pdfSettingsByStore.InvoiceFooterTextColumn1) && string.IsNullOrEmpty(pdfSettingsByStore.InvoiceFooterTextColumn2))
                return;

        var column1Lines = string.IsNullOrEmpty(pdfSettingsByStore.InvoiceFooterTextColumn1)
            ? new List<string>()
            : pdfSettingsByStore.InvoiceFooterTextColumn1
                .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();
        var column2Lines = string.IsNullOrEmpty(pdfSettingsByStore.InvoiceFooterTextColumn2)
            ? new List<string>()
            : pdfSettingsByStore.InvoiceFooterTextColumn2
                .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();

        if (!column1Lines.Any() && !column2Lines.Any())
            return;

        var totalLines = Math.Max(column1Lines.Count, column2Lines.Count);
        const float margin = 43;

        //if you have really a lot of lines in the footer, then replace 9 with 10 or 11
        var footerHeight = totalLines * 9;
        var directContent = pdfWriter.DirectContent;
        directContent.MoveTo(pageSize.GetLeft(margin), pageSize.GetBottom(margin) + footerHeight);
        directContent.LineTo(pageSize.GetRight(margin), pageSize.GetBottom(margin) + footerHeight);
        directContent.Stroke();

        var footerTable = new PdfPTable(2)
        {
            WidthPercentage = 100f,
            RunDirection = GetDirection(lang)
        };
        footerTable.SetTotalWidth(new float[] { 250, 250 });

        //column 1
        if (column1Lines.Any())
        {
            var column1 = new PdfPCell(new Phrase())
            {
                Border = Rectangle.NO_BORDER,
                HorizontalAlignment = Element.ALIGN_LEFT
            };

            foreach (var footerLine in column1Lines)
            {
                column1.Phrase.Add(new Phrase(footerLine, font));
                column1.Phrase.Add(new Phrase(Environment.NewLine));
            }

            footerTable.AddCell(column1);
        }
        else
        {
            var column = new PdfPCell(new Phrase(" ")) { Border = Rectangle.NO_BORDER };
            footerTable.AddCell(column);
        }

        //column 2
        if (column2Lines.Any())
        {
            var column2 = new PdfPCell(new Phrase())
            {
                Border = Rectangle.NO_BORDER,
                HorizontalAlignment = Element.ALIGN_LEFT
            };

            foreach (var footerLine in column2Lines)
            {
                column2.Phrase.Add(new Phrase(footerLine, font));
                column2.Phrase.Add(new Phrase(Environment.NewLine));
            }

            footerTable.AddCell(column2);
        }
        else
        {
            var column = new PdfPCell(new Phrase(" ")) { Border = Rectangle.NO_BORDER };
            footerTable.AddCell(column);
        }

        footerTable.WriteSelectedRows(0, totalLines, pageSize.GetLeft(margin), pageSize.GetBottom(margin) + footerHeight, directContent);
    }

    /// <summary>
    /// Print header
    /// </summary>
    /// <param name="pdfSettingsByStore">PDF settings</param>
    /// <param name="lang">Language</param>
    /// <param name="order">Order</param>
    /// <param name="font">Text font</param>
    /// <param name="titleFont">Title font</param>
    /// <param name="doc">Document</param>
    protected virtual async Task PrintHeader(PdfSettings pdfSettingsByStore, Language lang, Font font, Font titleFont, Document doc, Store store)
    {
        //logo
        var logoPicture = await _pictureService.GetPictureByIdAsync(pdfSettingsByStore.LogoPictureId);
        var logoExists = logoPicture != null;

        //header
        var headerTable = new PdfPTable(logoExists ? 2 : 1)
        {
            RunDirection = GetDirection(lang)
        };
        headerTable.DefaultCell.Border = Rectangle.NO_BORDER;

        //store info
        var anchor = new Anchor(store.Url.Trim('/'), font)
        {
            Reference = store.Url
        };

        var cellHeader = GetPdfCell(string.Format(await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Admin.PdfReport.Store", lang.Id), store.Name), titleFont);
        cellHeader.Phrase.Add(new Phrase(Environment.NewLine));
        cellHeader.Phrase.Add(new Phrase(anchor));
        cellHeader.Phrase.Add(new Phrase(Environment.NewLine));
        cellHeader.Phrase.Add(await GetParagraphAsync("Plugins.Shipping.Manager.Admin.PdfReport.Date",
            lang, font, DateTime.Now.ToString("D", new CultureInfo(lang.LanguageCulture))));
        cellHeader.Phrase.Add(new Phrase(Environment.NewLine));
        cellHeader.Phrase.Add(new Phrase(Environment.NewLine));
        cellHeader.HorizontalAlignment = Element.ALIGN_LEFT;
        cellHeader.Border = Rectangle.NO_BORDER;

        headerTable.AddCell(cellHeader);

        if (logoExists)
            headerTable.SetWidths(lang.Rtl ? new[] { 0.2f, 0.8f } : new[] { 0.8f, 0.2f });
        headerTable.WidthPercentage = 100f;

        //logo               
        if (logoExists)
        {
            var logoFilePath = await _pictureService.GetThumbLocalPathAsync(logoPicture, 0, false);
            var logo = Image.GetInstance(logoFilePath);
            logo.Alignment = GetAlignment(lang, true);
            logo.ScaleToFit(65f, 65f);

            var cellLogo = new PdfPCell { Border = Rectangle.NO_BORDER };
            cellLogo.AddElement(logo);
            headerTable.AddCell(cellLogo);

        }

        doc.Add(headerTable);
    }

    /// <summary>
    /// Print totals
    /// </summary>
    /// <param name="pdfSettingsByStore">PDF settings</param>
    /// <param name="lang">Language</param>
    /// <param name="order">Order</param>
    /// <param name="font">Text font</param>
    /// <param name="titleFont">Title font</param>
    /// <param name="doc">PDF document</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    protected virtual async Task PrintTotalsAsync(PdfSettings pdfSettingsByStore, Language lang, Order order, Font font, Font titleFont, Document doc)
    {
        //subtotal
        var totalsTable = new PdfPTable(1)
        {
            RunDirection = GetDirection(lang),
            WidthPercentage = 100f
        };
        totalsTable.DefaultCell.Border = Rectangle.NO_BORDER;

        var languageId = lang.Id;

        if (order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax)
        {
            //including tax
            var orderShippingInclTaxInCustomerCurrency =
                _currencyService.ConvertCurrency(order.OrderShippingInclTax, order.CurrencyRate);
            var orderShippingInclTaxStr = await _priceFormatter.FormatShippingPriceAsync(
                orderShippingInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, languageId, true);

            var p = GetPdfCell($"{ await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Admin.PdfReport.ShippingTotal", languageId)} {orderShippingInclTaxStr}", titleFont);
            p.HorizontalAlignment = Element.ALIGN_RIGHT;
            p.Border = Rectangle.NO_BORDER;
            totalsTable.AddCell(p);
        }
        else
        {
            //excluding tax
            var orderShippingExclTaxInCustomerCurrency =
                _currencyService.ConvertCurrency(order.OrderShippingExclTax, order.CurrencyRate);
            var orderShippingExclTaxStr = await _priceFormatter.FormatShippingPriceAsync(
                orderShippingExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, languageId, false);

            var p = GetPdfCell($"{await _localizationService.GetResourceAsync("Plugins.Shipping.Manager.Admin.PdfReport.ShippingTotal", languageId)} {orderShippingExclTaxStr}", titleFont);
            p.HorizontalAlignment = Element.ALIGN_RIGHT;
            p.Border = Rectangle.NO_BORDER;
            totalsTable.AddCell(p);
        }


        //tax
        var taxStr = string.Empty;
        var taxRates = new SortedDictionary<decimal, decimal>();
        bool displayTax;
        var displayTaxRates = true;
        if (_taxSettings.HideTaxInOrderSummary && order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax)
        {
            displayTax = false;
        }
        else
        {
            if (order.OrderTax == 0 && _taxSettings.HideZeroTax)
            {
                displayTax = false;
                displayTaxRates = false;
            }
            else
            {
                taxRates = _orderService.ParseTaxRates(order, order.TaxRates);

                displayTaxRates = _taxSettings.DisplayTaxRates && taxRates.Any();
                displayTax = !displayTaxRates;

                var orderTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderTax, order.CurrencyRate);
                taxStr = await _priceFormatter.FormatPriceAsync(orderTaxInCustomerCurrency, true, order.CustomerCurrencyCode,
                    false, languageId);
            }
        }

        if (displayTax)
        {
            var p = GetPdfCell($"{await _localizationService.GetResourceAsync("Order.Tax", languageId)} {taxStr}", font);
            p.HorizontalAlignment = Element.ALIGN_RIGHT;
            p.Border = Rectangle.NO_BORDER;
            totalsTable.AddCell(p);
        }

        if (displayTaxRates)
        {
            foreach (var item in taxRates)
            {
                var taxRate = string.Format(await _localizationService.GetResourceAsync("Order.TaxRateLine", languageId),
                    _priceFormatter.FormatTaxRate(item.Key));
                var taxValue = await _priceFormatter.FormatPriceAsync(
                    _currencyService.ConvertCurrency(item.Value, order.CurrencyRate), true, order.CustomerCurrencyCode,
                    false, languageId);

                var p = GetPdfCell($"{taxRate} {taxValue}", font);
                p.HorizontalAlignment = Element.ALIGN_RIGHT;
                p.Border = Rectangle.NO_BORDER;
                totalsTable.AddCell(p);
            }
        }

        //order total
        var orderTotalInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderTotal, order.CurrencyRate);
        var orderTotalStr = await _priceFormatter.FormatPriceAsync(orderTotalInCustomerCurrency, true, order.CustomerCurrencyCode, false, languageId);

        var pTotal = GetPdfCell($"{await _localizationService.GetResourceAsync("Account.CustomerOrders.Ordertotal", languageId)} {orderTotalStr}", font);
        pTotal.HorizontalAlignment = Element.ALIGN_RIGHT;
        pTotal.Border = Rectangle.NO_BORDER;
        totalsTable.AddCell(pTotal);

        doc.Add(totalsTable);
    }

    #endregion

        #region PDFInvoice

        /// <summary>
        /// Print order notes
        /// </summary>
        /// <param name="pdfSettingsByStore">PDF settings</param>
        /// <param name="order">Order</param>
        /// <param name="lang">Language</param>
        /// <param name="titleFont">Title font</param>
        /// <param name="doc">Document</param>
        /// <param name="font">Font</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        protected virtual async Task PrintOrderNotesAsync(PdfSettings pdfSettingsByStore, Order order, Language lang, Font titleFont, Document doc, Font font)
        {
            if (!pdfSettingsByStore.RenderOrderNotes)
                return;

            var orderNotes = (await _orderService.GetOrderNotesByOrderIdAsync(order.Id, true))
                .OrderByDescending(on => on.CreatedOnUtc)
                .ToList();

            if (!orderNotes.Any())
                return;

            var notesHeader = new PdfPTable(1)
            {
                RunDirection = GetDirection(lang),
                WidthPercentage = 100f
            };

            var cellOrderNote = await GetPdfCellAsync("PDFInvoice.OrderNotes", lang, titleFont);
            cellOrderNote.Border = Rectangle.NO_BORDER;
            notesHeader.AddCell(cellOrderNote);
            doc.Add(notesHeader);
            doc.Add(new Paragraph(" "));

            var notesTable = new PdfPTable(2)
            {
                RunDirection = GetDirection(lang),
                WidthPercentage = 100f
            };
            notesTable.SetWidths(lang.Rtl ? new[] { 70, 30 } : new[] { 30, 70 });

            //created on
            cellOrderNote = await GetPdfCellAsync("PDFInvoice.OrderNotes.CreatedOn", lang, font);
            cellOrderNote.BackgroundColor = BaseColor.LightGray;
            cellOrderNote.HorizontalAlignment = Element.ALIGN_CENTER;
            notesTable.AddCell(cellOrderNote);

            //note
            cellOrderNote = await GetPdfCellAsync("PDFInvoice.OrderNotes.Note", lang, font);
            cellOrderNote.BackgroundColor = BaseColor.LightGray;
            cellOrderNote.HorizontalAlignment = Element.ALIGN_CENTER;
            notesTable.AddCell(cellOrderNote);

            foreach (var orderNote in orderNotes)
            {
                cellOrderNote = GetPdfCell(await _dateTimeHelper.ConvertToUserTimeAsync(orderNote.CreatedOnUtc, DateTimeKind.Utc), font);
                cellOrderNote.HorizontalAlignment = Element.ALIGN_LEFT;
                notesTable.AddCell(cellOrderNote);

                cellOrderNote = GetPdfCell(_htmlFormatter.ConvertHtmlToPlainText(_orderService.FormatOrderNoteText(orderNote), true, true), font);
                cellOrderNote.HorizontalAlignment = Element.ALIGN_LEFT;
                notesTable.AddCell(cellOrderNote);

                //should we display a link to downloadable files here?
                //I think, no. Anyway, PDFs are printable documents and links (files) are useful here
            }

            doc.Add(notesTable);
        }

        /// <summary>
        /// Print totals
        /// </summary>
        /// <param name="vendorId">Vendor identifier</param>
        /// <param name="lang">Language</param>
        /// <param name="order">Order</param>
        /// <param name="font">Text font</param>
        /// <param name="titleFont">Title font</param>
        /// <param name="doc">PDF document</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        protected virtual async Task PrintTotalsAsync(int vendorId, Language lang, Order order, Font font, Font titleFont, Document doc)
        {
            //vendors cannot see totals
            if (vendorId != 0)
                return;

            //subtotal
            var totalsTable = new PdfPTable(1)
            {
                RunDirection = GetDirection(lang),
                WidthPercentage = 100f
            };
            totalsTable.DefaultCell.Border = Rectangle.NO_BORDER;

            var languageId = lang.Id;

            //order subtotal
            if (order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax &&
                !_taxSettings.ForceTaxExclusionFromOrderSubtotal)
            {
                //including tax

                var orderSubtotalInclTaxInCustomerCurrency =
                    _currencyService.ConvertCurrency(order.OrderSubtotalInclTax, order.CurrencyRate);
                var orderSubtotalInclTaxStr = await _priceFormatter.FormatPriceAsync(orderSubtotalInclTaxInCustomerCurrency, true,
                    order.CustomerCurrencyCode, languageId, true);

                var p = GetPdfCell($"{await _localizationService.GetResourceAsync("PDFInvoice.Sub-Total", languageId)} {orderSubtotalInclTaxStr}", font);
                p.HorizontalAlignment = Element.ALIGN_RIGHT;
                p.Border = Rectangle.NO_BORDER;
                totalsTable.AddCell(p);
            }
            else
            {
                //excluding tax

                var orderSubtotalExclTaxInCustomerCurrency =
                    _currencyService.ConvertCurrency(order.OrderSubtotalExclTax, order.CurrencyRate);
                var orderSubtotalExclTaxStr = await _priceFormatter.FormatPriceAsync(orderSubtotalExclTaxInCustomerCurrency, true,
                    order.CustomerCurrencyCode, languageId, false);

                var p = GetPdfCell($"{await _localizationService.GetResourceAsync("PDFInvoice.Sub-Total", languageId)} {orderSubtotalExclTaxStr}", font);
                p.HorizontalAlignment = Element.ALIGN_RIGHT;
                p.Border = Rectangle.NO_BORDER;
                totalsTable.AddCell(p);
            }

            //discount (applied to order subtotal)
            if (order.OrderSubTotalDiscountExclTax > decimal.Zero)
            {
                //order subtotal
                if (order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax &&
                    !_taxSettings.ForceTaxExclusionFromOrderSubtotal)
                {
                    //including tax

                    var orderSubTotalDiscountInclTaxInCustomerCurrency =
                        _currencyService.ConvertCurrency(order.OrderSubTotalDiscountInclTax, order.CurrencyRate);
                    var orderSubTotalDiscountInCustomerCurrencyStr = await _priceFormatter.FormatPriceAsync(
                        -orderSubTotalDiscountInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, languageId, true);

                    var p = GetPdfCell($"{await _localizationService.GetResourceAsync("PDFInvoice.Discount", languageId)} {orderSubTotalDiscountInCustomerCurrencyStr}", font);
                    p.HorizontalAlignment = Element.ALIGN_RIGHT;
                    p.Border = Rectangle.NO_BORDER;
                    totalsTable.AddCell(p);
                }
                else
                {
                    //excluding tax

                    var orderSubTotalDiscountExclTaxInCustomerCurrency =
                        _currencyService.ConvertCurrency(order.OrderSubTotalDiscountExclTax, order.CurrencyRate);
                    var orderSubTotalDiscountInCustomerCurrencyStr = await _priceFormatter.FormatPriceAsync(
                        -orderSubTotalDiscountExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, languageId, false);

                    var p = GetPdfCell($"{await _localizationService.GetResourceAsync("PDFInvoice.Discount", languageId)} {orderSubTotalDiscountInCustomerCurrencyStr}", font);
                    p.HorizontalAlignment = Element.ALIGN_RIGHT;
                    p.Border = Rectangle.NO_BORDER;
                    totalsTable.AddCell(p);
                }
            }

            //shipping
            if (order.ShippingStatus != ShippingStatus.ShippingNotRequired)
            {
                if (order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax)
                {
                    //including tax
                    var orderShippingInclTaxInCustomerCurrency =
                        _currencyService.ConvertCurrency(order.OrderShippingInclTax, order.CurrencyRate);
                    var orderShippingInclTaxStr = await _priceFormatter.FormatShippingPriceAsync(
                        orderShippingInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, languageId, true);

                    var p = GetPdfCell($"{await _localizationService.GetResourceAsync("PDFInvoice.Shipping", languageId)} {orderShippingInclTaxStr}", font);
                    p.HorizontalAlignment = Element.ALIGN_RIGHT;
                    p.Border = Rectangle.NO_BORDER;
                    totalsTable.AddCell(p);
                }
                else
                {
                    //excluding tax
                    var orderShippingExclTaxInCustomerCurrency =
                        _currencyService.ConvertCurrency(order.OrderShippingExclTax, order.CurrencyRate);
                    var orderShippingExclTaxStr = await _priceFormatter.FormatShippingPriceAsync(
                        orderShippingExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, languageId, false);

                    var p = GetPdfCell($"{await _localizationService.GetResourceAsync("PDFInvoice.Shipping", languageId)} {orderShippingExclTaxStr}", font);
                    p.HorizontalAlignment = Element.ALIGN_RIGHT;
                    p.Border = Rectangle.NO_BORDER;
                    totalsTable.AddCell(p);
                }
            }

            //payment fee
            if (order.PaymentMethodAdditionalFeeExclTax > decimal.Zero)
            {
                if (order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax)
                {
                    //including tax
                    var paymentMethodAdditionalFeeInclTaxInCustomerCurrency =
                        _currencyService.ConvertCurrency(order.PaymentMethodAdditionalFeeInclTax, order.CurrencyRate);
                    var paymentMethodAdditionalFeeInclTaxStr = await _priceFormatter.FormatPaymentMethodAdditionalFeeAsync(
                        paymentMethodAdditionalFeeInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, languageId, true);

                    var p = GetPdfCell($"{await _localizationService.GetResourceAsync("PDFInvoice.PaymentMethodAdditionalFee", languageId)} {paymentMethodAdditionalFeeInclTaxStr}", font);
                    p.HorizontalAlignment = Element.ALIGN_RIGHT;
                    p.Border = Rectangle.NO_BORDER;
                    totalsTable.AddCell(p);
                }
                else
                {
                    //excluding tax
                    var paymentMethodAdditionalFeeExclTaxInCustomerCurrency =
                        _currencyService.ConvertCurrency(order.PaymentMethodAdditionalFeeExclTax, order.CurrencyRate);
                    var paymentMethodAdditionalFeeExclTaxStr = await _priceFormatter.FormatPaymentMethodAdditionalFeeAsync(
                        paymentMethodAdditionalFeeExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, languageId, false);

                    var p = GetPdfCell($"{await _localizationService.GetResourceAsync("PDFInvoice.PaymentMethodAdditionalFee", languageId)} {paymentMethodAdditionalFeeExclTaxStr}", font);
                    p.HorizontalAlignment = Element.ALIGN_RIGHT;
                    p.Border = Rectangle.NO_BORDER;
                    totalsTable.AddCell(p);
                }
            }

            //tax
            var taxStr = string.Empty;
            var taxRates = new SortedDictionary<decimal, decimal>();
            bool displayTax;
            var displayTaxRates = true;
            if (_taxSettings.HideTaxInOrderSummary && order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax)
            {
                displayTax = false;
            }
            else
            {
                if (order.OrderTax == 0 && _taxSettings.HideZeroTax)
                {
                    displayTax = false;
                    displayTaxRates = false;
                }
                else
                {
                    taxRates = _orderService.ParseTaxRates(order, order.TaxRates);

                    displayTaxRates = _taxSettings.DisplayTaxRates && taxRates.Any();
                    displayTax = !displayTaxRates;

                    var orderTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderTax, order.CurrencyRate);
                    taxStr = await _priceFormatter.FormatPriceAsync(orderTaxInCustomerCurrency, true, order.CustomerCurrencyCode,
                        false, languageId);
                }
            }

            if (displayTax)
            {
                var p = GetPdfCell($"{await _localizationService.GetResourceAsync("PDFInvoice.Tax", languageId)} {taxStr}", font);
                p.HorizontalAlignment = Element.ALIGN_RIGHT;
                p.Border = Rectangle.NO_BORDER;
                totalsTable.AddCell(p);
            }

            if (displayTaxRates)
            {
                foreach (var item in taxRates)
                {
                    var taxRate = string.Format(await _localizationService.GetResourceAsync("PDFInvoice.TaxRate", languageId),
                        _priceFormatter.FormatTaxRate(item.Key));
                    var taxValue = await _priceFormatter.FormatPriceAsync(
                        _currencyService.ConvertCurrency(item.Value, order.CurrencyRate), true, order.CustomerCurrencyCode,
                        false, languageId);

                    var p = GetPdfCell($"{taxRate} {taxValue}", font);
                    p.HorizontalAlignment = Element.ALIGN_RIGHT;
                    p.Border = Rectangle.NO_BORDER;
                    totalsTable.AddCell(p);
                }
            }

            //discount (applied to order total)
            if (order.OrderDiscount > decimal.Zero)
            {
                var orderDiscountInCustomerCurrency =
                    _currencyService.ConvertCurrency(order.OrderDiscount, order.CurrencyRate);
                var orderDiscountInCustomerCurrencyStr = await _priceFormatter.FormatPriceAsync(-orderDiscountInCustomerCurrency,
                    true, order.CustomerCurrencyCode, false, languageId);

                var p = GetPdfCell($"{await _localizationService.GetResourceAsync("PDFInvoice.Discount", languageId)} {orderDiscountInCustomerCurrencyStr}", font);
                p.HorizontalAlignment = Element.ALIGN_RIGHT;
                p.Border = Rectangle.NO_BORDER;
                totalsTable.AddCell(p);
            }

            //gift cards
            foreach (var gcuh in await _giftCardService.GetGiftCardUsageHistoryAsync(order))
            {
                var gcTitle = string.Format(await _localizationService.GetResourceAsync("PDFInvoice.GiftCardInfo", languageId),
                    (await _giftCardService.GetGiftCardByIdAsync(gcuh.GiftCardId))?.GiftCardCouponCode);
                var gcAmountStr = await _priceFormatter.FormatPriceAsync(
                    -_currencyService.ConvertCurrency(gcuh.UsedValue, order.CurrencyRate), true,
                    order.CustomerCurrencyCode, false, languageId);

                var p = GetPdfCell($"{gcTitle} {gcAmountStr}", font);
                p.HorizontalAlignment = Element.ALIGN_RIGHT;
                p.Border = Rectangle.NO_BORDER;
                totalsTable.AddCell(p);
            }

            //reward points
            if (order.RedeemedRewardPointsEntryId.HasValue && await _rewardPointService.GetRewardPointsHistoryEntryByIdAsync(order.RedeemedRewardPointsEntryId.Value) is RewardPointsHistory redeemedRewardPointsEntry)
            {
                var rpTitle = string.Format(await _localizationService.GetResourceAsync("PDFInvoice.RewardPoints", languageId),
                    -redeemedRewardPointsEntry.Points);
                var rpAmount = await _priceFormatter.FormatPriceAsync(
                    -_currencyService.ConvertCurrency(redeemedRewardPointsEntry.UsedAmount, order.CurrencyRate),
                    true, order.CustomerCurrencyCode, false, languageId);

                var p = GetPdfCell($"{rpTitle} {rpAmount}", font);
                p.HorizontalAlignment = Element.ALIGN_RIGHT;
                p.Border = Rectangle.NO_BORDER;
                totalsTable.AddCell(p);
            }

            //order total
            var orderTotalInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderTotal, order.CurrencyRate);
            var orderTotalStr = await _priceFormatter.FormatPriceAsync(orderTotalInCustomerCurrency, true, order.CustomerCurrencyCode, false, languageId);

            var pTotal = GetPdfCell($"{await _localizationService.GetResourceAsync("PDFInvoice.OrderTotal", languageId)} {orderTotalStr}", titleFont);
            pTotal.HorizontalAlignment = Element.ALIGN_RIGHT;
            pTotal.Border = Rectangle.NO_BORDER;
            totalsTable.AddCell(pTotal);

            doc.Add(totalsTable);
        }

        /// <summary>
        /// Print checkout attributes
        /// </summary>
        /// <param name="vendorId">Vendor identifier</param>
        /// <param name="order">Order</param>
        /// <param name="doc">Document</param>
        /// <param name="lang">Language</param>
        /// <param name="font">Font</param>
        protected virtual void PrintCheckoutAttributes(int vendorId, Order order, Document doc, Language lang, Font font)
        {
            //vendors cannot see checkout attributes
            if (vendorId != 0 || string.IsNullOrEmpty(order.CheckoutAttributeDescription))
                return;

            doc.Add(new Paragraph(" "));
            var attribTable = new PdfPTable(1)
            {
                RunDirection = GetDirection(lang),
                WidthPercentage = 100f
            };

            var cCheckoutAttributes = GetPdfCell(_htmlFormatter.ConvertHtmlToPlainText(order.CheckoutAttributeDescription, true, true), font);
            cCheckoutAttributes.Border = Rectangle.NO_BORDER;
            cCheckoutAttributes.HorizontalAlignment = Element.ALIGN_LEFT;
            attribTable.AddCell(cCheckoutAttributes);
            doc.Add(attribTable);
        }

        /// <summary>
        /// Print products
        /// </summary>
        /// <param name="vendorId">Vendor identifier</param>
        /// <param name="lang">Language</param>
        /// <param name="titleFont">Title font</param>
        /// <param name="doc">Document</param>
        /// <param name="order">Order</param>
        /// <param name="font">Text font</param>
        /// <param name="attributesFont">Product attributes font</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        protected virtual async Task PrintProductsAsync(int vendorId, Language lang, Font titleFont, Document doc, Order order, Font font, Font attributesFont)
        {
            var productsHeader = new PdfPTable(1)
            {
                RunDirection = GetDirection(lang),
                WidthPercentage = 100f
            };
            var cellProducts = await GetPdfCellAsync("PDFInvoice.Product(s)", lang, titleFont);
            cellProducts.Border = Rectangle.BOTTOM_BORDER;
            cellProducts.BorderWidthBottom = 1f;

            productsHeader.AddCell(cellProducts);
            doc.Add(productsHeader);

            doc.Add(new Paragraph(" "));

            //a vendor should have access only to products
            var orderItems = await _orderService.GetOrderItemsAsync(order.Id, vendorId: vendorId);

            var count = 4 + (_catalogSettings.ShowSkuOnProductDetailsPage ? 1 : 0)
                        + (_vendorSettings.ShowVendorOnOrderDetailsPage ? 1 : 0);

            var productsTable = new PdfPTable(count)
            {
                RunDirection = GetDirection(lang),
                WidthPercentage = 100f
            };

            var widths = new Dictionary<int, int[]>
            {
                { 4, new[] { 50, 20, 10, 20 } },
                { 5, new[] { 45, 15, 15, 10, 15 } },
                { 6, new[] { 40, 13, 13, 12, 10, 12 } }
            };

            productsTable.SetWidths(lang.Rtl ? widths[count].Reverse().ToArray() : widths[count]);

            //product name
            var cellProductItem = await GetPdfCellAsync("PDFInvoice.ProductName", lang, font);
            cellProductItem.BackgroundColor = BaseColor.LightGray;
            cellProductItem.HorizontalAlignment = Element.ALIGN_CENTER;
            productsTable.AddCell(cellProductItem);

            //SKU
            if (_catalogSettings.ShowSkuOnProductDetailsPage)
            {
                cellProductItem = await GetPdfCellAsync("PDFInvoice.SKU", lang, font);
                cellProductItem.BackgroundColor = BaseColor.LightGray;
                cellProductItem.HorizontalAlignment = Element.ALIGN_CENTER;
                productsTable.AddCell(cellProductItem);
            }

            //Vendor name
            if (_vendorSettings.ShowVendorOnOrderDetailsPage)
            {
                cellProductItem = await GetPdfCellAsync("PDFInvoice.VendorName", lang, font);
                cellProductItem.BackgroundColor = BaseColor.LightGray;
                cellProductItem.HorizontalAlignment = Element.ALIGN_CENTER;
                productsTable.AddCell(cellProductItem);
            }

            //price
            cellProductItem = await GetPdfCellAsync("PDFInvoice.ProductPrice", lang, font);
            cellProductItem.BackgroundColor = BaseColor.LightGray;
            cellProductItem.HorizontalAlignment = Element.ALIGN_CENTER;
            productsTable.AddCell(cellProductItem);

            //qty
            cellProductItem = await GetPdfCellAsync("PDFInvoice.ProductQuantity", lang, font);
            cellProductItem.BackgroundColor = BaseColor.LightGray;
            cellProductItem.HorizontalAlignment = Element.ALIGN_CENTER;
            productsTable.AddCell(cellProductItem);

            //total
            cellProductItem = await GetPdfCellAsync("PDFInvoice.ProductTotal", lang, font);
            cellProductItem.BackgroundColor = BaseColor.LightGray;
            cellProductItem.HorizontalAlignment = Element.ALIGN_CENTER;
            productsTable.AddCell(cellProductItem);

            var vendors = _vendorSettings.ShowVendorOnOrderDetailsPage ? await _vendorService.GetVendorsByProductIdsAsync(orderItems.Select(item => item.ProductId).ToArray()) : new List<Vendor>();

            foreach (var orderItem in orderItems)
            {
                var product = await _productService.GetProductByIdAsync(orderItem.ProductId);

                var pAttribTable = new PdfPTable(1) { RunDirection = GetDirection(lang) };
                pAttribTable.DefaultCell.Border = Rectangle.NO_BORDER;

                //product name
                var name = await _localizationService.GetLocalizedAsync(product, x => x.Name, lang.Id);
                pAttribTable.AddCell(new Paragraph(name, font));
                cellProductItem.AddElement(new Paragraph(name, font));
                //attributes
                if (!string.IsNullOrEmpty(orderItem.AttributeDescription))
                {
                    var attributesParagraph =
                        new Paragraph(_htmlFormatter.ConvertHtmlToPlainText(orderItem.AttributeDescription, true, true),
                            attributesFont);
                    pAttribTable.AddCell(attributesParagraph);
                }

                //rental info
                if (product.IsRental)
                {
                    var rentalStartDate = orderItem.RentalStartDateUtc.HasValue
                        ? _productService.FormatRentalDate(product, orderItem.RentalStartDateUtc.Value)
                        : string.Empty;
                    var rentalEndDate = orderItem.RentalEndDateUtc.HasValue
                        ? _productService.FormatRentalDate(product, orderItem.RentalEndDateUtc.Value)
                        : string.Empty;
                    var rentalInfo = string.Format(await _localizationService.GetResourceAsync("Order.Rental.FormattedDate"),
                        rentalStartDate, rentalEndDate);

                    var rentalInfoParagraph = new Paragraph(rentalInfo, attributesFont);
                    pAttribTable.AddCell(rentalInfoParagraph);
                }

                productsTable.AddCell(pAttribTable);

                //SKU
                if (_catalogSettings.ShowSkuOnProductDetailsPage)
                {
                    var sku = await _productService.FormatSkuAsync(product, orderItem.AttributesXml);
                    cellProductItem = GetPdfCell(sku ?? string.Empty, font);
                    cellProductItem.HorizontalAlignment = Element.ALIGN_CENTER;
                    productsTable.AddCell(cellProductItem);
                }

                //Vendor name
                if (_vendorSettings.ShowVendorOnOrderDetailsPage)
                {
                    var vendorName = vendors.FirstOrDefault(v => v.Id == product.VendorId)?.Name ?? string.Empty;
                    cellProductItem = GetPdfCell(vendorName, font);
                    cellProductItem.HorizontalAlignment = Element.ALIGN_CENTER;
                    productsTable.AddCell(cellProductItem);
                }

                //price
                string unitPrice;
                if (order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax)
                {
                    //including tax
                    var unitPriceInclTaxInCustomerCurrency =
                        _currencyService.ConvertCurrency(orderItem.UnitPriceInclTax, order.CurrencyRate);
                    unitPrice = await _priceFormatter.FormatPriceAsync(unitPriceInclTaxInCustomerCurrency, true,
                        order.CustomerCurrencyCode, lang.Id, true);
                }
                else
                {
                    //excluding tax
                    var unitPriceExclTaxInCustomerCurrency =
                        _currencyService.ConvertCurrency(orderItem.UnitPriceExclTax, order.CurrencyRate);
                    unitPrice = await _priceFormatter.FormatPriceAsync(unitPriceExclTaxInCustomerCurrency, true,
                        order.CustomerCurrencyCode, lang.Id, false);
                }

                cellProductItem = GetPdfCell(unitPrice, font);
                cellProductItem.HorizontalAlignment = Element.ALIGN_LEFT;
                productsTable.AddCell(cellProductItem);

                //qty
                cellProductItem = GetPdfCell(orderItem.Quantity, font);
                cellProductItem.HorizontalAlignment = Element.ALIGN_LEFT;
                productsTable.AddCell(cellProductItem);

                //total
                string subTotal;
                if (order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax)
                {
                    //including tax
                    var priceInclTaxInCustomerCurrency =
                        _currencyService.ConvertCurrency(orderItem.PriceInclTax, order.CurrencyRate);
                    subTotal = await _priceFormatter.FormatPriceAsync(priceInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode,
                        lang.Id, true);
                }
                else
                {
                    //excluding tax
                    var priceExclTaxInCustomerCurrency =
                        _currencyService.ConvertCurrency(orderItem.PriceExclTax, order.CurrencyRate);
                    subTotal = await _priceFormatter.FormatPriceAsync(priceExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode,
                        lang.Id, false);
                }

                cellProductItem = GetPdfCell(subTotal, font);
                cellProductItem.HorizontalAlignment = Element.ALIGN_LEFT;
                productsTable.AddCell(cellProductItem);
            }

            doc.Add(productsTable);
        }

        /// <summary>
        /// Print addresses
        /// </summary>
        /// <param name="vendorId">Vendor identifier</param>
        /// <param name="lang">Language</param>
        /// <param name="titleFont">Title font</param>
        /// <param name="order">Order</param>
        /// <param name="font">Text font</param>
        /// <param name="doc">Document</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        protected virtual async Task PrintAddressesAsync(int vendorId, Language lang, Font titleFont, Order order, Font font, Document doc)
        {
            var addressTable = new PdfPTable(2) { RunDirection = GetDirection(lang) };
            addressTable.DefaultCell.Border = Rectangle.NO_BORDER;
            addressTable.WidthPercentage = 100f;
            addressTable.SetWidths(new[] { 50, 50 });

            //billing info
            await PrintBillingInfoAsync(vendorId, lang, titleFont, order, font, addressTable);

            //shipping info
            await PrintShippingInfoAsync(lang, order, titleFont, font, addressTable);

            doc.Add(addressTable);
            doc.Add(new Paragraph(" "));
        }

        /// <summary>
        /// Print shipping info
        /// </summary>
        /// <param name="lang">Language</param>
        /// <param name="order">Order</param>
        /// <param name="titleFont">Title font</param>
        /// <param name="font">Text font</param>
        /// <param name="addressTable">PDF table for address</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        protected virtual async Task PrintShippingInfoAsync(Language lang, Order order, Font titleFont, Font font, PdfPTable addressTable)
        {
            var shippingAddressPdf = new PdfPTable(1)
            {
                RunDirection = GetDirection(lang)
            };
            shippingAddressPdf.DefaultCell.Border = Rectangle.BOTTOM_BORDER;

            if (order.ShippingStatus != ShippingStatus.ShippingNotRequired)
            {
                const string indent = "   ";

                //Paragraph lineSeparator = new Paragraph(new Chunk(new iTextSharp.text.pdf.draw.LineSeparator(0.0F, 100.0F, BaseColor.Black, Element.ALIGN_LEFT, 1)));
                //shippingAddressPdf.AddCell(lineSeparator);

                if (!order.PickupInStore)
                {
                    if (order.ShippingAddressId == null || await _addressService.GetAddressByIdAsync(order.ShippingAddressId.Value) is not Address shippingAddress)
                        throw new NopException($"Shipping is required, but address is not available. Order ID = {order.Id}");

                    shippingAddressPdf.AddCell(await GetParagraphAsync("PDFInvoice.ShippingInformation", lang, titleFont));

                    shippingAddressPdf.DefaultCell.Border = Rectangle.NO_BORDER;

                    if (!string.IsNullOrEmpty(shippingAddress.Company))
                        shippingAddressPdf.AddCell(await GetParagraphAsync("PDFInvoice.Company", indent, lang, font, shippingAddress.Company));
                    shippingAddressPdf.AddCell(await GetParagraphAsync("PDFInvoice.Name", indent, lang, font, shippingAddress.FirstName + " " + shippingAddress.LastName));
                    shippingAddressPdf.AddCell(await GetParagraphAsync("PDFInvoice.Email", indent, lang, font, shippingAddress.Email));
                    if (_addressSettings.PhoneEnabled)
                        shippingAddressPdf.AddCell(await GetParagraphAsync("PDFInvoice.Phone", indent, lang, font, shippingAddress.PhoneNumber));
                    if (_addressSettings.FaxEnabled && !string.IsNullOrEmpty(shippingAddress.FaxNumber))
                        shippingAddressPdf.AddCell(await GetParagraphAsync("PDFInvoice.Fax", indent, lang, font, shippingAddress.FaxNumber));
                    if (_addressSettings.StreetAddressEnabled)
                        shippingAddressPdf.AddCell(await GetParagraphAsync("PDFInvoice.Address", indent, lang, font, shippingAddress.Address1));
                    if (_addressSettings.StreetAddress2Enabled && !string.IsNullOrEmpty(shippingAddress.Address2))
                        shippingAddressPdf.AddCell(await GetParagraphAsync("PDFInvoice.Address2", indent, lang, font, shippingAddress.Address2));
                    if (_addressSettings.CityEnabled || _addressSettings.StateProvinceEnabled ||
                        _addressSettings.CountyEnabled || _addressSettings.ZipPostalCodeEnabled)
                    {
                        var addressLine = $"{indent}{shippingAddress.City}, " +
                            $"{(!string.IsNullOrEmpty(shippingAddress.County) ? $"{shippingAddress.County}, " : string.Empty)}" +
                            $"{(await _stateProvinceService.GetStateProvinceByAddressAsync(shippingAddress) is StateProvince stateProvince ? await _localizationService.GetLocalizedAsync(stateProvince, x => x.Name, lang.Id) : string.Empty)} " +
                            $"{shippingAddress.ZipPostalCode}";
                        shippingAddressPdf.AddCell(new Paragraph(addressLine, font));
                    }

                    if (_addressSettings.CountryEnabled && await _countryService.GetCountryByAddressAsync(shippingAddress) is Country country)
                    {
                        shippingAddressPdf.AddCell(
                            new Paragraph(indent + await _localizationService.GetLocalizedAsync(country, x => x.Name, lang.Id), font));
                    }
                    //custom attributes
                    var customShippingAddressAttributes = await _addressAttributeFormatter
                        .FormatAttributesAsync(shippingAddress.CustomAttributes, $"<br />{indent}");
                    if (!string.IsNullOrEmpty(customShippingAddressAttributes))
                    {
                        var text = _htmlFormatter.ConvertHtmlToPlainText(customShippingAddressAttributes, true, true);
                        shippingAddressPdf.AddCell(new Paragraph(indent + text, font));
                    }

                    shippingAddressPdf.AddCell(new Paragraph(" "));
                }
                else if (order.PickupAddressId.HasValue && await _addressService.GetAddressByIdAsync(order.PickupAddressId.Value) is Address pickupAddress)
                {
                    shippingAddressPdf.AddCell(await GetParagraphAsync("PDFInvoice.Pickup", lang, titleFont));

                    if (!string.IsNullOrEmpty(pickupAddress.Address1))
                        shippingAddressPdf.AddCell(new Paragraph(
                            $"{indent}{string.Format(await _localizationService.GetResourceAsync("PDFInvoice.Address", lang.Id), pickupAddress.Address1)}",
                            font));

                    if (!string.IsNullOrEmpty(pickupAddress.City))
                        shippingAddressPdf.AddCell(new Paragraph($"{indent}{pickupAddress.City}", font));

                    if (!string.IsNullOrEmpty(pickupAddress.County))
                        shippingAddressPdf.AddCell(new Paragraph($"{indent}{pickupAddress.County}", font));

                    if (await _countryService.GetCountryByAddressAsync(pickupAddress) is Country country)
                        shippingAddressPdf.AddCell(
                            new Paragraph($"{indent}{await _localizationService.GetLocalizedAsync(country, x => x.Name, lang.Id)}", font));

                    if (!string.IsNullOrEmpty(pickupAddress.ZipPostalCode))
                        shippingAddressPdf.AddCell(new Paragraph($"{indent}{pickupAddress.ZipPostalCode}", font));

                    shippingAddressPdf.AddCell(new Paragraph(" "));
                }

                shippingAddressPdf.AddCell(await GetParagraphAsync("PDFInvoice.ShippingMethod", indent, lang, font, order.ShippingMethod));
                shippingAddressPdf.AddCell(new Paragraph());

                addressTable.AddCell(shippingAddressPdf);
            }
            else
            {
                shippingAddressPdf.AddCell(new Paragraph());
                addressTable.AddCell(shippingAddressPdf);
            }
        }

        /// <summary>
        /// Print billing info
        /// </summary>
        /// <param name="vendorId">Vendor identifier</param>
        /// <param name="lang">Language</param>
        /// <param name="titleFont">Title font</param>
        /// <param name="order">Order</param>
        /// <param name="font">Text font</param>
        /// <param name="addressTable">Address PDF table</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        protected virtual async Task PrintBillingInfoAsync(int vendorId, Language lang, Font titleFont, Order order, Font font, PdfPTable addressTable)
        {
            const string indent = "   ";
            var billingAddressPdf = new PdfPTable(1) { RunDirection = GetDirection(lang) };
            billingAddressPdf.DefaultCell.Border = Rectangle.BOTTOM_BORDER;

            billingAddressPdf.AddCell(await GetParagraphAsync("PDFInvoice.BillingInformation", lang, titleFont));

            billingAddressPdf.DefaultCell.Border = Rectangle.NO_BORDER;

            var billingAddress = await _addressService.GetAddressByIdAsync(order.BillingAddressId);

            if (_addressSettings.CompanyEnabled && !string.IsNullOrEmpty(billingAddress.Company))
                billingAddressPdf.AddCell(await GetParagraphAsync("PDFInvoice.Company", indent, lang, font, billingAddress.Company));

            billingAddressPdf.AddCell(await GetParagraphAsync("PDFInvoice.Name", indent, lang, font, billingAddress.FirstName + " " + billingAddress.LastName));

            billingAddressPdf.AddCell(await GetParagraphAsync("PDFInvoice.Email", indent, lang, font, billingAddress.Email));

            if (_addressSettings.PhoneEnabled)
                billingAddressPdf.AddCell(await GetParagraphAsync("PDFInvoice.Phone", indent, lang, font, billingAddress.PhoneNumber));

            if (_addressSettings.FaxEnabled && !string.IsNullOrEmpty(billingAddress.FaxNumber))
                billingAddressPdf.AddCell(await GetParagraphAsync("PDFInvoice.Fax", indent, lang, font, billingAddress.FaxNumber));

            if (_addressSettings.StreetAddressEnabled)
                billingAddressPdf.AddCell(await GetParagraphAsync("PDFInvoice.Address", indent, lang, font, billingAddress.Address1));

            if (_addressSettings.StreetAddress2Enabled && !string.IsNullOrEmpty(billingAddress.Address2))
                billingAddressPdf.AddCell(await GetParagraphAsync("PDFInvoice.Address2", indent, lang, font, billingAddress.Address2));

            if (_addressSettings.CityEnabled || _addressSettings.StateProvinceEnabled ||
                _addressSettings.CountyEnabled || _addressSettings.ZipPostalCodeEnabled)
            {
                var addressLine = $"{indent}{billingAddress.City}, " +
                    $"{(!string.IsNullOrEmpty(billingAddress.County) ? $"{billingAddress.County}, " : string.Empty)}" +
                    $"{(await _stateProvinceService.GetStateProvinceByAddressAsync(billingAddress) is StateProvince stateProvince ? await _localizationService.GetLocalizedAsync(stateProvince, x => x.Name, lang.Id) : string.Empty)} " +
                    $"{billingAddress.ZipPostalCode}";
                billingAddressPdf.AddCell(new Paragraph(addressLine, font));
            }

            if (_addressSettings.CountryEnabled && await _countryService.GetCountryByAddressAsync(billingAddress) is Country country)
                billingAddressPdf.AddCell(new Paragraph(indent + await _localizationService.GetLocalizedAsync(country, x => x.Name, lang.Id), font));

            //VAT number
            if (!string.IsNullOrEmpty(order.VatNumber))
                billingAddressPdf.AddCell(await GetParagraphAsync("PDFInvoice.VATNumber", indent, lang, font, order.VatNumber));

            //custom attributes
            var customBillingAddressAttributes = await _addressAttributeFormatter
                .FormatAttributesAsync(billingAddress.CustomAttributes, $"<br />{indent}");
            if (!string.IsNullOrEmpty(customBillingAddressAttributes))
            {
                var text = _htmlFormatter.ConvertHtmlToPlainText(customBillingAddressAttributes, true, true);
                billingAddressPdf.AddCell(new Paragraph(indent + text, font));
            }

            //vendors payment details
            if (vendorId == 0)
            {
                //payment method
                var paymentMethod = await _paymentPluginManager.LoadPluginBySystemNameAsync(order.PaymentMethodSystemName);
                var paymentMethodStr = paymentMethod != null
                    ? await _localizationService.GetLocalizedFriendlyNameAsync(paymentMethod, lang.Id)
                    : order.PaymentMethodSystemName;
                if (!string.IsNullOrEmpty(paymentMethodStr))
                {
                    billingAddressPdf.AddCell(new Paragraph(" "));
                    billingAddressPdf.AddCell(await GetParagraphAsync("PDFInvoice.PaymentMethod", indent, lang, font, paymentMethodStr));
                    billingAddressPdf.AddCell(new Paragraph());
                }

                //custom values
                var customValues = _paymentService.DeserializeCustomValues(order);
                if (customValues != null)
                {
                    foreach (var item in customValues)
                    {
                        billingAddressPdf.AddCell(new Paragraph(" "));
                        billingAddressPdf.AddCell(new Paragraph(indent + item.Key + ": " + item.Value, font));
                        billingAddressPdf.AddCell(new Paragraph());
                    }
                }
            }

            addressTable.AddCell(billingAddressPdf);
        }

        protected virtual async Task PrintHeaderAsync(PdfSettings pdfSettingsByStore, Language lang, Order order, Font font, Font titleFont, Document doc)
        {
            PdfPCell cellHeader;

            //logo
            var logoPicture = await _pictureService.GetPictureByIdAsync(pdfSettingsByStore.LogoPictureId);
            var logoExists = logoPicture != null;

            //header
            var headerTable = new PdfPTable(logoExists ? 3 : 2)
            {
                RunDirection = GetDirection(lang)
            };
            headerTable.DefaultCell.Border = Rectangle.NO_BORDER;

            headerTable.WidthPercentage = 100f;

            if (logoExists)
                headerTable.SetWidths(new[] { 40, 10, 50 });
            else
                headerTable.SetWidths(new[] { 50, 50 });

            //logo               
            if (logoExists)
            {
                var logoFilePath = await _pictureService.GetThumbLocalPathAsync(logoPicture, 0, false);
                var logo = Image.GetInstance(logoFilePath);
                logo.Alignment = GetAlignment(lang, false);
                float size = 100;
                if (_shippingManagerSettings.LogoSize != 0)
                    size = _shippingManagerSettings.LogoSize;
                logo.ScaleToFit(size, size);

                var cellLogo = new PdfPCell { Border = Rectangle.NO_BORDER };
                cellLogo.HorizontalAlignment = Element.ALIGN_LEFT;
                cellLogo.AddElement(logo);
                headerTable.AddCell(cellLogo);
            }

            cellHeader = GetPdfCell("", titleFont);
            cellHeader.HorizontalAlignment = Element.ALIGN_LEFT;
            cellHeader.Border = Rectangle.NO_BORDER;
            headerTable.AddCell(cellHeader);

            //store info
            var store = await _storeService.GetStoreByIdAsync(order.StoreId) ?? await _storeContext.GetCurrentStoreAsync();
            var anchor = new Anchor(store.Url.Trim('/'), font)
            {
                Reference = store.Url
            };

            string text = string.Format(await _localizationService.GetResourceAsync("PDFInvoice.Order#", lang.Id, true, ""), order.CustomOrderNumber);
            cellHeader = GetPdfCell(text, titleFont);
            cellHeader.Phrase.Add(new Phrase(Environment.NewLine));
            cellHeader.Phrase.Add(new Phrase(anchor));
            cellHeader.Phrase.Add(new Phrase(Environment.NewLine));
            cellHeader.Phrase.Add(await GetParagraphAsync("PDFInvoice.OrderDate", lang, font, (await _dateTimeHelper.ConvertToUserTimeAsync(order.CreatedOnUtc, DateTimeKind.Utc)).ToString("D", new CultureInfo(lang.LanguageCulture))));
            cellHeader.Phrase.Add(new Phrase(Environment.NewLine));
            cellHeader.Phrase.Add(new Phrase(Environment.NewLine));
            cellHeader.HorizontalAlignment = Element.ALIGN_LEFT;
            cellHeader.Border = Rectangle.NO_BORDER;

            headerTable.AddCell(cellHeader);

            doc.Add(headerTable);
        }

        /// <summary>
        /// Print header
        /// </summary>
        /// <param name="pdfSettingsByStore">PDF settings</param>
        /// <param name="lang">Language</param>
        /// <param name="order">Order</param>
        /// <param name="font">Text font</param>
        /// <param name="titleFont">Title font</param>
        /// <param name="doc">Document</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        protected virtual async Task PrintStandardHeaderAsync(PdfSettings pdfSettingsByStore, Language lang, Order order, Font font, Font titleFont, Document doc)
        {
            //logo
            var logoPicture = await _pictureService.GetPictureByIdAsync(pdfSettingsByStore.LogoPictureId);
            var logoExists = logoPicture != null;

            //header
            var headerTable = new PdfPTable(logoExists ? 2 : 1)
            {
                RunDirection = GetDirection(lang)
            };
            headerTable.DefaultCell.Border = Rectangle.NO_BORDER;

            //store info
            var store = await _storeService.GetStoreByIdAsync(order.StoreId) ?? await _storeContext.GetCurrentStoreAsync();
            var anchor = new Anchor(store.Url.Trim('/'), font)
            {
                Reference = store.Url
            };

            var cellHeader = GetPdfCell(string.Format(await _localizationService.GetResourceAsync("PDFInvoice.Order#", lang.Id), order.CustomOrderNumber), titleFont);
            cellHeader.Phrase.Add(new Phrase(Environment.NewLine));
            cellHeader.Phrase.Add(new Phrase(anchor));
            cellHeader.Phrase.Add(new Phrase(Environment.NewLine));
            cellHeader.Phrase.Add(await GetParagraphAsync("PDFInvoice.OrderDate", lang, font, (await _dateTimeHelper.ConvertToUserTimeAsync(order.CreatedOnUtc, DateTimeKind.Utc)).ToString("D", new CultureInfo(lang.LanguageCulture))));
            cellHeader.Phrase.Add(new Phrase(Environment.NewLine));
            cellHeader.Phrase.Add(new Phrase(Environment.NewLine));
            cellHeader.HorizontalAlignment = Element.ALIGN_LEFT;
            cellHeader.Border = Rectangle.NO_BORDER;

            headerTable.AddCell(cellHeader);

            if (logoExists)
                headerTable.SetWidths(lang.Rtl ? new[] { 0.2f, 0.8f } : new[] { 0.8f, 0.2f });
            headerTable.WidthPercentage = 100f;

            //logo               
            if (logoExists)
            {
                var logoFilePath = await _pictureService.GetThumbLocalPathAsync(logoPicture, 0, false);
                var logo = Image.GetInstance(logoFilePath);
                logo.Alignment = GetAlignment(lang, true);
                float size = 100;
                if (_shippingManagerSettings.LogoSize != 0)
                    size = _shippingManagerSettings.LogoSize;
                logo.ScaleToFit(size, size);

                var cellLogo = new PdfPCell { Border = Rectangle.NO_BORDER };
                cellLogo.AddElement(logo);
                headerTable.AddCell(cellLogo);
            }

            doc.Add(headerTable);
        }

        /// <summary>
        /// Print an order to PDF
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="languageId">Language identifier; 0 to use a language used when placing an order</param>
        /// <param name="vendorId">Vendor identifier to limit products; 0 to print all products. If specified, then totals won't be printed</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains a path of generated file
        /// </returns>
        public virtual async Task<string> PrintInvoiceToPdfAsync(Order order, int languageId = 0, int vendorId = 0)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            var fileName = $"order_{order.OrderGuid}_{CommonHelper.GenerateRandomDigitCode(4)}.pdf";
            var filePath = _fileProvider.Combine(_fileProvider.MapPath("~/wwwroot/files/exportimport"), fileName);
            await using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                var orders = new List<Order> { order };
                await PrintInvoicesToPdfAsync(fileStream, orders, languageId, vendorId);
            }

            return filePath;
        }

        /// <summary>
        /// Print orders to PDF
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="orders">Orders</param>
        /// <param name="languageId">Language identifier; 0 to use a language used when placing an order</param>
        /// <param name="vendorId">Vendor identifier to limit products; 0 to print all products. If specified, then totals won't be printed</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public virtual async Task PrintInvoicesToPdfAsync(Stream stream, IList<Order> orders, int languageId = 0, int vendorId = 0)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (orders == null)
                throw new ArgumentNullException(nameof(orders));

            var pageSize = PageSize.A4;

            if (_pdfSettings.LetterPageSizeEnabled)
                pageSize = PageSize.Letter;

            var doc = new Document(pageSize);
            var pdfWriter = PdfWriter.GetInstance(doc, stream);
            doc.Open();

            //fonts
            var titleFont = GetFont();
            titleFont.SetStyle(Font.BOLD);
            titleFont.Color = BaseColor.Black;
            var font = GetFont();
            var attributesFont = GetFont();
            attributesFont.SetStyle(Font.ITALIC);

            var ordCount = orders.Count;
            var ordNum = 0;

            foreach (var order in orders)
            {
                //by default _pdfSettings contains settings for the current active store
                //and we need PdfSettings for the store which was used to place an order
                //so let's load it based on a store of the current order
                var pdfSettingsByStore = await _settingService.LoadSettingAsync<PdfSettings>(order.StoreId);

                var lang = await _languageService.GetLanguageByIdAsync(languageId == 0 ? order.CustomerLanguageId : languageId);
                if (lang == null || !lang.Published)
                    lang = await _workContext.GetWorkingLanguageAsync();

                //header

                await PrintHeaderAsync(pdfSettingsByStore, lang, order, font, titleFont, doc);

                //addresses
                await PrintAddressesAsync(vendorId, lang, titleFont, order, font, doc);

                //products
                await PrintProductsAsync(vendorId, lang, titleFont, doc, order, font, attributesFont);

                //checkout attributes
                PrintCheckoutAttributes(vendorId, order, doc, lang, font);

                //totals
                await PrintTotalsAsync(vendorId, lang, order, font, titleFont, doc);

                //order notes
                await PrintOrderNotesAsync(pdfSettingsByStore, order, lang, titleFont, doc, font);

                //footer
                PrintFooter(pdfSettingsByStore, pdfWriter, pageSize, lang, font);

                ordNum++;
                if (ordNum < ordCount)
                    doc.NewPage();
            }

            doc.Close();
        }


        #endregion

    }
