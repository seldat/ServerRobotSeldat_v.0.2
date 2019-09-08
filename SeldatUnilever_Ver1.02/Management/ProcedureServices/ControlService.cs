using SeldatMRMS.Management.RobotManagent;
using System;
using static SeldatMRMS.Management.RobotManagent.RobotUnity;
using static SeldatMRMS.Management.RobotManagent.RobotUnityControl;

namespace SeldatMRMS
{
    public class ControlService : DBProcedureService
    {
        RobotUnity robot;
        public ControlService(RobotUnity robot) : base(robot)
        {
            this.robot = robot;
            if (robot != null)
            {
                robot.FinishStatesCallBack += FinishStatesCallBack;
                robot.LineEnableCallBack += LineEnableCallBack;
                //robot.AgvLaserError += AgvErrorCallBack;
            }
        }
        // robot control
        public virtual void ZoneHandler(Communication.Message message) { }
        public virtual void FinishStatesCallBack(Int32 message)
        {

            ResponseCommand resCmd = (ResponseCommand)message;
            if (resCmd == ResponseCommand.RESPONSE_FINISH_GOBACK_FRONTLINE)
            {
                robot.SwitchToDetectLine(false);
                robot.onFlagFinishPalletUpDownINsideBuffer = false;
            }
            else if(resCmd == ResponseCommand.RESPONSE_LINEDETECT_PALLETUP || resCmd == ResponseCommand.RESPONSE_LINEDETECT_PALLETDOWN)
            {
                robot.onFlagFinishPalletUpDownINsideBuffer = true;
            }
        }
        public virtual void LineEnableCallBack(Int32 message)
        {
            ResponseCommand resCmd = (ResponseCommand)message;
            if (resCmd == ResponseCommand.RESPONSE_START_DETECT_LINE && !robot.onFlagGoBackReady)
            {
                robot.SwitchToDetectLine(true);
            }
        }

        public virtual void AgvErrorCallBack(bool message)
        {
            if (message)
            {
                robot.setColorRobotStatus(RobotStatusColorCode.ROBOT_STATUS_ERROR);
            }
            else {
                robot.setColorRobotStatus(RobotStatusColorCode.ROBOT_STATUS_OK);
            }
        }
        public virtual void AmclPoseHandler(Communication.Message message) { }
        public virtual void CtrlRobotSpeed() { }
        public virtual void MoveBaseGoal() { }
        public virtual void AcceptDoSomething() { }
        // door control
        public virtual void ReceiveRounterEvent(String message) { }
        public virtual void KillEvent()
        {
            if (robot != null)
            {
                robot.FinishStatesCallBack -= FinishStatesCallBack;
                robot.LineEnableCallBack -= LineEnableCallBack;
            }
        }
    }
}
