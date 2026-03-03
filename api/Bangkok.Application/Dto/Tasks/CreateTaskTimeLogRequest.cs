using System.ComponentModel.DataAnnotations;

namespace Bangkok.Application.Dto.Tasks;

public class CreateTaskTimeLogRequest
{
    [Required]
    [Range(0.01, 999.99)]
    public decimal Hours { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}
