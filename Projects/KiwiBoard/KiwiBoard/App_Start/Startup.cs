using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Owin;
using Owin;
using System;

[assembly: OwinStartup(typeof(KiwiBoard.Startup))]

namespace KiwiBoard
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            /*
            app.UseHangfire(config =>
            {
                config.UseSqlServerStorage("KiwiBoardDb");
                var options = new BackgroundJobServerOptions
                {
                    SchedulePollingInterval = TimeSpan.FromSeconds(15)
                };
                config.UseServer(options);
            });*/
        }
    }
}