using System;
using System.Collections.Generic;
using Nop.Web.Framework.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace Nop.Plugin.Shipping.Manager.Models.Order
{

    public record OrderSalesModel : BaseNopEntityModel
    {
        public OrderSalesModel()
        {
            AvailableGroupProducts = new List<SelectListItem>();
            AvailableAssociatedProducts = new List<SelectListItem>();
            AvailablePaymentMethods = new List<SelectListItem>();
            PaymentDate = string.Empty;
            PaymentMethod = string.Empty;
            Name = string.Empty;
            ParentProduct = string.Empty;
            AttributeDescription = string.Empty;
            RentalDescription = string.Empty;
            Customer = string.Empty;
            PaymentStatus = string.Empty;
            Price = string.Empty;
            TotalPrice = string.Empty;
            PaymentMethodSystemName = string.Empty;
            CustomerEmail = string.Empty;
            CustomerFullName = string.Empty;
            ShippingMethodSystemName = string.Empty;
            DisplayUrl = string.Empty;
            ShipmentUrl = string.Empty;
            Status = string.Empty;
            AvailableWarehouse = new List<SelectListItem>();
        }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Sales.Fields.FromDate")]
        public DateTime? FromDate { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Sales.Fields.ToDate")]
        public DateTime? ToDate { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Sales.Fields.PaymentDate")]
        public string PaymentDate { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Sales.Fields.IsPay")]
        public bool Ispay { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Sales.Fields.OrderbyName")]
        public bool OrderbyName { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Sales.Fields.PaymentMethod")]
        public string PaymentMethod { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.GroupProductId")]
        public int GroupProductId { get; set; }
        public IList<SelectListItem> AvailableGroupProducts { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Fields.AssociatedProductId")]
        public int AssociatedProductId { get; set; }

        public IList<SelectListItem> AvailableAssociatedProducts { get; set; }

        public List<SelectListItem> AvailablePaymentMethods { get; set; }

        public int OrderItemId { get; set; }

        public int ProductId { get; set; }

        public string Name { get; set; }

        public string ParentProduct { get; set; }

        public string AttributeDescription { get; set; }

        public string RentalDescription { get; set; }

        public string Customer { get; set; }

        public int Quantity { get; set; }

        public int OrderId { get; set; }

        public string PaymentStatus { get; set; }

        public string Price { get; set; }

        public string TotalPrice { get; set; }

        public string PaymentMethodSystemName { get; set; }

        public string CustomerEmail { get; set; }

        public string CustomerFullName { get; set; }

        public string ShippingMethodSystemName { get; set; }

        public string DisplayUrl { get; set; }

        public string ShipmentUrl { get; set; }

        public string Status { get; set; }

        public List<SelectListItem> AvailableWarehouse { get; set; }

        public DateTime? OrderCreatedDate { get; set; }

    }

    public partial record OrderSalesListModel : BasePagedListModel<OrderSalesModel>
    {
    }

    public partial record OrderSalesSearchModel : BaseSearchModel
    {
        public OrderSalesSearchModel()
        {
            AvailablePaymentMethods = new List<SelectListItem>();
            PaymentMethod = string.Empty;
            SearchProductName = string.Empty;
        }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Sales.Fields.SearchProductName")]
        public string SearchProductName { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Sales.Fields.FromDate")]
        [UIHint("DateNullable")]
        public DateTime? FromDate { get; set; }

        public DateTime? FDate { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Sales.Fields.ToDate")]
        [UIHint("DateNullable")]
        public DateTime? ToDate { get; set; }

        public DateTime? TDate { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Sales.Fields.IsPay")]
        public bool Ispay { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Sales.Fields.OrderbyName")]
        public bool OrderbyName { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.Manager.Sales.Fields.PaymentMethod")]
        public string PaymentMethod { get; set; }

        public List<SelectListItem> AvailablePaymentMethods { get; set; }
    }

}
