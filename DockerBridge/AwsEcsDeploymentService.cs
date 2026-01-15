using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Amazon;
using Amazon.ECS;
using Amazon.ECS.Model;
using Amazon.ECR;
using Amazon.ECR.Model;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Microsoft.Extensions.Logging;

namespace DockerDeploy
{
    // ECS部署配置
    public class EcsDeployConfig
    {
        public string ClusterName { get; set; } = "default-cluster";
        public string ServiceName { get; set; } = "my-service";
        public string TaskDefinitionName { get; set; } = "my-task";
        public string ContainerName { get; set; } = "app";
        public string ImageUri { get; set; } = string.Empty;
        public string AwsRegion { get; set; } = "us-east-1";
        public string AwsProfile { get; set; } = "default";
        public int DesiredCount { get; set; } = 1;
        public int Cpu { get; set; } = 256; // 256 = 0.25 vCPU
        public int Memory { get; set; } = 512; // MB
        public List<int> ContainerPorts { get; set; } = new() { 80 };
        public List<string> EnvironmentVariables { get; set; } = new();
        public Dictionary<string, string> Tags { get; set; } = new();
        public bool EnableAutoScaling { get; set; } = false;
        public int MinCapacity { get; set; } = 1;
        public int MaxCapacity { get; set; } = 3;
        public bool EnableLoadBalancer { get; set; } = false;
        public string LoadBalancerArn { get; set; } = string.Empty;
        public string TargetGroupArn { get; set; } = string.Empty;
        public string SecurityGroupId { get; set; } = string.Empty;
        public List<string> SubnetIds { get; set; } = new();
        public string ExecutionRoleArn { get; set; } = string.Empty;
        public string TaskRoleArn { get; set; } = string.Empty;
    }

    // ECS部署服务接口
    public interface IAwsEcsDeploymentService : IDisposable
    {
        Task<bool> CheckAwsCredentialsAsync(CancellationToken ct = default);
        Task<string> CreateOrUpdateClusterAsync(string clusterName, CancellationToken ct = default);
        Task<string> CreateTaskDefinitionAsync(EcsDeployConfig config, CancellationToken ct = default);
        Task<string> CreateOrUpdateServiceAsync(EcsDeployConfig config, CancellationToken ct = default);
        Task<bool> DeployServiceAsync(EcsDeployConfig config, CancellationToken ct = default);
        Task<bool> UpdateServiceAsync(EcsDeployConfig config, CancellationToken ct = default);
        Task<bool> RollbackServiceAsync(string serviceName, string clusterName, CancellationToken ct = default);
        Task<List<Service>> ListServicesAsync(string clusterName, CancellationToken ct = default);
        Task<List<TaskDefinition>> ListTaskDefinitionsAsync(string familyPrefix, CancellationToken ct = default);
        Task<Service> GetServiceStatusAsync(string serviceName, string clusterName, CancellationToken ct = default);
        Task<bool> DeleteServiceAsync(string serviceName, string clusterName, CancellationToken ct = default);
        Task<bool> DeleteClusterAsync(string clusterName, CancellationToken ct = default);
        Task<string> PushImageToEcrAsync(string imageName, string repositoryName, 
            string region, CancellationToken ct = default);
    }

    // ECS部署服务实现
    public class AwsEcsDeploymentService : IAwsEcsDeploymentService
    {
        private readonly IAmazonECS _ecsClient;
        private readonly IAmazonECR _ecrClient;
        private readonly IAmazonIdentityManagementService _iamClient;
        private readonly IAmazonCloudFormation _cloudFormationClient;
        private readonly ILogger<AwsEcsDeploymentService> _logger;
        private readonly RegionEndpoint _region;
        private bool _disposed;
        
