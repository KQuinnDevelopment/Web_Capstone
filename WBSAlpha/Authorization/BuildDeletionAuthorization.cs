using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using WBSAlpha.Models;
/*
Modified By:    Quinn Helm
Date:           27-12-2021
*/
namespace WBSAlpha.Authorization
{
    /// <summary>
    /// Prevents any user other than the creator of an item build, or an administrator, from deleting an item build.
    /// The option to do so should only be visible to those users, as a catch-all in the event that other code fails.
    /// </summary>
    public class BuildDeletionAuthorization : AuthorizationHandler<OperationAuthorizationRequirement, CoreUser>
    {
        UserManager<CoreUser> _userManager;

        public BuildDeletionAuthorization(UserManager<CoreUser> userManager)
        {
            _userManager = userManager;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OperationAuthorizationRequirement requirement, CoreUser resource)
        {
            if (context.User == null || resource == null)
            {
                return Task.CompletedTask;
            }

            if ((resource.Id != _userManager.GetUserId(context.User)) || 
                (!context.User.IsInRole(AuthorizationConstants.AdministratorRole)))
            {
                // if someone other than the owner (matching ID) tries to delete a build, block it UNLESS
                // it is an administrator trying to delete the build
                return Task.CompletedTask;
            }

            if ((resource.Id == _userManager.GetUserId(context.User))
                || context.User.IsInRole(AuthorizationConstants.AdministratorRole))
            {
                // if the user has made it this far and they meet these requirements, they've succeeded
                context.Succeed(requirement); // otherwise it will result in an empty completed task!
            }

            return Task.CompletedTask;
        }
    }
}