# Smart Backup

A modular backup application which supports many of the widely used software products designed for portability and ease of use.

SmartBackup lets you configure your backup jobs entirely with a json schema. 
## Usage

Just run smartbackup.exe to start all your backup operations. You can see additional features with --help parameter

```bash
SmartBackup.exe --help
```

Be sure to set up your back up tasks by editing config.json file on your favourite text editor with json schema capability. (Not Notepad, Use an advanced editor like Visual Studio)

Use "$schema": "config-schema.json" directive in your config file so your editor helps you with configuration as you type.

```json
{
  "$schema": "config-schema.json",
  "ErrorActionPreference": "Stop",
  "BackupPath": "D:\\BACKUP\\",

  "Items": [
    {
      "Type": "File",
      "Path": "d:\\bacup",
      "CompressionLevel": 1,
      "Name": "File Backup",
      "Skip": ["sil"]
    },
    {
      "Type": "MongoDb",
      "Host": "localhost",
      "Port": 27017,
      "MongoDbDir": "X:\\MongoDB\\Server\\4.0\\bin",
      "Database": "test",
      "BackupPath": "d:\\backup\\",
      "Username": "admin",
      "Password": "*******"
    },
    {
      "Type": "HyperV",
      "VM": "TestVM",
      "Server": "localhost",
    },
    {
      "Type": "MySQL",
      "Database": "imon",
      "Server": "localhost",
      "Username": "root",
      "Password": "",
      "BackupPath": "d:\\backup\\",
      "CloseProcesses": [ "win32calc.exe" ]
    },
    {
      "Type": "MSSQL",
      "Server": "(local)",
      "Database": "MyProjectDb",
      "BackupType": "Incremental",
      "Name": "MSSQL Backup",
      "Username": "sa",
      "Password": "******"
    }
  ],

  "CompressionLevel": 3,
  "Password": "myglobalpassword",
  "SMTP": {
    "Server": "mail.mymail.com",
    "UseSSL": true,
    "Port": 4650,
    "FromName": "John",
    "From": "backup@mailserver.com",
    "To": "backup@mymail.com",
    "Username": "backup@mymail.com",
    "Password": "mymailpassword"
  },
  "PreKillProcesses": [
    "calc.exe"
  ] 
}

```

You can see explanations of parameters on mouseover.

Just execute SmartBackup.exe when your config file is ready.

## Author

| [![twitter/aytekustundag](https://gravatar.com/avatar/06cc721135e469d84995ec9bf550c645?s=70)](https://twitter.com/aytekustundag "Follow @aytekustundag on Twitter") |
|---|
| [Aytek Ustundag](https://www.aytekustundag.com/) |