        public AwsEcsDeploymentService(string region = "us-east-1", 
            ILogger<AwsEcsDeploymentService> logger = null)
        {
            _region = RegionEndpoint.GetBySystemName(region);
            _logger = logger;
            
            // 创建AWS客户端
            _ecsClient = new AmazonECSClient(_region);
            _ecrClient = new AmazonECRClient(_region);
            _iamClient = new AmazonIdentityManagementServiceClient(_region);
            _cloudFormationClient = new AmazonCloudFormationClient(_region);
        }
        
        public async Task<bool> CheckAwsCredentialsAsync(CancellationToken ct = default)
        {
            try
            {
                _logger?.LogInformation("Checking AWS credentials...");
                
                // 尝试调用一个简单的API来验证凭证
                var response = await _ecsClient.ListClustersAsync(new ListClustersRequest(), ct);
                
                _logger?.LogInformation($"AWS credentials are valid. Found {response.ClusterArns.Count} clusters");
                return true;
            }
            catch (AmazonECSException ex)
            {
                _logger?.LogError(ex, "AWS ECS error");
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "AWS credentials check failed");
                return false;
            }
        }
        
        public async Task<string> CreateOrUpdateClusterAsync(string clusterName, CancellationToken ct = default)
        {
            try
            {
                _logger?.LogInformation($"Creating/updating ECS cluster: {clusterName}");
                
                // 检查集群是否已存在
                var listRequest = new ListClustersRequest();
                var clusters = await _ecsClient.ListClustersAsync(listRequest, ct);
                
                if (clusters.ClusterArns.Any(c => c.Contains(clusterName)))
                {
                    _logger?.LogInformation($"Cluster {clusterName} already exists");
                    return clusters.ClusterArns.First(c => c.Contains(clusterName));
                }
                
                // 创建新集群
                var createRequest = new CreateClusterRequest
                {
                    ClusterName = clusterName,
                    CapacityProviders = new List<string> { "FARGATE" },
                    DefaultCapacityProviderStrategy = new List<CapacityProviderStrategyItem>
                    {
                        new CapacityProviderStrategyItem
                        {
                            CapacityProvider = "FARGATE",
                            Weight = 1,
                            Base = 0
                        }
                    },
                    Settings = new List<ClusterSetting>
                    {
                        new ClusterSetting
                        {
                            Name = ClusterSettingName.ContainerInsights,
                            Value = "enabled"
                        }
                    },
                    Tags = new List<Tag>
                    {
                        new Tag { Key = "CreatedBy", Value = "DockerDeploy" },
                        new Tag { Key = "Environment", Value = "Production" }
                    }
                };
                
                var response = await _ecsClient.CreateClusterAsync(createRequest, ct);
                
                _logger?.LogInformation($"Created ECS cluster: {response.Cluster.ClusterArn}");
                return response.Cluster.ClusterArn;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error creating/updating cluster {clusterName}");
                throw;
            }
        }
        
