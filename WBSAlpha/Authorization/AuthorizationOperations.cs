using Microsoft.AspNetCore.Authorization.Infrastructure;
/*
Modified By:    Quinn Helm
Date:           28-11-2021
*/
namespace WBSAlpha.Authorization
{
    /// <summary>
    /// Used to create authorization contracts that determine whether a user can perform a certain action.
    /// </summary>
    public static class AuthorizationOperations
    {
        public static OperationAuthorizationRequirement Create = new OperationAuthorizationRequirement { Name = AuthorizationConstants.CreateOperation };
        public static OperationAuthorizationRequirement Read = new OperationAuthorizationRequirement { Name = AuthorizationConstants.ReadOperation };
        public static OperationAuthorizationRequirement Update = new OperationAuthorizationRequirement { Name = AuthorizationConstants.UpdateOperation };
        public static OperationAuthorizationRequirement Delete = new OperationAuthorizationRequirement { Name = AuthorizationConstants.DeleteOperation };
    }

    /// <summary>
    /// Used to create authorization contracts that determine whether a user can perform a certain action.
    /// </summary>
    public class AuthorizationConstants
    {
        // operations
        public static readonly string CreateOperation = "Create";
        public static readonly string ReadOperation = "Read";
        public static readonly string UpdateOperation = "Update";
        public static readonly string DeleteOperation = "Delete";

        // roles
        public static readonly string AdministratorRole = "Administrator";
        public static readonly string ModeratorRole = "Moderator";
    }
}
