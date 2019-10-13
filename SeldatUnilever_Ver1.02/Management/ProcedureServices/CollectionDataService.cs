using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SeldatMRMS;
using SeldatMRMS.Management;
using SelDatUnilever_Ver1._00.Communication.HttpBridge;
using SelDatUnilever_Ver1._00.Management.ProcedureServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static SeldatMRMS.Management.RobotManagent.RobotUnityControl;
using static SeldatMRMS.Management.TrafficRobotUnity;
using static SelDatUnilever_Ver1._00.Management.DeviceManagement.DeviceItem;

namespace SelDatUnilever_Ver1
{


    public class CollectionDataService
    {
        public enum PalletStatus
        {
            F = 200, // Free pallet
            W = 201, // Have Pallet
            P = 202,
            H = 203,
            R = 204

        }
        //public int planID { get; set; }
        // public int productID { get; set; }
        // public int planID { get; set; }
        protected OrderItem order;
        //public String typeRequest; // FL: ForkLift// BM: BUFFER MACHINE // PR: Pallet return
        //public String activeDate;
        // public int timeWorkID;
        public List<Pose> checkInBuffer = new List<Pose>();

        protected int palletId { get; set; }
        protected int planId { get; set; }
        protected int bayId { get; set; }
        public Pose goalFrontLinePos;
        public CollectionDataService()
        {
            // clientRequest = new BridgeClientRequest();
            // clientRequest.ReceiveResponseHandler += ReceiveResponseHandler;
            planId = -1;
        }
        public CollectionDataService(OrderItem order)
        {
            this.order = order;
            //clientRequest = new BridgeClientRequest();
            // clientRequest.ReceiveResponseHandler += ReceiveResponseHandler;

        }
        public virtual void AssignAnOrder(OrderItem order)
        {
            this.order = order;
        }
        public String createPlanBuffer()
        {
            dynamic product = new JObject();
            product.timeWorkId = 1;
            product.activeDate = order.activeDate;
            product.productId = order.productId;
            product.productDetailId = order.productDetailId;
            product.updUsrId = Global_Object.userLogin;
            product.palletAmount = 1;
            String response = RequestDataProcedure(product.ToString(), Global_Object.url + "plan/createPlanPallet");
            return response;
        }

        public void FreePlanedBuffer(int palletId)
        {
            String url = Global_Object.url + "pallet/updatePalletStatus";
            if (palletId > 0)
            {
                dynamic product = new JObject();
                product.palletId = palletId;
                product.planId = order.planId;
                product.palletStatus = PalletStatus.F.ToString();
                product.updUsrId = Global_Object.userLogin;
                var data = RequestDataProcedure(product.ToString(), url);

            }

        }

        public void FreeHoldBuffer(int palletId)
        {
            String url = Global_Object.url + "pallet/updatePalletStatus";
            if (palletId > 0)
            {
                dynamic product = new JObject();
                product.palletId = palletId;
                product.planId = order.planId;
                product.palletStatus = PalletStatus.W.ToString();
                product.updUsrId = Global_Object.userLogin;
                var data = RequestDataProcedure(product.ToString(), url);

            }

        }
        // lấy pallet info cho viec free the plan planed

        public int GetPalletId_planed(OrderItem order)
        {
            int palletId = -1;
            try
            {
                String collectionData = RequestDataProcedure(order.dataRequest, Global_Object.url + "plan/getListPlanPallet");
                if (collectionData.Length > 0)
                {
                    try
                    {
                        JArray results = JArray.Parse(collectionData);
                        foreach (var result in results)
                        {

                            int temp_planId = (int)result["planId"];
                            if (temp_planId == order.planId)
                            {
                                foreach (var buffer in result["buffers"])
                                {
                                    if (buffer["pallets"].Count() > 0)
                                    {
                                        var bufferResults = buffer;
                                        var palletInfo = bufferResults["pallets"][buffer["pallets"].Count() - 1];
                                        palletId = (int)palletInfo["palletId"];
                                        return palletId;
                                    }
                                }
                            }
                        }

                    }
                    catch { }

                }
            }
            catch
            {
                Console.WriteLine("Errror Get palletID");
            }
            return palletId;
        }
        public int GetPalletId_Hold(int planId, bool onPlanId)
        {
            int palletId = -1;
            try
            {
                String collectionData = RequestDataProcedure(order.dataRequest, Global_Object.url + "plan/getListPlanPallet");
                if (collectionData.Length > 0)
                {
                    try
                    {
                        JArray results = JArray.Parse(collectionData);
                        var result = results[0];
                        foreach (var buffer in result["buffers"])
                        {
                            if (buffer["pallets"].Count() > 0)
                            {
                                var palletInfo = buffer["pallets"][0];
                                palletId = (int)palletInfo["palletId"];
                                return palletId;
                            }
                        }
                    }
                    catch { }

                }
            }
            catch
            {
                Console.WriteLine("Errror Get palletID");
            }
            return palletId;
        }

        public String RequestDataProcedure(String dataReq, String url)
        {

            //String url = Global_Object.url+"plan/getListPlanPallet";
            // String url = "http://localhost:8080";
            BridgeClientRequest clientRequest = new BridgeClientRequest();
            var data = clientRequest.PostCallAPI(url, dataReq);
            return data.Result;


        }
        public void delay(int ms)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            bool responsed = false;

            while (true)
            {
                if (sw.ElapsedMilliseconds > ms) break;
            }
        }

