using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;

namespace AutomacaoVersaoTask
{
    public class ProcessUtils
    {
        private const string NOME_SOLUCAO = ""; // Nome da Solução do projeto (Visual Studio)
        private const string VISUAL_STUDIO_19 = "VisualStudio.DTE.16.0";

        public static string CleanAndBuildSolution(VersionTaskType versionTaskType = VersionTaskType.CompleteVersion)
        {
            var processoVS = Process.GetProcessesByName("devenv");
            Process processoTaskToDo = processoVS.Where(w => w.MainWindowTitle.Contains(NOME_SOLUCAO)).FirstOrDefault();

            if (processoTaskToDo != null)
            {
                EnvDTE.DTE dte = GetSolutionTaskToDo(processoTaskToDo.Id);
                SelectTypeBuild(dte.Solution.SolutionBuild, versionTaskType);

                Thread.Sleep(1000);

                dte.Solution.SolutionBuild.Clean(true);
                dte.Solution.SolutionBuild.Build(true);

                if (dte.Solution.SolutionBuild.LastBuildInfo != 0)
                {
                    Console.WriteLine("O build falhou.");
                    throw new Exception("O build falhou.");
                }

                // Return to Debug After Build Release Version
                if (versionTaskType == VersionTaskType.OficialVersion)
                    SelectTypeBuild(dte.Solution.SolutionBuild, VersionTaskType.CompleteVersion);

                return dte.Solution.FullName;
            }

            Console.WriteLine($"Processo do {NOME_SOLUCAO} não encontrado");
            throw new Exception($"Processo do {NOME_SOLUCAO} não encontrado");
        }

        public static string GetNomeBranch(string workingDirectory)
        {
            string command = "git branch --show-current";
            return ExecutarComandoPowerShell(command, workingDirectory);
        }

        public static string GetVersaoTask(string workingDirectory)
        {
            string commandUpdateTags = "git pull --tags";
            ExecutarComandoPowerShell(commandUpdateTags, workingDirectory);

            string command = "git for-each-ref --sort=-creatordate --format '%(refname:short)' refs/tags | Select-Object -First 1";
            return ExecutarComandoPowerShell(command, workingDirectory);
        }


        private static void SelectTypeBuild(EnvDTE.SolutionBuild solutionBuild, VersionTaskType versionTaskType)
        {
            string configurationName = versionTaskType == VersionTaskType.OficialVersion ? "Release" : "Debug";

            if (solutionBuild.ActiveConfiguration.Name != configurationName)
            {
                foreach (EnvDTE.SolutionConfiguration config in solutionBuild.SolutionConfigurations)
                {
                    if (config.Name == configurationName)
                    {
                        config.Activate();
                        break;
                    }
                }
            }
        }

        private static string ExecutarComandoPowerShell(string command, string workingDirectory)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-Command \"{command}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                WorkingDirectory = workingDirectory,
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();
                process.WaitForExit();

                if (process.ExitCode != 0 && !string.IsNullOrEmpty(process.StandardError.ReadToEnd().Trim()))
                {
                    Console.WriteLine($"Erro: {process.StandardError.ReadToEnd().Trim()}");
                    throw new Exception($"Erro: {process.StandardError.ReadToEnd().Trim()}");
                }

                return process.StandardOutput.ReadToEnd().Trim();
            }
        }

        private static EnvDTE.DTE GetSolutionTaskToDo(int processId)
        {
            string runningObjectDisplayName = $"{VISUAL_STUDIO_19}:{processId}"; // Visual Studio 19

            IEnumerable<string> runningObjectDisplayNames = null;
            object runningObject = null;
            for (int i = 0; i < 60; i++)
            {
                try
                {
                    runningObject = GetRunningObject(runningObjectDisplayName, out runningObjectDisplayNames);
                }
                catch
                {
                    runningObject = null;
                }

                if (runningObject != null)
                {
                    return (EnvDTE.DTE)runningObject;
                }

                Thread.Sleep(millisecondsTimeout: 1000);
            }

            Console.WriteLine($"Failed to retrieve DTE object. Current running objects: {string.Join(";", runningObjectDisplayNames)}");
            throw new TimeoutException($"Failed to retrieve DTE object. Current running objects: {string.Join(";", runningObjectDisplayNames)}");
        }

        private static object GetRunningObject(string displayName, out IEnumerable<string> runningObjectDisplayNames)
        {
            IBindCtx bindContext = null;
            NativeMethods.CreateBindCtx(0, out bindContext);

            IRunningObjectTable runningObjectTable = null;
            bindContext.GetRunningObjectTable(out runningObjectTable);

            IEnumMoniker monikerEnumerator = null;
            runningObjectTable.EnumRunning(out monikerEnumerator);

            object runningObject = null;
            List<string> runningObjectDisplayNameList = new List<string>();
            IMoniker[] monikers = new IMoniker[1];
            IntPtr numberFetched = IntPtr.Zero;
            while (monikerEnumerator.Next(1, monikers, numberFetched) == 0)
            {
                IMoniker moniker = monikers[0];

                string objectDisplayName = null;
                try
                {
                    moniker.GetDisplayName(bindContext, null, out objectDisplayName);
                }
                catch (UnauthorizedAccessException)
                {
                    // Some ROT objects require elevated permissions.
                }

                if (!string.IsNullOrWhiteSpace(objectDisplayName))
                {
                    runningObjectDisplayNameList.Add(objectDisplayName);
                    if (objectDisplayName.EndsWith(displayName, StringComparison.Ordinal))
                    {
                        runningObjectTable.GetObject(moniker, out runningObject);
                        if (runningObject == null)
                        {
                            throw new InvalidOperationException($"Failed to get running object with display name {displayName}");
                        }
                    }
                }
            }

            runningObjectDisplayNames = runningObjectDisplayNameList;
            return runningObject;
        }

        private static class NativeMethods
        {
            [DllImport("ole32.dll")]
            public static extern int CreateBindCtx(uint reserved, out IBindCtx ppbc);
        }

        public static void IniciarProcesso(string filePath, string[] args, string outputDirectory)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = filePath,
                Arguments = string.Join(" ", args),
                WorkingDirectory = outputDirectory,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();
                process.WaitForExit();

                if (process.ExitCode != 0 && !string.IsNullOrEmpty(process.StandardError.ReadToEnd().Trim()))
                {
                    Console.WriteLine($"Erro: {process.StandardError.ReadToEnd().Trim()}");
                    throw new Exception($"Erro: {process.StandardError.ReadToEnd().Trim()}");
                }
            }
        }
    }
}
