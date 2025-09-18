// -----------------------------------------------------------------------------
// UserDTO.cs
// Author: Adyasha Mallick
// Description:
//   Data Transfer Object for systemuser entity from Dataverse.
//   Maps relevant fields for synchronization or business use.
// -----------------------------------------------------------------------------

using System;

namespace fs.Shared.DTO
{
    public class UserDTO
    {
        // ---------------------------
        // D365 Model Fields
        // ---------------------------
        public Guid Id { get; set; }
        public int Status { get; set; }          // 0 = Active, 1 = Disabled
        public int IsSynced { get; set; }        // Sync status
        public int? IsDeleted { get; set; }      // Null if not deleted
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        // ---------------------------
        // Business Fields
        // ---------------------------
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? JobTitle { get; set; }
        public string? Mobile { get; set; }

        // ---------------------------
        // Optional / Custom Fields
        // ---------------------------
        public string? Gender { get; set; }
        public DateTime? JoiningDate { get; set; }
        public DateTime? TerminationDate { get; set; }
        public string? Nationality { get; set; }
        public DateTime? DateOfBirth { get; set; }
    }
}
