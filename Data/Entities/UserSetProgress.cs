using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace EasyLingo.Data.Entities
{
    public class UserSetProgress
    {
        [Key]
        public int UserProgressId { get; set; }

        public int ProgressPercent { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public int SetId { get; set; }
        public Set Set { get; set; }
    }
}

