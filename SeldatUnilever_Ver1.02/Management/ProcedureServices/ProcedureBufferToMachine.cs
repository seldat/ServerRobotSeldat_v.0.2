 using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SeldatMRMS.Management.RobotManagent;
using SeldatMRMS.Management.TrafficManager;
using SeldatUnilever_Ver1._02.Management.ProcedureServices;
using SeldatUnilever_Ver1._02.Management.TrafficManager;
using SelDatUnilever_Ver1._00.Management.DeviceManagement;
using static SeldatMRMS.Management.RobotManagent.RobotBaseService;
using static SeldatMRMS.Management.RobotManagent.RobotUnity;
using static SeldatMRMS.Management.RobotManagent.RobotUnityControl;
using static SeldatMRMS.Management.TrafficRobotUnity;
using static SelDatUnilever_Ver1._00.Management.DeviceManagement.DeviceItem;
using static SelDatUnilever_Ver1._00.Management.TrafficManager.TrafficRounterService;

namespace SeldatMRMS
{
    public class ProcedureBufferToMachine : TrafficProcedureService
    {
        public class DataBufferToMachine
        {
            // public Pose PointCheckInBuffer;
            // public Pose PointFrontLineBuffer;
            // public PointDetectBranching PointDetectLineBranching;
            // public PointDetect PointPickPallet;
            // public Pose PointFrontLineMachine;
            // public PointDetect PointDropPallet;
        }
        // DataBufferToMachine points;
        BufferToMachine StateBufferToMachine;

        public RobotUnity robot;
        bool onUpdatedPalletState = false;
        ResponseCommand resCmd;
        TrafficManagementService Traffic;
        public Pose frontLinePose;
        public JPallet jPResult;
        private DeviceRegistrationService deviceService;
        public void Registry(DeviceRegistrationService deviceService)
        {
            this.deviceService = deviceService;
        }
        public override event Action<Object> ReleaseProcedureHandler;
        // public override event Action<Object> ErrorProcedureHandler;
        public ProcedureBufferToMachine(RobotUnity robot, TrafficManagementService trafficService) : base(robot,trafficService)
        {
            StateBufferToMachine = BufferToMachine.BUFMAC_IDLE;
            this.robot = robot;
            // this.points = new DataBufferToMachine();
            this.Traffic = trafficService;
            procedureCode = ProcedureCode.PROC_CODE_BUFFER_TO_MACHINE;
        }

        public void Start(BufferToMachine state = BufferToMachine.BUFMAC_SELECT_BEHAVIOR_ONZONE)
        {
            errorCode = ErrorCode.RUN_OK;
            robot.robotTag = RobotStatus.WORKING;
            robot.ProcedureAs = ProcedureControlAssign.PRO_BUFFER_TO_MACHINE;
            StateBufferToMachine = state;
            Task ProBuferToMachine = new Task(() => this.Procedure(this));
            procedureStatus = ProcedureStatus.PROC_ALIVE;
            ProRun = true;
            ProRunStopW = true;
            //robot.prioritLevel.OnAuthorizedPriorityProcedure = false;
            order.startTimeProcedure = DateTime.Now;
            registryRobotJourney = new RegistryRobotJourney();
            registryRobotJourney.robot = robot;
            registryRobotJourney.traffic = Traffic;
            robot.bayId = -1;
            ProBuferToMachine.Start();
        }
        public void Destroy()
        {
            ProRunStopW = false;
            robot.robotTag = RobotStatus.IDLE;
            robot.orderItem = null;
            StateBufferToMachine = BufferToMachine.BUFMAC_ROBOT_DESTROY;
            TrafficRountineConstants.ReleaseAll(robot);
            robot.bayId = -1;

        }

