using GeniusesProMax;
using Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddInfrastructureDependencyInjection();
builder.Services.AddDependencyIndection();


// JWT Authentication
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        var jwtSection = builder.Configuration.GetSection("Jwt");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSection["Key"]!))
        };
    });
builder.Services.AddAuthorization(options =>
{
    // Ownership policy: Student can access only their own record, Admin can access any record.
    options.AddPolicy("StudentOwnerOrAdmin", policy =>
        policy.Requirements.Add(new UserOwnerOrAdminRequirement()));
});

// Register the policy handler that contains the ownership logic.
builder.Services.AddSingleton<IAuthorizationHandler, UserOwnerOrAdminHandler>();


builder.Services.AddSwaggerGen(options =>
{
// Define the "Bearer" security scheme.
// Swagger will use it to display an input box for Authorization header.
options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
{
    // The header name where the JWT should be placed.
    Name = "Authorization",

    // HTTP authentication scheme (Authorization header).
    Type = SecuritySchemeType.Http,

    // The scheme name must be "Bearer" for JWT Bearer tokens.
    Scheme = "Bearer",

    // Optional, but helps documentation and UI.
    BearerFormat = "JWT",

    // Token is sent in the request header.
    In = ParameterLocation.Header,

    // Instruction shown in Swagger UI.
    Description = "Enter: Bearer {your JWT token}"
});


    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                // Reference the security scheme defined above by its Id: "Bearer".
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },

            // No specific scopes are required for JWT Bearer in this setup.
            new string[] {}
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();
app.UseAuthentication();

app.MapControllers();

app.Run();
