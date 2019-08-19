﻿using Newtonsoft.Json;
using SeldatMRMS.Communication;
using SeldatUnilever_Ver1._02.Management.McuCom;
using SeldatUnilever_Ver1._02.Management.RobotManagent;
using SelDatUnilever_Ver1._00.Management;
using SelDatUnilever_Ver1._00.Management.ChargerCtrl;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using WebSocketSharp;
using static SeldatMRMS.Management.RobotManagent.RobotUnity;

namespace SeldatMRMS.Management.RobotManagent
{
    public class RobotUnityControl : RosSocket
    {
        public McuCtrl mcuCtrl;
        public event Action<int> FinishStatesCallBack;
        public event Action<int> LineEnableCallBack;
        public event Action<Pose, Object> PoseHandler;
        public event Action<Object, ConnectionStatus> ConnectionStatusHandler;
        private Timer timerCheckKeepAlive;
        public RobotLogOut robotLogOut;
        public bool onFlagDetectLine = false;
        public bool onFlagGoBackReady = false;
        public bool onFlagReadyGo = false;
        private const float delBatterry = 0;
        RobotSpeedLevel regRobotSpeed;
        public enum ResponseCtrl
        {
            RESPONSE_NONE = 0,
            RESPONSE_POS_PALLET = 3216,
            RESPONSE_AREA_PALLET = 3217,
            RESPONSE_ROBOT_NAVIGATION = 3218,
            RESPONSE_LINE_CTRL = 3219
        }
        public class Pose
        {
            public Pose(Point p, double Angle) // Angle gốc
            {
                this.Position = p;
                this.AngleW = Angle * Math.PI / 180.0;
                this.Angle = Angle;
            }
            public Pose(double X, double Y, double Angle) // Angle gốc
            {
                this.Position = new Point(X, Y);
                this.AngleW = Angle * Math.PI / 180.0;
                this.Angle = Angle;
            }
            public Pose() { }
            public void Destroy() // hủy vị trí robot để robot khác có thể làm việc trong quá trình detect
            {
                //this.Position = new Point (-1000, -1000);
                //this.AngleW = 0;
            }
            public Point Position { get; set; }
            public double VFbx { get; set; }
            public double VFby { get; set; }
            public double VFbw { get; set; }

            public double VCtrlx { get; set; }
            public double VCtrly { get; set; }
            public double VCtrlw { get; set; }

            public double AngleW { get; set; } // radian
            public double Angle { get; set; } // radian
            public int bayId;
        }
        public enum RobotSpeedLevel
        {
            ROBOT_SPEED_NORMAL = 2,
            ROBOT_SPEED_SLOW = 1,
            ROBOT_SPEED_STOP = 0,
        }
        public bool getBattery()
        {
            return properties.RequestChargeBattery;
        }

        public class PropertiesRobotUnity : NotifyUIBase
        {
            [CategoryAttribute("ID Settings"), DescriptionAttribute("Name of the customer")]
            private String _NameId;
            public String NameId { get => _NameId; set { _NameId = value; RaisePropertyChanged("NameID"); } }
            private String _Label;
            public String Label { get => _Label; set { _Label = value; RaisePropertyChanged("Label"); } }
            private String _Url { get; set; }
            public String Url { get => _Url; set { _Url = value; RaisePropertyChanged("Url"); } }
            public Pose pose = new Pose();
            public Pose poseRoot = new Pose();
            public String URL;
            public bool IsConnected { get; set; }
            private double _L1 { get; set; }
            private double _L2 { get; set; }
            private double _WS { get; set; }
            private double _Width { get; set; }
            private double _Length { get; set; }
            private double _Height { get; set; }
            [CategoryAttribute("Laser"), DescriptionAttribute("Name of the customer")]
            public String LaserOperation;
            [CategoryAttribute("Battery"), DescriptionAttribute("Name of the customer")]
            private float _BatteryLevelRb;
            public float BatteryLevelRb { get => _BatteryLevelRb; set { _BatteryLevelRb = value; RaisePropertyChanged("BatteryLevelRb"); } }
            private float _BatteryLowLevel;
            public float BatteryLowLevel { get => _BatteryLowLevel; set { _BatteryLowLevel = value; RaisePropertyChanged("BatteryLowLevel"); } }
            public bool RequestChargeBattery;
            private ChargerCtrl.ChargerId _ChargeID;
            public ChargerCtrl.ChargerId ChargeID { get => _ChargeID; set { _ChargeID = value; RaisePropertyChanged("ChargeID"); } }
            private double _DistanceIntersection;
            public double DistInter { get => _DistanceIntersection; set { _DistanceIntersection = value; RaisePropertyChanged("Distance Intersection"); } }
            public double L1 { get => _L1; set { _L1 = value; RaisePropertyChanged("L1"); } }
            public double L2 { get => _L2; set { _L2 = value; RaisePropertyChanged("L2"); } }
            public double WS { get => _WS; set { _WS = value; RaisePropertyChanged("WS"); } }


