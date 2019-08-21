using SeldatMRMS;
using SeldatMRMS.Management.RobotManagent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SeldatUnilever_Ver1._02.Management.TrafficManager
{
    public class RegistryIntersectionZone
    {
        private const int NUM_ROBOT_REG = 3;
        private int numReg;
        public String Name{ get; set; }
        private List<RobotUnity> Registryrobotlist = new List<RobotUnity>();
        private Point[] pointData;
        public RegistryIntersectionZone(String Name,int nRobot= NUM_ROBOT_REG)
        {
            this.Name = Name;
            numReg = nRobot;
        }
        public void setPoints(Point[] pList)
        {
            Array.Copy(pList, pointData, pList.Length);
        }
        public bool ObjectInZone(Point pos)
        {
            return ExtensionService.IsInPolygon(pointData, pos);
        }
        public void Registry(RobotUnity robot)
        {
            if (Registryrobotlist.Count > numReg)
                return;
            Registryrobotlist.Add(robot);
        }
        public bool Contains(RobotUnity robot)
        {
            return Registryrobotlist.Contains(robot);
        }
        public int GetIndex(RobotUnity robot)
        {
            if(Registryrobotlist.Contains(robot))
                return Registryrobotlist.IndexOf(robot);
            return -1;
        }
        public bool Remove(RobotUnity robot)
        {
            try
            {
                return Registryrobotlist.Remove(robot);
            }
            catch
            {
                return false;
            }
           
        }
        
        public String getNames()
        {
            String str = "";
            int index = 0;
            if(Registryrobotlist.Count>0)
            {
                foreach(RobotUnity r in Registryrobotlist)
                {
                    str += r.properties.Label;
                    str += "["+ index + "]";
                    str += "/ ";
                    index++;
                }
            }
            return str;
        }
        public bool CheckPermission(RobotUnity robot)
        {
            int index = GetIndex(robot);
            if (index == 0)
            {
                return true;
            }
            else
                return false;
        }
        public void Release(RobotUnity robot)
        {
            if(Registryrobotlist.Contains(robot))
               Remove(robot);
        }
        public bool ProcessRegistryIntersectionZone(RobotUnity robot) // 
        {
            if(!Contains(robot))
            {
                Registry(robot);
              ///  return false ;
            }
          //  else
           // {
                return CheckPermission(robot);
           // }
        }
    }
}
