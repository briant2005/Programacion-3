namespace BBAPP.Data.Models
{
    public class Libro
    {
        public int id { get; set; }
        public int Id_Libro { get; set; }
        public string Titulo { get; set; }
        public string Autor { get; set; }
        public int Stock { get; set; }
        public int CopiasDisponibles { get; set; }
        public int TotalCopias { get; set; }
        public string Genero { get; set; }
        public int AnioPublicacion { get; set; }
        public bool EstaActivo { get; set; } = true; // Indica si el libro todavía está en circulación

        // Propiedad de navegación para préstamos
        public ICollection<Prestamo> Prestamos { get; set; }

        public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();

        // … el resto de columnas
    }
}
