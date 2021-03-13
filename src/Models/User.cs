using System.Collections.Generic;
using AlbedoTeam.Identity.Contracts.Common;
using AlbedoTeam.Sdk.DataLayerAccess.Abstractions;
using AlbedoTeam.Sdk.DataLayerAccess.Attributes;

namespace Identity.Business.Users.Models
{
    [BsonCollection("Users")]
    public class User : DocumentWithAccount
    {
        public string UserTypeId { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public bool Active { get; set; }
        public Dictionary<string, string> CustomProfileFields { get; set; }
        public List<string> Groups { get; set; }
        public Provider Provider { get; set; }
        public string ProviderId { get; set; }
        public string UpdateReason { get; set; }
    }
}