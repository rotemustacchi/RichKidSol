using System.ComponentModel.DataAnnotations;

namespace RichKid.Web.Models
{
    public class User
    {
        public int UserID { get; set; }

        public bool Active { get; set; }

        [Required(ErrorMessage = "שם משתמש חובה")]
        [StringLength(20, ErrorMessage = "שם משתמש לא יכול להיות יותר מ-20 תווים")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "סיסמה חובה")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "הסיסמה חייבת להכיל לפחות 6 תווים")]
        public string Password { get; set; } = string.Empty;

        public int? UserGroupID { get; set; }

        [Required(ErrorMessage = "נתוני משתמש חובה")]
        public UserData Data { get; set; } = new();
    }

    public class UserData
    {
        public string CreationDate { get; set; } = string.Empty;

        [Required(ErrorMessage = "שם פרטי חובה")]
        [StringLength(30, ErrorMessage = "שם פרטי לא יכול להיות יותר מ-30 תווים")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "שם משפחה חובה")]
        [StringLength(30, ErrorMessage = "שם משפחה לא יכול להיות יותר מ-30 תווים")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "טלפון חובה")]
        [Phone(ErrorMessage = "מספר טלפון לא תקין")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "אימייל חובה")]
        [EmailAddress(ErrorMessage = "כתובת אימייל לא תקינה")]
        public string Email { get; set; } = string.Empty;
    }
}
