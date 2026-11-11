using System.ComponentModel.DataAnnotations;

namespace HotelPMS.API.Models;

public enum RoomStatus { Free = 0, Occupied = 1, Cleaning = 2, Maintenance = 3 }
public enum BookingStatus { Booked = 0, CheckedIn = 1, Completed = 2, Cancelled = 3 }
public enum PaymentType { Cash = 0, Card = 1 }

public class Guest
{
    public int Id { get; set; }
    [Required, MaxLength(150)] public string FullName { get; set; } = "";
    [MaxLength(30)] public string Phone { get; set; } = "";
    [MaxLength(50)] public string PassportData { get; set; } = "";
    public List<Booking> Bookings { get; set; } = new();
}

public class RoomType
{
    public int Id { get; set; }
    [Required, MaxLength(50)] public string Name { get; set; } = "";   // Стандарт, Полулюкс, Люкс, Апартаменты
    [MaxLength(500)] public string Description { get; set; } = "";
    public int Capacity { get; set; }
    public decimal BasePricePerNight { get; set; }
    public List<Room> Rooms { get; set; } = new();
}

public class Room
{
    public int Id { get; set; }
    [Required, MaxLength(10)] public string Number { get; set; } = "";
    public int Floor { get; set; }
    public int RoomTypeId { get; set; }
    public RoomType? RoomType { get; set; }
    public RoomStatus Status { get; set; } = RoomStatus.Free;
}

public class Booking
{
    public int Id { get; set; }
    public int GuestId { get; set; }
    public Guest? Guest { get; set; }
    public int RoomId { get; set; }
    public Room? Room { get; set; }

    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }   // дата выезда (ночь выезда не оплачивается)
    public decimal PricePerNight { get; set; }   // цена фиксируется на момент брони
    public BookingStatus Status { get; set; } = BookingStatus.Booked;

    public List<ClientService> Services { get; set; } = new();
    public List<FinancialTransaction> Transactions { get; set; } = new();

    public int Nights => CheckOutDate.DayNumber - CheckInDate.DayNumber;
}

public class Service
{
    public int Id { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = "";   // ресторан, СПА, прачечная...
    public decimal Price { get; set; }
}

public class ClientService
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public Booking? Booking { get; set; }
    public int ServiceId { get; set; }
    public Service? Service { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }       // цена фиксируется на момент оказания
    public DateTime ProvidedAt { get; set; } = DateTime.Now;
}

public class FinancialTransaction
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public Booking? Booking { get; set; }
    public decimal Amount { get; set; }
    public PaymentType PaymentType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
