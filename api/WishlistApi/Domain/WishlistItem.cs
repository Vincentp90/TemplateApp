namespace Domain;

public class WishlistItem
{
    public int Id { get; private set; }
    public int AppId { get; private set; }
    //TODO how to handle AppListing field
    public string AppName { get; private set; }
    public DateTimeOffset DateAdded { get; private set; }

    public int UserId { get; private set; }

    public WishlistItem(int id, int appId, string name, DateTimeOffset dateAdded, int userId)
    {
        Id = id;
        AppId = appId;
        AppName = name;
        DateAdded = dateAdded;
        UserId = userId;
    }

    public WishlistItem(int appId, DateTimeOffset dateAdded, int userId)
    {
        AppId = appId;
        DateAdded = dateAdded;
        UserId = userId;
        Id = 0;
        AppName = string.Empty;
    }
}
