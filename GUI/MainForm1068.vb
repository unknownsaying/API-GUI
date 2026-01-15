Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Drawing
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports DeploymentAutomationGUI.Core
Imports DeploymentAutomationGUI.ViewModels
Imports DeploymentAutomationGUI.Services
Imports DeploymentAutomationGUI.Converters
Imports DeploymentAutomationGUI.Models

Namespace DeploymentAutomationGUI.Views
    Public Class MainForm
        Inherits Form
        
        ' ViewModel
        Private _viewModel As MainViewModel
        Private _deploymentService As DeploymentService
        
        ' 控件
        Private WithEvents tabControl As TabControl
        Private WithEvents statusStrip As StatusStrip
        Private WithEvents lblStatus As ToolStripStatusLabel
        Private WithEvents progressBar As ToolStripProgressBar
        
        ' 部署选项卡
        Private WithEvents dockerTab As TabPage
        Private WithEvents ecsTab As TabPage
        Private WithEvents installerTab As TabPage
        Private WithEvents logsTab As TabPage
        
        ' Docker 选项卡控件
        Private WithEvents pnlDocker As Panel
        Private WithEvents txtDockerfile As TextBox
        Private WithEvents txtComposeFile As TextBox
        Private WithEvents btnBrowseDockerfile As Button
        Private WithEvents btnBrowseCompose As Button
        Private WithEvents chkPullLatest As CheckBox
        Private WithEvents btnBuildImage As Button
        Private WithEvents btnComposeUp As Button
        Private WithEvents btnComposeDown As Button
        Private WithEvents lstDockerImages As ListBox
        Private WithEvents lstDockerContainers As ListBox
        
        ' ECS 选项卡控件
        Private WithEvents pnlEcs As Panel
        Private WithEvents txtEcsCluster As TextBox
        Private WithEvents txtEcsService As TextBox
        Private WithEvents txtEcsRegion As TextBox
        Private WithEvents txtEcrRepository As TextBox
        Private WithEvents btnDeployEcs As Button
        Private WithEvents btnCheckEcsStatus As Button
        Private WithEvents btnRollbackEcs As Button
        
        ' Installer 选项卡控件
        Private WithEvents pnlInstaller As Panel
        Private WithEvents txtAppName As TextBox
        Private WithEvents txtAppVersion As TextBox
        Private WithEvents txtOutputDir As TextBox
        Private WithEvents btnBrowseOutputDir As Button
        Private WithEvents btnGenerateScript As Button
        Private WithEvents btnBuildInstaller As Button
        Private WithEvents chkSignInstaller As CheckBox
        
        ' Logs 选项卡控件
        Private WithEvents pnlLogs As Panel
        Private WithEvents txtLogs As TextBox
        Private WithEvents btnClearLogs As Button
        Private WithEvents btnSaveLogs As Button
        Private WithEvents chkAutoScroll As CheckBox
        
        ' 菜单和工具栏
        Private WithEvents menuStrip As MenuStrip
        Private WithEvents fileMenu As ToolStripMenuItem
        Private WithEvents editMenu As ToolStripMenuItem
        Private WithEvents viewMenu As ToolStripMenuItem
        Private WithEvents toolsMenu As ToolStripMenuItem
        Private WithEvents helpMenu As ToolStripMenuItem
        
        Private WithEvents toolStrip As ToolStrip
        Private WithEvents btnNewProject As ToolStripButton
        Private WithEvents btnOpenProject As ToolStripButton
        Private WithEvents btnSaveProject As ToolStripButton
        Private WithEvents toolStripSeparator1 As ToolStripSeparator
        Private WithEvents btnStartDeployment As ToolStripButton
        Private WithEvents btnStopDeployment As ToolStripButton
        Private WithEvents toolStripSeparator2 As ToolStripSeparator
        Private WithEvents btnSettings As ToolStripButton
        
        Public Sub New()
            InitializeComponent()
            InitializeViewModel()
            ApplyTheme()
        End Sub
        
        Private Sub InitializeComponent()
            ' 窗体设置
            Me.Text = "Deployment Automation Studio"
            Me.Size = New Size(1200, 800)
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.Icon = My.Resources.AppIcon
            
            ' 创建菜单
            CreateMenuStrip()
            
            ' 创建工具栏
            CreateToolStrip()
            
            ' 创建选项卡控件
            CreateTabControl()
            
            ' 创建状态栏
            CreateStatusStrip()
            
            ' 初始化控件
            InitializeControls()
        End Sub
        
        Private Sub CreateMenuStrip()
            menuStrip = New MenuStrip()
            
            ' 文件菜单
            fileMenu = New ToolStripMenuItem("&File")
            fileMenu.DropDownItems.AddRange({
                New ToolStripMenuItem("&New Project", Nothing, AddressOf NewProject_Click, Keys.Control Or Keys.N),
                New ToolStripMenuItem("&Open Project...", Nothing, AddressOf OpenProject_Click, Keys.Control Or Keys.O),
                New ToolStripMenuItem("&Save Project", Nothing, AddressOf SaveProject_Click, Keys.Control Or Keys.S),
                New ToolStripMenuItem("Save Project &As...", Nothing, AddressOf SaveProjectAs_Click),
                New ToolStripSeparator(),
                New ToolStripMenuItem("E&xit", Nothing, AddressOf Exit_Click, Keys.Alt Or Keys.F4)
            })
            
            ' 编辑菜单
            editMenu = New ToolStripMenuItem("&Edit")
            editMenu.DropDownItems.AddRange({
                New ToolStripMenuItem("&Undo", Nothing, AddressOf Undo_Click, Keys.Control Or Keys.Z),
                New ToolStripMenuItem("&Redo", Nothing, AddressOf Redo_Click, Keys.Control Or Keys.Y),
                New ToolStripSeparator(),
                New ToolStripMenuItem("Cu&t", Nothing, AddressOf Cut_Click, Keys.Control Or Keys.X),
                New ToolStripMenuItem("&Copy", Nothing, AddressOf Copy_Click, Keys.Control Or Keys.C),
                New ToolStripMenuItem("&Paste", Nothing, AddressOf Paste_Click, Keys.Control Or Keys.V),
                New ToolStripMenuItem("&Delete", Nothing, AddressOf Delete_Click, Keys.Delete),
                New ToolStripSeparator(),
                New ToolStripMenuItem("&Find...", Nothing, AddressOf Find_Click, Keys.Control Or Keys.F),
                New ToolStripMenuItem("&Replace...", Nothing, AddressOf Replace_Click, Keys.Control Or Keys.H)
            })
            
            ' 视图菜单
            viewMenu = New ToolStripMenuItem("&View")
            viewMenu.DropDownItems.AddRange({
                New ToolStripMenuItem("&Toolbar", Nothing, AddressOf ToggleToolbar_Click) With {.Checked = True},
                New ToolStripMenuItem("&Status Bar", Nothing, AddressOf ToggleStatusBar_Click) With {.Checked = True},
                New ToolStripSeparator(),
                New ToolStripMenuItem("&Dark Theme", Nothing, AddressOf ToggleTheme_Click)
            })
            
            ' 工具菜单
            toolsMenu = New ToolStripMenuItem("&Tools")
            toolsMenu.DropDownItems.AddRange({
                New ToolStripMenuItem("&Settings...", Nothing, AddressOf Settings_Click, Keys.Control Or Keys.T),
                New ToolStripMenuItem("&Docker Diagnostics", Nothing, AddressOf DockerDiagnostics_Click),
                New ToolStripMenuItem("&AWS Configuration", Nothing, AddressOf AwsConfig_Click),
                New ToolStripSeparator(),
                New ToolStripMenuItem("&Command Line Tools", Nothing, AddressOf CommandLineTools_Click)
            })
            
            ' 帮助菜单
            helpMenu = New ToolStripMenuItem("&Help")
            helpMenu.DropDownItems.AddRange({
                New ToolStripMenuItem("&Documentation", Nothing, AddressOf Documentation_Click, Keys.F1),
                New ToolStripMenuItem("&Check for Updates", Nothing, AddressOf CheckUpdates_Click),
                New ToolStripSeparator(),
                New ToolStripMenuItem("&About", Nothing, AddressOf About_Click)
            })
            
            menuStrip.Items.AddRange({fileMenu, editMenu, viewMenu, toolsMenu, helpMenu})
            Me.Controls.Add(menuStrip)
            Me.MainMenuStrip = menuStrip
        End Sub
        
        Private Sub CreateToolStrip()
            toolStrip = New ToolStrip()
            toolStrip.ImageList = New ImageList()
            toolStrip.ImageList.Images.Add("New", My.Resources.NewProject)
            toolStrip.ImageList.Images.Add("Open", My.Resources.OpenProject)
            toolStrip.ImageList.Images.Add("Save", My.Resources.SaveProject)
            toolStrip.ImageList.Images.Add("Start", My.Resources.StartDeployment)
            toolStrip.ImageList.Images.Add("Stop", My.Resources.StopDeployment)
            toolStrip.ImageList.Images.Add("Settings", My.Resources.Settings)
            
            btnNewProject = New ToolStripButton()
            btnNewProject.ImageKey = "New"
            btnNewProject.ToolTipText = "New Project (Ctrl+N)"
            AddHandler btnNewProject.Click, AddressOf NewProject_Click
            
            btnOpenProject = New ToolStripButton()
            btnOpenProject.ImageKey = "Open"
            btnOpenProject.ToolTipText = "Open Project (Ctrl+O)"
            AddHandler btnOpenProject.Click, AddressOf OpenProject_Click
            
            btnSaveProject = New ToolStripButton()
            btnSaveProject.ImageKey = "Save"
            btnSaveProject.ToolTipText = "Save Project (Ctrl+S)"
            AddHandler btnSaveProject.Click, AddressOf SaveProject_Click
            
            toolStripSeparator1 = New ToolStripSeparator()
            
            btnStartDeployment = New ToolStripButton()
            btnStartDeployment.ImageKey = "Start"
            btnStartDeployment.ToolTipText = "Start Deployment"
            AddHandler btnStartDeployment.Click, AddressOf StartDeployment_Click
            
            btnStopDeployment = New ToolStripButton()
            btnStopDeployment.ImageKey = "Stop"
            btnStopDeployment.ToolTipText = "Stop Deployment"
            btnStopDeployment.Enabled = False
            AddHandler btnStopDeployment.Click, AddressOf StopDeployment_Click
            
            toolStripSeparator2 = New ToolStripSeparator()
            
            btnSettings = New ToolStripButton()
            btnSettings.ImageKey = "Settings"
            btnSettings.ToolTipText = "Settings (Ctrl+T)"
            AddHandler btnSettings.Click, AddressOf Settings_Click
            
            toolStrip.Items.AddRange({
                btnNewProject, btnOpenProject, btnSaveProject,
                toolStripSeparator1,
                btnStartDeployment, btnStopDeployment,
                toolStripSeparator2,
                btnSettings
            })
            
            Me.Controls.Add(toolStrip)
        End Sub
        
        Private Sub CreateTabControl()
            tabControl = New TabControl()
            tabControl.Dock = DockStyle.Fill
            tabControl.Location = New Point(0, menuStrip.Height + toolStrip.Height)
            tabControl.Size = New Size(Me.ClientSize.Width, 
                                     Me.ClientSize.Height - menuStrip.Height - toolStrip.Height - 50)
            
            ' 创建各个选项卡
            CreateDockerTab()
            CreateEcsTab()
            CreateInstallerTab()
            CreateLogsTab()
            
            tabControl.TabPages.AddRange({dockerTab, ecsTab, installerTab, logsTab})
            Me.Controls.Add(tabControl)
        End Sub
        
        Private Sub CreateDockerTab()
            dockerTab = New TabPage("Docker")
            dockerTab.BackColor = Color.White
            
            pnlDocker = New Panel()
            pnlDocker.Dock = DockStyle.Fill
            pnlDocker.AutoScroll = True
            pnlDocker.Padding = New Padding(10)
            
            ' Dockerfile 选择
            Dim lblDockerfile As New Label()
            lblDockerfile.Text = "Dockerfile:"
            lblDockerfile.Location = New Point(10, 20)
            lblDockerfile.Size = New Size(100, 25)
            
            txtDockerfile = New TextBox()
            txtDockerfile.Location = New Point(120, 20)
            txtDockerfile.Size = New Size(400, 25)
            txtDockerfile.Text = "./Dockerfile"
            
            btnBrowseDockerfile = New Button()
            btnBrowseDockerfile.Text = "Browse..."
            btnBrowseDockerfile.Location = New Point(530, 20)
            btnBrowseDockerfile.Size = New Size(80, 25)
            AddHandler btnBrowseDockerfile.Click, AddressOf BrowseDockerfile_Click
            
            ' Compose 文件选择
            Dim lblComposeFile As New Label()
            lblComposeFile.Text = "Docker Compose File:"
            lblComposeFile.Location = New Point(10, 60)
            lblComposeFile.Size = New Size(120, 25)
            
            txtComposeFile = New TextBox()
            txtComposeFile.Location = New Point(140, 60)
            txtComposeFile.Size = New Size(380, 25)
            txtComposeFile.Text = "./docker-compose.yml"
            
            btnBrowseCompose = New Button()
            btnBrowseCompose.Text = "Browse..."
            btnBrowseCompose.Location = New Point(530, 60)
            btnBrowseCompose.Size = New Size(80, 25)
            AddHandler btnBrowseCompose.Click, AddressOf BrowseComposeFile_Click
            
            ' 选项
            chkPullLatest = New CheckBox()
            chkPullLatest.Text = "Pull latest images before deployment"
            chkPullLatest.Location = New Point(10, 100)
            chkPullLatest.Size = New Size(300, 25)
            chkPullLatest.Checked = True
            
            ' 按钮区域
            Dim pnlDockerButtons As New Panel()
            pnlDockerButtons.Location = New Point(10, 140)
            pnlDockerButtons.Size = New Size(600, 40)
            
            btnBuildImage = New Button()
            btnBuildImage.Text = "Build Image"
            btnBuildImage.Location = New Point(0, 0)
            btnBuildImage.Size = New Size(120, 35)
            btnBuildImage.BackColor = Color.FromArgb(70, 130, 180)
            btnBuildImage.ForeColor = Color.White
            AddHandler btnBuildImage.Click, AddressOf BuildImage_Click
            
            btnComposeUp = New Button()
            btnComposeUp.Text = "Compose Up"
            btnComposeUp.Location = New Point(130, 0)
            btnComposeUp.Size = New Size(120, 35)
            btnComposeUp.BackColor = Color.FromArgb(60, 179, 113)
            btnComposeUp.ForeColor = Color.White
            AddHandler btnComposeUp.Click, AddressOf ComposeUp_Click
            
            btnComposeDown = New Button()
            btnComposeDown.Text = "Compose Down"
            btnComposeDown.Location = New Point(260, 0)
            btnComposeDown.Size = New Size(120, 35)
            btnComposeDown.BackColor = Color.FromArgb(220, 53, 69)
            btnComposeDown.ForeColor = Color.White
            AddHandler btnComposeDown.Click, AddressOf ComposeDown_Click
            
            pnlDockerButtons.Controls.AddRange({btnBuildImage, btnComposeUp, btnComposeDown})
            
            ' 列表区域
            Dim splitContainer As New SplitContainer()
            splitContainer.Location = New Point(10, 200)
            splitContainer.Size = New Size(750, 300)
            splitContainer.Orientation = Orientation.Vertical
            
            ' Docker 镜像列表
            Dim pnlImages As New Panel()
            pnlImages.Dock = DockStyle.Fill
            pnlImages.Padding = New Padding(5)
            
            Dim lblImages As New Label()
            lblImages.Text = "Docker Images"
            lblImages.Dock = DockStyle.Top
            lblImages.Font = New Font(lblImages.Font, FontStyle.Bold)
            
            lstDockerImages = New ListBox()
            lstDockerImages.Dock = DockStyle.Fill
            lstDockerImages.Font = New Font("Consolas", 10)
            
            pnlImages.Controls.Add(lstDockerImages)
            pnlImages.Controls.Add(lblImages)
            
            ' Docker 容器列表
            Dim pnlContainers As New Panel()
            pnlContainers.Dock = DockStyle.Fill
            pnlContainers.Padding = New Padding(5)
            
            Dim lblContainers As New Label()
            lblContainers.Text = "Docker Containers"
            lblContainers.Dock = DockStyle.Top
            lblContainers.Font = New Font(lblContainers.Font, FontStyle.Bold)
            
            lstDockerContainers = New ListBox()
            lstDockerContainers.Dock = DockStyle.Fill
            lstDockerContainers.Font = New Font("Consolas", 10)
            
            pnlContainers.Controls.Add(lstDockerContainers)
            pnlContainers.Controls.Add(lblContainers)
            
            splitContainer.Panel1.Controls.Add(pnlImages)
            splitContainer.Panel2.Controls.Add(pnlContainers)
            
            ' 添加所有控件到面板
            pnlDocker.Controls.AddRange({
                lblDockerfile, txtDockerfile, btnBrowseDockerfile,
                lblComposeFile, txtComposeFile, btnBrowseCompose,
                chkPullLatest,
                pnlDockerButtons,
                splitContainer
            })
            
            dockerTab.Controls.Add(pnlDocker)
        End Sub
        
        Private Sub CreateEcsTab()
            ecsTab = New TabPage("AWS ECS")
            ecsTab.BackColor = Color.White
            
            pnlEcs = New Panel()
            pnlEcs.Dock = DockStyle.Fill
            pnlEcs.AutoScroll = True
            pnlEcs.Padding = New Padding(10)
            
            ' ECS 配置
            Dim lblCluster As New Label()
            lblCluster.Text = "Cluster Name:"
            lblCluster.Location = New Point(10, 20)
            lblCluster.Size = New Size(100, 25)
            
            txtEcsCluster = New TextBox()
            txtEcsCluster.Location = New Point(120, 20)
            txtEcsCluster.Size = New Size(300, 25)
            txtEcsCluster.Text = "production-cluster"
            
            Dim lblService As New Label()
            lblService.Text = "Service Name:"
            lblService.Location = New Point(10, 60)
            lblService.Size = New Size(100, 25)
            
            txtEcsService = New TextBox()
            txtEcsService.Location = New Point(120, 60)
            txtEcsService.Size = New Size(300, 25)
            txtEcsService.Text = "my-service"
            
            Dim lblRegion As New Label()
            lblRegion.Text = "AWS Region:"
            lblRegion.Location = New Point(10, 100)
            lblRegion.Size = New Size(100, 25)
            
            txtEcsRegion = New TextBox()
            txtEcsRegion.Location = New Point(120, 100)
            txtEcsRegion.Size = New Size(300, 25)
            txtEcsRegion.Text = "us-east-1"
            
            Dim lblEcr As New Label()
            lblEcr.Text = "ECR Repository:"
            lblEcr.Location = New Point(10, 140)
            lblEcr.Size = New Size(100, 25)
            
            txtEcrRepository = New TextBox()
            txtEcrRepository.Location = New Point(120, 140)
            txtEcrRepository.Size = New Size(300, 25)
            txtEcrRepository.Text = "my-repo"
            
            ' 按钮区域
            Dim pnlEcsButtons As New Panel()
            pnlEcsButtons.Location = New Point(10, 180)
            pnlEcsButtons.Size = New Size(500, 40)
            
            btnDeployEcs = New Button()
            btnDeployEcs.Text = "Deploy to ECS"
            btnDeployEcs.Location = New Point(0, 0)
            btnDeployEcs.Size = New Size(150, 35)
            btnDeployEcs.BackColor = Color.FromArgb(60, 179, 113)
            btnDeployEcs.ForeColor = Color.White
            AddHandler btnDeployEcs.Click, AddressOf DeployEcs_Click
            
            btnCheckEcsStatus = New Button()
            btnCheckEcsStatus.Text = "Check Status"
            btnCheckEcsStatus.Location = New Point(160, 0)
            btnCheckEcsStatus.Size = New Size(150, 35)
            btnCheckEcsStatus.BackColor = Color.FromArgb(70, 130, 180)
            btnCheckEcsStatus.ForeColor = Color.White
            AddHandler btnCheckEcsStatus.Click, AddressOf CheckEcsStatus_Click
            
            btnRollbackEcs = New Button()
            btnRollbackEcs.Text = "Rollback"
            btnRollbackEcs.Location = New Point(320, 0)
            btnRollbackEcs.Size = New Size(150, 35)
            btnRollbackEcs.BackColor = Color.FromArgb(220, 53, 69)
            btnRollbackEcs.ForeColor = Color.White
            AddHandler btnRollbackEcs.Click, AddressOf RollbackEcs_Click
            
            pnlEcsButtons.Controls.AddRange({btnDeployEcs, btnCheckEcsStatus, btnRollbackEcs})
            
            ' ECS 状态显示
            Dim pnlEcsStatus As New Panel()
            pnlEcsStatus.Location = New Point(10, 240)
            pnlEcsStatus.Size = New Size(750, 200)
            pnlEcsStatus.BorderStyle = BorderStyle.FixedSingle
            
            Dim lblStatusTitle As New Label()
            lblStatusTitle.Text = "ECS Deployment Status"
            lblStatusTitle.Location = New Point(10, 10)
            lblStatusTitle.Font = New Font(lblStatusTitle.Font, FontStyle.Bold)
            lblStatusTitle.Size = New Size(200, 25)
            
            pnlEcsStatus.Controls.Add(lblStatusTitle)
            
            pnlEcs.Controls.AddRange({
                lblCluster, txtEcsCluster,
                lblService, txtEcsService,
                lblRegion, txtEcsRegion,
                lblEcr, txtEcrRepository,
                pnlEcsButtons,
                pnlEcsStatus
            })
            
            ecsTab.Controls.Add(pnlEcs)
        End Sub
        
        Private Sub CreateInstallerTab()
            installerTab = New TabPage("Installer")
            installerTab.BackColor = Color.White
            
            pnlInstaller = New Panel()
            pnlInstaller.Dock = DockStyle.Fill
            pnlInstaller.AutoScroll = True
            pnlInstaller.Padding = New Padding(10)
            
            ' 安装程序配置
            Dim lblAppName As New Label()
            lblAppName.Text = "Application Name:"
            lblAppName.Location = New Point(10, 20)
            lblAppName.Size = New Size(120, 25)
            
            txtAppName = New TextBox()
            txtAppName.Location = New Point(140, 20)
            txtAppName.Size = New Size(300, 25)
            txtAppName.Text = "My Application"
            
            Dim lblAppVersion As New Label()
            lblAppVersion.Text = "Application Version:"
            lblAppVersion.Location = New Point(10, 60)
            lblAppVersion.Size = New Size(120, 25)
            
            txtAppVersion = New TextBox()
            txtAppVersion.Location = New Point(140, 60)
            txtAppVersion.Size = New Size(300, 25)
            txtAppVersion.Text = "1.0.0"
            
            Dim lblOutputDir As New Label()
            lblOutputDir.Text = "Output Directory:"
            lblOutputDir.Location = New Point(10, 100)
            lblOutputDir.Size = New Size(120, 25)
            
            txtOutputDir = New TextBox()
            txtOutputDir.Location = New Point(140, 100)
            txtOutputDir.Size = New Size(300, 25)
            txtOutputDir.Text = "./Installer"
            
            btnBrowseOutputDir = New Button()
            btnBrowseOutputDir.Text = "Browse..."
            btnBrowseOutputDir.Location = New Point(450, 100)
            btnBrowseOutputDir.Size = New Size(80, 25)
            AddHandler btnBrowseOutputDir.Click, AddressOf BrowseOutputDir_Click
            
            ' 选项
            chkSignInstaller = New CheckBox()
            chkSignInstaller.Text = "Sign installer with digital certificate"
            chkSignInstaller.Location = New Point(10, 140)
            chkSignInstaller.Size = New Size(300, 25)
            chkSignInstaller.Checked = False
            
            ' 按钮区域
            Dim pnlInstallerButtons As New Panel()
            pnlInstallerButtons.Location = New Point(10, 180)
            pnlInstallerButtons.Size = New Size(500, 40)
            
            btnGenerateScript = New Button()
            btnGenerateScript.Text = "Generate Script"
            btnGenerateScript.Location = New Point(0, 0)
            btnGenerateScript.Size = New Size(150, 35)
            btnGenerateScript.BackColor = Color.FromArgb(70, 130, 180)
            btnGenerateScript.ForeColor = Color.White
            AddHandler btnGenerateScript.Click, AddressOf GenerateScript_Click
            
            btnBuildInstaller = New Button()
            btnBuildInstaller.Text = "Build Installer"
            btnBuildInstaller.Location = New Point(160, 0)
            btnBuildInstaller.Size = New Size(150, 35)
            btnBuildInstaller.BackColor = Color.FromArgb(60, 179, 113)
            btnBuildInstaller.ForeColor = Color.White
            AddHandler btnBuildInstaller.Click, AddressOf BuildInstaller_Click
            
            pnlInstallerButtons.Controls.AddRange({btnGenerateScript, btnBuildInstaller})
            
            ' 安装程序预览
            Dim pnlPreview As New Panel()
            pnlPreview.Location = New Point(10, 240)
            pnlPreview.Size = New Size(750, 300)
            pnlPreview.BorderStyle = BorderStyle.FixedSingle
            
            Dim lblPreview As New Label()
            lblPreview.Text = "Installation Script Preview"
            lblPreview.Location = New Point(10, 10)
            lblPreview.Font = New Font(lblPreview.Font, FontStyle.Bold)
            lblPreview.Size = New Size(200, 25)
            
            Dim txtPreview As New TextBox()
            txtPreview.Location = New Point(10, 40)
            txtPreview.Size = New Size(730, 250)
            txtPreview.Multiline = True
            txtPreview.ScrollBars = ScrollBars.Both
            txtPreview.Font = New Font("Consolas", 10)
            txtPreview.ReadOnly = True
            
            pnlPreview.Controls.Add(lblPreview)
            pnlPreview.Controls.Add(txtPreview)
            
            pnlInstaller.Controls.AddRange({
                lblAppName, txtAppName,
                lblAppVersion, txtAppVersion,
                lblOutputDir, txtOutputDir, btnBrowseOutputDir,
                chkSignInstaller,
                pnlInstallerButtons,
                pnlPreview
            })
            
            installerTab.Controls.Add(pnlInstaller)
        End Sub
        
        Private Sub CreateLogsTab()
            logsTab = New TabPage("Logs")
            logsTab.BackColor = Color.FromArgb(30, 30, 30)
            
            pnlLogs = New Panel()
            pnlLogs.Dock = DockStyle.Fill
            pnlLogs.Padding = New Padding(10)
            
            ' 日志文本框
            txtLogs = New TextBox()
            txtLogs.Dock = DockStyle.Fill
            txtLogs.Multiline = True
            txtLogs.ScrollBars = ScrollBars.Both
            txtLogs.Font = New Font("Consolas", 10)
            txtLogs.BackColor = Color.FromArgb(40, 40, 40)
            txtLogs.ForeColor = Color.FromArgb(200, 200, 200)
            txtLogs.ReadOnly = True
            
            ' 控制按钮面板
            Dim pnlLogControls As New Panel()
            pnlLogControls.Dock = DockStyle.Bottom
            pnlLogControls.Height = 40
            pnlLogControls.BackColor = Color.FromArgb(50, 50, 50)
            
            btnClearLogs = New Button()
            btnClearLogs.Text = "Clear Logs"
            btnClearLogs.Location = New Point(10, 5)
            btnClearLogs.Size = New Size(100, 30)
            AddHandler btnClearLogs.Click, AddressOf ClearLogs_Click
            
            btnSaveLogs = New Button()
            btnSaveLogs.Text = "Save Logs..."
            btnSaveLogs.Location = New Point(120, 5)
            btnSaveLogs.Size = New Size(100, 30)
            AddHandler btnSaveLogs.Click, AddressOf SaveLogs_Click
            
            chkAutoScroll = New CheckBox()
            chkAutoScroll.Text = "Auto-scroll"
            chkAutoScroll.Location = New Point(230, 10)
            chkAutoScroll.Size = New Size(100, 25)
            chkAutoScroll.ForeColor = Color.White
            chkAutoScroll.Checked = True
            
            pnlLogControls.Controls.AddRange({btnClearLogs, btnSaveLogs, chkAutoScroll})
            
            pnlLogs.Controls.Add(txtLogs)
            pnlLogs.Controls.Add(pnlLogControls)
            
            logsTab.Controls.Add(pnlLogs)
        End Sub
        
        Private Sub CreateStatusStrip()
            statusStrip = New StatusStrip()
            statusStrip.Dock = DockStyle.Bottom
            
            lblStatus = New ToolStripStatusLabel()
            lblStatus.Text = "Ready"
            lblStatus.Spring = True
            
            progressBar = New ToolStripProgressBar()
            progressBar.Visible = False
            
            statusStrip.Items.AddRange({lblStatus, progressBar})
            Me.Controls.Add(statusStrip)
        End Sub
        
        Private Sub InitializeControls()
            ' 设置控件的DataBindings
            SetupDataBindings()
            
            ' 加载初始数据
            LoadInitialData()
        End Sub
        
        Private Sub InitializeViewModel()
            ' 创建服务和ViewModel
            _deploymentService = New DeploymentService()
            _viewModel = New MainViewModel(_deploymentService)
            
            ' 订阅ViewModel事件
            AddHandler _viewModel.StatusMessageChanged, AddressOf OnStatusMessageChanged
            AddHandler _viewModel.ProgressChanged, AddressOf OnProgressChanged
            AddHandler _viewModel.LogMessageAdded, AddressOf OnLogMessageAdded
            AddHandler _viewModel.DockerImagesUpdated, AddressOf OnDockerImagesUpdated
            AddHandler _viewModel.DockerContainersUpdated, AddressOf OnDockerContainersUpdated
        End Sub
        
        Private Sub SetupDataBindings()
            ' 绑定文本框到ViewModel属性
            txtDockerfile.DataBindings.Add("Text", _viewModel, "DockerfilePath", 
                                          True, DataSourceUpdateMode.OnPropertyChanged)
            txtComposeFile.DataBindings.Add("Text", _viewModel, "ComposeFilePath", 
                                           True, DataSourceUpdateMode.OnPropertyChanged)
            txtEcsCluster.DataBindings.Add("Text", _viewModel, "EcsClusterName", 
                                          True, DataSourceUpdateMode.OnPropertyChanged)
            txtEcsService.DataBindings.Add("Text", _viewModel, "EcsServiceName", 
                                          True, DataSourceUpdateMode.OnPropertyChanged)
            txtEcsRegion.DataBindings.Add("Text", _viewModel, "EcsRegion", 
                                         True, DataSourceUpdateMode.OnPropertyChanged)
            txtAppName.DataBindings.Add("Text", _viewModel, "InstallerAppName", 
                                       True, DataSourceUpdateMode.OnPropertyChanged)
            txtAppVersion.DataBindings.Add("Text", _viewModel, "InstallerAppVersion", 
                                          True, DataSourceUpdateMode.OnPropertyChanged)
            txtOutputDir.DataBindings.Add("Text", _viewModel, "InstallerOutputDir", 
                                         True, DataSourceUpdateMode.OnPropertyChanged)
            
            ' 绑定复选框
            chkPullLatest.DataBindings.Add("Checked", _viewModel, "PullLatestImages", 
                                          True, DataSourceUpdateMode.OnPropertyChanged)
            chkSignInstaller.DataBindings.Add("Checked", _viewModel, "SignInstaller", 
                                            True, DataSourceUpdateMode.OnPropertyChanged)
        End Sub
        
        Private Sub LoadInitialData()
            ' 加载保存的设置
            _viewModel.LoadSettings()
            
            ' 检查Docker环境
            Task.Run(Async Sub()
                         Await _viewModel.CheckDockerEnvironmentAsync()
                     End Sub)
            
            ' 刷新Docker列表
            Task.Run(Async Sub()
                         Await _viewModel.RefreshDockerListsAsync()
                     End Sub)
        End Sub
        
        Private Sub ApplyTheme()
            ' 应用主题颜色
            If _viewModel.IsDarkTheme Then
                ApplyDarkTheme()
            Else
                ApplyLightTheme()
            End If
        End Sub
        
        Private Sub ApplyLightTheme()
            Me.BackColor = Color.White
            For Each tab As TabPage In tabControl.TabPages
                tab.BackColor = Color.White
            Next
        End Sub
        
        Private Sub ApplyDarkTheme()
            Me.BackColor = Color.FromArgb(45, 45, 48)
            menuStrip.BackColor = Color.FromArgb(60, 60, 60)
            menuStrip.ForeColor = Color.White
            toolStrip.BackColor = Color.FromArgb(60, 60, 60)
            statusStrip.BackColor = Color.FromArgb(60, 60, 60)
            statusStrip.ForeColor = Color.White
            
            For Each tab As TabPage In tabControl.TabPages
                If tab IsNot logsTab Then
                    tab.BackColor = Color.FromArgb(45, 45, 48)
                    tab.ForeColor = Color.White
                End If
            Next
        End Sub
        
        ' 事件处理方法
        Private Async Sub BuildImage_Click(sender As Object, e As EventArgs)
            btnBuildImage.Enabled = False
            btnBuildImage.Text = "Building..."
            
            Try
                Dim success = Await _viewModel.BuildDockerImageAsync()
                If success Then
                    UpdateStatus("Docker image built successfully")
                    Await _viewModel.RefreshDockerListsAsync()
                Else
                    UpdateStatus("Failed to build Docker image", True)
                End If
            Catch ex As Exception
                UpdateStatus($"Error: {ex.Message}", True)
            Finally
                btnBuildImage.Enabled = True
                btnBuildImage.Text = "Build Image"
            End Try
        End Sub
        
        Private Async Sub ComposeUp_Click(sender As Object, e As EventArgs)
            btnComposeUp.Enabled = False
            btnComposeUp.Text = "Starting..."
            
            Try
                Dim success = Await _viewModel.StartDockerComposeAsync()
                If success Then
                    UpdateStatus("Docker Compose services started")
                    Await _viewModel.RefreshDockerListsAsync()
                Else
                    UpdateStatus("Failed to start Docker Compose services", True)
                End If
            Catch ex As Exception
                UpdateStatus($"Error: {ex.Message}", True)
            Finally
                btnComposeUp.Enabled = True
                btnComposeUp.Text = "Compose Up"
            End Try
        End Sub
        
        Private Async Sub ComposeDown_Click(sender As Object, e As EventArgs)
            btnComposeDown.Enabled = False
            btnComposeDown.Text = "Stopping..."
            
            Try
                Dim success = Await _viewModel.StopDockerComposeAsync()
                If success Then
                    UpdateStatus("Docker Compose services stopped")
                    Await _viewModel.RefreshDockerListsAsync()
                Else
                    UpdateStatus("Failed to stop Docker Compose services", True)
                End If
            Catch ex As Exception
                UpdateStatus($"Error: {ex.Message}", True)
            Finally
                btnComposeDown.Enabled = True
                btnComposeDown.Text = "Compose Down"
            End Try
        End Sub
        
        Private Async Sub DeployEcs_Click(sender As Object, e As EventArgs)
            btnDeployEcs.Enabled = False
            btnDeployEcs.Text = "Deploying..."
            
            Try
                Dim success = Await _viewModel.DeployToEcsAsync()
                If success Then
                    UpdateStatus("Successfully deployed to AWS ECS")
                Else
                    UpdateStatus("Failed to deploy to AWS ECS", True)
                End If
            Catch ex As Exception
                UpdateStatus($"Error: {ex.Message}", True)
            Finally
                btnDeployEcs.Enabled = True
                btnDeployEcs.Text = "Deploy to ECS"
            End Try
        End Sub
        
        Private Async Sub GenerateScript_Click(sender As Object, e As EventArgs)
            btnGenerateScript.Enabled = False
            btnGenerateScript.Text = "Generating..."
            
            Try
                Dim script = Await _viewModel.GenerateInstallerScriptAsync()
                If Not String.IsNullOrEmpty(script) Then
                    UpdateStatus("Installer script generated successfully")
                Else
                    UpdateStatus("Failed to generate installer script", True)
                End If
            Catch ex As Exception
                UpdateStatus($"Error: {ex.Message}", True)
            Finally
                btnGenerateScript.Enabled = True
                btnGenerateScript.Text = "Generate Script"
            End Try
        End Sub
        
        Private Async Sub BuildInstaller_Click(sender As Object, e As EventArgs)
            btnBuildInstaller.Enabled = False
            btnBuildInstaller.Text = "Building..."
            
            Try
                Dim installerPath = Await _viewModel.BuildInstallerAsync()
                If Not String.IsNullOrEmpty(installerPath) Then
                    UpdateStatus($"Installer created: {installerPath}")
                Else
                    UpdateStatus("Failed to build installer", True)
                End If
            Catch ex As Exception
                UpdateStatus($"Error: {ex.Message}", True)
            Finally
                btnBuildInstaller.Enabled = True
                btnBuildInstaller.Text = "Build Installer"
            End Try
        End Sub
        
        Private Sub BrowseDockerfile_Click(sender As Object, e As EventArgs)
            Using dialog As New OpenFileDialog()
                dialog.Filter = "Dockerfile (*.dockerfile)|*.dockerfile|All Files (*.*)|*.*"
                dialog.Title = "Select Dockerfile"
                dialog.InitialDirectory = Environment.CurrentDirectory
                
                If dialog.ShowDialog() = DialogResult.OK Then
                    txtDockerfile.Text = dialog.FileName
                End If
            End Using
        End Sub
        
        Private Sub BrowseComposeFile_Click(sender As Object, e As EventArgs)
            Using dialog As New OpenFileDialog()
                dialog.Filter = "Docker Compose Files (*.yml;*.yaml)|*.yml;*.yaml|All Files (*.*)|*.*"
                dialog.Title = "Select Docker Compose File"
                dialog.InitialDirectory = Environment.CurrentDirectory
                
                If dialog.ShowDialog() = DialogResult.OK Then
                    txtComposeFile.Text = dialog.FileName
                End If
            End Using
        End Sub
        
        Private Sub BrowseOutputDir_Click(sender As Object, e As EventArgs)
            Using dialog As New FolderBrowserDialog()
                dialog.Description = "Select Output Directory"
                dialog.SelectedPath = Environment.CurrentDirectory
                
                If dialog.ShowDialog() = DialogResult.OK Then
                    txtOutputDir.Text = dialog.SelectedPath
                End If
            End Using
        End Sub
        
        Private Sub ClearLogs_Click(sender As Object, e As EventArgs)
            _viewModel.ClearLogs()
        End Sub
        
        Private Sub SaveLogs_Click(sender As Object, e As EventArgs)
            Using dialog As New SaveFileDialog()
                dialog.Filter = "Log Files (*.log)|*.log|Text Files (*.txt)|*.txt|All Files (*.*)|*.*"
                dialog.Title = "Save Logs"
                dialog.DefaultExt = ".log"
                
                If dialog.ShowDialog() = DialogResult.OK Then
                    _viewModel.SaveLogs(dialog.FileName)
                End If
            End Using
        End Sub
        
        ' ViewModel 事件处理
        Private Sub OnStatusMessageChanged(sender As Object, e As StatusChangedEventArgs)
            If InvokeRequired Then
                Invoke(New Action(Sub() OnStatusMessageChanged(sender, e)))
                Return
            End If
            
            lblStatus.Text = e.Message
            If e.IsError Then
                lblStatus.ForeColor = Color.Red
            Else
                lblStatus.ForeColor = SystemColors.ControlText
            End If
        End Sub
        
        Private Sub OnProgressChanged(sender As Object, e As ProgressChangedEventArgs)
            If InvokeRequired Then
                Invoke(New Action(Sub() OnProgressChanged(sender, e)))
                Return
            End If
            
            If e.IsIndeterminate Then
                progressBar.Style = ProgressBarStyle.Marquee
            Else
                progressBar.Style = ProgressBarStyle.Continuous
                progressBar.Value = e.ProgressPercentage
            End If
            
            progressBar.Visible = e.IsBusy
        End Sub
        
        Private Sub OnLogMessageAdded(sender As Object, e As LogMessageEventArgs)
            If InvokeRequired Then
                Invoke(New Action(Sub() OnLogMessageAdded(sender, e)))
                Return
            End If
            
            Dim timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            Dim logLine = $"[{timestamp}] [{e.Level}] {e.Message}{Environment.NewLine}"
            
            txtLogs.AppendText(logLine)
            
            If chkAutoScroll.Checked Then
                txtLogs.SelectionStart = txtLogs.TextLength
                txtLogs.ScrollToCaret()
            End If
        End Sub
        
        Private Sub OnDockerImagesUpdated(sender As Object, e As DockerImagesUpdatedEventArgs)
            If InvokeRequired Then
                Invoke(New Action(Sub() OnDockerImagesUpdated(sender, e)))
                Return
            End If
            
            lstDockerImages.Items.Clear()
            For Each image In e.Images
                lstDockerImages.Items.Add(image)
            Next
        End Sub
        
        Private Sub OnDockerContainersUpdated(sender As Object, e As DockerContainersUpdatedEventArgs)
            If InvokeRequired Then
                Invoke(New Action(Sub() OnDockerContainersUpdated(sender, e)))
                Return
            End If
            
            lstDockerContainers.Items.Clear()
            For Each container In e.Containers
                lstDockerContainers.Items.Add(container)
            Next
        End Sub
        
        Private Sub UpdateStatus(message As String, Optional isError As Boolean = False)
            _viewModel.UpdateStatus(message, isError)
        End Sub
        
        ' 菜单事件
        Private Sub NewProject_Click(sender As Object, e As EventArgs)
            _viewModel.NewProject()
        End Sub
        
        Private Sub OpenProject_Click(sender As Object, e As EventArgs)
            Using dialog As New OpenFileDialog()
                dialog.Filter = "Deployment Project (*.deploy)|*.deploy|All Files (*.*)|*.*"
                dialog.Title = "Open Deployment Project"
                
                If dialog.ShowDialog() = DialogResult.OK Then
                    _viewModel.OpenProject(dialog.FileName)
                End If
            End Using
        End Sub
        
        Private Sub SaveProject_Click(sender As Object, e As EventArgs)
            _viewModel.SaveProject()
        End Sub
        
        Private Sub SaveProjectAs_Click(sender As Object, e As EventArgs)
            Using dialog As New SaveFileDialog()
                dialog.Filter = "Deployment Project (*.deploy)|*.deploy"
                dialog.Title = "Save Deployment Project As"
                
                If dialog.ShowDialog() = DialogResult.OK Then
                    _viewModel.SaveProjectAs(dialog.FileName)
                End If
            End Using
        End Sub
        
        Private Sub Exit_Click(sender As Object, e As EventArgs)
            Me.Close()
        End Sub
        
        Private Sub Settings_Click(sender As Object, e As EventArgs)
            Using settingsForm As New SettingsForm(_viewModel)
                settingsForm.ShowDialog()
            End Using
        End Sub
        
        Private Sub ToggleTheme_Click(sender As Object, e As EventArgs)
            _viewModel.ToggleTheme()
            ApplyTheme()
        End Sub
        
        Private Sub MainForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
            ' 保存设置
            _viewModel.SaveSettings()
        End Sub
    End Class
End Namespace