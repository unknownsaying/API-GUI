#ifndef DOCKER_CPP_DEPLOY_H
#define DOCKER_CPP_DEPLOY_H

#include <iostream>
#include <string>
#include <vector>
#include <memory>
#include <thread>
#include <future>
#include <chrono>
#include <map>
#include <sstream>
#include <fstream>
#include <iomanip>

#ifdef _WIN32
#include <windows.h>
#else
#include <unistd.h>
#include <sys/wait.h>
#endif

namespace DockerDeploy {

// 异常类
class DockerException : public std::runtime_error {
public:
    explicit DockerException(const std::string& message) 
        : std::runtime_error(message) {}
};

// Docker容器信息
struct ContainerInfo {
    std::string id;
    std::string name;
    std::string image;
    std::string status;
    std::vector<std::string> ports;
    std::string created;
    std::string state;
    std::string command;
    
    std::string to_string() const {
        std::ostringstream oss;
        oss << "Container: " << name << " (" << id.substr(0, 12) << ")\n"
            << "  Image: " << image << "\n"
            << "  Status: " << status << "\n"
            << "  State: " << state << "\n"
            << "  Created: " << created << "\n";
        return oss.str();
    }
};

// Docker镜像信息
struct ImageInfo {
    std::string repository;
    std::string tag;
    std::string imageId;
    std::string created;
    size_t size;
    
    std::string to_string() const {
        std::ostringstream oss;
        oss << "Image: " << repository << ":" << tag << "\n"
            << "  ID: " << imageId.substr(0, 12) << "\n"
            << "  Created: " << created << "\n"
            << "  Size: " << (size / (1024 * 1024)) << " MB\n";
        return oss.str();
    }
};

// Docker网络信息
struct NetworkInfo {
    std::string id;
    std::string name;
    std::string driver;
    std::string scope;
    
    std::string to_string() const {
        return "Network: " + name + " (" + driver + ")";
    }
};

// Docker卷信息
struct VolumeInfo {
    std::string name;
    std::string driver;
    std::string mountpoint;
    
    std::string to_string() const {
        return "Volume: " + name + " (" + driver + ")";
    }
};

// Docker Compose服务配置
struct ComposeService {
    std::string name;
    std::string image;
    std::string build_context;
    std::vector<std::string> ports;
    std::vector<std::string> environment;
    std::vector<std::string> depends_on;
    std::map<std::string, std::string> volumes;
    std::vector<std::string> networks;
    std::string restart_policy;
    
    std::string to_yaml(int indent = 2) const {
        std::ostringstream oss;
        std::string indent_str(indent, ' ');
        
        oss << "  " << name << ":\n";
        
        if (!image.empty()) {
            oss << indent_str << "image: " << image << "\n";
        }
        
        if (!build_context.empty()) {
            oss << indent_str << "build:\n";
            oss << indent_str << indent_str << "context: " << build_context << "\n";
        }
        
        if (!ports.empty()) {
            oss << indent_str << "ports:\n";
            for (const auto& port : ports) {
                oss << indent_str << indent_str << "- \"" << port << "\"\n";
            }
        }
        
        if (!environment.empty()) {
            oss << indent_str << "environment:\n";
            for (const auto& env : environment) {
                oss << indent_str << indent_str << "- " << env << "\n";
            }
        }
        
        if (!depends_on.empty()) {
            oss << indent_str << "depends_on:\n";
            for (const auto& dep : depends_on) {
                oss << indent_str << indent_str << "- " << dep << "\n";
            }
        }
        
        if (!volumes.empty()) {
            oss << indent_str << "volumes:\n";
            for (const auto& vol : volumes) {
                oss << indent_str << indent_str << "- " << vol.first << ":" << vol.second << "\n";
            }
        }
        
        if (!restart_policy.empty()) {
            oss << indent_str << "restart: " << restart_policy << "\n";
        }
        
        return oss.str();
    }
};

// Docker部署配置
class DeployConfig {
public:
    std::string project_name;
    std::string dockerfile_path;
    std::string compose_file_path;
    std::string build_context;
    std::string registry_url;
    std::string registry_username;
    std::string registry_password;
    bool pull_on_deploy;
    bool prune_after_deploy;
    int timeout_seconds;
    std::vector<ComposeService> services;
    
    DeployConfig() 
        : pull_on_deploy(false), 
          prune_after_deploy(false), 
          timeout_seconds(300) {}
    
