using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands
{
    public record UpdateUserDetailsCommand(
        Guid ExternalUserId,
        uint RowVersion,
        string? FirstName,
        string? LastName,
        string? Country,
        string? City,
        string? Address);

    public record GetUserCommand(Guid ExternalUserId);
}