            private double _Scale { get; set; }
            public double Scale { get => _Scale; set { _Scale = value; RaisePropertyChanged("Scale"); } }

            public double Width { get => _Width; set { _Width = value; RaisePropertyChanged("Width"); } }
            public double Length { get => _Length; set { _Length = value; RaisePropertyChanged("Length"); } }
            public double Height { get => _Height; set { _Height = value; RaisePropertyChanged("Height"); } }
            public String problemContent;
            public String solvedProblemContent;
            public String detailInfo;
            private String _ipMcuCtrl;
            public String ipMcuCtrl { get => _ipMcuCtrl; set { _ipMcuCtrl = value; RaisePropertyChanged("IpMCU"); } }
            private int _portMcuCtrl;
            public int portMcuCtrl { get => _portMcuCtrl; set { _portMcuCtrl = value; RaisePropertyChanged("PortMCU"); } }
            public String speedInSpecicalArea = "ROBOT_SPEED_NORMAL";
            public double errorVx = 0.0001;
            public double errorVy = 0.0001;
            public double errorW = 0.0001;
            public double errorDx = 0.5;
            public double errorDy = 0.5;
            public Point goalPoint;

        }

        public enum RequestCommandLineDetect
        {
            REQUEST_CHARGECTRL_CANCEL = 1201,
            REQUEST_LINEDETECT_PALLETUP = 1203,
            REQUEST_LINEDETECT_PALLETDOWN = 1204,
            REQUEST_LINEDETECT_GETIN_CHARGER = 1206,
            REQUEST_LINEDETECT_GETOUT_CHARGER = 1207,
            REQUEST_LINEDETECT_READYAREA = 1208,
        }

        public enum RequestCommandPosPallet
        {

            REQUEST_LINEDETECT_COMING_POSITION = 1205,
            REQUEST_TURN_LEFT = 1210,
            REQUEST_TURN_RIGHT = 1211,
            REQUEST_FORWARD_DIRECTION = 1212,
            // REQUEST_GOBACK_FRONTLINE = 1213,
            REQUEST_GOBACK_FRONTLINE_TURN_RIGHT = 1213,
            REQUEST_TURNOFF_PC = 1214,
            REQUEST_DROPDOWN_PALLET = 1216,
            REQUEST_GOBACK_FRONTLINE_TURN_LEFT = 1217,
            //REQUEST_GOBACK_FRONTLINE_TURN_RIGHT =
        }

        public enum ResponseCommand
        {
            RESPONSE_NONE = 0,
            RESPONSE_START_DETECT_LINE = 1000,
            RESPONSE_LASER_CAME_POINT = 2000,
            RESPONSE_LINEDETECT_PALLETUP = 3203,
            RESPONSE_LINEDETECT_PALLETDOWN = 3204,
            RESPONSE_FINISH_GOTO_POSITION = 3205,
            RESPONSE_FINISH_DETECTLINE_GETIN_CHARGER = 3206,
            RESPONSE_FINISH_DETECTLINE_GETOUT_CHARGER = 3207,
            RESPONSE_FINISH_TURN_LEFT = 3210,
            RESPONSE_FINISH_TURN_RIGHT = 3211,
            RESPONSE_FINISH_GOBACK_FRONTLINE = 3213,
            RESPONSE_ERROR = 3215,
            RESPONSE_FINISH_DROPDOWN_PALLET = 3216,

        }

        public virtual void updateparams() { }
        public virtual void OnOccurencyTrigger() { }
        public virtual void OnBatteryLowTrigger() { }
        public struct ParamsRosSocket
        {
            public int publication_RobotInfo;
            public int publication_RobotParams;
            public int publication_ServerRobotCtrl;
            public int publication_CtrlRobotHardware;
            public int publication_DriveRobot;
            public int publication_BatteryRegister;
            public int publication_EmergencyRobot;
            public int publication_ctrlrobotdriving;
            public int publication_robotnavigation;
            public int publication_killActionLid;
            public int publication_linedetectionctrl;
            public int publication_checkAliveTimeOut;
            public int publication_postPallet;
            public int publication_finishStatesCallBack;
            public int publication_cmdAreaPallet;
            public int publication_killpid;
            public int publication_liftCtrl;

        }

