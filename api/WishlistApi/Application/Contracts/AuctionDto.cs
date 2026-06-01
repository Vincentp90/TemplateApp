using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Contracts
{
    public record AuctionDto(
            int ID,
            DateTimeOffset StartDate,
            DateTimeOffset EndDate,
            bool UserHasBid,
            decimal StartingPrice,
            decimal? CurrentPrice,
            int AppID,
            string AppName,
            uint RowVersion
            );
}
