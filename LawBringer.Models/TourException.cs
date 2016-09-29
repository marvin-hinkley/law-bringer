using System;

namespace LawBringer.Models
{
    public enum LeaveType
    {
        None = 0,
        Unpaid = 1,
        Annual = 2,
        Sick = 3,
        Holiday = 4,
        Authorized = 5,
        Military = 6,
        RestoredAnnual = 7,
        NonPayAnnual = 8,
        Family = 9,
        Adoption = 10,
        Donor = 11,
        Travel = 12,
        Training = 13
    }

    public enum CompType
    {
        None = 0,
        Earned = 1,
        Used = 2
    }

    public class TourException
    {
        public long? Id { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public LeaveType Leave { get; set; } = LeaveType.None;
        public CompType CompTime { get; set; } = CompType.None;
        public bool Overtime { get; set; }
        public string Comments { get; set; }
    }
}