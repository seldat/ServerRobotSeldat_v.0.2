using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SeldatMRMS;
using SeldatUnilever_Ver1._02;
using SelDatUnilever_Ver1._00.Communication;
using SelDatUnilever_Ver1._00.Communication.HttpServerRounter;
using static SelDatUnilever_Ver1._00.Communication.HttpServerRounter.HttpProcessor;
using static SelDatUnilever_Ver1._00.Management.DeviceManagement.DeviceItem;

namespace SelDatUnilever_Ver1._00.Management.DeviceManagement
{
    public class DeviceRegistrationService : HttpServer
    {

        public MainWindow mainWindow;
        public List<DeviceItem> deviceItemList;

        public DeviceRegistrationService(int port) : base(port)
        {
            deviceItemList = new List<DeviceItem>();
            CreateFolder();
        }

        public void RegistryMainWindow (MainWindow mainWindow)
        {
            this.mainWindow = new MainWindow();
            this.mainWindow = mainWindow;
        }

        public void RemoveDeviceItem(String userName)
        {
            if (deviceItemList.Count > 0)
            {
                deviceItemList.RemoveAt(deviceItemList.FindIndex(e => e.userName == userName));
            }
        }
        public void CreateFolder()
        {
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            // Combine the base folder with your specific folder....
            string specificFolder = Path.Combine(folder, "SelDat\\DeviceItems");
            // CreateDirectory will check if folder exists and, if not, create it.
            // If folder exists then CreateDirectory will do nothing.
            Directory.CreateDirectory(specificFolder);
            if (!System.IO.Directory.Exists(specificFolder))
            {
                System.IO.Directory.CreateDirectory(specificFolder);
            }
        }
        public int HasDeviceItemAt(String userName)
        {
            return deviceItemList.FindIndex(e => e.userName.Equals(userName));
            //
        }
        public DeviceItem FindDeviceItem(String userName)
        {
            return deviceItemList.Find(e => e.userName == userName);
        }
        public override void handlePOSTRequest(HttpProcessor p, StreamReader inputData)
        {
            StatusOrderResponse statusOrder = null;
            if (Global_Object.onAcceptDevice)
            {
                String data = inputData.ReadToEnd();
                JObject results = JObject.Parse(data);
                String userName = (String)results["userName"];
               
                if (HasDeviceItemAt(userName) >= 0)
                {
                    statusOrder = FindDeviceItem(userName).ParseData(data);

                }
                else
                {
                    DeviceItem deviceItem = new DeviceItem(this.mainWindow);
                    deviceItem.userName = userName;
                    statusOrder = deviceItem.ParseData(data);
                    deviceItemList.Add(deviceItem);

                }
            }
            else
            {
                statusOrder.status = (int)StatusOrderResponseCode.ORDER_STATUS_RESPONSE_ERROR_DATA;
            }
            if(statusOrder.status==(int)StatusOrderResponseCode.ORDER_STATUS_RESPONSE_SUCCESS)
            {
                p.handlePOSTResponse(p, StatusHttPResponse.STATUS_MESSAGE_SUCCESS);
            }
            else if (statusOrder.status == (int)StatusOrderResponseCode.ORDER_STATUS_RESPONSE_ERROR_DATA)
            {
                p.handlePOSTResponse(p, StatusHttPResponse.STATUS_MESSAGE_ERROR);
            }
            else if (statusOrder.status == (int)StatusOrderResponseCode.ORDER_STATUS_RESPONSE_NOACCEPTED)
            {
                p.handlePOSTResponse(p, StatusHttPResponse.STATUS_MESSAGE_NOACCEPTED);
            }
            else if (statusOrder.status == (int)StatusOrderResponseCode.ORDER_STATUS_DOOR_BUSY)
            {
                p.handlePOSTResponse(p, StatusHttPResponse.STATUS_MESSAGE_DOOR_BUSY);
            }
        }

        public List<DeviceItem> GetDeviceItemList()
        {
            return deviceItemList;
        }
       
        public bool SaveDeviceOrderList()
        {
            foreach(DeviceItem device in deviceItemList)
            {
                if (!SaveFileOrders(device))
                    return false;
            }
            return true;
        }
        public bool SaveFileOrders(DeviceItem device)
        {
            List<OrderItem> listCol = new List<OrderItem>();
            foreach (OrderItem item in device.OrderedItemList)
            {
                if(item.status==StatusOrderResponseCode.DELIVERING)
                {
                    return false;
                }
                else if(item.status == StatusOrderResponseCode.PENDING)
                {
                    device.RemoveCallBack(item);
                    listCol.Add(item);

                }
            }
            String path = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "OrderStore.txt");
            using (StreamWriter fs = File.CreateText(path))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(fs, listCol);
            }
            return true;
        }

        public List<OrderItem> LoadFileOrders()
        {
            List<OrderItem> returnList = new List<OrderItem>();
            String path = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "OrderStore.txt");
            using (StreamReader sr = File.OpenText(path))
            {
                string s = "";
                while ((s = sr.ReadLine()) != null)
                {
                    Console.WriteLine(s);
                    dynamic response = JsonConvert.DeserializeObject(s);
                    returnList = response.ToObject<List<OrderItem>>();
                }
            }
            return returnList;
        }
    }
}