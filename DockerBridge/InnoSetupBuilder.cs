using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace DockerDeploy
{
    // Inno Setup安装配置
    public class InnoSetupConfig
    {
        public string AppName { get; set; } = "My Application";
        public string AppVersion { get; set; } = "1.0.0";
        public string Publisher { get; set; } = "My Company";
        public string AppPublisherUrl { get; set; } = "https://example.com";
        public string AppSupportUrl { get; set; } = "https://support.example.com";
        public string AppUpdatesUrl { get; set; } = "https://updates.example.com";
        public string DefaultDirName { get; set; } = "{autopf}\\MyApp";
        public string DefaultGroupName { get; set; } = "My Application";
        public string OutputDir { get; set; } = "./Installer";
        public string OutputBaseFilename { get; set; } = "MyApp_Setup";
        public string SetupIconFile { get; set; } = string.Empty;
        public string LicenseFile { get; set; } = string.Empty;
        public string InfoBeforeFile { get; set; } = string.Empty;
        public string InfoAfterFile { get; set; } = string.Empty;
        public string Compression { get; set; } = "lzma2";
        public bool SolidCompression { get; set; } = true;
        public bool CreateUninstallIcon { get; set; } = true;
        public bool CreateDesktopIcon { get; set; } = true;
        public string WizardStyle { get; set; } = "modern";
        public List<string> Architectures { get; set; } = new() { "x64" };
        public Dictionary<string, string> RegistryEntries { get; set; } = new();
        public List<InstallFile> Files { get; set; } = new();
        public List<InstallRun> RunCommands { get; set; } = new();
        public List<InstallIcon> Icons { get; set; } = new();
        public List<CustomCode> CustomCodeSections { get; set; } = new();
        
        public class InstallFile
        {
            public string Source { get; set; } = string.Empty;
            public string DestDir { get; set; } = "{app}";
            public string Flags { get; set; } = "ignoreversion recursesubdirs createallsubdirs";
            public List<string> Excludes { get; set; } = new();
        }
        
        public class InstallRun
        {
            public string Filename { get; set; } = string.Empty;
            public string Parameters { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Flags { get; set; } = "postinstall nowait skipifsilent";
            public bool CheckReturnCode { get; set; } = false;
        }
        
        public class InstallIcon
        {
            public string Name { get; set; } = string.Empty;
            public string Filename { get; set; } = string.Empty;
            public string IconName { get; set; } = string.Empty;
            public string Parameters { get; set; } = string.Empty;
            public string WorkingDir { get; set; } = string.Empty;
            public string IconFilename { get; set; } = string.Empty;
            public int IconIndex { get; set; } = 0;
        }
        
        public class CustomCode
        {
            public string Section { get; set; } = string.Empty; // [Code], [Setup], etc.
            public string Code { get; set; } = string.Empty;
        }
    }

    // Inno Setup编译器接口
    public interface IInnoSetupBuilder : IDisposable
    {
        Task<bool> CheckInnoSetupInstallationAsync(CancellationToken ct = default);
        Task<string> GenerateScriptAsync(InnoSetupConfig config, CancellationToken ct = default);
        Task<string> CompileInstallerAsync(string scriptPath, CancellationToken ct = default);
        Task<string> BuildInstallerAsync(InnoSetupConfig config, string outputPath = "", 
            CancellationToken ct = default);
        Task<bool> ValidateScriptAsync(string scriptPath, CancellationToken ct = default);
        Task<List<string>> GetCompilerVersionsAsync(CancellationToken ct = default);
        Task<string> SignInstallerAsync(string installerPath, string certificatePath, 
            string password, CancellationToken ct = default);
    }

    // Inno Setup编译器实现
    public class InnoSetupBuilder : IInnoSetupBuilder
    {
        private readonly ILogger<InnoSetupBuilder> _logger;
        private readonly string _innoSetupPath;
        private bool _disposed;
        
        public InnoSetupBuilder(ILogger<InnoSetupBuilder> logger = null)
        {
            _logger = logger;
            _innoSetupPath = FindInnoSetupPath();
        }
        
        private string FindInnoSetupPath()
        {
            // 常见安装路径
            var possiblePaths = new[]
            {
                @"C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
                @"C:\Program Files\Inno Setup 6\ISCC.exe",
                @"C:\Program Files (x86)\Inno Setup 5\ISCC.exe",
                @"C:\Program Files\Inno Setup 5\ISCC.exe",
                Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"),
                Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\Inno Setup 6\ISCC.exe")
            };
            
            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    _logger?.LogInformation($"Found Inno Setup at: {path}");
                    return path;
                }
            }
            
            // 检查PATH环境变量
            var pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(pathEnv))
            {
                foreach (var directory in pathEnv.Split(Path.PathSeparator))
                {
                    var candidate = Path.Combine(directory, "ISCC.exe");
                    if (File.Exists(candidate))
                    {
                        _logger?.LogInformation($"Found Inno Setup in PATH: {candidate}");
                        return candidate;
                    }
                }
            }
            
            _logger?.LogWarning("Inno Setup not found in standard locations");
            return string.Empty;
        }
        
        public async Task<bool> CheckInnoSetupInstallationAsync(CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrEmpty(_innoSetupPath))
                {
                    _logger?.LogError("Inno Setup not found");
                    return false;
                }
                
                // 运行编译器获取版本信息
                var result = await RunCompilerAsync("--version", ct);
                
                if (result.exitCode == 0)
                {
                    _logger?.LogInformation($"Inno Setup is installed: {result.output}");
                    return true;
                }
                else
                {
                    _logger?.LogError($"Inno Setup check failed: {result.error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking Inno Setup installation");
                return false;
            }
        }
        
        public async Task<string> GenerateScriptAsync(InnoSetupConfig config, CancellationToken ct = default)
        {
            try
            {
                _logger?.LogInformation($"Generating Inno Setup script for {config.AppName}");
                
                var scriptBuilder = new StringBuilder();
                
                // [Setup] 部分
                scriptBuilder.AppendLine("[Setup]");
                scriptBuilder.AppendLine($"AppName={config.AppName}");
                scriptBuilder.AppendLine($"AppVersion={config.AppVersion}");
                scriptBuilder.AppendLine($"AppPublisher={config.Publisher}");
                
                if (!string.IsNullOrEmpty(config.AppPublisherUrl))
                    scriptBuilder.AppendLine($"AppPublisherURL={config.AppPublisherUrl}");
                
                if (!string.IsNullOrEmpty(config.AppSupportUrl))
                    scriptBuilder.AppendLine($"AppSupportURL={config.AppSupportUrl}");
                
                if (!string.IsNullOrEmpty(config.AppUpdatesUrl))
                    scriptBuilder.AppendLine($"AppUpdatesURL={config.AppUpdatesUrl}");
                
                scriptBuilder.AppendLine($"DefaultDirName={config.DefaultDirName}");
                scriptBuilder.AppendLine($"DefaultGroupName={config.DefaultGroupName}");
                scriptBuilder.AppendLine($"OutputDir={config.OutputDir}");
                scriptBuilder.AppendLine($"OutputBaseFilename={config.OutputBaseFilename}");
                
                if (!string.IsNullOrEmpty(config.SetupIconFile))
                    scriptBuilder.AppendLine($"SetupIconFile={config.SetupIconFile}");
                
                if (!string.IsNullOrEmpty(config.LicenseFile))
                    scriptBuilder.AppendLine($"LicenseFile={config.LicenseFile}");
                
                if (!string.IsNullOrEmpty(config.InfoBeforeFile))
                    scriptBuilder.AppendLine($"InfoBeforeFile={config.InfoBeforeFile}");
                
                if (!string.IsNullOrEmpty(config.InfoAfterFile))
                    scriptBuilder.AppendLine($"InfoAfterFile={config.InfoAfterFile}");
                
                scriptBuilder.AppendLine($"Compression={config.Compression}");
                scriptBuilder.AppendLine($"SolidCompression={(config.SolidCompression ? "yes" : "no")}");
                scriptBuilder.AppendLine($"WizardStyle={config.WizardStyle}");
                
                if (config.Architectures.Count > 0)
                {
                    scriptBuilder.AppendLine($"ArchitecturesInstallIn64BitMode={string.Join(" ", config.Architectures)}");
                }
                
                scriptBuilder.AppendLine();
                
                // [Languages] 部分
                scriptBuilder.AppendLine("[Languages]");
                scriptBuilder.AppendLine(@"Name: ""english""; MessagesFile: ""compiler:Default.isl""");
                scriptBuilder.AppendLine();
                
                // [Tasks] 部分
                scriptBuilder.AppendLine("[Tasks]");
                scriptBuilder.AppendLine(@"Name: ""desktopicon""; Description: ""{cm:CreateDesktopIcon}""; ");
                scriptBuilder.AppendLine(@"  GroupDescription: ""{cm:AdditionalIcons}""; Flags: unchecked");
                scriptBuilder.AppendLine(@"Name: ""quicklaunchicon""; Description: ""{cm:CreateQuickLaunchIcon}""; ");
                scriptBuilder.AppendLine(@"  GroupDescription: ""{cm:AdditionalIcons}""; Flags: unchecked; OnlyBelowVersion: 0,6.1");
                scriptBuilder.AppendLine();
                
                // [Files] 部分
                scriptBuilder.AppendLine("[Files]");
                
                foreach (var file in config.Files)
                {
                    var source = file.Source;
                    var destDir = file.DestDir;
                    var flags = file.Flags;
                    
                    // 处理排除文件
                    if (file.Excludes.Count > 0)
                    {
                        foreach (var exclude in file.Excludes)
                        {
                            scriptBuilder.AppendLine($@"Source: ""{source}\{exclude}""; DestDir: ""{destDir}""; Flags: {flags} ignoreversion deleteafterinstall");
                        }
                    }
                    else
                    {
                        scriptBuilder.AppendLine($@"Source: ""{source}""; DestDir: ""{destDir}""; Flags: {flags}");
                    }
                }
                
                scriptBuilder.AppendLine();
                
                // [Icons] 部分
                scriptBuilder.AppendLine("[Icons]");
                
                // 开始菜单图标
                scriptBuilder.AppendLine($@"Name: ""{{group}}\{config.AppName}""; Filename: ""{{app}}\{GetMainExecutable(config)}""");
                
                if (config.CreateUninstallIcon)
                {
                    scriptBuilder.AppendLine(@"Name: ""{group}\{cm:UninstallProgram,My Program}""; Filename: ""{uninstallexe}""");
                }
                
                if (config.CreateDesktopIcon)
                {
                    scriptBuilder.AppendLine($@"Name: ""{{commondesktop}}\{config.AppName}""; Filename: ""{{app}}\{GetMainExecutable(config)}""; Tasks: desktopicon");
                }
                
                // 自定义图标
                foreach (var icon in config.Icons)
                {
                    var iconLine = $@"Name: ""{icon.Name}""; Filename: ""{icon.Filename}""";
                    
                    if (!string.IsNullOrEmpty(icon.Parameters))
                        iconLine += $@"; Parameters: ""{icon.Parameters}""";
                    
                    if (!string.IsNullOrEmpty(icon.WorkingDir))
                        iconLine += $@"; WorkingDir: ""{icon.WorkingDir}""";
                    
                    if (!string.IsNullOrEmpty(icon.IconFilename))
                        iconLine += $@"; IconFilename: ""{icon.IconFilename}""";
                    
                    if (icon.IconIndex != 0)
                        iconLine += $"; IconIndex: {icon.IconIndex}";
                    
                    scriptBuilder.AppendLine(iconLine);
                }
                
                scriptBuilder.AppendLine();
                
                // [Run] 部分
                if (config.RunCommands.Count > 0)
                {
                    scriptBuilder.AppendLine("[Run]");
                    
                    foreach (var run in config.RunCommands)
                    {
                        var runLine = $@"Filename: ""{run.Filename}""";
                        
                        if (!string.IsNullOrEmpty(run.Parameters))
                            runLine += $@"; Parameters: ""{run.Parameters}""";
                        
                        if (!string.IsNullOrEmpty(run.Description))
                            runLine += $@"; Description: ""{run.Description}""";
                        
                        if (!string.IsNullOrEmpty(run.Flags))
                            runLine += $"; Flags: {run.Flags}";
                        
                        if (run.CheckReturnCode)
                            runLine += "; Check: CheckReturnCode";
                        
                        scriptBuilder.AppendLine(runLine);
                    }
                    
                    scriptBuilder.AppendLine();
                }
                
                // [Registry] 部分
                if (config.RegistryEntries.Count > 0)
                {
                    scriptBuilder.AppendLine("[Registry]");
                    
                    foreach (var entry in config.RegistryEntries)
                    {
                        var parts = entry.Key.Split('|');
                        if (parts.Length >= 2)
                        {
                            var root = parts[0];
                            var subkey = parts[1];
                            var valueType = parts.Length > 2 ? parts[2] : "string";
                            
                            scriptBuilder.AppendLine($@"Root: {root}; Subkey: ""{subkey}""; ValueType: {valueType}; ValueName: ""{entry.Value}""; ValueData: ""{{app}}""; Flags: uninsdeletevalue");
                        }
                    }
                    
                    scriptBuilder.AppendLine();
                }
                
                // [UninstallDelete] 部分
                scriptBuilder.AppendLine("[UninstallDelete]");
                scriptBuilder.AppendLine(@"Type: filesandordirs; Name: ""{app}\Logs""");
                scriptBuilder.AppendLine(@"Type: files; Name: ""{app}\settings.ini""");
                scriptBuilder.AppendLine();
                
                // [Code] 部分 - 自定义代码
                if (config.CustomCodeSections.Count > 0)
                {
                    foreach (var customCode in config.CustomCodeSections)
                    {
                        scriptBuilder.AppendLine($"[{customCode.Section}]");
                        scriptBuilder.AppendLine(customCode.Code);
                        scriptBuilder.AppendLine();
                    }
                }
                else
                {
                    // 添加默认的Code部分
                    scriptBuilder.AppendLine(@"[Code]
function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
  NetFrameWorkInstalled : Boolean;
begin
  // 检查.NET Framework
  NetFrameWorkInstalled := RegKeyExists(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full');
  if not NetFrameWorkInstalled then
  begin
    if MsgBox('This application requires Microsoft .NET Framework 4.8.' + #13#10 +
              'Do you want to download and install it now?',
              mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open',
                'https://dotnet.microsoft.com/download/dotnet-framework/thank-you/net48-web-installer',
                '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
    end;
    Result := False;
  end
  else
    Result := True;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // 创建必要的目录
    ForceDirectories(ExpandConstant('{app}\Backups'));
    ForceDirectories(ExpandConstant('{app}\Logs'));
    ForceDirectories(ExpandConstant('{app}\Exports'));
  end;
end;

function CheckReturnCode(Param: String): Boolean;
begin
  // 自定义返回码检查逻辑
  Result := True;
end;
");
                }
                
                var script = scriptBuilder.ToString();
                _logger?.LogDebug($"Generated Inno Setup script:\n{script}");
                
                return script;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error generating Inno Setup script");
                throw;
            }
        }
        
        public async Task<string> CompileInstallerAsync(string scriptPath, CancellationToken ct = default)
        {
            try
            {
                if (!await CheckInnoSetupInstallationAsync(ct))
                {
                    throw new InvalidOperationException("Inno Setup is not installed");
                }
                
                if (!File.Exists(scriptPath))
                {
                    throw new FileNotFoundException($"Inno Setup script not found: {scriptPath}");
                }
                
                _logger?.LogInformation($"Compiling Inno Setup script: {scriptPath}");
                
                // 运行编译器
                var result = await RunCompilerAsync($"\"{scriptPath}\"", ct);
                
                if (result.exitCode == 0)
                {
                    _logger?.LogInformation($"Inno Setup compilation successful: {result.output}");
                    
                    // 从输出中提取生成的安装程序路径
                    var installerPath = ExtractInstallerPath(result.output);
                    if (!string.IsNullOrEmpty(installerPath) && File.Exists(installerPath))
                    {
                        _logger?.LogInformation($"Installer created: {installerPath}");
                        return installerPath;
                    }
                    else
                    {
                        // 尝试在脚本所在目录的OutputDir中查找
                        var scriptDir = Path.GetDirectoryName(scriptPath);
                        var outputDir = Path.Combine(scriptDir, "Output");
                        if (Directory.Exists(outputDir))
                        {
                            var installers = Directory.GetFiles(outputDir, "*.exe");
                            if (installers.Length > 0)
                            {
                                return installers[0];
                            }
                        }
                    }
                }
                else
                {
                    _logger?.LogError($"Inno Setup compilation failed: {result.error}");
                }
                
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error compiling Inno Setup script");
                throw;
            }
        }
        
        public async Task<string> BuildInstallerAsync(InnoSetupConfig config, string outputPath = "", 
            CancellationToken ct = default)
        {
            try
            {
                _logger?.LogInformation($"Building installer for {config.AppName}");
                
                // 生成脚本
                var script = await GenerateScriptAsync(config, ct);
                
                // 确定输出目录
                var actualOutputDir = string.IsNullOrEmpty(outputPath) ? 
                    config.OutputDir : Path.GetDirectoryName(outputPath);
                
                if (string.IsNullOrEmpty(actualOutputDir))
                    actualOutputDir = "./Installer";
                
                // 确保输出目录存在
                Directory.CreateDirectory(actualOutputDir);
                
                // 保存脚本文件
                var scriptFileName = $"{config.AppName.Replace(" ", "_")}.iss";
                var scriptPath = Path.Combine(actualOutputDir, scriptFileName);
                
                await File.WriteAllTextAsync(scriptPath, script, ct);
                _logger?.LogInformation($"Saved Inno Setup script to: {scriptPath}");
                
                // 编译安装程序
                var installerPath = await CompileInstallerAsync(scriptPath, ct);
                
                if (!string.IsNullOrEmpty(installerPath))
                {
                    // 如果需要，重命名安装程序
                    if (!string.IsNullOrEmpty(outputPath) && installerPath != outputPath)
                    {
                        File.Move(installerPath, outputPath, true);
                        installerPath = outputPath;
                    }
                    
                    _logger?.LogInformation($"Installer built successfully: {installerPath}");
                    return installerPath;
                }
                else
                {
                    throw new InvalidOperationException("Failed to build installer");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error building installer");
                throw;
            }
        }
        
        public async Task<string> SignInstallerAsync(string installerPath, string certificatePath, 
            string password, CancellationToken ct = default)
        {
            try
            {
                if (!File.Exists(installerPath))
                {
                    throw new FileNotFoundException($"Installer not found: {installerPath}");
                }
                
                if (!File.Exists(certificatePath))
                {
                    throw new FileNotFoundException($"Certificate not found: {certificatePath}");
                }
                
                _logger?.LogInformation($"Signing installer: {installerPath}");
                
                // 使用signtool签名（需要Windows SDK）
                var signToolPath = FindSignToolPath();
                if (string.IsNullOrEmpty(signToolPath))
                {
                    _logger?.LogWarning("SignTool not found. Installer will not be signed.");
                    return installerPath;
                }
                
                var tempPfx = Path.GetTempFileName();
                try
                {
                    // 复制证书文件
                    File.Copy(certificatePath, tempPfx, true);
                    
                    var command = $"sign /f \"{tempPfx}\" /p \"{password}\" /t http://timestamp.digicert.com \"{installerPath}\"";
                    var result = await ExecuteCommandAsync(signToolPath, command, ct);
                    
                    if (result.exitCode == 0)
                    {
                        _logger?.LogInformation($"Installer signed successfully: {installerPath}");
                        return installerPath;
                    }
                    else
                    {
                        _logger?.LogError($"Failed to sign installer: {result.error}");
                        return installerPath; // 仍然返回未签名的安装程序
                    }
                }
                finally
                {
                    if (File.Exists(tempPfx))
                        File.Delete(tempPfx);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error signing installer");
                return installerPath; // 仍然返回未签名的安装程序
            }
        }
        
        private async Task<(int exitCode, string output, string error)> RunCompilerAsync(string arguments, CancellationToken ct)
        {
            using var process = new System.Diagnostics.Process();
            process.StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = _innoSetupPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            
            process.OutputDataReceived += (sender, args) => 
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    outputBuilder.AppendLine(args.Data);
                    _logger?.LogDebug(args.Data);
                }
            };
            
            process.ErrorDataReceived += (sender, args) => 
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    errorBuilder.AppendLine(args.Data);
                    _logger?.LogError(args.Data);
                }
            };
            
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            await process.WaitForExitAsync(ct);
            
            return (process.ExitCode, outputBuilder.ToString(), errorBuilder.ToString());
        }
        
        private async Task<(int exitCode, string output, string error)> ExecuteCommandAsync(string command, string arguments, CancellationToken ct)
        {
            using var process = new System.Diagnostics.Process();
            process.StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            
            process.OutputDataReceived += (sender, args) => 
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    outputBuilder.AppendLine(args.Data);
                }
            };
            
            process.ErrorDataReceived += (sender, args) => 
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    errorBuilder.AppendLine(args.Data);
                }
            };
            
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            await process.WaitForExitAsync(ct);
            
            return (process.ExitCode, outputBuilder.ToString(), errorBuilder.ToString());
        }
        
        private string ExtractInstallerPath(string compilerOutput)
        {
            // 从编译器输出中提取安装程序路径
            var pattern = @"Output filename:\s*(.*\.exe)";
            var match = Regex.Match(compilerOutput, pattern);
            
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value.Trim();
            }
            
            return string.Empty;
        }
        
        private string FindSignToolPath()
        {
            // 常见SignTool路径
            var possiblePaths = new[]
            {
                @"C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x64\signtool.exe",
                @"C:\Program Files (x86)\Windows Kits\10\bin\x64\signtool.exe",
                Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Windows Kits\10\bin\10.0.19041.0\x64\signtool.exe"),
                @"C:\Program Files\Microsoft SDKs\Windows\v7.1\Bin\signtool.exe"
            };
            
            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }
            
            return string.Empty;
        }
        
        private string GetMainExecutable(InnoSetupConfig config)
        {
            // 从文件列表中查找主要的可执行文件
            var exeFile = config.Files
                .SelectMany(f => 
                {
                    if (Directory.Exists(f.Source))
                    {
                        return Directory.GetFiles(f.Source, "*.exe", SearchOption.AllDirectories);
                    }
                    else if (File.Exists(f.Source) && f.Source.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        return new[] { f.Source };
                    }
                    return Array.Empty<string>();
                })
                .FirstOrDefault();
            
            return exeFile != null ? Path.GetFileName(exeFile) : "MyApp.exe";
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}