﻿using SeldatMRMS.RobotView;
using SeldatUnilever_Ver1._02.Management.RobotManagent;
using SeldatUnilever_Ver1._02.Management.TrafficManager;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using static DoorControllerService.DoorService;
using static SelDatUnilever_Ver1._00.Management.TrafficManager.TrafficRounterService;

namespace SeldatMRMS.Management.RobotManagent
{
    public class RobotUnity : RobotBaseService
    {
        public String name = "";
        public String StartPointName = "";
        public String EndPointName = "";
        public Point StartPoint = new Point();
        public Point EndPoint = new Point();
        Ellipse headerPoint;
        Ellipse headerPoint1;
        Ellipse headerPoint2;
        Ellipse headerPoint3;
        Path safetyArea;

        SafeCircle smallCircle;
        SafeCircle blueCircle;
        SafeCircle greenCircle;
        SafeCircle redCircle;
        SafeCircle orangeCircle;
        double angle = 0.0f;
        public Point org = new Point(600, 350);
        public double rad = 0;
        public double anglestep = 0;
        Rect area = new Rect(30, 30, 500, 500);
        Point loc = new Point(0, 0);
        public event Action<string> RemoveHandle;
        public enum RobotStatusColorCode
        {
            ROBOT_STATUS_OK = 0,
            ROBOT_STATUS_RUNNING,
            ROBOT_STATUS_ERROR,
            ROBOT_STATUS_WAIT_FIX,
            ROBOT_STATUS_DISCONNECT,
            ROBOT_STATUS_CONNECT,
            ROBOT_STATUS_RECONNECT,
            ROBOT_STATUS_CHARGING,
            ROBOT_STATUS_CAN_NOTGET_DATA,
            ROBOT_STOP_BUUFER_SAME_LINE
        }
        public struct Props
        {
            public string name;
            public bool isSelected;
            public bool isHovering;
            public Grid mainGrid;
            public Grid statusGrid;

