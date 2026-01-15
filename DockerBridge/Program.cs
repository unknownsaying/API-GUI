using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DockerDeploy
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Docker Deployment System in C# ===\n");
            
            try
            {
                // 创建日志记录器
                using var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });
                
                var logger = loggerFactory.CreateLogger<Program>();
                
                // 创建部署配置
                var config = new DeployConfig
                {
                    ProjectName = "my-application",
                    DockerfilePath = "./Dockerfile",
                    ComposeFilePath = "./docker-compose.yml",
                    TimeoutSeconds = 600,
                    BuildArgs = new Dictionary<string, string>
                    {
                        ["VERSION"] = "1.0.0",
                        ["BUILD_DATE"] = DateTime.UtcNow.ToString("yyyy-MM-dd")
                    }
                };
                
                // 创建Docker Compose配置
                var composeConfig = new ComposeConfig
                {
                    Services = new Dictionary<string, ComposeService>
                    {
                        ["web"] = new ComposeService
                        {
                            Image = "nginx:alpine",
                            Ports = new List<string> { "80:80", "443:443" },
                            Restart = "unless-stopped"
                        },
                        ["api"] = new ComposeService
                        {
                            Build = new ComposeService.BuildConfig
                            {
                                Context = ".",
                                Dockerfile = "Dockerfile"
                            },
                            Ports = new List<string> { "5000:5000" },
                            Environment = new Dictionary<string, string>
                            {
                                ["ASPNETCORE_ENVIRONMENT"] = "Production",
                                ["ConnectionStrings__Default"] = "Host=db;Database=appdb;Username=user;Password=pass"
                            },
                            DependsOn = new List<string> { "db" },
                            Restart = "unless-stopped"
                        },
                        ["db"] = new ComposeService
                        {
                            Image = "postgres:13-alpine",
                            Environment = new Dictionary<string, string>
                            {
                                ["POSTGRES_DB"] = "appdb",
                                ["POSTGRES_USER"] = "user",
                                ["POSTGRES_PASSWORD"] = "pass"
                            },
                            Volumes = new List<string> { "postgres_data:/var/lib/postgresql/data" },
                            Restart = "unless-stopped"
                        }
                    },
                    Volumes = new Dictionary<string, object>
                    {
                        ["postgres_data"] = new { }
                    }
                };
                
                // 保存docker-compose.yml文件
                var yaml = composeConfig.ToYaml();
                await File.WriteAllTextAsync(config.ComposeFilePath, yaml);
                Console.WriteLine("Created docker-compose.yml file");
                
                // 创建Dockerfile（如果不存在）
                if (!File.Exists(config.DockerfilePath))
                {
                    var dockerfileContent = @"
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT [""dotnet"", ""myapp.dll""]
";
                    await File.WriteAllTextAsync(config.DockerfilePath, dockerfileContent);
                    Console.WriteLine("Created Dockerfile");
                }
                
                // 创建部署服务
                using var dockerService = new DockerDeploymentService(config, 
                    loggerFactory.CreateLogger<DockerDeploymentService>());
                
                // 检查Docker环境
                Console.WriteLine("\nChecking Docker environment...");
                var status = await dockerService.CheckDockerEnvironmentAsync();
                
                if (status != DeployStatus.Success)
                {
                    Console.WriteLine($"Error: Docker environment check failed - {status}");
                    return;
                }
                
                Console.WriteLine("Docker environment is ready");
                
                // 构建镜像
                Console.WriteLine("\nBuilding Docker image...");
                status = await dockerService.BuildImageAsync(
                    config.DockerfilePath, 
                    $"{config.ProjectName}:latest", 
                    config.BuildContext);
                
                if (status == DeployStatus.Success)
                {
                    Console.WriteLine("Docker image built successfully");
                }
                else
                {
                    Console.WriteLine($"Failed to build Docker image: {status}");
                }
                
                // 启动服务
                Console.WriteLine("\nStarting Docker Compose services...");
                status = await dockerService.ComposeUpAsync();
                
                if (status == DeployStatus.Success)
                {
                    Console.WriteLine("Docker Compose services started successfully");
                    
                    // 列出运行的容器
                    Console.WriteLine("\nRunning containers:");
                    var containers = await dockerService.ListContainersAsync();
                    foreach (var container in containers)
                    {
                        Console.WriteLine($"  {container}");
                    }
                    
                    // 等待一段时间让服务稳定
                    Console.WriteLine("\nServices are running. Press any key to stop...");
                    Console.ReadKey();
                    
                    // 停止服务
                    Console.WriteLine("\nStopping services...");
                    status = await dockerService.ComposeDownAsync(removeVolumes: true);
                    
                    if (status == DeployStatus.Success)
                    {
                        Console.WriteLine("Services stopped successfully");
                    }
                }
                
                // 清理系统
                Console.WriteLine("\nCleaning up Docker system...");
                status = await dockerService.CleanupSystemAsync(removeImages: false, removeVolumes: true);
                
                if (status == DeployStatus.Success)
                {
                    Console.WriteLine("Cleanup completed");
                }
                
                Console.WriteLine("\n=== Deployment completed successfully! ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner error: {ex.InnerException.Message}");
                }
            }
        }
    }
}