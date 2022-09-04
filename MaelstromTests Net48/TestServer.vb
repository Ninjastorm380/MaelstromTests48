Imports System.Threading
Imports Maelstrom

Friend Class TestServer : Inherits ServerBase
    Private Shared Payload as Byte() =
                {1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1, 
                 1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1}
    
    Private ReadOnly WaitLock As Object = New Object()
    Dim Speed As Double = 1
    Friend Sub SetSpeed(Rate as Double)
        Speed = Rate
    End Sub
    Protected Overrides Sub OnConnectionErrored(Socket As Socket, SError As SocketError)
        Console.WriteLine("DEBUG - SERVER: client connection errored: " + SError.ToString())
    End Sub

    Protected Overrides Sub OnConnectionForged(Socket As Socket)
        Console.WriteLine("DEBUG - SERVER: client has connected")
        Console.WriteLine("DEBUG - SERVER: creating async instances....") 
        SyncLock WaitLock
            For x = 0 to 7
                CreateAsyncInstance(Socket,x)
                Console.WriteLine("DEBUG - SERVER: async instance " + x.ToString() + " created.") 
            Next
        End SyncLock
        Console.WriteLine("DEBUG - SERVER: all async instances created.") 
    End Sub
    
    Protected Overrides Sub OnConnectionReleased(Socket As Socket)
        Console.WriteLine("DEBUG - SERVER: client has disconnected.")
    End Sub

    Protected Overrides Sub OnServerOnline()
        Console.WriteLine("DEBUG - SERVER: server is now online")
    End Sub

    Protected Overrides Sub OnServerOffline()
        Console.WriteLine("DEBUG - SERVER: server is now offline")
    End Sub
    
    Private Sub CreateAsyncInstance(Socket as Socket, SubSocket as UInt32)
        Dim AsyncThread as new Thread(
            Sub()
                Dim Governor as new Governor(Speed)
                Dim Buffer as Byte() = Nothing
                Dim Compared as Boolean
                Socket.CreateSubSocket(SubSocket)
                Socket.ConfigureSubSocket(SubSocket) = SubSocketConfigFlag.Encrypted + SubSocketConfigFlag.Compressed
                
                Console.WriteLine("DEBUG - SERVER - ASYNC INSTANCE " + SubSocket.ToString() + ": awaiting unlock...") 
                SyncLock WaitLock : End SyncLock
                Console.WriteLine("DEBUG - SERVER - ASYNC INSTANCE " + SubSocket.ToString() + ": unlocked!") 
                Thread.Sleep(1000)
                Console.WriteLine("DEBUG - SERVER - ASYNC INSTANCE " + SubSocket.ToString() + ": writing...") 
                Socket.Write(SubSocket, Payload)
                Console.WriteLine("DEBUG - SERVER - ASYNC INSTANCE " + SubSocket.ToString() + ": written!") 
                
                Do While Socket.Connected = True And Online = True
                    If Socket.SubSocketHasData(SubSocket) = True Then
                        Socket.Read(SubSocket, Buffer)
                        Compared = BinaryCompare(Buffer, Payload, 0, Buffer.Length)
                        If Compared = False Then Socket.Close()
                        Socket.Write(SubSocket, Payload)
                        'Console.WriteLine("DEBUG - SERVER - ASYNC INSTANCE " + SubSocket.ToString() + ": integerity is " +Compared.ToString().ToLower() + ", execution time: " + Governor.Delta.ToString() + "")

                    End If
                    If Governor.Delta > 1.01 Then
                        Console.WriteLine("DEBUG - SERVER - ASYNC INSTANCE " + SubSocket.ToString() + ": execution time: " + Governor.Delta.ToString() + " - overloaded!")
                    Else
                        Console.WriteLine("DEBUG - SERVER - ASYNC INSTANCE " + SubSocket.ToString() + ": execution time: " + Governor.Delta.ToString() + " - ok!")
                    End If
                    Governor.Limit()
                Loop
                Socket.RemoveSubSocket(SubSocket)
            End Sub)
        AsyncThread.Start()
    End Sub
End Class