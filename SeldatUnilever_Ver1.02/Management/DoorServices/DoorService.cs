﻿using SelDatUnilever_Ver1._00.Management;
using SelDatUnilever_Ver1._00.Management.ComSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using static SeldatMRMS.Management.RobotManagent.RobotUnityControl;

namespace DoorControllerService
{
    public class DoorService : TranferData
    {
        private enum CmdDoor
        {
            CMD_GET_STATUS_DOOR = 0x70, /*0x70 */
            RES_GET_STATUS_DOOR, /*0x71 */
            CMD_OPEN_DOOR_PRESS, /*0x72 */
            RES_OPEN_DOOR_PRESS, /*0x73 */
            CMD_OPEN_DOOR_RELEASE, /*0x74 */
            RES_OPEN_DOOR_RELEASE, /*0x75 */
            CMD_CLOSE_DOOR_PRESS, /*0x76 */
            RES_CLOSE_DOOR_PRESS, /*0x77 */
            CMD_CLOSE_DOOR_RELEASE, /*0x78 */
            RES_CLOSE_DOOR_RELEASE, /*0x79 */
            CMD_ON_LAMP, /*0x7A */
            RES_ON_LAMP, /*0x7B */
            CMD_OFF_LAMP, /*0x7C */
            RES_OFF_LAMP, /*0x7D */
        }
        public enum DoorId
        {
            DOOR_MEZZAMINE_UP_NEW = 1, /* 0x01 */
            DOOR_MEZZAMINE_UP = 2,  /* 0x02 */
            DOOR_MEZZAMINE_RETURN, /* 0x03 */
            DOOR_ELEVATOR, /* 0x04 */
        }

        public enum DoorCmdRq
        {
            DOOR_OPEN = 0x01,
            DOOR_CLOSE,
            LAMP_ON,
            LAMP_OFF
        }
        public enum DoorType
        {
            DOOR_FRONT = 0x01,
            DOOR_BACK /* 0x02 */
        }

        public enum DoorStatus
        {
            DOOR_UNKNOW = 0x00,
            DOOR_CLOSE, /* 0x01 */
            DOOR_OPEN, /* 0x02 */
            DOOR_ERROR, /* 0x04 */
        }

        public enum StateCtrl
        {
            DOOR_ST_IDLE = 0x00,
            DOOR_ST_OPEN,
            DOOR_ST_WAITTING_OPEN,
            DOOR_ST_OPEN_SUCCESS,
            DOOR_ST_CLOSE,
            DOOR_ST_WAITTING_CLOSE,
            DOOR_ST_CLOSE_SUCCESS,
            LAMP_DOOR_ON,
            LAMP_DOOR_OFF
        }
        public enum RetState
        {
            DOOR_CTRL_SUCCESS = 0,
            DOOR_CTRL_WAITTING,
            DOOR_CTRL_ERROR
        }
        public class DoorInfoConfig : NotifyUIBase
        {
            private String _Name;
            public String Name { get => _Name; set { _Name = value; RaisePropertyChanged("Name"); } }
            private String _Ip;
            public String Ip { get => _Ip; set { _Ip = value; RaisePropertyChanged("Ip"); } }
            private Int32 _Port;
            public Int32 Port { get => _Port; set { _Port = value; RaisePropertyChanged("Port"); } }
            private DoorId _Id;
            public DoorId Id { get => _Id; set { _Id = value; RaisePropertyChanged("Id"); } }
            public Pose PointCheckInGate;
            private String _PointCheckInGateStr;
            public String PointCheckInGateStr { get => _PointCheckInGateStr; set { _PointCheckInGateStr = value; RaisePropertyChanged("PointCheckInGateStr"); } }
            public Pose PointFrontLine;
            public Pose PointFrontLineInv;
            private String _PointFrontLineStr;
            public String PointFrontLineStr { get => _PointFrontLineStr; set { _PointFrontLineStr = value; RaisePropertyChanged("PointFrontLineStr"); } }
            
            private String _PointFrontLineStrInv;
            public String PointFrontLineStrInv { get => _PointFrontLineStrInv; set { _PointFrontLineStrInv = value; RaisePropertyChanged("PointFrontLineStr"); } }


