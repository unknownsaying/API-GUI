#include "docker_c_deploy.h"

// 执行shell命令并获取输出
int execute_command(const char* command, char** output, int timeout) {
#ifdef _WIN32
    SECURITY_ATTRIBUTES sa;
    HANDLE hRead, hWrite;
    PROCESS_INFORMATION pi;
    STARTUPINFO si;
    char cmd[MAX_COMMAND_LENGTH];
    DWORD bytesRead, exitCode;
    char buffer[MAX_BUFFER_SIZE];
    char* result = NULL;
    size_t totalSize = 0;
    int timed_out = 0;
    
    sa.nLength = sizeof(SECURITY_ATTRIBUTES);
    sa.bInheritHandle = TRUE;
    sa.lpSecurityDescriptor = NULL;
    
    if (!CreatePipe(&hRead, &hWrite, &sa, 0)) {
        return -1;
    }
    
    ZeroMemory(&pi, sizeof(PROCESS_INFORMATION));
    ZeroMemory(&si, sizeof(STARTUPINFO));
    si.cb = sizeof(STARTUPINFO);
    si.hStdError = hWrite;
    si.hStdOutput = hWrite;
    si.dwFlags |= STARTF_USESTDHANDLES;
    
    snprintf(cmd, MAX_COMMAND_LENGTH, "cmd.exe /C %s", command);
    
    if (!CreateProcess(NULL, cmd, NULL, NULL, TRUE, 0, NULL, NULL, &si, &pi)) {
        CloseHandle(hRead);
        CloseHandle(hWrite);
        return -1;
    }
    
    CloseHandle(hWrite);
    
    // 设置超时
    DWORD startTime = GetTickCount();
    
    while (1) {
        DWORD waitResult = WaitForSingleObject(pi.hProcess, 100);
        
        if (waitResult == WAIT_OBJECT_0) {
            break;
        }
        
        if (GetTickCount() - startTime > (DWORD)timeout * 1000) {
            timed_out = 1;
            TerminateProcess(pi.hProcess, 1);
            break;
        }
        
        // 读取输出
        if (PeekNamedPipe(hRead, NULL, 0, NULL, &bytesRead, NULL) && bytesRead > 0) {
            ReadFile(hRead, buffer, sizeof(buffer) - 1, &bytesRead, NULL);
            buffer[bytesRead] = '\0';
            
            char* newResult = realloc(result, totalSize + bytesRead + 1);
            if (!newResult) {
                free(result);
                CloseHandle(hRead);
                CloseHandle(pi.hProcess);
                CloseHandle(pi.hThread);
                return -1;
            }
            
            result = newResult;
            strcpy(result + totalSize, buffer);
            totalSize += bytesRead;
        }
    }
    
    if (!timed_out) {
        GetExitCodeProcess(pi.hProcess, &exitCode);
    }
    
    CloseHandle(pi.hProcess);
    CloseHandle(pi.hThread);
    CloseHandle(hRead);
    
    if (output) {
        *output = result;
    } else {
        free(result);
    }
    
    return timed_out ? -2 : (int)exitCode;
    
#else
    // Unix/Linux实现
    FILE* fp;
    char buffer[MAX_BUFFER_SIZE];
    char* result = NULL;
    size_t totalSize = 0;
    
    fp = popen(command, "r");
    if (fp == NULL) {
        return -1;
    }
    
    // 设置超时（使用alarm信号）
    signal(SIGALRM, SIG_IGN);
    alarm(timeout > 0 ? timeout : 0);
    
    while (fgets(buffer, sizeof(buffer), fp) != NULL) {
        size_t len = strlen(buffer);
        char* newResult = realloc(result, totalSize + len + 1);
        if (!newResult) {
            free(result);
            pclose(fp);
            return -1;
        }
        
        result = newResult;
        strcpy(result + totalSize, buffer);
        totalSize += len;
    }
    
    alarm(0);  // 取消超时
    
    int status = pclose(fp);
    
    if (output) {
        *output = result;
    } else {
        free(result);
    }
    
    return WEXITSTATUS(status);
#endif
}

// 检查Docker是否安装
DeployStatus check_docker_installation() {
    char* output = NULL;
    int result = execute_command("docker --version", &output, 10);
    
    if (output) {
        free(output);
    }
    
    if (result == 0) {
        return DEPLOY_SUCCESS;
    } else {
        return DEPLOY_DOCKER_NOT_FOUND;
    }
}

// 检查Docker Compose是否安装
DeployStatus check_docker_compose_installation() {
    char* output = NULL;
    int result = execute_command("docker-compose --version", &output, 10);
    
    if (output) {
        free(output);
    }
    
    if (result == 0) {
        return DEPLOY_SUCCESS;
    } else {
        return DEPLOY_COMPOSE_NOT_FOUND;
    }
}

// 构建Docker镜像
DeployStatus build_docker_image(const char* dockerfile, const char* tag, const char* context_path, char** output) {
    char command[MAX_COMMAND_LENGTH];
    
    if (!dockerfile || !tag) {
        return DEPLOY_FAILED;
    }
    
    // 检查Dockerfile是否存在
    FILE* f = fopen(dockerfile, "r");
    if (!f) {
        return DEPLOY_FILE_NOT_FOUND;
    }
    fclose(f);
    
    // 构建命令
    if (context_path) {
        snprintf(command, MAX_COMMAND_LENGTH, "docker build -f \"%s\" -t \"%s\" \"%s\"", 
                dockerfile, tag, context_path);
    } else {
        snprintf(command, MAX_COMMAND_LENGTH, "docker build -f \"%s\" -t \"%s\" .", 
                dockerfile, tag);
    }
    
    int result = execute_command(command, output, 300);  // 5分钟超时
    
    if (result == 0) {
        return DEPLOY_SUCCESS;
    } else if (result == -2) {
        return DEPLOY_TIMEOUT;
    } else {
        return DEPLOY_FAILED;
    }
}

