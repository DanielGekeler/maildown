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

        var output = new Option<string>(
            name: "-o",
            description: "Output path where emails should be saved to",
            getDefaultValue: () => ".")
        { ArgumentHelpName = "path" };

        var maxlength = new Option<int>(
            name: "--length",
            description: "Maximum file name length",
            getDefaultValue: () => 0)
        { ArgumentHelpName = "n" };

        var nospaces = new Option<bool>(
            name: "--nospaces",
            description: "Replace spaces in filenames with underscores");

        var rootCommand = new RootCommand("Download all emails your emails from any IMAP server")
            { server, port, user, password, output, maxlength, nospaces };
        rootCommand.SetHandler(DownloadAsync, server, port, user, password, output, maxlength, nospaces);
        rootCommand.Invoke(args);
    }

    private static async Task DownloadAsync(string server, int port, string user, string pass, string output, int maxlength, bool nospaces)
    {
        using var client = new ImapClient();
        await client.ConnectAsync(server, port, SecureSocketOptions.SslOnConnect);
        await client.AuthenticateAsync(user, pass);

        var folders = await client.GetFoldersAsync(new FolderNamespace('/', ""));
        foreach (var folder in folders)
        {
            await folder.OpenAsync(FolderAccess.ReadOnly);
            await folder.SearchAsync(SearchQuery.All);
            var folderName = folder.FullName.Replace(folder.DirectorySeparator, Path.DirectorySeparatorChar);
            Console.WriteLine($"Found {folder.Count} messages in {folderName}");
            if (folder.Count == 0) continue;
            Directory.CreateDirectory(Path.Combine(output, folderName));

            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var msg in folder)
            {
                var name = $"{msg.From[0].Name}-{msg.Subject}";
                if (nospaces) name = Regex.Replace(name, @"\s+", "_");
                name = new string([.. name.Select(c => invalidChars.Contains(c) ? '_' : c)]);
                if (maxlength > 0 && name.Length > maxlength) name = name.Substring(0, maxlength);

                await msg.WriteToAsync(Path.Combine(output, folderName, name + ".eml"));
            }
        }
        await client.DisconnectAsync(true);
    }
}
