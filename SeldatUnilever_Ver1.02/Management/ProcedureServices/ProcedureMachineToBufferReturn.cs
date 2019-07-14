using SeldatMRMS;
using System;
using System.Diagnostics;
using System.Threading;
using SeldatMRMS.Management.RobotManagent;
using SeldatMRMS.Management.TrafficManager;
using static SeldatMRMS.Management.RobotManagent.RobotBaseService;
using static SeldatMRMS.Management.RobotManagent.RobotUnityControl;
using static SeldatMRMS.Management.TrafficRobotUnity;
using static SelDatUnilever_Ver1._00.Management.DeviceManagement.DeviceItem;

namespace SeldatUnilever_Ver1._02.Management.ProcedureServices
{
    public class ProcedureMachineToBufferReturn:ProcedureControlServices
    {
        MachineToBufferReturn StateMachineToBufferReturn;
        Thread ProMachineToBufferReturn;
        public RobotUnity robot;
        ResponseCommand resCmd;
        TrafficManagementService Traffic;
        public override event Action<Object> ReleaseProcedureHandler;
        // public override event Action<Object> ErrorProcedureHandler;
        public ProcedureMachineToBufferReturn(RobotUnity robot, TrafficManagementService traffiicService) : base(robot)
        {
            StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_IDLE;
            this.robot = robot;
            this.Traffic = traffiicService;
            procedureCode = ProcedureCode.PROC_CODE_MACHINE_TO_BUFFER_RETURN;
        }

