// -----------------------------------------------------------------------------
// CustomerDeletionPlugin.cs
// Author: Adyasha Mallick
// Description:
//   Plugin for Microsoft Dynamics 365 (CRM CE) that triggers on deletion of
//   Customer (account) records or when Users are added/removed from Teams.
//   This plugin handles cascading business logic, logging, or cleanup.
// -----------------------------------------------------------------------------

using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace fs.API.Plugins
{
    public class CustomerDeletionPlugin : IPlugin
    {
        /// <summary>
        /// Entry point for the plugin
        /// </summary>
        /// <param name="serviceProvider">Service provider injected by CRM</param>
        public void Execute(IServiceProvider serviceProvider)
        {
            // Obtain execution context from the service provider
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            // Obtain organization service reference
            var factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = factory.CreateOrganizationService(context.UserId);

            // Obtain tracing service for logging
            var tracer = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            try
            {
                // Plugin triggers on Delete, Associate, and Disassociate
                // Check the message name
                if (context.MessageName == "Delete")
                {
                    // Get target entity reference
                    var target = (EntityReference)context.InputParameters["Target"];

                    if (target.LogicalName == "account")
                    {
                        // Customer deleted
                        tracer.Trace($"Customer deleted: {target.Id}");
                        HandleCustomerDeletion(service, target, tracer);
                    }
                }
                else if (context.MessageName == "Associate" || context.MessageName == "Disassociate")
                {
                    // Handle User-Team changes
                    var relationship = (Relationship)context.InputParameters["Relationship"];
                    var relatedEntities = (EntityReferenceCollection)context.InputParameters["RelatedEntities"];

                    if (relationship.SchemaName == "teamuser")
                    {
                        foreach (var userRef in relatedEntities)
                        {
                            if (context.MessageName == "Associate")
                                tracer.Trace($"User {userRef.Id} added to team {((EntityReference)context.InputParameters["Target"]).Id}");
                            else
                                tracer.Trace($"User {userRef.Id} removed from team {((EntityReference)context.InputParameters["Target"]).Id}");

                            HandleUserTeamChange(service, userRef, (EntityReference)context.InputParameters["Target"], context.MessageName, tracer);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                tracer.Trace($"Plugin error: {ex.Message}");
                throw new InvalidPluginExecutionException($"CustomerDeletionPlugin failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Handles business logic for Customer deletion
        /// </summary>
        private void HandleCustomerDeletion(IOrganizationService service, EntityReference customer, ITracingService tracer)
        {
            // Example: delete related custom records, log activity, etc.
            tracer.Trace($"Performing cleanup for deleted Customer: {customer.Id}");

            // Example: fetch related contacts
            var query = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet("contactid"),
                Criteria = { Conditions = { new ConditionExpression("parentcustomerid", ConditionOperator.Equal, customer.Id) } }
            };

            var contacts = service.RetrieveMultiple(query);
            foreach (var c in contacts.Entities)
            {
                tracer.Trace($"Deleting related Contact {c.Id}");
                service.Delete("contact", c.Id);
            }
        }

        /// <summary>
        /// Handles logic when a User is added/removed from a Team
        /// </summary>
        private void HandleUserTeamChange(IOrganizationService service, EntityReference user, EntityReference team, string action, ITracingService tracer)
        {
            // action: "Associate" = added, "Disassociate" = removed
            tracer.Trace($"Handling User-Team change: User {user.Id}, Team {team.Id}, Action: {action}");

            // Example: log activity or update a custom field
            var activity = new Entity("annotation")
            {
                ["subject"] = $"User-Team {action}",
                ["notetext"] = $"User {user.Id} was {action.ToLower()} to Team {team.Id}",
                ["objectid"] = user,
                ["objecttypecode"] = "systemuser"
            };

            service.Create(activity);
        }
    }
}
