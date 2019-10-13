using PieControls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SeldatUnilever_Ver1._02
{
    public class PieChartPercent
    {
       public double perc0;
       public  double perc1;
        public double perc2;
         public double perc3;
        
        

        string _name0;
        string _name1;
        string _name2;
        string _name3;
      
        Color _cl0;
        Color _cl1;
        Color _cl2;
        Color _cl3;

        public  ObservableCollection<PieSegment> pieCollection;

        MainWindow _Main;

        public PieChartPercent(MainWindow _main)
        {
            _Main = _main;

            
        }




        public void  Draw (List<ChartInfo> list_chartInfo)
        {
            pieCollection = new ObservableCollection<PieSegment>();
            for (int i= 0; i < list_chartInfo.Count; i++)
            {
                pieCollection.Add(new PieSegment { Color = list_chartInfo[i].color, Value = list_chartInfo[i].value, Name = list_chartInfo[i].name });
               
            }
         //   return pieCollection;
               //_Main.pieChart.Data = pieCollection;
        }

        

        public void SetName(string name0, string name1, string name2, string name3)
        {
            _name0 = name0;
            _name1 = name1;
            _name2 = name2;
            _name3 = name3;   
        }

        public void SetColor0(Color cl0)
        {
            _cl0 = cl0;            
        }

        public void SetColor1 (Color cl1)
        {
            _cl1 = cl1;
        }

        public void SetColor2 (Color cl2)
        {
            _cl2 = cl2;
        }
        public void SetColor3 (Color cl3)
        {
            _cl3 = cl3;
        }

        public void SetValue(double pc0, double pc1, double pc2, double pc3)
        {
            perc0 = pc0;
            perc1 = pc1;
            perc2 = pc2;
            perc3 = pc3;
        }

        public void Reset()
        {

        }

        
    }

    public class ChartInfo
    {
        public string name;
        public Color color;
        public double value;

        public ChartInfo()
        {

        }
    }
}
