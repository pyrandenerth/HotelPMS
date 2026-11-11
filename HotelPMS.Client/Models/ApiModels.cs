namespace HotelPMS.Client.Models;

// Модели ответов API (имена полей совпадают с JSON сервера, регистр не важен)
public record RoomRow(int Id, string Number, int Floor, string Type, decimal BasePricePerNight, string Status);
public record AvailableRoom(int Id, string Number, string Type, decimal BasePricePerNight)
{
    public string Display => $"{Number} · {Type} · {BasePricePerNight:0.##}/сут";
}
public record GuestItem(int Id, string FullName, string Phone, string PassportData);
public record ServiceItem(int Id, string Name, decimal Price)
{
    public string Display => $"{Name} ({Price:0.##})";
}
public record BookingRow(int Id, string Guest, string Room, DateOnly CheckInDate, DateOnly CheckOutDate, string Status);
public record Bill(int BookingId, int Nights, decimal PricePerNight, decimal RoomCharge,
                   decimal ServicesCharge, decimal Total, decimal Paid, decimal Balance);
public record KpiReport(DateOnly From, DateOnly To, int AvailableRoomNights, int SoldRoomNights,
                        decimal RoomRevenue, decimal ServicesRevenue, decimal TotalRevenue,
                        decimal OccupancyRate, decimal Adr, decimal RevPar);
public record MonthRevenueRow(string Month, string RoomType, decimal RoomRevenue);