        public void RestoreOrderItem()
        {
            OrderItem _order = new OrderItem();
            _order.activeDate = order.activeDate;
            _order.bufferId = order.bufferId;

            dynamic product = new JObject();
            product.timeWorkId = order.timeWorkId;
            product.activeDate = order.activeDate;
            order.dateTime = (string)DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss tt");
            product.productId = order.productId;
            product.productDetailId = order.productDetailId;
            // chu y sua 
            product.palletStatus = PalletStatus.W.ToString(); // W

            _order.dataRequest = product.ToString();
            _order.dateTime = order.dateTime;
            _order.deviceId = order.deviceId;
            _order.palletAtMachine = order.palletAtMachine;
            _order.palletId = order.palletId;
            _order.palletStatus = order.palletStatus;
            _order.planId = order.planId;
            _order.productDetailId = order.productDetailId;
            _order.productDetailName = order.productDetailName;
            _order.productId = order.productId;
            _order.robot = "";
            _order.typeReq = order.typeReq;
            _order.updUsrId = order.updUsrId;
            _order.userName = order.userName;
            _order.lengthPallet = order.lengthPallet;
            _order.palletAmount = order.palletAmount;
            _order.bufferId = order.bufferId;
            _order.status = StatusOrderResponseCode.PENDING;

            deviceService.FindDeviceItem(_order.userName).AddOrder(_order);
        }
        public void Procedure(object ojb)
        {
            ProcedureBufferToMachine BfToMa = (ProcedureBufferToMachine)ojb;
            RobotUnity rb = BfToMa.robot;
            // DataBufferToMachine p = BfToMa.points;
            TrafficManagementService Traffic = BfToMa.Traffic;
            robot.ShowText(" Start -> " + procedureCode);
            //GetFrontLineBuffer(false);
            //  ProRun = false;
            rb.mcuCtrl.lampRbOn();
            frontLinePose = BfToMa.GetFrontLineBuffer();
            while (ProRun)
            {
                switch (StateBufferToMachine)
                {
                    case BufferToMachine.BUFMAC_IDLE:
                        //robot.ShowText("BUFMAC_IDLE");
                        break;
                    case BufferToMachine.BUFMAC_SELECT_BEHAVIOR_ONZONE:
                        if (Traffic.RobotIsInArea("READY", robot.properties.pose.Position,TypeZone.OPZS))
                        {
                            if (rb.PreProcedureAs == ProcedureControlAssign.PRO_READY)
                            {
                                    StateBufferToMachine = BufferToMachine.BUFMAC_ROBOT_GOTO_BACK_FRONTLINE_READY;
                                    registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                    registryRobotJourney.startPoint = robot.properties.pose.Position;
                                    registryRobotJourney.endPoint = frontLinePose.Position;
                                    //robot.ShowText("BUFMAC_SELECT_BEHAVIOR_ONZONE : READY");
                                    //robot.ShowText("CHECK - REG");
                            }
                        }
                        else if (Traffic.RobotIsInArea("VIM", robot.properties.pose.Position,TypeZone.MAIN_ZONE))
                        {
                                //robot.ShowText("BUFMAC_SELECT_BEHAVIOR_ONZONE : VIM");
                                if (rb.SendPoseStamped(frontLinePose))
                                {
                                    StateBufferToMachine = BufferToMachine.BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_VIM_REG;
                                    registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                    registryRobotJourney.startPoint = robot.properties.pose.Position;
                                    registryRobotJourney.endPoint = frontLinePose.Position;
                                    //robot.ShowText("BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER");
                                    //robot.ShowText("CHECK - REG");
                            }
                        }
                        else if (Traffic.RobotIsInArea("OUTER", robot.properties.pose.Position,TypeZone.MAIN_ZONE))
                        {
                            Point destPos1 = frontLinePose.Position;
                            String destName1 = Traffic.DetermineArea(destPos1, TypeZone.MAIN_ZONE);
                            //robot.ShowText("BUFMAC_SELECT_BEHAVIOR_ONZONE : OUTER");
                            if (destName1.Equals("OUTER"))
                            {
                                //robot.ShowText("COME FRONTLINE : OUTER");
                                if (rb.SendPoseStamped(frontLinePose))
                                {
                                    StateBufferToMachine = BufferToMachine.BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER;
                                    registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                    registryRobotJourney.startPoint = robot.properties.pose.Position;
                                    registryRobotJourney.endPoint = destPos1;
                                    //robot.ShowText("BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER");
                                }
                            }
                            else if(destName1.Equals("VIM"))
                            {
                                //robot.ShowText("COME FRONTLINE : OUTER");
                                if (rb.SendPoseStamped(frontLinePose))
                                {
                                    StateBufferToMachine = BufferToMachine.BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_VIM_REG;
                                    registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                    registryRobotJourney.startPoint = robot.properties.pose.Position;
                                    registryRobotJourney.endPoint = destPos1;
                                    //robot.ShowText("BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER");
                                }
                            }
                        }

                        break;
                    case BufferToMachine.BUFMAC_ROBOT_GOTO_BACK_FRONTLINE_READY:
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
                                robot.onFlagGoBackReady = true;
                                if (resCmd == ResponseCommand.RESPONSE_FINISH_GOBACK_FRONTLINE)
                                {
                                    robot.onFlagGoBackReady = false;
                                    resCmd = ResponseCommand.RESPONSE_NONE;
                                    Point destPos2 = frontLinePose.Position;
                                    String destName2 = Traffic.DetermineArea(destPos2, TypeZone.MAIN_ZONE);
                                    if (destName2.Equals("OUTER"))
                                    {
                                        //robot.ShowText("GO FRONTLINE IN OUTER");
                                        if (rb.SendPoseStamped(frontLinePose))
                                        {
                                            StateBufferToMachine = BufferToMachine.BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER;
                                            registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                            registryRobotJourney.startPoint = robot.properties.pose.Position;
                                            registryRobotJourney.endPoint = destPos2;
                                            //robot.ShowText("BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER");
                                        }
                                    }
                                    else if (destName2.Equals("VIM"))
                                    {
                                        //robot.ShowText("GO FRONTLINE IN VIM");
                                        if (rb.SendPoseStamped(frontLinePose))
                                        {
                                            StateBufferToMachine = BufferToMachine.BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_VIM_FROM_READY;
                                            registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                            registryRobotJourney.startPoint = robot.properties.pose.Position;
                                            registryRobotJourney.endPoint = destPos2;
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
                    case BufferToMachine.BUFMAC_ROBOT_WAITTING_GOTO_CHECKIN_BUFFER_VIM:
                        if (TrafficRountineConstants.DetetectInsideStationCheck(registryRobotJourney))
                        {
                            break;
                        }
                        else
                        {
                            TrafficRountineConstants.DetectRelease(registryRobotJourney);
                        }
                        if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            //rb.prioritLevel.OnAuthorizedPriorityProcedure = true;
                            StateBufferToMachine = BufferToMachine.BUFMAC_ROBOT_WAITTING_ZONE_BUFFER_READY;
                            //robot.ShowText("BUFMAC_ROBOT_WAITTING_ZONE_BUFFER_READY");
                        }
                        break;
                    case BufferToMachine.BUFMAC_ROBOT_WAITTING_GOTO_CHECKIN_BUFFER: // doi robot di den khu vuc checkin cua vung buffer
                        if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            StateBufferToMachine = BufferToMachine.BUFMAC_ROBOT_WAITTING_ZONE_BUFFER_READY;
                            //robot.ShowText("BUFMAC_ROBOT_WAITTING_ZONE_BUFFER_READY");
                        }
                        break;
                    case BufferToMachine.BUFMAC_ROBOT_WAITTING_ZONE_BUFFER_READY: // doi khu vuc buffer san sang de di vao
                        try
                        {
                            if (false == robot.CheckInZoneBehavior(BfToMa.GetAnyPointInBuffer().Position))
                            {
                                if (rb.SendPoseStamped(frontLinePose))
                                {
                                    StateBufferToMachine = BufferToMachine.BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER;
                                    //robot.ShowText("BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER");
                                }
                            }
                        }
                        catch (System.Exception)
                        {
                            errorCode = ErrorCode.CAN_NOT_GET_DATA;
                            CheckUserHandleError(this);
                        }
                        break;
                    case BufferToMachine.BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_VIM_FROM_READY:
                        TrafficRountineConstants.DetectRelease(registryRobotJourney);
                        if (TrafficCheckInBuffer(goalFrontLinePos, bayId))
                            break;
                        try
                        {
                            if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                            {
                                robot.SwitchToDetectLine(true);
                                resCmd = ResponseCommand.RESPONSE_NONE;
                                //rb.prioritLevel.OnAuthorizedPriorityProcedure = true;
                                JPallet jInfoPallet_H = BfToMa.GetInfoPallet_H_InBuffer(PistonPalletCtrl.PISTON_PALLET_UP);
                                jPResult = BfToMa.GetInfoOfPalletBuffer_Compare_W_H(PistonPalletCtrl.PISTON_PALLET_UP, jInfoPallet_H.jInfoPallet);

                                String data = JsonConvert.SerializeObject(jPResult.jInfoPallet);

                                if (rb.SendCmdAreaPallet(data))
                                {
                                    StateBufferToMachine = BufferToMachine.BUFMAC_ROBOT_WAITTING_PICKUP_PALLET_BUFFER;
                                    //robot.ShowText("BUFMAC_ROBOT_WAITTING_PICKUP_PALLET_BUFFER");
                                }
                            }
                        }
                        catch (System.Exception)
                        {
                            errorCode = ErrorCode.CAN_NOT_GET_DATA;
                            CheckUserHandleError(this);
                        }
                        break;
                    case BufferToMachine.BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_VIM_REG:

                        if (TrafficRountineConstants.DetetectInsideStationCheck(registryRobotJourney))
                        {
                            break;
                        }
                        else
                        {
                            StateBufferToMachine = BufferToMachine.BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_VIM;
                        }
                        break;
                    case BufferToMachine.BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_VIM:
                        TrafficRountineConstants.DetectRelease(registryRobotJourney);
                        if (TrafficCheckInBuffer(goalFrontLinePos, bayId))
                            break;
                        try
                        {
                            if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                            {
                                robot.SwitchToDetectLine(true);
                                resCmd = ResponseCommand.RESPONSE_NONE;
                                //rb.prioritLevel.OnAuthorizedPriorityProcedure = true;
                                JPallet jInfoPallet_H = BfToMa.GetInfoPallet_H_InBuffer(PistonPalletCtrl.PISTON_PALLET_UP);
                                jPResult = BfToMa.GetInfoOfPalletBuffer_Compare_W_H(PistonPalletCtrl.PISTON_PALLET_UP, jInfoPallet_H.jInfoPallet);
                                String data = JsonConvert.SerializeObject(jPResult.jInfoPallet);
                                if (rb.SendCmdAreaPallet(data))
                                {
                                    StateBufferToMachine = BufferToMachine.BUFMAC_ROBOT_WAITTING_PICKUP_PALLET_BUFFER;
                                    //robot.ShowText("BUFMAC_ROBOT_WAITTING_PICKUP_PALLET_BUFFER");
                                }
                            }
                        }
                        catch (System.Exception)
                        {
                            errorCode = ErrorCode.CAN_NOT_GET_DATA;
                            CheckUserHandleError(this);
                        }
                        break;
                    case BufferToMachine.BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER:
                        // dò và release vùng đăng ky
                        if (TrafficCheckInBuffer(goalFrontLinePos, bayId))
                            break;
                        TrafficRountineConstants.DetectRelease(registryRobotJourney);
                        try
                        {
                            if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                            {
                                robot.SwitchToDetectLine(true);                       
                                resCmd = ResponseCommand.RESPONSE_NONE;
                                //rb.prioritLevel.OnAuthorizedPriorityProcedure = true;
                                JPallet jInfoPallet_H = BfToMa.GetInfoPallet_H_InBuffer(PistonPalletCtrl.PISTON_PALLET_UP);
                                jPResult = BfToMa.GetInfoOfPalletBuffer_Compare_W_H(PistonPalletCtrl.PISTON_PALLET_UP, jInfoPallet_H.jInfoPallet);
                                String data = JsonConvert.SerializeObject(jPResult.jInfoPallet);
                                if (rb.SendCmdAreaPallet(data))
                                {
                                    StateBufferToMachine = BufferToMachine.BUFMAC_ROBOT_WAITTING_PICKUP_PALLET_BUFFER;
                                    //robot.ShowText("BUFMAC_ROBOT_WAITTING_PICKUP_PALLET_BUFFER");
                                }
                            }
                        }
                        catch (System.Exception)
                        {
                            errorCode = ErrorCode.CAN_NOT_GET_DATA;
                            CheckUserHandleError(this);
                        }
                        break;
                    case BufferToMachine.BUFMAC_ROBOT_WAITTING_PICKUP_PALLET_BUFFER:
                        if (resCmd == ResponseCommand.RESPONSE_LINEDETECT_PALLETUP)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                 
                            BfToMa.UpdatePalletState(PalletStatus.F, jPResult.palletId, order.planId);
                            onUpdatedPalletState = true;
                            StateBufferToMachine = BufferToMachine.BUFMAC_ROBOT_WAITTING_GOBACK_FRONTLINE_BUFFER;
                            //robot.ShowText("BUFMAC_ROBOT_WAITTING_GOBACK_FRONTLINE_BUFFER");
                        }
                        else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                        {
                            errorCode = ErrorCode.DETECT_LINE_ERROR;
                            CheckUserHandleError(this);
                        }
                        break;
                    case BufferToMachine.BUFMAC_ROBOT_WAITTING_GOBACK_FRONTLINE_BUFFER: // đợi
                        try
                        {
                            if (resCmd == ResponseCommand.RESPONSE_FINISH_GOBACK_FRONTLINE)
                            {
                                robot.bayId = -1;
                                robot.ReleaseWorkingZone();
                                robot.SwitchToDetectLine(false);                              
                                resCmd = ResponseCommand.RESPONSE_NONE;
                                StateBufferToMachine = BufferToMachine.BUFMAC_ROBOT_GOTO_FRONTLINE_DROPDOWN_PALLET_SELECT_ZONE;
                                // cập nhật lại điểm xuất phát
                                registryRobotJourney.startPoint = robot.properties.pose.Position;
                                //robot.ShowText("BUFMAC_ROBOT_GOTO_FRONTLINE_DROPDOWN_PALLET");
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
                    case BufferToMachine.BUFMAC_ROBOT_GOTO_FRONTLINE_DROPDOWN_PALLET_SELECT_ZONE:
                        String startNamePoint= Traffic.DetermineArea(registryRobotJourney.startPoint, TypeZone.MAIN_ZONE);
                        Pose destPos = BfToMa.GetFrontLineMachine();
                        String destName = Traffic.DetermineArea(destPos.Position, TypeZone.MAIN_ZONE);
                        if (startNamePoint.Equals("VIM"))
                        {
                            if (rb.SendPoseStamped(destPos))
                            {
                                StateBufferToMachine = BufferToMachine.BUFMAC_ROBOT_GOTO_FRONTLINE_DROPDOWN_PALLET_IN_VIM_REG;
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
                                    StateBufferToMachine = BufferToMachine.BUFMAC_ROBOT_GOTO_FRONTLINE_DROPDOWN_PALLET;
                                    registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                    registryRobotJourney.startPoint = robot.properties.pose.Position;
                                    registryRobotJourney.endPoint = destPos.Position;
                                    //robot.ShowText("BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER");
                                }
                            }
                            else if(destName.Equals("VIM"))
                            {
                                if (rb.SendPoseStamped(destPos))
                                {
                                    StateBufferToMachine = BufferToMachine.BUFMAC_ROBOT_GOTO_FRONTLINE_DROPDOWN_PALLET_IN_VIM_REG;
                                    registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                    registryRobotJourney.startPoint = robot.properties.pose.Position;
                                    registryRobotJourney.endPoint = destPos.Position;
                                    //robot.ShowText("BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER");
                                }
                            }
                        }
                        break;
                    case BufferToMachine.BUFMAC_ROBOT_GOTO_FRONTLINE_DROPDOWN_PALLET_IN_VIM_REG:
                        if (TrafficRountineConstants.DetetectInsideStationCheck(registryRobotJourney))
                        {
                            break;
                        }
                        else
                        {
                            StateBufferToMachine = BufferToMachine.BUFMAC_ROBOT_GOTO_FRONTLINE_DROPDOWN_PALLET_IN_VIM;
                        }
                        break;
                    case BufferToMachine.BUFMAC_ROBOT_GOTO_FRONTLINE_DROPDOWN_PALLET_IN_VIM:
                        try
                        {
                            TrafficRountineConstants.DetectRelease(registryRobotJourney);
                            if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                            {
                                robot.SwitchToDetectLine(true);
                      
                                if (rb.SendCmdAreaPallet(BfToMa.GetInfoOfPalletMachine(PistonPalletCtrl.PISTON_PALLET_DOWN)))
                                {
                                    //rb.prioritLevel.OnAuthorizedPriorityProcedure = true;
                                    StateBufferToMachine = BufferToMachine.BUFMAC_ROBOT_WAITTING_DROPDOWN_PALLET;
                                    //robot.ShowText("BUFMAC_ROBOT_WAITTING_DROPDOWN_PALLET");
                                }
                            }
                        }
                        catch (System.Exception)
                        {
                            errorCode = ErrorCode.CAN_NOT_GET_DATA;
                            CheckUserHandleError(this);
                        }
                        break;
                    case BufferToMachine.BUFMAC_ROBOT_GOTO_FRONTLINE_DROPDOWN_PALLET:
                        try
                        {
                            if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                            {
                                robot.SwitchToDetectLine(true);

                                if (rb.SendCmdAreaPallet(BfToMa.GetInfoOfPalletMachine(PistonPalletCtrl.PISTON_PALLET_DOWN)))
                                {
                                    //rb.prioritLevel.OnAuthorizedPriorityProcedure = true;
                                    StateBufferToMachine = BufferToMachine.BUFMAC_ROBOT_WAITTING_DROPDOWN_PALLET;
                                    //robot.ShowText("BUFMAC_ROBOT_WAITTING_DROPDOWN_PALLET");
                                }
                            }
                        }
                        catch (System.Exception)
                        {
                            errorCode = ErrorCode.CAN_NOT_GET_DATA;
                            CheckUserHandleError(this);
                        }
                        break;
                    case BufferToMachine.BUFMAC_ROBOT_WAITTING_DROPDOWN_PALLET:
                        if (resCmd == ResponseCommand.RESPONSE_LINEDETECT_PALLETDOWN)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            StateBufferToMachine = BufferToMachine.BUFMAC_ROBOT_WAITTING_GOTO_FRONTLINE;
                            //robot.ShowText("BUFMAC_ROBOT_WAITTING_GOTO_FRONTLINE");
                        }
                        else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                        {
                            errorCode = ErrorCode.DETECT_LINE_ERROR;
                            CheckUserHandleError(this);
                        }
                        break;
                    case BufferToMachine.BUFMAC_ROBOT_WAITTING_GOTO_FRONTLINE:
                        if (resCmd == ResponseCommand.RESPONSE_FINISH_GOBACK_FRONTLINE)
                        {
                            robot.SwitchToDetectLine(false);
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            //rb.prioritLevel.OnAuthorizedPriorityProcedure = false;
                            StateBufferToMachine = BufferToMachine.BUFMAC_ROBOT_RELEASED;
                            //robot.ShowText("BUFMAC_ROBOT_RELEASED");
                        }
                        else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                        {
                            errorCode = ErrorCode.DETECT_LINE_ERROR;
                            CheckUserHandleError(this);
                        }
                        break;
                    case BufferToMachine.BUFMAC_ROBOT_RELEASED: // trả robot về robotmanagement để nhận quy trình mới

                        TrafficRountineConstants.ReleaseAll(robot);
                        robot.bayId = -1;
                        robot.orderItem = null;
                        robot.SwitchToDetectLine(false);
                        // Release WorkinZone Robot
                   //   robot.robotTag = RobotStatus.IDLE;
                        rb.PreProcedureAs = ProcedureControlAssign.PRO_BUFFER_TO_MACHINE;
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
                        SaveOrderItem(order);
                        KillEvent();
                        break;
                    case BufferToMachine.BUFMAC_ROBOT_DESTROY:
                        order.endTimeProcedure = DateTime.Now;
                        order.totalTimeProcedure = order.endTimeProcedure.Subtract(order.startTimeProcedure).TotalMinutes;
                        SaveOrderItem(order);
                        robot.SwitchToDetectLine(false);
                        robot.ReleaseWorkingZone();
                        // StateBufferToMachine = BufferToMachine.BUFMAC_ROBOT_RELEASED;
                        //robot.prioritLevel.OnAuthorizedPriorityProcedure = false;
                        ProRun = false;
                        UpdateInformationInProc(this, ProcessStatus.F);
                        order.status = StatusOrderResponseCode.ROBOT_ERROR;
                        //reset status pallet Faile H->Ws
                        selectHandleError = SelectHandleError.CASE_ERROR_EXIT;
                        procedureStatus = ProcedureStatus.PROC_KILLED;
                        //FreeHoldBuffer(order.palletId_H);
                        KillEvent();

                        //this.robot.DestroyRegistrySolvedForm();
                        break;
                    default:
                        break;
                }
             //   robot.ShowText("-> " + procedureCode);
                Thread.Sleep(50);
            }
            StateBufferToMachine = BufferToMachine.BUFMAC_IDLE;
        }
        public override void FinishStatesCallBack(Int32 message)
        {
            this.resCmd = (ResponseCommand)message;
            base.FinishStatesCallBack(message);
            if (this.resCmd == ResponseCommand.RESPONSE_FINISH_GOBACK_FRONTLINE)
            {
                // reset giao thông và xóa vùng đăng ký
                robot.SetSafeSmallcircle(true);

            }
        }
        protected override void CheckUserHandleError(object obj)
        {
            if (errorCode == ErrorCode.CAN_NOT_GET_DATA)
            {
                if (!this.Traffic.RobotIsInArea("READY", robot.properties.pose.Position))
                {
                    ProRun = false;
                    robot.setColorRobotStatus(RobotStatusColorCode.ROBOT_STATUS_CAN_NOTGET_DATA);
                    order.status = StatusOrderResponseCode.NO_BUFFER_DATA;
                    robot.TurnOnSupervisorTraffic(true);
                    robot.PreProcedureAs = robot.ProcedureAs;
                    // reset pallet state

                    ReleaseProcedureHandler(obj);
                    return;
                }
                else
                {
                    ProRun = false;
                    robot.setColorRobotStatus(RobotStatusColorCode.ROBOT_STATUS_CAN_NOTGET_DATA);
                    order.status = StatusOrderResponseCode.NO_BUFFER_DATA;
                    robot.TurnOnSupervisorTraffic(true);
                    return;
                }
            }
            base.CheckUserHandleError(obj);
        }
    }
}
