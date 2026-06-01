using Domain.ValueObjects;

namespace Application.Commands
{
    public record UpdateUserDetailsCommand(
        Guid ExternalUserId,
        uint RowVersion,
        FullName Name,
        Address Location);

    public record GetUserCommand(Guid ExternalUserId);
}
