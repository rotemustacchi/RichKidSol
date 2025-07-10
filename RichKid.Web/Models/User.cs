namespace RichKid.Web.Models
{
    public class User
    {
        public int UserID { get; set; }
        public bool Active { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int? UserGroupID { get; set; }
        public UserData Data { get; set; } = new UserData();
    }

    public class UserData
{
    public string CreationDate { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
}