            public Label rbID;
            public Label rbTask;
            public Rectangle headLed;
            public Rectangle tailLed;
            public TranslateTransform rbTranslate;
            public TransformGroup rbTransformGroup;
            public RotateTransform rbRotateTransform;
            public TranslateTransform contentTranslate;
            public TransformGroup contentTransformGroup;
            public RotateTransform contentRotateTransform;
            public Border statusBorder;
            public List<Point> eightCorner;
        }
        public Canvas canvas;
        public Props props;
        public Border border;
        public LoadedConfigureInformation loadConfigureInformation;
        public RobotManagementService robotService;
        public SolvedProblem solvedProblem;
        public Robot3D robot3DModel = null;
        public RobotUnity()
        {
            solvedProblem = new SolvedProblem();

        }
        MenuItem problemSolutionItem = new MenuItem();
        MenuItem startItem = new MenuItem();
        MenuItem pauseItem = new MenuItem();
        MenuItem resumeItem = new MenuItem();
        MenuItem liftUp = new MenuItem();
        MenuItem liftDown = new MenuItem();
        MenuItem laserBackOn = new MenuItem();
        MenuItem laserBackOff = new MenuItem();
        MenuItem connectItem = new MenuItem();
        MenuItem retryconnectItem = new MenuItem();
        MenuItem disposeItem = new MenuItem();
        MenuItem disconnectedItem = new MenuItem();
        MenuItem turnOnOffItem = new MenuItem();
        MenuItem addReadyListItem = new MenuItem();
        MenuItem addWaitTaskListItem = new MenuItem();
        MenuItem logOutItem = new MenuItem();
        MenuItem chargeManualStart = new MenuItem();
        MenuItem chargeManualStop = new MenuItem();
        MenuItem forcedGate = new MenuItem();
        public void Initialize(Canvas canvas)
        {
            this.canvas = canvas;
            //ModelVisual3D layer = new ModelVisual3D();
            // robot3DModel = new Robot3D(properties.NameId, layer);
            safetyArea = new Path();
            safetyArea.Stroke = new SolidColorBrush(Colors.MediumBlue);
            safetyArea.StrokeThickness = 1;
            border = new Border();
            border.ToolTip = "";
            border.ToolTipOpening += ChangeToolTipContent;
            props.isSelected = false;
            props.isHovering = false;
            border.ContextMenu = new ContextMenu();


            problemSolutionItem.Header = "Problem Solution";
            problemSolutionItem.Click += PoblemSolutionItem;
            //===================================

            startItem.Header = "Start";
            startItem.Click += StartMenu;
            //===================================

            pauseItem.Header = "Pause";
            pauseItem.Click += PauseMenu;
            pauseItem.IsEnabled = true;

            resumeItem.Header = "Resume";
            resumeItem.Click += ResumeMenu;
            resumeItem.IsEnabled = true;

            liftUp.Header = "Lift Up";
            liftUp.Click += LiftUpMenu;
            liftUp.IsEnabled = true;
            liftDown.Header = "Lift Down";
            liftDown.Click += LiftDownMenu;
            liftDown.IsEnabled = true;

            laserBackOn.Header = "Laser Back On";
            laserBackOn.Click += LaserBackOn;
            laserBackOn.IsEnabled = true;

            laserBackOff.Header = "Laser Back Off";
            laserBackOff.Click += LaserBackOff;
            laserBackOff.IsEnabled = true;

            logOutItem.Header = "Log";
            logOutItem.Click += LogOut;
            logOutItem.IsEnabled = true;

            addReadyListItem.Header = "Add To Ready Mode";
            addReadyListItem.Click += AddReadyListMenu;
            addReadyListItem.IsEnabled = true;

            addWaitTaskListItem.Header = "Add To WaitTask Mode";
            addWaitTaskListItem.Click += AddWaitTaskListMenu;
            addWaitTaskListItem.IsEnabled = true;

            connectItem.Header = "Connect";
            connectItem.Click += ConnectMenu;
            connectItem.IsEnabled = true;

            retryconnectItem.Header = "ReConnect";
            retryconnectItem.Click += ReConnectMenu;
            retryconnectItem.IsEnabled = false;

            disconnectedItem.Header = "DisConnect";
            disconnectedItem.Click += DisConnectMenu;
            disconnectedItem.IsEnabled = false;

            disposeItem.Header = "Dispose";
            disposeItem.Click += DisposeMenu;
            disposeItem.IsEnabled = false;

            turnOnOffItem.Header = "Set On/Off Traffic";
            turnOnOffItem.Click += SetOnOffTrafficMenu;
            turnOnOffItem.IsEnabled = false;

            chargeManualStart.Header = "Request Charge";
            chargeManualStart.Click += ChargeManualStartMenu;
            chargeManualStart.IsEnabled = true;

            chargeManualStop.Header = "Stop Charge";
            chargeManualStop.Click += ChargeManualStopMenu;
            chargeManualStop.IsEnabled = true;


            forcedGate.Header = "Force Gate";
            forcedGate.Click += ForceGateMenu;
            forcedGate.IsEnabled = false;



            border.ContextMenu.Items.Add(problemSolutionItem);
            border.ContextMenu.Items.Add(startItem);
            border.ContextMenu.Items.Add(pauseItem);
            border.ContextMenu.Items.Add(resumeItem);

            border.ContextMenu.Items.Add(liftUp);
            border.ContextMenu.Items.Add(liftDown);
            border.ContextMenu.Items.Add(logOutItem);

            border.ContextMenu.Items.Add(laserBackOn);
            border.ContextMenu.Items.Add(laserBackOff);

            border.ContextMenu.Items.Add(connectItem);
            border.ContextMenu.Items.Add(retryconnectItem);
            border.ContextMenu.Items.Add(disconnectedItem);
            border.ContextMenu.Items.Add(disposeItem);
            border.ContextMenu.Items.Add(turnOnOffItem);

            border.ContextMenu.Items.Add(addReadyListItem);
            border.ContextMenu.Items.Add(addWaitTaskListItem);

            border.ContextMenu.Items.Add(chargeManualStart);
            border.ContextMenu.Items.Add(chargeManualStop);
            border.ContextMenu.Items.Add(forcedGate);

            //====================EVENT=====================
            //MouseLeave += MouseLeavePath;
            //MouseMove += MouseHoverPath;
            //MouseLeftButtonDown += MouseLeftButtonDownPath;
            //MouseRightButtonDown += MouseRightButtonDownPath;
            //===================CREATE=====================
            //Name = "Robotx" + Global_Mouse.EncodeTransmissionTimestamp();
            props.mainGrid = new Grid();
            props.statusGrid = new Grid();
            props.statusBorder = new Border();
            props.rbID = new Label();
            props.rbTask = new Label();
            props.headLed = new Rectangle();
            props.tailLed = new Rectangle();
            props.eightCorner = new List<Point>();
            for (int i = 0; i < 8; i++)
            {
                Point temp = new Point();
                props.eightCorner.Add(temp);
            }
            props.rbRotateTransform = new RotateTransform();
            props.rbTranslate = new TranslateTransform();
            props.rbTransformGroup = new TransformGroup();
            props.contentRotateTransform = new RotateTransform();
            props.contentTranslate = new TranslateTransform();
            props.contentTransformGroup = new TransformGroup();
            // robotProperties = new Properties(this);
            //===================STYLE=====================
            //Robot border
            border.Width = 22;
            border.Height = 15;
            border.BorderThickness = new Thickness(1);
            border.BorderBrush = new SolidColorBrush(Colors.Linen);
            border.Background = new SolidColorBrush(Colors.Blue);
            border.CornerRadius = new CornerRadius(3);
            border.RenderTransformOrigin = new Point(0.5, 0.5);
            border.MouseLeftButtonDown += KeepOffTraffic;
            border.MouseLeftButtonUp += KeepOnTraffic;
            //mainGrid
            props.mainGrid.Background = new SolidColorBrush(Colors.Transparent);
            for (int i = 0; i < 3; i++)
            {
                ColumnDefinition colTemp = new ColumnDefinition();
                //colTemp.Name = properties.NameID + "xL" + i;
                if ((i == 0) || (i == 2))
                {
                    colTemp.Width = new GridLength(1);
                }
                props.mainGrid.ColumnDefinitions.Add(colTemp);
            }
            //headLed
            props.headLed.Height = 7;
            props.headLed.Fill = new SolidColorBrush(Colors.LightYellow);
            Grid.SetColumn(props.headLed, 2);
            //tailLed
            props.tailLed.Height = 7;
            props.tailLed.Fill = new SolidColorBrush(Colors.OrangeRed);
            Grid.SetColumn(props.tailLed, 0);
            //statusBorder
            props.statusBorder.Width = 10;
            props.statusBorder.Height = 13;
            props.statusBorder.RenderTransformOrigin = new Point(0.5, 0.5);
            Grid.SetColumn(props.statusBorder, 1);
            //statusGrid
            for (int i = 0; i < 2; i++)
            {
                RowDefinition rowTemp = new RowDefinition();
                // rowTemp.Name = properties.NameID + "xR" + i;
                props.statusGrid.RowDefinitions.Add(rowTemp);
            }
            //rbID
            props.rbID.Padding = new Thickness(0);
            props.rbID.Margin = new Thickness(-5, -2, -5, 0);
            props.rbID.HorizontalAlignment = HorizontalAlignment.Center;
            props.rbID.VerticalAlignment = VerticalAlignment.Bottom;
            props.rbID.Content = "27";
            props.rbID.Foreground = new SolidColorBrush(Colors.Yellow);
            props.rbID.FontFamily = new FontFamily("Calibri");
            props.rbID.FontSize = 14;
            props.rbID.FontWeight = FontWeights.Bold;
            Grid.SetRow(props.rbID, 0);
            Grid.SetRowSpan(props.rbID, 2);

            //rbTask
            props.rbTask.Padding = new Thickness(0);
            props.rbTask.Margin = new Thickness(-5, -1, -5, -1);
            props.rbTask.HorizontalAlignment = HorizontalAlignment.Center;
            props.rbTask.VerticalAlignment = VerticalAlignment.Top;
            props.rbTask.Content = "";
            props.rbTask.Foreground = new SolidColorBrush(Colors.LawnGreen);
            props.rbTask.FontFamily = new FontFamily("Calibri");
            props.rbTask.FontSize = 6;
            props.rbTask.FontWeight = FontWeights.Bold;


            //===================CHILDREN===================
            props.statusGrid.Children.Add(props.rbID);
            props.statusGrid.Children.Add(props.rbTask);
            props.statusBorder.Child = props.statusGrid;
            props.mainGrid.Children.Add(props.headLed);
            props.mainGrid.Children.Add(props.tailLed);
            props.mainGrid.Children.Add(props.statusBorder);
            props.rbTransformGroup.Children.Add(props.rbRotateTransform);
            props.rbTransformGroup.Children.Add(props.rbTranslate);
            border.RenderTransform = props.rbTransformGroup;
            props.contentTransformGroup.Children.Add(props.contentRotateTransform);
            props.contentTransformGroup.Children.Add(props.contentTranslate);
            props.statusBorder.RenderTransform = props.contentTransformGroup;
            border.Child = props.mainGrid;
            this.canvas.Children.Add(border);
            headerPoint = new Ellipse();
            headerPoint.Width = 5;
            headerPoint.Height = 5;
            headerPoint.Fill = new SolidColorBrush(Colors.Red);

            headerPoint1 = new Ellipse();
            headerPoint1.Width = 5;
            headerPoint1.Height = 5;
            headerPoint1.Fill = new SolidColorBrush(Colors.Red);

            headerPoint2 = new Ellipse();
            headerPoint2.Width = 5;
            headerPoint2.Height = 5;
            headerPoint2.Fill = new SolidColorBrush(Colors.Red);

            headerPoint3 = new Ellipse();
            headerPoint3.Width = 5;
            headerPoint3.Height = 5;
            headerPoint3.Fill = new SolidColorBrush(Colors.Red);

            canvas.Children.Add(safetyArea);
            canvas.Children.Add(headerPoint);
            canvas.Children.Add(headerPoint1);
            canvas.Children.Add(headerPoint2);
            canvas.Children.Add(headerPoint3);
            setColorRobotStatus(RobotStatusColorCode.ROBOT_STATUS_DISCONNECT);

            smallCircle = new SafeCircle(canvas, Colors.Black, 1);
            blueCircle = new SafeCircle(canvas, Colors.Blue, 1);
            redCircle = new SafeCircle(canvas, Colors.Red, 1);
            greenCircle = new SafeCircle(canvas, Colors.Green, 1);
            orangeCircle = new SafeCircle(canvas, Colors.Orange, 1);

            Center_S = 0;
            Center_B = 45;//40
            Center_G = 45;
            Center_R = 10;
            Center_O = -20;
            new Thread(() =>
            {
                while (true)
                {
                    Draw();
                    Thread.Sleep(700);
                }
            }).Start();
            //  robotLogOut.SetName(properties.Label);

        }

