using System.ComponentModel.DataAnnotations;

namespace FinMind.Web.Models;

public class FetchRequestViewModel
{
    [Required]
    [Display(Name = "Symbol")]
    public string Symbol { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Start Date")]
    [DataType(DataType.Date)]
    public DateOnly? StartDate { get; set; }

    [Required]
    [Display(Name = "End Date")]
    [DataType(DataType.Date)]
    public DateOnly? EndDate { get; set; }

    public string OutputMessage { get; set; } = string.Empty;

    public string ErrorMessage { get; set; } = string.Empty;

    public string CommandOutput { get; set; } = string.Empty;
}
