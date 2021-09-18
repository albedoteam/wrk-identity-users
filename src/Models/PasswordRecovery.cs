namespace Identity.Business.Users.Models
{
    using System;
    using AlbedoTeam.Sdk.DataLayerAccess.Abstractions;
    using AlbedoTeam.Sdk.DataLayerAccess.Attributes;

    [Collection("PasswordRecoveries")]
    public class PasswordRecovery : DocumentWithAccount
    {
        public string UserId { get; set; }
        public string ValidationToken { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}