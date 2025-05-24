using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess
{
    public class GameListing
    {
        [Key]
        public int appid { get; set; }
        public string name { get; set; }

    }
}
