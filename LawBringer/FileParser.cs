using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LawBringer.Models;

namespace LawBringer
{
    public class FileParser
    {
        private string _file;
        private string[] _lines;
        private string[] _leaveCodes =
        {
            "WP LWOP",
            "AL ANNUAL LV",
            "SL SICK LV",
            "HX HOL EX",
            "HW HOL WK",
            "AA AUTH ABS",
            "CB FAM CARE",
            "OT OVERTIME",
            "RG REG TIME",
            "CT CT/CH ERND",
            "CU CT/CH USED"
        };

        public FileParser(string file)
        {
            _file = file;
            //_lines = file.Split(new [] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
            _lines = file.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToArray();
        }

        public Dictionary<string, string> GetName()
        {
            var pattern = @"Select EMPLOYEE: (\S+, \S+).*(\d{3}-\d{2}-\d{4})";
            var match = Regex.Match(_file, pattern);

            var nameTokens = match.Groups[1].Value.Split(',').Select(x => x.Trim()).ToArray();
            var firstName = nameTokens[1];
            var lastName = nameTokens[0];

            return new Dictionary<string, string>
            {
                {"first", firstName},
                {"last", lastName}
            };
        }

        public long GetSSN()
        {
            var pattern = @"Select EMPLOYEE: (\S+, \S+).*(\d{3}-\d{2}-\d{4})";
            var match = Regex.Match(_file, pattern);

            long ssn;
            var success = long.TryParse(match.Groups[2].Value.Replace("-", ""), out ssn);

            return ssn;
        }

        public string GetTL()
        {
            var pattern = @"T&L (\d{3})";
            var match = Regex.Match(_file, pattern);

            return match.Groups[1].Value;
        }
        
