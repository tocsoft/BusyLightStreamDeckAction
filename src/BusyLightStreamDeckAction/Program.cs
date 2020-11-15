using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using Tocsoft.StreamDeck;

namespace Tocsoft.BusyLightStreamDeckAction
{
    class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureServices((c,s)=> {
                s.AddSingleton<OpenhabManager>();
            })
            .ConfigureStreamDeck(args, c =>
                {
                    c.AddAction<MyActionHandler>(a =>
                    {
                        a.Name = "Office Busy Light";
                    });
                });
    }
}
