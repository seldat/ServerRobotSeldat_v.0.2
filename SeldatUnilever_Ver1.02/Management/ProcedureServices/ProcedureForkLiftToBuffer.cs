using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using DoorControllerService;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SeldatMRMS.Management;
using SeldatMRMS.Management.DoorServices;
using SeldatMRMS.Management.RobotManagent;
using SeldatMRMS.Management.TrafficManager;
using SeldatUnilever_Ver1._02.Management.ProcedureServices;
using SeldatUnilever_Ver1._02.Management.TrafficManager;
using SelDatUnilever_Ver1._00.Management.DeviceManagement;
using static DoorControllerService.DoorService;
using static SeldatMRMS.Management.RobotManagent.RobotBaseService;
using static SeldatMRMS.Management.RobotManagent.RobotUnityControl;
using static SeldatMRMS.Management.TrafficRobotUnity;
using static SeldatUnilever_Ver1._02.Management.TrafficManager.TrafficRountineConstants;
using static SelDatUnilever_Ver1._00.Management.DeviceManagement.DeviceItem;
using static SelDatUnilever_Ver1._00.Management.TrafficManager.TrafficRounterService;

namespace SeldatMRMS
{

    public class ProcedureForkLiftToBuffer : TrafficProcedureService
    {

        private ForkLift StateForkLift;
        public DoorService door;
        private ResponseCommand resCmd;
        public RobotUnity robot;
        private TrafficManagementService Traffic;
        public bool onFlagResetedGate = false;
        private DeviceRegistrationService deviceService;
        private DoorManagementService doorservice;
        public override event Action<Object> ReleaseProcedureHandler;
        public Point endPointBuffer;

