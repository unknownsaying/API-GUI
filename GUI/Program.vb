Imports System
Imports System.Windows.Forms
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Logging
Imports DeploymentAutomationGUI.Views
Imports DeploymentAutomationGUI.ViewModels
Imports DeploymentAutomationGUI.Services

Module Program
    Private ReadOnly _serviceProvider As ServiceProvider
    
    Shared Sub New()
        ' 配置依赖注入
        Dim services = New ServiceCollection()
        
        ' 注册服务
        services.AddSingleton(Of IDeploymentService, DeploymentService)()
        services.AddSingleton(Of MainViewModel)()
        services.AddSingleton(Of MainForm)()
        
        ' 配置日志
        services.AddLogging(Sub(builder)
                                builder.AddConsole()
                                builder.SetMinimumLevel(LogLevel.Information)
                            End Sub)
        
        _serviceProvider = services.BuildServiceProvider()
    End Sub
    
    <STAThread>
    Sub Main()
        Application.SetHighDpiMode(HighDpiMode.SystemAware)
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        
        ' 设置异常处理
        AddHandler Application.ThreadException, AddressOf Application_ThreadException
        AddHandler AppDomain.CurrentDomain.UnhandledException, AddressOf CurrentDomain_UnhandledException
        
        Try
            ' 从服务容器获取主窗体
            Dim mainForm = _serviceProvider.GetRequiredService(Of MainForm)()
            Application.Run(mainForm)
        Catch ex As Exception
            MessageBox.Show($"Fatal error: {ex.Message}{Environment.NewLine}{ex.StackTrace}",
                          "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    
    Private Sub Application_ThreadException(sender As Object, e As Threading.ThreadExceptionEventArgs)
        HandleException(e.Exception)
    End Sub
    
    Private Sub CurrentDomain_UnhandledException(sender As Object, e As UnhandledExceptionEventArgs)
        HandleException(TryCast(e.ExceptionObject, Exception))
    End Sub
    
    Private Sub HandleException(ex As Exception)
        If ex Is Nothing Then Return
        
        Dim errorMessage = $"An unhandled exception occurred:{Environment.NewLine}{Environment.NewLine}" &
                          $"Message: {ex.Message}{Environment.NewLine}" &
                          $"Stack Trace:{Environment.NewLine}{ex.StackTrace}"
        
        MessageBox.Show(errorMessage, "Unhandled Exception", 
                       MessageBoxButtons.OK, MessageBoxIcon.Error)
    End Sub
End Module