namespace NevApps.Classes
{
    public class Muhurat
    {
        private static readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> MuhuratTable = new()
        {
            {
                "Sunday", new Dictionary<string, Dictionary<string, string>>
                {
                    {
                        "day", new Dictionary<string, string>
                        {
                            { "06:40 - 08:10", "Udveg" },
                            { "08:10 - 09:40", "Chal" },
                            { "09:40 - 11:10", "Labh" },
                            { "11:10 - 12:40", "Amrit" },
                            { "12:40 - 14:10", "Kaal" },
                            { "14:10 - 15:40", "Shub" },
                            { "15:40 - 17:10", "Rog" },
                            { "17:10 - 18:40", "Udveg" }
                        }
                    },
                    {
                        "night", new Dictionary<string, string>
                        {
                            { "18:40 - 20:10", "Shub" },
                            { "20:10 - 21:40", "Amrit" },
                            { "21:40 - 23:10", "Chal" },
                            { "23:10 - 00:40", "Rog" },
                            { "00:40 - 02:10", "Kaal" },
                            { "02:10 - 03:40", "Labh" },
                            { "03:40 - 05:10", "Udveg" },
                            { "05:10 - 06:40", "Shub" }
                        }
                    }
                }
            },
            {
                "Monday", new Dictionary<string, Dictionary<string, string>>
                {
                    {
                        "day", new Dictionary<string, string>
                        {
                            { "06:40 - 08:10", "Amrit" },
                            { "08:10 - 09:40", "Kaal" },
                            { "09:40 - 11:10", "Shub" },
                            { "11:10 - 12:40", "Rog" },
                            { "12:40 - 14:10", "Udveg" },
                            { "14:10 - 15:40", "Chal" },
                            { "15:40 - 17:10", "Labh" },
                            { "17:10 - 18:40", "Amrit" }
                        }
                    },
                    {
                        "night", new Dictionary<string, string>
                        {
                            { "18:40 - 20:10", "Chal" },
                            { "20:10 - 21:40", "Rog" },
                            { "21:40 - 23:10", "Kaal" },
                            { "23:10 - 00:40", "Labh" },
                            { "00:40 - 02:10", "Udveg" },
                            { "02:10 - 03:40", "Shub" },
                            { "03:40 - 05:10", "Amrit" },
                            { "05:10 - 06:40", "Chal" }
                        }
                    }
                }
            },
            {
                "Tuesday", new Dictionary<string, Dictionary<string, string>>
                {
                    {
                        "day", new Dictionary<string, string>
                        {
                            { "06:40 - 08:10", "Rog" },
                            { "08:10 - 09:40", "Udveg" },
                            { "09:40 - 11:10", "Chal" },
                            { "11:10 - 12:40", "Labh" },
                            { "12:40 - 14:10", "Amrit" },
                            { "14:10 - 15:40", "Kaal" },
                            { "15:40 - 17:10", "Shub" },
                            { "17:10 - 18:40", "Rog" }
                        }
                    },
                    {
                        "night", new Dictionary<string, string>
                        {
                            { "18:40 - 20:10", "Kaal" },
                            { "20:10 - 21:40", "Labh" },
                            { "21:40 - 23:10", "Udveg" },
                            { "23:10 - 00:40", "Shub" },
                            { "00:40 - 02:10", "Amrit" },
                            { "02:10 - 03:40", "Chal" },
                            { "03:40 - 05:10", "Rog" },
                            { "05:10 - 06:40", "Kaal" }
                        }
                    }
                }
            },
            {
                "Wednesday", new Dictionary<string, Dictionary<string, string>>
                {
                    {
                        "day", new Dictionary<string, string>
                        {
                            { "06:40 - 08:10", "Labh" },
                            { "08:10 - 09:40", "Amrit" },
                            { "09:40 - 11:10", "Kaal" },
                            { "11:10 - 12:40", "Shub" },
                            { "12:40 - 14:10", "Rog" },
                            { "14:10 - 15:40", "Udveg" },
                            { "15:40 - 17:10", "Chal" },
                            { "17:10 - 18:40", "Labh" }
                        }
                    },
                    {
                        "night", new Dictionary<string, string>
                        {
                            { "18:40 - 20:10", "Udveg" },
                            { "20:10 - 21:40", "Shub" },
                            { "21:40 - 23:10", "Amrit" },
                            { "23:10 - 00:40", "Chal" },
                            { "00:40 - 02:10", "Rog" },
                            { "02:10 - 03:40", "Kaal" },
                            { "03:40 - 05:10", "Labh" },
                            { "05:10 - 06:40", "Udveg" }
                        }
                    }
                }
            },
            {
                "Thursday", new Dictionary<string, Dictionary<string, string>>
                {
                    {
                        "day", new Dictionary<string, string>
                        {
                            { "06:40 - 08:10", "Shub" },
                            { "08:10 - 09:40", "Rog" },
                            { "09:40 - 11:10", "Udveg" },
                            { "11:10 - 12:40", "Chal" },
                            { "12:40 - 14:10", "Labh" },
                            { "14:10 - 15:40", "Amrit" },
                            { "15:40 - 17:10", "Kaal" },
                            { "17:10 - 18:40", "Shub" }
                        }
                    },
                    {
                        "night", new Dictionary<string, string>
                        {
                            { "18:40 - 20:10", "Amrit" },
                            { "20:10 - 21:40", "Chal" },
                            { "21:40 - 23:10", "Rog" },
                            { "23:10 - 00:40", "Kaal" },
                            { "00:40 - 02:10", "Labh" },
                            { "02:10 - 03:40", "Udveg" },
                            { "03:40 - 05:10", "Shub" },
                            { "05:10 - 06:40", "Amrit" }
                        }
                    }
                }
            },
            {
                "Friday", new Dictionary<string, Dictionary<string, string>>
                {
                    {
                        "day", new Dictionary<string, string>
                        {
                            { "06:40 - 08:10", "Chal" },
                            { "08:10 - 09:40", "Labh" },
                            { "09:40 - 11:10", "Amrit" },
                            { "11:10 - 12:40", "Kaal" },
                            { "12:40 - 14:10", "Shub" },
                            { "14:10 - 15:40", "Rog" },
                            { "15:40 - 17:10", "Udveg" },
                            { "17:10 - 18:40", "Chal" }
                        }
                    },
                    {
                        "night", new Dictionary<string, string>
                        {
                            { "18:40 - 20:10", "Rog" },
                            { "20:10 - 21:40", "Kaal" },
                            { "21:40 - 23:10", "Labh" },
                            { "23:10 - 00:40", "Udveg" },
                            { "00:40 - 02:10", "Shub" },
                            { "02:10 - 03:40", "Amrit" },
                            { "03:40 - 05:10", "Chal" },
                            { "05:10 - 06:40", "Rog" }
                        }
                    }
                }
            },
            {
                "Saturday", new Dictionary<string, Dictionary<string, string>>
                {
                    {
                        "day", new Dictionary<string, string>
                        {
                            { "06:40 - 08:10", "Kaal" },
                            { "08:10 - 09:40", "Shub" },
                            { "09:40 - 11:10", "Rog" },
                            { "11:10 - 12:40", "Udveg" },
                            { "12:40 - 14:10", "Chal" },
                            { "14:10 - 15:40", "Labh" },
                            { "15:40 - 17:10", "Amrit" },
                            { "17:10 - 18:40", "Kaal" }
                        }
                    },
                    {
                        "night", new Dictionary<string, string>
                        {
                            { "18:40 - 20:10", "Labh" },
                            { "20:10 - 21:40", "Udveg" },
                            { "21:40 - 23:10", "Shub" },
                            { "23:10 - 00:40", "Amrit" },
                            { "00:40 - 02:10", "Chal" },
                            { "02:10 - 03:40", "Rog" },
                            { "03:40 - 05:10", "Kaal" },
                            { "05:10 - 06:40", "Labh" }
                        }
                    }
                }
            }
        };

