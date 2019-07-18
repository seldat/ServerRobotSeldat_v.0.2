using SeldatMRMS;
using System;
using System;
using System.Diagnostics;
using System.Threading;
using DoorControllerService;
using SeldatMRMS.Management.DoorServices;
using SeldatMRMS.Management.RobotManagent;
using SeldatMRMS.Management.TrafficManager;
using static DoorControllerService.DoorService;
using static SeldatMRMS.Management.RobotManagent.RobotBaseService;
using static SeldatMRMS.Management.RobotManagent.RobotUnityControl;
using static SeldatMRMS.Management.TrafficRobotUnity;
using System.Windows;
using SeldatUnilever_Ver1._02.Management.TrafficManager;
using static SelDatUnilever_Ver1._00.Management.TrafficManager.TrafficRounterService;

namespace SeldatUnilever_Ver1._02.Management.ProcedureServices
{
    public class ProcedureBufferToGate : TrafficProcedureService
    {
        // DataBufferToGate points;
        BufferToGate StateBufferToGate;
        Thread ProBufferToGate;
        public RobotUnity robot;
        public DoorManagementService door;
        ResponseCommand resCmd;
        TrafficManagementService Traffic;

        public override event Action<Object> ReleaseProcedureHandler;
        // public override event Action<Object> ErrorProcedureHandler;
        public ProcedureBufferToGate(RobotUnity robot, DoorManagementService doorservice, TrafficManagementService traffiicService) : base(robot,doorservice,traffiicService)
        {
            StateBufferToGate = BufferToGate.BUFGATE_IDLE;
            resCmd = ResponseCommand.RESPONSE_NONE;
            this.robot = robot;
            base.robot = robot;
            // this.points = new DataBufferToGate();
            this.door = doorservice;
            this.Traffic = traffiicService;
            errorCode = ErrorCode.RUN_OK;
            procedureCode = ProcedureCode.PROC_CODE_BUFFER_TO_GATE;
        }
        public void Start(BufferToGate state = BufferToGate.BUFGATE_ROBOT_WAITTING_GOTO_CHECKIN_BUFFER)
        {
            errorCode = ErrorCode.RUN_OK;
            robot.robotTag = RobotStatus.WORKING;
            robot.ProcedureAs = ProcedureControlAssign.PRO_BUFFER_TO_GATE;
            StateBufferToGate = state;
            ProBufferToGate = new Thread(this.Procedure);
            ProBufferToGate.Start(this);
            ProRun = true;
            ProRunStopW = true;
            //robot.prioritLevel.OnAuthorizedPriorityProcedure = false;
            order.startTimeProcedure = DateTime.Now;
            registryRobotJourney = new RegistryRobotJourney();
            registryRobotJourney.robot = robot;
            registryRobotJourney.traffic = Traffic;
        }
        public void Destroy()
        {
            StateBufferToGate = BufferToGate.BUFGATE_ROBOT_DESTROY;

        }
        public void Procedure(object ojb)
        {
            ProcedureBufferToGate BuffToGate = (ProcedureBufferToGate)ojb;
            RobotUnity rb = BuffToGate.robot;
            // DataBufferToGate p = BuffToGate.points;
            DoorService ds = getDoorService();
            TrafficManagementService Traffic = BuffToGate.Traffic;
            rb.mcuCtrl.TurnOnLampRb();
            
            robot.ShowText(" Start -> " + procedureCode);
            while (ProRun)
            {
                switch (StateBufferToGate)
                {
                    case BufferToGate.BUFGATE_IDLE:
                        break;
                    case BufferToGate.BUFGATE_SELECT_BEHAVIOR_ONZONE:
                        if (Traffic.RobotIsInArea("READY", robot.properties.pose.Position,TypeZone.OPZS))
                        {
                            if (rb.PreProcedureAs == ProcedureControlAssign.PRO_READY)
                            {
                                StateBufferToGate = BufferToGate.BUFGATE_ROBOT_GOTO_BACK_FRONTLINE_READY;
                                registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                registryRobotJourney.startPoint = robot.properties.pose.Position;
                                registryRobotJourney.endPoint = BuffToGate.GetFrontLineBuffer().Position;
                                //robot.ShowText("BUFMAC_SELECT_BEHAVIOR_ONZONE : READY");
                                //robot.ShowText("CHECK - REG");
                            }
                        }
                        else if (Traffic.RobotIsInArea("VIM", robot.properties.pose.Position, TypeZone.MAIN_ZONE))
                        {
                            if (rb.SendPoseStamped(BuffToGate.GetFrontLineBuffer()))
                            {
                                StateBufferToGate = BufferToGate.BUFGATE_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER;
                                registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position);
                                registryRobotJourney.startPoint = robot.properties.pose.Position;
                                registryRobotJourney.endPoint = BuffToGate.GetFrontLineBuffer().Position;
                                //robot.ShowText("BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER");
                            }
                        }
                        else if (Traffic.RobotIsInArea("OUTER", robot.properties.pose.Position, TypeZone.MAIN_ZONE))
                        {
                            Point destPos1 = BuffToGate.GetFrontLineBuffer().Position;
                            String destName1 = Traffic.DetermineArea(destPos1, TypeZone.MAIN_ZONE);
                            if (destName1.Equals("OUTER"))
                            {
                                //robot.ShowText("GO FRONTLINE IN OUTER");
                                if (rb.SendPoseStamped(BuffToGate.GetFrontLineBuffer()))
                                {
                                    StateBufferToGate = BufferToGate.BUFGATE_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER;
                                    registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                    registryRobotJourney.startPoint = robot.properties.pose.Position;
                                    registryRobotJourney.endPoint = destPos1;
                                    //robot.ShowText("BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER");
                                }
                            }
                            else if (destName1.Equals("VIM"))
                            {
                                //robot.ShowText("GO FRONTLINE IN VIM");
                                if (rb.SendPoseStamped(BuffToGate.GetFrontLineBuffer()))
                                {
                                    StateBufferToGate = BufferToGate.BUFGATE_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_FROM_VIM;
                                    registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                    registryRobotJourney.startPoint = robot.properties.pose.Position;
                                    registryRobotJourney.endPoint = destPos1;
                                    //robot.ShowText("BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER");
                                    //robot.ShowText("CHECK - REG");
                                }
                            }
                        }
                        break;
                    case BufferToGate.BUFGATE_ROBOT_GOTO_BACK_FRONTLINE_READY:
                        if (TrafficRountineConstants.DetetectInsideStationCheck(registryRobotJourney))
                        {
                            break;
                        }
                        //robot.ShowText("START :GOTO_BACK_FRONTLINE_READY");
                        if (rb.SendCmdPosPallet(RequestCommandPosPallet.REQUEST_GOBACK_FRONTLINE_TURN_RIGHT))
                        {
                            Stopwatch sw = new Stopwatch();
                            sw.Start();
                            do
                            {
                                if (resCmd == ResponseCommand.RESPONSE_FINISH_GOBACK_FRONTLINE)
                                {
                                    resCmd = ResponseCommand.RESPONSE_NONE;
                                    Pose destPos2 = BuffToGate.GetFrontLineBuffer();
                                    String destName2 = Traffic.DetermineArea(destPos2.Position, TypeZone.MAIN_ZONE);
                                    if (destName2.Equals("OUTER"))
                                    {
                                        //robot.ShowText("GO FRONTLINE IN OUTER");
                                        if (rb.SendPoseStamped(BuffToGate.GetFrontLineBuffer()))
                                        {
                                            StateBufferToGate = BufferToGate.BUFGATE_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER;
                                            registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                            registryRobotJourney.startPoint = robot.properties.pose.Position;
                                            registryRobotJourney.endPoint = destPos2.Position;
                                            //robot.ShowText("BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER");
                                        }
                                    }
                                    else if (destName2.Equals("VIM"))
                                    {
                                        //robot.ShowText("GO FRONTLINE IN VIM");
                                        if (rb.SendPoseStamped(BuffToGate.GetFrontLineBuffer()))
                                        {
                                            StateBufferToGate = BufferToGate.BUFGATE_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_FROM_VIM_READY;
                                            registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                            registryRobotJourney.startPoint = robot.properties.pose.Position;
                                            registryRobotJourney.endPoint = destPos2.Position;
                                            //robot.ShowText("BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER");
                                            //robot.ShowText("CHECK - REG");
                                        }
                                    }
                                    break;
                                }
                                else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                                {
                                    errorCode = ErrorCode.DETECT_LINE_ERROR;
                                    CheckUserHandleError(this);
                                    break;
                                }
                                if (sw.ElapsedMilliseconds > TIME_OUT_WAIT_GOTO_FRONTLINE)
                                {
                                    errorCode = ErrorCode.DETECT_LINE_ERROR;
                                    CheckUserHandleError(this);
                                    break;
                                }
                                Thread.Sleep(100);
                            } while (ProRunStopW);
                            sw.Stop();
                        }
                        break;
                    case BufferToGate.BUFGATE_ROBOT_WAITTING_ZONE_BUFFER_READY: // doi khu vuc buffer san sang de di vao
                        try
                        {
                            if (false == robot.CheckInZoneBehavior(BuffToGate.GetFrontLineBuffer().Position))
                            {
                                if (rb.SendPoseStamped(BuffToGate.GetFrontLineBuffer()))
                                {
                                    StateBufferToGate = BufferToGate.BUFGATE_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER;
                                    //robot.ShowText("BUFGATE_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER");
                                }
                            }
                        }
                        catch (System.Exception)
                        {
                            errorCode = ErrorCode.CAN_NOT_GET_DATA;
                            CheckUserHandleError(this);
                        }
                        break;
                    case BufferToGate.BUFGATE_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_FROM_VIM_READY:
                        TrafficRountineConstants.DetectRelease(registryRobotJourney);
                        try
                        {
                            if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                            {
                                resCmd = ResponseCommand.RESPONSE_NONE;
                                if (rb.SendCmdAreaPallet(BuffToGate.GetInfoOfPalletReturn(PistonPalletCtrl.PISTON_PALLET_UP)))
                                {
                                    StateBufferToGate = BufferToGate.BUFGATE_ROBOT_WAITTING_PICKUP_PALLET_BUFFER;
                                    //robot.ShowText("BUFGATE_ROBOT_WAITTING_PICKUP_PALLET_BUFFER");
                                }
                            }
                            else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                            {
                                errorCode = ErrorCode.DETECT_LINE_ERROR;
                                CheckUserHandleError(this);
                            }
                        }
                        catch (System.Exception)
                        {
                            errorCode = ErrorCode.CAN_NOT_GET_DATA;
                            CheckUserHandleError(this);
                        }
                    break;
                    case BufferToGate.BUFGATE_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_FROM_VIM:
                        if (TrafficRountineConstants.DetetectInsideStationCheck(registryRobotJourney))
                        {
                            break;
                        }
                        else
                        {
                            TrafficRountineConstants.DetectRelease(registryRobotJourney);
                        }
                        try
                        {
                            if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                            {
                                resCmd = ResponseCommand.RESPONSE_NONE;
                                if (rb.SendCmdAreaPallet(BuffToGate.GetInfoOfPalletReturn(PistonPalletCtrl.PISTON_PALLET_UP)))
                                {
                                    StateBufferToGate = BufferToGate.BUFGATE_ROBOT_WAITTING_PICKUP_PALLET_BUFFER;
                                    //robot.ShowText("BUFGATE_ROBOT_WAITTING_PICKUP_PALLET_BUFFER");
                                }
                            }
                            else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                            {
                                errorCode = ErrorCode.DETECT_LINE_ERROR;
                                CheckUserHandleError(this);
                            }
                        }
                        catch (System.Exception)
                        {
                            errorCode = ErrorCode.CAN_NOT_GET_DATA;
                            CheckUserHandleError(this);
                        }
                        break;
                    case BufferToGate.BUFGATE_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER:
                        try
                        {
                            if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                            {
                                resCmd = ResponseCommand.RESPONSE_NONE;
                                if (rb.SendCmdAreaPallet(BuffToGate.GetInfoOfPalletReturn(PistonPalletCtrl.PISTON_PALLET_UP)))
                                {
                                    // rb.SendCmdLineDetectionCtrl(RequestCommandLineDetect.REQUEST_LINEDETECT_PALLETUP);
                                    //rb.prioritLevel.OnAuthorizedPriorityProcedure = true;
                                    StateBufferToGate = BufferToGate.BUFGATE_ROBOT_WAITTING_PICKUP_PALLET_BUFFER;
                                    //robot.ShowText("BUFGATE_ROBOT_WAITTING_PICKUP_PALLET_BUFFER");
                                }
                            }
                            else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                            {
                                errorCode = ErrorCode.DETECT_LINE_ERROR;
                                CheckUserHandleError(this);
                            }
                        }
                        catch (System.Exception)
                        {
                            errorCode = ErrorCode.CAN_NOT_GET_DATA;
                            CheckUserHandleError(this);
                        }
                        break;
                    case BufferToGate.BUFGATE_ROBOT_WAITTING_PICKUP_PALLET_BUFFER:
                        if (resCmd == ResponseCommand.RESPONSE_LINEDETECT_PALLETUP)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            BuffToGate.UpdatePalletState(PalletStatus.F);
                            StateBufferToGate = BufferToGate.BUFGATE_ROBOT_WAITTING_GOBACK_FRONTLINE_BUFFER;
                            //robot.ShowText("BUFGATE_ROBOT_WAITTING_GOBACK_FRONTLINE_BUFFER");
                        }
                        else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                        {
                            errorCode = ErrorCode.DETECT_LINE_ERROR;
                            CheckUserHandleError(this);
                        }
                        break;
                    case BufferToGate.BUFGATE_ROBOT_WAITTING_GOBACK_FRONTLINE_BUFFER: // đợi
                        if (resCmd == ResponseCommand.RESPONSE_FINISH_GOBACK_FRONTLINE)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            //rb.prioritLevel.OnAuthorizedPriorityProcedure = false;
                            StateBufferToGate = BufferToGate.BUFGATE_SELECT_BEHAVIOR_ONZONE_TO_GATE;
                            // cập nhật lại điểm xuất phát
                            registryRobotJourney.startPoint = robot.properties.pose.Position;
                        }
                        else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                        {
                            errorCode = ErrorCode.DETECT_LINE_ERROR;
                            CheckUserHandleError(this);
                        }
                        break;
                    case BufferToGate.BUFGATE_SELECT_BEHAVIOR_ONZONE_TO_GATE:
                        String startNamePoint = Traffic.DetermineArea(registryRobotJourney.startPoint, TypeZone.MAIN_ZONE);
                        Pose destPos = BuffToGate.GetFrontLineMachine();
                        String destName = Traffic.DetermineArea(destPos.Position, TypeZone.MAIN_ZONE);
                            // đi tới đầu line cổng theo tọa độ chỉ định. gate 1 , 2, 3
                        if (rb.SendPoseStamped(ds.config.PointFrontLine))
                        {
                                StateBufferToGate = BufferToGate.BUFGATE_ROBOT_WAITTING_GOTO_GATE_FROM_VIM;
                                // Cap Nhat Thong Tin CHuyen Di
                                registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position);
                                registryRobotJourney.startPoint = robot.properties.pose.Position;
                                registryRobotJourney.endPoint = ds.config.PointFrontLine.Position;
                                //robot.ShowText("FORBUF_ROBOT_WAITTING_GOTO_GATE");
                        }
                        break;
                    case BufferToGate.BUFGATE_ROBOT_WAITTING_GOTO_CHECKIN_GATE:
                        if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            //rb.prioritLevel.OnAuthorizedPriorityProcedure = true;
                            StateBufferToGate = BufferToGate.BUFGATE_ROBOT_CAME_CHECKIN_GATE;
                            //robot.ShowText("BUFGATE_ROBOT_CAME_CHECKIN_GATE");
                        }
                        break;
                    case BufferToGate.BUFGATE_ROBOT_CAME_CHECKIN_GATE: // đã đến vị trí, kiem tra va cho khu vuc cong san sang de di vao.
                        if (TrafficRountineConstants.DetetectInsideStationCheck(registryRobotJourney))
                        {
                            break;
                        }
                        else
                        {
                            TrafficRountineConstants.DetectRelease(registryRobotJourney);
                        }
                        if (false == robot.CheckInZoneBehavior(ds.config.PointFrontLine.Position))
                        {
                            //rb.prioritLevel.OnAuthorizedPriorityProcedure = false;
                            if (rb.SendPoseStamped(ds.config.PointFrontLine))
                            {
                                StateBufferToGate = BufferToGate.BUFGATE_ROBOT_WAITTING_GOTO_GATE;
                                //robot.ShowText("BUFGATE_ROBOT_WAITTING_GOTO_GATE");
                            }
                        }
                        break;
                    case BufferToGate.BUFGATE_ROBOT_WAITTING_GOTO_GATE_FROM_VIM:
                        if (TrafficRountineConstants.DetetectInsideStationCheck(registryRobotJourney))
                        {
                            break;
                        }
                        if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            //rb.prioritLevel.OnAuthorizedPriorityProcedure = true;
                            StateBufferToGate = BufferToGate.BUFGATE_ROBOT_CAME_GATE_POSITION;
                            //robot.ShowText("BUFGATE_ROBOT_CAME_GATE_POSITION");
                        }
                        break;
                    case BufferToGate.BUFGATE_ROBOT_WAITTING_GOTO_GATE:
                        if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            //rb.prioritLevel.OnAuthorizedPriorityProcedure = true;
                            StateBufferToGate = BufferToGate.BUFGATE_ROBOT_CAME_GATE_POSITION;
                            //robot.ShowText("BUFGATE_ROBOT_CAME_GATE_POSITION");
                        }
                        break;
                    case BufferToGate.BUFGATE_ROBOT_CAME_GATE_POSITION: // da den khu vuc cong , gui yeu cau mo cong.
                        ds.openDoor(DoorService.DoorType.DOOR_BACK);
                        StateBufferToGate = BufferToGate.BUFGATE_ROBOT_WAITTING_OPEN_DOOR;
                        //robot.ShowText("BUFGATE_ROBOT_WAITTING_OPEN_DOOR");
                        break;
                    case BufferToGate.BUFGATE_ROBOT_WAITTING_OPEN_DOOR: //doi mo cong
                        RetState ret = ds.checkOpen(DoorService.DoorType.DOOR_BACK);
                        if (ret == RetState.DOOR_CTRL_SUCCESS)
                        {
                            if (rb.SendCmdAreaPallet(ds.config.infoPallet))
                            {
                                StateBufferToGate = BufferToGate.BUFGATE_ROBOT_WAITTING_DROPDOWN_PALLET_BUFFER;
                                //robot.ShowText("BUFGATE_ROBOT_WAITTING_DROPDOWN_PALLET_BUFFER");
                            }
                        }
                        else if (ret == RetState.DOOR_CTRL_ERROR)
                        {
                            StateBufferToGate = BufferToGate.BUFGATE_ROBOT_CAME_GATE_POSITION;
                        }
                        break;
                    case BufferToGate.BUFGATE_ROBOT_WAITTING_DROPDOWN_PALLET_BUFFER: // doi robot gap hang
                        if (resCmd == ResponseCommand.RESPONSE_LINEDETECT_PALLETDOWN)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            // BuffToGate.UpdatePalletState(PalletStatus.W);
                            StateBufferToGate = BufferToGate.BUFGATE_ROBOT_WAITTING_GOBACK_FRONTLINE_GATE;
                            //robot.ShowText("BUFGATE_ROBOT_WAITTING_GOBACK_FRONTLINE_GATE");
                        }
                        else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                        {
                            errorCode = ErrorCode.DETECT_LINE_ERROR;
                            CheckUserHandleError(this);
                        }
                        break;
                    case BufferToGate.BUFGATE_ROBOT_WAITTING_GOBACK_FRONTLINE_GATE:
                        if (resCmd == ResponseCommand.RESPONSE_FINISH_GOBACK_FRONTLINE)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            ds.closeDoor(DoorService.DoorType.DOOR_BACK);
                            StateBufferToGate = BufferToGate.BUFGATE_ROBOT_WAITTING_CLOSE_GATE;
                            //robot.ShowText("BUFGATE_ROBOT_WAITTING_CLOSE_GATE");
                        }
                        else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                        {
                            errorCode = ErrorCode.DETECT_LINE_ERROR;
                            CheckUserHandleError(this);
                        }
                        break;
                    case BufferToGate.BUFGATE_ROBOT_WAITTING_CLOSE_GATE: // doi dong cong.
                        StateBufferToGate = BufferToGate.BUFGATE_ROBOT_RELEASED;
                        //robot.ShowText("BUFGATE_ROBOT_WAITTING_CLOSE_GATE");
                        break;

                    case BufferToGate.BUFGATE_ROBOT_RELEASED: // trả robot về robotmanagement để nhận quy trình mới
                        TrafficRountineConstants.ReleaseAll(robot);
                        robot.robotTag = RobotStatus.IDLE;
                        rb.PreProcedureAs = ProcedureControlAssign.PRO_BUFFER_TO_GATE;
                        ReleaseProcedureHandler(this);
                        ProRun = false;
                        UpdateInformationInProc(this, ProcessStatus.S);
                        order.endTimeProcedure = DateTime.Now;
                        order.totalTimeProcedure = order.endTimeProcedure.Subtract(order.startTimeProcedure).TotalMinutes;
                        SaveOrderItem(order);
                        KillEvent();
                        break;
                    case BufferToGate.BUFGATE_ROBOT_DESTROY:
                        ProRunStopW = false;
                        ProRunStopW = false;
                        robot.robotTag = RobotStatus.IDLE;
                        ProRun = false;
                        order.endTimeProcedure = DateTime.Now;
                        order.totalTimeProcedure = order.endTimeProcedure.Subtract(order.startTimeProcedure).TotalMinutes;
                        SaveOrderItem(order);
                        TrafficRountineConstants.ReleaseAll(robot);
                        break;
                    default:
                        break;
                }
                robot.ShowText("-> " + procedureCode);
                Thread.Sleep(5);
            }
            StateBufferToGate = BufferToGate.BUFGATE_IDLE;
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
}
