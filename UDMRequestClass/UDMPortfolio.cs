using System;
using System.Collections.Generic;
using System.Text;

namespace UDMRequestClass
{
    public enum Risk { Low, Med, High }
    public class UDMPortfolio
    {
        public int Id { get; set; } // LEAVE EMPTY
        public int OwnerId { get; set; } // LEAVE EMPTY
        public bool Active { get; set; } //FILL ALL THESE BELOW 
        public DateTime Generated { get; set; }
        public decimal InitialValue { get; set; }
        public decimal StopValue { get; set; }
        public Risk DesiredRisk { get; set; }
        public List<UDMHolding> Holding { get; set; }
    }
}
