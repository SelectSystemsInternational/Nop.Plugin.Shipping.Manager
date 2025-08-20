using System;
using System.Threading.Tasks;

namespace Nop.Plugin.Shipping.Manager.Services
{

    public partial interface IShippingManagerInstallService
    {

        #region Methods

        public Task InstallPermissionsAsync(bool install);

        public Task InstallLocalisationAsync(bool install);

        public Task InstallMessageTemaplatesAsync(bool install);

        //public Task InstallPackagingOptionsAsync(bool install);

        public Task InstallConfigurationAsync(bool install);

        #endregion

    }
}