        private void ForceGateMenu(object sender, RoutedEventArgs e)
        {
            //ForceGate forceGateF = new ForceGate(this);
            //forceGateF.Show();
            if (!onForceGoToGate)
                onForceGoToGate = true;
        }

        private void KeepOnTraffic(object sender, MouseButtonEventArgs e)
        {
            //throw new NotImplementedException();
            TurnOnSupervisorTraffic(true);
        }

        private void KeepOffTraffic(object sender, MouseButtonEventArgs e)
        {
            TurnOnSupervisorTraffic(false);
            //throw new NotImplementedException();
        }

        private void Itemw0_Checked(object sender, RoutedEventArgs e)
        {

        }

        public void RegistrySolvedForm(Object obj)
        {
            //  if(obj.GetType()==typeof(ProcedureControlServices))
            {
                solvedProblem.Registry(obj);
                //solvedProblem.Show();
            }
        }
        public void RegistryRobotService(RobotManagementService robotService)
        {
            this.robotService = robotService;
        }
        public void DestroyRegistrySolvedForm()
        {
            //  if(obj.GetType()==typeof(ProcedureControlServices))
            {
                solvedProblem.Dispose();
                //solvedProblem.Show();
            }
        }
        public void DisplaySolvedForm()
        {
            try
            {
                if (solvedProblem.obj != null)
                    solvedProblem.Show();
                else
                    MessageBox.Show("Không có nội dung lỗi!!");
            }
            catch { }
        }
        public Point CirclePoint(double radius, double angleInDegrees, Point origin)
        {
            double x = (double)(radius * Math.Cos(angleInDegrees * Math.PI / 180)) + origin.X;
            double y = (double)(radius * Math.Sin(angleInDegrees * Math.PI / 180)) + origin.Y;
            return new Point(x, y);
        }
        //public void setColorRobotStatus(RobotStatusColorCode rsc)
        //{
        //    switch (rsc)
        //    {
        //        case RobotStatusColorCode.ROBOT_STATUS_OK:
        //            border.Background = new SolidColorBrush(Colors.Blue);
        //            break;
        //        case RobotStatusColorCode.ROBOT_STATUS_ERROR:
        //            border.Background = new SolidColorBrush(Colors.Red);
        //            break;
        //    }
        //}