// 运行Docker Compose
DeployStatus run_docker_compose(const char* compose_file, const char* command, char** output) {
    char full_command[MAX_COMMAND_LENGTH];
    
    if (!compose_file || !command) {
        return DEPLOY_FAILED;
    }
    
    // 检查docker-compose文件是否存在
    FILE* f = fopen(compose_file, "r");
    if (!f) {
        return DEPLOY_FILE_NOT_FOUND;
    }
    fclose(f);
    
    snprintf(full_command, MAX_COMMAND_LENGTH, "docker-compose -f \"%s\" %s", compose_file, command);
    
    int result = execute_command(full_command, output, 180);  // 3分钟超时
    
    if (result == 0) {
        return DEPLOY_SUCCESS;
    } else if (result == -2) {
        return DEPLOY_TIMEOUT;
    } else {
        return DEPLOY_FAILED;
    }
}

// 启动Docker Compose服务
DeployStatus docker_compose_up(const char* compose_file, int detached, char** output) {
    char command[256];
    
    if (detached) {
        snprintf(command, sizeof(command), "up -d");
    } else {
        snprintf(command, sizeof(command), "up");
    }
    
    return run_docker_compose(compose_file, command, output);
}

// 停止Docker Compose服务
DeployStatus docker_compose_down(const char* compose_file, char** output) {
    return run_docker_compose(compose_file, "down", output);
}

// 创建docker-compose.yml文件
char* create_docker_compose_file(const char* services_config) {
    char* content = NULL;
    size_t content_size = 0;
    FILE* f = NULL;
    
    // 基础模板
    const char* base_template = 
        "version: '3.8'\n\n"
        "services:\n";
    
    if (!services_config) {
        // 创建默认配置
        const char* default_config = 
            "  web:\n"
            "    build: .\n"
            "    ports:\n"
            "      - \"5000:5000\"\n"
            "    environment:\n"
            "      - NODE_ENV=production\n"
            "    depends_on:\n"
            "      - db\n\n"
            "  db:\n"
            "    image: postgres:13\n"
            "    environment:\n"
            "      POSTGRES_DB: appdb\n"
            "      POSTGRES_USER: user\n"
            "      POSTGRES_PASSWORD: password\n"
            "    volumes:\n"
            "      - postgres_data:/var/lib/postgresql/data\n\n"
            "volumes:\n"
            "  postgres_data:\n";
        
        content_size = strlen(base_template) + strlen(default_config) + 1;
        content = malloc(content_size);
        if (content) {
            snprintf(content, content_size, "%s%s", base_template, default_config);
        }
    } else {
        content_size = strlen(base_template) + strlen(services_config) + 1;
        content = malloc(content_size);
        if (content) {
            snprintf(content, content_size, "%s%s", base_template, services_config);
        }
    }
    
    // 写入文件
    if (content) {
        f = fopen(DOCKER_COMPOSE_FILE, "w");
        if (f) {
            fwrite(content, 1, strlen(content), f);
            fclose(f);
        } else {
            free(content);
            content = NULL;
        }
    }
    
    return content;
}

// 清理Docker系统
DeployStatus cleanup_docker_system(int remove_images, int remove_volumes) {
    char* output = NULL;
    DeployStatus status = DEPLOY_SUCCESS;
    
    // 停止所有容器
    execute_command("docker stop $(docker ps -aq)", &output, 30);
    if (output) free(output);
    output = NULL;
    
    // 删除所有容器
    execute_command("docker rm $(docker ps -aq)", &output, 30);
    if (output) free(output);
    output = NULL;
    
    // 删除所有网络
    execute_command("docker network prune -f", &output, 30);
    if (output) free(output);
    output = NULL;
    
    if (remove_images) {
        // 删除悬空镜像
        execute_command("docker image prune -f", &output, 30);
        if (output) free(output);
        output = NULL;
        
        // 删除所有镜像
        execute_command("docker rmi $(docker images -q)", &output, 30);
        if (output) free(output);
        output = NULL;
    }
    
    if (remove_volumes) {
        // 删除所有卷
        execute_command("docker volume prune -f", &output, 30);
        if (output) free(output);
    }
    
    return status;
}

// 主部署函数示例
int main() {
    printf("=== Docker Deployment System in C ===\n\n");
    
    // 检查环境
    DeployStatus status = check_docker_installation();
    if (status != DEPLOY_SUCCESS) {
        printf("Error: Docker is not installed or not in PATH\n");
        return 1;
    }
    
    status = check_docker_compose_installation();
    if (status != DEPLOY_SUCCESS) {
        printf("Warning: Docker Compose is not installed\n");
    }
    
    // 创建docker-compose文件
    char* compose_content = create_docker_compose_file(NULL);
    if (compose_content) {
        printf("Created docker-compose.yml file\n");
        free(compose_content);
    }
    
    // 构建镜像
    char* build_output = NULL;
    status = build_docker_image("Dockerfile", "myapp:latest", ".", &build_output);
    
    if (status == DEPLOY_SUCCESS) {
        printf("Docker image built successfully\n");
    } else {
        printf("Failed to build Docker image\n");
        if (build_output) {
            printf("Output: %s\n", build_output);
            free(build_output);
        }
    }
    
    // 启动服务
    char* up_output = NULL;
    status = docker_compose_up(DOCKER_COMPOSE_FILE, 1, &up_output);
    
    if (status == DEPLOY_SUCCESS) {
        printf("Docker Compose services started successfully\n");
    } else {
        printf("Failed to start services\n");
    }
    
    if (up_output) free(up_output);
    
    return 0;
}