using System.ComponentModel.DataAnnotations;

namespace DealFinder.Api.Models;

public class ProductSearchRequest
{
    [Required]
    [MaxLength(100)]
    public string Query { get; set; } = "";
}