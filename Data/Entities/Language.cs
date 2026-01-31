using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EasyLingo.Data.Entities
{
    public class Language
    {
        [Key]
        public int LangId { get; set; }

        public string Name { get; set; }
        public string Code { get; set; }

        public ICollection<Set> Sets { get; set; }
    }
}
