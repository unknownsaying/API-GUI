Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Threading.Tasks
Imports DeploymentAutomationGUI.Core
Imports DeploymentAutomationGUI.Models
Imports Newtonsoft.Json

Namespace DeploymentAutomationGUI.Services
    Public Interface IDeploymentService
        Event LogMessage As EventHandler(Of LogMessageEventArgs)
        
        Function CheckDockerEnvironmentAsync() As Task(Of Boolean)
        Function BuildDockerImageAsync(dockerfile As String, tag As String, 
                                      context As String, buildArgs As Dictionary(Of String, String)) As Task(Of DeploymentResult)
        Function StartDockerComposeAsync(composeFile As String, pullLatest As Boolean) As Task(Of DeploymentResult)
        Function StopDockerComposeAsync(composeFile As String, removeVolumes As Boolean) As Task(Of DeploymentResult)
        Function GetDockerImagesAsync() As Task(Of IEnumerable(Of DockerImageInfo))
        Function GetDockerContainersAsync() As Task(Of IEnumerable(Of DockerContainerInfo))
        Function DeployToEcsAsync(config As EcsDeployConfig) As Task(Of DeploymentResult)
        Function GenerateInstallerScriptAsync(config As InstallerConfig) As Task(Of String)
        Function BuildInstallerAsync(config As InstallerConfig) As Task(Of String)
    End Interface
    
    Public Class DeploymentService
        Implements IDeploymentService
        
        Public Event LogMessage As EventHandler(Of LogMessageEventArgs) Implements IDeploymentService.LogMessage
        
        Private ReadOnly _commandExecutor As CommandExecutor
        Private ReadOnly _settingsManager As SettingsManager
        
        Public Sub New()
            _commandExecutor = New CommandExecutor()
            _settingsManager = New SettingsManager()
            
            AddHandler _commandExecutor.OutputReceived, AddressOf OnCommandOutputReceived
            AddHandler _commandExecutor.ErrorReceived, AddressOf OnCommandErrorReceived
        End Sub
        
        Public Async Function CheckDockerEnvironmentAsync() As Task(Of Boolean) Implements IDeploymentService.CheckDockerEnvironmentAsync
            Try
                RaiseLogMessage("Checking Docker installation...", LogLevel.Info)
                
                ' 检查Docker是否安装
                Dim result = Await _commandExecutor.ExecuteCommandAsync("docker --version")
                If result.ExitCode = 0 Then
                    RaiseLogMessage($"Docker version: {result.Output}", LogLevel.Info)
                    
                    ' 检查Docker守护进程是否运行
                    Dim psResult = Await _commandExecutor.ExecuteCommandAsync("docker ps")
                    If psResult.ExitCode = 0 Then
                        RaiseLogMessage("Docker daemon is running", LogLevel.Info)
                        Return True
                    Else
                        RaiseLogMessage("Docker daemon is not running", LogLevel.Error)
                        Return False
                    End If
                Else
                    RaiseLogMessage("Docker is not installed or not in PATH", LogLevel.Error)
                    Return False
                End If
            Catch ex As Exception
                RaiseLogMessage($"Error checking Docker environment: {ex.Message}", LogLevel.Error)
                Return False
            End Try
        End Function
        
        Public Async Function BuildDockerImageAsync(dockerfile As String, tag As String, 
                                                   context As String, buildArgs As Dictionary(Of String, String)) As Task(Of DeploymentResult) Implements IDeploymentService.BuildDockerImageAsync
            Try
                If Not File.Exists(dockerfile) Then
                    Return DeploymentResult.Failed($"Dockerfile not found: {dockerfile}")
                End If
                
                Dim command = New StringBuilder($"docker build -f ""{dockerfile}"" -t ""{tag}""")
                
                ' 添加构建参数
                If buildArgs IsNot Nothing Then
                    For Each arg In buildArgs
                        command.Append($" --build-arg {arg.Key}={arg.Value}")
                    Next
                End If
                
                command.Append($" ""{context}""")
                
                RaiseLogMessage($"Building Docker image with command: {command}", LogLevel.Info)
                
                Dim result = Await _commandExecutor.ExecuteCommandAsync(command.ToString(), 600) ' 10分钟超时
                
                If result.ExitCode = 0 Then
                    Return DeploymentResult.Success($"Image built successfully: {tag}")
                Else
                    Return DeploymentResult.Failed($"Build failed: {result.Error}")
                End If
            Catch ex As Exception
                Return DeploymentResult.Failed($"Error building image: {ex.Message}")
            End Try
        End Function
        
        Public Async Function StartDockerComposeAsync(composeFile As String, pullLatest As Boolean) As Task(Of DeploymentResult) Implements IDeploymentService.StartDockerComposeAsync
            Try
                If Not File.Exists(composeFile) Then
                    Return DeploymentResult.Failed($"Compose file not found: {composeFile}")
                End If
                
                Dim command = $"docker-compose -f ""{composeFile}"" up -d"
                If pullLatest Then
                    command &= " --pull always"
                End If
                
                RaiseLogMessage($"Starting Docker Compose services...", LogLevel.Info)
                
                Dim result = Await _commandExecutor.ExecuteCommandAsync(command, 300) ' 5分钟超时
                
                If result.ExitCode = 0 Then
                    Return DeploymentResult.Success("Docker Compose services started successfully")
                Else
                    Return DeploymentResult.Failed($"Failed to start services: {result.Error}")
                End If
            Catch ex As Exception
                Return DeploymentResult.Failed($"Error starting Docker Compose: {ex.Message}")
            End Try
        End Function
        
        Public Async Function GetDockerImagesAsync() As Task(Of IEnumerable(Of DockerImageInfo)) Implements IDeploymentService.GetDockerImagesAsync
            Try
                Dim command = "docker images --format ""{{.Repository}}|{{.Tag}}|{{.ID}}|{{.CreatedSince}}|{{.Size}}"""
                Dim result = Await _commandExecutor.ExecuteCommandAsync(command)
                
                If result.ExitCode = 0 Then
                    Dim images = New List(Of DockerImageInfo)()
                    Dim lines = result.Output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                    
                    For Each line In lines
                        Dim parts = line.Split("|"c)
                        If parts.Length >= 5 Then
                            images.Add(New DockerImageInfo() With {
                                .Repository = parts(0),
                                .Tag = parts(1),
                                .Id = parts(2),
                                .Created = parts(3),
                                .Size = parts(4)
                            })
                        End If
                    Next
                    
                    Return images
                End If
            Catch ex As Exception
                RaiseLogMessage($"Error getting Docker images: {ex.Message}", LogLevel.Error)
            End Try
            
            Return Enumerable.Empty(Of DockerImageInfo)()
        End Function
        
        Public Async Function DeployToEcsAsync(config As EcsDeployConfig) As Task(Of DeploymentResult) Implements IDeploymentService.DeployToEcsAsync
            Try
                RaiseLogMessage($"Starting ECS deployment to cluster: {config.ClusterName}", LogLevel.Info)
                
                ' 1. 检查AWS凭证
                RaiseLogMessage("Checking AWS credentials...", LogLevel.Info)
                Dim awsCheck = Await _commandExecutor.ExecuteCommandAsync("aws sts get-caller-identity")
                If awsCheck.ExitCode <> 0 Then
                    Return DeploymentResult.Failed("AWS credentials not configured or invalid")
                End If
                
                ' 2. 登录ECR
                RaiseLogMessage("Logging into Amazon ECR...", LogLevel.Info)
                Dim ecrLogin = Await _commandExecutor.ExecuteCommandAsync(
                    $"aws ecr get-login-password --region {config.Region} | " &
                    $"docker login --username AWS --password-stdin {config.AccountId}.dkr.ecr.{config.Region}.amazonaws.com")
                
                If ecrLogin.ExitCode <> 0 Then
                    Return DeploymentResult.Failed("Failed to login to Amazon ECR")
                End If
                
                ' 3. 推送镜像到ECR
                Dim ecrImage = $"{config.AccountId}.dkr.ecr.{config.Region}.amazonaws.com/{config.RepositoryName}:{config.ImageTag}"
                RaiseLogMessage($"Tagging and pushing image to ECR: {ecrImage}", LogLevel.Info)
                
                Dim tagResult = Await _commandExecutor.ExecuteCommandAsync($"docker tag {config.ImageTag} {ecrImage}")
                If tagResult.ExitCode <> 0 Then
                    Return DeploymentResult.Failed("Failed to tag Docker image")
                End If
                
                Dim pushResult = Await _commandExecutor.ExecuteCommandAsync($"docker push {ecrImage}")
                If pushResult.ExitCode <> 0 Then
                    Return DeploymentResult.Failed("Failed to push Docker image to ECR")
                End If
                
                ' 4. 更新ECS服务
                RaiseLogMessage("Updating ECS service...", LogLevel.Info)
                Dim updateCommand = $"aws ecs update-service --cluster {config.ClusterName} " &
                                   $"--service {config.ServiceName} --region {config.Region} " &
                                   $"--force-new-deployment"
                
                Dim updateResult = Await _commandExecutor.ExecuteCommandAsync(updateCommand, 300)
                If updateResult.ExitCode <> 0 Then
                    Return DeploymentResult.Failed($"Failed to update ECS service: {updateResult.Error}")
                End If
                
                RaiseLogMessage("ECS deployment initiated successfully", LogLevel.Info)
                RaiseLogMessage("Waiting for deployment to complete...", LogLevel.Info)
                
                ' 5. 等待部署完成
                Await WaitForEcsDeployment(config.ClusterName, config.ServiceName, config.Region)
                
                Return DeploymentResult.Success("ECS deployment completed successfully")
                
            Catch ex As Exception
                Return DeploymentResult.Failed($"Error deploying to ECS: {ex.Message}")
            End Try
        End Function
        
        Public Async Function GenerateInstallerScriptAsync(config As InstallerConfig) As Task(Of String) Implements IDeploymentService.GenerateInstallerScriptAsync
            Try
                ' 创建Inno Setup脚本
                Dim script = $"[Setup]
