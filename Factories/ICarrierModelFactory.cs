using System.Threading.Tasks;

using Nop.Plugin.Shipping.Manager.Domain;
using Nop.Plugin.Shipping.Manager.Models.Carrier;

namespace Nop.Plugin.Shipping.Manager.Factories
{
    /// <summary>
    /// Represents the shipping model factory
    /// </summary>
    public partial interface ICarrierModelFactory
    {

        /// <summary>
        /// Prepare carrier model
        /// </summary>
        /// <param name="model">Carrier model</param>
        /// <param name="carrier">Carrier</param>
        /// <param name="excludeProperties">Whether to exclude populating of some properties of model</param>
        /// <returns>Carrier model</returns>
        public Task<CarrierModel> PrepareCarrierModelAsync(CarrierModel model, Carrier carrier, bool excludeProperties = false);

        /// <summary>
        /// Prepare carrier search model
        /// </summary>
        /// <param name="searchModel">Carrier search model</param>
        /// <returns>Carrier search model</returns>
        public Task<CarrierSearchModel> PrepareCarrierSearchModelAsync(CarrierSearchModel searchModel);

        /// <summary>
        /// Prepare paged carrier list model
        /// </summary>
        /// <param name="searchModel">Carrier search model</param>
        /// <returns>Carrier list model</returns>
        public Task<CarrierListModel> PrepareCarrierListModelAsync(CarrierSearchModel searchModel);

    }
}