﻿using Newtonsoft.Json.Linq;
using SeldatMRMS;
using SeldatUnilever_Ver1._02;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using static DoorControllerService.DoorService;
using static SeldatMRMS.Management.RobotManagent.RobotUnityControl;
using static SelDatUnilever_Ver1.CollectionDataService;

namespace SelDatUnilever_Ver1._00.Management.DeviceManagement
{
    public class DeviceItem : NotifyUIBase
    {
        public enum StatusOrderResponseCode
        {
            ORDER_STATUS_RESPONSE_SUCCESS = 200,
            ORDER_STATUS_RESPONSE_ERROR_DATA = 201,
            ORDER_STATUS_RESPONSE_NOACCEPTED = 202,
            ORDER_STATUS_DOOR_BUSY = 203,
            PENDING = 300,
            DELIVERING = 301,
            FINISHED = 302,
            ROBOT_ERROR = 303,
            NO_BUFFER_DATA = 304,
            CHANGED_FORKLIFT = 305,
            DESTROYED = 306,
            GOING_AND_PICKING_UP = 307,
            PICKED_UP = 308,
            ERROR_GET_FRONTLINE = 309,
            ERROR_GET_PALLETID = 310,
        }

        public class StatusOrderResponse
        {
            public int status;
            public String content;
        }
        public enum PalletCtrl
        {
            Pallet_CTRL_DOWN = 0,
            Pallet_CTRL_UP = 1

        }
        public class DataPallet
        {
            public int row;
            public int bay;
            public int directMain;
            public int directSub;
            public int directOut;
            public int line_ord;
            public PalletCtrl palletCtrl;
            public Pose linePos;
        }
        public enum TyeRequest
        {
            TYPEREQUEST_FORLIFT_TO_BUFFER = 1,
            TYPEREQUEST_BUFFER_TO_MACHINE = 2,
            TYPEREQUEST_BUFFER_TO_RETURN = 3,
            TYPEREQUEST_PALLET_EMPTY_MACHINE_TO_RETURN = 4,
            TYPEREQUEST_RETURN_TO_GATE = 5,
            TYPEREQUEST_CLEAR = 6,
            TYPEREQUEST_OPEN_FRONTDOOR_DELIVERY_PALLET_GATE_1 = 7,
            TYPEREQUEST_CLOSE_FRONTDOOR_DELIVERY_PALLET_GATE_1 = 8,
            TYPEREQUEST_OPEN_FRONTDOOR_RETURN_PALLET = 9,
            TYPEREQUEST_CLOSE_FRONTDOOR_RETURN_PALLET = 10,
            TYPEREQUEST_CLEAR_FORLIFT_TO_BUFFER = 11,
            TYPEREQUEST_FORLIFT_TO_MACHINE = 12, // forlift to machine
            TYPEREQUEST_MACHINE_TO_BUFFERRETURN = 13, // machine to bufferreturn
            TYPEREQUEST_CHARGE = 14, // santao jujeng cap bottle
            TYPEREQUEST_GOTO_READY = 15, // goto ready
            TYPEREQUEST_OPEN_FRONTDOOR_DELIVERY_PALLET_GATE_2 = 16,
            TYPEREQUEST_CLOSE_FRONTDOOR_DELIVERY_PALLET_GATE_2 = 17,
            TYPEREQUEST_WMS_RETURN_PALLET_BUFFER_TO_GATE = 18, // 
            TYPEREQUEST_WMS_RETURN_PALLET_BUFFERRETURN_TO_BUFFER401 = 19, // 
        }
        public enum TabletConTrol
        {
            TABLET_MACHINE = 10000,
            TABLET_FORKLIFT = 10001
        }
        public enum CommandRequest
        {
            CMD_DATA_ORDER_BUFFERTOMACHINE = 100,
            CMD_DATA_ORDER_RETURN = 100,
            CMD_DATA_ORDER_FORKLIFT = 101,
            CMD_DATA_STATE = 102
        }
        public class OrderItem
        {
            public OrderItem() { }
            public String userName { get; set; }
            public String robot { get; set; }
            public TyeRequest typeReq { get; set; } // FL: ForkLift// BM: BUFFER MACHINE // PR: Pallet return
            public Point frontLinePos;
            public StatusOrderResponseCode status { get; set; }
            private String OrderId;
            public int planId;
            public int deviceId;
            public int deviceIdPut;
            public String productDetailName { get; set; }
            public int productId;
            public int gate { get; set; }
            public int palletBay;
            public int palletRow;

            public int palletId_H;
            public int palletId_P;
            public int palletId_F;
            public int productDetailId;


            public String activeDate;
            public String dateTime;

            public int timeWorkId;
            public String palletStatus;
            public int palletId;
            public int updUsrId;
            public int lengthPallet;
            public String dataRequest;
            public String dataRequest_BufferReturn;
            public String dataRequest_Buffer401;
            // public bool status = false; // chua hoan thanh
            public DataPallet palletAtMachine;

            public int bufferId;
            public int bayId;
            public int bufferIdPut;
            public int palletAmount;

