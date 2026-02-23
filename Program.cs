using Elmansa_api.Configuration;
using Elmansa_api.Data;
using Elmansa_api.Models;
using Elmansa_api.Repositories;
using Elmansa_api.Services;
using Elmansa_api.Services.Implementation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Configure DbContext
builder.Services.AddDbContext<AppDbContext>(options => 
{ 
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")); 
});

// ============================================
// Configure Identity with Email Confirmation
// ============================================
builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Configure Identity Options
builder.Services.Configure<IdentityOptions>(options =>
{
    IdentityConfiguration.ConfigureIdentityOptions(options);
});

// ============================================
// Configure JWT Authentication
// ============================================
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Elmansa";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ElmansaUsers";

if (string.IsNullOrEmpty(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException(
        "JWT Key is not configured in appsettings.json or is too short (minimum 32 characters required)");
}

var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Authority = null;
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
        };
    });

// Configure Authorization
builder.Services.AddAuthorization();

// ============================================
// Register Repositories and Unit of Work
// ============================================

// Generic Repository (auto-registered via UnitOfWork)
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// Specific Repositories
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
builder.Services.AddScoped<ILessonRepository, LessonRepository>();

// Unit of Work
builder.Services.AddScoped<IUnitOfWork>(provider => 
    new UnitOfWork(
        provider.GetRequiredService<AppDbContext>(),
        provider.GetRequiredService<ILogger<UnitOfWork>>(),
        provider
    )
);

// ============================================
// Register Services
// ============================================

// Course Service
builder.Services.AddScoped<ICourseService, CourseService>();

// Enrollment Service
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();

// ============================================
// Register Authentication & Authorization Services
// ============================================

// ============================================
// Register Email Configuration
// ============================================
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection(EmailSettings.SectionName));

// Token Service (JWT & Refresh Token generation)
builder.Services.AddScoped<ITokenService, TokenService>();

// Email Service (for confirmation emails and password reset)
builder.Services.AddScoped<IEmailService, EmailService>();

// ============================================
// AI and Rate Limiting Services
// ============================================

// AI Service Configuration
builder.Services.Configure<AIOptions>(builder.Configuration.GetSection(AIOptions.SectionName));
builder.Services.AddHttpClient<IAIService, GeminiAIService>();

// Rate Limit Service Configuration
builder.Services.Configure<RateLimitOptions>(builder.Configuration.GetSection(RateLimitOptions.SectionName));
builder.Services.AddScoped<IRateLimitService, RateLimitService>();


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Elmansa Educational AI API",
        Version = "v1",
        Description = "Complete educational platform API with AI integration, course management, student enrollment, and rate limiting",
        Contact = new OpenApiContact
        {
            Name = "Elmansa Support",
            Email = "hamedrabi3@gmail.com",
            Url = new Uri("https://elmanssa.com")
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // ============================================
    // JWT Bearer Token Security Scheme
    // ============================================
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme. Enter your JWT token.\n\n" +
                      "Example: \"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\"",
        In = ParameterLocation.Header,
        Name = "Authorization"
    });

    // ============================================
    // Apply Security Globally to All Endpoints
    // ============================================
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "bearer",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });

    // ============================================
    // Add XML Comments Support (Optional)
    // ============================================
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (System.IO.File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});


var app = builder.Build();

// Note: MapIdentityApi is removed - we use custom AuthController instead
// This prevents duplicate endpoints and allows better customization

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Elmansa API v1");
        options.RoutePrefix = string.Empty;  // Serve Swagger at root (https://localhost:5001/)
        
        // Enable JWT token persistance across requests
        options.DefaultModelsExpandDepth(0);  // Collapse models by default
        options.DefaultModelExpandDepth(0);
        options.DisplayOperationId();
        options.DisplayRequestDuration();
        options.EnableFilter();
        options.ShowExtensions();
        
        // Add custom CSS for better styling
        options.InjectStylesheet("https://cdnjs.cloudflare.com/ajax/libs/swagger-ui/4.15.5/swagger-ui.min.css");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
