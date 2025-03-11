using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;

namespace maildown;

class Program
{
    static void Main(string[] args)
    {
        using var client = new ImapClient();
        client.Connect(args[0], 993, SecureSocketOptions.SslOnConnect);
        client.Authenticate(args[1], args[2]);

        client.Inbox.Open(FolderAccess.ReadOnly);
        var uids = client.Inbox.Search(SearchQuery.All);

        foreach (var uid in uids)
        {
            var message = client.Inbox.GetMessage(uid);
            message.WriteTo(string.Format("{0}.eml", uid));
        }

        client.Disconnect(true);
    }
}