        public async Task<string> CreateTaskDefinitionAsync(EcsDeployConfig config, CancellationToken ct = default)
        {
            try
            {
                _logger?.LogInformation($"Creating task definition: {config.TaskDefinitionName}");
                
                // 准备容器定义
                var containerDefinition = new ContainerDefinition
                {
                    Name = config.ContainerName,
                    Image = config.ImageUri,
                    Cpu = config.Cpu,
                    Memory = config.Memory,
                    Essential = true,
                    Environment = config.EnvironmentVariables.Select(ev =>
                    {
                        var parts = ev.Split('=', 2);
                        return new Amazon.ECS.Model.KeyValuePair
                        {
                            Name = parts[0],
                            Value = parts.Length > 1 ? parts[1] : string.Empty
                        };
                    }).ToList(),
                    PortMappings = config.ContainerPorts.Select(port => new PortMapping
                    {
                        ContainerPort = port,
                        Protocol = TransportProtocol.TCP
                    }).ToList(),
                    LogConfiguration = new LogConfiguration
                    {
                        LogDriver = LogDriver.Awslogs,
                        Options = new Dictionary<string, string>
                        {
                            ["awslogs-group"] = $"/ecs/{config.TaskDefinitionName}",
                            ["awslogs-region"] = config.AwsRegion,
                            ["awslogs-stream-prefix"] = "ecs"
                        }
                    }
                };
                
                // 创建任务定义
                var request = new RegisterTaskDefinitionRequest
                {
                    Family = config.TaskDefinitionName,
                    NetworkMode = NetworkMode.Awsvpc,
                    RequiresCompatibilities = new List<string> { "FARGATE" },
                    Cpu = config.Cpu.ToString(),
                    Memory = config.Memory.ToString(),
                    ContainerDefinitions = new List<ContainerDefinition> { containerDefinition },
                    ExecutionRoleArn = config.ExecutionRoleArn,
                    TaskRoleArn = config.TaskRoleArn,
                    Tags = config.Tags.Select(t => new Tag { Key = t.Key, Value = t.Value }).ToList()
                };
                
                var response = await _ecsClient.RegisterTaskDefinitionAsync(request, ct);
                
                _logger?.LogInformation($"Created task definition: {response.TaskDefinition.TaskDefinitionArn}");
                return response.TaskDefinition.TaskDefinitionArn;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error creating task definition {config.TaskDefinitionName}");
                throw;
            }
        }
        