        public void Start(MachineToBufferReturn state = MachineToBufferReturn.MACBUFRET_ROBOT_GOTO_FRONTLINE_MACHINE)
        {
            robot.orderItem = null;
            errorCode = ErrorCode.RUN_OK;
            robot.robotTag = RobotStatus.WORKING;
            robot.ProcedureAs = ProcedureControlAssign.PRO_MACHINE_TO_BUFFER_RETURN;
            StateMachineToBufferReturn = state;
            ProMachineToBufferReturn = new Thread(this.Procedure);
            ProMachineToBufferReturn.Start(this);
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
            ProRunStopW = false;
            robot.orderItem = null;
            robot.robotTag = RobotStatus.IDLE;
            // StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_RELEASED;
            //robot.prioritLevel.OnAuthorizedPriorityProcedure = false;
            ProRun = false;
            UpdateInformationInProc(this, ProcessStatus.F);
            order.status = StatusOrderResponseCode.ROBOT_ERROR;
            selectHandleError = SelectHandleError.CASE_ERROR_EXIT;
            order.endTimeProcedure = DateTime.Now;
            order.totalTimeProcedure = order.endTimeProcedure.Subtract(order.startTimeProcedure).TotalMinutes;
            SaveOrderItem(order);
            //   this.robot.DestroyRegistrySolvedForm();
        }
        public void Procedure(object ojb)
        {
            ProcedureMachineToBufferReturn BfToBufRe = (ProcedureMachineToBufferReturn)ojb;
            RobotUnity rb = BfToBufRe.robot;
            TrafficManagementService Traffic = BfToBufRe.Traffic;
            rb.mcuCtrl.TurnOnLampRb();
            robot.ShowText(" Start -> " + procedureCode);
            while (ProRun)
            {
                switch (StateMachineToBufferReturn)
                {
                    case MachineToBufferReturn.MACBUFRET_IDLE:
                        break;
                    case MachineToBufferReturn.MACBUFRET_ROBOT_GOTO_FRONTLINE_MACHINE: // doi khu vuc buffer san sang de di vao
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
                                            resCmd = ResponseCommand.RESPONSE_NONE;
                                            if (rb.SendPoseStamped(BfToBufRe.GetFrontLineMachine()))
                                            {
                                                StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE;
                                                robot.ShowText("MACBUFRET_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE");
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
                                if (rb.SendPoseStamped(BfToBufRe.GetFrontLineMachine()))
                                {
                                    StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE;
                                    robot.ShowText("MACBUFRET_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE");
                                }
                            }
                        }
                        catch (System.Exception)
                        {
                            errorCode = ErrorCode.CAN_NOT_GET_DATA;
                            CheckUserHandleError(this);
                        }
                        break;
                    case MachineToBufferReturn.MACBUFRET_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE:
                        try
                        {
                            if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                            //if (robot.ReachedGoal())
                            {
                                resCmd = ResponseCommand.RESPONSE_NONE;
                                if (rb.SendCmdAreaPallet(BfToBufRe.GetInfoOfPalletMachine(PistonPalletCtrl.PISTON_PALLET_UP)))
                                {
                                    // rb.SendCmdLineDetectionCtrl(RequestCommandLineDetect.REQUEST_LINEDETECT_PALLETUP);
                                    //rb.prioritLevel.OnAuthorizedPriorityProcedure = true;
                                    StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_WAITTING_PICKUP_PALLET_MACHINE;
                                    robot.ShowText("MACBUFRET_ROBOT_WAITTING_PICKUP_PALLET_MACHINE");
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
                    // case MachineToBufferReturn.MACBUFRET_ROBOT_GOTO_PICKUP_PALLET_MACHINE:
                    //     if (true == rb.CheckPointDetectLine(BfToBufRe.GetPointPallet(), rb))
                    //     {
                    //         rb.SendCmdPosPallet(RequestCommandPosPallet.REQUEST_LINEDETECT_COMING_POSITION);
                    //         StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_WAITTING_PICKUP_PALLET_MACHINE;
                    //     }
                    //     break;
                    case MachineToBufferReturn.MACBUFRET_ROBOT_WAITTING_PICKUP_PALLET_MACHINE:
                        if (resCmd == ResponseCommand.RESPONSE_LINEDETECT_PALLETUP)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            BfToBufRe.UpdatePalletState(PalletStatus.F);
                            StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_WAITTING_GOBACK_FRONTLINE_MACHINE;
                            robot.ShowText("MACBUFRET_ROBOT_WAITTING_GOBACK_FRONTLINE_MACHINE");
                        }
                        else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                        {
                            errorCode = ErrorCode.DETECT_LINE_ERROR;
                            CheckUserHandleError(this);
                        }
                        break;
                    case MachineToBufferReturn.MACBUFRET_ROBOT_WAITTING_GOBACK_FRONTLINE_MACHINE: // đợi
                        try
                        {
                            if (resCmd == ResponseCommand.RESPONSE_FINISH_GOBACK_FRONTLINE)
                            {
                                resCmd = ResponseCommand.RESPONSE_NONE;
                                //rb.prioritLevel.OnAuthorizedPriorityProcedure = false;
                                if (rb.SendPoseStamped(BfToBufRe.GetCheckInReturn()))
                                {
                                    StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_GOTO_CHECKIN_BUFFER_RETURN;
                                    robot.ShowText("MACBUFRET_ROBOT_GOTO_CHECKIN_RETURN");
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
                    case MachineToBufferReturn.MACBUFRET_ROBOT_GOTO_CHECKIN_BUFFER_RETURN: // dang di
                        if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                        //if (robot.ReachedGoal())
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            //rb.prioritLevel.OnAuthorizedPriorityProcedure = true;
                            rb.UpdateRiskAraParams(0, rb.properties.L2, rb.properties.WS, rb.properties.DistInter);
                            StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_CAME_CHECKIN_BUFFER_RETURN;
                            robot.ShowText("MACBUFRET_ROBOT_CAME_CHECKIN_RETURN");
                        }
                        break;
                    case MachineToBufferReturn.MACBUFRET_ROBOT_CAME_CHECKIN_BUFFER_RETURN: // đã đến vị trí
                        try
                        {

                            if (false == robot.CheckInZoneBehavior(BfToBufRe.GetFrontLineBuffer().Position))
                            {
                                Global_Object.onFlagRobotComingGateBusy = true;
                                rb.UpdateRiskAraParams(40, rb.properties.L2, rb.properties.WS, rb.properties.DistInter);
                                //rb.prioritLevel.OnAuthorizedPriorityProcedure = false;
                                if (rb.SendPoseStamped(BfToBufRe.GetFrontLineReturn()))
                                {
                                    StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_GOTO_FRONTLINE_BUFFER_RETURN;
                                    robot.ShowText("MACBUFRET_ROBOT_GOTO_FRONTLINE_RETURN");
                                }
                            }
                        }
                        catch (System.Exception)
                        {
                            errorCode = ErrorCode.CAN_NOT_GET_DATA;
                            CheckUserHandleError(this);
                        }
                        break;
                    case MachineToBufferReturn.MACBUFRET_ROBOT_GOTO_FRONTLINE_BUFFER_RETURN: // dang di
                        try
                        {
                            if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                            //if ( robot.ReachedGoal())
                            {
                                resCmd = ResponseCommand.RESPONSE_NONE;

                                if (rb.SendCmdAreaPallet(BfToBufRe.GetInfoOfPalletReturn(PistonPalletCtrl.PISTON_PALLET_DOWN)))
                                {
                                    //rb.prioritLevel.OnAuthorizedPriorityProcedure = true;
                                    StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_WAITTING_DROPDOWN_PALLET;
                                    robot.ShowText("MACBUFRET_ROBOT_WAITTING_DROPDOWN_PALLET");
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
                    // case MachineToBufferReturn.MACBUFRET_ROBOT_CAME_CHECKIN_RETURN: // đã đến vị trí
                    //     if (false == Traffic.HasRobotUnityinArea(BfToBufRe.GetFrontLineReturn().Position))
                    //     {
                    //         rb.SendPoseStamped(BfToBufRe.GetFrontLineReturn());
                    //         StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_GOTO_FRONTLINE_DROPDOWN_PALLET;
                    //     }
                    //     break;
                    // case MachineToBufferReturn.MACBUFRET_ROBOT_GOTO_FRONTLINE_DROPDOWN_PALLET:
                    //     if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                    //     {
                    //         resCmd = ResponseCommand.RESPONSE_NONE;
                    //         StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_CAME_FRONTLINE_DROPDOWN_PALLET;
                    //     }
                    //     break;
                    // case MachineToBufferReturn.MACBUFRET_ROBOT_CAME_FRONTLINE_DROPDOWN_PALLET:  // đang trong tiến trình dò line và thả pallet
                    //     rb.SendCmdLineDetectionCtrl(RequestCommandLineDetect.REQUEST_LINEDETECT_PALLETDOWN);
                    //     StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_WAITTING_GOTO_POINT_DROP_PALLET;
                    //     break;
                    // case MachineToBufferReturn.MACBUFRET_ROBOT_WAITTING_GOTO_POINT_DROP_PALLET:
                    //     if (true == rb.CheckPointDetectLine(BfToRe.GetPointPallet(), rb))
                    //     {
                    //         rb.SendCmdPosPallet(RequestCommandPosPallet.REQUEST_LINEDETECT_COMING_POSITION);
                    //         StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_WAITTING_DROPDOWN_PALLET;
                    //     }
                    //     break;
                    case MachineToBufferReturn.MACBUFRET_ROBOT_WAITTING_DROPDOWN_PALLET:
                        if (resCmd == ResponseCommand.RESPONSE_LINEDETECT_PALLETDOWN)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            BfToBufRe.UpdatePalletState(PalletStatus.W);
                            StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_WAITTING_GOTO_FRONTLINE;
                            robot.ShowText("MACBUFRET_ROBOT_WAITTING_GOTO_FRONTLINE");
                        }
                        else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                        {
                            errorCode = ErrorCode.DETECT_LINE_ERROR;
                            CheckUserHandleError(this);
                        }
                        break;
                    case MachineToBufferReturn.MACBUFRET_ROBOT_WAITTING_GOTO_FRONTLINE:
                        if (resCmd == ResponseCommand.RESPONSE_FINISH_GOBACK_FRONTLINE)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            //rb.prioritLevel.OnAuthorizedPriorityProcedure = false;
                            StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_RELEASED;
                            robot.ShowText("MACBUFRET_ROBOT_RELEASED");
                        }
                        else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                        {
                            errorCode = ErrorCode.DETECT_LINE_ERROR;
                            CheckUserHandleError(this);
                        }
                        break;
                    case MachineToBufferReturn.MACBUFRET_ROBOT_RELEASED: // trả robot về robotmanagement để nhận quy trình mới
                        Global_Object.onFlagRobotComingGateBusy = false;
                        robot.orderItem = null;
                        //   robot.robotTag = RobotStatus.IDLE;
                        rb.PreProcedureAs = ProcedureControlAssign.PRO_MACHINE_TO_BUFFER_RETURN;
                        // if (errorCode == ErrorCode.RUN_OK) {
                        ReleaseProcedureHandler(this);
                        // } else {
                        //     ErrorProcedureHandler (this);
                        // }
                        ProRun = false;
                        robot.ShowText("RELEASED");
                        UpdateInformationInProc(this, ProcessStatus.S);
                        order.status = StatusOrderResponseCode.FINISHED;
                        order.endTimeProcedure = DateTime.Now;
                        order.totalTimeProcedure = order.endTimeProcedure.Subtract(order.startTimeProcedure).TotalMinutes;
                        SaveOrderItem(order);
                        KillEvent();
                        break;
                    default:
                        break;
                }
                Thread.Sleep(5);
            }
            StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_IDLE;
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
