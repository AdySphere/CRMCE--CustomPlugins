// TeamUserDTO.cs
using System;

namespace fs.Shared.DTO
{
    public class TeamUserDTO
    {
        public Guid UserId { get; set; }
        public Guid TeamId { get; set; }
        public string Action { get; set; } // "Associate" or "Disassociate"
        public DateTime ActionDate { get; set; } = DateTime.UtcNow;
    }
}