    std::string generate_compose_yaml() const {
        std::ostringstream oss;
        
        oss << "version: '3.8'\n\n";
        oss << "services:\n";
        
        for (const auto& service : services) {
            oss << service.to_yaml() << "\n";
        }
        
        // 网络配置
        oss << "networks:\n";
        oss << "  default:\n";
        oss << "    driver: bridge\n";
        
        return oss.str();
    }
    
    bool save_compose_file(const std::string& filepath = "") const {
        std::string path = filepath.empty() ? compose_file_path : filepath;
        if (path.empty()) path = "docker-compose.yml";
        
        std::ofstream file(path);
        if (!file.is_open()) {
            return false;
        }
        
        file << generate_compose_yaml();
        file.close();
        
        return true;
    }
};

// 命令执行结果
struct CommandResult {
    int exit_code;
    std::string output;
    std::string error;
    bool timed_out;
    
    bool success() const { return exit_code == 0 && !timed_out; }
};

// Docker客户端基类
class DockerClient {
protected:
    std::string docker_path;
    std::string compose_path;
    
public:
    DockerClient() {
        docker_path = "docker";
        compose_path = "docker-compose";
    }
    
    virtual ~DockerClient() = default;
    
    virtual CommandResult execute(const std::string& command, int timeout_seconds = 300) = 0;
    
    std::string get_docker_path() const { return docker_path; }
    std::string get_compose_path() const { return compose_path; }
};

// 跨平台Docker客户端实现
class DockerClientImpl : public DockerClient {
private:
#ifdef _WIN32
    HANDLE create_pipe(PHANDLE read_pipe, PHANDLE write_pipe) {
        SECURITY_ATTRIBUTES sa;
        sa.nLength = sizeof(SECURITY_ATTRIBUTES);
        sa.bInheritHandle = TRUE;
        sa.lpSecurityDescriptor = NULL;
        
        if (!CreatePipe(read_pipe, write_pipe, &sa, 0)) {
            throw DockerException("Failed to create pipe");
        }
        
        // 确保读管道不可继承
        SetHandleInformation(*read_pipe, HANDLE_FLAG_INHERIT, 0);
        
        return *write_pipe;
    }
#endif

public:
    CommandResult execute(const std::string& command, int timeout_seconds = 300) override {
        CommandResult result;
        result.timed_out = false;
        
#ifdef _WIN32
        // Windows实现
        SECURITY_ATTRIBUTES sa;
        HANDLE hRead, hWrite;
        PROCESS_INFORMATION pi;
        STARTUPINFO si;
        
        sa.nLength = sizeof(SECURITY_ATTRIBUTES);
        sa.bInheritHandle = TRUE;
        sa.lpSecurityDescriptor = NULL;
        
        if (!CreatePipe(&hRead, &hWrite, &sa, 0)) {
            result.exit_code = -1;
            result.error = "Failed to create pipe";
            return result;
        }
        
        // 确保读管道不可继承
        SetHandleInformation(hRead, HANDLE_FLAG_INHERIT, 0);
        
        ZeroMemory(&pi, sizeof(PROCESS_INFORMATION));
        ZeroMemory(&si, sizeof(STARTUPINFO));
        si.cb = sizeof(STARTUPINFO);
        si.hStdError = hWrite;
        si.hStdOutput = hWrite;
        si.hStdInput = GetStdHandle(STD_INPUT_HANDLE);
        si.dwFlags |= STARTF_USESTDHANDLES;
        
        std::string cmd = "cmd.exe /C " + command;
        char* cmd_cstr = new char[cmd.length() + 1];
        strcpy(cmd_cstr, cmd.c_str());
        
        if (!CreateProcess(NULL, cmd_cstr, NULL, NULL, TRUE, 
                           CREATE_NO_WINDOW, NULL, NULL, &si, &pi)) {
            delete[] cmd_cstr;
            CloseHandle(hRead);
            CloseHandle(hWrite);
            result.exit_code = -1;
            result.error = "Failed to create process";
            return result;
        }
        
        delete[] cmd_cstr;
        CloseHandle(hWrite);
        
        // 读取输出
        DWORD bytesRead;
        CHAR buffer[4096];
        std::string output;
        
        auto start_time = std::chrono::steady_clock::now();
        
        while (true) {
            DWORD wait_result = WaitForSingleObject(pi.hProcess, 100);
            
            if (wait_result == WAIT_OBJECT_0) {
                break;
            }
            
            auto elapsed = std::chrono::duration_cast<std::chrono::seconds>(
                std::chrono::steady_clock::now() - start_time);
            
            if (elapsed.count() > timeout_seconds) {
                result.timed_out = true;
                TerminateProcess(pi.hProcess, 1);
                break;
            }
            
            // 检查管道是否有数据
            DWORD bytesAvailable = 0;
            if (PeekNamedPipe(hRead, NULL, 0, NULL, &bytesAvailable, NULL) && 
                bytesAvailable > 0) {
                if (ReadFile(hRead, buffer, sizeof(buffer) - 1, &bytesRead, NULL)) {
                    buffer[bytesRead] = '\0';
                    output += buffer;
                }
            }
        }
        
        if (!result.timed_out) {
            // 读取剩余数据
            while (PeekNamedPipe(hRead, NULL, 0, NULL, &bytesAvailable, NULL) && 
                   bytesAvailable > 0) {
                if (ReadFile(hRead, buffer, sizeof(buffer) - 1, &bytesRead, NULL)) {
                    buffer[bytesRead] = '\0';
                    output += buffer;
                }
            }
            
            GetExitCodeProcess(pi.hProcess, (LPDWORD)&result.exit_code);
        }
        
        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);
        CloseHandle(hRead);
        
