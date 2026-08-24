using MiniStopWeb.Models; // Dòng này cực kỳ quan trọng để hệ thống nhận diện được Database

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache(); // Đăng ký bộ nhớ đệm

// Đăng ký Session cho Giỏ hàng và Đăng nhập
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Giỏ hàng sẽ tự xóa nếu khách treo máy 30 phút
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ---- ĐĂNG KÝ DATABASE CONTEXT (FIX LỖI UNABLE TO RESOLVE SERVICE) ----
builder.Services.AddDbContext<MiniStopDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// ---- TẠM ẨN DÒNG NÀY ĐỂ FIX CẢNH BÁO MÀU VÀNG (HTTPS REDIRECTION) ----
// app.UseHttpsRedirection(); 

app.UseStaticFiles();

app.UseRouting();

// Kích hoạt Session (Bắt buộc phải nằm đúng vị trí này: Giữa UseRouting và UseAuthorization)
app.UseSession(); 

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();