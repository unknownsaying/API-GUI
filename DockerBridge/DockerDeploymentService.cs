using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;

namespace DockerDeploy
{
    // 部署状态枚举
    public enum DeployStatus
    {
        Success,
        Failed,
        DockerNotRunning,
        ComposeFileNotFound,
        BuildFailed,
        PushFailed,
        PullFailed,
        NetworkError,
        Timeout,
        InsufficientResources
    }

    // Docker容器信息
    public class DockerContainerInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public List<string> Ports { get; set; } = new();
        public DateTime Created { get; set; }
        public Dictionary<string, string> Labels { get; set; } = new();
        public string Command { get; set; } = string.Empty;
        
        public override string ToString() => 
            $"{Name} ({Id[..12]}) - {State} - {Image}";
    }

    // Docker镜像信息
    public class DockerImageInfo
    {
        public string Id { get; set; } = string.Empty;
        public List<string> RepoTags { get; set; } = new();
        public DateTime Created { get; set; }
        public long Size { get; set; }
        public long VirtualSize { get; set; }
        public Dictionary<string, string> Labels { get; set; } = new();
        
        public string GetDisplaySize() => 
            Size > 1024 * 1024 ? $"{Size / (1024 * 1024):F2} MB" : $"{Size / 1024:F2} KB";
    }

    // Docker Compose服务配置
    public class ComposeService
    {
        [YamlMember(Alias = "image")]
        public string? Image { get; set; }
        
        [YamlMember(Alias = "build")]
        public BuildConfig? Build { get; set; }
        
        [YamlMember(Alias = "ports")]
        public List<string> Ports { get; set; } = new();
        
        [YamlMember(Alias = "environment")]
        public Dictionary<string, string> Environment { get; set; } = new();
        
        [YamlMember(Alias = "depends_on")]
        public List<string> DependsOn { get; set; } = new();
        
        [YamlMember(Alias = "volumes")]
        public List<string> Volumes { get; set; } = new();
        
        [YamlMember(Alias = "networks")]
        public List<string> Networks { get; set; } = new();
        
        [YamlMember(Alias = "restart")]
        public string Restart { get; set; } = "unless-stopped";
        
        [YamlMember(Alias = "healthcheck")]
        public HealthCheckConfig? HealthCheck { get; set; }
        
        public class BuildConfig
        {
            [YamlMember(Alias = "context")]
            public string Context { get; set; } = ".";
            
            [YamlMember(Alias = "dockerfile")]
            public string Dockerfile { get; set; } = "Dockerfile";
            
            [YamlMember(Alias = "args")]
            public Dictionary<string, string> Args { get; set; } = new();
        }
        
        public class HealthCheckConfig
        {
            [YamlMember(Alias = "test")]
            public List<string> Test { get; set; } = new();
            
            [YamlMember(Alias = "interval")]
            public string Interval { get; set; } = "30s";
            
            [YamlMember(Alias = "timeout")]
            public string Timeout { get; set; } = "10s";
            
            [YamlMember(Alias = "retries")]
            public int Retries { get; set; } = 3;
            
            [YamlMember(Alias = "start_period")]
            public string StartPeriod { get; set; } = "5s";
        }
    }

    // Docker Compose配置
    public class ComposeConfig
    {
        [YamlMember(Alias = "version")]
        public string Version { get; set; } = "3.8";
        
        [YamlMember(Alias = "services")]
        public Dictionary<string, ComposeService> Services { get; set; } = new();
        
        [YamlMember(Alias = "networks")]
        public Dictionary<string, object> Networks { get; set; } = new();
        
        [YamlMember(Alias = "volumes")]
        public Dictionary<string, object> Volumes { get; set; } = new();
        
        public string ToYaml()
        {
            var serializer = new SerializerBuilder()
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                .Build();
            
            return serializer.Serialize(this);
        }
        
        public static ComposeConfig FromYaml(string yamlContent)
        {
            var deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build();
            
            return deserializer.Deserialize<ComposeConfig>(yamlContent);
        }
    }

    // 部署配置
    public class DeployConfig
    {
        public string ProjectName { get; set; } = "my-project";
        public string DockerfilePath { get; set; } = "./Dockerfile";
        public string ComposeFilePath { get; set; } = "./docker-compose.yml";
        public string BuildContext { get; set; } = ".";
        public string RegistryUrl { get; set; } = string.Empty;
        public string RegistryUsername { get; set; } = string.Empty;
        public string RegistryPassword { get; set; } = string.Empty;
        public bool PullOnDeploy { get; set; } = false;
        public bool PruneAfterDeploy { get; set; } = false;
        public int TimeoutSeconds { get; set; } = 300;
        public Dictionary<string, string> BuildArgs { get; set; } = new();
        public List<string> TargetPlatforms { get; set; } = new() { "linux/amd64" };
        
        public static DeployConfig LoadFromFile(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<DeployConfig>(json) ?? new DeployConfig();
        }
        
        public void SaveToFile(string filePath)
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
    }

    // Docker部署服务接口
    public interface IDockerDeploymentService : IAsyncDisposable
    {
        Task<DeployStatus> CheckDockerEnvironmentAsync(CancellationToken ct = default);
        Task<DeployStatus> BuildImageAsync(string dockerfile, string tag, 
            string context = ".", CancellationToken ct = default);
        Task<DeployStatus> PushImageAsync(string imageName, string registry = "", 
            CancellationToken ct = default);
        Task<DeployStatus> ComposeUpAsync(string composeFile = "", bool detached = true, 
            CancellationToken ct = default);
        Task<DeployStatus> ComposeDownAsync(string composeFile = "", 
            bool removeVolumes = false, CancellationToken ct = default);
        Task<List<DockerContainerInfo>> ListContainersAsync(bool all = false, 
            CancellationToken ct = default);
        Task<List<DockerImageInfo>> ListImagesAsync(CancellationToken ct = default);
        Task<DeployStatus> CleanupSystemAsync(bool removeImages = false, 
            bool removeVolumes = false, CancellationToken ct = default);
        Task<Stream> GetContainerLogsAsync(string containerId, bool follow = false, 
            CancellationToken ct = default);
        Task<DeployStatus> HealthCheckAsync(CancellationToken ct = default);
    }

    // Docker部署服务实现
    public class DockerDeploymentService : IDockerDeploymentService
    {
        private readonly DockerClient _dockerClient;
        private readonly ILogger<DockerDeploymentService> _logger;
        private readonly DeployConfig _config;
        private bool _disposed;
        
        public DockerDeploymentService(DeployConfig config, ILogger<DockerDeploymentService> logger = null)
        {
            _config = config;
            _logger = logger;
            
            // 根据操作系统选择合适的Docker API端点
            var dockerUri = GetDockerUri();
            _dockerClient = new DockerClientConfiguration(dockerUri)
                .CreateClient();
        }
        
        private Uri GetDockerUri()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows上的Docker Desktop
                var isDockerDesktop = Environment.GetEnvironmentVariable("DOCKER_HOST");
                if (!string.IsNullOrEmpty(isDockerDesktop))
                {
                    return new Uri("npipe://./pipe/docker_engine");
                }
                return new Uri("npipe://./pipe/docker_engine");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || 
                     RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Linux/macOS
                var dockerHost = Environment.GetEnvironmentVariable("DOCKER_HOST");
                if (!string.IsNullOrEmpty(dockerHost))
                {
                    return new Uri(dockerHost);
                }
                return new Uri("unix:///var/run/docker.sock");
            }
            
            throw new PlatformNotSupportedException("Unsupported operating system");
        }
        
        public async Task<DeployStatus> CheckDockerEnvironmentAsync(CancellationToken ct = default)
        {
            try
            {
                _logger?.LogInformation("Checking Docker environment...");
                
                // 检查Docker守护进程是否运行
                var version = await _dockerClient.System.GetVersionAsync(ct);
                _logger?.LogInformation($"Docker version: {version.Version}");
                
                // 检查Docker Compose是否可用（通过命令行）
                var composeCheck = await RunCommandAsync("docker-compose --version", ct);
                if (composeCheck.exitCode != 0)
                {
                    _logger?.LogWarning("Docker Compose not found. Trying Docker Compose Plugin...");
                    var pluginCheck = await RunCommandAsync("docker compose version", ct);
                    if (pluginCheck.exitCode != 0)
                    {
                        _logger?.LogWarning("Docker Compose not available");
                    }
                }
                
                // 检查可用资源
                var systemInfo = await _dockerClient.System.GetSystemInfoAsync(ct);
                _logger?.LogInformation($"Docker info: {systemInfo.Name} - CPUs: {systemInfo.NCPU} - Memory: {systemInfo.MemTotal / (1024 * 1024)} MB");
                
                return DeployStatus.Success;
            }
            catch (DockerApiException ex)
            {
                _logger?.LogError(ex, "Docker API error");
                return DeployStatus.DockerNotRunning;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking Docker environment");
                return DeployStatus.Failed;
            }
        }
        
        public async Task<DeployStatus> BuildImageAsync(string dockerfile, string tag, 
            string context = ".", CancellationToken ct = default)
        {
            try
            {
                _logger?.LogInformation($"Building image {tag} from {dockerfile}");
                
                // 检查Dockerfile是否存在
                if (!File.Exists(dockerfile))
                {
                    _logger?.LogError($"Dockerfile not found: {dockerfile}");
                    return DeployStatus.Failed;
                }
                
                // 准备构建参数
                var buildParameters = new ImageBuildParameters
                {
                    Dockerfile = Path.GetFileName(dockerfile),
                    Tags = new List<string> { tag },
                    Platform = _config.TargetPlatforms.FirstOrDefault()
                };
                
                // 添加构建参数
                foreach (var arg in _config.BuildArgs)
                {
                    buildParameters.BuildArgs.Add(arg.Key, arg.Value);
                }
                
                // 读取Dockerfile内容
                using var dockerfileStream = File.OpenRead(dockerfile);
                
                // 准备构建上下文（tar文件）
                var buildContext = CreateBuildContext(context, dockerfile);
                
                // 执行构建
                using var response = await _dockerClient.Images.BuildImageFromDockerfileAsync(
                    buildContext, buildParameters, ct);
                
                // 读取构建输出
                using var reader = new StreamReader(response);
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        try
                        {
                            var message = JsonSerializer.Deserialize<DockerBuildMessage>(line);
                            if (message?.Stream != null)
                            {
                                _logger?.LogInformation(message.Stream.Trim());
                            }
                        }
                        catch (JsonException)
                        {
                            _logger?.LogDebug(line);
                        }
                    }
                }
                
                _logger?.LogInformation($"Image built successfully: {tag}");
                return DeployStatus.Success;
            }
            catch (DockerApiException ex)
            {
                _logger?.LogError(ex, $"Docker API error while building image {tag}");
                return DeployStatus.BuildFailed;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error building image {tag}");
                return DeployStatus.Failed;
            }
        }
        
        public async Task<DeployStatus> ComposeUpAsync(string composeFile = "", 
            bool detached = true, CancellationToken ct = default)
        {
            try
            {
                var file = string.IsNullOrEmpty(composeFile) ? _config.ComposeFilePath : composeFile;
                
                if (!File.Exists(file))
                {
                    _logger?.LogError($"Docker compose file not found: {file}");
                    return DeployStatus.ComposeFileNotFound;
                }
                
                _logger?.LogInformation($"Starting Docker Compose services from {file}");
                
                // 使用Docker Compose命令行
                var command = $"docker-compose -f \"{file}\" up";
                if (detached) command += " -d";
                if (_config.PullOnDeploy) command += " --pull always";
                
                var (exitCode, output, error) = await RunCommandAsync(command, ct, _config.TimeoutSeconds);
                
                if (exitCode == 0)
                {
                    _logger?.LogInformation("Docker Compose services started successfully");
                    
                    // 等待服务健康检查
                    await Task.Delay(5000, ct);
                    
                    // 列出服务状态
                    await ListContainersAsync(false, ct);
                    
                    return DeployStatus.Success;
                }
                else
                {
                    _logger?.LogError($"Failed to start Docker Compose services. Error: {error}");
                    return DeployStatus.Failed;
                }
            }
            catch (TaskCanceledException)
            {
                _logger?.LogError("Docker Compose operation timed out");
                return DeployStatus.Timeout;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error starting Docker Compose services");
                return DeployStatus.Failed;
            }
        }
        
        public async Task<DeployStatus> ComposeDownAsync(string composeFile = "", 
            bool removeVolumes = false, CancellationToken ct = default)
        {
            try
            {
                var file = string.IsNullOrEmpty(composeFile) ? _config.ComposeFilePath : composeFile;
                
                _logger?.LogInformation($"Stopping Docker Compose services from {file}");
                
                var command = $"docker-compose -f \"{file}\" down";
                if (removeVolumes) command += " -v";
                
                var (exitCode, output, error) = await RunCommandAsync(command, ct, 180);
                
                if (exitCode == 0)
                {
                    _logger?.LogInformation("Docker Compose services stopped successfully");
                    return DeployStatus.Success;
                }
                else
                {
                    _logger?.LogError($"Failed to stop Docker Compose services. Error: {error}");
                    return DeployStatus.Failed;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error stopping Docker Compose services");
                return DeployStatus.Failed;
            }
        }
        
        public async Task<List<DockerContainerInfo>> ListContainersAsync(bool all = false, 
            CancellationToken ct = default)
        {
            try
            {
                var parameters = new ContainersListParameters
                {
                    All = all,
                    Filters = new Dictionary<string, IDictionary<string, bool>>()
                };
                
                var containers = await _dockerClient.Containers.ListContainersAsync(parameters, ct);
                
                var result = new List<DockerContainerInfo>();
                
                foreach (var container in containers)
                {
                    var info = new DockerContainerInfo
                    {
                        Id = container.ID,
                        Name = container.Names?.FirstOrDefault()?.TrimStart('/') ?? string.Empty,
                        Image = container.Image,
                        Status = container.Status,
                        State = container.State,
                        Created = container.Created,
                        Command = container.Command
                    };
                    
                    if (container.Ports != null)
                    {
                        info.Ports = container.Ports
                            .Select(p => $"{p.PublicPort}:{p.PrivatePort}/{p.Type}")
                            .ToList();
                    }
                    
                    if (container.Labels != null)
                    {
                        info.Labels = container.Labels;
                    }
                    
                    result.Add(info);
                }
                
                _logger?.LogInformation($"Found {result.Count} containers");
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error listing containers");
                return new List<DockerContainerInfo>();
            }
        }
        
        public async Task<DeployStatus> CleanupSystemAsync(bool removeImages = false, 
            bool removeVolumes = false, CancellationToken ct = default)
        {
            try
            {
                _logger?.LogInformation("Starting Docker system cleanup...");
                
                // 停止所有容器
                var containers = await ListContainersAsync(false, ct);
                foreach (var container in containers)
                {
                    try
                    {
                        await _dockerClient.Containers.StopContainerAsync(
                            container.Id, new ContainerStopParameters(), ct);
                        _logger?.LogDebug($"Stopped container: {container.Name}");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, $"Could not stop container: {container.Name}");
                    }
                }
                
                // 删除所有容器
                containers = await ListContainersAsync(true, ct);
                foreach (var container in containers)
                {
                    try
                    {
                        await _dockerClient.Containers.RemoveContainerAsync(
                            container.Id, new ContainerRemoveParameters(), ct);
                        _logger?.LogDebug($"Removed container: {container.Name}");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, $"Could not remove container: {container.Name}");
                    }
                }
                
                // 清理网络
                var networks = await _dockerClient.Networks.ListNetworksAsync(ct);
                foreach (var network in networks.Where(n => !n.Name.StartsWith("bridge") && 
                                                          !n.Name.StartsWith("host") && 
                                                          !n.Name.StartsWith("none")))
                {
                    try
                    {
                        await _dockerClient.Networks.DeleteNetworkAsync(network.Name, ct);
                        _logger?.LogDebug($"Removed network: {network.Name}");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, $"Could not remove network: {network.Name}");
                    }
                }
                
                if (removeImages)
                {
                    // 删除所有镜像
                    var images = await _dockerClient.Images.ListImagesAsync(
                        new ImagesListParameters { All = true }, ct);
                    
                    foreach (var image in images)
                    {
                        try
                        {
                            await _dockerClient.Images.DeleteImageAsync(
                                image.ID, new ImageDeleteParameters(), ct);
                            _logger?.LogDebug($"Removed image: {image.ID[..12]}");
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, $"Could not remove image: {image.ID[..12]}");
                        }
                    }
                }
                
                if (removeVolumes)
                {
                    // 清理卷
                    var volumes = await _dockerClient.Volumes.ListAsync(ct);
                    foreach (var volume in volumes.Volumes)
                    {
                        try
                        {
                            await _dockerClient.Volumes.RemoveAsync(volume.Name, false, ct);
                            _logger?.LogDebug($"Removed volume: {volume.Name}");
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, $"Could not remove volume: {volume.Name}");
                        }
                    }
                }
                
                // 执行系统级清理
                await _dockerClient.System.PruneSystemAsync(new SystemPruneParameters(), ct);
                
                _logger?.LogInformation("Docker system cleanup completed");
                return DeployStatus.Success;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during Docker system cleanup");
                return DeployStatus.Failed;
            }
        }
        
        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _dockerClient?.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
        
        // 辅助方法
        private async Task<(int exitCode, string output, string error)> RunCommandAsync(
            string command, CancellationToken ct, int timeoutSeconds = 300)
        {
            try
            {
                using var process = new System.Diagnostics.Process();
                process.StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "/bin/bash",
                    Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"/C {command}" : $"-c \"{command}\"",
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
                
                // 等待进程完成或超时
                var completed = await Task.Run(() => process.WaitForExit(timeoutSeconds * 1000), ct);
                
                if (!completed)
                {
                    process.Kill();
                    return (-1, "", "Command timed out");
                }
                
                await Task.Delay(100, ct); // 确保所有输出被捕获
                
                return (process.ExitCode, outputBuilder.ToString(), errorBuilder.ToString());
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error running command: {command}");
                return (-1, "", ex.Message);
            }
        }
        
        private Stream CreateBuildContext(string contextPath, string dockerfile)
        {
            // 创建一个tar文件作为构建上下文
            var tempFile = Path.GetTempFileName();
            
            using (var tarFile = File.Create(tempFile))
            using (var tar = new TarWriter(tarFile))
            {
                // 添加Dockerfile
                var dockerfileName = Path.GetFileName(dockerfile);
                tar.WriteFile(dockerfile, dockerfileName);
                
                // 添加上下文目录中的所有文件
                if (Directory.Exists(contextPath))
                {
                    AddDirectoryToTar(tar, contextPath, contextPath);
                }
            }
            
            return File.OpenRead(tempFile);
        }
        
        private void AddDirectoryToTar(TarWriter tar, string root, string current)
        {
            foreach (var file in Directory.GetFiles(current))
            {
                var relativePath = Path.GetRelativePath(root, file).Replace('\\', '/');
                tar.WriteFile(file, relativePath);
            }
            
            foreach (var dir in Directory.GetDirectories(current))
            {
                AddDirectoryToTar(tar, root, dir);
            }
        }
        
        // Docker构建消息类
        private class DockerBuildMessage
        {
            [JsonPropertyName("stream")]
            public string? Stream { get; set; }
            
            [JsonPropertyName("error")]
            public string? Error { get; set; }
            
            [JsonPropertyName("errorDetail")]
            public object? ErrorDetail { get; set; }
            
            [JsonPropertyName("aux")]
            public object? Aux { get; set; }
        }
    }

    // Tar写入器（简化版）
    public class TarWriter : IDisposable
    {
        private readonly Stream _stream;
        
        public TarWriter(Stream stream)
        {
            _stream = stream;
        }
        
        public void WriteFile(string filePath, string entryName)
        {
            // 简化实现 - 实际应实现完整的tar格式
            var fileBytes = File.ReadAllBytes(filePath);
            var header = Encoding.ASCII.GetBytes(entryName);
            _stream.Write(header, 0, header.Length);
            _stream.Write(fileBytes, 0, fileBytes.Length);
        }
        
        public void Dispose()
        {
            _stream?.Dispose();
        }
    }
}