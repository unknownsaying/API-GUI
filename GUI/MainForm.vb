Imports System.Net.Http
Imports System.Text
Imports System.Text.Json
Imports System.Windows.Forms

Public Class MainForm
    Private ReadOnly httpClient As New HttpClient()
    Private Const API_BASE_URL As String = "http://localhost:5000/api"
    Private currentSayings As List(Of Saying) = New List(Of Saying)()
    
    Public Class Saying
        Public Property Id As Integer
        Public Property Content As String
        Public Property Author As String
        Public Property Category As String
        Public Property CreatedDate As String
        Public Property LastModified As String
    End Class
    
    Public Class ApiResponse
        Public Property Success As Boolean
        Public Property Message As String
        Public Property Data As Object
        Public Property Count As Integer
    End Class
    
    Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' 设置窗体属性
        Me.Text = "Unknown Sayings Manager"
        Me.Icon = My.Resources.AppIcon ' 如果有图标资源
        
        ' 初始化UI
        InitializeDataGridView()
        LoadCategories()
        CheckApiConnection()
        
        ' 加载数据
        LoadSayings()
    End Sub
    
    Private Sub InitializeDataGridView()
        ' 配置DataGridView
        With dgvSayings
            .AutoGenerateColumns = False
            .AllowUserToAddRows = False
            .AllowUserToDeleteRows = False
            .ReadOnly = True
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect
            .MultiSelect = False
            
            ' 清除现有列
            .Columns.Clear()
            
            ' 添加列
            .Columns.Add("Id", "ID")
            .Columns("Id").DataPropertyName = "Id"
            .Columns("Id").Width = 50
            
            .Columns.Add("Content", "Content")
            .Columns("Content").DataPropertyName = "Content"
            .Columns("Content").Width = 300
            
            .Columns.Add("Author", "Author")
            .Columns("Author").DataPropertyName = "Author"
            .Columns("Author").Width = 100
            
            .Columns.Add("Category", "Category")
            .Columns("Category").DataPropertyName = "Category"
            .Columns("Category").Width = 100
            
            .Columns.Add("CreatedDate", "Created")
            .Columns("CreatedDate").DataPropertyName = "CreatedDate"
            .Columns("CreatedDate").Width = 120
        End With
    End Sub
    
    Private Sub LoadCategories()
        ' 预定义分类
        cmbCategory.Items.AddRange({"All", "Philosophy", "Literature", "Education", "Motivation", "General", "Science", "Art"})
        cmbCategory.SelectedIndex = 0
        
        cmbNewCategory.Items.AddRange({"General", "Philosophy", "Literature", "Education", "Motivation", "Science", "Art"})
        cmbNewCategory.SelectedIndex = 0
        
        cmbEditCategory.Items.AddRange({"General", "Philosophy", "Literature", "Education", "Motivation", "Science", "Art"})
        cmbEditCategory.SelectedIndex = 0
    End Sub
    
    Private Async Sub CheckApiConnection()
        Try
            Dim response = Await httpClient.GetAsync("http://localhost:5000/")
            If response.IsSuccessStatusCode Then
                lblStatus.Text = "API Connected"
                lblStatus.ForeColor = Color.Green
            Else
                lblStatus.Text = "API Error"
                lblStatus.ForeColor = Color.Red
            End If
        Catch ex As Exception
            lblStatus.Text = "API Not Connected"
            lblStatus.ForeColor = Color.Red
            MessageBox.Show($"Cannot connect to API: {ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    
    Private Async Sub LoadSayings()
        Try
            Cursor = Cursors.WaitCursor
            lblStatus.Text = "Loading..."
            
            Dim response = Await httpClient.GetAsync($"{API_BASE_URL}/sayings")
            
            If response.IsSuccessStatusCode Then
                Dim jsonString = Await response.Content.ReadAsStringAsync()
                Dim apiResponse = JsonSerializer.Deserialize(Of ApiResponse)(jsonString, New JsonSerializerOptions With {.PropertyNameCaseInsensitive = True})
                
                If apiResponse.Success Then
                    If apiResponse.Data IsNot Nothing Then
                        ' 转换数据
                        Dim sayingsJson = apiResponse.Data.ToString()
                        currentSayings = JsonSerializer.Deserialize(Of List(Of Saying))(sayingsJson, New JsonSerializerOptions With {.PropertyNameCaseInsensitive = True})
                        
                        ' 绑定到DataGridView
                        dgvSayings.DataSource = currentSayings
                        lblCount.Text = $"Total: {currentSayings.Count} sayings"
                        lblStatus.Text = "Data loaded successfully"
                    End If
                Else
                    MessageBox.Show($"API Error: {apiResponse.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End If
            Else
                MessageBox.Show($"HTTP Error: {response.StatusCode}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        Catch ex As Exception
            MessageBox.Show($"Error loading sayings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            lblStatus.Text = "Load failed"
        Finally
            Cursor = Cursors.Default
        End Try
    End Sub
    
    Private Sub btnLoad_Click(sender As Object, e As EventArgs) Handles btnLoad.Click
        LoadSayings()
    End Sub
    
    Private Sub btnRefresh_Click(sender As Object, e As EventArgs) Handles btnRefresh.Click
        LoadSayings()
    End Sub
    
    Private Async Sub btnSearch_Click(sender As Object, e As EventArgs) Handles btnSearch.Click
        Try
            Cursor = Cursors.WaitCursor
            Dim searchText = txtSearch.Text.Trim()
            Dim category = cmbCategory.Text
            
            Dim url = $"{API_BASE_URL}/sayings/search"
            Dim hasParams = False
            
            If Not String.IsNullOrEmpty(searchText) Then
                url += $"?q={Uri.EscapeDataString(searchText)}"
                hasParams = True
            End If
            
            If category <> "All" AndAlso Not String.IsNullOrEmpty(category) Then
                url += If(hasParams, "&", "?") & $"category={Uri.EscapeDataString(category)}"
                hasParams = True
            End If
            
            Dim response = Await httpClient.GetAsync(url)
            
            If response.IsSuccessStatusCode Then
                Dim jsonString = Await response.Content.ReadAsStringAsync()
                Dim apiResponse = JsonSerializer.Deserialize(Of ApiResponse)(jsonString, New JsonSerializerOptions With {.PropertyNameCaseInsensitive = True})
                
                If apiResponse.Success Then
                    If apiResponse.Data IsNot Nothing Then
                        Dim sayingsJson = apiResponse.Data.ToString()
                        Dim filteredSayings = JsonSerializer.Deserialize(Of List(Of Saying))(sayingsJson, New JsonSerializerOptions With {.PropertyNameCaseInsensitive = True})
                        dgvSayings.DataSource = filteredSayings
                        lblCount.Text = $"Found: {filteredSayings.Count} sayings"
                        lblStatus.Text = "Search completed"
                    End If
                End If
            End If
        Catch ex As Exception
            MessageBox.Show($"Search error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            Cursor = Cursors.Default
        End Try
    End Sub
    
    Private Sub btnClearSearch_Click(sender As Object, e As EventArgs) Handles btnClearSearch.Click
        txtSearch.Text = ""
        cmbCategory.SelectedIndex = 0
        LoadSayings()
    End Sub
    
    Private Sub dgvSayings_SelectionChanged(sender As Object, e As EventArgs) Handles dgvSayings.SelectionChanged
        If dgvSayings.SelectedRows.Count > 0 Then
            Dim selectedRow = dgvSayings.SelectedRows(0)
            If selectedRow.DataBoundItem IsNot Nothing Then
                Dim saying = CType(selectedRow.DataBoundItem, Saying)
                DisplaySayingDetails(saying)
            End If
        End If
    End Sub
    
    Private Sub DisplaySayingDetails(saying As Saying)
        txtEditId.Text = saying.Id.ToString()
        txtEditContent.Text = saying.Content
        txtEditAuthor.Text = saying.Author
        
        ' 设置分类
        Dim categoryIndex = cmbEditCategory.Items.IndexOf(saying.Category)
        If categoryIndex >= 0 Then
            cmbEditCategory.SelectedIndex = categoryIndex
        Else
            cmbEditCategory.SelectedIndex = 0
        End If
        
        lblEditCreated.Text = saying.CreatedDate
        lblEditModified.Text = saying.LastModified
    End Sub
    
    Private Async Sub btnAdd_Click(sender As Object, e As EventArgs) Handles btnAdd.Click
        Dim content = txtNewContent.Text.Trim()
        Dim author = txtNewAuthor.Text.Trim()
        Dim category = cmbNewCategory.Text
        
        If String.IsNullOrEmpty(content) Then
            MessageBox.Show("Content cannot be empty", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtNewContent.Focus()
            Return
        End If
        
        Try
            Cursor = Cursors.WaitCursor
            
            Dim sayingData = New With {
                .content = content,
                .author = If(String.IsNullOrEmpty(author), "Unknown", author),
                .category = category
            }
            
            Dim json = JsonSerializer.Serialize(sayingData)
            Dim contentData = New StringContent(json, Encoding.UTF8, "application/json")
            
            Dim response = Await httpClient.PostAsync($"{API_BASE_URL}/sayings", contentData)
            
            If response.IsSuccessStatusCode Then
                Dim responseJson = Await response.Content.ReadAsStringAsync()
                Dim apiResponse = JsonSerializer.Deserialize(Of ApiResponse)(responseJson, New JsonSerializerOptions With {.PropertyNameCaseInsensitive = True})
                
                If apiResponse.Success Then
                    MessageBox.Show("Saying added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    
                    ' 清除输入
                    txtNewContent.Text = ""
                    txtNewAuthor.Text = ""
                    cmbNewCategory.SelectedIndex = 0
                    
                    ' 重新加载数据
                    LoadSayings()
                End If
            Else
                Dim errorText = Await response.Content.ReadAsStringAsync()
                MessageBox.Show($"Failed to add saying: {errorText}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        Catch ex As Exception
            MessageBox.Show($"Error adding saying: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            Cursor = Cursors.Default
        End Try
    End Sub
    
    Private Async Sub btnUpdate_Click(sender As Object, e As EventArgs) Handles btnUpdate.Click
        If String.IsNullOrEmpty(txtEditId.Text) OrElse Not IsNumeric(txtEditId.Text) Then
            MessageBox.Show("Please select a saying to update", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        
        Dim id = Integer.Parse(txtEditId.Text)
        Dim content = txtEditContent.Text.Trim()
        Dim author = txtEditAuthor.Text.Trim()
        Dim category = cmbEditCategory.Text
        
        If String.IsNullOrEmpty(content) Then
            MessageBox.Show("Content cannot be empty", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtEditContent.Focus()
            Return
        End If
        
        Try
            Cursor = Cursors.WaitCursor
            
            Dim sayingData = New With {
                .content = content,
                .author = If(String.IsNullOrEmpty(author), "Unknown", author),
                .category = category
            }
            
            Dim json = JsonSerializer.Serialize(sayingData)
            Dim contentData = New StringContent(json, Encoding.UTF8, "application/json")
            
            Dim response = Await httpClient.PutAsync($"{API_BASE_URL}/sayings/{id}", contentData)
            
            If response.IsSuccessStatusCode Then
                Dim responseJson = Await response.Content.ReadAsStringAsync()
                Dim apiResponse = JsonSerializer.Deserialize(Of ApiResponse)(responseJson, New JsonSerializerOptions With {.PropertyNameCaseInsensitive = True})
                
                If apiResponse.Success Then
                    MessageBox.Show("Saying updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    LoadSayings()
                End If
            Else
                Dim errorText = Await response.Content.ReadAsStringAsync()
                MessageBox.Show($"Failed to update saying: {errorText}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        Catch ex As Exception
            MessageBox.Show($"Error updating saying: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            Cursor = Cursors.Default
        End Try
    End Sub
    
    Private Async Sub btnDelete_Click(sender As Object, e As EventArgs) Handles btnDelete.Click
        If String.IsNullOrEmpty(txtEditId.Text) OrElse Not IsNumeric(txtEditId.Text) Then
            MessageBox.Show("Please select a saying to delete", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        
        Dim id = Integer.Parse(txtEditId.Text)
        
        Dim result = MessageBox.Show($"Are you sure you want to delete saying #{id}?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
        
        If result = DialogResult.Yes Then
            Try
                Cursor = Cursors.WaitCursor
                
                Dim response = Await httpClient.DeleteAsync($"{API_BASE_URL}/sayings/{id}")
                
                If response.IsSuccessStatusCode Then
                    Dim responseJson = Await response.Content.ReadAsStringAsync()
                    Dim apiResponse = JsonSerializer.Deserialize(Of ApiResponse)(responseJson, New JsonSerializerOptions With {.PropertyNameCaseInsensitive = True})
                    
                    If apiResponse.Success Then
                        MessageBox.Show("Saying deleted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        
                        ' 清除详情区域
                        ClearDetails()
                        
                        ' 重新加载数据
                        LoadSayings()
                    End If
                Else
                    Dim errorText = Await response.Content.ReadAsStringAsync()
                    MessageBox.Show($"Failed to delete saying: {errorText}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End If
            Catch ex As Exception
                MessageBox.Show($"Error deleting saying: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Finally
                Cursor = Cursors.Default
            End Try
        End If
    End Sub
    
    Private Sub ClearDetails()
        txtEditId.Text = ""
        txtEditContent.Text = ""
        txtEditAuthor.Text = ""
        cmbEditCategory.SelectedIndex = 0
        lblEditCreated.Text = ""
        lblEditModified.Text = ""
    End Sub
    
    Private Sub btnExport_Click(sender As Object, e As EventArgs) Handles btnExport.Click
        If currentSayings Is Nothing OrElse currentSayings.Count = 0 Then
            MessageBox.Show("No data to export", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If
        
        Using sfd As New SaveFileDialog()
            sfd.Filter = "CSV Files (*.csv)|*.csv|JSON Files (*.json)|*.json|Text Files (*.txt)|*.txt"
            sfd.Title = "Export Sayings"
            sfd.FileName = $"sayings_export_{DateTime.Now:yyyyMMdd_HHmmss}"
            
            If sfd.ShowDialog() = DialogResult.OK Then
                Try
                    Select Case sfd.FilterIndex
                        Case 1 ' CSV
                            ExportToCsv(sfd.FileName)
                        Case 2 ' JSON
                            ExportToJson(sfd.FileName)
                        Case 3 ' Text
                            ExportToText(sfd.FileName)
                    End Select
                    
                    MessageBox.Show($"Data exported successfully to {sfd.FileName}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Catch ex As Exception
                    MessageBox.Show($"Error exporting data: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End If
        End Using
    End Sub
    
    Private Sub ExportToCsv(filePath As String)
        Using writer As New System.IO.StreamWriter(filePath)
            ' 写入标题行
            writer.WriteLine("ID,Content,Author,Category,Created Date,Last Modified")
            
            ' 写入数据行
            For Each saying In currentSayings
                writer.WriteLine($"{saying.Id},""{saying.Content}"",{saying.Author},{saying.Category},{saying.CreatedDate},{saying.LastModified}")
            Next
        End Using
    End Sub
    
    Private Sub ExportToJson(filePath As String)
        Dim json = JsonSerializer.Serialize(currentSayings, New JsonSerializerOptions With {.WriteIndented = True})
        System.IO.File.WriteAllText(filePath, json)
    End Sub
    
    Private Sub ExportToText(filePath As String)
        Using writer As New System.IO.StreamWriter(filePath)
            writer.WriteLine($"Unknown Sayings Export - {DateTime.Now}")
            writer.WriteLine($"Total: {currentSayings.Count} sayings")
            writer.WriteLine("=" & New String("=", 60))
            writer.WriteLine()
            
            For Each saying In currentSayings
                writer.WriteLine($"ID: {saying.Id}")
                writer.WriteLine($"Content: {saying.Content}")
                writer.WriteLine($"Author: {saying.Author}")
                writer.WriteLine($"Category: {saying.Category}")
                writer.WriteLine($"Created: {saying.CreatedDate}")
                writer.WriteLine($"Modified: {saying.LastModified}")
                writer.WriteLine(New String("-", 60))
                writer.WriteLine()
            Next
        End Using
    End Sub
    
    Private Sub btnExit_Click(sender As Object, e As EventArgs) Handles btnExit.Click
        Me.Close()
    End Sub
End Class