        ParamsRosSocket paramsRosSocket;

        private const UInt32 TIME_OUT_WAT_RESPONSE = 10000;
        private const UInt32 NUM_TRY_SEND_TO_RB = 1000;
        private UInt32 numResendData = 0;
        private bool waitRes = false;
        private ResponseCtrl rbResCtrl = new ResponseCtrl();

        public PropertiesRobotUnity properties = new PropertiesRobotUnity();
        protected virtual void SupervisorTraffic() { }
        public RobotUnityControl()
        {
            timerCheckKeepAlive = new Timer();
            timerCheckKeepAlive.Interval = 250;
            timerCheckKeepAlive.Elapsed += checkKeepAliveEvent;
            timerCheckKeepAlive.AutoReset = true;
            timerCheckKeepAlive.Enabled = true;
            robotLogOut = new RobotLogOut();
        }
        public virtual void setColorRobotStatus(RobotStatusColorCode rsc, RobotUnity robotTemp)
        {
            try
            {
                robotTemp.border.Dispatcher.Invoke(() =>
                {
                    switch (rsc)
                    {
                        case RobotStatusColorCode.ROBOT_STATUS_OK:
                            {

                                robotTemp.border.Background = new SolidColorBrush(Colors.Blue);
                                break;
                            }
                        case RobotStatusColorCode.ROBOT_STATUS_ERROR:
                            {
                                robotTemp.border.Background = new SolidColorBrush(Colors.Red);
                                break;
                            }
                        case RobotStatusColorCode.ROBOT_STATUS_RUNNING:
                            {
                                robotTemp.border.Background = new SolidColorBrush(Colors.Green);
                                break;
                            }
                        case RobotStatusColorCode.ROBOT_STATUS_WAIT_FIX:
                            {
                                robotTemp.border.Background = new SolidColorBrush(Colors.Yellow);
                                break;
                            }
                        case RobotStatusColorCode.ROBOT_STATUS_DISCONNECT:
                            {
                                robotTemp.border.Background = new SolidColorBrush(Colors.Orange);
                                break;
                            }
                        case RobotStatusColorCode.ROBOT_STATUS_RECONNECT:
                            {
                                robotTemp.border.Background = new SolidColorBrush(Colors.Yellow);
                                break;
                            }
                        case RobotStatusColorCode.ROBOT_STATUS_CONNECT:
                            {
                                robotTemp.border.Background = new SolidColorBrush(Colors.Blue);
                                break;
                            }
                        case RobotStatusColorCode.ROBOT_STATUS_CAN_NOTGET_DATA:
                            {
                                robotTemp.border.Background = new SolidColorBrush(Colors.YellowGreen);
                                break;
                            }
                        case RobotStatusColorCode.ROBOT_STATUS_CHARGING:
                            {
                                robotTemp.border.Background = new SolidColorBrush(Colors.LightGreen);
                                break;
                            }
                    }
                });
            }
            catch { }
        }
        private void checkKeepAliveEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            StandardInt32 msg = new StandardInt32();
            msg.data = 1234;
            this.Publish(paramsRosSocket.publication_checkAliveTimeOut, msg);
        }
        public void createRosTerms()
        {
            int subscription_robotInfo = this.Subscribe("/amcl_pose", "geometry_msgs/PoseWithCovarianceStamped", AmclPoseHandler, 10);
            paramsRosSocket.publication_ctrlrobotdriving = this.Advertise("/ctrlRobotDriving", "std_msgs/Int32");
            int subscription_finishedStates = this.Subscribe("/finishedStates", "std_msgs/Int32", FinishedStatesHandler, 10);
            paramsRosSocket.publication_checkAliveTimeOut = this.Advertise("/checkAliveTimeOut", "std_msgs/Int32");

            int subscription_respCtrlCallBack = this.Subscribe("/respCtrl", "std_msgs/Int32", ResponseCtrlHandler, 10);
            paramsRosSocket.publication_linedetectionctrl = this.Advertise("/linedetectionctrl", "std_msgs/Int32");
            paramsRosSocket.publication_postPallet = this.Advertise("/pospallet", "std_msgs/Int32");
            paramsRosSocket.publication_finishStatesCallBack = this.Advertise("/finishStatesCallBack", "std_msgs/Int32");
            paramsRosSocket.publication_liftCtrl = this.Advertise("/lift_control", "std_msgs/String");
            paramsRosSocket.publication_cmdAreaPallet = this.Advertise("/cmdAreaPallet", "std_msgs/String");
            paramsRosSocket.publication_robotnavigation = this.Advertise("/robot_navigation", "geometry_msgs/PoseStamped");

            paramsRosSocket.publication_killpid = this.Advertise("/key_press", "std_msgs/String");
            paramsRosSocket.publication_killActionLid = this.Advertise("/killActionLibCallback", "std_msgs/Int32");
            float subscription_publication_batteryvol = this.Subscribe("/battery_vol", "std_msgs/Int32", BatteryVolHandler);
            int subscription_Odom = this.Subscribe("/odom", "nav_msgs/Odometry", OdometryCallback, 10);
            int subscription_Navi = this.Subscribe("/cmd_vel_mux/input/navi", "geometry_msgs/Twist", NaviCallback, 10);
            int subscription_lineEnable = this.Subscribe("/line_enable", "std_msgs/Int32", LineEnableHandler);
            float subscription_RequestGotoReady = this.Subscribe("/requestGotoReady", "std_msgs/Int32", RequestGotoReadyHandler);
        }