        public void setColorRobotStatus(RobotStatusColorCode rsc)
        {
            base.setColorRobotStatus(rsc, this);
        }

        private void ChangeToolTipContent(object sender, ToolTipEventArgs e)
        {
            try
            {
                String OrderStr = "";
                if (orderItem != null)
                    OrderStr = "" + orderItem.typeReq + " / " + orderItem.productDetailName;

                TypeZone typezone = trafficManagementService.GetTypeZone(properties.pose.Position, 0, 200);
                double angle = -properties.pose.Angle;
                Point position = Global_Object.CoorLaser(properties.pose.Position);
                String statusLaserBack;
                if (resetLaserBack)
                {
                    statusLaserBack = " Passed error laserBack";
                }
                else
                {
                    statusLaserBack = " LaserBack run normal";
                }
                String tooltipStr = "Name: " + properties.Label + Environment.NewLine + "Zone: " + typezone +
                    Environment.NewLine + " Location: " + position.X.ToString("0.00") + " / " +
                    position.Y.ToString("0.00") + " / " + angle.ToString("0.00") + Environment.NewLine +
                    "Place: " + TyprPlaceStr + Environment.NewLine +
                    "Working Zone: " + robotRegistryToWorkingZone.WorkingZone + "/ " + trafficManagementService.DetermineArea(this.properties.pose.Position, 0, 200) + Environment.NewLine +
                    "Radius _S" + Radius_S + Environment.NewLine +
                    "Radius _Y" + Radius_R + Environment.NewLine +
                    "Radius _B" + Radius_B + Environment.NewLine +
                    "Speed Set :" + properties.speedInSpecicalArea + Environment.NewLine +
                    "STATE: " + STATE_SPEED + Environment.NewLine +
                    "MIDDLE :" + MiddleHeaderCv().X.ToString("0.00") + " /" + MiddleHeaderCv().Y.ToString("0.00") + Environment.NewLine +
                    "MIDDLE1 :" + MiddleHeaderCv1().X.ToString("0.00") + " /" + MiddleHeaderCv1().Y.ToString("0.00") + Environment.NewLine +
                    "MIDDLE2 :" + MiddleHeaderCv2().X.ToString("0.00") + " /" + MiddleHeaderCv2().Y.ToString("0.00") + Environment.NewLine +
                    "ValueR SC:" + valueSC + Environment.NewLine +
                    "ValueR BigC:" + valueBigC + Environment.NewLine +
                    "RobotTag:" + robotTag + Environment.NewLine +
                    "CheckGate: " + robotRegistryToWorkingZone.onRobotwillCheckInsideGate + Environment.NewLine +
                    "Order: " + OrderStr + Environment.NewLine +
                    "Battery Level: " + properties.BatteryLevelRb + Environment.NewLine +
                    "Robots Registry in Ready: " + TrafficRountineConstants.RegIntZone_READY.getNames() + Environment.NewLine +
                     "Robots Registry in GATE 12: " + TrafficRountineConstants.RegIntZone_GATE12.getNames() + Environment.NewLine +
                     "Robots Registry in Elevator: " + TrafficRountineConstants.RegIntZone_ELEVATOR.getNames() + Environment.NewLine +
                       "Robots Registry in Gate3: " + TrafficRountineConstants.RegIntZone_GATE3.getNames() + Environment.NewLine +
                    "Gate 1: " + Global_Object.getGateStatus((int)DoorId.DOOR_MEZZAMINE_UP) + Environment.NewLine +
                    "Gate 2: " + Global_Object.getGateStatus((int)DoorId.DOOR_MEZZAMINE_UP_NEW) + Environment.NewLine +
                    "Robot_BAYID: " + bayId + Environment.NewLine +
                    "Start_Point_Name: " + StartPointName + Environment.NewLine +
                    "Start_Point: " + StartPoint.ToString() + Environment.NewLine +
                    "End_Point_Name: " + EndPointName + Environment.NewLine +
                    "end_Point: " + EndPoint.ToString() + Environment.NewLine +
                    "laserBackStatus :" + statusLaserBack + Environment.NewLine
                    ;

                border.ToolTip = tooltipStr;
            }
            catch { }
        }

