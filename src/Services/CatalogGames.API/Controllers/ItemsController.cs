using Catalog.API.Data;
using Catalog.API.Model;
using Catalog.API.Model.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Catalog.API.Integration.ItemEvents;


[Route("api/items")]
[ApiController]
public class CatalogItemsController : ControllerBase
{
    private readonly CatalogContext _context;
    private readonly IEventsPublisher _eventsPublisher;

    public CatalogItemsController(CatalogContext context, IEventsPublisher eventsPublisher)
    {
        _context = context;
        _eventsPublisher = eventsPublisher;
    }

    // GET: api/CatalogItem
    [HttpGet]
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedItemsDTO<CatalogItemDTO>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> GetCatalogItems([FromQuery] int pageSize = 10, [FromQuery] int pageIndex = 0)
    {
        var itemsOnPage = await _context.CatalogItems
        //join table CatalogBrand and CatalogType
        .Include(item => item.CatalogBrand).Include(item => item.CatalogType)
        .Select(item => new CatalogItemDTO()
        {
            Name = item.Name,
            Description = item.Description,
            Price = item.Price,
            CatalogType = item.CatalogType.Type,
            CatalogBrand = item.CatalogBrand.Brand,
            AvailableStock = item.AvailableStock,
            RestockThreshold = item.RestockThreshold,
            MaxStockThreshold = item.MaxStockThreshold,
            OnReorder = item.OnReorder
        }).OrderBy(c => c.Name)
        .Skip(pageSize * pageIndex)
        .Take(pageSize)
        .ToListAsync();
        var totalItems = await _context.CatalogItems.LongCountAsync();
        var model = new PaginatedItemsDTO<CatalogItemDTO>(pageIndex, pageSize, totalItems, itemsOnPage);
        return Ok(model);
    }

    // GET: api/CatalogItem/5
    [HttpGet("{id}")]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(CatalogItemDTO), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<CatalogItemDTO>> GetCatalogItem(int id)
    {
        if (id <= 0)
            return BadRequest();
        var catalogItem = await _context.CatalogItems
            //join table CatalogBrand and CatalogType
            .Include(item => item.CatalogBrand).Include(item => item.CatalogType)
            .FirstOrDefaultAsync<CatalogItem>(item => item.Id == id);
        if (catalogItem == null)
            return NotFound();
        var catalogItemDTO = new CatalogItemDTO()
        {
            Name = catalogItem.Name,
            Description = catalogItem.Description,
            Price = catalogItem.Price,
            CatalogType = catalogItem.CatalogType.Type,
            CatalogBrand = catalogItem.CatalogBrand.Brand,
            AvailableStock = catalogItem.AvailableStock,
            RestockThreshold = catalogItem.RestockThreshold,
            MaxStockThreshold = catalogItem.MaxStockThreshold,
            OnReorder = catalogItem.OnReorder
        };
        return catalogItemDTO;
    }

    //PUT: api/Items/5
    //To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CatalogItemDTO), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<CatalogItemDTO>> PutCatalogItem(int id, CatalogItemDTO itemDTO)
    {
        bool priceChanged = false;

        if (id <= 0 || string.IsNullOrWhiteSpace(itemDTO.Name))
            return BadRequest("El ID y el nombre deben ser válidos.");

        CatalogItem? item = await _context.CatalogItems
            .Include(i => i.CatalogBrand)
            .Include(i => i.CatalogType)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (item == null)
            return NotFound($"No existe ningún producto con ID {id}.");

        bool nameAlreadyExists = await _context.CatalogItems
            .AnyAsync(i => i.Name == itemDTO.Name && i.Id != id);

        if (nameAlreadyExists)
            return BadRequest("Ya existe otro producto con ese nombre.");
        if (item.Price != itemDTO.Price) priceChanged = true;
        if (item.CatalogType.Type != itemDTO.CatalogType)
        {
            var type = _context.CatalogTypes.FirstOrDefault(t => t.Type == itemDTO.CatalogType);
            if (type == null) item.CatalogType = new CatalogType { Type = itemDTO.CatalogType };
            else item.CatalogType = type;
        }
        if (item.CatalogBrand.Brand != itemDTO.CatalogBrand)
        {
            var brand = _context.CatalogBrands.FirstOrDefault(t => t.Brand == itemDTO.CatalogBrand);
            if (brand == null) item.CatalogBrand = new CatalogBrand { Brand = itemDTO.CatalogBrand };
            else item.CatalogBrand = brand;
        }
        //it is recommended to use automapper to avoid this coding
        item.MaxStockThreshold = itemDTO.MaxStockThreshold;
        item.Name = itemDTO.Name;
        item.OnReorder = itemDTO.OnReorder;
        item.Price = itemDTO.Price;
        item.RestockThreshold = itemDTO.RestockThreshold;
        item.AvailableStock = itemDTO.AvailableStock;
        item.Description = itemDTO.Description;
        _context.Entry(item).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync();
            if (priceChanged)
            {
                var itemPriceChangedEvent = new ItemPriceChangedEvent
                {
                    Name = item.Name,
                    Price = item.Price
                };
                bool published = await _eventsPublisher.Publish(itemPriceChangedEvent);

            }
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }

        return Ok(itemDTO);
    }



    // POST: api/CatalogItem
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<CatalogItemDTO>> PostCatalogItem(CatalogItemDTO catalogItemDTO)
    {
        CatalogItem catalogItem = await _context.CatalogItems.FirstOrDefaultAsync(ci => ci.Name == catalogItemDTO.Name);
        if (catalogItem != null) return BadRequest("Product Name must be unique");
        catalogItem = new CatalogItem()
        {
            Name = catalogItemDTO.Name,
            Description = catalogItemDTO.Description,
            Price = catalogItemDTO.Price,
            AvailableStock = catalogItemDTO.AvailableStock,
            RestockThreshold = catalogItemDTO.RestockThreshold,
            MaxStockThreshold = catalogItemDTO.MaxStockThreshold,
            OnReorder = catalogItemDTO.OnReorder
        };
        CatalogType catalogtype = await _context.CatalogTypes.FirstOrDefaultAsync(ct => ct.Type == catalogItemDTO.CatalogType);
        if (catalogtype == null) catalogtype = new CatalogType() { Type = catalogItemDTO.CatalogType };
        CatalogBrand catalogBrand = await _context.CatalogBrands.FirstOrDefaultAsync(cb => cb.Brand == catalogItemDTO.CatalogBrand);
        if (catalogBrand == null) catalogBrand = new CatalogBrand() { Brand = catalogItemDTO.CatalogBrand };
        catalogItem.CatalogBrand = catalogBrand;
        catalogItem.CatalogType = catalogtype;
        _context.CatalogItems.Add(catalogItem);
        await _context.SaveChangesAsync();
        return CreatedAtAction("GetCatalogItem", new { id = catalogItem.Id }, catalogItemDTO);
    }

    // DELETE: api/CatalogItem/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCatalogItem(int id)
    {
        var catalogItem = await _context.CatalogItems.FindAsync(id);
        if (catalogItem == null)
        {
            return NotFound();
        }

        _context.CatalogItems.Remove(catalogItem);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool CatalogItemExists(int? id)
    {
        return _context.CatalogItems.Any(e => e.Id == id);
    }
}