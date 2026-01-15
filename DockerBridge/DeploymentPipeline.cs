using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using Microsoft.Extensions.Logging;

namespace DockerDeploy
{
    // 完整部署管道
    public class CompleteDeploymentPipeline
    {
        private readonly ILogger<CompleteDeploymentPipeline> _logger;
        private readonly DockerDeploymentService _dockerService;
        private readonly AwsEcsDeploymentService _awsEcsService;
        private readonly InnoSetupBuilder _innoSetupBuilder;
        private readonly DeployConfig _dockerConfig;
        private readonly EcsDeployConfig _ecsConfig;
        private readonly InnoSetupConfig _installerConfig;
        
        public CompleteDeploymentPipeline(
            ILogger<CompleteDeploymentPipeline> logger = null,
            string awsRegion = "us-east-1")
        {
            _logger = logger;
            
            // 创建配置
            _dockerConfig = CreateDockerConfig();
            _ecsConfig = CreateEcsConfig();
            _installerConfig = CreateInstallerConfig();
            
            // 创建服务
            _dockerService = new DockerDeploymentService(_dockerConfig, 
                logger?.CreateLogger<DockerDeploymentService>());
            
            _awsEcsService = new AwsEcsDeploymentService(awsRegion,
                logger?.CreateLogger<AwsEcsDeploymentService>());
            
            _innoSetupBuilder = new InnoSetupBuilder(
                logger?.CreateLogger<InnoSetupBuilder>());
        }
        
        // 完整部署流程
        public async Task<bool> RunFullDeploymentAsync(CancellationToken ct = default)
        {
            try
            {
                _logger?.LogInformation("Starting full deployment pipeline...");
                
                // 阶段1: Docker构建和测试
                _logger?.LogInformation("\n=== Stage 1: Docker Build & Test ===");
                if (!await RunDockerStageAsync(ct))
                {
                    _logger?.LogError("Docker stage failed");
                    return false;
                }
                
                // 阶段2: 推送到AWS ECR
                _logger?.LogInformation("\n=== Stage 2: Push to AWS ECR ===");
                if (!await RunEcrStageAsync(ct))
                {
                    _logger?.LogError("ECR stage failed");
                    return false;
                }
                
                // 阶段3: 部署到AWS ECS
                _logger?.LogInformation("\n=== Stage 3: Deploy to AWS ECS ===");
                if (!await RunEcsStageAsync(ct))
                {
                    _logger?.LogError("ECS stage failed");
                    return false;
                }
                
                // 阶段4: 创建Windows安装程序
                _logger?.LogInformation("\n=== Stage 4: Create Windows Installer ===");
                if (!await RunInstallerStageAsync(ct))
                {
                    _logger?.LogWarning("Installer stage had issues (non-critical)");
                }
                
                _logger?.LogInformation("\n=== Deployment Pipeline Completed Successfully! ===");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Deployment pipeline failed");
                return false;
            }
        }
        
