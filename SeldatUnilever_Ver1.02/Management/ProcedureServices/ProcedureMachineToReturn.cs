using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using SeldatMRMS.Management.RobotManagent;
using SeldatMRMS.Management.TrafficManager;
using SeldatUnilever_Ver1._02.Management.ProcedureServices;
using SeldatUnilever_Ver1._02.Management.TrafficManager;
using static SeldatMRMS.Management.RobotManagent.RobotBaseService;
using static SeldatMRMS.Management.RobotManagent.RobotUnityControl;
using static SeldatMRMS.Management.TrafficRobotUnity;
using static SelDatUnilever_Ver1._00.Management.DeviceManagement.DeviceItem;
using static SelDatUnilever_Ver1._00.Management.TrafficManager.TrafficRounterService;

namespace SeldatMRMS
{
    public class ProcedureMachineToReturn : TrafficProcedureService
    {
        public struct DataMachineToReturn
        {
            // public Pose PointFrontLineMachine;
            // public PointDetect PointPickPallet;
            // public Pose PointCheckInReturn;
            // public Pose PointFrontLineReturn;
            // public PointDetect PointDropPallet;
        }
        DataMachineToReturn points;
        MachineToReturn StateMachineToReturn;
        Thread ProMachineToReturn;
        public RobotUnity robot;
        ResponseCommand resCmd;
        TrafficManagementService Traffic;
        public override event Action<Object> ReleaseProcedureHandler;
        // public override event Action<Object> ErrorProcedureHandler;
        public ProcedureMachineToReturn(RobotUnity robot, TrafficManagementService traffiicService) : base(robot, traffiicService)
        {
            StateMachineToReturn = MachineToReturn.MACRET_IDLE;
            this.robot = robot;
            this.Traffic = traffiicService;
            procedureCode = ProcedureCode.PROC_CODE_MACHINE_TO_RETURN;
        }

