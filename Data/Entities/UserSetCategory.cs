using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EasyLingo.Data.Entities
{
    public class UserSetCategory
    {
        [Key]
        public int SetCategoryId { get; set; }

        public string Name { get; set; } = "";

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public ICollection<Set> Sets { get; set; } = new List<Set>();
    }
}