        private void CheckRbRes(ResponseCtrl res)
        {
            if (this.rbResCtrl == res)
            {
                this.waitRes = false;
            }
        }
        private void PubToRb(int topic, object cmd, ResponseCtrl cmdRes)
        {
            this.rbResCtrl = cmdRes;
            switch (cmdRes)
            {
                case ResponseCtrl.RESPONSE_POS_PALLET:
                    this.Publish(topic, (StandardInt32)cmd);
                    break;
                case ResponseCtrl.RESPONSE_AREA_PALLET:
                    this.Publish(topic, (StandardString)cmd);
                    break;
                case ResponseCtrl.RESPONSE_ROBOT_NAVIGATION:
                    this.Publish(topic, (GeometryPoseStamped)cmd);
                    break;
                case ResponseCtrl.RESPONSE_LINE_CTRL:
                    this.Publish(topic, (StandardInt32)cmd);
                    break;
            }
        }
        private bool SendToRb(int topic, object cmd, ResponseCtrl cmdRes)
        {
            bool ret = true;
            PubToRb(topic, cmd, cmdRes);
            this.waitRes = true;
            this.numResendData = 0;
            Stopwatch et = new Stopwatch();
            et.Start();
            while (waitRes)
            {
                if (et.ElapsedMilliseconds >= TIME_OUT_WAT_RESPONSE)
                {
                    PubToRb(topic, cmd, cmdRes);
                    et.Restart();
                    this.numResendData++;
                    robotLogOut.ShowText(this.properties.Label, "TIME_OUT_WAT_RESPONSE from RB, try time : " + this.numResendData);
                    if (this.numResendData >= NUM_TRY_SEND_TO_RB)
                    {
                        ret = false;
                        break;
                    }
                }
                System.Threading.Thread.Sleep(40);
            }
            return ret;
        }

        private void RequestGotoReadyHandler(Communication.Message message)
        {
            StandardInt32 rqVal = (StandardInt32)message;
            if (rqVal.data == 1)
            {
                Console.WriteLine("request goto ready");
            }
        }
        private void LineEnableHandler(Communication.Message message)
        {
            StandardInt32 data = (StandardInt32)message;
            try
            {
                LineEnableCallBack(data.data);
            }
            catch { }
        }
        public void LiftCtrlUp()
        {
            try
            {
                StandardString msg = new StandardString();
                msg.data = "lift_up";
                this.Publish(paramsRosSocket.publication_liftCtrl, msg);
            }
            catch { }
        }
        public void LiftCtrlDown()
        {
            try
            {
                StandardString msg = new StandardString();
                msg.data = "lift_down";
                this.Publish(paramsRosSocket.publication_liftCtrl, msg);
            }
            catch { }
        }
        private void BatteryVolHandler(Communication.Message message)
        {
            StandardInt32 batVal = (StandardInt32)message;
            properties.BatteryLevelRb = batVal.data;
            //robotLogOut.ShowText(this.properties.Label, "BatteryLevelRb[" + batVal.data + "]");
            if (properties.RequestChargeBattery == false)
            {
                if (properties.BatteryLevelRb <= properties.BatteryLowLevel)
                {
                    properties.RequestChargeBattery = true;
                    robotLogOut.ShowText(this.properties.Label, "RequestChargeBattery");
                }
            }
            else
            {
                if (properties.BatteryLevelRb > (properties.BatteryLowLevel + delBatterry))
                {
                    properties.RequestChargeBattery = false;
                }
            }
        }

