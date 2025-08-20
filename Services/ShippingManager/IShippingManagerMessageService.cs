using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domain.Orders;

namespace Nop.Plugin.Shipping.Manager.Services
{
    public partial interface IShippingManagerMessageService
    {

        public Task<IList<int>> SendOrderShippmentCreatedVendorNotificationAsync(Order order, int languageId,
            string attachmentFilePath = null, string attachmentFileName = null);

    }
}
