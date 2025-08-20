
namespace Nop.Plugin.Shipping.Manager.Models
{
    public partial class ExportRatesModel
    {
        public ExportRatesModel()
        {
            Id = 0;
            Store = 0;
            Vendor = string.Empty;
            Warehouse = string.Empty;
            Carrier = string.Empty;
            Country = string.Empty;
            StateProvince = string.Empty;
            PostcodeZip = string.Empty;
            ShippingMethod = string.Empty;
            WeightFrom = string.Empty;
            WeightTo = string.Empty;
            CalculateCubicWeight = string.Empty;
            CubicWeightFactor = string.Empty;
            OrderSubtotalFrom = string.Empty;
            OrderSubtotalTo = string.Empty;
            AdditionalFixedCost = string.Empty;
            PercentageRateOfSubtotal = string.Empty;
            RatePerWeightUnit = string.Empty;
            LowerWeightLimit = string.Empty;
            CutOffTime = string.Empty;
            FriendlyName = string.Empty;
        }

        #region Properties

        public int Id { get; set; }

        public bool Active { get; set; }

        public int DisplayOrder { get; set; }

        public int Store { get; set; }

        public string Vendor { get; set; }

        public string Warehouse { get; set; }

        public string Carrier { get; set; }

        public string Country { get; set; }

        public string StateProvince { get; set; }

        public string PostcodeZip { get; set; }

        public string ShippingMethod { get; set; }

        public string WeightFrom { get; set; }

        public string WeightTo { get; set; }

        public string CalculateCubicWeight { get; set; }

        public string CubicWeightFactor { get; set; }

        public string OrderSubtotalFrom { get; set; }

        public string OrderSubtotalTo { get; set; }

        public string AdditionalFixedCost { get; set; }

        public string PercentageRateOfSubtotal { get; set; }

        public string RatePerWeightUnit { get; set; }

        public string LowerWeightLimit { get; set; }

        public string CutOffTime { get; set; }

        public string FriendlyName { get; set; }

        public string Description { get; set; }

        public int? TransitDays { get; set; }

        public int? SendFromAddress { get; set; }
        
        public bool Delete { get; set; }

        #endregion
    }
}