        private void PoblemSolutionItem(object sender, RoutedEventArgs e)
        {
            DisplaySolvedForm();
        }
        private void PauseMenu(object sender, RoutedEventArgs e)
        {
            SetSpeedTrafficManual(RobotSpeedLevel.ROBOT_SPEED_STOP);
        }

        private void ResumeMenu(object sender, RoutedEventArgs e)
        {
            this.setTrafficMode(TrafficMode.AUTO_MODE);
            SetSpeedTraffic(RobotSpeedLevel.ROBOT_SPEED_NORMAL, false);
        }
        private void AddReadyListMenu(object sender, RoutedEventArgs e)
        {
            string msgtext = "Hãy đảm bảo Robot đã được đưa về vị trí trạm Sạc, Quy trình trước đó sẽ được hủy !";
            string txt = "Cảnh báo";
            MessageBoxButton button = MessageBoxButton.OKCancel;
            MessageBoxResult result = MessageBox.Show(msgtext, txt, button);
            switch (result)
            {
                case MessageBoxResult.OK:
                    DisposeProcedure();
                    KillPID();
                    KillActionLib();
                    TurnOnSupervisorTraffic(false);
                    this.PreProcedureAs = ProcedureControlAssign.PRO_READY;
                    robotService.RemoveRobotUnityReadyList(this);
                    robotService.RemoveRobotUnityWaitTaskList(this);
                    robotService.AddRobotUnityReadyList(this);
                    Draw();
                    break;
                case MessageBoxResult.Cancel:
                    break;
            }
        }
        public void LogOut(object sender, RoutedEventArgs e)
        {
            robotLogOut.Show();
        }
        public void ShowText(String text)
        {
            robotLogOut.ShowText(this.properties.Label, text);

        }
        public void ClearText()
        {
            robotLogOut.Clear();
        }
        private void ReConnectMenu(object sender, RoutedEventArgs e)
        {
            /*  if (webSocket != null)
              {
                  webSocket.Connect();
              }*/
        }
        private void ConnectMenu(object sender, RoutedEventArgs e)
        {

            // Dispose();
            if (!properties.IsConnected)
            {
                onBinding = true;
                Start(properties.Url);
                connectItem.IsEnabled = false;
                disposeItem.IsEnabled = true;
                disconnectedItem.IsEnabled = true;
                MessageBox.Show("Để robot có thể tiếp tục hãy add Robot vào Ready Mode hoặc TaskWait Mode !");

                SetSpeedHighPrioprity(RobotSpeedLevel.ROBOT_SPEED_NORMAL, false);
            }
            /*Radius_S = 4 * properties.Scale;
            Radius_B = 4 * properties.Scale;
            Radius_Y = 4 * properties.Scale;
            Radius_O = 3 * properties.Scale;*/

        }
        private void DisposeMenu(object sender, RoutedEventArgs e)
        {
            laserBackOffRb();
            DisposeF();
        }
        public void DisposeF()
        {
            DisposeProcedure();
            KillPID();
            KillActionLib();
            laserBackOffRb();
            TurnOnSupervisorTraffic(true);
            SwitchToDetectLine(false);
            robotService.RemoveRobotUnityReadyList(this);
            robotService.RemoveRobotUnityWaitTaskList(this);
            //  MessageBox.Show("Đã Xóa Khỏi  Ready Mode hoặc TaskWait Mode !");
            onBinding = false;
            onFlagDetectLine = false;
            onFlagFinishPalletUpDownINsideBuffer = false;
            onFlagGoBackReady = false;
            onFlagReadyGo = false;
            onForceGoToGate = false;
            Draw();
        }
        private void DisConnectMenu(object sender, RoutedEventArgs e)
        {
            laserBackOffRb();
            DisposeProcedure();
            Dispose();
            KillPID();
            KillActionLib();
            robotService.RemoveRobotUnityReadyList(this);
            robotService.RemoveRobotUnityWaitTaskList(this);
            MessageBox.Show("Đã Xóa Khỏi  Ready Mode hoặc TaskWait Mode !");
            onBinding = false;
            connectItem.IsEnabled = true;
            Reset();
            setColorRobotStatus(RobotStatusColorCode.ROBOT_STATUS_DISCONNECT);
            Draw();

        }
        public override void RequestGotoReadyHandler(Communication.Message message)
        {
          /*  StandardInt32 rqVal = (StandardInt32)message;
            if (rqVal.data == 1)
            {
                DisposeF();
                AddWaitTask();
                //Console.WriteLine("request goto ready");
            }*/
        }
        public void Reset()
        {
            TrafficRountineConstants.RegIntZone_READY.Release(this);
            properties.pose.Position = properties.poseRoot.Position;
            properties.pose.Angle = properties.poseRoot.Angle;
            properties.pose.AngleW = properties.poseRoot.AngleW;
            connectItem.IsEnabled = true;
            disposeItem.IsEnabled = false;
            TurnOnSupervisorTraffic(false);
            SwitchToDetectLine(false);
            properties.IsConnected = false;
            robotService.RemoveRobotUnityReadyList(this);
            robotService.RemoveRobotUnityWaitTaskList(this);
            robotRegistryToWorkingZone.Release();
            robotRegistryToWorkingZone.onRobotwillCheckInsideGate = false;
            robotTag = RobotStatus.IDLE;
            onFlagDetectLine = false;
            onFlagFinishPalletUpDownINsideBuffer = false;
            onFlagGoBackReady = false;
            onFlagReadyGo = false;
            onForceGoToGate = false;
            setColorRobotStatus(RobotStatusColorCode.ROBOT_STATUS_DISCONNECT);

            // reset lost position
            flagLostPosition = false;
            SetNormalSpeedOrtherRobotLostPosition();

            Radius_S = 0;
            Radius_B = 0;
            Radius_O = 0;
            Radius_R = 0;
            Radius_G = 0;
            /* Center_S = 0;
             Center_B = 0;
             Center_Y = 0;
             Center_O = 0;*/
        }

