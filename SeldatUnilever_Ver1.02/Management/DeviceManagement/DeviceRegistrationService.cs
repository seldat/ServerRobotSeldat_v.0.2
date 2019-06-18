﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
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
        public void SaveDeviceOrder()
        {
            try
            {
                String path = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "SavePendingOrder.txt");
                if (!File.Exists(path))
                {
                    var myfile = File.Create(path);
                }
                if(deviceItemList.Count>0)
                {
                    foreach(DeviceItem dev in deviceItemList)
                    {
                        if(dev.OrderedItemList.Count>0)
                        {
                            foreach(OrderItem ord in dev.OrderedItemList)
                            {
                                if(ord.status == StatusOrderResponseCode.PENDING)
                                {
                                  //  File.AppendAllText(path, ord. Environment.NewLine);
                                }
                            }
                        }
                    }
                }
                
            }
            catch { }
        }
    }
}