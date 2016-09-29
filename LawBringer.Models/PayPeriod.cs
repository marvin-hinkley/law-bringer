using System;
using System.Collections.Generic;

namespace LawBringer.Models
{
    public class PayPeriod
    {
        public string Id { get; set; }
        public List<Tour> Tours { get; set; }
    }
}