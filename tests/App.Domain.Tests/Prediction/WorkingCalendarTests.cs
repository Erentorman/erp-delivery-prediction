using App.Domain.Prediction;

namespace App.Domain.Tests.Prediction;

public class WorkingCalendarTests
{
    private static readonly TimeOnly StartTime = new(8, 0);
    private static readonly TimeOnly EndTime = new(17, 0);
    private static readonly TimeOnly BreakStartTime = new(12, 0);
    private static readonly TimeOnly BreakEndTime = new(13, 0);
    private const long NetMinutesPerDay = 480;
    private static readonly DayOfWeek[] WorkingDays =
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];

    private readonly WorkingCalendar _calendar = CreateCalendar();

    private static WorkingCalendar CreateCalendar(
        TimeOnly? startTime = null,
        TimeOnly? endTime = null,
        TimeOnly? breakStartTime = null,
        TimeOnly? breakEndTime = null,
        long netMinutesPerDay = NetMinutesPerDay,
        IReadOnlyCollection<DayOfWeek>? workingDays = null) =>
        new(
            startTime ?? StartTime,
            endTime ?? EndTime,
            breakStartTime ?? BreakStartTime,
            breakEndTime ?? BreakEndTime,
            netMinutesPerDay,
            workingDays ?? WorkingDays);

    [Fact]
    public void Constructor_StartTimeNotBeforeEndTime_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateCalendar(startTime: new TimeOnly(17, 0), endTime: new TimeOnly(8, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateCalendar(startTime: new TimeOnly(8, 0), endTime: new TimeOnly(8, 0)));
    }

    [Fact]
    public void Constructor_BreakStartAfterBreakEnd_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateCalendar(breakStartTime: new TimeOnly(13, 0), breakEndTime: new TimeOnly(12, 0)));
    }

    [Fact]
    public void Constructor_BreakOutsideShiftWindow_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateCalendar(breakStartTime: new TimeOnly(7, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateCalendar(breakEndTime: new TimeOnly(18, 0)));
    }

    [Fact]
    public void Constructor_NonPositiveNetMinutesPerDay_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateCalendar(netMinutesPerDay: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateCalendar(netMinutesPerDay: -1));
    }

    [Fact]
    public void Constructor_EmptyWorkingDays_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CreateCalendar(workingDays: []));
    }

    [Theory]
    [InlineData("2026-08-01", false)] // Saturday
    [InlineData("2026-08-02", false)] // Sunday
    [InlineData("2026-08-03", true)]  // Monday
    [InlineData("2026-08-07", true)]  // Friday
    public void IsWorkingDay_ReturnsCorrectResult(string dateString, bool expected)
    {
        var date = DateOnly.Parse(dateString);
        var result = _calendar.IsWorkingDay(date);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void AddWorkingMinutes_WithNegativeMinutes_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _calendar.AddWorkingMinutes(DateTimeOffset.UtcNow, -1));
    }

    [Fact]
    public void AddWorkingMinutes_ZeroMinutes_ReturnsStartTime()
    {
        var start = new DateTimeOffset(2026, 8, 3, 10, 0, 0, TimeSpan.Zero); // Monday 10:00, already valid

        var result = _calendar.AddWorkingMinutes(start, 0);

        Assert.Equal(start, result);
    }

    [Fact]
    public void AddWorkingMinutes_ZeroMinutes_FromOutsideShift_StillSnapsForward()
    {
        // CPM'in ilk operasyonu EarliestStartMinutes=0 taşır; bu 0dk kısayolu
        // "start" mesai dışındaysa hâlâ geçerli bir çalışma anına atlamalı,
        // ham (gece) zaman damgasını olduğu gibi döndürmemeli.
        var start = new DateTimeOffset(2026, 8, 3, 22, 40, 0, TimeSpan.Zero); // Monday 22:40
        var expected = new DateTimeOffset(2026, 8, 4, 8, 0, 0, TimeSpan.Zero); // Tuesday 08:00

        var result = _calendar.AddWorkingMinutes(start, 0);

        Assert.Equal(expected, result);
    }

    // Senaryo 1 — mesai içi, moladan önce: 09:00 + 120 dk = 11:00 (aynı gün, molaya değmeden).
    [Fact]
    public void AddWorkingMinutes_WithinShiftBeforeBreak_StaysSameDay()
    {
        var start = new DateTimeOffset(2026, 8, 3, 9, 0, 0, TimeSpan.Zero); // Monday 09:00
        var expected = new DateTimeOffset(2026, 8, 3, 11, 0, 0, TimeSpan.Zero); // Monday 11:00

        var result = _calendar.AddWorkingMinutes(start, 120);

        Assert.Equal(expected, result);
    }

    // Senaryo 2 — molaya denk gelen: 11:30 + 90 dk = 14:00 (12:00-13:00 arası atlanır).
    [Fact]
    public void AddWorkingMinutes_SpanningBreak_SkipsBreakWindow()
    {
        var start = new DateTimeOffset(2026, 8, 3, 11, 30, 0, TimeSpan.Zero); // Monday 11:30
        var expected = new DateTimeOffset(2026, 8, 3, 14, 0, 0, TimeSpan.Zero); // Monday 14:00

        var result = _calendar.AddWorkingMinutes(start, 90);

        Assert.Equal(expected, result);
    }

    // Senaryo 3 — mesai dışına taşan: 15:00 + 180 dk = ertesi gün 09:00.
    [Fact]
    public void AddWorkingMinutes_OverflowsPastShiftEnd_RollsToNextWorkingDay()
    {
        var start = new DateTimeOffset(2026, 8, 3, 15, 0, 0, TimeSpan.Zero); // Monday 15:00
        var expected = new DateTimeOffset(2026, 8, 4, 9, 0, 0, TimeSpan.Zero); // Tuesday 09:00

        var result = _calendar.AddWorkingMinutes(start, 180);

        Assert.Equal(expected, result);
    }

    // Senaryo 4 — mesai dışında (gece) başlayan, orijinal bug: 18:10 + 60 dk = ertesi gün 09:00.
    [Fact]
    public void AddWorkingMinutes_StartOutsideShift_SnapsToNextWorkingDayStart()
    {
        var start = new DateTimeOffset(2026, 8, 3, 18, 10, 0, TimeSpan.Zero); // Monday 18:10
        var expected = new DateTimeOffset(2026, 8, 4, 9, 0, 0, TimeSpan.Zero); // Tuesday 09:00

        var result = _calendar.AddWorkingMinutes(start, 60);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void AddWorkingMinutes_StartBeforeShiftStart_SnapsToShiftStartSameDay()
    {
        var start = new DateTimeOffset(2026, 8, 3, 6, 0, 0, TimeSpan.Zero); // Monday 06:00
        var expected = new DateTimeOffset(2026, 8, 3, 9, 0, 0, TimeSpan.Zero); // Monday 08:00 + 60dk

        var result = _calendar.AddWorkingMinutes(start, 60);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void AddWorkingMinutes_StartOnWeekend_SnapsToMonday()
    {
        var start = new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero); // Saturday 10:00
        var expected = new DateTimeOffset(2026, 8, 3, 9, 0, 0, TimeSpan.Zero); // Monday 08:00 + 60dk

        var result = _calendar.AddWorkingMinutes(start, 60);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void AddWorkingMinutes_StartInsideBreak_SnapsToBreakEnd()
    {
        var start = new DateTimeOffset(2026, 8, 3, 12, 30, 0, TimeSpan.Zero); // Monday 12:30 (mola içinde)
        var expected = new DateTimeOffset(2026, 8, 3, 13, 30, 0, TimeSpan.Zero); // 13:00 + 30dk

        var result = _calendar.AddWorkingMinutes(start, 30);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void AddWorkingMinutes_FridayOverflow_SkipsWeekendAndBreak()
    {
        var start = new DateTimeOffset(2026, 8, 7, 16, 30, 0, TimeSpan.Zero); // Friday 16:30 (30dk kaldı, 17:00'a kadar)
        var expected = new DateTimeOffset(2026, 8, 10, 8, 30, 0, TimeSpan.Zero); // Monday 08:30

        var result = _calendar.AddWorkingMinutes(start, 60);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void AddWorkingMinutes_ExactlyReachesBreakStart_DoesNotSkipPastBreak()
    {
        // 09:00'dan tam olarak molanın başladığı ana (12:00) kadar süren bir operasyon,
        // 13:00'e atlamaz — "mola başlarken bitti" geçerli, anlamlı bir zaman damgasıdır.
        var start = new DateTimeOffset(2026, 8, 3, 9, 0, 0, TimeSpan.Zero); // Monday 09:00
        var expected = new DateTimeOffset(2026, 8, 3, 12, 0, 0, TimeSpan.Zero);

        var result = _calendar.AddWorkingMinutes(start, 180);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void AddWorkingMinutes_ExactlyReachesShiftEnd_DoesNotRollToNextDay()
    {
        var start = new DateTimeOffset(2026, 8, 3, 13, 0, 0, TimeSpan.Zero); // Monday 13:00
        var expected = new DateTimeOffset(2026, 8, 3, 17, 0, 0, TimeSpan.Zero);

        var result = _calendar.AddWorkingMinutes(start, 240);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void AddWorkingMinutes_MultiDayOverflow_SkipsWeekendAndEachDailyBreak()
    {
        // 1440 dk = tam 3 iş günü net kapasitesi (3 x 480). Cuma 10:00'dan başlayınca
        // hafta sonu ve her günün molası atlanarak Çarşamba 10:00'a ulaşılmalı.
        var start = new DateTimeOffset(2026, 8, 7, 10, 0, 0, TimeSpan.Zero); // Friday 10:00
        var expected = new DateTimeOffset(2026, 8, 12, 10, 0, 0, TimeSpan.Zero); // Wednesday 10:00

        var result = _calendar.AddWorkingMinutes(start, 1440);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(480, 1.0)]
    [InlineData(960, 2.0)]
    [InlineData(240, 0.5)]
    [InlineData(0, 0.0)]
    public void ToDisplayWorkingDays_ReturnsCorrectDecimal(long minutes, decimal expectedDays)
    {
        var result = _calendar.ToDisplayWorkingDays(minutes);

        Assert.Equal(expectedDays, result);
    }

    [Fact]
    public void ToDisplayWorkingDays_WithNegativeMinutes_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _calendar.ToDisplayWorkingDays(-1));
    }
}
