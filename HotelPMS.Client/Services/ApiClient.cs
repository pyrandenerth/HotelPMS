using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HotelPMS.Client.Models;

namespace HotelPMS.Client.Services;

/// <summary>Клиент REST API сервера. Единственное место, знающее про HTTP.</summary>
public class ApiClient
{
    // Порт взят из HotelPMS.API/Properties/launchSettings.json (профиль https)
    public const string DefaultBaseUrl = "https://localhost:7296/";

    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public ApiClient(string baseUrl = DefaultBaseUrl) =>
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };

    private static async Task EnsureOk(HttpResponseMessage r)
    {
        if (r.IsSuccessStatusCode) return;
        var text = (await r.Content.ReadAsStringAsync()).Trim('"');
        throw new ApiException(string.IsNullOrWhiteSpace(text) ? r.ReasonPhrase ?? "Ошибка сервера" : text);
    }

    private async Task<T> Get<T>(string url)
    {
        var r = await _http.GetAsync(url);
        await EnsureOk(r);
        return (await r.Content.ReadFromJsonAsync<T>(Json))!;
    }

    private async Task<HttpResponseMessage> Send(HttpMethod m, string url, object? body = null)
    {
        var req = new HttpRequestMessage(m, url);
        if (body is not null) req.Content = JsonContent.Create(body, options: Json);
        var r = await _http.SendAsync(req);
        await EnsureOk(r);
        return r;
    }

    private static string D(DateOnly d) => d.ToString("yyyy-MM-dd");

    // Справочники
    public Task<List<RoomRow>> GetRooms() => Get<List<RoomRow>>("api/rooms");
    public Task<List<GuestItem>> GetGuests() => Get<List<GuestItem>>("api/guests");
    public Task<List<ServiceItem>> GetServices() => Get<List<ServiceItem>>("api/services");
    public Task<List<AvailableRoom>> GetAvailable(DateOnly from, DateOnly to) =>
        Get<List<AvailableRoom>>($"api/rooms/available?from={D(from)}&to={D(to)}");

    public async Task SetRoomStatus(int roomId, string status) =>
        await Send(HttpMethod.Put, $"api/rooms/{roomId}/status", status);

    public async Task CreateGuest(string fullName, string phone, string passport) =>
        await Send(HttpMethod.Post, "api/guests", new { fullName, phone, passportData = passport });

    // Бронирования
    public Task<List<BookingRow>> GetBookings() => Get<List<BookingRow>>("api/bookings");

    public async Task CreateBooking(int guestId, int roomId, DateOnly from, DateOnly to) =>
        await Send(HttpMethod.Post, "api/bookings", new { guestId, roomId, checkInDate = D(from), checkOutDate = D(to) });

    public async Task CheckIn(int id) => await Send(HttpMethod.Post, $"api/bookings/{id}/check-in");
    public async Task<Bill> CheckOut(int id) =>
        (await (await Send(HttpMethod.Post, $"api/bookings/{id}/check-out")).Content.ReadFromJsonAsync<Bill>(Json))!;
    public async Task Cancel(int id) => await Send(HttpMethod.Post, $"api/bookings/{id}/cancel");
    public Task<Bill> GetBill(int id) => Get<Bill>($"api/bookings/{id}/bill");

    public async Task AddService(int bookingId, int serviceId, int qty) =>
        await Send(HttpMethod.Post, $"api/bookings/{bookingId}/services", new { serviceId, quantity = qty });

    public async Task Pay(int bookingId, decimal amount, string paymentType) =>
        await Send(HttpMethod.Post, $"api/bookings/{bookingId}/payments", new { amount, paymentType });

    // Отчёты
    public Task<KpiReport> GetKpi(DateOnly from, DateOnly to) =>
        Get<KpiReport>($"api/reports/kpi?from={D(from)}&to={D(to)}");
    public Task<List<MonthRevenueRow>> GetRevenueByMonth(int year) =>
        Get<List<MonthRevenueRow>>($"api/reports/revenue-by-month?year={year}");
}

public class ApiException(string message) : Exception(message);
