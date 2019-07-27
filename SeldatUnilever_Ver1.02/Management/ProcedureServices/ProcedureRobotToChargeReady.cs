using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using SeldatMRMS.Management.RobotManagent;
using SeldatMRMS.Management.TrafficManager;
using SeldatUnilever_Ver1._02.Management.ProcedureServices;
using SeldatUnilever_Ver1._02.Management.TrafficManager;
using SelDatUnilever_Ver1._00.Management.ChargerCtrl;
using SelDatUnilever_Ver1._00.Management.DeviceManagement;
using static DoorControllerService.DoorService;
using static SeldatMRMS.Management.RobotManagent.RobotBaseService;
using static SeldatMRMS.Management.RobotManagent.RobotUnity;
using static SeldatMRMS.Management.RobotManagent.RobotUnityControl;
using static SeldatMRMS.Management.TrafficRobotUnity;
using static SelDatUnilever_Ver1._00.Management.ChargerCtrl.ChargerCtrl;
using static SelDatUnilever_Ver1._00.Management.ComSocket.RouterComPort;
using static SelDatUnilever_Ver1._00.Management.DeviceManagement.DeviceItem;
using static SelDatUnilever_Ver1._00.Management.TrafficManager.TrafficRounterService;

namespace SeldatMRMS
{
    public class ProcedureRobotToCharger : ProcedureControlServices
    {

