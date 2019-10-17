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
using Newtonsoft.Json;

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
        private DoorService ds;

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
        public void Start(BufferToGate state = BufferToGate.BUFGATE_SELECT_BEHAVIOR_ONZONE)
        {
            robot.bayId = -1;
            errorCode = ErrorCode.RUN_OK;
            robot.robotTag = RobotStatus.WORKING;
            robot.ProcedureAs = ProcedureControlAssign.PRO_BUFFER_TO_GATE;
            StateBufferToGate = state;
            ProBufferToGate = new Thread(this.Procedure);
            ProRun = true;
            ProRunStopW = true;
            //robot.prioritLevel.OnAuthorizedPriorityProcedure = false;
            order.startTimeProcedure = DateTime.Now;
            registryRobotJourney = new RegistryRobotJourney();
            registryRobotJourney.robot = robot;
            registryRobotJourney.traffic = Traffic;
            ProBufferToGate.Start(this);
        }
        public void Destroy()
        {
            StateBufferToGate = BufferToGate.BUFGATE_ROBOT_DESTROY;
            robot.bayId = -1;
            robot.bayIdReg = false;
            if (ds != null) {
                ds.LampSetStateOff(DoorType.DOOR_BACK);
                ds.setDoorBusy(false);
                ds.removeListCtrlDoorBack();
            }
        }
        public void Procedure(object ojb)
        {
            ProcedureBufferToGate BuffToGate = (ProcedureBufferToGate)ojb;
            RobotUnity rb = BuffToGate.robot;
            // DataBufferToGate p = BuffToGate.points;
            ds = getDoorService();
            ds.setRb(rb);
            TrafficManagementService Traffic = BuffToGate.Traffic;
            rb.mcuCtrl.lampRbOn();
            
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
                                ////robot.ShowText("BUFMAC_SELECT_BEHAVIOR_ONZONE : READY");
                                ////robot.ShowText("CHECK - REG");
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
                                ////robot.ShowText("BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER");
                            }
                        }
                        else if (Traffic.RobotIsInArea("OUTER", robot.properties.pose.Position, TypeZone.MAIN_ZONE))
                        {
                            Point destPos1 = BuffToGate.GetFrontLineBuffer().Position;
                            String destName1 = Traffic.DetermineArea(destPos1, TypeZone.MAIN_ZONE);
                            if (destName1.Equals("OUTER"))
                            {
                                ////robot.ShowText("GO FRONTLINE IN OUTER");
                                if (rb.SendPoseStamped(BuffToGate.GetFrontLineBuffer()))
                                {
                                    StateBufferToGate = BufferToGate.BUFGATE_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER;
                                    registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                    registryRobotJourney.startPoint = robot.properties.pose.Position;
                                    registryRobotJourney.endPoint = destPos1;
                                    ////robot.ShowText("BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER");
                                }
                            }
                            else if (destName1.Equals("VIM"))
                            {
                                ////robot.ShowText("GO FRONTLINE IN VIM");
                                if (rb.SendPoseStamped(BuffToGate.GetFrontLineBuffer()))
                                {
                                    StateBufferToGate = BufferToGate.BUFGATE_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_FROM_VIM_REG;
                                    registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                    registryRobotJourney.startPoint = robot.properties.pose.Position;
                                    registryRobotJourney.endPoint = destPos1;
                                    ////robot.ShowText("BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER");
                                    ////robot.ShowText("CHECK - REG");
                                }
                            }
                        }
                        break;
                    case BufferToGate.BUFGATE_ROBOT_GOTO_BACK_FRONTLINE_READY:
                        if (TrafficRountineConstants.DetetectInsideStationCheck(registryRobotJourney))
                        {
                            break;
                        }
                        ////robot.ShowText("START :GOTO_BACK_FRONTLINE_READY");
                        if (rb.SendCmdPosPallet(RequestCommandPosPallet.REQUEST_GOBACK_FRONTLINE_TURN_RIGHT))
                        {
                            Stopwatch sw = new Stopwatch();
                            sw.Start();
                            do
                            {
                                robot.onFlagGoBackReady = true;
                                if (resCmd == ResponseCommand.RESPONSE_FINISH_GOBACK_FRONTLINE)
                                {
                                    robot.onFlagGoBackReady = false;
                                   
                                    Pose destPos2 = BuffToGate.GetFrontLineBuffer();
                                    String destName2 = Traffic.DetermineArea(destPos2.Position, TypeZone.MAIN_ZONE);
                                    if (destName2.Equals("OUTER"))
                                    {
                                        ////robot.ShowText("GO FRONTLINE IN OUTER");
                                        if (rb.SendPoseStamped(BuffToGate.GetFrontLineBuffer()))
                                        {
                                            resCmd = ResponseCommand.RESPONSE_NONE;
                                            StateBufferToGate = BufferToGate.BUFGATE_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER;
                                            registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                            registryRobotJourney.startPoint = robot.properties.pose.Position;
                                            registryRobotJourney.endPoint = destPos2.Position;
                                            ////robot.ShowText("BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER");
                                        }
                                    }
                                    else if (destName2.Equals("VIM"))
                                    {
                                        ////robot.ShowText("GO FRONTLINE IN VIM");
                                        if (rb.SendPoseStamped(BuffToGate.GetFrontLineBuffer()))
                                        {
                                            resCmd = ResponseCommand.RESPONSE_NONE;
                                            StateBufferToGate = BufferToGate.BUFGATE_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_FROM_VIM_READY;
                                            registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                            registryRobotJourney.startPoint = robot.properties.pose.Position;
                                            registryRobotJourney.endPoint = destPos2.Position;
                                            ////robot.ShowText("BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER");
                                            ////robot.ShowText("CHECK - REG");
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
                                    ////robot.ShowText("BUFGATE_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER");
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
                                StateBufferToGate = BufferToGate.BUFGATE_ROBOT_SEND_CMD_PICKUP_PALLET_BUFFER;

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
                    case BufferToGate.BUFGATE_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_FROM_VIM_REG:

                        if (TrafficRountineConstants.DetetectInsideStationCheck(registryRobotJourney))
                        {
                            break;
                        }
                        else
                        {
                            StateBufferToGate = BufferToGate.BUFGATE_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_FROM_VIM;
                        }
                        break;
                    case BufferToGate.BUFGATE_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_FROM_VIM:
                        TrafficRountineConstants.DetectRelease(registryRobotJourney);
                        if (TrafficCheckInBuffer(goalFrontLinePos, bayId))
                            break;
                        try
                        {
                            if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                            {
                                resCmd = ResponseCommand.RESPONSE_NONE;
                                StateBufferToGate = BufferToGate.BUFGATE_ROBOT_SEND_CMD_PICKUP_PALLET_BUFFER;
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
                            if (TrafficCheckInBuffer(goalFrontLinePos, bayId))
                                break;
                            if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                            {
                                resCmd = ResponseCommand.RESPONSE_NONE;
                                StateBufferToGate = BufferToGate.BUFGATE_ROBOT_SEND_CMD_PICKUP_PALLET_BUFFER;

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
                    case BufferToGate.BUFGATE_ROBOT_SEND_CMD_PICKUP_PALLET_BUFFER:
                        String palletInfo = JsonConvert.SerializeObject(BuffToGate.GetInfoOfPalletBuffer(PistonPalletCtrl.PISTON_PALLET_UP));
                        if (rb.SendCmdAreaPallet(palletInfo))
                        {
                            // rb.SendCmdLineDetectionCtrl(RequestCommandLineDetect.REQUEST_LINEDETECT_PALLETUP);
                            //rb.prioritLevel.OnAuthorizedPriorityProcedure = true;
                            StateBufferToGate = BufferToGate.BUFGATE_ROBOT_WAITTING_PICKUP_PALLET_BUFFER;
                            ////robot.ShowText("BUFGATE_ROBOT_WAITTING_PICKUP_PALLET_BUFFER");
                        }
                        break;
                    case BufferToGate.BUFGATE_ROBOT_WAITTING_PICKUP_PALLET_BUFFER:
                        if (resCmd == ResponseCommand.RESPONSE_LINEDETECT_PALLETUP)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            BuffToGate.UpdatePalletState(PalletStatus.F, order.palletId_H,order.planId);
                            StateBufferToGate = BufferToGate.BUFGATE_ROBOT_WAITTING_GOBACK_FRONTLINE_BUFFER;
                            ////robot.ShowText("BUFGATE_ROBOT_WAITTING_GOBACK_FRONTLINE_BUFFER");
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
                            robot.bayId = -1;
                            robot.bayIdReg = false;
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
                        Pose destPos = ds.config.PointFrontLine;
                        String destName = Traffic.DetermineArea(destPos.Position, TypeZone.MAIN_ZONE);
                            // đi tới đầu line cổng theo tọa độ chỉ định. gate 1 , 2, 3
                        if (rb.SendPoseStamped(ds.config.PointFrontLine))
                        {
                                StateBufferToGate = BufferToGate.BUFGATE_ROBOT_WAITTING_GOTO_GATE_FROM_VIM_REG;
                                // Cap Nhat Thong Tin CHuyen Di
                                registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position);
                                registryRobotJourney.startPoint = robot.properties.pose.Position;
                                registryRobotJourney.endPoint = ds.config.PointFrontLine.Position;
                                ////robot.ShowText("FORBUF_ROBOT_WAITTING_GOTO_GATE");
                        }
                        break;
                    case BufferToGate.BUFGATE_ROBOT_WAITTING_GOTO_GATE_FROM_VIM_REG:
                        if (TrafficRountineConstants.DetetectInsideStationCheck(registryRobotJourney))
                        {
                            break;
                        }
                        else
                        {
                            StateBufferToGate = BufferToGate.BUFGATE_ROBOT_WAITTING_GOTO_GATE_FROM_VIM;
                        }
                        break;

                    case BufferToGate.BUFGATE_ROBOT_WAITTING_GOTO_GATE_FROM_VIM:
                        TrafficRountineConstants.DetectRelease(registryRobotJourney);
                        if (Traffic.RobotIsInArea("C5", rb.properties.pose.Position))
                        {
                            ds.setDoorBusy(true);
                            ds.openDoor(DoorService.DoorType.DOOR_BACK);
                            StateBufferToGate = BufferToGate.BUFGATE_ROBOT_WAITTING_GOTO_GATE_FROM_VIM_OPEN_DOOR;
                        }

                        break;
                    case BufferToGate.BUFGATE_ROBOT_WAITTING_GOTO_GATE_FROM_VIM_OPEN_DOOR:
                        TrafficRountineConstants.DetectRelease(registryRobotJourney);
                        if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            //rb.prioritLevel.OnAuthorizedPriorityProcedure = true;
                            StateBufferToGate = BufferToGate.BUFGATE_ROBOT_CAME_GATE_POSITION;
                            ////robot.ShowText("BUFGATE_ROBOT_CAME_GATE_POSITION");
                        }
                        break;
                    case BufferToGate.BUFGATE_ROBOT_WAITTING_GOTO_GATE:
                        if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            //rb.prioritLevel.OnAuthorizedPriorityProcedure = true;
                            StateBufferToGate = BufferToGate.BUFGATE_ROBOT_CAME_GATE_POSITION;
                            ////robot.ShowText("BUFGATE_ROBOT_CAME_GATE_POSITION");
                        }
                        break;
                    case BufferToGate.BUFGATE_ROBOT_CAME_GATE_POSITION: // da den khu vuc cong , gui yeu cau mo cong.
                      //  ds.setDoorBusy(true);
                     //   ds.openDoor(DoorService.DoorType.DOOR_BACK);
                        StateBufferToGate = BufferToGate.BUFGATE_ROBOT_WAITTING_OPEN_DOOR;
                        ////robot.ShowText("BUFGATE_ROBOT_WAITTING_OPEN_DOOR");
                        break;
                    case BufferToGate.BUFGATE_ROBOT_WAITTING_OPEN_DOOR: //doi mo cong
                        RetState ret = ds.checkOpen(DoorService.DoorType.DOOR_BACK);
                        if (ret == RetState.DOOR_CTRL_SUCCESS)
                        {
                            if (rb.SendCmdAreaPallet(ds.config.infoPallet))
                            {
                                StateBufferToGate = BufferToGate.BUFGATE_ROBOT_WAITTING_DROPDOWN_PALLET_BUFFER;
                                ////robot.ShowText("BUFGATE_ROBOT_WAITTING_DROPDOWN_PALLET_BUFFER");
                            }
                        }
                        else if (ret == RetState.DOOR_CTRL_ERROR)
                        {
                            robot.ShowText("BUFGATE_ROBOT_WAITTING_OPEN_DOOR_ERROR__(-_-)");
                            Thread.Sleep(1000);
                            ds.setDoorBusy(true);
                            ds.openDoor(DoorService.DoorType.DOOR_BACK);
                        }
                        break;
                    case BufferToGate.BUFGATE_ROBOT_WAITTING_DROPDOWN_PALLET_BUFFER: // doi robot gap hang
                        if (resCmd == ResponseCommand.RESPONSE_LINEDETECT_PALLETDOWN)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            // BuffToGate.UpdatePalletState(PalletStatus.W);
                            StateBufferToGate = BufferToGate.BUFGATE_ROBOT_WAITTING_GOBACK_FRONTLINE_GATE;
                            ////robot.ShowText("BUFGATE_ROBOT_WAITTING_GOBACK_FRONTLINE_GATE");
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
                            ds.setDoorBusy(false);
                            ds.LampSetStateOn(DoorType.DOOR_BACK);
                            StateBufferToGate = BufferToGate.BUFGATE_ROBOT_WAITTING_CLOSE_GATE;
                            ////robot.ShowText("BUFGATE_ROBOT_WAITTING_CLOSE_GATE");
                        }
                        else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                        {
                            errorCode = ErrorCode.DETECT_LINE_ERROR;
                            CheckUserHandleError(this);
                        }
                        break;
                    case BufferToGate.BUFGATE_ROBOT_WAITTING_CLOSE_GATE: // doi dong cong.
                        StateBufferToGate = BufferToGate.BUFGATE_ROBOT_RELEASED;
                        ////robot.ShowText("BUFGATE_ROBOT_WAITTING_CLOSE_GATE");
                        break;

                    case BufferToGate.BUFGATE_ROBOT_RELEASED: // trả robot về robotmanagement để nhận quy trình mới
                        ds.removeListCtrlDoorBack();
                        robot.bayId = -1;
                        robot.bayIdReg = false;
                        TrafficRountineConstants.ReleaseAll(robot);
                        robot.robotTag = RobotStatus.IDLE;
                        rb.PreProcedureAs = ProcedureControlAssign.PRO_BUFFER_TO_GATE;
                        ReleaseProcedureHandler(this);
                        ProRun = false;
                        UpdateInformationInProc(this, ProcessStatus.S);
                        order.endTimeProcedure = DateTime.Now;
                        order.totalTimeProcedure = order.endTimeProcedure.Subtract(order.startTimeProcedure).TotalMinutes;
                        KillEvent();
                        break;
                    case BufferToGate.BUFGATE_ROBOT_DESTROY:
                        ds.removeListCtrlDoorBack();
                        ProRunStopW = false;
                        ProRunStopW = false;
                        robot.robotTag = RobotStatus.IDLE;
                        ProRun = false;
                        order.endTimeProcedure = DateTime.Now;
                        order.totalTimeProcedure = order.endTimeProcedure.Subtract(order.startTimeProcedure).TotalMinutes;
                        TrafficRountineConstants.ReleaseAll(robot);
                        break;
                    default:
                        break;
                }
                //robot.ShowText("-> " + procedureCode);
                Thread.Sleep(500);
            }
            StateBufferToGate = BufferToGate.BUFGATE_IDLE;
        }

        public override void FinishStatesCallBack(Int32 message)
        {
            this.resCmd = (ResponseCommand)message;
            base.FinishStatesCallBack(message);
            if (this.resCmd == ResponseCommand.RESPONSE_FINISH_GOBACK_FRONTLINE)
            {
                robot.ReleaseWorkingZone();
            }
        }
    }
}
