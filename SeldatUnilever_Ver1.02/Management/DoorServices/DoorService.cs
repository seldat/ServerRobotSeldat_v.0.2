using SelDatUnilever_Ver1._00.Management;
using SelDatUnilever_Ver1._00.Management.ComSocket;
using System;
using System.Diagnostics;
using System.Threading;
using static SeldatMRMS.Management.RobotManagent.RobotUnityControl;
using static SeldatMRMS.Management.TrafficRobotUnity;

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
            DOOR_MEZZAMINE_UP_NEW = 1, /* 0x02 */
            DOOR_MEZZAMINE_UP = 2,
            DOOR_MEZZAMINE_RETURN, /* 0x03 */
            DOOR_ELEVATOR, /* 0x04 */
        }
        public enum DoorType
        {
            DOOR_FRONT = 0x01,
            DOOR_BACK, /* 0x02 */
        };
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
            DOOR_ST_OPEN_FRONT,
            DOOR_ST_WAITTING_CLOSE_DOOR_BACK_WHEN_IT_OPENING,
            DOOR_ST_WAITTING_OPEN_DOOR_FRONT,
            DOOR_ST_OPEN_FRONT_SUCCESS,
            DOOR_ST_CLOSE_DOOR_FRONT,
            DOOR_ST_WAITTING_CLOSE_DOOR_FRONT,
            DOOR_ST_CLOSE_DOOR_FRONT_SUCCESS,

            DOOR_ST_OPEN_DOOR_BACK,
            DOOR_ST_WAITTING_OPEN_DOOR_BACK,
            DOOR_ST_OPEN_DOOR_BACK_SUCCESS,
            DOOR_ST_CLOSE_DOOR_BACK,
            DOOR_ST_WAITTING_CLOSE_DOOR_BACK,
            DOOR_ST_CLOSE_DOOR_BACK_SUCCESS,
            DOOR_ST_ERROR
        }


        public enum stateCtrlLampDoor
        {
            LAMP_DOOR_IDLE = 0,
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
            private String _PointFrontLineStr;
            public String PointFrontLineStr { get => _PointFrontLineStr; set { _PointFrontLineStr = value; RaisePropertyChanged("PointFrontLineStr"); } }

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
        }

        public DoorInfoConfig config;
        private Thread doorFrontServiceThread;
        private Thread doorBackServiceThread;
        private StateCtrl stateCtrlDoorFront;
        private StateCtrl stateCtrlDoorBack;
        private stateCtrlLampDoor stateCtrlLampFront;
        private stateCtrlLampDoor stateCtrlLampBack;
        //public Stopwatch elapsedTimeFront_;
        //public Stopwatch elapsedTimeBack_;
        private const UInt32 TIME_OUT_WAIT_DOOR_FRONT = 9000;
        private const UInt32 TIME_OUT_WAIT_DOOR_BACK = 9000;
        private const UInt32 NUM_TRY_OPEN_DOOR = 100;
        private const UInt32 NUM_TRY_CLOSE_DOOR = 100;
        private UInt32 numTryOpen = 0;
        private UInt32 numTryClose = 0;
        private bool socketBusy = false;
        private bool doorBusy;

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
            doorFrontServiceThread = new Thread(this.doorFrontCtrlProcess);
            doorFrontServiceThread.Start(this);
            doorBackServiceThread = new Thread(this.doorBackCtrlProcess);
            doorBackServiceThread.Start(this);
            stateCtrlDoorFront = StateCtrl.DOOR_ST_IDLE;
            stateCtrlDoorBack = StateCtrl.DOOR_ST_IDLE;
            stateCtrlLampFront = stateCtrlLampDoor.LAMP_DOOR_IDLE;
            stateCtrlLampBack = stateCtrlLampDoor.LAMP_DOOR_IDLE;
            //elapsedTimeFront_ = new Stopwatch();
            //elapsedTimeBack_ = new Stopwatch();
            this.numTryClose = 0;
            this.numTryOpen = 0;
            this.doorBusy = false;
            //SetId(cf.id);
        }

        public void LampSetStateOn(DoorType dt)
        {
            switch (dt)
            {
                case DoorType.DOOR_FRONT:
                    this.stateCtrlLampFront = stateCtrlLampDoor.LAMP_DOOR_ON;
                    break;
                case DoorType.DOOR_BACK:
                    this.stateCtrlLampBack = stateCtrlLampDoor.LAMP_DOOR_ON;
                    break;
                default:
                    break;
            }
        }

        public void LampSetStateOff(DoorType dt)
        {
            switch (dt)
            {
                case DoorType.DOOR_FRONT:
                    this.stateCtrlLampFront = stateCtrlLampDoor.LAMP_DOOR_OFF;
                    break;
                case DoorType.DOOR_BACK:
                    this.stateCtrlLampBack = stateCtrlLampDoor.LAMP_DOOR_OFF;
                    break;
                default:
                    break;
            }
        }

        private void lampFrontProcess()
        {
            switch (stateCtrlLampFront)
            {
                case stateCtrlLampDoor.LAMP_DOOR_IDLE:
                    break;
                case stateCtrlLampDoor.LAMP_DOOR_ON:
                    if (true == this.LampOn(DoorType.DOOR_FRONT))
                    {
                        this.stateCtrlLampFront = stateCtrlLampDoor.LAMP_DOOR_IDLE;
                    }
                    break;
                case stateCtrlLampDoor.LAMP_DOOR_OFF:
                    if (true == this.LampOff(DoorType.DOOR_FRONT))
                    {
                        this.stateCtrlLampFront = stateCtrlLampDoor.LAMP_DOOR_IDLE;
                    }
                    break;
                default:
                    break;
            }
        }

        private void lampBackProcess()
        {

            switch (stateCtrlLampBack)
            {
                case stateCtrlLampDoor.LAMP_DOOR_IDLE:
                    break;
                case stateCtrlLampDoor.LAMP_DOOR_ON:
                    if (true == this.LampOn(DoorType.DOOR_BACK))
                    {
                        this.stateCtrlLampBack = stateCtrlLampDoor.LAMP_DOOR_IDLE;
                    }
                    break;
                case stateCtrlLampDoor.LAMP_DOOR_OFF:
                    if (true == this.LampOff(DoorType.DOOR_BACK))
                    {
                        this.stateCtrlLampBack = stateCtrlLampDoor.LAMP_DOOR_IDLE;
                    }
                    break;
                default:
                    break;
            }
        }

        public void openDoor(DoorType type)
        {
            this.numTryOpen = 0;
            switch (type)
            {
                case DoorType.DOOR_FRONT:
                    if ((this.stateCtrlDoorFront == StateCtrl.DOOR_ST_IDLE) || (this.stateCtrlDoorFront == StateCtrl.DOOR_ST_ERROR) ||
                    (this.stateCtrlDoorFront == StateCtrl.DOOR_ST_OPEN_FRONT_SUCCESS) || (this.stateCtrlDoorFront == StateCtrl.DOOR_ST_CLOSE_DOOR_FRONT_SUCCESS))
                    {
                        this.stateCtrlDoorFront = StateCtrl.DOOR_ST_OPEN_FRONT;
                    }
                    break;
                case DoorType.DOOR_BACK:
                    if ((this.stateCtrlDoorBack == StateCtrl.DOOR_ST_IDLE) || (this.stateCtrlDoorBack == StateCtrl.DOOR_ST_ERROR) ||
                    (this.stateCtrlDoorBack == StateCtrl.DOOR_ST_OPEN_DOOR_BACK_SUCCESS) || (this.stateCtrlDoorBack == StateCtrl.DOOR_ST_CLOSE_DOOR_BACK_SUCCESS))
                    {
                        this.stateCtrlDoorBack = StateCtrl.DOOR_ST_OPEN_DOOR_BACK;
                    }
                    break;
                default:
                    break;
            }
        }

        public void closeDoor(DoorType type)
        {
            this.numTryClose = 0;
            switch (type)
            {
                case DoorType.DOOR_FRONT:
                    //if ((this.stateCtrlDoorFront == StateCtrl.DOOR_ST_IDLE) || (this.stateCtrlDoorFront == StateCtrl.DOOR_ST_ERROR) ||
                    //(this.stateCtrlDoorFront == StateCtrl.DOOR_ST_OPEN_FRONT_SUCCESS) || (this.stateCtrlDoorFront == StateCtrl.DOOR_ST_CLOSE_DOOR_FRONT_SUCCESS))
                    //{
                    this.stateCtrlDoorFront = StateCtrl.DOOR_ST_CLOSE_DOOR_FRONT;
                    //}                     
                    break;
                case DoorType.DOOR_BACK:
                    //if ((this.stateCtrlDoorBack == StateCtrl.DOOR_ST_IDLE) || (this.stateCtrlDoorBack == StateCtrl.DOOR_ST_ERROR) ||
                    //(this.stateCtrlDoorBack == StateCtrl.DOOR_ST_OPEN_DOOR_BACK_SUCCESS) || (this.stateCtrlDoorBack == StateCtrl.DOOR_ST_CLOSE_DOOR_BACK_SUCCESS))
                    //{
                    this.stateCtrlDoorBack = StateCtrl.DOOR_ST_CLOSE_DOOR_BACK;
                    //}
                    break;
                default:
                    break;
            }
        }

        public RetState checkOpen(DoorType type)
        {
            switch (type)
            {
                case DoorType.DOOR_FRONT:
                    if (this.stateCtrlDoorFront == StateCtrl.DOOR_ST_OPEN_FRONT_SUCCESS)
                    {
                        return RetState.DOOR_CTRL_SUCCESS;
                    }
                    else if (this.stateCtrlDoorFront == StateCtrl.DOOR_ST_ERROR)
                    {
                        Console.WriteLine("OPEN_DOOR_FRONT_ERROR");
                        return RetState.DOOR_CTRL_ERROR;
                    }
                    break;
                case DoorType.DOOR_BACK:
                    if (this.stateCtrlDoorBack == StateCtrl.DOOR_ST_OPEN_DOOR_BACK_SUCCESS)
                    {
                        return RetState.DOOR_CTRL_SUCCESS;
                    }
                    else if (this.stateCtrlDoorBack == StateCtrl.DOOR_ST_ERROR)
                    {
                        Console.WriteLine("OPEN_DOOR_BACK_ERROR");
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
                    if (this.stateCtrlDoorFront == StateCtrl.DOOR_ST_CLOSE_DOOR_FRONT_SUCCESS)
                    {
                        return RetState.DOOR_CTRL_SUCCESS;
                    }
                    else if (this.stateCtrlDoorFront == StateCtrl.DOOR_ST_ERROR)
                    {
                        Console.WriteLine("CLOSE_DOOR_FRONT_ERROR");
                        return RetState.DOOR_CTRL_ERROR;
                    }
                    break;
                case DoorType.DOOR_BACK:
                    if (this.stateCtrlDoorBack == StateCtrl.DOOR_ST_CLOSE_DOOR_BACK_SUCCESS)
                    {
                        return RetState.DOOR_CTRL_SUCCESS;
                    }
                    else if (this.stateCtrlDoorBack == StateCtrl.DOOR_ST_ERROR)
                    {
                        Console.WriteLine("CLOSE_DOOR_BACK_ERROR");
                        return RetState.DOOR_CTRL_ERROR;
                    }
                    break;
                default:
                    break;
            }
            return RetState.DOOR_CTRL_WAITTING;
        }

        public void doorFrontCtrlProcess(object ojb)
        {
            DoorService dS = (DoorService)ojb;
            DataReceive status = new DataReceive();
            Stopwatch elapsedTimeFront = new Stopwatch();
            elapsedTimeFront.Start();
            while (true)
            {
                switch (this.stateCtrlDoorFront)
                {
                    case StateCtrl.DOOR_ST_IDLE:
                        break;
                    case StateCtrl.DOOR_ST_OPEN_FRONT:
                        this.GetStatus(ref status, DoorType.DOOR_BACK);
                        if (status.data[0] == (byte)DoorStatus.DOOR_CLOSE)
                        {
                            if (this.socketBusy == false)
                            {
                                Console.WriteLine("DOOR_ST_OPEN_FRONT");
                                this.socketBusy = true;
                                if (this.OpenPress(DoorType.DOOR_FRONT))
                                {
                                    elapsedTimeFront.Restart();
                                    this.stateCtrlDoorFront = StateCtrl.DOOR_ST_WAITTING_OPEN_DOOR_FRONT;
                                    Console.WriteLine("DOOR_ST_WAITTING_OPEN_DOOR_FRONT");
                                }
                                else
                                {
                                    this.numTryOpen++;
                                    if (this.numTryOpen >= NUM_TRY_OPEN_DOOR)
                                    {
                                        this.numTryOpen = 0;
                                        this.stateCtrlDoorFront = StateCtrl.DOOR_ST_ERROR;
                                    }
                                }
                            }
                            this.socketBusy = false;
                        }
                        else
                        {
                            this.closeDoor(DoorType.DOOR_BACK);
                            this.stateCtrlDoorFront = StateCtrl.DOOR_ST_WAITTING_CLOSE_DOOR_BACK_WHEN_IT_OPENING;
                            Console.WriteLine("DOOR_ST_WAITTING_CLOSE_DOOR_BACK_WHEN_IT_OPENING");
                        }
                        break;
                    case StateCtrl.DOOR_ST_WAITTING_CLOSE_DOOR_BACK_WHEN_IT_OPENING:
                        if (RetState.DOOR_CTRL_SUCCESS == this.checkClose(DoorType.DOOR_BACK))
                        {
                            this.stateCtrlDoorFront = StateCtrl.DOOR_ST_OPEN_FRONT;
                            Console.WriteLine("DOOR_ST_OPEN_FRONT");
                        }
                        break;
                    case StateCtrl.DOOR_ST_WAITTING_OPEN_DOOR_FRONT:
                        if (elapsedTimeFront.ElapsedMilliseconds >= TIME_OUT_WAIT_DOOR_FRONT)
                        {
                            elapsedTimeFront.Stop();
                            this.stateCtrlDoorFront = StateCtrl.DOOR_ST_OPEN_FRONT;
                            this.numTryOpen++;
                            Console.WriteLine("TIME_OUT_WAIT_OPEN_DOOR_FRONT");
                            if (this.numTryOpen >= NUM_TRY_OPEN_DOOR)
                            {
                                this.numTryOpen = 0;
                                this.stateCtrlDoorFront = StateCtrl.DOOR_ST_ERROR;
                            }
                        }
                        else
                        {
                            if (this.socketBusy == false)
                            {
                                this.socketBusy = true;
                                try
                                {
                                    this.GetStatus(ref status, DoorType.DOOR_FRONT);
                                    if (status.data[0] == (byte)DoorStatus.DOOR_OPEN)
                                    {
                                        this.OpenRelease(DoorType.DOOR_FRONT);
                                        this.stateCtrlDoorFront = StateCtrl.DOOR_ST_OPEN_FRONT_SUCCESS;
                                        elapsedTimeFront.Stop();
                                        Console.WriteLine("DOOR_ST_OPEN_FRONT_SUCCESS");
                                    }
                                    else if (status.data[0] == (byte)DoorStatus.DOOR_ERROR)
                                    {
                                        elapsedTimeFront.Stop();
                                        this.stateCtrlDoorFront = StateCtrl.DOOR_ST_OPEN_FRONT;
                                        this.numTryOpen++;
                                        Console.WriteLine("DOOR_FRONT_OPEN DoorStatus.DOOR_ERROR");
                                        if (this.numTryOpen >= NUM_TRY_OPEN_DOOR)
                                        {
                                            this.numTryOpen = 0;
                                            this.stateCtrlDoorFront = StateCtrl.DOOR_ST_ERROR;
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                }
                            }
                            this.socketBusy = false;
                        }
                        break;
                    case StateCtrl.DOOR_ST_OPEN_FRONT_SUCCESS:
                        break;
                    case StateCtrl.DOOR_ST_CLOSE_DOOR_FRONT:
                        if (this.socketBusy == false)
                        {
                            this.socketBusy = true;
                            Console.WriteLine("DOOR_ST_CLOSE_DOOR_FRONT");
                            if (this.ClosePress(DoorType.DOOR_FRONT))
                            {
                                elapsedTimeFront.Restart();
                                this.stateCtrlDoorFront = StateCtrl.DOOR_ST_WAITTING_CLOSE_DOOR_FRONT;
                                Console.WriteLine("DOOR_ST_WAITTING_CLOSE_DOOR_FRONT");
                            }
                            else
                            {
                                this.numTryClose++;
                                if (this.numTryClose >= NUM_TRY_CLOSE_DOOR)
                                {
                                    this.numTryClose = 0;
                                    this.stateCtrlDoorFront = StateCtrl.DOOR_ST_ERROR;
                                }
                            }
                        }
                        this.socketBusy = false;
                        break;
                    case StateCtrl.DOOR_ST_WAITTING_CLOSE_DOOR_FRONT:
                        if (elapsedTimeFront.ElapsedMilliseconds >= TIME_OUT_WAIT_DOOR_FRONT)
                        {
                            elapsedTimeFront.Stop();
                            this.stateCtrlDoorFront = StateCtrl.DOOR_ST_CLOSE_DOOR_FRONT;
                            this.numTryClose++;
                            Console.WriteLine("TIME_OUT_WAIT_CLOSE_DOOR_FRONT");
                            if (this.numTryClose >= NUM_TRY_CLOSE_DOOR)
                            {
                                this.numTryClose = 0;
                                this.stateCtrlDoorFront = StateCtrl.DOOR_ST_ERROR;
                            }
                        }
                        else
                        {
                            if (this.socketBusy == false)
                            {
                                this.socketBusy = true;
                                try
                                {
                                    this.GetStatus(ref status, DoorType.DOOR_FRONT);
                                    if (status.data[0] == (byte)DoorStatus.DOOR_CLOSE)
                                    {
                                        this.CloseRelease(DoorType.DOOR_FRONT);
                                        this.stateCtrlDoorFront = StateCtrl.DOOR_ST_CLOSE_DOOR_FRONT_SUCCESS;
                                        elapsedTimeFront.Stop();
                                        Console.WriteLine("DOOR_ST_CLOSE_DOOR_FRONT_SUCCESS");
                                    }
                                    else if (status.data[0] == (byte)DoorStatus.DOOR_ERROR)
                                    {
                                        elapsedTimeFront.Stop();
                                        this.stateCtrlDoorFront = StateCtrl.DOOR_ST_CLOSE_DOOR_FRONT;
                                        this.numTryClose++;
                                        Console.WriteLine("CLOSE_DOOR_FRONT DoorStatus.DOOR_ERROR");
                                        if (this.numTryClose >= NUM_TRY_CLOSE_DOOR)
                                        {
                                            this.numTryClose = 0;
                                            this.stateCtrlDoorFront = StateCtrl.DOOR_ST_ERROR;
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                }
                            }
                            this.socketBusy = false;
                        }
                        break;
                    case StateCtrl.DOOR_ST_CLOSE_DOOR_FRONT_SUCCESS:
                        break;
                    case StateCtrl.DOOR_ST_ERROR:
                        break;
                    default:
                        break;
                }
                this.lampFrontProcess();
                Thread.Sleep(50);
            }
        }

        public void doorBackCtrlProcess(object ojb)
        {
            DoorService dS = (DoorService)ojb;
            DataReceive status = new DataReceive();
            Stopwatch elapsedTimeBack = new Stopwatch();
            elapsedTimeBack.Start();
            while (true)
            {
                switch (this.stateCtrlDoorBack)
                {
                    case StateCtrl.DOOR_ST_IDLE:
                        break;
                    case StateCtrl.DOOR_ST_OPEN_DOOR_BACK:
                        this.GetStatus(ref status, DoorType.DOOR_FRONT);
                        if (status.data[0] == (byte)DoorStatus.DOOR_CLOSE)
                        {
                            if (this.socketBusy == false)
                            {
                                this.socketBusy = true;
                                Console.WriteLine("DOOR_ST_OPEN_DOOR_BACK");
                                if (this.OpenPress(DoorType.DOOR_BACK))
                                {
                                    elapsedTimeBack.Restart();
                                    this.stateCtrlDoorBack = StateCtrl.DOOR_ST_WAITTING_OPEN_DOOR_BACK;
                                    Console.WriteLine("DOOR_ST_WAITTING_OPEN_DOOR_BACK");
                                }
                                else
                                {
                                    this.numTryOpen++;
                                    if (this.numTryOpen >= NUM_TRY_OPEN_DOOR)
                                    {
                                        this.numTryOpen = 0;
                                        this.stateCtrlDoorBack = StateCtrl.DOOR_ST_ERROR;
                                    }
                                }
                                this.socketBusy = false;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Door front is opening please close it ___(^_^)___");
                        }
                        break;
                    case StateCtrl.DOOR_ST_WAITTING_OPEN_DOOR_BACK:
                        if (elapsedTimeBack.ElapsedMilliseconds >= TIME_OUT_WAIT_DOOR_BACK)
                        {
                            elapsedTimeBack.Stop();
                            this.stateCtrlDoorBack = StateCtrl.DOOR_ST_OPEN_DOOR_BACK;
                            this.numTryOpen++;
                            Console.WriteLine("TIME_OUT_WAIT_OPEN_DOOR_BACK");
                            if (this.numTryOpen >= NUM_TRY_OPEN_DOOR)
                            {
                                this.numTryOpen = 0;
                                this.stateCtrlDoorBack = StateCtrl.DOOR_ST_ERROR;
                            }
                        }
                        else
                        {
                            if (this.socketBusy == false)
                            {
                                this.socketBusy = true;
                                try
                                {
                                    this.GetStatus(ref status, DoorType.DOOR_BACK);
                                    if (status.data[0] == (byte)DoorStatus.DOOR_OPEN)
                                    {
                                        this.OpenRelease(DoorType.DOOR_BACK);
                                        this.stateCtrlDoorBack = StateCtrl.DOOR_ST_OPEN_DOOR_BACK_SUCCESS;
                                        elapsedTimeBack.Stop();
                                        Console.WriteLine("DOOR_ST_OPEN_DOOR_BACK_SUCCESS");
                                    }
                                    else if (status.data[0] == (byte)DoorStatus.DOOR_ERROR)
                                    {
                                        elapsedTimeBack.Stop();
                                        this.stateCtrlDoorBack = StateCtrl.DOOR_ST_OPEN_DOOR_BACK;
                                        this.numTryOpen++;
                                        Console.WriteLine("DOOR_BACK_OPEN DoorStatus.DOOR_ERROR");
                                        if (this.numTryOpen >= NUM_TRY_OPEN_DOOR)
                                        {
                                            this.numTryOpen = 0;
                                            this.stateCtrlDoorBack = StateCtrl.DOOR_ST_ERROR;
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                }
                            }
                            this.socketBusy = false;
                        }
                        break;
                    case StateCtrl.DOOR_ST_OPEN_DOOR_BACK_SUCCESS:
                        break;
                    case StateCtrl.DOOR_ST_CLOSE_DOOR_BACK:
                        if (this.socketBusy == false)
                        {
                            this.socketBusy = true;
                            Console.WriteLine("DOOR_ST_CLOSE_DOOR_BACK");
                            if (this.ClosePress(DoorType.DOOR_BACK))
                            {
                                elapsedTimeBack.Restart();
                                this.stateCtrlDoorBack = StateCtrl.DOOR_ST_WAITTING_CLOSE_DOOR_BACK;
                                Console.WriteLine("DOOR_ST_WAITTING_CLOSE_DOOR_BACK");
                            }
                            else
                            {
                                this.numTryClose++;
                                if (this.numTryClose >= NUM_TRY_CLOSE_DOOR)
                                {
                                    this.numTryClose = 0;
                                    this.stateCtrlDoorBack = StateCtrl.DOOR_ST_ERROR;
                                }
                            }
                        }
                        this.socketBusy = false;
                        break;
                    case StateCtrl.DOOR_ST_WAITTING_CLOSE_DOOR_BACK:
                        if (elapsedTimeBack.ElapsedMilliseconds >= TIME_OUT_WAIT_DOOR_BACK)
                        {
                            elapsedTimeBack.Stop();
                            this.stateCtrlDoorBack = StateCtrl.DOOR_ST_CLOSE_DOOR_BACK;
                            this.numTryClose++;
                            Console.WriteLine("TIME_OUT_WAIT_CLOSE_DOOR_BACK");
                            if (this.numTryClose >= NUM_TRY_CLOSE_DOOR)
                            {
                                this.numTryClose = 0;
                                this.stateCtrlDoorBack = StateCtrl.DOOR_ST_ERROR;
                            }
                        }
                        else
                        {
                            if (this.socketBusy == false)
                            {
                                this.socketBusy = true;
                                try
                                {
                                    this.GetStatus(ref status, DoorType.DOOR_BACK);
                                    if (status.data[0] == (byte)DoorStatus.DOOR_CLOSE)
                                    {
                                        this.CloseRelease(DoorType.DOOR_BACK);
                                        this.stateCtrlDoorBack = StateCtrl.DOOR_ST_CLOSE_DOOR_BACK_SUCCESS;
                                        elapsedTimeBack.Stop();
                                        Console.WriteLine("DOOR_ST_CLOSE_DOOR_BACK_SUCCESS");
                                    }
                                    else if (status.data[0] == (byte)DoorStatus.DOOR_ERROR)
                                    {
                                        elapsedTimeBack.Stop();
                                        this.stateCtrlDoorBack = StateCtrl.DOOR_ST_CLOSE_DOOR_BACK;
                                        this.numTryClose++;
                                        Console.WriteLine("DOOR_BACK_CLOSE DoorStatus.DOOR_ERROR");
                                        if (this.numTryClose >= NUM_TRY_CLOSE_DOOR)
                                        {
                                            this.numTryClose = 0;
                                            this.stateCtrlDoorBack = StateCtrl.DOOR_ST_ERROR;
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                }
                            }
                            this.socketBusy = false;
                        }
                        break;
                    case StateCtrl.DOOR_ST_CLOSE_DOOR_BACK_SUCCESS:
                        break;
                    case StateCtrl.DOOR_ST_ERROR:
                        break;
                    default:
                        break;
                }
                this.lampBackProcess();
                Thread.Sleep(50);
            }
        }

        private bool GetStatus(ref DataReceive data, DoorType id)
        {
            //Console.WriteLine(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss tt") + "GetStatus Door");
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
            return ret;
        }

        private bool OpenPress(DoorType id)
        {
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
            bool ret = false;
            byte[] dataSend = new byte[7];

            dataSend[0] = 0xFA;
            dataSend[1] = 0x55;
            dataSend[2] = (byte)CmdDoor.CMD_ON_LAMP;
            dataSend[3] = 0x05;
            dataSend[4] = 0x00;
            dataSend[5] = (byte)id;
            dataSend[6] = CalChecksum(dataSend, 4);
            if (this.socketBusy == true)
            {
                return false;
            }
            this.socketBusy = true;
            ret = this.Tranfer(dataSend);
            this.socketBusy = false;
            return ret;
        }

        private bool LampOff(DoorType id)
        {
            bool ret = false;
            byte[] dataSend = new byte[7];

            dataSend[0] = 0xFA;
            dataSend[1] = 0x55;
            dataSend[2] = (byte)CmdDoor.CMD_OFF_LAMP;
            dataSend[3] = 0x05;
            dataSend[4] = 0x00;
            dataSend[5] = (byte)id;
            dataSend[6] = CalChecksum(dataSend, 4);
            if (this.socketBusy == true)
            {
                return false;
            }
            this.socketBusy = true;
            ret = this.Tranfer(dataSend);
            this.socketBusy = false;
            return ret;
        }
    }
}
