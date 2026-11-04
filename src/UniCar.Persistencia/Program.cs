using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using UniCar.Persistencia;
using UniCar.Persistencia.Dados;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CurrentCulture = new CultureInfo("pt-BR");

using var db = new UniCarContext();
// Cria o unicar.db na primeira vez e aplica as migrations que faltarem.
db.Database.Migrate();

if (args.Contains("--demo"))
{
    Demo.Rodar(db);
    return;
}

new Menu(db).Rodar();