        public void ChargeManualStartMenu(object sender, RoutedEventArgs e)
        {
            if (true == robotService.CheckRobotUnityInReadyList(this))
            {
                this.properties.enableChage = true;
            }
            else {
                String wstr = "Warning";
                String txtstr = "Robot outside the ready area. Can't start manual chage .";
                MessageBoxButton msgb = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Warning;
                var result = MessageBox.Show(txtstr, wstr, msgb,icon);
            }
        }

        public void ChargeManualStopMenu(object sender, RoutedEventArgs e)
        {
            this.properties.enableChage = false;
        }
        public void SetOnOffTrafficMenu(object sender, RoutedEventArgs e)
        {

            TurnOnSupervisorTraffic(onFlagSupervisorTraffic ? false : true);
        }
        public void AddWaitTaskListMenu(object sender, RoutedEventArgs e)
        {


            string msgtext = "Quy trình trước đó sẽ được hủy, Robot sẽ bắt đầu với quy trình mới !";
            string txt = "Cảnh báo";
            MessageBoxButton button = MessageBoxButton.OKCancel;
            MessageBoxResult result = MessageBox.Show(msgtext, txt, button);
         
            switch (result)
            {
                case MessageBoxResult.OK:
                    AddWaitTask();
                    break;
                case MessageBoxResult.Cancel:
                    break;
            }
        }
        public void AddWaitTask()
        {
            DisposeProcedure();
            KillPID();
            KillActionLib();
            TurnOnSupervisorTraffic(true);
            SwitchToDetectLine(false);
            this.PreProcedureAs = ProcedureControlAssign.PRO_IDLE;
            robotService.RemoveRobotUnityReadyList(this);
            robotService.RemoveRobotUnityWaitTaskList(this);
            robotService.AddRobotUnityWaitTaskList(this);
            Draw();
        }
        private void StartMenu(object sender, RoutedEventArgs e)
        {
            this.setTrafficMode(TrafficMode.AUTO_MODE);
            SetSpeedTraffic(RobotSpeedLevel.ROBOT_SPEED_NORMAL, false);
        }