        Thread ProRobotToCharger;
        public RobotUnity robot;
        ResponseCommand resCmd;
        RobotGoToCharge StateRobotToCharge;
        public ChargerCtrl chargerCtrl;
        DataReceive batLevel;
        DataReceive statusCharger;
        Stopwatch sw = new Stopwatch();
        const bool USE_MANUAL_CHARGE = true;
        const UInt32 TIME_OUT_WAIT_TURNOFF_PC = 60000 * 5;
        const UInt32 TIME_OUT_WAIT_STATE = 60000;
        const UInt32 TIME_OUT_ROBOT_RECONNECT_SERVER = 60000 * 10;
        const UInt32 TIME_COUNT_GET_BAT_LEVEL = 1000;
        const UInt32 TIME_DELAY_RELEASE_CHARGE = 60000 * 5;
        const UInt32 BATTERY_FULL_LEVEL = 99; /*Never battery full 100%*/
        const UInt32 BATTERY_NEW_BAT = 20; /*Never battery full 100%*/
        private UInt32 timeCountGetBatLevel = 0;
        public override event Action<Object> ReleaseProcedureHandler;
        // public override event Action<Object> ErrorProcedureHandler;
        public byte getBatteryLevel()
        {
            return batLevel.data[0];
        }
        public byte getStatusCharger()
        {
            return statusCharger.data[0];
        }
        public ProcedureRobotToCharger(RobotUnity robot, ChargerManagementService charger, ChargerId id) : base(robot)
        {
            StateRobotToCharge = RobotGoToCharge.ROBCHAR_IDLE;
            batLevel = new DataReceive();
            statusCharger = new DataReceive();
            this.robot = robot;
            chargerCtrl = charger.ChargerStationList[id];
            procedureCode = ProcedureCode.PROC_CODE_ROBOT_TO_CHARGE;
        }

#if USE_MANUAL_CHARGE
        public void Start(RobotGoToCharge state = RobotGoToCharge.ROBCHAR_ROBOT_GOTO_CHARGER)
#else
        public void Start(RobotGoToCharge state = RobotGoToCharge.ROBCHAR_WAITTING_CHARGEBATTERY)
#endif
        {
            order = new OrderItem();
            order.typeReq = TyeRequest.TYPEREQUEST_CHARGE;
            robot.robotTag = RobotStatus.CHARGING;
            errorCode = ErrorCode.RUN_OK;
            robot.ProcedureAs = ProcedureControlAssign.PRO_CHARGE;
            StateRobotToCharge = state;
            ProRobotToCharger = new Thread(this.Procedure);
            ProRobotToCharger.Start(this);
            procedureCode = ProcedureCode.PROC_CODE_ROBOT_TO_CHARGE;
            ProRun = true;
            //robot.prioritLevel.OnAuthorizedPriorityProcedure = false;
        }
        public void Destroy()
        {
            // StateRobotToCharge = RobotGoToCharge.ROBCHAR_ROBOT_RELEASED;
            robot.robotTag = RobotStatus.IDLE;
            //robot.prioritLevel.OnAuthorizedPriorityProcedure = false;
            ProRun = false;
            UpdateInformationInProc(this, ProcessStatus.F);
            selectHandleError = SelectHandleError.CASE_ERROR_EXIT;
            order.endTimeProcedure = DateTime.Now;
            order.totalTimeProcedure = order.endTimeProcedure.Subtract(order.startTimeProcedure).TotalMinutes;
            SaveOrderItem(order);
            //   this.robot.DestroyRegistrySolvedForm();
        }
        public void Procedure(object ojb)
        {
            ProcedureRobotToCharger RbToChar = (ProcedureRobotToCharger)ojb;
            RobotUnity rb = RbToChar.robot;
            ErrorCodeCharger result;
            robot.ShowText(" Start -> " + procedureCode);
            while (ProRun)
            {
                switch (StateRobotToCharge)
                {
                    case RobotGoToCharge.ROBCHAR_IDLE:
                        //robot.ShowText("ROBCHAR_IDLE");
                        break;
#if USE_MANUAL_CHARGE
                    // case RobotGoToCharge.ROBCHAR_CHARGER_CHECKSTATUS:
                    //     if(true == chargerCtrl.WaitState(ChargerState.ST_READY,TIME_OUT_WAIT_STATE)){
                    //         StateRobotToCharge = RobotGoToCharge.ROBCHAR_ROBOT_ALLOW_CUTOFF_POWER_ROBOT;
                    //     }
                    //     break; //kiểm tra kết nối và trạng thái sạc
                    case RobotGoToCharge.ROBCHAR_ROBOT_GOTO_CHARGER:
                        //robot.ShowText("ROBCHAR_ROBOT_GOTO_CHARGER");
                        robot.TurnOnSupervisorTraffic(false);
                        if (rb.SendCmdLineDetectionCtrl(RequestCommandLineDetect.REQUEST_LINEDETECT_GETIN_CHARGER))
                        {
                            //Thread.Sleep(1000);
                            StateRobotToCharge = RobotGoToCharge.ROBCHAR_ROBOT_ALLOW_CUTOFF_POWER_ROBOT;
                            //robot.ShowText("ROBCHAR_ROBOT_ALLOW_CUTOFF_POWER_ROBOT");
                        }
                        break;
                    case RobotGoToCharge.ROBCHAR_ROBOT_ALLOW_CUTOFF_POWER_ROBOT:
                        try
                        {
                            if (resCmd == ResponseCommand.RESPONSE_FINISH_DETECTLINE_GETIN_CHARGER)
                            {
                                if (rb.SendCmdPosPallet(RequestCommandPosPallet.REQUEST_TURNOFF_PC))
                                {
                                    rb.Dispose();
                                    StateRobotToCharge = RobotGoToCharge.ROBCHAR_ROBOT_WAITTING_CUTOFF_POWER_PC;
                                    sw.Start();
                                    //robot.ShowText("ROBCHAR_ROBOT_WAITTING_CUTOFF_POWER_PC");
                                }
                            }
                            else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                            {
                                errorCode = ErrorCode.DETECT_LINE_CHARGER_ERROR;
                                CheckUserHandleError(this);
                            }
                        }
                        catch (System.Exception)
                        {
                            errorCode = ErrorCode.CONNECT_CHARGER_ERROR;
                            CheckUserHandleError(this);
                        }
                        break; //cho phép cắt nguồn robot
                    case RobotGoToCharge.ROBCHAR_ROBOT_WAITTING_CUTOFF_POWER_PC:
                        if (true != rb.properties.IsConnected)
                        {
                            rb.Dispose();
                            //robot.ShowText("Sleep 30s waitting turnoff power");
                            Thread.Sleep(30000);
                            StateRobotToCharge = RobotGoToCharge.ROBCHAR_ROBOT_START_CHARGE;
                            sw.Stop();
                            //robot.ShowText("ROBCHAR_ROBOT_START_CHARGE");
                        }
                        else
                        {
                            if (sw.ElapsedMilliseconds > TIME_OUT_WAIT_TURNOFF_PC)
                            {
                                sw.Stop();
                                errorCode = ErrorCode.CAN_NOT_TURN_OFF_PC;
                                CheckUserHandleError(this);
                                StateRobotToCharge = RobotGoToCharge.ROBCHAR_ROBOT_ALLOW_CUTOFF_POWER_ROBOT;
                            }
                        }
                        break;

                    case RobotGoToCharge.ROBCHAR_ROBOT_START_CHARGE:
                        try
                        {
                            if (true == chargerCtrl.StartCharge())
                            {
                                StateRobotToCharge = RobotGoToCharge.ROBCHAR_WAITTING_ROBOT_CONTACT_CHARGER;
                                //robot.ShowText("ROBCHAR_WAITTING_ROBOT_CONTACT_CHARGER");
                            }
                            else
                            {
                                errorCode = ErrorCode.CONNECT_CHARGER_ERROR;
                                CheckUserHandleError(this);
                            }
                        }
                        catch (System.Exception)
                        {
                            errorCode = ErrorCode.CONNECT_CHARGER_ERROR;
                            CheckUserHandleError(this);
                        }
                        break;
                    case RobotGoToCharge.ROBCHAR_WAITTING_ROBOT_CONTACT_CHARGER:
                        try
                        {
                            result = chargerCtrl.WaitState(ChargerState.ST_CHARGING, TIME_OUT_WAIT_STATE);
                            if (ErrorCodeCharger.TRUE == result)
                            {
                                StateRobotToCharge = RobotGoToCharge.ROBCHAR_WAITTING_CHARGEBATTERY;
                                //robot.ShowText("ROBCHAR_WAITTING_CHARGEBATTERY");
                            }
                            else
                            {
                                if (result == ErrorCodeCharger.ERROR_CONNECT)
                                {
                                    errorCode = ErrorCode.CONNECT_CHARGER_ERROR;
                                    //robot.ShowText("CONNECT_CHARGER_ERROR");
                                }
                                else
                                {
                                    errorCode = ErrorCode.CONTACT_CHARGER_ERROR;
                                    //robot.ShowText("CONTACT_CHARGER_ERROR");
                                }
                                CheckUserHandleError(this);
                                StateRobotToCharge = RobotGoToCharge.ROBCHAR_ROBOT_START_CHARGE;
                            }
                        }
                        catch (System.Exception)
                        {
                            errorCode = ErrorCode.CONNECT_CHARGER_ERROR;
                            //robot.ShowText("CONNECT_CHARGER_ERROR");
                            CheckUserHandleError(this);
                            StateRobotToCharge = RobotGoToCharge.ROBCHAR_ROBOT_START_CHARGE;
                        }
                        break; //robot tiep xuc tram sac        
                    //case RobotGoToCharge.ROBCHAR_ROBOT_ALLOW_CUTOFF_POWER_ROBOT:
                    //    rb.SendCmdPosPallet (RequestCommandPosPallet.REQUEST_TURNOFF_PC);
                    //    StateRobotToCharge = RobotGoToCharge.ROBCHAR_ROBOT_WAITTING_CUTOFF_POWER_PC;
                    //    //robot.ShowText("ROBCHAR_ROBOT_WAITTING_CUTOFF_POWER_PC"); 
                    //    sw.Start ();
                    //    break; //cho phép cắt nguồn robot
                    //case RobotGoToCharge.ROBCHAR_ROBOT_WAITTING_CUTOFF_POWER_PC:
                    //    if (true != rb.properties.IsConnected) {
                    //        StateRobotToCharge = RobotGoToCharge.ROBCHAR_WAITTING_CHARGEBATTERY;
                    //        //robot.ShowText("ROBCHAR_WAITTING_CHARGEBATTERY"); 
                    //    } else {
                    //        if (sw.ElapsedMilliseconds > TIME_OUT_WAIT_TURNOFF_PC) {
                    //            sw.Stop ();
                    //            errorCode = ErrorCode.CAN_NOT_TURN_OFF_PC;
                    //            CheckUserHandleError (this);
                    //            StateRobotToCharge = RobotGoToCharge.ROBCHAR_ROBOT_ALLOW_CUTOFF_POWER_ROBOT;
                    //        }
                    //    }
                    //    break;
#endif
                    case RobotGoToCharge.ROBCHAR_WAITTING_CHARGEBATTERY:
#if false  //for test
                        StateRobotToCharge = RobotGoToCharge.ROBCHAR_FINISHED_CHARGEBATTERY;
#else
                        try
                        {
                            timeCountGetBatLevel++;
                            if (timeCountGetBatLevel >= TIME_COUNT_GET_BAT_LEVEL)
                            {
                                timeCountGetBatLevel = 0;
                                result = rb.mcuCtrl.GetBatteryLevel(ref batLevel);
                                Console.WriteLine("=================****+++++bat level {0}+++++++++++++++++++", batLevel.data[0]);
                                if (ErrorCodeCharger.TRUE == result)
                                {
                                    rb.properties.BatteryLevelRb = (float)batLevel.data[0];
#if USE_MANUAL_CHARGE
                                    if (batLevel.data[0] >= BATTERY_FULL_LEVEL)
#else
                                    if (batLevel.data[0] >= BATTERY_NEW_BAT)
#endif
                                    {
                                        //Thread.Sleep((int)TIME_DELAY_RELEASE_CHARGE);
#if USE_MANUAL_CHARGE
                                        StateRobotToCharge = RobotGoToCharge.ROBCHAR_FINISHED_CHARGEBATTERY;
#else
                                        StateRobotToCharge = RobotGoToCharge.ROBCHAR_ROBOT_WAITTING_RECONNECTING;
#endif
                                        //robot.ShowText("ROBCHAR_FINISHED_CHARGEBATTERY");
                                    }
                                }
                                else
                                {
#if USE_MANUAL_CHARGE
                                    if (result == ErrorCodeCharger.ERROR_CONNECT)
                                    {
                                        errorCode = ErrorCode.CONNECT_BOARD_CTRL_ROBOT_ERROR;
                                    }
                                    CheckUserHandleError(this);
#endif
                                }
                            }

                        }
                        catch (System.Exception e)
                        {
                            Console.WriteLine(e);
#if USE_MANUAL_CHARGE
                            errorCode = ErrorCode.CONNECT_BOARD_CTRL_ROBOT_ERROR;
                            CheckUserHandleError(this);
#endif
                        }
#endif
                                    break; //dợi charge battery và thông tin giao tiếp server và trạm sạc
#if USE_MANUAL_CHARGE

                    case RobotGoToCharge.ROBCHAR_FINISHED_CHARGEBATTERY:
                        try
                        {
                            if (true == chargerCtrl.StopCharge())
                            {
                                Console.WriteLine("Stop charger success");
                            }
                            else
                            {
                                errorCode = ErrorCode.CONNECT_CHARGER_ERROR;
                                CheckUserHandleError(this);
                            }
                        }
                        catch (System.Exception)
                        {
                            errorCode = ErrorCode.CONNECT_CHARGER_ERROR;
                            CheckUserHandleError(this);
                        }

                        Thread.Sleep(10000);

                        try
                        {
                            if (true == rb.mcuCtrl.TurnOnPcRobot())
                            {
                                //robot.ShowText("Turn on pc");
                                Thread.Sleep(45000);
                                //robot.ShowText("Reconnect server");
                                rb.Start(rb.properties.Url);
                                StateRobotToCharge = RobotGoToCharge.ROBCHAR_ROBOT_WAITTING_RECONNECTING;
                                //robot.ShowText("ROBCHAR_ROBOT_WAITTING_RECONNECTING");
                            }
                            else
                            {
                                errorCode = ErrorCode.CAN_NOT_TURN_ON_PC;
                                CheckUserHandleError(this);
                            }
                        }
                        catch (System.Exception)
                        {
                            errorCode = ErrorCode.CONNECT_BOARD_CTRL_ROBOT_ERROR;
                            CheckUserHandleError(this);
                        }
                        break; //Hoàn Thành charge battery và thông tin giao tiếp server và trạm sạc
#endif
                    case RobotGoToCharge.ROBCHAR_ROBOT_WAITTING_RECONNECTING:
                        if (true == CheckReconnectServer(TIME_OUT_ROBOT_RECONNECT_SERVER))
                        {
#if USE_MANUAL_CHARGE
                            StateRobotToCharge = RobotGoToCharge.ROBCHAR_ROBOT_GETOUT_CHARGER;
#else
                            StateRobotToCharge = RobotGoToCharge.ROBCHAR_ROBOT_RELEASED;
#endif
                            //robot.ShowText("ROBCHAR_ROBOT_GETOUT_CHARGER");
                        }
                        else
                        {
                            errorCode = ErrorCode.ROBOT_CANNOT_CONNECT_SERVER_AFTER_CHARGE;
                            CheckUserHandleError(this);
                        }
                        break; //Robot mở nguồng và đợi connect lại
#if USE_MANUAL_CHARGE
                    case RobotGoToCharge.ROBCHAR_ROBOT_GETOUT_CHARGER:
                        if (rb.SendCmdLineDetectionCtrl(RequestCommandLineDetect.REQUEST_LINEDETECT_GETOUT_CHARGER))
                        {
                            StateRobotToCharge = RobotGoToCharge.ROBCHAR_ROBOT_WAITTING_GETOUT_CHARGER;
                            //robot.ShowText("ROBCHAR_ROBOT_WAITTING_GETOUT_CHARGER");
                        }
                        break;
                    case RobotGoToCharge.ROBCHAR_ROBOT_WAITTING_GETOUT_CHARGER:
                        if (resCmd == ResponseCommand.RESPONSE_FINISH_DETECTLINE_GETOUT_CHARGER)
                        {
                            StateRobotToCharge = RobotGoToCharge.ROBCHAR_ROBOT_RELEASED;
                            //robot.ShowText("ROBCHAR_ROBOT_RELEASED");
                        }
                        else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                        {
                            errorCode = ErrorCode.DETECT_LINE_CHARGER_ERROR;
                            CheckUserHandleError(this);
                        }
                        break;
#endif
                    case RobotGoToCharge.ROBCHAR_ROBOT_RELEASED:
                        robot.robotTag = RobotStatus.IDLE;
                        rb.PreProcedureAs = ProcedureControlAssign.PRO_READY;
                        ReleaseProcedureHandler(this);
                        ProRun = false;
                        //robot.ShowText("RELEASED");
                        UpdateInformationInProc(this, ProcessStatus.S);
                        order.endTimeProcedure = DateTime.Now;
                        order.totalTimeProcedure = order.endTimeProcedure.Subtract(order.startTimeProcedure).TotalMinutes;
                        SaveOrderItem(order);
                        KillEvent();
                        break; // trả robot về robotmanagement để nhận quy trình mới
                    default:
                        break;
                }
                robot.ShowText("-> " + procedureCode);
                Thread.Sleep(5);
            }
            StateRobotToCharge = RobotGoToCharge.ROBCHAR_IDLE;
        }

