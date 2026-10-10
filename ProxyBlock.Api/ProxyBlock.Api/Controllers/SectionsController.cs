using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProxyBlock.Api.Data;
using ProxyBlock.Api.DTOs;
using ProxyBlock.Api.Models;
using System.Security.Claims;

namespace ProxyBlock.Api.Controllers;

public class UploadRosterForm
{
    [FromForm(Name = "sectionName")]
    public string SectionName { get; set; } = "";
    [FromForm(Name = "courseName")]
    public string CourseName { get; set; } = "";
    [FromForm(Name = "courseCode")]
    public string CourseCode { get; set; } = "";
    [FromForm(Name = "file")]
    public IFormFile File { get; set; } = null!;
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SectionsController : ControllerBase
{
    private readonly AppDbContext _db;
    public SectionsController(AppDbContext db) => _db = db;

    private int CurrentUserId =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    // Teacher: create an empty section (students come via roster upload or add-by-roll)
    [HttpPost]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> Create(CreateSectionRequest req)
    {
        var code = req.CourseCode.Trim().ToUpper();
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Code == code);
        if (course == null)
        {
            course = new Course { Name = req.CourseName.Trim(), Code = code };
            _db.Courses.Add(course);
            await _db.SaveChangesAsync();
        }

        var section = new CourseSection
        {
            CourseId = course.Id,
            SectionName = req.SectionName.Trim().ToUpper(),
            TeacherId = CurrentUserId
        };
        _db.Sections.Add(section);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            section.Id,
            CourseName = course.Name,
            CourseCode = course.Code,
            section.SectionName
        });
    }

    // Teacher: upload class list (.xlsx) -> creates section, enrolls roll numbers.
    // Column A = roll numbers, row 1 = header, data from row 2.
    [HttpPost("upload-roster")]
    [Authorize(Roles = "Teacher")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> UploadRoster([FromForm] UploadRosterForm form)
    {
        var sectionName = form.SectionName;
        var courseName = form.CourseName;
        var courseCode = form.CourseCode;
        var file = form.File;

        if (string.IsNullOrWhiteSpace(sectionName) ||
            string.IsNullOrWhiteSpace(courseName) ||
            string.IsNullOrWhiteSpace(courseCode))
            return BadRequest("Section name, course name and course code are required.");

        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        if (!string.Equals(Path.GetExtension(file.FileName), ".xlsx",
                StringComparison.OrdinalIgnoreCase))
            return BadRequest("Please upload an .xlsx Excel file.");

        var code = courseCode.Trim().ToUpper();
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Code == code);
        if (course == null)
        {
            course = new Course { Name = courseName.Trim(), Code = code };
            _db.Courses.Add(course);
            await _db.SaveChangesAsync();
        }

        var section = new CourseSection
        {
            CourseId = course.Id,
            SectionName = sectionName.Trim().ToUpper(),
            TeacherId = CurrentUserId
        };
        _db.Sections.Add(section);
        await _db.SaveChangesAsync();

        var rolls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using (var stream = file.OpenReadStream())
            using (var wb = new XLWorkbook(stream))
            {
                var ws = wb.Worksheets.First();
                foreach (var row in ws.RowsUsed().Skip(1))
                {
                    var val = row.Cell(1).GetString().Trim().ToUpperInvariant();
                    if (!string.IsNullOrWhiteSpace(val))
                        rolls.Add(val);
                }
            }
        }
        catch
        {
            return BadRequest("Could not read the Excel file. Make sure it is a valid .xlsx file.");
        }

        if (rolls.Count == 0)
            return BadRequest("No roll numbers found in column A of the file.");

        int enrolled = 0, pending = 0;
        foreach (var roll in rolls)
        {
            var student = await _db.Users.FirstOrDefaultAsync(u =>
                u.Role == UserRole.Student && u.RollNumber.ToUpper() == roll);
            if (student == null)
            {
                if (!await _db.PendingEnrollments.AnyAsync(p =>
                        p.CourseSectionId == section.Id && p.RollNumber == roll))
                {
                    _db.PendingEnrollments.Add(new PendingEnrollment
                    {
                        CourseSectionId = section.Id,
                        RollNumber = roll
                    });
                    pending++;
                }
            }
            else if (!await _db.Enrollments.AnyAsync(e =>
                         e.CourseSectionId == section.Id && e.StudentId == student.Id))
            {
                _db.Enrollments.Add(new Enrollment
                {
                    CourseSectionId = section.Id,
                    StudentId = student.Id
                });
                enrolled++;
            }
        }
        await _db.SaveChangesAsync();

        return Ok(new
        {
            section.Id,
            CourseName = course.Name,
            CourseCode = course.Code,
            SectionName = section.SectionName,
            TotalRolls = rolls.Count,
            Enrolled = enrolled,
            Pending = pending
        });
    }

    // Teacher: my sections with student counts
    [HttpGet("mine")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> Mine()
    {
        var sections = await _db.Sections
            .Where(s => s.TeacherId == CurrentUserId)
            .Include(s => s.Course)
            .Select(s => new
            {
                s.Id,
                CourseName = s.Course.Name,
                CourseCode = s.Course.Code,
                s.SectionName,
                StudentCount = s.Enrollments.Count
            })
            .ToListAsync();
        return Ok(sections);
    }

    // Student: my enrolled sections.
    // Also links any pending roster spots waiting for this student's roll number.
    [HttpGet("enrolled")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Enrolled()
    {
        var me = await _db.Users.FindAsync(CurrentUserId);
        if (me != null && !string.IsNullOrWhiteSpace(me.RollNumber))
        {
            var myRoll = me.RollNumber.Trim().ToUpperInvariant();
            var pendings = await _db.PendingEnrollments
                .Where(p => p.RollNumber == myRoll)
                .ToListAsync();
            foreach (var p in pendings)
            {
                if (!await _db.Enrollments.AnyAsync(e =>
                        e.CourseSectionId == p.CourseSectionId && e.StudentId == me.Id))
                {
                    _db.Enrollments.Add(new Enrollment
                    {
                        CourseSectionId = p.CourseSectionId,
                        StudentId = me.Id
                    });
                }
                _db.PendingEnrollments.Remove(p);
            }
            if (pendings.Count > 0)
                await _db.SaveChangesAsync();
        }

        var sections = await _db.Enrollments
            .Where(e => e.StudentId == CurrentUserId)
            .Select(e => new
            {
                e.CourseSection.Id,
                CourseName = e.CourseSection.Course.Name,
                CourseCode = e.CourseSection.Course.Code,
                e.CourseSection.SectionName,
                TeacherName = e.CourseSection.Teacher.FullName
            })
            .ToListAsync();
        return Ok(sections);
    }

    // Teacher: add a student by roll number
    [HttpPost("{id}/students")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> AddStudent(int id, AddStudentRequest req)
    {
        var section = await _db.Sections
            .FirstOrDefaultAsync(s => s.Id == id && s.TeacherId == CurrentUserId);
        if (section == null) return NotFound("Section not found.");

        var roll = req.RollNumber.Trim().ToUpper();
        var student = await _db.Users.FirstOrDefaultAsync(u =>
            u.Role == UserRole.Student && u.RollNumber.ToUpper() == roll);
        if (student == null) return BadRequest("No student found with that roll number.");

        if (await _db.Enrollments.AnyAsync(e =>
                e.CourseSectionId == id && e.StudentId == student.Id))
            return BadRequest("Student is already enrolled.");

        _db.Enrollments.Add(new Enrollment { CourseSectionId = id, StudentId = student.Id });
        await _db.SaveChangesAsync();
        return Ok(new { student.Id, student.FullName, student.RollNumber });
    }

    // Teacher: live roster with present/absent for a session
    [HttpGet("{id}/roster")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> Roster(int id, [FromQuery] int sessionId)
    {
        var section = await _db.Sections
            .Include(s => s.Enrollments)
                .ThenInclude(e => e.Student)
            .FirstOrDefaultAsync(s => s.Id == id && s.TeacherId == CurrentUserId);
        if (section == null) return NotFound("Section not found.");

        var presentIds = await _db.Records
            .Where(r => r.SessionId == sessionId)
            .Select(r => r.StudentId)
            .ToListAsync();

        var roster = section.Enrollments
            .Select(e => new
            {
                e.Student.Id,
                e.Student.FullName,
                e.Student.RollNumber,
                Present = presentIds.Contains(e.Student.Id)
            })
            .OrderBy(r => r.RollNumber)
            .ToList();

        return Ok(new
        {
            Total = roster.Count,
            Present = roster.Count(r => r.Present),
            Roster = roster
        });
    }
}
