using System.Text.RegularExpressions;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;

namespace maildown;

class Program
{
    static async Task Main(string[] args)
    {
        var target = "mails";
        Directory.CreateDirectory(target);

        using var client = new ImapClient();
        await client.ConnectAsync(args[0], 993, SecureSocketOptions.SslOnConnect);
        await client.AuthenticateAsync(args[1], args[2]);

        var folders = await client.GetFoldersAsync(new FolderNamespace('/', ""));
        foreach (var folder in folders)
        {
            await folder.OpenAsync(FolderAccess.ReadOnly);
            await folder.SearchAsync(SearchQuery.All);
            Console.WriteLine($"Found {folder.Count} messages in {folder.FullName}");

            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var msg in folder)
            {
                var name = $"{msg.From[0].Name}-{msg.Subject}";
                name = Regex.Replace(name, @"\s+", "_");
                name = new string([.. name.Select(c => invalidChars.Contains(c) ? '_' : c)]);
                if (name.Length > 50) name = name.Substring(0, 50);

                await msg.WriteToAsync(Path.Combine(target, name + ".eml"));
            }
        }
        await client.DisconnectAsync(true);
    }
}
