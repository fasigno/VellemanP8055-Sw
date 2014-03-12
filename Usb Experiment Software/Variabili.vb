Imports System.Net.Sockets

'Usb Experiment Software, Velleman P8055 usb card control software with network functions.
'High school exam project, Fabrizio Signoretti - fasigno37@gmail.com
'Repository:github.com/fasigno/VellemanP8055-Sw.git
'Under GPLv3.

Module Variabili
    Public Declare Function SearchDevices Lib "k8055d.dll" () As Integer
    Public Declare Function SetCurrentDevice Lib "k8055d.dll" (ByVal Address As Integer) As Integer

    'variabili per la connessione in rete:
    Public server As Boolean
    Public connesso As Boolean

    ' variabili per client e server :
    Public Client_str1 As New TcpClient 'client del netstr.
    Public Client_str2 As New TcpClient 'client del puls_str.
    Public Client_str3 As New TcpClient 'client dell'invio conferma.

    'Stream dati :
    Public NetStr As NetworkStream

    'stream esclusivo per i pulsanti:
    Public Puls_str As NetworkStream

    'Stream di conferma ricezione dati :
    Public conf_str As NetworkStream


    ' variabili per server :
    Public Listener_str1 As New TcpListener(20)
    Public Listener_str2 As New TcpListener(25)
    Public Listener_str3 As New TcpListener(30)

End Module
