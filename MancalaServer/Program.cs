using System;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace MancalaServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                     policy
                        .AllowAnyOrigin() 
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(5214);
            });


            builder.Services.AddSignalR();
            
            var app = builder.Build();
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseCors();
            app.MapHub<GameHub>("/game");
            app.Run();
        }
    }
}