        private void AmclPoseHandler(Communication.Message message)
        {
            try
            {
                GeometryPoseWithCovarianceStamped standardString = (GeometryPoseWithCovarianceStamped)message;
                double posX = (double)standardString.pose.pose.position.x;
                double posY = (double)standardString.pose.pose.position.y;
                double posThetaZ = (double)standardString.pose.pose.orientation.z;
                double posThetaW = (double)standardString.pose.pose.orientation.w;
                double posTheta = (double)2 * Math.Atan2(posThetaZ, posThetaW);
                properties.pose.Position = new Point(posX, posY);
                properties.pose.AngleW = posTheta;
                properties.pose.Angle = posTheta * 180 / Math.PI;
            }
            catch
            {
                Console.WriteLine(" Error in AMCL");
            }


        }
        private void FinishedStatesHandler(Communication.Message message)
        {
            try
            {
                StandardInt32 standard = (StandardInt32)message;
                robotLogOut.ShowText(this.properties.Label, "Finished State [" + standard.data + "]");
                //StandardInt32 cmd = new StandardInt32();
                //cmd.data = 0;
                //this.Publish(paramsRosSocket.publication_finishStatesCallBack, cmd);
                try
                {
                    FinishStatesCallBack(standard.data);
                }
                catch { }


            }
            catch
            {
                Console.WriteLine(" Error FinishedStatesHandler");
            }

        }
        private void ResponseCtrlHandler(Communication.Message message)
        {
            try
            {
                StandardInt32 standard = (StandardInt32)message;
                robotLogOut.ShowText(this.properties.Label, "ResponseCtrl [" + standard.data + "]");
                CheckRbRes((ResponseCtrl)standard.data);
            }
            catch
            {
                Console.WriteLine(" Error FinishedStatesHandler");
            }

        }

        private void OdometryCallback(Communication.Message message)
        {
            NavigationOdometry standard = (NavigationOdometry)message;
            properties.pose.VFbx = standard.twist.twist.linear.x;
            properties.pose.VFby = standard.twist.twist.linear.y;
            properties.pose.VFbw = standard.twist.twist.angular.z;

        }
        private void NaviCallback(Communication.Message message)
        {
            GeometryTwist standard = (GeometryTwist)message;
            properties.pose.VCtrlx = standard.linear.x;
            properties.pose.VCtrly = standard.linear.y;
            properties.pose.VCtrlw = standard.angular.z;
        }

        public void KillPID()
        {
            try
            {
                StandardString msg = new StandardString();
                msg.data = "stop pid";
                this.Publish(paramsRosSocket.publication_killpid, msg);
            }
            catch { MessageBox.Show("Kill PID error !"); }
        }
        // kill navigation
        public void KillActionLib()
        {
            try
            {
                StandardInt32 msg = new StandardInt32();
                msg.data = 0;
                this.Publish(paramsRosSocket.publication_killActionLid, msg);
            }
            catch { MessageBox.Show("Kill PID error !"); }
        }
        public virtual void UpdateProperties(PropertiesRobotUnity proR)
        {
            properties = proR;
        }
        double gx;
        double gy;
        public bool SendPoseStamped(Pose pose)
        {
            bool ret = false;
            try
            {
                if (pose != null)
                {
                    GeometryPoseStamped data = new GeometryPoseStamped();
                    data.header.frame_id = "map";
                    data.pose.position.x = (float)pose.Position.X;
                    data.pose.position.y = (float)pose.Position.Y;
                    data.pose.position.z = 0;
                    double theta = pose.AngleW;
                    data.pose.orientation.z = (float)Math.Sin(theta / 2);
                    data.pose.orientation.w = (float)Math.Cos(theta / 2);

                    Console.WriteLine(this.properties.Label, "Send Pose => " + JsonConvert.SerializeObject(data).ToString());

                    //this.Publish(paramsRosSocket.publication_robotnavigation, data);
                    robotLogOut.ShowText(this.properties.Label, "Send Pose => " + JsonConvert.SerializeObject(data).ToString());
                    ret = SendToRb(paramsRosSocket.publication_robotnavigation,data, ResponseCtrl.RESPONSE_ROBOT_NAVIGATION);
                    // lưu vị trí đích đến
                    gx = data.pose.position.x;
                    gy = data.pose.position.y;
                }
                else
                {
                    robotLogOut.ShowText(this.properties.Label, "Without Data SendPoseStamped : pose is Null");
                    return false;
                }
            }
            catch
            {
                robotLogOut.ShowText(this.properties.Label, "Robot Control Error SendPoseStamped");
                return false;
            }
            return ret;
        }
        bool flagSpeedTraffic = false;
        bool flagSpeedRegZone = false;
        bool flagSetSpeedHighPrioprity = false;
        bool flagSetCtrlSpeed = true;

