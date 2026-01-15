Imports System
Imports System.Globalization
Imports System.Windows
Imports System.Windows.Data

Namespace DeploymentAutomationGUI.Converters
    
    ''' 空值到可见性转换器
    
    Public Class NullToVisibilityConverter
        Implements IValueConverter
        
        
        ''' 是否反转逻辑
        
        Public Property Invert As Boolean = False
        
        
        ''' 空字符串是否视为空值
        
        Public Property TreatEmptyStringAsNull As Boolean = True
        
        Public Function Convert(value As Object, targetType As Type, 
                               parameter As Object, culture As CultureInfo) As Object _
                               Implements IValueConverter.Convert
            
            Dim isNull As Boolean = False
            
            If value Is Nothing Then
                isNull = True
            ElseIf TreatEmptyStringAsNull AndAlso TypeOf value Is String Then
                isNull = String.IsNullOrEmpty(value.ToString())
            ElseIf TypeOf value Is String Then
                isNull = value Is Nothing
            ElseIf TypeOf value Is DBNull Then
                isNull = True
            Else
                ' 检查集合是否为空
                Dim enumerable = TryCast(value, System.Collections.IEnumerable)
                If enumerable IsNot Nothing Then
                    isNull = Not enumerable.GetEnumerator().MoveNext()
                End If
            End If
            
            If Invert Then
                isNull = Not isNull
            End If
            
            Return If(isNull, Visibility.Collapsed, Visibility.Visible)
        End Function
        
        Public Function ConvertBack(value As Object, targetType As Type, 
                                   parameter As Object, culture As CultureInfo) As Object _
                                   Implements IValueConverter.ConvertBack
            Return Nothing
        End Function
    End Class
End Namespace