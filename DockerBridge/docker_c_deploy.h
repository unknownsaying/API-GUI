#ifndef DOCKER_C_DEPLOY_H
#define DOCKER_C_DEPLOY_H

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <sys/wait.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <jansson.h>  // JSON解析库

#ifdef _WIN32
#include <windows.h>
#define PATH_SEPARATOR '\\'
#else
#define PATH_SEPARATOR '/'
#endif

#define MAX_COMMAND_LENGTH 4096
#define MAX_BUFFER_SIZE 8192
#define DOCKER_COMPOSE_FILE "docker-compose.yml"
#define DOCKERFILE_NAME "Dockerfile"

// 部署状态枚举
typedef enum {
    DEPLOY_SUCCESS = 0,
    DEPLOY_FAILED = 1,
    DEPLOY_DOCKER_NOT_FOUND = 2,
    DEPLOY_COMPOSE_NOT_FOUND = 3,
    DEPLOY_FILE_NOT_FOUND = 4,
    DEPLOY_TIMEOUT = 5,
    DEPLOY_NETWORK_ERROR = 6
} DeployStatus;

// Docker容器信息结构
typedef struct {
    char container_id[65];
    char name[256];
    char image[256];
    char status[50];
    char ports[512];
    char created[50];
} DockerContainer;

// Docker镜像信息结构
typedef struct {
    char repository[256];
    char tag[100];
    char image_id[65];
    char created[50];
    size_t size;
} DockerImage;

// 部署配置结构
typedef struct {
    char project_name[256];
    char dockerfile_path[512];
    char compose_file_path[512];
    char build_context[512];
    char registry_url[512];
    char registry_username[256];
    char registry_password[256];
    int pull_on_deploy;
    int prune_after_deploy;
    int timeout_seconds;
} DeployConfig;

// 函数声明
DeployStatus check_docker_installation();
DeployStatus check_docker_compose_installation();
DeployStatus build_docker_image(const char* dockerfile, const char* tag, const char* context_path, char** output);
DeployStatus push_docker_image(const char* image_name, const char* registry_url, 
                               const char* username, const char* password, char** output);
DeployStatus run_docker_compose(const char* compose_file, const char* command, char** output);
DeployStatus docker_compose_up(const char* compose_file, int detached, char** output);
DeployStatus docker_compose_down(const char* compose_file, char** output);
DeployStatus docker_compose_ps(const char* compose_file, DockerContainer*** containers, int* count);
DeployStatus docker_compose_logs(const char* compose_file, const char* service_name, char** output);
DeployStatus list_docker_containers(int show_all, DockerContainer*** containers, int* count);
DeployStatus list_docker_images(DockerImage*** images, int* count);
DeployStatus remove_docker_containers(const char** container_ids, int count);
DeployStatus remove_docker_images(const char** image_ids, int count);
DeployStatus cleanup_docker_system(int remove_images, int remove_volumes);

// 辅助函数
int execute_command(const char* command, char** output, int timeout);
char* create_docker_compose_file(const char* services_config);
char* create_dockerfile(const char* base_image, const char* instructions);
void free_docker_containers(DockerContainer** containers, int count);
void free_docker_images(DockerImage** images, int count);

#endif // DOCKER_C_DEPLOY_H