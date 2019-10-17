using System;
using System.Diagnostics;
using System.Threading;
using SeldatMRMS.Management.RobotManagent;
using SeldatMRMS.Management.TrafficManager;
using static SeldatMRMS.Management.RobotManagent.RobotBaseService;
using static SeldatMRMS.Management.RobotManagent.RobotUnity;
using static SeldatMRMS.Management.RobotManagent.RobotUnityControl;
using static SeldatMRMS.Management.TrafficRobotUnity;
using static SelDatUnilever_Ver1._00.Management.DeviceManagement.DeviceItem;
using SeldatMRMS;
using SeldatMRMS.Management.DoorServices;
using DoorControllerService;
using static DoorControllerService.DoorService;

namespace SeldatUnilever_Ver1._02.Management.ProcedureServices
{
    public class ProcedureMachineToGate : ProcedureControlServices
    {
        // DataBufferToGate points;
        MachineToGate StateMachineToGate;
        Thread ProBufferToGate;
        public RobotUnity robot;
        public DoorManagementService door;
        ResponseCommand resCmd;
        TrafficManagementService Traffic;
        private DoorService ds;

        public override event Action<Object> ReleaseProcedureHandler;
        // public override event Action<Object> ErrorProcedureHandler;
        public ProcedureMachineToGate(RobotUnity robot, DoorManagementService doorservice, TrafficManagementService traffiicService) : base(robot)
        {
            StateMachineToGate = MachineToGate.MACGATE_IDLE;
            resCmd = ResponseCommand.RESPONSE_NONE;
            this.robot = robot;
            base.robot = robot;
            // this.points = new DataBufferToGate();
            this.door = doorservice;
            this.Traffic = traffiicService;
            errorCode = ErrorCode.RUN_OK;
            procedureCode = ProcedureCode.PROC_CODE_MACHINE_TO_GATE;
        }

