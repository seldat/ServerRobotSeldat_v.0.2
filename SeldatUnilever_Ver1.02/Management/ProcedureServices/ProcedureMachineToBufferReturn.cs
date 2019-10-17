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
using static SelDatUnilever_Ver1._00.Management.TrafficManager.TrafficRounterService;
using SeldatUnilever_Ver1._02.Management.TrafficManager;
using Newtonsoft.Json;

namespace SeldatUnilever_Ver1._02.Management.ProcedureServices
{
    public class ProcedureMachineToBufferReturn: TrafficProcedureService
    {
        MachineToBufferReturn StateMachineToBufferReturn;
        Thread ProMachineToBufferReturn;
        public RobotUnity robot;
        public JPallet JResult;
        ResponseCommand resCmd;
        TrafficManagementService Traffic;
        public override event Action<Object> ReleaseProcedureHandler;
        // public override event Action<Object> ErrorProcedureHandler;
        public ProcedureMachineToBufferReturn(RobotUnity robot, TrafficManagementService traffiicService) : base(robot, traffiicService)
        {
            StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_IDLE;
            this.robot = robot;
            this.Traffic = traffiicService;
            procedureCode = ProcedureCode.PROC_CODE_MACHINE_TO_BUFFER_RETURN;
        }

        public void Start(MachineToBufferReturn state = MachineToBufferReturn.MACBUFRET_SELECT_BEHAVIOR_ONZONE)
        {
            robot.orderItem = null;
            errorCode = ErrorCode.RUN_OK;
            robot.robotTag = RobotStatus.WORKING;
            robot.ProcedureAs = ProcedureControlAssign.PRO_MACHINE_TO_BUFFER_RETURN;
            StateMachineToBufferReturn = state;
            ProMachineToBufferReturn = new Thread(this.Procedure);
            ProRun = true;
            ProRunStopW = true;
            //robot.prioritLevel.OnAuthorizedPriorityProcedure = false;
            order.startTimeProcedure = DateTime.Now;
            registryRobotJourney = new RegistryRobotJourney();
            registryRobotJourney.robot = robot;
            registryRobotJourney.traffic = Traffic;
            robot.bayId = -1;
            robot.bayIdReg = false;
            ProMachineToBufferReturn.Start(this);
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
            TrafficRountineConstants.ReleaseAll(robot);
            robot.bayId = -1;
            robot.bayIdReg = false;
            //   this.robot.DestroyRegistrySolvedForm();
        }
        public void Procedure(object ojb)
        {
            ProcedureMachineToBufferReturn BfToBufRe = (ProcedureMachineToBufferReturn)ojb;
            RobotUnity rb = BfToBufRe.robot;
            TrafficManagementService Traffic = BfToBufRe.Traffic;
            rb.mcuCtrl.lampRbOn();
            robot.ShowText(" Start -> " + procedureCode);
           //Console.WriteLine( BfToBufRe.GetInfoOfPalletBufferReturn_MBR(PistonPalletCtrl.PISTON_PALLET_DOWN, order.dataRequest));
            //ProRun = false;
            while (ProRun)
            {
                switch (StateMachineToBufferReturn)
                {
                    case MachineToBufferReturn.MACBUFRET_IDLE:
                        break;
                    case MachineToBufferReturn.MACBUFRET_SELECT_BEHAVIOR_ONZONE:
                        if (Traffic.RobotIsInArea("READY", robot.properties.pose.Position, TypeZone.OPZS))
                        {
                            if (rb.PreProcedureAs == ProcedureControlAssign.PRO_READY)
                            {
                                StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_GOTO_BACK_FRONTLINE_READY;
                                registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                registryRobotJourney.startPoint = robot.properties.pose.Position;
                                registryRobotJourney.endPoint = BfToBufRe.GetFrontLineMachine().Position;
                                //robot.ShowText("BUFMAC_SELECT_BEHAVIOR_ONZONE : READY");
                                //robot.ShowText("CHECK - REG");
                            }
                        }
                        else if (Traffic.RobotIsInArea("VIM", robot.properties.pose.Position, TypeZone.MAIN_ZONE))
                        {
                            Pose mPose = BfToBufRe.GetFrontLineMachine();
                            if (rb.SendPoseStamped(mPose))
                            {
                                StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE_FROM_VIM_REG;
                                registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position);
                                registryRobotJourney.startPoint = robot.properties.pose.Position;
                                registryRobotJourney.endPoint = mPose.Position;
                                //robot.ShowText("BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE");
                            }
                        }
                        else if (Traffic.RobotIsInArea("OUTER", robot.properties.pose.Position, TypeZone.MAIN_ZONE))
                        {
                            Pose mPose = BfToBufRe.GetFrontLineMachine();
                            String destName1 = Traffic.DetermineArea(mPose.Position, TypeZone.MAIN_ZONE);
                            if (destName1.Equals("OUTER"))
                            {
                                //robot.ShowText("GO FRONTLINE IN OUTER");
                                if (rb.SendPoseStamped(mPose))
                                {
                                    StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE;
                                    registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                    registryRobotJourney.startPoint = robot.properties.pose.Position;
                                    registryRobotJourney.endPoint = mPose.Position;
                                    //robot.ShowText("BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE");
                                }
                            }
                            else if (destName1.Equals("VIM"))
                            {
                                //robot.ShowText("GO FRONTLINE IN VIM");
                                if (rb.SendPoseStamped(mPose))
                                {
                                    StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE_FROM_VIM_REG;
                                    registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                    registryRobotJourney.startPoint = robot.properties.pose.Position;
                                    registryRobotJourney.endPoint = mPose.Position;
                                    //robot.ShowText("BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER");
                                    //robot.ShowText("CHECK - REG");
                                }
                            }
                        }
                        break;
                    case MachineToBufferReturn.MACBUFRET_ROBOT_GOTO_BACK_FRONTLINE_READY:
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
                                    
                                    Pose destPos = BfToBufRe.GetFrontLineMachine();
                                    String destName2 = Traffic.DetermineArea(destPos.Position, TypeZone.MAIN_ZONE);
                                    if (destName2.Equals("OUTER"))
                                    {
                                        //robot.ShowText("GO FRONTLINE IN OUTER");
                                        if (rb.SendPoseStamped(destPos))
                                        {
                                            resCmd = ResponseCommand.RESPONSE_NONE;
                                            StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE;
                                            registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                            registryRobotJourney.startPoint = robot.properties.pose.Position;
                                            registryRobotJourney.endPoint = destPos.Position;
                                            //robot.ShowText("BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE");
                                        }
                                    }
                                    else if (destName2.Equals("VIM"))
                                    {
                                        //robot.ShowText("GO FRONTLINE IN VIM");
                                        if (rb.SendPoseStamped(destPos))
                                        {
                                            resCmd = ResponseCommand.RESPONSE_NONE;
                                            StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE_FROM_VIM_REG;
                                            registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                            registryRobotJourney.startPoint = robot.properties.pose.Position;
                                            registryRobotJourney.endPoint = destPos.Position;
                                            //robot.ShowText("BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE");
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
                    case MachineToBufferReturn.MACBUFRET_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE_FROM_VIM_READY:
                        try
                        {
                            TrafficRountineConstants.DetectRelease(registryRobotJourney);
                            if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                            {
                                resCmd = ResponseCommand.RESPONSE_NONE;
                                StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_SEND_CMD_PICKUP_PALLET_MACHINE;

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
                    case MachineToBufferReturn.MACBUFRET_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE_FROM_VIM_REG:
                        if (TrafficRountineConstants.DetetectInsideStationCheck(registryRobotJourney))
                        {
                            break;
                        }
                        else
                        {
                            StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE_FROM_VIM;
                        }
                        break;
                    case MachineToBufferReturn.MACBUFRET_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE_FROM_VIM:
                        try
                        {
                            TrafficRountineConstants.DetectRelease(registryRobotJourney);
                            if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                            {
                                resCmd = ResponseCommand.RESPONSE_NONE;
                                StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_SEND_CMD_PICKUP_PALLET_MACHINE;
 
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
                    case MachineToBufferReturn.MACBUFRET_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE:
                        try
                        {
                            if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                            //if (robot.ReachedGoal())
                            {
                                resCmd = ResponseCommand.RESPONSE_NONE;
                                StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_SEND_CMD_PICKUP_PALLET_MACHINE;
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
                    case MachineToBufferReturn.MACBUFRET_ROBOT_SEND_CMD_PICKUP_PALLET_MACHINE:
                        if (rb.SendCmdAreaPallet(BfToBufRe.GetInfoOfPalletMachine(PistonPalletCtrl.PISTON_PALLET_UP)))
                        {
                            StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_WAITTING_PICKUP_PALLET_MACHINE;
                        }
                        break;
                    case MachineToBufferReturn.MACBUFRET_ROBOT_WAITTING_PICKUP_PALLET_MACHINE:
                        if (resCmd == ResponseCommand.RESPONSE_LINEDETECT_PALLETUP)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            //BfToBufRe.UpdatePalletState(PalletStatus.F);
                            StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_WAITTING_GOBACK_FRONTLINE_MACHINE;
                            //robot.ShowText("MACBUFRET_ROBOT_WAITTING_GOBACK_FRONTLINE_MACHINE");
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
                                StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_GOTO_FRONTLINE_BUFFER_RETURN_SELECT_ZONE;
                                // cập nhật lại điểm xuất phát
                                registryRobotJourney.startPoint = robot.properties.pose.Position;
                                //robot.ShowText("MACBUFRET_ROBOT_GOTO_CHECKIN_RETURN");
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
                    case MachineToBufferReturn.MACBUFRET_ROBOT_GOTO_FRONTLINE_BUFFER_RETURN_SELECT_ZONE: // dang di
                        String startNamePoint = Traffic.DetermineArea(registryRobotJourney.startPoint, TypeZone.MAIN_ZONE);
                        Pose frontlinePose = BfToBufRe.GetFrontLineBufferReturn_MBR(order.dataRequest);
                        String destName = Traffic.DetermineArea(frontlinePose.Position, TypeZone.MAIN_ZONE);
                        if (startNamePoint.Equals("VIM"))
                        {
                            if (rb.SendPoseStamped(frontlinePose))
                            {
                                StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_GOTO_FRONTLINE_BUFFER_RETURN_FROM_VIM_REG;
                                registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                registryRobotJourney.startPoint = robot.properties.pose.Position;
                                registryRobotJourney.endPoint = frontlinePose.Position;
                                //robot.ShowText("BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER");
                            }
                        }
                        else if (startNamePoint.Equals("OUTER"))
                        {
                            if (destName.Equals("OUTER"))
                            {
                                if (rb.SendPoseStamped(frontlinePose))
                                {
                                    StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_GOTO_FRONTLINE_BUFFER_RETURN;
                                    registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                    registryRobotJourney.startPoint = robot.properties.pose.Position;
                                    registryRobotJourney.endPoint = frontlinePose.Position;
                                    //robot.ShowText("BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER");
                                }
                            }
                            else if (destName.Equals("VIM"))
                            {
                                if (rb.SendPoseStamped(frontlinePose))
                                {
                                    StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_GOTO_FRONTLINE_BUFFER_RETURN_FROM_VIM_REG;
                                    registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                    registryRobotJourney.startPoint = robot.properties.pose.Position;
                                    registryRobotJourney.endPoint = frontlinePose.Position;
                                    //robot.ShowText("BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER");
                                }
                            }
                        }

                        break;
                    case MachineToBufferReturn.MACBUFRET_ROBOT_GOTO_FRONTLINE_BUFFER_RETURN_FROM_VIM_REG: // dang di

                        if (TrafficRountineConstants.DetetectInsideStationCheck(registryRobotJourney))
                        {
                            break;
                        }
                        else
                        {
                            StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_GOTO_FRONTLINE_BUFFER_RETURN_FROM_VIM;
                        }
                        break;
                    case MachineToBufferReturn.MACBUFRET_ROBOT_GOTO_FRONTLINE_BUFFER_RETURN_FROM_VIM: // dang di
                        try
                        {
                            TrafficRountineConstants.DetectRelease(registryRobotJourney);
                            if (TrafficCheckInBuffer(goalFrontLinePos, bayId))
                                break;
                            if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                            {
                                resCmd = ResponseCommand.RESPONSE_NONE;
                                StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_SEND_CMD_DROPDOWN_PALLET;

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
                    case MachineToBufferReturn.MACBUFRET_ROBOT_GOTO_FRONTLINE_BUFFER_RETURN: // dang di
                        try
                        {
                            if (TrafficCheckInBuffer(goalFrontLinePos, bayId))
                                break;
                            if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                            {
                                resCmd = ResponseCommand.RESPONSE_NONE;
                                StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_SEND_CMD_DROPDOWN_PALLET;

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
                    case MachineToBufferReturn.MACBUFRET_ROBOT_SEND_CMD_DROPDOWN_PALLET:
                        JResult = BfToBufRe.GetInfoPallet_P_InBuffer(PistonPalletCtrl.PISTON_PALLET_DOWN);
                        String data = JsonConvert.SerializeObject(JResult.jInfoPallet);
                        if (rb.SendCmdAreaPallet(data))
                        {
                            StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_WAITTING_DROPDOWN_PALLET;
                            //robot.ShowText("MACBUFRET_ROBOT_WAITTING_DROPDOWN_PALLET");
                        }
                        break;
                    case MachineToBufferReturn.MACBUFRET_ROBOT_WAITTING_DROPDOWN_PALLET:
                        if (resCmd == ResponseCommand.RESPONSE_LINEDETECT_PALLETDOWN)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            BfToBufRe.UpdatePalletState(PalletStatus.W);
                            StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_WAITTING_GOTO_FRONTLINE;
                            //robot.ShowText("MACBUFRET_ROBOT_WAITTING_GOTO_FRONTLINE");
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
                            robot.bayId = -1;
                            robot.bayIdReg = false;
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            //rb.prioritLevel.OnAuthorizedPriorityProcedure = false;
                            StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_ROBOT_RELEASED;
                            //robot.ShowText("MACBUFRET_ROBOT_RELEASED");
                        }
                        else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                        {
                            errorCode = ErrorCode.DETECT_LINE_ERROR;
                            CheckUserHandleError(this);
                        }
                        break;
                    case MachineToBufferReturn.MACBUFRET_ROBOT_RELEASED: // trả robot về robotmanagement để nhận quy trình mới

                        TrafficRountineConstants.ReleaseAll(robot);
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
            StateMachineToBufferReturn = MachineToBufferReturn.MACBUFRET_IDLE;
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
