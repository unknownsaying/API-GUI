# Deployment Automation GUI

一个使用Visual Basic .NET开发的跨平台部署自动化图形界面，支持Docker、Docker Compose、AWS ECS和Windows安装程序构建。

## 功能特性

### 🐋 Docker 支持
- 构建Docker镜像
- 管理Docker Compose服务
- 查看Docker镜像和容器列表
- 一键启动/停止服务

### ☁️ AWS ECS 部署
- 自动部署到Amazon ECS
- ECR镜像推送
- 服务状态监控
- 回滚支持

### 📦 Windows 安装程序
- 自动生成Inno Setup脚本
- 构建Windows安装程序
- 数字签名支持
- 自定义配置

### 🎨 用户界面
- 现代化选项卡界面
- 实时日志查看
- 暗黑/明亮主题
- 项目保存/加载

## 系统要求

### 软件要求
- .NET 8.0 Runtime 或 SDK
- Docker Desktop (Windows/Mac) 或 Docker Engine (Linux)
- AWS CLI (用于ECS部署)
- Inno Setup (用于安装程序构建)

### 操作系统
- Windows 10/11 (推荐)
- macOS 10.15+
- Linux (Ubuntu 20.04+, CentOS 8+)

## 安装

### Windows
1. 从Releases页面下载安装程序
2. 运行安装程序并按照向导完成安装
3. 启动"Deployment Automation GUI"

### macOS/Linux
```bash
# 克隆仓库
git clone https://github.com/unknownsaying/API-GUI.git
cd DeploymentAutomationGUI

# 构建应用程序
./deploy.sh

# 运行应用程序
cd publish-linux-x64
./DeploymentAutomationGUI