        private bool CheckReconnectServer(UInt32 timeOut)
        {
            bool result = true;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            do
            {
                Thread.Sleep(1000);
                if (sw.ElapsedMilliseconds > timeOut)
                {
                    result = false;
                    break;
                }
            } while (true != robot.properties.IsConnected);
            sw.Stop();
            return result;
        }
        public override void FinishStatesCallBack(Int32 message)
        {
            this.resCmd = (ResponseCommand)message;
            if (this.resCmd == ResponseCommand.RESPONSE_FINISH_GOBACK_FRONTLINE)
            {
                robot.ReleaseWorkingZone();
            }
        }
    }
    public class ProcedureRobotToReady : TrafficProcedureService
    {
        public struct DataRobotToReady
        {
            public Pose PointCheckIn;
            public Pose PointFrontLine;
            public String PointOfCharger;
        }
        DataRobotToReady points;
        //List<DataRobotToReady> DataRobotToReadyList;
        Thread ProRobotToReady;
        ChargerManagementService charger;
        public RobotUnity robot;
        ResponseCommand resCmd;
        ChargerId chardgeId;
        RobotGoToReady StateRobotGoToReady;
        TrafficManagementService Traffic;
        public override event Action<Object> ReleaseProcedureHandler;
        private DeviceRegistrationService deviceService;

