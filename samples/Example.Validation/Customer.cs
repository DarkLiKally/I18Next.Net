using System.ComponentModel.DataAnnotations;

namespace Example.Validation;

public class Customer
{
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string Name { get; set; }

    [Required]
    [EmailAddress]
    [Display(Name = "fields.email")]
    public string Email { get; set; }

    [Compare(nameof(Email))]
    [Display(Name = "fields.confirmEmail")]
    public string ConfirmEmail { get; set; }

    [Range(18, 120)]
    public int? Age { get; set; }

    [RegularExpression("^[A-Z]{2}[0-9]{4}$", ErrorMessage = "customerNumber")]
    [Display(Name = "fields.customerNumber")]
    public string CustomerNumber { get; set; }
}
