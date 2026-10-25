// Calcula PERT y CPM a partir de actividades.csv y escribe pert-cpm.md.
// Uso (desde docs/planificacion):  dotnet run PertCpm.cs
//
// Columnas del CSV (separador ;):
//   Id;EDT;Paquete;Responsable;Predecesoras;PorRecurso;O;M;P;Issue
// - Issue: número del issue de GitHub de la actividad (opcional).
// - Predecesoras: dependencias técnicas (el trabajo necesita el resultado de otra actividad).
// - PorRecurso: la misma persona no puede hacer dos paquetes a la vez, así que el paquete
//   espera al anterior de su responsable aunque no dependa técnicamente de él.

using System.Globalization;
using System.Text;

const double DiasPorIteracion = 7;
// Día 1 = jueves 15/10/2026, inicio de la iteración 1 (iteraciones de jueves a miércoles).
var dia1 = new DateOnly(2026, 10, 15);
const double MaxDiasPorPaquete = 5;
var es = CultureInfo.GetCultureInfo("es-ES");

string F(double v, int dec = 2) => v.ToString("F" + dec, es);

var dir = AppContext.GetData("EntryPointFileDirectoryPath") as string ?? Directory.GetCurrentDirectory();
var csvPath = args.Length > 0 ? args[0] : Path.Combine(dir, "actividades.csv");
var outPath = args.Length > 1 ? args[1] : Path.Combine(dir, "pert-cpm.md");

var acts = new List<Act>();
foreach (var line in File.ReadAllLines(csvPath, Encoding.UTF8).Skip(1))
{
    if (string.IsNullOrWhiteSpace(line)) continue;
    var c = line.Split(';');
    acts.Add(new Act(
        c[0].Trim(), c[1].Trim(), c[2].Trim(), c[3].Trim(),
        Lista(c[4]), Lista(c[5]),
        double.Parse(c[6], CultureInfo.InvariantCulture),
        double.Parse(c[7], CultureInfo.InvariantCulture),
        double.Parse(c[8], CultureInfo.InvariantCulture),
        c.Length > 9 ? c[9].Trim() : ""));
}

static string[] Lista(string s) =>
    s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

var porId = acts.ToDictionary(a => a.Id);
var errores = new List<string>();
foreach (var a in acts)
{
    foreach (var p in a.Todas.Where(p => !porId.ContainsKey(p)))
        errores.Add($"{a.Id}: predecesora desconocida {p}");
    if (!(a.O <= a.M && a.M <= a.P))
        errores.Add($"{a.Id}: debe cumplirse O ≤ M ≤ P");
}
if (errores.Count > 0)
{
    foreach (var e in errores) Console.Error.WriteLine(e);
    return 1;
}

// Orden topológico (Kahn); si queda algo sin ordenar hay un ciclo.
var entrantes = acts.ToDictionary(a => a.Id, a => a.Todas.Length);
var sucesoras = acts.ToDictionary(a => a.Id, _ => new List<string>());
foreach (var a in acts) foreach (var p in a.Todas) sucesoras[p].Add(a.Id);
var cola = new Queue<string>(acts.Where(a => entrantes[a.Id] == 0).Select(a => a.Id));
var orden = new List<Act>();
while (cola.Count > 0)
{
    var id = cola.Dequeue();
    orden.Add(porId[id]);
    foreach (var s in sucesoras[id])
        if (--entrantes[s] == 0) cola.Enqueue(s);
}
if (orden.Count != acts.Count)
{
    Console.Error.WriteLine("Hay un ciclo en las dependencias.");
    return 1;
}

// Ida: inicio y fin más temprano.
foreach (var a in orden)
{
    a.IT = a.Todas.Length == 0 ? 0 : a.Todas.Max(p => porId[p].FT);
    a.FT = a.IT + a.TE;
}
var duracion = acts.Max(a => a.FT);

// Vuelta: inicio y fin más tardío.
foreach (var a in Enumerable.Reverse(orden))
{
    a.FL = sucesoras[a.Id].Count == 0 ? duracion : sucesoras[a.Id].Min(s => porId[s].IL);
    a.IL = a.FL - a.TE;
}

