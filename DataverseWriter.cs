// -----------------------------------------------------------------------------
// DataverseWriter.cs
// Author: Adyasha Mallick
// Description:
//   A service to write (create/update) data into Microsoft Dataverse entities.
//   Supports upsert operations based on primary keys or unique fields.
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

namespace fs.API.Services.Integration;

public sealed class DataverseWriter
{
    private readonly IConfiguration _cfg;
    private readonly ILogger<DataverseWriter> _log;

    /// <summary>
    /// Constructor
    /// </summary>
    public DataverseWriter(IConfiguration cfg, ILogger<DataverseWriter> log)
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

        var cs = $"AuthType=ClientSecret;Url={url};ClientId={clientId};ClientSecret={clientSecret};";
        return new ServiceClient(cs);
    }

    // -------------------------------------------------------------------------
    // Write Users (systemuser entity)
    // -------------------------------------------------------------------------
    public async Task WriteUsersAsync(IEnumerable<UserDTO> users, CancellationToken ct = default)
    {
        using var svc = CreateClient();

        foreach (var user in users)
        {
            ct.ThrowIfCancellationRequested();

            var entity = new Entity("systemuser")
            {
                Id = user.Id // For upsert; if Id is empty, it will create a new record
            };

            // Map DTO to Dataverse fields
            if (!string.IsNullOrWhiteSpace(user.Name))
            {
                var names = user.Name.Split(' ');
                if (names.Length > 0) entity["firstname"] = names[0];
                if (names.Length > 1) entity["lastname"] = string.Join(' ', names.Skip(1));
            }

            if (!string.IsNullOrWhiteSpace(user.Email)) entity["internalemailaddress"] = user.Email;
            if (!string.IsNullOrWhiteSpace(user.Mobile)) entity["mobilephone"] = user.Mobile;
            if (!string.IsNullOrWhiteSpace(user.JobTitle)) entity["jobtitle"] = user.JobTitle;
            if (user.Status == 1) entity["isdisabled"] = true;

            try
            {
                // Upsert operation: Create if Id empty, else Update
                if (user.Id == Guid.Empty)
                    svc.Create(entity);
                else
                    svc.Update(entity);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, $"Error writing user {user.Name} ({user.Id})");
            }
        }

        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // Write Customers (account entity)
    // -------------------------------------------------------------------------
    public async Task WriteCustomersAsync(IEnumerable<CustomerDTO> customers, CancellationToken ct = default)
    {
        using var svc = CreateClient();

        foreach (var customer in customers)
        {
            ct.ThrowIfCancellationRequested();

            var entity = new Entity("account")
            {
                Id = customer.Id // For upsert; create if empty
            };

            if (!string.IsNullOrWhiteSpace(customer.Name)) entity["name"] = customer.Name;
            if (!string.IsNullOrWhiteSpace(customer.Code)) entity["accountnumber"] = customer.Code;
            if (!string.IsNullOrWhiteSpace(customer.Phone)) entity["telephone1"] = customer.Phone;
            if (!string.IsNullOrWhiteSpace(customer.Email)) entity["emailaddress1"] = customer.Email;
            if (customer.CreditLimit.HasValue) entity["creditlimit"] = new Microsoft.Xrm.Sdk.Money(customer.CreditLimit.Value);

            // Optional: map custom fields if available
            if (!string.IsNullOrWhiteSpace(customer.VATNumber)) entity["new_vatnumber"] = customer.VATNumber;
            if (customer.CustomerGroupId.HasValue) entity["new_customergroupid"] = new EntityReference("new_customergroup", customer.CustomerGroupId.Value);

            try
            {
                if (customer.Id == Guid.Empty)
                    svc.Create(entity);
                else
                    svc.Update(entity);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, $"Error writing customer {customer.Name} ({customer.Id})");
            }
        }

        await Task.CompletedTask;
    }
}
