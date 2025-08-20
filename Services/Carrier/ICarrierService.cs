using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core;

using Nop.Plugin.Shipping.Manager.Domain;
using Nop.Plugin.Shipping.Manager.Models.Carrier;

namespace Nop.Plugin.Shipping.Manager.Services
{
    /// <summary>
    /// Shipping service interface
    /// </summary>
    public partial interface ICarrierService
    {

        #region Utilities

        #endregion

        #region Methods

        #region Carriers

        /// <summary>
        /// Gets a carrier
        /// </summary>
        /// <param name="carrierId">The carrier identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the carrier
        /// </returns>
        Task<Carrier> GetCarrierByIdAsync(int carrierId);

        /// <summary>
        /// Gets a carrier
        /// </summary>
        /// <param name="carrierId">The carrier identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the carrier
        /// </returns>
        Task<Carrier> GetCarrierByNameAsync(string name);

        /// <summary>
        /// Gets a carrier by system name
        /// </summary>
        /// <param name="carrierId">The carrier identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the carrier
        /// </returns>
        Task<Carrier> GetCarrierBySystemNameAsync(string name);

        /// <summary>
        /// Gets a carrier shipping plugin provider 
        /// </summary>
        /// <param name="carrierId">The carrier identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipping plugin provider 
        /// </returns>
        Task<string> GetCarrierShippingPluginProvideAsync(int carrierId);

        /// <summary>
        /// Gets all carriers
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of carriers
        /// </returns>
        Task<IList<Carrier>> GetAllCarriersAsync(bool showHidden = false);

        /// <summary>
        /// Gets all carriers
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of carriers
        /// </returns>
        Task<IList<Carrier>> GetAllCarriersAsync(CarrierSearchModel searchModel);

        /// <summary>
        /// Inserts a carrier
        /// </summary>
        /// <param name="carrier">Carrier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// </returns> 
        Task InsertCarrierAsync(Carrier carrier);

        /// <summary>
        /// Updates the carrier
        /// </summary>
        /// <param name="carrier">Carrier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// </returns>  
        Task UpdateCarrierAsync(Carrier carrier);

        /// <summary>
        /// Deletes a carrier
        /// </summary>
        /// <param name="carrier">The carrier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// </returns> 
        Task DeleteCarrierAsync(Carrier carrier);

        #endregion

        #region Cut of time

        /// <summary>
        /// Get a cut of time
        /// </summary>
        /// <param name="cutOffTimeId">The cut of time identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// </returns>  
        Task<CutOffTime> GetCutOffTimeByIdAsync(int cutOffTimeId);

        /// <summary>
        /// Get all cut of times
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of cut off times
        /// </returns>
        Task<IList<CutOffTime>> GetAllCutOffTimesAsync();

        /// <summary>
        /// Insert the cut of time
        /// </summary>
        /// <param name="cutOffTime">Cut off time</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// </returns> 
        Task InsertCutOffTimeAsync(CutOffTime cutOffTime);

        /// <summary>
        /// Update the cut of time
        /// </summary>
        /// <param name="cutOffTime">Cut off time</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// </returns>
        Task UpdateCutOffTimeAsync(CutOffTime cutOffTime);

        /// <summary>
        /// Delete the cut of time
        /// </summary>
        /// <param name="cutOffTime">Cut off time</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// </returns>         
        Task DeleteCutOffTimeAsync(CutOffTime cutOffTime);

        #endregion

        #endregion
    }
}