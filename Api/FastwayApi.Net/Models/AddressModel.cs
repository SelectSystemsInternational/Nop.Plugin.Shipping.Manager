﻿namespace myFastway.ApiClient.Models
{
    public class AddressModel
    {
        public int AddressId { get; set; }
        public string StreetAddress { get; set; }
        public string AdditionalDetails { get; set; }
        public string Locality { get; set; }
        public string StateOrProvince { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public decimal Lat { get; set; }
        public decimal Lng { get; set; }
        public bool UserCreated { get; set; }
        public string Hash { get; set; }
        public string PlaceId { get; set; }

    }
}
