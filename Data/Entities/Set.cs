using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EasyLingo.Data.Entities
{
    public class Set
    {
        [Key]
        public int SetId { get; set; }

        public string Name { get; set; } = "";
        public string Description { get; set; } = "";

        public int LangId { get; set; }
        public Language Language { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int? SetCategoryId { get; set; }
        public UserSetCategory? SetCategory { get; set; }

        public ICollection<Term> Terms { get; set; } = new List<Term>();
        public ICollection<UserSetProgress> UserSetProgresses { get; set; } = new List<UserSetProgress>();
    }
}
