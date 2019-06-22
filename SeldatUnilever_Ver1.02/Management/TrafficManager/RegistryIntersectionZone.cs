using SeldatMRMS.Management.RobotManagent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeldatUnilever_Ver1._02.Management.TrafficManager
{
    public class RegistryIntersectionZone
    {
        private const int NUM_ROBOT_REG = 3;
        public String Name{ get; set; }
        private List<RobotUnity> Registryrobotlist = new List<RobotUnity>();
        public RegistryIntersectionZone(String Name)
        {
            this.Name = Name;
        }
        public void Registry(RobotUnity robot)
        {
            if (Registryrobotlist.Count > NUM_ROBOT_REG)
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
            return Registryrobotlist.Remove(robot);
        }
        public bool CheckPermission(RobotUnity robot)
        {
            int index = GetIndex(robot);
            if (GetIndex(robot) == 0)
            {
                return true;
            }
            else
                return false;
        }
        public void Release(RobotUnity robot)
        {
            Remove(robot);
        }
        public bool ProcessRegistryIntersectionZone(RobotUnity robot) // 
        {
            if(!Contains(robot))
            {
                Registry(robot);
                return false ;
            }
            else
            {
                return CheckPermission(robot);
            }
        }
    }
}
