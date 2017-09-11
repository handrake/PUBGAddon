using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.IpV4;
using System.Net;
using System.Net.Sockets;
using System.Configuration;

namespace PUBGAddon
{
    public partial class Form1 : Form
    {
        private PacketDevice selectedDevice;
        private IPAddress localIP;
        private IList<Tuple<String, String>> japanServerList;
        private IList<Tuple<String, String>> koreaServerList;
        private BackgroundWorker packetCaptureWorker;

        public Form1()
        {
            InitializeComponent();
            packetCaptureWorker = new BackgroundWorker();
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

            String[] tmpList1 = ConfigurationManager.AppSettings["KoreaServerList"].Split('|');
            String[] tmpList2 = ConfigurationManager.AppSettings["JapanServerList"].Split('|');

            koreaServerList = new List<Tuple<String, String>>();
            japanServerList = new List<Tuple<String, String>>();

            for (int i = 0; i < tmpList1.Count(); i = i + 2)
            {
                koreaServerList.Add(Tuple.Create(tmpList1[i], tmpList1[i + 1]));
            }

            for (int i = 0; i < tmpList2.Count(); i = i + 2)
            {
                japanServerList.Add(Tuple.Create(tmpList2[i], tmpList2[i + 1]));
            }

            this.ActiveControl = button1;
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
            Dictionary<IpV4Address, int> IPDict = new Dictionary<IpV4Address, int>();

            using (PacketCommunicator communicator = selectedDevice.Open(1024, PacketDeviceOpenAttributes.Promiscuous, 200))
            {
                Packet packet;

                using (BerkeleyPacketFilter filter = communicator.CreateFilter("ip and udp"))
                {
                    communicator.SetFilter(filter);
                }
                for (int i = 0; i < 10; i++)
                {
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
            textBox1.Text = "알 수 없음";

            if (IPDict.Count() == 0)
            {
                textBox2.Text = "";
                return;

            }

            String mostCommonIP = IPDict.Aggregate((a, b) => a.Value > b.Value ? a : b).Key.ToString();

            textBox2.Text = mostCommonIP;

            if (koreaServerList.Any(x => IsIPInRange(mostCommonIP.ToString(), x.Item1, x.Item2)))
            {
                textBox1.Text = "한국";
                return;
            }

            if (japanServerList.Any(x => IsIPInRange(mostCommonIP.ToString(), x.Item1, x.Item2)))
            {
                textBox1.Text = "일본";
                return;
            }
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            button1.Enabled = true;
            label3.Text = "";
            this.ActiveControl = this.button1;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            textBox1.Text = "";
            textBox2.Text = "";
            label3.Text = "검색중...";
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
    }
}
