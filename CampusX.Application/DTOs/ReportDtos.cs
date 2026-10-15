using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CampusX.Domain.Enums;

namespace CampusX.Application.DTOs;

public record CreateReportDto(
    string Reason,
    Guid? TargetProductId = null,
    Guid? TargetUserId = null
);

public record ReportResponseDto(
    Guid Id,
    string Reason,
    ReportStatus Status,
    DateTime CreatedAt,
    Guid ReporterId,
    Guid? TargetProductId,
    Guid? TargetUserId
);
