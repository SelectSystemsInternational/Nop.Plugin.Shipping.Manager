﻿using System.Runtime.Serialization;

namespace SendCloudApi.Net.Models
{
    [DataContract]
    public class Country
    {
        [DataMember(Name = "id", EmitDefaultValue = false, IsRequired = false)]
        public int Id { get; set; }

        [DataMember(Name = "iso_3", EmitDefaultValue = false, IsRequired = true)]
        public string Iso3 { get; set; }

        [DataMember(Name = "iso_2", EmitDefaultValue = false, IsRequired = true)]
        public string Iso2 { get; set; }

        [DataMember(Name = "price", EmitDefaultValue = false, IsRequired = false)]
        public decimal Price { get; set; }

        [DataMember(Name = "name", EmitDefaultValue = false, IsRequired = true)]
        public string Name { get; set; }
    }
}
