namespace HotelPMS.API.Dtos;

public record GuestDto(string FullName, string Phone, string PassportData);
public record RoomDto(string Number, int Floor, int RoomTypeId);
public record RoomTypeDto(string Name, string Description, int Capacity, decimal BasePricePerNight);
public record CreateBookingDto(int GuestId, int RoomId, DateOnly CheckInDate, DateOnly CheckOutDate);
public record AddServiceDto(int ServiceId, int Quantity);
public record PaymentDto(decimal Amount, Models.PaymentType PaymentType);
public record ServiceDto(string Name, decimal Price);

public record BillDto(
    int BookingId, int Nights, decimal PricePerNight,
    decimal RoomCharge, decimal ServicesCharge, decimal Total,
    decimal Paid, decimal Balance);

public record KpiReport(
    DateOnly From, DateOnly To,
    int AvailableRoomNights, int SoldRoomNights,
    decimal RoomRevenue, decimal ServicesRevenue, decimal TotalRevenue,
    decimal OccupancyRate, decimal Adr, decimal RevPar);

public record MonthRevenueRow(string Month, string RoomType, decimal RoomRevenue);
