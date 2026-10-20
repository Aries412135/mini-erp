namespace MiniErp.Application.Common.Interface;

public interface ICurrentUser
{
    Guid? UserId { get; }
}