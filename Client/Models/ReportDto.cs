using System;
using System.Collections.Generic;

namespace Admin.Models
{
    public class ReportDto
    {
        public int TotalOrders { get; set; }
        public int PaidOrders { get; set; }
        public int UnpaidOrders { get; set; }

        public long TotalSales { get; set; }
        public long TotalDiscount { get; set; }

        public List<RecentOrderDto> RecentOrders { get; set; }
            = new List<RecentOrderDto>();

        public List<TopProductDto> TopProducts { get; set; }
            = new List<TopProductDto>();
    }

    public class RecentOrderDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string CustomerName { get; set; }
        public int Total { get; set; }
        public bool IsPaid { get; set; }
        public DateTime CreationDateTime { get; set; }
    }

    public class TopProductDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public int Count { get; set; }
        public long TotalSales { get; set; }
    }
}