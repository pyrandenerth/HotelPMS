using HotelPMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelPMS.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Guest> Guests => Set<Guest>();
    public DbSet<RoomType> RoomTypes => Set<RoomType>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<ClientService> ClientServices => Set<ClientService>();
    public DbSet<FinancialTransaction> Transactions => Set<FinancialTransaction>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Денежные поля
        b.Entity<RoomType>().Property(x => x.BasePricePerNight).HasPrecision(12, 2);
        b.Entity<Booking>().Property(x => x.PricePerNight).HasPrecision(12, 2);
        b.Entity<Service>().Property(x => x.Price).HasPrecision(12, 2);
        b.Entity<ClientService>().Property(x => x.UnitPrice).HasPrecision(12, 2);
        b.Entity<FinancialTransaction>().Property(x => x.Amount).HasPrecision(12, 2);

        // Enum хранятся строками — читаемо в БД
        b.Entity<Room>().Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Entity<Booking>().Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Entity<FinancialTransaction>().Property(x => x.PaymentType).HasConversion<string>().HasMaxLength(20);

        // Ограничения
        b.Entity<Room>().HasIndex(x => x.Number).IsUnique();
        b.Entity<RoomType>().HasIndex(x => x.Name).IsUnique();
        b.Entity<Booking>().ToTable(t => t.HasCheckConstraint("CK_Booking_Dates", "[CheckOutDate] > [CheckInDate]"));
        b.Entity<ClientService>().ToTable(t => t.HasCheckConstraint("CK_ClientService_Qty", "[Quantity] > 0"));
        b.Entity<FinancialTransaction>().ToTable(t => t.HasCheckConstraint("CK_Transaction_Amount", "[Amount] > 0"));

        // Связи: запрет каскадного удаления, чтобы не терять финансовую историю
        b.Entity<Booking>().HasOne(x => x.Guest).WithMany(g => g.Bookings).HasForeignKey(x => x.GuestId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Booking>().HasOne(x => x.Room).WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Room>().HasOne(x => x.RoomType).WithMany(t => t.Rooms).HasForeignKey(x => x.RoomTypeId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ClientService>().HasOne(x => x.Service).WithMany().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict);

        // Начальные данные
        b.Entity<RoomType>().HasData(
            new RoomType { Id = 1, Name = "Стандарт", Description = "Стандартный номер", Capacity = 2, BasePricePerNight = 80 },
            new RoomType { Id = 2, Name = "Полулюкс", Description = "Номер повышенной комфортности", Capacity = 2, BasePricePerNight = 120 },
            new RoomType { Id = 3, Name = "Люкс", Description = "Двухкомнатный люкс", Capacity = 3, BasePricePerNight = 200 },
            new RoomType { Id = 4, Name = "Апартаменты", Description = "Апартаменты с кухней", Capacity = 4, BasePricePerNight = 300 });

        b.Entity<Room>().HasData(
            new Room { Id = 1, Number = "101", Floor = 1, RoomTypeId = 1 },
            new Room { Id = 2, Number = "102", Floor = 1, RoomTypeId = 1 },
            new Room { Id = 3, Number = "201", Floor = 2, RoomTypeId = 2 },
            new Room { Id = 4, Number = "202", Floor = 2, RoomTypeId = 2 },
            new Room { Id = 5, Number = "301", Floor = 3, RoomTypeId = 3 },
            new Room { Id = 6, Number = "401", Floor = 4, RoomTypeId = 4 });

        b.Entity<Service>().HasData(
            new Service { Id = 1, Name = "Ресторан", Price = 25 },
            new Service { Id = 2, Name = "СПА-комплекс", Price = 60 },
            new Service { Id = 3, Name = "Прачечная", Price = 10 },
            new Service { Id = 4, Name = "Парковка (сутки)", Price = 8 },
            new Service { Id = 5, Name = "Трансфер", Price = 35 });
    }
}
