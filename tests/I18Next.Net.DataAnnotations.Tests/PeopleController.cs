using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc;

namespace I18Next.Net.DataAnnotations.Tests;

[ApiController]
[Route("people")]
public class PeopleController : ControllerBase
{
    [HttpPost]
    public IActionResult Create(PersonModel person)
    {
        return Ok(person.Name);
    }

    [HttpGet("age")]
    public IActionResult Age([FromQuery] int age)
    {
        return Ok(age);
    }

    [HttpGet("count")]
    public IActionResult Count([FromQuery] [Range(1, 10)] int count)
    {
        return Ok(count);
    }
}

public class PersonModel
{
    [Required]
    [StringLength(20, MinimumLength = 3)]
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

    [RegularExpression("^[0-9]+$", ErrorMessage = "codeDigits")]
    public string Code { get; set; }

    public AddressModel Address { get; set; }
}

public record AddressModel([Required] string Street, [property: Display(Name = "fields.city")] [Required] string City);
