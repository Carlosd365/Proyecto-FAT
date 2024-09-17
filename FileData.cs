using System;

public class FileData
{
    public string Datos { get; set; } = string.Empty;
    public string? SiguienteArchivo { get; set; }
    public bool EOF { get; set; }
}
