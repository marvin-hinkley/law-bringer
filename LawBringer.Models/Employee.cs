using System.Collections.Generic;

namespace LawBringer.Models
{
    public class Employee
    {
        public long Id { get; set; }
        public string TL { get; set; }
        public int Grade { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Title { get; set; }
        public List<PayPeriod> PayPeriods { get; set; }
    }
}