AppName={config.AppName}
AppVersion={config.AppVersion}
DefaultDirName={{autopf}}\{config.AppName.Replace(" ", "")}
DefaultGroupName={config.AppName}
OutputDir={config.OutputDir}
OutputBaseFilename={config.AppName.Replace(" ", "")}_Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Files]
Source: ""dist\*""; DestDir: ""{{app}}""; Flags: ignoreversion recursesubdirs

[Icons]
Name: ""{{group}}\{config.AppName}""; Filename: ""{{app}}\{config.AppName}.exe""
Name: ""{{commondesktop}}\{config.AppName}""; Filename: ""{{app}}\{config.AppName}.exe""

[Run]
Filename: ""{{app}}\{config.AppName}.exe""; Description: ""Launch {config.AppName}""; Flags: postinstall nowait skipifsilent
"
                
                ' 确保输出目录存在
                Directory.CreateDirectory(config.OutputDir)
                
                Dim scriptPath = Path.Combine(config.OutputDir, "installer.iss")
                Await File.WriteAllTextAsync(scriptPath, script)
                
                RaiseLogMessage($"Installer script generated: {scriptPath}", LogLevel.Info)
                Return scriptPath
                
            Catch ex As Exception
                RaiseLogMessage($"Error generating installer script: {ex.Message}", LogLevel.Error)
                Return Nothing
            End Try
        End Function
        
        Public Async Function BuildInstallerAsync(config As InstallerConfig) As Task(Of String) Implements IDeploymentService.BuildInstallerAsync
            Try
                ' 首先生成脚本
                Dim scriptPath = Await GenerateInstallerScriptAsync(config)
                If String.IsNullOrEmpty(scriptPath) Then
                    Return Nothing
                End If
                
                ' 检查Inno Setup是否安装
                RaiseLogMessage("Checking Inno Setup installation...", LogLevel.Info)
                Dim innoCheck = Await _commandExecutor.ExecuteCommandAsync("ISCC.exe /?")
                If innoCheck.ExitCode <> 0 Then
                    RaiseLogMessage("Inno Setup not found. Please install it first.", LogLevel.Error)
                    Return Nothing
                End If
                
                ' 编译安装程序
                RaiseLogMessage("Building installer with Inno Setup...", LogLevel.Info)
                Dim buildResult = Await _commandExecutor.ExecuteCommandAsync($"ISCC.exe ""{scriptPath}""", 300)
                
                If buildResult.ExitCode = 0 Then
                    ' 从输出中提取安装程序路径
                    Dim outputLines = buildResult.Output.Split(Environment.NewLine)
                    For Each line In outputLines
                        If line.Contains("Output filename:") Then
                            Dim parts = line.Split(":")
                            If parts.Length >= 2 Then
                                Dim installerPath = parts(1).Trim()
                                If File.Exists(installerPath) Then
                                    RaiseLogMessage($"Installer created: {installerPath}", LogLevel.Info)
                                    
                                    ' 如果需要签名
                                    If config.SignInstaller AndAlso Not String.IsNullOrEmpty(config.CertificatePath) Then
                                        Await SignInstallerAsync(installerPath, config.CertificatePath, config.CertificatePassword)
                                    End If
                                    
                                    Return installerPath
                                End If
                            End If
                        End If
                    Next
                Else
                    RaiseLogMessage($"Failed to build installer: {buildResult.Error}", LogLevel.Error)
                End If
                
                Return Nothing
                
            Catch ex As Exception
                RaiseLogMessage($"Error building installer: {ex.Message}", LogLevel.Error)
                Return Nothing
            End Try
        End Function
        
        Private Async Function SignInstallerAsync(installerPath As String, certPath As String, password As String) As Task
            Try
                RaiseLogMessage("Signing installer with digital certificate...", LogLevel.Info)
                
                Dim signTool = FindSignTool()
                If String.IsNullOrEmpty(signTool) Then
                    RaiseLogMessage("SignTool not found. Installer will not be signed.", LogLevel.Warning)
                    Return
                End If
                
                Dim command = $"""{signTool}"" sign /f ""{certPath}"" /p ""{password}"" /t http://timestamp.digicert.com ""{installerPath}"""
                Dim result = Await _commandExecutor.ExecuteCommandAsync(command, 60)
                
                If result.ExitCode = 0 Then
                    RaiseLogMessage("Installer signed successfully", LogLevel.Info)
                Else
                    RaiseLogMessage($"Failed to sign installer: {result.Error}", LogLevel.Warning)
                End If
                
            Catch ex As Exception
                RaiseLogMessage($"Error signing installer: {ex.Message}", LogLevel.Warning)
            End Try
        End Function
        
        Private Async Function WaitForEcsDeployment(clusterName As String, serviceName As String, region As String) As Task
            Dim maxAttempts = 30
            Dim attempt = 0
            
            While attempt < maxAttempts
                attempt += 1
                
                Dim command = $"aws ecs describe-services --cluster {clusterName} " &
                             $"--services {serviceName} --region {region} " &
                             $"--query ""services[0].deployments[?status=='PRIMARY'].runningCount"" " &
                             $"--output text"
                
                Dim result = Await _commandExecutor.ExecuteCommandAsync(command)
                If result.ExitCode = 0 AndAlso result.Output.Trim() = "1" Then
                    RaiseLogMessage("ECS deployment completed successfully", LogLevel.Info)
                    Return
                End If
                
                RaiseLogMessage($"Waiting for deployment... (attempt {attempt}/{maxAttempts})", LogLevel.Info)
                Await Task.Delay(10000) ' 等待10秒
            End While
            
            RaiseLogMessage("ECS deployment timed out", LogLevel.Warning)
        End Function
        
        Private Function FindSignTool() As String
            ' 查找SignTool的常见位置
            Dim possiblePaths = New String() {
                "C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x64\signtool.exe",
                "C:\Program Files (x86)\Windows Kits\10\bin\x64\signtool.exe",
                Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%\Windows Kits\10\bin\10.0.19041.0\x64\signtool.exe"),
                "C:\Program Files\Microsoft SDKs\Windows\v7.1\Bin\signtool.exe"
            }
            
            For Each path In possiblePaths
                If File.Exists(path) Then
                    Return path
                End If
            Next
            
            Return Nothing
        End Function
        
        Private Sub OnCommandOutputReceived(sender As Object, e As CommandOutputEventArgs)
            If Not String.IsNullOrEmpty(e.Output) Then
                RaiseLogMessage(e.Output.Trim(), LogLevel.Info)
            End If
        End Sub
        
        Private Sub OnCommandErrorReceived(sender As Object, e As CommandOutputEventArgs)
            If Not String.IsNullOrEmpty(e.Output) Then
                RaiseLogMessage(e.Output.Trim(), LogLevel.Error)
            End If
        End Sub
        
        Private Sub RaiseLogMessage(message As String, level As LogLevel)
            RaiseEvent LogMessage(Me, New LogMessageEventArgs() With {
                .Level = level,
                .Message = message
            })
        End Sub
    End Class
    
    Public Class CommandExecutor
        Public Event OutputReceived As EventHandler(Of CommandOutputEventArgs)
        Public Event ErrorReceived As EventHandler(Of CommandOutputEventArgs)
        
        Public Async Function ExecuteCommandAsync(command As String, Optional timeoutSeconds As Integer = 60) As Task(Of CommandResult)
            Dim processInfo As New ProcessStartInfo()
            
            ' 根据操作系统设置
            If Environment.OSVersion.Platform = PlatformID.Win32NT Then
                processInfo.FileName = "cmd.exe"
                processInfo.Arguments = $"/C {command}"
            Else
                processInfo.FileName = "/bin/bash"
                processInfo.Arguments = $"-c ""{command.Replace("""", "\""")}"""
            End If
            
            processInfo.RedirectStandardOutput = True
            processInfo.RedirectStandardError = True
            processInfo.UseShellExecute = False
            processInfo.CreateNoWindow = True
            processInfo.WindowStyle = ProcessWindowStyle.Hidden
            
            Using process As New Process()
                process.StartInfo = processInfo
                
                Dim outputBuilder As New StringBuilder()
                Dim errorBuilder As New StringBuilder()
                
                ' 设置输出和错误数据接收事件
                AddHandler process.OutputDataReceived, Sub(sender, e)
                                                            If Not String.IsNullOrEmpty(e.Data) Then
                                                                outputBuilder.AppendLine(e.Data)
                                                                RaiseEvent OutputReceived(Me, New CommandOutputEventArgs() With {.Output = e.Data})
                                                            End If
                                                        End Sub
                
                AddHandler process.ErrorDataReceived, Sub(sender, e)
                                                          If Not String.IsNullOrEmpty(e.Data) Then
                                                              errorBuilder.AppendLine(e.Data)
                                                              RaiseEvent ErrorReceived(Me, New CommandOutputEventArgs() With {.Output = e.Data})
                                                          End If
                                                      End Sub
                
                process.Start()
                process.BeginOutputReadLine()
                process.BeginErrorReadLine()
                
                ' 等待进程退出或超时
                Dim completed = Await Task.Run(Function() process.WaitForExit(timeoutSeconds * 1000))
                
                If Not completed Then
                    process.Kill()
                    Return New CommandResult() With {
                        .ExitCode = -1,
                        .Output = "",
                        .Error = "Command timed out"
                    }
                End If
                
                Await Task.Delay(100) ' 确保所有输出被捕获
                
                Return New CommandResult() With {
                    .ExitCode = process.ExitCode,
                    .Output = outputBuilder.ToString(),
                    .Error = errorBuilder.ToString()
                }
            End Using
        End Function
    End Class
    
    Public Class CommandResult
        Public Property ExitCode As Integer
        Public Property Output As String
        Public Property Error As String
    End Class
    
    Public Class CommandOutputEventArgs
        Inherits EventArgs
        Public Property Output As String
    End Class
End Namespace