using System;
using System.Collections.Generic;
using System.Text;

namespace UDMRequestClass
{
    public enum UDMRequestType { Holding, Portfolio, PortfolioHistory, User}

    public enum UDMOperation { Insert, Read, Update, Delete}

    public class UDMRequest
    {
        public string Email { get; set; } 
        public int? objectID { get; set; }
        public UDMRequestType RequestType { get; set; }
        public UDMOperation Operation { get; set; }
        public UDMHolding Holding  { get; set; }
        public List<UDMPortfolio> Portfolio { get; set; }
        public List<List<UDMPortfolioHistory>> PortfolioHistory { get; set; }
        public UDMUser User { get; set; }
    }
}
