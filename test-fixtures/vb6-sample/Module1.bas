Attribute VB_Name = "Module1"
Option Explicit

Public Sub Main()
    Dim conn As Object
    Set conn = CreateObject("ADODB.Connection")
    conn.Open "Provider=SQLOLEDB;Data Source=LEGACYDB;Initial Catalog=Northwind;Integrated Security=SSPI;"

    Dim rs As Object
    Set rs = CreateObject("ADODB.Recordset")
    rs.Open "SELECT CustomerID, CompanyName FROM Customers", conn

    Do While Not rs.EOF
        MsgBox rs("CompanyName")
        rs.MoveNext
    Loop

    rs.Close
    conn.Close
    Set rs = Nothing
    Set conn = Nothing
End Sub
