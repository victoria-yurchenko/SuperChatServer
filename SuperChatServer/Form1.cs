using ChatDataLibrary;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace SuperChatServer
{
    public partial class Form1 : Form
    {
        TcpListener server = null;
        Semaphore semaphore = null;
        static List<string> users = new List<string>();
        static Dictionary<string, List<ChatData>> messages = new Dictionary<string, List<ChatData>>();
        BinaryFormatter formatter = null;
        public Form1()
        {
            InitializeComponent();
            semaphore = new Semaphore(10, 10);
            formatter = new BinaryFormatter();
        }

        private void btListen_Click(object sender, EventArgs e)
        {
            string address = tbAddress.Text.Trim();
            if (string.IsNullOrEmpty(address))
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var item in host.AddressList)
                {
                    if (item.AddressFamily == AddressFamily.InterNetwork)
                    {
                        address = item.ToString();
                        tbAddress.Text = address;
                        break;
                    }
                }
            }
            server = new TcpListener(IPAddress.Parse(address),
                Convert.ToInt32(tbPort.Text.Trim()));
            server.Start();
            btListen.BackColor = Color.GreenYellow;
            btListen.Text = "Listening...";

            Thread thread = new Thread(WaitForClientConnect);
            thread.IsBackground = true;
            thread.Start();
        }

        private void WaitForClientConnect()
        {
            while (true)
            {
                semaphore.WaitOne();
                Thread thread = new Thread(Dialog);
                thread.IsBackground = true;
                thread.Start();
            }
        }

        private void Dialog(object obj)
        {
            TcpClient client = server.AcceptTcpClient();            //app blocks here
            NetworkStream ns = client.GetStream();
            while (true)
            {
                try
                {
                    StreamReader sr = new StreamReader(ns, Encoding.UTF8);
                    ChatData chatData = (ChatData)formatter.Deserialize(sr.BaseStream);
                    switch (chatData.Command)
                    {
                        case "Register":
                            break;
                        case "Login":
                            LoginCommand(chatData);
                            break;
                        case "Text":
                            TextCommand(chatData);
                            break;
                        case "Data":
                            break;
                        case "Sync":
                            SyncCommand(client);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

        }

        private void SyncCommand(TcpClient client)
        {
            try
            {
                NetworkStream ns = client.GetStream();
                string chatHistory = tbMessages.Text.Trim();
                ns.Write(Encoding.UTF8.GetBytes(chatHistory), 0, chatHistory.Length);
                ns.Flush();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void LoginCommand(ChatData chatData)
        {
            string name = users.Find(n => chatData.FromName == n);
            if (string.IsNullOrEmpty(name))
            {
                users.Add(chatData.FromName);
                messages.Add(chatData.FromName, new List<ChatData>());
                lbUsers.Items.Add(chatData.FromName);
            }
            else
            {
                //TO DO
            }
        }

        private void TextCommand(ChatData chatData)
        {
            if (string.IsNullOrEmpty(chatData.ToName))
            {
                string info = "\r\n" + DateTime.Now.ToShortTimeString() + "\t" +
                    chatData.FromName + ">>" + chatData.Text;
                                                 
                this.Invoke((MethodInvoker)delegate ()
                {
                    tbMessages.Text += info;
                });

                foreach (KeyValuePair<string, List<ChatData>> item in messages)
                {
                    item.Value.Add(chatData);
                }
            }
            else
            {
                //process private message - TO DO
            }
        }

        private void DownloadData(byte[] data, string fileName)
        {
            using (var binaryWriter = new BinaryWriter(File.OpenWrite(fileName)))
            {
                binaryWriter.Write(data);
            }
        }
    }
}
