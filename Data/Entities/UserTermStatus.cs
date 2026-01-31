using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace EasyLingo.Data.Entities
{
    public class UserTermStatus
    {
        [Key]
        public int UserTermStatusId { get; set; }

        public int Status { get; set; } // 0 - nie zna, 1 - zna

        public int UserId { get; set; }
        public User User { get; set; }

        public int TermId { get; set; }
        public Term Term { get; set; }
    }
}
