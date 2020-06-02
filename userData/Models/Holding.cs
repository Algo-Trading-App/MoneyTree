using System;
using System.Collections.Generic;

namespace Messaging.Models
{
    public partial class Holding
    {
        public int Id { get; set; }
        public int PortfolioId { get; set; }
        public string Name { get; set; }
        public string Abbreviation { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }

        public virtual Portfolio Portfolio { get; set; }
    }
}
