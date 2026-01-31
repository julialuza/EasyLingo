using System.ComponentModel.DataAnnotations;

namespace EasyLingo.Data.Entities
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        public string Username { get; set; }
        public string PasswordHash { get; set; }

        public ICollection<UserTermStatus> UserTermStatuses { get; set; }
        public ICollection<UserSetCategory> UserSetCategories { get; set; } = new List<UserSetCategory>();
        public ICollection<UserSetProgress> UserSetProgresses { get; set; }
        public ICollection<Set> Sets { get; set; }

    }
}

