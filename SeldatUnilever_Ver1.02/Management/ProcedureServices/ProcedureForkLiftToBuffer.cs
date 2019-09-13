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
        public Pose endPointBuffer;
        private DoorService ds;
        public JPallet JResult;
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
        public void Start(ForkLift state = ForkLift.FORBUF_GET_FRONTLINE)
        {

            errorCode = ErrorCode.RUN_OK;
            robot.robotTag = RobotStatus.WORKING;
            robot.ProcedureAs = ProcedureControlAssign.PRO_FORKLIFT_TO_BUFFER;
            robot.bayId = -1;
            robot.bayIdReg = false;
            robot.robotBahaviorAtGate = RobotBahaviorAtReadyGate.GOING_INSIDE_GATE;
            StateForkLift = state;

            Task ProForkLift = new Task(() => this.Procedure(this));
            procedureStatus = ProcedureStatus.PROC_ALIVE;
            ProRun = true;
            ProRunStopW = true;
            //robot.prioritLevel.OnAuthorizedPriorityProcedure = false;
            robot.robotRegistryToWorkingZone.onRobotwillCheckInsideGate = true;
            order.startTimeProcedure = DateTime.Now;
            registryRobotJourney = new RegistryRobotJourney();
            registryRobotJourney.robot = robot;
            registryRobotJourney.traffic = Traffic;
            ProForkLift.Start();
        }
        public void Destroy()
        {
            Global_Object.setGateStatus(order.gate, false);
            if (ds != null) {
                ds.LampSetStateOff(DoorType.DOOR_FRONT);
                ds.setDoorBusy(false);
                ds.removeListCtrlDoorBack();
            }

            ProRunStopW = false;
            robot.orderItem = null;
            robot.robotTag = RobotStatus.IDLE;
            StateForkLift = ForkLift.FORMAC_ROBOT_DESTROY;
            TrafficRountineConstants.ReleaseAll(robot);
            robot.bayId = -1;
            robot.bayIdReg = false;
        }
        int countFrontLineNull = 0;
        public void Procedure(object ojb)
        {
            ProcedureForkLiftToBuffer FlToBuf = (ProcedureForkLiftToBuffer)ojb;
            RobotUnity rb = FlToBuf.robot;
            ds = getDoorService();
            ds.setRb(rb);
            TrafficManagementService Traffic = FlToBuf.Traffic;
            ForkLiftToMachineInfo flToMachineInfo = new ForkLiftToMachineInfo();
            rb.mcuCtrl.lampRbOn();
            robot.ShowText(" Start -> " + procedureCode);
          /*  endPointBuffer = FlToBuf.GetFrontLineBuffer(true);
            if (endPointBuffer == null)
            {
                robot.bayId = -1;
                Console.WriteLine("Error Data Request" + order.dataRequest);
                order.status = StatusOrderResponseCode.ERROR_GET_FRONTLINE;
                TrafficRountineConstants.ReleaseAll(robot);
                robot.orderItem = null;
                robot.SwitchToDetectLine(false);
                robot.ReleaseWorkingZone();
                if (Traffic.RobotIsInArea("READY", robot.properties.pose.Position))
                {
                    TrafficRountineConstants.RegIntZone_READY.Release(robot);
                    robot.robotTag = RobotStatus.IDLE;
                    robot.SetSafeYellowcircle(false);
                    robot.SetSafeBluecircle(false);
                    robot.SetSafeSmallcircle(false);
                    robot.TurnOnSupervisorTraffic(false);
                   // rb.mcuCtrl.lampRbOff();
                    procedureCode = ProcedureCode.PROC_CODE_ROBOT_TO_READY;
                }
                else
                    procedureCode = ProcedureCode.PROC_CODE_FORKLIFT_TO_BUFFER;
                ReleaseProcedureHandler(this);
                ProRun = false;
                robot.ShowText("RELEASED");
                UpdateInformationInProc(this, ProcessStatus.S);
                order.endTimeProcedure = DateTime.Now;
                order.totalTimeProcedure = order.endTimeProcedure.Subtract(order.startTimeProcedure).TotalMinutes;
                SaveOrderItem(order);
                KillEvent();
            }
            else
            {
                order.frontLinePos = endPointBuffer.Position;
                robot.bayId = bayId;
            }*/
            while (ProRun)
            {
               // JPallet aa= GetInfoPallet_P_InBuffer(TrafficRobotUnity.PistonPalletCtrl.PISTON_PALLET_DOWN);
                switch (StateForkLift)
                {
                    case ForkLift.FORBUF_IDLE:
                        robot.ShowText("FORBUF_IDLE");
                        break;
                    case ForkLift.FORBUF_GET_FRONTLINE:
                        endPointBuffer = FlToBuf.GetFrontLineBuffer(true);
                        if (endPointBuffer == null)
                        {
                            if (countFrontLineNull++ < 10)
                            {
                                break;
                            }
                            countFrontLineNull = 0;
                            robot.bayId = -1;
                            Console.WriteLine("Error Data Request" + order.dataRequest);
                            order.status = StatusOrderResponseCode.ERROR_GET_FRONTLINE;
                            TrafficRountineConstants.ReleaseAll(robot);
                            robot.orderItem = null;
                            robot.SwitchToDetectLine(false);
                            robot.ReleaseWorkingZone();
                            if (Traffic.RobotIsInArea("READY", robot.properties.pose.Position))
                            {
                                TrafficRountineConstants.RegIntZone_READY.Release(robot);
                                robot.robotTag = RobotStatus.IDLE;
                                robot.SetSafeYellowcircle(false);
                                robot.SetSafeBluecircle(false);
                                robot.SetSafeSmallcircle(false);
                                robot.TurnOnSupervisorTraffic(false);
                                // rb.mcuCtrl.lampRbOff();
                                procedureCode = ProcedureCode.PROC_CODE_ROBOT_TO_READY;
                            }
                            else
                                procedureCode = ProcedureCode.PROC_CODE_FORKLIFT_TO_BUFFER;
                            ReleaseProcedureHandler(this);
                            ProRun = false;
                            robot.ShowText("RELEASED");
                            UpdateInformationInProc(this, ProcessStatus.S);
                            order.endTimeProcedure = DateTime.Now;
                            order.totalTimeProcedure = order.endTimeProcedure.Subtract(order.startTimeProcedure).TotalMinutes;
                            SaveOrderItem(order);
                            KillEvent();
                        }
                        else
                        {
                            order.frontLinePos = endPointBuffer.Position;
                            robot.bayId = bayId;
                            StateForkLift = ForkLift.FORBUF_SELECT_BEHAVIOR_ONZONE;
                        }
                        break;
                    case ForkLift.FORBUF_SELECT_BEHAVIOR_ONZONE:
                        order.status = StatusOrderResponseCode.GOING_AND_PICKING_UP;
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
                                    
                                    robot.robotTag = RobotStatus.WORKING;
                                    if (rb.SendPoseStamped(ds.config.PointFrontLine))
                                    {
                                        resCmd = ResponseCommand.RESPONSE_NONE;
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
                       // TrafficRountineConstants.DetectRelease(registryRobotJourney);
                        if (Traffic.RobotIsInArea("C3", rb.properties.pose.Position))
                        {
                            ds.setDoorBusy(true);
                            ds.openDoor(DoorService.DoorType.DOOR_BACK);
                            StateForkLift = ForkLift.FORBUF_ROBOT_WAITTING_GOTO_GATE_READY_OPEN_DOOR;
                        }
                        break;
                    case ForkLift.FORBUF_ROBOT_WAITTING_GOTO_GATE_READY_OPEN_DOOR:
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
                        //TrafficRountineConstants.DetectRelease(registryRobotJourney);
                        if (Traffic.RobotIsInArea("C3", rb.properties.pose.Position))
                        {
                            ds.setDoorBusy(true);
                            ds.openDoor(DoorService.DoorType.DOOR_BACK);
                            StateForkLift = ForkLift.FORBUF_ROBOT_WAITTING_GOTO_GATE_FROM_VIM_OPEN_DOOR;
                        }
                        break;
                    case ForkLift.FORBUF_ROBOT_WAITTING_GOTO_GATE_FROM_VIM_OPEN_DOOR:
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
                        //ds.setDoorBusy(true);
                        //ds.openDoor(DoorService.DoorType.DOOR_BACK);
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
                            //StateForkLift = ForkLift.FORBUF_ROBOT_CAME_GATE_POSITION;
                            Thread.Sleep(1000);
                            ds.setDoorBusy(true);
                            ds.openDoor(DoorService.DoorType.DOOR_BACK);
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
                        try
                        {
                            if (resCmd == ResponseCommand.RESPONSE_LINEDETECT_PALLETUP)
                            {
                                resCmd = ResponseCommand.RESPONSE_NONE;
                                // FlToBuf.UpdatePalletState(PalletStatus.F);
                                //   rb.SendCmdPosPallet (RequestCommandPosPallet.REQUEST_GOBACK_FRONTLINE);
                                StateForkLift = ForkLift.FORBUF_ROBOT_FINISH_PALLET_UP;
                                Console.WriteLine("pallet ID" + order.palletId);

                                robot.robotBahaviorAtGate = RobotBahaviorAtReadyGate.GOING_OUTSIDE_GATE;
                                robot.ShowText("FORBUF_ROBOT_WAITTING_GOBACK_FRONTLINE_GATE");
                            }
                            else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                            {
                                errorCode = ErrorCode.DETECT_LINE_ERROR;
                                CheckUserHandleError(this);
                            }
                        }
                        catch
                        {
                            
                        }
                        break;
                    case ForkLift.FORBUF_ROBOT_FINISH_PALLET_UP:

                       String destName = Traffic.DetermineArea(endPointBuffer.Position, TypeZone.MAIN_ZONE);
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
                        // kiem tra da toi vung dong cong
                        if (Traffic.RobotIsInArea("CLOSE-GATE", robot.properties.pose.Position))
                        {
                            if (resCmd == ResponseCommand.RESPONSE_FINISH_GOBACK_FRONTLINE)
                            {
                                Global_Object.setGateStatus(order.gate, false);
                                resCmd = ResponseCommand.RESPONSE_NONE;
                                ds.LampSetStateOff(DoorService.DoorType.DOOR_FRONT);
                                ds.closeDoor(DoorService.DoorType.DOOR_BACK);
                                ds.setDoorBusy(false);
                                StateForkLift = ForkLift.FORBUF_ROBOT_WAITTING_CLOSE_GATE;
                                robot.ShowText("FORBUF_ROBOT_WAITTING_CLOSE_GATE");
                            }
                            else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                            {
                                errorCode = ErrorCode.DETECT_LINE_ERROR;
                                CheckUserHandleError(this);
                            }
                        }
                        break;
                    case ForkLift.FORBUF_ROBOT_WAITTING_CLOSE_GATE:
                        try
                        {
                            
                            StateForkLift = ForkLift.FORBUF_ROBOT_CHECK_GOTO_BUFFER_OR_MACHINE;
                            robot.SwitchToDetectLine(false);
                            registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                            registryRobotJourney.startPoint = robot.properties.pose.Position;
                            registryRobotJourney.endPoint = endPointBuffer.Position ;
                           
                        }
                        catch (System.Exception)
                        {
                            errorCode = ErrorCode.CAN_NOT_GET_DATA;
                            CheckUserHandleError(this);
                        }
                        break;
                    case ForkLift.FORBUF_ROBOT_CHECK_GOTO_BUFFER_OR_MACHINE:
                        order.status = StatusOrderResponseCode.DELIVERING;
                        flToMachineInfo = GetPriorityTaskForkLiftToMachine(order.productDetailId);
                        if (flToMachineInfo == null)
                        {
                            StateForkLift = ForkLift.FORBUF_ROBOT_WAITTING_ZONE_BUFFER_READY;
                            registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                            registryRobotJourney.startPoint = robot.properties.pose.Position;
                            registryRobotJourney.endPoint = endPointBuffer.Position;
                        }
                        else
                        {
                           FreePlanedBuffer(order.palletId_P);
                           UpdatePalletState(PalletStatus.W, flToMachineInfo.palletId, flToMachineInfo.planId);
                           StateForkLift = ForkLift.FORMAC_ROBOT_GOTO_FRONTLINE_MACHINE_FROM_VIM_REG;
                        }
                        break;

                    case ForkLift.FORBUF_ROBOT_WAITTING_ZONE_BUFFER_READY:
                        if (TrafficRountineConstants.DetetectInsideStationCheck(registryRobotJourney))
                        {
                            break;
                        }
                        Pose destPos1 = endPointBuffer;
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
                                StateForkLift = ForkLift.FORBUF_ROBOT_SEND_CMD_DROPDOWN_PALLET_BUFFER;

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
                                StateForkLift = ForkLift.FORBUF_ROBOT_SEND_CMD_DROPDOWN_PALLET_BUFFER;
                            }
                        }
                        catch (System.Exception)
                        {
                            errorCode = ErrorCode.CAN_NOT_GET_DATA;
                            CheckUserHandleError(this);
                        }
                        break;
                    case ForkLift.FORBUF_ROBOT_SEND_CMD_DROPDOWN_PALLET_BUFFER:
                        JResult = FlToBuf.GetInfoPallet_P_InBuffer(PistonPalletCtrl.PISTON_PALLET_DOWN);
                        String data = JsonConvert.SerializeObject(JResult.jInfoPallet);
                        if (rb.SendCmdAreaPallet(data))
                        {
                            StateForkLift = ForkLift.FORBUF_ROBOT_WAITTING_DROPDOWN_PALLET_BUFFER;
                            robot.ShowText("FORBUF_ROBOT_WAITTING_DROPDOWN_PALLET_BUFFER");
                        }
                        break;
                    case ForkLift.FORBUF_ROBOT_WAITTING_DROPDOWN_PALLET_BUFFER:
                        if (resCmd == ResponseCommand.RESPONSE_LINEDETECT_PALLETDOWN)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            FlToBuf.UpdatePalletState(PalletStatus.W,JResult.palletId,order.planId);
                            //   rb.SendCmdPosPallet (RequestCommandPosPallet.REQUEST_GOBACK_FRONTLINE);
                            StateForkLift = ForkLift.FORBUF_ROBOT_WAITTING_GOBACK_FRONTLINE_BUFFER;
                            robot.ShowText("FORBUF_ROBOT_WAITTING_GOBACK_FRONTLINE_BUFFER");
                        }
                        else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                        {
                            Console.WriteLine("Loi Update :ForkLift.FORBUF_ROBOT_WAITTING_DROPDOWN_PALLET_BUFFER");
                            errorCode = ErrorCode.DETECT_LINE_ERROR;
                            CheckUserHandleError(this);
                        }
                        break;
                    case ForkLift.FORBUF_ROBOT_WAITTING_GOBACK_FRONTLINE_BUFFER: // đợi
                        if (resCmd == ResponseCommand.RESPONSE_FINISH_GOBACK_FRONTLINE)
                        {
                            robot.bayId = -1;
                            robot.bayIdReg = false;
                            robot.ReleaseWorkingZone();
                            robot.SwitchToDetectLine(false);
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            //rb.prioritLevel.OnAuthorizedPriorityProcedure = false;
                            StateForkLift = ForkLift.FORBUF_ROBOT_RELEASED;
                            robot.ShowText("FORBUF_ROBOT_RELEASED");
                        }
                        else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                        {
                            Console.WriteLine("Loi Update :ForkLift.FORBUF_ROBOT_WAITTING_GOBACK_FRONTLINE_BUFFER");
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
                            TrafficRountineConstants.DetectRelease(registryRobotJourney);
                            if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                            {
                                robot.SwitchToDetectLine(true);
                                StateForkLift = ForkLift.FORMAC_ROBOT_SEND_CMD_DROPDOWN_PALLET_MACHINE;
                            }
                        }
                        catch (System.Exception)
                        {
                            errorCode = ErrorCode.CAN_NOT_GET_DATA;
                            CheckUserHandleError(this);
                        }
                        break;
                    case ForkLift.FORMAC_ROBOT_SEND_CMD_DROPDOWN_PALLET_MACHINE:
                        if (rb.SendCmdAreaPallet(flToMachineInfo.infoPallet))
                        {
                            StateForkLift = ForkLift.FORMAC_ROBOT_WAITTING_DROPDOWN_PALLET_MACHINE;
                            robot.ShowText("FORMAC_ROBOT_WAITTING_DROPDOWN_PALLET_MACHINE");
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
                        ds.removeListCtrlDoorBack();
                        TrafficRountineConstants.ReleaseAll(robot);
                        robot.orderItem = null;
                       // Global_Object.onFlagDoorBusy = false;
                        robot.SwitchToDetectLine(false);
                       // robot.robotTag = RobotStatus.IDLE;
                        robot.ReleaseWorkingZone();
                        rb.PreProcedureAs = ProcedureControlAssign.PRO_FORKLIFT_TO_BUFFER;
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
                        ds.removeListCtrlDoorBack();
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
                        //FreePlanedBuffer(order.palletId_P);
                        order.endTimeProcedure = DateTime.Now;
                        order.totalTimeProcedure = order.endTimeProcedure.Subtract(order.startTimeProcedure).TotalMinutes;
                        SaveOrderItem(order);
                        KillEvent();
                        break;
                    //////////////////////////////////////////////////////
                    default:
                        break;
                }
                Thread.Sleep(50);
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
            public int palletId;
            public int planId;
        }
        public ForkLiftToMachineInfo GetPriorityTaskForkLiftToMachine(int productDetailId)
        {
            ForkLiftToMachineInfo forkLiftToMachineInfo = null;
            try
            {

                bool onHasOrder = false;
                foreach (DeviceItem deviceItem in deviceService.GetDeviceItemList())
                {
                    foreach (OrderItem _order in deviceItem.PendingOrderList)
                    {
                        if (_order.typeReq == TyeRequest.TYPEREQUEST_BUFFER_TO_MACHINE)
                        {
                            if (_order.productDetailId == productDetailId)
                            {
                                forkLiftToMachineInfo = new ForkLiftToMachineInfo();
                                forkLiftToMachineInfo.frontLinePose = _order.palletAtMachine.linePos;
                                JInfoPallet infoPallet = new JInfoPallet();

                                infoPallet.pallet = PistonPalletCtrl.PISTON_PALLET_DOWN; /* dropdown */
                                infoPallet.bay = _order.palletAtMachine.bay;
                                infoPallet.hasSubLine = "no"; /* no */
                                infoPallet.dir_main = (TrafficRobotUnity.BrDirection)_order.palletAtMachine.directMain;
                                infoPallet.dir_sub = (TrafficRobotUnity.BrDirection)_order.palletAtMachine.directSub;
                                infoPallet.dir_out = (TrafficRobotUnity.BrDirection)_order.palletAtMachine.directOut;
                                infoPallet.line_ord = _order.palletAtMachine.line_ord;
                                infoPallet.row = _order.palletAtMachine.row;

                                forkLiftToMachineInfo.infoPallet = JsonConvert.SerializeObject(infoPallet);
                                forkLiftToMachineInfo.palletId = _order.palletId_H;
                                forkLiftToMachineInfo.planId = _order.planId;
                                _order.status = StatusOrderResponseCode.CHANGED_FORKLIFT;
                                onHasOrder = true;
                                deviceItem.PendingOrderList.Remove(_order);
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
