using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EasyLingo.Data.Entities
{
    public class Term
    {
        [Key]
        public int TermId { get; set; }
        public string TermName { get; set; }
        public string Definition { get; set; }

        public int SetId { get; set; }
        public Set Set { get; set; }

        public ICollection<UserTermStatus> UserTermStatuses { get; set; }
    }
}

