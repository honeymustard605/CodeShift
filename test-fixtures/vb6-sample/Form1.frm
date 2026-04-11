VERSION 5.00
Object = "{65E121D4-0C60-11D2-A9FC-0000F8754DA1}#2.0#0"; "MSWINSCK.OCX"
Begin VB.Form Form1
   Caption         =   "Legacy App"
   ClientHeight    =   3195
   ClientLeft      =   60
   ClientTop       =   645
   ClientWidth     =   4680
   LinkTopic       =   "Form1"
   ScaleHeight     =   3195
   ScaleWidth      =   4680
   StartUpPosition =   3  'Windows Default
   Begin MSFlexGridLib.MSFlexGrid Grid1
      Height          =   2055
      Left            =   120
      TabIndex        =   0
      Top             =   120
      Width           =   4455
   End
End

Attribute VB_Name = "Form1"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False

Private Sub Form_Load()
    Call Main
    Grid1.Rows = 10
    Grid1.Cols = 3
End Sub
