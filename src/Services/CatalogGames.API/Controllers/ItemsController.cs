using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Catalog.API.Model;
using Catalog.API.Data;

[Route("api/[controller]")]
[ApiController]
public class ItemsController : ControllerBase
{
    private readonly CatalogContext _context;
    public ItemsController(CatalogContext context)
    {
        _context = context;
    }

    // GET: api/CatalogItem
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CatalogItem>>> GetCatalogItem()
    {
        return await _context.CatalogItems.ToListAsync();
    }

    // GET: api/CatalogItem/5
    [HttpGet("{id}")]
    public async Task<ActionResult<CatalogItem>> GetCatalogItem(int id)
    {
        var catalogitem = await _context.CatalogItems.FindAsync(id);

        if (catalogitem == null)
        {
            return NotFound();
        }

        return catalogitem;
    }

    // PUT: api/CatalogItem/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutCatalogItem(int? id, CatalogItem catalogitem)
    {
        if (id != catalogitem.Id)
        {
            return BadRequest();
        }

        _context.Entry(catalogitem).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!CatalogItemExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // POST: api/CatalogItem
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<CatalogItem>> PostCatalogItem(CatalogItem catalogitem)
    {
        _context.CatalogItems.Add(catalogitem);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetCatalogItem", new { id = catalogitem.Id }, catalogitem);
    }

    // DELETE: api/CatalogItem/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCatalogItem(int? id)
    {
        var catalogitem = await _context.CatalogItems.FindAsync(id);
        if (catalogitem == null)
        {
            return NotFound();
        }

        _context.CatalogItems.Remove(catalogitem);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool CatalogItemExists(int? id)
    {
        return _context.CatalogItems.Any(e => e.Id == id);
    }
}
