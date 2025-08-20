﻿namespace myFastway.ApiClient.Models
{
    public class FastwayItemsModel
    {
        public int MyItemId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal WeightDead { get; set; }
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public decimal WeightCubic { get; set; }
        public byte[] Version { get; set; }
    }
}
