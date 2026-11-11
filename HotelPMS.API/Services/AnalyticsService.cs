using HotelPMS.API.Data;
using HotelPMS.API.Dtos;
using HotelPMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelPMS.API.Services;

/// <summary>Расчёт финансовых показателей гостиницы: Occupancy, ADR, RevPAR, выручка.</summary>
public class AnalyticsService
{
    private readonly AppDbContext _db;
    public AnalyticsService(AppDbContext db) => _db = db;

    private static readonly BookingStatus[] Counted = { BookingStatus.CheckedIn, BookingStatus.Completed };

    /// <summary>Число ночей брони, попадающих в период [from, to).</summary>
    public static int OverlapNights(Booking b, DateOnly from, DateOnly to)
    {
        var start = b.CheckInDate > from ? b.CheckInDate : from;
        var end = b.CheckOutDate < to ? b.CheckOutDate : to;
        return Math.Max(0, end.DayNumber - start.DayNumber);
    }

    /// <summary>Стоимость проживания и допуслуг, начисленная по брони.</summary>
    public static BillDto BuildBill(Booking b)
    {
        var room = b.Nights * b.PricePerNight;
        var services = b.Services.Sum(s => s.Quantity * s.UnitPrice);
        var paid = b.Transactions.Sum(t => t.Amount);
        return new BillDto(b.Id, b.Nights, b.PricePerNight, room, services, room + services, paid, room + services - paid);
    }

    /// <summary>KPI за период [from, to) — to не включается.</summary>
    public async Task<KpiReport> GetKpiAsync(DateOnly from, DateOnly to)
    {
        if (to <= from) throw new ArgumentException("Конец периода должен быть позже начала.");

        int days = to.DayNumber - from.DayNumber;
        // Номера на тех. обслуживании не считаются доступными к продаже
        int rooms = await _db.Rooms.CountAsync(r => r.Status != RoomStatus.Maintenance);
        int available = rooms * days;

        var bookings = await _db.Bookings
            .Where(b => Counted.Contains(b.Status) && b.CheckInDate < to && b.CheckOutDate > from)
            .ToListAsync();

        int sold = bookings.Sum(b => OverlapNights(b, from, to));
        decimal roomRevenue = bookings.Sum(b => OverlapNights(b, from, to) * b.PricePerNight);

        var fromDt = from.ToDateTime(TimeOnly.MinValue);
        var toDt = to.ToDateTime(TimeOnly.MinValue);
        decimal servicesRevenue = await _db.ClientServices
            .Where(s => s.ProvidedAt >= fromDt && s.ProvidedAt < toDt
                        && Counted.Contains(s.Booking!.Status))
            .SumAsync(s => (decimal?)(s.Quantity * s.UnitPrice)) ?? 0m;

        decimal occupancy = available == 0 ? 0 : Math.Round(100m * sold / available, 2);
        decimal adr = sold == 0 ? 0 : Math.Round(roomRevenue / sold, 2);
        decimal revpar = available == 0 ? 0 : Math.Round(roomRevenue / available, 2);

        return new KpiReport(from, to, available, sold, roomRevenue, servicesRevenue,
            roomRevenue + servicesRevenue, occupancy, adr, revpar);
    }

    /// <summary>Выручка от номеров по месяцам и категориям — для таблиц и графиков.</summary>
    public async Task<List<MonthRevenueRow>> GetRevenueByMonthAsync(int year)
    {
        var yearStart = new DateOnly(year, 1, 1);
        var yearEnd = new DateOnly(year + 1, 1, 1);

        var bookings = await _db.Bookings
            .Include(b => b.Room!).ThenInclude(r => r.RoomType)
            .Where(b => Counted.Contains(b.Status) && b.CheckInDate < yearEnd && b.CheckOutDate > yearStart)
            .ToListAsync();

        var rows = new List<MonthRevenueRow>();
        for (int m = 1; m <= 12; m++)
        {
            var from = new DateOnly(year, m, 1);
            var to = from.AddMonths(1);
            foreach (var g in bookings.GroupBy(b => b.Room!.RoomType!.Name))
            {
                var sum = g.Sum(b => OverlapNights(b, from, to) * b.PricePerNight);
                rows.Add(new MonthRevenueRow($"{year}-{m:D2}", g.Key, sum));
            }
        }
        return rows;
    }
}
