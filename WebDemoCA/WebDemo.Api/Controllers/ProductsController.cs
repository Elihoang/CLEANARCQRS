using MediatR;
using Microsoft.AspNetCore.Mvc;
using WebDemo.Application.Features.Products.Commands.CreateProduct;
using WebDemo.Application.Features.Products.Queries;

namespace WebDemo.Web.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    //  CREATE
    [HttpPost]
    public async Task<IActionResult> Create(CreateProductCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    //  GET
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var result = await _mediator.Send(new GetProductsQuery());
        return Ok(result);
    }
}
