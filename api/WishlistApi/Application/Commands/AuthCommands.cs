using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands
{
    public record LoginCommand(
        string Username,
        string Password);

    public record LoginResult(
        Guid UserId,
        string Username,
        string Role);

    public record RegisterUserCommand(
        string Username,
        string Password);
}
