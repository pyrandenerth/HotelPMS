using HotelPMS.API.Data;
using HotelPMS.API.Dtos;
using HotelPMS.API.Models;
using HotelPMS.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelPMS.API.Controllers;

[ApiController, Route("api/room-types")]
public class RoomTypesController : ControllerBase
{
    private readonly AppDbContext _db;
    public RoomTypesController(AppDbContext db) => _db = db;

    [HttpGet] public async Task<IEnumerable<RoomType>> Get() => await _db.RoomTypes.AsNoTracking().ToListAsync();

    [HttpPost]
    public async Task<ActionResult<RoomType>> Create(RoomTypeDto d)
    {
        var t = new RoomType { Name = d.Name, Description = d.Description, Capacity = d.Capacity, BasePricePerNight = d.BasePricePerNight };
        _db.RoomTypes.Add(t);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = t.Id }, t);
    }
}

[ApiController, Route("api/rooms")]
public class RoomsController : ControllerBase
{
    private readonly AppDbContext _db;
    public RoomsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get() =>
        Ok(await _db.Rooms.AsNoTracking().Include(r => r.RoomType)
            .Select(r => new { r.Id, r.Number, r.Floor, Type = r.RoomType!.Name, r.RoomType.BasePricePerNight, Status = r.Status.ToString() })
            .ToListAsync());

