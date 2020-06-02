using System;
using System.Collections.Generic;

namespace Messaging.Models
{
    public partial class User
    {
        public User()
        {
            Portfolio = new HashSet<Portfolio>();
        }

        public int Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string BrokerageAccount { get; set; }

        public virtual ICollection<Portfolio> Portfolio { get; set; }
    }
}
