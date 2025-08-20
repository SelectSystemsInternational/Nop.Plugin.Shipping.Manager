using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;

namespace Nop.Plugin.Shipping.Manager.Services
{
    public interface IOrderItemPdfService
    {
        /// <summary>
        /// Print sales orders to PDF
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="orderItems">Order items</param>
        /// <param name="languageId">Language identifier; 0 to use a language used when placing an order</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public Task PrintOrdersToPdfAsync(Stream stream, IList<OrderItem> orderItems, int languageId = 0, int vendorId = 0);

        /// <summary>
        /// Print packaging report to PDF
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="shipments">Shipments</param>
        /// <param name="languageId">Language identifier; 0 to use a language used when placing an order</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public Task PrintPackagingReportToPdfAsync(Stream stream, IList<Shipment> shipments, int languageId = 0);

        /// <summary>
        /// Print packaging slips to PDF
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="shipments">Shipments</param>
        /// <param name="languageId">Language identifier; 0 to use a language used when placing an order</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public Task PrintPackagingSlipsToPdfAsync(Stream stream, IList<Shipment> shipments, int languageId = 0);

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
        Task<string> PrintInvoiceToPdfAsync(Order order, int languageId = 0, int vendorId = 0);

        /// <summary>
        /// Print orders to PDF
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="orders">Orders</param>
        /// <param name="languageId">Language identifier; 0 to use a language used when placing an order</param>
        /// <param name="vendorId">Vendor identifier to limit products; 0 to print all products. If specified, then totals won't be printed</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task PrintInvoicesToPdfAsync(Stream stream, IList<Order> orders, int languageId = 0, int vendorId = 0);

    }
}
