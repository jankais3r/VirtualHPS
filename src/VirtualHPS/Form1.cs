namespace VirtualHPS
{
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Diagnostics;
    using System.Net.WebSockets;
    using System.Runtime.InteropServices;
    using static nng;
    using PeterO.Cbor;

    static class CalibrationPath
    {
        public static string calibrationFile = "calibration.txt";
        public static string calibrationString = "";
        public static bool resolutionOverride = false;
        public static int resolutionOverrideScreenId = 999;
    }

    public partial class Form1 : Form
    {
        protected override void WndProc(ref Message message) // Based on https://stackoverflow.com/questions/5135724/c-hide-controlbox-winforms
        {
            const int WM_NCHITTEST = 0x0084;
            if (message.Msg == WM_NCHITTEST)
            {
                return;
            }
            base.WndProc(ref message);
        }
        class Win32Api
        {
            [DllImport("User32.dll", SetLastError = true)]
            internal static extern IntPtr
            MonitorFromPoint(POINT pt, int dwFlags);

            internal const int MONITORINFOF_PRIMARY = 0x00000001;
            internal const int MONITOR_DEFAULTTONEAREST = 0x00000002;
            internal const int MONITOR_DEFAULTTONULL = 0x00000000;
            internal const int MONITOR_DEFAULTTOPRIMARY = 0x00000001;

            [StructLayout(LayoutKind.Sequential)]
            internal struct POINT
            {
                internal int x;
                internal int y;

                internal POINT(int x, int y)
                {
                    this.x = x;
                    this.y = y;
                }
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct RECT
            {
                internal int left;
                internal int top;
                internal int right;
                internal int bottom;
            }

            [DllImport("Shcore.dll", SetLastError = true)]
            internal static extern int
            GetDpiForMonitor(IntPtr hmonitor, Monitor_DPI_Type dpiType, out uint dpiX, out uint dpiY);

            internal enum Monitor_DPI_Type : int { MDT_Effective_DPI = 0, MDT_Angular_DPI = 1, MDT_Raw_DPI = 2, MDT_Default = MDT_Effective_DPI }
        }
        public (int startingPointX, int startingPointY, int width, int height, string scaling) getScreenProperties(int screenId = 999)
        {
            var screenCount = Screen.AllScreens.Length;
            var i = 0;
            while (i < screenCount)
            {
                var scr = System.Windows.Forms.Screen.AllScreens[i];

                Win32Api.POINT startingPoint = new Win32Api.POINT(scr.Bounds.Left, scr.Bounds.Top);
                IntPtr mhwnd = Win32Api.MonitorFromPoint(startingPoint, Win32Api.MONITOR_DEFAULTTONULL);
                _ = Win32Api.GetDpiForMonitor(mhwnd, Win32Api.Monitor_DPI_Type.MDT_Effective_DPI, out uint dpiX, out _);

                var scaling = (100.0 * (dpiX / 96.0)).ToString() + "%";
                string[] screenProperties = { i.ToString(), scr.Bounds.Width.ToString(), scr.Bounds.Height.ToString(), scaling };

                if (screenId <= screenCount)
                {
                    if (screenId == i)
                        return (scr.Bounds.Left, scr.Bounds.Top, scr.Bounds.Width, scr.Bounds.Height, scaling);
                }
                else
                {
                    dataGridView1.Rows.Add(screenProperties);
                }
                i++;
            }
            return (0, 0, 0, 0, "");
        }

        public void refreshScreens(object sender, EventArgs e)
        {
            var selectedScreen = 999;
            if (dataGridView1.SelectedRows.Count > 0)
            {
                selectedScreen = dataGridView1.SelectedRows[0].Index;
            }
            dataGridView1.Rows.Clear();
            getScreenProperties();
            try
            {
                if (dataGridView1.Rows.Count >= selectedScreen)
                {
                    dataGridView1.Rows[selectedScreen].Selected = true;
                }
            }
            catch { }
        }

        public static void nngServer(int startingPointX, int startingPointY, float quiltAspect)
        {
            // Portrait debug:
            // string jsonString = "{'devices': [{'buttons': [0, 0, 0, 0], 'calibration': {'DPI': {'value': 324.0}, 'center': {'value': 0.901610791683197}, 'configVersion': '3.0', 'flipImageX': {'value': 0.0}, 'flipImageY': {'value': 0.0}, 'flipSubp': {'value': 0.0}, 'fringe': {'value': 0.0}, 'invView': {'value': 1.0}, 'pitch': {'value': 52.58183670043945}, 'screenH': {'value': 2048.0}, 'screenW': {'value': 1536.0}, 'serial': 'LKG-PORT-', 'slope': {'value': -7.2032060623168945}, 'verticalAngle': {'value': 0.0}, 'viewCone': {'value': 40.0}}, 'defaultQuilt': {'quiltAspect': 0.75, 'quiltX': 3840, 'quiltY': 3840, 'tileX': 8, 'tileY': 6}, 'hardwareVersion': 'portrait', 'hwid': 'LKG-P00655', 'index': 0, 'joystickIndex': -1, 'state': 'ok', 'unityIndex': 1, 'windowCoords': [" + startingPointX.ToString() + ", " + startingPointY.ToString() + "]}], 'error': 0, 'version': '1.2.2'}".Replace("'", "\"");
            // 8.9" devkit debug:
            // string jsonString = "{'devices': [{'buttons': [0, 0, 0, 0], 'calibration': {'DPI': {'value': 338.0}, 'center': {'value': 0.374184787273407}, 'configVersion': '1.0', 'flipImageX': {'value': 0.0}, 'flipImageY': {'value': 0.0}, 'flipSubp': {'value': 0.0}, 'invView': {'value': 1.0}, 'pitch': {'value': 47.56401443481445}, 'screenH': {'value': 1600.0}, 'screenW': {'value': 2560.0}, 'serial': 'LKG-2K-04409', 'slope': {'value': -5.480000019073486}, 'verticalAngle': {'value': 0.0}, 'viewCone': {'value': 40.0}}, 'defaultQuilt': {'quiltAspect': 1.6, 'quiltX': 4096, 'quiltY': 4096, 'tileX': 5, 'tileY': 9}, 'hardwareVersion': 'standard', 'hwid': 'LKG03KBuXQxi3', 'index': 0, 'joystickIndex': 0, 'state': 'ok', 'unityIndex': 1, 'windowCoords': [" + startingPointX.ToString() + ", " + startingPointY.ToString() + "]}], 'error': 0, 'version': '1.2.5'}".Replace("'", "\"");
            var jsonString = "{\"devices\":[{\"buttons\":[0,0,0,0],\"calibration\":" + CalibrationPath.calibrationString + ",\"defaultQuilt\":{\"quiltAspect\":" + quiltAspect.ToString() + ",\"quiltX\":4096,\"quiltY\":4096,\"tileX\":5,\"tileY\":9},\"hardwareVersion\":\"standard\",\"hwid\":\"LKGZHVtbXlTTg\",\"index\":0,\"joystickIndex\":0,\"state\":\"ok\",\"unityIndex\":1,\"windowCoords\":[" + startingPointX.ToString() + ", " + startingPointY.ToString() + "]}],\"error\":0,\"version\":\"1.2.5\"}";
            var cborObj = CBORObject.FromJSONString(jsonString, new JSONOptions("keepkeyorder=true;numberconversion=intorfloat"));
            byte[] cborBytes = cborObj.EncodeToBytes();
            string ipcName = "ipc:///tmp/holoplay-driver.ipc";
            _ = new EventWaitHandle(false, EventResetMode.ManualReset);
            nng_socket sock = default;

            nng_assert(nng_rep0_open(ref sock));
            Console.WriteLine("Using the following calibration values: " + cborObj.ToJSONString());

            try
            {
                nng_listener listener = default;
                try
                {
                    nng_assert(nng_listen(sock, ipcName, ref listener, 0));
                    Console.WriteLine("NNG server started at endpoint " + ipcName);
                }
                catch
                {
                    MessageBox.Show("Address already in use.\n\nMake sure that you are not running a second instance of VirtualHPS or an actual HoloPlayService.\n");
                    System.Windows.Forms.Application.Exit();
                }
                while (true)
                {
                    nng_assert(nng_recv(sock, out var buffer));
                    nng_assert(nng_send(sock, cborBytes));
                    buffer.Dispose();
                    Console.WriteLine("NNG request received.");
                }
            }
            finally
            {
                _ = nng_close(sock);
                Console.WriteLine("NNG server stopped.");
            }
        }

        public static async void wsServer() // Based on https://stackoverflow.com/questions/30490140/how-to-work-with-system-net-websockets-without-asp-net
        {
            HttpListener httpListener = new();
            var wsBytes = new ArraySegment<byte>(Encoding.UTF8.GetBytes(CalibrationPath.calibrationString));
            string wsHostname = "127.0.0.1";
            int wsPort = 11222;
            string wsEndpoint = "/";

            httpListener.Prefixes.Add("http://" + wsHostname + ":" + wsPort.ToString() + wsEndpoint);
            try
            {
                httpListener.Start();
            }
            catch
            {
                MessageBox.Show("Address already in use.\n\nMake sure that you are not running a second instance of VirtualHPS or an actual HoloPlayService.\n");
                System.Windows.Forms.Application.Exit();
            }
            
            Console.WriteLine("WS server started at endpoint http://" + wsHostname + ":" + wsPort.ToString() + wsEndpoint);

            while (true)
            {
                await ReceiveConnection();
            }

            async Task ReceiveConnection()
            {
                HttpListenerContext context = await httpListener.GetContextAsync();
                HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);
                WebSocket webSocket = webSocketContext.WebSocket;
                await webSocket.SendAsync(wsBytes, WebSocketMessageType.Text, true, CancellationToken.None);
                Console.WriteLine("WS request received.");
            }
        }

        public Form1()
        {
            InitializeComponent();
            Console.WriteLine("");

            DriveInfo[] allDrives = DriveInfo.GetDrives();
            bool foundOfficial = false;
            foreach (DriveInfo d in allDrives)
            {
                if (d.IsReady == true)
                {
                    string potentialCalibrationFile = d.Name.ToString() + @"LKG_calibration\visual.json";
                    if (d.DriveType.ToString() == "Removable" && File.Exists(potentialCalibrationFile))
                    {
                        CalibrationPath.calibrationFile = potentialCalibrationFile;
                        foundOfficial = true;
                        break;
                    }
                }
            }
            if (foundOfficial == true)
            {
                Console.WriteLine("Found an official calibration file at " + CalibrationPath.calibrationFile);
            }
            else
            {
                Console.WriteLine("Official calibration file not found. Using manual values from calibration.txt.");
            }
        }

        private void Form1_Load(object sender, EventArgs e) { }

        private void button1_Click(object sender, EventArgs e)
        {
            int selectedScreen, startingPointX, startingPointY, screenWidth, screenHeight;
            string resolution, scaling;
            selectedScreen = dataGridView1.SelectedRows[0].Index;
            (startingPointX, startingPointY, screenWidth, screenHeight, scaling) = getScreenProperties(selectedScreen);
            resolution = screenWidth.ToString() + "x" + screenHeight.ToString();

            List<string> validDisplays = new(){
                "1536x2048", // Looking Glass Portrait
                "3840x2160", // Looking Glass 4K Gen 2
                "7680×4320", // Looking Glass 8K Gen 2
                "2560x1600", // Looking Glass 8.9 Gen 1
                "3840x2160", // Looking Glass 15.6 Gen 1
                "7680×4320"  // Looking Glass 8K Gen 1
            // ,"1920x1080"  // debug
            }; // https://docs.lookingglassfactory.com/getting-started/holoplay-service#display-resolutions-for-looking-glass-devices

            if (File.Exists(CalibrationPath.calibrationFile) && new FileInfo(CalibrationPath.calibrationFile).Length > 0)
            {
                CalibrationPath.calibrationString = File.ReadAllText(CalibrationPath.calibrationFile);
                try
                {
                    var cborObj = CBORObject.FromJSONString(CalibrationPath.calibrationString);
                    if (validDisplays.Contains(resolution) || (CalibrationPath.resolutionOverride && CalibrationPath.resolutionOverrideScreenId == selectedScreen))
                    {
                        if (scaling == "100%" || CalibrationPath.resolutionOverride && CalibrationPath.resolutionOverrideScreenId == selectedScreen)
                        {
                            string legacyCalibrationFolder = @"C:\LKG_calibration";
                            string legacyCalibrationFile = legacyCalibrationFolder + @"\visual.json";
                            if (Directory.Exists(legacyCalibrationFolder))
                            {
                                Directory.Delete(legacyCalibrationFolder, true);
                            }
                            Directory.CreateDirectory(legacyCalibrationFolder);
                            using (StreamWriter sw = File.CreateText(legacyCalibrationFile))
                            {
                                sw.Write(CalibrationPath.calibrationString);
                                sw.Close();
                                Console.WriteLine("Wrote the current calibration data to " + legacyCalibrationFile);
                            }
                            Thread nngThread = new(() => nngServer(startingPointX, startingPointY, (float)screenWidth / screenHeight)) { IsBackground = true };
                            nngThread.Start();
                            Thread wsThread = new(() => wsServer()) { IsBackground = true };
                            wsThread.Start();
                            button1.Text = "VirtualHPS started...";
                            button1.Enabled = false;
                            dataGridView1.Enabled = false;
                        }
                        else
                        {
                            MessageBox.Show("The holographic display's scaling has to be set to 100%.");
                            Process.Start("explorer.exe", "ms-settings:display");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Selected screen's resolution does not match any known holographic display model.\n\nConfirm this screen again to bypass the resolution check.");
                        CalibrationPath.resolutionOverride = true;
                        CalibrationPath.resolutionOverrideScreenId = selectedScreen;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    MessageBox.Show("Provided calibration string is not a valid JSON.");
                    Process.Start("notepad.exe", CalibrationPath.calibrationFile);
                }
            }
            else
            {
                MessageBox.Show("Calibration file not found or empty.\n\nCopy your calibration JSON string into calibration.txt and retry.\n");
                File.CreateText(CalibrationPath.calibrationFile).Close();
                Process.Start("notepad.exe", CalibrationPath.calibrationFile);
            }
        }
    }
}