        result.output = output;
        
#else
        // Linux/macOS实现
        std::string full_command = command + " 2>&1";
        
        FILE* pipe = popen(full_command.c_str(), "r");
        if (!pipe) {
            result.exit_code = -1;
            result.error = "Failed to open pipe";
            return result;
        }
        
        // 异步执行
        auto future = std::async(std::launch::async, [pipe, &result, timeout_seconds]() {
            char buffer[4096];
            std::string output;
            
            auto start_time = std::chrono::steady_clock::now();
            
            while (!feof(pipe)) {
                auto elapsed = std::chrono::duration_cast<std::chrono::seconds>(
                    std::chrono::steady_clock::now() - start_time);
                
                if (elapsed.count() > timeout_seconds) {
                    result.timed_out = true;
                    pclose(pipe);
                    return output;
                }
                
                if (fgets(buffer, sizeof(buffer), pipe) != NULL) {
                    output += buffer;
                }
            }
            
            result.exit_code = pclose(pipe);
            return output;
        });
        
        // 等待完成或超时
        if (future.wait_for(std::chrono::seconds(timeout_seconds)) == std::future_status::timeout) {
            result.timed_out = true;
            result.output = "Command timed out";
        } else {
            result.output = future.get();
        }
#endif
        
        return result;
    }
};

// Docker管理器
class DockerManager {
private:
    std::shared_ptr<DockerClient> client;
    DeployConfig config;
    
public:
    DockerManager() : client(std::make_shared<DockerClientImpl>()) {}
    
    DockerManager(const DeployConfig& cfg) 
        : client(std::make_shared<DockerClientImpl>()), config(cfg) {}
    
    // 检查Docker环境
    bool check_environment() {
        try {
            auto result = client->execute("docker --version", 10);
            if (!result.success()) {
                std::cerr << "Docker not found or not accessible" << std::endl;
                return false;
            }
            
            result = client->execute("docker-compose --version", 10);
            if (!result.success()) {
                std::cout << "Warning: Docker Compose not found" << std::endl;
            }
            
            return true;
        } catch (const std::exception& e) {
            std::cerr << "Error checking environment: " << e.what() << std::endl;
            return false;
        }
    }
    
    // 构建镜像
    bool build_image(const std::string& dockerfile, 
                     const std::string& tag, 
                     const std::string& context = ".", 
                     const std::vector<std::string>& build_args = {}) {
        
        std::string command = docker_path() + " build";
        command += " -f \"" + dockerfile + "\"";
        command += " -t \"" + tag + "\"";
        
        for (const auto& arg : build_args) {
            command += " --build-arg " + arg;
        }
        
        command += " \"" + context + "\"";
        
        auto result = client->execute(command, config.timeout_seconds);
        
        if (result.success()) {
            std::cout << "Image built successfully: " << tag << std::endl;
            return true;
        } else {
            std::cerr << "Failed to build image: " << result.output << std::endl;
            return false;
        }
    }
    
    // 推送镜像到仓库
    bool push_image(const std::string& image_name, 
                    const std::string& registry = "") {
        
        std::string full_image_name = image_name;
        if (!registry.empty()) {
            full_image_name = registry + "/" + image_name;
            
            // 如果需要，先标记镜像
            std::string tag_command = docker_path() + " tag " + image_name + " " + full_image_name;
            auto tag_result = client->execute(tag_command, 60);
            
            if (!tag_result.success()) {
                std::cerr << "Failed to tag image: " << tag_result.output << std::endl;
                return false;
            }
        }
        
        std::string command = docker_path() + " push " + full_image_name;
        auto result = client->execute(command, 600);  // 10分钟超时
        
        if (result.success()) {
            std::cout << "Image pushed successfully: " << full_image_name << std::endl;
            return true;
        } else {
            std::cerr << "Failed to push image: " << result.output << std::endl;
            return false;
        }
    }
    
