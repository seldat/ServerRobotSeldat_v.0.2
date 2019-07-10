using Newtonsoft.Json.Linq;
using SeldatMRMS;
using SeldatMRMS.Management.RobotManagent;
using SeldatMRMS.Management.TrafficManager;
using SelDatUnilever_Ver1._00.Management.DeviceManagement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static SeldatMRMS.Management.RobotManagent.RobotBaseService;
using static SeldatMRMS.Management.RobotManagent.RobotUnity;
using static SeldatMRMS.Management.RobotManagent.RobotUnityControl;
using static SeldatMRMS.Management.TrafficRobotUnity;
using static SeldatMRMS.ProcedureControlServices;
using static SelDatUnilever_Ver1._00.Management.DeviceManagement.DeviceItem;

namespace SeldatUnilever_Ver1._02.Management.ProcedureServices
{
    public class ProcedureBufferToBuffer : ProcedureControlServices
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
        public ProcedureBufferToBuffer(RobotUnity robot, TrafficManagementService trafficService) : base(robot)
        {
            StateBufferToBuffer = BufferToBuffer.BUFTOBUF_IDLE;
            this.robot = robot;
            // this.points = new DataBufferToBuffer();
            this.Traffic = trafficService;
            procedureCode = ProcedureCode.PROC_CODE_BUFFER_TO_BUFFER;
        }

