Imports System
Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Data

Namespace DeploymentAutomationGUI.Converters
    
    ''' 字符串到画刷转换器
    
    Public Class StringToBrushConverter
        Implements IValueConverter
        
        Public Function Convert(value As Object, targetType As Type, 
                               parameter As Object, culture As CultureInfo) As Object _
                               Implements IValueConverter.Convert
            If value Is Nothing Then
                Return Brushes.Transparent
            End If
            
            Dim colorName = value.ToString()
            
            Select Case colorName.ToLower()
                Case "success", "green", "ok"
                    Return Brushes.LightGreen
                Case "error", "red", "fail"
                    Return Brushes.LightPink
                Case "warning", "yellow", "warn"
                    Return Brushes.LightYellow
                Case "info", "blue", "information"
                    Return Brushes.LightBlue
                Case "running", "processing", "orange"
                    Return Brushes.LightGoldenrodYellow
                Case Else
                    Try
                        Return New SolidBrush(Color.FromName(colorName))
                    Catch
                        Return Brushes.Transparent
                    End Try
            End Select
        End Function
        
        Public Function ConvertBack(value As Object, targetType As Type, 
                                   parameter As Object, culture As CultureInfo) As Object _
                                   Implements IValueConverter.ConvertBack
            Return Nothing
        End Function
    End Class
End Namespace