        public List<PayPeriod> GetPayPeriods()
        {
            var periodPattern = @"Select PAY PERIOD: (\d{2}-\d{2})";
            var dayPattern = @"\S{3} {1,2}\d{1,2}-\S{3}-\d{2}";
            
            var periodId = "";
            var rawPayPeriods = new Dictionary<string, Dictionary<DateTime, List<List<string>>>>();
            var recording = false;
            var currentDay = new DateTime();
            var payPeriods = new List<PayPeriod>();

            for (var i = 0; i < _lines.Length-1; i++)
            {
                if (_lines[i].Contains("No Tour Entered"))
                {
                    continue;
                }

                if (recording)
                {
                    //End of pay period. Save to parent list and start a new list.
                    if (_lines[i].StartsWith("Select EMPLOYEE: ^") || _lines[i].StartsWith("8B Codes"))
                    {
                        recording = false;
                    }
                    //Still in pay period
                    else
                    {
                        var lineTokens = Regex.Split(_lines[i], @"\s{2,}").ToList();

                        //same day
                        if (Regex.IsMatch(_lines[i], @"^\s{5,}"))
                        {
                            if (lineTokens.First() == string.Empty)
                            {
                                lineTokens.RemoveAt(0);
                            }

                            rawPayPeriods[periodId][currentDay].Add(lineTokens);
                        }
                        //new day
                        else
                        {
                            if (lineTokens.First() == string.Empty)
                            {
                                lineTokens.RemoveAt(0);
                            }
                            
                            var possibleDateParts = lineTokens[0].Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                            if (possibleDateParts.Length > 1)
                            {
                                currentDay = ParseDate(possibleDateParts[1]);
                                lineTokens.RemoveAt(0);
                            }
                            else
                            {
                                currentDay = ParseDate(lineTokens[1]);
                                lineTokens.RemoveAt(0);
                                lineTokens.RemoveAt(0);
                            }

                            if (lineTokens.Count > 0)
                            {
                                rawPayPeriods[periodId].Add(currentDay, new List<List<string>> { lineTokens });
                            }
                        }
                    }
                }
                else
                {
                    //Check for a new pay period
                    if (Regex.IsMatch(_lines[i], periodPattern))
                    {
                        recording = true;
                        periodId = Regex.Match(_lines[i], periodPattern).Groups[1].Value;
                        rawPayPeriods.Add(periodId, new Dictionary<DateTime, List<List<string>>>());
                        //Skip unused lines
                        i += 5;
                    }
                }
            }




            //parse tours into objects
            foreach (var rawPayPeriod in rawPayPeriods)
            {
                var tours = new List<Tour>();

                foreach (var day in rawPayPeriod.Value)
                {
                    var tourTimeString = day.Value[0][0];
                    day.Value[0].RemoveAt(0);

                    var tour = new Tour
                    {
                        Day = day.Key
                    };

                    switch (tourTimeString)
                    {
                        case "Intermittent":
                            tour.Type = TourType.Intermittent;
                            break;
                        case "Day Off":
                            tour.Type = TourType.DayOff;
                            break;
                        case "No Tour Entered":
                            tour.Type = TourType.NotEntered;
                            break;
                        default:
                            tour.Type = TourType.Scheduled;

                            var timeSpan = ParseTimeSpan(tourTimeString, tour.Day);
                            tour.Start = timeSpan[0];
                            tour.End = timeSpan[1];
                            break;
                    }

                    var tourExceptions = new List<TourException>();
                    //Parse tour lines
                    foreach (var line in day.Value)
                    {
                        //skip this iteration if there are no tokens
                        if (line.Count < 1)
                        {
                            continue;
                        }

                        var tourException = new TourException();
                        
                        if (Regex.IsMatch(line[0], @"(?:\d{2}:\d{2}[A|P]|NOON|MID)-(?:\d{2}:\d{2}[A|P]|NOON|MID)"))
                        {
                            var exceptionDuration = ParseTimeSpan(line[0], tour.Day);

                            //time string is only token. additional tour on same day
                            if (line.Count == 1)
                            {
                                //add new tour with current day
                                tours.Add(new Tour
                                {
                                    Type = TourType.Scheduled,
                                    Day = tour.Day,
                                    Start = exceptionDuration[0],
                                    End = exceptionDuration[1]
                                });

                                continue;
                            }

                            //following time string found. additional tour on same day
                            if (Regex.IsMatch(line[1], @"(?:\d{2}:\d{2}[A|P]|NOON|MID)-(?:\d{2}:\d{2}[A|P]|NOON|MID)"))
                            {
                                //add new tour with current day
                                tours.Add(new Tour
                                {
                                    Type = TourType.Scheduled,
                                    Day = tour.Day,
                                    Start = exceptionDuration[0],
                                    End = exceptionDuration[1]
                                });
                                //remove this time string
                                line.RemoveAt(0);

                                //set duration to next time string
                                exceptionDuration = ParseTimeSpan(line[0], tour.Day);
                            //this is an intermittent tour value, not an exception
                            } else if (line[1] == "RG REG TIME")
                            {
                                //add new tour with current day
                                tours.Add(new Tour
                                {
                                    Type = TourType.Intermittent,
                                    Day = tour.Day,
                                    Start = exceptionDuration[0],
                                    End = exceptionDuration[1]
                                });

                                //break, since there shouldn't be an exception here
                                continue;
                            }

                            tourException.Start = exceptionDuration[0];
                            tourException.End = exceptionDuration[1];

                            if (line[1] == "OT OVERTIME")
                            {
                                tourException.Overtime = true;
                            } 
                            //leave or comp time
                            else
                            {
                                var leaveType = ParseLeaveType(line[1]);
                                var compType = ParseCompType(line[1]);
                                
                                if (leaveType == LeaveType.None)
                                {
                                    if (compType == CompType.None)
                                    {
                                        throw new ArgumentException("Invalid Leave-Compensation-Overtime Type");
                                    }

                                    tourException.CompTime = compType;
                                }
                                else
                                {
                                    tourException.Leave = leaveType;
                                }
                            }
                            tourExceptions.Add(tourException);
                        }

                        //tourExceptions.Add(tourException);
                    }

                    tour.Exceptions = tourExceptions;

                    if (tour.Type != TourType.Intermittent)
                    {
                        tours.Add(tour);
                    }
                }

                payPeriods.Add(new PayPeriod
                {
                    Id = rawPayPeriod.Key,
                    Tours = tours
                });
            }

            return payPeriods;
        }

        /// <summary>
        /// Parses date strings in the format: dd-{3 char month abbreviation}-yy
        /// </summary>
        /// <param name="date"></param>
        /// <returns>DateTime</returns>
        private DateTime ParseDate(string date)
        {
            var dateParts = date.Split(new[] { "-" }, StringSplitOptions.None);
            var dateNum = int.Parse(dateParts[0]);
            var monthNum = TranslateMonth(dateParts[1]);
            var year = int.Parse(dateParts[2]) + 2000;

            return new DateTime(year, monthNum, dateNum);
        }