        private void LiftUpMenu(object sender, RoutedEventArgs e)
        {
            LiftCtrlUp();
        }

        private void LiftDownMenu(object sender, RoutedEventArgs e)
        {
            LiftCtrlDown();
        }

        private void LaserBackOn(object sender, RoutedEventArgs e)
        {
            resetLaserBack = false;
        }

        private void LaserBackOff(object sender, RoutedEventArgs e)
        {
            resetLaserBack = true;
            laserBackOffRb();
        }
        public void RemoveDraw()
        {
            this.canvas.Children.Remove(border);
            this.canvas.Children.Remove(headerPoint);
            this.canvas.Children.Remove(safetyArea);
            //RemoveHandle(props.name);
        }

       public override bool SetSpeedHighPrioprity(RobotSpeedLevel robotspeed, bool highpriority)
        {
            setColorRobotStatus(RobotStatusColorCode.ROBOT_STOP_BUUFER_SAME_LINE);
           return  base.SetSpeedHighPrioprity(robotspeed, highpriority);
        }
        public override void Draw()
        {

            try
            {
                if (properties.IsConnected)
                {
                    if ((agvErr == true) || (flagLostPosition == true))
                    {
                        setColorRobotStatus(RobotStatusColorCode.ROBOT_STATUS_ERROR);
                    }
                    else
                    {
                        setColorRobotStatus(RobotStatusColorCode.ROBOT_STATUS_CONNECT);
                    }
                }
                else
                {
                    if (properties.RequestChargeBattery)
                        setColorRobotStatus(RobotStatusColorCode.ROBOT_STATUS_CHARGING);
                    else
                        setColorRobotStatus(RobotStatusColorCode.ROBOT_STATUS_DISCONNECT);

                }
                this.border.Dispatcher.BeginInvoke(new System.Threading.ThreadStart(() =>
                {

                    props.rbRotateTransform.Angle = -properties.pose.Angle;
                    Point cPoint = Global_Object.CoorCanvas(properties.pose.Position);
                    props.rbTranslate = new TranslateTransform(cPoint.X - (border.Width / 2), cPoint.Y - (border.Height / 2));
                    props.rbTransformGroup.Children[1] = props.rbTranslate;
                    //Render Status
                    props.contentRotateTransform.Angle = (properties.pose.Angle);
                    props.contentTranslate = new TranslateTransform(0, 0);
                    props.contentTransformGroup.Children[1] = props.contentTranslate;
                    headerPoint.RenderTransform = new TranslateTransform(MiddleHeaderCv().X - 2.5, MiddleHeaderCv().Y - 1);
                    headerPoint1.RenderTransform = new TranslateTransform(MiddleHeaderCv1().X - 2.5, MiddleHeaderCv1().Y - 1);
                    headerPoint2.RenderTransform = new TranslateTransform(MiddleHeaderCv2().X - 2.5, MiddleHeaderCv2().Y - 1);
                    headerPoint3.RenderTransform = new TranslateTransform(MiddleHeaderCv3().X - 2.5, MiddleHeaderCv3().Y - 1);


                    PathGeometry pgeometry = new PathGeometry();
                    PathFigure pF = new PathFigure();
                    pF.StartPoint = TopHeaderCv();

                    // pF.StartPoint = new Point(TopHeader().X * 10, TopHeader().Y * 10);
                    LineSegment pp = new LineSegment();

                    pF.Segments.Add(new LineSegment() { Point = BottomHeaderCv() });
                    pF.Segments.Add(new LineSegment() { Point = BottomTailCv() });
                    pF.Segments.Add(new LineSegment() { Point = TopTailCv() });
                    pF.Segments.Add(new LineSegment() { Point = TopHeaderCv() });
                    // pF.Segments.Add(new LineSegment() { Point = new Point(BottomHeader().X*10, BottomHeader().Y * 10) });
                    //pF.Segments.Add(new LineSegment() { Point = new Point(BottomTail().X * 10, BottomTail().Y * 10) });
                    //  pF.Segments.Add(new LineSegment() { Point = new Point(TopTail().X * 10, TopTail().Y * 10) });
                    //pF.Segments.Add(new LineSegment() { Point = new Point(TopHeader().X * 10, TopHeader().Y * 10) });
                    pgeometry.Figures.Add(pF);
                    safetyArea.Data = pgeometry;

                    //  props.rbID.Content = properties.pose.Position.X.ToString("0");
                    // props.rbTask.Content = properties.pose.Position.Y.ToString("0");
                    props.rbID.Content = properties.Label;
                    // props.rbTask.Content = properties.pose.Position.Y.ToString("0");

                    smallCircle.Set(cPoint, new Point(0, 0), new Point(Radius_S, Radius_S));

                    Point ccR = CenterOnLineCv(Center_R);
                    redCircle.Set(ccR, new Point(0, 0), new Point(Radius_R, Radius_R));

                    Point ccB = CenterOnLineCv(Center_B);
                    blueCircle.Set(ccB, new Point(0, 0), new Point(Radius_B, Radius_B));

                    Point ccG = CenterOnLineCv(Center_G);
                    greenCircle.Set(ccG, new Point(0, 0), new Point(Radius_G, Radius_G));

                    Point ccO = CenterOnLineCv(Center_O);
                    orangeCircle.Set(ccO, new Point(0, 0), new Point(Radius_O, Radius_O));
                }));
            }
            catch { }

        }

        public override void UpdateProperties(PropertiesRobotUnity proR)
        {

            base.UpdateProperties(proR);
            DfL1 = proR.L1;
            DfL2 = proR.L2;
            DfWS = proR.WS;
            DfDistanceInter = proR.DistInter;

            DfL1Cv = proR.L1 * properties.Scale;
            DfL2Cv = proR.L2 * properties.Scale;
            DfWSCv = proR.WS * properties.Scale;
            DfDistInterCv = proR.DistInter * properties.Scale;

            L1Cv = proR.L1 * properties.Scale;
            L2Cv = proR.L2 * properties.Scale;
            WSCv = proR.WS * properties.Scale;
            DistInterCv = proR.DistInter * properties.Scale;
            //Draw ();

        }
    }
}
