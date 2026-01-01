using CHBackend.Models;
using CHBackend.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Runtime.ConstrainedExecution;

namespace CHBackend.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ContractController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ContractController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("search")]
        [Authorize(Policy = "CanReadContracts")]
        public async Task<IActionResult> Search([FromQuery] ContractFilterDto contractFilterDto)
        {
            var query = _context.Contracts.Include(i => i.Contractor).AsQueryable();

            if (User.IsInRole("Contractor"))
            {
                var contractorIdClaim = User.FindFirst("ContractorId");

                if (contractorIdClaim != null && int.TryParse(contractorIdClaim.Value, out int cid))
                {
                    query = query.Where(c => c.ContractorId == cid);
                }
                else
                {
                    // Jest w roli Contractor, ale nie ma ID w tokenie -> Pusta lista (bezpieczeństwo)
                    return Ok(new List<ContractListDto>());
                }
            }

            if (!string.IsNullOrEmpty(contractFilterDto.ContractNumber))
            {
                query = query.Where(c => c.ContractNumber.Contains(contractFilterDto.ContractNumber));
            }
            if (!string.IsNullOrEmpty(contractFilterDto.Name))
            {
                query = query.Where(c => c.Name.Contains(contractFilterDto.Name));
            }
            if (contractFilterDto.ContractorId.HasValue)
            {
                query = query.Where(c => c.ContractorId == contractFilterDto.ContractorId.Value);
            }
            if (!string.IsNullOrEmpty(contractFilterDto.Status))
            {
                query = query.Where(c => c.Status == contractFilterDto.Status);
            }

            switch (contractFilterDto.SortBy)
            {
                case "Name":
                    query = query.OrderBy(c => c.Name);
                    break;
                case "NameDesc":
                    query = query.OrderByDescending(c => c.Name);
                    break;
                case "StartDate":
                    query = query.OrderBy(c => c.StartDate);
                    break;
                case "EndDate":
                    query = query.OrderBy(c => c.EndDate);
                    break;
                case "StartDateDesc":
                    query = query.OrderByDescending(c => c.StartDate);
                    break;
                case "EndDateDesc":
                    query = query.OrderByDescending(c => c.EndDate);
                    break;
                default:
                    query = query.OrderBy(c => c.Id);
                    break;
            }

            var results = await query.Select(c => new ContractListDto
                {
                Id = c.Id,
                ContractNumber = c.ContractNumber,
                Name = c.Name,
                Description = c.Description,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Status = c.Status,
                ContractorId = c.ContractorId,
                ContractorName = c.Contractor.CompanyName
            }).ToListAsync();
            return Ok(results);
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = "CanReadContracts")]
        public async Task<IActionResult> GetContractById(int id)
        {
            var query = _context.Contracts.Include(i => i.Contractor).AsQueryable();

            if (User.IsInRole("Contractor"))
            {
                var contractorIdClaim = User.FindFirst("ContractorId");
                if (contractorIdClaim != null && int.TryParse(contractorIdClaim.Value, out int cid))
                {
                    query = query.Where(c => c.ContractorId == cid);
                }
                else return NotFound(); // Brak ID w tokenie
            }

            query = query.Where(c => c.Id == id);

            var result = await query.Select(c => new ContractDetailsDto
            {
                Id = c.Id,
                ContractNumber = c.ContractNumber,
                Name = c.Name,
                Description = c.Description,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Status = c.Status,
                ContractorId = c.ContractorId,
                Contractor = c.Contractor
            }).FirstOrDefaultAsync();

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }


        [HttpPost]
        [Authorize(Policy = "CanUpdateCreateContracts")]
        public async Task<IActionResult> AddContract([FromForm] ContractDto contractDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var contractorExists = await _context.Contractors
                .AnyAsync(c => c.Id == contractDto.ContractorId);

            if (!contractorExists)
            {
                return BadRequest("Podany wykonawca nie istnieje.");
            }

            var newContract = new Contract
            {
                ContractNumber = contractDto.ContractNumber,
                Name = contractDto.Name,
                Description = contractDto.Description,
                StartDate = contractDto.StartDate,
                EndDate = contractDto.EndDate,
                ContractorId = contractDto.ContractorId,
                Status = contractDto.Status
            };

            _context.Contracts.Add(newContract);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetContractById), new { id = newContract.Id }, newContract);
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = "CanUpdateCreateContracts")]
        public async Task<IActionResult> UpdateContract(int id, [FromForm] ContractDto contractDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingContract = await _context.Contracts.FindAsync(id);
            if (existingContract == null)
            {
                return NotFound();
            }

            var contractorExists = await _context.Contractors
                .AnyAsync(c => c.Id == contractDto.ContractorId);
            if (!contractorExists)
            {
                return BadRequest("Podany wykonawca nie istnieje.");
            }

            existingContract.ContractNumber = contractDto.ContractNumber;
            existingContract.Name = contractDto.Name;
            existingContract.Description = contractDto.Description;
            existingContract.StartDate = contractDto.StartDate;
            existingContract.EndDate = contractDto.EndDate;
            existingContract.ContractorId = contractDto.ContractorId;
            existingContract.Status = contractDto.Status;

            //_context.Contracts.Update(existingContract);

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = "CanDeleteContracts")]
        public async Task<IActionResult> DeleteContract(int id)
        {
            var existingContract = await _context.Contracts.FindAsync(id);
            if (existingContract == null)
            {
                return NotFound();
            }
            _context.Contracts.Remove(existingContract);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
