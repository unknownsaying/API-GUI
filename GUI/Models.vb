Imports System
Imports System.Collections.Generic
Imports System.ComponentModel

Namespace DeploymentAutomationGUI.Models
    Public Enum LogLevel
        Debug
        Info
        Warning
        [Error]
    End Enum
    
    Public Class LogEntry
        Public Property Timestamp As DateTime
        Public Property Level As LogLevel
        Public Property Message As String
    End Class
    
    Public Class DockerImageInfo
        Public Property Repository As String
        Public Property Tag As String
        Public Property Id As String
        Public Property Created As String
        Public Property Size As String
        
        Public Overrides Function ToString() As String
            Return $"{Repository}:{Tag} ({Id.Substring(0, 12)}) - {Size} - {Created}"
        End Function
    End Class
    
    Public Class DockerContainerInfo
        Public Property Id As String
        Public Property Name As String
        Public Property Image As String
        Public Property Status As String
        Public Property State As String
        Public Property Ports As String
        
        Public Overrides Function ToString() As String
            Return $"{Name} ({Id.Substring(0, 12)}) - {Image} - {State}"
        End Function
    End Class
    
    Public Class DeploymentProject
        Public Property Name As String = "New Project"
        Public Property Version As String = "1.0.0"
        Public Property FilePath As String = ""
        
        ' Docker 配置
        Public Property DockerfilePath As String = "./Dockerfile"
        Public Property ComposeFilePath As String = "./docker-compose.yml"
        Public Property PullLatestImages As Boolean = True
        
        ' ECS 配置
        Public Property EcsClusterName As String = "default-cluster"
        Public Property EcsServiceName As String = "my-service"
        Public Property EcsRegion As String = "us-east-1"
        Public Property EcsDesiredCount As Integer = 1
        Public Property EcsCpu As Integer = 256
        Public Property EcsMemory As Integer = 512
        
        ' Installer 配置
        Public Property InstallerAppName As String = "My Application"
        Public Property InstallerAppVersion As String = "1.0.0"
        Public Property InstallerOutputDir As String = "./Installer"
        Public Property SignInstaller As Boolean = False
        
        ' 高级选项
        Public Property EnvironmentVariables As Dictionary(Of String, String) = 
            New Dictionary(Of String, String)()
        Public Property BuildArgs As Dictionary(Of String, String) = 
            New Dictionary(Of String, String)()
        Public Property Tags As Dictionary(Of String, String) = 
            New Dictionary(Of String, String)()
    End Class
    
    Public Class EcsDeployConfig
        Public Property ClusterName As String = "default-cluster"
        Public Property ServiceName As String = "my-service"
        Public Property Region As String = "us-east-1"
        Public Property AccountId As String = ""
        Public Property RepositoryName As String = "my-repo"
        Public Property ImageTag As String = "latest"
        Public Property DesiredCount As Integer = 1
        Public Property Cpu As Integer = 256
        Public Property Memory As Integer = 512
        Public Property EnableLoadBalancer As Boolean = True
        Public Property SubnetIds As List(Of String) = New List(Of String)()
        Public Property SecurityGroupIds As List(Of String) = New List(Of String)()
    End Class
    
    Public Class InstallerConfig
        Public Property AppName As String = "My Application"
        Public Property AppVersion As String = "1.0.0"
        Public Property OutputDir As String = "./Installer"
        Public Property SignInstaller As Boolean = False
        Public Property CertificatePath As String = ""
        Public Property CertificatePassword As String = ""
        Public Property Files As List(Of InstallerFile) = New List(Of InstallerFile)()
    End Class
    
    Public Class InstallerFile
        Public Property Source As String = ""
        Public Property Destination As String = ""
        Public Property Flags As String = "ignoreversion"
    End Class
    
    Public Class AppSettings
        Public Property IsDarkTheme As Boolean = False
        Public Property AutoSave As Boolean = True
        Public Property SaveInterval As Integer = 5 ' 分钟
        Public Property DefaultOutputDir As String = "./Output"
        Public Property DockerComposePath As String = ""
        Public Property AwsProfile As String = "default"
        Public Property LastProjects As List(Of String) = New List(Of String)()
    End Class
    
    Public Class DeploymentResult
        Public Property Success As Boolean
        Public Property Message As String
        Public Property ErrorMessage As String
        
        Public Shared Function Success(message As String) As DeploymentResult
            Return New DeploymentResult() With {
                .Success = True,
                .Message = message
            }
        End Function
        
        Public Shared Function Failed(errorMessage As String) As DeploymentResult
            Return New DeploymentResult() With {
                .Success = False,
                .ErrorMessage = errorMessage
            }
        End Function
    End Class
End Namespace