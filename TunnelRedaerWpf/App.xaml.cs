using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;


namespace TunnelRedaerWpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    /// 
   
    public partial class App : Application
    {

        public IServiceProvider ServiceProvider { get; private set; }

        public IConfiguration Configuration { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            var builder = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
             .AddJsonFile("appsetting.json", optional: false, reloadOnChange: true);

            Configuration = builder.Build();
          
            Console.WriteLine(Configuration.GetConnectionString("ReaderIp"));
            Console.WriteLine(Configuration.GetConnectionString("SystemIp"));
            Console.WriteLine(Configuration.GetConnectionString("Port"));

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();

           
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // ...

            services.AddTransient(typeof(MainWindow));
        }
    }
}
