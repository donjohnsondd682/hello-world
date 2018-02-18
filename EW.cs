using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using TCTS.Interfaces;


namespace EW
{
    public partial class TestHarness : Form
    {
        public delegate void TextBoxDelegate(string TextString);
        static UdpClient client;
        static IPEndPoint receivePoint;
        static string EWrcvIPAddress;
        static string EWxmtIPAddress;
        static string[] arrDec;
        static byte[] ipBytes;

        static int EWxmtPort;
        static int EWrcvPort;
        static RangeInterfaces.TimeOfDay mdToD;
        static RangeInterfaces.ManeuverDataHeader mdHdr;
        static RangeInterfaces.ManeuverData maneuverData;
        static IPEndPoint sender;
        static EndPoint Remote;
        static IPAddress address;
        static Socket server;
        static StreamReader reader;
        static FileStream fs;
        static int displayReceivePort;
        static System.Windows.Forms.Form isParentof;
        static Byte[] ddsIP = new Byte[4];
        static Byte[] displayIP = new Byte[4];
        static bool disposed = false;

        public TestHarness()
        {
            InitializeComponent();
            string[] splitIt;
            // Read in site specific data
            string line;
            fs = new FileStream(@"\DMD\properties\dmd.properties",
            FileMode.Open, FileAccess.Read);
            reader = new StreamReader(fs);
            char delimiter = '=';

            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith("FromEW"))
                {
                    int i;
                    byte[] ipBytes = new byte[4];
                    string[] arrDec = new string[4];
                    string[] splitAgain;
                    delimiter = '=';
                    splitIt = line.Split(delimiter);
                    delimiter = ':';
                    line = splitIt[1];
                    splitAgain = line.Split(delimiter);
                    EWrcvIPAddress = splitAgain[0];
                    EWrcvIPAddress = EWrcvIPAddress.Trim();
                    EWrcvPort = Convert.ToInt32(splitAgain[1]);
                    arrDec = EWrcvIPAddress.Split('.');
                    client = new UdpClient(EWrcvPort);
                    for (i = 0; i < 4; i++)
                    {
                        ipBytes[i] = Convert.ToByte(arrDec[i]);
                    }
                    receivePoint = new IPEndPoint(new IPAddress(ipBytes), EWrcvPort);

                }
                if (line.StartsWith("ToEW"))
                {
                    int i;
                    string[] arrDec = new string[4];
                    byte[] ipBytes = new byte[4]; ;
                    string[] splitAgain;
                    delimiter = '=';
                    splitIt = line.Split(delimiter);
                    delimiter = ':';
                    line = splitIt[1];
                    splitAgain = line.Split(delimiter);
                    EWxmtIPAddress = splitAgain[0];
                    EWxmtIPAddress = EWxmtIPAddress.Trim();
                    EWxmtPort = Convert.ToInt32(splitAgain[1]);
                    arrDec = EWxmtIPAddress.Split('.');
                    for (i = 0; i < 4; i++)
                    {
                        ipBytes[i] = Convert.ToByte(arrDec[i]);
                    }
                }


            }
            reader.Close();
            EWTest dt = new EWTest();
            dt.Parent = this;
            Thread EWTest_In = new Thread(new ThreadStart(dt.ProcesstestInput));
            EWTest_In.Name = "EW Input";
            EWTest_In.Priority = ThreadPriority.Normal;
            EWTest_In.Start();
        }


        /// Luckily the designers for Windows Forms realized the plight 
        /// that may be caused without being able to interact with the UI 
        /// from another thread.  So they add the Invoke method to the Control class.  
        /// Invoke runs the specified delegate on the thread that create the 
        /// control's window (or the parents window if it doesn't have one).
        /// There are a number of good reference materials on Delegates, such 
        /// as the Code Project at http://www.codeproject.com/. A good text reference
        /// book is "Core C# and .NET" (C# 2.0) by Stephen C. Perry.
        /// </summary>
        /// <summary>
        /// TSPIHeader_Del
        /// Updates the Status strip and the RRU Activity button indicators.
        /// </summary>
        /// <param name="RangeTime"></param>
        /// <param name="RRUActivity"></param>
        /// <param name="NumAC"></param>
        public void TextBox_Del(string TextString)
        {
            // Make sure we're on the right thread.
            if (textBox.InvokeRequired == false)
            {
                textBox.Text += TextString;
            }
            else
            { // Show asyncronously.

                try
                {
                    this.Invoke(new TextBoxDelegate(DoText), new object[] { TextString });
                }
                catch
                {
                    Console.WriteLine("Done");
                }
            }
        }
        private void DoText(string TextString)
        {
            textBox.Text += TextString;
        }

        public int Property
        {
            get
            {
                throw new System.NotImplementedException();
            }
            set
            {
            }
        }

        public class EWTest
        {
            private static System.Windows.Forms.Form isParentof;

            byte[] data = new byte[24000];
            public void ProcesstestInput()
            {
                //IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                Byte[] receiveBytes = new Byte[6000];
                while (true)
                {
                    try
                    {

                        receiveBytes = client.Receive(ref receivePoint);
                        ((TestHarness)isParentof).TextBox_Del((receiveBytes.Length).ToString() + "  Bytes rcv'd" + "\r\n");
                    }
                    catch (SocketException exception)
                    {
                        Console.WriteLine("EWTest read error:   " + exception.Message.ToString() + "\r\n");
                    }
                }
            }
            /// <summary>
            /// Name        : Parent
            /// Function    : Get:Returns the Parent form
            ///             : Set:Sets the Parent form
            /// </summary>
            public System.Windows.Forms.Form Parent
            {
                get { return isParentof; }
                set { isParentof = value; }
            }

        }
        /// <summary>
        ///  See  http://csharp.codenewbie.com/articles/c/c-and-advanced-binary-files
        ///  for a discusion of this Deserialization technique
        /// </summary>
        /// <param name="rawdatas"></param>
        /// <param name="anytype"></param>
        /// <returns></returns>
        public static object RawDeserialize(byte[] rawdatas, Type anytype)
        {
            int rawsize = Marshal.SizeOf(anytype);
            if (rawsize > rawdatas.Length)
                return null;
            IntPtr buffer = Marshal.AllocHGlobal(rawsize);
            Marshal.Copy(rawdatas, 0, buffer, rawsize);
            object retobj = Marshal.PtrToStructure(buffer, anytype);
            Marshal.FreeHGlobal(buffer);
            return retobj;
        }

        private void send_Click(object sender, EventArgs e)
        {
                        // Test Only
                        Byte[] sendBytes = Encoding.ASCII.GetBytes("Data received at EWTestHarness and returned to LM");
                        int bytesSent = client.Send(sendBytes, sendBytes.Length, EWxmtIPAddress, EWxmtPort);
        }
    }
}