            public DateTime startTimeProcedure = new DateTime();
            public DateTime endTimeProcedure = new DateTime();
            public double totalTimeProcedure { get; set; }
            public bool onAssiged = false;


        }
        public string userName { get; set; } // dia chi Emei
        public string codeID;
        public List<OrderItem> PendingOrderList;
        public List<OrderItem> OrderedItemList;
        public int orderedAmount = 0;
        public int doneAmount = 0;
        public MainWindow mainWindow;

        public DeviceItem()
        {
            PendingOrderList = new List<OrderItem>();
            OrderedItemList = new List<OrderItem>();
        }
        public DeviceItem(MainWindow mainWindow)
        {
            PendingOrderList = new List<OrderItem>();
            OrderedItemList = new List<OrderItem>();
            this.mainWindow = mainWindow;
        }
        public void state(CommandRequest pCommandRequest, String data)
        {
            switch (pCommandRequest)
            {
                case CommandRequest.CMD_DATA_ORDER_BUFFERTOMACHINE:
                    break;
                case CommandRequest.CMD_DATA_ORDER_FORKLIFT:
                    break;
                case CommandRequest.CMD_DATA_STATE:
                    break;
            }
        }
        public void StatusOrderItem(OrderItem item, StatusOrderResponse statusOrderResponse)
        {

        }
        public void RemoveFirstOrder()
        {
            if (PendingOrderList.Count > 0)
            {
                PendingOrderList.RemoveAt(0);
            }
        }
        public void RemoveOrder(OrderItem order)
        {
            if(PendingOrderList.Contains(order))
                PendingOrderList.Remove(order);

        }
        public void AddOrder(OrderItem hasOrder)
        {
            try
            {
                PendingOrderList.Add(hasOrder);
                OrderedItemList.Add(hasOrder);
            }
            catch { }
        }
        public void AddOrderCreatePlan(OrderItem order)
        {
            try
            {

                if (Convert.ToInt32(CreatePlanBuffer(order)) > 0)
                {
                    PendingOrderList.Add(order);
                    OrderedItemList.Add(order);
                }
            }
            catch { }
        }
        public OrderItem GetOrder()
        {
            if (PendingOrderList.Count > 0)
            {
                return PendingOrderList[0];
            }
            return null;
        }
        public void ClearOrderList()
        {
            if (PendingOrderList.Count > 0)
            {
                PendingOrderList.Clear();
            }
        }
        public void rounter(String data)
        {

        }
        public StatusOrderResponse ParseData(String dataReq)
        {
            StatusOrderResponse statusOrderResponse = null;
            try
            {
                JObject results = JObject.Parse(dataReq);
                int typeReq = (int)results["typeReq"];
                #region TYPEREQUEST_FORLIFT_TO_BUFFER
                if (typeReq == (int)TyeRequest.TYPEREQUEST_FORLIFT_TO_BUFFER)
                {
                    int gate = (int)results["gate"];
                    if (Global_Object.getGateStatus(gate))
                    {
                        statusOrderResponse = new StatusOrderResponse() { status = (int)StatusOrderResponseCode.ORDER_STATUS_DOOR_BUSY, content = "" };
                        return statusOrderResponse;
                    }
                    OrderItem order = new OrderItem();
                    order.typeReq = (TyeRequest)typeReq;
                    order.userName = (String)results["userName"];
                    order.productDetailId = (int)results["productDetailId"];
                    order.productDetailName = (String)results["productDetailName"];
                    order.productId = (int)results["productId"];
                    order.planId = (int)results["planId"];
                    order.deviceId = (int)results["deviceId"];
                    order.timeWorkId = (int)results["timeWorkId"];
                    order.activeDate = (string)results["activeDate"];

                    order.gate = (int)results["gate"];
                    // order.palletStatus = (String)results["palletStatus"];
                    dynamic product = new JObject();
                    product.timeWorkId = order.timeWorkId;
                    product.activeDate = order.activeDate;
                    product.productId = order.productId;
                    product.productDetailId = order.productDetailId;

                    // chu y sua 
                    product.palletStatus = PalletStatus.P.ToString();
                    order.dataRequest = product.ToString();
                    order.status = StatusOrderResponseCode.PENDING;
                    order.dateTime = (string)DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss tt");
                
                    int palletId_P = Convert.ToInt32(CreatePlanBuffer(order));
                
                   if (palletId_P > 0)
                   {
                        /*if (GetFrontLineBuffer(order, true) == null)
                        {
                            statusOrderResponse = new StatusOrderResponse() { status = (int)StatusOrderResponseCode.ORDER_STATUS_RESPONSE_NOACCEPTED, content = "Loai Ma Code trong Buffer" };
                            return statusOrderResponse;
                        }*/
                        //  Global_Object.onFlagDoorBusy = true;
                        Global_Object.setGateStatus(gate, true);
                        order.palletId_P = palletId_P;
                        PendingOrderList.Add(order);
                        OrderedItemList.Add(order);
                        Global_Object.cntForkLiftToBuffer++;
                    }
                    else
                    {
                        statusOrderResponse = new StatusOrderResponse() { status = (int)StatusOrderResponseCode.ORDER_STATUS_RESPONSE_NOACCEPTED, content = "Buffer Đầy ,Vui Lòng Kiểm Tra Lai !" };
                        return statusOrderResponse;
                    }
                    try
                    {
                        if (gate == (int)DoorId.DOOR_MEZZAMINE_UP)
                            Global_Object.doorManagementServiceCtrl.DoorMezzamineUp.LampSetStateOn(DoorType.DOOR_FRONT);
                        if (gate == (int)DoorId.DOOR_MEZZAMINE_UP_NEW)
                            Global_Object.doorManagementServiceCtrl.DoorMezzamineUpNew.LampSetStateOn(DoorType.DOOR_FRONT);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("control lamp failed" + e);
                    }
                }
                #endregion
                #region TYPEREQUEST_FORLIFT_TO_MACHINE
                if (typeReq == (int)TyeRequest.TYPEREQUEST_FORLIFT_TO_MACHINE)
                {
                    int gate = (int)results["gate"];
                    if (Global_Object.getGateStatus(gate))
                    {
                        statusOrderResponse = new StatusOrderResponse() { status = (int)StatusOrderResponseCode.ORDER_STATUS_DOOR_BUSY, content = "" };
                        return statusOrderResponse;
                    }
                    Global_Object.setGateStatus(gate, true);
                    //Global_Object.onFlagDoorBusy = true;
                    OrderItem order = new OrderItem();
                    order.typeReq = (TyeRequest)typeReq;
                    order.userName = (String)results["userName"];
                    order.productDetailId = (int)results["productDetailId"];
                    order.productDetailName = (String)results["productDetailName"];
                    order.productId = (int)results["productId"];
                    order.planId = (int)results["planId"];
                    order.deviceId = (int)results["deviceId"];
                    order.timeWorkId = (int)results["timeWorkId"];
                    order.activeDate = (string)results["activeDate"];
                    order.gate = (int)results["gate"];
                    // order.palletStatus = (String)results["palletStatus"];
                    dynamic product = new JObject();
                    product.timeWorkId = order.timeWorkId;
                    product.activeDate = order.activeDate;
                    product.productId = order.productId;
                    product.productDetailId = order.productDetailId;

                    // chu y sua 
                    product.palletStatus = PalletStatus.P.ToString();
                    order.dataRequest = product.ToString();
                    order.status = StatusOrderResponseCode.PENDING;
                    order.dateTime = (string)DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss tt");
                    DataPallet datapallet = GetLineMachineInfo(order.deviceId);
                    if (datapallet != null)
                    {
                        order.palletAtMachine = datapallet;
                        PendingOrderList.Add(order);
                        OrderedItemList.Add(order);
                    }
                    else
                    {
                        statusOrderResponse = new StatusOrderResponse() { status = (int)StatusOrderResponseCode.ORDER_STATUS_RESPONSE_NOACCEPTED, content = "" };
                        return statusOrderResponse;
                    }
                    try
                    {
                        if (gate == (int)DoorId.DOOR_MEZZAMINE_UP)
                            Global_Object.doorManagementServiceCtrl.DoorMezzamineUp.LampSetStateOn(DoorType.DOOR_FRONT);
                        if (gate == (int)DoorId.DOOR_MEZZAMINE_UP_NEW)
                            Global_Object.doorManagementServiceCtrl.DoorMezzamineUpNew.LampSetStateOn(DoorType.DOOR_FRONT);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("control lamp failed");
                    }
                }
                #endregion
                #region TYPEREQUEST_BUFFER_TO_MACHINE
                else if (typeReq == (int)TyeRequest.TYPEREQUEST_BUFFER_TO_MACHINE)
                {
                    Console.WriteLine(dataReq);
                    Console.WriteLine("-----------------------------");
                    int len = (int)results["length"];
                    int palletAmountInBuffer = (int)results["palletAmount"];
                    int productDetailId = (int)results["productDetailId"];
                    int cntOrderReg = 0;
                    int orderAmount = 0;
                    foreach (OrderItem ord in PendingOrderList)
                    {
                        if (productDetailId == ord.productDetailId)
                        {
                            if (ord.status == StatusOrderResponseCode.PENDING)
                                cntOrderReg++;
                        }
                    }
                    if (cntOrderReg == 0) // chưa có order với productdetailID nào uoc đăng ký. thêm vào đúng số lượng trong orderlist
                    {
                        if (len <= palletAmountInBuffer) // nếu số lượn yêu cầu nhỏ hơn bằng số pallet có trong buffer add vào orderlist 
                            orderAmount = len;
                    }
                    else if (cntOrderReg >= palletAmountInBuffer) // số lượng yêu cầu trước đó bằng hoặc hơn số lượng yêu cầu hiện tại. không duoc phép đưa vào thêm
                    {
                        statusOrderResponse = new StatusOrderResponse() { status = (int)StatusOrderResponseCode.ORDER_STATUS_RESPONSE_NOACCEPTED, content = "" };
                        return statusOrderResponse;
                    }
                    else if (cntOrderReg < palletAmountInBuffer) // số lượng yêu cầu hiện tại nhỏ hơn thì phải tính lại số lượng để bổ sung vào thêm
                    {
                        int availableOrder = palletAmountInBuffer - cntOrderReg; // tính số lượng pallet có thể 
                        int willOrder = availableOrder - len; // số lượng pallet sẽ duoc add thêm vào orederlist
                        if (willOrder >= 0)
                        {
                            orderAmount = len;
                        }
                        else
                        {
                            orderAmount = len - availableOrder;
                        }

                    }
                    for (int i = 0; i < orderAmount; i++)
                    {

                        OrderItem order = new OrderItem();
                        order.typeReq = (TyeRequest)typeReq;
                        order.userName = (String)results["userName"];
                        order.productDetailId = (int)results["productDetailId"];
                        order.productDetailName = (String)results["productDetailName"];
                        order.productId = (int)results["productId"];
                        // order.planId = (int)results["planId"];
                        order.deviceId = (int)results["deviceId"];
                        order.timeWorkId = 1;
                        // order.activeDate = (string)DateTime.Now.ToString("yyyy-MM-dd");
                        // order.palletStatus = (String)results["palletStatus"];
                        String jsonDPst = (string)results["datapallet"][i];
                        JObject stuffPallet = JObject.Parse(jsonDPst);
                        double xx = (double)stuffPallet["line"]["x"];
                        double yy = (double)stuffPallet["line"]["y"];
                        double angle = (double)stuffPallet["line"]["angle"];
                        int row = (int)stuffPallet["pallet"]["row"];
                        int bay = (int)stuffPallet["pallet"]["bay"];
                        int directMain = (int)stuffPallet["pallet"]["dir_main"];
                        int directSub = (int)stuffPallet["pallet"]["dir_sub"];
                        int directOut = (int)stuffPallet["pallet"]["dir_out"];
                        int line_ord = (int)stuffPallet["pallet"]["line_ord"];
                        order.palletAtMachine = new DataPallet() { linePos = new Pose(xx, yy, angle), row = row, bay = bay, directMain = directMain, directSub = directSub, directOut = directOut, line_ord = line_ord };
                        dynamic product = new JObject();
                        product.timeWorkId = order.timeWorkId;
                        //product.activeDate = "";
                        order.dateTime = (string)DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss tt");
                        product.productId = order.productId;
                        product.productDetailId = order.productDetailId;
                        // chu y sua 
                        product.palletStatus = PalletStatus.W.ToString(); // W
                        order.dataRequest = product.ToString();

                        order.status = StatusOrderResponseCode.PENDING;
                        PendingOrderList.Add(order);
                        OrderedItemList.Add(order);
                        Global_Object.cntBufferToMachine++;
                    }

                }
                #endregion
                #region TYPEREQUEST_MACHINE_TO_RETURN
                else if (typeReq == (int)TyeRequest.TYPEREQUEST_PALLET_EMPTY_MACHINE_TO_RETURN)
                {
                    int len = (int)results["length"];

                    for (int i = 0; i < len; i++)
                    {
                        OrderItem order = new OrderItem();
                        order.typeReq = (TyeRequest)typeReq;
                        order.userName = (String)results["userName"];
                        order.activeDate = (string)results["activeDate"];
                        order.deviceId = (int)results["deviceId"];
                        //order.palletStatus = (String)results["palletStatus"];
                        String jsonDPst = (string)results["datapallet"][i];
                        JObject stuffPallet = JObject.Parse(jsonDPst);
                        double xx = (double)stuffPallet["line"]["x"];
                        double yy = (double)stuffPallet["line"]["y"];
                        double angle = (double)stuffPallet["line"]["angle"];
                        int row = (int)stuffPallet["pallet"]["row"];
                        int bay = (int)stuffPallet["pallet"]["bay"];
                        int directMain = (int)stuffPallet["pallet"]["dir_main"];
                        int directSub = (int)stuffPallet["pallet"]["dir_sub"];
                        int dir_out = (int)stuffPallet["pallet"]["dir_out"];
                        int line_ord = (int)stuffPallet["pallet"]["line_ord"];
                        order.dateTime = (string)DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss tt");
                        order.palletAtMachine = new DataPallet() { linePos = new Pose(xx, yy, angle), row = row, bay = bay, directMain = directMain, directSub = directSub, directOut = dir_out, line_ord = line_ord };
                        dynamic product = new JObject();
                        product.palletStatus = PalletStatus.F.ToString();
                        order.dataRequest = product.ToString();
                        order.status = StatusOrderResponseCode.PENDING;
                        PendingOrderList.Add(order);
                        OrderedItemList.Add(order);

                    }
                }
                #endregion
                #region TYPEREQUEST_BUFFER_TO_GATE
                else if (typeReq == (int)TyeRequest.TYPEREQUEST_WMS_RETURN_PALLET_BUFFER_TO_GATE)
                {
                    OrderItem order = new OrderItem();
                    order.gate = (int)DoorId.DOOR_MEZZAMINE_RETURN;
                    order.typeReq = (TyeRequest)typeReq;
                    order.userName = (String)results["userName"];
                    order.deviceId = (int)results["deviceId"];
                    order.productDetailId = (int)results["productDetailId"];
                    order.productId = (int)results["productId"];
                    order.timeWorkId = 1;
                    order.activeDate = (string)results["activeDate"];
                    order.planId = (int)results["planId"];
                    order.palletId = (int)results["palletId"];
                    //order.activeDate = (string)results["activeDate"];
                    // order.palletStatus = (String)results["palletStatus"];
                    dynamic product = new JObject();
                    product.timeWorkId = order.timeWorkId;
                    product.activeDate = order.activeDate;
                    product.productId = order.productId;
                    product.productDetailId = order.productDetailId;
                    order.dateTime = (string)DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss tt");
                    // chu y sua 
                    product.palletStatus = PalletStatus.W.ToString();
                    order.dataRequest = product.ToString();
                    order.status = StatusOrderResponseCode.PENDING;
                    if (UpdatePalletStatusReturnBufferToGate(order))
                    {
                        PendingOrderList.Add(order);
                        OrderedItemList.Add(order);
                    }
                    else
                    {
                        statusOrderResponse = new StatusOrderResponse() { status = (int)StatusOrderResponseCode.ORDER_STATUS_RESPONSE_NOACCEPTED, content = "" };
                        return statusOrderResponse;
                    }
                }
                #endregion
                #region TYPEREQUEST_WMS_RETURN_PALLET_BUFFERRETURN
                else if (typeReq == (int)TyeRequest.TYPEREQUEST_MACHINE_TO_BUFFERRETURN)
                {
                    int len = (int)results["length"];
                    for (int i = 0; i < len; i++)
                    {
                        // tạo plan vùng buffer return
                        OrderItem order = new OrderItem();
                        order.typeReq = (TyeRequest)typeReq;
                        order.userName = (String)results["userName"];
                        order.productDetailId = (int)results["productDetailId"];
                        order.productDetailName = (String)results["productDetailName"];
                        order.activeDate = (string)results["activeDate"];
                        order.productId = (int)results["productId"];
                        // order.planId = (int)results["planId"];
                        int deviceId = getDeviceId("RETURN_MAIN 0");
                        if (deviceId < 0)
                        {
                            statusOrderResponse = new StatusOrderResponse() { status = (int)StatusOrderResponseCode.ORDER_STATUS_RESPONSE_NOACCEPTED, content = "" };
                            return statusOrderResponse;
                        }
                        order.deviceId = deviceId;  // Buffer Return
                        order.timeWorkId = 1;
                        String jsonDPst = (string)results["datapallet"][i];
                        JObject stuffPallet = JObject.Parse(jsonDPst);
                        double xx = (double)stuffPallet["line"]["x"];
                        double yy = (double)stuffPallet["line"]["y"];
                        double angle = (double)stuffPallet["line"]["angle"];
                        int row = (int)stuffPallet["pallet"]["row"];
                        int bay = (int)stuffPallet["pallet"]["bay"];
                        int directMain = (int)stuffPallet["pallet"]["dir_main"];
                        int directSub = (int)stuffPallet["pallet"]["dir_sub"];
                        int dir_out = (int)stuffPallet["pallet"]["dir_out"];
                        int line_ord = (int)stuffPallet["pallet"]["line_ord"];
                        order.dateTime = (string)DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss tt");
                        order.palletAtMachine = new DataPallet() { linePos = new Pose(xx, yy, angle), row = row, bay = bay, directMain = directMain, directSub = directSub, directOut = dir_out, line_ord = line_ord };
                        dynamic product = new JObject();
                        product.timeWorkId = order.timeWorkId;
                        product.activeDate = order.activeDate;
                        order.dateTime = (string)DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss tt");
                        product.productId = order.productId;
                        product.productDetailId = order.productDetailId;
                        // chu y sua 
                        product.palletStatus = PalletStatus.P.ToString();
                        order.dataRequest = product.ToString();
                        order.status = StatusOrderResponseCode.PENDING;
                        int palletId_P = Convert.ToInt32(CreatePlanBuffer(order));
                        if (palletId_P > 0)
                        {
                            order.palletId_P = palletId_P;
                            PendingOrderList.Add(order);
                            OrderedItemList.Add(order);
                        }
                        else
                        {
                            statusOrderResponse = new StatusOrderResponse() { status = (int)StatusOrderResponseCode.ORDER_STATUS_RESPONSE_NOACCEPTED, content = "" };
                            return statusOrderResponse;
                        }
                    }

                }
                #endregion
                #region TYPEREQUEST_WMS_RETURN_PALLET_BUFFERRETURN_TO_BUFFER401
                else if (typeReq == (int)TyeRequest.TYPEREQUEST_WMS_RETURN_PALLET_BUFFERRETURN_TO_BUFFER401)
                {
                    OrderItem order = new OrderItem();
                    order.typeReq = (TyeRequest)typeReq;
                    order.userName = (String)results["userName"];
                    order.productDetailId = (int)results["productDetailId"];
                    order.productDetailName = (String)results["productDetailName"];
                    order.productId = (int)results["productId"];
                    order.planId = (int)results["planId"];
                    order.deviceId = (int)results["deviceId"];
                    order.bufferId = (int)results["bufferId"];
                    order.deviceIdPut = (int)results["deviceIdPut"];
                    order.bufferIdPut = (int)results["bufferIdPut"];
                    order.timeWorkId = 1;
                    order.activeDate = (string)results["activeDate"];
                    order.palletId = (int)results["palletId"];
                    // order.activeDate = (string)DateTime.Now.ToString("yyyy-MM-dd");
                    // order.palletStatus = (String)results["palletStatus"];
                    order.status = StatusOrderResponseCode.PENDING;

                    PlanDataRequest planDataRequest_B401 = new PlanDataRequest();
                    planDataRequest_B401.activeDate = order.activeDate;
                    planDataRequest_B401.deviceId = order.deviceIdPut;
                    planDataRequest_B401.productDetailId = order.productDetailId;
                    planDataRequest_B401.productId = order.productId;


                    UpdatePalletRequest updatePalletRequest_BufferReturn = new UpdatePalletRequest();
                    updatePalletRequest_BufferReturn.planId = order.planId;
                    updatePalletRequest_BufferReturn.palletStatus = "H";
                    updatePalletRequest_BufferReturn.palletId = order.palletId;

                    bool onUpdateBR = UpdatePalletStatusToHoldBufferReturn_BRB401(updatePalletRequest_BufferReturn);
                    int palletId_P = CreatePlanBuffer401(planDataRequest_B401);

                    dynamic product_B401 = new JObject();
                    product_B401.timeWorkId = order.timeWorkId;
                    product_B401.activeDate = order.activeDate;
                    order.dateTime = (string)DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss tt");
                    product_B401.productId = order.productId;
                    product_B401.productDetailId = order.productDetailId;
                    // chu y sua 
                    product_B401.palletStatus = PalletStatus.P.ToString();
                    order.dataRequest_Buffer401 = product_B401.ToString();

                    dynamic product_BR = new JObject();
                    product_BR.timeWorkId = order.timeWorkId;
                    product_BR.activeDate = order.activeDate;
                    order.dateTime = (string)DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss tt");
                    product_BR.productId = order.productId;
                    product_BR.productDetailId = order.productDetailId;
                    // chu y sua 
                    product_BR.palletStatus = PalletStatus.H.ToString();
                    order.dataRequest_BufferReturn = product_BR.ToString();

                    if (onUpdateBR && palletId_P > 0)
                    {
                        order.palletId_P = palletId_P;
                        PendingOrderList.Add(order);
                        OrderedItemList.Add(order);
                    }
                    else
                    {
                        FreePlanedBuffer(order.dataRequest_Buffer401, order.planId);
                        updatePalletRequest_BufferReturn.palletStatus = "W";
                        UpdatePalletState(updatePalletRequest_BufferReturn);
                    }



                }
                #endregion
                #region TYPEREQUEST_CLEAR
                else if (typeReq == (int)TyeRequest.TYPEREQUEST_CLEAR)
                {
                    String userName = (String)results["userName"];
                    // kiểm tra quy trình và hủy task 

                    foreach (OrderItem ord in PendingOrderList)
                    {
                        if (ord.userName.Equals(userName))
                            PendingOrderList.Remove(ord);
                    }
                    foreach (OrderItem ord in OrderedItemList)
                    {
                        if (ord.userName.Equals(userName))
                            if (ord.status == StatusOrderResponseCode.PENDING)
                            {
                                ord.status = StatusOrderResponseCode.DESTROYED;
                            }
                    }

                }
                #endregion
                #region TYPEREQUEST_OPEN_FRONTDOOR_DELIVERY_PALLET_GATE_1
                else if (typeReq == (int)TyeRequest.TYPEREQUEST_OPEN_FRONTDOOR_DELIVERY_PALLET_GATE_1)
                {
                    // same deviceID forklift
                    try
                    {
                        if (false == Global_Object.doorManagementServiceCtrl.DoorMezzamineUpNew.getDoorBusy()) {
                            Global_Object.doorManagementServiceCtrl.DoorMezzamineUpNew.openDoor(DoorType.DOOR_FRONT);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("control door failed");
                        statusOrderResponse = new StatusOrderResponse() { status = (int)StatusOrderResponseCode.ORDER_STATUS_RESPONSE_ERROR_DATA, content = e.Message };
                        return statusOrderResponse;
                    }
                }
                #endregion
                #region TYPEREQUEST_OPEN_FRONTDOOR_DELIVERY_PALLET_GATE_2
                else if (typeReq == (int)TyeRequest.TYPEREQUEST_OPEN_FRONTDOOR_DELIVERY_PALLET_GATE_2)
                {
                    // same deviceID forklift
                    try
                    {   
                        if(false == Global_Object.doorManagementServiceCtrl.DoorMezzamineUp.getDoorBusy())
                        {
                            Global_Object.doorManagementServiceCtrl.DoorMezzamineUp.openDoor(DoorType.DOOR_FRONT);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("control door failed");
                        statusOrderResponse = new StatusOrderResponse() { status = (int)StatusOrderResponseCode.ORDER_STATUS_RESPONSE_ERROR_DATA, content = e.Message };
                        return statusOrderResponse;
                    }
                }
                #endregion
                #region TYPEREQUEST_CLOSE_FRONTDOOR_DELIVERY_PALLET_GATE_1
                else if (typeReq == (int)TyeRequest.TYPEREQUEST_CLOSE_FRONTDOOR_DELIVERY_PALLET_GATE_1)
                {
                    // same deviceID forklift
                    try
                    {
                        Global_Object.doorManagementServiceCtrl.DoorMezzamineUpNew.closeDoor(DoorType.DOOR_FRONT);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("control door failed");
                        statusOrderResponse = new StatusOrderResponse() { status = (int)StatusOrderResponseCode.ORDER_STATUS_RESPONSE_ERROR_DATA, content = e.Message };
                        return statusOrderResponse;
                    }
                }
                #endregion
                #region TYPEREQUEST_CLOSE_FRONTDOOR_DELIVERY_PALLET_GATE_2
                else if (typeReq == (int)TyeRequest.TYPEREQUEST_CLOSE_FRONTDOOR_DELIVERY_PALLET_GATE_2)
                {
                    // same deviceID forklift
                    try
                    {
                        Global_Object.doorManagementServiceCtrl.DoorMezzamineUp.closeDoor(DoorType.DOOR_FRONT);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("control door failed");
                        statusOrderResponse = new StatusOrderResponse() { status = (int)StatusOrderResponseCode.ORDER_STATUS_RESPONSE_ERROR_DATA, content = e.Message };
                        return statusOrderResponse;
                    }
                }
                #endregion
                #region TYPEREQUEST_OPEN_FRONTDOOR_RETURN_PALLET
                else if (typeReq == (int)TyeRequest.TYPEREQUEST_OPEN_FRONTDOOR_RETURN_PALLET)
                {
                    // same deviceID forklift
                    try
                    {
                        if(false == Global_Object.doorManagementServiceCtrl.DoorMezzamineReturn.getDoorBusy())
                        {
                            Global_Object.doorManagementServiceCtrl.DoorMezzamineReturn.openDoor(DoorType.DOOR_FRONT);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("control door failed");
                        statusOrderResponse = new StatusOrderResponse() { status = (int)StatusOrderResponseCode.ORDER_STATUS_RESPONSE_ERROR_DATA, content = e.Message };
                        return statusOrderResponse;
                    }
                }
                #endregion
                #region TYPEREQUEST_CLOSE_FRONTDOOR_RETURN_PALLET
                else if (typeReq == (int)TyeRequest.TYPEREQUEST_CLOSE_FRONTDOOR_RETURN_PALLET)
                {
                    // same deviceID forklift
                    try
                    {
                        Global_Object.doorManagementServiceCtrl.DoorMezzamineReturn.closeDoor(DoorType.DOOR_FRONT);
                        Global_Object.doorManagementServiceCtrl.DoorMezzamineReturn.LampSetStateOff(DoorType.DOOR_BACK);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("control door failed");
                        statusOrderResponse = new StatusOrderResponse() { status = (int)StatusOrderResponseCode.ORDER_STATUS_RESPONSE_ERROR_DATA, content = e.Message };
                        return statusOrderResponse;
                    }
                }
                #endregion
                statusOrderResponse = new StatusOrderResponse() { status = (int)StatusOrderResponseCode.ORDER_STATUS_RESPONSE_SUCCESS, content = "" };
            }
            catch (Exception e)
            {
                statusOrderResponse = new StatusOrderResponse() { status = (int)StatusOrderResponseCode.ORDER_STATUS_RESPONSE_ERROR_DATA, content = e.Message };
                return statusOrderResponse;
            }
            try
            {
                String path = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "RecoderDataOrder.txt");
                if (!File.Exists(path))
                    File.Create(path);
                if (File.ReadAllBytes(path).Length > 3000000) // lon 3M thi xoa bot
                {
                    String[] lines = File.ReadAllLines(path);
                    Array.Clear(lines, 0, 615); // xoa bot text 615 vong tuong ung 1M
                }
                File.AppendAllText(path, DateTime.Now.ToString("yyyyMMdd HH:mm:ss tt >> ") + dataReq + Environment.NewLine + "[Response] >> " + (statusOrderResponse.status) + Environment.NewLine);
            }
            catch { }
            return statusOrderResponse;
        }


        public void RemoveCallBack(OrderItem item)
        {
            try
            {
                if (item.typeReq == TyeRequest.TYPEREQUEST_FORLIFT_TO_BUFFER)
                {
                    if (item.status == StatusOrderResponseCode.PENDING)
                    {
                        PendingOrderList.Remove(item);
                        OrderedItemList.Remove(item);
                        UpdatePalletState(item.palletId_P, item.planId, PalletStatus.F);
                    }
                }
                else if (item.typeReq == TyeRequest.TYPEREQUEST_BUFFER_TO_MACHINE)
                {
                    if (item.status == StatusOrderResponseCode.PENDING)
                    {
                        PendingOrderList.Remove(item);
                        OrderedItemList.Remove(item);
                        UpdatePalletState(item.palletId_H, item.planId, PalletStatus.W);
                    }
                }
                else if (item.typeReq == TyeRequest.TYPEREQUEST_PALLET_EMPTY_MACHINE_TO_RETURN)
                {
                    if (item.status == StatusOrderResponseCode.PENDING)
                    {
                        PendingOrderList.Remove(item);
                        OrderedItemList.Remove(item);
                        UpdatePalletState(item.palletId_P, item.planId, PalletStatus.F);
                    }
                }
                else if (item.typeReq == TyeRequest.TYPEREQUEST_WMS_RETURN_PALLET_BUFFERRETURN_TO_BUFFER401)
                {
                    if (item.status == StatusOrderResponseCode.PENDING)
                    {
                        PendingOrderList.Remove(item);
                        OrderedItemList.Remove(item);
                        UpdatePalletState(item.palletId_P, item.planId, PalletStatus.F);
                        UpdatePalletState(item.palletId_H, item.planId, PalletStatus.W);
                    }
                }
                else if (item.typeReq == TyeRequest.TYPEREQUEST_WMS_RETURN_PALLET_BUFFER_TO_GATE)
                {
                    if (item.status == StatusOrderResponseCode.PENDING)
                    {
                        PendingOrderList.Remove(item);
                        OrderedItemList.Remove(item);
                        UpdatePalletState(item.palletId_H, item.planId, PalletStatus.W);
                    }
                }
                else if (item.typeReq == TyeRequest.TYPEREQUEST_MACHINE_TO_BUFFERRETURN)
                {
                    if (item.status == StatusOrderResponseCode.PENDING)
                    {
                        PendingOrderList.Remove(item);
                        OrderedItemList.Remove(item);
                        UpdatePalletState(item.palletId_P, item.planId, PalletStatus.F);
                    }
                }
            }
            catch { }
        }
        public void RestoreCallBack(OrderItem item)
        {
            if (item.typeReq == TyeRequest.TYPEREQUEST_FORLIFT_TO_BUFFER)
            {
                if (item.status == StatusOrderResponseCode.PENDING)
                {
                    PendingOrderList.Add(item);
                    OrderedItemList.Remove(item);
                    UpdatePalletState(item.palletId_P, item.planId, PalletStatus.P);
                }
            }
            else if (item.typeReq == TyeRequest.TYPEREQUEST_BUFFER_TO_MACHINE)
            {
                if (item.status == StatusOrderResponseCode.PENDING)
                {
                    PendingOrderList.Remove(item);
                    OrderedItemList.Remove(item);
                    UpdatePalletState(item.palletId_H, item.planId, PalletStatus.H);
                }
            }
            else if (item.typeReq == TyeRequest.TYPEREQUEST_PALLET_EMPTY_MACHINE_TO_RETURN)
            {
                if (item.status == StatusOrderResponseCode.PENDING)
                {
                    PendingOrderList.Remove(item);
                    OrderedItemList.Remove(item);
                }
            }
            else if (item.typeReq == TyeRequest.TYPEREQUEST_WMS_RETURN_PALLET_BUFFERRETURN_TO_BUFFER401)
            {
                if (item.status == StatusOrderResponseCode.PENDING)
                {
                    PendingOrderList.Remove(item);
                    OrderedItemList.Remove(item);
                    UpdatePalletState(item.palletId_P, item.planId, PalletStatus.P);
                    UpdatePalletState(item.palletId_H, item.planId, PalletStatus.H);
                }
            }
            else if (item.typeReq == TyeRequest.TYPEREQUEST_WMS_RETURN_PALLET_BUFFER_TO_GATE)
            {
                if (item.status == StatusOrderResponseCode.PENDING)
                {
                    PendingOrderList.Remove(item);
                    OrderedItemList.Remove(item);
                    UpdatePalletState(item.palletId_H, item.planId, PalletStatus.H);
                }
            }
            else if (item.typeReq == TyeRequest.TYPEREQUEST_MACHINE_TO_BUFFERRETURN)
            {
                if (item.status == StatusOrderResponseCode.PENDING)
                {
                    PendingOrderList.Remove(item);
                    OrderedItemList.Remove(item);
                    UpdatePalletState(item.palletId_P, item.planId, PalletStatus.P);
                }
            }
        }

    }
       
}
