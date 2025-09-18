// -----------------------------------------------------------------------------
// CustomerDTO.cs
// Author: Adyasha Mallick
// Description:
//   Data Transfer Object for account entity from Dataverse.
//   Maps relevant fields for synchronization or business use.
// -----------------------------------------------------------------------------

using System;

namespace fs.Shared.DTO
{
    public class CustomerDTO
    {
        // ---------------------------
        // D365 Model Fields
        // ---------------------------
        public Guid Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        // ---------------------------
        // Business Fields
        // ---------------------------
        public string? Name { get; set; }
        public string? Code { get; set; }           // accountnumber
        public string? Phone { get; set; }          // telephone1
        public string? Email { get; set; }          // emailaddress1
        public decimal? CreditLimit { get; set; }   // creditlimit

        // ---------------------------
        // Optional / Custom Fields
        // ---------------------------
        public decimal? Outstanding { get; set; }      // custom field if needed
        public string? VATNumber { get; set; }         // custom field if needed
        public Guid? CustomerGroupId { get; set; }     // custom lookup
        public string? CustomerGroup { get; set; }     // display name
    }
}
