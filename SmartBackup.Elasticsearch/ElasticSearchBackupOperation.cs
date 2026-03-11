using SmartBackup.Common;
using SmartBackup.Model;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SmartBackup.Elasticsearch
{
    [BackupModule(Type = "ElasticSearch", ConfigurationType = typeof(ElasticSearchInfo))]
    public class ElasticSearchBackupOperation : BackupOperation
    {
        private string? generatedSnapshotName;

        public ElasticSearchInfo Info { get { return (ElasticSearchInfo)Data; } set { Data = value; } }

        public override bool EncryptionSupported { get { return false; } }

        public override void Configure(object o)
        {
            JsonElement jitem = (JsonElement)o;
            Info = jitem.Deserialize<ElasticSearchInfo>(SerializationOptions);
        }

        public override async Task BackupAsync()
        {
            string snapshotName = GetSnapshotName();
            string endpoint = BuildSnapshotEndpoint(snapshotName);
            string backupMetadataPath = Path.Combine(Info.BackupPath, GetBackupFileName());

            using HttpClientHandler handler = new HttpClientHandler();
            if (Info.IgnoreCertificateErrors == true)
            {
                handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
            }

            using HttpClient client = new HttpClient(handler);
            AddAuthorization(client);

            var payload = BuildSnapshotPayload();
            string payloadJson = JsonSerializer.Serialize(payload);

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, endpoint);
            request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

            ReportProgress(0);
            HttpResponseMessage response = await client.SendAsync(request);
            string responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Elasticsearch snapshot request failed ({(int)response.StatusCode} {response.ReasonPhrase}). Response: {responseText}");
            }

            await File.WriteAllTextAsync(backupMetadataPath, responseText);
            ReportProgress(100);
        }

        public override string GetBackupFileName()
        {
            string repository = BackupOperation.GenerateSlug(Info.SnapshotRepository);
            string snapshot = BackupOperation.GenerateSlug(GetSnapshotName());
            return $"{repository}-{snapshot}.snapshot.json";
        }

        private string GetSnapshotName()
        {
            if (!string.IsNullOrWhiteSpace(Info.SnapshotName))
            {
                return Info.SnapshotName;
            }

            if (!string.IsNullOrWhiteSpace(generatedSnapshotName))
            {
                return generatedSnapshotName;
            }

            string prefix = "smartbackup";
            generatedSnapshotName = $"{prefix}-{DateTime.UtcNow:yyyyMMddHHmmss}";
            return generatedSnapshotName;
        }

        private string BuildSnapshotEndpoint(string snapshotName)
        {
            string scheme = string.IsNullOrWhiteSpace(Info.Scheme) ? "http" : Info.Scheme.Trim();
            string host = string.IsNullOrWhiteSpace(Info.Host) ? "localhost" : Info.Host.Trim();
            int port = Info.Port ?? 9200;

            string prefix = string.Empty;
            if (!string.IsNullOrWhiteSpace(Info.PathPrefix))
            {
                prefix = "/" + Info.PathPrefix.Trim('/');
            }

            string repository = Uri.EscapeDataString(Info.SnapshotRepository);
            string snapshot = Uri.EscapeDataString(snapshotName);

            return $"{scheme}://{host}:{port}{prefix}/_snapshot/{repository}/{snapshot}?wait_for_completion=true";
        }

        private Dictionary<string, object> BuildSnapshotPayload()
        {
            Dictionary<string, object> payload = new Dictionary<string, object>();

            if (Info.Indices != null)
            {
                string[] selectedIndices = Info.Indices
                    .Where(i => !string.IsNullOrWhiteSpace(i))
                    .Select(i => i.Trim())
                    .ToArray();

                if (selectedIndices.Length > 0)
                {
                    payload["indices"] = string.Join(",", selectedIndices);
                }
            }

            if (Info.IncludeGlobalState.HasValue)
            {
                payload["include_global_state"] = Info.IncludeGlobalState.Value;
            }

            return payload;
        }

        private void AddAuthorization(HttpClient client)
        {
            if (!string.IsNullOrWhiteSpace(Info.ApiKey))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("ApiKey", Info.ApiKey.Trim());
                return;
            }

            if (!string.IsNullOrWhiteSpace(Info.Username))
            {
                string raw = $"{Info.Username}:{Info.Password ?? string.Empty}";
                string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encoded);
            }
        }
    }

    [BackupModule(Type = "Elasticsearch", ConfigurationType = typeof(ElasticSearchInfo))]
    public class ElasticsearchBackupOperation : ElasticSearchBackupOperation
    {
    }
}