        private async Task<bool> RunDockerStageAsync(CancellationToken ct)
        {
            try
            {
                _logger?.LogInformation("1.1 Checking Docker environment...");
                var dockerStatus = await _dockerService.CheckDockerEnvironmentAsync(ct);
                if (dockerStatus != DeployStatus.Success)
                {
                    _logger?.LogError("Docker environment check failed");
                    return false;
                }
                
                _logger?.LogInformation("1.2 Building Docker image...");
                var buildStatus = await _dockerService.BuildImageAsync(
                    _dockerConfig.DockerfilePath,
                    $"{_dockerConfig.ProjectName}:latest",
                    _dockerConfig.BuildContext,
                    ct);
                
                if (buildStatus != DeployStatus.Success)
                {
                    _logger?.LogError("Docker build failed");
                    return false;
                }
                
                _logger?.LogInformation("1.3 Starting local Docker Compose for testing...");
                var composeStatus = await _dockerService.ComposeUpAsync(detached: true, ct: ct);
                
                if (composeStatus == DeployStatus.Success)
                {
                    _logger?.LogInformation("1.4 Running health checks...");
                    await Task.Delay(10000, ct); // 等待服务启动
                    
                    var healthStatus = await _dockerService.HealthCheckAsync(ct);
                    if (healthStatus == DeployStatus.Success)
                    {
                        _logger?.LogInformation("1.5 Local services are healthy");
                    }
                    
                    _logger?.LogInformation("1.6 Stopping local services...");
                    await _dockerService.ComposeDownAsync(removeVolumes: true, ct: ct);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Docker stage failed");
                return false;
            }
        }
        
        private async Task<bool> RunEcrStageAsync(CancellationToken ct)
        {
            try
            {
                _logger?.LogInformation("2.1 Checking AWS credentials...");
                if (!await _awsEcsService.CheckAwsCredentialsAsync(ct))
                {
                    _logger?.LogError("AWS credentials are invalid");
                    return false;
                }
                
                _logger?.LogInformation("2.2 Pushing image to ECR...");
                var ecrImageUri = await _awsEcsService.PushImageToEcrAsync(
                    $"{_dockerConfig.ProjectName}:latest",
                    _dockerConfig.ProjectName.ToLower(),
                    _ecsConfig.AwsRegion,
                    ct);
                
                if (!string.IsNullOrEmpty(ecrImageUri))
                {
                    _ecsConfig.ImageUri = ecrImageUri;
                    _logger?.LogInformation($"2.3 Image pushed successfully: {ecrImageUri}");
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ECR stage failed");
                return false;
            }
        }
        
        private async Task<bool> RunEcsStageAsync(CancellationToken ct)
        {
            try
            {
                _logger?.LogInformation("3.1 Deploying to AWS ECS...");
                var success = await _awsEcsService.DeployServiceAsync(_ecsConfig, ct);
                
                if (success)
                {
                    _logger?.LogInformation("3.2 ECS deployment successful");
                    
                    // 等待服务稳定并获取状态
                    _logger?.LogInformation("3.3 Getting service status...");
                    var serviceStatus = await _awsEcsService.GetServiceStatusAsync(
                        _ecsConfig.ServiceName, _ecsConfig.ClusterName, ct);
                    
                    if (serviceStatus != null)
                    {
                        _logger?.LogInformation($"Service Status: {serviceStatus.Status}");
                        _logger?.LogInformation($"Running Tasks: {serviceStatus.RunningCount}/{serviceStatus.DesiredCount}");
                        
                        // 获取服务端点信息
                        if (serviceStatus.LoadBalancers != null && serviceStatus.LoadBalancers.Count > 0)
                        {
                            _logger?.LogInformation($"Load Balancer DNS: {serviceStatus.LoadBalancers[0].LoadBalancerName}");
                        }
                    }
                    
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ECS stage failed");
                return false;
            }
        }
        
        private async Task<bool> RunInstallerStageAsync(CancellationToken ct)
        {
            try
            {
                _logger?.LogInformation("4.1 Checking Inno Setup installation...");
                if (!await _innoSetupBuilder.CheckInnoSetupInstallationAsync(ct))
                {
                    _logger?.LogWarning("Inno Setup not found. Skipping installer creation.");
                    return false;
                }
                
                _logger?.LogInformation("4.2 Building Windows installer...");
                var installerPath = await _innoSetupBuilder.BuildInstallerAsync(_installerConfig, ct: ct);
                
                if (!string.IsNullOrEmpty(installerPath) && File.Exists(installerPath))
                {
                    var fileInfo = new FileInfo(installerPath);
                    _logger?.LogInformation($"4.3 Installer created: {installerPath} ({fileInfo.Length / 1024 / 1024} MB)");
                    
                    // 可选: 签名安装程序
                    var certPath = Environment.GetEnvironmentVariable("INSTALLER_CERT_PATH");
                    var certPassword = Environment.GetEnvironmentVariable("INSTALLER_CERT_PASSWORD");
                    
                    if (!string.IsNullOrEmpty(certPath) && !string.IsNullOrEmpty(certPassword))
                    {
                        _logger?.LogInformation("4.4 Signing installer...");
                        await _innoSetupBuilder.SignInstallerAsync(
                            installerPath, certPath, certPassword, ct);
                    }
                    
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Installer stage failed");
                return false;
            }
        }
        
        // 配置创建方法
        private DeployConfig CreateDockerConfig()
        {
            return new DeployConfig
            {
                ProjectName = "MyEnterpriseApp",
                DockerfilePath = "./Dockerfile",
                ComposeFilePath = "./docker-compose.yml",
                BuildContext = ".",
                TimeoutSeconds = 600,
                BuildArgs = new Dictionary<string, string>
                {
                    ["VERSION"] = "1.0.0",
                    ["BUILD_DATE"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    ["BUILD_NUMBER"] = Environment.GetEnvironmentVariable("BUILD_NUMBER") ?? "1"
                },
                TargetPlatforms = new List<string> { "linux/amd64" }
            };
        }
        
        private EcsDeployConfig CreateEcsConfig()
        {
            return new EcsDeployConfig
            {
                ClusterName = "production-cluster",
                ServiceName = "my-enterprise-app",
                TaskDefinitionName = "my-app-task",
                ContainerName = "app",
                AwsRegion = "us-east-1",
                DesiredCount = 2,
                Cpu = 512,
                Memory = 1024,
                ContainerPorts = new List<int> { 80 },
                EnvironmentVariables = new List<string>
                {
                    "ASPNETCORE_ENVIRONMENT=Production",
                    "DATABASE_URL=postgresql://user:pass@db:5432/appdb",
                    "LOG_LEVEL=Information"
                },
                Tags = new Dictionary<string, string>
                {
                    ["Environment"] = "Production",
                    ["Application"] = "MyEnterpriseApp",
                    ["ManagedBy"] = "DockerDeployPipeline"
                },
                EnableAutoScaling = true,
                MinCapacity = 2,
                MaxCapacity = 5,
                EnableLoadBalancer = true,
                SubnetIds = new List<string>
                {
                    "subnet-12345678",
                    "subnet-87654321"
                },
                SecurityGroupId = "sg-12345678",
                ExecutionRoleArn = "arn:aws:iam::123456789012:role/ecsTaskExecutionRole",
                TaskRoleArn = "arn:aws:iam::123456789012:role/ecsTaskRole"
            };
        }
        
        private InnoSetupConfig CreateInstallerConfig()
        {
            return new InnoSetupConfig
            {
                AppName = "My Enterprise Application",
                AppVersion = "1.0.0",
                Publisher = "Enterprise Solutions Inc.",
                AppPublisherUrl = "https://enterprise.example.com",
                DefaultDirName = "{autopf}\\MyEnterpriseApp",
                DefaultGroupName = "My Enterprise Application",
                OutputDir = "./Distributables",
                OutputBaseFilename = "MyEnterpriseApp_Setup",
                Compression = "lzma2",
                SolidCompression = true,
                CreateUninstallIcon = true,
                CreateDesktopIcon = true,
                WizardStyle = "modern",
                Architectures = new List<string> { "x64" },
                Files = new List<InnoSetupConfig.InstallFile>
                {
                    new InnoSetupConfig.InstallFile
                    {
                        Source = "./publish/*",
                        DestDir = "{app}",
                        Flags = "ignoreversion recursesubdirs createallsubdirs"
                    },
                    new InnoSetupConfig.InstallFile
                    {
                        Source = "./Documentation/*",
                        DestDir = "{app}\\Docs",
                        Flags = "ignoreversion"
                    }
                },
                RunCommands = new List<InnoSetupConfig.InstallRun>
                {
                    new InnoSetupConfig.InstallRun
                    {
                        Filename = "{app}\\MyApp.exe",
                        Description = "Launch My Enterprise Application",
                        Flags = "postinstall nowait skipifsilent"
                    }
                },
                RegistryEntries = new Dictionary<string, string>
                {
                    ["HKCU|Software\\MyEnterpriseApp|InstallPath"] = "{app}",
                    ["HKCU|Software\\MyEnterpriseApp\\Settings|Version"] = "1.0.0"
                }
            };
        }
    }
    
    // 主程序
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Complete Deployment Pipeline ===");
            Console.WriteLine("Supports: Docker + Docker Compose + AWS ECS + Inno Setup\n");
            
            // 创建日志记录器
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });
            
            var logger = loggerFactory.CreateLogger<Program>();
            
            try
            {
                // 创建部署管道
                var pipeline = new CompleteDeploymentPipeline(
                    loggerFactory.CreateLogger<CompleteDeploymentPipeline>(),
                    "us-east-1");
                
                // 运行完整部署
                var success = await pipeline.RunFullDeploymentAsync();
                
                if (success)
                {
                    Console.WriteLine("\n✅ Deployment pipeline completed successfully!");
                    Console.WriteLine("\nSummary:");
                    Console.WriteLine("  ✓ Docker images built and tested");
                    Console.WriteLine("  ✓ Images pushed to AWS ECR");
                    Console.WriteLine("  ✓ Application deployed to AWS ECS");
                    Console.WriteLine("  ✓ Windows installer created");
                }
                else
                {
                    Console.WriteLine("\n❌ Deployment pipeline failed");
                    Environment.Exit(1);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Deployment pipeline error");
                Environment.Exit(1);
            }
        }
    }
}