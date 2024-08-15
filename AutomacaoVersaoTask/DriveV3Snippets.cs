using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace AutomacaoVersaoTask
{
    public class DriveV3Snippets
    {
        private static DriveService _DriverService = null;
        private static Regex _VersionDriveFolderNamePattern = new Regex(@"^v(\d+)\.(\d+)\.(\d+)");
        private const string OFICIAL_FOLDER_NAME = "Versões de Liberação";
        private const string TESTE_FOLDER_NAME = "Versões de Teste";

        /// <summary>
        /// Upload new file.
        /// </summary>
        /// <param name="filePath">Image path to upload.</param>
        /// <returns>Inserted file metadata if successful, null otherwise.</returns>
        public static async Task<string> DriveUploadBasic(UploadOptions uploadOptions)
        {
            try
            {
                await Login();

                string destinationFolderId;
                if (uploadOptions.VersionType == VersionTaskType.OficialVersion)
                {
                    string parentFolderId = GetFolderID(OFICIAL_FOLDER_NAME);
                    string folderName = GetOficalFolderNameByPath(uploadOptions.FilePath);
                    destinationFolderId = GetFolderID(folderName, parentFolderId);
                } else
                {
                    destinationFolderId = GetFolderID(TESTE_FOLDER_NAME);
                }

                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = uploadOptions.FileName,
                    MimeType = MimeMapping.GetMimeMapping(uploadOptions.FilePath),
                    Parents = new List<string>() { destinationFolderId }
                };

                FilesResource.CreateMediaUpload request;
                using (var stream = new FileStream(uploadOptions.FilePath,
                           FileMode.Open))
                {
                    request = _DriverService.Files.Create(fileMetadata, stream, fileMetadata.MimeType);
                    UploadFile(request);
                }

                var file = request.ResponseBody;

                return file.Id;
            }
            catch (Exception e)
            {
                if (e is AggregateException)
                {
                    Console.WriteLine("Credential Not found");
                }
                else if (e is FileNotFoundException)
                {
                    Console.WriteLine("File not found");
                }
                else
                {
                    Console.WriteLine(e.Message);
                }
            }

            return null;
        }

        private static async Task Login()
        {
            UserCredential credential;
            using (var stream = new FileStream("client_secrets.json", FileMode.Open, FileAccess.Read))
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    new[] { DriveService.Scope.Drive, DriveService.Scope.DriveFile },
                    "user", CancellationToken.None, new FileDataStore("Drive.Store"));
            }

            _DriverService = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Drive API Snippets"
            });
        }

        private static string GetOficalFolderNameByPath(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            Match match = _VersionDriveFolderNamePattern.Match(fileName);

            if (!match.Success)
                throw new Exception("Erro ao obter nome do arquivo compactado");

           return match.Value;
        }

        private static void UploadFile(FilesResource.CreateMediaUpload request)
        {
            request.Fields = "id";
            int totalBytes = (int)request.ContentStream.Length; // Obter o tamanho total do arquivo
            int lastProgress = 0; // Armazenar o último progresso para evitar impressão repetida

            request.ProgressChanged += (IUploadProgress progress) =>
            {
                if (progress.Exception != null)
                {
                    throw new Exception($"Erro durante o upload: {progress.Exception.Message}");
                }

                int currentProgress = (int)(progress.BytesSent * 100 / totalBytes);

                // Verificar se houve mudança no progresso para evitar impressão repetida
                if (currentProgress > lastProgress)
                {
                    lastProgress = currentProgress;
                    Console.Write($"\r[{new string('#', currentProgress / 2)}{new string(' ', 50 - currentProgress / 2)}] {currentProgress}%");
                }
            };

            request.Upload();
        }

        private static string GetFolderID(string folderName, string parentFolderId = null)
        {
            string query = $"mimeType='application/vnd.google-apps.folder' and name='{folderName}'";
            if (!string.IsNullOrEmpty(parentFolderId))
            {
                query += $" and '{parentFolderId}' in parents";
            }

            var request = _DriverService.Files.List();
            request.Q = query;
            request.Spaces = "drive";
            request.Fields = "nextPageToken, files(id, name)";

            var results = request.Execute();

            if (results.Files == null || results.Files.Count == 0)
            {
                return CreateFolder(folderName);
            }

            return results.Files[0].Id;
        }

        private static string CreateFolder(string folderName)
        {
            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = folderName,
                MimeType = "application/vnd.google-apps.folder"
            };

            var request = _DriverService.Files.Create(fileMetadata);
            request.Fields = "id";
            var file = request.Execute();

            Console.WriteLine("Pasta criada com Sucesso: " + file.Name);

            return file.Id;
        }
    }
}