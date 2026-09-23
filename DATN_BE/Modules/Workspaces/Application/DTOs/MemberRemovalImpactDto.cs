using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DATN_BE.Modules.Workspaces.Application.DTOs
{
    public class LeaderProjectDto
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
    }
    public class MemberRemovalImpactDto
    {
        public Guid MemberId { get; set; }
        public bool IsProjectLeader { get; set; }
        public int AssignedCardsCount { get; set; }
        public List<LeaderProjectDto> LeaderProjects { get; set; } = new();
    }
}