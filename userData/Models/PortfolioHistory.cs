using System;
using System.Collections.Generic;

namespace Messaging.Models
{
    public partial class PortfolioHistory
    {
        public int Id { get; set; }
        public int PortfolioId { get; set; }
        public DateTime Date { get; set; }
        public decimal Valuation { get; set; }
        public int Risk { get; set; }
        public int ActionTakenId { get; set; }

        public virtual PortfolioActions ActionTaken { get; set; }
        public virtual Portfolio Portfolio { get; set; }
    }
}
