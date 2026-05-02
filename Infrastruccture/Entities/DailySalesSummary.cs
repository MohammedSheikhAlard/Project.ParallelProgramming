using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Entities
{
    public class DailySalesSummary
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int TotalOrders { get; set; }    
        public decimal TotalRevenue { get; set; }

    }
}
