using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands
{
    public record PlaceBidCommand(
        int AuctionId,
        int UserId,
        decimal Amount,
        uint RowVersion);
}
