using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using WBSAlpha.Models;
/*
Modified By:    Quinn Helm
Date:           27-11-2021
*/
namespace WBSAlpha.Authorization
{
    public class ProfileViewAuthorization : AuthorizationHandler<OperationAuthorizationRequirement, CoreUser>
    {
        UserManager<CoreUser> _userManager;

        public ProfileViewAuthorization(UserManager<CoreUser> userManager)
        {
            _userManager = userManager;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OperationAuthorizationRequirement requirement, CoreUser resource)
        {
            if (context.User == null || resource == null)
            {
                return Task.CompletedTask;
            }

            if ((resource.Id != _userManager.GetUserId(context.User)) || (context.User.IsInRole(AuthorizationConstants.ModeratorRole) 
                && requirement.Name != AuthorizationConstants.ReadOperation 
                && requirement.Name != AuthorizationConstants.DeleteOperation)
                || (context.User.IsInRole(AuthorizationConstants.AdministratorRole)
                && requirement.Name != AuthorizationConstants.ReadOperation 
                && requirement.Name != AuthorizationConstants.UpdateOperation 
                && requirement.Name != AuthorizationConstants.DeleteOperation))
            {
                // if someone other than the right user (matching id) tries to access the profile, block it UNLESS
                // it is a moderator trying to view or delete the profile
                // it is an administrator trying to read/update/delete a profile -- just in case
                return Task.CompletedTask;
            }

            if ((resource.Id == _userManager.GetUserId(context.User)) 
                || context.User.IsInRole(AuthorizationConstants.ModeratorRole) 
                || context.User.IsInRole(AuthorizationConstants.AdministratorRole))
            {
                // if the user has made it this far and they meet these requirements, they've succeeded
                context.Succeed(requirement); // otherwise it will result in an empty completed task!
            }

            return Task.CompletedTask;
        }
    }
}