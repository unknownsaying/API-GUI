Imports System
Imports System.Threading.Tasks
Imports System.Windows.Input

Namespace DeploymentAutomationGUI.Commands
    
    ''' 实现了ICommand接口的通用命令类
    
    Public Class RelayCommand
        Implements ICommand
        
        Private ReadOnly _execute As Action(Of Object)
        Private ReadOnly _canExecute As Func(Of Object, Boolean)
        
        
        ''' 命令执行时引发的事件
        
        Public Event CanExecuteChanged As EventHandler _
            Implements ICommand.CanExecuteChanged
        
        
        ''' 初始化RelayCommand类的新实例
        
        ''' <param name="execute">要执行的逻辑</param>
        ''' <param name="canExecute">确定命令是否可以执行的逻辑</param>
        Public Sub New(execute As Action(Of Object), Optional canExecute As Func(Of Object, Boolean) = Nothing)
            _execute = If(execute, Function(x) Throw New ArgumentNullException(NameOf(execute)))
            _canExecute = canExecute
        End Sub
        
        
        ''' 确定命令是否可以执行
        
        Public Function CanExecute(parameter As Object) As Boolean _
            Implements ICommand.CanExecute
            Return _canExecute Is Nothing OrElse _canExecute(parameter)
        End Function
        
        
        ''' 执行命令
        
        Public Sub Execute(parameter As Object) _
            Implements ICommand.Execute
            _execute(parameter)
        End Sub
        
        
        ''' 引发CanExecuteChanged事件
        
        Public Sub RaiseCanExecuteChanged()
            RaiseEvent CanExecuteChanged(Me, EventArgs.Empty)
        End Sub
    End Class
    
    
    ''' 异步RelayCommand
    
    Public Class AsyncRelayCommand
        Implements ICommand
        
        Private ReadOnly _execute As Func(Of Object, Task)
        Private ReadOnly _canExecute As Func(Of Object, Boolean)
        Private _isExecuting As Boolean
        
        
        ''' 命令执行时引发的事件
        
        Public Event CanExecuteChanged As EventHandler _
            Implements ICommand.CanExecuteChanged
        
        
        ''' 获取命令是否正在执行
        
        Public ReadOnly Property IsExecuting As Boolean
            Get
                Return _isExecuting
            End Get
        End Property
        
        
        ''' 初始化AsyncRelayCommand类的新实例
        
        Public Sub New(execute As Func(Of Object, Task), Optional canExecute As Func(Of Object, Boolean) = Nothing)
            _execute = If(execute, Function(x) Throw New ArgumentNullException(NameOf(execute)))
            _canExecute = canExecute
        End Sub
        
        
        ''' 确定命令是否可以执行
        
        Public Function CanExecute(parameter As Object) As Boolean _
            Implements ICommand.CanExecute
            Return Not _isExecuting AndAlso (_canExecute Is Nothing OrElse _canExecute(parameter))
        End Function
        
        
        ''' 异步执行命令
        
        Public Async Sub Execute(parameter As Object) _
            Implements ICommand.Execute
            _isExecuting = True
            RaiseCanExecuteChanged()
            
            Try
                Await _execute(parameter)
            Finally
                _isExecuting = False
                RaiseCanExecuteChanged()
            End Try
        End Sub
        
        
        ''' 引发CanExecuteChanged事件
        
        Public Sub RaiseCanExecuteChanged()
            RaiseEvent CanExecuteChanged(Me, EventArgs.Empty)
        End Sub
    End Class
    
    
    ''' 带有返回值的异步RelayCommand
    
    Public Class AsyncRelayCommand(Of TResult)
        Implements ICommand
        
        Private ReadOnly _execute As Func(Of Object, Task(Of TResult))
        Private ReadOnly _canExecute As Func(Of Object, Boolean)
        Private ReadOnly _callback As Action(Of TResult)
        Private _isExecuting As Boolean
        
        
        ''' 命令执行时引发的事件
        
        Public Event CanExecuteChanged As EventHandler _
            Implements ICommand.CanExecuteChanged
        
        
        ''' 获取命令是否正在执行
        
        Public ReadOnly Property IsExecuting As Boolean
            Get
                Return _isExecuting
            End Get
        End Property
        
        
        ''' 初始化AsyncRelayCommand类的新实例
        
        Public Sub New(execute As Func(Of Object, Task(Of TResult)), 
                      Optional callback As Action(Of TResult) = Nothing,
                      Optional canExecute As Func(Of Object, Boolean) = Nothing)
            _execute = If(execute, Function(x) Throw New ArgumentNullException(NameOf(execute)))
            _callback = callback
            _canExecute = canExecute
        End Sub
        
        
        ''' 确定命令是否可以执行
        
        Public Function CanExecute(parameter As Object) As Boolean _
            Implements ICommand.CanExecute
            Return Not _isExecuting AndAlso (_canExecute Is Nothing OrElse _canExecute(parameter))
        End Function
        
        
        ''' 异步执行命令
        
        Public Async Sub Execute(parameter As Object) _
            Implements ICommand.Execute
            _isExecuting = True
            RaiseCanExecuteChanged()
            
            Try
                Dim result = Await _execute(parameter)
                _callback?.Invoke(result)
            Finally
                _isExecuting = False
                RaiseCanExecuteChanged()
            End Try
        End Sub
        
        
        ''' 引发CanExecuteChanged事件
        
        Public Sub RaiseCanExecuteChanged()
            RaiseEvent CanExecuteChanged(Me, EventArgs.Empty)
        End Sub
    End Class
    
    
    ''' 取消命令
    
    Public Class CancelCommand
        Implements ICommand
        
        Private ReadOnly _cancellationTokenSource As Threading.CancellationTokenSource
        
        
        ''' 命令执行时引发的事件
        
        Public Event CanExecuteChanged As EventHandler _
            Implements ICommand.CanExecuteChanged
        
        
        ''' 初始化CancelCommand类的新实例
        
        Public Sub New(cancellationTokenSource As Threading.CancellationTokenSource)
            _cancellationTokenSource = cancellationTokenSource
        End Sub
        
        
        ''' 总是可以执行取消命令
        
        Public Function CanExecute(parameter As Object) As Boolean _
            Implements ICommand.CanExecute
            Return Not _cancellationTokenSource.IsCancellationRequested
        End Function
        
        
        ''' 执行取消命令
        
        Public Sub Execute(parameter As Object) _
            Implements ICommand.Execute
            _cancellationTokenSource.Cancel()
            RaiseCanExecuteChanged()
        End Sub
        
        
        ''' 引发CanExecuteChanged事件
        Public Sub RaiseCanExecuteChanged()
            RaiseEvent CanExecuteChanged(Me, EventArgs.Empty)
        End Sub
    End Class
End Namespace