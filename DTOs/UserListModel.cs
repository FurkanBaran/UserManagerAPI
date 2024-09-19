namespace UserManager.DTOs
{
    public class UserListModel
    {
        public List<UserListItem> UserInfos { get; set; } = [];
        public int TotalItemCount { get; set; }              
        public int PageIndex { get; set; }           
        public int PageItemCount { get; set; }     

    }


}
