using Microsoft.AspNetCore.Authorization.Infrastructure;
/*
Modified By:    Quinn Helm
Date:           27-11-2021
*/
namespace WBSAlpha.Authorization
{
    public static class AuthorizationOperations
    {
        public static OperationAuthorizationRequirement Create = new OperationAuthorizationRequirement { Name = AuthorizationConstants.CreateOperation };
        public static OperationAuthorizationRequirement Read = new OperationAuthorizationRequirement { Name = AuthorizationConstants.ReadOperation };
        public static OperationAuthorizationRequirement Update = new OperationAuthorizationRequirement { Name = AuthorizationConstants.UpdateOperation };
        public static OperationAuthorizationRequirement Delete = new OperationAuthorizationRequirement { Name = AuthorizationConstants.DeleteOperation };
        public static OperationAuthorizationRequirement Promote = new OperationAuthorizationRequirement { Name = AuthorizationConstants.PromoteOperation };
        public static OperationAuthorizationRequirement Demote = new OperationAuthorizationRequirement { Name = AuthorizationConstants.DemoteOperation };
    }

    public class AuthorizationConstants
    {
        public static readonly string CreateOperation = "Create";
        public static readonly string ReadOperation = "Read";
        public static readonly string UpdateOperation = "Update";
        public static readonly string DeleteOperation = "Delete";
        public static readonly string PromoteOperation = "Promote";
        public static readonly string DemoteOperation = "Demote";

        public static readonly string AdministratorRole = "Administrator";
        public static readonly string ModeratorRole = "Moderator";
    }
}