var criticas = orden.Where(a => a.Critica).OrderBy(a => a.IT).ToList();
var varianza = criticas.Sum(a => a.Sigma * a.Sigma);
var sigmaProyecto = Math.Sqrt(varianza);
int Iter(int dia) => (int)((dia - 1) / DiasPorIteracion) + 1;

var sb = new StringBuilder();
sb.AppendLine("# Resultado PERT / CPM");
sb.AppendLine();
sb.AppendLine("> Archivo generado por `PertCpm.cs` a partir de `actividades.csv`. No editar a mano: cambiar el CSV y volver a ejecutar `dotnet run PertCpm.cs`.");
sb.AppendLine();
sb.AppendLine("Unidad: días de trabajo de la pareja (se trabaja de jueves a miércoles). TE = (O + 4M + P) ÷ 6 · σ = (P − O) ÷ 6.");
sb.AppendLine();

sb.AppendLine("## 1. Estimación PERT");
sb.AppendLine();
sb.AppendLine("| Act. | EDT | Paquete de trabajo | Responsable | Pred. | O | M | P | TE | σ |");
sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|");
foreach (var a in acts)
{
    var pred = string.Join(", ", a.Pred.Concat(a.Recurso.Select(r => r + "*")));
    sb.AppendLine($"| {a.Id} | {a.Edt} | {a.Nombre} | {a.Resp} | {(pred == "" ? "—" : pred)} | {F(a.O, 0)} | {F(a.M, 0)} | {F(a.P, 0)} | {F(a.TE)} | {F(a.Sigma)} |");
}
sb.AppendLine();
sb.AppendLine("\\* Dependencia por recurso: la misma persona termina su paquete anterior antes de empezar este.");
sb.AppendLine();

sb.AppendLine("## 2. CPM: ida, vuelta y holgura");
sb.AppendLine();
sb.AppendLine("| Act. | Paquete | IT | Dur. | FT | IL | FL | Holgura | Ruta crítica |");
sb.AppendLine("|---|---|---|---|---|---|---|---|---|");
foreach (var a in orden.OrderBy(a => a.IT).ThenBy(a => a.Edt))
    sb.AppendLine($"| {a.Id} | {a.Nombre} | {F(a.IT)} | {F(a.TE)} | {F(a.FT)} | {F(a.IL)} | {F(a.FL)} | {F(a.Holgura)} | {(a.Critica ? "**Sí**" : "")} |");
sb.AppendLine();
sb.AppendLine("IT/FT: inicio y fin más temprano (ida) · IL/FL: inicio y fin más tardío (vuelta) · Holgura = IL − IT.");
sb.AppendLine();

sb.AppendLine("## 3. Ruta crítica y duración del proyecto");
sb.AppendLine();
sb.AppendLine($"**Ruta crítica:** {string.Join(" › ", criticas.Select(a => a.Id))}");
sb.AppendLine();
sb.AppendLine($"- Duración esperada: **{F(duracion)} días** (≈ {F(duracion / DiasPorIteracion, 1)} iteraciones de {DiasPorIteracion} días).");
sb.AppendLine($"- Desviación de la ruta crítica: σ = √(Σσ²) = √{F(varianza)} = **{F(sigmaProyecto)} días**.");
sb.AppendLine($"- Con 84 % de probabilidad termina en {F(duracion + sigmaProyecto, 1)} días o menos (TE + 1σ).");
sb.AppendLine($"- Con 95 % de probabilidad termina en {F(duracion + 1.645 * sigmaProyecto, 1)} días o menos (TE + 1,645σ).");
sb.AppendLine();

sb.AppendLine("## 4. Carga por responsable");
sb.AppendLine();
sb.AppendLine("| Responsable | Paquetes | Días (Σ TE) |");
sb.AppendLine("|---|---|---|");
foreach (var g in acts.GroupBy(a => a.Resp).OrderBy(g => g.Key))
    sb.AppendLine($"| {g.Key} | {g.Count()} | {F(g.Sum(a => a.TE))} |");
sb.AppendLine();