        public void Start(BufferToBuffer state = BufferToBuffer.BUFTOBUF_ROBOT_GOTO_CHECKIN_BUFFER_A)
        {
            errorCode = ErrorCode.RUN_OK;
            robot.robotTag = RobotStatus.WORKING;
            robot.ProcedureAs = ProcedureControlAssign.PRO_BUFFER_TO_BUFFER;
            StateBufferToBuffer = state;
            Task ProBuferToMachine = new Task(() => this.Procedure(this));
            ProBuferToMachine.Start();
            procedureStatus = ProcedureStatus.PROC_ALIVE;
            ProRun = true;
            ProRunStopW = true;
            //robot.prioritLevel.OnAuthorizedPriorityProcedure = false;
            order.startTimeProcedure = DateTime.Now;

        }
        public void Destroy()
        {
            ProRunStopW = false;
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
            ProcedureBufferToBuffer BfToBf = (ProcedureBufferToBuffer)ojb;
            RobotUnity rb = BfToBf.robot;
            TrafficManagementService Traffic = BfToBf.Traffic;
            robot.ShowText(" Start -> " + procedureCode);
            rb.mcuCtrl.TurnOnLampRb();
            while (ProRun)
            {
                switch (StateBufferToBuffer)
                {
                    case BufferToBuffer.BUFTOBUF_IDLE:
                        robot.ShowText("BUFTOBUF_IDLE");
                        break;
                    case BufferToBuffer.BUFTOBUF_ROBOT_GOTO_CHECKIN_BUFFER_A: // bắt đầu rời khỏi vùng GATE đi đến check in/ đảm bảo check out vùng cổng để robot kế tiếp vào làm việc
                        robot.ShowText("BUFTOBUF_ROBOT_GOTO_CHECKIN_BUFFER_A");
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
                                            if (rb.SendPoseStamped(BfToBf.GetCheckInBuffer_BufferReturn(order.dataRequest_BufferReturn)))
                                            {
                                                StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_GOTO_CHECKIN_BUFFER_A;
                                                robot.ShowText("BUFTOBUF_ROBOT_WAITTING_GOTO_CHECKIN_BUFFER_A");
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
                                if (rb.SendPoseStamped(BfToBf.GetCheckInBuffer_BufferReturn(order.dataRequest_BufferReturn)))
                                {
                                    StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_GOTO_CHECKIN_BUFFER_A;
                                    robot.ShowText("BUFTOBUF_ROBOT_WAITTING_GOTO_CHECKIN_BUFFER_A");
                                }
                            }
                        }
                        catch (System.Exception)
                        {
                            errorCode = ErrorCode.CAN_NOT_GET_DATA;

                            CheckUserHandleError(this);
                        }
                        break;
                    case BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_GOTO_CHECKIN_BUFFER_A: // doi robot di den khu vuc checkin cua vung buffer
                        //if (rb.checkNewPci())
                        //{
                        //    bool onComePoint = robot.ReachedGoal(rb.getPointCheckInConfirm());
                        if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                        //if (onComePoint)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            //rb.prioritLevel.OnAuthorizedPriorityProcedure = true;
                            StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_ZONE_BUFFER_READY_A;
                            robot.ShowText("BUFTOBUF_ROBOT_WAITTING_ZONE_BUFFER_READY_A");
                        }
                        //}
                        break;
                    case BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_ZONE_BUFFER_READY_A: // doi khu vuc buffer san sang de di vao
                        try
                        {
                            if (false == robot.CheckInZoneBehavior(BfToBf.GetAnyPointInBuffer_BufferReturn(order.dataRequest).Position))
                            {

                                //rb.prioritLevel.OnAuthorizedPriorityProcedure = false;
                                if (rb.SendPoseStamped(BfToBf.GetFrontLineBuffer()))
                                {
                                    StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_A;
                                    robot.ShowText("BUFTOBUF_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_A");
                                }
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
                            if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                            //if (onComePoint2)
                            {
                                // 

                                robot.SwitchToDetectLine(true);

                                resCmd = ResponseCommand.RESPONSE_NONE;
                                //rb.prioritLevel.OnAuthorizedPriorityProcedure = true;
                                if (rb.SendCmdAreaPallet(BfToBf.GetInfoOfPalletBuffer_BufferReturn(PistonPalletCtrl.PISTON_PALLET_UP,order.dataRequest_BufferReturn)))
                                {
                                    StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_PICKUP_PALLET_BUFFER_A;
                                    robot.ShowText("BUFTOBUF_ROBOT_WAITTING_PICKUP_PALLET_BUFFER_A");
                                }
                            }
                        }
                        catch (System.Exception)
                        {
                            errorCode = ErrorCode.CAN_NOT_GET_DATA;
                            CheckUserHandleError(this);
                        }
                        break;
                    case BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_PICKUP_PALLET_BUFFER_A:
                        if (resCmd == ResponseCommand.RESPONSE_LINEDETECT_PALLETUP)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            BfToBf.UpdatePalletState(PalletStatus.F);
                            onUpdatedPalletState = true;
                            StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_GOBACK_FRONTLINE_BUFFER_A;
                            robot.ShowText("BUFTOBUF_ROBOT_WAITTING_GOBACK_FRONTLINE_BUFFER_A");
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
                                robot.ReleaseWorkingZone();
                                robot.SwitchToDetectLine(false);

                                resCmd = ResponseCommand.RESPONSE_NONE;
                                //rb.prioritLevel.OnAuthorizedPriorityProcedure = false;
                                if (rb.SendPoseStamped(BfToBf.GetCheckInBuffer_Buffer401(order.dataRequest_Buffer401)))
                                {
                                    StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_GOTO_CHECKIN_BUFFER_B;
                                    robot.ShowText("BUFTOBUF_ROBOT_WAITTING_GOTO_CHECKIN_BUFFER_B");
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

                    case BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_GOTO_CHECKIN_BUFFER_B: // doi robot di den khu vuc checkin cua vung buffer
                        //if (rb.checkNewPci())
                        //{
                        //    bool onComePoint = robot.ReachedGoal(rb.getPointCheckInConfirm());
                        if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                        //if (onComePoint)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            //rb.prioritLevel.OnAuthorizedPriorityProcedure = true;
                            StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_ZONE_BUFFER_READY_B;
                            robot.ShowText("BUFTOBUF_ROBOT_WAITTING_GOTO_CHECKIN_BUFFER_B");
                        }
                        //}
                        break;
                    case BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_ZONE_BUFFER_READY_B: // doi khu vuc buffer san sang de di vao
                        try
                        {
                            if (false == robot.CheckInZoneBehavior(BfToBf.GetAnyPointInBuffer_Buffer401(order.dataRequest_Buffer401).Position))
                            {

                                //rb.prioritLevel.OnAuthorizedPriorityProcedure = false;
                                if (rb.SendPoseStamped(BfToBf.GetFrontLineBuffer_BufferB401(order.dataRequest_Buffer401)))
                                {
                                    StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_B;
                                    robot.ShowText("BUFTOBUF_ROBOT_WAITTING_CAME_FRONTLINE_BUFFER_B");
                                }
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
                            //bool onComePoint2 = robot.ReachedGoal();
                            if (resCmd == ResponseCommand.RESPONSE_LASER_CAME_POINT)
                            //if (onComePoint2)
                            {
                                // 

                                robot.SwitchToDetectLine(true);

                                resCmd = ResponseCommand.RESPONSE_NONE;
                                //rb.prioritLevel.OnAuthorizedPriorityProcedure = true;
                                if (rb.SendCmdAreaPallet(BfToBf.GetInfoOfPalletBuffer_BufferB401(PistonPalletCtrl.PISTON_PALLET_DOWN,order.dataRequest_Buffer401)))
                                {
                                    StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_DROPDOWN_PALLET_BUFFER_B;
                                    robot.ShowText("BUFTOBUF_ROBOT_WAITTING_DROPDOWN_PALLET_BUFFER_B");
                                }
                            }
                        }
                        catch (System.Exception)
                        {
                            errorCode = ErrorCode.CAN_NOT_GET_DATA;
                            CheckUserHandleError(this);
                        }
                        break;
                    case BufferToBuffer.BUFTOBUF_ROBOT_WAITTING_DROPDOWN_PALLET_BUFFER_B:
                        if (resCmd == ResponseCommand.RESPONSE_LINEDETECT_PALLETUP)
                        {
                            resCmd = ResponseCommand.RESPONSE_NONE;
                            BfToBf.UpdatePalletState(PalletStatus.W);
                            //onUpdatedPalletState = true;
                            StateBufferToBuffer = BufferToBuffer.FBUFTOBUF_ROBOT_WAITTING_GOBACK_FRONTLINE_BUFFER_B;
                            robot.ShowText("FBUFTOBUF_ROBOT_WAITTING_GOBACK_FRONTLINE_BUFFER_B");
                        }
                        else if (resCmd == ResponseCommand.RESPONSE_ERROR)
                        {
                            errorCode = ErrorCode.DETECT_LINE_ERROR;
                            CheckUserHandleError(this);
                        }
                        break;
                    case BufferToBuffer.FBUFTOBUF_ROBOT_WAITTING_GOBACK_FRONTLINE_BUFFER_B: // đợi
                        try
                        {
                            if (resCmd == ResponseCommand.RESPONSE_FINISH_GOBACK_FRONTLINE)
                            {
                                robot.ReleaseWorkingZone();
                                robot.SwitchToDetectLine(false);

                                resCmd = ResponseCommand.RESPONSE_NONE;

                                StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_RELEASED;
                                robot.ShowText("BUFTOBUF_ROBOT_RELEASED");
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
                        robot.orderItem = null;
                        robot.SwitchToDetectLine(false);
                        // Release WorkinZone Robot
                        //   robot.robotTag = RobotStatus.IDLE;
                        rb.PreProcedureAs = ProcedureControlAssign.PRO_BUFFER_TO_BUFFER;
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
                    case BufferToBuffer.BUFTOBUF_ROBOT_DESTROY:
                        order.endTimeProcedure = DateTime.Now;
                        order.totalTimeProcedure = order.endTimeProcedure.Subtract(order.startTimeProcedure).TotalMinutes;
                        SaveOrderItem(order);
                        robot.SwitchToDetectLine(false);
                        robot.ReleaseWorkingZone();
                        // StateBufferToBuffer = BufferToBuffer.BUFTOBUF_ROBOT_RELEASED;
                        //robot.prioritLevel.OnAuthorizedPriorityProcedure = false;
                        ProRun = false;
                        UpdateInformationInProc(this, ProcessStatus.F);
                        order.status = StatusOrderResponseCode.ROBOT_ERROR;
                        //reset status pallet Faile H->Ws
                        if (!onUpdatedPalletState)
                            UpdatePalletState(PalletStatus.W);
                        selectHandleError = SelectHandleError.CASE_ERROR_EXIT;
                        procedureStatus = ProcedureStatus.PROC_KILLED;
                        FreeHoldBuffer();
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
