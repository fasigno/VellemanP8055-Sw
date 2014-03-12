Imports System.Net.Sockets

'Usb Experiment Software, Velleman P8055 usb card control software with network functions.
'High school exam project, Fabrizio Signoretti - fasigno37@gmail.com
'Repository:github.com/fasigno/VellemanP8055-Sw.git
'Under GPLv3.

Public Class Rete
    Dim label3 As New Label
    Dim button4 As New Button
    Dim Ip As String

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        server = False

        Button1.Enabled = False
        Panel1.Visible = True

        ' viene definito l'ip di default
        TextBox1.Text = "127"
        TextBox2.Text = "0"
        TextBox3.Text = "0"
        TextBox4.Text = "1"

    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        ProgressBar1.Visible = True
        server = True

        Button2.Enabled = False
        Controls.Add(label3)

        ' viene controllato il collegamento da parte del client.
        Listener_str1.Start()
        Listener_str2.Start()
        Listener_str3.Start()

        tmrControlConnection.Start()
    End Sub

    Private Sub Rete_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        label3.Location = New Point(75, 120)
        label3.Size = New Size(184, 13)
        label3.Text = "Attendere la connessione del client ..."

        If connesso = True Then
            Button1.Enabled = False
            Button2.Enabled = False

            'button4.Location = New Point(101, 19)
            'button4.Size = New Size(73, 28)
            'button4.Text = "Disconnetti"
            'Controls.Add(button4)
        End If

    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click
        ProgressBar1.Visible = True
        Label4.Text = "" ' la label viene azzerata così se prima aveva
        'fallito la ricerca, il messaggio non compare +.

        'preleva l'ip inserito dall'utente.

        If TextBox1.Text <> "" Then
            Ip = TextBox1.Text + "." + TextBox2.Text + "." + TextBox3.Text + "." + TextBox4.Text
        End If

        'Prova a connettersi al server 
        Try
            Client_str1.Connect(Ip, 20)
            Client_str2.Connect(Ip, 25)
            Client_str3.Connect(Ip, 30)

        Catch ex As Exception
            ProgressBar1.Visible = False
            Label4.Text = "Server non trovato!"
        End Try

        'Se è avvenuta la connessione 
        If Client_str1.Connected = True And Client_str2.Connected = True And Client_str3.Connected = True Then
            'Inizializza lo stream 

            NetStr = Client_str1.GetStream
            Puls_str = Client_str2.GetStream
            conf_str = Client_str3.GetStream

            Form1.tmrGetData.Start()
            Form1.Label5.Text = "Connesso"
            connesso = True

            Me.Dispose()

        End If

    End Sub

    Private Sub tmrControlConnection_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tmrControlConnection.Tick
        'Se ci sono connessioni in attesa 

        If Listener_str1.Pending = True Then
            'Inizializza il client 
            Client_str1 = Listener_str1.AcceptTcpClient
            'Inizializza lo stream 

            NetStr = Client_str1.GetStream

            'Termina il controllo del timer 
            tmrControlConnection.Stop()
            'Termina l'ascolto del TcpListener 
            Listener_str1.Stop()
        End If

        If Listener_str2.Pending Then
            'Inizializza il client 
            Client_str2 = Listener_str2.AcceptTcpClient
            'Inizializza lo stream 

            Puls_str = Client_str2.GetStream

            'Termina il controllo del timer 
            tmrControlConnection.Stop()
            'Termina l'ascolto del TcpListener 
            Listener_str2.Stop()
        End If


        If Listener_str3.Pending Then
            'Inizializza il client 
            Client_str3 = Listener_str3.AcceptTcpClient
            'Inizializza lo stream 

            conf_str = Client_str3.GetStream

            'Termina il controllo del timer 
            tmrControlConnection.Stop()
            'Termina l'ascolto del TcpListener 
            Listener_str3.Stop()
        End If


        If Client_str1.Connected And Client_str2.Connected And Client_str3.Connected Then

            Form1.Label5.Text = "Connesso"
            tmrControlConnection.Stop()
            Form1.tmrGetData.Start()

            connesso = True

            Me.Dispose()

        End If

    End Sub

    'Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
    '    Form1.disconnessione_rete()
    '    Me.Dispose()
    'End Sub
End Class