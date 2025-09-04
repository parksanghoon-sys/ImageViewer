using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add HTTP client for service communication
builder.Services.AddHttpClient();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://frontend:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Configure JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                builder.Configuration["JWT:Secret"] ?? "SuperSecretKeyForImageViewerApplicationThatIsVeryLongAndSecure123!")),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference(options =>
    {
        options.Title = "ImageViewer API Gateway";
        options.Theme = Scalar.AspNetCore.ScalarTheme.BluePlanet;
        options.ShowSidebar = true;
    });
}

app.UseCors("AllowReactApp");
app.UseAuthentication();
app.UseAuthorization();

var authServiceUrl = builder.Configuration["Services:AuthService"] ?? "http://localhost:5001";
var imageServiceUrl = builder.Configuration["Services:ImageService"] ?? "http://localhost:5002";
var shareServiceUrl = builder.Configuration["Services:ShareService"] ?? "http://localhost:5003";

// Auth Service routes
app.MapPost("/api/auth/register", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var httpClient = httpClientFactory.CreateClient();
    var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
    
    var response = await httpClient.PostAsync($"{authServiceUrl}/api/auth/register", 
        new StringContent(requestBody, Encoding.UTF8, "application/json"));
    
    var content = await response.Content.ReadAsStringAsync();
    context.Response.StatusCode = (int)response.StatusCode;
    await context.Response.WriteAsync(content);
});

app.MapPost("/api/auth/login", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var httpClient = httpClientFactory.CreateClient();
    var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
    
    var response = await httpClient.PostAsync($"{authServiceUrl}/api/auth/login", 
        new StringContent(requestBody, Encoding.UTF8, "application/json"));
    
    var content = await response.Content.ReadAsStringAsync();
    context.Response.StatusCode = (int)response.StatusCode;
    await context.Response.WriteAsync(content);
});

app.MapPost("/api/auth/refresh", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var httpClient = httpClientFactory.CreateClient();
    var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
    
    var response = await httpClient.PostAsync($"{authServiceUrl}/api/auth/refresh", 
        new StringContent(requestBody, Encoding.UTF8, "application/json"));
    
    var content = await response.Content.ReadAsStringAsync();
    context.Response.StatusCode = (int)response.StatusCode;
    await context.Response.WriteAsync(content);
});

app.MapGet("/api/auth/users", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var httpClient = httpClientFactory.CreateClient();
    var token = context.Request.Headers["Authorization"].FirstOrDefault();
    if (!string.IsNullOrEmpty(token))
        httpClient.DefaultRequestHeaders.Add("Authorization", token);
    
    var response = await httpClient.GetAsync($"{authServiceUrl}/api/auth/users");
    var content = await response.Content.ReadAsStringAsync();
    context.Response.StatusCode = (int)response.StatusCode;
    await context.Response.WriteAsync(content);
}).RequireAuthorization();

// Image Service routes
app.MapPost("/api/images/upload", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var httpClient = httpClientFactory.CreateClient();
    var token = context.Request.Headers["Authorization"].FirstOrDefault();
    if (!string.IsNullOrEmpty(token))
        httpClient.DefaultRequestHeaders.Add("Authorization", token);
    
    var form = await context.Request.ReadFormAsync();
    var multipartContent = new MultipartFormDataContent();
    
    foreach (var file in form.Files)
    {
        var streamContent = new StreamContent(file.OpenReadStream());
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
        multipartContent.Add(streamContent, file.Name, file.FileName);
    }
    
    var response = await httpClient.PostAsync($"{imageServiceUrl}/api/images/upload", multipartContent);
    var content = await response.Content.ReadAsStringAsync();
    context.Response.StatusCode = (int)response.StatusCode;
    await context.Response.WriteAsync(content);
}).RequireAuthorization();

app.MapGet("/api/images", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var httpClient = httpClientFactory.CreateClient();
    var token = context.Request.Headers["Authorization"].FirstOrDefault();
    if (!string.IsNullOrEmpty(token))
        httpClient.DefaultRequestHeaders.Add("Authorization", token);
    
    var queryString = context.Request.QueryString.ToString();
    var response = await httpClient.GetAsync($"{imageServiceUrl}/api/images{queryString}");
    var content = await response.Content.ReadAsStringAsync();
    context.Response.StatusCode = (int)response.StatusCode;
    await context.Response.WriteAsync(content);
}).RequireAuthorization();

app.MapGet("/api/images/{fileName}", async (HttpContext context, IHttpClientFactory httpClientFactory, string fileName) =>
{
    var httpClient = httpClientFactory.CreateClient();
    var response = await httpClient.GetAsync($"{imageServiceUrl}/api/images/{fileName}");
    
    if (response.IsSuccessStatusCode)
    {
        context.Response.ContentType = response.Content.Headers.ContentType?.ToString() ?? "image/jpeg";
        await response.Content.CopyToAsync(context.Response.Body);
    }
    else
    {
        context.Response.StatusCode = (int)response.StatusCode;
    }
});

// Share Service routes
app.MapPost("/api/share/request", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var httpClient = httpClientFactory.CreateClient();
    var token = context.Request.Headers["Authorization"].FirstOrDefault();
    if (!string.IsNullOrEmpty(token))
        httpClient.DefaultRequestHeaders.Add("Authorization", token);
    
    var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
    var response = await httpClient.PostAsync($"{shareServiceUrl}/api/share/request", 
        new StringContent(requestBody, Encoding.UTF8, "application/json"));
    
    var content = await response.Content.ReadAsStringAsync();
    context.Response.StatusCode = (int)response.StatusCode;
    await context.Response.WriteAsync(content);
}).RequireAuthorization();

app.MapPost("/api/share/approve", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var httpClient = httpClientFactory.CreateClient();
    var token = context.Request.Headers["Authorization"].FirstOrDefault();
    if (!string.IsNullOrEmpty(token))
        httpClient.DefaultRequestHeaders.Add("Authorization", token);
    
    var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
    var response = await httpClient.PostAsync($"{shareServiceUrl}/api/share/approve", 
        new StringContent(requestBody, Encoding.UTF8, "application/json"));
    
    var content = await response.Content.ReadAsStringAsync();
    context.Response.StatusCode = (int)response.StatusCode;
    await context.Response.WriteAsync(content);
}).RequireAuthorization();

app.MapGet("/api/share/requests", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var httpClient = httpClientFactory.CreateClient();
    var token = context.Request.Headers["Authorization"].FirstOrDefault();
    if (!string.IsNullOrEmpty(token))
        httpClient.DefaultRequestHeaders.Add("Authorization", token);
    
    var response = await httpClient.GetAsync($"{shareServiceUrl}/api/share/requests");
    var content = await response.Content.ReadAsStringAsync();
    context.Response.StatusCode = (int)response.StatusCode;
    await context.Response.WriteAsync(content);
}).RequireAuthorization();

app.MapGet("/api/share/shared/{userId}/images", async (HttpContext context, IHttpClientFactory httpClientFactory, string userId) =>
{
    var httpClient = httpClientFactory.CreateClient();
    var token = context.Request.Headers["Authorization"].FirstOrDefault();
    if (!string.IsNullOrEmpty(token))
        httpClient.DefaultRequestHeaders.Add("Authorization", token);
    
    var response = await httpClient.GetAsync($"{shareServiceUrl}/api/share/shared/{userId}/images");
    var content = await response.Content.ReadAsStringAsync();
    context.Response.StatusCode = (int)response.StatusCode;
    await context.Response.WriteAsync(content);
}).RequireAuthorization();

app.Run();

// 테스트를 위해 Program 클래스를 public으로 만듦
public partial class Program { }
