namespace Drawing.Application.Abstractions.Identity;
public interface ICurrentUser
{
    Guid? UserId { get; }
    bool IsAuthenticated { get; }
}