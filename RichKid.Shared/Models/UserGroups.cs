namespace RichKid.Shared.Models
{
    public static class UserGroups
    {
        public const int ADMIN = 1;
        public const int EDITOR = 2;
        public const int REGULAR_USER = 3;
        public const int VIEW_ONLY = 4;

        public static readonly Dictionary<int, string> Groups = new()
        {
            { ADMIN, "מנהל" },
            { EDITOR, "עורך" },
            { REGULAR_USER, "משתמש רגיל" },
            { VIEW_ONLY, "צפייה בלבד" }
        };

        public static readonly Dictionary<int, string> GroupsEnglish = new()
        {
            { ADMIN, "Admin" },
            { EDITOR, "Editor" },
            { REGULAR_USER, "Regular User" },
            { VIEW_ONLY, "View Only" }
        };

        public static string GetName(int? groupId, bool useEnglish = false)
        {
            if (groupId.HasValue)
            {
                var groups = useEnglish ? GroupsEnglish : Groups;
                if (groups.ContainsKey(groupId.Value))
                    return groups[groupId.Value];
            }
            return useEnglish ? "Unassigned" : "לא משויך";
        }

        public static string GetPermissionsByGroup(int? groupId)
        {
            return groupId switch
            {
                ADMIN => "Full Access (Create, Edit, Delete, View)",
                EDITOR => "Create, Edit, View",
                REGULAR_USER => "Edit Own Profile, View",
                VIEW_ONLY => "View Only",
                _ => "No Access"
            };
        }
    }
}