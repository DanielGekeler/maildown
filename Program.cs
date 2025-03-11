using System.CommandLine;
using System.Text.RegularExpressions;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;

namespace maildown;

class Program
{
    static void Main(string[] args)
    {
        var server = new Option<string>(name: "-s", description: "Domain of the IMAP server")
        { IsRequired = true, ArgumentHelpName = "imap.example.com" };

        var port = new Option<int>(
            name: "--port",
            description: "Port of the IMAP server",
            getDefaultValue: () => 993)
        { ArgumentHelpName = "port" };

        var user = new Option<string>(name: "-u", description: "IMAP username (usually the email address)")
        { IsRequired = true, ArgumentHelpName = "user@example.com" };

        var password = new Option<string>(name: "-p", description: "IMAP password")
        { IsRequired = true, ArgumentHelpName = "password" };

        var target = new Option<string>(
            name: "-t",
            description: "Path where emails should be saved to",
            getDefaultValue: () => ".")
        { ArgumentHelpName = "path" };

        var maxlength = new Option<int>(
            name: "--length",
            description: "Maximum file name length",
            getDefaultValue: () => 0)
        { ArgumentHelpName = "n" };

        var rootCommand = new RootCommand("Download all emails your emails from any IMAP server")
            { server, port, user, password, target, maxlength };
        rootCommand.SetHandler(DownloadAsync, server, port, user, password, target, maxlength);
        rootCommand.Invoke(args);
    }

    private static async Task DownloadAsync(string server, int port, string user, string pass, string target, int maxlength)
    {
        Directory.CreateDirectory(target);

        using var client = new ImapClient();
        await client.ConnectAsync(server, port, SecureSocketOptions.SslOnConnect);
        await client.AuthenticateAsync(user, pass);

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
                if (maxlength > 0 && name.Length > maxlength) name = name.Substring(0, maxlength);

                await msg.WriteToAsync(Path.Combine(target, name + ".eml"));
            }
        }
        await client.DisconnectAsync(true);
    }
}