        #region [ Check In, AnyPoint, FrontLine, GetPalletInfo ] [MAchine to Buffer Return]  
        #region GET CHECK IN BUFFERRETURN [Machine to Buffer Return]
        public Pose GetCheckInBufferReturn_MBR(String dataReq)
        {
            Pose poseTemp = null;
            try
            {
                String collectionData = RequestDataProcedure(dataReq, Global_Object.url + "plan/getListPlanPallet");
                if (collectionData.Length > 0)
                {
                    JArray results = JArray.Parse(collectionData);
                    foreach (var result in results)
                    {
                        int deviceId = (int)result["deviceId"];
                        if (deviceId == order.deviceId)
                        {
                            foreach (var buffer in result["buffers"])
                            {
                                if (buffer["pallets"].Count() > 0)
                                {
                                    String checkinResults = (String)buffer["bufferCheckIn"];
                                    JObject stuff = JObject.Parse(checkinResults);
                                    double x = (double)stuff["checkin"]["x"];
                                    double y = (double)stuff["checkin"]["y"];
                                    double angle = (double)stuff["checkin"]["angle"];
                                    poseTemp = new Pose(x, y, angle);
                                    planId = order.planId;
                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }
                    }


                }
            }
            catch { Console.WriteLine("Error check in data collection"); }
            return poseTemp;
        }
        #endregion
        #region GET FRONT LINE BUFFER [ Machine to BufferReturn]
        public Pose GetFrontLineBufferReturn_MBR(String dataReq)
        {
            Pose poseTemp = null;
            try
            {
                String collectionData = RequestDataProcedure(dataReq, Global_Object.url + "plan/getListPlanPallet");
                if (collectionData.Length > 0)
                {
                    JArray results = JArray.Parse(collectionData);
                    foreach (var result in results)
                    {
                        int deviceId = (int)result["deviceId"];
                        if (deviceId == order.deviceId)
                        {
                            //var bufferResults = result["buffers"][0];
                            foreach (var buffer in result["buffers"])
                            {
                                if (buffer["pallets"].Count() > 0)
                                {
                                    foreach (var palletInfo in buffer["pallets"])
                                    {

                                        int palletId = (int)palletInfo["palletId"];
                                        if (palletId == order.palletId_P)
                                        {
                                            JObject stuff = JObject.Parse((String)palletInfo["dataPallet"]);
                                            bayId = (int)stuff["bayId"];
                                            double x = (double)stuff["line"]["x"];
                                            double y = (double)stuff["line"]["y"];
                                            double angle = (double)stuff["line"]["angle"];
                                            poseTemp = new Pose(x, y, angle);
                                            goalFrontLinePos = poseTemp;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                //  Console.WriteLine(""+poseTemp.Position.ToString());
            }
            catch
            {
                Console.WriteLine("Error Front Line");
            }
            return poseTemp;
        }
        #endregion
        #region GET PALLET INFO BUFFER [ Machine to BufferReturn]
        public String GetInfoOfPalletBufferReturn_MBR(TrafficRobotUnity.PistonPalletCtrl pisCtrl, String dataReq)
        {
            JInfoPallet infoPallet = new JInfoPallet();
            try
            {
                String collectionData = RequestDataProcedure(dataReq, Global_Object.url + "plan/getListPlanPallet");
                if (collectionData.Length > 0)
                {
                    JArray results = JArray.Parse(collectionData);
                    foreach (var result in results)
                    {
                        int deviceId = (int)result["deviceId"];
                        if (deviceId == order.deviceId)
                        {

                            //var bufferResults = result["buffers"][0];
                            foreach (var buffer in result["buffers"])
                            {
                                //int bufferId = (int)buffer["bufferId"];
                                //if (bufferId == order.bufferId)
                                {
                                    if (buffer["pallets"].Count() > 0)
                                    {
                                        foreach (var palletInfo in buffer["pallets"])
                                        {
                                            palletId = (int)palletInfo["palletId"];
                                            if (palletId == order.palletId_P)
                                            {

                                                JObject stuff = JObject.Parse((String)palletInfo["dataPallet"]);
                                                int row = (int)stuff["pallet"]["row"];
                                                int bay = (int)stuff["pallet"]["bay"];
                                                int directMain = (int)stuff["pallet"]["dir_main"];
                                                int directSub = (int)stuff["pallet"]["dir_sub"];
                                                int directOut = (int)stuff["pallet"]["dir_out"];
                                                int line_ord = (int)stuff["pallet"]["line_ord"];
                                                string subline = (string)stuff["pallet"]["hasSubLine"];

                                                infoPallet.pallet = pisCtrl; /* dropdown */
                                                infoPallet.dir_main = (TrafficRobotUnity.BrDirection)directMain;
                                                infoPallet.bay = bay;
                                                infoPallet.hasSubLine = subline; /* yes or no */
                                                infoPallet.dir_sub = (TrafficRobotUnity.BrDirection)directSub; /* right */
                                                infoPallet.dir_out = (TrafficRobotUnity.BrDirection)directOut;
                                                infoPallet.row = row;
                                                infoPallet.line_ord = line_ord;
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                Console.WriteLine("Error at GetInfoOfPalletBuffer");
                return "";
            }
            return JsonConvert.SerializeObject(infoPallet);
        }
        #endregion
        #endregion

        #region [ Check In, AnyPoint, FrontLine, GetPalletInfo ] [BufferReturn to Buffer401]  
        #region GET CHECK IN BUFFERRETURN [ BufferReturn to Buffer401]
        public Pose GetCheckInBufferReturn_BRB401(String dataReq)
        {
            Pose poseTemp = null;
            try
            {
                String collectionData = RequestDataProcedure(dataReq, Global_Object.url + "plan/getListPlanPallet");
                if (collectionData.Length > 0)
                {
                    JArray results = JArray.Parse(collectionData);
                    foreach (var result in results)
                    {
                        int deviceId = (int)result["deviceId"];
                        if (deviceId == order.deviceId)
                        {
                            foreach (var buffer in result["buffers"])
                            {
                                int bufferId = (int)buffer["bufferId"];
                                if (bufferId == order.bufferId)
                                {
                                    if (buffer["pallets"].Count() > 0)
                                    {
                                        String checkinResults = (String)buffer["bufferCheckIn"];
                                        JObject stuff = JObject.Parse(checkinResults);
                                        double x = (double)stuff["checkin"]["x"];
                                        double y = (double)stuff["checkin"]["y"];
                                        double angle = (double)stuff["checkin"]["angle"];
                                        poseTemp = new Pose(x, y, angle);
                                        planId = order.planId;
                                        break;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                            }
                        }
                    }


                }
            }
            catch { Console.WriteLine("Error check in data collection"); }
            return poseTemp;
        }
        #endregion
        #region GET ANY POINT BUFFER [ BufferReturn to Buffer401]
        public Pose GetAnyPointInBufferReturn_BRB401(String dataReq) // đổi 
        {

            Pose poseTemp = null;
            try
            {
                String collectionData = RequestDataProcedure(dataReq, Global_Object.url + "plan/getListPlanPallet");
                if (collectionData.Length > 0)
                {
                    JArray results = JArray.Parse(collectionData);

                    foreach (var result in results)
                    {
                        int deviceId = (int)result["deviceId"];
                        if (deviceId == order.deviceId)
                        {
                            foreach (var buffer in result["buffers"])
                            {
                                int bufferId = (int)buffer["bufferId"];
                                if (bufferId == order.bufferId)
                                {
                                    if (buffer["pallets"].Count() > 0)
                                    {
                                        //var bufferResults = result["buffers"][0];
                                        String checkinResults = (String)buffer["bufferCheckIn"];
                                        JObject stuff = JObject.Parse(checkinResults);
                                        double x = (double)stuff["headpoint"]["x"];
                                        double y = (double)stuff["headpoint"]["y"];
                                        double angle = (double)stuff["headpoint"]["angle"];
                                        poseTemp = new Pose(x, y, angle);
                                        break;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                Console.WriteLine("Get AnyPoint Error");
            }
            return poseTemp;
        }
        #endregion
        #region GET FRONT LINE BUFFER [ BufferReturn to Buffer401]
        public Pose GetFrontLineBufferReturn_BRB401(String dataReq)
        {
            Pose poseTemp = null;
            try
            {
                String collectionData = RequestDataProcedure(dataReq, Global_Object.url + "plan/getListPlanPallet");
                if (collectionData.Length > 0)
                {
                    JArray results = JArray.Parse(collectionData);
                    foreach (var result in results)
                    {
                        int deviceId = (int)result["deviceId"];
                        if (deviceId == order.deviceId)
                        {
                            //var bufferResults = result["buffers"][0];
                            foreach (var buffer in result["buffers"])
                            {
                                int bufferId = (int)buffer["bufferId"];
                                if (bufferId == order.bufferId)
                                {
                                    if (buffer["pallets"].Count() > 0)
                                    {
                                        foreach (var palletInfo in buffer["pallets"])
                                        {
                                            int palletId = (int)palletInfo["palletId"];
                                            if (palletId == order.palletId_H)
                                            {
                                                JObject stuff = JObject.Parse((String)palletInfo["dataPallet"]);
                                                bayId = (int)stuff["bayId"];
                                                double x = (double)stuff["line"]["x"];
                                                double y = (double)stuff["line"]["y"];
                                                double angle = (double)stuff["line"]["angle"];
                                                poseTemp = new Pose(x, y, angle);
                                                goalFrontLinePos = poseTemp;
                                                break;
                                            }

                                        }
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                            }
                        }
                    }
                }
                //  Console.WriteLine(""+poseTemp.Position.ToString());
            }
            catch
            {
                Console.WriteLine("Error Front Line");
            }
            return poseTemp;
        }
        #endregion
        #region GET PALLET INFO BUFFER [ BufferReturn to Buffer401]
        public String GetInfoOfPalletBufferReturn_BRB401(TrafficRobotUnity.PistonPalletCtrl pisCtrl, String dataReq)
        {
            JInfoPallet infoPallet = new JInfoPallet();
            try
            {
                String collectionData = RequestDataProcedure(dataReq, Global_Object.url + "plan/getListPlanPallet");
                if (collectionData.Length > 0)
                {
                    JArray results = JArray.Parse(collectionData);
                    foreach (var result in results)
                    {
                        int deviceId = (int)result["deviceId"];
                        if (deviceId == order.deviceId)
                        {

                            //var bufferResults = result["buffers"][0];
                            foreach (var buffer in result["buffers"])
                            {
                                int bufferId = (int)buffer["bufferId"];
                                if (bufferId == order.bufferId)
                                {
                                    if (buffer["pallets"].Count() > 0)
                                    {
                                        var palletInfo = buffer["pallets"][0];
                                        palletId = (int)palletInfo["palletId"];
                                        JObject stuff = JObject.Parse((String)palletInfo["dataPallet"]);
                                        int row = (int)stuff["pallet"]["row"];
                                        int bay = (int)stuff["pallet"]["bay"];
                                        int directMain = (int)stuff["pallet"]["dir_main"];
                                        int directSub = (int)stuff["pallet"]["dir_sub"];
                                        int directOut = (int)stuff["pallet"]["dir_out"];
                                        int line_ord = (int)stuff["pallet"]["line_ord"];
                                        string subline = (string)stuff["pallet"]["hasSubLine"];

                                        infoPallet.pallet = pisCtrl; /* dropdown */
                                        infoPallet.dir_main = (TrafficRobotUnity.BrDirection)directMain;
                                        infoPallet.bay = bay;
                                        infoPallet.hasSubLine = subline; /* yes or no */
                                        infoPallet.dir_sub = (TrafficRobotUnity.BrDirection)directSub; /* right */
                                        infoPallet.dir_out = (TrafficRobotUnity.BrDirection)directOut;
                                        infoPallet.row = row;
                                        infoPallet.line_ord = line_ord;
                                        break;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                Console.WriteLine("Error at GetInfoOfPalletBuffer");
                return "";
            }
            return JsonConvert.SerializeObject(infoPallet);
        }
        #endregion

        #region GET CHECK IN BUFFER401 [ BufferReturn to Buffer401]
        public Pose GetCheckInBuffer401_BRB401(String dataReq)
        {
            Pose poseTemp = null;
            try
            {
                String collectionData = RequestDataProcedure(dataReq, Global_Object.url + "plan/getListPlanPallet");
                if (collectionData.Length > 0)
                {
                    JArray results = JArray.Parse(collectionData);
                    foreach (var result in results)
                    {
                        int deviceIdPut = (int)result["deviceIdPut"];
                        if (deviceIdPut == order.deviceIdPut)
                        {
                            foreach (var buffer in result["buffers"])
                            {
                                if (buffer["pallets"].Count() > 0)
                                {
                                    String checkinResults = (String)buffer["bufferCheckIn"];
                                    JObject stuff = JObject.Parse(checkinResults);
                                    double x = (double)stuff["checkin"]["x"];
                                    double y = (double)stuff["checkin"]["y"];
                                    double angle = (double)stuff["checkin"]["angle"];
                                    poseTemp = new Pose(x, y, angle);
                                    planId = order.planId;
                                    break;
                                }
                                else
                                {
                                    continue;
                                }

                            }
                        }
                    }


                }
            }
            catch { Console.WriteLine("Error check in data collection"); }
            return poseTemp;
        }
        #endregion

        #region GET ANY POINT BUFFER [ BufferReturn to Buffer401]
        public Pose GetAnyPointInBuffer401_BRB401(String dataReq) // đổi 
        {

            Pose poseTemp = null;
            try
            {
                String collectionData = RequestDataProcedure(dataReq, Global_Object.url + "plan/getListPlanPallet");
                if (collectionData.Length > 0)
                {
                    JArray results = JArray.Parse(collectionData);

                    foreach (var result in results)
                    {
                        int deviceIdPut = (int)result["deviceIdPut"];
                        if (deviceIdPut == order.deviceIdPut)
                        {
                            foreach (var buffer in result["buffers"])
                            {
                                if (buffer["pallets"].Count() > 0)
                                {
                                    //var bufferResults = result["buffers"][0];
                                    String checkinResults = (String)buffer["bufferCheckIn"];
                                    JObject stuff = JObject.Parse(checkinResults);
                                    double x = (double)stuff["headpoint"]["x"];
                                    double y = (double)stuff["headpoint"]["y"];
                                    double angle = (double)stuff["headpoint"]["angle"];
                                    poseTemp = new Pose(x, y, angle);
                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                Console.WriteLine("Get AnyPoint Error");
            }
            return poseTemp;
        }
        #endregion

        #region GET FRONT LINE BUFFER [ BufferReturn to Buffer401]
        public Pose GetFrontLineBuffer401_BRB401(String dataReq)
        {
            Pose poseTemp = null;
            try
            {
                String collectionData = RequestDataProcedure(dataReq, Global_Object.url + "plan/getListPlanPallet");
                if (collectionData.Length > 0)
                {
                    JArray results = JArray.Parse(collectionData);
                    foreach (var result in results)
                    {
                        int deviceIdPut = (int)result["deviceIdPut"];
                        if (deviceIdPut == order.deviceIdPut)
                        {
                            //var bufferResults = result["buffers"][0];
                            foreach (var buffer in result["buffers"])
                            {
                                if (buffer["pallets"].Count() > 0)
                                {
                                    foreach (var palletInfo in buffer["pallets"])
                                    {
                                        int palletId = (int)palletInfo["palletId"];
                                        if (palletId == order.palletId_P)
                                        {
                                            JObject stuff = JObject.Parse((String)palletInfo["dataPallet"]);
                                            bayId = (int)stuff["bayId"];
                                            double x = (double)stuff["line"]["x"];
                                            double y = (double)stuff["line"]["y"];
                                            double angle = (double)stuff["line"]["angle"];
                                            poseTemp = new Pose(x, y, angle);
                                            goalFrontLinePos = poseTemp;
                                            break;
                                        }

                                    }
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }
                    }
                }
                //  Console.WriteLine(""+poseTemp.Position.ToString());
            }
            catch
            {
                Console.WriteLine("Error Front Line");
            }
            return poseTemp;
        }
        #endregion

        #region GET PALLET INFO BUFFER401 [ BufferReturn to Buffer401]
        public String GetInfoOfPalletBuffer401_BRB401(TrafficRobotUnity.PistonPalletCtrl pisCtrl, String dataReq)
        {
            JInfoPallet infoPallet = new JInfoPallet();
            try
            {

                String collectionData = RequestDataProcedure(dataReq, Global_Object.url + "plan/getListPlanPallet");
                if (collectionData.Length > 0)
                {
                    JArray results = JArray.Parse(collectionData);
                    foreach (var result in results)
                    {
                        int deviceId = (int)result["deviceId"];
                        if (deviceId == order.deviceId)
                        {
                            //var bufferResults = result["buffers"][0];
                            foreach (var buffer in result["buffers"])
                            {
                                if (buffer["pallets"].Count() > 0)
                                {
                                    var palletInfo = buffer["pallets"][0];
                                    palletId = (int)palletInfo["palletId"];
                                    JObject stuff = JObject.Parse((String)palletInfo["dataPallet"]);
                                    int row = (int)stuff["pallet"]["row"];
                                    int bay = (int)stuff["pallet"]["bay"];
                                    int directMain = (int)stuff["pallet"]["dir_main"];
                                    int directSub = (int)stuff["pallet"]["dir_sub"];
                                    int directOut = (int)stuff["pallet"]["dir_out"];
                                    int line_ord = (int)stuff["pallet"]["line_ord"];
                                    string subline = (string)stuff["pallet"]["hasSubLine"];

                                    infoPallet.pallet = pisCtrl; /* dropdown */
                                    infoPallet.dir_main = (TrafficRobotUnity.BrDirection)directMain;
                                    infoPallet.bay = bay;
                                    infoPallet.hasSubLine = subline; /* yes or no */
                                    infoPallet.dir_sub = (TrafficRobotUnity.BrDirection)directSub; /* right */
                                    infoPallet.dir_out = (TrafficRobotUnity.BrDirection)directOut;
                                    infoPallet.row = row;
                                    infoPallet.line_ord = line_ord;
                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                Console.WriteLine("Error at GetInfoOfPalletBuffer");
                return "";
            }
            return JsonConvert.SerializeObject(infoPallet);
        }
        #endregion
        #endregion
        #region [ Check In, AnyPoint, FrontLine, GetPalletInfo ] [ FF-> BF / BF -> MACH] 
        #region GET CHECK IN BUFFER
        public Pose GetCheckInBuffer(bool onPlandId = false)
        {
            Pose poseTemp = null;
            try
            {

                String collectionData = RequestDataProcedure(order.dataRequest, Global_Object.url + "plan/getListPlanPallet");
                if (collectionData.Length > 0)
                {
                    JArray results = JArray.Parse(collectionData);
                    if (onPlandId)
                    {
                        foreach (var result in results)
                        {
                            int temp_planId = (int)result["planId"];

                            if (temp_planId == order.planId)
                            {
                                //var bufferResults = result["buffers"][0];
                                foreach (var buffer in result["buffers"])
                                {
                                    if (buffer["pallets"].Count() > 0)
                                    {
                                        String checkinResults = (String)buffer["bufferCheckIn"];
                                        JObject stuff = JObject.Parse(checkinResults);
                                        double x = (double)stuff["checkin"]["x"];
                                        double y = (double)stuff["checkin"]["y"];
                                        double angle = (double)stuff["checkin"]["angle"];
                                        poseTemp = new Pose(x, y, angle);
                                        planId = order.planId;
                                        break;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                                break;
                            }
                        }
                    }
                    else
                    {
                        var result = results[0];
                        foreach (var buffer in result["buffers"])
                        {
                            String bufferDataStr = (String)buffer["bufferData"];
                            JObject stuffBData = JObject.Parse(bufferDataStr);
                            bool canOpEdit = (bool)stuffBData["canOpEdit"];
                            if (canOpEdit) // buffer có edit nên bỏ qua lý do bởi buffer có edit nằm gần các máy
                                continue;
                            if (buffer["pallets"].Count() > 0)
                            {
                                String checkinResults = (String)buffer["bufferCheckIn"];
                                JObject stuff = JObject.Parse(checkinResults);
                                double x = (double)stuff["checkin"]["x"];
                                double y = (double)stuff["checkin"]["y"];
                                double angle = (double)stuff["checkin"]["angle"];
                                poseTemp = new Pose(x, y, angle);
                                planId = order.planId;
                                break;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        //var bufferResults = result["buffers"][0];
                        //String checkinResults = (String)bufferResults["bufferCheckIn"];
                        //JObject stuff = JObject.Parse(checkinResults);
                        //double x = (double)stuff["checkin"]["x"];
                        //double y = (double)stuff["checkin"]["y"];
                        //double angle = (double)stuff["checkin"]["angle"];
                        //poseTemp = new Pose(x, y, angle);
                        //planId = order.planId;
                    }
                }
            }
            catch { Console.WriteLine("Error check in data collection"); }
            return poseTemp;
        }
        #endregion
        #region GET ANY POINT BUFFER
        public Pose GetAnyPointInBuffer(bool onPlandId = false) // đổi 
        {

            Pose poseTemp = null;
            try
            {
                String collectionData = RequestDataProcedure(order.dataRequest, Global_Object.url + "plan/getListPlanPallet");
                if (collectionData.Length > 0)
                {
                    JArray results = JArray.Parse(collectionData);
                    if (onPlandId)
                    {
                        foreach (var result in results)
                        {
                            int temp_planId = (int)result["planId"];
                            if (temp_planId == order.planId)
                            {
                                //var bufferResults = result["buffers"][0];
                                foreach (var buffer in result["buffers"])
                                {
                                    if (buffer["pallets"].Count() > 0)
                                    {
                                        String checkinResults = (String)buffer["bufferCheckIn"];
                                        JObject stuff = JObject.Parse(checkinResults);
                                        double x = (double)stuff["headpoint"]["x"];
                                        double y = (double)stuff["headpoint"]["y"];
                                        double angle = (double)stuff["headpoint"]["angle"];
                                        poseTemp = new Pose(x, y, angle);
                                        break;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }

                                break;

                            }
                        }
                    }
                    else
                    {
                        var result = results[0];
                        foreach (var buffer in result["buffers"])
                        {
                            String bufferDataStr = (String)buffer["bufferData"];
                            JObject stuffBData = JObject.Parse(bufferDataStr);
                            bool canOpEdit = (bool)stuffBData["canOpEdit"];
                            if (canOpEdit) // buffer có edit nên bỏ qua lý do bởi buffer có edit nằm gần các máy
                                continue;
                            if (buffer["pallets"].Count() > 0)
                            {
                                //var bufferResults = result["buffers"][0];
                                String checkinResults = (String)buffer["bufferCheckIn"];
                                JObject stuff = JObject.Parse(checkinResults);
                                double x = (double)stuff["headpoint"]["x"];
                                double y = (double)stuff["headpoint"]["y"];
                                double angle = (double)stuff["headpoint"]["angle"];
                                poseTemp = new Pose(x, y, angle);
                                break;
                            }
                            else
                            {
                                continue;
                            }
                        }

                    }
                }
            }
            catch
            {
                Console.WriteLine("Get AnyPoint Error");
            }
            return poseTemp;
        }
        #endregion
        #region GET FRONT LINE BUFFER
        public Pose GetFrontLineBuffer(bool onPlandId = false)
        {
            Pose poseTemp = null;
            String collectionData="";
            try
            {
               collectionData = RequestDataProcedure(order.dataRequest, Global_Object.url + "plan/getListPlanPallet");
                if (collectionData.Length > 0)
                {
                    JArray results = JArray.Parse(collectionData);
                    if (onPlandId)
                    {
                        foreach (var result in results)
                        {
                            int temp_planId = (int)result["planId"];
                            //  if (temp_planId == order.planId)
                            {
                                //var bufferResults = result["buffers"][0];
                                foreach (var buffer in result["buffers"])
                                {
                                    int bufferId = (int)buffer["bufferId"];
                                    if (buffer["pallets"].Count() > 0 && bufferId==order.bufferId )
                                    {
                                        foreach (var palletInfo in buffer["pallets"])
                                        {
                                            int palletId = (int)palletInfo["palletId"];
                                            int bay = (int)palletInfo["bay"];
                                            if (bay == order.palletBay )
                                            {
                                                JObject stuff = JObject.Parse((String)palletInfo["dataPallet"]);
                                                bayId = (int)stuff["bayId"];
                                                double x = (double)stuff["line"]["x"];
                                                double y = (double)stuff["line"]["y"];
                                                double angle = (double)stuff["line"]["angle"];
                                                poseTemp = new Pose(x, y, angle);
                                                goalFrontLinePos = poseTemp;
                                                break;
                                            }

                                        }
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                                break;

                            }
                        }
                    }
                    else
                    {

                        var result = results[0];
                        //var bufferResults = result["buffers"][0];
                        foreach (var buffer in result["buffers"])
                        {
                            int bufferId = (int)buffer["bufferId"];
                            String bufferDataStr = (String)buffer["bufferData"];
                            JObject stuffBData = JObject.Parse(bufferDataStr);
                            bool canOpEdit = (bool)stuffBData["canOpEdit"];
                            if (canOpEdit) // buffer có edit nên bỏ qua lý do bởi buffer có edit nằm gần các máy
                                continue;
                            if (buffer["pallets"].Count() > 0 && bufferId==order.bufferId)
                            {
                                foreach (var palletInfo in buffer["pallets"])
                                {
                                    int palletId = (int)palletInfo["palletId"];
                                    if (palletId == order.palletId_H)
                                    {
                                        JObject stuff = JObject.Parse((String)palletInfo["dataPallet"]);
                                        bayId = (int)stuff["bayId"];
                                        double x = (double)stuff["line"]["x"];
                                        double y = (double)stuff["line"]["y"];
                                        double angle = (double)stuff["line"]["angle"];
                                        poseTemp = new Pose(x, y, angle);
                                        goalFrontLinePos = poseTemp;
                                        break;
                                    }

                                }
                            }
                            else
                            {
                                continue;
                            }
                        }

                    }
                }
                //  Console.WriteLine(""+poseTemp.Position.ToString());
            }
            catch
            {
                Console.WriteLine("-----------Error CollectionData----------");
                Console.WriteLine(collectionData);
                Console.WriteLine("Error Front Line");
            }
            if(poseTemp==null)
            {
                Console.WriteLine(collectionData);
                Console.WriteLine("Error Front Line");
            }
            return poseTemp;
        }
        #endregion
        #region GET PALLET INFO BUFFER

        // Get pallet info status W buffer with same bay
        public int GetBufferId_from_PalletId(String datareq, int samePalletId)
        {
            int bufferId = -1;
            try
            {
                String collectionData = RequestDataProcedure(order.dataRequest, Global_Object.url + "plan/getListPlanPallet");
                if (collectionData.Length > 0)
                {
                    JArray results = JArray.Parse(collectionData);
                    var result = results[0];
                    //var bufferResults = result["buffers"][0];
                    foreach (var buffer in result["buffers"])
                    {
                        int _bufferId = (int)buffer["bufferId"];
                        String bufferDataStr = (String)buffer["bufferData"];
                        JObject stuffBData = JObject.Parse(bufferDataStr);
                        bool canOpEdit = (bool)stuffBData["canOpEdit"];
                        if (canOpEdit) // buffer có edit nên bỏ qua lý do bởi buffer có edit nằm gần các máy, áp dụng trong quy trình Buffer -> Machine
                            continue;
                        if (buffer["pallets"].Count() > 0)
                        {
                            foreach (var palletInfo in buffer["pallets"])
                            {
                                int _palletId = (int)palletInfo["palletId"];
                                if (_palletId == samePalletId)
                                {
                                    bufferId = _bufferId;
                                    break;
                                }
                            }
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }

                }
            }
            catch
            {
                Console.WriteLine("Error at GetInfoOfPalletBuffer");
                return bufferId;
            }
            return bufferId;
        }
        public JPallet GetInfoOfPalletBuffer_Compare_W_H(TrafficRobotUnity.PistonPalletCtrl pisCtrl, JPallet JPResult)
        {
            /*JPallet JPResult = new JPallet();
            JPResult.jInfoPallet = jInfoPallet_H;
            JPResult.palletId = order.palletId_H;*/
            List<JPallet> jPalletList = new List<JPallet>();
            try
            {
                dynamic product_W = new JObject();
                product_W.timeWorkId = order.timeWorkId;
                product_W.activeDate = order.activeDate;
                product_W.productId = order.productId;
                product_W.productDetailId = order.productDetailId;
                product_W.palletStatus = PalletStatus.W.ToString(); // W
                int bufferId = GetBufferId_from_PalletId(order.dataRequest, JPResult.palletId);
                String collectionData_W = RequestDataProcedure(product_W.ToString(), Global_Object.url + "plan/getListPlanPallet");
                String collectionData_H = RequestDataProcedure(order.dataRequest, Global_Object.url + "plan/getListPlanPallet");
                if (collectionData_W.Length > 0)
                {
                    JArray results = JArray.Parse(collectionData_W);
                    foreach (var plan in results) //loop plan
                    {
                        foreach (var buffer in plan["buffers"]) //loop buffer
                        {
                            int _bufferId = (int)buffer["bufferId"];
                            String bufferDataStr = (String)buffer["bufferData"];
                            JObject stuffBData = JObject.Parse(bufferDataStr);
                            bool canOpEdit = (bool)stuffBData["canOpEdit"];
                            if (canOpEdit) // buffer có edit nên bỏ qua lý do bởi buffer có edit nằm gần các máy, áp dụng trong quy trình Buffer -> Machine
                                continue;
                            if (buffer["pallets"].Count() > 0 && _bufferId==order.bufferId)
                            {
                                foreach (var palletInfo in buffer["pallets"])
                                {
                                    int bay = (int)palletInfo["bay"];
                                    int _palletId = (int)palletInfo["palletId"];
                                    if (bay == JPResult.jInfoPallet.bay)
                                    {
                                        JPallet jPallet = new JPallet();
                                        JInfoPallet infoPallet = new JInfoPallet();
                                        JObject stuff = JObject.Parse((String)palletInfo["dataPallet"]);
                                        int _row = (int)stuff["pallet"]["row"];
                                        int _bay = (int)stuff["pallet"]["bay"];
                                        int directMain = (int)stuff["pallet"]["dir_main"];
                                        int directSub = (int)stuff["pallet"]["dir_sub"];
                                        int directOut = (int)stuff["pallet"]["dir_out"];
                                        int line_ord = (int)stuff["pallet"]["line_ord"];
                                        string subline = (string)stuff["pallet"]["hasSubLine"];

                                        infoPallet.pallet = pisCtrl; /* dropdown */
                                        infoPallet.dir_main = (TrafficRobotUnity.BrDirection)directMain;
                                        infoPallet.bay = _bay;
                                        infoPallet.hasSubLine = subline; /* yes or no */
                                        infoPallet.dir_sub = (TrafficRobotUnity.BrDirection)directSub; /* right */
                                        infoPallet.dir_out = (TrafficRobotUnity.BrDirection)directOut;
                                        infoPallet.row = _row;
                                        infoPallet.line_ord = line_ord;
                                        jPallet.jInfoPallet = infoPallet;
                                        jPallet.palletId = _palletId;

                                        jPalletList.Add(jPallet);
                                    }
                                }
                            }
                        }
                    }

                    int rowMin = JPResult.jInfoPallet.row;
                    foreach (JPallet jp in jPalletList)
                    {
                        if (rowMin > jp.jInfoPallet.row)
                        {
                            rowMin = jp.jInfoPallet.row;
                            JPResult.jInfoPallet = jp.jInfoPallet;
                            JPResult.palletId = jp.palletId;


                        }
                    }
                }

            }
            catch
            {
                Console.WriteLine("Error at GetInfoOfPalletBuffer");
                return JPResult;
            }
            return JPResult;
        }
        public JPallet GetInfoPallet_P_InBuffer(TrafficRobotUnity.PistonPalletCtrl pisCtrl)
        {
            JPallet infoPallet = new JPallet();
            infoPallet.jInfoPallet = new JInfoPallet();
            try
            {
                String collectionData = RequestDataProcedure(order.dataRequest, Global_Object.url + "plan/getListPlanPallet");
                if (collectionData.Length > 0)
                {
                    JArray results = JArray.Parse(collectionData);
                    //var result = results[0];
                    bool gotLastPalletInBay = false;
                    foreach (var plan in results)
                    {
                        if (!gotLastPalletInBay)
                        {
                            int temp_planId = (int)plan["planId"];
                            if (temp_planId == order.planId)
                            {
                                //var bufferResults = result["buffers"][0];
                                foreach (var buffer in plan["buffers"])
                                {
                                    int bufferId = (int)buffer["bufferId"];
                                    if (!gotLastPalletInBay)
                                    {
                                        String bufferDataStr = (String)buffer["bufferData"];
                                        JObject stuffBData = JObject.Parse(bufferDataStr);
                                        bool canOpEdit = (bool)stuffBData["canOpEdit"];
                                        //if (canOpEdit) // buffer có edit nên bỏ qua lý do bởi buffer có edit nằm gần các máy, áp dụng trong quy trình Buffer -> Machine
                                        //    continue;
                                        if (buffer["pallets"].Count() > 0 && bufferId==order.bufferId)
                                        {
                                            int bayToGo = -1;
                                            bool lastPallet = true;
                                            foreach (var palletInfo in buffer["pallets"])
                                            {
                                                //palletInfo = buffer["pallets"][0];
                                                JObject stuff = JObject.Parse((String)palletInfo["dataPallet"]);
                                                int row = (int)stuff["pallet"]["row"];
                                                int bay = (int)stuff["pallet"]["bay"];
                                                int directMain = (int)stuff["pallet"]["dir_main"];
                                                int directSub = (int)stuff["pallet"]["dir_sub"];
                                                int directOut = (int)stuff["pallet"]["dir_out"];
                                                int line_ord = (int)stuff["pallet"]["line_ord"];
                                                string subline = (string)stuff["pallet"]["hasSubLine"];



                                                if (bay != bayToGo)
                                                {
                                                    bayToGo = bay;
                                                    lastPallet = true;
                                                }
                                                else
                                                {
                                                    lastPallet = false;
                                                }

                                                palletId = (int)palletInfo["palletId"];

                                                //Keep first Pallet in each bay
                                                if (lastPallet)
                                                {
                                                    infoPallet.jInfoPallet.pallet = pisCtrl; /* dropdown */
                                                    infoPallet.jInfoPallet.dir_main = (TrafficRobotUnity.BrDirection)directMain;
                                                    infoPallet.jInfoPallet.bay = bay;
                                                    infoPallet.jInfoPallet.hasSubLine = subline; /* yes or no */
                                                    infoPallet.jInfoPallet.dir_sub = (TrafficRobotUnity.BrDirection)directSub; /* right */
                                                    infoPallet.jInfoPallet.dir_out = (TrafficRobotUnity.BrDirection)directOut;
                                                    infoPallet.jInfoPallet.row = row;
                                                    infoPallet.jInfoPallet.line_ord = line_ord;
                                                    infoPallet.palletId = palletId;
                                                }


                                                //Compare if pallet H is in this bay
                                                //if (palletId == order.palletId_P)
                                                if (bayToGo == order.palletBay)
                                                {
                                                    gotLastPalletInBay = true;
                                                    break;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                Console.WriteLine("Error at GetInfoOfPalletBuffer");
                return null;
            }
            return infoPallet;
        }
        public JPallet GetInfoPallet_H_InBuffer(TrafficRobotUnity.PistonPalletCtrl pisCtrl)
        {
            JPallet infoPallet = new JPallet();
            infoPallet.jInfoPallet = new JInfoPallet();
            try
            {
                String collectionData = RequestDataProcedure(order.dataRequest, Global_Object.url + "plan/getListPlanPallet");
                if (collectionData.Length > 0)
                {
                    JArray results = JArray.Parse(collectionData);
                    //var result = results[0];
                    bool gotFirstPalletInBay = false;
                    foreach (var plan in results)
                    {
                        if (!gotFirstPalletInBay)
                        {
                            //var bufferResults = result["buffers"][0];
                            foreach (var buffer in plan["buffers"])
                            {
                                int bufferId = (int)buffer["bufferId"];
                                if (!gotFirstPalletInBay)
                                {
                                    String bufferDataStr = (String)buffer["bufferData"];
                                    JObject stuffBData = JObject.Parse(bufferDataStr);
                                    bool canOpEdit = (bool)stuffBData["canOpEdit"];
                                    if (canOpEdit) // buffer có edit nên bỏ qua lý do bởi buffer có edit nằm gần các máy, áp dụng trong quy trình Buffer -> Machine
                                        continue;
                                    if (buffer["pallets"].Count() > 0 && bufferId==order.bufferId)
                                    {
                                        int bayToGo = -1;
                                        bool firstPallet = true;
                                        foreach (var palletInfo in buffer["pallets"])
                                        {
                                            //palletInfo = buffer["pallets"][0];
                                            JObject stuff = JObject.Parse((String)palletInfo["dataPallet"]);
                                            int row = (int)stuff["pallet"]["row"];
                                            int bay = (int)stuff["pallet"]["bay"];
                                            int directMain = (int)stuff["pallet"]["dir_main"];
                                            int directSub = (int)stuff["pallet"]["dir_sub"];
                                            int directOut = (int)stuff["pallet"]["dir_out"];
                                            int line_ord = (int)stuff["pallet"]["line_ord"];
                                            string subline = (string)stuff["pallet"]["hasSubLine"];

                                            if (bay != bayToGo)
                                            {
                                                bayToGo = bay;
                                                firstPallet = true;
                                            }
                                            else
                                            {
                                                firstPallet = false;
                                            }

                                            palletId = (int)palletInfo["palletId"];

                                            //Keep first Pallet in each bay
                                            if (firstPallet)
                                            {
                                                infoPallet.jInfoPallet.pallet = pisCtrl; /* dropdown */
                                                infoPallet.jInfoPallet.dir_main = (TrafficRobotUnity.BrDirection)directMain;
                                                infoPallet.jInfoPallet.bay = bay;
                                                infoPallet.jInfoPallet.hasSubLine = subline; /* yes or no */
                                                infoPallet.jInfoPallet.dir_sub = (TrafficRobotUnity.BrDirection)directSub; /* right */
                                                infoPallet.jInfoPallet.dir_out = (TrafficRobotUnity.BrDirection)directOut;
                                                infoPallet.jInfoPallet.row = row;
                                                infoPallet.jInfoPallet.line_ord = line_ord;
                                                infoPallet.palletId = palletId;
                                            }


                                            //Compare if pallet H is in this bay
                                            //if (palletId == order.palletId_H)
                                            if (bayToGo == order.palletBay)
                                            {
                                                gotFirstPalletInBay = true;
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                Console.WriteLine("Error at GetInfoOfPalletBuffer");
                return null;
            }
            return infoPallet;
        }
        public JInfoPallet GetInfoOfPalletBuffer(TrafficRobotUnity.PistonPalletCtrl pisCtrl, bool onPlandId = false)
        {
            JInfoPallet infoPallet = new JInfoPallet();
            try
            {

                String collectionData = RequestDataProcedure(order.dataRequest, Global_Object.url + "plan/getListPlanPallet");
                if (collectionData.Length > 0)
                {
                    JArray results = JArray.Parse(collectionData);
                    if (onPlandId)
                    {
                        foreach (var result in results)
                        {
                            int temp_planId = (int)result["planId"];
                            if (temp_planId == order.planId)
                            {
                                foreach (var buffer in result["buffers"])
                                {
                                    //var bufferResults = result["buffers"][0];
                                    if (buffer["pallets"].Count() > 0)
                                    {
                                        var palletInfo = buffer["pallets"][0];

                                        palletId = (int)palletInfo["palletId"];
                                        JObject stuff = JObject.Parse((String)palletInfo["dataPallet"]);

                                        int row = (int)stuff["pallet"]["row"];
                                        int bay = (int)stuff["pallet"]["bay"];
                                        int directMain = (int)stuff["pallet"]["dir_main"];
                                        int directSub = (int)stuff["pallet"]["dir_sub"];
                                        int directOut = (int)stuff["pallet"]["dir_out"];
                                        int line_ord = (int)stuff["pallet"]["line_ord"];
                                        string subline = (string)stuff["pallet"]["hasSubLine"];

                                        infoPallet.pallet = pisCtrl; /* dropdown */
                                        infoPallet.dir_main = (TrafficRobotUnity.BrDirection)directMain;
                                        infoPallet.bay = bay;
                                        infoPallet.hasSubLine = subline; /* yes or no */
                                        infoPallet.dir_sub = (TrafficRobotUnity.BrDirection)directSub; /* right */
                                        infoPallet.dir_out = (TrafficRobotUnity.BrDirection)directOut;
                                        infoPallet.row = row;
                                        infoPallet.line_ord = line_ord;
                                        break;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                                break;
                            }
                        }
                    }
                    else
                    {

                        var result = results[0];

                        //var bufferResults = result["buffers"][0];
                        foreach (var buffer in result["buffers"])
                        {
                            String bufferDataStr = (String)buffer["bufferData"];
                            JObject stuffBData = JObject.Parse(bufferDataStr);
                            bool canOpEdit = (bool)stuffBData["canOpEdit"];
                            if (canOpEdit) // buffer có edit nên bỏ qua lý do bởi buffer có edit nằm gần các máy, áp dụng trong quy trình Buffer -> Machine
                                continue;
                            if (buffer["pallets"].Count() > 0)
                            {
                                var palletInfo = buffer["pallets"][0];
                                palletId = (int)palletInfo["palletId"];
                                JObject stuff = JObject.Parse((String)palletInfo["dataPallet"]);
                                int row = (int)stuff["pallet"]["row"];
                                int bay = (int)stuff["pallet"]["bay"];
                                int directMain = (int)stuff["pallet"]["dir_main"];
                                int directSub = (int)stuff["pallet"]["dir_sub"];
                                int directOut = (int)stuff["pallet"]["dir_out"];
                                int line_ord = (int)stuff["pallet"]["line_ord"];
                                string subline = (string)stuff["pallet"]["hasSubLine"];

                                infoPallet.pallet = pisCtrl; /* dropdown */
                                infoPallet.dir_main = (TrafficRobotUnity.BrDirection)directMain;
                                infoPallet.bay = bay;
                                infoPallet.hasSubLine = subline; /* yes or no */
                                infoPallet.dir_sub = (TrafficRobotUnity.BrDirection)directSub; /* right */
                                infoPallet.dir_out = (TrafficRobotUnity.BrDirection)directOut;
                                infoPallet.row = row;
                                infoPallet.line_ord = line_ord;
                                break;
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }
                }
            }
            catch
            {
                Console.WriteLine("Error at GetInfoOfPalletBuffer");
                return null;
            }
            return infoPallet;
        }
        #endregion
        #endregion
        public Pose GetCheckInBuffer_Return(int bufferId)
        {
            Pose poseTemp = null;
            try
            {

                String collectionData = RequestDataProcedure(order.dataRequest, Global_Object.url + "plan/getListPlanPallet");
                if (collectionData.Length > 0)
                {
                    JArray results = JArray.Parse(collectionData);
                    var result = results[0];

                    foreach (var buffer in result["buffers"])
                    {
                        if ((int)buffer["bufferId"] == bufferId)
                        {
                            if (buffer["pallets"].Count() > 0)
                            {
                                String checkinResults = (String)buffer["bufferCheckIn"];
                                JObject stuff = JObject.Parse(checkinResults);
                                double x = (double)stuff["checkin"]["x"];
                                double y = (double)stuff["checkin"]["y"];
                                double angle = (double)stuff["checkin"]["angle"];
                                poseTemp = new Pose(x, y, angle);
                                planId = order.planId;
                                break;
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }
                    //var bufferResults = result["buffers"][0];
                    //String checkinResults = (String)bufferResults["bufferCheckIn"];
                    //JObject stuff = JObject.Parse(checkinResults);
                    //double x = (double)stuff["checkin"]["x"];
                    //double y = (double)stuff["checkin"]["y"];
                    //double angle = (double)stuff["checkin"]["angle"];
                    //poseTemp = new Pose(x, y, angle);
                    //planId = order.planId;

                }
            }
            catch { Console.WriteLine("Error check in data collection"); }
            return poseTemp;
        }


        public Pose GetAnyPointInBuffer_Return(int bufferId) // đổi 
        {

            Pose poseTemp = null;
            try
            {
                String collectionData = RequestDataProcedure(order.dataRequest, Global_Object.url + "plan/getListPlanPallet");
                if (collectionData.Length > 0)
                {
                    JArray results = JArray.Parse(collectionData);
                    var result = results[0];
                    foreach (var buffer in result["buffers"])
                    {
                        if (bufferId == (int)buffer["bufferId"])
                        {
                            if (buffer["pallets"].Count() > 0)
                            {
                                //var bufferResults = result["buffers"][0];
                                String checkinResults = (String)buffer["bufferCheckIn"];
                                JObject stuff = JObject.Parse(checkinResults);
                                double x = (double)stuff["headpoint"]["x"];
                                double y = (double)stuff["headpoint"]["y"];
                                double angle = (double)stuff["headpoint"]["angle"];
                                poseTemp = new Pose(x, y, angle);
                                break;
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }

                }
            }
            catch
            {
                Console.WriteLine("Get AnyPoint Error");
            }
            return poseTemp;
        }




        public Pose GetFrontLineMachine()
        {
            goalFrontLinePos = order.palletAtMachine.linePos;
            return order.palletAtMachine.linePos;
        }


        public Pose GetCheckInReturn()
        {
            dynamic product = new JObject();
            product.palletStatus = PalletStatus.F.ToString();
            Pose poseTemp = null;
            String collectionData = RequestDataProcedure(product.ToString(), Global_Object.url + "buffer/getListBufferReturn");
            if (collectionData.Length > 0)
            {
                JArray results = JArray.Parse(collectionData);
                // var result = results[0];
                foreach (var buffer in results)
                {
                    if (buffer["pallets"].Count() > 0)
                    {
                        String checkinResults = (String)buffer["bufferCheckIn"];
                        JObject stuff = JObject.Parse(checkinResults);
                        double x = (double)stuff["checkin"]["x"];
                        double y = (double)stuff["checkin"]["y"];
                        double angle = (double)stuff["checkin"]["angle"];
                        poseTemp = new Pose(x, y, angle);
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }


            }
            return poseTemp;
        }

        public Pose GetFrontLineReturn()
        {

            Pose poseTemp = null;
            dynamic product = new JObject();
            product.palletStatus = PalletStatus.P.ToString();
            String collectionData = RequestDataProcedure(product.ToString(), Global_Object.url + "buffer/getListBufferReturn");
            if (collectionData.Length > 0)
            {
                JArray results = JArray.Parse(collectionData);
                // var result = results[0];
                //var bufferResults = result["buffers"][0];
                foreach (var buffer in results)
                {
                    if (buffer["pallets"].Count() > 0)
                    {
                        foreach (var palletInfo in buffer["pallets"])
                        {
                            int palletId = (int)palletInfo["palletId"];
                            if (palletId == order.palletId_F)
                            {
                                JObject stuff = JObject.Parse((String)palletInfo["dataPallet"]);
                                bayId = (int)stuff["bayId"];
                                double x = (double)stuff["line"]["x"];
                                double y = (double)stuff["line"]["y"];
                                double angle = (double)stuff["line"]["angle"];
                                poseTemp = new Pose(x, y, angle);
                                goalFrontLinePos = poseTemp;
                                break;
                            }

                        }
                    }
                    else
                    {
                        continue;
                    }
                }


            }
            return poseTemp;
        }


        /*
         */


        public String GetInfoOfPalletBuffer_Return(TrafficRobotUnity.PistonPalletCtrl pisCtrl, int bufferId)
        {
            JInfoPallet infoPallet = new JInfoPallet();
            try
            {

                String collectionData = RequestDataProcedure(order.dataRequest, Global_Object.url + "buffer/getListBufferReturn"); //"plan/getListPlanPallet"
                if (collectionData.Length > 0)
                {
                    JArray results = JArray.Parse(collectionData);
                    var result = results[0];

                    //var bufferResults = result["buffers"][0];
                    foreach (var buffer in result["buffers"])
                    {
                        if (bufferId == (int)buffer["bufferId"])
                        {
                            if (buffer["pallets"].Count() > 0)
                            {
                                var palletInfo = buffer["pallets"][0];
                                palletId = (int)palletInfo["palletId"];
                                JObject stuff = JObject.Parse((String)palletInfo["dataPallet"]);
                                int row = (int)stuff["pallet"]["row"];
                                int bay = (int)stuff["pallet"]["bay"];
                                int directMain = (int)stuff["pallet"]["dir_main"];
                                int directSub = (int)stuff["pallet"]["dir_sub"];
                                int directOut = (int)stuff["pallet"]["dir_out"];
                                int line_ord = (int)stuff["pallet"]["line_ord"];
                                string subline = (string)stuff["pallet"]["hasSubLine"];

                                infoPallet.pallet = pisCtrl; /* dropdown */
                                infoPallet.dir_main = (TrafficRobotUnity.BrDirection)directMain;
                                infoPallet.bay = bay;
                                infoPallet.hasSubLine = subline; /* yes or no */
                                infoPallet.dir_sub = (TrafficRobotUnity.BrDirection)directSub; /* right */
                                infoPallet.dir_out = (TrafficRobotUnity.BrDirection)directOut;
                                infoPallet.row = row;
                                infoPallet.line_ord = line_ord;
                                break;
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }
                }
            }
            catch
            {
                Console.WriteLine("Error at GetInfoOfPalletBuffer");
                return "";
            }
            return JsonConvert.SerializeObject(infoPallet);
        }

        public String GetInfoOfPalletMachine(TrafficRobotUnity.PistonPalletCtrl pisCtrl)
        {
            JInfoPallet infoPallet = new JInfoPallet();

            infoPallet.pallet = pisCtrl; /* dropdown */
            infoPallet.bay = order.palletAtMachine.bay;
            infoPallet.hasSubLine = "no"; /* no */
            infoPallet.dir_main = (TrafficRobotUnity.BrDirection)order.palletAtMachine.directMain;
            infoPallet.dir_sub = (TrafficRobotUnity.BrDirection)order.palletAtMachine.directSub;
            infoPallet.dir_out = (TrafficRobotUnity.BrDirection)order.palletAtMachine.directOut;
            infoPallet.line_ord = order.palletAtMachine.line_ord;
            infoPallet.row = order.palletAtMachine.row;

            return JsonConvert.SerializeObject(infoPallet);
        }

        public String GetInfoOfPalletReturn(TrafficRobotUnity.PistonPalletCtrl pisCtrl)
        {
            JInfoPallet infoPallet = new JInfoPallet();

            dynamic product = new JObject();
            product.palletStatus = PalletStatus.P.ToString();
            String collectionData = RequestDataProcedure(product.ToString(), Global_Object.url + "buffer/getListBufferReturn");
            if (collectionData.Length > 0)
            {
                JArray results = JArray.Parse(collectionData);
                //  var result = results[0];
                foreach (var buffer in results)
                {
                    if (buffer["pallets"].Count() > 0)
                    {
                        var palletInfo = buffer["pallets"][0];
                        palletId = (int)palletInfo["palletId"];
                        JObject stuff = JObject.Parse((String)palletInfo["dataPallet"]);
                        int row = (int)stuff["pallet"]["row"];
                        int bay = (int)stuff["pallet"]["bay"];
                        int directMain = (int)stuff["pallet"]["dir_main"];
                        int directSub = (int)stuff["pallet"]["dir_sub"];
                        int directOut = (int)stuff["pallet"]["dir_out"];
                        int line_ord = (int)stuff["pallet"]["line_ord"];
                        string hasSubLine = (string)stuff["pallet"]["hasSubLine"];
                        infoPallet.pallet = pisCtrl; /* dropdown */
                        infoPallet.bay = bay;
                        infoPallet.hasSubLine = hasSubLine;
                        infoPallet.dir_main = (TrafficRobotUnity.BrDirection)directMain;
                        infoPallet.dir_sub = (TrafficRobotUnity.BrDirection)directSub;
                        infoPallet.dir_out = (TrafficRobotUnity.BrDirection)directOut;
                        infoPallet.row = row;
                        infoPallet.line_ord = line_ord;
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }



            }
            return JsonConvert.SerializeObject(infoPallet);
        }

        /*    public void UpdatePalletState(PalletStatus palletStatus)
            {
                String url = Global_Object.url + "pallet/updatePalletStatus";
                dynamic product = new JObject();
                product.palletId = palletId;
                product.planId = planId;
                product.palletStatus = palletStatus.ToString();
                product.updUsrId = Global_Object.userLogin;
                var data = RequestDataProcedure( product.ToString(),url);

            }*/
        public void UpdatePalletState(PalletStatus palletStatus, int _palletId, int _planId)
        {
            String url = Global_Object.url + "pallet/updatePalletStatus";
            dynamic product = new JObject();
            product.palletId = _palletId;
            product.planId = _planId;
            product.palletStatus = palletStatus.ToString();
            product.updUsrId = Global_Object.userLogin;
            var data = RequestDataProcedure(product.ToString(), url);

        }
        public void UpdatePalletState(PalletStatus palletStatus)
        {
            String url = Global_Object.url + "pallet/updatePalletStatus";
            dynamic product = new JObject();
            product.palletId = palletId;
            product.planId = order.planId;
            product.palletStatus = palletStatus.ToString();
            product.updUsrId = Global_Object.userLogin;
            var data = RequestDataProcedure(product.ToString(), url);

        }
        protected virtual void ReceiveResponseHandler(String msg) { }
        protected virtual void ErrorHandler(ProcedureMessages.ProcMessage procMessage) { }
    }
}