        public void Registry(DeviceRegistrationService deviceService)
        {
            this.deviceService = deviceService;
        }
        public ProcedureForkLiftToBuffer(RobotUnity robot, DoorManagementService doorservice, TrafficManagementService trafficService) : base(robot,doorservice,trafficService)
        {
            StateForkLift = ForkLift.FORBUF_IDLE;
            resCmd = ResponseCommand.RESPONSE_NONE;
            this.robot = robot;
            this.doorservice = doorservice;
            this.Traffic = trafficService;
            procedureCode = ProcedureCode.PROC_CODE_FORKLIFT_TO_BUFFER;

        }
        public void Start(ForkLift state = ForkLift.FORBUF_SELECT_BEHAVIOR_ONZONE)
        {

            errorCode = ErrorCode.RUN_OK;
            robot.robotTag = RobotStatus.WORKING;
            robot.ProcedureAs = ProcedureControlAssign.PRO_FORKLIFT_TO_BUFFER;
            robot.bayId = -1;
            robot.robotBahaviorAtGate = RobotBahaviorAtReadyGate.GOING_INSIDE_GATE;
            StateForkLift = state;

            Task ProForkLift = new Task(() => this.Procedure(this));
            procedureStatus = ProcedureStatus.PROC_ALIVE;
            ProForkLift.Start();
            ProRun = true;
            ProRunStopW = true;
            //robot.prioritLevel.OnAuthorizedPriorityProcedure = false;
            robot.robotRegistryToWorkingZone.onRobotwillCheckInsideGate = true;
            order.startTimeProcedure = DateTime.Now;
            registryRobotJourney = new RegistryRobotJourney();
            registryRobotJourney.robot = robot;
            registryRobotJourney.traffic = Traffic;


        }
        public void Destroy()
        {
            Global_Object.setGateStatus(order.gate, false);
            ProRunStopW = false;
            robot.orderItem = null;
            robot.robotTag = RobotStatus.IDLE;
            StateForkLift = ForkLift.FORMAC_ROBOT_DESTROY;
            TrafficRountineConstants.ReleaseAll(robot);
            robot.bayId = -1;
        }
        public void Procedure(object ojb)
        {
            ProcedureForkLiftToBuffer FlToBuf = (ProcedureForkLiftToBuffer)ojb;
            RobotUnity rb = FlToBuf.robot;
            DoorService ds = getDoorService();
            TrafficManagementService Traffic = FlToBuf.Traffic;
            ForkLiftToMachineInfo flToMachineInfo = new ForkLiftToMachineInfo();
            rb.mcuCtrl.TurnOnLampRb();
            robot.ShowText(" Start -> " + procedureCode);
            while (ProRun)
            {
                switch (StateForkLift)
                {
                    case ForkLift.FORBUF_IDLE:
                        robot.ShowText("FORBUF_IDLE");
                        break;
                    case ForkLift.FORBUF_SELECT_BEHAVIOR_ONZONE:
                        if (Traffic.RobotIsInArea("READY", robot.properties.pose.Position))
                        {
                            if (rb.PreProcedureAs == ProcedureControlAssign.PRO_READY)
                            {
                               //if (false == rb.CheckInGateFromReadyZoneBehavior(ds.config.PointFrontLine.Position))
                                {
                                    robot.ShowText("FORBUF_ROBOT_GOTO_BACK_FRONTLINE_READY");
                                    registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                    registryRobotJourney.startPoint = robot.properties.pose.Position;
                                    registryRobotJourney.endPoint = ds.config.PointFrontLine.Position;
                                    StateForkLift = ForkLift.FORBUF_ROBOT_GOTO_BACK_FRONTLINE_READY;

                                }
                            }
                        }
                        else if (Traffic.RobotIsInArea("VIM",robot.properties.pose.Position))
                        {
                            robot.robotTag = RobotStatus.WORKING;
                            if (rb.SendPoseStamped(ds.config.PointFrontLine))
                            {
                                StateForkLift = ForkLift.FORBUF_ROBOT_WAITTING_GOTO_GATE_FROM_VIM_REG;
                                // Cap Nhat Thong Tin CHuyen Di
                                registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                registryRobotJourney.startPoint = robot.properties.pose.Position;
                                registryRobotJourney.endPoint = ds.config.PointFrontLine.Position;
                                robot.ShowText("FORBUF_ROBOT_WAITTING_GOTO_GATE");
                            }
                        }
                        else if (Traffic.RobotIsInArea("OUTER", robot.properties.pose.Position))
                        {
                            // public void Start (ForkLiftToBuffer state = ForkLiftToBuffer.FORBUF_ROBOT_RELEASED) {
                            robot.robotTag = RobotStatus.WORKING;
                            if (rb.SendPoseStamped(ds.config.PointFrontLine))
                            {
                                
                                StateForkLift = ForkLift.FORBUF_ROBOT_WAITTING_GOTO_GATE_FROM_VIM_REG;
                                registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                registryRobotJourney.startPoint = robot.properties.pose.Position;
                                registryRobotJourney.endPoint = ds.config.PointFrontLine.Position;
                                robot.ShowText("FORBUF_ROBOT_WAITTING_GOTO_CHECKIN_GATE");
                            }
                        }
                        break;
                    case ForkLift.FORBUF_ROBOT_GOTO_BACK_FRONTLINE_READY:
                        if (TrafficRountineConstants.DetetectInsideStationCheck(registryRobotJourney))
                        {
                            break;
                        }
                        if (rb.SendCmdPosPallet(RequestCommandPosPallet.REQUEST_GOBACK_FRONTLINE_TURN_LEFT))
                        {
                            Stopwatch sw = new Stopwatch();
                            sw.Start();
                            do
                            {
                                robot.onFlagGoBackReady = true;
                                if (resCmd == ResponseCommand.RESPONSE_FINISH_GOBACK_FRONTLINE)
                                {
                                    robot.onFlagGoBackReady = false;
                                    resCmd = ResponseCommand.RESPONSE_NONE;
                                    robot.robotTag = RobotStatus.WORKING;
                                    if (rb.SendPoseStamped(ds.config.PointFrontLine))
                                    {
                                        StateForkLift = ForkLift.FORBUF_ROBOT_WAITTING_GOTO_GATE_READY;
                                        robot.ShowText("FORBUF_ROBOT_WAITTING_GOTO_GATE");
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
                        break;
                    case ForkLift.FORBUF_ROBOT_WAITTING_GOTO_GATE_READY:
                        // dò ra điểm đích đến và xóa đăng ký vùng
                        TrafficRountineConstants.DetectRelease(registryRobotJourney);
                        if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                        {
                            robot.setTrafficAllCircles(false, false, false, false);
                            TrafficRountineConstants.RegIntZone_READY.Release(robot);
                            robot.SwitchToDetectLine(true);
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            StateForkLift = ForkLift.FORBUF_ROBOT_CAME_GATE_POSITION;
                            robot.ShowText("FORBUF_ROBOT_CAME_GATE_POSITION");
                        }
                        break;
                    case ForkLift.FORBUF_ROBOT_WAITTING_GOTO_GATE_FROM_VIM_REG:
                        // kiem tra vung đăng ký tai khu vuc xac định
                        if (TrafficRountineConstants.DetetectInsideStationCheck(registryRobotJourney))
                        {
                            break;
                        }
                        else
                        {
                            // dò ra điểm đích đến và xóa đăng ký vùng
                            StateForkLift= ForkLift.FORBUF_ROBOT_WAITTING_GOTO_GATE_FROM_VIM;
                        }
                        break;
                    case ForkLift.FORBUF_ROBOT_WAITTING_GOTO_GATE_FROM_VIM:
                        // kiem tra vung đăng ký tai khu vuc xac định
                        TrafficRountineConstants.DetectRelease(registryRobotJourney);
                        if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                        {
                            // robot.setTrafficAllCircles(false, false, false, false);
                            TrafficRountineConstants.RegIntZone_READY.Release(robot);
                            robot.SwitchToDetectLine(true);
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            //rb.prioritLevel.OnAuthorizedPriorityProcedure = true;
                            StateForkLift = ForkLift.FORBUF_ROBOT_CAME_GATE_POSITION;
                            robot.ShowText("FORBUF_ROBOT_CAME_GATE_POSITION");
                        }
                        break;
                    case ForkLift.FORBUF_ROBOT_CAME_GATE_POSITION: // da den khu vuc cong , gui yeu cau mo cong.
                        robot.robotRegistryToWorkingZone.onRobotwillCheckInsideGate = false;
                        ds.openDoor(DoorService.DoorType.DOOR_BACK);
                        StateForkLift = ForkLift.FORBUF_ROBOT_WAITTING_OPEN_DOOR;
                        robot.ShowText("FORBUF_ROBOT_WAITTING_OPEN_DOOR");
                        break;
                    case ForkLift.FORBUF_ROBOT_WAITTING_OPEN_DOOR: //doi mo cong
                        RetState ret = ds.checkOpen(DoorService.DoorType.DOOR_BACK);
                        if (RetState.DOOR_CTRL_SUCCESS == ret)
                        {
                            StateForkLift = ForkLift.FORBUF_ROBOT_OPEN_DOOR_SUCCESS;
                            robot.ShowText("FORBUF_ROBOT_OPEN_DOOR_SUCCESS");
                        }
                        else if (RetState.DOOR_CTRL_ERROR == ret)
                        {
                            robot.ShowText("FORBUF_ROBOT_OPEN_DOOR_ERROR");
                            StateForkLift = ForkLift.FORBUF_ROBOT_CAME_GATE_POSITION;
                            Thread.Sleep(50);
                        }
                        break;
                    case ForkLift.FORBUF_ROBOT_OPEN_DOOR_SUCCESS: // mo cua thang cong ,gui toa do line de robot di vao gap hang
                        // rb.SendCmdLineDetectionCtrl(RequestCommandLineDetect.REQUEST_LINEDETECT_PALLETUP);
                        if (rb.SendCmdAreaPallet(ds.config.infoPallet))
                        {
                            StateForkLift = ForkLift.FORBUF_ROBOT_WAITTING_PICKUP_PALLET_IN;
                            robot.ShowText("FORBUF_ROBOT_WAITTING_PICKUP_PALLET_IN");
                        }
                        break;
                    case ForkLift.FORBUF_ROBOT_WAITTING_PICKUP_PALLET_IN: // doi robot gap hang
                        if (resCmd == ResponseCommand.RESPONSE_LINEDETECT_PALLETUP)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            // FlToBuf.UpdatePalletState(PalletStatus.F);
                            //   rb.SendCmdPosPallet (RequestCommandPosPallet.REQUEST_GOBACK_FRONTLINE);
                            StateForkLift = ForkLift.FORBUF_ROBOT_FINISH_PALLET_UP;
                            endPointBuffer = FlToBuf.GetFrontLineBuffer(true).Position;
                            robot.robotBahaviorAtGate = RobotBahaviorAtReadyGate.GOING_OUTSIDE_GATE;
                            robot.ShowText("FORBUF_ROBOT_WAITTING_GOBACK_FRONTLINE_GATE");
                        }
                        else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                        {
                            errorCode = ErrorCode.DETECT_LINE_ERROR;
                            CheckUserHandleError(this);
                        }
                        break;
                    case ForkLift.FORBUF_ROBOT_FINISH_PALLET_UP:

                        String destName = Traffic.DetermineArea(endPointBuffer, TypeZone.MAIN_ZONE);
                        if (destName.Equals("VIM"))
                        {
                            if (checkAnyRobotAtElevator(robot))
                            {
                                break;
                            }
                            else
                            {
                                StateForkLift = ForkLift.FORBUF_ROBOT_WAITTING_GOBACK_FRONTLINE_GATE;
                            }
                        }
                        else
                        {
                            StateForkLift = ForkLift.FORBUF_ROBOT_WAITTING_GOBACK_FRONTLINE_GATE;
                        }
                        break;
                    case ForkLift.FORBUF_ROBOT_WAITTING_GOBACK_FRONTLINE_GATE:
                        if (resCmd == ResponseCommand.RESPONSE_FINISH_GOBACK_FRONTLINE)
                        {
                            Global_Object.setGateStatus(order.gate, false);
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            ds.LampOff(DoorService.DoorType.DOOR_FRONT);
                            ds.closeDoor(DoorService.DoorType.DOOR_BACK);

                            StateForkLift = ForkLift.FORBUF_ROBOT_WAITTING_CLOSE_GATE;
                            robot.ShowText("FORBUF_ROBOT_WAITTING_CLOSE_GATE");
                        }
                        else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                        {
                            errorCode = ErrorCode.DETECT_LINE_ERROR;
                            CheckUserHandleError(this);
                        }
                        break;
                    case ForkLift.FORBUF_ROBOT_WAITTING_CLOSE_GATE:
                        try
                        {
                            
                            StateForkLift = ForkLift.FORBUF_ROBOT_CHECK_GOTO_BUFFER_OR_MACHINE;
                            robot.SwitchToDetectLine(false);
                            registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                            registryRobotJourney.startPoint = robot.properties.pose.Position;
                            registryRobotJourney.endPoint = FlToBuf.GetFrontLineBuffer(true).Position;
                           
                        }
                        catch (System.Exception)
                        {
                            errorCode = ErrorCode.CAN_NOT_GET_DATA;
                            CheckUserHandleError(this);
                        }
                        break;
                    case ForkLift.FORBUF_ROBOT_CHECK_GOTO_BUFFER_OR_MACHINE:
                        flToMachineInfo = GetPriorityTaskForkLiftToMachine(order.productId);
                        if (flToMachineInfo == null)
                        {
                            StateForkLift = ForkLift.FORBUF_ROBOT_WAITTING_ZONE_BUFFER_READY;
                        }
                        else
                        {
                           FreePlanedBuffer();
                           StateForkLift = ForkLift.FORMAC_ROBOT_GOTO_FRONTLINE_MACHINE_FROM_VIM_REG;
                        }
                        break;

                    case ForkLift.FORBUF_ROBOT_WAITTING_ZONE_BUFFER_READY:
                        if (TrafficRountineConstants.DetetectInsideStationCheck(registryRobotJourney))
                        {
                            break;
                        }
                        Pose destPos1 = FlToBuf.GetFrontLineBuffer(true);
                        if (rb.SendPoseStamped(destPos1))
                        {
                            StateForkLift = ForkLift.FORBUF_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER;
                            robot.ShowText("FORBUF_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER");
                        }
                        break;
                    case ForkLift.FORBUF_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_FROM_VIM_REG:
                        if (TrafficRountineConstants.DetetectInsideStationCheck(registryRobotJourney))
                        {
                            break;
                        }
                        else
                        {
                            StateForkLift = ForkLift.FORBUF_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_FROM_VIM;
                        }
                        break;
                    case ForkLift.FORBUF_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_FROM_VIM:
                        TrafficRountineConstants.DetectRelease(registryRobotJourney);
                        try
                        {
                            if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                            {
                                robot.SwitchToDetectLine(true);
                                resCmd = ResponseCommand.RESPONSE_NONE;
                                if (rb.SendCmdAreaPallet(FlToBuf.GetInfoOfPalletBuffer(PistonPalletCtrl.PISTON_PALLET_DOWN, true)))
                                {
                                    StateForkLift = ForkLift.FORBUF_ROBOT_WAITTING_DROPDOWN_PALLET_BUFFER;
                                    robot.ShowText("FORBUF_ROBOT_WAITTING_DROPDOWN_PALLET_BUFFER");
                                }
                            }
                        }
                        catch (System.Exception)
                        {
                            errorCode = ErrorCode.CAN_NOT_GET_DATA;
                            CheckUserHandleError(this);
                        }
                        break;
                    case ForkLift.FORBUF_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER:
                        // xóa đăng ký vùng
                        TrafficRountineConstants.DetectRelease(registryRobotJourney);
                        if (TrafficCheckInBuffer(goalFrontLinePos, bayId))
                            break;
                       
                        try
                        {
                            if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                            {
                                robot.SwitchToDetectLine(true);
                                resCmd = ResponseCommand.RESPONSE_NONE;
                                if (rb.SendCmdAreaPallet(FlToBuf.GetInfoOfPalletBuffer(PistonPalletCtrl.PISTON_PALLET_DOWN, true)))
                                {
                                    StateForkLift = ForkLift.FORBUF_ROBOT_WAITTING_DROPDOWN_PALLET_BUFFER;
                                    robot.ShowText("FORBUF_ROBOT_WAITTING_DROPDOWN_PALLET_BUFFER");
                                }
                            }
                        }
                        catch (System.Exception)
                        {
                            errorCode = ErrorCode.CAN_NOT_GET_DATA;
                            CheckUserHandleError(this);
                        }
                        break;

                    case ForkLift.FORBUF_ROBOT_WAITTING_DROPDOWN_PALLET_BUFFER:
                        if (resCmd == ResponseCommand.RESPONSE_LINEDETECT_PALLETDOWN)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            FlToBuf.UpdatePalletState(PalletStatus.W);
                            //   rb.SendCmdPosPallet (RequestCommandPosPallet.REQUEST_GOBACK_FRONTLINE);
                            StateForkLift = ForkLift.FORBUF_ROBOT_WAITTING_GOBACK_FRONTLINE_BUFFER;
                            robot.ShowText("FORBUF_ROBOT_WAITTING_GOBACK_FRONTLINE_BUFFER");
                        }
                        else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                        {
                            errorCode = ErrorCode.DETECT_LINE_ERROR;
                            CheckUserHandleError(this);
                        }
                        break;
                    case ForkLift.FORBUF_ROBOT_WAITTING_GOBACK_FRONTLINE_BUFFER: // đợi
                        if (resCmd == ResponseCommand.RESPONSE_FINISH_GOBACK_FRONTLINE)
                        {
                            robot.bayId = -1;
                            robot.ReleaseWorkingZone();
                            robot.SwitchToDetectLine(false);
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            //rb.prioritLevel.OnAuthorizedPriorityProcedure = false;
                            StateForkLift = ForkLift.FORBUF_ROBOT_RELEASED;
                            robot.ShowText("FORBUF_ROBOT_RELEASED");
                        }
                        else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                        {
                            errorCode = ErrorCode.DETECT_LINE_ERROR;
                            CheckUserHandleError(this);
                        }
                        break;
                    case ForkLift.FORBUF_ROBOT_RELEASED: // trả robot về robotmanagement để nhận quy trình mới
                        rb.PreProcedureAs = ProcedureControlAssign.PRO_FORKLIFT_TO_BUFFER;
                        ReleaseProcedureHandler(this);
                        ProRun = false;
                        robot.ShowText("RELEASED");
                        UpdateInformationInProc(this, ProcessStatus.S);
                        order.status = StatusOrderResponseCode.FINISHED;
                        break;
                    case ForkLift.FORMAC_ROBOT_GOTO_FRONTLINE_MACHINE_FROM_VIM_REG:
                        if (TrafficRountineConstants.DetetectInsideStationCheck(registryRobotJourney))
                        {
                            break;
                        }
                        else
                        {
                            StateForkLift = ForkLift.FORMAC_ROBOT_GOTO_FRONTLINE_MACHINE_FROM_VIM;
                        }
                        break;
                    case ForkLift.FORMAC_ROBOT_GOTO_FRONTLINE_MACHINE_FROM_VIM:
                         TrafficRountineConstants.DetectRelease(registryRobotJourney);
                        try
                        {
                            if (rb.SendPoseStamped(flToMachineInfo.frontLinePose))
                            {
                                   StateForkLift = ForkLift.FORMAC_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE;
                                robot.ShowText("FORMAC_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE");
                                onFlagResetedGate = false;
                            }
                        }
                        catch (System.Exception)
                        {
                            errorCode = ErrorCode.CAN_NOT_GET_DATA;
                            CheckUserHandleError(this);
                        }
                        break;
                    case ForkLift.FORMAC_ROBOT_GOTO_FRONTLINE_MACHINE:
                        try
                        {
                            if (TrafficRountineConstants.DetetectInsideStationCheck(registryRobotJourney))
                            {
                                break;
                            }
                            if (rb.SendPoseStamped(flToMachineInfo.frontLinePose))
                            {
                                StateForkLift = ForkLift.FORMAC_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE;
                                robot.ShowText("FORMAC_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE");
                                onFlagResetedGate = false;
                            }
                        }
                        catch (System.Exception)
                        {
                            errorCode = ErrorCode.CAN_NOT_GET_DATA;
                            CheckUserHandleError(this);
                        }
                        break;

                    case ForkLift.FORMAC_ROBOT_WAITTING_CAME_FRONTLINE_MACHINE:
                        try
                        {
                            // xóa đăng ký vùng
                            TrafficRountineConstants.DetectRelease(registryRobotJourney);
                           // Global_Object.onFlagDoorBusy = false;
                           /* if (!Traffic.HasRobotUnityinArea("GATE_CHECKOUT", robot))
                            {
                                if (!onFlagResetedGate)
                                {
                                    TrafficRountineConstants.RegIntZone_READY.Release(robot);
                                    onFlagResetedGate = true;
                                    robot.robotBahaviorAtGate = RobotBahaviorAtReadyGate.IDLE;
                                    Global_Object.onFlagRobotComingGateBusy = false;
                                    robot.ReleaseWorkingZone();
                                }
                            }*/
                            if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                            //if (robot.ReachedGoal())
                            {
                                robot.SwitchToDetectLine(true);
                           
                                if (rb.SendCmdAreaPallet(flToMachineInfo.infoPallet))
                                {
                                    //rb.prioritLevel.OnAuthorizedPriorityProcedure = true;
                                    StateForkLift = ForkLift.FORMAC_ROBOT_WAITTING_DROPDOWN_PALLET_MACHINE;
                                    robot.ShowText("FORMAC_ROBOT_WAITTING_DROPDOWN_PALLET_MACHINE");
                                }
                            }
                        }
                        catch (System.Exception)
                        {
                            errorCode = ErrorCode.CAN_NOT_GET_DATA;
                            CheckUserHandleError(this);
                        }
                        break;

                    case ForkLift.FORMAC_ROBOT_WAITTING_DROPDOWN_PALLET_MACHINE:
                        if (resCmd == ResponseCommand.RESPONSE_LINEDETECT_PALLETDOWN)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            // FlToBuf.UpdatePalletState(PalletStatus.W);
                            //   rb.SendCmdPosPallet (RequestCommandPosPallet.REQUEST_GOBACK_FRONTLINE);
                            StateForkLift = ForkLift.FORMAC_ROBOT_WAITTING_GOBACK_FRONTLINE_MACHINE;
                            robot.ShowText("FORMAC_ROBOT_WAITTING_GOBACK_FRONTLINE_MACHINE");
                        }
                        else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                        {
                            errorCode = ErrorCode.DETECT_LINE_ERROR;
                            CheckUserHandleError(this);
                        }
                        break;
                    case ForkLift.FORMAC_ROBOT_WAITTING_GOBACK_FRONTLINE_MACHINE: // đợi
                        if (resCmd == ResponseCommand.RESPONSE_FINISH_GOBACK_FRONTLINE)
                        {
                            robot.SwitchToDetectLine(false);
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            //rb.prioritLevel.OnAuthorizedPriorityProcedure = false;
                            StateForkLift = ForkLift.FORMAC_ROBOT_RELEASED;
                            robot.ShowText("FORMAC_ROBOT_RELEASED");
                        }
                        else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                        {
                            errorCode = ErrorCode.DETECT_LINE_ERROR;
                            CheckUserHandleError(this);
                        }
                        break;
                    case ForkLift.FORMAC_ROBOT_RELEASED: // trả robot về robotmanagement để nhận quy trình mới
                        TrafficRountineConstants.ReleaseAll(robot);
                        robot.orderItem = null;
                       // Global_Object.onFlagDoorBusy = false;
                        robot.SwitchToDetectLine(false);
                       // robot.robotTag = RobotStatus.IDLE;
                        robot.ReleaseWorkingZone();
                        rb.PreProcedureAs = ProcedureControlAssign.PRO_FORKLIFT_TO_MACHINE;
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
                    case ForkLift.FORMAC_ROBOT_DESTROY: // trả robot về robotmanagement để nhận quy trình mới
                        TrafficRountineConstants.ReleaseAll(robot);
                        robot.SwitchToDetectLine(false);
                        robot.ReleaseWorkingZone();
                        robot.robotBahaviorAtGate = RobotBahaviorAtReadyGate.IDLE;
                        //robot.prioritLevel.OnAuthorizedPriorityProcedure = false;
                        ProRun = false;
                        UpdateInformationInProc(this, ProcessStatus.F);
                        order.status = StatusOrderResponseCode.ROBOT_ERROR;
                        selectHandleError = SelectHandleError.CASE_ERROR_EXIT;
                        procedureStatus = ProcedureStatus.PROC_KILLED;
                        // RestoreOrderItem();
                        FreePlanedBuffer();
                        order.endTimeProcedure = DateTime.Now;
                        order.totalTimeProcedure = order.endTimeProcedure.Subtract(order.startTimeProcedure).TotalMinutes;
                        SaveOrderItem(order);
                        KillEvent();
                        break;
                    //////////////////////////////////////////////////////
                    default:
                        break;
                }
                Thread.Sleep(5);
            }
            StateForkLift = ForkLift.FORBUF_IDLE;
        }
        public override void FinishStatesCallBack(Int32 message)
        {
            this.resCmd = (ResponseCommand)message;
            base.FinishStatesCallBack(message);
        }
        public class ForkLiftToMachineInfo
        {
            public Pose frontLinePose;
            public String infoPallet;
        }
        public ForkLiftToMachineInfo GetPriorityTaskForkLiftToMachine(int productId)
        {
            ForkLiftToMachineInfo forkLiftToMachineInfo = null;
            try
            {

                bool onHasOrder = false;
                foreach (DeviceItem deviceItem in deviceService.GetDeviceItemList())
                {
                    foreach (OrderItem order in deviceItem.PendingOrderList)
                    {
                        if (order.typeReq == TyeRequest.TYPEREQUEST_BUFFER_TO_MACHINE)
                        {
                            if (order.productId == productId)
                            {
                                forkLiftToMachineInfo = new ForkLiftToMachineInfo();
                                forkLiftToMachineInfo.frontLinePose = order.palletAtMachine.linePos;
                                JInfoPallet infoPallet = new JInfoPallet();

                                infoPallet.pallet = PistonPalletCtrl.PISTON_PALLET_DOWN; /* dropdown */
                                infoPallet.bay = order.palletAtMachine.bay;
                                infoPallet.hasSubLine = "no"; /* no */
                                infoPallet.dir_main = (TrafficRobotUnity.BrDirection)order.palletAtMachine.directMain;
                                infoPallet.dir_sub = (TrafficRobotUnity.BrDirection)order.palletAtMachine.directSub;
                                infoPallet.dir_out = (TrafficRobotUnity.BrDirection)order.palletAtMachine.directOut;
                                infoPallet.line_ord = order.palletAtMachine.line_ord;
                                infoPallet.row = order.palletAtMachine.row;

                                forkLiftToMachineInfo.infoPallet = JsonConvert.SerializeObject(infoPallet);
                                order.status = StatusOrderResponseCode.CHANGED_FORKLIFT;
                                onHasOrder = true;
                                deviceItem.PendingOrderList.Remove(order);
                                break;
                            }

                        }
                    }
                    if (onHasOrder)
                        break;
                }
            }
            catch
            {
                Console.WriteLine("Error in GetPriorityTaskForkLiftToMachine");
            }
            return forkLiftToMachineInfo;
        }
    }
}
