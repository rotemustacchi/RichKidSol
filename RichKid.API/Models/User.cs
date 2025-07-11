using System.ComponentModel.DataAnnotations;

namespace RichKid.Shared.Models
{
    public class User
    {
        public int UserID { get; set; }

        public bool Active { get; set; }

        [Required(ErrorMessage = "שם משתמש הוא שדה חובה")]
        [MinLength(3, ErrorMessage = "שם משתמש חייב להכיל לפחות 3 תווים")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "סיסמה היא שדה חובה")]
        [MinLength(4, ErrorMessage = "הסיסמה חייבת להכיל לפחות 4 תווים")]
        public string Password { get; set; } = string.Empty;

        public int? UserGroupID { get; set; }

        [Required]
        public UserData Data { get; set; } = new UserData();
    }

    public class UserData
    {
        public string CreationDate { get; set; } = string.Empty;

        [Required(ErrorMessage = "שם פרטי הוא שדה חובה")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "שם משפחה הוא שדה חובה")]
        public string LastName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "מספר טלפון אינו תקין")]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "כתובת אימייל אינה תקינה")]
        public string Email { get; set; } = string.Empty;
    }
}
