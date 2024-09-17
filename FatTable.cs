using System;

public class FatTable
{
    public string NombreArchivo { get; set; } = string.Empty;
    public string RutaArchivo { get; set; } = string.Empty;
     public string RutaArchivoBackup { get; set; } = string.Empty;
    public bool PapeleraReciclaje { get; set; }
    public int CantidadCaracteres { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaModificacion { get; set; }
    public DateTime? FechaEliminacion { get; set; }
}
