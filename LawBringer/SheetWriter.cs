using System;
using System.Collections.Generic;
using System.Linq;
using LawBringer.Models;
using Microsoft.Office.Interop.Excel;

namespace LawBringer
{
    public class SheetWriter
    {
        private readonly Worksheet _worksheet;
        private readonly string[] _columns = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z"};
        
        public SheetWriter()
        {
            
        }

        public SheetWriter(Worksheet worksheet)
        {
            _worksheet = worksheet;
        }

        public void SetName(string name, Worksheet worksheet = null)
        {
            var ws = worksheet ?? _worksheet;

            ws.Name = name;
        }

        public void WriteUnformattedPayPeriods(List<PayPeriod> payPeriods, Worksheet worksheet = null, bool writeHeaders = false)
        {
            var ws = worksheet ?? _worksheet;
            var tourOffset = 8;

            if (writeHeaders)
            {
                ws.Range["B1", "K1"].Value2 = new[] { "Day", "Date", "Tour Type", "Start Time", "End Time", "Tour Total", "Exception Start Time", "Exception End Time", "Exception Code", "Exception Total" };
                ws.Application.ActiveWindow.SplitRow = 1;
                ws.Application.ActiveWindow.FreezePanes = true;
            }

            foreach (var payPeriod in payPeriods)
            {
                //write pay period row header
//                var payPeriodHeader = ws.Range["A" + tourOffset, "A" + (tourOffset + payPeriod.Tours.Count)];
//                
//                payPeriodHeader.Merge(Missing.Value);
//                payPeriodHeader.Orientation = 90;
//                payPeriodHeader.HorizontalAlignment = XlHAlign.xlHAlignCenter;
//                payPeriodHeader.VerticalAlignment = XlVAlign.xlVAlignCenter;
//                payPeriodHeader.Value2 = new[] {$"Pay Period {payPeriod.Id}"};
//                payPeriodHeader.BorderAround(XlLineStyle.xlContinuous);

                var hourTotal = 0;
                var minuteTotal = 0;
                var exceptionHourTotal = 0;
                var exceptionMinuteTotal = 0;

                foreach (var tour in payPeriod.Tours)
                {
                    var tourDuration = tour.End.Subtract(tour.Start);

                    //update pay period totals if intermittent or scheduled
                    if (tour.Type == TourType.Scheduled || tour.Type == TourType.Intermittent)
                    {
                        hourTotal += tourDuration.Hours;
                        if (tourDuration.Minutes != 30)
                        {
                            minuteTotal += tourDuration.Minutes;
                        }
                    }

                    //write hours
                    ws.Range["B" + tourOffset, "G" + tourOffset].Value2 = new[]
                    {
                        tour.Day.DayOfWeek.ToString(),
                        tour.Day.ToShortDateString(),
                        tour.Type.ToString(),
                        tour.Start.Date == default(DateTime) ? "" : tour.Start.ToShortTimeString(),
                        tour.End.Date == default(DateTime) ? "" : tour.End.ToShortTimeString(),
                        tour.Start.Date == default(DateTime) ? "" : tourDuration.Hours.ToString() + ":" + (tourDuration.Minutes == 30 ? "0" : tourDuration.Minutes.ToString())
                    };

                    //write exceptions
                    if (tour.Exceptions != null && tour.Exceptions.Count >0)
                    {
                        foreach (var tourException in tour.Exceptions)
                        {
                            dynamic exceptionType;

                            //find exception type
                            if (tourException.CompTime == CompType.None)
                            {
                                if (tourException.Leave == LeaveType.None)
                                {
                                    if (tourException.Overtime)
                                    {
                                        exceptionType = "Overtime";
                                    }
                                    else
                                    {
                                        throw new ArgumentException("Invalid Leave-Compensation-Overtime Type");
                                    }
                                }
                                else
                                {
                                    exceptionType = tourException.Leave;
                                }
                            }
                            else
                            {
                                exceptionType = tourException.CompTime;
                            }

                            //find exception duration
                            var exceptionDuration = tourException.End.Subtract(tourException.Start);

                            if (!tourException.Overtime && tourException.Leave != LeaveType.None && tourException.CompTime == CompType.None)
                            {
                                exceptionHourTotal += exceptionDuration.Hours;
                                if (exceptionDuration.Minutes != 30)
                                {
                                    exceptionMinuteTotal += exceptionDuration.Minutes;
                                }
                            }

                            ws.Range["H" + tourOffset, "K" + tourOffset].Value2 = new[]
                            {
                                tourException.Start.ToShortTimeString(),
                                tourException.End.ToShortTimeString(),
                                exceptionType.ToString(),
                                exceptionDuration.Hours.ToString() + ":" + exceptionDuration.Minutes.ToString()
                            };
                            
                            tourOffset++;
                        }
                    }

                    if (tour.Exceptions == null || tour.Exceptions.Count < 1)
                    {
                        tourOffset++;
                    }
                }

                //write tour totals
                if (payPeriod.Tours.Count > 0)
                {
                    var scheduledHfm = minuteTotal / 60;
                    var exceptionHfm = exceptionMinuteTotal / 60;

                    ws.Range["B" + tourOffset, "K" + tourOffset].Value2 = new[] {
                        "", "", "", "", "",
                        $"Tour Total Scheduled {hourTotal + scheduledHfm} : {minuteTotal - (scheduledHfm * 60)}",
                        "", "", "",
                        $"Tour Total Leave {exceptionHourTotal + exceptionHfm} : {exceptionMinuteTotal - (exceptionHfm * 60)}"
                    };

                    tourOffset++;
                }
            }
        }

        private string IndexToColumn(int index)
        {
            return _columns[index - 1];
        }

        private string ColumnToIndex(string column)
        {
            return _columns.First(x => x == column);
        }

        private int CountTours(List<PayPeriod> payPeriods)
        {
            return payPeriods.Sum(payPeriod => payPeriod.Tours.Count());
        }
    }
}