        public bool SetSpeedCtrl(RobotSpeedLevel robotspeed, bool highpriority)
        {

            flagSetCtrlSpeed = highpriority;
            if (flagSpeedRegZone || flagSpeedTraffic || flagSetSpeedHighPrioprity || flagSetCtrlSpeed)
            {
                try
                {
                    properties.speedInSpecicalArea = robotspeed + "_HIGH_PRIORITY";
                    StandardInt32 msg = new StandardInt32();
                    msg.data = Convert.ToInt32(RobotSpeedLevel.ROBOT_SPEED_STOP);
                    this.Publish(paramsRosSocket.publication_ctrlrobotdriving, msg);
                    delay(700);
                    return true;
                }
                catch
                {
                    Console.WriteLine("Robot Control Error  SetSpeed");
                    return false;
                }
            }
            else
            {
                try
                {
                    properties.speedInSpecicalArea = robotspeed + "_HIGH_PRIORITY";
                    StandardInt32 msg = new StandardInt32();
                    msg.data = Convert.ToInt32(RobotSpeedLevel.ROBOT_SPEED_NORMAL);
                    this.Publish(paramsRosSocket.publication_ctrlrobotdriving, msg);
                    delay(700);
                    return true;
                }
                catch
                {
                    Console.WriteLine("Robot Control Error  SetSpeed");
                    return false;
                }
            }
        }

        public bool SetSpeedHighPrioprity(RobotSpeedLevel robotspeed, bool highpriority)
        {

            flagSetSpeedHighPrioprity = highpriority;
            if (flagSpeedRegZone || flagSpeedTraffic || flagSetSpeedHighPrioprity)
            {
                try
                {
                    properties.speedInSpecicalArea = robotspeed + "_HIGH_PRIORITY";
                    StandardInt32 msg = new StandardInt32();
                    msg.data = Convert.ToInt32(RobotSpeedLevel.ROBOT_SPEED_STOP);
                    this.Publish(paramsRosSocket.publication_ctrlrobotdriving, msg);
                    delay(700);
                    return true;
                }
                catch
                {
                    Console.WriteLine("Robot Control Error  SetSpeed");
                    return false;
                }
            }
            else
            {
                try
                {
                    properties.speedInSpecicalArea = robotspeed + "_HIGH_PRIORITY";
                    StandardInt32 msg = new StandardInt32();
                    msg.data = Convert.ToInt32(RobotSpeedLevel.ROBOT_SPEED_NORMAL);
                    this.Publish(paramsRosSocket.publication_ctrlrobotdriving, msg);
                    delay(700);
                    return true;
                }
                catch
                {
                    Console.WriteLine("Robot Control Error  SetSpeed");
                    return false;
                }
            }
        }

