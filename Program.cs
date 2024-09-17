using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

public class Program
{
    public static void Main(string[] args)
    {
        var archivoManager = new ArchivoManager();
        while (true)
        {
            Console.Clear();
            Console.WriteLine("Menú:");
            Console.WriteLine("1. Crear archivo");
            Console.WriteLine("2. Listar archivos");
            Console.WriteLine("3. Abrir archivo");
            Console.WriteLine("4. Modificar archivo");
            Console.WriteLine("5. Eliminar archivo");
            Console.WriteLine("6. Recuperar archivo");
            Console.WriteLine("7. Salir");
            Console.Write("Selecciona una opción: ");

            string opcion = Console.ReadLine()!;
            switch (opcion)
            {
                case "1":
                    archivoManager.CrearArchivo();
                    break;
                case "2":
                    archivoManager.ListarArchivos();
                    break;
                case "3":
                    archivoManager.AbrirArchivo();
                    break;
                case "4":
                    archivoManager.ModificarArchivo();
                    break;
                case "5":
                    archivoManager.EliminarArchivo();
                    break;
                case "6":
                    archivoManager.RecuperarArchivo();
                    break;
                case "7":
                    return; // Salir del programa
                default:
                    Console.WriteLine("Opción no válida. Inténtalo de nuevo.");
                    break;
            }

            Console.WriteLine("Presiona cualquier tecla para volver al menú...");
            Console.ReadKey();
        }
    }
}

public class ArchivoManager
{
    private string carpetaArchivos = "ArchivosFAT";

    private string ObtenerRutaDirectorio()
    {
        string escritorio = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string rutaDirectorio = Path.Combine(escritorio, carpetaArchivos);

        if (!Directory.Exists(rutaDirectorio))
        {
            Directory.CreateDirectory(rutaDirectorio);
        }

        return rutaDirectorio;
    }

    public void CrearArchivo()
    {
        string directorioRaiz = ObtenerRutaDirectorio();
        Console.Write("Introduce el nombre del archivo: ");
        string nombreArchivo = Console.ReadLine()?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(nombreArchivo))
        {
            Console.WriteLine("El nombre del archivo no puede estar vacío.");
            return;
        }

        Console.WriteLine("Introduce los datos para el archivo y presiona Enter para terminar:");
        string datos = LeerDatosDesdeConsolaSimple();

        var fatTable = new FatTable
        {
            NombreArchivo = nombreArchivo,
            RutaArchivo = Path.Combine(directorioRaiz, $"{nombreArchivo}_datos.json"),
            PapeleraReciclaje = false,
            CantidadCaracteres = datos.Length,
            FechaCreacion = DateTime.Now,
            FechaModificacion = DateTime.Now
        };

        // Guardar la tabla FAT
        GuardarTablaFat(fatTable);

        // Dividir datos y guardar en fragmentos
        DividirYGuardarDatos(datos, fatTable.RutaArchivo);

