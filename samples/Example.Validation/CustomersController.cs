using Microsoft.AspNetCore.Mvc;

namespace Example.Validation;

[ApiController]
[Route("mvc/customers")]
public class CustomersController : ControllerBase
{
    // curl -X POST -H "Accept-Language: de" -H "Content-Type: application/json" -d '{"name":"J","email":"jane"}' http://localhost:5000/mvc/customers
    [HttpPost]
    public IActionResult Create(Customer customer)
    {
        return Ok(customer);
    }

    // curl -H "Accept-Language: de" "http://localhost:5000/mvc/customers?page=abc"
    [HttpGet]
    public IActionResult List([FromQuery] int page)
    {
        return Ok(new { page });
    }
}