        public void Start(MachineToGate state = MachineToGate.MACGATE_ROBOT_GOTO_FRONTLINE_MACHINE)
        {
            errorCode = ErrorCode.RUN_OK;
            robot.robotTag = RobotStatus.WORKING;
            robot.ProcedureAs = ProcedureControlAssign.PRO_MACHINE_TO_GATE;
            StateMachineToGate = state;
            ProBufferToGate = new Thread(this.Procedure);
            ProRun = true;
            ProRunStopW = true;
            //robot.prioritLevel.OnAuthorizedPriorityProcedure = false;
            order.startTimeProcedure = DateTime.Now;
            ProBufferToGate.Start(this);
        }
        public void Destroy()
        {
            if (ds != null)
            {
                ds.LampSetStateOff(DoorType.DOOR_BACK);
                ds.setDoorBusy(false);
                ds.removeListCtrlDoorBack();
            }
            ProRunStopW = false;
            robot.orderItem = null;
            robot.SwitchToDetectLine(false);
            robot.robotTag = RobotStatus.IDLE;
            robot.ReleaseWorkingZone();
            //robot.prioritLevel.OnAuthorizedPriorityProcedure = false;
            ProRun = false;
            UpdateInformationInProc(this, ProcessStatus.F);
            order.status = StatusOrderResponseCode.ROBOT_ERROR;
            selectHandleError = SelectHandleError.CASE_ERROR_EXIT;
        }
        public void Procedure(object ojb)
        {
            ProcedureMachineToGate MaToGate = (ProcedureMachineToGate)ojb;
            RobotUnity rb = MaToGate.robot;
            TrafficManagementService Traffic = MaToGate.Traffic;
            ds = MaToGate.door.DoorMezzamineReturn;
            ds.setRb(rb);
            rb.mcuCtrl.lampRbOn();
            robot.ShowText(" Start -> " + procedureCode);
            while (ProRun)
            {
                switch (StateMachineToGate)
                {
                    case MachineToGate.MACGATE_IDLE:
                        break;
                    case MachineToGate.MACGATE_ROBOT_GOTO_FRONTLINE_MACHINE: // doi khu vuc buffer san sang de di vao
                        try
                        {
                            if (rb.PreProcedureAs == ProcedureControlAssign.PRO_READY)
                            {
                                if (rb.SendCmdPosPallet(RequestCommandPosPallet.REQUEST_GOBACK_FRONTLINE_TURN_RIGHT))
                                {
                                    Stopwatch sw = new Stopwatch();
                                    sw.Start();
                                    do
                                    {
                                        if (resCmd == ResponseCommand.RESPONSE_FINISH_GOBACK_FRONTLINE)
                                        {
                                            
                                            if (rb.SendPoseStamped(MaToGate.GetFrontLineMachine()))
                                            {
                                                resCmd = ResponseCommand.RESPONSE_NONE;
                                                StateMachineToGate = MachineToGate.MACGATE_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE;
                                                robot.ShowText("MACGATE_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE");
                                                break;
                                            }

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
                            }
                            else
                            {
                                if (rb.SendPoseStamped(MaToGate.GetFrontLineMachine()))
                                {
                                    StateMachineToGate = MachineToGate.MACGATE_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE;
                                    robot.ShowText("MACGATE_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE");
                                }
                            }
                        }
                        catch (System.Exception)
                        {
                            errorCode = ErrorCode.CAN_NOT_GET_DATA;
                            CheckUserHandleError(this);
                        }
                        break;
                    case MachineToGate.MACGATE_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE:
                        try
                        {
                            if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                            {
                                resCmd = ResponseCommand.RESPONSE_NONE;
                                StateMachineToGate = MachineToGate.MACGATE_ROBOT_SEND_CMD_CAME_FRONTLINE_MACHINE;

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
                    case MachineToGate.MACGATE_ROBOT_SEND_CMD_CAME_FRONTLINE_MACHINE:
                        if (rb.SendCmdAreaPallet(MaToGate.GetInfoOfPalletMachine(PistonPalletCtrl.PISTON_PALLET_UP)))
                        {
                            StateMachineToGate = MachineToGate.MACGATE_ROBOT_WAITTING_PICKUP_PALLET_MACHINE;
                            robot.ShowText("MACGATE_ROBOT_WAITTING_PICKUP_PALLET_MACHINE");
                        }
                        break;
                    case MachineToGate.MACGATE_ROBOT_WAITTING_PICKUP_PALLET_MACHINE:
                        if (resCmd == ResponseCommand.RESPONSE_LINEDETECT_PALLETUP)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                          //  MaToGate.UpdatePalletState(PalletStatus.F);
                            StateMachineToGate = MachineToGate.MACGATE_ROBOT_WAITTING_GOBACK_FRONTLINE_MACHINE;
                            robot.ShowText("MACGATE_ROBOT_WAITTING_GOBACK_FRONTLINE_MACHINE");
                        }
                        else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                        {
                            errorCode = ErrorCode.DETECT_LINE_ERROR;
                            CheckUserHandleError(this);
                        }
                        break;
                    case MachineToGate.MACGATE_ROBOT_WAITTING_GOBACK_FRONTLINE_MACHINE: // đợi
                        try
                        {
                            if (resCmd == ResponseCommand.RESPONSE_FINISH_GOBACK_FRONTLINE)
                            {
                               
                                //rb.prioritLevel.OnAuthorizedPriorityProcedure = false;
                                if (rb.SendPoseStamped(ds.config.PointCheckInGate))
                                {
                                    resCmd = ResponseCommand.RESPONSE_NONE;
                                    StateMachineToGate = MachineToGate.MACGATE_ROBOT_WAITTING_GOTO_CHECKIN_GATE;
                                    robot.ShowText("MACGATE_ROBOT_WAITTING_GOTO_CHECKIN_GATE");
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
                    case MachineToGate.MACGATE_ROBOT_WAITTING_GOTO_CHECKIN_GATE:
                        if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            //rb.prioritLevel.OnAuthorizedPriorityProcedure = true;
                            StateMachineToGate = MachineToGate.MACGATE_ROBOT_CAME_CHECKIN_GATE;
                            robot.ShowText("MACGATE_ROBOT_CAME_CHECKIN_GATE");
                        }
                        break;
                    case MachineToGate.MACGATE_ROBOT_CAME_CHECKIN_GATE: // đã đến vị trí, kiem tra va cho khu vuc cong san sang de di vao.
                        if (false == robot.CheckInZoneBehavior(ds.config.PointFrontLine.Position))
                        {
                            //rb.prioritLevel.OnAuthorizedPriorityProcedure = false;
                            if (rb.SendPoseStamped(ds.config.PointFrontLine))
                            {
                                StateMachineToGate = MachineToGate.MACGATE_ROBOT_WAITTING_GOTO_GATE;
                                robot.ShowText("MACGATE_ROBOT_WAITTING_GOTO_GATE");
                            }
                        }
                        break;
                    case MachineToGate.MACGATE_ROBOT_WAITTING_GOTO_GATE:
                        if (Traffic.RobotIsInArea("GATE3", rb.properties.pose.Position))
                        {
                            ds.setDoorBusy(true);
                            ds.openDoor(DoorService.DoorType.DOOR_BACK);
                            StateMachineToGate = MachineToGate.MACGATE_ROBOT_WAITTING_GOTO_GATE_OPENDOOR;
                        }
                        break;
                    case MachineToGate.MACGATE_ROBOT_WAITTING_GOTO_GATE_OPENDOOR:
                        if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            //rb.prioritLevel.OnAuthorizedPriorityProcedure = true;
                            StateMachineToGate = MachineToGate.MACGATE_ROBOT_CAME_GATE_POSITION;
                            robot.ShowText("MACGATE_ROBOT_CAME_GATE_POSITION");
                        }
                        break;
                    case MachineToGate.MACGATE_ROBOT_CAME_GATE_POSITION: // da den khu vuc cong , gui yeu cau mo cong.
                        //ds.setDoorBusy(true);
                        //ds.openDoor(DoorService.DoorType.DOOR_BACK);
                        StateMachineToGate = MachineToGate.MACGATE_ROBOT_WAITTING_OPEN_DOOR;
                        robot.ShowText("MACGATE_ROBOT_WAITTING_OPEN_DOOR");
                        break;
                    case MachineToGate.MACGATE_ROBOT_WAITTING_OPEN_DOOR: //doi mo cong
                        RetState ret = ds.checkOpen(DoorService.DoorType.DOOR_BACK);
                        if (ret == RetState.DOOR_CTRL_SUCCESS)
                        {
                            if (rb.SendCmdAreaPallet(ds.config.infoPallet))
                            {
                                StateMachineToGate = MachineToGate.MACGATE_ROBOT_WAITTING_DROPDOWN_PALLET_RETURN;
                                robot.ShowText("MACGATE_ROBOT_WAITTING_DROPDOWN_PALLET_RETURN");
                            }
                        }
                        else if (ret == RetState.DOOR_CTRL_ERROR)
                        {
                            robot.ShowText("MACGATE_ROBOT_WAITTING_OPEN_DOOR_ERROR__(-_-)");
                            Thread.Sleep(1000);
                            ds.setDoorBusy(true);
                            ds.openDoor(DoorService.DoorType.DOOR_BACK);
                        }
                        break;
                    // case MachineToGate.MACGATE_ROBOT_OPEN_DOOR_SUCCESS: // mo cua thang cong ,gui toa do line de robot di vao
                    //     rb.SendCmdLineDetectionCtrl(RequestCommandLineDetect.REQUEST_LINEDETECT_PALLETDOWN);
                    //     StateMachineToGate = MachineToGate.MACGATE_ROBOT_GOTO_POSITION_PALLET_RETURN;
                    //     break;
                    // case MachineToGate.MACGATE_ROBOT_GOTO_POSITION_PALLET_RETURN:
                    //     if (true == rb.CheckPointDetectLine(ds.config.PointOfPallet, rb))
                    //     {
                    //         rb.SendCmdPosPallet(RequestCommandPosPallet.REQUEST_LINEDETECT_COMING_POSITION);
                    //         StateMachineToGate = MachineToGate.MACGATE_ROBOT_WAITTING_DROPDOWN_PALLET_RETURN;
                    //     }
                    //     break;
                    case MachineToGate.MACGATE_ROBOT_WAITTING_DROPDOWN_PALLET_RETURN: // doi robot gap hang
                        if (resCmd == ResponseCommand.RESPONSE_LINEDETECT_PALLETDOWN)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            // ReToGate.UpdatePalletState(PalletStatus.W);
                            StateMachineToGate = MachineToGate.MACGATE_ROBOT_WAITTING_GOBACK_FRONTLINE_GATE;
                            robot.ShowText("MACGATE_ROBOT_WAITTING_GOBACK_FRONTLINE_GATE");
                        }
                        else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                        {
                            errorCode = ErrorCode.DETECT_LINE_ERROR;
                            CheckUserHandleError(this);
                        }
                        break;
                    case MachineToGate.MACGATE_ROBOT_WAITTING_GOBACK_FRONTLINE_GATE:
                        if (resCmd == ResponseCommand.RESPONSE_FINISH_GOBACK_FRONTLINE)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            ds.closeDoor(DoorService.DoorType.DOOR_BACK);
                            ds.setDoorBusy(false);
                            StateMachineToGate = MachineToGate.MACGATE_ROBOT_WAITTING_CLOSE_GATE;
                            robot.ShowText("MACGATE_ROBOT_WAITTING_CLOSE_GATE");
                        }
                        else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                        {
                            errorCode = ErrorCode.DETECT_LINE_ERROR;
                            CheckUserHandleError(this);
                        }
                        break;
                    case MachineToGate.MACGATE_ROBOT_WAITTING_CLOSE_GATE: // doi dong cong.
                        //if (true == ds.WaitClose(DoorService.DoorType.DOOR_BACK, TIME_OUT_CLOSE_DOOR))
                        //{
                        StateMachineToGate = MachineToGate.MACGATE_ROBOT_RELEASED;
                        //rb.prioritLevel.OnAuthorizedPriorityProcedure = false;
                        robot.ShowText("MACGATE_ROBOT_WAITTING_CLOSE_GATE");
                        //}
                        //else
                        //{
                        //    errorCode = ErrorCode.CLOSE_DOOR_ERROR;
                        //    CheckUserHandleError(this);
                        //}
                        break;

                    case MachineToGate.MACGATE_ROBOT_RELEASED: // trả robot về robotmanagement để nhận quy trình mới
                        robot.robotTag = RobotStatus.IDLE;
                        rb.PreProcedureAs = ProcedureControlAssign.PRO_MACHINE_TO_GATE;
                        ds.removeListCtrlDoorBack();
                        // if (errorCode == ErrorCode.RUN_OK) {
                        ReleaseProcedureHandler(this);
                        // } else {
                        // ErrorProcedureHandler (this);
                        // }
                        ProRun = false;
                        robot.ShowText("RELEASED");
                        UpdateInformationInProc(this, ProcessStatus.S);
                        order.endTimeProcedure = DateTime.Now;
                        order.totalTimeProcedure = order.endTimeProcedure.Subtract(order.startTimeProcedure).TotalMinutes;
                        KillEvent();
                        break;
                    default:
                        break;
                }
                Thread.Sleep(5);
            }
            StateMachineToGate = MachineToGate.MACGATE_IDLE;
        }

        protected override void CheckUserHandleError(object obj)
        {
            if (errorCode == ErrorCode.CAN_NOT_GET_DATA)
            {
                if (!this.Traffic.RobotIsInArea("READY", robot.properties.pose.Position))
                {
                    ProRun = false;
                    robot.setColorRobotStatus(RobotStatusColorCode.ROBOT_STATUS_CAN_NOTGET_DATA);
                    robot.TurnOnSupervisorTraffic(true);

                    robot.PreProcedureAs = robot.ProcedureAs;
                    ReleaseProcedureHandler(obj);
                    return;
                }
                else
                {
                    ProRun = false;
                    robot.setColorRobotStatus(RobotStatusColorCode.ROBOT_STATUS_CAN_NOTGET_DATA);
                    robot.TurnOnSupervisorTraffic(true);

                    return;
                }
            }
            base.CheckUserHandleError(obj);
        }
        public override void FinishStatesCallBack(Int32 message)
        {
            this.resCmd = (ResponseCommand)message;
            if (this.resCmd == ResponseCommand.RESPONSE_FINISH_GOBACK_FRONTLINE)
            {

            }
        }
    }
}
