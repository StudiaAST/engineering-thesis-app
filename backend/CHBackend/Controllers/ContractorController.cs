using CHBackend.Models;
using CHBackend.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize]
[ApiController]
public class ContractorController : Controller
{
    private readonly AppDbContext _context;

    public ContractorController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("api/Contractor/all")]
    public async Task<IActionResult> GetContractors()
    {
        var contractors = await _context.Contractors.ToListAsync();
        return Ok(contractors);
    }

    [HttpGet("api/contractor/all-simple")]
    [Authorize(Policy = "CanReadContracts")]
    public async Task<IActionResult> GetAllSimple()
    {
        // Zwracamy tylko ID i Nazwę, żeby nie przesyłać zbędnych danych
        var list = await _context.Contractors
            .Select(c => new
            {
                Id = c.Id,
                CompanyName = c.CompanyName
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("api/Contractor/id")]
    public async Task<IActionResult> GetById(int id)
    {
        var contractor = await _context.Contractors.FirstOrDefaultAsync(c => c.Id == id);

        if (contractor == null)
        {
            return NotFound();
        }

        return Ok(contractor);
    }

    [HttpGet("api/Contractor/by-name")]
    public async Task<IActionResult> GetByName([FromQuery] string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return BadRequest("Name parameter is required.");
        }

        var contractors = await _context.Contractors
            .Include(c => c.Issues)
            .Where(c => c.CompanyName.ToLower().Contains(name.ToLower())) // Zmiana na Contains
            .ToListAsync();

        if (contractors == null || !contractors.Any())
        {
            return NotFound("No contractors found with the given name.");
        }

        return Ok(contractors);
    }

    //[Authorize(Roles = "Admin, Manager")]
    [HttpPost("api/Contractor")]
    public async Task<IActionResult> Create([FromBody] ContractorDto contractorDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        string normalizedName = contractorDto.CompanyName.Trim().ToLower();

        bool exists = await _context.Contractors.AnyAsync(c =>
            c.CompanyName.ToLower() == normalizedName
        );

        if (exists)
        {
            return Conflict($"Wykonawca o nazwie '{contractorDto.CompanyName}' już istnieje w bazie. Sprawdź listę (również nieaktywnych).");
        }

        var newContractor = new Contractor
        {
            CompanyName = contractorDto.CompanyName,
            ContactInfo = contractorDto.ContactInfo,
            Status = contractorDto.Status
        };

        _context.Contractors.Add(newContractor);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = newContractor.Id }, newContractor);
    }

    //[Authorize(Roles = "Admin, Manager")]
    [HttpPut("api/Contractor")]
    public async Task<IActionResult> Update(int id, [FromBody] ContractorDto contractorDto)
    {
        var dataExists = await _context.Contractors.FindAsync(id);
        if (dataExists == null)
            return NotFound();

        string normalizedName = contractorDto.CompanyName.Trim().ToLower();

        bool duplicateExists = await _context.Contractors.AnyAsync(c =>
            c.Id != id &&
            c.CompanyName.ToLower() == normalizedName
        );

        if (duplicateExists)
        {
            return Conflict($"Inny wykonawca o nazwie '{contractorDto.CompanyName}' już istnieje.");
        }

        dataExists.CompanyName = contractorDto.CompanyName;
        dataExists.ContactInfo = contractorDto.ContactInfo;
        dataExists.Status = contractorDto.Status;

        await _context.SaveChangesAsync();
        return NoContent();
    }


    //[Authorize(Roles = "Admin, Manager")]
    [HttpDelete("api/Contractor")]
    public async Task<IActionResult> Delete(int id)
    {
        var contractor = await _context.Contractors.FindAsync(id);
        if (contractor == null)
            return NotFound();

        _context.Contractors.Remove(contractor);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("api/Contractor/search")]
    public async Task<IActionResult> Search([FromQuery] ContractorFilterDto filter)
    {
        var query = _context.Contractors
            .Include(c => c.Issues)
            .AsQueryable();

        // 2. Nakładamy filtry dynamicznie (tylko jeśli user coś wpisał)

        // Filtr po ID
        if (filter.Id.HasValue)
        {
            query = query.Where(c => c.Id == filter.Id.Value);
        }

        // Filtr po Nazwie (case-insensitive)
        if (!string.IsNullOrWhiteSpace(filter.Name))
        {
            query = query.Where(c => c.CompanyName.ToLower().Contains(filter.Name.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            query = query.Where(c => c.Status == filter.Status);
        }

        if (filter.HasIssues.HasValue)
        {
            if (filter.HasIssues.Value == true)
            {
                // Tylko z usterkami
                query = query.Where(c => c.Issues.Any());
            }
            else
            {
                // Tylko BEZ usterek
                query = query.Where(c => !c.Issues.Any());
            }
        }

        // 3. Sortowanie (Server-side sorting)
        switch (filter.SortBy?.ToLower())
        {
            case "status":
                query = query.OrderBy(c => c.Status);
                break;
            case "name":
            default:
                query = query.OrderBy(c => c.CompanyName);
                break;
        }

        // 4. Projekcja do DTO (żeby nie zwracać całych encji z cyklami)
        var result = await query
            .Select(c => new ContractorDto
            {
                Id = c.Id,
                CompanyName = c.CompanyName,
                ContactInfo = c.ContactInfo,
                Status = c.Status
                // Opcjonalnie możesz dodać ilość usterek, np: IssuesCount = c.Issues.Count
            })
            .ToListAsync();

        return Ok(result);
    }
}

