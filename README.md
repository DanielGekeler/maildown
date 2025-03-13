# Maildown
Maildown is a tiny cli tool to download all emails from an IMAP account.

## Prequisites
- [Dotnet SDK 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

## Building
```sh
dotnet publish
```

## Usage
Your email provider must allow normal password authentication. This tool does not support Oauth.

```sh
maildown -s <server> -u <user> -p <password>
```
This will download all emails of all folders and recreate the folder structure locally. Each email is going to be placed in it's respective folder.

## Help
```
Description:
  Download all emails your emails from any IMAP server

Usage:
  maildown [options]

Options:
  -s <imap.example.com> (REQUIRED)  Domain of the IMAP server
  --port <port>                     Port of the IMAP server [default: 993]
  -u <user@example.com> (REQUIRED)  IMAP username (usually the email address)
  -p <password> (REQUIRED)          IMAP password
  -o <path>                         Output path where emails should be saved to [default: .]
  --length <n>                      Maximum file name length [default: 0]
  --nospaces                        Replace spaces in filenames with underscores
  --version                         Show version information
  -?, -h, --help                    Show help and usage information
```
