using System.Windows;

namespace ShoeShop
{
    public partial class App : Application
    {
        public int? CurrentUserId { get; set; }
        public string CurrentUserFullName { get; set; }
    }
}