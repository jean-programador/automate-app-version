using System;
using System.Threading.Tasks;

namespace AutomacaoVersaoTask
{
    class AutomacaoVersaoTask
    {
        static void Main(string[] args)
        {
            string option = GetOption();
            Console.WriteLine("\n");

            while (!Enum.IsDefined(typeof(VersionTaskType), option))
            {
                switch (option)
                {
                    case "1":
                        GenerateOficialVersion().GetAwaiter().GetResult();
                        break;
                    case "2":
                        GenerateCompleteTestVersion(VersionTaskType.CompleteVersion).GetAwaiter().GetResult();
                        break;
                    case "3":
                        GenerateCompleteTestVersion(VersionTaskType.CompleteTaskVersion).GetAwaiter().GetResult();
                        break;
                    case "4":
                        GenerateCompactTestVersion(VersionTaskType.CompactTaskVersion).GetAwaiter().GetResult();
                        break;
                    case "5":
                        GenerateCompactTestVersion(VersionTaskType.CompactTaskCompleteBrainVersion).GetAwaiter().GetResult();
                        break;
                    default:
                        Console.WriteLine("Opção Inválida!");
                        break;
                }

                Console.WriteLine("\nVersão Gerada com sucesso!");
                Console.WriteLine("\n");
                option = GetOption();
            }  
        }

        private static string GetOption()
        {
            Console.WriteLine("Digite o número da opção escolhida:");
            Console.WriteLine("1 - Versão Oficial");
            Console.WriteLine("2 - Versão Teste Completa (Task + Brain)");
            Console.WriteLine("3 - Versão (APENAS TASK) Completa");
            Console.WriteLine("4 - Versão (APENAS TASK) Reduzida");
            Console.WriteLine("5 - Versão TASK Reduzida + Brain");
            return Console.ReadLine();
        }

        public async static Task GenerateOficialVersion()
        {
            Console.WriteLine("Iniciando Complilação da Solução....");
            var solutionName = ProcessUtils.CleanAndBuildSolution(VersionTaskType.OficialVersion);
            Console.WriteLine("Solução Compilada com Sucesso!");

            var workingDirectory = solutionName.Replace("\\TaskToDo.sln", "");
            string taskVersion = ProcessUtils.GetVersaoTask(workingDirectory);

            Console.WriteLine("Iniciando Processo de Cópia e Compactação....");
            UploadOptions uploadOptions = FileUtils.OficialVersion(taskVersion);
            Console.WriteLine($"Arquivos Copiados e Compactados com Sucesso na Pasta: {uploadOptions.FilePath}");

            Console.WriteLine("Iniciando Processo de Upload....");
            await DriveV3Snippets.DriveUploadBasic(uploadOptions);
        }

        public async static Task GenerateCompleteTestVersion(VersionTaskType versionTaskType)
        {
            Console.WriteLine("Iniciando Complilação da Solução....");
            var solutionName = ProcessUtils.CleanAndBuildSolution();
            Console.WriteLine("Solução Compilada com Sucesso!");

            var workingDirectory = solutionName.Replace("\\TaskToDo.sln", "");
            string taskVersion = ProcessUtils.GetNomeBranch(workingDirectory);

            Console.WriteLine("Iniciando Processo de Cópia e Compactação....");
            UploadOptions uploadOptions = FileUtils.TestVersionComplete(taskVersion, versionTaskType == VersionTaskType.CompleteVersion);
            Console.WriteLine($"Arquivos Copiados e Compactados com Sucesso na Pasta: {uploadOptions.FilePath}");

            Console.WriteLine("Iniciando Processo de Upload....");
            await DriveV3Snippets.DriveUploadBasic(uploadOptions);
        }

        public async static Task GenerateCompactTestVersion(VersionTaskType versionTaskType)
        {
            Console.WriteLine("Iniciando Complilação da Solução....");
            var solutionName = ProcessUtils.CleanAndBuildSolution();
            Console.WriteLine("Solução Compilada com Sucesso");

            var workingDirectory = solutionName.Replace("\\TaskToDo.sln", "");
            string taskVersion = ProcessUtils.GetNomeBranch(workingDirectory);

            Console.WriteLine("Iniciando Processo de Cópia e Compactação....");
            UploadOptions uploadOptions = FileUtils.TestVersionCompact(taskVersion, versionTaskType == VersionTaskType.CompactTaskCompleteBrainVersion);
            Console.WriteLine($"Arquivos Copiados e Compactados com Sucesso na Pasta: {uploadOptions.FilePath}");

            Console.WriteLine("Iniciando Processo de Upload....");
            await DriveV3Snippets.DriveUploadBasic(uploadOptions);
        }
    }
}
