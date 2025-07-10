namespace BBAPP.Data.Models
{
    public class Multa
    {
        public int Id { get; set; }
        public int PrestamoId { get; set; }
        public string UsuarioId { get; set; }
        public DateTime FechaMulta { get; set; }
        public decimal Monto { get; set; }
        public bool Pagada { get; set; }

        // Propiedades de navegación
        public Prestamo Prestamo { get; set; }
        public UsuarioAplicacion Usuario { get; set; }
    }
}
