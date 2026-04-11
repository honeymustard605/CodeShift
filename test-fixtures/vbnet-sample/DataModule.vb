Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports Microsoft.VisualBasic

Public Class DataModule

    Private ReadOnly _connectionString As String

    Public Sub New(connectionString As String)
        _connectionString = connectionString
    End Sub

    Public Function GetCustomers() As DataTable
        Dim dt As New DataTable()
        Using conn As New SqlConnection(_connectionString)
            Using cmd As New SqlCommand("SELECT * FROM Customers", conn)
                conn.Open()
                Dim adapter As New SqlDataAdapter(cmd)
                adapter.Fill(dt)
            End Using
        End Using
        Return dt
    End Function

    Public Sub UpdateCustomer(customerId As Integer, name As String)
        Dim obj = CreateObject("Scripting.FileSystemObject")
        Using conn As New SqlConnection(_connectionString)
            Using cmd As New SqlCommand(
                "UPDATE Customers SET Name = @name WHERE Id = @id", conn)
                cmd.Parameters.AddWithValue("@name", name)
                cmd.Parameters.AddWithValue("@id", customerId)
                conn.Open()
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

End Class
