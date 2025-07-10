namespace RichKid.Web.Models
{
    public static class UserGroups
    {
        public static readonly Dictionary<int, string> Groups = new()
        {
            { 1, "מנהל" },
            { 2, "עורך" },
            { 3, "משתמש רגיל" },
            { 4, "צפייה בלבד" }
        };

        public static string GetName(int? groupId)
        {
            if (groupId.HasValue && Groups.ContainsKey(groupId.Value))
                return Groups[groupId.Value];
            return "לא משויך";
        }
    }
}
