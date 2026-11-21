using Elmotamer.DbContext; // تأكد من مطابقة اسم الـ Namespace لمشروعك الحالي
using Elmotamer.Services; // الـ Namespace الخاص بالخدمات
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

namespace Elmotamer // تأكد من مطابقة اسم الـ Namespace لمشروعك الحالي
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. تسجيل الـ DbContext
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // 2. إعدادات CORS (مطابقة للمشروع الشغال)
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            // 3. إعدادات JWT (مطابقة للمشروع الشغال)
            var jwtSettings = builder.Configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? "YourVerySecretLongKeyHere123!");

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false, // تم ضبطها لـ false لتفادي مشاكل السيرفر
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            // ==================== تسجيل خدمة تحليل الدروس (كشكول الخدمة - Gemini AI) ====================
            builder.Services.AddHttpClient<ILessonAnalysisService, LessonAnalysisService>(client =>
            {
                client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
                client.Timeout = TimeSpan.FromSeconds(60); // مهلة زمنية كافية لاستجابة الموديل
            });
            // ===========================================================================================

            // 4. إعداد Swagger
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                        new string[] {}
                    }
                });
            });

            // ==================== تفعيل Firebase Admin SDK بأمان ====================
            try
            {
                var firebaseConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "firebase-config.json");
                if (File.Exists(firebaseConfigPath))
                {
                    if (FirebaseApp.DefaultInstance == null)
                    {
                        FirebaseApp.Create(new AppOptions()
                        {
                            Credential = GoogleCredential.FromFile(firebaseConfigPath)
                        });
                    }
                }
                else
                {
                    Console.WriteLine("Warning: firebase-config.json file was not found on server directory.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Firebase initialization skipped or failed: {ex.Message}");
            }
            // ===================================================================

            var app = builder.Build();

            // 5. تفعيل الـ Middleware بالترتيب الصحيح (هام جداً للاستضافة)
            app.UseStaticFiles();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
                c.RoutePrefix = "swagger";
            });

            app.UseCors("AllowAll");
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}