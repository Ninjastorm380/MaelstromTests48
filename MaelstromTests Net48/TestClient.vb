Imports System.Threading
Imports Maelstrom

Friend Class TestClient : Inherits ClientBase
    Private Shared Payload as Byte() =
                {1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1, 
                 1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1}
    
    Private ReadOnly WaitLock As Object = New Object()
    Dim Speed As Double = 1
    Friend Sub SetSpeed(Rate as Double)
        Speed = Rate
    End Sub

    Protected Overrides Sub OnConnectionErrored(Byval Socket as Socket, SError As SocketError)
        Console.WriteLine("DEBUG - CLIENT: client connection errored: " + SError.ToString())
    End Sub

    Protected Overrides Sub OnConnectionForged(Socket As Socket)
        Console.WriteLine("DEBUG - CLIENT: connected to server")
        Console.WriteLine("DEBUG - CLIENT: creating async instances....") 
        SyncLock WaitLock
            For x = 0 to 7
                CreateAsyncInstance(Socket,x)
                Console.WriteLine("DEBUG - CLIENT: async instance " + x.ToString() + " created.") 
            Next
        End SyncLock
        Console.WriteLine("DEBUG - CLIENT: all async instances created.") 
    End Sub

    Protected Overrides Sub OnConnectionReleased(Socket As Socket)
        Console.WriteLine("DEBUG - CLIENT: disconnected from server.")  
    End Sub
    
    Private Sub CreateAsyncInstance(Socket as Socket, SubSocket as Int32)
        Dim AsyncThread as new Thread(
            Sub()
                Dim Governor as new Governor(Speed)
                Dim Buffer as Byte() = Nothing
                Dim Compared as Boolean
                Socket.CreateSubSocket(SubSocket)
                Socket.ConfigureSubSocket(SubSocket) = SubSocketConfigFlag.Encrypted + SubSocketConfigFlag.Compressed

                
                Console.WriteLine("DEBUG - CLIENT - ASYNC INSTANCE " + SubSocket.ToString() + ": awaiting unlock...") 
                SyncLock WaitLock : End SyncLock
                Console.WriteLine("DEBUG - CLIENT - ASYNC INSTANCE " + SubSocket.ToString() + ": unlocked!") 
                Thread.Sleep(1000)
                Console.WriteLine("DEBUG - CLIENT - ASYNC INSTANCE " + SubSocket.ToString() + ": writing...") 
                Socket.Write(SubSocket, Payload)
                Console.WriteLine("DEBUG - CLIENT - ASYNC INSTANCE " + SubSocket.ToString() + ": written!") 
                
                Do While Socket.Connected = True
                    If Socket.SubSocketHasData(SubSocket) = True Then
                        Socket.Read(SubSocket, Buffer)
                        Compared = BinaryCompare(Buffer, Payload, 0, Buffer.Length)

                        If Compared = False Then Socket.Close()
                        Socket.Write(SubSocket, Payload)
                        'Console.WriteLine("DEBUG - CLIENT - ASYNC INSTANCE " + SubSocket.ToString() + ": integerity is " +Compared.ToString().ToLower() + ", execution time: " + Governor.Delta.ToString())
                    End If
                    If Governor.Delta > 1.01 Then
                        Console.WriteLine("DEBUG - CLIENT - ASYNC INSTANCE " + SubSocket.ToString() + ": execution time: " + Governor.Delta.ToString() + " - overloaded!")
                    Else
                        Console.WriteLine("DEBUG - CLIENT - ASYNC INSTANCE " + SubSocket.ToString() + ": execution time: " + Governor.Delta.ToString() + " - ok!")
                    End If
                    Governor.Limit()
                Loop
                Socket.RemoveSubSocket(SubSocket)
            End Sub)
        AsyncThread.Start()
    End Sub
End Class