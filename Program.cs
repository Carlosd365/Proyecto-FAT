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
        string nombreArchivo = Console.ReadLine()!;
        Console.WriteLine("Introduce los datos para el archivo y presiona Enter para terminar:");
        string datos = LeerDatosDesdeConsolaSimple();

        var fatTable = new FatTable
        {
            NombreArchivo = nombreArchivo,
            RutaArchivo = $"{directorioRaiz}/{nombreArchivo}_datos.json",
            PapeleraReciclaje = false,
            CantidadPalabras = datos.Split(' ').Length,
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
                    Console.WriteLine($"{i++}. {fatTable.NombreArchivo} - {fatTable.CantidadPalabras} palabras - Creación: {fatTable.FechaCreacion} - Modificación: {fatTable.FechaModificacion}");
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
        int index = int.Parse(Console.ReadLine()!) - 1;

        var archivos = Directory.GetFiles(directorioRaiz, "*_datos.json");
        if (index >= 0 && index < archivos.Length)
        {
            var archivo = archivos[index];
            var rutaFatTable = archivo.Replace("_datos.json", "_fat.json");
            var fatTable = CargarTablaFat(rutaFatTable);
            Console.WriteLine($"Nombre: {fatTable.NombreArchivo}");
            Console.WriteLine($"Cantidad total de palabras: {fatTable.CantidadPalabras}");
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
        AbrirArchivo();
        string directorioRaiz = ObtenerRutaDirectorio();
        Console.WriteLine("Introduce el nuevo contenido (presiona Enter para terminar):");
        string nuevoContenido = LeerDatosDesdeConsolaSimple();

        string rutaArchivoDatos = ObtenerRutaArchivoDatos();
        var fatTable = CargarTablaFat(rutaArchivoDatos.Replace("_datos.json", "_fat.json"));
        var nuevoFatTable = new FatTable
        {
            NombreArchivo = fatTable.NombreArchivo,
            RutaArchivo = $"{directorioRaiz}/{fatTable.NombreArchivo}_datos.json",
            PapeleraReciclaje = false,
            CantidadPalabras = nuevoContenido.Split(' ').Length,
            FechaCreacion = fatTable.FechaCreacion,
            FechaModificacion = DateTime.Now
        };

        Console.Write($"¿Deseas guardar los cambios en el archivo '{fatTable.NombreArchivo}'? (s/n): ");
        if (Console.ReadLine()!.ToLower() == "s")
        {
            // Eliminar los fragmentos antiguos
            EliminarArchivosViejos(fatTable.RutaArchivo);

            // Dividir y guardar los nuevos datos
            DividirYGuardarDatos(nuevoContenido, nuevoFatTable.RutaArchivo);
            GuardarTablaFat(nuevoFatTable);
            Console.WriteLine($"Archivo '{fatTable.NombreArchivo}' modificado exitosamente.");
        }
        else
        {
            Console.WriteLine("Modificación cancelada.");
        }
    }

    public void EliminarArchivo()
    {
        ListarArchivos();
        string directorioRaiz = ObtenerRutaDirectorio();
        Console.Write("Selecciona el número del archivo a eliminar: ");
        int index = int.Parse(Console.ReadLine()!) - 1;

        var archivos = Directory.GetFiles(directorioRaiz, "*_datos.json");
        if (index >= 0 && index < archivos.Length)
        {
            var archivoDatos = archivos[index];
            var archivoFat = archivoDatos.Replace("_datos.json", "_fat.json");
            var fatTable = CargarTablaFat(archivoFat);

            Console.WriteLine($"Nombre: {fatTable.NombreArchivo}");
            Console.WriteLine($"Cantidad de palabras: {fatTable.CantidadPalabras}");
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
        foreach (var archivoFat in archivosEliminados)
        {
            var fatTable = CargarTablaFat(archivoFat);
            Console.WriteLine($"{i++}. {fatTable.NombreArchivo} - Fecha de eliminación: {fatTable.FechaEliminacion}");
        }

        Console.Write("Selecciona el número del archivo a recuperar: ");
        int index = int.Parse(Console.ReadLine()!) - 1;

        if (index >= 0 && index < archivosEliminados.Count)
        {
            var archivoFat = archivosEliminados[index];
            var fatTable = CargarTablaFat(archivoFat);

            if (File.Exists(fatTable.RutaArchivoBackup))
            {
                // Restaurar archivo desde respaldo
                File.Copy(fatTable.RutaArchivoBackup, fatTable.RutaArchivo, true);
                fatTable.PapeleraReciclaje = false;
                fatTable.FechaEliminacion = null; // Limpiar la fecha de eliminación
                GuardarTablaFat(fatTable);

                // Eliminar el archivo de respaldo después de restaurar
                File.Delete(fatTable.RutaArchivoBackup);

                Console.WriteLine($"Archivo '{fatTable.NombreArchivo}' recuperado exitosamente.");
            }
            else
            {
                Console.WriteLine("No se encontró el archivo de respaldo.");
            }
        }
        else
        {
            Console.WriteLine("Selección no válida.");
        }
    }

    private string LeerDatosDesdeConsolaSimple()
    {
        var palabras = new List<string>();
        string palabra;
        while ((palabra = Console.ReadLine()!) != string.Empty)
        {
            palabras.Add(palabra);
        }
        return string.Join(" ", palabras);
    }

    private void DividirYGuardarDatos(string datos, string rutaArchivo)
    {
        string directorioRaiz = ObtenerRutaDirectorio();
        int maxPalabras = 20;
        string archivoActual = rutaArchivo;
        var palabras = datos.Split(' ');

        for (int i = 0; i < palabras.Length; i += maxPalabras)
        {
            var fragmento = string.Join(" ", palabras.Skip(i).Take(maxPalabras));
            bool esUltimo = i + maxPalabras >= palabras.Length;
            string siguienteArchivo = esUltimo ? null : $"{Path.GetFileNameWithoutExtension(rutaArchivo)}_{i / maxPalabras + 1}.json";

            var fileData = new FileData
            {
                Datos = fragmento,
                SiguienteArchivo = siguienteArchivo,
                EOF = esUltimo
            };

            File.WriteAllText(archivoActual, JsonSerializer.Serialize(fileData));
            if (!esUltimo)
            {
                archivoActual = Path.Combine(directorioRaiz, siguienteArchivo);
            }
        }
    }

    private string LeerContenidoArchivo(string rutaArchivo)
    {
        var contenido = new List<string>();
        string archivoActual = rutaArchivo;
        
        while (archivoActual != null)
        {
            
            if (File.Exists(archivoActual))
            {
                var fileData = JsonSerializer.Deserialize<FileData>(File.ReadAllText(archivoActual));
                if (fileData != null)
                {
                    contenido.Add(fileData.Datos);
                    archivoActual = fileData.SiguienteArchivo != null
                        ? Path.Combine(Path.GetDirectoryName(rutaArchivo)!, fileData.SiguienteArchivo)
                        : null;
                }
                else
                {
                    Console.WriteLine($"Advertencia: El contenido del archivo '{archivoActual}' está vacío o dañado.");
                    archivoActual = null; // Terminar el bucle
                }
            }
            else
            {
                Console.WriteLine($"Advertencia: No se encontró el archivo '{archivoActual}'.");
                archivoActual = null; // Terminar el bucle
            }
        }
        
        return string.Join(" ", contenido);
    }


    private void EliminarArchivosViejos(string rutaArchivoDatos)
    {
        string directorioRaiz = Path.GetDirectoryName(rutaArchivoDatos)!;
        string archivoBase = Path.GetFileNameWithoutExtension(rutaArchivoDatos);
        
        // Obtener todos los archivos en el directorio
        var archivosEnDirectorio = Directory.GetFiles(directorioRaiz, "*.json");

        foreach (var archivo in archivosEnDirectorio)
        {
            // Verificar si el archivo actual es un fragmento asociado al archivo que estamos modificando
            if (EsArchivoFragmento(archivo, archivoBase))
            {
                if (File.Exists(archivo))
                {
                    File.Delete(archivo);
                }
            }
        }
    }

    // Función para verificar si un archivo es un fragmento del archivo base
    private bool EsArchivoFragmento(string archivo, string archivoBase)
    {
        // Verifica si el nombre del archivo contiene el nombre base y un sufijo de fragmento
        return archivo.Contains(archivoBase) && !archivo.EndsWith("_fat.json");
    }

    private void GuardarTablaFat(FatTable fatTable)
    {
        string rutaFat = fatTable.RutaArchivo.Replace("_datos.json", "_fat.json");
        File.WriteAllText(rutaFat, JsonSerializer.Serialize(fatTable));
    }

    private FatTable CargarTablaFat(string rutaFat)
    {
        if (File.Exists(rutaFat))
        {
            return JsonSerializer.Deserialize<FatTable>(File.ReadAllText(rutaFat))!;
        }
        else
        {
            throw new FileNotFoundException("La tabla FAT no existe.");
        }
    }
    private string ObtenerRutaArchivoDatos()
    {
        // Obtén el directorio raíz donde se guardarán los archivos
        string directorioRaiz = ObtenerRutaDirectorio();
        
        // Asegúrate de que el directorio raíz existe
        if (!Directory.Exists(directorioRaiz))
        {
            Console.WriteLine($"Error: El directorio '{directorioRaiz}' no existe.");
            return string.Empty;
        }
        
        // Solicita el nombre del archivo al usuario
        Console.Write("Introduce el nombre del archivo de datos: ");
        string nombreArchivo = Console.ReadLine()!;
        
        // Verifica si el nombre del archivo es válido
        if (string.IsNullOrWhiteSpace(nombreArchivo))
        {
            Console.WriteLine("Error: El nombre del archivo no puede estar vacío.");
            return string.Empty;
        }
        
        // Construye la ruta completa para el archivo de datos
        string rutaArchivo = Path.Combine(directorioRaiz, $"{nombreArchivo}_datos.json");
        
        // Opcionalmente, verifica si el archivo existe para informar al usuario
        if (!File.Exists(rutaArchivo))
        {
            Console.WriteLine($"Nota: El archivo '{rutaArchivo}' no existe.");
        }
        
        return rutaArchivo;
    }
}