        public bool SetSpeedTraffic(RobotSpeedLevel robotspeed, bool highpriority)
        {
            flagSpeedTraffic = highpriority;
            if (flagSpeedRegZone || flagSpeedTraffic || flagSetSpeedHighPrioprity)
            {
                try
                {
                    properties.speedInSpecicalArea = robotspeed + "_TRAFFIC";
                    StandardInt32 msg = new StandardInt32();
                    msg.data = Convert.ToInt32(RobotSpeedLevel.ROBOT_SPEED_STOP);
                    this.Publish(paramsRosSocket.publication_ctrlrobotdriving, msg);
                    delay(700);
                    return true;
                }
                catch
                {
                    Console.WriteLine("Robot Control Error  SetSpeed");
                    return false;
                }
            }
            else
            {
                try
                {
                    properties.speedInSpecicalArea = robotspeed + "_TRAFFIC";
                    StandardInt32 msg = new StandardInt32();
                    msg.data = Convert.ToInt32(RobotSpeedLevel.ROBOT_SPEED_NORMAL);
                    this.Publish(paramsRosSocket.publication_ctrlrobotdriving, msg);
                    delay(700);
                    return true;
                }
                catch
                {
                    Console.WriteLine("Robot Control Error  SetSpeed");
                    return false;
                }
            }
        }
        public void delay(int ms)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (true)
            {
                if (sw.ElapsedMilliseconds > ms) break;
            }
        }
        public bool SetSpeedRegZone(RobotSpeedLevel robotspeed, bool highpriority)
        {
            flagSpeedRegZone = highpriority;
            if (flagSpeedRegZone || flagSpeedTraffic || flagSetSpeedHighPrioprity)
            {
                try
                {
                    properties.speedInSpecicalArea = robotspeed + "_REG_ZONE";
                    StandardInt32 msg = new StandardInt32();
                    msg.data = Convert.ToInt32(RobotSpeedLevel.ROBOT_SPEED_STOP);

                    this.Publish(paramsRosSocket.publication_ctrlrobotdriving, msg);
                    delay(700);
                    return true;
                }
                catch
                {
                    Console.WriteLine("Robot Control Error  SetSpeed");
                    return false;
                }
            }
            else
            {
                try
                {
                    properties.speedInSpecicalArea = robotspeed + "_REG_ZONE";
                    StandardInt32 msg = new StandardInt32();
                    msg.data = Convert.ToInt32(RobotSpeedLevel.ROBOT_SPEED_NORMAL);

                    this.Publish(paramsRosSocket.publication_ctrlrobotdriving, msg);
                    delay(700);
                    return true;
                }
                catch
                {
                    Console.WriteLine("Robot Control Error  SetSpeed");
                    return false;
                }
            }
        }

        public bool SendCmdLineDetectionCtrl(RequestCommandLineDetect cmd)
        {
            bool ret = false;
            try
            {
                StandardInt32 msg = new StandardInt32();
                msg.data = Convert.ToInt32(cmd);
                //this.Publish(paramsRosSocket.publication_linedetectionctrl, msg);
                robotLogOut.ShowText(this.properties.Label, "SendCmdLineDetectionCtrl => " + msg.data);
                ret = SendToRb(paramsRosSocket.publication_linedetectionctrl, msg, ResponseCtrl.RESPONSE_LINE_CTRL);
            }
            catch
            {
                Console.WriteLine("Robot Control Error SendCmdLineDetectionCtrl");
                return false;
            }
            return ret;
        }

        public bool SendCmdPosPallet(RequestCommandPosPallet cmd)
        {
            bool ret = false;
            try
            {
                StandardInt32 msg = new StandardInt32();
                msg.data = Convert.ToInt32(cmd);
                //this.Publish(paramsRosSocket.publication_postPallet, msg);
                robotLogOut.ShowText(this.properties.Label, "SendCmdPosPallet => " + msg.data);
                ret = SendToRb(paramsRosSocket.publication_postPallet, msg, ResponseCtrl.RESPONSE_POS_PALLET);
            }
            catch
            {
                Console.WriteLine("Robot Control Error SendCmdPosPallet");
                return false;
            }
            return ret;
        }

        int countGoal = 0;
        public bool ReachedGoal()
        {
            if (countGoal++ > 200)
            {
                countGoal = 0;

                double _currentgoal_Ex = Math.Abs(properties.pose.Position.X - gx);
                double _currentgoal_Ey = Math.Abs(properties.pose.Position.Y - gy);
                /* */
                double gxx = Math.Abs(gx);
                double gyy = Math.Abs(gy);
                if (gxx >= 7.0 && gxx <= 8.65 && gyy >= 9.8 && gyy <= 11.0) // truong hop dat biet
                {
                    //robotLogOut.ShowText("", "Truong hop dat biet");

                    // if (_currentgoal_Ex <= properties.errorDx && _currentgoal_Ey <= 5.5 && _currentgoal_Ex >= 0 && _currentgoal_Ey >= 0)
                    if (_currentgoal_Ex <= 2.0 && _currentgoal_Ey <= 5.5 && _currentgoal_Ex >= 0 && _currentgoal_Ey >= 0)
                    {
                        if (Math.Abs(properties.pose.VFbx) < properties.errorVx)
                        {
                            properties.pose.VCtrlx = 0;
                            properties.pose.VCtrly = 0;
                            properties.pose.VCtrlw = 0;
                            robotLogOut.ShowText("", "------------------------------  " + this.properties.NameId);
                            robotLogOut.ShowText("", "Goal X=" + gx);
                            robotLogOut.ShowText("", "Goal Y=" + gy);
                            robotLogOut.ShowText("", "Current amcl X=" + properties.pose.Position.X);
                            robotLogOut.ShowText("", "Current amcl Y=" + properties.pose.Position.Y);
                            robotLogOut.ShowText("", "Error amcl X=" + _currentgoal_Ex);
                            robotLogOut.ShowText("", "Error amcl Y=" + _currentgoal_Ey);
                            robotLogOut.ShowText("", "VX=" + properties.pose.VFbx);
                            robotLogOut.ShowText("", "VY=" + properties.pose.VFby);
                            robotLogOut.ShowText("", "REACHED GOAL");
                            return true;
                        }
                    }
                }
                else
                {
                    //&& Math.Abs(properties.pose.VCtrlx) <= 0.01 && Math.Abs(properties.pose.VCtrlw) <= 0.01


                    if (_currentgoal_Ex <= properties.errorDx && _currentgoal_Ey <= properties.errorDy && _currentgoal_Ex >= 0.0 && _currentgoal_Ey >= 0.0)
                    {
                        if (Math.Abs(properties.pose.VFbx) < properties.errorVx)
                        {
                            properties.pose.VCtrlx = 0;
                            properties.pose.VCtrly = 0;
                            properties.pose.VCtrlw = 0;
                            robotLogOut.ShowText("", "------------------------------  " + this.properties.NameId);
                            robotLogOut.ShowText("", "Goal X=" + gx);
                            robotLogOut.ShowText("", "Goal Y=" + gy);
                            robotLogOut.ShowText("", "Current amcl X=" + properties.pose.Position.X);
                            robotLogOut.ShowText("", "Current amcl Y=" + properties.pose.Position.Y);
                            robotLogOut.ShowText("", "Error amcl X=" + _currentgoal_Ex);
                            robotLogOut.ShowText("", "Error amcl Y=" + _currentgoal_Ey);
                            robotLogOut.ShowText("", "VX=" + properties.pose.VFbx);
                            robotLogOut.ShowText("", "VY=" + properties.pose.VFby);
                            robotLogOut.ShowText("", "REACHED GOAL");
                            return true;
                        }
                    }
                }
            }

            return false;
        }
        public bool SendCmdAreaPallet(String cmd)
        {
            bool ret = false;
            try
            {
                StandardString msg = new StandardString();
                msg.data = cmd;
                Console.WriteLine(cmd);
                //this.Publish(paramsRosSocket.publication_cmdAreaPallet, msg);
                robotLogOut.ShowText(this.properties.Label, "SendCmdAreaPallet => " + msg.data);
                ret = SendToRb(paramsRosSocket.publication_cmdAreaPallet, msg, ResponseCtrl.RESPONSE_AREA_PALLET);
            }
            catch
            {
                Console.WriteLine("Error Send SendCmdAreaPallet");
                return false;
            }
            return ret;
        }

        protected override void OnOpenedEvent()
        {
            properties.IsConnected = true;

            robotLogOut.ShowText(this.properties.Label, "Connected to Ros Master");

            try
            {
                createRosTerms();
                Draw();
            }
            catch
            {
                Console.WriteLine("Robot Control Error Send OnOpenedEvent");
            }

            //   ConnectionStatusHandler(this, ConnectionStatus.CON_OK);
        }

        protected override void OnClosedEvent(object sender, CloseEventArgs e)
        {
            //ConnectionStatusHandler(this, ConnectionStatus.CON_FAILED);
            //  robotLogOut.ShowText(this.properties.Label,  "Disconnected to Ros Master");
            //  robotLogOut.ShowText(this.properties.Label,  "Reconnecting...");
            properties.IsConnected = false;
            this.url = properties.URL;
            base.OnClosedEvent(sender, e);

        }

        public override void Dispose()
        {
            robotLogOut.ShowText(this.properties.Label, "Disconnected to Ros Master");
            properties.pose.Destroy();
            base.Dispose();
        }
        public virtual void Draw() { }
        public virtual void TrafficUpdate() { }
    }
}
