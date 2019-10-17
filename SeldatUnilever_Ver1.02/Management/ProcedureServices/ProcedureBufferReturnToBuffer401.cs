using Newtonsoft.Json.Linq;
using SeldatMRMS;
using SeldatMRMS.Management.RobotManagent;
using SeldatMRMS.Management.TrafficManager;
using SeldatUnilever_Ver1._02.Management.TrafficManager;
using SelDatUnilever_Ver1._00.Management.DeviceManagement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static SeldatMRMS.Management.RobotManagent.RobotBaseService;
using static SeldatMRMS.Management.RobotManagent.RobotUnity;
using static SeldatMRMS.Management.RobotManagent.RobotUnityControl;
using static SeldatMRMS.Management.TrafficRobotUnity;
using static SeldatMRMS.ProcedureControlServices;
using static SelDatUnilever_Ver1._00.Management.DeviceManagement.DeviceItem;
using static SelDatUnilever_Ver1._00.Management.TrafficManager.TrafficRounterService;

namespace SeldatUnilever_Ver1._02.Management.ProcedureServices
{
    public class ProcedureBufferReturnToBuffer401 : TrafficProcedureService
    {
        BufferToBuffer StateBufferToBuffer;

        public RobotUnity robot;
        bool onUpdatedPalletState = false;
        ResponseCommand resCmd;
        TrafficManagementService Traffic;
        private DeviceRegistrationService deviceService;
        public void Registry(DeviceRegistrationService deviceService)
        {
            this.deviceService = deviceService;
        }
        public override event Action<Object> ReleaseProcedureHandler;
        // public override event Action<Object> ErrorProcedureHandler;
        public ProcedureBufferReturnToBuffer401(RobotUnity robot, TrafficManagementService trafficService) : base(robot, trafficService)
        {
            StateBufferToBuffer = BufferToBuffer.BUFTOBUF_IDLE;
            this.robot = robot;
            // this.points = new DataBufferToBuffer();
            this.Traffic = trafficService;
            procedureCode = ProcedureCode.PROC_CODE_BUFFER_TO_BUFFER;
        }

