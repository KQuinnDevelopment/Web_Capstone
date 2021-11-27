using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using System.Threading.Tasks;
using WBSAlpha.Models;
/*
Modified By:    Quinn Helm
Date:           27-11-2021
*/
namespace WBSAlpha.Authorization
{
    public class ModerationAlterAuthorization : AuthorizationHandler<OperationAuthorizationRequirement, CoreUser>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OperationAuthorizationRequirement requirement, CoreUser resource)
        {
            if (context.User == null || resource == null)
            {
                return Task.CompletedTask;
            }

            if (requirement.Name != AuthorizationConstants.PromoteOperation &&
                requirement.Name != AuthorizationConstants.DemoteOperation)
            {
                // if someone is trying to perform any other action than these two, it returns
                return Task.CompletedTask;
            }

            if (context.User.IsInRole(AuthorizationConstants.AdministratorRole))
            {
                // if the user has made it this far and they are a mod, they've succeeded
                context.Succeed(requirement); // otherwise it will result in an empty completed task!
            }

            return Task.CompletedTask;
        }
    }
}
