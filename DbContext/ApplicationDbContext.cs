using Elmotamer.Models;
using Microsoft.EntityFrameworkCore;

namespace Elmotamer.DbContext
{
    public class ApplicationDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<ConferenceFile> ConferenceFiles { get; set; }

        // التعديل: إضافة جدول المشتركين (Attendees) الجديد ليكون متاحاً في الداتا بيز
        public DbSet<Attendee> Attendees { get; set; }

        // إضافة جدول مواعيد الوجبات (MealSchedules)
        public DbSet<MealSchedule> MealSchedules { get; set; }

        // 👈 إضافة جدول المنهج الروحي اليومي (SpiritualDailyPoints)
        public DbSet<SpiritualDailyPoint> SpiritualDailyPoints { get; set; }

        // 👈 إضافة جدول الأرشيف (AttendeesHistory)
        public DbSet<AttendeesHistory> AttendeesHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. تحديد اسم الجدول في الداتا بيز بالظبط (Users) وتحديد الـ Primary Key
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<User>().HasKey(u => u.UserId);

            // 2. تحديد إعدادات جدول الأدمنز والـ Primary Key الخاص به (AdminId)
            modelBuilder.Entity<Admin>().ToTable("Admins");
            modelBuilder.Entity<Admin>().HasKey(a => a.AdminId);

            // 3. ربط جدول الملفات بالأدمنز وتحديد سلوك الحذف (SetNull) عشان ملفاتك ما تضيعش
            modelBuilder.Entity<ConferenceFile>()
                .HasOne<Admin>() // الملف يخص أدمن واحد
                .WithMany()      // الأدمن يقدر يرفع ملفات كتير
                .HasForeignKey(f => f.UploadedByAdminId) // المفتاح الأجنبي
                .OnDelete(DeleteBehavior.SetNull); // لو الأدمن اتمسح، حط NULL ومتمسحش الملف!

            // التعديل الجديد: تحديد إعدادات جدول المشتركين (Attendees) والـ Primary Key الخاص به
            modelBuilder.Entity<Attendee>().ToTable("Attendees");
            modelBuilder.Entity<Attendee>().HasKey(a => a.AttendeeId);

            // إعدادات جدول مواعيد الوجبات (MealSchedules) وقيم السجل الموحد الافتراضية
            modelBuilder.Entity<MealSchedule>().ToTable("MealSchedules");
            modelBuilder.Entity<MealSchedule>().HasKey(m => m.Id);
            modelBuilder.Entity<MealSchedule>().HasData(
                new MealSchedule { Id = 1, MealDate = null, MealTime = null }
            );

            // 👈 إعدادات جدول المنهج الروحي الجديد (SpiritualDailyPoints)
            modelBuilder.Entity<SpiritualDailyPoint>().ToTable("SpiritualDailyPoints");
            modelBuilder.Entity<SpiritualDailyPoint>().HasKey(s => s.Id);

            // 👈 إعدادات جدول الأرشيف الجديد (AttendeesHistory)
            modelBuilder.Entity<AttendeesHistory>().ToTable("AttendeesHistory");
            modelBuilder.Entity<AttendeesHistory>().HasKey(ah => ah.Id);
        }
    }
}