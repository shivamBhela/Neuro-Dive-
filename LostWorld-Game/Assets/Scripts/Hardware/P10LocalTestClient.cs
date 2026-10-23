using System;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
namespace LostWorlds.Person2.Hardware
{
    /// <summary>Local test client sends real TCP bytes, never calls the adapter/channel directly.</summary>
    public sealed class P10LocalTestClient : MonoBehaviour
    {
        [SerializeField] ProcessedHardwareAdapter adapter;
        TcpClient client;
        public void Bind(ProcessedHardwareAdapter target)=>adapter=target;
        void OnDisable()=>Disconnect();
        public bool Connect()
        {
            Disconnect();
            try{client=new TcpClient();client.Connect("127.0.0.1",adapter.Port);client.NoDelay=true;return true;}
            catch(Exception ex){Disconnect();Debug.LogWarning("P10 local test: "+ex.Message,this);return false;}
        }
        public void Disconnect(){client?.Close();client=null;}
        public bool SendRaw(string text)
        {
            if(client==null&&!Connect())return false;
            try{var bytes=Encoding.UTF8.GetBytes(text);client.GetStream().Write(bytes,0,bytes.Length);return true;}
            catch(Exception ex){Disconnect();Debug.LogWarning("P10 local test: "+ex.Message,this);return false;}
        }
        void Update()=>PollKeyboard();
        public void PollKeyboard()
        {
            if(!isActiveAndEnabled||Keyboard.current==null)return;var k=Keyboard.current;
            if(k.f4Key.wasPressedThisFrame)Connect();
            if(k.f5Key.wasPressedThisFrame)SendRaw("{\"command\":\"LEFT\"}\n");
            if(k.f6Key.wasPressedThisFrame)SendRaw("{\"command\":\"RIGHT\"}\n");
            if(k.f7Key.wasPressedThisFrame)SendRaw("{\"command\":\"JUMP\"}\n");
            if(k.f8Key.wasPressedThisFrame)SendRaw("{\"command\":\"GRAB\"}\n");
            if(k.f9Key.wasPressedThisFrame)SendRaw("{\"command\":\"ACTIVATE\"}\n");
            if(k.f10Key.wasPressedThisFrame)SendRaw("{\"command\":\"INVALID\"}\n");
            if(k.f11Key.wasPressedThisFrame)Disconnect();
        }
    }
}
