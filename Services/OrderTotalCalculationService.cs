using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Tax;
using Nop.Services.Attributes;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Discounts;
using Nop.Services.Payments;
using Nop.Services.Shipping;
using Nop.Services.Tax;
using Nop.Services.Configuration;

namespace Nop.Services.Orders;

/// <summary>
/// Order service
/// </summary>
public partial class CustomerOrderTotalCalculationService : OrderTotalCalculationService
{

    #region Fields

    protected readonly ISettingService _settingService;

    #endregion

    #region Ctor

    public CustomerOrderTotalCalculationService(CatalogSettings catalogSettings,
        IAddressService addressService,
        IAttributeParser<CheckoutAttribute, CheckoutAttributeValue> checkoutAttributeParser,
        ICustomerService customerService,
        IDiscountService discountService,
        IGenericAttributeService genericAttributeService,
        IGiftCardService giftCardService,
        IOrderService orderService,
        IPaymentService paymentService,
        IPriceCalculationService priceCalculationService,
        IProductService productService,
        IRewardPointService rewardPointService,
        IShippingPluginManager shippingPluginManager,
        IShippingService shippingService,
        IShoppingCartService shoppingCartService,
        IStoreContext storeContext,
        ITaxService taxService,
        IWorkContext workContext,
        RewardPointsSettings rewardPointsSettings,
        ShippingSettings shippingSettings,
        ShoppingCartSettings shoppingCartSettings,
        TaxSettings taxSettings,
        ISettingService settingService) : base(catalogSettings,
            addressService,
            checkoutAttributeParser,
            customerService,
            discountService,
            genericAttributeService,
            giftCardService,
            orderService,
            paymentService,
            priceCalculationService,
            productService,
            rewardPointService,
            shippingPluginManager,
            shippingService,
            shoppingCartService,
            storeContext,
            taxService,
            workContext,
            rewardPointsSettings,
            shippingSettings,
            shoppingCartSettings,
            taxSettings)
    {
        _settingService = settingService;
    }

    #endregion

    #region Utilities
 
    #endregion

    #region Methods
 
    /// <summary>
    /// Gets a value indicating whether shipping is free
    /// </summary>
    /// <param name="cart">Cart</param>
    /// <param name="subTotal">Subtotal amount; pass null to calculate subtotal</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains a value indicating whether shipping is free
    /// </returns>
    public override async Task<bool> IsFreeShippingAsync(IList<ShoppingCartItem> cart, decimal? subTotal = null)
    {
        //check whether customer is in a customer role with free shipping applied
        var customer = await _customerService.GetCustomerByIdAsync(cart.FirstOrDefault()?.CustomerId ?? 0);

        if (customer != null && (await _customerService.GetCustomerRolesAsync(customer)).Any(role => role.FreeShipping))
            return true;

        //check whether all shopping cart items and their associated products marked as free shipping
        if (await cart.AllAwaitAsync(async shoppingCartItem => await _shippingService.IsFreeShippingAsync(shoppingCartItem)))
            return true;

        //Get Vendor Shipping Settings
        int vendorScope = 0;
        foreach (var shoppingCartItem in cart)
        {
            var product = await _productService.GetProductByIdAsync(shoppingCartItem.ProductId);
            if (product != null)
            {
                vendorScope = product.VendorId;
                break;
            }
        }

        // Set default values
        var shippingSettingsFreeShippingOverXEnabled = _shippingSettings.FreeShippingOverXEnabled;
        var shippingSettingsFreeShippingOverXIncludingTax = _shippingSettings.FreeShippingOverXIncludingTax;
        var shippingSettingsFreeShippingOverXValue = _shippingSettings.FreeShippingOverXValue;

        if (vendorScope != 0)
        {
            var shippingSettings = await _settingService.LoadSettingAsync<ShippingSettings>(vendorScope);
            shippingSettingsFreeShippingOverXEnabled = shippingSettings.FreeShippingOverXEnabled;
        }

        //free shipping over $X
        if (!shippingSettingsFreeShippingOverXEnabled)
            return false;

        if (!subTotal.HasValue)
        {
            var (_, _, _, subTotalWithDiscount, _) = await GetShoppingCartSubTotalAsync(cart, shippingSettingsFreeShippingOverXIncludingTax);
            subTotal = subTotalWithDiscount;
        }

        //check whether we have subtotal enough to have free shipping
        if (subTotal.Value > shippingSettingsFreeShippingOverXValue)
            return true;

        return false;
    }

    #endregion
}