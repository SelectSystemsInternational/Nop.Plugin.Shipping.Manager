using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

using Nop.Plugin.Shipping.Manager.Domain;
//using Nop.Plugin.Shipping.Manager.Models.PackagingOption;

namespace Nop.Plugin.Shipping.Manager.Services
{
    /// <summary>
    /// Shipping service interface
    /// </summary>
    public partial interface IPackagingOptionService
    {

        #region Utilities

        #endregion

        #region Methods

        #region Simple Packing Options

        /// <summary>
        /// Gets a packaging option
        /// </summary>
        /// <param name="packagingOptionId">The packaging option identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the packaging option
        /// </returns>
        /// 
        public PackagingOption GetSimplePackagingOptionById(int packagingOptionId);

        /// <summary>
        /// Gets all simple packaging options
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of PackagingOptions
        /// </returns>
        public IList<PackagingOption> GetSimplePackagingOptions(bool defaultValue = true);

        /// <summary>
        /// Gets all simple packaging options select list
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of PackagingOptions
        /// </returns>
        public IList<SelectListItem> PrepareAvailablePackagingOptionsSelectList(int selectedItem);

        #endregion

        //#region PackagingOptions

        ///// <summary>
        ///// Gets a packaging option
        ///// </summary>
        ///// <param name="packagingOptionId">The packaging option identifier</param>
        ///// <returns>
        ///// A task that represents the asynchronous operation
        ///// The task result contains the packaging option
        ///// </returns>
        //public Task<PackagingOption> GetPackagingOptionByIdAsync(int packagingOptionId);

        ///// <summary>
        ///// Gets a packaging option by name
        ///// </summary>
        ///// <param name="name">The packaging option name</param>
        ///// <returns>
        ///// A task that represents the asynchronous operation
        ///// The task result contains the packagingoption
        ///// </returns>
        //public Task<PackagingOption> GetPackagingOptionByNameAsync(string name);

        ///// <summary>
        ///// Gets all packaging options
        ///// </summary>
        ///// <returns>
        ///// A task that represents the asynchronous operation
        ///// The task result contains the list of packagingoptions
        ///// </returns>
        //public Task<IList<PackagingOption>> GetAllPackagingOptionsAsync(bool showHidden = false);

        ///// <summary>
        ///// Gets all packaging options for search criteria
        ///// </summary>
        ///// <returns>
        ///// A task that represents the asynchronous operation
        ///// The task result contains the list of packaging options
        ///// </returns>
        //public Task<IList<PackagingOption>> GetAllPackagingOptionsAsync(PackagingOptionSearchModel searchModel);

        ///// <summary>
        ///// Inserts a packaging option
        ///// </summary>
        ///// <param name="packagingOption">The packaging option</param>
        ///// <returns>
        ///// A task that represents the asynchronous operation
        ///// </returns> 
        //public Task InsertPackagingOptionAsync(PackagingOption packagingOption);

        ///// <summary>
        ///// Updates the packaging option
        ///// </summary>
        ///// <param name="packagingOption">The packaging option</param>
        ///// <returns>
        ///// A task that represents the asynchronous operation
        ///// </returns>  
        //public Task UpdatePackagingOptionAsync(PackagingOption packagingOption);

        ///// <summary>
        ///// Deletes a packaging option
        ///// </summary>
        ///// <param name="packagingOption">The packaging option</param>
        ///// <returns>
        ///// A task that represents the asynchronous operation
        ///// </returns> 
        //public Task DeletePackagingOptionAsync(PackagingOption packagingOption);

        //#endregion

        #endregion
    }
}