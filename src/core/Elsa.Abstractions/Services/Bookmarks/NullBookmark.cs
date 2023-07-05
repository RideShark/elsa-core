namespace Elsa.Services
{
    public class NullBookmark  : BaseIBookmark
    {
        public static readonly IBookmark Instance = new NullBookmark();
    }
}