            private String _infoPallet;
            private String _infoPalletInv;
            public String infoPallet { get => _infoPallet; set { _infoPallet = value; RaisePropertyChanged("infoPallet"); } }
            public String infoPalletInv { get => _infoPalletInv; set { _infoPalletInv = value; RaisePropertyChanged("infoPalletInv"); } }

            public void ParsePointCheckInGateValue(String value)
            {
                try
                {
                    double xx = double.Parse(value.Split(',')[0]);
                    double yy = double.Parse(value.Split(',')[1]);
                    double angle = double.Parse(value.Split(',')[2]);
                    PointCheckInGate = new Pose(xx, yy, angle);
                }
                catch { }
            }

            public void ParsePointFrontLineValue(String value)
            {
                try
                {
                    double xx = double.Parse(value.Split(',')[0]);
                    double yy = double.Parse(value.Split(',')[1]);
                    double angle = double.Parse(value.Split(',')[2]);
                    PointFrontLine = new Pose(xx, yy, angle);
                }
                catch { }
            }
            public void ParsePointFrontLineValueInv(String value)
            {
                try
                {
                    double xx = double.Parse(value.Split(',')[0]);
                    double yy = double.Parse(value.Split(',')[1]);
                    double angle = double.Parse(value.Split(',')[2]);
                    PointFrontLineInv = new Pose(xx, yy, angle);
                }
                catch { }
            }
        }

        public class cmdRqDoor
        {
            public DoorType dType;
            public DoorCmdRq cmdRq;
            public long timePre;
        }

        public DoorInfoConfig config;
        private Thread doorServiceThread;
        private StateCtrl stateCtrlDoor;
        private DoorStatus doorFrontStatus;
        private DoorStatus doorBackStatus;
        private const UInt32 TIME_OUT_WAIT_DOOR = 9000;
        //private const UInt32 TIME_OUT_PRESS_BUTTON = 1500;
        private const long TIMEOUT_REMOVE_COMMAND = 300000000; //30 second
        private bool doorBusy;
        private bool kProcess = true;
        private bool waitOpenBack = false;

        public void resetWaitOpenBack()
        {
            waitOpenBack = false;
        }

        private List<cmdRqDoor> listCmdRqCtrl = new List<cmdRqDoor>();

        public bool getDoorBusy()   
        {
            return doorBusy;
        }
        public void setDoorBusy(bool bs)
        {
            this.doorBusy = bs;
        }

        public DoorService(DoorInfoConfig cf) : base(cf.Ip, cf.Port)
        {
            config = cf;
            doorServiceThread = new Thread(this.doorCtrlProcess);
            doorServiceThread.Start(this);
            stateCtrlDoor = StateCtrl.DOOR_ST_IDLE;
            this.doorBusy = false;
        }

        private void AddRqToList(DoorType dType, DoorCmdRq dRq)
        {
            cmdRqDoor valTamp = new cmdRqDoor();
            DateTime currentDate = DateTime.Now;
            valTamp.timePre = currentDate.Ticks;
            valTamp.cmdRq = dRq;
            valTamp.dType = dType;
            listCmdRqCtrl.Add(valTamp);
        }
        public void LampSetStateOn(DoorType dt)
        {
            AddRqToList(dt, DoorCmdRq.LAMP_ON);
        }

        public void LampSetStateOff(DoorType dt)
        {
            AddRqToList(dt, DoorCmdRq.LAMP_OFF);
        }

        public void openDoor(DoorType dt)
        {
            AddRqToList(dt, DoorCmdRq.DOOR_OPEN);
        }

        public void closeDoor(DoorType dt)
        {
            AddRqToList(dt, DoorCmdRq.DOOR_CLOSE);
        }

