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
        public DbSet<Preset> Presets { get; set; }
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

            // Configure Computer relationships from second file
            modelBuilder.Entity<Computer>()
                .HasOne<Item>()
                .WithMany()
                .HasForeignKey(c => c.Id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Item>()
                .HasKey(i => i.Id);

            modelBuilder.Entity<Computer>()
                .HasOne(c => c.Item)
                .WithMany()
                .HasForeignKey(c => c.Id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Computer>()
                .HasMany(c => c.Computer_ComputerParts)
                .WithOne(ccp => ccp.Computer)
                .HasForeignKey(ccp => ccp.ComputerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Computer_ComputerPart>()
                .HasOne(ccp => ccp.ComputerPart)
                .WithOne(cp => cp.Computer_ComputerPart)
                .HasForeignKey<Computer_ComputerPart>(ccp => ccp.ComputerPartId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Address Id as PK
            modelBuilder.Entity<Address>()
                .Property(a => a.Id)
                .ValueGeneratedOnAdd();

            // Configure Computer Id
            modelBuilder.Entity<Computer>()
                .Property(c => c.Id)
                .ValueGeneratedNever();

            // Configure other entities' keys and value generation from first file
            modelBuilder.Entity<Item>()
                .Property(i => i.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<ItemGroup>()
                .Property(i => i.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<ItemType>()
                .Property(i => i.Id)
                .ValueGeneratedOnAdd();

            // Configure Preset <-> ItemType relationship
            modelBuilder.Entity<ItemType>()
                .HasOne(it => it.Preset)
                .WithMany(p => p.ItemTypes)
                .HasForeignKey(it => it.PresetId)
                .OnDelete(DeleteBehavior.Restrict); // Preset is constant/static, don't delete it


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

            // Seed data for Role (merging both files, using first file's comprehensive data)
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Admin", Description = "Administrator" },
                new Role { Id = 2, Name = "Instruktør", Description = "Instruktør" },
                new Role { Id = 3, Name = "Elev", Description = "Elev" },
                new Role { Id = 4, Name = "Drift", Description = "Drift" }
            );

            // Seed data for Address (from first file)
            modelBuilder.Entity<Address>().HasData(
                new Address { Id = 1, ZipCode = 2750, City = "Ballerup", Region = "Sjælland", Road = "Telegrafvej 9" },
                new Address { Id = 2, ZipCode = 2650, City = "Hvidovre", Region = "Sjælland", Road = "Stamholmen 193, 215" },
                new Address { Id = 3, ZipCode = 2000, City = "Frederiksberg", Region = "Sjælland", Road = "Stæhr Johansens Vej 7" },
                new Address { Id = 4, ZipCode = 2860, City = "Gladsaxe", Region = "Sjælland", Road = "Tobaksvejen 19" },
                new Address { Id = 5, ZipCode = 2800, City = "Lyngby", Region = "Sjælland", Road = "Gyrithe Lemches Vej 14" }
            );

            // Seed data for Building (from first file)
            modelBuilder.Entity<Building>().HasData(
                new Building { Id = 1, BuildingName = "A", AddressId = 3 },
                new Building { Id = 2, BuildingName = "C", AddressId = 2 }
            );

            // Seed data for Room (from first file)
            modelBuilder.Entity<Room>().HasData(
                new Room { Id = 1, BuildingId = 1, RoomNumber = 1 },
                new Room { Id = 2, BuildingId = 2, RoomNumber = 19 },
                new Room { Id = 3, BuildingId = 1, RoomNumber = 3 },
                new Room { Id = 4, BuildingId = 2, RoomNumber = 17 },
                new Room { Id = 5, BuildingId = 1, RoomNumber = 4 },
                new Room { Id = 6, BuildingId = 2, RoomNumber = 15 }
            );

            // Seed data for Status (from first file)
            modelBuilder.Entity<Status>().HasData(
                new Status { Id = 1, Name = "Virker" },
                new Status { Id = 2, Name = "Gik stykker" },
                new Status { Id = 3, Name = "Under service" },
                new Status { Id = 4, Name = "Udlånt" }
            );

            // Seed data for ItemType (from first file)
            modelBuilder.Entity<ItemType>().HasData(
                new ItemType { Id = 1, TypeName = "Computer", PresetId = 1 },
                new ItemType { Id = 2, TypeName = "Bord", PresetId = 8 },
                new ItemType { Id = 3, TypeName = "Stole", PresetId = 9 },
                new ItemType { Id = 4, TypeName = "Skærm", PresetId = 3 },   // Assuming screens use the Computer preset
                new ItemType { Id = 5, TypeName = "Tastatur", PresetId = 4 }
            );

            // Seed data for ItemGroup (from first file)
            modelBuilder.Entity<ItemGroup>().HasData(
                new ItemGroup { Id = 1, ItemTypeId = 1, ModelName = "Acer Nitro 5", Price = 9875.99m, Manufacturer = "Acer", WarrantyPeriod = "3 år", Quantity = 30 },
                new ItemGroup { Id = 2, ItemTypeId = 2, ModelName = "SANDSBERG ", Price = 299.00m, Manufacturer = "IKEA", WarrantyPeriod = "2 år", Quantity = 20 },
                new ItemGroup { Id = 3, ItemTypeId = 1, ModelName = "HP Envy x360", Price = 11249.50m, Manufacturer = "HP", WarrantyPeriod = "2 år", Quantity = 15 },
                new ItemGroup { Id = 4, ItemTypeId = 3, ModelName = "MARKUS", Price = 1395.00m, Manufacturer = "IKEA", WarrantyPeriod = "10 år", Quantity = 12 },
                new ItemGroup { Id = 5, ItemTypeId = 4, ModelName = "Dell UltraSharp U2723QE", Price = 5299.99m, Manufacturer = "Dell", WarrantyPeriod = "3 år", Quantity = 10 },
                new ItemGroup { Id = 6, ItemTypeId = 5, ModelName = "Logitech MX Keys", Price = 999.00m, Manufacturer = "Logitech", WarrantyPeriod = "2 år", Quantity = 25 }
            );

            // Seed data for Item (from first file)
            modelBuilder.Entity<Item>().HasData(
                // ItemGroupId = 1 (Acer Nitro 5 - Computer)
                new Item { Id = 1, RoomId = 1, ItemGroupId = 1, SerialNumber = "ACN5-001A" },
                new Item { Id = 2, RoomId = 3, ItemGroupId = 1, SerialNumber = "ACN5-001B" },
                new Item { Id = 3, RoomId = 5, ItemGroupId = 1, SerialNumber = "ACN5-001C" },
                new Item { Id = 4, RoomId = 2, ItemGroupId = 1, SerialNumber = "ACN5-001D" }
            );

            // Seed data for Computer (from second file)
            modelBuilder.Entity<Computer>().HasData(
                new Computer { Id = 1 },
                new Computer { Id = 3 }
            );

            // Seed data for StatusHistory (from first file)
            modelBuilder.Entity<StatusHistory>().HasData(
    new StatusHistory { Id = 1, ItemId = 1, StatusId = 1, StatusUpdateDate = new DateTime(2024, 10, 28), Note = "Ny" },
    new StatusHistory { Id = 2, ItemId = 2, StatusId = 1, StatusUpdateDate = new DateTime(2024, 10, 28), Note = "Ny" },
    new StatusHistory { Id = 3, ItemId = 3, StatusId = 2, StatusUpdateDate = new DateTime(2024, 11, 28), Note = "Virke ikke" },
    new StatusHistory { Id = 4, ItemId = 4, StatusId = 2, StatusUpdateDate = new DateTime(2024, 11, 30), Note = "Virke ikke" }
    // Add more StatusHistory seed data as needed, ensuring each entry is valid and not corrupted.
               );

            // Seed data for PartGroup (from second file)
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

            // Seed data for PartType (from second file)
            modelBuilder.Entity<PartType>().HasData(
                new PartType { Id = 1, PartTypeName = "RAM" },
                new PartType { Id = 2, PartTypeName = "Graffikort" }
            );

            // Seed data for ComputerPart (from second file)
            modelBuilder.Entity<ComputerPart>().HasData(
                new ComputerPart { Id = 1, PartGroupId = 1, SerialNumber = "11345134513", ModelNumber = "14123VGE34" },
                new ComputerPart { Id = 2, PartGroupId = 2, SerialNumber = "546873957", ModelNumber = "3456345GB45" },
                new ComputerPart { Id = 3, PartGroupId = 1, SerialNumber = "546873957", ModelNumber = "3456345GB45" }
            );

            // Seed data for Computer_ComputerPart (from second file)
            modelBuilder.Entity<Computer_ComputerPart>().HasData(
                new Computer_ComputerPart { Id = 1, ComputerId = 1, ComputerPartId = 1 },
                new Computer_ComputerPart { Id = 2, ComputerId = 1, ComputerPartId = 2 },
                new Computer_ComputerPart { Id = 3, ComputerId = 3, ComputerPartId = 3 }
            );

            // Seed data for User (from first file, with encrypted emails)
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

            // Seed data for Request (from first file)
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

            // Seed data for Loan (from first file)
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

            modelBuilder.Entity<Preset>().HasData(
    new Preset
    {
        Id = 1,
        Name = "Computer",
        Data = @"{
            ""CPU"": ""string"",
            ""RAM"": ""string"",
            ""GPU"": ""string"",
            ""Lager"": ""string"",
            ""OS"": ""string"",
            ""Motherboard"": ""string"",
            ""Strømforsyning"": ""string"",
            ""Kabinet"": ""string"",
            ""Farve"": ""string""
        }"
    },
    new Preset
    {
        Id = 2,
        Name = "Bærbar",
        Data = @"{
            ""CPU"": ""string"",
            ""RAM"": ""string"",
            ""GPU"": ""string"",
            ""Lager"": ""string"",
            ""OS"": ""string"",
            ""Skærmstørrelse"": ""decimal"",
            ""BatteriKapacitet"": ""string"",
            ""Vægt"": ""decimal"",
            ""HDMI"": ""bool"",
            ""Thunderbolt"": ""bool"",
            ""Farve"": ""string""
        }"
    },
    new Preset
    {
        Id = 3,
        Name = "Skærm",
        Data = @"{
            ""Skærmstørrelse"": ""decimal"",
            ""Opløsning"": ""string"",
            ""Paneltype"": ""string"",
            ""Lysstyrke"": ""int"",
            ""Reaktionstid"": ""string"",
            ""HDR"": ""bool"",
            ""BuiltInSpeakers"": ""bool"",
            ""Porte"": ""string"",
            ""Farve"": ""string""
        }"
    },
    new Preset
    {
        Id = 4,
        Name = "Tastatur",
        Data = @"{
            ""Type"": ""string"",
            ""RGB"": ""bool"",            
            ""NumPad"": ""bool"",
            ""Mediakontroller"": ""bool"",
            ""Forbindelse"": ""string"",
            ""Trådløs"": ""bool"",
            ""Farve"": ""string""
        }"
    },
    new Preset
    {
        Id = 5,
        Name = "Mus",
        Data = @"{
            ""Type"": ""string"",
            ""Sensor"": ""string"",
            ""MinDPI"": ""int"",
            ""MaxDPI"": ""int"",
            ""Knapper"": ""int"",
            ""RGB"": ""bool"",
            ""Trådløs"": ""bool"",
            ""Vægt"": ""decimal"",
            ""Ergonomisk"": ""bool"",
            ""Farve"": ""string""
        }"
    },
    new Preset
    {
        Id = 6,
        Name = "Headset",
        Data = @"{
            ""Type"": ""string"",
            ""Størrelse"": ""decimal"",
            ""Mikrofon"": ""bool"",
            ""NoiseCanellation"": ""bool"",
            ""Forbindelse"": ""string"",
            ""Trådløs"": ""bool"",
            ""BatteriTid"": ""string"",
            ""RGB"": ""bool"",
            ""Farve"": ""#EC4899""
        }"
    },
    new Preset
    {
        Id = 7,
        Name = "Printer",
        Data = @"{
            ""Type"": ""string"",
            ""Farve"": ""bool"",
            ""Duplex"": ""bool"",
            ""PrintHastighed"": ""int"",
            ""Opløsning"": ""string"",
            ""Papirstørrelse"": ""string"",
            ""PapirKapacitet"": ""int"",
            ""Scanner"": ""bool"",
            ""Kopimaskine"": ""bool"",
            ""Fax"": ""bool"",
            ""Forbindelse"": ""string"",
            ""Netværk"": ""bool"",
            ""MånedligtVolumen"": ""int"",
            ""Farve"": ""#06B6D4""
        }"
    },
    new Preset
    {
        Id = 8,
        Name = "Skrivebord",
        Data = @"{
            ""Længde"": ""decimal"",
            ""Bredde"": ""decimal"",
            ""Højde"": ""decimal"",
            ""MinHøjde"": ""decimal"",
            ""MaxHøjde"": ""decimal"",
            ""Materiale"": ""string"",
            ""HøjdeJusterbart"": ""bool"",
            ""Elektrisk"": ""bool"",
            ""Bæreevne"": ""decimal"",
            ""KabelManagement"": ""bool"",
            ""HukommelsesPresets"": ""int"",
            ""Farve"": ""string""
        }"
    },
    new Preset
    {
        Id = 9,
        Name = "Stol",
        Data = @"{
            ""MinHøjde"": ""decimal"",
            ""MaxHøjde"": ""decimal"",
            ""Bredde"": ""decimal"",
            ""Dybde"": ""decimal"",
            ""Materiale"": ""string"",
            ""PolstringType"": ""string"",
            ""Ergonomisk"": ""bool"",
            ""LændeStøtte"": ""bool"",
            ""JusterbarLændestøtte"": ""bool"",
            ""Armlæn"": ""bool"",
            ""JusterbarArmlæn"": ""string"",
            ""Nakkestøtte"": ""bool"",
            ""TiltFunktion"": ""bool"",
            ""MaxVægt"": ""decimal"",
            ""Hjul"": ""string"",
            ""Farve"": ""string""
        }"
    },
    new Preset
    {
        Id = 10,
        Name = "Projektor",
        Data = @"{
            ""Opløsning"": ""string"",
            ""NativOpløsning"": ""string"",
            ""Lumens"": ""int"",
            ""Kontrast"": ""string"",
            ""Teknologi"": ""string"",
            ""LampeLeveTid"": ""int"",
            ""ThrowRatio"": ""string"",
            ""ProjektionsAfstand"": ""string"",
            ""Keystone"": ""bool"",
            ""HDR"": ""bool"",
            ""3D"": ""bool"",
            ""Forbindelse"": ""string"",
            ""Trådløs"": ""bool"",
            ""BuiltInSpeakers"": ""bool"",
            ""Støjniveau"": ""int"",
            ""Farve"": ""string""
        }"
    },
    new Preset
    {
        Id = 11,
        Name = "Router",
        Data = @"{
            ""WiFiStandard"": ""string"",
            ""MaxHastighed"": ""string"",
            ""Frekvens"": ""string"",
            ""DualBand"": ""bool"",
            ""TriBand"": ""bool"",
            ""MU-MIMO"": ""bool"",
            ""Beamforming"": ""bool"",
            ""LANPorter"": ""int"",
            ""WANPorter"": ""int"",
            ""PortHastighed"": ""string"",
            ""USB"": ""int"",
            ""QoS"": ""bool"",
            ""VPN"": ""bool"",
            ""GæsteNetværk"": ""bool"",
            ""Mesh"": ""bool"",
            ""Farve"": ""string""
        }"
    },
    new Preset
    {
        Id = 12,
        Name = "Netværksswitch",
        Data = @"{
            ""Porter"": ""int"",
            ""PoEPorter"": ""int"",
            ""Managed"": ""bool"",
            ""Hastighed"": ""string"",
            ""PortHastighed"": ""string"",
            ""SFPPorter"": ""int"",
            ""Switching Kapacitet"": ""string"",
            ""VLAN"": ""bool"",
            ""QoS"": ""bool"",
            ""LinkAggregation"": ""bool"",
            ""Stacking"": ""bool"",
            ""PoEBudget"": ""int"",
            ""Strømforbrug"": ""int"",
            ""Rack-montering"": ""bool"",
            ""Farve"": ""string""
        }"
    },
    new Preset
    {
        Id = 13,
        Name = "Tablet",
        Data = @"{
            ""CPU"": ""string"",
            ""RAM"": ""string"",
            ""Storage"": ""string"",
            ""UdvidelsesMulighed"": ""bool"",
            ""OS-Type"": ""string"",
            ""OSVersion"": ""string"",
            ""Skærmstørrelse"": ""decimal"",
            ""Opløsning"": ""string"",
            ""SkærmType"": ""string"",
            ""Batteritid"": ""string"",
            ""BatteriKapacitet"": ""int"",
            ""Kamera"": ""string"",
            ""FrontKamera"": ""string"",
            ""Mobilnetværk"": ""bool"",
            ""WiFi"": ""string"",
            ""Bluetooth"": ""string"",
            ""USB-C"": ""bool"",
            ""Stylus"": ""bool"",
            ""Tastatur"": ""bool"",
            ""Vægt"": ""decimal"",
            ""Farve"": ""string""
        }"
    },
    new Preset
    {
        Id = 14,
        Name = "Ekstern Lagerenhed",
        Data = @"{
            ""Type"": ""string"",
            ""Kapacitet"": ""string"",
            ""Forbindelse"": ""string"",
            ""USBVersion"": ""string"",
            ""LæseHastighed"": ""int"",
            ""SkrivHastighed"": ""int"",
            ""Kryptering"": ""bool"",
            ""AdgangskodeBeskyttelse"": ""bool"",
            ""Backup Software"": ""bool"",
            ""RAID"": ""bool"",
            ""Strømforsyning"": ""string"",
            ""Dimensioner"": ""string"",
            ""Vægt"": ""decimal"",
            ""Farve"": ""string""
        }"
    },
    new Preset
    {
        Id = 15,
        Name = "Grafiktablet",
        Data = @"{
            ""AktivtOmråde"": ""string"",
            ""Opløsning"": ""int"",
            ""Trykniveauer"": ""int"",
            ""TiltGenkendelse"": ""int"",
            ""Læsehastighed"": ""int"",
            ""Forbindelse"": ""string"",
            ""Trådløs"": ""bool"",
            ""MultiTouch"": ""bool"",
            ""ExpressKeys"": ""int"",
            ""TouchRing"": ""bool"",
            ""Skærm"": ""bool"",
            ""Skærmstørrelse"": ""decimal"",
            ""SkærmOpløsning"": ""string"",
            ""PenType"": ""string"",
            ""BatteriLøsPen"": ""bool"",
            ""Kompatibilitet"": ""string"",
            ""Tykkelse"": ""decimal"",
            ""Vægt"": ""decimal"",
            ""Farve"": ""string""
        }"
    },
    new Preset
    {
        Id = 16,
        Name = "Server",
        Data = @"{
            ""CPU"": ""string"",
            ""CPUAntal"": ""int"",
            ""Kerner"": ""int"",
            ""Tråde"": ""int"",
            ""RAM"": ""string"",
            ""MaxRAM"": ""string"",
            ""RAMSlots"": ""int"",
            ""Lager"": ""string"",
            ""LagerType"": ""string"",
            ""DriveBays"": ""int"",
            ""RAID"": ""string"",
            ""PSU"": ""string"",
            ""RedundantPSU"": ""bool"",
            ""FormFaktor"": ""string"",
            ""RackUnits"": ""string"",
            ""Netværk"": ""string"",
            ""NetworkPorts"": ""int"",
            ""RemoteManagement"": ""string"",
            ""HotSwap"": ""bool"",
            ""Farve"": ""string""
        }"
    },
    new Preset
    {
        Id = 17,
        Name = "UPS",
        Data = @"{
            ""Kapacitet"": ""int"",
            ""OutputWatt"": ""int"",
            ""Topologi"": ""string"",
            ""BatteriType"": ""string"",
            ""Runtime"": ""string"",
            ""OpladningsTid"": ""string"",
            ""InputVoltage"": ""string"",
            ""OutputVoltage"": ""string"",
            ""Stikdåser"": ""int"",
            ""BatteryBackup"": ""int"",
            ""SurgeOnly"": ""int"",
            ""USBPort"": ""bool"",
            ""NetworkManagement"": ""bool"",
            ""LCD"": ""bool"",
            ""Rack-montering"": ""bool"",
            ""Software"": ""string"",
            ""Farve"": ""string""
        }"
    },
    new Preset
    {
        Id = 18,
        Name = "Webcam",
        Data = @"{
            ""Opløsning"": ""string"",
            ""Billedfrekvens"": ""int"",
            ""Sensor"": ""string"",
            ""Linse"": ""string"",
            ""Autofokus"": ""bool"",
            ""Zoom"": ""string"",
            ""LowLight"": ""bool"",
            ""HDR"": ""bool"",
            ""Mikrofon"": ""bool"",
            ""Forbindelse"": ""string"",
            ""Monteringstype"": ""string"",
            ""PrivacyShutter"": ""bool"",
            ""Software"": ""string"",
            ""Farve"": ""string""
        }"
    }
);


        }


    }
    
}