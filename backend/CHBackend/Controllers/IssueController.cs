using CHBackend.Models;
using CHBackend.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CHBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class IssueController : ControllerBase
    {
        private readonly AppDbContext _context;

        public IssueController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] IssueFilterDto filter)
        {
            var query = _context.Issues
                .Include(i => i.Contractor)
                .AsQueryable(); 

            // 1. Filtr: ID
            if (filter.Id.HasValue)
            {
                query = query.Where(i => i.Id == filter.Id.Value);
            }

            // 2. Filtr: Tytuł (wyszukiwanie po części ciągu)
            if (!string.IsNullOrWhiteSpace(filter.Title))
            {
                query = query.Where(i => i.Title.ToLower().Contains(filter.Title.ToLower()));
            }

            // 3. Filtr: Status (dokładne dopasowanie)
            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                query = query.Where(i => i.Status == filter.Status);
            }

            // 4. Filtr: Wykonawca (ID)
            if (filter.ContractorId.HasValue)
            {
                query = query.Where(i => i.ContractorId == filter.ContractorId.Value);
            }

            // 5. Sortowanie
            switch (filter.SortBy?.ToLower())
            {
                case "status":
                    query = query.OrderBy(i => i.Status);
                    break;
                case "contractor":
                    query = query.OrderBy(i => i.Contractor!.CompanyName);
                    break;
                case "title":
                default:
                    query = query.OrderBy(i => i.Title);
                    break;
            }

            // 6. Projekcja do DTO i wykonanie zapytania
            var result = await query
                .Select(i => new IssueListDto // Używamy nowego DTO do listy
                {
                    Id = i.Id,
                    Title = i.Title,
                    Description = i.Description,
                    Location = i.Location,
                    Status = i.Status,
                    ContractorId = i.ContractorId,
                    ContractorName = i.Contractor != null ? i.Contractor.CompanyName : null
                })
                .ToListAsync();

            return Ok(result);
        }
        // GET: api/Issue/all
        //[HttpGet("all")]
        //public async Task<IActionResult> GetAllIssues()
        //{
        //    var issues = await _context.Issues
        //        .Include(i => i.Contractor)
        //        .Include(i => i.Photos)
        //        .ToListAsync();

        //    return Ok(issues);
        //}

        // GET: api/Issue/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetIssue(int id)
        {
            var issue = await _context.Issues
                .Include(i => i.Contractor)
                .Include(i => i.Photos)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (issue == null)
                return NotFound();

            return Ok(issue);
        }

        // GET: api/Issue/search?title=...
        //[HttpGet("search")]
        //public async Task<IActionResult> GetByTitle([FromQuery] string title)
        //{
        //    if (string.IsNullOrWhiteSpace(title))
        //        return BadRequest("Title parameter is required.");

        //    var issues = await _context.Issues
        //        .Include(i => i.Contractor)
        //        .Include(i => i.Photos)
        //        .Where(i => i.Title.ToLower().Contains(title.ToLower()))
        //        .ToListAsync();

        //    if (issues.Count == 0)
        //        return NotFound("No issues found with the given title.");

        //    return Ok(issues);
        //}

        // ✅ POST: api/Issue (z obsługą zdjęcia)
        [HttpPost]
        [RequestSizeLimit(15_000_000)] // do 15 MB
        public async Task<IActionResult> AddIssue([FromForm] IssueDto issueDto, IFormFile? photo)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!issueDto.ContractorId.HasValue)
            {
                return BadRequest("Wykonawca jest wymagany.");
            }

            var contractorExists = await _context.Contractors
                .AnyAsync(c => c.Id == issueDto.ContractorId.Value);

            if (!contractorExists)
            {
                return BadRequest("Podany wykonawca nie istnieje.");
            }

            var newIssue = new Issue
            {
                Title = issueDto.Title,
                Description = issueDto.Description,
                Location = issueDto.Location,
                Status = issueDto.Status ?? "Nowa",
                ContractorId = issueDto.ContractorId
            };

            _context.Issues.Add(newIssue);
            await _context.SaveChangesAsync();

            // 📸 Jeśli przesłano zdjęcie, zapisz je
            if (photo != null && photo.Length > 0)
            {
                var directoryPath = Path.Combine("wwwroot", "photos");
                if (!Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);

                var fileName = $"{Guid.NewGuid()}_{photo.FileName}";
                var filePath = Path.Combine(directoryPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await photo.CopyToAsync(stream);
                }

                var newPhoto = new Photo
                {
                    Url = $"/photos/{fileName}",
                    IssueId = newIssue.Id
                };

                newIssue.Photos ??= new List<Photo>();
                newIssue.Photos.Add(newPhoto);
                _context.Photos.Add(newPhoto);
                await _context.SaveChangesAsync();
            }

            var createdIssue = await _context.Issues
                .Include(i => i.Contractor)
                .Include(i => i.Photos)
                .FirstOrDefaultAsync(i => i.Id == newIssue.Id);

            return CreatedAtAction(nameof(GetIssue), new { id = newIssue.Id }, createdIssue);
        }

        // PUT: api/Issue/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateIssue(int id, [FromBody] IssueDto issueDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingData = await _context.Issues.FindAsync(id);
            if (existingData == null)
                return NotFound();

            if (!issueDto.ContractorId.HasValue)
            {
                return BadRequest("Wykonawca jest wymagany.");
            }

            var contractorExists = await _context.Contractors
                .AnyAsync(c => c.Id == issueDto.ContractorId.Value);

            if (!contractorExists)
            {
                return BadRequest("Podany wykonawca nie istnieje.");
            }


            existingData.Title = issueDto.Title;
            existingData.Description = issueDto.Description;
            existingData.Location = issueDto.Location;
            existingData.Status = issueDto.Status;
            existingData.ContractorId = issueDto.ContractorId;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PUT: api/Issue/5/assign
        //[HttpPut("{id:int}/assign")]
        //public async Task<IActionResult> AssignContractor(int id, [FromBody] int contractorId)
        //{
        //    var issue = await _context.Issues.FindAsync(id);
        //    if (issue == null)
        //        return NotFound("Usterka nie została znaleziona.");

        //    var contractor = await _context.Contractors.FindAsync(contractorId);
        //    if (contractor == null)
        //        return NotFound("Wykonawca nie został znaleziony.");

        //    issue.ContractorId = contractorId;
        //    await _context.SaveChangesAsync();

        //    return NoContent();
        //}

        // DELETE: api/Issue/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteIssue(int id)
        {
            var issue = await _context.Issues.FindAsync(id);
            if (issue == null)
                return NotFound();

            _context.Issues.Remove(issue);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/Issue/5/photos
        [HttpPost("{id:int}/photos")]
        public async Task<IActionResult> AddPhotoToIssue(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var issue = await _context.Issues
                .Include(i => i.Photos)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (issue == null)
                return NotFound("Issue not found.");

            var directoryPath = Path.Combine("wwwroot", "photos");
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(directoryPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var photo = new Photo
            {
                Url = $"/photos/{fileName}",
                IssueId = id
            };

            issue.Photos ??= new List<Photo>();
            issue.Photos.Add(photo);
            _context.Photos.Add(photo);
            await _context.SaveChangesAsync();

            return Ok(photo);
        }
    }
}