        // public override event Action<Object> ErrorProcedureHandler;
        public ProcedureRobotToReady(RobotUnity robot, ChargerId id, TrafficManagementService trafficService, ChargerManagementService chargerService, Pose PointCheckIn) : base(robot,trafficService)
        {
            StateRobotGoToReady = RobotGoToReady.ROBREA_IDLE;
            this.robot = robot;
            this.Traffic = trafficService;
            this.charger = chargerService;
            this.chardgeId = id;
            procedureCode = ProcedureCode.PROC_CODE_ROBOT_TO_READY;
        }
        public void Registry(DeviceRegistrationService deviceService)
        {
            this.deviceService = deviceService;
        }

        public void Start(RobotGoToReady state = RobotGoToReady.ROBREA_SELECT_BEHAVIOR_ONZONE)
        {
            order = new OrderItem();
            order.typeReq = TyeRequest.TYPEREQUEST_GOTO_READY;
            errorCode = ErrorCode.RUN_OK;
            robot.robotTag = RobotStatus.WORKING;
            robot.ProcedureAs = ProcedureControlAssign.PRO_READY;
            StateRobotGoToReady = state;
            ProRobotToReady = new Thread(this.Procedure);
            ProRobotToReady.Start(this);
            procedureCode = ProcedureCode.PROC_CODE_ROBOT_TO_READY;
            ProRun = true;
            //robot.prioritLevel.OnAuthorizedPriorityProcedure = false;
            order.startTimeProcedure = DateTime.Now;

            points.PointCheckIn = null;
            points.PointFrontLine = GetFrontLineChargeStation();
            points.PointOfCharger = this.charger.PropertiesCharge_List[(int)chardgeId - 1].PointOfPallet;
            registryRobotJourney = new RegistryRobotJourney();
            registryRobotJourney.robot = robot;
            registryRobotJourney.traffic = Traffic;
        }
        public void Destroy()
        {
            // StateRobotGoToReady = RobotGoToReady.ROBREA_ROBOT_RELEASED;
            robot.SetSafeYellowcircle(false);
            robot.SetSafeBluecircle(false);
            robot.SetSafeSmallcircle(false);
            robot.robotTag = RobotStatus.IDLE;
            //robot.prioritLevel.OnAuthorizedPriorityProcedure = false;
            ProRun = false;
            UpdateInformationInProc(this, ProcessStatus.F);
            selectHandleError = SelectHandleError.CASE_ERROR_EXIT;
            order.endTimeProcedure = DateTime.Now;
            order.totalTimeProcedure = order.endTimeProcedure.Subtract(order.startTimeProcedure).TotalMinutes;
            SaveOrderItem(order);
        }
        protected Pose GetFrontLineChargeStation()
        {
            if (this.Traffic.RobotIsInArea("OUTER", robot.properties.pose.Position, TypeZone.MAIN_ZONE))
            {
                return this.charger.PropertiesCharge_List[(int)chardgeId - 1].PointFrontLine;
            }
            else
            {
                return this.charger.PropertiesCharge_List[(int)chardgeId - 1].PointFrontLineInv;
            }
        }
        public void Procedure(object ojb)
        {
            ProcedureRobotToReady RbToRd = (ProcedureRobotToReady)ojb;
            RobotUnity rb = RbToRd.robot;
            DataRobotToReady p = RbToRd.points;
            TrafficManagementService Traffic = RbToRd.Traffic;
            //robot.ShowText(" Start -> " + procedureCode);
            while (ProRun)
            {
                switch (StateRobotGoToReady)
                {
                    case RobotGoToReady.ROBREA_IDLE:
                        //robot.ShowText("ROBREA_IDLE");
                        break;
                    case RobotGoToReady.ROBREA_SELECT_BEHAVIOR_ONZONE:
                        if (Traffic.RobotIsInArea("READY", robot.properties.pose.Position,TypeZone.OPZS))
                        {
                            StateRobotGoToReady = RobotGoToReady.ROBREA_ROBOT_RELEASED;
                        }
                         if (rb.SendPoseStamped(p.PointFrontLine))
                         {
                                StateRobotGoToReady = RobotGoToReady.ROBREA_ROBOT_WAITTING_GOTO_READYSTATION;
                                //robot.ShowText("ROBREA_ROBOT_GOTO_FRONTLINE_READYSTATION");
                                registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position);
                                registryRobotJourney.startPoint = robot.properties.pose.Position;
                                registryRobotJourney.endPoint = p.PointFrontLine.Position;
                         }
                    break;
                    case RobotGoToReady.ROBREA_ROBOT_WAITTING_GOTO_READYSTATION: // Robot dang di toi dau line ready station
                        if (TrafficRountineConstants.DetetectInsideStationCheck(registryRobotJourney))
                        {
                            break;
                        }
                        if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                        {
                            TrafficRountineConstants.DetectRelease(registryRobotJourney);
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            StateRobotGoToReady = RobotGoToReady.ROBREA_ROBOT_WAIITNG_DETECTLINE_TO_READYSTATION;
                            //robot.ShowText("ROBREA_ROBOT_WAIITNG_DETECTLINE_TO_READYSTATION");
                        }
                        else if (Traffic.RobotIsInArea("READY", robot.properties.pose.Position))
                        {
                            // robot.TurnOnSupervisorTraffic(false);
                        }
                        break;
                    case RobotGoToReady.ROBREA_ROBOT_WAIITNG_DETECTLINE_TO_READYSTATION: // đang đợi dò line để đến vị trí line trong buffer
                        if (rb.SendCmdAreaPallet(RbToRd.points.PointOfCharger))
                        {
                            StateRobotGoToReady = RobotGoToReady.ROBREA_ROBOT_WAITTING_CAME_POSITION_READYSTATION;
                            //robot.ShowText("ROBREA_ROBOT_WAITTING_CAME_POSITION_READYSTATION");
                        }
                        break;
                    case RobotGoToReady.ROBREA_ROBOT_WAITTING_CAME_POSITION_READYSTATION: // đến vị trả robot về robotmanagement để nhận quy trình mới
                        if (resCmd == ResponseCommand.RESPONSE_FINISH_GOTO_POSITION)
                        {
                            //rb.prioritLevel.OnAuthorizedPriorityProcedure = false;
                            StateRobotGoToReady = RobotGoToReady.ROBREA_ROBOT_RELEASED;
                            //robot.ShowText("ROBREA_ROBOT_RELEASED");
                        }
                        else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                        {
                            errorCode = ErrorCode.DETECT_LINE_ERROR;
                            CheckUserHandleError(this);
                        }
                        break;
                    case RobotGoToReady.ROBREA_ROBOT_RELEASED:
                        TrafficRountineConstants.RegIntZone_READY.Release(robot);
                        robot.robotTag = RobotStatus.IDLE;
                        robot.SetSafeYellowcircle(false);
                        robot.SetSafeBluecircle(false);
                        robot.SetSafeSmallcircle(false);
                        robot.TurnOnSupervisorTraffic(false);
                        rb.mcuCtrl.TurnOffLampRb();
                        rb.PreProcedureAs = ProcedureControlAssign.PRO_READY;
                        // if (errorCode == ErrorCode.RUN_OK) {
                        ReleaseProcedureHandler(this);
                        // } else {
                        //     ErrorProcedureHandler (this);
                        // }
                        robot.setColorRobotStatus(RobotStatusColorCode.ROBOT_STATUS_OK);
                        ProRun = false;
                        //robot.ShowText("RELEASED");
                        UpdateInformationInProc(this, ProcessStatus.S);
                        order.endTimeProcedure = DateTime.Now;
                        order.totalTimeProcedure = order.endTimeProcedure.Subtract(order.startTimeProcedure).TotalMinutes;
                        SaveOrderItem(order);
                        KillEvent();
                        break;
                    case RobotGoToReady.ROBREA_ROBOT_WAITINGREADY_FORCERELEASED:
                        // add to wait task;
                        //robot.robotTag = RobotStatus.IDLE;

                        robot.setColorRobotStatus(RobotStatusColorCode.ROBOT_STATUS_OK);
                        procedureCode = ProcedureControlServices.ProcedureCode.PROC_CODE_ROBOT_WAITINGTO_READY;
                        ProRun = false;
                        //robot.ShowText("RELEASED WHEN WAITTING TO READY, HAS AN NEW TASK");
                        UpdateInformationInProc(this, ProcessStatus.S);
                        order.endTimeProcedure = DateTime.Now;
                        order.totalTimeProcedure = order.endTimeProcedure.Subtract(order.startTimeProcedure).TotalMinutes;
                        SaveOrderItem(order);
                        rb.PreProcedureAs = ProcedureControlAssign.PRO_WAIT_TASK;
                        ReleaseProcedureHandler(this);
                        KillEvent();
                        break;
                }
                Thread.Sleep(500);
            }
            StateRobotGoToReady = RobotGoToReady.ROBREA_IDLE;
        }
        // do vung layout moi vung gate 1 va ready robot 3 gan nhau nen kiem tra dieu kien
        public bool checkinRobot3(RobotUnity robot)
        {
            if (robot.properties.Label.Equals("Robot3"))
            {
                if(TrafficRountineConstants.RegIntZone_READY.GetIndex(robot)==0)
                {
                    return false;
                }
                if (Global_Object.getGateStatus((int)DoorId.DOOR_MEZZAMINE_UP_NEW))
                {
                    return true;
                }
            }
           return false;
        }
        // xác định còn task trong order
        public bool DetermineHasTaskWaitingAnRobotAvailable()
        {
            try
            {
                List<DeviceItem> deviceList = deviceService.GetDeviceItemList();
                if (deviceList.Count > 0)
                {
                    int cntAmoutOrderItem = 0;
                    foreach (DeviceItem item in deviceList)
                    {
                        if (item.PendingOrderList.Count > 0)
                        {
                            cntAmoutOrderItem++;
                        }
                    }
                    if (cntAmoutOrderItem > 0) //
                    {
                        if (robotService.RobotUnityWaitTaskList.Count > 0 || robotService.RobotUnityReadyList.Count > 0)
                            return false;
                        else
                        {
                            //GATE_CHECKIN
                            if (!Traffic.HasRobotUnityinArea("READY", robot) || !Traffic.HasRobotUnityinArea("GATE_CHECKIN", robot))
                            {
                                return true;
                            }
                        }

                          
                    }

                }
            }
            catch { }
            return false;
        }
      //  !Traffic.HasRobotUnityinArea("GATE_CHECKOUT", robot)
        public override void FinishStatesCallBack(Int32 message)
        {
            this.resCmd = (ResponseCommand)message;
            
        }
    }
}
