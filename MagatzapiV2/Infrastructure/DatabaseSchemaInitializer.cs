using MagatzapiV2.Data;
using Microsoft.EntityFrameworkCore;

namespace MagatzapiV2.Infrastructure;

public static class DatabaseSchemaInitializer
{
    public static async Task EnsureOperationalTablesAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS stock_moviments (
                id INT NOT NULL AUTO_INCREMENT,
                tipus VARCHAR(30) NOT NULL,
                id_producte INT NOT NULL,
                id_lot INT NULL,
                id_ubicacio_origen INT NULL,
                id_ubicacio_desti INT NULL,
                quantitat INT NOT NULL,
                total_origen_abans INT NULL,
                total_origen_despres INT NULL,
                reservat_origen_abans INT NULL,
                reservat_origen_despres INT NULL,
                total_desti_abans INT NULL,
                total_desti_despres INT NULL,
                reservat_desti_abans INT NULL,
                reservat_desti_despres INT NULL,
                motiu VARCHAR(255) NULL,
                data_moviment DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (id),
                INDEX idx_stock_moviments_producte (id_producte),
                INDEX idx_stock_moviments_lot (id_lot),
                INDEX idx_stock_moviments_ubicacio_origen (id_ubicacio_origen),
                INDEX idx_stock_moviments_ubicacio_desti (id_ubicacio_desti)
            );
            """);
    }
}
