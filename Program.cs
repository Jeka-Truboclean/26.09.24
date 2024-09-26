using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.IO;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LoggingSQL
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using (ApplicationContext db = new ApplicationContext())
            {
                db.GetService<ILoggerFactory>().AddProvider(new MyLoggerProvider());

                // 1
                db.Users.Add(new User { Name = "A", Age = 5 });
                db.SaveChanges();

                // 2
                var users = db.Users.ToList();

                // 3
                var user = db.Users.First();
                user.Name = "Updated A";
                db.SaveChanges();

                // 4
                db.Users.Remove(user);
                db.SaveChanges();
            }
        }
    }

    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    public class ApplicationContext : DbContext
    {
        public static readonly ILoggerFactory MyLoggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddProvider(new MyLoggerProvider());
            builder.AddFilter((category, logLevel) =>
                category == DbLoggerCategory.Database.Command.Name && logLevel == LogLevel.Information);
        });

        public DbSet<User> Users { get; set; } = null!;

        public ApplicationContext() => Database.EnsureCreated();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseLoggerFactory(MyLoggerFactory);
            optionsBuilder.UseSqlServer("Server=(localdb)\\ProjectModels;Database=NewDb;Trusted_Connection=true");
        }
    }

    public class MyLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            return new MyLogger();
        }

        public void Dispose()
        {
        }

        private class MyLogger : ILogger
        {
            public IDisposable BeginScope<TState>(TState state)
            {
                return null;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                var message = formatter(state, exception);

                if (eventId.Id == RelationalEventId.CommandExecuted.Id)
                {
                    File.AppendAllText("log.txt", message + Environment.NewLine);
                }
            }
        }
    }
}
