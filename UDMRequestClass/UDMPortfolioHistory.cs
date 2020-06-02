using System;
using System.Collections.Generic;
using System.Text;

namespace UDMRequestClass
{
    public class UDMPortfolioHistory
    {
        public int Id { get; set; } // LEAVE EMPTY
        public int PortfolioId { get; set; } // FILL ALL THESE BELOW
        public DateTime Date { get; set; }
        public decimal Valuation { get; set; }
        public Risk Risk { get; set; }
        public int ActionTakenId { get; set; }
    }
}
