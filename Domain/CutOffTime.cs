using Nop.Core;
using Nop.Core.Domain.Localization;

namespace Nop.Plugin.Shipping.Manager.Domain
{
    /// <summary>
    /// Represents a cut off time record 
    /// </summary>
    public partial class CutOffTime : BaseEntity, ILocalizedEntity
    {
        /// <summary>
        /// Gets or sets the cutOff time 
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public int DisplayOrder { get; set; }
    }
}