using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SimpleTodo.Api;

public class TodoDb : DbContext
{
    public TodoDb(DbContextOptions options) : base(options) { }
    public DbSet<TodoItem> Items => Set<TodoItem>();
    public DbSet<TodoList> Lists => Set<TodoList>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    => optionsBuilder
         .LogTo(
            Console.WriteLine,
            (eventId, logLevel) => 
                logLevel >= LogLevel.Information
                || eventId == CoreEventId.ExecutionStrategyRetrying,
            DbContextLoggerOptions.SingleLine);
}