        public void removeListCtrlDoorFront()
        {
            if (listCmdRqCtrl.Count > 0)
            {
                try
                {
                    foreach (var item in listCmdRqCtrl)
                    {
                        if ((item.dType == DoorType.DOOR_FRONT) && ((item.cmdRq == DoorCmdRq.DOOR_OPEN) || (item.cmdRq == DoorCmdRq.DOOR_CLOSE)))
                        {
                            listCmdRqCtrl.Remove(item);
                        }
                        if ((item.dType == DoorType.DOOR_BACK) && ((item.cmdRq == DoorCmdRq.LAMP_ON) || (item.cmdRq == DoorCmdRq.LAMP_OFF)))
                        {
                            listCmdRqCtrl.Remove(item);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        public void removeListCtrlDoorBack()
        {
            if (listCmdRqCtrl.Count > 0)
            {
                try
                {
                    foreach (var item in listCmdRqCtrl)
                    {
                        if ((item.dType == DoorType.DOOR_BACK) && ((item.cmdRq == DoorCmdRq.DOOR_OPEN) || (item.cmdRq == DoorCmdRq.DOOR_CLOSE)))
                        {
                            listCmdRqCtrl.Remove(item);
                        }
                        if ((item.dType == DoorType.DOOR_FRONT) && ((item.cmdRq == DoorCmdRq.LAMP_ON) || (item.cmdRq == DoorCmdRq.LAMP_OFF)))
                        {
                            listCmdRqCtrl.Remove(item);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        public void removeItemListCtrlDoor(cmdRqDoor item)
        {
            if (listCmdRqCtrl.Count > 0)
            {
                try
                {
                    listCmdRqCtrl.Remove(item);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        public void ResetDoor()
        {
            this.removeListCtrlDoorBack();
            this.removeListCtrlDoorFront();
            kProcess = false;
            stateCtrlDoor = StateCtrl.DOOR_ST_IDLE;
            //this.OpenRelease(DoorType.DOOR_BACK);
            //this.OpenRelease(DoorType.DOOR_FRONT);
            //this.CloseRelease(DoorType.DOOR_BACK);
            //this.CloseRelease(DoorType.DOOR_FRONT);
        }
        public RetState checkOpen(DoorType type)
        {
            switch (type)
            {
                case DoorType.DOOR_FRONT:
                    if (this.doorFrontStatus == DoorStatus.DOOR_OPEN)
                    {
                        return RetState.DOOR_CTRL_SUCCESS;
                    }
                    break;
                case DoorType.DOOR_BACK:
                    if (this.doorBackStatus == DoorStatus.DOOR_OPEN)
                    {
                        return RetState.DOOR_CTRL_SUCCESS;
                    }
                    else if (this.doorBackStatus == DoorStatus.DOOR_ERROR)
                    {
                        return RetState.DOOR_CTRL_ERROR;
                    }
                    break;
                default:
                    break;
            }
            return RetState.DOOR_CTRL_WAITTING;
        }

        public RetState checkClose(DoorType type)
        {
            switch (type)
            {
                case DoorType.DOOR_FRONT:
                    if (this.doorFrontStatus == DoorStatus.DOOR_CLOSE)
                    {
                        return RetState.DOOR_CTRL_SUCCESS;
                    }
                    break;
                case DoorType.DOOR_BACK:
                    if (this.doorBackStatus == DoorStatus.DOOR_CLOSE)
                    {
                        return RetState.DOOR_CTRL_SUCCESS;
                    }
                    break;
                default:
                    break;
            }
            return RetState.DOOR_CTRL_WAITTING;
        }

        public DoorStatus getStatusDoor(DoorType type)
        {
            if (type == DoorType.DOOR_FRONT)
            {
                return doorFrontStatus;
            }
            return doorBackStatus;
        }
        public void doorCtrlProcess(object ojb)
        {
            DataReceive status = new DataReceive();
            Stopwatch elapsedTimeFront = new Stopwatch();
            elapsedTimeFront.Start();
            Stopwatch elapsedTimeReleaseButton = new Stopwatch();
            elapsedTimeReleaseButton.Start();
            kProcess = true;
            while (true)
            {
                if (listCmdRqCtrl.Count > 0)
                {
                    //if (this.rb != null)
                    Console.WriteLine("Doorctrl listCmdRqCtrl.Count : " + listCmdRqCtrl.Count);
                    cmdRqDoor resCmd = listCmdRqCtrl[0];
                    if ((DateTime.Now.Ticks - resCmd.timePre) > TIMEOUT_REMOVE_COMMAND)
                    {
                        Console.WriteLine("Remove cmd time out : " + resCmd.dType + " " + resCmd.cmdRq);
                        removeItemListCtrlDoor(resCmd);
                        continue;
                    }
                    kProcess = true;
                    if (resCmd.cmdRq == DoorCmdRq.DOOR_OPEN)
                    {
                        this.stateCtrlDoor = StateCtrl.DOOR_ST_OPEN;
                    }
                    else if (resCmd.cmdRq == DoorCmdRq.DOOR_CLOSE)
                    {
                        this.stateCtrlDoor = StateCtrl.DOOR_ST_CLOSE;
                    }
                    else if (resCmd.cmdRq == DoorCmdRq.LAMP_ON)
                    {
                        this.stateCtrlDoor = StateCtrl.LAMP_DOOR_ON;
                    }
                    else if (resCmd.cmdRq == DoorCmdRq.LAMP_OFF)
                    {
                        this.stateCtrlDoor = StateCtrl.LAMP_DOOR_OFF;
                    }
                    else
                    {
                        Console.WriteLine("Remove not command");
                        //if (this.rb != null)
                        Console.WriteLine("Doorctrl Remove not command");
                        removeItemListCtrlDoor(resCmd);
                        Thread.Sleep(100);
                        continue;
                    }
                    while (kProcess)
                    {
                        switch (this.stateCtrlDoor)
                        {
                            case StateCtrl.DOOR_ST_IDLE:
                                //if (this.rb != null)
                                Console.WriteLine("StateCtrl.DOOR_ST_IDLE");
                                break;
                            case StateCtrl.DOOR_ST_OPEN:
                                try
                                {
                                    //if (this.rb != null)
                                    Console.WriteLine("StateCtrl.DOOR_ST_OPEN" + resCmd.dType);
                                    if (resCmd.dType == DoorType.DOOR_FRONT)
                                    {
                                        if (true == this.GetStatus(ref status, DoorType.DOOR_BACK))
                                        {
                                            if (status.data[0] == (byte)DoorStatus.DOOR_OPEN)
                                            {
                                                //if (this.rb != null)
                                                Console.WriteLine("StateCtrl.DOOR_ST_OPEN" + resCmd.dType + ':' + DoorType.DOOR_BACK + "is open");
                                                cmdRqDoor varTamp = new cmdRqDoor();
                                                DateTime currentDate = DateTime.Now;
                                                varTamp.timePre = currentDate.Ticks;
                                                varTamp.cmdRq = DoorCmdRq.DOOR_CLOSE;
                                                varTamp.dType = DoorType.DOOR_BACK;
                                                listCmdRqCtrl[0].timePre = currentDate.Ticks;
                                                listCmdRqCtrl.Insert(0, varTamp);
                                                kProcess = false;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            if (this.rb != null)
                                                this.rb.ShowText("Get status door failed");
                                        }
                                        Thread.Sleep(50);
                                    }
                                    else if (resCmd.dType == DoorType.DOOR_BACK)
                                    {
                                        if (true == this.GetStatus(ref status, DoorType.DOOR_FRONT))
                                        {
                                            if (status.data[0] == (byte)DoorStatus.DOOR_OPEN)
                                            {
                                                //if (this.rb != null)
                                                Console.WriteLine("StateCtrl.DOOR_ST_OPEN" + resCmd.dType + ':' + DoorType.DOOR_FRONT + "is open");
                                                removeItemListCtrlDoor(resCmd);
                                                waitOpenBack = true;
                                                doorBackStatus = DoorStatus.DOOR_ERROR;
                                                kProcess = false;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            if (this.rb != null)
                                                this.rb.ShowText("Get status door failed");
                                        }
                                        Thread.Sleep(50);
                                    }
                                    if (true == this.GetStatus(ref status, resCmd.dType))
                                    {
                                        if (status.data[0] == (byte)DoorStatus.DOOR_OPEN)
                                        {
                                            //if (this.rb != null)
                                            Console.WriteLine("StateCtrl.DOOR_ST_OPEN" + resCmd.dType + ':' + "DOOR_ST_OPEN_SUCCESS");
                                            this.stateCtrlDoor = StateCtrl.DOOR_ST_OPEN_SUCCESS;
                                            if (resCmd.dType == DoorType.DOOR_FRONT)
                                            {
                                                doorFrontStatus = DoorStatus.DOOR_OPEN;
                                            }
                                            else
                                            {
                                                doorBackStatus = DoorStatus.DOOR_OPEN;
                                            }
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        if (this.rb != null)
                                            this.rb.ShowText("Get status door failed");
                                    }
                                    Thread.Sleep(50);
                                    Console.WriteLine("DOOR_ST_OPEN");
                                    if (this.OpenPress(resCmd.dType))
                                    {
                                        elapsedTimeFront.Restart();
                                        elapsedTimeReleaseButton.Restart();
                                        this.stateCtrlDoor = StateCtrl.DOOR_ST_WAITTING_OPEN;
                                        //if (this.rb != null)
                                        Console.WriteLine("StateCtrl.DOOR_ST_OPEN" + resCmd.dType + ':' + "DOOR_ST_WAITTING_OPEN_DOOR");
                                        Console.WriteLine("DOOR_ST_WAITTING_OPEN_DOOR");
                                    }
                                }
                                catch (Exception e)
                                {
                                    //if (this.rb != null)
                                    Console.WriteLine(e.ToString());
                                }
                                break;
                            case StateCtrl.DOOR_ST_WAITTING_OPEN:
                                try
                                {
                                    //if (elapsedTimeReleaseButton.ElapsedMilliseconds >= TIME_OUT_PRESS_BUTTON)
                                    //{
                                    //    if (this.OpenRelease(resCmd.dType))
                                    //    {
                                    //        Console.WriteLine("OpenRelease(DoorType.DOOR) success");
                                    //        //if (this.rb != null)
                                    //        Console.WriteLine("StateCtrl.DOOR_ST_WAITTING_OPEN" + resCmd.dType + ':' + "OpenRelease(DoorType.DOOR) success");
                                    //        elapsedTimeReleaseButton.Reset();
                                    //    }
                                    //    else
                                    //    {
                                    //        Console.WriteLine("OpenRelease(DoorType.DOOR) failed");
                                    //        //if (this.rb != null)
                                    //        Console.WriteLine("StateCtrl.DOOR_ST_WAITTING_OPEN" + resCmd.dType + ':' + "OpenRelease(DoorType.DOOR) failed");
                                    //        elapsedTimeReleaseButton.Restart();
                                    //    }
                                    //    Thread.Sleep(50);
                                    //}
                                    if (elapsedTimeFront.ElapsedMilliseconds >= TIME_OUT_WAIT_DOOR)
                                    {
                                        elapsedTimeFront.Reset();
                                        this.stateCtrlDoor = StateCtrl.DOOR_ST_OPEN;
                                        //Console.WriteLine("TIME_OUT_WAIT_OPEN_DOOR");
                                        if (this.rb != null)
                                            this.rb.ShowText("TIME_OUT_WAIT_OPEN_DOOR");
                                        Console.WriteLine("StateCtrl.DOOR_ST_WAITTING_OPEN" + resCmd.dType + ':' + "TIME_OUT_WAIT_OPEN_DOOR");
                                    }
                                    else
                                    {
                                        if (true == this.GetStatus(ref status, resCmd.dType))
                                        {
                                            if (status.data[0] == (byte)DoorStatus.DOOR_OPEN)
                                            {
                                                this.stateCtrlDoor = StateCtrl.DOOR_ST_OPEN_SUCCESS;
                                                if (resCmd.dType == DoorType.DOOR_FRONT)
                                                {
                                                    doorFrontStatus = DoorStatus.DOOR_OPEN;
                                                }
                                                else
                                                {
                                                    doorBackStatus = DoorStatus.DOOR_OPEN;
                                                }
                                                elapsedTimeFront.Reset();
                                                Console.WriteLine("DOOR_ST_OPEN_SUCCESS");
                                                //if (this.rb != null)
                                                Console.WriteLine("StateCtrl.DOOR_ST_WAITTING_OPEN" + resCmd.dType + ':' + "DOOR_ST_OPEN_SUCCESS");
                                            }
                                        }
                                        else
                                        {
                                            if (this.rb != null)
                                                this.rb.ShowText("Get status door failed");
                                        }
                                    }

                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                    //if (this.rb != null)
                                    Console.WriteLine(e.ToString());
                                }
                                break;
                            case StateCtrl.DOOR_ST_OPEN_SUCCESS:
                                //if (this.rb != null)
                                Console.WriteLine("StateCtrl.DOOR_ST_OPEN_SUCCESS" + resCmd.dType + ':' + "Remove list");
                                removeItemListCtrlDoor(resCmd);
                                kProcess = false;
                                break;
                            case StateCtrl.DOOR_ST_CLOSE:
                                try
                                {
                                    if (true == this.GetStatus(ref status, resCmd.dType))
                                    {
                                        if (status.data[0] == (byte)DoorStatus.DOOR_CLOSE)
                                        {
                                            //if (this.rb != null)
                                            Console.WriteLine("StateCtrl.DOOR_ST_CLOSE" + resCmd.dType + ':' + "DOOR_ST_CLOSE_SUCCESS");
                                            this.stateCtrlDoor = StateCtrl.DOOR_ST_CLOSE_SUCCESS;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        if (this.rb != null)
                                            this.rb.ShowText("Get status door failed");
                                    }
                                    Console.WriteLine("DOOR_ST_CLOSE_DOOR");
                                    Thread.Sleep(50);
                                    if (this.ClosePress(resCmd.dType))
                                    {
                                        elapsedTimeFront.Restart();
                                        elapsedTimeReleaseButton.Restart();
                                        this.stateCtrlDoor = StateCtrl.DOOR_ST_WAITTING_CLOSE;
                                        //if (this.rb != null)
                                        Console.WriteLine("StateCtrl.DOOR_ST_CLOSE" + resCmd.dType + ':' + "DOOR_ST_WAITTING_CLOSE_DOOR");
                                        Console.WriteLine("DOOR_ST_WAITTING_CLOSE_DOOR");
                                    }
                                }
                                catch (Exception e)
                                {
                                    //if (this.rb != null)
                                    Console.WriteLine(e.ToString());
                                }

                                break;
                            case StateCtrl.DOOR_ST_WAITTING_CLOSE:
                                try
                                {
                                    //if (elapsedTimeReleaseButton.ElapsedMilliseconds >= TIME_OUT_PRESS_BUTTON)
                                    //{
                                    //    if (this.CloseRelease(resCmd.dType))
                                    //    {
                                    //        //if (this.rb != null)
                                    //        Console.WriteLine("StateCtrl.DOOR_ST_WAITTING_CLOSE" + resCmd.dType + ':' + "this.CloseRelease(DoorType.DOOR)) success");
                                    //        Console.WriteLine("this.CloseRelease(DoorType.DOOR)) success");
                                    //        elapsedTimeReleaseButton.Reset();
                                    //    }
                                    //    else
                                    //    {
                                    //        //if (this.rb != null)
                                    //        Console.WriteLine("StateCtrl.DOOR_ST_WAITTING_CLOSE" + resCmd.dType + ':' + "this.CloseRelease(DoorType.DOOR)) failed");
                                    //        Console.WriteLine("this.CloseRelease(DoorType.DOOR)) failed");
                                    //        elapsedTimeReleaseButton.Restart();
                                    //    }
                                    //    Thread.Sleep(50);
                                    //}
                                    if (elapsedTimeFront.ElapsedMilliseconds >= TIME_OUT_WAIT_DOOR)
                                    {
                                        elapsedTimeFront.Restart();
                                        this.stateCtrlDoor = StateCtrl.DOOR_ST_CLOSE;
                                        if (this.rb != null)
                                            this.rb.ShowText("TIME_OUT_WAIT_CLOSE_DOOR");
                                        Console.WriteLine("StateCtrl.DOOR_ST_WAITTING_CLOSE" + resCmd.dType + ':' + "TIME_OUT_WAIT_CLOSE_DOOR");
                                        
                                    }
                                    else
                                    {

                                        if (true == this.GetStatus(ref status, resCmd.dType))
                                        {
                                            if (status.data[0] == (byte)DoorStatus.DOOR_CLOSE)
                                            {
                                                this.stateCtrlDoor = StateCtrl.DOOR_ST_CLOSE_SUCCESS;
                                                if (resCmd.dType == DoorType.DOOR_FRONT)
                                                {
                                                    doorFrontStatus = DoorStatus.DOOR_CLOSE;
                                                }
                                                else
                                                {
                                                    doorBackStatus = DoorStatus.DOOR_CLOSE;
                                                }
                                                elapsedTimeFront.Reset();
                                                //if (this.rb != null)
                                                Console.WriteLine("StateCtrl.DOOR_ST_WAITTING_CLOSE" + resCmd.dType + ':' + "DOOR_ST_CLOSE_DOOR_SUCCESS");
                                                Console.WriteLine("DOOR_ST_CLOSE_DOOR_SUCCESS");
                                            }
                                        }
                                        else
                                        {
                                            if (this.rb != null)
                                                this.rb.ShowText("Get status door failed");
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    //if (this.rb != null)
                                    Console.WriteLine(e.ToString());
                                }
                                break;
                            case StateCtrl.DOOR_ST_CLOSE_SUCCESS:
                                //if (this.rb != null)
                                Console.WriteLine("StateCtrl.DOOR_ST_CLOSE_SUCCESS" + resCmd.dType + ':' + "Remove list");
                                removeItemListCtrlDoor(resCmd);
                                kProcess = false;
                                break;
                            case StateCtrl.LAMP_DOOR_ON:
                                try
                                {
                                    if (true == this.LampOn(resCmd.dType))
                                    {
                                        removeItemListCtrlDoor(resCmd);
                                        kProcess = false;
                                        //if (this.rb != null)
                                        Console.WriteLine("StateCtrl.LAMP_DOOR_ON" + resCmd.dType + ':' + "Lamp on success");
                                        Console.WriteLine(resCmd.dType + "Lamp on success");
                                    }
                                    else
                                    {
                                        //if (this.rb != null)
                                        Console.WriteLine("StateCtrl.LAMP_DOOR_ON" + resCmd.dType + ':' + "Lamp on failed");
                                    }
                                }
                                catch (Exception e)
                                {
                                    //if (this.rb != null)
                                    Console.WriteLine(e.ToString());
                                }
                                break;
                            case StateCtrl.LAMP_DOOR_OFF:
                                try
                                {
                                    if (true == this.LampOff(resCmd.dType))
                                    {
                                        removeItemListCtrlDoor(resCmd);
                                        kProcess = false;
                                        //if (this.rb != null)
                                        Console.WriteLine("StateCtrl.LAMP_DOOR_OFF" + resCmd.dType + ':' + "Lamp off success");
                                        Console.WriteLine(resCmd.dType + "Lamp off success");
                                    }
                                    else
                                    {
                                        //if (this.rb != null)
                                        Console.WriteLine("StateCtrl.LAMP_DOOR_OFF" + resCmd.dType + ':' + "Lamp off failed");
                                    }
                                }
                                catch (Exception e)
                                {
                                    //if (this.rb != null)
                                    Console.WriteLine(e.ToString());
                                }
                                break;
                            default:
                                break;
                        }
                        Thread.Sleep(50);
                    }
                }
                else
                {
                    if ((true == this.doorBusy)&&(this.waitOpenBack == false))
                    {
                        Thread.Sleep(350);
                        if (true == this.GetStatus(ref status, DoorType.DOOR_BACK))
                        {
                            doorBackStatus = (DoorStatus)status.data[0];
                        }
                        //Thread.Sleep(250);
                        //if (true == this.GetStatus(ref status, DoorType.DOOR_FRONT))
                        //{
                        //    doorFrontStatus = (DoorStatus)status.data[0];
                        //}
                    }
                }
                Thread.Sleep(150);
            }
        }

        private bool GetStatus(ref DataReceive data, DoorType id)
        {
            //Console.WriteLine(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss tt") + "GetStatus Door :" + id);
            bool ret = false;
            byte[] dataSend = new byte[7];

            dataSend[0] = 0xFA;
            dataSend[1] = 0x55;
            dataSend[2] = (byte)CmdDoor.CMD_GET_STATUS_DOOR;
            dataSend[3] = 0x05;
            dataSend[4] = 0x00;
            dataSend[5] = (byte)id;
            dataSend[6] = CalChecksum(dataSend, 4);
            ret = this.Tranfer(dataSend, ref data);
            //if (this.rb != null)
                //this.rb.ShowText("Status door : " + id + ": " + (DoorStatus)data.data[0]);
            //Console.WriteLine(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss tt") + "Status door : " + id + ": " + (DoorStatus)data.data[0]);
            return ret;
        }

        private bool OpenPress(DoorType id)
        {
            //Console.WriteLine(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss tt") + "OpenPress Door");
            bool ret = false;
            byte[] dataSend = new byte[7];

            dataSend[0] = 0xFA;
            dataSend[1] = 0x55;
            dataSend[2] = (byte)CmdDoor.CMD_OPEN_DOOR_PRESS;
            dataSend[3] = 0x05;
            dataSend[4] = 0x00;
            dataSend[5] = (byte)id;
            dataSend[6] = CalChecksum(dataSend, 4);
            ret = this.Tranfer(dataSend);
            return ret;
        }

        private bool OpenRelease(DoorType id)
        {
            //Console.WriteLine(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss tt") + "OpenRelease Door");
            bool ret = false;
            byte[] dataSend = new byte[7];

            dataSend[0] = 0xFA;
            dataSend[1] = 0x55;
            dataSend[2] = (byte)CmdDoor.CMD_OPEN_DOOR_RELEASE;
            dataSend[3] = 0x05;
            dataSend[4] = 0x00;
            dataSend[5] = (byte)id;
            dataSend[6] = CalChecksum(dataSend, 4);
            ret = this.Tranfer(dataSend);
            return ret;
        }


        private bool ClosePress(DoorType id)
        {
            //Console.WriteLine(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss tt") + "ClosePress Door");
            bool ret = false;
            byte[] dataSend = new byte[7];

            dataSend[0] = 0xFA;
            dataSend[1] = 0x55;
            dataSend[2] = (byte)CmdDoor.CMD_CLOSE_DOOR_PRESS;
            dataSend[3] = 0x05;
            dataSend[4] = 0x00;
            dataSend[5] = (byte)id;
            dataSend[6] = CalChecksum(dataSend, 4);
            ret = this.Tranfer(dataSend);
            return ret;
        }

        private bool CloseRelease(DoorType id)
        {
            //Console.WriteLine(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss tt") + "CloseRelease Door");
            bool ret = false;
            byte[] dataSend = new byte[7];

            dataSend[0] = 0xFA;
            dataSend[1] = 0x55;
            dataSend[2] = (byte)CmdDoor.CMD_CLOSE_DOOR_RELEASE;
            dataSend[3] = 0x05;
            dataSend[4] = 0x00;
            dataSend[5] = (byte)id;
            dataSend[6] = CalChecksum(dataSend, 4);
            ret = this.Tranfer(dataSend);
            return ret;
        }
        private bool LampOn(DoorType id)
        {
            //Console.WriteLine(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss tt") + "LampOn Door");
            bool ret = false;
            byte[] dataSend = new byte[7];

            dataSend[0] = 0xFA;
            dataSend[1] = 0x55;
            dataSend[2] = (byte)CmdDoor.CMD_ON_LAMP;
            dataSend[3] = 0x05;
            dataSend[4] = 0x00;
            dataSend[5] = (byte)id;
            dataSend[6] = CalChecksum(dataSend, 4);
            ret = this.Tranfer(dataSend);
            return ret;
        }

        private bool LampOff(DoorType id)
        {
            //Console.WriteLine(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss tt") + "LampOff Door");
            bool ret = false;
            byte[] dataSend = new byte[7];

            dataSend[0] = 0xFA;
            dataSend[1] = 0x55;
            dataSend[2] = (byte)CmdDoor.CMD_OFF_LAMP;
            dataSend[3] = 0x05;
            dataSend[4] = 0x00;
            dataSend[5] = (byte)id;
            dataSend[6] = CalChecksum(dataSend, 4);
            ret = this.Tranfer(dataSend);
            return ret;
        }
    }
}
