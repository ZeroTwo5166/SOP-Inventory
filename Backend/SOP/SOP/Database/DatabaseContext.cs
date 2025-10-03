using SOP.Encryption;
using SOP.DTOs;
using Microsoft.EntityFrameworkCore;
using SOP.Archive.Entities;

namespace SOP.Database
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }

        // DbSets from both files
        public DbSet<Address> Address { get; set; }
        public DbSet<Building> Building { get; set; }
        public DbSet<Computer> Computer { get; set; }
        public DbSet<Computer_ComputerPart> Computer_ComputerPart { get; set; }
        public DbSet<ComputerPart> ComputerPart { get; set; }
        public DbSet<Item> Item { get; set; }
        public DbSet<ItemGroup> ItemGroup { get; set; }
        public DbSet<ItemType> ItemType { get; set; }
        public DbSet<Loan> Loan { get; set; }
        public DbSet<PartGroup> PartGroup { get; set; }
        public DbSet<PartType> PartType { get; set; }
        public DbSet<Request> Request { get; set; }
        public DbSet<Role> Role { get; set; }
        public DbSet<Room> Room { get; set; }
        public DbSet<Status> Status { get; set; }
        public DbSet<StatusHistory> StatusHistory { get; set; }
        public DbSet<User> User { get; set; }

        // Archive DbSets from first file
        public DbSet<Archive_Item> Archive_Item { get; set; }
        public DbSet<Archive_ItemType> Archive_ItemType { get; set; }
        public DbSet<Archive_ItemGroup> Archive_ItemGroup { get; set; }
        public DbSet<Archive_Loan> Archive_Loan { get; set; }
        public DbSet<Archive_Request> Archive_Request { get; set; }
        public DbSet<Archive_StatusHistory> Archive_StatusHistory { get; set; }
        public DbSet<Archive_User> Archive_User { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =========================
            // Keys / Value Generation
            // =========================

            modelBuilder.Entity<Address>()
                .Property(a => a.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Item>()
                .Property(i => i.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<ItemGroup>()
                .Property(i => i.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<ItemType>()
                .Property(i => i.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Loan>()
                .Property(l => l.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Request>()
                .Property(r => r.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<StatusHistory>()
                .Property(s => s.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<User>()
                .Property(u => u.Id)
                .ValueGeneratedOnAdd();

            // Archive tables use existing ids (no auto-gen)
            modelBuilder.Entity<Archive_Item>()
                .Property(i => i.Id)
                .ValueGeneratedNever();

            modelBuilder.Entity<Archive_ItemGroup>()
                .Property(i => i.Id)
                .ValueGeneratedNever();

            modelBuilder.Entity<Archive_ItemType>()
                .Property(i => i.Id)
                .ValueGeneratedNever();

            modelBuilder.Entity<Archive_Loan>()
                .Property(l => l.Id)
                .ValueGeneratedNever();

            modelBuilder.Entity<Archive_Request>()
                .Property(r => r.Id)
                .ValueGeneratedNever();

            modelBuilder.Entity<Archive_StatusHistory>()
                .Property(s => s.Id)
                .ValueGeneratedNever();

            modelBuilder.Entity<Archive_User>()
                .Property(u => u.Id)
                .ValueGeneratedNever();

            // Computer uses Item's PK as FK (table-per-type-like)
            modelBuilder.Entity<Item>()
                .HasKey(i => i.Id);

            modelBuilder.Entity<Computer>()
                .Property(c => c.Id)
                .ValueGeneratedNever();

            // =========================
            // RELATIONSHIPS (RESTRICT)
            // =========================

            // Address (1) -> Building (many)
            modelBuilder.Entity<Building>()
                .HasOne(b => b.Address)
                .WithMany() // keep if Address doesn't expose ICollection<Building> Buildings
                .HasForeignKey(b => b.AddressId)
                .OnDelete(DeleteBehavior.Restrict);

            // Building (1) -> Room (many)
            modelBuilder.Entity<Room>()
                .HasOne(r => r.Building)
                .WithMany() // keep if Building doesn't expose ICollection<Room> Rooms
                .HasForeignKey(r => r.BuildingId)
                .OnDelete(DeleteBehavior.Restrict);

            // Room (1) -> Item (many)  **FIX: point to real inverse nav to avoid RoomId1**
            modelBuilder.Entity<Item>()
                .HasOne(i => i.Room)
                .WithMany(r => r.Items)                 // <— Room.Items must exist
                .HasForeignKey(i => i.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            // ItemGroup (1) -> Item (many) (leave inverse blank if ItemGroup.Items doesn't exist)
            modelBuilder.Entity<Item>()
                .HasOne(i => i.ItemGroup)
                .WithMany() // or .WithMany(g => g.Items) if you have it
                .HasForeignKey(i => i.ItemGroupId)
                .OnDelete(DeleteBehavior.Restrict);

            // ItemType (1) -> ItemGroup (many)
            modelBuilder.Entity<ItemGroup>()
                .HasOne(ig => ig.ItemType)
                .WithMany() // or .WithMany(it => it.ItemGroups) if you have it
                .HasForeignKey(ig => ig.ItemTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Loan -> User (many-to-one)  **FIX: point to User.Loans to avoid UserId1**
            modelBuilder.Entity<Loan>()
                .HasOne(l => l.User)
                .WithMany(u => u.Loans)                 // <— User.Loans must exist
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Item ↔ Loan **FIX: make it one-to-one** (you use Item.Loan everywhere)
            // This replaces the previous "Loan -> Item .WithMany()" mapping.
            modelBuilder.Entity<Item>()
                .HasOne(i => i.Loan)                    // <— Item.Loan (singular) must exist
                .WithOne(l => l.Item)                   // <— Loan.Item must exist
                .HasForeignKey<Loan>(l => l.ItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // StatusHistory -> Item  **FIX: point to Item.StatusHistories to avoid ItemId1 on SH**
            modelBuilder.Entity<StatusHistory>()
                .HasOne(sh => sh.Item)
                .WithMany(i => i.StatusHistories)       // <— Item.StatusHistories must exist
                .HasForeignKey(sh => sh.ItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // StatusHistory -> Status (keep inverse blank if Status doesn't expose a collection)
            modelBuilder.Entity<StatusHistory>()
                .HasOne(sh => sh.Status)
                .WithMany()
                .HasForeignKey(sh => sh.StatusId)
                .OnDelete(DeleteBehavior.Restrict);

            // Request -> User (keep inverse blank unless you have User.Requests)
            modelBuilder.Entity<Request>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // PartGroup -> PartType
            modelBuilder.Entity<PartGroup>()
                .HasOne(pg => pg.PartType)
                .WithMany()
                .HasForeignKey(pg => pg.PartTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // ComputerPart -> PartGroup
            modelBuilder.Entity<ComputerPart>()
                .HasOne(cp => cp.PartGroup)
                .WithMany() // or .WithMany(pg => pg.ComputerParts) if you have it
                .HasForeignKey(cp => cp.PartGroupId)
                .OnDelete(DeleteBehavior.Restrict);

            // Computer (child) -> Item (parent) via same PK
            modelBuilder.Entity<Computer>()
                .HasOne(c => c.Item)
                .WithMany()
                .HasForeignKey(c => c.Id)
                .OnDelete(DeleteBehavior.Restrict);

            // Computer -> Computer_ComputerPart (join)
            modelBuilder.Entity<Computer>()
                .HasMany(c => c.Computer_ComputerParts)
                .WithOne(ccp => ccp.Computer)
                .HasForeignKey(ccp => ccp.ComputerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Join -> ComputerPart (one-to-one link, block deleting part if joined)
            modelBuilder.Entity<Computer_ComputerPart>()
                .HasOne(ccp => ccp.ComputerPart)
                .WithOne(cp => cp.Computer_ComputerPart)
                .HasForeignKey<Computer_ComputerPart>(ccp => ccp.ComputerPartId)
                .OnDelete(DeleteBehavior.Restrict);

            // =========================
            // Seeding
            // =========================

            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Admin", Description = "Administrator" },
                new Role { Id = 2, Name = "Instruktør", Description = "Instruktør" },
                new Role { Id = 3, Name = "Elev", Description = "Elev" },
                new Role { Id = 4, Name = "Drift", Description = "Drift" }
            );

            modelBuilder.Entity<Address>().HasData(
                new Address { Id = 1, ZipCode = 2750, City = "Ballerup", Region = "Sjælland", Road = "Telegrafvej 9" },
                new Address { Id = 2, ZipCode = 2650, City = "Hvidovre", Region = "Sjælland", Road = "Stamholmen 193, 215" },
                new Address { Id = 3, ZipCode = 2000, City = "Frederiksberg", Region = "Sjælland", Road = "Stæhr Johansens Vej 7" },
                new Address { Id = 4, ZipCode = 2860, City = "Gladsaxe", Region = "Sjælland", Road = "Tobaksvejen 19" },
                new Address { Id = 5, ZipCode = 2800, City = "Lyngby", Region = "Sjælland", Road = "Gyrithe Lemches Vej 14" }
            );

            modelBuilder.Entity<Building>().HasData(
                new Building { Id = 1, BuildingName = "A", AddressId = 3 },
                new Building { Id = 2, BuildingName = "C", AddressId = 2 }
            );

            modelBuilder.Entity<Room>().HasData(
                new Room { Id = 1, BuildingId = 1, RoomNumber = 1 },
                new Room { Id = 2, BuildingId = 2, RoomNumber = 19 },
                new Room { Id = 3, BuildingId = 1, RoomNumber = 3 },
                new Room { Id = 4, BuildingId = 2, RoomNumber = 17 },
                new Room { Id = 5, BuildingId = 1, RoomNumber = 4 },
                new Room { Id = 6, BuildingId = 2, RoomNumber = 15 }
            );

            modelBuilder.Entity<Status>().HasData(
                new Status { Id = 1, Name = "Virker" },
                new Status { Id = 2, Name = "Gik stykker" },
                new Status { Id = 3, Name = "Under service" },
                new Status { Id = 4, Name = "Udlånt" }
            );

            modelBuilder.Entity<ItemType>().HasData(
                new ItemType { Id = 1, TypeName = "Computer" },
                new ItemType { Id = 2, TypeName = "Bord" },
                new ItemType { Id = 3, TypeName = "Stole" },
                new ItemType { Id = 4, TypeName = "Skærm" },
                new ItemType { Id = 5, TypeName = "Tastatur" }
            );

            modelBuilder.Entity<ItemGroup>().HasData(
                new ItemGroup { Id = 1, ItemTypeId = 1, ModelName = "Acer Nitro 5", Price = 9875.99m, Manufacturer = "Acer", WarrantyPeriod = "3 år", Quantity = 30 },
                new ItemGroup { Id = 2, ItemTypeId = 2, ModelName = "SANDSBERG ", Price = 299.00m, Manufacturer = "IKEA", WarrantyPeriod = "2 år", Quantity = 20 },
                new ItemGroup { Id = 3, ItemTypeId = 1, ModelName = "HP Envy x360", Price = 11249.50m, Manufacturer = "HP", WarrantyPeriod = "2 år", Quantity = 15 },
                new ItemGroup { Id = 4, ItemTypeId = 3, ModelName = "MARKUS", Price = 1395.00m, Manufacturer = "IKEA", WarrantyPeriod = "10 år", Quantity = 12 },
                new ItemGroup { Id = 5, ItemTypeId = 4, ModelName = "Dell UltraSharp U2723QE", Price = 5299.99m, Manufacturer = "Dell", WarrantyPeriod = "3 år", Quantity = 10 },
                new ItemGroup { Id = 6, ItemTypeId = 5, ModelName = "Logitech MX Keys", Price = 999.00m, Manufacturer = "Logitech", WarrantyPeriod = "2 år", Quantity = 25 }
            );

            modelBuilder.Entity<Item>().HasData(
                new Item { Id = 1, RoomId = 1, ItemGroupId = 1, SerialNumber = "ACN5-001A" },
                new Item { Id = 2, RoomId = 3, ItemGroupId = 1, SerialNumber = "ACN5-001B" },
                new Item { Id = 3, RoomId = 5, ItemGroupId = 1, SerialNumber = "ACN5-001C" },
                new Item { Id = 4, RoomId = 2, ItemGroupId = 1, SerialNumber = "ACN5-001D" }
            );

            modelBuilder.Entity<Computer>().HasData(
                new Computer { Id = 1 },
                new Computer { Id = 3 }
            );

            modelBuilder.Entity<StatusHistory>().HasData(
                new StatusHistory { Id = 1, ItemId = 1, StatusId = 1, StatusUpdateDate = new DateTime(2024, 10, 28), Note = "Ny" },
                new StatusHistory { Id = 2, ItemId = 2, StatusId = 1, StatusUpdateDate = new DateTime(2024, 10, 28), Note = "Ny" },
                new StatusHistory { Id = 3, ItemId = 3, StatusId = 2, StatusUpdateDate = new DateTime(2024, 11, 28), Note = "Virke ikke" },
                new StatusHistory { Id = 4, ItemId = 4, StatusId = 2, StatusUpdateDate = new DateTime(2024, 11, 30), Note = "Virke ikke" }
            );

            modelBuilder.Entity<PartGroup>().HasData(
                new PartGroup
                {
                    Id = 1,
                    PartName = "Corsair Vengeance RGB DDR5-6400",
                    Price = 999.00m,
                    Manufacturer = "Corsair",
                    WarrantyPeriod = "3 år",
                    ReleaseDate = new DateTime(2024, 10, 30),
                    Quantity = 30,
                    PartTypeId = 1
                },
                new PartGroup
                {
                    Id = 2,
                    PartName = "ASUS GeForce RTX 4060 DUAL EVO OC",
                    Price = 2368.00m,
                    Manufacturer = "ASUS",
                    WarrantyPeriod = "3 år",
                    ReleaseDate = new DateTime(2024, 10, 30),
                    Quantity = 10,
                    PartTypeId = 2
                }
            );

            modelBuilder.Entity<PartType>().HasData(
                new PartType { Id = 1, PartTypeName = "RAM" },
                new PartType { Id = 2, PartTypeName = "Graffikort" }
            );

            modelBuilder.Entity<ComputerPart>().HasData(
                new ComputerPart { Id = 1, PartGroupId = 1, SerialNumber = "11345134513", ModelNumber = "14123VGE34" },
                new ComputerPart { Id = 2, PartGroupId = 2, SerialNumber = "546873957", ModelNumber = "3456345GB45" },
                new ComputerPart { Id = 3, PartGroupId = 1, SerialNumber = "546873957", ModelNumber = "3456345GB45" }
            );

            modelBuilder.Entity<Computer_ComputerPart>().HasData(
                new Computer_ComputerPart { Id = 1, ComputerId = 1, ComputerPartId = 1 },
                new Computer_ComputerPart { Id = 2, ComputerId = 1, ComputerPartId = 2 },
                new Computer_ComputerPart { Id = 3, ComputerId = 3, ComputerPartId = 3 }
            );

            string salt = BCrypt.Net.BCrypt.GenerateSalt(10);
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Email = EncryptionHelper.Encrypt("admin@tec.dk"),
                    Name = "Admin",
                    Password = BCrypt.Net.BCrypt.HashPassword("1234!", salt, true, BCrypt.Net.HashType.SHA512),
                    RoleId = 1,
                    TwoFactorAuthentication = true,
                    TwoFactorSecretKey = ""
                },
                new User
                {
                    Id = 2,
                    Email = EncryptionHelper.Encrypt("drift@tec.dk"),
                    Name = "Drift",
                    Password = BCrypt.Net.BCrypt.HashPassword("1234!", salt, true, BCrypt.Net.HashType.SHA512),
                    RoleId = 4,
                    TwoFactorAuthentication = true,
                    TwoFactorSecretKey = ""
                },
                new User
                {
                    Id = 3,
                    Email = EncryptionHelper.Encrypt("instruktør@tec.dk"),
                    Name = "Instruktør",
                    Password = BCrypt.Net.BCrypt.HashPassword("1234!", salt, true, BCrypt.Net.HashType.SHA512),
                    RoleId = 2,
                    TwoFactorAuthentication = true,
                    TwoFactorSecretKey = ""
                },
                new User
                {
                    Id = 4,
                    Email = EncryptionHelper.Encrypt("elev@tec.dk"),
                    Name = "Elev",
                    Password = BCrypt.Net.BCrypt.HashPassword("1234!", salt, true, BCrypt.Net.HashType.SHA512),
                    RoleId = 3,
                    TwoFactorAuthentication = true,
                    TwoFactorSecretKey = ""
                }
            );

            modelBuilder.Entity<Request>().HasData(
                new Request
                {
                    Id = 1,
                    UserId = 1,
                    Message = "I need a laptop for my studies.",
                    Date = new DateTime(2024, 11, 12, 14, 30, 0),
                    RecipientEmail = "admin@tec.dk",
                    Item = "Laptop",
                    Status = "Godkent"
                }
            );

            modelBuilder.Entity<Loan>().HasData(
                new Loan
                {
                    Id = 1,
                    ItemId = 1,
                    UserId = 1,
                    LoanDate = new DateTime(2024, 10, 15, 8, 59, 59),
                    ReturnDate = new DateTime(2026, 6, 29, 14, 59, 59)
                }
            );
        }
    }
}
