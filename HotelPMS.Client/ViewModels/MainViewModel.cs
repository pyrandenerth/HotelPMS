using System.Collections.ObjectModel;
using System.Windows;
using HotelPMS.Client.Models;
using HotelPMS.Client.Services;

namespace HotelPMS.Client.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly ApiClient _api = new();

    public static string[] RoomStatuses { get; } = { "Free", "Occupied", "Cleaning", "Maintenance" };
    public static string[] PaymentTypes { get; } = { "Cash", "Card" };

    // ---------- Списки ----------
    public ObservableCollection<RoomRow> Rooms { get; } = new();
    public ObservableCollection<GuestItem> Guests { get; } = new();
    public ObservableCollection<ServiceItem> Services { get; } = new();
    public ObservableCollection<BookingRow> Bookings { get; } = new();
    public ObservableCollection<AvailableRoom> AvailableRooms { get; } = new();
    public ObservableCollection<MonthRevenueRow> RevenueRows { get; } = new();

    // ---------- Выбранные элементы ----------
    private RoomRow? _selectedRoom;
    public RoomRow? SelectedRoom { get => _selectedRoom; set => Set(ref _selectedRoom, value); }

    private string _newRoomStatus = "Free";
    public string NewRoomStatus { get => _newRoomStatus; set => Set(ref _newRoomStatus, value); }

    private BookingRow? _selectedBooking;
    public BookingRow? SelectedBooking
    {
        get => _selectedBooking;
        set { if (Set(ref _selectedBooking, value)) _ = LoadBillAsync(); }
    }

    private Bill? _bill;
    public Bill? CurrentBill { get => _bill; set => Set(ref _bill, value); }

    // ---------- Новая бронь ----------
    private GuestItem? _bookingGuest;
    public GuestItem? BookingGuest { get => _bookingGuest; set => Set(ref _bookingGuest, value); }

    private AvailableRoom? _bookingRoom;
    public AvailableRoom? BookingRoom { get => _bookingRoom; set => Set(ref _bookingRoom, value); }

    private DateTime _checkIn = DateTime.Today;
    public DateTime CheckIn { get => _checkIn; set => Set(ref _checkIn, value); }

    private DateTime _checkOut = DateTime.Today.AddDays(1);
    public DateTime CheckOut { get => _checkOut; set => Set(ref _checkOut, value); }

    // ---------- Новый гость ----------
    private string _guestName = "", _guestPhone = "", _guestPassport = "";
    public string GuestName { get => _guestName; set => Set(ref _guestName, value); }
    public string GuestPhone { get => _guestPhone; set => Set(ref _guestPhone, value); }
    public string GuestPassport { get => _guestPassport; set => Set(ref _guestPassport, value); }

    // ---------- Услуги и оплата по выбранной брони ----------
    private ServiceItem? _selectedService;
    public ServiceItem? SelectedService { get => _selectedService; set => Set(ref _selectedService, value); }

    private int _serviceQty = 1;
    public int ServiceQty { get => _serviceQty; set => Set(ref _serviceQty, value); }

    private decimal _payAmount;
    public decimal PayAmount { get => _payAmount; set => Set(ref _payAmount, value); }

    private string _payType = "Cash";
    public string PayType { get => _payType; set => Set(ref _payType, value); }

    // ---------- Отчёты ----------
    private DateTime _reportFrom = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    public DateTime ReportFrom { get => _reportFrom; set => Set(ref _reportFrom, value); }

    private DateTime _reportTo = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1);
    public DateTime ReportTo { get => _reportTo; set => Set(ref _reportTo, value); }

    private KpiReport? _kpi;
    public KpiReport? Kpi { get => _kpi; set => Set(ref _kpi, value); }

    private int _year = DateTime.Today.Year;
    public int Year { get => _year; set => Set(ref _year, value); }

    private string _status = "Готово";
    public string StatusText { get => _status; set => Set(ref _status, value); }

    // ---------- Команды ----------
    public RelayCommand RefreshCommand { get; }
    public RelayCommand SetRoomStatusCommand { get; }
    public RelayCommand CreateGuestCommand { get; }
    public RelayCommand FindRoomsCommand { get; }
    public RelayCommand CreateBookingCommand { get; }
    public RelayCommand CheckInCommand { get; }
    public RelayCommand CheckOutCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand AddServiceCommand { get; }
    public RelayCommand PayCommand { get; }
    public RelayCommand LoadKpiCommand { get; }

    public MainViewModel()
    {
        RefreshCommand = new(_ => Run(LoadAllAsync));
        SetRoomStatusCommand = new(_ => Run(async () =>
        {
            await _api.SetRoomStatus(SelectedRoom!.Id, NewRoomStatus);
            await LoadRoomsAsync();
        }), _ => SelectedRoom is not null);

        CreateGuestCommand = new(_ => Run(async () =>
        {
            await _api.CreateGuest(GuestName, GuestPhone, GuestPassport);
            GuestName = GuestPhone = GuestPassport = "";
            await LoadGuestsAsync();
        }), _ => !string.IsNullOrWhiteSpace(GuestName));

        FindRoomsCommand = new(_ => Run(async () =>
        {
            var list = await _api.GetAvailable(DateOnly.FromDateTime(CheckIn), DateOnly.FromDateTime(CheckOut));
            Fill(AvailableRooms, list);
        }));

        CreateBookingCommand = new(_ => Run(async () =>
        {
            await _api.CreateBooking(BookingGuest!.Id, BookingRoom!.Id,
                DateOnly.FromDateTime(CheckIn), DateOnly.FromDateTime(CheckOut));
            await LoadBookingsAsync();
            AvailableRooms.Clear();
        }), _ => BookingGuest is not null && BookingRoom is not null);

        CheckInCommand = new(_ => Run(async () =>
        {
            await _api.CheckIn(SelectedBooking!.Id);
            await LoadBookingsAsync(); await LoadRoomsAsync();
        }), _ => SelectedBooking?.Status == "Booked");

        CheckOutCommand = new(_ => Run(async () =>
        {
            CurrentBill = await _api.CheckOut(SelectedBooking!.Id);
            await LoadBookingsAsync(); await LoadRoomsAsync();
        }), _ => SelectedBooking?.Status == "CheckedIn");

        CancelCommand = new(_ => Run(async () =>
        {
            await _api.Cancel(SelectedBooking!.Id);
            await LoadBookingsAsync();
        }), _ => SelectedBooking?.Status == "Booked");

        AddServiceCommand = new(_ => Run(async () =>
        {
            await _api.AddService(SelectedBooking!.Id, SelectedService!.Id, ServiceQty);
            await LoadBillAsync();
        }), _ => SelectedBooking?.Status == "CheckedIn" && SelectedService is not null && ServiceQty > 0);

        PayCommand = new(_ => Run(async () =>
        {
            await _api.Pay(SelectedBooking!.Id, PayAmount, PayType);
            await LoadBillAsync();
        }), _ => SelectedBooking is not null && PayAmount > 0);

        LoadKpiCommand = new(_ => Run(async () =>
        {
            Kpi = await _api.GetKpi(DateOnly.FromDateTime(ReportFrom), DateOnly.FromDateTime(ReportTo));
            Fill(RevenueRows, await _api.GetRevenueByMonth(Year));
        }));

        _ = Run(LoadAllAsync);
    }

    // ---------- Загрузка данных ----------
    private async Task LoadAllAsync()
    {
        await LoadRoomsAsync(); await LoadGuestsAsync(); await LoadBookingsAsync();
        Fill(Services, await _api.GetServices());
    }
    private async Task LoadRoomsAsync() => Fill(Rooms, await _api.GetRooms());
    private async Task LoadGuestsAsync() => Fill(Guests, await _api.GetGuests());
    private async Task LoadBookingsAsync() => Fill(Bookings, await _api.GetBookings());

    private async Task LoadBillAsync()
    {
        if (SelectedBooking is null) { CurrentBill = null; return; }
        try { CurrentBill = await _api.GetBill(SelectedBooking.Id); } catch { CurrentBill = null; }
    }

    private static void Fill<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (var i in items) target.Add(i);
    }

    /// <summary>Единая обёртка: показывает ошибки сервера (например, пересечение броней) пользователю.</summary>
    private async Task Run(Func<Task> action)
    {
        try
        {
            StatusText = "Выполняется…";
            await action();
            StatusText = "Готово";
        }
        catch (ApiException ex)
        {
            StatusText = ex.Message;
            MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (HttpRequestException)
        {
            StatusText = "Сервер недоступен";
            MessageBox.Show($"Не удалось подключиться к серверу ({ApiClient.DefaultBaseUrl}). Запущен ли HotelPMS.API?",
                "Нет связи", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
