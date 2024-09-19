using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;
using UserManager.Constants; 
using UserManager.Data;
using UserManager.Models;
using UserManager.Services;
using Role= UserManager.Models.Role;
var builder = WebApplication.CreateBuilder(args);

// ===========================================
// 1. Configure Services
// ===========================================

// 1.1 Add Controllers
builder.Services.AddControllers();

// 1.2 Configure Swagger with JWT Authentication
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Define the "Bearer" scheme for JWT
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // Apply the "Bearer" scheme globally
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement{
    {
        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Reference = new Microsoft.OpenApi.Models.OpenApiReference
            {
                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        },
        new string[] {}
    }});
});

// 1.3 Configure CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder => builder
            .AllowAnyOrigin()  // Allow any origin; consider restricting in production
            .AllowAnyMethod()  // Allow any HTTP method
            .AllowAnyHeader()); // Allow any header
});

// 1.4 Configure Redis Connection
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString!));

// 1.5 Configure Entity Framework with PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 1.6 Configure Identity with Custom Password Options
builder.Services.AddIdentity<User, Role>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 1.7 Register Application Services
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<TokenService>(); // Ensure TokenService is properly implemented

// 1.8 Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    // Set the default authentication scheme to JWT Bearer
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Configure JWT Bearer options
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, // Validate the issuer
        ValidateAudience = true, // Validate the audience
        ValidateLifetime = true, // Validate the token's lifetime
        ValidateIssuerSigningKey = true, // Validate the signing key
        ValidIssuer = builder.Configuration["Jwt:Issuer"], // Issuer from configuration
        ValidAudience = builder.Configuration["Jwt:Audience"], // Audience from configuration
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)) // Key from configuration
    };
});

// 1.9 Add Authorization Services
builder.Services.AddAuthorization();

// ===========================================
// 2. Build the Application
// ===========================================

var app = builder.Build();

// ===========================================
// 3. Configure the HTTP Request Pipeline
// ===========================================

// 3.1 Enable Swagger in Development Environment
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    });
    app.UseDeveloperExceptionPage();  // Show detailed error pages
}

// 3.2 Enforce HTTPS Redirection
app.UseHttpsRedirection();

// 3.3 Apply CORS Policy
app.UseCors("AllowAllOrigins");  // Apply the defined CORS policy

// 3.4 Enable Authentication and Authorization
app.UseAuthentication();  // Enable JWT authentication
app.UseAuthorization();   // Enable authorization based on policies

// 3.5 Map Controller Endpoints
app.MapControllers();

// 3.6 Run the Application
app.Run();
