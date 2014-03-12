Imports System.Net.Sockets
Imports System.Threading

'Usb Experiment Software, Velleman P8055 usb card control software with network functions.
'High school exam project, Fabrizio Signoretti - fasigno37@gmail.com
'Repository:github.com/fasigno/VellemanP8055-Sw.git
'Under GPLv3.

Public Class Form1
    'dichiarazione delle procedure e funzioni presenti nella dll:
    Private Declare Function OpenDevice Lib "k8055d.dll" (ByVal CardAddress As Integer) As Integer
    Private Declare Sub CloseDevice Lib "k8055d.dll" ()
    Private Declare Function ReadAnalogChannel Lib "k8055d.dll" (ByVal Channel As Integer) As Integer
    Private Declare Sub ReadAllAnalog Lib "k8055d.dll" (ByVal Data1 As Integer, ByVal Data2 As Integer)
    Private Declare Sub OutputAnalogChannel Lib "k8055d.dll" (ByVal Channel As Integer, ByVal Data As Integer)
    Private Declare Sub OutputAllAnalog Lib "k8055d.dll" (ByVal Data1 As Integer, ByVal Data2 As Integer)
    Private Declare Sub ClearAnalogChannel Lib "k8055d.dll" (ByVal Channel As Integer)
    Private Declare Sub SetAllAnalog Lib "k8055d.dll" ()
    Private Declare Sub ClearAllAnalog Lib "k8055d.dll" ()
    Private Declare Sub SetAnalogChannel Lib "k8055d.dll" (ByVal Channel As Integer)
    Private Declare Sub WriteAllDigital Lib "k8055d.dll" (ByVal Data As Integer)
    Private Declare Sub ClearDigitalChannel Lib "k8055d.dll" (ByVal Channel As Integer)
    Private Declare Sub ClearAllDigital Lib "k8055d.dll" ()
    Private Declare Sub SetDigitalChannel Lib "k8055d.dll" (ByVal Channel As Integer)
    Private Declare Sub SetAllDigital Lib "k8055d.dll" ()
    Private Declare Function ReadDigitalChannel Lib "k8055d.dll" (ByVal Channel As Integer) As Boolean
    Private Declare Function ReadAllDigital Lib "k8055d.dll" () As Integer
    Private Declare Function ReadCounter Lib "k8055d.dll" (ByVal CounterNr As Integer) As Integer
    Private Declare Sub ResetCounter Lib "k8055d.dll" (ByVal CounterNr As Integer)
    Private Declare Sub SetCounterDebounceTime Lib "k8055d.dll" (ByVal CounterNr As Integer, ByVal DebounceTime As Integer)

    Private Declare Sub Version Lib "k8055d.dll" ()

    ' definizione degli oggetti che appaiono all'avvio del form.
    Dim Checkbox1 As New CheckBox
    Dim Checkbox2 As New CheckBox
    Dim label1 As New Label

    Dim label2 As New Label
    Dim stato As New Label

    Dim label3 As New Label

    Dim progressbar1 As New ProgressBar

    Dim label4 As New Label

    ' definizione del thread di invio dati:
    Dim back_invio As New Thread(AddressOf invia_dati)
    Dim back_invio_puls As New Thread(AddressOf invia_dati_puls)

    ' definizione variabili :
    Dim data1 As Long
    Dim data2 As Long ' variabili degli AD

    ' la seguente variabile serve ad evitare un ciclaggio infinito che
    ' accadrebbe a causa dell'utilizzo in rete.
    Dim ricevuto As Boolean = False

    Dim usb_conn As Boolean = False ' la variabile indica se la scheda è connessa : True = connesso, False = disconnesso

    Public stream1 As String = "" ' variabili di supporto per i tre stream.
    Public stream2 As String = ""
    Public stream3 As String = ""

    Private Sub Form1_Disposed(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Disposed
        CloseDevice() ' quando il form1 viene chiuso avviene la disconnessione della card.
        disconnessione_rete() ' e la disconnessione dalla rete.
    End Sub

    Public Sub disconnessione_rete()
        Try
            Client_str1.Close()
            Client_str2.Close()

            NetStr.Close()
            Puls_str.Close()
            conf_str.Close()

        Catch ex As Exception
            MsgBox("Si è verificato un problema nello scaricamento di alcuni componenti in fase di chiusura. Il programma è terminerà ugualmente.")
        End Try

    End Sub

    Private Sub Form1_HelpButtonClicked(ByVal sender As Object, ByVal e As System.ComponentModel.CancelEventArgs) Handles Me.HelpButtonClicked
        Version()
    End Sub

    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        ' definizioni proprietà checkbox

        Checkbox1.Location = New Point(258, 116)

        Checkbox2.Location = New Point(258, 139)

        Checkbox1.Text = "SK5"
        Checkbox2.Text = "SK6"

        Checkbox1.Checked = True
        Checkbox2.Checked = True

        ' definizioni proprietà labels
        label1.Location = New Point(213, 9)
        label1.Size = New Size(134, 13)
        label1.Text = "Ricerca scheda in corso..."


        label2.Location = New Point(211, 91)
        label2.AutoSize = True
        label2.Text = "Stato :"


        label3.Location = New Point(166, 116)
        label3.Size = New Size(83, 13)
        label3.Text = "Configurazione :"


        stato.Location = New Point(245, 91)
        stato.AutoSize = True
        stato.Text = ""


        label4.Location = New Point(128, 191)
        label4.AutoSize = True
        label4.Text = "Fare il plug in della scheda ora o attendere se già effettuato."



        ' definizione proprietà progressbar

        progressbar1.Location = New Point(168, 37)
        progressbar1.Size = New Size(225, 20)
        progressbar1.Style = ProgressBarStyle.Marquee


        ' caricamento di tutti gli oggetti sul form prima definiti :
        Controls.Add(label1)
        Controls.Add(label2)
        Controls.Add(label3)
        Controls.Add(label4)
        Controls.Add(stato)
        Controls.Add(progressbar1)
        Controls.Add(Checkbox1)
        Controls.Add(Checkbox2)

        ' vengono impostati i radiobutton per default a 2ms.
        RadioButton7.Checked = True
        RadioButton12.Checked = True


        ' Viene fatto partire il timer che controlla la connessione della board.
        Timer1.Start()

    End Sub

    Private Sub conn_usb() ' la procedura viene richiamata all'avvio del form e permette di aprire il collegamento con l'usb

        If usb_conn = False Then ' viene controllato il collegamento della scheda, se False continua a controllare la sua connessione.

            Dim CardAddress As Integer = 0  'la variabile indica l'indirizzo della scheda che di default è 0, cambia solo in caso di aggiunta di altre schede.
            Dim h As Integer

            CardAddress = 3 - (Checkbox1.CheckState + Checkbox2.CheckState * 2) ' somma i valori dei checkbox in caso si voglia cambiare l'indirizzo della scheda.

            h = OpenDevice(CardAddress) 'viene fatto il collegamento con la scheda.

            'si controlla se il collegamento ha avuto esito positivo.
            Select Case h
                Case 0, 1, 2, 3
                    ' in caso di scheda connessa :
                    stato.Text = "Scheda" + Str(CardAddress) + " connessa"
                    usb_conn = True

                    Label7.Text = h ' la label indica quale scheda è in uso. 


                Case -1
                    ' in caso di scheda disconnessa :
                    stato.Text = "Scheda" + Str(CardAddress) + " non trovata"
                    usb_conn = False

            End Select

        End If

    End Sub
    ' il timer controlla la connessione della scheda.
    Private Sub Timer1_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Timer1.Tick
        conn_usb() 'richiamo procedura per connessione alla scheda :

        If usb_conn = True Then 'se la scheda risulta connessa :
            Timer1.Stop() ' viene stoppato il timer1.

            ' vengono cancellati dal form gli oggetti iniziali.
            cancella_oggetti_iniziali()

            ' vengono visualizzati i nuovi oggetti del form.
            carica_componenti_form()

        End If

    End Sub

    Private Sub cancella_oggetti_iniziali()
        'la procedura cancella gli oggetti iniziali, è stata creata perchè utilizzata in 2 punti differenti.
        label1.Visible = False
        label2.Visible = False
        label3.Visible = False
        label4.Visible = False
        stato.Visible = False
        progressbar1.Visible = False
        Checkbox1.Visible = False
        Checkbox2.Visible = False
    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click
        ' se si fa click su Clear All Digital :

        ClearAllDigital() ' si portano a 0 tutti i led.

        ' vengono impostati su false tutti i checkbox relativi ai led.
        CheckBox3.Checked = False
        CheckBox4.Checked = False
        CheckBox5.Checked = False
        CheckBox6.Checked = False
        CheckBox7.Checked = False
        CheckBox8.Checked = False
        CheckBox9.Checked = False
        CheckBox10.Checked = False

        ' X rete:

        If connesso = True And ricevuto = False Then
            invia_dati("ClearAllDigital()")
        End If

        ricevuto = False
    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        ' un clic su Set On All Digital :

        SetAllDigital() ' imposta tutti i led a 1.

        ' vengono impostati su true tutti i checkbox relativi ai led.

        CheckBox3.Checked = True
        CheckBox4.Checked = True
        CheckBox5.Checked = True
        CheckBox6.Checked = True
        CheckBox7.Checked = True
        CheckBox8.Checked = True
        CheckBox9.Checked = True
        CheckBox10.Checked = True

        ' X rete:

        If connesso = True And ricevuto = False Then
            invia_dati("SetAllDigital()")
        End If

        ricevuto = False
    End Sub

    Private Sub carica_componenti_form()
        ' la procedura visualizza tutti gli oggetti nascosti all'avvio del form.

        Button3.Visible = True
        Button1.Visible = True

        Button4.Visible = True
        Button5.Visible = True

        CheckBox3.Visible = True
        CheckBox4.Visible = True
        CheckBox5.Visible = True
        CheckBox6.Visible = True
        CheckBox7.Visible = True
        CheckBox8.Visible = True
        CheckBox9.Visible = True
        CheckBox10.Visible = True

        CheckBox11.Visible = True


        Panel1.Visible = True

        Label6.Visible = True
        Label7.Visible = True
        Button6.Visible = True

        HScrollBar1.Visible = True
        HScrollBar2.Visible = True
        Label8.Visible = True
        Label9.Visible = True
        Label10.Visible = True
        Label11.Visible = True

        Label12.Visible = True
        Label13.Visible = True

        VScrollBar1.Visible = True
        VScrollBar2.Visible = True
        Label14.Visible = True
        Label15.Visible = True
        Label18.Visible = True
        Label19.Visible = True
        GroupBox1.Visible = True
        GroupBox2.Visible = True

        Label21.Visible = True
        TextBox3.Visible = True
        TextBox4.Visible = True

        Panel3.Visible = True
        Panel4.Visible = True

        If (connesso = True And server = True) Then ' il timer pulsanti si esegue sl sul software server.
            ' back_invio.Start() ' esegue il sottoprocesso invia dati.
            ' back_invio_puls.Start() ' esegue il sottoprocesso invia dati pulsanti.
        ElseIf connesso = False Then
            timer_pulsanti.Start() ' fa partire il timer che controlla la pressione dei pulsanti sulla board.
            Panel2.Visible = True
        End If

    End Sub

    Private Sub Button5_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button5.Click
        If server = True Or connesso = False Then
            SetAllAnalog() ' viene impostata l'uscita analogica al massimo.
        End If

        ' vengono impostate le scroll al massimo.
        HScrollBar1.Value = 255
        HScrollBar2.Value = 255


        ' X rete: In questo caso i dati in rete vanno inviati prima per evitare il conflitto degli eventi relativi.

        'If connesso = True And ricevuto = False Then
        '    invia_dati("SetAllAnalog()")
        'End If

        ricevuto = False
    End Sub

    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button4.Click
        If server = True Or connesso = False Then
            ClearAllAnalog() ' viene impostata l'uscita analogica al minimo.
        End If

        ' vengono impostate le scroll al minimo.
        HScrollBar1.Value = 0
        HScrollBar2.Value = 0

        ' X rete: In questo caso i dati in rete vanno inviati prima per evitare il conflitto degli eventi relativi.
        'If connesso = True And ricevuto = False Then
        '    invia_dati("ClearAllAnalog()")
        'End If

        ricevuto = False
    End Sub

    ' il seguente timer controlla la pressione dei tasti di input sulla board e ne visualizza lo stato sul form.
    Private Sub timer_pulsanti_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles timer_pulsanti.Tick

        If ReadDigitalChannel(1) = True Then
            CheckBox16.Checked = True

            'If connesso = True Then
            '    invia_dati_puls("Pulsante(1) = 1")
            'End If

        Else
            CheckBox16.Checked = False

            'If connesso = True Then
            '    invia_dati_puls("Pulsante(1) = 0")
            'End If

        End If

        If ReadDigitalChannel(2) = True Then
            CheckBox15.Checked = True

            'If connesso = True Then
            '    invia_dati_puls("Pulsante(2) = 1")
            'End If

        Else
            CheckBox15.Checked = False

            'If connesso = True Then
            '    invia_dati_puls("Pulsante(2) = 0")
            'End If

        End If

        If ReadDigitalChannel(3) = True Then
            CheckBox13.Checked = True

            'If connesso = True Then
            '    invia_dati_puls("Pulsante(3) = 1")
            'End If

        Else
            CheckBox13.Checked = False

            'If connesso = True Then
            '    invia_dati_puls("Pulsante(3) = 0")
            'End If

        End If

        If ReadDigitalChannel(4) = True Then
            CheckBox14.Checked = True

            'If connesso = True Then
            '    invia_dati_puls("Pulsante(4) = 1")
            'End If

        Else
            CheckBox14.Checked = False

            'If connesso = True Then
            '    invia_dati_puls("Pulsante(4) = 0")
            'End If

        End If

        If ReadDigitalChannel(5) = True Then
            CheckBox12.Checked = True

            'If connesso = True Then
            '    invia_dati_puls("Pulsante(5) = 1")
            'End If

        Else
            CheckBox12.Checked = False

            'If connesso = True Then
            '    invia_dati_puls("Pulsante(5) = 0")
            'End If

        End If

        ' utilizzo lo stesso timer per i due AD1 e AD2 e contatore:

        ' contatore
        TextBox1.Text = ReadCounter(1)
        TextBox2.Text = ReadCounter(2)

        'AD1 e AD2


        data1 = ReadAnalogChannel(1)
        data2 = ReadAnalogChannel(2)

        Try
            VScrollBar1.Value = data1
            VScrollBar2.Value = data2
        Catch ex As Exception

        End Try

    End Sub

    Private Sub textbox1_textchanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TextBox1.TextChanged
        ' x la rete appena il valore del couter cambia viene inviato al client.
        If server = True Then
            invia_dati("Counter1 = " + TextBox1.Text)
        End If
    End Sub
    Private Sub textbox2_textchanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TextBox2.TextChanged
        ' x la rete appena il valore del couter cambia viene inviato al client.
        If server = True Then
            invia_dati("Counter2 = " + TextBox2.Text)
        End If
    End Sub

    Private Sub CheckBox11_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CheckBox11.CheckedChanged
        Timer_outputTest.Start() ' la pressione del tasto "Output test" fa partire il timer che accende i led in maniera sequenziale.

        ' X rete:

        If connesso = True And ricevuto = False Then
            If CheckBox11.Checked = True Then
                invia_dati("OutputTest = 1")
            Else
                invia_dati("OutputTest = 0")
            End If
        End If

        ricevuto = False

    End Sub

    Private Sub Timer_outputTest_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Timer_outputTest.Tick
        Static i As Integer ' variabile i statica per evitare che venga ridefinita ogi volta che si esegue il tick del timer.

        If CheckBox11.Checked = True Then ' se il tasto "Output Test" è ancora premuto i led continuano ad accendersi.

            i = i + 1 ' viene incrementato i

            SetDigitalChannel(i) ' viene acceso il led i
            ClearDigitalChannel(9 - i) ' viene cancellato il led 9-i

            stato_led(i) ' viene richiamata la procedura che controlla lo stato dei led per impostare correttamente i checkbox sul form.

        Else
            Timer_outputTest.Stop() ' se il tasto test non è + premuto viene stoppato il timer.
        End If

        If i >= 8 Then ' se il contatore i è > 8 allora va azzerato. I led sono in totale 8.
            i = 0
        End If

    End Sub

    Private Sub stato_led(ByVal i) ' stato led controlla se i led sn accesi o no e modifica lo stato dei checkbox sul form.

        Select Case i

            Case 1
                CheckBox3.Checked = True
            Case 2
                CheckBox4.Checked = True
            Case 3
                CheckBox5.Checked = True
            Case 4
                CheckBox6.Checked = True
            Case 5
                CheckBox7.Checked = True
            Case 6
                CheckBox8.Checked = True
            Case 7
                CheckBox9.Checked = True
            Case 8
                CheckBox10.Checked = True
        End Select

        Select Case 9 - i
            Case 1
                CheckBox3.Checked = False
            Case 2
                CheckBox4.Checked = False
            Case 3
                CheckBox5.Checked = False
            Case 4
                CheckBox6.Checked = False
            Case 5
                CheckBox7.Checked = False
            Case 6
                CheckBox8.Checked = False
            Case 7
                CheckBox9.Checked = False
            Case 8
                CheckBox10.Checked = False
        End Select
    End Sub

    ' un click sul tasto "cambia" fa visualizzare il form che permette di cambiare la card in utilizzo nel caso di utilizzo di card multiplo.
    Private Sub Button6_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button6.Click
        Dim cambia_card As New Card_in_uso

        cambia_card.Show()
    End Sub

    ' tutte le seguenti procedure modificano lo stato dei check box al click su di essi.
    ' e vengono impostati sulla board i led relativi.

    ' è stato sostituito il textbox cn lo stream.

    Private Sub CheckBox3_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles CheckBox3.Click

        If connesso = True And ricevuto = True Then

            If Mid(stream1, 10, 1) = 0 Then
                CheckBox3.Checked = False
            Else
                CheckBox3.Checked = True
            End If

        End If


        If CheckBox3.Checked = True Then
            SetDigitalChannel(1)

            ' X rete:

            If connesso = True And ricevuto = False Then
                invia_dati("Led(1) = 1")
            End If

            ricevuto = False

        End If

        If CheckBox3.Checked = False Then
            ClearDigitalChannel(1)

            ' X rete:

            If connesso = True And ricevuto = False Then
                invia_dati("Led(1) = 0")
            End If

            ricevuto = False

        End If
    End Sub

    Private Sub CheckBox4_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles CheckBox4.Click

        If connesso = True And ricevuto = True Then

            If Mid(stream1, 10, 1) = 0 Then
                CheckBox4.Checked = False
            Else
                CheckBox4.Checked = True
            End If

        End If

        If CheckBox4.Checked = True Then
            SetDigitalChannel(2)

            ' X rete:

            If connesso = True And ricevuto = False Then
                invia_dati("Led(2) = 1")
            End If

            ricevuto = False

        End If

        If CheckBox4.Checked = False Then
            ClearDigitalChannel(2)
            ' X rete:

            If connesso = True And ricevuto = False Then
                invia_dati("Led(2) = 0")
            End If

            ricevuto = False
        End If
    End Sub

    Private Sub CheckBox5_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles CheckBox5.Click

        If connesso = True And ricevuto = True Then

            If Mid(stream1, 10, 1) = 0 Then
                CheckBox5.Checked = False
            Else
                CheckBox5.Checked = True
            End If

        End If

        If CheckBox5.Checked = True Then
            SetDigitalChannel(3)

            ' X rete:

            If connesso = True And ricevuto = False Then
                invia_dati("Led(3) = 1")
            End If

            ricevuto = False

        End If

        If CheckBox5.Checked = False Then
            ClearDigitalChannel(3)

            ' X rete:

            If connesso = True And ricevuto = False Then
                invia_dati("Led(3) = 0")
            End If

            ricevuto = False

        End If
    End Sub

    Private Sub CheckBox6_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles CheckBox6.Click

        If connesso = True And ricevuto = True Then

            If Mid(stream1, 10, 1) = 0 Then
                CheckBox6.Checked = False
            Else
                CheckBox6.Checked = True
            End If

        End If

        If CheckBox6.Checked = True Then
            SetDigitalChannel(4)

            ' X rete:

            If connesso = True And ricevuto = False Then
                invia_dati("Led(4) = 1")
            End If

            ricevuto = False

        End If

        If CheckBox6.Checked = False Then
            ClearDigitalChannel(4)

            ' X rete:

            If connesso = True And ricevuto = False Then
                invia_dati("Led(4) = 0")
            End If

            ricevuto = False

        End If
    End Sub

    Private Sub CheckBox7_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles CheckBox7.Click

        If connesso = True And ricevuto = True Then

            If Mid(stream1, 10, 1) = 0 Then
                CheckBox7.Checked = False
            Else
                CheckBox7.Checked = True
            End If

        End If

        If CheckBox7.Checked = True Then
            SetDigitalChannel(5)

            ' X rete:

            If connesso = True And ricevuto = False Then
                invia_dati("Led(5) = 1")
            End If

            ricevuto = False

        End If

        If CheckBox7.Checked = False Then
            ClearDigitalChannel(5)

            ' X rete:

            If connesso = True And ricevuto = False Then
                invia_dati("Led(5) = 0")
            End If

            ricevuto = False

        End If
    End Sub

    Private Sub CheckBox8_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles CheckBox8.Click

        If connesso = True And ricevuto = True Then

            If Mid(stream1, 10, 1) = 0 Then
                CheckBox8.Checked = False
            Else
                CheckBox8.Checked = True
            End If

        End If

        If CheckBox8.Checked = True Then
            SetDigitalChannel(6)

            ' X rete:

            If connesso = True And ricevuto = False Then
                invia_dati("Led(6) = 1")
            End If

            ricevuto = False

        End If

        If CheckBox8.Checked = False Then
            ClearDigitalChannel(6)

            ' X rete:

            If connesso = True And ricevuto = False Then
                invia_dati("Led(6) = 0")
            End If

            ricevuto = False

        End If
    End Sub

    Private Sub CheckBox9_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles CheckBox9.Click

        If connesso = True And ricevuto = True Then

            If Mid(stream1, 10, 1) = 0 Then
                CheckBox9.Checked = False
            Else
                CheckBox9.Checked = True
            End If

        End If

        If CheckBox9.Checked = True Then
            SetDigitalChannel(7)

            ' X rete:

            If connesso = True And ricevuto = False Then
                invia_dati("Led(7) = 1")
            End If

            ricevuto = False

        End If

        If CheckBox9.Checked = False Then
            ClearDigitalChannel(7)

            ' X rete:

            If connesso = True And ricevuto = False Then
                invia_dati("Led(7) = 0")
            End If

            ricevuto = False

        End If
    End Sub

    Private Sub CheckBox10_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles CheckBox10.Click

        If connesso = True And ricevuto = True Then

            If Mid(stream1, 10, 1) = 0 Then
                CheckBox10.Checked = False
            Else
                CheckBox10.Checked = True
            End If

        End If

        If CheckBox10.Checked = True Then
            SetDigitalChannel(8)

            ' X rete:

            If connesso = True And ricevuto = False Then
                invia_dati("Led(8) = 1")
            End If

            ricevuto = False

        End If

        If CheckBox10.Checked = False Then
            ClearDigitalChannel(8)

            ' X rete:

            If connesso = True And ricevuto = False Then
                invia_dati("Led(8) = 0")
            End If

            ricevuto = False

        End If
    End Sub

    Private Sub HScrollBar2_ValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles HScrollBar2.ValueChanged


        ' viene richiamato l'evento e modifica i valori delle scroll.

        Label10.Text = HScrollBar2.Value ' il valore della scrollbar viene visualizzato anche nella label in basso ad essa.

        Label12.Text = Str(Format((5 * HScrollBar2.Value / 255), "###0.000")) + " Volt" ' viene visualizzato il valore in Volt sul form.

        If server = True Or connesso = False Then
            OutputAnalogChannel(1, HScrollBar2.Value) ' viene impostata l'uscita analogica in base al valore della scroll2
        End If

        ' X rete:

        If connesso = True And ricevuto = False Then
            invia_dati("DA1 = " + Str(HScrollBar2.Value))
        End If

        ricevuto = False

    End Sub

    Private Sub HScrollBar1_ValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles HScrollBar1.ValueChanged

        ' viene richiamato l'evento e modifica i valori delle scroll.

        Label11.Text = HScrollBar1.Value ' il valore della scrollbar viene visualizzato anche nella label in basso ad essa.

        Label13.Text = Str(Format((5 * HScrollBar1.Value / 255), "###0.000")) + " Volt" ' viene visualizzato il valore in Volt sul form.

        If server = True Or connesso = False Then
            OutputAnalogChannel(2, HScrollBar1.Value) ' viene impostata l'uscita analogica in base al valore della scroll1
        End If

        ' X rete:

        If connesso = True And ricevuto = False Then
            invia_dati("DA2 = " + Str(HScrollBar1.Value))
        End If

        ricevuto = False

    End Sub

    Private Sub VScrollBar1_ValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles VScrollBar1.ValueChanged

        Label18.Text = VScrollBar1.Value

        Dim val As Long

        val = (VScrollBar1.Value) / 25

        Select Case val
            Case 0
                PictureBox1.BackColor = Color.White
                PictureBox2.BackColor = Color.White
                PictureBox3.BackColor = Color.White
                PictureBox4.BackColor = Color.White
                PictureBox5.BackColor = Color.White
                PictureBox6.BackColor = Color.White
                PictureBox7.BackColor = Color.White
                PictureBox8.BackColor = Color.White
                PictureBox9.BackColor = Color.White
                PictureBox10.BackColor = Color.White
            Case 1
                PictureBox1.BackColor = Color.Red
                PictureBox2.BackColor = Color.White
                PictureBox3.BackColor = Color.White
                PictureBox4.BackColor = Color.White
                PictureBox5.BackColor = Color.White
                PictureBox6.BackColor = Color.White
                PictureBox7.BackColor = Color.White
                PictureBox8.BackColor = Color.White
                PictureBox9.BackColor = Color.White
                PictureBox10.BackColor = Color.White
            Case 2
                PictureBox1.BackColor = Color.Red
                PictureBox2.BackColor = Color.Red
                PictureBox3.BackColor = Color.White
                PictureBox4.BackColor = Color.White
                PictureBox5.BackColor = Color.White
                PictureBox6.BackColor = Color.White
                PictureBox7.BackColor = Color.White
                PictureBox8.BackColor = Color.White
                PictureBox9.BackColor = Color.White
                PictureBox10.BackColor = Color.White
            Case 3
                PictureBox1.BackColor = Color.Red
                PictureBox2.BackColor = Color.Red
                PictureBox3.BackColor = Color.Red
                PictureBox4.BackColor = Color.White
                PictureBox5.BackColor = Color.White
                PictureBox6.BackColor = Color.White
                PictureBox7.BackColor = Color.White
                PictureBox8.BackColor = Color.White
                PictureBox9.BackColor = Color.White
                PictureBox10.BackColor = Color.White
            Case 4
                PictureBox1.BackColor = Color.Red
                PictureBox2.BackColor = Color.Red
                PictureBox3.BackColor = Color.Red
                PictureBox4.BackColor = Color.Red
                PictureBox5.BackColor = Color.White
                PictureBox6.BackColor = Color.White
                PictureBox7.BackColor = Color.White
                PictureBox8.BackColor = Color.White
                PictureBox9.BackColor = Color.White
                PictureBox10.BackColor = Color.White
            Case 5
                PictureBox1.BackColor = Color.Red
                PictureBox2.BackColor = Color.Red
                PictureBox3.BackColor = Color.Red
                PictureBox4.BackColor = Color.Red
                PictureBox5.BackColor = Color.Red
                PictureBox6.BackColor = Color.White
                PictureBox7.BackColor = Color.White
                PictureBox8.BackColor = Color.White
                PictureBox9.BackColor = Color.White
                PictureBox10.BackColor = Color.White
            Case 6
                PictureBox1.BackColor = Color.Red
                PictureBox2.BackColor = Color.Red
                PictureBox3.BackColor = Color.Red
                PictureBox4.BackColor = Color.Red
                PictureBox5.BackColor = Color.Red
                PictureBox6.BackColor = Color.White
                PictureBox7.BackColor = Color.White
                PictureBox8.BackColor = Color.White
                PictureBox9.BackColor = Color.White
                PictureBox10.BackColor = Color.White
            Case 7
                PictureBox1.BackColor = Color.Red
                PictureBox2.BackColor = Color.Red
                PictureBox3.BackColor = Color.Red
                PictureBox4.BackColor = Color.Red
                PictureBox5.BackColor = Color.Red
                PictureBox6.BackColor = Color.Red
                PictureBox7.BackColor = Color.Red
                PictureBox8.BackColor = Color.White
                PictureBox9.BackColor = Color.White
                PictureBox10.BackColor = Color.White
            Case 8
                PictureBox1.BackColor = Color.Red
                PictureBox2.BackColor = Color.Red
                PictureBox3.BackColor = Color.Red
                PictureBox4.BackColor = Color.Red
                PictureBox5.BackColor = Color.Red
                PictureBox6.BackColor = Color.Red
                PictureBox7.BackColor = Color.Red
                PictureBox8.BackColor = Color.Red
                PictureBox9.BackColor = Color.White
                PictureBox10.BackColor = Color.White
            Case 9
                PictureBox1.BackColor = Color.Red
                PictureBox2.BackColor = Color.Red
                PictureBox3.BackColor = Color.Red
                PictureBox4.BackColor = Color.Red
                PictureBox5.BackColor = Color.Red
                PictureBox6.BackColor = Color.Red
                PictureBox7.BackColor = Color.Red
                PictureBox8.BackColor = Color.Red
                PictureBox9.BackColor = Color.Red
                PictureBox10.BackColor = Color.White
            Case 10
                PictureBox1.BackColor = Color.Red
                PictureBox2.BackColor = Color.Red
                PictureBox3.BackColor = Color.Red
                PictureBox4.BackColor = Color.Red
                PictureBox5.BackColor = Color.Red
                PictureBox6.BackColor = Color.Red
                PictureBox7.BackColor = Color.Red
                PictureBox8.BackColor = Color.Red
                PictureBox9.BackColor = Color.Red
                PictureBox10.BackColor = Color.Red
        End Select


        If connesso = True And server = True Then
            invia_dati("AD1 = " + Label18.Text)
        End If


    End Sub

    Private Sub VScrollBar2_ValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles VScrollBar2.ValueChanged

        Label19.Text = VScrollBar2.Value

        Dim val As Long

        val = (VScrollBar2.Value) / 25

        Select Case val
            Case 0
                PictureBox20.BackColor = Color.White
                PictureBox19.BackColor = Color.White
                PictureBox17.BackColor = Color.White
                PictureBox18.BackColor = Color.White
                PictureBox15.BackColor = Color.White
                PictureBox16.BackColor = Color.White
                PictureBox13.BackColor = Color.White
                PictureBox14.BackColor = Color.White
                PictureBox11.BackColor = Color.White
                PictureBox12.BackColor = Color.White
            Case 1
                PictureBox20.BackColor = Color.Red
                PictureBox19.BackColor = Color.White
                PictureBox17.BackColor = Color.White
                PictureBox18.BackColor = Color.White
                PictureBox15.BackColor = Color.White
                PictureBox16.BackColor = Color.White
                PictureBox13.BackColor = Color.White
                PictureBox14.BackColor = Color.White
                PictureBox11.BackColor = Color.White
                PictureBox12.BackColor = Color.White
            Case 2
                PictureBox20.BackColor = Color.Red
                PictureBox19.BackColor = Color.Red
                PictureBox17.BackColor = Color.White
                PictureBox18.BackColor = Color.White
                PictureBox15.BackColor = Color.White
                PictureBox16.BackColor = Color.White
                PictureBox13.BackColor = Color.White
                PictureBox14.BackColor = Color.White
                PictureBox11.BackColor = Color.White
                PictureBox12.BackColor = Color.White
            Case 3
                PictureBox20.BackColor = Color.Red
                PictureBox19.BackColor = Color.Red
                PictureBox17.BackColor = Color.Red
                PictureBox18.BackColor = Color.White
                PictureBox15.BackColor = Color.White
                PictureBox16.BackColor = Color.White
                PictureBox13.BackColor = Color.White
                PictureBox14.BackColor = Color.White
                PictureBox11.BackColor = Color.White
                PictureBox12.BackColor = Color.White
            Case 4
                PictureBox20.BackColor = Color.Red
                PictureBox19.BackColor = Color.Red
                PictureBox17.BackColor = Color.Red
                PictureBox18.BackColor = Color.Red
                PictureBox15.BackColor = Color.White
                PictureBox16.BackColor = Color.White
                PictureBox13.BackColor = Color.White
                PictureBox14.BackColor = Color.White
                PictureBox11.BackColor = Color.White
                PictureBox12.BackColor = Color.White
            Case 5
                PictureBox20.BackColor = Color.Red
                PictureBox19.BackColor = Color.Red
                PictureBox17.BackColor = Color.Red
                PictureBox18.BackColor = Color.Red
                PictureBox15.BackColor = Color.Red
                PictureBox16.BackColor = Color.White
                PictureBox13.BackColor = Color.White
                PictureBox14.BackColor = Color.White
                PictureBox11.BackColor = Color.White
                PictureBox12.BackColor = Color.White
            Case 6
                PictureBox20.BackColor = Color.Red
                PictureBox19.BackColor = Color.Red
                PictureBox17.BackColor = Color.Red
                PictureBox18.BackColor = Color.Red
                PictureBox15.BackColor = Color.Red
                PictureBox16.BackColor = Color.Red
                PictureBox13.BackColor = Color.White
                PictureBox14.BackColor = Color.White
                PictureBox11.BackColor = Color.White
                PictureBox12.BackColor = Color.White
            Case 7
                PictureBox20.BackColor = Color.Red
                PictureBox19.BackColor = Color.Red
                PictureBox17.BackColor = Color.Red
                PictureBox18.BackColor = Color.Red
                PictureBox15.BackColor = Color.Red
                PictureBox16.BackColor = Color.Red
                PictureBox13.BackColor = Color.Red
                PictureBox14.BackColor = Color.White
                PictureBox11.BackColor = Color.White
                PictureBox12.BackColor = Color.White
            Case 8
                PictureBox20.BackColor = Color.Red
                PictureBox19.BackColor = Color.Red
                PictureBox17.BackColor = Color.Red
                PictureBox18.BackColor = Color.Red
                PictureBox15.BackColor = Color.Red
                PictureBox16.BackColor = Color.Red
                PictureBox13.BackColor = Color.Red
                PictureBox14.BackColor = Color.Red
                PictureBox11.BackColor = Color.White
                PictureBox12.BackColor = Color.White
            Case 9
                PictureBox20.BackColor = Color.Red
                PictureBox19.BackColor = Color.Red
                PictureBox17.BackColor = Color.Red
                PictureBox18.BackColor = Color.Red
                PictureBox15.BackColor = Color.Red
                PictureBox16.BackColor = Color.Red
                PictureBox13.BackColor = Color.Red
                PictureBox14.BackColor = Color.Red
                PictureBox11.BackColor = Color.Red
                PictureBox12.BackColor = Color.White
            Case 10
                PictureBox20.BackColor = Color.Red
                PictureBox19.BackColor = Color.Red
                PictureBox17.BackColor = Color.Red
                PictureBox18.BackColor = Color.Red
                PictureBox15.BackColor = Color.Red
                PictureBox16.BackColor = Color.Red
                PictureBox13.BackColor = Color.Red
                PictureBox14.BackColor = Color.Red
                PictureBox11.BackColor = Color.Red
                PictureBox12.BackColor = Color.Red
        End Select


        If connesso = True And server = True Then
            invia_dati("AD2 = " + Label19.Text)
        End If


    End Sub

    ' la seguente serie di procedure definisce il tempo di antirimbalzo del contatore in base ai radiobutton inseriti dall'utente.
    Private Sub RadioButton6_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles RadioButton6.Click
        SetCounterDebounceTime(1, 0)

        ' X rete:
        If connesso = True And ricevuto = False Then
            invia_dati("Debounce(1) = 1")
        ElseIf ricevuto = True Then
            RadioButton6.Checked = True
        End If

        ricevuto = False

    End Sub

    Private Sub RadioButton7_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RadioButton7.Click
        SetCounterDebounceTime(1, 2)

        ' X rete:
        If connesso = True And ricevuto = False Then
            invia_dati("Debounce(1) = 2")
        ElseIf ricevuto = True Then
            RadioButton7.Checked = True
        End If

        ricevuto = False

    End Sub

    Private Sub RadioButton8_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles RadioButton8.Click
        SetCounterDebounceTime(1, 10)

        ' X rete:
        If connesso = True And ricevuto = False Then
            invia_dati("Debounce(1) = 3")
        ElseIf ricevuto = True Then
            RadioButton8.Checked = True
        End If

        ricevuto = False

    End Sub

    Private Sub RadioButton9_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles RadioButton9.Click
        SetCounterDebounceTime(1, 1000)

        ' X rete:
        If connesso = True And ricevuto = False Then
            invia_dati("Debounce(1) = 4")
        ElseIf ricevuto = True Then
            RadioButton9.Checked = True
        End If

        ricevuto = False

    End Sub

    Private Sub RadioButton13_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles RadioButton13.Click
        SetCounterDebounceTime(2, 0)

        ' X rete:
        If connesso = True And ricevuto = False Then
            invia_dati("Debounce(2) = 1")
        ElseIf ricevuto = True Then
            RadioButton13.Checked = True
        End If

        ricevuto = False

    End Sub

    Private Sub RadioButton12_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles RadioButton12.Click
        SetCounterDebounceTime(2, 2)

        ' X rete:
        If connesso = True And ricevuto = False Then
            invia_dati("Debounce(2) = 2")
        ElseIf ricevuto = True Then
            RadioButton12.Checked = True
        End If

        ricevuto = False

    End Sub

    Private Sub RadioButton11_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles RadioButton11.Click
        SetCounterDebounceTime(2, 10)

        ' X rete:
        If connesso = True And ricevuto = False Then
            invia_dati("Debounce(2) = 3")
        ElseIf ricevuto = True Then
            RadioButton11.Checked = True
        End If

        ricevuto = False

    End Sub

    Private Sub RadioButton10_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles RadioButton10.Click
        SetCounterDebounceTime(2, 1000)

        ' X rete:
        If connesso = True And ricevuto = False Then
            invia_dati("Debounce(2) = 4")
        ElseIf ricevuto = True Then
            RadioButton10.Checked = True
        End If

        ricevuto = False

    End Sub

    Private Sub Button7_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button7.Click
        ResetCounter(1) ' viene eseguita la procedura che azzera il contatore 1

        ' X rete:
        If connesso = True And ricevuto = False Then
            invia_dati("Reset(1)")
        End If

        ricevuto = False

    End Sub

    Private Sub Button8_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button8.Click
        ResetCounter(2) ' viene eseguita la procedura che azzera il contatore 2

        ' X rete:
        If connesso = True And ricevuto = False Then
            invia_dati("Reset(2)")
        End If

        ricevuto = False

    End Sub

    Private Sub Panel1_MouseClick(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Panel1.MouseClick
        Dim rete As New Rete
        rete.Show()
    End Sub

    Private Sub tmrGetData_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tmrGetData.Tick
        Static Avvio As Boolean = False ' indica se la procedura viene eseguita per la prima volta o no. No = "True" Si = "False".

        If Avvio = False Then
            If server = True Then
                Panel5.Visible = True
                ' Se eseguiamo il timer per la prima volta
                ' dobbiamo impostare sul client gli "Ad" se siamo server.
                VScrollBar1_ValueChanged(Me, e)
                VScrollBar2_ValueChanged(Me, e)

                'vengono impostati anche i couter sul client.
                textbox1_textchanged(Me, e)
                textbox2_textchanged(Me, e)
            Else
                cancella_oggetti_iniziali()
                carica_componenti_form()
                Timer1.Stop() ' Viene fermato controllo connessione board.
                Button6.Enabled = False
                Panel5.Visible = True
                Panel2.Visible = False
            End If
        End If

        ' Parte ricezione dati Server e Client :

        If Client_str1.Connected = True Then
            'Controlla se ci sono dati da leggere, che possono essere letti (dati disp. >0)
            If Client_str1.Available > 0 And NetStr.CanRead = True Then
                'Definisce un array di byte.

                'Dim dati_Bytes(Client_str1.ReceiveBufferSize) As Byte
                Dim dati_Bytes(Client_str1.Available - 1) As Byte

                'Legge Client.ReceiveBufferSize bytes a partire dal primo 
                'dallo stream e li deposita in Bytes 
                'se ci sono bytes nulli, non verranno contati 
                'di default, Client.ReceiveBufferSize = 8129 

                'prende i dati e li mette nell'array di byte.
                NetStr.Read(dati_Bytes, 0, Client_str1.Available)

                'Trasforma i bytes ricevuti in stringa 
                Dim S1 As String = System.Text.ASCIIEncoding.ASCII.GetString(dati_Bytes)

                ricevuto = True

                esamina_dati_ricevuti(S1) 'I dati vengono esaminati.
                invia_conferma()
            End If
        End If

        ' Parte ricezione dati pulsanti :
        If Client_str2.Connected = True Then
            If Client_str2.Available > 0 And Puls_str.CanRead = True Then

                'Definisce un array di byte.
                Dim puls_Bytes(Client_str2.Available - 1) As Byte

                'Legge Client.ReceiveBufferSize bytes a partire dal primo 
                'dallo stream e li deposita in Bytes 
                'se ci sono bytes nulli, non verranno contati 
                'di default, Client.ReceiveBufferSize = 8129 

                Puls_str.Read(puls_Bytes, 0, Client_str2.Available)

                'Trasforma i bytes ricevuti in stringa 

                Dim S2 As String = System.Text.ASCIIEncoding.ASCII.GetString(puls_Bytes)

                ricevuto = True
                esamina_dati_ricevuti(S2)

                invia_conferma()
            End If
        End If
        Avvio = True
    End Sub

    Private Sub invia_dati(ByVal par As String)
        Static Avvio As Boolean = False ' indica se la procedura viene eseguita per la prima volta o no. No = "True" Si = "False".
        Dim inviato As Boolean = False ' la variabile indica se il messaggio è stato inserito nello stream.

        'Se il client è connesso e si può scrivere sullo stream.
        If Client_str1.Connected = True And NetStr.CanWrite = True Then

            Do While inviato <> True

                If ricevi_conferma() = True Or Avvio = False Then
                    'Converte il messaggio in bytes 
                    Dim Bytes() As Byte = _
                    System.Text.ASCIIEncoding.ASCII.GetBytes(par)

                    'E li scrive sullo stream 
                    NetStr.Write(Bytes, 0, Bytes.Length)
                    inviato = True
                End If
            Loop
        End If
        Avvio = True
    End Sub

    Private Sub invia_dati_puls(ByVal par As String)
        Static Avvio As Boolean = False ' indica se la procedura viene eseguita per la prima volta o no. No = "True" Si = "False".
        Dim inviato As Boolean = False ' la variabile indica se il messaggio è stato inserito nello stream.
        'Dim Contatore As Integer = 0

        'Se il client è connesso e si può scrivere sullo stream.
        If Client_str2.Connected = True And Puls_str.CanWrite = True Then

            Do While inviato <> True
                If ricevi_conferma() = True Or Avvio = False Then
                    'Converte il messaggio in bytes 
                    Dim Bytes() As Byte = _
                    System.Text.ASCIIEncoding.ASCII.GetBytes(par)

                    'E li scrive sullo stream 
                    Puls_str.Write(Bytes, 0, Bytes.Length)
                    inviato = True
                End If
            Loop
        End If
        Avvio = True
    End Sub

    Private Sub esamina_dati_ricevuti(ByVal s As String)
        ' i dati ricevuti vengono analizzati per richiamare le procedure adeguate :
        Dim e As New System.EventArgs

        If Mid(s, 1, 9) Like "Pulsante(" Then
            stream2 = s
            TextBox4.Text = s
        Else
            stream1 = s
            TextBox3.Text = s
        End If

        ' vengono confrontate le stringhe ricevute 

        If stream1 Like "SetAllDigital()" Then
            Button1_Click(Me, e)
        End If

        If stream1 Like "ClearAllDigital()" Then
            Button3_Click(Me, e)
        End If

        If stream1 Like "SetAllAnalog()" Then
            Button5_Click(Me, e)
        End If

        If stream1 Like "ClearAllAnalog()" Then
            Button4_Click(Me, e)
        End If


        If Mid(stream1, 1, 10) Like "OutputTest" Then
            Select Case Mid(stream1, 14, 1)
                Case 0
                    CheckBox11.Checked = False
                Case 1
                    CheckBox11.Checked = True
            End Select
        End If


        If Mid(stream2, 1, 9) Like "Pulsante(" Then
            Select Case Mid(stream2, 10, 1)
                Case 1
                    If Mid(stream2, 15, 1) = "1" Then
                        CheckBox16.Checked = True
                    Else
                        CheckBox16.Checked = False
                    End If

                Case 2
                    If Mid(stream2, 15, 1) = "1" Then
                        CheckBox15.Checked = True
                    Else
                        CheckBox15.Checked = False
                    End If

                Case 3
                    If Mid(stream2, 15, 1) = "1" Then
                        CheckBox13.Checked = True
                    Else
                        CheckBox13.Checked = False
                    End If

                Case 4
                    If Mid(stream2, 15, 1) = "1" Then
                        CheckBox14.Checked = True
                    Else
                        CheckBox14.Checked = False
                    End If

                Case 5
                    If Mid(stream2, 15, 1) = "1" Then
                        CheckBox12.Checked = True
                    Else
                        CheckBox12.Checked = False
                    End If

            End Select
        End If

        If Mid(stream1, 1, 4) Like "Led(" Then
            Select Case Mid(stream1, 5, 1)
                Case 1
                    CheckBox3_Click(Me, e)
                Case 2
                    CheckBox4_Click(Me, e)
                Case 3
                    CheckBox5_Click(Me, e)
                Case 4
                    CheckBox6_Click(Me, e)
                Case 5
                    CheckBox7_Click(Me, e)
                Case 6
                    CheckBox8_Click(Me, e)
                Case 7
                    CheckBox9_Click(Me, e)
                Case 8
                    CheckBox10_Click(Me, e)
            End Select

        End If

        If Mid(stream1, 1, 2) Like "AD" Then
            Select Case Mid(stream1, 3, 1)
                Case 1
                    VScrollBar1.Value = Val(Mid(stream1, 7, Len(stream1) - 6))
                Case 2
                    VScrollBar2.Value = Val(Mid(stream1, 7, Len(stream1) - 6))
            End Select
        End If

        If Mid(stream1, 1, 2) Like "DA" Then
            Select Case Mid(stream1, 3, 1)
                Case 1
                    HScrollBar2.Value = Val(Mid(stream1, 7, Len(stream1) - 6))
                Case 2
                    HScrollBar1.Value = Val(Mid(stream1, 7, Len(stream1) - 6))
            End Select
        End If

        If Mid(stream1, 1, 7) Like "Counter" Then
            Select Case Mid(stream1, 8, 1)
                Case 1
                    TextBox1.Text = Mid(stream1, 11, Len(stream1) - 10)
                Case 2
                    TextBox2.Text = Mid(stream1, 11, Len(stream1) - 10)
            End Select
        End If

        If Mid(stream1, 1, 6) Like "Reset(" Then
            Select Case Mid(stream1, 7, 1)
                Case 1
                    Button7_Click(Me, e)
                Case 2
                    Button8_Click(Me, e)
            End Select
        End If

        If Mid(stream1, 1, 9) Like "Debounce(" Then
            Select Case Mid(stream1, 10, 1)

                Case 1
                    Select Case Mid(stream1, 15, 1)
                        Case 1
                            RadioButton6_Click(Me, e)
                        Case 2
                            RadioButton7_Click(Me, e)
                        Case 3
                            RadioButton8_Click(Me, e)
                        Case 4
                            RadioButton9_Click(Me, e)
                    End Select

                Case 2
                    Select Case Mid(stream1, 15, 1)
                        Case 1
                            RadioButton13_Click(Me, e)
                        Case 2
                            RadioButton12_Click(Me, e)
                        Case 3
                            RadioButton11_Click(Me, e)
                        Case 4
                            RadioButton10_Click(Me, e)
                    End Select
            End Select
        End If
    End Sub

    Private Sub invia_conferma()
        'la procedura invia un messaggio di conferma sul secondo stream
        'ed indica che il messaggio inviato è stato ricevuto e prelevato.
        'Converte il messaggio in bytes 
        Dim inviato As Boolean = False

        Do While inviato <> True
            If Client_str3.Connected Then
                'Se si può scrivere sullo stream 
                If conf_str.CanWrite Then

                    Dim conferma_byte() As Byte = _
                    System.Text.ASCIIEncoding.ASCII.GetBytes("R")

                    'E li scrive sullo stream 
                    conf_str.Write(conferma_byte, 0, conferma_byte.Length)
                    inviato = True
                End If
            End If
        Loop

    End Sub

    Function ricevi_conferma() As Boolean

        If Client_str3.Connected Then
            'Se si può scrivere sullo stream 
            ' If conf_str.CanWrite Then

            If Client_str3.Available > 0 And conf_str.CanRead Then

                Dim conferma_byte(Client_str3.Available - 1) As Byte

                conf_str.Read(conferma_byte, 0, Client_str3.Available)

                'Trasforma i bytes ricevuti in stringa 
                Dim appoggio As String = System.Text.ASCIIEncoding.ASCII.GetString(conferma_byte)

                If appoggio = "R" Then
                    Return True
                Else
                    Return False
                End If

            End If
        End If

    End Function

End Class