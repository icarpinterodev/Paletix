## Comanda utilitzada per generar els models a partir de la base de dades:

`dotnet ef dbcontext scaffold "server=localhost;database=paletix;user=root;password=ioni2005;" Pomelo.EntityFrameworkCore.MySql --output-dir Models --context-dir Data --context AppDbContext --no-onconfiguring --data-annotations --no-pluralize --force --verbose --project MagatzapiV2`

## Descripció de la comanda i parametres utilitzats:
- `dotnet ef dbcontext scaffold`: Comanda per generar els models a partir d'una base de dades.
- `"server=localhost;database=paletix;user=root;password=ioni2005;"`: Cadena de connexió a la base de dades MySQL.
- `Pomelo.EntityFrameworkCore.MySql`: Proveïdor de base de dades utilitzat per Entity Framework Core.
- `--output-dir Models`: Especifica el directori on es generaran els models.
- `--context-dir Data`: Especifica el directori on es generarà el context de la base de dades.
- `--context AppDbContext`: Especifica el nom del context de la base de dades que es generarà.
- `--no-onconfiguring`: Indica que no es generarà el mètode `OnConfiguring` al context de la base de dades.
- `--data-annotations`: Indica que es generaran anotacions de dades en els models.
- `--no-pluralize`: Indica que no es pluralitzaran els noms de les taules per generar els models.
- `--force`: Indica que es sobreescriuran els fitxers existents sense preguntar.
-	`--verbose`: Mostra informació detallada durant l'execució de la comanda.
- `--project MagatzapiV2`: Especifica el projecte on es generaran els models i el context de la base de dades.
