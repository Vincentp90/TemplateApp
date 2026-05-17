using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands
{
    public record AddToWishlistCommand(
        int UserId,
        int AppId);
}
