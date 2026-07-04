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
