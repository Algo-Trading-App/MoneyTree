using System;
using System.Collections.Generic;

namespace Messaging.Models
{
    public partial class Portfolio
    {
        public Portfolio()
        {
            Holding = new HashSet<Holding>();
            PortfolioHistory = new HashSet<PortfolioHistory>();
        }

        public int Id { get; set; }
        public int? OwnerId { get; set; }
        public bool Active { get; set; }
        public DateTime Generated { get; set; }
        public decimal InitialValue { get; set; }
        public decimal StopValue { get; set; }
        public int DesiredRisk { get; set; }

        public virtual User Owner { get; set; }
        public virtual ICollection<Holding> Holding { get; set; }
        public virtual ICollection<PortfolioHistory> PortfolioHistory { get; set; }
    }
}
