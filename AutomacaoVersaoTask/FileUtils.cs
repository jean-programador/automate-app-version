using System;
using System.IO;
using System.Linq;

namespace AutomacaoVersaoTask
{
    public class FileUtils
    {
        private const string ORIGIN_FOLDER_TASK = @""; // Caminho de origem do projeto principal
        private const string ORIGIN_FOLDER_BRAIN = @""; // Caminho de origem serviço windows
        private const string DESTINATION_FOLDER_TEST = @""; // Caminho de destino da versão de TESTE
        private const string DESTINATION_FOLDER_OFICIAL = @""; // Caminho de destino da versão de OFICIAL 
        private const string WINRAR_PATH = @"C:\Program Files\WinRAR\WinRAR.exe";
        private const string SEVEN_ZIP_PATH = @"C:\Program Files\7-Zip\7z.exe";
        private const string NOME_PROJETO_PRINCIPAL = "";
        private const string NOME_SERVICO_WINDOWS = "";
        private const string CONFIG_FILE_TASK = ""; // Nome do arquivo de configuração do projeto principal
        private const string CONFIG_FILE_BRAIN = ""; // Nome do arquivo de configuração do serviço windows
        private const string NOME_INICIAL_DLLS_NECESSARIAS = ""; // Na versão compacta, as dlls necessárias iniciam sempre com o mesmo nome
        private const string NOME_ARQUIVO_EXE = "";

        public static UploadOptions OficialVersion(string folderName)
        {
            string destinationFolder = Path.Combine(DESTINATION_FOLDER_OFICIAL, folderName);
            string fileRAR = Path.GetFileName($"{destinationFolder}(123).rar");

            GenerateCompleteVersion(Path.Combine(destinationFolder, NOME_PROJETO_PRINCIPAL), ORIGIN_FOLDER_TASK.Replace("Debug", "Release"), brainService: false);
            GenerateCompleteVersion(Path.Combine(destinationFolder, NOME_SERVICO_WINDOWS), ORIGIN_FOLDER_BRAIN.Replace("Debug", "Release"), brainService: true);

            UploadOptions uploadOptions = new UploadOptions
            {
                FileName = fileRAR,
                FilePath = destinationFolder,
                VersionType = VersionTaskType.OficialVersion
            };

            CompactVersionFiles(uploadOptions);
            Directory.Delete(destinationFolder, true);

            uploadOptions.FilePath = Path.Combine(DESTINATION_FOLDER_TEST, fileRAR);

            return uploadOptions;
        }

        public static UploadOptions TestVersionComplete(string folderName, bool includeBrainService)
        {
            string destinationFolder = Path.Combine(DESTINATION_FOLDER_TEST, folderName);
            string fileRAR = Path.GetFileName($"{destinationFolder}.rar");

            GenerateCompleteVersion(Path.Combine(destinationFolder, NOME_PROJETO_PRINCIPAL), ORIGIN_FOLDER_TASK, brainService: false);
            if(includeBrainService)
                GenerateCompleteVersion(Path.Combine(destinationFolder, NOME_SERVICO_WINDOWS), ORIGIN_FOLDER_BRAIN, brainService: true);

            UploadOptions uploadOptions = new UploadOptions
            {
                FileName = fileRAR,
                FilePath = destinationFolder,
                VersionType = includeBrainService ? VersionTaskType.CompleteVersion : VersionTaskType.CompleteTaskVersion
            };

            CompactVersionFiles(uploadOptions);
            Directory.Delete(destinationFolder, true);

            uploadOptions.FilePath = Path.Combine(DESTINATION_FOLDER_TEST, fileRAR);

            return uploadOptions;
        }

        public static UploadOptions TestVersionCompact(string folderName, bool includeBrainService)
        {
            string destinationFolder = Path.Combine(DESTINATION_FOLDER_TEST, folderName);
            string fileRAR = Path.GetFileName($"{destinationFolder}.rar");

            GenerateCompactVersion(Path.Combine(destinationFolder, NOME_PROJETO_PRINCIPAL), ORIGIN_FOLDER_TASK);
            if (includeBrainService)
                GenerateCompleteVersion(Path.Combine(destinationFolder, NOME_SERVICO_WINDOWS), ORIGIN_FOLDER_BRAIN, brainService: true);

            UploadOptions uploadOptions = new UploadOptions
            {
                FileName = fileRAR,
                FilePath = destinationFolder,
                VersionType = includeBrainService ? VersionTaskType.CompactTaskCompleteBrainVersion : VersionTaskType.CompactTaskVersion
            };

            CompactVersionFiles(uploadOptions);
            Directory.Delete(destinationFolder, true);

            uploadOptions.FilePath = Path.Combine(DESTINATION_FOLDER_TEST, fileRAR);

            return uploadOptions;
        }

