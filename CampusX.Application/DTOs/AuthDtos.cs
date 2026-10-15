using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CampusX.Application.DTOs;

public record RegisterStudentDto(
    string FullName,
    string Email,
    string Password,
    string CollegeName,
    string? PhoneNumber
);

public record LoginDto(
    string Email,
    string Password
);

public record AuthResponseDto(
    bool Success,
    string Message,
    Guid? UserId = null,
    string? FullName = null,
    string? Role = null
);
