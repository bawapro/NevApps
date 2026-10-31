namespace NevApps.Classes;

public class ShenshahiCalendar
{
    private readonly List<string> Days;
    private readonly List<string> Mah;
    private readonly List<string> Months;
    private readonly List<string> Roj;
    private DateTime[] arrDate;
    private DateTime currDate;

    public ShenshahiCalendar()
    {
        Roj = new List<string>();
        Mah = new List<string>();
        Months = new List<string>();
        Days = new List<string>();
        arrDate = new DateTime[12];
        SetMah();
        SetRoj();
        SetMonth();
        SetDay();
    }

    public DateTime BaseDate =>
        //Base Date Property
        new(1920, 9, 10);

    public DateTime[] CalculateNavroz(DateTime curDate)
    {
        //Get Days Difference between current date and base date
        var daysDiff = (curDate - BaseDate).TotalDays;
        //Get No. Leap Days
        int leapDays = Convert.ToInt16(Math.Round(daysDiff / 365.0));
        //Subtract the Leap Days from the base date
        var newBaseDate = BaseDate.AddDays(-leapDays);
        //Multiply 365 to the leap days
        var val1 = leapDays * 365;
        //Add total days
        var addDays = val1 + leapDays;
        //Add total days to the base date
        currDate = newBaseDate.AddDays(addDays);
        //Get month difference
        int monthsDiff = Convert.ToInt16(Math.Round(currDate.Subtract(BaseDate).Days / (365.25 / 12)));
        //Add month difference by 12 to get 1 complete year
        var date = new DateTime[Convert.ToInt16(monthsDiff) + 12];
        //Assign basedate to the first array position
        date[0] = BaseDate;
        var j = 0;

        for (var i = 1; i < monthsDiff + 12; i++)
        {
            j++;
            if (j == 12)
            {
                //if it is the last day of shenshai calendar, add 5 extra days
                date[i] = date[i - 1].AddDays(35);
                j = 0;
            }
            else
            {
                date[i] = date[i - 1].AddDays(30);
            }
        }

        return date;
    }

    public string[] GetStartMah(DateTime[] Dates)
    {
        var j = 0;
        var startMah = new string[Dates.Length];
        for (var i = 0; i < Dates.Length; i++)
        {
            j++;
            if (j != 12)
            {
                startMah[i] = Mah[j - 1];
            }
            else
            {
                startMah[i] = Mah[j - 1];
                j = 0;
            }
        }

        return startMah;
    }

    public DateTime GetStartDay(DateTime CurrentDate, DateTime[] Dates, string[] startMah, out bool isGatha,
        out string Mahino)
    {
        isGatha = false;
        Mahino = "";
        var j = 0;
        for (var i = 0; i < Dates.Length; i++)
        {
            j++;
            if ((CurrentDate.CompareTo(Dates[i]) > 0 || CurrentDate.CompareTo(Dates[i]) == 0) &&
                CurrentDate.CompareTo(Dates[i + 1]) < 0)
            {
                Mahino = startMah[i];
                if (Mahino == "Aspandard")
                    isGatha = true;
                return Dates[i];
            }
        }

        return Convert.ToDateTime("1900-01-01");
    }

    public int GetYazdegerdiYear(DateTime targetDate)
    {
        // 1. Calculate the absolute total days elapsed since the anchor epoch
        var totalDaysElapsed = (targetDate - BaseDate).TotalDays;

        // 2. Divide by the fixed Zoroastrian year length (365 days)
        var elapsedYears = (int)Math.Floor(totalDaysElapsed / 365.0);

        // 3. Add the base year epoch offset (1290 Y.Z.)
        return 1290 + elapsedYears;
    }

    public (string Roj, string Mah, bool isGatha) GetRojAndMah(DateTime targetDate)
    {
        // 1. Day-of-year in the 365-day Shahenshahi cycle
        long totalDays = (targetDate.Date - BaseDate).Days;
        var yearDay = (int)(totalDays % 365);
        if (yearDay < 0) yearDay += 365;

        // 2. Mah (0-11) and Roj index
        int mahIndex;
        int rojIndex;
        var isGatha = false;

        if (yearDay < 360)
        {
            mahIndex = yearDay / 30; // 0..11
            rojIndex = yearDay % 30; // 0..29  (regular Roj)
        }
        else
        {
            // Gatha days (yearDay 360-364)
            mahIndex = 11; // still Aspandard for naming
            rojIndex = 30 + (yearDay - 360); // 30..34
            isGatha = true;
        }

        var mah = Mah[mahIndex];
        var roj = Roj[rojIndex];

        return (roj, mah, isGatha);
    }

