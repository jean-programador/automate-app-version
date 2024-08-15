using System;
using System.IO;
using System.Linq;

namespace AutomacaoVersaoTask
{
    public class FileUtils
    {
        private static readonly string _OriginFolderTask = @"C:\PilarSistemas\TaskToDo\TaskTodoAutomation\TaskToDo\bin\Debug";
        private static readonly string _OriginFolderBrain = @"C:\PilarSistemas\TaskToDo\TaskToDoBrainService\TaskToDoBrainService\bin\Debug";
        private static readonly string _DestinationFolderTest = @"C:\Users\Dev\Downloads\versoes\VersoesTeste";
        private static readonly string _DestinationFolderOficial = @"C:\Users\Dev\Downloads\versoes\VersoesOficiais";
        private static readonly string _WinrarPath = @"C:\Program Files\WinRAR\WinRAR.exe";
        private static readonly string _SevenZipPath = @"C:\Program Files\7-Zip\7z.exe";

        public static UploadOptions OficialVersion(string folderName)
        {
            string destinationFolder = Path.Combine(_DestinationFolderOficial, folderName);
            string fileRAR = Path.GetFileName($"{destinationFolder}(123).rar");

            GenerateCompleteVersion(Path.Combine(destinationFolder, "TaskToDo"), _OriginFolderTask.Replace("Debug", "Release"), brainService: false);
            GenerateCompleteVersion(Path.Combine(destinationFolder, "BrainService"), _OriginFolderBrain.Replace("Debug", "Release"), brainService: true);

            UploadOptions uploadOptions = new UploadOptions
            {
                FileName = fileRAR,
                FilePath = destinationFolder,
                VersionType = VersionTaskType.OficialVersion
            };

            CompactVersionFiles(uploadOptions);
            Directory.Delete(destinationFolder, true);

            uploadOptions.FilePath = Path.Combine(_DestinationFolderOficial, fileRAR);

            return uploadOptions;
        }

        public static UploadOptions TestVersionComplete(string folderName, bool includeBrainService)
        {
            string destinationFolder = Path.Combine(_DestinationFolderTest, folderName);
            string fileRAR = Path.GetFileName($"{destinationFolder}.rar");

            GenerateCompleteVersion(Path.Combine(destinationFolder, "TaskToDo"), _OriginFolderTask, brainService: false);
            if(includeBrainService)
                GenerateCompleteVersion(Path.Combine(destinationFolder, "BrainService"), _OriginFolderBrain, brainService: true);

            UploadOptions uploadOptions = new UploadOptions
            {
                FileName = fileRAR,
                FilePath = destinationFolder,
                VersionType = includeBrainService ? VersionTaskType.CompleteVersion : VersionTaskType.CompleteTaskVersion
            };

            CompactVersionFiles(uploadOptions);
            Directory.Delete(destinationFolder, true);

            uploadOptions.FilePath = Path.Combine(_DestinationFolderTest, fileRAR);

            return uploadOptions;
        }

        public static UploadOptions TestVersionCompact(string folderName, bool includeBrainService)
        {
            string destinationFolder = Path.Combine(_DestinationFolderTest, folderName);
            string fileRAR = Path.GetFileName($"{destinationFolder}.rar");

            GenerateCompactVersion(Path.Combine(destinationFolder, "TaskToDo"), _OriginFolderTask);
            if (includeBrainService)
                GenerateCompleteVersion(Path.Combine(destinationFolder, "BrainService"), _OriginFolderBrain, brainService: true);

            UploadOptions uploadOptions = new UploadOptions
            {
                FileName = fileRAR,
                FilePath = destinationFolder,
                VersionType = includeBrainService ? VersionTaskType.CompactTaskCompleteBrainVersion : VersionTaskType.CompactTaskVersion
            };

            CompactVersionFiles(uploadOptions);
            Directory.Delete(destinationFolder, true);

            uploadOptions.FilePath = Path.Combine(_DestinationFolderTest, fileRAR);

            return uploadOptions;
        }

        private static void GenerateCompleteVersion(string destinationFolder, string originFolder, bool brainService)
        {
            string destinationFolderDlls = Path.Combine(destinationFolder, "dlls");
            string configName = brainService ? "tasktodobrainservice.exe.config" : "tasktodo.exe.config";

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
                (Path.GetFileName(f).ToLower().Contains("tasktodoautomation") && Path.GetExtension(f) == ".dll") ||
                (Path.GetFileName(f).ToLower() == "tasktodo.exe")
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
                string outputDirectory = uploadOptions.VersionType == VersionTaskType.OficialVersion ? _DestinationFolderOficial : _DestinationFolderTest;
                if (File.Exists(_WinrarPath))
                {
                    if (File.Exists($"{uploadOptions.FilePath}.rar"))
                        File.Delete($"{uploadOptions.FilePath}.rar");

                    string[] args;
                    if (uploadOptions.VersionType == VersionTaskType.OficialVersion)
                        args = new string[] { "a", "-o+", "-p123", "-ep1", "-m5", uploadOptions.FileName, uploadOptions.FilePath };
                    else
                        args = new string[] { "a", "-o+", "-ep1", "-m5", uploadOptions.FileName, uploadOptions.FilePath };

                    ProcessUtils.IniciarProcesso(_WinrarPath, args, outputDirectory);
                }
                else if (File.Exists(_SevenZipPath))
                {
                    if (File.Exists($"{uploadOptions.FilePath}.zip"))
                        File.Delete($"{uploadOptions.FilePath}.zip");

                    var fileName = uploadOptions.FileName.Replace(".rar", ".zip");
                    string[] args;
                    if (uploadOptions.VersionType == VersionTaskType.OficialVersion)
                        args = new string[] { "a", "-aoa", "-tzip", "-p123", fileName, uploadOptions.FilePath };
                    else
                        args = new string[] { "a", "-aoa", "-tzip", fileName, uploadOptions.FilePath };

                    ProcessUtils.IniciarProcesso(_SevenZipPath, args, outputDirectory);
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
