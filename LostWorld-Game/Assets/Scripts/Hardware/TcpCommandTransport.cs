using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
namespace LostWorlds.Person2.Hardware
{
    public interface IProcessedMessageTransport : IDisposable
    {
        bool Connected {get;}
        bool Listening {get;}
        string Error {get;}
        int Dropped {get;}
        bool TryRead(out string message);
    }
    /// <summary>Bounded newline TCP receiver. Worker never touches Unity APIs or gameplay.</summary>
    public sealed class TcpCommandTransport : IProcessedMessageTransport
    {
        struct Packet { public string text;public long time; }
        const int Capacity=64;
        readonly ConcurrentQueue<Packet> pending=new ConcurrentQueue<Packet>();
        readonly TcpListener listener;
        Thread worker;
        volatile TcpClient client;
        volatile bool running,connected,listening;
        volatile string error="";
        int queued,dropped;
        long acceptAfter;
        public void DiscardPending()=>Interlocked.Exchange(ref acceptAfter,Stopwatch.GetTimestamp());
        public bool Connected=>connected;
        public bool Listening=>listening;
        public string Error=>error;
        public int Dropped=>Volatile.Read(ref dropped);
        public TcpCommandTransport(string address,int port)
        {
            if(!IPAddress.TryParse(address,out var ip)||port<1||port>65535)throw new ArgumentException("Use a numeric bind address and port 1-65535.");
            listener=new TcpListener(ip,port);
            try{listener.Start(1);listening=true;running=true;worker=new Thread(Run){IsBackground=true,Name="LostWorlds processed commands"};worker.Start();}
            catch{listener.Stop();throw;}
        }
        void Enqueue(string text,long frameStarted)
        {
            if(Interlocked.Increment(ref queued)>Capacity){Interlocked.Decrement(ref queued);Interlocked.Increment(ref dropped);return;}
            pending.Enqueue(new Packet{text=text,time=frameStarted});
        }
        public bool TryRead(out string message)
        {
            message=null;
            while(pending.TryDequeue(out var packet))
            {
                Interlocked.Decrement(ref queued);
                // Do not replay stale commands after a paused/stalled Unity frame.
                if(packet.time<=Interlocked.Read(ref acceptAfter)||(Stopwatch.GetTimestamp()-packet.time)/(double)Stopwatch.Frequency>1){Interlocked.Increment(ref dropped);continue;}
                message=packet.text;return true;
            }
            return false;
        }
        void Run()
        {
            try
            {
                while(running)
                {
                    if(!listener.Pending()){Thread.Sleep(10);continue;}
                    client=listener.AcceptTcpClient();if(!running){client.Close();break;}
                    client.NoDelay=true;connected=true;
                    try{ReadClient(client);}catch(IOException){}catch(SocketException){}catch(ObjectDisposedException){}
                    finally{connected=false;client.Close();client=null;}
                }
            }
            catch(Exception ex){if(running)error="TCP receiver stopped: "+ex.GetType().Name;}
            finally{connected=false;listening=false;listener.Stop();}
        }
        void ReadClient(TcpClient peer)
        {
            var stream=peer.GetStream();stream.ReadTimeout=100;var buffer=new byte[256];var line=new StringBuilder();bool invalid=false;long frameStarted=0;
            while(running)
            {
                int count;
                try{count=stream.Read(buffer,0,buffer.Length);}
                catch(IOException ex)when(ex.InnerException is SocketException socket&&socket.SocketErrorCode==SocketError.TimedOut){continue;}
                if(count==0){if(line.Length>0||invalid)Enqueue("<incomplete frame>",frameStarted);break;}
                for(int i=0;i<count;i++)
                {
                    byte value=buffer[i];
                    if(frameStarted==0)frameStarted=Stopwatch.GetTimestamp();
                    if(value==10){Enqueue(invalid?"<invalid or oversized frame>":line.ToString(),frameStarted);line.Clear();invalid=false;frameStarted=0;continue;}
                    if(invalid)continue;
                    if(value>127||line.Length>=ProcessedCommandParser.MaximumBytes){line.Clear();invalid=true;continue;}
                    line.Append((char)value);
                }
            }
        }
        public void Dispose()
        {
            running=false;connected=false;listening=false;listener.Stop();client?.Close();
            if(worker!=null&&worker!=Thread.CurrentThread)worker.Join(500);
            while(pending.TryDequeue(out _)){}queued=0;
        }
    }
}
