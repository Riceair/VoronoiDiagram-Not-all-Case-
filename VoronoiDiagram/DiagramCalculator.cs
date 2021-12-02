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
        private float max_number;
        private TextBox textBox; //Debug

        public DiagramCalculator(float max_number){
            this.max_number = max_number;
            calMath = new CalMath(max_number);
        }

        public void setTextBox(TextBox text){ //Debug
            textBox = text;
        }

        public PointEdgeRecoder run(List<PointF> points_list){
            points_list = calMath.getSortPoints(points_list); //先對points進行排序
            recoder_buffer.Clear();
            PointEdgeRecoder recod_result = runVoronoiDiagram(points_list); //執行遞迴式
            recod_result.points_list = calMath.getSortPoints(recod_result.points_list);
            recod_result.edges_list = calMath.getSortEdges(recod_result.edges_list);
            return recod_result;
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
                    /*外心為3中垂線交點(找2個中垂線交點即可)*/
                    if(calMath.is3ALine(pList[0], pList[1], pList[2])){ //檢查是否三點共線
                        eList.Add(calMath.getEdge(pList[0], pList[1])); //三點共線直接加入兩個邊
                        eList.Add(calMath.getEdge(pList[1], pList[2]));
                    }
                    else{ //沒有三點共線，找重心
                        List<PointF> pList_sort = calMath.getCounterClockwiseSortPoints(pList); //確保點逆時鐘排序
                        
                        PointF centroid = calMath.getIntersection(calMath.getEdge(pList_sort[0], pList_sort[1]), calMath.getEdge(pList_sort[1], pList_sort[2])); //找重心
                        //若經過排序，重心往中垂線向量的方向就是要保留的邊
                        PointF vertVec_0_1 = calMath.getVerticalVec(calMath.getVector(pList_sort[0], pList_sort[1])); //點0 1的中垂線向量
                        PointF vertVec_1_2 = calMath.getVerticalVec(calMath.getVector(pList_sort[1], pList_sort[2])); //點1 2的中垂線向量
                        PointF vertVec_2_0 = calMath.getVerticalVec(calMath.getVector(pList_sort[2], pList_sort[0])); //點2 0的中垂線向量
                        //加入Voroni的Edge points (centroid+vector*max_number, center)
                        eList.Add(new Edge(calMath.addPoints(centroid, calMath.multPoints(vertVec_0_1, max_number)), centroid, pList_sort[0], pList_sort[1]));
                        eList.Add(new Edge(calMath.addPoints(centroid, calMath.multPoints(vertVec_1_2, max_number)), centroid, pList_sort[1], pList_sort[2]));
                        eList.Add(new Edge(calMath.addPoints(centroid, calMath.multPoints(vertVec_2_0, max_number)), centroid, pList_sort[2], pList_sort[0]));
                    }
                    point_edge  = new PointEdgeRecoder(pList, eList);
                }

                recoder_buffer.Add(point_edge); //記錄到buffer中
                return point_edge;
            }
            else{
                textBox.AppendText("尚無法處理大於三個的情況");
                return new PointEdgeRecoder(pList);
            }
        }

        public List<PointEdgeRecoder> getRecoderBuffer(){
            return recoder_buffer;
        }
    }
}
