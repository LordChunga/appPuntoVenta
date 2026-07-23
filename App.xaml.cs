using System.Configuration;
using System.Data;
using System.Windows;
using Dapper;

namespace MiniPosWpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        SqlMapper.AddTypeHandler(new DecimalTypeHandler());
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        System.IO.File.AppendAllText("log.txt", "\n[UI CRASH] " + e.Exception.ToString() + "\n");
        e.Handled = true; // prevent exit to see if we can continue, but it might still crash if critical
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        System.IO.File.AppendAllText("log.txt", "\n[BG CRASH] " + e.ExceptionObject.ToString() + "\n");
    }

    private class DecimalTypeHandler : SqlMapper.TypeHandler<decimal>
    {
        public override void SetValue(System.Data.IDbDataParameter parameter, decimal value)
        {
            parameter.Value = value;
        }

        public override decimal Parse(object value)
        {
            return Convert.ToDecimal(value);
        }
    }
}
