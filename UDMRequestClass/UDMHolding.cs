using System;
using System.Collections.Generic;
using System.Text;

namespace UDMRequestClass
{
    public class UDMHolding
    {
        public int Id { get; set; } // LEAVE EMPTY
        public int PortfolioId { get; set; } // FILL THIS MEMBER AND ALL BELOW
        public string Name { get; set; }
        public string Abbreviation { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }
    }
}