        private static void GenerateCompleteVersion(string destinationFolder, string originFolder, bool brainService)
        {
            string destinationFolderDlls = Path.Combine(destinationFolder, "dlls");
            string configName = brainService ? CONFIG_FILE_BRAIN : CONFIG_FILE_TASK;

            var files = Directory.GetFiles(originFolder, "*", SearchOption.AllDirectories);

            var dlls = files.Where(f => Path.GetFileName(f).ToLower() != configName);
            var configFile = files.FirstOrDefault(f => Path.GetFileName(f).ToLower() == configName);

            if (!Directory.Exists(destinationFolderDlls))
            {
                Directory.CreateDirectory(destinationFolderDlls);
            }

            foreach (var dll in dlls)
            {
                string dllFileName = dll.Replace(originFolder, "").TrimStart('\\');
                string destinationPath = Path.Combine(destinationFolderDlls, dllFileName);

                if (!Directory.Exists(Path.GetDirectoryName(destinationPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                }

                File.Copy(dll, destinationPath, true);
            }

            if (configFile != null)
            {
                string destinationConfigPath = Path.Combine(destinationFolder, Path.GetFileName(configFile));
                File.Copy(configFile, destinationConfigPath, true);
            }
        }

        private static void GenerateCompactVersion(string destinationFolder, string originFolder)
        {
            var files = Directory.GetFiles(originFolder, "*", SearchOption.AllDirectories);

            var dlls = files.Where(f =>
                (Path.GetFileName(f).ToLower().Contains(NOME_INICIAL_DLLS_NECESSARIAS) && Path.GetExtension(f) == ".dll") ||
                (Path.GetFileName(f).ToLower() == NOME_ARQUIVO_EXE)
            );

            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }

            foreach (var dll in dlls)
            {
                File.Copy(dll, Path.Combine(destinationFolder, Path.GetFileName(dll)), true);
            }
        }

        private static void CompactVersionFiles(UploadOptions uploadOptions)
        {
            try
            {
                string outputDirectory = uploadOptions.VersionType == VersionTaskType.OficialVersion ? DESTINATION_FOLDER_OFICIAL : DESTINATION_FOLDER_TEST;
                if (File.Exists(WINRAR_PATH))
                {
                    if (File.Exists($"{uploadOptions.FilePath}.rar"))
                        File.Delete($"{uploadOptions.FilePath}.rar");

                    string[] args;
                    if (uploadOptions.VersionType == VersionTaskType.OficialVersion)
                        args = new string[] { "a", "-o+", "-p123", "-ep1", "-m5", uploadOptions.FileName, uploadOptions.FilePath };
                    else
                        args = new string[] { "a", "-o+", "-ep1", "-m5", uploadOptions.FileName, uploadOptions.FilePath };

                    ProcessUtils.IniciarProcesso(WINRAR_PATH, args, outputDirectory);
                }
                else if (File.Exists(SEVEN_ZIP_PATH))
                {
                    if (File.Exists($"{uploadOptions.FilePath}.zip"))
                        File.Delete($"{uploadOptions.FilePath}.zip");

                    var fileName = uploadOptions.FileName.Replace(".rar", ".zip");
                    string[] args;
                    if (uploadOptions.VersionType == VersionTaskType.OficialVersion)
                        args = new string[] { "a", "-aoa", "-tzip", "-p123", fileName, uploadOptions.FilePath };
                    else
                        args = new string[] { "a", "-aoa", "-tzip", fileName, uploadOptions.FilePath };

                    ProcessUtils.IniciarProcesso(SEVEN_ZIP_PATH, args, outputDirectory);
                } else
                {
                    Console.WriteLine("Erro ao compactar arquivos, verifique se possuí um dos seguintes programas instalados: '7Zip', 'WinRAR'");
                    throw new Exception("Erro ao compactar arquivos, verifique se possuí um dos seguintes programas instalados: '7Zip', 'WinRAR'");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Erro ao compactar arquivos: {e.Message}");
                throw new Exception($"Erro ao compactar arquivos: {e.Message}");
            }
        }
    }
}