sb.AppendLine("## 5. Fechas, iteraciones e issues de GitHub");
sb.AppendLine();
sb.AppendLine($"Día 1 = jueves {dia1:dd/MM/yyyy}, inicio de la iteración 1 (iteraciones de jueves a miércoles). Inicio = día siguiente a IT; vencimiento = FT redondeado hacia arriba.");
sb.AppendLine();
sb.AppendLine("| Issue (formato del PDF) | Responsable | Inicio | Vencimiento | Iteración | Etiquetas |");
sb.AppendLine("|---|---|---|---|---|---|");
foreach (var a in orden.OrderBy(a => a.IT).ThenBy(a => a.Edt))
{
    var inicio = (int)Math.Floor(a.IT + 1e-9) + 1;
    var fin = Math.Max(inicio, (int)Math.Ceiling(a.FT - 1e-9));
    var (ii, ifin) = (Iter(inicio), Iter(fin));
    var iter = ii == ifin ? $"{ii}" : $"{ii}–{ifin}";
    var fase = "Paquete " + a.Edt.Split('.')[0] + ".0";
    var producto = a.Resp switch { "Jean" => " · Web", "Melanie" => " · Móvil", _ => " · Web y Móvil" };
    var critica = a.Critica ? " · Ruta crítica" : "";
    var issue = a.Issue == "" ? "#—" : "#" + a.Issue;
    string Fecha(int dia) => dia1.AddDays(dia - 1).ToString("ddd dd/MM", es);
    sb.AppendLine($"| [{a.Edt}] {issue} {a.Nombre} | {a.Resp} | {Fecha(inicio)} | {Fecha(fin)} | {iter} | {fase}{producto}{critica} |");
}
sb.AppendLine();
sb.AppendLine("Formato del PDF del curso: `[EDT] #issue Título`. La rama de cada actividad es `feature/<issue>-<descripcion>`.");
sb.AppendLine();

var largos = acts.Where(a => a.TE > MaxDiasPorPaquete).ToList();
sb.AppendLine("## 6. Validaciones");
sb.AppendLine();
sb.AppendLine($"- Paquetes con TE > {F(MaxDiasPorPaquete, 0)} días (regla «paquete ≤ 1 semana»): {(largos.Count == 0 ? "ninguno ✔" : string.Join(", ", largos.Select(a => $"{a.Id} ({F(a.TE)})")))}");
sb.AppendLine("- Dependencias sin ciclos y todas las predecesoras existen ✔");
sb.AppendLine("- En todas las actividades O ≤ M ≤ P ✔");

File.WriteAllText(outPath, sb.ToString(), new UTF8Encoding(false));
Console.WriteLine($"Duración {F(duracion)} días · σ {F(sigmaProyecto)} · ruta crítica: {string.Join(" > ", criticas.Select(a => a.Id))}");
foreach (var g in acts.GroupBy(a => a.Resp)) Console.WriteLine($"{g.Key}: {F(g.Sum(a => a.TE))} días");
Console.WriteLine($"Paquetes > {MaxDiasPorPaquete} días: {(largos.Count == 0 ? "ninguno" : string.Join(", ", largos.Select(a => a.Id)))}");
Console.WriteLine($"Escrito: {outPath}");
return 0;

class Act(string id, string edt, string nombre, string resp, string[] pred, string[] recurso, double o, double m, double p, string issue)
{
    public string Issue { get; } = issue;
    public string Id { get; } = id;
    public string Edt { get; } = edt;
    public string Nombre { get; } = nombre;
    public string Resp { get; } = resp;
    public string[] Pred { get; } = pred;
    public string[] Recurso { get; } = recurso;
    public string[] Todas { get; } = pred.Concat(recurso).Distinct().ToArray();
    public double O { get; } = o;
    public double M { get; } = m;
    public double P { get; } = p;
    public double TE => Math.Round((O + 4 * M + P) / 6, 2);
    public double Sigma => Math.Round((P - O) / 6, 2);
    public double IT, FT, IL, FL;
    public double Holgura => Math.Round(IL - IT, 2);
    public bool Critica => Math.Abs(Holgura) < 0.005;
}
