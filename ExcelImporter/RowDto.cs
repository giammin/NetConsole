using System.ComponentModel.DataAnnotations;

namespace ExcelImporter
{
    public record RowDto
    {
        [Required, EmailAddress]
        public string? Email { get; set; }
        [Required,StringLength(50)]
        public string? Name { get; set; }
        [Required,StringLength(50)]
        public string? LastName { get; set; }
        [StringLength(50)]
        public string? Organization { get; set; }
    }
}