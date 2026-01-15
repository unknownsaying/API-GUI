Imports System
Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.IO
Imports System.Linq
Imports System.Runtime.CompilerServices
Imports System.Text
Imports System.Threading.Tasks
Imports DeploymentAutomationGUI.Core
Imports DeploymentAutomationGUI.Models
Imports DeploymentAutomationGUI.Services
Imports Newtonsoft.Json

Namespace DeploymentAutomationGUI.ViewModels
    Public Class MainViewModel
        Implements INotifyPropertyChanged
        
        Private _deploymentService As DeploymentService
        Private _project As DeploymentProject
        Private _settings As AppSettings
        Private _logs As ObservableCollection(Of LogEntry)
        Private _dockerImages As ObservableCollection(Of DockerImageInfo)
        Private _dockerContainers As ObservableCollection(Of DockerContainerInfo)
        Private _isBusy As Boolean
        Private _statusMessage As String
        Private _isDarkTheme As Boolean
        
        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
        Public Event StatusMessageChanged As EventHandler(Of StatusChangedEventArgs)
        Public Event ProgressChanged As EventHandler(Of ProgressChangedEventArgs)
        Public Event LogMessageAdded As EventHandler(Of LogMessageEventArgs)
        Public Event DockerImagesUpdated As EventHandler(Of DockerImagesUpdatedEventArgs)
        Public Event DockerContainersUpdated As EventHandler(Of DockerContainersUpdatedEventArgs)
        
        Public Sub New(deploymentService As DeploymentService)
            _deploymentService = deploymentService
            _project = New DeploymentProject()
            _settings = New AppSettings()
            _logs = New ObservableCollection(Of LogEntry)()
            _dockerImages = New ObservableCollection(Of DockerImageInfo)()
            _dockerContainers = New ObservableCollection(Of DockerContainerInfo)()
            
            ' 订阅服务事件
            AddHandler _deploymentService.LogMessage, AddressOf OnServiceLogMessage
        End Sub
        
        ' 属性
        Public Property DockerfilePath As String
            Get
                Return _project.DockerfilePath
            End Get
            Set(value As String)
                If _project.DockerfilePath <> value Then
                    _project.DockerfilePath = value
                    OnPropertyChanged()
                End If
            End Set
        End Property
        
        Public Property ComposeFilePath As String
            Get
                Return _project.ComposeFilePath
            End Get
            Set(value As String)
                If _project.ComposeFilePath <> value Then
                    _project.ComposeFilePath = value
                    OnPropertyChanged()
                End If
            End Set
        End Property
        
        Public Property PullLatestImages As Boolean
            Get
                Return _project.PullLatestImages
            End Get
            Set(value As Boolean)
                If _project.PullLatestImages <> value Then
                    _project.PullLatestImages = value
                    OnPropertyChanged()
                End If
            End Set
        End Property
        
        Public Property EcsClusterName As String
            Get
                Return _project.EcsClusterName
            End Get
            Set(value As String)
                If _project.EcsClusterName <> value Then
                    _project.EcsClusterName = value
                    OnPropertyChanged()
                End If
            End Set
        End Property
        
        Public Property EcsServiceName As String
            Get
                Return _project.EcsServiceName
            End Get
            Set(value As String)
                If _project.EcsServiceName <> value Then
                    _project.EcsServiceName = value
                    OnPropertyChanged()
                End If
            End Set
        End Property
        
        Public Property EcsRegion As String
            Get
                Return _project.EcsRegion
            End Get
            Set(value As String)
                If _project.EcsRegion <> value Then
                    _project.EcsRegion = value
                    OnPropertyChanged()
                End If
            End Set
        End Property
        
        Public Property InstallerAppName As String
            Get
                Return _project.InstallerAppName
            End Get
            Set(value As String)
                If _project.InstallerAppName <> value Then
                    _project.InstallerAppName = value
                    OnPropertyChanged()
                End If
            End Set
        End Property
        
        Public Property InstallerAppVersion As String
            Get
                Return _project.InstallerAppVersion
            End Get
            Set(value As String)
                If _project.InstallerAppVersion <> value Then
                    _project.InstallerAppVersion = value
                    OnPropertyChanged()
                End If
            End Set
        End Property
        
        Public Property InstallerOutputDir As String
            Get
                Return _project.InstallerOutputDir
            End Get
            Set(value As String)
                If _project.InstallerOutputDir <> value Then
                    _project.InstallerOutputDir = value
                    OnPropertyChanged()
                End If
            End Set
        End Property
        
        Public Property SignInstaller As Boolean
            Get
                Return _project.SignInstaller
            End Get
            Set(value As Boolean)
                If _project.SignInstaller <> value Then
                    _project.SignInstaller = value
                    OnPropertyChanged()
                End If
            End Set
        End Property
        
        Public Property IsDarkTheme As Boolean
            Get
                Return _settings.IsDarkTheme
            End Get
            Set(value As Boolean)
                If _settings.IsDarkTheme <> value Then
                    _settings.IsDarkTheme = value
                    OnPropertyChanged()
                End If
            End Set
        End Property
        
        Public ReadOnly Property Logs As ObservableCollection(Of LogEntry)
            Get
                Return _logs
            End Get
        End Property
        
        Public ReadOnly Property DockerImages As ObservableCollection(Of DockerImageInfo)
            Get
                Return _dockerImages
            End Get
        End Property
        
        Public ReadOnly Property DockerContainers As ObservableCollection(Of DockerContainerInfo)
            Get
                Return _dockerContainers
            End Get
        End Property
        
        Public Property IsBusy As Boolean
            Get
                Return _isBusy
            End Get
            Private Set(value As Boolean)
                If _isBusy <> value Then
                    _isBusy = value
                    OnPropertyChanged()
                    RaiseEvent ProgressChanged(Me, New ProgressChangedEventArgs With {
                        .IsBusy = value,
                        .IsIndeterminate = True
                    })
                End If
            End Set
        End Property
        
        Public Property StatusMessage As String
            Get
                Return _statusMessage
            End Get
            Private Set(value As String)
                If _statusMessage <> value Then
                    _statusMessage = value
                    OnPropertyChanged()
                End If
            End Set
        End Property
        
        ' 方法
        Public Async Function CheckDockerEnvironmentAsync() As Task(Of Boolean)
            Try
                UpdateStatus("Checking Docker environment...")
                IsBusy = True
                
                Dim isDockerRunning = Await _deploymentService.CheckDockerEnvironmentAsync()
                
                If isDockerRunning Then
                    UpdateStatus("Docker is running and accessible")
                    Return True
                Else
                    UpdateStatus("Docker is not running or not accessible", True)
                    Return False
                End If
            Catch ex As Exception
                UpdateStatus($"Error checking Docker: {ex.Message}", True)
                Return False
            Finally
                IsBusy = False
            End Try
        End Function
        
        Public Async Function BuildDockerImageAsync() As Task(Of Boolean)
            Try
                UpdateStatus($"Building Docker image from {DockerfilePath}")
                IsBusy = True
                
                Dim result = Await _deploymentService.BuildDockerImageAsync(
                    DockerfilePath,
                    $"{_project.Name.ToLower()}:{_project.Version}",
                    Path.GetDirectoryName(DockerfilePath),
                    New Dictionary(Of String, String)())
                
                If result.Success Then
                    AddLogMessage("Docker image built successfully", LogLevel.Info)
                    UpdateStatus("Docker image built successfully")
                    Return True
                Else
                    AddLogMessage($"Failed to build Docker image: {result.ErrorMessage}", LogLevel.Error)
                    UpdateStatus("Failed to build Docker image", True)
                    Return False
                End If
            Catch ex As Exception
                AddLogMessage($"Error building Docker image: {ex.Message}", LogLevel.Error)
                UpdateStatus($"Error: {ex.Message}", True)
                Return False
            Finally
                IsBusy = False
            End Try
        End Function
        
        Public Async Function StartDockerComposeAsync() As Task(Of Boolean)
            Try
                UpdateStatus("Starting Docker Compose services...")
                IsBusy = True
                
                Dim result = Await _deploymentService.StartDockerComposeAsync(
                    ComposeFilePath,
                    PullLatestImages)
                
                If result.Success Then
                    AddLogMessage("Docker Compose services started successfully", LogLevel.Info)
                    UpdateStatus("Docker Compose services started")
                    
                    ' 等待一段时间后刷新容器列表
                    Await Task.Delay(3000)
                    Await RefreshDockerListsAsync()
                    
                    Return True
                Else
                    AddLogMessage($"Failed to start Docker Compose: {result.ErrorMessage}", LogLevel.Error)
                    UpdateStatus("Failed to start Docker Compose services", True)
                    Return False
                End If
            Catch ex As Exception
                AddLogMessage($"Error starting Docker Compose: {ex.Message}", LogLevel.Error)
                UpdateStatus($"Error: {ex.Message}", True)
                Return False
            Finally
                IsBusy = False
            End Try
        End Function
        
        Public Async Function StopDockerComposeAsync() As Task(Of Boolean)
            Try
                UpdateStatus("Stopping Docker Compose services...")
                IsBusy = True
                
                Dim result = Await _deploymentService.StopDockerComposeAsync(
                    ComposeFilePath,
                    True) ' Remove volumes
                
                If result.Success Then
                    AddLogMessage("Docker Compose services stopped", LogLevel.Info)
                    UpdateStatus("Docker Compose services stopped")
                    
                    Await RefreshDockerListsAsync()
                    Return True
                Else
                    AddLogMessage($"Failed to stop Docker Compose: {result.ErrorMessage}", LogLevel.Error)
                    UpdateStatus("Failed to stop Docker Compose services", True)
                    Return False
                End If
            Catch ex As Exception
                AddLogMessage($"Error stopping Docker Compose: {ex.Message}", LogLevel.Error)
                UpdateStatus($"Error: {ex.Message}", True)
                Return False
            Finally
                IsBusy = False
            End Try
        End Function
        
        Public Async Function RefreshDockerListsAsync() As Task
            Try
                IsBusy = True
                
                ' 获取镜像列表
                Dim images = Await _deploymentService.GetDockerImagesAsync()
                _dockerImages.Clear()
                For Each image In images
                    _dockerImages.Add(image)
                Next
                
                RaiseEvent DockerImagesUpdated(Me, New DockerImagesUpdatedEventArgs With {
                    .Images = images
                })
                
                ' 获取容器列表
                Dim containers = Await _deploymentService.GetDockerContainersAsync()
                _dockerContainers.Clear()
                For Each container In containers
                    _dockerContainers.Add(container)
                Next
                
                RaiseEvent DockerContainersUpdated(Me, New DockerContainersUpdatedEventArgs With {
                    .Containers = containers
                })
                
            Catch ex As Exception
                AddLogMessage($"Error refreshing Docker lists: {ex.Message}", LogLevel.Error)
            Finally
                IsBusy = False
            End Try
        End Function
        
        Public Async Function DeployToEcsAsync() As Task(Of Boolean)
            Try
                UpdateStatus($"Deploying to AWS ECS cluster: {EcsClusterName}")
                IsBusy = True
                
                ' 创建ECS部署配置
                Dim config = New EcsDeployConfig() With {
                    .ClusterName = EcsClusterName,
                    .ServiceName = EcsServiceName,
                    .Region = EcsRegion,
                    .ImageTag = $"{_project.Name.ToLower()}:{_project.Version}",
                    .DesiredCount = _project.EcsDesiredCount,
                    .Cpu = _project.EcsCpu,
                    .Memory = _project.EcsMemory
                }
                
                Dim result = Await _deploymentService.DeployToEcsAsync(config)
                
                If result.Success Then
                    AddLogMessage("Successfully deployed to AWS ECS", LogLevel.Info)
                    UpdateStatus("Deployed to AWS ECS successfully")
                    Return True
                Else
                    AddLogMessage($"Failed to deploy to AWS ECS: {result.ErrorMessage}", LogLevel.Error)
                    UpdateStatus("Failed to deploy to AWS ECS", True)
                    Return False
                End If
            Catch ex As Exception
                AddLogMessage($"Error deploying to ECS: {ex.Message}", LogLevel.Error)
                UpdateStatus($"Error: {ex.Message}", True)
                Return False
            Finally
                IsBusy = False
            End Try
        End Function
        
        Public Async Function GenerateInstallerScriptAsync() As Task(Of String)
            Try
                UpdateStatus("Generating installer script...")
                IsBusy = True
                
                ' 创建安装程序配置
                Dim config = New InstallerConfig() With {
                    .AppName = InstallerAppName,
                    .AppVersion = InstallerAppVersion,
                    .OutputDir = InstallerOutputDir,
                    .SignInstaller = SignInstaller
                }
                
                Dim script = Await _deploymentService.GenerateInstallerScriptAsync(config)
                
                If Not String.IsNullOrEmpty(script) Then
                    AddLogMessage("Installer script generated successfully", LogLevel.Info)
                    UpdateStatus("Installer script generated")
                    Return script
                Else
                    AddLogMessage("Failed to generate installer script", LogLevel.Error)
                    UpdateStatus("Failed to generate installer script", True)
                    Return Nothing
                End If
            Catch ex As Exception
                AddLogMessage($"Error generating installer script: {ex.Message}", LogLevel.Error)
                UpdateStatus($"Error: {ex.Message}", True)
                Return Nothing
            Finally
                IsBusy = False
            End Try
        End Function
        
        Public Async Function BuildInstallerAsync() As Task(Of String)
            Try
                UpdateStatus("Building installer...")
                IsBusy = True
                
                ' 创建安装程序配置
                Dim config = New InstallerConfig() With {
                    .AppName = InstallerAppName,
                    .AppVersion = InstallerAppVersion,
                    .OutputDir = InstallerOutputDir,
                    .SignInstaller = SignInstaller
                }
                
                Dim installerPath = Await _deploymentService.BuildInstallerAsync(config)
                
                If Not String.IsNullOrEmpty(installerPath) AndAlso File.Exists(installerPath) Then
                    AddLogMessage($"Installer created: {installerPath}", LogLevel.Info)
                    UpdateStatus("Installer built successfully")
                    Return installerPath
                Else
                    AddLogMessage("Failed to build installer", LogLevel.Error)
                    UpdateStatus("Failed to build installer", True)
                    Return Nothing
                End If
            Catch ex As Exception
                AddLogMessage($"Error building installer: {ex.Message}", LogLevel.Error)
                UpdateStatus($"Error: {ex.Message}", True)
                Return Nothing
            Finally
                IsBusy = False
            End Try
        End Function
        
        Public Sub NewProject()
            _project = New DeploymentProject() With {
                .Name = "New Deployment Project",
                .Version = "1.0.0",
                .DockerfilePath = "./Dockerfile",
                .ComposeFilePath = "./docker-compose.yml",
                .InstallerOutputDir = "./Installer"
            }
            
            ClearLogs()
            UpdateStatus("New project created")
            
            ' 触发属性更改通知
            OnPropertyChanged(NameOf(DockerfilePath))
            OnPropertyChanged(NameOf(ComposeFilePath))
            OnPropertyChanged(NameOf(InstallerOutputDir))
        End Sub
        
        Public Sub OpenProject(filePath As String)
            Try
                If File.Exists(filePath) Then
                    Dim json = File.ReadAllText(filePath)
                    _project = JsonConvert.DeserializeObject(Of DeploymentProject)(json)
                    
                    UpdateStatus($"Project loaded: {Path.GetFileName(filePath)}")
                    AddLogMessage($"Project loaded from {filePath}", LogLevel.Info)
                    
                    ' 触发属性更改通知
                    OnPropertyChanged(String.Empty) ' 通知所有属性
                Else
                    UpdateStatus("Project file not found", True)
                End If
            Catch ex As Exception
                UpdateStatus($"Error loading project: {ex.Message}", True)
            End Try
        End Sub
        
        Public Sub SaveProject()
            Try
                If String.IsNullOrEmpty(_project.FilePath) Then
                    SaveProjectAs()
                Else
                    SaveProjectToFile(_project.FilePath)
                End If
            Catch ex As Exception
                UpdateStatus($"Error saving project: {ex.Message}", True)
            End Try
        End Sub
        
        Public Sub SaveProjectAs()
            ' 在UI中处理文件对话框
            ' 这个方法会被UI调用
        End Sub
        
        Public Sub SaveProjectToFile(filePath As String)
            Try
                _project.FilePath = filePath
                Dim json = JsonConvert.SerializeObject(_project, Formatting.Indented)
                File.WriteAllText(filePath, json)
                
                UpdateStatus($"Project saved: {Path.GetFileName(filePath)}")
                AddLogMessage($"Project saved to {filePath}", LogLevel.Info)
            Catch ex As Exception
                UpdateStatus($"Error saving project: {ex.Message}", True)
            End Try
        End Sub
        
        Public Sub LoadSettings()
            Try
                Dim settingsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "DeploymentAutomationGUI",
                    "settings.json")
                
                If File.Exists(settingsPath) Then
                    Dim json = File.ReadAllText(settingsPath)
                    _settings = JsonConvert.DeserializeObject(Of AppSettings)(json)
                End If
            Catch ex As Exception
                ' 使用默认设置
                _settings = New AppSettings()
            End Try
        End Sub
        
        Public Sub SaveSettings()
            Try
                Dim appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "DeploymentAutomationGUI")
                
                Directory.CreateDirectory(appDataPath)
                
                Dim settingsPath = Path.Combine(appDataPath, "settings.json")
                Dim json = JsonConvert.SerializeObject(_settings, Formatting.Indented)
                File.WriteAllText(settingsPath, json)
            Catch ex As Exception
                ' 忽略设置保存错误
            End Try
        End Sub
        
        Public Sub ToggleTheme()
            IsDarkTheme = Not IsDarkTheme
        End Sub
        
        Public Sub UpdateStatus(message As String, Optional isError As Boolean = False)
            StatusMessage = message
            RaiseEvent StatusMessageChanged(Me, New StatusChangedEventArgs With {
                .Message = message,
                .IsError = isError
            })
            
            If isError Then
                AddLogMessage(message, LogLevel.Error)
            Else
                AddLogMessage(message, LogLevel.Info)
            End If
        End Sub
        
        Public Sub AddLogMessage(message As String, level As LogLevel)
            Dim entry = New LogEntry() With {
                .Timestamp = DateTime.Now,
                .Level = level,
                .Message = message
            }
            
            _logs.Add(entry)
            
            RaiseEvent LogMessageAdded(Me, New LogMessageEventArgs With {
                .Level = level,
                .Message = message
            })
        End Sub
        
        Public Sub ClearLogs()
            _logs.Clear()
        End Sub
        
        Public Sub SaveLogs(filePath As String)
            Try
                Using writer As New StreamWriter(filePath)
                    For Each entry In _logs
                        writer.WriteLine($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] [{entry.Level}] {entry.Message}")
                    Next
                End Using
                
                UpdateStatus($"Logs saved to {filePath}")
            Catch ex As Exception
                UpdateStatus($"Error saving logs: {ex.Message}", True)
            End Try
        End Sub
        
        Private Sub OnServiceLogMessage(sender As Object, e As LogMessageEventArgs)
            AddLogMessage(e.Message, e.Level)
        End Sub
        
        Protected Sub OnPropertyChanged(<CallerMemberName> Optional propertyName As String = Nothing)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
        End Sub
    End Class
    
    ' 事件参数类
    Public Class StatusChangedEventArgs
        Inherits EventArgs
        Public Property Message As String
        Public Property IsError As Boolean
    End Class
    
    Public Class ProgressChangedEventArgs
        Inherits EventArgs
        Public Property IsBusy As Boolean
        Public Property ProgressPercentage As Integer
        Public Property IsIndeterminate As Boolean
    End Class
    
    Public Class LogMessageEventArgs
        Inherits EventArgs
        Public Property Level As LogLevel
        Public Property Message As String
    End Class
    
    Public Class DockerImagesUpdatedEventArgs
        Inherits EventArgs
        Public Property Images As IEnumerable(Of DockerImageInfo)
    End Class
    
    Public Class DockerContainersUpdatedEventArgs
        Inherits EventArgs
        Public Property Containers As IEnumerable(Of DockerContainerInfo)
    End Class
End Namespace