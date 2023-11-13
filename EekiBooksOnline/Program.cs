using EekiBooks.DataAccess;
using EekiBooks.DataAcess.Repository;
using EekiBooks.DataAcess.Repository.IRepository;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using EekiBooks.Utilities;
using Microsoft.AspNetCore.Identity.UI.Services;
using Stripe;
using Microsoft.Extensions.Hosting;
using EekiBooksOnline.Areas.Admin.Controllers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connString,
        sqlServerOptions =>
        {
            sqlServerOptions.CommandTimeout(3600);
            sqlServerOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
        }));
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));
builder.Services.AddSingleton<IStripeClient, StripeClient>();
builder.Services.AddMvc().AddNewtonsoftJson();
builder.Services.AddIdentity<IdentityUser, IdentityRole>().AddDefaultTokenProviders()
    .AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultUI();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddSingleton<IEmailSender, EmailSender>();
builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
var fbAppId = builder.Configuration.GetValue<string>("FacebookAppId");
var fbAppSecret = builder.Configuration.GetValue<string>("FacebookAppSecret");
builder.Services.AddAuthentication().AddFacebook(options =>
{
    options.AppId = fbAppId;
    options.AppSecret = fbAppSecret;
});
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = $"/Identity/Account/Login";
    options.LogoutPath = $"/Identity/Account/Logout";
    options.AccessDeniedPath = $"/Identity/Account/AccessDenied";
});


builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(100);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;

});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowStripeCheckout",
        builder =>
        {
            builder.WithOrigins("https://checkout.stripe.com")
                   .AllowAnyHeader()
                   .AllowAnyMethod()
                   .AllowCredentials();
        });
});


builder.Services.AddControllersWithViews().AddApplicationPart(typeof(StripeWebhookController).Assembly);


var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseCors("AllowStripeCheckout");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();


app.UseAuthentication();

app.UseAuthorization();
StripeConfiguration.ApiKey = builder.Configuration.GetSection("Stripe:SecretKey").Get<string>();
app.UseSession();


app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}");

app.Run();




