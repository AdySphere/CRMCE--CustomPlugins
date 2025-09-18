// -----------------------------------------------------------------------------
// DataverseReader.cs
// Author: Adyasha Mallick
// Description:
//   A service to read data from Microsoft Dataverse entities (e.g., Users, Customers)
//   using ServiceClient. Supports delta queries, paging, and maps results to DTOs.
//
// Prerequisites:
//   - Microsoft.PowerPlatform.Dataverse.Client
//   - Microsoft.Xrm.Sdk
//   - Microsoft.Extensions.Logging
// -----------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using fs.Shared.DTO;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace fs.API.Services.Integration;

public sealed class DataverseReader
{
    private readonly IConfiguration _cfg;
    private readonly ILogger<DataverseReader> _log;

    /// <summary>
    /// Constructor
    /// </summary>
    public DataverseReader(IConfiguration cfg, ILogger<DataverseReader> log)
    {
        _cfg = cfg;
        _log = log;
    }

    /// <summary>
    /// Creates and returns a Dataverse ServiceClient
    /// using client credentials (ClientId + ClientSecret)
    /// </summary>
    private ServiceClient CreateClient()
    {
        var url = _cfg["Dataverse:Url"] ?? throw new InvalidOperationException("Dataverse:Url missing");
        var clientId = _cfg["Dataverse:ClientId"] ?? throw new InvalidOperationException("Dataverse:ClientId missing");
        var clientSecret = _cfg["Dataverse:ClientSecret"] ?? throw new InvalidOperationException("Dataverse:ClientSecret missing");

        // ServiceClient automatically handles token acquisition
        var cs = $"AuthType=ClientSecret;Url={url};ClientId={clientId};ClientSecret={clientSecret};";
        return new ServiceClient(cs);
    }

    // -------------------------------------------------------------------------
    // Read Users (systemuser entity)
    // -------------------------------------------------------------------------
    public async IAsyncEnumerable<List<UserDTO>> ReadUsersAsync(
        DateTime sinceUtc,
        int pageSize,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        using var svc = CreateClient();

        // Build the query for systemuser
        var qe = new QueryExpression("systemuser")
        {
            ColumnSet = new ColumnSet(
                "systemuserid",
                "firstname", "lastname",
                "internalemailaddress", "mobilephone",
                "jobtitle",
                "isdisabled",
                "modifiedon"
            ),
            PageInfo = new PagingInfo { Count = pageSize, PageNumber = 1 }
        };

        // Delta query: only modified since a given date
        qe.Criteria.AddCondition("modifiedon", ConditionOperator.OnOrAfter, sinceUtc);

        string? cookie = null;
        do
        {
            ct.ThrowIfCancellationRequested();

            qe.PageInfo.PagingCookie = cookie;

            var page = svc.RetrieveMultiple(qe);

            // Map entities to DTOs
            var batch = page.Entities.Select(e =>
            {
                var first = e.GetAttributeValue<string>("firstname");
                var last = e.GetAttributeValue<string>("lastname");
                var name = string.Join(' ', new[] { first, last }.Where(s => !string.IsNullOrWhiteSpace(s)));

                var modified = e.GetAttributeValue<DateTime?>("modifiedon")?.ToUniversalTime();
                var disabled = e.GetAttributeValue<bool?>("isdisabled") ?? false;

                return new UserDTO
                {
                    Id = e.Id,
                    Status = disabled ? 1 : 0,
                    IsSynced = 0,
                    IsDeleted = null,
                    CreatedDate = modified ?? DateTime.UtcNow,
                    ModifiedDate = modified ?? DateTime.UtcNow,
                    Name = string.IsNullOrWhiteSpace(name) ? null : name,
                    Email = e.GetAttributeValue<string>("internalemailaddress"),
                    JobTitle = e.GetAttributeValue<string>("jobtitle"),
                    Mobile = e.GetAttributeValue<string>("mobilephone"),
                    Gender = null,
                    JoiningDate = null,
                    TerminationDate = disabled ? modified : null,
                    Nationality = null,
                    DateOfBirth = null
                };
            }).ToList();

            yield return batch;

            cookie = page.PagingCookie;
            qe.PageInfo.PageNumber++;
        }
        while (!string.IsNullOrEmpty(cookie));

        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // Read Customers (account entity)
    // -------------------------------------------------------------------------
    public async IAsyncEnumerable<List<CustomerDTO>> ReadCustomersAsync(
        DateTime sinceUtc,
        int pageSize,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        using var svc = CreateClient();

        // Build the query for account
        var qe = new QueryExpression("account")
        {
            ColumnSet = new ColumnSet(
                "accountid",
                "name",
                "accountnumber",
                "telephone1",
                "emailaddress1",
                "creditlimit",
                "modifiedon"
                // Add custom columns here if needed
            ),
            PageInfo = new PagingInfo { Count = pageSize, PageNumber = 1 }
        };

        // Delta query + guard for required fields
        qe.Criteria.AddCondition("modifiedon", ConditionOperator.OnOrAfter, sinceUtc);
        qe.Criteria.AddCondition("name", ConditionOperator.NotNull);

        string? cookie = null;
        do
        {
            ct.ThrowIfCancellationRequested();

            qe.PageInfo.PagingCookie = cookie;

            var page = svc.RetrieveMultiple(qe);

            var list = page.Entities.Select(e =>
            {
                var modified = e.GetAttributeValue<DateTime?>("modifiedon")?.ToUniversalTime();
                var credit = e.GetAttributeValue<Money>("creditlimit")?.Value;

                return new CustomerDTO
                {
                    Id = e.Id,
                    CreatedDate = modified ?? DateTime.UtcNow,
                    ModifiedDate = modified ?? DateTime.UtcNow,
                    Name = e.GetAttributeValue<string>("name"),
                    Code = e.GetAttributeValue<string>("accountnumber"),
                    Phone = e.GetAttributeValue<string>("telephone1"),
                    Email = e.GetAttributeValue<string>("emailaddress1"),
                    CreditLimit = credit,
                    Outstanding = null,
                    VATNumber = null,
                    CustomerGroupId = null,
                    CustomerGroup = null
                };
            })
            .Where(c => !string.IsNullOrWhiteSpace(c.Name))
            .ToList();

            yield return list;

            cookie = page.PagingCookie;
            qe.PageInfo.PageNumber++;
        }
        while (!string.IsNullOrEmpty(cookie));

        await Task.CompletedTask;
    }
}
