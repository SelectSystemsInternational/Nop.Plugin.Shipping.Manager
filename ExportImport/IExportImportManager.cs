using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Nop.Core.Domain.Orders;

using Nop.Plugin.Shipping.Manager.Models;

namespace Nop.Plugin.Shipping.Manager.ExportImport
{
    /// <summary>
    /// Export manager interface
    /// </summary>
    public partial interface IExportImportManager
    {
        Task<byte[]> ExportOrderItemToXlsx(IEnumerable<OrderItem> itemsToExport);

        Task<byte[]> ExportRatesToXlsxAsync(List<ExportRatesModel> itemsToExport);

        Task ImportRatesFromXlsxAsync(Stream stream);
    }
}
