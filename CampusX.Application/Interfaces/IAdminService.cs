using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CampusX.Application.DTOs;
using CampusX.Domain.Enums;

namespace CampusX.Application.Interfaces;

public interface IAdminService
{
    Task ReviewReportAsync(Guid reportId, ReportStatus decision);
}