    /// <summary>
    ///     Returns the Gregorian date of the given Roj + Mah in the specified Gregorian year.
    /// </summary>
    public DateTime GetDateFromRojMah(string mah, string roj, int gregorianYear)
    {
        var yearDay = GetYearDayFromRojMah(mah, roj);

        var startOfYear = new DateTime(gregorianYear, 1, 1);
        long daysFromBase = (startOfYear - BaseDate).Days;

        var remainder = daysFromBase % 365;
        if (remainder < 0) remainder += 365;

        var daysToAdd = yearDay - remainder;
        if (daysToAdd < 0)
            daysToAdd += 365;

        var result = startOfYear.AddDays(daysToAdd);

        // Ensure it really falls inside the requested year
        // (can only slip by ±1 day at the extreme edges)
        if (result.Year > gregorianYear)
            result = result.AddDays(-365);
        else if (result.Year < gregorianYear)
            result = result.AddDays(365);

        return result;
    }

    private int GetYearDayFromRojMah(string mah, string roj)
    {
        var mahIdx = Mah.IndexOf(mah);
        var rojIdx = Roj.IndexOf(roj);

        if (mahIdx < 0)
            throw new ArgumentException($"Unknown Mah: {mah}");
        if (rojIdx < 0)
            throw new ArgumentException($"Unknown Roj: {roj}");

        // Regular days (0-29)
        if (rojIdx < 30)
            return mahIdx * 30 + rojIdx;

        // Gatha days (30-34) – only valid with Aspandard
        if (mahIdx != 11)
            throw new ArgumentException("Gatha days only occur in Aspandard Mah");

        return 360 + (rojIdx - 30);
    }

    public List<string> GetMahNames()
    {
        //Get Mah List
        return Mah;
    }

    public List<string> GetRojNames()
    {
        //Get Roj List
        return Roj;
    }

    public List<string> GetDays()
    {
        //Get Days List
        return Days;
    }

    public List<string> GetMonths()
    {
        //Get Months List
        return Months;
    }

    public List<string> SetMah()
    {
        Mah.Insert(0, "Fravardin");
        Mah.Insert(1, "Ardibehesht");
        Mah.Insert(2, "Khordad");
        Mah.Insert(3, "Tir");
        Mah.Insert(4, "Amardad");
        Mah.Insert(5, "Shehrevar");
        Mah.Insert(6, "Meher");
        Mah.Insert(7, "Avan");
        Mah.Insert(8, "Adar");
        Mah.Insert(9, "Dae");
        Mah.Insert(10, "Bahman");
        Mah.Insert(11, "Aspandard");
        return Mah;
    }

    public List<string> SetRoj()
    {
        Roj.Insert(0, "Hormazd");
        Roj.Insert(1, "Bahman");
        Roj.Insert(2, "Ardibesht");
        Roj.Insert(3, "Shehrevar");
        Roj.Insert(4, "Aspandard");
        Roj.Insert(5, "Khordad");
        Roj.Insert(6, "Amardad");
        Roj.Insert(7, "Dae-Pa-Adar");
        Roj.Insert(8, "Adar");
        Roj.Insert(9, "Avan");
        Roj.Insert(10, "Khurshed");
        Roj.Insert(11, "Mohor");
        Roj.Insert(12, "Tir");
        Roj.Insert(13, "Gosh");
        Roj.Insert(14, "Dae-Pa-Meher");
        Roj.Insert(15, "Meher");
        Roj.Insert(16, "Sarosh");
        Roj.Insert(17, "Rashne");
        Roj.Insert(18, "Fravardin");
        Roj.Insert(19, "Behram");
        Roj.Insert(20, "Ram");
        Roj.Insert(21, "Govad");
        Roj.Insert(22, "Dae-Pa-Din");
        Roj.Insert(23, "Din");
        Roj.Insert(24, "Ashishvangh");
        Roj.Insert(25, "Astad");
        Roj.Insert(26, "Asman");
        Roj.Insert(27, "Zamyad");
        Roj.Insert(28, "Mahrespand");
        Roj.Insert(29, "Aneran");
        Roj.Insert(30, "Ahuna-vaiti");
        Roj.Insert(31, "Ushta-vaiti");
        Roj.Insert(32, "Spenta-mainyu");
        Roj.Insert(33, "Vohu-shathra");
        Roj.Insert(34, "Vahishto-ishti");
        return Roj;
    }

    public List<string> SetMonth()
    {
        Months.Insert(0, "January");
        Months.Insert(1, "February");
        Months.Insert(2, "March");
        Months.Insert(3, "April");
        Months.Insert(4, "May");
        Months.Insert(5, "June");
        Months.Insert(6, "July");
        Months.Insert(7, "August");
        Months.Insert(8, "September");
        Months.Insert(9, "October");
        Months.Insert(10, "November");
        Months.Insert(11, "December");
        return Months;
    }

    public List<string> SetDay()
    {
        Days.Insert(0, "Sunday");
        Days.Insert(1, "Monday");
        Days.Insert(2, "Tuesday");
        Days.Insert(3, "Wednesday");
        Days.Insert(4, "Thursday");
        Days.Insert(5, "Friday");
        Days.Insert(6, "Saturday");
        return Days;
    }

    public int GetMonthFromMah(string mah)
    {
        return Mah.IndexOf(mah);
    }

    public int GetDayFromRoj(string roj)
    {
        return Roj.IndexOf(roj);
    }
}