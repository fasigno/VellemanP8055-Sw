'Usb Experiment Software, Velleman P8055 usb card control software with network functions.
'High school exam project, Fabrizio Signoretti - fasigno37@gmail.com
'Repository:github.com/fasigno/VellemanP8055-Sw.git
'Under GPLv3.

Public Class Card_in_uso
    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        Dim h As Integer

        h = SetCurrentDevice(Val(ComboBox1.Text))

        Select Case h
            Case -1
                MsgBox("Card non connessa")
            Case 0, 1, 2, 3

        End Select

        Form1.Label7.Text = Str(h)

        Me.Close()
    End Sub

    Private Sub Card_in_uso_Disposed(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Disposed
        SetCurrentDevice(0)
    End Sub

    Private Sub Card_in_uso_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        Dim dev As Integer

        dev = SearchDevices()

        Select Case dev
            Case 0
                ComboBox1.Items.Add("Nessuna Card trovata")
                Button1.Enabled = False
            Case dev > 0
                ComboBox1.Items.Add(dev)
        End Select

    End Sub
End Class