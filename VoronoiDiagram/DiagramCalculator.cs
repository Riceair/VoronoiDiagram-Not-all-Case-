using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;

namespace VoronoiDiagram
{
    public class DiagramCalculator: Object
    {
        private List<PointEdgeRecoder> recoder_buffer = new List<PointEdgeRecoder>(); //紀錄執行步驟的buffer(以處理step模式)
        private CalMath calMath; //數學公式的物件
        private float maxX, maxY;
        private TextBox textBox; //Debug

        public DiagramCalculator(float maxX, float maxY){
            this.maxX = maxX;
            this.maxY = maxY;
            calMath = new CalMath(maxX);
        }

        public void setTextBox(TextBox text){ //Debug
            textBox = text;
        }

        public PointEdgeRecoder run(List<PointF> points_list){
            points_list = calMath.getSortPoints(points_list); //先對points進行排序
            recoder_buffer.Clear();
            return runVoronoiDiagram(points_list); //執行遞迴式
        }

        private PointEdgeRecoder runVoronoiDiagram(List<PointF> pList){ //遞迴
            int count = pList.Count; //紀錄當前的點個數
            if(count <= 3){ //直接做Voroni Diagram
                PointEdgeRecoder point_edge;
                List<Edge> eList = new List<Edge>();

                if(count == 1) //若只有1個，直接返回點
                    point_edge = new PointEdgeRecoder(pList);
                else if(count==2){ //若只有2個，畫中垂線再return
                    Edge edge = calMath.getEdge(pList[0], pList[1]); //取得中垂線兩點
                    eList.Add(edge);
                    point_edge = new PointEdgeRecoder(pList, eList);
                }
                else{ //若有3個點
                    //外心為3中垂線交點(找2個中垂線交點即可)
                    point_edge  = new PointEdgeRecoder(pList);
                }

                recoder_buffer.Add(point_edge); //記錄到buffer中
                return point_edge;
            }
            else{
                textBox.AppendText("尚無法處理大於三個的情況");
                return null;
            }
        }
    }
}
