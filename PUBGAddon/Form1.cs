using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.IpV4;
using System.Net;
using System.Net.Sockets;
using System.Configuration;
using System.Diagnostics;

namespace PUBGAddon
{
    public partial class Form1 : Form
    {
        private PacketDevice selectedDevice;
        private IPAddress localIP;
        private IList<Tuple<String, List<Tuple<String, String>>>> serverList;
        private BackgroundWorker packetCaptureWorker;
        private Dictionary<IpV4Address, int> IPDict;
        private bool serverFound;
        private string lastIP;

        public Form1()
        {
            InitializeComponent();
            packetCaptureWorker = new BackgroundWorker();
            packetCaptureWorker.WorkerSupportsCancellation = true;

            packetCaptureWorker.DoWork += new DoWorkEventHandler(worker_DoWork);
            packetCaptureWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            IList<LivePacketDevice> allDevices = LivePacketDevice.AllLocalMachine;
            
            if (allDevices.Count == 0)
            {
                MessageBox.Show("네트워크 장치가 없습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }

            for (int i = 0; i < allDevices.Count(); i++)
            {
                comboBox1.Items.Add(allDevices[i].Description);
            }

            comboBox1.SelectedIndex = 0;

            var tmpList = new List<Tuple<String, String[]>>();
            tmpList.Add(Tuple.Create("한국", ConfigurationManager.AppSettings["KoreaServerList"].Split('|')));
            tmpList.Add(Tuple.Create("일본", ConfigurationManager.AppSettings["JapanServerList"].Split('|')));
            tmpList.Add(Tuple.Create("미국 동부-1", ConfigurationManager.AppSettings["USEast1ServerList"].Split('|')));

            serverList = new List<Tuple<String, List<Tuple<String, String>>>>();

            foreach (var t in tmpList)
            {
                serverList.Add(Tuple.Create(t.Item1, new List<Tuple<String, String>>()));
                for (int j = 0; j < t.Item2.Count(); j = j + 2)
                {
                    serverList.Last().Item2.Add(Tuple.Create(t.Item2[j], t.Item2[j + 1]));
                }
            }

            this.ActiveControl = label1;
            button2.Enabled = false;
            serverFound = false;
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private Boolean IsIPInRange(String ip, String startRange, String endRange)
        {
            int[] ipParts = ip.Split('.').Select(x => Int32.Parse(x)).ToArray();
            int[] startRangeParts = startRange.Split('.').Select(x => Int32.Parse(x)).ToArray();
            int[] endRangeParts = endRange.Split('.').Select(x => Int32.Parse(x)).ToArray();

            for (int i = 0; i < ipParts.Count(); i++)
            {
                if (ipParts[i] < startRangeParts[i] || ipParts[i] > endRangeParts[i])
                {
                    return false;
                }
            }

            return true;
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            IPDict = new Dictionary<IpV4Address, int>();

            using (PacketCommunicator communicator = selectedDevice.Open(1024, PacketDeviceOpenAttributes.Promiscuous, 200))
            {
                Packet packet;

                using (BerkeleyPacketFilter filter = communicator.CreateFilter("ip and udp"))
                {
                    communicator.SetFilter(filter);
                }
                for (int i = 0; i < 10; i++)
                {
                    if (packetCaptureWorker.CancellationPending)
                    {
                        e.Cancel = true;
                        return;
                    }
                    PacketCommunicatorReceiveResult result = communicator.ReceivePacket(out packet);
                    switch (result)
                    {
                        case PacketCommunicatorReceiveResult.Timeout:
                            continue;
                        case PacketCommunicatorReceiveResult.Ok:
                            IpV4Datagram ip = packet.Ethernet.IpV4;
                            if (ip.Source.ToString() == localIP.ToString())
                            {
                                if (IPDict.ContainsKey(ip.Destination))
                                    IPDict[ip.Destination]++;
                                else
                                    IPDict.Add(ip.Destination, 1);
                            }
                            else if (ip.Destination.ToString() == localIP.ToString())
                            {
                                if (IPDict.ContainsKey(ip.Source))
                                    IPDict[ip.Source]++;
                                else
                                    IPDict.Add(ip.Source, 1);
                            }
                            break;
                        default:
                            throw new InvalidOperationException();
                    }
                }
            }
        }

        private async void getLatency()
        {
            WebRequest request = WebRequest.Create((new UriBuilder(textBox2.Text)).ToString());
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                WebResponse response = await request.GetResponseAsync();
            }
            catch
            {
            }
            finally
            {
                sw.Stop();
                textBox3.Text = sw.ElapsedMilliseconds.ToString() + "ms";
            }
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (IPDict.Count() == 0)
            {
                goto ServerNotFound;
            }

            if (serverFound && IPDict.ContainsKey(new IpV4Address(lastIP)))
            {
                goto End;
            }

            String mostCommonIP = IPDict.Aggregate((a, b) => a.Value > b.Value ? a : b).Key.ToString();

            textBox2.Text = mostCommonIP;

            foreach (var t in serverList)
            {
                if (t.Item2.Any(x => IsIPInRange(mostCommonIP.ToString(), x.Item1, x.Item2)))
                {
                    textBox1.Text = t.Item1;
                    lastIP = textBox2.Text;
                    serverFound = true;
                    goto End;
                }
            }
            ServerNotFound:
            textBox1.Text = "알 수 없음";
            textBox2.Text = "";
            textBox3.Text = "";
            serverFound = false;

            End:
            if (serverFound)
            {
                getLatency();
            }

            if (button2.Enabled)
            {
                packetCaptureWorker.RunWorkerAsync();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            button2.Enabled = true;
            packetCaptureWorker.RunWorkerAsync();
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.selectedDevice = LivePacketDevice.AllLocalMachine[comboBox1.SelectedIndex];
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            for (int i = 0; i < host.AddressList.Length; i++)
            {
                if (host.AddressList[i].AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    localIP = host.AddressList[i];
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;
            button1.Enabled = true;
            packetCaptureWorker.CancelAsync();
        }
    }
}