        public static string GetCurrentMuhurat(DateTime currentDateTime)
        {
            // 1. Try checking the current day's active "day" table slots
            string result = GetMuharratByDayAndPeriod(currentDateTime.DayOfWeek.ToString(), "day", currentDateTime);
            if (result != null) return result;

            // 2. Try checking the current day's active "night" table slots
            result = GetMuharratByDayAndPeriod(currentDateTime.DayOfWeek.ToString(), "night", currentDateTime);
            if (result != null) return result;

            // 3. Fallback: If it's early morning (e.g. 2 AM Monday), look at the previous day's (Sunday) night slots
            string previousDay = currentDateTime.AddDays(-1).DayOfWeek.ToString();
            result = GetMuharratByDayAndPeriod(previousDay, "night", currentDateTime);
            if (result != null) return result;

            return "No Muhurat found for the current time.";
        }

        private static string? GetMuharratByDayAndPeriod(string dayName, string period, DateTime timeStamp)
        {
            if (MuhuratTable.TryGetValue(dayName, out var periods) && periods.TryGetValue(period, out var locations))
            {
                foreach (var item in locations)
                {
                    var timeRange = item.Key.Split(" - ");
                    if (DateTime.TryParse(timeRange[0], out var startTime) &&
                        DateTime.TryParse(timeRange[1], out var endTime))
                    {
                        var start = startTime.TimeOfDay;
                        var end = endTime.TimeOfDay;
                        var now = timeStamp.TimeOfDay;
                        bool crossesMidnight = timeRange[1].Trim().StartsWith("00") || end < start;

                        if (crossesMidnight)
                        {
                            if (now >= start || now < end)
                            {
                                return item.Value;
                            }
                        }
                        else
                        {
                            if (now >= start && now < end)
                            {
                                return item.Value;
                            }
                        }
                    }
                }
            }

            return null;
        }

        public static List<(string TimeRange, string Name)> GetFullDayMuhurat(DateTime targetDateTime)
        {
            var timeline = new List<(string TimeRange, string Name)>();
            string day = targetDateTime.DayOfWeek.ToString();

            if (MuhuratTable.TryGetValue(day, out var periods))
            {
                if (periods.TryGetValue("day", out var dayMuhurats))
                    foreach (var kvp in dayMuhurats)
                        timeline.Add((kvp.Key, kvp.Value));

                if (periods.TryGetValue("night", out var nightMuhurats))
                    foreach (var kvp in nightMuhurats)
                        timeline.Add((kvp.Key, kvp.Value));
            }

            return timeline;
        }
    }
}