    // 运行Docker Compose
    bool compose_up(bool detached = true, 
                    const std::vector<std::string>& services = {}) {
        
        if (!config.compose_file_path.empty()) {
            if (!std::ifstream(config.compose_file_path)) {
                std::cerr << "Docker compose file not found: " << config.compose_file_path << std::endl;
                return false;
            }
        }
        
        std::string command = compose_path();
        if (!config.compose_file_path.empty()) {
            command += " -f \"" + config.compose_file_path + "\"";
        }
        
        command += " up";
        
        if (detached) {
            command += " -d";
        }
        
        if (!services.empty()) {
            for (const auto& service : services) {
                command += " " + service;
            }
        }
        
        if (config.pull_on_deploy) {
            command += " --pull always";
        }
        
        auto result = client->execute(command, config.timeout_seconds);
        
        if (result.success()) {
            std::cout << "Docker Compose services started successfully" << std::endl;
            return true;
        } else {
            std::cerr << "Failed to start Docker Compose services: " << result.output << std::endl;
            return false;
        }
    }
    
    // 停止Docker Compose
    bool compose_down(bool remove_volumes = false) {
        std::string command = compose_path();
        if (!config.compose_file_path.empty()) {
            command += " -f \"" + config.compose_file_path + "\"";
        }
        
        command += " down";
        
        if (remove_volumes) {
            command += " -v";
        }
        
        auto result = client->execute(command, 180);
        
        if (result.success()) {
            std::cout << "Docker Compose services stopped" << std::endl;
            return true;
        } else {
            std::cerr << "Failed to stop Docker Compose services: " << result.output << std::endl;
            return false;
        }
    }
    
    // 列出容器
    std::vector<ContainerInfo> list_containers(bool all = false) {
        std::vector<ContainerInfo> containers;
        
        std::string command = docker_path() + " ps";
        if (all) command += " -a";
        command += " --format \"{{.ID}}|{{.Names}}|{{.Image}}|{{.Status}}|{{.Ports}}|{{.CreatedAt}}\"";
        
        auto result = client->execute(command, 30);
        
        if (result.success()) {
            std::istringstream iss(result.output);
            std::string line;
            
            while (std::getline(iss, line)) {
                if (line.empty()) continue;
                
                std::vector<std::string> tokens;
                std::string token;
                std::istringstream token_stream(line);
                
                while (std::getline(token_stream, token, '|')) {
                    tokens.push_back(token);
                }
                
                if (tokens.size() >= 6) {
                    ContainerInfo info;
                    info.id = tokens[0];
                    info.name = tokens[1];
                    info.image = tokens[2];
                    info.status = tokens[3];
                    info.ports = split_string(tokens[4], ',');
                    info.created = tokens[5];
                    
                    containers.push_back(info);
                }
            }
        }
        
        return containers;
    }
    
    // 清理Docker资源
    void cleanup(bool remove_images = false, bool remove_volumes = false) {
        std::cout << "Cleaning up Docker resources..." << std::endl;
        
        // 停止所有容器
        client->execute(docker_path() + " stop $(docker ps -aq)", 60);
        
        // 删除所有容器
        client->execute(docker_path() + " rm $(docker ps -aq)", 60);
        
        // 清理网络
        client->execute(docker_path() + " network prune -f", 30);
        
        if (remove_images) {
            // 删除所有镜像
            client->execute(docker_path() + " rmi $(docker images -q) -f", 120);
        }
        
        if (remove_volumes) {
            // 清理卷
            client->execute(docker_path() + " volume prune -f", 30);
        }
        
        // 系统级清理
        client->execute(docker_path() + " system prune -f", 60);
        
        std::cout << "Cleanup completed" << std::endl;
    }
    
private:
    std::vector<std::string> split_string(const std::string& str, char delimiter) {
        std::vector<std::string> tokens;
        std::string token;
        std::istringstream token_stream(str);
        
        while (std::getline(token_stream, token, delimiter)) {
            if (!token.empty()) {
                tokens.push_back(token);
            }
        }
        
        return tokens;
    }
    
    std::string docker_path() { return client->get_docker_path(); }
    std::string compose_path() { return client->get_compose_path(); }
};

} // namespace DockerDeploy

#endif // DOCKER_CPP_DEPLOY_H