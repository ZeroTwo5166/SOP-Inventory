// Validate encryption keys exist
var encryptionKey = Environment.GetEnvironmentVariable("ENCRYPTION_KEY");
var encryptionIv = Environment.GetEnvironmentVariable("ENCRYPTION_IV");

if (string.IsNullOrEmpty(encryptionKey) || encryptionKey.Length != 32)
{
    throw new InvalidOperationException("ENCRYPTION_KEY must be exactly 32 characters.");
}

if (string.IsNullOrEmpty(encryptionIv) || encryptionIv.Length != 16)
{
    throw new InvalidOperationException("ENCRYPTION_IV must be exactly 16 characters.");
}


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IArchive_ItemRepository, Archive_ItemRepository>();

builder.Services.AddScoped<IArchive_ItemGroupRepository, Archive_ItemGroupRepository>();

builder.Services.AddScoped<IArchive_ItemTypeRepository, Archive_ItemTypeRepository>();

builder.Services.AddScoped<IArchive_LoanRepository, Archive_LoanRepository>();

builder.Services.AddScoped<IArchive_RequestRepository, Archive_RequestRepository>();

builder.Services.AddScoped<IArchive_UserRepository, Archive_UserRepository>();

builder.Services.AddScoped<IAddressRepository, AddressRepository>();

builder.Services.AddScoped<IBuildingRepository, BuildingRepository>();

builder.Services.AddScoped<IItemGroupRepository, ItemGroupRepository>();

builder.Services.AddScoped<IItemRepository, ItemRepository>();

builder.Services.AddScoped<IItemTypeRepository, ItemTypeRepository>();

builder.Services.AddScoped<ILoanRepository, LoanRepository>();

builder.Services.AddScoped<IRequestRepository,  RequestRepository>();

builder.Services.AddScoped<IRoleRepository, RoleRepository>();

builder.Services.AddScoped<IRoomRepository, RoomRepository>();

builder.Services.AddScoped<IRoomRepository, RoomRepository>();

builder.Services.AddScoped<IStatusHistoryRepository, StatusHistoryRepository>();

builder.Services.AddScoped<IStatusRepository, StatusRepository>();

builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<IJwtUtils, JwtUtils>();

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddDbContext<DatabaseContext>(options => {
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder => builder
    .WithOrigins("http://localhost:4200")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials());
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SOP-Inventar-API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization", 
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer", 
        BearerFormat = "JWT", 
        In = ParameterLocation.Header, 
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
       {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {} 
        }
    });
});

builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection(); 
app.UseCors("CorsPolicy"); 

app.Use(async (context, next) =>
{
    Console.WriteLine("Incoming Request Headers:");

    foreach (var header in context.Request.Headers)
    {
        Console.WriteLine($"{header.Key}: {header.Value}");
    }

    await next();
});


app.UseMiddleware<JwtMiddleware>();
app.UseAuthorization(); 
app.MapControllers(); 

app.Run();
