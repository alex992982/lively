﻿using Lively.Grpc.Client;
using Lively.UI.Wpf.ViewModels;
using Lively.UI.Wpf.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Lively.UI.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IServiceProvider _serviceProvider;
        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> instance for the current application instance.
        /// </summary>
        public static IServiceProvider Services
        {
            get
            {
                IServiceProvider serviceProvider = ((App)Current)._serviceProvider;
                return serviceProvider ?? throw new InvalidOperationException("The service provider is not initialized");
            }
        }

        public App()
        {
            _serviceProvider = ConfigureServices();

            Services.GetRequiredService<MainWindow>().Show();

            SetupUnhandledExceptionLogging();

            Logger.Info("Initialization complete.");
        }

        private IServiceProvider ConfigureServices()
        {
            //TODO: make nlogger write only to console.
            var provider = new ServiceCollection()
                //singleton
                .AddSingleton<IDesktopCoreClient, WinDesktopCoreClient>()
                .AddSingleton<IUserSettingsClient, UserSettingsClient>()
                .AddSingleton<IDisplayManagerClient, DisplayManagerClient>()
                .AddSingleton<MainWindow>()
                .AddSingleton<LibraryViewModel>()
                .AddSingleton<SettingsViewModel>()
                //transient
                .AddTransient<ScreenLayoutViewModel>()
                .BuildServiceProvider();

            return provider;
        }

        public static void ShutDown()
        {
            try
            {
                ((ServiceProvider)App.Services)?.Dispose();
            }
            catch (InvalidOperationException) { /* not initialised */ }
            //Shutdown needs to be called from dispatcher..
            Application.Current.Dispatcher.Invoke(Application.Current.Shutdown);
        }

        private void SetupUnhandledExceptionLogging()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

            Dispatcher.UnhandledException += (s, e) =>
                LogUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException");

            TaskScheduler.UnobservedTaskException += (s, e) =>
                LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
        }

        private void LogUnhandledException(Exception exception, string source)
        {
            string message = $"Unhandled exception ({source})";
            try
            {
                System.Reflection.AssemblyName assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();
                message = string.Format("Unhandled exception in {0} v{1}", assemblyName.Name, assemblyName.Version);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Exception in LogUnhandledException");
            }
            finally
            {
                Logger.Error("{0}\n{1}", message, exception.ToString());
            }
        }
    }
}
