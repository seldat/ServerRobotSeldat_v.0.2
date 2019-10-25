using SeldatMRMS;
using SeldatMRMS.Management.RobotManagent;
using SeldatMRMS.Management.UnityService;
using SeldatMRMS.RobotView;
using SeldatUnilever_Ver1._02.Form;
using SeldatUnilever_Ver1._02.Management.Statistics;
using SelDatUnilever_Ver1._00.Management.DeviceManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static DoorControllerService.DoorService;
using static SelDatUnilever_Ver1._00.Management.DeviceManagement.DeviceItem;

namespace SeldatUnilever_Ver1._02
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SoundPlayer Player = null;
        public System.Timers.Timer stationtimer;
        public System.Timers.Timer chartIntterupTimer;

        public System.Timers.Timer robotTimer;



        public PieChartPercent pie;
        public PieChartPercent pieTime;


        public bool drag = true;
        public UnityManagementService unityService;
        public CanvasControlService canvasControlService;
        CtrlRobot ctrR;

        public MainWindow()
        {
            InitializeComponent();

            ApplyLanguage();
            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
            canvasMatrixTransform = new MatrixTransform(1, 0, 0, -1, 0, 0);

            //  ImageBrush img = LoadImage("Map_aTan___Copy2");
            ImageBrush img = LoadImage("Map_Layout2");
            map.Width = img.ImageSource.Width;
            map.Height = img.ImageSource.Height;
            map.Background = img;
            canvasControlService = new CanvasControlService(this);
            DataContext = canvasControlService;
            pie = new PieChartPercent(this);
            pieTime = new PieChartPercent(this);
            Global_Object.startTimeProgram = DateTime.Now.Ticks;
            //DataContext = this;
            //DataContext = new ViewModel();
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            try
            {
                unityService.robotManagementService.close();
            }
            catch { }
            Environment.Exit(Environment.ExitCode);
        }

        private void OnTimedOrderListEvent(object sender, ElapsedEventArgs e)
        {
           

            
            Dispatcher.BeginInvoke(new ThreadStart(() =>
            {
                if (bx_Check.IsChecked == true)
                {
                    try
                    {
                        canvasControlService.ReloadListDeviceItems();
                    }
                    catch { Console.WriteLine("Error reload device list"); }
                }
            }));
        }

        public int GetAmountForkLift()
        {
            if (unityService.deviceRegistrationService.deviceItemList.Count > 0)
            {
                try
                {
                    DeviceItem item = unityService.deviceRegistrationService.deviceItemList.Find(e => e.userName == "f");
                    if (item.OrderedItemList != null)
                        return item.OrderedItemList.Count;
                }
                catch { }
            }
            return 0;
        }
        public int GetAmountBufferToMachine()
        {
            int count = 0;
            if (unityService.deviceRegistrationService.deviceItemList.Count > 0)
            {
                
                foreach(DeviceItem item in unityService.deviceRegistrationService.deviceItemList)
                {
                    try
                    {
                        if (!item.userName.Equals("f"))
                        {
                            count += item.OrderedItemList.Count;
                        }
                    }
                    catch { }
                }
            }
            return count;
        }

        private void OnTimedRedrawRobotEvent(object sender, ElapsedEventArgs e)
        {
        }

        private void CenterWindowOnScreen()
        {
            double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            double windowWidth = this.Width;
            double windowHeight = this.Height;
            this.Left = (screenWidth / 2) - (windowWidth / 2);
            this.Top = (screenHeight / 2) - (windowHeight / 2);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
           /*CenterWindowOnScreen();
            myManagementWindow.Visibility = Visibility.Hidden;
            LoginForm frm = new LoginForm(Thread.CurrentThread.CurrentCulture.ToString());
            frm.ShowDialog();*/
            Global_Object.userAuthor = 2;
            if (Global_Object.userAuthor <= 2)
            {
                myManagementWindow.Visibility = Visibility.Visible;
                unityService = new UnityManagementService(this);
                unityService.Initialize();
                ctrR = new CtrlRobot(unityService.robotManagementService);
                Global_Object.mainWindowCtrl = this;
                stationtimer = new System.Timers.Timer();
                stationtimer.Interval = 20000;
                stationtimer.Elapsed += OnTimedOrderListEvent;
                stationtimer.AutoReset = true;
                stationtimer.Enabled = true;

               chartIntterupTimer= new System.Timers.Timer();
                chartIntterupTimer.Interval = 5000;
                chartIntterupTimer.Elapsed += OnChartIntterupTimer;
                chartIntterupTimer.AutoReset = true;
                chartIntterupTimer.Enabled = true;

            }
        }

        private void OnChartIntterupTimer(object sender, ElapsedEventArgs e)
        {
            List<ChartInfo> listRealChart = new List<ChartInfo>();
            List<ChartInfo> listRealChartTime = new List<ChartInfo>();
            long diffTicksProgram = DateTime.Now.Ticks-Global_Object.startTimeProgram;
            double tickMinutes = new TimeSpan(diffTicksProgram).Minutes;

            double readyTime = 24*60- TotalWorkingTime()+15;


            ChartInfo _temp_WT = new ChartInfo();
            _temp_WT.name = "Woking Time";
            _temp_WT.value = TotalWorkingTime();
            _temp_WT.color = Colors.LightSalmon;
            listRealChartTime.Add(_temp_WT);

            ChartInfo _temp_RT = new ChartInfo();
            _temp_RT.name = "Present Time";
            _temp_RT.value = tickMinutes + 1;
            _temp_RT.color = Colors.DarkGray;
            listRealChartTime.Add(_temp_RT);

            ChartInfo _temp_FB = new ChartInfo();
            _temp_FB.name = "Forklift to Buffer";
            _temp_FB.value = Global_Object.cntForkLiftToBuffer;
            _temp_FB.color = Colors.YellowGreen;
            listRealChart.Add(_temp_FB);

            ChartInfo _temp_BM = new ChartInfo();
            _temp_BM.name = "Buffer to Machine";
            _temp_BM.value = Global_Object.cntBufferToMachine;
            _temp_BM.color = Colors.DarkOrange;
            listRealChart.Add(_temp_BM);

         ChartInfo _temp_RD = new ChartInfo();
            _temp_RD.name = "Idle";
            _temp_RD.value = 10;
            _temp_RD.color = Colors.MediumBlue;
            listRealChart.Add(_temp_RD);


            //   _pie.Draw(listRealChart);
            pie.Draw(listRealChart);
            pieTime.Draw(listRealChartTime);
            pieChart.Data = pie.pieCollection;
            pieChartTime.Data = pieTime.pieCollection;
        }
        public double TotalWorkingTime()
        {
            double timeTotal = 0;
            List<DeviceItem> deviceList=unityService.deviceRegistrationService.deviceItemList;
            if (deviceList.Count > 0)
            {
                foreach (DeviceItem item in deviceList)
                {
                    if (item.OrderedItemList.Count > 0)
                    {
                        foreach (OrderItem order in item.OrderedItemList)
                        {
                            timeTotal += order.totalTimeProcedure;
                        }
                    }
                }
            }
            return timeTotal;
        }
        public void SetTextInfo(String txt)
        {
           txt_Info.Dispatcher.Invoke(() =>
            {
                txt_Info.Content = txt;
            });
        }
        private void btn_ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            ChangePassForm changePassForm = new ChangePassForm(Thread.CurrentThread.CurrentCulture.ToString());
            changePassForm.ShowDialog();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            myManagementWindow.Visibility = Visibility.Hidden;

            Global_Object.userAuthor = -2;
            Global_Object.userLogin = -2;
            Global_Object.userName = "";

            LoginForm frm = new LoginForm(Thread.CurrentThread.CurrentCulture.ToString());
            frm.ShowDialog();
            if (Global_Object.userLogin <= 2)
            {
                myManagementWindow.Visibility = Visibility.Visible;
            }
        }

        public ImageBrush LoadImage(string name)
        {
            System.Drawing.Bitmap bmp = (System.Drawing.Bitmap)Properties.Resources.ResourceManager.GetObject(name);
            ImageBrush img = new ImageBrush();
            img.ImageSource = ImageSourceForBitmap(bmp);
            return img;
        }

        public ImageSource ImageSourceForBitmap(System.Drawing.Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;

            ApplyLanguage(menuItem.Tag.ToString());

        }

        private void ApplyLanguage(string cultureName = null)
        {
            if (cultureName != null)
                Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(cultureName);

            ResourceDictionary dict = new ResourceDictionary();
            switch (Thread.CurrentThread.CurrentCulture.ToString())
            {
                case "vi-VN":
                    dict.Source = new Uri("..\\Lang\\Vietnamese.xaml", UriKind.Relative);
                    break;
                // ...
                default:
                    dict.Source = new Uri("..\\Lang\\English.xaml", UriKind.Relative);
                    break;
            }
            this.Resources.MergedDictionaries.Add(dict);

            // check/uncheck the language menu items based on the current culture
            foreach (var item in languageMenuItem.Items)
            {
                MenuItem menuItem = item as MenuItem;
                if (menuItem.Tag.ToString() == Thread.CurrentThread.CurrentCulture.Name)
                    menuItem.IsChecked = true;
                else
                    menuItem.IsChecked = false;
            }
        }

        private void Btn_MapReCenter_Click(object sender, RoutedEventArgs e)
        {
            /*MessageBox.Show(unityService.robotManagementService.RobotUnityRegistedList.Count + "");

            unityService.assigmentTaskService.AssignTaskGoToReady(unityService.robotManagementService.RobotUnityRegistedList.ElementAt(0).Value);
            */
#if true
            FormResetDoor formResetDoor;
            formResetDoor = new FormResetDoor();
            formResetDoor.ShowDialog();
#else
            String wstr = "Cảnh Báo!";
            String txtstr = "Xóa Trạng Thái Cổng ! ";
            MessageBoxButton msgb = MessageBoxButton.YesNo;
            var result = MessageBox.Show(txtstr,wstr,msgb);
            if(result== MessageBoxResult.Yes)
            {
                Global_Object.onFlagDoorBusy = false;
                Global_Object.onFlagRobotComingGateBusy = false;
                Global_Object.setGateStatus((int)DoorId.DOOR_MEZZAMINE_UP_NEW, false); // gate 1
                Global_Object.setGateStatus((int)DoorId.DOOR_MEZZAMINE_UP, false); // gate 2
                Global_Object.doorManagementServiceCtrl.DoorMezzamineUpNew.LampSetStateOff(DoorType.DOOR_FRONT);
                Global_Object.doorManagementServiceCtrl.DoorMezzamineUp.LampSetStateOff(DoorType.DOOR_FRONT);
                Global_Object.doorManagementServiceCtrl.DoorMezzamineUpNew.ResetDoor();
                Global_Object.doorManagementServiceCtrl.DoorMezzamineUp.ResetDoor();
            }
#endif
        }

        private void Ctrl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }

        private void ListBoxItem_Selected(object sender, RoutedEventArgs e)
        {
            try
            {
                ctrR.Show();
            }
            catch { }
        }

        private void Btn_Robot_Click(object sender, RoutedEventArgs e)
        {
            unityService.OpenConfigureForm("RCF");
        }

        private void Btn_Area_Click(object sender, RoutedEventArgs e)
        {
            unityService.OpenConfigureForm("ACF");
        }

        private void Btn_Charge_Click(object sender, RoutedEventArgs e)
        {
            unityService.OpenConfigureForm("CCF");
        }

        private void Btn_Door_Click(object sender, RoutedEventArgs e)
        {
            unityService.OpenConfigureForm("DCF");
        }

        private void Btn_Statistics_Click(object sender, RoutedEventArgs e)
        {
            Statistics statistics = new Statistics(Thread.CurrentThread.CurrentCulture.ToString());
            statistics.ShowDialog();
        }

        private void DeviceItemsListDg_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (DeviceItemsListDg.SelectedItem != null)
            {
                DeviceItem temp = DeviceItemsListDg.SelectedItem as DeviceItem;
                canvasControlService.ReloadListOrderItems(temp);
            }
        }

        private void Btn_Test_Click(object sender, RoutedEventArgs e)
        {
            //unityService.deviceRegistrationService.AddNewDeviceItem();

            // canvasControlService.ReloadListDeviceItems();

            try
            {
                ctrR.Show();
            }
            catch { }
        }

        private void btn_3Dmap_Click(object sender, RoutedEventArgs e)
        {
            RobotView3D robotView = new RobotView3D();
            robotView.loadAWareHouseMap();
            robotView.RegisterRobotUnityList(new List<RobotUnity>(unityService.robotManagementService.RobotUnityRegistedList.Values));

            robotView.Show();
        }

        private void btn_Stop_Click(object sender, RoutedEventArgs e)
        {
            //unityService.robotManagementService.Stop();
            btn_Play.IsEnabled = true;
            btn_Stop.IsEnabled = false;
            btn_Play_icon.Foreground = new SolidColorBrush(Colors.Green);
            btn_Stop_icon.Foreground = new SolidColorBrush(Colors.Red);
            unityService.assigmentTaskService.Dispose();
           // Global_Object.onAcceptDevice = false;
        }

        private void btn_Play_Click(object sender, RoutedEventArgs e)
        {
            unityService.robotManagementService.Run();
            btn_Play.IsEnabled = false;
            btn_Stop.IsEnabled = true;
            btn_Play_icon.Foreground = new SolidColorBrush(Colors.Red);
            btn_Stop_icon.Foreground = new SolidColorBrush(Colors.Green);
            //Global_Object.onAcceptDevice = true;
            unityService.assigmentTaskService.Start();
        }

        private void btn_RiskArea_Click(object sender, RoutedEventArgs e)
        {
            unityService.OpenConfigureForm("RACF");
        }

        private void OrderItemsListDg_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void DeviceItemsListDg_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Btn_Sound_Click(object sender, RoutedEventArgs e)
        {
            PlayWav(Properties.Resources.ALARM, true);
        }
        private void PlayWav(Stream stream, bool play_looping)
        {
            // Stop the player if it is running.
            if (Player != null)
            {
                Player.Stop();
                Player.Dispose();
                Player = null;
            }

            // If we have no stream, we're done.
            if (stream == null) return;

            // Make the new player for the WAV stream.
            Player = new SoundPlayer(stream);

            // Play.
            if (play_looping)
                Player.PlayLooping();
            else
                Player.Play();
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {

        }

   

        private void RemoveOrder_Click(object sender, RoutedEventArgs e)
        {
            OrderItem orderItem = (sender as Button).DataContext as OrderItem;
            Task.Run(() => {
                try
                {
                    foreach (DeviceItem devI in unityService.deviceRegistrationService.deviceItemList)
                    {
                        if (devI.userName == orderItem.userName)
                        {
                            if (orderItem.status == StatusOrderResponseCode.PENDING)
                                devI.RemoveCallBack(orderItem);
                            else
                                MessageBox.Show("Chỉ có thể xóa Pending !");
                        }
                    }
                }
                catch { }
                
            });


        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
         
            if(unityService.robotManagementService.CheckAnyRobotWorking())
            {
                String wstr = "Cảnh Báo!";
                String txtstr = "Đang có robot trong quy trình, Vẫn tiếp tục đóng chương trình? ";
                MessageBoxButton msgb = MessageBoxButton.YesNo;
                var result = MessageBox.Show(txtstr, wstr, msgb);
                if (result == MessageBoxResult.Yes)
                {
                    if (unityService == null)
                    {
                        Environment.Exit(0);
                        return;
                    }

                    /*Global_Object.onFlagDoorBusy = false;
                    Global_Object.onFlagRobotComingGateBusy = false;
                    Global_Object.setGateStatus((int)DoorId.DOOR_MEZZAMINE_UP_NEW, false); // gate 1
                    Global_Object.setGateStatus((int)DoorId.DOOR_MEZZAMINE_UP, false); // gate 2
                    Global_Object.doorManagementServiceCtrl.DoorMezzamineUpNew.LampOff(DoorType.DOOR_FRONT);
                    Global_Object.doorManagementServiceCtrl.DoorMezzamineUp.LampOff(DoorType.DOOR_FRONT);*/
                    unityService.deviceRegistrationService.SaveDeviceOrderList();
                    unityService.robotManagementService.close();
                    Environment.Exit(0);
                }
                else if (result == MessageBoxResult.No)
                {

                }
            }
            else
            {
                //String wstr = "Cảnh Báo!";
                //String txtstr = "Vẫn tiếp tục đóng chương trình? ";
                //MessageBoxButton msgb = MessageBoxButton.YesNo;
                //var result = MessageBox.Show(txtstr, wstr, msgb);
                //if (result == MessageBoxResult.Yes)
                //{
                //   /* Global_Object.onFlagDoorBusy = false;
                //    Global_Object.onFlagRobotComingGateBusy = false;
                //    Global_Object.setGateStatus((int)DoorId.DOOR_MEZZAMINE_UP_NEW, false); // gate 1
                //    Global_Object.setGateStatus((int)DoorId.DOOR_MEZZAMINE_UP, false); // gate 2
                //    Global_Object.doorManagementServiceCtrl.DoorMezzamineUpNew.LampOff(DoorType.DOOR_FRONT);
                //    Global_Object.doorManagementServiceCtrl.DoorMezzamineUp.LampOff(DoorType.DOOR_FRONT);*/
                //    unityService.robotManagementService.close();
                //    Environment.Exit(0);
                //}
                //else if (result == MessageBoxResult.No)
                //{

                //}
            }
        }

        private void Window_Closing_1(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void Btn_ResetSpeed_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Btn_ResetSpeed_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private void Bx_Check_Checked(object sender, RoutedEventArgs e)
        {
           
        }
    }
}
