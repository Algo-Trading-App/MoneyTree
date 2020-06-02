using System;
using System.Collections.Generic;

namespace Messaging.Models
{
    public partial class PortfolioActions
    {
        public PortfolioActions()
        {
            PortfolioHistory = new HashSet<PortfolioHistory>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }

        public virtual ICollection<PortfolioHistory> PortfolioHistory { get; set; }
    }
}
