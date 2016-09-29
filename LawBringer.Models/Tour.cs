using System;
using System.Collections.Generic;

namespace LawBringer.Models
{
    public enum TourType
    {
        DayOff = 0,
        Intermittent = 1,
        Scheduled = 2,
        NotEntered = 3
    }

    public class Tour
    {
        public long? Id { get; set; }
        public bool Compressed { get; set; }
        public TourType Type { get; set; }
        public DateTime Day { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public List<TourException> Exceptions { get; set; }
    }
}