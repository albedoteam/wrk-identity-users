namespace Identity.Business.Users.Models
{
    using System.Collections.Generic;
    using AlbedoTeam.Identity.Contracts.Common;
    using AlbedoTeam.Sdk.DataLayerAccess.Abstractions;
    using AlbedoTeam.Sdk.DataLayerAccess.Attributes;
    using MongoDB.Bson;
    using MongoDB.Bson.Serialization.Attributes;

    [Collection("Users")]
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
        
        [BsonRepresentation(BsonType.String)]
        public Provider Provider { get; set; }
        public string ProviderId { get; set; }
        public string UsernameAtProvider { get; set; }
        public string UpdateReason { get; set; }
    }
}