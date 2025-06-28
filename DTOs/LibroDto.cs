namespace BBAPP.DTOs
{
    public class LibroDto
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Autor { get; set; }
        public int CopiasDisponibles { get; set; }
        public int TotalCopias { get; set; }
        public string Genero { get; set; }
        public int AnioPublicacion { get; set; }
    }
}
