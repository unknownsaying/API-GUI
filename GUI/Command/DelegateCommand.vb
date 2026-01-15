Imports System
Imports System.Threading.Tasks
Imports System.Windows.Input

Namespace DeploymentAutomationGUI.Commands
    
    ''' 委托命令基类
    
    Public MustInherit Class DelegateCommandBase
        Implements ICommand
        
        Private _isActive As Boolean = True
        
        
        ''' 命令执行时引发的事件
        
        Public Event CanExecuteChanged As EventHandler _
            Implements ICommand.CanExecuteChanged
        
        
        ''' 获取或设置命令是否处于活动状态
        
        Public Property IsActive As Boolean
            Get
                Return _isActive
            End Get
            Set(value As Boolean)
                If _isActive <> value Then
                    _isActive = value
                    OnIsActiveChanged()
                End If
            End Set
        End Property
        
        
        ''' 确定命令是否可以执行
        
        Public MustOverride Function CanExecute(parameter As Object) As Boolean _
            Implements ICommand.CanExecute
        
        
        ''' 执行命令
        
        Public MustOverride Sub Execute(parameter As Object) _
            Implements ICommand.Execute
        
        
        ''' 引发CanExecuteChanged事件
        
        Public Sub RaiseCanExecuteChanged()
            RaiseEvent CanExecuteChanged(Me, EventArgs.Empty)
        End Sub
        
        
        ''' IsActive属性更改时引发的事件
        
        Public Event IsActiveChanged As EventHandler
        
        
        ''' 引发IsActiveChanged事件
        
        Protected Overridable Sub OnIsActiveChanged()
            RaiseEvent IsActiveChanged(Me, EventArgs.Empty)
        End Sub
    End Class
    
    
    ''' 通用委托命令
    
    Public Class DelegateCommand
        Inherits DelegateCommandBase
        
        Private ReadOnly _executeMethod As Action(Of Object)
        Private ReadOnly _canExecuteMethod As Func(Of Object, Boolean)
        
        
        ''' 初始化DelegateCommand类的新实例
        
        Public Sub New(executeMethod As Action(Of Object), 
                      Optional canExecuteMethod As Func(Of Object, Boolean) = Nothing)
            _executeMethod = If(executeMethod, 
                               Function(x) Throw New ArgumentNullException(NameOf(executeMethod)))
            _canExecuteMethod = canExecuteMethod
        End Sub
        
        
        ''' 确定命令是否可以执行
        
        Public Overrides Function CanExecute(parameter As Object) As Boolean
            Return IsActive AndAlso (_canExecuteMethod Is Nothing OrElse _canExecuteMethod(parameter))
        End Function
        
        
        ''' 执行命令
        
        Public Overrides Sub Execute(parameter As Object)
            If CanExecute(parameter) Then
                _executeMethod(parameter)
            End If
        End Sub
    End Class
    
    
    ''' 异步委托命令
    
    Public Class DelegateCommandAsync
        Inherits DelegateCommandBase
        
        Private ReadOnly _executeMethod As Func(Of Object, Task)
        Private ReadOnly _canExecuteMethod As Func(Of Object, Boolean)
        Private _isExecuting As Boolean
        
        
        ''' 获取命令是否正在执行
        
        Public ReadOnly Property IsExecuting As Boolean
            Get
                Return _isExecuting
            End Get
        End Property
        
        
        ''' 初始化DelegateCommandAsync类的新实例
        
        Public Sub New(executeMethod As Func(Of Object, Task), 
                      Optional canExecuteMethod As Func(Of Object, Boolean) = Nothing)
            _executeMethod = If(executeMethod, 
                               Function(x) Throw New ArgumentNullException(NameOf(executeMethod)))
            _canExecuteMethod = canExecuteMethod
        End Sub
        
        
        ''' 确定命令是否可以执行
        
        Public Overrides Function CanExecute(parameter As Object) As Boolean
            Return IsActive AndAlso Not _isExecuting AndAlso 
                   (_canExecuteMethod Is Nothing OrElse _canExecuteMethod(parameter))
        End Function
        
        
        ''' 异步执行命令
        
        Public Async Overrides Sub Execute(parameter As Object)
            If CanExecute(parameter) Then
                _isExecuting = True
                RaiseCanExecuteChanged()
                
                Try
                    Await _executeMethod(parameter)
                Finally
                    _isExecuting = False
                    RaiseCanExecuteChanged()
                End Try
            End If
        End Sub
    End Class
    
    
    ''' 复合命令
    
    Public Class CompositeCommand
        Inherits DelegateCommandBase
        
        Private ReadOnly _commands As New List(Of ICommand)()
        Private ReadOnly _monitorCommandActivity As Boolean
        
        
        ''' 初始化CompositeCommand类的新实例
        
        Public Sub New(Optional monitorCommandActivity As Boolean = False)
            _monitorCommandActivity = monitorCommandActivity
        End Sub
        
        
        ''' 添加命令
        
        Public Sub AddCommand(command As ICommand)
            If command Is Nothing Then
                Throw New ArgumentNullException(NameOf(command))
            End If
            
            If Not _commands.Contains(command) Then
                _commands.Add(command)
                AddHandler command.CanExecuteChanged, AddressOf ChildCommandCanExecuteChanged
                
                If _monitorCommandActivity Then
                    Dim activeAwareCommand = TryCast(command, DelegateCommandBase)
                    If activeAwareCommand IsNot Nothing Then
                        AddHandler activeAwareCommand.IsActiveChanged, AddressOf ChildCommandIsActiveChanged
                    End If
                End If
                
                RaiseCanExecuteChanged()
            End If
        End Sub
        
        
        ''' 移除命令
        
        Public Sub RemoveCommand(command As ICommand)
            If command Is Nothing Then
                Throw New ArgumentNullException(NameOf(command))
            End If
            
            If _commands.Contains(command) Then
                _commands.Remove(command)
                RemoveHandler command.CanExecuteChanged, AddressOf ChildCommandCanExecuteChanged
                
                If _monitorCommandActivity Then
                    Dim activeAwareCommand = TryCast(command, DelegateCommandBase)
                    If activeAwareCommand IsNot Nothing Then
                        RemoveHandler activeAwareCommand.IsActiveChanged, AddressOf ChildCommandIsActiveChanged
                    End If
                End If
                
                RaiseCanExecuteChanged()
            End If
        End Sub
        
        
        ''' 确定命令是否可以执行
        
        Public Overrides Function CanExecute(parameter As Object) As Boolean
            If Not IsActive Then
                Return False
            End If
            
            For Each command In _commands
                If Not command.CanExecute(parameter) Then
                    Return False
                End If
            Next
            
            Return True
        End Function
        
        
        ''' 执行命令
        
        Public Overrides Sub Execute(parameter As Object)
            If CanExecute(parameter) Then
                For Each command In _commands
                    command.Execute(parameter)
                Next
            End If
        End Sub
        
        Private Sub ChildCommandCanExecuteChanged(sender As Object, e As EventArgs)
            RaiseCanExecuteChanged()
        End Sub
        
        Private Sub ChildCommandIsActiveChanged(sender As Object, e As EventArgs)
            RaiseCanExecuteChanged()
        End Sub
    End Class
End Namespace