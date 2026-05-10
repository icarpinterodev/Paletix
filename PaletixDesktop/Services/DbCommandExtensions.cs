using System;
using System.Data.Common;

namespace PaletixDesktop.Services
{
    internal static class DbCommandExtensions
    {
        public static void AddParameter(this DbCommand command, string name, object? value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }
    }
}