        public void Start(BufferToBuffer state = BufferToBuffer.BUFTOBUF_SELECT_BEHAVIOR_ONZONE_BUFFER_A)
        {
            robot.bayId = -1;
            robot.bayIdReg = false;
            errorCode = ErrorCode.RUN_OK;
            robot.robotTag = RobotStatus.WORKING;
            robot.ProcedureAs = ProcedureControlAssign.PRO_BUFFER_TO_BUFFER;
            StateBufferToBuffer = state;
            Task ProBuferToMachine = new Task(() => this.Procedure(this));
            procedureStatus = ProcedureStatus.PROC_ALIVE;
            ProRun = true;
            ProRunStopW = true;
            //robot.prioritLevel.OnAuthorizedPriorityProcedure = false;
            order.startTimeProcedure = DateTime.Now;
            registryRobotJourney = new RegistryRobotJourney();
            registryRobotJourney.robot = robot;
            registryRobotJourney.traffic = Traffic;
            ProBuferToMachine.Start();
        }
        public void Destroy()
        {
            robot.bayId = -1;
            ProRunStopW = false;
            robot.bayIdReg = false;
            robot.robotTag = RobotStatus.IDLE;
            robot.orderItem = null;
            StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_DESTROY;
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
            ProcedureBufferReturnToBuffer401 BfToBf = (ProcedureBufferReturnToBuffer401)ojb;
            RobotUnity rb = BfToBf.robot;
            TrafficManagementService Traffic = BfToBf.Traffic;
            robot.ShowText(" Start -> " + procedureCode);
            rb.mcuCtrl.lampRbOn();
            while (ProRun)
            {
                switch (StateBufferToBuffer)
                {
                    case BufferToBuffer.BUFTOBUF_IDLE:
                        //robot.ShowText("BUFTOBUF_IDLE");
                        break;
                    case BufferToBuffer.BUFTOBUF_SELECT_BEHAVIOR_ONZONE_BUFFER_A:
                        if (Traffic.RobotIsInArea("READY", robot.properties.pose.Position))
                        {
                            Pose endPose = BfToBf.GetFrontLineBufferReturn_BRB401(order.dataRequest_BufferReturn);
                            if (rb.PreProcedureAs == ProcedureControlAssign.PRO_READY)
                            {
                                StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_GOTO_BACK_FRONTLINE_READY;
                                registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position);
                                registryRobotJourney.startPoint = robot.properties.pose.Position;
                                registryRobotJourney.endPoint = endPose.Position;
                            }
                        }
                        else if (Traffic.RobotIsInArea("VIM", robot.properties.pose.Position))
                        {
                                Pose endPose = BfToBf.GetFrontLineBufferReturn_BRB401(order.dataRequest_BufferReturn);
                                if (rb.SendPoseStamped(endPose))
                                {
                                    StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_A_FROM_VIM_REG;
                                    registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position);
                                    registryRobotJourney.startPoint = robot.properties.pose.Position;
                                    registryRobotJourney.endPoint = endPose.Position;
                                    //robot.ShowText("BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER");
                                }
                        }
                        else if (Traffic.RobotIsInArea("OUTER", robot.properties.pose.Position))
                        {
                            Pose destPos1 = BfToBf.GetFrontLineBufferReturn_BRB401(order.dataRequest_BufferReturn);
                            String destName1 = Traffic.DetermineArea(destPos1.Position, TypeZone.MAIN_ZONE);
                            if (destName1.Equals("OUTER"))
                            {
                                //robot.ShowText("GO FRONTLINE IN OUTER");
                                if (rb.SendPoseStamped(destPos1))
                                {
                                    StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_A;
                                    registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                    registryRobotJourney.startPoint = robot.properties.pose.Position;
                                    registryRobotJourney.endPoint = destPos1.Position;
                                    //robot.ShowText("BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER");
                                }
                            }
                            else if (destName1.Equals("VIM"))
                            {
                                //robot.ShowText("GO FRONTLINE IN VIM");
                                if (rb.SendPoseStamped(destPos1))
                                {
                                    StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_A_FROM_VIM_REG;
                                    registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                    registryRobotJourney.startPoint = robot.properties.pose.Position;
                                    registryRobotJourney.endPoint = destPos1.Position;
                                    //robot.ShowText("BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER");
                                    //robot.ShowText("CHECK - REG");
                                }
                            }

                        }
                        break;
                    case BufferToBuffer.BUFTOBUF_ROBOT_GOTO_BACK_FRONTLINE_READY:
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
                                    
                                    Pose destPos2 = BfToBf.GetFrontLineBufferReturn_BRB401(order.dataRequest_BufferReturn);
                                    String destName2 = Traffic.DetermineArea(destPos2.Position, TypeZone.MAIN_ZONE);
                                    if (destName2.Equals("OUTER"))
                                    {
                                        //robot.ShowText("GO FRONTLINE IN OUTER");
                                        if (rb.SendPoseStamped(destPos2))
                                        {
                                            resCmd = ResponseCommand.RESPONSE_NONE;
                                            StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_A;
                                            registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                            registryRobotJourney.startPoint = robot.properties.pose.Position;
                                            registryRobotJourney.endPoint = destPos2.Position;
                                            //robot.ShowText("BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER");
                                        }
                                    }
                                    else if (destName2.Equals("VIM"))
                                    {
                                        //robot.ShowText("GO FRONTLINE IN VIM");
                                        if (rb.SendPoseStamped(destPos2))
                                        {
                                            resCmd = ResponseCommand.RESPONSE_NONE;
                                            StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_A_FROM_VIM_READY;
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
                    case BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_A_FROM_VIM_READY:
                        TrafficRountineConstants.DetectRelease(registryRobotJourney);
                        if (TrafficCheckInBuffer(goalFrontLinePos, bayId))
                            break;
                       
                        try
                        {
                            //bool onComePoint2 = robot.ReachedGoal();
                            if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                            //if (onComePoint2)
                            {
                                // 

                                robot.SwitchToDetectLine(true);

                                resCmd = ResponseCommand.RESPONSE_NONE;
                                //rb.prioritLevel.OnAuthorizedPriorityProcedure = true;
                                StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_SEND_CMD_PICKUP_PALLET_BUFFER_A;
                            }
                        }
                        catch (System.Exception)
                        {
                            errorCode = ErrorCode.CAN_NOT_GET_DATA;
                            CheckUserHandleError(this);
                        }
                        break;
                    case BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_A_FROM_VIM_REG:

                        if (TrafficRountineConstants.DetetectInsideStationCheck(registryRobotJourney))
                        {
                            break;
                        }
                        else
                        {
                            StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_A_FROM_VIM;
                        }
                        break;
                    case BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_A_FROM_VIM:
                        TrafficRountineConstants.DetectRelease(registryRobotJourney);
                        if (TrafficCheckInBuffer(goalFrontLinePos, bayId))
                            break;
                        try
                        {
                            if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                            {
                                robot.SwitchToDetectLine(true);

                                resCmd = ResponseCommand.RESPONSE_NONE;
                                StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_SEND_CMD_PICKUP_PALLET_BUFFER_A;
                            }
                        }
                        catch (System.Exception)
                        {
                            errorCode = ErrorCode.CAN_NOT_GET_DATA;
                            CheckUserHandleError(this);
                        }
                        break;
                    case BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_A:
                        try
                        {
                            //bool onComePoint2 = robot.ReachedGoal();
                            if (TrafficCheckInBuffer(goalFrontLinePos, bayId))
                                break;
                            if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                            //if (onComePoint2)
                            {
                                robot.SwitchToDetectLine(true);
                                resCmd = ResponseCommand.RESPONSE_NONE;
                                //rb.prioritLevel.OnAuthorizedPriorityProcedure = true;
                                StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_SEND_CMD_PICKUP_PALLET_BUFFER_A;
                            }
                        }
                        catch (System.Exception)
                        {
                            errorCode = ErrorCode.CAN_NOT_GET_DATA;
                            CheckUserHandleError(this);
                        }
                        break;
                    case BufferToBuffer.BUFTOBUF_ROBOT_SEND_CMD_PICKUP_PALLET_BUFFER_A:
                        if (rb.SendCmdAreaPallet(BfToBf.GetInfoOfPalletBufferReturn_BRB401(PistonPalletCtrl.PISTON_PALLET_UP, order.dataRequest_BufferReturn)))
                        {
                            StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_PICKUP_PALLET_BUFFER_A;
                        }
                        break;
                    case BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_PICKUP_PALLET_BUFFER_A:
                        if (resCmd == ResponseCommand.RESPONSE_LINEDETECT_PALLETUP)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            BfToBf.UpdatePalletState(PalletStatus.F,order.palletId_H,order.planId);
                            onUpdatedPalletState = true;
                            StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_GOBACK_FRONTLINE_BUFFER_A;
                            //robot.ShowText("BUFTOBUF_ROBOT_WAITTING_GOBACK_FRONTLINE_BUFFER_A");
                        }
                        else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                        {
                            errorCode = ErrorCode.DETECT_LINE_ERROR;
                            CheckUserHandleError(this);
                        }
                        break;
                    case BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_GOBACK_FRONTLINE_BUFFER_A: // đợi
                        try
                        {
                            if (resCmd == ResponseCommand.RESPONSE_FINISH_GOBACK_FRONTLINE)
                            {
                                robot.bayId = -1;
                                robot.bayIdReg = false;
                                robot.ReleaseWorkingZone();
                                robot.SwitchToDetectLine(false);

                                resCmd = ResponseCommand.RESPONSE_NONE;
                                StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_BUFFER_B_SELECT_ZONE;
                                // cập nhật lại điểm xuất phát
                                registryRobotJourney.startPoint = robot.properties.pose.Position;
                                //robot.ShowText("BUFTOBUF_ROBOT_WAITTING_GOTO_FRONTLINE_BUFFER_B");
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

                       case BufferToBuffer.BUFTOBUF_ROBOT_BUFFER_B_SELECT_ZONE:
                        String startNamePoint = Traffic.DetermineArea(registryRobotJourney.startPoint, TypeZone.MAIN_ZONE);
                        Pose destPos = BfToBf.GetFrontLineBuffer401_BRB401(order.dataRequest_Buffer401);
                        String destName = Traffic.DetermineArea(destPos.Position, TypeZone.MAIN_ZONE);
                        if (startNamePoint.Equals("VIM"))
                        {
                            if (rb.SendPoseStamped(destPos))
                            {
                                StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_B_FROM_VIM_REG;
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
                                    StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_B;
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
                                    StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_B_FROM_VIM_REG;
                                    registryRobotJourney.startPlaceName = Traffic.DetermineArea(robot.properties.pose.Position, TypeZone.OPZS);
                                    registryRobotJourney.startPoint = robot.properties.pose.Position;
                                    registryRobotJourney.endPoint = destPos.Position;
                                    //robot.ShowText("BUFMAC_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER");
                                }
                            }
                        }

                        break;
                    case BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_B_FROM_VIM_REG:
                        if (TrafficCheckInBuffer(goalFrontLinePos, bayId))
                            break;
                        if (TrafficRountineConstants.DetetectInsideStationCheck(registryRobotJourney))
                        {
                            break;
                        }
                        else
                        {
                            StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_B_FROM_VIM;

                        }
                        break;
                    case BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_B_FROM_VIM:
                        try
                        {
                            TrafficRountineConstants.DetectRelease(registryRobotJourney);
                            if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                            {
                                robot.SwitchToDetectLine(true);
                                resCmd = ResponseCommand.RESPONSE_NONE;
                                StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_SEND_CMD_CAME_FRONTLINE_BUFFER_B_FROM_VIM;
                            }
                        }
                        catch (System.Exception)
                        {
                            errorCode = ErrorCode.CAN_NOT_GET_DATA;
                            CheckUserHandleError(this);
                        }
                        break;
                    case BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_B:
                        try
                        {
                            if (TrafficCheckInBuffer(goalFrontLinePos, bayId))
                                break;
                            if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                            {
                                robot.SwitchToDetectLine(true);

                                resCmd = ResponseCommand.RESPONSE_NONE;
                                //rb.prioritLevel.OnAuthorizedPriorityProcedure = true;
                                StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_SEND_CMD_CAME_FRONTLINE_BUFFER_B_FROM_VIM;

                            }
                        }
                        catch (System.Exception)
                        {
                            errorCode = ErrorCode.CAN_NOT_GET_DATA;
                            CheckUserHandleError(this);
                        }
                        break;
                    case BufferToBuffer.BUFTOBUF_ROBOT_SEND_CMD_CAME_FRONTLINE_BUFFER_B_FROM_VIM:
                        if (rb.SendCmdAreaPallet(BfToBf.GetInfoOfPalletBuffer401_BRB401(PistonPalletCtrl.PISTON_PALLET_DOWN, order.dataRequest_Buffer401)))
                        {
                            StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_DROPDOWN_PALLET_BUFFER_B;
                            robot.ShowText("BUFTOBUF_ROBOT_WAITTING_DROPDOWN_PALLET_BUFFER_B");
                        }
                        break;
                    case BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_DROPDOWN_PALLET_BUFFER_B:
                        if (resCmd == ResponseCommand.RESPONSE_LINEDETECT_PALLETUP)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            BfToBf.UpdatePalletState(PalletStatus.W);
                            //onUpdatedPalletState = true;
                            StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_GOBACK_FRONTLINE_BUFFER_B;
                            //robot.ShowText("FBUFTOBUF_ROBOT_WAITTING_GOBACK_FRONTLINE_BUFFER_B");
                        }
                        else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                        {
                            errorCode = ErrorCode.DETECT_LINE_ERROR;
                            CheckUserHandleError(this);
                        }
                        break;
                    case BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_GOBACK_FRONTLINE_BUFFER_B: // đợi
                        try
                        {
                            if (resCmd == ResponseCommand.RESPONSE_FINISH_GOBACK_FRONTLINE)
                            {
                                robot.bayId = -1;
                                robot.bayIdReg = false;
                                robot.ReleaseWorkingZone();
                                robot.SwitchToDetectLine(false);

                                resCmd = ResponseCommand.RESPONSE_NONE;

                                StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_RELEASED;
                                //robot.ShowText("BUFTOBUF_ROBOT_RELEASED");
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

                    case BufferToBuffer.BUFTOBUF_ROBOT_RELEASED: // trả robot về robotmanagement để nhận quy trình mới
                        robot.bayId = -1;
                        TrafficRountineConstants.ReleaseAll(robot);
                        robot.orderItem = null;
                        robot.SwitchToDetectLine(false);
                        rb.PreProcedureAs = ProcedureControlAssign.PRO_BUFFER_TO_BUFFER;
                        ReleaseProcedureHandler(this);
                        ProRun = false;
                        UpdateInformationInProc(this, ProcessStatus.S);
                        order.status = StatusOrderResponseCode.FINISHED;
                        order.endTimeProcedure = DateTime.Now;
                        order.totalTimeProcedure = order.endTimeProcedure.Subtract(order.startTimeProcedure).TotalMinutes;
                        KillEvent();
                        break;
                    case BufferToBuffer.BUFTOBUF_ROBOT_DESTROY:
                        TrafficRountineConstants.ReleaseAll(robot);
                       order.endTimeProcedure = DateTime.Now;
                        order.totalTimeProcedure = order.endTimeProcedure.Subtract(order.startTimeProcedure).TotalMinutes;
                        robot.SwitchToDetectLine(false);
                        robot.ReleaseWorkingZone();
                        // StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_RELEASED;
                        //robot.prioritLevel.OnAuthorizedPriorityProcedure = false;
                        ProRun = false;
                        UpdateInformationInProc(this, ProcessStatus.F);
                        order.status = StatusOrderResponseCode.ROBOT_ERROR;
                        //reset status pallet Faile H->Ws
                        if (!onUpdatedPalletState)
                        {
                            BfToBf.UpdatePalletState(PalletStatus.F, order.palletId_P, order.planId);
                        }
                        selectHandleError = SelectHandleError.CASE_ERROR_EXIT;
                        procedureStatus = ProcedureStatus.PROC_KILLED;
                        FreeHoldBuffer(order.palletId_H);
                        KillEvent();

                        //this.robot.DestroyRegistrySolvedForm();
                        break;
                    default:
                        break;
                }
                Thread.Sleep(5);
            }
            StateBufferToBuffer = BufferToBuffer.BUFTOBUF_IDLE;
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