        private LeaveType ParseLeaveType(string leaveString)
        {
            var leave = LeaveType.None;

            switch (leaveString)
            {
                case "WP LWOP":
                    leave = LeaveType.Unpaid;
                    break;
                case "AL ANNUAL LV":
                    leave = LeaveType.Annual;
                    break;
                case "SL SICK LV":
                    leave = LeaveType.Sick;
                    break;
                case "HX HOL EX":
                    leave = LeaveType.Holiday;
                    break;
                case "HW HOL WK":
                    leave = LeaveType.Holiday;
                    break;
                case "AA AUTH ABS":
                    leave = LeaveType.Authorized;
                    break;
                case "ML MIL LV":
                    leave = LeaveType.Military;
                    break;
                case "RL RES ANN LV":
                    leave = LeaveType.RestoredAnnual;
                    break;
                case "NP NON PAY":
                    leave = LeaveType.NonPayAnnual;
                    break;
                case "CB FAM CARE":
                    leave = LeaveType.Family;
                    break;
                case "AD ADOPT":
                    leave = LeaveType.Adoption;
                    break;
                case "DL DONOR LV":
                    leave = LeaveType.Donor;
                    break;
                case "TV TRAVEL":
                    leave = LeaveType.Travel;
                    break;
                case "TR TRAINING":
                    leave = LeaveType.Training;
                    break;
            }

            return leave;
        }

        private CompType ParseCompType(string compString)
        {
            var compType = CompType.None;

            switch (compString)
            {
                case "CT CT/CH ERND":
                    compType = CompType.Earned;
                    break;
                case "CU CT/CH USED":
                    compType = CompType.Used;
                    break;
            }

            return compType;
        }

        private List<DateTime> ParseTimeSpan(string timeSpanString, DateTime reference)
        {
            var tokens = timeSpanString.Split(new[] {"-"}, StringSplitOptions.None).ToList();
            var dateObjects = new List<DateTime>();

            foreach (var token in tokens)
            {
                var time = token;
                int? hour = null;

                //replace mid/noon
                switch (time)
                {
                    case "MID":
                        time = "12:00A";
                        hour = 0;
                        break;
                    case "NOON":
                        time = "12:00P";
                        hour = 12;
                        break;
                }

                //parse time
                var timeParts = time.Split(new[] {":"}, StringSplitOptions.None);
                var minuteString = timeParts[1].Replace("A", string.Empty).Replace("P", string.Empty);
                var minute = int.Parse(minuteString);
                
                var timeObject = new DateTime(reference.Year, reference.Month, reference.Day, hour ?? int.Parse(timeParts[0]), minute, 0);
                
                if (timeParts[1].EndsWith("P"))
                {
                    if (hour != 12 && hour != 0 && timeObject.Hour != 12 && timeObject.Hour != 0)
                    {
                        timeObject = timeObject.AddHours(12);
                    }
                }

                //time span crossed midnight boundary. we need to adjust the day
                if (token == tokens.Last() && timeParts[1].EndsWith("A") && dateObjects[0].Hour >= 12)
                {
                    timeObject = timeObject.AddDays(1);
                }

                //add datetime to return object
                dateObjects.Add(timeObject);
            }

            return dateObjects;
        }

        private int TranslateMonth(string abbreviation)
        {
            var month = 0;

            switch (abbreviation)
            {
                case "Jan":
                    month = 1;
                    break;
                case "Feb":
                    month = 2;
                    break;
                case "Mar":
                    month = 3;
                    break;
                case "Apr":
                    month = 4;
                    break;
                case "May":
                    month = 5;
                    break;
                case "Jun":
                    month = 6;
                    break;
                case "Jul":
                    month = 7;
                    break;
                case "Aug":
                    month = 8;
                    break;
                case "Sep":
                    month = 9;
                    break;
                case "Oct":
                    month = 10;
                    break;
                case "Nov":
                    month = 11;
                    break;
                case "Dec":
                    month = 12;
                    break;
                default:
                    throw new ArgumentException("Invalid month");
            }

            return month;
        }
    }
}