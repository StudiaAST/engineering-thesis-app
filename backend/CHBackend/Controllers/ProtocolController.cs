using CHBackend.Models;
using CHBackend.Models.DTOs;
using CHBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace CHBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProtocolController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProtocolController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<ProtocolListDto>>> Search([FromQuery] ProtocolFilterDto filter)
        {
            var query = _context.Protocols.AsQueryable();

            // --- FILTROWANIE (To zostaje bez zmian) ---
            if (filter.Id.HasValue)
                query = query.Where(p => p.Id == filter.Id.Value);

            if (!string.IsNullOrWhiteSpace(filter.ProtocolNumber))
                query = query.Where(p => p.ProtocolNumber.ToLower().Contains(filter.ProtocolNumber.ToLower()));

            if (!string.IsNullOrWhiteSpace(filter.Area))
                query = query.Where(p => p.Area.ToLower().Contains(filter.Area.ToLower()));

            if (!string.IsNullOrWhiteSpace(filter.Type))
                query = query.Where(p => p.Type.ToLower().Contains(filter.Type.ToLower()));

            if (!string.IsNullOrWhiteSpace(filter.State))
                query = query.Where(p => p.State == filter.State);

            // --- SORTOWANIE ---
            query = filter.SortBy?.ToLower() switch
            {
                "protocolnumber" => query.OrderBy(p => p.ProtocolNumber),
                "area" => query.OrderBy(p => p.Area),
                "state" => query.OrderBy(p => p.State),
                "date" or _ => query.OrderByDescending(p => p.ReceiptDate),
            };

            // --- PROJEKCJA DO LIST DTO (Kluczowa zmiana) ---
            var result = await query
                .Select(p => new ProtocolListDto
                {
                    Id = p.Id,
                    ReceiptDate = p.ReceiptDate,
                    ProtocolNumber = p.ProtocolNumber,
                    Type = p.Type,
                    Area = p.Area,
                    DocumentNumber = p.DocumentNumber,
                    StatusDescription = p.StatusDescription,
                    FixDate = p.FixDate,
                    State = p.State
                })
                .ToListAsync();

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetProtocol(int id)
        {
            var protocol = await _context.Protocols.FindAsync(id);
            if (protocol == null) return NotFound();
            return Ok(protocol);
        }

        [HttpPost]
        public async Task<IActionResult> AddProtocol([FromBody] ProtocolDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var newProtocol = new Protocol
            {
                ReceiptDate = dto.ReceiptDate,
                ProtocolNumber = dto.ProtocolNumber,
                Type = dto.Type,
                Area = dto.Area,
                DocumentNumber = dto.DocumentNumber,
                StatusDescription = dto.StatusDescription,
                FixDate = dto.FixDate,
                State = dto.State
            };

            _context.Protocols.Add(newProtocol);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProtocol), new { id = newProtocol.Id }, newProtocol);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateProtocol(int id, [FromBody] ProtocolDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existing = await _context.Protocols.FindAsync(id);
            if (existing == null) return NotFound();

            existing.ReceiptDate = dto.ReceiptDate;
            existing.ProtocolNumber = dto.ProtocolNumber;
            existing.Type = dto.Type;
            existing.Area = dto.Area;
            existing.DocumentNumber = dto.DocumentNumber;
            existing.StatusDescription = dto.StatusDescription;
            existing.FixDate = dto.FixDate;
            existing.State = dto.State;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteProtocol(int id)
        {
            var protocol = await _context.Protocols.FindAsync(id);
            if (protocol == null) return NotFound();

            _context.Protocols.Remove(protocol);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("{id:int}/pdf")]
        public async Task<IActionResult> GeneratePdf(int id)
        {
            var protocol = await _context.Protocols.FindAsync(id);
            if (protocol == null) return NotFound();

            var document = new ProtocolDocument(protocol);
            var pdfBytes = document.GeneratePdf();

            var fileName = $"Protokol_{protocol.ProtocolNumber}_{DateTime.Now:yyyyMMdd}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
    }
}