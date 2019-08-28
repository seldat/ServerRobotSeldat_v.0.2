using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SeldatMRMS.Management.RobotManagent;
using SeldatUnilever_Ver1._02.DTO;
using SelDatUnilever_Ver1._00.Management.ComSocket;
using static SelDatUnilever_Ver1._00.Management.ChargerCtrl.ChargerCtrl;

namespace SeldatUnilever_Ver1._02.Management.McuCom
{
    public class McuCtrl : TranferData
    {
        private enum CmdCtrlMcu
        {
            CMD_GET_BAT_LEVEL = 0x07,
            RES_GET_BAT_LEVEL = 0x08,
            CMD_TURNON_PC = 0x0B,
            RES_TURNOFF_PC = 0x0C,
            CMD_CTRL_LAMP = 0x0D,
            RES_CTRL_LAMP = 0x0E
        }

        private enum lampLgCtrl
        {
            LAMP_OFF = 0,
            LAMP_ON
        }

        //public enum stateCtrlLamp
        //{
        //    LAMP_MCU_IDLE = 0,
        //    LAMP_MCU_ON,
        //    LAMP_MCU_OFF
        //}

        private Thread ctrlLampOnRbThread;
        //private stateCtrlLamp stateCtrlLampRb;
        private List<lampLgCtrl> listCtrlLamRb  = new List<lampLgCtrl>();

        public McuCtrl(RobotUnity rb) : base(rb.properties.ipMcuCtrl, rb.properties.portMcuCtrl)
        {
            //this.stateCtrlLampRb = stateCtrlLamp.LAMP_MCU_IDLE;
            ctrlLampOnRbThread = new Thread(this.lampOnRbCtrlProcess);
            ctrlLampOnRbThread.Start(this);
        }

        public void lampRbOn()
        {
            lampLgCtrl ctrl = lampLgCtrl.LAMP_ON;
            this.listCtrlLamRb.Add(ctrl);
            //this.stateCtrlLampRb = stateCtrlLamp.LAMP_MCU_ON;
        }

        public void lampRbOff()
        {
            lampLgCtrl ctrl = lampLgCtrl.LAMP_OFF;
            this.listCtrlLamRb.Add(ctrl);
            //this.stateCtrlLampRb = stateCtrlLamp.LAMP_MCU_OFF;
        }

        private void lampOnRbCtrlProcess(object ojb)
        {
            while (true)
            {
                if (listCtrlLamRb.Count > 0)
                {
                    lampLgCtrl lgCtrl = listCtrlLamRb[0];
                    while (false == this.LampCtrl(lgCtrl)) {
                        Thread.Sleep(100);
                    }
                    listCtrlLamRb.RemoveAt(0);
                    Console.WriteLine("Lamp MCU " + lgCtrl+ " SUCCESS");
                }
                //switch (stateCtrlLampRb)
                //{
                //    case stateCtrlLamp.LAMP_MCU_IDLE:
                //        break;
                //    case stateCtrlLamp.LAMP_MCU_ON:
                //        if (true == this.TurnOnLampRb())
                //        {
                //            Thread.Sleep(100);
                //            this.TurnOnLampRb();
                //            Console.WriteLine(" Lamp MCU ON SUCCESS");
                //            stateCtrlLampRb = stateCtrlLamp.LAMP_MCU_IDLE;
                //        }
                //        break;
                //    case stateCtrlLamp.LAMP_MCU_OFF:
                //        if (true == this.TurnOffLampRb())
                //        {
                //            Thread.Sleep(100);
                //            this.TurnOffLampRb();
                //            Console.WriteLine(" Lamp MCU OFF SUCCESS");
                //            stateCtrlLampRb = stateCtrlLamp.LAMP_MCU_IDLE;
                //        }
                //        break;
                //    default:
                //        break;
                //}
                Thread.Sleep(100);
            }
        }

        public bool TurnOnPcRobot()
        {
            bool ret = false;
            byte[] dataSend = new byte[6];

            dataSend[0] = 0xFA;
            dataSend[1] = 0x55;
            dataSend[2] = (byte)CmdCtrlMcu.CMD_TURNON_PC;
            dataSend[3] = 0x04;
            dataSend[4] = 0x00;
            dataSend[5] = CalChecksum(dataSend, 3);
            ret = this.Tranfer(dataSend);
            return ret;
        }

        public ErrorCodeCharger GetBatteryLevel(ref DataReceive data)
        {
            ErrorCodeCharger ret = ErrorCodeCharger.TRUE;
            byte[] dataSend = new byte[6];

            dataSend[0] = 0xFA;
            dataSend[1] = 0x55;
            dataSend[2] = (byte)CmdCtrlMcu.CMD_GET_BAT_LEVEL;
            dataSend[3] = 0x04;
            dataSend[4] = 0x00;
            dataSend[5] = CalChecksum(dataSend, 3);
            bool result = this.Tranfer(dataSend, ref data);
            if (result == false)
            {
                ret = ErrorCodeCharger.ERROR_CONNECT;
            }
            return ret;
        }

        private bool LampCtrl(lampLgCtrl lg)
        {
            bool ret = false;
            byte[] dataSend = new byte[7];

            dataSend[0] = 0xFA;
            dataSend[1] = 0x55;
            dataSend[2] = (byte)CmdCtrlMcu.CMD_CTRL_LAMP;
            dataSend[3] = 0x05;
            dataSend[4] = 0x00;
            dataSend[5] = (byte)lg;
            dataSend[6] = CalChecksum(dataSend, 4);
            ret = this.Tranfer(dataSend);
            return ret;
        }

        //private bool TurnOnLampRb()
        //{
        //    bool ret = false;
        //    byte[] dataSend = new byte[7];

        //    dataSend[0] = 0xFA;
        //    dataSend[1] = 0x55;
        //    dataSend[2] = (byte)CmdCtrlMcu.CMD_CTRL_LAMP;
        //    dataSend[3] = 0x05;
        //    dataSend[4] = 0x00;
        //    dataSend[5] = (byte)lampLgCtrl.LAMP_ON;
        //    dataSend[6] = CalChecksum(dataSend, 4);
        //    ret = this.Tranfer(dataSend);
        //    return ret;
        //}

        //private bool TurnOffLampRb()
        //{
        //    bool ret = false;
        //    byte[] dataSend = new byte[7];

        //    dataSend[0] = 0xFA;
        //    dataSend[1] = 0x55;
        //    dataSend[2] = (byte)CmdCtrlMcu.CMD_CTRL_LAMP;
        //    dataSend[3] = 0x05;
        //    dataSend[4] = 0x00;
        //    dataSend[5] = (byte)lampLgCtrl.LAMP_OFF;
        //    dataSend[6] = CalChecksum(dataSend, 4);
        //    ret = this.Tranfer(dataSend);
        //    return ret;
        //}
    }
}