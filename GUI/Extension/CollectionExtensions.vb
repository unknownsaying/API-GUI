Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.CompilerServices

Namespace DeploymentAutomationGUI.Extensions
    
    ''' 集合扩展方法
    
    Public Module CollectionExtensions
        
        
        ''' 检查集合是否为null或空
        
        <Extension>
        Public Function IsNullOrEmpty(Of T)(collection As IEnumerable(Of T)) As Boolean
            Return collection Is Nothing OrElse Not collection.Any()
        End Function
        
        
        ''' 安全地遍历集合，如果集合为null则不执行
        
        <Extension>
        Public Sub ForEachSafe(Of T)(collection As IEnumerable(Of T), action As Action(Of T))
            If collection Is Nothing OrElse action Is Nothing Then
                Return
            End If
            
            For Each item In collection
                action(item)
            Next
        End Sub
        
        
        ''' 将集合转换为只读集合
        
        <Extension>
        Public Function AsReadOnly(Of T)(collection As IEnumerable(Of T)) As IReadOnlyCollection(Of T)
            If collection Is Nothing Then
                Return New List(Of T)().AsReadOnly()
            End If
            
            Dim list As IList(Of T)
            If TypeOf collection Is IList(Of T) Then
                list = DirectCast(collection, IList(Of T))
            Else
                list = collection.ToList()
            End If
            
            Return New System.Collections.ObjectModel.ReadOnlyCollection(Of T)(list)
        End Function
        
        
        ''' 查找集合中符合条件的元素的索引
        
        <Extension>
        Public Function FindIndex(Of T)(collection As IEnumerable(Of T), predicate As Func(Of T, Boolean)) As Integer
            If collection Is Nothing Then
                Throw New ArgumentNullException(NameOf(collection))
            End If
            
            If predicate Is Nothing Then
                Throw New ArgumentNullException(NameOf(predicate))
            End If
            
            Dim index = 0
            For Each item In collection
                If predicate(item) Then
                    Return index
                End If
                index += 1
            Next
            
            Return -1
        End Function
        
        
        ''' 查找集合中符合条件的最后一个元素的索引
        
        <Extension>
        Public Function FindLastIndex(Of T)(collection As IEnumerable(Of T), predicate As Func(Of T, Boolean)) As Integer
            If collection Is Nothing Then
                Throw New ArgumentNullException(NameOf(collection))
            End If
            
            If predicate Is Nothing Then
                Throw New ArgumentNullException(NameOf(predicate))
            End If
            
            Dim list = collection.ToList()
            For i As Integer = list.Count - 1 To 0 Step -1
                If predicate(list(i)) Then
                    Return i
                End If
            Next
            
            Return -1
        End Function
        
        
        ''' 获取集合中的最大值，如果集合为空则返回默认值
        
        <Extension>
        Public Function MaxOrDefault(Of T, TResult)(collection As IEnumerable(Of T), 
                                                   selector As Func(Of T, TResult), 
                                                   Optional defaultValue As TResult = Nothing) As TResult
            If collection.IsNullOrEmpty() OrElse selector Is Nothing Then
                Return defaultValue
            End If
            
            Try
                Return collection.Max(selector)
            Catch
                Return defaultValue
            End Try
        End Function
        
        
        ''' 获取集合中的最小值，如果集合为空则返回默认值
        
        <Extension>
        Public Function MinOrDefault(Of T, TResult)(collection As IEnumerable(Of T), 
                                                   selector As Func(Of T, TResult), 
                                                   Optional defaultValue As TResult = Nothing) As TResult
            If collection.IsNullOrEmpty() OrElse selector Is Nothing Then
                Return defaultValue
            End If
            
            Try
                Return collection.Min(selector)
            Catch
                Return defaultValue
            End Try
        End Function
        
        
        ''' 获取集合的平均值，如果集合为空则返回默认值
        
        <Extension>
        Public Function AverageOrDefault(collection As IEnumerable(Of Integer), 
                                        Optional defaultValue As Double = 0) As Double
            If collection.IsNullOrEmpty() Then
                Return defaultValue
            End If
            
            Try
                Return collection.Average()
            Catch
                Return defaultValue
            End Try
        End Function
        
        
        ''' 从集合中随机选择一个元素
        
        <Extension>
        Public Function RandomElement(Of T)(collection As IEnumerable(Of T), 
                                           Optional random As Random = Nothing) As T
            If collection.IsNullOrEmpty() Then
                Return Nothing
            End If
            
            Dim list = collection.ToList()
            If random Is Nothing Then
                random = New Random()
            End If
            
            Dim index = random.Next(0, list.Count)
            Return list(index)
        End Function
        
        
        ''' 从集合中随机选择多个元素
        
        <Extension>
        Public Function RandomElements(Of T)(collection As IEnumerable(Of T), count As Integer, 
                                            Optional random As Random = Nothing) As IEnumerable(Of T)
            If collection.IsNullOrEmpty() OrElse count <= 0 Then
                Return Enumerable.Empty(Of T)()
            End If
            
            Dim list = collection.ToList()
            If count >= list.Count Then
                Return list
            End If
            
            If random Is Nothing Then
                random = New Random()
            End If
            
            ' Fisher-Yates洗牌算法
            Dim result = New List(Of T)(list)
            For i As Integer = result.Count - 1 To 1 Step -1
                Dim j = random.Next(0, i + 1)
                Dim temp = result(i)
                result(i) = result(j)
                result(j) = temp
            Next
            
            Return result.Take(count)
        End Function
        
        
        ''' 将集合按指定大小分块
        
        <Extension>
        Public Function Chunk(Of T)(collection As IEnumerable(Of T), size As Integer) As IEnumerable(Of IEnumerable(Of T))
            If collection Is Nothing Then
                Throw New ArgumentNullException(NameOf(collection))
            End If
            
            If size <= 0 Then
                Throw New ArgumentOutOfRangeException(NameOf(size), "Size must be greater than 0")
            End If
            
            Dim chunk As New List(Of T)(size)
            
            For Each item In collection
                chunk.Add(item)
                If chunk.Count = size Then
                    Yield chunk.ToList()
                    chunk.Clear()
                End If
            Next
            
            If chunk.Count > 0 Then
                Yield chunk.ToList()
            End If
        End Function
        
        
        ''' 将集合转换为字典，处理重复键
        
        <Extension>
        Public Function ToDictionarySafe(Of TKey, TValue)(collection As IEnumerable(Of TValue), 
                                                         keySelector As Func(Of TValue, TKey), 
                                                         Optional duplicateKeyHandler As Func(Of TKey, TValue, TValue, TValue) = Nothing) As Dictionary(Of TKey, TValue)
            If collection Is Nothing Then
                Return New Dictionary(Of TKey, TValue)()
            End If
            
            Dim dictionary = New Dictionary(Of TKey, TValue)()
            
            For Each item In collection
                Dim key = keySelector(item)
                
                If dictionary.ContainsKey(key) Then
                    If duplicateKeyHandler IsNot Nothing Then
                        dictionary(key) = duplicateKeyHandler(key, dictionary(key), item)
                    End If
                Else
                    dictionary.Add(key, item)
                End If
            Next
            
            Return dictionary
        End Function
        
        
        ''' 获取集合中满足条件的元素数量
        
        <Extension>
        Public Function CountIf(Of T)(collection As IEnumerable(Of T), predicate As Func(Of T, Boolean)) As Integer
            If collection Is Nothing OrElse predicate Is Nothing Then
                Return 0
            End If
            
            Return collection.Count(predicate)
        End Function
        
        
        ''' 判断集合中是否包含满足条件的元素
        
        <Extension>
        Public Function ContainsIf(Of T)(collection As IEnumerable(Of T), predicate As Func(Of T, Boolean)) As Boolean
            If collection Is Nothing OrElse predicate Is Nothing Then
                Return False
            End If
            
            Return collection.Any(predicate)
        End Function
        
        
        ''' 获取集合中第一个满足条件的元素，如果没有则返回默认值
        
        <Extension>
        Public Function FirstOrDefault(Of T)(collection As IEnumerable(Of T), 
                                            predicate As Func(Of T, Boolean), 
                                            defaultValue As T) As T
            If collection Is Nothing OrElse predicate Is Nothing Then
                Return defaultValue
            End If
            
            For Each item In collection
                If predicate(item) Then
                    Return item
                End If
            Next
            
            Return defaultValue
        End Function
        
        
        ''' 获取集合中最后一个满足条件的元素，如果没有则返回默认值
        
        <Extension>
        Public Function LastOrDefault(Of T)(collection As IEnumerable(Of T), 
                                           predicate As Func(Of T, Boolean), 
                                           defaultValue As T) As T
            If collection Is Nothing OrElse predicate Is Nothing Then
                Return defaultValue
            End If
            
            Dim result As T = defaultValue
            For Each item In collection
                If predicate(item) Then
                    result = item
                End If
            Next
            
            Return result
        End Function
        
        
        ''' 将集合中的元素连接到字符串
        
        <Extension>
        Public Function JoinToString(Of T)(collection As IEnumerable(Of T), 
                                          Optional separator As String = ", ", 
                                          Optional selector As Func(Of T, String) = Nothing) As String
            If collection.IsNullOrEmpty() Then
                Return String.Empty
            End If
            
            If selector Is Nothing Then
                selector = Function(x) x.ToString()
            End If
            
            Return String.Join(separator, collection.Select(selector))
        End Function
        
        
        ''' 对集合进行深度复制
        
        <Extension>
        Public Function DeepClone(Of T)(collection As IEnumerable(Of T)) As List(Of T)
            If collection Is Nothing Then
                Return New List(Of T)()
            End If
            
            Return New List(Of T)(collection)
        End Function
        
        
        ''' 移除集合中满足条件的所有元素
        
        <Extension>
        Public Function RemoveAll(Of T)(list As IList(Of T), predicate As Func(Of T, Boolean)) As Integer
            If list Is Nothing OrElse predicate Is Nothing Then
                Return 0
            End If
            
            Dim removedCount = 0
            For i As Integer = list.Count - 1 To 0 Step -1
                If predicate(list(i)) Then
                    list.RemoveAt(i)
                    removedCount += 1
                End If
            Next
            
            Return removedCount
        End Function
        
        
        ''' 交换集合中两个元素的位置
        
        <Extension>
        Public Sub Swap(Of T)(list As IList(Of T), index1 As Integer, index2 As Integer)
            If list Is Nothing Then
                Throw New ArgumentNullException(NameOf(list))
            End If
            
            If index1 < 0 OrElse index1 >= list.Count OrElse index2 < 0 OrElse index2 >= list.Count Then
                Throw New ArgumentOutOfRangeException("Index out of range")
            End If
            
            If index1 = index2 Then
                Return
            End If
            
            Dim temp = list(index1)
            list(index1) = list(index2)
            list(index2) = temp
        End Sub
        
        
        ''' 将集合转换为HashSet
        
        <Extension>
        Public Function ToHashSet(Of T)(collection As IEnumerable(Of T), 
                                       Optional comparer As IEqualityComparer(Of T) = Nothing) As HashSet(Of T)
            If collection Is Nothing Then
                Return New HashSet(Of T)(comparer)
            End If
            
            Return If(comparer Is Nothing, 
                     New HashSet(Of T)(collection), 
                     New HashSet(Of T)(collection, comparer))
        End Function
        
        
        ''' 将集合转换为Lookup
        
        <Extension>
        Public Function ToLookup(Of TKey, TValue)(collection As IEnumerable(Of TValue), 
                                                 keySelector As Func(Of TValue, TKey), 
                                                 Optional comparer As IEqualityComparer(Of TKey) = Nothing) As ILookup(Of TKey, TValue)
            If collection Is Nothing Then
                Throw New ArgumentNullException(NameOf(collection))
            End If
            
            Return collection.ToLookup(keySelector, comparer)
        End Function
    End Module
End Namespace