        // Confirmación al usuario
        Console.WriteLine($"Archivo '{nombreArchivo}' creado exitosamente.");
    }

    public void ListarArchivos()
    {
        string directorioRaiz = ObtenerRutaDirectorio();
        var archivos = Directory.GetFiles(directorioRaiz, "*_datos.json");

        if (archivos.Length == 0)
        {
            Console.WriteLine("No hay archivos para mostrar.");
            return;
        }

        Console.WriteLine("Archivos disponibles:");
        int i = 1;
        foreach (var archivo in archivos)
        {
            var rutaFatTable = archivo.Replace("_datos.json", "_fat.json");
            if (File.Exists(rutaFatTable))
            {
                var fatTable = CargarTablaFat(rutaFatTable);
                if (!fatTable.PapeleraReciclaje)
                {
                    Console.WriteLine($"{i++}. {fatTable.NombreArchivo} - {fatTable.CantidadCaracteres} caracteres - Creación: {fatTable.FechaCreacion} - Modificación: {fatTable.FechaModificacion}");
                }
            }
        }

        if (i == 1)
        {
            Console.WriteLine("No hay archivos disponibles.");
        }
    }

    public void AbrirArchivo()
    {
        ListarArchivos();
        string directorioRaiz = ObtenerRutaDirectorio();
        Console.Write("Selecciona el número del archivo a abrir: ");
        string inputIndex = Console.ReadLine()!;
        if (!int.TryParse(inputIndex, out int index) || index < 1)
        {
            Console.WriteLine("Selección no válida.");
            return;
        }

        var archivos = Directory.GetFiles(directorioRaiz, "*_datos.json");
        index--; // Ajustar el índice porque la lista está basada en cero

        if (index >= 0 && index < archivos.Length)
        {
            var archivo = archivos[index];
            var rutaFatTable = archivo.Replace("_datos.json", "_fat.json");
            var fatTable = CargarTablaFat(rutaFatTable);
            Console.WriteLine($"Nombre: {fatTable.NombreArchivo}");
            Console.WriteLine($"Tamaño total: {fatTable.CantidadCaracteres} caracteres");
            Console.WriteLine($"Fecha de creación: {fatTable.FechaCreacion}");
            Console.WriteLine($"Fecha de modificación: {fatTable.FechaModificacion}");
            Console.WriteLine("Contenido:");
            Console.WriteLine(LeerContenidoArchivo(archivo));
        }
        else
        {
            Console.WriteLine("Selección no válida.");
        }
    }

    public void ModificarArchivo()
    {
        AbrirArchivo(); // Muestra la lista de archivos y permite seleccionar uno
        string directorioRaiz = ObtenerRutaDirectorio();

        Console.Write("Introduce el nombre del archivo a modificar (sin extensión): ");
        string nombreArchivo = Console.ReadLine()?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(nombreArchivo))
        {
            Console.WriteLine("El nombre del archivo no puede estar vacío.");
            return;
        }

        string rutaArchivoDatos = Path.Combine(directorioRaiz, $"{nombreArchivo}_datos.json");
        if (!File.Exists(rutaArchivoDatos))
        {
            Console.WriteLine("El archivo no existe.");
            return;
        }

        Console.WriteLine("Introduce el nuevo contenido (presiona Enter para terminar):");
        string nuevoContenido = LeerDatosDesdeConsolaSimple();

        var fatTable = CargarTablaFat(Path.Combine(directorioRaiz, $"{nombreArchivo}_fat.json"));
        if (fatTable.PapeleraReciclaje)
        {
            Console.WriteLine("El archivo está en la papelera de reciclaje y no puede ser modificado.");
            return;
        }

        var nuevoFatTable = new FatTable
        {
            NombreArchivo = fatTable.NombreArchivo,
            RutaArchivo = rutaArchivoDatos,
            PapeleraReciclaje = false,
            CantidadCaracteres = nuevoContenido.Length,
            FechaCreacion = fatTable.FechaCreacion,
            FechaModificacion = DateTime.Now
        };

        Console.Write($"¿Deseas guardar los cambios en el archivo '{fatTable.NombreArchivo}'? (s/n): ");
        string confirmacion = Console.ReadLine()?.Trim().ToLower() ?? string.Empty;
        if (confirmacion != "s")
        {
            Console.WriteLine("Modificación cancelada.");
            return;
        }

        // Eliminar todos los fragmentos viejos
        EliminarArchivosViejos(fatTable.RutaArchivo);

        // Guardar los nuevos datos
        DividirYGuardarDatos(nuevoContenido, nuevoFatTable.RutaArchivo);
        GuardarTablaFat(nuevoFatTable);

        Console.WriteLine($"Archivo '{fatTable.NombreArchivo}' modificado exitosamente.");
    }


    public void EliminarArchivo()
    {
        ListarArchivos();
        string directorioRaiz = ObtenerRutaDirectorio();
        Console.Write("Selecciona el número del archivo a eliminar: ");
        string inputIndex = Console.ReadLine()!;
        if (!int.TryParse(inputIndex, out int index) || index < 1)
        {
            Console.WriteLine("Selección no válida.");
            return;
        }

        var archivos = Directory.GetFiles(directorioRaiz, "*_datos.json");
        index--; // Ajustar el índice porque la lista está basada en cero

        if (index >= 0 && index < archivos.Length)
        {
            var archivoDatos = archivos[index];
            var archivoFat = archivoDatos.Replace("_datos.json", "_fat.json");
            var fatTable = CargarTablaFat(archivoFat);

            Console.WriteLine($"Nombre: {fatTable.NombreArchivo}");
            Console.WriteLine($"Tamaño: {fatTable.CantidadCaracteres} caracteres");
            Console.Write($"¿Deseas eliminar el archivo '{fatTable.NombreArchivo}'? (s/n): ");
            if (Console.ReadLine()!.ToLower() == "s")
            {
                // Crear copia de seguridad del archivo de datos
                string copiaRespaldo = archivoDatos + ".backup";
                File.Copy(archivoDatos, copiaRespaldo, true);

                // Marcar archivo como eliminado en FAT
                fatTable.PapeleraReciclaje = true;
                fatTable.FechaEliminacion = DateTime.Now;
                fatTable.RutaArchivoBackup = copiaRespaldo; // Guardar ruta de respaldo
                GuardarTablaFat(fatTable);

                // Eliminar el archivo de datos
                File.Delete(archivoDatos);
                Console.WriteLine($"Archivo '{fatTable.NombreArchivo}' eliminado exitosamente.");
            }
            else
            {
                Console.WriteLine("Eliminación cancelada.");
            }
        }
        else
        {
            Console.WriteLine("Selección no válida.");
        }
    }

    public void RecuperarArchivo()
    {
        string directorioRaiz = ObtenerRutaDirectorio();
        var archivosEliminados = Directory.GetFiles(directorioRaiz, "*_fat.json")
                                        .Where(file => CargarTablaFat(file).PapeleraReciclaje)
                                        .ToList();

        if (archivosEliminados.Count == 0)
        {
            Console.WriteLine("No hay archivos en la papelera de reciclaje.");
            return;
        }

        Console.WriteLine("Archivos en la papelera de reciclaje:");
        int i = 1;
        foreach (var archivo in archivosEliminados)
        {
            var fatTable = CargarTablaFat(archivo);
            Console.WriteLine($"{i++}. {fatTable.NombreArchivo} - Eliminado: {fatTable.FechaEliminacion}");
        }

        Console.Write("Selecciona el número del archivo a recuperar: ");
        string inputIndex = Console.ReadLine()!;
        if (!int.TryParse(inputIndex, out int index) || index < 1)
        {
            Console.WriteLine("Selección no válida.");
            return;
        }

        index--; // Ajustar el índice porque la lista está basada en cero
        if (index >= 0 && index < archivosEliminados.Count)
        {
            var archivoFat = archivosEliminados[index];
            var fatTable = CargarTablaFat(archivoFat);
            if (File.Exists(fatTable.RutaArchivoBackup))
            {
                File.Copy(fatTable.RutaArchivoBackup, fatTable.RutaArchivo, true);
                fatTable.PapeleraReciclaje = false;
                fatTable.FechaEliminacion = null;
                GuardarTablaFat(fatTable);
                Console.WriteLine($"Archivo '{fatTable.NombreArchivo}' recuperado exitosamente.");
            }
            else
            {
                Console.WriteLine("El archivo de respaldo no existe.");
            }
        }
        else
        {
            Console.WriteLine("Selección no válida.");
        }
    }

    private string LeerDatosDesdeConsolaSimple()
    {
        string contenido = "";
        string linea;
        while ((linea = Console.ReadLine()!) != null && linea != "")
        {
            contenido += linea + Environment.NewLine;
        }
        return contenido.TrimEnd(Environment.NewLine.ToCharArray());
    }

    private void EliminarArchivosViejos(string rutaArchivo)
    {
        int fragmento = 0;
        while (File.Exists($"{rutaArchivo}_{fragmento}.json"))
        {
            File.Delete($"{rutaArchivo}_{fragmento}.json");
            fragmento++;
        }
    }

    private void DividirYGuardarDatos(string datos, string rutaArchivo)
    {
        int fragmentoSize = 1024; // Tamaño del fragmento en bytes
        int fragmentoCount = (int)Math.Ceiling((double)datos.Length / fragmentoSize);

        for (int i = 0; i < fragmentoCount; i++)
        {
            string fragmento = datos.Substring(i * fragmentoSize, Math.Min(fragmentoSize, datos.Length - i * fragmentoSize));
            File.WriteAllText($"{rutaArchivo}_{i}.json", fragmento);
        }
    }

    private string LeerContenidoArchivo(string rutaArchivo)
    {
        var fragmentos = Directory.GetFiles(Path.GetDirectoryName(rutaArchivo)!, $"{Path.GetFileNameWithoutExtension(rutaArchivo)}_*.json");
        return fragmentos.OrderBy(f => int.Parse(Path.GetFileNameWithoutExtension(f).Split('_').Last())).Aggregate("", (current, fragmento) => current + File.ReadAllText(fragmento));
    }

    private void GuardarTablaFat(FatTable fatTable)
    {
        string rutaFatTable = fatTable.RutaArchivo.Replace("_datos.json", "_fat.json");
        File.WriteAllText(rutaFatTable, JsonSerializer.Serialize(fatTable));
    }

    private FatTable CargarTablaFat(string rutaFatTable)
    {
        if (!File.Exists(rutaFatTable))
        {
            throw new FileNotFoundException("El archivo de la tabla FAT no se encontró.", rutaFatTable);
        }

        return JsonSerializer.Deserialize<FatTable>(File.ReadAllText(rutaFatTable)) ?? throw new Exception("Error al cargar la tabla FAT.");
    }

    private string ObtenerRutaArchivoDatos()
    {
        Console.Write("Introduce el nombre del archivo (sin extensión): ");
        string nombreArchivo = Console.ReadLine()?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(nombreArchivo))
        {
            throw new ArgumentException("El nombre del archivo no puede estar vacío.");
        }

        return Path.Combine(ObtenerRutaDirectorio(), $"{nombreArchivo}_datos.json");
    }
}