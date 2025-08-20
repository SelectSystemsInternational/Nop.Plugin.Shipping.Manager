using System.Runtime.Serialization;

namespace SendCloudApi.Net.Models
{
    [DataContract]
    public class CarrierService
    {
        [DataMember(Name = "code", EmitDefaultValue = false, IsRequired = false)]
        public string Code { get; set; }

        [DataMember(Name = "name", EmitDefaultValue = false, IsRequired = false)]
        public string Name { get; set; }
    }
}
