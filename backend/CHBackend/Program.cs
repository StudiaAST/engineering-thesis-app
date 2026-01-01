using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

//
// ===================== DATABASE =====================
//
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

//
// ===================== IDENTITY =====================
//
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;

    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

QuestPDF.Settings.License = LicenseType.Community;

//
// ===================== JWT AUTH =====================
//
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "JwtBearer";
    options.DefaultChallengeScheme = "JwtBearer";
})
.AddJwtBearer("JwtBearer", options =>
{
    var jwtKey = builder.Configuration["Jwt:Key"]
        ?? throw new InvalidOperationException("Brak konfiguracji Jwt:Key w appsettings.json");

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});

//
// ===================== CORS =====================
//
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy
            // DEV: Blazor WASM działa na http://localhost:5234
            .WithOrigins(
                "http://localhost:5234",
                // zostawiamy też ten stary origin (może się jeszcze przydać)
                "https://localhost:7039"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            // ważne, jeśli kiedykolwiek użyjesz cookies/credentials; nie przeszkadza dla JWT
            .AllowCredentials()
    );
});

//
// ===================== AUTHORIZATION POLICIES =====================
//
builder.Services.AddAuthorization(options =>
{
    // USERS
    options.AddPolicy("CanReadUsers", p => p.RequireRole("Admin"));
    options.AddPolicy("CanUpdateCreateUsers", p => p.RequireRole("Admin"));
    options.AddPolicy("CanDeleteUsers", p => p.RequireRole("Admin"));

    // CONTRACTS
    options.AddPolicy("CanReadContracts", p => p.RequireRole("Admin", "Manager", "Contractor"));
    options.AddPolicy("CanUpdateCreateContracts", p => p.RequireRole("Admin", "Manager"));
    options.AddPolicy("CanDeleteContracts", p => p.RequireRole("Admin", "Manager"));

    // ISSUES
    options.AddPolicy("CanReadIssues", p => p.RequireRole("Admin", "Manager", "User", "Contractor"));
    options.AddPolicy("CanUpdateCreateIssues", p => p.RequireRole("Admin", "Manager", "Contractor"));
    options.AddPolicy("CanDeleteIssues", p => p.RequireRole("Admin", "Manager"));

    // CONTRACTORS
    options.AddPolicy("CanReadContractors", p => p.RequireRole("Admin", "Manager"));
    options.AddPolicy("CanUpdateCreateContractors", p => p.RequireRole("Admin", "Manager"));
    options.AddPolicy("CanDeleteContractors", p => p.RequireRole("Admin", "Manager"));
});

//
// ===================== CONTROLLERS + SWAGGER =====================
//
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Construct Hub Backend",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

//
// ===================== DATABASE MIGRATION + SEED =====================
//
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<AppDbContext>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<AppUser>>();

    db.Database.Migrate();

    // Seed roles
    string[] roles = { "Admin", "User", "Manager", "Contractor" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // BOOTSTRAP ADMIN
    var adminUser = await userManager.FindByNameAsync("admin");
    if (adminUser != null && !await userManager.IsInRoleAsync(adminUser, "Admin"))
    {
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}

//
// ===================== MIDDLEWARE =====================
//
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// UWAGA: dla dev możesz to zostawić.
// Jeśli kiedyś będziesz miał problemy z http lokalnie, można to wyłączyć w dev, ale na razie zostawiamy.
app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// CORS MUSI być przed Authentication/Authorization
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Wykonawcy}/{action=Index}/{id?}");

app.Run();