    [HttpPost]
    public async Task<ActionResult<Room>> Create(RoomDto d)
    {
        var r = new Room { Number = d.Number, Floor = d.Floor, RoomTypeId = d.RoomTypeId };
        _db.Rooms.Add(r);
        await _db.SaveChangesAsync();
        return Ok(r);
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> SetStatus(int id, [FromBody] RoomStatus status)
    {
        var r = await _db.Rooms.FindAsync(id);
        if (r is null) return NotFound();
        r.Status = status;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Свободные на период номера (без пересечения с активными бронями).</summary>
    [HttpGet("available")]
    public async Task<IActionResult> Available(DateOnly from, DateOnly to)
    {
        if (to <= from) return BadRequest("Дата выезда должна быть позже даты заезда.");
        var busy = _db.Bookings
            .Where(b => (b.Status == BookingStatus.Booked || b.Status == BookingStatus.CheckedIn)
                        && b.CheckInDate < to && b.CheckOutDate > from)
            .Select(b => b.RoomId);
        var list = await _db.Rooms.AsNoTracking().Include(r => r.RoomType)
            .Where(r => r.Status != RoomStatus.Maintenance && !busy.Contains(r.Id))
            .Select(r => new { r.Id, r.Number, Type = r.RoomType!.Name, r.RoomType.BasePricePerNight })
            .ToListAsync();
        return Ok(list);
    }
}

[ApiController, Route("api/guests")]
public class GuestsController : ControllerBase
{
    private readonly AppDbContext _db;
    public GuestsController(AppDbContext db) => _db = db;

    [HttpGet] public async Task<IEnumerable<Guest>> Get([FromQuery] string? search) =>
        await _db.Guests.AsNoTracking()
            .Where(g => search == null || g.FullName.Contains(search))
            .ToListAsync();

    [HttpPost]
    public async Task<ActionResult<Guest>> Create(GuestDto d)
    {
        var g = new Guest { FullName = d.FullName, Phone = d.Phone, PassportData = d.PassportData };
        _db.Guests.Add(g);
        await _db.SaveChangesAsync();
        return Ok(g);
    }
}

[ApiController, Route("api/services")]
public class ServicesController : ControllerBase
{
    private readonly AppDbContext _db;
    public ServicesController(AppDbContext db) => _db = db;

    [HttpGet] public async Task<IEnumerable<Service>> Get() => await _db.Services.AsNoTracking().ToListAsync();

    [HttpPost]
    public async Task<ActionResult<Service>> Create(ServiceDto d)
    {
        var s = new Service { Name = d.Name, Price = d.Price };
        _db.Services.Add(s);
        await _db.SaveChangesAsync();
        return Ok(s);
    }
}

[ApiController, Route("api/bookings")]
public class BookingsController : ControllerBase
{
    private readonly AppDbContext _db;
    public BookingsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get() =>
        Ok(await _db.Bookings.AsNoTracking()
            .Select(b => new { b.Id, Guest = b.Guest!.FullName, Room = b.Room!.Number, b.CheckInDate, b.CheckOutDate, Status = b.Status.ToString() })
            .ToListAsync());

    /// <summary>Создание брони с проверкой пересечения дат по номеру.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateBookingDto d)
    {
        if (d.CheckOutDate <= d.CheckInDate) return BadRequest("Дата выезда должна быть позже даты заезда.");

        var room = await _db.Rooms.Include(r => r.RoomType).FirstOrDefaultAsync(r => r.Id == d.RoomId);
        if (room is null) return NotFound("Номер не найден.");
        if (room.Status == RoomStatus.Maintenance) return Conflict("Номер на техническом обслуживании.");
        if (!await _db.Guests.AnyAsync(g => g.Id == d.GuestId)) return NotFound("Гость не найден.");

        bool overlap = await _db.Bookings.AnyAsync(b => b.RoomId == d.RoomId
            && (b.Status == BookingStatus.Booked || b.Status == BookingStatus.CheckedIn)
            && b.CheckInDate < d.CheckOutDate && b.CheckOutDate > d.CheckInDate);
        if (overlap) return Conflict("Номер уже забронирован на выбранные даты.");

        var booking = new Booking
        {
            GuestId = d.GuestId, RoomId = d.RoomId,
            CheckInDate = d.CheckInDate, CheckOutDate = d.CheckOutDate,
            PricePerNight = room.RoomType!.BasePricePerNight,
            Status = BookingStatus.Booked
        };
        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();
        return Ok(new { booking.Id });
    }

    [HttpPost("{id:int}/check-in")]
    public async Task<IActionResult> CheckIn(int id)
    {
        var b = await _db.Bookings.Include(x => x.Room).FirstOrDefaultAsync(x => x.Id == id);
        if (b is null) return NotFound();
        if (b.Status != BookingStatus.Booked) return Conflict("Заселение возможно только для активной брони.");
        b.Status = BookingStatus.CheckedIn;
        b.Room!.Status = RoomStatus.Occupied;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Выселение: возвращает итоговый счёт; номер уходит на уборку.</summary>
    [HttpPost("{id:int}/check-out")]
    public async Task<IActionResult> CheckOut(int id)
    {
        var b = await _db.Bookings.Include(x => x.Room).Include(x => x.Services).Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (b is null) return NotFound();
        if (b.Status != BookingStatus.CheckedIn) return Conflict("Выселить можно только заселённого гостя.");
        b.Status = BookingStatus.Completed;
        b.Room!.Status = RoomStatus.Cleaning;
        await _db.SaveChangesAsync();
        return Ok(AnalyticsService.BuildBill(b));
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var b = await _db.Bookings.FindAsync(id);
        if (b is null) return NotFound();
        if (b.Status != BookingStatus.Booked) return Conflict("Отменить можно только не начатую бронь.");
        b.Status = BookingStatus.Cancelled;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id:int}/bill")]
    public async Task<IActionResult> Bill(int id)
    {
        var b = await _db.Bookings.AsNoTracking().Include(x => x.Services).Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.Id == id);
        return b is null ? NotFound() : Ok(AnalyticsService.BuildBill(b));
    }

    /// <summary>Привязка допуслуги к счёту проживающего гостя.</summary>
    [HttpPost("{id:int}/services")]
    public async Task<IActionResult> AddService(int id, AddServiceDto d)
    {
        if (d.Quantity <= 0) return BadRequest("Количество должно быть положительным.");
        var b = await _db.Bookings.FindAsync(id);
        if (b is null) return NotFound();
        if (b.Status != BookingStatus.CheckedIn) return Conflict("Услуги доступны только проживающим гостям.");
        var s = await _db.Services.FindAsync(d.ServiceId);
        if (s is null) return NotFound("Услуга не найдена.");

        _db.ClientServices.Add(new ClientService { BookingId = id, ServiceId = s.Id, Quantity = d.Quantity, UnitPrice = s.Price });
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/payments")]
    public async Task<IActionResult> Pay(int id, PaymentDto d)
    {
        if (d.Amount <= 0) return BadRequest("Сумма должна быть положительной.");
        var b = await _db.Bookings.FindAsync(id);
        if (b is null) return NotFound();
        if (b.Status == BookingStatus.Cancelled) return Conflict("Бронь отменена.");
        _db.Transactions.Add(new FinancialTransaction { BookingId = id, Amount = d.Amount, PaymentType = d.PaymentType });
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

[ApiController, Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly AnalyticsService _analytics;
    public ReportsController(AnalyticsService analytics) => _analytics = analytics;

    /// <summary>GET /api/reports/kpi?from=2026-11-01&amp;to=2026-12-01 (to не включается)</summary>
    [HttpGet("kpi")]
    public async Task<IActionResult> Kpi(DateOnly from, DateOnly to)
    {
        if (to <= from) return BadRequest("Конец периода должен быть позже начала.");
        return Ok(await _analytics.GetKpiAsync(from, to));
    }

    [HttpGet("revenue-by-month")]
    public async Task<IActionResult> RevenueByMonth(int year) => Ok(await _analytics.GetRevenueByMonthAsync(year));
}
