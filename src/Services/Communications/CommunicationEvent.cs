namespace Identity.Business.Users.Services.Communications
{
    public enum CommunicationEvent
    {
        OnUserCreated,
        OnPasswordChangeRequested,
        OnPasswordChanged,
        OnUserActivated,
        OnUserDeactivated
    }
}