        public async Task<string> CreateOrUpdateServiceAsync(EcsDeployConfig config, CancellationToken ct = default)
        {
            try
            {
                _logger?.LogInformation($"Creating/updating service: {config.ServiceName}");
                
                // 检查服务是否已存在
                var describeRequest = new DescribeServicesRequest
                {
                    Cluster = config.ClusterName,
                    Services = new List<string> { config.ServiceName }
                };
                
                Service existingService = null;
                
                try
                {
                    var describeResponse = await _ecsClient.DescribeServicesAsync(describeRequest, ct);
                    existingService = describeResponse.Services.FirstOrDefault();
                }
                catch (ServiceNotFoundException)
                {
                    // 服务不存在，将创建新服务
                }
                
                if (existingService != null)
                {
                    // 更新现有服务
                    _logger?.LogInformation($"Updating existing service: {config.ServiceName}");
                    
                    var updateRequest = new UpdateServiceRequest
                    {
                        Cluster = config.ClusterName,
                        Service = config.ServiceName,
                        DesiredCount = config.DesiredCount,
                        TaskDefinition = config.TaskDefinitionName,
                        DeploymentConfiguration = new DeploymentConfiguration
                        {
                            MaximumPercent = 200,
                            MinimumHealthyPercent = 100,
                            DeploymentCircuitBreaker = new DeploymentCircuitBreaker
                            {
                                Enable = true,
                                Rollback = true
                            }
                        },
                        NetworkConfiguration = new NetworkConfiguration
                        {
                            AwsvpcConfiguration = new AwsVpcConfiguration
                            {
                                Subnets = config.SubnetIds,
                                SecurityGroups = !string.IsNullOrEmpty(config.SecurityGroupId) 
                                    ? new List<string> { config.SecurityGroupId } 
                                    : null,
                                AssignPublicIp = AssignPublicIp.ENABLED
                            }
                        }
                    };
                    
                    var updateResponse = await _ecsClient.UpdateServiceAsync(updateRequest, ct);
                    
                    _logger?.LogInformation($"Updated service: {updateResponse.Service.ServiceArn}");
                    return updateResponse.Service.ServiceArn;
                }
                else
                {
                    // 创建新服务
                    _logger?.LogInformation($"Creating new service: {config.ServiceName}");
                    
                    var createRequest = new CreateServiceRequest
                    {
                        Cluster = config.ClusterName,
                        ServiceName = config.ServiceName,
                        TaskDefinition = config.TaskDefinitionName,
                        DesiredCount = config.DesiredCount,
                        LaunchType = LaunchType.FARGATE,
                        DeploymentConfiguration = new DeploymentConfiguration
                        {
                            MaximumPercent = 200,
                            MinimumHealthyPercent = 100,
                            DeploymentCircuitBreaker = new DeploymentCircuitBreaker
                            {
                                Enable = true,
                                Rollback = true
                            }
                        },
                        NetworkConfiguration = new NetworkConfiguration
                        {
                            AwsvpcConfiguration = new AwsVpcConfiguration
                            {
                                Subnets = config.SubnetIds,
                                SecurityGroups = !string.IsNullOrEmpty(config.SecurityGroupId) 
                                    ? new List<string> { config.SecurityGroupId } 
                                    : null,
                                AssignPublicIp = AssignPublicIp.ENABLED
                            }
                        },
                        Tags = config.Tags.Select(t => new Tag { Key = t.Key, Value = t.Value }).ToList()
                    };
                    
                    // 如果启用了负载均衡器
                    if (config.EnableLoadBalancer && 
                        !string.IsNullOrEmpty(config.LoadBalancerArn) && 
                        !string.IsNullOrEmpty(config.TargetGroupArn))
                    {
                        createRequest.LoadBalancers = new List<LoadBalancer>
                        {
                            new LoadBalancer
                            {
                                TargetGroupArn = config.TargetGroupArn,
                                ContainerName = config.ContainerName,
                                ContainerPort = config.ContainerPorts.FirstOrDefault()
                            }
                        };
                    }
                    
                    var createResponse = await _ecsClient.CreateServiceAsync(createRequest, ct);
                    
                    _logger?.LogInformation($"Created service: {createResponse.Service.ServiceArn}");
                    return createResponse.Service.ServiceArn;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error creating/updating service {config.ServiceName}");
                throw;
            }
        }
        
        public async Task<bool> DeployServiceAsync(EcsDeployConfig config, CancellationToken ct = default)
        {
            try
            {
                _logger?.LogInformation($"Starting deployment of service: {config.ServiceName}");
                
                // 1. 检查AWS凭证
                if (!await CheckAwsCredentialsAsync(ct))
                {
                    throw new InvalidOperationException("AWS credentials are invalid");
                }
                
                // 2. 创建或更新集群
                var clusterArn = await CreateOrUpdateClusterAsync(config.ClusterName, ct);
                
                // 3. 创建任务定义
                var taskDefinitionArn = await CreateTaskDefinitionAsync(config, ct);
                
                // 4. 创建或更新服务
                var serviceArn = await CreateOrUpdateServiceAsync(config, ct);
                
                // 5. 等待服务稳定
                _logger?.LogInformation("Waiting for service to become stable...");
                
                var stable = await WaitForServiceStableAsync(config.ServiceName, config.ClusterName, 
                    TimeSpan.FromMinutes(10), ct);
                
                if (stable)
                {
                    _logger?.LogInformation($"Service {config.ServiceName} is now stable and running");
                    
                    // 6. 获取服务状态
                    var serviceStatus = await GetServiceStatusAsync(config.ServiceName, config.ClusterName, ct);
                    
                    _logger?.LogInformation($"Service details:\n" +
                        $"  Status: {serviceStatus.Status}\n" +
                        $"  Desired Count: {serviceStatus.DesiredCount}\n" +
                        $"  Running Count: {serviceStatus.RunningCount}\n" +
                        $"  Pending Count: {serviceStatus.PendingCount}");
                    
                    return true;
                }
                else
                {
                    _logger?.LogError($"Service {config.ServiceName} failed to become stable");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Deployment of service {config.ServiceName} failed");
                return false;
            }
        }
        
        public async Task<string> PushImageToEcrAsync(string imageName, string repositoryName, 
            string region, CancellationToken ct = default)
        {
            try
            {
                _logger?.LogInformation($"Pushing image {imageName} to ECR repository {repositoryName}");
                
                // 1. 检查ECR仓库是否存在，不存在则创建
                DescribeRepositoriesResponse describeResponse;
                try
                {
                    describeResponse = await _ecrClient.DescribeRepositoriesAsync(
                        new DescribeRepositoriesRequest 
                        { 
                            RepositoryNames = new List<string> { repositoryName } 
                        }, ct);
                }
                catch (RepositoryNotFoundException)
                {
                    // 创建新仓库
                    var createResponse = await _ecrClient.CreateRepositoryAsync(
                        new CreateRepositoryRequest 
                        { 
                            RepositoryName = repositoryName,
                            ImageTagMutability = ImageTagMutability.MUTABLE,
                            ImageScanningConfiguration = new ImageScanningConfiguration
                            {
                                ScanOnPush = true
                            },
                            Tags = new List<Tag>
                            {
                                new Tag { Key = "CreatedBy", Value = "DockerDeploy" }
                            }
                        }, ct);
                    
                    describeResponse = new DescribeRepositoriesResponse
                    {
                        Repositories = new List<Repository> { createResponse.Repository }
                    };
                }
                
                var repositoryUri = describeResponse.Repositories[0].RepositoryUri;
                
                // 2. 获取ECR登录命令
                var authResponse = await _ecrClient.GetAuthorizationTokenAsync(
                    new GetAuthorizationTokenRequest(), ct);
                
                var authToken = authResponse.AuthorizationData[0].AuthorizationToken;
                var decodedToken = Convert.FromBase64String(authToken);
                var tokenString = System.Text.Encoding.UTF8.GetString(decodedToken);
                var credentials = tokenString.Split(':');
                
                // 3. 使用Docker命令推送镜像
                var ecrImageName = $"{repositoryUri}:latest";
                
                // 标记镜像
                var tagCommand = $"docker tag {imageName} {ecrImageName}";
                await ExecuteShellCommandAsync(tagCommand, ct);
                
                // 登录ECR
                var loginCommand = $"docker login -u {credentials[0]} -p {credentials[1]} {repositoryUri}";
                await ExecuteShellCommandAsync(loginCommand, ct);
                
                // 推送镜像
                var pushCommand = $"docker push {ecrImageName}";
                await ExecuteShellCommandAsync(pushCommand, ct);
                
                _logger?.LogInformation($"Successfully pushed image to ECR: {ecrImageName}");
                return ecrImageName;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error pushing image to ECR");
                throw;
            }
        }
        
        private async Task<bool> WaitForServiceStableAsync(string serviceName, string clusterName, 
            TimeSpan timeout, CancellationToken ct)
        {
            var startTime = DateTime.UtcNow;
            
            while (DateTime.UtcNow - startTime < timeout)
            {
                try
                {
                    var service = await GetServiceStatusAsync(serviceName, clusterName, ct);
                    
                    if (service == null)
                    {
                        _logger?.LogWarning($"Service {serviceName} not found");
                        return false;
                    }
                    
                    // 检查服务是否稳定
                    if (service.Status == "ACTIVE" && 
                        service.DesiredCount == service.RunningCount &&
                        service.PendingCount == 0)
                    {
                        _logger?.LogInformation($"Service {serviceName} is stable");
                        return true;
                    }
                    
                    _logger?.LogInformation($"Waiting for service to stabilize... " +
                        $"(Desired: {service.DesiredCount}, Running: {service.RunningCount}, Pending: {service.PendingCount})");
                    
                    await Task.Delay(TimeSpan.FromSeconds(10), ct);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, $"Error checking service stability");
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                }
            }
            
            return false;
        }
        
        private async Task ExecuteShellCommandAsync(string command, CancellationToken ct)
        {
            using var process = new System.Diagnostics.Process();
            process.StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            process.Start();
            await process.WaitForExitAsync(ct);
            
            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new InvalidOperationException($"Command failed: {command}\nError: {error}");
            }
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                _ecsClient?.Dispose();
                _ecrClient?.Dispose();
                _iamClient?.Dispose();
                _cloudFormationClient?.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}