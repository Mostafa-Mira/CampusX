using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CampusX.Application.DTOs;

namespace CampusX.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterStudentDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task LogoutAsync();
}