        public void Start(MachineToReturn state = MachineToReturn.MACRET_SELECT_BEHAVIOR_ONZONE)
        {
            robot.orderItem = null;
            errorCode = ErrorCode.RUN_OK;
            robot.robotTag = RobotStatus.WORKING;
            robot.ProcedureAs = ProcedureControlAssign.PRO_MACHINE_TO_RETURN;
            StateMachineToReturn = state;
            ProMachineToReturn = new Thread(this.Procedure);
            ProRun = true;
            ProRunStopW = true;
            //robot.prioritLevel.OnAuthorizedPriorityProcedure = false;
            order.startTimeProcedure = DateTime.Now;
            registryRobotJourney = new RegistryRobotJourney();
            registryRobotJourney.robot = robot;
            registryRobotJourney.traffic = Traffic;
            ProMachineToReturn.Start(this);
        }
        public void Destroy()
        {
            ProRunStopW = false;
            robot.orderItem = null;
            robot.robotTag = RobotStatus.IDLE;
            ProRun = false;
            UpdateInformationInProc(this, ProcessStatus.F);
            order.status = StatusOrderResponseCode.ROBOT_ERROR;
            selectHandleError = SelectHandleError.CASE_ERROR_EXIT;
            order.endTimeProcedure = DateTime.Now;
            order.totalTimeProcedure = order.endTimeProcedure.Subtract(order.startTimeProcedure).TotalMinutes;
            TrafficRountineConstants.ReleaseAll(robot);
        }
        public void Procedure(object ojb)
        {
            ProcedureMachineToReturn BfToRe = (ProcedureMachineToReturn)ojb;
            RobotUnity rb = BfToRe.robot;
            DataMachineToReturn p = BfToRe.points;
            TrafficManagementService Traffic = BfToRe.Traffic;
            rb.mcuCtrl.lampRbOn();
            robot.ShowText(" Start -> " + procedureCode);
            while (ProRun)
            {
                switch (StateMachineToReturn)
                {
                    case MachineToReturn.MACRET_IDLE:
                        break;
                    case MachineToReturn.MACRET_SELECT_BEHAVIOR_ONZONE:
                        if (Traffic.RobotIsInArea("READY", robot.properties.pose.Position))
                        {
                            if (rb.PreProcedureAs == ProcedureControlAssign.PRO_READY)
                            {
                                StateMachineToReturn = MachineToReturn.MACRET_ROBOT_GOTO_BACK_FRONTLINE_READY;
                                registryRobotJourney.startPlaceName = Traffic.DetermineArea(BfToRe.GetFrontLineMachine().Position, TypeZone.OPZS);
                                registryRobotJourney.startPoint = robot.properties.pose.Position;
                                registryRobotJourney.endPoint = BfToRe.GetFrontLineMachine().Position;
                            }
                        }
                        else if (Traffic.RobotIsInArea("VIM", robot.properties.pose.Position))
                        {
                            if (rb.SendPoseStamped(BfToRe.GetFrontLineMachine()))
                            {
                                StateMachineToReturn = MachineToReturn.MACRET_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE_FROM_VIM_REG;
                                registryRobotJourney.startPlaceName = Traffic.DetermineArea(BfToRe.GetFrontLineMachine().Position, TypeZone.OPZS);
                                registryRobotJourney.startPoint = robot.properties.pose.Position;
                                registryRobotJourney.endPoint = BfToRe.GetFrontLineMachine().Position;
                                //robot.ShowText("BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER");
                            }
                        }
                        else if (Traffic.RobotIsInArea("OUTER", robot.properties.pose.Position))
                        {
                            Pose destPos1 = BfToRe.GetFrontLineMachine();
                            String destName1 = Traffic.DetermineArea(destPos1.Position, TypeZone.MAIN_ZONE);
                            if (destName1.Equals("OUTER"))
                            {
                                //robot.ShowText("GO FRONTLINE IN OUTER");
                                if (rb.SendPoseStamped(destPos1))
                                {
                                    StateMachineToReturn = MachineToReturn.MACRET_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE;
                                    registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                    registryRobotJourney.startPoint = robot.properties.pose.Position;
                                    registryRobotJourney.endPoint = destPos1.Position;
                                    //robot.ShowText("MACRET_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE");
                                }
                            }
                            else if (destName1.Equals("VIM"))
                            {
                                //robot.ShowText("GO FRONTLINE IN VIM");
                                if (rb.SendPoseStamped(destPos1))
                                {
                                    StateMachineToReturn = MachineToReturn.MACRET_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE_FROM_VIM_REG;
                                    registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                    registryRobotJourney.startPoint = robot.properties.pose.Position;
                                    registryRobotJourney.endPoint = destPos1.Position;
                                    //robot.ShowText("MACRET_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE");
                                    //robot.ShowText("CHECK - REG");
                                }
                            }
                        }
                        break;
                    case MachineToReturn.MACRET_ROBOT_GOTO_BACK_FRONTLINE_READY:
                        if (TrafficRountineConstants.DetetectInsideStationCheck(registryRobotJourney))
                        {
                            break;
                        }
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
                                    
                                    Pose destPos1 = BfToRe.GetFrontLineMachine();
                                    String destName1 = Traffic.DetermineArea(destPos1.Position, TypeZone.MAIN_ZONE);
                                    if (destName1.Equals("OUTER"))
                                    {
                                        //robot.ShowText("GO FRONTLINE IN OUTER");
                                        if (rb.SendPoseStamped(destPos1))
                                        {
                                            resCmd = ResponseCommand.RESPONSE_NONE;
                                            StateMachineToReturn = MachineToReturn.MACRET_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE;
                                            registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                            registryRobotJourney.startPoint = robot.properties.pose.Position;
                                            registryRobotJourney.endPoint = destPos1.Position;
                                            //robot.ShowText("MACRET_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE");
                                        }
                                    }
                                    else if (destName1.Equals("VIM"))
                                    {
                                        //robot.ShowText("GO FRONTLINE IN VIM");
                                        if (rb.SendPoseStamped(destPos1))
                                        {
                                            resCmd = ResponseCommand.RESPONSE_NONE;
                                            StateMachineToReturn = MachineToReturn.MACRET_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE_FROM_VIM_READY;
                                            registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                            registryRobotJourney.startPoint = robot.properties.pose.Position;
                                            registryRobotJourney.endPoint = destPos1.Position;
                                            //robot.ShowText("MACRET_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE");
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
                    case MachineToReturn.MACRET_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE_FROM_VIM_READY:
                        try
                        {
                            TrafficRountineConstants.DetectRelease(registryRobotJourney);
                            if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                            //if (robot.ReachedGoal())
                            {
                                resCmd = ResponseCommand.RESPONSE_NONE;
                                StateMachineToReturn = MachineToReturn.MACRET_ROBOT_SEND_CMD_CAME_FRONTLINE_MACHINE;
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
                    case MachineToReturn.MACRET_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE_FROM_VIM_REG:
                        if (TrafficRountineConstants.DetetectInsideStationCheck(registryRobotJourney))
                        {
                            break;
                        }
                        else
                        {
                            StateMachineToReturn = MachineToReturn.MACRET_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE_FROM_VIM;
                        }
                        break;

                    case MachineToReturn.MACRET_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE_FROM_VIM:
                        try
                        {
                            TrafficRountineConstants.DetectRelease(registryRobotJourney);
                            if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                            //if (robot.ReachedGoal())
                            {
                                resCmd = ResponseCommand.RESPONSE_NONE;
                                StateMachineToReturn = MachineToReturn.MACRET_ROBOT_SEND_CMD_CAME_FRONTLINE_MACHINE;
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
                    case MachineToReturn.MACRET_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE:
                        try
                        {
                            if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                            //if (robot.ReachedGoal())
                            {
                                resCmd = ResponseCommand.RESPONSE_NONE;
                                StateMachineToReturn = MachineToReturn.MACRET_ROBOT_SEND_CMD_CAME_FRONTLINE_MACHINE;

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
                    case MachineToReturn.MACRET_ROBOT_SEND_CMD_CAME_FRONTLINE_MACHINE:
                        if (rb.SendCmdAreaPallet(BfToRe.GetInfoOfPalletMachine(PistonPalletCtrl.PISTON_PALLET_UP)))
                        {
                            StateMachineToReturn = MachineToReturn.MACRET_ROBOT_WAITTING_PICKUP_PALLET_MACHINE;
                            //robot.ShowText("MACRET_ROBOT_WAITTING_PICKUP_PALLET_MACHINE");
                        }
                        break;
                    case MachineToReturn.MACRET_ROBOT_WAITTING_PICKUP_PALLET_MACHINE:
                        if (resCmd == ResponseCommand.RESPONSE_LINEDETECT_PALLETUP)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;

                            StateMachineToReturn = MachineToReturn.MACRET_ROBOT_WAITTING_GOBACK_FRONTLINE_MACHINE;
                            //robot.ShowText("MACRET_ROBOT_WAITTING_GOBACK_FRONTLINE_MACHINE");
                        }
                        else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                        {
                            errorCode = ErrorCode.DETECT_LINE_ERROR;
                            CheckUserHandleError(this);
                        }
                        break;
                    case MachineToReturn.MACRET_ROBOT_WAITTING_GOBACK_FRONTLINE_MACHINE: // đợi
                        try
                        {
                            if (resCmd == ResponseCommand.RESPONSE_FINISH_GOBACK_FRONTLINE)
                            {
                                StateMachineToReturn = MachineToReturn.MACRET_ROBOT_GOTO_RETURN_SELECT_BEHAVIOR_ONZONE;
                                // cập nhật lại điểm xuất phát
                                registryRobotJourney.startPoint = robot.properties.pose.Position;
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
                    case MachineToReturn.MACRET_ROBOT_GOTO_RETURN_SELECT_BEHAVIOR_ONZONE:
                        String startNamePoint = Traffic.DetermineArea(registryRobotJourney.startPoint, TypeZone.MAIN_ZONE);
                        Pose destPos = BfToRe.GetFrontLineReturn();
                        String destName = Traffic.DetermineArea(destPos.Position, TypeZone.MAIN_ZONE);
                        if (startNamePoint.Equals("VIM"))
                        {
                            if (rb.SendPoseStamped(destPos))
                            {
                                StateMachineToReturn = MachineToReturn.MACRET_ROBOT_GOTO_FRONTLINE_RETURN_FROM_VIM;
                                registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                registryRobotJourney.startPoint = robot.properties.pose.Position;
                                registryRobotJourney.endPoint = destPos.Position;
                                //robot.ShowText("BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER");
                            }
                        }
                        else if (startNamePoint.Equals("OUTER"))
                        {
                            if (destName.Equals("OUTER"))
                            {
                                if (rb.SendPoseStamped(destPos))
                                {
                                    StateMachineToReturn = MachineToReturn.MACRET_ROBOT_GOTO_FRONTLINE_RETURN_FROM_VIM;
                                    registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                    registryRobotJourney.startPoint = robot.properties.pose.Position;
                                    registryRobotJourney.endPoint = destPos.Position;
                                    //robot.ShowText("BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER");
                                }
                            }
                            else if (destName.Equals("VIM"))
                            {
                                if (rb.SendPoseStamped(destPos))
                                {
                                    StateMachineToReturn = MachineToReturn.MACRET_ROBOT_GOTO_FRONTLINE_RETURN_FROM_VIM;
                                    registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                    registryRobotJourney.startPoint = robot.properties.pose.Position;
                                    registryRobotJourney.endPoint = destPos.Position;
                                    //robot.ShowText("BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER");
                                }
                            }
                        }
                        break;
                    case MachineToReturn.MACRET_ROBOT_GOTO_FRONTLINE_RETURN_FROM_VIM: // dang di
                        if (TrafficRountineConstants.DetetectInsideStationCheck(registryRobotJourney))
                        {
                            break;
                        }
                        try
                        {
                            if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                            //if ( robot.ReachedGoal())
                            {
                                resCmd = ResponseCommand.RESPONSE_NONE;
                                StateMachineToReturn = MachineToReturn.MACRET_ROBOT_SEND_CMD_DROPDOWN_PALLET;

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
                    case MachineToReturn.MACRET_ROBOT_GOTO_FRONTLINE_RETURN: // dang di
                        try
                        {
                            if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                            //if ( robot.ReachedGoal())
                            {
                                resCmd = ResponseCommand.RESPONSE_NONE;
                                StateMachineToReturn = MachineToReturn.MACRET_ROBOT_SEND_CMD_DROPDOWN_PALLET;

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
                    case MachineToReturn.MACRET_ROBOT_SEND_CMD_DROPDOWN_PALLET:
                        if (rb.SendCmdAreaPallet(BfToRe.GetInfoOfPalletReturn(PistonPalletCtrl.PISTON_PALLET_DOWN)))
                        {
                            //rb.prioritLevel.OnAuthorizedPriorityProcedure = true;
                            StateMachineToReturn = MachineToReturn.MACRET_ROBOT_WAITTING_DROPDOWN_PALLET;
                            //robot.ShowText("MACRET_ROBOT_WAITTING_DROPDOWN_PALLET");
                        }
                        break;
                    case MachineToReturn.MACRET_ROBOT_WAITTING_DROPDOWN_PALLET:
                        if (resCmd == ResponseCommand.RESPONSE_LINEDETECT_PALLETDOWN)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            BfToRe.UpdatePalletState(PalletStatus.W, order.palletId_P, order.palletId_P);
                            StateMachineToReturn = MachineToReturn.MACRET_ROBOT_WAITTING_GOTO_FRONTLINE;
                            //robot.ShowText("MACRET_ROBOT_WAITTING_GOTO_FRONTLINE");
                        }
                        else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                        {
                            errorCode = ErrorCode.DETECT_LINE_ERROR;
                            CheckUserHandleError(this);
                        }
                        break;
                    case MachineToReturn.MACRET_ROBOT_WAITTING_GOTO_FRONTLINE:
                        if (resCmd == ResponseCommand.RESPONSE_FINISH_GOBACK_FRONTLINE)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            //rb.prioritLevel.OnAuthorizedPriorityProcedure = false;
                            StateMachineToReturn = MachineToReturn.MACRET_ROBOT_RELEASED;
                            //robot.ShowText("MACRET_ROBOT_RELEASED");
                        }
                        else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                        {
                            errorCode = ErrorCode.DETECT_LINE_ERROR;
                            CheckUserHandleError(this);
                        }
                        break;
                    case MachineToReturn.MACRET_ROBOT_RELEASED: // trả robot về robotmanagement để nhận quy trình mới
                        TrafficRountineConstants.ReleaseAll(robot);
                        Global_Object.onFlagRobotComingGateBusy = false;
                        robot.orderItem = null;
                        rb.PreProcedureAs = ProcedureControlAssign.PRO_MACHINE_TO_RETURN;
                        ReleaseProcedureHandler(this);
                        ProRun = false;
                        //robot.ShowText("RELEASED");
                        UpdateInformationInProc(this, ProcessStatus.S);
                        order.status = StatusOrderResponseCode.FINISHED;
                        order.endTimeProcedure = DateTime.Now;
                        order.totalTimeProcedure = order.endTimeProcedure.Subtract(order.startTimeProcedure).TotalMinutes;
                        KillEvent();
                        break;
                    default:
                        break;
                }
                //robot.ShowText("-> " + procedureCode);
                Thread.Sleep(5);
            }
            StateMachineToReturn = MachineToReturn.MACRET_IDLE;
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