using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;

namespace VoronoiDiagram
{
    public class DiagramCalculator: Object
    {
        private Queue<List<PointEdgeRecoder>> recoder_buffer = new Queue<List<PointEdgeRecoder>>(); //紀錄執行步驟的buffer(以處理step模式)
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
                    Edge edge = calMath.getVoronoiEdge(pList[0], pList[1]); //取得中垂線兩點
                    eList.Add(edge);
                    point_edge = new PointEdgeRecoder(pList, eList);
                }
                else{ //若有3個點
                    /*外心為3中垂線交點(找2個中垂線交點即可)*/
                    if(calMath.is3ALine(pList[0], pList[1], pList[2])){ //檢查是否三點共線
                        eList.Add(calMath.getVoronoiEdge(pList[0], pList[1])); //三點共線直接加入兩個邊
                        eList.Add(calMath.getVoronoiEdge(pList[1], pList[2]));
                    }
                    else{ //沒有三點共線，找重心
                        List<PointF> pList_sort = calMath.getCounterClockwiseSortPoints(pList); //確保點逆時鐘排序
                        
                        PointF centroid = calMath.getIntersection(calMath.getVoronoiEdge(pList_sort[0], pList_sort[1]), calMath.getVoronoiEdge(pList_sort[1], pList_sort[2])); //找重心
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
                
                point_edge.point_color = Color.Blue;
                point_edge.edge_color = Color.LightBlue;
                //存Buffer
                addToBuffer(point_edge); //記錄到buffer中
                return (PointEdgeRecoder) point_edge.getClone(); //用clone的物件
            }
            else{
                //左右兩邊分別跑VoronoiDiagram遞迴
                PointEdgeRecoder left_PE = runVoronoiDiagram(pList.GetRange(0, pList.Count/2));
                PointEdgeRecoder right_PE = runVoronoiDiagram(pList.GetRange(pList.Count/2, pList.Count - pList.Count/2));

                //存Buffer
                //取得左右邊的convex hull
                left_PE.edge_color = Color.DarkBlue;
                right_PE.edge_color = Color.Orange;
                left_PE.convex_list = calMath.getConvexHull(left_PE.points_list);
                right_PE.convex_list = calMath.getConvexHull(right_PE.points_list);
                addToBuffer(left_PE, right_PE); //記錄到buffer中
                
                //對所有的點找Convex Hull
                PointEdgeRecoder all_PE = new PointEdgeRecoder(new List<PointF>(left_PE.points_list)); //先複製左半部的點
                all_PE.points_list.AddRange(right_PE.points_list); //再將右半部的點複製加入
                List<Edge> ch_upper = calMath.getConvexUpper(all_PE.points_list); //找Convex Hull上包
                List<Edge> ch_lower = calMath.getConvexLower(all_PE.points_list); //找Convex Hull下包

                //存Buffer
                //針對找到所有Convex Hull存入buffer(為了顯示左右側，因此加入的是左右側的recoder)
                left_PE.convex_list = ch_upper;
                right_PE.convex_list = ch_lower;
                addToBuffer(left_PE, right_PE);

                Edge convex_upper_line = calMath.getPointDiffSideEdge(ch_upper, left_PE.points_list); //Convex Hull上方橫跨左右側的邊
                Edge convex_lower_line = calMath.getPointDiffSideEdge(ch_lower, left_PE.points_list); //Convex Hull下方橫跨左右側的邊

                Edge hyper_voronoi_edge = calMath.getVoronoiEdge(convex_upper_line.edgePA, convex_upper_line.edgePB); //取得hyper edge
                //複製左側的邊
                List<Edge> left_edges = left_PE.edges_list.ConvertAll(edge => new Edge(edge.edgePA, edge.edgePB, edge.pointA, edge.pointB));
                //複製右側的邊
                List<Edge> right_edges = right_PE.edges_list.ConvertAll(edge => new Edge(edge.edgePA, edge.edgePB, edge.pointA, edge.pointB));

                PointF start_left, start_right, end_left, end_right; //儲存merge的左右邊開始結束點
                 //確定start/end point左右側是真的存左右側
                if(convex_upper_line.edgePA.X < convex_upper_line.edgePB.X){
                    start_left = convex_upper_line.edgePA;
                    start_right = convex_upper_line.edgePB;
                }
                else{
                    start_left = convex_upper_line.edgePB;
                    start_right = convex_upper_line.edgePA;
                }
                if(convex_lower_line.edgePA.X < convex_lower_line.edgePB.X){
                    end_left = convex_lower_line.edgePA;
                    end_right = convex_lower_line.edgePB;
                }
                else{
                    end_left = convex_lower_line.edgePB;
                    end_right = convex_lower_line.edgePA;
                }

                PointF current_left = start_left, current_right = start_right;
                Edge current_edge = hyper_voronoi_edge;

                bool isInit = true;
                while(true){
                    List<Edge> left_remove_edge = new List<Edge>();
                    Edge left_first = null; //紀錄hyper plane第一個碰到的edge
                    PointF left_intersection = new PointF(); //紀錄交點
                    foreach(Edge edge in left_edges){
                        if(edge.pointA.Equals(current_left) || (edge.pointB.Equals(current_left)&&isInit)){
                            if(left_first==null){
                                left_first = edge;
                                left_intersection = calMath.getIntersection(edge, current_edge);
                            }
                            else{
                                PointF edge_intersection = calMath.getIntersection(edge, current_edge);
                                if(edge_intersection.Y > left_intersection.Y){
                                    left_first = edge;
                                    left_intersection = edge_intersection;
                                }
                            }
                        }
                    }

                    // if(!calMath.isIntersection(edge, current_edge)){ //兩線段沒有相交
                    //         // if(Math.Min(current_edge.edgePA.X, current_edge.edgePB.X)<Math.Min(edge.edgePA.X, edge.edgePB.X) //線完全在hyper plane右上方(要完全去除)
                    //         // && Math.Min(current_edge.edgePA.Y, current_edge.edgePB.Y)<Math.Min(edge.edgePA.Y, edge.edgePB.Y)
                    //         // ) left_remove_edge.Add(edge);
                    //         left_remove_edge.Add(edge);
                    //     }
                    // foreach(Edge edge in left_remove_edge){
                    //     left_edges.Remove(edge);
                    // }

                    List<Edge> right_remove_edge = new List<Edge>();
                    Edge right_first = null; //紀錄hyper plane第一個碰到的edge
                    PointF right_intersection = new PointF(); //紀錄交點
                    foreach(Edge edge in right_edges){
                        if(edge.pointA.Equals(current_right) || (edge.pointB.Equals(current_right)&&isInit)){
                            if(right_first==null){
                                right_first = edge;
                                right_intersection = calMath.getIntersection(edge, current_edge);
                            }
                            else{
                                PointF edge_intersection = calMath.getIntersection(edge, current_edge);
                                if(edge_intersection.Y > right_intersection.Y){
                                    right_first = edge;
                                    right_intersection = edge_intersection;
                                }
                            }
                        }
                    }
                    
                    PointF intersection;
                    if(left_intersection.Y > right_intersection.Y){
                        intersection = left_intersection;
                        if(!current_left.Equals(end_left)){
                            if(left_first.pointA.Y < left_first.pointB.Y)
                                current_left = left_first.pointA;
                            else
                                current_left = left_first.pointB;
                        }
                        left_edges.Remove(left_first);
                    }
                    else{
                        intersection = right_intersection;
                        if(!current_right.Equals(end_right)){
                            if(right_first.pointA.Y < right_first.pointB.Y)
                                current_left = right_first.pointA;
                            else
                                current_right = right_first.pointB;
                        }
                        right_edges.Remove(right_first);
                    }
                    if(current_edge.edgePA.Y < current_edge.edgePB.Y)
                        current_edge.edgePA = intersection;
                    else
                        current_edge.edgePB = intersection;

                    isInit = false;
                    all_PE.hyper_list.Add((Edge)current_edge.getClone());
                    if(current_left.Equals(end_left) && current_right.Equals(end_right)) break;

                    current_edge = calMath.getVoronoiEdge(current_left, current_right);
                    if(current_edge.edgePA.Y < current_edge.edgePB.Y)
                        current_edge.edgePB = intersection;
                    else
                        current_edge.edgePA = intersection;
                    //all_PE.hyper_list.Add((Edge)current_edge.getClone());
                    
                }

                //eL.Add(bottom_edge);
                all_PE.edges_list.AddRange(left_edges);
                all_PE.edges_list.AddRange(right_edges);
                addToBuffer(all_PE);
                return all_PE;
            }
        }

        public Queue<List<PointEdgeRecoder>> getRecoderBuffer(){
            return recoder_buffer;
        }

        private void addToBuffer(PointEdgeRecoder recoder){
            List<PointEdgeRecoder> PE_recoder = new List<PointEdgeRecoder>();
            PE_recoder.Add((PointEdgeRecoder) recoder.getClone());
            recoder_buffer.Enqueue(PE_recoder);
        }

        private void addToBuffer(PointEdgeRecoder recoder1, PointEdgeRecoder recoder2){
            List<PointEdgeRecoder> PE_recoder = new List<PointEdgeRecoder>();
            PE_recoder.Add((PointEdgeRecoder) recoder1.getClone());
            PE_recoder.Add((PointEdgeRecoder) recoder2.getClone());
            recoder_buffer.Enqueue(PE_recoder);
        }
    }
}
